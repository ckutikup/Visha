using NUnit.Framework;
using UnityEngine;

public class SerpentsBiteTests
{
    private GameObject playerObject;
    private GameObject enemyObject;
    private SerpentsBite bite;
    private Health enemyHealth;
    private Poison enemyPoison;

    [SetUp]
    public void SetUp()
    {
        playerObject = new GameObject("TestPlayer");
        bite = playerObject.AddComponent<SerpentsBite>();
        bite.SetDamagePerStack(4);

        enemyObject = new GameObject("TestEnemy");
        enemyHealth = enemyObject.AddComponent<Health>();
        enemyHealth.SetMaxHealth(50);
        enemyPoison = enemyObject.AddComponent<Poison>();
        enemyPoison.SetValues(5, 2);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(playerObject);
        Object.DestroyImmediate(enemyObject);
    }

    [Test]
    public void TryUseOn_ConsumesStacksAndDealsDamage()
    {
        enemyPoison.AddStacks(3);

        bool used = bite.TryUseOn(enemyObject);

        Assert.IsTrue(used);
        Assert.AreEqual(0, enemyPoison.PoisonStacks);
        Assert.AreEqual(38, enemyHealth.CurrentHealth);
    }

    [Test]
    public void TryUseOn_ConsumesStacksOnlyOnce()
    {
        enemyPoison.AddStacks(3);

        bite.TryUseOn(enemyObject);
        bool usedAgain = bite.TryUseOn(enemyObject);

        Assert.IsFalse(usedAgain);
        Assert.AreEqual(38, enemyHealth.CurrentHealth);
    }

    [Test]
    public void TryUseOn_NoStacks_IsRejected()
    {
        bool used = bite.TryUseOn(enemyObject);

        Assert.IsFalse(used);
        Assert.AreEqual(50, enemyHealth.CurrentHealth);
    }

    [Test]
    public void TryUseOn_DeadTarget_IsRejectedAndKeepsStacks()
    {
        enemyPoison.AddStacks(2);
        enemyHealth.TakeDamage(50);

        bool used = bite.TryUseOn(enemyObject);

        Assert.IsFalse(used);
        Assert.AreEqual(2, enemyPoison.PoisonStacks);
    }

    [Test]
    public void TryUseOn_NullTarget_IsRejected()
    {
        bool used = bite.TryUseOn(null);

        Assert.IsFalse(used);
    }

    [Test]
    public void TryUseOn_TargetWithoutPoison_IsRejected()
    {
        GameObject plainTarget = new GameObject("PlainTarget");
        Health plainHealth = plainTarget.AddComponent<Health>();
        plainHealth.SetMaxHealth(50);

        bool used = bite.TryUseOn(plainTarget);

        Assert.IsFalse(used);
        Assert.AreEqual(50, plainHealth.CurrentHealth);
        Object.DestroyImmediate(plainTarget);
    }

    [Test]
    public void CanUseOn_MatchesPoisonState()
    {
        Assert.IsFalse(bite.CanUseOn(enemyObject));

        enemyPoison.AddStacks(1);

        Assert.IsTrue(bite.CanUseOn(enemyObject));
    }

    [Test]
    public void TryUseOn_UsesConfiguredDamage()
    {
        bite.SetDamagePerStack(10);
        enemyPoison.AddStacks(2);

        bite.TryUseOn(enemyObject);

        Assert.AreEqual(30, enemyHealth.CurrentHealth);
    }

    [Test]
    public void InvalidDamage_IsSetToOne()
    {
        bite.SetDamagePerStack(0);

        Assert.AreEqual(1, bite.DamagePerStack);
    }
}