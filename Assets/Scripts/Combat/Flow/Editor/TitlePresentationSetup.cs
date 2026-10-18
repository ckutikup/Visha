using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Visha.UI;

namespace Visha.Combat.Flow.EditorTools
{
    public static class TitlePresentationSetup
    {
        [MenuItem("Visha/Apply Title Presentation")]
        public static void ApplyFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Apply();
        }

        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Campaign_Demo.unity");
            var view = Object.FindFirstObjectByType<BattleScreenView>();
            var panel = view.transform.Find("TitlePanel");
            var previous = panel.Find("TitleArt");
            if (previous != null) Object.DestroyImmediate(previous.gameObject);
            previous = panel.Find("Story");
            if (previous != null) Object.DestroyImmediate(previous.gameObject);
            previous = panel.Find("Subtitle");
            if (previous != null) Object.DestroyImmediate(previous.gameObject);
            var art = new GameObject("TitleArt", typeof(RectTransform), typeof(Image));
            art.transform.SetParent(panel, false);
            art.transform.SetAsFirstSibling();
            var rect = (RectTransform)art.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = art.GetComponent<Image>();
            image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Environment/TempleCourtyardDusk.png");
            image.color = new Color(.34f, .38f, .42f, 1);
            image.raycastTarget = false;
            var title = panel.Find("Title").GetComponent<Text>();
            title.text = "V I S H A"; title.fontSize = 92; title.color = new Color(.91f, .77f, .48f);
            Place(title.rectTransform, 0, 315, 1500, 125);
            Label(panel, "Subtitle", "THE LAST GUARDIAN", 28, 235, 1000, 55);
            var story = Label(panel, "Story", "", 33, 95, 1100, 150);
            var group = story.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            var instructions = panel.Find("Instructions").GetComponent<Text>();
            instructions.text = "BUILD POISON    /    TIME YOUR BITE    /    FADE BEFORE A HEAVY HIT\n\nChoose an attack, then click the enemy. Fade acts immediately.\nEsc cancels a target. Your enemy's next move is always visible.";
            instructions.fontSize = 26;
            instructions.color = new Color(.84f, .89f, .86f);
            Place(instructions.rectTransform, 0, -125, 1400, 185);
            var start = panel.Find("StartButton").GetComponent<Button>();
            Place((RectTransform)start.transform, 0, -310, 440, 80);
            start.GetComponentInChildren<Text>().text = "ENTER THE COURTYARD";
            var sequence = panel.GetComponent<TitleSequence>();
            if (sequence == null) sequence = panel.gameObject.AddComponent<TitleSequence>();
            var data = new SerializedObject(sequence);
            data.FindProperty("story").objectReferenceValue = story;
            data.FindProperty("backdrop").objectReferenceValue = rect;
            data.FindProperty("storyGroup").objectReferenceValue = group;
            data.ApplyModifiedPropertiesWithoutUndo();
            var source = view.GetComponent<AudioSource>();
            if (source == null) source = view.gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false; source.spatialBlend = 0; source.volume = .5f;
            foreach (var button in view.GetComponentsInChildren<Button>(true))
            {
                var feedback = button.GetComponent<MenuButtonFeedback>();
                if (feedback == null) feedback = button.gameObject.AddComponent<MenuButtonFeedback>();
                var settings = new SerializedObject(feedback);
                settings.FindProperty("source").objectReferenceValue = source;
                settings.FindProperty("click").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/MenuConfirm.wav");
                settings.ApplyModifiedPropertiesWithoutUndo();
                var colors = button.colors;
                colors.highlightedColor = new Color(.78f, 1f, .88f);
                colors.selectedColor = colors.highlightedColor;
                colors.pressedColor = new Color(.55f, .75f, .65f);
                colors.fadeDuration = .12f; button.colors = colors;
            }
            var message = view.transform.Find("CompletePanel/Message").GetComponent<Text>();
            message.text = "The courtyard is clear. The Vault and Sanctum await.\nThe next chapter is still in development.";
            foreach (string name in new[] { "Player", "Enemy" })
            {
                var visual = GameObject.Find(name + "Placeholder");
                visual.transform.localScale = Vector3.one * .93f;
                var motion = visual.GetComponent<CharacterIdleMotion>();
                if (motion == null) motion = visual.AddComponent<CharacterIdleMotion>();
                var motionData = new SerializedObject(motion);
                motionData.FindProperty("health").objectReferenceValue = GameObject.Find(name).GetComponent<Health>();
                motionData.ApplyModifiedPropertiesWithoutUndo();
            }
            foreach (var component in view.GetComponentsInChildren<Component>(true))
                if (component != null && component != view.transform && PrefabUtility.IsPartOfPrefabInstance(component))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }
        static Text Label(Transform parent, string name, string text, int size, float y, float width, float height)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = size; label.text = text; label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(.9f, .91f, .86f); label.raycastTarget = false;
            Place(label.rectTransform, 0, y, width, height); return label;
        }
        static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(width, height);
        }
    }
}
