using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Helper class to execute actions on Unity's main thread.
/// Required because WebSocket callbacks happen on background threads.
/// </summary>
public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static UnityMainThreadDispatcher _instance;
    private readonly Queue<Action> _executionQueue = new Queue<Action>();
    private readonly object _lock = new object();

    public static UnityMainThreadDispatcher Instance()
    {
        if (_instance == null)
        {
            var go = new GameObject("UnityMainThreadDispatcher");
            _instance = go.AddComponent<UnityMainThreadDispatcher>();
            DontDestroyOnLoad(go);
        }
        return _instance;
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Enqueue an action to be executed on the main thread.
    /// </summary>
    public void Enqueue(Action action)
    {
        if (action == null)
            return;

        lock (_lock)
        {
            _executionQueue.Enqueue(action);
        }
    }

    /// <summary>
    /// Enqueue an async action to be executed on the main thread and await its completion.
    /// </summary>
    public Task EnqueueAsync(Func<Task> asyncAction)
    {
        var tcs = new TaskCompletionSource<bool>();

        Enqueue(async () =>
        {
            try
            {
                await asyncAction();
                tcs.SetResult(true);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        return tcs.Task;
    }

    /// <summary>
    /// Enqueue a function to be executed on the main thread and return its result.
    /// </summary>
    public Task<T> EnqueueAsync<T>(Func<T> function)
    {
        var tcs = new TaskCompletionSource<T>();

        Enqueue(() =>
        {
            try
            {
                T result = function();
                tcs.SetResult(result);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        });

        return tcs.Task;
    }

    private void Update()
    {
        // Execute all queued actions on the main thread
        lock (_lock)
        {
            while (_executionQueue.Count > 0)
            {
                try
                {
                    _executionQueue.Dequeue()?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error executing queued action: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}

