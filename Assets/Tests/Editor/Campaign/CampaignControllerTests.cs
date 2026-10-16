using NUnit.Framework;
using UnityEngine;
using Visha.Battle;
using Visha.Campaign;
using Visha.Enemies;

public class CampaignControllerTests
{
    private GameObject playerObject;
    private GameObject enemyObject;
    private GameObject systemsObject;
    private Health playerHealth;
    private Health enemyHealth;
    private Poison enemyPoison;
    private EnemyPatternRunner enemyPattern;
    private BattleOutcomeController outcome;
    private CampaignController campaign;
    private EncounterConfig[] encounters;

    [SetUp]
    public void SetUp()
    {
        playerObject = new GameObject("Player");
        playerHealth = playerObject.AddComponent<Health>();
        playerHealth.SetMaxHealth(30);

        enemyObject = new GameObject("Enemy");
        enemyHealth = enemyObject.AddComponent<Health>();
        enemyHealth.SetMaxHealth(24);
        enemyPoison = enemyObject.AddComponent<Poison>();
        enemyPoison.SetValues(5, 2);
        enemyPattern = enemyObject.AddComponent<EnemyPatternRunner>();

        encounters = new[]
        {
            CreateEncounter("Temple Guardian", 24),
            CreateEncounter("Guardian Sentinel", 30),
            CreateEncounter("Guardian Warden", 36)
        };

        systemsObject = new GameObject("Systems");
        outcome = systemsObject.AddComponent<BattleOutcomeController>();
        outcome.SetCombatants(playerHealth, enemyHealth);
        campaign = systemsObject.AddComponent<CampaignController>();
        campaign.SetReferences(outcome, playerHealth, enemyHealth, enemyPattern, encounters);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(systemsObject);
        Object.DestroyImmediate(playerObject);
        Object.DestroyImmediate(enemyObject);
        foreach (EncounterConfig config in encounters) Object.DestroyImmediate(config);
    }

    private static EncounterConfig CreateEncounter(string name, int maxHealth, int startingHealth = 0)
    {
        var config = ScriptableObject.CreateInstance<EncounterConfig>();
        config.Set(name, EnemyPatternRunner.PatternKind.TempleGuardian, maxHealth, startingHealth);
        return config;
    }

    private void WinEncounter() => enemyHealth.TakeDamage(999);
    private void LoseEncounter() => playerHealth.TakeDamage(999);

    [Test]
    public void BeforeStart_CampaignIsOnTitle()
    {
        Assert.AreEqual(CampaignState.Title, campaign.State);
    }

    [Test]
    public void Start_LoadsFirstEncounterConfig()
    {
        campaign.StartCampaign();

        Assert.AreEqual(CampaignState.InBattle, campaign.State);
        Assert.AreEqual(1, campaign.EncounterNumber);
        Assert.AreEqual("Temple Guardian", enemyPattern.EnemyName);
        Assert.AreEqual(24, enemyHealth.MaxHealth);
        Assert.AreEqual(24, enemyHealth.CurrentHealth);
    }

    [Test]
    public void Encounters_LoadInOrder_AndCompleteAfterThird()
    {
        campaign.StartCampaign();
        string[] names = { "Temple Guardian", "Guardian Sentinel", "Guardian Warden" };
        int[] health = { 24, 30, 36 };

        for (int i = 0; i < 3; i++)
        {
            Assert.AreEqual(i + 1, campaign.EncounterNumber);
            Assert.AreEqual(names[i], enemyPattern.EnemyName);
            Assert.AreEqual(health[i], enemyHealth.CurrentHealth);
            Assert.AreEqual(BattleOutcome.None, outcome.Outcome);

            WinEncounter();
            if (i < 2)
            {
                Assert.AreEqual(CampaignState.Victory, campaign.State);
                campaign.NextEncounter();
            }
        }

        Assert.AreEqual(CampaignState.DemoComplete, campaign.State);
    }

    [Test]
    public void Victory_WaitsForNext_BeforeLoadingAnotherEncounter()
    {
        campaign.StartCampaign();
        WinEncounter();

        Assert.AreEqual(CampaignState.Victory, campaign.State);
        Assert.AreEqual(1, campaign.EncounterNumber);
        Assert.IsTrue(enemyHealth.IsDead, "the defeated enemy stays down until Next");
    }

    [Test]
    public void Next_DuringBattle_DoesNothing()
    {
        campaign.StartCampaign();

        campaign.NextEncounter();

        Assert.AreEqual(1, campaign.EncounterNumber);
        Assert.AreEqual(CampaignState.InBattle, campaign.State);
    }

    [Test]
    public void Retry_ResetsHealthPoisonAndPattern_OnSameEncounter()
    {
        campaign.StartCampaign();
        WinEncounter();
        campaign.NextEncounter();

        enemyPoison.AddStacks(3);
        enemyHealth.TakeDamage(5);
        enemyPattern.TakeTurn();
        LoseEncounter();
        Assert.AreEqual(CampaignState.Defeat, campaign.State);

        campaign.RetryEncounter();

        Assert.AreEqual(CampaignState.InBattle, campaign.State);
        Assert.AreEqual(2, campaign.EncounterNumber);
        Assert.AreEqual(30, playerHealth.CurrentHealth);
        Assert.AreEqual(30, enemyHealth.CurrentHealth);
        Assert.AreEqual(0, enemyPoison.PoisonStacks);
        Assert.AreEqual(TempleGuardianPattern.Strike.Label, enemyPattern.IntentLabel);
        Assert.AreEqual(BattleOutcome.None, outcome.Outcome);
    }

    [Test]
    public void RetryThroughOutcomeController_AlsoReturnsCampaignToBattle()
    {
        campaign.StartCampaign();
        LoseEncounter();

        outcome.Retry();

        Assert.AreEqual(CampaignState.InBattle, campaign.State);
        Assert.AreEqual(30, playerHealth.CurrentHealth);
    }

    [Test]
    public void NewEncounter_StartsEnemyPatternFromFirstMove()
    {
        campaign.StartCampaign();
        enemyPattern.TakeTurn();
        WinEncounter();

        campaign.NextEncounter();

        Assert.AreEqual(TempleGuardianPattern.Strike.Label, enemyPattern.IntentLabel);
    }

    [Test]
    public void RestoreToFull_PlayerStartsEachEncounterAtFullHealth()
    {
        campaign.PlayerHealthRule = PlayerHealthBetweenEncounters.RestoreToFull;
        campaign.StartCampaign();
        playerHealth.TakeDamage(10);
        WinEncounter();

        campaign.NextEncounter();

        Assert.AreEqual(30, playerHealth.CurrentHealth);
    }

    [Test]
    public void CarryOver_PlayerKeepsHealth_AndRetryRestoresEncounterStart()
    {
        campaign.PlayerHealthRule = PlayerHealthBetweenEncounters.CarryOver;
        campaign.StartCampaign();
        playerHealth.TakeDamage(10);
        WinEncounter();

        campaign.NextEncounter();
        Assert.AreEqual(20, playerHealth.CurrentHealth);

        LoseEncounter();
        campaign.RetryEncounter();
        Assert.AreEqual(20, playerHealth.CurrentHealth, "retry restarts from the health the encounter began with");
    }

    [Test]
    public void StartingHealth_BelowMaximum_IsAppliedOnStartAndRetry()
    {
        encounters[0].Set("Wounded Guardian", EnemyPatternRunner.PatternKind.TempleGuardian, 24, 10);
        campaign.StartCampaign();
        Assert.AreEqual(24, enemyHealth.MaxHealth);
        Assert.AreEqual(10, enemyHealth.CurrentHealth);

        LoseEncounter();
        campaign.RetryEncounter();
        Assert.AreEqual(10, enemyHealth.CurrentHealth);
    }

    [Test]
    public void Restart_AfterDemoComplete_ResetsEncounterIndexAndState()
    {
        campaign.PlayerHealthRule = PlayerHealthBetweenEncounters.CarryOver;
        campaign.StartCampaign();
        WinEncounter(); campaign.NextEncounter();
        WinEncounter(); campaign.NextEncounter();
        playerHealth.TakeDamage(12);
        WinEncounter();
        Assert.AreEqual(CampaignState.DemoComplete, campaign.State);

        campaign.RestartCampaign();

        Assert.AreEqual(CampaignState.InBattle, campaign.State);
        Assert.AreEqual(1, campaign.EncounterNumber);
        Assert.AreEqual("Temple Guardian", enemyPattern.EnemyName);
        Assert.AreEqual(24, enemyHealth.CurrentHealth);
        Assert.AreEqual(30, playerHealth.CurrentHealth);
        Assert.AreEqual(BattleOutcome.None, outcome.Outcome);
    }

    [Test]
    public void StateChanged_ReportsEachScreenTheUiShouldShow()
    {
        var seen = new System.Collections.Generic.List<CampaignState>();
        campaign.StateChanged += seen.Add;

        campaign.StartCampaign();
        LoseEncounter();
        campaign.RetryEncounter();
        WinEncounter();

        CollectionAssert.AreEqual(new[]
        {
            CampaignState.InBattle, CampaignState.Defeat, CampaignState.InBattle, CampaignState.Victory
        }, seen);
    }
}
