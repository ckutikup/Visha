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
        private GameObject playerGO;
        private GameObject enemyGO;
        private GameObject controllerGO;
        private CombatFlowController controller;
        private Health playerHealth;
        private Health enemyHealth;
        private Poison playerPoison;
        private Poison enemyPoison;
        private EnemyPatternRunner enemyPatternRunner;
        private BattleOutcomeController outcomeController;
        private BattleHUD battleHUD;

        [SetUp]
        public void Setup()
        {
            // Create player
            playerGO = new GameObject("Player");
            playerHealth = playerGO.AddComponent<Health>();
            playerHealth.SetMaxHealth(30);
            playerPoison = playerGO.AddComponent<Poison>();
            playerPoison.SetValues(5, 2);

            // Create enemy
            enemyGO = new GameObject("Enemy");
            enemyHealth = enemyGO.AddComponent<Health>();
            enemyHealth.SetMaxHealth(24);
            enemyPoison = enemyGO.AddComponent<Poison>();
            enemyPoison.SetValues(5, 2);
            enemyPatternRunner = enemyGO.AddComponent<EnemyPatternRunner>();

            // Create controller
            controllerGO = new GameObject("CombatFlowController");
            controller = controllerGO.AddComponent<CombatFlowController>();

            // Initialize battle outcome controller before wiring references into the controller
            outcomeController = controllerGO.AddComponent<BattleOutcomeController>();
            outcomeController.SetCombatants(playerHealth, enemyHealth);

            // Manually assign references (since we can't use SerializeField in tests)
            SetControllerReferences();
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(playerGO);
            Object.DestroyImmediate(enemyGO);
            Object.DestroyImmediate(controllerGO);
        }

        private void CallPrivate(string methodName)
        {
            var method = typeof(CombatFlowController).GetMethod(methodName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(method, $"CombatFlowController should define {methodName}");
            method.Invoke(controller, null);
        }

        private void SetControllerReferences()
        {
            var playerField = controller.GetType().GetField("playerGameObject",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var enemyField = controller.GetType().GetField("enemyGameObject",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var outcomeField = controller.GetType().GetField("outcomeController",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var enemyPatternField = controller.GetType().GetField("enemyPatternRunner",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            playerField?.SetValue(controller, playerGO);
            enemyField?.SetValue(controller, enemyGO);
            outcomeField?.SetValue(controller, outcomeController);
            enemyPatternField?.SetValue(controller, enemyPatternRunner);
        }

        [Test]
        public void VenomStrike_AppliesPoisonToEnemy()
        {
            int initialPoison = enemyPoison.PoisonStacks;
            var venomStrike = playerGO.AddComponent<VenomStrike>();
            venomStrike.SetStacksApplied(2);

            SetControllerReferences();
            var venomField = controller.GetType().GetField("venomStrike",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            venomField?.SetValue(controller, venomStrike);

            CallPrivate("Awake");
            CallPrivate("Start");

            var executeMethod = controller.GetType().GetMethod("UseVenomStrike",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = executeMethod?.Invoke(controller, new object[] { 0 });

            Assert.IsTrue((bool)result, "Venom Strike should succeed");
            Assert.AreEqual(initialPoison + 2, enemyPoison.PoisonStacks,
                "Enemy should have 2 more poison stacks");
        }

        [Test]
        public void SerpentsBite_RequiresPoisonToUse()
        {
            CallPrivate("Awake");
            CallPrivate("Start");
            enemyPoison.ConsumeAllStacks();

            var executeMethod = controller.GetType().GetMethod("UseSerpentsBite",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = executeMethod?.Invoke(controller, new object[] { 0 });

            Assert.IsFalse((bool)result, "Serpent's Bite should fail without poison");
            Assert.AreEqual(24, enemyHealth.CurrentHealth, "Enemy should take no damage");
        }

        [Test]
        public void SerpentsBite_DealsDamageAndConsumesPoison()
        {
            CallPrivate("Awake");
            CallPrivate("Start");
            enemyPoison.AddStacks(3);
            int initialHealth = enemyHealth.CurrentHealth;

            var executeMethod = controller.GetType().GetMethod("UseSerpentsBite",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = executeMethod?.Invoke(controller, new object[] { 0 });

            Assert.IsTrue((bool)result, "Serpent's Bite should succeed with poison");
            Assert.AreEqual(0, enemyPoison.PoisonStacks, "Enemy poison should be consumed");
            Assert.Less(enemyHealth.CurrentHealth, initialHealth, "Enemy should take damage");
        }

        [Test]
        public void Fade_ActivatesShield()
        {
            CallPrivate("Awake");
            CallPrivate("Start");

            var executeMethod = controller.GetType().GetMethod("UseFade",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var result = executeMethod?.Invoke(controller, new object[] { });

            Assert.IsTrue((bool)result, "Fade should succeed");
        }


        [Test]
        public void InitializeViewState_CreatesValidSnapshot()
        {
            CallPrivate("Awake");

            var viewStateField = controller.GetType().GetField("viewState",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var viewState = (BattleViewState)viewStateField?.GetValue(controller);

            Assert.IsNotNull(viewState, "ViewState should be initialized");
            Assert.AreEqual(30, viewState.playerMaxHealth, "Player max health should be 30");
            Assert.AreEqual(24, viewState.enemyMaxHealth, "Enemy max health should be 24");
            Assert.AreEqual(BattlePhase.Waiting, viewState.phase, "Initial phase should be Waiting");
        }

        [Test]
        public void UpdateViewState_ReflectsCurrentHealth()
        {
            CallPrivate("Awake");
            CallPrivate("Start");
            playerHealth.TakeDamage(5);

            var updateMethod = controller.GetType().GetMethod("UpdateViewState",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            updateMethod?.Invoke(controller, null);

            var viewStateField = controller.GetType().GetField("viewState",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var viewState = (BattleViewState)viewStateField?.GetValue(controller);

            Assert.AreEqual(25, viewState.playerHealth, "ViewState should reflect damage taken");
        }

        [Test]
        public void UpdateViewState_ShowsEnemyPoisonStacks()
        {
            CallPrivate("Awake");
            CallPrivate("Start");
            enemyPoison.AddStacks(3);

            var updateMethod = controller.GetType().GetMethod("UpdateViewState",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            updateMethod?.Invoke(controller, null);

            var viewStateField = controller.GetType().GetField("viewState",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var viewState = (BattleViewState)viewStateField?.GetValue(controller);

            Assert.AreEqual(3, viewState.poison,
                "HUD poison should show the enemy's stacks so Serpent's Bite can be enabled");
            Assert.AreEqual(enemyPoison.MaxStacks, viewState.poisonCap,
                "HUD poison cap should come from the enemy's Poison component");
        }


        [Test]
        public void EnterPlayerTurn_SetsCorrectPhase()
        {
            CallPrivate("Awake");

            var enterPlayerMethod = controller.GetType().GetMethod("EnterPlayerTurn",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            enterPlayerMethod?.Invoke(controller, null);

            var currentPhaseField = controller.GetType().GetField("currentPhase",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var phase = (BattlePhase)currentPhaseField?.GetValue(controller);

            Assert.AreEqual(BattlePhase.PlayerTurn, phase, "Should enter PlayerTurn phase");
        }

        [Test]
        public void EnterEnemyTurn_SetsCorrectPhase()
        {
            CallPrivate("Awake");
            CallPrivate("Start");

            var enterEnemyMethod = controller.GetType().GetMethod("EnterEnemyTurn",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            enterEnemyMethod?.Invoke(controller, null);

            var currentPhaseField = controller.GetType().GetField("currentPhase",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var phase = (BattlePhase)currentPhaseField?.GetValue(controller);

            Assert.AreEqual(BattlePhase.EnemyTurn, phase, "Should enter EnemyTurn phase");
        }


        [Test]
        public void ComponentInitialization_FindsAllRequiredComponents()
        {
            CallPrivate("Awake");

            var playerHealthField = controller.GetType().GetField("playerHealth",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var enemyHealthField = controller.GetType().GetField("enemyHealth",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            Assert.IsNotNull(playerHealthField?.GetValue(controller), "Player health should be found");
            Assert.IsNotNull(enemyHealthField?.GetValue(controller), "Enemy health should be found");
        }

        [Test]
        public void BattleOutcomeIntegration_ReadsOutcomeCorrectly()
        {
            CallPrivate("Awake");
            CallPrivate("Start");

            playerHealth.TakeDamage(1000);

            Assert.IsTrue(outcomeController.IsOver, "Battle should be over");
            Assert.AreEqual(BattleOutcome.Defeat, outcomeController.Outcome,
                "Outcome should be Defeat");
        }

        [Test]
        public void Retry_ResetsGameState()
        {
            CallPrivate("Awake");
            CallPrivate("Start");
            int initialHealth = playerHealth.CurrentHealth;
            playerHealth.TakeDamage(10);

            outcomeController.Retry();

            Assert.AreEqual(initialHealth, playerHealth.CurrentHealth,
                "Player health should be reset");
            Assert.AreEqual(0, playerPoison.PoisonStacks,
                "Player poison should be cleared");
            Assert.AreEqual(0, enemyPoison.PoisonStacks,
                "Enemy poison should be cleared");
        }
    }
}