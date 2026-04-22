using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArcaneAtelier.Battle
{
    [CreateAssetMenu(menuName = "Arcane Atelier/Battle/Content Database", fileName = "BattleContentDatabase")]
    public sealed class BattleContentDatabase : ScriptableObject
    {
        [SerializeField] private BattleBossDefinition[] bosses;
        [SerializeField] private BattleCardEffectTemplate[] cardEffectTemplates;

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

        public void Configure(BattleBossDefinition[] bossList, BattleCardEffectTemplate[] templateList)
        {
            bosses = bossList ?? Array.Empty<BattleBossDefinition>();
            cardEffectTemplates = templateList ?? Array.Empty<BattleCardEffectTemplate>();
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

            return errors;
        }

        private void OnValidate()
        {
            IReadOnlyList<string> errors = ValidateContent();
            if (errors.Count == 0)
            {
                return;
            }

            Debug.LogWarning(
                $"BattleContentDatabase '{name}' validation found {errors.Count} issue(s):\n- {string.Join("\n- ", errors)}",
                this);
        }
    }
}
