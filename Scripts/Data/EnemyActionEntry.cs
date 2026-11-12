using Godot;

namespace KujiraKo;

[GlobalClass]
public partial class EnemyActionEntry : Resource
{
    // Action 是动作模板，Copies 决定它在一轮随机行为序列中出现几次。
    [Export] public EnemyActionData? Action { get; set; }
    // 例如填 2，表示此动作在每一轮队列中固定出现两次。
    [Export(PropertyHint.Range, "1,99,1")] public int Copies { get; set; } = 1;
}
