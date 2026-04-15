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
        [SerializeField] private Color tint = Color.white;
        [SerializeField] private string battleCardId = string.Empty;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public WorkshopItemKind Kind => kind;
        public Color Tint => tint;
        public string BattleCardId => string.IsNullOrWhiteSpace(battleCardId) ? id : battleCardId;

        public void Configure(
            string itemId,
            string itemDisplayName,
            string itemDescription,
            WorkshopItemKind itemKind,
            Color itemTint,
            string linkedBattleCardId = "")
        {
            id = itemId;
            displayName = itemDisplayName;
            description = itemDescription;
            kind = itemKind;
            tint = itemTint;
            battleCardId = linkedBattleCardId;
        }
    }
}
