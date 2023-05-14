using Godot;

public class DragEventArgs
{
    public Camera3D Camera3D { get; }
    public Vector3 Position { get; }

    public DragEventArgs(Camera3D camera3D, Vector3 position)
    {
        Camera3D = camera3D;
        Position = position;
    }
}