# Battle Logic Optimization Notes

## Summary

> Last Updated: 2026-05-24

当前战斗体验已经完成一轮 demo 向收紧，本文记录当前可运行数值方向，避免再按旧方案误改。

当前实现重点：

- 玩家生命使用 `BattleSceneController.playerMaxHealth = 60`，再叠加 meta progression 加成。
- 每回合 `3 AP`，起手 `5` 张。
- 攻击牌 `1 AP`，防御牌和治疗牌 `2 AP`。
- 出牌后不会立即补牌；结束回合才弃整手并重抽。
- 敌方回合通过 `BossTurnPending` 展示约 `1.1s` 的 intent windup。

当前 balance 目标是把战斗保持在“demo 可赢，但不能无脑只点输出”的区间：

- 普通玩家可以赢，但不能再稳定无伤通关。
- 玩家每回合必须在输出、防御、续航之间做取舍。
- 敌方回合要形成真实压迫，且威胁应被清楚提示给玩家。

## Key Changes

### 1. 玩家行动经济

当前不再采用旧方案里的 `2 AP / 4 hand` 收紧方式。实际代码使用：

- `MaxActionPoints = 3`
- `TurnDrawCount = 5`
- 攻击牌 `1 AP`
- 防御牌 `2 AP`
- 治疗牌 `2 AP`

预期效果：

- 玩家能打出多张攻击牌保持演示节奏。
- 防御和治疗占用更大行动经济，避免低成本完全抹平敌方伤害。
- 支援牌需要足够强，才能在 `2 AP` 成本下值得选择。

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

### 3. 支援牌当前 tuning

基础与中级支援牌应与 Workshop 产出定义保持一致，不再使用旧方案里的下调建议。当前 Battle content 已对齐以下 Workshop 数值：

- `Tidal Mend`：`Heal(6, 1)` + `Regen 8`
- `Lumen Prayer`：`Heal(5, 2)` + `Bless 12`
- `Stoneguard Sigil`：`Shield(7, 1)` + `Bulwark 18`
- `Gloam Ward`：`Shield(6, 1)` + `Veil 20`
- `Tide Chorus`：`Heal(11, 2)` + `Regen 14`
- `Bastion Pulse`：`Shield(12, 1)` + `Ward 28`
- `Umbral Bastion`：`Shield(10, 2)` + `Shade 24`

中级和高级恢复牌保留强度梯度。后续如果玩家仍过稳，优先调整敌方压力、奖励节奏或支援牌成本，不要让 Battle content 再和 Workshop card table 分叉。

预期效果：

- 玩家仍有防守工具，但不能用低成本护盾和恢复完全抹平敌方伤害。
- 中高阶牌的成长感会更明确。

### 4. 缩短敌方回合等待，增强回合压迫

当前 `BossTurnTransitionDelay = 1.1f`。保留 windup 设计，但让它服务于威胁提示，而不是拖慢节奏。

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

- `BattleSimulation` 使用 `3 AP`。
- `BattleDeckController.GetActionPointCost()` 保持 Attack = `1`，Defense/Healing = `2`。
- `EndTurn()` 后仍先进入 `BossTurnPending`，不会同步立即执行敌方动作。
- `Freeze` / `Stun` 仍能在数值调整后阻止敌方动作，不破坏现有控制逻辑。
- 各 archetype 敌人不会连续多回合做无压力空转动作。
- 缺少 Workshop payload 时，Battle deck 保持空手牌。

### Manual Validation

- 使用基础工坊牌组对战 `Ash Imp` 时，玩家应在正常流程中明显掉血。
- 每场战斗结束后应能通过结果层回到 Workshop，由集成层配置下一场。
- 如果玩家连续贪输出、不做防守，应较容易被中后段敌人压低血量。
- 如果玩家合理轮换攻击、防御和恢复，应能以中低血而不是满血状态完成通关。
- 敌方回合提示应清晰，但整体战斗不应拖沓。

## Important Interface And Data Changes

本方案不引入新的公共系统，只调整现有参数和少量展示行为。

- `BattleContentBootstrapper`
  - Battle card support values should stay aligned with `WorkshopDefaultContentFactory`
- `Assets/ArcaneAtelier/Battle/Content/CardDefinition_*.asset`
  - Existing generated card assets must be updated alongside bootstrapper changes

不新增新的 `ScriptableObject` 类型，不改变现有 bridge、payload 或 asmdef 依赖。

## Assumptions

- 本轮目标是先把“过于简单、没有操作感”修正到可玩的中等偏紧版本，不做大规模系统重构。
- 继续保持单敌战斗，不在本轮引入多敌人、怒气、能量、弃牌惩罚等新机制。
- 状态效果系统现状保持不变，本轮主要通过数值、节奏与反馈调整提升体验。
- 如果第一轮实现后仍偏软，优先继续下压玩家续航，其次继续提高前两场敌人伤害，而不是给玩家增加更多资源。
