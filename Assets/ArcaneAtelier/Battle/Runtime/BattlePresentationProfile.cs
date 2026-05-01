using UnityEngine;

namespace ArcaneAtelier.Battle
{
    [CreateAssetMenu(menuName = "Arcane Atelier/Battle/Presentation Profile", fileName = "BattlePresentationProfile")]
    public sealed class BattlePresentationProfile : ScriptableObject
    {
        [SerializeField] private string bossId = "boss.id";
        [SerializeField] private Sprite bossSprite;
        [SerializeField] private BattleUnitAnimationProfile bossAnimationProfile;
        [SerializeField] private Sprite backgroundSprite;
        [SerializeField] private Vector3 bossPosition = new Vector3(3.5f, 0f, 0f);
        [SerializeField] private Vector3 bossScale = new Vector3(2.8f, 2.8f, 1f);
        [SerializeField] private Vector3 backgroundScale = new Vector3(6f, 6f, 1f);

        public string BossId => bossId;
        public Sprite BossSprite => bossSprite;
        public BattleUnitAnimationProfile BossAnimationProfile => bossAnimationProfile;
        public Sprite BackgroundSprite => backgroundSprite;
        public Vector3 BossPosition => bossPosition;
        public Vector3 BossScale => bossScale;
        public Vector3 BackgroundScale => backgroundScale;

        public void Configure(
            string id,
            Sprite resolvedBossSprite,
            BattleUnitAnimationProfile resolvedBossAnimationProfile,
            Sprite resolvedBackgroundSprite,
            Vector3 resolvedBossPosition,
            Vector3 resolvedBossScale,
            Vector3 resolvedBackgroundScale)
        {
            bossId = id ?? string.Empty;
            bossSprite = resolvedBossSprite;
            bossAnimationProfile = resolvedBossAnimationProfile;
            backgroundSprite = resolvedBackgroundSprite;
            bossPosition = resolvedBossPosition;
            bossScale = resolvedBossScale;
            backgroundScale = resolvedBackgroundScale;
        }

        public void Configure(
            string id,
            Sprite resolvedBossSprite,
            Sprite resolvedBackgroundSprite,
            Vector3 resolvedBossPosition,
            Vector3 resolvedBossScale,
            Vector3 resolvedBackgroundScale)
        {
            Configure(
                id,
                resolvedBossSprite,
                null,
                resolvedBackgroundSprite,
                resolvedBossPosition,
                resolvedBossScale,
                resolvedBackgroundScale);
        }
    }
}
