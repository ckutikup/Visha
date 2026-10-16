using System;
using NUnit.Framework;
using Visha.Battle;
using Visha.Campaign;

public class CampaignProgressTests
{
    private CampaignProgress progress;

    [SetUp]
    public void SetUp()
    {
        progress = new CampaignProgress(3);
    }

    [Test]
    public void NewCampaign_StartsOnTitle()
    {
        Assert.AreEqual(CampaignState.Title, progress.State);
        Assert.AreEqual(1, progress.EncounterNumber);
    }

    [Test]
    public void Start_EntersFirstEncounter()
    {
        Assert.IsTrue(progress.Start());

        Assert.AreEqual(CampaignState.InBattle, progress.State);
        Assert.AreEqual(0, progress.EncounterIndex);
    }

    [Test]
    public void Victories_AdvanceThroughEncountersInOrder_ThenCompleteDemo()
    {
        progress.Start();

        for (int expected = 0; expected < 3; expected++)
        {
            Assert.AreEqual(expected, progress.EncounterIndex);
            progress.ReportOutcome(BattleOutcome.Victory);
            if (expected < 2)
            {
                Assert.AreEqual(CampaignState.Victory, progress.State);
                Assert.IsTrue(progress.Next());
            }
        }

        Assert.AreEqual(CampaignState.DemoComplete, progress.State);
        Assert.AreEqual(2, progress.EncounterIndex);
    }

    [Test]
    public void Next_IsIgnoredUntilVictory()
    {
        progress.Start();

        Assert.IsFalse(progress.Next());
        Assert.AreEqual(0, progress.EncounterIndex);
        Assert.AreEqual(CampaignState.InBattle, progress.State);
    }

    [Test]
    public void RepeatedNext_AdvancesOnlyOnce()
    {
        progress.Start();
        progress.ReportOutcome(BattleOutcome.Victory);

        Assert.IsTrue(progress.Next());
        Assert.IsFalse(progress.Next());
        Assert.AreEqual(1, progress.EncounterIndex);
    }

    [Test]
    public void Defeat_ThenRetry_StaysOnSameEncounter()
    {
        progress.Start();
        progress.ReportOutcome(BattleOutcome.Victory);
        progress.Next();

        progress.ReportOutcome(BattleOutcome.Defeat);
        Assert.AreEqual(CampaignState.Defeat, progress.State);

        Assert.IsTrue(progress.Retry());
        Assert.AreEqual(CampaignState.InBattle, progress.State);
        Assert.AreEqual(1, progress.EncounterIndex);
    }

    [Test]
    public void Retry_IsIgnoredUnlessDefeated()
    {
        progress.Start();
        Assert.IsFalse(progress.Retry());

        progress.ReportOutcome(BattleOutcome.Victory);
        Assert.IsFalse(progress.Retry());
        Assert.AreEqual(CampaignState.Victory, progress.State);
    }

    [Test]
    public void Outcome_IsIgnoredOutsideBattle()
    {
        Assert.IsFalse(progress.ReportOutcome(BattleOutcome.Victory));
        Assert.AreEqual(CampaignState.Title, progress.State);

        progress.Start();
        progress.ReportOutcome(BattleOutcome.Defeat);
        Assert.IsFalse(progress.ReportOutcome(BattleOutcome.Victory));
        Assert.AreEqual(CampaignState.Defeat, progress.State);
    }

    [Test]
    public void Restart_AfterDemoComplete_ResetsToFirstEncounter()
    {
        progress.Start();
        progress.ReportOutcome(BattleOutcome.Victory); progress.Next();
        progress.ReportOutcome(BattleOutcome.Victory); progress.Next();
        progress.ReportOutcome(BattleOutcome.Victory);

        progress.Restart();

        Assert.AreEqual(CampaignState.InBattle, progress.State);
        Assert.AreEqual(0, progress.EncounterIndex);
    }

    [Test]
    public void ReturnToTitle_ResetsIndex_AndAllowsStartAgain()
    {
        progress.Start();
        progress.ReportOutcome(BattleOutcome.Victory);
        progress.Next();

        progress.ReturnToTitle();
        Assert.AreEqual(CampaignState.Title, progress.State);
        Assert.AreEqual(0, progress.EncounterIndex);
        Assert.IsTrue(progress.Start());
    }

    [Test]
    public void StateChanged_FiresOnEachTransition()
    {
        int changes = 0;
        progress.StateChanged += _ => changes++;

        progress.Start();
        progress.ReportOutcome(BattleOutcome.Victory);
        progress.Next();
        progress.Next(); // ignored

        Assert.AreEqual(3, changes);
    }

    [Test]
    public void Campaign_RequiresAtLeastOneEncounter()
    {
        Assert.Throws<ArgumentException>(() => new CampaignProgress(0));
    }
}
