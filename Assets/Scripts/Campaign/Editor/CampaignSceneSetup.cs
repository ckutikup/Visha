using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Visha.Battle;
using Visha.Enemies;

namespace Visha.Campaign.EditorTools
{
    /// <summary>
    /// Editor-only helper that adds the campaign to a scene already wired by
    /// Visha > Setup Combat Flow: creates the three encounter configs (if
    /// missing), a Campaign object with CampaignController, and a CampaignCanvas
    /// with the title, victory, defeat and end-of-demo screens.
    ///
    /// Safe to re-run: the Campaign object is reused, CampaignCanvas is rebuilt,
    /// and existing encounter assets are never overwritten.
    /// </summary>
    public static class CampaignSceneSetup
    {
        private const string SourceScenePath = "Assets/Scenes/BattleUI_Demo.unity";
        private const string CampaignScenePath = "Assets/Scenes/Campaign_Demo.unity";
        private const string EncounterFolder = "Assets/Data/Encounters";
        private const string CampaignName = "Campaign";
        private const string CanvasName = "CampaignCanvas";

        // Placeholder encounters until the enemy configurations are finalised.
        // Only the Temple Guardian pattern exists so far, so difficulty rises through health.
        private static readonly (string file, string name, int maxHealth)[] DefaultEncounters =
        {
            ("Encounter1_TempleGuardian", "Temple Guardian", 24),
            ("Encounter2_GuardianSentinel", "Guardian Sentinel", 30),
            ("Encounter3_GuardianWarden", "Guardian Warden", 36)
        };

        private static readonly Color Overlay = new Color(0.03f, 0.05f, 0.06f, 0.94f);
        private static readonly Color Dim = new Color(0.02f, 0.03f, 0.04f, 0.72f);
        private static readonly Color Card = new Color(0.09f, 0.13f, 0.14f, 0.98f);
        private static readonly Color Gold = new Color(0.86f, 0.74f, 0.48f, 1f);
        private static readonly Color Venom = new Color(0.45f, 0.91f, 0.68f, 1f);
        private static readonly Color Blood = new Color(0.90f, 0.38f, 0.33f, 1f);
        private static readonly Color Body = new Color(0.86f, 0.89f, 0.86f, 1f);
        private static readonly Color ButtonColor = new Color(0.16f, 0.26f, 0.24f, 1f);

        private const string Instructions =
            "<b>HOW TO PLAY</b>\n\n" +
            "<b>Enemy intention</b>  The panel above the enemy shows its next move and the damage it will deal. Read it before you act.\n\n" +
            "<b>Venom Strike</b>  Choose it, then click the enemy. Adds poison stacks up to the cap. Poison damages the enemy after each of its turns.\n\n" +
            "<b>Serpent's Bite</b>  Needs poison on the enemy. Deals heavy damage plus a bonus for each stack, then uses up all stacks.\n\n" +
            "<b>Fade</b>  Slip out of reach of the enemy's next attack. Save it for a telegraphed heavy hit.\n\n" +
            "Defeat three guardians to finish the demo. If you fall, retry the encounter.";

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
                if (root.name == CanvasName) Undo.DestroyObjectImmediate(root);
            }
            BuildScreens(scene, campaign);

            Undo.CollapseUndoOperations(undoGroup);
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = campaignObject;
            Debug.Log($"Campaign Setup: wired '{scene.name}' with {encounters.Count} encounters. Save the scene to keep it.");
            return null;
        }

        private static List<EncounterConfig> LoadOrCreateEncounters()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Data")) AssetDatabase.CreateFolder("Assets", "Data");
            if (!AssetDatabase.IsValidFolder(EncounterFolder)) AssetDatabase.CreateFolder("Assets/Data", "Encounters");

            var result = new List<EncounterConfig>();
            foreach (var (file, name, maxHealth) in DefaultEncounters)
            {
                string path = $"{EncounterFolder}/{file}.asset";
                EncounterConfig config = AssetDatabase.LoadAssetAtPath<EncounterConfig>(path);
                if (config == null)
                {
                    config = ScriptableObject.CreateInstance<EncounterConfig>();
                    config.Set(name, EnemyPatternRunner.PatternKind.TempleGuardian, maxHealth);
                    AssetDatabase.CreateAsset(config, path);
                }
                result.Add(config);
            }
            AssetDatabase.SaveAssets();
            return result;
        }

        private static void BuildScreens(Scene scene, CampaignController campaign)
        {
            var canvasObject = new GameObject(CanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            SceneManager.MoveGameObjectToScene(canvasObject, scene);
            Undo.RegisterCreatedObjectUndo(canvasObject, "Create Campaign Canvas");

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // above the BattleHUD
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            Transform root = canvasObject.transform;

            Text encounterLabel = AddText(root, "EncounterLabel", "ENCOUNTER 1 / 3", 30, Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            Place(encounterLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0, -36), new Vector2(600, 50));

            // Title / tutorial
            GameObject title = AddPanel(root, "TitleScreen", Overlay);
            Place(AddText(title.transform, "Title", "VISHA: THE LAST GUARDIAN", 76, Gold, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform,
                  new Vector2(0.5f, 0.5f), new Vector2(0, 390), new Vector2(1600, 100));
            Place(AddText(title.transform, "Subtitle", "The temple's guardians have turned. Three stand between you and the inner sanctum.", 30, Body, TextAnchor.MiddleCenter).rectTransform,
                  new Vector2(0.5f, 0.5f), new Vector2(0, 310), new Vector2(1500, 50));
            Text instructions = AddText(title.transform, "Instructions", Instructions, 28, Body, TextAnchor.UpperLeft);
            Place(instructions.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(1180, 520));
            Button start = AddButton(title.transform, "StartButton", "START", new Vector2(0, -380));

            // Victory
            GameObject victory = AddPanel(root, "VictoryScreen", Dim);
            GameObject victoryCard = AddCard(victory.transform, "Card");
            Place(AddText(victoryCard.transform, "Heading", "VICTORY", 68, Venom, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform,
                  new Vector2(0.5f, 0.5f), new Vector2(0, 140), new Vector2(760, 90));
            Text victoryMessage = AddText(victoryCard.transform, "Message", "", 30, Body, TextAnchor.MiddleCenter);
            Place(victoryMessage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(760, 110));
            Button next = AddButton(victoryCard.transform, "NextButton", "NEXT", new Vector2(0, -130));

            // Defeat
            GameObject defeat = AddPanel(root, "DefeatScreen", Dim);
            GameObject defeatCard = AddCard(defeat.transform, "Card");
            Place(AddText(defeatCard.transform, "Heading", "DEFEAT", 68, Blood, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform,
                  new Vector2(0.5f, 0.5f), new Vector2(0, 140), new Vector2(760, 90));
            Text defeatMessage = AddText(defeatCard.transform, "Message", "", 30, Body, TextAnchor.MiddleCenter);
            Place(defeatMessage.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(760, 110));
            Button retry = AddButton(defeatCard.transform, "RetryButton", "RETRY", new Vector2(-170, -130));
            Button defeatToTitle = AddButton(defeatCard.transform, "ReturnToStartButton", "RETURN TO START", new Vector2(170, -130));

            // End of demo
            GameObject end = AddPanel(root, "EndScreen", Overlay);
            Place(AddText(end.transform, "Heading", "DEMO COMPLETE", 76, Gold, TextAnchor.MiddleCenter, FontStyle.Bold).rectTransform,
                  new Vector2(0.5f, 0.5f), new Vector2(0, 170), new Vector2(1400, 100));
            Place(AddText(end.transform, "Message", "All three guardians have fallen. The path to the inner sanctum is open.\nThe guardian boss awaits in the next milestone.", 32, Body, TextAnchor.MiddleCenter).rectTransform,
                  new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(1400, 120));
            Button restart = AddButton(end.transform, "RestartButton", "RESTART", new Vector2(-170, -120));
            Button endToTitle = AddButton(end.transform, "ReturnToStartButton", "RETURN TO START", new Vector2(170, -120));

            CampaignScreens screens = canvasObject.AddComponent<CampaignScreens>();
            SerializedObject data = new SerializedObject(screens);
            data.FindProperty("campaign").objectReferenceValue = campaign;
            data.FindProperty("titleScreen").objectReferenceValue = title;
            data.FindProperty("victoryScreen").objectReferenceValue = victory;
            data.FindProperty("defeatScreen").objectReferenceValue = defeat;
            data.FindProperty("endScreen").objectReferenceValue = end;
            data.FindProperty("encounterLabel").objectReferenceValue = encounterLabel;
            data.FindProperty("victoryMessage").objectReferenceValue = victoryMessage;
            data.FindProperty("defeatMessage").objectReferenceValue = defeatMessage;
            data.FindProperty("startButton").objectReferenceValue = start;
            data.FindProperty("nextButton").objectReferenceValue = next;
            data.FindProperty("retryButton").objectReferenceValue = retry;
            data.FindProperty("restartButton").objectReferenceValue = restart;
            SerializedProperty titleButtons = data.FindProperty("titleButtons");
            titleButtons.arraySize = 2;
            titleButtons.GetArrayElementAtIndex(0).objectReferenceValue = defeatToTitle;
            titleButtons.GetArrayElementAtIndex(1).objectReferenceValue = endToTitle;
            data.ApplyModifiedPropertiesWithoutUndo();

            victory.SetActive(false);
            defeat.SetActive(false);
            end.SetActive(false);
        }

        private static GameObject AddPanel(Transform parent, string name, Color color)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            var rect = (RectTransform)panel.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = color; // also blocks clicks on the HUD underneath
            return panel;
        }

        private static GameObject AddCard(Transform parent, string name)
        {
            var card = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Outline));
            card.transform.SetParent(parent, false);
            Place((RectTransform)card.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860, 440));
            card.GetComponent<Image>().color = Card;
            var outline = card.GetComponent<Outline>();
            outline.effectColor = new Color(Gold.r, Gold.g, Gold.b, 0.6f);
            outline.effectDistance = new Vector2(2, -2);
            return card;
        }

        private static Text AddText(Transform parent, string name, string content, int size, Color color,
                                    TextAnchor alignment, FontStyle style = FontStyle.Normal)
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            var text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = content;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.supportRichText = true;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Button AddButton(Transform parent, string name, string label, Vector2 position)
        {
            var buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            Place((RectTransform)buttonObject.transform, new Vector2(0.5f, 0.5f), position, new Vector2(300, 76));
            var image = buttonObject.GetComponent<Image>();
            image.color = ButtonColor;
            var button = buttonObject.GetComponent<Button>();
            Text text = AddText(buttonObject.transform, "Label", label, 28, Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            RectTransform textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;
            return button;
        }

        private static void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, anchor.y >= 1f ? 1f : 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
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
