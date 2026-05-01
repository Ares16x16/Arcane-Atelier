# Battle 核心架构

> 当前 Battle 模块中除表现与交互层之外的三层：数据与内容层、战斗模拟层、场景编排层。
> Last Updated: 2026-04-30

---

## 1. 总体分层

Battle 的核心逻辑可以拆成三层：

- 数据与内容层
- 战斗模拟层
- 场景编排层

这三层共同负责：

- 定义战斗内容
- 运行战斗规则
- 组织 BattleScene 的进入、运行与结束

表现层会消费这三层的输出，但不改变这三层的规则。

---

## 2. 数据与内容层

数据与内容层负责“战斗里有哪些东西，以及这些东西如何被配置”。

当前核心类型如下：

- `BattleBossDefinition`
- `BattleCardDefinition`
- `BattleCardEffectTemplate`
- `BattlePresentationProfile`
- `BattleStatusEffectDefinition`
- `BattleContentDatabase`

### 2.1 `BattleBossDefinition`

定义一个敌人或 Boss 的基础战斗信息：

- `BossId`
- `DisplayName`
- `MaxHealth`
- `Element`
- `EncounterType`
- `DifficultyRank`
- `EnemyArchetype`
- `ActionPattern`
- `phase2ActionPattern`
- `DefeatRewardId`

虽然类型名仍然使用 `Boss`，但当前同一条内容链路同时服务：

- 真正的 Boss
- 普通敌人

### 2.2 `BattleCardDefinition`

**Per-card 指令化定义**，与 Workshop 产出的每张卡牌一一对应。

当前为 23 张卡牌生成了独立定义：

- 20 张完整 Workshop 卡牌（8 Basic + 8 Intermediate + 4 Advanced）
- 3 张运行时回退卡牌（Flame Bolt / Frost Sigil / Arcane Ward）

每张定义包含：

- `BattleCardId`：与 `WorkshopBattleCardEntry.CardId` 对齐
- `DisplayName`
- `Element` / `Tier`
- `Instructions[]`：效果指令列表（如 Damage → ApplyStatus）

每条 `BattleEffectInstruction` 现在还包含：

- `Target`：`Self` / `Opponent`
- `Value`
- `HitCount`
- `StatusId`
- `Duration`

这让每张卡牌可以有**多段组合效果**。例如 `Cinder Dart` 的指令是：

```text
[Damage 8 x1] → [ApplyStatus "Burn" 2回合]
```

### 2.3 `BattleEffectInstruction`

效果指令的基本单元，支持五种类型：

- `Damage`：对目标造成伤害（支持 `HitCount` 与元素克制）
- `Heal`：恢复生命（支持 `HitCount`）
- `Shield`：获得护盾（支持 `HitCount`）
- `ApplyStatus`：附加状态效果（支持目标与强度）
- `DrawCard`：抽牌（预留）

### 2.4 `BattleCardEffectTemplate`

负责把 Workshop 传来的 `WorkshopBattleCardEntry` 转成可结算的 Battle 效果。

**当前角色变化**：它不再是主路径，而是 **fallback 卡组专用**。

当 `BattleDeckController` 按 `CardId` 找不到 `BattleCardDefinition` 时，才会回退到按 `Role` 匹配旧模板。

当前 fallback deck 本身也改为直接复用 Workshop 的 8 张基础卡定义，当前默认分布为：

- 3 × `Cinder Dart`
- 2 × `Zephyr Cut`
- 2 × `Frost Pin`
- 2 × `Volt Javelin`
- 2 × `Tidal Mend`
- 2 × `Lumen Prayer`
- 2 × `Stoneguard Sigil`
- 1 × `Gloam Ward`

旧模板路径仍保留，但已降级为安全网，不再是正常内容流。

### 2.5 `BattlePresentationProfile`

虽然它会被表现层消费，但它本质上仍然属于内容配置层。

它把某个 `bossId` 和表现资源绑定起来，包括：

- `BossSprite`
- `BackgroundSprite`
- `BossPosition`
- `BossScale`
- `BackgroundScale`

当前 4 个 encounter 对应的 `Presentation_*` 已全部接入实际 sprite 资源，运行时切换 encounter 时会优先按这些 profile 更新敌人与背景表现；场景上的同名 sprite 引用仅保留为 fallback。

### 2.6 `BattleStatusEffectDefinition`

状态效果的数据定义 ScriptableObject，描述一种状态如何触发、持续多久、每次触发产生什么效果。

字段包括：

- `StatusId`：唯一标识（如 `"Burn"`、`"Regen"`）
- `DisplayName`
- `Trigger`：触发时机（OnTurnStart / OnTurnEnd / OnHitTaken / OnHitDealt / OnShieldBroken）
- `TickEffect`：每次触发时执行的效果指令
- `IsStackable` / `MaxStackCount`

当前生成了 16 个状态定义，覆盖全部 20 张 Workshop 卡牌的关键词。

Battle 目前对关键词采用“当前卡池优先”的实用型实现：

- `Burn` / `Regen` / `Scald`：按回合触发
- `Freeze` / `Stun`：在敌方行动开始时消费并阻止一次行动
- `Expose` / `Bulwark` / `Shade` / `Veil`：修正后续承伤
- `Ward`：修正后续护盾收益
- `Bless` / `Radiance`：修正后续治疗收益
- `Shock` / `Rend` / `Static Shell`：以一次性附加伤害或反制效果落地

### 2.7 `BattleContentDatabase`

`BattleContentDatabase` 是 Battle 内容入口，统一持有：

- `bosses`
- `cardDefinitions`
- `cardEffectTemplates`（fallback 用）
- `presentationProfiles`
- `statusEffectDefinitions`

它提供常用查找能力：

- `FindBoss(string bossId)`
- `FindCardDefinition(string battleCardId)`
- `FindStatusEffectDefinition(string statusId)`
- `FindTemplate(string cardId)`
- `FindTemplateByRole(...)`
- `FindPresentationProfile(string bossId)`

BattleScene 启动时依赖它来拿敌人定义、卡牌定义和表现资源。

---

## 3. 战斗模拟层

战斗模拟层负责“战斗如何按规则运行”。

当前核心类型如下：

- `BattleSimulation`
- `BattleDeckController`
- `BattleBossAI`
- `BattleEffectExecutor`
- `BattleStatusEffectController`
- `BattleActionResolver`
- `BattleElementUtility`
- `BattleUnit`
- `BattleActionResolution`

### 3.1 `BattleSimulation`

`BattleSimulation` 是 Battle 运行时的权威状态机。

它管理：

- `Player`
- `Boss`
- `Deck`
- `BossAI`
- `StatusController`（新增）
- `State`
- `TurnsElapsed`
- `ActionPoints` / `MaxActionPoints`
- 累计战斗统计

当前状态现在有四个：

- `WaitingForPlayer`
- `BossTurnPending`
- `ResolvingBoss`
- `BattleEnded`

玩家操作最终都会进入：

- `TryPlayCard(int handIndex)`
- `EndTurn()`
- `AdvancePendingTurn()`

然后由它决定：

- 是否进入敌方回合
- 是否结束战斗
- 是否广播结果事件

**双路径执行**

`TryPlayCard` 内部现在支持两条效果执行路径：

- **Path A/B（新系统）**：如果 `Deck.LastPlayedDefinition` 不为 null，调用 `BattleEffectExecutor.Execute(...)` 执行指令列表
- **Path C（fallback）**：如果走的是旧模板，继续调用 `BattleActionResolver.ResolvePlayerEffect(...)`

两条路径的结果统一通过 `PlayerActionResolved` 事件广播。

**状态效果触发**

`BattleSimulation` 在以下时机调用 `StatusEffectController.Tick(...)`：

- 玩家回合结束（`OnTurnEnd` on Player）
- 敌人回合开始（`OnTurnStart` on Boss）
- 敌人回合结束（`OnTurnEnd` on Boss）
- 玩家下一回合开始（`OnTurnStart` on Player）

此外：

- 玩家/敌人的直接数值结算会通过 `BattleStatusEffectController` 查询伤害、护盾、治疗修正
- `Freeze` / `Stun` 在 `BattleBossAI.ExecuteNextAction()` 前被消费
- 旧模板 fallback 路径也恢复为真实伤害 / 回复 / 护盾结算，不再返回空效果

**AP 机制**

- 每回合开始时恢复 `MaxActionPoints`（固定为 3）。
- `TryPlayCard` 会先按手牌索引查询 AP 消耗，再检查当前 AP 是否足够。
- AP 校验发生在真正移除手牌之前；若 AP 不足，则不会消耗手牌。
- 只有在卡牌成功打出后，才会扣除对应 AP。
- 当 AP 降至 0 时，不再同步立即执行敌方动作，而是先进入 `BossTurnPending`。
- `EndTurn()` 允许玩家主动结束回合；弃牌重抽后同样先进入 `BossTurnPending`。
- `AdvancePendingTurn()` 由场景编排层在短暂表现过渡后调用，再真正执行敌方动作。

**敌方回合过渡**

Battle 当前把“敌方回合开始”拆成了两段：

- 模拟层负责把状态从 `WaitingForPlayer` 切到 `BossTurnPending`
- 场景编排层负责展示 `Enemy Turn` 提示、intent 预警和短暂 windup
- 过渡结束后，再调用 `AdvancePendingTurn()` 进入 `ResolvingBoss`

这让回合切换不再是同步瞬时完成，表现层有明确窗口去展示敌方回合反馈。

### 3.2 `BattleActionResolution`

`BattleActionResolution` 现在不仅承载数值结算结果，也承载表现层消费的反馈上下文。

除原有字段外，还补充了：

- `SourceTarget`
- `Target`
- `FeedbackKind`
- `PrimaryText`
- `StatusId`
- `StatusDuration`

这让表现层可以在不回读具体业务分支的前提下，统一驱动：

- 伤害 / 治疗 / 护盾浮动数字
- 状态附加提示
- 状态 tick 提示
- 敌方动作 callout

### 3.2 `BattleDeckController`

负责牌库循环：

- Draw pile
- Hand
- Discard pile

支持：

- 开局抽 5 张
- 出牌后补 1 张
- 结束回合时弃掉整手并重抽
- 抽空后把弃牌洗回抽牌堆

**双路径查找（新增）**

`BattleDeckController` 现在采用双路径查找机制：

1. 先按 `CardId` 查找 `BattleCardDefinition`（Path A/B）
2. 找不到时回退到按 `Role` 查找 `BattleCardEffectTemplate`（Path C）

`LastPlayedDefinition` 属性会记录最近一次出牌匹配到的 `BattleCardDefinition`，供 `BattleSimulation` 判断走哪条执行路径。

fallback deck 的构建现在也收敛成 `AddFallbackCard(...)` 辅助方法，避免手写重复条目。

AP 消耗查询：

- `Attack` → 2 AP
- `Defense` / `Healing` → 1 AP

此外，当前还存在按手牌索引读取 AP 费用的只读查询：

- `TryGetActionPointCost(int handIndex, out int actionPointCost)`

这个入口主要用于出牌前校验和 HUD 可用性判断，不会修改牌库状态。

如果没有 Workshop payload，会生成 fallback deck（采用 Workshop 基础卡牌定义）：

- 3 张 Cinder Dart（Fire，8 伤害 + Burn，2 AP）
- 2 张 Zephyr Cut（Wind，5x2 伤害 + Expose，2 AP）
- 2 张 Frost Pin（Ice，4x2 伤害 + Slow，2 AP）
- 2 张 Volt Javelin（Thunder，7 伤害 + Shock，2 AP）
- 2 张 Tidal Mend（Water，6 治疗 + Regen，1 AP）
- 2 张 Lumen Prayer（Light，5x2 治疗 + Bless，1 AP）
- 2 张 Stoneguard Sigil（Earth，7 护盾 + Bulwark，1 AP）
- 1 张 Gloam Ward（Dark，6 护盾 + Veil，1 AP）

Battle 当前也不再是“单场单敌人结束”，而是固定连续 encounter：

- `Ash Imp`
- `Mist Leech`
- `Moss Shell`
- `Corrupted Earth Golem`

中间 encounter 胜利时，`BattleSceneController` 会直接切到下一个敌人；只有击败最后的 Boss 才提交最终 Victory。

跨 encounter 当前保留：

- 玩家剩余 HP
- 当前牌库 / 手牌 / 弃牌堆进度
- 全局累计战斗统计

跨 encounter 当前重置：

- 玩家护盾
- 玩家临时状态
- 当前敌人单位与其 AI

### 3.3 `BattleBossAI`

当前敌人 AI 不是动态决策，而是固定循环行动表。

它从 `BattleBossDefinition.ActionPattern` 读取有序动作列表，并循环执行。

可提供：

- `ExecuteNextAction()`
- `PeekNextAction()`

其中 `PeekNextAction()` 主要服务表现层的 intent 展示。

### 3.4 `BattleEffectExecutor`

**新增的统一效果执行器**，采用 Command Pattern。

它持有一个 `Dictionary<BattleEffectType, IBattleEffectCommand>`，当前注册了四个命令：

- `DamageEffectCommand`：计算伤害（含 `HitCount` 和元素克制）
- `HealEffectCommand`：恢复生命
- `ShieldEffectCommand`：获得护盾
- `ApplyStatusEffectCommand`：附加状态效果

执行流程：

```text
BattleCardDefinition.Instructions[]
        ↓
foreach instruction → Commands[instruction.Type].Execute(...)
        ↓
List<BattleActionResolution>
```

这是现在**玩家卡牌的主执行路径**。

### 3.5 `BattleStatusEffectController`

**新增的状态效果管理器**，负责：

- **施加状态**：`Apply(target, statusId, duration, caster)`
  - 支持同类型状态堆叠（若 `IsStackable`）
  - 支持刷新持续时间
- **按触发时机结算**：`Tick(trigger, unit)`
  - 遍历目标身上所有匹配该触发时机的状态实例
  - 执行 `TickEffect`（伤害/治疗/护盾/其他）
  - 减少 `RemainingDuration`，到期后自动移除
- **查询状态**：`GetEffects(unit)`
- **清空状态**：`ClearEffects(unit)`

当前支持的触发时机：

- `OnTurnStart`
- `OnTurnEnd`
- `OnHitTaken`
- `OnHitDealt`
- `OnShieldBroken`

当前状态 tick 还会生成 `BattleActionResolution`，让表现层可以直接为状态伤害 / 治疗 / 护盾绘制反馈。

### 3.6 `BattleActionResolver`

负责把"敌方动作"结算成对单位的真实影响。

**当前角色变化**：玩家卡牌已完全统一走 `BattleEffectExecutor`，`BattleActionResolver` 现在**仅服务于敌人 AI 动作**。

`ResolvePlayerEffect` 仍保留为安全回退入口，若被调用会输出警告日志，提示配置异常。

敌方动作支持：

- `Attack`
- `Defend`
- `Heal`
- `Special`

其中：

- 玩家攻击会应用元素修正（fallback 路径仍保留此逻辑）
- 敌方攻击当前仍然是平伤

### 3.7 `BattleElementUtility`

负责元素克制关系与倍率：

- Water ↔ Fire
- Wind ↔ Earth
- Ice ↔ Thunder
- Light ↔ Dark

当前规则：

- 优势：+25%
- 劣势：-25%
- 中立：不修正

### 3.8 `BattleUnit`

`BattleUnit` 是玩家和敌人共用的数值模型。

包含：

- `DisplayName`
- `MaxHealth`
- `CurrentHealth`
- `Shield`
- `Element`
- `IsAlive`
- `StatusEffects`（新增）：当前生效的状态实例列表
- `StatusEffectController`（新增）：状态控制器引用

提供：

- `TakeDamage(int)`：现在会在伤害结算后触发 `OnHitTaken` 和 `OnShieldBroken`
- `Heal(int)`
- `AddShield(int)`

它是整个 Battle 规则层中最基础的承载对象。状态效果的触发事件通过 `StatusEffectController` 注入，不破坏 `BattleUnit` 本身的独立性。

---

## 4. 场景编排层

场景编排层负责“BattleScene 什么时候启动、如何拿输入、如何把战斗系统接起来、如何输出结果”。

当前核心类型如下：

- `BattleSceneController`
- `BattleResult`
- `BattleResultBridge`

### 4.1 `BattleSceneController`

`BattleSceneController` 是 BattleScene 的唯一正式入口。

它负责：

- 初始化玩家
- 尝试消费 `WorkshopBattlePayloadBridge`
- 从 `BattleContentDatabase` 查找当前敌人
- 创建 `BattleDeckController`
- 创建 `BattleBossAI`
- 创建 `BattleSimulation`
- 初始化 `BattleVisualManager`
- 初始化 `BattleHudPresenter`
- 接收键盘输入
- 向表现层暴露当前输入入口与可用性查询
- 监听 Battle 事件并记录 recent events
- 在结束时提交结果

它不负责真正的规则计算，但负责把所有运行时部件接成一条链。

当前 HUD 依赖的公开入口主要包括：

- `TryPlayCardFromHud(int handIndex)`
- `CanPlayCard(int handIndex)`
- `EndTurnFromHud()`

这些接口把表现层输入约束在场景编排层，再由编排层转发到 `BattleSimulation`。

### 4.2 Workshop -> Battle 入口

Battle 的场景入口是单向的：

- Workshop 构建卡牌负载
- `WorkshopBattlePayloadBridge` 暂存数据
- `BattleSceneController.Awake()` 调用 `TryConsume(...)`

消费成功时：

- 使用 payload 中的卡牌构建牌库

消费失败时：

- 记录 warning
- 回退到 fallback deck

### 4.3 Battle -> 外部系统出口

战斗结束后，`BattleSimulation` 会生成 `BattleResult`，再由：

- `BattleResultBridge.Commit(result)`

对外暴露。

当前 `BattleResult` 包含：

- `ResultType`
- `BossId`
- `BossDisplayName`
- `TotalDamageDealt`
- `TotalHealingDone`
- `TotalShieldGained`
- `CardsPlayed`
- `TurnsElapsed`
- `DefeatRewardId`

这个出口已经存在，但“提交结果后如何切回 Workshop”还没有完成。

---

## 5. 当前核心运行链路

当前 Battle 的主链路如下：

```text
WorkshopBattlePayloadBridge
        ↓
BattleSceneController
        ↓
BattleContentDatabase
        ↓
BattleDeckController + BattleBossAI
        ↓
BattleSimulation
        ↓
BattleEffectExecutor / BattleStatusEffectController
        ↓
BattleActionResolver (fallback + enemy only)
        ↓
BattleElementUtility / BattleUnit
        ↓
BattleResultBridge
```

如果从玩家出牌开始看：

```text
BattleSceneController / BattleHudPresenter
        ↓
BattleSceneController.TryPlayCardFromHud(...) / CanPlayCard(...)
        ↓
BattleSimulation.TryPlayCard(...)  [先查询并校验 AP]
        ↓
BattleDeckController.TryPlayCard(...)
        ↓
BattleCardDefinition  [所有玩家卡牌，含 fallback]
        ↓
BattleEffectExecutor.Execute(...)
        ↓
[Damage/Heal/Shield/ApplyStatus commands]
        ↓
BattleSimulation.CheckBattleEnd()
        ↓
[若 AP > 0 等待玩家继续出牌；若 AP == 0 自动进入敌方回合]
        ↓
BattleResultBridge.Commit(...)
```

---

## 6. 当前 BattleScene 与这三层的关系

当前 `BattleScene` 已经是这三层的有效承载场景。

已完成的对接包括：

- `BattleSceneController` 已挂到场景中
- `contentDatabase` 已绑定
- `startingBossId` 默认是 `enemy.ash.imp`
- 四个 `Presentation_*` 已完整配置，并且已经接入各自独立的敌人 sprite 与背景 sprite
- 没有 Workshop payload 时，场景可直接用 fallback deck 进入可玩状态

这意味着：

- 场景开发现在主要是“表现层调试和资源替换”
- 这三层本身已经能支持完整的连续战斗闭环

---

## 7. 当前状态与限制

这三层已经能支撑 Battle 原型完整跑通，但仍有明确限制。

已完成：

- 4 encounter 连续战斗主循环
- 固定循环敌方 AI
- 数据驱动内容查找
- Workshop -> Battle payload 消费
- Battle -> ResultBridge 输出
- fallback deck
- **行动点（AP）机制：每回合 3 AP，Attack 2 AP / Defense·Healing 1 AP**
- **Per-card 指令化效果系统：22 张卡牌定义 + EffectExecutor Command Pattern**
- **状态效果基础框架：16 个状态定义 + 回合触发 + 堆叠 + 过期清理**
- **敌方回合过渡状态：`BossTurnPending` + 场景侧延时推进**
- **最小 Battle EditMode 测试基线：锁定结束回合后不会立即同步执行敌方动作**

未完成或暂缓：

- 多敌战斗
- 复杂关键词行为（Freeze 跳过行动、Expose 增伤、Stun 眩晕等仍为占位实现）
- 敌方攻击元素修正
- 完整战后返场流程
- 保存 / 读档

---

## 8. 开发时如何使用这三层

如果你要改 Battle 规则或扩内容，先判断改动属于哪层：

- 改敌人数据、卡牌定义、状态效果定义、表现资源绑定：数据与内容层
- 改出牌逻辑、效果指令、伤害公式、回合推进：战斗模拟层
- 改场景入口、敌人选择、结果输出：场景编排层

如果只是调整：

- 相机
- 立绘
- 背景
- HUD
- 拖拽判定手感

那通常不应该从这份文档对应的三层入手，而应该去看表现与交互层文档。
