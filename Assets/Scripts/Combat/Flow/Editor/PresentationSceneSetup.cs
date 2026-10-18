using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Visha.Battle;
using Visha.Campaign;
using Visha.UI;

namespace Visha.Combat.Flow.EditorTools
{
    public static class PresentationSceneSetup
    {
        private const string CampaignScenePath = "Assets/Scenes/Campaign_Demo.unity";
        private const string BattleScenePath = "Assets/Scenes/BattleUI_Demo.unity";
        private const string ArtPath = "Assets/Art/";
        private const string AudioPath = "Assets/Audio/";

        [MenuItem("Visha/Apply Presentation to Campaign Demo")]
        private static void ApplyFromMenu()
        {
            string error = Apply();
            if (error != null) EditorUtility.DisplayDialog("Campaign Presentation", error, "OK");
        }

        public static void ApplyCampaignDemoPresentationBatch()
        {
            string error = Apply();
            if (error != null)
            {
                Debug.LogError(error);
                EditorApplication.Exit(1);
                return;
            }
            EditorApplication.Exit(0);
        }

        private static string Apply()
        {
            Scene scene = EditorSceneManager.OpenScene(CampaignScenePath, OpenSceneMode.Single);
            GameObject playerVisual = FindRoot(scene, "PlayerPlaceholder");
            GameObject enemyVisual = FindRoot(scene, "EnemyPlaceholder");
            GameObject player = FindRoot(scene, "Player");
            GameObject enemy = FindRoot(scene, "Enemy");
            CampaignController campaign = FindComponent<CampaignController>(scene);
            CombatFlowController combatFlow = FindComponent<CombatFlowController>(scene);
            BattleOutcomeController outcome = FindComponent<BattleOutcomeController>(scene);
            if (playerVisual == null || enemyVisual == null || player == null || enemy == null || campaign == null || combatFlow == null || outcome == null)
                return "Campaign_Demo is missing a combatant, presentation object, or campaign controller.";

            Sprite playerSprite = PrepareSprite(ArtPath + "Characters/Vishakanya.png", 90f, FilterMode.Bilinear);
            Sprite guardianSprite = PrepareSprite(ArtPath + "Characters/TempleGuardian.png", 90f, FilterMode.Bilinear);
            Sprite wardenSprite = PrepareSprite(ArtPath + "Characters/GuardianWarden.png", 90f, FilterMode.Bilinear);
            Sprite sentinelSprite = PrepareSprite(ArtPath + "Characters/VaultSentinel.png", 90f, FilterMode.Bilinear);
            Sprite backgroundSprite = PrepareSprite(ArtPath + "Environment/TempleCourtyard.png", 53.333333f, FilterMode.Bilinear);
            Sprite poisonSprite = PrepareSprite(ArtPath + "Effects/PoisonMark.png", 100f, FilterMode.Bilinear);
            Sprite fadeSprite = PrepareSprite(ArtPath + "Effects/FadeAura.png", 100f, FilterMode.Bilinear);
            Sprite biteSprite = PrepareSprite(ArtPath + "Effects/BiteFlash.png", 100f, FilterMode.Bilinear);

            SpriteRenderer playerRenderer = playerVisual.GetComponent<SpriteRenderer>();
            SpriteRenderer enemyRenderer = enemyVisual.GetComponent<SpriteRenderer>();
            if (playerRenderer == null || enemyRenderer == null) return "Campaign_Demo presentation objects need SpriteRenderers.";

            playerRenderer.sprite = playerSprite;
            playerRenderer.color = Color.white;
            playerRenderer.sortingOrder = 0;
            playerVisual.transform.position = new Vector3(-3.7f, -0.22f, 0f);
            playerVisual.transform.localScale = new Vector3(1.12f, 1.12f, 1f);

            enemyRenderer.sprite = guardianSprite;
            enemyRenderer.color = Color.white;
            enemyRenderer.sortingOrder = 0;
            enemyVisual.transform.position = new Vector3(3.7f, -0.22f, 0f);
            enemyVisual.transform.localScale = new Vector3(1.12f, 1.12f, 1f);

            GameObject background = GetOrCreateRoot(scene, "CampaignBackground");
            SpriteRenderer backgroundRenderer = GetOrAdd<SpriteRenderer>(background);
            backgroundRenderer.sprite = backgroundSprite;
            backgroundRenderer.color = Color.white;
            backgroundRenderer.sortingOrder = -20;
            background.transform.position = new Vector3(0f, 0f, 1f);
            background.transform.localScale = Vector3.one;

            SpriteRenderer playerPoison = GetOrCreateEffect(playerVisual.transform, "PoisonMark", poisonSprite, 2, new Vector3(0.63f, 1.22f, 0f), Vector3.one * 0.48f);
            SpriteRenderer playerFade = GetOrCreateEffect(playerVisual.transform, "FadeAura", fadeSprite, 1, Vector3.zero, Vector3.one);
            SpriteRenderer playerBite = GetOrCreateEffect(playerVisual.transform, "BiteFlash", biteSprite, 3, Vector3.zero, Vector3.one);
            SpriteRenderer enemyPoison = GetOrCreateEffect(enemyVisual.transform, "PoisonMark", poisonSprite, 2, new Vector3(-0.63f, 1.22f, 0f), Vector3.one * 0.48f);
            SpriteRenderer enemyFade = GetOrCreateEffect(enemyVisual.transform, "FadeAura", fadeSprite, 1, Vector3.zero, Vector3.one);
            SpriteRenderer enemyBite = GetOrCreateEffect(enemyVisual.transform, "BiteFlash", biteSprite, 3, Vector3.zero, Vector3.one);

            BattleFeedback playerFeedback = GetOrAdd<BattleFeedback>(player);
            SetReference(playerFeedback, "health", player.GetComponent<Health>());
            SetReference(playerFeedback, "poison", player.GetComponent<Poison>());
            SetReference(playerFeedback, "fade", player.GetComponent<Fade>());
            SetReference(playerFeedback, "characterRenderer", playerRenderer);
            SetReference(playerFeedback, "poisonMark", playerPoison);
            SetReference(playerFeedback, "fadeAura", playerFade);
            SetReference(playerFeedback, "biteFlash", playerBite);

            BattleFeedback enemyFeedback = GetOrAdd<BattleFeedback>(enemy);
            SetReference(enemyFeedback, "health", enemy.GetComponent<Health>());
            SetReference(enemyFeedback, "poison", enemy.GetComponent<Poison>());
            SetReference(enemyFeedback, "fade", null);
            SetReference(enemyFeedback, "characterRenderer", enemyRenderer);
            SetReference(enemyFeedback, "poisonMark", enemyPoison);
            SetReference(enemyFeedback, "fadeAura", enemyFade);
            SetReference(enemyFeedback, "biteFlash", enemyBite);

            SetReference(campaign, "enemySpriteRenderer", enemyRenderer);
            SetEncounterSprite("Assets/Data/Encounters/Encounter1_TempleGuardian.asset", guardianSprite);
            SetEncounterSprite("Assets/Data/Encounters/Encounter2_GuardianWarden.asset", wardenSprite);
            SetEncounterSprite("Assets/Data/Encounters/Encounter3_VaultSentinel.asset", sentinelSprite);

            GameObject audioObject = GetOrCreateRoot(scene, "BattlePresentationAudio");
            AudioSource audioSource = GetOrAdd<AudioSource>(audioObject);
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
            audioSource.volume = 0.52f;
            BattleAudio battleAudio = GetOrAdd<BattleAudio>(audioObject);
            SetReference(battleAudio, "audioSource", audioSource);
            SetReference(battleAudio, "combatFlow", combatFlow);
            SetReference(battleAudio, "outcomeController", outcome);
            SetReference(battleAudio, "playerHealth", player.GetComponent<Health>());
            SetReference(battleAudio, "enemyHealth", enemy.GetComponent<Health>());
            SetReference(battleAudio, "enemyFeedback", enemyFeedback);
            SetReference(battleAudio, "hitClip", LoadClip("Hit.wav"));
            SetReference(battleAudio, "venomStrikeClip", LoadClip("VenomStrike.wav"));
            SetReference(battleAudio, "serpentsBiteClip", LoadClip("SerpentsBite.wav"));
            SetReference(battleAudio, "fadeClip", LoadClip("Fade.wav"));
            SetReference(battleAudio, "victoryClip", LoadClip("Victory.wav"));
            SetReference(battleAudio, "defeatClip", LoadClip("Defeat.wav"));

            GameObject debugPanel = FindRoot(scene, "DeveloperPanel");
            if (debugPanel != null) Undo.DestroyObjectImmediate(debugPanel);
            GameObject previewControls = FindRoot(scene, "PreviewControls");
            if (previewControls != null) previewControls.SetActive(false);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(CampaignScenePath, true),
                new EditorBuildSettingsScene(BattleScenePath, true)
            };
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;

            EditorUtility.SetDirty(campaign);
            EditorUtility.SetDirty(combatFlow);
            EditorUtility.SetDirty(playerRenderer);
            EditorUtility.SetDirty(enemyRenderer);
            EditorUtility.SetDirty(backgroundRenderer);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(scene)) return "Could not save Campaign_Demo after applying presentation assets.";
            Debug.Log("Campaign_Demo presentation is wired and saved.");
            return null;
        }

        private static Sprite PrepareSprite(string path, float pixelsPerUnit, FilterMode filterMode)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Missing texture importer for " + path);
            bool changed = importer.textureType != TextureImporterType.Sprite ||
                           importer.spriteImportMode != SpriteImportMode.Single ||
                           importer.spritePixelsPerUnit != pixelsPerUnit ||
                           importer.filterMode != filterMode ||
                           importer.textureCompression != TextureImporterCompression.Uncompressed ||
                           !importer.alphaIsTransparency;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerUnit;
            importer.filterMode = filterMode;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            if (changed) importer.SaveAndReimport();
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null) throw new InvalidOperationException("Could not import sprite " + path);
            return sprite;
        }

        private static AudioClip LoadClip(string fileName)
        {
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioPath + fileName);
            if (clip == null) throw new InvalidOperationException("Could not import audio " + fileName);
            return clip;
        }

        private static void SetEncounterSprite(string path, Sprite sprite)
        {
            EncounterConfig config = AssetDatabase.LoadAssetAtPath<EncounterConfig>(path);
            if (config == null) throw new InvalidOperationException("Missing encounter config " + path);
            SerializedObject data = new SerializedObject(config);
            SerializedProperty property = data.FindProperty("enemySprite");
            if (property == null) throw new InvalidOperationException("EncounterConfig has no enemySprite field.");
            property.objectReferenceValue = sprite;
            data.ApplyModifiedProperties();
            EditorUtility.SetDirty(config);
        }

        private static SpriteRenderer GetOrCreateEffect(Transform parent, string name, Sprite sprite, int order, Vector3 position, Vector3 scale)
        {
            Transform child = parent.Find(name);
            GameObject effect = child != null ? child.gameObject : new GameObject(name);
            if (child == null)
            {
                Undo.RegisterCreatedObjectUndo(effect, "Create " + name);
                effect.transform.SetParent(parent, false);
            }
            effect.transform.localPosition = position;
            effect.transform.localScale = scale;
            SpriteRenderer renderer = GetOrAdd<SpriteRenderer>(effect);
            renderer.sprite = sprite;
            renderer.color = Color.white;
            renderer.sortingOrder = order;
            renderer.enabled = false;
            return renderer;
        }

        private static T FindComponent<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T component = root.GetComponentInChildren<T>(true);
                if (component != null) return component;
            }
            return null;
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == name) return root;
            return null;
        }

        private static GameObject GetOrCreateRoot(Scene scene, string name)
        {
            GameObject root = FindRoot(scene, name);
            if (root != null) return root;
            root = new GameObject(name);
            SceneManager.MoveGameObjectToScene(root, scene);
            Undo.RegisterCreatedObjectUndo(root, "Create " + name);
            return root;
        }

        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            T existing = target.GetComponent<T>();
            return existing != null ? existing : Undo.AddComponent<T>(target);
        }

        private static void SetReference(UnityEngine.Object target, string fieldName, UnityEngine.Object value)
        {
            SerializedObject data = new SerializedObject(target);
            SerializedProperty property = data.FindProperty(fieldName);
            if (property == null) throw new InvalidOperationException(target.GetType().Name + " has no field " + fieldName);
            property.objectReferenceValue = value;
            data.ApplyModifiedProperties();
        }
    }
}
