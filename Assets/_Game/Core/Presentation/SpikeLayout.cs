using System.Collections.Generic;
using UnityEngine;

namespace Parallax.Core.Presentation
{
    /// <summary>PAX-A18 W2 (D-151): a spike strip laid out blade by blade. Every blade draws at one fixed size; the strip's
    /// width is filled by a seeded run of the painted blade variants with irregular gaps, never an even picket, and the
    /// leftover is shared out among the gaps, so the row spans the strip end to end without stretching or cutting a blade.
    /// Pure: the same strip (length, seed) always gets the same blades.</summary>
    public static class SpikeLayout
    {
        /// <summary>One blade: which variant, and where its centre sits along the strip (0 = the strip's centre).</summary>
        public readonly struct Blade
        {
            public readonly int Variant; public readonly float Along, Width;
            public Blade(int variant, float along, float width) { Variant = variant; Along = along; Width = width; }
        }

        /// <summary>The smallest and largest gap between neighbouring blades, before the leftover is shared out (world units).</summary>
        public const float MinGap = 0.04f, MaxGap = 0.16f;

        /// <summary>Blades along a strip `length` wide. `widths` are the variants' drawn widths (world units); `seed` makes
        /// each strip's run its own. Blades never overlap, stay inside the strip, and no variant follows itself.</summary>
        public static List<Blade> Lay(float length, IReadOnlyList<float> widths, uint seed)
        {
            var blades = new List<Blade>();
            if (widths == null || widths.Count == 0 || length <= 0f) return blades;
            var picks = new List<int>();
            var gaps = new List<float>();
            float used = 0f;
            int last = -1;
            for (int i = 0; i < 4096; i++)
            {
                int v = (int)(TrapArtMath.Hash01(seed, 900 + i * 2) * widths.Count) % widths.Count;
                if (v == last && widths.Count > 1) v = (v + 1) % widths.Count;
                float gap = picks.Count == 0 ? 0f : Mathf.Lerp(MinGap, MaxGap, TrapArtMath.Hash01(seed, 901 + i * 2));
                if (used + gap + widths[v] > length && picks.Count > 0) break;
                if (picks.Count == 0 && widths[v] > length) { v = Narrowest(widths); }
                picks.Add(v); gaps.Add(gap);
                used += gap + widths[v];
                last = v;
            }
            // The leftover goes evenly into the gaps and both ends (so the end blades sit near the strip's ends).
            float leftover = Mathf.Max(0f, length - used), share = leftover / (picks.Count + 1);
            float x = -length * 0.5f + share;
            for (int i = 0; i < picks.Count; i++)
            {
                if (i > 0) x += gaps[i] + share;
                float w = widths[picks[i]];
                blades.Add(new Blade(picks[i], x + w * 0.5f, w));
                x += w;
            }
            return blades;
        }

        static int Narrowest(IReadOnlyList<float> widths)
        {
            int best = 0;
            for (int i = 1; i < widths.Count; i++) if (widths[i] < widths[best]) best = i;
            return best;
        }
    }
}
