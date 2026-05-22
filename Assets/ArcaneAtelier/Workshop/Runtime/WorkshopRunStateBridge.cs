using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArcaneAtelier.Workshop
{
    [Serializable]
    public sealed class WorkshopRunItemStackSnapshot
    {
        public string ItemId = string.Empty;
        public int Amount;
    }

    [Serializable]
    public sealed class WorkshopRunNodeSnapshot
    {
        public string NodeId = string.Empty;
        public Vector2Int Position;
        public int RotationQuarterTurns;
        public float SpeedMultiplier = 1f;
        public float CycleProgress;
        public int InputPortsMask;
        public int OutputPortsMask;
        public List<WorkshopRunItemStackSnapshot> Buffer = new List<WorkshopRunItemStackSnapshot>();
    }

    [Serializable]
    public sealed class WorkshopRunStateSnapshot
    {
        public List<string> UnlockedNodeIds = new List<string>();
        public List<string> AppliedRewardIds = new List<string>();
        public List<WorkshopRunItemStackSnapshot> ReserveItems = new List<WorkshopRunItemStackSnapshot>();
        public List<WorkshopRunNodeSnapshot> Nodes = new List<WorkshopRunNodeSnapshot>();
        public float SimulatedSeconds;
        public int TotalElementProduced;
        public int TotalElementConsumed;
        public int TotalSpellsProduced;
        public int Tokens;
    }

    public static class WorkshopRunStateBridge
    {
        public static WorkshopRunStateSnapshot CurrentState { get; private set; }

        public static bool HasState => CurrentState != null;

        public static void Commit(WorkshopRunStateSnapshot state)
        {
            CurrentState = state;
        }

        public static bool TryGet(out WorkshopRunStateSnapshot state)
        {
            state = CurrentState;
            return state != null;
        }

        public static void Clear()
        {
            CurrentState = null;
        }
    }
}
