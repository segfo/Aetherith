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
using Unity.VisualScripting;
using System.Threading;


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
    MainThreadDispatcher dispatcher;

    // 設定ファイルを読み込んで、LLMCharacterを初期化する。
    // プロンプトの初期化、ユーザ名・AIキャラクタ名の初期化、使用するモデルの初期化などを行う。
    void Awake()
    {
        dispatcher = MainThreadDispatcher.Instance;
    }
    async void Start()
    {
        chatUI.InputFieldSetEnable(false);
        await Task.Delay(2000);
        chatUI.AppendTextLine("SYSTEM - LLM Loader: ローカルLLMをセットアップしています...");
        Debug.Log("ChatManager - LLMの初期化を開始します。");
        //await Task.Run(()=> { LoadLLM(); });
    }
    void LoadLLM()
    {
        // ローカルLLMの初期化
        LLMConfig llmConfig = AppConfigManager.Instance.Config.llm;
        string systemPromptPath = Path.Combine(Application.streamingAssetsPath, llmConfig.agentSystemPromptFile);
        string emotionalSystemPromptPath = Path.Combine(Application.streamingAssetsPath, llmConfig.emotionSystemPromptFile);
        string systemPrompt = SafeFileReader.ReadOrCreateTextFile(systemPromptPath, Encoding.UTF8, "あなたは優秀なAIアシスタントです。");
        string emotionalSystemprompt = SafeFileReader.ReadOrCreateTextFile(emotionalSystemPromptPath, Encoding.UTF8, "あなたは優秀な感情判定アシスタントです。あなたはユーザの発言に応じて、AIの感情を推定します。");
        // 実LLMキャラクターのセットアップ
        llmCharacter.SetPrompt(systemPrompt);
        llmCharacter.playerName = llmConfig.userName;
        llmCharacter.AIName = llmConfig.assistantName;
        llmCharacter.temperature = (float)llmConfig.temperature;
        llmCharacterEmotional.SetPrompt(emotionalSystemprompt);
        llmCharacterEmotional.playerName = llmConfig.userName;
        llmCharacterEmotional.AIName = llmConfig.assistantName;
        llmCharacter.temperature = (float)llmConfig.emotionalTemperature;
        llm.maxContextLength = llmConfig.maxContextLength;
        llmEmotional.maxContextLength = llmConfig.llmEmotionMaxContextLength;
        // 待機メッセージを設定する
        waitMessage = llmConfig.waitMessage;
        // ここからLLMのロード処理
        // SetModelメソッドはUnityEditorで実行したとき
        // LLM.csでメインスレッドで動かさないと例外吐くのでその対策する。
        // UnityEditorで動かさなければLLM.csではメインスレッドで動かすべき関数は呼ばれないので
        // 完全に別スレッドで実行して問題ない。
#if UNITY_EDITOR
        MainThreadDispatcher.Instance.Enqueue(() =>
        {
#endif
            llm.SetModel(Path.Combine(Application.streamingAssetsPath, "LLM", llmConfig.modelName));
            llmEmotional.SetModel(Path.Combine(Application.streamingAssetsPath, "LLM", llmConfig.llmEmotionModelName));
#if UNITY_EDITOR
        });
#endif
        _ =llmCharacterEmotional.Warmup(EmotionAiWarmupCompleted);
        _ = llmCharacter.Warmup(CharacterAiWarmupCompleted);
    }
    
    // 残リソース一覧
    static Dictionary<LoadResources, string> loadResources = new Dictionary<LoadResources, string>
    {
        { LoadResources.VRM,"VRMモデル"},
        { LoadResources.MainCharacterLLM, "メインキャラクターAI(Local)" },
        { LoadResources.EmotionCharacterLLM, "感情・表情推定AI(Local)"},
    };
    enum LoadResources{
        VRM,MainCharacterLLM, EmotionCharacterLLM
    }
    private void LoadedResource(LoadResources loadResource)
    {
        if (loadResources.ContainsKey(loadResource))
        {
            chatUI.AppendTextLine($"SYSTEM：{loadResources[loadResource]}を読み込みました。");
            loadResources.Remove(loadResource);
            // 全てのリソースが読み込まれたのでチャットUIを有効にする
            if (loadResources.Count == 0)
            {
                LLMConfig llmConfig = AppConfigManager.Instance.Config.llm;
                chatUI.SetText(llmConfig.welcomeMessage);
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
    private void CharacterAiWarmupCompleted()
    {
        LoadedResource(LoadResources.MainCharacterLLM);
    }
    private void EmotionAiWarmupCompleted()
    {
        LoadedResource(LoadResources.EmotionCharacterLLM);  
    }
    public void VrmLoadCompleted()
    {
        LoadedResource(LoadResources.VRM);
    }
    public void AppendTextLine(string msg)
    {
        chatUI.AppendTextLine(msg);
    }

    Dictionary<string, ExpressionKey> ExpressionList = new Dictionary<string, ExpressionKey> {
        { "Happy", ExpressionKey.Happy },
        { "Sad", ExpressionKey.Sad },
        { "Angry", ExpressionKey.Angry },
        { "Surprised", ExpressionKey.Surprised },
        { "Neutral", ExpressionKey.Neutral },
        { "Relaxed" , ExpressionKey.Relaxed }
    };

    private void SetVrmExpressionFromJson(string expressionList)
    {
        try
        {
            Dictionary<string, float> dict = JsonConvert.DeserializeObject<Dictionary<string, float>>(expressionList);
            foreach (KeyValuePair<string, float> kvp in dict)
            {
                ExpressionKey exp = ExpressionKey.Neutral;
                ExpressionList.TryGetValue(kvp.Key, out exp);
                vrmCharacter.SetExpression(exp, kvp.Value);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("SetVrmExpressionFromJson: " + e.Message);
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
    string emoJson = "";
    async public void OnSubmit(string _input)
    {
        ResetVrmExpression();
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
            emoJson = await llmCharacterEmotional.Chat(userInput);
            // 感情を取得し表情に反映する。
            // VRMの表情を変更する処理を追加する。
            // LLMからは {"Happy": "0.6", "Sad": "0.2", ...} のようなJSONが返ってくる。
            _ = llmCharacter.Chat(userInput, HandleReply, OnComplete);
            // 待機メッセージを表示し、入力フォームをクリアする
            // ここはローカルLLM/リモートLLMのどちらでも処理する
            llmCharacter.AddPlayerMessage(userInput);
            llmCharacterEmotional.AddPlayerMessage(userInput);
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
        SetVrmExpressionFromJson(emoJson);
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
        receivedText = "";
        lipSyncSimulator.LipSyncEnd();
        Task.Run(() => {
            Thread.Sleep(UnityEngine.Random.Range(1000,2000));
            ResetVrmExpression();
        });
    }
}
