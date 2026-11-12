namespace KujiraKo;

public sealed class PlayerBattleState : CombatantState
{
    // 能量是玩家独有的运行时属性；敌人不会使用这套出牌资源。
    public int Energy { get; set; }

    // 创建一场新战斗的玩家运行时状态。
    public PlayerBattleState(int maxHealth, int? currentHealth = null)
    {
        DisplayName = "玩家";
        MaxHealth = maxHealth;
        CurrentHealth = System.Math.Clamp(currentHealth ?? maxHealth, 1, maxHealth);
    }

    // UI 和 Battle 都调用同一个校验，避免“看起来能打但实际不能打”。
    public bool CanAfford(CardData card) => Energy >= card.Cost;
}
