using System;

namespace ArcaneAtelier.Battle
{
    [Serializable]
    public sealed class BattleBossAction
    {
        public BattleActionType ActionType;
        public int Value;
        public float SecondaryValue;
        public string Description;
    }
}
