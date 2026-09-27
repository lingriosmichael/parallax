using System.Collections.Generic;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Setup;
using UnityEngine;
using static Parallax.Editor.Routes.R;

namespace Parallax.Editor.Routes
{
    // PAX-060 (D-093): synthetic rooms, routes and replays for Band2RulesTests. The routes are never replayed (the content
    // rules read their declarations); the replays are built record by record, so the chaos and element rules can be seen
    // red on exact inputs.
    public static class Band2Fixtures
    {
        static readonly string[] Codes = { "J", "NJ", "W", "NW", "SS", "B", "OL", "LW" };

        // A 30 u floor room with `sections` checkpoint sections (gates every 8 u) and, optionally, one precision section
        // holding `precisionJumps` required jumps.
        public static SoloRoomDefinition Room(int sections, bool precision, int precisionJumps)
        {
            var elements = new List<SoloRoomElement> {
                new(SoloRoomElementKind.Ceiling, "Ceiling", new Vector2(15f, 7.5f), new Vector2(30f, 1f)),
                new(SoloRoomElementKind.Checkpoint, "Checkpoint", new Vector2(2f, 0f), Vector2.zero),
                new(SoloRoomElementKind.Floor, "Floor", new Vector2(15f, -.5f), new Vector2(30f, 1f)),
                new(SoloRoomElementKind.Door, "Door", new Vector2(28f, .75f), new Vector2(.6f, 1.5f)),
            };
            var list = new List<CheckpointSection>();
            for (int i = 0; i < sections; i++)
                list.Add(i == 0 ? CheckpointSection.Start("S0", new Vector2(2f, 0f), System.Array.Empty<string>())
                    : new CheckpointSection("S" + i, new Vector2(2f + 8f * i + 1f, 0f), new Rect(2f + 8f * i, -1f, .2f, 8f), System.Array.Empty<string>()));
            var jumps = new List<RequiredJump>();
            for (int i = 0; i < precisionJumps; i++)
                jumps.Add(new RequiredJump("Floor", RequiredJumpKind.Pit, RequiredJumpFrame.Floor, RequiredJumpDirection.Right, 12f + 3f * i, 14f + 3f * i, 0f, 0f, 1f));
            PrecisionSection[] precise = precision ? new[] { new PrecisionSection("Run", Rect.MinMaxRect(10f, -1f, 26f, 3f)) } : null;
            return new SoloRoomDefinition(0, 0f, 30f, elements.ToArray(), System.Array.Empty<SoloRoomOpening>(), jumps.ToArray(), null, precise, null, list.ToArray());
        }

        // Room(2, false, 0) with its door at x `doorX` (the start is at x 2 in a 30 u room, 7 u high).
        public static SoloRoomDefinition RoomWithDoor(float doorX)
        {
            SoloRoomDefinition r = Room(2, false, 0);
            SoloRoomElement[] elements = r.Elements.Select(e => e.Kind == SoloRoomElementKind.Door ? new SoloRoomElement(e.Kind, e.Name, new Vector2(doorX, e.Position.y), e.Size) : e).ToArray();
            return new SoloRoomDefinition(0, 0f, 30f, elements, r.Openings, r.RequiredJumps, null, r.PrecisionSections, null, r.CheckpointSections);
        }

        // `chain` betrayals in sequence (each shares a longer start of the solution) with distinct killers; `side` more
        // lethal ones that share the first's start (so they aren't in sequence); `diesDeadEnds` "Dead end..." Dies routes;
        // `recovers` Recovers routes. Every trap gets its own answer code, except that the first `repeatJ` traps all answer J
        // and the first `bait` traps answer BAIT; `untagged` drops the tag from the first betrayal.
        public static RoomRoutes Routes(int chain, int side, int diesDeadEnds, int recovers, int repeatJ, int bait, bool untagged)
        {
            var steps = new List<RouteStep> { Hold(Right) };
            for (int k = 1; k <= 12; k++) steps.Add(Until(XAtLeast(k)));
            steps.Add(Until(RoomComplete()));
            var solution = new Route("band2 solution", steps.ToArray());
            var betrayals = new List<Betrayal>();
            int trap = 0;
            string Tag(string label)
            {
                string code = trap < bait ? "BAIT" : trap < repeatJ ? "J" : Codes[trap % Codes.Length];
                string name = trap == 0 && untagged ? $"{label}: untagged" : $"{label} [{code}]: trap {trap}";
                trap++;
                return name;
            }
            for (int i = 0; i < chain; i++)
                betrayals.Add(new Betrayal(Tag($"T{i}"), $"Killer_{i}", DeathCause.Hazard, Route.PrefixOf(solution, $"Until(X>={i + 2})", $"chain {i}", Until(Dead()))));
            for (int i = 0; i < side; i++)
                betrayals.Add(new Betrayal(Tag($"S{i}"), $"Side_{i}", DeathCause.Hazard, Route.PrefixOf(solution, "Until(X>=2)", $"side {i}", Release(), Until(Dead()))));
            for (int i = 0; i < diesDeadEnds; i++)
                betrayals.Add(new Betrayal($"Dead end D{i} [OL]: side", $"DeadEnd_{i}", DeathCause.Hazard, Route.PrefixOf(solution, "Until(X>=1)", $"dead end {i}", Release(), Until(Dead()))));
            for (int i = 0; i < recovers; i++)
                betrayals.Add(Betrayal.Recovers($"Dead end R{i} [LW]: recovers", "Door", Route.PrefixOf(solution, "Until(X>=1)", $"recovers {i}", Until(RoomComplete()))));
            return new RoomRoutes(solution, betrayals.ToArray());
        }

        // A solution replay over 200 ticks: element E<i> changes look (for one tick, then holds) at onsets[i]. The door,
        // a gate marker and the cat's extras change at `extraTick` (-1: never); the rules must not count them.
        public static ReplayResult ChaosReplay(int[] onsets, int extraTick)
        {
            var names = onsets.Select((_, i) => "E" + i).Concat(new[] { "Door", "S1_Marker", CatGravity, CatInverted }).ToList();
            int[] at = onsets.Concat(Enumerable.Repeat(extraTick, 4)).ToArray();
            var replay = new ReplayResult { Route = "chaos" };
            replay.Elements.AddRange(names);
            for (int t = 0; t <= 200; t++)
            {
                var r = new TickRecord { Tick = t, FireTick = new int[names.Count], Signature = new int[names.Count], Rendered = new bool[names.Count], RenderBounds = new Rect[names.Count] };
                for (int e = 0; e < names.Count; e++) { r.Signature[e] = at[e] >= 0 && t >= at[e] ? 1 : 0; r.Rendered[e] = true; r.RenderBounds[e] = new Rect(e, 0f, 1f, 1f); }
                replay.Records.Add(r);
            }
            return replay;
        }

        public static SoloRoomDefinition ChaosRoom() => Room(2, false, 0);

        // Level 14's element rule: a room with two vines, a solution, and `climbing` + `killedByVine` + `plain` betrayals.
        // A climbing betrayal's own part of its replay shows the cat on a vine; a killedByVine one names a vine as its
        // reveal; a plain one does neither.
        public static SoloRoomDefinition VineRoom()
        {
            SoloRoomDefinition r = Room(2, false, 0);
            var elements = r.Elements.ToList();
            elements.Add(new SoloRoomElement(SoloRoomElementKind.Vine, "Vine_A", new Vector2(10f, 2f), new Vector2(.6f, 4f)));
            elements.Add(new SoloRoomElement(SoloRoomElementKind.Vine, "Vine_B", new Vector2(20f, 2f), new Vector2(.6f, 4f)));
            return new SoloRoomDefinition(0, 0f, 30f, elements.ToArray(), r.Openings, r.RequiredJumps, null, null, null, r.CheckpointSections);
        }

        public static RoomRoutes VineRoutes(int climbing, int revealedByVine, int plain)
        {
            var solution = new Route("vine solution", Hold(Right), Until(XAtLeast(1f)), Until(RoomComplete()));
            var betrayals = new List<Betrayal>();
            for (int i = 0; i < climbing; i++) betrayals.Add(new Betrayal($"C{i} [J]: climbs", $"Pit_C{i}", DeathCause.Hazard, new Route($"climb {i}", Hold(Up), Until(Dead()))));
            for (int i = 0; i < revealedByVine; i++) betrayals.Add(new Betrayal($"V{i} [B]: snaps", $"Pit_V{i}", DeathCause.Hazard, new Route($"snap {i}", Hold(Up), Until(Dead())), revealedBy: "Vine_A"));
            for (int i = 0; i < plain; i++) betrayals.Add(new Betrayal($"P{i} [W]: walks", $"Pit_P{i}", DeathCause.Hazard, new Route($"walk {i}", Hold(Right), Until(Dead()))));
            return new RoomRoutes(solution, betrayals.ToArray());
        }

        // PAX-060 review: one element of level 11's, 12's or 13's kind in the fixture room (a spear, an inverter, a geyser),
        // named "Elem" so no betrayal counts it by killer or reveal.
        public static SoloRoomDefinition ElementRoom(int level)
        {
            SoloRoomDefinition r = Room(2, false, 0);
            var elements = r.Elements.ToList();
            elements.Add(level switch
            {
                11 => new SoloRoomElement(SoloRoomElementKind.Arrow, "Elem", new Vector2(10f, 1f), new Vector2(.5f, .4f),
                    settings: new SoloRoomTrapSettings(ArrowLane.SpearLane(ArrowDirection.Right, 1f, 20f), new SoloRoomTrapSettings(delayTicks: 0))),
                12 => new SoloRoomElement(SoloRoomElementKind.Inverter, "Elem", new Vector2(10f, 1.5f), new Vector2(.6f, 3f),
                    settings: new SoloRoomTrapSettings(new InverterSettings(), new SoloRoomTrapSettings(delayTicks: 0))),
                _ => new SoloRoomElement(SoloRoomElementKind.Geyser, "Elem", new Vector2(10f, -.15f), new Vector2(1f, .3f),
                    settings: new SoloRoomTrapSettings(new GeyserSettings(), new SoloRoomTrapSettings(repeatMode: TrapRepeatMode.Periodic, periodTicks: 100))),
            });
            return new SoloRoomDefinition(0, 0f, 30f, elements.ToArray(), r.Openings, r.RequiredJumps, null, null, null, r.CheckpointSections);
        }

        // `acting` betrayals whose replays show the element acting on the cat ("A"), and `plain` ones that don't ("P").
        public static RoomRoutes ElementRoutes(int acting, int plain)
        {
            var solution = new Route("element solution", Hold(Right), Until(XAtLeast(1f)), Until(RoomComplete()));
            var betrayals = new List<Betrayal>();
            for (int i = 0; i < acting; i++) betrayals.Add(new Betrayal($"A{i} [J]: meets it", $"Pit_A{i}", DeathCause.Hazard, new Route($"act {i}", Jump(), Until(Dead()))));
            for (int i = 0; i < plain; i++) betrayals.Add(new Betrayal($"P{i} [W]: walks", $"Pit_P{i}", DeathCause.Hazard, new Route($"walk {i}", Hold(Right), Until(Dead()))));
            return new RoomRoutes(solution, betrayals.ToArray());
        }

        // Ten ticks each. On tick 5 of an "A" betrayal: level 11, the cat stands on Elem_Shaft; level 12, the inversion's
        // signature changes; level 13, the cat rises at 14 u/s inside Elem's drawn bounds.
        public static Dictionary<string, ReplayResult> ElementReplays(int level, RoomRoutes routes)
        {
            var replays = new Dictionary<string, ReplayResult>();
            foreach (Betrayal b in routes.Betrayals)
            {
                var replay = new ReplayResult { Route = b.Route.Name };
                replay.Elements.AddRange(new[] { "Elem", CatGravity, CatInverted });
                for (int t = 0; t <= 10; t++)
                {
                    bool acts = b.Name.StartsWith("A") && t == 5;
                    var record = new TickRecord { Tick = t, X = 10f, Y = .3f, Grounded = true, Ground = "Floor", FireTick = new int[3], Signature = new int[3], Rendered = new bool[3], RenderBounds = new Rect[3] };
                    record.Rendered[0] = true; record.RenderBounds[0] = new Rect(9.5f, 0f, 1f, 1.5f);
                    if (acts && level == 11) record.Ground = "Elem_Shaft";
                    if (acts && level == 12) record.Signature[2] = 1;
                    if (acts && level == 13) { record.Grounded = false; record.Ground = null; record.Vy = 14f; }
                    replay.Records.Add(record);
                }
                replays[b.Name] = replay;
            }
            return replays;
        }

        // Each betrayal's replay: ten ticks, climbing on tick 5 for the "C" ones.
        public static Dictionary<string, ReplayResult> VineReplays(RoomRoutes routes)
        {
            var replays = new Dictionary<string, ReplayResult>();
            foreach (Betrayal b in routes.Betrayals)
            {
                var replay = new ReplayResult { Route = b.Route.Name };
                replay.Elements.AddRange(new[] { "Vine_A", "Vine_B", CatGravity, CatInverted });
                for (int t = 0; t <= 10; t++)
                    replay.Records.Add(new TickRecord { Tick = t, IsClimbing = b.Name.StartsWith("C") && t == 5, FireTick = new int[4], Signature = new int[4], Rendered = new bool[4], RenderBounds = new Rect[4] });
                replays[b.Name] = replay;
            }
            return replays;
        }
    }
}
