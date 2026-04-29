using ArcaneAtelier.Workshop;
using UnityEngine;

namespace ArcaneAtelier.Battle
{
    public static class BattleElementUtility
    {
        public static BattleElementRelation GetRelation(WorkshopElementAttribute attacker, WorkshopElementAttribute defender)
        {
            if (attacker == WorkshopElementAttribute.None || defender == WorkshopElementAttribute.None)
            {
                return BattleElementRelation.Neutral;
            }

            if (HasAdvantage(attacker, defender))
            {
                return BattleElementRelation.Advantage;
            }

            if (HasAdvantage(defender, attacker))
            {
                return BattleElementRelation.Disadvantage;
            }

            return BattleElementRelation.Neutral;
        }

        private static bool HasAdvantage(WorkshopElementAttribute attacker, WorkshopElementAttribute defender)
        {
            switch (attacker)
            {
                case WorkshopElementAttribute.Water:
                    return defender == WorkshopElementAttribute.Fire;
                case WorkshopElementAttribute.Fire:
                    return defender == WorkshopElementAttribute.Water;
                case WorkshopElementAttribute.Wind:
                    return defender == WorkshopElementAttribute.Earth;
                case WorkshopElementAttribute.Earth:
                    return defender == WorkshopElementAttribute.Wind;
                case WorkshopElementAttribute.Ice:
                    return defender == WorkshopElementAttribute.Thunder;
                case WorkshopElementAttribute.Thunder:
                    return defender == WorkshopElementAttribute.Ice;
                case WorkshopElementAttribute.Light:
                    return defender == WorkshopElementAttribute.Dark;
                case WorkshopElementAttribute.Dark:
                    return defender == WorkshopElementAttribute.Light;
                default:
                    return false;
            }
        }

        public static float ApplyMultiplier(float baseValue, BattleElementRelation relation)
        {
            switch (relation)
            {
                case BattleElementRelation.Advantage:
                    return baseValue * 1.25f;
                case BattleElementRelation.Disadvantage:
                    return baseValue * 0.75f;
                default:
                    return baseValue;
            }
        }
    }
}
