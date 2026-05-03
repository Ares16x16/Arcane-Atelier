using UnityEngine;

namespace ArcaneAtelier.Battle
{
    [CreateAssetMenu(menuName = "Arcane Atelier/Battle/Unit Animation Profile", fileName = "BattleUnitAnimationProfile")]
    public sealed class BattleUnitAnimationProfile : ScriptableObject
    {
        [System.Serializable]
        public struct AnimationSequence
        {
            [SerializeField] private Sprite[] frames;
            [SerializeField] private float framesPerSecond;
            [SerializeField] private bool loop;

            public Sprite[] Frames => frames;
            public float FramesPerSecond => framesPerSecond;
            public bool Loop => loop;

            public bool IsConfigured
            {
                get
                {
                    return frames != null && frames.Length > 0;
                }
            }

            public float FrameDuration
            {
                get
                {
                    return framesPerSecond > 0f ? 1f / framesPerSecond : 0.1f;
                }
            }

            public void Configure(Sprite[] resolvedFrames, float resolvedFramesPerSecond, bool shouldLoop)
            {
                frames = resolvedFrames ?? System.Array.Empty<Sprite>();
                framesPerSecond = Mathf.Max(0.01f, resolvedFramesPerSecond);
                loop = shouldLoop;
            }
        }

        [SerializeField] private Sprite previewSprite;
        [SerializeField] private AnimationSequence idleSequence;
        [SerializeField] private AnimationSequence attackSequence;
        [SerializeField] private AnimationSequence hurtSequence;

        public Sprite PreviewSprite => previewSprite;
        public AnimationSequence IdleSequence => idleSequence;
        public AnimationSequence AttackSequence => attackSequence;
        public AnimationSequence HurtSequence => hurtSequence;

        public void Configure(
            Sprite resolvedPreviewSprite,
            AnimationSequence resolvedIdleSequence,
            AnimationSequence resolvedAttackSequence,
            AnimationSequence resolvedHurtSequence)
        {
            previewSprite = resolvedPreviewSprite;
            idleSequence = resolvedIdleSequence;
            attackSequence = resolvedAttackSequence;
            hurtSequence = resolvedHurtSequence;
        }
    }
}
