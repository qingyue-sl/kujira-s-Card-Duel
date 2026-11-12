using System.Collections.Generic;
using Godot;

namespace KujiraKo;

/// <summary>胜利结算页：选卡或跳过后，由玩家点击“继续冒险”进入下一节点。</summary>
public partial class RewardScreen : Control
{
    [Export] public Battle? Battle { get; set; }
    [Export] public Godot.Collections.Array<CardData> RewardPool { get; set; } = new();
    [Export] public Label? TitleLabel { get; set; }
    [Export] public Label? ResultLabel { get; set; }
    [Export] public Control? ChoicesContainer { get; set; }
    [Export] public Button? FirstChoiceButton { get; set; }
    [Export] public Button? SecondChoiceButton { get; set; }
    [Export] public Button? ThirdChoiceButton { get; set; }
    [Export] public Button? SkipButton { get; set; }
    [Export] public Button? ContinueButton { get; set; }

    private readonly List<CardData> _choices = new();
    private bool _resolved;

    public override void _Ready()
    {
        if (FirstChoiceButton is not null) FirstChoiceButton.Pressed += () => Choose(0);
        if (SecondChoiceButton is not null) SecondChoiceButton.Pressed += () => Choose(1);
        if (ThirdChoiceButton is not null) ThirdChoiceButton.Pressed += () => Choose(2);
        if (SkipButton is not null) SkipButton.Pressed += Skip;
        if (ContinueButton is not null) ContinueButton.Pressed += Continue;
        if (Battle is not null) Battle.BattleEnded += OnBattleEnded;
        Hide();
    }

    private void OnBattleEnded(bool victory)
    {
        if (!victory || Battle is null) return;
        _resolved = false;
        var isBoss = Battle.CurrentEncounter?.Kind == EncounterKind.Boss;
        if (TitleLabel is not null) TitleLabel.Text = isBoss ? "Boss 已被击败" : "战斗胜利 · 选择一张新卡";
        if (ResultLabel is not null) ResultLabel.Text = isBoss ? "本次冒险完成。确认后查看结算。" : "选择一张奖励卡，或跳过本次奖励。";
        if (ContinueButton is not null) ContinueButton.Visible = false;

        if (isBoss)
        {
            if (ChoicesContainer is not null) ChoicesContainer.Visible = false;
            if (SkipButton is not null) SkipButton.Visible = false;
            _resolved = true;
            if (ContinueButton is not null) ContinueButton.Visible = true;
            Show();
            return;
        }

        if (ChoicesContainer is not null) ChoicesContainer.Visible = true;
        if (SkipButton is not null) SkipButton.Visible = true;
        ShowRandomRewards();
        Show();
    }

    private void ShowRandomRewards()
    {
        _choices.Clear();
        var pool = RewardPool.Count > 0 ? RewardPool : DemoContentFactory.CreateRewardPool();
        var candidates = new List<CardData>(pool);
        var random = new RandomNumberGenerator();
        random.Randomize();
        while (_choices.Count < 3 && candidates.Count > 0)
        {
            var index = random.RandiRange(0, candidates.Count - 1);
            _choices.Add(candidates[index]);
            candidates.RemoveAt(index);
        }
        SetButton(FirstChoiceButton, 0);
        SetButton(SecondChoiceButton, 1);
        SetButton(ThirdChoiceButton, 2);
    }

    private void SetButton(Button? button, int index)
    {
        if (button is null) return;
        button.Visible = index < _choices.Count;
        if (index < _choices.Count)
        {
            var card = _choices[index];
            button.Text = $"{card.DisplayName}（{card.Cost}）\n{card.Description}";
        }
    }

    private void Choose(int index)
    {
        if (_resolved || Battle is null || index < 0 || index >= _choices.Count) return;
        Battle.AddCardToRunDeck(_choices[index]);
        ResolveChoice($"获得「{_choices[index].DisplayName}」。");
    }

    private void Skip()
    {
        if (_resolved) return;
        ResolveChoice("已跳过本次奖励。");
    }

    private void ResolveChoice(string result)
    {
        _resolved = true;
        if (ResultLabel is not null) ResultLabel.Text = result;
        if (ChoicesContainer is not null) ChoicesContainer.Visible = false;
        if (SkipButton is not null) SkipButton.Visible = false;
        if (ContinueButton is not null) ContinueButton.Visible = true;
    }

    private void Continue()
    {
        if (!_resolved) return;
        Battle?.ContinueAfterVictory();
    }
}
