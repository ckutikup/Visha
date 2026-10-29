using NUnit.Framework;
using UnityEngine;

public class TwoEnemyAbilityTests
{
    private GameObject playerObject;
    private GameObject enemyA;
    private GameObject enemyB;
    private VenomStrike venomStrike;
    private SerpentsBite bite;
    private Fade fade;
    private Health healthA;
    private Health healthB;
    private Poison poisonA;
    private Poison poisonB;

    [SetUp]
    public void SetUp()
    {
        playerObject = new GameObject("TestPlayer");
        venomStrike = playerObject.AddComponent<VenomStrike>();
        venomStrike.SetStacksApplied(2);
        bite = playerObject.AddComponent<SerpentsBite>();
        bite.SetDamagePerStack(4);
        fade = playerObject.AddComponent<Fade>();
        fade.SetReductionPercent(50);

        enemyA = new GameObject("EnemyA");
        healthA = enemyA.AddComponent<Health>();
        healthA.SetMaxHealth(50);
        poisonA = enemyA.AddComponent<Poison>();
        poisonA.SetValues(5, 2);

        enemyB = new GameObject("EnemyB");
        healthB = enemyB.AddComponent<Health>();
        healthB.SetMaxHealth(50);
        poisonB = enemyB.AddComponent<Poison>();
        poisonB.SetValues(5, 2);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(playerObject);
        Object.DestroyImmediate(enemyA);
        Object.DestroyImmediate(enemyB);
    }

    [Test]
    public void VenomStrike_OnlyPoisonsSelectedEnemy()
    {
        venomStrike.TryUseOn(enemyA);
        Assert.AreEqual(2, poisonA.PoisonStacks);
        Assert.AreEqual(0, poisonB.PoisonStacks);
    }

    [Test]
    public void PoisonCap_IsTrackedSeparatelyPerEnemy()
    {
        venomStrike.TryUseOn(enemyA);
        venomStrike.TryUseOn(enemyA);
        venomStrike.TryUseOn(enemyA);
        venomStrike.TryUseOn(enemyB);
        Assert.AreEqual(5, poisonA.PoisonStacks);
        Assert.AreEqual(2, poisonB.PoisonStacks);
    }

    [Test]
    public void Bite_OnlyConsumesAndDamagesSelectedEnemy()
    {
        venomStrike.TryUseOn(enemyA);
        venomStrike.TryUseOn(enemyA);
        venomStrike.TryUseOn(enemyB);
        bool used = bite.TryUseOn(enemyA);
        Assert.IsTrue(used);
        Assert.AreEqual(0, poisonA.PoisonStacks);
        Assert.AreEqual(34, healthA.CurrentHealth);
        Assert.AreEqual(2, poisonB.PoisonStacks);
        Assert.AreEqual(50, healthB.CurrentHealth);
    }

    [Test]
    public void Bite_OnUnpoisonedEnemy_IsRejected_EvenIfOtherHasPoison()
    {
        venomStrike.TryUseOn(enemyA);
        bool used = bite.TryUseOn(enemyB);
        Assert.IsFalse(used);
        Assert.AreEqual(2, poisonA.PoisonStacks);
        Assert.AreEqual(50, healthB.CurrentHealth);
    }

    [Test]
    public void PoisonTick_EachEnemyTakesOnlyItsOwnDamage()
    {
        venomStrike.TryUseOn(enemyA);
        venomStrike.TryUseOn(enemyA);
        venomStrike.TryUseOn(enemyB);
        poisonA.Tick();
        poisonB.Tick();
        Assert.AreEqual(42, healthA.CurrentHealth);
        Assert.AreEqual(46, healthB.CurrentHealth);
    }

    [Test]
    public void DeadEnemy_RejectsVenomAndBite_OtherEnemyStillWorks()
    {
        venomStrike.TryUseOn(enemyA);
        healthA.TakeDamage(50);
        Assert.IsFalse(venomStrike.TryUseOn(enemyA));
        Assert.IsFalse(bite.TryUseOn(enemyA));
        Assert.IsTrue(venomStrike.TryUseOn(enemyB));
        Assert.AreEqual(2, poisonB.PoisonStacks);
    }

    [Test]
    public void Fade_HalvesEveryHitInTheSameEnemyPhase()
    {
        fade.TryUse();
        Assert.AreEqual(4, fade.ReduceDamage(9));
        Assert.AreEqual(2, fade.ReduceDamage(4));
        fade.EndEnemyPhase();
        Assert.AreEqual(9, fade.ReduceDamage(9));
    }

    [Test]
    public void ResetStacks_OnOneEnemy_LeavesOtherUntouched()
    {
        venomStrike.TryUseOn(enemyA);
        venomStrike.TryUseOn(enemyB);
        poisonA.ResetStacks();
        Assert.AreEqual(0, poisonA.PoisonStacks);
        Assert.AreEqual(2, poisonB.PoisonStacks);
    }

    [Test]
    public void TwinFang_PutsResidueOnOtherLivingEnemy()
    {
        TwinFang.ApplyResidue(poisonA, poisonB);
        Assert.AreEqual(0, poisonA.PoisonStacks);
        Assert.AreEqual(2, poisonB.PoisonStacks);
    }

    [Test]
    public void TwinFang_FallsBackToStruckEnemy_WhenOtherIsDead()
    {
        healthB.TakeDamage(50);
        TwinFang.ApplyResidue(poisonA, poisonB);
        Assert.AreEqual(2, poisonA.PoisonStacks);
        Assert.AreEqual(0, poisonB.PoisonStacks);
    }
}