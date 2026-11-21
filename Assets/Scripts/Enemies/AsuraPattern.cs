using UnityEngine;

namespace Visha.Enemies
{
    /// <summary>
    /// The final boss in the Sanctum: the Asura, from the team's approved concept.
    /// It has two phases. Each phase is a fixed, repeating cycle, so every move is
    /// telegraphed a turn ahead and nothing is random.
    ///
    /// Phase 1 (full health down to half):
    ///   0. Recover     — 0 damage. A free turn to start poison or Bite.
    ///   1. Cursed Claw — 10 damage.
    ///   2. Ruin        — 16 damage, the phase 1 big hit. Fade halves it to 8.
    ///
    /// Phase 2 (half health or below; starts on its first move):
    ///   0. Roar        — 0 damage. The phase change itself gives the player a free
    ///                    turn to counterattack, and it comes back every cycle.
    ///   1. Cleave      — 12 damage.
    ///   2. Hex         — 8 damage.
    ///   3. Cataclysm   — 22 damage, the biggest hit in the game. Fade halves it to 11.
    ///
    /// Every attack is 8 or more, so Fading any of them earns an Awakening charge
    /// (the combat flow's existing rule). Phase 2 hits harder and has a longer cycle,
    /// but it still has a free turn every cycle and one telegraphed big hit to Fade.
    ///
    /// Labels stay at 11 characters or fewer so the HUD's intent box shows them whole.
    /// Damage values are playtest starting points, tuned in one place here.
    /// </summary>
    public static class AsuraPattern
    {
        public const string EnemyName = "The Asura";

        /// <summary>Number of phases, for BattleViewState.bossPhaseCount.</summary>
        public const int PhaseCount = 2;

        /// <summary>Phase 2 starts once health is at or below this fraction of max health.</summary>
        public const float PhaseTwoHealthFraction = 0.5f;

        // Phase 1
        public static EnemyAction Recover    => EnemyAction.Defend("Recover");
        public static EnemyAction CursedClaw => EnemyAction.Attack("Cursed Claw", 10);
        public static EnemyAction Ruin       => EnemyAction.Attack("Ruin", 16);

        // Phase 2
        public static EnemyAction Roar       => EnemyAction.Defend("Roar");
        public static EnemyAction Cleave     => EnemyAction.Attack("Cleave", 12);
        public static EnemyAction Hex        => EnemyAction.Attack("Hex", 8);
        public static EnemyAction Cataclysm  => EnemyAction.Attack("Cataclysm", 22);

        /// <summary>Builds a fresh phase 1 pattern, starting on Recover.</summary>
        public static EnemyAttackPattern CreatePhaseOne()
        {
            return new EnemyAttackPattern(EnemyName, new[] { Recover, CursedClaw, Ruin });
        }

        /// <summary>Builds a fresh phase 2 pattern, starting on Roar.</summary>
        public static EnemyAttackPattern CreatePhaseTwo()
        {
            return new EnemyAttackPattern(EnemyName, new[] { Roar, Cleave, Hex, Cataclysm });
        }

        /// <summary>
        /// Health at or below which phase 2 starts (half of max health, rounded down).
        /// With the current 70 max health this is 35.
        /// </summary>
        public static int PhaseTwoThreshold(int maxHealth)
        {
            return Mathf.FloorToInt(Mathf.Max(1, maxHealth) * PhaseTwoHealthFraction);
        }
    }
}
