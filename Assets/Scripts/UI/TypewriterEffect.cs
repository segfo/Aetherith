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
    public float typingInterval = 0.025f;

    private ITextWriterTarget writerTarget;

    private readonly Queue<(string text, bool clearBeforeTyping,TaskCompletionSource<bool>)> typingQueue = new();
    private CancellationTokenSource cancellationTokenSource;
    private bool isTyping = false;
    [SerializeField] private LipSyncSimulator lipSync;
    private TaskCompletionSource<bool> lipSyncCompletion = new TaskCompletionSource<bool>();

    public void Init(ITextWriterTarget writer)
    {
        if (writerTarget != null)
        {
            return;
        }
        writerTarget = writer;
    }

    public void SetTypingInterval(float interval)
    {
        typingInterval = interval;
    }


    /// <summary>
    /// キューに追加して順番にタイプライティングを行う（テキストクリアあり）
    /// </summary>
    public Task StartTyping(string text)
    {
        var tcs = new TaskCompletionSource<bool>();
        lock (typingQueue)
        {
            typingQueue.Enqueue((text, true, tcs));
        }
        if (isTyping == false)
        {
            TryStartNextTyping();
        }
        return tcs.Task;
    }

    /// <summary>
    /// キューに追加して順番にタイプライティングを行う（テキスト追記）
    /// </summary>
    public Task StartTypingAppend(string text)
    {
        var tcs = new TaskCompletionSource<bool>();
        lock (typingQueue)
        {
            typingQueue.Enqueue((text, false, tcs));
        }
        if (isTyping == false)
        {
            TryStartNextTyping();
        }
        return tcs.Task;
    }

    /// <summary>
    /// 現在タイプライティング中でなければ次の処理を開始
    /// </summary>
    private void TryStartNextTyping()
    {
        (string text, bool clearBeforeTyping, TaskCompletionSource<bool> tcs) nextTask;
        lock (typingQueue)
        {
            if (typingQueue.Count == 0){
                return;
            }
            nextTask = typingQueue.Dequeue();
        }
        _ = StartTypingInternalAsync(nextTask.text, nextTask.clearBeforeTyping,nextTask.tcs);
    }

    /// <summary>
    /// 内部のタイプライティング処理。キャンセル可能。
    /// </summary>
    private async Task StartTypingInternalAsync(string text, bool clearBeforeTyping, TaskCompletionSource<bool> tcs)
    {
        isTyping = true;
        if (writerTarget == null)
        {
            Debug.LogWarning("WriterTarget is null.");
            isTyping = false;
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
                // リップシンク無効もしくはSystemの場合は文章と口の動きを同期しない
                lipSync?.SpeakText(c.ToString());
                await Task.Delay(TimeSpan.FromSeconds(typingInterval), token);
            }
        }
        catch (OperationCanceledException)
        {
            Debug.Log("TypewriterEffectAsync: タイピングがキャンセルされました。");
        }
        catch (Exception e) {
            Debug.LogError($"{e}");
        }
        finally
        {
            cancellationTokenSource.Dispose();
            cancellationTokenSource = null;
            isTyping = false;
            if (typingQueue.Count==0) { tcs.SetResult(true); }
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
