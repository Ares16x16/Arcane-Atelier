using System;
using System.Collections.Generic;
using ArcaneAtelier.Workshop;
using UnityEngine;

namespace ArcaneAtelier.Battle
{
    [CreateAssetMenu(menuName = "Arcane Atelier/Battle/Card Definition", fileName = "CardDefinition")]
    public sealed class BattleCardDefinition : ScriptableObject
    {
        [SerializeField] private string battleCardId = "";
        [SerializeField] private string displayName = "";
        [SerializeField] private WorkshopElementAttribute element = WorkshopElementAttribute.None;
        [SerializeField] private WorkshopSpellTier tier = WorkshopSpellTier.None;
        [SerializeField] private BattleEffectInstruction[] instructions = Array.Empty<BattleEffectInstruction>();

        public string BattleCardId => battleCardId;
        public string DisplayName => displayName;
        public WorkshopElementAttribute Element => element;
        public WorkshopSpellTier Tier => tier;
        public IReadOnlyList<BattleEffectInstruction> Instructions => instructions ?? Array.Empty<BattleEffectInstruction>();

        public void Configure(
            string id,
            string name,
            WorkshopElementAttribute cardElement,
            WorkshopSpellTier cardTier,
            BattleEffectInstruction[] effectInstructions)
        {
            battleCardId = id ?? string.Empty;
            displayName = name ?? string.Empty;
            element = cardElement;
            tier = cardTier;
            instructions = effectInstructions ?? Array.Empty<BattleEffectInstruction>();
        }
    }
}
