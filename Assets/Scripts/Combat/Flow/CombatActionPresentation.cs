using System;
using System.Collections;
using UnityEngine;
using Visha.UI;

namespace Visha.Combat.Flow
{
    public sealed class CombatActionPresentation : MonoBehaviour
    {
        [SerializeField] private CharacterIdleMotion player;
        [SerializeField] private CharacterIdleMotion enemy;

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
                yield return Move(player, new Vector3(-.35f, 0, 0), .18f);
            }
            impact();
            yield return new WaitForSeconds(.08f);
            yield return Move(player, Vector3.zero, .2f);
        }

        public IEnumerator EnemyAction(bool attacks, Action impact, bool heavy = false)
        {
            if (enemy == null || player == null) { impact(); yield break; }
            if (attacks)
            {
                yield return Move(enemy, new Vector3(heavy ? .3f : .18f, 0, 0), heavy ? .24f : .12f);
                float distance = Mathf.Max(0, enemy.HomePosition.x - player.HomePosition.x - 1.35f);
                yield return Move(enemy, new Vector3(-distance, 0, 0), .22f);
            }
            else
                yield return new WaitForSeconds(.2f);
            impact();
            yield return new WaitForSeconds(attacks ? (heavy ? .24f : .18f) : .08f);
            yield return Move(enemy, Vector3.zero, .24f);
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
            if (player != null) player.ActionOffset = Vector3.zero;
            if (enemy != null) enemy.ActionOffset = Vector3.zero;
        }
        private void OnDisable() => ResetPose();
    }
}
