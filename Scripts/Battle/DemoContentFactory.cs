using Godot;

namespace KujiraKo;

/// <summary>
/// 第一版原型的临时内容库。
///
/// 如果战斗场景还没有在 Inspector 配置 Resource，Battle 会从这里取得一套完整的
/// 起始牌、随机普通敌人和奖励池。以后改用 .tres 资源时，不需要删除本类；只要在
/// battle.tscn 的 StartingDeck 与 StartingEnemies 填入资源即可覆盖默认内容。
/// </summary>
public static class DemoContentFactory
{
    /// <summary>创建 5 攻击、4 防御、1 反击架势组成的十张初始牌组。</summary>
    public static Godot.Collections.Array<CardData> CreateStarterDeck()
    {
        var deck = new Godot.Collections.Array<CardData>();
        var strike = Card("tide_strike", "潮汐打击", "造成 6 点伤害", 1, CardType.Attack, TargetType.SingleEnemy,
            Effect(EffectType.DealDamage, EffectTarget.PrimaryTarget, value: 6));
        var guard = Card("whale_shell", "鲸壳防御", "获得 5 点护甲", 1, CardType.Skill, TargetType.Self,
            Effect(EffectType.GainArmor, EffectTarget.Self, value: 5));
        var stance = Card("counter_stance", "反击架势", "获得 5 点护甲。护甲承伤时反击 3", 1, CardType.Skill, TargetType.Self,
            Effect(EffectType.GainArmor, EffectTarget.Self, value: 5),
            Effect(EffectType.ApplyStatus, EffectTarget.Self, statusType: StatusType.CounterStance, statusAmount: 3));

        for (var i = 0; i < 5; i++) deck.Add(strike);
        for (var i = 0; i < 4; i++) deck.Add(guard);
        deck.Add(stance);
        return deck;
    }

    /// <summary>创建本原型的三种普通敌人之一，随机用于每次重新运行的演示战斗。</summary>
    public static Godot.Collections.Array<EnemyData> CreateDemoEncounter(RandomNumberGenerator random)
    {
        var choices = new[] { CreateReefCrab(), CreateJellyfish(), CreateRaider() };
        return new Godot.Collections.Array<EnemyData> { choices[random.RandiRange(0, choices.Length - 1)] };
    }

    /// <summary>创建胜利后三选一卡可用的奖励牌池；同一次抽取不会重复。</summary>
    public static Godot.Collections.Array<CardData> CreateRewardPool()
    {
        return new Godot.Collections.Array<CardData>
        {
            Card("wave_breaker", "破浪一击", "造成 8 点伤害", 1, CardType.Attack, TargetType.SingleEnemy,
                Effect(EffectType.DealDamage, EffectTarget.PrimaryTarget, value: 8)),
            Card("thick_shell", "厚壳防线", "获得 8 点护甲", 1, CardType.Skill, TargetType.Self,
                Effect(EffectType.GainArmor, EffectTarget.Self, value: 8)),
            Card("shield_bash", "盾击", "造成等于当前护甲的伤害", 1, CardType.Attack, TargetType.SingleEnemy,
                Effect(EffectType.DamageFromArmor, EffectTarget.PrimaryTarget, value: 1)),
            Card("armor_spikes", "甲刺", "获得 4 点护甲。护甲承伤时反击 2", 1, CardType.Skill, TargetType.Self,
                Effect(EffectType.GainArmor, EffectTarget.Self, value: 4),
                Effect(EffectType.ApplyStatus, EffectTarget.Self, statusType: StatusType.CounterStance, statusAmount: 2)),
            Card("spot_weakness", "破绽观察", "使敌人获得 2 层易伤", 1, CardType.Skill, TargetType.SingleEnemy,
                Effect(EffectType.ApplyStatus, EffectTarget.PrimaryTarget, statusType: StatusType.Vulnerable, statusAmount: 2)),
            Card("tide_combo", "连潮斩", "造成 3 点伤害 2 次", 1, CardType.Attack, TargetType.SingleEnemy,
                Effect(EffectType.DealDamage, EffectTarget.PrimaryTarget, value: 3, hitCount: 2)),
            Card("supplies", "补给", "抽 1 张牌。消耗", 0, CardType.Skill, TargetType.Self,
                Effect(EffectType.DrawCards, EffectTarget.Self, value: 1), exhaust: true),
            Card("great_whale_barrier", "巨鲸屏障", "获得 16 点护甲", 2, CardType.Skill, TargetType.Self,
                Effect(EffectType.GainArmor, EffectTarget.Self, value: 16), rarity: Rarity.Uncommon),
            Card("counter_tide", "反潮姿态", "获得 8 点护甲。护甲承伤时反击 5", 1, CardType.Skill, TargetType.Self,
                Effect(EffectType.GainArmor, EffectTarget.Self, value: 8),
                Effect(EffectType.ApplyStatus, EffectTarget.Self, statusType: StatusType.CounterStance, statusAmount: 5), rarity: Rarity.Uncommon),
            Card("go_with_current", "顺流", "获得 1 点能量，抽 1 张牌。消耗", 0, CardType.Skill, TargetType.Self,
                Effect(EffectType.GainEnergy, EffectTarget.Self, value: 1),
                Effect(EffectType.DrawCards, EffectTarget.Self, value: 1), exhaust: true, rarity: Rarity.Uncommon),
            Card("armor_crusher", "潮甲粉碎", "失去全部护甲，造成其 2 倍伤害", 2, CardType.Attack, TargetType.SingleEnemy,
                Effect(EffectType.DamageFromArmorAndRemoveArmor, EffectTarget.PrimaryTarget, value: 2), rarity: Rarity.Uncommon),
            Card("whale_song_echo", "鲸歌回响", "获得 12 点护甲、反击 8，并抽 1 张牌", 2, CardType.Skill, TargetType.Self,
                Effect(EffectType.GainArmor, EffectTarget.Self, value: 12),
                Effect(EffectType.ApplyStatus, EffectTarget.Self, statusType: StatusType.CounterStance, statusAmount: 8),
                Effect(EffectType.DrawCards, EffectTarget.Self, value: 1), rarity: Rarity.Rare)
        };
    }

    private static EnemyData CreateReefCrab()
    {
        return Enemy("reef_crab", "礁石蟹", 32,
            Entry(Action("蟹钳", IntentType.Attack, Effect(EffectType.DealDamage, EffectTarget.PrimaryTarget, value: 7)), 2),
            Entry(Action("缩壳", IntentType.Defend, Effect(EffectType.GainArmor, EffectTarget.Self, value: 8))),
            Entry(Action("夹碎", IntentType.Attack, Effect(EffectType.DealDamage, EffectTarget.PrimaryTarget, value: 10))));
    }

    private static EnemyData CreateJellyfish()
    {
        return Enemy("deep_jellyfish", "深海水母", 28,
            Entry(Action("电刺", IntentType.Mixed,
                Effect(EffectType.DealDamage, EffectTarget.PrimaryTarget, value: 6),
                Effect(EffectType.ApplyStatus, EffectTarget.PrimaryTarget, statusType: StatusType.Weak, statusAmount: 1)), 2),
            Entry(Action("麻痹触须", IntentType.Debuff,
                Effect(EffectType.ApplyStatus, EffectTarget.PrimaryTarget, statusType: StatusType.Vulnerable, statusAmount: 2))),
            Entry(Action("毒雾", IntentType.Debuff,
                Effect(EffectType.ApplyStatus, EffectTarget.PrimaryTarget, statusType: StatusType.Poison, statusAmount: 4))));
    }

    private static EnemyData CreateRaider()
    {
        return Enemy("tide_raider", "潮汐掠夺者", 38,
            Entry(Action("弯刀斩", IntentType.Attack, Effect(EffectType.DealDamage, EffectTarget.PrimaryTarget, value: 8)), 2),
            Entry(Action("掠夺姿态", IntentType.Defend, Effect(EffectType.GainArmor, EffectTarget.Self, value: 10))),
            Entry(Action("猛冲", IntentType.Attack, Effect(EffectType.DealDamage, EffectTarget.PrimaryTarget, value: 14))));
    }

    private static CardData Card(string id, string name, string description, int cost, CardType type, TargetType targetType, CardEffect firstEffect, CardEffect? secondEffect = null, CardEffect? thirdEffect = null, bool exhaust = false, Rarity rarity = Rarity.Common)
    {
        var effects = new Godot.Collections.Array<CardEffect> { firstEffect };
        if (secondEffect is not null) effects.Add(secondEffect);
        if (thirdEffect is not null) effects.Add(thirdEffect);
        return new CardData { Id = id, DisplayName = name, Description = description, Cost = cost, CardType = type, TargetType = targetType, Exhaust = exhaust, Rarity = rarity, Effects = effects };
    }

    private static CardEffect Effect(EffectType type, EffectTarget target, int value = 0, int hitCount = 1, StatusType statusType = StatusType.Strength, int statusAmount = 0)
    {
        return new CardEffect { EffectType = type, Target = target, Value = value, HitCount = hitCount, StatusType = statusType, StatusAmount = statusAmount };
    }

    private static EnemyActionData Action(string name, IntentType intentType, params CardEffect[] effects)
    {
        return new EnemyActionData { ActionName = name, IntentType = intentType, Effects = new Godot.Collections.Array<CardEffect>(effects) };
    }

    private static EnemyActionEntry Entry(EnemyActionData action, int copies = 1)
    {
        return new EnemyActionEntry { Action = action, Copies = copies };
    }

    private static EnemyData Enemy(string id, string name, int maxHealth, params EnemyActionEntry[] entries)
    {
        return new EnemyData { Id = id, DisplayName = name, MaxHealth = maxHealth, ActionEntries = new Godot.Collections.Array<EnemyActionEntry>(entries) };
    }
}
