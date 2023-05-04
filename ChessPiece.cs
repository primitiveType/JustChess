using System;
using Godot;
using Rudzoft.ChessLib;
using Rudzoft.ChessLib.Types;

public partial class ChessPiece : Node3D
{
    private Vector2 _dragStartPosition;
    private Piece _piece;
    [Export] private Sprite3D Sprite3D { get; set; }
    private Square currentSquare { get; set; }
    [Export] private ClickAndDrag Picker { get; set; }

    private ChessGame Game { get; set; }
    // private Tween Tween { get; set; }

    public override void _Ready()
    {
        ;
        base._Ready();
        Picker.DragRelease += PickerOnDragRelease;
        Picker.Drag += PickerOnDrag;
    }

    private void PickerOnDrag(object sender, DragEventArgs args)
    {
        Vector2 mousePosition = GetViewport().GetMousePosition();
        Vector3 worldPosition = args.Camera3D.ProjectPosition(mousePosition, 0);
        worldPosition.Z = 0;
        Position = worldPosition;
    }

    private void PickerOnDragRelease(object sender, DragReleaseEventArgs args)
    {
        GD.Print("Drag release!");
        Vector2 mousePosition = GetViewport().GetMousePosition();
        Vector3 worldPosition = args.Camera3D.ProjectPosition(mousePosition, Position.Z);
        Square moveSquare = GetPositionSquare(worldPosition);
        var move = new Move(currentSquare, moveSquare);
        if (Game.Game.Pos.IsLegal(move))
        {
            Game.Game.Pos.TakeMove(move);
        }
        else
        {
            SetSquare(currentSquare);
        }
    }

    public void Initialize(ChessGame game, Square square)
    {
        Game = game;
        _piece = game.Game.Pos.GetPiece(square);
        if (_piece.IsBlack)
        {
            Sprite3D.Modulate = new Color(.25f, .25f, .25f, 1);
        }
        else
        {
            Sprite3D.Modulate = new Color(5, 5, 5, 5);
        }

        switch (_piece.Type())
        {
            case PieceTypes.NoPieceType:
                break;
            case PieceTypes.Pawn:
                Sprite3D.Scale = new Vector3(.25f, .25f, .25f);
                break;
            case PieceTypes.Knight:
                Sprite3D.Scale = new Vector3(.35f, .35f, .35f);
                break;
            case PieceTypes.Bishop:
                Sprite3D.Scale = new Vector3(.25f, .65f, .25f);
                break;
            case PieceTypes.Rook:
                Sprite3D.Scale = new Vector3(.35f, .85f, .35f);
                break;
            case PieceTypes.Queen:
                Sprite3D.Scale = new Vector3(.5f, 1f, .5f);
                break;
            case PieceTypes.King:
                Sprite3D.Scale = new Vector3(1, 1, 1);
                break;
            case PieceTypes.PieceTypeNb:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }


        SetSquare(square);
        GD.Print($"created piece {_piece.Value} at square {square.Value} and position {Sprite3D.Position}");

        game.Game.Pos.PieceMoved += PosOnPieceMoved;
        game.Game.Pos.PieceRemoved += PosOnPieceRemoved;
    }

    private void PosOnPieceRemoved(object sender, PieceRemovedEventArgs args)
    {
        if (args.EmptiedSquare == currentSquare && args.RemovedPiece == _piece)
        {
            Tween tween = CreateTween();
            tween.Stop();
            tween.TweenInterval(0);
            tween.Connect("finished", new Callable(this, nameof(AnimateCapturePiece)));
            Game.AnimationQueue.Add(tween);
        }
    }

    private void AnimateCapturePiece()
    {
        QueueFree();
    }

    private void SetSquare(Square square)
    {
        Transform = new Transform3D(Basis, GetSquarePosition(square));
        currentSquare = square;
    }

    private void SetSquareDeferred(Square square)
    {
        currentSquare = square;
        Vector3 destination = GetSquarePosition(square);
        Tween tween = CreateTween();
        tween.Stop();
        tween.TweenProperty(this, "position", destination, 1f);
        Game.AnimationQueue.Add(tween);
    }

    private void PosOnPieceMoved(object sender, PieceMovedEventArgs args)
    {
        if (args.From == currentSquare)
        {
            SetSquareDeferred(args.To);
        }
    }

    private Vector3 GetSquarePosition(Square square)
    {
        int index = (int)square.Value;
        return new Vector3(index % 8, index / 8, 0) - new Vector3(4, 4, 0);
    }

    private Square GetPositionSquare(Vector3 position)
    {
        Vector3 adjustedPos = position + new Vector3(4, 4, 0);
        int x = (int)Math.Clamp(adjustedPos.X, 0, 8);
        int y = (int)Math.Clamp(adjustedPos.Y, 0, 8);

        int index = (y * 8) + x;
        return index;
    }
}
