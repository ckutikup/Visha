using NUnit.Framework;
using UnityEngine;
using Visha.Campaign;
using Visha.Enemies;

/// <summary>
/// Locks the agreed two-enemy Vault configuration (Docs/VaultEncounter.md): which
/// enemy types, their starting health, and the opening offsets that keep the two big
/// hits on different turns. These are the values the team balances against.
/// </summary>
public class VaultEncounterTests
{
    private VaultEncounter vault;

    [SetUp]
    public void SetUp()
    {
        vault = ScriptableObject.CreateInstance<VaultEncounter>();
        vault.ApplyDefaultVault(); // CreateInstance does not run OnValidate in tests
    }

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(vault);

    [Test]
    public void Default_HasTwoEnemiesWithUniqueIds()
    {
        Assert.AreEqual(2, vault.Count);
        Assert.AreEqual(0, vault.Enemy(0).TargetId);
        Assert.AreEqual(1, vault.Enemy(1).TargetId);
        Assert.AreNotEqual(vault.Enemy(0).TargetId, vault.Enemy(1).TargetId);
        Assert.IsTrue(vault.IsValid);
    }

    [Test]
    public void Default_UsesBothExistingEnemyTypes()
    {
        Assert.AreEqual(EnemyPatternRunner.PatternKind.TempleGuardian, vault.Enemy(0).Pattern);
        Assert.AreEqual(EnemyPatternRunner.PatternKind.VaultSentinel, vault.Enemy(1).Pattern);
        Assert.AreNotEqual(vault.Enemy(0).Pattern, vault.Enemy(1).Pattern);
    }

    [Test]
    public void Default_AgreedNamesAndHealth()
    {
        Assert.AreEqual("Vault Guardian", vault.Enemy(0).ResolvedName);
        Assert.AreEqual(18, vault.Enemy(0).MaxHealth);

        Assert.AreEqual("Vault Sentinel", vault.Enemy(1).ResolvedName);
        Assert.AreEqual(26, vault.Enemy(1).MaxHealth);
    }

    [Test]
    public void Default_BothEnemiesStartAtFullHealth()
    {
        Assert.AreEqual(vault.Enemy(0).MaxHealth, vault.Enemy(0).StartingHealth);
        Assert.AreEqual(vault.Enemy(1).MaxHealth, vault.Enemy(1).StartingHealth);
    }

    [Test]
    public void Default_OpeningIntentsAreClearAndTelegraphed()
    {
        // Each enemy shows a concrete, named move on turn one — a clear intention.
        EnemyAction g = vault.Enemy(0).OpeningIntent;
        Assert.AreEqual("Strike", g.Label);
        Assert.AreEqual(4, g.Damage);
        Assert.AreEqual(EnemyActionKind.Attack, g.Kind);

        EnemyAction s = vault.Enemy(1).OpeningIntent;
        Assert.AreEqual("Shield Bash", s.Label);
        Assert.AreEqual(8, s.Damage);
        Assert.AreEqual(EnemyActionKind.Attack, s.Kind);
    }

    [Test]
    public void Default_EachEnemyHasATelegraphedBigHitInItsCycle()
    {
        Assert.IsTrue(CycleContains(vault.Enemy(0), "Heavy Blow", 9), "Guardian should telegraph Heavy Blow (9)");
        Assert.IsTrue(CycleContains(vault.Enemy(1), "Crush", 16), "Sentinel should telegraph Crush (16)");
    }

    [Test]
    public void Default_BigHitsAreStaggeredOntoDifferentTurns()
    {
        // The whole point of the opening offset: the Guardian's Heavy Blow and the
        // Sentinel's Crush must never land on the same turn.
        int guardianBigTurn = HighestDamageTurn(vault.Enemy(0));
        int sentinelBigTurn = HighestDamageTurn(vault.Enemy(1));
        Assert.AreNotEqual(guardianBigTurn, sentinelBigTurn,
            "Big hits collide — adjust an opening offset");

        // Combined incoming over the first cycle is the agreed 12 / 9 / 16 rhythm,
        // so the worst single turn is the lone Crush (16), never 9 + 16 = 25.
        Assert.AreEqual(12, vault.CombinedIncomingOnTurn(0));
        Assert.AreEqual(9, vault.CombinedIncomingOnTurn(1));
        Assert.AreEqual(16, vault.CombinedIncomingOnTurn(2));
        int worst = Mathf.Max(vault.CombinedIncomingOnTurn(0),
                     Mathf.Max(vault.CombinedIncomingOnTurn(1), vault.CombinedIncomingOnTurn(2)));
        Assert.AreEqual(16, worst);
    }

    [Test]
    public void Default_CyclePressureForcesThePlayerToAct()
    {
        // The player has 50 HP in the campaign scenes. One three-turn cycle deals 37,
        // so two cycles (74) are lethal: surviving means killing an enemy and/or
        // Fading the Crush, which is the intended difficulty.
        const int playerMaxHealth = 50;
        int cycle = vault.CombinedIncomingOnTurn(0) + vault.CombinedIncomingOnTurn(1) + vault.CombinedIncomingOnTurn(2);
        Assert.AreEqual(37, cycle);
        Assert.Less(cycle, playerMaxHealth, "one cycle alone should not be lethal");
        Assert.Greater(cycle * 2, playerMaxHealth, "two cycles of doing nothing should be");
    }

    [Test]
    public void IsValid_FalseForDuplicateIds()
    {
        vault.Enemy(1).Set(0, EnemyPatternRunner.PatternKind.VaultSentinel, 26, 2);
        Assert.IsFalse(vault.IsValid);
    }

    [Test]
    public void Placement_StartingHealthClampsAndDefaultsToFull()
    {
        var p = new VaultEncounter.EnemyPlacement();
        p.Set(0, EnemyPatternRunner.PatternKind.TempleGuardian, newMaxHealth: 20, newStartingHealth: 0);
        Assert.AreEqual(20, p.StartingHealth, "0 should mean full health");

        p.Set(0, EnemyPatternRunner.PatternKind.TempleGuardian, newMaxHealth: 20, newStartingHealth: 99);
        Assert.AreEqual(20, p.StartingHealth, "starting health should clamp to max");
    }

    // --- helpers ---

    private static bool CycleContains(VaultEncounter.EnemyPlacement e, string label, int damage)
    {
        EnemyAttackPattern p = e.BuildPattern();
        for (int t = 0; t < p.Length; t++)
        {
            EnemyAction a = p.Peek(t);
            if (a.Label == label && a.Damage == damage) return true;
        }
        return false;
    }

    private static int HighestDamageTurn(VaultEncounter.EnemyPlacement e)
    {
        EnemyAttackPattern p = e.BuildPattern();
        int bestTurn = 0, bestDamage = -1;
        for (int t = 0; t < p.Length; t++)
        {
            int d = p.Peek(t).Damage;
            if (d > bestDamage) { bestDamage = d; bestTurn = t; }
        }
        return bestTurn;
    }
}
