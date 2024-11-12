using Godot;
using Godot.Collections;
using System;
using System.IO;

using FileAccess = Godot.FileAccess;

/// <summary>
///  Sets some default values on startup and handles saving/loading user preferences
/// </summary>
public partial class DefaultValues : Node
{
    private static readonly string _userPreferencePath = "user://user_preferences.txt";

	private ColorRect _backgroundColorRect;
	private Wallpaper _wallpaper;

    public static DefaultValues Instance;

    public string WallpaperName { get; set; }


    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        Instance = this;

		_backgroundColorRect = GetNode<ColorRect>("/root/Control/BackgroundColor");
		_wallpaper = GetNode<Wallpaper>("/root/Control/Wallpaper");

		DisplayServer.WindowSetMinSize(new Vector2I(800, 600));

		// NOTE: Vsync is disabled due to input lag: https://github.com/godotengine/godot/issues/75830
		// NOTE: Web can't get screen refresh rate
        var newMaxFps = (int) DisplayServer.ScreenGetRefreshRate();
        Engine.MaxFps = (newMaxFps == -1) ? 60 : newMaxFps;

		if (_wallpaper == null || _backgroundColorRect == null)
		{
			GD.PrintErr("DefaultValues.cs: Couldn't find wallpaper (are you debugging a scene?)");
            return;
        }

        if (FileAccess.FileExists(_userPreferencePath))
        {
            LoadState();
        }
        else
        {
            SaveState();
        }
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

    public void SaveState()
    {
        var saveDict = new Dictionary
        {
            { "wallpaper_name", WallpaperName },
            { "background_color", _backgroundColorRect.Color.ToHtml() },
            { "zoom_level", GetWindow().ContentScaleFactor }
        };

        string jsonString = Json.Stringify(saveDict);

        using (var saveFile = FileAccess.Open(_userPreferencePath, FileAccess.ModeFlags.Write))
        {
            saveFile.StoreLine(jsonString);
        }
    }

    public void LoadState()
    {
        using (var saveFile = FileAccess.Open(_userPreferencePath, FileAccess.ModeFlags.Read))
        {
            string jsonString = saveFile.GetLine();
            var json = new Json();
            var parseResult = json.Parse(jsonString);

            if (parseResult != Error.Ok)
            {
                GD.PrintErr("DefaultValues.cs: Failed to parse user preferences file!");
                return;
            }

            var saveDict = (Dictionary) json.Data;

            WallpaperName = (string) saveDict["wallpaper_name"];

            if (!string.IsNullOrEmpty(WallpaperName))
            {
                _wallpaper.ApplyWallpaperFromPath(WallpaperName);
            }
            _backgroundColorRect.Color = Color.FromString((string) saveDict["background_color"], Color.Color8(77, 77, 77));
            GetWindow().ContentScaleFactor = (float) saveDict["zoom_level"];
        }
    }

    // 将壁纸复制到 "user://" 文件夹中，以便可以在下次启动时加载它。
    public void SaveWallpaper(FakeEntry wallpaperFile)
    {
        DeleteWallpaper();

        string from = Path.Combine("user://files/", wallpaperFile.EntryPath, wallpaperFile.EntryName);
        string to = Path.Combine($"user://{wallpaperFile.EntryName}");
        DirAccess.CopyAbsolute(from, to);

        WallpaperName = wallpaperFile.EntryName;
        SaveState();
    }

    // 删除已存储的壁纸文件。
    public void DeleteWallpaper()
    {
        if (!string.IsNullOrEmpty(WallpaperName))
        {
            DirAccess.RemoveAbsolute($"user://{WallpaperName}");
        }

        WallpaperName = "";
        SaveState();
    }
}
