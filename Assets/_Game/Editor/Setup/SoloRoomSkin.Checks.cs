using System.Collections.Generic;
using System.Linq;
using Parallax.Editor.Art;
using UnityEngine;

namespace Parallax.Editor.Setup
{
    /// <summary>PAX-106: the moss sign and the broken-stone outline, checked from the plan (PlanDressing, the side strips'
    /// geometry and the kit's slots), so a test needs no scene. Each returns its errors, empty when the room passes.</summary>
    public static partial class SoloRoomSkin
    {
        /// <summary>D-110 amendment 4 (2) and D-114 (1), ruling R10: every grip face wears moss over at least GripMossCover of
        /// its height; no ordinary face has moss or ivy below its top band (lip curtains end within it, under-overhang drapes
        /// are no longer than it); grip moss is only on grip solids.</summary>
        public static List<string> CheckMossSign(string id, SoloRoomDefinition room)
        {
            var errors = new List<string>();
            List<Solid> solids = Solids(room);
            Dictionary<string, Solid> byName = solids.GroupBy(s => s.Name).ToDictionary(g => g.Key, g => g.First());
            Bounds content = SoloRoomBuilder.ComputeRoomBounds(room, 0f);
            List<(string owner, string name, string slot, Rect box)> plan = PlanDressing(room);

            foreach (Edge e in ExposedEdges(solids).Where(e => e.Owner.Grip && e.Side is Side.Left or Side.Right && !FacesOut(e, content, room)))
            {
                var spans = plan.Where(p => p.owner == e.Owner.Name && p.name.StartsWith("GripMoss_") && p.box.xMin < e.Line + 0.3f && p.box.xMax > e.Line - 0.3f)
                    .Select(p => (Mathf.Max(p.box.yMin, e.From), Mathf.Min(p.box.yMax, e.To))).Where(s => s.Item2 > s.Item1).OrderBy(s => s.Item1).ToList();
                float covered = 0f, reach = e.From;
                foreach ((float lo, float hi) in spans)
                {
                    if (hi <= reach) continue;
                    covered += hi - Mathf.Max(lo, reach);
                    reach = hi;
                }
                if (covered < GripMossCover * e.Length - 1e-3f)
                    errors.Add($"{id}: grip face {e.Owner.Name} ({e.Side}) is mossy over {covered:F2} of {e.Length:F2} u ({covered / e.Length:P0}); it needs {GripMossCover:P0} (D-110 amendment 4 (2)).");
            }

            foreach ((string owner, string name, string slot, Rect box) in plan)
            {
                if (!byName.TryGetValue(owner, out Solid s)) continue;
                float band = TopBand(s.Rect.height);
                if (name.StartsWith("GripMoss_") && !s.Grip)
                    errors.Add($"{id}: {owner} is no grip solid but wears grip moss ({name}).");
                else if (name.StartsWith("Ivy_") && box.yMin < s.Rect.yMax - band - 1e-3f)
                    errors.Add($"{id}: {owner}'s {name} hangs to {s.Rect.yMax - box.yMin:F2} u under its top; its top band is {band:F2} u (D-114 (1)).");
                else if (name.StartsWith("Drape_") && box.height > band + 1e-3f)
                    errors.Add($"{id}: {owner}'s {name} under its overhang is {box.height:F2} u long; at most {band:F2} u (D-114 (1)).");
            }
            return errors;
        }

        /// <summary>D-114 (2), PAX-A18 W3: every side face's drawn strips reach no further outside its collider face than their
        /// class allows (BandReach for band strips, DeepReach for deep ones; env_bumpy.py cuts them so inward too), and no deep
        /// strip stands where a cat can be (its front overlaps no reach zone, and it's out of the face's walk band). Checked for
        /// the strips each face actually draws.</summary>
        public static List<string> CheckSideStrips(string id, SoloRoomDefinition room)
        {
            var errors = new List<string>();
            if (!ChipsReady(room)) { errors.Add($"{id}: the broken-stone strips aren't in the kit (run Tools/Art/env_kit.py and import it)."); return errors; }
            List<Solid> solids = Solids(room);
            Bounds content = SoloRoomBuilder.ComputeRoomBounds(room, 0f);
            bool flips = room.Elements.Any(x => x.Kind == SoloRoomElementKind.GravityFlip);
            List<Edge> allEdges = ExposedEdges(solids);
            List<Rect> zones = ReachZones(room, solids), hazards = HazardZones(room);
            foreach (Edge e in allEdges.Where(e => e.Side is Side.Left or Side.Right && !FacesOut(e, content, room) && e.Owner.Shape is Shape.Block or Shape.PitBottom))
            {
                foreach (FaceSpan span in FaceSpans(e, zones, allEdges, flips, hazards))
                {
                    string slot = SpanSlot(room, e, span);
                    if (slot == null) { errors.Add($"{id}: {e.Owner.Name}'s {e.Side} face has no broken-stone strip."); continue; }
                    bool deep = slot.StartsWith("ENV_SideDeep_");
                    Rect strip = SideStrip(new Edge(e.Owner, e.Side, span.From, span.To, e.Line), EnvironmentKit.Get(slot));
                    float outside = e.Side == Side.Left ? e.Line - strip.xMin : strip.xMax - e.Line, limit = deep ? DeepReach : BandReach;   // ENV_SideIn_* (a moving trap's) stands proud within BandReach too (2026-10-16)
                    if (outside > limit + 1e-3f)
                        errors.Add($"{id}: {e.Owner.Name}'s {e.Side} face draws {outside:F3} u outside its collider at y {span.From:F2}-{span.To:F2}; at most {limit:F2} (PAX-A18 W3).");
                    if (!deep) continue;
                    float x0 = e.Side == Side.Left ? e.Line - DeepReach : e.Line, x1 = e.Side == Side.Left ? e.Line : e.Line + DeepReach;
                    var front = Rect.MinMaxRect(x0, span.From, x1, span.To);
                    Rect hit = zones.FirstOrDefault(z => z.Overlaps(front));
                    bool inBand = span.To > e.Owner.Rect.yMax - WalkBand + 1e-3f && allEdges.Any(t => t.Owner.Name == e.Owner.Name && t.Side == Side.Top);
                    if (e.Owner.Trap || hit != default || inBand)
                        errors.Add($"{id}: {e.Owner.Name}'s {e.Side} face draws a deep strip at y {span.From:F2}-{span.To:F2}, where a cat can be (PAX-A18 W3; zone {hit}, walk band {inBand}).");
                }
            }
            return errors;
        }

        /// <summary>PAX-A18 W3: the walkable tops' art. The cap and the slabs carry lumps no taller than TopLump beyond the line
        /// (env_bumpy.TOP_LUMP) and nothing missing at the line; a flip room's undersides use the walkable underside.</summary>
        public static List<string> CheckTops(string id, SoloRoomDefinition room)
        {
            var errors = new List<string>();
            foreach (string slot in new[] { "ENV_Cap", "ENV_SlabFlip", "ENV_UnderWalk" })
                if (EnvironmentKit.Get(slot) == null) errors.Add($"{id}: {slot} isn't in the kit (PAX-A18 W3).");
            bool flips = room.Elements.Any(x => x.Kind == SoloRoomElementKind.GravityFlip);
            if (flips && UnderSlot(true)?.name != "ENV_UnderWalk")
                errors.Add($"{id}: a flip room's undersides must draw ENV_UnderWalk (a gravity-up cat stands on them).");
            return errors;
        }

        /// <summary>env_bumpy.TOP_LUMP: how far a walkable top's drawn stone may stand beyond its line.</summary>
        public const float TopLump = 0.05f;   // 2026-10-17: was 0.09 (env_bumpy.TOP_LUMP)
    }
}
