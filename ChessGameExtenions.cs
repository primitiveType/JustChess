using System;
using Rudzoft.ChessLib.Types;

static internal class ChessGameExtenions
{
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
}