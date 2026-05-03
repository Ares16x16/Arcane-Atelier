using System.Linq;
using ArcaneAtelier;
using UnityEngine;

namespace ArcaneAtelier.Workshop
{
    public sealed class WorkshopHudPresenter : MonoBehaviour
    {
        private const float Margin = 20f;
        private const float RightRailWidth = 380f;
        private const float BottomDockHeight = 292f;
        private const float TopHudHeight = 94f;

        private Vector2 paletteScroll;
        private Vector2 rewardScroll;
        private bool showGuide;
        private bool showRewards;
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

        private void Update()
        {
            if (controller == null)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                showGuide = false;
                showRewards = false;
            }

            if (Input.GetKeyDown(KeyCode.F1))
            {
                showGuide = !showGuide;
            }

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                showRewards = !showRewards;
            }
        }

        private void OnGUI()
        {
            if (controller == null)
            {
                return;
            }

            EnsureTheme();
            ApplyCameraLayout();

            DrawBackdrop();

            const float throughputWidth = 278f;
            const float controlWidth = 196f;
            var topLeftRect = new Rect(Margin, Margin, throughputWidth, 110f);
            var topCenterRect = new Rect(
                topLeftRect.xMax + 16f,
                Margin,
                Mathf.Max(240f, Screen.width - topLeftRect.width - RightRailWidth - controlWidth - Margin * 4f - 16f),
                60f);
            var topRightRect = new Rect(Screen.width - controlWidth - Margin, Margin, controlWidth, 60f);
            var rightRailRect = new Rect(
                Screen.width - RightRailWidth - Margin,
                TopHudHeight + 10f,
                RightRailWidth,
                Screen.height - BottomDockHeight - TopHudHeight - Margin * 2f);
            var paletteRect = new Rect(Margin, Screen.height - BottomDockHeight - Margin, Screen.width - Margin * 2f, BottomDockHeight);

            DrawThroughputPanel(topLeftRect);
            DrawStatusPanel(topCenterRect);
            DrawControlPanel(topRightRect);
            DrawRightRail(rightRailRect);
            DrawPaletteDock(paletteRect);
            DrawHoverTooltip();

            if (showRewards)
            {
                DrawRewardDrawer(new Rect(Screen.width - RightRailWidth - 336f, 104f, 320f, Mathf.Min(360f, Screen.height - BottomDockHeight - 138f)));
            }

            if (showGuide)
            {
                DrawGuideOverlay(new Rect(Screen.width * 0.5f - 410f, Screen.height * 0.5f - 250f, 820f, 500f));
            }
        }

        private void DrawBackdrop()
        {
            DrawRect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.08f));
            DrawRect(new Rect(0f, 0f, Screen.width, TopHudHeight + Margin), new Color(0.02f, 0.03f, 0.05f, 0.42f));
            DrawRect(new Rect(0f, Screen.height - BottomDockHeight - Margin * 2f, Screen.width, BottomDockHeight + Margin * 2f), new Color(0.01f, 0.02f, 0.03f, 0.74f));
        }

        private void DrawThroughputPanel(Rect rect)
        {
            var stats = controller != null && controller.Simulation != null
                ? controller.BuildFlowStatsView()
                : new WorkshopFlowStatsView(0f, 0f, 0f, 0f);
            DrawPanelFrame(rect, new Color(0.78f, 0.61f, 0.31f));

            GUI.BeginGroup(rect);
            GUI.Label(new Rect(18f, 14f, 220f, 24f), "Arcane Atelier", titleStyle);
            GUI.Label(new Rect(18f, 38f, 220f, 20f), "Workshop", mutedStyle);

            DrawMiniStat(new Rect(18f, 62f, 54f, 30f), $"{controller.RemainingPreparationTicks}", "Ticks");
            DrawMiniStat(new Rect(78f, 62f, 64f, 30f), $"{stats.ElementProductionPerSecond:0.0}", "Flow");
            DrawMiniStat(new Rect(148f, 62f, 54f, 30f), $"{stats.SpellProductionPerSecond:0.0}", "Spell");
            DrawMiniStat(new Rect(208f, 62f, 54f, 30f), $"{stats.ElementConsumptionPerSecond:0.0}", "Use");
            GUI.EndGroup();
        }

        private void DrawStatusPanel(Rect rect)
        {
            DrawPanelFrame(rect, new Color(0.54f, 0.4f, 0.85f));

            GUI.BeginGroup(rect);
            GUI.Label(new Rect(18f, 12f, rect.width - 36f, 18f), "Status", sectionStyle);
            GUI.Label(new Rect(18f, 30f, rect.width - 36f, 18f), controller.StatusMessage, bodyStyle);
            GUI.EndGroup();
        }

        private void DrawControlPanel(Rect rect)
        {
            DrawPanelFrame(rect, new Color(0.92f, 0.45f, 0.24f));

            GUI.BeginGroup(rect);
            const float buttonSize = 32f;
            const float buttonGap = 6f;
            var buttonStartX = rect.width - 12f - (buttonSize * 4f + buttonGap * 3f);
            GUI.Label(new Rect(12f, 14f, 22f, 16f), $"{controller.PlacementRotationQuarterTurns * 90}°", tinyLabelStyle);

            if (GUI.Button(new Rect(buttonStartX, 10f, buttonSize, buttonSize), "?", smallButtonStyle))
            {
                showGuide = !showGuide;
            }

            if (GUI.Button(new Rect(buttonStartX + buttonSize + buttonGap, 10f, buttonSize, buttonSize), "✦", smallButtonStyle))
            {
                showRewards = !showRewards;
            }

            var pauseLabel = controller.IsPaused ? "▶" : "⏸";
            if (GUI.Button(new Rect(buttonStartX + (buttonSize + buttonGap) * 2f, 10f, buttonSize, buttonSize), pauseLabel, smallButtonStyle))
            {
                controller.TogglePause();
            }

            if (GUI.Button(new Rect(buttonStartX + (buttonSize + buttonGap) * 3f, 10f, buttonSize, buttonSize), "↺", smallButtonStyle))
            {
                controller.ResetWorkshop();
            }
            GUI.EndGroup();
        }

        private void DrawRightRail(Rect rect)
        {
            DrawPanelFrame(rect, new Color(0.37f, 0.72f, 0.94f));

            GUI.BeginGroup(rect);
            var contentWidth = rect.width - 36f;
            GUI.Label(new Rect(18f, 14f, rect.width - 36f, 20f), "Selected", sectionStyle);
            GUI.Label(new Rect(18f, 34f, rect.width - 36f, 18f), $"{controller.EncounterLabel}  {controller.RemainingPreparationTicks}/{controller.TotalPreparationTicks} ticks", tinyLabelStyle);
            GUI.Label(new Rect(18f, 50f, rect.width - 36f, 18f), $"Cell {controller.SelectedCell.x}, {controller.SelectedCell.y}", tinyLabelStyle);

            var node = controller.SelectedNode;
            const float detailBottom = 166f;
            if (node == null)
            {
                DrawRect(new Rect(18f, 76f, contentWidth, 70f), new Color(0.08f, 0.1f, 0.14f, 0.78f));
                DrawOutline(new Rect(18f, 76f, contentWidth, 70f), new Color(0.18f, 0.25f, 0.32f));
                GUI.Label(new Rect(32f, 92f, contentWidth - 28f, 34f), "Choose a tile to inspect a machine. Place with LMB on empty cells.", bodyStyle);
            }
            else
            {
                GUI.Label(new Rect(18f, 70f, contentWidth, 22f), node.Definition.DisplayName, sectionStyle);
                GUI.Label(new Rect(18f, 92f, contentWidth, 18f), node.Definition.Category.ToString(), tinyLabelStyle);
                GUI.Label(new Rect(18f, 112f, contentWidth, 18f), $"Rot {node.RotationQuarterTurns * 90}°   Spd x{node.SpeedMultiplier:0.00}", mutedStyle);

                var bufferRows = node.EnumerateBuffer()
                    .Take(rect.height < 420f ? 2 : 3)
                    .Select(pair => (ShortItemName(pair.Key.DisplayName), pair.Value, pair.Key.Tint, GetItemIcon(pair.Key)))
                    .ToArray();

                var y = 140f;
                if (bufferRows.Length > 0)
                {
                    DrawCompactList(new Rect(18f, y, contentWidth, bufferRows.Length * 18f), bufferRows);
                    y += bufferRows.Length * 18f + 8f;
                }
                else
                {
                    GUI.Label(new Rect(18f, y, contentWidth, 18f), "Buffer empty", tinyLabelStyle);
                    y += 24f;
                }

                GUI.Label(new Rect(18f, y + 2f, contentWidth, 18f), "R rotate   RMB remove", tinyLabelStyle);
            }

            var inventory = controller.BuildInventoryView();
            var deployButtonY = rect.height - 42f;
            var stepButtonY = deployButtonY - 34f;
            var payloadBottom = stepButtonY - 14f;
            var payloadTop = detailBottom + 18f;
            var payloadHeight = Mathf.Max(150f, payloadBottom - payloadTop);
            var columnGap = 12f;
            var columnWidth = (contentWidth - columnGap) * 0.5f;
            var inventoryItems = inventory.NetworkItems
                .OrderBy(pair => pair.Key.DisplayName)
                .Select(pair => (ShortItemName(pair.Key.DisplayName), pair.Value, pair.Key.Tint, GetItemIcon(pair.Key)))
                .ToArray();
            var deckItems = inventory.PreparedCards
                .OrderBy(pair => pair.Key.DisplayName)
                .Select(pair => (ShortItemName(pair.Key.DisplayName), pair.Value, pair.Key.Tint, GetItemIcon(pair.Key)))
                .ToArray();

            DrawRect(new Rect(18f, detailBottom, contentWidth, 1f), new Color(0.22f, 0.24f, 0.28f));

            DrawRect(new Rect(18f, payloadTop, columnWidth, payloadHeight), new Color(0.07f, 0.09f, 0.13f, 0.92f));
            DrawOutline(new Rect(18f, payloadTop, columnWidth, payloadHeight), new Color(0.18f, 0.24f, 0.3f));
            GUI.Label(new Rect(30f, payloadTop + 12f, columnWidth - 24f, 20f), "Inventory", sectionStyle);
            GUI.Label(new Rect(30f, payloadTop + 32f, columnWidth - 24f, 18f), "Network + reserve", tinyLabelStyle);
            DrawCompactList(new Rect(30f, payloadTop + 58f, columnWidth - 24f, payloadHeight - 70f), inventoryItems);

            var deckX = 18f + columnWidth + columnGap;
            DrawRect(new Rect(deckX, payloadTop, columnWidth, payloadHeight), new Color(0.07f, 0.09f, 0.13f, 0.92f));
            DrawOutline(new Rect(deckX, payloadTop, columnWidth, payloadHeight), new Color(0.27f, 0.33f, 0.42f));
            GUI.Label(new Rect(deckX + 12f, payloadTop + 12f, columnWidth - 24f, 20f), "Battle Deck", sectionStyle);
            GUI.Label(new Rect(deckX + 12f, payloadTop + 32f, columnWidth - 24f, 18f), "Cards sent to combat", tinyLabelStyle);
            DrawCompactList(new Rect(deckX + 12f, payloadTop + 58f, columnWidth - 24f, payloadHeight - 70f), deckItems);

            if (GUI.Button(new Rect(18f, stepButtonY, contentWidth, 28f), "Advance 1 Prep Tick", buttonStyle))
            {
                controller.StepPreparationOnce();
            }

            if (GUI.Button(new Rect(18f, deployButtonY, contentWidth, 28f), "Forge And Deploy", buttonStyle))
            {
                controller.DeployToBattle();
            }

            GUI.EndGroup();
        }

        private void DrawRewardDrawer(Rect rect)
        {
            DrawPanelFrame(rect, new Color(0.57f, 0.45f, 0.89f));
            GUI.BeginGroup(rect);
            GUI.Label(new Rect(18f, 14f, rect.width - 36f, 20f), "Arcane Boons", sectionStyle);
            GUI.Label(new Rect(18f, 34f, rect.width - 36f, 18f), "TAB or ✦ to close", tinyLabelStyle);

            var contentRect = new Rect(14f, 60f, rect.width - 28f, rect.height - 74f);
            var rewards = controller.DebugRewards.Where(reward => reward != null).ToArray();
            var viewHeight = rewards.Length * 86f + 8f;
            rewardScroll = GUI.BeginScrollView(contentRect, rewardScroll, new Rect(0f, 0f, contentRect.width - 18f, viewHeight), false, true);

            var y = 0f;
            foreach (var reward in rewards)
            {
                var itemRect = new Rect(0f, y, contentRect.width - 24f, 76f);
                DrawRect(itemRect, new Color(0.09f, 0.11f, 0.15f, 0.95f));
                DrawOutline(itemRect, new Color(0.4f, 0.34f, 0.62f));
                DrawRewardIcon(new Rect(12f, y + 12f, 42f, 42f), reward);
                GUI.Label(new Rect(62f, y + 10f, itemRect.width - 136f, 18f), reward.DisplayName, sectionStyle);
                GUI.Label(new Rect(62f, y + 30f, itemRect.width - 136f, 30f), reward.Description, bodyStyle);
                if (GUI.Button(new Rect(itemRect.width - 68f, y + 24f, 56f, 26f), "Use", buttonStyle))
                {
                    controller.ApplyReward(reward);
                }

                y += 84f;
            }

            GUI.EndScrollView();
            GUI.EndGroup();
        }

        private void DrawGuideOverlay(Rect rect)
        {
            DrawRect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.5f));
            DrawPanelFrame(rect, new Color(0.88f, 0.74f, 0.33f));
            GUI.BeginGroup(rect);
            GUI.Label(new Rect(24f, 18f, rect.width - 132f, 24f), "Workshop Guide", titleStyle);
            GUI.Label(new Rect(24f, 48f, rect.width - 132f, 20f), "Place machines, route outputs into matching inputs, then deploy the crafted deck.", mutedStyle);
            if (GUI.Button(new Rect(rect.width - 64f, 18f, 38f, 28f), "X", buttonStyle))
            {
                showGuide = false;
            }

            const float leftX = 24f;
            const float columnWidth = 354f;
            const float rightX = 426f;

            DrawRect(new Rect(leftX, 86f, columnWidth, 154f), new Color(0.07f, 0.09f, 0.13f, 0.9f));
            DrawOutline(new Rect(leftX, 86f, columnWidth, 154f), new Color(0.28f, 0.24f, 0.18f));
            GUI.Label(new Rect(leftX + 14f, 100f, columnWidth - 28f, 20f), "Starter Layout", sectionStyle);
            GUI.Label(new Rect(leftX + 14f, 126f, columnWidth - 28f, 34f), "Spell line: two Fire Shapers feed Spell Fusion I -> Inferno Brand.", bodyStyle);
            GUI.Label(new Rect(leftX + 14f, 164f, columnWidth - 28f, 48f), "Element line: Water enters Element Fusion from the left, Wind enters from below, then the shaped output becomes Frost Pin.", bodyStyle);
            GUI.Label(new Rect(leftX + 14f, 214f, columnWidth - 28f, 18f), "Broken facing stalls the whole recipe.", tinyLabelStyle);

            DrawRect(new Rect(leftX, 258f, columnWidth, 188f), new Color(0.07f, 0.09f, 0.13f, 0.9f));
            DrawOutline(new Rect(leftX, 258f, columnWidth, 188f), new Color(0.18f, 0.25f, 0.32f));
            GUI.Label(new Rect(leftX + 14f, 272f, columnWidth - 28f, 20f), "Controls", sectionStyle);
            DrawGuideRow(new Rect(leftX + 14f, 304f, columnWidth - 28f, 22f), "LMB", "Place empty tile / select occupied tile");
            DrawGuideRow(new Rect(leftX + 14f, 332f, columnWidth - 28f, 22f), "RMB", "Remove selected tile");
            DrawGuideRow(new Rect(leftX + 14f, 360f, columnWidth - 28f, 22f), "R", "Rotate selected machine");
            DrawGuideRow(new Rect(leftX + 14f, 388f, columnWidth - 28f, 22f), "Q / E", "Rotate next placement");
            DrawGuideRow(new Rect(leftX + 14f, 416f, columnWidth - 28f, 22f), "TAB", "Open reward drawer");

            DrawRect(new Rect(rightX, 86f, columnWidth, 178f), new Color(0.07f, 0.09f, 0.13f, 0.9f));
            DrawOutline(new Rect(rightX, 86f, columnWidth, 178f), new Color(0.27f, 0.33f, 0.42f));
            GUI.Label(new Rect(rightX + 14f, 100f, columnWidth - 28f, 20f), "Element Fusion", sectionStyle);
            DrawElementRecipeRow(new Rect(rightX + 14f, 130f, columnWidth - 28f, 24f), WorkshopElementAttribute.Wind, WorkshopElementAttribute.Water, WorkshopElementAttribute.Ice);
            DrawElementRecipeRow(new Rect(rightX + 14f, 160f, columnWidth - 28f, 24f), WorkshopElementAttribute.Wind, WorkshopElementAttribute.Fire, WorkshopElementAttribute.Thunder);
            DrawElementRecipeRow(new Rect(rightX + 14f, 190f, columnWidth - 28f, 24f), WorkshopElementAttribute.Earth, WorkshopElementAttribute.Fire, WorkshopElementAttribute.Light);
            DrawElementRecipeRow(new Rect(rightX + 14f, 220f, columnWidth - 28f, 24f), WorkshopElementAttribute.Earth, WorkshopElementAttribute.Water, WorkshopElementAttribute.Dark);

            DrawRect(new Rect(rightX, 282f, columnWidth, 164f), new Color(0.07f, 0.09f, 0.13f, 0.9f));
            DrawOutline(new Rect(rightX, 282f, columnWidth, 164f), new Color(0.26f, 0.2f, 0.34f));
            GUI.Label(new Rect(rightX + 14f, 296f, columnWidth - 28f, 20f), "Spell Ladder", sectionStyle);
            GUI.Label(new Rect(rightX + 14f, 324f, columnWidth - 28f, 28f), "Element Shaper: one element becomes one basic spell.", bodyStyle);
            GUI.Label(new Rect(rightX + 14f, 356f, columnWidth - 28f, 28f), "Spell Fusion I: two same-element basic spells become an intermediate spell.", bodyStyle);
            GUI.Label(new Rect(rightX + 14f, 388f, columnWidth - 28f, 28f), "Spell Fusion II: compatible basic spell pairs become Ice, Thunder, Light, or Dark intermediates.", bodyStyle);
            GUI.Label(new Rect(rightX + 14f, 420f, columnWidth - 28f, 18f), "Spell Fusion III: opposing intermediates become advanced cards.", tinyLabelStyle);
            GUI.EndGroup();
        }

        private void DrawPaletteDock(Rect rect)
        {
            DrawPanelFrame(rect, new Color(0.88f, 0.74f, 0.33f));

            GUI.BeginGroup(rect);
            GUI.Label(new Rect(18f, 14f, 220f, 24f), "Workshop Palette", titleStyle);
            GUI.Label(new Rect(18f, 38f, 520f, 18f), "Choose a blueprint. LMB empty tile places; LMB occupied tile selects.", mutedStyle);
            DrawElementLegend(new Rect(18f, 58f, 272f, 18f));

            var contentRect = new Rect(14f, 82f, rect.width - 28f, rect.height - 92f);
            var nodes = controller.PlaceableNodes.Where(node => node != null).ToArray();
            const float cardWidth = 220f;
            const float cardHeight = 88f;
            const float spacing = 12f;
            var spiritNodes = nodes.Where(node => node.Category == WorkshopNodeCategory.Source).ToArray();
            var machineNodes = nodes.Where(node => node.Category != WorkshopNodeCategory.Source).ToArray();
            var spiritRowWidth = spiritNodes.Length * (cardWidth + spacing) + spacing;
            var machineRowWidth = machineNodes.Length * (cardWidth + spacing) + spacing;
            var viewWidth = Mathf.Max(contentRect.width - 18f, spiritRowWidth, machineRowWidth);
            var viewHeight = spacing + cardHeight * 2f + spacing * 3f;

            paletteScroll = GUI.BeginScrollView(contentRect, paletteScroll, new Rect(0f, 0f, viewWidth, viewHeight), viewWidth > contentRect.width, false);
            DrawBlueprintRow(spiritNodes, spacing, spacing, cardWidth, cardHeight, spacing);
            DrawBlueprintRow(machineNodes, spacing, spacing * 2f + cardHeight, cardWidth, cardHeight, spacing);

            GUI.EndScrollView();
            GUI.EndGroup();
        }

        private void DrawBlueprintRow(WorkshopNodeDefinition[] nodes, float startX, float y, float cardWidth, float cardHeight, float spacing)
        {
            var x = startX;
            foreach (var node in nodes)
            {
                DrawBlueprintCard(new Rect(x, y, cardWidth, cardHeight), node);
                x += cardWidth + spacing;
            }
        }

        private void DrawHoverTooltip()
        {
            if (controller == null || controller.HoveredCell.x < 0 || showGuide)
            {
                return;
            }

            var node = controller.HoveredNode;
            var mouse = Event.current.mousePosition;
            var tooltipWidth = 238f;
            var tooltipHeight = node == null ? 82f : 126f;
            var x = Mathf.Min(mouse.x + 18f, Screen.width - tooltipWidth - 16f);
            var y = Mathf.Min(mouse.y + 18f, Screen.height - tooltipHeight - 16f);
            var rect = new Rect(x, y, tooltipWidth, tooltipHeight);

            DrawPanelFrame(rect, node == null ? new Color(0.42f, 0.54f, 0.7f) : GetCategoryColor(node.Definition.Category, node.Definition.Tint));
            GUI.BeginGroup(rect);
            GUI.Label(new Rect(14f, 12f, rect.width - 28f, 18f), node == null ? "Empty Tile" : node.Definition.DisplayName, sectionStyle);
            GUI.Label(new Rect(14f, 30f, rect.width - 28f, 18f), $"Cell {controller.HoveredCell.x}, {controller.HoveredCell.y}", tinyLabelStyle);

            if (node == null)
            {
                GUI.Label(new Rect(14f, 50f, rect.width - 28f, 18f), "LMB place armed machine", bodyStyle);
            }
            else
            {
                GUI.Label(new Rect(14f, 50f, rect.width - 28f, 18f), node.Definition.Category.ToString(), tinyLabelStyle);
                GUI.Label(new Rect(14f, 68f, rect.width - 28f, 34f), node.Definition.Description, bodyStyle);
                GUI.Label(new Rect(14f, 104f, rect.width - 28f, 18f), $"Rot {node.RotationQuarterTurns * 90}°  Buffer {node.BufferedItemCount}/{node.Definition.BufferCapacity}", tinyLabelStyle);
            }

            GUI.EndGroup();
        }

        private void DrawBlueprintCard(Rect rect, WorkshopNodeDefinition node)
        {
            if (node == null)
            {
                return;
            }

            var simulation = controller != null ? controller.Simulation : null;
            var unlocked = simulation != null && simulation.IsUnlocked(node);
            var selected = controller != null && controller.SelectedPaletteNode == node;
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

            if (!DrawSprite(new Rect(rect.x + 10f, rect.y + 12f, 32f, 32f), node.NodeSprite, Color.white))
            {
                GUI.Label(new Rect(rect.x + 12f, rect.y + 14f, 28f, 28f), GetCategorySymbol(node.Category), iconStyle);
            }
            DrawNodeElementBadge(new Rect(rect.x + rect.width - 34f, rect.y + 12f, 22f, 22f), node);
            GUI.Label(new Rect(rect.x + 48f, rect.y + 14f, rect.width - 88f, 20f), node.DisplayName, cardTitleStyle);
            GUI.Label(new Rect(rect.x + 48f, rect.y + 36f, rect.width - 58f, 16f), node.Category.ToString(), tinyLabelStyle);
            GUI.Label(new Rect(rect.x + 48f, rect.y + 54f, rect.width - 58f, 16f), unlocked ? "Ready" : "Locked reward", tinyLabelStyle);

            if (!unlocked)
            {
                DrawRect(rect, new Color(0f, 0f, 0f, 0.46f));
                GUI.Label(new Rect(rect.x, rect.y + rect.height * 0.5f - 10f, rect.width, 20f), "LOCKED", chipStyle);
            }
        }

        private void DrawMiniStat(Rect rect, string value, string label)
        {
            DrawRect(rect, new Color(0.09f, 0.11f, 0.14f, 0.92f));
            DrawOutline(rect, new Color(0.24f, 0.22f, 0.18f));
            GUI.Label(new Rect(rect.x, rect.y + 3f, rect.width, 14f), value, statValueStyle);
            GUI.Label(new Rect(rect.x, rect.y + 16f, rect.width, 12f), label, statLabelStyle);
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

        private void DrawCompactList(Rect rect, (string Label, int Amount, Color Tint, Sprite Icon)[] items)
        {
            if (items.Length == 0)
            {
                GUI.Label(new Rect(rect.x, rect.y, rect.width, 18f), "None", tinyLabelStyle);
                return;
            }

            var visibleCount = Mathf.Min(items.Length, Mathf.Max(1, Mathf.FloorToInt(rect.height / 18f)));
            var hasOverflow = items.Length > visibleCount;
            var itemCount = hasOverflow ? Mathf.Max(0, visibleCount - 1) : visibleCount;
            for (var index = 0; index < itemCount; index++)
            {
                var item = items[index];
                var y = rect.y + index * 18f;
                if (!DrawSprite(new Rect(rect.x, y + 1f, 14f, 14f), item.Icon, Color.white))
                {
                    DrawRect(new Rect(rect.x + 2f, y + 4f, 10f, 10f), item.Tint);
                }
                GUI.Label(new Rect(rect.x + 20f, y, rect.width - 76f, 18f), item.Label, bodyStyle);
                GUI.Label(new Rect(rect.x + rect.width - 42f, y, 40f, 18f), $"x{item.Amount}", tinyLabelStyle);
            }

            if (hasOverflow)
            {
                var overflowY = rect.y + (visibleCount - 1) * 18f;
                GUI.Label(new Rect(rect.x, overflowY, rect.width, 18f), $"+{items.Length - itemCount} more", tinyLabelStyle);
            }
        }

        private void DrawElementRecipeRow(Rect rect, WorkshopElementAttribute first, WorkshopElementAttribute second, WorkshopElementAttribute output)
        {
            DrawFormulaIcon(new Rect(rect.x, rect.y, 22f, 22f), first);
            GUI.Label(new Rect(rect.x + 28f, rect.y + 2f, 16f, 18f), "+", chipStyle);
            DrawFormulaIcon(new Rect(rect.x + 48f, rect.y, 22f, 22f), second);
            GUI.Label(new Rect(rect.x + 76f, rect.y + 2f, 24f, 18f), "->", chipStyle);
            DrawFormulaIcon(new Rect(rect.x + 104f, rect.y, 22f, 22f), output);
            GUI.Label(new Rect(rect.x + 134f, rect.y + 2f, rect.width - 134f, 18f), GetElementDisplayName(output), bodyStyle);
        }

        private void DrawFormulaIcon(Rect rect, WorkshopElementAttribute element)
        {
            if (!DrawSprite(rect, ArcaneArtCatalog.GetElementIcon(element), Color.white))
            {
                DrawRect(rect, GetElementColor(element));
                DrawOutline(rect, new Color(0.9f, 0.86f, 0.74f));
                GUI.Label(rect, GetElementDisplayName(element).Substring(0, 1), chipStyle);
            }
        }

        private void DrawNodeElementBadge(Rect rect, WorkshopNodeDefinition node)
        {
            if (TryGetNodeElement(node, out var element))
            {
                DrawFormulaIcon(rect, element);
            }
        }

        private void DrawGuideRow(Rect rect, string key, string action)
        {
            DrawChip(new Rect(rect.x, rect.y, 58f, 20f), key, new Color(0.88f, 0.74f, 0.33f));
            GUI.Label(new Rect(rect.x + 70f, rect.y + 1f, rect.width - 70f, 18f), action, bodyStyle);
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

        private bool DrawSprite(Rect rect, Sprite sprite, Color tint)
        {
            if (sprite == null || sprite.texture == null)
            {
                return false;
            }

            var previousColor = GUI.color;
            GUI.color = tint;
            Rect textureRect = sprite.textureRect;
            Rect uv = new Rect(
                textureRect.x / sprite.texture.width,
                textureRect.y / sprite.texture.height,
                textureRect.width / sprite.texture.width,
                textureRect.height / sprite.texture.height);
            GUI.DrawTextureWithTexCoords(rect, sprite.texture, uv, true);
            GUI.color = previousColor;
            return true;
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
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = new Color(0.96f, 0.95f, 0.92f) }
            };

            statLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 9,
                alignment = TextAnchor.UpperCenter,
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
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            cardTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                wordWrap = false,
                clipping = TextClipping.Clip,
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
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = new Color(0.67f, 0.72f, 0.79f) }
            };
        }

        private void ApplyCameraLayout()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                return;
            }

            camera.rect = new Rect(0f, 0f, 1f, 1f);

            var simulation = controller.Simulation;
            if (simulation == null || !camera.orthographic)
            {
                return;
            }

            var safeArea = new Rect(
                Margin,
                BottomDockHeight + Margin * 1.5f,
                Screen.width - RightRailWidth - Margin * 3f,
                Screen.height - TopHudHeight - BottomDockHeight - Margin * 3f);

            var safeWidthRatio = Mathf.Clamp(safeArea.width / Screen.width, 0.35f, 1f);
            var safeHeightRatio = Mathf.Clamp(safeArea.height / Screen.height, 0.35f, 1f);
            var boardWidth = Mathf.Max(1f, (simulation.GridSize.x - 1) * controller.GridCellSize + controller.GridCellSize * 1.9f);
            var boardHeight = Mathf.Max(1f, (simulation.GridSize.y - 1) * controller.GridCellSize + controller.GridCellSize * 1.9f);
            var fitHeight = boardHeight / (2f * safeHeightRatio);
            var fitWidth = boardWidth / (2f * camera.aspect * safeWidthRatio);
            camera.orthographicSize = Mathf.Max(4.7f, fitHeight, fitWidth);

            var boardCenter = new Vector2(
                (simulation.GridSize.x - 1) * controller.GridCellSize * 0.5f,
                (simulation.GridSize.y - 1) * controller.GridCellSize * 0.5f);
            var screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            var pixelOffset = safeArea.center - screenCenter;
            var worldOffset = new Vector2(
                pixelOffset.x / Screen.width * camera.orthographicSize * camera.aspect * 2f,
                pixelOffset.y / Screen.height * camera.orthographicSize * 2f);

            camera.transform.position = new Vector3(boardCenter.x - worldOffset.x, boardCenter.y - worldOffset.y, -10f);
        }

        private static string ShortItemName(string displayName)
        {
            if (displayName.EndsWith(" Essence"))
            {
                return displayName.Replace(" Essence", string.Empty);
            }

            if (displayName.StartsWith("Arcane "))
            {
                return displayName.Replace("Arcane ", string.Empty);
            }

            return displayName;
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

        private static bool TryGetNodeElement(WorkshopNodeDefinition node, out WorkshopElementAttribute element)
        {
            element = WorkshopElementAttribute.None;
            if (node == null || string.IsNullOrWhiteSpace(node.Id))
            {
                return false;
            }

            string id = node.Id.ToLowerInvariant();
            if (id.Contains(".fire"))
            {
                element = WorkshopElementAttribute.Fire;
            }
            else if (id.Contains(".water"))
            {
                element = WorkshopElementAttribute.Water;
            }
            else if (id.Contains(".wind"))
            {
                element = WorkshopElementAttribute.Wind;
            }
            else if (id.Contains(".earth"))
            {
                element = WorkshopElementAttribute.Earth;
            }
            else if (id.Contains(".ice"))
            {
                element = WorkshopElementAttribute.Ice;
            }
            else if (id.Contains(".thunder"))
            {
                element = WorkshopElementAttribute.Thunder;
            }
            else if (id.Contains(".light"))
            {
                element = WorkshopElementAttribute.Light;
            }
            else if (id.Contains(".dark"))
            {
                element = WorkshopElementAttribute.Dark;
            }
            else
            {
                return false;
            }

            return true;
        }

        private static string GetElementDisplayName(WorkshopElementAttribute element)
        {
            return element switch
            {
                WorkshopElementAttribute.Fire => "Fire",
                WorkshopElementAttribute.Water => "Water",
                WorkshopElementAttribute.Wind => "Wind",
                WorkshopElementAttribute.Earth => "Earth",
                WorkshopElementAttribute.Ice => "Ice",
                WorkshopElementAttribute.Thunder => "Thunder",
                WorkshopElementAttribute.Light => "Light",
                WorkshopElementAttribute.Dark => "Dark",
                _ => "Neutral"
            };
        }

        private static Color GetElementColor(WorkshopElementAttribute element)
        {
            return element switch
            {
                WorkshopElementAttribute.Fire => new Color(0.95f, 0.36f, 0.2f),
                WorkshopElementAttribute.Water => new Color(0.26f, 0.62f, 0.97f),
                WorkshopElementAttribute.Wind => new Color(0.57f, 0.84f, 0.9f),
                WorkshopElementAttribute.Earth => new Color(0.63f, 0.47f, 0.28f),
                WorkshopElementAttribute.Ice => new Color(0.67f, 0.9f, 1f),
                WorkshopElementAttribute.Thunder => new Color(0.95f, 0.88f, 0.3f),
                WorkshopElementAttribute.Light => new Color(1f, 0.95f, 0.69f),
                WorkshopElementAttribute.Dark => new Color(0.45f, 0.4f, 0.62f),
                _ => new Color(0.65f, 0.73f, 0.8f)
            };
        }

        private static Sprite GetItemIcon(WorkshopItemDefinition item)
        {
            return item == null ? null : ArcaneArtCatalog.GetElementIcon(item.Element);
        }

        private void DrawRewardIcon(Rect rect, WorkshopRewardDefinition reward)
        {
            Sprite icon = reward == null
                ? null
                : reward.RewardKind switch
                {
                    WorkshopRewardKind.UnlockNode => reward.TargetNode != null ? reward.TargetNode.NodeSprite : null,
                    WorkshopRewardKind.GrantItems => reward.GrantedItems != null && reward.GrantedItems.Length > 0 ? GetItemIcon(reward.GrantedItems[0].Item) : null,
                    WorkshopRewardKind.EfficiencyBoost => reward.TargetNode != null ? reward.TargetNode.NodeSprite : null,
                    _ => null
                };

            if (!DrawSprite(rect, icon, Color.white))
            {
                DrawRect(rect, new Color(0.17f, 0.19f, 0.24f, 0.96f));
                DrawOutline(rect, new Color(0.4f, 0.34f, 0.62f));
                GUI.Label(rect, reward != null ? "✦" : "•", iconStyle);
            }
        }

        private void DrawElementLegend(Rect rect)
        {
            WorkshopElementAttribute[] elements =
            {
                WorkshopElementAttribute.Fire,
                WorkshopElementAttribute.Water,
                WorkshopElementAttribute.Wind,
                WorkshopElementAttribute.Earth,
                WorkshopElementAttribute.Ice,
                WorkshopElementAttribute.Thunder,
                WorkshopElementAttribute.Light,
                WorkshopElementAttribute.Dark
            };

            float x = rect.x;
            foreach (WorkshopElementAttribute element in elements)
            {
                Sprite icon = ArcaneArtCatalog.GetElementIcon(element);
                if (DrawSprite(new Rect(x, rect.y, 16f, 16f), icon, Color.white))
                {
                    x += 22f;
                }
            }
        }
    }
}
