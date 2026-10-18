using NUnit.Framework;
using UnityEngine;

public class FadeTests
{
    private GameObject playerObject;
    private Fade fade;
    [SetUp]
    public void SetUp()
    {
        playerObject = new GameObject("TestPlayer");
        fade = playerObject.AddComponent<Fade>();
        fade.SetReductionPercent(50);
    }
    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(playerObject);
    }
    [Test]
    public void TryUse_ActivatesFade()
    {
        bool used = fade.TryUse();

        Assert.IsTrue(used);
        Assert.IsTrue(fade.IsActive);
        Assert.IsFalse(fade.CanUse);
    }

    [Test]
    public void TryUse_TwoTurnsInARow_IsRejected()
    {
        fade.TryUse();
        fade.EndEnemyPhase();

        bool usedAgain = fade.TryUse();

        Assert.IsFalse(usedAgain);
        Assert.IsFalse(fade.IsActive);
    }
    [Test]
    public void OtherAbilityUsed_MakesFadeAvailableAgain()
    {
        fade.TryUse();
        fade.EndEnemyPhase();
        fade.OnOtherAbilityUsed();

        bool usedAgain = fade.TryUse();

        Assert.IsTrue(usedAgain);
    }
    [Test]
    public void ReduceDamage_HalvesWhileActive_RoundsDown()
    {
        fade.TryUse();

        Assert.AreEqual(5, fade.ReduceDamage(10));
        Assert.AreEqual(3, fade.ReduceDamage(7));
    }
    [Test]
    public void ReduceDamage_NoChange_WhenNotActive()
    {
        Assert.AreEqual(10, fade.ReduceDamage(10));
    }

    [Test]
    public void EndEnemyPhase_StopsReduction()
    {
        fade.TryUse();
        fade.EndEnemyPhase();

        Assert.AreEqual(10, fade.ReduceDamage(10));
    }

    [Test]
    public void ResetFade_ClearsState()
    {
        fade.TryUse();

        fade.ResetFade();

        Assert.IsFalse(fade.IsActive);
        Assert.IsTrue(fade.CanUse);
    }
    [Test]
    public void OnFadeChanged_FiresOnUseAndReset()
    {
        int changeCount = 0;
        fade.OnFadeChanged += () =>
        {
            changeCount++;
        };

        fade.TryUse();
        fade.ResetFade();

        Assert.AreEqual(2, changeCount);
    }
    [Test]
    public void InvalidReduction_IsClamped()
    {
        fade.SetReductionPercent(150);
        Assert.AreEqual(100, fade.ReductionPercent);

        fade.SetReductionPercent(-20);
        Assert.AreEqual(0, fade.ReductionPercent);
    }
}