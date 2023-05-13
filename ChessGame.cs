using System;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using Rudzoft.ChessLib;
using Rudzoft.ChessLib.Factories;
using Rudzoft.ChessLib.Fen;
using Rudzoft.ChessLib.MoveGeneration;
using Rudzoft.ChessLib.Types;
using UciSharp;
using Board = JustChess.Board;

public partial class ChessGame : Node
{
    private int _moves;
    public IGame Game { get; private set; }

    [Export] public string PiecePrefab { get; set; }
    [Export] private NodePath AnimationQueuePath { get; set; }
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
        while (!Game.Pos.IsMate && !token.IsCancellationRequested)
        {
            await PlayerMove(WhitePlayer);
            if (Game.Pos.IsMate || token.IsCancellationRequested)
            {
                break;
            }
            await PlayerMove(BlackPlayer);
        }

        token.ThrowIfCancellationRequested();
        GD.Print($"Checkmate! in {_moves}.");
    }

    private async Task BlackPlayerMove()
    {
        
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


    private static File GetFile(char c)
    {
        switch (c)
        {
            case 'a': return File.FileA;
            case 'b': return File.FileB;
            case 'c': return File.FileC;
            case 'd': return File.FileD;
            case 'e': return File.FileE;
            case 'f': return File.FileF;
            case 'g': return File.FileG;
            case 'h': return File.FileH;
            default: throw new ArgumentException($"file {c} invalid!");
        }
    }

    public class Player : IChessPlayer
    {
        public IGame Game { get; }

        public Player(IGame game)
        {
            Game = game;
        }

        private TaskCompletionSource<Move> MoveTask { get; set; }

        public async Task<Move> MakeMove(IGame game)
        {
            MoveTask = new TaskCompletionSource<Move>();
            return await MoveTask.Task;
        }

        public bool ReceiveMoveFromHumanPlayer(Move move)
        {
            if (MoveTask == null)
            {
                return false;
            }

            if (Game.Pos.IsLegal(move))
            {
                return MoveTask.TrySetResult(move);
            }

            return false;
        }

        public bool HumanPlayerCanMove => true;
    }

    public class DumbAi : IChessPlayer
    {
        public async Task<Move> MakeMove(IGame game)
        {
            //let player make move... fake it for now.
            return game.Pos.GenerateMoves().First().Move;
        }

        public bool ReceiveMoveFromHumanPlayer(Move move)
        {
            return true;
        }

        public bool HumanPlayerCanMove { get; } = false;
    }

    public class AiPlayer : IChessPlayer
    {
        public AiPlayer()
        {
            Engine = new ChessEngine("C:\\Users\\Arthu\\Downloads\\arasan23.5\\arasanx-64.exe");
        }

        public ChessEngine Engine { get; }
        private bool Initialized { get; set; }

        public async Task<Move> MakeMove(IGame game)
        {
            if (!Initialized)
            {
                await Initialize();
            }

            await Engine.SetPositionAsync($"{game.Pos.FenNotation}");
            await Engine.WaitForReadyAsync();

            string moveStr = await Engine.GoAsync();

            GD.Print($"Engine made move {moveStr}");
            Square from = new(Rank.All[int.Parse(moveStr[1].ToString()) - 1], GetFile(moveStr[0]));
            Square to = new(Rank.All[int.Parse(moveStr[3].ToString()) - 1], GetFile(moveStr[2]));

            Move move = Move.Create(from, to);
            GD.Print($"rudz move {move}");
            return move;
        }

        public bool ReceiveMoveFromHumanPlayer(Move move)
        {
            return true;
        }

        public bool HumanPlayerCanMove { get; } = false;

        public async Task Initialize()
        {
            await Engine.StartAsync();
            await Engine.StartGameAsync();
            await Engine.WaitForReadyAsync();
            Initialized = true;
        }
    }
}
