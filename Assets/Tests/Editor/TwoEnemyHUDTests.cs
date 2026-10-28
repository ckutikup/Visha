using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Visha.UI;

public class TwoEnemyHUDTests
{
    private GameObject root;
    private BattleHUD hud;
    private int calls, target;
    private BattleViewState state;
    [SetUp] public void SetUp()
    {
        root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BattleHUD.prefab"));
        hud = root.GetComponent<BattleHUD>();
        hud.AbilityRequested += (ability, id) => { calls++; target = id; };
        calls = 0; target = -99;
        state = new BattleViewState { phase = BattlePhase.PlayerTurn, enemies = new[] {
            new EnemyViewState { targetId = 10, name = "Guardian", health = 30, maxHealth = 30 },
            new EnemyViewState { targetId = 20, name = "Sentinel", health = 40, maxHealth = 46, poison = 5 }
        }};
        hud.Render(state);
    }
    [TearDown] public void TearDown() => Object.DestroyImmediate(root);
    [Test] public void RequestsStableIdAndLocksRepeatedClicks()
    {
        hud.ChooseAbility(BattleAbility.VenomStrike); hud.SelectEnemy(20); hud.SelectEnemy(10);
        hud.Render(state); hud.RequestFade();
        Assert.AreEqual(1, calls); Assert.AreEqual(20, target);
    }
    [Test] public void BiteRejectsUnpoisonedEnemyButAllowsOtherTarget()
    {
        hud.ChooseAbility(BattleAbility.SerpentsBite); hud.SelectEnemy(10);
        Assert.AreEqual(0, calls);
        hud.SelectEnemy(20); Assert.AreEqual(1, calls); Assert.AreEqual(20, target);
    }
    [Test] public void DeathDoesNotPreventSurvivorSelection()
    {
        state.enemies[0].health = 0; hud.Render(state);
        hud.ChooseAbility(BattleAbility.VenomStrike); hud.SelectEnemy(10);
        Assert.AreEqual(0, calls);
        hud.SelectEnemy(20); Assert.AreEqual(20, target);
    }
    [Test] public void KeyboardCyclesAndConfirmsEligibleBiteTarget()
    {
        hud.ChooseAbility(BattleAbility.SerpentsBite); hud.CycleEnemyTarget();
        Assert.AreEqual(0, calls); Assert.AreEqual(20, hud.SelectedEnemyId);
        hud.SelectTarget(); Assert.AreEqual(20, target); Assert.AreEqual(1, calls);
    }
    [Test] public void SnapshotsAreCopiedAndReorderingPreservesIdentity()
    {
        state.enemies[1].poison = 0;
        hud.ChooseAbility(BattleAbility.SerpentsBite); hud.SelectEnemy(20);
        Assert.AreEqual(1, calls);
        state.phase = BattlePhase.EnemyTurn; hud.Render(state);
        System.Array.Reverse(state.enemies); hud.Render(state);
        Assert.AreEqual(20, hud.SelectedEnemyId);
    }
    [Test] public void OverlayAndEnemyPhaseBlockAllTargets()
    {
        hud.SetInputEnabled(false); hud.ChooseAbility(BattleAbility.VenomStrike); hud.SelectEnemy(20);
        hud.SetInputEnabled(true); state.phase = BattlePhase.EnemyTurn; hud.Render(state);
        hud.ChooseAbility(BattleAbility.VenomStrike); hud.SelectEnemy(20); hud.RequestFade();
        Assert.AreEqual(0, calls);
    }
    [Test] public void EmptyRosterAndAllDeadBlockFade()
    {
        foreach (var e in state.enemies) e.health = 0;
        hud.Render(state); hud.RequestFade();
        state.enemies = new EnemyViewState[0]; hud.Render(state); hud.RequestFade();
        Assert.AreEqual(0, calls);
    }
    [Test] public void DuplicateIdsAreRejectedBeforeUpdatingTheView()
    {
        state.enemies[1].targetId = 10;
        Assert.Throws<System.ArgumentException>(() => hud.Render(state));
        Assert.AreEqual(10, hud.SelectedEnemyId);
    }
    [Test] public void FadeStillUsesSelfTarget()
    {
        hud.RequestFade(); Assert.AreEqual(-1, target); Assert.AreEqual(1, calls);
    }
    [Test] public void UpdatedPoisonPreventsStaleBiteRequest()
    {
        hud.ChooseAbility(BattleAbility.SerpentsBite);
        state.enemies[1].poison = 0; hud.Render(state); hud.SelectEnemy(20);
        Assert.AreEqual(0, calls);
    }
    [Test] public void ActingEnemyDoesNotOverwritePlayerSelection()
    {
        hud.SelectEnemy(20);
        state.phase = BattlePhase.EnemyTurn; state.actingEnemyId = 10; hud.Render(state);
        Assert.AreEqual(10, hud.DisplayedEnemyId); Assert.AreEqual(20, hud.SelectedEnemyId);
        hud.SelectEnemy(10); Assert.AreEqual(20, hud.SelectedEnemyId);
        state.phase = BattlePhase.PlayerTurn; state.actingEnemyId = -1; hud.Render(state);
        Assert.AreEqual(20, hud.DisplayedEnemyId);
    }
    [Test] public void BiteFocusesEligibleEnemyWithoutSubmitting()
    {
        hud.ChooseAbility(BattleAbility.SerpentsBite);
        Assert.AreEqual(20, hud.SelectedEnemyId); Assert.AreEqual(0, calls);
    }
    [Test] public void DefeatedOrMissingAttackerDoesNotStealFocus()
    {
        hud.SelectEnemy(20); state.enemies[0].health = 0;
        state.phase = BattlePhase.EnemyTurn; state.actingEnemyId = 10; hud.Render(state);
        Assert.AreEqual(20, hud.DisplayedEnemyId);
        state.actingEnemyId = 999; hud.Render(state); Assert.AreEqual(20, hud.DisplayedEnemyId);
    }
}
