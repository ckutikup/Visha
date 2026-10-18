using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Visha.Campaign;
using Visha.Enemies;

/// <summary>
/// Checks the shipped encounter assets against the values agreed with Anshul
/// (Docs/EnemyPattern.md) and the order Campaign_Demo plays them in.
/// </summary>
public class EncounterAssetTests
{
    private const string ScenePath = "Assets/Scenes/Campaign_Demo.unity";

    private static readonly (string path, string name, int kind, int maxHealth)[] Expected =
    {
        ("Assets/Data/Encounters/Encounter1_TempleGuardian.asset", "Temple Guardian", 0, 30),
        ("Assets/Data/Encounters/Encounter2_GuardianWarden.asset", "Guardian Warden", 0, 44),
        ("Assets/Data/Encounters/Encounter3_VaultSentinel.asset", "Vault Sentinel", 1, 46)
    };

    [Test]
    public void EncounterAssets_HoldTheAgreedValues()
    {
        foreach (var (path, name, kind, maxHealth) in Expected)
        {
            var config = AssetDatabase.LoadAssetAtPath<EncounterConfig>(path);
            Assert.IsNotNull(config, $"missing {path}");
            Assert.AreEqual(name, config.DisplayName, path);
            Assert.AreEqual(kind, (int)config.Pattern, $"{path}: 0 = TempleGuardian, 1 = VaultSentinel");
            Assert.AreEqual(maxHealth, config.MaxHealth, path);
            Assert.AreEqual(config.MaxHealth, config.StartingHealth, $"{path} should start at full health");
        }
    }

    [Test]
    public void EncounterAssets_UseBothEnemyTypes()
    {
        var first = AssetDatabase.LoadAssetAtPath<EncounterConfig>(Expected[0].path);
        var last = AssetDatabase.LoadAssetAtPath<EncounterConfig>(Expected[2].path);
        Assert.AreNotEqual(first.Pattern, last.Pattern);
    }

    [Test]
    public void CampaignScene_ListsEncountersInOrder()
    {
        string[] dependencies = AssetDatabase.GetDependencies(ScenePath, false);
        foreach (var (path, _, _, _) in Expected)
            CollectionAssert.Contains(dependencies, path, $"{ScenePath} should reference {path}");

        // The scene stores the list as GUIDs in play order.
        string scene = System.IO.File.ReadAllText(ScenePath);
        int previous = -1;
        foreach (var (path, _, _, _) in Expected)
        {
            int at = scene.IndexOf(AssetDatabase.AssetPathToGUID(path), System.StringComparison.Ordinal);
            Assert.Greater(at, previous, $"{path} is out of order in {ScenePath}");
            previous = at;
        }
    }
}
