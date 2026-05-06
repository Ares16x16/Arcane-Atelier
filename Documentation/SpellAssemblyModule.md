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

- Grid bounds: **50 x 50** cells.
- Starter focus: the camera opens on the seeded starter workshop cluster, then the player can pan outward to the rest of the map.
- Input mapping (current debug UX):
- **LMB empty cell**: place the armed palette node.
- **LMB occupied cell**: select the placed node without replacing it.
- **RMB**: remove node.
- **RMB on corner conduit palette card**: arm the mirrored L-shape variant instead of the default corner.
- **R**: rotate selected placed node clockwise.
- **Rotate Placement**: rotate armed placement orientation.
  - **Mouse Wheel**: zoom the workshop map in / out.
  - **Hold Left Click + Drag**: pan the workshop map.

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

### Buffer input/output contract

Each placed node owns one local buffer. The buffer stores resource tokens and spell-card tokens waiting for either recipe execution or transfer.

Node input rules:

- **Source** nodes do not need inputs. Their recipes generate elemental resources into their own buffer.
- **Arcane Conduit** accepts and transfers element resources only.
- **Turning Conduit** accepts and transfers element resources only, using an L-shaped east-to-north default path that can be rotated.
- A mirrored `Turning Conduit Mirror` variant provides a west-to-north default path and is armed from the same palette card with right click.
- **Spell Conduit** accepts and transfers spell cards only.
- **Turning Spell Conduit** accepts and transfers spell cards only, using an L-shaped east-to-north default path that can be rotated.
- A mirrored `Turning Spell Conduit Mirror` variant provides a west-to-north default path and is armed from the same palette card with right click.
- **Battle Deck Collector** accepts spell cards only and has no output. Only spell cards buffered in this node are included in the battle deck snapshot.
- Other future **Storage** nodes may define their own accepted item lanes.
- **Processor** nodes, including `Element Fusion`, only accept items that appear in one of their recipe inputs.
- **Crafter** nodes, including `Element Shaper` and `Spell Fusion`, only accept items that appear in one of their recipe inputs.
- `Spell Fusion I / II / III` also allow direct adjacent card injection when the neighboring node is outputting a spell card into the fusion machine, even when the target-side port marker is not on that edge.

Node output rules:

- **Storage** nodes may output any buffered item they were allowed to accept. This is the only pass-through behavior.
- **Source**, **Processor**, and **Crafter** nodes may only output items that are declared as outputs of one of their own recipes.
- A processor/crafter must not leak raw recipe inputs through its output port. Example: `Element Fusion` may hold `Wind` and `Earth`, but it must not forward either token unless a recipe turns them into a valid output.
- `Spell Fusion I / II / III` all expose an east-facing spell-card output by default so their completed cards can be routed through `Spell Conduit` into `Battle Deck Collector`.

Recipe execution rules:

- Recipes execute before transfer on each simulation step.
- A recipe consumes its complete input set from the node buffer.
- If no recipe has all required inputs, the node keeps its buffer and waits.
- If a recipe succeeds, its outputs are added to the node buffer.
- If several recipes are possible in one buffer, the machine resolves the first matching recipe in recipe-list order, then scans from the top again on the next available cycle.
- After recipes execute, transfer moves only legal output items to connected downstream nodes.
- End-of-line spell cards stay visible in their machine or conduit buffer.
- Only spell cards inside `Battle Deck Collector` are included in the battle deck snapshot.
- `Spell Fusion I / II / III` ports are editable after placement. Click an edge to cycle that edge through input, output, and off. A placed spell-fusion node allows at most two inputs and one output.

Important invalid-input example:

- `Wind + Earth` is not an `Element Fusion` recipe in the current design.
- If both are inserted into `Element Fusion`, the machine should hold them but produce nothing.
- It should not forward `Wind` or `Earth` into a conduit, because those are recipe inputs, not legal outputs.

### Production execution

- Simulation advances in fixed-time steps (`SimulationStepSeconds`).
- Per-node recipe execution consumes inputs from local buffer first, then reserve inventory.
- Resource outputs enter node buffers.
- Spell cards travel through the line while a valid downstream receiver exists.
- End-of-line spell cards inside `Spell Conduit` remain visible as the card-lane buffer, but they are not included in the battle deck until routed into a `Battle Deck Collector`.
- The debug hack layout intentionally feeds every `Spell Fusion III` from two separate `Spell Fusion II` branches, warms the network for inspection, routes final cards into `Battle Deck Collector` blocks, and resets the debug preparation timer. This mirrors the recipe requirement that final cards consume two matching advanced spells and makes the buffers easier to inspect with `Hover + T`.

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
   - Intermediate: compatible intermediate spell fusion -> advanced spell.
   - Advanced: matching advanced spell fusion -> final advanced spell card.
5. **Reward-fed spirit expansion**
   - Secondary spirit nodes can be unlocked as post-battle rewards and dropped into the same production network.

Starter scene note:

- The generated opening layout already produces `Inferno Brand` through `Spell Fusion I -> Spell Conduit -> Battle Deck Collector` and `Frost Pin` through `Element Fusion`.
- `Element Fusion` is available from the start so players can immediately understand secondary-element crafting.
- `Spell Fusion I` is available from the start for feature testing; `Spell Fusion II / III` remain reward-gated.

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
