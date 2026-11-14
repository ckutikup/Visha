using UnityEngine;
using Visha.Battle;
using Visha.UI;

namespace Visha.Combat.Flow
{
    public sealed class SanctumPresentation : MonoBehaviour
    {
        [SerializeField] private TempleBlessing blessing;
        [SerializeField] private SpriteRenderer playerRenderer;
        [SerializeField] private SpriteRenderer guardianRenderer;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip blessingClip;
        [SerializeField] private AudioClip phaseChangeClip;
        [SerializeField] private AudioClip bossAttackClip;
        [SerializeField] private float blessingDuration = .65f;
        [SerializeField] private float phaseDuration = .75f;

        private CombatFlowController combatFlow;
        private BattleOutcomeController outcome;
        private SpriteRenderer playerGlow, bossGlow;
        private float playerUntil, bossUntil, playerDuration, bossDuration;
        private Color bossColor;
        private int previousBossPhase;
        private BattlePhase previousTurn = BattlePhase.Waiting;

        public void Configure(CombatFlowController flow)
        {
            if (combatFlow != null) combatFlow.OnStateUpdated -= HandleStateUpdated;
            combatFlow = flow;
            ResetEffects();
            if (isActiveAndEnabled && combatFlow != null)
                combatFlow.OnStateUpdated += HandleStateUpdated;
        }

        private void OnEnable()
        {
            if (blessing != null) blessing.OnBlessingTriggered += HandleBlessingTriggered;
            outcome = FindAnyObjectByType<BattleOutcomeController>();
            if (outcome != null) outcome.OnRetry += ResetEffects;
            Configure(combatFlow != null ? combatFlow : FindAnyObjectByType<CombatFlowController>());
        }

        private void OnDisable()
        {
            if (blessing != null) blessing.OnBlessingTriggered -= HandleBlessingTriggered;
            if (outcome != null) outcome.OnRetry -= ResetEffects;
            if (combatFlow != null) combatFlow.OnStateUpdated -= HandleStateUpdated;
            ResetEffects();
        }

        private void HandleStateUpdated(BattleViewState state)
        {
            if (state == null) return;
            if (state.bossPhase <= 0 || state.phase == BattlePhase.Waiting ||
                state.phase == BattlePhase.Victory || state.phase == BattlePhase.Defeat)
            {
                ResetEffects();
                return;
            }
            if (previousBossPhase > state.bossPhase) ResetEffects();
            if (previousBossPhase > 0 && state.bossPhase > previousBossPhase)
                PlayPhaseChange();
            if (state.phase == BattlePhase.EnemyTurn && previousTurn != BattlePhase.EnemyTurn && state.incomingDamage > 0)
                PlayBossAttack();
            previousBossPhase = state.bossPhase;
            previousTurn = state.phase;
        }

        private void HandleBlessingTriggered()
        {
            if (!isActiveAndEnabled) return;
            playerDuration = Mathf.Max(.01f, blessingDuration);
            playerUntil = Time.time + playerDuration;
            Play(blessingClip);
        }

        public void PlayBossAttack()
        {
            if (!isActiveAndEnabled) return;
            // Let a phase-change cue finish instead of replacing it with an attack pulse.
            if (Time.time >= bossUntil)
                StartBossGlow(.2f, new Color(1f, .55f, .32f));
            Play(bossAttackClip);
        }

        public void PlayPhaseChange()
        {
            if (!isActiveAndEnabled) return;
            StartBossGlow(phaseDuration, new Color(.55f, 1f, .72f));
            Play(phaseChangeClip);
        }

        private void StartBossGlow(float duration, Color color)
        {
            bossDuration = Mathf.Max(.01f, duration);
            bossUntil = Time.time + bossDuration;
            bossColor = color;
        }

        private void LateUpdate()
        {
            UpdateGlow(ref playerGlow, playerRenderer, playerUntil, playerDuration, new Color(1f, .9f, .35f));
            UpdateGlow(ref bossGlow, guardianRenderer, bossUntil, bossDuration, bossColor);
        }

        private void UpdateGlow(ref SpriteRenderer glow, SpriteRenderer actor, float until, float duration, Color color)
        {
            bool visible = actor != null && actor.enabled && actor.gameObject.activeInHierarchy && Time.time < until;
            if (!visible)
            {
                if (glow != null) glow.enabled = false;
                return;
            }
            if (glow == null)
            {
                var go = new GameObject("SanctumGlow", typeof(SpriteRenderer));
                go.transform.SetParent(actor.transform, false);
                glow = go.GetComponent<SpriteRenderer>();
            }
            // A separate renderer leaves hit flashes, Fade alpha and idle movement untouched.
            glow.sprite = actor.sprite;
            glow.sharedMaterial = actor.sharedMaterial;
            glow.sortingLayerID = actor.sortingLayerID;
            glow.sortingOrder = actor.sortingOrder + 1;
            glow.flipX = actor.flipX;
            glow.flipY = actor.flipY;
            float progress = 1f - Mathf.Clamp01((until - Time.time) / duration);
            float pulse = Mathf.Sin(progress * Mathf.PI);
            color.a = pulse * .45f * actor.color.a;
            glow.color = color;
            glow.transform.localScale = Vector3.one * (1f + pulse * .035f);
            glow.enabled = true;
        }

        private void ResetEffects()
        {
            playerUntil = bossUntil = 0;
            previousBossPhase = 0;
            previousTurn = BattlePhase.Waiting;
            if (playerGlow != null) playerGlow.enabled = false;
            if (bossGlow != null) bossGlow.enabled = false;
        }

        private void OnDestroy()
        {
            if (playerGlow != null) Destroy(playerGlow.gameObject);
            if (bossGlow != null) Destroy(bossGlow.gameObject);
        }

        private void Play(AudioClip clip)
        {
            if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
        }
    }
}
