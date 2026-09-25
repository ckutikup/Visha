using UnityEngine;

namespace Visha.Enemies
{
    /// <summary>
    /// Drop this on the enemy placeholder in the battle scene. It owns one
    /// <see cref="EnemyAttackPattern"/> and exposes the telegraph to whatever combat
    /// controller Rahul builds, without depending on the UI or the health/ability code.
    ///
    /// Typical use from the controller each round:
    ///   1. On the player's turn, read <see cref="IntentLabel"/> and
    ///      <see cref="IncomingDamage"/> and copy them into BattleViewState.intention /
    ///      incomingDamage so the player sees what is coming.
    ///   2. When the enemy turn resolves, call <see cref="TakeTurn"/>; apply its Damage
    ///      to the player (Balaji's code) and the runner auto-advances the cycle.
    ///   3. On a new encounter or retry, call <see cref="ResetPattern"/>.
    ///
    /// This component performs no damage and reads no other systems; it only decides
    /// and reports the enemy's intended move.
    /// </summary>
    public sealed class EnemyPatternRunner : MonoBehaviour
    {
        [Tooltip("Which built-in pattern this enemy uses. Only the Temple Guardian exists this milestone.")]
        [SerializeField] private PatternKind pattern = PatternKind.TempleGuardian;

        public enum PatternKind
        {
            TempleGuardian
        }

        private EnemyAttackPattern _pattern;

        /// <summary>The pattern instance, created on first use so it works in play and in tests.</summary>
        private EnemyAttackPattern Pattern => _pattern ??= Build(pattern);

        private void Awake()
        {
            _pattern ??= Build(pattern);
        }

        private static EnemyAttackPattern Build(PatternKind kind)
        {
            switch (kind)
            {
                case PatternKind.TempleGuardian:
                default:
                    return TempleGuardianPattern.Create();
            }
        }

        /// <summary>Display name of this enemy, for BattleViewState.enemyName.</summary>
        public string EnemyName => Pattern.Name;

        /// <summary>The move the enemy will perform on its next turn — the value to telegraph.</summary>
        public EnemyAction NextAction => Pattern.Current;

        /// <summary>Short label for the intent panel (BattleViewState.intention).</summary>
        public string IntentLabel => Pattern.Current.Label;

        /// <summary>Damage the telegraphed move will deal (BattleViewState.incomingDamage). 0 for a defend.</summary>
        public int IncomingDamage => Pattern.Current.Damage;

        /// <summary>True when the next move is a defend rather than an attack.</summary>
        public bool NextIsDefend => Pattern.Current.Kind == EnemyActionKind.Defend;

        /// <summary>Look further down the cycle without changing it (0 = the next move).</summary>
        public EnemyAction PeekAhead(int turnsAhead) => Pattern.Peek(turnsAhead);

        /// <summary>
        /// Resolves the enemy's turn: returns the move to apply and advances the cycle
        /// so the next telegraph is ready. The caller applies the damage.
        /// </summary>
        public EnemyAction TakeTurn() => Pattern.TakeTurn();

        /// <summary>Restarts the pattern from its first move.</summary>
        public void ResetPattern() => Pattern.Reset();
    }
}
