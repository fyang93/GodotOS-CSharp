using Godot;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using FileAccess = Godot.FileAccess;

public partial class BaseFileManager : SmoothContainer
{
    private string _relativePath = "";

    /// <summary>
    /// 当前文件管理器所在文件夹相对于 user://files/ 的路径
    /// </summary>
    [Export]
    [NotNull]
    public string RelativePath {
        get => _relativePath;
        set
        {
            if (value != null && _relativePath != value)
            {
                _relativePath = value.Replace('\\', '/');
            }
        }
    }

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public async void PopulateFileManager()
	{
		foreach (Node child in GetChildren())
		{
			if (child is FakeEntry)
			{
				child.QueueFree();
			}
		}

        // 添加文件夹
		foreach (var folderName in DirAccess.GetDirectoriesAt(Path.Combine("user://files/", RelativePath)))
		{
            AddEntryAsChild(folderName, Path.Combine(RelativePath, folderName), FakeEntryType.Folder);
		}

        // 添加文件
        foreach (var fileName in DirAccess.GetFilesAt(Path.Combine("user://files/", RelativePath)))
        {
            if (fileName.EndsWith(".txt") || fileName.EndsWith(".md"))
            {
                AddEntryAsChild(fileName, RelativePath, FakeEntryType.Text);
            } else if (fileName.EndsWith(".png") || fileName.EndsWith(".jpg") || fileName.EndsWith(".jpeg") || fileName.EndsWith(".webp"))
            {
                AddEntryAsChild(fileName, RelativePath, FakeEntryType.Image);
            }
        }

        // 这里是在做什么？
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); // TODO fix whatever's causing a race condition :/
        await SortEntriesAsync();
    }

	/// <summary>
	/// 新建文件夹
	/// </summary>
	/// <returns></returns>
	public async Task NewFolderAsync()
	{
		// TODO: 新文件夹名字应该放到全局变量里面，以便做本地化
		// TODO: user://files这种也不应该硬编码
		var newFolderName = "New Folder";

        if (DirAccess.DirExistsAbsolute(Path.Combine("user://files/", RelativePath, newFolderName)))
		{
			for (int i = 2; i < 1000; i++)
			{
				newFolderName = $"New Folder {i}";
				if (!DirAccess.DirExistsAbsolute(Path.Combine("user://files/", RelativePath, newFolderName)))
				{
					break;
				}
			}
		}

		DirAccess.MakeDirAbsolute(Path.Combine("user://files/", RelativePath, newFolderName));

        // 位于根目录，不在任何打开的文件管理器中，直接在桌面新建
        if (string.IsNullOrEmpty(RelativePath))
        {
            AddEntryAsChild(newFolderName, newFolderName, FakeEntryType.Folder);
            await SortEntriesAsync();
        }
        // 位于非根目录，那就是通过某个打开的文件管理器新建的
        else
		{
            foreach (FileManagerWindow fileManager in GetTree().GetNodesInGroup("file_manager_window"))
            {
                if (fileManager.RelativePath == RelativePath)
                {
                    fileManager.AddEntryAsChild(newFolderName, Path.Combine(RelativePath, newFolderName), FakeEntryType.Folder);
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
					await fileManager.SortEntriesAsync();
                }
            }
        }
    }

	/// <summary>
	/// 新建文件
	/// </summary>
	/// <param name="extension">文件扩展名</param>
	/// <param name="fileType">文件类型</param>
	/// <returns></returns>
    public async Task NewFileAsync(string extension, FakeEntryType fileType)
	{
		var newFileName = $"New File{extension}";

		if (FileAccess.FileExists(Path.Combine("user://files/", RelativePath, newFileName)))
		{
			for (var i = 2; i < 1000; i++)
			{
				newFileName = $"New File {i}{extension}";
				if (!FileAccess.FileExists(Path.Combine("user://files/", RelativePath, newFileName)))
				{
					break;
				}
			}
		}

		FileAccess.Open(Path.Combine("user://files/", RelativePath, newFileName), FileAccess.ModeFlags.Write);

        // 位于根目录，不在任何打开的 FileMangerWindow 中，直接在桌面新建
        if (string.IsNullOrEmpty(RelativePath))
		{
			AddEntryAsChild(newFileName, RelativePath, fileType);
			await SortEntriesAsync();
		}
        // 位于非根目录，那就是通过某个打开的 FileManagerWindow 新建的
        else
        {
            foreach (FileManagerWindow fileManager in GetTree().GetNodesInGroup("file_manager_window"))
            {
                if (fileManager.RelativePath == RelativePath)
                {
                    fileManager.AddEntryAsChild(newFileName, RelativePath, fileType);
                    await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
                    await fileManager.SortEntriesAsync();
                }
            }
        }
    }

    /// <summary>
    /// 找到特定名称的文件/文件夹并从文件管理器中移除，但并不删除文件
    /// </summary>
    /// <param name="entryName">文件/文件夹名称</param>
    /// <returns></returns>
	public async Task RemoveEntryWithNameAsync(string entryName)
	{
		foreach (Node child in GetChildren())
		{
			if (child is FakeEntry entry && entry.EntryName == entryName)
			{
				child.QueueFree();
			}
		}

		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await SortEntriesAsync();
	}

    /// <summary>
    /// 将文件/文件夹排序到正确的位置
    /// </summary>
    /// <returns></returns>
    public async Task SortEntriesAsync()
    {
        if (GetChildren().Count < 3)
        {
            UpdatePositions(false);
            return;
        }

        var children = GetChildren().OfType<FakeEntry>().ToList();
        foreach (var child in children)
        {
            RemoveChild(child);
        }

        var sortedChildren = children.OrderBy(x => x.EntryName.ToLower()).OrderBy(x => x.EntryType);

        foreach (var child in sortedChildren)
        {
            AddChild(child);
        }

        // 等待场景树处理完当前帧后才进行 TODO: why?
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        UpdatePositions(false);
    }

    /// <summary>
    /// 将一个文件/文件夹添加为子节点 (原 instantiate_file)
    /// </summary>
    /// <param name="entryName">文件/文件夹名字</param>
    /// <param name="entryPath">文件/文件夹路径</param>
    /// <param name="entryType">文件/文件夹类型</param>
    public void AddEntryAsChild(string entryName, string entryPath, FakeEntryType entryType)
    {
        var entry = (FakeEntry)GD.Load<PackedScene>("res://Scenes/Desktop/entry.tscn").Instantiate();
        entry.EntryName = entryName;
        entry.EntryPath = entryPath;
        entry.EntryType = entryType;
        AddChild(entry);
    }

    #region 上下左右 选择文件/文件夹
    public void SelectFolderUp(FakeEntry currentFolder)
	{
		switch (_direction)
		{
			case "Horizontal":
				SelectPreviousLineFolder(currentFolder);
				break;
			case "Vertical":
				SelectPreviousFolder(currentFolder);
				break;
		}
	}

	public void SelectFolderDown(FakeEntry currentFolder)
	{
        switch (_direction)
        {
            case "Horizontal":
                SelectNextLineFolder(currentFolder);
                break;
            case "Vertical":
                SelectNextFolder(currentFolder);
                break;
        }
    }

    public void SelectFolderLeft(FakeEntry currentFolder)
    {
        switch (_direction)
        {
            case "Horizontal":
                SelectPreviousFolder(currentFolder);
                break;
            case "Vertical":
                SelectPreviousLineFolder(currentFolder);
                break;
        }
    }
    public void SelectFolderRight(FakeEntry currentFolder)
    {
        switch (_direction)
        {
            case "Horizontal":
                SelectNextFolder(currentFolder);
                break;
            case "Vertical":
                SelectNextLineFolder(currentFolder);
                break;
        }
    }

    private void SelectNextFolder(FakeEntry currentFolder)
    {
        var targetIndex = currentFolder.GetIndex() + 1;
        if (targetIndex >= GetChildCount())
        {
            return;
        }
        var nextChild = GetChild(targetIndex);
        if (nextChild is FakeEntry nextFolder)
        {
            currentFolder.HideSelectedHighlight();
            nextFolder.ShowSelectedHighlight();
        }
    }

    private void SelectNextLineFolder(FakeEntry currentFolder)
    {
        var targetIndex = currentFolder.GetIndex() + LineCount;
        if (targetIndex >= GetChildCount())
        {
            return;
        }
        var targetChild = GetChild(targetIndex);
        if (targetChild is FakeEntry targetFolder)
        {
            currentFolder.HideSelectedHighlight();
            targetFolder.ShowSelectedHighlight();
        }
    }

    private void SelectPreviousFolder(FakeEntry currentFolder)
    {
        var targetIndex = currentFolder.GetIndex() - 1;
        if (targetIndex < 0)
        {
            return;
        }
        var previousChild = GetChild(targetIndex);
        if (previousChild is FakeEntry previousFolder)
        {
            currentFolder.HideSelectedHighlight();
            previousFolder.ShowSelectedHighlight();
        }
    }

    private void SelectPreviousLineFolder(FakeEntry currentFolder)
    {
        var targetIndex = currentFolder.GetIndex() - LineCount;
        if (targetIndex < 0)
        {
            return;
        }
        var targetChild = GetChild(targetIndex);
        if (targetChild is FakeEntry targetFolder)
        {
            currentFolder.HideSelectedHighlight();
            targetFolder.ShowSelectedHighlight();
        }
    }
    #endregion
}
