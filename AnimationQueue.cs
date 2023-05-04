using Godot;

public partial class AnimationQueue : Node
{
    private Tween TopTween { get; set; }
    private Tween CurrentRunningTween { get; set; }

    public override void _Ready()
    {
        base._Ready();
    }

    public void Add(Tween nextTween)
    {
        if (CurrentRunningTween == null || !CurrentRunningTween.IsRunning())
        {
            CurrentRunningTween = nextTween;
            TopTween = CurrentRunningTween;
            CurrentRunningTween.Play();
            return;
        }

        var runningTween = CurrentRunningTween;

        TopTween.Finished += () =>
        {
            CurrentRunningTween = nextTween;
            nextTween.Play();
        };

        TopTween = nextTween;
    }


    public override void _Process(double delta)
    {
        base._Process(delta);
    }
}
