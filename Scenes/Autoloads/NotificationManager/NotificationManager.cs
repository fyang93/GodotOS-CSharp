using Godot;
using System;

/// <summary>
/// Spawns notifications in the bottom right of the screen.
/// Often used to show errors or file actions (copying, pasting).
/// </summary>
public partial class NotificationManager : Control
{
	public static NotificationManager Instance;

	private PackedScene _notificationScene => GD.Load<PackedScene>("res://Scenes/Autoloads/NotificationManager/notification.tscn");

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Instance = this;
	}

	public void SpawnNotification(string text)
	{
		var newNotification = _notificationScene.Instantiate();
		newNotification.GetNode<RichTextLabel>("NotificationText").Text = $"[center]{text}";

		AddChild(newNotification);
	}
}
