using Godot;
using System;

/// <summary>
/// Smoothly tweens all children into place. Used in file managers
/// </summary>
public partial class SmoothContainer : Control
{
	[Export(PropertyHint.Enum, "Horizontal,Vertical")]
	protected string _direction = "Horizontal";

	// How often the update function runs, in seconds. Low values are performance intensive!
	[Export]
	protected float _pollRate = 0.15f;

	[Export]
	protected float _animationSpeed = 0.5f;

	[ExportGroup("Spacing")]
	[Export]
	protected int _horizontalSpacing = 10;

	[Export]
	protected int _verticalSpacing = 10;

	[ExportGroup("Margin")]
	[Export]
	protected int _leftMargin;
	[Export]
    protected int _upMargin;
	[Export]
    protected int _rightMargin;
    [Export]
	protected int _downMargin;

	// Global Tween so it doesn't create one each time the function runs
	private Tween _tween;

	// Bool used to check if there's a cooldown or not
	private bool _justUpdated;

    // Global Vector2 to calculate the next position of each container child
    private Vector2 _nextPosition;

    // 容器初始最小尺寸
    private Vector2 _startMinSize;

    /// <summary>
    /// 一行最多有多少个文件/文件夹
    /// </summary>
    public int LineCount { get; set; }

    // Called when the node enters the scene tree for the first time.
    public override async void _Ready()
	{
        // 当节点的父级是一个容器时，在场景刚加载的第一帧里，该节点的尺寸会被强制设置为 (0, 0)。等待一帧，确保容器已经完成布局调整
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

		_startMinSize = CustomMinimumSize;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
	{
	}

    /// <summary>
    /// 更新元素位置
    /// </summary>
    /// <param name="isCooldownRequired">启动冷却机制，确保位置更新后不会立即重复更新</param>
    public void UpdatePositions(bool isCooldownRequired = true)
	{
		if (_justUpdated) return;

		if (isCooldownRequired) StartCooldown();

		_nextPosition = new(_leftMargin, _upMargin);
		_tween?.Kill();

		switch(_direction)
		{
			case "Horizontal":
				UpdateHorizontalDirection();
				break;

			case "Vertical":
				UpdateVerticalDirection();
				break;
        }
    }

	/// <summary>
	/// 横向更新子元素位置，主要用于文件管理器
	/// 
	/// </summary>
    private void UpdateHorizontalDirection()
	{
		var newLineCount = 0;
		LineCount = 0;
		var tallestChild = 0f;

		foreach (Node child in GetChildren())
		{
			if (child is not Control control) continue;

			// 判断是否已经超出当前行，超出则换行
			if (_nextPosition.X + _rightMargin + control.Size.X > Size.X)
			{
				_nextPosition.X = _leftMargin;
				_nextPosition.Y += tallestChild + _verticalSpacing;
				tallestChild = 0;

				LineCount = newLineCount;
				newLineCount = 0;
			}

			if (control.Position != _nextPosition)
			{
				if (_tween == null || !_tween.IsRunning())
					RestoreTween();
				_tween.TweenProperty(control, "position", _nextPosition, _animationSpeed);
            }

			tallestChild = Mathf.Max(tallestChild, control.Size.Y);
			_nextPosition.X += control.Size.X + _horizontalSpacing;
			newLineCount++;
		}

		LineCount = LineCount == 0 ? newLineCount : LineCount;

		if (GetParent() is ScrollContainer scrollContainer)
		{
			if (_nextPosition.Y + tallestChild > scrollContainer.Size.Y)
			{
				CustomMinimumSize = CustomMinimumSize with
				{
					Y = _nextPosition.Y + tallestChild + _downMargin
				};
			}
			else
			{
				CustomMinimumSize = CustomMinimumSize with
				{
					Y = _startMinSize.Y
				};
			}
		}
	}

	/// <summary>
	/// 纵向更新子元素位置，主要用于桌面
	/// </summary>
	private void UpdateVerticalDirection()
	{
        var newLineCount = 0;
        LineCount = 0;
        var longestChild = 0f;

        foreach (Node child in GetChildren())
        {
            if (child is not Control control) continue;

            if (_nextPosition.Y + _downMargin + control.Size.Y > Size.Y)
            {
                _nextPosition.Y = _upMargin;
                _nextPosition.X += longestChild + _horizontalSpacing;
                longestChild = 0;

                LineCount = newLineCount;
                newLineCount = 0;
            }

            if (control.Position != _nextPosition)
            {
                if (_tween == null || !_tween.IsRunning())
                    RestoreTween();
                _tween.TweenProperty(control, "position", _nextPosition, _animationSpeed);
            }

            longestChild = Mathf.Max(longestChild, control.Size.X);
            _nextPosition.Y += control.Size.Y + _verticalSpacing;
            newLineCount++;
        }

        LineCount = LineCount == 0 ? newLineCount : LineCount;

        if (GetParent() is ScrollContainer scrollContainer)
        {
            if (_nextPosition.X + longestChild > scrollContainer.Size.X)
			{
				CustomMinimumSize = CustomMinimumSize with
				{
					X = _nextPosition.X + longestChild + _rightMargin
				};
            }
            else
			{
				CustomMinimumSize = CustomMinimumSize with
				{
					X = _startMinSize.X
				};
			}
        }
    }

    /// <summary>
    /// 控制更新频率，防止 UpdatePositions 函数在每一帧都被调用
    /// </summary>
    private async void StartCooldown()
	{
		_justUpdated = true;
		await ToSignal(GetTree().CreateTimer(_pollRate), Timer.SignalName.Timeout);
		_justUpdated = false;
		UpdatePositions(false);
	}

	private void RestoreTween()
	{
		_tween = CreateTween();
		_tween.SetParallel(true);
		_tween.SetTrans(Tween.TransitionType.Cubic);
		_tween.SetEase(Tween.EaseType.Out);
	}
}
