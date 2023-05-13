using System.Threading.Tasks;
using Rudzoft.ChessLib;
using Rudzoft.ChessLib.Types;

public interface IChessPlayer
{
    Task<Move> MakeMove(IGame game);
    bool ReceiveMoveFromHumanPlayer(Move move);

    bool HumanPlayerCanMove { get; }
}