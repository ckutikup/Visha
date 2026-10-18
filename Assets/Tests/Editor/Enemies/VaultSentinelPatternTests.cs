using NUnit.Framework;
using Visha.Enemies;

public class VaultSentinelPatternTests
{
    // ---- Vault Sentinel cycle: Defend -> Crushing Blow -> Shield Bash ----

    [Test]
    public void SentinelCycleFollowsDefendCrushingBlowShieldBashInOrder()
    {
        var p = VaultSentinelPattern.Create();

        Assert.AreEqual("Vault Sentinel", p.Name);

        Assert.AreEqual("Defend", p.Current.Label);
        Assert.AreEqual(0, p.Current.Damage);
        Assert.AreEqual(EnemyActionKind.Defend, p.Current.Kind);

        p.Advance();
        Assert.AreEqual("Crushing Blow", p.Current.Label);
        Assert.AreEqual(16, p.Current.Damage);
        Assert.AreEqual(EnemyActionKind.Attack, p.Current.Kind);

        p.Advance();
        Assert.AreEqual("Shield Bash", p.Current.Label);
        Assert.AreEqual(8, p.Current.Damage);
        Assert.AreEqual(EnemyActionKind.Attack, p.Current.Kind);
    }

    [Test]
    public void SentinelCycleLoopsBackToDefend()
    {
        var p = VaultSentinelPattern.Create();
        Assert.AreEqual(3, p.Length);
        p.Advance();
        p.Advance();
        p.Advance();
        Assert.AreEqual(0, p.CurrentIndex);
        Assert.AreEqual("Defend", p.Current.Label);
    }

    [Test]
    public void SentinelIsDeterministic_TwoFreshCopiesProduceSameSequence()
    {
        var a = VaultSentinelPattern.Create();
        var b = VaultSentinelPattern.Create();
        for (int i = 0; i < 10; i++)
        {
            Assert.AreEqual(a.Current.Label, b.Current.Label, $"Divergence at step {i}");
            Assert.AreEqual(a.Current.Damage, b.Current.Damage, $"Divergence at step {i}");
            Assert.AreEqual(a.Current.Kind, b.Current.Kind, $"Divergence at step {i}");
            a.Advance();
            b.Advance();
        }
    }

    [Test]
    public void SentinelDefendLabelShowsTheShieldIcon()
    {
        // IntentView shows the defend icon when the label contains "defend".
        var p = VaultSentinelPattern.Create();
        StringAssert.Contains("defend", p.Current.Label.ToLowerInvariant());
    }

    [Test]
    public void SentinelAttackLabelsDoNotTriggerTheShieldIcon()
    {
        var p = VaultSentinelPattern.Create();
        for (int i = 0; i < p.Length; i++)
        {
            EnemyAction move = p.Peek(i);
            bool showsShield = move.Label.ToLowerInvariant().Contains("defend");
            Assert.AreEqual(move.Kind == EnemyActionKind.Defend, showsShield,
                $"'{move.Label}' icon does not match its kind");
        }
    }

    [Test]
    public void SentinelPlaysDifferentlyFromTheGuardian()
    {
        // The plan asks for two enemy types that behave differently. Compare the
        // full damage sequence of one cycle.
        var guardian = TempleGuardianPattern.Create();
        var sentinel = VaultSentinelPattern.Create();

        var guardianDamage = new int[guardian.Length];
        for (int i = 0; i < guardian.Length; i++) guardianDamage[i] = guardian.Peek(i).Damage;

        var sentinelDamage = new int[sentinel.Length];
        for (int i = 0; i < sentinel.Length; i++) sentinelDamage[i] = sentinel.Peek(i).Damage;

        CollectionAssert.AreNotEqual(guardianDamage, sentinelDamage);
        Assert.AreNotEqual(guardian.Current.Kind, sentinel.Current.Kind,
            "The Sentinel opens with Defend, the Guardian with an attack.");
    }
}
