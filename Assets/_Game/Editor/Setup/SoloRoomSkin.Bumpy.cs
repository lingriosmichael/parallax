using System.Collections.Generic;
using System.Linq;
using Parallax.Editor.Art;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Editor.Setup
{
    /// <summary>PAX-A18 W3 (amends D-114): bumpy stone, drawn only; colliders untouched. Which stretch of a side face the cat
    /// can be next to (its walk band: the top 0.6 u of a face, and wherever a cat standing, jumping, riding or climbing
    /// nearby can touch it) gets the band strip, an outline within BandReach of the face; every other stretch (a pit's wall,
    /// a face over air nothing reaches) gets the deep strip, within DeepReach. The outlines themselves are cut into the
    /// strips' alpha by Tools/Art/env_bumpy.py; the reaches here must equal its BAND_REACH and DEEP_REACH.</summary>
    public static partial class SoloRoomSkin
    {
        /// <summary>The walk band's outline reach (env_bumpy.BAND_REACH), and the deep faces' (DEEP_REACH).</summary>
        // 2026-10-17 (the developer: "WAAAAAY too obvious"): 0.12 and 0.30 before; band strips cut inward only (env_bumpy).
        public const float BandReach = 0.08f, DeepReach = 0.18f, HazardReach = 0.06f, TrapProud = 0.03f;
        /// <summary>2026-10-16: an upright narrower than this takes band strips only.</summary>
        public const float ThinWidth = 0.8f;
        /// <summary>The top of every exposed face is walk band this far down (the cat stands at the corner).</summary>
        public const float WalkBand = 0.6f;
        /// <summary>A cat standing on a top reaches this high above it (a jump's apex 1.6, the cat's 0.56, a margin), and
        /// this far past its ends.</summary>
        public const float ReachUp = 2.4f, ReachAcross = 0.6f;
        /// <summary>A free stretch shorter than this stays band (no flicker of strips).</summary>
        const float MinFreeRun = 0.3f, SpanStep = 0.05f;
        /// <summary>How far around a hazard only band strips stand (its drawn spikes and the air behind them).</summary>
        const float HazardClear = 0.6f;

        /// <summary>A stretch of a side edge, and whether nothing can reach it (deep strip) or something can (band strip).</summary>
        public readonly struct FaceSpan
        {
            public readonly float From, To; public readonly bool Free, Inward;
            public FaceSpan(float from, float to, bool free, bool inward = false) { From = from; To = to; Free = free; Inward = inward; }
        }

        /// <summary>Where a cat can be: above every walkable top (and, in a room with a gravity flip, below every exposed
        /// underside), along a mover's travel, and everything VisualKeepOut keeps clear (hazards, lanes, vines, drops,
        /// sweeps, the door, checkpoints).</summary>
        public static List<Rect> ReachZones(SoloRoomDefinition room, List<Solid> solids)
        {
            bool flips = room.Elements.Any(x => x.Kind == SoloRoomElementKind.GravityFlip);
            var zones = new List<Rect>(VisualKeepOut(room));
            foreach (Edge e in ExposedEdges(solids))
            {
                if (e.Side == Side.Top && e.Owner.Shape != Shape.PitBottom)
                    zones.Add(Rect.MinMaxRect(e.From - ReachAcross, e.Line, e.To + ReachAcross, e.Line + ReachUp));
                else if (flips && e.Side == Side.Bottom)
                    zones.Add(Rect.MinMaxRect(e.From - ReachAcross, e.Line - ReachUp, e.To + ReachAcross, e.Line));
            }
            foreach (SoloRoomElement x in room.Elements.Where(x => x.Kind == SoloRoomElementKind.MovingTrap && x.Settings.Offset != Vector2.zero))
            {
                var r = new Rect(room.Origin + x.Position - x.Size * 0.5f + x.Settings.Offset, x.Size);
                zones.Add(Rect.MinMaxRect(r.xMin - ReachAcross, r.yMin - (flips ? ReachUp : 0f), r.xMax + ReachAcross, r.yMax + ReachUp));
            }
            return zones;
        }

        /// <summary>Around every hazard: spikes draw up to 1.8x their box (D-105) and must keep their contrast (D-108), so a face
        /// here keeps D-114's outline (HazardReach, ENV_SideIn_*).</summary>
        public static List<Rect> HazardZones(SoloRoomDefinition room) =>
            room.Elements.Where(x => x.Kind is SoloRoomElementKind.Hazard or SoloRoomElementKind.HiddenSpikes)
                .Select(x => Grow(new Rect(room.Origin + x.Position - x.Size * 0.5f, x.Size), HazardClear)).ToList();

        /// <summary>A side edge cut into band and free stretches. A trap's faces are always band (they move next to the cat);
        /// so is the top WalkBand of a face whose solid's top is exposed (and its bottom WalkBand in a flip room).</summary>
        public static List<FaceSpan> FaceSpans(Edge e, List<Rect> zones, List<Edge> allEdges, bool flips, List<Rect> hazards = null)
        {
            var spans = new List<FaceSpan>();
            if (e.Side is not (Side.Left or Side.Right)) return spans;
            float nx0 = e.Side == Side.Left ? e.Line - BandReach : e.Line, nx1 = e.Side == Side.Left ? e.Line : e.Line + BandReach;
            // The developer (2026-10-16): "All edges need to be a bit bumpy". A face beside a hazard takes the same band or deep
            // strips as any other (it kept D-114's near-straight ENV_SideIn_* before); HazardContrastTests still holds D-108.
            bool Near(float y0, float y1) => false;
            // 2026-10-16: a thin upright (under ThinWidth) takes band strips only: deep ones would leave no stone between its faces.
            if (e.Owner.Trap || e.Owner.Grip || e.Owner.Shape != Shape.Block && e.Owner.Shape != Shape.PitBottom || e.Owner.Rect.width < ThinWidth)
            {
                spans.Add(new FaceSpan(e.From, e.To, false, Near(e.From, e.To)));
                return spans;
            }
            Rect owner = e.Owner.Rect;
            bool topOpen = allEdges.Any(t => t.Owner.Name == e.Owner.Name && t.Side == Side.Top);
            bool bottomOpen = flips && allEdges.Any(t => t.Owner.Name == e.Owner.Name && t.Side == Side.Bottom);
            float x0 = e.Side == Side.Left ? e.Line - DeepReach - 0.05f : e.Line, x1 = e.Side == Side.Left ? e.Line : e.Line + DeepReach + 0.05f;
            int n = Mathf.Max(1, Mathf.CeilToInt(e.Length / SpanStep));
            var free = new bool[n];
            for (int i = 0; i < n; i++)
            {
                float y0 = e.From + e.Length * i / n, y1 = e.From + e.Length * (i + 1) / n;
                bool band = topOpen && y1 > owner.yMax - WalkBand || bottomOpen && y0 < owner.yMin + WalkBand;
                var front = Rect.MinMaxRect(x0, y0, x1, y1);
                free[i] = !band && !zones.Any(z => z.Overlaps(front));
            }
            // Runs (deep, band, inward), with short deep runs folded into band.
            var kind = new int[n];
            for (int i = 0; i < n; i++)
            {
                float y0 = e.From + e.Length * i / n, y1 = e.From + e.Length * (i + 1) / n;
                kind[i] = Near(y0, y1) ? 2 : free[i] ? 1 : 0;
            }
            int start = 0;
            for (int i = 1; i <= n; i++)
            {
                if (i < n && kind[i] == kind[start]) continue;
                float a = e.From + e.Length * start / n, b = e.From + e.Length * i / n;
                int k = kind[start] == 1 && b - a < MinFreeRun ? 0 : kind[start];
                if (spans.Count > 0 && (spans[^1].Free ? 1 : spans[^1].Inward ? 2 : 0) == k) spans[^1] = new FaceSpan(spans[^1].From, b, k == 1, k == 2);
                else spans.Add(new FaceSpan(a, b, k == 1, k == 2));
                start = i;
            }
            return spans;
        }

        /// <summary>The strip slot for a stretch: one of four band (ENV_SideChip_*) or deep (ENV_SideDeep_*) strips cut from the
        /// room's fill, by hash; null when the kit lacks them.</summary>
        public static string SpanSlot(SoloRoomDefinition room, Edge e, FaceSpan span)
        {
            string chip = ChipSlot(room, e);
            if (chip == null) return null;
            if (span.Inward) { string inward = chip.Replace("ENV_SideChip_", "ENV_SideIn_"); return EnvironmentKit.Get(inward) != null && EnvironmentKit.TileMaterial("SideIn") != null ? inward : chip; }
            if (!span.Free) return chip;
            string deep = chip.Replace("ENV_SideChip_", "ENV_SideDeep_");
            return EnvironmentKit.Get(deep) != null && EnvironmentKit.TileMaterial(EdgeKindOf(room, "SideDeep")) != null ? deep : chip;
        }

        /// <summary>A fixed solid's fill pieces cut where its exposed sides change strips, each with the inset its side needs
        /// there (BandReach along band strips, DeepReach along deep ones, HazardReach beside a hazard, none where a neighbour
        /// covers the side), so every notch, and every broken corner end, shows the air.</summary>
        internal static List<(Rect piece, float left, float right)> InsetPieces(Solid s, List<Rect> pieces, List<Edge> sides, List<Rect> zones, List<Edge> allEdges, bool flips, List<Rect> hazards)
        {
            var result = new List<(Rect, float, float)>();
            List<FaceSpan> Spans(Side side) => sides.Where(e => e.Owner.Name == s.Name && e.Side == side).SelectMany(e => FaceSpans(e, zones, allEdges, flips, hazards)).ToList();
            List<FaceSpan> left = Spans(Side.Left), right = Spans(Side.Right);
            // 2026-10-16: one inset per side, its deepest strip's reach (every strip reaches DeepReach + overlap into the stone,
            // env_bumpy), so the face shadow can follow the fill without a lit sliver between band and deep stretches.
            // ... and never more than half the solid's width less a sliver, so a thin solid keeps stone between its faces.
            float Deepest(List<FaceSpan> spans) => Mathf.Min(spans.Any(p => p.Free) ? DeepReach : BandReach, s.Rect.width * 0.5f - 0.06f);
            float InsetAt(List<FaceSpan> spans, float y)
            {
                foreach (FaceSpan p in spans)
                    if (y > p.From && y < p.To) return Deepest(spans);
                return 0f;
            }
            var cuts = left.Concat(right).SelectMany(p => new[] { p.From, p.To }).Distinct().OrderBy(y => y).ToList();
            foreach (Rect piece in pieces)
            {
                var ys = new List<float> { piece.yMin };
                ys.AddRange(cuts.Where(y => y > piece.yMin + Eps && y < piece.yMax - Eps));
                ys.Add(piece.yMax);
                for (int i = 0; i + 1 < ys.Count; i++)
                {
                    var part = Rect.MinMaxRect(piece.xMin, ys[i], piece.xMax, ys[i + 1]);
                    float mid = (ys[i] + ys[i + 1]) * 0.5f;
                    float l = Mathf.Abs(piece.xMin - s.Rect.xMin) < Eps ? InsetAt(left, mid) : 0f;
                    float r = Mathf.Abs(piece.xMax - s.Rect.xMax) < Eps ? InsetAt(right, mid) : 0f;
                    result.Add((part, l, r));
                }
            }
            return result;
        }

        /// <summary>Where a side face's strips stop under a broken corner end (the end draws the top of that face itself), or
        /// the face's own top when its corner has none.</summary>
        public static float StripTop(Edge side, List<Edge> allEdges)
        {
            Rect r = side.Owner.Rect;
            if (side.To < r.yMax - Eps) return side.To;
            bool atLeft = side.Side == Side.Left;
            foreach (Edge top in allEdges.Where(t => t.Owner.Name == side.Owner.Name && t.Side == Side.Top))
            {
                if (!CornerExposed(top, atLeft, allEdges)) continue;
                string slot = CapEndSlot(top, atLeft);
                EnvironmentKit.Slot end = slot != null ? EnvironmentKit.Get(slot) : null;
                if (end == null || top.Length < 2f * (end.width - end.outside) + 0.1f) continue;
                return Mathf.Max(side.From, r.yMax - (end.height - end.above) + 0.04f);
            }
            return side.To;
        }

        /// <summary>A top from `from` to `to` cut into runs: flat where a hazard's zone stands on it, bumpy elsewhere.</summary>
        public static List<(float from, float to, bool flat)> CapRuns(float from, float to, float line, List<Rect> hazards)
        {
            var flatSpans = (hazards ?? new List<Rect>()).Where(z => z.yMin < line + 0.3f && z.yMax > line)
                .Select(z => (Mathf.Max(from, z.xMin), Mathf.Min(to, z.xMax))).Where(p => p.Item2 > p.Item1 + Eps).OrderBy(p => p.Item1).ToList();
            var runs = new List<(float, float, bool)>();
            float cursor = from;
            foreach ((float a, float b) in flatSpans)
            {
                if (b <= cursor) continue;
                if (a > cursor + Eps) runs.Add((cursor, a, false));
                runs.Add((Mathf.Max(a, cursor), b, true));
                cursor = b;
            }
            if (to > cursor + Eps) runs.Add((cursor, to, false));
            return runs;
        }

        /// <summary>A top's exposed corner at its left (or right) end: the solid's own corner, with its side face exposed up to
        /// the top. It takes a broken corner end (ENV_CapEnd_*), and the cap stops short of it.</summary>
        public static bool CornerExposed(Edge top, bool atLeft, List<Edge> allEdges)
        {
            Rect r = top.Owner.Rect;
            if (atLeft ? Mathf.Abs(top.From - r.xMin) > Eps : Mathf.Abs(top.To - r.xMax) > Eps) return false;
            Side side = atLeft ? Side.Left : Side.Right;
            return allEdges.Any(e => e.Owner.Name == top.Owner.Name && e.Side == side && e.To >= r.yMax - Eps && e.Length >= Mathf.Min(WalkBand, r.height) - Eps);
        }

        /// <summary>The corner end slot for a top (one of four by hash), or null when the kit lacks it.</summary>
        public static string CapEndSlot(Edge top, bool atLeft)
        {
            int v = Mathf.Min(3, (int)(Hash(top.Owner.Name, 900 + (atLeft ? 0 : 1)) * 4f));
            string slot = "ENV_CapEnd_" + v;
            return EnvironmentKit.Get(slot) != null ? slot : null;
        }
    }
}
