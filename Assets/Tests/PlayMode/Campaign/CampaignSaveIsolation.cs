#if UNITY_EDITOR
using System;
using System.IO;
using NUnit.Framework;

public class CampaignSaveIsolation
{
    private string previousDirectory, testDirectory;
    [SetUp]
    public void IsolateSave()
    {
        previousDirectory = Environment.GetEnvironmentVariable("VISHA_SAVE_DIRECTORY");
        testDirectory = Path.Combine(Path.GetTempPath(), "visha-campaign-test-" + Guid.NewGuid());
        Environment.SetEnvironmentVariable("VISHA_SAVE_DIRECTORY", testDirectory);
    }
    [TearDown]
    public void RestoreSaveDirectory()
    {
        Environment.SetEnvironmentVariable("VISHA_SAVE_DIRECTORY", previousDirectory);
        if (Directory.Exists(testDirectory)) Directory.Delete(testDirectory, true);
    }
}
#endif
