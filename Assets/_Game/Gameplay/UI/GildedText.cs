using UnityEngine;
using UnityEngine.UI;

namespace Parallax.Gameplay.UI
{
    /// <summary>D-280: old gold on a uGUI letter, with no custom shader. It cuts the glyph's quad into vertical strips and horizontal bands
    /// and colours the corners: a three-stop metal gradient from top to bottom, plus a band of light across the word. Alpha comes
    /// from the Text's own colour. Apply it before Outline and Shadow, so they copy the shape and keep their colours.</summary>
    [RequireComponent(typeof(Text))]
    public sealed class GildedText : BaseMeshEffect
    {
        const int Columns = 6, Rows = 4;
        const float SheenBand = 0.09f;

        [SerializeField] Color top = new Color(1f, 0.95f, 0.78f, 1f);
        [SerializeField] Color middle = new Color(0.96f, 0.76f, 0.38f, 1f);
        [SerializeField] Color bottom = new Color(0.62f, 0.36f, 0.14f, 1f);
        [SerializeField] Color shineColor = new Color(1f, 1f, 0.94f, 1f);

        float sheen = -1f, wordWidth = 1f;

        /// <summary>Where the light is across the word, 0 to 1 (negative for none), and how wide the word is.</summary>
        public void SetSheen(float position, float width)
        {
            if (Mathf.Approximately(sheen, position)) return;
            sheen = position;
            wordWidth = Mathf.Max(1f, width);
            if (graphic != null) graphic.SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper mesh)
        {
            if (!IsActive() || mesh.currentVertCount == 0) return;
            var quad = new UIVertex[4];
            var quads = new System.Collections.Generic.List<UIVertex>();
            for (int v = 0; v + 3 < mesh.currentVertCount; v += 4)
            {
                for (int k = 0; k < 4; k++) { UIVertex vertex = default; mesh.PopulateUIVertex(ref vertex, v + k); quad[k] = vertex; }
                quads.AddRange(quad);
            }
            float low = float.MaxValue, high = float.MinValue;
            foreach (UIVertex vertex in quads) { low = Mathf.Min(low, vertex.position.y); high = Mathf.Max(high, vertex.position.y); }
            mesh.Clear();
            float offset = ((RectTransform)transform).anchoredPosition.x;
            for (int q = 0; q + 3 < quads.Count; q += 4)
            {
                UIVertex a = quads[q], b = quads[q + 1], c = quads[q + 2], d = quads[q + 3];    // top-left, top-right, bottom-right, bottom-left
                int start = mesh.currentVertCount;
                for (int row = 0; row <= Rows; row++)
                    for (int column = 0; column <= Columns; column++)
                    {
                        float u = (float)column / Columns, w = (float)row / Rows;
                        UIVertex vertex = a;
                        vertex.position = Vector3.Lerp(Vector3.Lerp(a.position, b.position, u), Vector3.Lerp(d.position, c.position, u), w);
                        vertex.uv0 = Vector4.Lerp(Vector4.Lerp(a.uv0, b.uv0, u), Vector4.Lerp(d.uv0, c.uv0, u), w);
                        float height = Mathf.InverseLerp(low, high, vertex.position.y);
                        Color metal = height > 0.5f ? Color.Lerp(middle, top, (height - 0.5f) * 2f) : Color.Lerp(bottom, middle, height * 2f);
                        if (sheen >= 0f)
                        {
                            float across = (vertex.position.x + offset + vertex.position.y * 0.35f) / wordWidth + 0.5f;
                            float shine = Mathf.Clamp01(1f - Mathf.Abs(across - sheen * 1.3f + 0.15f) / SheenBand);
                            metal = Color.Lerp(metal, shineColor, shine * shine);
                        }
                        metal.a = a.color.a / 255f;
                        vertex.color = metal;
                        mesh.AddVert(vertex);
                    }
                for (int row = 0; row < Rows; row++)
                    for (int column = 0; column < Columns; column++)
                    {
                        int i = start + row * (Columns + 1) + column;
                        mesh.AddTriangle(i, i + 1, i + Columns + 2);
                        mesh.AddTriangle(i + Columns + 2, i + Columns + 1, i);
                    }
            }
        }
    }
}
