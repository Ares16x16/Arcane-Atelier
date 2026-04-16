# Arcane Atelier — Factory-to-Battle Payload Contract

**Document Type:** Interface Contract (Cross-Scene Runtime)
**Owner:** Gameplay Engineering (Factory + Combat)
**Status:** Active integration boundary

---

## 1. Contract Intent

This contract defines the only supported handoff from Factory Scene output into Battle Scene card availability.

- Factory owns **production**.
- Battle owns **consumption and combat semantics**.
- The bridge must remain the single source of truth between scenes.

Contract surface:

- `Assets/ArcaneAtelier/Workshop/Runtime/WorkshopBattlePayload.cs`

---

## 2. Payload Shape

### `WorkshopBattlePayload`

- `List<WorkshopBattleCardEntry> Cards`

### `WorkshopBattleCardEntry`

- `CardId` — stable integration key for battle card lookup.
- `DisplayName` — display/debug string only (non-authoritative).
- `Amount` — prepared count available at battle scene entry.

Current ID namespace families produced by factory content:

- `combat.spell.basic.*`
- `combat.spell.intermediate.*`
- `combat.spell.advanced.*`

---

## 3. Lifecycle

### 3.1 Commit (Factory)

```csharp
WorkshopBattlePayloadBridge.Commit(preparedCards);
```

Behavior:

- Snapshot copy of current prepared cards.
- Null/empty entries ignored.
- Entries sorted by `CardId` for deterministic downstream processing.

### 3.2 Consume (Battle)

```csharp
if (WorkshopBattlePayloadBridge.TryConsume(out var payload))
{
    // hydrate battle inventory/deck state
}
```

Behavior:

- Intended as one-time read at battle scene startup.
- Successful consume clears bridge state.

### 3.3 Explicit Clear

```csharp
WorkshopBattlePayloadBridge.Clear();
```

Use cases:

- Return-to-title resets.
- Run abort/restart flows.
- Defensive cleanup in scene transition recovery.

---

## 4. Integration Rules (Must Follow)

1. Battle must resolve gameplay data by `CardId`, not by display text.
2. Empty payloads are valid and must map to a safe battle fallback.
3. Combat code must not introspect workshop scene state for card counts.
4. Workshop-only internals (node IDs, positions, buffers) are prohibited in this payload.
5. Payload lifecycle is edge-triggered by commit/consume; avoid hidden side channels.

---

## 5. Error Handling Expectations

- Unknown `CardId` on battle side should be logged with structured context and skipped safely.
- Payload parse/load issues must not crash scene entry.
- Repeated consume without commit should return false and preserve stable battle startup behavior.

---

## 6. Compatibility and Versioning

When evolving payload shape:

- Prefer additive fields.
- Keep existing fields backward compatible for at least one release window.
- Add battle-facing semantics only (e.g., tags, rarity, generated modifiers).
- Do not include factory implementation details.

If a breaking change is unavoidable, gate it behind explicit contract versioning.

---

## 7. QA Acceptance Checklist

Factory -> Battle integration is considered valid when all checks pass:

- Commit with no prepared cards -> `TryConsume == false`.
- Commit with prepared cards -> `TryConsume == true` and counts match production output.
- Second consume without new commit -> returns false/empty.
- `Clear()` removes staged payload.
- Unknown card IDs do not hard-fail battle initialization.
