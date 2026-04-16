# Arcane Atelier — Workshop Slice

Unity 6 implementation of the **Factory Scene** gameplay loop for `Arcane Atelier`.

This repository currently contains the **workshop / factory scene**, not the full game.
It is the part where players build a production line that turns elemental resources into spell cards for later combat.

The **authoritative gameplay content** for this slice is the generated workshop database and generated scene created by `WorkshopProjectBootstrap`.
If any runtime fallback content differs, treat the generated scene/database as correct.

## Unity compatibility

- **Target editor**: Unity `6000.4.0f1` (Unity 6 LTS).
- The project is assembly-definition based and organized into runtime/editor modules under `Assets/ArcaneAtelier/Workshop`.

## Quick start

1. Open this repository root as a Unity project.
2. Let the initial import complete.
3. The workshop bootstrap (`WorkshopProjectBootstrap`) auto-generates required data + scene if missing:
   - `Assets/ArcaneAtelier/Workshop/Generated/*`
   - `Assets/Scenes/SpellAssemblyScene.unity`
4. Open and run:
   - `Assets/Scenes/SpellAssemblyScene.unity`
5. If the scene/content looks stale, run:
   - `Arcane Atelier -> Workshop -> Rebuild Spell Assembly Content`

The generated starter scene already demonstrates two working production lines:

- `Fire Spirit -> Arcane Conduit -> Element Shaper -> Cinder Dart`
- `Water + Wind -> Element Fusion -> Element Shaper -> Frost Pin`

## What the player does

Plain-language version:

1. Place spirit nodes on the grid.
2. Spirit nodes produce elements over time.
3. Route those elements into factories.
4. Factories convert elements into spells.
5. Higher-tier factories fuse weaker spells into stronger ones.
6. Finished spell cards collect into the factory inventory.
7. The player commits that inventory as the battle deck payload.

If you want the full player-facing explanation, read:

- [Factory Scene Guide](Documentation/FactoryHowToPlay.md)

## Current gameplay scope (Factory Scene)

Implemented in this slice:

- Fixed-size grid-based node placement and rotation.
- Spirit nodes that generate basic elements (Fire/Water/Wind/Earth).
- Reward-unlocked secondary spirit nodes (Ice/Thunder/Light/Dark).
- Element Fusion Factory for secondary elements (Ice/Thunder/Light/Dark).
- Element Shaping Factory and 3-tier Spell Fusion factories.
- Continuous spell-card preparation and battle payload export.
- Spell cards with names, role, tier, rarity band, element tag, and effect metadata.
- Factory throughput panel (element production/consumption + spell output rates).
- Time pause/resume support for shared scene-time control.

Not implemented in this slice:

- Battle scene gameplay
- Boss logic
- Drag-and-drop card casting
- Post-battle level-up / heal / passive selection loop

So this slice matches the **factory half** of the design loop, not the full run loop yet.

## Teammate note

This slice is intended to be easy to integrate, but some content-facing parts may still change later:

- spell names
- effect values
- rarity weights
- UI layout
- reward tuning

The stable assumption teammates should use is:

- the workshop scene is the producer of battle cards
- combat should consume the payload from the workshop bridge
- factory content flows from spirit -> element -> spell -> fused spell
- the generated workshop database / generated scene are the current source of truth

## Documentation

- [Factory Scene Guide](Documentation/FactoryHowToPlay.md)
- [Spell Assembly Module](Documentation/SpellAssemblyModule.md)
- [Factory Architecture](Documentation/FactoryArchitecture.md)
- [Workshop ↔ Battle Contract](Documentation/WorkshopBattleContract.md)
