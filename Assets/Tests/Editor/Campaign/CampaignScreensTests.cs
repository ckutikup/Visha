using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Visha.Battle;
using Visha.Campaign;
using Visha.Enemies;
using Visha.UI;

/// <summary>
/// Checks CampaignScreens against Charith's real BattleScreens prefab and the
/// screen contract in Docs/BattleUI.md.
/// </summary>
public class CampaignScreensTests
{
    private GameObject playerObject, enemyObject, systemsObject, screensObject;
    private Health playerHealth, enemyHealth;
    private BattleOutcomeController outcome;
    private CampaignController campaign;
    private CampaignScreens screens;
    private BattleScreenView view;
    private EncounterConfig[] encounters;

    [SetUp]
    public void SetUp()
    {
        playerObject = new GameObject("Player");
        playerHealth = playerObject.AddComponent<Health>();
        playerHealth.SetMaxHealth(30);

        enemyObject = new GameObject("Enemy");
        enemyHealth = enemyObject.AddComponent<Health>();
        enemyObject.AddComponent<Poison>();
        var pattern = enemyObject.AddComponent<EnemyPatternRunner>();

        encounters = new[] { Encounter("Temple Guardian", 30), Encounter("Guardian Warden", 44) };

        systemsObject = new GameObject("Systems");
        outcome = systemsObject.AddComponent<BattleOutcomeController>();
        outcome.SetCombatants(playerHealth, enemyHealth);
        campaign = systemsObject.AddComponent<CampaignController>();
        campaign.SetReferences(outcome, playerHealth, enemyHealth, pattern, encounters);

        screensObject = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BattleScreens.prefab"));
        view = screensObject.GetComponent<BattleScreenView>();
        screens = systemsObject.AddComponent<CampaignScreens>();
        screens.SetReferences(campaign, view, null);
        screens.Show(campaign.State);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(screensObject);
        Object.DestroyImmediate(systemsObject);
        Object.DestroyImmediate(playerObject);
        Object.DestroyImmediate(enemyObject);
        foreach (EncounterConfig config in encounters) if (config) Object.DestroyImmediate(config);
    }

    private static EncounterConfig Encounter(string name, int maxHealth)
    {
        var config = ScriptableObject.CreateInstance<EncounterConfig>();
        config.Set(name, EnemyPatternRunner.PatternKind.TempleGuardian, maxHealth);
        return config;
    }

    private T Field<T>(string name) where T : Object =>
        (T)new SerializedObject(view).FindProperty(name).objectReferenceValue;

    [Test]
    public void ScreensFollowCampaignState_WithEncounterCounter()
    {
        Assert.AreEqual(BattleScreen.Title, view.Screen);

        view.RequestStart();
        Assert.AreEqual(BattleScreen.Battle, view.Screen);
        Assert.AreEqual("ENCOUNTER 1 / 2", Field<Text>("encounterLabel").text);

        enemyHealth.TakeDamage(999);
        Assert.AreEqual(BattleScreen.Victory, view.Screen);
        StringAssert.StartsWith("Temple Guardian defeated.", Field<Text>("victoryMessage").text);

        view.RequestNext();
        Assert.AreEqual(BattleScreen.Battle, view.Screen);
        Assert.AreEqual("ENCOUNTER 2 / 2", Field<Text>("encounterLabel").text);

        playerHealth.TakeDamage(999);
        Assert.AreEqual(BattleScreen.Defeat, view.Screen);
        view.RequestRetry();
        Assert.AreEqual(BattleScreen.Battle, view.Screen);
        Assert.AreEqual(2, campaign.EncounterNumber);

        enemyHealth.TakeDamage(999);
        Assert.AreEqual(BattleScreen.Complete, view.Screen);
        view.RequestRestart();
        Assert.AreEqual(BattleScreen.Battle, view.Screen);
        Assert.AreEqual(1, campaign.EncounterNumber);
    }

    [Test]
    public void ReturnToStart_ShowsTitleAndHidesCounter()
    {
        view.RequestStart();
        playerHealth.TakeDamage(999);
        view.RequestReturn();

        Assert.AreEqual(BattleScreen.Title, view.Screen);
        Assert.AreEqual(CampaignState.Title, campaign.State);
        Assert.IsFalse(Field<Text>("encounterLabel").gameObject.activeSelf);
    }

    [Test]
    public void RefusedNavigation_UnlocksTheButtons()
    {
        // A missing encounter makes Start fail; the view must not stay locked.
        campaign.SetReferences(outcome, playerHealth, enemyHealth, enemyObject.GetComponent<EnemyPatternRunner>(),
                               new EncounterConfig[] { null });
        screens.SetReferences(campaign, view, null);
        LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("assign at least one encounter"));

        view.RequestStart();

        Assert.AreEqual(BattleScreen.Title, view.Screen);
        Assert.IsTrue(Field<Button>("startButton").interactable, "Start should unlock after a refused request");
        Assert.IsNotEmpty(Field<Text>("feedbackLabel").text);
    }

    [Test]
    public void Disabling_UnbindsNavigation()
    {
        screens.enabled = false;
        typeof(CampaignScreens).GetMethod("OnDisable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            .Invoke(screens, null);

        Assert.IsFalse(Field<Button>("startButton").interactable);
    }
}
