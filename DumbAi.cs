using System.Linq;
using System.Threading.Tasks;
using Rudzoft.ChessLib;
using Rudzoft.ChessLib.MoveGeneration;
using Rudzoft.ChessLib.Types;

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