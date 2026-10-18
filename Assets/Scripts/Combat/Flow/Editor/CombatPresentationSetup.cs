using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Visha.Campaign;
using Visha.UI;

namespace Visha.Combat.Flow.EditorTools
{
    public static class CombatPresentationSetup
    {
        [MenuItem("Visha/Apply Combat Presentation")]
        public static void ApplyFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Apply();
        }
        public static void Apply()
        {
            AssetDatabase.Refresh();
            const string vaultPath = "Assets/Art/Environment/TempleVault.png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(vaultPath);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 94.05f; importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048; importer.SaveAndReimport();
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Campaign_Demo.unity");
            var controller = Object.FindAnyObjectByType<CombatFlowController>();
            if (controller.GetComponent<RoutineBreak>() == null) controller.gameObject.AddComponent<RoutineBreak>();
            var presentation = controller.GetComponent<CombatActionPresentation>();
            if (presentation == null) presentation = controller.gameObject.AddComponent<CombatActionPresentation>();
            var data = new SerializedObject(presentation);
            Set(data, "player", GameObject.Find("PlayerPlaceholder").GetComponent<CharacterIdleMotion>());
            Set(data, "enemy", GameObject.Find("EnemyPlaceholder").GetComponent<CharacterIdleMotion>());
            data.ApplyModifiedPropertiesWithoutUndo();
            data = new SerializedObject(controller); Set(data, "presentation", presentation); data.ApplyModifiedPropertiesWithoutUndo();
            var warden = AssetDatabase.LoadAssetAtPath<EncounterConfig>("Assets/Data/Encounters/Encounter2_GuardianWarden.asset");
            data = new SerializedObject(warden); data.FindProperty("openingOffset").intValue = 1;
            data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(warden);
            var campaign = Object.FindAnyObjectByType<CampaignController>();
            var screens = Object.FindAnyObjectByType<BattleScreenView>();
            if (campaign.GetComponent<TempleMapView>() == null) campaign.gameObject.AddComponent<TempleMapView>();
            var mapData = new SerializedObject(campaign.GetComponent<TempleMapView>());
            Set(mapData, "courtyard", AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/TempleCourtyardDusk.png"));
            Set(mapData, "vault", AssetDatabase.LoadAssetAtPath<Sprite>(vaultPath));
            mapData.ApplyModifiedPropertiesWithoutUndo();
            var environment = campaign.GetComponent<CampaignEnvironment>();
            if (environment == null) environment = campaign.gameObject.AddComponent<CampaignEnvironment>();
            data = new SerializedObject(environment);
            Set(data, "campaign", campaign); Set(data, "screens", screens);
            Set(data, "background", GameObject.Find("CampaignBackground").GetComponent<SpriteRenderer>());
            Set(data, "courtyard", AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/TempleCourtyardDusk.png"));
            Set(data, "vault", AssetDatabase.LoadAssetAtPath<Sprite>(vaultPath));
            data.ApplyModifiedPropertiesWithoutUndo();
            var body = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Barlow-Regular.ttf");
            var heading = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Barlow-SemiBold.ttf");
            foreach (var text in Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                text.font = text.name == "Description" || text.name == "Instructions" || text.name == "Story" ? body : heading;
                text.fontStyle = FontStyle.Normal;
                if (text.name == "Name")
                {
                    text.fontSize = 26;
                    if (text.text.Contains("V I S H")) text.text = "Vishakanya";
                }
                if (text.name == "Health") { text.fontSize = 22; text.color = new Color(.9f, .94f, .9f); }
                if (text.name == "Description")
                { text.fontSize = 21; text.resizeTextMinSize = 18; text.resizeTextMaxSize = 21; }
                if (PrefabUtility.IsPartOfPrefabInstance(text)) PrefabUtility.RecordPrefabInstancePropertyModifications(text);
            }
            var hud = Object.FindAnyObjectByType<BattleHUD>();
            data = new SerializedObject(hud); data.FindProperty("compactAbilities").boolValue = true; data.ApplyModifiedPropertiesWithoutUndo();
            foreach (var anchor in hud.GetComponentsInChildren<WorldHUDAnchor>(true))
            {
                data = new SerializedObject(anchor);
                data.FindProperty("fixedPosition").boolValue = true;
                if (anchor.name == "PlayerStatus" || anchor.name == "EnemyStatus")
                {
                    data.FindProperty("worldOffset").vector3Value = new Vector3(0, -.5f, 0);
                    ((RectTransform)anchor.transform).sizeDelta = new Vector2(380, 92);
                    var poison = anchor.transform.Find("Poison");
                    if (poison != null) ((RectTransform)poison).anchoredPosition = new Vector2(0, -48);
                    var backdrop = anchor.transform.Find("StatusBackdrop");
                    if (backdrop != null) ((RectTransform)backdrop).anchorMin = new Vector2(0, -.18f);
                }
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            foreach (var button in hud.GetComponentsInChildren<Button>(true))
            {
                if (button.name != "VenomStrike" && button.name != "SerpentsBite" && button.name != "Fade") continue;
                foreach (var image in button.GetComponentsInChildren<Image>(true))
                    if (image.name == "Icon" || image.name == "IconBack") image.gameObject.SetActive(false);
                var icon = button.transform.Find("Icon").GetComponent<Image>();
                icon.gameObject.SetActive(false);
                Transform glyphTransform = button.transform.Find("Glyph");
                if (glyphTransform == null)
                {
                    var glyphObject = new GameObject("Glyph", typeof(RectTransform), typeof(AbilityGlyph));
                    glyphObject.transform.SetParent(button.transform, false);
                    glyphTransform = glyphObject.transform;
                }
                if (glyphTransform.GetComponent<CanvasRenderer>() == null) glyphTransform.gameObject.AddComponent<CanvasRenderer>();
                var glyph = glyphTransform.GetComponent<AbilityGlyph>();
                glyph.raycastTarget = false;
                glyph.color = button.name == "Fade" ? new Color(.9f, .8f, .53f) : new Color(.48f, .92f, .72f);
                var glyphData = new SerializedObject(glyph);
                glyphData.FindProperty("ability").enumValueIndex = button.name == "VenomStrike" ? 0 : button.name == "SerpentsBite" ? 1 : 2;
                glyphData.ApplyModifiedPropertiesWithoutUndo();
                Place((RectTransform)glyphTransform, 0, 34, 30, 30);
                string effect = button.name == "VenomStrike" ? "PoisonMark" : button.name == "SerpentsBite" ? "BiteFlash" : "FadeAura";
                icon.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/Effects/{effect}.png");
                icon.color = Color.white;
                icon.preserveAspect = true;
                Place(icon.rectTransform, 0, 34, 34, 34);
                var ability = button.GetComponent<AbilityButtonView>();
                data = new SerializedObject(ability);
                Set(data, "hud", hud);
                data.FindProperty("ability").enumValueIndex = button.name == "VenomStrike" ? 0 : button.name == "SerpentsBite" ? 1 : 2;
                var circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
                if (circle != null) { button.GetComponent<Image>().sprite = circle; button.GetComponent<Image>().type = Image.Type.Simple; }
                var accent = button.transform.Find("TopEdge");
                if (accent != null) accent.gameObject.SetActive(false);
                Set(data, "selectionSource", Object.FindAnyObjectByType<BattleAudio>().GetComponent<AudioSource>());
                Set(data, "selectionClip", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/MenuConfirm.wav"));
                data.ApplyModifiedPropertiesWithoutUndo();
                var title = button.transform.Find("AbilityName").GetComponent<Text>();
                title.text = button.name == "VenomStrike" ? "Venom" : button.name == "SerpentsBite" ? "Bite" : "Fade";
                title.fontSize = 24;
                Place(title.rectTransform, 0, -5, 145, 32);
                var description = button.transform.Find("Description").GetComponent<Text>();
                Place(description.rectTransform, 0, -36, 145, 26);
                description.fontSize = 20; description.resizeTextMaxSize = 20; description.resizeTextMinSize = 18;
            }
            foreach (var image in hud.GetComponentsInChildren<Image>(true))
            {
                if (image.name == "BarLeft" || image.name == "BarRight") image.gameObject.SetActive(false);
                if (image.name == "HealthFill") image.color = image.transform.parent.parent.name == "EnemyStatus"
                    ? new Color(.76f, .48f, .37f) : new Color(.31f, .7f, .55f);
            }
            foreach (var rect in hud.GetComponentsInChildren<RectTransform>(true))
            {
                if (rect.name == "HeaderBackdrop" || rect.name == "TopRule" || rect.name == "CommandBackdrop"
                    || rect.name == "Title" || rect.name == "Footer") rect.gameObject.SetActive(false);
                if (rect.name == "Phase") Place(rect, 0, 484, 320, 46);
                if (rect.name == "Feedback") Place(rect, 450, -346, 900, 38);
                if (rect.name == "VenomStrike") Place(rect, 400, -448, 150, 150);
                if (rect.name == "SerpentsBite") Place(rect, 620, -448, 150, 150);
                if (rect.name == "Fade") Place(rect, 840, -448, 150, 150);
                if (rect.name == "StatusBackdrop") rect.GetComponent<Image>().color = new Color(.035f, .065f, .07f, .48f);
            }
            data = new SerializedObject(screens);
            var encounter = (Text)data.FindProperty("encounterLabel").objectReferenceValue;
            Place(encounter.rectTransform, -670, 484, 490, 50);
            encounter.alignment = TextAnchor.MiddleLeft;
            encounter.fontSize = 28;
            foreach (var text in hud.GetComponentsInChildren<Text>(true))
            {
                if (text.name == "Health") { text.gameObject.SetActive(true); text.transform.SetAsLastSibling(); }
                if (text.name == "Phase" || text.name == "Health" || text.name == "Name" || text.name == "Feedback")
                {
                    var shadow = text.GetComponent<Shadow>();
                    if (shadow == null) shadow = text.gameObject.AddComponent<Shadow>();
                    shadow.effectColor = new Color(0, 0, 0, .85f);
                    shadow.effectDistance = new Vector2(1, -2);
                }
            }
            var areaShadow = encounter.GetComponent<Shadow>();
            if (areaShadow == null) areaShadow = encounter.gameObject.AddComponent<Shadow>();
            areaShadow.effectColor = new Color(0, 0, 0, .9f);
            areaShadow.effectDistance = new Vector2(1, -2);
            var complete = screens.transform.Find("CompletePanel/Message").GetComponent<Text>();
            complete.fontSize = 28;
            complete.rectTransform.sizeDelta = new Vector2(1200, 140);
            var victoryText = screens.transform.Find("VictoryPanel/Card/Message").GetComponent<Text>();
            victoryText.rectTransform.sizeDelta = new Vector2(950, 130);
            victoryText.fontSize = 28;
            screens.transform.Find("CompletePanel/ReturnButton").GetComponentInChildren<Text>().text = "Return to Title";
            complete.text = "The path ahead remains sealed.\nThe guardians beyond still await release.\nInner Vault and Sanctum are unavailable in this build.";
            foreach (var label in screens.transform.Find("CompletePanel").GetComponentsInChildren<Text>(true))
            {
                if (label.text.ToLowerInvariant().Contains("demo complete")) label.text = "THE SEALED PATH";
                if (label.transform.parent.name == "RestartButton") label.text = "Replay Available Encounters";
            }
            screens.transform.Find("TitlePanel/StartButton").GetComponentInChildren<Text>().text = "Enter the Temple";
            screens.transform.Find("VictoryPanel/Card/NextButton").GetComponentInChildren<Text>().text = "Return to Temple Map";
            screens.transform.Find("CompletePanel/RestartButton").GetComponentInChildren<Text>().text = "Return to Temple Map";
            foreach (var root in new[] { hud.transform, screens.transform })
                foreach (var component in root.GetComponentsInChildren<Component>(true))
                    if (component != null && component != root && PrefabUtility.IsPartOfPrefabInstance(component))
                        PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        }
        static void Set(SerializedObject data, string field, Object value) => data.FindProperty(field).objectReferenceValue = value;
        static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(width, height);
        }
    }
}
