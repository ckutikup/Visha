using NUnit.Framework;
using UnityEngine;
using Visha.Combat;

public class AbilityAuditTests
{
    private GameObject playerObject;
    private GameObject enemyA;
    private GameObject enemyB;
    private SerpentsBite bite;
    private Health healthA;
    private Health healthB;
    private Poison poisonA;
    private Poison poisonB;

    [SetUp]
    public void SetUp()
    {
        playerObject = new GameObject("TestPlayer");
        bite = playerObject.AddComponent<SerpentsBite>();
        bite.SetDamagePerStack(4);

        enemyA = new GameObject("EnemyA");
        healthA = enemyA.AddComponent<Health>();
        healthA.SetMaxHealth(30);
        poisonA = enemyA.AddComponent<Poison>();
        poisonA.SetValues(5, 2);

        enemyB = new GameObject("EnemyB");
        healthB = enemyB.AddComponent<Health>();
        healthB.SetMaxHealth(30);
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
    public void Awakening_NoChargesEarned_WhileActive()
    {
        GuardianAwakening awakening = new GuardianAwakening();
        awakening.Reset(true, true);
        awakening.TryActivate();

        awakening.EarnCharge();
        awakening.EarnCharge();

        Assert.AreEqual(0, awakening.Charges);
    }

    [Test]
    public void TwinFang_NotReady_BeforeOrAfterAwakening()
    {
        GuardianAwakening awakening = new GuardianAwakening();
        awakening.Reset(true, true);
        Assert.IsFalse(awakening.TryUseTwinFang());

        awakening.TryActivate();
        awakening.CompletePlayerAction();
        awakening.CompletePlayerAction();

        Assert.IsFalse(awakening.TryUseTwinFang());
    }

    [Test]
    public void FinishingAnAction_WhenNotActive_ChangesNothing()
    {
        GuardianAwakening awakening = new GuardianAwakening();
        awakening.Reset(true);

        awakening.CompletePlayerAction();

        Assert.AreEqual(0, awakening.ActionsRemaining);
        Assert.IsFalse(awakening.Active);
    }

    [Test]
    public void Retry_ClearsUsedAwakeningAndTwinFang()
    {
        GuardianAwakening awakening = new GuardianAwakening();
        awakening.Reset(true, true);
        awakening.TryActivate();
        awakening.TryUseTwinFang();

        awakening.Reset(true, true);

        Assert.IsFalse(awakening.Used);
        Assert.IsFalse(awakening.TwinFangUsed);
        Assert.IsTrue(awakening.Ready);
    }

    [Test]
    public void TwinFang_TargetKilled_OtherAlive_PoisonGoesToOther()
    {
        poisonA.AddStacks(4);
        healthA.TakeDamage(14);

        bite.TryUseOn(enemyA);
        TwinFang.ApplyResidue(poisonA, poisonB);

        Assert.IsTrue(healthA.IsDead);
        Assert.AreEqual(2, poisonB.PoisonStacks);
    }

    [Test]
    public void TwinFang_TargetKilled_NoOtherEnemy_NoPoisonPlaced()
    {
        poisonA.AddStacks(4);
        healthA.TakeDamage(14);

        bite.TryUseOn(enemyA);
        TwinFang.ApplyResidue(poisonA, null);

        Assert.IsTrue(healthA.IsDead);
        Assert.AreEqual(0, poisonA.PoisonStacks);
    }
}