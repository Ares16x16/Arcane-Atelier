# Arcane Atelier - Content Table Guidance

**Document Type:** Content schema guidance  
**Audience:** Technical Design, Content Design, Gameplay Engineering  
**Status:** Target data-authoring structure for the full game

---

## 1. Purpose

This folder defines the recommended content-table shape for Arcane Atelier.

The goal is to keep game tuning data outside hardcoded gameplay logic wherever reasonable. Designers should be able to author:

- nodes
- spells
- enemies
- route nodes
- act progression rules
- rewards
- meta unlocks

without rewriting core systems.

This guidance expands the lighter structure that existed on the `textfile` branch.

---

## 2. Recommended table groups

| Table | Purpose |
|-------|---------|
| `nodes` | workshop machine definitions and unlock rules |
| `spells` | battle-facing card definitions and workshop recipe outputs |
| `entities` | player, enemies, bosses |
| `encounters` | normal, elite, and boss fight setup |
| `route_nodes` | map-node definitions, prep windows, reward bias |
| `act_progression` | battle-count thresholds that trigger act bosses |
| `rewards` | post-battle reward choices |
| `legacy_sigils` | permanent cross-run unlocks |

---

## 3. Node table guidance

Nodes should define both workshop behavior and progression metadata.

Recommended shape:

```json
{
  "nodes": [
    {
      "id": "node.spirit.fire",
      "display_name": "Fire Spirit",
      "category": "source",
      "tier": 1,
      "unlocked_by_default": true,
      "input_rules": [],
      "output_rules": [
        {
          "item_id": "element.fire",
          "amount": 1,
          "cadence_ticks": 1
        }
      ],
      "placement_cost": 0,
      "tags": ["spirit", "basic_element"]
    }
  ]
}
```

Recommended fields:

- `id`
- `display_name`
- `category`
- `tier`
- `unlocked_by_default`
- `input_rules`
- `output_rules`
- `placement_cost`
- `tags`
- `unlock_source`

---

## 4. Spell table guidance

Spells need to satisfy both workshop output and battle execution.

Recommended shape:

```json
{
  "spells": [
    {
      "id": "spell.basic.fire.cinder_dart",
      "combat_id": "combat.spell.basic.fire",
      "display_name": "Cinder Dart",
      "tier": "basic",
      "rarity": "common",
      "element": "fire",
      "role": "attack",
      "energy_cost": 1,
      "primary_value": 8,
      "hit_count": 1,
      "secondary_value": 1,
      "effect_keyword": "burn",
      "art_key": "card_fire_cinder_dart",
      "description": "Deal 8 damage and apply 1 Burn."
    }
  ]
}
```

Recommended fields:

- `id`
- `combat_id`
- `display_name`
- `tier`
- `rarity`
- `element`
- `role`
- `energy_cost`
- `primary_value`
- `hit_count`
- `secondary_value`
- `effect_keyword`
- `art_key`
- `description`

---

## 5. Entity table guidance

Entities should support player, standard enemies, elites, and bosses.

Recommended shape:

```json
{
  "entities": [
    {
      "id": "boss.briar_colossus",
      "display_name": "Briar Colossus",
      "entity_type": "boss",
      "element": "earth",
      "max_health": 220,
      "base_shield": 0,
      "intent_pattern": [
        { "type": "attack", "value": 14, "description": "Root Slam" },
        { "type": "defend", "value": 12, "description": "Barkplate" },
        { "type": "special", "value": 0, "description": "Seed the arena" }
      ],
      "tags": ["act_1", "boss", "siege"]
    }
  ]
}
```

Recommended fields:

- `id`
- `display_name`
- `entity_type`
- `element`
- `max_health`
- `base_shield`
- `intent_pattern`
- `tags`
- `reward_pool_id`

---

## 6. Encounter table guidance

Encounters are the bridge between route selection and battle setup.

Recommended shape:

```json
{
  "encounters": [
    {
      "id": "encounter.act1.skirmish.ashmaw_pack",
      "display_name": "Ashmaw Pack",
      "encounter_type": "skirmish",
      "act": 1,
      "enemy_ids": ["enemy.ashmaw.hound", "enemy.ashmaw.alpha"],
      "prep_ticks_base": 120,
      "reward_pool_id": "reward.skirmish.act1",
      "preview_tags": ["aggressive", "multi_hit", "fire_weak"]
    }
  ]
}
```

Recommended fields:

- `id`
- `display_name`
- `encounter_type`
- `act`
- `enemy_ids`
- `prep_ticks_base`
- `reward_pool_id`
- `preview_tags`
- `hazard_modifiers`

---

## 7. Route node table guidance

Route nodes let design author map behavior without hardcoding each path.

Recommended shape:

```json
{
  "route_nodes": [
    {
      "id": "route.skirmish.act1",
      "node_type": "skirmish",
      "display_name": "Fractured Gate",
      "encounter_pool_id": "encounter_pool.skirmish.act1",
      "prep_tick_modifier": 0,
      "reward_bias": ["balanced"],
      "map_weight": 40
    }
  ]
}
```

Recommended fields:

- `id`
- `node_type`
- `display_name`
- `encounter_pool_id`
- `prep_tick_modifier`
- `reward_bias`
- `map_weight`
- `can_appear_after`

Boss encounters should not live in the normal `route_nodes` table when bosses are progression-triggered. They should be referenced by an act progression table.

---

## 8. Act progression guidance

Act progression defines when bosses appear.

Recommended shape:

```json
{
  "act_progression": [
    {
      "act": 1,
      "boss_encounter_id": "encounter.act1.boss.briar_colossus",
      "boss_trigger_after_cleared_combats": 4,
      "boss_prep_ticks": 150
    }
  ]
}
```

Recommended fields:

- `act`
- `boss_encounter_id`
- `boss_trigger_after_cleared_combats`
- `boss_prep_ticks`
- `pre_boss_briefing_id`

---

## 9. Reward table guidance

Rewards must support both immediate run power and long-tail progression.

Recommended shape:

```json
{
  "rewards": [
    {
      "id": "reward.unlock.spell_fusion_1",
      "reward_type": "unlock_node",
      "display_name": "Blueprint: Spell Fusion I",
      "description": "Unlock the first spell fusion machine.",
      "target_id": "node.factory.spell_fusion_1",
      "rarity": "uncommon",
      "tags": ["workshop", "blueprint"]
    }
  ]
}
```

Recommended fields:

- `id`
- `reward_type`
- `display_name`
- `description`
- `target_id`
- `rarity`
- `tags`
- `stacking_rules`

---

## 10. Legacy sigil table guidance

Meta progression should be data-authored too.

Recommended shape:

```json
{
  "legacy_sigils": [
    {
      "id": "legacy.kindled_start",
      "display_name": "Kindled Start",
      "unlock_condition": "defeat_final_boss_once",
      "effect_type": "prep_ticks_bonus",
      "effect_value": 20,
      "description": "Start each run with 20 extra prep ticks in the first encounter."
    }
  ]
}
```

Recommended fields:

- `id`
- `display_name`
- `unlock_condition`
- `effect_type`
- `effect_value`
- `description`
- `ui_icon_key`

---

## 11. Authoring rules

To keep the data layer safe:

1. IDs must be stable and never repurposed.
2. Display names can change without breaking references.
3. Battle should resolve cards by `combat_id` or stable gameplay ID, never by display text.
4. Reward tables should reference node/spell/entity IDs, not scene objects.
5. Route content should author prep windows through data, not hardcoded scene logic.
6. Boss appearance timing should come from `act_progression`, not from a hand-authored boss route node.

---

## 12. Implementation recommendation

The current project already uses ScriptableObject-driven workshop content. The long-term recommendation is:

- keep runtime systems data-driven
- allow editor tooling to import/export JSON or CSV
- use validation passes before content enters runtime

This means the content tables can stay as design truth even if the runtime storage format remains ScriptableObjects.
