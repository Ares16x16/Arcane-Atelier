using System.Linq;
using ArcaneAtelier;
using ArcaneAtelier.Audio;
using UnityEngine;

namespace ArcaneAtelier.Workshop
{
    public sealed class WorkshopHudPresenter : MonoBehaviour
    {
        private const float Margin = 20f;
        private const float RightRailWidth = 380f;
        private const float BottomDockHeight = 292f;
        private const float TopHudHeight = 94f;

        private static readonly Color HudBackground = new Color(0.035f, 0.052f, 0.085f, 0.94f);
        private static readonly Color HudPanel = new Color(0.075f, 0.105f, 0.15f, 0.93f);
        private static readonly Color HudPanelSoft = new Color(0.095f, 0.13f, 0.19f, 0.82f);
        private static readonly Color HudStroke = new Color(0.24f, 0.3f, 0.38f, 0.86f);
        private static readonly Color HudText = new Color(0.97f, 0.95f, 0.9f, 1f);
        private static readonly Color HudMuted = new Color(0.65f, 0.71f, 0.78f, 1f);
        private static readonly Color AtelierGold = new Color(0.88f, 0.72f, 0.3f, 1f);
        private static readonly Color ArcaneBlue = new Color(0.42f, 0.72f, 0.94f, 1f);
        private static readonly Color SpellViolet = new Color(0.72f, 0.5f, 0.96f, 1f);

        private Vector2 rewardScroll;
        private bool showGuide;
        private bool showRewards;
        private int paletteTabIndex;
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
        private GUIStyle tabButtonStyle;
        private GUIStyle tooltipPrimaryStyle;
        private GUIStyle tooltipSecondaryStyle;
        private GUIStyle tooltipEmptyStyle;

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
            const float controlWidth = 236f;
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

            if (showRewards)
            {
                DrawRewardDrawer(GetRewardDrawerRect());
            }

            DrawHoverTooltip();

            if (showGuide)
            {
                DrawGuideOverlay(new Rect(Screen.width * 0.5f - 410f, Screen.height * 0.5f - 250f, 820f, 500f));
            }
        }

        private void DrawBackdrop()
        {
            DrawRect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0.01f, 0.015f, 0.026f, 0.14f));
            DrawRect(new Rect(0f, 0f, Screen.width, TopHudHeight + Margin), new Color(0.025f, 0.04f, 0.07f, 0.58f));
            DrawRect(new Rect(0f, 0f, Screen.width, 3f), new Color(AtelierGold.r, AtelierGold.g, AtelierGold.b, 0.64f));
            DrawRect(new Rect(0f, TopHudHeight + Margin - 2f, Screen.width, 2f), new Color(ArcaneBlue.r, ArcaneBlue.g, ArcaneBlue.b, 0.22f));
            DrawRect(new Rect(0f, Screen.height - BottomDockHeight - Margin * 2f, Screen.width, BottomDockHeight + Margin * 2f), new Color(0.015f, 0.025f, 0.045f, 0.82f));
            DrawRect(new Rect(0f, Screen.height - BottomDockHeight - Margin * 2f, Screen.width, 3f), new Color(AtelierGold.r, AtelierGold.g, AtelierGold.b, 0.3f));
            DrawRect(new Rect(Screen.width - RightRailWidth - Margin * 2f, TopHudHeight, RightRailWidth + Margin * 2f, Screen.height - TopHudHeight), new Color(0f, 0f, 0f, 0.12f));
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
            const float buttonSize = 26f;
            const float buttonGap = 6f;
            const float rotationChipWidth = 54f;
            var buttonStartX = rect.width - 12f - (rotationChipWidth + 12f + buttonSize * 5f + buttonGap * 4f);
            DrawRect(new Rect(buttonStartX, 12f, rotationChipWidth, 24f), new Color(0.1f, 0.12f, 0.16f, 0.94f));
            DrawOutline(new Rect(buttonStartX, 12f, rotationChipWidth, 24f), new Color(0.32f, 0.28f, 0.22f));
            GUI.Label(new Rect(buttonStartX, 16f, rotationChipWidth, 14f), $"{controller.PlacementRotationQuarterTurns * 90}°", chipStyle);
            buttonStartX += rotationChipWidth + 12f;

            if (DrawThemedButton(new Rect(buttonStartX, 10f, buttonSize, buttonSize), "?", ArcaneBlue, smallButtonStyle, "toggle_guide"))
            {
                showGuide = !showGuide;
            }

            if (DrawThemedButton(new Rect(buttonStartX + buttonSize + buttonGap, 10f, buttonSize, buttonSize), "✦", SpellViolet, smallButtonStyle, "toggle_rewards"))
            {
                showRewards = !showRewards;
            }

            var pauseLabel = controller.IsPaused ? "▶" : "⏸";
            if (DrawThemedButton(new Rect(buttonStartX + (buttonSize + buttonGap) * 2f, 10f, buttonSize, buttonSize), pauseLabel, AtelierGold, smallButtonStyle, "toggle_pause"))
            {
                controller.TogglePause();
            }

            if (DrawThemedButton(new Rect(buttonStartX + (buttonSize + buttonGap) * 3f, 10f, buttonSize, buttonSize), "↺", new Color(0.9f, 0.5f, 0.34f, 1f), smallButtonStyle, "reset_workshop"))
            {
                controller.ResetWorkshop();
            }

            if (DrawThemedButton(new Rect(buttonStartX + (buttonSize + buttonGap) * 4f, 10f, buttonSize, buttonSize), "H", new Color(0.54f, 0.78f, 0.54f, 1f), smallButtonStyle, "load_hack_layout"))
            {
                controller.LoadHackFactoryLayout();
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
                DrawSubPanel(new Rect(18f, 76f, contentWidth, 70f), ArcaneBlue);
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

            DrawRect(new Rect(18f, detailBottom, contentWidth, 1f), new Color(HudStroke.r, HudStroke.g, HudStroke.b, 0.66f));

            DrawSubPanel(new Rect(18f, payloadTop, columnWidth, payloadHeight), ArcaneBlue);
            GUI.Label(new Rect(30f, payloadTop + 12f, columnWidth - 24f, 20f), "Inventory", sectionStyle);
            GUI.Label(new Rect(30f, payloadTop + 32f, columnWidth - 24f, 18f), "Network + reserve", tinyLabelStyle);
            DrawCompactList(new Rect(30f, payloadTop + 58f, columnWidth - 24f, payloadHeight - 70f), inventoryItems);

            var deckX = 18f + columnWidth + columnGap;
            DrawSubPanel(new Rect(deckX, payloadTop, columnWidth, payloadHeight), AtelierGold);
            GUI.Label(new Rect(deckX + 12f, payloadTop + 12f, columnWidth - 24f, 20f), "Battle Deck", sectionStyle);
            GUI.Label(new Rect(deckX + 12f, payloadTop + 32f, columnWidth - 24f, 18f), "Cards that reached collectors", tinyLabelStyle);
            DrawCompactList(new Rect(deckX + 12f, payloadTop + 58f, columnWidth - 24f, payloadHeight - 70f), deckItems);

            if (DrawThemedButton(new Rect(18f, stepButtonY, contentWidth, 28f), "Advance 1 Prep Tick", ArcaneBlue, buttonStyle, "advance_tick"))
            {
                controller.StepPreparationOnce();
            }

            if (DrawThemedButton(new Rect(18f, deployButtonY, contentWidth, 28f), "Forge And Deploy", AtelierGold, buttonStyle, "forge_and_deploy", playClickSound: false))
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
                DrawSubPanel(itemRect, SpellViolet);
                DrawRewardIcon(new Rect(12f, y + 12f, 42f, 42f), reward);
                GUI.Label(new Rect(62f, y + 10f, itemRect.width - 136f, 18f), reward.DisplayName, sectionStyle);
                GUI.Label(new Rect(62f, y + 30f, itemRect.width - 136f, 30f), reward.Description, bodyStyle);
                if (DrawThemedButton(new Rect(itemRect.width - 68f, y + 24f, 56f, 26f), "Use", SpellViolet, buttonStyle, $"reward_use_{reward.Id}"))
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
            if (DrawThemedButton(new Rect(rect.width - 64f, 18f, 38f, 28f), "X", new Color(0.9f, 0.5f, 0.34f, 1f), buttonStyle, "close_guide"))
            {
                showGuide = false;
            }

            const float leftX = 24f;
            const float columnWidth = 354f;
            const float rightX = 426f;

            DrawSubPanel(new Rect(leftX, 86f, columnWidth, 154f), AtelierGold);
            GUI.Label(new Rect(leftX + 14f, 100f, columnWidth - 28f, 20f), "Starter Layout", sectionStyle);
            GUI.Label(new Rect(leftX + 14f, 126f, columnWidth - 28f, 34f), "Spell line: two Fire Shapers feed Spell Fusion I -> Inferno Brand.", bodyStyle);
            GUI.Label(new Rect(leftX + 14f, 164f, columnWidth - 28f, 48f), "Element line: Water enters Element Fusion from the left, Wind enters from below, then the shaped output becomes Frost Pin.", bodyStyle);
            GUI.Label(new Rect(leftX + 14f, 214f, columnWidth - 28f, 18f), "Broken facing stalls the whole recipe.", tinyLabelStyle);

            DrawSubPanel(new Rect(leftX, 258f, columnWidth, 216f), ArcaneBlue);
            GUI.Label(new Rect(leftX + 14f, 272f, columnWidth - 28f, 20f), "Controls", sectionStyle);
            DrawGuideRow(new Rect(leftX + 14f, 304f, columnWidth - 28f, 22f), "LMB", "Click place/select, hold-drag pan map");
            DrawGuideRow(new Rect(leftX + 14f, 332f, columnWidth - 28f, 22f), "RMB Tile", "Remove selected tile");
            DrawGuideRow(new Rect(leftX + 14f, 360f, columnWidth - 28f, 22f), "RMB Card", "Arm mirror corner conduit");
            DrawGuideRow(new Rect(leftX + 14f, 388f, columnWidth - 28f, 22f), "R", "Rotate selected machine");
            DrawGuideRow(new Rect(leftX + 14f, 416f, columnWidth - 28f, 22f), "Q / E", "Rotate next placement");
            DrawGuideRow(new Rect(leftX + 14f, 444f, columnWidth - 28f, 22f), "Fusion Edge", "Click edge cycles input, output, off");
            DrawGuideRow(new Rect(leftX + 14f, 472f, columnWidth - 28f, 22f), "Wheel", "Zoom workshop map");

            DrawSubPanel(new Rect(rightX, 86f, columnWidth, 178f), ArcaneBlue);
            GUI.Label(new Rect(rightX + 14f, 100f, columnWidth - 28f, 20f), "Element Fusion", sectionStyle);
            DrawElementRecipeRow(new Rect(rightX + 14f, 130f, columnWidth - 28f, 24f), WorkshopElementAttribute.Wind, WorkshopElementAttribute.Water, WorkshopElementAttribute.Ice);
            DrawElementRecipeRow(new Rect(rightX + 14f, 160f, columnWidth - 28f, 24f), WorkshopElementAttribute.Wind, WorkshopElementAttribute.Fire, WorkshopElementAttribute.Thunder);
            DrawElementRecipeRow(new Rect(rightX + 14f, 190f, columnWidth - 28f, 24f), WorkshopElementAttribute.Earth, WorkshopElementAttribute.Fire, WorkshopElementAttribute.Light);
            DrawElementRecipeRow(new Rect(rightX + 14f, 220f, columnWidth - 28f, 24f), WorkshopElementAttribute.Earth, WorkshopElementAttribute.Water, WorkshopElementAttribute.Dark);

            DrawSubPanel(new Rect(rightX, 282f, columnWidth, 164f), SpellViolet);
            GUI.Label(new Rect(rightX + 14f, 296f, columnWidth - 28f, 20f), "Spell Ladder", sectionStyle);
            GUI.Label(new Rect(rightX + 14f, 324f, columnWidth - 28f, 28f), "Element Shaper: one element becomes one basic spell.", bodyStyle);
            GUI.Label(new Rect(rightX + 14f, 356f, columnWidth - 28f, 28f), "Spell Fusion I: two same-element basic spells become an intermediate spell.", bodyStyle);
            GUI.Label(new Rect(rightX + 14f, 388f, columnWidth - 28f, 28f), "Spell Fusion II: compatible intermediate spells become advanced spells.", bodyStyle);
            GUI.Label(new Rect(rightX + 14f, 420f, columnWidth - 28f, 18f), "Spell Fusion III: two matching advanced spells become final cards.", tinyLabelStyle);
            GUI.EndGroup();
        }

        private void DrawPaletteDock(Rect rect)
        {
            DrawPanelFrame(rect, new Color(0.88f, 0.74f, 0.33f));

            GUI.BeginGroup(rect);
            GUI.Label(new Rect(18f, 14f, 220f, 24f), "Workshop Palette", titleStyle);
            GUI.Label(new Rect(18f, 38f, 760f, 18f), "Choose a blueprint. LMB arms default, RMB arms mirror on corner conduits. Short LMB places/selects; hold LMB and drag pans.", mutedStyle);
            DrawElementLegend(new Rect(18f, 58f, 272f, 18f));

            DrawPaletteTabs(new Rect(18f, 82f, rect.width - 36f, 28f));

            var contentRect = new Rect(14f, 118f, rect.width - 28f, rect.height - 128f);
            var nodes = GetPaletteNodesForActiveTab();
            const float spacing = 12f;
            const float cardHeight = 96f;
            int columns = Mathf.Max(1, Mathf.FloorToInt((contentRect.width + spacing) / (204f + spacing)));
            float cardWidth = Mathf.Floor((contentRect.width - spacing * (columns + 1)) / columns);
            int rows = Mathf.Max(1, Mathf.CeilToInt(nodes.Length / (float)columns));
            int visibleRows = Mathf.Min(rows, 2);
            float gridHeight = visibleRows * cardHeight + (visibleRows + 1) * spacing;
            DrawSubPanel(new Rect(contentRect.x, contentRect.y, contentRect.width, gridHeight), AtelierGold);

            for (int index = 0; index < nodes.Length; index++)
            {
                int row = index / columns;
                int column = index % columns;
                if (row >= visibleRows)
                {
                    break;
                }

                float x = contentRect.x + spacing + column * (cardWidth + spacing);
                float y = contentRect.y + spacing + row * (cardHeight + spacing);
                DrawBlueprintCard(new Rect(x, y, cardWidth, cardHeight), nodes[index]);
            }
            GUI.EndGroup();
        }

        private void DrawHoverTooltip()
        {
            if (controller == null || controller.HoveredCell.x < 0 || showGuide)
            {
                return;
            }

            var node = controller.HoveredNode;
            var mouse = Event.current.mousePosition;
            if (IsPointerOverWorkshopUi(mouse))
            {
                return;
            }

            bool showBufferDetails = node != null && Input.GetKey(KeyCode.T);
            var bufferEntries = !showBufferDetails
                ? System.Array.Empty<System.Collections.Generic.KeyValuePair<WorkshopItemDefinition, int>>()
                : node.EnumerateBuffer().Where(pair => pair.Key != null && pair.Value > 0).Take(8).ToArray();
            var tooltipWidth = showBufferDetails ? 386f : node == null ? 238f : 304f;
            var tooltipHeight = node == null ? 82f : showBufferDetails ? 172f + Mathf.Min(bufferEntries.Length, 8) * 48f : 132f;
            Rect rect = PositionTooltip(mouse, tooltipWidth, tooltipHeight);

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
                string activeText = node.IsRecentlyActive ? "Active" : "Idle";
                GUI.Label(new Rect(14f, 104f, rect.width - 28f, 18f), $"Rot {node.RotationQuarterTurns * 90}°  Buffer {node.BufferedItemCount}/{node.Definition.BufferCapacity}  {activeText}", tinyLabelStyle);
                if (showBufferDetails)
                {
                    DrawTooltipBuffer(new Rect(14f, 126f, rect.width - 28f, rect.height - 138f), node, bufferEntries);
                }
            }

            GUI.EndGroup();
        }

        private Rect PositionTooltip(Vector2 mouse, float tooltipWidth, float tooltipHeight)
        {
            Rect viewport = GetFactoryViewportRect();
            if (showRewards)
            {
                Rect rewardRect = GetRewardDrawerRect();
                viewport.xMax = Mathf.Min(viewport.xMax, rewardRect.xMin - 8f);
            }

            float x = mouse.x + 18f;
            float y = mouse.y + 18f;

            if (x + tooltipWidth > viewport.xMax)
            {
                x = mouse.x - tooltipWidth - 18f;
            }

            if (y + tooltipHeight > viewport.yMax)
            {
                y = mouse.y - tooltipHeight - 18f;
            }

            x = Mathf.Clamp(x, viewport.xMin + 8f, viewport.xMax - tooltipWidth - 8f);
            y = Mathf.Clamp(y, viewport.yMin + 8f, viewport.yMax - tooltipHeight - 8f);
            return new Rect(x, y, tooltipWidth, tooltipHeight);
        }

        private void DrawTooltipBuffer(Rect rect, WorkshopNodeState node, System.Collections.Generic.KeyValuePair<WorkshopItemDefinition, int>[] bufferEntries)
        {
            GUI.Label(new Rect(rect.x, rect.y, rect.width, 18f), "Buffer", sectionStyle);
            var listY = rect.y + 24f;
            if (node == null || bufferEntries.Length == 0)
            {
                DrawRect(new Rect(rect.x, listY, rect.width, 34f), new Color(0.05f, 0.06f, 0.08f, 0.84f));
                GUI.Label(new Rect(rect.x + 10f, listY + 5f, rect.width - 20f, 24f), "Empty", tooltipEmptyStyle);
                return;
            }

            foreach (var pair in bufferEntries)
            {
                var rowRect = new Rect(rect.x, listY, rect.width, 44f);
                DrawRect(rowRect, new Color(pair.Key.Tint.r * 0.22f, pair.Key.Tint.g * 0.22f, pair.Key.Tint.b * 0.22f, 0.82f));
                DrawItemIcon(new Rect(rowRect.x + 7f, rowRect.y + 7f, 30f, 30f), pair.Key);
                GUI.Label(new Rect(rowRect.x + 46f, rowRect.y + 5f, rowRect.width - 104f, 18f), pair.Key.DisplayName, tooltipPrimaryStyle);
                GUI.Label(new Rect(rowRect.x + 46f, rowRect.y + 23f, rowRect.width - 104f, 18f), pair.Key.Kind == WorkshopItemKind.Card ? pair.Key.SpellTier.ToString() : pair.Key.Element.ToString(), tooltipSecondaryStyle);
                GUI.Label(new Rect(rowRect.xMax - 48f, rowRect.y + 12f, 40f, 18f), $"x{pair.Value}", tooltipPrimaryStyle);
                listY += 48f;
            }
        }

        private void DrawItemIcon(Rect rect, WorkshopItemDefinition item)
        {
            if (item == null)
            {
                return;
            }

            Sprite sprite = ArcaneArtCatalog.GetElementIcon(item.Element);
            if (sprite != null && sprite.texture != null)
            {
                GUI.DrawTexture(rect, sprite.texture, ScaleMode.ScaleToFit, true);
                return;
            }

            DrawRect(rect, item.Tint);
            DrawOutline(rect, new Color(1f, 1f, 1f, 0.28f));
        }

        private bool IsPointerOverWorkshopUi(Vector2 mousePosition)
        {
            const float throughputWidth = 278f;
            const float controlWidth = 236f;
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

            if (topLeftRect.Contains(mousePosition) || topCenterRect.Contains(mousePosition) || topRightRect.Contains(mousePosition) || rightRailRect.Contains(mousePosition) || paletteRect.Contains(mousePosition))
            {
                return true;
            }

            if (showRewards)
            {
                var rewardRect = GetRewardDrawerRect();
                if (rewardRect.Contains(mousePosition))
                {
                    return true;
                }
            }

            return false;
        }

        private static Rect GetFactoryViewportRect()
        {
            return new Rect(
                Margin,
                TopHudHeight + Margin,
                Screen.width - RightRailWidth - Margin * 3f,
                Screen.height - BottomDockHeight - TopHudHeight - Margin * 3f);
        }

        private static Rect GetRewardDrawerRect()
        {
            return new Rect(Screen.width - RightRailWidth - 336f, 104f, 320f, Mathf.Min(360f, Screen.height - BottomDockHeight - 138f));
        }

        private void DrawPaletteTabs(Rect rect)
        {
            string[] labels = { "Spirits", "Routing", "Core", "Fusion" };
            float gap = 8f;
            float tabWidth = Mathf.Min(148f, (rect.width - gap * (labels.Length - 1)) / labels.Length);

            for (int index = 0; index < labels.Length; index++)
            {
                Rect tabRect = new Rect(rect.x + index * (tabWidth + gap), rect.y, tabWidth, rect.height);
                bool selected = paletteTabIndex == index;
                bool isHover = IsInteractiveHover(tabRect, true, $"palette_tab_{index}");
                Color accent = selected ? AtelierGold : HudStroke;
                Color background = selected
                    ? new Color(0.18f, 0.16f, 0.09f, isHover ? 1f : 0.96f)
                    : isHover ? new Color(0.12f, 0.16f, 0.23f, 0.98f) : HudPanel;
                Color outline = selected
                    ? new Color(accent.r, accent.g, accent.b, isHover ? 0.94f : 0.82f)
                    : new Color(accent.r, accent.g, accent.b, isHover ? 0.68f : 0.46f);
                float accentAlpha = selected ? (isHover ? 1f : 0.9f) : (isHover ? 0.56f : 0.34f);
                DrawRect(new Rect(tabRect.x + 2f, tabRect.y + 3f, tabRect.width, tabRect.height), new Color(0f, 0f, 0f, 0.16f));
                DrawRect(tabRect, background);
                DrawOutline(tabRect, outline);
                DrawRect(new Rect(tabRect.x, tabRect.y, tabRect.width, 3f), new Color(accent.r, accent.g, accent.b, accentAlpha));
                GUI.Label(tabRect, labels[index], tabButtonStyle);
                if (HandleInteractiveRect(tabRect, $"palette_tab_{index}"))
                {
                    paletteTabIndex = index;
                }
            }
        }

        private WorkshopNodeDefinition[] GetPaletteNodesForActiveTab()
        {
            var nodes = controller.PlaceableNodes
                .Where(node => node != null && !WorkshopNodeVariantUtility.IsMirrorVariant(node.Id))
                .ToArray();

            return paletteTabIndex switch
            {
                0 => nodes.Where(node => node.Category == WorkshopNodeCategory.Source).ToArray(),
                1 => nodes.Where(node =>
                        node.Id == "node.factory.conduit" ||
                        node.Id == WorkshopNodeVariantUtility.TurningConduitId ||
                        node.Id == "node.factory.spell_conduit" ||
                        node.Id == WorkshopNodeVariantUtility.TurningSpellConduitId)
                    .ToArray(),
                2 => nodes.Where(node =>
                        node.Id == "node.factory.element_fusion" ||
                        node.Id == "node.factory.element_shaping" ||
                        node.Id == "node.factory.deck_collector")
                    .ToArray(),
                3 => nodes.Where(node =>
                        node.Id == "node.factory.spell_fusion.basic" ||
                        node.Id == "node.factory.spell_fusion.intermediate" ||
                        node.Id == "node.factory.spell_fusion.advanced")
                    .ToArray(),
                _ => nodes
            };
        }

        private void DrawBlueprintCard(Rect rect, WorkshopNodeDefinition node)
        {
            if (node == null)
            {
                return;
            }

            var simulation = controller != null ? controller.Simulation : null;
            var unlocked = simulation != null && simulation.IsUnlocked(node);
            var mirrorNode = FindMirrorPaletteNode(node);
            var selectedNode = controller != null ? controller.SelectedPaletteNode : null;
            var selected = controller != null && (selectedNode == node || selectedNode == mirrorNode);
            var mirrorSelected = mirrorNode != null && selectedNode == mirrorNode;
            var accent = GetCategoryColor(node.Category, node.Tint);
            bool isHover = IsInteractiveHover(rect, unlocked, $"palette_node_{node.Id}");
            float lift = isHover ? 3f : 0f;
            Rect drawRect = new Rect(rect.x, rect.y - lift, rect.width, rect.height);
            Color cardFill = selected
                ? new Color(accent.r * 0.28f, accent.g * 0.28f, accent.b * 0.28f, 0.98f)
                : isHover ? new Color(0.11f, 0.15f, 0.22f, 0.98f) : HudPanel;
            Color cardOutline = selected
                ? new Color(0.99f, 0.86f, 0.5f, 0.95f)
                : new Color(accent.r, accent.g, accent.b, isHover ? 0.8f : 0.58f);
            Color badgeFill = new Color(accent.r, accent.g, accent.b, selected || isHover ? 0.24f : 0.14f);

            DrawRect(new Rect(drawRect.x + 3f, drawRect.y + 4f, drawRect.width, drawRect.height), new Color(0f, 0f, 0f, 0.2f));
            DrawRect(drawRect, cardFill);
            DrawOutline(drawRect, cardOutline);
            DrawRect(new Rect(drawRect.x, drawRect.y, drawRect.width, 5f), accent);
            DrawRect(new Rect(drawRect.x + 8f, drawRect.y + 10f, 30f, 34f), badgeFill);
            DrawRect(new Rect(drawRect.x + 8f, drawRect.y + drawRect.height - 8f, drawRect.width - 16f, 1f), new Color(1f, 1f, 1f, 0.045f));

            Event current = Event.current;
            if (unlocked &&
                mirrorNode != null &&
                current != null &&
                current.type == EventType.MouseDown &&
                current.button == 1 &&
                rect.Contains(current.mousePosition))
            {
                AudioManager.PlaySFX(SFXType.ButtonClick);
                controller.SetPaletteNode(mirrorNode);
                current.Use();
            }

            if (HandleInteractiveRect(rect, $"palette_node_{node.Id}", unlocked))
            {
                controller.SetPaletteNode(node);
            }

            var iconSprite = ResolveNodeSprite(node);
            var iconTint = ResolveNodeSpriteTint(node);
            if (!DrawSprite(new Rect(drawRect.x + 10f, drawRect.y + 12f, 32f, 32f), iconSprite, iconTint))
            {
                GUI.Label(new Rect(drawRect.x + 12f, drawRect.y + 14f, 28f, 28f), GetCategorySymbol(node.Category), iconStyle);
            }
            DrawNodeElementBadge(new Rect(drawRect.x + drawRect.width - 34f, drawRect.y + 12f, 22f, 22f), node);
            GUI.Label(new Rect(drawRect.x + 48f, drawRect.y + 14f, drawRect.width - 88f, 20f), node.DisplayName, cardTitleStyle);
            GUI.Label(new Rect(drawRect.x + 48f, drawRect.y + 36f, drawRect.width - 58f, 16f), node.Category.ToString(), tinyLabelStyle);
            GUI.Label(new Rect(drawRect.x + 48f, drawRect.y + 54f, drawRect.width - 58f, 16f), unlocked ? "Ready" : "Locked reward", tinyLabelStyle);

            if (mirrorNode != null)
            {
                DrawCornerVariantStrip(new Rect(drawRect.x + 10f, drawRect.y + 72f, drawRect.width - 20f, 18f), node, mirrorNode, mirrorSelected, unlocked);
            }

            if (!unlocked)
            {
                DrawRect(drawRect, new Color(0f, 0f, 0f, 0.46f));
                GUI.Label(new Rect(drawRect.x, drawRect.y + drawRect.height * 0.5f - 10f, drawRect.width, 20f), "LOCKED", chipStyle);
            }
        }

        private void DrawCornerVariantStrip(Rect rect, WorkshopNodeDefinition primaryNode, WorkshopNodeDefinition mirrorNode, bool mirrorSelected, bool unlocked)
        {
            DrawRect(rect, new Color(0.045f, 0.065f, 0.095f, 0.94f));
            DrawOutline(rect, new Color(HudStroke.r, HudStroke.g, HudStroke.b, 0.56f));

            var leftPreview = new Rect(rect.x + 6f, rect.y + 2f, 14f, 14f);
            var rightPreview = new Rect(rect.x + 26f, rect.y + 2f, 14f, 14f);
            var defaultHighlight = new Rect(rect.x + 4f, rect.y + 1f, 18f, 16f);
            var mirrorHighlight = new Rect(rect.x + 24f, rect.y + 1f, 18f, 16f);
            if (mirrorSelected)
            {
                DrawRect(mirrorHighlight, new Color(AtelierGold.r, AtelierGold.g, AtelierGold.b, 0.25f));
                DrawOutline(mirrorHighlight, new Color(0.98f, 0.82f, 0.48f, 0.9f));
            }
            else
            {
                DrawRect(defaultHighlight, new Color(ArcaneBlue.r, ArcaneBlue.g, ArcaneBlue.b, 0.22f));
                DrawOutline(defaultHighlight, new Color(0.76f, 0.92f, 1f, 0.82f));
            }

            DrawSprite(leftPreview, ResolveNodeSprite(primaryNode), ResolveNodeSpriteTint(primaryNode));
            DrawSprite(rightPreview, ResolveNodeSprite(mirrorNode), ResolveNodeSpriteTint(mirrorNode), true);
            GUI.Label(new Rect(rect.x + 50f, rect.y + 1f, 60f, 16f), mirrorSelected ? "Mirror" : "Default", tinyLabelStyle);
            GUI.Label(new Rect(rect.x + 108f, rect.y + 1f, rect.width - 114f, 16f), unlocked ? "LMB default  RMB mirror" : "Unlock to arm", tinyLabelStyle);
        }

        private void DrawMiniStat(Rect rect, string value, string label)
        {
            DrawRect(new Rect(rect.x + 2f, rect.y + 3f, rect.width, rect.height), new Color(0f, 0f, 0f, 0.18f));
            DrawRect(rect, new Color(0.07f, 0.095f, 0.135f, 0.94f));
            DrawOutline(rect, new Color(AtelierGold.r, AtelierGold.g, AtelierGold.b, 0.42f));
            DrawRect(new Rect(rect.x + 4f, rect.y + 3f, rect.width - 8f, 1f), new Color(1f, 1f, 1f, 0.08f));
            GUI.Label(new Rect(rect.x, rect.y + 3f, rect.width, 14f), value, statValueStyle);
            GUI.Label(new Rect(rect.x, rect.y + 16f, rect.width, 12f), label, statLabelStyle);
        }

        private static Sprite ResolveNodeSprite(WorkshopNodeDefinition node)
        {
            if (node == null)
            {
                return null;
            }

            return node.NodeSprite != null
                ? node.NodeSprite
                : ArcaneArtCatalog.GetWorkshopNodeSprite(node.Id);
        }

        private static Color ResolveNodeSpriteTint(WorkshopNodeDefinition node)
        {
            return node == null
                ? Color.white
                : ArcaneArtCatalog.GetWorkshopNodeTint(node.Id);
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
            DrawRect(new Rect(rect.x + 1f, rect.y + 2f, rect.width, rect.height), new Color(0f, 0f, 0f, 0.16f));
            DrawRect(rect, new Color(tint.r * 0.22f, tint.g * 0.22f, tint.b * 0.22f, 0.94f));
            DrawOutline(rect, new Color(tint.r * 0.95f, tint.g * 0.95f, tint.b * 0.95f, 0.82f));
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
                if ((index & 1) == 0)
                {
                    DrawRect(new Rect(rect.x - 4f, y, rect.width + 4f, 17f), new Color(1f, 1f, 1f, 0.018f));
                }

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
            DrawRect(new Rect(rect.x + 5f, rect.y + 7f, rect.width, rect.height), new Color(0f, 0f, 0f, 0.24f));
            DrawRect(rect, HudBackground);
            DrawOutline(rect, new Color(accent.r, accent.g, accent.b, 0.68f));
            DrawRect(new Rect(rect.x, rect.y, rect.width, 4f), accent);
            DrawRect(new Rect(rect.x + 7f, rect.y + 8f, rect.width - 14f, rect.height - 16f), new Color(1f, 1f, 1f, 0.012f));
            DrawRect(new Rect(rect.x + 1f, rect.y + 5f, rect.width - 2f, 1f), new Color(1f, 1f, 1f, 0.05f));
        }

        private void DrawSubPanel(Rect rect, Color accent)
        {
            DrawRect(new Rect(rect.x + 3f, rect.y + 4f, rect.width, rect.height), new Color(0f, 0f, 0f, 0.18f));
            DrawRect(rect, HudPanelSoft);
            DrawOutline(rect, new Color(accent.r, accent.g, accent.b, 0.42f));
            DrawRect(new Rect(rect.x, rect.y, rect.width, 3f), new Color(accent.r, accent.g, accent.b, 0.72f));
            DrawRect(new Rect(rect.x + 8f, rect.y + 8f, rect.width - 16f, 1f), new Color(1f, 1f, 1f, 0.045f));
        }

        private bool DrawThemedButton(Rect rect, string label, Color accent, GUIStyle labelStyle, string interactionId, bool playClickSound = true)
        {
            bool isHover = IsInteractiveHover(rect, true, interactionId);
            Color fillColor = isHover
                ? new Color(accent.r * 0.3f, accent.g * 0.3f, accent.b * 0.3f, 0.99f)
                : new Color(accent.r * 0.22f, accent.g * 0.22f, accent.b * 0.22f, 0.96f);
            Color outlineColor = isHover
                ? new Color(accent.r, accent.g, accent.b, 0.94f)
                : new Color(accent.r, accent.g, accent.b, 0.72f);
            Color topStripColor = new Color(accent.r, accent.g, accent.b, isHover ? 1f : 0.92f);
            DrawRect(new Rect(rect.x + 2f, rect.y + 3f, rect.width, rect.height), new Color(0f, 0f, 0f, 0.2f));
            DrawRect(rect, fillColor);
            DrawOutline(rect, outlineColor);
            DrawRect(new Rect(rect.x, rect.y, rect.width, 3f), topStripColor);
            GUI.Label(rect, label, labelStyle);
            return HandleInteractiveRect(rect, interactionId, true, playClickSound);
        }

        private bool HandleInteractiveRect(Rect rect, string interactionId, bool enabled = true, bool playClickSound = true)
        {
            if (!enabled)
            {
                return false;
            }

            if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
            {
                if (playClickSound)
                    AudioManager.PlaySFX(SFXType.ButtonClick);
                return true;
            }

            return false;
        }

        private static bool IsInteractiveHover(Rect rect, bool enabled, string interactionId)
        {
            if (!enabled)
            {
                return false;
            }

            Event current = Event.current;
            if (current == null || !rect.Contains(current.mousePosition))
            {
                return false;
            }

            AudioManager.ReportUIHover($"workshop:{interactionId}");
            return true;
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

        private WorkshopNodeDefinition FindMirrorPaletteNode(WorkshopNodeDefinition node)
        {
            if (node == null || controller == null || !WorkshopNodeVariantUtility.TryGetMirrorVariantId(node.Id, out string mirrorNodeId))
            {
                return null;
            }

            return controller.PlaceableNodes.FirstOrDefault(candidate => candidate != null && candidate.Id == mirrorNodeId);
        }

        private bool DrawSprite(Rect rect, Sprite sprite, Color tint)
        {
            return DrawSprite(rect, sprite, tint, false);
        }

        private bool DrawSprite(Rect rect, Sprite sprite, Color tint, bool flipX)
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
            if (flipX)
            {
                uv = new Rect(uv.x + uv.width, uv.y, -uv.width, uv.height);
            }
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
                normal = { textColor = HudText }
            };

            sectionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.96f, 0.86f, 0.62f) }
            };

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true,
                normal = { textColor = new Color(0.84f, 0.87f, 0.91f) }
            };

            mutedStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true,
                normal = { textColor = HudMuted }
            };

            statValueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = HudText }
            };

            statLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 9,
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = HudMuted }
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
                normal = { textColor = HudText }
            };

            buttonStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = HudText }
            };

            smallButtonStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = HudText }
            };

            tabButtonStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = HudText }
            };

            cardTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                wordWrap = false,
                clipping = TextClipping.Clip,
                normal = { textColor = HudText }
            };

            cardBodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                wordWrap = true,
                normal = { textColor = new Color(0.76f, 0.81f, 0.88f) }
            };

            tinyLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                wordWrap = true,
                alignment = TextAnchor.UpperLeft,
                normal = { textColor = HudMuted }
            };

            tooltipPrimaryStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                wordWrap = false,
                clipping = TextClipping.Clip,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.94f, 0.93f, 0.89f) }
            };

            tooltipSecondaryStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                wordWrap = false,
                clipping = TextClipping.Clip,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.67f, 0.72f, 0.79f) }
            };

            tooltipEmptyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = false,
                clipping = TextClipping.Clip,
                alignment = TextAnchor.MiddleLeft,
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
