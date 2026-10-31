using UnityEngine;
using Visha.Battle;
using Visha.Enemies;

namespace Visha.Campaign
{
    /// <summary>
    /// Adds the two-enemy Vault to the campaign without changing
    /// <see cref="CampaignController"/>. The Vault is listed on the campaign as an
    /// ordinary <see cref="EncounterConfig"/> describing its first enemy (the lineup's
    /// target id 0), so unlocking, the temple map, Next, Retry, Replay and Restart all
    /// work unchanged. This component adds the second enemy (target id 1) on top.
    ///
    /// Every encounter start and retry ends with BattleOutcomeController.OnRetry.
    /// On it, this component brings the second enemy into play for the Vault (active,
    /// handed to BattleOutcomeController so victory waits for both) and resets it to
    /// the lineup's starting values; for any other encounter, and on the title screen,
    /// it takes the second enemy back out.
    ///
    /// It applies no damage and runs no turns.
    /// </summary>
    public sealed class VaultCampaignLink : MonoBehaviour
    {
        [SerializeField] private CampaignController campaign;
        [SerializeField] private BattleOutcomeController outcomeController;

        [Header("Vault")]
        [Tooltip("The campaign encounter fought as the Vault. Its fields match the lineup's first enemy.")]
        [SerializeField] private EncounterConfig vaultEncounter;
        [Tooltip("Anshul's two-enemy lineup. The second enemy is set up from its target id 1.")]
        [SerializeField] private VaultEncounter lineup;

        [Header("Second enemy")]
        [SerializeField] private Health secondEnemyHealth;
        [SerializeField] private EnemyPatternRunner secondEnemyPattern;
        [Tooltip("Optional. Receives the second enemy's sprite, and is shown and hidden with it.")]
        [SerializeField] private SpriteRenderer secondEnemySpriteRenderer;

        /// <summary>True while the second enemy is part of the fight.</summary>
        public bool SecondEnemyInPlay => secondEnemyHealth != null && secondEnemyHealth.gameObject.activeSelf;

        /// <summary>True while the Vault is the encounter being fought.</summary>
        public bool IsVaultInBattle => campaign != null && vaultEncounter != null
            && campaign.State == CampaignState.InBattle && campaign.CurrentEncounter == vaultEncounter;

        private void OnEnable() => Subscribe();
        private void OnDisable() => Unsubscribe();

        /// <summary>Assigns every reference at runtime. Safe to call before or after OnEnable.</summary>
        public void SetReferences(CampaignController campaignController, BattleOutcomeController outcome,
                                  EncounterConfig vault, VaultEncounter vaultLineup,
                                  Health secondHealth, EnemyPatternRunner secondPattern,
                                  SpriteRenderer secondSpriteRenderer = null)
        {
            if (isActiveAndEnabled) Unsubscribe();

            campaign = campaignController;
            outcomeController = outcome;
            vaultEncounter = vault;
            lineup = vaultLineup;
            secondEnemyHealth = secondHealth;
            secondEnemyPattern = secondPattern;
            secondEnemySpriteRenderer = secondSpriteRenderer;

            if (isActiveAndEnabled) Subscribe();
        }

        private void Subscribe()
        {
            if (campaign != null) campaign.StateChanged += HandleCampaignStateChanged;
            if (outcomeController != null) outcomeController.OnRetry += HandleEncounterReset;
        }

        private void Unsubscribe()
        {
            if (campaign != null) campaign.StateChanged -= HandleCampaignStateChanged;
            if (outcomeController != null) outcomeController.OnRetry -= HandleEncounterReset;
        }

        private void HandleCampaignStateChanged(CampaignState state)
        {
            // Entering an encounter is handled on OnRetry, after the campaign has set it up.
            if (state == CampaignState.Title) SetSecondEnemyInPlay(false);
        }

        private void HandleEncounterReset()
        {
            if (!isActiveAndEnabled) return;
            bool vault = IsVaultInBattle && HasValidLineup();
            SetSecondEnemyInPlay(vault);
            if (vault) ResetSecondEnemy();
        }

        private bool HasValidLineup()
        {
            if (lineup != null && lineup.IsValid && secondEnemyHealth != null) return true;
            Debug.LogError("VaultCampaignLink: assign a valid two-enemy lineup and the second enemy's Health");
            return false;
        }

        /// <summary>The lineup's second enemy: the placement with the higher target id.</summary>
        private VaultEncounter.EnemyPlacement SecondPlacement =>
            lineup.Enemy(1).TargetId > lineup.Enemy(0).TargetId ? lineup.Enemy(1) : lineup.Enemy(0);

        private void SetSecondEnemyInPlay(bool inPlay)
        {
            if (secondEnemyHealth == null) return;
            // Activated before it is configured, so Health.Awake on a first activation
            // cannot undo the health set in ResetSecondEnemy.
            secondEnemyHealth.gameObject.SetActive(inPlay);
            if (secondEnemySpriteRenderer != null) secondEnemySpriteRenderer.gameObject.SetActive(inPlay);
            if (outcomeController != null) outcomeController.SetSecondEnemy(inPlay ? secondEnemyHealth : null);
        }

        private void ResetSecondEnemy()
        {
            VaultEncounter.EnemyPlacement placement = SecondPlacement;
            EnemyPatternRunner pattern = secondEnemyPattern != null ? secondEnemyPattern
                : secondEnemyHealth.GetComponent<EnemyPatternRunner>();

            if (pattern != null) pattern.Configure(placement.Pattern, placement.ResolvedName, placement.OpeningOffset);
            secondEnemyHealth.SetMaxHealth(placement.MaxHealth);
            if (secondEnemyHealth.TryGetComponent(out Poison poison)) poison.ConsumeAllStacks();
            if (secondEnemySpriteRenderer != null && placement.EnemySprite != null)
                secondEnemySpriteRenderer.sprite = placement.EnemySprite;

            // Health has no setter for current health, so a lower starting value is
            // applied as damage, as CampaignController does for the first enemy.
            int difference = secondEnemyHealth.CurrentHealth - placement.StartingHealth;
            if (difference > 0) secondEnemyHealth.TakeDamage(difference);
        }
    }
}
