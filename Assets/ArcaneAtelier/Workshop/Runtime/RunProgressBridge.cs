using System;
using System.Collections.Generic;
using System.Linq;

namespace ArcaneAtelier.Workshop
{
    [Serializable]
    public sealed class RunEncounterContext
    {
        public int EncounterIndex;
        public int TotalNormalEncountersBeforeBoss;
        public string EncounterId = string.Empty;
        public string EncounterLabel = string.Empty;
        public string EncounterDescription = string.Empty;
        public string BossId = string.Empty;
        public bool IsBoss;
        public string RewardId = string.Empty;
        public string RewardDisplayName = string.Empty;
        public string RewardDescription = string.Empty;
    }

    [Serializable]
    public sealed class RunBattleRecord
    {
        public int EncounterIndex;
        public string EncounterLabel = string.Empty;
        public string EncounterDescription = string.Empty;
        public string BossDisplayName = string.Empty;
        public bool IsBoss;
        public bool Victory;
        public int PrepTicksUsed;
        public int CraftedCardCopies;
        public int CraftedCardTypes;
        public int StartingShieldBonus;
        public int TurnsElapsed;
        public int CardsPlayed;
        public int TotalDamageDealt;
        public int TotalHealingDone;
        public int TotalShieldGained;
        public int TokensEarned;
        public string RewardDisplayName = string.Empty;
        public string RewardDescription = string.Empty;
    }

    [Serializable]
    public sealed class RunSummaryData
    {
        public List<RunBattleRecord> BattleHistory = new List<RunBattleRecord>();
        public List<string> RewardsClaimed = new List<string>();
        public int TotalPrepTicksUsed;
        public int TotalCraftedCardCopies;
        public int TotalCraftedCardTypes;
        public int TotalCardsPlayed;
        public int TotalDamageDealt;
        public int TotalHealingDone;
        public int TotalShieldGained;
        public int TotalTurnsElapsed;
        public int TotalTokensEarned;
        public int Victories;
        public int Defeats;
        public string FinalBossName = string.Empty;
        public string LegacyUnlockName = string.Empty;
        public string FinalOutcomeTitle = string.Empty;
        public string FinalOutcomeDescription = string.Empty;
        public bool RunEnded;
        public bool RunWon;
    }

    public static class RunProgressBridge
    {
        public static RunEncounterContext CurrentEncounter { get; private set; } = new RunEncounterContext();
        public static RunSummaryData CurrentSummary { get; private set; } = new RunSummaryData();
        public static int PendingPrepTicksUsed { get; private set; }
        public static int PendingCraftedCardCopies { get; private set; }
        public static int PendingCraftedCardTypes { get; private set; }
        public static int PendingStartingShieldBonus { get; private set; }

        public static event Action StateChanged;

        public static void Reset()
        {
            CurrentEncounter = new RunEncounterContext();
            CurrentSummary = new RunSummaryData();
            PendingPrepTicksUsed = 0;
            PendingCraftedCardCopies = 0;
            PendingCraftedCardTypes = 0;
            PendingStartingShieldBonus = 0;
            StateChanged?.Invoke();
        }

        public static void ConfigureEncounter(
            int encounterIndex,
            int totalNormalEncountersBeforeBoss,
            string encounterId,
            string encounterLabel,
            string encounterDescription,
            string bossId,
            bool isBoss,
            string rewardId,
            string rewardDisplayName,
            string rewardDescription)
        {
            CurrentEncounter = new RunEncounterContext
            {
                EncounterIndex = encounterIndex,
                TotalNormalEncountersBeforeBoss = totalNormalEncountersBeforeBoss,
                EncounterId = encounterId ?? string.Empty,
                EncounterLabel = encounterLabel ?? string.Empty,
                EncounterDescription = encounterDescription ?? string.Empty,
                BossId = bossId ?? string.Empty,
                IsBoss = isBoss,
                RewardId = rewardId ?? string.Empty,
                RewardDisplayName = rewardDisplayName ?? string.Empty,
                RewardDescription = rewardDescription ?? string.Empty
            };

            PendingPrepTicksUsed = 0;
            PendingCraftedCardCopies = 0;
            PendingCraftedCardTypes = 0;
            PendingStartingShieldBonus = 0;
            StateChanged?.Invoke();
        }

        public static void RegisterPreparation(int prepTicksUsed, WorkshopBattlePayload payload, int startingShieldBonus)
        {
            PendingPrepTicksUsed = Math.Max(0, prepTicksUsed);
            PendingCraftedCardTypes = payload?.Cards?.Count ?? 0;
            PendingCraftedCardCopies = payload?.Cards?.Sum(card => Math.Max(0, card.Amount)) ?? 0;
            PendingStartingShieldBonus = Math.Max(0, startingShieldBonus);
            StateChanged?.Invoke();
        }

        public static void RecordBattleResult(
            string bossDisplayName,
            bool victory,
            int turnsElapsed,
            int cardsPlayed,
            int totalDamageDealt,
            int totalHealingDone,
            int totalShieldGained,
            int tokensEarned,
            string finalOutcomeTitle,
            string finalOutcomeDescription,
            string legacyUnlockName)
        {
            var record = new RunBattleRecord
            {
                EncounterIndex = CurrentEncounter.EncounterIndex,
                EncounterLabel = CurrentEncounter.EncounterLabel,
                EncounterDescription = CurrentEncounter.EncounterDescription,
                BossDisplayName = bossDisplayName ?? "Enemy",
                IsBoss = CurrentEncounter.IsBoss,
                Victory = victory,
                PrepTicksUsed = PendingPrepTicksUsed,
                CraftedCardCopies = PendingCraftedCardCopies,
                CraftedCardTypes = PendingCraftedCardTypes,
                StartingShieldBonus = PendingStartingShieldBonus,
                TurnsElapsed = Math.Max(0, turnsElapsed),
                CardsPlayed = Math.Max(0, cardsPlayed),
                TotalDamageDealt = Math.Max(0, totalDamageDealt),
                TotalHealingDone = Math.Max(0, totalHealingDone),
                TotalShieldGained = Math.Max(0, totalShieldGained),
                TokensEarned = Math.Max(0, tokensEarned),
                RewardDisplayName = victory ? CurrentEncounter.RewardDisplayName : string.Empty,
                RewardDescription = victory ? CurrentEncounter.RewardDescription : string.Empty
            };

            CurrentSummary.BattleHistory.Add(record);
            CurrentSummary.TotalPrepTicksUsed += record.PrepTicksUsed;
            CurrentSummary.TotalCraftedCardCopies += record.CraftedCardCopies;
            CurrentSummary.TotalCraftedCardTypes += record.CraftedCardTypes;
            CurrentSummary.TotalCardsPlayed += record.CardsPlayed;
            CurrentSummary.TotalDamageDealt += record.TotalDamageDealt;
            CurrentSummary.TotalHealingDone += record.TotalHealingDone;
            CurrentSummary.TotalShieldGained += record.TotalShieldGained;
            CurrentSummary.TotalTurnsElapsed += record.TurnsElapsed;
            CurrentSummary.TotalTokensEarned += record.TokensEarned;

            if (victory)
            {
                CurrentSummary.Victories++;
                if (!string.IsNullOrWhiteSpace(record.RewardDisplayName))
                {
                    CurrentSummary.RewardsClaimed.Add(record.RewardDisplayName);
                }
            }
            else
            {
                CurrentSummary.Defeats++;
            }

            if (CurrentEncounter.IsBoss && victory)
            {
                CurrentSummary.RunEnded = true;
                CurrentSummary.RunWon = true;
                CurrentSummary.FinalBossName = bossDisplayName ?? "Final Boss";
                CurrentSummary.LegacyUnlockName = legacyUnlockName ?? string.Empty;
                CurrentSummary.FinalOutcomeTitle = finalOutcomeTitle ?? "Run Complete";
                CurrentSummary.FinalOutcomeDescription = finalOutcomeDescription ?? string.Empty;
            }
            else if (!victory)
            {
                CurrentSummary.RunEnded = true;
                CurrentSummary.RunWon = false;
                CurrentSummary.FinalBossName = bossDisplayName ?? "Enemy";
                CurrentSummary.FinalOutcomeTitle = finalOutcomeTitle ?? "Run Failed";
                CurrentSummary.FinalOutcomeDescription = finalOutcomeDescription ?? string.Empty;
            }

            PendingPrepTicksUsed = 0;
            PendingCraftedCardCopies = 0;
            PendingCraftedCardTypes = 0;
            PendingStartingShieldBonus = 0;
            StateChanged?.Invoke();
        }
    }
}
