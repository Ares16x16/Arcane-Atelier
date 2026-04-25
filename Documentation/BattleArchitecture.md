# Battle System Architecture

> **Module**: ArcaneAtelier.Battle  
> **Status**: Phase 1 complete (skeleton + data layer). Core combat loop pending Phase 2.  
> **Owner**: Battle System Lead Programmer  
> **Last Updated**: 2026-04-22

---

## 1. Design Principles

The Battle module follows the same **feature-module separation** as Workshop:
- **Physical isolation**: `ArcaneAtelier/Battle/` is a sibling to `ArcaneAtelier/Workshop/`, not nested inside it.
- **Assembly boundaries**: Separate `Runtime` and `Editor` asmdef files.
- **One-way dependency**: `Battle.Runtime` references `Workshop.Runtime` to consume the cross-scene payload contract. Workshop has zero knowledge of Battle.
- **Data-driven**: All boss and card-effect behavior is authored via ScriptableObjects (`BattleBossDefinition`, `BattleCardEffectTemplate`).

---

## 2. Directory Structure

```
ArcaneAtelier/Battle/
├── Runtime/                           # ArcaneAtelier.Battle.Runtime
│   ├── ArcaneAtelier.Battle.Runtime.asmdef
│   ├── BattleEnums.cs                 # ActionType, ElementRelation, ResultType
│   ├── BattleElementUtility.cs        # Element advantage/disadvantage resolver
│   ├── BattleUnit.cs                  # Shared HP/shield/element model (player + boss)
│   ├── BattleBossAction.cs            # Serializable single-step in a boss pattern
│   ├── BattleBossDefinition.cs        # ScriptableObject: boss data + action pattern
│   ├── BattleCardEffectTemplate.cs    # ScriptableObject: Workshop payload → combat effect
│   ├── BattleContentDatabase.cs       # Registry + validation for all battle content
│   ├── BattleResult.cs                # Post-battle payload + static bridge
│   └── BattleSceneController.cs       # Scene entry: consumes Workshop payload, init units
├── Editor/                            # ArcaneAtelier.Battle.Editor
│   ├── ArcaneAtelier.Battle.Editor.asmdef
│   └── BattleContentBootstrapper.cs   # MenuItem to generate default SO assets
└── Content/                           # Generated ScriptableObject assets
    ├── BattleContentDatabase.asset
    ├── Boss_EarthGolem.asset
    ├── CardEffectTemplate_Attack.asset
    ├── CardEffectTemplate_Heal.asset
    └── CardEffectTemplate_Defend.asset
```

---

## 3. Layered Architecture

```
┌─────────────────────────────────────────────────────────────┐
│  Presentation (Phase 3)                                     │
│  BattleHudPresenter (IMGUI) — hand, HP, buffs, damage text  │
├─────────────────────────────────────────────────────────────┤
│  Orchestration (Phase 1 ✓)                                 │
│  BattleSceneController — scene lifecycle, payload handoff   │
├─────────────────────────────────────────────────────────────┤
│  Simulation (Phase 2 ✓)                                    │
│  BattleSimulation — turn state machine, win/loss detection  │
│  BattleDeckController — deck / hand / draw / discard        │
│  BattleBossAI — fixed-pattern cyclic execution              │
│  BattleActionResolver — damage formula + elemental mod      │
├─────────────────────────────────────────────────────────────┤
│  Data / Authoring (Phase 1 ✓)                              │
│  BattleBossDefinition, BattleCardEffectTemplate,            │
│  BattleContentDatabase, BattleUnit                          │
└─────────────────────────────────────────────────────────────┘
```

---

## 4. Core Systems

### 4.1 Elemental Advantage

**Rule** (from `A-GameRule.md`):
- Water ↔ Fire
- Wind ↔ Earth
- Ice ↔ Thunder
- Light ↔ Dark

**Implementation**: `BattleElementUtility.GetRelation(attacker, defender)`
- Returns `Advantage` / `Disadvantage` / `Neutral`
- `ApplyMultiplier(baseValue, relation)` applies ±25% modifier.

### 4.2 Boss Action Pattern

Bosses use a **fixed, cyclic action list** — no dynamic AI.

Each `BattleBossAction` specifies:
- `ActionType`: Attack / Heal / Defend / Special
- `Value`: numeric magnitude (damage, heal, or shield amount)
- `SecondaryValue`: percentage modifier (reserved for future use)
- `Description`: human-readable action name

Example — Earth Golem (4-step loop):
1. Attack 15 — "Slams the ground"
2. Defend 10 — "Hardens its shell"
3. Attack 20 — "Heavy strike"
4. Heal 10 — "Absorbs earth energy"

### 4.3 Card Effect Resolution

Battle does **not** duplicate card definitions. It resolves by `CardId`:

```
WorkshopBattleCardEntry (from payload)
        ↓
BattleCardEffectTemplate.FindTemplate(cardId)
        ↓
BattleResolvedEffect
        ↓
Applied to BattleUnit
```

`BattleResolvedEffect` carries:
- `Role`: Attack / Healing / Defense
- `PrimaryValue`: damage/heal/shield amount
- `HitCount`: number of repetitions
- `SecondaryValue`: percentage modifier
- `Element`: for elemental advantage calculation

### 4.4 Shared Unit Model

`BattleUnit` is used for **both player and boss**:
- `MaxHealth`, `CurrentHealth`, `Shield`
- `Element` (for advantage calculation)
- `IsAlive`
- `TakeDamage(int)` — shield absorbs first, then HP
- `Heal(int)` — capped at MaxHealth
- `AddShield(int)` — stacks additively

---

## 5. Cross-Scene Integration

### 5.1 Workshop → Battle (Entry)

```csharp
// In BattleSceneController.Awake()
if (WorkshopBattlePayloadBridge.TryConsume(out var payload))
{
    // Hydrate deck from payload.Cards
}
```

- Battle reads **once** at scene entry.
- Successful consume clears bridge state.
- Empty payload is handled gracefully (logs warning, runs with empty deck).

### 5.2 Battle → Integration (Exit)

```csharp
// On victory/defeat
BattleResultBridge.Commit(new BattleResult { ... });
```

- `BattleResult` carries: result type, boss info, damage/heal/shield totals, cards played, turns elapsed, reward ID.
- `BattleResultBridge` mirrors `WorkshopBattlePayloadBridge` pattern for symmetry.
- Consumed by the **integration programmer** to trigger post-battle rewards and scene transition.

---

## 6. Content Authoring

### ScriptableObject Types

| Type | Menu Path | Purpose |
|------|-----------|---------|
| `BattleBossDefinition` | `Arcane Atelier/Battle/Boss Definition` | Boss stats, element, action pattern |
| `BattleCardEffectTemplate` | `Arcane Atelier/Battle/Card Effect Template` | Maps card metadata to combat params |
| `BattleContentDatabase` | `Arcane Atelier/Battle/Content Database` | Registry of all battle content |

### Bootstrap Menu

`Arcane Atelier → Battle → Generate Default Content`

Generates:
- `Boss_EarthGolem` (Earth, 150 HP, 4-step pattern)
- 3 card effect templates (Attack / Heal / Defend)
- `BattleContentDatabase` registry linking them all

---

## 7. Phase Roadmap

| Phase | Deliverables | Status |
|-------|-------------|--------|
| **1** | Skeleton + data layer: enums, utilities, SO definitions, unit model, scene controller, result bridge | ✅ Complete |
| **2** | Core combat loop: simulation step, card player (deck/hand/discard), boss AI executor, damage calculator | ✅ Complete |
| **3** | IMGUI battle UI: hand area, HP bars, buff/debuff display, damage numbers, win/loss screen | ⏳ Pending |
| **4** | First boss polish + E2E: test scene, visual feedback hooks, post-battle handoff verification | ⏳ Pending |

---

## 8. Known Limitations

1. **No status effects** (Phase 2+). `EffectKeyword` is parsed but ignored. Buff/debuff system deferred to Phase 3+.
2. **No UI** yet. Combat is playable via keyboard input with `Debug.Log` output only.
3. **No .unity scene file** yet. Scene created in Phase 4.
4. **IMGUI for rapid iteration** — same as Workshop. Retained-mode UI migration TBD.

---

## 9. Dependencies

| Dependency | Direction | Purpose |
|-----------|-----------|---------|
| `ArcaneAtelier.Workshop.Runtime` | Battle → Workshop | `WorkshopBattlePayloadBridge`, `WorkshopElementAttribute`, `WorkshopSpellRole`, `WorkshopBattleCardEntry` |
| `UnityEngine.IMGUIModule` | Battle (Phase 3) | UI rendering (same approach as Workshop) |
| `com.unity.test-framework` | Test-time only | Future unit/integration tests |

---

## 10. QA Checklist for Integration

- [ ] `BattleSceneController` placed in scene + `contentDatabase` assigned → Boss initializes correctly
- [ ] Workshop commits cards → `TryConsume` returns true with expected counts
- [ ] Workshop commits zero cards → `TryConsume` returns false, scene warns but continues
- [ ] `BattleElementUtility` — Water vs Fire → Advantage; Fire vs Water → Disadvantage; Fire vs Wind → Neutral
- [ ] `BattleUnit.TakeDamage()` — shield absorbs first, overflow hits HP, HP floor at 0
- [ ] `BattleUnit.Heal()` — respects MaxHealth cap
- [ ] `BattleResultBridge.Commit()` + `TryConsume()` → one-time read, then empty
