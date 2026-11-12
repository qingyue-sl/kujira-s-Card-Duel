using Godot;

namespace KujiraKo;

/// <summary>当前仅有一名角色的最小角色选择页；以后可把角色卡复制为多个选项。</summary>
public partial class MainMenu : Control
{
    [Export] public Button? StartButton { get; set; }

    public override void _Ready()
    {
        if (StartButton is not null) StartButton.Pressed += () => GameFlow.Instance?.BeginNewRun();
    }
}
