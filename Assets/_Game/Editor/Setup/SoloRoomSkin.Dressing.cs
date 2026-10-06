using System.Collections.Generic;
using System.Linq;
using Parallax.Editor.Art;
using Parallax.Editor.Levels;
using Parallax.Core;
using UnityEngine;

namespace Parallax.Editor.Setup
{
    /// <summary>PAX-A15 §2.4: the play layer's dressing (moss, rubble, drapes, banners, glyphs), placed by rule from the
    /// surface alone, and the keep-out zones it respects. Split from SoloRoomSkin (A16) to keep each file small.</summary>
    public static partial class SoloRoomSkin
    {
        // ---------- dressing (§2.4: a function of the surface only, so trap surfaces are dressed like real ones) ----------

        delegate SpriteRenderer Placer(Solid s, string name, string slot, Vector2 centre, Vector2 size, float z);

        /// <summary>Gauntlet: variants 3–5 live in the second atlas; a tiny depth step keeps each atlas's sprites consecutive
        /// in the draw order, so they batch (alternating atlases at one depth broke every batch).</summary>
        static float AtlasNudge(string slot) => 0f;   // round 9: all dressing in one atlas

        static void Dress(SoloRoomDefinition room, List<Solid> solids, List<Edge> edges, Placer place)
        {
            LevelLooks.Dressing dress = LevelLooks.For(LevelLooks.LevelOf(room)).Dress;
            List<Rect> keepOut = KeepOut(room);
            bool flips = room.Elements.Any(e => e.Kind == SoloRoomElementKind.GravityFlip);
            bool Clear(Rect r) => !keepOut.Any(k => k.Overlaps(r));
            // Gauntlet: tufts and ivy skip only what's visible (hazards, lanes, the door, checkpoints, flips), never a trap's
            // invisible trigger box: a bare patch over a trigger would mark the trap.
            List<Rect> visibleKeepOut = VisibleKeepOut(room);
            bool ClearOfVisible(Rect r) => !visibleKeepOut.Any(k => k.Overlaps(r));
            // Tufts sit behind the stone and every trap (ZTuft), so they grow anywhere but the door and checkpoints: a revealed
            // spike draws over them, and a hidden one is never marked by a bare patch.
            List<Rect> tuftKeepOut = TuftKeepOut(room);

            int rubbleCount = 0, drapeCount = 0;
            float lowestTop = edges.Where(e => e.Side == Side.Top && e.Owner.Shape != Shape.PitBottom).Select(e => e.Line).DefaultIfEmpty(0f).Min();
            foreach (Edge e in edges.Where(e => e.Side == Side.Top && e.Owner.Shape is Shape.Block or Shape.Slab))
            {
                // Gauntlet P1-R3: tufts one by one (a strip repeated them): six shapes, random size, flips and gaps, some
                // bare stone. Low (at most 0.42 u above the line), so the walk line stays clear.
                {
                    float x = e.From + 0.2f * Hash(e.Owner.Name, 1);
                    for (int i = 0; x < e.To && i < 80; i++)
                    {
                        float pick = Hash(e.Owner.Name, 60 + i);
                        string slot = "ENV_Moss_" + (int)(Hash(e.Owner.Name, 50 + i) * 3f);   // one atlas (batching); variety from flips and sizes
                        EnvironmentKit.Slot def = EnvironmentKit.Get(slot);
                        if (def == null) { x += 0.5f; continue; }
                        float k = (0.55f + 0.6f * Hash(e.Owner.Name, 70 + i)) * (0.75f + dress.Moss * 0.5f);
                        Vector2 size = new(def.width * k, Mathf.Min(def.height * k, 0.5f));
                        var centre = new Vector2(x + size.x * 0.5f, e.Line + size.y * 0.5f - 0.12f);
                        x += size.x * (0.45f + 0.5f * Hash(e.Owner.Name, 80 + i)) + (pick < 0.2f ? 0.3f + 0.7f * Hash(e.Owner.Name, 90 + i) : 0f);
                        if (centre.x + size.x * 0.5f > e.To || pick < 0.08f) continue;
                        if (Hash(e.Owner.Name, 95 + i) < 0.5f) size.x = -size.x;   // flipped
                        if (!tuftKeepOut.Any(k => k.Overlaps(Box(centre, new Vector2(Mathf.Abs(size.x), size.y))))) place(e.Owner, $"Moss_{Idx(e)}_{i}", slot, centre, size, ZTuft + AtlasNudge(slot));
                    }
                }
                // Gauntlet: ivy curtains down the block's face from its lip (the concept's ivy-covered bridge face), random
                // pieces and lengths, never above the walkable line; the same rule for real and disguised blocks (P10).
                if (e.Owner.Shape == Shape.Block && e.Owner.Rect.height >= 1.2f && !flips)
                {
                    // Irregular: a random walk along the lip, some gaps, lengths from a short tuft to a long curtain.
                    float x = e.From + 0.3f * Hash(e.Owner.Name, 399);
                    for (int i = 0; x < e.To && i < 40; i++)
                    {
                        float pick = Hash(e.Owner.Name, 405 + i);
                        string slot = "ENV_Drape_" + (int)(Hash(e.Owner.Name, 400 + i) * 6f);
                        EnvironmentKit.Slot def = EnvironmentKit.Get(slot);
                        if (def == null) { x += 0.5f; continue; }
                        // Lengths from a short tuft to a long curtain (a skewed spread, so most are short and a few run deep).
                        // PAX-106 (D-114 (1), R7): never past the face's top band (measured from the walk line; it hangs from 0.18
                        // under it).
                        float u = Hash(e.Owner.Name, 410 + i);
                        float h = Mathf.Min(e.Owner.Rect.height - 0.3f, 0.35f + 3.2f * u * u * u + 0.4f * Hash(e.Owner.Name, 415 + i));
                        h = Mathf.Min(h, TopBand(e.Owner.Rect.height) - IvyDrop);
                        if (h < MinDressing) { x += 0.4f; continue; }
                        float w = def.width * h / def.height * (0.75f + 0.5f * Hash(e.Owner.Name, 425 + i));
                        var centre = new Vector2(x + w * 0.5f, e.Line - IvyDrop - h * 0.5f);
                        x += w * (0.6f + 1.1f * Hash(e.Owner.Name, 420 + i)) + (pick < 0.4f ? 1.0f + 2.2f * Hash(e.Owner.Name, 430 + i) : 0f);
                        if (pick < 0.22f) continue;   // a gap
                        if (centre.x - w * 0.5f < e.From - 0.05f || centre.x + w * 0.5f > e.To + 0.05f) continue;
                        if (ClearOfVisible(Box(centre, new Vector2(w, h)))) place(e.Owner, $"Ivy_{Idx(e)}_{i}", slot, centre, new Vector2(Hash(e.Owner.Name, 435 + i) < 0.5f ? -w : w, h), ZDrape + AtlasNudge(slot));
                    }
                }
                // Gauntlet (the developer: front ferns fixed in the world, never over a trap, not blurry): sharp fern clumps at
                // the foot of a tall block's face, well below its walk line (their tops at least 1 u under it), clear of
                // everything the player must see; the same rule for real and disguised blocks (P10).
                if (e.Owner.Shape == Shape.Block && e.Owner.Rect.height >= 2.4f)
                {
                    Rect b = e.Owner.Rect;
                    float x = e.From + 0.4f * Hash(e.Owner.Name, 600);
                    for (int i = 0; x < e.To && i < 30; i++)
                    {
                        string slot = "ENV_Fern_" + (int)(Hash(e.Owner.Name, 610 + i) * 3f);
                        EnvironmentKit.Slot def = EnvironmentKit.Get(slot);
                        if (def == null) { x += 1f; continue; }
                        float w = def.width * (0.7f + 0.5f * Hash(e.Owner.Name, 620 + i)), h = Mathf.Min(def.height * w / def.width, b.height - 1f);
                        if (h < 0.4f) break;
                        var centre = new Vector2(x + w * 0.5f, b.yMin + h * 0.5f - 0.05f);
                        x += w * (0.55f + 0.5f * Hash(e.Owner.Name, 630 + i)) + (Hash(e.Owner.Name, 640 + i) < 0.3f ? 0.6f + Hash(e.Owner.Name, 650 + i) : 0f);
                        if (centre.x + w * 0.5f > e.To) continue;
                        if (ClearOfVisible(Box(centre, new Vector2(w, h)))) place(e.Owner, $"Fern_{Idx(e)}_{i}", slot, centre, new Vector2(Hash(e.Owner.Name, 660 + i) < 0.5f ? -w : w, h), ZPanel);
                    }
                }
                if (dress.Rubble && e.Length >= 3f && rubbleCount < MaxRubble)
                {
                    string slot = "ENV_Rubble_" + (int)(Hash(e.Owner.Name, 90) * 2f);
                    EnvironmentKit.Slot def = EnvironmentKit.Get(slot);
                    Vector2 size = new(def.width * 0.6f, def.height * 0.6f);
                    float x = Hash(e.Owner.Name, 91) < 0.5f ? e.From + size.x * 0.6f : e.To - size.x * 0.6f;
                    var centre = new Vector2(x, e.Line + size.y * 0.5f - 0.08f);
                    if (Clear(Box(centre, size))) { place(e.Owner, "Rubble_" + Idx(e), slot, centre, size, ZMoss); rubbleCount++; }
                }
            }

            if (!flips)
                // Gauntlet P1-R3: short roots and ivy under every overhang one by one (the underside strip no longer carries
                // any): six shapes, random lengths (at most 1 u, so they never read as a lower ceiling or a climbable vine).
                foreach (Edge e in edges.Where(e => e.Side == Side.Bottom && e.Owner.Shape is Shape.Block or Shape.Slab && e.Length >= 1f))
                {
                    if (ClearBelow(e, solids) < 2.2f) continue;
                    float x = e.From + 0.2f * Hash(e.Owner.Name, 119);
                    for (int i = 0; x < e.To && i < 50; i++)
                    {
                        float pick = Hash(e.Owner.Name, 125 + i);
                        string slot = "ENV_Drape_" + (int)(Hash(e.Owner.Name, 120 + i) * 6f);
                        EnvironmentKit.Slot def = EnvironmentKit.Get(slot);
                        if (def == null) { x += 0.5f; continue; }
                        float h = 0.25f + 0.75f * Hash(e.Owner.Name, 130 + i) * Hash(e.Owner.Name, 135 + i);
                        // PAX-106 (D-114 (1), R7): at most the face's top band below the overhang.
                        h = Mathf.Min(h, TopBand(e.Owner.Rect.height));
                        if (h < MinDressing) { x += 0.4f; continue; }
                        float w = def.width * h / def.height * (0.7f + 0.5f * Hash(e.Owner.Name, 140 + i));
                        var centre = new Vector2(x + w * 0.5f, e.Line - h * 0.5f + 0.05f);
                        x += w * (0.5f + 0.8f * Hash(e.Owner.Name, 145 + i)) + (pick < 0.3f ? 0.3f + Hash(e.Owner.Name, 150 + i) : 0f);
                        if (centre.x + w * 0.5f > e.To || pick < 0.15f) continue;
                        if (ClearOfVisible(Box(centre, new Vector2(w, h)))) { place(e.Owner, $"Drape_{Idx(e)}_{i}", slot, centre, new Vector2(Hash(e.Owner.Name, 155 + i) < 0.5f ? -w : w, h), ZDrape + 0.0001f + AtlasNudge(slot)); drapeCount++; }
                    }
                }

            // PAX-106 (D-110 amendment 4 (2)): moss means grip. Each side face of a grip solid wears a moss carpet over at least
            // GripMossCover of its height, brighter than the stone: the kit's ENV_MossCarpet when it has one (R9), stacked
            // ENV_Moss tufts otherwise. A trap's carpet hangs on its skin (a grip falling block's moves and lands with it).
            foreach (Edge e in edges.Where(e => e.Owner.Grip && e.Side is Side.Left or Side.Right))
                GripMoss(e, place, ClearOfVisible);

            // Banners and glyph panels: on the biggest faces, at most two of each per room, largest first (ties by name).
            List<Solid> faces = solids.Where(s => s.Shape == Shape.Block).OrderByDescending(s => s.Rect.width * s.Rect.height).ThenBy(s => s.Name, System.StringComparer.Ordinal).ToList();
            if (dress.Banners)
            {
                int n = 0;
                foreach (Solid s in faces.Where(s => s.Kind == SoloRoomElementKind.Wall && s.Rect.height >= 3f && s.Rect.width >= 0.9f))
                {
                    if (n >= 2) break;
                    string slot = "ENV_Banner_" + (int)(Hash(s.Name, 150) * 2f);
                    EnvironmentKit.Slot def = EnvironmentKit.Get(slot);
                    float w = Mathf.Min(def.width, s.Rect.width * 0.9f), h = Mathf.Min(def.height * w / def.width, s.Rect.height - 1f);
                    var centre = new Vector2(s.Rect.center.x, s.Rect.yMax - 0.4f - h * 0.5f);
                    if (Clear(Box(centre, new Vector2(w, h)))) { place(s, "Banner", slot, centre, new Vector2(w, h), ZPanel); n++; }
                }
            }
            if (dress.Glyphs)
            {
                int n = 0;
                foreach (Solid s in faces.Where(s => s.Rect.width >= 3f && s.Rect.height >= 2.5f))
                {
                    if (n >= 2) break;
                    string slot = "ENV_Glyph_" + (int)(Hash(s.Name, 170) * 2f);
                    EnvironmentKit.Slot def = EnvironmentKit.Get(slot);
                    Vector2 size = new(def.width * 0.85f, def.height * 0.85f);
                    var centre = new Vector2(s.Rect.center.x + (Hash(s.Name, 171) - 0.5f) * (s.Rect.width - size.x - 1f), s.Rect.center.y - 0.2f);
                    if (Clear(Box(centre, size))) { place(s, "Glyph", slot, centre, size, ZPanel); n++; }
                }
            }
        }

        // ---------- PAX-106 (D-114 (1), D-110 amendment 4 (2)) ----------

        /// <summary>D-114 (1): how far moss and ivy may hang down an ordinary face from its top: 30% of its height, at most 0.6 u.</summary>
        public static float TopBand(float faceHeight) => Mathf.Min(0.3f * faceHeight, 0.6f);
        /// <summary>A lip curtain hangs from this far under the walk line; dressing shorter than MinDressing isn't placed.</summary>
        public const float IvyDrop = 0.18f, MinDressing = 0.12f;
        /// <summary>D-110 amendment 4 (2): a grip face's moss covers at least this share of its height.</summary>
        public const float GripMossCover = 0.8f;
        /// <summary>D-118: the stacked grip-moss tufts are drawn turned upright: their footprint is (width across the face,
        /// height down it); the sprite lies along the height.</summary>
        static bool UprightMoss(string name) => name.StartsWith("GripMoss_") && !name.Contains("Carpet") && name.Split('_').Length >= 3;
        static readonly Color GripMossTint = new(0.8f, 1f, 0.52f, 1f);

        static void GripMoss(Edge e, Placer place, System.Func<Rect, bool> clear)
        {
            string side = e.Side == Side.Left ? "L" : "R", n = e.Owner.Name + side;   // both faces of a block share their y (Idx)
            float into = e.Side == Side.Left ? 1f : -1f;   // from the face line into the stone
            if (EnvironmentKit.Get("ENV_MossCarpet") is EnvironmentKit.Slot carpet)
            {
                float w = 0.42f, h = e.Length * 0.96f;
                var centre = new Vector2(e.Line + into * (w * 0.5f - 0.06f), (e.From + e.To) * 0.5f);
                if (clear(Box(centre, new Vector2(w, h)))) Tint(place(e.Owner, $"GripMoss_{side}{Idx(e)}", carpet.name, centre, new Vector2(e.Side == Side.Left ? w : -w, h), ZMoss));
                return;
            }
            // D-118 (the developer: "not drawn on horizontally like it is now but vertically"): a strand of tufts stood upright
            // (each sprite turned a quarter, UprightMoss), narrow across the face line and long down it, overlapping from the foot
            // to the top. Sizes here are the footprint (width across, height down the face).
            float y = e.From + 0.02f;
            for (int i = 0; y < e.To - 0.06f && i < 400; i++)
            {
                string slot = "ENV_Moss_" + (int)(Hash(n, 900 + i) * 3f);
                EnvironmentKit.Slot def = EnvironmentKit.Get(slot);
                if (def == null) return;
                float h = 0.55f + 0.25f * Hash(n, 920 + i), w = h * def.height / def.width * (0.9f + 0.3f * Hash(n, 940 + i));
                float inward = 0.02f + 0.06f * Hash(n, 960 + i);
                var centre = new Vector2(e.Line + into * inward, Mathf.Min(y + h * 0.5f, e.To - h * 0.5f));
                if (clear(Box(centre, new Vector2(w, h))))
                    Tint(place(e.Owner, $"GripMoss_{side}{Idx(e)}_{i}", slot, centre, new Vector2(w, Hash(n, 980 + i) < 0.5f ? -h : h), ZMoss));
                y += h * (0.42f + 0.22f * Hash(n, 990 + i));
            }

            static void Tint(SpriteRenderer r) { if (r != null) r.color = GripMossTint; }
        }

        /// <summary>PAX-106 (tests): the dressing a room gets, without building it: each piece's owner, name, slot and box.</summary>
        public static List<(string owner, string name, string slot, Rect box)> PlanDressing(SoloRoomDefinition room)
        {
            List<Solid> solids = Solids(room);
            Bounds content = SoloRoomBuilder.ComputeRoomBounds(room, 0f);
            List<Edge> edges = ExposedEdges(solids).Where(e => !FacesOut(e, content)).ToList();
            var list = new List<(string, string, string, Rect)>();
            Dress(room, solids, edges, (s, name, slot, centre, size, z) =>
            {
                Vector2 a = new(Mathf.Abs(size.x), Mathf.Abs(size.y));
                list.Add((s.Name, name, slot, new Rect(centre - a * 0.5f, a)));
                return null;
            });
            return list;
        }

        /// <summary>Where dressing never goes: within 0.5 u of hazards, checkpoints, trigger boxes, flip zones and arrow
        /// lanes, and within 1 u of the door.</summary>
        public static List<Rect> KeepOut(SoloRoomDefinition room) => KeepOut(room, true);

        /// <summary>D-103: what ivy and drapes avoid: everything the player must see, never an invisible trigger.</summary>
        public static List<Rect> VisibleKeepOut(SoloRoomDefinition room) => KeepOut(room, false, skipHidden: true);

        /// <summary>D-103: what tufts avoid (they draw behind the stone and every trap): the door and the checkpoints.</summary>
        public static List<Rect> TuftKeepOut(SoloRoomDefinition room) =>
            room.Elements.Where(x => x.Kind is SoloRoomElementKind.Door or SoloRoomElementKind.Checkpoint)
                .Select(x => Grow(new Rect(room.Origin + x.Position - x.Size * 0.5f, x.Size), 0.6f)).ToList();

        static List<Rect> KeepOut(SoloRoomDefinition room, bool triggers, bool skipHidden = false)
        {
            var list = new List<Rect>();
            foreach (SoloRoomElement e in room.Elements)
            {
                if (skipHidden && e.Kind == SoloRoomElementKind.HiddenSpikes) continue;
                Vector2 p = room.Origin + e.Position;
                switch (e.Kind)
                {
                    case SoloRoomElementKind.Hazard: case SoloRoomElementKind.HiddenSpikes: case SoloRoomElementKind.GravityFlip:
                    case SoloRoomElementKind.Inverter: case SoloRoomElementKind.Geyser: case SoloRoomElementKind.StormCloud:
                        list.Add(Grow(new Rect(p - e.Size * 0.5f, e.Size), 0.5f)); break;
                    case SoloRoomElementKind.Checkpoint:
                        list.Add(Grow(new Rect(p + new Vector2(-0.5f, 0f), new Vector2(1f, 1.5f)), 0.5f)); break;
                    case SoloRoomElementKind.Door:
                    {
                        // Both of the door's poses: a retreat trap moves it by its offset.
                        var door = new Rect(p - e.Size * 0.5f, e.Size);
                        list.Add(Grow(door, 1f));
                        foreach (SoloRoomElement retreat in room.Elements.Where(x => x.Kind == SoloRoomElementKind.DoorRetreat))
                            list.Add(Grow(new Rect(door.position + retreat.Settings.Offset, door.size), 1f));
                        break;
                    }
                    case SoloRoomElementKind.Arrow when e.Settings.Arrow.IsConfigured:
                    {
                        ArrowLane lane = e.Settings.Arrow;
                        float face = lane.Direction == ArrowDirection.Right ? p.x + e.Size.x * 0.5f : p.x - e.Size.x * 0.5f;
                        float end = room.Origin.x + lane.LaneEndX;
                        var laneRect = Rect.MinMaxRect(Mathf.Min(face, end), room.Origin.y + lane.LaneY - lane.Thickness * 0.5f, Mathf.Max(face, end), room.Origin.y + lane.LaneY + lane.Thickness * 0.5f);
                        list.Add(Grow(laneRect, 0.5f));
                        break;
                    }
                }
                if (triggers && e.SecondarySize != Vector2.zero && e.Kind != SoloRoomElementKind.Door)
                    list.Add(Grow(new Rect(room.Origin + e.SecondaryPosition - e.SecondarySize * 0.5f, e.SecondarySize), 0.5f));
            }
            return list;
        }

        static float ClearBelow(Edge e, List<Solid> solids)
        {
            float best = float.MaxValue;
            foreach (Solid o in solids)
            {
                if (o.Name == e.Owner.Name || o.Rect.xMax <= e.From || o.Rect.xMin >= e.To || o.Rect.yMax > e.Line + Eps) continue;
                best = Mathf.Min(best, e.Line - o.Rect.yMax);
            }
            return best;
        }

        static Rect Box(Vector2 centre, Vector2 size) => new(centre - size * 0.5f, size);
        static Rect Grow(Rect r, float d) => Rect.MinMaxRect(r.xMin - d, r.yMin - d, r.xMax + d, r.yMax + d);

        /// <summary>A deterministic fraction in [0, 1) from a name and a salt (FNV-1a): placement never uses Random.</summary>
        public static float Hash(string name, int salt)
        {
            unchecked
            {
                uint h = 2166136261u;
                foreach (char c in name) { h ^= c; h *= 16777619u; }
                h ^= (uint)salt; h *= 16777619u;
                h ^= h >> 15; h *= 2246822519u; h ^= h >> 13;
                return (h & 0xFFFFFF) / 16777216f;
            }
        }
    }
}
