using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Visha.Battle;
using Visha.Enemies;

namespace Visha.Campaign.EditorTools
{
    /// <summary>
    /// Editor-only helper that adds the two-enemy Vault to a scene already set up by
    /// Visha > Setup Campaign In Open Scene. It only adds: the Vault encounter at the
    /// end of the campaign's list, an inactive SecondEnemy (Health, Poison, pattern
    /// runner), and a VaultCampaignLink on the Campaign object. Existing wiring is
    /// left alone, including the combat flow's second-enemy slot.
    ///
    /// Safe to re-run. Command line:
    /// Unity -batchmode -quit -projectPath . -executeMethod Visha.Campaign.EditorTools.VaultCampaignSceneSetup.AddToCampaignSceneBatch
    /// </summary>
    public static class VaultCampaignSceneSetup
    {
        private const string CampaignScenePath = "Assets/Scenes/Campaign_Demo.unity";
        private const string VaultConfigPath = "Assets/Data/Encounters/Encounter4_InnerVault.asset";
        // Created by Anshul's Visha/Setup/Create Vault Encounter Asset menu.
        private const string LineupPath = "Assets/Data/Encounters/VaultEncounter_GuardianAndSentinel.asset";
        private const string SecondEnemyName = "SecondEnemy";

        [MenuItem("Visha/Add Vault Encounter To Open Scene")]
        private static void AddToOpenScene()
        {
            string error = Add(SceneManager.GetActiveScene());
            if (error != null) EditorUtility.DisplayDialog("Vault Setup", error, "OK");
        }

        public static void AddToCampaignSceneBatch()
        {
            Scene scene = EditorSceneManager.OpenScene(CampaignScenePath, OpenSceneMode.Single);
            string error = Add(scene);
            if (error == null && !EditorSceneManager.SaveScene(scene)) error = $"Could not save {CampaignScenePath}.";
            if (error == null) return;
            Debug.LogError("Vault Setup: " + error);
            EditorApplication.Exit(1);
        }

        private static string Add(Scene scene)
        {
            CampaignController campaign = FindInScene<CampaignController>(scene);
            if (campaign == null) return $"'{scene.name}' has no campaign. Run Visha > Setup Campaign In Open Scene first.";

            var vault = AssetDatabase.LoadAssetAtPath<EncounterConfig>(VaultConfigPath);
            var lineup = AssetDatabase.LoadAssetAtPath<VaultEncounter>(LineupPath);
            if (vault == null) return $"Missing {VaultConfigPath}.";
            if (lineup == null) return $"Missing {LineupPath}. Run Visha > Setup > Create Vault Encounter Asset first.";

            SerializedObject campaignData = new SerializedObject(campaign);
            var outcome = campaignData.FindProperty("outcomeController").objectReferenceValue as BattleOutcomeController;
            var enemy = campaignData.FindProperty("enemyHealth").objectReferenceValue as Health;
            if (outcome == null || enemy == null) return "CampaignController is missing its outcome controller or enemy Health.";

            Undo.SetCurrentGroupName("Add Vault Encounter");
            int undoGroup = Undo.GetCurrentGroup();

            SerializedProperty list = campaignData.FindProperty("encounters");
            bool listed = false;
            for (int i = 0; i < list.arraySize; i++) listed |= list.GetArrayElementAtIndex(i).objectReferenceValue == vault;
            if (!listed)
            {
                list.arraySize++;
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = vault;
                campaignData.ApplyModifiedProperties();
            }

            Health second = FindOrCreateSecondEnemy(scene, enemy);

            VaultCampaignLink link = campaign.GetComponent<VaultCampaignLink>();
            if (link == null) link = Undo.AddComponent<VaultCampaignLink>(campaign.gameObject);
            SerializedObject linkData = new SerializedObject(link);
            linkData.FindProperty("campaign").objectReferenceValue = campaign;
            linkData.FindProperty("outcomeController").objectReferenceValue = outcome;
            linkData.FindProperty("vaultEncounter").objectReferenceValue = vault;
            linkData.FindProperty("lineup").objectReferenceValue = lineup;
            linkData.FindProperty("secondEnemyHealth").objectReferenceValue = second;
            linkData.FindProperty("secondEnemyPattern").objectReferenceValue = second.GetComponent<EnemyPatternRunner>();
            linkData.ApplyModifiedProperties();

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log($"Vault Setup: '{scene.name}' plays the Vault as encounter {list.arraySize}.");
            return null;
        }

        private static Health FindOrCreateSecondEnemy(Scene scene, Health enemy)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == SecondEnemyName && root.TryGetComponent(out Health existing)) return existing;
            }

            var created = new GameObject(SecondEnemyName);
            SceneManager.MoveGameObjectToScene(created, scene);
            Undo.RegisterCreatedObjectUndo(created, "Create Second Enemy");
            created.transform.position = enemy.transform.position;
            Health health = created.AddComponent<Health>();
            Poison poison = created.AddComponent<Poison>();
            if (enemy.TryGetComponent(out Poison enemyPoison)) poison.SetValues(enemyPoison.MaxStacks, enemyPoison.DamagePerStack);
            created.AddComponent<EnemyPatternRunner>();
            // Out of play until VaultCampaignLink starts the Vault.
            created.SetActive(false);
            return health;
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (T found in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (found.gameObject.scene == scene) return found;
            }
            return null;
        }
    }
}
