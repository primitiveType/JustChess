using Godot;

namespace JustChess;

public partial class BoardSquare3d : Node3D
{
    public void SetPosition(Vector3 position)
    {
        Transform = new Transform3D(Basis, position);
    }
}
