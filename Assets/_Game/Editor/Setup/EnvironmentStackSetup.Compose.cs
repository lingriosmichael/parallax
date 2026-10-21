using System.Collections.Generic;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Art;
using Parallax.Editor.Levels;
using Parallax.Gameplay.Cameras;
using Parallax.Gameplay.Reality;
using Parallax.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Parallax.Editor.Setup
{
    /// <summary>PAX-A16 gauntlet (the developer's brief): the composer. Every background element is a whole piece placed once
    /// (no band repeats across the view): clouds, the far skyline, the level's signature at mid distance, haze between the
    /// depths (aerial perspective), low fog and the water. Placement is seeded by the level's id, so a rebuild reproduces it
    /// and no two levels share a skyline. A piece goes only where its layer can show it over the camera's travel, and A2
    /// (FindLegalBase) still keeps flat tops off the walkable lines. Then the 2D lights: the sun's backlight following the
    /// camera and the cat's key light following the cat. Presentation only; nothing here touches a collider or a tick.</summary>
    public static partial class EnvironmentStackSetup
    {
        public sealed class ComposeContext
        {
            public TierContext Tier; public LevelPalettes.Palette Palette; public string LevelId, Signature;
            public System.Func<string, Color> Tint;
        }

        // A_BG_01's cluster is out (triage: low resolution, magenta baked into its mist).
        static readonly string[] FarSet = { "ENV_FarSpire_0", "ENV_FarIsland_1", "ENV_FarSpire_2", "ENV_FarIsland_2", "ENV_FarSpire_1", "ENV_FarIsland_3", "ENV_FarSpire_3", "ENV_FarIsland_0" };

        static readonly Dictionary<string, string[]> Signatures = new()
        {
            ["Aqueduct"] = new[] { "ENV_MidAqueduct", "ENV_MidTree_0" },
            ["Ruins"] = new[] { "ENV_MidRuins" },   // 2026-10-18: no ENV_MidPillar (it read as a platform; see NearPalette)
            ["Colonnade"] = new[] { "ENV_MidColonnade", "ENV_MidArch_1", "ENV_MidTree_1" },
            ["Towers"] = new[] { "ENV_MidTowers", "ENV_MidArch_0" },
            ["GreatTree"] = new[] { "ENV_MidTree_1", "ENV_MidRuins" },
            ["Temple"] = new[] { "ENV_MidRuins", "ENV_MidColonnade" },
            ["Waterfalls"] = new[] { "ENV_MidArch_1", "ENV_MidTowers" },
            ["FloatingIsles"] = new[] { "ENV_MidArch_0", "ENV_MidTowers" },
        };

        /// <summary>Round 2: the developer's layered city (ENV-41) stands in the middle of the far skyline in these levels (day
        /// and storm light, where its warm stone reads); the far pieces keep clear of it.</summary>
        static readonly HashSet<string> CityLevels = new() { "L004", "L007", "L011", "L016", "L019" };

        /// <summary>The paintings a level's signature shows at mid distance (the near tier skips them: no painting twice).</summary>
        static HashSet<string> SignatureSlots(string levelId) =>
            new(Signatures.TryGetValue(LevelPalettes.Signature(levelId), out string[] s) ? s : Signatures["Ruins"]);

        /// <summary>Round 5: how far the frame must open under the lowest floor before a lower row of the signature stands there.</summary>
        const float UnderRowOpen = 3f;

        /// <summary>Round 6: the lower row's tops (world y at mid travel) in levels the open-below rule can't read: L016 walks
        /// three corridors over sky (PAX-107 D-142: the row stands behind corridors 1 and 2); L019 (PAX-107 D-149) has L016's frame (its row
        /// stands behind corridors 1 and 2 too).</summary>
        static readonly Dictionary<string, float> UnderRowTop = new() { ["L016"] = 8f, ["L019"] = 8f };

        /// <summary>Round 2: how far the frame must open under the lowest floor before the far lake shows.</summary>
        const float OpenBelow = 3.5f;

        static float R(string id, int salt) => SoloRoomSkin.Hash(id ?? "template", salt);

        /// <summary>The x range (at mid travel) a layer at `speed` can show over the camera's travel, plus a margin.</summary>
        static (float from, float to) Visible(TierContext c, float speed, float margin = 1f)
        {
            float half = c.Travel.MaxViewWidth * 0.5f, travel = (c.Travel.Max.x - c.Travel.Min.x) * 0.5f;
            float reach = half + speed * travel + margin;
            return (c.Travel.Mid.x - reach, c.Travel.Mid.x + reach);
        }

        static void Compose(ComposeContext k)
        {
            TierContext c = k.Tier;
            LevelPalettes.Palette p = k.Palette;
            string id = k.LevelId;
            float view = c.Travel.ViewHeight;

            // Clouds: three whole cloud banks, soft enough to scale, at three depths.
            string[] cloudLayers = { "CloudsFar", "CloudsMid", "CloudsNear" };
            for (int i = 0; i < 3; i++)
            {
                LevelLookConfig.Layer def = c.Config.GetLayer(cloudLayers[i]);
                var (from, to) = Visible(c, def.Speed);
                float x = Mathf.Lerp(from, to, 0.15f + 0.7f * R(id, 100 + i));
                float y = c.Horizon + view * (0.18f + 0.14f * i + 0.08f * R(id, 110 + i));
                float s = 1.3f + 0.6f * R(id, 120 + i);
                Color tint = p.Clouds; tint.a = 0.55f + 0.15f * i;
                c.Place(c.Layer(cloudLayers[i]), "Cloud_" + i, "ENV_Cloud_" + ((i + (int)(R(id, 130) * 3)) % 3), new Vector2(x, y), Vector2.one * s, tint, def.Order, c.Back, R(id, 140 + i) > 0.5f);
            }

            // The far skyline: whole pieces, never larger than baked, swallowed by the haze toward the horizon.
            {
                LevelLookConfig.Layer def = c.Config.GetLayer("Far");
                var (from, to) = Visible(c, def.Speed, 2f);
                Color tint = Color.Lerp(Color.white, p.Haze, p.FarHaze) * 0.98f; tint.a = 1f;
                // Round 2: the city, whole and at its own size, centred in the skyline; its foot under the horizon's haze.
                float cityFrom = float.MaxValue, cityTo = float.MinValue;
                Sprite city = CityLevels.Contains(id ?? "") ? EnvironmentKit.Sprite("ENV_CitySkyline") : null;
                if (city != null)
                {
                    Vector2 cs = city.bounds.size;
                    float cx = Mathf.Lerp(from, to, 0.4f + 0.2f * R(id, 205));
                    cityFrom = cx - cs.x * 0.5f; cityTo = cx + cs.x * 0.5f;
                    c.Place(c.Layer("Far"), "City_far", "ENV_CitySkyline", new Vector2(cx, c.Horizon - 1.4f + cs.y * 0.5f), Vector2.one, tint, def.Order, c.Back, R(id, 206) > 0.5f, 0.001f);
                }
                int start = (int)(R(id, 200) * FarSet.Length);
                float x = from + R(id, 201) * 2f;
                for (int n = 0; x < to && n < FarSet.Length; n++)   // each far piece once
                {
                    string slot = FarSet[(start + n) % FarSet.Length];
                    Sprite sprite = EnvironmentKit.Sprite(slot);
                    if (sprite == null) continue;
                    float s = 0.55f + 0.4f * R(id, 210 + n);
                    Vector2 size = (Vector2)sprite.bounds.size * s;
                    if (x + size.x > cityFrom && x < cityTo) { x = cityTo + 0.8f; continue; }   // round 2: clear of the city
                    bool island = slot.Contains("Island") || slot.Contains("Cluster");
                    float baseY = c.Horizon + (island ? 1.5f + 2.5f * R(id, 220 + n) : -1.2f + 1.0f * R(id, 230 + n));
                    c.Place(c.Layer("Far"), $"{slot}_far_{n}", slot, new Vector2(x + size.x * 0.5f, baseY + size.y * 0.5f), Vector2.one * s, tint, def.Order, c.Back, R(id, 240 + n) > 0.5f, -0.001f * n);
                    x += size.x * (0.6f + 0.5f * R(id, 250 + n)) + 0.8f;
                }
            }

            // Haze over the far skyline: the palette's haze rising from below the horizon, thinning upward (no texture).
            Gradient(c, "Haze", "FarHaze", c.Horizon - 3f, c.Horizon + view * 0.42f, p.Haze, p.FarHaze * 0.85f, up: true);

            // The signature at mid distance: its pieces, whole, standing from below the ground line.
            {
                LevelLookConfig.Layer def = c.Config.GetLayer("Mid");
                var (from, to) = Visible(c, def.Speed, 0f);
                string[] set = Signatures.TryGetValue(k.Signature, out string[] s) ? s : Signatures["Ruins"];
                List<SoloRoomSkin.Edge> tops = c.Room.HasValue ? SoloRoomSkin.WalkableTops(c.Room.Value) : new List<SoloRoomSkin.Edge>();
                // P1-R4: the signature is the mid plane's subject: lightly hazed, standing well above the walk line.
                Color tint = Color.Lerp(Color.white, p.Haze, p.FarHaze * 0.55f) * 0.95f; tint.a = 1f;   // back enough not to read as a platform
                float span = to - from, x = from + span * (0.05f + 0.15f * R(id, 300));
                // Round 3 (critics: "lone platforms against a sky card"; at the L001 zoom a view is a third of a level, so two
                // pieces left most views empty): the set runs twice across the visible range, the repeat a level apart.
                for (int n = 0; n < 2 * set.Length && x < to; n++)
                {
                    string slot = set[n % set.Length];
                    Sprite sprite = EnvironmentKit.Sprite(slot);
                    if (sprite == null) continue;
                    Vector2 size = sprite.bounds.size;
                    var bottom = new Vector2(x + size.x * 0.5f, c.Horizon - 1.2f - 0.8f * R(id, 310 + n));
                    x += size.x * (0.75f + 0.35f * R(id, 320 + n)) + 1.5f;
                    if (!FindLegalBase(slot, def.Speed, bottom, size, tops, c.Travel, out float legal)) { c.Changes.Add($"left out mid {slot} (A2)"); continue; }
                    bottom.y = legal;
                    SpriteRenderer r = c.Place(c.Layer("Mid"), $"{slot}_mid_{n}", slot, bottom + new Vector2(0f, size.y * 0.5f), Vector2.one, tint, def.Order, c.Middle, (R(id, 330 + n % set.Length) > 0.5f) ^ (n >= set.Length), -0.001f * n);
                    if (slot.Contains("Tree") && r != null) r.gameObject.AddComponent<AmbientMotion>().Configure(0f, 0f, 0f, 0f, 0.4f, 0.08f, 0f, 0f, R(id, 340 + n));
                }
                // Round 5 (critics: "the lower 40% is empty sky", L016/L019/L005): where the frame opens UnderRowOpen under the
                // lowest floor, a second, hazier row of the signature rises from below the frame, its tops a unit under that floor.
                float lowestTop = tops.Count > 0 ? tops.Min(t => t.Line) : c.Frame.min.y;
                // Round 6: levels whose walk runs high over open air with a floor far below (the flip level's beds, the pits)
                // set the row's tops by hand.
                bool handSet = UnderRowTop.TryGetValue(id ?? "", out float setTop);
                if (handSet) lowestTop = setTop + 1f;
                if (handSet || lowestTop - c.Frame.min.y >= UnderRowOpen)
                {
                    Color under = Color.Lerp(Color.white, p.Haze, Mathf.Clamp(p.FarHaze * 0.9f, 0.5f, 0.85f)); under.a = 1f;
                    float ux = from + span * (0.12f + 0.2f * R(id, 350));
                    for (int n = 0; n < 12 && ux < to; n++)   // across the whole visible range (L019 is 64 u wide)
                    {
                        string slot = set[(n + 1) % set.Length];
                        Sprite sprite = EnvironmentKit.Sprite(slot);
                        if (sprite == null) continue;
                        Vector2 size = sprite.bounds.size;
                        var bottom = new Vector2(ux + size.x * 0.5f, lowestTop - 1f - size.y);
                        ux += size.x * (0.9f + 0.5f * R(id, 360 + n)) + 3f;
                        if (!FindLegalBase(slot, def.Speed, bottom, size, tops, c.Travel, out float legal)) { c.Changes.Add($"left out under-row {slot} (A2)"); continue; }
                        bottom.y = legal;
                        c.Place(c.Layer("Mid"), $"{slot}_under_{n}", slot, bottom + new Vector2(0f, size.y * 0.5f), Vector2.one, under, def.Order - 1, c.Middle, R(id, 370 + n) > 0.5f, 0.001f * n);
                    }
                }
                // Round 5: the signature adds no waterfalls of its own (critics, L013: "they pour onto and behind walkable
                // floors", the water rule); a level's own look still places them where it clears its floors.
            }

            // A pale veil in front of the mid distance, behind the near tier: a third step of distance.
            Gradient(c, "Mid", "MidVeil", c.Horizon - 2f, c.Horizon + view * 0.4f, p.Haze, p.FarHaze * 0.25f, up: true);
            // Mist rising behind the walk line: the floor's lip stands against haze, not hard against the sky.
            // Phase 2 round 2: thinner (the lower halves read milky, with no darks).
            Gradient(c, "Atmosphere", "MidMist", c.Horizon - 1.2f, c.Horizon + 2.2f, p.Haze, Mathf.Min(0.5f, p.Fog * 0.85f), up: true);

            // Round 2 (the developer: "water only goes where nothing walkable sits on it"): no water plane across the whole
            // frame (floors stood on it, L020). The developer's water ruins (ENV-40) stand on the far layer only where the frame
            // opens at least OpenBelow under the lowest floor, their waterline down there, so every floor stands well above it.
            {
                float lowest = c.Room.HasValue ? SoloRoomSkin.WalkableTops(c.Room.Value).Select(t => t.Line + c.Room.Value.Origin.y).DefaultIfEmpty(c.Frame.min.y).Min() : c.Frame.min.y;
                string lakeSlot = "ENV_FarLake_" + (int)(R(id, 260) * 2f);
                // Round 3: the waterline also sits half a unit under every solid's foot (a pit wall or a floor's thickness read
                // as standing in the lake), and the frame still has to open OpenBelow − 1 under it.
                float deepest = c.Room.HasValue ? SoloRoomSkin.Solids(c.Room.Value).Select(s => s.Rect.yMin).DefaultIfEmpty(lowest).Min() : lowest;
                float waterTop = Mathf.Min(lowest - 1f, deepest - 0.5f);
                Sprite lake = waterTop - c.Frame.min.y >= OpenBelow - 1f ? EnvironmentKit.Sprite(lakeSlot) : null;
                if (lake != null)
                {
                    LevelLookConfig.Layer def = c.Config.GetLayer("Far");
                    var (from, to) = Visible(c, def.Speed, 0f);
                    Vector2 size = lake.bounds.size;
                    Color tint = Color.Lerp(Color.white, p.Haze, p.FarHaze * 0.6f); tint.a = 1f;
                    // The water is the piece's lower quarter, its top at waterTop.
                    float y = waterTop - size.y * 0.25f + size.y * 0.5f;
                    c.Place(c.Layer("Far"), "Lake_far", lakeSlot, new Vector2(Mathf.Lerp(from, to, 0.3f + 0.4f * R(id, 261)), y), Vector2.one, tint, def.Order, c.Back, R(id, 262) > 0.5f, 0.002f);
                }
            }

            // A low warm mist over the bottom of the view (the wall's foot): one quad on the camera, in front of the play layer,
            // too faint to hide anything (alpha 0.22 at the frame's edge, clear by a fifth of the view up).
            {
                Sprite fade = EnvironmentKit.Sprite("ENV_SkyFade");
                if (fade != null)
                {
                    var go = new GameObject("FootMist");
                    go.transform.SetParent(c.Camera.transform, false);
                    go.layer = c.LayerIndex;
                    go.transform.localPosition = new Vector3(0f, -view * 0.5f + view * 0.11f, 10f);
                    go.transform.localScale = new Vector3((c.Travel.MaxViewWidth + 2f) / fade.bounds.size.x, view * 0.22f / fade.bounds.size.y, 1f);
                    var r = go.AddComponent<SpriteRenderer>();
                    r.sprite = fade; r.sharedMaterial = EnvironmentKit.UnlitMaterial; r.flipY = true;
                    Color warm = Color.Lerp(p.Haze, p.SunLight, 0.4f); warm.a = 0.06f; r.color = warm;
                    r.sortingLayerName = c.Front; r.sortingOrder = c.Config.GetLayer("Foreground").Order + 1;
                }
            }

            PitVoids(c);
            Lights(k);
        }

        /// <summary>The developer (2026-10-16): the back wall's shading: the shadow down each side wall (its
        /// strength, 0..1, and its reach in world units). (Its faint darkening toward the bottom went for the draw budget.)</summary>
        const float SideShade = 0.3f, SideReach = 0.4f;
        /// <summary>The shadow down a pit's back wall at its hazard's line and under it (0..1).</summary>
        const float DepthShade = 0.8f;   // 0.62 barely showed on L010's chasm

        /// <summary>The recessed stone's tint. 2026-10-17 visual review: the shadow is in the recess texture itself (env_kit.py
        /// RECESS_MEAN: the stone's hue and texture, just under a lit face); a tint here (0.44) flattened it to felt.</summary>
        static readonly Color VoidTint = Color.white;

        // The developer (2026-10-16): "you shouldnt be able to see the sky if the floor caves in". Behind every pit (an opening
        // whose hazard lines its bottom) the inside of the ruin: an opaque back wall of the room's own stone, recessed (its
        // fill's _Recess twin: softer, lighter, warmer), shaded under the lip and down both walls. Unlit, so
        // every pit is the same mid brown whatever the light there: dark iron spikes read against it at 3:1 (D-108); lit, it
        // fell between what the iron and its bright edges can read against (luminance 0.09-0.13, t22).
        static void PitVoids(TierContext c)
        {
            SoloRoomDefinition room = c.Room.Value;
            Sprite fill = SoloRoomSkin.FillSprite(room);
            Sprite wall = fill != null ? EnvironmentKit.Sprite(fill.name + "_Recess") : null, ramp = EnvironmentKit.Sprite("ENV_FaceShade"), solidShade = EnvironmentKit.Sprite("ENV_FaceShadeSolid");
            Material stone = EnvironmentKit.UnlitMaterial, m = EnvironmentKit.TileMaterial("Sprite") ?? EnvironmentKit.UnlitMaterial;
            if (wall == null || ramp == null || stone == null) { Debug.LogWarning("EnvironmentStack: no recessed stone (ENV_Fill_*_Recess) in the kit; pits keep the sky behind them. Run Tools/Art/env_kit.py and let Unity import the kit, then rebuild."); return; }
            string gameplay = RealitySpace.SortingLayerName(c.Root.Id, SortingBand.Gameplay);
            string lipSlot = fill != null && fill.name.StartsWith("ENV_Fill_") ? "ENV_RecessLip_" + fill.name.Substring("ENV_Fill_".Length) : null;
            Sprite farLip = lipSlot != null ? EnvironmentKit.Sprite(lipSlot) : null;
            EnvironmentKit.Slot farLipSlot = lipSlot != null ? EnvironmentKit.Get(lipSlot) : null;
            List<SoloRoomSkin.Solid> solids = SoloRoomSkin.Solids(room);
            int n = 0;
            // 2026-10-19 (the developer: "if it's part of the ceiling or floor you see the background of the floor or ceiling, not
            // the sky ... make it even brighter so you can tell the difference"): a veil of the level's haze over every back wall,
            // so a hole into the ruin reads lighter and further off than the stone around it.
            Sprite white = EnvironmentKit.Sprite("ENV_White");
            void Veil(string name, Vector3 at, Vector2 size)
            {
                if (white == null) return;
                var go = new GameObject(name);
                go.transform.SetParent(c.EnvRoot, false);
                go.layer = c.LayerIndex;
                go.transform.position = new Vector3(at.x, at.y, c.EnvRoot.position.z - 0.0002f);
                go.transform.localScale = new Vector3(size.x / white.bounds.size.x, size.y / white.bounds.size.y, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = white; sr.sharedMaterial = stone; sr.sortingLayerName = gameplay; sr.sortingOrder = -4;
                // A warm stone light leaning toward the level's haze: over the recess's darker stone, a cool haze alone flattened
                // it to grey (L004's Roof_4).
                Color haze = Color.Lerp(c.Grade.Haze, VeilLight, 0.65f);
                sr.color = new Color(haze.r, haze.g, haze.b, BackVeil);
            }
            // PAX-107: or the hidden spikes a pit opening names as its hazard (L007's shaft 1: its spikes are T1's trigger).
            // 2026-10-18 visual review (L015-L019: "every pit in this level is a hole into the sky"): also any spike strip lying in a
            // pit by its shape, a floor at each side of it reaching higher than it and down past it (band 2's pits aren't
            // declared as openings).
            bool InPit(SoloRoomElement x)
            {
                if (x.Kind is not (SoloRoomElementKind.Hazard or SoloRoomElementKind.HiddenSpikes)) return false;
                var hr = new Rect(room.Origin + x.Position - x.Size * 0.5f, x.Size);
                bool Side(bool left) => solids.Any(o => !o.Trap && Mathf.Abs((left ? o.Rect.xMax : o.Rect.xMin) - (left ? hr.xMin : hr.xMax)) < 0.3f
                    && o.Rect.yMax >= hr.yMax + 0.3f && o.Rect.yMax <= hr.yMax + 6f && o.Rect.yMin <= hr.yMin + 0.05f);
                // (never spikes hung flush under stone, in a ceiling's recess: L016/L020's Spikes_CC*, whose "pit" ran down to the
                // floor below as a dark column)
                bool hung = solids.Any(o => Mathf.Abs(o.Rect.yMin - hr.yMax) < 0.1f && Mathf.Min(o.Rect.xMax, hr.xMax) - Mathf.Max(o.Rect.xMin, hr.xMin) > 0.5f * hr.width);
                // (nor a strip hidden inside a solid, such as L004's RecessC_Hazard inside the rising Sink_R: its "pit" ran down
                // through the open roof walk to the ground as a dark column; 2026-10-19, the developer: "it used to be clean")
                bool inside = room.Elements.Any(o => o.Kind is SoloRoomElementKind.Floor or SoloRoomElementKind.Ceiling or SoloRoomElementKind.Wall
                    or SoloRoomElementKind.MovingTrap or SoloRoomElementKind.FallingBlock or SoloRoomElementKind.CollapsingFloor
                    && new Rect(room.Origin + o.Position - o.Size * 0.5f, o.Size).Contains(hr.center));
                return hr.width > hr.height && !hung && !inside && Side(true) && Side(false);
            }
            foreach (SoloRoomElement e in room.Elements.Where(x => (x.Kind == SoloRoomElementKind.Hazard && x.HazardRole == SoloRoomHazardRole.OpeningBottom)
                || (x.Kind == SoloRoomElementKind.HiddenSpikes && (room.Openings ?? System.Array.Empty<SoloRoomOpening>()).Any(o => o.Kind == SoloRoomOpeningKind.Pit && o.HazardName == x.Name))
                || InPit(x)))
            {
                var r = new Rect(room.Origin + e.Position - e.Size * 0.5f, e.Size);
                // The pit's lip: the lower of the floors at its two sides (a tall neighbour must not pull the wall up the sky).
                float top = solids.Where(s => s.Rect.yMax > r.yMax && s.Rect.yMin < r.yMax + 0.5f && (Mathf.Abs(s.Rect.xMax - r.xMin) < 0.3f || Mathf.Abs(s.Rect.xMin - r.xMax) < 0.3f)).Select(s => s.Rect.yMax).DefaultIfEmpty(r.yMax + 2f).Min();
                // Down to the pit's own floor (its kill plane), never past it into open air below a raised floor (L012). 2026-10-16: a
                // pit with nothing under its kill plane (L010's chasm) goes on down to the next solid, or past the room's bottom,
                // so its back wall never ends in a straight edge over the air.
                float bottom = r.yMin;
                if (!solids.Any(s => Mathf.Abs(s.Rect.yMax - r.yMin) < 0.05f && s.Rect.xMin < r.xMax && s.Rect.xMax > r.xMin))
                {
                    float below = solids.Where(s => s.Rect.yMax < r.yMin && s.Rect.xMin < r.xMax && s.Rect.xMax > r.xMin).Select(s => s.Rect.yMax).DefaultIfEmpty(float.NegativeInfinity).Max();
                    bottom = float.IsNegativeInfinity(below) ? SoloRoomBuilder.ComputeRoomBounds(room, 0f).min.y - 1f : below;
                }
                float height = top - bottom;
                if (height <= 0.01f) continue;
                // 2026-10-17 visual review (L003's spike corridor: "an opaque band runs the whole corridor length ... a regular
                // wavy cardboard-mountain line"): a shallow trench along a floating slab is a spike bed under the sky, not a hole
                // into the ruin; no back wall there (the bed's spikes read against the sky like any other row).
                // (2026-10-18, L018: unless something covers it, a trench inside a slab under its cover is a recess)
                bool coveredOver = solids.Any(o => o.Rect.yMin >= top - 0.05f && o.Rect.yMin <= top + 1.5f && o.Rect.xMin <= r.xMin + 0.05f && o.Rect.xMax >= r.xMax - 0.05f);
                if (height < ShallowTrench && r.width > Mathf.Max(3f * height, 6f) && !coveredOver) continue;   // (a long corridor bed, L003's; L018's 4 u briar keeps its wall)
                // PAX-A18 W3: as wide as the walls' broken outline reaches into them (DeepReach each side; the stone draws over it),
                // so a notch in a pit's wall shows the pit's back wall, not the sky behind the level.
                float width = r.width + 0.1f + 2f * SoloRoomSkin.DeepReach;
                var centre = new Vector2(r.center.x, (top + bottom) * 0.5f);
                SpriteRenderer Quad(string name, Sprite s, Vector2 at, float z, Material mat)
                {
                    var go = new GameObject(name);
                    go.transform.SetParent(c.EnvRoot, false);
                    go.layer = c.LayerIndex;
                    go.transform.position = new Vector3(at.x, at.y, c.EnvRoot.position.z + z);
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = s; sr.sharedMaterial = mat;
                    sr.sortingLayerName = gameplay; sr.sortingOrder = -4;
                    return sr;
                }
                SpriteRenderer back = Quad($"PitVoid_{n}", wall, centre, 0f, stone);
                back.drawMode = SpriteDrawMode.Tiled; back.tileMode = SpriteTileMode.Continuous; back.size = new Vector2(width, height); back.color = VoidTint;
                // The far edge of the hole (the developer: "All edges need to be a bit bumpy"): the wall's own recessed stone,
                // broken around the lip's height (ENV_RecessLip_*), over the wall's top. No grass and no lit lip, so it never
                // reads as a walkway across the pit.
                if (farLip != null && farLipSlot != null)
                {
                    // 2026-10-17 visual review ("its wavy outline sticks up above the floor line into the sky"): the broken edge
                    // stays below the lip's line (LipBelow), where the floors' own lit caps frame it.
                    SpriteRenderer far = Quad($"PitVoid_{n}_FarLip", farLip, new Vector2(centre.x, top - LipBelow - farLipSlot.height * 0.5f), -0.0005f, stone);
                    far.drawMode = SpriteDrawMode.Tiled; far.tileMode = SpriteTileMode.Continuous;
                    far.size = new Vector2(width, farLipSlot.height); far.color = VoidTint;
                    back.size = new Vector2(width, height - LipBelow - farLipSlot.height * 0.5f);
                    back.transform.position = new Vector3(centre.x, bottom + back.size.y * 0.5f, back.transform.position.z);
                }
                Veil($"PitVoid_{n}_Veil", new Vector3(centre.x, (top + bottom) * 0.5f, 0f), new Vector2(width, height));
                // The shading: the ramp (clear at its top, opaque at its bottom) turned so its dark end meets the lip or a wall.
                var shade = new Color(0.36f, 0.26f, 0.2f, 1f);
                void Ramp(string name, Vector2 at, float angle, float across, float along, float strength)
                {
                    SpriteRenderer sr = Quad(name, ramp, at, -0.001f, m);
                    sr.transform.rotation = Quaternion.Euler(0f, 0f, angle);
                    sr.transform.localScale = new Vector3(across / ramp.bounds.size.x, along / ramp.bounds.size.y, 1f);
                    sr.color = new Color(shade.r, shade.g, shade.b, strength);
                }
                // (No shadow quad under the lip: darkest at its top, it drew a straight line through the ragged far edge, whose
                // own dark rim does that job.)
                float side = Mathf.Min(SideReach, width * 0.5f);
                Ramp($"PitVoid_{n}_Left", new Vector2(centre.x - width * 0.5f + side * 0.5f, centre.y), -90f, height, side, SideShade);
                Ramp($"PitVoid_{n}_Right", new Vector2(centre.x + width * 0.5f - side * 0.5f, centre.y), 90f, height, side, SideShade);
                // 2026-10-17 visual review (L006-L010: "the pit back walls read lighter and nearer than the faces around them";
                // L010: "the chasm is a fully lit flat wall ... the spike row floats mid-wall"): the wall darkens down the pit,
                // light near the lip (the developer: the cave-in easy to see), in shadow at the hazard's line, and fully in
                // shadow under it (a chasm's wall going on down past its spikes).
                float kill = Mathf.Max(bottom, r.yMin);
                if (top - kill > 0.2f) Ramp($"PitVoid_{n}_Depth", new Vector2(centre.x, (top + kill) * 0.5f), 0f, width, top - kill, DepthShade);
                if (kill - bottom > 0.05f && solidShade != null)
                {
                    SpriteRenderer under = Quad($"PitVoid_{n}_Deep", solidShade, new Vector2(centre.x, (kill + bottom) * 0.5f), -0.001f, m);
                    under.transform.localScale = new Vector3(width / solidShade.bounds.size.x, (kill - bottom) / solidShade.bounds.size.y, 1f);
                    under.color = new Color(shade.r, shade.g, shade.b, DepthShade);
                }
                n++;
            }

            // 2026-10-17 visual review (L004 Roof_4: "once it collapses, two dark posts float over open sky"): a trap that fills a
            // notch in fixed stone (fixed stone at both its sides and over or under it) leaves a recess, not a window to the
            // sky: the same recessed back wall behind it (the trap's skin covers it until it goes), unless a pit's already does.
            float roomTop = SoloRoomBuilder.ComputeRoomBounds(room, 0f).max.y;
            var voids = new List<Rect>();
            foreach (Transform v in c.EnvRoot) if (v.name.StartsWith("PitVoid_") && v.GetComponent<SpriteRenderer>() is SpriteRenderer pv && v.name.Split('_').Length == 2) voids.Add(new Rect((Vector2)pv.bounds.min, pv.bounds.size));
            bool Flush(SoloRoomSkin.Solid s, Rect r, System.Func<Rect, Rect, bool> touches) => !s.Trap && touches(s.Rect, r);
            int k = 0;
            foreach (SoloRoomSkin.Solid t in solids.Where(o => o.Trap && o.Kind is SoloRoomElementKind.CollapsingFloor or SoloRoomElementKind.FallingBlock or SoloRoomElementKind.ShrinkingFloor && !o.Grip))
            {
                Rect r = t.Rect;
                // A side wall counts where it stands along at least half the trap's height; the recess is where both do.
                float Along(Rect a) => Mathf.Min(a.yMax, r.yMax) - Mathf.Max(a.yMin, r.yMin);
                List<Rect> lefts = solids.Where(o => Flush(o, r, (a, b) => Mathf.Abs(a.xMax - b.xMin) < 0.05f && Along(a) >= 0.5f * b.height)).Select(o => o.Rect).ToList();
                List<Rect> rights = solids.Where(o => Flush(o, r, (a, b) => Mathf.Abs(a.xMin - b.xMax) < 0.05f && Along(a) >= 0.5f * b.height)).Select(o => o.Rect).ToList();
                // (or the masonry over the room: a block that drops out of the room's roof leaves a hole in it; L006-L009)
                bool over = solids.Any(o => Flush(o, r, (a, b) => Mathf.Abs(a.yMin - b.yMax) < 0.05f && a.xMin <= b.xMin + 0.05f && a.xMax >= b.xMax - 0.05f))
                    || r.yMax >= roomTop - 0.05f;
                bool under = solids.Any(o => Flush(o, r, (a, b) => Mathf.Abs(a.yMax - b.yMin) < 0.05f && a.xMin <= b.xMin + 0.05f && a.xMax >= b.xMax - 0.05f));
                if (lefts.Count == 0 || rights.Count == 0 || voids.Any(v => v.Overlaps(r))) continue;
                // 2026-10-19 (the developer, L007's S1_T4: "it's falling from the second floor, the empty space should look like the
                // background"): a trap flush in a slab open above and below leaves a real hole, the sky through it (the set-back
                // stone the 2026-10-18 round put there read as a floor that wasn't). That round's "gap in the ceiling" was the
                // notch beside it at rest, now stone in the layout (L007's Lintel_G).
                if (!over && !under) continue;
                // The recess runs as far as stone stands flush at both sides (every solid there, joined end to end, from the
                // closed end), so it never stops short of the stone framing it.
                float Reach(bool atLeft, bool down)
                {
                    float edge = atLeft ? r.xMin : r.xMax, y = down ? r.yMax : r.yMin;
                    for (bool grew = true; grew;)
                    {
                        grew = false;
                        foreach (SoloRoomSkin.Solid o in solids.Where(o => !o.Trap && Mathf.Abs((atLeft ? o.Rect.xMax : o.Rect.xMin) - edge) < 0.05f))
                            if (down ? o.Rect.yMax >= y - 0.05f && o.Rect.yMin < y - 0.05f : o.Rect.yMin <= y + 0.05f && o.Rect.yMax > y + 0.05f)
                            { y = down ? o.Rect.yMin : o.Rect.yMax; grew = true; }
                    }
                    return down ? Mathf.Max(y, r.yMin) : Mathf.Min(y, r.yMax);
                }
                float y0 = over ? Mathf.Max(Reach(true, true), Reach(false, true)) : r.yMin;
                float y1 = over ? r.yMax : Mathf.Min(Reach(true, false), Reach(false, false));
                if (y1 - y0 < 0.3f) continue;
                Recess(Rect.MinMaxRect(r.xMin, y0, r.xMax, y1), over, under);
            }

            // 2026-10-17 visual review (L006: "a flat, featureless block-shaped hole in the ceiling" where Block_2 dropped out
            // of the one ceiling it sat in): every hole a falling block leaves in fixed stone (SoloRoomSkin.Holes) is a recess
            // too, open on the side the block left by.
            foreach (SoloRoomSkin.Solid f in solids.Where(o => !o.Trap))
                foreach (Rect h in SoloRoomSkin.Holes(f, room))
                {
                    if (voids.Any(v => v.Overlaps(h))) continue;
                    // (a hole through a roof is closed above by the masonry over the room)
                    bool openBelow = h.yMin <= f.Rect.yMin + 0.05f, openAbove = h.yMax >= f.Rect.yMax - 0.05f && f.Rect.yMax < roomTop - 0.05f;
                    if (openBelow == openAbove) continue;   // through the whole solid, or not open at all: nothing to frame
                    Recess(h, openBelow, openAbove);
                }

            // 2026-10-18 (the developer, L004 Sink_R: "there is too much white space"): a section that rises into a recess (a
            // solid mover going straight up, walled at both sides, under a cover within 0.6 u of where it stops) has the recess's
            // back wall behind its whole travel.
            foreach (SoloRoomElement e in room.Elements.Where(x => x.Kind == SoloRoomElementKind.MovingTrap && x.Settings.MovingKind == Parallax.Gameplay.Rooms.MovingTrapKind.Solid
                && x.Settings.Offset.y > 0f && Mathf.Abs(x.Settings.Offset.x) < 1e-3f))
            {
                var rest = new Rect(room.Origin + e.Position - e.Size * 0.5f, e.Size);
                float stop = rest.yMax + e.Settings.Offset.y;
                SoloRoomSkin.Solid? cover = solids.Where(o => !o.Trap && o.Rect.yMin >= stop - 0.05f && o.Rect.yMin <= stop + 0.6f && o.Rect.xMin <= rest.xMin + 0.05f && o.Rect.xMax >= rest.xMax - 0.05f)
                    .Select(o => (SoloRoomSkin.Solid?)o).FirstOrDefault();
                bool Side(bool left) => solids.Any(o => !o.Trap && Mathf.Abs((left ? o.Rect.xMax : o.Rect.xMin) - (left ? rest.xMin : rest.xMax)) < 0.05f && o.Rect.yMax > rest.yMax && o.Rect.yMin < stop);
                if (!cover.HasValue || !Side(true) || !Side(false) || voids.Any(v => v.Overlaps(rest))) continue;
                Recess(Rect.MinMaxRect(rest.xMin, rest.yMin, rest.xMax, cover.Value.Rect.yMin), true, false);
            }

            // The back wall over `area` (as wide as the walls' broken outline reaches into it) and its broken open end. `over`:
            // closed above, open below (and the reverse for `under`).
            void Recess(Rect area, bool over, bool under)
            {
                var go = new GameObject($"TrapVoid_{k}");
                go.transform.SetParent(c.EnvRoot, false);
                go.layer = c.LayerIndex;
                var back = go.AddComponent<SpriteRenderer>();
                // 2026-10-18 (the developer, L004's Roof_4: "Why is the normal tiles whiter than the section with the trap"): a
                // recess in a wall is the wall's own stone, lit and world-tiled like it (so the room's StoneShade map shades both
                // alike), set back (RecessBack); the pits' unlit recessed stone never matched the wall around it.
                Material litFill = EnvironmentKit.TileMaterial("Fill");
                Sprite fillSprite = SoloRoomSkin.FillSprite(room);
                bool lit = litFill != null && fillSprite != null;
                back.sprite = lit ? fillSprite : wall; back.sharedMaterial = lit ? litFill : stone;
                back.sortingLayerName = gameplay; back.sortingOrder = -4;
                back.drawMode = lit ? SpriteDrawMode.Sliced : SpriteDrawMode.Tiled; if (!lit) back.tileMode = SpriteTileMode.Continuous;
                back.size = new Vector2(area.width + 2f * SoloRoomSkin.DeepReach, area.height + (over ? SoloRoomSkin.DeepReach : 0f) + (under ? SoloRoomSkin.DeepReach : 0f));
                go.transform.position = new Vector3(area.center.x, area.center.y + ((over ? SoloRoomSkin.DeepReach : 0f) - (under ? SoloRoomSkin.DeepReach : 0f)) * 0.5f, c.EnvRoot.position.z);
                back.color = lit ? SoloRoomSkin.Body * RecessBack : VoidTint; back.color = new Color(back.color.r, back.color.g, back.color.b, 1f);
                voids.Add(area);
                Veil($"TrapVoid_{k}_Veil", go.transform.position, back.size);
                // 2026-10-18 visual review (L016: "ceiling spikes appear mid-face after firing"): the lit back wall reads as a
                // recess, not as more face: in shadow at its closed end and down both its sides, like a pit's.
                if (lit && ramp != null)
                {
                    var shade = new Color(0.36f, 0.26f, 0.2f, 1f);
                    void Shade(string name, Vector2 at, float angle, float across, float along, float strength)
                    {
                        var s = new GameObject($"TrapVoid_{k}_{name}");
                        s.transform.SetParent(c.EnvRoot, false);
                        s.layer = c.LayerIndex;
                        s.transform.position = new Vector3(at.x, at.y, c.EnvRoot.position.z - 0.001f);
                        s.transform.rotation = Quaternion.Euler(0f, 0f, angle);
                        s.transform.localScale = new Vector3(across / ramp.bounds.size.x, along / ramp.bounds.size.y, 1f);
                        var sr = s.AddComponent<SpriteRenderer>();
                        sr.sprite = ramp; sr.sharedMaterial = m; sr.sortingLayerName = gameplay; sr.sortingOrder = -4;
                        sr.color = new Color(shade.r, shade.g, shade.b, strength);
                    }
                    float side = Mathf.Min(SideReach, area.width * 0.5f);
                    Shade("Left", new Vector2(area.xMin + side * 0.5f, area.center.y), -90f, area.height, side, RecessSideShade);
                    Shade("Right", new Vector2(area.xMax - side * 0.5f, area.center.y), 90f, area.height, side, RecessSideShade);
                    // (the ramp is opaque at its bottom: turned over for a recess closed above)
                    float deep = Mathf.Min(RecessDeepReach, area.height);
                    if (over && !under) Shade("Deep", new Vector2(area.center.x, area.yMax - deep * 0.5f), 180f, area.width, deep, RecessDeepShade);
                    if (under && !over) Shade("Deep", new Vector2(area.center.x, area.yMin + deep * 0.5f), 0f, area.width, deep, RecessDeepShade);
                }
                // Its open end: the recess's broken edge (the pits' far lip, turned to face the opening), not a ruler line. (Not on
                // the wall's own stone: there the open end is the line of the faces either side, whose broken strips edge it.)
                if (!lit && farLip != null && farLipSlot != null)
                {
                    var lip = new GameObject($"TrapVoid_{k}_Lip");
                    lip.transform.SetParent(c.EnvRoot, false);
                    lip.layer = c.LayerIndex;
                    float open = over ? area.yMin : area.yMax, inward = over ? 1f : -1f;
                    lip.transform.position = new Vector3(area.center.x, open + inward * (LipBelow + farLipSlot.height * 0.5f), c.EnvRoot.position.z - 0.0005f);
                    var lr = lip.AddComponent<SpriteRenderer>();
                    lr.sprite = farLip; lr.sharedMaterial = stone; lr.sortingLayerName = gameplay; lr.sortingOrder = -4;
                    lr.drawMode = SpriteDrawMode.Tiled; lr.tileMode = SpriteTileMode.Continuous;
                    lr.size = new Vector2(back.size.x, farLipSlot.height); lr.color = VoidTint; lr.flipY = over;
                    // The panel stops where the lip's solid part begins.
                    float cut = LipBelow + farLipSlot.height * 0.5f;
                    back.size = new Vector2(back.size.x, Mathf.Max(0.1f, back.size.y - cut));
                    go.transform.position += new Vector3(0f, inward * cut * 0.5f, 0f);
                }
                k++;
            }
        }

        /// <summary>How far below the pit's lip line its back wall's broken top edge stays (world units).</summary>
        const float LipBelow = 0.04f;
        /// <summary>A recess's back wall against the wall around it (the same stone, set back in shadow).</summary>
        const float RecessBack = 0.72f;
        /// <summary>The haze veil over every pit and recess back wall (its alpha): the hole reads lighter and further off.</summary>
        const float BackVeil = 0.22f;
        static readonly Color VeilLight = new(1f, 0.9f, 0.74f, 1f);
        /// <summary>A lit recess's shadow at its closed end (its strength, 0..1, and how far it reaches toward the opening) and down its sides.</summary>
        const float RecessDeepShade = 0.8f, RecessDeepReach = 1.2f, RecessSideShade = 0.5f;
        /// <summary>A pit shallower than this (lip to floor, world units) and over three times as wide is a trench: no back wall.</summary>
        const float ShallowTrench = 1.2f;

        /// <summary>A soft vertical gradient of `colour` between two heights over a layer's whole visible range: one quad, no
        /// texture to repeat. `up`: opaque at the bottom, clear at the top.</summary>
        static void Gradient(TierContext c, string layerName, string name, float y0, float y1, Color colour, float alpha, bool up)
        {
            Sprite fade = EnvironmentKit.Sprite("ENV_SkyFade");
            if (fade == null || alpha <= 0f) return;
            LevelLookConfig.Layer def = c.Config.GetLayer(layerName);
            // One cheap quad: a whole view wider than it can ever be seen on each side, so its ends never show.
            var (from, to) = Visible(c, def.Speed, c.Travel.MaxViewWidth);
            Color tint = colour; tint.a = alpha;
            SpriteRenderer r = c.Place(c.Layer(layerName), name, "ENV_SkyFade", new Vector2((from + to) * 0.5f, (y0 + y1) * 0.5f),
                new Vector2((to - from) / fade.bounds.size.x, (y1 - y0) / fade.bounds.size.y), tint, def.Order + 2, def.Speed < 0.3f ? c.Back : c.Middle);
            if (r == null) return;
            r.flipY = up;
            // Round 8: its opaque end continues into a soft fade (2 u), so a pit or a gap never shows the gradient's cut edge.
            float fadeY = up ? y0 - 1f : y1 + 1f;
            SpriteRenderer tail = c.Place(c.Layer(layerName), name + "_Fade", "ENV_SkyFade", new Vector2((from + to) * 0.5f, fadeY),
                new Vector2((to - from) / fade.bounds.size.x, 2f / fade.bounds.size.y), tint, def.Order + 2, def.Speed < 0.3f ? c.Back : c.Middle);
            if (tail != null) tail.flipY = !up;
        }

        // ---------- the 2D lights ----------

        static void Lights(ComposeContext k)
        {
            TierContext c = k.Tier;
            LevelPalettes.Palette p = k.Palette;
            int[] targets =
            {
                SortingLayer.NameToID(RealitySpace.SortingLayerName(c.Root.Id, SortingBand.Background)),
                SortingLayer.NameToID(RealitySpace.SortingLayerName(c.Root.Id, SortingBand.Middle)),
                SortingLayer.NameToID(RealitySpace.SortingLayerName(c.Root.Id, SortingBand.Gameplay)),
                SortingLayer.NameToID(RealitySpace.SortingLayerName(c.Root.Id, SortingBand.Foreground)),
            };
            Light2D Point(string name, Color colour, float intensity, float inner, float outer, float falloff)
            {
                var go = new GameObject(name);
                go.transform.SetParent(c.EnvRoot, false);
                go.layer = c.LayerIndex;
                var light = go.AddComponent<Light2D>();
                light.lightType = Light2D.LightType.Point;
                light.color = colour; light.intensity = intensity;
                light.pointLightInnerRadius = inner; light.pointLightOuterRadius = outer; light.falloffIntensity = falloff;
                light.targetSortingLayers = targets;
                return light;
            }

            // The sun's backlight: from the sun's side of the view, warm, wide, following the camera.
            float view = c.Travel.ViewHeight, width = view * 20f / 9f;
            var sunOffset = new Vector2((p.SunAt.x - 0.5f) * width, (p.SunAt.y - 0.5f) * view);
            Light2D sun = Point("SunLight", LevelPalettes.CapLight(p.SunLight), p.SunIntensity, view * 0.15f, width * 0.85f, 0.45f);
            sun.gameObject.AddComponent<FollowTransform>().Configure(c.Camera.transform, sunOffset);

            // The cat's key light: a small warm light over its shoulder, so the painted cat reads (not a black shape).
            var follow = c.Camera.GetComponent<LevelCameraFollow>();
            Transform cat = follow != null ? new SerializedObject(follow).FindProperty("target").objectReferenceValue as Transform : null;
            if (cat == null) { c.Changes.Add("no cat light (the camera follows no target)"); return; }
            Light2D key = Point("CatLight", LevelPalettes.CapLight(p.CatLight), p.CatIntensity, 0.4f, 2.2f, 0.5f);
            key.gameObject.AddComponent<FollowTransform>().Configure(cat, new Vector2(-0.35f, 0.9f));
        }
    }
}
