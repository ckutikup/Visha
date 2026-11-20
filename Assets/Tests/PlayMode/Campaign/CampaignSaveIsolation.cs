#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using UnityEngine.TestTools;

public class CampaignSaveIsolation
{
    private string previousDirectory, testDirectory;
    [UnitySetUp]
    public IEnumerator IsolateSave()
    {
        previousDirectory = Environment.GetEnvironmentVariable("VISHA_SAVE_DIRECTORY");
        testDirectory = Path.Combine(Path.GetTempPath(), "visha-campaign-test-" + Guid.NewGuid());
        Environment.SetEnvironmentVariable("VISHA_SAVE_DIRECTORY", testDirectory);
        yield return null;
    }
    [UnityTearDown]
    public IEnumerator RestoreSaveDirectory()
    {
        Environment.SetEnvironmentVariable("VISHA_SAVE_DIRECTORY", previousDirectory);
        if (Directory.Exists(testDirectory)) Directory.Delete(testDirectory, true);
        yield return null;
    }
}
#endif
