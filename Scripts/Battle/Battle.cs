using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

namespace KujiraKo;

/// <summary>管理一场战斗的规则、分阶段演出节奏和结果播报；跨战斗状态由 GameFlow 保存。</summary>
public partial class Battle : Node
{
    [Signal] public delegate void BattleStateChangedEventHandler();
    [Signal] public delegate void MessageRaisedEventHandler(string message);
    [Signal] public delegate void BattleEndedEventHandler(bool victory);
    public event Action<CombatFeedback>? FeedbackRaised;

    [ExportGroup("独立战斗备用配置")]
    [Export(PropertyHint.Range, "1,999,1")] public int PlayerMaxHealth { get; set; } = 60;
    [Export] public Godot.Collections.Array<CardData> StartingDeck { get; set; } = new();
    [Export] public Godot.Collections.Array<EnemyData> StartingEnemies { get; set; } = new();
    [Export] public Godot.Collections.Array<EnemyData> DemoEnemyPool { get; set; } = new();

    public PlayerBattleState Player { get; private set; } = null!;
    public DeckManager Deck { get; } = new();
    public List<Enemy> Enemies { get; } = new();
    public BattlePhase CurrentPhase { get; private set; } = BattlePhase.Setup;
    public CardInstance? SelectedCard { get; private set; }
    public int TurnNumber { get; private set; }
    public bool IsResolving { get; private set; }
    public EncounterData? CurrentEncounter => GameFlow.Instance?.GetCurrentEncounter();

    private readonly RandomNumberGenerator _random = new();
    // 没有从主菜单进入时，仍可单独打开 battle.tscn 调试。
    private readonly List<CardData> _standaloneRunDeck = new();

    public override void _Ready() => _ = StartBattleAsync();

    private async Task StartBattleAsync()
    {
        _random.Randomize();
        CurrentPhase = BattlePhase.Setup;
        SelectedCard = null;
        IsResolving = true;
        TurnNumber = 0;
        Enemies.Clear();

        var run = GameFlow.Instance?.ActiveRun;
        var deckSource = run?.Deck;
        if (deckSource is null)
        {
            if (_standaloneRunDeck.Count == 0)
                _standaloneRunDeck.AddRange(StartingDeck.Count > 0 ? StartingDeck : DemoContentFactory.CreateStarterDeck());
            deckSource = _standaloneRunDeck;
        }

        Player = new PlayerBattleState(run?.MaxHealth ?? PlayerMaxHealth, run?.CurrentHealth);
        Deck.InitializeDeck(deckSource, _random);
        EmitSignal(SignalName.BattleStateChanged);

        var encounter = CurrentEncounter;
        var enemySource = encounter?.Enemies.Count > 0
            ? encounter.Enemies
            : StartingEnemies.Count > 0
                ? StartingEnemies
                : DemoEnemyPool.Count > 0
                    ? new Godot.Collections.Array<EnemyData> { DemoEnemyPool[_random.RandiRange(0, DemoEnemyPool.Count - 1)] }
                    : DemoContentFactory.CreateDemoEncounter(_random);

        // 按关卡编队顺序逐只进入；UI 会对新增面板播放出现动画。
        foreach (var enemyData in enemySource)
        {
            var enemy = new Enemy(enemyData, _random);
            enemy.PrepareNextIntent();
            Enemies.Add(enemy);
            EmitSignal(SignalName.BattleStateChanged);
            await Delay(0.45f);
        }

        await StartPlayerTurnAsync(isFirstTurn: true);
    }

    public void AddCardToRunDeck(CardData card)
    {
        if (GameFlow.Instance?.ActiveRun is not null) GameFlow.Instance.AddCardToRunDeck(card);
        else _standaloneRunDeck.Add(card);
        EmitSignal(SignalName.MessageRaised, $"获得新卡：{card.DisplayName}");
    }

    /// <summary>奖励确认后的流程入口。Boss 胜利改为本局结算。</summary>
    public void ContinueAfterVictory()
    {
        if (CurrentPhase != BattlePhase.Victory) return;
        if (CurrentEncounter?.Kind == EncounterKind.Boss) GameFlow.Instance?.FinishBossRun(Player.CurrentHealth);
        else GameFlow.Instance?.ContinueAfterVictory(Player.CurrentHealth);
    }

    public bool TrySelectCard(CardInstance card)
    {
        if (CurrentPhase != BattlePhase.PlayerTurn || IsResolving || !Deck.Hand.Contains(card)) return false;
        if (!Player.CanAfford(card.Data))
        {
            EmitSignal(SignalName.MessageRaised, "能量不足。");
            return false;
        }
        SelectedCard = ReferenceEquals(SelectedCard, card) ? null : card;
        EmitSignal(SignalName.BattleStateChanged);
        return true;
    }

    public void CancelSelectedCard()
    {
        SelectedCard = null;
        EmitSignal(SignalName.BattleStateChanged);
    }

    public bool TryPlaySelectedCard(Enemy? target = null)
    {
        if (SelectedCard is null || CurrentPhase != BattlePhase.PlayerTurn || IsResolving) return false;
        var card = SelectedCard;
        var cardData = card.Data;
        if (!Player.CanAfford(cardData) || !Deck.Hand.Contains(card))
        {
            CancelSelectedCard();
            return false;
        }
        if (RequiresSingleEnemyTarget(cardData) && (target is null || target.IsDead))
        {
            EmitSignal(SignalName.MessageRaised, "请选择一名存活敌人。");
            return false;
        }

        IsResolving = true;
        try
        {
            Player.Energy -= cardData.Cost;
            Deck.RemoveFromHand(card);
            SelectedCard = null;
            var results = new List<string>();
            foreach (var effect in cardData.Effects)
            {
                ResolveEffect(effect, Player, target, results);
                if (CheckBattleEnd()) break;
            }
            EmitSignal(SignalName.MessageRaised, $"{Player.DisplayName} 使用 {cardData.DisplayName}：{FormatResults(results)}");
            if (cardData.Exhaust) Deck.MoveToExhaust(card); else Deck.MoveToDiscard(card);
        }
        finally
        {
            IsResolving = false;
            EmitSignal(SignalName.BattleStateChanged);
        }
        return true;
    }

    /// <summary>按钮事件入口；敌人行动需要等待，因此采用 async void。</summary>
    public async void EndPlayerTurn()
    {
        if (CurrentPhase != BattlePhase.PlayerTurn || IsResolving) return;
        SelectedCard = null;
        Deck.DiscardHandAtTurnEnd();
        StatusManager.OnOwnerTurnEnd(Player);
        CurrentPhase = BattlePhase.EnemyTurn;
        IsResolving = true;
        EmitSignal(SignalName.BattleStateChanged);
        await Delay(0.2f);
        await StartEnemyTurnAsync();
    }

    private async Task StartPlayerTurnAsync(bool isFirstTurn)
    {
        CurrentPhase = BattlePhase.PlayerTurn;
        TurnNumber = isFirstTurn ? 1 : TurnNumber + 1;
        StatusManager.OnOwnerTurnStart(Player);
        Player.Energy = 3;
        IsResolving = true;
        EmitSignal(SignalName.BattleStateChanged);

        // 开局六张、后续四张，逐张进入手牌；不会覆盖上一名敌人的结果播报。
        var drawCount = isFirstTurn ? 6 : 4;
        for (var i = 0; i < drawCount; i++)
        {
            if (Deck.DrawOne() is null) break;
            EmitSignal(SignalName.BattleStateChanged);
            await Delay(0.18f);
        }
        IsResolving = false;
        EmitSignal(SignalName.BattleStateChanged);
    }

    private async Task StartEnemyTurnAsync()
    {
        foreach (var enemy in Enemies)
            if (!enemy.IsDead) StatusManager.OnOwnerTurnStart(enemy);

        foreach (var enemy in Enemies)
        {
            if (enemy.IsDead || enemy.CurrentIntent is null) continue;
            var results = new List<string>();
            var battleEnded = false;
            foreach (var effect in enemy.CurrentIntent.Effects)
            {
                ResolveEffect(effect, enemy, Player, results);
                if (CheckBattleEnd()) { battleEnded = true; break; }
            }

            EmitSignal(SignalName.MessageRaised, $"{enemy.DisplayName} 使用 {enemy.CurrentIntent.ActionName}：{FormatResults(results)}");
            EmitSignal(SignalName.BattleStateChanged);
            if (battleEnded) return;

            enemy.PrepareNextIntent();
            EmitSignal(SignalName.BattleStateChanged);
            // 每位敌人结算后停 1.25 秒，供玩家阅读飘字、意图和结果播报。
            await Delay(1.25f);
        }

        foreach (var enemy in Enemies)
            if (!enemy.IsDead) StatusManager.OnOwnerTurnEnd(enemy);

        if (!CheckBattleEnd()) await StartPlayerTurnAsync(isFirstTurn: false);
    }

    private void ResolveEffect(CardEffect effect, CombatantState source, CombatantState? primaryTarget, List<string> results)
    {
        var targets = ResolveTargets(effect.Target, source, primaryTarget);
        switch (effect.EffectType)
        {
            case EffectType.DealDamage:
                foreach (var target in targets)
                    for (var hit = 0; hit < effect.HitCount && !target.IsDead; hit++)
                    {
                        var damage = DamageResolver.ResolveAttack(source, target, effect.Value);
                        RaiseDamageFeedback(source, target, damage);
                        results.Add(DescribeDamage(target, damage));
                    }
                break;
            case EffectType.GainArmor:
                foreach (var target in targets)
                {
                    var before = target.Armor;
                    target.GainArmor(effect.Value);
                    var gained = target.Armor - before;
                    if (gained > 0) FeedbackRaised?.Invoke(new CombatFeedback(target, CombatFeedbackKind.ArmorGain, gained));
                    results.Add($"{target.DisplayName} 获得 {gained} 点护甲");
                }
                break;
            case EffectType.ApplyStatus:
                foreach (var target in targets)
                {
                    StatusManager.Add(target, effect.StatusType, effect.StatusAmount);
                    results.Add($"{target.DisplayName} 获得 {StatusName(effect.StatusType)} {effect.StatusAmount} 层");
                }
                break;
            case EffectType.DrawCards when source is PlayerBattleState:
                var cardsBefore = Deck.Hand.Count;
                Deck.DrawCards(effect.Value);
                results.Add($"抽取 {Deck.Hand.Count - cardsBefore} 张牌");
                break;
            case EffectType.GainEnergy when source is PlayerBattleState:
                Player.Energy += effect.Value;
                results.Add($"获得 {effect.Value} 点能量");
                break;
            case EffectType.DamageFromArmor:
                DealArmorDamage(source, targets, effect.Value, false, results);
                break;
            case EffectType.DamageFromArmorAndRemoveArmor:
                DealArmorDamage(source, targets, effect.Value, true, results);
                break;
            case EffectType.Heal:
                foreach (var target in targets)
                {
                    var healed = target.Heal(effect.Value);
                    if (healed > 0) FeedbackRaised?.Invoke(new CombatFeedback(target, CombatFeedbackKind.Heal, healed));
                    results.Add(healed > 0 ? $"{target.DisplayName} 恢复 {healed} 点生命" : $"{target.DisplayName} 生命已满");
                }
                break;
        }
    }

    private void RaiseDamageFeedback(CombatantState attacker, CombatantState target, DamageResult result)
    {
        if (result.ArmorAbsorbed > 0) FeedbackRaised?.Invoke(new CombatFeedback(target, CombatFeedbackKind.Block, result.ArmorAbsorbed));
        if (result.HealthDamage > 0) FeedbackRaised?.Invoke(new CombatFeedback(target, CombatFeedbackKind.Damage, result.HealthDamage));
        if (result.CounterResult?.HealthDamage > 0) FeedbackRaised?.Invoke(new CombatFeedback(attacker, CombatFeedbackKind.Counter, result.CounterResult.HealthDamage));
    }

    private void DealArmorDamage(CombatantState source, List<CombatantState> targets, int multiplier, bool removeArmor, List<string> results)
    {
        var armorValue = source.Armor;
        if (removeArmor) source.Armor = 0;
        foreach (var target in targets)
        {
            var damage = DamageResolver.ResolveAttack(source, target, armorValue * multiplier);
            RaiseDamageFeedback(source, target, damage);
            results.Add(DescribeDamage(target, damage));
        }
    }

    private List<CombatantState> ResolveTargets(EffectTarget rule, CombatantState source, CombatantState? primaryTarget)
    {
        var targets = new List<CombatantState>();
        switch (rule)
        {
            case EffectTarget.Self: targets.Add(source); break;
            case EffectTarget.PrimaryTarget:
                if (primaryTarget is not null && !primaryTarget.IsDead) targets.Add(primaryTarget);
                break;
            case EffectTarget.AllOpponents:
                if (source is PlayerBattleState) foreach (var enemy in Enemies) if (!enemy.IsDead) targets.Add(enemy);
                else if (!Player.IsDead) targets.Add(Player);
                break;
            case EffectTarget.RandomOpponent:
                var opponents = ResolveTargets(EffectTarget.AllOpponents, source, primaryTarget);
                if (opponents.Count > 0) targets.Add(opponents[_random.RandiRange(0, opponents.Count - 1)]);
                break;
            case EffectTarget.Inherit:
                if (primaryTarget is not null && !primaryTarget.IsDead) targets.Add(primaryTarget); else targets.Add(source);
                break;
        }
        return targets;
    }

    private static bool RequiresSingleEnemyTarget(CardData card) => card.TargetType == TargetType.SingleEnemy;
    private static string FormatResults(List<string> results) => results.Count > 0 ? string.Join("；", results) : "没有产生可见效果";
    private static string DescribeDamage(CombatantState target, DamageResult result)
    {
        var text = result.HealthDamage > 0 ? $"{target.DisplayName} 生命 -{result.HealthDamage}" : $"{target.DisplayName} 未损失生命";
        if (result.ArmorAbsorbed > 0) text += $"（护甲吸收 {result.ArmorAbsorbed}）";
        if (result.CounterResult is not null) text += $"，触发反击 {result.CounterResult.HealthDamage} 点";
        return text;
    }

    public static string StatusName(StatusType status) => status switch
    {
        StatusType.Strength => "力量", StatusType.Dexterity => "敏捷", StatusType.Weak => "虚弱",
        StatusType.Vulnerable => "易伤", StatusType.Poison => "中毒", StatusType.CounterStance => "反击架势", _ => status.ToString()
    };

    private bool CheckBattleEnd()
    {
        if (Player.IsDead) { FinishBattle(false); return true; }
        foreach (var enemy in Enemies) if (!enemy.IsDead) return false;
        FinishBattle(true);
        return true;
    }

    private void FinishBattle(bool victory)
    {
        if (CurrentPhase is BattlePhase.Victory or BattlePhase.Defeat) return;
        CurrentPhase = victory ? BattlePhase.Victory : BattlePhase.Defeat;
        SelectedCard = null;
        IsResolving = false;
        EmitSignal(SignalName.BattleStateChanged);
        EmitSignal(SignalName.BattleEnded, victory);
        if (!victory && GameFlow.Instance?.ActiveRun is not null) GameFlow.Instance.HandleBattleDefeat(Player.CurrentHealth);
    }

    private async Task Delay(float seconds) => await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
}
