# Arcane Atelier — Workshop Slice

Unity 6 implementation of the **Factory Scene** gameplay loop for `Arcane Atelier`.

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

## Current gameplay scope (Factory Scene)

Implemented in this slice:

- Fixed-size grid-based node placement and rotation.
- Spirit nodes that generate basic elements (Fire/Water/Wind/Earth).
- Element Fusion Factory for secondary elements (Ice/Thunder/Light/Dark).
- Element Shaping Factory and 3-tier Spell Fusion factories.
- Continuous spell-card preparation and battle payload export.
- Factory throughput panel (element production/consumption + spell output rates).
- Time pause/resume support for shared scene-time control.

## Documentation

- [Spell Assembly Module](Documentation/SpellAssemblyModule.md)
- [Factory Architecture](Documentation/FactoryArchitecture.md)
- [Workshop ↔ Battle Contract](Documentation/WorkshopBattleContract.md)
