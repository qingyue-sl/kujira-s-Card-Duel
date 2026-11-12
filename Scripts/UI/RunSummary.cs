using Godot;

namespace KujiraKo;

/// <summary>一局 Boss 胜利或战败后的最小结算页。</summary>
public partial class RunSummary : Control
{
    [Export] public Label? TitleLabel { get; set; }
    [Export] public Label? DetailLabel { get; set; }
    [Export] public Button? RetryButton { get; set; }
    [Export] public Button? MainMenuButton { get; set; }

    public override void _Ready()
    {
        var flow = GameFlow.Instance;
        if (flow is not null)
        {
            if (TitleLabel is not null) TitleLabel.Text = flow.LastRunWon ? "冒险完成" : "冒险失败";
            if (DetailLabel is not null)
            {
                var run = flow.ActiveRun;
                DetailLabel.Text = $"完成战斗：{flow.LastCompletedCombatCount}\n最终生命：{run?.CurrentHealth ?? 0}/{run?.MaxHealth ?? 60}\n最终牌组：{run?.Deck.Count ?? 0} 张";
            }
        }
        if (RetryButton is not null) RetryButton.Pressed += () => GameFlow.Instance?.BeginNewRun();
        if (MainMenuButton is not null) MainMenuButton.Pressed += () => GameFlow.Instance?.ReturnToMainMenu();
    }
}
