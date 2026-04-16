# Workshop ↔ Battle Payload Contract

## 1) Ownership boundary

- **Workshop module** owns card production and preparation.
- **Battle module** owns combat-time card consumption.

The integration surface is:

- `Assets/ArcaneAtelier/Workshop/Runtime/WorkshopBattlePayload.cs`

This boundary is intentional and should be preserved to avoid scene-level coupling.

---

## 2) Data contract

`WorkshopBattlePayload`

- `List<WorkshopBattleCardEntry> Cards`

`WorkshopBattleCardEntry`

- `CardId` (stable integration key)
- `DisplayName` (UI/display only)
- `Amount` (prepared quantity)

Current exported ID families in slice content:

- `combat.spell.basic.*`
- `combat.spell.intermediate.*`
- `combat.spell.advanced.*`

---

## 3) Lifecycle semantics

### Commit (workshop side)

```csharp
WorkshopBattlePayloadBridge.Commit(preparedCards);
```

- Takes a snapshot of currently prepared cards.
- Filters null/empty entries.
- Sorts by `CardId` for stable downstream ordering.

### Consume (battle side)

```csharp
if (WorkshopBattlePayloadBridge.TryConsume(out var payload))
{
    // hydrate loadout / deck / opening hand
}
```

- Battle should read once at scene entry.
- Successful consume clears bridge state.

### Explicit clear

```csharp
WorkshopBattlePayloadBridge.Clear();
```

Use for hard reset flows (e.g., returning to title, aborting run).

---

## 4) Integration rules

1. Treat the bridge payload as the **single source of truth** for workshop output.
2. Resolve combat cards by `CardId`; do not key off display text.
3. Handle empty payloads gracefully with battle fallback behavior.
4. Do not infer card counts from workshop ScriptableObjects or scene objects.
5. Keep workshop-only state (grid coords, node IDs, buffers) out of this contract.

---

## 5) Versioning guidance

When extending the payload, only add fields that are battle-facing and stable. Good candidates:

- Upgrade tier / rarity tier
- Elemental tags
- Generated combat modifiers

Do **not** include workshop implementation details. If uncertain, add a new wrapper field and keep old fields backward compatible for one release cycle.

---

## 6) QA checklist for integration handoff

- Commit with zero cards results in `TryConsume == false`.
- Commit with valid cards results in `TryConsume == true` and expected counts.
- Second consume without new commit returns empty.
- `Clear()` empties state and broadcasts payload-changed event.
- Combat lookup failures by `CardId` are logged and safely skipped.
