using Godot;
using System;

public partial class ContextMenuOption : Panel
{
	// Signal for option clicked
	[Signal]
	public delegate void OptionClickedEventHandler();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		MouseEntered += OnMouseEntered;
		MouseEntered += OnMouseExited;
	}

    public override void _GuiInput(InputEvent @event)
    {
		if (@event is InputEventMouseButton e && e.ButtonIndex == MouseButton.Left && e.IsPressed())
		{
			EmitSignal(SignalName.OptionClicked);
		}
    }

	private void OnMouseEntered()
	{
		var tween = CreateTween()
			.SetEase(Tween.EaseType.Out)
			.SetTrans(Tween.TransitionType.Cubic);
		tween.TweenProperty(this, "self_modulate:a", 1f, 0.2f);
	}

	private void OnMouseExited()
	{
		var tween = CreateTween()
			.SetEase(Tween.EaseType.Out)
			.SetTrans(Tween.TransitionType.Cubic);
		tween.TweenProperty(this, "self_modulate:a", 0.3f, 0.2f);
	}
}
