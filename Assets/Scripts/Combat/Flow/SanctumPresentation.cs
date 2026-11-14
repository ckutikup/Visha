using System.Collections;
using UnityEngine;
using Visha.UI;

namespace Visha.Combat.Flow
{
    /// <summary>
    /// Presentation-only feedback for the Sanctum/final boss encounter.
    /// Does not alter combat values, progression, or Blessing behaviour.
    /// </summary>
    public sealed class SanctumPresentation : MonoBehaviour
    {
        [Header("Gameplay")]
        [SerializeField] private TempleBlessing blessing;

        [Header("Actors")]
        [SerializeField] private SpriteRenderer playerRenderer;
        [SerializeField] private SpriteRenderer guardianRenderer;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip blessingClip;
        [SerializeField] private AudioClip phaseChangeClip;
        [SerializeField] private AudioClip bossAttackClip;

        [Header("Timing")]
        [SerializeField] private float blessingDuration = 0.65f;
        [SerializeField] private float phaseDuration = 0.75f;

        private Coroutine blessingRoutine;
        private Coroutine phaseRoutine;
        private CombatFlowController combatFlow;
        private int previousBossPhase;

        public void Configure(CombatFlowController flow)
        {
            if (combatFlow != null)
                combatFlow.OnStateUpdated -= HandleStateUpdated;

            combatFlow = flow;
            previousBossPhase = 0;

            if (combatFlow != null)
                combatFlow.OnStateUpdated += HandleStateUpdated;
        }

        private void OnEnable()
        {
            if (blessing != null)
                blessing.OnBlessingTriggered += HandleBlessingTriggered;

            if (combatFlow != null)
                combatFlow.OnStateUpdated += HandleStateUpdated;
        }

        private void OnDisable()
        {
            if (blessing != null)
                blessing.OnBlessingTriggered -= HandleBlessingTriggered;

            if (combatFlow != null)
                combatFlow.OnStateUpdated -= HandleStateUpdated;
        }

        private void Start()
        {
            if (audioSource == null)
                return;

            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
        }

        private void HandleStateUpdated(BattleViewState state)
        {
            if (state == null)
                return;

            if (previousBossPhase > 0 &&
                state.bossPhase > previousBossPhase)
            {
                PlayPhaseChange();
            }

            previousBossPhase = state.bossPhase;
        }

        private void HandleBlessingTriggered()
        {
            if (blessingRoutine != null)
                StopCoroutine(blessingRoutine);

            blessingRoutine = StartCoroutine(BlessingEffect());
            Play(blessingClip);
        }

        public void PlayBossAttack()
        {
            Play(bossAttackClip);

            if (guardianRenderer != null)
                StartCoroutine(BossAttackPulse());
        }

        public void PlayPhaseChange()
        {
            if (phaseRoutine != null)
                StopCoroutine(phaseRoutine);

            phaseRoutine = StartCoroutine(PhaseChangeEffect());
            Play(phaseChangeClip);
        }

        private IEnumerator BlessingEffect()
        {
            if (playerRenderer == null)
                yield break;

            Color originalColor = playerRenderer.color;
            Vector3 originalScale = playerRenderer.transform.localScale;

            float elapsed = 0f;

            while (elapsed < blessingDuration)
            {
                elapsed += Time.deltaTime;

                float t = Mathf.Clamp01(elapsed / blessingDuration);
                float pulse = Mathf.Sin(t * Mathf.PI * 4f) * 0.08f;

                playerRenderer.color = Color.Lerp(
                    originalColor,
                    new Color(1f, 0.9f, 0.35f, 1f),
                    Mathf.Sin(t * Mathf.PI)
                );

                playerRenderer.transform.localScale =
                    originalScale * (1f + pulse);

                yield return null;
            }

            playerRenderer.color = originalColor;
            playerRenderer.transform.localScale = originalScale;

            blessingRoutine = null;
        }

        private IEnumerator BossAttackPulse()
        {
            if (guardianRenderer == null)
                yield break;

            Color originalColor = guardianRenderer.color;
            Vector3 originalScale = guardianRenderer.transform.localScale;

            guardianRenderer.color =
                new Color(1f, 0.55f, 0.32f, 1f);

            guardianRenderer.transform.localScale =
                originalScale * 1.06f;

            yield return new WaitForSeconds(0.12f);

            guardianRenderer.color = originalColor;
            guardianRenderer.transform.localScale = originalScale;
        }

        private IEnumerator PhaseChangeEffect()
        {
            if (guardianRenderer == null)
                yield break;

            Color originalColor = guardianRenderer.color;
            Vector3 originalScale = guardianRenderer.transform.localScale;

            float elapsed = 0f;

            while (elapsed < phaseDuration)
            {
                elapsed += Time.deltaTime;

                float t = Mathf.Clamp01(elapsed / phaseDuration);
                float pulse = Mathf.Sin(t * Mathf.PI * 6f);

                guardianRenderer.color = Color.Lerp(
                    originalColor,
                    new Color(0.55f, 1f, 0.72f, 1f),
                    Mathf.Abs(pulse)
                );

                guardianRenderer.transform.localScale =
                    originalScale *
                    (1f + Mathf.Abs(pulse) * 0.07f);

                yield return null;
            }

            guardianRenderer.color = originalColor;
            guardianRenderer.transform.localScale = originalScale;

            phaseRoutine = null;
        }

        private void Play(AudioClip clip)
        {
            if (audioSource == null || clip == null)
                return;

            audioSource.PlayOneShot(clip);
        }
    }
}