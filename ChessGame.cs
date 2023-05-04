using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Rudzoft.ChessLib;
using Rudzoft.ChessLib.Factories;
using Rudzoft.ChessLib.Fen;
using Rudzoft.ChessLib.MoveGeneration;
using Rudzoft.ChessLib.Types;
using UciSharp;

public partial class ChessGame : Node
{
    public IGame Game { get; private set; }

    [Export] public string PiecePrefab { get; set; }
    [Export] private NodePath AnimationQueuePath { get; set; }
    public AnimationQueue AnimationQueue => GetNode<AnimationQueue>(AnimationQueuePath);
    private Task GameTask { get; set; }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        GD.Print("Creating chess game.");

        GameTask = Task.Run(StartGame);

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
        GameTask?.Dispose();
    }

    private async Task StartGame()
    { // construct game and start a new game
        // throw new NotImplementedException();
        Game = GameFactory.Create(Fen.StartPositionFen);
        Game.Pos.IsProbing = false;

        IPosition position = Game.Pos;
        State state = new();


        PackedScene piecePrefab = (PackedScene)ResourceLoader.Load(PiecePrefab);

        foreach (Square square in Game.Pos.Pieces())
        {
            ChessPiece piece = piecePrefab.Instantiate<ChessPiece>();
            piece.Initialize(this, square);
            AddChild(piece);
        }

        Player player = new Player();
        AiPlayer engine = new AiPlayer();
        await engine.Initialize();
        while (!Game.Pos.IsMate)
        {
            Move playerMove = await player.MakeMove(Game);
            GD.Print($"Player making move {playerMove}.");
            Game.Pos.MakeMove(playerMove, Game.Pos.State);

            GD.Print($"Setting engine position to {Game.Pos.FenNotation}.");
            await engine.Engine.SetPositionAsync($"{Game.Pos.FenNotation}");
            await engine.Engine.WaitForReadyAsync();
            
            Move engineMove = await engine.MakeMove(Game);
            GD.Print($"Engine making move {engineMove}.");
            Game.Pos.MakeMove(engineMove, Game.Pos.State);
        }
        
        GD.Print("Checkmate!");
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
        public async Task<Move> MakeMove(IGame game)
        {
            //let player make move... fake it for now.
            return game.Pos.GenerateMoves().First().Move;
        }
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

            string moveStr = await Engine.GoAsync();

            GD.Print($"Engine made move {moveStr}");
            Square from = new(Rank.All[int.Parse(moveStr[1].ToString()) - 1], GetFile(moveStr[0]));
            Square to = new(Rank.All[int.Parse(moveStr[3].ToString()) - 1], GetFile(moveStr[2]));

            Move move = Move.Create(from, to);
            GD.Print($"rudz move {move}");
            return move;
        }

        public async Task Initialize()
        {
            await Engine.StartAsync();
            await Engine.StartGameAsync();
            await Engine.WaitForReadyAsync();
            Initialized = true;
        }
    }
}

public interface IChessPlayer
{
    Task<Move> MakeMove(IGame game);
}
