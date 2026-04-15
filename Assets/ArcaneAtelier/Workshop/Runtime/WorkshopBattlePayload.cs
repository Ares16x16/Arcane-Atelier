using System;
using System.Collections.Generic;
using System.Linq;

namespace ArcaneAtelier.Workshop
{
    [Serializable]
    public struct WorkshopBattleCardEntry
    {
        public string CardId;
        public string DisplayName;
        public int Amount;
    }

    [Serializable]
    public sealed class WorkshopBattlePayload
    {
        public List<WorkshopBattleCardEntry> Cards = new();

        public bool HasCards => Cards.Count > 0;
    }

    public static class WorkshopBattlePayloadBridge
    {
        public static WorkshopBattlePayload CurrentPayload { get; private set; } = new();

        public static event Action PayloadCommitted;

        public static void Commit(IReadOnlyDictionary<WorkshopItemDefinition, int> craftedCards)
        {
            var payload = new WorkshopBattlePayload();

            foreach (var pair in craftedCards.Where(pair => pair.Key != null && pair.Value > 0))
            {
                payload.Cards.Add(new WorkshopBattleCardEntry
                {
                    CardId = pair.Key.BattleCardId,
                    DisplayName = pair.Key.DisplayName,
                    Amount = pair.Value
                });
            }

            payload.Cards.Sort((left, right) => string.CompareOrdinal(left.CardId, right.CardId));
            CurrentPayload = payload;
            PayloadCommitted?.Invoke();
        }

        public static bool TryConsume(out WorkshopBattlePayload payload)
        {
            payload = CurrentPayload;
            if (!payload.HasCards)
            {
                return false;
            }

            CurrentPayload = new WorkshopBattlePayload();
            return true;
        }

        public static void Clear()
        {
            CurrentPayload = new WorkshopBattlePayload();
            PayloadCommitted?.Invoke();
        }
    }
}
