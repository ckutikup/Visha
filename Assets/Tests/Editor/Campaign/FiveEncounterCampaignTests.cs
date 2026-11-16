using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Visha.Battle;
using Visha.Campaign;
using Visha.Enemies;
using Visha.UI;

/// <summary>
/// The full five-encounter campaign with the shipped encounter assets: the Sanctum
/// unlocks only after the Inner Vault, the Asura is fought last with Temple's
/// Blessing, defeat and retry reset the boss to phase 1, winning shows the ending,
/// and replay, restart and return to title work across all five encounters.
/// </summary>
public class FiveEncounterCampaignTests
{
    private const string Folder = "Assets/Data/Encounters/";
    private const string ScenePath = "Assets/Scenes/Campaign_Demo.unity";
    private static readonly string[] EncounterPaths =
    {
        Folder + "Encounter1_TempleGuardian.asset",
        Folder + "Encounter2_GuardianWarden.asset",
        Folder + "Encounter3_VaultSentinel.asset",
        Folder + "Encounter4_InnerVault.asset",
        Folder + "Encounter5_TheAsura.asset"
    };
    private const int VaultIndex = 3, SanctumIndex = 4;

    private GameObject playerObject, firstObject, secondObject, systemsObject, screensObject;
    private Health playerHealth, bossHealth, secondHealth;
    private Poison bossPoison;
    private EnemyPatternRunner bossPattern;
    private TempleBlessing blessing;
    private BattleOutcomeController outcome;
    private CampaignController campaign;
    private VaultCampaignLink link;
    private CampaignScreens screens;
    private BattleScreenView view;

    [SetUp]
    public void SetUp()
    {
        playerObject = new GameObject("Player");
        playerHealth = playerObject.AddComponent<Health>();
        playerHealth.SetMaxHealth(50);
        blessing = playerObject.AddComponent<TempleBlessing>();

        firstObject = MakeEnemy("Enemy", out bossHealth, out bossPoison, out bossPattern);
        secondObject = MakeEnemy("SecondEnemy", out secondHealth, out _, out EnemyPatternRunner secondPattern);
        secondObject.SetActive(false);

        var encounters = new EncounterConfig[EncounterPaths.Length];
        for (int i = 0; i < encounters.Length; i++)
        {
            encounters[i] = AssetDatabase.LoadAssetAtPath<EncounterConfig>(EncounterPaths[i]);
            Assert.IsNotNull(encounters[i], $"missing {EncounterPaths[i]}");
        }
        var lineup = AssetDatabase.LoadAssetAtPath<VaultEncounter>(Folder + "VaultEncounter_GuardianAndSentinel.asset");

        systemsObject = new GameObject("Systems");
        outcome = systemsObject.AddComponent<BattleOutcomeController>();
        outcome.SetCombatants(playerHealth, bossHealth);
        campaign = systemsObject.AddComponent<CampaignController>();
        campaign.SetReferences(outcome, playerHealth, bossHealth, bossPattern, encounters);
        link = systemsObject.AddComponent<VaultCampaignLink>();
        link.SetReferences(campaign, outcome, encounters[VaultIndex], lineup, secondHealth, secondPattern);

        screensObject = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BattleScreens.prefab"));
        view = screensObject.GetComponent<BattleScreenView>();
        screens = systemsObject.AddComponent<CampaignScreens>();
        screens.SetReferences(campaign, view, null);
        screens.Show(campaign.State);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(screensObject);
        Object.DestroyImmediate(systemsObject);
        Object.DestroyImmediate(playerObject);
        Object.DestroyImmediate(firstObject);
        Object.DestroyImmediate(secondObject);
    }

    private static GameObject MakeEnemy(string name, out Health health, out Poison poison, out EnemyPatternRunner pattern)
    {
        var go = new GameObject(name);
        health = go.AddComponent<Health>();
        poison = go.AddComponent<Poison>();
        poison.SetValues(5, 2);
        pattern = go.AddComponent<EnemyPatternRunner>();
        return go;
    }

    /// <summary>Lethal damage until the player falls; the boss's Blessing absorbs the first hit.</summary>
    private void Lose()
    {
        playerHealth.TakeDamage(999);
        playerHealth.TakeDamage(999);
    }

    [Test]
    public void Blessing_SavesThePlayerOnce_PerBossAttempt()
    {
        ReachSanctum();

        playerHealth.TakeDamage(999);
        Assert.AreEqual(1, playerHealth.CurrentHealth, "the Blessing survives one lethal hit at 1 HP");
        Assert.AreEqual(CampaignState.InBattle, campaign.State);
        playerHealth.TakeDamage(999);
        Assert.AreEqual(CampaignState.Defeat, campaign.State, "only once per attempt");

        campaign.RetryEncounter();
        Assert.IsTrue(blessing.IsAvailable, "retry restores it");
        playerHealth.TakeDamage(999);
        Assert.AreEqual(1, playerHealth.CurrentHealth);
    }

    /// <summary>Wins the current encounter, whether it has one enemy or two.</summary>
    private void Win()
    {
        bossHealth.TakeDamage(999);
        if (link.SecondEnemyInPlay) secondHealth.TakeDamage(999);
    }

    private void ReachSanctum()
    {
        campaign.StartCampaign();
        for (int i = 0; i < SanctumIndex; i++) { Win(); campaign.NextEncounter(); }
        Assert.AreEqual(SanctumIndex + 1, campaign.EncounterNumber);
    }

    private void AssertBossAtStart()
    {
        Assert.AreEqual(CampaignState.InBattle, campaign.State);
        Assert.AreEqual(EnemyPatternRunner.PatternKind.Asura, bossPattern.Kind);
        Assert.AreEqual("The Asura", bossPattern.EnemyName);
        Assert.AreEqual(76, bossHealth.CurrentHealth);
        Assert.AreEqual(0, bossPoison.PoisonStacks);
        Assert.AreEqual(1, bossPattern.BossPhase);
        Assert.AreEqual(AsuraPattern.Recover.Label, bossPattern.IntentLabel);
        Assert.IsFalse(link.SecondEnemyInPlay, "the Asura fights alone");
        Assert.IsTrue(blessing.IsAvailable, "Temple's Blessing is ready for the boss");
    }

    private string CompleteText(string child) =>
        view.transform.Find("CompletePanel/" + child).GetComponent<Text>().text;

    [Test]
    public void CampaignScene_PlaysTheFiveEncountersInOrder_WithTheAsuraLast()
    {
        string scene = System.IO.File.ReadAllText(ScenePath);
        int previous = -1;
        foreach (string path in EncounterPaths)
        {
            int at = scene.IndexOf(AssetDatabase.AssetPathToGUID(path), System.StringComparison.Ordinal);
            Assert.Greater(at, previous, $"{path} is missing or out of order in {ScenePath}");
            previous = at;
        }
        Assert.AreEqual(5, campaign.EncounterCount);
    }

    [Test]
    public void Sanctum_UnlocksOnlyAfterTheInnerVault()
    {
        campaign.StartCampaign();
        for (int i = 0; i < VaultIndex; i++)
        {
            Assert.IsFalse(campaign.CanEnterEncounter(SanctumIndex));
            Win(); campaign.NextEncounter();
        }
        Assert.IsTrue(link.SecondEnemyInPlay, "encounter 4 is the two-enemy Inner Vault");
        Assert.IsFalse(campaign.CanEnterEncounter(SanctumIndex), "locked while the Vault is being fought");

        Win();
        Assert.AreEqual(CampaignState.Victory, campaign.State, "the Vault is no longer the last encounter");
        Assert.IsTrue(campaign.CanEnterEncounter(SanctumIndex));
    }

    [Test]
    public void Sanctum_StartsTheAsuraAlone_WithTheBlessing()
    {
        ReachSanctum();

        AssertBossAtStart();
    }

    [Test]
    public void Blessing_IsOnlyForTheBoss()
    {
        campaign.StartCampaign();
        Assert.IsFalse(blessing.IsEnabled, "solo encounters have no Blessing");

        ReachSanctumFromCurrent();
        Assert.IsTrue(blessing.IsEnabled);

        Win();
        Assert.IsTrue(campaign.EnterEncounter(0));
        Assert.IsFalse(blessing.IsEnabled, "replaying an earlier encounter turns it off again");
    }

    private void ReachSanctumFromCurrent()
    {
        while (campaign.EncounterNumber < SanctumIndex + 1) { Win(); campaign.NextEncounter(); }
    }

    [Test]
    public void BossDefeat_ThenRetry_ResetsTheBossToPhaseOneAndRestoresTheBlessing()
    {
        ReachSanctum();
        bossHealth.TakeDamage(40);
        Assert.IsTrue(bossPattern.UpdateBossPhase(bossHealth.CurrentHealth, bossHealth.MaxHealth));
        Assert.AreEqual(2, bossPattern.BossPhase);
        bossPoison.AddStacks(3);
        blessing.Spend();
        Lose();
        Assert.AreEqual(CampaignState.Defeat, campaign.State);

        campaign.RetryEncounter();

        AssertBossAtStart();
        Assert.AreEqual(50, playerHealth.CurrentHealth);
        Assert.AreEqual(SanctumIndex + 1, campaign.EncounterNumber);
    }

    [Test]
    public void WinningTheAsura_CompletesTheCampaign_AndShowsTheEnding()
    {
        ReachSanctum();

        Win();

        Assert.AreEqual(CampaignState.DemoComplete, campaign.State);
        Assert.AreEqual(SanctumIndex, campaign.HighestCompletedEncounter);
        Assert.AreEqual(BattleScreen.Complete, view.Screen);
        Assert.AreEqual(screens.EndingHeading, CompleteText("Heading"));
        Assert.AreEqual(screens.EndingMessage, CompleteText("Message"));
        Assert.AreEqual("THE CURSE IS BROKEN", CompleteText("Heading"));
    }

    [Test]
    public void AfterTheEnding_EveryEncounterCanBeReplayed()
    {
        ReachSanctum();
        Win();

        for (int i = 0; i < EncounterPaths.Length; i++)
            Assert.IsTrue(campaign.CanEnterEncounter(i), $"encounter {i + 1} can be replayed");

        Assert.IsTrue(campaign.EnterEncounter(VaultIndex));
        Assert.IsTrue(link.SecondEnemyInPlay, "replaying the Vault brings both enemies back");
        Win();
        Assert.AreEqual(CampaignState.Victory, campaign.State);

        Assert.IsTrue(campaign.EnterEncounter(SanctumIndex));
        AssertBossAtStart();
        Win();
        Assert.AreEqual(CampaignState.DemoComplete, campaign.State, "replaying the boss ends on the ending again");
    }

    [Test]
    public void Restart_FromTheBoss_GoesBackToEncounterOne()
    {
        ReachSanctum();
        bossHealth.TakeDamage(40);
        bossPattern.UpdateBossPhase(bossHealth.CurrentHealth, bossHealth.MaxHealth);
        Lose();

        campaign.RestartCampaign();

        Assert.AreEqual(1, campaign.EncounterNumber);
        Assert.AreEqual("Temple Guardian", bossPattern.EnemyName);
        Assert.AreEqual(0, bossPattern.BossPhase, "the Temple Guardian is not a boss");
        Assert.IsFalse(blessing.IsEnabled);
        Assert.IsFalse(campaign.CanEnterEncounter(SanctumIndex), "restart locks the Sanctum again");
    }

    [Test]
    public void ReturnToTitle_AfterTheEnding_LocksTheRouteAgain()
    {
        ReachSanctum();
        Win();

        campaign.ReturnToTitle();

        Assert.AreEqual(CampaignState.Title, campaign.State);
        Assert.AreEqual(-1, campaign.HighestCompletedEncounter);
        for (int i = 1; i < EncounterPaths.Length; i++)
            Assert.IsFalse(campaign.CanEnterEncounter(i), $"encounter {i + 1} is locked on a fresh start");
    }

    [Test]
    public void DefeatAndRetry_WorkInEveryEncounter()
    {
        campaign.StartCampaign();
        for (int i = 0; i < EncounterPaths.Length; i++)
        {
            Lose();
            Assert.AreEqual(CampaignState.Defeat, campaign.State, $"encounter {i + 1}");
            campaign.RetryEncounter();
            Assert.AreEqual(CampaignState.InBattle, campaign.State, $"encounter {i + 1}");
            Assert.AreEqual(i + 1, campaign.EncounterNumber, "retry stays on the same encounter");
            Assert.AreEqual(50, playerHealth.CurrentHealth);
            Assert.AreEqual(bossHealth.MaxHealth, bossHealth.CurrentHealth);

            Win();
            if (i < EncounterPaths.Length - 1) campaign.NextEncounter();
        }
        Assert.AreEqual(CampaignState.DemoComplete, campaign.State);
    }
}
