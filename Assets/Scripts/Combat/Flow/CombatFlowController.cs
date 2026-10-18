using System;
using System.Collections;
using UnityEngine;
using Visha.Battle;
using Visha.Campaign;
using Visha.Enemies;
using Visha.UI;

namespace Visha.Combat.Flow
{
    public class CombatFlowController : MonoBehaviour
    {
        [Header("Combatants")]
        [SerializeField] private GameObject playerGameObject;
        [SerializeField] private GameObject enemyGameObject;
        [SerializeField] private BattleHUD battleHUD;
        [SerializeField] private BattleOutcomeController outcomeController;
        [SerializeField] private EnemyPatternRunner enemyPatternRunner;

        [Header("Ability Components (on the Player; found automatically if left empty)")]
        [SerializeField] private VenomStrike venomStrike;
        [SerializeField] private SerpentsBite serpentsBite;
        [SerializeField] private Fade fade;

        [Header("Campaign (optional)")]
        [Tooltip("When set, combat only accepts actions while an encounter is InBattle. " +
                 "Found automatically if the scene has one. Leave empty for a single battle.")]
        [SerializeField] private CampaignController campaign;

        private Health playerHealth;
        private Health enemyHealth;
        private Poison playerPoison;
        private Poison enemyPoison;

        private BattleViewState viewState;
        private BattlePhase currentPhase = BattlePhase.Waiting;
        private bool turnInProgress;
        private bool combatActive = true;
        private bool subscribed;

        /// <summary>Fired when the view state is updated, for UI to render.</summary>
        public event Action<BattleViewState> OnStateUpdated;

        public BattlePhase CurrentPhase => currentPhase;
        public bool IsCombatActive => combatActive;

        private void Awake()
        {
            InitializeCombatants();
            InitializeViewState();
        }

        private void Start()
        {
            SubscribeToBattleSystem();
            BindCampaign();
            EnterPlayerTurn();
        }

        private void OnDestroy()
        {
            UnsubscribeFromBattleSystem();
        }

        /// <summary>Caches Health and Poison, and finds the ability components on the player if unassigned.</summary>
        private void InitializeCombatants()
        {
            if (playerGameObject == null || enemyGameObject == null)
            {
                Debug.LogError("CombatFlowController: Player or Enemy GameObject not assigned");
                return;
            }

            playerHealth = playerGameObject.GetComponent<Health>();
            enemyHealth = enemyGameObject.GetComponent<Health>();
            playerPoison = playerGameObject.GetComponent<Poison>();
            enemyPoison = enemyGameObject.GetComponent<Poison>();

            if (venomStrike == null) venomStrike = playerGameObject.GetComponent<VenomStrike>();
            if (serpentsBite == null) serpentsBite = playerGameObject.GetComponent<SerpentsBite>();
            if (fade == null) fade = playerGameObject.GetComponent<Fade>();

            if (playerHealth == null) Debug.LogError("Player missing Health component");
            if (enemyHealth == null) Debug.LogError("Enemy missing Health component");
            if (enemyPoison == null) Debug.LogWarning("Enemy missing Poison component");
            if (venomStrike == null) Debug.LogWarning("Player missing VenomStrike component");
            if (serpentsBite == null) Debug.LogWarning("Player missing SerpentsBite component");
            if (fade == null) Debug.LogWarning("Player missing Fade component");
        }

        private void InitializeViewState()
        {
            viewState = new BattleViewState();
            UpdateViewState();
        }

        private void SubscribeToBattleSystem()
        {
            if (subscribed) return;
            subscribed = true;

            if (battleHUD != null) battleHUD.AbilityRequested += HandleAbilityRequested;

            if (outcomeController != null)
            {
                outcomeController.OnBattleEnded += HandleBattleEnded;
                outcomeController.OnRetry += HandleRetry;
            }

            if (playerHealth != null) playerHealth.OnHealthChanged += HandleHealthChanged;
            if (enemyHealth != null) enemyHealth.OnHealthChanged += HandleHealthChanged;
            if (playerPoison != null) playerPoison.OnPoisonChanged += HandlePoisonChanged;
            if (enemyPoison != null) enemyPoison.OnPoisonChanged += HandlePoisonChanged;
            if (fade != null) fade.OnFadeChanged += Refresh;
        }

        private void UnsubscribeFromBattleSystem()
        {
            if (!subscribed) return;
            subscribed = false;

            if (battleHUD != null) battleHUD.AbilityRequested -= HandleAbilityRequested;

            if (outcomeController != null)
            {
                outcomeController.OnBattleEnded -= HandleBattleEnded;
                outcomeController.OnRetry -= HandleRetry;
            }

            if (playerHealth != null) playerHealth.OnHealthChanged -= HandleHealthChanged;
            if (enemyHealth != null) enemyHealth.OnHealthChanged -= HandleHealthChanged;
            if (playerPoison != null) playerPoison.OnPoisonChanged -= HandlePoisonChanged;
            if (enemyPoison != null) enemyPoison.OnPoisonChanged -= HandlePoisonChanged;
            if (fade != null) fade.OnFadeChanged -= Refresh;
            if (campaign != null) campaign.StateChanged -= HandleCampaignStateChanged;
        }
        private void BindCampaign()
        {
            if (campaign == null) campaign = FindAnyObjectByType<CampaignController>();
            if (campaign == null) return;

            campaign.StateChanged += HandleCampaignStateChanged;
            combatActive = campaign.State == CampaignState.InBattle;
        }

        private void HandleCampaignStateChanged(CampaignState state)
        {
            SetCombatActive(state == CampaignState.InBattle);
        }

        public void SetCombatActive(bool active)
        {
            combatActive = active;

            if (!active)
            {
                StopAllCoroutines();
                if (!IsBattleOver())
                {
                    turnInProgress = false;
                    SetPhase(BattlePhase.Waiting);
                }
                return;
            }

            if (!IsBattleOver() && currentPhase == BattlePhase.Waiting)
            {
                EnterPlayerTurn();
            }
        }

        private bool CanAcceptAction()
        {
            return combatActive && !turnInProgress && currentPhase == BattlePhase.PlayerTurn && !IsBattleOver();
        }

        /// <summary>True as soon as either side is dead, even before the outcome event arrives.</summary>
        private bool IsBattleOver()
        {
            if (outcomeController != null && outcomeController.IsOver) return true;
            return (playerHealth != null && playerHealth.IsDead) || (enemyHealth != null && enemyHealth.IsDead);
        }


        private void EnterPlayerTurn()
        {
            if (turnInProgress || IsBattleOver()) return;

            if (!combatActive)
            {
                SetPhase(BattlePhase.Waiting);
                return;
            }

            SetPhase(BattlePhase.PlayerTurn);
            Debug.Log("Combat: Entered Player Turn");
        }

        private void EnterEnemyTurn()
        {
            if (enemyPatternRunner == null || enemyHealth == null || IsBattleOver())
            {
                turnInProgress = false;
                return;
            }

            turnInProgress = true;
            SetPhase(BattlePhase.EnemyTurn);
            Debug.Log("Combat: Enemy turn starting");
            StartCoroutine(ExecuteEnemyActionNextFrame());
        }

        private IEnumerator ExecuteEnemyActionNextFrame()
        {
            yield return null; // one frame so the HUD shows EnemyTurn
            ResolveEnemyTurn();
        }

        private void ResolveEnemyTurn()
        {
            if (IsBattleOver() || !combatActive)
            {
                return;
            }

            EnemyAction action = enemyPatternRunner.TakeTurn();
            int damage = action.Damage;
            if (action.Kind == EnemyActionKind.Attack && fade != null)
            {
                damage = fade.ReduceDamage(damage);
            }

            Debug.Log($"Combat: Enemy used {action.Label} for {damage} (telegraphed {action.Damage})");
            if (damage > 0 && playerHealth != null) playerHealth.TakeDamage(damage);

            if (IsBattleOver())
            {
                if (fade != null) fade.EndEnemyPhase();
                return;
            }

            if (enemyPoison != null) enemyPoison.Tick();
            if (fade != null) fade.EndEnemyPhase();

            if (IsBattleOver()) return;

            turnInProgress = false;
            EnterPlayerTurn();
        }

        /// <summary>Handles ability requests from the BattleHUD.</summary>
        private void HandleAbilityRequested(BattleAbility ability, int targetIndex)
        {
            if (!CanAcceptAction())
            {
                Debug.LogWarning($"Combat: {ability} ignored (phase {currentPhase}, active {combatActive}, busy {turnInProgress})");
                return;
            }

            turnInProgress = true;

            string reason = ExecuteAbility(ability);
            if (reason != null)
            {
                turnInProgress = false;
                Debug.LogWarning($"Combat: {ability} refused - {reason}");
                if (battleHUD != null) battleHUD.RejectAction(reason);
                return;
            }

            if (ability != BattleAbility.Fade && fade != null) fade.OnOtherAbilityUsed();

            Debug.Log($"Combat: Player used {ability}");

            if (IsBattleOver()) return;

            EnterEnemyTurn();
        }

        private string ExecuteAbility(BattleAbility ability)
        {
            if (playerHealth == null || enemyHealth == null) return "Combatants are missing";

            switch (ability)
            {
                case BattleAbility.VenomStrike: return UseVenomStrike();
                case BattleAbility.SerpentsBite: return UseSerpentsBite();
                case BattleAbility.Fade: return UseFade();
                default: return "Unknown ability";
            }
        }

        private string UseVenomStrike()
        {
            if (venomStrike == null) return "Venom Strike is not set up";
            return venomStrike.TryUseOn(enemyGameObject) ? null : "Venom Strike cannot be used";
        }

        private string UseSerpentsBite()
        {
            if (serpentsBite == null) return "Serpent's Bite is not set up";
            if (!serpentsBite.CanUseOn(enemyGameObject)) return "Serpent's Bite needs poison on the enemy";
            return serpentsBite.TryUseOn(enemyGameObject) ? null : "Serpent's Bite cannot be used";
        }

        private string UseFade()
        {
            if (fade == null) return "Fade is not set up";
            return fade.TryUse() ? null : "Fade can't be used two turns in a row";
        }

        private void UpdateViewState()
        {
            if (viewState == null) return;

            viewState.playerHealth = playerHealth != null ? playerHealth.CurrentHealth : 0;
            viewState.playerMaxHealth = playerHealth != null ? playerHealth.MaxHealth : 0;
            viewState.enemyHealth = enemyHealth != null ? enemyHealth.CurrentHealth : 0;
            viewState.enemyMaxHealth = enemyHealth != null ? enemyHealth.MaxHealth : 0;
            viewState.poison = enemyPoison != null ? enemyPoison.PoisonStacks : 0;
            viewState.poisonCap = enemyPoison != null ? enemyPoison.MaxStacks : 5;
            viewState.enemyName = enemyPatternRunner != null ? enemyPatternRunner.EnemyName : "Enemy";
            viewState.intention = enemyPatternRunner != null ? enemyPatternRunner.IntentLabel : "...";
            viewState.incomingDamage = enemyPatternRunner != null ? enemyPatternRunner.IncomingDamage : 0;
            viewState.fadeAvailable = fade != null && fade.CanUse;
            viewState.phase = currentPhase;
        }

        private void SetPhase(BattlePhase phase)
        {
            currentPhase = phase;
            Refresh();
        }

        private void Refresh()
        {
            UpdateViewState();
            if (battleHUD != null && viewState != null) battleHUD.Render(viewState);
            OnStateUpdated?.Invoke(viewState);
        }


        private void HandleBattleEnded(BattleOutcome outcome)
        {
            StopAllCoroutines();
            turnInProgress = true;
            SetPhase(outcome == BattleOutcome.Victory ? BattlePhase.Victory : BattlePhase.Defeat);
            Debug.Log($"Combat: Battle ended - {outcome}");
        }

        private void HandleRetry()
        {
            StopAllCoroutines();
            turnInProgress = false;
            if (fade != null) fade.ResetFade();

            SetPhase(BattlePhase.Waiting);
            EnterPlayerTurn();
            Debug.Log("Combat: Battle reset");
        }

        private void HandleHealthChanged(int current, int max) => Refresh();
        private void HandlePoisonChanged(int stacks) => Refresh();
    }
}
