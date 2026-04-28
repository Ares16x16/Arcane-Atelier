using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ArcaneAtelier.Workshop
{
    [Serializable]
    public sealed class WorkshopPlacedNodeSeed
    {
        [SerializeField] private WorkshopNodeDefinition nodeDefinition;
        [SerializeField] private Vector2Int position;
        [SerializeField, Range(0, 3)] private int rotationQuarterTurns;

        public WorkshopNodeDefinition NodeDefinition => nodeDefinition;
        public Vector2Int Position => position;
        public int RotationQuarterTurns => rotationQuarterTurns;

        public static WorkshopPlacedNodeSeed Create(WorkshopNodeDefinition definition, Vector2Int cell, int rotation)
        {
            return new WorkshopPlacedNodeSeed
            {
                nodeDefinition = definition,
                position = cell,
                rotationQuarterTurns = ((rotation % 4) + 4) % 4
            };
        }
    }

    [CreateAssetMenu(menuName = "Arcane Atelier/Workshop/Content Database", fileName = "WorkshopContentDatabase")]
    public sealed class WorkshopContentDatabase : ScriptableObject
    {
        [SerializeField] private Vector2Int gridSize = new Vector2Int(8, 6);
        [SerializeField] private float simulationStepSeconds = 0.25f;
        [SerializeField] private WorkshopNodeDefinition[] placeableNodes;
        [SerializeField] private WorkshopRewardDefinition[] debugRewards;
        [SerializeField] private WorkshopPlacedNodeSeed[] defaultLayout;

        public Vector2Int GridSize => gridSize;
        public float SimulationStepSeconds => Mathf.Clamp(simulationStepSeconds, 0.05f, 1f);
        public WorkshopNodeDefinition[] PlaceableNodes => placeableNodes ?? Array.Empty<WorkshopNodeDefinition>();
        public WorkshopRewardDefinition[] DebugRewards => debugRewards ?? Array.Empty<WorkshopRewardDefinition>();
        public WorkshopPlacedNodeSeed[] DefaultLayout => defaultLayout ?? Array.Empty<WorkshopPlacedNodeSeed>();

        public void Configure(
            Vector2Int contentGridSize,
            float stepSeconds,
            WorkshopNodeDefinition[] nodes,
            WorkshopRewardDefinition[] rewards,
            WorkshopPlacedNodeSeed[] layout)
        {
            gridSize = contentGridSize;
            simulationStepSeconds = stepSeconds;
            placeableNodes = nodes ?? Array.Empty<WorkshopNodeDefinition>();
            debugRewards = rewards ?? Array.Empty<WorkshopRewardDefinition>();
            defaultLayout = layout ?? Array.Empty<WorkshopPlacedNodeSeed>();
        }

        public WorkshopRewardDefinition FindReward(string rewardId)
        {
            if (string.IsNullOrWhiteSpace(rewardId))
            {
                return null;
            }

            foreach (var reward in DebugRewards)
            {
                if (reward != null && reward.Id == rewardId)
                {
                    return reward;
                }
            }

            return null;
        }

        public IReadOnlyList<string> ValidateContent()
        {
            var errors = new List<string>();

            if (gridSize.x <= 0 || gridSize.y <= 0)
            {
                errors.Add("GridSize must be greater than zero on both axes.");
            }

            var nodeIds = new HashSet<string>();
            foreach (var node in (placeableNodes ?? Array.Empty<WorkshopNodeDefinition>()).Where(node => node != null))
            {
                if (string.IsNullOrWhiteSpace(node.Id))
                {
                    errors.Add($"Node '{node.name}' has an empty Id.");
                    continue;
                }

                if (!nodeIds.Add(node.Id))
                {
                    errors.Add($"Duplicate node Id detected: '{node.Id}'.");
                }
            }

            foreach (var seed in (defaultLayout ?? Array.Empty<WorkshopPlacedNodeSeed>()).Where(seed => seed != null))
            {
                if (seed.NodeDefinition == null)
                {
                    errors.Add($"Default layout seed at {seed.Position} is missing a NodeDefinition.");
                    continue;
                }

                if (seed.Position.x < 0 || seed.Position.y < 0 || seed.Position.x >= gridSize.x || seed.Position.y >= gridSize.y)
                {
                    errors.Add($"Default layout seed '{seed.NodeDefinition.DisplayName}' is outside grid bounds at {seed.Position}.");
                }
            }

            var rewardIds = new HashSet<string>();
            foreach (var reward in (debugRewards ?? Array.Empty<WorkshopRewardDefinition>()).Where(reward => reward != null))
            {
                if (string.IsNullOrWhiteSpace(reward.Id))
                {
                    errors.Add($"Reward '{reward.name}' has an empty Id.");
                    continue;
                }

                if (!rewardIds.Add(reward.Id))
                {
                    errors.Add($"Duplicate reward Id detected: '{reward.Id}'.");
                }
            }

            return errors;
        }

        private void OnValidate()
        {
            var errors = ValidateContent();
            if (errors.Count == 0)
            {
                return;
            }

            Debug.LogWarning(
                $"WorkshopContentDatabase '{name}' validation found {errors.Count} issue(s):\n- {string.Join("\n- ", errors)}",
                this);
        }
    }
}
