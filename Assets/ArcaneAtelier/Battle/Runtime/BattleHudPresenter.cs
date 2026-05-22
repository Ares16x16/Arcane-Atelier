using ArcaneAtelier.Audio;
using ArcaneAtelier.Workshop;
using UnityEngine;

namespace ArcaneAtelier.Battle
{
    public sealed class BattleHudPresenter : MonoBehaviour
    {
        private const float Margin = 18f;
        private const float TopBarHeight = 192f;
        private const float BottomPanelHeight = 276f;
        private const float CardWidth = 196f;
        private const float CardHeight = 176f;
        private const float CardSpacing = 14f;
        private const float DragThreshold = 10f;

        private static readonly Color HudBackground = new Color(0.04f, 0.06f, 0.1f, 0.92f);
        private static readonly Color HudPanel = new Color(0.08f, 0.11f, 0.15f, 0.92f);
        private static readonly Color HudPanelSoft = new Color(0.1f, 0.14f, 0.2f, 0.78f);
        private static readonly Color HudStroke = new Color(0.22f, 0.27f, 0.34f, 0.84f);
        private static readonly Color HudText = new Color(0.97f, 0.95f, 0.9f, 1f);
        private static readonly Color HudMuted = new Color(0.66f, 0.71f, 0.78f, 1f);
        private static readonly Color VictoryAccent = new Color(0.35f, 0.78f, 0.45f, 1f);
        private static readonly Color DefeatAccent = new Color(0.84f, 0.34f, 0.32f, 1f);
        private static readonly Color WarningAccent = new Color(0.9f, 0.25f, 0.2f, 1f);
        private static readonly Color ShieldAccent = new Color(0.64f, 0.8f, 0.98f, 1f);
        private static readonly Color ApAccent = new Color(0.88f, 0.72f, 0.3f, 1f);

        private BattleSceneController controller;
        private Texture2D whiteTexture;
        private GUIStyle titleStyle;
        private GUIStyle sectionStyle;
        private GUIStyle bodyStyle;
        private GUIStyle mutedStyle;
        private GUIStyle statStyle;
        private GUIStyle cardTitleStyle;
        private GUIStyle pillStyle;
        private GUIStyle resultStyle;
        private GUIStyle centeredMutedStyle;
        private GUIStyle centeredBodyStyle;
        private GUIStyle chipStyle;
        private GUIStyle darkChipStyle;
        private GUIStyle cardMetaStyle;
        private GUIStyle cardSummaryStyle;
        private GUIStyle targetHintStyle;
        private Vector2 handScroll;
        private int selectedCardIndex = -1;
        private int pressedCardIndex = -1;
        private int draggingCardIndex = -1;
        private Vector2 pressMousePosition;
        private Vector2 dragMousePosition;
        private Rect playerTargetScreenRect;
        private Rect bossTargetScreenRect;
        private DragTarget activeDropTarget = DragTarget.None;
        private float actionPointFlash;
        private int lastObservedActionPoints = -1;
        private BattleResult lastShownResult;
        private float resultOverlayShownAt = -1f;

        private enum DragTarget
        {
            None,
            Player,
            Boss
        }

        public void Initialize(BattleSceneController sceneController)
        {
            controller = sceneController;
        }

        private void OnGUI()
        {
            if (controller == null || controller.Simulation == null || controller.Player == null || controller.Boss == null)
            {
                return;
            }

            EnsureTheme();
            SyncTransientState();
            UpdateTargetRects();
            ProcessInput(Event.current);
            DrawBackdrop();

            Rect topBarRect = new Rect(Margin, Margin, Screen.width - Margin * 2f, TopBarHeight);
            Rect handRect = new Rect(Margin, Screen.height - BottomPanelHeight - Margin, Screen.width - Margin * 2f, BottomPanelHeight);

            DrawWorldTargetHighlights();
            DrawTopBar(topBarRect);
            DrawHandPanel(handRect);
            DrawDraggedCard();

            if (controller.CurrentResult != null)
            {
                DrawResultOverlay(new Rect(Screen.width * 0.5f - 270f, Screen.height * 0.5f - 168f, 540f, 336f));
            }
        }

        private void ProcessInput(Event currentEvent)
        {
            if (currentEvent == null)
            {
                return;
            }

            if (controller.CurrentResult != null || !controller.IsPlayerInputAllowed)
            {
                ClearInteractionState();
                return;
            }

            Vector2 mousePosition = currentEvent.mousePosition;

            if (currentEvent.type == EventType.MouseDrag && pressedCardIndex >= 0)
            {
                if (Vector2.Distance(mousePosition, pressMousePosition) >= DragThreshold)
                {
                    draggingCardIndex = pressedCardIndex;
                    dragMousePosition = mousePosition;
                    activeDropTarget = GetDropTargetForCard(draggingCardIndex, mousePosition);
                    currentEvent.Use();
                }
            }
            else if (currentEvent.type == EventType.MouseUp)
            {
                if (draggingCardIndex >= 0)
                {
                    DragTarget dropTarget = GetDropTargetForCard(draggingCardIndex, mousePosition);
                    int resolvedIndex = draggingCardIndex;
                    bool played = dropTarget != DragTarget.None && controller.TryPlayCardFromHud(resolvedIndex);
                    selectedCardIndex = played ? -1 : resolvedIndex;
                    pressedCardIndex = -1;
                    draggingCardIndex = -1;
                    activeDropTarget = DragTarget.None;
                    currentEvent.Use();
                    return;
                }

                if (pressedCardIndex >= 0)
                {
                    selectedCardIndex = pressedCardIndex;
                    pressedCardIndex = -1;
                    currentEvent.Use();
                }
            }
            else if (draggingCardIndex >= 0 && currentEvent.type == EventType.Repaint)
            {
                dragMousePosition = mousePosition;
                activeDropTarget = GetDropTargetForCard(draggingCardIndex, mousePosition);
            }
        }

        private void DrawBackdrop()
        {
            DrawRect(new Rect(0f, 0f, Screen.width, 220f), new Color(0.02f, 0.04f, 0.08f, 0.24f));
            DrawRect(new Rect(0f, 0f, Screen.width, 104f), new Color(0.18f, 0.12f, 0.04f, 0.08f));
            DrawRect(new Rect(0f, Screen.height - BottomPanelHeight - Margin * 2f, Screen.width, BottomPanelHeight + Margin * 2f), new Color(0.02f, 0.03f, 0.06f, 0.44f));
            DrawRect(new Rect(0f, Screen.height - BottomPanelHeight - Margin * 1.5f, Screen.width, 48f), new Color(0.2f, 0.14f, 0.05f, 0.08f));
        }

        private void DrawTopBar(Rect rect)
        {
            DrawPanelFrame(rect, ApAccent, 0.94f);
            GUI.BeginGroup(rect);

            float sideWidth = Mathf.Clamp((rect.width - 440f) * 0.5f, 260f, 360f);
            Rect playerRect = new Rect(16f, 16f, sideWidth, rect.height - 32f);
            Rect centerRect = new Rect(rect.width * 0.5f - 190f, 16f, 380f, rect.height - 32f);
            Rect bossRect = new Rect(rect.width - sideWidth - 16f, 16f, sideWidth, rect.height - 32f);

            DrawUnitStatusBlock(playerRect, controller.Player, "Player", false);
            DrawCenterBattleStrip(centerRect);
            DrawUnitStatusBlock(bossRect, controller.Boss, "Enemy", true);

            GUI.EndGroup();
        }

        private void DrawUnitStatusBlock(Rect rect, BattleUnit unit, string fallbackTitle, bool alignRight)
        {
            string displayName = unit != null && !string.IsNullOrWhiteSpace(unit.DisplayName) ? unit.DisplayName : fallbackTitle;
            int currentHealth = unit != null ? unit.CurrentHealth : 0;
            int maxHealth = unit != null ? unit.MaxHealth : 1;
            int shield = unit != null ? unit.Shield : 0;
            WorkshopElementAttribute element = unit != null ? unit.Element : WorkshopElementAttribute.None;
            Color accent = GetElementColor(element);

            DrawPanelWithShadow(rect, new Color(HudPanel.r, HudPanel.g, HudPanel.b, 0.96f), new Color(accent.r, accent.g, accent.b, 0.48f));

            GUIStyle nameStyle = new GUIStyle(sectionStyle)
            {
                alignment = alignRight ? TextAnchor.UpperRight : TextAnchor.UpperLeft
            };
            GUIStyle valueStyle = new GUIStyle(mutedStyle)
            {
                alignment = alignRight ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft
            };

            float contentX = rect.x + 14f;
            float contentWidth = rect.width - 28f;
            float labelWidth = Mathf.Max(64f, contentWidth * 0.26f);
            string elementText = element == WorkshopElementAttribute.None ? "Neutral" : element.ToString();

            GUI.Label(new Rect(contentX, rect.y + 10f, contentWidth, 18f), displayName, nameStyle);
            GUI.Label(new Rect(contentX, rect.y + 30f, contentWidth, 14f), elementText, valueStyle);
            GUI.Label(new Rect(contentX, rect.y + 52f, labelWidth, 14f), $"HP {currentHealth}/{maxHealth}", valueStyle);

            Rect healthBarRect = new Rect(contentX + labelWidth + 8f, rect.y + 54f, contentWidth - labelWidth - 8f, 10f);
            Rect shieldBarRect = new Rect(contentX + labelWidth + 8f, rect.y + 78f, contentWidth - labelWidth - 8f, 8f);
            float healthRatio = maxHealth > 0 ? Mathf.Clamp01(currentHealth / (float)maxHealth) : 0f;
            float shieldRatio = maxHealth > 0 ? Mathf.Clamp01(shield / (float)maxHealth) : 0f;

            DrawProgressBar(healthBarRect, healthRatio, accent, new Color(0.12f, 0.16f, 0.22f, 1f));
            DrawProgressBar(shieldBarRect, shieldRatio, ShieldAccent, new Color(0.09f, 0.12f, 0.18f, 1f));
            GUI.Label(new Rect(contentX, rect.y + 74f, labelWidth, 14f), shield > 0 ? $"Shield {shield}" : "Shield 0", valueStyle);

            DrawRect(new Rect(contentX, rect.y + 98f, contentWidth, 1f), new Color(HudStroke.r, HudStroke.g, HudStroke.b, 0.6f));

            string statusText = BuildStatusList(unit);
            if (string.IsNullOrEmpty(statusText))
            {
                statusText = "Status: None";
            }

            GUI.Label(new Rect(contentX, rect.y + 106f, contentWidth, 36f), statusText, mutedStyle);
        }

        private void DrawActionPoints(Rect rect)
        {
            if (controller.Simulation == null)
            {
                return;
            }

            string apText = $"AP {controller.Simulation.ActionPoints}/{controller.Simulation.MaxActionPoints}";
            float glow = 0.18f + actionPointFlash * 0.26f;
            DrawPanelWithShadow(rect, new Color(HudPanel.r, HudPanel.g, HudPanel.b, 0.96f), new Color(ApAccent.r, ApAccent.g, ApAccent.b, 0.52f + glow));

            float segmentWidth = 20f;
            float segmentSpacing = 10f;
            float startX = rect.x + 16f;
            for (int i = 0; i < controller.Simulation.MaxActionPoints; i++)
            {
                Rect segmentRect = new Rect(startX + i * (segmentWidth + segmentSpacing), rect.y + 3f, segmentWidth, 12f);
                bool isActive = i < controller.Simulation.ActionPoints;
                float pulse = isActive ? 0.08f * Mathf.Sin(Time.unscaledTime * 6f + i) : 0f;
                DrawRect(segmentRect, isActive
                    ? new Color(ApAccent.r, ApAccent.g, ApAccent.b, 0.84f + pulse)
                    : new Color(0.2f, 0.24f, 0.3f, 0.9f));
                DrawOutline(segmentRect, isActive
                    ? new Color(0.98f, 0.92f, 0.72f, 0.8f)
                    : new Color(HudStroke.r, HudStroke.g, HudStroke.b, 0.88f));
            }

            GUI.Label(new Rect(rect.x + rect.width - 86f, rect.y + 1f, 74f, 16f), apText, chipStyle);
        }

        private void DrawCenterBattleStrip(Rect rect)
        {
            DrawPanelWithShadow(rect, new Color(HudPanelSoft.r, HudPanelSoft.g, HudPanelSoft.b, 0.9f), new Color(HudStroke.r, HudStroke.g, HudStroke.b, 0.82f));

            BattleBossAction nextAction = controller.Simulation.BossAI.PeekNextAction();
            Color intentAccent = GetIntentColor(nextAction.ActionType);
            string intentBadge = GetIntentBadge(nextAction.ActionType);
            string intent = TruncateText(controller.BossIntentDescription, 44);
            bool bossTurnPending = controller.IsBossTurnPending;
            float windupProgress = controller.BossTurnWindupProgress;

            if (bossTurnPending)
            {
                DrawRect(
                    new Rect(rect.x + 8f, rect.y + 8f, rect.width - 16f, rect.height - 16f),
                    new Color(intentAccent.r, intentAccent.g, intentAccent.b, 0.08f + windupProgress * 0.08f));
            }

            GUI.Label(new Rect(rect.x, rect.y + 8f, rect.width, 14f), $"Encounter {controller.CurrentEncounterNumber}/{controller.TotalEncounterCount}", centeredMutedStyle);
            GUI.Label(
                new Rect(rect.x, rect.y + 24f, rect.width, 14f),
                bossTurnPending ? "Enemy action incoming" : $"Turn {controller.Simulation.TurnsElapsed + 1}",
                centeredMutedStyle);
            DrawTag(new Rect(rect.x + rect.width * 0.5f - 56f, rect.y + 46f, 112f, 20f), intentBadge, new Color(intentAccent.r, intentAccent.g, intentAccent.b, 0.88f));
            GUI.Label(
                new Rect(rect.x + 18f, rect.y + 72f, rect.width - 36f, 30f),
                bossTurnPending ? $"Preparing: {intent}" : intent,
                centeredBodyStyle);

            DrawActionPoints(new Rect(rect.x + 28f, rect.y + 108f, rect.width - 56f, 20f));

            bool canEnd = controller.CanEndTurn;
            if (DrawThemedButton(new Rect(rect.x + rect.width * 0.5f - 82f, rect.y + 136f, 164f, 24f), "End Turn", ApAccent, "end_turn", canEnd, playClickSound: false))
            {
                controller.EndTurnFromHud();
            }

            if (bossTurnPending)
            {
                DrawRect(
                    new Rect(rect.x + 18f, rect.y + rect.height - 7f, (rect.width - 36f) * windupProgress, 3f),
                    new Color(intentAccent.r, intentAccent.g, intentAccent.b, 0.88f));
            }
        }

        private void DrawHandPanel(Rect rect)
        {
            DrawPanelFrame(rect, ApAccent, 0.96f);
            GUI.BeginGroup(rect);

            int handCount = controller.Simulation.Deck.HandCount;
            Rect headerRect = new Rect(14f, 12f, rect.width - 28f, 30f);
            Rect contentRect = new Rect(14f, 48f, rect.width - 28f, rect.height - 62f);

            DrawRect(headerRect, new Color(0.06f, 0.08f, 0.12f, 0.66f));
            DrawOutline(headerRect, new Color(HudStroke.r, HudStroke.g, HudStroke.b, 0.42f));
            GUI.Label(new Rect(headerRect.x + 12f, headerRect.y + 6f, 180f, 18f), "Prepared Cards", sectionStyle);
            GUI.Label(
                new Rect(headerRect.x + headerRect.width - 320f, headerRect.y + 7f, 308f, 16f),
                $"Hand {controller.Simulation.Deck.HandCount}  •  Draw {controller.Simulation.Deck.DrawPileCount}  •  Discard {controller.Simulation.Deck.DiscardPileCount}",
                new GUIStyle(mutedStyle) { alignment = TextAnchor.MiddleRight });

            DrawRect(contentRect, new Color(0.05f, 0.07f, 0.11f, 0.54f));
            DrawOutline(contentRect, new Color(HudStroke.r, HudStroke.g, HudStroke.b, 0.32f));
            DrawRect(new Rect(contentRect.x + 10f, contentRect.y + 8f, contentRect.width - 20f, 1f), new Color(1f, 1f, 1f, 0.03f));
            DrawRect(new Rect(contentRect.x + 10f, contentRect.yMax - 9f, contentRect.width - 20f, 1f), new Color(0f, 0f, 0f, 0.18f));

            float viewWidth = Mathf.Max(contentRect.width - 18f, handCount * (CardWidth + CardSpacing) + CardSpacing);
            handScroll = GUI.BeginScrollView(contentRect, handScroll, new Rect(0f, 0f, viewWidth, CardHeight + 12f), true, false);
            for (int i = 0; i < handCount; i++)
            {
                Rect cardRect = new Rect(CardSpacing + i * (CardWidth + CardSpacing), 6f, CardWidth, CardHeight);
                DrawCard(cardRect, controller.Simulation.Deck.Hand[i], i);
            }

            GUI.EndScrollView();
            GUI.EndGroup();
        }

        private void DrawCard(Rect rect, WorkshopBattleCardEntry card, int index)
        {
            if (draggingCardIndex == index)
            {
                DrawGhostCard(rect);
                return;
            }

            Event currentEvent = Event.current;
            bool isSelected = selectedCardIndex == index;
            bool isPressed = pressedCardIndex == index;
            bool isHover = rect.Contains(currentEvent.mousePosition);
            bool canAfford = controller.CanPlayCard(index);
            bool canInteract = controller.IsPlayerInputAllowed && canAfford;

            if (isHover && canInteract)
            {
                AudioManager.ReportUIHover($"battle:card:{card.CardId}:{index}");
            }

            if (currentEvent.type == EventType.MouseDown &&
                currentEvent.button == 0 &&
                rect.Contains(currentEvent.mousePosition) &&
                canInteract)
            {
                AudioManager.PlaySFX(SFXType.CardDraw);
                pressedCardIndex = index;
                pressMousePosition = currentEvent.mousePosition;
                dragMousePosition = currentEvent.mousePosition;
                activeDropTarget = DragTarget.None;
                currentEvent.Use();
            }

            Color accent = GetElementColor(card.Element);
            Color outline = isSelected || isPressed
                ? accent
                : new Color(accent.r, accent.g, accent.b, 0.62f);

            float lift = isPressed ? 2f : isHover || isSelected ? 5f : 0f;
            DrawCardVisual(new Rect(rect.x, rect.y - lift, rect.width, rect.height), card, index, outline, accent, canAfford, false);
        }

        private void DrawGhostCard(Rect rect)
        {
            DrawPanelWithShadow(rect, new Color(0.08f, 0.11f, 0.15f, 0.22f), new Color(0.4f, 0.45f, 0.5f, 0.32f));
        }

        private void DrawDraggedCard()
        {
            if (draggingCardIndex < 0 || draggingCardIndex >= controller.Simulation.Deck.Hand.Count)
            {
                return;
            }

            WorkshopBattleCardEntry card = controller.Simulation.Deck.Hand[draggingCardIndex];
            Rect dragRect = new Rect(
                dragMousePosition.x - CardWidth * 0.5f,
                dragMousePosition.y - CardHeight * 0.5f,
                CardWidth,
                CardHeight);
            Color accent = GetElementColor(card.Element);
            Color outline = activeDropTarget != DragTarget.None
                ? accent
                : new Color(0.55f, 0.59f, 0.66f, 0.72f);

            DrawCardVisual(dragRect, card, draggingCardIndex, outline, accent, true, true);
        }

        private void DrawCardVisual(
            Rect rect,
            WorkshopBattleCardEntry card,
            int index,
            Color outline,
            Color accent,
            bool canAfford,
            bool isDragged)
        {
            int apCost = BattleDeckController.GetActionPointCost(card.Role);
            Color panelColor = canAfford
                ? new Color(HudPanel.r, HudPanel.g, HudPanel.b, isDragged ? 0.98f : 0.94f)
                : new Color(0.11f, 0.12f, 0.14f, isDragged ? 0.88f : 0.78f);
            Color shadowColor = canAfford
                ? new Color(accent.r, accent.g, accent.b, isDragged ? 0.28f : 0.18f)
                : new Color(0f, 0f, 0f, 0.12f);

            DrawPanelWithShadow(rect, panelColor, outline, shadowColor);
            DrawRect(new Rect(rect.x, rect.y, rect.width, 6f), accent);
            DrawRect(new Rect(rect.x + 12f, rect.y + 12f, 30f, 22f), new Color(accent.r, accent.g, accent.b, 0.9f));
            GUI.Label(new Rect(rect.x + 12f, rect.y + 14f, 30f, 18f), index < 9 ? (index + 1).ToString() : "-", pillStyle);

            DrawTag(new Rect(rect.x + rect.width - 58f, rect.y + 12f, 46f, 20f), $"{apCost} AP", new Color(ApAccent.r, ApAccent.g, ApAccent.b, 0.96f), darkChipStyle);
            GUI.Label(new Rect(rect.x + 50f, rect.y + 10f, rect.width - 114f, 34f), card.DisplayName, cardTitleStyle);
            DrawRect(new Rect(rect.x + 12f, rect.y + 48f, rect.width - 24f, 1f), new Color(HudStroke.r, HudStroke.g, HudStroke.b, 0.8f));
            GUI.Label(new Rect(rect.x + 12f, rect.y + 56f, rect.width - 24f, 40f), BuildCardSummary(card), cardSummaryStyle);
            GUI.Label(new Rect(rect.x + 12f, rect.y + 100f, rect.width - 24f, 14f), BuildCardMeta(card), cardMetaStyle);
            DrawTag(new Rect(rect.x + 12f, rect.y + 122f, 100f, 18f), BuildTargetLabel(card), new Color(accent.r, accent.g, accent.b, 0.26f));
            GUI.Label(new Rect(rect.x + 12f, rect.y + 146f, rect.width - 24f, 18f), canAfford ? BuildDragHint(card) : "Insufficient AP", centeredMutedStyle);

            if (!canAfford)
            {
                DrawRect(rect, new Color(0.04f, 0.04f, 0.05f, 0.26f));
            }
        }

        private void DrawWorldTargetHighlights()
        {
            if (draggingCardIndex < 0 || draggingCardIndex >= controller.Simulation.Deck.Hand.Count)
            {
                return;
            }

            WorkshopBattleCardEntry card = controller.Simulation.Deck.Hand[draggingCardIndex];
            DragTarget expectedTarget = GetExpectedTarget(card);

            if (expectedTarget == DragTarget.Player && playerTargetScreenRect.width > 0f)
            {
                DrawTargetOverlay(playerTargetScreenRect, new Color(0.24f, 0.56f, 0.95f, activeDropTarget == DragTarget.Player ? 0.34f : 0.15f), "Target Self");
            }
            else if (expectedTarget == DragTarget.Boss && bossTargetScreenRect.width > 0f)
            {
                DrawTargetOverlay(bossTargetScreenRect, new Color(0.93f, 0.37f, 0.28f, activeDropTarget == DragTarget.Boss ? 0.34f : 0.15f), "Target Enemy");
            }
        }

        private void DrawTargetOverlay(Rect rect, Color color, string text)
        {
            Rect expandedRect = new Rect(rect.x - 10f, rect.y - 10f, rect.width + 20f, rect.height + 20f);
            DrawRect(expandedRect, new Color(color.r, color.g, color.b, color.a * 0.55f));
            DrawOutline(expandedRect, new Color(color.r, color.g, color.b, 0.82f));
            DrawRect(new Rect(expandedRect.x, expandedRect.y, expandedRect.width, 4f), new Color(color.r, color.g, color.b, 0.86f));
            DrawTag(new Rect(expandedRect.x + expandedRect.width * 0.5f - 50f, expandedRect.y - 24f, 100f, 18f), text, new Color(color.r, color.g, color.b, 0.9f));
        }

        private void DrawTag(Rect rect, string text, Color background)
        {
            DrawTag(rect, text, background, centeredMutedStyle);
        }

        private void DrawTag(Rect rect, string text, Color background, GUIStyle style)
        {
            DrawRect(rect, background);
            DrawOutline(rect, new Color(1f, 1f, 1f, 0.08f));
            GUI.Label(rect, text, style);
        }

        private void DrawResultOverlay(Rect rect)
        {
            BattleResult result = controller.CurrentResult;
            string outcome = result.ResultType == BattleResultType.Victory ? "Victory" : "Defeat";
            Color accent = result.ResultType == BattleResultType.Victory ? VictoryAccent : DefeatAccent;
            float intro = resultOverlayShownAt >= 0f ? Mathf.Clamp01((Time.unscaledTime - resultOverlayShownAt) * 4f) : 1f;
            float easedIntro = intro * intro * (3f - 2f * intro);
            float width = Mathf.Lerp(520f, rect.width, easedIntro);
            float height = Mathf.Lerp(286f, rect.height, easedIntro);
            Rect animatedRect = new Rect(Screen.width * 0.5f - width * 0.5f, Screen.height * 0.5f - height * 0.5f, width, height);

            bool isFinalVictory = result.ResultType == BattleResultType.Victory && controller.CurrentEncounterNumber >= controller.TotalEncounterCount;
            bool isDefeat = result.ResultType == BattleResultType.Defeat;

            DrawRect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.54f + 0.14f * easedIntro));
            DrawPanelFrame(animatedRect, accent, 0.98f);
            DrawRect(new Rect(animatedRect.x, animatedRect.y, animatedRect.width, 52f), new Color(accent.r, accent.g, accent.b, 0.2f));

            GUI.BeginGroup(animatedRect);
            GUI.Label(new Rect(28f, 14f, animatedRect.width - 56f, 30f), outcome, resultStyle);
            GUI.Label(new Rect(28f, 44f, animatedRect.width - 56f, 18f), result.BossDisplayName, sectionStyle);
            GUI.Label(new Rect(28f, 68f, animatedRect.width - 56f, 16f), result.ResultType == BattleResultType.Victory ? $"Run cleared. {result.EncountersCleared} encounters completed." : $"Run failed after clearing {result.EncountersCleared} encounter(s).", mutedStyle);

            DrawMiniStat(new Rect(28f, 98f, 104f, 52f), $"{result.TotalDamageDealt}", "Damage");
            DrawMiniStat(new Rect(144f, 98f, 104f, 52f), $"{result.TotalHealingDone}", "Healing");
            DrawMiniStat(new Rect(260f, 98f, 104f, 52f), $"{result.TotalShieldGained}", "Shield");
            DrawMiniStat(new Rect(376f, 98f, 104f, 52f), $"{result.CardsPlayed}", "Cards");

            DrawRect(new Rect(28f, 168f, animatedRect.width - 56f, 1f), new Color(HudStroke.r, HudStroke.g, HudStroke.b, 0.72f));
            GUI.Label(new Rect(28f, 182f, animatedRect.width - 56f, 18f), $"Final encounter: {result.FinalEncounterId}", bodyStyle);
            GUI.Label(new Rect(28f, 204f, animatedRect.width - 56f, 18f), $"Turns elapsed: {result.TurnsElapsed}", bodyStyle);
            GUI.Label(new Rect(28f, 226f, animatedRect.width - 56f, 18f), "Run summary recorded.", mutedStyle);

            if (isDefeat || isFinalVictory)
            {
                if (DrawThemedButton(new Rect(animatedRect.width - 176f, animatedRect.height - 54f, 148f, 30f), "Main Menu", accent, "result_main_menu", true))
                {
                    controller.ReturnToMainMenu();
                }
            }

            else
            {
                if (DrawThemedButton(new Rect(animatedRect.width - 176f, animatedRect.height - 54f, 148f, 30f), "To Workshop", accent, "result_to_workshop", true))
                {
                    controller.ReturnToWorkshop(); // We will add this method to the controller
                }
                
                // GUI.Label(new Rect(28f, 248f, animatedRect.width - 56f, 18f), "Prepare for the next encounter.", VictoryAccent);
            }

            GUI.EndGroup();
        }

        private void DrawMiniStat(Rect rect, string value, string label)
        {
            DrawPanelWithShadow(rect, new Color(HudPanel.r, HudPanel.g, HudPanel.b, 0.98f), new Color(HudStroke.r, HudStroke.g, HudStroke.b, 0.72f));
            GUI.Label(new Rect(rect.x, rect.y + 8f, rect.width, 18f), value, statStyle);
            GUI.Label(new Rect(rect.x, rect.y + 28f, rect.width, 14f), label, centeredMutedStyle);
        }

        private void UpdateTargetRects()
        {
            playerTargetScreenRect = BuildWorldTargetRect(controller.VisualManager != null ? controller.VisualManager.PlayerVisual : null);
            bossTargetScreenRect = BuildWorldTargetRect(controller.VisualManager != null ? controller.VisualManager.BossVisual : null);
        }

        private Rect BuildWorldTargetRect(BattleUnitVisual visual)
        {
            if (visual == null || controller.VisualManager == null || controller.VisualManager.BattleCamera == null)
            {
                return Rect.zero;
            }

            SpriteRenderer renderer = visual.SpriteRenderer;
            Camera camera = controller.VisualManager.BattleCamera;
            if (renderer == null || renderer.sprite == null)
            {
                return Rect.zero;
            }

            Bounds bounds = renderer.bounds;
            Vector3 min = camera.WorldToScreenPoint(bounds.min);
            Vector3 max = camera.WorldToScreenPoint(bounds.max);

            if (min.z < 0f || max.z < 0f)
            {
                return Rect.zero;
            }

            float xMin = Mathf.Min(min.x, max.x);
            float xMax = Mathf.Max(min.x, max.x);
            float yMin = Mathf.Min(min.y, max.y);
            float yMax = Mathf.Max(min.y, max.y);

            float top = Screen.height - yMax;
            float height = yMax - yMin;
            return new Rect(xMin, top, xMax - xMin, height);
        }

        private DragTarget GetDropTargetForCard(int handIndex, Vector2 mousePosition)
        {
            if (handIndex < 0 || handIndex >= controller.Simulation.Deck.Hand.Count)
            {
                return DragTarget.None;
            }

            DragTarget expectedTarget = GetExpectedTarget(controller.Simulation.Deck.Hand[handIndex]);
            switch (expectedTarget)
            {
                case DragTarget.Player:
                    return playerTargetScreenRect.Contains(mousePosition) ? DragTarget.Player : DragTarget.None;
                case DragTarget.Boss:
                    return bossTargetScreenRect.Contains(mousePosition) ? DragTarget.Boss : DragTarget.None;
                default:
                    return DragTarget.None;
            }
        }

        private DragTarget GetExpectedTarget(WorkshopBattleCardEntry card)
        {
            switch (card.Role)
            {
                case WorkshopSpellRole.Attack:
                    return DragTarget.Boss;
                case WorkshopSpellRole.Healing:
                case WorkshopSpellRole.Defense:
                    return DragTarget.Player;
                default:
                    return DragTarget.None;
            }
        }

        private string BuildCardSummary(WorkshopBattleCardEntry card)
        {
            BattleCardDefinition definition = controller != null ? controller.GetCardDefinition(card.CardId) : null;

            if (definition != null)
            {
                return BuildDefinitionSummary(definition);
            }

            switch (card.Role)
            {
                case WorkshopSpellRole.Attack:
                    return Mathf.Max(1, card.HitCount) > 1
                        ? $"Deal {card.PrimaryValue} x{Mathf.Max(1, card.HitCount)} damage"
                        : $"Deal {card.PrimaryValue} damage";
                case WorkshopSpellRole.Healing:
                    return Mathf.Max(1, card.HitCount) > 1
                        ? $"Restore {card.PrimaryValue} HP x{Mathf.Max(1, card.HitCount)}"
                        : $"Restore {card.PrimaryValue} HP";
                case WorkshopSpellRole.Defense:
                    return Mathf.Max(1, card.HitCount) > 1
                        ? $"Gain {card.PrimaryValue} shield x{Mathf.Max(1, card.HitCount)}"
                        : $"Gain {card.PrimaryValue} shield";
                default:
                    return "No effect data";
            }
        }

        private string BuildCardMeta(WorkshopBattleCardEntry card)
        {
            if (card.Element == WorkshopElementAttribute.None)
            {
                return card.Role.ToString();
            }

            return $"{card.Element}  •  {card.Role}";
        }

        private string BuildDragHint(WorkshopBattleCardEntry card)
        {
            switch (GetExpectedTarget(card))
            {
                case DragTarget.Boss:
                    return "Release on enemy";
                case DragTarget.Player:
                    return "Release on self";
                default:
                    return "Cannot be dragged";
            }
        }

        private string BuildTargetLabel(WorkshopBattleCardEntry card)
        {
            switch (GetExpectedTarget(card))
            {
                case DragTarget.Boss:
                    return "Enemy";
                case DragTarget.Player:
                    return "Self";
                default:
                    return "None";
            }
        }

        private string BuildDefinitionSummary(BattleCardDefinition definition)
        {
            if (definition == null || definition.Instructions.Count == 0)
            {
                return "No effect data";
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            for (int i = 0; i < definition.Instructions.Count; i++)
            {
                BattleEffectInstruction instruction = definition.Instructions[i];
                if (builder.Length > 0)
                {
                    builder.Append(" + ");
                }

                switch (instruction.Type)
                {
                    case BattleEffectType.Damage:
                        builder.Append(instruction.HitCount > 1
                            ? $"Deal {instruction.Value} x{instruction.HitCount}"
                            : $"Deal {instruction.Value}");
                        break;
                    case BattleEffectType.Heal:
                        builder.Append(instruction.HitCount > 1
                            ? $"Restore {instruction.Value} x{instruction.HitCount}"
                            : $"Restore {instruction.Value}");
                        break;
                    case BattleEffectType.Shield:
                        builder.Append(instruction.HitCount > 1
                            ? $"Shield {instruction.Value} x{instruction.HitCount}"
                            : $"Shield {instruction.Value}");
                        break;
                    case BattleEffectType.ApplyStatus:
                        builder.Append($"{instruction.StatusId} {instruction.Duration}T");
                        break;
                    default:
                        builder.Append(instruction.Type.ToString());
                        break;
                }
            }

            return builder.ToString();
        }

        private string BuildStatusList(BattleUnit unit)
        {
            if (controller == null || controller.Simulation == null || unit == null || controller.Simulation.StatusController == null)
            {
                return string.Empty;
            }

            System.Collections.Generic.IReadOnlyList<BattleStatusEffectInstance> effects = controller.Simulation.StatusController.GetEffects(unit);
            if (effects.Count == 0)
            {
                return string.Empty;
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder("Status: ");
            for (int i = 0; i < effects.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append("  |  ");
                }

                BattleStatusEffectInstance effect = effects[i];
                builder.Append(effect.Definition.DisplayName);
                builder.Append(" ");
                builder.Append(effect.RemainingDuration);
                builder.Append("T");
                if (effect.StackCount > 1)
                {
                    builder.Append(" x");
                    builder.Append(effect.StackCount);
                }
            }

            return builder.ToString();
        }

        private void ClearInteractionState()
        {
            pressedCardIndex = -1;
            draggingCardIndex = -1;
            activeDropTarget = DragTarget.None;
        }

        private string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return "No intent";
            }

            if (text.Length <= maxLength)
            {
                return text;
            }

            return text.Substring(0, maxLength - 3) + "...";
        }

        private Color GetElementColor(WorkshopElementAttribute element)
        {
            switch (element)
            {
                case WorkshopElementAttribute.Fire:
                    return new Color(0.94f, 0.41f, 0.22f);
                case WorkshopElementAttribute.Water:
                    return new Color(0.28f, 0.66f, 0.98f);
                case WorkshopElementAttribute.Wind:
                    return new Color(0.55f, 0.88f, 0.82f);
                case WorkshopElementAttribute.Earth:
                    return new Color(0.74f, 0.58f, 0.36f);
                case WorkshopElementAttribute.Ice:
                    return new Color(0.76f, 0.94f, 1f);
                case WorkshopElementAttribute.Thunder:
                    return new Color(0.98f, 0.88f, 0.34f);
                case WorkshopElementAttribute.Light:
                    return new Color(0.98f, 0.96f, 0.72f);
                case WorkshopElementAttribute.Dark:
                    return new Color(0.56f, 0.51f, 0.77f);
                default:
                    return new Color(0.67f, 0.71f, 0.78f);
            }
        }

        private Color GetIntentColor(BattleActionType actionType)
        {
            switch (actionType)
            {
                case BattleActionType.Attack:
                    return WarningAccent;
                case BattleActionType.Heal:
                    return new Color(0.36f, 0.82f, 0.62f, 1f);
                case BattleActionType.Defend:
                    return ShieldAccent;
                case BattleActionType.Special:
                    return new Color(0.76f, 0.55f, 0.92f, 1f);
                default:
                    return HudMuted;
            }
        }

        private string GetIntentBadge(BattleActionType actionType)
        {
            switch (actionType)
            {
                case BattleActionType.Attack:
                    return "ATTACK";
                case BattleActionType.Heal:
                    return "HEAL";
                case BattleActionType.Defend:
                    return "DEFEND";
                case BattleActionType.Special:
                    return "SPECIAL";
                default:
                    return "IDLE";
            }
        }

        private void SyncTransientState()
        {
            if (controller != null && controller.Simulation != null)
            {
                if (lastObservedActionPoints != controller.Simulation.ActionPoints)
                {
                    if (lastObservedActionPoints >= 0)
                    {
                        actionPointFlash = 1f;
                    }

                    lastObservedActionPoints = controller.Simulation.ActionPoints;
                }
            }
            else
            {
                lastObservedActionPoints = -1;
            }

            if (controller != null && controller.CurrentResult != lastShownResult)
            {
                lastShownResult = controller.CurrentResult;
                resultOverlayShownAt = controller.CurrentResult != null ? Time.unscaledTime : -1f;
            }

            actionPointFlash = Mathf.MoveTowards(actionPointFlash, 0f, Time.unscaledDeltaTime * 2.5f);
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
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = HudText }
            };

            sectionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = HudText }
            };

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true,
                normal = { textColor = new Color(0.92f, 0.93f, 0.95f) }
            };

            centeredBodyStyle = new GUIStyle(bodyStyle)
            {
                alignment = TextAnchor.MiddleCenter
            };

            mutedStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                wordWrap = true,
                normal = { textColor = HudMuted }
            };

            centeredMutedStyle = new GUIStyle(mutedStyle)
            {
                alignment = TextAnchor.MiddleCenter
            };

            targetHintStyle = new GUIStyle(centeredMutedStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = HudText }
            };

            statStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = HudText }
            };

            chipStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = HudText }
            };

            darkChipStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.14f, 0.1f, 0.04f) }
            };

            cardMetaStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = new Color(0.78f, 0.82f, 0.88f) }
            };

            cardSummaryStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft,
                wordWrap = true,
                normal = { textColor = HudText }
            };

            cardTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft,
                wordWrap = true,
                clipping = TextClipping.Clip,
                normal = { textColor = HudText }
            };

            pillStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.08f, 0.1f, 0.14f) }
            };

            resultStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal = { textColor = HudText }
            };
        }

        private void DrawPanelFrame(Rect rect, Color accent, float alpha)
        {
            DrawRect(rect, new Color(HudBackground.r, HudBackground.g, HudBackground.b, alpha));
            DrawOutline(rect, new Color(accent.r, accent.g, accent.b, 0.7f));
            DrawRect(new Rect(rect.x, rect.y, rect.width, 3f), accent);
            DrawRect(new Rect(rect.x + 6f, rect.y + 6f, rect.width - 12f, rect.height - 12f), new Color(1f, 1f, 1f, 0.012f));
        }

        private bool DrawThemedButton(Rect rect, string label, Color accent, string interactionId, bool enabled, bool playClickSound = true)
        {
            bool isHover = enabled && Event.current != null && rect.Contains(Event.current.mousePosition);
            if (isHover)
            {
                AudioManager.ReportUIHover($"battle:{interactionId}");
            }

            Color fillColor = enabled
                ? new Color(HudPanel.r, HudPanel.g, HudPanel.b, isHover ? 0.98f : 0.92f)
                : new Color(0.12f, 0.13f, 0.16f, 0.72f);
            Color outlineColor = enabled
                ? new Color(accent.r, accent.g, accent.b, isHover ? 0.96f : 0.72f)
                : new Color(HudStroke.r, HudStroke.g, HudStroke.b, 0.42f);
            Color textColor = enabled
                ? HudText
                : new Color(HudMuted.r, HudMuted.g, HudMuted.b, 0.72f);
            GUIStyle labelStyle = new GUIStyle(chipStyle)
            {
                normal = { textColor = textColor }
            };

            DrawPanelWithShadow(rect, fillColor, outlineColor, new Color(0f, 0f, 0f, 0.18f));
            DrawRect(new Rect(rect.x, rect.y, rect.width, 3f), new Color(accent.r, accent.g, accent.b, enabled ? (isHover ? 1f : 0.88f) : 0.26f));
            GUI.Label(rect, label, labelStyle);

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

        private void DrawRect(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, whiteTexture);
            GUI.color = previous;
        }

        private void DrawOutline(Rect rect, Color color)
        {
            DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), color);
            DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), color);
            DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), color);
            DrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), color);
        }

        private void DrawPanelWithShadow(Rect rect, Color fillColor, Color outlineColor)
        {
            DrawPanelWithShadow(rect, fillColor, outlineColor, new Color(0f, 0f, 0f, 0.16f));
        }

        private void DrawPanelWithShadow(Rect rect, Color fillColor, Color outlineColor, Color shadowColor)
        {
            DrawRect(new Rect(rect.x + 3f, rect.y + 4f, rect.width, rect.height), shadowColor);
            DrawRect(rect, fillColor);
            DrawOutline(rect, outlineColor);
        }

        private void DrawProgressBar(Rect rect, float ratio, Color fillColor, Color backgroundColor)
        {
            DrawRect(rect, backgroundColor);
            DrawOutline(rect, new Color(HudStroke.r, HudStroke.g, HudStroke.b, 0.72f));
            if (ratio <= 0f)
            {
                return;
            }

            DrawRect(new Rect(rect.x + 1f, rect.y + 1f, (rect.width - 2f) * ratio, rect.height - 2f), fillColor);
        }
    }
}
