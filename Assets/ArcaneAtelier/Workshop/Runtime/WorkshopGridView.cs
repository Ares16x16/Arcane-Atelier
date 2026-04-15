using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ArcaneAtelier.Workshop
{
    public sealed class WorkshopGridView : MonoBehaviour
    {
        [SerializeField] private float cellSize = 1.2f;
        [SerializeField] private Color gridTint = new(0.17f, 0.18f, 0.22f);
        [SerializeField] private Color selectedTint = new(0.85f, 0.79f, 0.34f);
        [SerializeField] private Color hoverTint = new(0.35f, 0.45f, 0.6f);

        private readonly Dictionary<Vector2Int, SpriteRenderer> cellRenderers = new();
        private readonly Dictionary<Vector2Int, NodeVisual> nodeVisuals = new();

        private Camera cachedCamera;
        private WorkshopSceneController controller;
        private Sprite sharedSprite;
        private Vector2Int hoveredCell = new(-1, -1);

        private sealed class NodeVisual
        {
            public GameObject Root;
            public SpriteRenderer Body;
            public List<SpriteRenderer> PortMarkers = new();
        }

        public void Initialize(WorkshopSceneController sceneController)
        {
            controller = sceneController;
            cachedCamera = Camera.main;
            sharedSprite = CreateSquareSprite();

            BuildGrid();
            RefreshVisuals();
        }

        private void Update()
        {
            if (controller == null || cachedCamera == null)
            {
                return;
            }

            var worldPosition = cachedCamera.ScreenToWorldPoint(Input.mousePosition);
            var cell = WorldToCell(worldPosition);
            var nextHoveredCell = controller.Simulation.IsInsideGrid(cell) ? cell : new Vector2Int(-1, -1);
            if (nextHoveredCell != hoveredCell)
            {
                hoveredCell = nextHoveredCell;
                RefreshVisuals();
            }

            if (Input.GetMouseButtonDown(0) && controller.Simulation.IsInsideGrid(cell))
            {
                controller.TryPlaceSelectedNode(cell);
            }

            if (Input.GetMouseButtonDown(1) && controller.Simulation.IsInsideGrid(cell))
            {
                controller.TryRemoveNode(cell);
            }

            if (Input.GetKeyDown(KeyCode.R) && controller.SelectedCell.x >= 0)
            {
                controller.RotatePlacedNode(controller.SelectedCell);
            }
        }

        private void OnGUI()
        {
            if (controller == null || cachedCamera == null)
            {
                return;
            }

            foreach (var pair in controller.Simulation.Nodes)
            {
                var screenRect = CellToScreenRect(pair.Key);
                var label = pair.Value.Definition.DisplayName + "\n" +
                            string.Join(" ", pair.Value.EnumerateBuffer().Select(item => $"{item.Key.DisplayName}:{item.Value}"));

                var style = new GUIStyle(GUI.skin.box)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 11,
                    wordWrap = true
                };
                GUI.Box(screenRect, label, style);
            }
        }

        public void RefreshVisuals()
        {
            if (controller == null)
            {
                return;
            }

            foreach (var pair in cellRenderers)
            {
                pair.Value.color = gridTint;
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

                UpdateNodeVisual(visual, pair.Value);
            }
        }

        private void BuildGrid()
        {
            foreach (var renderer in cellRenderers.Values)
            {
                if (renderer != null)
                {
                    Destroy(renderer.gameObject);
                }
            }

            cellRenderers.Clear();

            var gridRoot = new GameObject("Grid Cells");
            gridRoot.transform.SetParent(transform, false);

            for (var y = 0; y < controller.Simulation.GridSize.y; y++)
            {
                for (var x = 0; x < controller.Simulation.GridSize.x; x++)
                {
                    var cell = new Vector2Int(x, y);
                    var go = new GameObject($"Cell_{x}_{y}");
                    go.transform.SetParent(gridRoot.transform, false);
                    go.transform.position = CellToWorld(cell);
                    go.transform.localScale = Vector3.one * cellSize;

                    var spriteRenderer = go.AddComponent<SpriteRenderer>();
                    spriteRenderer.sprite = sharedSprite;
                    spriteRenderer.color = gridTint;
                    spriteRenderer.sortingOrder = 0;
                    cellRenderers.Add(cell, spriteRenderer);
                }
            }
        }

        private NodeVisual CreateNodeVisual(Vector2Int cell)
        {
            var root = new GameObject($"Node_{cell.x}_{cell.y}");
            root.transform.SetParent(transform, false);
            root.transform.position = CellToWorld(cell);
            root.transform.localScale = Vector3.one * cellSize * 0.8f;

            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(root.transform, false);
            var bodyRenderer = bodyGo.AddComponent<SpriteRenderer>();
            bodyRenderer.sprite = sharedSprite;
            bodyRenderer.sortingOrder = 2;

            var visual = new NodeVisual
            {
                Root = root,
                Body = bodyRenderer
            };

            foreach (var direction in WorkshopDirectionUtility.CardinalDirections)
            {
                var portGo = new GameObject(direction.ToString());
                portGo.transform.SetParent(root.transform, false);
                portGo.transform.localPosition = (Vector3)WorkshopDirectionUtility.ToOffset(direction) * 0.38f;
                portGo.transform.localScale = Vector3.one * 0.18f;

                var portRenderer = portGo.AddComponent<SpriteRenderer>();
                portRenderer.sprite = sharedSprite;
                portRenderer.sortingOrder = 3;
                visual.PortMarkers.Add(portRenderer);
            }

            return visual;
        }

        private void UpdateNodeVisual(NodeVisual visual, WorkshopNodeState state)
        {
            visual.Root.transform.position = CellToWorld(state.Position);
            visual.Body.color = state.Definition.Tint;

            for (var index = 0; index < WorkshopDirectionUtility.CardinalDirections.Count; index++)
            {
                var direction = WorkshopDirectionUtility.CardinalDirections[index];
                var renderer = visual.PortMarkers[index];

                var isInput = (state.RotatedInputPorts & direction) != 0;
                var isOutput = (state.RotatedOutputPorts & direction) != 0;
                renderer.enabled = isInput || isOutput;
                renderer.color = isOutput ? new Color(0.89f, 0.62f, 0.21f) : new Color(0.33f, 0.72f, 0.89f);
            }
        }

        private Vector2 CellToWorld(Vector2Int cell)
        {
            return new Vector2(cell.x * cellSize, cell.y * cellSize);
        }

        private Rect CellToScreenRect(Vector2Int cell)
        {
            var world = CellToWorld(cell);
            var screen = cachedCamera.WorldToScreenPoint(world);
            const float size = 80f;
            return new Rect(screen.x - size * 0.5f, Screen.height - screen.y - size * 0.5f, size, size);
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
