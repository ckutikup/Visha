using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Visha.Battle;
using Visha.Enemies;
using Visha.Preview;
using Visha.UI;

namespace Visha.Combat.Flow.EditorTools
{
    /// <summary>
    /// Editor-only helper, wires the real turn loop into the open battle scene:
    /// creates the Player and Enemy combatants, adds the BattleOutcomeController and
    /// CombatFlowController, assigns every inspector reference, and disables the
    /// BattleHUDPreview so it no longer fakes turn changes.
    /// </summary>
    public static class CombatFlowSceneSetup
    {
        private const string PlayerName = "Player";
        private const string EnemyName = "Enemy";
        private const string ControllerName = "CombatFlowController";

        private const int PlayerMaxHealth = 30;
        private const int EnemyMaxHealth = 24;

        [MenuItem("Visha/Setup Combat Flow In Open Scene")]
        private static void SetupOpenScene()
        {
            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                EditorUtility.DisplayDialog("Combat Flow Setup",
                    "You are editing a prefab. Exit Prefab Mode and open the battle scene first.", "OK");
                return;
            }

            Scene scene = SceneManager.GetActiveScene();
            BattleHUD[] huds = Object.FindObjectsByType<BattleHUD>(FindObjectsInactive.Include);
            BattleHUD hud = huds.Length > 0 ? huds[0] : null;
            if (hud == null)
            {
                EditorUtility.DisplayDialog("Combat Flow Setup",
                    $"No BattleHUD found in '{scene.name}'. Open BattleUI_Demo and try again.", "OK");
                return;
            }

            Undo.SetCurrentGroupName("Setup Combat Flow");
            int undoGroup = Undo.GetCurrentGroup();

            // Combatants
            GameObject player = FindOrCreateRoot(scene, PlayerName);
            Health playerHealth = GetOrAdd<Health>(player);
            GetOrAdd<Poison>(player);
            VenomStrike venomStrike = GetOrAdd<VenomStrike>(player);
            SetInt(playerHealth, "maxHealth", PlayerMaxHealth);

            GameObject enemy = FindOrCreateRoot(scene, EnemyName);
            Health enemyHealth = GetOrAdd<Health>(enemy);
            GetOrAdd<Poison>(enemy);
            EnemyPatternRunner patternRunner = GetOrAdd<EnemyPatternRunner>(enemy);
            SetInt(enemyHealth, "maxHealth", EnemyMaxHealth);

            // Outcome detection and turn orchestration
            GameObject controllerObject = FindOrCreateRoot(scene, ControllerName);
            BattleOutcomeController outcome = GetOrAdd<BattleOutcomeController>(controllerObject);
            SetReference(outcome, "playerHealth", playerHealth);
            SetReference(outcome, "enemyHealth", enemyHealth);

            CombatFlowController controller = GetOrAdd<CombatFlowController>(controllerObject);
            SetReference(controller, "playerGameObject", player);
            SetReference(controller, "enemyGameObject", enemy);
            SetReference(controller, "battleHUD", hud);
            SetReference(controller, "outcomeController", outcome);
            SetReference(controller, "enemyPatternRunner", patternRunner);
            SetReference(controller, "venomStrike", venomStrike);

            int previewsDisabled = 0;
            foreach (BattleHUDPreview preview in Object.FindObjectsByType<BattleHUDPreview>(FindObjectsInactive.Include))
            {
                if (!preview.enabled) continue;
                Undo.RecordObject(preview, "Disable BattleHUDPreview");
                preview.enabled = false;
                previewsDisabled++;
            }

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = controllerObject;

            Debug.Log($"Combat Flow Setup: wired '{scene.name}' (Player {PlayerMaxHealth} HP, " +
                      $"Enemy {EnemyMaxHealth} HP, {previewsDisabled} BattleHUDPreview disabled). Save the scene to keep it.");
        }

        private static GameObject FindOrCreateRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name) return root;
            }

            GameObject created = new GameObject(name);
            SceneManager.MoveGameObjectToScene(created, scene);
            Undo.RegisterCreatedObjectUndo(created, $"Create {name}");
            return created;
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            T existing = target.GetComponent<T>();
            return existing != null ? existing : Undo.AddComponent<T>(target);
        }

        private static void SetReference(Object target, string fieldName, Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            FindProperty(serialized, fieldName).objectReferenceValue = value;
            serialized.ApplyModifiedProperties();
        }

        private static void SetInt(Object target, string fieldName, int value)
        {
            SerializedObject serialized = new SerializedObject(target);
            FindProperty(serialized, fieldName).intValue = value;
            serialized.ApplyModifiedProperties();
        }

        private static SerializedProperty FindProperty(SerializedObject serialized, string fieldName)
        {
            SerializedProperty property = serialized.FindProperty(fieldName);
            if (property == null)
            {
                throw new System.InvalidOperationException(
                    $"{serialized.targetObject.GetType().Name} has no serialized field '{fieldName}'.");
            }
            return property;
        }
    }
}
