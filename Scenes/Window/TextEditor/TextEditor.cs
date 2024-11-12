using Godot;
using System;
using System.Drawing;
using System.IO;

using FileAccess = Godot.FileAccess;

public partial class TextEditor : CodeEdit
{
	private FakeWindow _window => GetNode<FakeWindow>("../..");
	private bool _textEdited;
	private string _filePath;

	public string FilePath
	{
		get => _filePath;
		set
		{
			_filePath = value;
			GetNode<RichTextLabel>("%TitleText").Text = $"[center]{Path.GetFileName(_filePath)}{(_textEdited ? "*" : "")}";
		}
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_window.Selected += OnWindowSelected;
		AdjustMenuOptions();

		TextChanged += OnTextChanged;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

    public override void _Input(InputEvent @event)
    {
		if (!_window.IsSelected) return;

		if (@event.IsActionPressed("save"))
		{
			AcceptEvent();
			SaveFile();
		}
    }

	public void PopulateText(string path)
	{
		FilePath = path;
		using (var file = FileAccess.Open(Path.Combine("user://files/", FilePath), FileAccess.ModeFlags.Read))
		{
			Text = file.GetAsText();
		}
	}

	private void OnTextChanged()
	{
		if (_textEdited) return;

		_textEdited = true;
		GetNode<RichTextLabel>("%TitleText").Text += '*';
	}

    private void OnWindowSelected(bool selected)
    {
        if (selected)
            GrabFocus();
        else
            ReleaseFocus();
    }

	private void SaveFile()
	{
		if (!_textEdited) return;

		if (!FileAccess.FileExists(Path.Combine("user://files/", FilePath)))
        {
			NotificationManager.Instance.SpawnNotification("[color = fc6c64]Couldn't save text file: File no longer exists");
            return;
        }

		using (var file = FileAccess.Open(Path.Combine("user://files/", FilePath), FileAccess.ModeFlags.Write))
		{
			file.StoreString(Text);
		}

		GetNode<RichTextLabel>("%TitleText").Text = GetNode<RichTextLabel>("%TitleText").Text.TrimEnd('*');
		_textEdited = false;

		var saveNotification = (Panel)GD.Load<PackedScene>("res://Scenes/Window/TextEditor/saved_notification.tscn").Instantiate();
		saveNotification.Position = new (_window.Size.X - saveNotification.Size.X - 15, _window.Size.Y - saveNotification.Size.Y - 15);
		_window.AddChild(saveNotification);
    }

    // Adjusts right-click options for this TextEdit.
    // Removes unnecessary options and adds one for word wrap.
	private void AdjustMenuOptions()
	{
		var menu = GetMenu();
		menu.RemoveItem(5);
		menu.RemoveItem(10);
		menu.RemoveItem(10);
		menu.RemoveItem(10);

		menu.AddCheckItem("Word Wrap");
		menu.SetItemChecked(-1, true);

		menu.IdPressed += SetWordWrap;
    }

    private void SetWordWrap(long id)
    {
        if (id == 10)
        {
            var menu = GetMenu();
            if (WrapMode == LineWrappingMode.None)
            {
                WrapMode = LineWrappingMode.Boundary;
                menu.SetItemChecked(-1, true);
            }
            else
            {
                WrapMode = LineWrappingMode.None;
                menu.SetItemChecked(-1, false);
            }
        }
    }
}
