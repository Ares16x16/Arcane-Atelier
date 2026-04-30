using ArcaneAtelier.Workshop;
using UnityEngine;

namespace ArcaneAtelier.Battle
{
    public sealed class DamageEffectCommand : IBattleEffectCommand
    {
        public BattleActionResolution Execute(BattleUnit caster, BattleUnit primaryTarget, BattleUnit resolvedTarget, BattleEffectInstruction instruction, WorkshopElementAttribute element)
        {
            if (resolvedTarget == null)
            {
                return new BattleActionResolution(0, 0, 0, "Invalid target for damage.");
            }

            int baseDamage = instruction.Value * Mathf.Max(1, instruction.HitCount);
            BattleElementRelation relation = BattleElementUtility.GetRelation(element, resolvedTarget.Element);
            float modifiedDamage = BattleElementUtility.ApplyMultiplier(baseDamage, relation);
            int finalDamage = Mathf.Max(0, Mathf.RoundToInt(modifiedDamage));
            if (resolvedTarget.StatusEffectController != null)
            {
                finalDamage = resolvedTarget.StatusEffectController.ModifyIncomingDamage(resolvedTarget, finalDamage);
                finalDamage += resolvedTarget.StatusEffectController.ConsumeShockBonus(resolvedTarget);
            }

            int previousShield = resolvedTarget.Shield;
            resolvedTarget.TakeDamage(finalDamage);
            if (resolvedTarget.StatusEffectController != null && previousShield > 0 && resolvedTarget.Shield == 0)
            {
                int bonusBreakDamage = resolvedTarget.StatusEffectController.ConsumeRendShieldBreakBonus(resolvedTarget);
                if (bonusBreakDamage > 0)
                {
                    resolvedTarget.TakeDamage(bonusBreakDamage);
                    finalDamage += bonusBreakDamage;
                }
            }

            string relationText = relation switch
            {
                BattleElementRelation.Advantage => " (Advantage!)",
                BattleElementRelation.Disadvantage => " (Disadvantage)",
                _ => string.Empty
            };

            string desc = $"Player attacks {resolvedTarget.DisplayName} for {finalDamage} damage{relationText}. " +
                          $"[{resolvedTarget.DisplayName} HP: {resolvedTarget.CurrentHealth}/{resolvedTarget.MaxHealth}]";

            return new BattleActionResolution(
                finalDamage,
                0,
                0,
                desc,
                BattleFeedbackTarget.Player,
                resolvedTarget == caster ? BattleFeedbackTarget.Player : BattleFeedbackTarget.Boss,
                BattleFeedbackKind.Damage,
                "Spell Hit");
        }
    }
}
