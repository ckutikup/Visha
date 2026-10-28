#if UNITY_EDITOR
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Visha.Campaign;
using Visha.Combat.Flow;
using Visha.Enemies;
using Visha.UI;

public class VaultAwakeningPlayModeTests
{
    [UnityTest] public IEnumerator VaultUnlockActivationTwinFangAndRetry()
    {
        yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/Scenes/Campaign_Demo.unity",new LoadSceneParameters(LoadSceneMode.Single));
        yield return null;
        var campaign=Object.FindAnyObjectByType<CampaignController>();
        var flow=Object.FindAnyObjectByType<CombatFlowController>();
        var hud=Object.FindAnyObjectByType<BattleHUD>();
        var enemy=Object.FindAnyObjectByType<EnemyPatternRunner>().GetComponent<Health>();
        var poison=enemy.GetComponent<Poison>();
        var actor=GameObject.Find("PlayerPlaceholder").GetComponent<SpriteRenderer>();
        var original=actor.sprite;
        campaign.StartCampaign(); Assert.IsFalse(flow.Awakening.Unlocked);
        enemy.TakeDamage(999); campaign.NextEncounter(); enemy.TakeDamage(999);
        var map=campaign.GetComponent<TempleMapView>(); map.Open(); map.SelectNode(2); map.EnterSelected();
        var arrival=campaign.GetComponent<VaultArrivalView>(); Assert.IsTrue(arrival.IsOpen);
        yield return Capture("vault-arrival");
        arrival.Continue(); yield return null;
        Assert.AreEqual(3,campaign.EncounterNumber); Assert.IsTrue(flow.Awakening.Ready);
        Assert.AreNotEqual(original,actor.sprite,"Vault armor must be loaded");
        yield return Capture("vault-armor");
        hud.RequestAwakening(); Assert.IsTrue(flow.Awakening.Active);
        Assert.AreEqual(BattlePhase.PlayerTurn,flow.CurrentPhase);
        hud.RequestAwakening(); Assert.AreEqual(2,flow.Awakening.ActionsRemaining);
        yield return Capture("vault-awakened");
        hud.ChooseAbility(BattleAbility.VenomStrike); hud.SelectTarget();
        yield return new WaitForSeconds(2.5f);
        Assert.AreEqual(1,flow.Awakening.ActionsRemaining);
        Assert.AreEqual(2,poison.PoisonStacks);
        hud.ChooseAbility(BattleAbility.SerpentsBite); hud.SelectTarget();
        yield return new WaitForSeconds(2.5f);
        Assert.AreEqual(2,poison.PoisonStacks,"Twin Fang leaves poison after consuming it");
        Assert.IsFalse(flow.Awakening.Active);
        GameObject.Find("Player").GetComponent<Health>().TakeDamage(999);
        campaign.RetryEncounter(); yield return null;
        Assert.IsTrue(flow.Awakening.Ready); Assert.IsFalse(arrival.IsOpen);
        Assert.AreEqual(0,poison.PoisonStacks);
        campaign.ReturnToTitle(); yield return null;
        Assert.IsFalse(flow.Awakening.Unlocked); Assert.AreEqual(original,actor.sprite);
    }
    private IEnumerator Capture(string name)
    {
        string folder=System.Environment.GetEnvironmentVariable("VISHA_UI_CAPTURE_DIR");
        if(string.IsNullOrEmpty(folder)) yield break;
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
