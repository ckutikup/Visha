using UnityEngine;
using Visha.Enemies;

namespace Visha.Campaign
{
    /// <summary>
    /// The data the campaign needs to set up one encounter. The enemy's moves and
    /// their damage come from the chosen <see cref="EnemyPatternRunner.PatternKind"/>,
    /// so balancing an enemy's damage stays in the pattern code.
    ///
    /// Create via Assets > Create > Visha > Encounter Config, then list the assets
    /// in order on <see cref="CampaignController"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Visha/Encounter Config", fileName = "EncounterConfig")]
    public class EncounterConfig : ScriptableObject
    {
        [Tooltip("Name shown on the HUD. Leave empty to use the pattern's own name.")]
        [SerializeField] private string displayName = "Temple Guardian";

        [Tooltip("Which enemy type and action pattern this encounter uses.")]
        [SerializeField] private EnemyPatternRunner.PatternKind pattern = EnemyPatternRunner.PatternKind.TempleGuardian;

        [Min(0)]
        [SerializeField] private int openingOffset;
        public int OpeningOffset => Mathf.Max(0, openingOffset);

        [Min(1)]
        [SerializeField] private int maxHealth = 24;

        [Tooltip("Health the enemy starts with. 0 means full health.")]
        [Min(0)]
        [SerializeField] private int startingHealth;

        [Tooltip("Optional sprite applied to the enemy's SpriteRenderer.")]
        [SerializeField] private Sprite enemySprite;

        public string DisplayName => displayName;
        public EnemyPatternRunner.PatternKind Pattern => pattern;
        public int MaxHealth => Mathf.Max(1, maxHealth);
        public int StartingHealth => startingHealth <= 0 ? MaxHealth : Mathf.Min(startingHealth, MaxHealth);
        public Sprite EnemySprite => enemySprite;

        /// <summary>Sets every field at once, for tests and editor tooling.</summary>
        public void Set(string newDisplayName, EnemyPatternRunner.PatternKind newPattern,
                        int newMaxHealth, int newStartingHealth = 0, Sprite newSprite = null, int newOpeningOffset = 0)
        {
            displayName = newDisplayName;
            pattern = newPattern;
            maxHealth = newMaxHealth;
            startingHealth = newStartingHealth;
            enemySprite = newSprite;
            openingOffset = Mathf.Max(0, newOpeningOffset);
        }
    }
}
