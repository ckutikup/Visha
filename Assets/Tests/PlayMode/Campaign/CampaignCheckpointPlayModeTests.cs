#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Visha.Campaign;
using Visha.UI;

/// <summary>
/// Checkpoint and menu paths in Campaign_Demo that the full-campaign tests do not
/// cover: broken or unreadable checkpoint files, a checkpoint that cannot be
/// written, Continue after losing health, and backing out of a chapter
/// introduction from the title screen and from the temple map.
///
/// Reloading the scene re-runs CampaignController.Awake, which reads the
/// checkpoint the same way a fresh launch does. It does not prove behaviour across
/// quitting and reopening a built application; that still needs a manual check.
/// </summary>
public class CampaignCheckpointPlayModeTests : CampaignSaveIsolation
{
    private const string ScenePath = "Assets/Scenes/Campaign_Demo.unity";

    private CampaignController campaign;
    private BattleScreenView screens;
    private Health playerHealth, enemyHealth;

    private static string SaveDirectory => Environment.GetEnvironmentVariable("VISHA_SAVE_DIRECTORY");
    private static string SavePath => Path.Combine(SaveDirectory, "campaign.json");

    private IEnumerator LoadScene()
    {
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        campaign = UnityEngine.Object.FindAnyObjectByType<CampaignController>();
        screens = UnityEngine.Object.FindAnyObjectByType<BattleScreenView>();
        playerHealth = GameObject.Find("Player").GetComponent<Health>();
        enemyHealth = GameObject.Find("Enemy").GetComponent<Health>();
    }

    private static void WriteCheckpointFile(string contents)
    {
        Directory.CreateDirectory(SaveDirectory);
        File.WriteAllText(SavePath, contents);
    }

    private Button Button(string path) => screens.transform.Find(path).GetComponent<Button>();
    private string Label(string path) => screens.transform.Find(path).GetComponentInChildren<Text>().text;
    private VaultArrivalView Arrival => campaign.GetComponent<VaultArrivalView>();

    private void AssertFreshTitle(string reason)
    {
        Assert.AreEqual(CampaignState.Title, campaign.State, reason);
        Assert.IsFalse(campaign.HasCheckpoint, reason);
        Assert.AreEqual(-1, campaign.HighestCompletedEncounter, reason + ": nothing may be unlocked");
        Assert.IsFalse(campaign.CanEnterEncounter(1), reason + ": encounter 2 must stay locked");
        Assert.AreEqual("Start Game", Label("TitlePanel/StartButton"), reason);
        StringAssert.Contains("could not be loaded", Label("TitlePanel/Checkpoint"));
    }

    [UnityTest]
    public IEnumerator MalformedOrEmptyCheckpoint_StartsFreshAndIsReplacedByTheNextSave()
    {
        foreach (string contents in new[] { "{ this is not json", "" })
        {
            WriteCheckpointFile(contents);
            yield return LoadScene();
            AssertFreshTitle($"checkpoint file containing \"{contents}\"");
        }

        Button("TitlePanel/StartButton").onClick.Invoke();
        yield return null;
        Assert.AreEqual(1, campaign.EncounterNumber);
        CampaignSave rewritten = CampaignSave.Load(SavePath, campaign.EncounterCount);
        Assert.IsNotNull(rewritten, "starting an encounter writes a valid checkpoint over the broken file");
        Assert.AreEqual(0, rewritten.resumeEncounter);
    }

    [UnityTest]
    public IEnumerator UnreadableCheckpoint_StartsFreshWithoutThrowing()
    {
        if (Application.platform != RuntimePlatform.OSXEditor && Application.platform != RuntimePlatform.LinuxEditor)
            Assert.Ignore("Removing read permission is only set up for macOS and Linux editors.");

        WriteCheckpointFile(JsonUtility.ToJson(new CampaignSave { highestCompleted = 2, resumeEncounter = 3 }));
        SetPermissions(SavePath, "000");
        try
        {
            yield return LoadScene();
            AssertFreshTitle("unreadable checkpoint file");
        }
        finally
        {
            SetPermissions(SavePath, "644");
        }
    }

    [UnityTest]
    public IEnumerator FailedCheckpointWrite_IsReportedOnTitle_AndThisSessionCanStillContinue()
    {
        // A regular file where the save folder should be makes every write fail.
        Directory.CreateDirectory(SaveDirectory);
        string blocker = Path.Combine(SaveDirectory, "not-a-folder");
        File.WriteAllText(blocker, "");
        Environment.SetEnvironmentVariable("VISHA_SAVE_DIRECTORY", Path.Combine(blocker, "saves"));
        yield return LoadScene();

        Button("TitlePanel/StartButton").onClick.Invoke();
        yield return null;
        enemyHealth.TakeDamage(999);
        yield return null;
        Assert.IsTrue(campaign.SaveFailed, "the failed write is recorded");
        StringAssert.Contains("could not be saved", Label("Feedback"));

        Button("VictoryPanel/Card/ReturnButton").onClick.Invoke();
        yield return null;
        Assert.AreEqual(CampaignState.Title, campaign.State);
        StringAssert.Contains("could not be saved", Label("TitlePanel/Checkpoint"));
        Assert.AreEqual("Continue", Label("TitlePanel/StartButton"), "progress is kept for the rest of this session");

        Button("TitlePanel/StartButton").onClick.Invoke();
        yield return null;
        Assert.AreEqual(2, campaign.EncounterNumber, "Continue still resumes the next encounter in this session");
    }

    [UnityTest]
    public IEnumerator Continue_RestartsTheSavedEncounterAtFullHealth()
    {
        Button("TitlePanel/StartButton").onClick.Invoke();
        yield return null;
        enemyHealth.TakeDamage(999);
        yield return null;
        Button("VictoryPanel/Card/NextButton").onClick.Invoke();
        yield return null;
        Assert.AreEqual(2, campaign.EncounterNumber);

        playerHealth.TakeDamage(20);
        enemyHealth.TakeDamage(10);
        campaign.ReturnToTitle();
        yield return LoadScene();

        Assert.AreEqual("Continue", Label("TitlePanel/StartButton"));
        Button("TitlePanel/StartButton").onClick.Invoke();
        yield return null;
        Assert.AreEqual(2, campaign.EncounterNumber, "Continue resumes the encounter that was in progress");
        Assert.AreEqual(playerHealth.MaxHealth, playerHealth.CurrentHealth, "the player starts at full health");
        Assert.AreEqual(enemyHealth.MaxHealth, enemyHealth.CurrentHealth, "the encounter starts over");
    }

    [UnityTest]
    public IEnumerator BackingOutOfAChapterIntro_LeavesTitleAndMapUsable()
    {
        // Two encounters cleared, next is the Vault Sentinel (encounter 3, which has an intro).
        WriteCheckpointFile(JsonUtility.ToJson(new CampaignSave { highestCompleted = 1, resumeEncounter = 2 }));
        yield return LoadScene();
        Assert.AreEqual("Continue", Label("TitlePanel/StartButton"));

        // From the title screen.
        Button("TitlePanel/StartButton").onClick.Invoke();
        Assert.IsTrue(Arrival.IsOpen, "Continue into the Vault shows its chapter introduction");
        Arrival.Cancel();
        yield return null;
        Assert.AreEqual(CampaignState.Title, campaign.State);
        Assert.IsTrue(Button("TitlePanel/StartButton").interactable, "Continue is usable again after Back");
        Assert.IsTrue(Button("TitlePanel/NewGame").interactable, "New Game is usable again after Back");
        Assert.IsTrue(Button("TitlePanel/TempleMap").interactable, "Temple Map is usable again after Back");

        // From the temple map.
        Button("TitlePanel/TempleMap").onClick.Invoke();
        var map = campaign.GetComponent<TempleMapView>();
        Assert.IsTrue(map.IsOpen);
        map.SelectNode(2);
        map.EnterSelected();
        Assert.IsTrue(Arrival.IsOpen);
        Arrival.Cancel();
        yield return null;
        Assert.IsTrue(map.IsOpen, "Back returns to the map, not to a locked screen");
        Assert.AreEqual(CampaignState.Title, campaign.State);

        map.SelectNode(3);
        map.EnterSelected();
        Assert.IsFalse(Arrival.IsOpen, "a locked encounter cannot be entered");
        Assert.AreEqual(CampaignState.Title, campaign.State);

        map.SelectNode(2);
        map.EnterSelected();
        Arrival.Continue();
        yield return null;
        Assert.AreEqual(CampaignState.InBattle, campaign.State);
        Assert.AreEqual(3, campaign.EncounterNumber);
        Assert.AreEqual(1, campaign.HighestCompletedEncounter, "entering from the map keeps earlier progress");
    }

    [UnityTest]
    public IEnumerator ReplayDoesNotReplaceTheFurthestCheckpoint()
    {
        WriteCheckpointFile(JsonUtility.ToJson(new CampaignSave { highestCompleted = 2, resumeEncounter = 0 }));
        yield return LoadScene();
        Assert.AreEqual(3, campaign.ResumeEncounter);
        campaign.EnterEncounter(0);
        yield return null;
        playerHealth.TakeDamage(999);
        yield return null;
        campaign.ReturnToTitle();
        yield return LoadScene();
        Assert.AreEqual(3, campaign.ResumeEncounter);
        Assert.AreEqual(2, campaign.HighestCompletedEncounter);
        campaign.ContinueCampaign();
        yield return null;
        Assert.AreEqual(4, campaign.EncounterNumber);
    }

    [UnityTest]
    public IEnumerator CompletedCampaignOffersReplayFinale()
    {
        WriteCheckpointFile(JsonUtility.ToJson(new CampaignSave { highestCompleted = 4, resumeEncounter = 4 }));
        yield return LoadScene();
        Assert.AreEqual("Replay Finale", Label("TitlePanel/StartButton"));
        StringAssert.Contains("Campaign complete", Label("TitlePanel/Checkpoint"));
        campaign.ContinueCampaign();
        yield return null;
        Assert.AreEqual(5, campaign.EncounterNumber);
        Assert.AreEqual(4, campaign.HighestCompletedEncounter);
    }

    [UnitySetUp]
    public IEnumerator LoadFreshScene()
    {
        // Tests that need a checkpoint file write it and reload the scene themselves.
        yield return LoadScene();
    }

    private static void SetPermissions(string path, string mode)
    {
        using (var chmod = System.Diagnostics.Process.Start("/bin/chmod", $"{mode} \"{path}\""))
            chmod.WaitForExit();
    }
}
#endif
