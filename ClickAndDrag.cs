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