#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Visha.Campaign;
using Visha.Enemies;
using Visha.UI;

/// <summary>
/// Plays Campaign_Demo through its real buttons and the real CombatFlowController:
/// start, win, Next, lose, Retry, finish all three encounters, and Restart.
/// Lethal damage is applied directly so the test does not depend on balance values.
/// </summary>
public class CampaignDemoPlayModeTests
{
    private const string ScenePath = "Assets/Scenes/Campaign_Demo.unity";

    private CampaignController campaign;
    private BattleHUD hud;
    private Health playerHealth;
    private Health enemyHealth;
    private Poison enemyPoison;
    private EnemyPatternRunner enemyPattern;
    private Transform canvas;

    [UnitySetUp]
    public IEnumerator LoadScene()
    {
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;

        campaign = Object.FindAnyObjectByType<CampaignController>();
        hud = Object.FindAnyObjectByType<BattleHUD>();
        enemyPattern = Object.FindAnyObjectByType<EnemyPatternRunner>();
        enemyHealth = enemyPattern.GetComponent<Health>();
        enemyPoison = enemyPattern.GetComponent<Poison>();
        playerHealth = GameObject.Find("Player").GetComponent<Health>();
        canvas = GameObject.Find("CampaignCanvas").transform;
    }

    private bool ScreenVisible(string name) => canvas.Find(name).gameObject.activeSelf;

    private void Click(string path)
    {
        Button button = canvas.Find(path).GetComponent<Button>();
        Assert.IsTrue(button.gameObject.activeInHierarchy, $"{path} should be visible");
        button.onClick.Invoke();
    }

    /// <summary>Uses Venom Strike through the HUD and checks the turn loop accepted it.</summary>
    private IEnumerator AssertBattleAcceptsActions()
    {
        int stacksBefore = enemyPoison.PoisonStacks;
        hud.ChooseAbility(BattleAbility.VenomStrike);
        hud.SelectTarget();
        Assert.Greater(enemyPoison.PoisonStacks, stacksBefore, "Venom Strike should apply during the player turn");
        yield return null; // enemy turn
        yield return null;
    }

    [UnityTest]
    public IEnumerator FullCampaign_StartWinNextLoseRetryFinishRestart()
    {
        Assert.AreEqual(CampaignState.Title, campaign.State);
        Assert.IsTrue(ScreenVisible("TitleScreen"));

        Click("TitleScreen/StartButton");
        yield return null;
        Assert.IsFalse(ScreenVisible("TitleScreen"));
        Assert.AreEqual(1, campaign.EncounterNumber);
        Assert.AreEqual("Temple Guardian", enemyPattern.EnemyName);
        yield return AssertBattleAcceptsActions();

        // Encounter 1: win, then wait for Next
        enemyHealth.TakeDamage(999);
        yield return null;
        Assert.AreEqual(CampaignState.Victory, campaign.State);
        Assert.IsTrue(ScreenVisible("VictoryScreen"));
        Assert.AreEqual(1, campaign.EncounterNumber);

        Click("VictoryScreen/Card/NextButton");
        yield return null;
        Assert.IsFalse(ScreenVisible("VictoryScreen"));
        Assert.AreEqual(2, campaign.EncounterNumber);
        Assert.AreEqual("Guardian Warden", enemyPattern.EnemyName);
        Assert.AreEqual(44, enemyHealth.CurrentHealth);
        Assert.AreEqual(0, enemyPoison.PoisonStacks);
        yield return AssertBattleAcceptsActions();

        // Encounter 2: lose, then Retry the same encounter
        playerHealth.TakeDamage(999);
        yield return null;
        Assert.AreEqual(CampaignState.Defeat, campaign.State);
        Assert.IsTrue(ScreenVisible("DefeatScreen"));

        Click("DefeatScreen/Card/RetryButton");
        yield return null;
        Assert.IsFalse(ScreenVisible("DefeatScreen"));
        Assert.AreEqual(2, campaign.EncounterNumber);
        Assert.AreEqual(playerHealth.MaxHealth, playerHealth.CurrentHealth);
        Assert.AreEqual(44, enemyHealth.CurrentHealth);
        Assert.AreEqual(0, enemyPoison.PoisonStacks);
        Assert.AreEqual(TempleGuardianPattern.Strike.Label, enemyPattern.IntentLabel);
        yield return AssertBattleAcceptsActions();

        // Encounters 2 and 3: win both, the last one ends the demo
        enemyHealth.TakeDamage(999);
        yield return null;
        Click("VictoryScreen/Card/NextButton");
        yield return null;
        Assert.AreEqual(3, campaign.EncounterNumber);
        Assert.AreEqual("Vault Sentinel", enemyPattern.EnemyName);
        Assert.AreEqual(46, enemyHealth.CurrentHealth);
        yield return AssertBattleAcceptsActions();

        enemyHealth.TakeDamage(999);
        yield return null;
        Assert.AreEqual(CampaignState.DemoComplete, campaign.State);
        Assert.IsTrue(ScreenVisible("EndScreen"));
        Assert.IsFalse(ScreenVisible("VictoryScreen"));

        // Restart: back to encounter 1 with a fresh battle
        Click("EndScreen/RestartButton");
        yield return null;
        Assert.IsFalse(ScreenVisible("EndScreen"));
        Assert.AreEqual(CampaignState.InBattle, campaign.State);
        Assert.AreEqual(1, campaign.EncounterNumber);
        Assert.AreEqual("Temple Guardian", enemyPattern.EnemyName);
        Assert.AreEqual(30, enemyHealth.CurrentHealth);
        Assert.AreEqual(playerHealth.MaxHealth, playerHealth.CurrentHealth);
        yield return AssertBattleAcceptsActions();
    }

    [UnityTest]
    public IEnumerator ReturnToStart_FromDefeat_ShowsTitleAndStartsFresh()
    {
        Click("TitleScreen/StartButton");
        yield return null;
        enemyHealth.TakeDamage(999);
        yield return null;
        Click("VictoryScreen/Card/NextButton");
        yield return null;
        playerHealth.TakeDamage(999);
        yield return null;

        Click("DefeatScreen/Card/ReturnToStartButton");
        yield return null;
        Assert.IsTrue(ScreenVisible("TitleScreen"));
        Assert.IsFalse(ScreenVisible("DefeatScreen"));

        Click("TitleScreen/StartButton");
        yield return null;
        Assert.AreEqual(1, campaign.EncounterNumber);
        Assert.AreEqual(playerHealth.MaxHealth, playerHealth.CurrentHealth);
        Assert.AreEqual(30, enemyHealth.CurrentHealth);
        yield return AssertBattleAcceptsActions();
    }
}
#endif
