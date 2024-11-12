using Godot;
using System;

/// <summary>
/// The game window, used to show games.
/// </summary>
public partial class GameWindow : SubViewport
{
	private FakeWindow _window => GetNode<FakeWindow>("../../..");
	private GamePauseManager _gamePauseManager => GetNode<GamePauseManager>("%GamePauseManager");

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
		_window.Minimized += HandleWindowMinimized;
		_window.Selected += HandleWindowSelected;
	}

	private void HandleWindowMinimized(bool isMinimized)
	{
		if (_gamePauseManager.IsPaused) return;

		GetChild(0).ProcessMode = isMinimized ? ProcessModeEnum.Disabled : ProcessModeEnum.Inherit;
	}

    // Disables input if the window isn't selected.
    private void HandleWindowSelected(bool isSelected)
	{
		if (isSelected)
		{
            PropagateCall("set_mouse_filter", [(int)Control.MouseFilterEnum.Stop]);
        }
		else
		{
            PropagateCall("set_mouse_filter", [(int)Control.MouseFilterEnum.Ignore]);
        }
    }
}
