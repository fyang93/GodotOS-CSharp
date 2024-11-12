using Godot;
using System;
using System.Threading.Tasks;

public partial class ContextMenuManager : Panel
{
    #region 快速实例化右键菜单子节点
    private ContextMenuOption _newContextMenuOption => GD.Load<PackedScene>("res://Scenes/Autoloads/ContextMenu/context_menu_option.tscn").Instantiate<ContextMenuOption>();
    private Node _newContextMenuSeparator => GD.Load<PackedScene>("res://Scenes/Autoloads/ContextMenu/context_menu_seperator.tscn").Instantiate();
    #endregion

    private VBoxContainer _optionContainer => GetNode<VBoxContainer>("VBoxContainer");

    // 被右键点击的节点
    private Node _target;

    // 检查鼠标是否在菜单上方
    private bool _isMouseOver;

    // 用作防止右键菜单频繁显示的冷却
    private bool _isShownRecently;

    public static ContextMenuManager Instance;

    /// <summary>
    /// 初始化函数，设置菜单为不可见，并为每个 "right_click_enabled" 组中的节点添加右键处理器
    /// </summary>
    public override async void _Ready()
	{
        Instance = this;

        Visible = false;
        // 让节点有机会添加子节点 ?
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

        foreach (var node in GetTree().GetNodesInGroup("right_click_enabled"))
        {
            AddRightClickHandler(node);
        }

        GetTree().NodeAdded += AddRightClickHandler;
	}

    // 处理鼠标输入事件，左键点击时隐藏菜单
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton e && e.IsPressed())
        {
            if (e.ButtonIndex == MouseButton.Left && Visible)
            {
                HideContextMenu();
            }
        }
    }

    // 为每个 "right_click_enabled" 组的节点添加右键处理器
    private void AddRightClickHandler(Node node)
    {
        if (node.IsInGroup("right_click_enabled") && node is Control control)
        {
            control.GuiInput += (@event) =>
            {
                if (@event is InputEventMouseButton e && e.ButtonIndex == MouseButton.Right && e.IsPressed())
                {
                    HandleRightClick(control);
                }
            };
        }
    }

    private void HandleRightClick(Control node)
    {
        if (_isShownRecently) return;

        foreach (var option in _optionContainer.GetChildren())
        {
            option.QueueFree();
        }

        if (node is FakeEntry)
        {
            _target = node;
            AddEntryOptions();
            PlayCooldown();
        }
        else if (node is FileManagerWindow || node is DesktopFileManager)
        {
            _target = node;
            AddFileManagerOptions();
            PlayCooldown();
        }

        ShowContextMenu();
    }

    #region 右键菜单显示/隐藏
    // 显示右键菜单并设置位置
    private void ShowContextMenu()
    {
        Visible = true;
        GlobalPosition = GetGlobalMousePosition() + new Vector2(10, 15);
        ClampInsideViewport();
        Modulate = Modulate with { A = 0 };
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate:a", 1, 0.15f);
    }

    // 隐藏右键菜单
    private async void HideContextMenu()
    {
        var tween = CreateTween();
        tween.TweenProperty(this, "modulate:a", 0, 0.10f);
        await ToSignal(tween, Tween.SignalName.Finished);
        if (Modulate.A == 0)
        {
            Visible = false;
        }
    }
    #endregion

    private async void PlayCooldown()
    {
        _isShownRecently = true;
        await ToSignal(GetTree().CreateTimer(0.1f), Timer.SignalName.Timeout);
        _isShownRecently = false;
    }
   
    /// <summary>
    /// Adds options visible when right-clicking a folder or file
    /// </summary>
    private void AddEntryOptions()
    {
        if (_target is not FakeEntry) return;

        var typeName = (_target as FakeEntry).EntryType switch
        {
            FakeEntryType.Folder => "Folder",
            _ => "File"
        };

        var renameOption = _newContextMenuOption;
        renameOption.GetNode<RichTextLabel>("%OptionText").Text = $"Rename {typeName}";
        renameOption.OptionClicked += () => ((FakeEntry)_target).GetNode<EntryTitleEdit>("%EntryTitleEdit").ShowRename();

        var copyOption = _newContextMenuOption;
        copyOption.GetNode<RichTextLabel>("%OptionText").Text = $"Copy {typeName}";
        copyOption.OptionClicked += () => CopyPasteManager.Instance.CopyEntry((FakeEntry)_target);

        var cutOption = _newContextMenuOption;
        cutOption.GetNode<RichTextLabel>("%OptionText").Text = $"Cut {typeName}";
        cutOption.OptionClicked += () => CopyPasteManager.Instance.CutEntry((FakeEntry)_target);

        var deleteOption = _newContextMenuOption;
        deleteOption.GetNode<RichTextLabel>("%OptionText").Text = "Move to trash";
        deleteOption.OptionClicked += async () => await ((FakeEntry)_target).DeleteEntryAsync();

        _optionContainer.AddChild(renameOption);

        if (((FakeEntry)_target).EntryType == FakeEntryType.Image)
        {
            var setWallpaperOption = _newContextMenuOption;
            setWallpaperOption.GetNode<RichTextLabel>("%OptionText").Text = "Set as wallpaper";
            setWallpaperOption.OptionClicked += () => GetNode<Wallpaper>("/root/Control/Wallpaper").ApplyWallpaperFromFile((FakeEntry)_target);
            _optionContainer.AddChild(setWallpaperOption);
        }

        _optionContainer.AddChild(_newContextMenuSeparator);
        _optionContainer.AddChild(copyOption);
        _optionContainer.AddChild(cutOption);
        _optionContainer.AddChild(_newContextMenuSeparator);
        _optionContainer.AddChild(deleteOption);
    }

    private void AddFileManagerOptions()
    {
        var newFolderOption = _newContextMenuOption;
        newFolderOption.GetNode<RichTextLabel>("%OptionText").Text = "New Folder";
        newFolderOption.OptionClicked += async () => await ((FileManagerWindow)_target).NewFolderAsync();

        var newTextFileOption = _newContextMenuOption;
        newTextFileOption.GetNode<RichTextLabel>("%OptionText").Text = "New Text File";
        newTextFileOption.OptionClicked += async () => await ((FileManagerWindow)_target).NewFileAsync(".txt", FakeEntryType.Text);

        if (!string.IsNullOrEmpty(CopyPasteManager.Instance.TargetEntryName))
        {
            var pasteEntryOption = _newContextMenuOption;
            pasteEntryOption.GetNode<RichTextLabel>("%OptionText").Text = CopyPasteManager.Instance.TargetEntryType == FakeEntryType.Folder ? "Paste Folder" : "Paste File";
            pasteEntryOption.OptionClicked += () => CopyPasteManager.Instance.PasteEntry(((BaseFileManager)_target).RelativePath);

            _optionContainer.AddChild(pasteEntryOption);
            _optionContainer.AddChild(_newContextMenuSeparator);
        }

        _optionContainer.AddChild(newFolderOption);
        _optionContainer.AddChild(newTextFileOption);
    }

    /// <summary>
    /// 限制菜单显示在视口范围内
    /// </summary>
    private void ClampInsideViewport()
    {
        Vector2 gameWindowSize = GetViewportRect().Size;
        // TODO: 40 不应该硬编码
        Size = new(Mathf.Min(Size.X, gameWindowSize.X),
                   Mathf.Min(Size.Y, gameWindowSize.Y - 40));

        GlobalPosition = new Vector2(
            Mathf.Clamp(GlobalPosition.X, 0, gameWindowSize.X - Size.X),
            Mathf.Clamp(GlobalPosition.Y, 0, gameWindowSize.Y - Size.Y - 40)
        );
    }

}
