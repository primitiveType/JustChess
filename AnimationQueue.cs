using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Godot;

public partial class AnimationQueue : Node
{
    private ConcurrentQueue<Tween> Queue { get; } = new();
    private Tween CurrentTween { get; set; }

    public void Add(Tween nextTween)
    {
        AnimationsCompleteTask.Reset();
        nextTween.Finished += PlayQueuedTween;

        if (CurrentTween == null)
        {
            GD.Print($"No tweens playing. Starting new tween.");
            CurrentTween = nextTween;
            CurrentTween.Play();
        }
        else
        {
            GD.Print("Tweens playing. queueing tween.");
            Queue.Enqueue(nextTween);
        }
    }

    private void PlayQueuedTween()
    {
        if (Queue.TryDequeue(out Tween tween))
        {
            GD.Print($"Starting next tween at {Time.GetTicksMsec()}.");
            CurrentTween = tween;
            tween.Play();
        }
        else
        {
            GD.Print("No tweens found.");

            CurrentTween = null;
            AnimationsCompleteTask.TrySetResult(true);
        }
    }

    private ReusableAwaiter<bool> AnimationsCompleteTask { get; } = new ReusableAwaiter<bool>();

    public async Task WaitForAnimationsToComplete()
    {
        await AnimationsCompleteTask;
    }
}

public sealed class ReusableAwaiter<T> : INotifyCompletion
{
    private Action _continuation = null;
    private T _result = default(T);
    private Exception _exception = null;

    public bool IsCompleted { get; private set; }

    public T GetResult()
    {
        if (_exception != null)
            throw _exception;
        return _result;
    }

    public void OnCompleted(Action continuation)
    {
        if (_continuation != null)
            throw new InvalidOperationException("This ReusableAwaiter instance has already been listened");
        _continuation = continuation;
    }

    /// <summary>
    /// Attempts to transition the completion state.
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    public bool TrySetResult(T result)
    {
        if (!this.IsCompleted)
        {
            this.IsCompleted = true;
            this._result = result;

            if (_continuation != null)
                _continuation();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to transition the exception state.
    /// </summary>
    /// <param name="result"></param>
    /// <returns></returns>
    public bool TrySetException(Exception exception)
    {
        if (!this.IsCompleted)
        {
            this.IsCompleted = true;
            this._exception = exception;

            if (_continuation != null)
                _continuation();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Reset the awaiter to initial status
    /// </summary>
    /// <returns></returns>
    public ReusableAwaiter<T> Reset()
    {
        this._result = default(T);
        this._continuation = null;
        this._exception = null;
        this.IsCompleted = false;
        return this;
    }

    public ReusableAwaiter<T> GetAwaiter()
    {
        return this;
    }
}
