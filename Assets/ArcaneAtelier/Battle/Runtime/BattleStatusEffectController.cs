using System.Collections.Generic;
using UnityEngine;

namespace ArcaneAtelier.Battle
{
    public sealed class BattleStatusEffectController
    {
        private readonly Dictionary<BattleUnit, List<BattleStatusEffectInstance>> unitEffects = new Dictionary<BattleUnit, List<BattleStatusEffectInstance>>();
        private readonly BattleContentDatabase contentDatabase;

        public BattleStatusEffectController(BattleContentDatabase database)
        {
            contentDatabase = database;
        }

        public void Apply(BattleUnit target, string statusId, int duration, BattleUnit caster, int magnitude = 1)
        {
            if (target == null || duration <= 0)
            {
                return;
            }

            BattleStatusEffectDefinition definition = FindDefinition(statusId);
            if (definition == null)
            {
                Debug.LogWarning($"StatusEffectController: no definition found for status '{statusId}'.");
                return;
            }

            if (!unitEffects.TryGetValue(target, out List<BattleStatusEffectInstance> effects))
            {
                effects = new List<BattleStatusEffectInstance>();
                unitEffects[target] = effects;
            }

            BattleStatusEffectInstance existing = null;
            foreach (BattleStatusEffectInstance instance in effects)
            {
                if (instance.Definition.StatusId == statusId)
                {
                    existing = instance;
                    break;
                }
            }

            if (existing != null)
            {
                if (definition.IsStackable && existing.StackCount < definition.MaxStackCount)
                {
                    existing.StackCount++;
                }
                existing.Magnitude = Mathf.Max(existing.Magnitude, magnitude);
                existing.RemainingDuration = Mathf.Max(existing.RemainingDuration, duration);
            }
            else
            {
                effects.Add(new BattleStatusEffectInstance(definition, duration, caster, Mathf.Max(1, magnitude)));
            }
        }

        public List<BattleActionResolution> Tick(BattleStatusTrigger trigger, BattleUnit unit)
        {
            List<BattleActionResolution> results = new List<BattleActionResolution>();

            if (unit == null || !unitEffects.TryGetValue(unit, out List<BattleStatusEffectInstance> effects))
            {
                return results;
            }

            for (int i = effects.Count - 1; i >= 0; i--)
            {
                BattleStatusEffectInstance instance = effects[i];
                if (instance.Definition.Trigger != trigger)
                {
                    continue;
                }

                BattleEffectInstruction tickEffect = instance.Definition.TickEffect;
                int value = Mathf.Max(tickEffect.Value, instance.Magnitude) * instance.StackCount;

                if (ShouldHandleAsPassiveModifier(instance.Definition.StatusId))
                {
                    if (trigger == BattleStatusTrigger.OnTurnStart || trigger == BattleStatusTrigger.OnTurnEnd)
                    {
                        instance.RemainingDuration--;
                        if (instance.RemainingDuration <= 0)
                        {
                            effects.RemoveAt(i);
                        }
                    }

                    continue;
                }

                switch (tickEffect.Type)
                {
                    case BattleEffectType.Damage:
                    {
                        int damage = Mathf.Max(0, value);
                        unit.TakeDamage(damage);
                        results.Add(new BattleActionResolution(damage, 0, 0,
                            $"[{instance.Definition.DisplayName}] deals {damage} damage to {unit.DisplayName}. [{unit.DisplayName} HP: {unit.CurrentHealth}/{unit.MaxHealth}]"));
                        break;
                    }

                    case BattleEffectType.Heal:
                    {
                        int heal = Mathf.Max(0, value);
                        unit.Heal(heal);
                        results.Add(new BattleActionResolution(0, heal, 0,
                            $"[{instance.Definition.DisplayName}] restores {heal} HP to {unit.DisplayName}. [{unit.DisplayName} HP: {unit.CurrentHealth}/{unit.MaxHealth}]"));
                        break;
                    }

                    case BattleEffectType.Shield:
                    {
                        int shield = Mathf.Max(0, value);
                        unit.AddShield(shield);
                        results.Add(new BattleActionResolution(0, 0, shield,
                            $"[{instance.Definition.DisplayName}] grants {shield} shield to {unit.DisplayName}. [{unit.DisplayName} Shield: {unit.Shield}]"));
                        break;
                    }

                    default:
                    {
                        results.Add(new BattleActionResolution(0, 0, 0,
                            $"[{instance.Definition.DisplayName}] triggers on {unit.DisplayName}."));
                        break;
                    }
                }

                instance.RemainingDuration--;
                if (instance.RemainingDuration <= 0)
                {
                    effects.RemoveAt(i);
                }
            }

            return results;
        }

        public IReadOnlyList<BattleStatusEffectInstance> GetEffects(BattleUnit unit)
        {
            if (unit == null || !unitEffects.TryGetValue(unit, out List<BattleStatusEffectInstance> effects))
            {
                return System.Array.Empty<BattleStatusEffectInstance>();
            }

            return effects.AsReadOnly();
        }

        public bool HasStatus(BattleUnit unit, string statusId)
        {
            if (unit == null || string.IsNullOrWhiteSpace(statusId) || !unitEffects.TryGetValue(unit, out List<BattleStatusEffectInstance> effects))
            {
                return false;
            }

            foreach (BattleStatusEffectInstance effect in effects)
            {
                if (effect.Definition.StatusId == statusId)
                {
                    return true;
                }
            }

            return false;
        }

        public int GetStatusMagnitude(BattleUnit unit, string statusId)
        {
            if (unit == null || string.IsNullOrWhiteSpace(statusId) || !unitEffects.TryGetValue(unit, out List<BattleStatusEffectInstance> effects))
            {
                return 0;
            }

            int maxMagnitude = 0;
            foreach (BattleStatusEffectInstance effect in effects)
            {
                if (effect.Definition.StatusId == statusId)
                {
                    maxMagnitude = Mathf.Max(maxMagnitude, effect.Magnitude * effect.StackCount);
                }
            }

            return maxMagnitude;
        }

        public bool ConsumeStatus(BattleUnit unit, string statusId)
        {
            if (unit == null || string.IsNullOrWhiteSpace(statusId) || !unitEffects.TryGetValue(unit, out List<BattleStatusEffectInstance> effects))
            {
                return false;
            }

            for (int i = effects.Count - 1; i >= 0; i--)
            {
                if (effects[i].Definition.StatusId == statusId)
                {
                    effects.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }

        public int ModifyIncomingDamage(BattleUnit unit, int baseDamage)
        {
            int damage = Mathf.Max(0, baseDamage);
            damage = Mathf.RoundToInt(damage * (1f + GetStatusMagnitude(unit, "Expose") / 100f));
            damage = Mathf.RoundToInt(damage * (1f - GetStatusMagnitude(unit, "Bulwark") / 100f));
            damage = Mathf.RoundToInt(damage * (1f - GetStatusMagnitude(unit, "Shade") / 100f));
            damage = Mathf.RoundToInt(damage * (1f - GetStatusMagnitude(unit, "Veil") / 100f));
            return Mathf.Max(0, damage);
        }

        public int ModifyShieldGain(BattleUnit unit, int baseShield)
        {
            int shield = Mathf.Max(0, baseShield);
            shield = Mathf.RoundToInt(shield * (1f + GetStatusMagnitude(unit, "Ward") / 100f));
            return Mathf.Max(0, shield);
        }

        public int ModifyHealing(BattleUnit unit, int baseHealing)
        {
            int heal = Mathf.Max(0, baseHealing);
            heal = Mathf.RoundToInt(heal * (1f + GetStatusMagnitude(unit, "Bless") / 100f));
            heal = Mathf.RoundToInt(heal * (1f + GetStatusMagnitude(unit, "Radiance") / 100f));
            return Mathf.Max(0, heal);
        }

        public int ConsumeShockBonus(BattleUnit unit)
        {
            int magnitude = GetStatusMagnitude(unit, "Shock");
            if (magnitude > 0)
            {
                ConsumeStatus(unit, "Shock");
            }

            return magnitude;
        }

        public int ConsumeRendShieldBreakBonus(BattleUnit unit)
        {
            int magnitude = GetStatusMagnitude(unit, "Rend");
            if (magnitude > 0)
            {
                ConsumeStatus(unit, "Rend");
            }

            return magnitude;
        }

        public int ConsumeStaticShellCounter(BattleUnit unit)
        {
            int magnitude = GetStatusMagnitude(unit, "Static Shell");
            if (magnitude > 0)
            {
                ConsumeStatus(unit, "Static Shell");
            }

            return magnitude;
        }

        private static bool ShouldHandleAsPassiveModifier(string statusId)
        {
            switch (statusId)
            {
                case "Expose":
                case "Bulwark":
                case "Slow":
                case "Shock":
                case "Veil":
                case "Ward":
                case "Freeze":
                case "Stun":
                case "Shade":
                case "Rend":
                case "Static Shell":
                    return true;
                default:
                    return false;
            }
        }

        private BattleStatusEffectDefinition FindDefinition(string statusId)
        {
            if (contentDatabase == null)
            {
                return null;
            }

            return contentDatabase.FindStatusEffectDefinition(statusId);
        }
    }
}
