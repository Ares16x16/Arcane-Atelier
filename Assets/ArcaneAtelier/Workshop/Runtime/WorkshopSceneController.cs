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
        private const float DefaultWorkshopCellSize = 1.22f;

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
                Debug.LogWarning("WorkshopSceneController could not find a serialized WorkshopContentDatabase. Falling back to runtime-generated content.", this);
            }
            else if (!HasCompleteWorkshopContent(contentDatabase))
            {
                ownedRuntimeDatabase = WorkshopDefaultContentFactory.CreateRuntimeDatabase();
                contentDatabase = ownedRuntimeDatabase;
                Debug.LogWarning("WorkshopSceneController found stale workshop content. Falling back to the complete runtime content set.", this);
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
            if (Simulation.TryGetNode(cell, out var existingNode))
            {
                statusMessage = $"Selected {existingNode.Definition.DisplayName}. Press R to rotate or RMB to remove.";
                return;
            }

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
            int openingShieldBonus = remainingPreparationTicks > 0 ? 4 : 0;
            RunProgressBridge.RegisterPreparation(
                UsedPreparationTicks,
                WorkshopBattlePayloadBridge.CurrentPayload,
                openingShieldBonus);
            statusMessage = WorkshopBattlePayloadBridge.CurrentPayload.HasCards
                ? openingShieldBonus > 0
                    ? $"Battle payload committed. Early deploy grants +{openingShieldBonus} opening shield."
                    : "Battle payload committed."
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
            if (activeCamera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                cameraObject.transform.SetParent(transform, false);
                activeCamera = cameraObject.AddComponent<Camera>();
            }

            activeCamera.orthographic = true;
            activeCamera.orthographicSize = 4.8f;
            activeCamera.clearFlags = CameraClearFlags.SolidColor;
            activeCamera.backgroundColor = new Color(0.06f, 0.07f, 0.09f);
            FocusCameraOnStarterArea(activeCamera);
        }

        private void FocusCameraOnStarterArea(Camera activeCamera)
        {
            if (activeCamera == null)
            {
                return;
            }

            float cellSize = gridView != null ? gridView.CellSize : DefaultWorkshopCellSize;
            Vector2 focusCenter = new Vector2(11.5f * cellSize, 11f * cellSize);
            float targetOrthographicSize = activeCamera.orthographicSize;
            if (contentDatabase != null && contentDatabase.DefaultLayout.Length > 0)
            {
                var min = contentDatabase.DefaultLayout[0].Position;
                var max = min;
                foreach (var seed in contentDatabase.DefaultLayout)
                {
                    if (seed == null)
                    {
                        continue;
                    }

                    min = Vector2Int.Min(min, seed.Position);
                    max = Vector2Int.Max(max, seed.Position);
                }

                Vector2 minWorld = new Vector2(min.x * cellSize, min.y * cellSize) - Vector2.one * (cellSize * 1.6f);
                Vector2 maxWorld = new Vector2(max.x * cellSize, max.y * cellSize) + Vector2.one * (cellSize * 1.6f);
                focusCenter = (minWorld + maxWorld) * 0.5f;

                float contentHeight = Mathf.Max(cellSize * 4f, maxWorld.y - minWorld.y);
                float contentWidth = Mathf.Max(cellSize * 6f, maxWorld.x - minWorld.x);
                float fitHeight = contentHeight * 0.5f;
                float fitWidth = contentWidth * 0.5f / Mathf.Max(0.01f, activeCamera.aspect);
                targetOrthographicSize = Mathf.Max(4.8f, fitHeight, fitWidth);
            }

            activeCamera.orthographicSize = targetOrthographicSize;
            float padding = 1.2f;
            float verticalExtent = targetOrthographicSize;
            float horizontalExtent = verticalExtent * activeCamera.aspect;
            float maxX = Mathf.Max(focusCenter.x, (contentDatabase != null ? contentDatabase.GridSize.x - 1 : 49) * cellSize + padding);
            float maxY = Mathf.Max(focusCenter.y, (contentDatabase != null ? contentDatabase.GridSize.y - 1 : 49) * cellSize + padding);
            float clampedX = ClampCameraAxis(focusCenter.x, -padding, maxX, horizontalExtent);
            float clampedY = ClampCameraAxis(focusCenter.y, -padding, maxY, verticalExtent);
            activeCamera.transform.position = new Vector3(clampedX, clampedY, -10f);
        }

        private static float ClampCameraAxis(float value, float min, float max, float extent)
        {
            if (max - min <= extent * 2f)
            {
                return (min + max) * 0.5f;
            }

            return Mathf.Clamp(value, min + extent, max - extent);
        }

        private static bool ShouldConfigureSceneEnvironment()
        {
            var activeScene = SceneManager.GetActiveScene();
            return string.IsNullOrEmpty(activeScene.path) || activeScene.path == GeneratedWorkshopScenePath;
        }

        private static bool HasCompleteWorkshopContent(WorkshopContentDatabase database)
        {
            if (database == null)
            {
                return false;
            }

            if (database.GridSize.x < 50 || database.GridSize.y < 50)
            {
                return false;
            }

            string[] requiredNodeIds =
            {
                "node.factory.element_fusion",
                "node.factory.element_shaping",
                "node.factory.conduit",
                "node.factory.turn_conduit",
                "node.factory.turn_conduit.mirror",
                "node.factory.spell_conduit",
                "node.factory.turn_spell_conduit",
                "node.factory.turn_spell_conduit.mirror",
                "node.factory.spell_fusion.basic",
                "node.factory.spell_fusion.intermediate",
                "node.factory.spell_fusion.advanced"
            };

            var nodes = database.PlaceableNodes;
            foreach (string nodeId in requiredNodeIds)
            {
                if (!nodes.Any(node => node != null && node.Id == nodeId))
                {
                    return false;
                }
            }

            var shaper = nodes.FirstOrDefault(node => node != null && node.Id == "node.factory.element_shaping");
            if (shaper == null || shaper.Recipes.Count < 8)
            {
                return false;
            }

            var fusionOne = nodes.FirstOrDefault(node => node != null && node.Id == "node.factory.spell_fusion.basic");
            if (fusionOne == null || fusionOne.Recipes.Count < 8)
            {
                return false;
            }

            var fusionTwo = nodes.FirstOrDefault(node => node != null && node.Id == "node.factory.spell_fusion.intermediate");
            if (fusionTwo == null || fusionTwo.Recipes.Count < 12)
            {
                return false;
            }

            var fusionThree = nodes.FirstOrDefault(node => node != null && node.Id == "node.factory.spell_fusion.advanced");
            if (fusionThree == null || fusionThree.Recipes.Count < 4)
            {
                return false;
            }

            if (!HasCurrentDefaultDemoLayout(database))
            {
                return false;
            }

            string[] requiredRecipeIds =
            {
                "recipe.shape.fire",
                "recipe.shape.water",
                "recipe.shape.wind",
                "recipe.shape.earth",
                "recipe.shape.ice",
                "recipe.shape.thunder",
                "recipe.shape.light",
                "recipe.shape.dark",
                "recipe.fusion.basic.fire",
                "recipe.fusion.basic.water",
                "recipe.fusion.basic.wind",
                "recipe.fusion.basic.earth",
                "recipe.fusion.basic.ice",
                "recipe.fusion.basic.thunder",
                "recipe.fusion.basic.light",
                "recipe.fusion.basic.dark",
                "recipe.fusion.intermediate.ice",
                "recipe.fusion.intermediate.ice_alt_a",
                "recipe.fusion.intermediate.ice_alt_b",
                "recipe.fusion.intermediate.thunder",
                "recipe.fusion.intermediate.thunder_alt_a",
                "recipe.fusion.intermediate.thunder_alt_b",
                "recipe.fusion.intermediate.light",
                "recipe.fusion.intermediate.light_alt_a",
                "recipe.fusion.intermediate.light_alt_b",
                "recipe.fusion.intermediate.dark",
                "recipe.fusion.intermediate.dark_alt_a",
                "recipe.fusion.intermediate.dark_alt_b",
                "recipe.fusion.advanced.steam",
                "recipe.fusion.advanced.tempest",
                "recipe.fusion.advanced.prism",
                "recipe.fusion.advanced.polarity"
            };

            var recipeIds = new HashSet<string>(
                nodes.Where(node => node != null)
                    .SelectMany(node => node.Recipes)
                    .Where(recipe => recipe != null)
                    .Select(recipe => recipe.Id));

            return requiredRecipeIds.All(recipeIds.Contains);
        }

        private static bool HasCurrentDefaultDemoLayout(WorkshopContentDatabase database)
        {
            var layout = database.DefaultLayout;
            return ContainsSeed(layout, "node.spirit.fire", new Vector2Int(8, 13), 0) &&
                   ContainsSeed(layout, "node.factory.element_shaping", new Vector2Int(10, 13), 0) &&
                   ContainsSeed(layout, "node.factory.spell_fusion.basic", new Vector2Int(11, 13), 0) &&
                   ContainsSeed(layout, "node.factory.spell_conduit", new Vector2Int(12, 13), 0) &&
                   ContainsSeed(layout, "node.spirit.fire", new Vector2Int(11, 10), 3) &&
                   ContainsSeed(layout, "node.factory.element_shaping", new Vector2Int(11, 12), 3) &&
                   ContainsSeed(layout, "node.spirit.water", new Vector2Int(8, 9), 0) &&
                   ContainsSeed(layout, "node.factory.element_fusion", new Vector2Int(12, 9), 0) &&
                   ContainsSeed(layout, "node.factory.element_shaping", new Vector2Int(13, 9), 0) &&
                   ContainsSeed(layout, "node.spirit.wind", new Vector2Int(12, 8), 3);
        }

        private static bool ContainsSeed(WorkshopPlacedNodeSeed[] layout, string nodeId, Vector2Int position, int rotation)
        {
            return layout.Any(seed =>
                seed != null &&
                seed.NodeDefinition != null &&
                seed.NodeDefinition.Id == nodeId &&
                seed.Position == position &&
                seed.RotationQuarterTurns == rotation);
        }
    }
}
