using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using MetaProperties.SynchronizationContexts;
using Newtonsoft.Json;
using Rudzoft.ChessLib;
using Rudzoft.ChessLib.Factories;
using Rudzoft.ChessLib.Fen;
using Rudzoft.ChessLib.Types;
using Board = JustChess.Board;
using File = System.IO.File;

public class GameRecord
{
    public string StartFen { get; set; }
    public string EndFen { get; set; }

    public List<Move> Moves { get; } = new();
}

public partial class ChessGame : Node
{
    private int _moves;
    public IGame Game { get; private set; }
    private GameRecord GameRecord { get; set; } = new();


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
        SynchronizationContext.SetSynchronizationContext(new SingleThreadSynchronizationContext() );
        ;
        GD.Print("Creating chess game.");

        GameTask = Task.Run(() => StartGame(TokenSource.Token, @"E:\Unity Projects\GodotChess\5_14_2023 1_56_06 PM.jcr", true));

        // make the moves necessary to create a mate

        // var resultingMoves = position.GenerateMoves();
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        Dispose();
        string recordStr = JsonConvert.SerializeObject(GameRecord);
        string path = DateTime.Now.ToString().Replace(":", "_").Replace("/", "_") + ".jcr";
        File.WriteAllText(path, recordStr);
        GD.Print($"Game record saved to {path}.");
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
            Exception exception = GameTask.Exception.InnerException;
            GameTask.Dispose();
            GameTask = null;
            GD.PrintErr(exception);
        }
    }

    private async Task StartGame(CancellationToken token, string loadPath = null, bool playOutMoves = false)
    { // construct game and start a new game

        if (loadPath == null)
        {
            Game = CreateDefaultGame();
        }
        else
        {
            Game = LoadGameFromPath(loadPath, !playOutMoves);
        }

        Game.Pos.IsProbing = false;
        Game.Pos.PieceAdded += PosOnPieceAdded;
        LoadPieces();

        if (playOutMoves) //animate moves that are already in the record.
        {
            await PlayMovesInRecord().ConfigureAwait(true);
        }

        // return;
        // WhitePlayer = new Player(Game);
        WhitePlayer = new AiPlayer();
        BlackPlayer = new AiPlayer();
        _moves = 0;

        //detect legal moves by generating full list.
        bool whiteWins = false;
        while (!GameOver() && !token.IsCancellationRequested)
        {
            await PlayerMove(WhitePlayer).ConfigureAwait(true);
            await AnimationQueue.WaitForAnimationsToComplete().ConfigureAwait(true);
            if (GameOver() || token.IsCancellationRequested)
            {
                //detect winner based on last move
                whiteWins = true;
                break;
            }

            await PlayerMove(BlackPlayer).ConfigureAwait(true);
            await AnimationQueue.WaitForAnimationsToComplete().ConfigureAwait(true);
        }


        token.ThrowIfCancellationRequested();
        GD.Print($"Checkmate! in {_moves}.");
        await AnimationQueue.WaitForAnimationsToComplete().ConfigureAwait(true);

        if (Game.Pos.IsDraw(0))
        {
            GameEndUi.SetEndGameState(ChessEndState.Stalemate, ChessEndStateVictor.Draw);
        }
        else
        {
            GameEndUi.SetEndGameState(ChessEndState.Checkmate, whiteWins ? ChessEndStateVictor.White : ChessEndStateVictor.Black);
        }

        GameEndUi.Visible = true;
    }

    private void LoadPieces()
    {
        PackedScene piecePrefab = (PackedScene)ResourceLoader.Load(PiecePrefab);

        foreach (Square square in Game.Pos.Pieces())
        {
            ChessPiece piece = piecePrefab.Instantiate<ChessPiece>();
            piece.Initialize(this, square, Game.Pos.GetPiece(square));
            AddChild(piece);
        }
    }

    private async Task PlayMovesInRecord()
    {
        // WhitePlayer = new Player(Game);
        var script = new ScriptedPlayer(GameRecord);
        _moves = 0;

        //detect winner based on last move
        //detect legal moves by generating full list.
        bool whiteWins = false;
        for (int i = 0; i < GameRecord.Moves.Count; i++)
        {
            await PlayerMove(script, false).ConfigureAwait(true);
            await AnimationQueue.WaitForAnimationsToComplete().ConfigureAwait(true);
        }
    }

    private IGame LoadGameFromPath(string loadPath, bool jumpToEnd)
    {
        string movesString = File.ReadAllText(loadPath);
        GameRecord = JsonConvert.DeserializeObject<GameRecord>(movesString);
        IGame game;
        if (jumpToEnd)
        {
            game = GameFactory.Create(GameRecord.EndFen);
        }
        else
        {
            game = GameFactory.Create(GameRecord.StartFen);
        }

        return game;
    }

    private IGame CreateDefaultGame(string fen = Fen.StartPositionFen)
    {
        IGame game = GameFactory.Create(fen);
        GameRecord.StartFen = fen;
        return game;
    }

    private bool GameOver()
    {
        return Game.Pos.IsDraw(0) || Game.Pos.IsMate;
    }

    private async Task PlayerMove(IChessPlayer player, bool record = true)
    {
        Move playerMove = await player.MakeMove(Game).ConfigureAwait(true);
        if (record)
        {
            GameRecord.Moves.Add(playerMove);
        }

        GD.Print($"White Player making move {playerMove}.");

        Game.Pos.MakeMove(playerMove, Game.Pos.State);
        GD.Print($"Setting engine position to {Game.Pos.FenNotation}.");
        _moves++;
    }

    private void PosOnPieceAdded(object sender, PieceAddedEventArgs args)
    {
        Tween tween = CreateTween();
        tween.Stop();
        tween.TweenInterval(ChessPiece.TweenInterval);
        Callable callable = Callable.From(() => { CreatePiece(args); });
        tween.Connect("finished", callable);
        AnimationQueue.Add(tween);
    }

    public void DoNothing() { }

    public void CreatePiece(PieceAddedEventArgs args)
    {
        GD.Print($"Piece added on thread {Thread.CurrentThread.ManagedThreadId}.");
        PackedScene piecePrefab = (PackedScene)ResourceLoader.Load(PiecePrefab);
        ChessPiece piece = piecePrefab.Instantiate<ChessPiece>();
        piece.Initialize(this, args.Square, args.NewPiece);
        AddChild(piece);
    }
}
