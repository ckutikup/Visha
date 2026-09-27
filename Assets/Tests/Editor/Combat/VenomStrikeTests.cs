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
    public void UseOn_AddsStacks()
    {
        venomStrike.UseOn(enemyObject);
        Assert.AreEqual(2, enemyPoison.PoisonStacks);
    }

    [Test]
    public void UseOn_DealsNoDirectDamage()
    {
        venomStrike.UseOn(enemyObject);
        Assert.AreEqual(50, enemyHealth.CurrentHealth);
    }
    [Test]
    public void UseOn_StacksStopAtPoisonCap()
    {
        venomStrike.UseOn(enemyObject);
        venomStrike.UseOn(enemyObject);
        venomStrike.UseOn(enemyObject);
        Assert.AreEqual(5, enemyPoison.PoisonStacks);
    }

    [Test]
    public void UseOn_DeadTarget_GetsNoStacks()
    {
        enemyHealth.TakeDamage(50);
        venomStrike.UseOn(enemyObject);
        Assert.AreEqual(0, enemyPoison.PoisonStacks);
    }

    [Test]
    public void UseOn_TargetWithoutPoison_DoesNotCrash()
    {
        GameObject plainTarget = new GameObject("PlainTarget");
        Assert.DoesNotThrow(() =>
        {
            venomStrike.UseOn(plainTarget);
        });
        Object.DestroyImmediate(plainTarget);
    }

    [Test]
    public void UseOn_NullTarget_DoesNotCrash()
    {
        Assert.DoesNotThrow(() =>
        {
            venomStrike.UseOn(null);
        });
    }

    [Test]
    public void InvalidStacks_AreSetToOne()
    {
        venomStrike.SetStacksApplied(0);
        Assert.AreEqual(1, venomStrike.StacksApplied);
    }
}