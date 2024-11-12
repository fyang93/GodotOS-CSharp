using Godot;
using System;
using System.Threading.Tasks;

public partial class FakeWindow : Panel
{
	public static int NumOfWindows;

    public string TitleText { get; set; }

	private bool _isDragging;
	private Vector2 _startDragPosition;
	private Vector2 _mouseStartDragPosition;

	public bool IsMinimized;
	public bool IsMaximized;
    public bool IsSelected;
    public bool IsClosing;

    private CompressedTexture2D _maximizeIcon = GD.Load<CompressedTexture2D>("res://Art/Icons/expand.png");
	private CompressedTexture2D _unmaximizeIcon = GD.Load<CompressedTexture2D>("res://Art/Icons/shrink.png");
	private Vector2 _oldUnmaximizedPosition;
	private Vector2 _oldUnmaximizedSize;

	private float _startBgColorAlpha;

    // child nodes
    private Panel _topBar => GetNode<Panel>("TopBar");
    private RichTextLabel _titleText => GetNode<RichTextLabel>("%TitleText");
    private Button _minimizeButton => GetNode<Button>("%MinimizeButton");
    private Button _maximizeButton => GetNode<Button>("%MaximizeButton");
    private Button _closeButton => GetNode<Button>("%CloseButton");
    private ResizeDragSpot _resizeDragSpot => GetNode<ResizeDragSpot>("ResizeDragSpot");


    [Signal]
	public delegate void MinimizedEventHandler(bool isMinimized);

    [Signal]
    public delegate void MaximizedEventHandler(bool isMaximized);

    [Signal]
	public delegate void SelectedEventHandler(bool isSelected);

	[Signal]
	public delegate void ClosedEventHandler();


    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
		// Duplicate theme override so values can be set without affecting other windows
		Set("theme_override_styles/panel", ((Resource)Get("theme_override_styles/panel")).Duplicate());
		_topBar.Set("theme_override_styles/panel", ((Resource)_topBar.Get("theme_override_styles/panel")).Duplicate());
		_startBgColorAlpha = ((Color)((StyleBox)Get("theme_override_styles/panel")).Get("bg_color")).A;

		NumOfWindows += 1;
		SelectWindow(false);

        _titleText.Text = string.Join(" ", TitleText.Split('\n'));
        GetViewport().SizeChanged += OnViewportSizeChanged;

        Modulate = Modulate with { A = 0 };
        var tween = CreateTween();
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(this, "modulate:a", 1, 0.5f);
        
		// 设置事件绑定
		_topBar.GuiInput += OnTopBarGuiInput;
		_minimizeButton.Pressed += async () => await MinimizeWindowAsync();
		_maximizeButton.Pressed += async () => await MaximizeWindowAsync();
		_closeButton.Pressed += async () => await CloseWindowAsync();
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
	{
		if (_isDragging)
		{
			GlobalPosition = _startDragPosition + (GetGlobalMousePosition() - _mouseStartDragPosition);
			ClampWindowInsideViewport();
		}
	}

    public override void _GuiInput(InputEvent @event)
    {
		if (@event is InputEventMouseButton buttonEvent && buttonEvent.IsPressed() &&
            (buttonEvent.ButtonIndex == MouseButton.Left || buttonEvent.ButtonIndex == MouseButton.Right))
		{
			SelectWindow(true);
		}
    }

    #region 最大化 / 最小化 / 关闭 窗口

    public async Task MinimizeWindowAsync()
    {
        await HideWindowAsync();
    }

    public async Task MaximizeWindowAsync()
    {
        // 恢复最大化之前的位置
        if (IsMaximized)
        {
            IsMaximized = !IsMaximized;
            _maximizeButton.Icon = _maximizeIcon;

            var tween = CreateTween()
                .SetParallel(true)
                .SetTrans(Tween.TransitionType.Quart)
                .SetEase(Tween.EaseType.Out);
            tween.TweenProperty(this, "global_position", _oldUnmaximizedPosition, 0.25f);
            tween.TweenProperty(this, "size", _oldUnmaximizedSize, 0.25f);
            tween.TweenProperty((Resource)Get("theme_override_styles/panel"), "bg_color:a", _startBgColorAlpha, 0.25f);
            await ToSignal(tween, Tween.SignalName.Finished);

            ((StyleBoxFlat)Get("theme_override_styles/panel")).SetCornerRadiusAll(5);
            ((StyleBoxFlat)_topBar.Get("theme_override_styles/panel")).Set("corner_radius_top_left", 5);
            ((StyleBoxFlat)_topBar.Get("theme_override_styles/panel")).Set("corner_radius_top_right", 5);

            _resizeDragSpot.EmitSignal(ResizeDragSpot.SignalName.WindowResized);
        }
        // 最大化窗口
        else
        {
            IsMaximized = !IsMaximized;
            _maximizeButton.Icon = _unmaximizeIcon;

            _oldUnmaximizedPosition = GlobalPosition;
            _oldUnmaximizedSize = Size;

            var newSize = GetViewportRect().Size;
            newSize.Y -= 40; // because taskbar

            var tween = CreateTween()
                .SetParallel(true)
                .SetTrans(Tween.TransitionType.Quart)
                .SetEase(Tween.EaseType.Out);
            tween.TweenProperty(this, "global_position", Vector2.Zero, 0.25f);
            tween.TweenProperty(this, "size", newSize, 0.25f);
            tween.TweenProperty((Resource)Get("theme_override_styles/panel"), "bg_color:a", 1, 0.25f);
            await ToSignal(tween, Tween.SignalName.Finished);

            ((StyleBoxFlat)Get("theme_override_styles/panel")).SetCornerRadiusAll(0);
            ((StyleBoxFlat)_topBar.Get("theme_override_styles/panel")).Set("corner_radius_top_left", 0);
            ((StyleBoxFlat)_topBar.Get("theme_override_styles/panel")).Set("corner_radius_top_right", 0);

            _resizeDragSpot.EmitSignal(ResizeDragSpot.SignalName.WindowResized);
        }
    }


    public async Task CloseWindowAsync()
    {
        if (IsClosing) return;

        if (GlobalValues.Instance.SelectedWindow == this)
        {
            GlobalValues.Instance.SelectedWindow = null;
        }

        EmitSignal(SignalName.Closed);
        NumOfWindows -= 1;
        IsClosing = true;
        var tween = CreateTween();
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(this, "modulate:a", 0, 0.25f);
        await ToSignal(tween, Tween.SignalName.Finished);
        QueueFree();
    }
    #endregion

    public void ClampWindowInsideViewport()
    {
        var gameWindowSize = GetViewportRect().Size;
        if (Size.Y > gameWindowSize.Y - 40)
        {
            Size = new Vector2(Size.Y, gameWindowSize.Y - 40);
        }
        if (Size.X > gameWindowSize.X)
        {
            Size = new Vector2(gameWindowSize.X, Size.Y);
        }

        GlobalPosition = new Vector2(
            Mathf.Clamp(GlobalPosition.X, 0, gameWindowSize.X - Size.X),
            Mathf.Clamp(GlobalPosition.Y, 0, gameWindowSize.Y - Size.Y - 40)
        );
    }

	public void ShowWindow()
	{
		if (!IsMinimized) return;

		SelectWindow(false);

		IsMinimized = false;
		EmitSignal(SignalName.Minimized, IsMinimized);

		Visible = true;
		var tween = CreateTween();
		tween.SetTrans(Tween.TransitionType.Cubic);
		tween.SetEase(Tween.EaseType.Out);
		tween.SetParallel(true);
		tween.TweenProperty(this, "position:y", Position.Y - 20, 0.25f);
		tween.TweenProperty(this, "modulate:a", 1, 0.25f);
    }

    public async Task HideWindowAsync()
    {
        if (IsMinimized) return;

        DeselectWindow();
        IsMinimized = true;
        EmitSignal(SignalName.Minimized, IsMinimized);

        Visible = true;
        var tween = CreateTween();
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.SetEase(Tween.EaseType.Out);
        tween.SetParallel(true);
        tween.TweenProperty(this, "position:y", Position.Y + 20, 0.25f);
        tween.TweenProperty(this, "modulate:a", 0, 0.25f);
        await ToSignal(tween, Tween.SignalName.Finished);

        if (!IsSelected) // ??
        {
            Visible = false;
        }
    }

    private void SelectWindow(bool playFadeAnimation)
	{
		if (IsSelected) return;

		IsSelected = true;
		EmitSignal(SignalName.Selected, IsSelected);
		GlobalValues.Instance.SelectedWindow = this;

		var tween = CreateTween();
		tween.SetParallel(true);
		tween.SetTrans(Tween.TransitionType.Cubic);
        tween.TweenProperty(GetNode<RichTextLabel>("TopBar/TitleText"), "modulate", new Color(0.24f, 1f, 1f), 0.25f);
        tween.TweenProperty((Resource)Get("theme_override_styles/panel"), "shadow_size", 20, 0.25f);
        if (playFadeAnimation)
		{
			tween.TweenProperty(this, "modulate:a", 1, 0.1f);
		}

		// Move in front of all other windows (+2 to ignore wallpaper and bg color)
        GetParent().MoveChild(this, NumOfWindows + 2);
		DeselectOtherWindows();
    }

	private void DeselectWindow()
	{
		if (!IsSelected) return;

		IsSelected = false;
		EmitSignal(SignalName.Selected, IsSelected);

		var tween = CreateTween();
		tween.SetParallel(true);
        tween.SetTrans(Tween.TransitionType.Cubic);
		tween.TweenProperty(this, "modulate:a", 0.75f, 0.25f);
        tween.TweenProperty(GetNode<RichTextLabel>("TopBar/TitleText"), "modulate", new Color(1, 1, 1), 0.25f);
        tween.TweenProperty((Resource)Get("theme_override_styles/panel"), "shadow_size", 0, 0.25f);
    }

    private void DeselectOtherWindows()
	{
		foreach (var window in GetTree().GetNodesInGroup("window"))
		{
			if (window == this)
				continue;

			(window as FakeWindow).DeselectWindow();
		}
	}

	private void OnViewportSizeChanged()
	{
		if (IsMaximized)
		{
			var newSize = GetViewportRect().Size;
			newSize.Y -= 40; // because taskbar, TODO: 把这个放到全局常量里面
			GlobalPosition = Vector2.Zero;
			Size = newSize;
		}

		ClampWindowInsideViewport();
	}

    private void OnTopBarGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton buttonEvent && buttonEvent.ButtonIndex == MouseButton.Left)
        {
            if (buttonEvent.IsPressed())
            {
                _isDragging = true;
                _startDragPosition = GlobalPosition;
                _mouseStartDragPosition = GetGlobalMousePosition();
            }
            else
            {
                _isDragging = false;
            }
        }
    }
}
