# Arcane Atelier — Factory Scene Architecture

**Document Type:** System Architecture Specification  
**Owner:** Gameplay Engineering (Factory)  
**Audience:** Gameplay Engineers, Technical Designers, QA, Producers  
**Status:** Current implementation baseline

---

## 1) Executive Summary

The Factory Scene is implemented as a data-driven simulation module with clear separation between:

- **Authoring data** (`ScriptableObject` content database + generated assets)
- **Runtime orchestration** (scene controller)
- **Simulation core** (deterministic step/update state machine)
- **Presentation/input** (grid view + HUD presenter)
- **Cross-scene handoff** (battle payload bridge)

The architecture is optimized for rapid iteration, reproducible content generation, and clean integration boundaries with battle systems.

---

## 2) High-Level Component Model

### 2.1 Authoring + generation layer (Editor)

- `WorkshopProjectBootstrap` auto-generates:
  - Item definitions (elements + spell cards)
  - Node definitions (spirits, factories, conduits)
  - Reward definitions (unlocks/boosts/resource grants)
  - `WorkshopContentDatabase`
  - `SpellAssemblyScene`

### 2.2 Runtime layer

- `WorkshopSceneController`
  - Owns simulation tick cadence, selection state, pause state, and user action routing.
- `WorkshopSimulation`
  - Owns all authoritative game state and transition logic.
- `WorkshopGridView`
  - Handles visual grid/node rendering + placement input.
- `WorkshopHudPresenter`
  - Displays inventory, rewards, throughput telemetry, and payload controls.
- `WorkshopBattlePayloadBridge`
  - Transfers prepared card counts to battle scene via explicit consume semantics.

---

## 3) Content Database Handling (Current)

The Factory module is **database-first**. All runtime behavior derives from `WorkshopContentDatabase` and referenced definitions.

### 3.1 Structure

`WorkshopContentDatabase` stores:

- `gridSize`
- `simulationStepSeconds`
- `placeableNodes[]`
- `debugRewards[]`
- `defaultLayout[]`

This makes scene behavior configurable without touching runtime code.

### 3.2 Authoring safety

Database handling includes the following protections:

1. **Null-safe configure path**
   - `Configure()` guards against null arrays using `Array.Empty<T>()` fallbacks.
2. **Validation contract** via `ValidateContent()`
   - Grid bounds sanity checks
   - Duplicate node IDs
   - Duplicate reward IDs
   - Missing seed node references
   - Default layout bounds validation
3. **Editor feedback**
   - `OnValidate()` emits warnings with grouped validation output.
4. **Fail-fast bootstrap**
   - Editor generation calls `ValidateContent()` and throws if invalid.

### 3.3 Runtime use

- `WorkshopSceneController` requires a valid database at startup.
- If missing, controller logs an error and disables itself (fail-fast runtime guard).
- `WorkshopSimulation` reads grid bounds, unlock defaults, and seed layout from the database at initialization/reset.

---

## 4) Simulation Core Design

### 4.1 State ownership

`WorkshopSimulation` is the single source of truth for:

- Node placement states (`WorkshopNodeState`)
- Reserve resources
- Prepared battle cards
- Unlock state
- Throughput counters

No view/UI object mutates gameplay state directly.

### 4.2 Step execution model

Per step (`Step(deltaTime)`):

1. Accumulate simulated time.
2. Sort/iterate nodes in deterministic order.
3. Execute first valid recipe per node with cycle progress.
4. Run buffered transfer pass between adjacent nodes.
5. Raise change notification when state mutates.

Hot path instrumentation is provided by profiler markers for step, recipe, and transfer phases.

### 4.3 Buffer/transfer rules

- Port-direction validation on both source and target edges.
- Capacity checks before acceptance.
- Per-node transfer budget (`MaxTransferPerStep`).
- Safe iteration through transfer buffer snapshots to avoid collection mutation hazards.

### 4.4 Throughput telemetry

Simulation tracks cumulative totals and exposes:

- Element production rate
- Element consumption rate
- Spell production rate

Telemetry is surfaced through `WorkshopFlowStatsView` for HUD diagnostics.

---

## 5) Scene Orchestration and Time Control

`WorkshopSceneController` responsibilities:

- Wiring dependencies (`WorkshopGridView`, `WorkshopHudPresenter`, content DB)
- Running fixed-step simulation with bounded catch-up iterations per frame
- Handling placement/removal/rotation commands
- Applying rewards and workshop reset
- Committing payload to battle bridge
- Managing pause/resume state (`Time.timeScale` + simulation guard)

The bounded catch-up policy prevents long-frame stalls during heavy frame hitches.

---

## 6) Input and Presentation Layer

### 6.1 Grid View

- Mouse-to-cell conversion for placement/remove interactions
- Node visual rendering with directional IO markers
- Selected and hovered cell highlighting

### 6.2 HUD Presenter

- Palette and node inspector
- Inventory + prepared card display
- Debug reward controls
- Throughput telemetry panel
- Pause/resume and payload commit actions

Current UI stack is IMGUI for rapid iteration and debug support.

---

## 7) Factory-to-Battle Boundary

Factory output is exported via `WorkshopBattlePayloadBridge` only.

- `Commit()` snapshots prepared cards.
- `TryConsume()` performs one-time read + clears staged payload.
- `Clear()` supports explicit cleanup flows.

This avoids hard references between battle systems and factory scene internals.

---

## 8) Initialization and Reset Lifecycle

### 8.1 Cold start

1. Editor bootstrap ensures generated content + scene exist.
2. Scene controller validates DB and creates simulation.
3. Simulation unlocks default nodes and places default layout.
4. Views initialize and render current state.

### 8.2 Runtime reset

`ResetWorkshop()`:

- Clears node/reserve/prepared state
- Re-applies default unlocks and layout seeds from database
- Resets placement rotation and refreshes UI state

---

## 9) Non-Functional Characteristics

### Strengths

- Data-driven pipeline supports fast content iteration.
- Deterministic iteration order improves reproducibility.
- Validation + fail-fast behavior reduces invalid-content risk.
- Explicit integration boundary keeps combat coupling low.

### Known limitations

- No save/load persistence yet.
- IMGUI is not final production UX stack.
- No deterministic multiplayer/network authority model.

---

## 10) Recommended Next Milestones

1. Add automated playmode coverage for transfer and fusion edge-cases.
2. Add serialization for run persistence and meta progression.
3. Migrate UI to retained-mode production UI framework.
4. Add structured telemetry events for live balancing workflows.
5. Introduce explicit contract versioning for future payload expansion.
