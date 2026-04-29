# Arcane Atelier - Implementation Reference

**Document Type:** Branch-to-design mapping guide  
**Audience:** Engineering, Technical Design, Production, QA  
**Status:** Active reference for the rewritten design documentation

---

## 1. Purpose

This document explains where the new design set came from and how it maps to existing repo artifacts.

It exists because the design rewrite intentionally consolidates three previously separate sources:

- workshop technical docs already on the current branch
- `textfile` branch gameplay/content notes
- `integration` branch scene-flow references

---

## 2. Key source references

### 2.1 Current branch workshop references

These files define the current implemented workshop model:

- [Factory Scene Guide](FactoryHowToPlay.md)
- [Spell Assembly Module](SpellAssemblyModule.md)
- [Factory Architecture](FactoryArchitecture.md)
- [Workshop ↔ Battle Contract](WorkshopBattleContract.md)

These are the source references for:

- spirit -> element -> spell -> fusion ladder
- current node roster
- prepared card payload ownership
- workshop simulation boundaries

### 2.2 `origin/textfile` references

These branch files were the seed for the broader game-facing documentation:

- `origin/textfile:Documentation/GameHowToPlay.md`
- `origin/textfile:Documentation/contenttable/ContentTableGuidance.md`

They provided:

- the original full-loop intent
- early boss and reward language
- initial content-table concept

The new doc set expands and replaces those fragments with a production-ready structure.

### 2.3 `origin/integration` references

These branch files define the current scene naming and return-flow assumptions:

- `origin/integration:Assets/ArcaneAtelier/MainMenu/MainMenuManager.cs`
- `origin/integration:Assets/ArcaneAtelier/Integration/Runtime/GameFlowRuntime.cs`
- `origin/integration:Assets/ArcaneAtelier/Battle/Runtime/BattleSceneController.cs`
- `origin/integration:Documentation/BattleArchitecture.md`
- `origin/integration:Documentation/BattleWorkshopDependencies.md`

They are the direct reference for:

- `MainMenuScene`
- `WorkshopScene`
- `BattleScene`
- start-run bridge clearing
- battle return and reward application direction

---

## 3. Design decisions that intentionally preserve current work

### 3.1 Scene count

The new flow keeps the current three-scene shape instead of proposing a large scene rewrite.

Reason:

- it fits the integration branch
- it reduces engineering cost
- route board, reward screen, and interludes can live inside `WorkshopScene`

### 3.2 Boss pacing

The rewritten flow treats bosses as progression-triggered encounters instead of selectable route nodes.

Reason:

- it creates stronger act pacing
- it keeps route choice focused on normal risk/reward planning
- it matches the requested rule that bosses appear after a certain number of battles

### 3.3 Workshop-to-battle contract

The new design keeps the workshop as the sole producer of the battle deck snapshot.

Reason:

- it matches [Workshop ↔ Battle Contract](WorkshopBattleContract.md)
- it avoids coupling battle code to workshop scene objects

### 3.4 Factory roster

The new workshop design starts from the existing implemented nodes and only proposes future expansion as a second wave.

Reason:

- the current prototype already teaches a clean production ladder
- it lets the team polish what exists before adding a larger content explosion

---

## 4. Recommended reading order by discipline

### 4.1 Producers and leads

Read in this order:

1. [Game Design Document](GameDesignDocument.md)
2. [Game Flow And Scene Guide](GameFlowAndSceneGuide.md)
3. [Implementation Reference](ImplementationReference.md)

### 4.2 Gameplay engineers

Read in this order:

1. [Game Flow And Scene Guide](GameFlowAndSceneGuide.md)
2. [Workshop Preparation Design](WorkshopPreparationDesign.md)
3. [Combat, Rewards, And Meta Progression](CombatRewardsAndMeta.md)
4. [Workshop ↔ Battle Contract](WorkshopBattleContract.md)

### 4.3 Narrative, UI, and art

Read in this order:

1. [Game Design Document](GameDesignDocument.md)
2. [Narrative And World Guide](NarrativeAndWorld.md)
3. [Game Flow And Scene Guide](GameFlowAndSceneGuide.md)

---

## 5. Suggested ownership split

| Discipline | Primary ownership |
|------------|-------------------|
| Gameplay Design | node progression, route pacing, reward pools |
| Workshop Engineering | prep timer, node logic, payload commit, workshop UI states |
| Battle Engineering | card resolution, enemy intent, result generation |
| Integration | state handoff, route board, reward state, save/load |
| Narrative | world framing, encounter naming, boss identity |
| QA | flow transitions, reward duplication, empty payload edge cases |

---

## 6. New documents added by this rewrite

- [Game Design Document](GameDesignDocument.md)
- [Game Flow And Scene Guide](GameFlowAndSceneGuide.md)
- [Workshop Preparation Design](WorkshopPreparationDesign.md)
- [Combat, Rewards, And Meta Progression](CombatRewardsAndMeta.md)
- [Narrative And World Guide](NarrativeAndWorld.md)
- [Content Table Guidance](contenttable/ContentTableGuidance.md)

These docs should now be treated as the game-level source of truth.

---

## 7. What still remains technical-source-of-truth

Even after the design rewrite, these remain the technical references for current implementation:

- scene bootstrap behavior in the relevant branch code
- card payload fields in [Workshop ↔ Battle Contract](WorkshopBattleContract.md)
- current workshop module behavior in the runtime and editor code under `Assets/ArcaneAtelier/Workshop`

If a future implementation differs from the design docs, the team should update the design docs instead of allowing silent drift.
