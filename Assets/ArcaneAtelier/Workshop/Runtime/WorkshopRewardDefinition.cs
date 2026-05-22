using System;
using UnityEngine;

namespace ArcaneAtelier.Workshop
{
    [CreateAssetMenu(menuName = "Arcane Atelier/Workshop/Reward Definition", fileName = "RewardDefinition")]
    public sealed class WorkshopRewardDefinition : ScriptableObject
    {
        [SerializeField] private string id = "reward.id";
        [SerializeField] private string displayName = "Reward";
        [SerializeField, TextArea] private string description = "Placeholder workshop reward.";
        [SerializeField] private WorkshopRewardKind rewardKind = WorkshopRewardKind.UnlockNode;
        [SerializeField] private WorkshopNodeDefinition targetNode;
        [SerializeField] private float efficiencyBonus = 0.25f;
        [SerializeField] private WorkshopItemStack[] grantedItems;
        [SerializeField, Min(0)] private int tokenCost = 0;

        public string Id => id;
        public string DisplayName => displayName;
        public string Description => description;
        public WorkshopRewardKind RewardKind => rewardKind;
        public WorkshopNodeDefinition TargetNode => targetNode;
        public float EfficiencyBonus => efficiencyBonus;
        public WorkshopItemStack[] GrantedItems => grantedItems;
        public int TokenCost => tokenCost;

        public void Configure(
            string rewardId,
            string rewardDisplayName,
            string rewardDescription,
            WorkshopRewardKind kind,
            WorkshopNodeDefinition nodeTarget,
            float bonus,
            WorkshopItemStack[] items,
            int cost = 0)
        {
            id = rewardId;
            displayName = rewardDisplayName;
            description = rewardDescription;
            rewardKind = kind;
            targetNode = nodeTarget;
            efficiencyBonus = bonus;
            grantedItems = items ?? Array.Empty<WorkshopItemStack>();
            tokenCost = Mathf.Max(0, cost);
        }
    }
}
