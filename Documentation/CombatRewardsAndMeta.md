# Arcane Atelier - Combat, Rewards, And Meta Progression

**Document Type:** System design specification  
**Audience:** Gameplay Engineering, Combat Design, UI/UX, QA, Production  
**Status:** Target full-game design grounded in the current battle/workshop boundary

---

## 1. Combat goals

Combat should feel immediately readable to players who understand deck-battlers:

- visible enemy intent
- draw, energy, and turn sequencing
- strong value from deck composition and card order
- clear difference between normal, elite, and boss battles

The workshop determines what deck enters battle. Combat determines whether that deck was actually good enough.

---

## 2. Recommended battle rules

### 2.1 Turn structure

Player turn:

1. draw cards
2. gain energy
3. inspect enemy intent
4. play cards
5. end turn

Enemy turn:

1. resolve the telegraphed intent
2. apply buffs, healing, summons, or status effects
3. advance intent pattern

### 2.2 Baseline numbers

Recommended starting values for the first playable combat loop:

- opening hand: `5`
- energy per turn: `3`
- max hand size: `10`
- deck reshuffles when draw pile is empty

### 2.3 Card role mapping

The current workshop card roles already map cleanly:

- `Attack`: damage, hits, debuffs
- `Defense`: shield, ward, damage reduction
- `Healing`: heal, regen, cleanse, bless
- `Utility`: reserved for future tempo, draw, or manipulation effects

---

## 3. Card identity rules

### 3.1 Produced card counts

Prepared card counts from the workshop define the number of copies added to the next battle deck.

### 3.2 Element identity

Elements should matter in battle, not only in production.

Current element relationships already support this:

- `Water` counters `Fire`
- `Wind` counters `Earth`
- `Ice` counters `Thunder`
- `Light` counters `Dark`

### 3.3 Tier identity

- `Basic` cards: cheap, reliable, role-defining
- `Intermediate` cards: stronger and more specialized
- `Advanced` cards: swing cards that decide boss fights

Recommended future contract addition:

- add an explicit `EnergyCost` field to the battle-facing card payload once battle moves beyond the skeleton stage

---

## 4. Enemy design

### 4.1 Enemy families

The run should use faction-style enemy groups so workshop preparation can be informed by encounter previews.

Recommended families:

- `Rift Beasts`: direct damage, frenzy, multi-hit
- `Hollow Clergy`: healing, curses, delayed pressure
- `Glass Knights`: defense, counterattacks, retaliation
- `Ash Swarm`: many weak bodies, status spread
- `Court Heralds`: elite support and scaling buffs

### 4.2 Intent clarity

Every enemy should expose intent one turn ahead:

- attack amount
- defend amount
- heal amount
- status application

Bosses may show multi-step patterns or phase transitions once encountered before.

---

## 5. Battle types

### 5.1 Normal battle

- short
- teaches matchup adaptation
- rewards one of three moderate choices

### 5.2 Elite battle

- harder mechanics
- stronger stats and status pressure
- shorter prep time before entry
- higher-value reward bundle

### 5.3 Boss battle

- act-ending test
- longer battle
- full mechanic identity
- grants major reward and permanent run advancement

Boss battles are forced by act progression rather than selected from the route board.

Recommended thresholds:

- `Act I`: `4` combat clears
- `Act II`: `5` combat clears
- `Act III`: `6` combat clears

---

## 6. Reward structure

### 6.1 Post-battle reward screen

After victory, present `3` choices drawn from weighted pools.

Reward categories:

- unlock a new node
- unlock a new spirit source
- improve one factory family
- add a passive relic
- gain a healing or recovery boon
- gain a route or prep-time modifier

### 6.2 Reward philosophy

Every reward should improve one of these three axes:

- better preparation speed
- better card quality
- better battle survivability

No reward should feel like pure filler.

---

## 7. Boss rewards

Boss victories should do more than normal rewards.

Each act boss grants:

- one major run reward
- one story beat
- one unlock or system escalation

Recommended major reward examples:

- unlock a new machine archetype
- unlock a new passive slot
- permanently improve one starting workshop condition
- gain one legacy point toward meta progression

---

## 8. Meta progression after final boss

The run should end with a permanent unlock. This is the answer to "what buffs us for the next round?"

Use a meta system called **Legacy Sigils**.

Legacy Sigils are account-level unlocks gained after final-boss clears, major boss milestones, or first-time feat completions.

Examples:

- `Kindled Start`: first preparation phase of each run gains `+20` prep ticks
- `Bound Relay`: begin each run with one extra `Arcane Conduit` unlocked and free
- `Warden Reserve`: start the first battle with `+8` shield
- `Guided Embers`: first reward screen of each run rerolls once for free
- `Deep Archive`: carry one prepared card family between encounters

Meta progression must improve the opening shape of future runs without invalidating skill.

---

## 9. Run loss and recovery

On defeat:

- show a clear statistics screen
- grant small consolation meta currency if the team wants softer progression
- return to main menu or quick restart

The defeat screen should reinforce learning:

- where the player died
- what the enemy was doing
- what card mix they brought
- whether their factory under-produced key roles

---

## 10. Vertical slice combat requirements

Minimum battle slice for a convincing prototype:

- player HP, shield, and draw loop
- one enemy intent display
- attack/heal/defense card resolution
- win/loss handling
- reward return flow into workshop

Do not wait for full keyword complexity before validating the workshop-to-battle loop.

---

## 11. Existing technical references

This design is intentionally compatible with:

- [Workshop ↔ Battle Contract](WorkshopBattleContract.md)
- `Assets/ArcaneAtelier/Battle/Runtime/BattleSceneController.cs`
- `Documentation/Battle/BattleCoreArchitecture.md`
- `Documentation/BattleWorkshopDependencies.md`

Those references define the current boundary and skeleton. This document defines the intended game behavior on top of that boundary.
