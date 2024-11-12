using Godot;
using System;

/// <summary>
/// A generic pause manager for games in GodotOS.
/// When you press ui_cancel, it pauses or unpauses.
/// NOTE: This node disabled by default but gets enabled by start menu option
/// if the generic pause menu bool is enabled there.
/// </summary>
public partial class GamePauseManager : Node
{
	[Export]
	private PackedScene _pausePackedScene;

    private FakeWindow _window;

    private GameWindow _gameWindow;

    private CanvasLayer _currentPauseScreen;

	public bool IsPaused { get; private set; }

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        _window = GetNode<FakeWindow>("..");
        _gameWindow = GetNode<GameWindow>("%GameWindow");
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

    public override void _Input(InputEvent @event)
    {
        if (!_window.IsSelected) return;

        if (@event.IsActionPressed("pause_game"))
        {
            TogglePause();
        }
    }

    private void TogglePause()
    {
        if (_gameWindow.GetChildCount() == 0)
        {
            NotificationManager.Instance.SpawnNotification("Error: No game scene to pause???");
            return;
        }

        var gameScene = _gameWindow.GetChild(0);

        if (IsPaused)
        {
            gameScene.ProcessMode = ProcessModeEnum.Inherit;
            _currentPauseScreen?.QueueFree();
        }
        else
        {
            gameScene.ProcessMode = ProcessModeEnum.Disabled;
            var pauseScreen = _pausePackedScene.Instantiate<CanvasLayer>();
            gameScene.AddChild(pauseScreen);
            _currentPauseScreen = pauseScreen;
        }

        IsPaused = !IsPaused;
    }
}
