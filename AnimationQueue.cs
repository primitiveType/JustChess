using System.Collections.Concurrent;
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