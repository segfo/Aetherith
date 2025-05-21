using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using UnityEngine;

public class MainThreadDispatcher : MonoBehaviour
{
    private static MainThreadDispatcher _instance;

    public static MainThreadDispatcher Instance
    {
        get
        {
            if (_instance == null)
            {
                var obj = new GameObject("MainThreadDispatcher");
                _instance = obj.AddComponent<MainThreadDispatcher>();
                DontDestroyOnLoad(obj);
            }
            return _instance;
        }
    }

    // 非同期タスクと同期アクション両方に対応
    private readonly ConcurrentQueue<Action> _actionQueue = new();
    private readonly ConcurrentQueue<Func<Task>> _taskQueue = new();

    /// <summary>
    /// Unityのメインスレッドで実行したい処理を登録します。
    /// </summary>
    public void Enqueue(Action action)
    {
        if (action == null) return;
        _actionQueue.Enqueue(action);
    }

    /// <summary>
    /// Unityのメインスレッドで非同期に実行したい処理を登録します。
    /// </summary>
    public void Enqueue(Func<Task> taskFunc)
    {
        if (taskFunc == null) return;
        _taskQueue.Enqueue(taskFunc);
    }

    private void Update()
    {
        // 同期処理の実行
        while (_actionQueue.TryDequeue(out var action))
        {
            try { action(); }
            catch (Exception ex) { Debug.LogException(ex); }
        }

        // 非同期処理の実行
        while (_taskQueue.TryDequeue(out var taskFunc))
        {
            try
            {
                // 非同期だけど忘れても問題ない設計
                _ = taskFunc();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }
}
