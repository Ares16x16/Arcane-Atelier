# Arcane Atelier - Game Design Document

**Document Type:** High-Level Game Design Document  
**Audience:** Design, Engineering, UI/UX, Art, Production, QA  
**Status:** Target full-game direction built from the current workshop prototype and the `integration` branch flow reference

---

## 1. High concept

`Arcane Atelier` is a fantasy roguelike deck-battler where the player does not find cards in loot piles or draft them from merchants. They manufacture their next battle deck inside a magical workshop under siege.

Every run follows one repeating promise:

1. Choose where the next breach will be answered.
2. Reconfigure the atelier before the attack arrives.
3. Forge a battle loadout from spirits, conduits, and factories.
4. Fight a short, high-pressure encounter using the exact card mix you produced.
5. Claim a reward that changes both future factory lines and future combat options.
6. Survive enough encounters in the act to force the boss breach.

The structure should feel like `Slay the Spire` in its run readability, route planning, elite/boss pacing, and reward cadence, while the identity comes from the workshop preparation phase.

---

## 2. Player fantasy

The player is the **Warden Artificer**, master of the last living spell-forge known as the Arcane Atelier.

Fantasy goals:

- Build a machine that feels clever, not random.
- Watch raw elemental spirits become combat spells.
- Decide whether to spend time optimizing or deploy early.
- Feel a growing siege as preparation time gets shorter deeper into the run.
- Defeat a final boss and carry a meaningful legacy into the next run.

---

## 3. Why the atelier exists

The workshop is not just a mechanic. It is the central fiction of the world.

The Arcane Atelier is an ancient defense engine built to refine loose elemental essence into stable sigils. Ordinary mages can cast a handful of spells before burning out, but the atelier can mass-produce battle-ready spell cards if it is fed spirits, aligned through conduits, and shaped through rune machinery.

This explains:

- why the player has a factory in a fantasy world
- why cards are "forged" before each fight
- why routing, throughput, and timing matter
- why returning to the workshop between encounters is natural

The player does not walk from room to room like a dungeon crawler. They defend a threatened region by opening controlled breach gates from the atelier and sending a bound combat projection into each battle.

---

## 4. Game pillars

### 4.1 Build under pressure

Preparation is limited by a visible countdown measured in **Preparation Ticks**. The atelier is strongest when the player can think ahead, not when they can idle forever.

### 4.2 Manufacture your deck

The deck is authored by the player's machine layout. Better routing creates better card density, stronger tiers, and cleaner role balance.

### 4.3 Route with intent

Like a strong roguelike map game, the player should make informed route choices. Every branch should imply a different risk, reward, enemy profile, and prep budget.

### 4.4 Grow across runs

Boss clears unlock **Legacy Sigils** and new atelier options for future runs. A full run should end with permanent forward motion.

---

## 5. Core run loop

1. Start a new run from the main menu.
2. Receive an act briefing and choose the next route node.
3. Enter workshop preparation with a limited tick budget.
4. Build or adjust the production line.
5. Commit the forged loadout and deploy into battle.
6. Resolve combat.
7. On victory, choose one reward and either return to route selection or trigger the act boss once the battle threshold has been met.
8. On boss victory, gain a major reward and a meta-progression unlock.
9. Repeat across three acts, then defeat the final boss.

---

## 6. Scene structure

The target production flow should stay close to the existing integration-friendly scene count:

- `MainMenuScene`
- `WorkshopScene`
- `BattleScene`

The recommendation is to keep most non-combat states inside `WorkshopScene` as UI overlays or state panels:

- Run briefing
- Route board
- Workshop preparation
- Reward choice
- Act interlude
- Legacy unlock summary

This keeps scene management simple while still supporting a full roguelike loop.

---

## 7. Run structure

### 7.1 Acts

The run is divided into three escalating acts:

1. **Act I - Ember March**
   The outer wards fall. The player learns the basic spirit and shaping loop.
2. **Act II - Glass Wilds**
   The breach spreads through overgrown ley gardens. Mixed-element enemies and elites become common.
3. **Act III - Hollow Crown**
   The siege reaches the celestial core beneath the atelier. Prep windows are shortest and boss mechanics are harshest.

### 7.2 Node types

Each act map contains a branching route with node categories similar in readability to `Slay the Spire`, but reskinned for Arcane Atelier:

- `Skirmish`: baseline combat node
- `Elite Hunt`: harder fight, stronger reward, shorter prep
- `Harvest`: lighter encounter or no encounter, extra resources or recipe support
- `Sanctum`: heal, cleanse corruption, or adjust passives
- `Anvil`: upgrade one factory, spell family, or starting condition
- `Omen`: authored event with tradeoffs

Bosses are not chosen as normal route nodes. They appear automatically after a fixed number of cleared combat encounters in the act.

Recommended thresholds:

- `Act I`: boss after `4` cleared combat encounters
- `Act II`: boss after `5` cleared combat encounters
- `Act III`: final boss after `6` cleared combat encounters

### 7.3 Prep pressure

Preparation time gets shorter as a run progresses. This is a core identity rule.

The player should feel:

- comfortable experimentation early in the run
- meaningful time pressure mid-run
- real siege urgency late in the run

---

## 8. Workshop to combat relationship

The workshop does not directly execute battle actions. It produces the loadout that battle consumes.

Design rule:

- workshop determines the next battle deck composition
- battle determines tactical execution
- rewards feed both sides of the loop

This preserves clear ownership:

- workshop is about production, throughput, and deck shaping
- battle is about timing, card order, defense, and enemy intent reading

---

## 9. Story premise

For centuries the Arcane Atelier stabilized the kingdom's ley network by binding elemental spirits into useful sigils. That network is now being consumed by the **Hollow Court**, a mythic host of monsters born from failed constellations and broken vows.

The Warden Artificer cannot abandon the atelier because it is also the seal holding the world together. Instead, each battle is fought through a forge-marked projection sent through breach gates into the field.

That structure explains why the player:

- always returns to the workshop
- fights escalating breach encounters
- gains elemental rewards from defeated monsters
- unlocks deeper power only after defeating major breach sovereigns

---

## 10. Standard features the game should include

To feel like a complete commercial roguelike, the game should include the following support features in addition to the main loop:

- New Run / Continue Run / Settings / Credits on the main menu
- Pause menu during workshop and battle
- Codex for elements, nodes, enemies, and keywords
- Run summary screen on victory and defeat
- Tooltips for card keywords, enemy intents, and route node types
- Save-and-resume support between encounters
- Tutorial prompts for the first run

---

## 11. Target success metrics

Design targets for the first polished vertical slice:

- Average normal battle length: 4 to 7 turns
- Average boss battle length: 7 to 10 turns
- Average workshop prep duration: 2 to 4 minutes of active play
- Meaningful reward decision after every victory
- At least three viable early-run factory archetypes

---

## 12. Deliverable summary

This GDD is the hub document. The detailed follow-up docs are:

- [Game Flow And Scene Guide](GameFlowAndSceneGuide.md)
- [Workshop Preparation Design](WorkshopPreparationDesign.md)
- [Combat, Rewards, And Meta Progression](CombatRewardsAndMeta.md)
- [Narrative And World Guide](NarrativeAndWorld.md)
- [Implementation Reference](ImplementationReference.md)
