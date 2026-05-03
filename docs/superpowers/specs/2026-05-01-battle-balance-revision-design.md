# Battle Balance Revision Design

## Summary

修订 `Documentation/Battle/BattleLogicOptimize.md` 中的战斗平衡方案，修复两个结构性缺陷：

1. **2 AP 上限 + 2 费 Attack 造成"死 AP"**：玩家被迫在"纯输出/纯防御/纯续航"之间二选一，不存在混合操作，决策空间为零。
2. **Earth Golem P2 是数值绝境**：即使玩家每回合双防御，P2 狂暴伤害（30/39）也会在 3 回合内击杀 65 HP 玩家，而击杀 130 HP 的 Golem 需要 5–7 回合输出，时间不够。

目标：调整为"中等偏紧"，普通玩家可以通关但不稳定，每回合需要在输出、防御、续航之间做真实取舍。

---

## Key Changes

### 1. 保持 AP = 3，收紧手牌规模

**原因**：AP 降到 2 会让 Attack(2 AP) 独占整回合， Defense/Heal(1 AP) 的组合也恰好占满 2 AP。不存在"1 攻 + 1 防"的混合操作，1 AP 永远浪费。

**保持 AP = 3 后的真实取舍**：

| 组合 | AP | 策略 |
|------|-----|------|
| 1 Attack + 1 Defense | 2 + 1 = 3 | 攻守兼备（核心取舍）|
| 1 Attack + 1 Heal | 2 + 1 = 3 | 输出+续航 |
| 3 Defense | 1+1+1 = 3 | 全力龟缩 |
| 2 Heal + 1 Defense | 1+1+1 = 3 | 全力续航 |
| 1 Attack | 2 | 余 1 AP，可再出 1 防/1 疗 |

**收紧行动经济的其他手段**：
- `BattleDeckController` 起手和回合补牌从 **5 张下调到 4 张**
- 回合结束仍弃掉整手并补满，但目标手牌数改为 4

预期效果：玩家选择面收窄，每张牌分量更重，"出 Attack 就不能同时 Defense"的取舍更尖锐。

---

### 2. 引入场间恢复，重构敌人压力曲线

**原因**：4 场连战不恢复 HP 的数学必然性——前 3 场累积伤害 + Earth Golem 伤害必然超过玩家 HP。要么前 3 场完全没压力（和"中等偏紧"矛盾），要么 Earth Golem 必死。

**场间恢复规则**：
- 每场遭遇胜利后，玩家恢复 **15 HP**
- 恢复上限不超过 `playerMaxHealth`
- 在 `BattleSceneController.OnBattleEnded()` 中，当 `ResultType == Victory` 且不是最后一场时执行

**敌人数值基线（调整后）**：

#### Ash Imp
- 生命：`30 -> 35`
- 攻击：`6 / 9 / 8` -> `8 / 11 / 10`
- 保持 `Aggressive`
- burst 节奏不变（第 3 回合 Special = 10）

#### Mist Leech
- 生命：`40 -> 45`
- 攻击：`6 / 7` -> `8 / 10`
- 治疗保留为 `4`，但仅在低血时触发
- 保持 `Sustain`

#### Moss Shell
- 生命：`48 -> 50`
- 护盾保留 `6 / 8`
- 攻击从 `5` 提升到 `9`
- 保持 `Defensive`，但加盾后下一回合必须攻击，不允许连续两回合只堆盾

#### Earth Golem
- 生命：`110 -> 90`
- P1 攻击：`11 / 15` -> `10 / 14`
- P2 基础攻击：`16 / 20`（原方案 22 → 20，降低了基础值）
- 保留 enraged phase，P2 狂暴倍率保持 `1.5x`
- P2 实际输出：`16×1.5=24` / `20×1.5=30`（原方案 22×1.5=33，输出峰值从 33 降至 30）
- P1 护盾 `8` 保留，P1 治疗 `7` 保留
- P2 治疗 `10` 保留

> **实现说明**：设计阶段曾考虑将狂暴倍率从 1.5x 降至 1.25x，但最终实现保持了 1.5x，通过降低 P2 基础攻击值（22→20）来间接降低峰值输出。

**连战伤害推演（使用下调后的治疗/护盾）**：

| 场次 | 敌人 HP | 平均 DPT | 玩家净受伤/回合 | 回合 | 总受伤 | 恢复后 HP |
|------|---------|---------|----------------|------|--------|----------|
| 1. Ash Imp | 35 | ~8 | ~3 | 4 | ~12 | 80→68+15=**80** |
| 2. Mist Leech | 45 | ~7 | ~4 | 5 | ~20 | 80→60+15=**75** |
| 3. Moss Shell | 50 | ~5 | ~3 | 6 | ~18 | 75→57+15=**72** |
| 4. Earth Golem P1 | 90→36 | ~9 | ~4 | 7 | ~28 | 72→44 |
| 4. Earth Golem P2 | 36→0 | ~18 | ~10 | 4 | ~40 | 44→**4 HP 通关** |

预期效果：能通关，但只剩 4 HP。如果玩家某回合贪输出少防御，就会暴毙。这才是"中等偏紧"。

---

### 3. 下调玩家基础续航强度

在 `BattleContentBootstrapper.CreateAllCardDefinitions()` 中削弱基础治疗和护盾：

**治疗牌**：
- `Tidal Mend`：`Heal(6, 1)` -> `Heal(5, 1)`
- `Lumen Prayer`：`Heal(5, 2)` -> `Heal(4, 2)`

**护盾牌**：
- `Stoneguard Sigil`：`Shield(7, 1)` -> `Shield(6, 1)`
- `Gloam Ward`：`Shield(6, 1)` -> `Shield(5, 1)`

**Workshop 产出卡（fallback）**：
- `Arcane Ward`：`Shield(10)` -> `Shield(8)`

**Fallback Deck 同步**：
`BattleDeckController.BuildFallbackDeck()` 中的硬编码 fallback 卡数值也同步下调，确保无 Workshop 产出时体验一致：
- `Tidal Mend` PrimaryValue：`6` -> `5`
- `Lumen Prayer` PrimaryValue：`5` -> `4`
- `Stoneguard Sigil` PrimaryValue：`7` -> `6`
- `Gloam Ward` PrimaryValue：`6` -> `5`

中级和高级恢复牌保留强度梯度，但不允许在 3 AP 环境中成为稳定无脑解。

预期效果：1 张防御卡不再完全抵消 Boss 一整回合输出，玩家必须用多张卡或承受部分伤害。

---

### 4. 缩短敌方回合等待

- `BattleSceneController.BossTurnTransitionDelay`：`3.0f` -> `1.1f`
- 保留 `BossTurnPending` 状态和意图提示

---

### 5. 敌人行为引入最小随机性

**原因**：固定循环导致玩家看了第一回合就知道接下来所有回合该做什么，没有风险承担。

**最小改动（不新增系统）**：

在 `BattleBossAI` 的敌人原型选择逻辑中，从"固定取 pattern[index]"改为"从行为池按概率抽取"：

- **Aggressive**：Attack 池 [8, 11]，Special 固定 10。每回合 80% Attack、20% Special。
- **Sustain**：非低血时 Attack 池 [8, 10]，低血时（≤40%）优先 Heal(4)。
- **Defensive**：护盾低于阈值时优先 Defend，否则 Attack 池 [5, 9]，每 3 回合强制 1 次 Special。

改动范围：修改 `BattleBossAI` 的敌人原型选择逻辑，从"固定取 pattern[index]"改为"从行为池按概率抽取"。

**实现追加：敌人意图缓存机制**

引入概率池后，`PeekNextAction()` 每帧被 HUD 调用会导致意图不断重掷。解决方案：

- `plannedEnemyAction` + `hasPlannedEnemyAction` 字段缓存本回合计划动作
- `EnsurePlannedEnemyAction()`：首次 Peek 时生成并缓存；后续 Peek 返回同一招
- `ConsumePlannedEnemyAction()`：Execute 时消费缓存，确保"显示什么，敌人就打什么"
- `ClearPlannedEnemyAction()`：Freeze/Stun 跳过回合、或 `Reset()` 时清除缓存

预期效果：玩家不能精确预判下回合伤害，必须承担"可能不够防"的风险；同时 HUD 显示的意图稳定不变。

---

## Files Modified

| 文件 | 改动内容 |
|------|---------|
| `BattleSceneController.cs` | `playerMaxHealth = 100` -> `80`；`BossTurnTransitionDelay = 3.0f` -> `1.1f`；场间恢复逻辑 |
| `BattleSimulation.cs` | `MaxActionPoints = 3`（保持） |
| `BattleDeckController.cs` | 起手/补牌 `5` -> `4`；fallback deck 数值同步下调 |
| `BattleBossAI.cs` | 敌人行为随机池（Aggressive/Sustain/Defensive）；意图缓存机制（plannedEnemyAction） |
| `BattleContentBootstrapper.cs` | 4 个敌人数值重平衡；基础治疗/护盾下调；Workshop fallback 卡数值下调 |
| `BattleContentDatabase.asset` 等 | 9 个 `.asset` 文件同步更新数值 |

---

## Test Plan

### Automated

- `BattleSimulation` 在 3 AP 条件下，1 Attack(2 AP) + 1 Defense(1 AP) = 恰好 3 AP，合法。
- `BattleDeckController` 起手和回合补牌数量为 4。
- `EndTurn()` 后进入 `BossTurnPending`，等待时间 ≈ 1.1s。
- 场间恢复不超过 `playerMaxHealth` 上限。
- `EnemyIntent_RemainsStableUntilExecution`：概率敌人多次 Peek 返回同一招，Execute 与 Peek 一致。

### Manual

- 使用 fallback deck 对战 Ash Imp，玩家应在正常流程中明显掉血（非零伤）。
- 连战四场后，普通玩家以中低血（非满血）进入 Earth Golem。
- 贪输出、不做防守的玩家应较容易被中后段敌人压低血量。
- 合理轮换 Attack/Defense/Heal 的玩家应能以低血（约 0–10 HP）通关。
- Earth Golem P2 形成明显斩杀压力，但不会"必死"。
- HUD 显示的 Boss 意图在多帧内稳定，不会闪烁变化。

---

## Assumptions

- 本轮只改现有参数和最小行为逻辑，不新增资源系统、不弃牌惩罚、不引入多敌人。
- 状态效果系统（Burn/Slow/Expose 等）本轮不改动，Workshop 产出卡的状态缺失问题另案处理。
- 如果第一轮测试后仍偏软，优先继续下压玩家续航（治疗/护盾再降），其次提高前两场敌人伤害。
