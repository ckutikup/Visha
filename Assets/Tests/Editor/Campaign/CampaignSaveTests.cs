using System;
using System.IO;
using NUnit.Framework;
using Visha.Campaign;

public class CampaignSaveTests
{
    private string directory, path;
    [SetUp] public void Setup()
    {
        directory = Path.Combine(Path.GetTempPath(), "visha-save-test-" + Guid.NewGuid());
        path = Path.Combine(directory, "campaign.json");
    }
    [TearDown] public void Cleanup()
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, true);
    }
    [Test] public void CheckpointSurvivesReloadAndReplacement()
    {
        Assert.IsTrue(new CampaignSave { highestCompleted = 1, resumeEncounter = 2 }.Write(path));
        var save = CampaignSave.Load(path, 5);
        Assert.AreEqual(1, save.highestCompleted);
        Assert.AreEqual(2, save.resumeEncounter);
        Assert.IsTrue(new CampaignSave { highestCompleted = 4, resumeEncounter = 4 }.Write(path));
        Assert.AreEqual(4, CampaignSave.Load(path, 5).highestCompleted);
    }
    [Test] public void NewGameReplacesExistingCheckpoint()
    {
        new CampaignSave { highestCompleted = 3, resumeEncounter = 4 }.Write(path);
        new CampaignSave().Write(path);
        Assert.AreEqual(-1, CampaignSave.Load(path, 5).highestCompleted);
        Assert.AreEqual(0, CampaignSave.Load(path, 5).resumeEncounter);
    }
    [Test] public void MissingOrInvalidCheckpointCannotUnlockLevels()
    {
        Assert.IsNull(CampaignSave.Load(path, 5));
        foreach (var save in new[] {
            new CampaignSave { highestCompleted = -1, resumeEncounter = 4 },
            new CampaignSave { highestCompleted = 9 },
            new CampaignSave { version = 99 } })
        {
            save.Write(path);
            Assert.IsNull(CampaignSave.Load(path, 5));
        }
    }
}
