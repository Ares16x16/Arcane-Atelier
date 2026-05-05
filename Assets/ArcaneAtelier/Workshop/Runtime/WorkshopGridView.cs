using System.Collections.Generic;
using System.Linq;
using ArcaneAtelier;
using UnityEngine;

namespace ArcaneAtelier.Workshop
{
    public sealed class WorkshopGridView : MonoBehaviour
    {
        [SerializeField] private float cellSize = 1.22f;
        [SerializeField] private Color gridTintA = new Color(0.12f, 0.14f, 0.18f);
        [SerializeField] private Color gridTintB = new Color(0.16f, 0.18f, 0.22f);
        [SerializeField] private Color selectedTint = new Color(0.88f, 0.75f, 0.34f);
        [SerializeField] private Color hoverTint = new Color(0.36f, 0.48f, 0.64f);
        [SerializeField] private Color boardTint = new Color(0.06f, 0.07f, 0.1f);
        [SerializeField, Min(1.01f)] private float wheelZoomFactor = 1.12f;
        [SerializeField, Min(1f)] private float minZoom = 2.8f;
        [SerializeField, Min(1f)] private float maxZoom = 18f;
        [SerializeField, Min(0f)] private float cameraBoundsPadding = 4f;
        [SerializeField, Min(0f)] private float dragPixelThreshold = 5f;

        private readonly Dictionary<Vector2Int, SpriteRenderer> cellRenderers = new Dictionary<Vector2Int, SpriteRenderer>();
        private readonly Dictionary<Vector2Int, NodeVisual> nodeVisuals = new Dictionary<Vector2Int, NodeVisual>();

        private Camera cachedCamera;
        private WorkshopSceneController controller;
        private Sprite sharedSprite;
        private Transform gridRoot;
        private Transform nodeRoot;
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
            public SpriteRenderer Frame;
            public SpriteRenderer Body;
            public List<SpriteRenderer> PortMarkers = new List<SpriteRenderer>();
        }

        public float CellSize => cellSize;

        public void Initialize(WorkshopSceneController sceneController)
        {
            controller = sceneController;
            cachedCamera = Camera.main;
            sharedSprite = CreateSquareSprite();
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

            var worldPosition = ScreenToWorldUsingTargetCamera(Input.mousePosition);
            var cell = WorldToCell(worldPosition);
            var isInsideGrid = simulation.IsInsideGrid(cell);
            var nextHoveredCell = isInsideGrid ? cell : new Vector2Int(-1, -1);
            if (nextHoveredCell != hoveredCell)
            {
                hoveredCell = nextHoveredCell;
                controller.SetHoveredCell(hoveredCell);
                RefreshVisuals();
            }

            if (Input.GetMouseButtonDown(1) && isInsideGrid)
            {
                controller.TryRemoveNode(cell);
            }

            if (Input.GetKeyDown(KeyCode.R) && controller.SelectedCell.x >= 0)
            {
                controller.RotatePlacedNode(controller.SelectedCell);
            }
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
                        controller.TryPlaceSelectedNode(releasedCell);
                    }
                }
            }
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
            const float margin = 10f;

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

            if (controller.SelectedCell.x >= 0 && cellRenderers.TryGetValue(controller.SelectedCell, out var selectedRenderer))
            {
                selectedRenderer.color = selectedTint;
            }

            if (hoveredCell.x >= 0 && cellRenderers.TryGetValue(hoveredCell, out var hoverRenderer))
            {
                hoverRenderer.color = controller.SelectedCell == hoveredCell ? selectedTint : hoverTint;
            }

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
            nodeVisuals.Clear();

            gridRoot = new GameObject("Workshop Board").transform;
            gridRoot.SetParent(transform, false);

            nodeRoot = new GameObject("Node Visuals").transform;
            nodeRoot.SetParent(transform, false);

            var board = new GameObject("Board Backdrop");
            board.transform.SetParent(gridRoot, false);
            board.transform.position = new Vector3(
                (controller.Simulation.GridSize.x - 1) * cellSize * 0.5f,
                (controller.Simulation.GridSize.y - 1) * cellSize * 0.5f,
                1f);
            board.transform.localScale = new Vector3(
                controller.Simulation.GridSize.x * cellSize + 1.4f,
                controller.Simulation.GridSize.y * cellSize + 1.4f,
                1f);
            var boardRenderer = board.AddComponent<SpriteRenderer>();
            boardRenderer.sprite = sharedSprite;
            boardRenderer.color = boardTint;
            boardRenderer.sortingOrder = -2;

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
                }
            }
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
                Shadow = CreateVisualLayer(visualGroup.transform, "Shadow", new Vector3(0.05f, -0.06f, 0f), Vector3.one * 0.92f, new Color(0f, 0f, 0f, 0.35f), 1),
                Frame = CreateVisualLayer(visualGroup.transform, "Frame", Vector3.zero, Vector3.one * 0.88f, new Color(0.24f, 0.21f, 0.18f), 2),
                Body = CreateVisualLayer(visualGroup.transform, "Body", Vector3.zero, Vector3.one * 0.76f, Color.white, 3)
            };

            foreach (var direction in WorkshopDirectionUtility.CardinalDirections)
            {
                var portGo = new GameObject(direction.ToString());
                portGo.transform.SetParent(root.transform, false);
                var offset = WorkshopDirectionUtility.ToOffset(direction);
                portGo.transform.localPosition = new Vector3(offset.x, offset.y, 0f) * 0.36f;
                portGo.transform.localScale = Vector3.one * 0.18f;

                var portRenderer = portGo.AddComponent<SpriteRenderer>();
                portRenderer.sprite = sharedSprite;
                portRenderer.sortingOrder = 5;
                visual.PortMarkers.Add(portRenderer);
            }

            return visual;
        }

        private void UpdateNodeVisual(Vector2Int cell, NodeVisual visual, WorkshopNodeState state)
        {
            visual.Root.transform.position = CellToWorld(state.Position);
            visual.VisualRoot.localRotation = Quaternion.Euler(0, 0, -90f * state.RotationQuarterTurns);
            var pulseScale = 0.94f + Mathf.PingPong(Time.time * 0.55f, 0.05f);
            var nodeSprite = ResolveNodeSprite(state.Definition);
            var nodeTint = ResolveNodeSpriteTint(state.Definition);
            if (nodeSprite != null)
            {
                var spriteScale = CalculateSpriteFitScale(nodeSprite, 0.76f * pulseScale);
                var spriteDirection = WorkshopNodeVariantUtility.ShouldMirrorSprite(state.Definition.Id) ? -1f : 1f;
                visual.Body.sprite = nodeSprite;
                visual.Body.color = nodeTint;
                visual.Body.transform.localScale = new Vector3(spriteScale * spriteDirection, spriteScale, 1f);
            }
            else
            {
                var fallbackScale = 0.76f * pulseScale;
                var spriteDirection = WorkshopNodeVariantUtility.ShouldMirrorSprite(state.Definition.Id) ? -1f : 1f;
                visual.Body.sprite = sharedSprite;
                visual.Body.color = state.Definition.Tint;
                visual.Body.transform.localScale = new Vector3(fallbackScale * spriteDirection, fallbackScale, 1f);
            }
            visual.Frame.color = controller.SelectedCell == cell
                ? new Color(0.99f, 0.89f, 0.58f)
                : new Color(0.24f, 0.21f, 0.18f);
            visual.Shadow.color = controller.SelectedCell == cell
                ? new Color(0.92f, 0.74f, 0.33f, 0.22f)
                : new Color(0f, 0f, 0f, 0.35f);

            for (var index = 0; index < WorkshopDirectionUtility.CardinalDirections.Count; index++)
            {
                var direction = WorkshopDirectionUtility.CardinalDirections[index];
                var renderer = visual.PortMarkers[index];

                var isInput = (state.RotatedInputPorts & direction) != 0;
                var isOutput = (state.RotatedOutputPorts & direction) != 0;
                renderer.enabled = isInput || isOutput;
                renderer.color = isOutput ? new Color(0.91f, 0.62f, 0.24f) : new Color(0.36f, 0.78f, 0.95f);
            }
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

        private Color GetBaseCellTint(Vector2Int cell)
        {
            return ((cell.x + cell.y) & 1) == 0 ? gridTintA : gridTintB;
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

        private static Sprite CreateSquareSprite()
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1f);
        }
    }
}
