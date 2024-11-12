using Godot;
using System;
using System.IO;
using System.Threading.Tasks;

public partial class FileManagerWindow : BaseFileManager
{
	// Called when the node enters the scene tree for the first time.
	public override async void _Ready()
	{
		PopulateFileManager();
		await SortEntriesAsync();

        // Connect window resized signal
        GetNode<ResizeDragSpot>("%ResizeDragSpot").WindowResized += () => UpdatePositions();
		GetNode<Button>("%BackButton").Pressed += OnBackButtonPressed;

    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void ReloadWindow(string folderPath = "")
	{
		// Reload the smae path if not given folderPath
		if (!string.IsNullOrEmpty(folderPath))
		{
			RelativePath = folderPath;
		}

		// Free all FakeEntry nodes
		foreach (Node child in GetChildren())
		{
			if (child is FakeEntry entry)
			{
				entry.QueueFree();
			}
		}

		PopulateFileManager();

		// Update title text
		GetNode<RichTextLabel>("%TitleText").Text = $"[center]{RelativePath}";
	}

	public async Task CloseWindowAsync()
	{
		await GetNode<FakeWindow>("../..").CloseWindowAsync();
	}

    // Goes to the folder above the currently shown one. Can't go higher than user://files/
    private void OnBackButtonPressed()
	{
		if (string.IsNullOrEmpty(RelativePath)) return;

		var parent = Path.GetDirectoryName(RelativePath);
		if (string.IsNullOrEmpty(parent)) return;
        RelativePath = parent;
		ReloadWindow(RelativePath);
	}
}
