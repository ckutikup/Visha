using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Visha.Battle;

namespace Visha.UI.EditorTools
{
    public static class BattleScreenSetup
    {
        const string PrefabPath = "Assets/Prefabs/BattleScreens.prefab";
        static readonly Color Gold = new Color(.86f,.74f,.48f);
        static readonly Color Body = new Color(.86f,.89f,.86f);

        public static void BuildAssets()
        {
            var root = new GameObject("BattleScreens",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            var view=root.AddComponent<BattleScreenView>();var data=new SerializedObject(view);
            var encounter=Text(root.transform,"Encounter","",30,TextAnchor.MiddleCenter);
            Place(encounter.rectTransform,new Vector2(.5f,1),new Vector2(0,-45),new Vector2(600,60));
            Set(data,"encounterLabel",encounter);
            var feedback=Text(root.transform,"Feedback","",24,TextAnchor.MiddleCenter);
            Place(feedback.rectTransform,new Vector2(.5f,0),new Vector2(0,45),new Vector2(1100,60));
            Set(data,"feedbackLabel",feedback);
            var title=Panel(root.transform,"TitlePanel",true);Set(data,"titlePanel",title);
            Label(title.transform,"Title","VISHA: THE LAST GUARDIAN",64,300,1500);
            Label(title.transform,"Instructions","Read the enemy intention before choosing an ability.\n\nVenom Strike builds poison. Serpent's Bite consumes it for burst damage.\nFade reduces incoming damage; watch its availability before you act.\n\nChoose an ability, then click the enemy. Press Esc to cancel targeting.",30,40,1200,340);
            Set(data,"startButton",Button(title.transform,"StartButton","START",0,-300));
            var victory=Panel(root.transform,"VictoryPanel",false);Set(data,"victoryPanel",victory);
            var card=Card(victory.transform);Label(card,"Heading","VICTORY",64,160);
            Set(data,"victoryMessage",Label(card,"Message","",30,30));
            Set(data,"nextButton",Button(card,"NextButton","NEXT ENCOUNTER",0,-90));
            Set(data,"victoryRetryButton",Button(card,"VictoryRetryButton","RETRY BATTLE",0,-90));
            var back1=Button(card,"ReturnButton","RETURN TO START",0,-190);
            var defeat=Panel(root.transform,"DefeatPanel",false);Set(data,"defeatPanel",defeat);
            card=Card(defeat.transform);var heading=Label(card,"Heading","DEFEAT",64,160);heading.color=new Color(.9f,.38f,.33f);
            Set(data,"defeatMessage",Label(card,"Message","",30,30));
            Set(data,"retryButton",Button(card,"RetryButton","RETRY",0,-90));
            var back2=Button(card,"ReturnButton","RETURN TO START",0,-190);
            var complete=Panel(root.transform,"CompletePanel",true);Set(data,"completePanel",complete);
            Label(complete.transform,"Heading","DEMO COMPLETE",68,180);
            Label(complete.transform,"Message","The path to the inner sanctum is open.",32,60);
            Set(data,"restartButton",Button(complete.transform,"RestartButton","PLAY AGAIN",0,-100));
            var back3=Button(complete.transform,"ReturnButton","RETURN TO START",0,-200);
            var returns=data.FindProperty("returnButtons");returns.arraySize=3;
            returns.GetArrayElementAtIndex(0).objectReferenceValue=back1;returns.GetArrayElementAtIndex(1).objectReferenceValue=back2;returns.GetArrayElementAtIndex(2).objectReferenceValue=back3;
            data.ApplyModifiedPropertiesWithoutUndo();
            feedback.transform.SetAsLastSibling();
            title.SetActive(false);victory.SetActive(false);defeat.SetActive(false);complete.SetActive(false);
            PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Visha/Install Battle Screens")]
        public static void InstallInBattleScene()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/BattleUI_Demo.unity",OpenSceneMode.Single);
            var outcome=Object.FindFirstObjectByType<BattleOutcomeController>();
            var hud=Object.FindFirstObjectByType<BattleHUD>();
            if(!outcome || !hud) throw new System.InvalidOperationException("Battle scene needs HUD and outcome controller.");
            var view=Object.FindFirstObjectByType<BattleScreenView>();
            if(!view) view=((GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath),scene)).GetComponent<BattleScreenView>();
            var presenter=view.GetComponent<BattleScreenPresenter>();if(!presenter)presenter=view.gameObject.AddComponent<BattleScreenPresenter>();
            var data=new SerializedObject(presenter);Set(data,"view",view);Set(data,"hud",hud);Set(data,"outcome",outcome);data.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene);
        }
        public static void BuildAndInstall(){BuildAssets();InstallInBattleScene();}
        static void Set(SerializedObject data,string name,Object value)=>data.FindProperty(name).objectReferenceValue=value;
        static GameObject Panel(Transform parent,string name,bool opaque)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);
            var rect=(RectTransform)go.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            go.GetComponent<Image>().color=new Color(.03f,.05f,.06f,opaque?1:.85f);return go;
        }
        static Transform Card(Transform parent)
        {
            var go=new GameObject("Card",typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);
            Place((RectTransform)go.transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(900,540));
            go.GetComponent<Image>().color=new Color(.09f,.13f,.14f);return go.transform;
        }
        static Text Text(Transform parent,string name,string content,int size,TextAnchor align)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);
            var t=go.GetComponent<Text>();t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.fontSize=size;t.color=Body;t.alignment=align;t.text=content;t.raycastTarget=false;
            t.horizontalOverflow=HorizontalWrapMode.Wrap;t.verticalOverflow=VerticalWrapMode.Truncate;return t;
        }
        static Text Label(Transform parent,string name,string content,int size,float y,float width=820,float height=110)
        {
            var t=Text(parent,name,content,size,TextAnchor.MiddleCenter);Place(t.rectTransform,new Vector2(.5f,.5f),new Vector2(0,y),new Vector2(width,height));return t;
        }
        static Button Button(Transform parent,string name,string label,float x,float y)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image),typeof(Button));go.transform.SetParent(parent,false);
            Place((RectTransform)go.transform,new Vector2(.5f,.5f),new Vector2(x,y),new Vector2(380,76));go.GetComponent<Image>().color=new Color(.16f,.26f,.24f);
            var t=Text(go.transform,"Label",label,28,TextAnchor.MiddleCenter);t.color=Gold;t.rectTransform.anchorMin=Vector2.zero;t.rectTransform.anchorMax=Vector2.one;t.rectTransform.offsetMin=t.rectTransform.offsetMax=Vector2.zero;
            return go.GetComponent<Button>();
        }
        static void Place(RectTransform r,Vector2 anchor,Vector2 position,Vector2 size){r.anchorMin=r.anchorMax=anchor;r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=position;r.sizeDelta=size;}
    }
}
