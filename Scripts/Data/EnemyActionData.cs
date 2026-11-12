using Godot;

namespace KujiraKo;

[GlobalClass]
public partial class EnemyActionData : Resource
{
    // 一个 EnemyActionData 对应玩家头顶看到的“一次敌人意图”。
    [Export] public string ActionName { get; set; } = string.Empty;

    // 用于选择意图图标和视觉颜色；真正结算仍看 Effects。
    [Export] public IntentType IntentType { get; set; }

    // 敌人也复用 CardEffect，避免两套伤害、护甲和上状态逻辑逐渐分叉。
    [Export] public Godot.Collections.Array<CardEffect> Effects { get; set; } = new();
}
