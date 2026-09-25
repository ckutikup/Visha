namespace Visha.Enemies
{
    /// <summary>
    /// What kind of move an enemy performs on its turn. The combat controller can
    /// branch on this without parsing the display label, and Balaji's damage code
    /// can decide whether the move deals damage or defends.
    /// </summary>
    public enum EnemyActionKind
    {
        Attack,
        Defend
    }

    /// <summary>
    /// A single, fully-known enemy move. Every field is decided up front, so the
    /// player can be shown exactly what is coming before they act — there is no
    /// randomness anywhere in an enemy's turn.
    ///
    /// Label / Damage are the two values the Battle UI already consumes:
    /// they map straight onto BattleViewState.intention and
    /// BattleViewState.incomingDamage, which Charith's IntentView renders.
    /// </summary>
    public readonly struct EnemyAction
    {
        /// <summary>Short display name, e.g. "Strike" or "Defend". Shown in the intent panel.</summary>
        public readonly string Label;

        /// <summary>Damage this move deals to the player. 0 for a defend/no-damage move.</summary>
        public readonly int Damage;

        /// <summary>Attack or Defend, for logic that should not depend on the label text.</summary>
        public readonly EnemyActionKind Kind;

        public EnemyAction(string label, int damage, EnemyActionKind kind)
        {
            Label = label;
            Damage = damage < 0 ? 0 : damage;
            Kind = kind;
        }

        /// <summary>Convenience factory for a damaging move.</summary>
        public static EnemyAction Attack(string label, int damage)
            => new EnemyAction(label, damage, EnemyActionKind.Attack);

        /// <summary>
        /// Convenience factory for a defend move. The label contains "Defend" so the
        /// existing IntentView icon rule (which looks for "defend") shows the shield
        /// icon with no UI changes required.
        /// </summary>
        public static EnemyAction Defend(string label = "Defend")
            => new EnemyAction(label, 0, EnemyActionKind.Defend);

        public override string ToString()
            => Kind == EnemyActionKind.Defend ? $"{Label}" : $"{Label} ({Damage})";
    }
}
