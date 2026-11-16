using NUnit.Framework;
using UnityEngine;

public class BossAbilityTests
{
    private const int AsuraHealth = 76;
    private const int Ruin = 16;
    private const int Cataclysm = 22;
    private GameObject playerObject;
    private GameObject bossObject;
    private VenomStrike venomStrike;
    private SerpentsBite bite;
    private Fade fade;
    private TempleBlessing blessing;
    private Health playerHealth;
    private Health bossHealth;
    private Poison bossPoison;
    
    [SetUp]
    public void SetUp()
    {
        playerObject = new GameObject("TestPlayer");
        playerHealth = playerObject.AddComponent<Health>();
        playerHealth.SetMaxHealth(50);
        venomStrike = playerObject.AddComponent<VenomStrike>();
        venomStrike.SetStacksApplied(2);
        bite = playerObject.AddComponent<SerpentsBite>();
        bite.SetDamagePerStack(4);
        fade = playerObject.AddComponent<Fade>();
        fade.SetReductionPercent(50);
        blessing = playerObject.AddComponent<TempleBlessing>();
        blessing.EnableForEncounter();

        bossObject = new GameObject("TestAsura");
        bossHealth = bossObject.AddComponent<Health>();
        bossHealth.SetMaxHealth(AsuraHealth);
        bossPoison = bossObject.AddComponent<Poison>();
        bossPoison.SetValues(5, 2);
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(playerObject);
        Object.DestroyImmediate(bossObject);
    }

    [Test]
    public void Poison_AtCap_TicksTenOnBoss_AndStacksStay()
    {
        venomStrike.TryUseOn(bossObject);
        venomStrike.TryUseOn(bossObject);
        venomStrike.TryUseOn(bossObject);

        bossPoison.Tick();

        Assert.AreEqual(66, bossHealth.CurrentHealth);
        Assert.AreEqual(5, bossPoison.PoisonStacks);
    }

    [Test]
    public void Bite_AtCap_DealsTwentyToBoss_AndClearsStacks()
    {
        venomStrike.TryUseOn(bossObject);
        venomStrike.TryUseOn(bossObject);
        venomStrike.TryUseOn(bossObject);

        bite.TryUseOn(bossObject);

        Assert.AreEqual(56, bossHealth.CurrentHealth);
        Assert.AreEqual(0, bossPoison.PoisonStacks);
    }

    [Test]
    public void Bite_CanFinishBoss_AtLowHealth()
    {
        bossHealth.TakeDamage(56);
        venomStrike.TryUseOn(bossObject);
        venomStrike.TryUseOn(bossObject);
        venomStrike.TryUseOn(bossObject);

        bite.TryUseOn(bossObject);

        Assert.IsTrue(bossHealth.IsDead);
    }

    [Test]
    public void DeadBoss_RejectsVenomAndBite()
    {
        venomStrike.TryUseOn(bossObject);
        bossHealth.TakeDamage(AsuraHealth);

        Assert.IsFalse(venomStrike.TryUseOn(bossObject));
        Assert.IsFalse(bite.TryUseOn(bossObject));
    }

    [Test]
    public void Fade_HalvesRuinAndCataclysm()
    {
        fade.TryUse();

        Assert.AreEqual(8, fade.ReduceDamage(Ruin));
        Assert.AreEqual(11, fade.ReduceDamage(Cataclysm));
    }

    [Test]
    public void Blessing_SavesPlayer_FromCataclysm()
    {
        playerHealth.TakeDamage(30);

        playerHealth.TakeDamage(Cataclysm);

        Assert.AreEqual(1, playerHealth.CurrentHealth);
        Assert.IsTrue(blessing.IsSpent);
    }

    [Test]
    public void FadedCataclysm_NotLethal_KeepsBlessing()
    {
        playerHealth.TakeDamage(38);
        fade.TryUse();

        playerHealth.TakeDamage(fade.ReduceDamage(Cataclysm));

        Assert.AreEqual(1, playerHealth.CurrentHealth);
        Assert.IsTrue(blessing.IsAvailable);
    }

    [Test]
    public void FadedCataclysm_StillLethal_UsesBlessing()
    {
        playerHealth.TakeDamage(40);
        fade.TryUse();

        playerHealth.TakeDamage(fade.ReduceDamage(Cataclysm));

        Assert.AreEqual(1, playerHealth.CurrentHealth);
        Assert.IsTrue(blessing.IsSpent);
    }

    [Test]
    public void TwinFang_SingleBoss_ResidueGoesOnBoss()
    {
        TwinFang.ApplyResidue(bossPoison, null);

        Assert.AreEqual(2, bossPoison.PoisonStacks);
    }
}