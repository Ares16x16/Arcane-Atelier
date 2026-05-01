using System;
using UnityEngine;

namespace ArcaneAtelier.Battle
{
    public enum BattleFeedbackKind
    {
        None,
        Damage,
        Heal,
        Shield,
        StatusApplied,
        StatusTick,
        TurnBanner,
        CardPlayed,
        ActionCallout
    }

    public enum BattleFeedbackTarget
    {
        None,
        Player,
        Boss
    }

    public readonly struct BattleFeedbackRequest
    {
        public BattleFeedbackRequest(
            BattleFeedbackKind kind,
            BattleFeedbackTarget target,
            string text,
            string secondaryText = "",
            int amount = 0,
            Color? colorOverride = null,
            float duration = 0f,
            bool emphasize = false)
        {
            Kind = kind;
            Target = target;
            Text = text ?? string.Empty;
            SecondaryText = secondaryText ?? string.Empty;
            Amount = amount;
            ColorOverride = colorOverride;
            Duration = duration;
            Emphasize = emphasize;
        }

        public BattleFeedbackKind Kind { get; }
        public BattleFeedbackTarget Target { get; }
        public string Text { get; }
        public string SecondaryText { get; }
        public int Amount { get; }
        public Color? ColorOverride { get; }
        public float Duration { get; }
        public bool Emphasize { get; }
    }

    [Serializable]
    public sealed class BattleEffectAnchor : MonoBehaviour
    {
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.35f, 0f);

        public Vector3 WorldOffset => worldOffset;
        public Vector3 GetWorldPosition()
        {
            return transform.position + worldOffset;
        }
    }
}
