using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArcaneAtelier.Workshop.Editor
{
    public static class WorkshopProjectBootstrap
    {
        private const string GeneratedRoot = "Assets/ArcaneAtelier/Workshop/Generated";
        private const string DataRoot = GeneratedRoot + "/Data";
        private const string ScenePath = "Assets/Scenes/SpellAssemblyScene.unity";

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
            var databaseExists = AssetDatabase.LoadAssetAtPath<WorkshopContentDatabase>($"{DataRoot}/WorkshopContentDatabase.asset") != null;

            if (!sceneExists || !databaseExists)
            {
                Run();
                sceneExists = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null;
            }

            if (!sceneExists)
            {
                return;
            }

            var activeScene = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(activeScene.path))
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                if (SceneView.lastActiveSceneView != null)
                {
                    SceneView.lastActiveSceneView.in2DMode = true;
                }
            }
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

            var basicFireSpell = UpsertItem("spell.basic.fire", "Basic Fire Spell", "Tier-1 fire spell.", WorkshopItemKind.Card, new Color(0.95f, 0.43f, 0.2f), "combat.spell.basic.fire");
            var basicWaterSpell = UpsertItem("spell.basic.water", "Basic Water Spell", "Tier-1 water spell.", WorkshopItemKind.Card, new Color(0.33f, 0.68f, 1f), "combat.spell.basic.water");
            var basicWindSpell = UpsertItem("spell.basic.wind", "Basic Wind Spell", "Tier-1 wind spell.", WorkshopItemKind.Card, new Color(0.66f, 0.91f, 0.95f), "combat.spell.basic.wind");
            var basicEarthSpell = UpsertItem("spell.basic.earth", "Basic Earth Spell", "Tier-1 earth spell.", WorkshopItemKind.Card, new Color(0.71f, 0.55f, 0.35f), "combat.spell.basic.earth");
            var basicIceSpell = UpsertItem("spell.basic.ice", "Basic Ice Spell", "Tier-1 ice spell.", WorkshopItemKind.Card, new Color(0.72f, 0.92f, 1f), "combat.spell.basic.ice");
            var basicThunderSpell = UpsertItem("spell.basic.thunder", "Basic Thunder Spell", "Tier-1 thunder spell.", WorkshopItemKind.Card, new Color(1f, 0.9f, 0.36f), "combat.spell.basic.thunder");
            var basicLightSpell = UpsertItem("spell.basic.light", "Basic Light Spell", "Tier-1 light spell.", WorkshopItemKind.Card, new Color(1f, 0.98f, 0.74f), "combat.spell.basic.light");
            var basicDarkSpell = UpsertItem("spell.basic.dark", "Basic Dark Spell", "Tier-1 dark spell.", WorkshopItemKind.Card, new Color(0.56f, 0.5f, 0.74f), "combat.spell.basic.dark");

            var intermediateFireSpell = UpsertItem("spell.intermediate.fire", "Intermediate Fire Spell", "Tier-2 fire spell.", WorkshopItemKind.Card, new Color(0.99f, 0.48f, 0.3f), "combat.spell.intermediate.fire");
            var intermediateWaterSpell = UpsertItem("spell.intermediate.water", "Intermediate Water Spell", "Tier-2 water spell.", WorkshopItemKind.Card, new Color(0.45f, 0.72f, 1f), "combat.spell.intermediate.water");
            var intermediateWindSpell = UpsertItem("spell.intermediate.wind", "Intermediate Wind Spell", "Tier-2 wind spell.", WorkshopItemKind.Card, new Color(0.71f, 0.95f, 0.98f), "combat.spell.intermediate.wind");
            var intermediateEarthSpell = UpsertItem("spell.intermediate.earth", "Intermediate Earth Spell", "Tier-2 earth spell.", WorkshopItemKind.Card, new Color(0.78f, 0.6f, 0.38f), "combat.spell.intermediate.earth");
            var intermediateIceSpell = UpsertItem("spell.intermediate.ice", "Intermediate Ice Spell", "Tier-2 ice spell.", WorkshopItemKind.Card, new Color(0.8f, 0.96f, 1f), "combat.spell.intermediate.ice");
            var intermediateThunderSpell = UpsertItem("spell.intermediate.thunder", "Intermediate Thunder Spell", "Tier-2 thunder spell.", WorkshopItemKind.Card, new Color(1f, 0.94f, 0.46f), "combat.spell.intermediate.thunder");
            var intermediateLightSpell = UpsertItem("spell.intermediate.light", "Intermediate Light Spell", "Tier-2 light spell.", WorkshopItemKind.Card, new Color(1f, 0.99f, 0.81f), "combat.spell.intermediate.light");
            var intermediateDarkSpell = UpsertItem("spell.intermediate.dark", "Intermediate Dark Spell", "Tier-2 dark spell.", WorkshopItemKind.Card, new Color(0.63f, 0.57f, 0.8f), "combat.spell.intermediate.dark");

            var advancedPrismSpell = UpsertItem("spell.advanced.prism", "Advanced Prism Spell", "Tier-3 spell from Light + Dark opposition.", WorkshopItemKind.Card, new Color(0.97f, 0.84f, 1f), "combat.spell.advanced.prism");
            var advancedTempestSpell = UpsertItem("spell.advanced.tempest", "Advanced Tempest Spell", "Tier-3 spell from Wind + Earth opposition.", WorkshopItemKind.Card, new Color(0.8f, 0.93f, 0.84f), "combat.spell.advanced.tempest");
            var advancedSteamSpell = UpsertItem("spell.advanced.steam", "Advanced Steam Spell", "Tier-3 spell from Fire + Water opposition.", WorkshopItemKind.Card, new Color(0.89f, 0.78f, 0.72f), "combat.spell.advanced.steam");
            var advancedPolaritySpell = UpsertItem("spell.advanced.polarity", "Advanced Polarity Spell", "Tier-3 spell from Ice + Thunder opposition.", WorkshopItemKind.Card, new Color(0.86f, 0.92f, 1f), "combat.spell.advanced.polarity");

            var fireSpirit = UpsertNode(
                "node.spirit.fire",
                "Fire Spirit Node",
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
                "Water Spirit Node",
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
                "Wind Spirit Node",
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
                "Earth Spirit Node",
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

            var elementFusionFactory = UpsertNode(
                "node.factory.element_fusion",
                "Element Fusion Factory",
                "Combines non-opposing basic elements into secondary elements.",
                WorkshopNodeCategory.Processor,
                false,
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
                "Element Shaping Factory",
                "Shapes one element into one basic spell card.",
                WorkshopNodeCategory.Crafter,
                true,
                new Color(0.95f, 0.62f, 0.25f),
                NodePortMask.West,
                NodePortMask.None,
                12,
                0,
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
                "Spell Fusion Factory - Basic",
                "Fuses two basic spells of the same element into intermediate spells.",
                WorkshopNodeCategory.Crafter,
                false,
                new Color(0.9f, 0.44f, 0.63f),
                NodePortMask.West | NodePortMask.South,
                NodePortMask.None,
                12,
                0,
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
                "Spell Fusion Factory - Intermediate",
                "Fuses non-opposing basic spells into secondary intermediate spells.",
                WorkshopNodeCategory.Crafter,
                false,
                new Color(0.77f, 0.42f, 0.79f),
                NodePortMask.West | NodePortMask.South,
                NodePortMask.None,
                12,
                0,
                false,
                WorkshopProductionRecipe.Create("recipe.fusion.intermediate.ice", "Fuse Intermediate Ice", 2.4f, new[] { WorkshopItemStack.Create(basicWindSpell, 1), WorkshopItemStack.Create(basicWaterSpell, 1) }, new[] { WorkshopItemStack.Create(intermediateIceSpell, 1) }),
                WorkshopProductionRecipe.Create("recipe.fusion.intermediate.thunder", "Fuse Intermediate Thunder", 2.4f, new[] { WorkshopItemStack.Create(basicWindSpell, 1), WorkshopItemStack.Create(basicFireSpell, 1) }, new[] { WorkshopItemStack.Create(intermediateThunderSpell, 1) }),
                WorkshopProductionRecipe.Create("recipe.fusion.intermediate.light", "Fuse Intermediate Light", 2.4f, new[] { WorkshopItemStack.Create(basicEarthSpell, 1), WorkshopItemStack.Create(basicFireSpell, 1) }, new[] { WorkshopItemStack.Create(intermediateLightSpell, 1) }),
                WorkshopProductionRecipe.Create("recipe.fusion.intermediate.dark", "Fuse Intermediate Dark", 2.4f, new[] { WorkshopItemStack.Create(basicEarthSpell, 1), WorkshopItemStack.Create(basicWaterSpell, 1) }, new[] { WorkshopItemStack.Create(intermediateDarkSpell, 1) }));

            var spellFusionAdvancedFactory = UpsertNode(
                "node.factory.spell_fusion.advanced",
                "Spell Fusion Factory - Advanced",
                "Fuses opposing intermediate spells into advanced cards.",
                WorkshopNodeCategory.Crafter,
                false,
                new Color(0.59f, 0.31f, 0.72f),
                NodePortMask.West | NodePortMask.South,
                NodePortMask.None,
                12,
                0,
                false,
                WorkshopProductionRecipe.Create("recipe.fusion.advanced.steam", "Forge Advanced Steam", 3f, new[] { WorkshopItemStack.Create(intermediateFireSpell, 1), WorkshopItemStack.Create(intermediateWaterSpell, 1) }, new[] { WorkshopItemStack.Create(advancedSteamSpell, 1) }),
                WorkshopProductionRecipe.Create("recipe.fusion.advanced.tempest", "Forge Advanced Tempest", 3f, new[] { WorkshopItemStack.Create(intermediateWindSpell, 1), WorkshopItemStack.Create(intermediateEarthSpell, 1) }, new[] { WorkshopItemStack.Create(advancedTempestSpell, 1) }),
                WorkshopProductionRecipe.Create("recipe.fusion.advanced.prism", "Forge Advanced Prism", 3f, new[] { WorkshopItemStack.Create(intermediateLightSpell, 1), WorkshopItemStack.Create(intermediateDarkSpell, 1) }, new[] { WorkshopItemStack.Create(advancedPrismSpell, 1) }),
                WorkshopProductionRecipe.Create("recipe.fusion.advanced.polarity", "Forge Advanced Polarity", 3f, new[] { WorkshopItemStack.Create(intermediateIceSpell, 1), WorkshopItemStack.Create(intermediateThunderSpell, 1) }, new[] { WorkshopItemStack.Create(advancedPolaritySpell, 1) }));

            var conduit = UpsertNode(
                "node_conduit",
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

            var unlockElementFusion = UpsertReward("reward.unlock.element_fusion", "Unlock Element Fusion Factory", "Unlocks secondary element production.", WorkshopRewardKind.UnlockNode, elementFusionFactory, 0f, Array.Empty<WorkshopItemStack>());
            var unlockSpellFusionBasic = UpsertReward("reward.unlock.spell_fusion_basic", "Unlock Spell Fusion Basic", "Unlocks same-element spell fusion.", WorkshopRewardKind.UnlockNode, spellFusionBasicFactory, 0f, Array.Empty<WorkshopItemStack>());
            var unlockSpellFusionIntermediate = UpsertReward("reward.unlock.spell_fusion_intermediate", "Unlock Spell Fusion Intermediate", "Unlocks non-opposing mixed spell fusion.", WorkshopRewardKind.UnlockNode, spellFusionIntermediateFactory, 0f, Array.Empty<WorkshopItemStack>());
            var unlockSpellFusionAdvanced = UpsertReward("reward.unlock.spell_fusion_advanced", "Unlock Spell Fusion Advanced", "Unlocks opposing-element advanced fusion.", WorkshopRewardKind.UnlockNode, spellFusionAdvancedFactory, 0f, Array.Empty<WorkshopItemStack>());
            var boostShaping = UpsertReward("reward.boost.shaping", "Shaping Factory Overclock", "Applies +20% speed to Element Shaping Factories.", WorkshopRewardKind.EfficiencyBoost, elementShapingFactory, 0.2f, Array.Empty<WorkshopItemStack>());
            var reserveReward = UpsertReward("reward.resources.recovery", "Emergency Element Cache", "Adds a small reserve of all basic elements.", WorkshopRewardKind.GrantItems, null, 0f, new[]
            {
                WorkshopItemStack.Create(fire, 3),
                WorkshopItemStack.Create(water, 3),
                WorkshopItemStack.Create(wind, 3),
                WorkshopItemStack.Create(earth, 3)
            });

            var database = CreateOrLoadAsset<WorkshopContentDatabase>($"{DataRoot}/WorkshopContentDatabase.asset");
            database.Configure(
                new Vector2Int(9, 6),
                0.25f,
                new[] { fireSpirit, waterSpirit, windSpirit, earthSpirit, elementFusionFactory, elementShapingFactory, spellFusionBasicFactory, spellFusionIntermediateFactory, spellFusionAdvancedFactory, conduit },
                new[] { unlockElementFusion, unlockSpellFusionBasic, unlockSpellFusionIntermediate, unlockSpellFusionAdvanced, boostShaping, reserveReward },
                new[]
                {
                    WorkshopPlacedNodeSeed.Create(fireSpirit, new Vector2Int(0, 4), 0),
                    WorkshopPlacedNodeSeed.Create(waterSpirit, new Vector2Int(0, 3), 0),
                    WorkshopPlacedNodeSeed.Create(windSpirit, new Vector2Int(0, 2), 0),
                    WorkshopPlacedNodeSeed.Create(earthSpirit, new Vector2Int(0, 1), 0),
                    WorkshopPlacedNodeSeed.Create(conduit, new Vector2Int(1, 3), 0),
                    WorkshopPlacedNodeSeed.Create(conduit, new Vector2Int(2, 3), 0),
                    WorkshopPlacedNodeSeed.Create(elementShapingFactory, new Vector2Int(3, 3), 0)
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

        private static WorkshopItemDefinition UpsertItem(string id, string displayName, string description, WorkshopItemKind kind, Color tint, string battleCardId = "")
        {
            var asset = CreateOrLoadAsset<WorkshopItemDefinition>($"{DataRoot}/Items/{displayName.Replace(" ", string.Empty)}.asset");
            asset.Configure(id, displayName, description, kind, tint, battleCardId);
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
            var asset = CreateOrLoadAsset<WorkshopNodeDefinition>($"{DataRoot}/Nodes/{displayName.Replace(" ", string.Empty)}.asset");
            asset.Configure(id, displayName, description, category, unlockedByDefault, tint, inputPorts, outputPorts, bufferCapacity, maxTransferPerStep, acceptsAnyResource, recipes);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static WorkshopRewardDefinition UpsertReward(
            string id,
            string displayName,
            string description,
            WorkshopRewardKind rewardKind,
            WorkshopNodeDefinition targetNode,
            float efficiencyBonus,
            WorkshopItemStack[] grantedItems)
        {
            var asset = CreateOrLoadAsset<WorkshopRewardDefinition>($"{DataRoot}/Rewards/{displayName.Replace(" ", string.Empty)}.asset");
            asset.Configure(id, displayName, description, rewardKind, targetNode, efficiencyBonus, grantedItems);
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
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
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
