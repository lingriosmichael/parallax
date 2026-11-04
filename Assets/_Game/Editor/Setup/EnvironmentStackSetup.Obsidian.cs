using System.Collections.Generic;
using System.Linq;
using Parallax.Editor.Art;
using Parallax.Editor.Levels;
using Parallax.Presentation;
using UnityEngine;

namespace Parallax.Editor.Setup
{
    /// <summary>D-281 (the developer: "Make it a premium game, there are middle and front arts"): Obsidian Void's own composer, after
    /// the concept painting: a dark sky and a planet over a bright horizon of mist, ruins standing out of the mist in rows, and the
    /// play stone and the foreground darkest of all. Every painted piece repeats across its layer's whole visible range (Golden
    /// Ruins places each piece once, and these levels are 134 u long). A piece's flat top keeps off the walkable lines (A2), and
    /// pits stay open to the sky (D-253). The front row stands only over fixed stone or outside the room, never over air the cat
    /// can reach or anything it must see. Presentation only.</summary>
    public static partial class EnvironmentStackSetup
    {
        sealed class ObsidianSet
        {
            public string[] Mid, Near; public int Falls;
            public ObsidianSet(int falls, string[] mid, string[] near) { Falls = falls; Mid = mid; Near = near; }
        }

        // Each level's signature: its mid row, its near row, and how many voidfalls pour in its far distance.
        static readonly Dictionary<string, ObsidianSet> ObsidianSets = new()
        {
            ["Citadel"] = new(0, new[] { "ENV_MidCitadel", "ENV_MidArch_0", "ENV_MidMonolith", "ENV_MidTowers" }, new[] { "ENV_MidTree_0", "ENV_MidArch_1" }),
            ["Voidfalls"] = new(3, new[] { "ENV_MidColonnade", "ENV_MidTree_1", "ENV_MidArch_1" }, new[] { "ENV_MidMonolith", "ENV_MidTree_0" }),
            ["Rings"] = new(0, new[] { "ENV_MidArch_1", "ENV_MidCitadel", "ENV_MidArch_1", "ENV_MidMonolith" }, new[] { "ENV_MidTree_1" }),
            ["Crystals"] = new(1, new[] { "ENV_MidTree_0", "ENV_MidTree_1", "ENV_MidMonolith" }, new[] { "ENV_MidTree_1", "ENV_MidArch_0" }),
            ["Colonnade"] = new(0, new[] { "ENV_MidColonnade", "ENV_MidTowers", "ENV_MidColonnade", "ENV_MidArch_0" }, new[] { "ENV_MidMonolith" }),
            ["Span"] = new(2, new[] { "ENV_MidAqueduct", "ENV_MidArch_1", "ENV_MidTowers" }, new[] { "ENV_MidTree_0" }),
            ["Monoliths"] = new(0, new[] { "ENV_MidMonolith", "ENV_MidPillar", "ENV_MidCitadel", "ENV_MidMonolith" }, new[] { "ENV_MidArch_0", "ENV_MidTree_1" }),
        };

        static readonly string[] ObsidianIslands = { "ENV_FarIsland_0", "ENV_FarIsland_1", "ENV_FarIsland_2", "ENV_FarIsland_3" };
        static readonly string[] ObsidianSpires = { "ENV_FarSpire_0", "ENV_FarSpire_1", "ENV_FarSpire_3", "ENV_FarSpire_2" };
        const float RaysAlpha = 0.09f, FogAlpha = 0.46f, NearStep = 14f, LakeDrop = 1.5f;

        /// <summary>What a piece stands on at world x from..to: the lowest walkable top or hazard strip there, or the horizon where neither is
        /// (so ruins never end in mid air over a lake or a pit, whatever the level's lowest ledge).</summary>
        static float Footing(TierContext c, float from, float to)
        {
            SoloRoomDefinition room = c.Room.Value;
            float y = c.Horizon;
            foreach (SoloRoomSkin.Edge top in SoloRoomSkin.WalkableTops(room))
                if (top.To > from && top.From < to) y = Mathf.Min(y, top.Line);
            foreach (SoloRoomElement e in room.Elements)
            {
                if (e.Kind != SoloRoomElementKind.Hazard) continue;
                var r = new Rect(room.Origin + e.Position - e.Size * 0.5f, e.Size);
                if (r.xMax > from && r.xMin < to) y = Mathf.Min(y, r.yMin);
            }
            return y;
        }

        /// <summary>D-309: the level whose last climb stands under the Heart of the Void (Pack G, OG-01).</summary>
        const string FinaleLevel = "L040";

        /// <summary>D-309: the Heart of the Void on the far layer, framed from where the camera comes to rest by the door (the cat there,
        /// about 2.9 u over the view's foot): its ring in the view's upper part, a little back toward the climb, its tail going down
        /// into the mist behind the play. Hazed less than the far skyline, so it's the subject, never a platform.</summary>
        static void VoidHeart(TierContext c, LevelPalettes.Palette p)
        {
            Sprite heart = EnvironmentKit.Sprite("ENV_VoidHeart");
            if (heart == null) return;
            if (!(DoorCamera(c) is Vector2 cam)) { Debug.LogError("EnvironmentStack: L040 has no door, so the Heart of the Void has nowhere to stand."); return; }
            LevelLookConfig.Layer far = c.Config.GetLayer("Far");
            float view = c.Travel.ViewHeight, height = heart.bounds.size.y;
            // (the ring's centre is HeartRing of the way down the piece)
            Vector2 seen = new(cam.x - c.Travel.MaxViewWidth * 0.15f, cam.y + view * 0.22f - (0.5f - HeartRing) * height);
            SpriteRenderer r = c.Place(c.Layer("Far"), "VoidHeart", "ENV_VoidHeart", seen - (1f - far.Speed) * (cam - c.Travel.Mid), Vector2.one,
                Veiled(p.Haze, p.FarHaze * 0.4f, 1f), far.Order + 3, c.Back, false, -0.25f);
            if (r != null) r.gameObject.AddComponent<AmbientMotion>().Configure(0f, 0f, 0.06f, 0.08f, 0f, 0f, 0f, 0f, 0.4f);
        }

        const float HeartRing = 0.27f;

        static Color Veiled(Color haze, float mix, float value, float alpha = 1f)
        {
            Color c = Color.Lerp(Color.white, haze, mix) * value;
            c.a = alpha;
            return c;
        }

        /// <summary>A soft band repeated whole across its layer's visible range (OV-03's mists, OV-06's haze, OV-27's fog).</summary>
        static void BandRow(TierContext c, string layer, string name, string slot, float centreY, float scale, Color tint, string band, int orderOffset, int salt)
        {
            Sprite s = EnvironmentKit.Sprite(slot);
            if (s == null) return;
            LevelLookConfig.Layer def = c.Config.GetLayer(layer);
            var (from, to) = Visible(c, def.Speed, s.bounds.size.x);
            float step = s.bounds.size.x * scale * 0.82f;
            int n = 0;
            for (float x = from + R(c.LevelId, salt) * step; x < to; x += step, n++)
                c.Place(c.Layer(layer), $"{name}_{n}", slot, new Vector2(x, centreY + 0.5f * Mathf.Sin(n * 1.7f + salt)), Vector2.one * scale, tint, def.Order + orderOffset, band, n % 2 == 1, -0.001f * n);
        }

        static void ComposeObsidian(ComposeContext k)
        {
            TierContext c = k.Tier;
            LevelPalettes.Palette p = k.Palette;
            string id = k.LevelId;
            float view = c.Travel.ViewHeight, horizon = c.Horizon;
            ObsidianSet set = ObsidianSets.TryGetValue(k.Signature, out ObsidianSet s) ? s : ObsidianSets["Citadel"];
            HideTemplateAnchors(c);
            float skyTop = c.Frame.max.y + 2f;   // tall levels: the high rows reach the top of the frame

            // Mist across the sky, at three depths.
            string[] clouds = { "CloudsFar", "CloudsMid", "CloudsNear" };
            for (int i = 0; i < 3; i++)
            {
                Color tint = p.Clouds; tint.a = 0.42f + 0.1f * i;
                BandRow(c, clouds[i], "Cloud_" + i, "ENV_Cloud_" + i, horizon + view * (0.34f + 0.1f * i + 0.08f * R(id, 110 + i)), 1.2f + 0.4f * R(id, 120 + i), tint, c.Back, 0, 100 + i);
            }

            // Light falling from the planet's side (OV-28).
            Material rays = AdditiveMaterial("Ray");
            Sprite beam = EnvironmentKit.Sprite("ENV_Ray");
            if (rays != null && beam != null)
            {
                bool flip = c.SunAt.x > c.Travel.Mid.x;
                float scale = view * 1.5f / beam.bounds.size.y;
                for (int i = 0; i < 2; i++)
                {
                    Color tint = Color.Lerp(p.Haze, Color.white, 0.3f); tint.a = RaysAlpha * (i == 1 ? 1f : 0.7f);
                    var at = new Vector2(c.Travel.Mid.x + (i - 0.5f) * view * 1.3f + (R(id, 150 + i) - 0.5f) * view * 0.5f, c.Travel.Mid.y + view * 0.3f);
                    SpriteRenderer r = c.Place(c.Layer("Rays"), "Ray_" + i, "ENV_Ray", at, new Vector2(scale * (0.8f + 0.15f * i), scale), tint, c.Config.GetLayer("Rays").Order, c.Back, flip);
                    if (r != null) { r.sharedMaterial = rays; r.gameObject.AddComponent<AmbientMotion>().Configure(0f, 0f, 0f, 0f, 0.5f, 0.05f, 0.14f, 0.11f, i * 0.31f); }
                }
            }

            // The far city in the haze (OV-41), rising out of the horizon.
            {
                LevelLookConfig.Layer def = c.Config.GetLayer("FarCity");
                Sprite city = EnvironmentKit.Sprite("ENV_CitySkyline");
                var (from, to) = Visible(c, def.Speed, city != null ? city.bounds.size.x * 0.5f : 0f);
                int n = 0;
                if (city != null)
                    for (float x = from; x < to; x += city.bounds.size.x * 0.92f, n++)
                        c.Place(c.Layer("FarCity"), "City_" + n, "ENV_CitySkyline", new Vector2(x, horizon - 0.9f + city.bounds.size.y * 0.5f), Vector2.one, Veiled(p.Haze, 0.5f, 0.62f, 0.9f), def.Order, c.Back, n % 2 == 1, -0.001f * n);
            }

            // Far: spires standing in the mist, monoliths floating over them (OV-04, OV-05), and the voidfalls (OV-08, OV-09).
            {
                LevelLookConfig.Layer def = c.Config.GetLayer("Far");
                var (from, to) = Visible(c, def.Speed, 2f);
                Color tint = Veiled(p.Haze, p.FarHaze * 0.8f, 0.86f);
                float x = from + R(id, 201) * 3f;
                for (int n = 0; x < to && n < 16; n++)
                {
                    string slot = ObsidianSpires[n % ObsidianSpires.Length];
                    Sprite sprite = EnvironmentKit.Sprite(slot);
                    float scale = 0.6f + 0.45f * R(id, 210 + n);
                    Vector2 size = (Vector2)sprite.bounds.size * scale;
                    c.Place(c.Layer("Far"), $"{slot}_far_{n}", slot, new Vector2(x + size.x * 0.5f, horizon - 1.1f + R(id, 230 + n) + size.y * 0.5f), Vector2.one * scale, tint, def.Order, c.Back, R(id, 240 + n) > 0.5f, -0.001f * n);
                    x += size.x + 1.5f + 3.5f * R(id, 250 + n);
                }
                x = from + R(id, 202) * 4f;
                for (int n = 0; x < to && n < 16; n++)
                {
                    string slot = ObsidianIslands[(n + (int)(R(id, 203) * 4f)) % ObsidianIslands.Length];
                    float scale = 0.7f + 0.7f * R(id, 260 + n);
                    Vector2 size = (Vector2)EnvironmentKit.Sprite(slot).bounds.size * scale;
                    float y = horizon + view * (0.38f + 0.32f * R(id, 270 + n));
                    SpriteRenderer r = c.Place(c.Layer("Far"), $"{slot}_far_i{n}", slot, new Vector2(x, y), Vector2.one * scale, Veiled(p.Haze, p.FarHaze * 0.6f, 0.9f), def.Order + 1, c.Back, R(id, 280 + n) > 0.5f, -0.05f - 0.001f * n);
                    if (r != null) r.gameObject.AddComponent<AmbientMotion>().Configure(0f, 0f, 0.08f, 0.1f, 0f, 0f, 0f, 0f, R(id, 290 + n));
                    x += size.x + 3f + 5f * R(id, 295 + n);
                }
                // A tall level's higher storeys look out on more of them.
                int high = 0;
                for (float y = horizon + view * 1.05f; y < skyTop; y += 4.5f)
                    for (float hx = from + R(id, 300 + high) * 8f; hx < to && high < 24; hx += 9f + 6f * R(id, 310 + high), high++)
                    {
                        string slot = ObsidianIslands[high % ObsidianIslands.Length];
                        c.Place(c.Layer("Far"), $"{slot}_high_{high}", slot, new Vector2(hx, y + 2f * R(id, 320 + high)), Vector2.one * (0.8f + 0.6f * R(id, 330 + high)), Veiled(p.Haze, p.FarHaze * 0.7f, 0.88f), def.Order + 1, c.Back, R(id, 340 + high) > 0.5f, -0.1f);
                    }
                Sprite fall = EnvironmentKit.Sprite("ENV_Waterfall"), spray = EnvironmentKit.Sprite("ENV_VoidSpray");
                for (int n = 0; n < set.Falls && fall != null; n++)
                {
                    float fx = Mathf.Lerp(from, to, (n + 0.3f + 0.4f * R(id, 350 + n)) / set.Falls), top = horizon + view * 1.2f, foot = horizon - 0.2f;
                    SpriteRenderer f = c.Place(c.Layer("Far"), "Voidfall_" + n, "ENV_Waterfall", new Vector2(fx, (top + foot) * 0.5f), Vector2.one, Veiled(p.Haze, 0.2f, 0.95f, 0.9f), def.Order + 2, c.Back, n % 2 == 1, -0.2f);
                    if (f == null) continue;
                    f.drawMode = SpriteDrawMode.Tiled; f.tileMode = SpriteTileMode.Continuous; f.size = new Vector2(fall.bounds.size.x * 0.72f, top - foot);
                    if (spray != null) c.Place(c.Layer("Far"), "VoidSpray_" + n, "ENV_VoidSpray", new Vector2(fx, foot + spray.bounds.size.y * 0.32f), Vector2.one * 0.85f, Veiled(p.Haze, 0.2f, 1f, 0.9f), def.Order + 3, c.Back, n % 2 == 1, -0.21f);
                }
            }

            // D-309 (Pack G): Obsidian's last level ends on the Heart of the Void.
            if (id == FinaleLevel) VoidHeart(c, p);

            // The bright horizon: the concept's mist, painted (OV-06) over a soft rise of the palette's haze.
            Gradient(c, "Haze", "FarHaze", horizon - 3f, horizon + view * 0.3f, p.Haze, p.FarHaze * 0.5f, up: true);
            Color hazeBand = p.Haze; hazeBand.a = 0.5f;
            BandRow(c, "Haze", "HazeBand", "ENV_Haze", horizon + 0.8f, 1.1f, hazeBand, c.Back, 3, 400);

            ObsidianRow(c, "Mid", "mid", set.Mid, Veiled(p.Haze, 0.3f, 0.74f), 1f, 3.2f, 500);

            // Fog at the foot of the mid row (OV-27), behind the near row, and down over a lake where the walk runs high above one.
            Color fog = Color.Lerp(p.Haze, Color.white, 0.25f); fog.a = FogAlpha;
            BandRow(c, "Mid", "FogMid", "ENV_Fog", horizon - 0.5f, 1f, fog, c.Middle, 2, 450);
            float lowest = Footing(c, float.NegativeInfinity, float.PositiveInfinity);
            if (lowest < horizon - LakeDrop) BandRow(c, "Mid", "FogLake", "ENV_Fog", lowest + 1f, 1.1f, fog, c.Middle, 2, 460);

            ObsidianRow(c, "Near", "near", set.Near, Veiled(p.Haze, 0.48f, 0.58f), 1f, NearStep, 600);

            // Fog at the walk line, in front of the near row and behind the play layer: the black cat stands against it.
            Color near = fog; near.a = FogAlpha * 0.7f;
            BandRow(c, "Atmosphere", "FogNear", "ENV_Fog", horizon - 0.7f, 0.8f, near, c.Middle, 1, 470);

            Lights(k);
        }

        /// <summary>A row of whole pieces standing out of the fog, across its layer's visible range. `gap`: the least space between two.</summary>
        static void ObsidianRow(TierContext c, string layer, string tag, string[] set, Color tint, float scale, float gap, int salt)
        {
            LevelLookConfig.Layer def = c.Config.GetLayer(layer);
            if (def == null) return;
            string id = c.LevelId;
            SoloRoomDefinition room = c.Room.Value;
            List<SoloRoomSkin.Edge> tops = SoloRoomSkin.WalkableTops(room);
            var (from, to) = Visible(c, def.Speed, 1f);
            // The door and the portals keep a clear ground behind them (A6), wherever the camera shows them.
            List<Rect> keep = room.Elements.Where(e => e.Kind is SoloRoomElementKind.Door or SoloRoomElementKind.Portal)
                .Select(e => new Rect(room.Origin + e.Position - e.Size * 0.5f - Vector2.one, e.Size + Vector2.one * 2f)).ToList();
            float x = from + R(id, salt) * gap;
            for (int n = 0; x < to && n < 24; n++)
            {
                string slot = set[(n + (int)(R(id, salt + 1) * set.Length)) % set.Length];
                Sprite sprite = EnvironmentKit.Sprite(slot);
                if (sprite == null) continue;
                Vector2 size = (Vector2)sprite.bounds.size * scale;
                // Where the walk is when the camera comes to this piece: a layer at `speed` shows it mid-screen there.
                float seen = c.Travel.Mid.x + (x + size.x * 0.5f - c.Travel.Mid.x) / def.Speed;
                var bottom = new Vector2(x + size.x * 0.5f, Footing(c, seen - size.x, seen + size.x) - 1.3f - 0.8f * R(id, salt + 10 + n));
                x += size.x + gap * (0.8f + 0.7f * R(id, salt + 40 + n));
                if (!FindLegalBase(slot, def.Speed, bottom, size, tops, c.Travel, out float legal)) { c.Changes.Add($"left out {tag} {slot} (A2)"); continue; }
                bottom.y = legal;
                Vector2 seenAt = bottom + new Vector2(0f, size.y * 0.5f);
                var box = new Rect(seenAt - size * 0.5f, size);
                if (layer == "Near" && keep.Any(d => Mathf.Abs(d.center.x - box.center.x) < (d.width + box.width) * 0.5f + (1f - def.Speed) * 0.5f * c.Travel.MaxViewWidth)) continue;
                c.Place(c.Layer(layer), $"{slot}_{tag}_{n}", slot, seenAt, Vector2.one * scale, tint, def.Order, c.Middle, R(id, salt + 70 + n) > 0.5f, -0.001f * n);
            }
        }

        // ---------- the front row ----------

        const float FrontLipClear = 0.5f, FrontWallGap = 3.1f, FrontBlockGap = 0.3f, GoldenHangScale = 0.55f;
        static readonly string[] FrontFoot = { "ENV_FG_Ferns", "ENV_FG_Roots", "ENV_FG_Branch" };
        static readonly Color FrontTint = new(0.8f, 0.88f, 1f, 1f);

        /// <summary>The paintings in front of the play layer, in the world (never pinned to the view): hanging over the stone of the roof
        /// and standing beyond the room's end walls, with, in Obsidian, OV-26's crystals over the stone under the walk (Golden Ruins
        /// has its foot ferns, D-282: the developer's "these front ferns everywhere, I don't like it" stands). Each stands where the camera
        /// shows it fully, passes ForegroundClear, and has no element behind it, so it never hides a trap, a tell or the cat.</summary>
        static void ForegroundTier(TierContext c)
        {
            SoloRoomDefinition room = c.Room.Value;
            string id = c.LevelId;
            bool obsidian = EnvironmentKit.Current == Biome.ObsidianVoid;
            LevelLookConfig.Layer def = c.Config.GetLayer("Foreground");
            Color tint = obsidian ? FrontTint : Color.white;
            List<SoloRoomSkin.Solid> solids = SoloRoomSkin.Solids(room);
            List<Rect> blockers = room.Elements.Where(e => e.Kind is not (SoloRoomElementKind.Floor or SoloRoomElementKind.Ceiling or SoloRoomElementKind.Wall))
                .Select(e => new Rect(room.Origin + e.Position - e.Size * 0.5f - Vector2.one * FrontBlockGap, e.Size + Vector2.one * (2f * FrontBlockGap))).ToList();
            // Where it must stand when the camera comes to it, back to its place at mid travel.
            Vector2 AtMid(Vector2 centre)
            {
                var camera = new Vector2(Mathf.Clamp(centre.x, c.Travel.Min.x, c.Travel.Max.x), Mathf.Clamp(centre.y, c.Travel.Min.y, c.Travel.Max.y));
                return centre - (1f - def.Speed) * (camera - c.Travel.Mid);
            }
            bool Fits(Vector2 centre, Vector2 size)
            {
                var box = new Rect(centre - size * 0.5f, size);
                return !blockers.Any(b => b.Overlaps(box)) && ForegroundClear(new Rect(AtMid(centre) - size * 0.5f, size), def.Speed, room, c.Travel);
            }
            int placed = 0;
            void Put(string slot, Vector2 centre, float scale, bool flip) =>
                c.Place(c.Layer("Foreground"), $"Front_{placed++}", slot, AtMid(centre), Vector2.one * scale, tint, def.Order, c.Front, flip, -0.001f * placed);
            // Over the stone under the walk.
            if (obsidian)
                foreach (SoloRoomSkin.Edge e in SoloRoomSkin.WalkableTops(room).Where(t => !t.Owner.Trap && t.Length >= 5f))
                    for (float x = e.From + 1f + 4f * R(id, Mathf.RoundToInt(e.From * 10f) + 800); x < e.To - 1f; )
                    {
                        int i = Mathf.RoundToInt(x * 10f);
                        string slot = FrontFoot[(int)(R(id, i + 810) * FrontFoot.Length)];
                        Sprite s = EnvironmentKit.Sprite(slot);
                        if (s == null) break;
                        float scale = 0.8f + 0.45f * R(id, i + 820);
                        Vector2 size = (Vector2)s.bounds.size * scale;
                        Vector2 centre = new(x, e.Line - FrontLipClear - size.y * 0.5f);
                        if (Fits(centre, size)) { Put(slot, centre, scale, R(id, i + 830) > 0.5f); x += size.x + 7f + 8f * R(id, i + 840); }
                        else x += 2f;
                    }
            // Hanging over the stone of the roof.
            string hangSlot = obsidian ? "ENV_FG_Shards" : "ENV_FrameHang";
            Sprite hang = EnvironmentKit.Sprite(hangSlot);
            if (hang != null)
                foreach (SoloRoomSkin.Solid roof in solids.Where(s => !s.Trap && s.Kind == SoloRoomElementKind.Ceiling && s.Rect.width >= 6f))
                    for (float x = roof.Rect.xMin + 2f + 5f * R(id, Mathf.RoundToInt(roof.Rect.xMin) + 850); x < roof.Rect.xMax - 2f; )
                    {
                        int i = Mathf.RoundToInt(x * 10f);
                        float scale = (obsidian ? 0.7f : GoldenHangScale) + (obsidian ? 0.4f : 0.2f) * R(id, i + 860);
                        Vector2 size = (Vector2)hang.bounds.size * scale;
                        Vector2 centre = new(x, roof.Rect.yMin + FrontLipClear + size.y * 0.5f);
                        if (Fits(centre, size)) { Put(hangSlot, centre, scale, R(id, i + 870) > 0.5f); x += size.x + 9f + 9f * R(id, i + 880); }
                        else x += 2f;
                    }
            // The monolith or great trunk beyond each end wall (outside the room, and the camera stops short of showing it any nearer).
            Sprite trunk = EnvironmentKit.Sprite("ENV_FG_Trunk");
            if (trunk != null)
                foreach (bool right in new[] { false, true })
                {
                    float x = right ? room.Origin.x + room.Width + FrontWallGap : room.Origin.x - FrontWallGap;
                    Put("ENV_FG_Trunk", new Vector2(x, c.Horizon + trunk.bounds.size.y * 0.2f), 1f, right);
                }
        }

        const float RingTouch = 0.2f;   // a ring's outer stones may touch a wall by this much

        /// <summary>D-281's rules, for ObsidianLookTests: each level has an Obsidian palette and a row, no two levels the same pair, and
        /// no portal ring pierces fixed stone anywhere its end stands (no art through what should hide it).</summary>
        public static List<string> ObsidianLookProblems()
        {
            var problems = new List<string>();
            var pairs = new HashSet<string>();
            string last = null;
            for (int n = Biomes.ObsidianFirst; n <= Biomes.ObsidianFirst + 9; n++)
            {
                string id = $"L{n:000}", palette = LevelPalettes.For(id).Name, row = LevelPalettes.Signature(id);
                if (!palette.StartsWith("Void")) problems.Add($"{id}: palette {palette}");
                if (!ObsidianSets.ContainsKey(row)) problems.Add($"{id}: row {row}");
                if (!pairs.Add(palette + "/" + row)) problems.Add($"{id}: repeats {palette}/{row}");
                if (palette == last) problems.Add($"{id}: same palette as the level before");
                last = palette;
            }
            foreach (SoloRoomDefinition room in LevelLayouts.ById.Values)
            {
                List<SoloRoomSkin.Solid> stone = SoloRoomSkin.Solids(room).Where(s => !s.Trap).ToList();
                foreach (SoloRoomElement e in room.Elements.Where(x => x.Kind == SoloRoomElementKind.Portal))
                {
                    void Check(Vector2 centre, Vector2 size)
                    {
                        Vector2 drawn = TrapKitSetup.PortalRingDrawn(size);
                        var ring = new Rect(room.Origin + centre - drawn * 0.5f + Vector2.one * RingTouch, drawn - Vector2.one * (2f * RingTouch));
                        foreach (SoloRoomSkin.Solid s in stone.Where(s => s.Rect.Overlaps(ring))) problems.Add($"{e.Name} at {centre}: its ring goes into {s.Name}");
                    }
                    Check(e.Position, e.Size);
                    Check(e.SecondaryPosition, e.SecondarySize);
                    if (e.Settings.Path.HasPoints) foreach (Vector2 p in e.Settings.Path.Points) Check(e.SecondaryPosition + p, e.SecondarySize);
                }
            }
            return problems;
        }

        /// <summary>The template's co-op sandbox anchors (Vine_A, Plate_A: Golden Ruins art at fixed world positions, D-274) stay in the
        /// level, working and untouched; a level only doesn't draw them (Obsidian; D-310: Golden Ruins too).</summary>
        static void HideTemplateAnchors(TierContext c)
        {
            Transform anchors = c.Root.transform.Find("Anchors");
            if (anchors == null) return;
            foreach (SpriteRenderer r in anchors.GetComponentsInChildren<SpriteRenderer>(true))
                if (r.enabled) { r.enabled = false; c.Changes.Add("hid template anchor art " + r.name); }
        }
    }
}
