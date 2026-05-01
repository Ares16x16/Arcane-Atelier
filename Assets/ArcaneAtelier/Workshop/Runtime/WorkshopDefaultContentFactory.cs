using System;
using UnityEngine;

namespace ArcaneAtelier.Workshop
{
    public static class WorkshopDefaultContentFactory
    {
        public static WorkshopContentDatabase CreateRuntimeDatabase()
        {
            var fire = CreateItem("element.fire", "Fire", "Basic fire essence shaped by a bonded spirit.", WorkshopItemKind.Resource, new Color(0.95f, 0.36f, 0.2f));
            var water = CreateItem("element.water", "Water", "Basic water essence used for healing and control patterns.", WorkshopItemKind.Resource, new Color(0.26f, 0.62f, 0.97f));
            var wind = CreateItem("element.wind", "Wind", "Basic wind essence suited to multi-hit and tempo cards.", WorkshopItemKind.Resource, new Color(0.57f, 0.84f, 0.9f));
            var earth = CreateItem("element.earth", "Earth", "Basic earth essence used for durable defensive constructs.", WorkshopItemKind.Resource, new Color(0.63f, 0.47f, 0.28f));

            var cinderDart = CreateItem(
                "spell.basic.fire",
                "Cinder Dart",
                "Basic fire attack spell shaped from raw Fire.",
                WorkshopItemKind.Card,
                new Color(0.95f, 0.43f, 0.2f),
                "combat.spell.basic.fire",
                WorkshopElementAttribute.Fire,
                WorkshopSpellTier.Basic,
                WorkshopSpellRole.Attack,
                10f,
                8,
                1,
                1f,
                "Burn");
            var tidalMend = CreateItem(
                "spell.basic.water",
                "Tidal Mend",
                "Basic water healing spell shaped from raw Water.",
                WorkshopItemKind.Card,
                new Color(0.33f, 0.68f, 1f),
                "combat.spell.basic.water",
                WorkshopElementAttribute.Water,
                WorkshopSpellTier.Basic,
                WorkshopSpellRole.Healing,
                12f,
                6,
                1,
                8f,
                "Regen");
            var zephyrCut = CreateItem(
                "spell.basic.wind",
                "Zephyr Cut",
                "Basic wind attack spell that strikes in quick succession.",
                WorkshopItemKind.Card,
                new Color(0.66f, 0.91f, 0.95f),
                "combat.spell.basic.wind",
                WorkshopElementAttribute.Wind,
                WorkshopSpellTier.Basic,
                WorkshopSpellRole.Attack,
                11f,
                5,
                2,
                10f,
                "Expose");
            var stoneguardSigil = CreateItem(
                "spell.basic.earth",
                "Stoneguard Sigil",
                "Basic earth defense spell that anchors the caster.",
                WorkshopItemKind.Card,
                new Color(0.71f, 0.55f, 0.35f),
                "combat.spell.basic.earth",
                WorkshopElementAttribute.Earth,
                WorkshopSpellTier.Basic,
                WorkshopSpellRole.Defense,
                11f,
                7,
                1,
                18f,
                "Bulwark");

            var fireSpirit = CreateNode(
                "node.spirit.fire",
                "Fire Spirit",
                "Source node. Outputs Fire into the connected network.",
                WorkshopNodeCategory.Source,
                true,
                new Color(0.83f, 0.3f, 0.2f),
                NodePortMask.None,
                NodePortMask.East,
                10,
                2,
                false,
                WorkshopProductionRecipe.Create("recipe.spirit.fire", "Generate Fire", 1f, Array.Empty<WorkshopItemStack>(), new[] { WorkshopItemStack.Create(fire, 1) }));

            var waterSpirit = CreateNode(
                "node.spirit.water",
                "Water Spirit",
                "Source node. Outputs Water into the connected network.",
                WorkshopNodeCategory.Source,
                true,
                new Color(0.27f, 0.54f, 0.84f),
                NodePortMask.None,
                NodePortMask.East,
                10,
                2,
                false,
                WorkshopProductionRecipe.Create("recipe.spirit.water", "Generate Water", 1f, Array.Empty<WorkshopItemStack>(), new[] { WorkshopItemStack.Create(water, 1) }));

            var windSpirit = CreateNode(
                "node.spirit.wind",
                "Wind Spirit",
                "Source node. Outputs Wind into the connected network.",
                WorkshopNodeCategory.Source,
                true,
                new Color(0.5f, 0.76f, 0.83f),
                NodePortMask.None,
                NodePortMask.East,
                10,
                2,
                false,
                WorkshopProductionRecipe.Create("recipe.spirit.wind", "Generate Wind", 1f, Array.Empty<WorkshopItemStack>(), new[] { WorkshopItemStack.Create(wind, 1) }));

            var earthSpirit = CreateNode(
                "node.spirit.earth",
                "Earth Spirit",
                "Source node. Outputs Earth into the connected network.",
                WorkshopNodeCategory.Source,
                true,
                new Color(0.53f, 0.41f, 0.27f),
                NodePortMask.None,
                NodePortMask.East,
                10,
                2,
                false,
                WorkshopProductionRecipe.Create("recipe.spirit.earth", "Generate Earth", 1f, Array.Empty<WorkshopItemStack>(), new[] { WorkshopItemStack.Create(earth, 1) }));

            var elementShaper = CreateNode(
                "node.factory.element_shaping",
                "Element Shaper",
                "Crafter node. Shapes one element into one basic spell card.",
                WorkshopNodeCategory.Crafter,
                true,
                new Color(0.95f, 0.62f, 0.25f),
                NodePortMask.West,
                NodePortMask.East,
                12,
                2,
                false,
                WorkshopProductionRecipe.Create(
                    "recipe.shape.fire",
                    "Shape Cinder Dart",
                    1.2f,
                    new[] { WorkshopItemStack.Create(fire, 1) },
                    new[] { WorkshopItemStack.Create(cinderDart, 1) }),
                WorkshopProductionRecipe.Create(
                    "recipe.shape.water",
                    "Shape Tidal Mend",
                    1.2f,
                    new[] { WorkshopItemStack.Create(water, 1) },
                    new[] { WorkshopItemStack.Create(tidalMend, 1) }),
                WorkshopProductionRecipe.Create(
                    "recipe.shape.wind",
                    "Shape Zephyr Cut",
                    1.2f,
                    new[] { WorkshopItemStack.Create(wind, 1) },
                    new[] { WorkshopItemStack.Create(zephyrCut, 1) }),
                WorkshopProductionRecipe.Create(
                    "recipe.shape.earth",
                    "Shape Stoneguard Sigil",
                    1.2f,
                    new[] { WorkshopItemStack.Create(earth, 1) },
                    new[] { WorkshopItemStack.Create(stoneguardSigil, 1) }));

            var conduit = CreateNode(
                "node.factory.conduit",
                "Arcane Conduit",
                "Storage / relay node. Forwards any resource along its rotated output lane.",
                WorkshopNodeCategory.Storage,
                true,
                new Color(0.37f, 0.39f, 0.43f),
                NodePortMask.West,
                NodePortMask.East,
                12,
                2,
                true,
                Array.Empty<WorkshopProductionRecipe>());

            var shaperBoostReward = CreateReward("reward.boost.shaping", "Shaper Calibration", "Applies a permanent +25% cycle speed bonus to placed Element Shapers.", WorkshopRewardKind.EfficiencyBoost, elementShaper, 0.25f, Array.Empty<WorkshopItemStack>());
            var reserveReward = CreateReward("reward.resources.recovery", "Emergency Element Cache", "Injects extra Fire and Water into reserve inventory for chain recovery.", WorkshopRewardKind.GrantItems, null, 0f, new[]
            {
                WorkshopItemStack.Create(fire, 3),
                WorkshopItemStack.Create(water, 3)
            });

            var database = ScriptableObject.CreateInstance<WorkshopContentDatabase>();
            database.hideFlags = HideFlags.HideAndDontSave;
            database.Configure(
                new Vector2Int(9, 6),
                0.25f,
                new[] { fireSpirit, waterSpirit, windSpirit, earthSpirit, elementShaper, conduit },
                new[] { shaperBoostReward, reserveReward },
                new[]
                {
                    WorkshopPlacedNodeSeed.Create(fireSpirit, new Vector2Int(1, 4), 0),
                    WorkshopPlacedNodeSeed.Create(elementShaper, new Vector2Int(2, 4), 0),
                    WorkshopPlacedNodeSeed.Create(waterSpirit, new Vector2Int(1, 2), 0),
                    WorkshopPlacedNodeSeed.Create(elementShaper, new Vector2Int(2, 2), 0),
                    WorkshopPlacedNodeSeed.Create(windSpirit, new Vector2Int(5, 4), 0),
                    WorkshopPlacedNodeSeed.Create(elementShaper, new Vector2Int(6, 4), 0),
                    WorkshopPlacedNodeSeed.Create(earthSpirit, new Vector2Int(5, 2), 0),
                    WorkshopPlacedNodeSeed.Create(elementShaper, new Vector2Int(6, 2), 0)
                });
            return database;
        }

        private static WorkshopItemDefinition CreateItem(
            string id,
            string displayName,
            string description,
            WorkshopItemKind kind,
            Color tint,
            string battleCardId = "",
            WorkshopElementAttribute element = WorkshopElementAttribute.None,
            WorkshopSpellTier tier = WorkshopSpellTier.None,
            WorkshopSpellRole role = WorkshopSpellRole.None,
            float rarityWeight = 1f,
            int primaryValue = 0,
            int hitCount = 1,
            float secondaryValue = 0f,
            string effectKeyword = "")
        {
            var item = ScriptableObject.CreateInstance<WorkshopItemDefinition>();
            item.hideFlags = HideFlags.HideAndDontSave;
            item.Configure(id, displayName, description, kind, tint, battleCardId, element, tier, role, rarityWeight, primaryValue, hitCount, secondaryValue, effectKeyword);
            return item;
        }

        private static WorkshopNodeDefinition CreateNode(
            string id,
            string displayName,
            string description,
            WorkshopNodeCategory category,
            bool unlockedByDefault,
            Color tint,
            NodePortMask inputPorts,
            NodePortMask outputPorts,
            int bufferCapacity,
            int maxTransferPerStep,
            bool acceptsAnyResource,
            params WorkshopProductionRecipe[] recipes)
        {
            var node = ScriptableObject.CreateInstance<WorkshopNodeDefinition>();
            node.hideFlags = HideFlags.HideAndDontSave;
            node.Configure(id, displayName, description, category, unlockedByDefault, tint, inputPorts, outputPorts, bufferCapacity, maxTransferPerStep, acceptsAnyResource, recipes);
            return node;
        }

        private static WorkshopRewardDefinition CreateReward(
            string id,
            string displayName,
            string description,
            WorkshopRewardKind rewardKind,
            WorkshopNodeDefinition targetNode,
            float efficiencyBonus,
            WorkshopItemStack[] grantedItems)
        {
            var reward = ScriptableObject.CreateInstance<WorkshopRewardDefinition>();
            reward.hideFlags = HideFlags.HideAndDontSave;
            reward.Configure(id, displayName, description, rewardKind, targetNode, efficiencyBonus, grantedItems);
            return reward;
        }
    }
}
