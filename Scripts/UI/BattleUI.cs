using System.Collections.Generic;
using Godot;

namespace KujiraKo;

/// <summary>战斗数据与控件之间的桥梁；保留已有卡牌/敌人视图，支持逐个出现与飘字。</summary>
public partial class BattleUI : Control
{
    [Export] public Battle? Battle { get; set; }
    [Export] public PackedScene? CardViewScene { get; set; }
    [Export] public PackedScene? EnemyViewScene { get; set; }
    [Export] public Container? HandContainer { get; set; }
    [Export] public Container? EnemyContainer { get; set; }
    [Export] public Control? FeedbackLayer { get; set; }
    [Export] public Label? PlayerHealthLabel { get; set; }
    [Export] public Label? EnergyLabel { get; set; }
    [Export] public Label? DrawPileLabel { get; set; }
    [Export] public Label? DiscardPileLabel { get; set; }
    [Export] public Label? EncounterLabel { get; set; }
    [Export] public Label? MessageLabel { get; set; }
    [Export] public Label? HandHintLabel { get; set; }
    [Export] public Button? EndTurnButton { get; set; }

    private readonly Dictionary<Enemy, EnemyView> _enemyViews = new();
    private readonly Dictionary<CardInstance, CardView> _cardViews = new();

    public override void _Ready()
    {
        if (Battle is null)
        {
            GD.PushError("BattleUI 需要绑定 Battle 节点。");
            return;
        }
        Battle.BattleStateChanged += Refresh;
        Battle.MessageRaised += ShowMessage;
        Battle.FeedbackRaised += ShowFeedback;
        if (EndTurnButton is not null) EndTurnButton.Pressed += Battle.EndPlayerTurn;
        Refresh();
    }

    public override void _ExitTree()
    {
        if (Battle is null) return;
        Battle.BattleStateChanged -= Refresh;
        Battle.MessageRaised -= ShowMessage;
        Battle.FeedbackRaised -= ShowFeedback;
    }

    public void Refresh()
    {
        if (Battle is null || Battle.Player is null) return;
        SyncEnemyViews();
        SyncHandViews();

        var armorText = Battle.Player.Armor > 0 ? $" (+{Battle.Player.Armor})" : string.Empty;
        if (PlayerHealthLabel is not null) PlayerHealthLabel.Text = $"生命 {Battle.Player.CurrentHealth}/{Battle.Player.MaxHealth}{armorText}";
        if (EnergyLabel is not null) EnergyLabel.Text = $"能量 {Battle.Player.Energy}/3";
        if (DrawPileLabel is not null) DrawPileLabel.Text = $"抽牌堆：{Battle.Deck.DrawPile.Count}";
        if (DiscardPileLabel is not null) DiscardPileLabel.Text = $"弃牌堆：{Battle.Deck.DiscardPile.Count}";
        if (EncounterLabel is not null)
        {
            var encounter = Battle.CurrentEncounter;
            EncounterLabel.Text = encounter is null ? "独立战斗" : $"{GameFlow.Instance?.GetProgressText()} · {encounter.DisplayName}";
        }
        if (EndTurnButton is not null)
        {
            EndTurnButton.Disabled = Battle.CurrentPhase != BattlePhase.PlayerTurn || Battle.IsResolving;
            EndTurnButton.Text = $"结束回合\n第 {Battle.TurnNumber} 回合";
        }
        RefreshHandHint();
        foreach (var view in _enemyViews.Values) view.Refresh();
        foreach (var view in _cardViews.Values) view.Refresh();
    }

    private void SyncEnemyViews()
    {
        if (Battle is null || EnemyContainer is null || EnemyViewScene is null) return;
        foreach (var enemy in Battle.Enemies)
        {
            if (_enemyViews.ContainsKey(enemy)) continue;
            var view = EnemyViewScene.Instantiate<EnemyView>();
            EnemyContainer.AddChild(view);
            view.Bind(enemy, Battle);
            view.PlaySpawn();
            _enemyViews.Add(enemy, view);
        }
    }

    private void SyncHandViews()
    {
        if (Battle is null || HandContainer is null || CardViewScene is null) return;
        var inHand = new HashSet<CardInstance>(Battle.Deck.Hand);
        foreach (var pair in new List<KeyValuePair<CardInstance, CardView>>(_cardViews))
        {
            if (inHand.Contains(pair.Key)) continue;
            pair.Value.QueueFree();
            _cardViews.Remove(pair.Key);
        }
        foreach (var card in Battle.Deck.Hand)
        {
            if (_cardViews.ContainsKey(card)) continue;
            var view = CardViewScene.Instantiate<CardView>();
            HandContainer.AddChild(view);
            view.Bind(card, Battle);
            view.PlayDraw();
            _cardViews.Add(card, view);
        }
    }

    private void ShowMessage(string message)
    {
        if (MessageLabel is not null) MessageLabel.Text = message;
    }

    private void ShowFeedback(CombatFeedback feedback)
    {
        if (FeedbackLayer is null) return;
        var position = GetFeedbackPosition(feedback.Target);
        var label = new Label
        {
            Position = position,
            ZIndex = 20,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Text = FeedbackText(feedback)
        };
        label.AddThemeFontSizeOverride("font_size", 25);
        label.AddThemeColorOverride("font_color", FeedbackColor(feedback.Kind));
        FeedbackLayer.AddChild(label);
        var tween = label.CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(label, "position:y", label.Position.Y - 44, 0.8f);
        tween.TweenProperty(label, "modulate:a", 0f, 0.8f);
        tween.SetParallel(false);
        tween.TweenCallback(Callable.From(label.QueueFree));
    }

    private Vector2 GetFeedbackPosition(CombatantState target)
    {
        if (target is PlayerBattleState && PlayerHealthLabel is not null)
            return PlayerHealthLabel.GlobalPosition + new Vector2(PlayerHealthLabel.Size.X * 0.5f - 25, -22);
        if (target is Enemy enemy && _enemyViews.TryGetValue(enemy, out var view))
            return view.GetFeedbackPosition();
        return new Vector2(620, 280);
    }

    private static string FeedbackText(CombatFeedback feedback) => feedback.Kind switch
    {
        CombatFeedbackKind.Damage => $"-{feedback.Amount}",
        CombatFeedbackKind.Block => $"格挡 {feedback.Amount}",
        CombatFeedbackKind.ArmorGain => $"+{feedback.Amount} 护甲",
        CombatFeedbackKind.Heal => $"+{feedback.Amount}",
        CombatFeedbackKind.Counter => $"反击 -{feedback.Amount}",
        _ => feedback.Amount.ToString()
    };

    private static Color FeedbackColor(CombatFeedbackKind kind) => kind switch
    {
        CombatFeedbackKind.Damage => new Color(1f, 0.3f, 0.32f),
        CombatFeedbackKind.Block or CombatFeedbackKind.ArmorGain => new Color(0.38f, 0.74f, 1f),
        CombatFeedbackKind.Heal => new Color(0.38f, 1f, 0.56f),
        CombatFeedbackKind.Counter => new Color(1f, 0.7f, 0.25f),
        _ => Colors.White
    };

    private void RefreshHandHint()
    {
        if (Battle is null || HandHintLabel is null) return;
        if (Battle.CurrentPhase == BattlePhase.Setup) HandHintLabel.Text = "敌人正在出现…";
        else if (Battle.CurrentPhase == BattlePhase.EnemyTurn) HandHintLabel.Text = "敌人行动中…";
        else if (Battle.CurrentPhase == BattlePhase.Victory) HandHintLabel.Text = "战斗胜利：领取奖励后继续冒险。";
        else if (Battle.CurrentPhase == BattlePhase.Defeat) HandHintLabel.Text = "冒险失败。";
        else if (Battle.SelectedCard?.Data.TargetType == TargetType.SingleEnemy)
            HandHintLabel.Text = $"已选择「{Battle.SelectedCard.Data.DisplayName}」：点击高亮敌人施放";
        else HandHintLabel.Text = "选择一张可用卡牌；攻击牌需再选择敌人，技能牌会立即施放。";
    }
}
