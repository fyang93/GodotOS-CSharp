using Godot;
using System;
using System.IO;

using FileAccess = Godot.FileAccess;

public partial class Wallpaper : TextureRect
{
	[Signal]
	public delegate void WallpaperAddedEventHandler();

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        // 设置 Fade 节点的透明度为 0，并使其可见
        GetNode<Control>("Fade").Modulate = GetNode<Control>("Fade").Modulate with { A = 0 };
        GetNode<Control>("Fade").Visible = true;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
	{
	}

	public void ApplyWallpaperFromPath(string path)
	{
		EmitSignal(SignalName.WallpaperAdded);

        if (!FileAccess.FileExists($"user://{path}")) return;
        var image = Image.LoadFromFile($"user://{path}");
		AddWallpaper(image);
	}

    public void ApplyWallpaperFromFile(FakeEntry imageFile)
    {
        EmitSignal(SignalName.WallpaperAdded);

        DefaultValues.Instance.SaveWallpaper(imageFile);

        var image = Image.LoadFromFile(Path.Combine("user://files/", imageFile.EntryPath, imageFile.EntryName));
        AddWallpaper(image);
    }

	public async void AddWallpaper(Image image)
	{
		image.GenerateMipmaps();
		var textureImport = ImageTexture.CreateFromImage(image);

        // 创建 Tween 以淡入壁纸
        var fade = GetNode<Control>("Fade");
        var tween = GetTree().CreateTween();
        tween.TweenProperty(fade, "modulate:a", 1.0f, 0.5f).SetTrans(Tween.TransitionType.Cubic);
        await ToSignal(tween, Tween.SignalName.Finished);

        Texture = textureImport;

        // 创建第二个 Tween 以淡出效果
        var tween2 = GetTree().CreateTween();
        tween2.TweenProperty(fade, "modulate:a", 0.0f, 0.5f).SetTrans(Tween.TransitionType.Cubic);
    }

	public async void RemoveWallpaper()
	{
		DefaultValues.Instance.DeleteWallpaper();

        // 创建 Tween 以淡入效果
        var fade = GetNode<Control>("Fade");
        var tween = GetTree().CreateTween();
        tween.TweenProperty(fade, "modulate:a", 1.0f, 0.5f).SetTrans(Tween.TransitionType.Cubic);
        await ToSignal(tween, Tween.SignalName.Finished);

        Texture = null;

        // 创建第二个 Tween 以淡出效果
        var tween2 = GetTree().CreateTween();
        tween2.TweenProperty(fade, "modulate:a", 0.0f, 0.5f).SetTrans(Tween.TransitionType.Cubic);
    }
}
