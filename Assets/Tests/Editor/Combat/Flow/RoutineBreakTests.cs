using NUnit.Framework;
using UnityEngine;
using Visha.Combat.Flow;

public class RoutineBreakTests
{
    [TestCase("Strike", true, true, false)]
    [TestCase("Heavy Blow", false, true, false)]
    [TestCase("Heavy Blow", true, false, false)]
    [TestCase("Heavy Blow", true, true, true)]
    [TestCase("Crush", true, true, true)]
    public void OnlySurvivingAGuardedMarkedAttackEarnsABoon(string intent, bool guarded, bool survived, bool earns)
    {
        var go = new GameObject("RoutineTest");
        try
        {
            var routine = go.AddComponent<RoutineBreak>();
            Assert.AreEqual(earns, routine.TryGuard(intent, guarded, survived));
            Assert.AreEqual(earns, routine.HasBoon);
            Assert.AreEqual(earns ? 1 : 0, routine.ConsumeBonus());
            Assert.AreEqual(0, routine.ConsumeBonus());
            if (earns) Assert.IsFalse(routine.TryGuard(intent, true, true));
        }
        finally { Object.DestroyImmediate(go); }
    }
}
