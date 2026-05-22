using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ArcaneAtelier.Workshop.Editor
{
    public static class WorkshopProjectBootstrap
    {
        private const string GeneratedRoot = "Assets/ArcaneAtelier/Workshop/Generated";
        private const string DataRoot = GeneratedRoot + "/Data";
        private const string ScenePath = "Assets/Scenes/WorkshopScene.unity";
        private const string DatabaseAssetPath = DataRoot + "/WorkshopContentDatabase.asset";

        static WorkshopProjectBootstrap()
        {
            EditorApplication.delayCall += EnsureGeneratedOnLoad;
        }

        [MenuItem("Arcane Atelier/Workshop/Rebuild Spell Assembly Content")]
        public static void RunFromMenu()
        {
            Run();
        }

        public static void RunFromBatchMode()
        {
            Run();
        }

        private static void EnsureGeneratedOnLoad()
        {
            if (Application.isBatchMode)
            {
                return;
            }

            var sceneExists = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null;
            var databaseExists = AssetDatabase.LoadAssetAtPath<WorkshopContentDatabase>(DatabaseAssetPath) != null;
            if (!sceneExists || !databaseExists || GeneratedContentNeedsSpriteRefresh())
            {
                Run();
            }
        }

        private static bool GeneratedContentNeedsSpriteRefresh()
        {
            var nodeGuids = AssetDatabase.FindAssets("t:WorkshopNodeDefinition", new[] { $"{DataRoot}/Nodes" });
            if (nodeGuids == null || nodeGuids.Length == 0)
            {
                return true;
            }

            foreach (var guid in nodeGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var node = AssetDatabase.LoadAssetAtPath<WorkshopNodeDefinition>(path);
                if (node == null)
                {
                    continue;
                }

                var expectedSprite = FindNodeSprite(node.Id);
                if (expectedSprite == null)
                {
                    continue;
                }

                if (node.NodeSprite != expectedSprite)
                {
                    return true;
                }
            }

            return false;
        }

        private static void Run()
        {
            EnsureFolder("Assets/Scenes");
            EnsureFolder(GeneratedRoot);
            EnsureFolder(DataRoot);
            EnsureFolder($"{DataRoot}/Items");
            EnsureFolder($"{DataRoot}/Nodes");
            EnsureFolder($"{DataRoot}/Rewards");

            var fire = UpsertItem("element.fire", "Fire", "Basic element produced by fire spirits.", WorkshopItemKind.Resource, new Color(0.95f, 0.36f, 0.2f));
            var water = UpsertItem("element.water", "Water", "Basic element produced by water spirits.", WorkshopItemKind.Resource, new Color(0.26f, 0.62f, 0.97f));
            var wind = UpsertItem("element.wind", "Wind", "Basic element produced by wind spirits.", WorkshopItemKind.Resource, new Color(0.57f, 0.84f, 0.9f));
            var earth = UpsertItem("element.earth", "Earth", "Basic element produced by earth spirits.", WorkshopItemKind.Resource, new Color(0.63f, 0.47f, 0.28f));

            var ice = UpsertItem("element.ice", "Ice", "Secondary element from Wind + Water.", WorkshopItemKind.Resource, new Color(0.67f, 0.9f, 1f));
            var thunder = UpsertItem("element.thunder", "Thunder", "Secondary element from Wind + Fire.", WorkshopItemKind.Resource, new Color(0.95f, 0.88f, 0.3f));
            var light = UpsertItem("element.light", "Light", "Secondary element from Earth + Fire.", WorkshopItemKind.Resource, new Color(1f, 0.95f, 0.69f));
            var dark = UpsertItem("element.dark", "Dark", "Secondary element from Earth + Water.", WorkshopItemKind.Resource, new Color(0.45f, 0.4f, 0.62f));

            var basicFireSpell = UpsertItem("spell.basic.fire", "Cinder Dart", "Basic fire attack spell shaped from raw Fire.", WorkshopItemKind.Card, new Color(0.95f, 0.43f, 0.2f), "combat.spell.basic.fire", WorkshopElementAttribute.Fire, WorkshopSpellTier.Basic, WorkshopSpellRole.Attack, 10f, 8, 1, 1f, "Burn");
            var basicWaterSpell = UpsertItem("spell.basic.water", "Tidal Mend", "Basic water healing spell shaped from raw Water.", WorkshopItemKind.Card, new Color(0.33f, 0.68f, 1f), "combat.spell.basic.water", WorkshopElementAttribute.Water, WorkshopSpellTier.Basic, WorkshopSpellRole.Healing, 12f, 6, 1, 8f, "Regen");
            var basicWindSpell = UpsertItem("spell.basic.wind", "Zephyr Cut", "Basic wind attack spell that strikes in quick succession.", WorkshopItemKind.Card, new Color(0.66f, 0.91f, 0.95f), "combat.spell.basic.wind", WorkshopElementAttribute.Wind, WorkshopSpellTier.Basic, WorkshopSpellRole.Attack, 11f, 5, 2, 10f, "Expose");
            var basicEarthSpell = UpsertItem("spell.basic.earth", "Stoneguard Sigil", "Basic earth defense spell that anchors the caster.", WorkshopItemKind.Card, new Color(0.71f, 0.55f, 0.35f), "combat.spell.basic.earth", WorkshopElementAttribute.Earth, WorkshopSpellTier.Basic, WorkshopSpellRole.Defense, 11f, 7, 1, 18f, "Bulwark");
            var basicIceSpell = UpsertItem("spell.basic.ice", "Frost Pin", "Basic ice attack spell that slows the target.", WorkshopItemKind.Card, new Color(0.72f, 0.92f, 1f), "combat.spell.basic.ice", WorkshopElementAttribute.Ice, WorkshopSpellTier.Basic, WorkshopSpellRole.Attack, 14f, 4, 2, 20f, "Slow");
            var basicThunderSpell = UpsertItem("spell.basic.thunder", "Volt Javelin", "Basic thunder attack spell with burst impact.", WorkshopItemKind.Card, new Color(1f, 0.9f, 0.36f), "combat.spell.basic.thunder", WorkshopElementAttribute.Thunder, WorkshopSpellTier.Basic, WorkshopSpellRole.Attack, 14f, 7, 1, 15f, "Shock");
            var basicLightSpell = UpsertItem("spell.basic.light", "Lumen Prayer", "Basic light healing spell that restores through radiance.", WorkshopItemKind.Card, new Color(1f, 0.98f, 0.74f), "combat.spell.basic.light", WorkshopElementAttribute.Light, WorkshopSpellTier.Basic, WorkshopSpellRole.Healing, 16f, 5, 2, 12f, "Bless");
            var basicDarkSpell = UpsertItem("spell.basic.dark", "Gloam Ward", "Basic dark defense spell that shrouds the caster.", WorkshopItemKind.Card, new Color(0.56f, 0.5f, 0.74f), "combat.spell.basic.dark", WorkshopElementAttribute.Dark, WorkshopSpellTier.Basic, WorkshopSpellRole.Defense, 16f, 6, 1, 20f, "Veil");

            var intermediateFireSpell = UpsertItem("spell.intermediate.fire", "Inferno Brand", "Tier-2 fire spell forged from repeated flame shaping.", WorkshopItemKind.Card, new Color(0.99f, 0.48f, 0.3f), "combat.spell.intermediate.fire", WorkshopElementAttribute.Fire, WorkshopSpellTier.Intermediate, WorkshopSpellRole.Attack, 28f, 14, 2, 2f, "Burn");
            var intermediateWaterSpell = UpsertItem("spell.intermediate.water", "Tide Chorus", "Tier-2 water spell that restores in rolling waves.", WorkshopItemKind.Card, new Color(0.45f, 0.72f, 1f), "combat.spell.intermediate.water", WorkshopElementAttribute.Water, WorkshopSpellTier.Intermediate, WorkshopSpellRole.Healing, 30f, 11, 2, 14f, "Regen");
            var intermediateWindSpell = UpsertItem("spell.intermediate.wind", "Razor Monsoon", "Tier-2 wind spell that hits multiple times.", WorkshopItemKind.Card, new Color(0.71f, 0.95f, 0.98f), "combat.spell.intermediate.wind", WorkshopElementAttribute.Wind, WorkshopSpellTier.Intermediate, WorkshopSpellRole.Attack, 28f, 8, 3, 12f, "Expose");
            var intermediateEarthSpell = UpsertItem("spell.intermediate.earth", "Bastion Pulse", "Tier-2 earth spell that reinforces defenses.", WorkshopItemKind.Card, new Color(0.78f, 0.6f, 0.38f), "combat.spell.intermediate.earth", WorkshopElementAttribute.Earth, WorkshopSpellTier.Intermediate, WorkshopSpellRole.Defense, 28f, 12, 1, 28f, "Ward");
            var intermediateIceSpell = UpsertItem("spell.intermediate.ice", "Glacier Bind", "Tier-2 ice spell that freezes motion and tempo.", WorkshopItemKind.Card, new Color(0.8f, 0.96f, 1f), "combat.spell.intermediate.ice", WorkshopElementAttribute.Ice, WorkshopSpellTier.Intermediate, WorkshopSpellRole.Attack, 32f, 9, 2, 24f, "Freeze");
            var intermediateThunderSpell = UpsertItem("spell.intermediate.thunder", "Stormbreaker", "Tier-2 thunder spell with heavy burst damage.", WorkshopItemKind.Card, new Color(1f, 0.94f, 0.46f), "combat.spell.intermediate.thunder", WorkshopElementAttribute.Thunder, WorkshopSpellTier.Intermediate, WorkshopSpellRole.Attack, 32f, 16, 1, 18f, "Stun");
            var intermediateLightSpell = UpsertItem("spell.intermediate.light", "Dawn Benediction", "Tier-2 light spell that heals across multiple pulses.", WorkshopItemKind.Card, new Color(1f, 0.99f, 0.81f), "combat.spell.intermediate.light", WorkshopElementAttribute.Light, WorkshopSpellTier.Intermediate, WorkshopSpellRole.Healing, 34f, 9, 3, 18f, "Radiance");
            var intermediateDarkSpell = UpsertItem("spell.intermediate.dark", "Umbral Bastion", "Tier-2 dark spell that turns shadow into protection.", WorkshopItemKind.Card, new Color(0.63f, 0.57f, 0.8f), "combat.spell.intermediate.dark", WorkshopElementAttribute.Dark, WorkshopSpellTier.Intermediate, WorkshopSpellRole.Defense, 34f, 10, 2, 24f, "Shade");

            var advancedPrismSpell = UpsertItem("spell.advanced.prism", "Eclipse Covenant", "Tier-3 light-dark opposition spell that restores and stabilizes.", WorkshopItemKind.Card, new Color(0.97f, 0.84f, 1f), "combat.spell.advanced.prism", WorkshopElementAttribute.Light, WorkshopSpellTier.Advanced, WorkshopSpellRole.Healing, 58f, 14, 3, 24f, "Radiance");
            var advancedTempestSpell = UpsertItem("spell.advanced.tempest", "Worldsplit Tempest", "Tier-3 wind-earth opposition spell that tears through defenses.", WorkshopItemKind.Card, new Color(0.8f, 0.93f, 0.84f), "combat.spell.advanced.tempest", WorkshopElementAttribute.Wind, WorkshopSpellTier.Advanced, WorkshopSpellRole.Attack, 58f, 12, 3, 20f, "Rend");
            var advancedSteamSpell = UpsertItem("spell.advanced.steam", "Steam Requiem", "Tier-3 fire-water opposition spell that scalds in layered bursts.", WorkshopItemKind.Card, new Color(0.89f, 0.78f, 0.72f), "combat.spell.advanced.steam", WorkshopElementAttribute.Fire, WorkshopSpellTier.Advanced, WorkshopSpellRole.Attack, 58f, 20, 2, 28f, "Scald");
            var advancedPolaritySpell = UpsertItem("spell.advanced.polarity", "Absolute Zero Surge", "Tier-3 ice-thunder opposition spell that locks down incoming damage.", WorkshopItemKind.Card, new Color(0.86f, 0.92f, 1f), "combat.spell.advanced.polarity", WorkshopElementAttribute.Ice, WorkshopSpellTier.Advanced, WorkshopSpellRole.Defense, 58f, 16, 2, 35f, "Static Shell");
            var ultimateSteamSpell = UpsertItem("spell.ultimate.steam", "Boiling Star Requiem", "Final steam spell stabilized by repeated advanced fusion.", WorkshopItemKind.Card, new Color(1f, 0.72f, 0.55f), "combat.spell.ultimate.steam", WorkshopElementAttribute.Fire, WorkshopSpellTier.Advanced, WorkshopSpellRole.Attack, 90f, 30, 2, 36f, "Scald");
            var ultimateTempestSpell = UpsertItem("spell.ultimate.tempest", "Heavenbreaker Tempest", "Final tempest spell that turns pressure into a cutting storm.", WorkshopItemKind.Card, new Color(0.7f, 1f, 0.82f), "combat.spell.ultimate.tempest", WorkshopElementAttribute.Wind, WorkshopSpellTier.Advanced, WorkshopSpellRole.Attack, 90f, 18, 4, 28f, "Rend");
            var ultimatePrismSpell = UpsertItem("spell.ultimate.prism", "Eclipse Apotheosis", "Final prism spell that converts opposition into radiant recovery.", WorkshopItemKind.Card, new Color(1f, 0.88f, 1f), "combat.spell.ultimate.prism", WorkshopElementAttribute.Light, WorkshopSpellTier.Advanced, WorkshopSpellRole.Healing, 90f, 20, 4, 32f, "Radiance");
            var ultimatePolaritySpell = UpsertItem("spell.ultimate.polarity", "Zero Point Citadel", "Final polarity spell that compresses lightning and ice into a ward.", WorkshopItemKind.Card, new Color(0.75f, 0.88f, 1f), "combat.spell.ultimate.polarity", WorkshopElementAttribute.Ice, WorkshopSpellTier.Advanced, WorkshopSpellRole.Defense, 90f, 24, 3, 42f, "Static Shell");

            var fireSpirit = UpsertNode(
                "node.spirit.fire",
                "Fire Spirit",
                "Spirit source that continuously generates Fire.",
                WorkshopNodeCategory.Source,
                true,
                new Color(0.83f, 0.3f, 0.2f),
                NodePortMask.None,
                NodePortMask.East,
                10,
                2,
                false,
                WorkshopProductionRecipe.Create("recipe.spirit.fire", "Generate Fire", 1f, Array.Empty<WorkshopItemStack>(), new[] { WorkshopItemStack.Create(fire, 1) }));

            var waterSpirit = UpsertNode(
                "node.spirit.water",
                "Water Spirit",
                "Spirit source that continuously generates Water.",
                WorkshopNodeCategory.Source,
                true,
                new Color(0.27f, 0.54f, 0.84f),
                NodePortMask.None,
                NodePortMask.East,
                10,
                2,
                false,
                WorkshopProductionRecipe.Create("recipe.spirit.water", "Generate Water", 1f, Array.Empty<WorkshopItemStack>(), new[] { WorkshopItemStack.Create(water, 1) }));

            var windSpirit = UpsertNode(
                "node.spirit.wind",
                "Wind Spirit",
                "Spirit source that continuously generates Wind.",
                WorkshopNodeCategory.Source,
                true,
                new Color(0.5f, 0.76f, 0.83f),
                NodePortMask.None,
                NodePortMask.East,
                10,
                2,
                false,
                WorkshopProductionRecipe.Create("recipe.spirit.wind", "Generate Wind", 1f, Array.Empty<WorkshopItemStack>(), new[] { WorkshopItemStack.Create(wind, 1) }));

            var earthSpirit = UpsertNode(
                "node.spirit.earth",
                "Earth Spirit",
                "Spirit source that continuously generates Earth.",
                WorkshopNodeCategory.Source,
                true,
                new Color(0.53f, 0.41f, 0.27f),
                NodePortMask.None,
                NodePortMask.East,
                10,
                2,
                false,
                WorkshopProductionRecipe.Create("recipe.spirit.earth", "Generate Earth", 1f, Array.Empty<WorkshopItemStack>(), new[] { WorkshopItemStack.Create(earth, 1) }));

            var iceSpirit = UpsertNode(
                "node.spirit.ice",
                "Ice Spirit",
                "Reward spirit source that continuously generates Ice.",
                WorkshopNodeCategory.Source,
                false,
                new Color(0.67f, 0.88f, 1f),
                NodePortMask.None,
                NodePortMask.East,
                10,
                2,
                false,
                WorkshopProductionRecipe.Create("recipe.spirit.ice", "Generate Ice", 1.15f, Array.Empty<WorkshopItemStack>(), new[] { WorkshopItemStack.Create(ice, 1) }));

            var thunderSpirit = UpsertNode(
                "node.spirit.thunder",
                "Thunder Spirit",
                "Reward spirit source that continuously generates Thunder.",
                WorkshopNodeCategory.Source,
                false,
                new Color(0.95f, 0.86f, 0.25f),
                NodePortMask.None,
                NodePortMask.East,
                10,
                2,
                false,
                WorkshopProductionRecipe.Create("recipe.spirit.thunder", "Generate Thunder", 1.15f, Array.Empty<WorkshopItemStack>(), new[] { WorkshopItemStack.Create(thunder, 1) }));

            var lightSpirit = UpsertNode(
                "node.spirit.light",
                "Light Spirit",
                "Reward spirit source that continuously generates Light.",
                WorkshopNodeCategory.Source,
                false,
                new Color(1f, 0.95f, 0.67f),
                NodePortMask.None,
                NodePortMask.East,
                10,
                2,
                false,
                WorkshopProductionRecipe.Create("recipe.spirit.light", "Generate Light", 1.15f, Array.Empty<WorkshopItemStack>(), new[] { WorkshopItemStack.Create(light, 1) }));

            var darkSpirit = UpsertNode(
                "node.spirit.dark",
                "Dark Spirit",
                "Reward spirit source that continuously generates Dark.",
                WorkshopNodeCategory.Source,
                false,
                new Color(0.44f, 0.38f, 0.62f),
                NodePortMask.None,
                NodePortMask.East,
                10,
                2,
                false,
                WorkshopProductionRecipe.Create("recipe.spirit.dark", "Generate Dark", 1.15f, Array.Empty<WorkshopItemStack>(), new[] { WorkshopItemStack.Create(dark, 1) }));

            var elementFusionFactory = UpsertNode(
                "node.factory.element_fusion",
                "Element Fusion",
                "Combines non-opposing basic elements into secondary elements.",
                WorkshopNodeCategory.Processor,
                true,
                new Color(0.64f, 0.43f, 0.79f),
                NodePortMask.West | NodePortMask.South,
                NodePortMask.East,
                14,
                2,
                false,
                WorkshopProductionRecipe.Create("recipe.fusion.ice", "Fuse Ice", 1.8f, new[] { WorkshopItemStack.Create(wind, 1), WorkshopItemStack.Create(water, 1) }, new[] { WorkshopItemStack.Create(ice, 1) }),
                WorkshopProductionRecipe.Create("recipe.fusion.thunder", "Fuse Thunder", 1.8f, new[] { WorkshopItemStack.Create(wind, 1), WorkshopItemStack.Create(fire, 1) }, new[] { WorkshopItemStack.Create(thunder, 1) }),
                WorkshopProductionRecipe.Create("recipe.fusion.light", "Fuse Light", 1.8f, new[] { WorkshopItemStack.Create(earth, 1), WorkshopItemStack.Create(fire, 1) }, new[] { WorkshopItemStack.Create(light, 1) }),
                WorkshopProductionRecipe.Create("recipe.fusion.dark", "Fuse Dark", 1.8f, new[] { WorkshopItemStack.Create(earth, 1), WorkshopItemStack.Create(water, 1) }, new[] { WorkshopItemStack.Create(dark, 1) }));

            var elementShapingFactory = UpsertNode(
                "node.factory.element_shaping",
                "Element Shaper",
                "Shapes one element into one basic spell card.",
                WorkshopNodeCategory.Crafter,
                true,
                new Color(0.95f, 0.62f, 0.25f),
                NodePortMask.West,
                NodePortMask.East,
                12,
                2,
                false,
                WorkshopProductionRecipe.Create("recipe.shape.fire", "Shape Fire Spell", 1.2f, new[] { WorkshopItemStack.Create(fire, 1) }, new[] { WorkshopItemStack.Create(basicFireSpell, 1) }),
                WorkshopProductionRecipe.Create("recipe.shape.water", "Shape Water Spell", 1.2f, new[] { WorkshopItemStack.Create(water, 1) }, new[] { WorkshopItemStack.Create(basicWaterSpell, 1) }),
                WorkshopProductionRecipe.Create("recipe.shape.wind", "Shape Wind Spell", 1.2f, new[] { WorkshopItemStack.Create(wind, 1) }, new[] { WorkshopItemStack.Create(basicWindSpell, 1) }),
                WorkshopProductionRecipe.Create("recipe.shape.earth", "Shape Earth Spell", 1.2f, new[] { WorkshopItemStack.Create(earth, 1) }, new[] { WorkshopItemStack.Create(basicEarthSpell, 1) }),
                WorkshopProductionRecipe.Create("recipe.shape.ice", "Shape Ice Spell", 1.2f, new[] { WorkshopItemStack.Create(ice, 1) }, new[] { WorkshopItemStack.Create(basicIceSpell, 1) }),
                WorkshopProductionRecipe.Create("recipe.shape.thunder", "Shape Thunder Spell", 1.2f, new[] { WorkshopItemStack.Create(thunder, 1) }, new[] { WorkshopItemStack.Create(basicThunderSpell, 1) }),
                WorkshopProductionRecipe.Create("recipe.shape.light", "Shape Light Spell", 1.2f, new[] { WorkshopItemStack.Create(light, 1) }, new[] { WorkshopItemStack.Create(basicLightSpell, 1) }),
                WorkshopProductionRecipe.Create("recipe.shape.dark", "Shape Dark Spell", 1.2f, new[] { WorkshopItemStack.Create(dark, 1) }, new[] { WorkshopItemStack.Create(basicDarkSpell, 1) }));

            var spellFusionBasicFactory = UpsertNode(
                "node.factory.spell_fusion.basic",
                "Spell Fusion I",
                "Fuses two basic spells of the same element into intermediate spells.",
                WorkshopNodeCategory.Crafter,
                true,
                new Color(0.9f, 0.44f, 0.63f),
                NodePortMask.West | NodePortMask.South,
                NodePortMask.East,
                12,
                2,
                false,
                WorkshopProductionRecipe.Create("recipe.fusion.basic.fire", "Fuse Intermediate Fire", 2.2f, new[] { WorkshopItemStack.Create(basicFireSpell, 2) }, new[] { WorkshopItemStack.Create(intermediateFireSpell, 1) }),
                WorkshopProductionRecipe.Create("recipe.fusion.basic.water", "Fuse Intermediate Water", 2.2f, new[] { WorkshopItemStack.Create(basicWaterSpell, 2) }, new[] { WorkshopItemStack.Create(intermediateWaterSpell, 1) }),
                WorkshopProductionRecipe.Create("recipe.fusion.basic.wind", "Fuse Intermediate Wind", 2.2f, new[] { WorkshopItemStack.Create(basicWindSpell, 2) }, new[] { WorkshopItemStack.Create(intermediateWindSpell, 1) }),
                WorkshopProductionRecipe.Create("recipe.fusion.basic.earth", "Fuse Intermediate Earth", 2.2f, new[] { WorkshopItemStack.Create(basicEarthSpell, 2) }, new[] { WorkshopItemStack.Create(intermediateEarthSpell, 1) }),
                WorkshopProductionRecipe.Create("recipe.fusion.basic.ice", "Fuse Intermediate Ice", 2.2f, new[] { WorkshopItemStack.Create(basicIceSpell, 2) }, new[] { WorkshopItemStack.Create(intermediateIceSpell, 1) }),
                WorkshopProductionRecipe.Create("recipe.fusion.basic.thunder", "Fuse Intermediate Thunder", 2.2f, new[] { WorkshopItemStack.Create(basicThunderSpell, 2) }, new[] { WorkshopItemStack.Create(intermediateThunderSpell, 1) }),
                WorkshopProductionRecipe.Create("recipe.fusion.basic.light", "Fuse Intermediate Light", 2.2f, new[] { WorkshopItemStack.Create(basicLightSpell, 2) }, new[] { WorkshopItemStack.Create(intermediateLightSpell, 1) }),
                WorkshopProductionRecipe.Create("recipe.fusion.basic.dark", "Fuse Intermediate Dark", 2.2f, new[] { WorkshopItemStack.Create(basicDarkSpell, 2) }, new[] { WorkshopItemStack.Create(intermediateDarkSpell, 1) }));

            var spellFusionIntermediateFactory = UpsertNode(
                "node.factory.spell_fusion.intermediate",
                "Spell Fusion II",
                "Fuses Spell Fusion I outputs into advanced spells.",
                WorkshopNodeCategory.Crafter,
                false,
                new Color(0.77f, 0.42f, 0.79f),
                NodePortMask.West | NodePortMask.South,
                NodePortMask.East,
                12,
                2,
                false,
                WorkshopProductionRecipe.Create("recipe.fusion.intermediate.steam", "Fuse Advanced Steam", 2.6f, new[] { WorkshopItemStack.Create(intermediateFireSpell, 1), WorkshopItemStack.Create(intermediateWaterSpell, 1) }, new[] { WorkshopItemStack.Create(advancedSteamSpell, 1) }),
                WorkshopProductionRecipe.Create("recipe.fusion.intermediate.tempest", "Fuse Advanced Tempest", 2.6f, new[] { WorkshopItemStack.Create(intermediateWindSpell, 1), WorkshopItemStack.Create(intermediateEarthSpell, 1) }, new[] { WorkshopItemStack.Create(advancedTempestSpell, 1) }),
                WorkshopProductionRecipe.Create("recipe.fusion.intermediate.prism", "Fuse Advanced Prism", 2.6f, new[] { WorkshopItemStack.Create(intermediateLightSpell, 1), WorkshopItemStack.Create(intermediateDarkSpell, 1) }, new[] { WorkshopItemStack.Create(advancedPrismSpell, 1) }),
                WorkshopProductionRecipe.Create("recipe.fusion.intermediate.polarity", "Fuse Advanced Polarity", 2.6f, new[] { WorkshopItemStack.Create(intermediateIceSpell, 1), WorkshopItemStack.Create(intermediateThunderSpell, 1) }, new[] { WorkshopItemStack.Create(advancedPolaritySpell, 1) }));

            var spellFusionAdvancedFactory = UpsertNode(
                "node.factory.spell_fusion.advanced",
                "Spell Fusion III",
                "Fuses Spell Fusion II outputs into final advanced cards.",
                WorkshopNodeCategory.Crafter,
                false,
                new Color(0.59f, 0.31f, 0.72f),
                NodePortMask.West | NodePortMask.South,
                NodePortMask.East,
                12,
                2,
                false,
                WorkshopProductionRecipe.Create("recipe.fusion.advanced.steam", "Forge Final Steam", 3f, new[] { WorkshopItemStack.Create(advancedSteamSpell, 2) }, new[] { WorkshopItemStack.Create(ultimateSteamSpell, 1) }),
                WorkshopProductionRecipe.Create("recipe.fusion.advanced.tempest", "Forge Final Tempest", 3f, new[] { WorkshopItemStack.Create(advancedTempestSpell, 2) }, new[] { WorkshopItemStack.Create(ultimateTempestSpell, 1) }),
                WorkshopProductionRecipe.Create("recipe.fusion.advanced.prism", "Forge Final Prism", 3f, new[] { WorkshopItemStack.Create(advancedPrismSpell, 2) }, new[] { WorkshopItemStack.Create(ultimatePrismSpell, 1) }),
                WorkshopProductionRecipe.Create("recipe.fusion.advanced.polarity", "Forge Final Polarity", 3f, new[] { WorkshopItemStack.Create(advancedPolaritySpell, 2) }, new[] { WorkshopItemStack.Create(ultimatePolaritySpell, 1) }));

            var conduit = UpsertNode(
                "node.factory.conduit",
                "Arcane Conduit",
                "Relay node that forwards element resources through the line.",
                WorkshopNodeCategory.Storage,
                true,
                new Color(0.37f, 0.39f, 0.43f),
                NodePortMask.West,
                NodePortMask.East,
                12,
                2,
                true,
                Array.Empty<WorkshopProductionRecipe>());

            var turnConduit = UpsertNode(
                "node.factory.turn_conduit",
                "Turning Conduit",
                "L-shaped relay node that turns element resources around a corner.",
                WorkshopNodeCategory.Storage,
                true,
                new Color(0.46f, 0.42f, 0.34f),
                NodePortMask.East,
                NodePortMask.North,
                12,
                2,
                true,
                Array.Empty<WorkshopProductionRecipe>());

            var turnConduitMirror = UpsertNode(
                "node.factory.turn_conduit.mirror",
                "Turning Conduit Mirror",
                "Mirrored L-shaped relay node that turns element resources around a corner.",
                WorkshopNodeCategory.Storage,
                true,
                new Color(0.46f, 0.42f, 0.34f),
                NodePortMask.West,
                NodePortMask.North,
                12,
                2,
                true,
                Array.Empty<WorkshopProductionRecipe>());

            var spellConduit = UpsertNode(
                "node.factory.spell_conduit",
                "Spell Conduit",
                "Relay node that forwards crafted spell cards toward a deck collector.",
                WorkshopNodeCategory.Storage,
                true,
                new Color(0.58f, 0.42f, 0.78f),
                NodePortMask.West,
                NodePortMask.East,
                12,
                2,
                false,
                Array.Empty<WorkshopProductionRecipe>());

            var turnSpellConduit = UpsertNode(
                "node.factory.turn_spell_conduit",
                "Turning Spell Conduit",
                "L-shaped relay node that turns crafted spell cards around a corner.",
                WorkshopNodeCategory.Storage,
                true,
                new Color(0.78f, 0.34f, 0.3f),
                NodePortMask.East,
                NodePortMask.North,
                12,
                2,
                false,
                Array.Empty<WorkshopProductionRecipe>());

            var turnSpellConduitMirror = UpsertNode(
                "node.factory.turn_spell_conduit.mirror",
                "Turning Spell Conduit Mirror",
                "Mirrored L-shaped relay node that turns crafted spell cards around a corner.",
                WorkshopNodeCategory.Storage,
                true,
                new Color(0.78f, 0.34f, 0.3f),
                NodePortMask.West,
                NodePortMask.North,
                12,
                2,
                false,
                Array.Empty<WorkshopProductionRecipe>());

            var deckCollector = UpsertNode(
                "node.factory.deck_collector",
                "Battle Deck Collector",
                "Collection point. Only spell cards routed into this block are added to the battle deck.",
                WorkshopNodeCategory.Storage,
                true,
                new Color(0.94f, 0.72f, 0.28f),
                NodePortMask.All,
                NodePortMask.None,
                60,
                0,
                false,
                Array.Empty<WorkshopProductionRecipe>());

            var unlockSpellFusionBasic = UpsertReward("reward.unlock.spell_fusion_basic", "Unlock Spell Fusion Basic", "Unlocks same-element spell fusion.", WorkshopRewardKind.UnlockNode, spellFusionBasicFactory, 0f, Array.Empty<WorkshopItemStack>(), 0);
            var unlockSpellFusionIntermediate = UpsertReward("reward.unlock.spell_fusion_intermediate", "Unlock Spell Fusion Intermediate", "Unlocks intermediate-to-advanced spell fusion.", WorkshopRewardKind.UnlockNode, spellFusionIntermediateFactory, 0f, Array.Empty<WorkshopItemStack>(), 0);
            var unlockSpellFusionAdvanced = UpsertReward("reward.unlock.spell_fusion_advanced", "Unlock Spell Fusion Advanced", "Unlocks advanced-to-final spell fusion.", WorkshopRewardKind.UnlockNode, spellFusionAdvancedFactory, 0f, Array.Empty<WorkshopItemStack>(), 0);
            var unlockIceSpirit = UpsertReward("reward.unlock.spirit.ice", "Unlock Ice Spirit Node", "Adds the Ice spirit node to the workshop palette.", WorkshopRewardKind.UnlockNode, iceSpirit, 0f, Array.Empty<WorkshopItemStack>(), 80);
            var unlockThunderSpirit = UpsertReward("reward.unlock.spirit.thunder", "Unlock Thunder Spirit Node", "Adds the Thunder spirit node to the workshop palette.", WorkshopRewardKind.UnlockNode, thunderSpirit, 0f, Array.Empty<WorkshopItemStack>(), 80);
            var unlockLightSpirit = UpsertReward("reward.unlock.spirit.light", "Unlock Light Spirit Node", "Adds the Light spirit node to the workshop palette.", WorkshopRewardKind.UnlockNode, lightSpirit, 0f, Array.Empty<WorkshopItemStack>(), 150);
            var unlockDarkSpirit = UpsertReward("reward.unlock.spirit.dark", "Unlock Dark Spirit Node", "Adds the Dark spirit node to the workshop palette.", WorkshopRewardKind.UnlockNode, darkSpirit, 0f, Array.Empty<WorkshopItemStack>(), 150);
            var boostShaping = UpsertReward("reward.boost.shaping", "Shaping Factory Overclock", "Applies +20% speed to Element Shaping Factories.", WorkshopRewardKind.EfficiencyBoost, elementShapingFactory, 0.2f, Array.Empty<WorkshopItemStack>(), 30);
            var reserveReward = UpsertReward("reward.resources.recovery", "Emergency Element Cache", "Adds a small reserve of all basic elements.", WorkshopRewardKind.GrantItems, null, 0f, new[]
            {
                WorkshopItemStack.Create(fire, 3),
                WorkshopItemStack.Create(water, 3),
                WorkshopItemStack.Create(wind, 3),
                WorkshopItemStack.Create(earth, 3)
            }, 20);

            var database = CreateOrLoadAsset<WorkshopContentDatabase>($"{DataRoot}/WorkshopContentDatabase.asset");
            Vector2Int starterCollectorCell = new Vector2Int(24, 24);
            database.Configure(
                new Vector2Int(50, 50),
                0.25f,
                new[] { fireSpirit, waterSpirit, windSpirit, earthSpirit, iceSpirit, thunderSpirit, lightSpirit, darkSpirit, elementFusionFactory, elementShapingFactory, spellFusionBasicFactory, spellFusionIntermediateFactory, spellFusionAdvancedFactory, conduit, turnConduit, turnConduitMirror, spellConduit, turnSpellConduit, turnSpellConduitMirror, deckCollector },
                new[] { unlockSpellFusionBasic, unlockSpellFusionIntermediate, unlockSpellFusionAdvanced, unlockIceSpirit, unlockThunderSpirit, unlockLightSpirit, unlockDarkSpirit, boostShaping, reserveReward },
                new[]
                {
                    WorkshopPlacedNodeSeed.Create(deckCollector, starterCollectorCell, 0),
                });

            var validationErrors = database.ValidateContent();
            if (validationErrors.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Generated WorkshopContentDatabase is invalid:\n- {string.Join("\n- ", validationErrors)}");
            }

            EditorUtility.SetDirty(database);

            BuildScene(database);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static WorkshopItemDefinition UpsertItem(
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
            var asset = CreateOrLoadAsset<WorkshopItemDefinition>($"{DataRoot}/Items/{SanitizeAssetName(id)}.asset");
            asset.Configure(id, displayName, description, kind, tint, battleCardId, element, tier, role, rarityWeight, primaryValue, hitCount, secondaryValue, effectKeyword);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static WorkshopNodeDefinition UpsertNode(
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
            var asset = CreateOrLoadAsset<WorkshopNodeDefinition>($"{DataRoot}/Nodes/{SanitizeAssetName(id)}.asset");
            asset.Configure(id, displayName, description, category, unlockedByDefault, tint, inputPorts, outputPorts, bufferCapacity, maxTransferPerStep, acceptsAnyResource, recipes);

            var sprite = FindNodeSprite(id);
            if (sprite != null)
            {
                asset.NodeSprite = sprite;
            }

            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static Sprite FindNodeSprite(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            var parts = id.Split('.');
            var subdir = parts.Length >= 2 && parts[1] == "factory" ? "Factories" : "Spirits";
            var spritePath = $"Assets/ArcaneAtelier/Art/Nodes/{subdir}/{SanitizeAssetName(id)}.png";
            return AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        }

        private static WorkshopRewardDefinition UpsertReward(
            string id,
            string displayName,
            string description,
            WorkshopRewardKind rewardKind,
            WorkshopNodeDefinition targetNode,
            float efficiencyBonus,
            WorkshopItemStack[] grantedItems,
            int tokenCost = 0)
        {
            var asset = CreateOrLoadAsset<WorkshopRewardDefinition>($"{DataRoot}/Rewards/{SanitizeAssetName(id)}.asset");
            asset.Configure(id, displayName, description, rewardKind, targetNode, efficiencyBonus, grantedItems, tokenCost);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static T CreateOrLoadAsset<T>(string assetPath) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
            return asset;
        }

        private static void BuildScene(WorkshopContentDatabase database)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cameraGo = new GameObject("Main Camera");
            cameraGo.tag = "MainCamera";
            var camera = cameraGo.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 4.6f;
            camera.backgroundColor = new Color(0.08f, 0.08f, 0.1f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            cameraGo.transform.position = new Vector3(4.8f, 2.8f, -10f);

            var root = new GameObject("Spell Assembly Root");
            var view = root.AddComponent<WorkshopGridView>();
            var hud = root.AddComponent<WorkshopHudPresenter>();
            var controller = root.AddComponent<WorkshopSceneController>();
            controller.Configure(database, view, hud);

            EditorSceneManager.SaveScene(scene, ScenePath);

            var buildScenes = EditorBuildSettings.scenes.ToList();
            var existingIndex = buildScenes.FindIndex(entry => entry.path == ScenePath);
            if (existingIndex >= 0)
            {
                buildScenes[existingIndex] = new EditorBuildSettingsScene(ScenePath, true);
            }
            else
            {
                buildScenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            }

            EditorBuildSettings.scenes = buildScenes.ToArray();
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

        private static void EnsureFolder(string assetPath)
        {
            if (AssetDatabase.IsValidFolder(assetPath))
            {
                return;
            }

            var parent = Path.GetDirectoryName(assetPath)?.Replace("\\", "/");
            var folderName = Path.GetFileName(assetPath);
            if (string.IsNullOrWhiteSpace(parent) || string.IsNullOrWhiteSpace(folderName))
            {
                throw new InvalidOperationException($"Invalid asset folder path: {assetPath}");
            }

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
