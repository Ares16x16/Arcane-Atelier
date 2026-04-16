# Arcane Atelier — Factory Scene Technical Design

**Document Type:** Gameplay Module Spec
**Owner:** Gameplay Engineering (Factory)
**Audience:** Design, Engineering, QA, Technical Production, Combat Integration
**Status:** Active implementation baseline

---

## 1. Module Purpose

The Factory Scene module implements the production half of Arcane Atelier’s core loop:

1. Place and route factory nodes on a fixed grid.
2. Generate elemental resources from spirit nodes.
3. Transform elements into spell cards through factory chains.
4. Commit produced cards to the battle handoff bridge.
5. Receive post-battle rewards and expand factory capability over successive runs.

This module is intentionally decoupled from battle runtime systems except through the explicit battle payload contract.

---

## 2. Scope of Implementation (Current)

Implemented and supported in this slice:

- Grid-based placement with snap, replace, remove, and rotate interactions.
- Directional IO transfer model (input/output ports with edge validation).
- Element production and transformation pipeline:
  - Basic elements: Fire, Water, Wind, Earth
  - Secondary elements: Ice, Thunder, Light, Dark
- Spell production pipeline:
  - Element Shaping (element -> basic spell)
  - Spell Fusion Basic (same-element basic -> intermediate)
  - Spell Fusion Intermediate (non-opposing basic mix -> secondary intermediate)
  - Spell Fusion Advanced (opposing intermediate -> advanced)
- Reward hooks (unlock node, efficiency boost, reserve grant).
- Runtime throughput telemetry panel (element production, element consumption, spell production).
- Pause/resume time control for scene-level simulation suspension.
- Battle payload preparation and commit.

Out of scope in this slice:

- Combat effect resolution, enemy AI, and card drag targeting UX.
- Save/load persistence.
- Final production UI art and UX polish.

---

## 3. Runtime Architecture

Runtime code path: `Assets/ArcaneAtelier/Workshop/Runtime`

### Core classes

- **`WorkshopSimulation`**
  - Authoritative state machine for node placement state, recipe execution, transfer simulation, rewards, and payload commit.
  - Exposes inventory view and throughput stats view for UI.

- **`WorkshopSceneController`**
  - Scene orchestrator that owns simulation tick cadence, selected cell/node state, palette state, status messaging, and pause control.

- **`WorkshopGridView`**
  - Grid and node visualization with direct world interaction (hover/select/place/remove/rotate).

- **`WorkshopHudPresenter`**
  - IMGUI debug/production-support HUD for palette, inspector, inventory, rewards, throughput, payload status, and pause controls.

- **`WorkshopBattlePayloadBridge`**
  - One-way handoff boundary from factory output into battle scene consumption.

---

## 4. Factory Rules Implemented

### 4.1 Grid and placement

- Factory area uses a bounded grid (`WorkshopContentDatabase.GridSize`, currently `9x6`).
- Placement is deterministic and cell-snapped.
- Nodes can be rotated in 90-degree increments.
- Transfers occur only through valid directional adjacency.

### 4.2 Transfer validity

An item transfer is valid only when all conditions pass:

1. Source exposes an output port on the shared edge.
2. Target exposes a matching input port on the opposite edge.
3. Target accepts the item type.
4. Target has available buffer capacity.

### 4.3 Recipe execution

- Simulation ticks in fixed steps (`SimulationStepSeconds`).
- Inputs are consumed from local buffers first, then reserve inventory.
- Resource outputs are buffered in-node.
- Card outputs are added to `PreparedCards` for battle payload export.

### 4.4 Element and spell logic

Implemented transformations:

- **Element Fusion Factory**
  - Wind + Water -> Ice
  - Wind + Fire -> Thunder
  - Earth + Fire -> Light
  - Earth + Water -> Dark

- **Element Shaping Factory**
  - 1 element -> 1 basic spell card (all 8 elements supported)

- **Spell Fusion Factory — Basic**
  - 2 same-element basic spells -> 1 intermediate spell

- **Spell Fusion Factory — Intermediate**
  - Non-opposing mixed basic spells -> secondary intermediate spell

- **Spell Fusion Factory — Advanced**
  - Opposing intermediate spell pairs -> advanced spell card

---

## 5. Progression and Reward Hooks

Primary API: `WorkshopSimulation.ApplyReward(WorkshopRewardDefinition reward)`

Supported reward effects:

- **UnlockNode**: unlocks new spirit/factory nodes in placement palette.
- **EfficiencyBoost**: permanently increases cycle speed for target node type.
- **GrantItems**: injects reserve resources to recover stalled lines.

Current debug rewards mirror the intended post-boss unlock cadence for factory expansion.

---

## 6. Time and Scene Behavior

- Factory simulation supports explicit pause/resume controls.
- Pause is implemented by scene controller suspension plus timescale toggle for expected scene-level behavior.
- Throughput metrics continue to represent cumulative runtime rates over simulated time.

---

## 7. Authoring and Content Pipeline

Editor bootstrap: `Assets/ArcaneAtelier/Workshop/Editor/WorkshopProjectBootstrap.cs`

Bootstrap responsibilities:

- Generate/update item, node, reward ScriptableObject assets.
- Generate/update `WorkshopContentDatabase`.
- Validate generated content (`ValidateContent`) and fail fast on invalid assets.
- Generate/update `SpellAssemblyScene` and ensure build registration.

This pipeline is idempotent and intended for repeatable team onboarding.

---

## 8. Integration Contract

- Battle must consume factory output via `WorkshopBattlePayloadBridge` only.
- Battle systems must never infer state from scene objects or factory ScriptableObjects.
- Reward/meta systems may call `ApplyReward` directly or map equivalent effects in a higher-level progression service.

Refer to `Documentation/WorkshopBattleContract.md` for payload details.

---

## 9. Operational Notes (Production Readiness)

Known technical debt deliberately retained for this phase:

- IMGUI HUD (functional but not final UX stack).
- No save/load serialization for run continuity.
- No deterministic networking guarantees.

Recommended next production milestones:

1. Migrate UI to retained-mode production stack.
2. Add automated playmode coverage for transfer/recipe/payload edge cases.
3. Introduce persistence layer for run state and unlocked factory content.
4. Add telemetry events for throughput bottleneck diagnostics.
