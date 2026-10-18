using System;
using NUnit.Framework;
using UnityEngine;
using Visha.Enemies;

public class EnemyPatternRunnerTests
{
    private GameObject _enemy;
    private EnemyPatternRunner _runner;

    [SetUp]
    public void SetUp()
    {
        _enemy = new GameObject("Enemy");
        _runner = _enemy.AddComponent<EnemyPatternRunner>();
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(_enemy);
    }

    // ---- Defaults ----

    [Test]
    public void DefaultRunnerIsTheTempleGuardian()
    {
        Assert.AreEqual(EnemyPatternRunner.PatternKind.TempleGuardian, _runner.Kind);
        Assert.AreEqual("Temple Guardian", _runner.EnemyName);
        Assert.AreEqual("Strike", _runner.IntentLabel);
        Assert.AreEqual(4, _runner.IncomingDamage);
        Assert.AreEqual(EnemyActionKind.Attack, _runner.IntentKind);
    }

    [Test]
    public void PatternKindNumbersAreStable()
    {
        // Encounter assets store the enum as a number. Renumbering would silently
        // swap the enemy in every saved encounter.
        Assert.AreEqual(0, (int)EnemyPatternRunner.PatternKind.TempleGuardian);
        Assert.AreEqual(1, (int)EnemyPatternRunner.PatternKind.VaultSentinel);
    }

    // ---- Configure: switching enemy type for a new encounter ----

    [Test]
    public void ConfigureVaultSentinelSwitchesPatternAndName()
    {
        _runner.Configure(EnemyPatternRunner.PatternKind.VaultSentinel);

        Assert.AreEqual(EnemyPatternRunner.PatternKind.VaultSentinel, _runner.Kind);
        Assert.AreEqual("Vault Sentinel", _runner.EnemyName);
        Assert.AreEqual("Defend", _runner.IntentLabel);
        Assert.AreEqual(0, _runner.IncomingDamage);
        Assert.AreEqual(EnemyActionKind.Defend, _runner.IntentKind);
        Assert.IsTrue(_runner.NextIsDefend);
    }

    [Test]
    public void ConfigureWithDisplayNameOverridesTheName()
    {
        _runner.Configure(EnemyPatternRunner.PatternKind.TempleGuardian, "Guardian Warden");
        Assert.AreEqual("Guardian Warden", _runner.EnemyName);
        Assert.AreEqual("Strike", _runner.IntentLabel, "The display name must not change the moves.");
    }

    [Test]
    public void ConfigureWithEmptyDisplayNameUsesThePatternName()
    {
        _runner.Configure(EnemyPatternRunner.PatternKind.VaultSentinel, "Named");
        _runner.Configure(EnemyPatternRunner.PatternKind.VaultSentinel, "");
        Assert.AreEqual("Vault Sentinel", _runner.EnemyName);
    }

    [Test]
    public void ConfigureRestartsTheCycleFromTheFirstMove()
    {
        _runner.TakeTurn(); // Strike
        _runner.TakeTurn(); // Heavy Blow
        _runner.Configure(EnemyPatternRunner.PatternKind.TempleGuardian);
        Assert.AreEqual("Strike", _runner.IntentLabel);
    }

    [Test]
    public void ResetPatternRestartsTheSentinelOnRetry()
    {
        _runner.Configure(EnemyPatternRunner.PatternKind.VaultSentinel);
        _runner.TakeTurn(); // Defend
        _runner.TakeTurn(); // Crushing Blow
        _runner.ResetPattern();
        Assert.AreEqual("Defend", _runner.IntentLabel);
    }

    // ---- The telegraph always matches the move that executes ----

    [TestCase(EnemyPatternRunner.PatternKind.TempleGuardian)]
    [TestCase(EnemyPatternRunner.PatternKind.VaultSentinel)]
    public void DisplayedIntentMatchesTheExecutedAction(EnemyPatternRunner.PatternKind kind)
    {
        _runner.Configure(kind);

        // Seven turns covers two full cycles plus a wrap.
        for (int turn = 0; turn < 7; turn++)
        {
            string shownLabel = _runner.IntentLabel;
            int shownDamage = _runner.IncomingDamage;
            EnemyActionKind shownKind = _runner.IntentKind;

            EnemyAction executed = _runner.TakeTurn();

            Assert.AreEqual(shownLabel, executed.Label, $"{kind} turn {turn}: label");
            Assert.AreEqual(shownDamage, executed.Damage, $"{kind} turn {turn}: damage");
            Assert.AreEqual(shownKind, executed.Kind, $"{kind} turn {turn}: kind");
        }
    }

    [Test]
    public void EveryPatternKindBuildsAWorkingPattern()
    {
        foreach (EnemyPatternRunner.PatternKind kind in Enum.GetValues(typeof(EnemyPatternRunner.PatternKind)))
        {
            _runner.Configure(kind);
            Assert.IsFalse(string.IsNullOrEmpty(_runner.EnemyName), $"{kind} has no name");
            Assert.IsFalse(string.IsNullOrEmpty(_runner.IntentLabel), $"{kind} has no first move");
        }
    }
}
