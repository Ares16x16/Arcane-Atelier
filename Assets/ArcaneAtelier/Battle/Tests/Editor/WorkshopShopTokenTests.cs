using NUnit.Framework;
using UnityEngine;
using ArcaneAtelier.Workshop;

namespace ArcaneAtelier.Battle.Tests
{
    public sealed class WorkshopShopTokenTests
    {
        private static WorkshopSimulation CreateSimulation()
        {
            WorkshopContentDatabase database = WorkshopDefaultContentFactory.CreateRuntimeDatabase();
            return new WorkshopSimulation(database);
        }

        [Test]
        public void NewSimulation_StartsWithZeroTokens()
        {
            WorkshopSimulation sim = CreateSimulation();
            Assert.That(sim.Tokens, Is.EqualTo(0));
        }

        [Test]
        public void AddTokens_IncreasesWallet()
        {
            WorkshopSimulation sim = CreateSimulation();
            sim.AddTokens(50);
            Assert.That(sim.Tokens, Is.EqualTo(50));
        }

        [Test]
        public void AddTokens_NegativeOrZero_IsIgnored()
        {
            WorkshopSimulation sim = CreateSimulation();
            sim.AddTokens(10);
            sim.AddTokens(0);
            sim.AddTokens(-5);
            Assert.That(sim.Tokens, Is.EqualTo(10));
        }

        [Test]
        public void TrySpendTokens_SufficientFunds_SucceedsAndDeducts()
        {
            WorkshopSimulation sim = CreateSimulation();
            sim.AddTokens(50);

            bool spent = sim.TrySpendTokens(20);

            Assert.That(spent, Is.True);
            Assert.That(sim.Tokens, Is.EqualTo(30));
        }

        [Test]
        public void TrySpendTokens_InsufficientFunds_ReturnsFalseAndKeepsBalance()
        {
            WorkshopSimulation sim = CreateSimulation();
            sim.AddTokens(10);

            bool spent = sim.TrySpendTokens(50);

            Assert.That(spent, Is.False);
            Assert.That(sim.Tokens, Is.EqualTo(10));
        }

        [Test]
        public void TrySpendTokens_NonPositive_ReturnsFalse()
        {
            WorkshopSimulation sim = CreateSimulation();
            sim.AddTokens(10);

            Assert.That(sim.TrySpendTokens(0), Is.False);
            Assert.That(sim.TrySpendTokens(-1), Is.False);
            Assert.That(sim.Tokens, Is.EqualTo(10));
        }

        [Test]
        public void RewardDefinition_TokenCostDefault_IsZero()
        {
            WorkshopRewardDefinition reward = ScriptableObject.CreateInstance<WorkshopRewardDefinition>();
            reward.hideFlags = HideFlags.HideAndDontSave;
            reward.Configure(
                "test.reward",
                "Test",
                "Test",
                WorkshopRewardKind.GrantItems,
                null,
                0f,
                System.Array.Empty<WorkshopItemStack>());

            Assert.That(reward.TokenCost, Is.EqualTo(0));
        }

        [Test]
        public void RewardDefinition_TokenCost_ConfiguredValueClampedNonNegative()
        {
            WorkshopRewardDefinition reward = ScriptableObject.CreateInstance<WorkshopRewardDefinition>();
            reward.hideFlags = HideFlags.HideAndDontSave;
            reward.Configure(
                "test.reward",
                "Test",
                "Test",
                WorkshopRewardKind.GrantItems,
                null,
                0f,
                System.Array.Empty<WorkshopItemStack>(),
                -10);

            Assert.That(reward.TokenCost, Is.EqualTo(0));
        }

        [Test]
        public void Snapshot_PreservesTokens()
        {
            WorkshopSimulation original = CreateSimulation();
            original.AddTokens(75);

            WorkshopRunStateSnapshot snapshot = original.CaptureRunState();
            WorkshopSimulation restored = CreateSimulation();
            restored.RestoreRunState(snapshot);

            Assert.That(snapshot.Tokens, Is.EqualTo(75));
            Assert.That(restored.Tokens, Is.EqualTo(75));
        }
    }
}
