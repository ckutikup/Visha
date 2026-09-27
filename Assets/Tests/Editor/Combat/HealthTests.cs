using NUnit.Framework;
using UnityEngine;

public class HealthTests
{
    private GameObject testObject;
    private Health health;

    [SetUp]
    public void SetUp()
    {
        testObject = new GameObject("TestUnit");
        health = testObject.AddComponent<Health>();
        health.SetMaxHealth(50);
    }
    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(testObject);
    }
    [Test]
    public void LethalDamage_SetsHealthToZeroAndDead()
    {
        health.TakeDamage(60);
        Assert.AreEqual(0, health.CurrentHealth);
        Assert.IsTrue(health.IsDead);
    }
    [Test]
    public void IsDead_IsAlreadyTrue_WhenHealthChangedFiresOnLethalHit()
    {
        bool deadDuringEvent = false;
        health.OnHealthChanged += (current, max) =>
        {
            deadDuringEvent = health.IsDead;
        };
        health.TakeDamage(50);
        Assert.IsTrue(deadDuringEvent);
    }
    [Test]
    public void OnDied_FiresOnlyOnce()
    {
        int deathCount = 0;
        health.OnDied += () =>
        {
            deathCount++;
        };
        health.TakeDamage(50);
        health.TakeDamage(10);
        Assert.AreEqual(1, deathCount);
    }
    [Test]
    public void OnDied_FiresOnce_WhenListenerDealsMoreDamage()
    {
        int deathCount = 0;
        health.OnDied += () =>
        {
            deathCount++;
        };
        health.OnHealthChanged += (current, max) =>
        {
            health.TakeDamage(10);
        };
        health.TakeDamage(45);
        Assert.AreEqual(1, deathCount);
    }
    [Test]
    public void DamageAndHealAfterDeath_AreIgnored()
    {
        health.TakeDamage(50);
        health.TakeDamage(10);
        health.Heal(20);
        Assert.AreEqual(0, health.CurrentHealth);
        Assert.IsTrue(health.IsDead);
    }
    [Test]
    public void InvalidMaxHealth_IsSetToOne()
    {
        health.SetMaxHealth(0);
        Assert.AreEqual(1, health.MaxHealth);
        Assert.AreEqual(1, health.CurrentHealth);
        Assert.IsFalse(health.IsDead);

        //Trying negative scenario
        health.SetMaxHealth(-10);
        Assert.AreEqual(1, health.MaxHealth);
        Assert.IsFalse(health.IsDead);
    }
    [Test]
    public void ResetHealth_RestoresFullHealthAndRevives()
    {
        health.TakeDamage(50);
        health.ResetHealth();
        Assert.AreEqual(50, health.CurrentHealth);
        Assert.IsFalse(health.IsDead);
    }
}