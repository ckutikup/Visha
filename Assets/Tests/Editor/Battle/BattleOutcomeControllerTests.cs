using NUnit.Framework;
using UnityEngine;
using Visha.Battle;
using Visha.Enemies;

public class BattleOutcomeControllerTests
{
    private GameObject playerObject;
    private GameObject enemyObject;
    private GameObject controllerObject;
    private Health playerHealth;
    private Health enemyHealth;
    private BattleOutcomeController controller;

    [SetUp]
    public void SetUp()
    {
        playerObject = new GameObject("Player");
        playerHealth = playerObject.AddComponent<Health>();
        playerHealth.SetMaxHealth(30);

        enemyObject = new GameObject("Enemy");
        enemyHealth = enemyObject.AddComponent<Health>();
        enemyHealth.SetMaxHealth(24);
        enemyObject.AddComponent<EnemyPatternRunner>();

        controllerObject = new GameObject("BattleOutcomeController");
        controller = controllerObject.AddComponent<BattleOutcomeController>();
        controller.SetCombatants(playerHealth, enemyHealth);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(controllerObject);
        Object.DestroyImmediate(playerObject);
        Object.DestroyImmediate(enemyObject);
    }

    [Test]
    public void EnemyDeath_WhilePlayerAlive_IsVictory()
    {
        BattleOutcome? reported = null;
        controller.OnBattleEnded += outcome => reported = outcome;

        enemyHealth.TakeDamage(999);

        Assert.AreEqual(BattleOutcome.Victory, controller.Outcome);
        Assert.AreEqual(BattleOutcome.Victory, reported);
        Assert.IsTrue(controller.IsOver);
    }

    [Test]
    public void PlayerDeath_IsDefeat()
    {
        BattleOutcome? reported = null;
        controller.OnBattleEnded += outcome => reported = outcome;

        playerHealth.TakeDamage(999);

        Assert.AreEqual(BattleOutcome.Defeat, controller.Outcome);
        Assert.AreEqual(BattleOutcome.Defeat, reported);
    }

    [Test]
    public void SimultaneousDeath_FavorsDefeatOverVictory()
    {
        // Player already down when the enemy's death is processed.
        playerHealth.TakeDamage(999);
        enemyHealth.TakeDamage(999);

        Assert.AreEqual(BattleOutcome.Defeat, controller.Outcome);
    }

    [Test]
    public void OutcomeIsLocked_FurtherDeathsDoNotOverrideIt()
    {
        int endedCount = 0;
        controller.OnBattleEnded += _ => endedCount++;

        enemyHealth.TakeDamage(999); // Victory
        playerHealth.TakeDamage(999); // Should not flip the result to Defeat

        Assert.AreEqual(BattleOutcome.Victory, controller.Outcome);
        Assert.AreEqual(1, endedCount);
    }

    [Test]
    public void Retry_RevivesBothCombatantsAndClearsOutcome()
    {
        enemyHealth.TakeDamage(999);
        Assert.IsTrue(controller.IsOver);

        controller.Retry();

        Assert.AreEqual(BattleOutcome.None, controller.Outcome);
        Assert.IsFalse(controller.IsOver);
        Assert.IsFalse(playerHealth.IsDead);
        Assert.IsFalse(enemyHealth.IsDead);
        Assert.AreEqual(playerHealth.MaxHealth, playerHealth.CurrentHealth);
        Assert.AreEqual(enemyHealth.MaxHealth, enemyHealth.CurrentHealth);
    }

    [Test]
    public void Retry_ClearsPoisonStacks()
    {
        Poison poison = enemyObject.AddComponent<Poison>();
        poison.SetValues(5, 2); // Awake doesn't run in EditMode, so wire Poison up explicitly.
        poison.AddStacks(3);
        Assert.AreEqual(3, poison.PoisonStacks);

        controller.Retry();

        Assert.AreEqual(0, poison.PoisonStacks);
    }

    [Test]
    public void Retry_ResetsEnemyAttackPattern()
    {
        EnemyPatternRunner pattern = enemyObject.GetComponent<EnemyPatternRunner>();
        pattern.TakeTurn(); // advance off the first move

        controller.Retry();

        Assert.AreEqual("Strike", pattern.NextAction.Label);
    }

    [Test]
    public void Retry_FiresOnRetry()
    {
        bool retried = false;
        controller.OnRetry += () => retried = true;

        controller.Retry();

        Assert.IsTrue(retried);
    }

    [Test]
    public void DisabledController_DoesNotReactToDeath()
    {
        controller.enabled = false;

        enemyHealth.TakeDamage(999);

        Assert.AreEqual(BattleOutcome.None, controller.Outcome);
    }
}
