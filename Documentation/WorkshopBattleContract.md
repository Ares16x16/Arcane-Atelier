# Workshop to Battle Contract

## Ownership
The workshop module owns production and card preparation.
The battle module owns card consumption during combat.

The handoff surface is `Assets/ArcaneAtelier/Workshop/Runtime/WorkshopBattlePayload.cs`.

## Data Shape
`WorkshopBattlePayload` contains a flat list of `WorkshopBattleCardEntry`.

Each entry includes:

- `CardId`
- `DisplayName`
- `Amount`

`CardId` is the stable integration key. The battle team should map it to their combat card database.

Current exported IDs:

- `combat.flame_bolt`
- `combat.frost_sigil`
- `combat.arcane_ward`

## Lifecycle
### Commit

The workshop scene calls:

```csharp
WorkshopBattlePayloadBridge.Commit(preparedCards);
```

This snapshots the currently crafted cards into the bridge.

### Read

The battle scene should read once at scene entry:

```csharp
if (WorkshopBattlePayloadBridge.TryConsume(out var payload))
{
    // hydrate battle deck / hand / pre-combat loadout
}
```

### Clear

`TryConsume` clears the bridge after a successful read.
`WorkshopBattlePayloadBridge.Clear()` is available for explicit cleanup.

## Integration Rules

- Treat the bridge as the single source of truth for produced combat cards.
- Do not infer workshop output from scene objects or ScriptableObject asset counts.
- Use `CardId` for lookup; `DisplayName` is display-only.
- `Amount` is the count of prepared cards of that type for the next battle.
- If the payload is empty, combat should fall back to its empty-loadout handling.

## Extension Path
When the battle system stabilizes, extend the payload with additional fields only if they are battle-facing and stable, for example:

- upgrade tier,
- elemental tags,
- generated modifiers.

Do not place workshop-only data such as grid coordinates or node identifiers into the battle payload.
