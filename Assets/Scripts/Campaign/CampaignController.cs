using System;
using System.Collections.Generic;
using UnityEngine;
using Visha.Battle;
using Visha.Enemies;

namespace Visha.Campaign
{
    /// <summary>What happens to the player's health when moving on to the next encounter.</summary>
    public enum PlayerHealthBetweenEncounters
    {
        /// <summary>The player starts every encounter at full health.</summary>
        RestoreToFull,
        /// <summary>The player keeps the health they finished the previous encounter with.</summary>
        CarryOver
    }

    /// <summary>
    /// Connects the battles into a demo: start screen, encounters in order, Next
    /// after victory, Retry after defeat, and Restart after the final encounter.
    ///
    /// It applies no damage and runs no turns. Every encounter, whether new or
    /// retried, starts through <see cref="BattleOutcomeController.Retry"/>, which
    /// revives both combatants, clears poison and the enemy pattern, and fires
    /// OnRetry so CombatFlowController resets Fade and its turn state. This
    /// controller then sets the encounter's starting health on top of that.
    ///
    /// UI calls <see cref="StartCampaign"/>, <see cref="NextEncounter"/>,
    /// <see cref="RetryEncounter"/> and <see cref="RestartCampaign"/>, and shows
    /// screens from <see cref="StateChanged"/>.
    /// </summary>
    public class CampaignController : MonoBehaviour
    {
        [Header("Battle systems")]
        [SerializeField] private BattleOutcomeController outcomeController;
        [SerializeField] private Health playerHealth;
        [SerializeField] private Health enemyHealth;
        [SerializeField] private EnemyPatternRunner enemyPattern;
        [Tooltip("Optional. Receives each encounter's enemy sprite.")]
        [SerializeField] private SpriteRenderer enemySpriteRenderer;

        [Header("Campaign")]
        [Tooltip("Encounters in the order they are played.")]
        [SerializeField] private List<EncounterConfig> encounters = new List<EncounterConfig>();
        [SerializeField] private PlayerHealthBetweenEncounters playerHealthRule = PlayerHealthBetweenEncounters.RestoreToFull;

        private CampaignProgress progress;
        private int playerStartHealth;
        private int enemyStartHealth;
        public int HighestCompletedEncounter { get; private set; } = -1;
        public bool CanEnterEncounter(int index) => State != CampaignState.InBattle && index >= 0
            && index < EncounterCount && index <= HighestCompletedEncounter + 1;
        public bool EnterEncounter(int index)
        {
            if (!CanEnterEncounter(index) || !Progress.Select(index)) return false;
            BeginEncounter(fullPlayerHealth: true);
            return true;
        }

        /// <summary>Fires after every campaign state change, for screens to update.</summary>
        public event Action<CampaignState> StateChanged;

        private CampaignProgress Progress
        {
            get
            {
                if (progress == null && encounters.Count > 0)
                {
                    progress = new CampaignProgress(encounters.Count);
                    progress.StateChanged += HandleProgressChanged;
                }
                return progress;
            }
        }

        public CampaignState State => Progress?.State ?? CampaignState.Title;
        public int EncounterNumber => Progress?.EncounterNumber ?? 0;
        public int EncounterCount => encounters.Count;
        public EncounterConfig CurrentEncounter => Progress != null ? encounters[Progress.EncounterIndex] : null;
        public PlayerHealthBetweenEncounters PlayerHealthRule
        {
            get => playerHealthRule;
            set => playerHealthRule = value;
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        /// <summary>Assigns every reference at runtime. Safe to call before or after OnEnable.</summary>
        public void SetReferences(BattleOutcomeController outcome, Health player, Health enemy,
                                  EnemyPatternRunner pattern, IList<EncounterConfig> encounterList)
        {
            if (isActiveAndEnabled) Unsubscribe();

            outcomeController = outcome;
            playerHealth = player;
            enemyHealth = enemy;
            enemyPattern = pattern;
            encounters = new List<EncounterConfig>(encounterList);
            if (progress != null) progress.StateChanged -= HandleProgressChanged;
            progress = null;
            HighestCompletedEncounter = -1;

            if (isActiveAndEnabled) Subscribe();
        }

        private void Subscribe()
        {
            if (outcomeController == null) return;
            outcomeController.OnBattleEnded += HandleBattleEnded;
            outcomeController.OnRetry += HandleEncounterReset;
        }

        private void Unsubscribe()
        {
            if (outcomeController == null) return;
            outcomeController.OnBattleEnded -= HandleBattleEnded;
            outcomeController.OnRetry -= HandleEncounterReset;
        }

        /// <summary>Start button: leaves the title screen and loads the first encounter.</summary>
        public void StartCampaign()
        {
            if (!HasEncounters() || !Progress.Start()) return;
            BeginEncounter(fullPlayerHealth: true);
        }

        /// <summary>Next button: loads the following encounter. Only works after a victory.</summary>
        public void NextEncounter()
        {
            if (!HasEncounters() || !Progress.Next()) return;
            BeginEncounter(fullPlayerHealth: playerHealthRule == PlayerHealthBetweenEncounters.RestoreToFull);
        }

        /// <summary>Retry button: replays the current encounter from its start. Only works after a defeat.</summary>
        public void RetryEncounter()
        {
            if (!HasEncounters() || State != CampaignState.Defeat || outcomeController == null) return;
            // HandleEncounterReset moves the campaign back to InBattle, so a retry
            // wired straight to BattleOutcomeController.Retry() behaves the same.
            outcomeController.Retry();
        }

        /// <summary>Restart button: a fresh campaign from encounter 1 at full health.</summary>
        public void RestartCampaign()
        {
            if (!HasEncounters()) return;
            HighestCompletedEncounter = -1;
            Progress.Restart();
            BeginEncounter(fullPlayerHealth: true);
        }

        /// <summary>Back to the start screen with a fresh campaign. The battle stays idle underneath.</summary>
        public void ReturnToTitle()
        {
            if (!HasEncounters()) return;
            HighestCompletedEncounter = -1;
            Progress.ReturnToTitle();
        }

        private bool HasEncounters()
        {
            if (encounters.Count > 0 && !encounters.Contains(null)) return true;
            Debug.LogError("CampaignController: assign at least one encounter and leave no empty slots");
            return false;
        }

        private void BeginEncounter(bool fullPlayerHealth)
        {
            EncounterConfig config = CurrentEncounter;

            if (enemyPattern != null) enemyPattern.Configure(config.Pattern, config.DisplayName, config.OpeningOffset);
            if (enemyHealth != null) enemyHealth.SetMaxHealth(config.MaxHealth);
            if (enemySpriteRenderer != null && config.EnemySprite != null) enemySpriteRenderer.sprite = config.EnemySprite;

            // Remembered so a later retry restarts the encounter exactly as it began.
            playerStartHealth = playerHealth == null ? 0
                : fullPlayerHealth ? playerHealth.MaxHealth : playerHealth.CurrentHealth;
            enemyStartHealth = config.StartingHealth;

            Debug.Log($"Campaign: encounter {EncounterNumber}/{EncounterCount} - {config.DisplayName}");

            if (outcomeController != null) outcomeController.Retry();
            else HandleEncounterReset();
        }

        private void HandleEncounterReset()
        {
            if (Progress == null) return;
            if (Progress.State == CampaignState.Defeat) Progress.Retry();

            SetHealth(playerHealth, playerStartHealth);
            SetHealth(enemyHealth, enemyStartHealth);
        }

        private void HandleBattleEnded(BattleOutcome outcome)
        {
            if (Progress == null || !isActiveAndEnabled) return;
            if (State == CampaignState.InBattle && outcome == BattleOutcome.Victory)
                HighestCompletedEncounter = Mathf.Max(HighestCompletedEncounter, Progress.EncounterIndex);
            Progress.ReportOutcome(outcome);
        }

        private void HandleProgressChanged(CampaignState state)
        {
            Debug.Log($"Campaign: {state} (encounter {EncounterNumber}/{EncounterCount})");
            StateChanged?.Invoke(state);
        }

        /// <summary>
        /// Lowers already-revived health to the encounter's starting value.
        /// Health has no setter for current health, so this applies the difference
        /// as damage; the target never drops below 1.
        /// </summary>
        private static void SetHealth(Health health, int value)
        {
            if (health == null || value <= 0) return;
            int target = Mathf.Clamp(value, 1, health.MaxHealth);
            if (health.IsDead) health.ResetHealth();
            int difference = health.CurrentHealth - target;
            if (difference > 0) health.TakeDamage(difference);
        }
    }
}
