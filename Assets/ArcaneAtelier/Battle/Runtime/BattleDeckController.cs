using System;
using System.Collections.Generic;
using ArcaneAtelier.Workshop;
using UnityEngine;

namespace ArcaneAtelier.Battle
{
    public sealed class BattleDeckController
    {
        private readonly BattleContentDatabase contentDatabase;
        private readonly List<WorkshopBattleCardEntry> drawPile = new List<WorkshopBattleCardEntry>();
        private readonly List<WorkshopBattleCardEntry> hand = new List<WorkshopBattleCardEntry>();
        private readonly List<WorkshopBattleCardEntry> discardPile = new List<WorkshopBattleCardEntry>();
        private readonly System.Random rng = new System.Random();

        public IReadOnlyList<WorkshopBattleCardEntry> Hand => hand;
        public int HandCount => hand.Count;
        public int DrawPileCount => drawPile.Count;
        public int DiscardPileCount => discardPile.Count;
        public BattleCardDefinition LastPlayedDefinition { get; private set; }

        public BattleDeckController(BattleContentDatabase database, WorkshopBattlePayload payload)
        {
            contentDatabase = database;
            InitializeDeck(payload);
            DrawCards(5);
        }

        private void InitializeDeck(WorkshopBattlePayload payload)
        {
            if (payload != null && payload.HasCards)
            {
                foreach (WorkshopBattleCardEntry entry in payload.Cards)
                {
                    int amount = Mathf.Max(0, entry.Amount);
                    for (int i = 0; i < amount; i++)
                    {
                        drawPile.Add(entry);
                    }
                }
            }
            else
            {
                BuildFallbackDeck();
            }

            ShuffleDrawPile();
        }

        private void BuildFallbackDeck()
        {
            // Attack: Cinder Dart (Fire, 8 dmg, Burn)
            for (int i = 0; i < 5; i++)
            {
                drawPile.Add(new WorkshopBattleCardEntry
                {
                    CardId = "combat.spell.basic.fire",
                    DisplayName = "Cinder Dart",
                    Amount = 1,
                    Element = WorkshopElementAttribute.Fire,
                    Role = WorkshopSpellRole.Attack,
                    PrimaryValue = 8,
                    HitCount = 1,
                    SecondaryValue = 1f,
                    EffectKeyword = "Burn"
                });
            }

            // Defense: Stoneguard Sigil (Earth, 7 shield, Bulwark)
            for (int i = 0; i < 3; i++)
            {
                drawPile.Add(new WorkshopBattleCardEntry
                {
                    CardId = "combat.spell.basic.earth",
                    DisplayName = "Stoneguard Sigil",
                    Amount = 1,
                    Element = WorkshopElementAttribute.Earth,
                    Role = WorkshopSpellRole.Defense,
                    PrimaryValue = 7,
                    HitCount = 1,
                    SecondaryValue = 18f,
                    EffectKeyword = "Bulwark"
                });
            }

            // Healing: Tidal Mend (Water, 6 HP, Regen)
            for (int i = 0; i < 2; i++)
            {
                drawPile.Add(new WorkshopBattleCardEntry
                {
                    CardId = "combat.spell.basic.water",
                    DisplayName = "Tidal Mend",
                    Amount = 1,
                    Element = WorkshopElementAttribute.Water,
                    Role = WorkshopSpellRole.Healing,
                    PrimaryValue = 6,
                    HitCount = 1,
                    SecondaryValue = 8f,
                    EffectKeyword = "Regen"
                });
            }
        }

        private void ShuffleDrawPile()
        {
            int n = drawPile.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                WorkshopBattleCardEntry value = drawPile[k];
                drawPile[k] = drawPile[n];
                drawPile[n] = value;
            }
        }

        public bool TryPlayCard(int handIndex, out BattleResolvedEffect effect)
        {
            effect = new BattleResolvedEffect();
            LastPlayedDefinition = null;

            if (handIndex < 0 || handIndex >= hand.Count)
            {
                return false;
            }

            WorkshopBattleCardEntry card = hand[handIndex];
            object source = FindCardSource(card);

            if (source is BattleCardDefinition definition)
            {
                LastPlayedDefinition = definition;

                // Phase 2 compatibility: synthesize a BattleResolvedEffect from instructions
                // so basic damage/heal/shield works even before Phase 3 EffectExecutor.
                int primaryValue = 0;
                int hitCount = 1;
                foreach (BattleEffectInstruction instruction in definition.Instructions)
                {
                    if (instruction.Type == BattleEffectType.Damage ||
                        instruction.Type == BattleEffectType.Heal ||
                        instruction.Type == BattleEffectType.Shield)
                    {
                        primaryValue = instruction.Value;
                        hitCount = instruction.HitCount;
                        break;
                    }
                }

                effect = new BattleResolvedEffect
                {
                    Role = card.Role,
                    Element = card.Element,
                    PrimaryValue = primaryValue,
                    HitCount = hitCount,
                    SecondaryValue = card.SecondaryValue
                };
            }
            else if (source is BattleCardEffectTemplate template)
            {
                effect = template.Resolve(card);
            }
            else
            {
                Debug.LogWarning($"BattleDeckController: no source found for card '{card.DisplayName}' (ID: {card.CardId}, Role: {card.Role}).");
                return false;
            }

            discardPile.Add(card);
            hand.RemoveAt(handIndex);
            DrawCards(1);

            return true;
        }

        public bool TryGetActionPointCost(int handIndex, out int actionPointCost)
        {
            actionPointCost = 0;

            if (handIndex < 0 || handIndex >= hand.Count)
            {
                return false;
            }

            actionPointCost = GetActionPointCost(hand[handIndex].Role);
            return true;
        }

        public void EndTurn()
        {
            // Move entire hand to discard and draw fresh hand
            discardPile.AddRange(hand);
            hand.Clear();
            DrawCards(5);
        }

        public static int GetActionPointCost(WorkshopSpellRole role)
        {
            switch (role)
            {
                case WorkshopSpellRole.Attack:
                    return 2;
                case WorkshopSpellRole.Defense:
                case WorkshopSpellRole.Healing:
                    return 1;
                default:
                    return 1;
            }
        }

        private object FindCardSource(WorkshopBattleCardEntry card)
        {
            if (contentDatabase == null)
            {
                return null;
            }

            // Path A/B: per-card definition lookup
            BattleCardDefinition definition = contentDatabase.FindCardDefinition(card.CardId);
            if (definition != null)
            {
                return definition;
            }

            // Path C: fallback template lookup by Role (safety net)
            Debug.LogWarning($"BattleDeckController: no BattleCardDefinition found for '{card.CardId}' ({card.DisplayName}). " +
                             "Falling back to legacy template lookup by Role. This should not happen for properly configured cards.");
            return FindTemplate(card);
        }

        private BattleCardEffectTemplate FindTemplate(WorkshopBattleCardEntry card)
        {
            if (contentDatabase == null)
            {
                return null;
            }

            BattleCardEffectTemplate template = contentDatabase.FindTemplate(card.CardId);
            if (template != null)
            {
                return template;
            }

            return contentDatabase.FindTemplateByRole(card.Role);
        }

        private void DrawCards(int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (drawPile.Count == 0)
                {
                    if (discardPile.Count == 0)
                    {
                        break;
                    }

                    drawPile.AddRange(discardPile);
                    discardPile.Clear();
                    ShuffleDrawPile();
                }

                if (drawPile.Count > 0)
                {
                    hand.Add(drawPile[0]);
                    drawPile.RemoveAt(0);
                }
            }
        }
    }
}
