using System.Threading.Tasks;
using LLMUnity;
using UnityEngine;
using System;
using Newtonsoft.Json.Linq;

interface ILLMChat
{
    Task<string> Chat(string msg);
    Task<string> Chat(string msg, EmptyCallback PrepareReplyOnceCall = null, Callback<string> HandleReplyCallback = null, EmptyCallback OnCompletionCallback = null, bool addToHistory = true);
    Task Warmup(EmptyCallback completionCallback = null);
    int TotalSyllableCount { get; }
}

public class LocalLlmCharacterChat : ILLMChat
{
    private readonly LLMCharacter character;
    private string receivedText = string.Empty;
    private EmptyCallback OnCompletionCallback = null;
    private Callback<string> HandleReplyCallback = (_) => { Debug.Log("HandleReplyCallbackは呼ばれていませんが正しく終了しました。"); };
    private EmptyCallback PrepareReplyOnceCall = () => { Debug.Log("PrepareReplyOnceCallは呼ばれていませんが正しく終了しました。"); };
    private bool isFirstReply = true;
    public int TotalSyllableCount { get; private set; }

    public LocalLlmCharacterChat(LLMCharacter character)
    {
        this.character = character;
    }

    public async Task<string> Chat(string msg)
    {
        receivedText = msg;
        string llmMsg = await character.Chat(StringDiff.GetDiff(msg, receivedText));
        TotalSyllableCount = llmMsg.Length;
        return llmMsg;
    }

    public Task<string> Chat(string msg,EmptyCallback PrepareReplyOnceCall = null, Callback<string> HandleReplyCallback = null, EmptyCallback OnCompletionCallback = null, bool addToHistory = true)
    {
        isFirstReply = true;
        TotalSyllableCount = 0;
        this.PrepareReplyOnceCall = PrepareReplyOnceCall;
        this.HandleReplyCallback = HandleReplyCallback;
        this.OnCompletionCallback = OnCompletionCallback;
        return character.Chat(msg, HandleReply, Complete, addToHistory);
    }
    private void HandleReply(string msg)
    {
        if (isFirstReply)
        {
            PrepareReplyOnceCall.Invoke();
            isFirstReply = false;
        }
        string recvDiff = StringDiff.GetDiff(msg, receivedText);
        TotalSyllableCount += recvDiff.Length;
        receivedText = msg;
        this.OnCompletionCallback = OnCompletionCallback ?? (() => { Debug.Log("完了コールバックは呼ばれていませんが正しく終了しました。"); });
        HandleReplyCallback(recvDiff);
    }
    private void Complete()
    {
        OnCompletionCallback();
    }

    public Task Warmup(EmptyCallback completionCallback = null)
    {
        Debug.Log("[Local] メインキャラクターLLMがWarmupされました");
        return character.Warmup(completionCallback);
    }
}

internal static class DifyClient
{
    static public async Task<string> ConnectServer(LLMConfig conf, string msg, EmptyCallback PrepareReplyOnceCall = null,
        Callback<string> HandleReplyCallback = null)
    {
        MainThreadDispatcher mainThreadDispatcher = MainThreadDispatcher.Instance;
        var sseClient = new SseClient();
        string fullMessage = string.Empty;
        bool isFirstReply = true;
        string conversion_id = PlayerPrefs.GetString(conf.Dify.handoverConversationIdKeyName);
        await Task.Run(async () => {
            await sseClient.StartSseAsync(msg, conf.Dify.apiUrl, conf.Dify.apiKey, conversion_id, message =>
            {
                try
                {
                    var decode = JObject.Parse(message);
                    if (decode["event"] != null && decode["event"].ToString() == "message")
                    {
                        mainThreadDispatcher.Enqueue(() =>
                        {
                            if (isFirstReply)
                            {
                                isFirstReply = false;
                                PrepareReplyOnceCall?.Invoke();
                            }
                            // 継続して会話をするID
                            string conversationId = decode["conversation_id"]?.ToString();
                            if (!string.IsNullOrEmpty(conversationId))
                            {
                                PlayerPrefs.SetString(conf.Dify.handoverConversationIdKeyName, conversationId);
                                PlayerPrefs.Save(); // 明示的に保存
                            }
                            // 回答
                            var msg = decode["answer"].ToString();
                            HandleReplyCallback?.Invoke(msg);
                            fullMessage += msg;
                        });
                    }
                }
                catch (Exception e)
                {
                    Debug.Log(e);
                }
            }); }
        );
        return fullMessage;
    }
}

public class RemoteDifyCharacterChat : ILLMChat
{
    private readonly string url;
    private readonly string api_key;
    public int TotalSyllableCount { get; private set; }

    public RemoteDifyCharacterChat(string url, string api_key)
    {
        DifyConfig.InitConversationHandover(AppConfigManager.Instance.Config.characterLlm);
        this.url = url;
        this.api_key = api_key;
    }

    public async Task<string> Chat(string msg)
    {
        string llmMsg = await Chat(msg, () => { }, (s) => { }, () => { });
        TotalSyllableCount = llmMsg.Length;
        return llmMsg;
    }

    public async Task<string> Chat(
        string msg,
        EmptyCallback PrepareReplyOnceCall = null,
        Callback<string> HandleReplyCallback = null,
        EmptyCallback OnCompletionCallback = null,
        bool addToHistory = true)
    {
        var conf = AppConfigManager.Instance.Config.characterLlm;
        string llmMsg = await DifyClient.ConnectServer(conf, msg, PrepareReplyOnceCall, HandleReplyCallback);
        TotalSyllableCount = llmMsg.Length;
        OnCompletionCallback?.Invoke();
        return llmMsg;
    }

    public async Task Warmup(EmptyCallback completionCallback = null)
    {
        Debug.Log("[Remote] メインキャラクターLLMがWarmupされました" );
        await Task.Yield();
        completionCallback();
    }
}


public class LocalLlmEmotionChat : ILLMChat
{
    private readonly LLMCharacter character;
    public int TotalSyllableCount { get; private set; }

    public LocalLlmEmotionChat(LLMCharacter character)
    {
        this.character = character;
    }

    public async Task<string> Chat(string msg)
    {
        string s = await character.Chat(msg);
        TotalSyllableCount = s.Length;
        return s;
    }

    public async Task<string> Chat(string msg, EmptyCallback PrepareReplyOnceCall = null, Callback<string> HandleReplyCallback = null, EmptyCallback OnCompletionCallback = null, bool addToHistory = true)
    {
        string llmMsg = await character.Chat(msg, HandleReplyCallback, OnCompletionCallback, addToHistory);
        TotalSyllableCount = llmMsg.Length;
        return llmMsg;
    }

    public async Task Warmup(EmptyCallback completionCallback = null)
    {
        Debug.Log("[Local] 感情推定LLMがWarmupされました");
        await character.Warmup(completionCallback);
    }
}
public class RemoteDifyLlmEmotionChat : ILLMChat
{
    private readonly string url;
    private readonly string api_key;
    public int TotalSyllableCount { get; private set; }

    public RemoteDifyLlmEmotionChat(string url,string api_key)
    {
        DifyConfig.InitConversationHandover(AppConfigManager.Instance.Config.emotionLlm);
        this.url = url;
        this.api_key = api_key;
    }

    public async Task<string> Chat(string msg)
    {
        string llmMsg = await Chat(msg, () => { }, (s) => { }, () => { });
        TotalSyllableCount = llmMsg.Length;
        return llmMsg;
    }

    public async Task<string> Chat(string msg, EmptyCallback PrepareReplyOnceCall = null, Callback<string> HandleReplyCallback = null, EmptyCallback OnCompletionCallback = null, bool addToHistory = true)
    {
        var conf = AppConfigManager.Instance.Config.emotionLlm;
        string emoteExpression = await DifyClient.ConnectServer(conf, msg, PrepareReplyOnceCall, HandleReplyCallback);
        TotalSyllableCount = emoteExpression.Length;
        OnCompletionCallback?.Invoke();

        return emoteExpression;
    }

    public async Task Warmup(EmptyCallback completionCallback = null)
    {
        Debug.Log("[Remote] 感情推定LLMがWarmupされました");
        await Task.Yield();
        completionCallback();
    }
}