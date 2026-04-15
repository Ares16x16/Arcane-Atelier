using System;
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
        [SerializeField] private Vector2Int gridSize = new(8, 6);
        [SerializeField] private float simulationStepSeconds = 0.25f;
        [SerializeField] private WorkshopNodeDefinition[] placeableNodes;
        [SerializeField] private WorkshopRewardDefinition[] debugRewards;
        [SerializeField] private WorkshopPlacedNodeSeed[] defaultLayout;

        public Vector2Int GridSize => gridSize;
        public float SimulationStepSeconds => Mathf.Clamp(simulationStepSeconds, 0.05f, 1f);
        public WorkshopNodeDefinition[] PlaceableNodes => placeableNodes;
        public WorkshopRewardDefinition[] DebugRewards => debugRewards;
        public WorkshopPlacedNodeSeed[] DefaultLayout => defaultLayout;

        public void Configure(
            Vector2Int contentGridSize,
            float stepSeconds,
            WorkshopNodeDefinition[] nodes,
            WorkshopRewardDefinition[] rewards,
            WorkshopPlacedNodeSeed[] layout)
        {
            gridSize = contentGridSize;
            simulationStepSeconds = stepSeconds;
            placeableNodes = nodes;
            debugRewards = rewards;
            defaultLayout = layout;
        }
    }
}
