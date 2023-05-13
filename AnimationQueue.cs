using System;
using System.Collections.Generic;
using Godot;

public partial class AnimationQueue : Node
{
    private Tween TopTween { get; set; }
    private Tween CurrentRunningTween { get; set; }
    private Object CurrentTweenLock { get; } = new Object();

    private Queue<Tween> Queue { get; } = new Queue<Tween>();

    public override void _Ready()
    {
        base._Ready();
    }

    public void Add(Tween nextTween)
    {
        
        GD.Print($"Tween added on thread {System.Environment.CurrentManagedThreadId}");
        lock (CurrentTweenLock)
        {
            if (CurrentRunningTween == null || !CurrentRunningTween.IsRunning())
            {
                CurrentRunningTween = nextTween;
                TopTween = CurrentRunningTween;
                CurrentRunningTween.Play();
                return;
            }
        }

        var runningTween = CurrentRunningTween;

        lock (CurrentTweenLock)
        {
            TopTween.Finished += () =>
            {
                lock (CurrentTweenLock)
                {
                    CurrentRunningTween = nextTween;
                }

                nextTween.Play();
            };

            TopTween = nextTween;
        }
    }


    public override void _Process(double delta)
    {
        base._Process(delta);
    }
}
