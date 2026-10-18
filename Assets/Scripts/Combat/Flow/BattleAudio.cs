using UnityEngine;
using Visha.Battle;
using Visha.UI;

namespace Visha.Combat.Flow
{
    public sealed class BattleAudio : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private CombatFlowController combatFlow;
        [SerializeField] private BattleOutcomeController outcomeController;
        [SerializeField] private Health playerHealth;
        [SerializeField] private Health enemyHealth;
        [SerializeField] private BattleFeedback enemyFeedback;
        [SerializeField] private AudioClip hitClip;
        [SerializeField] private AudioClip venomStrikeClip;
        [SerializeField] private AudioClip serpentsBiteClip;
        [SerializeField] private AudioClip fadeClip;
        [SerializeField] private AudioClip victoryClip;
        [SerializeField] private AudioClip defeatClip;
        [SerializeField, Range(0f, 1f)] private float volume = 0.52f;

        private int previousPlayerHealth = -1;
        private int previousEnemyHealth = -1;

        private void OnEnable()
        {
            if (playerHealth != null) playerHealth.OnHealthChanged += HandlePlayerHealthChanged;
            if (enemyHealth != null) enemyHealth.OnHealthChanged += HandleEnemyHealthChanged;
            if (combatFlow != null) combatFlow.OnAbilityUsed += HandleAbilityUsed;
            if (outcomeController != null) outcomeController.OnBattleEnded += HandleBattleEnded;
        }

        private void Start()
        {
            if (playerHealth != null) previousPlayerHealth = playerHealth.CurrentHealth;
            if (enemyHealth != null) previousEnemyHealth = enemyHealth.CurrentHealth;
            if (audioSource != null)
            {
                audioSource.playOnAwake = false;
                audioSource.loop = false;
                audioSource.spatialBlend = 0f;
                audioSource.volume = volume;
            }
        }

        private void OnDisable()
        {
            if (playerHealth != null) playerHealth.OnHealthChanged -= HandlePlayerHealthChanged;
            if (enemyHealth != null) enemyHealth.OnHealthChanged -= HandleEnemyHealthChanged;
            if (combatFlow != null) combatFlow.OnAbilityUsed -= HandleAbilityUsed;
            if (outcomeController != null) outcomeController.OnBattleEnded -= HandleBattleEnded;
            if (audioSource != null) audioSource.Stop();
        }

        private void HandlePlayerHealthChanged(int current, int maximum)
        {
            if (previousPlayerHealth >= 0 && current < previousPlayerHealth) Play(hitClip);
            previousPlayerHealth = current;
        }

        private void HandleEnemyHealthChanged(int current, int maximum)
        {
            if (previousEnemyHealth >= 0 && current < previousEnemyHealth) Play(hitClip);
            previousEnemyHealth = current;
        }

        private void HandleAbilityUsed(BattleAbility ability)
        {
            switch (ability)
            {
                case BattleAbility.VenomStrike:
                    Play(venomStrikeClip);
                    break;
                case BattleAbility.SerpentsBite:
                    if (enemyFeedback != null) enemyFeedback.PlayBiteEffect();
                    Play(serpentsBiteClip);
                    break;
                case BattleAbility.Fade:
                    Play(fadeClip);
                    break;
            }
        }

        private void HandleBattleEnded(BattleOutcome outcome)
        {
            if (outcome == BattleOutcome.Victory) Play(victoryClip);
            else if (outcome == BattleOutcome.Defeat) Play(defeatClip);
        }

        private void Play(AudioClip clip)
        {
            if (audioSource == null || clip == null) return;
            audioSource.Stop();
            audioSource.clip = clip;
            audioSource.volume = volume;
            audioSource.Play();
        }
    }
}
