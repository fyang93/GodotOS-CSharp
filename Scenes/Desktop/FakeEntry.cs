using Godot;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;

using FileAccess = Godot.FileAccess;

// Enum to define entry types
// 顺序会被用于在文件浏览器中排序
public enum FakeEntryType
{
    Folder = 0,
    Text,
    Image
}

public partial class FakeEntry : Control
{
    /// <summary>
    /// Entry 的名称：如文件夹名，文件名
    /// </summary>
    [Export]
    [Required]
    public string EntryName { get; set; }

    private string _entryPath = "";

    /// <summary>
    /// Entry 相对于 user://files/ 的路径
    /// </summary>
    [Export]
    [Required]
    [NotNull]
    public string EntryPath
    {
        get => _entryPath;
        set
        {
            if (value != null && _entryPath != value)
            {
                _entryPath = value.Replace('\\', '/');
                GD.Print(_entryPath);
            }
        }
    }

    /// <summary>
    /// Entry 的类型
    /// </summary>
    [Export]
    [Required]
	public FakeEntryType EntryType;

    private static readonly Dictionary<FakeEntryType, Color> _defaultColors = new Dictionary<FakeEntryType, Color>
    {
        [FakeEntryType.Folder] = new Color("#4efa82"),
        [FakeEntryType.Text] = new Color("#4deff5"),
        [FakeEntryType.Image] = new Color("#f9ee13")
    };

    private static readonly Dictionary<FakeEntryType, Resource> _defaultIcons = new Dictionary<FakeEntryType, Resource>
    {
        [FakeEntryType.Folder] = GD.Load("res://Art/FolderIcons/folder.png"),
        [FakeEntryType.Text] = GD.Load("res://Art/FolderIcons/text_file.png"),
        [FakeEntryType.Image] = GD.Load("res://Art/FolderIcons/image.png")
    };

    private static readonly Dictionary<FakeEntryType, Resource> _defaultWindows = new Dictionary<FakeEntryType, Resource>
    {
        [FakeEntryType.Folder] = GD.Load("res://Scenes/Window/FileManager/file_manager_window.tscn"),
        [FakeEntryType.Text] = GD.Load("res://Scenes/Window/TextEditor/text_editor.tscn"),
        [FakeEntryType.Image] = GD.Load("res://Scenes/Window/ImageViewer/image_viewer.tscn"),
    };

	private bool _isMouseOver;
    private Timer _doubleClickTimer => GetNode<Timer>("DoubleClick");
    private Panel _hoverHighlight => GetNode<Panel>("HoverHighlight");
    private Panel _selectedHighlight => GetNode<Panel>("SelectedHighlight");
    private RichTextLabel _entryTitle => GetNode<RichTextLabel>("%EntryTitle");
    private TextureRect _textureRect => GetNode<TextureRect>("Icon/TextureRect");
    private BaseFileManager _parentFileManager => GetParent<BaseFileManager>();


    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
		_hoverHighlight.SelfModulate = GetNode<Panel>("HoverHighlight").SelfModulate with { A = 0 }; // Hide hover highlight initially
		_selectedHighlight.Visible = false;
		_entryTitle.Text = $"[center]{EntryName}";

        // Set the icon and color based on file type
        _textureRect.Modulate = _defaultColors[EntryType];
        _textureRect.Texture = _defaultIcons[EntryType] as Texture2D;

        MouseEntered += () =>
        {
            ShowHoverHighlight();
            _isMouseOver = true;
        };
        MouseExited += () =>
        {
            HideHoverHighlight();
            _isMouseOver = false;
        };
    }

    public override async void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.IsPressed())
        {
            if (!_isMouseOver)
            {
                HideSelectedHighlight();
            }
            else
            {
                ShowHoverHighlight();
                if (!_isMouseOver || mouseEvent.ButtonIndex != MouseButton.Left) return;

                // 首次单击，双击计时器处于停止状态，计时器启动
                if (_doubleClickTimer.IsStopped())
                {
                    _doubleClickTimer.Start();    
                }
                // 如果计时器未停止，表示用户在计时器时间间隔内进行了第二次单击
                else
                {
                    AcceptEvent();
                    OpenEntry();
                }
            }

            if (_selectedHighlight.Visible && !_entryTitle.Visible && @event is InputEventKey keyEvent)
            {
                if (@event.IsActionPressed("delete")) await DeleteEntryAsync();
                else if (@event.IsActionPressed("ui_copy")) CopyPasteManager.Instance.CopyEntry(this);
                else if (@event.IsActionPressed("ui_cut")) CopyPasteManager.Instance.CutEntry(this);
                else if (@event.IsActionPressed("ui_up"))
                {
                    AcceptEvent();
                    _parentFileManager.SelectFolderUp(this);
                }
                else if (@event.IsActionPressed("ui_down"))
                {
                    AcceptEvent();
                    _parentFileManager.SelectFolderDown(this);
                }
                else if (@event.IsActionPressed("ui_left"))
                {
                    AcceptEvent();
                    _parentFileManager.SelectFolderLeft(this);
                }
                else if (@event.IsActionPressed("ui_right"))
                {
                    AcceptEvent();
                    _parentFileManager.SelectFolderRight(this);
                }
                else if (@event.IsActionPressed("ui_accept"))
                {
                    AcceptEvent();
                    OpenEntry();
                }
            }
        }
    }

    public async Task DeleteEntryAsync()
    {
        if (EntryType == FakeEntryType.Folder)
        {
            var deletePath = ProjectSettings.GlobalizePath(Path.Combine("user://files/", EntryPath));
            if (!DirAccess.DirExistsAbsolute(deletePath)) return;

            OS.MoveToTrash(deletePath);

            foreach (FileManagerWindow fileManager in GetTree().GetNodesInGroup("file_manager_window"))
            {
                // 文件管理器对应的文件夹被删除了
                if (fileManager.RelativePath.StartsWith(EntryPath))
                {
                    await fileManager.CloseWindowAsync();
                }
                // 当前文件所在的文件管理器对应的文件夹被删除了
                else if (GetParent() is FileManagerWindow parent && parent.RelativePath == fileManager.RelativePath)
                {
                    await fileManager.RemoveEntryWithNameAsync(EntryName);
                    fileManager.UpdatePositions();
                }
            }
        }
        else
        {
            var deletePath = ProjectSettings.GlobalizePath(Path.Combine("user://files/", EntryPath, EntryName));
            if (!FileAccess.FileExists(deletePath)) return;

            OS.MoveToTrash(deletePath);

            foreach (FileManagerWindow fileManager in GetTree().GetNodesInGroup("file_manager_window"))
            {
                // 已打开的文件管理器中包含被删除的文件
                if (fileManager.RelativePath == EntryPath)
                {
                    await fileManager.RemoveEntryWithNameAsync(EntryName);
                    await fileManager.SortEntriesAsync();
                }
            }
        }

        // 文件/文件夹位于桌面
        if (string.IsNullOrEmpty(EntryPath) || 
            (EntryType == FakeEntryType.Folder && string.IsNullOrEmpty(Path.GetDirectoryName(EntryPath)))) {
            var desktopFileManager = GetTree().GetFirstNodeInGroup("desktop_file_manager") as DesktopFileManager;
            await desktopFileManager.RemoveEntryWithNameAsync(EntryName);
            await desktopFileManager.SortEntriesAsync();
        }

        NotificationManager.Instance.SpawnNotification($"Moved [color=59ea90][wave freq=7]{EntryName}[/wave][/color] to trash!");
        //this?.QueueFree();
    }

    public void OpenEntry()
    {
        HideSelectedHighlight();
        if (GetParent().IsInGroup("file_manager_window") && EntryType == FakeEntryType.Folder)
        {
            var parentFileManager = GetParent() as FileManagerWindow;
            parentFileManager?.ReloadWindow(EntryPath);
        }
        else
        {
            SpawnWindow();
        }
    }

    // Show/Hide hover effects
    private void ShowHoverHighlight()
    {
        var tween = CreateTween();
        tween.TweenProperty(_hoverHighlight, "self_modulate:a", 1, 0.25f);
    }

    private void HideHoverHighlight()
    {
        Tween tween = CreateTween();
        tween.TweenProperty(_hoverHighlight, "self_modulate:a", 0, 0.25f);
    }

    // Show/Hide selection highlight
    public void ShowSelectedHighlight()
    {
        _selectedHighlight.Visible = true;
    }

    public void HideSelectedHighlight()
    {
        _selectedHighlight.Visible = false;
    }

    // Spawning different windows based on file type
    private void SpawnWindow()
    {
        var windowScene = _defaultWindows[EntryType] as PackedScene;
        var window = windowScene.Instantiate<FakeWindow>();

        // Set window content based on file type
        switch (EntryType)
        {
            case FakeEntryType.Folder:
                window.GetNode<FileManagerWindow>("%FileManagerWindow").RelativePath = EntryPath;
                break;

            case FakeEntryType.Text:
                window.GetNode<TextEditor>("%TextEditor").PopulateText(string.IsNullOrEmpty(EntryPath) ? EntryName : $"{EntryPath}/{EntryName}");
                break;

            case FakeEntryType.Image:
                window.GetNode<ImageViewer>("%ImageViewer").ImportImage(string.IsNullOrEmpty(EntryPath) ? EntryName : $"{EntryPath}/{EntryName}");
                break;
        }

        window.TitleText = _entryTitle.Text;

        GetTree().CurrentScene.AddChild(window);

        var taskbarButton = GD.Load<PackedScene>("res://Scenes/Taskbar/taskbar_button.tscn").Instantiate<TaskbarButton>();
        taskbarButton.TargetWindow = window;

        taskbarButton.ActiveColor = _defaultColors[EntryType];
        taskbarButton.TextureRect.Texture = _textureRect.Texture;
        GetTree().GetFirstNodeInGroup("taskbar_buttons").AddChild(taskbarButton);
    }
}
