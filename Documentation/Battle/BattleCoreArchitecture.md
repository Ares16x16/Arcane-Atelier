# Battle 核心架构

> 当前 Battle 模块中除表现与交互层之外的三层：数据与内容层、战斗模拟层、场景编排层。
> Last Updated: 2026-04-25

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
- `BattleCardEffectTemplate`
- `BattlePresentationProfile`
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

### 2.2 `BattleCardEffectTemplate`

负责把 Workshop 传来的 `WorkshopBattleCardEntry` 转成可结算的 Battle 效果。

Battle 不直接复制 Workshop 卡牌定义，而是通过：

- `CardId` 精确查找
- 找不到时按 `Role` 回退查找

最终产出 `BattleResolvedEffect`。

### 2.3 `BattlePresentationProfile`

虽然它会被表现层消费，但它本质上仍然属于内容配置层。

它把某个 `bossId` 和表现资源绑定起来，包括：

- `BossSprite`
- `BackgroundSprite`
- `BossPosition`
- `BossScale`
- `BackgroundScale`

### 2.4 `BattleContentDatabase`

`BattleContentDatabase` 是 Battle 内容入口，统一持有：

- `bosses`
- `cardEffectTemplates`
- `presentationProfiles`

它提供常用查找能力：

- `FindBoss(string bossId)`
- `FindTemplate(string cardId)`
- `FindTemplateByRole(...)`
- `FindPresentationProfile(string bossId)`

BattleScene 启动时依赖它来拿敌人定义和表现资源。

---

## 3. 战斗模拟层

战斗模拟层负责“战斗如何按规则运行”。

当前核心类型如下：

- `BattleSimulation`
- `BattleDeckController`
- `BattleBossAI`
- `BattleActionResolver`
- `BattleElementUtility`
- `BattleUnit`

### 3.1 `BattleSimulation`

`BattleSimulation` 是 Battle 运行时的权威状态机。

它管理：

- `Player`
- `Boss`
- `Deck`
- `BossAI`
- `State`
- `TurnsElapsed`
- `ActionPoints` / `MaxActionPoints`
- 累计战斗统计

当前状态只有三个：

- `WaitingForPlayer`
- `ResolvingBoss`
- `BattleEnded`

玩家操作最终都会进入：

- `TryPlayCard(int handIndex)`
- `EndTurn()`

然后由它决定：

- 是否进入敌方回合
- 是否结束战斗
- 是否广播结果事件

**AP 机制**

- 每回合开始时恢复 `MaxActionPoints`（固定为 3）。
- `TryPlayCard` 会先扣除卡牌的 AP 消耗；若 AP 不足则拒绝出牌。
- 当 AP 降至 0 时，自动进入敌方回合。
- `EndTurn()` 允许玩家主动结束回合（弃牌重抽后进入敌方回合）。

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

AP 消耗查询：

- `Attack` → 2 AP
- `Defense` / `Healing` → 1 AP

如果没有 Workshop payload，会生成 fallback deck：

- 5 张攻击（15 伤害，2 AP）
- 3 张防御（10 护盾，1 AP）
- 2 张治疗（8 生命，1 AP）

### 3.3 `BattleBossAI`

当前敌人 AI 不是动态决策，而是固定循环行动表。

它从 `BattleBossDefinition.ActionPattern` 读取有序动作列表，并循环执行。

可提供：

- `ExecuteNextAction()`
- `PeekNextAction()`

其中 `PeekNextAction()` 主要服务表现层的 intent 展示。

### 3.4 `BattleActionResolver`

负责把“玩家效果”或“敌方动作”结算成对单位的真实影响。

玩家卡牌支持：

- `Attack`
- `Healing`
- `Defense`

敌方动作支持：

- `Attack`
- `Defend`
- `Heal`
- `Special`

其中：

- 玩家攻击会应用元素修正
- 敌方攻击当前仍然是平伤

### 3.5 `BattleElementUtility`

负责元素克制关系与倍率：

- Water ↔ Fire
- Wind ↔ Earth
- Ice ↔ Thunder
- Light ↔ Dark

当前规则：

- 优势：+25%
- 劣势：-25%
- 中立：不修正

### 3.6 `BattleUnit`

`BattleUnit` 是玩家和敌人共用的数值模型。

包含：

- `DisplayName`
- `MaxHealth`
- `CurrentHealth`
- `Shield`
- `Element`
- `IsAlive`

提供：

- `TakeDamage(int)`
- `Heal(int)`
- `AddShield(int)`

它是整个 Battle 规则层中最基础的承载对象。

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
- 监听 Battle 事件并记录 recent events
- 在结束时提交结果

它不负责真正的规则计算，但负责把所有运行时部件接成一条链。

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
BattleActionResolver / BattleElementUtility / BattleUnit
        ↓
BattleResultBridge
```

如果从玩家出牌开始看：

```text
BattleSceneController / BattleHudPresenter
        ↓
BattleSimulation.TryPlayCard(...)  [检查 AP 是否足够]
        ↓
BattleDeckController.TryPlayCard(...)
        ↓
BattleCardEffectTemplate.Resolve(...)
        ↓
BattleActionResolver.ResolvePlayerEffect(...)
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
- 四个 `Presentation_*` 已完整配置，可供表现层读取
- 没有 Workshop payload 时，场景可直接用 fallback deck 进入可玩状态

这意味着：

- 场景开发现在主要是“表现层调试和资源替换”
- 这三层本身已经能支持完整的单敌战斗闭环

---

## 7. 当前状态与限制

这三层已经能支撑 Battle 原型完整跑通，但仍有明确限制。

已完成：

- 单敌战斗主循环
- 固定循环敌方 AI
- 数据驱动内容查找
- Workshop -> Battle payload 消费
- Battle -> ResultBridge 输出
- fallback deck
- **行动点（AP）机制：每回合 3 AP，Attack 2 AP / Defense·Healing 1 AP**

未完成或暂缓：

- 多敌战斗
- 状态效果系统
- 敌方攻击元素修正
- 完整战后返场流程
- 保存 / 读档

---

## 8. 开发时如何使用这三层

如果你要改 Battle 规则或扩内容，先判断改动属于哪层：

- 改敌人数据、卡牌模板、表现资源绑定：数据与内容层
- 改出牌逻辑、伤害公式、回合推进：战斗模拟层
- 改场景入口、敌人选择、结果输出：场景编排层

如果只是调整：

- 相机
- 立绘
- 背景
- HUD
- 拖拽判定手感

那通常不应该从这份文档对应的三层入手，而应该去看表现与交互层文档。
