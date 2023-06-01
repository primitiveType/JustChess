using System;
using System.Collections.Generic;
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
    public override async void _Ready()
    {
        // SynchronizationContext.SetSynchronizationContext(new Shaman.Runtime.SingleThreadSynchronizationContext());
        ;
        Print("Creating chess game.");

        // await StartGame(TokenSource.Token, @"E:\Unity Projects\GodotChess\5_21_2023 12_59_25 PM.jcr", true);
        await StartGame(TokenSource.Token);

        // make the moves necessary to create a mate

        // var resultingMoves = position.GenerateMoves();
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        Dispose();
        WhitePlayer?.Dispose();
        BlackPlayer?.Dispose();
        
        string recordStr = JsonConvert.SerializeObject(GameRecord);
        string path = DateTime.Now.ToString().Replace(":", "_").Replace("/", "_") + ".jcr";
        File.WriteAllText(path, recordStr);
        Print($"Game record saved to {path}.");
        
    }

    private void Print(string message)
    {
        GD.Print($"{GetThread()} {message}");
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
        Print("Starting game.");
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
            await PlayMovesInRecord();
        }

        // return;
        // WhitePlayer = new Player(Game);
        WhitePlayer = new AiPlayer();
        BlackPlayer = new AiPlayer();
        _moves = 0;

        //detect legal moves by generating full list.
        while (!GameOver() && !token.IsCancellationRequested)
        {
            await PlayerMove(WhitePlayer);
            await AnimationQueue.WaitForAnimationsToComplete();
            if (GameOver() || token.IsCancellationRequested)
            {
                break;
            }

            await PlayerMove(BlackPlayer);
            await AnimationQueue.WaitForAnimationsToComplete();
        }


        token.ThrowIfCancellationRequested();
        Print($"Checkmate! in {_moves}.");
        await AnimationQueue.WaitForAnimationsToComplete();

        
        if (Game.Pos.IsDraw(0))
        {
            GameEndUi.SetEndGameState(ChessEndState.Stalemate, ChessEndStateVictor.Draw);
        }
        else
        {
            GameEndUi.SetEndGameState(ChessEndState.Checkmate, Game.Pos.SideToMove == Rudzoft.ChessLib.Types.Player.Black ? ChessEndStateVictor.White : ChessEndStateVictor.Black);
        }

        GameEndUi.Visible = true;

        GetTree().ChangeSceneToFile("res://chess_game_scene.tscn");
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
        ScriptedPlayer script = new ScriptedPlayer(GameRecord);
        _moves = 0;

        //detect winner based on last move
        //detect legal moves by generating full list.
        bool whiteWins = false;
        for (int i = 0; i < GameRecord.Moves.Count; i++)
        {
            await PlayerMove(script, false);
            await AnimationQueue.WaitForAnimationsToComplete();
        }
        
        script.Dispose();
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
        Move playerMove = await player.MakeMove(Game);
        if (record)
        {
            GameRecord.Moves.Add(playerMove);
        }

        Print($"${GetThread()}White Player making move {playerMove}.");

        Game.Pos.MakeMove(playerMove, Game.Pos.State);
        Print($"Setting engine position to {Game.Pos.FenNotation}.");
        _moves++;
    }

    private string GetThread()
    {
        return $"[{System.Threading.Thread.CurrentThread.ManagedThreadId}]";
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


    public void CreatePiece(PieceAddedEventArgs args)
    {
        Print($"Piece added on thread {Thread.CurrentThread.ManagedThreadId}.");
        PackedScene piecePrefab = (PackedScene)ResourceLoader.Load(PiecePrefab);
        ChessPiece piece = piecePrefab.Instantiate<ChessPiece>();
        piece.Initialize(this, args.Square, args.NewPiece);
        AddChild(piece);
    }
}
