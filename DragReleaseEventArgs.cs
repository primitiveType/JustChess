using Godot;

public class DragReleaseEventArgs
{
    public Camera3D Camera3D { get; }
    public Vector3 Position { get; }

    public DragReleaseEventArgs(Camera3D camera3D, Vector3 position)
    {
        Camera3D = camera3D;
        Position = position;
    }
}