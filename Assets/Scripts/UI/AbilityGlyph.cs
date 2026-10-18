using UnityEngine;
using UnityEngine.UI;

namespace Visha.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class AbilityGlyph : MaskableGraphic
    {
        [SerializeField] private BattleAbility ability;
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (ability == BattleAbility.VenomStrike)
            {
                Polygon(mesh, new[] { new Vector2(.34f,.5f), new Vector2(.19f,-.08f), new Vector2(-.12f,-.29f), new Vector2(-.02f,.04f) });
                Polygon(mesh, new[] { new Vector2(-.25f,-.12f), new Vector2(.15f,-.36f), new Vector2(.19f,-.29f), new Vector2(-.21f,-.05f) });
                Polygon(mesh, new[] { new Vector2(-.12f,-.23f), new Vector2(-.27f,-.48f), new Vector2(-.36f,-.43f), new Vector2(-.21f,-.18f) });
                Polygon(mesh, new[] { new Vector2(-.32f,.35f), new Vector2(-.45f,.1f), new Vector2(-.42f,-.01f), new Vector2(-.3f,-.04f), new Vector2(-.2f,.04f) });
            }
            else if (ability == BattleAbility.SerpentsBite)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    Polygon(mesh, new[] { new Vector2(side*.45f,.35f), new Vector2(side*.16f,.22f), new Vector2(side*.12f,-.12f), new Vector2(side*.23f,-.46f), new Vector2(side*.37f,-.07f) });
                    Polygon(mesh, new[] { new Vector2(side*.44f,.43f), new Vector2(side*.13f,.34f), new Vector2(side*.07f,.45f) });
                }
            }
            else
            {
                // A crescent trail keeps the evasive skill distinct from a shield.
                for (int i = 0; i < 20; i++)
                {
                    float a = (-110 + i * 11) * Mathf.Deg2Rad;
                    float b = (-110 + (i + 1) * 11) * Mathf.Deg2Rad;
                    Vector2 outerA = new Vector2(-Mathf.Cos(a)*.46f, Mathf.Sin(a)*.48f);
                    Vector2 outerB = new Vector2(-Mathf.Cos(b)*.46f, Mathf.Sin(b)*.48f);
                    Vector2 innerA = new Vector2(.12f-Mathf.Cos(a)*.35f, Mathf.Sin(a)*.48f);
                    Vector2 innerB = new Vector2(.12f-Mathf.Cos(b)*.35f, Mathf.Sin(b)*.48f);
                    Polygon(mesh, new[] { outerA, outerB, innerB, innerA });
                }
                Polygon(mesh, new[] {new Vector2(.27f,.24f),new Vector2(.33f,.06f),new Vector2(.47f,0),new Vector2(.33f,-.06f),new Vector2(.27f,-.24f),new Vector2(.21f,-.06f),new Vector2(.07f,0),new Vector2(.21f,.06f)});
            }
        }
        private void Polygon(VertexHelper mesh, Vector2[] points)
        {
            Rect rect = GetPixelAdjustedRect();
            int start = mesh.currentVertCount;
            foreach (Vector2 point in points)
                mesh.AddVert(new Vector3(rect.center.x + point.x * rect.width, rect.center.y + point.y * rect.height), color, Vector2.zero);
            for (int i = 1; i < points.Length - 1; i++) mesh.AddTriangle(start, start + i, start + i + 1);
        }
    }
}
