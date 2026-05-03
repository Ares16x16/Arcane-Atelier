using ArcaneAtelier.Workshop;

namespace ArcaneAtelier.Battle
{
    public sealed class ApplyStatusEffectCommand : IBattleEffectCommand
    {
        public BattleActionResolution Execute(BattleUnit caster, BattleUnit primaryTarget, BattleUnit resolvedTarget, BattleEffectInstruction instruction, WorkshopElementAttribute element)
        {
            if (string.IsNullOrEmpty(instruction.StatusId) || resolvedTarget == null)
            {
                return new BattleActionResolution(0, 0, 0, "Failed to apply status effect.");
            }

            resolvedTarget.StatusEffectController?.Apply(resolvedTarget, instruction.StatusId, instruction.Duration, caster, instruction.Value);

            string desc = $"Applies [{instruction.StatusId}] for {instruction.Duration} turn(s) to {resolvedTarget.DisplayName}.";
            return new BattleActionResolution(
                0,
                0,
                0,
                desc,
                BattleFeedbackTarget.Player,
                resolvedTarget == caster ? BattleFeedbackTarget.Player : BattleFeedbackTarget.Boss,
                BattleFeedbackKind.StatusApplied,
                instruction.StatusId,
                instruction.StatusId,
                instruction.Duration);
        }
    }
}
