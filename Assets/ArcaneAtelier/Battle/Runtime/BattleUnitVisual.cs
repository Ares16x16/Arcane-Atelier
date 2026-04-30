using System.Collections;
using UnityEngine;

namespace ArcaneAtelier.Battle
{
    public sealed class BattleUnitVisual : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        [SerializeField] private float idleAmplitude = 0.08f;
        [SerializeField] private float idleFrequency = 2.5f;
        [SerializeField] private float attackDistance = 1.2f;
        [SerializeField] private float attackDuration = 0.25f;
        [SerializeField] private float hurtDuration = 0.3f;
        [SerializeField] private float deathDuration = 0.8f;
        [SerializeField] private float heavyHurtScalePulse = 0.08f;

        private Vector3 originalPosition;
        private Vector3 originalScale;
        private Color originalColor;
        private bool isAnimating;
        private Coroutine idleCoroutine;

        public SpriteRenderer SpriteRenderer => spriteRenderer;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
            originalPosition = transform.position;
            originalScale = transform.localScale;
        }

        private void OnEnable()
        {
            StartIdle();
        }

        private void OnDisable()
        {
            StopIdle();
        }

        public void Setup(Sprite sprite, Color color, Vector3 scale)
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            spriteRenderer.sprite = sprite;
            spriteRenderer.color = color;
            transform.localScale = scale;

            originalPosition = transform.position;
            originalScale = scale;
            originalColor = color;
            StartIdle();
        }

        public void StartIdle()
        {
            StopIdle();
            idleCoroutine = StartCoroutine(IdleRoutine());
        }

        public void StopIdle()
        {
            if (idleCoroutine != null)
            {
                StopCoroutine(idleCoroutine);
                idleCoroutine = null;
            }
        }

        public void PlayAttack(Vector3 direction, bool emphasize)
        {
            if (isAnimating)
            {
                return;
            }

            StopIdle();
            StartCoroutine(AttackRoutine(direction, emphasize));
        }

        public void PlayHurt(bool heavy)
        {
            if (isAnimating)
            {
                return;
            }

            StopIdle();
            StartCoroutine(HurtRoutine(heavy));
        }

        public void PlaySupportPulse(Color pulseColor)
        {
            if (isAnimating)
            {
                return;
            }

            StopIdle();
            StartCoroutine(SupportPulseRoutine(pulseColor));
        }

        public void PlayDeath()
        {
            if (isAnimating)
            {
                return;
            }

            StopIdle();
            StartCoroutine(DeathRoutine());
        }

        private IEnumerator IdleRoutine()
        {
            while (true)
            {
                float y = Mathf.Sin(Time.time * idleFrequency) * idleAmplitude;
                transform.position = originalPosition + new Vector3(0f, y, 0f);
                yield return null;
            }
        }

        private IEnumerator AttackRoutine(Vector3 direction, bool emphasize)
        {
            isAnimating = true;
            float distance = emphasize ? attackDistance * 1.1f : attackDistance;
            Vector3 targetPos = originalPosition + direction.normalized * distance;
            float halfDuration = attackDuration * 0.5f;
            float elapsed = 0f;

            while (elapsed < halfDuration)
            {
                float t = elapsed / halfDuration;
                transform.position = Vector3.Lerp(originalPosition, targetPos, t);
                transform.localScale = Vector3.Lerp(originalScale, originalScale * (emphasize ? 1.04f : 1.02f), t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.position = targetPos;
            elapsed = 0f;

            while (elapsed < halfDuration)
            {
                float t = elapsed / halfDuration;
                transform.position = Vector3.Lerp(targetPos, originalPosition, t);
                transform.localScale = Vector3.Lerp(originalScale * (emphasize ? 1.04f : 1.02f), originalScale, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.position = originalPosition;
            transform.localScale = originalScale;
            isAnimating = false;
            StartIdle();
        }

        private IEnumerator HurtRoutine(bool heavy)
        {
            isAnimating = true;
            float elapsed = 0f;
            float scalePulse = heavy ? heavyHurtScalePulse : heavyHurtScalePulse * 0.5f;

            while (elapsed < hurtDuration)
            {
                float x = Mathf.Sin(elapsed * 60f) * (heavy ? 0.12f : 0.08f);
                transform.position = originalPosition + new Vector3(x, 0f, 0f);
                transform.localScale = originalScale * (1f + Mathf.Sin(elapsed * 24f) * scalePulse);

                if (spriteRenderer != null)
                {
                    float flash = Mathf.PingPong(elapsed * (heavy ? 18f : 12f), 1f);
                    spriteRenderer.color = Color.Lerp(originalColor, Color.white, heavy ? flash : flash * 0.8f);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.position = originalPosition;
            transform.localScale = originalScale;

            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }

            isAnimating = false;
            StartIdle();
        }

        private IEnumerator SupportPulseRoutine(Color pulseColor)
        {
            isAnimating = true;
            float duration = 0.28f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float progress = elapsed / duration;
                float pulse = Mathf.Sin(progress * Mathf.PI);
                transform.localScale = originalScale * (1f + pulse * 0.05f);
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = Color.Lerp(originalColor, pulseColor, pulse * 0.85f);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.localScale = originalScale;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }

            isAnimating = false;
            StartIdle();
        }

        private IEnumerator DeathRoutine()
        {
            isAnimating = true;
            float elapsed = 0f;

            while (elapsed < deathDuration)
            {
                float t = elapsed / deathDuration;
                transform.localScale = originalScale * Mathf.Max(0.1f, 1f - t);

                if (spriteRenderer != null)
                {
                    Color c = originalColor;
                    c.a = 1f - t;
                    spriteRenderer.color = c;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.localScale = Vector3.zero;
            gameObject.SetActive(false);
            isAnimating = false;
        }
    }
}
