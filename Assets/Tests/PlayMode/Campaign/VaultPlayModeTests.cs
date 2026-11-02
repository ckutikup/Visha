#if UNITY_EDITOR
using System.Collections;
using System.IO;
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
        var presentation = Object.FindAnyObjectByType<CombatActionPresentation>();
        Assert.IsNotNull(presentation.SecondEnemyActor, "the second enemy needs a visible actor");
        var renderer = presentation.SecondEnemyActor.GetComponent<SpriteRenderer>();
        Assert.IsTrue(renderer.gameObject.activeInHierarchy);
        Assert.AreEqual("VaultSentinel", renderer.sprite.name);
        var firstVisual = GameObject.Find("EnemyPlaceholder").GetComponent<CharacterIdleMotion>();
        Assert.Greater(presentation.SecondEnemyActor.HomePosition.x, firstVisual.HomePosition.x + 2f);


        yield return Capture("inner-vault");

        // Venom Strike on the second enemy through the HUD's own targeting.
        hud.ChooseAbility(BattleAbility.VenomStrike);
        hud.SelectEnemy(1);
        float deadline = Time.time + 2f;
        while (secondPoison.PoisonStacks == 0 && Time.time < deadline) yield return null;
        Assert.Greater(secondPoison.PoisonStacks, 0, "Venom Strike reached the second enemy");
        Assert.AreEqual(0, first.GetComponent<Poison>().PoisonStacks, "and only the second enemy");
        Assert.AreSame(presentation.SecondEnemyActor, presentation.ActiveEnemy, "the strike animation must use the selected enemy");

        deadline = Time.time + 6f;
        while (flow.CurrentPhase != BattlePhase.PlayerTurn && Time.time < deadline) yield return null;
        Assert.AreEqual(BattlePhase.PlayerTurn, flow.CurrentPhase, "both enemies acted and the turn came back");

        var player = GameObject.Find("Player").GetComponent<Health>();
        player.TakeDamage(999);
        yield return null;
        campaign.RetryEncounter();
        yield return null;
        Assert.AreEqual(26, second.GetComponent<Health>().CurrentHealth);
        Assert.AreEqual(0, secondPoison.PoisonStacks);
        Assert.AreEqual(Vector3.zero, presentation.SecondEnemyActor.ActionOffset);
        Assert.AreEqual(1, System.Array.FindAll(Object.FindObjectsByType<CharacterIdleMotion>(FindObjectsSortMode.None),
            actor => actor.name == "SecondEnemyVisual").Length);

        first.TakeDamage(999);
        yield return null;
        Assert.AreEqual(CampaignState.InBattle, campaign.State, "the second enemy still stands");
        hud.ChooseAbility(BattleAbility.VenomStrike);
        hud.SelectEnemy(1);
        deadline = Time.time + 2f;
        while (secondPoison.PoisonStacks == 0 && Time.time < deadline) yield return null;
        Assert.Greater(secondPoison.PoisonStacks, 0, "the surviving enemy remains targetable");
        deadline = Time.time + 6f;
        while (flow.CurrentPhase != BattlePhase.PlayerTurn && Time.time < deadline) yield return null;
        Assert.AreEqual(BattlePhase.PlayerTurn, flow.CurrentPhase);

        yield return Capture("inner-vault-survivor");
        second.GetComponent<Health>().TakeDamage(999);
        yield return null;
        Assert.AreEqual(CampaignState.DemoComplete, campaign.State);

        campaign.RestartCampaign();
        yield return null;
        Assert.IsFalse(flow.HasSecondEnemy, "restart returns to one enemy");
        Assert.IsFalse(presentation.SecondEnemyActor.gameObject.activeSelf);
        Assert.AreEqual(Vector3.zero, firstVisual.ActionOffset);
    }
    private IEnumerator Capture(string name)
    {
        string folder=System.Environment.GetEnvironmentVariable("VISHA_UI_CAPTURE_DIR");
        if(string.IsNullOrEmpty(folder)) folder=Path.Combine(Path.GetTempPath(), "VishaBattleCaptures");
        Directory.CreateDirectory(folder);
        var camera=Camera.main;
        foreach(var size in new[]{new Vector2Int(1280,720),new Vector2Int(1024,768),new Vector2Int(1920,1080)})
        {
            var rt=new RenderTexture(size.x,size.y,24); camera.targetTexture=rt;
            foreach(var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if(canvas.isRootCanvas) {canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;}
            yield return null; Canvas.ForceUpdateCanvases(); camera.Render();
            var before=RenderTexture.active;RenderTexture.active=rt;
            var image=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,size.x,size.y),0,0);image.Apply();
            File.WriteAllBytes(Path.Combine(folder,$"{name}-{size.x}x{size.y}.png"),image.EncodeToPNG());
            Object.Destroy(image);RenderTexture.active=before;camera.targetTexture=null;rt.Release();Object.Destroy(rt);
        }
    }
}
#endif
