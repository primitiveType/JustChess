using Godot;

public partial class MainMenu : Control
{
    [Export] private Button PlayButton { get; set; }
    [Export] private Button ExitButton { get; set; }
    [Export] private string ChessScene { get; set; }


    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        PlayButton.Pressed += PlayButtonOnPressed;
        ExitButton.Pressed += ExitButtonOnPressed;
    }

    private void ExitButtonOnPressed()
    {
        GD.Print("quitting.");
        GetTree().Root.PropagateNotification((int)NotificationWMCloseRequest);
        GetTree().Quit();
    }

    private void PlayButtonOnPressed()
    {
        GD.Print("Lets play chess!");
        GetTree().ChangeSceneToFile(ChessScene);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) { }
}
