using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Parallax.Core;
using Parallax.Core.Presentation;
using Parallax.Editor.Levels;
using Parallax.Editor.Routes;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Rooms;
using Parallax.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Parallax.Editor.Art
{
    /// <summary>PAX-A13 (§9.3, §12 R10): PARALLAX/Art/Trap Contact Sheet. Every trap-kit element in its states, caught from
    /// real replays (the route harness plays a level's solution or one of its betrayals; each tile is rendered on the tick its
    /// state happens, relative to the trap's fire or to the death), at phone scale (90 px per unit), against a warm backdrop.
    /// Elements whose phases the trap exposes (arrows, geysers, the cloud) are caught by phase; the rest at fixed ticks after
    /// the fire. A row still drawn with a code placeholder for a ChatGPT body says PLACEHOLDER. Each animated element also gets
    /// a strip of 10 frames, 2 ticks apart, in Docs/Art/Traps/strips/. Plus the launcher facing both ways. Saved to
    /// Docs/Art/Traps/contact_sheet.png. Nothing is saved to a scene; the open scene is restored.</summary>
    public static class TrapContactSheet
    {
        const int TileW = 360, TileH = 300, Gap = 12, LabelH = 26, Columns = 5, StripFrames = 10;
        const string OutPath = "Docs/Art/Traps/contact_sheet.png", StripDir = "Docs/Art/Traps/strips/";
        const string Death = "DEATH";
        static readonly Color Backdrop = new(0.93f, 0.82f, 0.62f, 1f);   // the styleframe's warm haze

        sealed class Row
        {
            public string Title, Level, Trap, Route;   // Route: null = the solution, else the betrayal whose name starts with it
            public bool A02Host, Strip;
            public Func<TrapArtConfig, Sprite[]> Bodies = _ => new Sprite[0];
            public List<(string label, Func<RoomTrap, int, bool> when)> States = new();
        }

        [MenuItem("PARALLAX/Art/Trap Contact Sheet")]
        public static void Make()
        {
            if (!TrapArtReady()) return;
            var rows = BuildRows();
            var config = AssetDatabase.LoadAssetAtPath<TrapArtConfig>(TrapArtSetup.ConfigPath);
            var tiles = new List<(string title, List<(string label, Texture2D tex)> shots)>();
            var strips = new List<(string name, List<Texture2D> frames)>();
            string flipNote;
            using (var session = new RouteSession())
            {
                foreach (Row row in rows)
                {
                    (List<(string, Texture2D)> shots, List<Texture2D> strip) = CaptureRow(session, row);
                    string[] placeholders = row.Bodies(config).Where(b => b != null && config.IsPlaceholder(b)).Select(b => b.name).Distinct().ToArray();
                    tiles.Add((placeholders.Length > 0 ? $"{row.Title}  [PLACEHOLDER: {string.Join(", ", placeholders)}]" : row.Title, shots));
                    if (strip.Count > 0) strips.Add(($"{row.Level}_{row.Trap}{(row.A02Host ? "_A02" : "")}", strip));
                }
                (List<(string, Texture2D)> flipShots, string note) = CaptureFlipLighting(session);
                tiles.Add(("TRAP-01 FACING BOTH WAYS (FLIP SHADER; POINT-LIGHT SHADING IS CHECKED IN THE SCENE VIEW)", flipShots));
                flipNote = note;
            }
            Texture2D sheet = Compose(tiles, "PAX-A13 TRAP KIT - EVERY ELEMENT AT PHONE SCALE (90 PX/U) - HOSTS GREY-BOX UNLESS NOTED");
            TrapShots.SavePng(sheet, OutPath);
            foreach ((string name, List<Texture2D> frames) in strips) SaveStrip(name, frames);
            Debug.LogWarning($"Trap Contact Sheet: {OutPath} ({sheet.width}x{sheet.height}), {tiles.Count} rows, {strips.Count} strips in {StripDir}; {flipNote}");
            foreach (var (_, shots) in tiles) foreach (var (_, tex) in shots) if (tex != null) Object.DestroyImmediate(tex);
            foreach (var (_, frames) in strips) foreach (Texture2D f in frames) Object.DestroyImmediate(f);
            Object.DestroyImmediate(sheet);
        }

        internal static bool TrapArtReady()
        {
            if (AssetDatabase.LoadAssetAtPath<TrapArtConfig>(TrapArtSetup.ConfigPath) == null) { Debug.LogError("Trap art: no TrapArtConfig; run PARALLAX/Art/Import Trap Kit first."); return false; }
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) { Debug.LogError("Trap art: save or discard the open scene first (the route session restores it from disk)."); return false; }
            return true;
        }

        static List<Row> BuildRows()
        {
            static bool Before(RoomTrap t, int tick) => tick == 2;
            var arrowLeft = new Row { Title = "ARROW (DISGUISED, FACING LEFT) - L015 ARROW_3", Level = "L015", Trap = "Arrow_3" };
            var arrowRight = new Row { Title = "ARROW (DISGUISED, FACING RIGHT) - L018 ARROW_2", Level = "L018", Trap = "Arrow_2" };
            foreach (Row r in new[] { arrowLeft, arrowRight })
            {
                r.States.Add(("BEFORE: WALL SKIN", Before));
                r.States.Add(("TELL, MID-WINDOW", (t, _) => TrapShots.TicksSinceFire(t) == Tell(t) / 2));
                r.States.Add(("FLIGHT", (t, _) => TrapShots.TicksSinceFire(t) == Tell(t) + 3));
                r.States.Add(("STOPPED + IMPACT", (t, _) => TrapShots.TicksSinceFire(t) == Tell(t) + Flight(t) + 2));
                r.States.Add(("AT REST", (t, _) => TrapShots.TicksSinceFire(t) == Tell(t) + Flight(t) + 24));
            }
            var honest = new Row { Title = "ARROW (HONEST) - L015 ARROW_9", Level = "L015", Trap = "Arrow_9" };
            honest.States.Add(("BEFORE: THE SLOT", Before));
            honest.States.Add(("TELL", (t, _) => TrapShots.TicksSinceFire(t) == Tell(t) / 2));

            var geyserUp = new Row { Title = "GEYSER (UP) - L018 G_1", Level = "L018", Trap = "G_1" };
            var geyserDown = new Row { Title = "GEYSER (DOWN, IN A CEILING) - L018 G_D", Level = "L018", Trap = "G_D" };
            foreach (Row r in new[] { geyserUp, geyserDown })
            {
                r.States.Add(("IDLE", (t, tick) => tick > 2 && ((GeyserTrap)t).Phase == GeyserPhase.Idle));
                r.States.Add(("TELL, MID-WINDOW", (t, _) => ((GeyserTrap)t).Phase == GeyserPhase.Tell && TrapShots.TicksSinceFire(t) == 12));
                r.States.Add(("ERUPT: BURST", (t, _) => ((GeyserTrap)t).Phase == GeyserPhase.Erupt && TrapShots.TicksSinceFire(t) == GeyserTell(t) + 3));
                r.States.Add(("ERUPT: MID", (t, _) => ((GeyserTrap)t).Phase == GeyserPhase.Erupt && TrapShots.TicksSinceFire(t) == GeyserTell(t) + 20));
            }

            var floor = new Row { Title = "COLLAPSING FLOOR - L015 COLLAPSE_2 (GREY-BOX HOST)", Level = "L015", Trap = "Collapse_2" };
            var floorA02 = new Row { Title = "COLLAPSING FLOOR ON THE A02 FILL TILE (HOST RE-SKINNED FOR THIS SHEET; ENV-10 NOT IN YET)", Level = "L015", Trap = "Collapse_2", A02Host = true };
            foreach (Row r in new[] { floor, floorA02 })
            {
                r.States.Add(("BEFORE: HOST SKIN", Before));
                r.States.Add(("REVEAL FRAME", (t, _) => TrapShots.TicksSinceFire(t) == 0));
                r.States.Add(("CRUMBLE +6", (t, _) => TrapShots.TicksSinceFire(t) == 6));
                r.States.Add(("CRUMBLE +16", (t, _) => TrapShots.TicksSinceFire(t) == 16));
                r.States.Add(("GONE +40", (t, _) => TrapShots.TicksSinceFire(t) == 40));
            }
            foreach (Row r in new[] { arrowLeft, arrowRight, geyserUp, geyserDown, floor, floorA02 }) r.Strip = true;
            geyserUp.Bodies = geyserDown.Bodies = c => c.Steam;
            var rows = new List<Row> { arrowLeft, arrowRight, honest };

            var spear = new Row { Title = "SPEAR - L011 SPEAR_1", Level = "L011", Trap = "Spear_1", Strip = true, Bodies = c => new[] { c.SpearHead, c.SpearShaft } };
            spear.States.Add(("BEFORE", Before));
            spear.States.Add(("TELL, MID-WINDOW", (t, _) => TrapShots.TicksSinceFire(t) == Tell(t) / 2));
            spear.States.Add(("FLIGHT", (t, _) => TrapShots.TicksSinceFire(t) == Tell(t) + 3));
            spear.States.Add(("STUCK + SHIVER", (t, _) => TrapShots.TicksSinceFire(t) == Tell(t) + Flight(t) + 2));
            spear.States.Add(("PLANTED", (t, _) => TrapShots.TicksSinceFire(t) == Tell(t) + Flight(t) + 24));
            rows.Add(spear);
            rows.AddRange(new[] { geyserUp, geyserDown, floor, floorA02 });
            rows.Add(Timed("FAKE PLATFORM - L011 LEDGE_M", "L011", "Ledge_M", null, "BEFORE: HOST SKIN", null, 0, 6, 16, 40));

            Func<TrapArtConfig, Sprite[]> spikes = c => new[] { c.SpikeStrip };
            var briar = new Row { Title = "SPIKES, STATIC (HONEST) - L018 BRIAR", Level = "L018", Trap = "Briar", Bodies = spikes };
            briar.States.Add(("ALWAYS", Before));
            rows.Add(briar);
            rows.Add(Timed("SPIKES, HIDDEN - L004 SPIKES_1 (BETRAYAL T1)", "L004", "Spikes_1", "T1:", "BEFORE: NOTHING", spikes, 0, 1, 2, 3));
            rows.Add(Timed("SPIKES, PERIODIC - L006 SPIKES_1", "L006", "Spikes_1", null, "DOWN", spikes, 0, 2, 8, 30));
            rows.Add(Timed("SPIKES, SLIDING - L006 SWEEP_3", "L006", "Sweep_3", null, "REST", spikes, 0, 8, 16, 40));
            rows.Add(Timed("FALLING BLOCK - L001 BLOCK_1", "L001", "Block_1", null, "BEFORE: HOST SKIN", c => new[] { c.BlockCrack }, 0, 4, 12, 30));
            rows.Add(Timed("MOVING FLOOR, CARRIES - L015 MOVER_M", "L015", "Mover_M", null, "REST: HOST SKIN", null, 0, 10, 30, 60));
            rows.Add(Timed("DROP-AND-RETURN - L018 DROP_3", "L018", "Drop_3", null, "REST: HOST SKIN", null, 0, 4, 12, 40));
            rows.Add(Timed("LIFT INTO THE ROOF - L008 LIFT_3 (BETRAYAL T3)", "L008", "Lift_3", "T3:", "REST: HOST SKIN", null, 0, 3, 6, 9));
            rows.Add(Timed("PUSH WALL - L020 PUSH_3", "L020", "Push_3", null, "REST: HOST SKIN", null, 0, 6, 16, 40));
            rows.Add(Timed("SHRINKING FLOOR - L014 SHRINK", "L014", "Shrink", null, "FULL", null, 5, 20, 35, 50));
            rows.Add(Timed("GRAVITY FLIP, VISIBLE - L004 FLIP_A (BETRAYAL T2)", "L004", "Flip_A", "T2:", "IDLE: GLYPH + MOTES", c => new[] { c.GlyphRing }, 0, 2, 5, 8));
            rows.Add(Timed("GRAVITY FLIP, HIDDEN - L016 FLIP_H7 (BETRAYAL T7)", "L016", "Flip_H7", "T7", "BEFORE: NOTHING", null, 0, 2, 8, 20));
            rows.Add(Timed("INVERTER - L012 ORB_A", "L012", "Orb_A", null, "IDLE: THE ORB", c => new[] { c.InverterOrb, c.CueRing, c.CueMark }, 0, 3, 40, 120));

            var cloud = new Row { Title = "STORM CLOUD - L015 CLOUD", Level = "L015", Trap = "Cloud", Strip = true, Bodies = c => new[] { c.StormCloud, c.Scorch } };
            cloud.States.Add(("ASLEEP", Before));
            cloud.States.Add(("AWAKE, FOLLOWING", (t, _) => ((StormCloudTrap)t).Phase == StormCloudPhase.Follow && TrapShots.TicksSinceFire(t) == 12));
            cloud.States.Add(("CHARGE, MID", (t, _) => ((StormCloudTrap)t).Phase == StormCloudPhase.Charge && InCycle(t) == (int)Field(t, "tellTicks") / 2));
            cloud.States.Add(("STRIKE", (t, _) => ((StormCloudTrap)t).Phase == StormCloudPhase.Strike && InCycle(t) == (int)Field(t, "tellTicks") + 1));
            cloud.States.Add(("STRIKE, LAST TICK", (t, _) => ((StormCloudTrap)t).Phase == StormCloudPhase.Strike
                && InCycle(t) == (int)Field(t, "tellTicks") + (int)Field(t, "strikeTicks") - 1));
            rows.Add(cloud);
            rows.Add(Timed("SNAP VINE - L014 V1", "L014", "V1", null, "WHOLE", c => c.Leaves, 0, 2, 8, 20));
            rows.Add(Timed("DOOR RETREAT - L020 RETREAT_10", "L020", "Retreat_10", null, "AT REST", null, 0, 4, 10, 20));

            foreach ((string title, string level, string route) in new[]
            {
                ("DEATH BY SPIKES - L004 T1", "L004", "T1:"), ("DEATH BY CRUSH - L008 T1", "L008", "T1:"),
                ("DEATH BY ARROW - L015 T3", "L015", "T3 ["), ("DEATH BY SPEAR - L015 T10", "L015", "T10"),
                ("DEATH BY LIGHTNING - L015 T1", "L015", "T1 ["), ("DEATH BY FALL - L001 T1", "L001", "T1:"),
            })
            {
                var death = new Row { Title = title, Level = level, Trap = Death, Route = route };
                foreach (int n in new[] { 1, 4, 10, 20 }) { int at = n; death.States.Add(($"HOLD +{at}", (_, __) => HoldTicks() == at)); }
                rows.Add(death);
            }
            return rows;
        }

        // A row caught at fixed ticks after the trap's fire (for traps that don't expose their phases), plus the look before.
        static Row Timed(string title, string level, string trap, string route, string before, Func<TrapArtConfig, Sprite[]> bodies, params int[] after)
        {
            var row = new Row { Title = title, Level = level, Trap = trap, Route = route, Strip = true };
            if (bodies != null) row.Bodies = bodies;
            row.States.Add((before, (t, tick) => tick == 2));
            foreach (int n in after) { int at = n; row.States.Add(($"FIRE +{at}", (t, _) => TrapShots.TicksSinceFire(t) == at)); }
            return row;
        }

        // Ticks since the held death, while the room is frozen; -1 otherwise.
        static int HoldTicks()
        {
            var death = Object.FindFirstObjectByType<RoomDeath>();
            var observers = Object.FindFirstObjectByType<ObserverSet>();
            return death != null && observers != null && death.IsHolding ? observers.Tick - death.HoldTick : -1;
        }

        // Where the storm cloud is in its strike cycle (tell then strike), from its first strike on; -1 before.
        static int InCycle(RoomTrap t)
        {
            int s = TrapShots.TicksSinceFire(t), first = (int)Field(t, "firstStrikeDelay"), period = (int)Field(t, "strikePeriod");
            return s >= first && period > 0 ? (s - first) % period : -1;
        }

        static int Tell(RoomTrap t) => (int)Field(t, "tellTicks");
        static int GeyserTell(RoomTrap t) => (int)Field(t, "tellTicks");
        static int Flight(RoomTrap t) => ArrowMath.FlightTicks((float)Field(t, "travel"), (float)Field(t, "unitsPerTick"));

        static object Field(object o, string name) =>
            o.GetType().GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(o);

        static (List<(string, Texture2D)>, List<Texture2D>) CaptureRow(RouteSession session, Row row)
        {
            var shots = new List<(string, Texture2D)>();
            var strip = new List<Texture2D>();
            var done = new bool[row.States.Count];
            int stripFrom = -1;
            using var rig = new TrapShots.Rig();
            bool reskinned = false;
            var options = new ReplayOptions
            {
                AfterTick = tick =>
                {
                    // A bare hazard (static spikes) has no RoomTrap: its states read the tick only. A death row frames the cat.
                    Transform target = row.Trap == Death ? null : FindElement(row.Trap);
                    if (row.Trap != Death && target == null) return;
                    RoomTrap trap = target != null && target.TryGetComponent(out RoomTrap t) ? t : null;
                    if (row.A02Host && !reskinned) { TrapArtPreview.ReskinHostsWithA02(); reskinned = true; }
                    TrapShots.ApplyArt();
                    (Vector2 centre, float view) = Frame(target, trap);
                    for (int i = 0; i < row.States.Count; i++)
                    {
                        if (done[i] || !row.States[i].when(trap, tick)) continue;
                        rig.Ensure(Backdrop, 1f);
                        shots.Add(($"{row.States[i].label}  T{tick}", rig.Shot(centre, view, TileW, TileH)));
                        done[i] = true;
                    }
                    if (!row.Strip || trap == null) return;
                    if (stripFrom < 0 && TrapShots.TicksSinceFire(trap) == 0) stripFrom = tick;
                    if (stripFrom >= 0 && strip.Count < StripFrames && (tick - stripFrom) % 2 == 0)
                    {
                        rig.Ensure(Backdrop, 1f);
                        strip.Add(rig.Shot(centre, view, TileW / 2, TileH / 2));
                    }
                },
            };
            RoomRoutes routes = LevelRoutes.ById[row.Level];
            Route route = row.Route == null ? routes.Solution : routes.Betrayals.FirstOrDefault(b => b.Name.StartsWith(row.Route))?.Route;
            if (route == null) { Debug.LogWarning($"Trap Contact Sheet: {row.Level} has no betrayal starting '{row.Route}'."); return (shots, strip); }
            RouteHarness.Replay(session, LevelLayouts.ById[row.Level], route, options);
            for (int i = 0; i < row.States.Count; i++)
                if (!done[i]) Debug.LogWarning($"Trap Contact Sheet: {row.Trap} never reached '{row.States[i].label}' in {row.Level} ({row.Route ?? "solution"}).");
            return (shots, strip);
        }

        // The trap by name, else a bare hazard (static spikes) by name.
        static Transform FindElement(string name)
        {
            RoomTrap trap = TrapShots.FindTrap(name);
            if (trap != null) return trap.transform;
            Hazard hazard = Object.FindObjectsByType<Hazard>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault(h => h.name == name);
            return hazard != null ? hazard.transform : null;
        }

        // The tile's view, 90 px per unit: an arrow from its launcher along its lane, a geyser up its column, the cloud and
        // the ground under it, the door a retreat moves, the cat for a death, anything else around itself.
        static (Vector2, float) Frame(Transform target, RoomTrap trap)
        {
            float view = TileH / TrapShots.PhonePixelsPerUnit;
            if (target == null)
            {
                var cat = Object.FindObjectsByType<CatMotor2D>(FindObjectsSortMode.None).FirstOrDefault();
                return (cat != null ? (Vector2)cat.transform.position : Vector2.zero, view);
            }
            Vector2 at = target.position;
            switch (trap)
            {
                case ArrowTrap arrow:
                    float sign = (ArrowDirection)Field(arrow, "direction") == ArrowDirection.Left ? -1f : 1f;
                    return (at + new Vector2(sign * 1.4f, 0.2f), view);
                case GeyserTrap geyser:
                    return (at + GeyserMath.Push((GeyserDirection)Field(geyser, "direction")) * 1.2f, view);
                case StormCloudTrap storm:
                    return ((Vector2)storm.Cloud.transform.position + new Vector2(0f, -1.6f), view * 1.5f);
                case DoorRetreatTrap retreat:
                    return ((Vector2)((Transform)Field(retreat, "doorRoot")).position, view * 1.3f);
                case MovingTrap:
                    return (at, view * 1.4f);
                default:
                    return (at + new Vector2(0f, -0.5f), view);
            }
        }

        // ---------- the launcher facing both ways ----------

        // Off-screen Editor renders don't draw 2D point lights (only global lights; checked 2026-09-29), so the sheet shows
        // the two facings under the global light, and the flipped normal maps are checked by eye in the Scene view with
        // PARALLAX/Art/Trap Flip Light Check.
        static (List<(string, Texture2D)>, string) CaptureFlipLighting(RouteSession session)
        {
            var config = AssetDatabase.LoadAssetAtPath<TrapArtConfig>(TrapArtSetup.ConfigPath);
            var shots = new List<(string, Texture2D)>();
            session.Clear();
            using (var rig = new TrapShots.Rig())
            {
                rig.Ensure(Backdrop, 1f);
                foreach ((float x, bool left) in new[] { (-1.05f, false), (1.05f, true) })
                {
                    var go = new GameObject("Launcher");
                    rig.Own(go);
                    go.transform.position = new Vector3(x, 0f, 0f);
                    go.transform.localScale = Vector3.one * 3.2f;
                    var r = go.AddComponent<SpriteRenderer>();
                    r.sprite = config.ArrowLauncher; r.sharedMaterial = config.TrapMaterial; r.flipX = left;
                }
                shots.Add(("FACING RIGHT / FACING LEFT", rig.Shot(Vector2.zero, TileH / TrapShots.PhonePixelsPerUnit, TileW, TileH)));
            }
            shots.Add(("LIGHTING: SEE TRAP FLIP LIGHT CHECK", null));
            return (shots, "flipped lighting: check in the Scene view with PARALLAX/Art/Trap Flip Light Check (off-screen renders don't draw 2D point lights)");
        }

        static void SaveStrip(string name, List<Texture2D> frames)
        {
            int w = frames[0].width, h = frames[0].height;
            var strip = new Texture2D(frames.Count * (w + 2) + 2, h + 4, TextureFormat.RGBA32, false);
            TrapShots.Fill(strip, new Color(0.16f, 0.14f, 0.12f, 1f));
            for (int i = 0; i < frames.Count; i++) TrapShots.Blit(strip, frames[i], 2 + i * (w + 2), 2);
            strip.Apply();
            TrapShots.SavePng(strip, StripDir + name + ".png");
            Object.DestroyImmediate(strip);
        }

        static Texture2D Compose(List<(string title, List<(string label, Texture2D tex)> shots)> rows, string title)
        {
            int width = Gap + Columns * (TileW + Gap);
            int rowH = LabelH + LabelH + TileH + Gap;
            int height = 60 + rows.Count * rowH;
            var sheet = new Texture2D(width, height, TextureFormat.RGBA32, false);
            TrapShots.Fill(sheet, new Color(0.16f, 0.14f, 0.12f, 1f));
            TrapShots.Text(sheet, title, Gap, 20, 3, new Color(1f, 0.9f, 0.7f));
            int y = 60;
            foreach ((string rowTitle, List<(string label, Texture2D tex)> shots) in rows)
            {
                Color titleColour = rowTitle.Contains("PLACEHOLDER") ? new Color(1f, 0.55f, 0.45f) : new Color(1f, 0.85f, 0.55f);
                TrapShots.Text(sheet, rowTitle, Gap, y + 6, 2, titleColour);
                int x = Gap;
                foreach ((string label, Texture2D tex) in shots.Take(Columns))
                {
                    TrapShots.Text(sheet, label, x, y + LabelH + 6, 2, new Color(0.9f, 0.9f, 0.85f));
                    if (tex != null) TrapShots.Blit(sheet, tex, x, y + 2 * LabelH);
                    x += TileW + Gap;
                }
                y += rowH;
            }
            sheet.Apply();
            return sheet;
        }
    }
}
