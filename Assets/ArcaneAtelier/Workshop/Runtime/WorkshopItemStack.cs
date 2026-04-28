using System;
using UnityEngine;

namespace ArcaneAtelier.Workshop
{
    [Serializable]
    public sealed class WorkshopItemStack
    {
        [SerializeField] private WorkshopItemDefinition item;
        [SerializeField] private int amount = 1;

        public WorkshopItemDefinition Item => item;
        public int Amount => Mathf.Max(0, amount);

        public static WorkshopItemStack Create(WorkshopItemDefinition definition, int itemAmount)
        {
            return new WorkshopItemStack
            {
                item = definition,
                amount = Mathf.Max(0, itemAmount)
            };
        }
    }
}
