using System.Collections.Generic;
using UnityEngine;

namespace ArcaneAtelier.Battle
{
    public sealed class BattleFeedbackPresenter : MonoBehaviour
    {
        private const int MaxFloatingEntries = 12;
        private const float DefaultBannerDuration = 1.15f;
        private const float DefaultCalloutDuration = 1.05f;
        private const float DefaultStatusDuration = 0.95f;
        private const float DefaultFloatDuration = 0.9f;

        private static readonly Color DamageColor = new Color(0.94f, 0.39f, 0.29f, 1f);
        private static readonly Color HealColor = new Color(0.42f, 0.86f, 0.54f, 1f);
        private static readonly Color ShieldColor = new Color(0.48f, 0.76f, 0.98f, 1f);
        private static readonly Color StatusColor = new Color(0.96f, 0.82f, 0.4f, 1f);
        private static readonly Color BannerColor = new Color(0.98f, 0.95f, 0.89f, 1f);
        private static readonly Color BannerBackdrop = new Color(0.09f, 0.11f, 0.15f, 0.84f);
        private static readonly Color CalloutBackdrop = new Color(0.08f, 0.1f, 0.14f, 0.8f);

        private readonly List<FloatingEntry> floatingEntries = new List<FloatingEntry>();
        private readonly Queue<BannerEntry> bannerQueue = new Queue<BannerEntry>();

        private BattleSceneController controller;
        private Camera battleCamera;
        private Texture2D whiteTexture;
        private GUIStyle bannerStyle;
        private GUIStyle bannerSubStyle;
        private GUIStyle floatingStyle;
        private GUIStyle calloutStyle;
        private GUIStyle calloutSubStyle;

        private BannerEntry activeBanner;
        private float activeBannerStartedAt = -1f;
        private string actionCalloutText = string.Empty;
        private string actionCalloutSubtext = string.Empty;
        private float actionCalloutUntil = -1f;

        private sealed class FloatingEntry
        {
            public BattleFeedbackTarget Target;
            public string Text;
            public Color Color;
            public float SpawnedAt;
            public float Duration;
            public float XOffset;
            public bool Emphasize;
            public float RiseDistance;
        }

        private readonly struct BannerEntry
        {
            public BannerEntry(string title, string subtitle, Color color, float duration)
            {
                Title = title ?? string.Empty;
                Subtitle = subtitle ?? string.Empty;
                Color = color;
                Duration = duration;
            }

            public string Title { get; }
            public string Subtitle { get; }
            public Color Color { get; }
            public float Duration { get; }
        }

        public void Initialize(BattleSceneController sceneController, Camera camera)
        {
            controller = sceneController;
            battleCamera = camera;
        }

        public void BindCamera(Camera camera)
        {
            battleCamera = camera;
        }

        public void Show(BattleFeedbackRequest request)
        {
            switch (request.Kind)
            {
                case BattleFeedbackKind.TurnBanner:
                    EnqueueBanner(
                        request.Text,
                        request.Amount > 0 ? $"Turn {request.Amount}" : string.Empty,
                        request.ColorOverride ?? BannerColor,
                        request.Duration > 0f ? request.Duration : DefaultBannerDuration);
                    break;
                case BattleFeedbackKind.CardPlayed:
                case BattleFeedbackKind.ActionCallout:
                    ShowActionCallout(
                        request.Text,
                        string.IsNullOrWhiteSpace(request.SecondaryText)
                            ? request.Kind == BattleFeedbackKind.CardPlayed ? "Card Played" : string.Empty
                            : request.SecondaryText,
                        request.Duration > 0f ? request.Duration : DefaultCalloutDuration);
                    break;
                case BattleFeedbackKind.StatusApplied:
                case BattleFeedbackKind.StatusTick:
                    AddFloatingEntry(
                        request.Target,
                        request.Text,
                        request.ColorOverride ?? StatusColor,
                        request.Duration > 0f ? request.Duration : DefaultStatusDuration,
                        request.Emphasize,
                        24f);
                    break;
                case BattleFeedbackKind.Damage:
                    AddFloatingEntry(
                        request.Target,
                        request.Amount > 0 ? request.Amount.ToString() : request.Text,
                        request.ColorOverride ?? DamageColor,
                        request.Duration > 0f ? request.Duration : DefaultFloatDuration,
                        request.Emphasize,
                        request.Emphasize ? 34f : 26f);
                    break;
                case BattleFeedbackKind.Heal:
                    AddFloatingEntry(
                        request.Target,
                        request.Amount > 0 ? $"+{request.Amount}" : request.Text,
                        request.ColorOverride ?? HealColor,
                        request.Duration > 0f ? request.Duration : DefaultFloatDuration,
                        request.Emphasize,
                        24f);
                    break;
                case BattleFeedbackKind.Shield:
                    AddFloatingEntry(
                        request.Target,
                        request.Amount > 0 ? $"+{request.Amount} Shield" : request.Text,
                        request.ColorOverride ?? ShieldColor,
                        request.Duration > 0f ? request.Duration : DefaultFloatDuration,
                        request.Emphasize,
                        22f);
                    break;
            }
        }

        private void OnGUI()
        {
            if (controller == null)
            {
                return;
            }

            EnsureTheme();
            if (battleCamera == null && controller.VisualManager != null)
            {
                battleCamera = controller.VisualManager.BattleCamera;
            }
            UpdateBannerState();
            DrawBanners();
            DrawActionCallout();
            DrawFloatingEntries();
        }

        private void UpdateBannerState()
        {
            if (activeBannerStartedAt >= 0f && Time.unscaledTime >= activeBannerStartedAt + activeBanner.Duration)
            {
                activeBanner = default;
                activeBannerStartedAt = -1f;
            }

            if (activeBannerStartedAt < 0f && bannerQueue.Count > 0)
            {
                activeBanner = bannerQueue.Dequeue();
                activeBannerStartedAt = Time.unscaledTime;
            }
        }

        private void DrawBanners()
        {
            if (activeBannerStartedAt < 0f)
            {
                return;
            }

            float elapsed = Time.unscaledTime - activeBannerStartedAt;
            float duration = Mathf.Max(0.01f, activeBanner.Duration);
            float normalized = Mathf.Clamp01(elapsed / duration);
            float fade = normalized < 0.2f
                ? normalized / 0.2f
                : normalized > 0.8f
                    ? (1f - normalized) / 0.2f
                    : 1f;
            fade = Mathf.Clamp01(fade);
            float pulse = 1f + Mathf.Sin(elapsed * 8f) * 0.02f;

            Rect rect = new Rect(Screen.width * 0.5f - 210f, 92f, 420f, 68f);
            DrawRect(rect, new Color(BannerBackdrop.r, BannerBackdrop.g, BannerBackdrop.b, BannerBackdrop.a * fade));
            DrawOutline(rect, new Color(activeBanner.Color.r, activeBanner.Color.g, activeBanner.Color.b, 0.74f * fade));
            DrawRect(new Rect(rect.x, rect.y, rect.width, 3f), new Color(activeBanner.Color.r, activeBanner.Color.g, activeBanner.Color.b, 0.92f * fade));

            Matrix4x4 previous = GUI.matrix;
            GUIUtility.ScaleAroundPivot(new Vector2(pulse, pulse), rect.center);
            Color previousColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, fade);
            GUI.Label(new Rect(rect.x, rect.y + 12f, rect.width, 28f), activeBanner.Title, bannerStyle);
            GUI.Label(new Rect(rect.x, rect.y + 38f, rect.width, 18f), activeBanner.Subtitle, bannerSubStyle);
            GUI.color = previousColor;
            GUI.matrix = previous;
        }

        private void DrawActionCallout()
        {
            if (Time.unscaledTime > actionCalloutUntil || string.IsNullOrWhiteSpace(actionCalloutText))
            {
                return;
            }

            float remaining = Mathf.Clamp01((actionCalloutUntil - Time.unscaledTime) / DefaultCalloutDuration);
            float alpha = Mathf.SmoothStep(0f, 1f, remaining);
            Rect rect = new Rect(Screen.width * 0.5f - 180f, Screen.height - 334f, 360f, 46f);
            DrawRect(rect, new Color(CalloutBackdrop.r, CalloutBackdrop.g, CalloutBackdrop.b, CalloutBackdrop.a * alpha));
            DrawOutline(rect, new Color(1f, 1f, 1f, 0.1f * alpha));
            GUI.Label(new Rect(rect.x, rect.y + 7f, rect.width, 18f), actionCalloutText, calloutStyle);
            if (!string.IsNullOrWhiteSpace(actionCalloutSubtext))
            {
                GUI.Label(new Rect(rect.x, rect.y + 24f, rect.width, 14f), actionCalloutSubtext, calloutSubStyle);
            }
        }

        private void DrawFloatingEntries()
        {
            if (battleCamera == null)
            {
                return;
            }

            for (int i = floatingEntries.Count - 1; i >= 0; i--)
            {
                FloatingEntry entry = floatingEntries[i];
                float elapsed = Time.unscaledTime - entry.SpawnedAt;
                if (elapsed >= entry.Duration)
                {
                    floatingEntries.RemoveAt(i);
                    continue;
                }

                BattleEffectAnchor anchor = GetAnchor(entry.Target);
                if (anchor == null)
                {
                    continue;
                }

                Vector3 screenPoint = battleCamera.WorldToScreenPoint(anchor.GetWorldPosition());
                if (screenPoint.z < 0f)
                {
                    continue;
                }

                float progress = elapsed / entry.Duration;
                float alpha = 1f - progress;
                float yOffset = Mathf.Lerp(0f, entry.RiseDistance, progress);
                float wobble = Mathf.Sin(progress * 8f + entry.XOffset) * 6f;
                Rect rect = new Rect(
                    screenPoint.x - 70f + wobble + entry.XOffset,
                    Screen.height - screenPoint.y - 8f - yOffset,
                    140f,
                    entry.Emphasize ? 24f : 20f);

                GUIStyle style = new GUIStyle(floatingStyle)
                {
                    fontSize = entry.Emphasize ? 19 : 16
                };

                Color previous = GUI.color;
                GUI.color = new Color(entry.Color.r, entry.Color.g, entry.Color.b, alpha);
                GUI.Label(rect, entry.Text, style);
                GUI.color = previous;
            }
        }

        private BattleEffectAnchor GetAnchor(BattleFeedbackTarget target)
        {
            if (controller == null || controller.VisualManager == null)
            {
                return null;
            }

            switch (target)
            {
                case BattleFeedbackTarget.Player:
                    return controller.VisualManager.PlayerAnchor;
                case BattleFeedbackTarget.Boss:
                    return controller.VisualManager.BossAnchor;
                default:
                    return null;
            }
        }

        private void EnqueueBanner(string title, string subtitle, Color color, float duration)
        {
            bannerQueue.Enqueue(new BannerEntry(title, subtitle, color, duration));
        }

        private void ShowActionCallout(string title, string subtitle, float duration)
        {
            actionCalloutText = title ?? string.Empty;
            actionCalloutSubtext = subtitle ?? string.Empty;
            actionCalloutUntil = Time.unscaledTime + Mathf.Max(0.2f, duration);
        }

        private void AddFloatingEntry(BattleFeedbackTarget target, string text, Color color, float duration, bool emphasize, float riseDistance)
        {
            if (target == BattleFeedbackTarget.None || string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            if (floatingEntries.Count >= MaxFloatingEntries)
            {
                floatingEntries.RemoveAt(0);
            }

            floatingEntries.Add(new FloatingEntry
            {
                Target = target,
                Text = text,
                Color = color,
                SpawnedAt = Time.unscaledTime,
                Duration = Mathf.Max(0.2f, duration),
                XOffset = Random.Range(-22f, 22f),
                Emphasize = emphasize,
                RiseDistance = riseDistance
            });
        }

        private void EnsureTheme()
        {
            if (whiteTexture == null)
            {
                whiteTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                whiteTexture.SetPixel(0, 0, Color.white);
                whiteTexture.Apply();
            }

            if (bannerStyle != null)
            {
                return;
            }

            bannerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 26,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = BannerColor }
            };

            bannerSubStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.8f, 0.84f, 0.9f, 1f) }
            };

            floatingStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };

            calloutStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.98f, 0.96f, 0.92f, 1f) }
            };

            calloutSubStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 11,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.72f, 0.76f, 0.82f, 1f) }
            };
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
