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
    [SerializeField] private string waitMessage = "\"�i�l�����ł��c�j\"";
    [SerializeField] private LLMCharacter llmCharacter;
    [SerializeField] private LLM llm;
    [SerializeField] private LipSyncSimulator lipSyncSimulator;
    // �ݒ�t�@�C����ǂݍ���ŁALLMCharacter������������B
    // �v�����v�g�̏������A���[�U���EAI�L�����N�^���̏������A�g�p���郂�f���̏������Ȃǂ��s���B
    private void Awake()
    {
        
        LLMConfig llmConfig = AppConfigManager.Instance.Config.llm;
        llmCharacter.SetPrompt(llmConfig.agentSystemPromptFile);
        llmCharacter.playerName = llmConfig.userName;
        llmCharacter.AIName = llmConfig.assistantName;
        llmCharacter.temperature = llmConfig.temperature;
        llm.maxContextLength = llmConfig.maxContextLength;
        llm.SetModel(Path.Combine(Application.streamingAssetsPath, "LLM", llmConfig.modelName));
        waitMessage = llmConfig.waitMessage;
    }
    void Start()
    {
        _ = llmCharacter.Warmup(WarmupCompleted);
        chatUI.SetText("Loading LLM...");
        chatUI.SetEnable(false);
    }

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
            // enter �P�� �� ���M����
        if (!string.IsNullOrWhiteSpace(userInput))
        {
        _ = llmCharacter.Chat(userInput, HandleReply, OnComplete);
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

        // �擪�����v���Ă��镶�������J�E���g
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
