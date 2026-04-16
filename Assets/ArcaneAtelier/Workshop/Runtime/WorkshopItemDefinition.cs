using UnityEngine;

namespace ArcaneAtelier.Workshop
{
    [CreateAssetMenu(menuName = "Arcane Atelier/Workshop/Item Definition", fileName = "ItemDefinition")]
    public sealed class WorkshopItemDefinition : ScriptableObject
    {
        [SerializeField] private string id = "item.id";
        [SerializeField] private string displayName = "Item";
        [SerializeField, TextArea] private string description = "Placeholder workshop item.";
        [SerializeField] private WorkshopItemKind kind = WorkshopItemKind.Resource;
        [SerializeField] private WorkshopElementAttribute element = WorkshopElementAttribute.None;
        [SerializeField] private WorkshopSpellTier spellTier = WorkshopSpellTier.None;
        [SerializeField] private WorkshopSpellRole spellRole = WorkshopSpellRole.None;
        [SerializeField, Min(0f)] private float rarityWeight = 1f;
        [SerializeField, Min(0)] private int effectPrimaryValue;
        [SerializeField, Min(1)] private int effectHitCount = 1;
        [SerializeField] private float effectSecondaryValue;
        [SerializeField] private string effectKeyword = string.Empty;
        [SerializeField] private Color tint = Color.white;
        [SerializeField] private string battleCardId = string.Empty;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public WorkshopItemKind Kind => kind;
        public WorkshopElementAttribute Element => element;
        public WorkshopSpellTier SpellTier => spellTier;
        public WorkshopSpellRole SpellRole => spellRole;
        public float RarityWeight => rarityWeight;
        public int EffectPrimaryValue => effectPrimaryValue;
        public int EffectHitCount => effectHitCount;
        public float EffectSecondaryValue => effectSecondaryValue;
        public string EffectKeyword => effectKeyword;
        public Color Tint => tint;
        public string BattleCardId => string.IsNullOrWhiteSpace(battleCardId) ? id : battleCardId;
        public bool IsSpellCard => kind == WorkshopItemKind.Card;
        public WorkshopSpellRarity Rarity => CalculateRarity(rarityWeight);

        public void Configure(
            string itemId,
            string itemDisplayName,
            string itemDescription,
            WorkshopItemKind itemKind,
            Color itemTint,
            string linkedBattleCardId = "",
            WorkshopElementAttribute itemElement = WorkshopElementAttribute.None,
            WorkshopSpellTier itemSpellTier = WorkshopSpellTier.None,
            WorkshopSpellRole itemSpellRole = WorkshopSpellRole.None,
            float itemRarityWeight = 1f,
            int itemEffectPrimaryValue = 0,
            int itemEffectHitCount = 1,
            float itemEffectSecondaryValue = 0f,
            string itemEffectKeyword = "")
        {
            id = itemId;
            displayName = itemDisplayName;
            description = itemDescription;
            kind = itemKind;
            element = itemElement;
            spellTier = itemKind == WorkshopItemKind.Card ? itemSpellTier : WorkshopSpellTier.None;
            spellRole = itemKind == WorkshopItemKind.Card ? itemSpellRole : WorkshopSpellRole.None;
            rarityWeight = Mathf.Max(0f, itemRarityWeight);
            effectPrimaryValue = Mathf.Max(0, itemEffectPrimaryValue);
            effectHitCount = Mathf.Max(1, itemEffectHitCount);
            effectSecondaryValue = itemEffectSecondaryValue;
            effectKeyword = itemEffectKeyword ?? string.Empty;
            tint = itemTint;
            battleCardId = linkedBattleCardId;
        }

        public string BuildEffectSummary()
        {
            if (!IsSpellCard)
            {
                return string.Empty;
            }

            return spellRole switch
            {
                WorkshopSpellRole.Attack => $"{effectPrimaryValue} dmg x{effectHitCount} {effectKeyword}".Trim(),
                WorkshopSpellRole.Healing => $"{effectPrimaryValue} heal x{effectHitCount} {effectKeyword}".Trim(),
                WorkshopSpellRole.Defense => $"+{effectPrimaryValue} guard x{effectHitCount} {effectKeyword} {effectSecondaryValue:0}%".Trim(),
                _ => string.Empty
            };
        }

        private static WorkshopSpellRarity CalculateRarity(float weight)
        {
            if (weight >= 85f)
            {
                return WorkshopSpellRarity.Legendary;
            }

            if (weight >= 55f)
            {
                return WorkshopSpellRarity.Epic;
            }

            if (weight >= 25f)
            {
                return WorkshopSpellRarity.Rare;
            }

            return WorkshopSpellRarity.Common;
        }
    }
}
