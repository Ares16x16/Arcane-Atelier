using NUnit.Framework;
using UnityEngine;
using ArcaneAtelier.Workshop;

namespace ArcaneAtelier.Battle.Tests
{
    public sealed class BattleRewardCalculatorTests
    {
        private static BattleBossDefinition CreateBoss(int tokenReward)
        {
            BattleBossDefinition boss = ScriptableObject.CreateInstance<BattleBossDefinition>();
            boss.hideFlags = HideFlags.HideAndDontSave;
            boss.Configure(
                "boss.test",
                "Test Boss",
                100,
                WorkshopElementAttribute.Fire,
                BattleEncounterType.Boss,
                10,
                BattleEnemyArchetype.None,
                0.4f,
                0,
                3,
                System.Array.Empty<BattleBossAction>(),
                "",
                null,
                0.5f,
                tokenReward);
            return boss;
        }

        [Test]
        public void Compute_Defeat_ReturnsZero()
        {
            BattleResult result = new BattleResult
            {
                ResultType = BattleResultType.Defeat,
                PlayerFinalHealth = 0,
                PlayerMaxHealth = 100,
                TurnsElapsed = 5
            };

            int tokens = BattleRewardCalculator.Compute(result, CreateBoss(50));

            Assert.That(tokens, Is.EqualTo(0));
        }

        [Test]
        public void Compute_NullResult_ReturnsZero()
        {
            int tokens = BattleRewardCalculator.Compute(null, CreateBoss(50));
            Assert.That(tokens, Is.EqualTo(0));
        }

        [Test]
        public void Compute_FullHealthSpeedKill_AppliesAllBonuses()
        {
            BattleResult result = new BattleResult
            {
                ResultType = BattleResultType.Victory,
                PlayerFinalHealth = 100,
                PlayerMaxHealth = 100,
                TurnsElapsed = 5
            };

            int tokens = BattleRewardCalculator.Compute(result, CreateBoss(50));

            // base 50 + health 25 (50*0.5*1.0) + speed 30 ((20-5)*2) + untouched 30 = 135
            Assert.That(tokens, Is.EqualTo(135));
        }

        [Test]
        public void Compute_LowHealthLongFight_OnlyBaseReward()
        {
            BattleResult result = new BattleResult
            {
                ResultType = BattleResultType.Victory,
                PlayerFinalHealth = 5,
                PlayerMaxHealth = 100,
                TurnsElapsed = 30
            };

            int tokens = BattleRewardCalculator.Compute(result, CreateBoss(50));

            // base 50 + health 1 (50*0.5*0.05) + speed 0 + untouched 0 = 51
            Assert.That(tokens, Is.EqualTo(51));
        }

        [Test]
        public void Compute_NullBoss_UsesDefaultBase()
        {
            BattleResult result = new BattleResult
            {
                ResultType = BattleResultType.Victory,
                PlayerFinalHealth = 100,
                PlayerMaxHealth = 100,
                TurnsElapsed = 10
            };

            int tokens = BattleRewardCalculator.Compute(result, null);

            // default base 50 + health 25 + speed 20 ((20-10)*2) + untouched 30 = 125
            Assert.That(tokens, Is.EqualTo(125));
        }
    }
}
