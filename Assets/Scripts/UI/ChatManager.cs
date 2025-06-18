using LLMUnity;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UniVRM10;

enum LipSyncState
{
    Initialized,Continue,OnComplete,Finalized
}
public enum ChatManagerInitializeState
{
    InitializePending, Initialized, Ready
}

public class ChatManager : MonoBehaviour
{
    [SerializeField] private ChatUIController chatUI;
    [SerializeField] private string logFilePath = "chat_log.txt";
    [SerializeField] private string waitMessage = "\"（考え中です…）\"";
    [SerializeField] private CharacterController vrmCharacter;
    [SerializeField] private LipSyncSimulator lipSyncSimulator;
    [SerializeField] private ThinkingMotionManager thinkingAnimation;

    private ILLMChat llmCharacter = null;
    private ILLMChat llmCharacterEmotional = null;
    private LLM llm = null;
    private LLM llmEmotional = null;
    private GameObject characterLLM = null;
    private GameObject emotionalLLM = null;
    public ChatManagerInitializeState InitializeState { get; private set; } = ChatManagerInitializeState.InitializePending;
    private MainThreadDispatcher mainThreadDispatcher;
    private BlinkController blinkController;
    private LipSyncState onComplete = LipSyncState.Initialized;

    // 残リソース一覧
    private Dictionary<LoadResources, string> loadResources;

    // 設定ファイルを読み込んで、LLMCharacterを初期化する。
    // プロンプトの初期化、ユーザ名・AIキャラクタ名の初期化、使用するモデルの初期化などを行う。
    void Awake()
    {
        mainThreadDispatcher = MainThreadDispatcher.Instance;
        loadResources = new Dictionary<LoadResources, string>
        {
            { LoadResources.VRM,"VRMモデル"},
            { LoadResources.MainCharacterLLM, $"キャラクターAI ({UseLLMName(AppConfigManager.Instance.Config.characterLlm)})" },
            { LoadResources.EmotionCharacterLLM, $"感情・表情推定AI ({UseLLMName(AppConfigManager.Instance.Config.emotionLlm)})"},
        };
        lipSyncSimulator.OnLipSyncEnd += DoFinalizeAfterLipSync;
    }

    bool externalApiUse(LLMConfig llm)
    {
        return llm.useLLM!=LLMProvider.Local;
    }

    async void Start()
    {
        chatUI.InputFieldSetEnable(false);
        _ = chatUI.StartTypingAppendSystem("SYSTEM: LLMをセットアップしています...\n");

        AppConfig config = AppConfigManager.Instance.Config;

        // LLM GameObjects
        characterLLM = new GameObject("CharacterLLM");
        emotionalLLM = new GameObject("EmotionalLLM");
        characterLLM.SetActive(false);
        emotionalLLM.SetActive(false);

        // LLM選定
        await InitializeCharacterLLM(config);
        await InitializeEmotionLLM(config);

        // 設定反映
        waitMessage = config.waitMessage;
        AppConfigManager.Instance.OnConfigUpdated += (newConfig) => {
            waitMessage = newConfig.waitMessage;
        };

        // 最後にGameObjectを有効化してロード
        await ActivateLLMGameObjects();
        LoadLLMs();
    }

    private async Task InitializeCharacterLLM(AppConfig config)
    {
        LLMCharacter llmCharacter = null;
        if (externalApiUse(config.characterLlm))
        {
            this.llmCharacter = new RemoteDifyCharacterChat(
                AppConfigManager.Instance.Config.characterLlm.Dify.apiUrl,
                AppConfigManager.Instance.Config.characterLlm.Dify.apiKey
            );
            // 外部API使用時の初期化（省略）
            return;
        }

        // ローカルLLM構成
        llm = characterLLM.AddComponent<LLM>();
        llmCharacter = characterLLM.AddComponent<LLMCharacter>();
        this.llmCharacter = new LocalLlmCharacterChat(llmCharacter);
        SetupLLMCharacter(config.characterLlm.Local, llmCharacter, llm, "あなたは優秀なAIアシスタントです。");
        await Task.Run(() =>
        {
            SetLocalModelPath(llm, config.characterLlm.Local.modelName);
        });
        
    }

    private async Task InitializeEmotionLLM(AppConfig config)
    {
        LLMCharacter llmCharacterEmotional = null;
        if (externalApiUse(config.emotionLlm))
        {
            // 外部API使用時の初期化（省略）
            this.llmCharacterEmotional = new RemoteDifyLlmEmotionChat(
                AppConfigManager.Instance.Config.characterLlm.Dify.apiUrl,
                AppConfigManager.Instance.Config.characterLlm.Dify.apiKey
            );
            return;
        }

        bool useSameLLM = config.characterLlm.Local.modelName == config.emotionLlm.Local.modelName && !externalApiUse(config.characterLlm);
        if (useSameLLM)
        {
            // 同一モデル名＆キャラもローカル → 共通インスタンス
            llmEmotional = llm;
            llmCharacterEmotional = characterLLM.AddComponent<LLMCharacter>();
        }
        else
        {
            llmEmotional = emotionalLLM.AddComponent<LLM>();
            llmCharacterEmotional = emotionalLLM.AddComponent<LLMCharacter>();
        }
        this.llmCharacterEmotional = new LocalLlmEmotionChat(llmCharacterEmotional);
        SetupLLMCharacter(config.emotionLlm.Local, llmCharacterEmotional, llmEmotional,
            "あなたは優秀な感情判定アシスタントです。ユーザの発言に応じてAIの感情を推定します。");
        if (!useSameLLM)
        {
            await Task.Run(() =>
            {
                SetLocalModelPath(llmEmotional, config.emotionLlm.Local.modelName);
            });
        }
    }

    private void SetLocalModelPath(LLM llmInstance, string modelName)
    {
#if UNITY_EDITOR
        mainThreadDispatcher.Enqueue(() => {
#endif
            string modelPath = Path.Combine(Application.streamingAssetsPath, "LLM", modelName);
            string verifiedPath = SafeFileReader.PathVerifier(Application.streamingAssetsPath, modelPath);
            llmInstance.SetModel(verifiedPath);
#if UNITY_EDITOR
        });
#endif
    }

    private async Task ActivateLLMGameObjects()
    {
        characterLLM.SetActive(true);
        emotionalLLM.SetActive(true);
        await Task.Yield();
    }

    private void LoadLLMs()
    {
        _ = llmCharacter?.Warmup(CharacterAiWarmupCompleted);
        _ = llmCharacterEmotional?.Warmup(EmotionAiWarmupCompleted);
    }

    private void SetupLLMCharacter(LocalLLMConfig config, LLMCharacter character, LLM llm, string defaultPrompt)
    {
        string promptPath = SafeFileReader.PathVerifier(Application.streamingAssetsPath, Path.Combine(Application.streamingAssetsPath, config.systemPromptFile));
        string promptText = SafeFileReader.ReadOrCreateTextFile(promptPath, Encoding.UTF8, defaultPrompt);

        character.SetPrompt(promptText);
        character.playerName = config.userName;
        character.AIName = config.assistantName;
        character.temperature = config.temperature;
        character.topK = config.topK;
        character.topP = config.topP;

        llm.maxContextLength = config.maxContextLength;
        llm.numGPULayers = config.numGPULayers;
    }

    enum LoadResources{
        VRM,MainCharacterLLM, EmotionCharacterLLM
    }
    private async void LoadedResource(LoadResources loadResource)
    {
        try {
            if (loadResources.ContainsKey(loadResource))
            {
                _ = chatUI.StartTypingAppendSystem($"SYSTEM：{loadResources[loadResource]}のセットアップが完了しました。\n");
                loadResources.Remove(loadResource);
                // 全てのリソースが読み込まれたのでチャットUIを有効にする
                if (loadResources.Count == 0)
                {
                    AppConfig config = AppConfigManager.Instance.Config;
                    InitializeState = ChatManagerInitializeState.Initialized;
                    await chatUI.StartTypingSystem(config.welcomeMessage);
                    chatUI.InputFieldSetEnable(true);
                    chatUI.AddInputFieldEventHandler(OnSubmit);
                    chatUI.ActivateInputField();
                    InitializeState = ChatManagerInitializeState.Ready;
                    return;
                }
                // 残りのリソースを表示する
                _ = chatUI.StartTypingAppendSystem("SYSTEM：しばらくお待ちください。\n");
                _ = chatUI.StartTypingAppendSystem("SYSTEM：セットアップ中のリソース\n");
                string reamining_resource = "";
                foreach (var resource in loadResources)
                {
                    reamining_resource += $" - {resource.Value}\n";
                }
                 _ = chatUI.StartTypingAppendSystem(reamining_resource);
            }
            else
            {
                Debug.LogError($"ChatManager: loadedResource - {loadResource.ToString()} is not found.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"ChatManager-Exception: {e.Message}");
        }
    }
    private string UseLLMName(LLMConfig conf) {
        if (externalApiUse(conf)) {
            return $"Remote({conf.useLLM})";
        }
        else
        {
            return "Local";
        }
    }

    private void CharacterAiWarmupCompleted()
    {
        mainThreadDispatcher.Enqueue(() =>
        {
            LoadedResource(LoadResources.MainCharacterLLM);
        });
    }
    private void EmotionAiWarmupCompleted()
    {
        mainThreadDispatcher.Enqueue(() =>
        {
            LoadedResource(LoadResources.EmotionCharacterLLM);
        });
    }
    public void VrmLoadCompleted()
    {
        // VRMモデルのロードが完了しので、BlinkControllerを取得しておく。
        blinkController = vrmCharacter.vrmInstance.GetComponent<BlinkController>();
        thinkingAnimation.SetAnimator(vrmCharacter.vrmInstance.GetComponent<Animator>());
        mainThreadDispatcher.Enqueue(() =>
        {
            LoadedResource(LoadResources.VRM);
        });
    }
    public void TypingAppendTextSystem(string msg)
    {
        chatUI.StartTypingAppendSystem(msg);
    }
    public void TypingTextSystem(string msg)
    {
        chatUI.StartTypingSystem(msg);
    }

    public void AppendTextLine(string msg)
    {
        chatUI.AppendTextLine(msg);
    }
    public void TypingAppendText(string msg)
    {
        chatUI.StartTypingAppend(msg);
    }
    public void TypingText(string msg)
    {
        chatUI.StartTyping(msg);
    }

    private void SetVrmExpression(Dictionary<string, float> expressionList)
    {
        try
        {
            foreach (KeyValuePair<string, float> kvp in expressionList)
            {
                ExpressionKey exp = ExpressionKey.Neutral;
                ExpressionController.ExpressionList.TryGetValue(kvp.Key, out exp);
                vrmCharacter.SetExpression(exp, kvp.Value);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("SetVrmExpression: " + e.Message);
            foreach (KeyValuePair<string, ExpressionKey> kvp in ExpressionController.ExpressionList)
            {
                vrmCharacter.SetExpression(kvp.Value, 0);
            }
        }
    }

    Dictionary<string, float> expressionList;
    bool firstReply = true;

    async public void OnSubmit(string _input)
    {
        // Continueなら実行中
        if (onComplete==LipSyncState.Continue) {
            return;
        }
        // 入力フィールドを消す
        chatUI.InputFieldSetEnable(false);
        firstReply = true;
        ExpressionController.Instance.ResetVrmExpression();
        string userInput = chatUI.GetInputField();
        Debug.Log("OnSubmit called");
        if (!string.IsNullOrWhiteSpace(userInput))
        {
            Debug.Log("Input: " + userInput);

            // 待ちモーションを再生する
            thinkingAnimation.DoThinking();
            // 考え中メッセージを出して、入力欄を消す
            _ = chatUI.StartTypingSystem(waitMessage);
            chatUI.ClearInputField();
            // 感情推定AIを呼び出す　
            string emotionText = await llmCharacterEmotional.Chat(userInput);
            string extractString = Regex.Match(emotionText.Replace("\r","").Replace("\n",""), @"```json(.+)```").Groups[1].Value;
            if (extractString == "") { extractString = emotionText; }
            Debug.Log("Emotion JSON: " + emotionText.Replace("\r", "").Replace("\n", ""));
            Debug.Log("Extracted Emotion JSON: "+extractString);
            try
            {
                expressionList = JsonConvert.DeserializeObject<Dictionary<string, float>>(extractString);
            }
            catch (JsonException e)
            {
                Debug.LogError("JSON Parse Error: " + e.Message);
                expressionList = new Dictionary<string, float>
                {
                    { "Happy", 0.0f },
                    { "Sad", 0.0f },
                    { "Angry", 0.0f },
                    { "Surprised", 0.0f },
                    { "Relaxed", 0.0f },
                    { "Neutral", 0.0f }
                };
            }
            // 感情を取得し表情に反映する。
            // VRMの表情を変更する処理を追加する。
            // LLMからは {"Happy": "0.6", "Sad": "0.2", ...} のようなJSONが返ってくる。
            _ = llmCharacter.Chat(userInput, ClearChatText , HandleReply, OnComplete);
            // 待機メッセージを表示し、入力フォームをクリアする
            // ここはローカルLLM/リモートLLMのどちらでも処理する
            //llmCharacter.AddPlayerMessage(userInput);
        }
        onComplete = LipSyncState.Continue;
        lipSyncSimulator.LipSyncStart();
    }

    void ClearChatText()
    {
        chatUI.StartTyping("");
    }

    private void AppendToLog(string message)
    {
        System.IO.File.AppendAllText(logFilePath, message + "\n");
    }

    async void VRMExpressionChange()
    {
        BlinkExclusionExpressionThreshold filter = AppConfigManager.Instance.Config.vrm.blinkExclusionExpressionThreshold;
        // 表情の閾値が超えていたら瞬きを止める(blinkDisableなら瞬きをする。例えば目を開けてびっくりした表情などの時)
        try
        {
            if (firstReply)
            {
                // 考え中アニメーションから元に戻す
                await thinkingAnimation.DoneThinking();
                if (expressionList["Happy"] > filter.Happy || expressionList["Sad"] > filter.Sad ||
                    expressionList["Angry"] > filter.Angry || expressionList["Surprised"] > filter.Surprise ||
                    expressionList["Relaxed"] > filter.Relaxed || expressionList["Neutral"] > filter.Neutral)
                {
                    // 瞬きを無効化する。目は開いたままにする
                    // 第二引数のopenが0.0f（開く）なのは表情のモーフィングで上書きされるため開いたままでよい
                    // 糸目キャラが驚いたときに目を開けるとか、怒った時に目を開ける表情になるとか、そういう時に瞬きを有効化する　
                    blinkController.SetBlinkEnabled(AppConfigManager.Instance.Config.vrm.blinkDisable, 0.0f);
                }
                firstReply = false;
            }
        }
        catch (KeyNotFoundException e)
        {
            Debug.LogError("表情に対応するキーがありません: " + e.Message);
        }
        SetVrmExpression(expressionList);
    }

    private void HandleReply(string reply)
    {
        VRMExpressionChange();
        chatUI.StartTypingAppend(reply);
        // ここではLip Syncしない。
        // TypewriterEffectが実行する。
    }
    
    public void OnComplete()
    {
        Debug.Log("OnCompleted");
        if (onComplete == LipSyncState.Continue) {
            onComplete = LipSyncState.OnComplete;
            DoFinalizeAfterLipSync(lipSyncSimulator.TotalSyllableCount);
        }
    }
    public void DoFinalizeAfterLipSync(int totalSyllableCount)
    {
        //Debug.Log($"{totalSyllableCount} < {llmCharacter.TotalSyllableCount}");
        if (llmCharacter.TotalSyllableCount==0 || totalSyllableCount < llmCharacter.TotalSyllableCount|| onComplete!=LipSyncState.OnComplete) { return; }
        onComplete = LipSyncState.Finalized;
        // 入力フィールドを再表示する
        chatUI.InputFieldSetEnable(true);
        Debug.Log("LipSyncEnd");
        // まだLipSync中だったらOnCompleteの処理を遅らせる
        lipSyncSimulator.LipSyncEnd();
        // 表情を戻す
        float wait = UnityEngine.Random.Range(1.0f, 2.0f);
        float fadeoutPlay = UnityEngine.Random.Range(1.5f, 3.0f);

        ExpressionController.Instance.StartExpressionFadeout(wait, fadeoutPlay);
        Task.Run(async () =>
        {
            // 目が開くまで瞬きを待つ
            await Task.Delay((int)((wait + fadeoutPlay) * 1000));
            blinkController.SetBlinkEnabled(!AppConfigManager.Instance.Config.vrm.blinkDisable, 0.0f);
        });
    }
}
