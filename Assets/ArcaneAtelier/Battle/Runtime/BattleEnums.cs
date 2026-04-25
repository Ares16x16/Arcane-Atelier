namespace ArcaneAtelier.Battle
{
    public enum BattleActionType
    {
        None = 0,
        Attack = 1,
        Heal = 2,
        Defend = 3,
        Special = 4
    }

    public enum BattleElementRelation
    {
        Neutral = 0,
        Advantage = 1,
        Disadvantage = 2
    }

    public enum BattleResultType
    {
        None = 0,
        Victory = 1,
        Defeat = 2
    }

    public enum BattleEncounterType
    {
        Enemy = 0,
        Boss = 1
    }

    public enum BattleEnemyArchetype
    {
        None = 0,
        Aggressive = 1,
        Sustain = 2,
        Defensive = 3
    }
}
