namespace ArcaneAtelier.Battle
{
    public sealed class BattleStatusEffectInstance
    {
        public BattleStatusEffectDefinition Definition { get; }
        public int RemainingDuration { get; set; }
        public int StackCount { get; set; }
        public int Magnitude { get; set; }
        public BattleUnit Caster { get; }

        public BattleStatusEffectInstance(BattleStatusEffectDefinition definition, int duration, BattleUnit caster, int magnitude)
        {
            Definition = definition;
            RemainingDuration = duration;
            StackCount = 1;
            Caster = caster;
            Magnitude = magnitude;
        }
    }
}
