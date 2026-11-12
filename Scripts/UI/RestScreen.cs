using System.Collections.Generic;
using Godot;

namespace KujiraKo;

/// <summary>休整页：恢复最大生命的 25%，并允许移除一张牌或跳过移除。</summary>
public partial class RestScreen : Control
{
    [Export] public Label? HealthLabel { get; set; }
    [Export] public Label? MessageLabel { get; set; }
    [Export] public Label? HealFloatLabel { get; set; }
    [Export] public Button? HealButton { get; set; }
    [Export] public Container? DeckContainer { get; set; }
    [Export] public Button? RemoveButton { get; set; }
    [Export] public Button? SkipRemoveButton { get; set; }
    [Export] public Button? ContinueButton { get; set; }

    private int _selectedIndex = -1;
    private bool _healed;
    private bool _removeResolved;

    public override void _Ready()
    {
        if (HealButton is not null) HealButton.Pressed += Heal;
        if (RemoveButton is not null) RemoveButton.Pressed += RemoveSelected;
        if (SkipRemoveButton is not null) SkipRemoveButton.Pressed += SkipRemoval;
        if (ContinueButton is not null) ContinueButton.Pressed += () => GameFlow.Instance?.ContinueAfterRest();
        if (HealFloatLabel is not null) HealFloatLabel.Visible = false;
        BuildDeckButtons();
        Refresh();
    }

    private void Heal()
    {
        var run = GameFlow.Instance?.ActiveRun;
        if (run is null || _healed) return;
        var healed = run.HealPercent(0.25f);
        _healed = true;
        if (HealButton is not null) HealButton.Disabled = true;
        if (MessageLabel is not null) MessageLabel.Text = healed > 0 ? "潮汐的回响治愈了你。" : "生命已满，无法再恢复。";
        PlayHealFloat(healed);
        Refresh();
    }

    private void BuildDeckButtons()
    {
        var run = GameFlow.Instance?.ActiveRun;
        if (DeckContainer is null || run is null) return;
        foreach (var child in DeckContainer.GetChildren()) child.QueueFree();
        for (var i = 0; i < run.Deck.Count; i++)
        {
            var index = i;
            var card = run.Deck[index];
            var button = new Button { Text = $"{card.DisplayName}  ·  {card.Cost}费", CustomMinimumSize = new Vector2(165, 46) };
            button.Pressed += () => SelectCard(index);
            DeckContainer.AddChild(button);
        }
    }

    private void SelectCard(int index)
    {
        if (_removeResolved) return;
        _selectedIndex = index;
        if (MessageLabel is not null) MessageLabel.Text = $"已选择移除「{GameFlow.Instance?.ActiveRun?.Deck[index].DisplayName}」。";
        if (RemoveButton is not null) RemoveButton.Disabled = false;
    }

    private void RemoveSelected()
    {
        var run = GameFlow.Instance?.ActiveRun;
        if (run is null || _removeResolved || _selectedIndex < 0 || _selectedIndex >= run.Deck.Count) return;
        var removed = run.Deck[_selectedIndex];
        run.Deck.RemoveAt(_selectedIndex);
        _removeResolved = true;
        if (MessageLabel is not null) MessageLabel.Text = $"已移除「{removed.DisplayName}」。";
        BuildDeckButtons();
        Refresh();
    }

    private void SkipRemoval()
    {
        if (_removeResolved) return;
        _removeResolved = true;
        if (MessageLabel is not null) MessageLabel.Text = "已跳过移除卡牌。";
        Refresh();
    }

    private void PlayHealFloat(int amount)
    {
        if (HealFloatLabel is null || amount <= 0) return;
        HealFloatLabel.Text = $"+{amount}";
        HealFloatLabel.Modulate = new Color(0.38f, 1f, 0.56f, 1f);
        HealFloatLabel.Position = new Vector2(600, 228);
        HealFloatLabel.Visible = true;
        var tween = HealFloatLabel.CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(HealFloatLabel, "position:y", 180f, 0.8f);
        tween.TweenProperty(HealFloatLabel, "modulate:a", 0f, 0.8f);
        tween.SetParallel(false);
        tween.TweenCallback(Callable.From(() => HealFloatLabel.Visible = false));
    }

    private void Refresh()
    {
        var run = GameFlow.Instance?.ActiveRun;
        if (run is null) return;
        if (HealthLabel is not null) HealthLabel.Text = $"生命 {run.CurrentHealth}/{run.MaxHealth}";
        if (RemoveButton is not null) RemoveButton.Disabled = _selectedIndex < 0 || _removeResolved;
        if (SkipRemoveButton is not null) SkipRemoveButton.Disabled = _removeResolved;
        if (ContinueButton is not null) ContinueButton.Disabled = !_healed || !_removeResolved;
    }
}
