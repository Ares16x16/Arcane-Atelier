# Arcane Atelier - Workshop Preparation Design

**Document Type:** System design specification  
**Audience:** Gameplay Engineering, System Design, UI/UX, QA  
**Status:** Target workshop loop built on the current factory prototype

---

## 1. Workshop role in the full game

The workshop is the signature system of Arcane Atelier.

Its job is to answer one question before every fight:

**What deck can the player manufacture before the next breach opens?**

The workshop phase should feel different from combat:

- spatial instead of hand-based
- throughput-focused instead of turn-focused
- strategic instead of reactive

---

## 2. Workshop loop per encounter

1. Enter preparation with route-defined context.
2. Read the encounter preview:
   - enemy faction
   - expected role pressure
   - prep tick budget
3. Adjust the workshop layout.
4. Run the machine and monitor output.
5. Decide whether to keep optimizing or deploy early.
6. Commit the prepared loadout.
7. Enter battle.

Boss encounters use the same workshop loop, but they are triggered by act progression rather than by choosing a boss route node.

---

## 3. Planning mode and live mode

To keep the system readable, workshop play should support two time states:

### 3.1 Planning mode

- time paused
- place, remove, rotate, inspect
- no tick consumption
- used for setup and troubleshooting

### 3.2 Live mode

- time running
- machines execute recipes
- prep ticks count down
- throughput panels matter

This preserves the current pause/resume feel from the factory prototype while making the countdown system fair.

---

## 4. Preparation tick rules

### 4.1 Tick ownership

The workshop uses **Preparation Ticks**.
Battle uses **Turns**.
These are different clocks and should never be conflated in UI or code.

### 4.2 Tick consequences

When prep ticks reach zero:

- the workshop locks
- the current prepared card snapshot is taken automatically if the player has not already deployed
- battle starts immediately

Current runnable prototype behavior:

- `Forge And Deploy` commits the current payload and enters battle immediately
- `Advance 1 Prep Tick` runs exactly one workshop simulation step and consumes one preparation tick
- live workshop time automatically advances preparation ticks while unpaused
- if the payload is empty when battle starts, battle supplies the emergency deck described below

### 4.3 Early deploy reward

If the player deploys before the timer ends, grant one small benefit:

- `+1` opening hand size, or
- `+1` opening energy, or
- `+X` starting shield

Only one initiative bonus should be active in the shipped build.

---

## 5. Core workshop resources

### 5.1 Resource layers

The current elemental ladder remains the base production model:

- basic elements: `Fire`, `Water`, `Wind`, `Earth`
- secondary elements: `Ice`, `Thunder`, `Light`, `Dark`

### 5.2 Product ladder

The machine converts resources upward in a clear hierarchy:

1. spirit -> element
2. element -> basic spell
3. basic spell -> intermediate spell
4. intermediate spell -> advanced spell

That ladder already exists in the current workshop docs and code shape, so the full-game design keeps it.

---

## 6. Starting node roster

The recommended starting run roster should stay close to the current implementation:

- `Fire Spirit`
- `Water Spirit`
- `Wind Spirit`
- `Earth Spirit`
- `Arcane Conduit`
- `Element Fusion`
- `Element Shaper`

Reward-gated early unlocks:

- `Spell Fusion I`
- `Spell Fusion II`
- `Spell Fusion III`
- secondary spirit sources

This lets the first run start simple and gradually expose the more expressive machinery.

---

## 7. Future factory expansion nodes

To make the workshop deepen across a full game, add a second-wave roster after the current prototype stabilizes:

### 7.1 Rune Compressor

- combines two identical basic spells into one compressed spell
- lower output count, higher quality
- useful for boss prep

### 7.2 Prism Splitter

- duplicates a spell into two weaker variants
- useful for swarm fights or status-spread plans

### 7.3 Archive Vault

- preserves a limited number of prepared cards between encounters
- gives the player a long-term deck-planning tool

### 7.4 Catalyst Furnace

- consumes extra elemental input to add a modifier to downstream cards
- example outcomes: burn chance, stun chance, ward gain

These are expansion candidates, not requirements for the first design pass.

---

## 8. Encounter-aware preparation

Preparation should respond to what the player chose on the route board.

Examples:

- an aggressive beast encounter favors shields and burst attack cards
- a healer cult encounter favors multi-hit pressure and disruption
- an elite stone guardian favors high-tier penetration and sustained defense

This means the workshop is not only about throughput. It is also about producing the right role mix.

Recommended act boss thresholds:

- `Act I`: after `4` cleared combat encounters
- `Act II`: after `5` cleared combat encounters
- `Act III`: after `6` cleared combat encounters

---

## 9. Encounter pressure scaling

The workshop should feel increasingly unstable as the run advances.

Recommended pressure levers:

- shorter prep windows
- more corrupted nodes in later acts
- route modifiers that block certain output lanes
- temporary hazards such as:
  - one disabled cell
  - one slowed machine family
  - one unstable element type for this encounter

These hazards should appear mostly on elite, omen, and late-act nodes.

---

## 10. Output rules for battle

### 10.1 Battle payload source of truth

The workshop produces a snapshot. Battle consumes that snapshot.

### 10.2 Recommended combat interpretation

- prepared card counts determine the battle deck composition for the next fight
- cards cycle within combat like a roguelike deck-battler
- cards are not permanently destroyed by being played in combat
- the next workshop phase forges a fresh deck snapshot for the next encounter

This keeps the workshop meaningful without punishing the player with permanent per-fight card depletion.

### 10.3 Empty-loadout protection

The game should never hard-lock because the player deployed with no usable cards.

Recommended fallback:

- if the forged payload is empty, add a tiny emergency deck:
  - `2x Arcane Bolt`
  - `2x Guard Thread`

The fallback should be intentionally weak so players still care about building correctly.

---

## 11. Workshop UI requirements

The workshop screen should clearly surface:

- remaining prep ticks
- current route node type
- encounter preview
- prepared card inventory
- throughput by resource and spell family
- locked versus unlocked node palette entries
- deploy button state

Optional but high-value UI:

- projected output forecast if current throughput continues
- warning when no attack card is being produced
- warning when no downstream route exists for a high-tier card

---

## 12. First-act target experience

The first act should teach the loop in a controlled order:

1. first preparation teaches spirit -> conduit -> shaper
2. second preparation teaches fusion for secondary elements
3. first elite introduces pressure and shorter prep time
4. first boss expects at least one deliberate upgrade or fusion path

The workshop should feel solved enough to be readable, but not so solved that every run uses the same line.

---

## 13. Design references already in the repo

This design directly builds on the existing workshop rules already described in:

- [Factory Scene Guide](FactoryHowToPlay.md)
- [Spell Assembly Module](SpellAssemblyModule.md)
- [Factory Architecture](FactoryArchitecture.md)

The difference is scope:

- those docs describe the module and current slice
- this doc describes how that module behaves inside the full roguelike run
