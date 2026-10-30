using System.Collections.Generic;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Art;
using Parallax.Editor.Levels;
using Parallax.Gameplay.Reality;
using Parallax.Gameplay.Rooms;
using Parallax.Presentation;
using UnityEditor;
using UnityEngine;

namespace Parallax.Editor.Setup
{
    /// <summary>PAX-A15 §2.4: the play layer's look, built inside the room build (so a saved level matches a fresh build of
    /// its layout). Fixed geometry gets the room's stone fill, world-tiled; then (after the trap art has copied that skin
    /// into every disguised trap, P10) every solid, real or disguised, gets the same trims from the same rule over the
    /// room's combined silhouette: a mossy cap on each exposed top, an underside on each exposed bottom, a wall face on each
    /// exposed side, a slab for 0.5 u shapes, a shaft for posts, water on pit bottoms; and dressing, placed by rule. A
    /// trap's trims and dressing hang under its art skin (ShownWith), so they move, hide and return with it. No collider,
    /// trigger or timing is touched. Off (and removed) without LevelLookConfig or the kit.</summary>
    public static partial class SoloRoomSkin
    {
        public const string EnvRootName = "Env";
        public const string TrimsName = "Trims";
        const float Eps = 0.02f;
        // Within a sorting order, nearer the camera draws later (the 2D renderer sorts by depth after the order).
        // The fill sits just in front of anything else at the geometry's order (the template's frozen sandbox objects share it).
        // Gauntlet: tufts sit behind the stone and the traps (positive is farther from the camera).
        const float ZTuft = 0.012f;
        /// <summary>2026-10-17: a hole's broken rim sits behind the falling block's skin (z 0 at the same order) until it falls.</summary>
        const float HoleRimZ = 0.004f;
        /// <summary>2026-10-17: how far a trap's lent stone runs past DeepReach into its neighbour (env_bumpy STRIP_OVERLAP +
        /// STRIP_BLEND, so it covers the neighbour's whole side strip), and how far its lent cap runs (past a broken top corner,
        /// ENV_CapEnd's 0.45 u).</summary>
        const float ReachPastStrip = 0.18f, CapReach = 0.5f;
        /// <summary>An underside with stone closer under it than this (world units) gets no hanging underside strip.</summary>
        const float ShallowOverhang = 0.3f;
        /// <summary>2026-10-17: how far a vine's drawn top may reach up to the stone over its grab box, and how far into it.</summary>
        const float VineReachUp = 4f, VineIntoStone = 0.08f;
        const float ZFill = -0.005f, ZSide = -0.010f, ZUnder = -0.011f, ZCap = -0.012f, ZDrape = -0.015f, ZPanel = -0.02f, ZMoss = -0.025f;
        const int MaxDrapes = 4, MaxRubble = 2;
        const float TrapTrimZ = -0.05f;

        public enum Side { Top, Bottom, Left, Right }
        public enum Shape { Block, Slab, SlimPost, Post, PitBottom }

        public readonly struct Solid
        {
            public readonly string Name; public readonly Rect Rect; public readonly bool Trap; public readonly SoloRoomElementKind Kind; public readonly bool Shrinks;
            /// <summary>PAX-106 (D-110 amendment 4): its side faces can be held (a grip wall, or a grip falling block), so they wear
            /// the moss carpet.</summary>
            public readonly bool Grip;
            /// <summary>2026-10-18 visual review (L020: "the walkway slab is drawn as its own one-course slab on top of the
            /// full-height wall"): a thin fixed floor lying wholly on fixed stone is that stone's top course, a block, not a slab.</summary>
            public readonly bool Bedded;
            public Solid(string name, Rect rect, bool trap, SoloRoomElementKind kind, bool shrinks = false, bool grip = false, bool bedded = false) { Name = name; Rect = rect; Trap = trap; Kind = kind; Shrinks = shrinks; Grip = grip; Bedded = bedded; }
            public Shape Shape =>
                Kind == SoloRoomElementKind.PitBottom ? Shape.PitBottom
                : Rect.height <= 0.55f && Rect.width > Rect.height && !Bedded ? Shape.Slab
                // D-111 (the developer, 2026-10-04: "give the walls that are falling a similar look and art as the floor"): a
                // falling block is never a column; it wears the floor's stone, fill and edges.
                : Kind == SoloRoomElementKind.FallingBlock ? Shape.Block
                // The developer (2026-10-16): "All edges need to be a bit bumpy". A narrow upright is stone with broken sides
                // like any block (the painted columns, ENV_Post and ENV_SlimPost, had ruler-straight sides).
                : Shape.Block;
        }

        /// <summary>An exposed stretch of a solid's edge: along x for tops and bottoms, along y for sides.</summary>
        public readonly struct Edge
        {
            public readonly Solid Owner; public readonly Side Side; public readonly float From, To, Line;
            public Edge(Solid owner, Side side, float from, float to, float line) { Owner = owner; Side = side; From = from; To = to; Line = line; }
            public float Length => To - From;
        }

        public static bool Enabled => AssetDatabase.LoadAssetAtPath<LevelLookConfig>(LevelLookConfig.AssetPath) != null && EnvironmentKit.Ready(out _) && EnvironmentKit.MaterialsReady;
        public const string FillName = "Fill";

        // ---------- the silhouette (pure; tests and the capture tool use it) ----------

        public static List<Solid> Solids(SoloRoomDefinition room)
        {
            var list = new List<Solid>();
            foreach (SoloRoomElement e in room.Elements)
            {
                var rect = new Rect(room.Origin + e.Position - e.Size * 0.5f, e.Size);
                switch (e.Kind)
                {
                    case SoloRoomElementKind.Floor: case SoloRoomElementKind.Ceiling: case SoloRoomElementKind.Wall: case SoloRoomElementKind.PitBottom:
                        list.Add(new Solid(e.Name, rect, false, e.Kind)); break;
                    // PAX-105 (D-110 amendment 2): a grip wall wears the wall look; PAX-106 (amendment 4): and the moss carpet.
                    case SoloRoomElementKind.GripWall:
                        list.Add(new Solid(e.Name, rect, false, SoloRoomElementKind.Wall, grip: true)); break;
                    case SoloRoomElementKind.CollapsingFloor: case SoloRoomElementKind.FakePlatform:
                        list.Add(new Solid(e.Name, rect, true, e.Kind)); break;
                    case SoloRoomElementKind.FallingBlock:
                        list.Add(new Solid(e.Name, rect, true, e.Kind, grip: e.Settings.Grip.IsConfigured)); break;
                    case SoloRoomElementKind.ShrinkingFloor:
                        list.Add(new Solid(e.Name, rect, true, e.Kind, shrinks: true)); break;
                    case SoloRoomElementKind.MovingTrap:
                        if (e.Settings.MovingKind != MovingTrapKind.Hazard) list.Add(new Solid(e.Name, rect, true, e.Kind));
                        break;
                    case SoloRoomElementKind.Arrow:
                        if (e.Settings.Arrow.IsConfigured && e.Settings.Arrow.Disguised) list.Add(new Solid(e.Name, rect, true, e.Kind));
                        break;
                }
            }
            // A thin fixed floor wholly on fixed stone is bedded (a block: Solid.Bedded).
            for (int i = 0; i < list.Count; i++)
            {
                Solid t = list[i];
                if (t.Trap || t.Rect.height > 0.55f) continue;
                float under = list.Where(o => !o.Trap && o.Name != t.Name && Mathf.Abs(o.Rect.yMax - t.Rect.yMin) < Eps)
                    .Sum(o => Mathf.Max(0f, Mathf.Min(o.Rect.xMax, t.Rect.xMax) - Mathf.Max(o.Rect.xMin, t.Rect.xMin)));
                if (under >= t.Rect.width - Eps) list[i] = new Solid(t.Name, t.Rect, t.Trap, t.Kind, t.Shrinks, t.Grip, bedded: true);
            }
            // The room's side walls (SoloRoomBuilder.BuildRoomCore).
            list.Add(new Solid("Wall_Left", new Rect(room.Origin + new Vector2(-1f, -4f), new Vector2(1f, 12f)), false, SoloRoomElementKind.Wall));
            list.Add(new Solid("Wall_Right", new Rect(room.Origin + new Vector2(room.Width, -4f), new Vector2(1f, 12f)), false, SoloRoomElementKind.Wall));
            return list;
        }

        /// <summary>Every exposed stretch of every solid's edges. A stretch is hidden where another solid continues past
        /// the edge; on fixed geometry it's also left to a disguised trap that reaches the edge from inside (the trap
        /// draws over its host, so the trap carries that stretch's trim and keeps it when it moves).</summary>
        public static List<Edge> ExposedEdges(List<Solid> solids)
        {
            var edges = new List<Edge>();
            foreach (Solid s in solids)
            {
                Rect r = s.Rect;
                AddExposed(edges, s, Side.Top, r.xMin, r.xMax, r.yMax, solids);
                AddExposed(edges, s, Side.Bottom, r.xMin, r.xMax, r.yMin, solids);
                AddExposed(edges, s, Side.Left, r.yMin, r.yMax, r.xMin, solids);
                AddExposed(edges, s, Side.Right, r.yMin, r.yMax, r.xMax, solids);
            }
            return edges;
        }

        static void AddExposed(List<Edge> edges, Solid s, Side side, float from, float to, float line, List<Solid> solids)
        {
            var covered = new List<(float, float)>();
            bool horizontal = side is Side.Top or Side.Bottom;
            float outward = side is Side.Top or Side.Right ? 1f : -1f;
            foreach (Solid o in solids)
            {
                if (o.Name == s.Name) continue;
                Rect q = o.Rect;
                float qa = horizontal ? q.xMin : q.yMin, qb = horizontal ? q.xMax : q.yMax;
                float lo = horizontal ? q.yMin : q.xMin, hi = horizontal ? q.yMax : q.xMax;
                float a = Mathf.Max(from, qa), b = Mathf.Min(to, qb);
                if (b - a <= Eps) continue;
                // Another solid continues past the edge. 2026-10-16: a trap doesn't cover a fixed solid's face (it may fall, slide or
                // shrink away and leave that face bare): the face keeps its broken trims under the trap's skin.
                // And a trap's underside resting on fixed stone stays exposed: its hanging stones draw over the floor's lip (a flat
                // trap bottom on a lit lip was a straight line) and leave with it.
                bool beyond = (s.Trap ? !(side == Side.Bottom && !o.Trap) : !o.Trap) && (outward > 0 ? lo <= line + Eps && hi > line + Eps : hi >= line - Eps && lo < line - Eps);
                // A trap reaching this fixed edge from inside owns the stretch.
                bool trapOwns = !s.Trap && o.Trap && (outward > 0 ? lo < line - Eps && hi >= line - Eps : hi > line + Eps && lo <= line + Eps);
                if (beyond || trapOwns) covered.Add((a, b));
            }
            float cursor = from;
            foreach ((float a, float b) in covered.OrderBy(c => c.Item1))
            {
                if (a - cursor > Eps) edges.Add(new Edge(s, side, cursor, a, line));
                cursor = Mathf.Max(cursor, b);
            }
            if (to - cursor > Eps) edges.Add(new Edge(s, side, cursor, to, line));
        }

        /// <summary>The walkable tops (A1, A2): exposed tops of floors, slabs and disguised floors, not pit bottoms.</summary>
        public static List<Edge> WalkableTops(SoloRoomDefinition room) =>
            ExposedEdges(Solids(room)).Where(e => e.Side == Side.Top && e.Owner.Shape != Shape.PitBottom && e.Length >= 0.4f).ToList();

        // ---------- fill ----------

        public static Sprite FillSprite(SoloRoomDefinition room) => LevelLooks.For(LevelLooks.LevelOf(room)).Stone switch
        {
            LevelLooks.Fill.A2 => EnvironmentKit.Sprite("ENV_Fill_A2"),
            LevelLooks.Fill.A3 => EnvironmentKit.Sprite("ENV_Fill_A3"),
            _ => EnvironmentKit.Sprite("ENV_Fill_A"),
        };

        /// <summary>Gives every fixed solid the room's stone. Its own renderer becomes the host the trap art copies (A13's
        /// world-tiled shader, sampled where it stands, so a disguised trap's pixels travel with it) and is hidden; a "Fill"
        /// child draws the same pixels with the world-UV material, which batches (approved 2026-10-01).</summary>
        public static void SkinFill(Transform roomRoot, RealityRoot root, SoloRoomDefinition room, List<string> changes)
        {
            Material worldTile = WorldTileMaterial(), fillMaterial = EnvironmentKit.TileMaterial("Fill");
            Sprite fill = FillSprite(room);
            if (worldTile == null || fill == null || fillMaterial == null) return;
            string layer = RealitySpace.SortingLayerName(root.Id, SortingBand.Gameplay);
            Color body = Body;
            HostSkin skin = HostSkin.WorldTiledFor("fill", fill, worldTile, body, layer);
            // PAX-106 (D-114 (2)), PAX-A18 W3: with the broken-stone strips, the fill stops inside each wholly exposed side (BandReach
            // along band strips, DeepReach along deep ones), so the chipped edge's notches show the air behind it.
            List<Solid> all = Solids(room);
            bool chips = ChipsReady(room);
            Bounds content = SoloRoomBuilder.ComputeRoomBounds(room, 0f);
            List<Edge> allEdges = ExposedEdges(all);
            List<Edge> sides = chips ? allEdges.Where(e => e.Side is Side.Left or Side.Right && !FacesOut(e, content, room)).ToList() : new List<Edge>();
            // PAX-A18 W3: the inset follows each side's strips (BandReach along band strips, DeepReach along deep ones).
            List<Rect> zones = chips ? ReachZones(room, all) : new List<Rect>();
            bool flips = room.Elements.Any(x => x.Kind == SoloRoomElementKind.GravityFlip);
            List<Rect> hazards = HazardZones(room);
            foreach (Solid s in all.Where(s => !s.Trap))
            {
                Transform t = roomRoot.Find(s.Name);
                SpriteRenderer r = t != null ? t.GetComponent<SpriteRenderer>() : null;
                if (r == null) continue;
                if (r.sprite != fill || r.sharedMaterial != worldTile || r.color != body) changes.Add("skinned " + s.Name);
                skin.ApplyWorldTiled(r, s.Rect.size, r.transform.position);
                r.enabled = false;
                // Slabs, slim posts and posts are covered edge to edge by their trims: no visible fill (one renderer less).
                Transform old = t.Find(FillName);
                if (s.Shape is Shape.Slab or Shape.SlimPost or Shape.Post) { if (old != null) Object.DestroyImmediate(old.gameObject); RemoveFillPieces(t, 1); continue; }
                // PAX-107 (the developer: "Falling platforms or ceiling need to show that it actually fell, leaving it black
                // looks weird. It should show the background"): the fill leaves out a falling block flush in a ceiling, whose own
                // skin draws the same world-tiled pixels there; once it falls, the air behind shows through.
                List<(Rect piece, float left, float right)> pieces = HoleInsets(InsetPieces(s, FillPieces(s, room), sides, zones, allEdges, flips, hazards), Holes(s, room));
                for (int i = 0; i < pieces.Count; i++)
                {
                    (Rect piece, float left, float right) = pieces[i];
                    Transform child = Child(t, i == 0 ? FillName : FillName + "_" + i, t.gameObject.layer);
                    child.localRotation = Quaternion.identity; child.localScale = Vector3.one;
                    SpriteRenderer visible = SetupUtility.Ensure<SpriteRenderer>(child.gameObject, changes);
                    visible.sprite = fill; visible.sharedMaterial = fillMaterial; visible.color = body;
                    Vector2 centre = piece.center + new Vector2((left - right) * 0.5f, 0f) - s.Rect.center;
                    child.localPosition = new Vector3(centre.x, centre.y, ZFill);
                    visible.drawMode = SpriteDrawMode.Sliced; visible.size = new Vector2(piece.width - left - right, piece.height);
                    visible.sortingLayerName = layer; visible.sortingOrder = r.sortingOrder;
                }
                RemoveFillPieces(t, Mathf.Max(1, pieces.Count));
            }
        }

        // PAX-107: a fixed solid's fill, less every falling block flush inside it (LeavesHole): strips left and right of each
        // block at its height, and above and below it at full width.
        static List<Rect> FillPieces(Solid s, SoloRoomDefinition room) => Cut(s.Rect, Holes(s, room));

        // 2026-10-17 (the developer: "when things fall from the ceiling everything is straight"): a fill piece beside a hole stops
        // DeepReach short of it, behind the hole's broken rim (HoleRims); the falling block lends its stone over that while it's
        // there (TrapReach).
        static List<(Rect piece, float left, float right)> HoleInsets(List<(Rect piece, float left, float right)> pieces, List<Rect> holes)
        {
            if (holes.Count == 0) return pieces;
            return pieces.Select(p =>
            {
                bool Beside(Rect h, float x) => Mathf.Abs(x) < Eps && p.piece.yMin < h.yMax - Eps && p.piece.yMax > h.yMin + Eps;
                float left = holes.Any(h => Beside(h, p.piece.xMin - h.xMax)) ? Mathf.Max(p.left, DeepReach) : p.left;
                float right = holes.Any(h => Beside(h, p.piece.xMax - h.xMin)) ? Mathf.Max(p.right, DeepReach) : p.right;
                return (p.piece, left, right);
            }).ToList();
        }

        // PAX-107: the falling blocks flush inside this fixed solid that leave a hole (LeavesHole).
        internal static List<Rect> Holes(Solid s, SoloRoomDefinition room)
        {
            var holes = new List<Rect>();
            if (s.Trap) return holes;
            foreach (SoloRoomElement e in room.Elements)
            {
                var hole = new Rect(room.Origin + e.Position - e.Size * 0.5f, e.Size);
                if (LeavesHole(room, e) && Contains(s.Rect, hole)) holes.Add(hole);
            }
            return holes;
        }

        // `r` less every hole: strips left and right of each at its height, and above and below it at r's full width.
        static List<Rect> Cut(Rect r, List<Rect> holes)
        {
            var pieces = new List<Rect> { r };
            foreach (Rect hole in holes)
            {
                var next = new List<Rect>();
                foreach (Rect p in pieces)
                {
                    if (!p.Overlaps(hole)) { next.Add(p); continue; }
                    // 2026-10-18 visual review (L012: "square holes through the walkway slab" over Drop_3's corners): the strips
                    // beside it at its height only, so their inset behind its broken rim (HoleInsets) stops where it does; above
                    // and below it at the piece's whole width.
                    float x0 = Mathf.Max(p.xMin, hole.xMin), x1 = Mathf.Min(p.xMax, hole.xMax);
                    float y0 = Mathf.Max(p.yMin, hole.yMin), y1 = Mathf.Min(p.yMax, hole.yMax);
                    if (x0 - p.xMin > Eps) next.Add(Rect.MinMaxRect(p.xMin, y0, x0, y1));
                    if (p.xMax - x1 > Eps) next.Add(Rect.MinMaxRect(x1, y0, p.xMax, y1));
                    if (y0 - p.yMin > Eps) next.Add(Rect.MinMaxRect(p.xMin, p.yMin, p.xMax, y0));
                    if (p.yMax - y1 > Eps) next.Add(Rect.MinMaxRect(p.xMin, y1, p.xMax, p.yMax));
                }
                pieces = next;
            }
            return pieces;
        }

        /// <summary>PAX-102/PAX-107 (the developer: "Falling platforms or ceiling need to show that it actually fell … It should
        /// show the background"): a falling block inside a ceiling, or in a floor slab's underside (flush with its bottom, short
        /// of its top), leaves a hole in that solid's art once it has gone, in a room with no gravity flip (no cat stands on an
        /// underside there). Never one at a floor's top: the floor's collider stays, so a drawn hole there would lie; never one
        /// in a thin slab (D-131: its stone is one strip): the slab is split around the block instead.</summary>
        public static bool LeavesHole(SoloRoomDefinition room, SoloRoomElement block)
        {
            if (block.Kind != SoloRoomElementKind.FallingBlock || room.Elements.Any(x => x.Kind == SoloRoomElementKind.GravityFlip)) return false;
            var box = new Rect(room.Origin + block.Position - block.Size * 0.5f, block.Size);
            foreach (SoloRoomElement x in room.Elements)
            {
                if (x.Kind is not (SoloRoomElementKind.Floor or SoloRoomElementKind.Ceiling or SoloRoomElementKind.Wall)) continue;
                var host = new Rect(room.Origin + x.Position - x.Size * 0.5f, x.Size);
                if (!Contains(host, box)) continue;
                if (x.Kind == SoloRoomElementKind.Ceiling) return true;
                // D-131: never in a thin slab (drawn as one stone strip, ENV_Slab, which isn't cut): split the slab instead.
                if (host.height <= 0.55f && host.width > host.height) return false;
                return Mathf.Abs(box.yMin - host.yMin) < Eps && host.yMax - box.yMax > Eps;
            }
            return false;
        }

        static bool Contains(Rect outer, Rect inner) =>
            inner.xMin >= outer.xMin - Eps && inner.xMax <= outer.xMax + Eps && inner.yMin >= outer.yMin - Eps && inner.yMax <= outer.yMax + Eps;

        static void RemoveFillPieces(Transform t, int keep)
        {
            for (int i = keep; ; i++)
            {
                Transform extra = t.Find(FillName + "_" + i);
                if (extra == null) return;
                Object.DestroyImmediate(extra.gameObject);
            }
        }

        /// <summary>PAX-A16 §3.1: the stone body's tint (dark, so the lit lip and the cat read against the air).</summary>
        public static Color Body
        {
            get
            {
                var config = AssetDatabase.LoadAssetAtPath<LevelLookConfig>(LevelLookConfig.AssetPath);
                return config != null ? config.BodyTint : Color.white;
            }
        }

        /// <summary>A block tall enough for the lit lip's wash (ENV_CapWash) under its cap.</summary>
        public const float WashMinHeight = 1.6f;
        /// <summary>PAX-A16 round 3 (the developer: "consistency"): every block's face, real or disguised (P10), falls into the
        /// same warm shadow below its lit lip: clear for this far under its top, then darkening over the ramp to the solid
        /// shade. One rule for every block, so no floor looks different from its neighbour.</summary>
        public const float ShadeStart = 1.1f, ShadeRamp = 4.5f;
        /// <summary>env_bumpy.THICK_END_FADE: the thick underside's end fades into the strip over this.</summary>
        const float ThickEndFade = 0.15f;
        /// <summary>2026-10-17: the room's StoneShade map: texels per unit; how far it reaches past the room's box; how far the face
        /// shadow carries out over the air beside a face (its broken strip's stones); how far it's softened across a change of
        /// column; and the width over which the outside's shade fades in across the room's box.</summary>
        const int ShadePpu = 4;
        const float ShadeMargin = 6f, ShadeDilate = 0.5f, ShadeSoften = 0.5f, OutsideFade = 1.2f;

        /// <summary>A horizontal box blur of radius r texels on a w × h map (edges clamped).</summary>
        static float[] BoxBlurRows(float[] src, int w, int h, int r)
        {
            if (r <= 0) return src;
            var dst = new float[src.Length];
            for (int j = 0; j < h; j++)
                for (int i = 0; i < w; i++)
                {
                    float sum = 0f;
                    for (int d = -r; d <= r; d++) sum += src[j * w + Mathf.Clamp(i + d, 0, w - 1)];
                    dst[j * w + i] = sum / (2 * r + 1);
                }
            return dst;
        }
        /// <summary>How far a thick underside's ragged end reaches into a solid it runs into (past that solid's deepest notch).</summary>
        const float ThickEndInto = 0.32f;

        /// <summary>The face shadow's colour at LevelLookConfig.FaceShadeAlpha.
        /// 2026-10-17: drawn by the stone's shaders from the room's StoneShade map, no longer by quads.</summary>
        static Color LevelShade
        {
            get
            {
                var config = AssetDatabase.LoadAssetAtPath<LevelLookConfig>(LevelLookConfig.AssetPath);
                // 2026-10-17: the outside's neutral dark, no longer the sprite's navy (round 5, critics: "a navy/purple translucent
                // overlay on the walls"; with the shadow now on every strip too, the navy turned whole undersides blue).
                return new Color(ShadeNavy.r * 0.9f, ShadeNavy.g * 0.66f, ShadeNavy.b * 0.37f, config != null ? config.FaceShadeAlpha : 0.8f);
            }
        }

        /// <summary>The outside masonry's shade: the navy turned a neutral dark (round 5, critics: "a navy/purple translucent
        /// overlay on the walls"), a quarter stronger than a face's.</summary>
        static Color OutsideShade
        {
            get
            {
                var config = AssetDatabase.LoadAssetAtPath<LevelLookConfig>(LevelLookConfig.AssetPath);
                return new Color(ShadeNavy.r * 0.9f, ShadeNavy.g * 0.66f, ShadeNavy.b * 0.37f, Mathf.Min(1f, (config != null ? config.FaceShadeAlpha : 0.8f) * 1.25f));
            }
        }

        /// <summary>ENV_FaceShadeSolid's colour.</summary>
        static readonly Color ShadeNavy = new(0.102f, 0.122f, 0.22f, 1f);

        static Material WorldTileMaterial()
        {
            var config = AssetDatabase.LoadAssetAtPath<TrapArtConfig>(TrapArtSetup.ConfigPath);
            return config != null ? config.WorldTileMaterial : null;
        }

        // ---------- trims and dressing ----------

        public static void Remove(Transform roomRoot, List<string> changes)
        {
            Transform env = roomRoot.Find(EnvRootName);
            if (env != null) { Object.DestroyImmediate(env.gameObject); changes.Add("removed " + roomRoot.name + "/" + EnvRootName); }
            Transform artRoot = roomRoot.Find(TrapArtSetup.ArtRootName);
            if (artRoot == null) return;
            foreach (Transform art in artRoot)
                foreach (string skinName in new[] { "Skin", "Launcher" })
                {
                    Transform trims = art.Find(skinName)?.Find(TrimsName);
                    if (trims == null) continue;
                    Object.DestroyImmediate(trims.gameObject);
                    changes.Add("removed " + art.name + " trims");
                }
        }

        /// <summary>Builds the trims and dressing from scratch (the room build is a regeneration: rebuilding leaves the
        /// same objects, in the same order).</summary>
        public static void BuildTrims(Transform roomRoot, RealityRoot root, SoloRoomDefinition room, List<string> changes)
        {
            Material worldTile = WorldTileMaterial();
            if (worldTile == null) return;
            Remove(roomRoot, new List<string>());
            int layer = root.gameObject.layer;
            string sortingLayer = RealitySpace.SortingLayerName(root.Id, SortingBand.Gameplay);
            Transform env = Child(roomRoot, EnvRootName, layer);
            Transform artRoot = roomRoot.Find(TrapArtSetup.ArtRootName);
            var hosts = new Dictionary<string, (Transform parent, SpriteRenderer owner, int order)>();
            var trapTrims = new Dictionary<string, List<SpriteRenderer>>();

            (Transform parent, SpriteRenderer owner, int order) Host(Solid s)
            {
                if (hosts.TryGetValue(s.Name, out var h)) return h;
                if (!s.Trap) h = (Child(env, s.Name, layer), null, -2);
                else
                {
                    Transform art = artRoot != null ? artRoot.Find(s.Name) : null;
                    SpriteRenderer skin = art == null ? null : (art.Find("Skin") ?? art.Find("Launcher"))?.GetComponent<SpriteRenderer>();
                    Transform old = skin != null ? skin.transform.Find(TrimsName) : null;
                    if (old != null) Object.DestroyImmediate(old.gameObject);
                    h = skin == null ? (null, null, 0) : (Child(skin.transform, TrimsName, layer), skin, skin.sortingOrder);
                }
                hosts[s.Name] = h;
                return h;
            }

            SpriteRenderer Add(Solid s, string name, string slot, Vector2 centre, Vector2 size, Vector2 tile, Vector4 rest, bool flip, float z, bool worldTiled, string kind = null)
            {
                var (parent, owner, order) = Host(s);
                if (parent == null) return null;
                Sprite sprite = EnvironmentKit.Sprite(slot);
                if (sprite == null) return null;
                Transform t = Child(parent, name, layer);
                // PAX-A16: a trap's trims sit a step nearer than the fixed ones at the same order, so the renderer sorts all the
                // fixed trims (one shader) before the traps' (SetPass); they still draw over their own skin.
                // 2026-10-16 (budget): trims of one slot share a depth a hair apart from other slots' (under 0.0004, well inside
                // the gaps between trim layers), so the renderer sorts same-sprite trims together and batches them; at one
                // shared depth hundreds of trims sorted arbitrarily and broke batches.
                float slotZ = -0.00005f * (int)(Hash(slot, 77) * 8f);
                t.position = new Vector3(centre.x, centre.y, parent.position.z + z + slotZ + (owner != null ? TrapTrimZ : 0f));
                t.localRotation = Quaternion.identity;
                var r = t.gameObject.AddComponent<SpriteRenderer>();
                r.sprite = sprite;
                r.sortingLayerName = sortingLayer;
                r.sortingOrder = order;
                r.flipX = flip;
                if (worldTiled)
                {
                    r.drawMode = SpriteDrawMode.Sliced;
                    r.size = size;
                    t.localScale = Vector3.one;
                    // Trims batch on their kind's world-UV material; only a trap that moves (a falling block, a mover, a
                    // shrinking floor) keeps the rest-pose shader, so its trims' pixels travel with it (both sample the same
                    // pixels at rest).
                    bool travels = s.Kind is SoloRoomElementKind.FallingBlock or SoloRoomElementKind.MovingTrap or SoloRoomElementKind.ShrinkingFloor;
                    if (owner == null || !travels) r.sharedMaterial = EnvironmentKit.TileMaterial(kind);
                    // A moving trap's vertical trims (its side faces, a post's shaft; L001's falling grip walls, D-111): the sprite
                    // tiled down the face in its own UV, so its pixels travel with it. The rest-pose shader's u comes from the
                    // vertex, which batching puts in world space: on a texture that clamps across (ENV_Side, ENV_Post), every row
                    // smeared its edge pixel.
                    else if (kind is "Side" or "SideChip" or "SideIn" or "SideDeep" or "Post" or "SlimPost" && EnvironmentKit.TileMaterial("Sprite") is Material plainSprite)
                    {
                        r.sharedMaterial = plainSprite;
                        r.drawMode = SpriteDrawMode.Tiled;
                        r.size = size;
                        t.localScale = Vector3.one;
                    }
                    else
                    {
                        r.sharedMaterial = worldTile;
                        t.gameObject.AddComponent<WorldTileSampling>().Set(tile, rest, new Vector4(0f, 0f, 1f, 1f));
                    }
                }
                else
                {
                    r.drawMode = SpriteDrawMode.Simple;
                    // PAX-A16: plain sprites batch with the trims (one shader, sprite UV).
                    if (EnvironmentKit.TileMaterial("Sprite") is Material plain) r.sharedMaterial = plain;
                    Vector2 native = sprite.bounds.size;
                    t.localScale = new Vector3(size.x / native.x, size.y / native.y, 1f);
                }
                if (owner != null)
                {
                    if (!trapTrims.TryGetValue(s.Name, out var list)) trapTrims[s.Name] = list = new List<SpriteRenderer>();
                    list.Add(r);
                }
                return r;
            }

            List<Solid> solids = Solids(room);
            // The ground's line: the lowest walkable top that isn't a pit floor (FaceShade joins steps to it).
            float groundLine = WalkableTops(room).Select(t => t.Line).DefaultIfEmpty(float.MinValue).Min();
            bool flips = room.Elements.Any(x => x.Kind == SoloRoomElementKind.GravityFlip);
            List<Rect> visualKeepOut = VisualKeepOut(room);
            Color body = Body, face = body * 1.12f, postTint = body * 1.3f, slabTint = Color.white;   // gauntlet: the slab's own lit stone
            face.a = postTint.a = 1f;
            // Faces looking out of the room (the side walls' outer faces, the ground's underside, the top ceiling's top) border
            // no play space: no trims there (each would be a draw call at the frame's edge).
            Bounds content = SoloRoomBuilder.ComputeRoomBounds(room, 0f);
            List<Edge> allEdges = ExposedEdges(solids);
            List<Edge> edges = allEdges.Where(e => !FacesOut(e, content, room)).ToList();
            List<Solid> fixedSolids = solids.Where(o => !o.Trap).ToList();
            // 2026-10-17 visual review (L001: "each top is a ruler-straight, flat, dark, featureless band ... no lit cap"): a block
            // that falls from inside the stone over it has a top no one sees until it falls; it gets its lit course anyway,
            // shown once it has left (ShownAtRest, whenMoved).
            var fallenTops = new HashSet<string>();
            foreach (Solid s in solids.Where(o => o.Trap && o.Kind == SoloRoomElementKind.FallingBlock))
                if (!edges.Any(e => e.Owner.Name == s.Name && e.Side == Side.Top)
                    && solids.Any(o => !o.Trap && Mathf.Abs(o.Rect.yMin - s.Rect.yMax) < Eps && o.Rect.xMin <= s.Rect.xMin + Eps && o.Rect.xMax >= s.Rect.xMax - Eps))
                {
                    edges.Add(new Edge(s, Side.Top, s.Rect.xMin, s.Rect.xMax, s.Rect.yMax));
                    fallenTops.Add(s.Name);
                }
            // PAX-A18 W3: where a cat can be (band strips there; deep strips elsewhere).
            List<Rect> zones = ReachZones(room, solids), hazards = HazardZones(room);
            EnvironmentKit.Slot cap = EnvironmentKit.Get("ENV_Cap"), capWash = EnvironmentKit.Get("ENV_CapWash"), lipVines = EnvironmentKit.Get("ENV_LipVines"), faceShade = EnvironmentKit.Get("ENV_FaceShade"), shadeSolid = EnvironmentKit.Get("ENV_FaceShadeSolid"), thickUnder = ThickUnderSlot(), under = UnderSlot(flips), side = EnvironmentKit.Get("ENV_Side");
            EnvironmentKit.Slot slab = EnvironmentKit.Get("ENV_Slab"), slabEnd = EnvironmentKit.Get("ENV_SlabEnd"), water = EnvironmentKit.Get("ENV_Water");
            EnvironmentKit.Slot post = EnvironmentKit.Get("ENV_Post"), slim = EnvironmentKit.Get("ENV_SlimPost"), postCap = EnvironmentKit.Get("ENV_PostCap"), postBase = EnvironmentKit.Get("ENV_PostBase");

            foreach (Solid s in solids)
            {
                Rect r = s.Rect;
                List<Edge> mine = edges.Where(e => e.Owner.Name == s.Name).ToList();
                if (s.Trap) TrapReach(s);
                switch (s.Shape)
                {
                    case Shape.PitBottom:
                        // Phase 2 round 2 (the developer: "water only goes where nothing walkable sits on it"): a pit's floor is
                        // trimmed like any block, no water (the spikes and walls in pits read as standing in it).
                        foreach (Edge e in mine) BlockEdge(s, e);
                        break;
                    case Shape.Slab:
                    {
                        float k = r.height / 0.5f, h = slab.height * k, above = slab.above * k;
                        float cy = r.yMax + above - h * 0.5f;
                        // PAX-A18 W3: a hanging, broken underside where no cat stands on it; the flat one in a flip room.
                        // 2026-10-18 visual review (L020: "its wavy, light underside rim and an evenly spaced row of moss drips run
                        // horizontally through the middle of the solid masonry"): a slab lying on stone (no underside exposed) takes the
                        // flat one too; its hanging underside drew across the wall under it.
                        bool slabCovered = !mine.Any(e => e.Side == Side.Bottom);
                        string slabSlot = (flips || slabCovered) && EnvironmentKit.Get("ENV_SlabFlip") != null ? "ENV_SlabFlip" : "ENV_Slab";
                        // PAX-A18 W3: broken ends within BandReach of the face (their `outside`); the right one is the thick end
                        // (a chunk hanging under it) where no cat stands under the slab. 2026-10-16: on every slab from 0.5 u (shorter
                        // ones were hard-cut rectangles), and the slab's strip stops where each end's stone begins, so the end's notches
                        // show the air, not the strip's straight end behind them.
                        // On a trap the ends stand BandReach further out: its skin is a whole rectangle under them, which inward notches
                        // would show.
                        float endW = Mathf.Min(slabEnd.width * k, r.width * 0.5f), endOut = slabEnd.outside * k + (s.Trap ? BandReach : 0f);
                        bool broken = r.width >= 0.5f;
                        bool leftEnd = broken && mine.Any(e => e.Side == Side.Left && e.Length >= r.height - Eps);
                        bool rightEnd = broken && mine.Any(e => e.Side == Side.Right && e.Length >= r.height - Eps);
                        float sx0 = r.xMin + (leftEnd ? endW - endOut - 0.02f : 0f), sx1 = r.xMax - (rightEnd ? endW - endOut - 0.02f : 0f);
                        if (sx1 > sx0 + 0.01f)
                            Add(s, "Slab", slabSlot, new Vector2((sx0 + sx1) * 0.5f, cy), new Vector2(sx1 - sx0, h), new Vector2(slab.width * k, h),
                                new Vector4((sx0 + sx1) * 0.5f, h * 0.5f, 1f, 1f), false, ZCap, true, "Slab");
                        foreach (Edge bottom in mine.Where(t => t.Side == Side.Bottom)) Thick(s, bottom);
                        if (leftEnd)
                            Tint(Add(s, "SlabEnd_L", "ENV_SlabEnd", new Vector2(r.xMin + endW * 0.5f - endOut, cy), new Vector2(endW, h), Vector2.zero, Vector4.zero, false, ZCap - 0.001f, false), slabTint);
                        if (rightEnd)
                        {
                            EnvironmentKit.Slot thickEnd = flips ? null : EnvironmentKit.Get("ENV_SlabEndThick");
                            EnvironmentKit.Slot rightSlot = thickEnd ?? slabEnd;
                            float hr = rightSlot.height * k, cyr = r.yMax + rightSlot.above * k - hr * 0.5f;
                            SpriteRenderer end = Add(s, "SlabEnd_R", rightSlot.name, new Vector2(r.xMax - endW * 0.5f + endOut, cyr), new Vector2(endW, hr), Vector2.zero, Vector4.zero, true, ZCap - 0.001f, false);
                            if (end != null) { end.flipX = true; end.color = slabTint; }
                        }
                        break;
                    }
                    case Shape.SlimPost:
                        AddVertical(s, "Shaft", "ENV_SlimPost", slim, r, r.width / 0.5f, "SlimPost");
                        break;
                    case Shape.Post:
                        AddVertical(s, "Shaft", "ENV_Post", post, r, r.width, "Post");
                        if (mine.Any(e => e.Side == Side.Top)) AddObject(s, "Capital", "ENV_PostCap", postCap, new Vector2(r.center.x, r.yMax), r.width * 1.3f, top: true);
                        if (!mine.Any(e => e.Side == Side.Bottom)) AddObject(s, "Base", "ENV_PostBase", postBase, new Vector2(r.center.x, r.yMin), r.width * 1.15f, top: false);
                        break;
                    default:
                        foreach (Edge e in mine) BlockEdge(s, e);
                        break;
                }
                if (!s.Trap) HoleRims(s);
            }

            // 2026-10-17 (the developer: "when things fall from the ceiling everything is straight"): the hole a falling block
            // leaves (LeavesHole) has broken sides and a ragged top: deep strips stand out from the host's stone at each of the
            // hole's sides and hanging stones from its top, all inside the hole and behind the block (a hair farther than its
            // skin, at the same order), so they show only once it has fallen.
            void HoleRims(Solid s)
            {
                Sprite fill = FillSprite(room);
                if (fill == null || !fill.name.StartsWith("ENV_Fill_") || !ChipsReady(room)) return;
                string suffix = fill.name.Substring("ENV_Fill_".Length);
                int k = 0;
                foreach (Rect hole in Holes(s, room))
                {
                    foreach (bool atLeft in new[] { true, false })
                    {
                        // The host's stone beside the hole (its face looks into it).
                        float line = atLeft ? hole.xMin : hole.xMax;
                        if (atLeft ? line <= s.Rect.xMin + Eps : line >= s.Rect.xMax - Eps) continue;
                        string slot = $"ENV_SideDeep_{suffix}_{(int)(Hash(s.Name + k, 640) * 4f)}";
                        EnvironmentKit.Slot def = EnvironmentKit.Get(slot);
                        if (def == null) continue;
                        var holeSide = new Edge(s, atLeft ? Side.Right : Side.Left, hole.yMin, hole.yMax, line);
                        Rect strip = SideStrip(holeSide, def);
                        Tint(Add(s, (atLeft ? "HoleL_" : "HoleR_") + k, slot, strip.center, strip.size, new Vector2(def.width, def.height),
                            new Vector4(def.width * 0.5f, hole.center.y, 1f, 1f), atLeft, HoleRimZ, true, EdgeKindOf(room, "SideDeep")), body);
                    }
                    // Its top: stones hanging from the stone over it.
                    if (under != null)
                        Tint(Add(s, "HoleTop_" + k, under.name, new Vector2(hole.center.x, hole.yMax - under.below + under.height * 0.5f), new Vector2(hole.width, under.height),
                            new Vector2(under.width, under.height), new Vector4(hole.center.x, under.height * 0.5f, 1f, 1f), false, HoleRimZ, true, "Under"), face);
                    k++;
                }
            }

            void BlockEdge(Solid s, Edge e)
            {
                float mid = (e.From + e.To) * 0.5f;
                switch (e.Side)
                {
                    case Side.Top:
                    {
                        // PAX-A16: a block tall enough takes the lit lip's wash under its cap (the same rule for traps, P10).
                        // Gauntlet: the plain cap everywhere (the wash read as a flat sepia overlay); the 2D lights light the lip.
                        EnvironmentKit.Slot top = cap;
                        string kind = top == capWash ? "CapWash" : "Cap";
                        // PAX-A18 W3: a broken corner end at each exposed corner (within BandReach of the face); the cap stops
                        // where the end's stone begins.
                        float from = e.From, to = e.To;
                        foreach (bool atLeft in new[] { true, false })
                        {
                            string endSlot = CornerExposed(e, atLeft, allEdges) ? CapEndSlot(e, atLeft) : null;
                            EnvironmentKit.Slot end = endSlot != null ? EnvironmentKit.Get(endSlot) : null;
                            if (end == null || e.Length < 2f * (end.width - end.outside) + 0.1f) continue;
                            // (on a trap, BandReach further out: its whole skin under the corner would show through inward notches)
                            float outside = end.outside + (s.Trap ? BandReach : 0f);
                            float inner = end.width - outside, x = atLeft ? e.From - outside + end.width * 0.5f : e.To + outside - end.width * 0.5f;
                            Add(s, (atLeft ? "CapEndL_" : "CapEndR_") + Idx(e), endSlot, new Vector2(x, e.Line + end.above - end.height * 0.5f), new Vector2(end.width, end.height),
                                Vector2.zero, Vector4.zero, !atLeft, ZCap, false);
                            if (atLeft) from += inner - 0.02f; else to -= inner - 0.02f;
                        }
                        // PAX-A18 W3: flat (ENV_CapFlat) only over a hazard's own width (its spikes need plain air behind them, D-108);
                        // bumpy everywhere else.
                        foreach ((float a, float b, bool flat) in CapRuns(from, to, e.Line, top == cap && EnvironmentKit.Get("ENV_CapFlat") != null ? hazards : null))
                        {
                            float capMid = (a + b) * 0.5f;
                            Add(s, "Cap_" + Idx(e) + (a > from + Eps ? "_" + Mathf.RoundToInt(a * 100f) : ""), flat ? "ENV_CapFlat" : top.name, new Vector2(capMid, e.Line + top.above - top.height * 0.5f), new Vector2(b - a, top.height),
                                new Vector2(top.width, top.height), new Vector4(capMid, top.height * 0.5f, 1f, 1f), false, ZCap, true, kind);
                        }
                        break;
                    }
                    case Side.Bottom:
                        if (Thick(s, e)) break;
                        // 2026-10-17 visual review ("a dead-straight horizontal seam through the big slab ... as if it were a lip"):
                        // an underside with stone just under it (closer than ShallowOverhang) is a joint in the wall, not a ledge;
                        // no hanging underside there (its drapes already need 2.2 u of air, ClearBelow).
                        if (ClearBelow(e, fixedSolids) < ShallowOverhang) break;   // (fixed stone: over a trap it shows once the trap goes)
                        Tint(Add(s, "Under_" + Idx(e), under.name, new Vector2(mid, e.Line - under.below + under.height * 0.5f), new Vector2(e.Length, under.height),
                            new Vector2(under.width, under.height), new Vector4(mid, under.height * 0.5f, 1f, 1f), false, ZUnder, true, under.name == "ENV_UnderWalk" ? "UnderWalk" : "Under"), face);
                        break;
                    default:
                    {
                        // PAX-106 (D-114): broken stone, a chipped strip cut from the level's fill (ENV_Side before the kit had them).
                        bool left = e.Side == Side.Left;
                        // PAX-A18 W3: band strips where a cat can be, deep strips where nothing reaches (FaceSpans).
                        List<FaceSpan> spans = ChipSlot(room, e) != null ? FaceSpans(e, zones, allEdges, flips, hazards) : new List<FaceSpan> { new(e.From, e.To, false) };
                        // PAX-A18 W3: under a broken corner end the end draws the face's top itself.
                        float stripTop = StripTop(e, allEdges);
                        spans = spans.Where(p => p.From < stripTop - Eps).Select(p => new FaceSpan(p.From, Mathf.Min(p.To, stripTop), p.Free, p.Inward)).ToList();
                        for (int i = 0; i < spans.Count; i++)
                        {
                            string chip = SpanSlot(room, e, spans[i]);
                            // A moving trap's trims are plain tiled sprites (their pixels travel with it), so they can't be masks on
                            // the world's fill: D-114's own strip there.
                            // 2026-10-16: every trap solid (its body is a whole rectangle, so an inward notch would show its straight edge).
                            if (chip != null && e.Owner.Trap
                                && EnvironmentKit.Get(chip.Replace("ENV_SideChip_", "ENV_SideIn_").Replace("ENV_SideDeep_", "ENV_SideIn_")) != null)
                                chip = chip.Replace("ENV_SideChip_", "ENV_SideIn_").Replace("ENV_SideDeep_", "ENV_SideIn_");
                            EnvironmentKit.Slot def = chip != null ? EnvironmentKit.Get(chip) : side;
                            string stripKind = chip == null ? "Side" : chip.StartsWith("ENV_SideDeep_") ? EdgeKindOf(room, "SideDeep") : chip.StartsWith("ENV_SideIn_") ? "SideIn" : EdgeKindOf(room, "SideChip");
                            // 2026-10-17: the face's shadow comes from the room's StoneShade map, in the stone's shaders, so the strip
                            // is one piece (it was cut and tinted down the shadow's ramp, never quite the colour of the stone beside it).
                            var chunks = new List<(float a, float b)> { (spans[i].From, spans[i].To) };
                            for (int j = 0; j < chunks.Count; j++)
                            {
                                var part = new Edge(e.Owner, e.Side, chunks[j].a, chunks[j].b, e.Line);
                                Rect strip = SideStrip(part, def);
                                // 2026-10-16: never past the solid's other face (on a thin wall the strip's soft inner end stood out
                                // over the air as a straight edge); the mask squeezes a little instead.
                                // (on a thin one, at its middle: past it the strips' soft ends would cross over air in the other's notches)
                                float limit = s.Rect.width < ThinWidth ? s.Rect.center.x + (left ? 0.04f : -0.04f) : (left ? s.Rect.xMax : s.Rect.xMin);
                                if (left && strip.xMax > limit) strip.xMax = Mathf.Max(limit, strip.xMin + 0.05f);
                                if (!left && strip.xMin < limit) strip.xMin = Mathf.Min(limit, strip.xMax - 0.05f);
                                Tint(Add(s, (left ? "SideL_" : "SideR_") + Idx(e) + (i == 0 ? "" : "_" + i) + (j == 0 ? "" : "c" + j), chip ?? "ENV_Side", strip.center, strip.size,
                                    new Vector2(def.width, def.height), new Vector4(def.width * 0.5f, (part.From + part.To) * 0.5f, 1f, 1f), !left, ZSide, true, stripKind),
                                    chip != null ? body : face);
                            }
                        }
                        break;
                    }
                }
            }

            // PAX-A16 round 2: a thick stone underside with its cornice and long ivy, where ThickUnderFits says it may hang.
            bool Thick(Solid s, Edge e)
            {
                if (thickUnder == null || !ThickUnderFits(e, solids, flips, visualKeepOut)) return false;
                float h = thickUnder.height, from = e.From, to = e.To, cy = e.Line + 0.04f - h * 0.5f;
                bool shortened = thickUnder.name == "ENV_ThickUnderShort";
                // 2026-10-16 ("All edges need to be a bit bumpy"): a broken end where the solid's side is open at its foot (the
                // strip stopped in a straight cut through its stone and ivy); the strip runs on under the end's soft inner side.
                EnvironmentKit.Slot end = EnvironmentKit.Get(thickUnder.name + "End");
                foreach (bool atLeft in new[] { true, false })
                {
                    if (end == null || e.Length < 2f * (end.width - end.outside) + 0.1f) continue;
                    // A ragged end where the underside stops: nothing beside it, or a solid going on down past its line (a wall it
                    // runs into: the end reaches ThickEndInto into it, over its broken face's notches). Where the neighbour's own
                    // underside carries on along the same line, the two strips join seamlessly (world-tiled): no end there (two
                    // ragged ends left a sliver of sky between them).
                    float beyond = atLeft ? e.From - 0.05f : e.To + 0.05f;
                    bool below = solids.Any(o => o.Name != s.Name && o.Rect.Contains(new Vector2(beyond, e.Line - 0.2f)));
                    // 2026-10-19 (the developer, L007: "this section is not clean"): a fixed underside never counts on a trap's to
                    // carry it on; the trap leaves (a floor collapses, a block falls), and the fixed one ended in a straight cut over
                    // the hole. Its broken end hides under the trap's strip until then.
                    bool carriesOn = !below && allEdges.Any(o => o.Owner.Name != s.Name && o.Side == Side.Bottom && Mathf.Abs(o.Line - e.Line) < Eps
                        && o.From <= beyond && o.To >= beyond && (s.Trap || !o.Owner.Trap) && ThickUnderFits(o, solids, flips, visualKeepOut));
                    if (carriesOn)
                    {
                        // A trap's strip runs on over the fixed neighbour's broken end (drawn since 2026-10-19, above), so the end's
                        // notches don't show the sky while the trap is in place; it leaves with the trap.
                        if (s.Trap && end != null) { float over = end.width - end.outside; if (atLeft) from -= over; else to += over; }
                        continue;
                    }
                    float into = below ? ThickEndInto : 0f;
                    float x = atLeft ? e.From - into - end.outside + end.width * 0.5f : e.To + into + end.outside - end.width * 0.5f;
                    SpriteRenderer piece = Add(s, (atLeft ? "ThickEndL_" : "ThickEndR_") + Idx(e), end.name, new Vector2(x, cy), new Vector2(end.width, h), Vector2.zero, Vector4.zero, false, ZUnder - 0.0005f, false);
                    if (piece != null) { piece.flipX = !atLeft; piece.color = body; }
                    float inner = end.width - end.outside - ThickEndFade - into;
                    if (atLeft) from += inner; else to -= inner;
                }
                float mid = (from + to) * 0.5f;
                Tint(Add(s, "Thick_" + Idx(e), thickUnder.name, new Vector2(mid, cy), new Vector2(to - from, h),
                    new Vector2(thickUnder.width, h), new Vector4(mid, h * 0.5f, 1f, 1f), false, ZUnder, true, shortened ? "ThickUnderShort" : "ThickUnder"), body);
                return true;
            }

            // PAX-A16 §3.3: ivy spilling over a lit lip, down its face (every walkable top of a block or slab, real or disguised).
            void LipVines(Solid s, Edge e)
            {
                if (lipVines == null || e.Length < 2f) return;
                float h = lipVines.height, cy = e.Line - 0.12f - h * 0.5f, from = e.From + 0.05f, to = e.To - 0.05f;
                // 2026-10-16 ("All edges need to be a bit bumpy"): the ivy ends raggedly (ENV_LipVinesEnd), not in a straight cut.
                EnvironmentKit.Slot end = EnvironmentKit.Get("ENV_LipVinesEnd");
                if (end != null && to - from >= 2f * (end.width - end.outside) + 0.1f)
                    foreach (bool atLeft in new[] { true, false })
                    {
                        float x = atLeft ? from - end.outside + end.width * 0.5f : to + end.outside - end.width * 0.5f;
                        SpriteRenderer piece = Add(s, (atLeft ? "VinesEndL_" : "VinesEndR_") + Idx(e), end.name, new Vector2(x, cy), new Vector2(end.width, h), Vector2.zero, Vector4.zero, false, ZMoss + 0.0005f, false);
                        if (piece != null) piece.flipX = !atLeft;
                        float inner = end.width - end.outside - ThickEndFade;
                        if (atLeft) from += inner; else to -= inner;
                    }
                float mid = (from + to) * 0.5f;
                Add(s, "Vines_" + Idx(e), "ENV_LipVines", new Vector2(mid, cy), new Vector2(to - from, h),
                    new Vector2(lipVines.width, h), new Vector4(mid, h * 0.5f, 1f, 1f), false, ZMoss, true, "LipVines");
            }

            // Round 3: the face's shadow, clear for ShadeStart under a block's lit lip, then darkening over ShadeRamp. A pit's floor
            // lies down in that shadow already: it's shaded whole, from its top. (Baked into the room's StoneShade map, BakeShade.)
            // 2026-10-17 (the developer: "sometimes it doesn't match the color of the tile it is attached to"): the shadow follows
            // the stone column at x (every solid stacked on, under or over s there), not s's own rectangle, so a trap floor flush
            // in the ground, a pit's floor under it and a wall drawn over another wall all shade exactly like the stone beside
            // them (each solid's own ramp drew its outline as a darker box).
            (float top, float bottom) Column(Solid s, float x)
            {
                float top = s.Rect.yMax, bottom = s.Rect.yMin;
                // 2026-10-18 visual review (L019: "cold slate hanging columns"): an upright hung under a slab or a roof is lit from
                // where it hangs, not shaded as the deep face of the slab's whole column (a thin pillar against the sky read as
                // slate); its top meets the slab's unshaded underside.
                bool upright = s.Kind == SoloRoomElementKind.Wall;
                for (bool grew = true; grew;)
                {
                    grew = false;
                    foreach (Solid o in solids)
                    {
                        if (o.Shape is not (Shape.Block or Shape.PitBottom or Shape.Slab) || o.Rect.xMin >= x || o.Rect.xMax <= x) continue;
                        if (o.Rect.yMin <= top + Eps && o.Rect.yMax > top + Eps && !(upright && o.Kind != SoloRoomElementKind.Wall)) { top = o.Rect.yMax; grew = true; }
                        if (o.Rect.yMax >= bottom - Eps && o.Rect.yMin < bottom - Eps) { bottom = o.Rect.yMin; grew = true; }
                    }
                }
                return (top, bottom);
            }

            (float top, float ramp, bool any) ShadeOf(Solid s, float x)
            {
                if (s.Shape is not (Shape.Block or Shape.PitBottom) || faceShade == null || shadeSolid == null) return (0f, 0f, false);
                if (!s.Trap && roomRoot.Find(s.Name)?.GetComponent<SpriteRenderer>() == null) return (0f, 0f, false);
                (float top, float bottom) = Column(s, x);
                // D-253 (the developer, of L030's corridor roof: "The ceiling is also shaded ... instead of using the same normal
                // tiling"): stone whose column ends on a ceiling is seen from under it: no lit top to fall away from, unshaded
                // (L030's Roof_A-C carry 24 u of Fill, so their undersides were deep in its shadow).
                if (solids.Any(o => o.Kind == SoloRoomElementKind.Ceiling && Mathf.Abs(o.Rect.yMin - bottom) < Eps && o.Rect.xMin < x && o.Rect.xMax > x))
                    return (0f, 0f, false);
                // An upright hung under a slab or a roof (Column stops at its top) has no lit top to fall away from: unshaded,
                // like the underside it hangs from.
                if (s.Kind == SoloRoomElementKind.Wall && solids.Any(o => o.Kind != SoloRoomElementKind.Wall && o.Shape is Shape.Block or Shape.Slab
                    && Mathf.Abs(o.Rect.yMin - top) < Eps && o.Rect.xMin < x && o.Rect.xMax > x)) return (0f, 0f, false);
                // A pit's floor with nothing on it lies down in the shadow already: shaded whole, from its top.
                if (s.Shape == Shape.PitBottom && top <= s.Rect.yMax + Eps) return (s.Rect.yMax, 0f, true);
                float start = ShadeStart;
                if (bottom <= groundLine + Eps && top > groundLine) start = ShadeStart + (top - groundLine);
                if (top - bottom <= start) return (0f, 0f, false);
                return (top - start, Mathf.Min(ShadeRamp, top - bottom - start), true);
            }

            // 2026-10-16: a fixed block's face against a trap keeps its broken strip (ExposedEdges), whose notches cut into the
            // block. While the trap is there its own stone fills them: the trap's skin reaches DeepReach into the neighbour (the
            // same world-tiled stone, so nothing changes) and leaves with it, baring the broken face.
            // The heights a vertical solid mover's side covers at every point of its travel (none for any other trap).
            (float, float) AlwaysCovered(Solid s)
            {
                SoloRoomElement e = room.Elements.FirstOrDefault(x => x.Name == s.Name);
                if (e.Kind != SoloRoomElementKind.MovingTrap || e.Settings.MovingKind != MovingTrapKind.Solid || Mathf.Abs(e.Settings.Offset.x) > Eps
                    || Mathf.Abs(e.Settings.Offset.y) <= Eps || e.Settings.Floor.Hinges) return (0f, 0f);
                float dy = e.Settings.Offset.y;
                return (Mathf.Max(s.Rect.yMin, s.Rect.yMin + dy), Mathf.Min(s.Rect.yMax, s.Rect.yMax + dy));
            }

            void TrapReach(Solid s)
            {
                Sprite fillSprite = FillSprite(room);
                if (fillSprite == null || !ChipsReady(room)) return;
                // 2026-10-19 (the developer, L011: "this gap ... looks super weird"): a vertical solid mover lends its stone over the
                // span its face covers at the far end of its travel too, and to a post beside it (G_East beside the Riser showed
                // its broken face as a sky sliver all the way up the risen pillar).
                (float c0, float c1) = AlwaysCovered(s);
                SoloRoomElement me = room.Elements.FirstOrDefault(x => x.Name == s.Name);
                float dy = c1 > c0 ? me.Settings.Offset.y : 0f;
                foreach (Solid f in solids.Where(o => !o.Trap && (o.Shape is Shape.Block or Shape.PitBottom or Shape.Slab || (dy != 0f && o.Shape == Shape.Post))))
                    foreach (bool right in new[] { true, false })
                    {
                        float face = right ? s.Rect.xMax : s.Rect.xMin;
                        // 2026-10-17: or the host it leaves a hole in, where the host's stone goes on beside it (HoleInsets).
                        bool host = Holes(f, room).Any(h => Mathf.Abs(h.xMin - s.Rect.xMin) < Eps && Mathf.Abs(h.yMin - s.Rect.yMin) < Eps)
                            && (right ? f.Rect.xMax > face + Eps : f.Rect.xMin < face - Eps);
                        if (!host && Mathf.Abs((right ? f.Rect.xMin : f.Rect.xMax) - face) > Eps) continue;
                        float y0 = Mathf.Max(s.Rect.yMin, f.Rect.yMin), y1 = Mathf.Min(s.Rect.yMax, f.Rect.yMax);
                        // A neighbour the mover slides along (its face runs on past the mover's rest in the way it travels): the lent
                        // stone covers every height of the face the mover's side passes, in the mover's rest frame, and rides with it.
                        bool ride = dy > 0f ? f.Rect.yMax > s.Rect.yMax + Eps : dy < 0f && f.Rect.yMin < s.Rect.yMin - Eps;
                        if (ride)
                        {
                            float m0 = Mathf.Max(s.Rect.yMin, f.Rect.yMin - dy), m1 = Mathf.Min(s.Rect.yMax, f.Rect.yMax - dy);
                            if (m1 - m0 > Eps) { if (y1 - y0 > Eps) { y0 = Mathf.Min(y0, m0); y1 = Mathf.Max(y1, m1); } else { y0 = m0; y1 = m1; } }
                        }
                        if (y1 - y0 <= Eps) continue;
                        string lend = ride ? (right ? "RideR_" : "RideL_") : (right ? "ReachR_" : "ReachL_");
                        // 2026-10-17: over the neighbour's whole strip (its notches and the shade it paints toward its broken edge,
                        // fading into the fill over STRIP_OVERLAP + STRIP_BLEND): stopping at DeepReach left a darker band beside it.
                        float reach = DeepReach + ReachPastStrip;
                        Rect q = right ? Rect.MinMaxRect(face, y0, face + reach, y1) : Rect.MinMaxRect(face - reach, y0, face, y1);
                        // And over its broken top corner, where the two tops are one line: the neighbour's cap runs on under the
                        // trap's (a broken corner beside the trap's whole top was a step in the lit lip).
                        // (2026-10-17 visual review, L009: on a post no wider than the cap, it stood out past the post as a floating
                        // lit chunk: only over a top wide enough to continue, CapReach + 0.3.)
                        if (!host && cap != null && f.Shape != Shape.Slab && Mathf.Abs(f.Rect.yMax - s.Rect.yMax) < Eps && f.Rect.width >= CapReach + 0.3f)
                        {
                            float cx = right ? face + CapReach * 0.5f : face - CapReach * 0.5f, cy = f.Rect.yMax + cap.above - cap.height * 0.5f;
                            Add(s, (right ? "ReachCapR_" : "ReachCapL_") + f.Name, cap.name, new Vector2(cx, cy), new Vector2(CapReach, cap.height),
                                new Vector2(cap.width, cap.height), new Vector4(cx, cap.height * 0.5f, 1f, 1f), false, ZCap - 0.0005f, true, "Cap");
                        }
                        if (f.Shape == Shape.Slab)
                        {
                            // A slab's stone is its own strip: the same strip, sampled as the slab samples it.
                            string slabName = flips && EnvironmentKit.Get("ENV_SlabFlip") != null ? "ENV_SlabFlip" : "ENV_Slab";
                            float k = f.Rect.height / 0.5f, h = slab.height * k, cy = f.Rect.yMax + slab.above * k - h * 0.5f;
                            q = Rect.MinMaxRect(q.xMin, cy - h * 0.5f, q.xMax, cy + h * 0.5f);
                            Add(s, lend + f.Name, slabName, q.center, q.size, new Vector2(slab.width * k, h), new Vector4(q.center.x, h * 0.5f, 1f, 1f),
                                false, ZCap - 0.0005f, true, "Slab");
                            continue;
                        }
                        // (Shaded where it stands by the room's StoneShade map, like the neighbour's own stone.)
                        Tint(Add(s, lend + f.Name, fillSprite.name, q.center, q.size, fillSprite.bounds.size, new Vector4(q.center.x, q.center.y, 1f, 1f),
                            false, ZSide - 0.001f, true, "Fill"), body);
                        // 2026-10-18 visual review (L011: "sky slivers between abutting bodies" beside the Riser): a mover sliding
                        // straight up or down along the face still covers it over the span it never leaves; there the neighbour's
                        // own stone fills its notches for good (the lent stone hides once the mover leaves its rest).
                        float k0 = Mathf.Max(Mathf.Max(s.Rect.yMin, f.Rect.yMin), c0), k1 = Mathf.Min(Mathf.Min(s.Rect.yMax, f.Rect.yMax), c1);
                        if (k1 - k0 > 0.1f)
                        {
                            Rect kq = Rect.MinMaxRect(q.xMin, k0, q.xMax, k1);
                            Tint(Add(f, (right ? "KeptR_" : "KeptL_") + s.Name, fillSprite.name, kq.center, kq.size, fillSprite.bounds.size, new Vector4(kq.center.x, kq.center.y, 1f, 1f),
                                false, ZSide - 0.001f, true, "Fill"), body);
                        }
                    }
            }

            // 2026-10-17 (the developer: "sometimes it doesn't match the color of the tile it is attached to"): the room's stone
            // shadow as a world-space map (StoneShade) the stone's shaders read, so the fill, its broken strips, a trap's skin and
            // the reach it lends its neighbour all take the same shadow at the same place (quads over the fill and multiply tints
            // on the strips never matched: the navy lifted the stone's blue past what a multiply can). Red: the face shadow;
            // green: the outside masonry's, fading in over OutsideFade across the room's box, so where the room's stone meets
            // the outside's there's no straight line.
            void BakeShade()
            {
                const int Ppu = ShadePpu;
                Bounds box = SoloRoomBuilder.ComputeRoomBounds(room, 0f);
                float bx0 = room.Origin.x, bx1 = room.Origin.x + room.Width, by0 = box.min.y, by1 = box.max.y;
                Rect area = Rect.MinMaxRect(Mathf.Floor(bx0 - ShadeMargin), Mathf.Floor(by0 - ShadeMargin), Mathf.Ceil(bx1 + ShadeMargin), Mathf.Ceil(by1 + ShadeMargin));
                int w = Mathf.RoundToInt(area.width * Ppu), h = Mathf.RoundToInt(area.height * Ppu);
                var face = new float[w * h];
                var stone = new bool[w * h];
                for (int i = 0; i < w; i++)
                {
                    float x = area.xMin + (i + 0.5f) / Ppu;
                    // Each solid's column at this x, once.
                    var here = solids.Where(o => o.Rect.xMin < x && o.Rect.xMax > x).Select(o => (o, shade: ShadeOf(o, x))).ToList();
                    for (int j = 0; j < h; j++)
                    {
                        float y = area.yMin + (j + 0.5f) / Ppu;
                        foreach ((Solid o, (float top, float ramp, bool any) sh) in here)
                        {
                            if (y < o.Rect.yMin || y > o.Rect.yMax) continue;
                            stone[j * w + i] = true;
                            if (!sh.any || y >= sh.top) continue;
                            float t = sh.ramp <= 0f ? 1f : Mathf.Clamp01((sh.top - y) / sh.ramp);
                            face[j * w + i] = Mathf.Max(face[j * w + i], t * t * (3f - 2f * t));   // ENV_FaceShade's profile
                        }
                    }
                }
                // Out over the air beside a face (its broken strip's stones stand there), from the nearest stone on the row.
                int reach = Mathf.CeilToInt(ShadeDilate * Ppu);
                var spread = (float[])face.Clone();
                for (int j = 0; j < h; j++)
                    for (int i = 0; i < w; i++)
                    {
                        if (stone[j * w + i]) continue;
                        for (int d = 1; d <= reach; d++)
                        {
                            bool l = i - d >= 0 && stone[j * w + i - d], r = i + d < w && stone[j * w + i + d];
                            if (!l && !r) continue;
                            spread[j * w + i] = Mathf.Max(l ? face[j * w + i - d] : 0f, r ? face[j * w + i + d] : 0f);
                            break;
                        }
                    }
                // Softened across a change of column (a step in the stone's top), so it's never a straight line down the stone.
                face = BoxBlurRows(spread, w, h, Mathf.RoundToInt(ShadeSoften * Ppu));
                // The masonry filling under the room's lowest stone (UnderFill) goes on in that stone's shadow.
                foreach (Rect f in UnderFill(room))
                {
                    int i0 = Mathf.Max(0, Mathf.FloorToInt((f.xMin - area.xMin) * Ppu)), i1 = Mathf.Min(w - 1, Mathf.CeilToInt((f.xMax - area.xMin) * Ppu));
                    int j0 = Mathf.Max(0, Mathf.FloorToInt((f.yMin - area.yMin) * Ppu)), jTop = Mathf.Clamp(Mathf.RoundToInt((f.yMax - area.yMin) * Ppu), 0, h - 1);
                    for (int i = i0; i <= i1; i++)
                        for (int j = j0; j < jTop; j++) face[j * w + i] = Mathf.Max(face[j * w + i], face[jTop * w + i]);
                }
                var outside = new float[w * h];
                for (int j = 0; j < h; j++)
                    for (int i = 0; i < w; i++)
                    {
                        float x = area.xMin + (i + 0.5f) / Ppu, y = area.yMin + (j + 0.5f) / Ppu;
                        float d = Mathf.Max(Mathf.Max(bx0 - x, x - bx1), Mathf.Max(by0 - y, y - by1));   // > 0 outside the room's box
                        outside[j * w + i] = Mathf.Clamp01(0.5f + d / OutsideFade);
                    }
                var texels = new byte[w * h * 2];
                for (int k = 0; k < w * h; k++)
                {
                    texels[2 * k] = (byte)Mathf.RoundToInt(Mathf.Clamp01(face[k]) * 255f);
                    texels[2 * k + 1] = (byte)Mathf.RoundToInt(outside[k] * 255f);
                }
                Transform holder = Child(env, "StoneShade", layer);
                SetupUtility.Ensure<StoneShade>(holder.gameObject, changes).Configure(area, w, h, texels, LevelShade, OutsideShade);
            }

            void AddVertical(Solid s, string name, string slot, EnvironmentKit.Slot def, Rect r, float k, string kind)
            {
                float w = def.width * k, tileH = def.height;
                Tint(Add(s, name, slot, r.center, new Vector2(w, r.height), new Vector2(w, tileH), new Vector4(w * 0.5f, r.center.y, 1f, 1f), false, ZSide, true, kind), postTint);
            }

            static void Tint(SpriteRenderer r, Color c) { if (r != null) r.color = c; }

            void AddObject(Solid s, string name, string slot, EnvironmentKit.Slot def, Vector2 at, float width, bool top)
            {
                float h = def.height * width / def.width;
                Add(s, name, slot, new Vector2(at.x, top ? at.y - h * 0.5f + 0.12f : at.y + h * 0.5f - 0.05f), new Vector2(width, h), Vector2.zero, Vector4.zero, false, ZCap, false);
            }

            Dress(room, solids, edges, (s, name, slot, centre, size, z) =>
            {
                if (!UprightMoss(name)) return Add(s, name, slot, centre, size, Vector2.zero, Vector4.zero, false, z, false);
                // D-118: the sprite's length down the face (a quarter turn), its footprint as planned.
                SpriteRenderer r = Add(s, name, slot, centre, new Vector2(size.y, size.x), Vector2.zero, Vector4.zero, false, z, false);
                if (r != null) r.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                return r;
            });

            // A trap's trims follow its skin.
            foreach (KeyValuePair<string, List<SpriteRenderer>> pair in trapTrims)
            {
                var (parent, owner, _) = hosts[pair.Key];
                ShownWith follow = parent.gameObject.AddComponent<ShownWith>();
                bool shrinks = solids.First(s => s.Name == pair.Key).Shrinks;
                TrapKitSetup.Write(follow, changes, ("owner", owner), ("followWidth", shrinks));
                // A collapsing floor keeps its trims on its reveal tick, while its first shard is still where it sat.
                CollapsingFloorArt crumble = owner.GetComponentInParent<CollapsingFloorArt>();
                SpriteRenderer shard = crumble != null ? crumble.transform.Find("Shard_00")?.GetComponent<SpriteRenderer>() : null;
                if (shard != null)
                {
                    var homes = new SerializedObject(crumble).FindProperty("shardHomes");
                    Vector2 home = (Vector2)crumble.transform.position + (homes != null && homes.arraySize > 0 ? homes.GetArrayElementAtIndex(0).vector2Value : Vector2.zero);
                    TrapKitSetup.Write(follow, changes, ("holdWith", shard), ("holdAt", home));
                }
                // 2026-10-17: the stone it lends its neighbours stays with them (ShownAtRest), never riding out with it.
                // (A vertical mover's stone for a face it slides along is named Ride*, not Reach*: it rides with it, TrapReach.)
                List<SpriteRenderer> lent = pair.Value.Where(r => r.name.StartsWith("Reach")).ToList();
                // A fallen block's top course (fallenTops) shows once it has left the stone it hung in.
                Solid trap = solids.First(o => o.Name == pair.Key);
                List<SpriteRenderer> topCourse = fallenTops.Contains(pair.Key)
                    ? pair.Value.Except(lent).Where(r => r.name.StartsWith("Cap") && r.bounds.min.y < trap.Rect.yMax - Eps && r.bounds.max.y > trap.Rect.yMax - Eps).ToList()
                    : new List<SpriteRenderer>();
                SetupUtility.SetArray(follow, "renderers", pair.Value.Except(lent).Except(topCourse).Cast<Object>().ToArray(), changes);
                follow.Apply();
                if (topCourse.Count > 0)
                {
                    ShownAtRest moved = parent.gameObject.AddComponent<ShownAtRest>();
                    TrapKitSetup.Write(moved, changes, ("owner", owner), ("rest", (Vector2)owner.transform.position), ("whenMoved", true));
                    SetupUtility.SetArray(moved, "renderers", topCourse.Cast<Object>().ToArray(), changes);
                    moved.Apply();
                }
                if (lent.Count > 0)
                {
                    ShownAtRest rest = parent.gameObject.AddComponent<ShownAtRest>();
                    TrapKitSetup.Write(rest, changes, ("owner", owner), ("rest", (Vector2)owner.transform.position));
                    SetupUtility.SetArray(rest, "renderers", lent.Cast<Object>().ToArray(), changes);
                    rest.Apply();
                }
            }
            VinesToTheirStone();
            UnderRestingTraps();
            BakeShade();
            changes.Add($"built {roomRoot.name} environment trims ({edges.Count} exposed edges)");

            // 2026-10-17 visual review (L003: "lit-cap strips show through the lower ground wall, exactly where the floor later
            // collapses"; L001: "a lit top course buried inside the floor wall"): a fixed top a trap rests on keeps its trims for
            // when the trap has gone (ExposedEdges), but its lit course is painted down into the stone below the trap's skin, a
            // stripe through the wall and a tell. Those trims (and an underside's over a trap below it) show once the trap has
            // gone (ShownAtRest, whenGone).
            void UnderRestingTraps()
            {
                foreach (Solid t in solids.Where(o => o.Trap))
                {
                    SpriteRenderer owner = Host(t).owner;
                    if (owner == null) continue;
                    var covered = new List<SpriteRenderer>();
                    foreach (Solid f in solids.Where(o => !o.Trap))
                    {
                        float line = Mathf.Abs(f.Rect.yMax - t.Rect.yMin) < Eps ? t.Rect.yMin : Mathf.Abs(f.Rect.yMin - t.Rect.yMax) < Eps ? t.Rect.yMax : float.NaN;
                        Transform host = float.IsNaN(line) ? null : env.Find(f.Name);
                        if (host == null) continue;
                        foreach (SpriteRenderer r in host.GetComponentsInChildren<SpriteRenderer>(true))
                        {
                            Bounds b = r.bounds;
                            float over = Mathf.Min(b.max.x, t.Rect.xMax) - Mathf.Max(b.min.x, t.Rect.xMin);
                            // Only a lit course or an underside strip (Cap*, Under*): a slab's body is one "Slab" sprite, and hiding it
                            // took the whole slab out wherever a trap rested against it (2026-10-18, L017's slams).
                            bool course = r.name.StartsWith("Cap") || r.name.StartsWith("Under");
                            if (course && b.min.y < line - Eps && b.max.y > line + Eps && over >= 0.6f * b.size.x) covered.Add(r);
                        }
                    }
                    // 2026-10-18 (the developer, L004: "Look how weird it looks"): a static hazard hidden in the trap's box (a kill strip
                    // in the recess behind Roof_4 or under Sink_R) drew over the trap's skin; it shows once the trap has gone.
                    // (hidden only: SpikeArt shows its own blades, and keeps its legacy strip off)
                    var blades = new List<SpriteRenderer>();
                    foreach (SoloRoomElement h in room.Elements.Where(x => x.Kind == SoloRoomElementKind.Hazard))
                    {
                        var hr = new Rect(room.Origin + h.Position - h.Size * 0.5f, h.Size);
                        Transform art = artRoot != null ? artRoot.Find(h.Name) : null;
                        if (art == null || !(t.Rect.Contains(hr.min + Vector2.one * Eps) && t.Rect.Contains(hr.max - Vector2.one * Eps))) continue;
                        blades.AddRange(art.GetComponentsInChildren<SpriteRenderer>(true));
                    }
                    if (blades.Count > 0)
                    {
                        ShownAtRest hide = Child(env, "InTrap_" + t.Name, layer).gameObject.AddComponent<ShownAtRest>();
                        TrapKitSetup.Write(hide, changes, ("owner", owner), ("rest", (Vector2)owner.transform.position), ("whenGone", true), ("hideOnly", true));
                        SetupUtility.SetArray(hide, "renderers", blades.Cast<Object>().ToArray(), changes);
                        hide.Apply();
                        changes.Add($"{blades.Count} hazard sprites in {t.Name} show once it has gone");
                    }
                    if (covered.Count == 0) continue;
                    ShownAtRest gone = Child(env, "UnderTrap_" + t.Name, layer).gameObject.AddComponent<ShownAtRest>();
                    TrapKitSetup.Write(gone, changes, ("owner", owner), ("rest", (Vector2)owner.transform.position), ("whenGone", true));
                    SetupUtility.SetArray(gone, "renderers", covered.Cast<Object>().ToArray(), changes);
                    gone.Apply();
                    changes.Add($"{covered.Count} trims under {t.Name} show once it has gone");
                }
            }

            // 2026-10-17 (the developer: "make of these vines attached to the ceiling and not just hanging mid air ... for ever
            // vine"): a vine reaches up from its grab box to the stone over it (VineReachUp at most) and a little into it. The vine
            // itself is untouched (its renderers are the route harness's, TrapArtBuildTests): the reach is environment art, its
            // middle tiled up from the vine's anchor and, past one anchor's height, a second anchor tied into the stone. It shows
            // while the vine hangs where it was built (ShownAtRest), so a snapping vine doesn't leave it behind.
            void VinesToTheirStone()
            {
                foreach (SoloRoomElement v in room.Elements.Where(x => x.Kind == SoloRoomElementKind.Vine))
                {
                    Transform vt = roomRoot.Find(v.Name);
                    SpriteRenderer midArt = vt != null ? vt.Find("Segment_1")?.GetComponent<SpriteRenderer>() : null;
                    SpriteRenderer anchorArt = vt != null ? vt.Find("Segment_2")?.GetComponent<SpriteRenderer>() : null;
                    if (anchorArt == null || midArt == null || midArt.drawMode != SpriteDrawMode.Tiled) continue;
                    Bounds knot = anchorArt.bounds;
                    float x = knot.center.x, top = knot.max.y;
                    // The nearest stone over it; a trap there (a slam, a falling block) stops it: no reach past something that moves
                    // (2026-10-18 visual review, L014: a vine run up through a slam's plate to the stone behind it).
                    Solid? over = solids.Where(o => o.Rect.xMin < x && o.Rect.xMax > x && o.Rect.yMin >= top - Eps)
                        .OrderBy(o => o.Rect.yMin).Cast<Solid?>().FirstOrDefault();
                    if (!over.HasValue || over.Value.Trap) continue;
                    float above = over.Value.Rect.yMin;
                    if (above - top > VineReachUp || above - top < Eps) continue;
                    float reachTop = above + VineIntoStone;
                    Transform holder = Child(env, "VineReach_" + v.Name, layer);
                    var shown = new List<SpriteRenderer>();
                    SpriteRenderer Piece(string name, SpriteRenderer like, Vector2 centre)
                    {
                        Transform t = Child(holder, name, layer);
                        t.position = new Vector3(centre.x, centre.y, like.transform.position.z);
                        t.localScale = like.transform.lossyScale;
                        SpriteRenderer r = SetupUtility.Ensure<SpriteRenderer>(t.gameObject, changes);
                        r.sprite = like.sprite; r.sharedMaterial = like.sharedMaterial; r.color = like.color;
                        r.sortingLayerID = like.sortingLayerID; r.sortingOrder = like.sortingOrder;
                        shown.Add(r);
                        return r;
                    }
                    // A second anchor when the gap holds one; the middle runs from the vine's anchor up to it (or to the stone).
                    float midTop = reachTop;
                    if (reachTop - top >= knot.size.y)
                    {
                        Piece("Anchor", anchorArt, new Vector2(x, reachTop - knot.extents.y));
                        midTop = reachTop - knot.extents.y;
                    }
                    SpriteRenderer reach = Piece("Middle", midArt, new Vector2(x, (knot.center.y + midTop) * 0.5f));
                    reach.drawMode = SpriteDrawMode.Tiled;
                    reach.size = new Vector2(midArt.size.x, midTop - knot.center.y);
                    reach.transform.position += new Vector3(0f, 0f, 0.001f);   // behind both knots, at the vine's own order
                    ShownAtRest rest = holder.gameObject.AddComponent<ShownAtRest>();
                    TrapKitSetup.Write(rest, changes, ("owner", anchorArt), ("rest", (Vector2)anchorArt.transform.position));
                    SetupUtility.SetArray(rest, "renderers", shown.Cast<Object>().ToArray(), changes);
                    rest.Apply();
                    changes.Add($"reached {v.Name} up {reachTop - top:F2} u to its stone");
                }
            }
        }

        // ---------- PAX-106 (D-114): broken-stone sides ----------

        /// <summary>The broken-stone strips are in the kit, cut from this room's fill, with their material.</summary>
        static bool ChipsReady(SoloRoomDefinition room) => ChipSlotName(room, 0) is string slot && EnvironmentKit.Get(slot) != null && EnvironmentKit.TileMaterial(EdgeKindOf(room, "SideChip")) != null;

        /// <summary>PAX-A18 W3: the edge-mask material kind for this room's fill (ENV_SideChip_A, …).</summary>
        static string EdgeKindOf(SoloRoomDefinition room, string kind)
        {
            Sprite fill = FillSprite(room);
            return fill == null || !fill.name.StartsWith("ENV_Fill_") ? kind : EnvironmentKit.EdgeKind(kind, fill.name.Substring("ENV_Fill_".Length));
        }

        static string ChipSlotName(SoloRoomDefinition room, int variant)
        {
            Sprite fill = FillSprite(room);
            return fill == null || !fill.name.StartsWith("ENV_Fill_") ? null : $"ENV_SideChip_{fill.name.Substring("ENV_Fill_".Length)}_{variant}";
        }

        /// <summary>The broken-stone strip a side face draws: one of four cut from the room's fill, picked by hash (deterministic per
        /// element and face); null when the kit or its material lacks them (ENV_Side then).</summary>
        public static string ChipSlot(SoloRoomDefinition room, Edge e)
        {
            if (!ChipsReady(room)) return null;
            int variant = Mathf.Min(3, (int)(Hash(e.Owner.Name, 700 + (e.Side == Side.Left ? 0 : 1) + Mathf.RoundToInt(e.From * 4f)) * 4f));
            string slot = ChipSlotName(room, variant);
            return EnvironmentKit.Get(slot) != null ? slot : null;
        }

        /// <summary>Where a side face's strip is drawn: its air side `outside` beyond the face line, the rest inside, along the
        /// face's exposed stretch.</summary>
        /// <summary>2026-10-16: a deep strip (slot and its edge-mask material) for an edge no solid owns: the outside masonry
        /// meeting the room's air (EnvironmentStackSetup.Outside). (null, null) when the kit lacks them.</summary>
        public static (string slot, Material material) OutsideStrip(SoloRoomDefinition room, int variant)
        {
            string chip = ChipsReady(room) ? ChipSlotName(room, Mathf.Clamp(variant, 0, 3)) : null;
            if (chip == null) return (null, null);
            string deep = chip.Replace("ENV_SideChip_", "ENV_SideDeep_");
            Material m = EnvironmentKit.TileMaterial(EdgeKindOf(room, "SideDeep"));
            return EnvironmentKit.Get(deep) != null && m != null ? (deep, m) : (null, null);
        }

        public static Rect SideStrip(Edge e, EnvironmentKit.Slot def)
        {
            float xMin = e.Side == Side.Left ? e.Line - def.outside : e.Line + def.outside - def.width;
            return new Rect(xMin, e.From, def.width, e.Length);
        }

        static bool WhollyExposed(Solid s, Side side, List<Edge> sides) =>
            sides.Where(e => e.Owner.Name == s.Name && e.Side == side).Sum(e => e.Length) >= s.Rect.height - 0.01f;

        /// <summary>D-114 (1), ruling R8: the thick underside cut to its stone band and 0.6 u of ivy, when the kit has it (and its
        /// material); the original otherwise.</summary>
        static EnvironmentKit.Slot ThickUnderSlot() =>
            EnvironmentKit.Get("ENV_ThickUnderShort") is EnvironmentKit.Slot cut && EnvironmentKit.TileMaterial("ThickUnderShort") != null ? cut : EnvironmentKit.Get("ENV_ThickUnder");

        /// <summary>PAX-A18 W3: an underside a gravity-up cat may stand on (a flip room) keeps a whole line with lumps beyond it
        /// (ENV_UnderWalk); every other one hangs broken stones (ENV_Under).</summary>
        static EnvironmentKit.Slot UnderSlot(bool flips) =>
            flips && EnvironmentKit.Get("ENV_UnderWalk") is EnvironmentKit.Slot walk && EnvironmentKit.TileMaterial("UnderWalk") != null ? walk : EnvironmentKit.Get("ENV_Under");

        /// <summary>2026-10-16: also a face on the room's own side line looking out (a jamb flush with the room's edge): the
        /// outside masonry's columns stand against it, so it isn't broken (its notches would show the air between them).</summary>
        static bool FacesOut(Edge e, Bounds content, SoloRoomDefinition room)
        {
            if (FacesOut(e, content) || FacesUnderFill(e, room)) return true;
            bool right = e.Side == Side.Right && e.Line >= room.Origin.x + room.Width - Eps, left = e.Side == Side.Left && e.Line <= room.Origin.x + Eps;
            if (!right && !left) return false;
            // Only up to the outside's column there (EnvironmentStackSetup.Outside): the wall's top, or the frame where the room's
            // roof runs into that side; above it the face looks into air.
            bool roofed = Solids(room).Any(s => s.Kind == SoloRoomElementKind.Ceiling && s.Rect.yMax >= content.max.y - 0.05f
                && (left ? s.Rect.xMin <= room.Origin.x + 0.05f : s.Rect.xMax >= room.Origin.x + room.Width - 0.05f));
            return roofed || e.To <= room.Origin.y + 8f + Eps;
        }

        /// <summary>The deepest a gap under the room's lowest fixed stone may be and still be filled by the masonry under the room.</summary>
        public const float MaxUnderFill = 2f;
        static SoloRoomElement[] underFillFor;
        static List<Rect> underFill;

        /// <summary>2026-10-18 visual review (L013: "the start floor's rectangular sky windows"): where the room's lowest fixed
        /// stone at an x stops short of the room's bottom (a 1 u ground beside pits sunk 1.5 u), the air under it is no play space;
        /// the masonry under the room fills it (EnvironmentStackSetup.Outside), and the stone over it shows no underside there.
        /// Not under a trap (its fall or travel must show), nor over a gap deeper than MaxUnderFill.</summary>
        public static List<Rect> UnderFill(SoloRoomDefinition room)
        {
            if (underFillFor == room.Elements && underFill != null) return underFill;
            const float Step = 0.05f;
            List<Solid> solids = Solids(room);
            float bottom = SoloRoomBuilder.ComputeRoomBounds(room, 0f).min.y, x0 = room.Origin.x, x1 = room.Origin.x + room.Width;
            var fills = new List<Rect>();
            float runFrom = x0, runTop = float.NaN;
            for (float x = x0; x <= x1 + 1e-4f; x += Step)
            {
                float top = float.NaN;
                if (x < x1 - 1e-4f)
                {
                    float mid = x + Step * 0.5f;
                    List<Solid> here = solids.Where(s => s.Rect.xMin < mid && s.Rect.xMax > mid).ToList();
                    if (here.Count > 0)
                    {
                        Solid lowest = here.OrderBy(s => s.Rect.yMin).First();
                        float gap = lowest.Rect.yMin - bottom;
                        if (!lowest.Trap && gap > 0.02f && gap <= MaxUnderFill) top = lowest.Rect.yMin;
                    }
                }
                bool same = float.IsNaN(top) ? float.IsNaN(runTop) : !float.IsNaN(runTop) && Mathf.Abs(top - runTop) < 1e-3f;
                if (same) continue;
                if (!float.IsNaN(runTop) && x - runFrom > 0.04f) fills.Add(Rect.MinMaxRect(runFrom, bottom, Mathf.Min(x, x1), runTop));
                runFrom = x; runTop = top;
            }
            // Only a gap walled at both ends (fixed stone down to the room's bottom, another fill, or the room's side): under a
            // bridge over an open chasm the air is the chasm's.
            bool Walled(Rect f, bool left)
            {
                float edge = left ? f.xMin : f.xMax;
                if (left ? edge <= x0 + 0.06f : edge >= x1 - 0.06f) return true;
                if (fills.Any(o => o != f && Mathf.Abs((left ? o.xMax : o.xMin) - edge) < 0.06f)) return true;
                return solids.Any(s => !s.Trap && Mathf.Abs((left ? s.Rect.xMax : s.Rect.xMin) - edge) < 0.06f && s.Rect.yMin <= f.yMin + 0.05f && s.Rect.yMax >= f.yMax - 0.05f);
            }
            for (bool dropped = true; dropped;)
            {
                List<Rect> keep = fills.Where(f => Walled(f, true) && Walled(f, false)).ToList();
                dropped = keep.Count < fills.Count;
                fills = keep;
            }
            underFillFor = room.Elements; underFill = fills;
            return fills;
        }

        /// <summary>Whether the air just outside edge `e` (all along it) lies in the masonry filling under the room.</summary>
        static bool FacesUnderFill(Edge e, SoloRoomDefinition room)
        {
            List<Rect> fills = UnderFill(room);
            if (fills.Count == 0) return false;
            bool Inside(Vector2 p) => fills.Any(f => f.xMin - Eps <= p.x && p.x <= f.xMax + Eps && f.yMin - Eps <= p.y && p.y <= f.yMax + Eps);
            Vector2 At(float t) => e.Side switch
            {
                Side.Bottom => new Vector2(Mathf.Lerp(e.From, e.To, t), e.Line - 0.02f),
                Side.Top => new Vector2(Mathf.Lerp(e.From, e.To, t), e.Line + 0.02f),
                Side.Left => new Vector2(e.Line - 0.02f, Mathf.Lerp(e.From, e.To, t)),
                _ => new Vector2(e.Line + 0.02f, Mathf.Lerp(e.From, e.To, t)),
            };
            return Inside(At(0.02f)) && Inside(At(0.5f)) && Inside(At(0.98f));
        }

        static bool FacesOut(Edge e, Bounds content) => e.Side switch
        {
            Side.Bottom => e.Line <= content.min.y + Eps,
            Side.Top => e.Line >= content.max.y - Eps,
            Side.Left => e.Line <= content.min.x + Eps,
            _ => e.Line >= content.max.x - Eps,
        };

        static string Idx(Edge e) => $"{Mathf.RoundToInt(e.From * 100f)}";

        static Transform Child(Transform parent, string name, int layer)
        {
            Transform t = parent.Find(name);
            if (t == null)
            {
                t = new GameObject(name).transform;
                t.SetParent(parent, false);
            }
            t.gameObject.layer = layer;
            return t;
        }
    }
}
