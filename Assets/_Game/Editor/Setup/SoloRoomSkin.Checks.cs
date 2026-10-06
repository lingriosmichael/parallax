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

            foreach (Edge e in ExposedEdges(solids).Where(e => e.Owner.Grip && e.Side is Side.Left or Side.Right && !FacesOut(e, content)))
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

        /// <summary>D-114 (2): every side face's drawn strip reaches no further than ChipInset outside its collider face (the
        /// chipped strips cut in no deeper than ChipInset inside it, by construction in Tools/Art/env_kit.py), and the fill stops
        /// ChipInset inside a wholly exposed side. Checked for the strip each face actually draws.</summary>
        public static List<string> CheckSideStrips(string id, SoloRoomDefinition room)
        {
            var errors = new List<string>();
            if (!ChipsReady(room)) { errors.Add($"{id}: the broken-stone strips aren't in the kit (run Tools/Art/env_kit.py and import it)."); return errors; }
            List<Solid> solids = Solids(room);
            Bounds content = SoloRoomBuilder.ComputeRoomBounds(room, 0f);
            foreach (Edge e in ExposedEdges(solids).Where(e => e.Side is Side.Left or Side.Right && !FacesOut(e, content) && e.Owner.Shape is Shape.Block or Shape.PitBottom))
            {
                string slot = ChipSlot(room, e);
                if (slot == null) { errors.Add($"{id}: {e.Owner.Name}'s {e.Side} face has no broken-stone strip."); continue; }
                Rect strip = SideStrip(e, EnvironmentKit.Get(slot));
                float outside = e.Side == Side.Left ? e.Line - strip.xMin : strip.xMax - e.Line;
                if (outside > ChipInset + 1e-3f)
                    errors.Add($"{id}: {e.Owner.Name}'s {e.Side} face draws {outside:F3} u outside its collider; at most {ChipInset:F2} (D-114 (2)).");
            }
            return errors;
        }
    }
}
