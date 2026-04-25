<!-- From: /Users/helluin/Desktop/AA/Arcane-Atelier/AGENTS.md -->
# AGENTS.md — Arcane Atelier

> Current-state context for AI agents working in this Unity project.

## Project Overview

- **Game**: Arcane Atelier
- **Engine**: Unity 6000.4.0f1 (Unity 6), Built-in Render Pipeline
- **Language**: C# (.NET Standard 2.1)
- **Genre**: 2D top-down hybrid of factory-building (`Workshop`) and single-enemy card combat (`Battle`)
- **UI**: IMGUI for both Workshop and Battle
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
- Core runtime pieces: `WorkshopSceneController`, `WorkshopSimulation`, `WorkshopGridView`, `WorkshopHudPresenter`, `WorkshopContentDatabase`.
- Workshop remains the producer of battle cards and commits them through `WorkshopBattlePayloadBridge`.

### Battle

- Battle is a playable single-enemy combat prototype with a complete core loop and full IMGUI HUD.
- See `Documentation/Battle/BattleCombatDesign.md` for combat logic details.
- See `Documentation/Battle/BattleHudDesign.md` for HUD and interaction design.
- The battle system is currently **single-enemy only**.
- Runtime type names still use `Boss` terminology, but the same content path is now used for both true bosses and normal enemies.

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

1. No status-effect system yet; parsed effect keywords are still ignored.
2. No complete Workshop → Battle → Workshop scene flow has been finalized.
3. No save/load persistence.
4. Enemy attacks do not currently apply elemental advantage/disadvantage.
5. Automated tests are still absent.
6. Documentation may lag behind rapid codebase iteration. Prefer runtime code and current assets as source of truth.

## Important Paths

| Path | Purpose |
|------|---------|
| `Assets/ArcaneAtelier/Workshop/Runtime/` | Workshop runtime code |
| `Assets/ArcaneAtelier/Battle/Runtime/` | Battle runtime code |
| `Assets/ArcaneAtelier/Battle/Editor/` | Battle content generation/editor tools |
| `Assets/ArcaneAtelier/Battle/Content/` | Bosses, normal enemies, templates, presentation profiles |
| `Assets/ArcaneAtelier/BattleScene.unity` | Current battle scene |
| `Documentation/Battle/BattleCombatDesign.md` | Battle combat logic design |
| `Documentation/Battle/BattleHudDesign.md` | Battle HUD and interaction design |
| `Documentation/BattleArchitecture.md` | Battle system architecture overview |
| `Documentation/BattleWorkshopDependencies.md` | Stable module dependency reference |
| `Documentation/WorkshopBattleContract.md` | Workshop ↔ Battle payload contract |

## Working Guidance For Agents

- Treat Battle as a **current-state playable prototype**, not a blank module.
- When changing Workshop, preserve zero-knowledge of Battle.
- When changing Battle content, prefer data-driven updates through `BattleContentDatabase`, `BattleBossDefinition`, and `BattlePresentationProfile`.
- If repo documentation disagrees with runtime code, verify against code and assets first.
