#if UNITY_EDITOR
using System.IO;
using ArcaneAtelier.Workshop;
using UnityEditor;
using UnityEngine;

namespace ArcaneAtelier.Battle.Editor
{
    public static class BattleContentBootstrapper
    {
        private const string ContentPath = "Assets/ArcaneAtelier/Battle/Content";

        [MenuItem("Arcane Atelier/Battle/Generate Default Content")]
        public static void GenerateDefaultContent()
        {
            EnsureDirectory(ContentPath);

            BattleCardEffectTemplate attackTemplate = CreateAttackTemplate();
            BattleCardEffectTemplate healTemplate = CreateHealTemplate();
            BattleCardEffectTemplate defendTemplate = CreateDefendTemplate();

            BattleBossDefinition ashImp = CreateAshImpEnemy();
            BattleBossDefinition mossShell = CreateMossShellEnemy();
            BattleBossDefinition mistLeech = CreateMistLeechEnemy();
            BattleBossDefinition earthGolem = CreateEarthGolemBoss();
            BattlePresentationProfile ashImpPresentation = CreateAshImpPresentation();
            BattlePresentationProfile mossShellPresentation = CreateMossShellPresentation();
            BattlePresentationProfile mistLeechPresentation = CreateMistLeechPresentation();
            BattlePresentationProfile earthGolemPresentation = CreateEarthGolemPresentation();

            BattleContentDatabase database = CreateContentDatabase(
                new[] { ashImp, mossShell, mistLeech, earthGolem },
                new[] { attackTemplate, healTemplate, defendTemplate },
                new[] { ashImpPresentation, mossShellPresentation, mistLeechPresentation, earthGolemPresentation });

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Battle Content Generated",
                "Created default battle content assets in 'ArcaneAtelier/Battle/Content/'.",
                "OK");

            Selection.activeObject = database;
        }

        private static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        private static BattleCardEffectTemplate CreateAttackTemplate()
        {
            string path = ContentPath + "/CardEffectTemplate_Attack.asset";
            BattleCardEffectTemplate asset = ScriptableObject.CreateInstance<BattleCardEffectTemplate>();
            asset.Configure(
                "template.attack",
                WorkshopSpellRole.Attack,
                1f,
                1f,
                "Standard attack effect.");
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static BattleCardEffectTemplate CreateHealTemplate()
        {
            string path = ContentPath + "/CardEffectTemplate_Heal.asset";
            BattleCardEffectTemplate asset = ScriptableObject.CreateInstance<BattleCardEffectTemplate>();
            asset.Configure(
                "template.heal",
                WorkshopSpellRole.Healing,
                1f,
                1f,
                "Standard healing effect.");
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static BattleCardEffectTemplate CreateDefendTemplate()
        {
            string path = ContentPath + "/CardEffectTemplate_Defend.asset";
            BattleCardEffectTemplate asset = ScriptableObject.CreateInstance<BattleCardEffectTemplate>();
            asset.Configure(
                "template.defend",
                WorkshopSpellRole.Defense,
                1f,
                1f,
                "Standard defense effect.");
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static BattleBossDefinition CreateEarthGolemBoss()
        {
            string path = ContentPath + "/Boss_EarthGolem.asset";
            BattleBossDefinition asset = ScriptableObject.CreateInstance<BattleBossDefinition>();

            BattleBossAction[] pattern = new BattleBossAction[]
            {
                new BattleBossAction
                {
                    ActionType = BattleActionType.Attack,
                    Value = 15,
                    SecondaryValue = 0f,
                    Description = "Slams the ground"
                },
                new BattleBossAction
                {
                    ActionType = BattleActionType.Defend,
                    Value = 10,
                    SecondaryValue = 0f,
                    Description = "Hardens its shell"
                },
                new BattleBossAction
                {
                    ActionType = BattleActionType.Attack,
                    Value = 20,
                    SecondaryValue = 0f,
                    Description = "Heavy strike"
                },
                new BattleBossAction
                {
                    ActionType = BattleActionType.Heal,
                    Value = 10,
                    SecondaryValue = 0f,
                    Description = "Absorbs earth energy"
                }
            };

            BattleBossAction[] phase2Pattern = new BattleBossAction[]
            {
                new BattleBossAction
                {
                    ActionType = BattleActionType.Attack,
                    Value = 22,
                    SecondaryValue = 0f,
                    Description = "Raging slam"
                },
                new BattleBossAction
                {
                    ActionType = BattleActionType.Attack,
                    Value = 30,
                    SecondaryValue = 0f,
                    Description = "Crushing blow"
                },
                new BattleBossAction
                {
                    ActionType = BattleActionType.Heal,
                    Value = 15,
                    SecondaryValue = 0f,
                    Description = "Devours earth energy"
                }
            };

            asset.Configure(
                "boss.earth.golem",
                "Corrupted Earth Golem",
                150,
                WorkshopElementAttribute.Earth,
                BattleEncounterType.Boss,
                100,
                BattleEnemyArchetype.None,
                0.35f,
                16,
                3,
                pattern,
                "reward.spirit.earth",
                phase2Pattern,
                0.5f);

            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static BattleBossDefinition CreateAshImpEnemy()
        {
            string path = ContentPath + "/Enemy_AshImp.asset";
            BattleBossDefinition asset = ScriptableObject.CreateInstance<BattleBossDefinition>();

            BattleBossAction[] pattern = new BattleBossAction[]
            {
                new BattleBossAction
                {
                    ActionType = BattleActionType.Attack,
                    Value = 8,
                    SecondaryValue = 0f,
                    Description = "Scorches with ember claws"
                },
                new BattleBossAction
                {
                    ActionType = BattleActionType.Attack,
                    Value = 12,
                    SecondaryValue = 0f,
                    Description = "Spits a burst of cinders"
                },
                new BattleBossAction
                {
                    ActionType = BattleActionType.Special,
                    Value = 10,
                    SecondaryValue = 0f,
                    Description = "Ignites the air"
                }
            };

            asset.Configure(
                "enemy.ash.imp",
                "Ash Imp",
                36,
                WorkshopElementAttribute.Fire,
                BattleEncounterType.Enemy,
                1,
                BattleEnemyArchetype.Aggressive,
                0.3f,
                0,
                3,
                pattern,
                "reward.enemy.fire.minor");

            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static BattleBossDefinition CreateMossShellEnemy()
        {
            string path = ContentPath + "/Enemy_MossShell.asset";
            BattleBossDefinition asset = ScriptableObject.CreateInstance<BattleBossDefinition>();

            BattleBossAction[] pattern = new BattleBossAction[]
            {
                new BattleBossAction
                {
                    ActionType = BattleActionType.Defend,
                    Value = 8,
                    SecondaryValue = 0f,
                    Description = "Raises a bark shield"
                },
                new BattleBossAction
                {
                    ActionType = BattleActionType.Attack,
                    Value = 6,
                    SecondaryValue = 0f,
                    Description = "Body slams forward"
                },
                new BattleBossAction
                {
                    ActionType = BattleActionType.Defend,
                    Value = 12,
                    SecondaryValue = 0f,
                    Description = "Roots tighten into armor"
                }
            };

            asset.Configure(
                "enemy.moss.shell",
                "Moss Shell",
                65,
                WorkshopElementAttribute.Earth,
                BattleEncounterType.Enemy,
                3,
                BattleEnemyArchetype.Defensive,
                0.45f,
                12,
                4,
                pattern,
                "reward.enemy.earth.minor");

            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static BattleBossDefinition CreateMistLeechEnemy()
        {
            string path = ContentPath + "/Enemy_MistLeech.asset";
            BattleBossDefinition asset = ScriptableObject.CreateInstance<BattleBossDefinition>();

            BattleBossAction[] pattern = new BattleBossAction[]
            {
                new BattleBossAction
                {
                    ActionType = BattleActionType.Attack,
                    Value = 7,
                    SecondaryValue = 0f,
                    Description = "Drains a thread of vitality"
                },
                new BattleBossAction
                {
                    ActionType = BattleActionType.Heal,
                    Value = 6,
                    SecondaryValue = 0f,
                    Description = "Condenses moisture to recover"
                },
                new BattleBossAction
                {
                    ActionType = BattleActionType.Attack,
                    Value = 9,
                    SecondaryValue = 0f,
                    Description = "Lashes with a liquid tendril"
                }
            };

            asset.Configure(
                "enemy.mist.leech",
                "Mist Leech",
                50,
                WorkshopElementAttribute.Water,
                BattleEncounterType.Enemy,
                2,
                BattleEnemyArchetype.Sustain,
                0.5f,
                0,
                3,
                pattern,
                "reward.enemy.water.minor");

            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static BattlePresentationProfile CreateEarthGolemPresentation()
        {
            string path = ContentPath + "/Presentation_EarthGolem.asset";
            BattlePresentationProfile asset = ScriptableObject.CreateInstance<BattlePresentationProfile>();
            asset.Configure(
                "boss.earth.golem",
                null,
                null,
                new Vector3(3.5f, 0f, 0f),
                new Vector3(2.8f, 2.8f, 1f),
                new Vector3(6f, 6f, 1f));
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static BattlePresentationProfile CreateAshImpPresentation()
        {
            string path = ContentPath + "/Presentation_AshImp.asset";
            BattlePresentationProfile asset = ScriptableObject.CreateInstance<BattlePresentationProfile>();
            asset.Configure(
                "enemy.ash.imp",
                null,
                null,
                new Vector3(3.5f, 0f, 0f),
                new Vector3(2.0f, 2.0f, 1f),
                new Vector3(6f, 6f, 1f));
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static BattlePresentationProfile CreateMossShellPresentation()
        {
            string path = ContentPath + "/Presentation_MossShell.asset";
            BattlePresentationProfile asset = ScriptableObject.CreateInstance<BattlePresentationProfile>();
            asset.Configure(
                "enemy.moss.shell",
                null,
                null,
                new Vector3(3.5f, 0f, 0f),
                new Vector3(2.6f, 2.6f, 1f),
                new Vector3(6f, 6f, 1f));
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static BattlePresentationProfile CreateMistLeechPresentation()
        {
            string path = ContentPath + "/Presentation_MistLeech.asset";
            BattlePresentationProfile asset = ScriptableObject.CreateInstance<BattlePresentationProfile>();
            asset.Configure(
                "enemy.mist.leech",
                null,
                null,
                new Vector3(3.5f, 0f, 0f),
                new Vector3(2.2f, 2.2f, 1f),
                new Vector3(6f, 6f, 1f));
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static BattleContentDatabase CreateContentDatabase(
            BattleBossDefinition[] bosses,
            BattleCardEffectTemplate[] templates,
            BattlePresentationProfile[] presentationProfiles)
        {
            string path = ContentPath + "/BattleContentDatabase.asset";
            BattleContentDatabase asset = ScriptableObject.CreateInstance<BattleContentDatabase>();
            asset.Configure(bosses, templates, presentationProfiles);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
#endif
