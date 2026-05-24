using ArcaneAtelier.Audio;
using ArcaneAtelier.Workshop;
using System.Collections.Generic;
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
        private const float FinalVictoryIntroDuration = 2.2f;
        private const float FinalVictorySummaryStagger = 0.14f;

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
        private static readonly Color WorkshopGold = new Color(0.88f, 0.72f, 0.3f, 1f);
        private static readonly Color WorkshopBlue = new Color(0.42f, 0.72f, 0.94f, 1f);
        private static readonly Color WorkshopViolet = new Color(0.72f, 0.5f, 0.96f, 1f);

        private BattleSceneController controller;
        private Texture2D whiteTexture;
        private Sprite uiPanelMainSprite;
        private Sprite uiPaletteDockSprite;
        private Sprite uiButtonSprite;
        private Sprite uiButtonSmallSprite;
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
        private GUIStyle turnInfoStyle;
        private GUIStyle summaryTitleStyle;
        private GUIStyle summaryHeadingStyle;
        private GUIStyle summaryBodyStyle;
        private GUIStyle summaryMutedStyle;
        private GUIStyle summaryStatValueStyle;
        private GUIStyle summaryStatLabelStyle;
        private Vector2 handScroll;
        private Vector2 runSummaryScroll;
        private int selectedCardIndex = -1;
        private int pressedCardIndex = -1;
        private int draggingCardIndex = -1;
        private Vector2 pressMousePosition;
        private Vector2 dragMousePosition;
        private Rect playerTargetScreenRect;
        private Rect bossTargetScreenRect;
        private Rect drawPileScreenRect;
        private Rect discardPileScreenRect;
        private DragTarget activeDropTarget = DragTarget.None;
        private float actionPointFlash;
        private float drawPilePulse;
        private float discardPilePulse;
        private float shufflePulse;
        private int lastObservedActionPoints = -1;
        private int lastObservedDrawPileCount = -1;
        private int lastObservedDiscardPileCount = -1;
        private BattleResult lastShownResult;
        private float resultOverlayShownAt = -1f;
        private FinalVictorySequencePhase finalVictorySequencePhase = FinalVictorySequencePhase.None;
        private float finalVictoryPhaseStartedAt = -1f;
        private float finalVictorySummaryShownAt = -1f;
        private float handAnimationStartedAt = -1f;
        private float shuffleNoticeUntil = -1f;
        private string[] lastHandCardIds = System.Array.Empty<string>();
        private WorkshopBattleCardEntry[] lastHandCards = System.Array.Empty<WorkshopBattleCardEntry>();
        private int animatedHandStartIndex = int.MaxValue;
        private int animatedHandEndIndex = -1;
        private int pendingDiscardSourceHandCount = -1;
        private readonly List<PendingDiscardRequest> pendingDiscardRequests = new List<PendingDiscardRequest>();
        private readonly List<int> pendingDrawIndices = new List<int>();
        private readonly List<TransientCardAnimation> transientCardAnimations = new List<TransientCardAnimation>();
        private readonly Dictionary<int, float> handRevealTimes = new Dictionary<int, float>();
        private string pendingDraggedDiscardCardId = string.Empty;
        private Rect pendingDraggedDiscardStartRect = Rect.zero;
        private string hoveredMetaTitle = string.Empty;
        private string hoveredMetaBody = string.Empty;
        private Color hoveredMetaAccent = Color.white;

        private enum DragTarget
        {
            None,
            Player,
            Boss
        }

        private enum FinalVictorySequencePhase
        {
            None,
            Intro,
            Summary
        }

        private sealed class PendingDiscardRequest
        {
            public WorkshopBattleCardEntry Card;
            public int SourceIndex;
            public bool HasStartRect;
            public Rect StartRect;
        }

        private sealed class TransientCardAnimation
        {
            public WorkshopBattleCardEntry Card;
            public Rect StartRect;
            public Rect EndRect;
            public float StartedAt;
            public float Duration;
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
            ClearMetaHover();
            SyncTransientState();
            UpdateTargetRects();
            ProcessInput(Event.current);
            DrawBackdrop();

            Rect topBarRect = new Rect(Margin, Margin, Screen.width - Margin * 2f, TopBarHeight);
            Rect handRect = new Rect(Margin, Screen.height - BottomPanelHeight - Margin, Screen.width - Margin * 2f, BottomPanelHeight);

            DrawWorldTargetHighlights();
            DrawTopBar(topBarRect);
            DrawLegacySigilStrip(new Rect(Margin + 8f, topBarRect.yMax + 6f, Mathf.Min(620f, Screen.width - Margin * 2f - 16f), 38f));
            DrawHandPanel(handRect);
            DrawTransientCardAnimations();
            DrawDraggedCard();
            DrawMetaHoverTooltip();

            if (controller.CurrentResult != null)
            {
                if (controller.ShouldShowRunSummaryPage)
                {
                    DrawFinalVictorySequence(new Rect(34f, 26f, Screen.width - 68f, Screen.height - 52f));
                }
                else
                {
                    DrawResultOverlay(new Rect(Screen.width * 0.5f - 270f, Screen.height * 0.5f - 168f, 540f, 336f));
                }
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
                    WorkshopBattleCardEntry draggedCard = controller.Simulation.Deck.Hand[resolvedIndex];
                    bool played = dropTarget != DragTarget.None && controller.TryPlayCardFromHud(resolvedIndex);
                    if (played)
                    {
                        pendingDraggedDiscardCardId = draggedCard.CardId ?? string.Empty;
                        pendingDraggedDiscardStartRect = new Rect(
                            dragMousePosition.x - CardWidth * 0.5f,
                            dragMousePosition.y - CardHeight * 0.5f,
                            CardWidth,
                            CardHeight);
                    }
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
            DrawPaletteDockFrame(rect, ApAccent, 0.94f);
            GUI.BeginGroup(rect);

            float sideWidth = Mathf.Clamp((rect.width - 400f) * 0.5f, 300f, 400f);
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

            //DrawPaletteCardFrame(rect, accent, 0.98f, 0.08f);

            GUIStyle nameStyle = new GUIStyle(sectionStyle)
            {
                alignment = alignRight ? TextAnchor.UpperRight : TextAnchor.UpperLeft
            };
            GUIStyle valueStyle = new GUIStyle(bodyStyle)
            {
                alignment = alignRight ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft
            };

            float contentX = rect.x + 20f;
            float contentWidth = rect.width - 35f;
            float labelWidth = Mathf.Max(64f, contentWidth * 0.26f);
            string elementText = element == WorkshopElementAttribute.None ? "Neutral" : element.ToString();

            GUI.Label(new Rect(contentX, rect.y + 15f, contentWidth, 18f), displayName, nameStyle);
            GUI.Label(new Rect(contentX, rect.y + 35f, contentWidth, 14f), elementText, valueStyle);
            GUI.Label(new Rect(contentX, rect.y + 57f, labelWidth, 14f), $"HP {currentHealth}/{maxHealth}", valueStyle);

            float barWidth = contentWidth - labelWidth - 18f;
            Rect healthBarRect = new Rect(contentX + labelWidth + 8f, rect.y + 59f, barWidth, 10f);
            Rect shieldBarRect = new Rect(contentX + labelWidth + 8f, rect.y + 83f, barWidth, 8f);
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

        private void DrawLegacySigilStrip(Rect rect)
        {
            DrawPaletteCardFrame(rect, WorkshopGold, 0.96f, 0.09f);
            DrawRect(new Rect(rect.x, rect.y, rect.width, 2f), new Color(WorkshopGold.r, WorkshopGold.g, WorkshopGold.b, 0.82f));
            DrawRect(new Rect(rect.x + rect.width * 0.5f - 0.5f, rect.y + 6f, 1f, rect.height - 12f), new Color(HudStroke.r, HudStroke.g, HudStroke.b, 0.45f));

            float leftX = rect.x + 10f;
            float rightX = rect.x + rect.width * 0.5f + 10f;
            float y = rect.y + 10f;
            float segmentHeight = 18f;
            float gap = 5f;
            float maxLeftWidth = rect.width * 0.5f - 22f;
            float maxRightWidth = rect.width * 0.5f - 20f;
            int boonTokenBonus = Mathf.Max(0, MetaProgressionStore.GetVictoryTokenBonus() - (MetaProgressionStore.ActiveOmen != null ? MetaProgressionStore.ActiveOmen.VictoryTokenBonus : 0));
            LegacyOmenView omen = MetaProgressionStore.ActiveOmen;
            float segmentWidth = Mathf.Clamp(
                Mathf.Min(
                    (maxLeftWidth - gap * 5f) / 6f,
                    (maxRightWidth - gap * (omen != null ? 3f : 2f)) / (omen != null ? 4.15f : 3f)),
                22f,
                32f);
            float omenWidth = segmentWidth * 1.15f;

            float usedLeft = 0f;
            usedLeft += DrawMetaSegment(new Rect(leftX + usedLeft, y, segmentWidth, segmentHeight), MetaHudIconKind.Prep, MetaProgressionStore.GetPreparationTickBonus() / 60f, WorkshopGold, "Kindled Start", $"+{MetaProgressionStore.GetPreparationTickBonus()} prep ticks at run start.", "meta_prep") + gap;
            usedLeft += DrawMetaSegment(new Rect(leftX + usedLeft, y, segmentWidth, segmentHeight), MetaHudIconKind.Shield, MetaProgressionStore.GetOpeningShieldBonus() / 12f, WorkshopBlue, "Warden Reserve", $"+{MetaProgressionStore.GetOpeningShieldBonus()} opening shield each battle.", "meta_ward") + gap;
            usedLeft += DrawMetaSegment(new Rect(leftX + usedLeft, y, segmentWidth, segmentHeight), MetaHudIconKind.Tokens, MetaProgressionStore.GetStartingRunTokens() / 55f, WorkshopViolet, "Ember Float", $"+{MetaProgressionStore.GetStartingRunTokens()} Tokens before the workshop store opens.", "meta_tokens") + gap;
            usedLeft += DrawMetaSegment(new Rect(leftX + usedLeft, y, segmentWidth, segmentHeight), MetaHudIconKind.Vitality, MetaProgressionStore.GetPlayerMaxHealthBonus() / 12f, new Color(0.72f, 0.94f, 0.52f, 1f), "Vital Script", $"+{MetaProgressionStore.GetPlayerMaxHealthBonus()} max HP in battle.", "meta_vital") + gap;
            usedLeft += DrawMetaSegment(new Rect(leftX + usedLeft, y, segmentWidth, segmentHeight), MetaHudIconKind.Healing, MetaProgressionStore.GetVictoryHealBonus() / 9f, new Color(0.46f, 0.9f, 0.7f, 1f), "Afterglow Seal", $"+{MetaProgressionStore.GetVictoryHealBonus()} healing after each win.", "meta_heal") + gap;
            usedLeft += DrawMetaSegment(new Rect(leftX + usedLeft, y, segmentWidth, segmentHeight), MetaHudIconKind.Bounty, boonTokenBonus / 28f, new Color(0.95f, 0.56f, 0.28f, 1f), "Bounty Seal", $"+{boonTokenBonus} Tokens added to victory payouts.", "meta_bounty");
            DrawGhostMetaSlots(new Rect(leftX + Mathf.Min(usedLeft + gap, maxLeftWidth - (segmentWidth * 2f + gap)), y, maxLeftWidth - Mathf.Min(usedLeft + gap, maxLeftWidth), segmentHeight), segmentWidth, gap);

            int pressureHealth = Mathf.Max(0, Mathf.RoundToInt((MetaProgressionStore.GetEnemyHealthScaleMultiplier() - 1f) * 100f));
            int pressureDamage = Mathf.Max(0, Mathf.RoundToInt((MetaProgressionStore.GetEnemyDamageScaleMultiplier() - 1f) * 100f));
            int pressureWard = MetaProgressionStore.GetEnemyStartingShieldBonus();

            float usedRight = 0f;
            usedRight += DrawMetaSegment(new Rect(rightX + usedRight, y, segmentWidth, segmentHeight), MetaHudIconKind.BreachPressure, pressureHealth / 120f, WarningAccent, "Breach Vitality", $"+{pressureHealth}% enemy health this cycle.", "pressure_hp") + gap;
            usedRight += DrawMetaSegment(new Rect(rightX + usedRight, y, segmentWidth, segmentHeight), MetaHudIconKind.BreachPressure, pressureDamage / 90f, new Color(0.94f, 0.5f, 0.22f, 1f), "Breach Force", $"+{pressureDamage}% enemy damage this cycle.", "pressure_dmg") + gap;
            usedRight += DrawMetaSegment(new Rect(rightX + usedRight, y, segmentWidth, segmentHeight), MetaHudIconKind.BreachPressure, pressureWard / 12f, ShieldAccent, "Breach Ward", $"+{pressureWard} starting shield on enemies.", "pressure_ward") + gap;
            if (omen != null)
            {
                usedRight += DrawMetaSegment(new Rect(rightX + usedRight, y, omenWidth, segmentHeight), MetaHudIconKind.Omen, 1f, WorkshopViolet, omen.DisplayName, omen.Description, "pressure_omen");
            }

            DrawGhostMetaSlots(new Rect(rightX + Mathf.Min(usedRight + gap, maxRightWidth - (segmentWidth * 2f + gap)), y, maxRightWidth - Mathf.Min(usedRight + gap, maxRightWidth), segmentHeight), segmentWidth, gap);
        }

        private float DrawMetaSegment(Rect rect, MetaHudIconKind iconKind, float normalizedValue, Color accent, string title, string body, string hoverId)
        {
            float clamped = Mathf.Clamp01(normalizedValue);
            DrawRect(rect, new Color(0.05f, 0.08f, 0.12f, 0.94f));
            DrawOutline(rect, new Color(accent.r, accent.g, accent.b, 0.42f));
            DrawRect(new Rect(rect.x + 1f, rect.y + 1f, rect.width - 2f, rect.height - 2f), new Color(accent.r, accent.g, accent.b, Mathf.Lerp(0.06f, 0.16f, clamped)));

            float iconSize = Mathf.Min(rect.width - 8f, rect.height - 8f);
            Rect iconRect = new Rect(rect.x + (rect.width - iconSize) * 0.5f, rect.y + 2f, iconSize, iconSize);
            DrawMetaIcon(iconRect, iconKind);

            Rect meterRect = new Rect(rect.x + 4f, rect.yMax - 4f, rect.width - 8f, 2f);
            DrawRect(meterRect, new Color(0.1f, 0.13f, 0.18f, 0.92f));
            if (clamped > 0.001f)
            {
                DrawRect(new Rect(meterRect.x, meterRect.y, Mathf.Max(2f, meterRect.width * clamped), meterRect.height), new Color(accent.r, accent.g, accent.b, 0.96f));
            }

            TryRegisterMetaHover(rect, title, body, accent, hoverId);
            return rect.width;
        }

        private void DrawMetaIcon(Rect rect, MetaHudIconKind kind)
        {
            Texture2D atlas = MetaHudIconAtlas.GetTexture();
            if (atlas == null)
            {
                return;
            }

            Color previous = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTextureWithTexCoords(rect, atlas, MetaHudIconAtlas.GetUv(kind), true);
            GUI.color = previous;
        }

        private void DrawGhostMetaSlots(Rect rect, float segmentWidth, float gap)
        {
            if (rect.width < segmentWidth)
            {
                return;
            }

            int count = Mathf.FloorToInt((rect.width + gap) / (segmentWidth + gap));
            for (int i = 0; i < count; i++)
            {
                Rect slotRect = new Rect(rect.x + i * (segmentWidth + gap), rect.y, segmentWidth, rect.height);
                DrawRect(slotRect, new Color(0.06f, 0.08f, 0.11f, 0.48f));
                DrawOutline(slotRect, new Color(HudStroke.r, HudStroke.g, HudStroke.b, 0.22f));
            }
        }

        private void TryRegisterMetaHover(Rect rect, string title, string body, Color accent, string hoverId)
        {
            Event current = Event.current;
            if (current == null || !rect.Contains(current.mousePosition))
            {
                return;
            }

            hoveredMetaTitle = title;
            hoveredMetaBody = body;
            hoveredMetaAccent = accent;
            AudioManager.ReportUIHover($"battle:{hoverId}");
        }

        private void DrawMetaHoverTooltip()
        {
            if (string.IsNullOrWhiteSpace(hoveredMetaTitle))
            {
                return;
            }

            Vector2 mouse = Event.current != null ? Event.current.mousePosition : Vector2.zero;
            float width = 312f;
            float bodyHeight = Mathf.Max(20f, bodyStyle.CalcHeight(new GUIContent(hoveredMetaBody), width - 28f));
            Rect rect = PositionUiTooltip(mouse, width, 44f + bodyHeight);
            DrawPanelFrame(rect, hoveredMetaAccent, 0.98f);
            GUI.BeginGroup(rect);
            GUI.Label(new Rect(14f, 12f, rect.width - 28f, 18f), hoveredMetaTitle, sectionStyle);
            GUI.Label(new Rect(14f, 34f, rect.width - 28f, bodyHeight), hoveredMetaBody, bodyStyle);
            GUI.EndGroup();
        }

        private void ClearMetaHover()
        {
            hoveredMetaTitle = string.Empty;
            hoveredMetaBody = string.Empty;
            hoveredMetaAccent = Color.white;
        }

        private void DrawCenterBattleStrip(Rect rect)
        {
            //DrawPaletteCardFrame(rect, WorkshopBlue, 0.97f, 0.07f);

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

            GUI.Label(new Rect(rect.x, rect.y + 15f, rect.width, 14f), $"Encounter {controller.CurrentEncounterNumber}/{controller.TotalEncounterCount}", turnInfoStyle);
            GUI.Label(
                new Rect(rect.x, rect.y + 31f, rect.width, 14f),
                bossTurnPending ? "Enemy action incoming" : $"Turn {controller.Simulation.TurnsElapsed + 1}",
                turnInfoStyle);
            DrawTag(new Rect(rect.x + rect.width * 0.5f - 56f, rect.y + 53f, 112f, 20f), intentBadge, new Color(intentAccent.r, intentAccent.g, intentAccent.b, 0.88f));
            GUI.Label(
                new Rect(rect.x + 18f, rect.y + 79f, rect.width - 36f, 30f),
                bossTurnPending ? $"Preparing: {intent}" : intent,
                turnInfoStyle);

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
            DrawPaletteDockFrame(rect, WorkshopGold, 0.96f);
            GUI.BeginGroup(rect);

            int handCount = controller.Simulation.Deck.HandCount;
            Rect headerRect = new Rect(14f, 12f, rect.width - 28f, 34f);
            Rect contentRect = new Rect(14f, 48f, rect.width - 28f, rect.height - 62f);
            float handCardOffsetX = 30f;

            //DrawPaletteCardFrame(headerRect, WorkshopGold, 0.98f, 0.08f);
            GUI.Label(new Rect(headerRect.x + 32f, headerRect.y + 12f, 140f, 18f), "Hand", sectionStyle);

            float pileWidth = 72f;
            float pileGap = 10f;
            float discardX = headerRect.x + headerRect.width - pileWidth - 50f;
            float drawX = discardX - pileGap - pileWidth;
            DrawCardPileMini(new Rect(drawX, headerRect.y + 10f, pileWidth, 50f), "Deck", controller.Simulation.Deck.DrawPileCount, WorkshopBlue, drawPilePulse, Time.unscaledTime < shuffleNoticeUntil);
            DrawCardPileMini(new Rect(discardX, headerRect.y + 10f, pileWidth, 50f), "Discard", controller.Simulation.Deck.DiscardPileCount, WorkshopViolet, discardPilePulse, false);
            drawPileScreenRect = OffsetRect(new Rect(drawX, headerRect.y + 10f, pileWidth, 50f), rect.position);
            discardPileScreenRect = OffsetRect(new Rect(discardX, headerRect.y + 10f, pileWidth, 50f), rect.position);
            GUI.Label(new Rect(drawX - 118f, headerRect.y + 8f, 104f, 16f), $"Hand {handCount}", new GUIStyle(mutedStyle) { alignment = TextAnchor.MiddleRight });

            //DrawPaletteCardFrame(contentRect, WorkshopBlue, 0.97f, 0.05f);
            DrawRect(new Rect(contentRect.x + 10f, contentRect.y + 8f, contentRect.width - 20f, 1f), new Color(1f, 1f, 1f, 0.03f));
            DrawRect(new Rect(contentRect.x + 10f, contentRect.yMax - 9f, contentRect.width - 20f, 1f), new Color(0f, 0f, 0f, 0.18f));

            float viewWidth = Mathf.Max(contentRect.width - 18f, handCount * (CardWidth + CardSpacing) + CardSpacing);
            handScroll = GUI.BeginScrollView(contentRect, handScroll, new Rect(0f, 0f, viewWidth, CardHeight + 20f), true, false);
            for (int i = 0; i < handCount; i++)
            {
                if (IsHandCardHidden(i, Time.unscaledTime))
                {
                    continue;
                }

                Rect cardRect = new Rect(handCardOffsetX + CardSpacing + i * (CardWidth + CardSpacing), 10f, CardWidth, CardHeight);
                DrawCard(cardRect, controller.Simulation.Deck.Hand[i], i);
            }

            GUI.EndScrollView();
            ResolvePendingDrawAnimations(rect, handCount);
            ResolvePendingDiscardAnimations(rect, handCount);
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

            float lift = isPressed ? 2f : isSelected ? 5f : 0f;
            Rect animatedRect = GetAnimatedHandCardRect(new Rect(rect.x, rect.y - lift, rect.width, rect.height), index, out float alpha);
            Color previous = GUI.color;
            GUI.color = new Color(previous.r, previous.g, previous.b, previous.a * alpha);
            DrawCardVisual(animatedRect, card, index, outline, accent, canAfford, false);
            GUI.color = previous;
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
            if (index >= 0)
            {
                DrawRect(new Rect(rect.x + 6f, rect.y + 8f, 22f, 16f), new Color(0.05f, 0.07f, 0.1f, 0.98f));
                DrawOutline(new Rect(rect.x + 6f, rect.y + 8f, 22f, 16f), new Color(accent.r, accent.g, accent.b, 0.74f));
                GUI.Label(new Rect(rect.x + 6f, rect.y + 8f, 22f, 16f), index < 9 ? (index + 1).ToString() : "-", pillStyle);
            }
            Rect iconRect = new Rect(rect.x + 12f, rect.y + 24f, 30f, 30f);
            DrawCardIconSlot(iconRect, card, accent);

            DrawTag(new Rect(rect.x + rect.width - 58f, rect.y + 12f, 46f, 20f), $"{apCost} AP", new Color(ApAccent.r, ApAccent.g, ApAccent.b, 0.96f), darkChipStyle);
            float headerLeft = rect.x + 56f;
            float headerRight = rect.x + rect.width - 64f;
            float headerWidth = Mathf.Max(40f, headerRight - headerLeft);
            float targetWidth = 42f;
            float tierWidth = Mathf.Max(52f, headerWidth - targetWidth - 6f);
            GUI.Label(new Rect(headerLeft, rect.y + 10f, headerWidth, 20f), card.DisplayName, cardTitleStyle);
            DrawTag(new Rect(headerLeft, rect.y + 32f, tierWidth, 16f), BuildTierLabel(card.Tier), new Color(accent.r, accent.g, accent.b, 0.2f), centeredMutedStyle);
            DrawTag(new Rect(headerLeft + tierWidth + 6f, rect.y + 32f, targetWidth, 16f), BuildTargetLabel(card), new Color(0.16f, 0.18f, 0.24f, 0.92f), centeredMutedStyle);
            DrawRect(new Rect(rect.x + 12f, rect.y + 56f, rect.width - 24f, 1f), new Color(HudStroke.r, HudStroke.g, HudStroke.b, 0.8f));
            GUI.Label(new Rect(rect.x + 12f, rect.y + 64f, rect.width - 24f, 40f), BuildCardSummary(card), cardSummaryStyle);
            GUI.Label(new Rect(rect.x + 12f, rect.y + 108f, rect.width - 24f, 14f), BuildCardMeta(card), cardMetaStyle);
            DrawTag(new Rect(rect.x + 12f, rect.y + 130f, 100f, 18f), BuildRoleLabel(card.Role), new Color(accent.r, accent.g, accent.b, 0.26f));
            GUI.Label(new Rect(rect.x + 12f, rect.y + 152f, rect.width - 24f, 18f), canAfford ? BuildDragHint(card) : "Insufficient AP", centeredMutedStyle);

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

        private static Rect PositionUiTooltip(Vector2 mouse, float tooltipWidth, float tooltipHeight)
        {
            float x = mouse.x + 16f;
            float y = mouse.y + 16f;
            if (x + tooltipWidth > Screen.width - 12f)
            {
                x = mouse.x - tooltipWidth - 16f;
            }

            if (y + tooltipHeight > Screen.height - 12f)
            {
                y = mouse.y - tooltipHeight - 16f;
            }

            x = Mathf.Clamp(x, 12f, Screen.width - tooltipWidth - 12f);
            y = Mathf.Clamp(y, 12f, Screen.height - tooltipHeight - 12f);
            return new Rect(x, y, tooltipWidth, tooltipHeight);
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
            Rect showcaseRect = new Rect(animatedRect.width - 82f, 6f, 48f, 48f);
            float headerTextWidth = animatedRect.width - 124f;
            GUI.Label(new Rect(28f, 14f, headerTextWidth, 30f), outcome, resultStyle);
            GUI.Label(new Rect(28f, 44f, headerTextWidth, 18f), result.BossDisplayName, sectionStyle);
            GUI.Label(new Rect(28f, 68f, headerTextWidth, 16f), result.ResultType == BattleResultType.Victory ? $"Run cleared. {result.EncountersCleared} encounters completed." : $"Run failed after clearing {result.EncountersCleared} encounter(s).", mutedStyle);
            DrawIllustratedSummaryCard(
                showcaseRect,
                string.Empty,
                string.Empty,
                string.Empty,
                accent,
                GetRunSummaryBossSprite(),
                GetRunSummaryBossFallbackIcon(),
                compact: true);

            DrawMiniStat(new Rect(28f, 98f, 88f, 52f), $"{result.TotalDamageDealt}", "Damage");
            DrawMiniStat(new Rect(126f, 98f, 88f, 52f), $"{result.TotalHealingDone}", "Healing");
            DrawMiniStat(new Rect(224f, 98f, 88f, 52f), $"{result.TotalShieldGained}", "Shield");
            DrawMiniStat(new Rect(322f, 98f, 88f, 52f), $"{result.CardsPlayed}", "Cards");
            DrawMiniStat(new Rect(420f, 98f, 88f, 52f), $"+{result.TokensEarned}", "Tokens");

            DrawRect(new Rect(28f, 168f, animatedRect.width - 56f, 1f), new Color(HudStroke.r, HudStroke.g, HudStroke.b, 0.72f));
            GUI.Label(new Rect(28f, 182f, animatedRect.width - 56f, 18f), $"Final encounter: {result.FinalEncounterId}", bodyStyle);
            GUI.Label(new Rect(28f, 204f, animatedRect.width - 56f, 18f), $"Turns elapsed: {result.TurnsElapsed}", bodyStyle);
            string tokenPayoutText = result.ResultType == BattleResultType.Victory
                ? $"Token payout ready: +{result.TokensEarned}"
                : "Token payout: none";
            GUI.Label(new Rect(28f, 226f, animatedRect.width - 56f, 18f), tokenPayoutText, mutedStyle);

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
                    controller.ReturnToWorkshop();
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

        private void DrawFinalVictorySequence(Rect rect)
        {
            UpdateFinalVictorySequence();
            if (finalVictorySequencePhase == FinalVictorySequencePhase.Intro)
            {
                DrawFinalVictoryIntro(rect);
                return;
            }

            DrawRunSummaryPage(rect);
        }

        private void DrawFinalVictoryIntro(Rect rect)
        {
            BattleResult result = controller.CurrentResult;
            float elapsed = finalVictoryPhaseStartedAt >= 0f ? Time.unscaledTime - finalVictoryPhaseStartedAt : 0f;
            float fadeIn = Mathf.Clamp01(elapsed / 0.45f);
            float fadeOut = Mathf.Clamp01((FinalVictoryIntroDuration - elapsed) / 0.45f);
            float visibility = Mathf.Min(fadeIn, fadeOut);
            float easedVisibility = visibility * visibility * (3f - 2f * visibility);
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2.6f);
            float glowAlpha = 0.08f + pulse * 0.06f;
            float titleScale = Mathf.Lerp(0.96f, 1f, easedVisibility);
            float sweep = Mathf.Clamp01(elapsed / 1.25f);
            GUIStyle centeredResultStyle = new GUIStyle(resultStyle)
            {
                alignment = TextAnchor.MiddleCenter
            };

            Rect cardRect = new Rect(rect.x + rect.width * 0.5f - 320f, rect.y + rect.height * 0.5f - 136f, 640f, 272f);
            Rect scaledCardRect = new Rect(
                cardRect.center.x - cardRect.width * titleScale * 0.5f,
                cardRect.center.y - cardRect.height * titleScale * 0.5f,
                cardRect.width * titleScale,
                cardRect.height * titleScale);

            Color previousColor = GUI.color;
            GUI.color = new Color(previousColor.r, previousColor.g, previousColor.b, easedVisibility);

            DrawRect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.76f));
            DrawRect(new Rect(0f, 0f, Screen.width, Screen.height * 0.34f), new Color(WorkshopBlue.r, WorkshopBlue.g, WorkshopBlue.b, 0.05f + glowAlpha));
            DrawRect(new Rect(0f, Screen.height * 0.58f, Screen.width, Screen.height * 0.42f), new Color(WorkshopViolet.r, WorkshopViolet.g, WorkshopViolet.b, 0.04f + glowAlpha * 0.7f));
            DrawPanelFrame(scaledCardRect, WorkshopGold, 0.99f);
            DrawRect(new Rect(scaledCardRect.x, scaledCardRect.y, scaledCardRect.width, 60f), new Color(WorkshopGold.r, WorkshopGold.g, WorkshopGold.b, 0.14f));
            DrawRect(
                new Rect(
                    scaledCardRect.x + 24f,
                    scaledCardRect.y + 86f,
                    (scaledCardRect.width - 48f) * sweep,
                    3f),
                new Color(WorkshopBlue.r, WorkshopBlue.g, WorkshopBlue.b, 0.92f));

            GUI.Label(new Rect(scaledCardRect.x, scaledCardRect.y + 34f, scaledCardRect.width, 18f), "CONGRATULATIONS", centeredMutedStyle);
            GUI.Label(new Rect(scaledCardRect.x, scaledCardRect.y + 86f, scaledCardRect.width, 44f), "ATELIER SECURED", centeredResultStyle);
            GUI.Label(new Rect(scaledCardRect.x, scaledCardRect.y + 144f, scaledCardRect.width, 20f), result != null ? result.BossDisplayName : "Final Boss", centeredBodyStyle);
            GUI.Label(new Rect(scaledCardRect.x, scaledCardRect.y + 174f, scaledCardRect.width, 18f), "The final breach is sealed", centeredMutedStyle);

            GUI.color = previousColor;
        }

        private void DrawRunSummaryPage(Rect rect)
        {
            BattleResult result = controller.CurrentResult;
            RunSummaryData summary = RunProgressBridge.CurrentSummary ?? new RunSummaryData();
            int battleCount = summary.BattleHistory != null ? summary.BattleHistory.Count : 0;
            string outcomeTitle = string.IsNullOrWhiteSpace(summary.FinalOutcomeTitle) ? "Atelier Secured" : summary.FinalOutcomeTitle;
            string finalBossName = string.IsNullOrWhiteSpace(summary.FinalBossName)
                ? result.BossDisplayName
                : summary.FinalBossName;
            string outcomeDescription = string.IsNullOrWhiteSpace(summary.FinalOutcomeDescription)
                ? "Reward system not ready yet. Showing your run board instead."
                : summary.FinalOutcomeDescription;

            float headerHeight = 138f;
            float footerHeight = 84f;
            float contentHeight = CalculateRunSummaryContentHeight(summary, rect.width - 84f);
            float runBoardHeight = 154f;

            DrawRect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.68f));
            DrawPaletteDockFrame(rect, WorkshopGold, 0.99f);
            DrawPaletteCardFrame(new Rect(rect.x + 18f, rect.y + 16f, rect.width - 36f, headerHeight - 20f), WorkshopGold, 0.98f, 0.05f);

            GUI.BeginGroup(rect);
            GUIStyle rightMutedStyle = new GUIStyle(summaryMutedStyle) { alignment = TextAnchor.UpperRight };
            GUI.Label(new Rect(54f, 26f, rect.width - 320f, 36f), outcomeTitle, summaryTitleStyle);
            GUI.Label(new Rect(54f, 66f, rect.width - 320f, 22f), finalBossName, summaryHeadingStyle);
            GUI.Label(new Rect(54f, 92f, rect.width - 88f, 30f), outcomeDescription, summaryBodyStyle);
            GUI.Label(new Rect(rect.width - 276f, 28f, 220f, 20f), $"Wins {summary.Victories}  •  Losses {summary.Defeats}", rightMutedStyle);
            GUI.Label(new Rect(rect.width - 276f, 52f, 220f, 20f), $"{battleCount} fights logged", rightMutedStyle);
            GUI.Label(new Rect(rect.width - 276f, 76f, 220f, 20f), "Run Result Ledger", rightMutedStyle);

            Rect scrollViewport = new Rect(32f, headerHeight, rect.width - 64f, rect.height - headerHeight - footerHeight);
            Rect contentRect = new Rect(0f, 0f, scrollViewport.width - 18f, contentHeight);
            runSummaryScroll = GUI.BeginScrollView(scrollViewport, runSummaryScroll, contentRect, false, true);

            float y = 0f;
            float spacing = 16f;

            Color previousColor;
            Rect animatedRect;

            if (BeginAnimatedBlock(new Rect(0f, y, contentRect.width, runBoardHeight), 0f, 36f, out animatedRect, out previousColor))
            {
                DrawPaletteDockFrame(animatedRect, WorkshopBlue, 0.98f);
                Rect showcaseRect = new Rect(animatedRect.xMax - 138f, animatedRect.y + 18f, 108f, 108f);
                float boardTextWidth = animatedRect.width - 190f;
                GUI.Label(new Rect(animatedRect.x + 44f, animatedRect.y + 23f, boardTextWidth - 20f, 22f), "Run Board", summaryHeadingStyle);
                GUI.Label(new Rect(animatedRect.x + 44f, animatedRect.y + 46f, boardTextWidth - 20f, 20f), $"Final Fight: {finalBossName}", summaryBodyStyle);
                GUI.Label(new Rect(animatedRect.x + 44f, animatedRect.y + 72f, boardTextWidth - 20f, 20f), $"{Mathf.Max(1, battleCount)} fights  •  {summary.TotalTurnsElapsed} turns", summaryBodyStyle);
                GUI.Label(new Rect(animatedRect.x + 44f, animatedRect.y + 98f, boardTextWidth - 20f, 18f), $"{summary.TotalCardsPlayed} cards  •  {summary.TotalDamageDealt} dmg  •  {summary.TotalHealingDone} heal", summaryMutedStyle);
                GUI.Label(new Rect(animatedRect.x + 44f, animatedRect.y + 120f, boardTextWidth - 20f, 20f), BuildRunBoardGainText(summary), summaryMutedStyle);
                DrawIllustratedSummaryCard(
                    showcaseRect,
                    string.Empty,
                    string.Empty,
                    string.Empty,
                    summary.RunWon ? WorkshopGold : DefeatAccent,
                    GetRunSummaryBossSprite(),
                    GetRunSummaryBossFallbackIcon(),
                    compact: true);
                EndAnimatedBlock(previousColor);
            }
            y += runBoardHeight + spacing;

            float endY;
            if (BeginAnimatedBlock(new Rect(0f, y, contentRect.width, 0f), FinalVictorySummaryStagger * 1f, 32f, out animatedRect, out previousColor))
            {
                endY = DrawHeroStatsSection(animatedRect, summary);
                EndAnimatedBlock(previousColor);
            }
            else
            {
                endY = y + GetHeroStatsSectionHeight(contentRect.width);
            }
            y = endY + spacing;

            if (BeginAnimatedBlock(new Rect(0f, y, contentRect.width, 0f), FinalVictorySummaryStagger * 2f, 28f, out animatedRect, out previousColor))
            {
                endY = DrawRunSummaryColumns(animatedRect, summary);
                EndAnimatedBlock(previousColor);
            }
            else
            {
                endY = y + GetRunSummaryColumnsSectionHeight();
            }
            y = endY + spacing;

            if (BeginAnimatedBlock(new Rect(0f, y, contentRect.width, 0f), FinalVictorySummaryStagger * 3f, 26f, out animatedRect, out previousColor))
            {
                endY = DrawEncounterHistorySection(animatedRect, summary);
                EndAnimatedBlock(previousColor);
            }
            else
            {
                endY = y + GetEncounterHistorySectionHeight(summary);
            }
            y = endY;

            GUI.EndScrollView();

            DrawRect(new Rect(30f, rect.height - footerHeight, rect.width - 60f, 1f), new Color(HudStroke.r, HudStroke.g, HudStroke.b, 0.72f));
            GUI.Label(new Rect(54f, rect.height - footerHeight + 14f, rect.width - 280f, 22f), "Legacy Sigils persist outside the run. Tokens remain run-only.", summaryMutedStyle);
            if (DrawThemedButton(new Rect(rect.width - 214f, rect.height - 58f, 184f, 34f), "Return To Menu", WorkshopGold, "run_summary_main_menu", true))
            {
                controller.ReturnToMainMenu();
            }

            GUI.EndGroup();
        }

        private float DrawHeroStatsSection(Rect rect, RunSummaryData summary)
        {
            string[] labels =
            {
                "Damage",
                "Cards",
                "Turns",
                "Prep",
                "Wins",
                "Tokens"
            };
            string[] values =
            {
                summary.TotalDamageDealt.ToString(),
                summary.TotalCardsPlayed.ToString(),
                summary.TotalTurnsElapsed.ToString(),
                summary.TotalPrepTicksUsed.ToString(),
                summary.Victories.ToString(),
                summary.TotalTokensEarned.ToString()
            };

            int columns = GetHeroStatsColumns(rect.width);
            int rows = Mathf.CeilToInt(labels.Length / (float)columns);
            float panelHeight = GetHeroStatsSectionHeight(rect.width);
            DrawPaletteDockFrame(new Rect(rect.x, rect.y, rect.width, panelHeight), WorkshopGold, 0.98f);
            GUI.Label(new Rect(rect.x + 24f, rect.y + 23f, rect.width - 48f, 22f), "Hero Stats", new GUIStyle(summaryHeadingStyle) { alignment = TextAnchor.MiddleCenter });

            float tileGap = 12f;
            float tileWidth = (rect.width - 102f - tileGap * (columns - 1)) / columns;
            float startX = rect.x + 20f;
            float startY = rect.y + 52f;
            for (int i = 0; i < labels.Length; i++)
            {
                int row = i / columns;
                int column = i % columns;
                float tileX = startX + column * (tileWidth + tileGap);
                float tileY = startY + row * 84f;
                Color accent = i % 3 == 0 ? WorkshopGold : i % 3 == 1 ? WorkshopBlue : WorkshopViolet;
                DrawRunSummaryStatTile(new Rect(tileX, tileY, tileWidth, 72f), values[i], labels[i], accent);
            }

            return rect.y + panelHeight;
        }

        private float DrawRunSummaryColumns(Rect rect, RunSummaryData summary)
        {
            float avgCardsPerTurn = summary.TotalTurnsElapsed > 0
                ? summary.TotalCardsPlayed / (float)summary.TotalTurnsElapsed
                : 0f;
            float panelHeight = 196f;
            float columnGap = 16f;
            float columnWidth = (rect.width - columnGap) * 0.5f;

            Rect battleRect = new Rect(rect.x, rect.y, columnWidth, panelHeight);
            Rect workshopRect = new Rect(rect.x + columnWidth + columnGap, rect.y, columnWidth, panelHeight);

            DrawPaletteDockFrame(battleRect, WorkshopBlue, 0.98f);
            GUI.Label(new Rect(battleRect.x + 40f, battleRect.y + 21f, battleRect.width - 60f, 22f), "Battle Summary", summaryHeadingStyle);
            DrawSummaryMetricLine(new Rect(battleRect.x + 40f, battleRect.y + 52f, battleRect.width - 60f, 20f), "Damage", summary.TotalDamageDealt.ToString(), WorkshopBlue);
            DrawSummaryMetricLine(new Rect(battleRect.x + 40f, battleRect.y + 78f, battleRect.width - 60f, 20f), "Healing", summary.TotalHealingDone.ToString(), new Color(0.46f, 0.9f, 0.7f, 1f));
            DrawSummaryMetricLine(new Rect(battleRect.x + 40f, battleRect.y + 104f, battleRect.width - 60f, 20f), "Shield", summary.TotalShieldGained.ToString(), ShieldAccent);
            DrawSummaryMetricLine(new Rect(battleRect.x + 40f, battleRect.y + 130f, battleRect.width - 60f, 20f), "Cards", summary.TotalCardsPlayed.ToString(), WorkshopGold);
            DrawSummaryMetricLine(new Rect(battleRect.x + 40f, battleRect.y + 156f, battleRect.width - 60f, 20f), "Turns", summary.TotalTurnsElapsed.ToString(), WorkshopViolet);

            DrawPaletteDockFrame(workshopRect, WorkshopGold, 0.98f);
            GUI.Label(new Rect(workshopRect.x + 40f, workshopRect.y + 21f, workshopRect.width - 60f, 22f), "Workshop Summary", summaryHeadingStyle);
            DrawSummaryMetricLine(new Rect(workshopRect.x + 40f, workshopRect.y + 52f, workshopRect.width - 60f, 20f), "Prep", summary.TotalPrepTicksUsed.ToString(), WorkshopGold);
            DrawSummaryMetricLine(new Rect(workshopRect.x + 40f, workshopRect.y + 78f, workshopRect.width - 60f, 20f), "Copies", summary.TotalCraftedCardCopies.ToString(), WorkshopBlue);
            DrawSummaryMetricLine(new Rect(workshopRect.x + 40f, workshopRect.y + 104f, workshopRect.width - 60f, 20f), "Types", summary.TotalCraftedCardTypes.ToString(), WorkshopViolet);
            DrawSummaryMetricLine(new Rect(workshopRect.x + 40f, workshopRect.y + 130f, workshopRect.width - 60f, 20f), "Tokens", summary.TotalTokensEarned.ToString(), new Color(0.95f, 0.56f, 0.28f, 1f));
            DrawSummaryMetricLine(new Rect(workshopRect.x + 40f, workshopRect.y + 156f, workshopRect.width - 60f, 20f), "Pace", $"{avgCardsPerTurn:0.00}/turn", new Color(0.78f, 0.84f, 0.98f, 1f));

            Rect gainsRect = new Rect(rect.x, rect.y + panelHeight + 16f, rect.width, 132f);
            DrawPaletteDockFrame(gainsRect, WorkshopViolet, 0.98f);
            GUI.Label(new Rect(gainsRect.x + 40f, gainsRect.y + 21f, gainsRect.width - 60f, 22f), "Battle Result & Legacy", summaryHeadingStyle);

            float cardGap = 16f;
            float cardWidth = (gainsRect.width - 40f - cardGap) * 0.5f;
            Rect resultCardRect = new Rect(gainsRect.x + 20f, gainsRect.y + 48f, cardWidth, 72f);
            Rect legacyCardRect = new Rect(resultCardRect.xMax + cardGap, gainsRect.y + 48f, cardWidth, 72f);

            DrawIllustratedSummaryCard(
                resultCardRect,
                "Battle Result",
                string.IsNullOrWhiteSpace(summary.FinalBossName) ? "Final Boss" : summary.FinalBossName,
                BuildBattleResultCardText(summary),
                WorkshopBlue,
                GetRunSummaryBossSprite(),
                GetRunSummaryBossFallbackIcon());
            DrawIllustratedSummaryCard(
                legacyCardRect,
                "Legacy Archive",
                BuildFinalRewardStatusText(summary),
                BuildLegacyCardText(summary),
                WorkshopViolet,
                null,
                MetaHudIconKind.Bounty);

            Rect graphRect = new Rect(rect.x + 16f, gainsRect.yMax + 16f, rect.width - 32f, 188f);
            DrawRunTrendChart(graphRect, summary);
            return graphRect.yMax;
        }

        private float DrawEncounterHistorySection(Rect rect, RunSummaryData summary)
        {
            int recordCount = summary.BattleHistory != null ? summary.BattleHistory.Count : 0;
            float y = rect.y;
            GUI.Label(new Rect(rect.x + 20f, y + 5f, rect.width - 20f, 22f), "Fight Log", summaryHeadingStyle);
            y += 32f;

            if (recordCount == 0)
            {
                DrawPaletteDockFrame(new Rect(rect.x, y, rect.width, 86f), WorkshopBlue, 0.98f);
                GUI.Label(new Rect(rect.x + 44f, y + 24f, rect.width - 68f, 22f), "No encounter records were captured for this run.", summaryBodyStyle);
                return y + 86f;
            }

            for (int i = 0; i < recordCount; i++)
            {
                RunBattleRecord record = summary.BattleHistory[i];
                float panelHeight = GetEncounterRecordHeight();
                DrawEncounterRecord(new Rect(rect.x, y, rect.width, panelHeight), record, i + 1);
                y += panelHeight + 14f;
            }

            return y - 14f;
        }

        private void DrawEncounterRecord(Rect rect, RunBattleRecord record, int index)
        {
            Color accent = record != null && record.IsBoss ? WorkshopGold : WorkshopBlue;
            string outcome = record != null && record.Victory ? "Victory" : "Defeat";
            string encounterLabel = record != null && !string.IsNullOrWhiteSpace(record.EncounterLabel)
                ? record.EncounterLabel
                : "Unknown Encounter";
            string bossName = record != null && !string.IsNullOrWhiteSpace(record.BossDisplayName)
                ? record.BossDisplayName
                : "Unknown Enemy";
            string gainText = BuildEncounterGainText(record);
            string detailText = record != null
                ? $"Prep {record.PrepTicksUsed}  •  Forge {record.CraftedCardCopies}/{record.CraftedCardTypes}  •  Bonus +{record.StartingShieldBonus}"
                : "No workshop detail";
            string battleText = record != null
                ? $"Turns {record.TurnsElapsed}  •  Cards {record.CardsPlayed}  •  Dmg {record.TotalDamageDealt}  •  Heal {record.TotalHealingDone}  •  Guard {record.TotalShieldGained}"
                : "No battle detail";

            DrawPaletteDockFrame(rect, accent, 0.98f);
            GUI.Label(new Rect(rect.x + 42f, rect.y + 14f, rect.width - 200f, 22f), $"Fight {index}: {encounterLabel}", summaryHeadingStyle);
            DrawTag(new Rect(rect.x + rect.width - 120f, rect.y + 16f, 98f, 20f), outcome, new Color(accent.r, accent.g, accent.b, 0.24f));
            GUI.Label(new Rect(rect.x + 42f, rect.y + 42f, rect.width - 64f, 20f), bossName, summaryBodyStyle);
            GUI.Label(new Rect(rect.x + 42f, rect.y + 66f, rect.width - 64f, 20f), battleText, summaryBodyStyle);
            GUI.Label(new Rect(rect.x + 42f, rect.y + 90f, rect.width - 64f, 20f), detailText, summaryBodyStyle);
            GUI.Label(new Rect(rect.x + 42f, rect.y + 112f, rect.width - 64f, 18f), gainText, summaryMutedStyle);
        }

        private void DrawRunSummaryStatTile(Rect rect, string value, string label, Color accent)
        {
            DrawPaletteCardFrame(rect, accent, 0.98f, 0.05f);
            GUI.Label(new Rect(rect.x + 12f, rect.y + 12f, rect.width - 24f, 26f), value, summaryStatValueStyle);
            GUI.Label(new Rect(rect.x + 12f, rect.y + 42f, rect.width - 24f, 20f), label, summaryStatLabelStyle);
        }

        private float CalculateRunSummaryContentHeight(RunSummaryData summary, float width)
        {
            float runBoardHeight = 154f;
            float spacing = 16f;
            float heroStatsHeight = GetHeroStatsSectionHeight(width);
            float summaryColumnsHeight = GetRunSummaryColumnsSectionHeight();
            float encounterHeight = GetEncounterHistorySectionHeight(summary);
            return runBoardHeight + spacing + heroStatsHeight + spacing + summaryColumnsHeight + spacing + encounterHeight + 8f;
        }

        private int GetHeroStatsColumns(float width)
        {
            return width < 1180f ? 3 : 6;
        }

        private float GetHeroStatsSectionHeight(float width)
        {
            int columns = GetHeroStatsColumns(width);
            int rows = Mathf.CeilToInt(6f / columns);
            return 52f + rows * 72f + (rows - 1) * 12f + 22f;
        }

        private float GetRunSummaryColumnsSectionHeight()
        {
            return 196f + 16f + 132f + 16f + 188f;
        }

        private float GetEncounterRecordHeight()
        {
            return 134f;
        }

        private float GetEncounterHistorySectionHeight(RunSummaryData summary)
        {
            int encounterCount = summary.BattleHistory != null ? summary.BattleHistory.Count : 0;
            return 32f + (encounterCount > 0 ? encounterCount * (GetEncounterRecordHeight() + 14f) - 14f : 86f);
        }

        private void DrawSummaryMetricLine(Rect rect, string label, string value, Color accent)
        {
            DrawRect(new Rect(rect.x, rect.y + rect.height * 0.5f - 5f, 10f, 10f), new Color(accent.r, accent.g, accent.b, 0.92f));
            GUI.Label(new Rect(rect.x + 18f, rect.y, rect.width * 0.5f, rect.height), label, summaryMutedStyle);
            GUI.Label(new Rect(rect.x + rect.width * 0.45f - 40f, rect.y, rect.width * 0.55f + 32f, rect.height), value, new GUIStyle(summaryBodyStyle) { alignment = TextAnchor.UpperRight });
        }

        private void DrawRunTrendChart(Rect rect, RunSummaryData summary)
        {
            DrawPaletteDockFrame(rect, WorkshopBlue, 0.98f);
            GUI.Label(new Rect(rect.x + 40f, rect.y + 21f, rect.width - 60f, 22f), "Fight Trend", summaryHeadingStyle);
            GUI.Label(new Rect(rect.x + 40f, rect.y + 40f, rect.width - 60f, 18f), "Damage, cards played, and token yield by encounter.", summaryMutedStyle);

            List<RunBattleRecord> records = summary.BattleHistory ?? new List<RunBattleRecord>();
            if (records.Count == 0)
            {
                GUI.Label(new Rect(rect.x + 40f, rect.y + 82f, rect.width - 60f, 22f), "No recorded fights available for charting.", summaryBodyStyle);
                return;
            }

            DrawMetricLegend(new Rect(rect.x + 30f, rect.y + 64f, rect.width - 50f, 16f), WorkshopGold, "Damage", WorkshopBlue, "Cards", WorkshopViolet, "Tokens");

            Rect chartRect = new Rect(rect.x + 30f, rect.y + 88f, rect.width - 50f, rect.height - 112f);
            float labelHeight = 22f;
            float barAreaHeight = chartRect.height - labelHeight;
            DrawRect(new Rect(chartRect.x, chartRect.yMax - labelHeight, chartRect.width, 1f), new Color(HudStroke.r, HudStroke.g, HudStroke.b, 0.52f));

            int maxDamage = 1;
            int maxCards = 1;
            int maxTokens = 1;
            for (int i = 0; i < records.Count; i++)
            {
                RunBattleRecord record = records[i];
                if (record == null)
                {
                    continue;
                }

                maxDamage = Mathf.Max(maxDamage, record.TotalDamageDealt);
                maxCards = Mathf.Max(maxCards, record.CardsPlayed);
                maxTokens = Mathf.Max(maxTokens, record.TokensEarned);
            }

            float groupWidth = chartRect.width / records.Count;
            GUIStyle centerLabelStyle = new GUIStyle(summaryMutedStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 11 };
            GUIStyle bossLabelStyle = new GUIStyle(summaryMutedStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 10, fontStyle = FontStyle.Bold };

            for (int i = 0; i < records.Count; i++)
            {
                RunBattleRecord record = records[i];
                Rect groupRect = new Rect(chartRect.x + i * groupWidth, chartRect.y, groupWidth, chartRect.height);
                if (record != null && record.IsBoss)
                {
                    DrawRect(new Rect(groupRect.x + 2f, groupRect.y + 2f, groupRect.width - 4f, barAreaHeight - 4f), new Color(WorkshopGold.r, WorkshopGold.g, WorkshopGold.b, 0.06f));
                    GUI.Label(new Rect(groupRect.x, groupRect.y + 2f, groupRect.width, 14f), "Boss", bossLabelStyle);
                }

                float innerWidth = Mathf.Max(24f, groupRect.width - 14f);
                float barGap = Mathf.Clamp(innerWidth * 0.08f, 4f, 10f);
                float barWidth = Mathf.Max(6f, (innerWidth - barGap * 2f) / 3f);
                float startX = groupRect.x + (groupRect.width - (barWidth * 3f + barGap * 2f)) * 0.5f;
                float baseY = chartRect.y + barAreaHeight - 2f;
                float damageHeight = record != null ? (barAreaHeight - 18f) * (record.TotalDamageDealt / (float)maxDamage) : 0f;
                float cardsHeight = record != null ? (barAreaHeight - 18f) * (record.CardsPlayed / (float)maxCards) : 0f;
                float tokensHeight = record != null ? (barAreaHeight - 18f) * (record.TokensEarned / (float)maxTokens) : 0f;

                DrawRect(new Rect(startX, baseY - damageHeight, barWidth, damageHeight), new Color(WorkshopGold.r, WorkshopGold.g, WorkshopGold.b, 0.92f));
                DrawRect(new Rect(startX + barWidth + barGap, baseY - cardsHeight, barWidth, cardsHeight), new Color(WorkshopBlue.r, WorkshopBlue.g, WorkshopBlue.b, 0.92f));
                DrawRect(new Rect(startX + (barWidth + barGap) * 2f, baseY - tokensHeight, barWidth, tokensHeight), new Color(WorkshopViolet.r, WorkshopViolet.g, WorkshopViolet.b, 0.92f));

                DrawOutline(new Rect(startX, baseY - damageHeight, barWidth, damageHeight), new Color(1f, 1f, 1f, 0.08f));
                DrawOutline(new Rect(startX + barWidth + barGap, baseY - cardsHeight, barWidth, cardsHeight), new Color(1f, 1f, 1f, 0.08f));
                DrawOutline(new Rect(startX + (barWidth + barGap) * 2f, baseY - tokensHeight, barWidth, tokensHeight), new Color(1f, 1f, 1f, 0.08f));

                string fightLabel = record != null && record.EncounterIndex > 0 ? $"F{record.EncounterIndex}" : $"F{i + 1}";
                GUI.Label(new Rect(groupRect.x, chartRect.yMax - labelHeight + 2f, groupRect.width, 18f), fightLabel, centerLabelStyle);
            }
        }

        private void DrawMetricLegend(Rect rect, Color firstColor, string firstLabel, Color secondColor, string secondLabel, Color thirdColor, string thirdLabel)
        {
            float swatch = 10f;
            float x = rect.x;
            DrawRect(new Rect(x, rect.y + 3f, swatch, swatch), firstColor);
            GUI.Label(new Rect(x + 36f, rect.y, 62f, rect.height), firstLabel, summaryMutedStyle);
            x += 84f;
            DrawRect(new Rect(x, rect.y + 3f, swatch, swatch), secondColor);
            GUI.Label(new Rect(x + 36f, rect.y, 54f, rect.height), secondLabel, summaryMutedStyle);
            x += 76f;
            DrawRect(new Rect(x, rect.y + 3f, swatch, swatch), thirdColor);
            GUI.Label(new Rect(x + 36f, rect.y, 58f, rect.height), thirdLabel, summaryMutedStyle);
        }

        private string BuildRewardsClaimedText(RunSummaryData summary)
        {
            if (summary.RewardsClaimed == null || summary.RewardsClaimed.Count == 0)
            {
                return "None yet";
            }

            int count = Mathf.Min(2, summary.RewardsClaimed.Count);
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            for (int i = 0; i < count; i++)
            {
                if (i > 0)
                {
                    builder.Append("  •  ");
                }

                builder.Append(summary.RewardsClaimed[i]);
            }

            return builder.ToString();
        }

        private string BuildFinalRewardStatusText(RunSummaryData summary)
        {
            if (!string.IsNullOrWhiteSpace(summary.LegacyUnlockName))
            {
                return summary.LegacyUnlockName;
            }

            return "No legacy gain";
        }

        private string BuildRunBoardGainText(RunSummaryData summary)
        {
            string rewardText = BuildRewardsClaimedText(summary);
            string legacyText = BuildFinalRewardStatusText(summary);
            return $"Workshop rewards  •  {rewardText}    Legacy  •  {legacyText}";
        }

        private string BuildBattleResultCardText(RunSummaryData summary)
        {
            if (summary == null)
            {
                return "No recorded battle result.";
            }

            if (summary.RewardsClaimed != null && summary.RewardsClaimed.Count > 0)
            {
                return $"Rewards: {BuildRewardsClaimedText(summary)}";
            }

            return $"Tokens earned: {summary.TotalTokensEarned}";
        }

        private string BuildLegacyCardText(RunSummaryData summary)
        {
            if (summary == null)
            {
                return "No legacy archive data recorded.";
            }

            if (!string.IsNullOrWhiteSpace(summary.LegacyUnlockName))
            {
                return "Permanent reward stored for future cycles.";
            }

            return "Clear the final boss to carve a permanent legacy reward.";
        }

        private string BuildEncounterGainText(RunBattleRecord record)
        {
            if (record == null)
            {
                return "Gain: none recorded";
            }

            if (!string.IsNullOrWhiteSpace(record.RewardDisplayName))
            {
                return $"Gain: {record.RewardDisplayName}  •  Tokens +{record.TokensEarned}";
            }

            return record.IsBoss
                ? $"Gain: Boss reward pending  •  Tokens +{record.TokensEarned}"
                    : record.TokensEarned > 0
                    ? $"Gain: Tokens +{record.TokensEarned}"
                    : "Gain: None";
        }

        private Sprite GetRunSummaryBossSprite()
        {
            Sprite bossSprite = controller != null &&
                controller.VisualManager != null &&
                controller.VisualManager.BossVisual != null &&
                controller.VisualManager.BossVisual.SpriteRenderer != null
                ? controller.VisualManager.BossVisual.SpriteRenderer.sprite
                : null;
            if (bossSprite != null)
            {
                return bossSprite;
            }

            if (controller == null || controller.CurrentBossDefinition == null)
            {
                return null;
            }

            Sprite spiritIcon = ArcaneArtCatalog.GetSpiritIcon(controller.CurrentBossDefinition.Element);
            if (spiritIcon != null)
            {
                return spiritIcon;
            }

            return ArcaneArtCatalog.GetElementIcon(controller.CurrentBossDefinition.Element);
        }

        private MetaHudIconKind GetRunSummaryBossFallbackIcon()
        {
            return controller != null &&
                controller.CurrentBossDefinition != null &&
                controller.CurrentBossDefinition.IsBoss
                ? MetaHudIconKind.Bounty
                : MetaHudIconKind.BreachPressure;
        }

        private void DrawIllustratedSummaryCard(
            Rect rect,
            string title,
            string headline,
            string body,
            Color accent,
            Sprite sprite,
            MetaHudIconKind fallbackIcon,
            bool compact = false)
        {
            DrawPaletteCardFrame(rect, accent, 0.98f, compact ? 0.04f : 0.05f);

            float inset = compact ? 8f : 12f;
            Rect artRect = new Rect(rect.x + inset, rect.y + inset, rect.height - inset * 2f, rect.height - inset * 2f);
            DrawPaletteCardFrame(artRect, accent, 0.98f, 0.03f);

            if (!DrawSpriteContained(new Rect(artRect.x + 7f, artRect.y + 7f, artRect.width - 14f, artRect.height - 14f), sprite, Color.white))
            {
                float iconSize = Mathf.Min(artRect.width - 16f, artRect.height - 16f);
                Rect iconRect = new Rect(artRect.center.x - iconSize * 0.5f, artRect.center.y - iconSize * 0.5f, iconSize, iconSize);
                DrawMetaIcon(iconRect, fallbackIcon);
            }

            if (compact)
            {
                return;
            }

            float textX = artRect.xMax + 14f;
            float textWidth = rect.xMax - textX - 14f;
            GUI.Label(new Rect(textX, rect.y + 10f, textWidth, 16f), title, summaryMutedStyle);
            GUI.Label(new Rect(textX, rect.y + 28f, textWidth, 20f), string.IsNullOrWhiteSpace(headline) ? "Unavailable" : headline, summaryBodyStyle);
            GUI.Label(new Rect(textX, rect.y + 48f, textWidth, rect.height - 58f), body, summaryMutedStyle);
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
                return $"{BuildTierLabel(card.Tier)}  •  {BuildRoleLabel(card.Role)}";
            }

            return $"{card.Element}  •  {BuildTierLabel(card.Tier)}  •  {BuildRoleLabel(card.Role)}";
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

        private void DrawCardPileMini(Rect rect, string label, int count, Color accent, float pulse, bool showShuffle)
        {
            float xJitter = pulse * 2f;
            float yLift = pulse * 2f;
            Rect backRect = new Rect(rect.x + 8f + xJitter, rect.y + 6f - yLift, 24f, 16f);
            Rect midRect = new Rect(rect.x + 5f + xJitter, rect.y + 3f - yLift, 24f, 16f);
            Rect frontRect = new Rect(rect.x + 2f + xJitter, rect.y - yLift, 24f, 16f);

            DrawRect(backRect, new Color(0.1f, 0.13f, 0.18f, 0.72f));
            DrawOutline(backRect, new Color(accent.r, accent.g, accent.b, 0.2f + pulse * 0.2f));
            DrawRect(midRect, new Color(0.12f, 0.16f, 0.22f, 0.82f));
            DrawOutline(midRect, new Color(accent.r, accent.g, accent.b, 0.3f + pulse * 0.24f));
            DrawRect(frontRect, new Color(HudPanel.r, HudPanel.g, HudPanel.b, 0.96f));
            DrawOutline(frontRect, new Color(accent.r, accent.g, accent.b, 0.72f + pulse * 0.18f));
            DrawRect(new Rect(frontRect.x, frontRect.y, frontRect.width, 3f), new Color(accent.r, accent.g, accent.b, 0.9f));
            DrawRect(new Rect(frontRect.x + 13f, frontRect.y - 2f, 14f, 12f), new Color(0.05f, 0.07f, 0.1f, 0.98f));
            DrawOutline(new Rect(frontRect.x + 13f, frontRect.y - 2f, 14f, 12f), new Color(accent.r, accent.g, accent.b, 0.74f));
            GUI.Label(new Rect(frontRect.x + 13f, frontRect.y - 2f, 14f, 12f), count.ToString(), chipStyle);

            GUI.Label(new Rect(rect.x + 34f, rect.y + 1f, rect.width - 20f, 20f), showShuffle ? "Shuffle" : label, mutedStyle);
            GUI.Label(new Rect(rect.x + 34f, rect.y + 13f, rect.width - 20f, 20f), count > 0 ? "Ready" : "Empty", sectionStyle);
        }

        private string BuildRoleLabel(WorkshopSpellRole role)
        {
            switch (role)
            {
                case WorkshopSpellRole.Attack:
                    return "Attack";
                case WorkshopSpellRole.Healing:
                    return "Heal";
                case WorkshopSpellRole.Defense:
                    return "Guard";
                default:
                    return "Spell";
            }
        }

        private string BuildTierLabel(WorkshopSpellTier tier)
        {
            switch (tier)
            {
                case WorkshopSpellTier.Basic:
                    return "Basic";
                case WorkshopSpellTier.Intermediate:
                    return "Fusion I";
                case WorkshopSpellTier.Advanced:
                    return "Fusion II+";
                default:
                    return "Spell";
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

                BattleDeckController deck = controller.Simulation.Deck;
                UpdateHandAnimationState(deck);

                if (lastObservedDrawPileCount >= 0)
                {
                    if (lastObservedDrawPileCount != deck.DrawPileCount)
                    {
                        drawPilePulse = 1f;
                    }

                    if (lastObservedDiscardPileCount != deck.DiscardPileCount)
                    {
                        discardPilePulse = 1f;
                    }

                    bool shuffled = lastObservedDrawPileCount == 0 &&
                                    deck.DrawPileCount > 0 &&
                                    lastObservedDiscardPileCount > deck.DiscardPileCount;
                    if (shuffled)
                    {
                        shufflePulse = 1f;
                        shuffleNoticeUntil = Time.unscaledTime + 0.8f;
                    }
                }

                lastObservedDrawPileCount = deck.DrawPileCount;
                lastObservedDiscardPileCount = deck.DiscardPileCount;
            }
            else
            {
                lastObservedActionPoints = -1;
                lastObservedDrawPileCount = -1;
                lastObservedDiscardPileCount = -1;
                lastHandCardIds = System.Array.Empty<string>();
                animatedHandStartIndex = int.MaxValue;
                animatedHandEndIndex = -1;
                handRevealTimes.Clear();
            }

            if (controller != null && controller.CurrentResult != lastShownResult)
            {
                lastShownResult = controller.CurrentResult;
                resultOverlayShownAt = controller.CurrentResult != null ? Time.unscaledTime : -1f;
                if (controller.CurrentResult == null)
                {
                    finalVictorySequencePhase = FinalVictorySequencePhase.None;
                    finalVictoryPhaseStartedAt = -1f;
                    finalVictorySummaryShownAt = -1f;
                }
                else if (controller.ShouldShowRunSummaryPage)
                {
                    finalVictorySequencePhase = FinalVictorySequencePhase.Intro;
                    finalVictoryPhaseStartedAt = Time.unscaledTime;
                    finalVictorySummaryShownAt = -1f;
                }
                else
                {
                    finalVictorySequencePhase = FinalVictorySequencePhase.Summary;
                    finalVictoryPhaseStartedAt = -1f;
                    finalVictorySummaryShownAt = Time.unscaledTime;
                }
            }

            actionPointFlash = Mathf.MoveTowards(actionPointFlash, 0f, Time.unscaledDeltaTime * 2.5f);
            drawPilePulse = Mathf.MoveTowards(drawPilePulse, 0f, Time.unscaledDeltaTime * 3f);
            discardPilePulse = Mathf.MoveTowards(discardPilePulse, 0f, Time.unscaledDeltaTime * 3f);
            shufflePulse = Mathf.MoveTowards(shufflePulse, 0f, Time.unscaledDeltaTime * 2.2f);
        }

        private void UpdateHandAnimationState(BattleDeckController deck)
        {
            if (deck == null || deck.Hand == null)
            {
                return;
            }

            WorkshopBattleCardEntry[] currentCards = new WorkshopBattleCardEntry[deck.Hand.Count];
            string[] currentIds = new string[deck.Hand.Count];
            for (int i = 0; i < deck.Hand.Count; i++)
            {
                currentCards[i] = deck.Hand[i];
                currentIds[i] = currentCards[i].CardId ?? string.Empty;
            }

            animatedHandStartIndex = int.MaxValue;
            animatedHandEndIndex = -1;

            if (lastHandCards.Length == 0)
            {
                if (currentIds.Length > 0)
                {
                    handAnimationStartedAt = Time.unscaledTime;
                    animatedHandStartIndex = 0;
                    animatedHandEndIndex = currentIds.Length - 1;
                    QueueDrawAnimations(0, currentIds.Length - 1);
                }

                lastHandCardIds = currentIds;
                lastHandCards = currentCards;
                return;
            }

            bool changed = currentIds.Length != lastHandCardIds.Length;
            if (!changed)
            {
                for (int i = 0; i < currentIds.Length; i++)
                {
                    if (currentIds[i] != lastHandCardIds[i])
                    {
                        changed = true;
                        break;
                    }
                }
            }

            if (!changed)
            {
                return;
            }

            pendingDiscardRequests.Clear();
            pendingDiscardSourceHandCount = -1;
            pendingDrawIndices.Clear();

            if (currentIds.Length > lastHandCardIds.Length)
            {
                int sharedPrefix = 0;
                int compareCount = Mathf.Min(lastHandCardIds.Length, currentIds.Length);
                while (sharedPrefix < compareCount && lastHandCardIds[sharedPrefix] == currentIds[sharedPrefix])
                {
                    sharedPrefix++;
                }

                handAnimationStartedAt = Time.unscaledTime;
                if (sharedPrefix == lastHandCardIds.Length)
                {
                    animatedHandStartIndex = lastHandCardIds.Length;
                    animatedHandEndIndex = currentIds.Length - 1;
                    QueueDrawAnimations(animatedHandStartIndex, animatedHandEndIndex);
                }
                else
                {
                    QueueDiscardAnimations(lastHandCards);
                    animatedHandStartIndex = 0;
                    animatedHandEndIndex = currentIds.Length - 1;
                    QueueDrawAnimations(animatedHandStartIndex, animatedHandEndIndex);
                }
            }
            else if (currentIds.Length < lastHandCardIds.Length)
            {
                QueueDiscardAnimations(FindRemovedCards(lastHandCards, currentCards), lastHandCards.Length);
            }
            else if (currentIds.Length == lastHandCardIds.Length && currentIds.Length > 0)
            {
                QueueDiscardAnimations(lastHandCards);
                handAnimationStartedAt = Time.unscaledTime;
                animatedHandStartIndex = 0;
                animatedHandEndIndex = currentIds.Length - 1;
                QueueDrawAnimations(animatedHandStartIndex, animatedHandEndIndex);
            }

            lastHandCardIds = currentIds;
            lastHandCards = currentCards;
        }

        private Rect GetAnimatedHandCardRect(Rect rect, int index, out float alpha)
        {
            if (IsHandCardHidden(index, Time.unscaledTime))
            {
                alpha = 0f;
                return rect;
            }

            if (handAnimationStartedAt < 0f || index < animatedHandStartIndex || index > animatedHandEndIndex)
            {
                alpha = 1f;
                return rect;
            }

            int localIndex = index - animatedHandStartIndex;
            float progress = Mathf.Clamp01((Time.unscaledTime - handAnimationStartedAt - localIndex * 0.045f) / 0.24f);
            float eased = progress * progress * (3f - 2f * progress);
            alpha = eased;
            return new Rect(rect.x, rect.y + (1f - eased) * 8f, rect.width, rect.height);
        }

        private void QueueDrawAnimations(int startIndex, int endIndex)
        {
            if (startIndex < 0 || endIndex < startIndex)
            {
                return;
            }

            for (int i = startIndex; i <= endIndex; i++)
            {
                pendingDrawIndices.Add(i);
            }
        }

        private void QueueDiscardAnimations(WorkshopBattleCardEntry[] cards)
        {
            if (cards == null || cards.Length == 0)
            {
                return;
            }

            pendingDiscardSourceHandCount = cards.Length;
            for (int i = 0; i < cards.Length; i++)
            {
                pendingDiscardRequests.Add(new PendingDiscardRequest
                {
                    Card = cards[i],
                    SourceIndex = i
                });
            }
        }

        private void QueueDiscardAnimations(PendingDiscardRequest[] requests, int sourceHandCount)
        {
            if (requests == null || requests.Length == 0)
            {
                return;
            }

            pendingDiscardSourceHandCount = sourceHandCount;
            for (int i = 0; i < requests.Length; i++)
            {
                pendingDiscardRequests.Add(requests[i]);
            }
        }

        private PendingDiscardRequest[] FindRemovedCards(WorkshopBattleCardEntry[] previousCards, WorkshopBattleCardEntry[] currentCards)
        {
            if (previousCards == null || previousCards.Length == 0)
            {
                return System.Array.Empty<PendingDiscardRequest>();
            }

            List<PendingDiscardRequest> removed = new List<PendingDiscardRequest>();
            int currentIndex = 0;
            for (int previousIndex = 0; previousIndex < previousCards.Length; previousIndex++)
            {
                string previousId = previousCards[previousIndex].CardId ?? string.Empty;
                if (currentIndex < currentCards.Length && previousId == (currentCards[currentIndex].CardId ?? string.Empty))
                {
                    currentIndex++;
                    continue;
                }

                bool useDraggedStart = !string.IsNullOrWhiteSpace(pendingDraggedDiscardCardId) &&
                                       pendingDraggedDiscardCardId == previousId;
                removed.Add(new PendingDiscardRequest
                {
                    Card = previousCards[previousIndex],
                    SourceIndex = previousIndex,
                    HasStartRect = useDraggedStart,
                    StartRect = useDraggedStart ? pendingDraggedDiscardStartRect : Rect.zero
                });
            }

            pendingDraggedDiscardCardId = string.Empty;
            pendingDraggedDiscardStartRect = Rect.zero;

            return removed.ToArray();
        }

        private void ResolvePendingDrawAnimations(Rect handRect, int handCount)
        {
            if (pendingDrawIndices.Count == 0 || drawPileScreenRect.width <= 0f)
            {
                return;
            }

            for (int i = 0; i < pendingDrawIndices.Count; i++)
            {
                int handIndex = pendingDrawIndices[i];
                if (handIndex < 0 || handIndex >= handCount)
                {
                    continue;
                }

                Rect endRect = BuildHandCardScreenRect(handRect, handIndex, handCount);
                float startedAt = handAnimationStartedAt + (handIndex - animatedHandStartIndex) * 0.045f;
                float revealAt = startedAt + 0.24f;
                transientCardAnimations.Add(new TransientCardAnimation
                {
                    Card = controller.Simulation.Deck.Hand[handIndex],
                    StartRect = new Rect(drawPileScreenRect.center.x - CardWidth * 0.5f, drawPileScreenRect.center.y - CardHeight * 0.5f, CardWidth, CardHeight),
                    EndRect = endRect,
                    StartedAt = startedAt,
                    Duration = 0.24f
                });
                handRevealTimes[handIndex] = revealAt;
            }

            pendingDrawIndices.Clear();
        }

        private void ResolvePendingDiscardAnimations(Rect handRect, int currentHandCount)
        {
            if (pendingDiscardRequests.Count == 0 || discardPileScreenRect.width <= 0f)
            {
                return;
            }

            int sourceHandCount = pendingDiscardSourceHandCount > 0 ? pendingDiscardSourceHandCount : currentHandCount;
            for (int i = 0; i < pendingDiscardRequests.Count; i++)
            {
                PendingDiscardRequest request = pendingDiscardRequests[i];
                Rect startRect = request.HasStartRect
                    ? request.StartRect
                    : BuildHandCardScreenRect(handRect, request.SourceIndex, sourceHandCount);
                transientCardAnimations.Add(new TransientCardAnimation
                {
                    Card = request.Card,
                    StartRect = startRect,
                    EndRect = new Rect(discardPileScreenRect.center.x - CardWidth * 0.35f, discardPileScreenRect.center.y - CardHeight * 0.35f, CardWidth * 0.7f, CardHeight * 0.7f),
                    StartedAt = Time.unscaledTime + i * 0.03f,
                    Duration = 0.24f
                });
            }

            pendingDiscardRequests.Clear();
            pendingDiscardSourceHandCount = -1;
        }

        private Rect BuildHandCardScreenRect(Rect handRect, int index, int handCount)
        {
            float localX = CardSpacing + index * (CardWidth + CardSpacing) - handScroll.x;
            float localY = 10f - handScroll.y;
            float screenX = handRect.x + 54f + localX;
            float screenY = handRect.y + 48f + localY;
            return new Rect(screenX, screenY, CardWidth, CardHeight);
        }

        private bool IsHandCardHidden(int handIndex, float atTime)
        {
            float revealAt = GetHandRevealTime(handIndex);
            if (revealAt < 0f)
            {
                return false;
            }

            if (atTime < revealAt)
            {
                return true;
            }

            handRevealTimes.Remove(handIndex);
            return false;
        }

        private float GetHandRevealTime(int handIndex)
        {
            float revealAt;
            if (handRevealTimes.TryGetValue(handIndex, out revealAt))
            {
                return revealAt;
            }

            return -1f;
        }

        private void DrawTransientCardAnimations()
        {
            if (transientCardAnimations.Count == 0)
            {
                return;
            }

            Color previous = GUI.color;
            for (int i = transientCardAnimations.Count - 1; i >= 0; i--)
            {
                TransientCardAnimation animation = transientCardAnimations[i];
                float elapsed = Time.unscaledTime - animation.StartedAt;
                if (elapsed < 0f)
                {
                    continue;
                }

                float progress = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, animation.Duration));
                if (progress >= 1f)
                {
                    transientCardAnimations.RemoveAt(i);
                    continue;
                }

                float eased = progress * progress * (3f - 2f * progress);
                Rect rect = new Rect(
                    Mathf.Lerp(animation.StartRect.x, animation.EndRect.x, eased),
                    Mathf.Lerp(animation.StartRect.y, animation.EndRect.y, eased),
                    Mathf.Lerp(animation.StartRect.width, animation.EndRect.width, eased),
                    Mathf.Lerp(animation.StartRect.height, animation.EndRect.height, eased));
                float alpha = 1f - progress * 0.4f;
                GUI.color = new Color(previous.r, previous.g, previous.b, previous.a * alpha);
                Color accent = GetElementColor(animation.Card.Element);
                DrawCardVisual(rect, animation.Card, -1, new Color(accent.r, accent.g, accent.b, 0.7f), accent, true, true);
            }

            GUI.color = previous;
        }

        private static Rect OffsetRect(Rect rect, Vector2 offset)
        {
            return new Rect(rect.x + offset.x, rect.y + offset.y, rect.width, rect.height);
        }

        private void UpdateFinalVictorySequence()
        {
            if (finalVictorySequencePhase != FinalVictorySequencePhase.Intro || finalVictoryPhaseStartedAt < 0f)
            {
                return;
            }

            if (Time.unscaledTime - finalVictoryPhaseStartedAt < FinalVictoryIntroDuration)
            {
                return;
            }

            finalVictorySequencePhase = FinalVictorySequencePhase.Summary;
            finalVictorySummaryShownAt = Time.unscaledTime;
        }

        private bool BeginAnimatedBlock(Rect rect, float delay, float riseDistance, out Rect animatedRect, out Color previousColor)
        {
            previousColor = GUI.color;
            float shownAt = finalVictorySummaryShownAt >= 0f ? finalVictorySummaryShownAt : Time.unscaledTime;
            float progress = Mathf.Clamp01((Time.unscaledTime - shownAt - delay) / 0.26f);
            if (progress <= 0f)
            {
                animatedRect = rect;
                return false;
            }

            float eased = progress * progress * (3f - 2f * progress);
            animatedRect = new Rect(rect.x, rect.y + (1f - eased) * riseDistance, rect.width, rect.height);
            GUI.color = new Color(previousColor.r, previousColor.g, previousColor.b, previousColor.a * eased);
            return true;
        }

        private void EndAnimatedBlock(Color previousColor)
        {
            GUI.color = previousColor;
        }

        private void EnsureTheme()
        {
            if (whiteTexture == null)
            {
                whiteTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                whiteTexture.SetPixel(0, 0, Color.white);
                whiteTexture.Apply();
            }

            if (uiPanelMainSprite == null)
            {
                uiPanelMainSprite = ArcaneArtCatalog.GetUiPanelMain();
            }

            if (uiPaletteDockSprite == null)
            {
                uiPaletteDockSprite = ArcaneArtCatalog.GetUiPaletteDock();
            }

            if (uiButtonSprite == null)
            {
                uiButtonSprite = ArcaneArtCatalog.GetUiButton();
            }

            if (uiButtonSmallSprite == null)
            {
                uiButtonSmallSprite = ArcaneArtCatalog.GetUiButtonSmall();
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

            turnInfoStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { textColor = new Color(0.95f, 0.95f, 0.98f) }
            };

            summaryTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 32,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft,
                wordWrap = true,
                normal = { textColor = HudText }
            };

            summaryHeadingStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperLeft,
                wordWrap = true,
                normal = { textColor = HudText }
            };

            summaryBodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                alignment = TextAnchor.UpperLeft,
                wordWrap = true,
                normal = { textColor = new Color(0.94f, 0.94f, 0.92f) }
            };

            summaryMutedStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.UpperLeft,
                wordWrap = true,
                normal = { textColor = new Color(0.74f, 0.78f, 0.82f) }
            };

            summaryStatValueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = HudText }
            };

            summaryStatLabelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { textColor = new Color(0.74f, 0.78f, 0.82f) }
            };
        }

        private void DrawPanelFrame(Rect rect, Color accent, float alpha)
        {
            if (uiPanelMainSprite != null)
            {
                DrawRect(new Rect(rect.x + 5f, rect.y + 7f, rect.width, rect.height), new Color(0f, 0f, 0f, 0.22f * alpha));
                DrawSprite(rect, uiPanelMainSprite, new Color(1f, 1f, 1f, alpha));
                DrawRect(new Rect(rect.x + 14f, rect.y + 14f, rect.width - 28f, rect.height - 28f), new Color(accent.r, accent.g, accent.b, 0.05f * alpha));
                DrawRect(new Rect(rect.x + 28f, rect.y + 18f, rect.width - 56f, 2f), new Color(accent.r, accent.g, accent.b, 0.32f * alpha));
                return;
            }

            DrawRect(rect, new Color(HudBackground.r, HudBackground.g, HudBackground.b, alpha));
            DrawOutline(rect, new Color(accent.r, accent.g, accent.b, 0.7f));
            DrawRect(new Rect(rect.x, rect.y, rect.width, 3f), accent);
            DrawRect(new Rect(rect.x + 6f, rect.y + 6f, rect.width - 12f, rect.height - 12f), new Color(1f, 1f, 1f, 0.012f));
        }

        private void DrawPaletteDockFrame(Rect rect, Color accent, float alpha)
        {
            if (uiPaletteDockSprite != null)
            {
                DrawRect(new Rect(rect.x + 5f, rect.y + 7f, rect.width, rect.height), new Color(0f, 0f, 0f, 0.24f * alpha));
                DrawSprite(rect, uiPaletteDockSprite, new Color(1f, 1f, 1f, alpha));
                DrawRect(new Rect(rect.x + 16f, rect.y + 16f, rect.width - 32f, rect.height - 32f), new Color(accent.r, accent.g, accent.b, 0.045f * alpha));
                DrawRect(new Rect(rect.x + 28f, rect.y + 22f, rect.width - 56f, 2f), new Color(accent.r, accent.g, accent.b, 0.24f * alpha));
                DrawRect(new Rect(rect.x + 28f, rect.yMax - 24f, rect.width - 56f, 2f), new Color(accent.r, accent.g, accent.b, 0.14f * alpha));
                return;
            }

            DrawPanelFrame(rect, accent, alpha);
        }

        private void DrawPaletteCardFrame(Rect rect, Color accent, float alpha, float accentFillAlpha)
        {
            if (uiPaletteDockSprite != null)
            {
                DrawRect(new Rect(rect.x + 4f, rect.y + 5f, rect.width, rect.height), new Color(0f, 0f, 0f, 0.18f * alpha));
                DrawSprite(rect, uiPaletteDockSprite, new Color(1f, 1f, 1f, alpha));
                DrawRect(new Rect(rect.x + 18f, rect.y + 16f, rect.width - 36f, rect.height - 32f), new Color(accent.r, accent.g, accent.b, accentFillAlpha * alpha));
                return;
            }

            DrawPanelWithShadow(
                rect,
                new Color(HudPanel.r, HudPanel.g, HudPanel.b, 0.96f * alpha),
                new Color(accent.r, accent.g, accent.b, 0.58f * alpha),
                new Color(0f, 0f, 0f, 0.16f * alpha));
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

            bool useWideWorkshopButton = uiButtonSprite != null && rect.width >= rect.height * 2.1f;
            if (useWideWorkshopButton)
            {
                DrawRect(new Rect(rect.x + 2f, rect.y + 3f, rect.width, rect.height), new Color(0f, 0f, 0f, 0.18f));
                DrawSprite(rect, uiButtonSprite, enabled ? Color.white : new Color(0.72f, 0.72f, 0.72f, 0.82f));
                DrawRect(new Rect(rect.x + 10f, rect.y + 8f, rect.width - 20f, rect.height - 16f), new Color(accent.r, accent.g, accent.b, enabled ? (isHover ? 0.14f : 0.08f) : 0.04f));
                GUI.Label(rect, label, labelStyle);
            }
            else if (uiButtonSmallSprite != null)
            {
                DrawRect(new Rect(rect.x + 2f, rect.y + 3f, rect.width, rect.height), new Color(0f, 0f, 0f, 0.16f));
                DrawSprite(rect, uiButtonSmallSprite, enabled ? Color.white : new Color(0.72f, 0.72f, 0.72f, 0.82f));
                DrawRect(new Rect(rect.x + 4f, rect.y + 4f, rect.width - 8f, rect.height - 8f), new Color(accent.r, accent.g, accent.b, enabled ? (isHover ? 0.14f : 0.06f) : 0.03f));
                GUI.Label(rect, label, labelStyle);
            }
            else
            {
                DrawPanelWithShadow(rect, fillColor, outlineColor, new Color(0f, 0f, 0f, 0.18f));
                DrawRect(new Rect(rect.x, rect.y, rect.width, 3f), new Color(accent.r, accent.g, accent.b, enabled ? (isHover ? 1f : 0.88f) : 0.26f));
                GUI.Label(rect, label, labelStyle);
            }

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

        private void DrawCardIconSlot(Rect rect, WorkshopBattleCardEntry card, Color accent)
        {
            DrawRect(rect, new Color(0.08f, 0.1f, 0.14f, 0.96f));
            DrawOutline(rect, new Color(accent.r, accent.g, accent.b, 0.74f));
            DrawRect(new Rect(rect.x, rect.y, rect.width, 3f), new Color(accent.r, accent.g, accent.b, 0.9f));

            Sprite icon = GetCardIconSprite(card);
            if (DrawSprite(new Rect(rect.x + 5f, rect.y + 5f, rect.width - 10f, rect.height - 10f), icon, Color.white))
            {
                return;
            }

            string fallback = card.Element == WorkshopElementAttribute.None ? "?" : card.Element.ToString().Substring(0, 1);
            GUI.Label(rect, fallback, chipStyle);
        }

        private static Sprite GetCardIconSprite(WorkshopBattleCardEntry card)
        {
            Sprite icon = ArcaneArtCatalog.GetElementIcon(card.Element);
            if (icon != null)
            {
                return icon;
            }

            return ArcaneArtCatalog.GetSpiritIcon(card.Element);
        }

        private bool DrawSprite(Rect rect, Sprite sprite, Color tint)
        {
            if (sprite == null || sprite.texture == null)
            {
                return false;
            }

            Rect textureRect = sprite.textureRect;
            Rect uv = new Rect(
                textureRect.x / sprite.texture.width,
                textureRect.y / sprite.texture.height,
                textureRect.width / sprite.texture.width,
                textureRect.height / sprite.texture.height);

            Color previous = GUI.color;
            GUI.color = tint;
            GUI.DrawTextureWithTexCoords(rect, sprite.texture, uv, true);
            GUI.color = previous;
            return true;
        }

        private bool DrawSpriteContained(Rect rect, Sprite sprite, Color tint)
        {
            if (sprite == null || sprite.texture == null)
            {
                return false;
            }

            Rect textureRect = sprite.textureRect;
            if (textureRect.width <= 0f || textureRect.height <= 0f)
            {
                return false;
            }

            float spriteAspect = textureRect.width / textureRect.height;
            float rectAspect = rect.width / rect.height;
            Rect fittedRect = rect;

            if (spriteAspect > rectAspect)
            {
                float fittedHeight = rect.width / spriteAspect;
                fittedRect.y += (rect.height - fittedHeight) * 0.5f;
                fittedRect.height = fittedHeight;
            }
            else
            {
                float fittedWidth = rect.height * spriteAspect;
                fittedRect.x += (rect.width - fittedWidth) * 0.5f;
                fittedRect.width = fittedWidth;
            }

            return DrawSprite(fittedRect, sprite, tint);
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
