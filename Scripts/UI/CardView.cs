using Godot;

namespace KujiraKo;

public partial class CardView : Control
{
    // 这是“显示一张手牌”的脚本；它不会计算伤害或修改牌库。
    // 以下四个导出字段都在 card.tscn 的 Inspector 中拖入对应控件。
    [Export] public Label? NameLabel { get; set; }
    [Export] public Label? CostLabel { get; set; }
    [Export] public Label? DescriptionLabel { get; set; }
    // 卡牌底部的类型提示，例如“攻击”或“技能”。
    [Export] public Label? TypeLabel { get; set; }
    [Export] public Button? SelectButton { get; set; }

    // 此卡牌视图当前代表的模板数据；战斗中不修改 CardData 本身。
    public CardInstance? Instance { get; private set; }

    // 由 Bind 注入的战斗管理器；点击事件只转交给它。
    private Battle? _battle;
    // 两个状态分开记录再统一绘制，避免“置灰后又被取消选中逻辑刷白”的显示错误。
    private bool _isPlayable;
    private bool _isSelected;

    public override void _Ready()
    {
        // 注册点击事件
        if (SelectButton is not null)
        {
            SelectButton.Pressed += OnPressed;
        }
    }

    /// <summary>
    /// 绑定卡牌数据和战斗管理器
    /// </summary>
    public void Bind(CardInstance instance, Battle battle)
    {
        // BattleUI 每次重建手牌时调用，让视图知道自己代表哪张牌。
        Instance = instance;
        _battle = battle;
        Refresh();
    }

    // 刷新卡牌ui显示
    public void Refresh()
    {
        // 统一从 Data 与 Battle 读取最新状态并更新颜色、文字、可点击性。
        if (Instance is null)
        {
            return;
        }

        var data = Instance.Data;
        if (NameLabel is not null) NameLabel.Text = data.DisplayName;
        if (CostLabel is not null) CostLabel.Text = data.Cost.ToString();
        if (DescriptionLabel is not null) DescriptionLabel.Text = data.Description;
        if (TypeLabel is not null) TypeLabel.Text = data.CardType == CardType.Attack ? "攻击" : "技能";

        // 即使按钮置灰，Battle 内部仍会再次校验；UI 只是第一层反馈。
        var playable = _battle is not null
            && _battle.CurrentPhase == BattlePhase.PlayerTurn
            && !_battle.IsResolving
            && _battle.Player.CanAfford(data);
        SetPlayable(playable);
        SetSelected(ReferenceEquals(_battle?.SelectedCard, Instance));
    }

    public void SetSelected(bool selected)
    {
        _isSelected = selected;
        RefreshAppearance();
    }

    public void SetPlayable(bool playable)
    {
        // 不可出牌时禁用按钮并置灰；Battle 逻辑仍会再次校验。
        _isPlayable = playable;
        if (SelectButton is not null) SelectButton.Disabled = !playable;
        RefreshAppearance();
    }

    private void OnPressed()
    {
        // 自我牌会在第一次选择后立即结算；其他牌维持“先选牌，再选目标”的交互。
        if (Instance is not null && _battle is not null)
        {
            var wasSelected = ReferenceEquals(_battle.SelectedCard, Instance);
            // 自我目标卡不必额外点玩家头像，选择后立即结算，减少一次无意义点击。
            if (_battle.TrySelectCard(Instance) && !wasSelected && Instance.Data.TargetType == TargetType.Self)
            {
                _battle.TryPlaySelectedCard();
            }
        }
    }

    private void RefreshAppearance()
    {
        // 位置必须完全交给 HandArea（HBoxContainer）计算；若在这里写 Position，所有手牌
        // 都会被重置到 (0, 0) 而重叠。选中效果改用颜色，避免破坏容器布局。
        Modulate = !_isPlayable
            ? new Color(0.52f, 0.52f, 0.58f)
            : _isSelected ? new Color(1.18f, 1.18f, 0.74f) : Colors.White;
    }

    /// <summary>抽到新牌时的轻微放大动画；不重建既有手牌，避免开场抽牌跳动。</summary>
    public void PlayDraw()
    {
        PivotOffset = Size * 0.5f;
        Scale = new Vector2(0.82f, 0.82f);
        var tween = CreateTween();
        tween.TweenProperty(this, "scale", Vector2.One, 0.2f);
    }
}
