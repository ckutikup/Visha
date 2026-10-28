using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Visha.Preview;

namespace Visha.UI.Editor
{
    public static class TwoEnemyPreviewSetup
    {
        [MenuItem("Visha/UI/Open Two Enemy Preview")]
        public static void Open()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var preview = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var source = EditorSceneManager.OpenScene("Assets/Scenes/Campaign_Demo.unity", OpenSceneMode.Additive);
            SceneManager.MergeScenes(source, preview);
            SceneManager.SetActiveScene(preview);
            var roots = preview.GetRootGameObjects();
            foreach (var behaviour in roots.SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true)).ToArray())
            {
                if (!behaviour) continue;
                var type = behaviour.GetType();
                bool keep = type.Namespace == "Visha.UI" || type.Name == "Health" ||
                    (type.Namespace != null && type.Namespace.StartsWith("UnityEngine"));
                if (!keep) Object.DestroyImmediate(behaviour);
            }
            var hud = roots.SelectMany(root => root.GetComponentsInChildren<BattleHUD>(true)).Single();
            var guardian = roots.SelectMany(root => root.GetComponentsInChildren<SpriteRenderer>(true))
                .First(renderer => renderer.sprite && renderer.sprite.name == "TempleGuardian");
            guardian.transform.position = new Vector3(1.8f, guardian.transform.position.y, guardian.transform.position.z);
            guardian.transform.localScale *= .82f;
            var sentinel = Object.Instantiate(guardian.gameObject);
            sentinel.name = "SentinelPreview";
            sentinel.transform.position = new Vector3(5.1f, guardian.transform.position.y, guardian.transform.position.z);
            sentinel.GetComponent<SpriteRenderer>().sprite = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Characters/CourtyardCombatants.png").OfType<Sprite>().Single(sprite => sprite.name == "VaultSentinel");
            guardian.gameObject.AddComponent<EnemyTargetAnchor>().Configure(hud, 10, Camera.main);
            sentinel.AddComponent<EnemyTargetAnchor>().Configure(hud, 20, Camera.main);
            hud.gameObject.AddComponent<TwoEnemyHUDPreview>();
            Selection.activeGameObject = hud.gameObject;
        }
    }
}
