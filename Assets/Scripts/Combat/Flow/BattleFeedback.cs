using System.Collections;
using UnityEngine;

namespace Visha.Combat.Flow
{
    public sealed class BattleFeedback : MonoBehaviour
    {
        [SerializeField] private Health health;
        [SerializeField] private Poison poison;
        [SerializeField] private Fade fade;
        [SerializeField] private SpriteRenderer characterRenderer;
        [SerializeField] private SpriteRenderer poisonMark;
        [SerializeField] private SpriteRenderer fadeAura;
        [SerializeField] private SpriteRenderer biteFlash;

        private Color originalColor = Color.white;
        private int previousHealth;
        private Vector3 poisonScale;
        private Vector3 fadeScale;
        private Vector3 biteScale;
        private Coroutine hitRoutine;
        private Coroutine biteRoutine;

        private void Awake()
        {
            if (health == null) health = GetComponent<Health>();
            if (poison == null) poison = GetComponent<Poison>();
            if (fade == null) fade = GetComponent<Fade>();
            if (characterRenderer == null) characterRenderer = GetComponentInChildren<SpriteRenderer>();
            if (characterRenderer != null) originalColor = characterRenderer.color;
            if (poisonMark != null) poisonScale = poisonMark.transform.localScale;
            if (fadeAura != null) fadeScale = fadeAura.transform.localScale;
            if (biteFlash != null)
            {
                biteScale = biteFlash.transform.localScale;
                biteFlash.enabled = false;
            }
        }

        private void OnEnable()
        {
            if (health != null) health.OnHealthChanged += HandleHealthChanged;
            if (poison != null) poison.OnPoisonChanged += HandlePoisonChanged;
            if (fade != null) fade.OnFadeChanged += HandleFadeChanged;
            RefreshEffects();
        }

        private void Start()
        {
            if (health != null) previousHealth = health.CurrentHealth;
            RefreshEffects();
        }

        private void OnDisable()
        {
            if (health != null) health.OnHealthChanged -= HandleHealthChanged;
            if (poison != null) poison.OnPoisonChanged -= HandlePoisonChanged;
            if (fade != null) fade.OnFadeChanged -= HandleFadeChanged;
            if (hitRoutine != null) StopCoroutine(hitRoutine);
            if (biteRoutine != null) StopCoroutine(biteRoutine);
            if (characterRenderer != null) characterRenderer.color = originalColor;
            if (biteFlash != null) biteFlash.enabled = false;
        }

        private void Update()
        {
            float time = Time.unscaledTime;
            if (poisonMark != null && poisonMark.enabled)
                poisonMark.transform.localScale = poisonScale * (1f + Mathf.Sin(time * 6f) * 0.07f);
            if (fadeAura != null && fadeAura.enabled)
                fadeAura.transform.localScale = fadeScale * (1f + Mathf.Sin(time * 4.5f) * 0.035f);
        }

        private void LateUpdate()
        {
            if (characterRenderer == null) return;
            Color color = characterRenderer.color;
            color.a = health != null && health.IsDead ? .3f : fade != null && fade.IsActive ? .58f : originalColor.a;
            characterRenderer.color = color;
        }

        public void PlayBiteEffect()
        {
            if (biteFlash == null || !isActiveAndEnabled) return;
            if (biteRoutine != null) StopCoroutine(biteRoutine);
            biteRoutine = StartCoroutine(BiteRoutine());
        }

        public void PlayHitFeedback()
        {
            if (isActiveAndEnabled) PlayHitFlash();
        }

        private void HandleHealthChanged(int current, int maximum)
        {
            if (current < previousHealth) PlayHitFlash();
            else if (current > previousHealth)
            {
                if (hitRoutine != null) StopCoroutine(hitRoutine);
                if (biteRoutine != null) StopCoroutine(biteRoutine);
                hitRoutine = biteRoutine = null;
                if (characterRenderer != null) characterRenderer.color = originalColor;
                if (biteFlash != null) biteFlash.enabled = false;
            }
            previousHealth = current;
        }

        private void HandlePoisonChanged(int stacks)
        {
            if (poisonMark != null) poisonMark.enabled = stacks > 0;
        }

        private void HandleFadeChanged()
        {
            if (fadeAura != null) fadeAura.enabled = fade != null && fade.IsActive;
        }

        private void PlayHitFlash()
        {
            if (characterRenderer == null) return;
            if (hitRoutine != null) StopCoroutine(hitRoutine);
            hitRoutine = StartCoroutine(HitRoutine());
        }

        private IEnumerator HitRoutine()
        {
            const float duration = 0.32f;
            float elapsed = 0f;
            Color hitColor = new Color(1f, 0.34f, 0.28f, 1f);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float strength = Mathf.Sin(Mathf.Clamp01(elapsed / duration) * Mathf.PI) * 0.9f;
                characterRenderer.color = Color.Lerp(originalColor, hitColor, strength);
                yield return null;
            }
            characterRenderer.color = originalColor;
            hitRoutine = null;
        }

        private IEnumerator BiteRoutine()
        {
            const float duration = 0.38f;
            float elapsed = 0f;
            biteFlash.enabled = true;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                float pulse = Mathf.Sin(progress * Mathf.PI);
                biteFlash.color = new Color(1f, 1f, 1f, pulse);
                biteFlash.transform.localScale = biteScale * (0.8f + pulse * 0.28f);
                yield return null;
            }
            biteFlash.enabled = false;
            biteFlash.color = Color.white;
            biteFlash.transform.localScale = biteScale;
            biteRoutine = null;
        }

        private void RefreshEffects()
        {
            if (poisonMark != null) poisonMark.enabled = poison != null && poison.PoisonStacks > 0;
            if (fadeAura != null) fadeAura.enabled = fade != null && fade.IsActive;
        }
    }
}
