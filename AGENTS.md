# AGENTS.md — Arcane Atelier

> Current-state context for AI agents working in this Unity project.

## Project Overview

- **Game**: Arcane Atelier
- **Engine**: Unity 6000.4.0f1 (Unity 6), Built-in Render Pipeline
- **Language**: C# (.NET Standard 2.1)
- **Genre**: 2D top-down hybrid of factory-building (`Workshop`) and single-enemy card combat (`Battle`)
- **UI**: Workshop uses IMGUI; Battle still relies mostly on keyboard input and logs, with no full battle HUD yet
- **Core loop target**: Build cards in Workshop → enter Battle → defeat enemy/boss → return with rewards

## Module Boundaries

```text
Workshop ──→ Core (future)
   ↑
   │ payload / shared enums only
   │
Battle ─────→ Workshop
```

- `Battle` depends on `Workshop` for payload structs and shared enums.
- `Workshop` must not know `Battle` implementation details.
- Cross-scene handoff is through `WorkshopBattlePayloadBridge` and `BattleResultBridge`.

## Current Project State

### Workshop

- Workshop runtime is present and playable as a factory simulation slice.
- Core runtime pieces are:
  - `WorkshopSceneController`
  - `WorkshopSimulation`
  - `WorkshopGridView`
  - `WorkshopHudPresenter`
  - `WorkshopContentDatabase`
- Workshop remains the producer of battle cards and commits them through `WorkshopBattlePayloadBridge`.

### Battle

- Battle is no longer just a skeleton. A playable single-enemy combat loop is implemented.
- Core runtime pieces are:
  - `BattleSceneController`: scene entry, payload consumption, enemy init, keyboard input, result commit
  - `BattleSimulation`: turn-state machine and battle-end handling
  - `BattleDeckController`: draw / hand / discard / fallback deck
  - `BattleBossAI`: fixed cyclic action pattern executor
  - `BattleActionResolver`: player and enemy action resolution
  - `BattleVisualManager` and `BattleUnitVisual`: basic visual setup and animation feedback
  - `BattleContentDatabase`: enemy definitions, card templates, and presentation profiles
  - `BattlePresentationProfile`: data-driven sprite/background/position/scale binding by `BossId`
- The battle system is currently **single-enemy only**.
- Runtime type names still use `Boss` terminology, but the same content path is now used for both true bosses and normal enemies.

## Current Battle Content

Current default content under `Assets/ArcaneAtelier/Battle/Content/` includes:

- Boss:
  - `Boss_EarthGolem`
- Normal enemies:
  - `Enemy_AshImp`
  - `Enemy_MossShell`
  - `Enemy_MistLeech`
- Card templates:
  - `CardEffectTemplate_Attack`
  - `CardEffectTemplate_Heal`
  - `CardEffectTemplate_Defend`
- Presentation profiles:
  - `Presentation_EarthGolem`
  - `Presentation_AshImp`
  - `Presentation_MossShell`
  - `Presentation_MistLeech`

Notes:

- `BattleContentDatabase` now stores `presentationProfiles` in addition to enemy definitions and card templates.
- Presentation profiles exist for the three normal enemies, but their sprite fields may still be unassigned; runtime will fall back to scene defaults or placeholders if art is missing.

## Scene Status

- `Assets/ArcaneAtelier/BattleScene.unity` exists.
- Battle scene setup is still in progress; agents should treat scene wiring and art hookup as partially completed work, not a finished production scene.
- `BattleVisualManager` can auto-create background/player/boss visuals if scene references are missing, but explicit scene references are preferred when building the final scene.

## Combat Rules Implemented

- Player and enemy take turns in a simple loop:
  - player plays a card or skips
  - enemy executes the next scripted action
  - win/loss is checked after each resolution
- `BattleBossAI` uses fixed cyclic patterns from `BattleBossDefinition`.
- `BattleActionResolver` supports:
  - attack
  - healing
  - defense / shield
  - special actions on the enemy side, currently resolved like attack
- Elemental advantage applies to **player attacks against enemies** using `BattleElementUtility`.
- Enemy attacks are still flat damage and do **not** currently apply elemental modifiers.

## Assembly Definitions

| Assembly | References | Platform | Notes |
|----------|-----------|----------|-------|
| `ArcaneAtelier.Workshop.Runtime` | Unity built-ins | All | Factory-building systems |
| `ArcaneAtelier.Workshop.Editor` | `Workshop.Runtime` | Editor only | Workshop bootstrapping |
| `ArcaneAtelier.Battle.Runtime` | `Workshop.Runtime`, `UnityEngine.IMGUIModule` | All | Combat systems |
| `ArcaneAtelier.Battle.Editor` | `Battle.Runtime`, `Workshop.Runtime` | Editor only | Battle content generation |

- Unity asmdef references are non-transitive; editor assemblies must explicitly reference required runtime assemblies.

## Code Style Constraints

- Namespaces: `ArcaneAtelier.Workshop`, `ArcaneAtelier.Battle`
- Use explicit access modifiers everywhere.
- Prefer modern C# features when readable.
- Use `[SerializeField]` for inspector-exposed private fields.
- Do **not** use target-typed `new()`. Use explicit type names.

## Known Limitations

1. No full Battle HUD yet; combat feedback is still largely logs plus basic animation.
2. No status-effect system yet; parsed effect keywords are still ignored.
3. No complete Workshop → Battle → Workshop scene flow has been finalized.
4. No save/load persistence.
5. Enemy attacks do not currently apply elemental advantage/disadvantage.
6. Automated tests are still absent.
7. Some documentation, especially `Documentation/BattleArchitecture.md`, is behind the codebase. Prefer runtime code and current assets as source of truth.

## Important Paths

| Path | Purpose |
|------|---------|
| `Assets/ArcaneAtelier/Workshop/Runtime/` | Workshop runtime code |
| `Assets/ArcaneAtelier/Battle/Runtime/` | Battle runtime code |
| `Assets/ArcaneAtelier/Battle/Editor/` | Battle content generation/editor tools |
| `Assets/ArcaneAtelier/Battle/Content/` | Bosses, normal enemies, templates, presentation profiles |
| `Assets/ArcaneAtelier/BattleScene.unity` | Current battle scene |
| `Documentation/BattleWorkshopDependencies.md` | Stable module dependency reference |
| `Documentation/WorkshopBattleContract.md` | Workshop ↔ Battle payload contract |

## Working Guidance For Agents

- Treat Battle as a **current-state playable prototype**, not a blank module.
- When changing Workshop, preserve zero-knowledge of Battle.
- When changing Battle content, prefer data-driven updates through `BattleContentDatabase`, `BattleBossDefinition`, and `BattlePresentationProfile`.
- If repo documentation disagrees with runtime code, verify against code and assets first.
