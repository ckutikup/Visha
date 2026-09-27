using System;
using UnityEngine;
using Visha.Enemies;

namespace Visha.Battle
{
    /// <summary>
    /// Drop this anywhere in the battle scene. It watches the player's and enemy's
    /// <see cref="Health"/> for death, decides Victory/Defeat, and resets the
    /// encounter for a retry.
    ///
    /// This performs no combat resolution, applies no damage, and drives no turn
    /// order — it only reacts to Health.OnDied and reports the result. Whatever
    /// combat controller Rahul builds should map <see cref="Outcome"/> onto
    /// BattleViewState.phase (Visha.UI.BattlePhase.Victory / .Defeat) and call
    /// <see cref="Retry"/> when the player asks to try again.
    ///
    /// It does not modify the shared BattlePrototype scene, BattleHUD, or any
    /// project settings. The two Health references are the only wiring needed,
    /// either in the inspector or via <see cref="SetCombatants"/> at runtime.
    /// </summary>
    public class BattleOutcomeController : MonoBehaviour
    {
        [SerializeField] private Health playerHealth;
        [SerializeField] private Health enemyHealth;

        /// <summary>Fires once, the moment the battle ends.</summary>
        public event Action<BattleOutcome> OnBattleEnded;

        /// <summary>Fires after Retry() has reset both combatants, so other systems can reset too.</summary>
        public event Action OnRetry;

        public BattleOutcome Outcome { get; private set; } = BattleOutcome.None;
        public bool IsOver => Outcome != BattleOutcome.None;

        private void OnEnable()
        {
            Subscribe(playerHealth, enemyHealth);
        }

        private void OnDisable()
        {
            Unsubscribe(playerHealth, enemyHealth);
        }

        /// <summary>Assigns the two combatants to watch. Safe to call before or after OnEnable.</summary>
        public void SetCombatants(Health player, Health enemy)
        {
            if (isActiveAndEnabled) Unsubscribe(playerHealth, enemyHealth);

            playerHealth = player;
            enemyHealth = enemy;

            if (isActiveAndEnabled) Subscribe(playerHealth, enemyHealth);
        }

        private void Subscribe(Health player, Health enemy)
        {
            // Win/lose is driven entirely off Health.OnDied — no HP polling anywhere.
            if (player != null) player.OnDied += HandlePlayerDied;
            if (enemy != null) enemy.OnDied += HandleEnemyDied;
        }

        private void Unsubscribe(Health player, Health enemy)
        {
            if (player != null) player.OnDied -= HandlePlayerDied;
            if (enemy != null) enemy.OnDied -= HandleEnemyDied;
        }

        private void HandlePlayerDied()
        {
            if (IsOver) return;
            SetOutcome(BattleOutcome.Defeat);
        }

        private void HandleEnemyDied()
        {
            if (IsOver) return;
            // A simultaneous death favors Defeat: going down still counts as losing
            // even if the killing blow also finished the enemy off.
            bool playerAlsoDown = playerHealth != null && playerHealth.IsDead;
            SetOutcome(playerAlsoDown ? BattleOutcome.Defeat : BattleOutcome.Victory);
        }

        private void SetOutcome(BattleOutcome outcome)
        {
            Outcome = outcome;
            Debug.Log("Battle ended: " + outcome);
            OnBattleEnded?.Invoke(outcome);
        }

        /// <summary>
        /// Resets the encounter to try again: revives both combatants
        /// (Health.ResetHealth()), clears poison stacks (Poison.ConsumeAllStacks()),
        /// restarts the enemy's attack pattern (if present), and clears the outcome.
        /// Fires OnRetry so an already-wired combat controller can reset its own
        /// turn state too.
        /// </summary>
        public void Retry()
        {
            Outcome = BattleOutcome.None;

            ReviveAndClearPoison(playerHealth);
            ReviveAndClearPoison(enemyHealth);

            if (enemyHealth != null && enemyHealth.TryGetComponent(out EnemyPatternRunner pattern))
            {
                pattern.ResetPattern();
            }

            Debug.Log("Battle retry requested");
            OnRetry?.Invoke();
        }

        private static void ReviveAndClearPoison(Health health)
        {
            if (health == null) return;
            health.ResetHealth();
            if (health.TryGetComponent(out Poison poison))
            {
                poison.ConsumeAllStacks();
            }
        }
    }
}
