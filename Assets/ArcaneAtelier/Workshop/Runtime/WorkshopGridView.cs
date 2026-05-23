using System.Collections.Generic;
using System.Linq;
using ArcaneAtelier;
using UnityEngine;

namespace ArcaneAtelier.Workshop
{
    public sealed class WorkshopGridView : MonoBehaviour
    {
        [SerializeField] private float cellSize = 1.22f;
        [SerializeField] private Color gridTintA = new Color(0.08f, 0.1f, 0.14f, 0.92f);
        [SerializeField] private Color gridTintB = new Color(0.1f, 0.13f, 0.18f, 0.92f);
        [SerializeField] private Color selectedTint = new Color(0.93f, 0.73f, 0.28f, 0.98f);
        [SerializeField] private Color hoverTint = new Color(0.32f, 0.58f, 0.76f, 0.9f);
        [SerializeField] private Color boardTint = new Color(0.025f, 0.035f, 0.06f, 1f);
        [SerializeField, Min(1.01f)] private float wheelZoomFactor = 1.12f;
        [SerializeField, Min(1f)] private float minZoom = 2.8f;
        [SerializeField, Min(1f)] private float maxZoom = 18f;
        [SerializeField, Min(0f)] private float cameraBoundsPadding = 4f;
        [SerializeField, Min(0f)] private float dragPixelThreshold = 5f;

        private const float BodySpriteTargetScale = 0.76f;
        private const float ActivityZoomBoostStart = 6f;
        private const int MajorGridStep = 5;

        private readonly Dictionary<Vector2Int, SpriteRenderer> cellRenderers = new Dictionary<Vector2Int, SpriteRenderer>();
        private readonly Dictionary<Vector2Int, SpriteRenderer> cellTextureRenderers = new Dictionary<Vector2Int, SpriteRenderer>();
        private readonly Dictionary<Vector2Int, NodeVisual> nodeVisuals = new Dictionary<Vector2Int, NodeVisual>();

        private Camera cachedCamera;
        private WorkshopSceneController controller;
        private Sprite sharedSprite;
        private Sprite workshopBackgroundSprite;
        private Sprite boardUnderlaySprite;
        private Sprite tileSpriteA;
        private Sprite tileSpriteB;
        private Sprite tileHoverSprite;
        private Sprite tileSelectedSprite;
        private Sprite leylineHorizontalSprite;
        private Sprite leylineVerticalSprite;
        private Transform gridRoot;
        private Transform nodeRoot;
        private SpriteRenderer hoverCellOverlayRenderer;
        private SpriteRenderer selectedCellOverlayRenderer;
        private Vector2Int hoveredCell = new Vector2Int(-1, -1);
        private bool isDraggingCamera;
        private bool isTrackingLeftPress;
        private Vector2 leftPressScreenPosition;
        private Vector3 dragCameraOrigin;
        private bool cameraStateInitialized;
        private Vector3 targetCameraPosition;
        private float targetOrthographicSize;

        private sealed class NodeVisual
        {
            public GameObject Root;
            public Transform VisualRoot;  // parent for Body/Frame/Shadow — gets rotation
            public SpriteRenderer Shadow;
            public SpriteRenderer Aura;
            public SpriteRenderer BreathRing;
            public SpriteRenderer Frame;
            public SpriteRenderer Body;
            public SpriteRenderer CoreGlow;
            public SpriteRenderer FlowComet;
            public List<SpriteRenderer> EdgeGlows = new List<SpriteRenderer>();
            public List<SpriteRenderer> OutputBeams = new List<SpriteRenderer>();
            public List<SpriteRenderer> PortMarkers = new List<SpriteRenderer>();
        }

        public float CellSize => cellSize;

        public void Initialize(WorkshopSceneController sceneController)
        {
            controller = sceneController;
            cachedCamera = Camera.main;
            sharedSprite = CreateSquareSprite();
            LoadWorkshopArt();
            ApplyWorkshopTheme();
            InitializeCameraState();

            BuildGrid();
            RefreshVisuals();
        }

        private void Update()
        {
            if (controller == null || cachedCamera == null)
            {
                return;
            }

            var simulation = controller.Simulation;
            if (simulation == null)
            {
                return;
            }

            HandleCameraNavigation(simulation);

            var mouseGuiPosition = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            var pointerOverFactory = IsPointerOverFactoryViewport(mouseGuiPosition);
            var worldPosition = pointerOverFactory ? ScreenToWorldUsingTargetCamera(Input.mousePosition) : Vector3.zero;
            var cell = pointerOverFactory ? WorldToCell(worldPosition) : new Vector2Int(-1, -1);
            var isInsideGrid = pointerOverFactory && simulation.IsInsideGrid(cell);
            var nextHoveredCell = isInsideGrid ? cell : new Vector2Int(-1, -1);
            if (nextHoveredCell != hoveredCell)
            {
                hoveredCell = nextHoveredCell;
                controller.SetHoveredCell(hoveredCell);
                RefreshVisuals();
            }

            if (pointerOverFactory && Input.GetMouseButtonDown(1) && isInsideGrid)
            {
                controller.TryRemoveNode(cell);
            }

            if (Input.GetKeyDown(KeyCode.R) && controller.SelectedCell.x >= 0)
            {
                controller.RotatePlacedNode(controller.SelectedCell);
            }

            UpdateAnimatedNodeEffects(simulation);
            UpdateSelectionOverlays();
        }

        private void LateUpdate()
        {
            if (cachedCamera == null || !cachedCamera.orthographic)
            {
                return;
            }

            InitializeCameraState();
            cachedCamera.orthographicSize = targetOrthographicSize;
            cachedCamera.transform.position = targetCameraPosition;
        }

        private void OnGUI()
        {
            if (controller == null || cachedCamera == null || controller.Simulation == null)
            {
                return;
            }

            Event current = Event.current;
            if (current == null || current.type != EventType.ScrollWheel || !IsPointerOverFactoryViewport(current.mousePosition))
            {
                return;
            }

            var screenPosition = new Vector3(
                current.mousePosition.x,
                Screen.height - current.mousePosition.y,
                0f);
            ZoomAtScreenPosition(controller.Simulation, screenPosition, current.delta.y);
            current.Use();
        }

        private void HandleCameraNavigation(WorkshopSimulation simulation)
        {
            if (cachedCamera == null || simulation == null || !cachedCamera.orthographic)
            {
                return;
            }

            InitializeCameraState();
            var mouseGuiPosition = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            var pointerOverFactory = IsPointerOverFactoryViewport(mouseGuiPosition);

            if (Input.GetMouseButtonDown(0) && pointerOverFactory)
            {
                isTrackingLeftPress = true;
                isDraggingCamera = false;
                leftPressScreenPosition = Input.mousePosition;
                dragCameraOrigin = targetCameraPosition;
            }

            if (isTrackingLeftPress && Input.GetMouseButton(0))
            {
                if (!isDraggingCamera)
                {
                    var draggedFarEnough = (leftPressScreenPosition - (Vector2)Input.mousePosition).sqrMagnitude >= dragPixelThreshold * dragPixelThreshold;
                    if (draggedFarEnough)
                    {
                        isDraggingCamera = true;
                    }
                }

                if (isDraggingCamera)
                {
                    var screenDelta = (Vector2)Input.mousePosition - leftPressScreenPosition;
                    var unitsPerPixelY = targetOrthographicSize * 2f / Mathf.Max(1, Screen.height);
                    var unitsPerPixelX = targetOrthographicSize * 2f * cachedCamera.aspect / Mathf.Max(1, Screen.width);
                    targetCameraPosition = dragCameraOrigin + new Vector3(
                        -screenDelta.x * unitsPerPixelX,
                        -screenDelta.y * unitsPerPixelY,
                        0f);
                    ClampCameraTargetToBoard(simulation);
                }
            }

            if (isTrackingLeftPress && Input.GetMouseButtonUp(0))
            {
                var releasedWithoutDrag = !isDraggingCamera;
                isTrackingLeftPress = false;
                isDraggingCamera = false;

                if (releasedWithoutDrag)
                {
                    var releasedWorldPosition = ScreenToWorldUsingTargetCamera(Input.mousePosition);
                    var releasedCell = WorldToCell(releasedWorldPosition);
                    if (simulation.IsInsideGrid(releasedCell))
                    {
                        if (simulation.TryGetNode(releasedCell, out var releasedNode) &&
                            releasedNode.HasEditablePorts &&
                            TryGetClickedEdgeDirection(releasedWorldPosition, releasedCell, out NodePortMask direction))
                        {
                            controller.CyclePlacedNodePort(releasedCell, direction);
                            return;
                        }

                        controller.TryPlaceSelectedNode(releasedCell);
                    }
                }
            }
        }

        private bool TryGetClickedEdgeDirection(Vector3 worldPosition, Vector2Int cell, out NodePortMask direction)
        {
            Vector3 center = CellToWorld(cell);
            Vector2 local = new Vector2(worldPosition.x - center.x, worldPosition.y - center.y);
            float edgeThreshold = cellSize * 0.28f;
            direction = NodePortMask.None;

            if (Mathf.Abs(local.x) >= Mathf.Abs(local.y))
            {
                if (Mathf.Abs(local.x) < edgeThreshold)
                {
                    return false;
                }

                direction = local.x > 0f ? NodePortMask.East : NodePortMask.West;
                return true;
            }

            if (Mathf.Abs(local.y) < edgeThreshold)
            {
                return false;
            }

            direction = local.y > 0f ? NodePortMask.North : NodePortMask.South;
            return true;
        }

        private void ZoomAtScreenPosition(WorkshopSimulation simulation, Vector3 screenPosition, float wheelDelta)
        {
            if (cachedCamera == null || simulation == null || Mathf.Abs(wheelDelta) <= 0.0001f)
            {
                return;
            }

            InitializeCameraState();
            var worldBeforeZoom = ScreenToWorldUsingTargetCamera(screenPosition);
            var nextSize = Mathf.Clamp(
                targetOrthographicSize * Mathf.Pow(wheelZoomFactor, wheelDelta),
                minZoom,
                maxZoom);
            if (Mathf.Approximately(nextSize, targetOrthographicSize))
            {
                return;
            }

            targetOrthographicSize = nextSize;
            var worldAfterZoom = ScreenToWorldUsingTargetCamera(screenPosition);
            targetCameraPosition += worldBeforeZoom - worldAfterZoom;
            ClampCameraTargetToBoard(simulation);
        }

        private static bool IsPointerOverFactoryViewport(Vector2 guiPosition)
        {
            const float rightRailWidth = 404f;
            const float bottomDockHeight = 292f;
            const float topHudHeight = 94f;
            const float margin = 10f;

            if (guiPosition.y <= topHudHeight + margin)
            {
                return false;
            }

            if (guiPosition.x >= Screen.width - rightRailWidth - margin)
            {
                return false;
            }

            if (guiPosition.y >= Screen.height - bottomDockHeight - margin)
            {
                return false;
            }

            return true;
        }

        private void InitializeCameraState()
        {
            if (cameraStateInitialized || cachedCamera == null)
            {
                return;
            }

            targetCameraPosition = cachedCamera.transform.position;
            targetOrthographicSize = Mathf.Clamp(cachedCamera.orthographicSize, minZoom, maxZoom);
            cameraStateInitialized = true;
        }

        private Vector3 ScreenToWorldUsingTargetCamera(Vector3 screenPosition)
        {
            InitializeCameraState();
            if (cachedCamera == null)
            {
                return Vector3.zero;
            }

            var screenWidth = Mathf.Max(1, Screen.width);
            var screenHeight = Mathf.Max(1, Screen.height);
            var unitsPerPixelY = targetOrthographicSize * 2f / screenHeight;
            var unitsPerPixelX = targetOrthographicSize * 2f * cachedCamera.aspect / screenWidth;
            return new Vector3(
                targetCameraPosition.x + (screenPosition.x - screenWidth * 0.5f) * unitsPerPixelX,
                targetCameraPosition.y + (screenPosition.y - screenHeight * 0.5f) * unitsPerPixelY,
                targetCameraPosition.z);
        }

        private void ClampCameraTargetToBoard(WorkshopSimulation simulation)
        {
            if (cachedCamera == null || simulation == null)
            {
                return;
            }

            var minX = -cameraBoundsPadding;
            var minY = -cameraBoundsPadding;
            var maxX = (simulation.GridSize.x - 1) * cellSize + cameraBoundsPadding;
            var maxY = (simulation.GridSize.y - 1) * cellSize + cameraBoundsPadding;
            var verticalExtent = targetOrthographicSize;
            var horizontalExtent = verticalExtent * cachedCamera.aspect;
            var position = targetCameraPosition;

            position.x = ClampAxis(position.x, minX, maxX, horizontalExtent);
            position.y = ClampAxis(position.y, minY, maxY, verticalExtent);
            targetCameraPosition = position;
        }

        private static float ClampAxis(float value, float min, float max, float extent)
        {
            if (max - min <= extent * 2f)
            {
                return (min + max) * 0.5f;
            }

            return Mathf.Clamp(value, min + extent, max - extent);
        }

        public void RefreshVisuals()
        {
            if (controller == null)
            {
                return;
            }

            foreach (var pair in cellRenderers)
            {
                pair.Value.color = GetBaseCellTint(pair.Key);
            }

            if (tileSelectedSprite == null &&
                controller.SelectedCell.x >= 0 &&
                cellRenderers.TryGetValue(controller.SelectedCell, out var selectedRenderer))
            {
                selectedRenderer.color = selectedTint;
            }

            if (tileHoverSprite == null &&
                hoveredCell.x >= 0 &&
                cellRenderers.TryGetValue(hoveredCell, out var hoverRenderer))
            {
                hoverRenderer.color = controller.SelectedCell == hoveredCell ? selectedTint : hoverTint;
            }

            UpdateSelectionOverlays();

            foreach (var key in nodeVisuals.Keys.ToArray())
            {
                if (controller.Simulation.Nodes.ContainsKey(key))
                {
                    continue;
                }

                Destroy(nodeVisuals[key].Root);
                nodeVisuals.Remove(key);
            }

            foreach (var pair in controller.Simulation.Nodes)
            {
                if (!nodeVisuals.TryGetValue(pair.Key, out var visual))
                {
                    visual = CreateNodeVisual(pair.Key);
                    nodeVisuals.Add(pair.Key, visual);
                }

                UpdateNodeVisual(pair.Key, visual, pair.Value);
            }
        }

        private void BuildGrid()
        {
            if (gridRoot != null)
            {
                Destroy(gridRoot.gameObject);
            }

            if (nodeRoot != null)
            {
                Destroy(nodeRoot.gameObject);
            }

            cellRenderers.Clear();
            cellTextureRenderers.Clear();
            nodeVisuals.Clear();
            hoverCellOverlayRenderer = null;
            selectedCellOverlayRenderer = null;

            gridRoot = new GameObject("Workshop Board").transform;
            gridRoot.SetParent(transform, false);

            nodeRoot = new GameObject("Node Visuals").transform;
            nodeRoot.SetParent(transform, false);

            float boardWidth = controller.Simulation.GridSize.x * cellSize + 1.4f;
            float boardHeight = controller.Simulation.GridSize.y * cellSize + 1.4f;
            Vector3 boardCenter = new Vector3(
                (controller.Simulation.GridSize.x - 1) * cellSize * 0.5f,
                (controller.Simulation.GridSize.y - 1) * cellSize * 0.5f,
                1f);

            CreateBoardLayer("Board Shadow", 2.4f, new Color(0f, 0f, 0f, 0.32f), -5, new Vector3(0.12f, -0.14f, 0f));

            if (workshopBackgroundSprite != null)
            {
                CreateScaledSpriteLayer(
                    "Workshop Far Background",
                    workshopBackgroundSprite,
                    boardCenter + new Vector3(0f, 0f, 0.15f),
                    boardWidth + 18f,
                    boardHeight + 18f,
                    -8,
                    Color.white,
                    true);
            }

            if (boardUnderlaySprite != null)
            {
                CreateScaledSpriteLayer(
                    "Board Underlay",
                    boardUnderlaySprite,
                    boardCenter,
                    boardWidth,
                    boardHeight,
                    -2,
                    Color.white);
            }
            else
            {
                var board = new GameObject("Board Backdrop");
                board.transform.SetParent(gridRoot, false);
                board.transform.position = boardCenter;
                board.transform.localScale = new Vector3(boardWidth, boardHeight, 1f);
                var boardRenderer = board.AddComponent<SpriteRenderer>();
                boardRenderer.sprite = sharedSprite;
                boardRenderer.color = boardTint;
                boardRenderer.sortingOrder = -2;

                CreateBoardLayer("Outer Brass Frame", 1.95f, new Color(0.58f, 0.43f, 0.18f, 0.32f), -4, Vector3.zero);
                CreateBoardLayer("Inner Arcane Wash", 0.62f, new Color(0.1f, 0.2f, 0.28f, 0.2f), -1, Vector3.zero);
            }

            for (var y = 0; y < controller.Simulation.GridSize.y; y++)
            {
                for (var x = 0; x < controller.Simulation.GridSize.x; x++)
                {
                    var cell = new Vector2Int(x, y);
                    var go = new GameObject($"Cell_{x}_{y}");
                    go.transform.SetParent(gridRoot, false);
                    go.transform.position = CellToWorld(cell);
                    go.transform.localScale = Vector3.one * (cellSize * 0.95f);

                    var spriteRenderer = go.AddComponent<SpriteRenderer>();
                    spriteRenderer.sprite = sharedSprite;
                    spriteRenderer.color = GetBaseCellTint(cell);
                    spriteRenderer.sortingOrder = 0;
                    cellRenderers.Add(cell, spriteRenderer);

                    Sprite tileSprite = ((x + y) & 1) == 0 ? tileSpriteA : tileSpriteB;
                    if (tileSprite != null)
                    {
                        var tileObject = new GameObject($"TileArt_{x}_{y}");
                        tileObject.transform.SetParent(go.transform, false);
                        tileObject.transform.localPosition = Vector3.zero;
                        tileObject.transform.localScale = Vector3.one * CalculateSpriteScaleToFit(tileSprite, cellSize * 0.965f);

                        var tileRenderer = tileObject.AddComponent<SpriteRenderer>();
                        tileRenderer.sprite = tileSprite;
                        tileRenderer.color = new Color(1f, 1f, 1f, 0.74f);
                        tileRenderer.sortingOrder = 1;
                        cellTextureRenderers.Add(cell, tileRenderer);
                    }
                }
            }

            BuildMajorGridLines();
            selectedCellOverlayRenderer = CreateCellOverlayRenderer("Selected Cell Overlay", tileSelectedSprite, 2);
            hoverCellOverlayRenderer = CreateCellOverlayRenderer("Hover Cell Overlay", tileHoverSprite, 3);
        }

        private NodeVisual CreateNodeVisual(Vector2Int cell)
        {
            var root = new GameObject($"Node_{cell.x}_{cell.y}");
            root.transform.SetParent(nodeRoot, false);
            root.transform.position = CellToWorld(cell);
            root.transform.localScale = Vector3.one * cellSize;

            var visualGroup = new GameObject("VisualGroup");
            visualGroup.transform.SetParent(root.transform, false);
            visualGroup.transform.localPosition = Vector3.zero;

            var visual = new NodeVisual
            {
                Root = root,
                VisualRoot = visualGroup.transform,
                Shadow = CreateVisualLayer(visualGroup.transform, "Shadow", new Vector3(0.05f, -0.06f, 0f), Vector3.one * 0.96f, new Color(0f, 0f, 0f, 0.44f), 2),
                Aura = CreateVisualLayer(root.transform, "Activity Aura", Vector3.zero, Vector3.one * 1.08f, new Color(0.35f, 0.85f, 1f, 0f), 3),
                BreathRing = CreateVisualLayer(root.transform, "Neon Breath Ring", Vector3.zero, Vector3.one * 1.18f, new Color(0.35f, 0.85f, 1f, 0f), 3),
                Frame = CreateVisualLayer(visualGroup.transform, "Frame", Vector3.zero, Vector3.one * 0.9f, new Color(0.24f, 0.21f, 0.18f), 4),
                Body = CreateVisualLayer(visualGroup.transform, "Body", Vector3.zero, Vector3.one * BodySpriteTargetScale, Color.white, 5),
                CoreGlow = CreateVisualLayer(root.transform, "Activity Core Glow", Vector3.zero, Vector3.one * 0.78f, new Color(0.35f, 0.85f, 1f, 0f), 6),
                FlowComet = CreateVisualLayer(root.transform, "Activity Flow Comet", Vector3.zero, Vector3.one * 0.14f, Color.white, 9)
            };

            CreateActivityEdge(visual, "North Edge Glow", new Vector3(0f, 0.47f, 0f), new Vector3(0.9f, 0.055f, 1f));
            CreateActivityEdge(visual, "East Edge Glow", new Vector3(0.47f, 0f, 0f), new Vector3(0.055f, 0.9f, 1f));
            CreateActivityEdge(visual, "South Edge Glow", new Vector3(0f, -0.47f, 0f), new Vector3(0.9f, 0.055f, 1f));
            CreateActivityEdge(visual, "West Edge Glow", new Vector3(-0.47f, 0f, 0f), new Vector3(0.055f, 0.9f, 1f));

            foreach (var direction in WorkshopDirectionUtility.CardinalDirections)
            {
                var beamGo = new GameObject($"{direction} Flow Beam");
                beamGo.transform.SetParent(root.transform, false);
                beamGo.transform.localPosition = Vector3.zero;
                beamGo.transform.localScale = Vector3.one * 0.2f;

                var beamRenderer = beamGo.AddComponent<SpriteRenderer>();
                beamRenderer.sprite = sharedSprite;
                beamRenderer.sortingOrder = 8;
                visual.OutputBeams.Add(beamRenderer);

                var portGo = new GameObject(direction.ToString());
                portGo.transform.SetParent(root.transform, false);
                var offset = WorkshopDirectionUtility.ToOffset(direction);
                portGo.transform.localPosition = new Vector3(offset.x, offset.y, 0f) * 0.36f;
                portGo.transform.localScale = Vector3.one * 0.18f;

                var portRenderer = portGo.AddComponent<SpriteRenderer>();
                portRenderer.sprite = sharedSprite;
                portRenderer.sortingOrder = 10;
                visual.PortMarkers.Add(portRenderer);
            }

            SetActivityRenderersEnabled(visual, false);
            return visual;
        }

        private void UpdateNodeVisual(Vector2Int cell, NodeVisual visual, WorkshopNodeState state)
        {
            visual.Root.transform.position = CellToWorld(state.Position);
            visual.VisualRoot.localRotation = Quaternion.Euler(0, 0, -90f * state.RotationQuarterTurns);
            var nodeSprite = ResolveNodeSprite(state.Definition);
            var nodeTint = ResolveNodeSpriteTint(state.Definition);
            if (nodeSprite != null)
            {
                var spriteScale = CalculateSpriteFitScale(nodeSprite, BodySpriteTargetScale);
                var spriteDirection = WorkshopNodeVariantUtility.ShouldMirrorSprite(state.Definition.Id) ? -1f : 1f;
                visual.Body.sprite = nodeSprite;
                visual.Body.color = nodeTint;
                visual.Body.transform.localScale = new Vector3(spriteScale * spriteDirection, spriteScale, 1f);
            }
            else
            {
                var fallbackScale = BodySpriteTargetScale;
                var spriteDirection = WorkshopNodeVariantUtility.ShouldMirrorSprite(state.Definition.Id) ? -1f : 1f;
                visual.Body.sprite = sharedSprite;
                visual.Body.color = state.Definition.Tint;
                visual.Body.transform.localScale = new Vector3(fallbackScale * spriteDirection, fallbackScale, 1f);
            }
            visual.Frame.color = Color.Lerp(new Color(0.18f, 0.21f, 0.28f), ResolveActivityColor(state.Definition), 0.28f);
            visual.Shadow.color = new Color(0f, 0f, 0f, 0.44f);

            for (var index = 0; index < WorkshopDirectionUtility.CardinalDirections.Count; index++)
            {
                var direction = WorkshopDirectionUtility.CardinalDirections[index];
                var renderer = visual.PortMarkers[index];

                var isInput = (state.RotatedInputPorts & direction) != 0;
                var isOutput = (state.RotatedOutputPorts & direction) != 0;
                var showEditablePreview = state.HasEditablePorts && (controller.SelectedCell == cell || hoveredCell == cell);
                renderer.enabled = isInput || isOutput || showEditablePreview;
                renderer.color = isOutput
                    ? new Color(0.91f, 0.62f, 0.24f, 1f)
                    : isInput
                        ? new Color(0.36f, 0.78f, 0.95f, 1f)
                        : new Color(0.82f, 0.9f, 1f, 0.28f);
            }

            UpdateNodeEnergyVisual(cell, visual, state);
        }

        private void UpdateAnimatedNodeEffects(WorkshopSimulation simulation)
        {
            if (simulation == null)
            {
                return;
            }

            foreach (var pair in nodeVisuals)
            {
                if (!simulation.Nodes.TryGetValue(pair.Key, out WorkshopNodeState state))
                {
                    continue;
                }

                UpdateNodeEnergyVisual(pair.Key, pair.Value, state);
            }
        }

        private void CreateActivityEdge(NodeVisual visual, string objectName, Vector3 localPosition, Vector3 localScale)
        {
            var edge = CreateVisualLayer(visual.Root.transform, objectName, localPosition, localScale, new Color(0.35f, 0.85f, 1f, 0f), 6);
            visual.EdgeGlows.Add(edge);
        }

        private void UpdateNodeEnergyVisual(Vector2Int cell, NodeVisual visual, WorkshopNodeState state)
        {
            if (visual == null || state == null)
            {
                return;
            }

            var isActive = state.IsRecentlyActive;
            SetActivityRenderersEnabled(visual, isActive);
            if (!isActive)
            {
                return;
            }

            var accent = ResolveActivityColor(state.Definition);
            var whiteHot = Color.Lerp(accent, Color.white, 0.35f);
            var zoomT = cachedCamera == null
                ? 0f
                : Mathf.InverseLerp(ActivityZoomBoostStart, maxZoom, targetOrthographicSize);
            var activitySeed = cell.x * 0.73f + cell.y * 0.41f;
            var breathPhase = Mathf.Repeat(Time.time * 0.52f + activitySeed * 0.07f, 1f);
            var inhale = breathPhase < 0.72f
                ? Mathf.SmoothStep(0f, 1f, breathPhase / 0.72f)
                : 1f - Mathf.SmoothStep(0f, 1f, (breathPhase - 0.72f) / 0.28f);
            var exhale = breathPhase < 0.72f
                ? 0f
                : Mathf.SmoothStep(0f, 1f, (breathPhase - 0.72f) / 0.28f);
            var breathEnergy = Mathf.Clamp01(0.46f + inhale * 0.28f + exhale * 0.34f);
            var shimmer = 0.86f + Mathf.Sin(Time.time * 3.2f + activitySeed) * 0.08f;
            var inhaleScale = Mathf.Lerp(1.24f, 1.04f, inhale);
            var exhaleScale = Mathf.Lerp(inhaleScale, 1.34f, exhale);
            var auraScale = Mathf.Lerp(exhaleScale, exhaleScale + 0.16f, zoomT);
            var ringScale = Mathf.Lerp(0.9f, Mathf.Lerp(1.34f, 1.54f, zoomT), exhale);

            visual.BreathRing.transform.localScale = Vector3.one * ringScale;
            visual.BreathRing.color = WithAlpha(whiteHot, Mathf.Lerp(0.015f, 0.2f, exhale) * (1f - inhale * 0.35f));
            visual.Aura.transform.localScale = Vector3.one * auraScale;
            visual.Aura.color = WithAlpha(accent, Mathf.Lerp(0.08f, 0.19f, zoomT) * shimmer * breathEnergy);
            visual.CoreGlow.transform.localScale = Vector3.one * Mathf.Lerp(Mathf.Lerp(0.66f, 0.84f, inhale), 0.96f, exhale * 0.5f + zoomT * 0.25f);
            visual.CoreGlow.color = WithAlpha(whiteHot, Mathf.Lerp(0.055f, 0.16f, zoomT) * shimmer * breathEnergy);

            var edgeLength = Mathf.Lerp(0.9f, 1.05f, zoomT);
            var edgeThickness = Mathf.Lerp(0.052f, 0.09f, zoomT) * Mathf.Lerp(0.86f, 1.28f, breathEnergy);
            UpdateEdgeGlow(visual.EdgeGlows[0], new Vector3(0f, 0.47f, 0f), new Vector3(edgeLength, edgeThickness, 1f), accent, 0, activitySeed, zoomT, breathEnergy);
            UpdateEdgeGlow(visual.EdgeGlows[1], new Vector3(0.47f, 0f, 0f), new Vector3(edgeThickness, edgeLength, 1f), accent, 1, activitySeed, zoomT, breathEnergy);
            UpdateEdgeGlow(visual.EdgeGlows[2], new Vector3(0f, -0.47f, 0f), new Vector3(edgeLength, edgeThickness, 1f), accent, 2, activitySeed, zoomT, breathEnergy);
            UpdateEdgeGlow(visual.EdgeGlows[3], new Vector3(-0.47f, 0f, 0f), new Vector3(edgeThickness, edgeLength, 1f), accent, 3, activitySeed, zoomT, breathEnergy);

            var pathProgress = Mathf.Repeat(Time.time * 0.9f + activitySeed * 0.08f, 1f);
            visual.FlowComet.transform.localPosition = EvaluatePerimeterPosition(pathProgress, Mathf.Lerp(0.48f, 0.54f, zoomT));
            visual.FlowComet.transform.localScale = Vector3.one * Mathf.Lerp(0.14f, 0.22f, zoomT);
            visual.FlowComet.transform.localRotation = Quaternion.Euler(0f, 0f, 45f + Time.time * 210f);
            visual.FlowComet.color = WithAlpha(whiteHot, Mathf.Lerp(0.8f, 1f, zoomT));

            for (var index = 0; index < WorkshopDirectionUtility.CardinalDirections.Count; index++)
            {
                var direction = WorkshopDirectionUtility.CardinalDirections[index];
                var beam = visual.OutputBeams[index];
                var outputActive = (state.RotatedOutputPorts & direction) != 0;
                beam.enabled = outputActive;
                if (!outputActive)
                {
                    continue;
                }

                UpdateOutputBeam(beam, direction, accent, index, activitySeed, zoomT, breathEnergy);
            }
        }

        private static void UpdateEdgeGlow(SpriteRenderer renderer, Vector3 position, Vector3 scale, Color accent, int index, float seed, float zoomT, float breathEnergy)
        {
            var edgePulse = 0.78f + Mathf.Sin(Time.time * 2.8f + seed + index * 0.9f) * 0.14f;
            renderer.transform.localPosition = position;
            renderer.transform.localScale = scale;
            renderer.color = WithAlpha(accent, Mathf.Lerp(0.3f, 0.62f, zoomT) * edgePulse * Mathf.Lerp(0.78f, 1.08f, breathEnergy));
        }

        private static void UpdateOutputBeam(SpriteRenderer renderer, NodePortMask direction, Color accent, int index, float seed, float zoomT, float breathEnergy)
        {
            var travel = Mathf.Repeat(Time.time * 1.32f + index * 0.2f + seed * 0.05f, 1f);
            var pulse = Mathf.Sin(travel * Mathf.PI);
            var offset = WorkshopDirectionUtility.ToOffset(direction);
            var centerDistance = Mathf.Lerp(0.17f, 0.42f, travel);
            renderer.transform.localPosition = new Vector3(offset.x, offset.y, 0f) * centerDistance;

            var length = Mathf.Lerp(0.22f, 0.36f, zoomT);
            var thickness = Mathf.Lerp(0.052f, 0.085f, zoomT);
            renderer.transform.localScale = direction == NodePortMask.East || direction == NodePortMask.West
                ? new Vector3(length, thickness, 1f)
                : new Vector3(thickness, length, 1f);
            renderer.color = WithAlpha(Color.Lerp(accent, Color.white, 0.22f), Mathf.Lerp(0.22f, 0.58f, zoomT) * pulse * Mathf.Lerp(0.82f, 1.12f, breathEnergy));
        }

        private static Vector3 EvaluatePerimeterPosition(float normalizedPath, float radius)
        {
            var segment = Mathf.Repeat(normalizedPath, 1f) * 4f;
            if (segment < 1f)
            {
                return new Vector3(Mathf.Lerp(-radius, radius, segment), radius, 0f);
            }

            if (segment < 2f)
            {
                return new Vector3(radius, Mathf.Lerp(radius, -radius, segment - 1f), 0f);
            }

            if (segment < 3f)
            {
                return new Vector3(Mathf.Lerp(radius, -radius, segment - 2f), -radius, 0f);
            }

            return new Vector3(-radius, Mathf.Lerp(-radius, radius, segment - 3f), 0f);
        }

        private static void SetActivityRenderersEnabled(NodeVisual visual, bool enabled)
        {
            visual.Aura.enabled = enabled;
            visual.BreathRing.enabled = enabled;
            visual.CoreGlow.enabled = enabled;
            visual.FlowComet.enabled = enabled;

            foreach (SpriteRenderer renderer in visual.EdgeGlows)
            {
                renderer.enabled = enabled;
            }

            foreach (SpriteRenderer renderer in visual.OutputBeams)
            {
                renderer.enabled = false;
            }
        }

        private static Color ResolveActivityColor(WorkshopNodeDefinition definition)
        {
            if (definition == null)
            {
                return new Color(0.43f, 0.87f, 1f);
            }

            Color categoryColor = definition.Category switch
            {
                WorkshopNodeCategory.Source => new Color(1f, 0.48f, 0.28f),
                WorkshopNodeCategory.Processor => new Color(0.78f, 0.52f, 1f),
                WorkshopNodeCategory.Crafter => new Color(0.29f, 0.86f, 1f),
                WorkshopNodeCategory.Storage => new Color(0.9f, 0.76f, 0.42f),
                _ => new Color(0.43f, 0.87f, 1f)
            };

            Color tint = definition.Tint;
            Color color = Color.Lerp(categoryColor, tint, 0.45f);
            var strongestChannel = Mathf.Max(color.r, Mathf.Max(color.g, color.b));
            if (strongestChannel > 0.001f)
            {
                color = new Color(color.r / strongestChannel, color.g / strongestChannel, color.b / strongestChannel, color.a);
            }

            color.a = 1f;
            return color;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = Mathf.Clamp01(alpha);
            return color;
        }

        private static Sprite ResolveNodeSprite(WorkshopNodeDefinition definition)
        {
            if (definition == null)
            {
                return null;
            }

            return definition.NodeSprite != null
                ? definition.NodeSprite
                : ArcaneArtCatalog.GetWorkshopNodeSprite(definition.Id);
        }

        private static Color ResolveNodeSpriteTint(WorkshopNodeDefinition definition)
        {
            return definition == null
                ? Color.white
                : ArcaneArtCatalog.GetWorkshopNodeTint(definition.Id);
        }

        private static float CalculateSpriteFitScale(Sprite sprite, float targetSize)
        {
            if (sprite == null)
            {
                return targetSize;
            }

            var bounds = sprite.bounds.size;
            var largestDimension = Mathf.Max(bounds.x, bounds.y);
            if (largestDimension <= 0.0001f)
            {
                return targetSize;
            }

            return targetSize / largestDimension;
        }

        private static Vector3 CalculateSpriteScale(Sprite sprite, float targetWidth, float targetHeight, bool cover)
        {
            if (sprite == null)
            {
                return new Vector3(targetWidth, targetHeight, 1f);
            }

            Vector2 bounds = sprite.bounds.size;
            float width = Mathf.Max(0.0001f, bounds.x);
            float height = Mathf.Max(0.0001f, bounds.y);
            float scaleFactor = cover
                ? Mathf.Max(targetWidth / width, targetHeight / height)
                : Mathf.Min(targetWidth / width, targetHeight / height);
            return new Vector3(scaleFactor, scaleFactor, 1f);
        }

        private static float CalculateSpriteScaleToFit(Sprite sprite, float targetSize)
        {
            if (sprite == null)
            {
                return targetSize;
            }

            Vector2 bounds = sprite.bounds.size;
            float width = Mathf.Max(0.0001f, bounds.x);
            float height = Mathf.Max(0.0001f, bounds.y);
            return Mathf.Min(targetSize / width, targetSize / height);
        }

        private SpriteRenderer CreateVisualLayer(Transform parent, string objectName, Vector3 localPosition, Vector3 localScale, Color tint, int sortingOrder)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sharedSprite;
            renderer.color = tint;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private void ApplyWorkshopTheme()
        {
            gridTintA = new Color(0.075f, 0.095f, 0.135f, 0.92f);
            gridTintB = new Color(0.095f, 0.125f, 0.17f, 0.92f);
            selectedTint = new Color(0.93f, 0.73f, 0.28f, 0.98f);
            hoverTint = new Color(0.32f, 0.58f, 0.76f, 0.92f);
            boardTint = new Color(0.025f, 0.035f, 0.06f, 1f);
        }

        private void LoadWorkshopArt()
        {
            workshopBackgroundSprite = ArcaneArtCatalog.GetWorkshopBackground();
            boardUnderlaySprite = ArcaneArtCatalog.GetWorkshopBoardUnderlay();
            tileSpriteA = ArcaneArtCatalog.GetWorkshopTileA();
            tileSpriteB = ArcaneArtCatalog.GetWorkshopTileB();
            tileHoverSprite = ArcaneArtCatalog.GetWorkshopTileHover();
            tileSelectedSprite = ArcaneArtCatalog.GetWorkshopTileSelected();
            leylineHorizontalSprite = ArcaneArtCatalog.GetWorkshopLeylineHorizontal();
            leylineVerticalSprite = ArcaneArtCatalog.GetWorkshopLeylineVertical();
        }

        private void CreateScaledSpriteLayer(string objectName, Sprite sprite, Vector3 position, float targetWidth, float targetHeight, int sortingOrder, Color tint, bool cover = false)
        {
            if (sprite == null)
            {
                return;
            }

            var layer = new GameObject(objectName);
            layer.transform.SetParent(gridRoot, false);
            layer.transform.position = position;
            layer.transform.localScale = CalculateSpriteScale(sprite, targetWidth, targetHeight, cover);

            var renderer = layer.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = tint;
            renderer.sortingOrder = sortingOrder;
        }

        private SpriteRenderer CreateCellOverlayRenderer(string objectName, Sprite sprite, int sortingOrder)
        {
            if (sprite == null)
            {
                return null;
            }

            var overlay = new GameObject(objectName);
            overlay.transform.SetParent(gridRoot, false);
            overlay.transform.localScale = Vector3.one * CalculateSpriteScaleToFit(sprite, cellSize * 0.99f);

            var renderer = overlay.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = Color.white;
            renderer.sortingOrder = sortingOrder;
            renderer.enabled = false;
            return renderer;
        }

        private void UpdateSelectionOverlays()
        {
            UpdateCellOverlay(selectedCellOverlayRenderer, controller.SelectedCell, tileSelectedSprite, true);
            UpdateCellOverlay(
                hoverCellOverlayRenderer,
                hoveredCell == controller.SelectedCell ? new Vector2Int(-1, -1) : hoveredCell,
                tileHoverSprite,
                false);
        }

        private void UpdateCellOverlay(SpriteRenderer renderer, Vector2Int cell, Sprite sprite, bool selected)
        {
            if (renderer == null || sprite == null)
            {
                return;
            }

            bool show = controller != null && controller.Simulation != null && controller.Simulation.IsInsideGrid(cell);
            renderer.enabled = show;
            if (!show)
            {
                return;
            }

            float pulse = selected
                ? 0.5f + Mathf.Sin(Time.time * 3.4f) * 0.5f
                : 0.5f + Mathf.Sin(Time.time * 4.2f) * 0.5f;
            float baseScale = CalculateSpriteScaleToFit(sprite, cellSize * (selected ? 1.06f : 1.01f));
            renderer.transform.position = CellToWorld(cell);
            renderer.transform.localScale = Vector3.one * (baseScale * (selected ? Mathf.Lerp(0.98f, 1.035f, pulse) : 1f));
            renderer.color = selected
                ? new Color(1f, 1f, 1f, Mathf.Lerp(0.78f, 0.98f, pulse))
                : new Color(1f, 1f, 1f, Mathf.Lerp(0.56f, 0.72f, pulse));
        }

        private void CreateBoardLayer(string objectName, float extraSize, Color color, int sortingOrder, Vector3 offset)
        {
            if (controller == null || controller.Simulation == null)
            {
                return;
            }

            var layer = new GameObject(objectName);
            layer.transform.SetParent(gridRoot, false);
            layer.transform.position = new Vector3(
                (controller.Simulation.GridSize.x - 1) * cellSize * 0.5f,
                (controller.Simulation.GridSize.y - 1) * cellSize * 0.5f,
                1f) + offset;
            layer.transform.localScale = new Vector3(
                controller.Simulation.GridSize.x * cellSize + extraSize,
                controller.Simulation.GridSize.y * cellSize + extraSize,
                1f);

            var renderer = layer.AddComponent<SpriteRenderer>();
            renderer.sprite = sharedSprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
        }

        private void BuildMajorGridLines()
        {
            if (controller == null || controller.Simulation == null)
            {
                return;
            }

            Vector2Int gridSize = controller.Simulation.GridSize;
            float width = gridSize.x * cellSize;
            float height = gridSize.y * cellSize;
            float centerX = (gridSize.x - 1) * cellSize * 0.5f;
            float centerY = (gridSize.y - 1) * cellSize * 0.5f;

            for (var x = 0; x < gridSize.x; x += MajorGridStep)
            {
                CreateGridLine(
                    $"Leyline_V_{x}",
                    new Vector3(x * cellSize, centerY, 0f),
                    new Vector3(0.035f, height, 1f),
                    new Color(0.64f, 0.8f, 0.9f, 0.16f),
                    leylineVerticalSprite);
            }

            for (var y = 0; y < gridSize.y; y += MajorGridStep)
            {
                CreateGridLine(
                    $"Leyline_H_{y}",
                    new Vector3(centerX, y * cellSize, 0f),
                    new Vector3(width, 0.035f, 1f),
                    new Color(0.88f, 0.67f, 0.28f, 0.13f),
                    leylineHorizontalSprite);
            }
        }

        private void CreateGridLine(string objectName, Vector3 position, Vector3 scale, Color color, Sprite lineSprite)
        {
            var line = new GameObject(objectName);
            line.transform.SetParent(gridRoot, false);
            line.transform.localPosition = position;

            var renderer = line.AddComponent<SpriteRenderer>();
            renderer.sprite = lineSprite != null ? lineSprite : sharedSprite;
            renderer.color = color;
            renderer.sortingOrder = 1;
            renderer.transform.localScale = lineSprite != null
                ? CalculateSpriteScale(lineSprite, scale.x, scale.y, false)
                : scale;
        }

        private Color GetBaseCellTint(Vector2Int cell)
        {
            Color baseColor = ((cell.x + cell.y) & 1) == 0 ? gridTintA : gridTintB;
            if (controller == null || controller.Simulation == null)
            {
                return baseColor;
            }

            Vector2 gridCenter = new Vector2(
                (controller.Simulation.GridSize.x - 1) * 0.5f,
                (controller.Simulation.GridSize.y - 1) * 0.5f);
            float distance = Vector2.Distance(cell, gridCenter);
            float edgeLift = Mathf.InverseLerp(2f, Mathf.Max(controller.Simulation.GridSize.x, controller.Simulation.GridSize.y) * 0.48f, distance);
            Color edgeColor = new Color(0.04f, 0.055f, 0.085f, baseColor.a);
            Color tinted = Color.Lerp(baseColor, edgeColor, edgeLift * 0.42f);

            if (cell.x % MajorGridStep == 0 || cell.y % MajorGridStep == 0)
            {
                tinted = Color.Lerp(tinted, new Color(0.18f, 0.22f, 0.27f, tinted.a), 0.28f);
            }

            return tinted;
        }

        private Vector2 CellToWorld(Vector2Int cell)
        {
            return new Vector2(cell.x * cellSize, cell.y * cellSize);
        }

        private Vector2Int WorldToCell(Vector3 worldPosition)
        {
            return new Vector2Int(
                Mathf.RoundToInt(worldPosition.x / cellSize),
                Mathf.RoundToInt(worldPosition.y / cellSize));
        }

        public void FrameLayout(IEnumerable<WorkshopPlacedNodeSeed> layout, Vector2Int gridSize)
        {
            if (cachedCamera == null || !cachedCamera.orthographic)
            {
                return;
            }

            InitializeCameraState();

            var seeds = layout?
                .Where(seed => seed != null && seed.NodeDefinition != null)
                .ToArray() ?? System.Array.Empty<WorkshopPlacedNodeSeed>();

            Vector2 focusCenter;
            float targetSize;
            if (seeds.Length == 0)
            {
                focusCenter = new Vector2((gridSize.x - 1) * cellSize * 0.5f, (gridSize.y - 1) * cellSize * 0.5f);
                targetSize = Mathf.Clamp(8f, minZoom, maxZoom);
            }
            else
            {
                Vector2Int min = seeds[0].Position;
                Vector2Int max = seeds[0].Position;
                foreach (WorkshopPlacedNodeSeed seed in seeds)
                {
                    min = Vector2Int.Min(min, seed.Position);
                    max = Vector2Int.Max(max, seed.Position);
                }

                Vector2 minWorld = new Vector2(min.x * cellSize, min.y * cellSize) - Vector2.one * (cellSize * 1.8f);
                Vector2 maxWorld = new Vector2(max.x * cellSize, max.y * cellSize) + Vector2.one * (cellSize * 1.8f);
                focusCenter = (minWorld + maxWorld) * 0.5f;

                float contentHeight = Mathf.Max(cellSize * 5f, maxWorld.y - minWorld.y);
                float contentWidth = Mathf.Max(cellSize * 7f, maxWorld.x - minWorld.x);
                float fitHeight = contentHeight * 0.5f;
                float fitWidth = contentWidth * 0.5f / Mathf.Max(0.01f, cachedCamera.aspect);
                targetSize = Mathf.Clamp(Mathf.Max(4.8f, fitHeight, fitWidth), minZoom, maxZoom);
            }

            targetOrthographicSize = targetSize;
            targetCameraPosition = new Vector3(focusCenter.x, focusCenter.y, cachedCamera.transform.position.z);
            ClampCameraTargetToBoard(gridSize);
        }

        private static Sprite CreateSquareSprite()
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1f);
        }

        private void ClampCameraTargetToBoard(Vector2Int gridSize)
        {
            if (cachedCamera == null)
            {
                return;
            }

            var minX = -cameraBoundsPadding;
            var minY = -cameraBoundsPadding;
            var maxX = (gridSize.x - 1) * cellSize + cameraBoundsPadding;
            var maxY = (gridSize.y - 1) * cellSize + cameraBoundsPadding;
            var verticalExtent = targetOrthographicSize;
            var horizontalExtent = verticalExtent * cachedCamera.aspect;
            var position = targetCameraPosition;

            position.x = ClampAxis(position.x, minX, maxX, horizontalExtent);
            position.y = ClampAxis(position.y, minY, maxY, verticalExtent);
            targetCameraPosition = position;
        }
    }
}
