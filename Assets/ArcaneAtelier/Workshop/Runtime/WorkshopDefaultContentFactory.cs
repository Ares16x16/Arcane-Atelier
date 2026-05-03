using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ArcaneAtelier.Workshop
{
    public static class WorkshopDefaultContentFactory
    {
        public static WorkshopContentDatabase CreateRuntimeDatabase()
        {
            return CreateModernRuntimeDatabase();
#if false
            var ember = CreateItem("ember_essence", "Ember Essence", "A volatile fire reagent distilled from bonded flame spirits.", WorkshopItemKind.Resource, new Color(0.9f, 0.43f, 0.2f));
            var mist = CreateItem("mist_essence", "Mist Essence", "A cold suspension used for cooling and sigil stabilization.", WorkshopItemKind.Resource, new Color(0.36f, 0.68f, 0.96f));
            var spellInk = CreateItem("spell_ink", "Spell Ink", "A processed reagent that captures and binds elemental instructions.", WorkshopItemKind.Resource, new Color(0.72f, 0.42f, 0.95f));
            var crystal = CreateItem("crystal_shard", "Crystal Shard", "A rigid catalyst required for defensive card structures.", WorkshopItemKind.Resource, new Color(0.74f, 0.95f, 1f));

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
#endif
        }

        private static WorkshopContentDatabase CreateModernRuntimeDatabase()
        {
            var fire = CreateItem("element.fire", "Fire", "Basic element produced by fire spirits.", WorkshopItemKind.Resource, new Color(0.95f, 0.36f, 0.2f));
            var water = CreateItem("element.water", "Water", "Basic element produced by water spirits.", WorkshopItemKind.Resource, new Color(0.26f, 0.62f, 0.97f));
            var wind = CreateItem("element.wind", "Wind", "Basic element produced by wind spirits.", WorkshopItemKind.Resource, new Color(0.57f, 0.84f, 0.9f));
            var earth = CreateItem("element.earth", "Earth", "Basic element produced by earth spirits.", WorkshopItemKind.Resource, new Color(0.63f, 0.47f, 0.28f));
            var ice = CreateItem("element.ice", "Ice", "Secondary element from Wind + Water.", WorkshopItemKind.Resource, new Color(0.67f, 0.9f, 1f));
            var thunder = CreateItem("element.thunder", "Thunder", "Secondary element from Wind + Fire.", WorkshopItemKind.Resource, new Color(0.95f, 0.88f, 0.3f));
            var light = CreateItem("element.light", "Light", "Secondary element from Earth + Fire.", WorkshopItemKind.Resource, new Color(1f, 0.95f, 0.69f));
            var dark = CreateItem("element.dark", "Dark", "Secondary element from Earth + Water.", WorkshopItemKind.Resource, new Color(0.45f, 0.4f, 0.62f));

            var basicFireSpell = CreateSpell("spell.basic.fire", "Cinder Dart", "Basic fire attack spell shaped from raw Fire.", new Color(0.95f, 0.43f, 0.2f), "combat.spell.basic.fire", WorkshopElementAttribute.Fire, WorkshopSpellTier.Basic, WorkshopSpellRole.Attack, 10f, 8, 1, 1f, "Burn");
            var basicWaterSpell = CreateSpell("spell.basic.water", "Tidal Mend", "Basic water healing spell shaped from raw Water.", new Color(0.33f, 0.68f, 1f), "combat.spell.basic.water", WorkshopElementAttribute.Water, WorkshopSpellTier.Basic, WorkshopSpellRole.Healing, 12f, 6, 1, 8f, "Regen");
            var basicWindSpell = CreateSpell("spell.basic.wind", "Zephyr Cut", "Basic wind attack spell that strikes in quick succession.", new Color(0.66f, 0.91f, 0.95f), "combat.spell.basic.wind", WorkshopElementAttribute.Wind, WorkshopSpellTier.Basic, WorkshopSpellRole.Attack, 11f, 5, 2, 10f, "Expose");
            var basicEarthSpell = CreateSpell("spell.basic.earth", "Stoneguard Sigil", "Basic earth defense spell that anchors the caster.", new Color(0.71f, 0.55f, 0.35f), "combat.spell.basic.earth", WorkshopElementAttribute.Earth, WorkshopSpellTier.Basic, WorkshopSpellRole.Defense, 11f, 7, 1, 18f, "Bulwark");
            var basicIceSpell = CreateSpell("spell.basic.ice", "Frost Pin", "Basic ice attack spell that slows the target.", new Color(0.72f, 0.92f, 1f), "combat.spell.basic.ice", WorkshopElementAttribute.Ice, WorkshopSpellTier.Basic, WorkshopSpellRole.Attack, 14f, 4, 2, 20f, "Slow");
            var basicThunderSpell = CreateSpell("spell.basic.thunder", "Volt Javelin", "Basic thunder attack spell with burst impact.", new Color(1f, 0.9f, 0.36f), "combat.spell.basic.thunder", WorkshopElementAttribute.Thunder, WorkshopSpellTier.Basic, WorkshopSpellRole.Attack, 14f, 7, 1, 15f, "Shock");
            var basicLightSpell = CreateSpell("spell.basic.light", "Lumen Prayer", "Basic light healing spell that restores through radiance.", new Color(1f, 0.98f, 0.74f), "combat.spell.basic.light", WorkshopElementAttribute.Light, WorkshopSpellTier.Basic, WorkshopSpellRole.Healing, 16f, 5, 2, 12f, "Bless");
            var basicDarkSpell = CreateSpell("spell.basic.dark", "Gloam Ward", "Basic dark defense spell that shrouds the caster.", new Color(0.56f, 0.5f, 0.74f), "combat.spell.basic.dark", WorkshopElementAttribute.Dark, WorkshopSpellTier.Basic, WorkshopSpellRole.Defense, 16f, 6, 1, 20f, "Veil");

            var intermediateFireSpell = CreateSpell("spell.intermediate.fire", "Inferno Brand", "Tier-2 fire spell forged from repeated flame shaping.", new Color(0.99f, 0.48f, 0.3f), "combat.spell.intermediate.fire", WorkshopElementAttribute.Fire, WorkshopSpellTier.Intermediate, WorkshopSpellRole.Attack, 28f, 14, 2, 2f, "Burn");
            var intermediateWaterSpell = CreateSpell("spell.intermediate.water", "Tide Chorus", "Tier-2 water spell that restores in rolling waves.", new Color(0.45f, 0.72f, 1f), "combat.spell.intermediate.water", WorkshopElementAttribute.Water, WorkshopSpellTier.Intermediate, WorkshopSpellRole.Healing, 30f, 11, 2, 14f, "Regen");
            var intermediateWindSpell = CreateSpell("spell.intermediate.wind", "Razor Monsoon", "Tier-2 wind spell that hits multiple times.", new Color(0.71f, 0.95f, 0.98f), "combat.spell.intermediate.wind", WorkshopElementAttribute.Wind, WorkshopSpellTier.Intermediate, WorkshopSpellRole.Attack, 28f, 8, 3, 12f, "Expose");
            var intermediateEarthSpell = CreateSpell("spell.intermediate.earth", "Bastion Pulse", "Tier-2 earth spell that reinforces defenses.", new Color(0.78f, 0.6f, 0.38f), "combat.spell.intermediate.earth", WorkshopElementAttribute.Earth, WorkshopSpellTier.Intermediate, WorkshopSpellRole.Defense, 28f, 12, 1, 28f, "Ward");
            var intermediateIceSpell = CreateSpell("spell.intermediate.ice", "Glacier Bind", "Tier-2 ice spell that freezes motion and tempo.", new Color(0.8f, 0.96f, 1f), "combat.spell.intermediate.ice", WorkshopElementAttribute.Ice, WorkshopSpellTier.Intermediate, WorkshopSpellRole.Attack, 32f, 9, 2, 24f, "Freeze");
            var intermediateThunderSpell = CreateSpell("spell.intermediate.thunder", "Stormbreaker", "Tier-2 thunder spell with heavy burst damage.", new Color(1f, 0.94f, 0.46f), "combat.spell.intermediate.thunder", WorkshopElementAttribute.Thunder, WorkshopSpellTier.Intermediate, WorkshopSpellRole.Attack, 32f, 16, 1, 18f, "Stun");
            var intermediateLightSpell = CreateSpell("spell.intermediate.light", "Dawn Benediction", "Tier-2 light spell that heals across multiple pulses.", new Color(1f, 0.99f, 0.81f), "combat.spell.intermediate.light", WorkshopElementAttribute.Light, WorkshopSpellTier.Intermediate, WorkshopSpellRole.Healing, 34f, 9, 3, 18f, "Radiance");
            var intermediateDarkSpell = CreateSpell("spell.intermediate.dark", "Umbral Bastion", "Tier-2 dark spell that turns shadow into protection.", new Color(0.63f, 0.57f, 0.8f), "combat.spell.intermediate.dark", WorkshopElementAttribute.Dark, WorkshopSpellTier.Intermediate, WorkshopSpellRole.Defense, 34f, 10, 2, 24f, "Shade");

            var advancedPrismSpell = CreateSpell("spell.advanced.prism", "Eclipse Covenant", "Tier-3 light-dark opposition spell that restores and stabilizes.", new Color(0.97f, 0.84f, 1f), "combat.spell.advanced.prism", WorkshopElementAttribute.Light, WorkshopSpellTier.Advanced, WorkshopSpellRole.Healing, 58f, 14, 3, 24f, "Radiance");
            var advancedTempestSpell = CreateSpell("spell.advanced.tempest", "Worldsplit Tempest", "Tier-3 wind-earth opposition spell that tears through defenses.", new Color(0.8f, 0.93f, 0.84f), "combat.spell.advanced.tempest", WorkshopElementAttribute.Wind, WorkshopSpellTier.Advanced, WorkshopSpellRole.Attack, 58f, 12, 3, 20f, "Rend");
            var advancedSteamSpell = CreateSpell("spell.advanced.steam", "Steam Requiem", "Tier-3 fire-water opposition spell that scalds in layered bursts.", new Color(0.89f, 0.78f, 0.72f), "combat.spell.advanced.steam", WorkshopElementAttribute.Fire, WorkshopSpellTier.Advanced, WorkshopSpellRole.Attack, 58f, 20, 2, 28f, "Scald");
            var advancedPolaritySpell = CreateSpell("spell.advanced.polarity", "Absolute Zero Surge", "Tier-3 ice-thunder opposition spell that locks down incoming damage.", new Color(0.86f, 0.92f, 1f), "combat.spell.advanced.polarity", WorkshopElementAttribute.Ice, WorkshopSpellTier.Advanced, WorkshopSpellRole.Defense, 58f, 16, 2, 35f, "Static Shell");

            var fireSpirit = CreateNodeWithSprite("node.spirit.fire", "Fire Spirit", "Spirit source that continuously generates Fire.", WorkshopNodeCategory.Source, true, new Color(0.83f, 0.3f, 0.2f), NodePortMask.None, NodePortMask.East, 10, 2, false, WorkshopProductionRecipe.Create("recipe.spirit.fire", "Generate Fire", 1f, Array.Empty<WorkshopItemStack>(), new[] { WorkshopItemStack.Create(fire, 1) }));
            var waterSpirit = CreateNodeWithSprite("node.spirit.water", "Water Spirit", "Spirit source that continuously generates Water.", WorkshopNodeCategory.Source, true, new Color(0.27f, 0.54f, 0.84f), NodePortMask.None, NodePortMask.East, 10, 2, false, WorkshopProductionRecipe.Create("recipe.spirit.water", "Generate Water", 1f, Array.Empty<WorkshopItemStack>(), new[] { WorkshopItemStack.Create(water, 1) }));
            var windSpirit = CreateNodeWithSprite("node.spirit.wind", "Wind Spirit", "Spirit source that continuously generates Wind.", WorkshopNodeCategory.Source, true, new Color(0.5f, 0.76f, 0.83f), NodePortMask.None, NodePortMask.East, 10, 2, false, WorkshopProductionRecipe.Create("recipe.spirit.wind", "Generate Wind", 1f, Array.Empty<WorkshopItemStack>(), new[] { WorkshopItemStack.Create(wind, 1) }));
            var earthSpirit = CreateNodeWithSprite("node.spirit.earth", "Earth Spirit", "Spirit source that continuously generates Earth.", WorkshopNodeCategory.Source, true, new Color(0.53f, 0.41f, 0.27f), NodePortMask.None, NodePortMask.East, 10, 2, false, WorkshopProductionRecipe.Create("recipe.spirit.earth", "Generate Earth", 1f, Array.Empty<WorkshopItemStack>(), new[] { WorkshopItemStack.Create(earth, 1) }));
            var iceSpirit = CreateNodeWithSprite("node.spirit.ice", "Ice Spirit", "Reward spirit source that continuously generates Ice.", WorkshopNodeCategory.Source, false, new Color(0.67f, 0.88f, 1f), NodePortMask.None, NodePortMask.East, 10, 2, false, WorkshopProductionRecipe.Create("recipe.spirit.ice", "Generate Ice", 1.15f, Array.Empty<WorkshopItemStack>(), new[] { WorkshopItemStack.Create(ice, 1) }));
            var thunderSpirit = CreateNodeWithSprite("node.spirit.thunder", "Thunder Spirit", "Reward spirit source that continuously generates Thunder.", WorkshopNodeCategory.Source, false, new Color(0.95f, 0.86f, 0.25f), NodePortMask.None, NodePortMask.East, 10, 2, false, WorkshopProductionRecipe.Create("recipe.spirit.thunder", "Generate Thunder", 1.15f, Array.Empty<WorkshopItemStack>(), new[] { WorkshopItemStack.Create(thunder, 1) }));
            var lightSpirit = CreateNodeWithSprite("node.spirit.light", "Light Spirit", "Reward spirit source that continuously generates Light.", WorkshopNodeCategory.Source, false, new Color(1f, 0.95f, 0.67f), NodePortMask.None, NodePortMask.East, 10, 2, false, WorkshopProductionRecipe.Create("recipe.spirit.light", "Generate Light", 1.15f, Array.Empty<WorkshopItemStack>(), new[] { WorkshopItemStack.Create(light, 1) }));
            var darkSpirit = CreateNodeWithSprite("node.spirit.dark", "Dark Spirit", "Reward spirit source that continuously generates Dark.", WorkshopNodeCategory.Source, false, new Color(0.44f, 0.38f, 0.62f), NodePortMask.None, NodePortMask.East, 10, 2, false, WorkshopProductionRecipe.Create("recipe.spirit.dark", "Generate Dark", 1.15f, Array.Empty<WorkshopItemStack>(), new[] { WorkshopItemStack.Create(dark, 1) }));

            var elementFusionFactory = CreateNodeWithSprite(
                "node.factory.element_fusion", "Element Fusion", "Combines non-opposing basic elements into secondary elements.", WorkshopNodeCategory.Processor, true, new Color(0.64f, 0.43f, 0.79f), NodePortMask.West | NodePortMask.South, NodePortMask.East, 14, 2, false,
                WorkshopProductionRecipe.Create("recipe.fusion.ice", "Fuse Ice", 1.8f, new[] { WorkshopItemStack.Create(wind, 1), WorkshopItemStack.Create(water, 1) }, new[] { WorkshopItemStack.Create(ice, 1) }),
                WorkshopProductionRecipe.Create("recipe.fusion.thunder", "Fuse Thunder", 1.8f, new[] { WorkshopItemStack.Create(wind, 1), WorkshopItemStack.Create(fire, 1) }, new[] { WorkshopItemStack.Create(thunder, 1) }),
                WorkshopProductionRecipe.Create("recipe.fusion.light", "Fuse Light", 1.8f, new[] { WorkshopItemStack.Create(earth, 1), WorkshopItemStack.Create(fire, 1) }, new[] { WorkshopItemStack.Create(light, 1) }),
                WorkshopProductionRecipe.Create("recipe.fusion.dark", "Fuse Dark", 1.8f, new[] { WorkshopItemStack.Create(earth, 1), WorkshopItemStack.Create(water, 1) }, new[] { WorkshopItemStack.Create(dark, 1) }));

            var elementShapingFactory = CreateNodeWithSprite(
                "node.factory.element_shaping", "Element Shaper", "Shapes one element into one basic spell card.", WorkshopNodeCategory.Crafter, true, new Color(0.95f, 0.62f, 0.25f), NodePortMask.West, NodePortMask.East, 12, 2, false,
                WorkshopProductionRecipe.Create("recipe.shape.fire", "Shape Fire Spell", 1.2f, new[] { WorkshopItemStack.Create(fire, 1) }, new[] { WorkshopItemStack.Create(basicFireSpell, 1) }),
                WorkshopProductionRecipe.Create("recipe.shape.water", "Shape Water Spell", 1.2f, new[] { WorkshopItemStack.Create(water, 1) }, new[] { WorkshopItemStack.Create(basicWaterSpell, 1) }),
                WorkshopProductionRecipe.Create("recipe.shape.wind", "Shape Wind Spell", 1.2f, new[] { WorkshopItemStack.Create(wind, 1) }, new[] { WorkshopItemStack.Create(basicWindSpell, 1) }),
                WorkshopProductionRecipe.Create("recipe.shape.earth", "Shape Earth Spell", 1.2f, new[] { WorkshopItemStack.Create(earth, 1) }, new[] { WorkshopItemStack.Create(basicEarthSpell, 1) }),
                WorkshopProductionRecipe.Create("recipe.shape.ice", "Shape Ice Spell", 1.2f, new[] { WorkshopItemStack.Create(ice, 1) }, new[] { WorkshopItemStack.Create(basicIceSpell, 1) }),
                WorkshopProductionRecipe.Create("recipe.shape.thunder", "Shape Thunder Spell", 1.2f, new[] { WorkshopItemStack.Create(thunder, 1) }, new[] { WorkshopItemStack.Create(basicThunderSpell, 1) }),
                WorkshopProductionRecipe.Create("recipe.shape.light", "Shape Light Spell", 1.2f, new[] { WorkshopItemStack.Create(light, 1) }, new[] { WorkshopItemStack.Create(basicLightSpell, 1) }),
                WorkshopProductionRecipe.Create("recipe.shape.dark", "Shape Dark Spell", 1.2f, new[] { WorkshopItemStack.Create(dark, 1) }, new[] { WorkshopItemStack.Create(basicDarkSpell, 1) }));

            var spellFusionBasicFactory = CreateNodeWithSprite(
                "node.factory.spell_fusion.basic", "Spell Fusion I", "Fuses two basic spells of the same element into intermediate spells.", WorkshopNodeCategory.Crafter, true, new Color(0.9f, 0.44f, 0.63f), NodePortMask.West | NodePortMask.South, NodePortMask.East, 12, 2, false,
                WorkshopProductionRecipe.Create("recipe.fusion.basic.fire", "Fuse Intermediate Fire", 2.2f, new[] { WorkshopItemStack.Create(basicFireSpell, 2) }, new[] { WorkshopItemStack.Create(intermediateFireSpell, 1) }),
                WorkshopProductionRecipe.Create("recipe.fusion.basic.water", "Fuse Intermediate Water", 2.2f, new[] { WorkshopItemStack.Create(basicWaterSpell, 2) }, new[] { WorkshopItemStack.Create(intermediateWaterSpell, 1) }),
                WorkshopProductionRecipe.Create("recipe.fusion.basic.wind", "Fuse Intermediate Wind", 2.2f, new[] { WorkshopItemStack.Create(basicWindSpell, 2) }, new[] { WorkshopItemStack.Create(intermediateWindSpell, 1) }),
                WorkshopProductionRecipe.Create("recipe.fusion.basic.earth", "Fuse Intermediate Earth", 2.2f, new[] { WorkshopItemStack.Create(basicEarthSpell, 2) }, new[] { WorkshopItemStack.Create(intermediateEarthSpell, 1) }),
                WorkshopProductionRecipe.Create("recipe.fusion.basic.ice", "Fuse Intermediate Ice", 2.2f, new[] { WorkshopItemStack.Create(basicIceSpell, 2) }, new[] { WorkshopItemStack.Create(intermediateIceSpell, 1) }),
                WorkshopProductionRecipe.Create("recipe.fusion.basic.thunder", "Fuse Intermediate Thunder", 2.2f, new[] { WorkshopItemStack.Create(basicThunderSpell, 2) }, new[] { WorkshopItemStack.Create(intermediateThunderSpell, 1) }),
                WorkshopProductionRecipe.Create("recipe.fusion.basic.light", "Fuse Intermediate Light", 2.2f, new[] { WorkshopItemStack.Create(basicLightSpell, 2) }, new[] { WorkshopItemStack.Create(intermediateLightSpell, 1) }),
                WorkshopProductionRecipe.Create("recipe.fusion.basic.dark", "Fuse Intermediate Dark", 2.2f, new[] { WorkshopItemStack.Create(basicDarkSpell, 2) }, new[] { WorkshopItemStack.Create(intermediateDarkSpell, 1) }));

            var spellFusionIntermediateFactory = CreateNodeWithSprite(
                "node.factory.spell_fusion.intermediate", "Spell Fusion II", "Fuses non-opposing basic spells into secondary intermediate spells.", WorkshopNodeCategory.Crafter, false, new Color(0.77f, 0.42f, 0.79f), NodePortMask.West | NodePortMask.South, NodePortMask.East, 12, 2, false,
                WorkshopProductionRecipe.Create("recipe.fusion.intermediate.ice", "Fuse Intermediate Ice", 2.4f, new[] { WorkshopItemStack.Create(basicWindSpell, 1), WorkshopItemStack.Create(basicWaterSpell, 1) }, new[] { WorkshopItemStack.Create(intermediateIceSpell, 1) }),
                WorkshopProductionRecipe.Create("recipe.fusion.intermediate.ice_alt_a", "Fuse Glacier Bind", 2.4f, new[] { WorkshopItemStack.Create(basicWaterSpell, 1), WorkshopItemStack.Create(basicIceSpell, 1) }, new[] { WorkshopItemStack.Create(intermediateIceSpell, 1) }),
                WorkshopProductionRecipe.Create("recipe.fusion.intermediate.ice_alt_b", "Fuse Glacier Bind", 2.4f, new[] { WorkshopItemStack.Create(basicWindSpell, 1), WorkshopItemStack.Create(basicIceSpell, 1) }, new[] { WorkshopItemStack.Create(intermediateIceSpell, 1) }),
                WorkshopProductionRecipe.Create("recipe.fusion.intermediate.thunder", "Fuse Intermediate Thunder", 2.4f, new[] { WorkshopItemStack.Create(basicWindSpell, 1), WorkshopItemStack.Create(basicFireSpell, 1) }, new[] { WorkshopItemStack.Create(intermediateThunderSpell, 1) }),
                WorkshopProductionRecipe.Create("recipe.fusion.intermediate.thunder_alt_a", "Fuse Stormbreaker", 2.4f, new[] { WorkshopItemStack.Create(basicFireSpell, 1), WorkshopItemStack.Create(basicThunderSpell, 1) }, new[] { WorkshopItemStack.Create(intermediateThunderSpell, 1) }),
                WorkshopProductionRecipe.Create("recipe.fusion.intermediate.thunder_alt_b", "Fuse Stormbreaker", 2.4f, new[] { WorkshopItemStack.Create(basicWindSpell, 1), WorkshopItemStack.Create(basicThunderSpell, 1) }, new[] { WorkshopItemStack.Create(intermediateThunderSpell, 1) }),
                WorkshopProductionRecipe.Create("recipe.fusion.intermediate.light", "Fuse Intermediate Light", 2.4f, new[] { WorkshopItemStack.Create(basicEarthSpell, 1), WorkshopItemStack.Create(basicFireSpell, 1) }, new[] { WorkshopItemStack.Create(intermediateLightSpell, 1) }),
                WorkshopProductionRecipe.Create("recipe.fusion.intermediate.light_alt_a", "Fuse Dawn Benediction", 2.4f, new[] { WorkshopItemStack.Create(basicFireSpell, 1), WorkshopItemStack.Create(basicLightSpell, 1) }, new[] { WorkshopItemStack.Create(intermediateLightSpell, 1) }),
                WorkshopProductionRecipe.Create("recipe.fusion.intermediate.light_alt_b", "Fuse Dawn Benediction", 2.4f, new[] { WorkshopItemStack.Create(basicEarthSpell, 1), WorkshopItemStack.Create(basicLightSpell, 1) }, new[] { WorkshopItemStack.Create(intermediateLightSpell, 1) }),
                WorkshopProductionRecipe.Create("recipe.fusion.intermediate.dark", "Fuse Intermediate Dark", 2.4f, new[] { WorkshopItemStack.Create(basicEarthSpell, 1), WorkshopItemStack.Create(basicWaterSpell, 1) }, new[] { WorkshopItemStack.Create(intermediateDarkSpell, 1) }),
                WorkshopProductionRecipe.Create("recipe.fusion.intermediate.dark_alt_a", "Fuse Umbral Bastion", 2.4f, new[] { WorkshopItemStack.Create(basicWaterSpell, 1), WorkshopItemStack.Create(basicDarkSpell, 1) }, new[] { WorkshopItemStack.Create(intermediateDarkSpell, 1) }),
                WorkshopProductionRecipe.Create("recipe.fusion.intermediate.dark_alt_b", "Fuse Umbral Bastion", 2.4f, new[] { WorkshopItemStack.Create(basicEarthSpell, 1), WorkshopItemStack.Create(basicDarkSpell, 1) }, new[] { WorkshopItemStack.Create(intermediateDarkSpell, 1) }));

            var spellFusionAdvancedFactory = CreateNodeWithSprite(
                "node.factory.spell_fusion.advanced", "Spell Fusion III", "Fuses opposing intermediate spells into advanced cards.", WorkshopNodeCategory.Crafter, false, new Color(0.59f, 0.31f, 0.72f), NodePortMask.West | NodePortMask.South, NodePortMask.None, 12, 0, false,
                WorkshopProductionRecipe.Create("recipe.fusion.advanced.steam", "Forge Advanced Steam", 3f, new[] { WorkshopItemStack.Create(intermediateFireSpell, 1), WorkshopItemStack.Create(intermediateWaterSpell, 1) }, new[] { WorkshopItemStack.Create(advancedSteamSpell, 1) }),
                WorkshopProductionRecipe.Create("recipe.fusion.advanced.tempest", "Forge Advanced Tempest", 3f, new[] { WorkshopItemStack.Create(intermediateWindSpell, 1), WorkshopItemStack.Create(intermediateEarthSpell, 1) }, new[] { WorkshopItemStack.Create(advancedTempestSpell, 1) }),
                WorkshopProductionRecipe.Create("recipe.fusion.advanced.prism", "Forge Advanced Prism", 3f, new[] { WorkshopItemStack.Create(intermediateLightSpell, 1), WorkshopItemStack.Create(intermediateDarkSpell, 1) }, new[] { WorkshopItemStack.Create(advancedPrismSpell, 1) }),
                WorkshopProductionRecipe.Create("recipe.fusion.advanced.polarity", "Forge Advanced Polarity", 3f, new[] { WorkshopItemStack.Create(intermediateIceSpell, 1), WorkshopItemStack.Create(intermediateThunderSpell, 1) }, new[] { WorkshopItemStack.Create(advancedPolaritySpell, 1) }));

            var conduit = CreateNodeWithSprite("node.factory.conduit", "Arcane Conduit", "Relay node that forwards element resources through the line.", WorkshopNodeCategory.Storage, true, new Color(0.37f, 0.39f, 0.43f), NodePortMask.West, NodePortMask.East, 12, 2, true, Array.Empty<WorkshopProductionRecipe>());
            var spellConduit = CreateNodeWithSprite("node.factory.spell_conduit", "Spell Conduit", "Relay node that forwards crafted spell cards into the battle deck.", WorkshopNodeCategory.Storage, true, new Color(0.58f, 0.42f, 0.78f), NodePortMask.West, NodePortMask.East, 12, 2, false, Array.Empty<WorkshopProductionRecipe>());
            var unlockSpellFusionBasic = CreateReward("reward.unlock.spell_fusion_basic", "Unlock Spell Fusion Basic", "Unlocks same-element spell fusion.", WorkshopRewardKind.UnlockNode, spellFusionBasicFactory, 0f, Array.Empty<WorkshopItemStack>());
            var unlockSpellFusionIntermediate = CreateReward("reward.unlock.spell_fusion_intermediate", "Unlock Spell Fusion Intermediate", "Unlocks non-opposing mixed spell fusion.", WorkshopRewardKind.UnlockNode, spellFusionIntermediateFactory, 0f, Array.Empty<WorkshopItemStack>());
            var unlockSpellFusionAdvanced = CreateReward("reward.unlock.spell_fusion_advanced", "Unlock Spell Fusion Advanced", "Unlocks opposing-element advanced fusion.", WorkshopRewardKind.UnlockNode, spellFusionAdvancedFactory, 0f, Array.Empty<WorkshopItemStack>());
            var unlockIceSpirit = CreateReward("reward.unlock.spirit.ice", "Unlock Ice Spirit Node", "Adds the Ice spirit node to the workshop palette.", WorkshopRewardKind.UnlockNode, iceSpirit, 0f, Array.Empty<WorkshopItemStack>());
            var unlockThunderSpirit = CreateReward("reward.unlock.spirit.thunder", "Unlock Thunder Spirit Node", "Adds the Thunder spirit node to the workshop palette.", WorkshopRewardKind.UnlockNode, thunderSpirit, 0f, Array.Empty<WorkshopItemStack>());
            var unlockLightSpirit = CreateReward("reward.unlock.spirit.light", "Unlock Light Spirit Node", "Adds the Light spirit node to the workshop palette.", WorkshopRewardKind.UnlockNode, lightSpirit, 0f, Array.Empty<WorkshopItemStack>());
            var unlockDarkSpirit = CreateReward("reward.unlock.spirit.dark", "Unlock Dark Spirit Node", "Adds the Dark spirit node to the workshop palette.", WorkshopRewardKind.UnlockNode, darkSpirit, 0f, Array.Empty<WorkshopItemStack>());
            var boostShaping = CreateReward("reward.boost.shaping", "Shaping Factory Overclock", "Applies +20% speed to Element Shaping Factories.", WorkshopRewardKind.EfficiencyBoost, elementShapingFactory, 0.2f, Array.Empty<WorkshopItemStack>());
            var reserveReward = CreateReward("reward.resources.recovery", "Emergency Element Cache", "Adds a small reserve of all basic elements.", WorkshopRewardKind.GrantItems, null, 0f, new[] { WorkshopItemStack.Create(fire, 3), WorkshopItemStack.Create(water, 3), WorkshopItemStack.Create(wind, 3), WorkshopItemStack.Create(earth, 3) });

            var database = ScriptableObject.CreateInstance<WorkshopContentDatabase>();
            database.hideFlags = HideFlags.HideAndDontSave;
            database.Configure(
                new Vector2Int(9, 6),
                0.25f,
                new[] { fireSpirit, waterSpirit, windSpirit, earthSpirit, iceSpirit, thunderSpirit, lightSpirit, darkSpirit, elementFusionFactory, elementShapingFactory, spellFusionBasicFactory, spellFusionIntermediateFactory, spellFusionAdvancedFactory, conduit, spellConduit },
                new[] { unlockSpellFusionBasic, unlockSpellFusionIntermediate, unlockSpellFusionAdvanced, unlockIceSpirit, unlockThunderSpirit, unlockLightSpirit, unlockDarkSpirit, boostShaping, reserveReward },
                new[]
                {
                    WorkshopPlacedNodeSeed.Create(fireSpirit, new Vector2Int(0, 5), 0),
                    WorkshopPlacedNodeSeed.Create(conduit, new Vector2Int(1, 5), 0),
                    WorkshopPlacedNodeSeed.Create(elementShapingFactory, new Vector2Int(2, 5), 0),
                    WorkshopPlacedNodeSeed.Create(spellFusionBasicFactory, new Vector2Int(3, 5), 0),
                    WorkshopPlacedNodeSeed.Create(spellConduit, new Vector2Int(4, 5), 0),
                    WorkshopPlacedNodeSeed.Create(fireSpirit, new Vector2Int(3, 2), 3),
                    WorkshopPlacedNodeSeed.Create(conduit, new Vector2Int(3, 3), 3),
                    WorkshopPlacedNodeSeed.Create(elementShapingFactory, new Vector2Int(3, 4), 3),
                    WorkshopPlacedNodeSeed.Create(waterSpirit, new Vector2Int(0, 1), 0),
                    WorkshopPlacedNodeSeed.Create(conduit, new Vector2Int(1, 1), 0),
                    WorkshopPlacedNodeSeed.Create(conduit, new Vector2Int(2, 1), 0),
                    WorkshopPlacedNodeSeed.Create(conduit, new Vector2Int(3, 1), 0),
                    WorkshopPlacedNodeSeed.Create(elementFusionFactory, new Vector2Int(4, 1), 0),
                    WorkshopPlacedNodeSeed.Create(elementShapingFactory, new Vector2Int(5, 1), 0),
                    WorkshopPlacedNodeSeed.Create(windSpirit, new Vector2Int(4, 0), 3),
                });
            return database;
        }

        private static WorkshopItemDefinition CreateSpell(string id, string displayName, string description, Color tint, string battleCardId, WorkshopElementAttribute element, WorkshopSpellTier tier, WorkshopSpellRole role, float rarityWeight, int primaryValue, int hitCount, float secondaryValue, string effectKeyword)
        {
            var item = ScriptableObject.CreateInstance<WorkshopItemDefinition>();
            item.hideFlags = HideFlags.HideAndDontSave;
            item.Configure(id, displayName, description, WorkshopItemKind.Card, tint, battleCardId, element, tier, role, rarityWeight, primaryValue, hitCount, secondaryValue, effectKeyword);
            return item;
        }

        private static WorkshopNodeDefinition CreateNodeWithSprite(string id, string displayName, string description, WorkshopNodeCategory category, bool unlockedByDefault, Color tint, NodePortMask inputPorts, NodePortMask outputPorts, int bufferCapacity, int maxTransferPerStep, bool acceptsAnyResource, params WorkshopProductionRecipe[] recipes)
        {
            var node = CreateNode(id, displayName, description, category, unlockedByDefault, tint, inputPorts, outputPorts, bufferCapacity, maxTransferPerStep, acceptsAnyResource, recipes);
            node.NodeSprite = TryLoadNodeSprite(id);
            return node;
        }

        private static Sprite TryLoadNodeSprite(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            var parts = id.Split('.');
            var subdir = parts.Length >= 2 && parts[1] == "factory" ? "Factories" : "Spirits";
            var filePath = Path.Combine(Application.dataPath, "ArcaneAtelier", "Art", "Nodes", subdir, $"{SanitizeAssetName(id)}.png");
            if (!File.Exists(filePath))
            {
                return null;
            }

            var bytes = File.ReadAllBytes(filePath);
            if (bytes.Length == 0)
            {
                return null;
            }

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                name = $"{SanitizeAssetName(id)}_runtime",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            if (!texture.LoadImage(bytes))
            {
                UnityEngine.Object.Destroy(texture);
                return null;
            }

            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }

        private static string SanitizeAssetName(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return "generated_asset";
            }

            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new string(id.Select(ch =>
                invalidChars.Contains(ch)
                    ? '_'
                    : ch switch
                    {
                        '.' => '_',
                        '/' => '_',
                        '\\' => '_',
                        ':' => '_',
                        ' ' => '_',
                        _ => ch
                    }).ToArray());

            return sanitized.Trim('_');
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
