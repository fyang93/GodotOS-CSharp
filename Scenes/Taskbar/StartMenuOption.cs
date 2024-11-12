using Godot;
using System;
using System.Threading.Tasks;

public partial class StartMenuOption : Panel
{
	[Export]
	public string GameScene { get; set; }

	[Export]
	public string TitleText { get; set; }

	[Export]
	public string DescriptionText { get; set; }

	[Export]
	public bool SpawnInsideWindow { get; set; } = true;

	[Export]
	public bool UseGenericPauseMenu { get; set; } = true;

	private bool _isMouseOver;
    private Panel _backgroundPanel => GetNode<Panel>("BackgroundPanel");
    private RichTextLabel _menuTitle => GetNode<RichTextLabel>("%MenuTitle");
    private RichTextLabel _menuDescription => GetNode<RichTextLabel>("%MenuDescription");

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
		_backgroundPanel.Visible = false;
		_menuTitle.Text = $"[center]{TitleText}";
		_menuDescription.Text = $"[center]{DescriptionText}";

        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.IsPressed())
        {
            if (SpawnInsideWindow)
            {
                SpawnWindow();
            }
            else
            {
                SpawnOutsideWindow();
            }
        }
    }

    private void OnMouseEntered()
    {
        _isMouseOver = true;
        GetNode<Control>("BackgroundPanel").Visible = true;
        var tween = CreateTween();
        tween.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(_backgroundPanel, "modulate:a", 1, 0.2f);
    }

    private async void OnMouseExited()
    {
        _isMouseOver = false;
        var tween = CreateTween();
        tween.SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(_backgroundPanel, "modulate:a", 0, 0.2f);
        await ToSignal(tween, Tween.SignalName.Finished);
        if (!_isMouseOver)
            _backgroundPanel.Visible = false;
    }

    /// <summary>
    /// 在单独的窗口内实例化游戏场景，并在任务栏生成图标
    /// </summary>
    private void SpawnWindow()
    {
        var window = GD.Load<PackedScene>("res://Scenes/Window/GameWindow/game_window.tscn").Instantiate<FakeWindow>();
        window.TitleText = _menuTitle.Text;

        var gameScene = GD.Load<PackedScene>(GameScene).Instantiate();
        window.GetNode("%GameWindow").AddChild(gameScene);

        if (UseGenericPauseMenu)
        {
            window.GetNode("%GamePauseManager").ProcessMode = ProcessModeEnum.Inherit;
        }
        GetTree().CurrentScene.AddChild(window);

        var taskbarButton = GD.Load<PackedScene>("res://Scenes/Taskbar/taskbar_button.tscn").Instantiate<TaskbarButton>();
        taskbarButton.TargetWindow = window;
        taskbarButton.GetNode<TextureRect>("TextureMargin/TextureRect").Texture = GetNode<TextureRect>("HBoxContainer/MarginContainer/TextureRect").Texture;
        taskbarButton.ActiveColor = GetNode<TextureRect>("HBoxContainer/MarginContainer/TextureRect").Modulate;
        GetTree().GetFirstNodeInGroup("taskbar_buttons").AddChild(taskbarButton);
    }

    /// <summary>
    /// 将游戏场景直接实例化到根控件的子节点中，会直接全屏但是无法退出游戏了
    /// </summary>
    private void SpawnOutsideWindow()
    {
        GetNode("/root/Control").AddChild(GD.Load<PackedScene>(GameScene).Instantiate());
    }
}
