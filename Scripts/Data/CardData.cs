using Godot;

namespace KujiraKo;

[GlobalClass]
public partial class CardData : Resource
{
    // Id 用于存档、图鉴和升级；展示名称可以改，Id 不应随意改。
    [Export] public string Id { get; set; } = string.Empty;
    // 卡面和奖励界面显示的名称。
    [Export] public string DisplayName { get; set; } = string.Empty;
    // 卡面显示的说明文字；数值本身仍以 Effects 为准。
    [Export(PropertyHint.MultilineText)] public string Description { get; set; } = string.Empty;
    // 打出此牌所需的能量；不能为负数。
    [Export(PropertyHint.Range, "0,9,1")] public int Cost { get; set; }
    // 用于卡牌视觉分类，也方便以后做“攻击牌相关”效果。
    [Export] public CardType CardType { get; set; }
    // 用于奖励抽取权重；第一版暂时只记录，不在代码中加权。
    [Export] public Rarity Rarity { get; set; }
    // 告诉 UI 玩家是否需要再选敌人；不直接决定每个效果的最终目标。
    [Export] public TargetType TargetType { get; set; }
    // true 时结算后进入消耗堆，而不是弃牌堆。
    [Export] public bool Exhaust { get; set; }
    // 数组顺序就是结算顺序：先护甲、后反击架势会按这里的排列执行。
    [Export] public Godot.Collections.Array<CardEffect> Effects { get; set; } = new();
}
