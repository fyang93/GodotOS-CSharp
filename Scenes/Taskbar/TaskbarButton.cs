using Godot;
using System;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// A window's taskbar button. Used to minimize/restore a window.
/// Also shows which window is selected or minimized via colors.
/// </summary>
public partial class TaskbarButton : Control
{
    /// <summary>
    /// 任务栏图标对应的窗口
    /// </summary>
    [Required]
    public FakeWindow TargetWindow { get; set; }

    public Color ActiveColor { get; set; } = new Color("6de700");
    public Color InactiveColor { get; set; } = new Color("908a8c");

    public MarginContainer TextureMargin => GetNode<MarginContainer>("TextureMargin");
    public TextureRect TextureRect => GetNode<TextureRect>("TextureMargin/TextureRect");
    public TextureRect SelectedBackground => GetNode<TextureRect>("SelectedBackground");

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        TargetWindow.Minimized += OnWindowMinimized;
        TargetWindow.Closed += OnWindowClosed;
        TargetWindow.Selected += OnWindowSelected;
        TextureRect.SelfModulate = ActiveColor;
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

    public override async void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.IsPressed())
        {
            if (TargetWindow.IsMinimized)
            {
                TargetWindow.ShowWindow();
            }
            else
            {
                await TargetWindow.HideWindowAsync();
            }
        }
    }

    private void OnMouseEntered()
	{
		TextureMargin.AddThemeConstantOverride("margin_buttom", 7);
		TextureMargin.AddThemeConstantOverride("margin_left", 7);
		TextureMargin.AddThemeConstantOverride("margin_right", 7);
		TextureMargin.AddThemeConstantOverride("margin_top", 7);
    }

	private void OnMouseExited()
	{
        TextureMargin.AddThemeConstantOverride("margin_buttom", 5);
        TextureMargin.AddThemeConstantOverride("margin_left", 5);
        TextureMargin.AddThemeConstantOverride("margin_right", 5);
        TextureMargin.AddThemeConstantOverride("margin_top", 5);
    }

    private void OnWindowMinimized(bool isMinimized)
    {
        var tween = CreateTween()
                    .SetEase(Tween.EaseType.Out)
                    .SetTrans(Tween.TransitionType.Cubic);
        
        if (isMinimized)
        {
            tween.TweenProperty(TextureRect, "self_modulate", InactiveColor, 0.25f);
        }
        else
        {
            tween.TweenProperty(TextureRect, "self_modulate", ActiveColor, 0.25f);
        }
    }

    private void OnWindowClosed()
    {
        QueueFree();
    }

    private void OnWindowSelected(bool selected)
    {
        var tween = CreateTween()
                    .SetEase(Tween.EaseType.Out)
                    .SetTrans(Tween.TransitionType.Cubic);

        if (selected)
        {
            tween.TweenProperty(SelectedBackground, "self_modulate:a", 1f, 0.25f);
        }
        else
        {
            tween.TweenProperty(SelectedBackground, "self_modulate:a", 0f, 0.25f);
        }
    }
}
