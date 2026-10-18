using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Visha.Campaign;
using Visha.UI;

namespace Visha.Combat.Flow.EditorTools
{
    public static class CleanPresentationSetup
    {
        const string Atlas = "Assets/Art/Characters/CourtyardCombatants.png";
        [MenuItem("Visha/Apply Courtyard Presentation")]
        public static void ApplyFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Apply();
        }
        public static void Apply()
        {
            AssetDatabase.Refresh();
            var importer = (TextureImporter)AssetImporter.GetAtPath(Atlas);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 170;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048;
            importer.spritesheet = new[]
            {
                Slice("Vishakanya", new Rect(125, 646, 470, 550)),
                Slice("TempleGuardian", new Rect(680, 642, 550, 586)),
                Slice("GuardianWarden", new Rect(32, 32, 594, 594)),
                Slice("VaultSentinel", new Rect(670, 32, 570, 603))
            };
            importer.SaveAndReimport();
            var sprites = AssetDatabase.LoadAllAssetsAtPath(Atlas).OfType<Sprite>().ToDictionary(s => s.name);
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Campaign_Demo.unity");
            GameObject.Find("PlayerPlaceholder").GetComponent<SpriteRenderer>().sprite = sprites["Vishakanya"];
            GameObject.Find("EnemyPlaceholder").GetComponent<SpriteRenderer>().sprite = sprites["TempleGuardian"];
            string[] names = { "TempleGuardian", "GuardianWarden", "VaultSentinel" };
            for (int i = 0; i < names.Length; i++)
            {
                var config = AssetDatabase.LoadAssetAtPath<EncounterConfig>($"Assets/Data/Encounters/Encounter{i + 1}_{names[i]}.asset");
                var data = new SerializedObject(config);
                data.FindProperty("enemySprite").objectReferenceValue = sprites[names[i]];
                data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(config);
            }
            var hud = Object.FindAnyObjectByType<BattleHUD>();
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if (PrefabUtility.IsPartOfPrefabInstance(canvas.transform))
                    PrefabUtility.RevertObjectOverride(canvas.transform, InteractionMode.AutomatedAction);
            foreach (var rect in hud.GetComponentsInChildren<RectTransform>(true))
            {
                switch (rect.name)
                {
                    case "TopRule": Place(rect, 0, 452, 1920, 1); break;
                    case "HeaderBackdrop": Place(rect, 0, 496, 1920, 88); break;
                    case "Title": Place(rect, -780, 496, 300, 42); break;
                    case "Subtitle": case "Location": case "CommandRule": rect.gameObject.SetActive(false); break;
                    case "Phase": Place(rect, 720, 496, 400, 42); break;
                    case "CommandBackdrop": Place(rect, 0, -437, 1280, 206); break;
                    case "Feedback": Place(rect, 0, -358, 1220, 32); break;
                    case "Footer": Place(rect, 0, -513, 1180, 24); break;
                }
            }
            foreach (var text in hud.GetComponentsInChildren<Text>(true))
            {
                if (text.text.Contains("READ THE INTENT")) text.gameObject.SetActive(false);
                if (text.name == "Title") { text.text = "V I S H A"; text.fontSize = 30; }
                if (text.name == "Phase") text.fontSize = 24;
                if (text.name == "Footer") { text.text = "ATTACK: CHOOSE ABILITY + ENEMY     /     FADE: IMMEDIATE     /     ESC: CANCEL"; text.fontSize = 16; }
                if (text.name == "Feedback") text.fontSize = 20;
                if (text.name == "AbilityName") text.fontSize = 24;
            }
            foreach (var image in hud.GetComponentsInChildren<Image>(true))
            {
                if (image.name == "HeaderBackdrop" || image.name == "CommandBackdrop")
                    image.color = new Color(.055f, .075f, .07f, .96f);
                if (image.name == "VenomStrike" || image.name == "SerpentsBite" || image.name == "Fade")
                    image.color = new Color(.12f, .17f, .145f, 1);
            }
            foreach (var component in hud.GetComponentsInChildren<Component>(true))
                if (component != null && component != hud.transform && PrefabUtility.IsPartOfPrefabInstance(component))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }
        static SpriteMetaData Slice(string name, Rect rect) => new SpriteMetaData
        { name = name, rect = rect, alignment = (int)SpriteAlignment.BottomCenter, pivot = new Vector2(.5f, 0) };
        static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(width, height);
        }
    }
}
