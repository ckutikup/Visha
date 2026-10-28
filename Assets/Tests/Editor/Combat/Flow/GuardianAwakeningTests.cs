using NUnit.Framework;
using Visha.Combat;

public class GuardianAwakeningTests
{
    [Test] public void CourtyardCannotChargeOrActivate()
    {
        var state=new GuardianAwakening(); state.Reset(false,true);
        for(int i=0;i<5;i++) state.EarnCharge();
        Assert.AreEqual(0,state.Charges); Assert.IsFalse(state.TryActivate());
    }
    [Test] public void AwakeningLastsTwoActionsAndAllowsOneTwinFang()
    {
        var state=new GuardianAwakening(); state.Reset(true,true);
        Assert.IsTrue(state.TryActivate()); Assert.IsFalse(state.TryActivate());
        Assert.IsTrue(state.TryUseTwinFang()); Assert.IsFalse(state.TryUseTwinFang());
        state.CompletePlayerAction(); Assert.AreEqual(1,state.ActionsRemaining);
        state.CompletePlayerAction(); Assert.IsFalse(state.Active);
        for(int i=0;i<4;i++) state.EarnCharge();
        Assert.IsFalse(state.TryActivate());
    }
    [Test] public void LaterEncountersEarnThreeChargesAndResetClearsPower()
    {
        var state=new GuardianAwakening(); state.Reset(true);
        state.EarnCharge(); state.EarnCharge(); Assert.IsFalse(state.Ready);
        state.EarnCharge(); state.EarnCharge(); Assert.AreEqual(3,state.Charges);
        Assert.IsTrue(state.TryActivate()); state.Reset(true);
        Assert.IsFalse(state.Active); Assert.IsFalse(state.Used); Assert.AreEqual(0,state.Charges);
        state.Reset(true,true); Assert.IsTrue(state.Ready);
        state.TryActivate(); state.EndBattle(); Assert.IsFalse(state.Active);
    }
}
