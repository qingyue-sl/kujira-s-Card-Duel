namespace KujiraKo;

/// <summary>
/// 一张实际进入牌堆的卡牌实例。
///
/// CardData 是可复用的静态模板资源；同一模板可以在牌组里出现多次。此类为每一张
/// 实体副本分配唯一编号，因此 UI 能明确知道“选中的是哪一张”，而不是误选所有同名牌。
/// </summary>
public sealed class CardInstance
{
    /// <summary>本局牌组内唯一的编号，仅用于运行时识别和调试。</summary>
    public int InstanceId { get; }
    /// <summary>本卡引用的只读模板资源，负责名称、费用和效果配置。</summary>
    public CardData Data { get; }

    public CardInstance(int instanceId, CardData data)
    {
        InstanceId = instanceId;
        Data = data;
    }
}
