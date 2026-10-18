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

        [SerializeField] private CombatActionPresentation presentation;

        [Header("Campaign (optional)")]
        [Tooltip("When set, combat only accepts actions while an encounter is InBattle. " +
                 "Found automatically if the scene has one. Leave empty for a single battle.")]
        [SerializeField] private CampaignController campaign;

        private RoutineBreak routine;
        private BattleAudio sound;
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
        public event Action<BattleAbility> OnAbilityUsed;

        public BattlePhase CurrentPhase => currentPhase;
        public bool IsCombatActive => combatActive;

        private void Awake()
        {
            routine = GetComponent<RoutineBreak>();
            sound = FindAnyObjectByType<BattleAudio>();
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
                if (routine != null) routine.ResetEncounter();
                StopAllCoroutines();
                if (presentation != null) presentation.ResetPose();
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
            yield return new WaitForSeconds(.12f);
            if (IsBattleOver() || !combatActive) yield break;
            if (enemyPoison != null && enemyPoison.PoisonStacks > 0)
            {
                SetPhase(BattlePhase.Poison);
                if (battleHUD != null) battleHUD.SetFeedback($"Poison · {enemyPoison.PoisonStacks * enemyPoison.DamagePerStack} damage");
                enemyPoison.Tick();
                if (IsBattleOver()) yield break;
                yield return new WaitForSeconds(.3f);
            }
            SetPhase(BattlePhase.EnemyTurn);
            int incoming = enemyPatternRunner.IncomingDamage;
            int predicted = fade != null ? fade.ReduceDamage(incoming) : incoming;
            if (battleHUD != null) battleHUD.SetFeedback(incoming > 0
                ? $"{enemyPatternRunner.IntentLabel} · {predicted} damage"
                : "The enemy holds position · no attack");
            if (presentation != null)
                yield return presentation.EnemyAction(incoming > 0, ApplyEnemyAction, incoming >= 9, enemyPatternRunner.IntentLabel);
            else
                ApplyEnemyAction();
            FinishRound();
        }

        private void ResolveEnemyTurn()
        {
            if (IsBattleOver() || !combatActive) return;
            if (enemyPoison != null) enemyPoison.Tick();
            if (IsBattleOver()) return;
            ApplyEnemyAction();
            FinishRound();
        }

        private void ApplyEnemyAction()
        {
            if (IsBattleOver() || !combatActive) return;
            EnemyAction action = enemyPatternRunner.NextAction;
            int damage = action.Damage;
            if (action.Kind == EnemyActionKind.Attack && fade != null)
                damage = fade.ReduceDamage(damage);
            bool guarded = fade != null && fade.IsActive;
            if (damage > 0 && playerHealth != null) playerHealth.TakeDamage(damage);
            if (routine != null && routine.TryGuard(enemyPatternRunner.IntentLabel, guarded,
                    playerHealth != null && !playerHealth.IsDead))
            {
                if (presentation != null) presentation.ShowCurseFragment();
                if (sound != null) sound.PlayRoutineBreak();
                if (battleHUD != null) battleHUD.SetFeedback("Routine Broken · next Venom +1 poison");
                Refresh();
            }
        }

        private void FinishRound()
        {
            if (IsBattleOver() || !combatActive) return;
            enemyPatternRunner.TakeTurn();
            if (fade != null) fade.EndEnemyPhase();
            turnInProgress = false;
            EnterPlayerTurn();
        }

        /// <summary>Handles ability requests from the BattleHUD.</summary>
        private void HandleAbilityRequested(BattleAbility ability, int targetIndex)
        {
            if (ability != BattleAbility.Fade && targetIndex != 0) return;
            if (!CanAcceptAction())
            {
                Debug.LogWarning($"Combat: {ability} ignored (phase {currentPhase}, active {combatActive}, busy {turnInProgress})");
                return;
            }

            if (presentation != null)
            {
                string error = ValidateAbility(ability);
                if (error != null)
                {
                    if (battleHUD != null) battleHUD.RejectAction(error);
                    return;
                }
                turnInProgress = true;
                SetPhase(BattlePhase.Resolving);
                StartCoroutine(AnimatePlayerAction(ability));
            }
            else
            {
                turnInProgress = true;
                if (ApplyPlayerAbility(ability) && !IsBattleOver()) EnterEnemyTurn();
            }
        }

        private string ValidateAbility(BattleAbility ability)
        {
            if (playerHealth == null || enemyHealth == null) return "Combatants are missing";
            if (ability == BattleAbility.VenomStrike)
                return venomStrike != null && enemyPoison != null ? null : "Venom Strike is not set up";
            if (ability == BattleAbility.SerpentsBite)
                return serpentsBite != null && serpentsBite.CanUseOn(enemyGameObject) ? null : "Bite needs poison";
            if (ability == BattleAbility.Fade)
                return fade != null && fade.CanUse ? null : "Fade needs another ability first";
            return "Unknown ability";
        }

        private IEnumerator AnimatePlayerAction(BattleAbility ability)
        {
            bool applied = false;
            if (battleHUD != null) battleHUD.SetFeedback(ability == BattleAbility.VenomStrike ? "Venom Strike"
                : ability == BattleAbility.SerpentsBite ? "Serpent's Bite" : "Fade · incoming damage reduced");
            yield return presentation.PlayerAction(ability, () => applied = ApplyPlayerAbility(ability));
            if (applied && !IsBattleOver() && combatActive) EnterEnemyTurn();
        }

        private bool ApplyPlayerAbility(BattleAbility ability)
        {
            if (!combatActive || IsBattleOver()) return false;
            string reason = ExecuteAbility(ability);
            if (reason != null)
            {
                turnInProgress = false;
                SetPhase(BattlePhase.PlayerTurn);
                if (battleHUD != null) battleHUD.RejectAction(reason);
                return false;
            }
            if (ability != BattleAbility.Fade && fade != null) fade.OnOtherAbilityUsed();
            OnAbilityUsed?.Invoke(ability);
            return true;
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
            if (!venomStrike.TryUseOn(enemyGameObject)) return "Venom Strike cannot be used";
            int bonus = routine != null ? routine.ConsumeBonus() : 0;
            if (bonus > 0) enemyPoison.AddStacks(bonus);
            return null;
        }

        private string UseSerpentsBite()
        {
            if (serpentsBite == null) return "Serpent's Bite is not set up";
            if (!serpentsBite.CanUseOn(enemyGameObject)) return "Serpent's Bite needs poison on the enemy";
            bool heavyThreat = enemyPatternRunner != null &&
                (enemyPatternRunner.IntentLabel == "Heavy Blow" || enemyPatternRunner.IntentLabel == "Crush");
            if (!serpentsBite.TryUseOn(enemyGameObject)) return "Serpent's Bite cannot be used";
            if (heavyThreat && enemyHealth.IsDead)
            {
                var screens = FindAnyObjectByType<BattleScreenView>();
                if (screens != null) screens.SetVictoryDetail("Threat Silenced · Bite stopped the heavy attack.");
            }
            return null;
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
            viewState.poisonDamagePerStack = enemyPoison != null ? enemyPoison.DamagePerStack : 0;
            viewState.poisonCap = enemyPoison != null ? enemyPoison.MaxStacks : 5;
            viewState.enemyName = enemyPatternRunner != null ? enemyPatternRunner.EnemyName : "Enemy";
            viewState.intention = enemyPatternRunner != null ? enemyPatternRunner.IntentLabel : "...";
            viewState.incomingDamage = enemyPatternRunner != null ? enemyPatternRunner.IncomingDamage : 0;
            if (fade != null) viewState.incomingDamage = fade.ReduceDamage(viewState.incomingDamage);
            viewState.fadeActive = fade != null && fade.IsActive;
            viewState.fadeAvailable = fade != null && fade.CanUse;
            viewState.venomStacks = venomStrike != null ? venomStrike.StacksApplied : 0;
            viewState.biteDamagePerStack = serpentsBite != null ? serpentsBite.DamagePerStack : 0;
            viewState.fadeReductionPercent = fade != null ? fade.ReductionPercent : 0;
            viewState.routineOpportunity = routine != null && routine.IsOpportunity(enemyPatternRunner != null ? enemyPatternRunner.IntentLabel : "");
            viewState.routineBoon = routine != null && routine.isActiveAndEnabled && routine.HasBoon;
            if (viewState.routineBoon) viewState.venomStacks++;
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
            if (presentation != null) presentation.ResetPose();
            turnInProgress = true;
            SetPhase(outcome == BattleOutcome.Victory ? BattlePhase.Victory : BattlePhase.Defeat);
            Debug.Log($"Combat: Battle ended - {outcome}");
        }

        private void HandleRetry()
        {
            if (routine != null) routine.ResetEncounter();
            StopAllCoroutines();
            if (presentation != null) presentation.ResetPose();
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
