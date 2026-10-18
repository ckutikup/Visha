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
    private BattleScreenView screens;
    private SpriteRenderer enemySpriteRenderer;

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
        enemySpriteRenderer = GameObject.Find("EnemyPlaceholder").GetComponent<SpriteRenderer>();
        playerHealth = GameObject.Find("Player").GetComponent<Health>();
        screens = Object.FindAnyObjectByType<BattleScreenView>();
        Assert.IsNull(Object.FindAnyObjectByType<BattleScreenPresenter>(),
            "only the campaign should drive the battle screens");
    }

    private bool ScreenVisible(BattleScreen screen) => screens.Screen == screen;

    private void Click(string path)
    {
        Button button = screens.transform.Find(path).GetComponent<Button>();
        Assert.IsTrue(button.gameObject.activeInHierarchy, $"{path} should be visible");
        Assert.IsTrue(button.interactable, $"{path} should be enabled");
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
        Assert.IsTrue(ScreenVisible(BattleScreen.Title));

        Click("TitlePanel/StartButton");
        yield return null;
        Assert.IsFalse(ScreenVisible(BattleScreen.Title));
        Assert.AreEqual(1, campaign.EncounterNumber);
        Assert.AreEqual("Temple Guardian", enemyPattern.EnemyName);
        Assert.AreEqual(campaign.CurrentEncounter.EnemySprite, enemySpriteRenderer.sprite);
        yield return AssertBattleAcceptsActions();

        // Encounter 1: win, then wait for Next
        enemyHealth.TakeDamage(999);
        yield return null;
        Assert.AreEqual(CampaignState.Victory, campaign.State);
        Assert.IsTrue(ScreenVisible(BattleScreen.Victory));
        Assert.AreEqual(1, campaign.EncounterNumber);

        Click("VictoryPanel/Card/NextButton");
        yield return null;
        Assert.IsFalse(ScreenVisible(BattleScreen.Victory));
        Assert.AreEqual(2, campaign.EncounterNumber);
        Assert.AreEqual("Guardian Warden", enemyPattern.EnemyName);
        Assert.AreEqual(campaign.CurrentEncounter.EnemySprite, enemySpriteRenderer.sprite);
        Assert.AreEqual("ENCOUNTER 2 / 3", screens.transform.Find("Encounter").GetComponent<Text>().text);
        Assert.AreEqual(44, enemyHealth.CurrentHealth);
        Assert.AreEqual(0, enemyPoison.PoisonStacks);
        yield return AssertBattleAcceptsActions();

        // Encounter 2: lose, then Retry the same encounter
        playerHealth.TakeDamage(999);
        yield return null;
        Assert.AreEqual(CampaignState.Defeat, campaign.State);
        Assert.IsTrue(ScreenVisible(BattleScreen.Defeat));

        Click("DefeatPanel/Card/RetryButton");
        yield return null;
        Assert.IsFalse(ScreenVisible(BattleScreen.Defeat));
        Assert.AreEqual(2, campaign.EncounterNumber);
        Assert.AreEqual(playerHealth.MaxHealth, playerHealth.CurrentHealth);
        Assert.AreEqual(44, enemyHealth.CurrentHealth);
        Assert.AreEqual(0, enemyPoison.PoisonStacks);
        Assert.AreEqual(TempleGuardianPattern.Strike.Label, enemyPattern.IntentLabel);
        yield return AssertBattleAcceptsActions();

        // Encounters 2 and 3: win both, the last one ends the demo
        enemyHealth.TakeDamage(999);
        yield return null;
        Click("VictoryPanel/Card/NextButton");
        yield return null;
        Assert.AreEqual(3, campaign.EncounterNumber);
        Assert.AreEqual("Vault Sentinel", enemyPattern.EnemyName);
        Assert.AreEqual(campaign.CurrentEncounter.EnemySprite, enemySpriteRenderer.sprite);
        Assert.AreEqual(46, enemyHealth.CurrentHealth);
        yield return AssertBattleAcceptsActions();

        enemyHealth.TakeDamage(999);
        yield return null;
        Assert.AreEqual(CampaignState.DemoComplete, campaign.State);
        Assert.IsTrue(ScreenVisible(BattleScreen.Complete));
        Assert.IsFalse(ScreenVisible(BattleScreen.Victory));

        // Restart: back to encounter 1 with a fresh battle
        Click("CompletePanel/RestartButton");
        yield return null;
        Assert.IsFalse(ScreenVisible(BattleScreen.Complete));
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
        Click("TitlePanel/StartButton");
        yield return null;
        enemyHealth.TakeDamage(999);
        yield return null;
        Click("VictoryPanel/Card/NextButton");
        yield return null;
        playerHealth.TakeDamage(999);
        yield return null;

        Click("DefeatPanel/Card/ReturnButton");
        yield return null;
        Assert.IsTrue(ScreenVisible(BattleScreen.Title));
        Assert.IsFalse(ScreenVisible(BattleScreen.Defeat));

        Click("TitlePanel/StartButton");
        yield return null;
        Assert.AreEqual(1, campaign.EncounterNumber);
        Assert.AreEqual(playerHealth.MaxHealth, playerHealth.CurrentHealth);
        Assert.AreEqual(30, enemyHealth.CurrentHealth);
        yield return AssertBattleAcceptsActions();
    }
}
#endif
