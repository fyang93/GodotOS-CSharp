using Godot;
using System;

public partial class ImageViewer : TextureRect
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void ImportImage(string filePath)
	{
		var fullPath = $"user://files/{filePath}";

        // Check if the file exists
        if (!FileAccess.FileExists(fullPath))
        {
            NotificationManager.Instance.SpawnNotification("Error: Cannot find file (was it moved or deleted?)");
            return;
        }

        // Load the image from the file
        var image = new Image();
        var loadError = image.Load(fullPath);

        if (loadError != Error.Ok)
        {
            NotificationManager.Instance.SpawnNotification("Error: Failed to load the image.");
            return;
        }

        image.GenerateMipmaps();
        var textureImport = ImageTexture.CreateFromImage(image);
        Texture = textureImport;
    }
}
