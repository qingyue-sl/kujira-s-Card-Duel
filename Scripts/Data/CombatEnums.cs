namespace KujiraKo;

// 这些枚举是所有 Resource 与战斗逻辑共用的“词典”。
// 先固定名称再写逻辑，可以避免用字符串比较带来的拼写错误。
public enum CardType
{
    Attack, // 会造成攻击伤害的卡；伤害受力量、虚弱、易伤影响。
    Skill  // 非直接攻击卡，例如获得护甲、抽牌、上状态。
}

public enum Rarity
{
    Starter,  // 初始牌；一般不会出现在普通奖励池。
    Common,   // 最常见的战斗奖励。
    Uncommon, // 比普通牌少见、通常更有构筑方向的奖励。
    Rare      // 极少出现、通常能显著改变玩法的奖励。
}

public enum TargetType
{
    Self,        // 点击卡后立即对玩家自己结算。
    SingleEnemy, // 必须再点一名存活敌人才能结算。
    AllEnemies,  // 应点击敌人区域，效果实际配置为 AllOpponents。
    EnemyArea,   // 为以后“敌人区域”类牌预留的 UI 目标类别。
    RandomEnemy  // 不需要玩家选敌人，由 Battle 随机选择一个存活敌人。
}

public enum EffectType
{
    DealDamage,                    // Value 为每一段基础伤害；HitCount 为段数。
    GainArmor,                     // Value 为获得护甲，敏捷会额外加成。
    ApplyStatus,                   // 使用 StatusType 和 StatusAmount。
    DrawCards,                     // Value 为抽牌张数；第一版仅玩家有效。
    GainEnergy,                    // Value 为获得能量；第一版仅玩家有效。
    DamageFromArmor,               // 按当前护甲×Value 造成伤害，但不消耗护甲。
    DamageFromArmorAndRemoveArmor, // 记录当前护甲，清零后按记录值×Value 造成伤害。
    Heal                           // 恢复生命，Value 为基础恢复值，最高不超过最大生命。
}

public enum EffectTarget
{
    Inherit,       // 有主目标就用主目标；没有时默认对出牌者自己。
    Self,          // 强制作用于效果来源，例如敌人给自己加甲。
    PrimaryTarget, // 作用于玩家点击的敌人，或敌人动作指定的玩家。
    AllOpponents,  // 作用于来源的所有敌对单位。
    RandomOpponent // 从来源的所有存活敌对单位中随机选一名。
}

public enum StatusType
{
    Strength,      // 每一段攻击伤害 +1/层；永久。
    Dexterity,     // 每次获得护甲 +1/层；永久。
    Weak,          // 持有者造成的攻击伤害 ×0.75；回合结束减层。
    Vulnerable,    // 持有者受到的攻击伤害 ×1.5；回合结束减层。
    Poison,        // 持有者回合结束失去等于层数的生命，然后减 1 层。
    CounterStance  // 护甲吸收攻击伤害时反击；在持有者下回合开始移除。
}

public enum IntentType
{
    Attack, // 主要是攻击伤害。
    Defend, // 主要是获得护甲。
    Debuff, // 主要是给对方施加减益。
    Buff,   // 主要是给自己施加增益。
    Mixed   // 同时包含两种以上类型，例如伤害加虚弱。
}

public enum BattlePhase
{
    Setup,      // 初始化牌库、角色、敌人意图的阶段。
    PlayerTurn, // 可以选择和打出卡牌的阶段。
    EnemyTurn,  // 敌人依次执行当前意图的阶段。
    Victory,    // 最后一名敌人死亡后的终止状态。
    Defeat      // 玩家生命归零后的终止状态。
}

/// <summary>关卡在整局流程里的职责。普通、精英和 Boss 都复用同一套战斗逻辑。</summary>
public enum EncounterKind
{
    Normal,
    Elite,
    Boss
}

/// <summary>用于路线生成的固定难度分档，不会根据玩家表现偷偷改变敌人数值。</summary>
public enum EncounterDifficulty
{
    Low,
    Medium,
    High
}
