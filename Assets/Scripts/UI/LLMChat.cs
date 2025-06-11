using System.Threading.Tasks;
using LLMUnity;
using Newtonsoft.Json;
using UnityEngine;
using ProcessStartInfo = System.Diagnostics.ProcessStartInfo;
using Process = System.Diagnostics.Process;
using System.IO;
using System.Collections.Generic;
using System;
using Newtonsoft.Json.Linq;

interface ILLMChat
{
    Task<string> Chat(string msg);
    Task<string> Chat(string msg, EmptyCallback PrepareReplyOnceCall = null, Callback<string> HandleReplyCallback = null, EmptyCallback OnCompletionCallback = null, bool addToHistory = true);
    Task Warmup(EmptyCallback completionCallback = null);
}

public class LocalLlmCharacterChat : ILLMChat
{
    private readonly LLMCharacter character;
    private string receivedText = string.Empty;
    private EmptyCallback OnCompletionCallback = () => { Debug.Log("完了コールバックは呼ばれていませんが正しく終了しました。"); };
    private Callback<string> HandleReplyCallback = (_) => { Debug.Log("HandleReplyCallbackは呼ばれていませんが正しく終了しました。"); };
    private EmptyCallback PrepareReplyOnceCall = () => { Debug.Log("PrepareReplyOnceCallは呼ばれていませんが正しく終了しました。"); };
    private bool isFirstReply = true;
    public LocalLlmCharacterChat(LLMCharacter character)
    {
        this.character = character;
    }

    public Task<string> Chat(string msg)
    {
        receivedText = msg;
        return character.Chat(StringDiff.GetDiff(msg, receivedText));
    }

    public Task<string> Chat(string msg,EmptyCallback PrepareReplyOnceCall = null, Callback<string> HandleReplyCallback = null, EmptyCallback OnCompletionCallback = null, bool addToHistory = true)
    {
        this.PrepareReplyOnceCall = PrepareReplyOnceCall;
        this.HandleReplyCallback = HandleReplyCallback;
        this.OnCompletionCallback = OnCompletionCallback;

        return character.Chat(msg, HandleReply, Complete, addToHistory);
    }
    private void HandleReply(string msg)
    {
        if (isFirstReply) { 
            PrepareReplyOnceCall.Invoke();
            isFirstReply = false;
        }
        string recvDiff = StringDiff.GetDiff(msg, receivedText);
        receivedText = msg;
        this.OnCompletionCallback = OnCompletionCallback ?? this.OnCompletionCallback;
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
        bool isFirstReply = true;
        string tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, msg);
        string fullMessage = string.Empty;
        try
        {
            await Task.Run(async () =>
            {
                var psi = new ProcessStartInfo
                {
                    FileName = Path.Combine(Application.streamingAssetsPath, "bin", "DifyConnector.exe"),
                    Arguments = $"\"{conf.difyApiKey}\" \"{conf.difyApiUrl}\" \"{tempFile}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                var process = new Process();
                process.StartInfo = psi;

                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        try
                        {
                            var decode = JObject.Parse(e.Data);
                            Debug.Log(decode);
                            if (decode["event"] != null && decode["event"].ToString() == "message")
                            {
                                mainThreadDispatcher.Enqueue(() =>
                                {
                                    if (isFirstReply)
                                    {
                                        isFirstReply = false;
                                        PrepareReplyOnceCall?.Invoke();
                                    }
                                    HandleReplyCallback?.Invoke(decode["answer"].ToString());
                                    fullMessage += decode["answer"].ToString();
                                });
                            }
                        }
                        catch (Exception jsonEx)
                        {
                            Debug.LogError("JSONデコードエラー: " + jsonEx.Message);
                        }
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Debug.LogError("[Python stderr] " + e.Data);
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await Task.Run(() => process.WaitForExit());
            });
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Failed to run Python script: " + ex.Message);
            return "エラーが発生しました＞＜";
        }
        finally
        {
            File.Delete(tempFile);
        }
        return fullMessage;
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
        return await Chat(msg, () => { }, (s) => { }, () => { });
    }

    public async Task<string> Chat(
        string msg,
        EmptyCallback PrepareReplyOnceCall = null,
        Callback<string> HandleReplyCallback = null,
        EmptyCallback OnCompletionCallback = null,
        bool addToHistory = true)
    {
        var conf = AppConfigManager.Instance.Config.characterLlm;
        string msgLlm = await DifyClient.ConnectServer(conf, msg, PrepareReplyOnceCall, HandleReplyCallback);
        OnCompletionCallback?.Invoke();

        return msgLlm;
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

    public Task<string> Chat(string msg, EmptyCallback PrepareReplyOnceCall = null, Callback<string> HandleReplyCallback = null, EmptyCallback OnCompletionCallback = null, bool addToHistory = true)
    {
        return character.Chat(msg, HandleReplyCallback, OnCompletionCallback, addToHistory);
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
        return await Chat(msg, () => { }, (s) => { }, () => { });
    }

    public async Task<string> Chat(string msg, EmptyCallback PrepareReplyOnceCall = null, Callback<string> HandleReplyCallback = null, EmptyCallback OnCompletionCallback = null, bool addToHistory = true)
    {
        var conf = AppConfigManager.Instance.Config.emotionLlm;
        string emoteExpression = await DifyClient.ConnectServer(conf, msg, PrepareReplyOnceCall, HandleReplyCallback);
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