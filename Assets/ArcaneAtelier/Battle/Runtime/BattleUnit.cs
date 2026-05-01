using ArcaneAtelier.Workshop;
using UnityEngine;

namespace ArcaneAtelier.Battle
{
    public sealed class BattleUnit
    {
        public string DisplayName { get; set; } = "Unit";
        public int MaxHealth { get; set; } = 100;
        public int CurrentHealth { get; set; } = 100;
        public int Shield { get; set; } = 0;
        public WorkshopElementAttribute Element { get; set; } = WorkshopElementAttribute.None;
        public bool IsAlive => CurrentHealth > 0;
        public BattleStatusEffectController StatusEffectController { get; set; }

        public void TakeDamage(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            int absorbed = Mathf.Min(Shield, amount);
            Shield -= absorbed;
            CurrentHealth -= (amount - absorbed);

            if (CurrentHealth < 0)
            {
                CurrentHealth = 0;
            }
        }

        public void Heal(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            CurrentHealth = Mathf.Min(CurrentHealth + amount, MaxHealth);
        }

        public void AddShield(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Shield += amount;
        }
    }
}
