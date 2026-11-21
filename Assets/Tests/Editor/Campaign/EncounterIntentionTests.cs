using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Visha.Campaign;
using Visha.Enemies;

/// <summary>
/// Loads the shipped encounter data and checks, enemy by enemy, that what the HUD
/// shows before each enemy turn (IntentLabel, IncomingDamage) is exactly the move
/// that then executes, including the opening offsets and the Asura's phase change.
/// See Docs/EncounterBalance.md.
/// </summary>
public class EncounterIntentionTests
{
    private const string Folder = "Assets/Data/Encounters/";
    private GameObject _enemy;
    private EnemyPatternRunner _runner;

    [SetUp]
    public void SetUp()
    {
        _enemy = new GameObject("Enemy");
        _runner = _enemy.AddComponent<EnemyPatternRunner>();
    }

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(_enemy);

    [TestCase("Encounter1_TempleGuardian", "Strike", 4)]
    [TestCase("Encounter2_GuardianWarden", "Heavy Blow", 9)]
    [TestCase("Encounter3_VaultSentinel", "Recover", 0)]
    [TestCase("Encounter5_TheAsura", "Recover", 0)]
    public void SingleEnemyEncountersTelegraphTheMoveThatExecutes(string asset, string openingLabel, int openingDamage)
    {
        var config = AssetDatabase.LoadAssetAtPath<EncounterConfig>(Folder + asset + ".asset");
        Assert.IsNotNull(config, asset);
        _runner.Configure(config.Pattern, config.DisplayName, config.OpeningOffset);

        Assert.AreEqual(openingLabel, _runner.IntentLabel, $"{asset} opening move");
        Assert.AreEqual(openingDamage, _runner.IncomingDamage, $"{asset} opening damage");
        AssertTelegraphMatches(_runner, turns: 9, asset);
    }

    [Test]
    public void InnerVaultEnemiesTelegraphTheMoveThatExecutes()
    {
        var vault = AssetDatabase.LoadAssetAtPath<VaultEncounter>(Folder + "VaultEncounter_GuardianAndSentinel.asset");
        Assert.IsNotNull(vault);
        Assert.IsTrue(vault.IsValid);

        var second = new GameObject("Second");
        try
        {
            var runners = new[] { _runner, second.AddComponent<EnemyPatternRunner>() };
            for (int i = 0; i < 2; i++)
            {
                var placement = vault.Enemy(i);
                runners[i].Configure(placement.Pattern, placement.ResolvedName, placement.OpeningOffset);
                Assert.AreEqual(placement.OpeningIntent.Label, runners[i].IntentLabel, placement.ResolvedName);
                AssertTelegraphMatches(runners[i], turns: 9, placement.ResolvedName);
            }
        }
        finally
        {
            Object.DestroyImmediate(second);
        }
    }

    [Test]
    public void InnerVaultNeverStacksTheTwoHeavyHits()
    {
        var vault = AssetDatabase.LoadAssetAtPath<VaultEncounter>(Folder + "VaultEncounter_GuardianAndSentinel.asset");
        for (int turn = 0; turn < 6; turn++)
            Assert.LessOrEqual(vault.CombinedIncomingOnTurn(turn), 16, $"turn {turn + 1}: Heavy Blow and Crush landed together");
    }

    [Test]
    public void AsuraPhaseTwoTelegraphStaysHonest()
    {
        var config = AssetDatabase.LoadAssetAtPath<EncounterConfig>(Folder + "Encounter5_TheAsura.asset");
        _runner.Configure(config.Pattern, config.DisplayName, config.OpeningOffset);
        _runner.TakeTurn();
        _runner.TakeTurn(); // phase 1 telegraph is now Ruin

        // A damage step drops the boss to half: the telegraph must switch to Roar
        // before the boss acts, so the player is never hit by something not shown.
        Assert.IsTrue(_runner.UpdateBossPhase(AsuraPattern.PhaseTwoThreshold(config.MaxHealth), config.MaxHealth));
        Assert.AreEqual("Roar", _runner.IntentLabel);
        Assert.AreEqual(0, _runner.IncomingDamage);
        AssertTelegraphMatches(_runner, turns: 9, "Asura phase 2");
    }

    private static void AssertTelegraphMatches(EnemyPatternRunner runner, int turns, string what)
    {
        for (int turn = 0; turn < turns; turn++)
        {
            string shown = runner.IntentLabel;
            int shownDamage = runner.IncomingDamage;
            EnemyActionKind shownKind = runner.IntentKind;
            EnemyAction executed = runner.TakeTurn();
            Assert.AreEqual(shown, executed.Label, $"{what} turn {turn + 1}: label");
            Assert.AreEqual(shownDamage, executed.Damage, $"{what} turn {turn + 1}: damage");
            Assert.AreEqual(shownKind, executed.Kind, $"{what} turn {turn + 1}: kind");
            Assert.AreEqual(executed.Damage == 0, executed.Kind == EnemyActionKind.Defend,
                $"{what} turn {turn + 1}: 0-damage moves must show as no attack");
        }
    }
}
