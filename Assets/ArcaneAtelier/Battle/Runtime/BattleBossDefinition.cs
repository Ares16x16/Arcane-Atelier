using System;
using System.Collections.Generic;
using ArcaneAtelier.Workshop;
using UnityEngine;

namespace ArcaneAtelier.Battle
{
    [CreateAssetMenu(menuName = "Arcane Atelier/Battle/Boss Definition", fileName = "BossDefinition")]
    public sealed class BattleBossDefinition : ScriptableObject
    {
        [SerializeField] private string bossId = "boss.id";
        [SerializeField] private string displayName = "Boss";
        [SerializeField] private int maxHealth = 100;
        [SerializeField] private WorkshopElementAttribute element = WorkshopElementAttribute.Earth;
        [SerializeField] private BattleEncounterType encounterType = BattleEncounterType.Boss;
        [SerializeField] private int difficultyRank = 100;
        [SerializeField] private BattleEnemyArchetype enemyArchetype = BattleEnemyArchetype.None;
        [SerializeField, Min(0f)] private float lowHealthThresholdNormalized = 0.4f;
        [SerializeField, Min(0)] private int defensiveShieldThreshold = 10;
        [SerializeField, Min(1)] private int preferredBurstTurnInterval = 3;
        [SerializeField] private BattleBossAction[] actionPattern;
        [SerializeField] private BattleBossAction[] phase2ActionPattern;
        [SerializeField, Range(0f, 1f)] private float phaseTransitionHealthPercent = 0.5f;
        [SerializeField] private string defeatRewardId = "";

        public string BossId => bossId;
        public string DisplayName => displayName;
        public int MaxHealth => maxHealth;
        public WorkshopElementAttribute Element => element;
        public BattleEncounterType EncounterType => encounterType;
        public bool IsBoss => encounterType == BattleEncounterType.Boss;
        public bool IsEnemy => encounterType == BattleEncounterType.Enemy;
        public int DifficultyRank => difficultyRank;
        public BattleEnemyArchetype EnemyArchetype => enemyArchetype;
        public float LowHealthThresholdNormalized => lowHealthThresholdNormalized;
        public int DefensiveShieldThreshold => defensiveShieldThreshold;
        public int PreferredBurstTurnInterval => preferredBurstTurnInterval;

        public IReadOnlyList<BattleBossAction> Phase2ActionPattern
        {
            get
            {
                if (phase2ActionPattern == null)
                {
                    return Array.Empty<BattleBossAction>();
                }
                return phase2ActionPattern;
            }
        }

        public float PhaseTransitionHealthPercent => phaseTransitionHealthPercent;

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
            BattleEncounterType type,
            int difficulty,
            BattleEnemyArchetype archetype,
            float lowHealthThreshold,
            int shieldThreshold,
            int burstTurnInterval,
            BattleBossAction[] pattern,
            string rewardId,
            BattleBossAction[] phase2Pattern = null,
            float phaseTransitionPercent = 0.5f)
        {
            bossId = id;
            displayName = name;
            maxHealth = Mathf.Max(1, health);
            element = bossElement;
            encounterType = type;
            difficultyRank = Mathf.Max(0, difficulty);
            enemyArchetype = type == BattleEncounterType.Enemy ? archetype : BattleEnemyArchetype.None;
            lowHealthThresholdNormalized = Mathf.Clamp01(lowHealthThreshold);
            defensiveShieldThreshold = Mathf.Max(0, shieldThreshold);
            preferredBurstTurnInterval = Mathf.Max(1, burstTurnInterval);
            actionPattern = pattern ?? Array.Empty<BattleBossAction>();
            defeatRewardId = rewardId ?? string.Empty;
            phase2ActionPattern = phase2Pattern ?? Array.Empty<BattleBossAction>();
            phaseTransitionHealthPercent = Mathf.Clamp01(phaseTransitionPercent);
        }
    }
}
