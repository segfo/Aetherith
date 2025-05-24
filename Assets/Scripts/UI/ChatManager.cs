using System.IO;
using LLMUnity;
using UnityEngine;
using System;
using System.Text;
using UniVRM10;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections;

public class ChatManager : MonoBehaviour
{
    [SerializeField] private ChatUIController chatUI;
    [SerializeField] private string logFilePath = "chat_log.txt";
    [SerializeField] private string waitMessage = "\"（考え中です…）\"";
    [SerializeField] private LLMCharacter llmCharacter;
    [SerializeField] private LLMCharacter llmCharacterEmotional;
    [SerializeField] private CharacterController vrmCharacter;
    [SerializeField] private LLM llm;
    [SerializeField] private LLM llmEmotional;
    [SerializeField] private LipSyncSimulator lipSyncSimulator;
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
        await Task.Delay(2000);
        chatUI.AppendTextLine("SYSTEM - LLM Loader: ローカルLLMをセットアップしています...");
        Debug.Log("ChatManager - LLMの初期化を開始します。");
        await Task.Run(()=> { LoadLLM(); });
    }
    void LoadLLM()
    {
        // ローカルLLMの初期化
        AppConfig config = AppConfigManager.Instance.Config;
        string systemPromptPath = Path.Combine(Application.streamingAssetsPath, config.characterLlm.systemPromptFile);
        string emotionalSystemPromptPath = Path.Combine(Application.streamingAssetsPath, config.emotionLlm.systemPromptFile);
        string systemPrompt = SafeFileReader.ReadOrCreateTextFile(systemPromptPath, Encoding.UTF8, "あなたは優秀なAIアシスタントです。");
        string emotionalSystemprompt = SafeFileReader.ReadOrCreateTextFile(emotionalSystemPromptPath, Encoding.UTF8, "あなたは優秀な感情判定アシスタントです。あなたはユーザの発言に応じて、AIの感情を推定します。");
        // 実LLMキャラクターのセットアップ
        SetupLLMCharactor(systemPrompt, config.characterLlm, llmCharacter);
        llm.maxContextLength = config.characterLlm.maxContextLength;
        // 感情推測LLMのセットアップ
        SetupLLMCharactor(emotionalSystemprompt, config.emotionLlm, llmCharacterEmotional);
        llmEmotional.maxContextLength = config.emotionLlm.maxContextLength;
        // 待機メッセージを設定する
        waitMessage = config.waitMessage;
        // ここからLLMのロード処理
        // SetModelメソッドはUnityEditorで実行したとき
        // LLM.csでメインスレッドで動かさないと例外吐くのでその対策する。
        // UnityEditorで動かさなければLLM.csではメインスレッドで動かすべき関数は呼ばれないので
        // 完全に別スレッドで実行して問題ない。
#if UNITY_EDITOR
        dispatcher.Enqueue(() =>
        {
#endif
            llm.SetModel(Path.Combine(Application.streamingAssetsPath, "LLM", config.characterLlm.modelName));
            llmEmotional.SetModel(Path.Combine(Application.streamingAssetsPath, "LLM", config.emotionLlm.modelName));
#if UNITY_EDITOR
        });
#endif
        _ =llmCharacterEmotional.Warmup(EmotionAiWarmupCompleted);
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
                chatUI.AppendTextLine($"SYSTEM：{loadResources[loadResource]}を読み込みました。");
                loadResources.Remove(loadResource);
                // 全てのリソースが読み込まれたのでチャットUIを有効にする
                if (loadResources.Count == 0)
                {
                    AppConfig config = AppConfigManager.Instance.Config;
                    chatUI.SetText(config.welcomeMessage);
                    chatUI.InputFieldSetEnable(true);
                    chatUI.AddInputFieldEventHandler(OnSubmit);
                    chatUI.ActivateInputField();
                    return;
                }
                // 残りのリソースを表示する
                chatUI.AppendTextLine("SYSTEM：しばらくお待ちください。");
                chatUI.AppendTextLine("SYSTEM：読み込み中のリソース");
                string reamining_resource = "";
                foreach (var resource in loadResources)
                {
                    reamining_resource += " - "+resource.Value+"\n";
                }
                chatUI.AppendTextLine(reamining_resource);
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

    readonly Dictionary<string, ExpressionKey> ExpressionList = new Dictionary<string, ExpressionKey> {
        { "Happy", ExpressionKey.Happy },
        { "Sad", ExpressionKey.Sad },
        { "Angry", ExpressionKey.Angry },
        { "Surprised", ExpressionKey.Surprised },
        { "Neutral", ExpressionKey.Neutral },
        { "Relaxed" , ExpressionKey.Relaxed }
    };

    private void SetVrmExpression(Dictionary<string, float> expressionList)
    {
        try
        {
            foreach (KeyValuePair<string, float> kvp in expressionList)
            {
                ExpressionKey exp = ExpressionKey.Neutral;
                ExpressionList.TryGetValue(kvp.Key, out exp);
                vrmCharacter.SetExpression(exp, kvp.Value);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("SetVrmExpression: " + e.Message);
            foreach (KeyValuePair<string, ExpressionKey> kvp in ExpressionList)
            {
                vrmCharacter.SetExpression(kvp.Value, 0);
            }
        }
    }
    private void ResetVrmExpression()
    {
        foreach (KeyValuePair<string, ExpressionKey> kvp in ExpressionList)
        {
            vrmCharacter.SetExpression(kvp.Value, 0);
        }
    }
    Dictionary<string, float> expressionList;
    async public void OnSubmit(string _input)
    {
        //ResetVrmExpression();
        //UniVRM10.ExpressionKey.Neutral;
        string userInput = chatUI.GetInputField();
        Debug.Log("OnSubmit called");
        Debug.Log("Input: " + userInput);
            // enter 単体 → 送信処理
        if (!string.IsNullOrWhiteSpace(userInput))
        {
            // ローカルLLMを呼び出しているのでリモートLLMも呼び出せるようにロジックを変更する
            chatUI.SetText(waitMessage);
            // 待ちモーションを再生する

            chatUI.ClearInputField();
            expressionList = JsonConvert.DeserializeObject<Dictionary<string, float>>(await llmCharacterEmotional.Chat(userInput));

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
        // 表情の閾値が超えていたら瞬きを止める
        if (expressionList["Happy"] > filter.Happy||expressionList["Sad"] > filter.Sad||
            expressionList["Angry"] > filter.Angry||expressionList["Surprised"] > filter.Surprise||
            expressionList["Relaxed"] > filter.Relaxed||expressionList["Neutral"] > filter.Neutral)
        {
            // 瞬きを無効化する。目は開いたままにする
            // 第二引数のopenが0.0f（開く）なのは表情のモーフィングで上書きされるため開いたままでよい
            blinkController.SetBlinkEnabled(false,0.0f);
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
        blinkController.SetBlinkEnabled(true, 0.0f);
        llmCharacter.AddAIMessage(receivedText);
        receivedText = "";
        lipSyncSimulator.LipSyncEnd();
        // 表情を戻す
        StartCoroutine(nameof(ExpressionFadeout));
    }
    private IEnumerator ExpressionFadeout()
    {
        CancelFade(); // 既存のフェードをキャンセル
        yield return FadeOutExpressionsAfterDelay(
                UnityEngine.Random.Range(1.0f, 2.0f), // フェードアウトまでの時間
                UnityEngine.Random.Range(2.5f, 4.0f)  // フェードアウトにかける時間
        );
    }
    // 表情周りの処理
    // 後々表情専用のクラスを作ってそこに追い出す予定
    private Coroutine fadeCoroutine;
    /// 表情を取得する
    public Dictionary<ExpressionKey, float> GetCurrentExpressionWeights(Vrm10RuntimeExpression expression)
    {
        var result = new Dictionary<ExpressionKey, float>();

        foreach (var key in ExpressionList.Keys)
        {
            float weight = expression.GetWeight(ExpressionList[key]);
            if (Mathf.Abs(weight) > 0.001f) // 0に近いものは省略（必要に応じて）
            {
                result[ExpressionList[key]] = weight;
            }
        }
        return result;
    }

    /// <summary>
    /// 現在進行中のフェードをキャンセル
    /// </summary>
    public void CancelFade()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
            fadeCoroutine = null;
        }
    }
    // 表情を徐々にフェードアウトさせていく処理
    private IEnumerator FadeOutExpressionsAfterDelay(float delay, float fadeTime)
    {
        Vrm10Instance vrmInstance = vrmCharacter.vrmInstance;
        yield return new WaitForSeconds(delay);
        Dictionary<ExpressionKey, float> presets = GetCurrentExpressionWeights(vrmInstance.Runtime.Expression);
        var expressionKeys = new List<ExpressionKey>();
        var startWeights = new Dictionary<ExpressionKey, float>();

        foreach (var preset in presets)
        {
            var key = preset.Key;
            expressionKeys.Add(key);
            float currentWeight = vrmInstance.Runtime.Expression.GetWeight(key);
            startWeights[key] = currentWeight;
        }
        float elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeTime);

            foreach (var key in expressionKeys)
            {
                float start = startWeights[key];
                float current = Mathf.Lerp(start, 0f, t);
                vrmInstance.Runtime.Expression.SetWeight(key, current);
            }

            yield return null;
        }

        foreach (var key in expressionKeys)
        {
            vrmInstance.Runtime.Expression.SetWeight(key, 0f);
        }
        fadeCoroutine = null; // フェード完了
    }
}
