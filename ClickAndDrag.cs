using System;
using Godot;

public partial class ClickAndDrag : Area3D
{
    [Export()] public bool AllowDrag { get; set; }
    public event DragReleaseEvent DragRelease;
    public event DragEvent Drag;
    private bool Dragging { get; set; }

    public override void _MouseEnter()
    {
        base._MouseEnter();
        GD.Print("Mouse entered!");
    }

    public override void _MouseExit()
    {
        base._MouseExit();
        GD.Print("Mouse exited!");
    }

    public override void _InputEvent(Camera3D camera, InputEvent @event, Vector3 position, Vector3 normal, int shapeIdx)
    {
        base._InputEvent(camera, @event, position, normal, shapeIdx);
        switch (@event)
        {
            case InputEventMouseButton inputEventMouseButton:
                if (inputEventMouseButton.Pressed)
                {
                    Dragging = true;
                }
                else if (Dragging)
                {
                    Dragging = false;
                    DragRelease?.Invoke(this, new DragReleaseEventArgs(camera, position));
                }

                break;
            case InputEventMouseMotion inputEventMouseMotion:
                if (Dragging)
                {
                    Drag?.Invoke(this, new DragEventArgs(camera, position));
                }
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(@event));
        }

    }
}

public delegate void DragEvent(object sender, DragEventArgs args);

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

public delegate void DragReleaseEvent(object sender, DragReleaseEventArgs args);

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
