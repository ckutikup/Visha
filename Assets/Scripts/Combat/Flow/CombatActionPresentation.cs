using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Visha.UI;

namespace Visha.Combat.Flow
{
    public sealed class CombatActionPresentation : MonoBehaviour
    {
        [SerializeField] private CharacterIdleMotion player;
        [SerializeField] private CharacterIdleMotion enemy;

        private readonly List<GameObject> effects = new List<GameObject>();
        private Material impactMaterial;

        public IEnumerator PlayerAction(BattleAbility ability, Action impact)
        {
            if (player == null || enemy == null) { impact(); yield break; }
            if (ability == BattleAbility.VenomStrike)
            {
                yield return Move(player, new Vector3(-.12f, 0, 0), .09f);
                float distance = Mathf.Max(0, enemy.HomePosition.x - player.HomePosition.x - 1.35f);
                yield return Move(player, new Vector3(distance, 0, 0), .18f);
            }
            else if (ability == BattleAbility.SerpentsBite)
            {
                yield return Move(player, new Vector3(-.18f, 0, 0), .1f);
                float distance = Mathf.Max(0, enemy.HomePosition.x - player.HomePosition.x - 1.35f);
                yield return Move(player, new Vector3(distance, 0, 0), .22f);
            }
            else
            {
                ShowAfterimage();
                yield return Move(player, new Vector3(-.35f, 0, 0), .18f);
            }
            impact();
            yield return new WaitForSeconds(.08f);
            yield return Move(player, Vector3.zero, .2f);
        }

        public IEnumerator EnemyAction(bool attacks, Action impact, bool heavy = false, string moveName = "Strike")
        {
            if (enemy == null || player == null) { impact(); yield break; }
            if (attacks)
            {
                enemy.ActionTilt = heavy ? -9f : moveName == "Shield Bash" ? 6f : -4f;
                yield return Move(enemy, new Vector3(heavy ? .3f : .18f, 0, 0), heavy ? .28f : .12f);
                float distance = Mathf.Max(0, enemy.HomePosition.x - player.HomePosition.x - 1.35f);
                yield return Move(enemy, new Vector3(-distance, 0, 0), .22f);
            }
            else
            {
                enemy.ActionTilt = -5f;
                yield return Move(enemy, new Vector3(.12f, -.04f, 0), .18f);
            }
            if (attacks)
            {
                enemy.ActionTilt = heavy ? 8f : 4f;
                ShowImpact(moveName, heavy);
            }
            impact();
            yield return new WaitForSeconds(attacks ? (heavy ? .24f : .18f) : .08f);
            yield return Move(enemy, Vector3.zero, .24f);
            if (enemy != null) enemy.ActionTilt = 0;
        }

        private void ShowImpact(string moveName, bool heavy)
        {
            if (player == null) return;
            Vector3 center = player.transform.position + new Vector3(.25f, 1.4f, 0);
            bool bash = moveName == "Shield Bash";
            var points = new Vector3[bash ? 25 : 9];
            for (int i = 0; i < points.Length; i++)
            {
                float t = (float)i / (points.Length - 1);
                if (bash)
                {
                    float angle = t * Mathf.PI * 2;
                    points[i] = new Vector3(Mathf.Cos(angle) * .48f, Mathf.Sin(angle) * .48f, 0);
                }
                else if (heavy)
                    points[i] = new Vector3(.3f * Mathf.Sin(t * Mathf.PI), .8f - t * 1.5f, 0);
                else
                    points[i] = new Vector3(-.5f + t, -.4f + t * .9f + Mathf.Sin(t * Mathf.PI) * .15f, 0);
            }
            var go = new GameObject(bash ? "BashImpact" : heavy ? "HeavyImpact" : "StrikeImpact", typeof(LineRenderer));
            go.transform.position = center;
            var line = go.GetComponent<LineRenderer>();
            if (impactMaterial == null)
            {
                impactMaterial = new Material(player.GetComponent<SpriteRenderer>().sharedMaterial);
                impactMaterial.mainTexture = Texture2D.whiteTexture;
            }
            line.sharedMaterial = impactMaterial;
            line.useWorldSpace = false;
            line.positionCount = points.Length;
            line.SetPositions(points);
            line.startWidth = heavy ? .15f : .08f;
            line.endWidth = bash ? .08f : .015f;
            line.numCapVertices = 2;
            line.sortingOrder = 12;
            effects.Add(go);
            StartCoroutine(FadeImpact(go, line, heavy ? .38f : .28f));
        }

        private IEnumerator FadeImpact(GameObject effect, LineRenderer line, float duration)
        {
            float elapsed = 0;
            while (elapsed < duration && effect != null)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                var color = new Color(1f, .78f, .46f, 1f - progress);
                line.startColor = line.endColor = color;
                effect.transform.localScale = Vector3.one * (1f + progress * .35f);
                yield return null;
            }
            effects.Remove(effect);
            if (effect != null) Destroy(effect);
        }

        private void ShowAfterimage()
        {
            var original = player.GetComponent<SpriteRenderer>();
            var go = new GameObject("FadeAfterimage", typeof(SpriteRenderer));
            go.transform.SetPositionAndRotation(player.transform.position, player.transform.rotation);
            go.transform.localScale = player.transform.lossyScale;
            var ghost = go.GetComponent<SpriteRenderer>();
            ghost.sprite = original.sprite;
            ghost.sharedMaterial = original.sharedMaterial;
            ghost.flipX = original.flipX;
            ghost.sortingLayerID = original.sortingLayerID;
            ghost.sortingOrder = original.sortingOrder - 1;
            effects.Add(go);
            StartCoroutine(FadeAfterimage(go, ghost));
        }

        private IEnumerator FadeAfterimage(GameObject effect, SpriteRenderer ghost)
        {
            float elapsed = 0;
            while (elapsed < .45f && effect != null)
            {
                elapsed += Time.deltaTime;
                ghost.color = new Color(.4f, .85f, .7f, .3f * (1f - elapsed / .45f));
                yield return null;
            }
            effects.Remove(effect);
            if (effect != null) Destroy(effect);
        }

        public void ShowCurseFragment()
        {
            if (player == null || enemy == null) return;
            StartCoroutine(CurseFragment());
        }

        private IEnumerator CurseFragment()
        {
            var go = new GameObject("CurseFragment", typeof(LineRenderer));
            effects.Add(go);
            var line = go.GetComponent<LineRenderer>();
            if (impactMaterial == null)
            {
                impactMaterial = new Material(player.GetComponent<SpriteRenderer>().sharedMaterial);
                impactMaterial.mainTexture = Texture2D.whiteTexture;
            }
            line.sharedMaterial = impactMaterial;
            line.useWorldSpace = false;
            line.positionCount = 5;
            line.SetPositions(new[] { Vector3.up * .12f, Vector3.right * .08f, Vector3.down * .12f,
                Vector3.left * .08f, Vector3.up * .12f });
            line.startWidth = line.endWidth = .06f;
            line.startColor = line.endColor = new Color(.5f, 1f, .72f);
            line.sortingOrder = 15;
            Vector3 from = enemy.transform.position + Vector3.up * 1.5f;
            float elapsed = 0;
            while (elapsed < .7f && player != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / .7f);
                go.transform.position = Vector3.Lerp(from, player.transform.position + Vector3.up * 1.5f, t)
                    + Vector3.up * Mathf.Sin(t * Mathf.PI) * .6f;
                yield return null;
            }
            effects.Remove(go);
            Destroy(go);
        }

        public void ShowTwinFang()
        {
            if (enemy != null) StartCoroutine(TwinFangFlash());
        }
        private IEnumerator TwinFangFlash()
        {
            for (int side=-1;side<=1;side+=2)
            {
                var go=new GameObject("TwinFangSlash",typeof(LineRenderer));
                effects.Add(go);
                var line=go.GetComponent<LineRenderer>();
                if (impactMaterial == null)
                {
                    impactMaterial=new Material(player.GetComponent<SpriteRenderer>().sharedMaterial);
                    impactMaterial.mainTexture=Texture2D.whiteTexture;
                }
                line.sharedMaterial=impactMaterial; line.useWorldSpace=false;
                line.positionCount=3; line.SetPositions(new[] {new Vector3(side*.45f,.65f,0),new Vector3(side*.15f,0,0),new Vector3(side*.25f,-.5f,0)});
                line.startWidth=.10f; line.endWidth=.025f;
                line.startColor=line.endColor=new Color(.48f,1f,.72f);
                line.sortingOrder=20; go.transform.position=enemy.transform.position+Vector3.up*1.5f;
            }
            yield return new WaitForSeconds(.32f);
            for (int i=effects.Count-1;i>=0;i--)
                if (effects[i] != null && effects[i].name=="TwinFangSlash") { Destroy(effects[i]); effects.RemoveAt(i); }
        }

        private IEnumerator Move(CharacterIdleMotion actor, Vector3 target, float duration)
        {
            if (actor == null) yield break;
            Vector3 from = actor.ActionOffset;
            float elapsed = 0;
            while (elapsed < duration && actor != null && isActiveAndEnabled)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(elapsed / duration));
                actor.ActionOffset = Vector3.Lerp(from, target, t);
                yield return null;
            }
            if (actor != null) actor.ActionOffset = target;
        }
        public void ResetPose()
        {
            StopAllCoroutines();
            foreach (var effect in effects) if (effect != null) Destroy(effect);
            effects.Clear();
            if (player != null) { player.ActionOffset = Vector3.zero; player.ActionTilt = 0; }
            if (enemy != null) { enemy.ActionOffset = Vector3.zero; enemy.ActionTilt = 0; }
        }
        private void OnDisable() => ResetPose();
        private void OnDestroy() { if (impactMaterial != null) Destroy(impactMaterial); }
    }
}
