using System.Collections.Generic;

namespace Visha.Enemies
{
    /// <summary>
    /// A fixed, repeating cycle of enemy moves. The pattern is deterministic: given
    /// the same starting point it always produces the same sequence, so the fight is
    /// a puzzle the player learns to read, never a dice roll.
    ///
    /// The two operations that matter for the "telegraph" design are:
    ///   • Current    — the move the enemy will perform on its NEXT turn. This is what
    ///                  the UI shows the player during the player's turn.
    ///   • Advance()  — call once after the enemy has taken its turn, to step the
    ///                  cycle forward so Current now points at the following move.
    ///
    /// Because Current is known before the player acts, the controller can populate
    /// BattleViewState.intention / incomingDamage and the player sees exactly what is
    /// coming — enough to decide whether to Fade, build poison, or burst.
    /// </summary>
    public sealed class EnemyAttackPattern
    {
        private readonly EnemyAction[] _actions;
        private int _index;

        /// <summary>A readable name for the pattern, e.g. "Temple Guardian".</summary>
        public string Name { get; }

        /// <param name="name">Display / debug name for this pattern.</param>
        /// <param name="actions">
        /// The ordered cycle of moves. Must contain at least one move. The cycle loops
        /// back to the first move after the last.
        /// </param>
        public EnemyAttackPattern(string name, IReadOnlyList<EnemyAction> actions)
        {
            if (actions == null || actions.Count == 0)
                throw new System.ArgumentException("An attack pattern needs at least one action.", nameof(actions));

            Name = string.IsNullOrEmpty(name) ? "Enemy" : name;
            _actions = new EnemyAction[actions.Count];
            for (int i = 0; i < actions.Count; i++) _actions[i] = actions[i];
            _index = 0;
        }

        /// <summary>Number of distinct moves in the cycle.</summary>
        public int Length => _actions.Length;

        /// <summary>Index (0-based) of the move that Current points at. Useful for tests and debugging.</summary>
        public int CurrentIndex => _index;

        /// <summary>
        /// The move the enemy will perform on its next turn. This is the value to
        /// telegraph to the player; reading it does not change the pattern.
        /// </summary>
        public EnemyAction Current => _actions[_index];

        /// <summary>
        /// The move that will come one turn after Current. Handy if the UI ever wants
        /// to preview two turns ahead; not required for the basic telegraph.
        /// </summary>
        public EnemyAction Peek(int turnsAhead = 0)
        {
            if (turnsAhead < 0) turnsAhead = 0;
            int i = (_index + turnsAhead) % _actions.Length;
            return _actions[i];
        }

        /// <summary>
        /// Steps the cycle forward by one. Call this once after the enemy has acted so
        /// that Current points at the following move for the next telegraph.
        /// </summary>
        public void Advance()
        {
            _index = (_index + 1) % _actions.Length;
        }

        /// <summary>
        /// Returns the current move and advances in one call — convenient for a
        /// controller that resolves the enemy turn and immediately wants the cycle
        /// ready for the next telegraph.
        /// </summary>
        public EnemyAction TakeTurn()
        {
            EnemyAction action = Current;
            Advance();
            return action;
        }

        /// <summary>Rewinds the cycle to its first move, for a new encounter or a retry.</summary>
        public void Reset()
        {
            _index = 0;
        }
    }
}
