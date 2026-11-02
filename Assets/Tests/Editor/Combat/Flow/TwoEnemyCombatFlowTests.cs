using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Visha.Battle;
using Visha.Enemies;
using Visha.UI;

namespace Visha.Combat.Flow.Tests
{
    /// <summary>
    /// Two-enemy enemy phase: per living enemy, poison ticks then it acts; dead enemies
    /// are skipped; Fade halves both hits and ends once after both; Victory only when
    /// both are down; Defeat and Retry still work. Both enemies are Temple Guardians
    /// (Strike 4, Heavy Blow 9, Recover 0). Player 50 HP, each enemy 30 HP.
    /// </summary>
    public class TwoEnemyCombatFlowTests
    {
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

        private GameObject playerGO, enemyAGO, enemyBGO, controllerGO;
        private CombatFlowController controller;
        private BattleOutcomeController outcome;
        private Health player, enemyA, enemyB;
        private Poison poisonA, poisonB;
        private EnemyPatternRunner patternA, patternB;
        private Fade fade;

        [SetUp]
        public void Setup()
        {
            playerGO = new GameObject("Player");
            player = playerGO.AddComponent<Health>();
            player.SetMaxHealth(50);
            playerGO.AddComponent<Poison>().SetValues(5, 2);
            playerGO.AddComponent<VenomStrike>().SetStacksApplied(2);
            playerGO.AddComponent<SerpentsBite>().SetDamagePerStack(4);
            fade = playerGO.AddComponent<Fade>();
            fade.SetReductionPercent(50);

            enemyAGO = MakeEnemy("EnemyA", out enemyA, out poisonA, out patternA);
            enemyBGO = MakeEnemy("EnemyB", out enemyB, out poisonB, out patternB);

            controllerGO = new GameObject("CombatFlowController");
            outcome = controllerGO.AddComponent<BattleOutcomeController>();
            outcome.SetCombatants(player, enemyA);
            controller = controllerGO.AddComponent<CombatFlowController>();

            Set("playerGameObject", playerGO);
            Set("enemyGameObject", enemyAGO);
            Set("secondEnemyGameObject", enemyBGO);
            Set("outcomeController", outcome);
            Set("enemyPatternRunner", patternA);

            Call("Awake");
            Call("Start");
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(playerGO);
            Object.DestroyImmediate(enemyAGO);
            Object.DestroyImmediate(enemyBGO);
            Object.DestroyImmediate(controllerGO);
        }

        private static GameObject MakeEnemy(string name, out Health health, out Poison poison, out EnemyPatternRunner pattern)
        {
            var go = new GameObject(name);
            health = go.AddComponent<Health>();
            health.SetMaxHealth(30);
            poison = go.AddComponent<Poison>();
            poison.SetValues(5, 2);
            pattern = go.AddComponent<EnemyPatternRunner>();
            return go;
        }

        private void Set(string field, object value)
        {
            FieldInfo info = typeof(CombatFlowController).GetField(field, Private);
            Assert.IsNotNull(info, $"CombatFlowController should have field {field}");
            info.SetValue(controller, value);
        }

        private object Call(string method, params object[] args)
        {
            MethodInfo info = typeof(CombatFlowController).GetMethod(method, Private);
            Assert.IsNotNull(info, $"CombatFlowController should define {method}");
            return info.Invoke(controller, args.Length == 0 ? null : args);
        }

        private void Request(BattleAbility ability) => Call("HandleAbilityRequested", ability, ability == BattleAbility.Fade ? -1 : 0);
        private void FinishEnemyPhase() => Call("ResolveEnemyTurn");
        private void PlayTurn(BattleAbility ability) { Request(ability); FinishEnemyPhase(); }

        [Test]
        public void SecondEnemy_IsDetected()
        {
            Assert.IsTrue(controller.HasSecondEnemy);
        }

        [Test]
        public void EachLivingEnemy_ActsOncePerPhase()
        {
            PlayTurn(BattleAbility.VenomStrike);

            Assert.AreEqual(50 - 4 - 4, player.CurrentHealth, "both Strikes land");
            Assert.AreEqual("Heavy Blow", patternA.IntentLabel, "first enemy advanced once");
            Assert.AreEqual("Heavy Blow", patternB.IntentLabel, "second enemy advanced once");
            Assert.AreEqual(BattlePhase.PlayerTurn, controller.CurrentPhase);
        }

        [Test]
        public void EachEnemysPoison_TicksBeforeThatEnemyActs()
        {
            poisonB.AddStacks(2);
            PlayTurn(BattleAbility.VenomStrike);

            Assert.AreEqual(30 - 4, enemyA.CurrentHealth, "A takes only its own poison");
            Assert.AreEqual(30 - 4, enemyB.CurrentHealth, "B takes only its own poison");
        }

        [Test]
        public void EnemyKilledByItsPoison_DoesNotActAndOtherStillActs()
        {
            enemyB.TakeDamage(26);
            poisonB.AddStacks(2);

            PlayTurn(BattleAbility.VenomStrike);

            Assert.IsTrue(enemyB.IsDead);
            Assert.AreEqual(50 - 4, player.CurrentHealth, "only the first enemy attacks");
            Assert.AreEqual("Strike", patternB.IntentLabel, "dead enemy's pattern does not advance");
            Assert.IsFalse(outcome.IsOver, "one enemy still alive");
            Assert.AreEqual(BattlePhase.PlayerTurn, controller.CurrentPhase);
        }

        [Test]
        public void DefeatedEnemy_IsSkippedInLaterPhases()
        {
            enemyA.TakeDamage(1000);

            PlayTurn(BattleAbility.Fade);

            Assert.AreEqual(50 - 2, player.CurrentHealth, "only the second enemy's Strike, halved");
            Assert.AreEqual("Strike", patternA.IntentLabel, "dead enemy skipped");
            Assert.AreEqual("Heavy Blow", patternB.IntentLabel);
        }

        [Test]
        public void Fade_HalvesBothHits_AndEndsOnceAfterBoth()
        {
            PlayTurn(BattleAbility.VenomStrike);

            PlayTurn(BattleAbility.Fade);

            Assert.AreEqual(42 - 4 - 4, player.CurrentHealth, "both Heavy Blows halved to 4");
            Assert.IsFalse(fade.IsActive, "Fade window closed after the phase");
            Assert.IsFalse(fade.CanUse, "Fade still on cooldown for the next turn");
        }

        [Test]
        public void AwakeningCharge_IsEarnedOncePerPhase()
        {
            controller.Awakening.Reset(true);
            PlayTurn(BattleAbility.VenomStrike);

            PlayTurn(BattleAbility.Fade);

            Assert.AreEqual(1, controller.Awakening.Charges);
        }

        [Test]
        public void Victory_OnlyAfterBothEnemiesAreDefeated()
        {
            enemyA.TakeDamage(1000);
            Assert.IsFalse(outcome.IsOver, "first defeat does not end the battle");
            Assert.AreEqual(BattlePhase.PlayerTurn, controller.CurrentPhase);

            enemyB.TakeDamage(1000);
            Assert.AreEqual(BattleOutcome.Victory, outcome.Outcome);
            Assert.AreEqual(BattlePhase.Victory, controller.CurrentPhase);
        }

        [Test]
        public void PlayerDeath_StopsThePhase_SecondEnemyDoesNotTickOrAct()
        {
            player.TakeDamage(46);
            poisonB.AddStacks(2);

            PlayTurn(BattleAbility.VenomStrike);

            Assert.IsTrue(player.IsDead);
            Assert.AreEqual(BattleOutcome.Defeat, outcome.Outcome);
            Assert.AreEqual(30, enemyB.CurrentHealth, "second enemy's poison never ticked");
            Assert.AreEqual("Strike", patternB.IntentLabel, "second enemy never acted");
        }

        [Test]
        public void Retry_AfterDefeat_RevivesBothEnemiesAndResetsTurn()
        {
            PlayTurn(BattleAbility.VenomStrike);
            poisonB.AddStacks(2);
            player.TakeDamage(1000);

            outcome.Retry();

            Assert.AreEqual(50, player.CurrentHealth);
            Assert.AreEqual(30, enemyA.CurrentHealth);
            Assert.AreEqual(30, enemyB.CurrentHealth);
            Assert.AreEqual(0, poisonA.PoisonStacks);
            Assert.AreEqual(0, poisonB.PoisonStacks);
            Assert.AreEqual("Strike", patternA.IntentLabel);
            Assert.AreEqual("Strike", patternB.IntentLabel);
            Assert.AreEqual(BattlePhase.PlayerTurn, controller.CurrentPhase);

            PlayTurn(BattleAbility.VenomStrike);
            Assert.AreEqual(50 - 8, player.CurrentHealth, "both enemies act again after retry");
        }

        [Test]
        public void Retry_AfterVictory_StartsAFreshTwoEnemyBattle()
        {
            enemyA.TakeDamage(1000);
            enemyB.TakeDamage(1000);

            outcome.Retry();

            Assert.IsFalse(outcome.IsOver);
            Assert.IsFalse(enemyA.IsDead);
            Assert.IsFalse(enemyB.IsDead);
            Assert.AreEqual(BattlePhase.PlayerTurn, controller.CurrentPhase);
        }
    }
}
