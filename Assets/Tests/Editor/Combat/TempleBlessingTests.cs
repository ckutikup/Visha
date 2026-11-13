using NUnit.Framework;
using UnityEngine;

public class TempleBlessingTests
{
    private GameObject playerObject;
    private Health health;
    private TempleBlessing blessing;
    private Fade fade;

    [SetUp]
    public void SetUp()
    {
        playerObject = new GameObject("TestPlayer");
        health = playerObject.AddComponent<Health>();
        health.SetMaxHealth(50);
        blessing = playerObject.AddComponent<TempleBlessing>();
        fade = playerObject.AddComponent<Fade>();
        fade.SetReductionPercent(50);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(playerObject);
    }

    [Test]
    public void LethalHit_LeavesOneHp_AndSpendsBlessing()
    {
        blessing.EnableForEncounter();

        health.TakeDamage(60);

        Assert.AreEqual(1, health.CurrentHealth);
        Assert.IsFalse(health.IsDead);
        Assert.IsTrue(blessing.IsSpent);
    }

    [Test]
    public void ExactLethalHit_IsAlsoSaved()
    {
        blessing.EnableForEncounter();

        health.TakeDamage(50);

        Assert.AreEqual(1, health.CurrentHealth);
        Assert.IsTrue(blessing.IsSpent);
    }

    [Test]
    public void SecondHit_AfterBlessingSpent_Kills()
    {
        blessing.EnableForEncounter();
        health.TakeDamage(60);

        health.TakeDamage(5);

        Assert.AreEqual(0, health.CurrentHealth);
        Assert.IsTrue(health.IsDead);
    }

    [Test]
    public void MultipleHits_OnlyFirstLethalHitIsSaved()
    {
        blessing.EnableForEncounter();
        health.TakeDamage(45);

        health.TakeDamage(9);
        Assert.AreEqual(1, health.CurrentHealth);

        health.TakeDamage(4);
        Assert.IsTrue(health.IsDead);
    }

    [Test]
    public void NormalHit_DoesNotSpendBlessing()
    {
        blessing.EnableForEncounter();

        health.TakeDamage(20);

        Assert.AreEqual(30, health.CurrentHealth);
        Assert.IsTrue(blessing.IsAvailable);
    }

    [Test]
    public void BlessingNotEnabled_PlayerDies()
    {
        health.TakeDamage(60);

        Assert.IsTrue(health.IsDead);
        Assert.IsFalse(blessing.IsSpent);
    }

    [Test]
    public void Fade_ReducesFirst_HitNotLethal_BlessingKept()
    {
        blessing.EnableForEncounter();
        health.TakeDamage(40);
        fade.TryUse();

        int damage = fade.ReduceDamage(18);
        health.TakeDamage(damage);

        Assert.AreEqual(1, health.CurrentHealth);
        Assert.IsTrue(blessing.IsAvailable);
    }

    [Test]
    public void Fade_ReducedHitStillLethal_UsesBlessing()
    {
        blessing.EnableForEncounter();
        health.TakeDamage(46);
        fade.TryUse();

        int damage = fade.ReduceDamage(9);
        health.TakeDamage(damage);

        Assert.AreEqual(1, health.CurrentHealth);
        Assert.IsTrue(blessing.IsSpent);
    }

    [Test]
    public void Retry_AfterDefeat_RestoresBlessing()
    {
        blessing.EnableForEncounter();
        health.TakeDamage(60);
        health.TakeDamage(5);

        health.ResetHealth();

        Assert.AreEqual(50, health.CurrentHealth);
        Assert.IsTrue(blessing.IsAvailable);
    }

    [Test]
    public void ResetHealth_DoesNotGiveBlessing_OutsideBossFight()
    {
        health.ResetHealth();

        Assert.IsFalse(blessing.IsEnabled);
        Assert.IsFalse(blessing.IsAvailable);
    }

    [Test]
    public void LeavingBossFight_TurnsBlessingOff()
    {
        blessing.EnableForEncounter();

        blessing.DisableForEncounter();
        health.TakeDamage(60);

        Assert.IsFalse(blessing.IsEnabled);
        Assert.IsTrue(health.IsDead);
    }

    [Test]
    public void TriggeredEvent_FiresOnlyOnce()
    {
        blessing.EnableForEncounter();
        int triggeredCount = 0;
        blessing.OnBlessingTriggered += () =>
        {
            triggeredCount++;
        };

        health.TakeDamage(60);
        health.TakeDamage(5);

        Assert.AreEqual(1, triggeredCount);
    }
}