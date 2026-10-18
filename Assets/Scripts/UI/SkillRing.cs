using UnityEngine;
using UnityEngine.UI;

namespace Visha.UI
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SkillRing : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = GetPixelAdjustedRect();
            float radius = Mathf.Min(rect.width, rect.height) * .5f;
            const int segments = 80;
            for (int i = 0; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2 / segments;
                Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                mesh.AddVert(rect.center + direction * radius, color, Vector2.zero);
                mesh.AddVert(rect.center + direction * (radius - 2), color, Vector2.zero);
                if (i == 0) continue;
                int v = i * 2;
                mesh.AddTriangle(v - 2, v, v - 1); mesh.AddTriangle(v, v + 1, v - 1);
            }
        }
    }
}
