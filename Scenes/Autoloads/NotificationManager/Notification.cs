using Godot;
using System;

public partial class Notification : Panel
{
	private RichTextLabel _notificationText => GetNode<RichTextLabel>("NotificationText");

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
		AdjustWidth();
		PlayAnimationAsync();
	}

	private void AdjustWidth()
	{
		while (true)
		{
			if (_notificationText.GetLineCount() > 1)
			{
				Size = Size with { X = Size.X + 20 };
				Position = Position with { X = Position.X - 20 };
			}
			else
			{
				Size = Size with { X = Size.X + 10 };
				Position = Position with { X = Position.X - 10 };
				return;
			}
		}
	}

	private async void PlayAnimationAsync()
	{
		var tween = CreateTween()
			.SetEase(Tween.EaseType.Out)
			.SetTrans(Tween.TransitionType.Sine);
		tween.TweenProperty(this, "position:y", Position.Y - 75, 3);
		await ToSignal(GetTree().CreateTimer(2), Timer.SignalName.Timeout);

		var fade = CreateTween()
			.SetEase(Tween.EaseType.Out)
			.SetTrans(Tween.TransitionType.Sine);
		fade.TweenProperty(this, "modulate:a", 0, 1.5);
		await ToSignal(fade, Tween.SignalName.Finished);

		QueueFree();
	}
}
