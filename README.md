# Arcane Atelier

Unity 6 prototype and design documentation for `Arcane Atelier`, a fantasy workshop-building roguelike where the player manufactures spell cards before each battle.

This branch still contains the workshop-heavy implementation baseline, while the full-run design now lives in the documentation set under `Documentation/`.

## Engine compatibility

- **Original target editor**: Unity `6000.4.0f1` (Unity 6 LTS).
- Runtime/editor C# source has been written with older C# compiler compatibility in mind (no target-typed `new` expressions), which improves portability to Unity forks based on older compiler stacks (including Tuanjie-derived workflows).
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

The generated starter scene still demonstrates the current workshop lines:

- `Fire Spirit -> Arcane Conduit -> Element Shaper -> Cinder Dart`
- `Water + Wind -> Element Fusion -> Element Shaper -> Frost Pin`

## Design documentation

Full-game design set:

- [Game Design Document](Documentation/GameDesignDocument.md)
- [Game Flow And Scene Guide](Documentation/GameFlowAndSceneGuide.md)
- [Workshop Preparation Design](Documentation/WorkshopPreparationDesign.md)
- [Combat, Rewards, And Meta Progression](Documentation/CombatRewardsAndMeta.md)
- [Narrative And World Guide](Documentation/NarrativeAndWorld.md)
- [Implementation Reference](Documentation/ImplementationReference.md)
- [Content Table Guidance](Documentation/contenttable/ContentTableGuidance.md)

Module-level technical docs:

- [Factory Scene Guide](Documentation/FactoryHowToPlay.md)
- [Spell Assembly Module](Documentation/SpellAssemblyModule.md)
- [Factory Architecture](Documentation/FactoryArchitecture.md)
- [Workshop ↔ Battle Contract](Documentation/WorkshopBattleContract.md)

## Current implementation scope

Implemented in the current branch:

- Workshop grid placement and rotation
- Spirit nodes, conduits, and factory nodes
- Element fusion, element shaping, and spell fusion
- Prepared card inventory and battle payload export
- Factory throughput telemetry and pause/resume support

Referenced by the new design docs, but sourced from the `integration` branch:

- `MainMenuScene`
- `WorkshopScene`
- `BattleScene`
- menu -> workshop -> battle return flow

- [Factory Scene Guide](Documentation/FactoryHowToPlay.md)
- [Spell Assembly Module](Documentation/SpellAssemblyModule.md)
- [Factory Architecture](Documentation/FactoryArchitecture.md)
- [Workshop ↔ Battle Contract](Documentation/WorkshopBattleContract.md)
- [Asset and Audio Production Checklist](Documentation/AssetAndAudioProductionChecklist.md)
