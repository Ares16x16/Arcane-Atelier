using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArcaneAtelier.Workshop
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(WorkshopGridView))]
    [RequireComponent(typeof(WorkshopHudPresenter))]
    public sealed class WorkshopSceneController : MonoBehaviour
    {
        private const int MaxSimulationCatchUpStepsPerFrame = 16;
        private const string GeneratedWorkshopScenePath = "Assets/Scenes/SpellAssemblyScene.unity";

        [SerializeField] private WorkshopContentDatabase contentDatabase;
        [SerializeField] private WorkshopGridView gridView;
        [SerializeField] private WorkshopHudPresenter hudPresenter;
        [SerializeField] private bool autoConfigureSceneEnvironment = true;
        [SerializeField, Min(1)] private int defaultPreparationTickBudget = 120;

        private float accumulatedSimulationTime;
        private string statusMessage = "Spell Assembly ready.";
        private bool isPaused;
        private bool isDeploying;
        private int totalPreparationTicks;
        private int remainingPreparationTicks;
        private string encounterLabel = "Skirmish";
        private WorkshopContentDatabase ownedRuntimeDatabase;

        public WorkshopSimulation Simulation { get; private set; }
        public WorkshopNodeDefinition SelectedPaletteNode { get; private set; }
        public Vector2Int SelectedCell { get; private set; } = new Vector2Int(-1, -1);
        public Vector2Int HoveredCell { get; private set; } = new Vector2Int(-1, -1);
        public int PlacementRotationQuarterTurns { get; private set; }
        public string StatusMessage => statusMessage;
        public bool IsPaused => isPaused;
        public float GridCellSize => gridView != null ? gridView.CellSize : 1.22f;
        public int TotalPreparationTicks => totalPreparationTicks;
        public int RemainingPreparationTicks => remainingPreparationTicks;
        public int UsedPreparationTicks => Mathf.Max(0, totalPreparationTicks - remainingPreparationTicks);
        public string EncounterLabel => encounterLabel;

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
            SetPreparationBudget(defaultPreparationTickBudget, "Skirmish");
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

            if (Simulation == null || isPaused || isDeploying)
            {
                return;
            }

            var stepSeconds = contentDatabase.SimulationStepSeconds;
            accumulatedSimulationTime += Time.deltaTime;
            var iterations = 0;
            while (accumulatedSimulationTime >= stepSeconds && iterations < MaxSimulationCatchUpStepsPerFrame)
            {
                AdvancePreparationStep(stepSeconds);
                accumulatedSimulationTime -= stepSeconds;
                iterations++;

                if (isDeploying)
                {
                    break;
                }
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

        public bool TryApplyRewardById(string rewardId, out WorkshopRewardDefinition reward)
        {
            reward = contentDatabase != null ? contentDatabase.FindReward(rewardId) : null;
            if (reward == null)
            {
                return false;
            }

            ApplyReward(reward);
            return true;
        }

        public void SetStatusMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            statusMessage = message;
        }

        public void ResetWorkshop()
        {
            Simulation.ResetToDefaultLayout();
            PlacementRotationQuarterTurns = 0;
            statusMessage = "Workshop reset to default layout.";
        }

        public void SetPreparationBudget(int tickBudget, string label)
        {
            totalPreparationTicks = Mathf.Max(1, tickBudget);
            remainingPreparationTicks = totalPreparationTicks;
            encounterLabel = string.IsNullOrWhiteSpace(label) ? "Skirmish" : label;
            accumulatedSimulationTime = 0f;
            isDeploying = false;
            statusMessage = $"{encounterLabel} preparation started: {remainingPreparationTicks} ticks before breach.";
        }

        public void StepPreparationOnce()
        {
            if (Simulation == null || isDeploying)
            {
                return;
            }

            AdvancePreparationStep(contentDatabase.SimulationStepSeconds);
        }

        public void DeployToBattle()
        {
            if (isDeploying)
            {
                return;
            }

            isDeploying = true;
            Time.timeScale = 1f;
            CommitBattlePayload();
            SceneManager.LoadScene("BattleScene");
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

        private void AdvancePreparationStep(float stepSeconds)
        {
            if (remainingPreparationTicks <= 0)
            {
                DeployToBattle();
                return;
            }

            Simulation.Step(stepSeconds);
            remainingPreparationTicks = Mathf.Max(0, remainingPreparationTicks - 1);

            if (remainingPreparationTicks == 0)
            {
                statusMessage = "Breach opened. Deploying battle deck.";
                DeployToBattle();
            }
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
            if (gridView == null)
            {
                gridView = gameObject.AddComponent<WorkshopGridView>();
            }

            if (hudPresenter == null)
            {
                hudPresenter = gameObject.AddComponent<WorkshopHudPresenter>();
            }

            if (!autoConfigureSceneEnvironment || !ShouldConfigureSceneEnvironment())
            {
                return;
            }

            var activeCamera = Camera.main;
            var createdCamera = false;
            if (activeCamera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                cameraObject.transform.SetParent(transform, false);
                activeCamera = cameraObject.AddComponent<Camera>();
                createdCamera = true;
            }

            if (createdCamera || activeCamera.transform.IsChildOf(transform))
            {
                activeCamera.orthographic = true;
                activeCamera.orthographicSize = 4.8f;
                activeCamera.clearFlags = CameraClearFlags.SolidColor;
                activeCamera.backgroundColor = new Color(0.06f, 0.07f, 0.09f);
                activeCamera.transform.position = new Vector3(4.8f, 2.8f, -10f);
            }
        }

        private static bool ShouldConfigureSceneEnvironment()
        {
            var activeScene = SceneManager.GetActiveScene();
            return string.IsNullOrEmpty(activeScene.path) || activeScene.path == GeneratedWorkshopScenePath;
        }
    }
}
