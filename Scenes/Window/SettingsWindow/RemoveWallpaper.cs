using Godot;
using System;

/// <summary>
/// The remove wallpaper button in the settings menu.
/// </summary>
public partial class RemoveWallpaper : Button
{
	private Wallpaper _wallpaper => GetNode<Wallpaper>("/root/Control/Wallpaper");

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if (_wallpaper == null)
		{
			GD.PrintErr("remove_wallpaper.gd: Couldn't find wallpaper (are you debugging the settings menu?)");
			return;
		}

		if (_wallpaper.Texture == null)
		{
			Disabled = true;
		}
		_wallpaper.WallpaperAdded += () => Disabled = false;
		Pressed += () =>
		{
			_wallpaper.RemoveWallpaper();
			Disabled = true;
		};
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
