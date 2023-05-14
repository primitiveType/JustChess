using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Rudzoft.ChessLib;
using Rudzoft.ChessLib.Enums;
using Rudzoft.ChessLib.Factories;
using Rudzoft.ChessLib.Fen;
using Rudzoft.ChessLib.MoveGeneration;
using Rudzoft.ChessLib.Types;
using Board = JustChess.Board;

public partial class ChessGame : Node
{
    private int _moves;
    public IGame Game { get; private set; }

    [Export] public string PiecePrefab { get; set; }
    [Export] private NodePath AnimationQueuePath { get; set; }
    [Export] private GameEndPanel GameEndUi { get; set; }
    public AnimationQueue AnimationQueue => GetNode<AnimationQueue>(AnimationQueuePath);
    private Task GameTask { get; set; }


    public IChessPlayer WhitePlayer { get; private set; }
    public IChessPlayer BlackPlayer { get; private set; }
    [Export] public Board Board { get; private set; }
    private CancellationTokenSource TokenSource { get; } = new();

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        GD.Print("Creating chess game.");

        GameTask = Task.Run(() => StartGame(TokenSource.Token));

        // make the moves necessary to create a mate

        // var resultingMoves = position.GenerateMoves();
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        Dispose();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        TokenSource.Cancel();

        // GameTask.Dispose();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (GameTask != null && GameTask.IsFaulted)
        {
            var exception = GameTask.Exception.InnerException;
            GameTask.Dispose();
            GameTask = null;
            GD.PrintErr(exception);
        }
    }

    private async Task StartGame(CancellationToken token)
    { // construct game and start a new game
        // throw new NotImplementedException();
        Game = GameFactory.Create(Fen.StartPositionFen);
        Game.Pos.IsProbing = false;
        Game.Pos.PieceAdded += PosOnPieceAdded;


        PackedScene piecePrefab = (PackedScene)ResourceLoader.Load(PiecePrefab);

        foreach (Square square in Game.Pos.Pieces())
        {
            ChessPiece piece = piecePrefab.Instantiate<ChessPiece>();
            piece.Initialize(this, square);
            AddChild(piece);
        }

        // WhitePlayer = new Player(Game);
        WhitePlayer = new AiPlayer();
        BlackPlayer = new AiPlayer();
        _moves = 0;

        //detect winner based on last move
        //detect legal moves by generating full list.
        while (!GameOver() && !token.IsCancellationRequested)
        {
            await PlayerMove(WhitePlayer);
            if (GameOver() || token.IsCancellationRequested)
            {
                break;
            }

            Game.Pos.GenerateMoves(MoveGenerationType.Quiets);
            await PlayerMove(BlackPlayer);
        }


        token.ThrowIfCancellationRequested();
        GD.Print($"Checkmate! in {_moves}.");
        await AnimationQueue.WaitForAnimationsToComplete();

        GameEndUi.SetEndGameState(ChessEndState.Checkmate, ChessEndStateVictor.Black);
        GameEndUi.Visible = true;
    }

    private bool GameOver()
    {
        return Game.Pos.IsDraw(50) || Game.Pos.IsMate;
    }

    private async Task PlayerMove(IChessPlayer player)
    {
        Move playerMove = await player.MakeMove(Game);
        GD.Print($"White Player making move {playerMove}.");

        Game.Pos.MakeMove(playerMove, Game.Pos.State);
        GD.Print($"Setting engine position to {Game.Pos.FenNotation}.");
        _moves++;
    }

    private void PosOnPieceAdded(object sender, PieceAddedEventArgs args)
    {
        PackedScene piecePrefab = (PackedScene)ResourceLoader.Load(PiecePrefab);
        ChessPiece piece = piecePrefab.Instantiate<ChessPiece>();
        piece.Initialize(this, args.Square);
        AddChild(piece);
    }
}
