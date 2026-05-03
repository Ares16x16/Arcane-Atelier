using System;

namespace ArcaneAtelier.Battle
{
    public enum BattleEffectTarget
    {
        Auto = 0,
        Self = 1,
        Opponent = 2
    }

    public enum BattleEffectType
    {
        Damage = 0,
        Heal = 1,
        Shield = 2,
        ApplyStatus = 3,
        DrawCard = 4
    }

    [Serializable]
    public struct BattleEffectInstruction
    {
        public BattleEffectType Type;
        public BattleEffectTarget Target;
        public int Value;
        public int HitCount;
        public string StatusId;
        public int Duration;

        public static BattleEffectInstruction Damage(int value, int hitCount = 1, BattleEffectTarget target = BattleEffectTarget.Opponent)
        {
            return new BattleEffectInstruction
            {
                Type = BattleEffectType.Damage,
                Target = target,
                Value = value,
                HitCount = hitCount,
                StatusId = string.Empty,
                Duration = 0
            };
        }

        public static BattleEffectInstruction Heal(int value, int hitCount = 1, BattleEffectTarget target = BattleEffectTarget.Self)
        {
            return new BattleEffectInstruction
            {
                Type = BattleEffectType.Heal,
                Target = target,
                Value = value,
                HitCount = hitCount,
                StatusId = string.Empty,
                Duration = 0
            };
        }

        public static BattleEffectInstruction Shield(int value, int hitCount = 1, BattleEffectTarget target = BattleEffectTarget.Self)
        {
            return new BattleEffectInstruction
            {
                Type = BattleEffectType.Shield,
                Target = target,
                Value = value,
                HitCount = hitCount,
                StatusId = string.Empty,
                Duration = 0
            };
        }

        public static BattleEffectInstruction ApplyStatus(string statusId, int duration, int magnitude = 1, BattleEffectTarget target = BattleEffectTarget.Opponent)
        {
            return new BattleEffectInstruction
            {
                Type = BattleEffectType.ApplyStatus,
                Target = target,
                Value = magnitude,
                HitCount = 1,
                StatusId = statusId ?? string.Empty,
                Duration = duration
            };
        }
    }
}
