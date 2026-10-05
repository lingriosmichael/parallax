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
        const float ZFill = -0.005f, ZSide = -0.010f, ZShade = -0.0105f, ZUnder = -0.011f, ZCap = -0.012f, ZDrape = -0.015f, ZPanel = -0.02f, ZMoss = -0.025f;
        const int MaxDrapes = 4, MaxRubble = 2;
        const float TrapTrimZ = -0.05f;

        public enum Side { Top, Bottom, Left, Right }
        public enum Shape { Block, Slab, SlimPost, Post, PitBottom }

        public readonly struct Solid
        {
            public readonly string Name; public readonly Rect Rect; public readonly bool Trap; public readonly SoloRoomElementKind Kind; public readonly bool Shrinks;
            public Solid(string name, Rect rect, bool trap, SoloRoomElementKind kind, bool shrinks = false) { Name = name; Rect = rect; Trap = trap; Kind = kind; Shrinks = shrinks; }
            public Shape Shape =>
                Kind == SoloRoomElementKind.PitBottom ? Shape.PitBottom
                : Rect.height <= 0.55f && Rect.width > Rect.height ? Shape.Slab
                // D-111 (the developer, 2026-10-04: "give the walls that are falling a similar look and art as the floor"): a
                // falling block is never a column; it wears the floor's stone, fill and edges.
                : Kind == SoloRoomElementKind.FallingBlock ? Shape.Block
                : Rect.width <= 0.55f && Rect.height > Rect.width ? Shape.SlimPost
                : Rect.width <= 1.05f && Rect.height >= 2.5f && Name is not ("Wall_Left" or "Wall_Right") ? Shape.Post   // gauntlet: the room's walls are stone, not a column
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
                    // PAX-105 (D-110 amendment 2): a grip wall wears the wall look (its claw marks are GripSurface's, at runtime).
                    case SoloRoomElementKind.GripWall:
                        list.Add(new Solid(e.Name, rect, false, SoloRoomElementKind.Wall)); break;
                    case SoloRoomElementKind.CollapsingFloor: case SoloRoomElementKind.FakePlatform: case SoloRoomElementKind.FallingBlock:
                        list.Add(new Solid(e.Name, rect, true, e.Kind)); break;
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
                // Another solid continues past the edge.
                bool beyond = outward > 0 ? lo <= line + Eps && hi > line + Eps : hi >= line - Eps && lo < line - Eps;
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
            foreach (Solid s in Solids(room).Where(s => !s.Trap))
            {
                Transform t = roomRoot.Find(s.Name);
                SpriteRenderer r = t != null ? t.GetComponent<SpriteRenderer>() : null;
                if (r == null) continue;
                if (r.sprite != fill || r.sharedMaterial != worldTile || r.color != body) changes.Add("skinned " + s.Name);
                skin.ApplyWorldTiled(r, s.Rect.size, r.transform.position);
                r.enabled = false;
                // Slabs, slim posts and posts are covered edge to edge by their trims: no visible fill (one renderer less).
                Transform old = t.Find(FillName);
                if (s.Shape is Shape.Slab or Shape.SlimPost or Shape.Post) { if (old != null) Object.DestroyImmediate(old.gameObject); continue; }
                Transform child = Child(t, FillName, t.gameObject.layer);
                child.localPosition = new Vector3(0f, 0f, ZFill); child.localRotation = Quaternion.identity; child.localScale = Vector3.one;
                SpriteRenderer visible = SetupUtility.Ensure<SpriteRenderer>(child.gameObject, changes);
                visible.sprite = fill; visible.sharedMaterial = fillMaterial; visible.color = body;
                visible.drawMode = SpriteDrawMode.Sliced; visible.size = s.Rect.size;
                visible.sortingLayerName = layer; visible.sortingOrder = r.sortingOrder;
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

        /// <summary>The face shadow's tint: white, at LevelLookConfig.FaceShadeAlpha (the sprites carry the colour).</summary>
        static Color LevelShade
        {
            get
            {
                var config = AssetDatabase.LoadAssetAtPath<LevelLookConfig>(LevelLookConfig.AssetPath);
                return new Color(1f, 1f, 1f, config != null ? config.FaceShadeAlpha : 0.8f);
            }
        }

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
                t.position = new Vector3(centre.x, centre.y, parent.position.z + z + (owner != null ? TrapTrimZ : 0f));
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
                    else if (kind is "Side" or "Post" or "SlimPost" && EnvironmentKit.TileMaterial("Sprite") is Material plainSprite)
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
            List<Edge> edges = ExposedEdges(solids).Where(e => !FacesOut(e, content)).ToList();
            EnvironmentKit.Slot cap = EnvironmentKit.Get("ENV_Cap"), capWash = EnvironmentKit.Get("ENV_CapWash"), lipVines = EnvironmentKit.Get("ENV_LipVines"), faceShade = EnvironmentKit.Get("ENV_FaceShade"), shadeSolid = EnvironmentKit.Get("ENV_FaceShadeSolid"), thickUnder = EnvironmentKit.Get("ENV_ThickUnder"), under = EnvironmentKit.Get("ENV_Under"), side = EnvironmentKit.Get("ENV_Side");
            EnvironmentKit.Slot slab = EnvironmentKit.Get("ENV_Slab"), slabEnd = EnvironmentKit.Get("ENV_SlabEnd"), water = EnvironmentKit.Get("ENV_Water");
            EnvironmentKit.Slot post = EnvironmentKit.Get("ENV_Post"), slim = EnvironmentKit.Get("ENV_SlimPost"), postCap = EnvironmentKit.Get("ENV_PostCap"), postBase = EnvironmentKit.Get("ENV_PostBase");

            foreach (Solid s in solids)
            {
                Rect r = s.Rect;
                List<Edge> mine = edges.Where(e => e.Owner.Name == s.Name).ToList();
                switch (s.Shape)
                {
                    case Shape.PitBottom:
                        FaceShade(s);
                        // Phase 2 round 2 (the developer: "water only goes where nothing walkable sits on it"): a pit's floor is
                        // trimmed like any block, no water (the spikes and walls in pits read as standing in it).
                        foreach (Edge e in mine) BlockEdge(s, e);
                        break;
                    case Shape.Slab:
                    {
                        float k = r.height / 0.5f, h = slab.height * k, above = slab.above * k;
                        float cy = r.yMax + above - h * 0.5f;
                        Add(s, "Slab", "ENV_Slab", new Vector2(r.center.x, cy), new Vector2(r.width, h), new Vector2(slab.width * k, h),
                            new Vector4(r.center.x, h * 0.5f, 1f, 1f), false, ZCap, true, "Slab");
                        foreach (Edge bottom in mine.Where(t => t.Side == Side.Bottom)) Thick(s, bottom);
                        float endW = Mathf.Min(slabEnd.width * k, r.width * 0.5f);
                        if (r.width < 1f) break;   // gauntlet: broken ends from 1 u (short slabs read as hard-cut rectangles)
                        if (mine.Any(e => e.Side == Side.Left && e.Length >= r.height - Eps))
                            Tint(Add(s, "SlabEnd_L", "ENV_SlabEnd", new Vector2(r.xMin + endW * 0.5f - 0.03f, cy), new Vector2(endW, h), Vector2.zero, Vector4.zero, false, ZCap - 0.001f, false), slabTint);
                        if (mine.Any(e => e.Side == Side.Right && e.Length >= r.height - Eps))
                        {
                            SpriteRenderer end = Add(s, "SlabEnd_R", "ENV_SlabEnd", new Vector2(r.xMax - endW * 0.5f + 0.03f, cy), new Vector2(endW, h), Vector2.zero, Vector4.zero, true, ZCap - 0.001f, false);
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
                        FaceShade(s);
                        foreach (Edge e in mine) BlockEdge(s, e);
                        break;
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
                        Add(s, "Cap_" + Idx(e), top.name, new Vector2(mid, e.Line + top.above - top.height * 0.5f), new Vector2(e.Length, top.height),
                            new Vector2(top.width, top.height), new Vector4(mid, top.height * 0.5f, 1f, 1f), false, ZCap, true, kind);
                        break;
                    }
                    case Side.Bottom:
                        if (Thick(s, e)) break;
                        Tint(Add(s, "Under_" + Idx(e), "ENV_Under", new Vector2(mid, e.Line - under.below + under.height * 0.5f), new Vector2(e.Length, under.height),
                            new Vector2(under.width, under.height), new Vector4(mid, under.height * 0.5f, 1f, 1f), false, ZUnder, true, "Under"), face);
                        break;
                    default:
                        bool left = e.Side == Side.Left;
                        float cx = left ? e.Line - side.outside + side.width * 0.5f : e.Line + side.outside - side.width * 0.5f;
                        Tint(Add(s, (left ? "SideL_" : "SideR_") + Idx(e), "ENV_Side", new Vector2(cx, mid), new Vector2(side.width, e.Length),
                            new Vector2(side.width, side.height), new Vector4(side.width * 0.5f, mid, 1f, 1f), !left, ZSide, true, "Side"), face);
                        break;
                }
            }

            // PAX-A16 round 2: a thick stone underside with its cornice and long ivy, where ThickUnderFits says it may hang.
            bool Thick(Solid s, Edge e)
            {
                if (thickUnder == null || !ThickUnderFits(e, solids, flips, visualKeepOut)) return false;
                float mid = (e.From + e.To) * 0.5f, h = thickUnder.height;
                Tint(Add(s, "Thick_" + Idx(e), "ENV_ThickUnder", new Vector2(mid, e.Line + 0.04f - h * 0.5f), new Vector2(e.Length, h),
                    new Vector2(thickUnder.width, h), new Vector4(mid, h * 0.5f, 1f, 1f), false, ZUnder, true, "ThickUnder"), body);
                return true;
            }

            // PAX-A16 §3.3: ivy spilling over a lit lip, down its face (every walkable top of a block or slab, real or disguised).
            void LipVines(Solid s, Edge e)
            {
                if (lipVines == null || e.Length < 2f) return;
                float mid = (e.From + e.To) * 0.5f, h = lipVines.height;
                Add(s, "Vines_" + Idx(e), "ENV_LipVines", new Vector2(mid, e.Line - 0.12f - h * 0.5f), new Vector2(e.Length - 0.1f, h),
                    new Vector2(lipVines.width, h), new Vector4(mid, h * 0.5f, 1f, 1f), false, ZMoss, true, "LipVines");
            }

            // Round 3: the face's shadow, over the fill and the side faces, under the lip's trims (FaceShade).
            // A pit's floor lies down in that shadow already: it's shaded whole, from its top.
            void FaceShade(Solid s)
            {
                Rect r = s.Rect;
                // Only over stone that's drawn: a fixed solid without a renderer (the room's side walls) shows the sky.
                if (!s.Trap && roomRoot.Find(s.Name)?.GetComponent<SpriteRenderer>() == null) return;
                float start = s.Shape == Shape.PitBottom ? 0f : ShadeStart;
                // A block rising from the ground (a step, a pillar) shades from the ground's line, so it joins the ground's
                // shadow without a seam; a floating block shades from its own top.
                if (s.Shape != Shape.PitBottom && r.yMin <= groundLine + Eps && r.yMax > groundLine) start = ShadeStart + (r.yMax - groundLine);
                // Phase 2 round 2: a small block buried in a bigger solid's shaded depth (its top under that solid's top less
                // ShadeStart) takes the full shade, not a bright patch in a dark wall.
                if (s.Shape != Shape.PitBottom && solids.Any(o => o.Name != s.Name && o.Rect.Overlaps(r) && o.Rect.width * o.Rect.height > r.width * r.height && r.yMax <= o.Rect.yMax - ShadeStart + Eps))
                    start = 0f;
                if (faceShade == null || shadeSolid == null || r.height <= start) return;
                float ramp = s.Shape == Shape.PitBottom || start == 0f ? 0f : Mathf.Min(ShadeRamp, r.height - start), top = r.yMax - start;
                Color shade = LevelShade;
                if (ramp > 0f) Tint(Add(s, "Shade", "ENV_FaceShade", new Vector2(r.center.x, top - ramp * 0.5f), new Vector2(r.width, ramp), Vector2.zero, Vector4.zero, false, ZShade, false), shade);
                float rest = r.height - start - ramp;
                if (rest > 0.01f)
                    Tint(Add(s, "ShadeSolid", "ENV_FaceShadeSolid", new Vector2(r.center.x, r.yMin + rest * 0.5f), new Vector2(r.width, rest + 0.02f), Vector2.zero, Vector4.zero, false, ZShade, false), shade);
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

            Dress(room, solids, edges, (s, name, slot, centre, size, z) => Add(s, name, slot, centre, size, Vector2.zero, Vector4.zero, false, z, false));

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
                SetupUtility.SetArray(follow, "renderers", pair.Value.Cast<Object>().ToArray(), changes);
                follow.Apply();
            }
            changes.Add($"built {roomRoot.name} environment trims ({edges.Count} exposed edges)");
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
