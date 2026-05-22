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
        private const string CharacterSpritePath = "Assets/ArcaneAtelier/Battle/Art/Sprites/Characters/";
        private const string CharacterAnimationPath = "Assets/ArcaneAtelier/Battle/Art/Sprites/Characters/Animated/";
        private const string BackgroundSpritePath = "Assets/ArcaneAtelier/Battle/Art/Sprites/Backgrounds/";

        [MenuItem("Arcane Atelier/Battle/Generate Default Content")]
        public static void GenerateDefaultContent()
        {
            EnsureDirectory(ContentPath);

            BattleCardEffectTemplate attackTemplate = CreateAttackTemplate();
            BattleCardEffectTemplate healTemplate = CreateHealTemplate();
            BattleCardEffectTemplate defendTemplate = CreateDefendTemplate();

            BattleCardDefinition[] cardDefinitions = CreateAllCardDefinitions();
            BattleStatusEffectDefinition[] statusDefinitions = CreateAllStatusEffectDefinitions();

            BattleBossDefinition ashImp = CreateAshImpEnemy();
            BattleBossDefinition mossShell = CreateMossShellEnemy();
            BattleBossDefinition mistLeech = CreateMistLeechEnemy();
            BattleBossDefinition earthGolem = CreateEarthGolemBoss();
            BattleUnitAnimationProfile ashImpAnimation = CreateEnemyAnimationProfile("Enemy_AshImp", "Enemy_AshImp");
            BattleUnitAnimationProfile mossShellAnimation = CreateEnemyAnimationProfile("Enemy_MossShell", "Enemy_MossShell");
            BattleUnitAnimationProfile mistLeechAnimation = CreateEnemyAnimationProfile("Enemy_MistLeech", "Enemy_MistLeech");
            BattleUnitAnimationProfile earthGolemAnimation = CreateEnemyAnimationProfile("Boss_EarthGolem", "Boss_EarthGolem");
            BattlePresentationProfile ashImpPresentation = CreateAshImpPresentation();
            BattlePresentationProfile mossShellPresentation = CreateMossShellPresentation();
            BattlePresentationProfile mistLeechPresentation = CreateMistLeechPresentation();
            BattlePresentationProfile earthGolemPresentation = CreateEarthGolemPresentation();

            BattleContentDatabase database = CreateContentDatabase(
                new[] { ashImp, mossShell, mistLeech, earthGolem },
                new[] { attackTemplate, healTemplate, defendTemplate },
                new[] { ashImpPresentation, mossShellPresentation, mistLeechPresentation, earthGolemPresentation },
                cardDefinitions,
                statusDefinitions);

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
                    Value = 16,
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
                    Value = 8,
                    SecondaryValue = 0f,
                    Description = "Absorbs earth energy"
                }
            };

            BattleBossAction[] phase2Pattern = new BattleBossAction[]
            {
                new BattleBossAction
                {
                    ActionType = BattleActionType.Attack,
                    Value = 15,
                    SecondaryValue = 0f,
                    Description = "Raging slam"
                },
                new BattleBossAction
                {
                    ActionType = BattleActionType.Attack,
                    Value = 19,
                    SecondaryValue = 0f,
                    Description = "Crushing blow"
                },
                new BattleBossAction
                {
                    ActionType = BattleActionType.Heal,
                    Value = 10,
                    SecondaryValue = 0f,
                    Description = "Devours earth energy"
                }
            };

            asset.Configure(
                "boss.earth.golem",
                "Corrupted Earth Golem",
                130,
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
                0.5f,
                200);

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
                    Value = 12,
                    SecondaryValue = 0f,
                    Description = "Scorches with ember claws"
                },
                new BattleBossAction
                {
                    ActionType = BattleActionType.Attack,
                    Value = 15,
                    SecondaryValue = 0f,
                    Description = "Spits a burst of cinders"
                },
                new BattleBossAction
                {
                    ActionType = BattleActionType.Special,
                    Value = 12,
                    SecondaryValue = 0f,
                    Description = "Ignites the air"
                }
            };

            asset.Configure(
                "enemy.ash.imp",
                "Ash Imp",
                42,
                WorkshopElementAttribute.Fire,
                BattleEncounterType.Enemy,
                1,
                BattleEnemyArchetype.Aggressive,
                0.3f,
                0,
                3,
                pattern,
                "reward.enemy.fire.minor",
                tokenReward: 50);

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
                    Value = 12,
                    SecondaryValue = 0f,
                    Description = "Body slams forward"
                },
                new BattleBossAction
                {
                    ActionType = BattleActionType.Attack,
                    Value = 14,
                    SecondaryValue = 0f,
                    Description = "Roots crash down in a heavy sweep"
                }
            };

            asset.Configure(
                "enemy.moss.shell",
                "Moss Shell",
                64,
                WorkshopElementAttribute.Earth,
                BattleEncounterType.Enemy,
                3,
                BattleEnemyArchetype.Defensive,
                0.45f,
                6,
                3,
                pattern,
                "reward.enemy.earth.minor",
                tokenReward: 80);

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
                    Value = 10,
                    SecondaryValue = 0f,
                    Description = "Drains a thread of vitality"
                },
                new BattleBossAction
                {
                    ActionType = BattleActionType.Heal,
                    Value = 5,
                    SecondaryValue = 0f,
                    Description = "Condenses moisture to recover"
                },
                new BattleBossAction
                {
                    ActionType = BattleActionType.Attack,
                    Value = 12,
                    SecondaryValue = 0f,
                    Description = "Lashes with a liquid tendril"
                }
            };

            asset.Configure(
                "enemy.mist.leech",
                "Mist Leech",
                52,
                WorkshopElementAttribute.Water,
                BattleEncounterType.Enemy,
                2,
                BattleEnemyArchetype.Sustain,
                0.35f,
                0,
                3,
                pattern,
                "reward.enemy.water.minor",
                tokenReward: 65);

            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static BattlePresentationProfile CreateEarthGolemPresentation()
        {
            string path = ContentPath + "/Presentation_EarthGolem.asset";
            BattlePresentationProfile asset = ScriptableObject.CreateInstance<BattlePresentationProfile>();
            asset.Configure(
                "boss.earth.golem",
                LoadSprite(CharacterSpritePath + "Boss_EarthGolem.png"),
                LoadAnimationProfile(CharacterAnimationPath + "Boss_EarthGolem/Boss_EarthGolem_Animation.asset"),
                LoadSprite(BackgroundSpritePath + "BG_EarthGolem.png"),
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
                LoadSprite(CharacterSpritePath + "Enemy_AshImp.png"),
                LoadAnimationProfile(CharacterAnimationPath + "Enemy_AshImp/Enemy_AshImp_Animation.asset"),
                LoadSprite(BackgroundSpritePath + "BG_AshImp.png"),
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
                LoadSprite(CharacterSpritePath + "Enemy_MossShell.png"),
                LoadAnimationProfile(CharacterAnimationPath + "Enemy_MossShell/Enemy_MossShell_Animation.asset"),
                LoadSprite(BackgroundSpritePath + "BG_MossShell.png"),
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
                LoadSprite(CharacterSpritePath + "Enemy_MistLeech.png"),
                LoadAnimationProfile(CharacterAnimationPath + "Enemy_MistLeech/Enemy_MistLeech_Animation.asset"),
                LoadSprite(BackgroundSpritePath + "BG_MistLeech.png"),
                new Vector3(3.5f, 0f, 0f),
                new Vector3(2.2f, 2.2f, 1f),
                new Vector3(6f, 6f, 1f));
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static BattleUnitAnimationProfile CreateEnemyAnimationProfile(string assetSlug, string spriteBaseName)
        {
            EnsureDirectory(CharacterAnimationPath.TrimEnd('/'));
            EnsureDirectory(CharacterAnimationPath + assetSlug);

            string assetPath = CharacterAnimationPath + assetSlug + "/" + assetSlug + "_Animation.asset";
            BattleUnitAnimationProfile asset = AssetDatabase.LoadAssetAtPath<BattleUnitAnimationProfile>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<BattleUnitAnimationProfile>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            Sprite[] idleFrames = LoadAnimationFrames(assetSlug, spriteBaseName, "Idle");
            Sprite[] attackFrames = LoadAnimationFrames(assetSlug, spriteBaseName, "Attack");
            Sprite[] hurtFrames = LoadAnimationFrames(assetSlug, spriteBaseName, "Hurt");

            BattleUnitAnimationProfile.AnimationSequence idleSequence = new BattleUnitAnimationProfile.AnimationSequence();
            idleSequence.Configure(idleFrames, 5f, true);

            BattleUnitAnimationProfile.AnimationSequence attackSequence = new BattleUnitAnimationProfile.AnimationSequence();
            attackSequence.Configure(attackFrames, 10f, false);

            BattleUnitAnimationProfile.AnimationSequence hurtSequence = new BattleUnitAnimationProfile.AnimationSequence();
            hurtSequence.Configure(hurtFrames, 10f, false);

            Sprite previewSprite = idleFrames != null && idleFrames.Length > 0
                ? idleFrames[0]
                : LoadSprite(CharacterSpritePath + spriteBaseName + ".png");

            asset.Configure(previewSprite, idleSequence, attackSequence, hurtSequence);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static Sprite LoadSprite(string assetPath)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static BattleUnitAnimationProfile LoadAnimationProfile(string assetPath)
        {
            return AssetDatabase.LoadAssetAtPath<BattleUnitAnimationProfile>(assetPath);
        }

        private static Sprite[] LoadAnimationFrames(string assetSlug, string spriteBaseName, string animationName)
        {
            string prefix = CharacterAnimationPath + assetSlug + "/" + spriteBaseName + "_" + animationName + "_";
            Sprite[] frames = new Sprite[4];

            for (int i = 0; i < frames.Length; i++)
            {
                frames[i] = LoadSprite(prefix + (i + 1).ToString("00") + ".png");
            }

            return frames;
        }

        private static BattleContentDatabase CreateContentDatabase(
            BattleBossDefinition[] bosses,
            BattleCardEffectTemplate[] templates,
            BattlePresentationProfile[] presentationProfiles,
            BattleCardDefinition[] definitions,
            BattleStatusEffectDefinition[] statusDefinitions)
        {
            string path = ContentPath + "/BattleContentDatabase.asset";
            BattleContentDatabase asset = ScriptableObject.CreateInstance<BattleContentDatabase>();
            asset.Configure(bosses, templates, presentationProfiles, definitions, statusDefinitions);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static BattleCardDefinition[] CreateAllCardDefinitions()
        {
            var definitions = new System.Collections.Generic.List<BattleCardDefinition>();

            // Basic spells (8)
            definitions.Add(CreateCardDefinition(
                "combat.spell.basic.fire", "Cinder Dart",
                WorkshopElementAttribute.Fire, WorkshopSpellTier.Basic,
                BattleEffectInstruction.Damage(8, 1),
                BattleEffectInstruction.ApplyStatus("Burn", 2, 1)));

            definitions.Add(CreateCardDefinition(
                "combat.spell.basic.water", "Tidal Mend",
                WorkshopElementAttribute.Water, WorkshopSpellTier.Basic,
                BattleEffectInstruction.Heal(4, 1),
                BattleEffectInstruction.ApplyStatus("Regen", 2, 5, BattleEffectTarget.Self)));

            definitions.Add(CreateCardDefinition(
                "combat.spell.basic.wind", "Zephyr Cut",
                WorkshopElementAttribute.Wind, WorkshopSpellTier.Basic,
                BattleEffectInstruction.Damage(5, 2),
                BattleEffectInstruction.ApplyStatus("Expose", 2, 10)));

            definitions.Add(CreateCardDefinition(
                "combat.spell.basic.earth", "Stoneguard Sigil",
                WorkshopElementAttribute.Earth, WorkshopSpellTier.Basic,
                BattleEffectInstruction.Shield(4, 1),
                BattleEffectInstruction.ApplyStatus("Bulwark", 2, 10, BattleEffectTarget.Self)));

            definitions.Add(CreateCardDefinition(
                "combat.spell.basic.ice", "Frost Pin",
                WorkshopElementAttribute.Ice, WorkshopSpellTier.Basic,
                BattleEffectInstruction.Damage(4, 2),
                BattleEffectInstruction.ApplyStatus("Slow", 2, 20)));

            definitions.Add(CreateCardDefinition(
                "combat.spell.basic.thunder", "Volt Javelin",
                WorkshopElementAttribute.Thunder, WorkshopSpellTier.Basic,
                BattleEffectInstruction.Damage(7, 1),
                BattleEffectInstruction.ApplyStatus("Shock", 2, 15)));

            definitions.Add(CreateCardDefinition(
                "combat.spell.basic.light", "Lumen Prayer",
                WorkshopElementAttribute.Light, WorkshopSpellTier.Basic,
                BattleEffectInstruction.Heal(3, 2),
                BattleEffectInstruction.ApplyStatus("Bless", 2, 8, BattleEffectTarget.Self)));

            definitions.Add(CreateCardDefinition(
                "combat.spell.basic.dark", "Gloam Ward",
                WorkshopElementAttribute.Dark, WorkshopSpellTier.Basic,
                BattleEffectInstruction.Shield(4, 1),
                BattleEffectInstruction.ApplyStatus("Veil", 2, 10, BattleEffectTarget.Self)));

            // Intermediate spells (8)
            definitions.Add(CreateCardDefinition(
                "combat.spell.intermediate.fire", "Inferno Brand",
                WorkshopElementAttribute.Fire, WorkshopSpellTier.Intermediate,
                BattleEffectInstruction.Damage(14, 2),
                BattleEffectInstruction.ApplyStatus("Burn", 3, 2)));

            definitions.Add(CreateCardDefinition(
                "combat.spell.intermediate.water", "Tide Chorus",
                WorkshopElementAttribute.Water, WorkshopSpellTier.Intermediate,
                BattleEffectInstruction.Heal(8, 2),
                BattleEffectInstruction.ApplyStatus("Regen", 3, 8, BattleEffectTarget.Self)));

            definitions.Add(CreateCardDefinition(
                "combat.spell.intermediate.wind", "Razor Monsoon",
                WorkshopElementAttribute.Wind, WorkshopSpellTier.Intermediate,
                BattleEffectInstruction.Damage(8, 3),
                BattleEffectInstruction.ApplyStatus("Expose", 3, 12)));

            definitions.Add(CreateCardDefinition(
                "combat.spell.intermediate.earth", "Bastion Pulse",
                WorkshopElementAttribute.Earth, WorkshopSpellTier.Intermediate,
                BattleEffectInstruction.Shield(9, 1),
                BattleEffectInstruction.ApplyStatus("Ward", 3, 16, BattleEffectTarget.Self)));

            definitions.Add(CreateCardDefinition(
                "combat.spell.intermediate.ice", "Glacier Bind",
                WorkshopElementAttribute.Ice, WorkshopSpellTier.Intermediate,
                BattleEffectInstruction.Damage(9, 2),
                BattleEffectInstruction.ApplyStatus("Freeze", 2, 24)));

            definitions.Add(CreateCardDefinition(
                "combat.spell.intermediate.thunder", "Stormbreaker",
                WorkshopElementAttribute.Thunder, WorkshopSpellTier.Intermediate,
                BattleEffectInstruction.Damage(16, 1),
                BattleEffectInstruction.ApplyStatus("Stun", 2, 18)));

            definitions.Add(CreateCardDefinition(
                "combat.spell.intermediate.light", "Dawn Benediction",
                WorkshopElementAttribute.Light, WorkshopSpellTier.Intermediate,
                BattleEffectInstruction.Heal(9, 3),
                BattleEffectInstruction.ApplyStatus("Radiance", 3, 18, BattleEffectTarget.Self)));

            definitions.Add(CreateCardDefinition(
                "combat.spell.intermediate.dark", "Umbral Bastion",
                WorkshopElementAttribute.Dark, WorkshopSpellTier.Intermediate,
                BattleEffectInstruction.Shield(8, 2),
                BattleEffectInstruction.ApplyStatus("Shade", 3, 16, BattleEffectTarget.Self)));

            // Advanced spells (4)
            definitions.Add(CreateCardDefinition(
                "combat.spell.advanced.prism", "Eclipse Covenant",
                WorkshopElementAttribute.Light, WorkshopSpellTier.Advanced,
                BattleEffectInstruction.Heal(14, 3),
                BattleEffectInstruction.ApplyStatus("Radiance", 3, 24, BattleEffectTarget.Self)));

            definitions.Add(CreateCardDefinition(
                "combat.spell.advanced.tempest", "Worldsplit Tempest",
                WorkshopElementAttribute.Wind, WorkshopSpellTier.Advanced,
                BattleEffectInstruction.Damage(12, 3),
                BattleEffectInstruction.ApplyStatus("Rend", 3, 20)));

            definitions.Add(CreateCardDefinition(
                "combat.spell.advanced.steam", "Steam Requiem",
                WorkshopElementAttribute.Fire, WorkshopSpellTier.Advanced,
                BattleEffectInstruction.Damage(20, 2),
                BattleEffectInstruction.ApplyStatus("Scald", 3, 28)));

            definitions.Add(CreateCardDefinition(
                "combat.spell.advanced.polarity", "Absolute Zero Surge",
                WorkshopElementAttribute.Ice, WorkshopSpellTier.Advanced,
                BattleEffectInstruction.Shield(16, 2),
                BattleEffectInstruction.ApplyStatus("Static Shell", 3, 35, BattleEffectTarget.Self)));

            // Final workshop fusion spells (4)
            definitions.Add(CreateCardDefinition(
                "combat.spell.ultimate.steam", "Boiling Star Requiem",
                WorkshopElementAttribute.Fire, WorkshopSpellTier.Advanced,
                BattleEffectInstruction.Damage(30, 2),
                BattleEffectInstruction.ApplyStatus("Scald", 3, 36)));

            definitions.Add(CreateCardDefinition(
                "combat.spell.ultimate.tempest", "Heavenbreaker Tempest",
                WorkshopElementAttribute.Wind, WorkshopSpellTier.Advanced,
                BattleEffectInstruction.Damage(18, 4),
                BattleEffectInstruction.ApplyStatus("Rend", 3, 28)));

            definitions.Add(CreateCardDefinition(
                "combat.spell.ultimate.prism", "Eclipse Apotheosis",
                WorkshopElementAttribute.Light, WorkshopSpellTier.Advanced,
                BattleEffectInstruction.Heal(20, 4),
                BattleEffectInstruction.ApplyStatus("Radiance", 3, 32, BattleEffectTarget.Self)));

            definitions.Add(CreateCardDefinition(
                "combat.spell.ultimate.polarity", "Zero Point Citadel",
                WorkshopElementAttribute.Ice, WorkshopSpellTier.Advanced,
                BattleEffectInstruction.Shield(24, 3),
                BattleEffectInstruction.ApplyStatus("Static Shell", 3, 42, BattleEffectTarget.Self)));

            // Runtime fallback workshop cards (3) — for current runtime compatibility
            definitions.Add(CreateCardDefinition(
                "combat.flame_bolt", "Flame Bolt",
                WorkshopElementAttribute.Fire, WorkshopSpellTier.Basic,
                BattleEffectInstruction.Damage(15, 1)));

            definitions.Add(CreateCardDefinition(
                "combat.frost_sigil", "Frost Sigil",
                WorkshopElementAttribute.Ice, WorkshopSpellTier.Basic,
                BattleEffectInstruction.Damage(12, 1)));

            definitions.Add(CreateCardDefinition(
                "combat.arcane_ward", "Arcane Ward",
                WorkshopElementAttribute.Earth, WorkshopSpellTier.Basic,
                BattleEffectInstruction.Shield(8)));

            return definitions.ToArray();
        }

        private static BattleCardDefinition CreateCardDefinition(
            string battleCardId,
            string displayName,
            WorkshopElementAttribute element,
            WorkshopSpellTier tier,
            params BattleEffectInstruction[] instructions)
        {
            string safeName = battleCardId.Replace(".", "_").Replace("/", "_");
            string path = ContentPath + "/CardDefinition_" + safeName + ".asset";
            BattleCardDefinition asset = ScriptableObject.CreateInstance<BattleCardDefinition>();
            asset.Configure(battleCardId, displayName, element, tier, instructions);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static BattleStatusEffectDefinition[] CreateAllStatusEffectDefinitions()
        {
            var statuses = new System.Collections.Generic.List<BattleStatusEffectDefinition>();

            statuses.Add(CreateStatusEffectDefinition(
                "Burn", "Burn",
                BattleStatusTrigger.OnTurnEnd,
                BattleEffectInstruction.Damage(1), false, 1));

            statuses.Add(CreateStatusEffectDefinition(
                "Regen", "Regen",
                BattleStatusTrigger.OnTurnStart,
                BattleEffectInstruction.Heal(1), false, 1));

            statuses.Add(CreateStatusEffectDefinition(
                "Expose", "Expose",
                BattleStatusTrigger.OnHitTaken,
                BattleEffectInstruction.Damage(0), false, 1));

            statuses.Add(CreateStatusEffectDefinition(
                "Bulwark", "Bulwark",
                BattleStatusTrigger.OnHitTaken,
                BattleEffectInstruction.Shield(0), true, 2));

            statuses.Add(CreateStatusEffectDefinition(
                "Slow", "Slow",
                BattleStatusTrigger.OnHitTaken,
                BattleEffectInstruction.Damage(0), false, 1));

            statuses.Add(CreateStatusEffectDefinition(
                "Shock", "Shock",
                BattleStatusTrigger.OnHitTaken,
                BattleEffectInstruction.Damage(0), false, 1));

            statuses.Add(CreateStatusEffectDefinition(
                "Bless", "Bless",
                BattleStatusTrigger.OnTurnStart,
                BattleEffectInstruction.Heal(1), false, 1));

            statuses.Add(CreateStatusEffectDefinition(
                "Veil", "Veil",
                BattleStatusTrigger.OnHitTaken,
                BattleEffectInstruction.Shield(0), false, 1));

            statuses.Add(CreateStatusEffectDefinition(
                "Ward", "Ward",
                BattleStatusTrigger.OnShieldBroken,
                BattleEffectInstruction.Shield(0), false, 1));

            statuses.Add(CreateStatusEffectDefinition(
                "Freeze", "Freeze",
                BattleStatusTrigger.OnTurnStart,
                BattleEffectInstruction.Damage(0), false, 1));

            statuses.Add(CreateStatusEffectDefinition(
                "Stun", "Stun",
                BattleStatusTrigger.OnTurnStart,
                BattleEffectInstruction.Damage(0), false, 1));

            statuses.Add(CreateStatusEffectDefinition(
                "Radiance", "Radiance",
                BattleStatusTrigger.OnTurnStart,
                BattleEffectInstruction.Heal(1), true, 2));

            statuses.Add(CreateStatusEffectDefinition(
                "Shade", "Shade",
                BattleStatusTrigger.OnHitTaken,
                BattleEffectInstruction.Shield(0), true, 2));

            statuses.Add(CreateStatusEffectDefinition(
                "Scald", "Scald",
                BattleStatusTrigger.OnTurnEnd,
                BattleEffectInstruction.Damage(1), false, 1));

            statuses.Add(CreateStatusEffectDefinition(
                "Rend", "Rend",
                BattleStatusTrigger.OnShieldBroken,
                BattleEffectInstruction.Damage(0), false, 1));

            statuses.Add(CreateStatusEffectDefinition(
                "Static Shell", "Static Shell",
                BattleStatusTrigger.OnHitTaken,
                BattleEffectInstruction.Damage(0), false, 1));

            return statuses.ToArray();
        }

        private static BattleStatusEffectDefinition CreateStatusEffectDefinition(
            string statusId,
            string displayName,
            BattleStatusTrigger trigger,
            BattleEffectInstruction tickEffect,
            bool stackable,
            int maxStacks)
        {
            string safeName = statusId.Replace(".", "_").Replace("/", "_").Replace(" ", "_");
            string path = ContentPath + "/StatusEffectDefinition_" + safeName + ".asset";
            BattleStatusEffectDefinition asset = ScriptableObject.CreateInstance<BattleStatusEffectDefinition>();
            asset.Configure(statusId, displayName, trigger, tickEffect, stackable, maxStacks);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
#endif
