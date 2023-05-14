﻿using System.Threading.Tasks;
using Godot;
using Rudzoft.ChessLib;
using Rudzoft.ChessLib.Types;
using UciSharp;

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
        var move = ChessGameExtenions.GetMoveFromEngineString(moveStr);
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
