using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Visha.Battle;
using Visha.Enemies;
using Visha.UI;

namespace Visha.Campaign.EditorTools
{
    /// <summary>
    /// Editor-only helper that adds the campaign to a scene already wired by
    /// Visha > Setup Combat Flow: creates the three encounter configs (if
    /// missing), a Campaign object with CampaignController, and connects it to
    /// Charith's shared BattleScreens (title, victory, defeat and end-of-demo).
    ///
    /// Safe to re-run: the Campaign object and BattleScreens are reused, and
    /// existing encounter assets are never overwritten.
    /// </summary>
    public static class CampaignSceneSetup
    {
        private const string SourceScenePath = "Assets/Scenes/BattleUI_Demo.unity";
        private const string CampaignScenePath = "Assets/Scenes/Campaign_Demo.unity";
        private const string ScreensPrefabPath = "Assets/Prefabs/BattleScreens.prefab";
        private const string EncounterFolder = "Assets/Data/Encounters";
        private const string CampaignName = "Campaign";
        // Overlay built by earlier versions of this tool; removed on setup.
        private const string LegacyCanvasName = "CampaignCanvas";

        // The three encounters agreed with Anshul (see Docs/EnemyPattern.md).
        private static readonly (string file, string name, EnemyPatternRunner.PatternKind kind, int maxHealth)[] DefaultEncounters =
        {
            ("Encounter1_TempleGuardian", "Temple Guardian", EnemyPatternRunner.PatternKind.TempleGuardian, 30),
            ("Encounter2_GuardianWarden", "Guardian Warden", EnemyPatternRunner.PatternKind.TempleGuardian, 44),
            ("Encounter3_VaultSentinel", "Vault Sentinel", EnemyPatternRunner.PatternKind.VaultSentinel, 46)
        };

        [MenuItem("Visha/Setup Campaign In Open Scene")]
        private static void SetupOpenScene()
        {
            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                EditorUtility.DisplayDialog("Campaign Setup",
                    "You are editing a prefab. Exit Prefab Mode and open the battle scene first.", "OK");
                return;
            }

            string error = Setup(SceneManager.GetActiveScene());
            if (error != null) EditorUtility.DisplayDialog("Campaign Setup", error, "OK");
        }

        [MenuItem("Visha/Create Campaign Demo Scene")]
        private static void CreateCampaignSceneMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string error = CreateCampaignScene();
            if (error != null) EditorUtility.DisplayDialog("Campaign Setup", error, "OK");
        }

        /// <summary>
        /// Copies BattleUI_Demo to Campaign_Demo, adds the campaign and saves it.
        /// Also callable from the command line:
        /// Unity -batchmode -quit -projectPath . -executeMethod Visha.Campaign.EditorTools.CampaignSceneSetup.CreateCampaignSceneBatch
        /// </summary>
        public static string CreateCampaignScene()
        {
            Scene source = EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);
            if (!EditorSceneManager.SaveScene(source, CampaignScenePath, saveAsCopy: true))
                return $"Could not save {CampaignScenePath}.";

            Scene scene = EditorSceneManager.OpenScene(CampaignScenePath, OpenSceneMode.Single);
            string error = Setup(scene);
            if (error != null) return error;

            EditorSceneManager.SaveScene(scene);
            Debug.Log($"Campaign Setup: saved {CampaignScenePath}");
            return null;
        }

        public static void CreateCampaignSceneBatch()
        {
            string error = CreateCampaignScene();
            if (error != null)
            {
                Debug.LogError("Campaign Setup: " + error);
                EditorApplication.Exit(1);
            }
        }

        private static string Setup(Scene scene)
        {
            BattleOutcomeController outcome = Object.FindAnyObjectByType<BattleOutcomeController>(FindObjectsInactive.Include);
            EnemyPatternRunner pattern = Object.FindAnyObjectByType<EnemyPatternRunner>(FindObjectsInactive.Include);
            if (outcome == null || pattern == null)
                return $"'{scene.name}' has no combat flow. Run Visha > Setup Combat Flow In Open Scene first.";

            BattleHUD hud = Object.FindAnyObjectByType<BattleHUD>(FindObjectsInactive.Include);
            if (hud == null) return $"'{scene.name}' has no BattleHUD.";

            SerializedObject outcomeData = new SerializedObject(outcome);
            Health player = outcomeData.FindProperty("playerHealth").objectReferenceValue as Health;
            Health enemy = outcomeData.FindProperty("enemyHealth").objectReferenceValue as Health;
            if (player == null || enemy == null)
                return "BattleOutcomeController is missing its player or enemy Health.";

            Undo.SetCurrentGroupName("Setup Campaign");
            int undoGroup = Undo.GetCurrentGroup();

            List<EncounterConfig> encounters = LoadOrCreateEncounters();

            GameObject campaignObject = FindOrCreateRoot(scene, CampaignName);
            CampaignController campaign = GetOrAdd<CampaignController>(campaignObject);
            SerializedObject campaignData = new SerializedObject(campaign);
            campaignData.FindProperty("outcomeController").objectReferenceValue = outcome;
            campaignData.FindProperty("playerHealth").objectReferenceValue = player;
            campaignData.FindProperty("enemyHealth").objectReferenceValue = enemy;
            campaignData.FindProperty("enemyPattern").objectReferenceValue = pattern;
            campaignData.FindProperty("enemySpriteRenderer").objectReferenceValue = enemy.GetComponentInChildren<SpriteRenderer>();
            SerializedProperty list = campaignData.FindProperty("encounters");
            list.arraySize = encounters.Count;
            for (int i = 0; i < encounters.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = encounters[i];
            campaignData.ApplyModifiedProperties();

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == LegacyCanvasName) Undo.DestroyObjectImmediate(root);
            }

            BattleScreenView view = FindOrCreateScreens(scene);
            if (view == null) return $"Could not load {ScreensPrefabPath}. Run Charith's battle screen setup first.";

            // The campaign drives the screens now. BattleScreenPresenter only knows a
            // single battle and would bind its own Retry over the campaign's buttons.
            foreach (BattleScreenPresenter presenter in Object.FindObjectsByType<BattleScreenPresenter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (presenter.gameObject.scene == scene) Undo.DestroyObjectImmediate(presenter);
            }

            CampaignScreens screens = GetOrAdd<CampaignScreens>(campaignObject);
            SerializedObject screensData = new SerializedObject(screens);
            screensData.FindProperty("campaign").objectReferenceValue = campaign;
            screensData.FindProperty("view").objectReferenceValue = view;
            screensData.FindProperty("hud").objectReferenceValue = hud;
            screensData.ApplyModifiedProperties();

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = campaignObject;
            Debug.Log($"Campaign Setup: wired '{scene.name}' with {encounters.Count} encounters. Save the scene to keep it.");
            return null;
        }

        private static BattleScreenView FindOrCreateScreens(Scene scene)
        {
            foreach (BattleScreenView existing in Object.FindObjectsByType<BattleScreenView>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (existing.gameObject.scene == scene) return existing;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ScreensPrefabPath);
            if (prefab == null) return null;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            Undo.RegisterCreatedObjectUndo(instance, "Add Battle Screens");
            return instance.GetComponent<BattleScreenView>();
        }

        private static List<EncounterConfig> LoadOrCreateEncounters()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Data")) AssetDatabase.CreateFolder("Assets", "Data");
            if (!AssetDatabase.IsValidFolder(EncounterFolder)) AssetDatabase.CreateFolder("Assets/Data", "Encounters");

            var result = new List<EncounterConfig>();
            foreach (var (file, name, kind, maxHealth) in DefaultEncounters)
            {
                string path = $"{EncounterFolder}/{file}.asset";
                EncounterConfig config = AssetDatabase.LoadAssetAtPath<EncounterConfig>(path);
                if (config == null)
                {
                    config = ScriptableObject.CreateInstance<EncounterConfig>();
                    config.Set(name, kind, maxHealth);
                    AssetDatabase.CreateAsset(config, path);
                }
                result.Add(config);
            }
            AssetDatabase.SaveAssets();
            return result;
        }

        private static GameObject FindOrCreateRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name) return root;
            }

            var created = new GameObject(name);
            SceneManager.MoveGameObjectToScene(created, scene);
            Undo.RegisterCreatedObjectUndo(created, $"Create {name}");
            return created;
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            T existing = target.GetComponent<T>();
            return existing != null ? existing : Undo.AddComponent<T>(target);
        }
    }
}
