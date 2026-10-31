using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Visha.Battle;
using Visha.Campaign;
using Visha.Enemies;

/// <summary>
/// The two-enemy Vault inside the campaign, through VaultCampaignLink and the
/// unchanged CampaignController: it unlocks after the solo encounters, sets up
/// both enemies from Anshul's lineup, waits for both to fall before victory, and
/// resets both on retry, replay and restart. Solo encounters keep the second
/// enemy out of play.
/// </summary>
public class TwoEnemyCampaignTests
{
    private const int VaultIndex = 2;
    private const string VaultConfigPath = "Assets/Data/Encounters/Encounter4_InnerVault.asset";
    private const string LineupPath = "Assets/Data/Encounters/VaultEncounter_GuardianAndSentinel.asset";

    private GameObject playerObject, firstObject, secondObject, systemsObject;
    private Health playerHealth, firstHealth, secondHealth;
    private Poison firstPoison, secondPoison;
    private EnemyPatternRunner firstPattern, secondPattern;
    private BattleOutcomeController outcome;
    private CampaignController campaign;
    private VaultCampaignLink link;
    private VaultEncounter lineup;
    private EncounterConfig[] encounters;

    [SetUp]
    public void SetUp()
    {
        playerObject = new GameObject("Player");
        playerHealth = playerObject.AddComponent<Health>();
        playerHealth.SetMaxHealth(50);

        firstObject = MakeEnemy("Enemy", out firstHealth, out firstPoison, out firstPattern);
        secondObject = MakeEnemy("SecondEnemy", out secondHealth, out secondPoison, out secondPattern);
        secondObject.SetActive(false);

        lineup = ScriptableObject.CreateInstance<VaultEncounter>();
        lineup.ApplyDefaultVault();
        encounters = new[]
        {
            CreateEncounter("Temple Guardian", EnemyPatternRunner.PatternKind.TempleGuardian, 24),
            CreateEncounter("Guardian Warden", EnemyPatternRunner.PatternKind.TempleGuardian, 30),
            CreateEncounter("Vault Guardian", EnemyPatternRunner.PatternKind.TempleGuardian, 18)
        };

        systemsObject = new GameObject("Systems");
        outcome = systemsObject.AddComponent<BattleOutcomeController>();
        outcome.SetCombatants(playerHealth, firstHealth);
        campaign = systemsObject.AddComponent<CampaignController>();
        campaign.SetReferences(outcome, playerHealth, firstHealth, firstPattern, encounters);
        link = systemsObject.AddComponent<VaultCampaignLink>();
        link.SetReferences(campaign, outcome, encounters[VaultIndex], lineup, secondHealth, secondPattern);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(systemsObject);
        Object.DestroyImmediate(playerObject);
        Object.DestroyImmediate(firstObject);
        Object.DestroyImmediate(secondObject);
        foreach (EncounterConfig config in encounters) Object.DestroyImmediate(config);
        Object.DestroyImmediate(lineup);
    }

    private static GameObject MakeEnemy(string name, out Health health, out Poison poison, out EnemyPatternRunner pattern)
    {
        var go = new GameObject(name);
        health = go.AddComponent<Health>();
        health.SetMaxHealth(24);
        poison = go.AddComponent<Poison>();
        poison.SetValues(5, 2);
        pattern = go.AddComponent<EnemyPatternRunner>();
        return go;
    }

    private static EncounterConfig CreateEncounter(string name, EnemyPatternRunner.PatternKind kind, int maxHealth)
    {
        var config = ScriptableObject.CreateInstance<EncounterConfig>();
        config.Set(name, kind, maxHealth);
        return config;
    }

    private void Lose() => playerHealth.TakeDamage(999);

    private void ReachVault()
    {
        campaign.StartCampaign();
        firstHealth.TakeDamage(999); campaign.NextEncounter();
        firstHealth.TakeDamage(999); campaign.NextEncounter();
        Assert.AreEqual(VaultIndex + 1, campaign.EncounterNumber);
    }

    private void AssertVaultAtStart()
    {
        VaultEncounter.EnemyPlacement guardian = lineup.Enemy(0), sentinel = lineup.Enemy(1);
        Assert.IsTrue(link.SecondEnemyInPlay);
        Assert.AreEqual(BattleOutcome.None, outcome.Outcome);

        Assert.AreEqual(guardian.ResolvedName, firstPattern.EnemyName);
        Assert.AreEqual(guardian.StartingHealth, firstHealth.CurrentHealth);
        Assert.AreEqual(0, firstPoison.PoisonStacks);
        Assert.AreEqual(guardian.OpeningIntent.Label, firstPattern.IntentLabel);

        Assert.AreEqual(sentinel.ResolvedName, secondPattern.EnemyName);
        Assert.AreEqual(sentinel.MaxHealth, secondHealth.MaxHealth);
        Assert.AreEqual(sentinel.StartingHealth, secondHealth.CurrentHealth);
        Assert.AreEqual(0, secondPoison.PoisonStacks);
        Assert.AreEqual(sentinel.OpeningIntent.Label, secondPattern.IntentLabel);
    }

    [Test]
    public void ShippedVaultConfig_MatchesTheLineupsFirstEnemy()
    {
        var config = AssetDatabase.LoadAssetAtPath<EncounterConfig>(VaultConfigPath);
        var shipped = AssetDatabase.LoadAssetAtPath<VaultEncounter>(LineupPath);
        Assert.IsNotNull(config, $"missing {VaultConfigPath}");
        Assert.IsNotNull(shipped, $"missing {LineupPath}");
        Assert.IsTrue(shipped.IsValid);

        VaultEncounter.EnemyPlacement first = shipped.Enemy(0);
        Assert.AreEqual(0, first.TargetId);
        Assert.AreEqual(first.ResolvedName, config.DisplayName);
        Assert.AreEqual(first.Pattern, config.Pattern);
        Assert.AreEqual(first.MaxHealth, config.MaxHealth);
        Assert.AreEqual(first.StartingHealth, config.StartingHealth);
        Assert.AreEqual(first.OpeningOffset, config.OpeningOffset);
    }

    [Test]
    public void SoloEncounters_KeepSecondEnemyOutOfPlay()
    {
        campaign.StartCampaign();

        Assert.IsFalse(link.SecondEnemyInPlay);
        firstHealth.TakeDamage(999);
        Assert.AreEqual(CampaignState.Victory, campaign.State, "a solo encounter ends on its one enemy");
    }

    [Test]
    public void Vault_UnlocksOnlyAfterSoloEncountersAreCleared()
    {
        Assert.IsFalse(campaign.CanEnterEncounter(VaultIndex));

        campaign.StartCampaign();
        firstHealth.TakeDamage(999);
        Assert.IsFalse(campaign.CanEnterEncounter(VaultIndex));

        campaign.NextEncounter();
        firstHealth.TakeDamage(999);
        Assert.IsTrue(campaign.CanEnterEncounter(VaultIndex));
    }

    [Test]
    public void Vault_SetsUpBothEnemiesFromTheLineup()
    {
        ReachVault();

        AssertVaultAtStart();
        Assert.AreEqual(18, firstHealth.MaxHealth);
        Assert.AreEqual(26, secondHealth.MaxHealth);
    }

    [Test]
    public void Vault_VictoryOnlyAfterBothEnemiesAreDefeated()
    {
        ReachVault();

        firstHealth.TakeDamage(999);
        Assert.AreEqual(CampaignState.InBattle, campaign.State);
        Assert.AreEqual(BattleOutcome.None, outcome.Outcome);
        Assert.AreEqual(1, campaign.HighestCompletedEncounter, "the Vault is not cleared while one enemy stands");

        secondHealth.TakeDamage(999);
        Assert.AreEqual(CampaignState.DemoComplete, campaign.State);
        Assert.AreEqual(VaultIndex, campaign.HighestCompletedEncounter);
    }

    [Test]
    public void Vault_DefeatThenRetry_ResetsBothEnemies()
    {
        ReachVault();
        firstPoison.AddStacks(3);
        secondPoison.AddStacks(2);
        secondHealth.TakeDamage(10);
        firstPattern.TakeTurn();
        secondPattern.TakeTurn();
        firstHealth.TakeDamage(999);
        Lose();
        Assert.AreEqual(CampaignState.Defeat, campaign.State);

        campaign.RetryEncounter();

        Assert.AreEqual(CampaignState.InBattle, campaign.State);
        Assert.AreEqual(VaultIndex + 1, campaign.EncounterNumber);
        Assert.AreEqual(50, playerHealth.CurrentHealth);
        AssertVaultAtStart();

        firstHealth.TakeDamage(999);
        Assert.AreEqual(CampaignState.InBattle, campaign.State, "the retried Vault still needs both enemies down");
    }

    [Test]
    public void Vault_RetryThroughOutcomeController_AlsoResetsBothEnemies()
    {
        ReachVault();
        secondHealth.TakeDamage(999);
        Lose();

        outcome.Retry();

        Assert.AreEqual(CampaignState.InBattle, campaign.State);
        AssertVaultAtStart();
    }

    [Test]
    public void Vault_SecondEnemyStartingHealthIsAppliedOnStartAndRetry()
    {
        lineup.Enemy(1).Set(1, EnemyPatternRunner.PatternKind.VaultSentinel, 26, 2, newStartingHealth: 20);
        ReachVault();
        Assert.AreEqual(20, secondHealth.CurrentHealth);

        Lose();
        campaign.RetryEncounter();

        Assert.AreEqual(20, secondHealth.CurrentHealth);
    }

    [Test]
    public void ReplaySoloAfterVault_TakesSecondEnemyOutOfPlay()
    {
        ReachVault();
        firstHealth.TakeDamage(999);
        secondHealth.TakeDamage(999);

        Assert.IsTrue(campaign.EnterEncounter(0));

        Assert.IsFalse(link.SecondEnemyInPlay);
        Assert.AreEqual("Temple Guardian", firstPattern.EnemyName);
        Assert.AreEqual(24, firstHealth.CurrentHealth);
        firstHealth.TakeDamage(999);
        Assert.AreEqual(CampaignState.Victory, campaign.State, "the replayed solo encounter is won without the second enemy");
    }

    [Test]
    public void ReplayVault_ResetsBothEnemies()
    {
        ReachVault();
        secondPoison.AddStacks(4);
        secondPattern.TakeTurn();
        firstHealth.TakeDamage(999);
        secondHealth.TakeDamage(999);

        Assert.IsTrue(campaign.EnterEncounter(VaultIndex));

        Assert.AreEqual(CampaignState.InBattle, campaign.State);
        AssertVaultAtStart();
    }

    [Test]
    public void RestartOrReturnToTitle_FromVault_TakesSecondEnemyOutOfPlay()
    {
        ReachVault();
        Lose();

        campaign.RestartCampaign();
        Assert.AreEqual(1, campaign.EncounterNumber);
        Assert.IsFalse(link.SecondEnemyInPlay);
        Assert.AreEqual("Temple Guardian", firstPattern.EnemyName);
        Assert.AreEqual(24, firstHealth.CurrentHealth);

        firstHealth.TakeDamage(999); campaign.NextEncounter();
        firstHealth.TakeDamage(999); campaign.NextEncounter();
        Assert.IsTrue(link.SecondEnemyInPlay);
        Lose();

        campaign.ReturnToTitle();
        Assert.AreEqual(CampaignState.Title, campaign.State);
        Assert.IsFalse(link.SecondEnemyInPlay);
    }
}
