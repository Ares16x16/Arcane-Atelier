# Asset and Audio Production Checklist

This checklist covers all visual and audio assets needed for the current workshop and battle scope of Arcane Atelier.

Items are grouped by area so each person can own a section.
Check off items as they are delivered and placed in the project.

---

## Visual Assets

### Element Icons

One icon per element.
Used in node tooltips, spell card frames, HUD displays, and the boon drawer.

- [ ] Fire icon
- [ ] Water icon
- [ ] Wind icon
- [ ] Earth icon
- [ ] Ice icon
- [ ] Thunder icon
- [ ] Light icon
- [ ] Dark icon

---

### Spirit Node Sprites

One sprite per spirit type.
Displayed on the grid when a spirit node is placed.

- [ ] Fire Spirit
- [ ] Water Spirit
- [ ] Wind Spirit
- [ ] Earth Spirit
- [ ] Ice Spirit *(reward-unlocked)*
- [ ] Thunder Spirit *(reward-unlocked)*
- [ ] Light Spirit *(reward-unlocked)*
- [ ] Dark Spirit *(reward-unlocked)*

---

### Factory Node Sprites

One sprite per factory type.
Displayed on the grid when a factory node is placed.

- [ ] Arcane Conduit
- [ ] Element Fusion
- [ ] Element Shaper
- [ ] Spell Fusion I *(reward-unlocked)*
- [ ] Spell Fusion II *(reward-unlocked)*
- [ ] Spell Fusion III *(reward-unlocked)*

---

### Spell Card Artwork

One art piece per spell card.
Art fills the card frame in the hand, inventory, and inspect views.

#### Basic Tier

- [ ] Cinder Dart *(Fire / Attack)*
- [ ] Tidal Mend *(Water / Healing)*
- [ ] Zephyr Cut *(Wind / Attack)*
- [ ] Stoneguard Sigil *(Earth / Defense)*
- [ ] Frost Pin *(Ice / Attack)*
- [ ] Volt Javelin *(Thunder / Attack)*
- [ ] Lumen Prayer *(Light / Healing)*
- [ ] Gloam Ward *(Dark / Defense)*

#### Intermediate Tier

- [ ] Inferno Brand *(Fire / Attack)*
- [ ] Tide Chorus *(Water / Healing)*
- [ ] Razor Monsoon *(Wind / Attack)*
- [ ] Bastion Pulse *(Earth / Defense)*
- [ ] Glacier Bind *(Ice / Attack)*
- [ ] Stormbreaker *(Thunder / Attack)*
- [ ] Dawn Benediction *(Light / Healing)*
- [ ] Umbral Bastion *(Dark / Defense)*

#### Advanced Tier

- [ ] Eclipse Covenant *(Light-Dark / Healing)*
- [ ] Worldsplit Tempest *(Wind-Earth / Attack)*
- [ ] Steam Requiem *(Fire-Water / Attack)*
- [ ] Absolute Zero Surge *(Ice-Thunder / Defense)*

---

### UI Elements

Shared widgets and panels used across the workshop and battle scenes.

#### Workshop Scene

- [ ] Grid cell background tile
- [ ] Grid border / frame
- [ ] Node placement ghost overlay
- [ ] Node selection highlight
- [ ] Palette panel background
- [ ] Palette item slot frame
- [ ] HUD throughput panel background
- [ ] Boon drawer background
- [ ] Boon item slot frame
- [ ] Commit payload button
- [ ] Pause / resume button
- [ ] Control guide overlay background

#### Battle Scene

- [ ] Battle background / environment art
- [ ] Player health bar
- [ ] Enemy health bar
- [ ] Shield / guard indicator
- [ ] Hand dock panel background
- [ ] Card frame (Common rarity)
- [ ] Card frame (Rare rarity)
- [ ] Card frame (Epic rarity)
- [ ] Card frame (Legendary rarity)
- [ ] Deck counter icon
- [ ] Spent pile icon
- [ ] End Turn button
- [ ] Enemy portrait placeholder (pending enemy art below)
- [ ] Win / loss summary panel background

---

### Enemy Art

One portrait or sprite per enemy type currently in the battle encounter sequence.

- [ ] Ember Wisp
- [ ] Hollow Cleric
- [ ] Glass Knight
- [ ] Corrupted Earth Golem *(final boss)*

---

### VFX / Animations

Visual feedback for simulation events.

- [ ] Element token moving along conduit
- [ ] Spirit node production pulse (per element color)
- [ ] Factory processing animation (spinning / glowing)
- [ ] Spell card output pop
- [ ] Node placement flash
- [ ] Node removal dissolve
- [ ] Spell cast hit flash (per element color)
- [ ] Shield absorb flash
- [ ] Heal restore pulse
- [ ] Enemy defeat animation

---

## Audio Assets

### Music

- [ ] Workshop scene loop *(factory ambience, medium energy)*
- [ ] Battle scene loop *(tense, loopable)*
- [ ] Main menu theme *(if a menu scene is added)*
- [ ] Victory fanfare / sting
- [ ] Defeat sting

---

### Sound Effects

#### Workshop / Factory

- [ ] Node placement click
- [ ] Node rotation click
- [ ] Node removal pop
- [ ] Element production tick (shared or per-element variant)
- [ ] Spell card output chime (Basic tier)
- [ ] Spell card output chime (Intermediate tier)
- [ ] Spell card output chime (Advanced tier)
- [ ] Payload commit confirmation sound

#### UI

- [ ] Button click
- [ ] Button hover
- [ ] Boon drawer open
- [ ] Boon drawer close
- [ ] Unlock notification sound
- [ ] Error / invalid placement buzz

#### Battle

- [ ] Card draw
- [ ] Card play whoosh
- [ ] Attack hit (generic)
- [ ] Fire spell hit
- [ ] Water spell hit
- [ ] Wind spell hit
- [ ] Earth spell hit
- [ ] Ice spell hit
- [ ] Thunder spell hit
- [ ] Light spell hit
- [ ] Dark spell hit
- [ ] Healing restore sound
- [ ] Shield block sound
- [ ] Player hurt sound
- [ ] Enemy hurt sound
- [ ] Enemy defeat sound
- [ ] Player defeat sound
- [ ] End turn confirm sound

---

## Notes

- **Rarity frame color cues**: Common = grey tint, Rare = blue tint, Epic = purple tint, Legendary = gold tint.
  Tint values are already in `WorkshopItemDefinition.cs` — art frames only need to respect these per-element accent colors.
- **Placeholder tints** are defined in `WorkshopProjectBootstrap.cs` and can serve as color guides for icon and sprite work.
- **Node art size**: the current grid cell size is defined at runtime scale. Sprites should be square and power-of-two (e.g., 256 × 256) so Unity can trim and pack them efficiently.
- **Audio format**: import all music as `.ogg` (loopable) and all SFX as `.wav` or `.ogg` (short, no loop).
  Set Compression Format to Vorbis for music and ADPCM for short SFX to keep build size down.
