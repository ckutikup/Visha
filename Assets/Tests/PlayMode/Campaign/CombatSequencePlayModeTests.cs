using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Visha.Campaign;
using Visha.Combat.Flow;
using Visha.Enemies;
using Visha.UI;

public class CombatSequencePlayModeTests : CampaignSaveIsolation
{
    private const string ScenePath = "Assets/Scenes/Campaign_Demo.unity";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    private CampaignController campaign;
    private CombatFlowController flow;
    private BattleHUD hud;
    private Health player, enemy;
    private Poison enemyPoison;
    private EnemyPatternRunner enemyPattern;
    private Fade fade;

    [UnitySetUp]
    public IEnumerator LoadScene()
    {
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        campaign = Object.FindAnyObjectByType<CampaignController>();
        flow = Object.FindAnyObjectByType<CombatFlowController>();
        hud = Object.FindAnyObjectByType<BattleHUD>();
        enemyPattern = GameObject.Find("Enemy").GetComponent<EnemyPatternRunner>();
        enemy = enemyPattern.GetComponent<Health>();
        enemyPoison = enemyPattern.GetComponent<Poison>();
        player = GameObject.Find("Player").GetComponent<Health>();
        fade = player.GetComponent<Fade>();
    }


    private bool Busy => (bool)typeof(CombatFlowController).GetField("turnInProgress", Private).GetValue(flow);
    private int ActingEnemyId => (int)typeof(CombatFlowController).GetField("actingEnemyId", Private).GetValue(flow);
    private bool HudSubmitted => (bool)typeof(BattleHUD).GetField("submitted", Private).GetValue(hud);

    private void RequestDirect(BattleAbility ability, int target) =>
        typeof(CombatFlowController).GetMethod("HandleAbilityRequested", Private).Invoke(flow, new object[] { ability, target });

    private IEnumerator WaitForPlayerTurn(float timeout = 8f)
    {
        float deadline = Time.time + timeout;
        while (Time.time < deadline && campaign.State == CampaignState.InBattle
               && (flow.CurrentPhase != BattlePhase.PlayerTurn || Busy))
            yield return null;
    }

    private IEnumerator WaitForState(CampaignState state, float timeout = 8f)
    {
        float deadline = Time.time + timeout;
        while (Time.time < deadline && campaign.State != state) yield return null;
    }

    private IEnumerator Use(BattleAbility ability, int target = -1)
    {
        hud.ChooseAbility(ability);
        if (ability != BattleAbility.Fade)
        {
            if (target >= 0) hud.SelectEnemy(target);
            else hud.SelectTarget();
        }
        Assert.AreNotEqual(BattlePhase.PlayerTurn, flow.CurrentPhase, $"{ability} should have been accepted");
        yield return WaitForPlayerTurn();
    }

    private List<string> RecordEnemyTurnIntents()
    {
        var intents = new List<string>();
        var last = flow.CurrentPhase;
        flow.OnStateUpdated += s =>
        {
            if (s.phase == BattlePhase.EnemyTurn && last != BattlePhase.EnemyTurn)
                intents.Add($"{s.intention} {s.incomingDamage}");
            last = s.phase;
        };
        return intents;
    }

    private IEnumerator StartCampaignAt(int encounterIndex)
    {
        campaign.StartCampaign();
        yield return null;
        for (int i = 0; i < encounterIndex; i++)
        {
            enemy.TakeDamage(999);
            var second = GameObject.Find("SecondEnemy");
            if (second != null) second.GetComponent<Health>().TakeDamage(999);
            yield return null;
            campaign.NextEncounter();
            yield return null;
        }
        Assert.AreEqual(encounterIndex + 1, campaign.EncounterNumber);
        Assert.AreEqual(BattlePhase.PlayerTurn, flow.CurrentPhase);
    }


    [UnityTest]
    public IEnumerator SingleEnemy_RoundRunsInOrder_AndInputDuringTheRoundIsIgnored()
    {
        yield return StartCampaignAt(0);
        var phases = new List<BattlePhase>();
        var enemyTurnIntents = RecordEnemyTurnIntents();
        flow.OnStateUpdated += s =>
        {
            if (phases.Count == 0 || phases[phases.Count - 1] != s.phase) phases.Add(s.phase);
        };
        int playerHits = 0;
        player.OnHealthChanged += (current, max) => playerHits++;

        hud.ChooseAbility(BattleAbility.VenomStrike);
        hud.SelectTarget();
        for (int i = 0; i < 3; i++)
        {
            hud.ChooseAbility(BattleAbility.VenomStrike); hud.SelectTarget();
            hud.ChooseAbility(BattleAbility.Fade);
            hud.RequestAwakening();
            RequestDirect(BattleAbility.VenomStrike, 0);
            RequestDirect(BattleAbility.Fade, -1);
            yield return new WaitForSeconds(.1f);
        }
        float deadline = Time.time + 3f;
        while (flow.CurrentPhase != BattlePhase.EnemyTurn && Time.time < deadline) yield return null;
        hud.ChooseAbility(BattleAbility.VenomStrike); hud.SelectTarget();
        RequestDirect(BattleAbility.VenomStrike, 0);
        yield return WaitForPlayerTurn();

        Assert.AreEqual(2, enemyPoison.PoisonStacks, "exactly one Venom Strike resolved");
        Assert.AreEqual(26, enemy.CurrentHealth, "poison ticked once");
        Assert.AreEqual(46, player.CurrentHealth, "the enemy struck once");
        Assert.AreEqual(1, playerHits);
        Assert.IsFalse(fade.IsActive, "Fade requests during the round were ignored");
        Assert.IsTrue(fade.CanUse);
        Assert.AreEqual("Heavy Blow", enemyPattern.IntentLabel, "pattern advanced once");
        Assert.AreEqual(BattlePhase.PlayerTurn, phases[phases.Count - 1]);
        Assert.AreEqual(1, phases.FindAll(p => p == BattlePhase.PlayerTurn).Count, "one return to the player");
        Assert.Less(phases.IndexOf(BattlePhase.Poison), phases.LastIndexOf(BattlePhase.EnemyTurn), "poison before the attack");
        CollectionAssert.AreEqual(new[] { "Strike 4", "Strike 4" }, enemyTurnIntents, "shown intent is the move that landed");
        Assert.IsFalse(HudSubmitted, "the HUD is unlocked for the next turn");
    }

    [UnityTest]
    public IEnumerator RejectedAbilities_KeepTheTurn_AndReleaseTheHudLock()
    {
        yield return StartCampaignAt(0);

        // Bite without poison, sent while the HUD believes it has submitted.
        typeof(BattleHUD).GetField("submitted", Private).SetValue(hud, true);
        RequestDirect(BattleAbility.SerpentsBite, 0);
        yield return new WaitForSeconds(.5f);
        Assert.AreEqual(BattlePhase.PlayerTurn, flow.CurrentPhase);
        Assert.IsFalse(Busy);
        Assert.IsFalse(HudSubmitted, "RejectAction releases the HUD");
        Assert.AreEqual(30, enemy.CurrentHealth);
        Assert.AreEqual(50, player.CurrentHealth, "no enemy turn was spent");

        // Fade, then Fade again on the next turn.
        yield return Use(BattleAbility.Fade);
        Assert.AreEqual(48, player.CurrentHealth, "Strike halved");
        typeof(BattleHUD).GetField("submitted", Private).SetValue(hud, true);
        RequestDirect(BattleAbility.Fade, -1);
        yield return new WaitForSeconds(.5f);
        Assert.AreEqual(BattlePhase.PlayerTurn, flow.CurrentPhase);
        Assert.IsFalse(HudSubmitted);
        Assert.IsFalse(fade.IsActive);
        Assert.AreEqual(48, player.CurrentHealth);

        yield return Use(BattleAbility.VenomStrike);
        Assert.AreEqual(2, enemyPoison.PoisonStacks, "the next real action is accepted");
    }

    [UnityTest]
    public IEnumerator DefeatDuringTheEnemyAttack_ThenRetry_LeavesNoStaleDamage()
    {
        yield return StartCampaignAt(0);
        player.TakeDamage(46);

        hud.ChooseAbility(BattleAbility.VenomStrike);
        hud.SelectTarget();
        yield return WaitForState(CampaignState.Defeat);
        Assert.AreEqual(CampaignState.Defeat, campaign.State, "Strike 4 is lethal at 4 HP");
        yield return new WaitForSeconds(1f);
        Assert.AreEqual(26, enemy.CurrentHealth, "nothing ran after the death");
        Assert.AreEqual("Strike", enemyPattern.IntentLabel, "pattern did not advance after the death");

        campaign.RetryEncounter();
        yield return null;
        Assert.AreEqual(50, player.CurrentHealth);
        Assert.AreEqual(30, enemy.CurrentHealth);
        Assert.AreEqual(0, enemyPoison.PoisonStacks);
        Assert.AreEqual("Strike", enemyPattern.IntentLabel);
        Assert.AreEqual(BattlePhase.PlayerTurn, flow.CurrentPhase);
        Assert.IsFalse(Busy);

        yield return new WaitForSeconds(1.5f);
        Assert.AreEqual(50, player.CurrentHealth, "no callback from the lost attempt");

        yield return Use(BattleAbility.VenomStrike);
        Assert.AreEqual(46, player.CurrentHealth);
        Assert.AreEqual(26, enemy.CurrentHealth);
    }

    [UnityTest]
    public IEnumerator ReturnToTitleDuringTheEnemyAttack_NewAttemptLeavesNoStaleDamageOrFade()
    {
        yield return StartCampaignAt(0);
        hud.ChooseAbility(BattleAbility.Fade);
        float deadline = Time.time + 3f;
        while (flow.CurrentPhase != BattlePhase.EnemyTurn && Time.time < deadline) yield return null;
        Assert.AreEqual(BattlePhase.EnemyTurn, flow.CurrentPhase);
        Assert.IsTrue(fade.IsActive);
        Assert.AreEqual(50, player.CurrentHealth, "the attack has not landed yet");

        campaign.ReturnToTitle();
        yield return null;
        campaign.StartCampaign();
        yield return null;
        Assert.IsFalse(fade.IsActive, "Fade does not carry into the new attempt");
        Assert.IsTrue(fade.CanUse);
        Assert.IsFalse(Busy);
        Assert.AreEqual(BattlePhase.PlayerTurn, flow.CurrentPhase);

        yield return new WaitForSeconds(1.5f);
        Assert.AreEqual(50, player.CurrentHealth, "the cancelled Strike never lands");
        Assert.AreEqual("Strike", enemyPattern.IntentLabel);

        yield return Use(BattleAbility.VenomStrike);
        Assert.AreEqual(46, player.CurrentHealth, "the new attempt plays normally");
    }


    [UnityTest]
    public IEnumerator TwoEnemies_EachActsOnce_PoisonBeforeItsOwnAction_ShownIntentMatches()
    {
        yield return StartCampaignAt(3);
        var second = GameObject.Find("SecondEnemy").GetComponent<Health>();
        var secondPattern = second.GetComponent<EnemyPatternRunner>();
        Assert.AreEqual("Strike", enemyPattern.IntentLabel);
        Assert.AreEqual("Shield Bash", secondPattern.IntentLabel);

        var log = new List<string>();
        var acting = new List<string>();
        int lastPlayer = player.CurrentHealth, lastFirst = enemy.CurrentHealth, lastSecond = second.CurrentHealth;
        player.OnHealthChanged += (c, m) => { log.Add($"P-{lastPlayer - c}@{ActingEnemyId}"); lastPlayer = c; };
        enemy.OnHealthChanged += (c, m) => { log.Add($"E0-{lastFirst - c}"); lastFirst = c; };
        second.OnHealthChanged += (c, m) => { log.Add($"E1-{lastSecond - c}"); lastSecond = c; };
        flow.OnStateUpdated += s =>
        {
            if (s.phase != BattlePhase.EnemyTurn || s.actingEnemyId < 0) return;
            var shown = s.enemies[s.actingEnemyId];
            string entry = $"{s.actingEnemyId}:{shown.intention} {shown.incomingDamage}";
            if (acting.Count == 0 || acting[acting.Count - 1] != entry) acting.Add(entry);
        };

        yield return Use(BattleAbility.VenomStrike, 1);

        CollectionAssert.AreEqual(new[] { "P-4@0", "E1-4", "P-8@1" }, log,
            "first enemy strikes; second enemy's poison ticks, then it strikes");
        CollectionAssert.AreEqual(new[] { "0:Strike 4", "1:Shield Bash 8" }, acting,
            "the acting enemy and its shown intent match what landed");
        Assert.AreEqual(38, player.CurrentHealth);
        Assert.AreEqual("Heavy Blow", enemyPattern.IntentLabel, "each pattern advanced once");
        Assert.AreEqual("Recover", secondPattern.IntentLabel);
        Assert.AreEqual(-1, ActingEnemyId);
    }

    [UnityTest]
    public IEnumerator TwoEnemies_FadeHalvesBothHits_AndExpiresOnceAfterThePhase()
    {
        yield return StartCampaignAt(3);
        yield return Use(BattleAbility.Fade);

        Assert.AreEqual(50 - 2 - 4, player.CurrentHealth, "Strike 4 and Shield Bash 8 both halved");
        Assert.IsFalse(fade.IsActive);
        Assert.IsFalse(fade.CanUse, "Fade still can't be used twice in a row");
    }

    [UnityTest]
    public IEnumerator TwoEnemies_PoisonKillSkipsThatEnemy_VictoryWaitsForBoth()
    {
        yield return StartCampaignAt(3);
        var second = GameObject.Find("SecondEnemy").GetComponent<Health>();
        var secondPattern = second.GetComponent<EnemyPatternRunner>();
        second.TakeDamage(22);
        second.GetComponent<Poison>().AddStacks(2);

        yield return Use(BattleAbility.Fade);

        Assert.IsTrue(second.IsDead, "poison killed the second enemy");
        Assert.AreEqual(48, player.CurrentHealth, "only the first enemy's halved Strike landed");
        Assert.AreEqual("Shield Bash", secondPattern.IntentLabel, "the dead enemy did not act");
        Assert.AreEqual(CampaignState.InBattle, campaign.State, "one enemy still stands");
        Assert.AreEqual(BattlePhase.PlayerTurn, flow.CurrentPhase);

        yield return Use(BattleAbility.VenomStrike, 0);
        Assert.AreEqual(14, enemy.CurrentHealth);
        Assert.AreEqual(39, player.CurrentHealth, "the survivor alone acts: Heavy Blow 9");
        Assert.AreEqual(CampaignState.InBattle, campaign.State);

        enemy.TakeDamage(999);
        yield return null;
        Assert.AreEqual(CampaignState.Victory, campaign.State);
    }


    [UnityTest]
    public IEnumerator Boss_PoisonTickStartsPhaseTwoOnce_RoarActs_ThenCleaveIsShown()
    {
        yield return StartCampaignAt(4);
        int phaseChanges = 0, lastBossPhase = 1;
        var enemyTurnIntents = RecordEnemyTurnIntents();
        flow.OnStateUpdated += s =>
        {
            if (s.bossPhase > lastBossPhase) phaseChanges++;
            lastBossPhase = s.bossPhase;
        };

        yield return Use(BattleAbility.VenomStrike);
        Assert.AreEqual(72, enemy.CurrentHealth);
        Assert.AreEqual("Cursed Claw", enemyPattern.IntentLabel);
        enemy.TakeDamage(32);

        enemyTurnIntents.Clear();
        yield return Use(BattleAbility.Fade);

        Assert.AreEqual(36, enemy.CurrentHealth);
        Assert.AreEqual(2, enemyPattern.BossPhase);
        Assert.AreEqual(50, player.CurrentHealth, "Roar replaced Cursed Claw; nothing landed");
        CollectionAssert.AreEqual(new[] { "Cursed Claw 5", "Roar 0" }, enemyTurnIntents,
            "Cursed Claw (halved by Fade) until the poison step, then Roar as the boss acts");
        Assert.AreEqual("Cleave", enemyPattern.IntentLabel, "next move after Roar");
        Assert.AreEqual(2, enemyPoison.PoisonStacks, "poison carries into phase 2");
        Assert.AreEqual(1, phaseChanges);

        yield return Use(BattleAbility.VenomStrike);
        Assert.AreEqual(28, enemy.CurrentHealth);
        Assert.AreEqual(38, player.CurrentHealth);
        Assert.AreEqual("Hex", enemyPattern.IntentLabel);
        Assert.AreEqual(1, phaseChanges, "the phase changes only once");
    }

    [UnityTest]
    public IEnumerator Boss_BiteStartsPhaseTwoBeforeTheBossActs()
    {
        yield return StartCampaignAt(4);
        var enemyTurnIntents = RecordEnemyTurnIntents();

        yield return Use(BattleAbility.VenomStrike);
        enemyPoison.AddStacks(3);
        enemy.TakeDamage(14);
        enemyTurnIntents.Clear();

        yield return Use(BattleAbility.SerpentsBite);

        Assert.AreEqual(38, enemy.CurrentHealth);
        Assert.AreEqual(2, enemyPattern.BossPhase);
        Assert.AreEqual(50, player.CurrentHealth);
        CollectionAssert.AreEqual(new[] { "Roar 0" }, enemyTurnIntents, "Roar is shown for the whole enemy turn");
        Assert.AreEqual("Cleave", enemyPattern.IntentLabel);
    }

    [UnityTest]
    public IEnumerator Boss_DefeatInPhaseTwo_RetryRestoresPhaseOneAndBlessing()
    {
        yield return StartCampaignAt(4);
        var blessing = player.GetComponent<TempleBlessing>();
        yield return Use(BattleAbility.VenomStrike);
        enemy.TakeDamage(34);
        yield return Use(BattleAbility.VenomStrike);
        Assert.AreEqual(2, enemyPattern.BossPhase);

        player.TakeDamage(999);
        Assert.IsTrue(blessing.IsSpent);
        player.TakeDamage(999);
        yield return WaitForState(CampaignState.Defeat);
        Assert.AreEqual(CampaignState.Defeat, campaign.State);

        campaign.RetryEncounter();
        yield return null;
        var view = (BattleViewState)typeof(CombatFlowController).GetField("viewState", Private).GetValue(flow);
        Assert.AreEqual(76, enemy.CurrentHealth);
        Assert.AreEqual(0, enemyPoison.PoisonStacks);
        Assert.AreEqual(1, enemyPattern.BossPhase);
        Assert.AreEqual(1, view.bossPhase);
        Assert.AreEqual("Recover", view.intention);
        Assert.AreEqual(BlessingStatus.Ready, view.blessing);
        Assert.AreEqual(50, player.CurrentHealth);
        Assert.AreEqual(BattlePhase.PlayerTurn, flow.CurrentPhase);

        yield return new WaitForSeconds(1.5f);
        Assert.AreEqual(50, player.CurrentHealth, "no callback from the lost attempt");
        yield return Use(BattleAbility.VenomStrike);
        Assert.AreEqual(72, enemy.CurrentHealth);
        Assert.AreEqual("Cursed Claw", enemyPattern.IntentLabel, "the phase 1 cycle runs again");
    }
}
