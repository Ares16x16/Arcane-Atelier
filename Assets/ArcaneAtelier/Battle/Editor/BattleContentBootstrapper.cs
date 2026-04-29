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

            BattleBossDefinition earthGolem = CreateEarthGolemBoss();

            BattleContentDatabase database = CreateContentDatabase(
                new[] { earthGolem },
                new[] { attackTemplate, healTemplate, defendTemplate });

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

            asset.Configure(
                "boss.earth.golem",
                "Corrupted Earth Golem",
                150,
                WorkshopElementAttribute.Earth,
                pattern,
                "reward_unlock_crystal");

            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static BattleContentDatabase CreateContentDatabase(
            BattleBossDefinition[] bosses,
            BattleCardEffectTemplate[] templates)
        {
            string path = ContentPath + "/BattleContentDatabase.asset";
            BattleContentDatabase asset = ScriptableObject.CreateInstance<BattleContentDatabase>();
            asset.Configure(bosses, templates);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
