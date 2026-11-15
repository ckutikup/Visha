using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Visha.Battle;
using Visha.Enemies;
using Visha.UI;

namespace Visha.Combat.Flow.Tests
{
       public class BossCombatFlowTests
    {
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

        private GameObject playerGO, bossGO, controllerGO;
        private CombatFlowController controller;
        private BattleOutcomeController outcome;
        private Health player, boss;
        private Poison bossPoison;
        private EnemyPatternRunner runner;
        private TempleBlessing blessing;
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
            blessing = playerGO.AddComponent<TempleBlessing>(); // not enabled unless a test enables it

            bossGO = new GameObject("Boss");
            boss = bossGO.AddComponent<Health>();
            boss.SetMaxHealth(76);
            bossPoison = bossGO.AddComponent<Poison>();
            bossPoison.SetValues(5, 2);
            runner = bossGO.AddComponent<EnemyPatternRunner>();
            runner.Configure(EnemyPatternRunner.PatternKind.Asura, "The Asura");

            controllerGO = new GameObject("CombatFlowController");
            outcome = controllerGO.AddComponent<BattleOutcomeController>();
            outcome.SetCombatants(player, boss);
            controller = controllerGO.AddComponent<CombatFlowController>();

            Set("playerGameObject", playerGO);
            Set("enemyGameObject", bossGO);
            Set("outcomeController", outcome);
            Set("enemyPatternRunner", runner);

            Call("Awake");
            Call("Start");
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(playerGO);
            Object.DestroyImmediate(bossGO);
            Object.DestroyImmediate(controllerGO);
        }

        private void Set(string field, object value)
        {
            FieldInfo info = typeof(CombatFlowController).GetField(field, Private);
            Assert.IsNotNull(info, $"CombatFlowController should have field {field}");
            info.SetValue(controller, value);
        }

        private T Get<T>(string field) => (T)typeof(CombatFlowController).GetField(field, Private).GetValue(controller);

        private object Call(string method, params object[] args)
        {
            MethodInfo info = typeof(CombatFlowController).GetMethod(method, Private);
            Assert.IsNotNull(info, $"CombatFlowController should define {method}");
            return info.Invoke(controller, args.Length == 0 ? null : args);
        }

        private BattleViewState View => Get<BattleViewState>("viewState");
        private void Request(BattleAbility ability) => Call("HandleAbilityRequested", ability, ability == BattleAbility.Fade ? -1 : 0);
        private void FinishEnemyTurn() => Call("ResolveEnemyTurn");
        private void PlayTurn(BattleAbility ability) { Request(ability); FinishEnemyTurn(); }
        private void BiteIntoPhaseTwo()
        {
            PlayTurn(BattleAbility.Fade);
            boss.TakeDamage(26);
            bossPoison.AddStacks(5);
            PlayTurn(BattleAbility.SerpentsBite);
        }

        // ---------------------------------------------------------------- HUD values

        [Test]
        public void ViewState_ReportsBossPhase()
        {
            Assert.AreEqual(1, View.bossPhase);
            Assert.AreEqual(2, View.bossPhaseCount);
            Assert.AreEqual("The Asura", View.enemyName);
            Assert.AreEqual("Recover", View.intention);
        }

        [Test]
        public void ViewState_BossPhaseIsZeroForNormalEnemies()
        {
            runner.Configure(EnemyPatternRunner.PatternKind.TempleGuardian);
            boss.TakeDamage(60); // well below half

            PlayTurn(BattleAbility.VenomStrike);

            Assert.AreEqual(0, View.bossPhase);
            Assert.AreEqual(0, View.bossPhaseCount);
            Assert.AreEqual("Heavy Blow", View.intention, "a normal enemy's pattern just advances");
        }


        [Test]
        public void BiteAcrossTheThreshold_StartsPhaseTwoBeforeTheBossActs()
        {
            PlayTurn(BattleAbility.Fade);
            Assert.AreEqual("Cursed Claw", runner.IntentLabel);
            boss.TakeDamage(26);
            bossPoison.AddStacks(5);

            Request(BattleAbility.SerpentsBite);

            Assert.AreEqual(2, runner.BossPhase);
            Assert.AreEqual(2, View.bossPhase);
            Assert.AreEqual("Roar", View.intention);
            Assert.AreEqual(0, View.incomingDamage);

            FinishEnemyTurn();

            Assert.AreEqual(50, player.CurrentHealth, "the boss Roars once and nothing else lands");
            Assert.AreEqual("Cleave", runner.IntentLabel, "the pattern advanced exactly once");
            Assert.AreEqual(BattlePhase.PlayerTurn, controller.CurrentPhase);
            Assert.IsFalse(Get<bool>("turnInProgress"));
        }

        [Test]
        public void PoisonTickAcrossTheThreshold_StartsPhaseTwo_AndPoisonStays()
        {
            PlayTurn(BattleAbility.VenomStrike);
            boss.TakeDamage(32);
            Request(BattleAbility.Fade);
            Assert.AreEqual(1, runner.BossPhase, "40 HP is still phase 1");
            Assert.AreEqual("Cursed Claw", runner.IntentLabel);

            FinishEnemyTurn();

            Assert.AreEqual(36, boss.CurrentHealth);
            Assert.AreEqual(2, runner.BossPhase);
            Assert.AreEqual(50, player.CurrentHealth, "Roar replaced Cursed Claw; the boss acted once");
            Assert.AreEqual("Cleave", runner.IntentLabel);
            Assert.AreEqual(2, bossPoison.PoisonStacks);
            Assert.IsFalse(fade.IsActive);
        }

        [Test]
        public void PhaseTwo_StartsOnce_ThenItsPatternRunsNormally()
        {
            BiteIntoPhaseTwo();

            PlayTurn(BattleAbility.VenomStrike);
            Assert.AreEqual(38, player.CurrentHealth);
            Assert.AreEqual("Hex", runner.IntentLabel, "more damage does not restart the phase");

            PlayTurn(BattleAbility.Fade);
            Assert.AreEqual(34, player.CurrentHealth);
            Assert.AreEqual("Cataclysm", runner.IntentLabel);
            Assert.AreEqual(2, View.bossPhase);
        }

        [Test]
        public void BossKilledByBite_IsVictory_WithNoPhaseChangeOrBossAction()
        {
            PlayTurn(BattleAbility.Fade);
            boss.TakeDamage(56);
            bossPoison.AddStacks(5);

            Request(BattleAbility.SerpentsBite);
            FinishEnemyTurn();

            Assert.AreEqual(BattleOutcome.Victory, outcome.Outcome);
            Assert.AreEqual(BattlePhase.Victory, controller.CurrentPhase);
            Assert.AreEqual(50, player.CurrentHealth);
            Assert.AreEqual(1, runner.BossPhase, "a dead boss does not change phase");
            Assert.AreEqual("Cursed Claw", runner.IntentLabel, "and its pattern does not advance");
        }

        [Test]
        public void BossKilledByPoison_IsVictory_WithNoBossAction()
        {
            BiteIntoPhaseTwo();
            boss.TakeDamage(26);
            bossPoison.AddStacks(2);

            PlayTurn(BattleAbility.Fade);

            Assert.IsTrue(boss.IsDead);
            Assert.AreEqual(BattleOutcome.Victory, outcome.Outcome);
            Assert.AreEqual(50, player.CurrentHealth, "Cleave never lands");
            Assert.AreEqual("Cleave", runner.IntentLabel);
        }

        [Test]
        public void PlayerKilledInPhaseTwo_IsDefeat_AndNothingRunsAfterwards()
        {
            BiteIntoPhaseTwo();
            player.TakeDamage(40);

            PlayTurn(BattleAbility.VenomStrike);

            Assert.AreEqual(BattleOutcome.Defeat, outcome.Outcome);
            Assert.AreEqual(BattlePhase.Defeat, controller.CurrentPhase);
            Assert.AreEqual("Cleave", runner.IntentLabel, "no advance after death");
            int bossHealthAtDeath = boss.CurrentHealth;

            Request(BattleAbility.SerpentsBite);
            FinishEnemyTurn();

            Assert.AreEqual(bossHealthAtDeath, boss.CurrentHealth, "no action or poison after the battle ends");
            Assert.AreEqual(2, bossPoison.PoisonStacks);
        }

        [Test]
        public void Retry_AfterDefeatInPhaseTwo_ResetsTheBoss()
        {
            BiteIntoPhaseTwo();
            bossPoison.AddStacks(3);
            player.TakeDamage(1000);
            Assert.AreEqual(BattleOutcome.Defeat, outcome.Outcome);

            outcome.Retry();

            Assert.AreEqual(1, runner.BossPhase);
            Assert.AreEqual(1, View.bossPhase);
            Assert.AreEqual("Recover", View.intention);
            Assert.AreEqual(76, boss.CurrentHealth);
            Assert.AreEqual(0, bossPoison.PoisonStacks);
            Assert.AreEqual(50, player.CurrentHealth);
            Assert.IsTrue(fade.CanUse);
            Assert.IsFalse(Get<bool>("turnInProgress"));
            Assert.AreEqual(BattlePhase.PlayerTurn, controller.CurrentPhase);
        }

        [Test]
        public void Retry_ThenPhaseTwoCanBeReachedAgain()
        {
            BiteIntoPhaseTwo();
            player.TakeDamage(1000);
            outcome.Retry();

            BiteIntoPhaseTwo();

            Assert.AreEqual(2, runner.BossPhase);
            Assert.AreEqual(50, player.CurrentHealth);
            Assert.AreEqual("Cleave", runner.IntentLabel);
        }

        [Test]
        public void Retry_AfterVictory_StartsInPhaseOne()
        {
            BiteIntoPhaseTwo();
            boss.TakeDamage(1000);
            Assert.AreEqual(BattleOutcome.Victory, outcome.Outcome);

            outcome.Retry();

            Assert.IsFalse(outcome.IsOver);
            Assert.AreEqual(1, View.bossPhase);
            Assert.AreEqual("Recover", runner.IntentLabel);
            Assert.AreEqual(BattlePhase.PlayerTurn, controller.CurrentPhase);
        }

        [Test]
        public void Blessing_IsUnavailableUntilEnabled()
        {
            PlayTurn(BattleAbility.Fade);
            Assert.AreEqual(BlessingStatus.Unavailable, View.blessing);
        }

        [Test]
        public void Blessing_ShowsReadyThenSpent_AndTheFightContinuesUntilARealDeath()
        {
            blessing.EnableForEncounter();
            PlayTurn(BattleAbility.VenomStrike);
            Assert.AreEqual(BlessingStatus.Ready, View.blessing);

            player.TakeDamage(45);
            PlayTurn(BattleAbility.VenomStrike);

            Assert.AreEqual(1, player.CurrentHealth);
            Assert.AreEqual(BlessingStatus.Spent, View.blessing);
            Assert.IsFalse(outcome.IsOver);
            Assert.AreEqual(BattlePhase.PlayerTurn, controller.CurrentPhase, "the player gets their next turn");
            Assert.AreEqual("Ruin", runner.IntentLabel, "the boss acted once");

            PlayTurn(BattleAbility.Fade);
            Assert.AreEqual(BattleOutcome.Defeat, outcome.Outcome);
        }

        [Test]
        public void Retry_RestoresASpentBlessing()
        {
            blessing.EnableForEncounter();
            PlayTurn(BattleAbility.VenomStrike);
            player.TakeDamage(45);
            PlayTurn(BattleAbility.VenomStrike);
            player.TakeDamage(1000);

            outcome.Retry();

            Assert.AreEqual(BlessingStatus.Ready, View.blessing);
            Assert.AreEqual(50, player.CurrentHealth);
        }
    }
}
