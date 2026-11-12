namespace KujiraKo;

/// <summary>UI 飘字的类型。它只描述表现，不参与任何伤害结算。</summary>
public enum CombatFeedbackKind
{
    Damage,
    Block,
    ArmorGain,
    Heal,
    Counter
}

/// <summary>一次需要立即表现给玩家看的战斗结果。</summary>
public sealed class CombatFeedback
{
    public CombatantState Target { get; }
    public CombatFeedbackKind Kind { get; }
    public int Amount { get; }

    public CombatFeedback(CombatantState target, CombatFeedbackKind kind, int amount)
    {
        Target = target;
        Kind = kind;
        Amount = amount;
    }
}
