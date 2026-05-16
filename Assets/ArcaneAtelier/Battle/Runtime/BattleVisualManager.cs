using System.Collections;
using UnityEngine;

namespace ArcaneAtelier.Battle
{
    public sealed class BattleVisualManager : MonoBehaviour
    {
        private static readonly Vector3 DefaultBackgroundPosition = new Vector3(0f, 0f, 10f);
        private static readonly Vector3 DefaultPlayerPosition = new Vector3(-3.5f, 0f, 0f);
        private static readonly Vector3 DefaultBossPosition = new Vector3(3.5f, 0f, 0f);
        private static readonly Vector3 DefaultPlayerScale = Vector3.one * 1.8f;
        private static readonly Vector3 DefaultBossScale = new Vector3(2.8f, 2.8f, 1f);
        private static readonly Vector3 DefaultBackgroundScale = new Vector3(6f, 6f, 1f);

        [Header("Edit-Mode References")]
        [SerializeField] private BattleUnitVisual playerVisual;
        [SerializeField] private BattleUnitVisual bossVisual;
        [SerializeField] private SpriteRenderer backgroundRenderer;
        [SerializeField] private SpriteRenderer playerShadowRenderer;
        [SerializeField] private SpriteRenderer bossShadowRenderer;
        [SerializeField] private SpriteRenderer centerRuneRenderer;
        [SerializeField] private BattleEffectAnchor playerAnchor;
        [SerializeField] private BattleEffectAnchor bossAnchor;

        [Header("Fallback Sprites")]
        [SerializeField] private Sprite playerSprite;
        [SerializeField] private Sprite bossSprite;
        [SerializeField] private Sprite backgroundSprite;
        [SerializeField] private BattleUnitAnimationProfile playerAnimationProfile;

        [Header("Camera & Effects")]
        [SerializeField] private Camera battleCamera;
        [SerializeField] private float screenShakeMagnitude = 0.12f;
        [SerializeField] private float screenShakeDuration = 0.18f;

        [Header("Placeholder Colors")]
        [SerializeField] private Color playerColor = new Color(0.9f, 0.25f, 0.2f);
        [SerializeField] private Color bossColor = new Color(0.25f, 0.65f, 0.3f);
        [SerializeField] private Color backgroundColor = new Color(0.06f, 0.06f, 0.1f);

        private Vector3 cameraOriginalPosition;
        private bool isShaking;
        private BattleSimulation subscribedSimulation;
        private BattlePresentationProfile activePresentationProfile;
        private Sprite playerShadowSprite;
        private Sprite bossShadowSprite;
        private Sprite centerRuneSprite;

        public Camera BattleCamera => battleCamera;
        public BattleUnitVisual PlayerVisual => playerVisual;
        public BattleUnitVisual BossVisual => bossVisual;
        public BattleEffectAnchor PlayerAnchor => playerAnchor;
        public BattleEffectAnchor BossAnchor => bossAnchor;

        public void Initialize(
            BattleSimulation simulation,
            BattleUnit player,
            BattleUnit boss,
            BattleContentDatabase contentDatabase,
            string bossId)
        {
            UnsubscribeFromSimulation();
            activePresentationProfile = contentDatabase != null
                ? contentDatabase.FindPresentationProfile(bossId)
                : null;
            EnsureCamera();
            EnsureBackground();
            EnsurePlayerVisual();
            EnsureBossVisual();
            EnsureStageMarks();
            EnsureAnchors();

            subscribedSimulation = simulation;
            simulation.PlayerActionResolved += OnPlayerActionResolved;
            simulation.BossActionResolved += OnBossActionResolved;
            simulation.BattleEnded += OnBattleEnded;
        }

        private void OnDestroy()
        {
            UnsubscribeFromSimulation();
        }

        private void EnsureCamera()
        {
            if (battleCamera == null)
            {
                battleCamera = Camera.main;
            }

            if (battleCamera == null)
            {
                GameObject camObj = new GameObject("Battle Camera");
                camObj.tag = "MainCamera";
                battleCamera = camObj.AddComponent<Camera>();
                battleCamera.orthographic = true;
                battleCamera.orthographicSize = 5f;
                battleCamera.backgroundColor = Color.black;
                battleCamera.transform.position = new Vector3(0f, 0f, -10f);
            }

            cameraOriginalPosition = battleCamera.transform.position;
        }

        private void EnsureBackground()
        {
            Sprite resolvedBackgroundSprite = ResolveBackgroundSprite();

            if (backgroundRenderer != null)
            {
                if (resolvedBackgroundSprite != null)
                {
                    backgroundRenderer.sprite = resolvedBackgroundSprite;
                }
                backgroundRenderer.transform.localScale = ResolveBackgroundScale(backgroundRenderer.sprite != null);
                backgroundRenderer.transform.position = DefaultBackgroundPosition;
                return;
            }

            GameObject bgObj = new GameObject("Background");
            backgroundRenderer = bgObj.AddComponent<SpriteRenderer>();
            backgroundRenderer.sprite = resolvedBackgroundSprite != null
                ? resolvedBackgroundSprite
                : CreateSolidSprite(backgroundColor, 64, 64, "BackgroundSprite");
            backgroundRenderer.transform.localScale = ResolveBackgroundScale(backgroundRenderer.sprite != null);
            backgroundRenderer.transform.position = DefaultBackgroundPosition;
            backgroundRenderer.sortingOrder = -100;
        }

        private void EnsurePlayerVisual()
        {
            BattleUnitAnimationProfile resolvedPlayerAnimationProfile = playerAnimationProfile;

            if (playerVisual != null)
            {
                if (playerSprite != null || resolvedPlayerAnimationProfile != null)
                {
                    playerVisual.transform.position = DefaultPlayerPosition;
                    playerVisual.Setup(playerSprite, resolvedPlayerAnimationProfile, playerColor, DefaultPlayerScale);
                }
                else
                {
                    playerVisual.StartIdle();
                }
                return;
            }

            GameObject playerObj = new GameObject("PlayerVisual");
            playerObj.transform.position = DefaultPlayerPosition;
            playerVisual = playerObj.AddComponent<BattleUnitVisual>();
            Sprite sprite = playerSprite != null
                ? playerSprite
                : CreateSolidSprite(playerColor, 64, 64, "PlayerPlaceholder");
            playerVisual.Setup(sprite, resolvedPlayerAnimationProfile, playerColor, DefaultPlayerScale);
        }

        private void EnsureBossVisual()
        {
            Sprite resolvedBossSprite = ResolveBossSprite();
            BattleUnitAnimationProfile resolvedBossAnimationProfile = ResolveBossAnimationProfile();
            Vector3 bossPosition = ResolveBossPosition();
            Vector3 bossScale = ResolveBossScale();

            if (bossVisual != null)
            {
                bossVisual.transform.position = bossPosition;
                if (resolvedBossSprite != null || resolvedBossAnimationProfile != null)
                {
                    bossVisual.Setup(resolvedBossSprite, resolvedBossAnimationProfile, bossColor, bossScale);
                }
                else
                {
                    bossVisual.transform.localScale = bossScale;
                    bossVisual.StartIdle();
                }
                return;
            }

            GameObject bossObj = new GameObject("BossVisual");
            bossObj.transform.position = bossPosition;
            bossVisual = bossObj.AddComponent<BattleUnitVisual>();
            Sprite sprite = resolvedBossSprite != null
                ? resolvedBossSprite
                : CreateSolidSprite(bossColor, 64, 64, "BossPlaceholder");
            bossVisual.Setup(sprite, resolvedBossAnimationProfile, bossColor, bossScale);
        }

        private void EnsureAnchors()
        {
            if (playerVisual != null)
            {
                playerAnchor = EnsureAnchor(playerVisual.gameObject, playerAnchor);
            }

            if (bossVisual != null)
            {
                bossAnchor = EnsureAnchor(bossVisual.gameObject, bossAnchor);
            }
        }

        private static BattleEffectAnchor EnsureAnchor(GameObject owner, BattleEffectAnchor existingAnchor)
        {
            if (existingAnchor != null)
            {
                return existingAnchor;
            }

            BattleEffectAnchor anchor = owner.GetComponent<BattleEffectAnchor>();
            if (anchor == null)
            {
                anchor = owner.AddComponent<BattleEffectAnchor>();
            }

            return anchor;
        }

        private void EnsureStageMarks()
        {
            if (playerVisual == null || bossVisual == null)
            {
                return;
            }

            playerShadowRenderer = EnsureStageSprite(
                "Player Grounding Shadow",
                playerShadowRenderer,
                ResolvePlayerShadowSprite(),
                playerVisual.transform.position + new Vector3(0f, -0.72f, 0.02f),
                new Vector3(1.65f, 0.5f, 1f),
                -12);

            bossShadowRenderer = EnsureStageSprite(
                "Enemy Grounding Shadow",
                bossShadowRenderer,
                ResolveBossShadowSprite(),
                bossVisual.transform.position + new Vector3(0f, -0.84f, 0.02f),
                new Vector3(2.15f, 0.54f, 1f),
                -12);

            centerRuneRenderer = EnsureStageSprite(
                "Battle Center Rune",
                centerRuneRenderer,
                ResolveCenterRuneSprite(),
                new Vector3(0f, -1.34f, 0.04f),
                new Vector3(1.55f, 0.36f, 1f),
                -13);
        }

        private Sprite ResolvePlayerShadowSprite()
        {
            if (playerShadowSprite == null)
            {
                playerShadowSprite = CreateEllipseSprite(new Color(0f, 0f, 0f, 0.42f), 96, 32, "PlayerShadow");
            }

            return playerShadowSprite;
        }

        private Sprite ResolveBossShadowSprite()
        {
            if (bossShadowSprite == null)
            {
                bossShadowSprite = CreateEllipseSprite(new Color(0f, 0f, 0f, 0.48f), 128, 36, "EnemyShadow");
            }

            return bossShadowSprite;
        }

        private Sprite ResolveCenterRuneSprite()
        {
            if (centerRuneSprite == null)
            {
                centerRuneSprite = CreateEllipseSprite(new Color(0.95f, 0.72f, 0.28f, 0.18f), 160, 42, "BattleCenterRune");
            }

            return centerRuneSprite;
        }

        private static SpriteRenderer EnsureStageSprite(
            string objectName,
            SpriteRenderer existingRenderer,
            Sprite sprite,
            Vector3 position,
            Vector3 scale,
            int sortingOrder)
        {
            SpriteRenderer renderer = existingRenderer;
            if (renderer == null)
            {
                GameObject stageObject = new GameObject(objectName);
                renderer = stageObject.AddComponent<SpriteRenderer>();
            }

            renderer.sprite = sprite;
            renderer.transform.position = position;
            renderer.transform.localScale = scale;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private Sprite ResolveBossSprite()
        {
            if (activePresentationProfile != null && activePresentationProfile.BossSprite != null)
            {
                return activePresentationProfile.BossSprite;
            }

            return bossSprite;
        }

        private Sprite ResolveBackgroundSprite()
        {
            if (activePresentationProfile != null && activePresentationProfile.BackgroundSprite != null)
            {
                return activePresentationProfile.BackgroundSprite;
            }

            return backgroundSprite;
        }

        private BattleUnitAnimationProfile ResolveBossAnimationProfile()
        {
            if (activePresentationProfile != null && activePresentationProfile.BossAnimationProfile != null)
            {
                return activePresentationProfile.BossAnimationProfile;
            }

            return null;
        }

        private Vector3 ResolveBossPosition()
        {
            if (activePresentationProfile != null)
            {
                return activePresentationProfile.BossPosition;
            }

            return DefaultBossPosition;
        }

        private Vector3 ResolveBossScale()
        {
            if (activePresentationProfile != null)
            {
                return activePresentationProfile.BossScale;
            }

            return DefaultBossScale;
        }

        private Vector3 ResolveBackgroundScale(bool hasBackgroundSprite)
        {
            if (activePresentationProfile != null)
            {
                return activePresentationProfile.BackgroundScale;
            }

            return hasBackgroundSprite ? DefaultBackgroundScale : new Vector3(24f, 16f, 1f);
        }

        private static Sprite CreateSolidSprite(Color color, int width, int height, string name)
        {
            Texture2D texture = new Texture2D(width, height);
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }
            texture.SetPixels(pixels);
            texture.Apply();

            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = name;
            return sprite;
        }

        private static Sprite CreateEllipseSprite(Color color, int width, int height, string name)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[width * height];
            float radiusX = width * 0.5f;
            float radiusY = height * 0.5f;
            Vector2 center = new Vector2(radiusX, radiusY);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float normalizedX = (x - center.x) / radiusX;
                    float normalizedY = (y - center.y) / radiusY;
                    float distance = normalizedX * normalizedX + normalizedY * normalizedY;
                    float alpha = Mathf.Clamp01(1f - Mathf.InverseLerp(0.55f, 1f, distance)) * color.a;
                    pixels[y * width + x] = new Color(color.r, color.g, color.b, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();

            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = name;
            return sprite;
        }

        private void OnPlayerActionResolved(BattleActionResolution resolution)
        {
            if (resolution.DamageDealt > 0)
            {
                playerVisual.PlayAttack(Vector3.right, true);
                bossVisual.PlayHurt(resolution.DamageDealt >= 12);
                StartCoroutine(ScreenShake());
            }
            else if (resolution.HealingDone > 0)
            {
                playerVisual.PlaySupportPulse(new Color(0.45f, 0.9f, 0.58f));
            }
            else if (resolution.ShieldGained > 0)
            {
                playerVisual.PlaySupportPulse(new Color(0.5f, 0.76f, 0.98f));
            }
        }

        private void OnBossActionResolved(BattleActionResolution resolution)
        {
            if (resolution.DamageDealt > 0)
            {
                bossVisual.PlayAttack(Vector3.left, true);
                playerVisual.PlayHurt(resolution.DamageDealt >= 12);
                StartCoroutine(ScreenShake());
            }
            else if (resolution.ShieldGained > 0 || resolution.HealingDone > 0)
            {
                bossVisual.PlaySupportPulse(resolution.HealingDone > 0
                    ? new Color(0.45f, 0.9f, 0.58f)
                    : new Color(0.5f, 0.76f, 0.98f));
            }
        }

        private void OnBattleEnded(BattleResult result)
        {
            if (result.ResultType == BattleResultType.Victory)
            {
                bossVisual.PlayDeath();
            }
            else
            {
                playerVisual.PlayDeath();
            }
        }

        private IEnumerator ScreenShake()
        {
            if (isShaking || battleCamera == null)
            {
                yield break;
            }

            isShaking = true;
            float elapsed = 0f;

            while (elapsed < screenShakeDuration)
            {
                float x = Random.Range(-1f, 1f) * screenShakeMagnitude;
                float y = Random.Range(-1f, 1f) * screenShakeMagnitude;
                battleCamera.transform.position = cameraOriginalPosition + new Vector3(x, y, 0f);
                elapsed += Time.deltaTime;
                yield return null;
            }

            battleCamera.transform.position = cameraOriginalPosition;
            isShaking = false;
        }

        private void UnsubscribeFromSimulation()
        {
            if (subscribedSimulation == null)
            {
                return;
            }

            subscribedSimulation.PlayerActionResolved -= OnPlayerActionResolved;
            subscribedSimulation.BossActionResolved -= OnBossActionResolved;
            subscribedSimulation.BattleEnded -= OnBattleEnded;
            subscribedSimulation = null;
        }
    }
}
