using ArcaneAtelier.Workshop;
using UnityEngine;

namespace ArcaneAtelier.Battle
{
    public static class BattleRewardCalculator
    {
        private const int FullHealthBonus = 30;
        private const int SpeedKillTurnsThreshold = 20;
        private const int SpeedKillMultiplier = 2;
        private const float HealthRemainingMultiplier = 0.5f;

        public static int Compute(BattleResult result, BattleBossDefinition boss)
        {
            if (result == null || result.ResultType != BattleResultType.Victory)
            {
                return 0;
            }

            int basic = boss != null ? boss.BasicTokenReward : 50;

            int healthBonus = 0;
            if (result.PlayerMaxHealth > 0)
            {
                float healthRatio = Mathf.Clamp01((float)result.PlayerFinalHealth / result.PlayerMaxHealth);
                healthBonus = Mathf.RoundToInt(basic * HealthRemainingMultiplier * healthRatio);
            }

            int speedBonus = Mathf.Max(0, SpeedKillTurnsThreshold - result.TurnsElapsed) * SpeedKillMultiplier;
            int untouchedBonus = (result.PlayerMaxHealth > 0 && result.PlayerFinalHealth >= result.PlayerMaxHealth) ? FullHealthBonus : 0;

            int metaBonus = MetaProgressionStore.GetVictoryTokenBonus();
            return Mathf.Max(0, basic + healthBonus + speedBonus + untouchedBonus + metaBonus);
        }
    }
}
