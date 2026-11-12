namespace KujiraKo;

public sealed class DamageResult
{
    // 结果对象便于 UI 以后显示“护甲吸收了多少”“反击是否触发”等战斗反馈。
    // 加上力量、虚弱和易伤之前的攻击基础值。
    public int RawDamage { get; init; }
    // 经过虚弱和易伤后、准备被护甲吸收的伤害。
    public int ModifiedDamage { get; init; }
    // 本次由护甲实际抵消的伤害。
    public int ArmorAbsorbed { get; init; }
    // 穿过护甲并真正扣除生命的伤害。
    public int HealthDamage { get; init; }
    // 发生反击时保存反击结算结果；没有反击则为 null。
    public DamageResult? CounterResult { get; set; }
}

public static class DamageResolver
{
    // 所有攻击类伤害都必须从这里经过，包括敌人攻击、玩家攻击与反击。
    public static DamageResult ResolveAttack(
        CombatantState attacker,
        CombatantState defender,
        int baseDamage,
        bool canTriggerCounter = true)
    {
        // canTriggerCounter 只在递归反击时传 false，用来截断“反击反击”的循环。
        // 力量先加到每一段攻击上，所以多段攻击会多次享受力量。
        var rawDamage = System.Math.Max(0, baseDamage + StatusManager.GetStacks(attacker, StatusType.Strength));
        var modifiedDamage = rawDamage;

        if (StatusManager.GetStacks(attacker, StatusType.Weak) > 0)
        {
            modifiedDamage = (int)System.Math.Floor(modifiedDamage * 0.75f);
        }

        if (StatusManager.GetStacks(defender, StatusType.Vulnerable) > 0)
        {
            modifiedDamage = (int)System.Math.Floor(modifiedDamage * 1.5f);
        }

        // 护甲只吸收当前这一段伤害；多段攻击会重复进入本方法。
        var absorbed = System.Math.Min(defender.Armor, modifiedDamage);
        defender.Armor -= absorbed;
        var healthDamage = modifiedDamage - absorbed;
        defender.TakeUnblockedDamage(healthDamage);

        var result = new DamageResult
        {
            RawDamage = rawDamage,
            ModifiedDamage = modifiedDamage,
            ArmorAbsorbed = absorbed,
            HealthDamage = healthDamage
        };

        var counterDamage = StatusManager.GetStacks(defender, StatusType.CounterStance);
        if (canTriggerCounter && absorbed > 0 && counterDamage > 0 && !attacker.IsDead)
        {
            // 反击不会再次触发对方的反击，避免双方都有反击架势时无限递归。
            result.CounterResult = ResolveAttack(defender, attacker, counterDamage, false);
        }

        return result;
    }
}
