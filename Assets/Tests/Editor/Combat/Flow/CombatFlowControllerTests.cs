using System.Reflection;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Visha.Battle;
using Visha.Combat.Flow;
using Visha.Enemies;
using Visha.UI;

namespace Visha.Combat.Flow.Tests
{
    public class CombatFlowControllerTests
    {
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

        private GameObject playerGO;
        private GameObject enemyGO;
        private GameObject controllerGO;
        private CombatFlowController controller;
        private Health playerHealth;
        private Health enemyHealth;
        private Poison playerPoison;
        private Poison enemyPoison;
        private VenomStrike venomStrike;
        private SerpentsBite serpentsBite;
        private Fade fade;
        private EnemyPatternRunner enemyPatternRunner;
        private BattleOutcomeController outcomeController;

        [SetUp]
        public void Setup()
        {
            playerGO = new GameObject("Player");
            playerHealth = playerGO.AddComponent<Health>();
            playerHealth.SetMaxHealth(50);
            playerPoison = playerGO.AddComponent<Poison>();
            playerPoison.SetValues(5, 2);
            venomStrike = playerGO.AddComponent<VenomStrike>();
            venomStrike.SetStacksApplied(2);
            serpentsBite = playerGO.AddComponent<SerpentsBite>();
            serpentsBite.SetDamagePerStack(4);
            fade = playerGO.AddComponent<Fade>();
            fade.SetReductionPercent(50);

            enemyGO = new GameObject("Enemy");
            enemyHealth = enemyGO.AddComponent<Health>();
            enemyHealth.SetMaxHealth(30);
            enemyPoison = enemyGO.AddComponent<Poison>();
            enemyPoison.SetValues(5, 2);
            enemyPatternRunner = enemyGO.AddComponent<EnemyPatternRunner>();

            controllerGO = new GameObject("CombatFlowController");
            outcomeController = controllerGO.AddComponent<BattleOutcomeController>();
            outcomeController.SetCombatants(playerHealth, enemyHealth);
            controller = controllerGO.AddComponent<CombatFlowController>();

            Set("playerGameObject", playerGO);
            Set("enemyGameObject", enemyGO);
            Set("outcomeController", outcomeController);
            Set("enemyPatternRunner", enemyPatternRunner);

            Call("Awake");
            Call("Start");
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(playerGO);
            Object.DestroyImmediate(enemyGO);
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

        private void Request(BattleAbility ability) => Call("HandleAbilityRequested", ability, 0);

        private void FinishEnemyTurn() => Call("ResolveEnemyTurn");

        private void PlayTurn(BattleAbility ability)
        {
            Request(ability);
            FinishEnemyTurn();
        }

        private BattleViewState View => Get<BattleViewState>("viewState");


        [Test]
        public void VenomStrike_AppliesStacksThroughComponent()
        {
            Request(BattleAbility.VenomStrike);

            Assert.AreEqual(2, enemyPoison.PoisonStacks);
            Assert.AreEqual(BattlePhase.EnemyTurn, controller.CurrentPhase, "a successful ability hands over to the enemy");
        }

        [Test]
        public void AbilityUsed_IsRaisedOnlyAfterAnAbilitySucceeds()
        {
            var used = new List<BattleAbility>();
            controller.OnAbilityUsed += used.Add;

            Request(BattleAbility.SerpentsBite);
            Assert.IsEmpty(used);

            Request(BattleAbility.VenomStrike);
            CollectionAssert.AreEqual(new[] { BattleAbility.VenomStrike }, used);

            FinishEnemyTurn();
            Request(BattleAbility.Fade);
            CollectionAssert.AreEqual(new[] { BattleAbility.VenomStrike, BattleAbility.Fade }, used);

            FinishEnemyTurn();
            Request(BattleAbility.Fade);
            CollectionAssert.AreEqual(new[] { BattleAbility.VenomStrike, BattleAbility.Fade }, used);
        }

        [Test]
        public void SerpentsBite_WithoutPoison_IsRefusedAndKeepsTheTurn()
        {
            Request(BattleAbility.SerpentsBite);

            Assert.AreEqual(30, enemyHealth.CurrentHealth);
            Assert.AreEqual(BattlePhase.PlayerTurn, controller.CurrentPhase, "a refused ability must not use the turn");
            Assert.IsFalse(Get<bool>("turnInProgress"), "the player can choose again");
        }

        [Test]
        public void SerpentsBite_DealsFourPerStackAndConsumesPoison()
        {
            enemyPoison.AddStacks(3);

            Request(BattleAbility.SerpentsBite);

            Assert.AreEqual(30 - 12, enemyHealth.CurrentHealth, "3 stacks x 4");
            Assert.AreEqual(0, enemyPoison.PoisonStacks);
        }

        [Test]
        public void Fade_HalvesTheTelegraphedAttack()
        {
            PlayTurn(BattleAbility.VenomStrike);
            Assert.AreEqual(9, enemyPatternRunner.IncomingDamage, "Heavy Blow is telegraphed next");

            PlayTurn(BattleAbility.Fade);

            Assert.AreEqual(46 - 4, playerHealth.CurrentHealth, "Fade halves Heavy Blow 9 to 4");
            Assert.IsFalse(fade.IsActive, "Fade only lasts for one enemy phase");
        }

        [Test]
        public void Fade_CannotBeUsedOnConsecutiveTurns()
        {
            PlayTurn(BattleAbility.Fade);
            Assert.IsFalse(View.fadeAvailable, "HUD should show Fade unavailable next turn");

            Request(BattleAbility.Fade);
            Assert.AreEqual(BattlePhase.PlayerTurn, controller.CurrentPhase, "second Fade in a row is refused");

            PlayTurn(BattleAbility.VenomStrike);
            Assert.IsTrue(View.fadeAvailable, "another ability makes Fade available again");
            Assert.IsTrue(fade.CanUse);
        }


        [Test]
        public void Poison_TicksAfterTheEnemyActs()
        {
            Request(BattleAbility.VenomStrike);
            Assert.AreEqual(30, enemyHealth.CurrentHealth, "no tick before the enemy has acted");

            FinishEnemyTurn();

            Assert.AreEqual(30 - 4, enemyHealth.CurrentHealth, "2 stacks x 2 after the enemy turn");
            Assert.AreEqual(50 - 4, playerHealth.CurrentHealth, "Strike hit first");
            Assert.AreEqual(BattlePhase.PlayerTurn, controller.CurrentPhase);
        }

        [Test]
        public void PlayerDeath_StopsTheTurnBeforePoisonTicks()
        {
            enemyPoison.AddStacks(2);
            playerHealth.TakeDamage(48);

            Request(BattleAbility.Fade);
            FinishEnemyTurn();

            Assert.IsTrue(playerHealth.IsDead);
            Assert.AreEqual(30, enemyHealth.CurrentHealth, "no poison tick after the player died");
            Assert.AreNotEqual(BattlePhase.PlayerTurn, controller.CurrentPhase);
        }

        [Test]
        public void EnemyKilledByBite_GetsNoEnemyTurn()
        {
            enemyHealth.TakeDamage(26);
            enemyPoison.AddStacks(1);

            Request(BattleAbility.SerpentsBite);

            Assert.IsTrue(enemyHealth.IsDead);
            Assert.AreEqual("Strike", enemyPatternRunner.IntentLabel, "enemy pattern must not advance");
            Assert.AreEqual(50, playerHealth.CurrentHealth);
            Assert.AreNotEqual(BattlePhase.EnemyTurn, controller.CurrentPhase);
        }

        [Test]
        public void SecondSubmission_DuringEnemyTurn_IsIgnored()
        {
            Request(BattleAbility.VenomStrike);
            Request(BattleAbility.VenomStrike);

            Assert.AreEqual(2, enemyPoison.PoisonStacks, "only the first submission resolves");
        }

        [Test]
        public void ResolveEnemyTurn_AfterBattleEnds_DoesNothing()
        {
            Request(BattleAbility.VenomStrike);
            enemyHealth.TakeDamage(1000);

            FinishEnemyTurn();

            Assert.AreEqual(50, playerHealth.CurrentHealth, "a dead enemy must not attack");
        }


        [Test]
        public void Retry_ResetsFadeTurnStateAndCombatants()
        {
            PlayTurn(BattleAbility.VenomStrike);
            Request(BattleAbility.Fade);
            Assert.IsFalse(fade.CanUse);

            outcomeController.Retry();

            Assert.AreEqual(50, playerHealth.CurrentHealth);
            Assert.AreEqual(0, enemyPoison.PoisonStacks);
            Assert.IsTrue(fade.CanUse, "Fade is ready after retry");
            Assert.IsFalse(fade.IsActive);
            Assert.IsFalse(Get<bool>("turnInProgress"));
            Assert.AreEqual(BattlePhase.PlayerTurn, controller.CurrentPhase);
            Assert.AreEqual("Strike", enemyPatternRunner.IntentLabel, "pattern restarts");
        }

        [Test]
        public void Retry_AfterDefeat_AcceptsActionsAgain()
        {
            playerHealth.TakeDamage(1000);
            outcomeController.Retry();

            Request(BattleAbility.VenomStrike);

            Assert.AreEqual(2, enemyPoison.PoisonStacks);
        }

        [Test]
        public void InactiveCombat_IgnoresActionsUntilReactivated()
        {
            controller.SetCombatActive(false);
            Assert.AreEqual(BattlePhase.Waiting, controller.CurrentPhase);

            Request(BattleAbility.VenomStrike);
            Assert.AreEqual(0, enemyPoison.PoisonStacks, "no actions while no encounter is active");

            controller.SetCombatActive(true);
            Assert.AreEqual(BattlePhase.PlayerTurn, controller.CurrentPhase);
            Request(BattleAbility.VenomStrike);
            Assert.AreEqual(2, enemyPoison.PoisonStacks);
        }


        [Test]
        public void ViewState_ReportsRealValues()
        {
            enemyPoison.AddStacks(3);
            playerHealth.TakeDamage(5);

            Assert.AreEqual(45, View.playerHealth);
            Assert.AreEqual(50, View.playerMaxHealth);
            Assert.AreEqual(30, View.enemyMaxHealth);
            Assert.AreEqual(3, View.poison, "enemy stacks, so the HUD can enable Bite");
            Assert.AreEqual(enemyPoison.MaxStacks, View.poisonCap);
            Assert.AreEqual(enemyPatternRunner.EnemyName, View.enemyName);
            Assert.AreEqual("Strike", View.intention);
            Assert.AreEqual(4, View.incomingDamage);
            Assert.IsTrue(View.fadeAvailable);
            Assert.AreEqual(BattlePhase.PlayerTurn, View.phase);
        }

        [Test]
        public void Abilities_AreFoundOnThePlayerWhenUnassigned()
        {
            Assert.AreSame(venomStrike, Get<VenomStrike>("venomStrike"));
            Assert.AreSame(serpentsBite, Get<SerpentsBite>("serpentsBite"));
            Assert.AreSame(fade, Get<Fade>("fade"));
        }
    }
}
