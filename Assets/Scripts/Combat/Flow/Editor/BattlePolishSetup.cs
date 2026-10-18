using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Visha.UI;

namespace Visha.Combat.Flow.EditorTools
{
    public static class BattlePolishSetup
    {
        [MenuItem("Visha/Apply Battle UI Polish")]
        public static void Apply()
        {
            CombatPresentationSetup.Apply();
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var hud = Object.FindAnyObjectByType<BattleHUD>();
            var screens = Object.FindAnyObjectByType<BattleScreenView>();
            var body = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Barlow-Regular.ttf");
            var heading = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/Barlow-SemiBold.ttf");
            if (hud.GetComponent<BattleShortcuts>() == null) hud.gameObject.AddComponent<BattleShortcuts>();
            if (Camera.main.GetComponent<BattleViewport>() == null) Camera.main.gameObject.AddComponent<BattleViewport>();
            foreach (var scaler in Object.FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include))
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            }
            foreach (var graphic in hud.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = graphic.GetComponent<Button>() != null;
            foreach (var anchor in hud.GetComponentsInChildren<WorldHUDAnchor>(true))
            {
                bool status = anchor.name == "PlayerStatus" || anchor.name == "EnemyStatus";
                bool intention = anchor.GetComponent<IntentView>() != null;
                if (!status && !intention) continue;
                var data = new SerializedObject(anchor);
                data.FindProperty("aboveTarget").boolValue = true;
                data.FindProperty("worldOffset").vector3Value = Vector3.zero;
                data.FindProperty("screenOffset").vector2Value = new Vector2(0, intention ? 245 : 85);
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            int index = 0;
            foreach (string name in new[] { "VenomStrike", "SerpentsBite", "Fade" })
            {
                var button = Find(hud.transform, name).GetComponent<Button>();
                float[] x = { -540, -340, -140 }, y = { 190, 220, 190 }, size = { 112, 124, 112 };
                Anchor((RectTransform)button.transform, Vector2.right, x[index], y[index], size[index], size[index]);
                var colors = button.colors;
                colors.normalColor = new Color(.12f, .2f, .19f, .97f);
                colors.highlightedColor = new Color(.22f, .34f, .27f);
                colors.pressedColor = new Color(.32f, .47f, .36f);
                colors.disabledColor = new Color(.09f, .12f, .13f, .95f);
                colors.selectedColor = colors.highlightedColor;
                button.colors = colors; button.image.color = Color.white;
                var glyph = button.GetComponentInChildren<AbilityGlyph>(true);
                Anchor((RectTransform)glyph.transform, new Vector2(.5f, .5f), 0, 0, size[index] * .52f, size[index] * .52f);
                var ringTransform = button.transform.Find("SkillRing");
                if (ringTransform == null)
                {
                    var go = new GameObject("SkillRing", typeof(RectTransform), typeof(SkillRing));
                    go.transform.SetParent(button.transform, false); ringTransform = go.transform;
                }
                var ring = ringTransform.GetComponent<SkillRing>(); ring.raycastTarget = false;
                ring.color = new Color(.69f, .64f, .47f, .8f);
                Anchor((RectTransform)ringTransform, new Vector2(.5f, .5f), 0, 0, size[index] + 8, size[index] + 8);
                var title = button.transform.Find("AbilityName").GetComponent<Text>();
                title.text = index == 0 ? "Venom Strike" : index == 1 ? "Serpent’s Bite" : "Fade";
                title.font = heading; title.fontSize = 23; title.resizeTextForBestFit = false;
                title.color = new Color(.94f, .91f, .81f);
                Anchor(title.rectTransform, new Vector2(.5f, .5f), 0, -size[index] * .5f - 23, 180, 32);
                var desc = button.transform.Find("Description").GetComponent<Text>();
                desc.font = body; desc.fontSize = 19; desc.resizeTextForBestFit = false;
                Anchor(desc.rectTransform, new Vector2(.5f, .5f), 0, -size[index] * .5f - 53, 180, 28);
                var key = button.transform.Find("Shortcut");
                if (key == null)
                {
                    var go = new GameObject("Shortcut", typeof(RectTransform), typeof(Text));
                    go.transform.SetParent(button.transform, false); key = go.transform;
                }
                var keyLabel = key.GetComponent<Text>(); keyLabel.font = heading; keyLabel.fontSize = 21;
                keyLabel.text = (index + 1).ToString(); keyLabel.alignment = TextAnchor.MiddleCenter;
                keyLabel.color = new Color(.92f, .79f, .51f); keyLabel.raycastTarget = false;
                Anchor(keyLabel.rectTransform, new Vector2(.5f, .5f), size[index] * .39f, size[index] * .39f, 32, 32);
                index++;
            }
            var feedback = Find(hud.transform, "Feedback").GetComponent<Text>();
            Anchor(feedback.rectTransform, Vector2.zero, 420, 92, 700, 90);
            feedback.font = body; feedback.fontSize = 26; feedback.resizeTextForBestFit = true;
            feedback.resizeTextMinSize = 22; feedback.resizeTextMaxSize = 26;
            feedback.alignment = TextAnchor.MiddleLeft; feedback.color = new Color(.91f, .93f, .87f);
            var plate = hud.transform.Find("AbilityHintPlate");
            if (plate == null)
            {
                var go = new GameObject("AbilityHintPlate", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(hud.transform, false); plate = go.transform;
            }
            Anchor((RectTransform)plate, Vector2.zero, 420, 92, 750, 120);
            plate.GetComponent<Image>().color = new Color(.025f, .06f, .065f, .87f);
            plate.GetComponent<Image>().raycastTarget = false;
            plate.SetAsFirstSibling();
            Anchor(Find(hud.transform, "Phase") as RectTransform, new Vector2(.5f, 1), 0, -52, 420, 50);
            Anchor(Find(screens.transform, "Encounter") as RectTransform, Vector2.up, 290, -52, 500, 50);
            foreach (string name in new[] { "PlayerStatus", "EnemyStatus" })
            {
                var panel = Find(hud.transform, name);
                var title = panel.Find("Name").GetComponent<Text>();
                title.font = heading; title.fontSize = 26; title.resizeTextForBestFit = true;
                title.resizeTextMinSize = 20; title.resizeTextMaxSize = 26;
                title.horizontalOverflow = HorizontalWrapMode.Wrap; title.verticalOverflow = VerticalWrapMode.Truncate;
                Anchor(title.rectTransform, new Vector2(.5f, .5f), 0, 35, 360, 60);
                var hp = panel.Find("Health").GetComponent<Text>();
                Anchor(hp.rectTransform, new Vector2(.5f, .5f), 0, -28, 280, 38);
                hp.transform.SetAsLastSibling();
                hp.fontSize = 23; hp.color = new Color(.92f, .94f, .86f);
                foreach (var image in panel.GetComponentsInChildren<Image>(true))
                {
                    image.raycastTarget = false;
                    if (image.name == "StatusBackdrop") { image.color = new Color(.025f, .055f, .06f, .8f); image.rectTransform.anchorMax = new Vector2(1, 1.25f); }
                    if (image.name == "HealthFill") image.color = name == "PlayerStatus" ? new Color(.36f, .76f, .59f) : new Color(.83f, .62f, .39f);
                }
            }
            var poisonCount = Find(hud.transform, "Count").GetComponent<Text>();
            poisonCount.fontSize = 22;
            poisonCount.color = new Color(.72f, .9f, .78f);
            poisonCount.rectTransform.sizeDelta = new Vector2(150, 34);
            var intent = hud.GetComponentInChildren<IntentView>(true);
            var divider = intent.transform.Find("IntentDivider");
            if (divider != null) divider.gameObject.SetActive(false);
            var intentData = new SerializedObject(intent);
            var action = (Text)intentData.FindProperty("actionLabel").objectReferenceValue;
            var damage = (Text)intentData.FindProperty("damageLabel").objectReferenceValue;
            var intentIcon = (Image)intentData.FindProperty("icon").objectReferenceValue;
            ((RectTransform)intent.transform).sizeDelta = new Vector2(340, 140);
            Anchor(action.rectTransform, new Vector2(.5f, .5f), 20, 22, 250, 72);
            action.fontSize = 24; action.resizeTextMinSize = 18; action.resizeTextMaxSize = 24;
            Anchor(damage.rectTransform, new Vector2(.5f, .5f), 20, -42, 250, 38);
            Anchor(intentIcon.rectTransform, new Vector2(.5f, .5f), -137, 0, 24, 38);
            foreach (var text in screens.GetComponentsInChildren<Text>(true))
            {
                text.raycastTarget = false;
                if (text.text == "RETURN TO START") text.text = "Return to Title";
                if (text.text == "RETRY") text.text = "Retry Encounter";
                if (text.name == "Message" || text.name == "Instructions" || text.name == "Story") text.font = body;
            }
            foreach (var component in Object.FindObjectsByType<Component>(FindObjectsInactive.Include))
                if (component != null && PrefabUtility.IsPartOfPrefabInstance(component))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        }
        public static void BuildReview() { Apply(); DemoBuild.BuildMac(); }

        private static Transform Find(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            throw new System.InvalidOperationException("Missing UI element: " + name);
        }
        private static void Anchor(RectTransform rect, Vector2 anchor, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = anchor; rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(w, h);
        }
    }
}
