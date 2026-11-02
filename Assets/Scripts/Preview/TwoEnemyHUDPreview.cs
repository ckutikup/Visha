using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Visha.Combat.Flow;
using Visha.UI;

namespace Visha.Preview
{
    public sealed class TwoEnemyHUDPreview : MonoBehaviour
    {
        private BattleHUD hud;
        private BattleViewState state;
        [SerializeField] private Transform[] actors;
        private Vector3[] homes;
        private SpriteRenderer[] sprites;
        [SerializeField] private BattleFeedback[] enemyHitFeedback;
        [SerializeField] private BattleFeedback playerHitFeedback;
        private Coroutine sequence;

        public void Configure(Transform[] enemyActors, BattleFeedback[] enemyFeedback, BattleFeedback targetFeedback)
        {
            actors = enemyActors;
            enemyHitFeedback = enemyFeedback;
            playerHitFeedback = targetFeedback;
        }

        private void Start()
        {
            hud = GetComponent<BattleHUD>(); hud.AbilityRequested += RecordRequest;
            var screens = Object.FindAnyObjectByType<BattleScreenView>();
            if (screens) { screens.SetHUD(hud); screens.ShowBattle(); }
            if (actors == null || actors.Length != 2 || enemyHitFeedback == null || enemyHitFeedback.Length != 2 || playerHitFeedback == null)
                throw new System.InvalidOperationException("Two-enemy preview actors and hit feedback must be configured by TwoEnemyPreviewSetup.");
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
            if (target >= 0)
            {
                int index = System.Array.FindIndex(state.enemies, enemy => enemy.targetId == target);
                if (index >= 0)
                {
                    enemyHitFeedback[index].PlayHitFeedback();
                    hud.SetFeedback($"Preview: {ability} targets {state.enemies[index].name} · visual hit only");
                }
                else hud.SetFeedback($"Preview: {ability} · no damage applied");
            }
            else hud.SetFeedback("Preview: Fade activates · no damage applied");
            sequence = StartCoroutine(ShowEnemyPhase());
        }
        private IEnumerator ShowEnemyPhase()
        {
            yield return new WaitForSeconds(.6f);
            for (int i=0;i<actors.Length;i++)
            {
                if (state.enemies[i].health <= 0) continue;
                state.phase = BattlePhase.EnemyTurn; state.actingEnemyId = state.enemies[i].targetId; hud.Render(state);
                int other = 1-i;
                sprites[i].sortingOrder = 12; sprites[other].sortingOrder = 10;
                sprites[i].color = Color.white;
                sprites[other].color = new Color(.58f,.65f,.68f,1f);
                bool attacks = state.enemies[i].incomingDamage > 0;
                Vector3 actionPose = attacks ? homes[i] + Vector3.left * .42f : homes[i] + new Vector3(.12f,-.04f,0);
                yield return MoveActor(i, actionPose, attacks ? .22f : .18f);
                if (attacks)
                {
                    hud.SetFeedback($"Preview: {state.enemies[i].name} · {state.enemies[i].intention} hits Visha for {state.enemies[i].incomingDamage}");
                    if (playerHitFeedback != null) playerHitFeedback.PlayHitFeedback();
                    yield return new WaitForSeconds(.38f);
                }
                else
                {
                    hud.SetFeedback($"Preview: {state.enemies[i].name} holds position · no attack");
                    yield return new WaitForSeconds(.24f);
                }
                yield return MoveActor(i, homes[i], .28f);
                RestoreFormation();
            }
            state.phase = BattlePhase.PlayerTurn; state.actingEnemyId = -1; hud.Render(state);
            hud.SetFeedback("Your target is preserved · choose an ability"); sequence = null;
        }
        private IEnumerator MoveActor(int index, Vector3 destination, float duration)
        {
            var from = actors[index].position;
            float elapsed = 0;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime; float t = Mathf.SmoothStep(0,1,elapsed/duration);
                actors[index].position = Vector3.Lerp(from,destination,t);
                yield return null;
            }
            actors[index].position = destination;
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
