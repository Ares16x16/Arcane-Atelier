using ArcaneAtelier.Workshop;
using UnityEngine;

namespace ArcaneAtelier.Battle
{
    public readonly struct BattleActionResolution
    {
        public int DamageDealt { get; }
        public int HealingDone { get; }
        public int ShieldGained { get; }
        public string LogDescription { get; }

        public BattleActionResolution(int damageDealt, int healingDone, int shieldGained, string logDescription)
        {
            DamageDealt = damageDealt;
            HealingDone = healingDone;
            ShieldGained = shieldGained;
            LogDescription = logDescription ?? string.Empty;
        }
    }

    public static class BattleActionResolver
    {
        public static BattleActionResolution ResolvePlayerEffect(
            BattleResolvedEffect effect,
            BattleUnit player,
            BattleUnit boss)
        {
            if (player == null || boss == null)
            {
                return new BattleActionResolution(0, 0, 0, "Invalid target.");
            }

            switch (effect.Role)
            {
                case WorkshopSpellRole.Attack:
                    return ResolveAttack(effect, boss);
                case WorkshopSpellRole.Healing:
                    return ResolveHealing(effect, player);
                case WorkshopSpellRole.Defense:
                    return ResolveDefense(effect, player);
                default:
                    return new BattleActionResolution(0, 0, 0, $"Unsupported legacy role '{effect.Role}'.");
            }
        }

        public static BattleActionResolution ResolveBossAction(
            BattleBossAction action,
            BattleUnit boss,
            BattleUnit player)
        {
            if (boss == null || player == null)
            {
                return new BattleActionResolution(0, 0, 0, "Invalid target.");
            }

            switch (action.ActionType)
            {
                case BattleActionType.Attack:
                case BattleActionType.Special:
                {
                    int damage = Mathf.Max(0, action.Value);
                    if (player.StatusEffectController != null)
                    {
                        damage = player.StatusEffectController.ModifyIncomingDamage(player, damage);
                        damage = Mathf.RoundToInt(damage * (1f - player.StatusEffectController.GetStatusMagnitude(player, "Slow") / 100f));
                    }

                    int previousShield = player.Shield;
                    player.TakeDamage(damage);
                    if (player.StatusEffectController != null && previousShield > 0 && player.Shield == 0)
                    {
                        int bonusBreakDamage = player.StatusEffectController.ConsumeRendShieldBreakBonus(player);
                        if (bonusBreakDamage > 0)
                        {
                            player.TakeDamage(bonusBreakDamage);
                            damage += bonusBreakDamage;
                        }
                    }

                    if (boss.StatusEffectController != null)
                    {
                        int counterDamage = boss.StatusEffectController.ConsumeStaticShellCounter(boss);
                        if (counterDamage > 0)
                        {
                            boss.TakeDamage(counterDamage);
                        }
                    }
                    string desc = $"{boss.DisplayName}: {action.Description} — {player.DisplayName} takes {damage} damage.";
                    return new BattleActionResolution(damage, 0, 0, desc);
                }

                case BattleActionType.Defend:
                {
                    int shield = Mathf.Max(0, action.Value);
                    boss.AddShield(shield);
                    string desc = $"{boss.DisplayName}: {action.Description} — gains {shield} shield.";
                    return new BattleActionResolution(0, 0, shield, desc);
                }

                case BattleActionType.Heal:
                {
                    int heal = Mathf.Max(0, action.Value);
                    boss.Heal(heal);
                    string desc = $"{boss.DisplayName}: {action.Description} — heals {heal} HP.";
                    return new BattleActionResolution(0, heal, 0, desc);
                }

                default:
                    return new BattleActionResolution(0, 0, 0, "Unknown boss action.");
            }
        }

        private static BattleActionResolution ResolveAttack(BattleResolvedEffect effect, BattleUnit target)
        {
            BattleElementRelation relation = BattleElementUtility.GetRelation(effect.Element, target.Element);
            int baseDamage = effect.PrimaryValue * Mathf.Max(1, effect.HitCount);
            float modifiedDamage = BattleElementUtility.ApplyMultiplier(baseDamage, relation);
            int finalDamage = Mathf.Max(0, Mathf.RoundToInt(modifiedDamage));

            target.TakeDamage(finalDamage);

            string relationText = relation switch
            {
                BattleElementRelation.Advantage => " (Advantage!)",
                BattleElementRelation.Disadvantage => " (Disadvantage)",
                _ => string.Empty
            };

            return new BattleActionResolution(
                finalDamage,
                0,
                0,
                $"Legacy attack hits {target.DisplayName} for {finalDamage} damage{relationText}. [{target.DisplayName} HP: {target.CurrentHealth}/{target.MaxHealth}]");
        }

        private static BattleActionResolution ResolveHealing(BattleResolvedEffect effect, BattleUnit target)
        {
            int heal = Mathf.Max(0, effect.PrimaryValue) * Mathf.Max(1, effect.HitCount);
            target.Heal(heal);
            return new BattleActionResolution(0, heal, 0, $"Legacy heal restores {heal} HP to {target.DisplayName}. [{target.DisplayName} HP: {target.CurrentHealth}/{target.MaxHealth}]");
        }

        private static BattleActionResolution ResolveDefense(BattleResolvedEffect effect, BattleUnit target)
        {
            int shield = Mathf.Max(0, effect.PrimaryValue) * Mathf.Max(1, effect.HitCount);
            target.AddShield(shield);
            return new BattleActionResolution(0, 0, shield, $"Legacy defense grants {shield} shield to {target.DisplayName}. [{target.DisplayName} Shield: {target.Shield}]");
        }

    }
}
