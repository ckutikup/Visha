using NUnit.Framework;
using UnityEditor;
using Visha.Campaign;
using Visha.Enemies;

/// <summary>
/// Checks the fifth encounter asset against the values in Docs/FinalBoss.md.
/// </summary>
public class FinalBossEncounterTests
{
    private const string AssetPath = "Assets/Data/Encounters/Encounter5_TheAsura.asset";

    [Test]
    public void FifthEncounterIsTheAsuraAtFullHealth()
    {
        var config = AssetDatabase.LoadAssetAtPath<EncounterConfig>(AssetPath);
        Assert.IsNotNull(config, $"missing {AssetPath}");
        Assert.AreEqual("The Asura", config.DisplayName);
        Assert.AreEqual(EnemyPatternRunner.PatternKind.Asura, config.Pattern);
        Assert.AreEqual(70, config.MaxHealth, "lowered from 76 in the balance audit (Docs/EncounterBalance.md)");
        Assert.AreEqual(config.MaxHealth, config.StartingHealth, "the boss starts at full health");
        Assert.AreEqual(0, config.OpeningOffset, "the boss opens on Recover");
    }

    [Test]
    public void PhaseTwoStartsAtThirtyFive()
    {
        var config = AssetDatabase.LoadAssetAtPath<EncounterConfig>(AssetPath);
        Assert.AreEqual(35, AsuraPattern.PhaseTwoThreshold(config.MaxHealth));
    }
}
