#if UNITY_EDITOR
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Visha.UI;

public class TwoEnemyHUDPlayModeTests
{
    [UnityTest] public IEnumerator RendersTwoEnemiesAtSupportedSizes()
    {
        var canvasObject = new GameObject("PreviewCanvas", typeof(Canvas), typeof(CanvasScaler));
        var canvas = canvasObject.GetComponent<Canvas>();
        var cameraObject = new GameObject("PreviewCamera", typeof(Camera));
        var camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.025f, .045f, .055f);
        canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
        var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/BattleHUD.prefab"), canvas.transform);
        var hud = root.GetComponent<BattleHUD>();
        int calls = 0, target = -1;
        hud.AbilityRequested += (a, id) => { calls++; target = id; };
        var rect = root.GetComponent<RectTransform>();
        rect.localScale = Vector3.one; rect.pivot = new Vector2(.5f, .5f);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
        var state = new BattleViewState { phase = BattlePhase.PlayerTurn, enemies = new[] {
            new EnemyViewState { targetId = 10, name = "Temple Guardian", health = 30, maxHealth = 30, intention = "Heavy Blow", incomingDamage = 9 },
            new EnemyViewState { targetId = 20, name = "Ancient Sentinel of the Inner Temple Vault", health = 8, maxHealth = 46, poison = 5, intention = "Recover" }
        }};
        try
        {
            hud.Render(state);
            foreach (var size in new[] { new Vector2Int(1280,720), new Vector2Int(1920,1080), new Vector2Int(1024,768) })
            {
                var rt = new RenderTexture(size.x, size.y, 24);
                camera.targetTexture = rt;
                yield return null; Canvas.ForceUpdateCanvases(); camera.Render();
                var panels = root.GetComponentsInChildren<EnemyTargetPanel>();
                Assert.AreEqual(2, panels.Length);
                var cornersA = new Vector3[4]; var cornersB = new Vector3[4];
                ((RectTransform)panels[0].transform).GetWorldCorners(cornersA);
                ((RectTransform)panels[1].transform).GetWorldCorners(cornersB);
                Assert.Less(camera.WorldToViewportPoint(cornersA[2]).x, camera.WorldToViewportPoint(cornersB[0]).x);
                foreach (var panel in panels)
                {
                    var corners = new Vector3[4]; ((RectTransform)panel.transform).GetWorldCorners(corners);
                    foreach (var corner in corners)
                    {
                        var pos = camera.WorldToViewportPoint(corner);
                        Assert.That(pos.x, Is.InRange(0f,1f)); Assert.That(pos.y, Is.InRange(0f,1f));
                    }
                }
                string folder = System.Environment.GetEnvironmentVariable("VISHA_UI_CAPTURE_DIR");
                if (!string.IsNullOrEmpty(folder))
                {
                    Directory.CreateDirectory(folder);
                    var previous = RenderTexture.active; RenderTexture.active = rt;
                    var texture = new Texture2D(size.x, size.y, TextureFormat.RGB24, false);
                    texture.ReadPixels(new Rect(0,0,size.x,size.y),0,0); texture.Apply();
                    File.WriteAllBytes(Path.Combine(folder, $"two-enemy-{size.x}x{size.y}.png"), texture.EncodeToPNG());
                    Object.Destroy(texture); RenderTexture.active = previous;
                }
                camera.targetTexture = null; rt.Release(); Object.Destroy(rt);
            }
            hud.ChooseAbility(BattleAbility.SerpentsBite);
            root.GetComponentsInChildren<EnemyTargetPanel>()[0].GetComponent<Button>().onClick.Invoke();
            Assert.AreEqual(0, calls);
            root.GetComponentsInChildren<EnemyTargetPanel>()[1].GetComponent<Button>().onClick.Invoke();
            Assert.AreEqual(1, calls); Assert.AreEqual(20, target);
            state.enemies = null; state.phase = BattlePhase.Waiting; hud.Render(state);
            Assert.AreEqual(0, root.GetComponentsInChildren<EnemyTargetPanel>().Length);
        }
        finally { Object.Destroy(canvasObject); Object.Destroy(cameraObject); }
        yield return null;
    }
}
#endif
