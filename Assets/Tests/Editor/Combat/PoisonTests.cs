using NUnit.Framework;
using UnityEngine;

public class PoisonTests
{
    private GameObject testObject;
    private Health health;
    private Poison poison;

    [SetUp]
    public void SetUp()
    {
        testObject = new GameObject("TestUnit");
        health = testObject.AddComponent<Health>();
        health.SetMaxHealth(50);
        poison = testObject.AddComponent<Poison>();
        poison.SetValues(5, 2);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(testObject);
    }

    [Test]
    public void AddStacks_IncreasesStacks()
    {
        poison.AddStacks(2);

        Assert.AreEqual(2, poison.PoisonStacks);
    }

    [Test]
    public void AddStacks_CapsAtMaxStacks()
    {
        poison.AddStacks(4);
        poison.AddStacks(4);

        Assert.AreEqual(5, poison.PoisonStacks);
    }

    [Test]
    public void AddStacks_IgnoresZeroAndNegative()
    {
        poison.AddStacks(0);
        poison.AddStacks(-3);

        Assert.AreEqual(0, poison.PoisonStacks);
    }

    [Test]
    public void Tick_DealsStacksTimesDamagePerStack_AndKeepsStacks()
    {
        poison.AddStacks(3);
        poison.Tick();

        Assert.AreEqual(44, health.CurrentHealth);
        Assert.AreEqual(3, poison.PoisonStacks);
    }

    [Test]
    public void Tick_DoesNothing_WithNoStacks()
    {
        poison.Tick();

        Assert.AreEqual(50, health.CurrentHealth);
    }

    [Test]
    public void ConsumeAllStacks_ReturnsCountAndClears()
    {
        poison.AddStacks(4);
        int consumed = poison.ConsumeAllStacks();

        Assert.AreEqual(4, consumed);
        Assert.AreEqual(0, poison.PoisonStacks);
    }

    [Test]
    public void AddStacksAndTick_AreIgnored_WhenDead()
    {
        poison.AddStacks(2);
        health.TakeDamage(50);

        poison.AddStacks(2);
        poison.Tick();

        Assert.AreEqual(2, poison.PoisonStacks);
        Assert.AreEqual(0, health.CurrentHealth);
    }

    [Test]
    public void OnPoisonChanged_SendsNewStackCount()
    {
        int reportedStacks = -1;
        poison.OnPoisonChanged += (stacks) =>
        {
            reportedStacks = stacks;
        };

        poison.AddStacks(3);
        Assert.AreEqual(3, reportedStacks);

        poison.ConsumeAllStacks();
        Assert.AreEqual(0, reportedStacks);
    }

    [Test]
    public void InvalidValues_AreSetToOne()
    {
        poison.SetValues(0, -2);

        Assert.AreEqual(1, poison.MaxStacks);
        poison.AddStacks(1);
        poison.Tick();
        Assert.AreEqual(49, health.CurrentHealth);
    }
}