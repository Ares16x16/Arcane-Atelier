## Content Table Guidance
#### 1. Purpose

This folder contains JSON content tables for different types of game data used in Arcane Atelier.

The content tables are designed to separate game content from game logic. Designers can edit nodes, spells, players, and bosses through JSON files without directly modifying the game code.

These files mainly support two game scenes:

* **Factory Scene**: uses node data and spell data.
* **Battle Scene**: uses spell data and entity data.

---

### 2. Node Content Structure
#### 2.1 Usage

Node content includes spirit nodes and factory nodes.

Nodes are mainly used in the Factory Scene.

Spirit nodes produce basic resources or elements.
Factory nodes receive input resources or spells and convert them into new outputs.

#### 2.2 JSON Structure

```
{
  "nodes": [
    {
      "id": 1,
      "name": "Fire Spirit Node",
      "type": "spirit",
      "input": {
        "if_input": false,
        "input_type": null
      },
      "output": {
        "if_output": true,
        "output_type": "fire_element"
      }
    }
  ]
}
```
#### 2.3 Field Explanation

| Field                | Type          | Description                                              |
| -------------------- | ------------- | -------------------------------------------------------- |
| `id`                 | number        | Unique ID of the node.                                   |
| `name`               | string        | Display name of the node.                                |
| `type`               | string        | Node type. Suggested values: `"spirit"` or `"factory"`.  |
| `input.if_input`     | boolean       | Whether this node requires input.                        |
| `input.input_type`   | string / null | Required input type. Use `null` if no input is required. |
| `output.if_output`   | boolean       | Whether this node produces output.                       |
| `output.output_type` | string / null | Output type produced by this node.                       |
---
### 3. Spell Content Structure
#### 3.1 Usage

Spell content includes all spell cards in the game.

Spell data should be used in both:

* **Factory Scene**: factories produce spell cards.
* **Battle Scene**: players use spell cards to fight bosses.

Each spell should contain basic card information, combat effects, and description text.

#### 3.2 JSON Structure
```
{
  "spells": [
    {
      "id": 1,
      "name": "Fireball",
      "rarity": "common",
      "card_artwork": "res://assets/cards/fireball.png",
      "effect": {
        "element": "fire",
        "damage": 10,
        "healing": 0,
        "defense": 0
      },
      "card_description": "Deal 10 fire damage to the enemy."
    }
  ]
}
```
#### 3.3 Field Explanation
| Field              | Type   | Description                                                                                   |
| ------------------ | ------ | --------------------------------------------------------------------------------------------- |
| `id`               | number | Unique ID of the spell.                                                                       |
| `name`             | string | Display name of the spell card.                                                               |
| `rarity`           | string | Spell rarity. Suggested values: `"common"`, `"rare"`, `"epic"`, `"legendary"`.                |
| `card_artwork`     | string | Artwork path or URL for the card image.                                                       |
| `effect.element`   | string | Spell element, such as `"fire"`, `"water"`, `"wind"`, `"earth"`, `"light"`, `"dark"`.         |
| `effect.damage`    | number | Damage dealt by the spell. Use `0` if the spell does not deal damage.                         |
| `effect.healing`   | number | Healing value of the spell. Use `0` if the spell does not heal.                               |
| `effect.defense`   | number | Defense or shield value provided by the spell. Use `0` if the spell does not provide defense. |
| `card_description` | string | Text shown on the card UI.                                                                    |
---
### 4. Entity Content Structure
#### 4.1 Usage

Entity content includes the player and bosses.

Entity data should be used in the Battle Scene.

* Player data defines the player’s base battle status.
* Boss data defines enemy health, attack, defense, attributes, and attack patterns.
#### 4.2 JSON Structure
```
{
  "entities": [
    {
      "id": 1,
      "name": "Beginner Spirit Contractor",
      "type": "player",
      "status": {
        "health": 100,
        "attribute": "neutral",
        "defense": 0,
        "attack": 5,
        "pattern": []
      }
    },
    {
      "id": 2,
      "name": "Fire Beast",
      "type": "boss",
      "status": {
        "health": 200,
        "attribute": "fire",
        "defense": 5,
        "attack": 15,
        "pattern": ["normal_attack", "fire_breath", "rage_attack"]
      }
    }
  ]
}
```
#### 4.3 Field Explanation
| Field              | Type   | Description                                                                            |
| ------------------ | ------ | -------------------------------------------------------------------------------------- |
| `id`               | number | Unique ID of the entity.                                                               |
| `name`             | string | Display name of the entity.                                                            |
| `type`             | string | Entity type. Suggested values: `"player"` or `"boss"`.                                 |
| `status.health`    | number | Maximum health value.                                                                  |
| `status.attribute` | string | Entity attribute or element type.                                                      |
| `status.defense`   | number | Base defense value.                                                                    |
| `status.attack`    | number | Base attack value.                                                                     |
| `status.pattern`   | array  | Boss behavior or attack pattern list. For player entities, this can be an empty array. |
