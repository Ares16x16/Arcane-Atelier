using System.Collections.Generic;
using ArcaneAtelier.Workshop;
using NUnit.Framework;

namespace ArcaneAtelier.Battle.Tests
{
    public sealed class BattleWorkshopPayloadIntegrationTests
    {
        [Test]
        public void RuntimeWorkshopFallbackCards_MapToBattleDefinitionsWithStatuses()
        {
            WorkshopContentDatabase workshopDatabase = WorkshopDefaultContentFactory.CreateRuntimeDatabase();
            WorkshopItemDefinition windCard = FindCardById(workshopDatabase, "spell.basic.wind");

            Assert.That(windCard, Is.Not.Null, "Fallback workshop content should expose the basic wind card.");
            Assert.That(windCard.BattleCardId, Is.EqualTo("combat.spell.basic.wind"));
            Assert.That(windCard.EffectKeyword, Is.EqualTo("Expose"));

            BattleContentDatabase battleDatabase = UnityEngine.ScriptableObject.CreateInstance<BattleContentDatabase>();
            BattleCardDefinition windDefinition = UnityEngine.ScriptableObject.CreateInstance<BattleCardDefinition>();
            windDefinition.Configure(
                "combat.spell.basic.wind",
                "Zephyr Cut",
                WorkshopElementAttribute.Wind,
                WorkshopSpellTier.Basic,
                new[]
                {
                    BattleEffectInstruction.Damage(5, 2),
                    BattleEffectInstruction.ApplyStatus("Expose", 2, 10)
                });
            battleDatabase.Configure(
                new BattleBossDefinition[0],
                new BattleCardEffectTemplate[0],
                new BattlePresentationProfile[0],
                new[] { windDefinition },
                new BattleStatusEffectDefinition[0]);

            WorkshopBattlePayload payload = new WorkshopBattlePayload
            {
                Cards = new List<WorkshopBattleCardEntry>
                {
                    new WorkshopBattleCardEntry
                    {
                        CardId = windCard.BattleCardId,
                        DisplayName = windCard.DisplayName,
                        Amount = 1,
                        Element = windCard.Element,
                        Tier = windCard.SpellTier,
                        Role = windCard.SpellRole,
                        Rarity = windCard.Rarity,
                        PrimaryValue = windCard.EffectPrimaryValue,
                        HitCount = windCard.EffectHitCount,
                        SecondaryValue = windCard.EffectSecondaryValue,
                        EffectKeyword = windCard.EffectKeyword
                    }
                }
            };

            BattleDeckController deck = new BattleDeckController(battleDatabase, payload);

            Assert.That(deck.HandCount, Is.EqualTo(1));
            Assert.That(deck.TryPlayCard(0, out _), Is.True);
            Assert.That(deck.LastPlayedDefinition, Is.Not.Null);
            Assert.That(deck.LastPlayedDefinition.BattleCardId, Is.EqualTo("combat.spell.basic.wind"));
            Assert.That(deck.LastPlayedDefinition.Instructions.Count, Is.EqualTo(2));
            Assert.That(deck.LastPlayedDefinition.Instructions[1].Type, Is.EqualTo(BattleEffectType.ApplyStatus));
            Assert.That(deck.LastPlayedDefinition.Instructions[1].StatusId, Is.EqualTo("Expose"));
        }

        private static WorkshopItemDefinition FindCardById(WorkshopContentDatabase database, string id)
        {
            foreach (WorkshopNodeDefinition node in database.PlaceableNodes)
            {
                if (node == null)
                {
                    continue;
                }

                foreach (WorkshopProductionRecipe recipe in node.Recipes)
                {
                    if (recipe == null)
                    {
                        continue;
                    }

                    foreach (WorkshopItemStack output in recipe.Outputs)
                    {
                        WorkshopItemDefinition card = output?.Item;
                        if (card != null && card.Id == id)
                        {
                            return card;
                        }
                    }
                }
            }

            return null;
        }
    }
}
