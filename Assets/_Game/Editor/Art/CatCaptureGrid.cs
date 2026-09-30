using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Parallax.Editor.Art
{
    /// <summary>PAX-V07 gauntlet item 0 (round 2): capture-only overlays drawn onto each rendered frame, never into the scene.
    /// A world reference so motion and foot slide read against the world: a faint 1 u grid fixed in world space, and tick
    /// marks every 0.25 u (longer every 1 u) drawn into every solid's top and bottom faces from the current collider poses
    /// (a moving floor carries its ticks), starting just inside the face so a paw on the surface is never covered. Nothing
    /// is drawn over the cat's own pixels (its body sprite's drawn pixels, grown to cover its outline). On death-hold frames,
    /// the cat's silhouette edge in cyan, so its pose reads where the game draws room sprites over it.</summary>
    sealed class CatCaptureGrid
    {
        public const float GridStep = 1f, TickStep = 0.25f;
        const float TickShort = 0.09f, TickLong = 0.2f, TickInset = 0.02f;
        const int CatMaskGrowPx = 3;                 // covers the 1.5-texel outline renderer at 160 px/u
        const float MaxSolidTiltDegrees = 1f;        // a tilted solid (a falling shard) gets no ticks: its box isn't its face
        static readonly Color GridColor = new(0f, 0f, 0f, 0.16f), TickColor = new(0.1f, 0.05f, 0f, 0.6f);
        static readonly Color32 HoldOverlay = new(0, 230, 255, 255);

        readonly List<Collider2D> solids;
        readonly float ppu;

        public CatCaptureGrid(CatCaptureRig rig, float pixelsPerUnit)
        {
            ppu = pixelsPerUnit;
            solids = Object.FindObjectsByType<Collider2D>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(c => !c.isTrigger && c != rig.CatCollider && c.attachedRigidbody != rig.Body
                            && (rig.Root.PhysicsMask & (1 << c.gameObject.layer)) != 0)
                .ToList();
        }

        /// <summary>Which image pixels the cat's body sprite covers (grown by CatMaskGrowPx).</summary>
        public bool[] CatMask(Texture2D tex, SpriteAlphaCache alpha, SpriteRenderer body, Vector2 cam, int grow)
        {
            int w = tex.width, h = tex.height;
            var mask = new bool[w * h];
            if (body.sprite == null) return mask;
            SpriteAlphaCache.Entry e = alpha.Get(body.sprite);
            Matrix4x4 m = body.transform.localToWorldMatrix;
            for (int c = 0; c < e.Columns.Length; c++)
                foreach (int y in e.Columns[c])
                {
                    Vector2 p = CatCaptureMath.PixelToWorld(e.ColumnX[c], y, e.PivotPx, e.Ppu, m);
                    int ix = Mathf.FloorToInt((p.x - cam.x) * ppu + w * 0.5f), iy = Mathf.FloorToInt((p.y - cam.y) * ppu + h * 0.5f);
                    for (int dx = -grow; dx <= grow + 1; dx++)
                        for (int dy = -grow; dy <= grow + 1; dy++)
                        {
                            int x = ix + dx, yy = iy + dy;
                            if (x >= 0 && x < w && yy >= 0 && yy < h) mask[yy * w + x] = true;
                        }
                }
            return mask;
        }

        /// <summary>The world grid and the surface ticks, onto `tex` (rendered with its centre at `cam`), outside the cat.</summary>
        public void DrawWorldReference(Texture2D tex, SpriteAlphaCache alpha, SpriteRenderer body, Vector2 cam)
        {
            int w = tex.width, h = tex.height;
            bool[] cat = CatMask(tex, alpha, body, cam, CatMaskGrowPx);
            Color32[] px = tex.GetPixels32();
            float x0 = cam.x - w * 0.5f / ppu, y0 = cam.y - h * 0.5f / ppu;
            int Px(float x) => Mathf.FloorToInt((x - x0) * ppu);
            int Py(float y) => Mathf.FloorToInt((y - y0) * ppu);
            void Blend(int x, int y, Color c)
            {
                if (x < 0 || x >= w || y < 0 || y >= h || cat[y * w + x]) return;
                int i = y * w + x;
                Color o = px[i];
                px[i] = Color.Lerp(o, new Color(c.r, c.g, c.b, 1f), c.a);
            }

            for (float gx = Mathf.Ceil(x0 / GridStep) * GridStep; gx <= x0 + w / ppu; gx += GridStep)
                for (int y = 0; y < h; y++) Blend(Px(gx), y, GridColor);
            for (float gy = Mathf.Ceil(y0 / GridStep) * GridStep; gy <= y0 + h / ppu; gy += GridStep)
                for (int x = 0; x < w; x++) Blend(x, Py(gy), GridColor);

            foreach (Collider2D c in solids)
            {
                if (c == null || !c.enabled || !c.gameObject.activeInHierarchy) continue;
                if (Mathf.Abs(Mathf.DeltaAngle(c.transform.eulerAngles.z, 0f)) > MaxSolidTiltDegrees
                    && Mathf.Abs(Mathf.DeltaAngle(c.transform.eulerAngles.z, 180f)) > MaxSolidTiltDegrees) continue;
                Bounds b = c.bounds;
                if (b.max.x < x0 || b.min.x > x0 + w / ppu || b.max.y < y0 || b.min.y > y0 + h / ppu) continue;
                for (float tx = Mathf.Ceil(b.min.x / TickStep) * TickStep; tx <= b.max.x + 1e-4f; tx += TickStep)
                {
                    bool whole = Mathf.Abs(tx - Mathf.Round(tx)) < 1e-3f;
                    float len = Mathf.Min(whole ? TickLong : TickShort, b.size.y * 0.5f);
                    int x = Px(tx);
                    for (int y = Py(b.max.y - TickInset - len); y <= Py(b.max.y - TickInset); y++) { Blend(x, y, TickColor); Blend(x + 1, y, TickColor); }
                    for (int y = Py(b.min.y + TickInset); y <= Py(b.min.y + TickInset + len); y++) { Blend(x, y, TickColor); Blend(x + 1, y, TickColor); }
                }
            }
            tex.SetPixels32(px);
            tex.Apply();
        }

        /// <summary>Death-hold frames: the edge of the cat's body sprite's drawn pixels, in cyan, over everything.</summary>
        public void DrawSilhouette(Texture2D tex, SpriteAlphaCache alpha, SpriteRenderer body, Vector2 cam)
        {
            int w = tex.width, h = tex.height;
            bool[] mask = CatMask(tex, alpha, body, cam, 0);
            Color32[] px = tex.GetPixels32();
            for (int y = 1; y < h - 1; y++)
                for (int x = 1; x < w - 1; x++)
                {
                    int i = y * w + x;
                    if (mask[i] && (!mask[i - 1] || !mask[i + 1] || !mask[i - w] || !mask[i + w])) px[i] = HoldOverlay;
                }
            tex.SetPixels32(px);
            tex.Apply();
        }
    }
}
