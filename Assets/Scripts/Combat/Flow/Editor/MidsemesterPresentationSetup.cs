using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Visha.Campaign;
using Visha.UI;

namespace Visha.Combat.Flow.EditorTools
{
    public static class MidsemesterPresentationSetup
    {
        const string AtlasPath = "Assets/Art/Characters/TempleCombatants.png";
        const string BackgroundPath = "Assets/Art/Environment/TempleCourtyardDusk.png";

        [MenuItem("Visha/Apply Midsemester Presentation")]
        public static void ApplyFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Apply();
        }

        public static void Apply()
        {
            AssetDatabase.Refresh();
            var importer = (TextureImporter)AssetImporter.GetAtPath(AtlasPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 160;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048;
            importer.spritesheet = new[]
            {
                Sprite("Vishakanya", new Rect(65, 650, 520, 555)),
                Sprite("TempleGuardian", new Rect(665, 650, 570, 600)),
                Sprite("GuardianWarden", new Rect(45, 20, 580, 630)),
                Sprite("VaultSentinel", new Rect(640, 16, 600, 640))
            };
            importer.SaveAndReimport();
            var sprites = AssetDatabase.LoadAllAssetsAtPath(AtlasPath).OfType<Sprite>().ToDictionary(s => s.name);
            var backdrop = (TextureImporter)AssetImporter.GetAtPath(BackgroundPath);
            backdrop.textureType = TextureImporterType.Sprite;
            backdrop.spriteImportMode = SpriteImportMode.Single;
            backdrop.spritePixelsPerUnit = 94.05f;
            backdrop.filterMode = FilterMode.Point;
            backdrop.textureCompression = TextureImporterCompression.Uncompressed;
            backdrop.mipmapEnabled = false;
            backdrop.maxTextureSize = 2048;
            backdrop.SaveAndReimport();

            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Campaign_Demo.unity", OpenSceneMode.Single);
            var player = GameObject.Find("PlayerPlaceholder");
            var enemy = GameObject.Find("EnemyPlaceholder");
            player.GetComponent<SpriteRenderer>().sprite = sprites["Vishakanya"];
            enemy.GetComponent<SpriteRenderer>().sprite = sprites["TempleGuardian"];
            player.transform.position = new Vector3(-3.7f, -1.9f, 0);
            enemy.transform.position = new Vector3(3.7f, -1.9f, 0);
            player.transform.localScale = enemy.transform.localScale = Vector3.one;
            foreach (var unit in new[] { player, enemy })
            {
                foreach (Transform child in unit.transform)
                {
                    if (child.name == "PoisonMark") child.localPosition = new Vector3(0.85f, 3.1f, 0);
                    else if (child.name == "FadeAura" || child.name == "BiteFlash")
                        child.localPosition = new Vector3(0, 1.8f, 0);
                }
            }
            var background = GameObject.Find("CampaignBackground").GetComponent<SpriteRenderer>();
            background.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BackgroundPath);
            background.transform.position = new Vector3(0, 0, 1);
            background.transform.localScale = Vector3.one;

            string[] configs = { "Encounter1_TempleGuardian", "Encounter2_GuardianWarden", "Encounter3_VaultSentinel" };
            string[] names = { "TempleGuardian", "GuardianWarden", "VaultSentinel" };
            for (int i = 0; i < configs.Length; i++)
            {
                var config = AssetDatabase.LoadAssetAtPath<EncounterConfig>($"Assets/Data/Encounters/{configs[i]}.asset");
                var data = new SerializedObject(config);
                data.FindProperty("enemySprite").objectReferenceValue = sprites[names[i]];
                data.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(config);
            }

            var hud = Object.FindFirstObjectByType<BattleHUD>();
            foreach (var anchor in hud.GetComponentsInChildren<WorldHUDAnchor>(true))
            {
                var data = new SerializedObject(anchor);
                float y = anchor.name == "EnemyTarget" ? 1.85f : anchor.name == "EnemyIntent" ? 4.45f : -0.32f;
                data.FindProperty("worldOffset").vector3Value = new Vector3(0, y, 0);
                data.ApplyModifiedPropertiesWithoutUndo();
                if (anchor.name == "EnemyTarget") ((RectTransform)anchor.transform).sizeDelta = new Vector2(350, 415);
                if (anchor.name == "PlayerStatus" || anchor.name == "EnemyStatus" || anchor.name == "EnemyIntent")
                {
                    var panel = anchor.GetComponent<Image>() ?? anchor.gameObject.AddComponent<Image>();
                    panel.color = new Color(0.025f, 0.045f, 0.06f, 0.94f);
                    panel.raycastTarget = false;
                    if (anchor.name == "EnemyStatus")
                    {
                        var existing = anchor.transform.Find("StatusBackdrop");
                        var plate = existing != null ? existing.gameObject : new GameObject("StatusBackdrop", typeof(RectTransform), typeof(Image));
                        plate.transform.SetParent(anchor.transform, false);
                        plate.transform.SetAsFirstSibling();
                        var rect = (RectTransform)plate.transform;
                        rect.anchorMin = new Vector2(0, -0.38f);
                        rect.anchorMax = Vector2.one;
                        rect.offsetMin = rect.offsetMax = Vector2.zero;
                        plate.GetComponent<Image>().color = panel.color;
                        plate.GetComponent<Image>().raycastTarget = false;
                        panel.color = Color.clear;
                    }
                }
            }
            foreach (var image in hud.GetComponentsInChildren<Image>(true))
                if (image.name == "CommandBackdrop" || image.name == "HeaderBackdrop")
                    image.color = new Color(0.025f, 0.045f, 0.06f, 0.98f);
            foreach (var text in hud.GetComponentsInChildren<Text>(true))
            {
                if (text.name == "Description")
                {
                    text.fontSize = 20;
                    text.color = new Color(0.82f, 0.89f, 0.86f);
                    text.resizeTextForBestFit = true;
                    text.resizeTextMinSize = 17;
                    text.resizeTextMaxSize = 20;
                }
                if (text.name == "Health" || text.name == "Count")
                {
                    text.fontSize = 22;
                    text.color = new Color(0.90f, 0.94f, 0.90f);
                }
            }
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        static SpriteMetaData Sprite(string name, Rect rect) => new SpriteMetaData
        {
            name = name, rect = rect, alignment = (int)SpriteAlignment.BottomCenter,
            pivot = new Vector2(0.5f, 0)
        };
    }
}
