using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.Unity._Game.Unity.Views
{
    public class PulseRingGraphic : MaskableGraphic
    {
        [SerializeField, Min(0.1f)] private float thickness = 2f;
        private const int m_Segments = 64;

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            var rect = rectTransform.rect;
            var outer = Mathf.Min(rect.width, rect.height) * 0.5f;
            var inner = Mathf.Max(0, outer - thickness);
            for (var index = 0; index <= m_Segments; index++)
            {
                var angle = index * Mathf.PI * 2f / m_Segments;
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                mesh.AddVert(rect.center + direction * outer, color, Vector2.zero);
                mesh.AddVert(rect.center + direction * inner, color, Vector2.zero);
                if (index == 0)
                    continue;
                var vertex = index * 2;
                mesh.AddTriangle(vertex - 2, vertex, vertex - 1);
                mesh.AddTriangle(vertex, vertex + 1, vertex - 1);
            }
        }
    }
}
