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

    [Export] private Resource WhitePawnImage { get; set; }
    [Export] private Resource WhiteKnightImage { get; set; }
    [Export] private Resource WhiteBishopImage { get; set; }
    [Export] private Resource WhiteRookImage { get; set; }
    [Export] private Resource WhiteQueenImage { get; set; }
    [Export] private Resource WhiteKingImage { get; set; }
    [Export] private Resource BlackPawnImage { get; set; }
    [Export] private Resource BlackKnightImage { get; set; }
    [Export] private Resource BlackBishopImage { get; set; }
    [Export] private Resource BlackRookImage { get; set; }
    [Export] private Resource BlackQueenImage { get; set; }
    [Export] private Resource BlackKingImage { get; set; }

    private float TweenInterval { get; } = .1f;

    private IChessPlayer Player => _piece.IsBlack ? Game.BlackPlayer : Game.WhitePlayer;
    
    private ChessGame Game { get; set; }
    // private Tween Tween { get; set; }

    public override void _Ready()
    {
        base._Ready();
        Picker.DragRelease += PickerOnDragRelease;
        Picker.Drag += PickerOnDrag;
    }

    private void PickerOnDrag(object sender, DragEventArgs args)
    {
        if (!Player.HumanPlayerCanMove)
        {
            return;
        }
        Vector2 mousePosition = GetViewport().GetMousePosition();
        Vector3 worldPosition = args.Camera3D.ProjectPosition(mousePosition, 0);
        worldPosition.Z = 0;
        Position = worldPosition;
    }

    private void PickerOnDragRelease(object sender, DragReleaseEventArgs args)
    {
        if (!Player.HumanPlayerCanMove)
        {
            return;
        }
        GD.Print("Drag release!");
        Vector2 mousePosition = GetViewport().GetMousePosition();
        Vector3 worldPosition = args.Camera3D.ProjectPosition(mousePosition, Position.Z);
        Square moveSquare = Game.Board.GetSquareFromWorldPosition(new Vector2(worldPosition.X, worldPosition.Y));
        Move move = new Move(currentSquare, moveSquare);

        if (Player.ReceiveMoveFromHumanPlayer(move))
        {
            SetSquare(currentSquare);

        }
    }

    public void Initialize(ChessGame game, Square square)
    {
        Game = game;
        _piece = game.Game.Pos.GetPiece(square);
        // if (_piece.IsBlack)
        // {
        //     Sprite3D.Modulate = new Color(.25f, .25f, .25f, 1);
        // }
        // else
        // {
        //     Sprite3D.Modulate = new Color(5, 5, 5, 5);
        // }

        switch (_piece.Type())
        {
            case PieceTypes.NoPieceType:
                break;
            case PieceTypes.Pawn:
                Sprite3D.Texture = (Texture2D)(_piece.IsBlack ? BlackPawnImage : WhitePawnImage);
                break;
            case PieceTypes.Knight:
                Sprite3D.Texture = (Texture2D)(_piece.IsBlack ? BlackKnightImage : WhiteKnightImage);
                break;
            case PieceTypes.Bishop:
                Sprite3D.Texture = (Texture2D)(_piece.IsBlack ? BlackBishopImage : WhiteBishopImage);
                break;
            case PieceTypes.Rook:
                Sprite3D.Texture = (Texture2D)(_piece.IsBlack ? BlackRookImage : WhiteRookImage);
                break;
            case PieceTypes.Queen:
                Sprite3D.Texture = (Texture2D)(_piece.IsBlack ? BlackQueenImage : WhiteQueenImage);
                break;
            case PieceTypes.King:
                Sprite3D.Texture = (Texture2D)(_piece.IsBlack ? BlackKingImage : WhiteKingImage);
                break;
            case PieceTypes.PieceTypeNb:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        Sprite3D.Offset = new Vector2(0, Sprite3D.Texture.GetSize().Y /2f);


        SetSquare(square);
        GD.Print($"created piece {_piece.Value} at square {square.Value} and position {Sprite3D.Position}");

        game.Game.Pos.PieceMoved += PosOnPieceMoved;
        game.Game.Pos.PieceRemoved += PosOnPieceRemoved;
    }

    private void PosOnPieceRemoved(object sender, PieceRemovedEventArgs args)
    {
        if (args.EmptiedSquare == currentSquare && args.RemovedPiece == _piece)
        {
            currentSquare = Square.None;

            Tween tween = CreateTween();
            tween.Stop();
            tween.TweenInterval(TweenInterval);
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
        Transform = new Transform3D(Basis, Game.Board.GetIsoMetricPositionFromSquare(square));
        currentSquare = square;
    }

    private void SetSquareDeferred(Square square)
    {
        currentSquare = square;
        Vector3 destination = Game.Board.GetIsoMetricPositionFromSquare(square);
        Tween tween = CreateTween();
        tween.Stop();
        tween.TweenProperty(this, "position", destination, TweenInterval);
        Game.AnimationQueue.Add(tween);
    }

    private void PosOnPieceMoved(object sender, PieceMovedEventArgs args)
    {
        if (args.From == currentSquare)
        {
            SetSquareDeferred(args.To);
        }
        
    }

    
    
    public override void _ExitTree()
    {
        base._ExitTree();
        
        Game.Game.Pos.PieceMoved -= PosOnPieceMoved;
        Game.Game.Pos.PieceRemoved -= PosOnPieceRemoved;
    }
}
