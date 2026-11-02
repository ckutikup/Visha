using System.Collections.Generic;
using UnityEngine;
using Visha.Enemies;

namespace Visha.Campaign
{
    /// <summary>
    /// Configures the two-enemy Vault encounter from the two existing enemy types.
    ///
    /// Where <see cref="EncounterConfig"/> describes a single enemy, a Vault encounter
    /// is two enemies that act in the same round, so it holds two
    /// <see cref="EnemyPlacement"/> records. Each record gives one enemy a stable
    /// target id, an enemy type (pattern), starting health and an opening offset.
    ///
    /// Difficulty is tuned entirely through the three per-enemy knobs here — pattern,
    /// max health and opening offset — because the moves and their damage live in the
    /// pattern code (TempleGuardianPattern / VaultSentinelPattern). Nothing here is
    /// random.
    ///
    /// Consumption (see Docs/VaultEncounter.md):
    ///   - Rahul's flow spawns one combatant per placement, keyed by TargetId, and each
    ///     turn copies the placement's live runner telegraph into the matching
    ///     BattleViewState.enemies entry (name, intention, incomingDamage).
    ///   - Charith's HUD renders those entries; ids stay stable even after a death.
    ///   - Shivani resets both enemies on start/retry; Akshat assigns EnemySprite.
    ///
    /// Create via Assets > Create > Visha > Vault Encounter, or with the
    /// "Visha/Setup/Create Vault Encounter Asset" menu once the two-enemy flow lands.
    /// </summary>
    [CreateAssetMenu(menuName = "Visha/Vault Encounter", fileName = "VaultEncounter")]
    public class VaultEncounter : ScriptableObject
    {
        /// <summary>One enemy within a multi-enemy encounter. Mirrors EncounterConfig's
        /// per-enemy fields and adds the stable target id the HUD and combat share.</summary>
        [System.Serializable]
        public class EnemyPlacement
        {
            [Tooltip("Stable, non-negative id shared with combat and the HUD. Never renumber on death.")]
            [Min(0)]
            [SerializeField] private int targetId;

            [Tooltip("Name shown on the HUD. Leave empty to use the pattern's own name.")]
            [SerializeField] private string displayName;

            [Tooltip("Which enemy type and action pattern this enemy uses.")]
            [SerializeField] private EnemyPatternRunner.PatternKind pattern = EnemyPatternRunner.PatternKind.TempleGuardian;

            [Tooltip("Max health for this enemy.")]
            [Min(1)]
            [SerializeField] private int maxHealth = 24;

            [Tooltip("Health this enemy starts with. 0 means full health.")]
            [Min(0)]
            [SerializeField] private int startingHealth;

            [Tooltip("Advances the pattern this many steps at the start, to stagger the two enemies so their big hits land on different turns.")]
            [Min(0)]
            [SerializeField] private int openingOffset;

            [Tooltip("Optional sprite applied to this enemy's SpriteRenderer (assigned by art).")]
            [SerializeField] private Sprite enemySprite;

            public int TargetId => Mathf.Max(0, targetId);
            public EnemyPatternRunner.PatternKind Pattern => pattern;
            public int MaxHealth => Mathf.Max(1, maxHealth);
            public int StartingHealth => startingHealth <= 0 ? MaxHealth : Mathf.Min(startingHealth, MaxHealth);
            public int OpeningOffset => Mathf.Max(0, openingOffset);
            public Sprite EnemySprite => enemySprite;

            /// <summary>Display name for the HUD: the override, or the pattern's own name.</summary>
            public string ResolvedName =>
                string.IsNullOrEmpty(displayName) ? EnemyPatternRunner.CreatePattern(pattern).Name : displayName;

            /// <summary>
            /// Builds this enemy's pattern positioned at its opening move (after the
            /// offset is applied), so callers and tests see exactly what the enemy will
            /// telegraph on turn one.
            /// </summary>
            public EnemyAttackPattern BuildPattern()
            {
                EnemyAttackPattern p = EnemyPatternRunner.CreatePattern(pattern);
                int steps = OpeningOffset % p.Length;
                for (int i = 0; i < steps; i++) p.Advance();
                return p;
            }

            /// <summary>The move this enemy telegraphs at the start of the encounter.</summary>
            public EnemyAction OpeningIntent => BuildPattern().Current;

            public void Set(int newTargetId, EnemyPatternRunner.PatternKind newPattern, int newMaxHealth,
                            int newOpeningOffset = 0, int newStartingHealth = 0,
                            string newDisplayName = null, Sprite newSprite = null)
            {
                targetId = Mathf.Max(0, newTargetId);
                pattern = newPattern;
                maxHealth = Mathf.Max(1, newMaxHealth);
                openingOffset = Mathf.Max(0, newOpeningOffset);
                startingHealth = Mathf.Max(0, newStartingHealth);
                displayName = newDisplayName;
                enemySprite = newSprite;
            }
        }

        [Tooltip("Name shown for the encounter as a whole (e.g. on the map).")]
        [SerializeField] private string encounterName = "The Vault";

        [Tooltip("The enemies in this encounter. The Vault uses exactly two, with unique ids.")]
        [SerializeField]
        private List<EnemyPlacement> enemies = new List<EnemyPlacement>();

        public string EncounterName => encounterName;
        public int Count => enemies.Count;
        public IReadOnlyList<EnemyPlacement> Enemies => enemies;
        public EnemyPlacement Enemy(int index) => enemies[index];

        /// <summary>True when this is a well-formed Vault encounter: exactly two enemies
        /// with unique, non-negative ids.</summary>
        public bool IsValid
        {
            get
            {
                if (enemies == null || enemies.Count != 2) return false;
                if (enemies[0] == null || enemies[1] == null) return false;
                return enemies[0].TargetId != enemies[1].TargetId;
            }
        }

        /// <summary>
        /// Combined damage the two enemies telegraph on a given turn of the encounter,
        /// before any Fade. Turn 0 is the first enemy phase. Used by the difficulty
        /// tests and handy for balancing.
        /// </summary>
        public int CombinedIncomingOnTurn(int turn)
        {
            int total = 0;
            foreach (EnemyPlacement e in enemies)
            {
                if (e == null) continue;
                EnemyAttackPattern p = e.BuildPattern();
                total += p.Peek(Mathf.Max(0, turn)).Damage;
            }
            return total;
        }

        private void OnValidate()
        {
            // Keep the default asset pointed at the agreed two-enemy Vault setup.
            if (enemies == null || enemies.Count == 0) ApplyDefaultVault();
        }

        /// <summary>
        /// Fills in the agreed starting values for the Vault (see Docs/VaultEncounter.md):
        /// a lower-health Temple Guardian (id 0) and a tankier Vault Sentinel (id 1)
        /// whose opening offset staggers its Crush away from the Guardian's Heavy Blow.
        /// These are playtest starting values, tuned to be tight but fair, not final.
        /// </summary>
        public void ApplyDefaultVault()
        {
            encounterName = "The Vault";
            enemies = new List<EnemyPlacement>(2);

            EnemyPlacement guardian = new EnemyPlacement();
            guardian.Set(0, EnemyPatternRunner.PatternKind.TempleGuardian,
                         newMaxHealth: 18, newOpeningOffset: 0, newDisplayName: "Vault Guardian");

            EnemyPlacement sentinel = new EnemyPlacement();
            sentinel.Set(1, EnemyPatternRunner.PatternKind.VaultSentinel,
                         newMaxHealth: 26, newOpeningOffset: 2, newDisplayName: "Vault Sentinel");

            enemies.Add(guardian);
            enemies.Add(sentinel);
        }
    }
}
