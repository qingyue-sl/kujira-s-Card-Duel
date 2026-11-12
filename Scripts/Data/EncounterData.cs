using Godot;

namespace KujiraKo;

/// <summary>
/// 一场遭遇的纯配置资源：只定义本关由哪些敌人组成，以及它属于哪一个路线池。
/// 敌人的生命、动作序列和数值仍保留在各自的 EnemyData 中。
/// </summary>
[GlobalClass]
public partial class EncounterData : Resource
{
    [Export] public string Id { get; set; } = string.Empty;
    [Export] public string DisplayName { get; set; } = string.Empty;
    [Export] public EncounterKind Kind { get; set; } = EncounterKind.Normal;
    [Export] public EncounterDifficulty Difficulty { get; set; } = EncounterDifficulty.Low;
    [Export] public Godot.Collections.Array<EnemyData> Enemies { get; set; } = new();
}
