using System;
using System.Collections.Generic;
using ArcaneAtelier.Workshop;
using UnityEngine;

namespace ArcaneAtelier.Battle
{
    [CreateAssetMenu(menuName = "Arcane Atelier/Battle/Content Database", fileName = "BattleContentDatabase")]
    public sealed class BattleContentDatabase : ScriptableObject
    {
        [SerializeField] private BattleBossDefinition[] bosses;
        [SerializeField] private BattleCardEffectTemplate[] cardEffectTemplates;
        [SerializeField] private BattlePresentationProfile[] presentationProfiles;

        public IReadOnlyList<BattleBossDefinition> Bosses
        {
            get
            {
                if (bosses == null)
                {
                    return Array.Empty<BattleBossDefinition>();
                }
                return bosses;
            }
        }

        public IReadOnlyList<BattleCardEffectTemplate> CardEffectTemplates
        {
            get
            {
                if (cardEffectTemplates == null)
                {
                    return Array.Empty<BattleCardEffectTemplate>();
                }
                return cardEffectTemplates;
            }
        }

        public IReadOnlyList<BattlePresentationProfile> PresentationProfiles
        {
            get
            {
                if (presentationProfiles == null)
                {
                    return Array.Empty<BattlePresentationProfile>();
                }
                return presentationProfiles;
            }
        }

        public BattleBossDefinition FindBoss(string bossId)
        {
            foreach (BattleBossDefinition boss in Bosses)
            {
                if (boss != null && boss.BossId == bossId)
                {
                    return boss;
                }
            }
            return null;
        }

        public BattleBossDefinition FindEnemyByDifficultyRank(int difficultyRank)
        {
            BattleBossDefinition bestMatch = null;
            foreach (BattleBossDefinition boss in Bosses)
            {
                if (boss == null || !boss.IsEnemy || boss.DifficultyRank != difficultyRank)
                {
                    continue;
                }

                if (bestMatch == null || string.CompareOrdinal(boss.BossId, bestMatch.BossId) < 0)
                {
                    bestMatch = boss;
                }
            }

            return bestMatch;
        }

        public BattleBossDefinition FindLowestDifficultyEnemy()
        {
            BattleBossDefinition bestMatch = null;
            foreach (BattleBossDefinition boss in Bosses)
            {
                if (boss == null || !boss.IsEnemy)
                {
                    continue;
                }

                if (bestMatch == null || boss.DifficultyRank < bestMatch.DifficultyRank)
                {
                    bestMatch = boss;
                }
            }

            return bestMatch;
        }

        public BattleCardEffectTemplate FindTemplate(string cardId)
        {
            foreach (BattleCardEffectTemplate template in CardEffectTemplates)
            {
                if (template != null && template.CardId == cardId)
                {
                    return template;
                }
            }
            return null;
        }

        public BattleCardEffectTemplate FindTemplateByRole(WorkshopSpellRole role)
        {
            foreach (BattleCardEffectTemplate template in CardEffectTemplates)
            {
                if (template != null && template.Role == role)
                {
                    return template;
                }
            }
            return null;
        }

        public BattlePresentationProfile FindPresentationProfile(string bossId)
        {
            foreach (BattlePresentationProfile profile in PresentationProfiles)
            {
                if (profile != null && profile.BossId == bossId)
                {
                    return profile;
                }
            }
            return null;
        }

        public void Configure(
            BattleBossDefinition[] bossList,
            BattleCardEffectTemplate[] templateList,
            BattlePresentationProfile[] profileList)
        {
            bosses = bossList ?? Array.Empty<BattleBossDefinition>();
            cardEffectTemplates = templateList ?? Array.Empty<BattleCardEffectTemplate>();
            presentationProfiles = profileList ?? Array.Empty<BattlePresentationProfile>();
        }

        public IReadOnlyList<string> ValidateContent()
        {
            List<string> errors = new List<string>();
            HashSet<string> bossIds = new HashSet<string>();

            foreach (BattleBossDefinition boss in Bosses)
            {
                if (boss == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(boss.BossId))
                {
                    errors.Add($"Boss '{boss.name}' has an empty BossId.");
                }
                else if (!bossIds.Add(boss.BossId))
                {
                    errors.Add($"Duplicate BossId detected: '{boss.BossId}'.");
                }

                if (boss.IsEnemy && boss.EnemyArchetype == BattleEnemyArchetype.None)
                {
                    errors.Add($"Enemy '{boss.name}' must define an EnemyArchetype.");
                }
            }

            HashSet<string> templateIds = new HashSet<string>();
            foreach (BattleCardEffectTemplate template in CardEffectTemplates)
            {
                if (template == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(template.CardId))
                {
                    errors.Add($"Card template '{template.name}' has an empty CardId.");
                }
                else if (!templateIds.Add(template.CardId))
                {
                    errors.Add($"Duplicate CardId in templates: '{template.CardId}'.");
                }
            }

            HashSet<string> presentationBossIds = new HashSet<string>();
            foreach (BattlePresentationProfile profile in PresentationProfiles)
            {
                if (profile == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(profile.BossId))
                {
                    errors.Add($"Presentation profile '{profile.name}' has an empty BossId.");
                }
                else if (!presentationBossIds.Add(profile.BossId))
                {
                    errors.Add($"Duplicate BossId in presentation profiles: '{profile.BossId}'.");
                }
            }

            return errors;
        }

        private void OnValidate()
        {
            IReadOnlyList<string> errors = ValidateContent();
            if (errors.Count == 0)
            {
                LogPresentationWarnings();
            }
            else
            {
                Debug.LogWarning(
                    $"BattleContentDatabase '{name}' validation found {errors.Count} issue(s):\n- {string.Join("\n- ", errors)}",
                    this);
            }
        }

        private void LogPresentationWarnings()
        {
            List<string> warnings = new List<string>();
            foreach (BattlePresentationProfile profile in PresentationProfiles)
            {
                if (profile == null)
                {
                    continue;
                }

                if (profile.BossSprite == null)
                {
                    warnings.Add($"Presentation profile '{profile.name}' has no BossSprite. Scene fallback will be used.");
                }

                if (profile.BackgroundSprite == null)
                {
                    warnings.Add($"Presentation profile '{profile.name}' has no BackgroundSprite. Scene fallback will be used.");
                }
            }

            if (warnings.Count > 0)
            {
                Debug.LogWarning(
                    $"BattleContentDatabase '{name}' presentation warnings:\n- {string.Join("\n- ", warnings)}",
                    this);
            }
        }
    }
}
