using System;
using System.Collections.Generic;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Levels;
using Parallax.Editor.Setup;
using UnityEngine;

namespace Parallax.Editor.Art
{
    // PAX-V07 item 2: the air scenarios (takeoff, rise, apex, fall, land, hard land), each in both gravities, and the drop
    // measurement. Most run in a capture-only bench room (never a level, never saved): a floor and a ceiling 7 u apart
    // with a low slab, a 1 u ledge and a 3.2 u tower; its gravity-up twin mirrors every block top to bottom, so the same
    // screen-relative script meets the same geometry on the ceiling. The geyser runs in Trap Lab room 8 (gravity down) and in
    // the bench with a ceiling vent erupting downward (gravity up).
    static partial class CatCaptureScenarios
    {
        const float BenchWidth = 40f, BenchHeight = 7f;   // the floor's top at 0, the ceiling's underside at 7

        // The bench's blocks (gravity down): a low slab over x 3-6 (underside 1.3: a jump bumps its head 0.74 u up), a 1 u
        // ledge over x 14-17 (a jump lands on it, its far end is a 1 u drop), a 3.2 u tower over x 28-31 (a drop off it is
        // past hardLandDistance, a jump off it falls 4.8 u).
        static readonly (string name, Vector2 centre, Vector2 size)[] BenchBlocks =
        {
            ("LowSlab", new Vector2(4.5f, 1.55f), new Vector2(3f, .5f)),
            ("Ledge", new Vector2(15.5f, .5f), new Vector2(3f, 1f)),
            ("Tower", new Vector2(29.5f, 1.6f), new Vector2(3f, 3.2f)),
        };

        /// <summary>The bench. Gravity up mirrors every block (y -> 7 - y) and keeps the checkpoint on the floor: the cat
        /// flips there and falls onto the mirrored surface above it. `checkpointY` (gravity down only) puts the start on a
        /// block's top, e.g. the tower's.</summary>
        static SoloRoomDefinition Bench(bool up, float checkpointX = 2f, float checkpointY = 0f, bool geyser = false)
        {
            float Y(float y) => up ? BenchHeight - y : y;
            var elements = new List<SoloRoomElement>
            {
                Element(SoloRoomElementKind.Floor, "Floor", new Vector2(BenchWidth * .5f, -.5f), new Vector2(BenchWidth, 1f)),
                Element(SoloRoomElementKind.Ceiling, "Ceiling", new Vector2(BenchWidth * .5f, BenchHeight + .5f), new Vector2(BenchWidth, 1f)),
                Element(SoloRoomElementKind.Checkpoint, "Checkpoint", new Vector2(checkpointX, up ? 0f : checkpointY), Vector2.zero),
                Element(SoloRoomElementKind.Door, "Door", new Vector2(BenchWidth - .5f, .75f), new Vector2(.6f, 1.5f)),
            };
            foreach (var b in BenchBlocks) elements.Add(Element(SoloRoomElementKind.Wall, b.name, new Vector2(b.centre.x, Y(b.centre.y)), b.size));
            // A vent in the surface the cat stands on (x 9), erupting every 100 ticks from room tick 80 as room 8's does:
            // the floor's pushing up, or (gravity up) the ceiling's pushing down.
            if (geyser)
                elements.Add(Element(SoloRoomElementKind.Geyser, "Geyser", new Vector2(9f, up ? BenchHeight + .15f : -.15f), new Vector2(1f, .3f),
                    new SoloRoomTrapSettings(new GeyserSettings(up ? GeyserDirection.Down : GeyserDirection.Up),
                        new SoloRoomTrapSettings(repeatMode: TrapRepeatMode.Periodic, periodTicks: 100, phaseTicks: 80))));
            return new SoloRoomDefinition(0, 0f, BenchWidth, elements.ToArray(), Array.Empty<SoloRoomOpening>(), Array.Empty<RequiredJump>());
        }

        static SoloRoomElement Element(SoloRoomElementKind kind, string name, Vector2 position, Vector2 size, SoloRoomTrapSettings settings = default) =>
            new(kind, name, position, size, Vector2.zero, Vector2.zero, settings);

        /// <summary>Gravity up for the air scenarios: the flip and the fall onto the surface above are part of the capture
        /// (a long fall and a landing: checked, unlike the ground scenarios' setup), then `steps`.</summary>
        static List<CaptureStep> UpAir(List<CaptureStep> steps)
        {
            var list = new List<CaptureStep>
            {
                new Act(r => r.Gravity.Flip(), "flip gravity"),
                new Until(0f, Airborne, 0.5f, "fall to the surface above") { Cut = true },
                new Until(0f, Grounded, 3f, "fall to the surface above"),
                new Hold(0f, 0.8f, "settle 0.8 s"),
            };
            list.AddRange(steps);
            return list;
        }

        static IEnumerable<CaptureScenario> AirPair(string name, string what, Func<List<CaptureStep>> script, float startX,
            Func<bool, SoloRoomDefinition> room = null, string where = "the air bench")
        {
            room ??= up => Bench(up);
            yield return new CaptureScenario { Name = name + "_down", Description = $"{where}, gravity down: {what}", Room = _ => room(false), StartX = startX, Steps = _ => script() };
            yield return new CaptureScenario { Name = name + "_up", Description = $"{where} mirrored, gravity up (on the ceiling): {what}", Room = _ => room(true), StartX = startX, Steps = _ => UpAir(script()) };
        }

        static List<CaptureStep> Land(float move, string label) => new()
        {
            new Until(move, Airborne, 0.2f, "take off"),
            new Until(move, Grounded, 3f, label),
        };

        // ---------- the scripts (screen-relative) ----------

        static List<CaptureStep> StandingJumps() => new List<CaptureStep> { new Hold(0f, 0.5f, "stand 0.5 s"), new Press(0f, "jump in place") }
            .Concat(Land(0f, "until landed")).Concat(new CaptureStep[] { new Hold(0f, 0.8f, "stand 0.8 s"), new Press(0f, "jump in place again") })
            .Concat(Land(0f, "until landed")).Append(new Hold(0f, 0.8f, "stand 0.8 s")).ToList();

        static List<CaptureStep> RunningJumps() => new List<CaptureStep>
            {
                new Hold(0f, 0.4f, "stand 0.4 s"),
                new Hold(1f, 0.5f, "run right 0.5 s") { Cut = true },
                new Press(1f, "jump while running, keep running"),
            }
            .Concat(Land(1f, "until landed running"))
            .Concat(new CaptureStep[] { new Hold(1f, 0.1f, "run on 0.1 s"), new Until(0f, Still, 1f, "release: stop") { Cut = true }, new Hold(0f, 0.4f, "stand"),
                new Hold(-1f, 0.5f, "run left 0.5 s"), new Press(-1f, "jump while running left") , new Until(-1f, Airborne, 0.2f, "take off"),
                new Hold(0f, 0.1f, "release in the air") })
            .Concat(new CaptureStep[] { new Until(0f, Grounded, 3f, "until landed (still)"), new Hold(0f, 0.6f, "stand"),
                new Hold(-0.45f, 0.5f, "walk left (0.45) 0.5 s") { Cut = true }, new Press(-0.45f, "jump while walking left") })
            .Concat(Land(-0.45f, "until landed walking"))
            .Concat(new CaptureStep[] { new Hold(-0.45f, 0.3f, "walk on"), new Until(0f, Still, 1f, "release: stop"), new Hold(0f, 0.5f, "stand") })
            .ToList();

        // From x 12.5 (1 u short of the ledge), a jump holding right lands on the ledge; then a slow walk off its far end (1 u).
        static List<CaptureStep> LedgeUpAndOff() => new List<CaptureStep> { new Hold(0f, 0.4f, "stand 0.4 s"), new Press(1f, "jump right onto the 1 u ledge") }
            .Concat(Land(1f, "until landed on the ledge"))
            .Concat(new CaptureStep[] { new Until(0f, Still, 1f, "release: stop"), new Hold(0f, 0.6f, "stand on the ledge"),
                new Until(0.4f, Airborne, 3f, "walk right (0.4) off the ledge's end") { Cut = true }, new Until(0f, Grounded, 3f, "release: the 1 u drop"),
                new Hold(0f, 0.8f, "stand") })
            .ToList();

        // On the tower's top: a slow walk off its left edge (3.2 u), released in the air.
        static List<CaptureStep> TowerWalkOff() => new()
        {
            new Hold(0f, 0.4f, "stand 0.4 s on the tower"),
            new Until(-0.4f, Airborne, 3f, "walk left (0.4) off the tower") { Cut = true },
            new Until(0f, Grounded, 3f, "release: the 3.2 u drop"),
            new Hold(0f, 1f, "stand 1 s"),
            new Hold(1f, 0.3f, "run right into the tower's wall"),
            new Until(0f, Still, 1f, "release"),
            new Hold(0f, 0.3f, "stand"),
        };

        // On the tower's top: a run left off it (a running landing), then back.
        static List<CaptureStep> TowerRunOff() => new()
        {
            new Hold(0f, 0.4f, "stand 0.4 s on the tower"),
            new Until(-1f, Airborne, 3f, "run left off the tower") { Cut = true },
            new Until(-1f, Grounded, 3f, "keep running: a running landing after 3.2 u"),
            new Hold(-1f, 0.3f, "run on"),
            new Until(0f, Still, 1f, "release: stop"),
            new Hold(0f, 0.5f, "stand"),
        };

        // On the tower's top: a short walk right (so the cat faces screen-right in both gravities), then a jump left off it
        // (a jump pressed against the facing; a 4.8 u fall), steering left then released.
        static List<CaptureStep> TowerJumpOff() => new()
        {
            new Hold(0f, 0.3f, "stand 0.3 s on the tower"),
            new Hold(0.4f, 0.3f, "walk right (0.4) 0.3 s: face screen-right"),
            new Until(0f, Still, 1f, "stop"),
            new Hold(0f, 0.4f, "stand 0.4 s"),
            new Press(-1f, "jump left off the tower (against the facing)"),
            new Hold(-1f, 0.45f, "steer left 0.45 s"),
            new Until(0f, Grounded, 3f, "release: the 4.8 u fall"),
            new Hold(0f, 1f, "stand 1 s"),
        };

        // Under the low slab (headroom 1.3 u): a jump in place bumps its head; then a jump while walking left out from under it.
        // From x 8 (clear of the slab, so gravity up lands on the ceiling, not on the slab), a walk left under it first.
        static List<CaptureStep> LowCeiling() => new List<CaptureStep> { new Hold(0f, 0.3f, "stand 0.3 s"),
                new Until(-0.4f, r => r.ColliderCentre.x - r.Origin.x <= 4.7f, 3f, "walk left (0.4) under the slab"), new Until(0f, Still, 1f, "stop"),
                new Hold(0f, 0.4f, "stand 0.4 s under the slab"), new Press(0f, "jump into the low ceiling") }
            .Concat(Land(0f, "until landed")).Concat(new CaptureStep[] { new Hold(0f, 0.6f, "stand"), new Hold(0.45f, 0.2f, "walk right (0.45)"), new Press(0.45f, "jump while walking under the slab") })
            .Concat(Land(0.45f, "until landed")).Concat(new CaptureStep[] { new Until(0f, Still, 1f, "release: stop"), new Hold(0f, 0.5f, "stand") })
            .ToList();

        // On the vent: stand through the tell until the eruption launches the cat (Rise, no Launched state), fall back, then
        // walk left off the vent before the next eruption.
        static List<CaptureStep> Geyser() => new()
        {
            new Hold(0f, 1f, "stand on the vent (the tell)"),
            new Until(0f, Airborne, 3f, "stand on the vent until it erupts") { Cut = true },
            new Until(0f, Grounded, 3f, "the launch and the fall back"),
            new Hold(0f, 0.3f, "stand"),
            new Hold(-1f, 0.5f, "run left off the vent, clear of the next eruption"),
            new Until(0f, Still, 1f, "release"),
            new Hold(0f, 0.3f, "stand"),
        };

        // PAX-V07 §3, §6: inputs pressed during TakeOff, Land and HardLand (CatVisualOnlyParityTests; not for the critic). On the
        // tower: a jump reversed at once (during TakeOff), a jump in place with a move on the landing tick (during Land), a
        // walk off the tower with a jump on the landing tick (during HardLand), a jump with a reversal on the landing tick.
        static List<CaptureStep> ParityAir() => new()
        {
            new Hold(0f, 0.3f, "stand on the tower"),
            new Press(0.4f, "jump right"),
            new Hold(-0.4f, 0.04f, "reverse during TakeOff"),
            new Until(0f, Grounded, 2f, "until landed"),
            new Hold(0f, 0.3f, "stand"),
            new Press(0f, "jump in place"),
            new Until(0f, Airborne, 0.2f, "take off"),
            new Until(0f, Grounded, 2f, "until landed"),
            new Hold(0.5f, 0.1f, "walk during Land"),
            new Until(0f, Still, 1f, "stop"),
            new Hold(0f, 0.3f, "stand"),
            new Until(-0.4f, Airborne, 3f, "walk left off the tower"),
            new Until(0f, Grounded, 3f, "release: the drop"),
            new Hold(0f, 0.06f, "HardLand"),
            new Press(0f, "jump during HardLand"),
            new Until(0f, Airborne, 0.2f, "take off"),
            new Until(0f, Grounded, 2f, "until landed"),
            new Hold(0f, 0.04f, "Land"),
            new Hold(-0.5f, 0.2f, "walk left during Land"),
            new Press(1f, "jump reversing"),
            new Until(1f, Airborne, 0.2f, "take off"),
            new Until(1f, Grounded, 2f, "until landed"),
            new Hold(-1f, 0.2f, "reverse on the landing tick"),
            new Until(0f, Still, 1f, "stop"),
            new Hold(0f, 0.3f, "stand"),
        };

        static IEnumerable<CaptureScenario> AirScenarios()
        {
            Func<bool, SoloRoomDefinition> tower = up => Bench(up, 29.5f, 3.2f);
            return AirPair("air_standing_jump", "two jumps in place", StandingJumps, 9f)
                .Concat(AirPair("air_running_jump", "a running jump landing running, a running jump released in the air, a walking jump", RunningJumps, 18.5f))
                .Concat(AirPair("air_ledge", "a jump onto the 1 u ledge, then a slow walk off its far end (a 1 u drop)", LedgeUpAndOff, 12.5f))
                .Concat(AirPair("air_tower_drop", "a slow walk off the 3.2 u tower, released in the air (a hard landing)", TowerWalkOff, 29.5f, tower))
                .Concat(AirPair("air_tower_run", "a run off the 3.2 u tower, still running at touchdown", TowerRunOff, 29.5f, tower))
                .Concat(AirPair("air_tower_jump", "facing right on the 3.2 u tower, a jump left off it (a 4.8 u fall), released in the air", TowerJumpOff, 29.5f, tower))
                .Concat(AirPair("air_low_ceiling", "a walk under the low slab (headroom 1.3 u), a jump into it, then a walking jump out from under it", LowCeiling, 8f))
                .Append(new CaptureScenario
                {
                    Name = "air_geyser_down", Description = "Trap Lab room 8, gravity down: stand on the geyser's vent until it erupts; the launch, the fall back, walk away",
                    Room = _ => TrapLabLayout.Rooms[8], StartX = 9f, Steps = _ => Geyser(),
                })
                .Append(new CaptureScenario
                {
                    Name = "air_geyser_up", Description = "The air bench mirrored with a ceiling vent erupting downward (room 8's timing), gravity up: stand on the vent until it erupts; the launch, the fall back, walk away",
                    Room = _ => Bench(true, geyser: true), StartX = 9f, Steps = _ => UpAir(Geyser()),
                })
                .Concat(AirPair("parity_air", "parity script: inputs during TakeOff, Land and HardLand", ParityAir, 29.5f, tower));
        }

        /// <summary>Not critic scenarios: every level's solution, replayed so the landings' fall distances can be measured
        /// (item 2 picks hardLandDistance from them). Only run by a filter starting with "measure_".</summary>
        public static readonly List<CaptureScenario> Measure = LevelLayouts.ById.Keys.Where(id => LevelRoutes.ById.ContainsKey(id)).OrderBy(id => id)
            .Select(id => new CaptureScenario
            {
                Name = $"measure_{id}_down", Description = $"{id}'s solution route (drop measurement only)",
                Room = _ => LevelLayouts.ById[id],
                Steps = session => new List<CaptureStep> { new Replay(RouteCommands(session, id), id + " solution"), new Hold(0f, 0.3f, "after") },
            }).ToList();
    }
}
