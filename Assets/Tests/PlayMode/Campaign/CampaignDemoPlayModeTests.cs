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
        var map = campaign.GetComponent<TempleMapView>();
        if (map != null && map.IsOpen)
        {
            if (path.Contains("RestartButton")) map.SelectNode(0);
            map.EnterSelected();
            var arrival = campaign.GetComponent<VaultArrivalView>();
            if (arrival != null && arrival.IsOpen) arrival.Continue();
        }
    }

    /// <summary>Uses Venom Strike through the HUD and checks the turn loop accepted it.</summary>
    private IEnumerator AssertBattleAcceptsActions()
    {
        int stacksBefore = enemyPoison.PoisonStacks;
        hud.ChooseAbility(BattleAbility.VenomStrike);
        hud.SelectTarget();
        yield return new WaitForSeconds(.5f);
        Assert.Greater(enemyPoison.PoisonStacks, stacksBefore, "Venom Strike applies when its cast lands");
        yield return new WaitForSeconds(1.5f);
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
        Assert.AreEqual("COURTYARD  /  2 OF 3", screens.transform.Find("Encounter").GetComponent<Text>().text);
        Assert.AreEqual(44, enemyHealth.CurrentHealth);
        Assert.AreEqual(1f, hud.transform.Find("EnemyStatus/HealthTrack/HealthFill").GetComponent<RectTransform>().anchorMax.x);
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
        Assert.AreEqual(1f, hud.transform.Find("EnemyStatus/HealthTrack/HealthFill").GetComponent<RectTransform>().anchorMax.x);
        Assert.AreEqual(0, enemyPoison.PoisonStacks);
        Assert.AreEqual(TempleGuardianPattern.HeavyBlow.Label, enemyPattern.IntentLabel);
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
    [UnityTest]
    public IEnumerator KillingBiteKeepsVictoryAudio_AndNextEncounterClearsIt()
    {
        Click("TitlePanel/StartButton");
        yield return null;
        enemyHealth.SetMaxHealth(4);
        enemyPoison.AddStacks(1);
        hud.ChooseAbility(BattleAbility.SerpentsBite);
        hud.SelectTarget();
        yield return new WaitForSeconds(.5f);
        Assert.AreEqual(CampaignState.Victory, campaign.State);
        var source = GameObject.Find("BattlePresentationAudio").GetComponent<AudioSource>();
        Assert.AreEqual("Victory", source.clip.name);

        Click("VictoryPanel/Card/NextButton");
        yield return null;
        Assert.IsNull(source.clip, "An encounter reset clears the previous result audio.");
        Assert.AreEqual(Color.white, enemySpriteRenderer.color, "The new enemy must not inherit the previous hit flash.");
        Assert.IsFalse(enemySpriteRenderer.transform.Find("BiteFlash").GetComponent<SpriteRenderer>().enabled);
        Assert.AreEqual(CampaignState.InBattle, campaign.State);
    }

    [UnityTest]
    public IEnumerator AttackAnimationLocksInput_AndRetryRestoresHomePositions()
    {
        Click("TitlePanel/StartButton"); yield return null;
        var actor = GameObject.Find("PlayerPlaceholder").transform;
        Vector3 home = actor.position;
        hud.ChooseAbility(BattleAbility.VenomStrike); hud.SelectTarget();
        Assert.AreEqual(0, enemyPoison.PoisonStacks, "cast does not hit instantly");
        hud.ChooseAbility(BattleAbility.VenomStrike); hud.SelectTarget();
        yield return new WaitForSeconds(.18f);
        Assert.Greater(actor.position.x, home.x, "the player steps into the cast");
        yield return new WaitForSeconds(2f);
        Assert.AreEqual(2, enemyPoison.PoisonStacks, "input stays locked during the animation");
        playerHealth.TakeDamage(999); yield return null;
        Click("DefeatPanel/Card/RetryButton"); yield return null;
        Assert.Less(Vector3.Distance(home, actor.position), .02f);
        Assert.AreEqual(0, enemyPoison.PoisonStacks);
    }

    [UnityTest]
    public IEnumerator VaultBackgroundChanges_AndReturnToTitleRestoresCourtyard()
    {
        var backdrop = GameObject.Find("CampaignBackground").GetComponent<SpriteRenderer>();
        var courtyard = backdrop.sprite;
        Click("TitlePanel/StartButton"); yield return null;
        enemyHealth.TakeDamage(999); yield return null;
        Click("VictoryPanel/Card/NextButton"); yield return null;
        enemyHealth.TakeDamage(999); yield return null;
        Click("VictoryPanel/Card/NextButton"); yield return null;
        Assert.AreNotEqual(courtyard, backdrop.sprite);
        Assert.AreEqual("TempleVault", backdrop.sprite.name);
        playerHealth.TakeDamage(999); yield return null;
        Click("DefeatPanel/Card/ReturnButton"); yield return null;
        Assert.AreEqual(courtyard, backdrop.sprite);
    }

    [UnityTest]
    public IEnumerator PoisonKillPreventsTheAnimatedEnemyAttack()
    {
        Click("TitlePanel/StartButton"); yield return null;
        enemyHealth.SetMaxHealth(4); playerHealth.TakeDamage(48);
        hud.ChooseAbility(BattleAbility.VenomStrike); hud.SelectTarget();
        yield return new WaitForSeconds(1.5f);
        Assert.AreEqual(CampaignState.Victory, campaign.State);
        Assert.AreEqual(2, playerHealth.CurrentHealth);
        Assert.AreEqual("Strike", enemyPattern.IntentLabel);
    }

    [UnityTest]
    public IEnumerator ReturnToTitleDuringCastCancelsThePendingImpact()
    {
        Click("TitlePanel/StartButton"); yield return null;
        var actor = GameObject.Find("PlayerPlaceholder").transform;
        Vector3 home = actor.position;
        hud.ChooseAbility(BattleAbility.VenomStrike); hud.SelectTarget();
        yield return new WaitForSeconds(.18f);
        campaign.ReturnToTitle();
        yield return new WaitForSeconds(.8f);
        Assert.AreEqual(0, enemyPoison.PoisonStacks);
        Assert.AreEqual(CampaignState.Title, campaign.State);
        Assert.Less(Vector3.Distance(home, actor.position), .02f);
        Assert.IsNull(GameObject.Find("VenomProjectile"));
    }

    [UnityTest]
    public IEnumerator EnemyClosesTheGapBeforeDealingDamage()
    {
        Click("TitlePanel/StartButton"); yield return null;
        var enemy = GameObject.Find("EnemyPlaceholder").transform;
        float homeX = enemy.position.x;
        hud.ChooseAbility(BattleAbility.VenomStrike); hud.SelectTarget();
        float deadline = Time.time + 3f;
        while (enemy.position.x > homeX - 1f && Time.time < deadline) yield return null;
        Assert.Less(enemy.position.x, homeX - 1f, "enemy moves into its attack");
        Assert.AreEqual(50, playerHealth.CurrentHealth, "damage waits for contact");
        yield return new WaitForSeconds(.8f);
        Assert.AreEqual(46, playerHealth.CurrentHealth);
    }

    [UnityTest]
    public IEnumerator VenomApproachesBeforeApplyingPoison()
    {
        Click("TitlePanel/StartButton"); yield return null;
        var actor = GameObject.Find("PlayerPlaceholder").transform;
        float home = actor.position.x;
        hud.ChooseAbility(BattleAbility.VenomStrike); hud.SelectTarget();
        yield return new WaitForSeconds(.17f);
        Assert.Greater(actor.position.x, home + .5f);
        Assert.AreEqual(0, enemyPoison.PoisonStacks);
        yield return new WaitForSeconds(.2f);
        Assert.AreEqual(2, enemyPoison.PoisonStacks);
    }

    [UnityTest]
    public IEnumerator FadeShowsReducedIntentThenExpiresAfterTheAttack()
    {
        Click("TitlePanel/StartButton"); yield return null;
        hud.ChooseAbility(BattleAbility.Fade);
        yield return new WaitForSeconds(.3f);
        var fade = playerHealth.GetComponent<Fade>();
        Assert.IsTrue(fade.IsActive);
        Assert.Less(GameObject.Find("PlayerPlaceholder").GetComponent<SpriteRenderer>().color.a, .8f);
        yield return new WaitForSeconds(1.4f);
        Assert.AreEqual(48, playerHealth.CurrentHealth);
        Assert.IsFalse(fade.IsActive);
        Assert.IsFalse(fade.CanUse);
        Assert.AreEqual("Heavy Blow", enemyPattern.IntentLabel);
    }

    [UnityTest]
    public IEnumerator MutingCanBeReversedWithoutRestartingTheGame()
    {
        var sound = Object.FindAnyObjectByType<DemoSoundControl>();
        float initial = AudioListener.volume;
        sound.ToggleMute();
        Assert.AreEqual(0, AudioListener.volume);
        sound.ToggleMute();
        Assert.AreEqual(initial, AudioListener.volume);
        yield return null;
    }

    [UnityTest]
    public IEnumerator EnemyImpactShowsDamageAndRecoilThenClearsOnRestart()
    {
        Click("TitlePanel/StartButton"); yield return null;
        var actor = GameObject.Find("PlayerPlaceholder").transform;
        float home = actor.position.x;
        hud.ChooseAbility(BattleAbility.VenomStrike); hud.SelectTarget();
        float deadline = Time.time + 3f;
        while (playerHealth.CurrentHealth == 50 && Time.time < deadline) yield return null;
        yield return null;
        Assert.AreEqual(46, playerHealth.CurrentHealth);
        var popup = GameObject.Find("PlayerDamage").GetComponent<Text>();
        Assert.AreEqual("−4 HP", popup.text);
        Assert.Less(actor.position.x, home - .08f);
        campaign.RestartCampaign(); yield return null;
        Assert.IsFalse(popup.gameObject.activeSelf);
        Assert.AreEqual(50, playerHealth.CurrentHealth);
        Assert.Less(Mathf.Abs(actor.position.x - home), .02f);
    }

    [UnityTest]
    public IEnumerator GuardedImpactDisplaysActualReducedDamage()
    {
        Click("TitlePanel/StartButton"); yield return null;
        hud.ChooseAbility(BattleAbility.Fade);
        float deadline = Time.time + 3f;
        while (playerHealth.CurrentHealth == 50 && Time.time < deadline) yield return null;
        Assert.AreEqual(48, playerHealth.CurrentHealth);
        Assert.AreEqual("−2  GUARDED", GameObject.Find("PlayerDamage").GetComponent<Text>().text);
        yield return new WaitForSeconds(1f);
        Assert.IsNull(GameObject.Find("PlayerDamage"));
    }

    [UnityTest]
    public IEnumerator EnemyMovesHaveDistinctEffectsAndRestartClearsThem()
    {
        Click("TitlePanel/StartButton"); yield return null;
        string[] names = { "StrikeImpact", "HeavyImpact", "BashImpact" };
        for (int i = 0; i < names.Length; i++)
        {
            campaign.RestartCampaign(); yield return null;
            enemyPattern.Configure(i == 2 ? EnemyPatternRunner.PatternKind.VaultSentinel : EnemyPatternRunner.PatternKind.TempleGuardian,
                null, i == 2 ? 2 : i);
            hud.ChooseAbility(BattleAbility.VenomStrike); hud.SelectTarget();
            float deadline = Time.time + 3f;
            while (playerHealth.CurrentHealth == 50 && Time.time < deadline) yield return null;
            Assert.IsNotNull(GameObject.Find(names[i]), names[i]);
            campaign.RestartCampaign(); yield return null;
            foreach (string name in names) Assert.IsNull(GameObject.Find(name));
        }
    }

    [UnityTest]
    public IEnumerator FadeAfterimageDoesNotSurviveReturningToTitle()
    {
        Click("TitlePanel/StartButton"); yield return null;
        hud.ChooseAbility(BattleAbility.Fade);
        yield return null;
        Assert.IsNotNull(GameObject.Find("FadeAfterimage"));
        campaign.ReturnToTitle(); yield return null;
        Assert.IsNull(GameObject.Find("FadeAfterimage"));
    }

    [UnityTest]
    public IEnumerator MapLocksFutureNodesAndRetainsCompletedNodesOnReplay()
    {
        var map = campaign.GetComponent<TempleMapView>();
        screens.RequestStart(); yield return null;
        Assert.IsTrue(map.IsOpen);
        Assert.AreEqual(CampaignState.Title, campaign.State);
        map.SelectNode(1); map.EnterSelected();
        Assert.AreEqual(CampaignState.Title, campaign.State);
        map.SelectNode(4); map.EnterSelected();
        Assert.AreEqual(CampaignState.Title, campaign.State);
        map.SelectNode(0); map.EnterSelected(); yield return null;
        Assert.IsFalse(map.IsOpen);
        enemyHealth.TakeDamage(999); yield return null;
        Assert.AreEqual(0, campaign.HighestCompletedEncounter);
        Assert.IsTrue(campaign.CanEnterEncounter(1));
        map.Open(); map.SelectNode(0); map.EnterSelected(); yield return null;
        playerHealth.TakeDamage(999); yield return null;
        Assert.AreEqual(0, campaign.HighestCompletedEncounter);
        Assert.IsTrue(campaign.CanEnterEncounter(1));
        Assert.IsFalse(campaign.CanEnterEncounter(2));
        campaign.ReturnToTitle(); yield return null;
        Assert.AreEqual(-1, campaign.HighestCompletedEncounter);
    }

    [UnityTest]
    public IEnumerator GuardedHeavyGrantsOneCappedBoonAndRetryClearsIt()
    {
        Click("TitlePanel/StartButton"); yield return null;
        var routine = Object.FindAnyObjectByType<Visha.Combat.Flow.RoutineBreak>();
        enemyPattern.Configure(campaign.CurrentEncounter.Pattern, "Guardian", 1);
        hud.ChooseAbility(BattleAbility.Fade);
        yield return new WaitForSeconds(2f);
        Assert.IsTrue(routine.HasBoon);
        Assert.AreEqual(46, playerHealth.CurrentHealth);
        enemyPoison.AddStacks(4);
        hud.ChooseAbility(BattleAbility.VenomStrike); hud.SelectTarget();
        yield return new WaitForSeconds(.45f);
        Assert.AreEqual(5, enemyPoison.PoisonStacks);
        Assert.IsFalse(routine.HasBoon);
        yield return new WaitForSeconds(1.5f);
        enemyPattern.Configure(campaign.CurrentEncounter.Pattern, "Guardian", 1);
        hud.ChooseAbility(BattleAbility.Fade);
        yield return new WaitForSeconds(2f);
        Assert.IsFalse(routine.HasBoon, "the same encounter cannot award another fragment");
        campaign.RestartCampaign(); yield return null;
        Assert.IsTrue(routine.IsOpportunity("Heavy Blow"));
        Assert.IsFalse(routine.HasBoon);
        enemyPattern.Configure(campaign.CurrentEncounter.Pattern, "Guardian", 1);
        hud.ChooseAbility(BattleAbility.Fade);
        yield return new WaitForSeconds(2f);
        Assert.IsTrue(routine.HasBoon);
        campaign.ReturnToTitle(); yield return null;
        Assert.IsFalse(routine.HasBoon);
        Assert.IsNull(GameObject.Find("CurseFragment"));
    }

    [UnityTest]
    public IEnumerator RoutineCanBeDisabledWithoutChangingBaseCombat()
    {
        Click("TitlePanel/StartButton"); yield return null;
        var routine = Object.FindAnyObjectByType<Visha.Combat.Flow.RoutineBreak>();
        routine.enabled = false;
        enemyPattern.Configure(campaign.CurrentEncounter.Pattern, "Guardian", 1);
        hud.ChooseAbility(BattleAbility.Fade);
        yield return new WaitForSeconds(2f);
        Assert.IsFalse(routine.HasBoon);
        hud.ChooseAbility(BattleAbility.VenomStrike); hud.SelectTarget();
        yield return new WaitForSeconds(.45f);
        Assert.AreEqual(2, enemyPoison.PoisonStacks);
    }

    [UnityTest]
    public IEnumerator BoonAddsThreeStacksAndDoesNotSurviveNextEncounter()
    {
        Click("TitlePanel/StartButton"); yield return null;
        var routine = Object.FindAnyObjectByType<Visha.Combat.Flow.RoutineBreak>();
        enemyPattern.Configure(campaign.CurrentEncounter.Pattern, "Guardian", 1);
        hud.ChooseAbility(BattleAbility.Fade);
        yield return new WaitForSeconds(2f);
        hud.ChooseAbility(BattleAbility.VenomStrike); hud.SelectTarget();
        yield return new WaitForSeconds(.45f);
        Assert.AreEqual(3, enemyPoison.PoisonStacks);
        Assert.IsFalse(routine.HasBoon);
        campaign.RestartCampaign(); yield return null;
        enemyPattern.Configure(campaign.CurrentEncounter.Pattern, "Guardian", 1);
        hud.ChooseAbility(BattleAbility.Fade);
        yield return new WaitForSeconds(2f);
        Assert.IsTrue(routine.HasBoon);
        enemyHealth.TakeDamage(999); yield return null;
        Click("VictoryPanel/Card/NextButton"); yield return null;
        Assert.AreEqual(0, enemyPoison.PoisonStacks);
        Assert.IsFalse(routine.HasBoon);
        Assert.IsTrue(routine.IsOpportunity("Heavy Blow"));
    }

    [UnityTest]
    public IEnumerator InvalidTargetDoesNotSpendAnAction()
    {
        Click("TitlePanel/StartButton"); yield return null;
        var flow = Object.FindAnyObjectByType<Visha.Combat.Flow.CombatFlowController>();
        var request = typeof(Visha.Combat.Flow.CombatFlowController).GetMethod("HandleAbilityRequested",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        request.Invoke(flow, new object[] { BattleAbility.VenomStrike, 1 });
        yield return new WaitForSeconds(.5f);
        Assert.AreEqual(0, enemyPoison.PoisonStacks);
        Assert.AreEqual(BattlePhase.PlayerTurn, flow.CurrentPhase);
        hud.ChooseAbility(BattleAbility.VenomStrike); hud.SelectTarget();
        yield return new WaitForSeconds(.5f);
        Assert.AreEqual(2, enemyPoison.PoisonStacks);
    }

    [UnityTest]
    public IEnumerator SkillArtworkDoesNotBlockPointerClicks()
    {
        Click("TitlePanel/StartButton"); yield return null;
        Canvas.ForceUpdateCanvases();
        foreach (var button in hud.GetComponentsInChildren<Button>())
        {
            if (button.name != "VenomStrike" && button.name != "Fade") continue;
            var point = RectTransformUtility.WorldToScreenPoint(null, button.transform.position);
            var input = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current) { position = point };
            var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            UnityEngine.EventSystems.EventSystem.current.RaycastAll(input, hits);
            Assert.IsNotEmpty(hits);
            Assert.IsTrue(hits[0].gameObject.transform.IsChildOf(button.transform), button.name + " is obscured");
        }
    }

}
#endif
