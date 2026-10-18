using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Visha.Campaign;
using Visha.Combat.Flow;

namespace Visha.Combat.Flow.Tests
{
    public class CombatFlowSceneWiringTests
    {
        private const string BattleScene = "Assets/Scenes/BattleUI_Demo.unity";
        private const string CampaignScene = "Assets/Scenes/Campaign_Demo.unity";

        private static void WithScene(string path, System.Action<Scene> check)
        {
            Scene scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try { check(scene); }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }

        private static T[] All<T>(Scene scene) where T : Component =>
            scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

        private static bool HasComponentNamed(Scene scene, string typeName) =>
            All<MonoBehaviour>(scene).Any(c => c != null && c.GetType().Name == typeName);

        private static void AssertCombatWired(Scene scene)
        {
            GameObject player = scene.GetRootGameObjects().First(go => go.name == "Player");
            Assert.IsNotNull(player.GetComponent<VenomStrike>(), $"{scene.name}: Player needs VenomStrike");
            Assert.IsNotNull(player.GetComponent<SerpentsBite>(), $"{scene.name}: Player needs SerpentsBite");
            Assert.IsNotNull(player.GetComponent<Fade>(), $"{scene.name}: Player needs Fade");
            Assert.AreEqual(50, player.GetComponent<Health>().MaxHealth, $"{scene.name}: player HP is 50 (contract v2)");

            CombatFlowController[] controllers = All<CombatFlowController>(scene);
            Assert.AreEqual(1, controllers.Length, $"{scene.name}: exactly one CombatFlowController");
            var data = new SerializedObject(controllers[0]);
            Assert.AreSame(player.GetComponent<SerpentsBite>(), data.FindProperty("serpentsBite").objectReferenceValue);
            Assert.AreSame(player.GetComponent<Fade>(), data.FindProperty("fade").objectReferenceValue);
        }

        [Test]
        public void BattleDemo_IsASingleBattleWithAllAbilities()
        {
            WithScene(BattleScene, scene =>
            {
                AssertCombatWired(scene);
                Assert.IsEmpty(All<CampaignController>(scene), "BattleUI_Demo must not contain a campaign");
                Assert.IsTrue(HasComponentNamed(scene, "BattleScreenPresenter"),
                    "BattleUI_Demo keeps BattleScreenPresenter for single-battle Retry");
            });
        }

        [Test]
        public void CampaignDemo_HasAllAbilitiesAndIsGatedByTheCampaign()
        {
            WithScene(CampaignScene, scene =>
            {
                AssertCombatWired(scene);
                CampaignController[] campaigns = All<CampaignController>(scene);
                Assert.AreEqual(1, campaigns.Length);
                var data = new SerializedObject(All<CombatFlowController>(scene)[0]);
                Assert.AreSame(campaigns[0], data.FindProperty("campaign").objectReferenceValue,
                    "combat should follow this scene's campaign state");
                Assert.IsFalse(HasComponentNamed(scene, "BattleScreenPresenter"));
            });
        }
    }
}
