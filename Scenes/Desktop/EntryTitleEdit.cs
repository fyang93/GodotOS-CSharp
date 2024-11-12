using Godot;
using System;
using System.IO;
using System.Threading.Tasks;

using FileAccess = Godot.FileAccess;

/// <summary>
/// Handles renaming of a folder
/// </summary>
public partial class EntryTitleEdit : TextEdit
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

    public override async void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("rename") && GetNode<Panel>("%SelectedHighlight").Visible)
		{
            ShowRename();
		}

        if (!GetParent<Control>().Visible) return;

        if (@event.IsActionPressed("ui_accept"))
        {
            AcceptEvent();
            await TriggerRenameAsync();
        }

        if (@event.IsActionPressed("ui_cancel"))
        {
            CancelRename();
        }

        // 检测鼠标点击是否发生在当前控件区域之外，如果是，则取消重命名操作
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.IsPressed())
        {
            var localEvent = MakeInputLocal(mouseEvent) as InputEventMouseButton;
            if (!new Rect2(Vector2.Zero, Size).HasPoint(localEvent.Position))
            {
                CancelRename();
            }
        }
    }

    public void ShowRename()
    {
        GetParent<Control>().Visible = true;
        GrabFocus();
        Text = GetNode<RichTextLabel>("%EntryTitle").Text.TrimPrefix("[center]").Split(".")[0];
        SelectAll();
    }

    private async Task TriggerRenameAsync()
    {
        if (Text.Contains("/") || Text.Contains("\\") || Text.Contains("¥") || Text.Contains("₩"))
        {
            NotificationManager.Instance.SpawnNotification("Error: File name can't include slashes!");
            return;
        }

        if (string.IsNullOrEmpty(Text))
        {
            NotificationManager.Instance.SpawnNotification("Error: File name can't be empty!");
            return;
        }

        GetParent<Control>().Visible = false;
        var entry = GetNode<FakeEntry>("../../..");

        // entry 是文件
        if (entry.EntryType != FakeEntryType.Folder)
        {
            var oldFileName = entry.EntryName;
            var newFileName = $"{Text}.{Path.GetExtension(entry.EntryName)}";

            if (FileAccess.FileExists(Path.Combine("user://files/", entry.EntryPath, newFileName)))
            {
                CancelRename();
                NotificationManager.Instance.SpawnNotification("That file already exists!");
                return;
            }

            entry.EntryName = newFileName;
            DirAccess.RenameAbsolute(Path.Combine("user://files/", entry.EntryPath, oldFileName),
                                     Path.Combine("user://files/", entry.EntryPath, entry.EntryName));
            GetNode<RichTextLabel>("%EntryTitle").Text = $"[center]{entry.EntryName}";

            if (entry.GetParent() is DesktopFileManager)
            {
                await (entry.GetParent() as DesktopFileManager).SortEntriesAsync();
            }
            else
            {
                foreach (FileManagerWindow fileManager in GetTree().GetNodesInGroup("file_manager_window"))
                {
                    if (fileManager.RelativePath == entry.EntryPath)
                    {
                        await fileManager.SortEntriesAsync();
                    }
                }
            }

            foreach (TextEditor textEditor in GetTree().GetNodesInGroup("text_editor_window"))
            {
                // TODO: 这里可能是为了分别处理桌面上的和文件管理器内的，但是好像不需要这么麻烦
                if (textEditor.FilePath == Path.Combine(entry.EntryPath, oldFileName))
                {
                    textEditor.FilePath = Path.Combine(entry.EntryPath, entry.EntryName);
                }
                else if (textEditor.FilePath == oldFileName)
                {
                    textEditor.FilePath = entry.EntryName;
                }
            }
        }
        // entry 是文件夹
        else if (entry.EntryType == FakeEntryType.Folder)
        {
            var oldFolderName = entry.EntryName;
            var oldFolderPath = entry.EntryPath;

            var newFolderPath = Path.Combine(Path.GetDirectoryName(oldFolderPath), Text);
            if (DirAccess.DirExistsAbsolute(Path.Combine("user://files/", newFolderPath)))
            {
                CancelRename();
                NotificationManager.Instance.SpawnNotification("That folder already exists!");
                return;
            }
            entry.EntryPath = newFolderPath;
            entry.EntryName = Text;

            GetNode<RichTextLabel>("%EntryTitle").Text = $"[center]{entry.EntryName}";
            DirAccess.RenameAbsolute(Path.Combine("user://files/", oldFolderPath),
                                     Path.Combine("user://files/", entry.EntryPath));

            if (entry.GetParent() is DesktopFileManager desktopFileManager)
            {
                await desktopFileManager.SortEntriesAsync();
            }

            foreach (FileManagerWindow fileManager in GetTree().GetNodesInGroup("file_manager_window"))
            {
                // 路径存在重命名，需要重载窗口
                if (fileManager.RelativePath.StartsWith(oldFolderPath))
                {
                    fileManager.RelativePath = fileManager.RelativePath.Replace(oldFolderPath, entry.EntryPath);
                    fileManager.ReloadWindow();
                }
                // 路径没变，但是里面有东西重命名了，需要重新排序
                else if (fileManager.RelativePath == Path.GetDirectoryName(entry.EntryPath))
                {
                    await fileManager.SortEntriesAsync();
                }
            }
        }
    }

    private void CancelRename()
    {
        GetParent<Control>().Visible = false;
        Text = "";
    }

}
