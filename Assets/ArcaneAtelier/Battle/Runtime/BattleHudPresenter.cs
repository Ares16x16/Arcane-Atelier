using ArcaneAtelier.Workshop;
using UnityEngine;

namespace ArcaneAtelier.Battle
{
    public sealed class BattleHudPresenter : MonoBehaviour
    {
        private const float Margin = 18f;
        private const float TopBarHeight = 88f;
        private const float BottomPanelHeight = 188f;
        private const float CardWidth = 176f;
        private const float CardHeight = 118f;
        private const float CardSpacing = 14f;
        private const float DragThreshold = 10f;

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
                DrawResultOverlay(new Rect(Screen.width * 0.5f - 240f, Screen.height * 0.5f - 136f, 480f, 272f));
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
                    if (played)
                    {
                        selectedCardIndex = -1;
                    }
                    else
                    {
                        selectedCardIndex = resolvedIndex;
                    }

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
            DrawRect(new Rect(0f, 0f, Screen.width, 128f), new Color(0.03f, 0.05f, 0.08f, 0.16f));
            DrawRect(new Rect(0f, Screen.height - BottomPanelHeight - Margin * 2f, Screen.width, BottomPanelHeight + Margin * 2f), new Color(0.02f, 0.03f, 0.06f, 0.34f));
        }

        private void DrawTopBar(Rect rect)
        {
            DrawPanelFrame(rect, new Color(0.84f, 0.62f, 0.28f), 0.88f);
            GUI.BeginGroup(rect);

            Rect playerRect = new Rect(14f, 12f, rect.width * 0.32f, 60f);
            Rect centerRect = new Rect(rect.width * 0.32f + 24f, 10f, rect.width * 0.36f - 48f, 64f);
            Rect bossRect = new Rect(rect.width - rect.width * 0.32f - 14f, 12f, rect.width * 0.32f, 60f);

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

            DrawRect(rect, new Color(0.07f, 0.1f, 0.15f, 0.88f));
            DrawOutline(rect, new Color(accent.r, accent.g, accent.b, 0.45f));

            GUIStyle nameStyle = new GUIStyle(sectionStyle)
            {
                alignment = alignRight ? TextAnchor.UpperRight : TextAnchor.UpperLeft
            };
            GUIStyle valueStyle = new GUIStyle(bodyStyle)
            {
                alignment = alignRight ? TextAnchor.UpperRight : TextAnchor.UpperLeft
            };

            GUI.Label(new Rect(rect.x + 10f, rect.y + 8f, rect.width - 20f, 18f), displayName, nameStyle);
            GUI.Label(new Rect(rect.x + 10f, rect.y + 28f, rect.width - 20f, 18f), $"HP {currentHealth}/{maxHealth}   Shield {shield}", valueStyle);

            Rect healthBarRect = new Rect(rect.x + 10f, rect.y + 48f, rect.width - 20f, 6f);
            DrawRect(healthBarRect, new Color(0.14f, 0.18f, 0.24f));
            float ratio = maxHealth > 0 ? Mathf.Clamp01(currentHealth / (float)maxHealth) : 0f;
            DrawRect(new Rect(healthBarRect.x, healthBarRect.y, healthBarRect.width * ratio, healthBarRect.height), accent);
        }

        private void DrawCenterBattleStrip(Rect rect)
        {
            DrawRect(rect, new Color(0.08f, 0.1f, 0.15f, 0.82f));
            DrawOutline(rect, new Color(0.2f, 0.24f, 0.3f, 0.72f));

            GUI.Label(new Rect(rect.x, rect.y + 8f, rect.width, 16f), $"Turn {controller.Simulation.TurnsElapsed}", centeredMutedStyle);

            string intent = TruncateText(controller.BossIntentDescription, 42);
            GUI.Label(new Rect(rect.x + 14f, rect.y + 28f, rect.width - 28f, 18f), intent, centeredBodyStyle);

            bool canSkip = controller.IsPlayerInputAllowed;
            bool previousEnabled = GUI.enabled;
            GUI.enabled = canSkip;
            if (GUI.Button(new Rect(rect.x + rect.width * 0.5f - 64f, rect.y + 44f, 128f, 22f), "Skip Turn", GUI.skin.button))
            {
                controller.SkipTurnFromHud();
            }
            GUI.enabled = previousEnabled;
        }

        private void DrawHandPanel(Rect rect)
        {
            DrawPanelFrame(rect, new Color(0.88f, 0.72f, 0.3f), 0.9f);
            GUI.BeginGroup(rect);

            int handCount = controller.Simulation.Deck.HandCount;
            GUI.Label(new Rect(18f, 12f, 120f, 22f), "Hand", sectionStyle);
            GUI.Label(new Rect(90f, 14f, 40f, 18f), handCount.ToString(), statStyle);
            DrawInfoChip(new Rect(rect.width - 230f, 12f, 64f, 22f), "Draw", controller.Simulation.Deck.DrawPileCount.ToString());
            DrawInfoChip(new Rect(rect.width - 158f, 12f, 64f, 22f), "Discard", controller.Simulation.Deck.DiscardPileCount.ToString());
            DrawInfoChip(new Rect(rect.width - 86f, 12f, 64f, 22f), "State", controller.Simulation.State.ToString());

            Rect contentRect = new Rect(14f, 44f, rect.width - 28f, rect.height - 58f);
            float viewWidth = Mathf.Max(contentRect.width - 18f, handCount * (CardWidth + CardSpacing) + CardSpacing);

            handScroll = GUI.BeginScrollView(contentRect, handScroll, new Rect(0f, 0f, viewWidth, CardHeight + 12f), true, false);
            for (int i = 0; i < handCount; i++)
            {
                Rect cardRect = new Rect(CardSpacing + i * (CardWidth + CardSpacing), 0f, CardWidth, CardHeight);
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

            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && rect.Contains(currentEvent.mousePosition) && controller.IsPlayerInputAllowed)
            {
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

            DrawRect(rect, new Color(0.08f, 0.11f, 0.15f, isHover ? 1f : 0.96f));
            DrawOutline(rect, outline);
            DrawRect(new Rect(rect.x, rect.y, rect.width, 5f), accent);
            DrawRect(new Rect(rect.x + 12f, rect.y + 12f, 28f, 22f), accent);
            GUI.Label(new Rect(rect.x + 12f, rect.y + 14f, 28f, 18f), index < 9 ? (index + 1).ToString() : "-", pillStyle);

            GUI.Label(new Rect(rect.x + 48f, rect.y + 12f, rect.width - 60f, 20f), card.DisplayName, cardTitleStyle);
            DrawTag(new Rect(rect.x + 12f, rect.y + 42f, 70f, 18f), card.Role.ToString(), new Color(0.24f, 0.29f, 0.36f, 0.92f));
            DrawTag(new Rect(rect.x + 88f, rect.y + 42f, 70f, 18f), card.Element.ToString(), new Color(accent.r, accent.g, accent.b, 0.88f));

            GUI.Label(new Rect(rect.x + 12f, rect.y + 68f, rect.width - 24f, 20f), BuildCardSummary(card), titleStyle);

            string hint = BuildDragHint(card);
            GUI.Label(new Rect(rect.x + 12f, rect.y + 92f, rect.width - 24f, 16f), hint, mutedStyle);
        }

        private void DrawGhostCard(Rect rect)
        {
            DrawRect(rect, new Color(0.08f, 0.11f, 0.15f, 0.38f));
            DrawOutline(rect, new Color(0.4f, 0.45f, 0.5f, 0.45f));
        }

        private void DrawDraggedCard()
        {
            if (draggingCardIndex < 0 || draggingCardIndex >= controller.Simulation.Deck.Hand.Count)
            {
                return;
            }

            WorkshopBattleCardEntry card = controller.Simulation.Deck.Hand[draggingCardIndex];
            Rect dragRect = new Rect(dragMousePosition.x - CardWidth * 0.5f, dragMousePosition.y - CardHeight * 0.5f, CardWidth, CardHeight);
            Color accent = GetElementColor(card.Element);
            Color outline = activeDropTarget != DragTarget.None
                ? accent
                : new Color(0.55f, 0.59f, 0.66f, 0.72f);

            DrawRect(dragRect, new Color(0.08f, 0.11f, 0.15f, 0.98f));
            DrawOutline(dragRect, outline);
            DrawRect(new Rect(dragRect.x, dragRect.y, dragRect.width, 5f), accent);
            DrawRect(new Rect(dragRect.x + 12f, dragRect.y + 12f, 28f, 22f), accent);
            GUI.Label(new Rect(dragRect.x + 12f, dragRect.y + 14f, 28f, 18f), draggingCardIndex < 9 ? (draggingCardIndex + 1).ToString() : "-", pillStyle);
            GUI.Label(new Rect(dragRect.x + 48f, dragRect.y + 12f, dragRect.width - 60f, 20f), card.DisplayName, cardTitleStyle);
            DrawTag(new Rect(dragRect.x + 12f, dragRect.y + 42f, 70f, 18f), card.Role.ToString(), new Color(0.24f, 0.29f, 0.36f, 0.92f));
            DrawTag(new Rect(dragRect.x + 88f, dragRect.y + 42f, 70f, 18f), card.Element.ToString(), new Color(accent.r, accent.g, accent.b, 0.88f));
            GUI.Label(new Rect(dragRect.x + 12f, dragRect.y + 68f, dragRect.width - 24f, 20f), BuildCardSummary(card), titleStyle);
            GUI.Label(new Rect(dragRect.x + 12f, dragRect.y + 92f, dragRect.width - 24f, 16f), BuildDragHint(card), mutedStyle);
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
                DrawTargetOverlay(playerTargetScreenRect, new Color(0.9f, 0.25f, 0.2f, activeDropTarget == DragTarget.Player ? 0.32f : 0.14f), "Release on self");
            }
            else if (expectedTarget == DragTarget.Boss && bossTargetScreenRect.width > 0f)
            {
                DrawTargetOverlay(bossTargetScreenRect, new Color(0.25f, 0.65f, 0.3f, activeDropTarget == DragTarget.Boss ? 0.32f : 0.14f), "Release on enemy");
            }
        }

        private void DrawTargetOverlay(Rect rect, Color color, string text)
        {
            DrawRect(rect, color);
            DrawOutline(rect, new Color(color.r, color.g, color.b, 0.72f));
            GUI.Label(new Rect(rect.x, rect.y - 22f, rect.width, 18f), text, targetHintStyle);
        }

        private void DrawInfoChip(Rect rect, string label, string value)
        {
            DrawRect(rect, new Color(0.08f, 0.11f, 0.15f, 0.9f));
            DrawOutline(rect, new Color(0.2f, 0.24f, 0.3f, 0.7f));
            GUI.Label(new Rect(rect.x + 6f, rect.y + 3f, rect.width - 12f, 10f), label, centeredMutedStyle);
            GUI.Label(new Rect(rect.x + 6f, rect.y + 10f, rect.width - 12f, 12f), value, chipStyle);
        }

        private void DrawTag(Rect rect, string text, Color background)
        {
            DrawRect(rect, background);
            GUI.Label(rect, text, centeredMutedStyle);
        }

        private void DrawResultOverlay(Rect rect)
        {
            BattleResult result = controller.CurrentResult;
            string outcome = result.ResultType == BattleResultType.Victory ? "Victory" : "Defeat";
            Color accent = result.ResultType == BattleResultType.Victory
                ? new Color(0.35f, 0.78f, 0.45f)
                : new Color(0.84f, 0.34f, 0.32f);

            DrawRect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.48f));
            DrawPanelFrame(rect, accent, 0.94f);

            GUI.BeginGroup(rect);
            GUI.Label(new Rect(24f, 18f, rect.width - 48f, 28f), outcome, resultStyle);
            GUI.Label(new Rect(24f, 50f, rect.width - 48f, 18f), result.BossDisplayName, sectionStyle);

            DrawMiniStat(new Rect(24f, 92f, 96f, 44f), $"{result.TotalDamageDealt}", "Damage");
            DrawMiniStat(new Rect(132f, 92f, 96f, 44f), $"{result.TotalHealingDone}", "Healing");
            DrawMiniStat(new Rect(240f, 92f, 96f, 44f), $"{result.TotalShieldGained}", "Shield");
            DrawMiniStat(new Rect(348f, 92f, 96f, 44f), $"{result.CardsPlayed}", "Cards");

            GUI.Label(new Rect(24f, 156f, rect.width - 48f, 18f), $"Turns elapsed: {result.TurnsElapsed}", bodyStyle);
            GUI.Label(new Rect(24f, 182f, rect.width - 48f, 18f), "Battle result has been committed. Scene handoff is still pending.", mutedStyle);
            GUI.EndGroup();
        }

        private void DrawMiniStat(Rect rect, string value, string label)
        {
            DrawRect(rect, new Color(0.09f, 0.11f, 0.15f, 0.96f));
            DrawOutline(rect, new Color(0.18f, 0.22f, 0.28f));
            GUI.Label(new Rect(rect.x, rect.y + 5f, rect.width, 16f), value, statStyle);
            GUI.Label(new Rect(rect.x, rect.y + 22f, rect.width, 14f), label, centeredMutedStyle);
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
            switch (card.Role)
            {
                case WorkshopSpellRole.Attack:
                    return $"Attack {card.PrimaryValue} x{Mathf.Max(1, card.HitCount)}";
                case WorkshopSpellRole.Healing:
                    return $"Heal {card.PrimaryValue}";
                case WorkshopSpellRole.Defense:
                    return $"Shield {card.PrimaryValue}";
                default:
                    return "No effect data";
            }
        }

        private string BuildDragHint(WorkshopBattleCardEntry card)
        {
            switch (GetExpectedTarget(card))
            {
                case DragTarget.Boss:
                    return "Drag to enemy";
                case DragTarget.Player:
                    return "Drag to self";
                default:
                    return "Cannot be dragged";
            }
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

            return text.Substring(0, maxLength - 1) + "\u2026";
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
                normal = { textColor = new Color(0.97f, 0.95f, 0.9f) }
            };

            sectionStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.96f, 0.94f, 0.9f) }
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
                normal = { textColor = new Color(0.66f, 0.71f, 0.78f) }
            };

            centeredMutedStyle = new GUIStyle(mutedStyle)
            {
                alignment = TextAnchor.MiddleCenter
            };

            targetHintStyle = new GUIStyle(centeredMutedStyle)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.98f, 0.96f, 0.9f) }
            };

            statStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.97f, 0.95f, 0.9f) }
            };

            chipStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.96f, 0.94f, 0.9f) }
            };

            cardTitleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal = { textColor = new Color(0.97f, 0.95f, 0.9f) }
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
                normal = { textColor = new Color(0.98f, 0.96f, 0.9f) }
            };
        }

        private void DrawPanelFrame(Rect rect, Color accent, float alpha)
        {
            DrawRect(rect, new Color(0.04f, 0.06f, 0.1f, alpha));
            DrawOutline(rect, new Color(accent.r, accent.g, accent.b, 0.7f));
            DrawRect(new Rect(rect.x, rect.y, rect.width, 3f), accent);
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
    }
}
