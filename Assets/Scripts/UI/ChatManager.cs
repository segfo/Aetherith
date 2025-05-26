using System.IO;
using LLMUnity;
using UnityEngine;
using System;
using System.Text;
using UniVRM10;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
public class ChatManager : MonoBehaviour
{
    [SerializeField] private ChatUIController chatUI;
    [SerializeField] private string logFilePath = "chat_log.txt";
    [SerializeField] private string waitMessage = "\"（考え中です…）\"";
    [SerializeField] private CharacterController vrmCharacter;
    [SerializeField] private LipSyncSimulator lipSyncSimulator;

    private LLMCharacter llmCharacter;
    private LLMCharacter llmCharacterEmotional;
    private LLM llm;
    private LLM llmEmotional;
    public bool Initialized { get; private set; } = false;
    private MainThreadDispatcher dispatcher;
    private BlinkController blinkController;
    // 残リソース一覧
    private Dictionary<LoadResources, string> loadResources;
    // 設定ファイルを読み込んで、LLMCharacterを初期化する。
    // プロンプトの初期化、ユーザ名・AIキャラクタ名の初期化、使用するモデルの初期化などを行う。
    void Awake()
    {
        dispatcher = MainThreadDispatcher.Instance;
        loadResources = new Dictionary<LoadResources, string>
        {
            { LoadResources.VRM,"VRMモデル"},
            { LoadResources.MainCharacterLLM, "メインキャラクターAI(Local)" },
            { LoadResources.EmotionCharacterLLM, "感情・表情推定AI(Local)"},
        };
    }
    async void Start()
    {
        chatUI.InputFieldSetEnable(false);
        //chatUI.AppendTextLine("SYSTEM: ローカルLLMをセットアップしています...");
        chatUI.StartTypingAppend("SYSTEM: ローカルLLMをセットアップしています...\n");
        // ゲームオブジェクトを無効化して、追加したコンポーネントの初期化を遅らせる。
        SetupLLM();
        await Task.Run(() => { LoadLLM(); });
    }
    void SetupLLM()
    {
        GameObject characterLLM = new GameObject("CharacterLLM");
        GameObject emotionalLLM = new GameObject("EmotionalLLM");
        characterLLM.SetActive(false);
        emotionalLLM.SetActive(false);

        // 必要なコンポーネントを追加（すでにあるなら追加しない）
        if (!characterLLM.TryGetComponent<LLM>(out llm))
            llm = characterLLM.AddComponent<LLM>();

        if (!characterLLM.TryGetComponent<LLMCharacter>(out llmCharacter))
            llmCharacter = characterLLM.AddComponent<LLMCharacter>();

        if (!emotionalLLM.TryGetComponent<LLM>(out llmEmotional))
            llmEmotional = emotionalLLM.AddComponent<LLM>();

        if (!emotionalLLM.TryGetComponent<LLMCharacter>(out llmCharacterEmotional))
            llmCharacterEmotional = emotionalLLM.AddComponent<LLMCharacter>();



        // ローカルLLMの初期化
        AppConfig config = AppConfigManager.Instance.Config;
        string systemPromptPath = Path.Combine(Application.streamingAssetsPath, config.characterLlm.systemPromptFile);
        string emotionalSystemPromptPath = Path.Combine(Application.streamingAssetsPath, config.emotionLlm.systemPromptFile);
        string systemPrompt = SafeFileReader.ReadOrCreateTextFile(systemPromptPath, Encoding.UTF8, "あなたは優秀なAIアシスタントです。");
        string emotionalSystemprompt = SafeFileReader.ReadOrCreateTextFile(emotionalSystemPromptPath, Encoding.UTF8, "あなたは優秀な感情判定アシスタントです。あなたはユーザの発言に応じて、AIの感情を推定します。");
        // 実LLMキャラクターのセットアップ
        string characterGgufModelPath = Path.Combine(Application.streamingAssetsPath, "LLM", config.characterLlm.modelName);
        SetupLLMCharactor(systemPrompt, config.characterLlm, llmCharacter);
        llm.maxContextLength = config.characterLlm.maxContextLength;
        llm.numGPULayers = config.characterLlm.numGPULayers;
        // 感情推測LLMのセットアップ
        string emotionGgufModelPath = Path.Combine(Application.streamingAssetsPath, "LLM", config.emotionLlm.modelName);
        SetupLLMCharactor(emotionalSystemprompt, config.emotionLlm, llmCharacterEmotional);
        llmEmotional.maxContextLength = config.emotionLlm.maxContextLength;
        llmEmotional.numGPULayers = config.emotionLlm.numGPULayers;
        // 待機メッセージを設定する
        waitMessage = config.waitMessage;
        // ここからLLMのロード処理
        // SetModelメソッドはUnityEditorで実行したとき
        // LLM.csでメインスレッドで動かさないと例外吐くのでその対策する。
        // UnityEditorで動かさなければLLM.csではメインスレッドで動かすべき関数は呼ばれないので
        // 完全に別スレッドで実行して問題ない。
        llm.SetModel(characterGgufModelPath);
        llmEmotional.SetModel(emotionGgufModelPath);
        emotionalLLM.SetActive(true);
        characterLLM.SetActive(true);
    }
    void LoadLLM()
    {
        _ = llmCharacterEmotional.Warmup(EmotionAiWarmupCompleted);
        _ = llmCharacter.Warmup(CharacterAiWarmupCompleted);
    }

    private void SetupLLMCharactor(string systemPrompt,LLMConfig config, LLMCharacter charactor)
    {
        charactor.SetPrompt(systemPrompt);
        charactor.playerName = config.userName;
        charactor.AIName = config.assistantName;
        charactor.temperature = config.temperature;
        charactor.topK = config.topK;
        charactor.topP = config.topP;
    }

    enum LoadResources{
        VRM,MainCharacterLLM, EmotionCharacterLLM
    }
    private void LoadedResource(LoadResources loadResource)
    {
        try {
            if (loadResources.ContainsKey(loadResource))
            {
                //chatUI.AppendTextLine($"SYSTEM：{loadResources[loadResource]}を読み込みました。");
                chatUI.StartTypingAppend($"SYSTEM：{loadResources[loadResource]}を読み込みました。\n");
                loadResources.Remove(loadResource);
                // 全てのリソースが読み込まれたのでチャットUIを有効にする
                if (loadResources.Count == 0)
                {
                    AppConfig config = AppConfigManager.Instance.Config;
                    chatUI.StartTyping(config.welcomeMessage);
                    chatUI.InputFieldSetEnable(true);
                    chatUI.AddInputFieldEventHandler(OnSubmit);
                    chatUI.ActivateInputField();
                    Initialized = true;
                    return;
                }
                // 残りのリソースを表示する
                //chatUI.AppendTextLine("SYSTEM：しばらくお待ちください。");
                //chatUI.AppendTextLine("SYSTEM：読み込み中のリソース");
                chatUI.StartTypingAppend("SYSTEM：しばらくお待ちください。\n");
                chatUI.StartTypingAppend("SYSTEM：読み込み中のリソース\n");
                string reamining_resource = "";
                foreach (var resource in loadResources)
                {
                    reamining_resource += " - "+resource.Value+"\n";
                }
                //chatUI.AppendTextLine(reamining_resource);
                 chatUI.StartTypingAppend(reamining_resource);
            }
            else
            {
                Debug.LogError("ChatManager: loadedResource - " + loadResource.ToString() + " is not found.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("ChatManager-Exception: " + e.Message);
        }
    }
    private void CharacterAiWarmupCompleted()
    {
        dispatcher.Enqueue(() =>
        {
            LoadedResource(LoadResources.MainCharacterLLM);
        });
    }
    private void EmotionAiWarmupCompleted()
    {
        dispatcher.Enqueue(() =>
        {
            LoadedResource(LoadResources.EmotionCharacterLLM);
        });
    }
    public void VrmLoadCompleted()
    {
        // VRMモデルのロードが完了しので、BlinkControllerを取得しておく。
        blinkController = vrmCharacter.vrmInstance.GetComponent<BlinkController>();
        dispatcher.Enqueue(() =>
        {
            LoadedResource(LoadResources.VRM);
        });
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
    async public void OnSubmit(string _input)
    {
        ExpressionController.Instance.ResetVrmExpression();
        //UniVRM10.ExpressionKey.Neutral;
        string userInput = chatUI.GetInputField();
        Debug.Log("OnSubmit called");
        Debug.Log("Input: " + userInput);
            // enter 単体 → 送信処理
        if (!string.IsNullOrWhiteSpace(userInput))
        {
            // ローカルLLMを呼び出しているのでリモートLLMも呼び出せるようにロジックを変更する
            chatUI.StartTyping(waitMessage);
            // 待ちモーションを再生する

            chatUI.ClearInputField();
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
            _ = llmCharacter.Chat(userInput, HandleReply, OnComplete);
            // 待機メッセージを表示し、入力フォームをクリアする
            // ここはローカルLLM/リモートLLMのどちらでも処理する
            llmCharacter.AddPlayerMessage(userInput);
        }
        lipSyncSimulator.LipSyncStart();
    }

    private void AppendToLog(string message)
    {
        System.IO.File.AppendAllText(logFilePath, message + "\n");
    }
    string receivedText = "";
    private void HandleReply(string reply)
    {
        BlinkExclusionExpressionTreshold filter = AppConfigManager.Instance.Config.vrm.blinkExclusionExpressionTreshold;
        // 表情の閾値が超えていたら瞬きを止める(blinkDisableなら瞬きをする。例えば目を開けてびっくりした表情などの時)
        if (expressionList["Happy"] > filter.Happy||expressionList["Sad"] > filter.Sad||
            expressionList["Angry"] > filter.Angry||expressionList["Surprised"] > filter.Surprise||
            expressionList["Relaxed"] > filter.Relaxed||expressionList["Neutral"] > filter.Neutral)
        {
            // 瞬きを無効化する。目は開いたままにする
            // 第二引数のopenが0.0f（開く）なのは表情のモーフィングで上書きされるため開いたままでよい
            //blinkController.SetBlinkEnabled(false,0.0f);
            // 糸目キャラが驚いたときに目を開けるとか、怒った時に目を開ける表情になるとか、そういう時に瞬きを有効化する　
            blinkController.SetBlinkEnabled(AppConfigManager.Instance.Config.vrm.blinkDisable, 0.0f);
        }
        

        SetVrmExpression(expressionList);
        chatUI.SetText(reply);
        lipSyncSimulator.SpeakText(GetDiff(reply,receivedText));
        receivedText = reply;
    }
    public string GetDiff(string current,string previous)
    {
        int commonLength = 0;
        int minLength = Math.Min(previous.Length, current.Length);

        // 先頭から一致している文字数をカウント
        for (int i = 0; i < minLength; i++)
        {
            if (previous[i] != current[i])
                break;
            commonLength++;
        }

        string diff = current.Substring(commonLength);
        previous = current;
        return diff;
    }

    public void OnComplete()
    {
        llmCharacter.AddAIMessage(receivedText);
        receivedText = "";
        lipSyncSimulator.LipSyncEnd();
        // 表情を戻す
        float wait = UnityEngine.Random.Range(1.0f, 2.0f);
        float fadeoutPlay = UnityEngine.Random.Range(1.5f, 3.0f);

        ExpressionController.Instance.StartExpressionFadeout(wait,fadeoutPlay);
        Task.Run(async () =>
        {
            // 目が開くまで瞬きを待つ
            await Task.Delay((int)((wait+fadeoutPlay)*1000));
            // blinkController.SetBlinkEnabled(true, 0.0f);
            blinkController.SetBlinkEnabled(!AppConfigManager.Instance.Config.vrm.blinkDisable, 0.0f);
        });
        
    }
}
