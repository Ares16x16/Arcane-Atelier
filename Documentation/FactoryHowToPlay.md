# Factory Scene Guide

For the full-game run structure, scene flow, story, and reward loop, use these as the primary game-level docs:

- [Game Design Document](GameDesignDocument.md)
- [Game Flow And Scene Guide](GameFlowAndSceneGuide.md)
- [Workshop Preparation Design](WorkshopPreparationDesign.md)

## What this scene is

This scene is the **workshop half** of Arcane Atelier.

The idea is simple:

1. Place spirit and factory nodes on the grid.
2. Let them produce elements and spells over time.
3. Route outputs into the correct next machine.
4. Build up a stock of finished spell cards.
5. Commit that stock as the deck payload for the future battle scene.

This slice does **not** implement the boss fight itself.
It only implements the factory scene and the handoff into battle.

For teammates: the generated workshop scene and generated database are the current source of truth for this guide.

---

## How this matches the game rules

The original game loop says:

1. Enter the factory scene.
2. Place spirit nodes to produce resources.
3. Convert resources into spell cards through factories.
4. Enter battle and use those spell cards.
5. Win rewards.
6. Return to the factory and optimize again.

This workshop slice implements the **factory-side part** of that loop:

- spirit generation
- element fusion
- spell shaping
- spell fusion
- reward-style unlock hooks
- battle deck payload export

This workshop slice does **not** yet implement:

- the battle scene
- bosses
- drag-and-drop casting
- auto-cast in battle
- health restore / level-up / passive-card choice after battle

So the correct way to describe the current implementation is:

> "The factory scene is playable now. The combat scene is not part of this slice yet."

---

## The resource system in plain English

There are two layers of elements:

- **Basic elements**
  - Fire
  - Water
  - Wind
  - Earth

- **Secondary elements**
  - Ice = Wind + Water
  - Thunder = Wind + Fire
  - Light = Earth + Fire
  - Dark = Earth + Water

In gameplay terms:

- spirit nodes create elements automatically
- factory nodes consume those elements
- the output becomes stronger spell cards

---

## The node types

### 1. Spirit nodes

Spirit nodes are the starting point of the line.

They continuously generate elemental resources.

Current default spirits:

- Fire Spirit
- Water Spirit
- Wind Spirit
- Earth Spirit

Reward-unlocked spirits also exist for:

- Ice
- Thunder
- Light
- Dark

## What you see at the start

When the generated scene first opens, the starter layout is already producing spells.

The opening layout demonstrates two working lines:

- **Top lane**
  - `Fire Spirit -> Arcane Conduit -> Element Shaper`
  - result: `Cinder Dart`

- **Middle lane**
  - `Water Spirit -> Arcane Conduits -> Element Fusion`
  - `Wind Spirit -> vertical Arcane Conduit -> Element Fusion`
  - `Element Fusion -> Element Shaper`
  - result: `Frost Pin`

You also begin with these nodes unlocked in the palette:

- Fire Spirit
- Water Spirit
- Wind Spirit
- Earth Spirit
- Arcane Conduit
- Element Shaper
- Element Fusion

You do **not** begin with every factory unlocked.

These still start locked:

- `Spell Fusion I`
- `Spell Fusion II`
- `Spell Fusion III`

To see more of the system:

1. open the boon drawer
2. unlock the factory you want to test
3. place it on the grid
4. route the correct inputs into it

---

### 2. Arcane Conduit

This is the relay node.

Use it to carry resources or spell cards forward through the line.

### 3. Element Fusion

This factory combines two different, non-opposing **basic** elements into one **secondary** element.

Examples:

- Wind + Water -> Ice
- Wind + Fire -> Thunder
- Earth + Fire -> Light
- Earth + Water -> Dark

### 4. Element Shaper

This factory turns **one element** into **one basic spell card**.

That means elements are the raw material, and the shaper turns them into usable spell cards.

### 5. Spell Fusion I

This is the first spell fusion tier.

It combines:

- two basic spells of the same element

Example:

- Fire spell + Fire spell -> stronger Fire spell

### 6. Spell Fusion II

This is the second spell fusion tier.

It combines:

- two different basic spells that are not opposing

Example:

- Fire spell + Thunder spell -> stronger mixed thunder-aligned result

This factory is meant to represent the "different but compatible" fusion rule from the design.

### 7. Spell Fusion III

This is the final spell fusion tier.

It combines:

- opposing intermediate spells

Example:

- Intermediate Fire + Intermediate Water -> advanced spell

---

## Step-by-step placement examples

These examples describe the intended way to place machines in the current workshop slice.

### Example A: Make a basic Fire spell

Goal:

- turn Fire into a basic spell card

Placement idea:

`Fire Spirit -> Arcane Conduit -> Element Shaper`

What to do:

1. place a `Fire Spirit`
2. place an `Arcane Conduit` directly to its right
3. place an `Element Shaper` to the right of the conduit
4. make sure all three face along the same line

What happens:

- `Fire Spirit` produces `Fire`
- the conduit passes `Fire` forward
- `Element Shaper` consumes `Fire`
- the shaper produces a basic Fire spell card

### Example B: Make Ice from Wind + Water

Goal:

- combine two basic elements into one secondary element

Placement idea:

```
Wind Spirit  -> [Element Fusion]
Water Spirit -> [Element Fusion]
```

Important:

- the fusion factory needs two valid inputs
- those inputs must enter from sides the machine accepts
- **direction has to match**

What to do:

1. place `Wind Spirit`
2. place `Water Spirit`
3. place `Element Fusion` so it can receive both lines
4. rotate the machine until both feeds enter valid input sides
5. place a conduit or shaper on its output side if you want to keep using the result

What happens:

- `Wind + Water -> Ice`

### Example C: Make an Ice spell

Goal:

- turn a fused secondary element into a spell card

Placement idea:

`Wind Spirit + Water Spirit -> Element Fusion -> Element Shaper`

What to do:

1. build Example B first
2. place `Element Shaper` on the output side of `Element Fusion`
3. make sure the shaper faces the incoming line

What happens:

- `Element Fusion` creates `Ice`
- `Element Shaper` consumes `Ice`
- the shaper produces a basic Ice spell card

### Example D: Make a stronger same-element spell

Goal:

- fuse two basic spells of the same element

Placement idea:

`Element Shaper -> Spell Fusion I`

What to do:

1. unlock `Spell Fusion I`
2. produce a steady flow of one basic spell type
3. route those basic cards into `Spell Fusion I`
4. keep the input line stable so the machine can collect pairs

What happens:

- two matching basic spells become one intermediate spell

### Example E: Make a mixed compatible spell

Goal:

- combine two different but non-opposing basic spells

Example pair:

- `Fire spell + Thunder spell`

What to do:

1. unlock `Spell Fusion II`
2. produce both source spells
3. route both lines into the same `Spell Fusion II`
4. keep both lines supplied

What happens:

- the factory consumes the compatible pair
- it outputs a stronger mixed intermediate result

### Example F: Make an advanced spell

Goal:

- combine opposing intermediate spells

Example pair:

- `Intermediate Fire + Intermediate Water`

What to do:

1. unlock `Spell Fusion III`
2. first build the earlier lines that produce the two intermediate spells
3. route both intermediate outputs into `Spell Fusion III`

What happens:

- the machine consumes the opposing intermediate pair
- it produces one advanced spell card

---

## What the player is actually trying to do

The workshop is not just about placing random machines.

The player goal is to build a **production line**.

That line should:

1. generate elements fast enough
2. deliver the right elements to the right machines
3. avoid blocking itself
4. keep producing finished spell cards over time

The more efficient the layout is, the more battle cards the player can prepare.

---

## How outputs move

Every node has a direction.

That direction matters because items only move when:

1. the current node outputs toward the next cell
2. the next node accepts input from that side
3. the next node accepts that item type
4. the next node still has room

This means placement and rotation both matter.

If the line is facing the wrong direction, nothing useful will move.

Simple rule:

- the arrow of the previous machine must point into the next machine
- the next machine must have an input on that side

Example:

- `Fire Spirit -> Arcane Conduit -> Element Shaper` works
- `Fire Spirit` facing away from the conduit does **not** work

---

## What happens to finished spell cards

Spell cards do not always go straight to the player immediately.

Current rule:

- if a spell card still has a valid downstream machine, it can continue through the line
- if it reaches the end of a valid line, it is auto-collected into the workshop's prepared card inventory

That is how the factory scene feeds the battle scene later.

---

## What the current spell cards contain

Every produced spell card now carries gameplay metadata for later battle use:

- spell name
- element
- tier
- role
  - Attack
  - Healing
  - Defense
- rarity band
- primary effect value
- hit count
- secondary effect value
- effect keyword

This is how the current code maps to the card rules you defined.

### Important note

`Card artwork` is **not implemented yet**.

The cards use placeholder colors and metadata only.

---

## Current Card List

The battle side consumes the spell metadata exported from the workshop payload.
The table below reflects the current generated content in `WorkshopProjectBootstrap.cs`, so names and numbers may still change during balance passes.

`Produced From` lists the main production path for each card, not every alternate recipe branch.
Battle now consumes `PrimaryValue`、`HitCount`、`SecondaryValue` and `EffectKeyword` through generated per-card definitions and runtime status handling.
The exact keyword semantics are still current-implementation rules rather than final long-term balance design, so timing, scaling, and wording may continue to evolve.

| Card | Tier | Element | Role | Effect | Keyword | Produced From |
| --- | --- | --- | --- | --- | --- | --- |
| `Cinder Dart` | Basic | Fire | Attack | Deal 8 damage. SecondaryValue: 1. | `Burn` | `Element Shaper` from `Fire` |
| `Tidal Mend` | Basic | Water | Healing | Restore 6 HP. SecondaryValue: 8. | `Regen` | `Element Shaper` from `Water` |
| `Zephyr Cut` | Basic | Wind | Attack | Deal 5 damage x2. SecondaryValue: 10. | `Expose` | `Element Shaper` from `Wind` |
| `Stoneguard Sigil` | Basic | Earth | Defense | Gain 7 shield. SecondaryValue: 18. | `Bulwark` | `Element Shaper` from `Earth` |
| `Frost Pin` | Basic | Ice | Attack | Deal 4 damage x2. SecondaryValue: 20. | `Slow` | `Element Shaper` from `Ice` |
| `Volt Javelin` | Basic | Thunder | Attack | Deal 7 damage. SecondaryValue: 15. | `Shock` | `Element Shaper` from `Thunder` |
| `Lumen Prayer` | Basic | Light | Healing | Restore 5 HP x2. SecondaryValue: 12. | `Bless` | `Element Shaper` from `Light` |
| `Gloam Ward` | Basic | Dark | Defense | Gain 6 shield. SecondaryValue: 20. | `Veil` | `Element Shaper` from `Dark` |
| `Inferno Brand` | Intermediate | Fire | Attack | Deal 14 damage x2. SecondaryValue: 2. | `Burn` | `Spell Fusion I` from `Cinder Dart + Cinder Dart` |
| `Tide Chorus` | Intermediate | Water | Healing | Restore 11 HP x2. SecondaryValue: 14. | `Regen` | `Spell Fusion I` from `Tidal Mend + Tidal Mend` |
| `Razor Monsoon` | Intermediate | Wind | Attack | Deal 8 damage x3. SecondaryValue: 12. | `Expose` | `Spell Fusion I` from `Zephyr Cut + Zephyr Cut` |
| `Bastion Pulse` | Intermediate | Earth | Defense | Gain 12 shield. SecondaryValue: 28. | `Ward` | `Spell Fusion I` from `Stoneguard Sigil + Stoneguard Sigil` |
| `Glacier Bind` | Intermediate | Ice | Attack | Deal 9 damage x2. SecondaryValue: 24. | `Freeze` | `Spell Fusion II` from `Zephyr Cut + Tidal Mend` |
| `Stormbreaker` | Intermediate | Thunder | Attack | Deal 16 damage. SecondaryValue: 18. | `Stun` | `Spell Fusion II` from `Zephyr Cut + Cinder Dart` |
| `Dawn Benediction` | Intermediate | Light | Healing | Restore 9 HP x3. SecondaryValue: 18. | `Radiance` | `Spell Fusion II` from `Stoneguard Sigil + Cinder Dart` |
| `Umbral Bastion` | Intermediate | Dark | Defense | Gain 10 shield x2. SecondaryValue: 24. | `Shade` | `Spell Fusion II` from `Stoneguard Sigil + Tidal Mend` |
| `Steam Requiem` | Advanced | Fire | Attack | Deal 20 damage x2. SecondaryValue: 28. | `Scald` | `Spell Fusion III` from `Inferno Brand + Tide Chorus` |
| `Worldsplit Tempest` | Advanced | Wind | Attack | Deal 12 damage x3. SecondaryValue: 20. | `Rend` | `Spell Fusion III` from `Razor Monsoon + Bastion Pulse` |
| `Eclipse Covenant` | Advanced | Light | Healing | Restore 14 HP x3. SecondaryValue: 24. | `Radiance` | `Spell Fusion III` from `Dawn Benediction + Umbral Bastion` |
| `Absolute Zero Surge` | Advanced | Ice | Defense | Gain 16 shield x2. SecondaryValue: 35. | `Static Shell` | `Spell Fusion III` from `Glacier Bind + Stormbreaker` |

---

## Controls

Current factory controls:

- `Left Click`: place or replace a node
- `Right Click`: remove a node
- `R`: rotate selected node
- `Q / E`: rotate placement direction
- `Space`: pause / resume time
- `Tab`: open / close boon drawer
- `F1`: show / hide control guide

---

## What teammates should assume

This workshop slice is a **playable factory prototype**, not final content.

Teammates should assume these parts may still change:

- spell names
- exact recipe numbers
- rarity weights
- effect values
- UI layout
- reward sequencing

Teammates should **not** assume these parts are unstable:

- factory scene owns production logic
- factory exports a battle payload
- battle should read the payload instead of inspecting workshop scene objects directly
- spirit -> element -> spell -> fused spell is the intended content flow

---

## Short version for non-technical teammates

If someone asks "How do I play Edward's part?", the simplest answer is:

> Place spirit nodes to make elements.  
> Route those elements into factories.  
> Use factories to turn elements into spells.  
> Use higher-tier factories to fuse spells into stronger ones.  
> Let finished spell cards collect into your deck for battle.

---

## Current status

This document describes the **current workshop implementation**.

It is meant to help teammates understand the slice today.
It is **not** a promise that names, values, or UI details are final.

If the scene content and this guide ever disagree, rebuild the generated workshop content first and treat that generated scene/database as authoritative.
