# Battle → Workshop Dependency Reference

> **Module**: `ArcaneAtelier.Battle`  
> **Depends on**: `ArcaneAtelier.Workshop` (one-way)  
> **Last Updated**: 2026-04-22

---

## 1. Overview

The Battle module consumes a small, stable surface area from Workshop. All cross-module references are **read-only** from Battle's perspective—Workshop has zero knowledge of Battle types.

This document enumerates every Workshop type referenced by Battle code, grouped by purpose.

---

## 2. Cross-Scene Data Bridge (Runtime-Critical)

| Workshop Type | Purpose in Battle |
|---------------|-------------------|
| `WorkshopBattlePayloadBridge` | Static bridge. Workshop calls `Commit()` to store the prepared deck; Battle calls `TryConsume()` to read it on scene entry. |
| `WorkshopBattlePayload` | The data packet carried across the scene boundary. Contains the player's assembled card list. |
| `WorkshopBattleCardEntry` | A single card identifier + quantity. Battle uses these entries to reconstruct the playable hand. |

**Consumption site**: `BattleSceneController.LoadWorkshopPayload()`

---

## 3. Element Attribute Enum

| Workshop Type | Purpose in Battle |
|---------------|-------------------|
| `WorkshopElementAttribute` | Eight elemental affiliations (`Water`, `Fire`, `Wind`, `Earth`, `Ice`, `Thunder`, `Light`, `Dark`, `None`). Used for player units, boss units, card effects, and advantage calculation. |

**Consumption sites**:
- `BattleUnit.Element` — element of the player or boss combatant
- `BattleBossDefinition.Element` — boss template configuration
- `BattleCardEffectTemplate.Element` — resolved element of a card effect
- `BattleElementUtility.GetRelation()` — advantage / disadvantage / neutral resolution

---

## 4. Spell Role Enum

| Workshop Type | Purpose in Battle |
|---------------|-------------------|
| `WorkshopSpellRole` | Card role classification (`Attack`, `Defense`, `Healing`, `Utility`, `None`). Determines how a card behaves when resolved in combat. |

**Consumption sites**:
- `BattleCardEffectTemplate.Role` — template definition
- `BattleResolvedEffect.Role` — post-resolution effect struct
- `BattleContentBootstrapper` — default content generation (assigns roles to templates)

---

## 5. Assembly Reference

| Dependency | Declaration Location | Notes |
|------------|----------------------|-------|
| `ArcaneAtelier.Workshop.Runtime` | `ArcaneAtelier.Battle.Runtime.asmdef` → `references` | Required for compilation. Without it, all Workshop type references in Battle code become undefined. |

---

## 6. Dependency Flow Diagram

```
Workshop Scene                          Battle Scene
───────────────                        ────────────
WorkshopSceneController
       │
       │  WorkshopBattlePayloadBridge.Commit(deck)
       ▼
┌────────────────────────┐
│  Static Payload Bridge │◄─────────────────────┐
└────────────────────────┘                      │
       ▲                                        │
       │     BattleSceneController               │
       │     TryConsume() → init combat          │
       │                                         │
       │     BattleSimulation (Phase 2)          │
       │           │                             │
       │           ▼                             │
       │     BattleResultBridge.Commit(result)   │
       │                                         │
       └─────────────────────────────────────────┘
```

---

## 7. Design Rationale

> **Battle knows Workshop; Workshop does not know Battle.**

Workshop only ever calls `WorkshopBattlePayloadBridge.Commit()`. It never imports anything from `ArcaneAtelier.Battle`. This means:

- Battle can be removed, replaced, or refactored without touching Workshop code.
- Workshop can be built and tested independently.
- The payload bridge acts as a stable, versioned contract between the two modules.

---

## 8. Future Refactor Candidate

Both modules currently share enums (`WorkshopElementAttribute`, `WorkshopSpellRole`) and payload structs that are conceptually cross-cutting. If a third module (e.g., `ArcaneAtelier.Exploration`) later needs elements or spell roles, these types should be extracted into a new `ArcaneAtelier.Core` assembly to prevent N×M dependency growth.

| Extracted Type | Destination | Consumers |
|----------------|-------------|-----------|
| `WorkshopElementAttribute` | `ArcaneAtelier.Core` | Workshop, Battle, future modules |
| `WorkshopSpellRole` | `ArcaneAtelier.Core` | Workshop, Battle, future modules |
| `WorkshopBattleCardEntry` | `ArcaneAtelier.Core` | Workshop, Battle |
| `WorkshopBattlePayload` + bridge | `ArcaneAtelier.Core` | Workshop, Battle |
