using System.Collections.Generic;
using Godot;

namespace KujiraKo;

public sealed class Enemy : CombatantState
{
    // Enemy 是 EnemyData 在本场战斗里的实例：这里才保存当前生命、护甲和待执行动作。
    // 指向不变的模板资源；同一个 EnemyData 可以生成多个敌人实例。
    public EnemyData Data { get; }
    // 已经展示给玩家、将在本敌人下一次行动时执行的动作。
    public EnemyActionData? CurrentIntent { get; private set; }
    // 尚未展示的本轮动作；CurrentIntent 被取出后不再放回这里。
    public List<EnemyActionData> ActionQueue { get; } = new();

    private readonly RandomNumberGenerator _random;

    public Enemy(EnemyData data, RandomNumberGenerator random)
    {
        // Enemy 不继承 Node，因此可以轻量地被 Battle 管理，UI 由 EnemyView 单独负责。
        Data = data;
        _random = random;
        DisplayName = data.DisplayName;
        MaxHealth = data.MaxHealth;
        CurrentHealth = data.MaxHealth;
    }

    public void PrepareNextIntent()
    {
        // 队列耗尽后才重新展开并洗牌，保证一轮内每种行为出现次数固定。
        if (ActionQueue.Count == 0)
        {
            RefillAndShuffleActionQueue();
        }

        // 取出即表示该意图已被预告；执行完成后再预告下一个。
        CurrentIntent = ActionQueue.Count == 0 ? null : ActionQueue[0];
        if (ActionQueue.Count > 0)
        {
            ActionQueue.RemoveAt(0);
        }
    }

    private void RefillAndShuffleActionQueue()
    {
        // 将 Resource 配置里的“动作 + Copies”展开成一个平铺列表。
        ActionQueue.Clear();
        foreach (var entry in Data.ActionEntries)
        {
            if (entry.Action is null)
            {
                continue;
            }

            // Copies 是“权重”的可控版本：两份蟹钳就保证一轮中恰好两次蟹钳。
            for (var copy = 0; copy < entry.Copies; copy++)
            {
                ActionQueue.Add(entry.Action);
            }
        }

        for (var i = ActionQueue.Count - 1; i > 0; i--)
        {
            var swapIndex = _random.RandiRange(0, i);
            (ActionQueue[i], ActionQueue[swapIndex]) = (ActionQueue[swapIndex], ActionQueue[i]);
        }
    }
}
