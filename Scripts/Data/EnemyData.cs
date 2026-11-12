using Godot;

namespace KujiraKo;

[GlobalClass]
public partial class EnemyData : Resource
{
    // 与 CardData 一样，EnemyData 只存模板，不存本场战斗的当前生命和护甲。
    [Export] public string Id { get; set; } = string.Empty;
    // 敌人 UI 显示的名称。
    [Export] public string DisplayName { get; set; } = string.Empty;
    // 每次创建 Enemy 运行时实例时写入其初始生命和最大生命。
    [Export(PropertyHint.Range, "1,999,1")] public int MaxHealth { get; set; } = 1;
    // 运行时会把这些条目展开、洗牌，得到敌人的当前行为队列。
    [Export] public Godot.Collections.Array<EnemyActionEntry> ActionEntries { get; set; } = new();
}
