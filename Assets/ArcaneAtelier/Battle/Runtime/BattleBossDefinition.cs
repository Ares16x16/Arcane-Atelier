using System;
using ArcaneAtelier.Workshop;
using UnityEngine;
using System.Collections.Generic;

namespace ArcaneAtelier.Battle
{
    [CreateAssetMenu(menuName = "Arcane Atelier/Battle/Boss Definition", fileName = "BossDefinition")]
    public sealed class BattleBossDefinition : ScriptableObject
    {
        [SerializeField] private string bossId = "boss.id";
        [SerializeField] private string displayName = "Boss";
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private WorkshopElementAttribute element = WorkshopElementAttribute.Earth;
        [SerializeField] private BattleBossAction[] actionPattern;
        [SerializeField] private string defeatRewardId = "";

        public string BossId => bossId;
        public string DisplayName => displayName;
        public int MaxHealth => maxHealth;
        public WorkshopElementAttribute Element => element;

        public IReadOnlyList<BattleBossAction> ActionPattern
        {
            get
            {
                if (actionPattern == null)
                {
                    return Array.Empty<BattleBossAction>();
                }
                return actionPattern;
            }
        }

        public string DefeatRewardId => defeatRewardId;

        public void Configure(
            string id,
            string name,
            int health,
            WorkshopElementAttribute bossElement,
            BattleBossAction[] pattern,
            string rewardId)
        {
            bossId = id;
            displayName = name;
            maxHealth = Mathf.Max(1, health);
            element = bossElement;
            actionPattern = pattern ?? Array.Empty<BattleBossAction>();
            defeatRewardId = rewardId ?? string.Empty;
        }
    }
}
