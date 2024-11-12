using Godot;
using System;
using System.IO;
using System.Runtime.Versioning;
using System.Threading.Tasks;

/// <summary>
/// Managed copying and pasting of files and folders.
/// </summary>
public partial class CopyPasteManager : Node
{
    public static CopyPasteManager Instance;

    /// <summary>
    /// The target entry. Not used for variables since it could be freed by a file manager window!
    /// </summary>
    private FakeEntry _targetEntry;

    private enum StateEnum { Copy, Cut }

    private StateEnum _state = StateEnum.Copy;

    /// <summary>
    /// The target entry's name. Gets emptied after a paste.
    /// </summary>
    public string TargetEntryName { get; set; }

	public string TargetEntryPath { get; set; }

	public FakeEntryType TargetEntryType { get; set; }

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        Instance = this;

        // Connect the files_dropped signal from the viewport to the handler
        ((Window)GetViewport()).FilesDropped += OnFilesDropped;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_paste"))
        {
            var selectedWindow = GlobalValues.Instance.SelectedWindow;
            // Paste in desktop if no selected window. Paste in file manager if file manager is selected.
            if (selectedWindow == null)
            {
                PasteEntry(string.Empty);
                return;
            }

            var fileManagerWindow = selectedWindow.GetNodeOrNull<FileManagerWindow>("%FileManagerWindow");
            if (selectedWindow != null && fileManagerWindow != null)
            {
                PasteEntry(fileManagerWindow.RelativePath);
            }
        }
    }

    public void CopyEntry(FakeEntry entry)
    {
        if (_targetEntry != null)
        {
            _targetEntry.Modulate = _targetEntry.Modulate with { A = 1 };
        }

        _targetEntry = entry;
        TargetEntryName = entry.EntryName;
        TargetEntryPath = entry.EntryPath;
        TargetEntryType = entry.EntryType;
        entry.Modulate = entry.Modulate with { A = 0.8f };
        _state = StateEnum.Copy;

        NotificationManager.Instance.SpawnNotification($"Copied [color=59ea90][wave freq=7]{TargetEntryName}[/wave][/color]");
    }

    public void CutEntry(FakeEntry entry)
    {
        if (_targetEntry != null)
        {
            _targetEntry.Modulate = _targetEntry.Modulate with { A = 1 };
        }

        _targetEntry = entry;
        _targetEntry.Modulate = _targetEntry.Modulate with { A = 0.8f };

        TargetEntryName = entry.EntryName;
        TargetEntryPath = entry.EntryPath;
        TargetEntryType = entry.EntryType;
        _state = StateEnum.Cut;

        NotificationManager.Instance.SpawnNotification($"Cutting [color=59ea90][wave freq=7]{TargetEntryName}[/wave][/color]");
    }

    /// <summary>
    /// Pastes the folder, calling PasteFolderCopy() or PasteFolderCut() depending on the state selected.
    /// </summary>
    /// <param name="toPath">The destination path where the folder will be pasted.</param>
    public void PasteEntry(string toPath)
    {
        if (string.IsNullOrEmpty(TargetEntryName))
        {
            NotificationManager.Instance.SpawnNotification("Error: Nothing to copy");
            return;
        }

        switch (_state)
        {
            case StateEnum.Copy:
                PasteEntryCopyAsync(toPath);
                break;
            case StateEnum.Cut:
                PasteEntryCutAsync(toPath);
                break;
        }
    }

    private async void PasteEntryCopyAsync(string toPath)
    {
        var to = Path.Combine("user://files/", toPath, TargetEntryName);
        if (TargetEntryType == FakeEntryType.Folder)
        {
            var from = $"user://files/{TargetEntryPath}";
            if (from != to)
            {
                DirAccess.MakeDirAbsolute(to);
                CopyDirectoryRecursively(from, to);
            }
        }
        else
        {
            var from = Path.Combine("user://files/", TargetEntryPath, TargetEntryName);
            if (from != to)
            {
                DirAccess.CopyAbsolute(from, to);
            }
        }

        if (_targetEntry != null)
        {
            _targetEntry.Modulate = _targetEntry.Modulate with { A = 1 };
        }

        if (string.IsNullOrEmpty(toPath))
        {
            var desktopFileManager = GetTree().GetFirstNodeInGroup("desktop_file_manager") as DesktopFileManager;
            await desktopFileManager.RemoveEntryWithNameAsync(TargetEntryName);
            await InstantiateFileAndSortAsync(desktopFileManager, toPath);
        }
        else
        {
            foreach (FileManagerWindow fileManager in GetTree().GetNodesInGroup("file_manager_window"))
            {
                if (fileManager.RelativePath == toPath)
                {
                    await fileManager.RemoveEntryWithNameAsync(TargetEntryName);
                    await InstantiateFileAndSortAsync(fileManager, toPath);
                }
            }
        }

        TargetEntryName = "";
        _targetEntry = null;
    }

    private async void PasteEntryCutAsync(string toPath)
    {
        var to = Path.Combine("user://files/", toPath, TargetEntryName);
        if (TargetEntryType == FakeEntryType.Folder)
        {
            var from = $"user://files/{TargetEntryPath}";
            DirAccess.RenameAbsolute(from, to);

            foreach (FileManagerWindow fileManager in GetTree().GetNodesInGroup("file_manager_window"))
            {
                if (fileManager.RelativePath.StartsWith(TargetEntryPath))
                {
                    await fileManager.CloseWindowAsync();
                }
                else if (fileManager.RelativePath == toPath)
                {
                    await InstantiateFileAndSortAsync(fileManager, toPath);
                }
            }
        }
        else
        {
            var from = Path.Combine("user://files/", TargetEntryPath, TargetEntryName);
            DirAccess.RenameAbsolute(from, to);

            foreach (FileManagerWindow fileManager in GetTree().GetNodesInGroup("file_manager_window"))
            {
                if (fileManager.RelativePath == toPath)
                {
                    await InstantiateFileAndSortAsync(fileManager, toPath);
                }
            }
        }

        await _targetEntry?.GetParent<BaseFileManager>()?.RemoveEntryWithNameAsync(TargetEntryName);

        if (string.IsNullOrEmpty(toPath))
        {
            DesktopFileManager desktopFileManager = GetTree().GetFirstNodeInGroup("desktop_file_manager") as DesktopFileManager;
            await InstantiateFileAndSortAsync(desktopFileManager, toPath);
        }

        TargetEntryName = "";
        _targetEntry = null;
    }

    private void CopyDirectoryRecursively(string dirPath, string toPath)
    {
        if (toPath.StartsWith(dirPath))
        {
            NotificationManager.Instance.SpawnNotification("ERROR: Can't copy a folder into itself!");
            return;
        }

        foreach (string dirName in DirAccess.GetDirectoriesAt(dirPath))
        {
            string newDirPath = $"{toPath}/{dirName}";
            DirAccess.MakeDirAbsolute(newDirPath);
            CopyDirectoryRecursively($"{dirPath}/{dirName}", newDirPath);
        }

        foreach (string fileName in DirAccess.GetFilesAt(dirPath))
        {
            string from = Path.Combine(dirPath, fileName);
            string to = Path.Combine(toPath, fileName);
            DirAccess.CopyAbsolute(from, to);
        }
    }

    /// <summary>
    /// Instantiates a new file in the file manager then refreshes. Used for adding a single file without causing a full refresh.
    /// </summary>
    /// <param name="fileManager">The file manager where the file will be instantiated.</param>
    /// <param name="toPath">The path where the file will be added.</param>
    private async Task InstantiateFileAndSortAsync(BaseFileManager fileManager, string toPath)
    {
        if (TargetEntryType == FakeEntryType.Folder)
        {
            fileManager.AddEntryAsChild(TargetEntryName, $"{toPath}/{TargetEntryName}", TargetEntryType);
        }
        else
        {
            fileManager.AddEntryAsChild(TargetEntryName, toPath, TargetEntryType);
        }

        await fileManager.SortEntriesAsync();
    }

    /// <summary>
    /// Copies files that get dragged and dropped into GodotOS (if the file format is supported).
    /// </summary>
    /// <param name="files">Array of file paths that were dropped.</param>
    private void OnFilesDropped(string[] files)
    {
        foreach (var fileName in files)
        {
            var extension = Path.GetExtension(fileName).ToLower();

            switch (extension)
            {
                case ".txt":
                case ".md":
                case ".jpg":
                case ".jpeg":
                case ".png":
                case ".webp":
                    var newFileName = Path.GetFileNameWithoutExtension(fileName);
                    var destinationPath = $"user://files/{newFileName}";
                    DirAccess.CopyAbsolute(fileName, destinationPath);

                    var desktopFileManager = GetTree().GetFirstNodeInGroup("desktop_file_manager") as DesktopFileManager;
                    desktopFileManager.PopulateFileManager();
                    break;
                default:
                    // Unsupported file format; handle accordingly if needed
                    break;
            }
        }
    }
}
