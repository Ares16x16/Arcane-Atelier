## Core Rules
### Game Positioning

Arcane Atelier is a 2D top-down factory-building and roguelike boss combat game. Players first place nodes in the factory scene to produce elements and spell cards, then enter the battle scene to use those cards against bosses. After each victory, players gain random rewards, which feed back into both factory building and combat deck construction, forming a loop of continuous growth.

### Gameplay Loop
1. The player enters the **factory scene**.
2. They place **spirit nodes** to produce resources.
3. Resources are converted into **spell cards** through conveyor belts and **factory nodes**.
4. The player enters the **battle scene**, uses the continuously produced **spell cards** to defeat the boss, and tries to survive.
5. After the battle, the player restores health and gains reward cards, including new **spirit nodes**, **passive cards**, and newly unlocked spell recipes.
6. The player returns to the **factory scene** to continue producing elements and spell cards and to optimize the production flow.
7. The next cycle begins.
### Scene Rules

The two scenes share the same time system, and time can be paused.

**Factory Scene**
1. The scene uses a fixed placement area based on a grid, and all nodes automatically snap to the grid.
2. The player can place the following from their inventory:
  a. **spirit nodes**
  b. **factory nodes**, including:
    * Element Fusion Factory: combines basic .elements into secondary elements
    * Element Shaping Factory: shapes elements into basic spells
    * Spell Fusion Factory: combines multiple spells into a new spell, with three levels:
      * Basic: fuses spells of the same element
      * Intermediate: fuses spells of different but non-opposing elements
      * Advanced: fuses spells of opposing elements
3. Finished cards are transferred into the player’s inventory.
4. A statistics panel displays the current element production rate, consumption rate, and spell production rate.
5. The player can switch to the battle scene at any time.
### Battle Scene
1. Enemies begin in a dormant state and awaken when they take damage.
2. Once awakened, enemies continuously act according to a fixed pattern, including attacking, healing, and defending.
3. The player can use cards by dragging them onto a target. Card effects include attack, healing, and defense.
4. The player can also enable an auto-cast mode, which automatically plays cards from the inventory.
### Resource Rules
1. **Spirit cards**: elemental cards that generate elements at a certain rate.
  * Basic elements: Water ↔ Fire, Wind ↔ Earth
  * Secondary elements: Ice (Wind + Water) ↔ Thunder (Wind + Fire), Light (Earth + Fire) ↔ Dark (Earth + Water)
2. **Factory cards**:
  * Element Fusion Factory: converts two different, non-opposing basic elements into one secondary element (input: 2, output: 1)
  * Element Shaping Factory: shapes one element into one basic spell (input: 1, output: 1)
  * Spell Fusion Factory: combines multiple spells into a new spell, with three levels:
    * Basic: fuses basic spells of the same element, for example Basic Fire Spell + Basic Fire Spell
    * Intermediate: fuses basic spells of different but non-opposing elements, for example Basic Fire Spell + Basic Thunder Spell
    * Advanced: fuses spells of opposing elements, for example Intermediate Fire Spell + Intermediate Water Spell
### Card Rules

Each **spell card** includes the following attributes:

1. Spell name
2. Spell rarity (calculated by weight)
3. Card artwork
4. Elemental attribute (elements with an advantage relationship deal bonus damage)
5. Spell effect:
  * Attack (damage + hit count + debuff)
  * Healing (healing amount + hit count + buff)
  * Defense (flat defense + hit count + percentage damage reduction)
6. Spell description
### Boss Rules

Bosses appear randomly in the game world. They are corrupted spirits, each belonging to one of the eight elemental attributes, which also follow elemental advantage relationships.
Bosses start in a dormant state, but in the **battle scene** the player can view their:

1. Health
2. Attribute
3. **Action pattern**

Once awakened by an attack, a boss will continue following its fixed **action pattern** of attacking, healing, and defending until its health is depleted.

### Reward Rules
1. After defeating a boss, the player receives a new **spirit node** that matches the boss’s attribute.
2. The player’s health is fully restored, and they level up to gain stat bonuses.
3. The player gets one chance to choose a **passive card**.
4. Before all factories are unlocked, one new factory is unlocked in sequence after each victory.
### Win/Loss Feedback Rules
1. **Victory**: the player gains rewards and unlocks a new factory.
2. **Defeat**: the game enters a result screen showing statistics such as the number of spells produced, total damage dealt, total healing done, and total playtime.