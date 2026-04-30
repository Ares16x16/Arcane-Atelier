using ArcaneAtelier.Workshop;
using UnityEngine;

namespace ArcaneAtelier.Battle
{
    public sealed class HealEffectCommand : IBattleEffectCommand
    {
        public BattleActionResolution Execute(BattleUnit caster, BattleUnit primaryTarget, BattleUnit resolvedTarget, BattleEffectInstruction instruction, WorkshopElementAttribute element)
        {
            if (resolvedTarget == null)
            {
                return new BattleActionResolution(0, 0, 0, "Invalid target for healing.");
            }

            int heal = Mathf.Max(0, instruction.Value) * Mathf.Max(1, instruction.HitCount);
            if (resolvedTarget.StatusEffectController != null)
            {
                heal = resolvedTarget.StatusEffectController.ModifyHealing(resolvedTarget, heal);
            }
            resolvedTarget.Heal(heal);

            string desc = $"Player restores {heal} HP to {resolvedTarget.DisplayName}. [{resolvedTarget.DisplayName} HP: {resolvedTarget.CurrentHealth}/{resolvedTarget.MaxHealth}]";
            return new BattleActionResolution(
                0,
                heal,
                0,
                desc,
                BattleFeedbackTarget.Player,
                resolvedTarget == caster ? BattleFeedbackTarget.Player : BattleFeedbackTarget.Boss,
                BattleFeedbackKind.Heal,
                "Restore");
        }
    }
}
