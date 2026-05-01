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
        [SerializeField] private BattleCardDefinition[] cardDefinitions;
        [SerializeField] private BattleStatusEffectDefinition[] statusEffectDefinitions;

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

        public IReadOnlyList<BattleCardDefinition> CardDefinitions
        {
            get
            {
                if (cardDefinitions == null)
                {
                    return Array.Empty<BattleCardDefinition>();
                }
                return cardDefinitions;
            }
        }

        public IReadOnlyList<BattleStatusEffectDefinition> StatusEffectDefinitions
        {
            get
            {
                if (statusEffectDefinitions == null)
                {
                    return Array.Empty<BattleStatusEffectDefinition>();
                }
                return statusEffectDefinitions;
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

        public BattleCardDefinition FindCardDefinition(string battleCardId)
        {
            foreach (BattleCardDefinition definition in CardDefinitions)
            {
                if (definition != null && definition.BattleCardId == battleCardId)
                {
                    return definition;
                }
            }
            return null;
        }

        public BattleStatusEffectDefinition FindStatusEffectDefinition(string statusId)
        {
            foreach (BattleStatusEffectDefinition definition in StatusEffectDefinitions)
            {
                if (definition != null && definition.StatusId == statusId)
                {
                    return definition;
                }
            }
            return null;
        }

        public void Configure(
            BattleBossDefinition[] bossList,
            BattleCardEffectTemplate[] templateList,
            BattlePresentationProfile[] profileList,
            BattleCardDefinition[] definitionList = null,
            BattleStatusEffectDefinition[] statusList = null)
        {
            bosses = bossList ?? Array.Empty<BattleBossDefinition>();
            cardEffectTemplates = templateList ?? Array.Empty<BattleCardEffectTemplate>();
            presentationProfiles = profileList ?? Array.Empty<BattlePresentationProfile>();
            cardDefinitions = definitionList ?? Array.Empty<BattleCardDefinition>();
            statusEffectDefinitions = statusList ?? Array.Empty<BattleStatusEffectDefinition>();
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

            HashSet<string> definitionIds = new HashSet<string>();
            foreach (BattleCardDefinition definition in CardDefinitions)
            {
                if (definition == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(definition.BattleCardId))
                {
                    errors.Add($"Card definition '{definition.name}' has an empty BattleCardId.");
                }
                else if (!definitionIds.Add(definition.BattleCardId))
                {
                    errors.Add($"Duplicate BattleCardId in definitions: '{definition.BattleCardId}'.");
                }
            }

            HashSet<string> statusIds = new HashSet<string>();
            foreach (BattleStatusEffectDefinition status in StatusEffectDefinitions)
            {
                if (status == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(status.StatusId))
                {
                    errors.Add($"Status effect definition '{status.name}' has an empty StatusId.");
                }
                else if (!statusIds.Add(status.StatusId))
                {
                    errors.Add($"Duplicate StatusId in status definitions: '{status.StatusId}'.");
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
