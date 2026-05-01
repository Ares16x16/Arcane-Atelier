# Battle Logic Optimization Plan

## Summary

当前战斗体验的主要问题不是单点 bug，而是三层一起偏软：

- 玩家容错过高：`playerMaxHealth = 100`、每回合 `3 AP`、起手 `5` 张，治疗与护盾成本偏低。
- 敌人压迫过低：前三只敌人血量和伤害都不足，且每回合只做一次低威胁动作。
- 操作反馈不足：虽然已有 HUD、turn banner、floating numbers 和 action callout，但玩家几乎不需要承担风险就能稳定获胜，导致操作决策缺乏重量。

本方案目标是把战斗调整为“中等偏紧”：

- 普通玩家可以赢，但不能再稳定无伤通关。
- 玩家每回合必须在输出、防御、续航之间做取舍。
- 敌方回合要形成真实压迫，且威胁应被清楚提示给玩家。

## Key Changes

### 1. 收紧玩家行动经济

优先通过现有 AP 和手牌系统解决“输出、治疗、护盾三项全拿”的问题，不新增复杂资源系统。

- 将 `BattleSceneController` 中的玩家初始生命从 `100` 下调到 `65`。
- 将 `BattleSimulation` 中的每回合 AP 从 `3` 下调到 `2`。
- 保持费用结构不变：
  - 攻击牌 `2 AP`
  - 防御牌 `1 AP`
  - 治疗牌 `1 AP`
- 将 `BattleDeckController` 的起手和回合补牌数量从 `5` 张下调到 `4` 张。
- 回合结束时仍弃掉整手并补满，但目标手牌数改为 `4`。

预期效果：

- 玩家通常每回合只能选择“打一张攻击”或“打一防一补”，不能再无脑兼顾全部。
- 每张牌的决策价值提升，回合选择更接近真正的取舍。

### 2. 重做敌人压力曲线

直接调整 `BattleContentBootstrapper` 里的敌人定义，让每场战斗都能真实威胁玩家血线。

建议数值基线：

#### Ash Imp

- 生命：`30 -> 42`
- 攻击动作提高到约 `8 / 11 / 10`
- 保持 `Aggressive`
- 缩短 burst 节奏，让第三回合威胁更明确

#### Mist Leech

- 生命：`40 -> 52`
- 攻击动作提高到约 `8 / 10`
- 治疗保留，但降低它在整体回合中的占比
- 保持 `Sustain`，但只在低血时明显偏向恢复

#### Moss Shell

- 生命：`48 -> 60`
- 护盾动作保留
- 至少一个攻击动作抬到 `9-11`
- 保证它不是连续数回合只加盾不打人

#### Earth Golem

- 生命：`110 -> 130`
- 一阶段攻击提高到约 `14 / 18`
- 二阶段攻击提高到约 `20 / 26`
- 保留 enraged phase
- 进入二阶段后两回合内必须形成明显的斩杀压力

行为层要求：

- `Aggressive` 敌人优先稳定打伤害，不要被低伤动作稀释节奏。
- `Sustain` 敌人仅在低血时显著提高治疗优先级。
- `Defensive` 敌人加盾后下一拍应尽快形成输出，不允许长期空转。
- 继续保持单敌战斗，不在本轮扩展多敌人框架。

### 3. 下调玩家基础续航强度

在 `BattleContentBootstrapper.CreateAllCardDefinitions()` 中削弱基础治疗和基础护盾的稳定收益，解决玩家“边打边无损站桩”的问题。

建议方向：

- 基础治疗牌整体下调约 `15%-25%`
  - `Tidal Mend`：`Heal(6, 1) -> Heal(5, 1)`
  - `Lumen Prayer`：`Heal(5, 2) -> Heal(4, 2)`，必要时同步降低 `Bless` 的收益
- 基础护盾牌整体下调约 `10%-20%`
  - `Stoneguard Sigil`：`Shield(7, 1) -> Shield(6, 1)`
  - `Gloam Ward`：`Shield(6, 1) -> Shield(5, 1)`
- 中级和高级恢复牌保留强度梯度，但不能在 `2 AP` 环境中继续成为稳定无脑解。
- 基础攻击牌不做大砍，优先压缩玩家防守资源，而不是直接削弱输出节奏。
- 保留状态效果特色，但减少基础牌单张同时提供的纯数值安全垫。

预期效果：

- 玩家仍有防守工具，但不能用低成本护盾和恢复完全抹平敌方伤害。
- 中高阶牌的成长感会更明确。

### 4. 缩短敌方回合等待，增强回合压迫

当前 `BossTurnTransitionDelay = 3.0f` 过长，会削弱战斗张力。保留 windup 设计，但让它服务于威胁提示，而不是拖慢节奏。

- 将 `BattleSceneController` 中的 `BossTurnTransitionDelay` 从 `3.0f` 下调到 `1.1f`。
- 保留 `BossTurnPending` 状态。
- 继续通过 `BossTurnPending` 展示回合切换和敌方意图。
- 在 HUD 顶部更突出展示下一次敌方动作描述。
- 对高伤和特殊动作使用更强的警示色和文案，例如：
  - `Heavy Attack`
  - `Burst Incoming`

预期效果：

- 战斗节奏更利落。
- 玩家依然能读懂敌方动作，但不会因为等待过长而失去紧张感。

### 5. 强化“操作感”的可见反馈

不引入大系统，直接利用现有 `BattleHudPresenter` 和 `BattleFeedbackPresenter` 增强风险感知与决策压力。

- 当 AP 不足以再打攻击牌时，强化 AP 区域的闪烁、颜色和文本提醒。
- 敌方进入 `BossTurnPending` 时，将意图描述做成更明显的危险提示，而不是普通说明文本。
- 当玩家掉血、破盾、吃到高伤时，加强浮字和 callout 的强调表现。
- 当拖拽卡牌到无效目标，或在当前状态下无法出牌时，加强视觉反馈。
- 不改变现有交互规则，优先提升“我必须做判断”的感受。

目标不是做更花哨的 UI，而是让玩家明显感到每回合存在风险与取舍。

## Test Plan

### Automated Tests

- `BattleSimulation` 在 `2 AP` 条件下，玩家打出一张攻击牌后不能继续打第二张攻击牌。
- `EndTurn()` 后仍先进入 `BossTurnPending`，且等待时间符合新的短延迟设定。
- `Freeze` / `Stun` 仍能在数值调整后阻止敌方动作，不破坏现有控制逻辑。
- 各 archetype 敌人不会连续多回合做无压力空转动作。

### Manual Validation

- 使用 fallback deck 或基础工坊牌组对战 `Ash Imp` 时，玩家应在正常流程中明显掉血。
- 连战四场后，普通玩家不应再稳定满血进入 `Earth Golem`。
- 如果玩家连续贪输出、不做防守，应较容易被中后段敌人压低血量。
- 如果玩家合理轮换攻击、防御和恢复，应能以中低血而不是满血状态完成通关。
- 敌方回合提示应清晰，但整体战斗不应拖沓。

## Important Interface And Data Changes

本方案不引入新的公共系统，只调整现有参数和少量展示行为。

- `BattleSceneController`
  - 调整 `playerMaxHealth`
  - 调整 `BossTurnTransitionDelay`
- `BattleSimulation`
  - 调整 `MaxActionPoints`
- `BattleDeckController`
  - 调整起手和补牌数量
- `BattleContentBootstrapper`
  - 重新平衡敌方 `BattleBossDefinition`
  - 重新平衡玩家 `BattleCardDefinition`

不新增新的 `ScriptableObject` 类型，不改变现有 bridge、payload 或 asmdef 依赖。

## Assumptions

- 本轮目标是先把“过于简单、没有操作感”修正到可玩的中等偏紧版本，不做大规模系统重构。
- 继续保持单敌战斗，不在本轮引入多敌人、怒气、能量、弃牌惩罚等新机制。
- 状态效果系统现状保持不变，本轮主要通过数值、节奏与反馈调整提升体验。
- 如果第一轮实现后仍偏软，优先继续下压玩家续航，其次继续提高前两场敌人伤害，而不是给玩家增加更多资源。
