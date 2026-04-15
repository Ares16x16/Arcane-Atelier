using System.Linq;
using UnityEngine;

namespace ArcaneAtelier.Workshop
{
    public sealed class WorkshopSceneController : MonoBehaviour
    {
        [SerializeField] private WorkshopContentDatabase contentDatabase;
        [SerializeField] private WorkshopGridView gridView;
        [SerializeField] private WorkshopHudPresenter hudPresenter;

        private float accumulatedSimulationTime;
        private string statusMessage = "Spell Assembly ready.";

        public WorkshopSimulation Simulation { get; private set; }
        public WorkshopNodeDefinition SelectedPaletteNode { get; private set; }
        public Vector2Int SelectedCell { get; private set; } = new(-1, -1);
        public int PlacementRotationQuarterTurns { get; private set; }
        public string StatusMessage => statusMessage;

        public WorkshopNodeState SelectedNode =>
            Simulation != null && Simulation.TryGetNode(SelectedCell, out var nodeState) ? nodeState : null;

        public WorkshopNodeDefinition[] PlaceableNodes =>
            contentDatabase == null ? new WorkshopNodeDefinition[0] : contentDatabase.PlaceableNodes;

        public WorkshopRewardDefinition[] DebugRewards =>
            contentDatabase == null ? new WorkshopRewardDefinition[0] : contentDatabase.DebugRewards;

        public void Configure(WorkshopContentDatabase database, WorkshopGridView view, WorkshopHudPresenter hud)
        {
            contentDatabase = database;
            gridView = view;
            hudPresenter = hud;
        }

        private void Awake()
        {
            if (gridView == null)
            {
                gridView = GetComponent<WorkshopGridView>();
            }

            if (hudPresenter == null)
            {
                hudPresenter = GetComponent<WorkshopHudPresenter>();
            }

            Simulation = new WorkshopSimulation(contentDatabase);
            Simulation.StateChanged += HandleSimulationStateChanged;

            gridView?.Initialize(this);
            hudPresenter?.Initialize(this);
            SetPaletteNode(PlaceableNodes.FirstOrDefault(node => node != null && Simulation.IsUnlocked(node)));
            HandleSimulationStateChanged();
        }

        private void OnDestroy()
        {
            if (Simulation != null)
            {
                Simulation.StateChanged -= HandleSimulationStateChanged;
            }
        }

        private void Update()
        {
            if (Simulation == null)
            {
                return;
            }

            accumulatedSimulationTime += Time.deltaTime;
            while (accumulatedSimulationTime >= contentDatabase.SimulationStepSeconds)
            {
                Simulation.Step(contentDatabase.SimulationStepSeconds);
                accumulatedSimulationTime -= contentDatabase.SimulationStepSeconds;
            }
        }

        public void SetSelectedCell(Vector2Int cell)
        {
            if (!Simulation.IsInsideGrid(cell))
            {
                return;
            }

            SelectedCell = cell;
            gridView.RefreshVisuals();
        }

        public void SetPaletteNode(WorkshopNodeDefinition definition)
        {
            SelectedPaletteNode = definition;
            statusMessage = definition == null
                ? "Node palette cleared."
                : $"Placement armed: {definition.DisplayName}.";
        }

        public void RotatePlacementClockwise()
        {
            PlacementRotationQuarterTurns = (PlacementRotationQuarterTurns + 1) % 4;
            statusMessage = $"Placement rotation: {PlacementRotationQuarterTurns * 90}°.";
            gridView.RefreshVisuals();
        }

        public void TryPlaceSelectedNode(Vector2Int cell)
        {
            SetSelectedCell(cell);
            var result = Simulation.PlaceNode(cell, SelectedPaletteNode, PlacementRotationQuarterTurns);
            statusMessage = result.Message;
        }

        public void TryRemoveNode(Vector2Int cell)
        {
            SetSelectedCell(cell);
            var result = Simulation.RemoveNode(cell);
            statusMessage = result.Message;
        }

        public void RotatePlacedNode(Vector2Int cell)
        {
            SetSelectedCell(cell);
            Simulation.RotateNodeClockwise(cell);
            statusMessage = SelectedNode == null ? "No node selected." : $"Rotated {SelectedNode.Definition.DisplayName}.";
        }

        public void ApplyReward(WorkshopRewardDefinition reward)
        {
            Simulation.ApplyReward(reward);
            statusMessage = reward == null ? "No reward selected." : $"Applied reward: {reward.DisplayName}.";
        }

        public void ResetWorkshop()
        {
            Simulation.ResetToDefaultLayout();
            statusMessage = "Workshop reset to default layout.";
        }

        public void CommitBattlePayload()
        {
            Simulation.CommitBattlePayload();
            statusMessage = WorkshopBattlePayloadBridge.CurrentPayload.HasCards
                ? "Battle payload committed."
                : "No crafted cards to commit.";
        }

        public WorkshopInventoryView BuildInventoryView()
        {
            return Simulation.BuildInventoryView();
        }

        private void HandleSimulationStateChanged()
        {
            gridView?.RefreshVisuals();
            hudPresenter?.Repaint();
        }
    }
}
