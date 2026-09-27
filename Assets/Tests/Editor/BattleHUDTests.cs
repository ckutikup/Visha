using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Visha.UI;

public class BattleHUDTests
{
    private GameObject root;
    private BattleHUD hud;
    private T Field<T>(string name) where T : Object => (T)new SerializedObject(hud).FindProperty(name).objectReferenceValue;
    [SetUp] public void SetUp()
    {
        root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BattleHUD.prefab"));
        hud = root.GetComponent<BattleHUD>();
        typeof(BattleHUD).GetMethod("Awake", System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.NonPublic).Invoke(hud, null);
    }
    [TearDown] public void TearDown() => Object.DestroyImmediate(root);

    [Test] public void RequiresTargetAndEmitsOnlyOneRequestUntilNextTurn()
    {
        int calls=0; int target=-2;
        hud.AbilityRequested += (a,t) => { calls++; target=t; };
        var state=new BattleViewState { phase=BattlePhase.PlayerTurn };
        hud.Render(state); hud.RequestVenomStrike(); Assert.AreEqual(0,calls);
        Assert.IsFalse(Field<Button>("targetButton").interactable);
        Field<Button>("venomButton").onClick.Invoke();
        Assert.IsTrue(Field<Button>("targetButton").interactable);
        Assert.AreEqual(0,calls);
        Field<Button>("targetButton").onClick.Invoke();
        hud.Render(state); hud.RequestVenomStrike();
        Assert.AreEqual(1,calls); Assert.AreEqual(0,target);
        state.phase=BattlePhase.EnemyTurn; hud.Render(state);
        state.phase=BattlePhase.PlayerTurn; hud.Render(state); hud.ChooseAbility(BattleAbility.VenomStrike); hud.SelectTarget();
        Assert.AreEqual(2,calls);
    }
    [Test] public void BlocksInputDuringEnemyTurnAndAfterBattleEnds()
    {
        int calls=0; hud.AbilityRequested += (a,t)=>calls++;
        foreach(var phase in new[]{BattlePhase.Waiting,BattlePhase.EnemyTurn,BattlePhase.Victory,BattlePhase.Defeat})
        {
            hud.Render(new BattleViewState { phase=phase, poison=3 });
            hud.SelectTarget(); hud.RequestVenomStrike(); hud.RequestSerpentsBite(); hud.RequestFade();
            Assert.IsFalse(Field<Button>("fadeButton").interactable);
        }
        Assert.AreEqual(0,calls);
    }
    [Test] public void EnforcesPoisonAndFadeAvailabilityWithoutCalculatingCombat()
    {
        int calls=0; hud.AbilityRequested += (a,t)=>calls++;
        var state=new BattleViewState { phase=BattlePhase.PlayerTurn, fadeAvailable=false };
        hud.Render(state); hud.SelectTarget(); hud.RequestSerpentsBite(); hud.RequestFade();
        Assert.AreEqual(0,calls);
        state.poison=2; hud.Render(state); hud.RequestSerpentsBite(); Assert.AreEqual(1,calls);
        Assert.AreEqual("24 / 24",Field<Text>("enemyHealthLabel").text);
    }
    [Test] public void FadeRequestsSelfWithoutAnEnemySelection()
    {
        int target=99; BattleAbility ability=BattleAbility.VenomStrike;
        hud.AbilityRequested += (a,t)=>{ability=a;target=t;};
        hud.Render(new BattleViewState {phase=BattlePhase.PlayerTurn}); hud.RequestFade();
        Assert.AreEqual(BattleAbility.Fade,ability); Assert.AreEqual(-1,target);
    }
    [Test] public void ClampsInvalidDisplayValuesAndDisablesDeadTargets()
    {
        hud.AbilityRequested += (a,t)=>Assert.Fail("Dead actors must not act");
        hud.Render(new BattleViewState { phase=BattlePhase.PlayerTurn, playerHealth=90,
            enemyHealth=-3, poison=90, poisonCap=5 });
        Assert.AreEqual("30 / 30",Field<Text>("playerHealthLabel").text);
        Assert.AreEqual("0 / 24",Field<Text>("enemyHealthLabel").text);
        Assert.AreEqual("Poison  5 / 5",Field<Text>("poisonLabel").text);
        hud.RequestFade();
        hud.Render(new BattleViewState { playerMaxHealth=0,enemyMaxHealth=-1 });
        Assert.AreEqual(0,Field<Image>("playerHealthFill").rectTransform.anchorMax.x);
    }
    [Test] public void DoesNotOfferActionsWithoutAController()
    {
        hud.Render(new BattleViewState {phase=BattlePhase.PlayerTurn,poison=3}); hud.SelectTarget();
        Assert.IsFalse(Field<Button>("venomButton").interactable);
        Assert.IsFalse(Field<Button>("biteButton").interactable);
        Assert.IsFalse(Field<Button>("fadeButton").interactable);
    }
    [Test] public void CancelTargetingDoesNotSubmitOrLeaveAnEligibleTarget()
    {
        int calls=0;hud.AbilityRequested+=(a,t)=>calls++;
        hud.Render(new BattleViewState{phase=BattlePhase.PlayerTurn,poison=2});
        hud.ChooseAbility(BattleAbility.SerpentsBite);hud.CancelTargeting();
        Assert.IsFalse(Field<Button>("targetButton").interactable);Assert.AreEqual(0,calls);
        hud.ChooseAbility(BattleAbility.VenomStrike);hud.SelectTarget();Assert.AreEqual(1,calls);
    }
    [Test] public void RenderingDoesNotMutateTheControllersSnapshot()
    {
        var state=new BattleViewState{phase=BattlePhase.PlayerTurn,enemyHealth=-4,poison=17,intention=null};
        hud.Render(state);
        Assert.AreEqual(-4,state.enemyHealth);Assert.AreEqual(17,state.poison);
    }
    [Test] public void TargetingIsCancelledWhenTheEnemyDiesOrThePhaseChanges()
    {
        int calls=0;hud.AbilityRequested+=(a,t)=>calls++;
        hud.Render(new BattleViewState{phase=BattlePhase.PlayerTurn});hud.ChooseAbility(BattleAbility.VenomStrike);
        hud.Render(new BattleViewState{phase=BattlePhase.Victory,enemyHealth=0});hud.SelectTarget();
        Assert.IsFalse(Field<Button>("targetButton").interactable);Assert.AreEqual(0,calls);
    }
    [Test] public void DemoSceneHasRequiredUIReferencesAndNoMissingScripts()
    {
        string path="Assets/Scenes/BattleUI_Demo.unity";
        var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);
        bool opened=!scene.isLoaded;
        if(opened)scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path,UnityEditor.SceneManagement.OpenSceneMode.Additive);
        try
        {
            int anchors=0;
            foreach(var sceneRoot in scene.GetRootGameObjects())
            foreach(var transform in sceneRoot.GetComponentsInChildren<Transform>(true))
            {
                Assert.AreEqual(0,GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject),transform.name);
                var anchor=transform.GetComponent<WorldHUDAnchor>();
                if(anchor){anchors++;var data=new SerializedObject(anchor);Assert.IsNotNull(data.FindProperty("target").objectReferenceValue);Assert.IsNotNull(data.FindProperty("worldCamera").objectReferenceValue);}
                if(transform.name=="DeveloperPanel")Assert.IsFalse(transform.gameObject.activeSelf);
            }
            Assert.AreEqual(4,anchors);
        }
        finally{if(opened)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);}
    }
}
