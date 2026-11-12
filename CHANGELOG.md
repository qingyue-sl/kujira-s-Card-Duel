# 变更日志

本文件记录 Codex 对项目源码、场景和功能的实际修改；不记录 Godot 自动生成的 `.godot/` 构建缓存。

## 2026-11-12 — 最小完整冒险流程与战斗演出

### 新增

- `Scripts/Run/GameFlow.cs`、`Scripts/Run/RunState.cs`
  - 新增跨场景的一局冒险状态：当前生命、长期牌组、路线、战斗进度与结算结果。
  - 固定生成三层路线：低级普通关 3 场 → 精英 → 休整；中级重复；高级重复；最后进入 Boss。
  - 每档普通池随机抽取 3 个不重复关卡；当前每档恰好配置 3 个，因此每局都会覆盖该档内容但顺序随机。
- `Scripts/Data/EncounterData.cs`
  - 新增纯遭遇配置资源，只记录关卡类别、难度档和敌人编队。
- `assets/encounters/`
  - 新增 9 个普通遭遇、3 个精英遭遇与 1 个 Boss 遭遇资源。
- `assets/enemies/elites/`、`assets/enemies/boss/`
  - 新增甲壳守卫、风暴水母、潮汐船长和深渊巨兽的敌人资源。
- `scenes/main_menu.tscn`、`scenes/rest.tscn`、`scenes/run_summary.tscn`、`scenes/game_flow.tscn`
  - 新增唯一角色选择主界面、休整页面、本局结算页与全局流程节点。
- `Scripts/UI/MainMenu.cs`、`Scripts/UI/RestScreen.cs`、`Scripts/UI/RunSummary.cs`
  - 接入开始冒险、恢复生命、删牌/跳过删牌、继续路线、重开与返回主界面。
- `Scripts/Battle/CombatFeedback.cs`
  - 新增伤害、格挡、护甲、反击与治疗的统一飘字数据。

### 修改

- `project.godot`
  - 主场景改为角色选择主界面；注册 `GameFlow` Autoload。
- `Scripts/Battle/Battle.cs`、`Scripts/Battle/DeckManager.cs`
  - 敌人按编队顺序逐只出现；开局和每回合抽牌改为逐张进入手牌。
  - 敌人按列表逐个行动，每名敌人完成行动后暂停 1.25 秒；下一玩家回合不再覆盖最后一条敌人结算播报。
  - 新增治疗效果与战斗反馈事件；胜利后进入奖励结算，失败后进入本局结算。
- `Scripts/UI/BattleUI.cs`、`Scripts/UI/EnemyView.cs`、`Scripts/UI/CardView.cs`
  - 不再销毁重建所有手牌/敌人视图，支持卡牌逐张抽取、敌人逐只出现。
  - 新增敌我伤害、格挡、护甲、反击与绿色治疗飘字。
  - 敌人状态意图改为中文显示；新增当前层数/关卡名称提示。
- `scenes/battle.tscn`、`Scripts/UI/RewardScreen.cs`
  - 奖励界面改为“选卡或跳过 → 继续冒险”；Boss 胜利改为“查看本局结算”。
  - 新增全屏飘字层与关卡进度标签。
- `scenes/ui/card.tscn`
  - 卡名改为占用整张卡牌标题区域并居中；费用徽章缩小、贴近左上角，避免挤偏名称。

## 2026-11-12 — 修复手牌重叠与路线称谓

### 修改

- `Scripts/UI/CardView.cs`
  - 移除刷新时强制把卡牌位置设为 `(0, 0)` 的逻辑；该逻辑会覆盖 `HBoxContainer` 的排版，导致多张手牌重叠为一张。
- `Scripts/Run/GameFlow.cs`、`scenes/battle.tscn`
  - 玩家可见的路线称谓由“循环”统一调整为“层”，例如“第 2 层 · 精英战”。

### 验证

- `dotnet build --no-restore`：0 个警告、0 个错误。
- Godot 4.7.2 无界面冒烟测试：首回合抽到 6 张，手牌区创建 6 个卡牌视图，横坐标为 184、338、492、646、800、954，确认未重叠。
- 同一测试确认路线共有 9 场普通战、3 场精英战、3 次休整与 1 场 Boss，且低/中/高难度普通战各 3 场；进度文字包含“第 1 层”且不含“循环”。

### 验证

- `dotnet build --no-restore`：0 个警告、0 个错误。
- Godot 4.7.2 无界面重导入关卡、敌人、场景资源并启动新的主菜单：未输出资源、脚本、节点路径或启动错误。

## 2026-11-11 — 战斗信息层级、资源化与交互反馈

### 新增

- `assets/cards/starter/`
  - 将三种起始牌（潮汐打击、鲸壳防御、反击架势）保存为可直接在 Inspector 编辑的 `CardData.tres`。
- `assets/cards/rewards/`
  - 将 12 张奖励卡保存为独立的 `CardData.tres`；奖励界面现在直接引用这些资源。
- `assets/enemies/`
  - 将礁石蟹、深海水母、潮汐掠夺者保存为独立的 `EnemyData.tres`，包含固定次数、随机排列的行为序列。
- `Scripts/Battle/CardInstance.cs`
  - 为同一 `CardData` 模板在牌组中的每个实体副本创建唯一运行时实例，避免同名牌共用选中状态。

### 修改

- `Scripts/Battle/DeckManager.cs`、`Scripts/Battle/Battle.cs`
  - 牌堆、手牌、弃牌与消耗堆改为保存 `CardInstance`；选中一张同名牌时只会高亮该实例。
  - 新增回合编号；每回合开始、结束、玩家出牌与敌人行动均会生成包含结果的战斗播报。
  - 场景可直接配置敌人资源池，并从中随机选择本场普通敌人。
- `Scripts/UI/CardView.cs`、`scenes/ui/card.tscn`
  - 卡费移到左上角独立圆角标记；名称、描述、类型分区显示，修复长描述挤压费用与文字错位。
  - 修复不可出牌卡被“取消选中”逻辑刷回白色的问题；选中牌会上抬并增亮。
- `Scripts/UI/BattleUI.cs`、`Scripts/UI/EnemyView.cs`、`scenes/battle.tscn`、`scenes/ui/enemy.tscn`
  - 抽牌堆与弃牌堆拆分显示为“抽牌堆：--”“弃牌堆：--”。
  - 结束回合按钮显示当前回合数，并上移到行动引导栏旁，避免与首回合第六张手牌重叠。
  - `HandHint` 改为靠近手牌的动态操作引导；选中单体攻击牌时，敌方面板高亮以提示下一步目标。
  - 敌人意图与生命/护甲标签扩大，避免多行意图、护甲数字被截断。

### 验证

- `dotnet build --no-restore`：0 个警告、0 个错误。
- Godot 4.7.2 无界面重新导入资源并启动主场景：未输出资源格式、节点路径或启动错误。

## 2026-11-11 — 第一版可玩战斗原型

### 新增

- `Scripts/Battle/DemoContentFactory.cs`
  - 提供无需 Inspector 资源配置即可运行的 10 张初始牌、3 种随机普通敌人和 12 张奖励牌。
  - 保留 `CardData` / `EnemyData` Resource 架构；未来在场景中填写资源即可覆盖演示内容。
- `CHANGELOG.md`
  - 作为之后所有项目修改的持续记录。

### 修改

- `Scripts/Battle/Battle.cs`
  - 当场景未配置起始牌或敌人时，自动使用演示内容，使主场景启动后立即进入可玩的战斗。
  - 新增本次运行的长期牌组；奖励选牌会被加入其中，重开同一场原型战斗时会保留新卡。
- `Scripts/UI/RewardScreen.cs`
  - 当 Inspector 未配置奖励池时，自动使用演示奖励牌池；胜利后可稳定显示三张不重复候选牌。
  - 接入奖励选牌事件，将选择的卡交给 `Battle` 加入运行时牌组。
- `scenes/battle.tscn`
  - 重整为完整的 1280×720 战斗布局：玩家状态、敌人区、战斗提示、牌组计数、手牌区、结束回合按钮和胜利奖励遮罩。
  - 接入 `BattleUI` 的牌堆计数与提示文字引用。
  - 接入 `RewardScreen`，战斗胜利时显示三选一卡与跳过按钮。
  - 恢复原主场景 UID `uid://ctx8gugf2v86y`，修复 `project.godot` 指向主场景时的 UID 识别错误。
- `scenes/ui/card.tscn`
  - 修复卡牌按钮的鼠标过滤设置，使点击事件能够触发。
- `scenes/ui/enemy.tscn`
  - 让文字标签忽略鼠标事件，确保点击敌人面板时可以正确作为单体攻击目标。

### 验证

- 修复演示奖励中两张消耗牌的 C# 命名参数顺序，随后重新构建 C# 项目。
- `dotnet build --no-restore`：0 个警告、0 个错误。
- Godot 4.7.2 无界面加载主场景：成功，未输出场景、节点路径或启动错误。
- 尚待在打开的 Godot 编辑器中手动试玩：首回合抽 6、后续抽 4、点击出牌、敌人意图、胜利奖励三选一。
