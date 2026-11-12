using System.Collections.Generic;

namespace KujiraKo;

public abstract class CombatantState
{
    // 玩家与敌人的共同运行时数据。抽象类避免两边重复写生命、护甲和状态。
    // 日志与 UI 显示用名称，由 Player 或 Enemy 的构造函数初始化。
    public string DisplayName { get; protected set; } = string.Empty;
    // 生命上限在本最小原型中不变，因此只允许子类初始化。
    public int MaxHealth { get; protected set; }
    // 当前生命；DamageResolver 或中毒会降低它，最低保持为 0。
    public int CurrentHealth { get; set; }
    // 当前护甲；只吸收本次伤害，并在拥有者回合开始时被 StatusManager 清零。
    public int Armor { get; set; }
    // 状态到层数的映射；不要从外部直接增减，优先使用 StatusManager。
    public Dictionary<StatusType, int> Statuses { get; } = new();

    // 统一死亡判断，避免各处各自比较生命值。
    public bool IsDead => CurrentHealth <= 0;

    public void GainArmor(int amount)
    {
        // 敏捷只影响“获得护甲”的事件，不会修改已有护甲。
        Armor += System.Math.Max(0, amount) + StatusManager.GetStacks(this, StatusType.Dexterity);
    }

    public void TakeUnblockedDamage(int amount)
    {
        // 这里的伤害已在 DamageResolver 处理过护甲；中毒等直伤也会走这里。
        CurrentHealth = System.Math.Max(0, CurrentHealth - System.Math.Max(0, amount));
    }

    /// <summary>恢复生命并返回实际恢复量；满血时返回 0。</summary>
    public int Heal(int amount)
    {
        var before = CurrentHealth;
        CurrentHealth = System.Math.Min(MaxHealth, CurrentHealth + System.Math.Max(0, amount));
        return CurrentHealth - before;
    }
}
