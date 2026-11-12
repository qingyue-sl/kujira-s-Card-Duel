using System.Collections.Generic;
using System.Linq;
using Godot;

namespace KujiraKo;

/// <summary>
/// 全局流程控制器。它在场景切换之间保留本局状态，并按“低→中→高→Boss”生成不重复的三层路线。
/// </summary>
public partial class GameFlow : Node
{
    public static GameFlow? Instance { get; private set; }

    [ExportGroup("角色与牌组")]
    [Export] public int CharacterMaxHealth { get; set; } = 60;
    [Export] public Godot.Collections.Array<CardData> StarterDeck { get; set; } = new();

    [ExportGroup("普通关卡池")]
    [Export] public Godot.Collections.Array<EncounterData> LowNormalEncounters { get; set; } = new();
    [Export] public Godot.Collections.Array<EncounterData> MediumNormalEncounters { get; set; } = new();
    [Export] public Godot.Collections.Array<EncounterData> HighNormalEncounters { get; set; } = new();

    [ExportGroup("精英与Boss")]
    [Export] public Godot.Collections.Array<EncounterData> LowEliteEncounters { get; set; } = new();
    [Export] public Godot.Collections.Array<EncounterData> MediumEliteEncounters { get; set; } = new();
    [Export] public Godot.Collections.Array<EncounterData> HighEliteEncounters { get; set; } = new();
    [Export] public EncounterData? BossEncounter { get; set; }

    public RunState? ActiveRun { get; private set; }
    public bool LastRunWon { get; private set; }
    public int LastCompletedCombatCount { get; private set; }

    private readonly RandomNumberGenerator _random = new();

    public override void _Ready()
    {
        Instance = this;
        _random.Randomize();
    }

    /// <summary>由角色选择页调用，开始一次新冒险并立刻进入第一场遭遇。</summary>
    public void BeginNewRun()
    {
        var startingDeck = StarterDeck.Count > 0 ? StarterDeck : DemoContentFactory.CreateStarterDeck();
        ActiveRun = new RunState(CharacterMaxHealth, startingDeck);
        LastRunWon = false;
        LastCompletedCombatCount = 0;
        BuildRoute(ActiveRun);
        GoToCurrentStep();
    }

    public EncounterData? GetCurrentEncounter()
    {
        if (ActiveRun is null || ActiveRun.StepIndex < 0 || ActiveRun.StepIndex >= ActiveRun.Route.Count)
        {
            return null;
        }

        return ActiveRun.Route[ActiveRun.StepIndex].Encounter;
    }

    public string GetProgressText()
    {
        if (ActiveRun is null)
        {
            return string.Empty;
        }

        var encounter = GetCurrentEncounter();
        if (encounter?.Kind == EncounterKind.Boss)
        {
            return "最终层 · Boss";
        }

        var layer = Mathf.Clamp(ActiveRun.StepIndex / 5 + 1, 1, 3);
        if (encounter?.Kind == EncounterKind.Elite)
        {
            return $"第 {layer} 层 · 精英战";
        }

        if (ActiveRun.Route[ActiveRun.StepIndex].IsRest)
        {
            return $"第 {layer} 层 · 潮汐休整";
        }

        var normalNumber = ActiveRun.StepIndex % 5 + 1;
        return $"第 {layer} 层 · 普通战 {normalNumber}/3";
    }

    public void AddCardToRunDeck(CardData card)
    {
        ActiveRun?.Deck.Add(card);
    }

    public void ContinueAfterVictory(int playerHealth)
    {
        if (ActiveRun is null)
        {
            GetTree().ChangeSceneToFile("res://scenes/main_menu.tscn");
            return;
        }

        ActiveRun.CurrentHealth = Mathf.Max(1, playerHealth);
        LastCompletedCombatCount++;
        ActiveRun.StepIndex++;
        GoToCurrentStep();
    }

    public void ContinueAfterRest()
    {
        if (ActiveRun is null)
        {
            return;
        }

        ActiveRun.StepIndex++;
        GoToCurrentStep();
    }

    /// <summary>战败时稍作停留，确保最后一条伤害飘字与播报能被玩家看见。</summary>
    public async void HandleBattleDefeat(int playerHealth)
    {
        if (ActiveRun is null)
        {
            return;
        }

        ActiveRun.CurrentHealth = Mathf.Max(0, playerHealth);
        await ToSignal(GetTree().CreateTimer(1.35f), SceneTreeTimer.SignalName.Timeout);
        LastRunWon = false;
        GetTree().ChangeSceneToFile("res://scenes/run_summary.tscn");
    }

    public void FinishBossRun(int playerHealth)
    {
        if (ActiveRun is null)
        {
            return;
        }

        ActiveRun.CurrentHealth = Mathf.Max(1, playerHealth);
        LastCompletedCombatCount++;
        LastRunWon = true;
        GetTree().ChangeSceneToFile("res://scenes/run_summary.tscn");
    }

    public void ReturnToMainMenu()
    {
        ActiveRun = null;
        GetTree().ChangeSceneToFile("res://scenes/main_menu.tscn");
    }

    private void BuildRoute(RunState run)
    {
        // 每一层严格取所在难度池的三个不重复普通关，再接一场对应难度精英与一次休整。
        AddLayer(run, LowNormalEncounters, LowEliteEncounters);
        AddLayer(run, MediumNormalEncounters, MediumEliteEncounters);
        AddLayer(run, HighNormalEncounters, HighEliteEncounters);

        if (BossEncounter is not null)
        {
            run.Route.Add(new RunStep(BossEncounter));
        }
        else
        {
            GD.PushError("GameFlow 未配置 BossEncounter，无法完成路线。");
        }
    }

    private void AddLayer(RunState run, Godot.Collections.Array<EncounterData> normalPool, Godot.Collections.Array<EncounterData> elitePool)
    {
        var normals = ShuffleCopy(normalPool).Take(3).ToList();
        if (normals.Count < 3)
        {
            GD.PushError("每个难度普通关池至少需要配置 3 个 EncounterData。");
        }

        foreach (var encounter in normals)
        {
            run.Route.Add(new RunStep(encounter));
        }

        var elite = ShuffleCopy(elitePool).FirstOrDefault();
        if (elite is null)
        {
            GD.PushError("每个难度精英池至少需要配置 1 个 EncounterData。");
        }
        else
        {
            run.Route.Add(new RunStep(elite));
        }

        run.Route.Add(new RunStep());
    }

    private List<EncounterData> ShuffleCopy(IEnumerable<EncounterData> source)
    {
        var copy = new List<EncounterData>(source);
        for (var i = copy.Count - 1; i > 0; i--)
        {
            var swap = _random.RandiRange(0, i);
            (copy[i], copy[swap]) = (copy[swap], copy[i]);
        }

        return copy;
    }

    private void GoToCurrentStep()
    {
        if (ActiveRun is null || ActiveRun.StepIndex >= ActiveRun.Route.Count)
        {
            LastRunWon = true;
            GetTree().ChangeSceneToFile("res://scenes/run_summary.tscn");
            return;
        }

        var step = ActiveRun.Route[ActiveRun.StepIndex];
        GetTree().ChangeSceneToFile(step.IsRest ? "res://scenes/rest.tscn" : "res://scenes/battle.tscn");
    }
}
