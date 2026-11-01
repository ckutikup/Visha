using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Visha.Battle;
using Visha.Enemies;
using Visha.UI;

namespace Visha.Combat.Flow.Tests
{
    /// <summary>
    /// The flow starts with one enemy (secondEnemyGameObject left empty, as in
    /// Campaign_Demo) and gets the second at runtime through SetSecondEnemy, as
    /// VaultCampaignLink does for the Vault. Abilities reach the selected target,
    /// the HUD receives both enemies, and SetSecondEnemy(null) restores one enemy.
    /// Both enemies are Temple Guardians (Strike 4); player 50 HP, each enemy 30 HP.
    /// </summary>
    public class SecondEnemySwitchFlowTests
    {
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

        private GameObject playerGO, enemyAGO, enemyBGO, controllerGO;
        private CombatFlowController controller;
        private BattleOutcomeController outcome;
        private Health player, enemyA, enemyB;
        private Poison poisonA, poisonB;
        private BattleViewState lastState;

        [SetUp]
        public void Setup()
        {
            playerGO = new GameObject("Player");
            player = playerGO.AddComponent<Health>();
            player.SetMaxHealth(50);
            playerGO.AddComponent<Poison>().SetValues(5, 2);
            playerGO.AddComponent<VenomStrike>().SetStacksApplied(2);
            playerGO.AddComponent<SerpentsBite>().SetDamagePerStack(4);
            playerGO.AddComponent<Fade>().SetReductionPercent(50);

            enemyAGO = MakeEnemy("EnemyA", out enemyA, out poisonA);
            enemyBGO = MakeEnemy("EnemyB", out enemyB, out poisonB);

            controllerGO = new GameObject("CombatFlowController");
            outcome = controllerGO.AddComponent<BattleOutcomeController>();
            outcome.SetCombatants(player, enemyA);
            controller = controllerGO.AddComponent<CombatFlowController>();

            Set("playerGameObject", playerGO);
            Set("enemyGameObject", enemyAGO);
            Set("outcomeController", outcome);
            Set("enemyPatternRunner", enemyAGO.GetComponent<EnemyPatternRunner>());

            Call("Awake");
            Call("Start");
            controller.OnStateUpdated += state => lastState = state;
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(playerGO);
            Object.DestroyImmediate(enemyAGO);
            Object.DestroyImmediate(enemyBGO);
            Object.DestroyImmediate(controllerGO);
        }

        private static GameObject MakeEnemy(string name, out Health health, out Poison poison)
        {
            var go = new GameObject(name);
            health = go.AddComponent<Health>();
            health.SetMaxHealth(30);
            poison = go.AddComponent<Poison>();
            poison.SetValues(5, 2);
            go.AddComponent<EnemyPatternRunner>();
            return go;
        }

        private void Set(string field, object value)
        {
            FieldInfo info = typeof(CombatFlowController).GetField(field, Private);
            Assert.IsNotNull(info, $"CombatFlowController should have field {field}");
            info.SetValue(controller, value);
        }

        private void Call(string method, params object[] args)
        {
            MethodInfo info = typeof(CombatFlowController).GetMethod(method, Private);
            Assert.IsNotNull(info, $"CombatFlowController should define {method}");
            info.Invoke(controller, args.Length == 0 ? null : args);
        }

        private void PlayTurn(BattleAbility ability, int target)
        {
            Call("HandleAbilityRequested", ability, ability == BattleAbility.Fade ? -1 : target);
            Call("ResolveEnemyTurn");
        }

        [Test]
        public void WithoutSecondEnemy_FlowIsOneEnemy_AndIgnoresTargetOne()
        {
            Assert.IsFalse(controller.HasSecondEnemy);

            Call("HandleAbilityRequested", BattleAbility.VenomStrike, 1);

            Assert.AreEqual(0, poisonA.PoisonStacks, "target 1 does not exist in a one-enemy fight");
            Assert.AreEqual(BattlePhase.PlayerTurn, controller.CurrentPhase, "an ignored request takes no turn");
        }

        [Test]
        public void SetSecondEnemy_SwitchesToTwoEnemies_AndBothAct()
        {
            controller.SetSecondEnemy(enemyBGO);
            Assert.IsTrue(controller.HasSecondEnemy);

            PlayTurn(BattleAbility.VenomStrike, 0);

            Assert.AreEqual(50 - 4 - 4, player.CurrentHealth, "both Strikes land");
        }

        [Test]
        public void VenomAndBite_ReachOnlyTheSelectedTarget()
        {
            controller.SetSecondEnemy(enemyBGO);

            PlayTurn(BattleAbility.VenomStrike, 1);
            Assert.AreEqual(0, poisonA.PoisonStacks);
            Assert.AreEqual(2, poisonB.PoisonStacks);

            int stacks = poisonB.PoisonStacks;
            Assert.Greater(stacks, 0, "B keeps poison after its tick");
            int before = enemyB.CurrentHealth;
            Call("HandleAbilityRequested", BattleAbility.SerpentsBite, 1);
            Assert.AreEqual(before - stacks * 4, enemyB.CurrentHealth);
            Assert.AreEqual(0, poisonB.PoisonStacks);
            Assert.AreEqual(30, enemyA.CurrentHealth, "the other enemy is untouched");
        }

        [Test]
        public void DefeatedEnemy_CannotBeTargeted()
        {
            controller.SetSecondEnemy(enemyBGO);
            enemyB.TakeDamage(1000);

            Call("HandleAbilityRequested", BattleAbility.VenomStrike, 1);

            Assert.AreEqual(0, poisonB.PoisonStacks);
            Assert.AreEqual(BattlePhase.PlayerTurn, controller.CurrentPhase, "the rejected request takes no turn");
        }

        [Test]
        public void Victory_NeedsBothEnemies_AfterTheSwitch()
        {
            controller.SetSecondEnemy(enemyBGO);

            enemyA.TakeDamage(1000);
            Assert.IsFalse(outcome.IsOver);

            enemyB.TakeDamage(1000);
            Assert.AreEqual(BattleOutcome.Victory, outcome.Outcome);
        }

        [Test]
        public void Hud_ReceivesBothEnemies_OnlyWithTwoEnemies()
        {
            controller.SetSecondEnemy(enemyBGO);
            poisonB.AddStacks(2);

            Assert.IsNotNull(lastState.enemies);
            Assert.AreEqual(2, lastState.enemies.Length);
            Assert.AreEqual(0, lastState.enemies[0].targetId);
            Assert.AreEqual(1, lastState.enemies[1].targetId);
            Assert.AreEqual(2, lastState.enemies[1].poison);
            Assert.AreEqual(0, lastState.enemies[0].poison, "poison is shown per enemy");

            controller.SetSecondEnemy(null);
            Assert.IsNull(lastState.enemies, "one enemy keeps the HUD's single-enemy layout");
        }

        [Test]
        public void SetSecondEnemyNull_ReturnsToOneEnemy()
        {
            controller.SetSecondEnemy(enemyBGO);
            controller.SetSecondEnemy(null);

            Assert.IsFalse(controller.HasSecondEnemy);
            PlayTurn(BattleAbility.VenomStrike, 0);
            Assert.AreEqual(50 - 4, player.CurrentHealth, "only the first enemy acts");

            enemyA.TakeDamage(1000);
            Assert.AreEqual(BattleOutcome.Victory, outcome.Outcome);
        }
    }
}
