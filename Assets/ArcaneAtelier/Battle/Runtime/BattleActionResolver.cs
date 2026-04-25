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

            BattleElementRelation relation = BattleElementUtility.GetRelation(effect.Element, boss.Element);

            switch (effect.Role)
            {
                case WorkshopSpellRole.Attack:
                    return ResolveAttack(effect, boss, relation);

                case WorkshopSpellRole.Healing:
                    return ResolveHealing(effect, player);

                case WorkshopSpellRole.Defense:
                    return ResolveDefense(effect, player);

                default:
                    return new BattleActionResolution(0, 0, 0, "Unsupported card role.");
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
                    player.TakeDamage(damage);
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

        private static BattleActionResolution ResolveAttack(
            BattleResolvedEffect effect,
            BattleUnit target,
            BattleElementRelation relation)
        {
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

            string desc = $"Player attacks {target.DisplayName} for {finalDamage} damage{relationText}. " +
                          $"[{target.DisplayName} HP: {target.CurrentHealth}/{target.MaxHealth}]";

            return new BattleActionResolution(finalDamage, 0, 0, desc);
        }

        private static BattleActionResolution ResolveHealing(BattleResolvedEffect effect, BattleUnit target)
        {
            int heal = Mathf.Max(0, effect.PrimaryValue);
            target.Heal(heal);

            string desc = $"Player heals for {heal} HP. [{target.DisplayName} HP: {target.CurrentHealth}/{target.MaxHealth}]";
            return new BattleActionResolution(0, heal, 0, desc);
        }

        private static BattleActionResolution ResolveDefense(BattleResolvedEffect effect, BattleUnit target)
        {
            int shield = Mathf.Max(0, effect.PrimaryValue);
            target.AddShield(shield);

            string desc = $"Player gains {shield} shield. [{target.DisplayName} Shield: {target.Shield}]";
            return new BattleActionResolution(0, 0, shield, desc);
        }
    }
}
