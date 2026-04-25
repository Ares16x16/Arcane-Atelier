# Battle 表现与交互层

> 当前 Battle 模块中与“玩家看到什么、如何操作、场景如何反馈”直接相关的部分。
> Last Updated: 2026-04-25

---

## 1. 层职责

表现与交互层负责 Battle 的玩家视角体验，不负责战斗规则本身。

这一层回答的是：

- 画面里显示哪些对象
- 玩家如何出牌、跳过回合
- 战斗信息如何展示
- 伤害、受击、死亡如何反馈到屏幕

当前主要由三个运行时类型组成：

- `BattleHudPresenter`：HUD 渲染、手牌交互、拖拽判定、结果覆盖层
- `BattleVisualManager`：相机、背景、玩家/敌人视觉对象、屏幕震动
- `BattleUnitVisual`：单个战斗单位的待机、攻击、受击、死亡动画

---

## 2. 当前 BattleScene 结构

当前 `Assets/ArcaneAtelier/BattleScene.unity` 已经不是只有控制器的空场景，而是显式包含以下根对象：

- `BattleSceneController`
- `Main Camera`
- `Background`
- `PlayerVisual`
- `BossVisual`

其中：

- `Main Camera` 使用正交相机，负责 Battle 取景
- `Background` 使用 `SpriteRenderer` 显示背景图
- `PlayerVisual` 使用 `BattleUnitVisual` + `SpriteRenderer`
- `BossVisual` 使用 `BattleUnitVisual` + `SpriteRenderer`

正式场景现在优先依赖这些显式对象，而不是完全依赖 `BattleVisualManager` 在运行时自动创建。

---

## 3. 美术资源接入方式

当前 Battle 专用占位美术位于：

- `Assets/ArcaneAtelier/Battle/Art/Sprites/Characters/`
- `Assets/ArcaneAtelier/Battle/Art/Sprites/Backgrounds/`

已接入的资源包括：

- 玩家占位图：`Player_Default`
- 敌人占位图：`Enemy_AshImp`、`Enemy_MossShell`、`Enemy_MistLeech`
- Boss 占位图：`Boss_EarthGolem`
- 背景图：`BG_AshImp`、`BG_MossShell`、`BG_MistLeech`、`BG_EarthGolem`

敌人与背景表现不直接写死在场景中，而是通过 `BattlePresentationProfile` 驱动：

- `BossSprite`
- `BackgroundSprite`
- `BossPosition`
- `BossScale`
- `BackgroundScale`

`BattleVisualManager.Initialize(...)` 会从 `BattleContentDatabase` 查找当前 `bossId` 对应的 `BattlePresentationProfile`，再把资源和布局应用到场景视觉对象上。

---

## 4. HUD 与玩家操作

### 4.1 HUD 入口

`BattleHudPresenter` 由 `BattleSceneController` 初始化，并在 `OnGUI()` 中直接绘制 IMGUI HUD。

当前 HUD 主要分为三块：

- 顶部状态栏
- 中间战场区域
- 底部手牌区

### 4.2 顶部状态栏

顶部显示：

- 玩家名称、HP、Shield、血条
- 当前回合数
- 敌人下一步 intent
- 当前 AP：`AP 3/3`
- `End Turn` 按钮
- 敌人名称、HP、Shield、血条

### 4.3 底部手牌区

底部区域显示：

- 当前手牌数量
- `Draw`、`Discard`、`State`
- 横向滚动的手牌列表

每张卡牌会显示：

- 快捷键编号
- 名称
- Role
- Element
- **AP 消耗标签**（如 `2 AP` / `1 AP`）
- 效果摘要
- 拖拽提示

### 4.4 交互方式

当前支持两种玩家输入：

- 鼠标拖拽出牌
- 键盘调试回退

键盘回退规则：

- `1` 到 `9`：按手牌索引出牌（受 AP 限制）
- `Space`：结束回合

拖拽规则：

- `Attack` 卡只能拖到敌人
- `Healing` / `Defense` 卡只能拖到自己
- 放到无效区域会回弹，不会出牌
- **AP 不足时卡牌置灰且不可交互**

拖拽目标判定依赖 `BattleVisualManager` 中玩家和敌人 `SpriteRenderer.bounds` 投影到屏幕后的矩形区域。

这意味着 Battle 场景里的角色图大小、位置、透明边距会直接影响拖拽手感。

---

## 5. 屏幕反馈与动画

### 5.1 `BattleVisualManager`

`BattleVisualManager` 是表现层协调者，负责：

- 持有 `battleCamera`
- 持有 `backgroundRenderer`
- 持有 `playerVisual`
- 持有 `bossVisual`
- 订阅 `BattleSimulation` 事件
- 在受击时触发屏幕震动

### 5.2 `BattleUnitVisual`

`BattleUnitVisual` 负责单体动画反馈，当前实现了：

- `StartIdle()`：轻微上下浮动
- `PlayAttack(Vector3 direction)`：向目标方向突进后回位
- `PlayHurt()`：横向抖动 + 白闪
- `PlayDeath()`：缩小并淡出

### 5.3 当前反馈链路

玩家攻击时：

- `BattleVisualManager` 让玩家执行攻击动画
- 敌人执行受击动画
- 相机执行 screen shake

敌人攻击时：

- 敌人执行攻击动画
- 玩家执行受击动画
- 相机执行 screen shake

战斗结束时：

- 胜利：敌人播放死亡动画
- 失败：玩家播放死亡动画

---

## 6. 结果覆盖层

当 `BattleSceneController.CurrentResult != null` 时，`BattleHudPresenter` 会绘制结果覆盖层。

当前结果层显示：

- `Victory` / `Defeat`
- 敌人名称
- 伤害、治疗、护盾、出牌数
- 回合数
- 当前提示语：结果已提交，但场景切换尚未完成

这一层目前是只读覆盖层，没有继续按钮，也不负责返回 Workshop。

---

## 7. 与其他三层的边界

表现与交互层不负责决定规则，只消费规则层和编排层的结果。

它主要依赖：

- `BattleSceneController`：提供 Battle 上下文、当前状态、敌人 intent、输入入口
- `BattleSimulation`：提供当前战斗状态与统计数据
- `BattleContentDatabase` / `BattlePresentationProfile`：提供当前敌人的表现资源

它不负责：

- 卡牌如何结算
- 敌人下一步行动如何决定
- 胜负条件如何判断
- BattleResult 如何被外部系统消费

---

## 8. 当前状态与限制

目前这层已经可以支撑一个可玩的单敌 BattleScene，但仍然是原型阶段。

已完成：

- IMGUI HUD
- 拖拽目标判定
- 键盘回退输入
- 结果覆盖层
- 场景显式相机 / 背景 / 玩家 / 敌人对象
- `Presentation_*` 资源驱动的敌人表现切换
- 占位美术接入
- **AP 显示与卡牌 AP 消耗标签**
- **AP 不足时卡牌置灰交互阻断**

当前限制：

- HUD 仍是 IMGUI，不是最终 UI 技术方案
- 玩家立绘仍是占位资源
- 拖拽判定高度依赖 sprite 边界质量
- 结果覆盖层后没有完整场景 handoff
- Scene 视图中的显示效果依赖编辑器观察角度，Game 视图才是最终玩家视角

---

## 9. 正式美术替换时该改什么

正式资源到位后，优先替换以下内容：

- `Presentation_*` 中的 `BossSprite`
- `Presentation_*` 中的 `BackgroundSprite`
- 玩家默认占位图

常见还需要调整的字段：

- `BossPosition`
- `BossScale`
- `BackgroundScale`

通常不需要改：

- `BattleHudPresenter` 的交互逻辑
- `BattleVisualManager` 的事件订阅逻辑
- `BattleScene` 的根对象结构

正确策略是“替换资源并微调 profile”，而不是重建一套新场景结构。
