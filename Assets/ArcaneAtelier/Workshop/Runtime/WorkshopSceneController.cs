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

        private static bool HasCompleteWorkshopContent(WorkshopContentDatabase database)
        {
            if (database == null)
            {
                return false;
            }

            string[] requiredNodeIds =
            {
                "node.factory.element_fusion",
                "node.factory.element_shaping",
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
            return ContainsSeed(layout, "node.spirit.fire", new Vector2Int(0, 5), 0) &&
                   ContainsSeed(layout, "node.factory.element_shaping", new Vector2Int(2, 5), 0) &&
                   ContainsSeed(layout, "node.factory.spell_fusion.basic", new Vector2Int(3, 5), 0) &&
                   ContainsSeed(layout, "node.spirit.fire", new Vector2Int(3, 2), 3) &&
                   ContainsSeed(layout, "node.factory.element_shaping", new Vector2Int(3, 4), 3) &&
                   ContainsSeed(layout, "node.spirit.water", new Vector2Int(0, 1), 0) &&
                   ContainsSeed(layout, "node.factory.element_fusion", new Vector2Int(4, 1), 0) &&
                   ContainsSeed(layout, "node.factory.element_shaping", new Vector2Int(5, 1), 0) &&
                   ContainsSeed(layout, "node.spirit.wind", new Vector2Int(4, 0), 3);
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
