using Godot;
using System;
using System.Linq;

/// <summary>
/// 用于显示桌面上的文件/文件夹
/// </summary>
public partial class DesktopFileManager : BaseFileManager
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		var userDir = DirAccess.Open("user://");
		if (!userDir.DirExists("files"))
		{
			// Can't just use absolute paths due to https://github.com/godotengine/godot/issues/82550
			// Also DirAccess can't open on res:// at export, but FileAccess does...
			userDir.MakeDirRecursive("files/WelcomeFolder");
			CopyFromRes("res://DefaultFiles/Welcome.txt", "user://files/WelcomeFolder/Welcome.txt");
            CopyFromRes("res://DefaultFiles/Credits.txt", "user://files/WelcomeFolder/Credits.txt");
            CopyFromRes("res://DefaultFiles/GodotOS_Handbook.txt", "user://files/WelcomeFolder/GodotOS_Handbook.txt");
            CopyFromRes("res://DefaultFiles/default_wall.webp", "user://files/default_wall.webp");

			var wallpaper = GetNode<Wallpaper>("/root/Control/Wallpaper");
			wallpaper.ApplyWallpaperFromPath("files/default_wall.webp");

			CopyFromRes("res://DefaultFiles/default_wall.webp", "user://default_wall.webp");
			DefaultValues.Instance.WallpaperName = "default_wall.webp";
			DefaultValues.Instance.SaveState();
        }

		PopulateFileManager();
        GetWindow().SizeChanged += () => UpdatePositions();
        GetWindow().FocusEntered += OnWindowFocus;
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

    /// <summary>
	/// 检查桌面上的文件是否有变化，如果有，则重新填充文件管理器
	/// </summary>
    private void OnWindowFocus()
	{
		var currentFileNames = (from child in GetChildren()
								where child is FakeEntry
								select (child as FakeEntry).EntryName).ToList();
		var newFileNames = DirAccess.GetFilesAt("user://files/").ToList();
		newFileNames.AddRange(DirAccess.GetDirectoriesAt("user://files/"));

		if (currentFileNames.Count != newFileNames.Count)
		{
			PopulateFileManager();
			return;
		}

		foreach (var fileName in newFileNames)
		{
			if (!currentFileNames.Contains(fileName))
			{
				PopulateFileManager();
				return;
			}
		}
	}

    private void CopyFromRes(string from, string to)
    {
        var fileFrom = FileAccess.Open(from, FileAccess.ModeFlags.Read);
        var fileTo = FileAccess.Open(to, FileAccess.ModeFlags.Write);
        fileTo.StoreBuffer(fileFrom.GetBuffer((long)fileFrom.GetLength()));

        fileFrom.Close();
        fileTo.Close();
    }
}
