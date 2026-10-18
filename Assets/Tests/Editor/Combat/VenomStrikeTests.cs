using NUnit.Framework;
using UnityEngine;

public class VenomStrikeTests
{
    private GameObject playerObject;
    private GameObject enemyObject;
    private VenomStrike venomStrike;
    private Health enemyHealth;
    private Poison enemyPoison;

    [SetUp]
    public void SetUp()
    {
        playerObject = new GameObject("TestPlayer");
        venomStrike = playerObject.AddComponent<VenomStrike>();
        venomStrike.SetStacksApplied(2);

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
    public void TryUseOn_AddsStacks()
    {
        bool used = venomStrike.TryUseOn(enemyObject);

        Assert.IsTrue(used);
        Assert.AreEqual(2, enemyPoison.PoisonStacks);
    }

    [Test]
    public void TryUseOn_DealsNoDirectDamage()
    {
        venomStrike.TryUseOn(enemyObject);

        Assert.AreEqual(50, enemyHealth.CurrentHealth);
    }

    [Test]
    public void TryUseOn_StacksStopAtPoisonCap()
    {
        venomStrike.TryUseOn(enemyObject);
        venomStrike.TryUseOn(enemyObject);
        venomStrike.TryUseOn(enemyObject);

        Assert.AreEqual(5, enemyPoison.PoisonStacks);
    }

    [Test]
    public void TryUseOn_AtCap_IsStillValid()
    {
        venomStrike.TryUseOn(enemyObject);
        venomStrike.TryUseOn(enemyObject);
        venomStrike.TryUseOn(enemyObject);

        bool used = venomStrike.TryUseOn(enemyObject);

        Assert.IsTrue(used);
        Assert.AreEqual(5, enemyPoison.PoisonStacks);
    }

    [Test]
    public void TryUseOn_DeadTarget_IsRejected()
    {
        enemyHealth.TakeDamage(50);

        bool used = venomStrike.TryUseOn(enemyObject);

        Assert.IsFalse(used);
        Assert.AreEqual(0, enemyPoison.PoisonStacks);
    }
    [Test]
    public void TryUseOn_TargetWithoutPoison_IsRejected()
    {
        GameObject plainTarget = new GameObject("PlainTarget");

        bool used = venomStrike.TryUseOn(plainTarget);

        Assert.IsFalse(used);
        Object.DestroyImmediate(plainTarget);
    }
    [Test]
    public void TryUseOn_NullTarget_IsRejected()
    {
        bool used = venomStrike.TryUseOn(null);

        Assert.IsFalse(used);
    }
    [Test]
    public void InvalidStacks_AreSetToOne()
    {
        venomStrike.SetStacksApplied(0);

        Assert.AreEqual(1, venomStrike.StacksApplied);
    }
}