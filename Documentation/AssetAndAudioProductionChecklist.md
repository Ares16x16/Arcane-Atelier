# Arcane Atelier - Asset And Audio Production Checklist

**Document Type:** Production checklist  
**Audience:** Art, UI/UX, Audio, Design, Production  
**Status:** Current vertical-slice backlog plus near-term full-game prep

---

## 1. Purpose

This document lists the visual and audio assets the team needs to produce for `Arcane Atelier`.

It is split into:

- `P0`: required to make the current `MainMenuScene -> WorkshopScene -> BattleScene` run feel intentional and shippable as a vertical slice
- `P1`: next-wave assets that deepen the same slice without changing its core loop
- `P2`: full-game support assets for later acts, events, codex, and long-term polish

The target tone should stay consistent across all departments:

- mystical workshop fantasy
- elegant but pressured siege atmosphere
- arcane machinery that feels handcrafted rather than industrial
- readable, game-first presentation over painterly clutter

Visual direction keywords:

- candlelit brass
- etched sigils
- dark slate stone
- glowing glass
- restrained gold accents
- elemental color coding
- celestial danger in the distance

Audio direction keywords:

- ritual chamber
- tense preparation
- magical pressure
- restrained heroism
- brittle glass and rune-metal textures
- elemental identity that is easy to read in a fight

---

## 2. Gameplay Effect Inventory

These are the effects the current game either already uses, names in data, or clearly needs next. Each entry lists the production need, not just the design name.

| Effect | Current status | Visual direction | Audio direction | Notes |
|------|------|------|------|------|
| Damage | Live | sharp rune slash, impact spark, brief hit flash on enemy | dry impact with magical crack | Must read instantly with no confusion |
| Heal | Live | upward motes, soft circular sigil, health refill shimmer | glassy chime with warm sustain | Should feel restorative, not explosive |
| Shield | Live | hex or barrier outline, layered plate flash, small ward pulse | low metallic hum with protective swell | Needs both gain feedback and shield-break feedback |
| Draw | Live | card slide from deck to hand, faint trail | soft paper-slide plus rune flick | Important because card flow is a core loop |
| Reshuffle | Live | deck pulse, spent-pile swirl into deck | brief shuffle whisper with magical whoosh | Make the spent pile mechanic obvious |
| Enemy Intent | Live | icon pulse above enemy, subtle forecast glow | none or very subtle tick | Readability first, no noisy looping SFX |
| Burn | Data only | ember scorch decal, orange crackle, lingering heat | ember hiss, small flame lick | Fire identity keyword |
| Regen | Data only | repeated green-blue pulse, droplets or woven threads | soft layered heartbeat and water tone | Healing over time visual |
| Expose | Data only | fractured target reticle, gust peel-away | airy slice with brittle crack | Wind offensive utility |
| Bulwark | Data only | thicker earth ward plates, grounded sigil ring | heavy stone set, low thud | Earth defensive keyword |
| Slow | Data only | icy drag trails, movement smear reduction | cold scrape, slowed crystal tick | Ice control keyword |
| Shock | Data only | forked arcs, flash-pop, charged outline | electric snap, short buzz | Thunder burst utility |
| Bless | Data only | pale gold halo, rune petals | clean choir-like ping | Light support keyword |
| Veil | Data only | dark mist wrap, crescent shadow ring | muffled breath, low veil sweep | Dark defense identity |
| Ward | Data only | refined shield variant, rune lattice | stronger barrier hum | Separate from raw shield if implemented |
| Freeze | Data only | fast crystal lock, frost shell | cold lock click | Stronger hard-control version of Slow |
| Stun | Data only | short starburst, arc overload | crack-pop, abrupt silence | Should not be confused with Shock |
| Radiance | Data only | broad light bloom, layered sigil fan | bright harmonic swell | Advanced light payoff |
| Shade | Data only | deep violet veil, silhouette blur | low hush, shadow inhale | Advanced dark protection |
| Rend | Data only | tearing line effect, fractured armor glyph | ripping magical shear | Advanced wind-earth offense |
| Scald | Data only | steam burst, wet flame flash | steam burst with burn hiss | Advanced fire-water offense |
| Static Shell | Data only | ice shell with crackling arcs | frozen hum with electric edge | Advanced ice-thunder defense |

Production note:

- `P0` only needs polished assets for `Damage`, `Heal`, `Shield`, `Draw`, `Reshuffle`, and `Enemy Intent`.
- The keyword list above should still be designed now so later implementations do not look disconnected.

---

## 3. Core Resource And Element Assets

### 3.1 Element resources

These appear in workshop production, inventory chips, card identity, and eventually FX and encounter previews.

| Resource | Tone | Needed assets |
|------|------|------|
| Fire | volatile, bright, forge-born | icon, chip, particle burst, source-node glow |
| Water | restorative, fluid, lucid | icon, chip, ripple burst, source-node glow |
| Wind | cutting, nimble, airy | icon, chip, streak burst, source-node glow |
| Earth | stable, protective, heavy | icon, chip, dust burst, source-node glow |
| Ice | precise, cold, locking | icon, chip, frost burst, source-node glow |
| Thunder | sudden, charged, aggressive | icon, chip, spark burst, source-node glow |
| Light | sacred, hopeful, cleansing | icon, chip, gold-white bloom, source-node glow |
| Dark | hidden, quiet, sheltering | icon, chip, violet haze, source-node glow |

`P0 required`:

- one clean icon per element
- one circular chip/background treatment per element
- one simple particle palette per element

---

## 4. Spell Card Asset List

The workshop data currently defines `20` spells. Each spell needs at minimum a card illustration concept, element color treatment, iconography support, and matching VFX language.

### 4.1 Basic spells

| Spell | Role | Tone | Art direction |
|------|------|------|------|
| Cinder Dart | Attack | quick, pointed, starter fire projectile | narrow ember lance, forge spark tail |
| Tidal Mend | Healing | calm, practical, early sustain | curved water ribbon around a rune seal |
| Zephyr Cut | Attack | fast, multi-hit, airy | thin pale blades and spiral motion |
| Stoneguard Sigil | Defense | grounded, reliable, warding | carved stone disk with glowing seams |
| Frost Pin | Attack | cold, precise, slowing | crystalline spike with icy vapor |
| Volt Javelin | Attack | direct, explosive, charged | bright spear of lightning with impact burst |
| Lumen Prayer | Healing | sacred, hopeful, radiant | suspended prayer sigil with warm halo |
| Gloam Ward | Defense | hidden, sheltering, obscure | crescent shield wrapped in violet mist |

### 4.2 Intermediate spells

| Spell | Role | Tone | Art direction |
|------|------|------|------|
| Inferno Brand | Attack | branded, hotter, more forceful fire | stamped sigil over roaring ember slash |
| Tide Chorus | Healing | rolling sustain, layered recovery | overlapping wave rings, harmonic rune pattern |
| Razor Monsoon | Attack | many cuts, storm tempo | storm spiral with multiple white-blue slices |
| Bastion Pulse | Defense | reinforced, fortified, controlled | earth shield plates radiating outward |
| Glacier Bind | Attack | locking, heavy cold pressure | chained ice geometry around target point |
| Stormbreaker | Attack | thunder detonation, heavier than Volt Javelin | hammering lightning column with fracture arcs |
| Dawn Benediction | Healing | sweeping blessing, larger restoration | sunrise fan, layered sainted sigils |
| Umbral Bastion | Defense | shadow turned solid | black-violet fortress silhouette with inward glow |

### 4.3 Advanced spells

| Spell | Role | Tone | Art direction |
|------|------|------|------|
| Eclipse Covenant | Healing | majestic, rare, stabilizing | eclipse ring with light-dark harmony ribbons |
| Worldsplit Tempest | Attack | violent, act-ending offense | storm fracture splitting a battlefield plane |
| Steam Requiem | Attack | pressurized, scorching, unstable | white-hot steam bloom with ember core |
| Absolute Zero Surge | Defense | severe, crystalline lockdown | radial frozen shell with trapped lightning veins |

### 4.4 Card production needs

`P0 required`:

- one shared card frame system
- one placeholder illustration style for all `20` cards
- role icon set: attack, heal, defense
- tier markers: basic, intermediate, advanced
- energy-cost badge
- hover/highlight state
- disabled/unplayable state

`P1`:

- unique illustration or badge motif per card
- foil or glow treatment for advanced spells
- animated card entry and play transition

---

## 5. Workshop Machine And Node Asset List

The workshop currently uses `14` nodes. These need consistent silhouettes, icons, placement ghosts, and production feedback.

### 5.1 Spirit source nodes

| Node | Tone | Asset need |
|------|------|------|
| Fire Spirit | restless forge ember | spirit core, idle flicker, fire output pulse |
| Water Spirit | floating ceremonial spring | spirit core, droplet loop, water output pulse |
| Wind Spirit | light, circling, unstable | spirit core, wind ribbons, output streak |
| Earth Spirit | heavy, anchored, old | spirit core, dust pulse, stone output marker |
| Ice Spirit | lucid frozen relic | cold source variant with frost halo |
| Thunder Spirit | charged storm reliquary | crackling source variant |
| Light Spirit | sanctified lantern-spirit | holy source variant with gold-white aura |
| Dark Spirit | hidden eclipse familiar | shadow source variant with dim violet aura |

### 5.2 Machine nodes

| Node | Tone | Asset need |
|------|------|------|
| Arcane Conduit | functional, humble, readable | pipe/relay tile, directional arrows, active transfer glow |
| Element Fusion | ritual apparatus, mixing chamber | dual-input machine with fusion flash |
| Element Shaper | engraving and shaping station | card-forming machine, rune press animation |
| Spell Fusion I | first true spell forge | dual-card merge machine, stronger forge pulse |
| Spell Fusion II | mixed-identity forge | more complex frame, crossed elemental channels |
| Spell Fusion III | apex fusion altar | act-ending forge silhouette, ceremonial rarity |

### 5.3 Workshop node production needs

`P0 required`:

- top-down icon/silhouette for all `14` nodes
- 4-direction port readability
- selected tile highlight
- placement ghost
- invalid placement highlight
- active transfer pulse
- produced-item popout marker

`P1`:

- machine-specific idle animation
- per-node crafting animation
- damaged/corrupted variant for hazards

---

## 6. Enemy And Encounter Asset List

The current battle loop uses `4` encounter enemies.

| Enemy | Role in run | Tone | Needed assets |
|------|------|------|------|
| Ember Wisp | early pressure scout | small, fast, irritating, fiery | portrait, idle loop, attack flash, defend flash, hit flash, defeat burst |
| Hollow Cleric | sustain check | mournful, ritualistic, watery, unclean | portrait, idle loop, heal cast, ward cast, attack cast, hit flash, defeat burst |
| Glass Knight | shielded duelist | elegant, brittle, disciplined | portrait, idle loop, guard stance, thrust, cleave, shield shimmer, defeat shatter |
| Corrupted Earth Golem | final boss | monumental, old, cracked, siege-scale | portrait, idle weight loop, slam, guard, heal/renewal pulse, hit flash, collapse |

### 6.1 Enemy intent icon needs

`P0 required`:

- attack intent icon
- defend intent icon
- heal intent icon
- value badge styling

`P1`:

- enemy-family-specific intent art variants
- boss phase marker iconography

---

## 7. UI Art Backlog

### 7.1 Main menu

`P0 required`:

- title logotype for `Arcane Atelier`
- background key art or layered matte
- new run button style
- continue button style
- settings button style
- credits button style

Tone:

- solemn magical workshop at night
- not cheerful mobile-fantasy
- should imply siege readiness and craft mastery

### 7.2 Workshop HUD

Current workshop already has a strong layout language. It needs final art support for:

- panel frame kit with dark body and colored accent top bar
- stat tiles
- reward drawer cards
- blueprint cards
- hover tooltip styling
- guide overlay styling
- button states: normal, hover, pressed, disabled

Tone:

- practical master-crafter workstation
- readable first, ornate second

### 7.3 Battle HUD

`P0 required`:

- matching panel frame kit shared with workshop
- boss health panel
- player health and energy panel
- encounter status panel
- bottom hand dock
- end-turn button style
- deck counter icon
- spent-pile icon
- intent display badge

Tone:

- same atelier UI language carried into combat
- still magical, but more focused and urgent

### 7.4 Post-battle and run summary UI

`P0 required`:

- victory panel
- defeat panel
- reward card presentation
- final run summary window
- return-to-menu button style

Tone:

- victory should feel like a breach sealed
- defeat should feel tragic but instructive

### 7.5 Route board and future overlays

`P1`:

- route board node icons
- route connectors
- act progression meter
- reward selection overlay
- boss warning overlay
- codex layout
- settings overlay

Tone:

- astral defense map
- breach-front cartography

---

## 8. VFX Backlog

### 8.1 Workshop VFX

`P0 required`:

- spirit emission pulse
- conduit transfer streak
- fusion combine flash
- shaper forge spark
- node-selected glow
- node-placed confirmation burst
- deploy-to-battle transition flash

### 8.2 Battle VFX

`P0 required`:

- card play flare
- damage hit spark
- heal burst
- shield gain flare
- shield break flash
- enemy intent pulse
- death burst per enemy family

### 8.3 Menu and transition VFX

`P1`:

- main menu ambient particles
- workshop load-in reveal
- boss intro breach crack
- run-complete celestial seal effect

---

## 9. Animation Backlog

### 9.1 Card animation

`P0 required`:

- hand draw-in
- hover raise
- play-to-target motion
- discard/spent exit
- reshuffle deck pulse

### 9.2 Enemy animation

`P0 required`:

- idle
- attack
- defend
- heal if applicable
- hit react
- death

### 9.3 Workshop machine animation

`P1`:

- spirit idle breathing
- conduit pulse
- machine active loop
- craft completion pulse
- unlock animation

---

## 10. Audio Backlog

### 10.1 Music

| Track | Priority | Tone | Use |
|------|------|------|------|
| Main Menu Theme | P0 | lonely, elegant, mystical, restrained | title and menu |
| Workshop Theme | P0 | focused, ticking, arcane, constructive | preparation phase |
| Normal Battle Theme | P0 | tense, forward-driving, readable loop | first 3 encounters |
| Final Boss Theme | P0 | heavy, ancient, high-stakes, percussive | earth golem fight |
| Victory Stinger | P0 | short, sealing, triumphant but not cheesy | post-win |
| Defeat Stinger | P0 | hollow, downward, brief | post-loss |
| Run Complete Theme | P1 | relieved, luminous, earned | end summary |

### 10.2 Ambient layers

| Layer | Priority | Tone | Use |
|------|------|------|------|
| Workshop room tone | P0 | low rune hum, distant mechanisms, soft flame | workshop base loop |
| Battle ambient bed | P0 | windy breach, magical instability, cavernous air | battle base loop |
| Boss arena ambience | P1 | stone groan, deep resonance, seismic tension | final boss |

### 10.3 UI SFX

`P0 required`:

- button hover
- button click
- panel open
- panel close
- reward select
- invalid action
- deploy confirm

Tone:

- glass, etched metal, rune clicks
- avoid generic sci-fi bleeps

### 10.4 Workshop SFX

`P0 required`:

- spirit generation pulse
- conduit transfer tick
- fusion combine
- shaping forge
- machine placement
- machine rotate
- machine remove
- reward apply

Tone:

- magical craft bench
- subtle machinery without steampunk noise overload

### 10.5 Battle SFX

`P0 required`:

- card draw
- card hover
- card play
- attack hit
- heal cast
- shield gain
- shield break
- enemy attack
- enemy defend
- enemy heal
- enemy death
- reshuffle
- turn end

### 10.6 Element-specific one-shot libraries

`P1`:

- fire cast and impact
- water cast and heal
- wind cut and sweep
- earth guard and slam
- ice lock and shatter
- thunder arc and detonation
- light prayer and radiance
- dark veil and hush

---

## 11. Shared Iconography And Graphic Language

`P0 required`:

- attack icon
- heal icon
- shield icon
- deck icon
- spent-pile icon
- energy icon
- turn icon
- reward icon
- boss icon
- workshop node category icons

Tone:

- etched rune symbols
- high contrast
- readable at small sizes

---

## 12. Typography And Branding

`P0 required`:

- display font for title and major headers
- body font for readable gameplay UI
- number font treatment for HP, energy, deck counts, and damage values

Tone:

- display font should feel ceremonial and magical
- body font should stay clean and readable
- avoid modern tech or comic-fantasy styling

---

## 13. Recommended First Production Order

### 13.1 Art/UI order

1. Shared panel frame kit
2. Shared iconography set
3. Element icon and chip set
4. Card frame kit and placeholder spell illustrations
5. Enemy portraits and simple animation passes
6. Workshop node icons and production VFX
7. Menu and end-of-run presentation

### 13.2 Audio order

1. Button/UI kit
2. Core workshop loop SFX
3. Core battle loop SFX
4. Workshop music
5. Normal battle music
6. Boss music and stingers

---

## 14. Minimum P0 Delivery For The Current Slice

If the team needs the shortest useful asset scope, deliver this first:

- main menu background and title
- workshop panel kit
- battle panel kit matching workshop
- element icons for all `8` elements
- card frame kit for `3` tiers
- placeholder art for all `20` spells
- portraits and simple loops for `Ember Wisp`, `Hollow Cleric`, `Glass Knight`, and `Corrupted Earth Golem`
- core VFX for damage, heal, shield, draw, reshuffle, and intent
- workshop and normal battle music
- boss music
- core UI, workshop, and battle SFX set

That set is enough to replace the current placeholder feel and make the full workshop-to-battle-to-summary loop presentable.
