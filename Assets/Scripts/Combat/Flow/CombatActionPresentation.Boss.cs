using System;
using System.Collections;
using UnityEngine;

namespace Visha.Combat.Flow
{
    public sealed partial class CombatActionPresentation
    {
        private IEnumerator BossSlam(string move, Action impact)
        {
            bool final = move == "Cataclysm";
            var ring = new GameObject("CurseWarning", typeof(LineRenderer));
            effects.Add(ring);
            var line = ring.GetComponent<LineRenderer>();
            if (impactMaterial == null)
            {
                impactMaterial = new Material(player.GetComponent<SpriteRenderer>().sharedMaterial);
                impactMaterial.mainTexture = Texture2D.whiteTexture;
            }
            line.sharedMaterial = impactMaterial;
            line.useWorldSpace = false;
            line.loop = true;
            line.positionCount = 48;
            line.sortingOrder = 12;
            line.startWidth = line.endWidth = final ? .085f : .055f;
            var color = final ? new Color(.82f, .4f, 1f) : new Color(1f, .65f, .3f);
            line.startColor = line.endColor = color;
            ring.transform.position = player.HomePosition + Vector3.up * .06f;
            for (int i = 0; i < 48; i++)
            {
                float angle = i * Mathf.PI * 2 / 48;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * (final ? 1.5f : 1f), Mathf.Sin(angle) * .25f, 0));
            }
            enemy.ActionTilt = -12f;
            yield return Move(enemy, new Vector3(.25f, .18f, 0), final ? .55f : .4f);
            float distance = Mathf.Max(0, enemy.HomePosition.x - player.HomePosition.x - 2f);
            yield return Move(enemy, new Vector3(-distance, .1f, 0), .22f);
            enemy.ActionTilt = 10f;
            ShowImpact(move, true);
            impact();
            // A lethal impact can reset the presentation and remove the effect immediately.
            float elapsed = 0;
            while (elapsed < .3f && ring != null)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / .3f);
                ring.transform.localScale = Vector3.one * (1f + t * 1.6f);
                color.a = 1f - t;
                line.startColor = line.endColor = color;
                yield return null;
            }
            if (ring != null) { effects.Remove(ring); Destroy(ring); }
            yield return Move(enemy, Vector3.zero, .3f);
            if (enemy != null) enemy.ActionTilt = 0;
        }
    }
}
