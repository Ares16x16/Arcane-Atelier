using System;

namespace ArcaneAtelier.Battle
{
    [Serializable]
    public sealed class BattleResult
    {
        public BattleResultType ResultType;
        public string BossId;
        public string BossDisplayName;
        public int EncountersCleared;
        public string FinalEncounterId;
        public int TotalDamageDealt;
        public int TotalHealingDone;
        public int TotalShieldGained;
        public int CardsPlayed;
        public int TurnsElapsed;
        public string DefeatRewardId;
        public int TokensEarned;
        public int PlayerFinalHealth;
        public int PlayerMaxHealth;
    }

    public static class BattleResultBridge
    {
        public static BattleResult CurrentResult { get; private set; }

        public static event Action ResultCommitted;

        public static void Commit(BattleResult result)
        {
            CurrentResult = result;
            ResultCommitted?.Invoke();
        }

        public static bool TryConsume(out BattleResult result)
        {
            result = CurrentResult;
            if (result == null || result.ResultType == BattleResultType.None)
            {
                return false;
            }

            CurrentResult = null;
            return true;
        }

        public static void Clear()
        {
            CurrentResult = null;
            ResultCommitted?.Invoke();
        }
    }
}
