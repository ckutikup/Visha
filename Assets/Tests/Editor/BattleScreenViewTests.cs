using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Visha.UI;

public class BattleScreenViewTests
{
    private GameObject root;
    private BattleScreenView view;
    private T Field<T>(string name) where T : Object => (T)new SerializedObject(view).FindProperty(name).objectReferenceValue;

    [SetUp] public void SetUp()
    {
        root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BattleScreens.prefab"));
        view = root.GetComponent<BattleScreenView>();
    }
    [TearDown] public void TearDown() => Object.DestroyImmediate(root);

    [Test] public void NavigationWaitsForControllerAndRejectAllowsRetry()
    {
        int calls = 0;
        view.BindNavigation(() => calls++, null, null, null, null);
        view.ShowTitle(); view.RequestStart(); view.RequestStart();
        Assert.AreEqual(1, calls);
        Assert.AreEqual(BattleScreen.Title, view.Screen);
        view.RejectNavigation("Loading failed"); view.RequestStart();
        Assert.AreEqual(2, calls);
    }

    [Test] public void RoutesEachScreenToItsControllerCallback()
    {
        int start = 0, next = 0, retry = 0, restart = 0, back = 0;
        view.BindNavigation(() => start++, () => next++, () => retry++, () => restart++, () => back++);
        view.ShowBattle(); view.RequestStart(); view.RequestNext(); view.RequestRetry(); view.RequestRestart(); view.RequestReturn();
        Assert.AreEqual(0, start + next + retry + restart + back);
        view.ShowTitle(); view.RequestStart();
        view.ShowVictory("Sentinel"); view.RequestNext();
        view.ShowDefeat("Guardian"); view.RequestRetry();
        view.ShowComplete(); view.RequestRestart();
        view.ShowVictory("Guardian"); view.RequestReturn();
        Assert.AreEqual(1, start); Assert.AreEqual(1, next); Assert.AreEqual(1, retry);
        Assert.AreEqual(1, restart); Assert.AreEqual(1, back);
    }

    [Test] public void MissingProgressionOffersBattleRetryWithoutInventingNextEncounter()
    {
        view.BindNavigation(null, null, () => {}, null, null);
        view.ShowVictory("Sentinel");
        Assert.IsFalse(Field<Button>("nextButton").gameObject.activeSelf);
        Assert.IsTrue(Field<Button>("victoryRetryButton").gameObject.activeSelf);
        Assert.AreEqual("Sentinel defeated.", Field<Text>("victoryMessage").text);
        view.ShowTitle(); Assert.IsFalse(Field<Button>("startButton").interactable);
    }

    [Test] public void EncounterDisplayUsesSuppliedProgressOnly()
    {
        view.SetEncounter(2, 3); view.ShowBattle();
        Assert.AreEqual("ENCOUNTER 2 / 3", Field<Text>("encounterLabel").text);
        view.ShowVictory("Sentinel");
        StringAssert.Contains("Encounter 2 of 3 cleared.", Field<Text>("victoryMessage").text);
        Assert.IsFalse(Field<Text>("encounterLabel").gameObject.activeSelf);
        view.SetEncounter(0, 0); view.ShowBattle();
        Assert.IsFalse(Field<Text>("encounterLabel").gameObject.activeSelf);
    }

    [Test] public void ScreenChangeAcknowledgesNavigationButRepeatedRenderDoesNot()
    {
        int calls = 0;
        view.BindNavigation(() => calls++, null, null, null, null);
        view.ShowTitle(); view.RequestStart(); view.ShowTitle(); view.RequestStart();
        Assert.AreEqual(1, calls);
        view.ShowBattle(); view.ShowTitle(); view.RequestStart();
        Assert.AreEqual(2, calls);
    }
}
