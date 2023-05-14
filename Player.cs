using System.Threading.Tasks;
using Rudzoft.ChessLib;
using Rudzoft.ChessLib.Types;

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