using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public interface ITextWriterTarget
{
    void SetText(string text);
    void AppendText(string text);
}

public class TypewriterEffect : MonoBehaviour
{
    public float characterInterval = 0.025f;

    public ITextWriterTarget writerTarget;

    private readonly Queue<(string text, bool clearBeforeTyping)> typingQueue = new();
    private CancellationTokenSource cancellationTokenSource;
    private bool isTyping = false;

    public void Init(ITextWriterTarget writer)
    {
        if (writerTarget != null)
        {
            return;
        }
        characterInterval = AppConfigManager.Instance.Config.chatTypingInterval;
        writerTarget = writer;
        // 設定ファイルが更新されたときの動作
        // 設定項目が増えたら関数化する
        AppConfigManager.Instance.OnConfigUpdated += (config) =>
        {
            characterInterval = config.chatTypingInterval;
        };
    }


    /// <summary>
    /// キューに追加して順番にタイプライティングを行う（テキストクリアあり）
    /// </summary>
    public void StartTyping(string text)
    {
        lock (typingQueue)
        {
            typingQueue.Enqueue((text, true));
        }
        TryStartNextTyping();
    }

    /// <summary>
    /// キューに追加して順番にタイプライティングを行う（テキスト追記）
    /// </summary>
    public void StartTypingAppend(string text)
    {
        lock (typingQueue)
        {
            typingQueue.Enqueue((text, false));
        }
        TryStartNextTyping();
    }

    /// <summary>
    /// 現在タイプライティング中でなければ次の処理を開始
    /// </summary>
    private void TryStartNextTyping()
    {
        if (isTyping){
            return;
        }
        (string text, bool clearBeforeTyping) nextTask;
        lock (typingQueue)
        {
            if (typingQueue.Count == 0){
                return;
            }

            nextTask = typingQueue.Dequeue();
        }

        _ = StartTypingInternalAsync(nextTask.text, nextTask.clearBeforeTyping);
    }

    /// <summary>
    /// 内部のタイプライティング処理。キャンセル可能。
    /// </summary>
    private async Task StartTypingInternalAsync(string text, bool clearBeforeTyping)
    {
        isTyping = true;

        if (writerTarget == null)
        {
            Debug.LogWarning("WriterTarget is null.");
            isTyping = false;
            TryStartNextTyping();
            return;
        }

        cancellationTokenSource = new CancellationTokenSource();
        var token = cancellationTokenSource.Token;

        try
        {
            if (clearBeforeTyping)
            {
                writerTarget.SetText(string.Empty);
            }

            foreach (char c in text)
            {
                token.ThrowIfCancellationRequested();
                writerTarget.AppendText(c.ToString());
                await Task.Delay(TimeSpan.FromSeconds(characterInterval), token);
            }
        }
        catch (OperationCanceledException)
        {
            Debug.Log("TypewriterEffectAsync: タイピングがキャンセルされました。");
        }
        finally
        {
            cancellationTokenSource.Dispose();
            cancellationTokenSource = null;
            isTyping = false;
            // 次があれば自動で開始
            TryStartNextTyping();
        }
    }

    /// <summary>
    /// 現在のタイピングをキャンセルし、キューもクリアする
    /// </summary>
    public void StopTyping()
    {
        lock (typingQueue)
        {
            typingQueue.Clear();
        }

        if (cancellationTokenSource != null)
        {
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
            cancellationTokenSource = null;
        }
    }
}
