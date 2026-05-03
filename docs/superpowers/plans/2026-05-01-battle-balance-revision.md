# Battle Balance Revision Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rebalance battle difficulty from "effortless zero-damage" to "medium-tight" by adjusting player resources, enemy stats, hand size, transition delay, and adding minimal randomness to enemy AI.

**Architecture:** Pure parameter and logic adjustments within existing battle system. No new ScriptableObject types, no new resource systems. Changes span 5 files: card/boss definitions (data), deck controller (hand size), scene controller (HP/delay/healing), boss AI (random pools), and tests.

**Tech Stack:** Unity C# (Battle module), NUnit (EditMode tests)

---

### Task 1: Update BattleContentBootstrapper — Player Card Definitions

**Files:**
- Modify: `Assets/ArcaneAtelier/Battle/Editor/BattleContentBootstrapper.cs`

Reduce base healing and shield values so 1 defense card no longer fully negates an enemy turn.

- [ ] **Step 1: Downgrade Tidal Mend healing**

```csharp
// Find this line (around line 485):
BattleEffectInstruction.Heal(6, 1),
// Change to:
BattleEffectInstruction.Heal(5, 1),
```

- [ ] **Step 2: Downgrade Lumen Prayer healing**

```csharp
// Find this line (around line 515):
BattleEffectInstruction.Heal(5, 2),
// Change to:
BattleEffectInstruction.Heal(4, 2),
```

- [ ] **Step 3: Downgrade Stoneguard Sigil shield**

```csharp
// Find this line (around line 497):
BattleEffectInstruction.Shield(7, 1),
// Change to:
BattleEffectInstruction.Shield(6, 1),
```

- [ ] **Step 4: Downgrade Gloam Ward shield**

```csharp
// Find this line (around line 521):
BattleEffectInstruction.Shield(6, 1),
// Change to:
BattleEffectInstruction.Shield(5, 1),
```

- [ ] **Step 5: Commit**

```bash
git add Assets/ArcaneAtelier/Battle/Editor/BattleContentBootstrapper.cs
git commit -m "balance: reduce base healing and shield card values"
```

---

### Task 2: Update BattleContentBootstrapper — Fallback Workshop Card Values

**Files:**
- Modify: `Assets/ArcaneAtelier/Battle/Editor/BattleContentBootstrapper.cs`

Reduce fallback deck shield so Workshop-produced Arcane Ward doesn't trivialize early encounters.

- [ ] **Step 1: Reduce Arcane Ward shield**

```csharp
// Find this block (around line 609-612):
definitions.Add(CreateCardDefinition(
    "combat.arcane_ward", "Arcane Ward",
    WorkshopElementAttribute.Earth, WorkshopSpellTier.Basic,
    BattleEffectInstruction.Shield(10)));
// Change Shield(10) to Shield(8)
```

- [ ] **Step 2: Commit**

```bash
git add Assets/ArcaneAtelier/Battle/Editor/BattleContentBootstrapper.cs
git commit -m "balance: reduce Arcane Ward fallback shield from 10 to 8"
```

---

### Task 3: Update BattleContentBootstrapper — Enemy Definitions

**Files:**
- Modify: `Assets/ArcaneAtelier/Battle/Editor/BattleContentBootstrapper.cs`

Increase enemy HP and damage to create real pressure across all 4 encounters.

- [ ] **Step 1: Buff Ash Imp stats**

```csharp
// In CreateAshImpEnemy(), change:
// MaxHealth: 30 -> 35
// Attack values: 6 -> 8, 9 -> 11, 8 -> 10
asset.Configure(
    "enemy.ash.imp",
    "Ash Imp",
    35,  // was 30
    WorkshopElementAttribute.Fire,
    BattleEncounterType.Enemy,
    1,
    BattleEnemyArchetype.Aggressive,
    0.3f,
    0,
    3,
    new BattleBossAction[]
    {
        new BattleBossAction
        {
            ActionType = BattleActionType.Attack,
            Value = 8,  // was 6
            SecondaryValue = 0f,
            Description = "Scorches with ember claws"
        },
        new BattleBossAction
        {
            ActionType = BattleActionType.Attack,
            Value = 11,  // was 9
            SecondaryValue = 0f,
            Description = "Spits a burst of cinders"
        },
        new BattleBossAction
        {
            ActionType = BattleActionType.Special,
            Value = 10,  // was 8
            SecondaryValue = 0f,
            Description = "Ignites the air"
        }
    },
    "reward.enemy.fire.minor");
```

- [ ] **Step 2: Buff Mist Leech stats**

```csharp
// In CreateMistLeechEnemy(), change:
// MaxHealth: 40 -> 45
// Attack values: 6 -> 8, 7 -> 10
asset.Configure(
    "enemy.mist.leech",
    "Mist Leech",
    45,  // was 40
    WorkshopElementAttribute.Water,
    BattleEncounterType.Enemy,
    2,
    BattleEnemyArchetype.Sustain,
    0.5f,
    0,
    3,
    new BattleBossAction[]
    {
        new BattleBossAction
        {
            ActionType = BattleActionType.Attack,
            Value = 8,  // was 6
            SecondaryValue = 0f,
            Description = "Drains a thread of vitality"
        },
        new BattleBossAction
        {
            ActionType = BattleActionType.Heal,
            Value = 4,
            SecondaryValue = 0f,
            Description = "Condenses moisture to recover"
        },
        new BattleBossAction
        {
            ActionType = BattleActionType.Attack,
            Value = 10,  // was 7
            SecondaryValue = 0f,
            Description = "Lashes with a liquid tendril"
        }
    },
    "reward.enemy.water.minor");
```

- [ ] **Step 3: Buff Moss Shell stats**

```csharp
// In CreateMossShellEnemy(), change:
// MaxHealth: 48 -> 50
// Attack value: 5 -> 9
asset.Configure(
    "enemy.moss.shell",
    "Moss Shell",
    50,  // was 48
    WorkshopElementAttribute.Earth,
    BattleEncounterType.Enemy,
    3,
    BattleEnemyArchetype.Defensive,
    0.45f,
    12,
    4,
    new BattleBossAction[]
    {
        new BattleBossAction
        {
            ActionType = BattleActionType.Defend,
            Value = 6,
            SecondaryValue = 0f,
            Description = "Raises a bark shield"
        },
        new BattleBossAction
        {
            ActionType = BattleActionType.Attack,
            Value = 9,  // was 5
            SecondaryValue = 0f,
            Description = "Body slams forward"
        },
        new BattleBossAction
        {
            ActionType = BattleActionType.Defend,
            Value = 8,
            SecondaryValue = 0f,
            Description = "Roots tighten into armor"
        }
    },
    "reward.enemy.earth.minor");
```

- [ ] **Step 4: Nerf Earth Golem stats**

```csharp
// In CreateEarthGolemBoss(), change:
// MaxHealth: 110 -> 90
// P1 attacks: 11 -> 10, 15 -> 14
// P2 attacks: 16 -> 16, 22 -> 20
// Phase transition: keep 0.5f
asset.Configure(
    "boss.earth.golem",
    "Corrupted Earth Golem",
    90,  // was 110
    WorkshopElementAttribute.Earth,
    BattleEncounterType.Boss,
    100,
    BattleEnemyArchetype.None,
    0.35f,
    16,
    3,
    new BattleBossAction[]  // P1 pattern
    {
        new BattleBossAction { ActionType = BattleActionType.Attack, Value = 10, SecondaryValue = 0f, Description = "Slams the ground" },
        new BattleBossAction { ActionType = BattleActionType.Defend, Value = 8, SecondaryValue = 0f, Description = "Hardens its shell" },
        new BattleBossAction { ActionType = BattleActionType.Attack, Value = 14, SecondaryValue = 0f, Description = "Heavy strike" },
        new BattleBossAction { ActionType = BattleActionType.Heal, Value = 7, SecondaryValue = 0f, Description = "Absorbs earth energy" }
    },
    "reward.spirit.earth",
    new BattleBossAction[]  // P2 pattern
    {
        new BattleBossAction { ActionType = BattleActionType.Attack, Value = 16, SecondaryValue = 0f, Description = "Raging slam" },
        new BattleBossAction { ActionType = BattleActionType.Attack, Value = 20, SecondaryValue = 0f, Description = "Crushing blow" },
        new BattleBossAction { ActionType = BattleActionType.Heal, Value = 10, SecondaryValue = 0f, Description = "Devours earth energy" }
    },
    0.5f);
```

- [ ] **Step 5: Commit**

```bash
git add Assets/ArcaneAtelier/Battle/Editor/BattleContentBootstrapper.cs
git commit -m "balance: rebalance all enemy stats for medium-tight difficulty"
```

---

### Task 4: Update BattleDeckController — Hand Size 5 → 4

**Files:**
- Modify: `Assets/ArcaneAtelier/Battle/Runtime/BattleDeckController.cs`

Reduce hand size to tighten decision space.

- [ ] **Step 1: Change initial draw**

```csharp
// Find this line (around line 26):
DrawCards(5);
// Change to:
DrawCards(4);
```

- [ ] **Step 2: Change end-of-turn redraw**

```csharp
// Find this line (around line 180):
DrawCards(5);
// Change to:
DrawCards(4);
```

- [ ] **Step 3: Commit**

```bash
git add Assets/ArcaneAtelier/Battle/Runtime/BattleDeckController.cs
git commit -m "balance: reduce hand size from 5 to 4"
```

---

### Task 5: Update BattleSceneController — Player HP, Delay, Inter-Encounter Healing

**Files:**
- Modify: `Assets/ArcaneAtelier/Battle/Runtime/BattleSceneController.cs`

- [ ] **Step 1: Reduce player max health**

```csharp
// Find this line (around line 17):
[SerializeField] private int playerMaxHealth = 100;
// Change to:
[SerializeField] private int playerMaxHealth = 80;
```

- [ ] **Step 2: Reduce boss turn transition delay**

```csharp
// Find this line (around line 10):
private const float BossTurnTransitionDelay = 3.0f;
// Change to:
private const float BossTurnTransitionDelay = 1.1f;
```

- [ ] **Step 3: Add inter-encounter healing**

```csharp
// Find OnBattleEnded method (around line 392), locate this block:
if (result.ResultType == BattleResultType.Victory && currentEncounterIndex < encounterDefinitions.Count - 1)
{
    string clearedName = result.BossDisplayName;
    currentEncounterIndex++;
    AddRecentEvent($"{clearedName} defeated. Advancing to next encounter.");
    Debug.Log($"=== ENCOUNTER CLEARED: {clearedName} ===");
    StartEncounter(currentEncounterIndex, resetDeck: false);
    return;
}

// Replace with:
if (result.ResultType == BattleResultType.Victory && currentEncounterIndex < encounterDefinitions.Count - 1)
{
    string clearedName = result.BossDisplayName;
    currentEncounterIndex++;

    int healAmount = 15;
    Player.Heal(healAmount);
    AddRecentEvent($"{clearedName} defeated. Recovered {healAmount} HP. Advancing to next encounter.");
    Debug.Log($"=== ENCOUNTER CLEARED: {clearedName} === Recovered {healAmount} HP ===");
    StartEncounter(currentEncounterIndex, resetDeck: false);
    return;
}
```

- [ ] **Step 4: Commit**

```bash
git add Assets/ArcaneAtelier/Battle/Runtime/BattleSceneController.cs
git commit -m "balance: player HP 100->80, boss delay 3.0->1.1s, add inter-encounter healing"
```

---

### Task 6: Update BattleBossAI — Enemy Random Behavior Pools

**Files:**
- Modify: `Assets/ArcaneAtelier/Battle/Runtime/BattleBossAI.cs`

Replace deterministic enemy action selection with probability-based pools. This prevents players from memorizing the exact sequence after turn 1.

- [ ] **Step 1: Add Random field**

```csharp
// Add after existing fields (around line 20):
private readonly System.Random rng = new System.Random();
```

- [ ] **Step 2: Rewrite SelectAggressiveEnemyAction**

```csharp
// Find method SelectAggressiveEnemyAction (around line 155), replace entire method:
private BattleBossAction SelectAggressiveEnemyAction()
{
    if (specialActions.Count > 0 && rng.Next(100) < 20)
    {
        return CloneAction(specialActions[rng.Next(specialActions.Count)], 1f);
    }

    if (attackActions.Count > 0)
    {
        return GetScaledAction(attackActions, rng.Next(attackActions.Count), 0.85f);
    }

    return PeekFallbackAction();
}
```

- [ ] **Step 3: Rewrite SelectSustainEnemyAction**

```csharp
// Find method SelectSustainEnemyAction (around line 165), replace entire method:
private BattleBossAction SelectSustainEnemyAction()
{
    if (IsLowHealth() && healActions.Count > 0 && rng.Next(100) < 80)
    {
        return CloneAction(healActions[rng.Next(healActions.Count)], 1f);
    }

    if (specialActions.Count > 0 && rng.Next(100) < 20)
    {
        return CloneAction(specialActions[rng.Next(specialActions.Count)], 1f);
    }

    if (attackActions.Count > 0)
    {
        return GetScaledAction(attackActions, rng.Next(attackActions.Count), 1f);
    }

    return PeekFallbackAction();
}
```

- [ ] **Step 4: Rewrite SelectDefensiveEnemyAction**

```csharp
// Find method SelectDefensiveEnemyAction (around line 190), replace entire method:
private BattleBossAction SelectDefensiveEnemyAction()
{
    if (ShouldAddShield() && defendActions.Count > 0)
    {
        return CloneAction(defendActions[rng.Next(defendActions.Count)], 1f);
    }

    if (specialActions.Count > 0 && rng.Next(100) < 20)
    {
        return CloneAction(specialActions[rng.Next(specialActions.Count)], 1f);
    }

    if (attackActions.Count > 0)
    {
        return GetScaledAction(attackActions, rng.Next(attackActions.Count), 0.95f);
    }

    if (defendActions.Count > 0)
    {
        return CloneAction(defendActions[rng.Next(defendActions.Count)], 1f);
    }

    return PeekFallbackAction();
}
```

- [ ] **Step 5: Commit**

```bash
git add Assets/ArcaneAtelier/Battle/Runtime/BattleBossAI.cs
git commit -m "feat: add probability-based behavior pools to enemy AI archetypes"
```

---

### Task 7: Update Existing Tests

**Files:**
- Modify: `Assets/ArcaneAtelier/Battle/Tests/Editor/BattleSimulationTurnFlowTests.cs`

Update test assertions that reference hardcoded values changed in this revision.

- [ ] **Step 1: Update EndTurn test for new hand size**

This test verifies that ending the turn puts the simulation into `BossTurnPending` state. It does not assert hand size directly, so no change needed for the `EndTurn_DoesNotResolveBossActionImmediately` test.

However, add a new test to verify the 4-card hand size:

```csharp
// Add new test method after existing tests:
[Test]
public void BattleDeckController_InitialHandSize_IsFour()
{
    BattleSimulation simulation = CreateSimulation();
    Assert.That(simulation.Deck.HandCount, Is.EqualTo(4));
}
```

- [ ] **Step 2: Add test for inter-encounter healing logic**

Since inter-encounter healing lives in `BattleSceneController` (a MonoBehaviour), this is harder to unit test in isolation. Skip adding a unit test for this — it will be validated in manual playtesting.

- [ ] **Step 3: Commit**

```bash
git add Assets/ArcaneAtelier/Battle/Tests/Editor/BattleSimulationTurnFlowTests.cs
git commit -m "test: add hand size assertion for new 4-card default"
```

---

### Task 8: Regenerate Battle Content Assets (Editor-Only)

**Files:**
- Trigger: Unity Editor menu item

The bootstrapper changes above only affect the C# code that generates assets. Existing `.asset` files in `Assets/ArcaneAtelier/Battle/Content/` must be regenerated for the new values to take effect at runtime.

- [ ] **Step 1: Run content regeneration**

In Unity Editor, run: **Arcane Atelier → Battle → Generate Default Content**

This will overwrite:
- `Boss_EarthGolem.asset`
- `Enemy_AshImp.asset`
- `Enemy_MossShell.asset`
- `Enemy_MistLeech.asset`
- `CardDefinition_*.asset` (all card definitions)
- `BattleContentDatabase.asset`

- [ ] **Step 2: Commit regenerated assets**

```bash
git add Assets/ArcaneAtelier/Battle/Content/
git commit -m "chore: regenerate battle content assets with rebalanced values"
```

---

## Self-Review Checklist

**1. Spec coverage:**
- [x] Player HP 80 (Task 5)
- [x] Hand size 4 (Task 4)
- [x] Boss delay 1.1s (Task 5)
- [x] Inter-encounter healing +15 (Task 5)
- [x] Enemy stat rebalance — Ash/Mist/Moss up, Golem down (Task 3)
- [x] Card values down — heal/shield (Tasks 1-2)
- [x] Enemy AI random pools (Task 6)

**2. Placeholder scan:** No TBD/TODO/fill-in found.

**3. Type consistency:** All method names and property names match existing codebase (`CreateSimulation`, `HandCount`, `ActionPoints`, etc.).

---

## Execution Handoff

Plan complete and saved to `docs/superpowers/plans/2026-05-01-battle-balance-revision.md`.

Two execution options:

**1. Subagent-Driven (recommended)** — I dispatch a fresh subagent per task, review between tasks, fast iteration.

**2. Inline Execution** — Execute tasks in this session using executing-plans, batch execution with checkpoints.

**Which approach?**
