namespace Visha.Enemies
{
    /// <summary>
    /// The second regular enemy type: the Vault Sentinel, from the team's approved
    /// concept (Recover, then a big hit, then a medium hit).
    ///
    /// It plays differently from the Temple Guardian:
    ///
    ///   0. Recover      — the Sentinel recovers and deals no damage. The fight opens
    ///                    with a free turn to start building poison.
    ///   1. Crush       — telegraphed 16-damage hit, the biggest regular hit in the
    ///                    demo. Fade halves it to 8, so Fade timing matters most here.
    ///   2. Shield Bash — 8-damage hit straight after. Fade can't be used two turns in
    ///                    a row, so the player can only soften one of the two hits.
    ///
    /// The Guardian asks "when do I Fade?"; the Sentinel asks "which hit do I Fade,
    /// and can I Bite it down before the next Crush?".
    ///
    /// Labels stay short so the HUD's intent box shows them whole.
    ///
    /// Damage values are this milestone's starting point for playtesting, not final
    /// balance. They are easy to tune in one place here.
    /// </summary>
    public static class VaultSentinelPattern
    {
        public const string EnemyName = "Vault Sentinel";

        public static EnemyAction Brace      => EnemyAction.Defend("Recover");
        public static EnemyAction Crush      => EnemyAction.Attack("Crush", 16);
        public static EnemyAction ShieldBash => EnemyAction.Attack("Shield Bash", 8);

        /// <summary>Builds a fresh Vault Sentinel pattern, starting on Recover.</summary>
        public static EnemyAttackPattern Create()
        {
            return new EnemyAttackPattern(EnemyName, new[]
            {
                Brace,
                Crush,
                ShieldBash
            });
        }
    }
}
