using Godot;
using System;

public partial class ResizeDragSpot : Control
{
	private FakeWindow _window;
	private bool _isDragging;

	private Vector2 _startSize;
	private Vector2 _mouseStartDragPosition;

	[Signal]
	public delegate void WindowResizedEventHandler();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_window = GetParent<FakeWindow>();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton buttonEvent && buttonEvent.ButtonIndex == MouseButton.Left)
        {
            if (buttonEvent.IsPressed())
            {
                _isDragging = true;
                _mouseStartDragPosition = GetGlobalMousePosition();
                _startSize = _window.Size;
            }
            else
            {
                _isDragging = false;
            }
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_isDragging)
        {
            EmitSignal(SignalName.WindowResized);
            if (Input.IsKeyPressed(Key.Shift))
            {
                var aspectRatio = _startSize.X / (_startSize.Y - 30); // TODO: 30是工具栏的高度，记得引用
                var mousePosition = GetGlobalMousePosition();
                _window.Size = new Vector2(
                    _startSize.X + (mousePosition.X - _mouseStartDragPosition.X) * aspectRatio,
                    _startSize.Y + (mousePosition.X - _mouseStartDragPosition.X)
                    );
            }
            else
            {
                _window.Size = _startSize + (GetGlobalMousePosition() - _mouseStartDragPosition);
            }

            _window.ClampWindowInsideViewport();
        }
    }
}
