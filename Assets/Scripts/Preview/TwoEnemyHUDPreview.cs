using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Visha.UI;

namespace Visha.Preview
{
    public sealed class TwoEnemyHUDPreview : MonoBehaviour
    {
        private BattleHUD hud;
        private BattleViewState state;
        private Transform[] actors;
        private Vector3[] homes;
        private SpriteRenderer[] sprites;
        private Coroutine sequence;
        private void Start()
        {
            hud = GetComponent<BattleHUD>(); hud.AbilityRequested += RecordRequest;
            var screens = Object.FindAnyObjectByType<BattleScreenView>();
            if (screens) { screens.SetHUD(hud); screens.ShowBattle(); }
            actors = new[] { GameObject.Find("EnemyPlaceholder").transform, GameObject.Find("SentinelPreview").transform };
            homes = new[] { actors[0].position, actors[1].position };
            sprites = new[] { actors[0].GetComponent<SpriteRenderer>(), actors[1].GetComponent<SpriteRenderer>() };
            var caption = new GameObject("PreviewCaption",typeof(RectTransform),typeof(Text));
            caption.transform.SetParent(hud.transform,false);
            var text = caption.GetComponent<Text>(); text.font = hud.GetComponentInChildren<Text>(true).font;
            text.fontSize = 20; text.text = "UI PREVIEW · NO COMBAT DAMAGE";
            text.alignment = TextAnchor.MiddleLeft; text.raycastTarget = false;
            var rect = text.rectTransform; rect.anchorMin = rect.anchorMax = new Vector2(0,1);
            rect.pivot = new Vector2(0,1); rect.anchoredPosition = new Vector2(24,-20); rect.sizeDelta = new Vector2(480,40);
            ResetPreview();
        }
        private void OnDestroy() { if (hud) hud.AbilityRequested -= RecordRequest; }
        private void RestoreFormation()
        {
            if (actors == null) return;
            for (int i=0;i<actors.Length;i++)
            {
                actors[i].position = homes[i];
                sprites[i].color = state != null && state.enemies[i].health <= 0 ? new Color(.3f,.3f,.3f,1) : Color.white; sprites[i].sortingOrder = 10;
            }
        }
        private void ResetPreview()
        {
            if (sequence != null) StopCoroutine(sequence);
            sequence = null; state = null; RestoreFormation();
            state = new BattleViewState { phase = BattlePhase.PlayerTurn, playerHealth = 50, playerMaxHealth = 50, enemies = new[] {
                new EnemyViewState { targetId = 10, name = "Temple Guardian", health = 30, maxHealth = 30, intention = "Heavy Blow", incomingDamage = 9 },
                new EnemyViewState { targetId = 20, name = "Vault Sentinel", health = 12, maxHealth = 46, poison = 5, intention = "Shield Bash", incomingDamage = 8 }
            }};
            hud.Render(new BattleViewState { phase = BattlePhase.Waiting }); hud.Render(state);
            hud.SetFeedback("Select an enemy · Space previews attacks · R resets");
        }
        private void RecordRequest(BattleAbility ability, int target)
        {
            state.phase = BattlePhase.Resolving; hud.Render(state);
            hud.SetFeedback($"Preview: {ability} selected · no damage applied");
            sequence = StartCoroutine(ShowEnemyPhase());
        }
        private IEnumerator ShowEnemyPhase()
        {
            yield return new WaitForSeconds(.6f);
            for (int i=0;i<actors.Length;i++)
            {
                if (state.enemies[i].health <= 0) continue;
                state.phase = BattlePhase.EnemyTurn; state.actingEnemyId = state.enemies[i].targetId; hud.Render(state);
                hud.SetFeedback($"Preview: {state.enemies[i].name} steps forward");
                int other = 1-i;
                var front = homes[0]; var rear = homes[1] + Vector3.up*.18f;
                sprites[i].sortingOrder = 12; sprites[other].sortingOrder = 10;
                sprites[other].color = new Color(.75f,.8f,.82f,1);
                yield return MovePair(i,front,other,rear,.38f);
                yield return new WaitForSeconds(.25f);
                if (state.enemies[i].incomingDamage > 0)
                {
                    yield return MovePair(i,front+Vector3.left*.65f,other,rear,.16f);
                    yield return MovePair(i,front,other,rear,.23f);
                }
                yield return new WaitForSeconds(.25f);
                yield return MovePair(0,homes[0],1,homes[1],.36f);
                RestoreFormation();
            }
            state.phase = BattlePhase.PlayerTurn; state.actingEnemyId = -1; hud.Render(state);
            hud.SetFeedback("Your target is preserved · choose an ability"); sequence = null;
        }
        private IEnumerator MovePair(int first, Vector3 destination, int second, Vector3 otherDestination, float duration)
        {
            var from = actors[first].position; var otherFrom = actors[second].position;
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime; float t = Mathf.SmoothStep(0,1,elapsed/duration);
                actors[first].position = Vector3.Lerp(from,destination,t);
                actors[second].position = Vector3.Lerp(otherFrom,otherDestination,t);
                yield return null;
            }
        }
        private void Update()
        {
            var keys = Keyboard.current; if (keys == null || state == null) return;
            if (keys.rKey.wasPressedThisFrame) { ResetPreview(); return; }
            if (sequence != null) return;
            if (keys.spaceKey.wasPressedThisFrame)
            {
                state.phase = BattlePhase.Resolving; hud.Render(state);
                sequence = StartCoroutine(ShowEnemyPhase()); return;
            }
            if (keys.kKey.wasPressedThisFrame)
            {
                for (int i=0;i<state.enemies.Length;i++)
                    if (state.enemies[i].targetId == hud.SelectedEnemyId) { state.enemies[i].health = 0; sprites[i].color = new Color(.3f,.3f,.3f,1); }
                hud.Render(state);
            }
            if (keys.tKey.wasPressedThisFrame)
            {
                state.phase = state.phase == BattlePhase.PlayerTurn ? BattlePhase.EnemyTurn : BattlePhase.PlayerTurn;
                state.actingEnemyId = -1; hud.Render(state);
            }
            if (keys.lKey.wasPressedThisFrame)
            {
                state.enemies[1].name = state.enemies[1].name == "Vault Sentinel" ? "Ancient Sentinel of the Inner Temple Vault" : "Vault Sentinel";
                hud.Render(state);
            }
        }
    }
}
