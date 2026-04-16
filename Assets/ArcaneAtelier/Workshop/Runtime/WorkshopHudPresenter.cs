using System.Linq;
using UnityEngine;

namespace ArcaneAtelier.Workshop
{
    public sealed class WorkshopHudPresenter : MonoBehaviour
    {
        private const float Margin = 18f;
        private const float RightSidebarWidth = 320f;
        private const float BottomDockHeight = 176f;
        private const float InventoryPanelHeight = 122f;

        private Vector2 paletteScroll;
        private Vector2 rewardScroll;
        private WorkshopSceneController controller;

        private Texture2D whiteTexture;
        private GUIStyle titleStyle;
        private GUIStyle sectionStyle;
        private GUIStyle bodyStyle;
        private GUIStyle mutedStyle;
        private GUIStyle statValueStyle;
        private GUIStyle statLabelStyle;
        private GUIStyle iconStyle;
        private GUIStyle chipStyle;
        private GUIStyle buttonStyle;
        private GUIStyle smallButtonStyle;
        private GUIStyle cardTitleStyle;
        private GUIStyle cardBodyStyle;
        private GUIStyle tinyLabelStyle;

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

            EnsureTheme();

            DrawBackdrop();

            var topLeftRect = new Rect(Margin, Margin, 318f, 132f);
            var topCenterRect = new Rect(352f, Margin, Mathf.Max(280f, Screen.width - RightSidebarWidth - 390f), 96f);
            var topRightRect = new Rect(Screen.width - RightSidebarWidth - Margin, Margin, RightSidebarWidth, 96f);
            var selectionRect = new Rect(Screen.width - RightSidebarWidth - Margin, 126f, RightSidebarWidth, 230f);
            var rewardRect = new Rect(Screen.width - RightSidebarWidth - Margin, 370f, RightSidebarWidth, Mathf.Max(172f, Screen.height - BottomDockHeight - 388f));
            var inventoryRect = new Rect(Margin, Screen.height - BottomDockHeight - InventoryPanelHeight - 14f, Mathf.Max(380f, Screen.width - RightSidebarWidth - Margin * 3f), InventoryPanelHeight);
            var paletteRect = new Rect(Margin, Screen.height - BottomDockHeight - Margin, Screen.width - Margin * 2f, BottomDockHeight);

            DrawThroughputPanel(topLeftRect);
            DrawStatusPanel(topCenterRect);
            DrawControlPanel(topRightRect);
            DrawSelectionPanel(selectionRect);
            DrawRewardPanel(rewardRect);
            DrawInventoryPanel(inventoryRect);
            DrawPaletteDock(paletteRect);
        }

        private void DrawBackdrop()
        {
            DrawRect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.03f, 0.04f, 0.06f, 0.34f));
            DrawRect(new Rect(0f, Screen.height - BottomDockHeight - Margin * 2f, Screen.width, BottomDockHeight + Margin * 2f), new Color(0.01f, 0.02f, 0.03f, 0.65f));
        }

        private void DrawThroughputPanel(Rect rect)
        {
            var stats = controller.BuildFlowStatsView();
            DrawPanelFrame(rect, new Color(0.78f, 0.61f, 0.31f));

            GUI.BeginGroup(rect);
            GUI.Label(new Rect(18f, 14f, 220f, 24f), "Arcane Atelier", titleStyle);
            GUI.Label(new Rect(18f, 38f, 220f, 20f), "Spell Assembly throughput", mutedStyle);

            DrawMiniStat(new Rect(18f, 70f, 64f, 44f), $"{stats.ElapsedSeconds:0}s", "Runtime");
            DrawMiniStat(new Rect(90f, 70f, 88f, 44f), $"{stats.ElementProductionPerSecond:0.0}/s", "Flow");
            DrawMiniStat(new Rect(186f, 70f, 56f, 44f), $"{stats.SpellProductionPerSecond:0.0}/s", "Spells");
            DrawMiniStat(new Rect(250f, 70f, 50f, 44f), $"{stats.ElementConsumptionPerSecond:0.0}/s", "Use");
            GUI.EndGroup();
        }

        private void DrawStatusPanel(Rect rect)
        {
            DrawPanelFrame(rect, new Color(0.54f, 0.4f, 0.85f));

            GUI.BeginGroup(rect);
            GUI.Label(new Rect(18f, 14f, rect.width - 36f, 24f), "Workshop Command", sectionStyle);
            GUI.Label(new Rect(18f, 40f, rect.width - 36f, 20f), controller.StatusMessage, bodyStyle);

            var selectedLabel = controller.SelectedPaletteNode == null
                ? "No active blueprint selected"
                : $"Blueprint armed: {controller.SelectedPaletteNode.DisplayName}";
            GUI.Label(new Rect(18f, 67f, rect.width - 36f, 18f), selectedLabel, mutedStyle);
            GUI.Label(new Rect(18f, rect.height - 26f, rect.width - 36f, 18f), "LMB place  RMB clear  R rotate node", tinyLabelStyle);
            GUI.EndGroup();
        }

        private void DrawControlPanel(Rect rect)
        {
            DrawPanelFrame(rect, new Color(0.92f, 0.45f, 0.24f));

            GUI.BeginGroup(rect);
            GUI.Label(new Rect(18f, 14f, 140f, 24f), "Workshop Controls", sectionStyle);
            GUI.Label(new Rect(18f, 38f, 220f, 18f), $"Ghost rotation: {controller.PlacementRotationQuarterTurns * 90}°", mutedStyle);

            var pauseLabel = controller.IsPaused ? "▶" : "⏸";
            if (GUI.Button(new Rect(rect.width - 168f, 16f, 44f, 44f), pauseLabel, smallButtonStyle))
            {
                controller.TogglePause();
            }

            if (GUI.Button(new Rect(rect.width - 118f, 16f, 44f, 44f), "↻", smallButtonStyle))
            {
                controller.RotatePlacementClockwise();
            }

            if (GUI.Button(new Rect(rect.width - 68f, 16f, 44f, 44f), "⟲", smallButtonStyle))
            {
                controller.ResetWorkshop();
            }

            GUI.Label(new Rect(rect.width - 174f, 64f, 152f, 18f), "Time  Rotate  Reset", tinyLabelStyle);
            GUI.EndGroup();
        }

        private void DrawSelectionPanel(Rect rect)
        {
            DrawPanelFrame(rect, new Color(0.37f, 0.72f, 0.94f));

            GUI.BeginGroup(rect);
            GUI.Label(new Rect(18f, 14f, rect.width - 36f, 24f), "Selected Apparatus", sectionStyle);
            GUI.Label(new Rect(18f, 38f, rect.width - 36f, 18f), $"Cell {controller.SelectedCell.x}, {controller.SelectedCell.y}", mutedStyle);

            var node = controller.SelectedNode;
            if (node == null)
            {
                GUI.Label(new Rect(18f, 82f, rect.width - 36f, 40f), "Pick a tile to inspect a machine.\nSelected machines can be rotated or dismantled here.", bodyStyle);
            }
            else
            {
                GUI.Label(new Rect(18f, 70f, rect.width - 36f, 24f), node.Definition.DisplayName, titleStyle);
                GUI.Label(new Rect(18f, 96f, rect.width - 36f, 36f), node.Definition.Description, bodyStyle);
                GUI.Label(new Rect(18f, 138f, rect.width - 36f, 18f), $"Rotation {node.RotationQuarterTurns * 90}°   Speed x{node.SpeedMultiplier:0.00}", mutedStyle);
                GUI.Label(new Rect(18f, 160f, rect.width - 36f, 18f), $"In {node.RotatedInputPorts}   Out {node.RotatedOutputPorts}", tinyLabelStyle);

                var y = 188f;
                foreach (var pair in node.EnumerateBuffer().Take(4))
                {
                    DrawChip(new Rect(18f, y, rect.width - 36f, 22f), $"{pair.Key.DisplayName}  x{pair.Value}", pair.Key.Tint);
                    y += 26f;
                }

                if (!node.EnumerateBuffer().Any())
                {
                    GUI.Label(new Rect(18f, 188f, rect.width - 36f, 18f), "Buffer empty", tinyLabelStyle);
                }

                if (GUI.Button(new Rect(18f, rect.height - 46f, 124f, 30f), "Rotate Node", buttonStyle))
                {
                    controller.RotatePlacedNode(controller.SelectedCell);
                }

                if (GUI.Button(new Rect(rect.width - 142f, rect.height - 46f, 124f, 30f), "Dismantle", buttonStyle))
                {
                    controller.TryRemoveNode(controller.SelectedCell);
                }
            }

            GUI.EndGroup();
        }

        private void DrawRewardPanel(Rect rect)
        {
            DrawPanelFrame(rect, new Color(0.57f, 0.45f, 0.89f));

            GUI.BeginGroup(rect);
            GUI.Label(new Rect(18f, 14f, rect.width - 36f, 24f), "Arcane Boons", sectionStyle);
            GUI.Label(new Rect(18f, 38f, rect.width - 36f, 36f), "Debug reward hooks for unlock and progression integration.", bodyStyle);

            var contentRect = new Rect(14f, 82f, rect.width - 28f, rect.height - 96f);
            var rewards = controller.DebugRewards.Where(reward => reward != null).ToArray();
            var viewHeight = rewards.Length * 84f + 8f;
            rewardScroll = GUI.BeginScrollView(contentRect, rewardScroll, new Rect(0f, 0f, contentRect.width - 18f, viewHeight), false, true);

            var y = 0f;
            foreach (var reward in rewards)
            {
                var itemRect = new Rect(0f, y, contentRect.width - 24f, 74f);
                DrawRect(itemRect, new Color(0.09f, 0.11f, 0.15f, 0.94f));
                DrawOutline(itemRect, new Color(0.4f, 0.34f, 0.62f));
                GUI.Label(new Rect(14f, y + 12f, itemRect.width - 110f, 20f), reward.DisplayName, sectionStyle);
                GUI.Label(new Rect(14f, y + 34f, itemRect.width - 110f, 28f), reward.Description, bodyStyle);
                if (GUI.Button(new Rect(itemRect.width - 84f, y + 20f, 70f, 28f), "Apply", buttonStyle))
                {
                    controller.ApplyReward(reward);
                }

                y += 82f;
            }

            GUI.EndScrollView();
            GUI.EndGroup();
        }

        private void DrawInventoryPanel(Rect rect)
        {
            var inventory = controller.BuildInventoryView();

            DrawPanelFrame(rect, new Color(0.22f, 0.72f, 0.6f));

            GUI.BeginGroup(rect);
            GUI.Label(new Rect(18f, 14f, 180f, 24f), "Workshop Stores", sectionStyle);
            GUI.Label(new Rect(18f, 38f, 220f, 18f), "Resources on the line and spells ready for battle.", mutedStyle);

            var leftColumn = new Rect(18f, 64f, rect.width * 0.52f, rect.height - 80f);
            var rightColumn = new Rect(rect.width * 0.56f, 64f, rect.width * 0.26f, rect.height - 80f);

            GUI.Label(new Rect(leftColumn.x, leftColumn.y, leftColumn.width, 18f), "Elements", tinyLabelStyle);
            DrawChipWrap(leftColumn.x, leftColumn.y + 22f, leftColumn.width, inventory.NetworkItems.OrderBy(pair => pair.Key.DisplayName).Select(pair => (pair.Key.DisplayName + $" x{pair.Value}", pair.Key.Tint)).ToArray());

            GUI.Label(new Rect(rightColumn.x, rightColumn.y, rightColumn.width, 18f), "Prepared Spells", tinyLabelStyle);
            DrawChipWrap(rightColumn.x, rightColumn.y + 22f, rightColumn.width, inventory.PreparedCards.OrderBy(pair => pair.Key.DisplayName).Select(pair => (pair.Key.DisplayName + $" x{pair.Value}", pair.Key.Tint)).ToArray());

            var payload = WorkshopBattlePayloadBridge.CurrentPayload;
            GUI.Label(new Rect(rect.width - 152f, 20f, 126f, 16f), payload.HasCards ? "Deck primed" : "Deck empty", tinyLabelStyle);
            if (GUI.Button(new Rect(rect.width - 160f, rect.height - 46f, 142f, 30f), "Forge Battle Deck", buttonStyle))
            {
                controller.CommitBattlePayload();
            }

            GUI.EndGroup();
        }

        private void DrawPaletteDock(Rect rect)
        {
            DrawPanelFrame(rect, new Color(0.88f, 0.74f, 0.33f));

            GUI.BeginGroup(rect);
            GUI.Label(new Rect(18f, 14f, 220f, 24f), "Workshop Palette", titleStyle);
            GUI.Label(new Rect(18f, 38f, 360f, 18f), "Choose a blueprint, then place it on the atelier floor.", mutedStyle);

            var contentRect = new Rect(14f, 62f, rect.width - 28f, rect.height - 72f);
            var nodes = controller.PlaceableNodes.Where(node => node != null).ToArray();
            const float cardWidth = 156f;
            const float cardHeight = 92f;
            const float spacing = 12f;
            var viewWidth = nodes.Length * (cardWidth + spacing) + spacing;

            paletteScroll = GUI.BeginScrollView(contentRect, paletteScroll, new Rect(0f, 0f, viewWidth, contentRect.height - 18f), true, false);
            var x = spacing;
            foreach (var node in nodes)
            {
                var cardRect = new Rect(x, 0f, cardWidth, cardHeight);
                DrawBlueprintCard(cardRect, node);
                x += cardWidth + spacing;
            }

            GUI.EndScrollView();
            GUI.EndGroup();
        }

        private void DrawBlueprintCard(Rect rect, WorkshopNodeDefinition node)
        {
            var unlocked = controller.Simulation.IsUnlocked(node);
            var selected = controller.SelectedPaletteNode == node;
            var accent = GetCategoryColor(node.Category, node.Tint);

            DrawRect(rect, selected ? new Color(accent.r * 0.38f, accent.g * 0.38f, accent.b * 0.38f, 0.95f) : new Color(0.08f, 0.1f, 0.14f, 0.96f));
            DrawOutline(rect, selected ? new Color(0.99f, 0.89f, 0.62f) : accent);
            DrawRect(new Rect(rect.x, rect.y, rect.width, 4f), accent);

            if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
            {
                if (unlocked)
                {
                    controller.SetPaletteNode(node);
                }
            }

            GUI.Label(new Rect(rect.x + 12f, rect.y + 14f, 28f, 28f), GetCategorySymbol(node.Category), iconStyle);
            GUI.Label(new Rect(rect.x + 44f, rect.y + 14f, rect.width - 56f, 22f), node.DisplayName, cardTitleStyle);
            GUI.Label(new Rect(rect.x + 44f, rect.y + 35f, rect.width - 56f, 16f), node.Category.ToString(), tinyLabelStyle);
            GUI.Label(new Rect(rect.x + 12f, rect.y + 56f, rect.width - 24f, 28f), unlocked ? node.Description : "Locked until granted by a reward.", cardBodyStyle);

            if (!unlocked)
            {
                DrawRect(rect, new Color(0f, 0f, 0f, 0.42f));
                GUI.Label(new Rect(rect.x + rect.width - 64f, rect.y + 12f, 52f, 18f), "LOCK", tinyLabelStyle);
            }
            else if (selected)
            {
                DrawChip(new Rect(rect.x + rect.width - 68f, rect.y + 12f, 56f, 20f), $"{controller.PlacementRotationQuarterTurns * 90}°", accent);
            }
        }

        private void DrawMiniStat(Rect rect, string value, string label)
        {
            DrawRect(rect, new Color(0.09f, 0.11f, 0.14f, 0.92f));
            DrawOutline(rect, new Color(0.24f, 0.22f, 0.18f));
            GUI.Label(new Rect(rect.x + 10f, rect.y + 4f, rect.width - 20f, 24f), value, statValueStyle);
            GUI.Label(new Rect(rect.x + 10f, rect.y + 26f, rect.width - 20f, 16f), label, statLabelStyle);
        }

        private void DrawChipWrap(float startX, float startY, float maxWidth, (string Text, Color Tint)[] items)
        {
            if (items.Length == 0)
            {
                GUI.Label(new Rect(startX, startY, maxWidth, 18f), "None", tinyLabelStyle);
                return;
            }

            var x = startX;
            var y = startY;
            foreach (var item in items)
            {
                var chipWidth = Mathf.Min(maxWidth, 22f + tinyLabelStyle.CalcSize(new GUIContent(item.Text)).x);
                if (x + chipWidth > startX + maxWidth)
                {
                    x = startX;
                    y += 26f;
                }

                DrawChip(new Rect(x, y, chipWidth, 20f), item.Text, item.Tint);
                x += chipWidth + 8f;
            }
        }

        private void DrawChip(Rect rect, string text, Color tint)
        {
            DrawRect(rect, new Color(tint.r * 0.28f, tint.g * 0.28f, tint.b * 0.28f, 0.94f));
            DrawOutline(rect, new Color(tint.r * 0.92f, tint.g * 0.92f, tint.b * 0.92f, 1f));
            GUI.Label(new Rect(rect.x + 10f, rect.y + 2f, rect.width - 20f, rect.height - 4f), text, chipStyle);
        }

        private void DrawPanelFrame(Rect rect, Color accent)
        {
            DrawRect(new Rect(rect.x + 6f, rect.y + 8f, rect.width, rect.height), new Color(0f, 0f, 0f, 0.24f));
            DrawRect(rect, new Color(0.05f, 0.06f, 0.08f, 0.94f));
            DrawOutline(rect, new Color(0.2f, 0.19f, 0.18f, 1f));
            DrawRect(new Rect(rect.x, rect.y, rect.width, 4f), accent);
        }

        private void DrawRect(Rect rect, Color color)
        {
            var previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, whiteTexture, ScaleMode.StretchToFill);
            GUI.color = previousColor;
        }

        private void DrawOutline(Rect rect, Color color)
        {
            DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), color);
            DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), color);
            DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), color);
            DrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), color);
        }

        private void EnsureTheme()
        {
            if (whiteTexture == null)
            {
                whiteTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                whiteTexture.SetPixel(0, 0, Color.white);
                whiteTexture.Apply();
            }

            if (titleStyle != null)
            {
                return;
            }

            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.97f, 0.95f, 0.9f) }
            };

            sectionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.96f, 0.87f, 0.66f) }
            };

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true,
                normal = { textColor = new Color(0.83f, 0.84f, 0.88f) }
            };

            mutedStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true,
                normal = { textColor = new Color(0.57f, 0.64f, 0.73f) }
            };

            statValueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = new Color(0.96f, 0.95f, 0.92f) }
            };

            statLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = new Color(0.62f, 0.68f, 0.74f) }
            };

            iconStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.98f, 0.9f, 0.68f) }
            };

            chipStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.96f, 0.95f, 0.92f) }
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            smallButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            cardTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal = { textColor = new Color(0.97f, 0.95f, 0.9f) }
            };

            cardBodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                wordWrap = true,
                normal = { textColor = new Color(0.76f, 0.8f, 0.86f) }
            };

            tinyLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                wordWrap = true,
                normal = { textColor = new Color(0.67f, 0.72f, 0.79f) }
            };
        }

        private static Color GetCategoryColor(WorkshopNodeCategory category, Color fallback)
        {
            return category switch
            {
                WorkshopNodeCategory.Source => new Color(0.91f, 0.43f, 0.24f),
                WorkshopNodeCategory.Processor => new Color(0.66f, 0.46f, 0.95f),
                WorkshopNodeCategory.Crafter => new Color(0.29f, 0.74f, 0.96f),
                WorkshopNodeCategory.Storage => new Color(0.65f, 0.73f, 0.8f),
                _ => fallback
            };
        }

        private static string GetCategorySymbol(WorkshopNodeCategory category)
        {
            return category switch
            {
                WorkshopNodeCategory.Source => "✦",
                WorkshopNodeCategory.Processor => "◇",
                WorkshopNodeCategory.Crafter => "✧",
                WorkshopNodeCategory.Storage => "▣",
                _ => "•"
            };
        }
    }
}
