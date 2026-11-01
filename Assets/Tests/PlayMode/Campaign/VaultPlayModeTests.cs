#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Visha.Campaign;
using Visha.Combat.Flow;
using Visha.UI;

/// <summary>
/// Plays the Vault in Campaign_Demo with the real flow and HUD: the flow switches
/// to two enemies for the Vault only, the player can target the second enemy, both
/// must fall for the demo to end, and a restart returns to one enemy.
/// </summary>
public class VaultPlayModeTests
{
    private const string ScenePath = "Assets/Scenes/Campaign_Demo.unity";

    [UnityTest]
    public IEnumerator Vault_IsPlayable_WithTwoEnemies()
    {
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;

        var campaign = Object.FindAnyObjectByType<CampaignController>();
        var flow = Object.FindAnyObjectByType<CombatFlowController>();
        var hud = Object.FindAnyObjectByType<BattleHUD>();
        var first = GameObject.Find("Enemy").GetComponent<Health>();

        campaign.StartCampaign();
        yield return null;
        Assert.IsFalse(flow.HasSecondEnemy, "solo encounters fight one enemy");
        for (int i = 0; i < 3; i++)
        {
            first.TakeDamage(999);
            yield return null;
            campaign.NextEncounter();
            yield return null;
        }

        Assert.AreEqual(4, campaign.EncounterNumber);
        Assert.IsTrue(flow.HasSecondEnemy, "the Vault fights two enemies");
        var second = GameObject.Find("SecondEnemy");
        Assert.IsNotNull(second);
        var secondPoison = second.GetComponent<Poison>();

        // Venom Strike on the second enemy through the HUD's own targeting.
        hud.ChooseAbility(BattleAbility.VenomStrike);
        hud.SelectEnemy(1);
        float deadline = Time.time + 2f;
        while (secondPoison.PoisonStacks == 0 && Time.time < deadline) yield return null;
        Assert.Greater(secondPoison.PoisonStacks, 0, "Venom Strike reached the second enemy");
        Assert.AreEqual(0, first.GetComponent<Poison>().PoisonStacks, "and only the second enemy");

        deadline = Time.time + 6f;
        while (flow.CurrentPhase != BattlePhase.PlayerTurn && Time.time < deadline) yield return null;
        Assert.AreEqual(BattlePhase.PlayerTurn, flow.CurrentPhase, "both enemies acted and the turn came back");

        first.TakeDamage(999);
        yield return null;
        Assert.AreEqual(CampaignState.InBattle, campaign.State, "the second enemy still stands");
        second.GetComponent<Health>().TakeDamage(999);
        yield return null;
        Assert.AreEqual(CampaignState.DemoComplete, campaign.State);

        campaign.RestartCampaign();
        yield return null;
        Assert.IsFalse(flow.HasSecondEnemy, "restart returns to one enemy");
    }
}
#endif
