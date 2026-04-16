# Workshop Slice Code Review (Unity 6)

Date: 2026-04-16  
Scope: `Assets/ArcaneAtelier/Workshop/*`

## Executive summary

The slice is architecturally solid for a vertical slice: clear simulation ownership, clean data-driven content pipeline, and explicit battle handoff boundaries.

Primary hardening needed for reliability in broader team usage was in scene bootstrap assumptions and simulation catch-up safeguards. Those items are now addressed.

## Review findings

### 1) Scene controller dependency safety

**Issue:** `WorkshopSceneController` assumed `WorkshopContentDatabase` was always assigned, causing null-reference risk if scene setup drifted.

**Impact:** Scene could fail during `Awake`, especially in hand-authored or partially migrated scenes.

**Resolution:** Added explicit null guard + error logging + component disable fail-fast path.

---

### 2) Frame spike simulation catch-up risk

**Issue:** Update loop could iterate unbounded if frame hitch accumulated large simulation debt.

**Impact:** Potential long-frame stalls and poor editor playmode responsiveness.

**Resolution:** Added bounded catch-up loop (`MaxSimulationCatchUpStepsPerFrame`) and debt clamping to keep runtime responsive.

---

### 3) Component composition drift risk

**Issue:** Grid/HUD dependencies were optional at runtime but effectively required for intended operation.

**Impact:** Team members could create invalid scene setups unintentionally.

**Resolution:** Added `[RequireComponent]` and `[DisallowMultipleComponent]` on controller to enforce expected composition.

---

### 4) Locked node selection UX consistency

**Issue:** Locked nodes could be selected programmatically, creating inconsistent palette behavior.

**Impact:** Confusing status and avoidable placement attempts.

**Resolution:** `SetPaletteNode` now rejects locked selections with explicit status messaging.

---

## Follow-up implementation (this pass)

The highest-impact stability items from the recommendations are now implemented:

1. Added profiling markers around simulation step, transfer pass, and recipe execution to improve runtime observability.
2. Added content validation APIs on `WorkshopContentDatabase` and wired bootstrap-time validation to fail generation if invalid data is produced.
3. Reduced simulation-frame allocations by replacing hot-path LINQ sorts with cached list sorting and transfer buffers.

## Remaining roadmap items

1. Add Unity PlayMode tests for transfer and recipe edge cases.
2. Add serialization checkpoints for workshop save/load.
3. Move IMGUI debug HUD to retained mode UI for shipping path.
