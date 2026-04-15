using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArcaneAtelier.Workshop
{
    [Flags]
    public enum NodePortMask
    {
        None = 0,
        North = 1 << 0,
        East = 1 << 1,
        South = 1 << 2,
        West = 1 << 3,
        All = North | East | South | West
    }

    public enum WorkshopItemKind
    {
        Resource = 0,
        Card = 1
    }

    public enum WorkshopNodeCategory
    {
        Source = 0,
        Processor = 1,
        Crafter = 2,
        Storage = 3
    }

    public enum WorkshopRewardKind
    {
        UnlockNode = 0,
        EfficiencyBoost = 1,
        GrantItems = 2
    }

    public static class WorkshopDirectionUtility
    {
        public static readonly IReadOnlyList<NodePortMask> CardinalDirections = new[]
        {
            NodePortMask.North,
            NodePortMask.East,
            NodePortMask.South,
            NodePortMask.West
        };

        public static Vector2Int ToOffset(NodePortMask direction)
        {
            return direction switch
            {
                NodePortMask.North => Vector2Int.up,
                NodePortMask.East => Vector2Int.right,
                NodePortMask.South => Vector2Int.down,
                NodePortMask.West => Vector2Int.left,
                _ => Vector2Int.zero
            };
        }

        public static NodePortMask Opposite(NodePortMask direction)
        {
            return direction switch
            {
                NodePortMask.North => NodePortMask.South,
                NodePortMask.East => NodePortMask.West,
                NodePortMask.South => NodePortMask.North,
                NodePortMask.West => NodePortMask.East,
                _ => NodePortMask.None
            };
        }

        public static NodePortMask Rotate(NodePortMask mask, int quarterTurnsClockwise)
        {
            var turns = ((quarterTurnsClockwise % 4) + 4) % 4;
            var rotated = NodePortMask.None;

            foreach (var direction in CardinalDirections)
            {
                if ((mask & direction) == 0)
                {
                    continue;
                }

                rotated |= RotateSingle(direction, turns);
            }

            return rotated;
        }

        private static NodePortMask RotateSingle(NodePortMask direction, int turns)
        {
            var index = direction switch
            {
                NodePortMask.North => 0,
                NodePortMask.East => 1,
                NodePortMask.South => 2,
                NodePortMask.West => 3,
                _ => 0
            };

            return CardinalDirections[(index + turns) % CardinalDirections.Count];
        }
    }
}

