namespace KujiraKo;

public static class StatusManager
{
    // 状态规则的唯一入口。不要把虚弱、易伤的减层散落在 UI 或卡牌脚本里。
    public static int GetStacks(CombatantState owner, StatusType status)
    {
        // 字典中不存在的状态等同于 0 层，调用者无需先判断 ContainsKey。
        return owner.Statuses.TryGetValue(status, out var stacks) ? stacks : 0;
    }

    public static void Add(CombatantState owner, StatusType status, int amount)
    {
        // 负数或 0 层状态没有意义，直接忽略，避免出现负层数。
        if (amount <= 0)
        {
            return;
        }

        owner.Statuses[status] = GetStacks(owner, status) + amount;
    }

    public static void Remove(CombatantState owner, StatusType status)
    {
        // Remove 即使状态不存在也安全，适合回合开始时无条件调用。
        owner.Statuses.Remove(status);
    }

    public static void OnOwnerTurnStart(CombatantState owner)
    {
        // 设计规则：护甲持续到“自己的下一个回合开始”，因此此处清零。
        owner.Armor = 0;
        // 反击架势同样只覆盖刚结束的那一个敌人回合。
        Remove(owner, StatusType.CounterStance);
    }

    public static void OnOwnerTurnEnd(CombatantState owner)
    {
        // 中毒不受护甲保护，并且先造成伤害、后减少层数。
        var poison = GetStacks(owner, StatusType.Poison);
        if (poison > 0)
        {
            owner.TakeUnblockedDamage(poison);
            Decrease(owner, StatusType.Poison);
        }

        // 临时减益在持有者自己的回合结束时减少，而不是施加者的回合结束。
        Decrease(owner, StatusType.Weak);
        Decrease(owner, StatusType.Vulnerable);
    }

    private static void Decrease(CombatantState owner, StatusType status)
    {
        // 层数从 1 减到 0 时删除键，保持状态字典干净。
        var current = GetStacks(owner, status);
        if (current <= 1)
        {
            Remove(owner, status);
            return;
        }

        owner.Statuses[status] = current - 1;
    }
}
