using System;
using Godot;
using Rudzoft.ChessLib.Types;

namespace JustChess;

public partial class Board3D : Board
{
    private const float sqrt3 = 1.732f;
    private const float gridCoordsToWorldCoords = .01f * 16;
    private const float worldCoordsToGridCoords = 1 / gridCoordsToWorldCoords;
    [Export] public Resource BlackSquare { get; private set; }
    [Export] public Resource WhiteSquare { get; private set; }
    
    private Vector3 Offset => new Vector3(-4, -4, -1);


    public override void _Ready()
    {
        base._Ready();
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                Vector2 cart = new(x, y);
                Resource square = (x + y) % 2 == 0 ? BlackSquare : WhiteSquare;
                PackedScene squarePrefab = (PackedScene)ResourceLoader.Load(square.ResourcePath);


                BoardSquare3d squareNode = squarePrefab.Instantiate<BoardSquare3d>();
                squareNode.SetPosition(Offset + new Vector3(x, y, 0));
                AddChild(squareNode);
            }
        }
    }

    public override Vector3 GetIsoMetricPositionFromSquare(Square square)
    {
        return Offset + new Vector3(square.File.AsInt(), square.Rank.AsInt(), .5f);
    }

    public override Square GetSquareFromWorldPosition(Vector2 position)
    {
        Vector3 relativePos = new Vector3(position.X, position.Y, 0) - Offset;
        return new Square((int)relativePos.X, (int)relativePos.Y);
    }
}
