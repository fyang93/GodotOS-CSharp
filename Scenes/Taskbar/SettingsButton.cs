using Godot;
using System;

/// <summary>
/// The settings menu in the start menu. Just spawns the settings menu.
/// </summary>
public partial class SettingsButton : Button
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        Pressed += OnPressed;
	}

    private void OnPressed()
    {
        var window = GD.Load<PackedScene>("res://Scenes/Window/SettingsWindow/settings_window.tscn").Instantiate<FakeWindow>();

        window.TitleText = "[center]Settings Menu";
        GetTree().CurrentScene.CallDeferred("add_child", window);

        var taskbarButton = GD.Load<PackedScene>("res://Scenes/Taskbar/taskbar_button.tscn").Instantiate<TaskbarButton>();
        taskbarButton.TargetWindow = window;
        taskbarButton.ActiveColor = Colors.White;
        taskbarButton.TextureRect.Texture = Icon;

        GetTree().GetFirstNodeInGroup("taskbar_buttons").CallDeferred("add_child", taskbarButton);
    }
}
