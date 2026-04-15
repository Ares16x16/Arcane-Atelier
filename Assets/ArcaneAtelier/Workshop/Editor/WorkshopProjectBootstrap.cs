using System;
using System.Collections.Generic;
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

            if (AssetDatabase.LoadAssetAtPath<WorkshopContentDatabase>($"{DataRoot}/WorkshopContentDatabase.asset") != null &&
                AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
            {
                return;
            }

            Run();
        }

        private static void Run()
        {
            EnsureFolder("Assets/Scenes");
            EnsureFolder(GeneratedRoot);
            EnsureFolder(DataRoot);
            EnsureFolder($"{DataRoot}/Items");
            EnsureFolder($"{DataRoot}/Nodes");
            EnsureFolder($"{DataRoot}/Rewards");

            var ember = UpsertItem("ember_essence", "Ember Essence", "A volatile fire reagent distilled from bonded flame spirits.", WorkshopItemKind.Resource, new Color(0.9f, 0.43f, 0.2f));
            var mist = UpsertItem("mist_essence", "Mist Essence", "A cold suspension used for cooling and sigil stabilization.", WorkshopItemKind.Resource, new Color(0.36f, 0.68f, 0.96f));
            var spellInk = UpsertItem("spell_ink", "Spell Ink", "A processed reagent that captures and binds elemental instructions.", WorkshopItemKind.Resource, new Color(0.72f, 0.42f, 0.95f));
            var crystal = UpsertItem("crystal_shard", "Crystal Shard", "A rigid catalyst required for defensive card structures.", WorkshopItemKind.Resource, new Color(0.74f, 0.95f, 1f));

            var flameBolt = UpsertItem("card_flame_bolt", "Flame Bolt", "Offensive battle card produced directly from ember throughput.", WorkshopItemKind.Card, new Color(0.97f, 0.59f, 0.23f), "combat.flame_bolt");
            var frostSigil = UpsertItem("card_frost_sigil", "Frost Sigil", "Control-focused battle card shaped from raw mist flow.", WorkshopItemKind.Card, new Color(0.49f, 0.81f, 1f), "combat.frost_sigil");
            var arcaneWard = UpsertItem("card_arcane_ward", "Arcane Ward", "Defensive battle card pressed from spell ink and crystal.", WorkshopItemKind.Card, new Color(0.84f, 0.72f, 1f), "combat.arcane_ward");

            var emberSpring = UpsertNode(
                "node_ember_spring",
                "Ember Spring",
                "Source node. Outputs Ember Essence into the connected network.",
                WorkshopNodeCategory.Source,
                true,
                new Color(0.78f, 0.28f, 0.16f),
                NodePortMask.None,
                NodePortMask.East,
                6,
                1,
                false,
                WorkshopProductionRecipe.Create("recipe_ember", "Harvest Ember", 1.25f, Array.Empty<WorkshopItemStack>(), new[] { WorkshopItemStack.Create(ember, 1) }));

            var mistWell = UpsertNode(
                "node_mist_well",
                "Mist Well",
                "Source node. Outputs Mist Essence into the connected network.",
                WorkshopNodeCategory.Source,
                true,
                new Color(0.24f, 0.53f, 0.8f),
                NodePortMask.None,
                NodePortMask.East,
                6,
                1,
                false,
                WorkshopProductionRecipe.Create("recipe_mist", "Harvest Mist", 1.25f, Array.Empty<WorkshopItemStack>(), new[] { WorkshopItemStack.Create(mist, 1) }));

            var infuser = UpsertNode(
                "node_infuser",
                "Arcane Infuser",
                "Processor node. Consumes Ember and Mist to create Spell Ink.",
                WorkshopNodeCategory.Processor,
                true,
                new Color(0.5f, 0.32f, 0.72f),
                NodePortMask.West | NodePortMask.South,
                NodePortMask.East,
                8,
                1,
                false,
                WorkshopProductionRecipe.Create(
                    "recipe_spell_ink",
                    "Brew Spell Ink",
                    2.25f,
                    new[]
                    {
                        WorkshopItemStack.Create(ember, 1),
                        WorkshopItemStack.Create(mist, 1)
                    },
                    new[] { WorkshopItemStack.Create(spellInk, 1) }));

            var flamePress = UpsertNode(
                "node_flame_press",
                "Flame Press",
                "Crafter node. Converts Ember Essence directly into Flame Bolt cards.",
                WorkshopNodeCategory.Crafter,
                true,
                new Color(0.9f, 0.45f, 0.14f),
                NodePortMask.West,
                NodePortMask.None,
                6,
                0,
                false,
                WorkshopProductionRecipe.Create(
                    "recipe_flame_bolt",
                    "Press Flame Bolt",
                    2f,
                    new[] { WorkshopItemStack.Create(ember, 2) },
                    new[] { WorkshopItemStack.Create(flameBolt, 1) }));

            var frostLoom = UpsertNode(
                "node_frost_loom",
                "Frost Loom",
                "Crafter node. Weaves Mist Essence into Frost Sigil cards.",
                WorkshopNodeCategory.Crafter,
                true,
                new Color(0.37f, 0.72f, 0.96f),
                NodePortMask.West,
                NodePortMask.None,
                6,
                0,
                false,
                WorkshopProductionRecipe.Create(
                    "recipe_frost_sigil",
                    "Weave Frost Sigil",
                    2f,
                    new[] { WorkshopItemStack.Create(mist, 2) },
                    new[] { WorkshopItemStack.Create(frostSigil, 1) }));

            var crystalLattice = UpsertNode(
                "node_crystal_lattice",
                "Crystal Lattice",
                "Source node. Outputs Crystal Shards for defensive card crafting.",
                WorkshopNodeCategory.Source,
                false,
                new Color(0.66f, 0.92f, 0.98f),
                NodePortMask.None,
                NodePortMask.North,
                6,
                1,
                false,
                WorkshopProductionRecipe.Create("recipe_crystal", "Grow Crystal", 1.6f, Array.Empty<WorkshopItemStack>(), new[] { WorkshopItemStack.Create(crystal, 1) }));

            var wardLoom = UpsertNode(
                "node_ward_loom",
                "Ward Loom",
                "Crafter node. Combines Spell Ink and Crystal Shards into Arcane Ward cards.",
                WorkshopNodeCategory.Crafter,
                false,
                new Color(0.8f, 0.72f, 1f),
                NodePortMask.West | NodePortMask.South,
                NodePortMask.None,
                8,
                0,
                false,
                WorkshopProductionRecipe.Create(
                    "recipe_arcane_ward",
                    "Weave Arcane Ward",
                    2.5f,
                    new[]
                    {
                        WorkshopItemStack.Create(spellInk, 1),
                        WorkshopItemStack.Create(crystal, 1)
                    },
                    new[] { WorkshopItemStack.Create(arcaneWard, 1) }));

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

            var unlockCrystalReward = UpsertReward("reward_unlock_crystal", "Unlock Crystal Lattice", "Adds a new crystal source node to the workshop palette.", WorkshopRewardKind.UnlockNode, crystalLattice, 0f, Array.Empty<WorkshopItemStack>());
            var unlockWardReward = UpsertReward("reward_unlock_ward", "Unlock Ward Loom", "Adds the Arcane Ward card production endpoint to the workshop palette.", WorkshopRewardKind.UnlockNode, wardLoom, 0f, Array.Empty<WorkshopItemStack>());
            var infuserBoostReward = UpsertReward("reward_infuser_boost", "Infuser Calibration", "Applies a permanent +25% cycle speed bonus to placed Arcane Infusers.", WorkshopRewardKind.EfficiencyBoost, infuser, 0.25f, Array.Empty<WorkshopItemStack>());
            var reserveReward = UpsertReward("reward_emergency_supplies", "Emergency Supplies", "Injects extra Ember and Mist into reserve inventory for chain recovery.", WorkshopRewardKind.GrantItems, null, 0f, new[]
            {
                WorkshopItemStack.Create(ember, 3),
                WorkshopItemStack.Create(mist, 3)
            });

            var database = CreateOrLoadAsset<WorkshopContentDatabase>($"{DataRoot}/WorkshopContentDatabase.asset");
            database.Configure(
                new Vector2Int(9, 6),
                0.25f,
                new[] { emberSpring, mistWell, infuser, flamePress, frostLoom, crystalLattice, wardLoom, conduit },
                new[] { unlockCrystalReward, unlockWardReward, infuserBoostReward, reserveReward },
                new[]
                {
                    WorkshopPlacedNodeSeed.Create(emberSpring, new Vector2Int(1, 4), 0),
                    WorkshopPlacedNodeSeed.Create(flamePress, new Vector2Int(2, 4), 0),
                    WorkshopPlacedNodeSeed.Create(mistWell, new Vector2Int(1, 2), 0),
                    WorkshopPlacedNodeSeed.Create(frostLoom, new Vector2Int(2, 2), 0),
                    WorkshopPlacedNodeSeed.Create(emberSpring, new Vector2Int(4, 3), 0),
                    WorkshopPlacedNodeSeed.Create(infuser, new Vector2Int(5, 3), 0),
                    WorkshopPlacedNodeSeed.Create(mistWell, new Vector2Int(5, 2), 1)
                });
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
