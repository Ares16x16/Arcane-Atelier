# Battle 表现与交互层

> 当前 Battle 模块中与“玩家看到什么、如何操作、场景如何反馈”直接相关的部分。
> Last Updated: 2026-05-24

---

## 1. 层职责

表现与交互层负责 Battle 的玩家视角体验，不负责战斗规则本身。

这一层回答的是：

- 画面里显示哪些对象
- 玩家如何出牌、跳过回合
- 战斗信息如何展示
- 伤害、受击、死亡如何反馈到屏幕

当前主要由四个运行时类型组成：

- `BattleHudPresenter`：HUD 渲染、手牌交互、拖拽判定、结果覆盖层
- `BattleVisualManager`：相机、背景、玩家/敌人视觉对象、屏幕震动
- `BattleUnitVisual`：单个战斗单位的待机、攻击、受击、死亡动画
- `BattleFeedbackPresenter`：回合 banner、动作 callout、浮动数字、状态提示

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

当前 4 个可用 encounter 已分别绑定独立的 `Presentation_*` 资源。每次 BattleScene 启动会按当前 encounter 应用对应 profile，因此敌人和背景会随本场 encounter 更新：

- 敌人 sprite
- 背景 sprite
- 敌人位置与缩放
- 背景缩放

场景上 `BattleVisualManager` 自带的 `bossSprite` / `backgroundSprite` 现在只作为兜底安全网使用：

- 当 `BattlePresentationProfile` 缺失时才会回退到场景级引用
- 正常内容流应以 `Presentation_*` 资产为准

---

## 4. HUD 与玩家操作

### 4.1 HUD 入口

`BattleHudPresenter` 由 `BattleSceneController` 初始化，并在 `OnGUI()` 中直接绘制 IMGUI HUD。

当前 HUD 主要分为三块：

- 顶部三段式战斗信息带
- 中间战场区域
- 底部手牌滚动区

### 4.2 顶部三段式战斗信息带

当前顶部 HUD 已不是早期的简单状态栏，而是左右状态块 + 中央操作区的三段式信息带。

左侧玩家状态块显示：

- 玩家名称
- 属性
- `HP x/y`
- `Shield`
- 双层生命 / 护盾条
- 当前状态效果列表

中间操作区显示：

- 当前 encounter 进度
- 当前回合数
- 敌人 intent badge
- intent 文案
- `End Turn` 按钮
- AP 资源条
- `AP x/y`
- 当敌方回合即将开始时，会切换到 `Enemy action incoming` 的高亮准备态
- 准备态期间会显示 intent windup 进度条，并把主文案切成 `Preparing: ...`

右侧敌人状态块显示：

- 敌人名称
- 属性
- `HP x/y`
- `Shield`
- 双层生命 / 护盾条
- 当前状态效果列表（名称 + 剩余回合，叠层状态额外显示 `xN`）

### 4.3 底部手牌滚动区

当前底部区域已经从“信息栏 + 手牌列表”的早期结构，收敛为以卡槽容器为主体的滚动区。

当前只保留：

- 一个顶部信息条
- 一个内层卡槽容器
- 横向滚动的手牌列表
- `Hand / Draw / Discard` 上下文

当前信息条仍保留固定 header：左侧显示 `Prepared Cards`，右侧显示 `Hand / Draw / Discard` 统计，作为手牌容器顶部的常驻概览。

### 4.4 卡牌视觉结构

每张手牌当前按统一卡面结构绘制，包含：

- 元素顶条
- 快捷键编号
- AP 徽标
- 标题
- 效果摘要（优先读取 `BattleCardDefinition.Instructions[]` 的真实组合效果）
- `Element • Role`
- 目标标签
- 底部拖拽提示

标题区域当前按“最多两行”的版式处理：

- 标题允许两行显示
- 启用自动换行
- 不再依赖固定字符数截断来控制标题长度
- 当标题超出两行可容纳范围时，会在卡面可用区域内直接裁切，而不是继续向下挤压摘要区

这次版式调整的目的，是适配中文和中英混排的长卡名，避免标题遮挡 AP 徽标、分隔线或摘要文本。

AP 不足时的当前表现为：

- 卡牌整体变暗
- 保留一处 `Insufficient AP` 提示

不再保留早期“双重 AP 不足提示”之类的历史描述。

### 4.5 交互方式

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
- AP 不足时卡牌会变暗，并阻断按下与拖拽交互

回合切换当前也加入了“动作前过渡”，不再是玩家结束回合后敌人立刻同步行动：

- 玩家结束回合或 AP 用尽后，先进入 `BossTurnPending`
- HUD 与反馈层展示 `Enemy Turn` 提示和 intent 预警
- 过渡窗口结束后，敌方才真正执行行动

拖拽目标判定依赖 `BattleVisualManager` 中玩家和敌人 `SpriteRenderer.bounds` 投影到屏幕后的矩形区域。

这意味着 Battle 场景里的角色图大小、位置、透明边距会直接影响拖拽手感。

### 4.6 目标高亮

当前目标指引已经不是简单的“释放到敌人 / 自己”文字说明，而是在拖拽时直接高亮世界中的合法目标。

具体表现为：

- 对合法目标绘制扩展后的高亮框
- 在高亮框顶部绘制标签
- 自身目标使用 `Target Self`
- 敌方目标使用 `Target Enemy`

激活中的可投放目标会比未激活状态显示更高的高亮强度。

Battle 目前仍然按卡牌 `Role` 判定拖拽合法目标：

- `Attack` → 敌人
- `Healing` / `Defense` → 自己

即使单张卡的内部 `Instructions[]` 混合了主效果与自/敌状态，拖拽入口仍按 `Role` 归类。

---

## 5. 屏幕反馈与动画

### 5.0 `BattleFeedbackPresenter`

`BattleFeedbackPresenter` 是当前新增的轻量演出层，专门负责把模拟层和场景编排层发出的反馈请求变成屏幕提示。

它当前负责：

- `Your Turn` / `Enemy Turn` banner
- 卡牌打出提示
- 敌方动作 callout
- 伤害 / 治疗 / 护盾浮动数字
- 状态附加与状态 tick 提示

它不负责改变战斗规则，只消费 `BattleFeedbackRequest`。

### 5.1 `BattleVisualManager`

`BattleVisualManager` 是表现层协调者，负责：

- 持有 `battleCamera`
- 持有 `backgroundRenderer`
- 持有 `playerVisual`
- 持有 `bossVisual`
- 为玩家和敌人维护 `BattleEffectAnchor`
- 订阅 `BattleSimulation` 事件
- 在受击时触发屏幕震动

### 5.2 `BattleUnitVisual`

`BattleUnitVisual` 负责单体动画反馈，当前实现了：

- `StartIdle()`：轻微上下浮动
- `PlayAttack(Vector3 direction, bool emphasize)`：带轻微缩放脉冲的突进
- `PlayHurt(bool heavy)`：横向抖动 + 白闪，并支持更强的重击反馈
- `PlaySupportPulse(Color pulseColor)`：治疗 / 护盾时的支援脉冲
- `PlayDeath()`：缩小并淡出

### 5.3 当前反馈链路

玩家攻击时：

- `BattleVisualManager` 让玩家执行攻击动画
- 敌人执行受击动画
- 相机执行 screen shake
- 同时由 `BattleFeedbackPresenter` 在敌人身上显示浮动伤害数字

敌人攻击时：

- 敌人执行攻击动画
- 玩家执行受击动画
- 相机执行 screen shake
- 同时在玩家身上显示浮动伤害数字

战斗结束时：

- 胜利：敌人播放死亡动画
- 失败：玩家播放死亡动画

---

## 6. 结果覆盖层

当 `BattleSceneController.CurrentResult != null` 时，`BattleHudPresenter` 会绘制结果覆盖层。

当前结果层已是 modal 风格覆盖层，而不是简单静态文本面板。

当前表现包括：

- `Victory` / `Defeat`
- 根据胜负切换标题强调色
- 最终敌人名称
- 已完成 encounter 数
- 伤害、治疗、护盾、出牌数的卡片化统计块
- 最终 encounter id
- 总回合数
- 结果按钮：普通胜利显示 `To Workshop`，最终胜利或失败显示 `Main Menu`

当前动画感受来自：

- 背景暗化淡入
- modal 尺寸放大过渡
- 标题区单独着色

这一层会通过 `BattleSceneController.ReturnToWorkshop()` 或 `ReturnToMainMenu()` 发起场景跳转；奖励与下一场配置仍由 WorkshopScene 载入后的集成层处理。

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

目前这层已经可以支撑一个可玩的单场 BattleScene，并通过 Workshop 返回流串起 3 个普通敌人 + 1 个 Boss 的原型运行。文档描述应以当前实现为准，而不是早期原型草图。

已完成：

- IMGUI HUD
- 拖拽目标判定
- 键盘回退输入
- 结果覆盖层
- 场景显式相机 / 背景 / 玩家 / 敌人对象
- `Presentation_*` 资源驱动的敌人表现切换
- 每个 encounter 的敌人 sprite / 背景 sprite 已通过独立 `Presentation_*` 资产正确切换
- 占位美术接入
- **AP 显示与卡牌 AP 消耗标签**
- 回合 banner 与敌方回合 windup 过渡
- 敌方行动前的 intent 预警 callout
- 浮动伤害 / 治疗 / 护盾数字
- 状态附加与状态 tick 的短提示
- 敌我单位的强化攻击 / 重击 / 支援脉冲反馈
- **AP 不足时卡牌置灰交互阻断**

当前限制：

- HUD 仍是 IMGUI，不是最终 UI 技术方案
- 当前 encounter 顺序仍由原型代码固定配置，尚未接入最终路线 UI
- 玩家立绘仍是占位资源
- 拖拽判定高度依赖 sprite 边界质量
- 结果覆盖层已有返回 Workshop / Main Menu 的按钮，但奖励选择和路线选择仍是原型级自动流程
- 状态列表较长时仍可能占用较多顶部空间
- 极长标题虽然不再遮挡其他卡面元素，但超出两行范围后仍会被直接裁切，尚未实现像素级省略号策略
- Scene 视图中的显示效果依赖编辑器观察角度，Game 视图才是最终玩家视角
- 场景级 `bossSprite` / `backgroundSprite` 仍保留在组件上，但仅用于内容缺失时的 fallback

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
