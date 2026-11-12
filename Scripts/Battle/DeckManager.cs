using System.Collections.Generic;
using Godot;

namespace KujiraKo;

public sealed class DeckManager
{
    // 抽牌、弃牌和消耗堆全部集中在这里，Battle 不直接操作各个 List。
    // 规则规定的硬上限；DrawCards 会在超过时把翻出的牌丢到弃牌堆。
    public const int HandLimit = 10;

    // 等待被抽到的牌；列表最后一个元素视为牌堆顶。
    public List<CardInstance> DrawPile { get; } = new();
    // 当前可供玩家打出的牌。
    public List<CardInstance> Hand { get; } = new();
    // 已打出或回合结束弃置的普通牌。
    public List<CardInstance> DiscardPile { get; } = new();
    // 消耗牌离开本场战斗后进入这里，不会被洗回抽牌堆。
    public List<CardInstance> ExhaustPile { get; } = new();

    private RandomNumberGenerator _random = new();

    public void InitializeDeck(IEnumerable<CardData> startingDeck, RandomNumberGenerator random)
    {
        // 每场战斗重置四个区域，并接受 Battle 提供的随机源。
        _random = random;
        DrawPile.Clear();
        Hand.Clear();
        DiscardPile.Clear();
        ExhaustPile.Clear();
        // 即使起始牌组重复引用同一份 CardData 资源，也必须创建不同 CardInstance。
        // 这样“同名的第五张打击”与“同名的第一张打击”仍是两张可独立选择的实体牌。
        var nextInstanceId = 1;
        foreach (var cardData in startingDeck)
        {
            DrawPile.Add(new CardInstance(nextInstanceId++, cardData));
        }
        Shuffle(DrawPile);
    }

    public void DrawCards(int count)
    {
        for (var i = 0; i < count; i++)
        {
            if (DrawOne() is null) return;
        }
    }

    /// <summary>
    /// 抽一张实际卡牌并返回它；用于开场和回合开始时的逐张抽牌演出。
    /// 如果牌堆与弃牌堆都为空则返回 null。
    /// </summary>
    public CardInstance? DrawOne()
    {
        if (DrawPile.Count == 0) ReshuffleDiscardIntoDrawPile();
        if (DrawPile.Count == 0) return null;

        var topIndex = DrawPile.Count - 1;
        var card = DrawPile[topIndex];
        DrawPile.RemoveAt(topIndex);
        if (Hand.Count >= HandLimit) DiscardPile.Add(card);
        else Hand.Add(card);
        return card;
    }

    // 出牌时从手牌移除；返回 false 代表该卡已不在手中。
    public bool RemoveFromHand(CardInstance card) => Hand.Remove(card);

    // 非消耗牌结算完成后的去向。
    public void MoveToDiscard(CardInstance card) => DiscardPile.Add(card);

    // 消耗牌结算完成后的去向。
    public void MoveToExhaust(CardInstance card) => ExhaustPile.Add(card);

    public void DiscardHandAtTurnEnd()
    {
        // 第一版没有保留（Retain）关键词，所以回合结束时全部弃置。
        DiscardPile.AddRange(Hand);
        Hand.Clear();
    }

    public void ReshuffleDiscardIntoDrawPile()
    {
        // 只在抽牌堆为空时由 DrawCards 调用；直接调用它会改变规则时序。
        if (DiscardPile.Count == 0)
        {
            return;
        }

        DrawPile.AddRange(DiscardPile);
        DiscardPile.Clear();
        Shuffle(DrawPile);
    }

    private void Shuffle(List<CardInstance> cards)
    {
        // Fisher-Yates 洗牌：每种排列出现的概率相同。
        for (var i = cards.Count - 1; i > 0; i--)
        {
            var swapIndex = _random.RandiRange(0, i);
            (cards[i], cards[swapIndex]) = (cards[swapIndex], cards[i]);
        }
    }
}
