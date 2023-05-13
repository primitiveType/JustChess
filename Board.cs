using System;
using Godot;
using Rudzoft.ChessLib.Types;

namespace JustChess;

public partial class Board : Node3D
{
    [Export] public Node3D BottomLeftposition { get; private set; }

    private const float sqrt3 = 1.732f;
    private const float gridCoordsToWorldCoords = .01f * 16;
    private const float worldCoordsToGridCoords = 1 / gridCoordsToWorldCoords;

    public override void _Ready()
    {
        base._Ready();
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                var cart = new Vector2(x, y);
                var iso = CartesianToIsometric(cart);
                var cart2 = IsometricToCartesian(iso);

                if (cart != cart2)
                {
                    throw new ApplicationException($"Conversion failed for {x},{y}! {cart} vs {cart2}");
                }
            }
        }
    }

    public Vector3 GetIsoMetricPositionFromSquare(Square square)
    {
        Vector2 cart = new(square.File.AsInt(), square.Rank.AsInt());


        // Vector2 iso = new(cart.X + cart.Y, (cart.Y - cart.X) / 2f);
        Vector2 iso = CartesianToIsometric(cart);
        Vector2 scaled = iso * gridCoordsToWorldCoords;
        Vector3 final = new Vector3(BottomLeftposition.Position.X + scaled.X, BottomLeftposition.Position.Y + scaled.Y,
            8 + BottomLeftposition.Position.Z - scaled.Y);
        return final;
    }

    public Square GetSquareFromWorldPosition(Vector2 position)
    {
        Vector2 cartesianPosition = IsometricToCartesian(position - new Vector2(BottomLeftposition.Position.X, BottomLeftposition.Position.Y));
        var gridPosition = cartesianPosition * worldCoordsToGridCoords;
        gridPosition = new Vector2(Math.Clamp(gridPosition.X, 0, 7), Math.Clamp(gridPosition.Y, 0, 7));
        // Vector2 cartesian = new((cartesianPosition.X - cartesianPosition.Y) / 1.5f, cartesianPosition.X / 3f + cartesianPosition.Y / 1.5f);
        // cartesian = new Vector2(Math.Clamp(cartesian.X, 0, 7), Math.Clamp(cartesian.Y, 0, 7));
        int index = (int)Math.Floor((gridPosition.Y * 8) + gridPosition.X) - 1;
        return index;
    }

    // private Vector2 IsometricToCartesian(Vector2 iso)
    // {
    //     float isoX = iso.X;
    //     float isoY = iso.Y;
    //     float carX = (isoX - isoY) / 1.5f;
    //     float carY = (isoX / 3.0f) + (isoY / 1.5f);
    //     return new Vector2(carX, carY);
    // }
    //
    // private Vector2 CartesianToIsometric(Vector2 cart)
    // {
    //     return new(cart.X + cart.Y, (cart.Y - cart.X) / 2f);
    // }

    private Vector2 IsometricToCartesian(Vector2 iso)
    {
        //Isometric to Cartesian:

        // x_Cart = ( 2 * y_Iso + x_Iso ) / 2;
        // y_Cart = ( 2 * y_Iso – x_Iso ) / 2;
        // return new Vector2(((2 * iso.Y) + iso.X) / 2f, ((2 * iso.Y - iso.X) / 2f));

        var x = (iso.X - (2 * iso.Y)) / 2f;
        var y = (2 * iso.Y) + x;

        return new Vector2(x, y);
    }

    private Vector2 CartesianToIsometric(Vector2 cart)
    {
        return new Vector2(cart.X + cart.Y, (cart.Y - cart.X) / 2f);
    }
}
