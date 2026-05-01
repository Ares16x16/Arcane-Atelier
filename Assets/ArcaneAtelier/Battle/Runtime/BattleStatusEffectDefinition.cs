using UnityEngine;

namespace ArcaneAtelier.Battle
{
    [CreateAssetMenu(menuName = "Arcane Atelier/Battle/Status Effect Definition", fileName = "StatusEffectDefinition")]
    public sealed class BattleStatusEffectDefinition : ScriptableObject
    {
        [SerializeField] private string statusId = "";
        [SerializeField] private string displayName = "";
        [SerializeField] private BattleStatusTrigger trigger = BattleStatusTrigger.OnTurnEnd;
        [SerializeField] private BattleEffectInstruction tickEffect;
        [SerializeField] private bool isStackable = false;
        [SerializeField] private int maxStackCount = 1;

        public string StatusId => statusId;
        public string DisplayName => displayName;
        public BattleStatusTrigger Trigger => trigger;
        public BattleEffectInstruction TickEffect => tickEffect;
        public bool IsStackable => isStackable;
        public int MaxStackCount => maxStackCount;

        public void Configure(
            string id,
            string name,
            BattleStatusTrigger statusTrigger,
            BattleEffectInstruction effect,
            bool stackable,
            int maxStacks)
        {
            statusId = id ?? string.Empty;
            displayName = name ?? string.Empty;
            trigger = statusTrigger;
            tickEffect = effect;
            isStackable = stackable;
            maxStackCount = Mathf.Max(1, maxStacks);
        }
    }
}
