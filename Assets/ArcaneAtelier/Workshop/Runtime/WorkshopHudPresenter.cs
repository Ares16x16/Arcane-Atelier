using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ArcaneAtelier.Workshop
{
    public sealed class WorkshopHudPresenter : MonoBehaviour
    {
        private readonly Dictionary<WorkshopNodeDefinition, GUIContent> paletteCache = new();
        private Vector2 paletteScroll;
        private Vector2 inventoryScroll;
        private Vector2 rewardScroll;
        private WorkshopSceneController controller;

        public void Initialize(WorkshopSceneController sceneController)
        {
            controller = sceneController;
        }

        public void Repaint()
        {
        }

        private void OnGUI()
        {
            if (controller == null)
            {
                return;
            }

            DrawPalettePanel();
            DrawInspectorPanel();
            DrawInventoryPanel();
            DrawRewardPanel();
            DrawStatusBar();
        }

        private void DrawPalettePanel()
        {
            GUILayout.BeginArea(new Rect(12, 12, 300, Screen.height - 120), GUI.skin.window);
            GUILayout.Label("Workshop Palette", HeaderStyle());
            GUILayout.Label("Left click places or replaces. Right click removes. R rotates the selected node. Use Rotate Ghost to orient placement before dropping.");

            paletteScroll = GUILayout.BeginScrollView(paletteScroll);
            foreach (var node in controller.PlaceableNodes.Where(node => node != null))
            {
                var unlocked = controller.Simulation.IsUnlocked(node);
                using (new GUIEnabledScope(unlocked))
                {
                    if (!paletteCache.TryGetValue(node, out var content))
                    {
                        content = new GUIContent($"{node.DisplayName} [{node.Category}]");
                        paletteCache.Add(node, content);
                    }

                    if (GUILayout.Button(content, GUILayout.Height(38)))
                    {
                        controller.SetPaletteNode(node);
                    }
                }

                GUILayout.Label(unlocked ? node.Description : "Locked. Unlock through the reward pipeline or debug reward panel.", WrappedLabel());
                GUILayout.Space(8);
            }

            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Rotate Ghost"))
            {
                controller.RotatePlacementClockwise();
            }

            if (GUILayout.Button("Reset Layout"))
            {
                controller.ResetWorkshop();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndArea();
        }

        private void DrawInspectorPanel()
        {
            GUILayout.BeginArea(new Rect(Screen.width - 340, 12, 328, 300), GUI.skin.window);
            GUILayout.Label("Selection", HeaderStyle());
            GUILayout.Label($"Cell: {controller.SelectedCell.x}, {controller.SelectedCell.y}");

            var node = controller.SelectedNode;
            if (node == null)
            {
                GUILayout.Label("No node selected.");
            }
            else
            {
                GUILayout.Label(node.Definition.DisplayName, HeaderStyle(14));
                GUILayout.Label(node.Definition.Description, WrappedLabel());
                GUILayout.Label($"Rotation: {node.RotationQuarterTurns * 90}°");
                GUILayout.Label($"Inputs: {node.RotatedInputPorts}");
                GUILayout.Label($"Outputs: {node.RotatedOutputPorts}");
                GUILayout.Label($"Speed: x{node.SpeedMultiplier:0.00}");
                GUILayout.Label("Buffer");

                foreach (var pair in node.EnumerateBuffer())
                {
                    GUILayout.Label($"{pair.Key.DisplayName}: {pair.Value}");
                }

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Rotate Node"))
                {
                    controller.RotatePlacedNode(controller.SelectedCell);
                }

                if (GUILayout.Button("Remove Node"))
                {
                    controller.TryRemoveNode(controller.SelectedCell);
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.EndArea();
        }

        private void DrawInventoryPanel()
        {
            var inventory = controller.BuildInventoryView();
            var width = Mathf.Max(340f, Screen.width - 676f);

            GUILayout.BeginArea(new Rect(324, Screen.height - 250, width, 238), GUI.skin.window);
            GUILayout.Label("Network Inventory + Battle Output", HeaderStyle());

            inventoryScroll = GUILayout.BeginScrollView(inventoryScroll);
            GUILayout.Label("Network Inventory", HeaderStyle(13));
            foreach (var pair in inventory.NetworkItems.OrderBy(pair => pair.Key.DisplayName))
            {
                GUILayout.Label($"{pair.Key.DisplayName}: {pair.Value}");
            }

            GUILayout.Space(8);
            GUILayout.Label("Prepared Cards", HeaderStyle(13));
            foreach (var pair in inventory.PreparedCards.OrderBy(pair => pair.Key.DisplayName))
            {
                GUILayout.Label($"{pair.Key.DisplayName}: {pair.Value}");
            }
            GUILayout.EndScrollView();

            if (GUILayout.Button("Commit Battle Payload", GUILayout.Height(32)))
            {
                controller.CommitBattlePayload();
            }

            GUILayout.EndArea();
        }

        private void DrawRewardPanel()
        {
            GUILayout.BeginArea(new Rect(Screen.width - 340, 320, 328, Screen.height - 394), GUI.skin.window);
            GUILayout.Label("Reward Hooks / Integration QA", HeaderStyle());
            GUILayout.Label("Use these debug hooks to validate unlocks and efficiency boosts before the shared reward scene is assembled.", WrappedLabel());

            rewardScroll = GUILayout.BeginScrollView(rewardScroll);
            foreach (var reward in controller.DebugRewards.Where(reward => reward != null))
            {
                if (GUILayout.Button(reward.DisplayName, GUILayout.Height(34)))
                {
                    controller.ApplyReward(reward);
                }

                GUILayout.Label(reward.Description, WrappedLabel());
                GUILayout.Space(8);
            }
            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        private void DrawStatusBar()
        {
            GUILayout.BeginArea(new Rect(12, Screen.height - 96, 300, 84), GUI.skin.window);
            GUILayout.Label("Workshop Flow", HeaderStyle());
            GUILayout.Label(controller.StatusMessage, WrappedLabel());

            var payload = WorkshopBattlePayloadBridge.CurrentPayload;
            GUILayout.Label(payload.HasCards
                ? $"Bridge payload ready: {payload.Cards.Count} card type(s)."
                : "Bridge payload empty.");
            GUILayout.EndArea();
        }

        private static GUIStyle HeaderStyle(int size = 16)
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = size,
                fontStyle = FontStyle.Bold
            };
        }

        private static GUIStyle WrappedLabel()
        {
            return new GUIStyle(GUI.skin.label)
            {
                wordWrap = true
            };
        }

        private readonly struct GUIEnabledScope : System.IDisposable
        {
            private readonly bool previousState;

            public GUIEnabledScope(bool enabled)
            {
                previousState = GUI.enabled;
                GUI.enabled = enabled;
            }

            public void Dispose()
            {
                GUI.enabled = previousState;
            }
        }
    }
}
