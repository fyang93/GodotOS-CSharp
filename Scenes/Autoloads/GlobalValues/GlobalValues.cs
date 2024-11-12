using Godot;
using System;

public partial class GlobalValues : Node
{
    public static GlobalValues Instance;

    /// <summary>
    /// 当前选中的窗口
    /// </summary>
    public FakeWindow SelectedWindow { get; set; }


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        Instance = this;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
