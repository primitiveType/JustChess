using System;
using Rudzoft.ChessLib.Types;

static internal class ChessGameExtenions
{
    public static File GetFile(char c)
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

    public static Move GetMoveFromEngineString(string moveStr)
    {
        Square from = new(Rank.All[int.Parse(moveStr[1].ToString()) - 1], ChessGameExtenions.GetFile(moveStr[0]));
        Square to = new(Rank.All[int.Parse(moveStr[3].ToString()) - 1], ChessGameExtenions.GetFile(moveStr[2]));
        Move move;
        var promotionType = ChessGameExtenions.TryGetPromotionPieceType(moveStr);
        if (promotionType != PieceTypes.NoPieceType)
        {
            move = Move.Create(from, to, MoveTypes.Promotion, promotionType);
        }
        else
        {
            move = Move.Create(from, to);
        }

        return move;
    }
    public static PieceTypes TryGetPromotionPieceType(char c)
    {
        switch (c)
        {
            case 'q':
            case 'Q': return PieceTypes.Queen;
            case 'p':
            case 'P': return PieceTypes.Pawn;
            case 'r':
            case 'R': return PieceTypes.Rook;
            case 'b':
            case 'B': return PieceTypes.Bishop;
            case 'k':
            case 'K': return PieceTypes.King;
            case 'n':
            case 'N': return PieceTypes.Knight;
            default: return PieceTypes.NoPieceType;
        }
    }
    public static PieceTypes TryGetPromotionPieceType(string moveStr)
    {
        if (moveStr.Length <= 4)
        {
            return PieceTypes.NoPieceType;
        }

        var c = moveStr[4];
        switch (c)
        {
            case 'q':
            case 'Q': return PieceTypes.Queen;
            case 'p':
            case 'P': return PieceTypes.Pawn;
            case 'r':
            case 'R': return PieceTypes.Rook;
            case 'b':
            case 'B': return PieceTypes.Bishop;
            case 'k':
            case 'K': return PieceTypes.King;
            case 'n':
            case 'N': return PieceTypes.Knight;
            default: return PieceTypes.NoPieceType;
        }
    }
}
