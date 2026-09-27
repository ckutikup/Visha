namespace Visha.Enemies
{
    /// <summary>
    /// The one enemy attack pattern for this milestone: the Temple Guardian.
    ///
    /// It is a fixed three-move cycle, chosen so the player has a real decision every
    /// turn once they have read it:
    ///
    ///   0. Strike      — light 4-damage hit. A normal turn.
    ///   1. Heavy Blow  — telegraphed 9-damage hit. Because the player is warned a full
    ///                    turn ahead, this is the turn to use Fade. It teaches Fade timing.
    ///   2. Defend      — the Guardian braces and deals no damage. A safe window to stack
    ///                    Venom Strike (build poison) instead of playing defensively.
    ///
    /// The cycle then repeats. Nothing here is random: the same sequence plays every
    /// fight, so mastering the Guardian is purely about reading and responding.
    ///
    /// Damage values are this milestone's starting point for playtesting, not final
    /// balance — they are easy to tune in one place here.
    /// </summary>
    public static class TempleGuardianPattern
    {
        public const string EnemyName = "Temple Guardian";

        public static EnemyAction Strike    => EnemyAction.Attack("Strike", 4);
        public static EnemyAction HeavyBlow => EnemyAction.Attack("Heavy Blow", 9);
        public static EnemyAction Brace     => EnemyAction.Defend("Defend");

        /// <summary>Builds a fresh Temple Guardian pattern, starting on Strike.</summary>
        public static EnemyAttackPattern Create()
        {
            return new EnemyAttackPattern(EnemyName, new[]
            {
                Strike,
                HeavyBlow,
                Brace
            });
        }
    }
}
