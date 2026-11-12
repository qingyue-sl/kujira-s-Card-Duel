using Godot;

namespace KujiraKo;

[GlobalClass]
public partial class CardEffect : Resource
{
    // 一张卡的“效果积木”。例如“获得护甲并反击”是两个 CardEffect。
    // 决定 Battle.ResolveEffect 使用哪一段结算分支。
    [Export] public EffectType EffectType { get; set; }
    // Inherit 表示沿用卡牌的主要目标；Self 表示永远对出牌者生效。
    [Export] public EffectTarget Target { get; set; } = EffectTarget.Inherit;
    // 伤害、护甲、抽牌数等通用数值；不同 EffectType 对它有不同解释。
    [Export] public int Value { get; set; }
    // 多段攻击次数。非伤害效果保持默认 1 即可。
    [Export(PropertyHint.Range, "1,99,1")] public int HitCount { get; set; } = 1;
    // ApplyStatus 时要添加的状态和层数。
    // ApplyStatus 时具体使用哪种状态；其他效果会忽略它。
    [Export] public StatusType StatusType { get; set; }
    // ApplyStatus 时添加的层数；其他效果会忽略它。
    [Export] public int StatusAmount { get; set; }
}
