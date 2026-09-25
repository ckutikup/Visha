using NUnit.Framework;
using Visha.Enemies;

public class EnemyPatternTests
{
    // ---- Temple Guardian cycle: deterministic and telegraphed ----

    [Test]
    public void GuardianCycleFollowsStrikeHeavyDefendInOrder()
    {
        var p = TempleGuardianPattern.Create();

        Assert.AreEqual("Strike", p.Current.Label);
        Assert.AreEqual(4, p.Current.Damage);
        Assert.AreEqual(EnemyActionKind.Attack, p.Current.Kind);

        p.Advance();
        Assert.AreEqual("Heavy Blow", p.Current.Label);
        Assert.AreEqual(9, p.Current.Damage);

        p.Advance();
        Assert.AreEqual(EnemyActionKind.Defend, p.Current.Kind);
        Assert.AreEqual(0, p.Current.Damage);
    }

    [Test]
    public void CycleLoopsBackToStartAfterLastMove()
    {
        var p = TempleGuardianPattern.Create();
        p.Advance(); // Heavy Blow
        p.Advance(); // Defend
        p.Advance(); // back to Strike
        Assert.AreEqual(0, p.CurrentIndex);
        Assert.AreEqual("Strike", p.Current.Label);
    }

    [Test]
    public void PatternIsDeterministic_TwoFreshCopiesProduceSameSequence()
    {
        var a = TempleGuardianPattern.Create();
        var b = TempleGuardianPattern.Create();
        for (int i = 0; i < 10; i++)
        {
            Assert.AreEqual(a.Current.Label, b.Current.Label, $"Divergence at step {i}");
            Assert.AreEqual(a.Current.Damage, b.Current.Damage, $"Divergence at step {i}");
            a.Advance();
            b.Advance();
        }
    }

    // ---- Peek / telegraph does not mutate the cycle ----

    [Test]
    public void CurrentAndPeekDoNotAdvanceThePattern()
    {
        var p = TempleGuardianPattern.Create();
        var _ = p.Current;
        var __ = p.Peek(1);
        var ___ = p.Peek(2);
        Assert.AreEqual(0, p.CurrentIndex, "Reading the telegraph must not change the pattern.");
        Assert.AreEqual("Strike", p.Current.Label);
    }

    [Test]
    public void PeekLooksAheadWithoutChangingCurrent()
    {
        var p = TempleGuardianPattern.Create();
        Assert.AreEqual("Strike", p.Peek(0).Label);
        Assert.AreEqual("Heavy Blow", p.Peek(1).Label);
        Assert.AreEqual("Defend", p.Peek(2).Label);
        Assert.AreEqual("Strike", p.Peek(3).Label); // wraps
        Assert.AreEqual(0, p.CurrentIndex);
    }

    // ---- TakeTurn returns then advances ----

    [Test]
    public void TakeTurnReturnsCurrentThenAdvances()
    {
        var p = TempleGuardianPattern.Create();
        var first = p.TakeTurn();
        Assert.AreEqual("Strike", first.Label);
        Assert.AreEqual("Heavy Blow", p.Current.Label);
    }

    [Test]
    public void ResetReturnsToFirstMove()
    {
        var p = TempleGuardianPattern.Create();
        p.Advance();
        p.Advance();
        p.Reset();
        Assert.AreEqual(0, p.CurrentIndex);
        Assert.AreEqual("Strike", p.Current.Label);
    }

    // ---- EnemyAction guards ----

    [Test]
    public void NegativeDamageIsClampedToZero()
    {
        var a = EnemyAction.Attack("Odd", -5);
        Assert.AreEqual(0, a.Damage);
    }

    [Test]
    public void DefendLabelContainsDefendSoTheUiShowsTheShieldIcon()
    {
        // IntentView picks the defend icon when the label contains "defend"
        // (case-insensitive). This guards that coupling so a rename doesn't
        // silently break the icon.
        var d = EnemyAction.Defend();
        StringAssert.Contains("defend", d.Label.ToLowerInvariant());
        Assert.AreEqual(EnemyActionKind.Defend, d.Kind);
    }

    [Test]
    public void EmptyPatternIsRejected()
    {
        Assert.Throws<System.ArgumentException>(
            () => new EnemyAttackPattern("Broken", new EnemyAction[0]));
    }
}
