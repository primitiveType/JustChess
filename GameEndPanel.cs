using Godot;
using System;
using Rudzoft.ChessLib;

public partial class GameEndPanel : Panel
{
    [Export] private Button ReturnButton { get; set; }
    [Export] private RichTextLabel EndStateLabel { get; set; }

    [Export] private Resource MainScene { get; set; }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        ReturnButton.Pressed += ReturnButtonOnPressed;
    }

    private void ReturnButtonOnPressed()
    {
        GetTree().ChangeSceneToFile(MainScene.ResourcePath);
    }

    public void SetEndGameState(ChessEndState state, ChessEndStateVictor victor)
    {
        if(victor != ChessEndStateVictor.Draw)
        {
            EndStateLabel.Text = $"{victor} wins by {state}!";
        }
        else
        {
            EndStateLabel.Text = $"Draw by {state}...";
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) { }
}


public enum ChessEndStateVictor
{
    White,
    Black,
    Draw
}

public enum ChessEndState
{
    Checkmate,
    Resignation,
    Timeout,
    Stalemate,
    InsufficientMaterial
}
