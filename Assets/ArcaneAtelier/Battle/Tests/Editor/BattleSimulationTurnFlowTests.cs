using NUnit.Framework;
using ArcaneAtelier.Workshop;

namespace ArcaneAtelier.Battle.Tests
{
    public sealed class BattleSimulationTurnFlowTests
    {
        [Test]
        public void EndTurn_DoesNotResolveBossActionImmediately()
        {
            BattleSimulation simulation = CreateSimulation();
            int bossResolutions = 0;

            simulation.BossActionResolved += _ => bossResolutions++;

            simulation.EndTurn();

            Assert.That(simulation.State, Is.EqualTo(BattleState.BossTurnPending));
            Assert.That(simulation.ActionPoints, Is.EqualTo(3));
            Assert.That(bossResolutions, Is.EqualTo(0));
        }

        [Test]
        public void BattleDeckController_InitialHandSize_IsFive()
        {
            BattleSimulation simulation = CreateSimulation();
            Assert.That(simulation.Deck.HandCount, Is.EqualTo(5));
        }

        [Test]
        public void BattleSimulation_UsesThreeActionPointsPerTurn()
        {
            BattleSimulation simulation = CreateSimulation();

            Assert.That(simulation.MaxActionPoints, Is.EqualTo(3));
            Assert.That(simulation.ActionPoints, Is.EqualTo(3));
        }

        [Test]
        public void BattleDeckController_ActionPointCosts_MatchDifficultyV2()
        {
            Assert.That(BattleDeckController.GetActionPointCost(WorkshopSpellRole.Attack), Is.EqualTo(1));
            Assert.That(BattleDeckController.GetActionPointCost(WorkshopSpellRole.Defense), Is.EqualTo(2));
            Assert.That(BattleDeckController.GetActionPointCost(WorkshopSpellRole.Healing), Is.EqualTo(2));
        }

        [Test]
        public void EnemyIntent_RemainsStableUntilExecution()
        {
            BattleUnit player = new BattleUnit
            {
                DisplayName = "Player",
                MaxHealth = 40,
                CurrentHealth = 40,
                Element = WorkshopElementAttribute.None
            };

            BattleUnit boss = new BattleUnit
            {
                DisplayName = "Ash Imp",
                MaxHealth = 30,
                CurrentHealth = 30,
                Element = WorkshopElementAttribute.Fire
            };

            BattleBossDefinition definition = UnityEngine.ScriptableObject.CreateInstance<BattleBossDefinition>();
            definition.Configure(
                "enemy.intent.test",
                "Intent Test Enemy",
                30,
                WorkshopElementAttribute.Fire,
                BattleEncounterType.Enemy,
                1,
                BattleEnemyArchetype.Aggressive,
                0.3f,
                0,
                3,
                new[]
                {
                    new BattleBossAction
                    {
                        ActionType = BattleActionType.Attack,
                        Value = 5,
                        SecondaryValue = 0f,
                        Description = "Strike A"
                    },
                    new BattleBossAction
                    {
                        ActionType = BattleActionType.Attack,
                        Value = 9,
                        SecondaryValue = 0f,
                        Description = "Strike B"
                    },
                    new BattleBossAction
                    {
                        ActionType = BattleActionType.Special,
                        Value = 7,
                        SecondaryValue = 0f,
                        Description = "Burst C"
                    }
                },
                "reward.test");

            BattleBossAI bossAI = new BattleBossAI(definition);
            bossAI.BindUnits(boss, player);

            BattleBossAction preview = bossAI.PeekNextAction();
            Assert.That(preview, Is.Not.Null);

            for (int i = 0; i < 12; i++)
            {
                BattleBossAction repeatedPreview = bossAI.PeekNextAction();
                Assert.That(repeatedPreview.ActionType, Is.EqualTo(preview.ActionType));
                Assert.That(repeatedPreview.Value, Is.EqualTo(preview.Value));
                Assert.That(repeatedPreview.Description, Is.EqualTo(preview.Description));
            }

            BattleBossAction executed = bossAI.ExecuteNextAction();
            Assert.That(executed.ActionType, Is.EqualTo(preview.ActionType));
            Assert.That(executed.Value, Is.EqualTo(preview.Value));
            Assert.That(executed.Description, Is.EqualTo(preview.Description));
        }

        private static BattleSimulation CreateSimulation()
        {
            BattleUnit player = new BattleUnit
            {
                DisplayName = "Player",
                MaxHealth = 40,
                CurrentHealth = 40,
                Element = WorkshopElementAttribute.None
            };

            BattleUnit boss = new BattleUnit
            {
                DisplayName = "Ash Imp",
                MaxHealth = 30,
                CurrentHealth = 30,
                Element = WorkshopElementAttribute.Fire
            };

            BattleBossDefinition definition = UnityEngine.ScriptableObject.CreateInstance<BattleBossDefinition>();
            definition.Configure(
                "enemy.test",
                "Test Enemy",
                30,
                WorkshopElementAttribute.Fire,
                BattleEncounterType.Enemy,
                1,
                BattleEnemyArchetype.Aggressive,
                0.3f,
                0,
                3,
                new[]
                {
                    new BattleBossAction
                    {
                        ActionType = BattleActionType.Attack,
                        Value = 5,
                        SecondaryValue = 0f,
                        Description = "Test strike"
                    }
                },
                "reward.test");

            BattleBossAI bossAI = new BattleBossAI(definition);
            bossAI.BindUnits(boss, player);

            WorkshopBattlePayload payload = new WorkshopBattlePayload
            {
                Cards = new System.Collections.Generic.List<WorkshopBattleCardEntry>
                {
                    new WorkshopBattleCardEntry
                    {
                        CardId = "combat.spell.basic.fire",
                        DisplayName = "Cinder Dart",
                        Amount = 1,
                        Element = WorkshopElementAttribute.Fire,
                        Role = WorkshopSpellRole.Attack,
                        PrimaryValue = 8,
                        HitCount = 1
                    },
                    new WorkshopBattleCardEntry
                    {
                        CardId = "combat.spell.basic.wind",
                        DisplayName = "Zephyr Cut",
                        Amount = 1,
                        Element = WorkshopElementAttribute.Wind,
                        Role = WorkshopSpellRole.Attack,
                        PrimaryValue = 5,
                        HitCount = 2
                    },
                    new WorkshopBattleCardEntry
                    {
                        CardId = "combat.spell.basic.ice",
                        DisplayName = "Frost Pin",
                        Amount = 1,
                        Element = WorkshopElementAttribute.Ice,
                        Role = WorkshopSpellRole.Attack,
                        PrimaryValue = 4,
                        HitCount = 2
                    },
                    new WorkshopBattleCardEntry
                    {
                        CardId = "combat.spell.basic.earth",
                        DisplayName = "Stoneguard Sigil",
                        Amount = 1,
                        Element = WorkshopElementAttribute.Earth,
                        Role = WorkshopSpellRole.Defense,
                        PrimaryValue = 6,
                        HitCount = 1
                    },
                    new WorkshopBattleCardEntry
                    {
                        CardId = "combat.spell.basic.water",
                        DisplayName = "Tidal Mend",
                        Amount = 1,
                        Element = WorkshopElementAttribute.Water,
                        Role = WorkshopSpellRole.Healing,
                        PrimaryValue = 5,
                        HitCount = 1
                    }
                }
            };

            BattleDeckController deck = new BattleDeckController(null, payload);
            return new BattleSimulation(player, boss, deck, bossAI, null);
        }
    }
}
