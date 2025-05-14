using System.IO;
using System.Threading.Tasks;
using LLMUnity;
using UnityEngine;
using UnityEngine.UI;
using System;


public class ChatManager : MonoBehaviour
{
    [SerializeField] private ChatUIController chatUI;
    [SerializeField] private string logFilePath = "chat_log.txt";
    [SerializeField] private string waitMessage = "\"（考え中です…）\"";
    [SerializeField] private LLMCharacter llmCharacter;
    [SerializeField] private LLM llm;
    [SerializeField] private LipSyncSimulator lipSyncSimulator;
    // 設定ファイルを読み込んで、LLMCharacterを初期化する。
    // プロンプトの初期化、ユーザ名・AIキャラクタ名の初期化、使用するモデルの初期化などを行う。
    private void Awake()
    {
        // ローカルLLMの初期化
        LLMConfig llmConfig = AppConfigManager.Instance.Config.llm;
        llmCharacter.SetPrompt(llmConfig.agentSystemPromptFile);
        llmCharacter.playerName = llmConfig.userName;
        llmCharacter.AIName = llmConfig.assistantName;
        llmCharacter.temperature = llmConfig.temperature;
        llm.maxContextLength = llmConfig.maxContextLength;
        llm.SetModel(Path.Combine(Application.streamingAssetsPath, "LLM", llmConfig.modelName));
        waitMessage = llmConfig.waitMessage;
        _ = llmCharacter.Warmup(WarmupCompleted);
        chatUI.SetText("Loading LLM...");
        chatUI.SetEnable(false);

    }
    //void Start()
    //{
    //}

    private void WarmupCompleted()
    {
        Debug.Log("ChatManager - WarmupCompleted: LLM is ready.");
        chatUI.SetText(AppConfigManager.Instance.Config.llm.welcomeMessage);
        chatUI.SetEnable(true);
        chatUI.AddInputFieldEventHandler(OnSubmit); // Fix applied here
    }

    public void OnSubmit(string _input)
    {
        string userInput = chatUI.GetInputField();
        Debug.Log("OnSubmit called");
        Debug.Log("Input: " + userInput);
            // enter 単体 → 送信処理
        if (!string.IsNullOrWhiteSpace(userInput))
        {
            // ローカルLLMを呼び出しているのでリモートLLMも呼び出せるようにロジックを変更する
            _ = llmCharacter.Chat(userInput, HandleReply, OnComplete);
            // 待機メッセージを表示し、入力フォームをクリアする
            // ここはローカルLLM/リモートLLMのどちらでも処理する
            chatUI.SetText(waitMessage);
            chatUI.ClearInputField();
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
    }
}
