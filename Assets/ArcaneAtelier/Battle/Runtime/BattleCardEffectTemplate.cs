using System;
using ArcaneAtelier.Workshop;
using UnityEngine;

namespace ArcaneAtelier.Battle
{
    [CreateAssetMenu(menuName = "Arcane Atelier/Battle/Card Effect Template", fileName = "CardEffectTemplate")]
    public sealed class BattleCardEffectTemplate : ScriptableObject
    {
        [SerializeField] private string cardId = "";
        [SerializeField] private WorkshopSpellRole role = WorkshopSpellRole.None;
        [SerializeField] private float primaryValueMultiplier = 1f;
        [SerializeField] private float secondaryValueMultiplier = 1f;
        [SerializeField] private string description = "";

        public string CardId => cardId;
        public WorkshopSpellRole Role => role;
        public float PrimaryValueMultiplier => primaryValueMultiplier;
        public float SecondaryValueMultiplier => secondaryValueMultiplier;
        public string Description => description;

        public void Configure(
            string templateCardId,
            WorkshopSpellRole templateRole,
            float primaryMultiplier,
            float secondaryMultiplier,
            string templateDescription)
        {
            cardId = templateCardId ?? string.Empty;
            role = templateRole;
            primaryValueMultiplier = primaryMultiplier;
            secondaryValueMultiplier = secondaryMultiplier;
            description = templateDescription ?? string.Empty;
        }

        public BattleResolvedEffect Resolve(WorkshopBattleCardEntry entry)
        {
            return new BattleResolvedEffect
            {
                Role = role,
                PrimaryValue = Mathf.RoundToInt(entry.PrimaryValue * primaryValueMultiplier),
                HitCount = Mathf.Max(1, entry.HitCount),
                SecondaryValue = entry.SecondaryValue * secondaryValueMultiplier,
                Element = entry.Element
            };
        }
    }

    [Serializable]
    public struct BattleResolvedEffect
    {
        public WorkshopSpellRole Role;
        public int PrimaryValue;
        public int HitCount;
        public float SecondaryValue;
        public WorkshopElementAttribute Element;
    }
}
