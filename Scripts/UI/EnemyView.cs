using System.Text;
using Godot;

namespace KujiraKo;

/// <summary>
/// 敌人视图类：把 Enemy 的运行时数据翻译成文字和按钮状态
/// </summary>
public partial class EnemyView : Control
{
    // ui层
    [Export] public Label? NameLabel { get; set; }          // 显示敌人名字的标签
    [Export] public Label? HealthLabel { get; set; }        // 显示生命值的标签
    //[Export] public Label? ArmorLabel { get; set; }       // 显示护甲的标签
    [Export] public Label? IntentLabel { get; set; }        // 显示敌人下一步意图的标签
    [Export] public Button? TargetButton { get; set; }      // 选中按钮，用来接收“单体攻击”


    // 指针层
    private Enemy? _enemy;              // 指向实际敌人实例
    private Battle? _battle;            // 指向战斗管理器

    /// <summary>返回敌人面板上方的飘字位置，供 BattleUI 的统一反馈层使用。</summary>
    public Vector2 GetFeedbackPosition() => GlobalPosition + new Vector2(Size.X * 0.5f - 28, -8);

    /// <summary>敌人按遭遇编队出现时播放一次缩放、淡入动画。</summary>
    public void PlaySpawn()
    {
        PivotOffset = Size * 0.5f;
        Scale = new Vector2(0.72f, 0.72f);
        Modulate = new Color(1, 1, 1, 0);
        var tween = CreateTween();
        tween.SetParallel(true);
        tween.TweenProperty(this, "scale", Vector2.One, 0.32f);
        tween.TweenProperty(this, "modulate:a", 1f, 0.32f);
    }

    public override void _Ready()
    {
        // 注册点击事件
        if (TargetButton is not null)
        {
            TargetButton.Pressed += OnTargetPressed;
        }
    }

    /// <summary>
    /// 绑定敌人实例和战斗管理器
    /// </summary>
    public void Bind(Enemy enemy, Battle battle)
    {
        _enemy = enemy;
        _battle = battle;
        Refresh();
    }

    /// <summary>
    /// 刷新敌人ui显示
    /// </summary>
    public void Refresh()
    {
        // 敌人死亡时隐藏视图，但不从 Battle.Enemies 删除，以保留死亡后的状态给动画使用。
        if (_enemy is null)
        {
            return;
        }

        if (NameLabel is not null) NameLabel.Text = _enemy.DisplayName;
        if (HealthLabel is not null)
        {
            string armorText = _enemy.Armor > 0 ? $" (+{_enemy.Armor})" : "";
            HealthLabel.Text = $"{_enemy.CurrentHealth}/{_enemy.MaxHealth}{armorText}";
        }
        if (IntentLabel is not null) IntentLabel.Text = FormatIntent(_enemy.CurrentIntent);
        var canBeSelectedTarget = _battle?.SelectedCard?.Data.TargetType == TargetType.SingleEnemy
            && _battle.CurrentPhase == BattlePhase.PlayerTurn
            && _battle.IsResolving == false;
        if (TargetButton is not null) TargetButton.Disabled = _enemy.IsDead || _battle?.IsResolving == true;
        // 玩家选择单体攻击牌后，用整块面板的暖色高亮明确提示“下一步点这里”。
        Modulate = canBeSelectedTarget ? new Color(1.18f, 1.08f, 0.68f) : Colors.White;
        Visible = !_enemy.IsDead;
    }

    /// <summary>
    /// 点击事件
    /// </summary>
    private void OnTargetPressed()
    {
        // 若当前选中的牌是单体牌，Battle 会在这里完成目标校验和结算。
        if (_battle is not null && _enemy is not null)
        {
            _battle.TryPlaySelectedCard(_enemy);
        }
    }

    /// <summary>
    /// 格式化敌人意图
    /// </summary>
    private static string FormatIntent(EnemyActionData? intent)
    {
        // 此处仅生成文字；图标可根据 intent.IntentType 在 UI 层额外选择。
        if (intent is null)
        {
            return "无意图";
        }

        // 意图必须把具体数值展示出来，不能只写“攻击”或“防御”。
        var parts = new StringBuilder(intent.ActionName);
        foreach (var effect in intent.Effects)
        {
            switch (effect.EffectType)
            {
                case EffectType.DealDamage:
                    parts.Append($"\n攻击 {effect.Value}");
                    if (effect.HitCount > 1) parts.Append($" × {effect.HitCount}");
                    break;
                case EffectType.GainArmor:
                    parts.Append($"\n护甲 +{effect.Value}");
                    break;
                case EffectType.ApplyStatus:
                    parts.Append($"\n{Battle.StatusName(effect.StatusType)} +{effect.StatusAmount}");
                    break;
            }
        }

        return parts.ToString();
    }
}
