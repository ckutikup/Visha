using System;
using UnityEngine;
using Visha.Battle;
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

        [Header("Ability Components")]
        [SerializeField] private VenomStrike venomStrike;

        [Header("Balance Values")]
        [SerializeField] private int serpentsBiteDamage = 12;
        [SerializeField] private int fadeReduction = 0;
        [SerializeField] private bool fadeBlocksNextAttack = true;

        private Health playerHealth;
        private Health enemyHealth;
        private Poison playerPoison;
        private Poison enemyPoison;

        private BattleViewState viewState;
        private BattlePhase currentPhase = BattlePhase.Waiting;
        private bool isFadeActive = false;
        private bool isProcessingAction = false;

        /// <summary>Fired when the view state is updated, for UI to render.</summary>
        public event Action<BattleViewState> OnStateUpdated;

        private void Awake()
        {
            InitializeCombatants();
            InitializeViewState();
        }

        private void Start()
        {
            SubscribeToBattleSystem();
            EnterPlayerTurn();
        }

        private void OnDestroy()
        {
            UnsubscribeFromBattleSystem();
        }

        /// <summary>Initializes and caches all health and poison components.</summary>
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

            if (playerHealth == null) Debug.LogError("Player missing Health component");
            if (enemyHealth == null) Debug.LogError("Enemy missing Health component");

            // Poison is optional but log if missing
            if (playerPoison == null) Debug.LogWarning("Player missing Poison component");
            if (enemyPoison == null) Debug.LogWarning("Enemy missing Poison component");
        }

        private void InitializeViewState()
        {
            viewState = new BattleViewState
            {
                playerHealth = playerHealth?.CurrentHealth ?? 0,
                playerMaxHealth = playerHealth?.MaxHealth ?? 30,
                enemyHealth = enemyHealth?.CurrentHealth ?? 0,
                enemyMaxHealth = enemyHealth?.MaxHealth ?? 24,
                poison = playerPoison?.PoisonStacks ?? 0,
                poisonCap = playerPoison?.MaxStacks ?? 5,
                enemyName = enemyPatternRunner?.EnemyName ?? "Enemy",
                intention = enemyPatternRunner?.IntentLabel ?? "...",
                incomingDamage = enemyPatternRunner?.IncomingDamage ?? 0,
                fadeAvailable = true,
                phase = BattlePhase.Waiting
            };
        }

        /// <summary>Subscribes to events from all battle systems.</summary>
        private void SubscribeToBattleSystem()
        {
            if (battleHUD != null)
            {
                battleHUD.AbilityRequested += HandleAbilityRequested;
            }

            if (outcomeController != null)
            {
                outcomeController.OnBattleEnded += HandleBattleEnded;
                outcomeController.OnRetry += HandleRetry;
            }

            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged += HandlePlayerHealthChanged;
                playerHealth.OnDied += HandlePlayerDied;
            }

            if (enemyHealth != null)
            {
                enemyHealth.OnHealthChanged += HandleEnemyHealthChanged;
                enemyHealth.OnDied += HandleEnemyDied;
            }

            if (playerPoison != null)
            {
                playerPoison.OnPoisonChanged += HandlePlayerPoisonChanged;
            }

            if (enemyPoison != null)
            {
                enemyPoison.OnPoisonChanged += HandleEnemyPoisonChanged;
            }
        }

        /// <summary>All Unsubscribes</summary>
        private void UnsubscribeFromBattleSystem()
        {
            if (battleHUD != null)
            {
                battleHUD.AbilityRequested -= HandleAbilityRequested;
            }

            if (outcomeController != null)
            {
                outcomeController.OnBattleEnded -= HandleBattleEnded;
                outcomeController.OnRetry -= HandleRetry;
            }

            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged -= HandlePlayerHealthChanged;
                playerHealth.OnDied -= HandlePlayerDied;
            }

            if (enemyHealth != null)
            {
                enemyHealth.OnHealthChanged -= HandleEnemyHealthChanged;
                enemyHealth.OnDied -= HandleEnemyDied;
            }

            if (playerPoison != null)
            {
                playerPoison.OnPoisonChanged -= HandlePlayerPoisonChanged;
            }

            if (enemyPoison != null)
            {
                enemyPoison.OnPoisonChanged -= HandleEnemyPoisonChanged;
            }
        }

        private void EnterPlayerTurn()
        {
            if (isProcessingAction) return;

            currentPhase = BattlePhase.PlayerTurn;
            isFadeActive = false;
            UpdateViewState();
            RenderUI();

            Debug.Log("Combat: Entered Player Turn");
        }

        private void EnterEnemyTurn()
        {
            if (isProcessingAction || enemyPatternRunner == null || enemyHealth == null)
                return;

            isProcessingAction = true;
            currentPhase = BattlePhase.EnemyTurn;
            UpdateViewState();
            RenderUI();

            Debug.Log("Combat: Enemy turn starting");

            StartCoroutine(ExecuteEnemyActionNextFrame());
        }

        private System.Collections.IEnumerator ExecuteEnemyActionNextFrame()
        {
            yield return null; // Wait one frame

            if (enemyPatternRunner == null || enemyHealth == null || enemyHealth.IsDead)
            {
                isProcessingAction = false;
                EnterPlayerTurn();
                yield break;
            }

            EnemyAction action = enemyPatternRunner.TakeTurn();
            Debug.Log($"Combat: Enemy action - {action.Label} ({action.Damage} damage)");

            int damageToPlayer = action.Damage;
            if (isFadeActive && action.Kind == EnemyActionKind.Attack)
            {
                damageToPlayer = fadeBlocksNextAttack
                    ? 0
                    : Mathf.Max(0, damageToPlayer - fadeReduction);
                Debug.Log($"Combat: Fade reduced damage from {action.Damage} to {damageToPlayer}");
            }

            if (damageToPlayer > 0 && playerHealth != null)
            {
                playerHealth.TakeDamage(damageToPlayer);
            }

            if (enemyPoison != null && enemyPoison.PoisonStacks > 0)
            {
                int poisonDamage = enemyPoison.PoisonStacks * 2;
                Debug.Log($"Combat: Enemy takes {poisonDamage} poison damage from {enemyPoison.PoisonStacks} stacks");
                enemyHealth.TakeDamage(poisonDamage);
            }

            isProcessingAction = false;

            if (!IsBattleOver())
            {
                EnterPlayerTurn();
            }
        }

        /// <summary>Handles ability requests from the BattleHUD UI.</summary>
        private void HandleAbilityRequested(BattleAbility ability, int targetIndex)
        {
            if (currentPhase != BattlePhase.PlayerTurn || isProcessingAction || IsBattleOver())
            {
                Debug.LogWarning("Combat: Ability requested in invalid state");
                return;
            }

            bool success = ExecuteAbility(ability, targetIndex);

            if (!success)
            {
                Debug.LogWarning($"Combat: Ability {ability} validation failed");
                ShowFeedback($"{ability} cannot be used");
                return;
            }

            if (ability != BattleAbility.Fade)
            {
                isFadeActive = false;
            }

            Debug.Log($"Combat: Player used {ability}");
            EnterEnemyTurn();
        }

        private bool ExecuteAbility(BattleAbility ability, int targetIndex)
        {
            if (playerHealth == null || enemyHealth == null)
            {
                return false;
            }

            switch (ability)
            {
                case BattleAbility.VenomStrike:
                    return UseVenomStrike(targetIndex);

                case BattleAbility.SerpentsBite:
                    return UseSerpentsBite(targetIndex);

                case BattleAbility.Fade:
                    return UseFade();

                default:
                    Debug.LogError($"Unknown ability: {ability}");
                    return false;
            }
        }

        private bool UseVenomStrike(int targetIndex)
        {
            if (venomStrike == null || enemyGameObject == null)
            {
                Debug.LogError("Venom Strike: Missing component references");
                return false;
            }

            venomStrike.UseOn(enemyGameObject);
            Debug.Log($"Combat: Venom Strike applied {venomStrike.StacksApplied} poison stacks");
            return true;
        }

        private bool UseSerpentsBite(int targetIndex)
        {
            if (enemyPoison == null || enemyHealth == null)
            {
                Debug.LogError("Serpent's Bite: Missing component references");
                return false;
            }

            if (enemyPoison.PoisonStacks <= 0)
            {
                Debug.LogWarning("Combat: Serpent's Bite requires poison stacks");
                ShowFeedback("Serpent's Bite requires poison");
                return false;
            }

            int baseDamage = serpentsBiteDamage;
            int poisonBonus = enemyPoison.PoisonStacks;
            int totalDamage = baseDamage + poisonBonus;

            enemyHealth.TakeDamage(totalDamage);
            enemyPoison.ConsumeAllStacks();

            Debug.Log($"Combat: Serpent's Bite dealt {totalDamage} damage ({baseDamage} base + {poisonBonus} poison bonus)");
            return true;
        }

        private bool UseFade()
        {
            isFadeActive = true;
            Debug.Log("Combat: Fade activated - next attack will be reduced");
            return true;
        }

        private void UpdateViewState()
        {
            if (viewState == null) return;

            viewState.playerHealth = playerHealth?.CurrentHealth ?? 0;
            viewState.playerMaxHealth = playerHealth?.MaxHealth ?? 30;
            viewState.enemyHealth = enemyHealth?.CurrentHealth ?? 0;
            viewState.enemyMaxHealth = enemyHealth?.MaxHealth ?? 24;
            viewState.poison = playerPoison?.PoisonStacks ?? 0;
            viewState.poisonCap = playerPoison?.MaxStacks ?? 5;
            viewState.enemyName = enemyPatternRunner?.EnemyName ?? "Enemy";
            viewState.intention = enemyPatternRunner?.IntentLabel ?? "...";
            viewState.incomingDamage = enemyPatternRunner?.IncomingDamage ?? 0;
            viewState.fadeAvailable = !isFadeActive; // Fade becomes unavailable after use
            viewState.phase = currentPhase;
        }

        private void RenderUI()
        {
            if (battleHUD != null && viewState != null)
            {
                battleHUD.Render(viewState);
            }

            OnStateUpdated?.Invoke(viewState);
        }

        private void ShowFeedback(string message)
        {
            if (battleHUD != null)
            {
                battleHUD.SetFeedback(message);
            }
        }

        private bool IsBattleOver()
        {
            return outcomeController != null && outcomeController.IsOver;
        }

        private void HandleBattleEnded(BattleOutcome outcome)
        {
            isProcessingAction = true;
            currentPhase = outcome == BattleOutcome.Victory ? BattlePhase.Victory : BattlePhase.Defeat;
            UpdateViewState();
            RenderUI();

            Debug.Log($"Combat: Battle ended - {outcome}");
        }

        private void HandleRetry()
        {
            isProcessingAction = false;
            isFadeActive = false;
            currentPhase = BattlePhase.Waiting;
            InitializeViewState();
            EnterPlayerTurn();

            Debug.Log("Combat: Battle retry initiated");
        }

        private void HandlePlayerHealthChanged(int current, int max)
        {
            UpdateViewState();
            RenderUI();
        }

        private void HandleEnemyHealthChanged(int current, int max)
        {
            UpdateViewState();
            RenderUI();
        }

        private void HandlePlayerDied()
        {
            Debug.Log("Combat: Player died");
            UpdateViewState();
            RenderUI();
        }

        private void HandleEnemyDied()
        {
            Debug.Log("Combat: Enemy died");
            UpdateViewState();
            RenderUI();
        }

        private void HandlePlayerPoisonChanged(int stacks)
        {
            UpdateViewState();
            RenderUI();
        }

        private void HandleEnemyPoisonChanged(int stacks)
        {
            UpdateViewState();
            RenderUI();
        }
    }
}