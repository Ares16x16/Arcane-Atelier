# Spell Assembly Module

## Purpose
This module owns the full `Spell Assembly Scene` gameplay slice for `Arcane Atelier`.
It provides a self-contained workshop loop where the player:

- builds on a fixed placement grid,
- places, removes, replaces, and rotates production nodes,
- runs production chains that generate resources and craft battle cards,
- monitors workshop inventory and prepared card output,
- receives reward-driven unlocks and efficiency upgrades,
- commits a battle payload for the combat team to consume.

## Runtime Scope
The runtime implementation lives under `Assets/ArcaneAtelier/Workshop/Runtime`.

Primary systems:

- `WorkshopSimulation`: authoritative factory state, production ticks, transfers, unlock state, and payload preparation.
- `WorkshopSceneController`: scene orchestration, placement state, reward application, and battle payload commit.
- `WorkshopGridView`: world-space grid rendering, node visuals, cell hover/selection, and placement input.
- `WorkshopHudPresenter`: immediate-mode production HUD for palette, inspection, inventory, rewards, and payload status.
- `WorkshopBattlePayloadBridge`: handoff surface for the combat scene.

## Factory Rules
### Grid and interaction

- The workshop uses a bounded 9x6 placement grid.
- Left click places or replaces a node on the hovered tile.
- Right click removes a node from the hovered tile.
- `Rotate Ghost` rotates the next placement.
- `R` rotates the currently selected placed node.

### Node model

Each node definition declares:

- category,
- input ports,
- output ports,
- buffer capacity,
- transfer rate per simulation step,
- one or more recipes.

All transfers are directional. A transfer succeeds only when:

- the source node exposes an output on the shared edge,
- the target node exposes an input on the opposite edge,
- the target node accepts the item,
- the target node has free buffer capacity.

### Production chains

Included chains:

1. `Ember Spring -> Flame Press -> Flame Bolt`
2. `Mist Well -> Frost Loom -> Frost Sigil`
3. `Ember Spring + Mist Well -> Arcane Infuser -> Ward Loom + Crystal Lattice -> Arcane Ward`

The third chain is deliberately reward-gated through node unlocks so reward integration can be tested before the shared reward scene exists.

## Reward Integration
The workshop scene exposes reward hooks directly through `WorkshopSimulation.ApplyReward`.

Supported reward effects:

- unlock a new node type,
- apply a permanent efficiency bonus to placed nodes of a target type,
- inject reserve resources for recovery / testing.

The debug reward panel exists as an integration harness until the shared reward system is assembled.

## Authoring Pipeline
The editor bootstrap lives at `Assets/ArcaneAtelier/Workshop/Editor/WorkshopProjectBootstrap.cs`.

Responsibilities:

- create or update all workshop item definitions,
- create or update all node definitions,
- create or update reward definitions,
- create or update the workshop content database,
- create the `SpellAssemblyScene`,
- register the generated scene in build settings.

The bootstrap is idempotent and auto-runs on editor load if generated content is missing.

## Integration Expectations
The combat team should not read workshop scene objects directly.
They should consume the committed payload from the battle bridge contract documented in `Documentation/WorkshopBattleContract.md`.

The reward / meta-progression owner should call into `WorkshopSimulation.ApplyReward` or replicate the same effect mapping when their final reward flow is connected.

## Visual Placeholder Policy
All visuals in this slice are code-generated placeholders by design:

- flat grid cells,
- tint-coded node bodies,
- port markers for flow readability,
- text overlays for buffer inspection.

This keeps the system functional and integration-ready while final art and UI assets are still in production.
