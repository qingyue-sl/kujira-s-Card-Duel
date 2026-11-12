using System.Collections.Generic;
using Godot;

namespace KujiraKo;

/// <summary>路线中的一个节点：战斗节点携带遭遇资源，休整节点没有敌人。</summary>
public sealed class RunStep
{
    public EncounterData? Encounter { get; }
    public bool IsRest { get; }

    public RunStep(EncounterData encounter)
    {
        Encounter = encounter;
    }

    public RunStep()
    {
        IsRest = true;
    }
}

/// <summary>
/// 一局冒险的持久运行时状态。切换战斗、休整、结算场景时，生命、牌组和路线都由此保留。
/// </summary>
public sealed class RunState
{
    public int MaxHealth { get; }
    public int CurrentHealth { get; set; }
    public List<CardData> Deck { get; } = new();
    public List<RunStep> Route { get; } = new();
    public int StepIndex { get; set; }

    public RunState(int maxHealth, IEnumerable<CardData> startingDeck)
    {
        MaxHealth = maxHealth;
        CurrentHealth = maxHealth;
        Deck.AddRange(startingDeck);
    }

    public int HealPercent(float percent)
    {
        // 向上取整让 60 生命的角色在 25% 休整时恰好恢复 15 点。
        var amount = Mathf.CeilToInt(MaxHealth * percent);
        var before = CurrentHealth;
        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
        return CurrentHealth - before;
    }
}
