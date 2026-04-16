using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ArcaneAtelier.Workshop
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(WorkshopGridView))]
    [RequireComponent(typeof(WorkshopHudPresenter))]
    public sealed class WorkshopSceneController : MonoBehaviour
    {
        private const int MaxSimulationCatchUpStepsPerFrame = 16;

        [SerializeField] private WorkshopContentDatabase contentDatabase;
        [SerializeField] private WorkshopGridView gridView;
        [SerializeField] private WorkshopHudPresenter hudPresenter;

        private float accumulatedSimulationTime;
        private string statusMessage = "Spell Assembly ready.";
        private bool isPaused;
        private WorkshopContentDatabase ownedRuntimeDatabase;

        public WorkshopSimulation Simulation { get; private set; }
        public WorkshopNodeDefinition SelectedPaletteNode { get; private set; }
        public Vector2Int SelectedCell { get; private set; } = new(-1, -1);
        public Vector2Int HoveredCell { get; private set; } = new(-1, -1);
        public int PlacementRotationQuarterTurns { get; private set; }
        public string StatusMessage => statusMessage;
        public bool IsPaused => isPaused;
        public float GridCellSize => gridView != null ? gridView.CellSize : 1.22f;

        public WorkshopNodeState SelectedNode =>
            Simulation != null && Simulation.TryGetNode(SelectedCell, out var nodeState) ? nodeState : null;
        public WorkshopNodeState HoveredNode =>
            Simulation != null && Simulation.TryGetNode(HoveredCell, out var nodeState) ? nodeState : null;

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

            if (contentDatabase == null)
            {
                ownedRuntimeDatabase = WorkshopDefaultContentFactory.CreateRuntimeDatabase();
                contentDatabase = ownedRuntimeDatabase;
            }

            EnsureSceneIs2DPlayable();

            var validationErrors = contentDatabase.ValidateContent();
            if (validationErrors.Count > 0)
            {
                statusMessage = $"WorkshopContentDatabase '{contentDatabase.name}' is invalid ({validationErrors.Count} error(s)); workshop disabled.";
                Debug.LogError(
                    $"{statusMessage}\n- {string.Join("\n- ", validationErrors)}",
                    this);
                enabled = false;
                return;
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
            Time.timeScale = 1f;
            if (Simulation != null)
            {
                Simulation.StateChanged -= HandleSimulationStateChanged;
            }

            if (ownedRuntimeDatabase != null)
            {
                Destroy(ownedRuntimeDatabase);
            }
        }

        private void Update()
        {
            HandleGlobalShortcuts();

            if (Simulation == null || isPaused)
            {
                return;
            }

            var stepSeconds = contentDatabase.SimulationStepSeconds;
            accumulatedSimulationTime += Time.deltaTime;
            var iterations = 0;
            while (accumulatedSimulationTime >= stepSeconds && iterations < MaxSimulationCatchUpStepsPerFrame)
            {
                Simulation.Step(stepSeconds);
                accumulatedSimulationTime -= stepSeconds;
                iterations++;
            }

            if (iterations == MaxSimulationCatchUpStepsPerFrame && accumulatedSimulationTime > stepSeconds)
            {
                accumulatedSimulationTime = stepSeconds;
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

        public void SetHoveredCell(Vector2Int cell)
        {
            HoveredCell = Simulation != null && Simulation.IsInsideGrid(cell) ? cell : new Vector2Int(-1, -1);
        }

        public void SetPaletteNode(WorkshopNodeDefinition definition)
        {
            if (definition != null && !Simulation.IsUnlocked(definition))
            {
                statusMessage = $"{definition.DisplayName} is still locked.";
                return;
            }

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

        public void RotatePlacementCounterClockwise()
        {
            PlacementRotationQuarterTurns = (PlacementRotationQuarterTurns + 3) % 4;
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
            if (reward == null)
            {
                statusMessage = "No reward selected.";
                return;
            }

            Simulation.ApplyReward(reward);
            statusMessage = $"Applied reward: {reward.DisplayName}.";
        }

        public void ResetWorkshop()
        {
            Simulation.ResetToDefaultLayout();
            PlacementRotationQuarterTurns = 0;
            statusMessage = "Workshop reset to default layout.";
        }

        public void CommitBattlePayload()
        {
            if (Simulation == null)
            {
                statusMessage = "Workshop is still booting.";
                return;
            }

            Simulation.CommitBattlePayload();
            statusMessage = WorkshopBattlePayloadBridge.CurrentPayload.HasCards
                ? "Battle payload committed."
                : "No crafted cards to commit.";
        }

        public WorkshopInventoryView BuildInventoryView()
        {
            return Simulation != null
                ? Simulation.BuildInventoryView()
                : new WorkshopInventoryView(
                    new Dictionary<WorkshopItemDefinition, int>(),
                    new Dictionary<WorkshopItemDefinition, int>());
        }

        public WorkshopFlowStatsView BuildFlowStatsView()
        {
            return Simulation != null
                ? Simulation.BuildFlowStatsView()
                : new WorkshopFlowStatsView(0f, 0f, 0f, 0f);
        }

        public void TogglePause()
        {
            isPaused = !isPaused;
            Time.timeScale = isPaused ? 0f : 1f;
            statusMessage = isPaused ? "Factory time paused." : "Factory time resumed.";
        }

        private void HandleSimulationStateChanged()
        {
            gridView?.RefreshVisuals();
            hudPresenter?.Repaint();
        }

        private void HandleGlobalShortcuts()
        {
            if (Simulation == null)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Space))
            {
                TogglePause();
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                RotatePlacementCounterClockwise();
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                RotatePlacementClockwise();
            }
        }

        private void EnsureSceneIs2DPlayable()
        {
            var activeCamera = Camera.main;
            if (activeCamera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                activeCamera = cameraObject.AddComponent<Camera>();
            }

            activeCamera.orthographic = true;
            activeCamera.orthographicSize = 4.8f;
            activeCamera.clearFlags = CameraClearFlags.SolidColor;
            activeCamera.backgroundColor = new Color(0.06f, 0.07f, 0.09f);
            activeCamera.transform.position = new Vector3(4.8f, 2.8f, -10f);

            foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                light.enabled = false;
                light.gameObject.SetActive(false);
            }

            if (gridView == null)
            {
                gridView = gameObject.AddComponent<WorkshopGridView>();
            }

            if (hudPresenter == null)
            {
                hudPresenter = gameObject.AddComponent<WorkshopHudPresenter>();
            }
        }
    }
}
