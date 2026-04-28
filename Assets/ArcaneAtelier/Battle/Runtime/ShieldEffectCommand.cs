using ArcaneAtelier.Workshop;
using UnityEngine;

namespace ArcaneAtelier.Battle
{
    public sealed class ShieldEffectCommand : IBattleEffectCommand
    {
        public BattleActionResolution Execute(BattleUnit caster, BattleUnit primaryTarget, BattleUnit resolvedTarget, BattleEffectInstruction instruction, WorkshopElementAttribute element)
        {
            if (resolvedTarget == null)
            {
                return new BattleActionResolution(0, 0, 0, "Invalid target for shield.");
            }

            int shield = Mathf.Max(0, instruction.Value) * Mathf.Max(1, instruction.HitCount);
            if (resolvedTarget.StatusEffectController != null)
            {
                shield = resolvedTarget.StatusEffectController.ModifyShieldGain(resolvedTarget, shield);
            }
            resolvedTarget.AddShield(shield);

            string desc = $"{resolvedTarget.DisplayName} gains {shield} shield. [{resolvedTarget.DisplayName} Shield: {resolvedTarget.Shield}]";
            return new BattleActionResolution(0, 0, shield, desc);
        }
    }
}
