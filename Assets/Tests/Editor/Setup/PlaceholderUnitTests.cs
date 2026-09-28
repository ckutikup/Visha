using NUnit.Framework;
using UnityEngine;

public class PlaceholderUnitTests
{
    private GameObject testObject;

    [SetUp]
    public void SetUp()
    {
        testObject = new GameObject("TestUnit");
        testObject.AddComponent<SpriteRenderer>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(testObject);
    }

    [Test]
    public void PlaceholderUnit_RequiresHealthComponent()
    {
        var unit = testObject.AddComponent<PlaceholderUnit>();
        Assert.IsNotNull(testObject.GetComponent<Health>(), "Health should be auto-added by RequireComponent");
    }

    [Test]
    public void PlayerSide_DefaultsCorrectly()
    {
        var unit = testObject.AddComponent<PlaceholderUnit>();
        Assert.AreEqual(UnitSide.Player, unit.side);
    }
}