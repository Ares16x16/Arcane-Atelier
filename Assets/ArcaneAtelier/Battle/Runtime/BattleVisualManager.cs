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

        [Header("Fallback Sprites")]
        [SerializeField] private Sprite playerSprite;
        [SerializeField] private Sprite bossSprite;
        [SerializeField] private Sprite backgroundSprite;

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

        public Camera BattleCamera => battleCamera;
        public BattleUnitVisual PlayerVisual => playerVisual;
        public BattleUnitVisual BossVisual => bossVisual;

        public void Initialize(
            BattleSimulation simulation,
            BattleUnit player,
            BattleUnit boss,
            BattleContentDatabase contentDatabase,
            string bossId)
        {
            activePresentationProfile = contentDatabase != null
                ? contentDatabase.FindPresentationProfile(bossId)
                : null;
            EnsureCamera();
            EnsureBackground();
            EnsurePlayerVisual();
            EnsureBossVisual();

            subscribedSimulation = simulation;
            simulation.PlayerActionResolved += OnPlayerActionResolved;
            simulation.BossActionResolved += OnBossActionResolved;
            simulation.BattleEnded += OnBattleEnded;
        }

        private void OnDestroy()
        {
            if (subscribedSimulation != null)
            {
                subscribedSimulation.PlayerActionResolved -= OnPlayerActionResolved;
                subscribedSimulation.BossActionResolved -= OnBossActionResolved;
                subscribedSimulation.BattleEnded -= OnBattleEnded;
            }
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
            if (playerVisual != null)
            {
                if (playerSprite != null)
                {
                    playerVisual.transform.position = DefaultPlayerPosition;
                    playerVisual.Setup(playerSprite, playerColor, DefaultPlayerScale);
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
            playerVisual.Setup(sprite, playerColor, DefaultPlayerScale);
        }

        private void EnsureBossVisual()
        {
            Sprite resolvedBossSprite = ResolveBossSprite();
            Vector3 bossPosition = ResolveBossPosition();
            Vector3 bossScale = ResolveBossScale();

            if (bossVisual != null)
            {
                bossVisual.transform.position = bossPosition;
                if (resolvedBossSprite != null)
                {
                    bossVisual.Setup(resolvedBossSprite, bossColor, bossScale);
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
            bossVisual.Setup(sprite, bossColor, bossScale);
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

        private void OnPlayerActionResolved(BattleActionResolution resolution)
        {
            if (resolution.DamageDealt > 0)
            {
                playerVisual.PlayAttack(Vector3.right);
                bossVisual.PlayHurt();
                StartCoroutine(ScreenShake());
            }
        }

        private void OnBossActionResolved(BattleActionResolution resolution)
        {
            if (resolution.DamageDealt > 0)
            {
                bossVisual.PlayAttack(Vector3.left);
                playerVisual.PlayHurt();
                StartCoroutine(ScreenShake());
            }
            else if (resolution.ShieldGained > 0 || resolution.HealingDone > 0)
            {
                bossVisual.PlayHurt();
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
    }
}
