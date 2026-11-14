using NUnit.Framework;
using UnityEngine;
using Visha.Enemies;

public class AsuraPatternTests
{
    private GameObject _boss;
    private EnemyPatternRunner _runner;

    [SetUp]
    public void SetUp()
    {
        _boss = new GameObject("Asura");
        _runner = _boss.AddComponent<EnemyPatternRunner>();
        _runner.Configure(EnemyPatternRunner.PatternKind.Asura);
    }

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(_boss);

    // ---- The two cycles ----

    [Test]
    public void PhaseOneCycleIsRecoverCursedClawRuin()
    {
        var p = AsuraPattern.CreatePhaseOne();
        AssertMove(p.Peek(0), "Recover", 0, EnemyActionKind.Defend);
        AssertMove(p.Peek(1), "Cursed Claw", 10, EnemyActionKind.Attack);
        AssertMove(p.Peek(2), "Ruin", 16, EnemyActionKind.Attack);
        Assert.AreEqual(3, p.Length);
    }

    [Test]
    public void PhaseTwoCycleIsRoarCleaveHexCataclysm()
    {
        var p = AsuraPattern.CreatePhaseTwo();
        AssertMove(p.Peek(0), "Roar", 0, EnemyActionKind.Defend);
        AssertMove(p.Peek(1), "Cleave", 12, EnemyActionKind.Attack);
        AssertMove(p.Peek(2), "Hex", 8, EnemyActionKind.Attack);
        AssertMove(p.Peek(3), "Cataclysm", 22, EnemyActionKind.Attack);
        Assert.AreEqual(4, p.Length);
    }

    [Test]
    public void PhaseTwoIsStrongerButKeepsAFreeTurnEveryCycle()
    {
        float phaseOneAverage = AverageDamage(AsuraPattern.CreatePhaseOne());
        float phaseTwoAverage = AverageDamage(AsuraPattern.CreatePhaseTwo());
        Assert.Greater(phaseTwoAverage, phaseOneAverage, "phase 2 should hit harder per turn");
        Assert.Greater(MaxDamage(AsuraPattern.CreatePhaseTwo()), MaxDamage(AsuraPattern.CreatePhaseOne()));

        Assert.IsTrue(HasFreeTurn(AsuraPattern.CreatePhaseOne()), "phase 1 needs a counterattack window");
        Assert.IsTrue(HasFreeTurn(AsuraPattern.CreatePhaseTwo()), "phase 2 needs a counterattack window");
    }

    [Test]
    public void EveryBossAttackCanEarnAwakeningWhenFaded()
    {
        // The combat flow grants a charge for Fading an attack of 8 or more.
        foreach (var p in new[] { AsuraPattern.CreatePhaseOne(), AsuraPattern.CreatePhaseTwo() })
            for (int i = 0; i < p.Length; i++)
                if (p.Peek(i).Kind == EnemyActionKind.Attack)
                    Assert.GreaterOrEqual(p.Peek(i).Damage, 8, p.Peek(i).Label);
    }

    [Test]
    public void EveryBossLabelFitsTheHudIntentBox()
    {
        foreach (var p in new[] { AsuraPattern.CreatePhaseOne(), AsuraPattern.CreatePhaseTwo() })
            for (int i = 0; i < p.Length; i++)
                Assert.LessOrEqual(p.Peek(i).Label.Length, 11, p.Peek(i).Label);
    }

    // ---- Runner: boss identity and phase reporting ----

    [Test]
    public void AsuraKindIsNumberTwo()
    {
        Assert.AreEqual(2, (int)EnemyPatternRunner.PatternKind.Asura, "encounter assets store this number");
    }

    [Test]
    public void BossReportsPhaseOneOfTwo()
    {
        Assert.IsTrue(_runner.IsBoss);
        Assert.AreEqual("The Asura", _runner.EnemyName);
        Assert.AreEqual(1, _runner.BossPhase);
        Assert.AreEqual(2, _runner.BossPhaseCount);
        Assert.AreEqual("Recover", _runner.IntentLabel);
    }

    [Test]
    public void NormalEnemiesReportNoBossPhase()
    {
        _runner.Configure(EnemyPatternRunner.PatternKind.VaultSentinel);
        Assert.IsFalse(_runner.IsBoss);
        Assert.AreEqual(0, _runner.BossPhase);
        Assert.AreEqual(0, _runner.BossPhaseCount);
        Assert.IsFalse(_runner.UpdateBossPhase(1, 46), "normal enemies never change phase");
    }

    // ---- Runner: the phase change ----

    [Test]
    public void ThresholdIsHalfOfMaxHealth()
    {
        Assert.AreEqual(38, AsuraPattern.PhaseTwoThreshold(76));
        Assert.AreEqual(37, AsuraPattern.PhaseTwoThreshold(75));
    }

    [Test]
    public void StaysInPhaseOneAboveHalfHealth()
    {
        Assert.IsFalse(_runner.UpdateBossPhase(39, 76));
        Assert.AreEqual(1, _runner.BossPhase);
    }

    [Test]
    public void EntersPhaseTwoAtHalfHealthOnRoar()
    {
        _runner.TakeTurn(); // Recover
        _runner.TakeTurn(); // Cursed Claw, so phase 1 is mid-cycle

        Assert.IsTrue(_runner.UpdateBossPhase(38, 76));

        Assert.AreEqual(2, _runner.BossPhase);
        Assert.AreEqual("Roar", _runner.IntentLabel, "phase 2 opens with a free turn");
        Assert.AreEqual(0, _runner.IncomingDamage);
    }

    [Test]
    public void PhaseChangeIsReportedOnlyOnce()
    {
        Assert.IsTrue(_runner.UpdateBossPhase(20, 76));
        Assert.IsFalse(_runner.UpdateBossPhase(10, 76));
        Assert.AreEqual(2, _runner.BossPhase);
    }

    [Test]
    public void DeadBossDoesNotChangePhase()
    {
        Assert.IsFalse(_runner.UpdateBossPhase(0, 76));
        Assert.AreEqual(1, _runner.BossPhase);
    }

    [Test]
    public void BigDropSkipsStraightToPhaseTwo()
    {
        // A Bite can take the boss from above half to well below in one step.
        Assert.IsTrue(_runner.UpdateBossPhase(5, 76));
        Assert.AreEqual("Roar", _runner.IntentLabel);
    }

    [Test]
    public void DisplayedIntentMatchesExecutedMoveInPhaseTwo()
    {
        _runner.UpdateBossPhase(30, 76);
        for (int turn = 0; turn < 9; turn++)
        {
            string shown = _runner.IntentLabel;
            int shownDamage = _runner.IncomingDamage;
            EnemyAction executed = _runner.TakeTurn();
            Assert.AreEqual(shown, executed.Label, $"turn {turn}");
            Assert.AreEqual(shownDamage, executed.Damage, $"turn {turn}");
        }
    }

    [Test]
    public void RetryResetsTheBossToPhaseOne()
    {
        _runner.UpdateBossPhase(30, 76);
        _runner.TakeTurn();

        _runner.ResetPattern();

        Assert.AreEqual(1, _runner.BossPhase);
        Assert.AreEqual("Recover", _runner.IntentLabel);
        Assert.IsTrue(_runner.UpdateBossPhase(30, 76), "the phase change can happen again after a retry");
    }

    [Test]
    public void ConfiguringANewEncounterClearsPhaseTwo()
    {
        _runner.UpdateBossPhase(30, 76);
        _runner.Configure(EnemyPatternRunner.PatternKind.Asura);
        Assert.AreEqual(1, _runner.BossPhase);
        Assert.AreEqual("Recover", _runner.IntentLabel);
    }

    // ---- helpers ----

    private static void AssertMove(EnemyAction move, string label, int damage, EnemyActionKind kind)
    {
        Assert.AreEqual(label, move.Label);
        Assert.AreEqual(damage, move.Damage, label);
        Assert.AreEqual(kind, move.Kind, label);
    }

    private static float AverageDamage(EnemyAttackPattern p)
    {
        int total = 0;
        for (int i = 0; i < p.Length; i++) total += p.Peek(i).Damage;
        return (float)total / p.Length;
    }

    private static int MaxDamage(EnemyAttackPattern p)
    {
        int max = 0;
        for (int i = 0; i < p.Length; i++) max = Mathf.Max(max, p.Peek(i).Damage);
        return max;
    }

    private static bool HasFreeTurn(EnemyAttackPattern p)
    {
        for (int i = 0; i < p.Length; i++)
            if (p.Peek(i).Damage == 0) return true;
        return false;
    }
}
