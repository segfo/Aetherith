using System;
using System.Threading.Tasks;
using LLMUnity;
using UnityEngine;

interface ILLMChat
{
    Task<string> Chat(string msg);
    Task<string> Chat(string msg, Callback<string> HandleReply = null, EmptyCallback OnCompletionCallback = null, bool addToHistory = true);
    Task Warmup(EmptyCallback completionCallback = null);
}

public class LocalLlmCharacterChat : ILLMChat
{
    private readonly LLMCharacter character;

    public LocalLlmCharacterChat(LLMCharacter character)
    {
        this.character = character;
    }

    public Task<string> Chat(string msg)
    {
        return character.Chat(msg);
    }

    public Task<string> Chat(string msg, Callback<string> HandleReply = null, EmptyCallback OnCompletionCallback = null, bool addToHistory = true)
    {
        return character.Chat(msg, HandleReply, OnCompletionCallback, addToHistory);
    }

    public Task Warmup(EmptyCallback completionCallback = null)
    {
        Debug.Log("[Local] メインキャラクターLLMがWarmupされました");
        return character.Warmup(completionCallback);
    }
}

public class RemoteDifyCharacterChat : ILLMChat
{
    private readonly string url;
    private readonly string api_key;
    public RemoteDifyCharacterChat(string url, string api_key)
    {
        this.url = url;
        this.api_key = api_key;
    }

    public async Task<string> Chat(string msg)
    {
        return await Task.Run(() => {
            return "まだ処理が実装されてないです！＞＜"; });
    }

    public async Task<string> Chat(string msg, Callback<string> HandleReply = null, EmptyCallback OnCompletionCallback = null, bool addToHistory = true)
    {
        string msgLlm = "まだ処理が実装されてないです！＞＜";
        for (int i = 0; i <= msgLlm.Length; i++)
        {
            HandleReply?.Invoke(msgLlm.Substring(0,i));
            await Task.Delay((int)(AppConfigManager.Instance.Config.chatTypingInterval*1000)); // 1文字ずつ遅延を入れる
        }
        OnCompletionCallback();
        return "まだ処理が実装されてないです！＞＜";
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

    public LocalLlmEmotionChat(LLMCharacter character)
    {
        this.character = character;
    }

    public Task<string> Chat(string msg)
    {
        return character.Chat(msg);
    }

    public Task<string> Chat(string msg, Callback<string> HandleReply = null, EmptyCallback OnCompletionCallback = null, bool addToHistory = true)
    {
        return character.Chat(msg, HandleReply, OnCompletionCallback, addToHistory);
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
    public RemoteDifyLlmEmotionChat(string url,string api_key)
    {
        this.url = url;
        this.api_key = api_key;
    }

    public async Task<string> Chat(string msg)
    {
        var s = "{\"Happy\": 0.0, \"Sad\": 1.0, \"Angry\": 0,\"Neutral\": 0,\"Surprised\": 0,\"Relaxed\": 0.4}";
        return await Task.Run(() => { return s; });
    }

    public async Task<string> Chat(string msg, Callback<string> HandleReply = null, EmptyCallback OnCompletionCallback = null, bool addToHistory = true)
    {
        var s = "{\"Happy\": 0, \"Sad\": 1.0, \"Angry\": 0,\"Neutral\": 0,\"Surprised\": 0,\"Relaxed\": 0.4}";
        await Task.Yield();
        HandleReply?.Invoke(s);
        OnCompletionCallback();
        return s;
    }

    public async Task Warmup(EmptyCallback completionCallback = null)
    {
        Debug.Log("[Remote] 感情推定LLMがWarmupされました");
        await Task.Yield();
        completionCallback();
    }
}