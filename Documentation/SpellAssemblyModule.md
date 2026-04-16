# Spell Assembly Module

## 1) Module intent

The Spell Assembly module owns the **entire workshop-side gameplay loop** for Arcane Atelier’s vertical slice.
It is intentionally self-contained so design, engineering, and combat integration can iterate independently.

Player-facing loop in this slice:

1. Assemble a production topology on a constrained grid.
2. Route directional resource flow through placed nodes.
3. Convert resources into battle cards via recipes.
4. Apply reward effects (factory unlocks / spirit unlocks / throughput tuning / reserve recovery).
5. Commit a battle payload snapshot for combat-scene consumption.

Produced spell cards now carry explicit factory-authored metadata:
- element attribute
- tier
- role (attack / healing / defense)
- rarity weight -> rarity band
- primary / secondary effect values

---

## 2) Runtime architecture

Runtime code lives in `Assets/ArcaneAtelier/Workshop/Runtime`.

### Core systems

- **`WorkshopSimulation`**
  Authoritative simulation state. Handles node placement state, production ticks, transfers, reward application, inventory views, and payload commit.

- **`WorkshopSceneController`**
  Scene orchestration layer. Owns selection state, palette state, status messaging, simulation stepping cadence, and system wiring.

- **`WorkshopGridView`**
  World-space grid and node visualization. Handles hover/select feedback and direct placement/remove/rotate input.

- **`WorkshopHudPresenter`**
  Immediate-mode debug HUD for node palette, selection inspector, inventory visibility, reward hook panel, and payload status.

- **`WorkshopBattlePayloadBridge`**
  Cross-scene handoff surface used to transfer prepared cards to combat.

---

## 3) Simulation contract and rules

### Grid + interaction model

- Grid bounds: **9 x 6** cells (configurable in content DB).
- Input mapping (current debug UX):
  - **LMB**: place/replace using armed palette node.
  - **RMB**: remove node.
  - **R**: rotate selected placed node clockwise.
  - **Rotate Ghost**: rotate armed placement orientation.

### Node behavior model

Each node definition provides:

- Category (`Source`, `Processor`, `Crafter`, `Storage`)
- Input/output port masks
- Buffer capacity
- Max transfer per simulation step
- Recipe list

Transfer across an edge succeeds only when **all** checks pass:

1. Source exposes output on the shared edge.
2. Target exposes input on opposite edge.
3. Target accepts the item type.
4. Target has remaining buffer capacity.

### Production execution

- Simulation advances in fixed-time steps (`SimulationStepSeconds`).
- Per-node recipe execution consumes inputs from local buffer first, then reserve inventory.
- Resource outputs enter node buffers.
- Spell cards travel through the line while a valid downstream receiver exists.
- End-of-line spell cards auto-collect into `PreparedCards` (battle-facing inventory).

---

## 4) Included factory chains (current slice content)

1. **Spirit generation**
   - Fire / Water / Wind / Earth spirit nodes generate basic elements.
2. **Element fusion**
   - Wind + Water -> Ice
   - Wind + Fire -> Thunder
   - Earth + Fire -> Light
   - Earth + Water -> Dark
3. **Element shaping**
   - 1 element -> 1 basic spell card (supports all 8 elements).
4. **Spell fusion (3 tiers)**
   - Basic: same-element basic spell fusion -> intermediate spell.
   - Intermediate: non-opposing mixed basic spell fusion -> secondary intermediate spell.
   - Advanced: opposing intermediate spell fusion -> advanced spell card.
5. **Reward-fed spirit expansion**
   - Secondary spirit nodes can be unlocked as post-battle rewards and dropped into the same production network.

Starter scene note:

- The generated opening layout already produces `Cinder Dart` and `Frost Pin`.
- `Element Fusion` is available from the start so players can immediately understand secondary-element crafting.
- `Spell Fusion I / II / III` remain reward-gated.

---

## 5) Reward integration surface

Primary entry point: `WorkshopSimulation.ApplyReward(WorkshopRewardDefinition reward)`

Supported reward kinds:

- **UnlockNode**: unlocks target node in placement palette.
- **EfficiencyBoost**: adds permanent speed bonus to placed nodes matching the target filter.
- **GrantItems**: injects reserve resources for testing/recovery flows.

The debug reward panel is temporary infrastructure and should be replaced by the project’s shared reward pipeline when available.

---

## 6) Content and scene authoring pipeline

Editor bootstrap script:

- `Assets/ArcaneAtelier/Workshop/Editor/WorkshopProjectBootstrap.cs`

Responsibilities:

- Upsert item, node, and reward ScriptableObject assets.
- Build/update `WorkshopContentDatabase`.
- Build/update `SpellAssemblyScene`.
- Register generated scene in Build Settings.

Design intent:

- **Idempotent generation** to minimize setup drift across machines.
- Fast project onboarding for designers and gameplay programmers.

Current source-of-truth note:

- The generated assets under `Assets/ArcaneAtelier/Workshop/Generated/*` and the generated `SpellAssemblyScene` are the authoritative content for this slice.
- The runtime fallback content path exists only as a safety net for empty / missing-content startup and should not be treated as the design baseline.

---

## 7) Integration guidance

### Combat integration

Combat systems should consume only the bridge payload contract described in `Documentation/WorkshopBattleContract.md`.
Do not read workshop scene objects directly.

### Reward/meta integration

Meta systems can call `WorkshopSimulation.ApplyReward` directly or replicate its effect mapping in their own orchestration layer.

---

## 8) Known limits (intentional for slice stage)

- UI stack is IMGUI-driven and still a prototype shell, not final production UI.
- Node visuals are generated placeholders (no final art pipeline hookup).
- Battle, boss, health, level-up, and passive-card systems are intentionally outside this factory-scene module.
- Runtime fallback content is not yet fully equivalent to the generated content database.
- No save/load persistence in this slice.
- No deterministic multiplayer lockstep guarantees yet.

---

## 9) Productionization recommendations

1. Migrate HUD to retained UI (UI Toolkit or uGUI) with input abstraction.
2. Add playmode tests for recipe resolution, transfer edge-cases, and payload commit semantics.
3. Introduce save/load serialization for node layout + inventory + unlock state.
4. Add telemetry hooks (placement churn, reward usage, throughput bottlenecks).
5. Externalize tuning values for live-ops balancing workflows.
