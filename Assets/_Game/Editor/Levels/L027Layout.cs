using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // D-232 (replaces D-171/D-194; the developer, 2026-10-28: "you cant see the ceiling so move it down so its visible", "the
    // checkpoints are not correctly positioned or centered", "I prefer a clear background in this level instead of the shaded part",
    // "level is too short make it longer. Dont add 4 vines in a row. Allow a maximum of 2 in a row then break, some other obstacle
    // or trap", "its not good looking nor was it fun"): level 27, "Updraft", a canyon crossed west to east under a low cave roof
    // (RoofU, always in view). The canyon floor is spikes; vines hang from the roof in pairs, each pair followed by something else.
    //  - Swing (V1, V2): Spin_1 turns between them.
    //  - Crumbles: Pad_3 (a checkpoint), then two crumbling stones over the spikes, GD_5 blowing down between them on its beat.
    //  - V3, V4, then Mid_Ledge: Slam_1 and Slam_2 drop from the roof on their beats (a checkpoint before them).
    //  - V5, V6 with Swing_1 swinging between them, then Ledge_3 (a checkpoint): Inv_1, hidden at its east end, swaps left and right
    //    just before the jump over Pit_3 onto Ledge_4.
    //  - V7, V8 with Spin_2 between them, onto Ledge_E: Slam_D drops from the roof in front of the door as the cat runs at it.
    // Every checkpoint stands at the middle of its pillar (or, on Mid_Ledge, clear of the slams).
    static class L027Layout
    {
        internal const float Canyon = 2f, Base = 0f, Ledge = 5f, PadTop = 4f, East = 6f, RoofU = 10f, RoofTop = 12f, VineLow = 4.5f, Width = 80f;
        // Vine snaps (ticks from the first touch); GD_5's beat; the crumbling stones' delay; the slams' beat.
        internal const int Snap = 45, Beat = 90, GD5Erupt = 30, Crumble = 12, SlamPeriod = 100, SlamMove = 10, SlamHold = 20, SlamBack = 30;
        internal const float V1X = 7.5f, V2X = 11f, V3X = 24.5f, V4X = 28f, V5X = 42.5f, V6X = 46f, V7X = 62.5f, V8X = 66f;
        // The pillars: (name, x0, x1, top). Pad_3, Mid_Ledge, Ledge_3 and Ledge_4 stand in the canyon.
        internal static readonly (string name, float x0, float x1, float top)[] Pillars = {
            ("Pad_3", 14f, 16.5f, PadTop), ("Mid_Ledge", 30f, 40f, Ledge), ("Ledge_3", 48f, 53f, Ledge), ("Ledge_4", 56f, 60f, Ledge),
        };
        // The roof's slams: (name, x0, x1, floor top under it).
        internal static readonly (string name, float x0, float x1, float floor)[] Slams = {
            ("Slam_1", 32.5f, 34f, Ledge), ("Slam_2", 36f, 37.5f, Ledge), ("Slam_D", 73f, 75f, East),
        };

        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,(Base - 1f + RoofTop) * .5f),(1f,RoofTop - Base + 1f)),
                E(SoloRoomElementKind.Wall,"Wall_R",(Width + .5f,(Base - 1f + RoofTop) * .5f),(1f,RoofTop - Base + 1f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(2.5f,Ledge),(0f,0f)),
                E(SoloRoomElementKind.Door,"Door",(78.5f,East + .75f),(.6f,1.5f)),
            };
            // The roof, cut where each slam sits in it.
            float cursor = 0f; int n = 0;
            foreach (var (_, x0, x1, _) in Slams)
            {
                Block(elements, SoloRoomElementKind.Ceiling, n == 0 ? "Roof" : $"Roof_{n}", cursor, x0, RoofU, RoofTop);
                cursor = x1; n++;
            }
            Block(elements, SoloRoomElementKind.Ceiling, $"Roof_{n}", cursor, Width, RoofU, RoofTop);

            // ---- The ground: the west ledge, the canyon (spiked between the pillars), the east ledge ----
            Block(elements, SoloRoomElementKind.Floor, "Ledge_W", 0f, 5f, Base, Ledge);
            Block(elements, SoloRoomElementKind.Floor, "Canyon_Floor", 5f, 68f, Base, Canyon);
            Block(elements, SoloRoomElementKind.Floor, "Ledge_E", 68f, Width, Base, East);
            float spikeFrom = 5f; int s = 1;
            foreach (var (name, x0, x1, top) in Pillars)
            {
                Block(elements, SoloRoomElementKind.Floor, name, x0, x1, Canyon, top);
                Spikes(elements, $"Canyon_{s++}", spikeFrom, x0, Canyon);
                spikeFrom = x1;
            }
            Spikes(elements, $"Canyon_{s}", spikeFrom, 68f, Canyon);

            // ---------- Swing ----------
            Vine(elements, "V1", V1X);
            elements.Add(E(SoloRoomElementKind.Spinner,"Spin_1",((V1X + V2X) * .5f,7.6f),(.5f,.5f),settings:new SoloRoomTrapSettings(new SpinnerSettings(1,1.5f,-2f),new SoloRoomTrapSettings())));
            Vine(elements, "V2", V2X);

            // ---------- Crumbles ----------
            // Two stones over the spikes, each crumbling Crumble ticks after a cat lands; GD_5, flush in the roof over the gap between
            // them, blows down onto it on its beat.
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Crumb_1",(18.75f,PadTop - .5f),(1.5f,1f),settings:new SoloRoomTrapSettings(delayTicks:Crumble)));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Crumb_2",(21.75f,PadTop - .5f),(1.5f,1f),settings:new SoloRoomTrapSettings(delayTicks:Crumble)));
            elements.Add(E(SoloRoomElementKind.Geyser,"GD_5",(20.25f,RoofU + .15f),(1f,.3f),settings:Geyser(Beat, GD5Erupt, RoofU - PadTop - .5f, GeyserDirection.Down)));
            Vine(elements, "V3", V3X);
            Vine(elements, "V4", V4X);

            // ---------- Crushers ----------
            // Slam_1 and Slam_2 drop from the roof onto Mid_Ledge (0.4 left) on one beat, half a beat apart; Slam_D, by the door, drops
            // once a cat passes x 71 (its trigger), timed to meet a cat running on.
            for (int i = 0; i < 2; i++)
            {
                var (name, x0, x1, floor) = Slams[i];
                elements.Add(E(SoloRoomElementKind.MovingTrap, name, ((x0 + x1) * .5f, (RoofU + RoofTop) * .5f), (x1 - x0, RoofTop - RoofU), settings: new SoloRoomTrapSettings(
                    new MovingFloorSettings(SurfaceMotion.Carry, slams: true),
                    new SoloRoomTrapSettings(offset: new Vector2(0f, floor + .4f - RoofU), moveTicks: SlamMove, holdTicks: SlamHold, returnTicks: SlamBack,
                        repeatMode: TrapRepeatMode.Periodic, periodTicks: SlamPeriod, phaseTicks: i * SlamPeriod / 2, cooldownTicks: SlamMove + SlamHold + SlamBack,
                        movingKind: MovingTrapKind.Solid))));
            }
            {
                var (name, x0, x1, floor) = Slams[2];
                elements.Add(E(SoloRoomElementKind.MovingTrap, name, ((x0 + x1) * .5f, (RoofU + RoofTop) * .5f), (x1 - x0, RoofTop - RoofU),
                    (71f, East + 1.5f), (.4f, 3f), new SoloRoomTrapSettings(
                        new MovingFloorSettings(SurfaceMotion.Carry, slams: true),
                        new SoloRoomTrapSettings(delayTicks: 14, offset: new Vector2(0f, floor + .4f - RoofU), moveTicks: 8, holdTicks: 40, returnTicks: 30,
                            repeatMode: TrapRepeatMode.Rearm, cooldownTicks: 8 + 40 + 30, movingKind: MovingTrapKind.Solid))));
            }

            // ---------- Pendulum ----------
            Vine(elements, "V5", V5X);
            elements.Add(E(SoloRoomElementKind.Spinner,"Swing_1",((V5X + V6X) * .5f,RoofU - .4f),(.5f,.5f),settings:new SoloRoomTrapSettings(SpinnerSettings.Pendulum(3.2f, 60f, 120),new SoloRoomTrapSettings())));
            Vine(elements, "V6", V6X);

            // ---------- Inverter ----------
            // Inv_1, hidden at Ledge_3's east end, swaps left and right for 45 ticks as the cat comes to jump Pit_3 (Canyon_4).
            elements.Add(E(SoloRoomElementKind.Inverter,"Inv_1",(52.3f,Ledge + 1f),(.4f,2f),settings:new SoloRoomTrapSettings(new InverterSettings(45, disguised: true),new SoloRoomTrapSettings(delayTicks:0))));

            // ---------- Last vines ----------
            Vine(elements, "V7", V7X);
            elements.Add(E(SoloRoomElementKind.Spinner,"Spin_2",((V7X + V8X) * .5f,7.4f),(.5f,.5f),settings:new SoloRoomTrapSettings(new SpinnerSettings(1,1.4f,3f),new SoloRoomTrapSettings())));
            Vine(elements, "V8", V8X);

            var sections = new[] {
                CheckpointSection.Start("Canyon", new Vector2(2.5f, Ledge), new[] { "V1", "Spin_1", "V2" }),
                new CheckpointSection("Crumbles", new Vector2(15.25f, PadTop), new Rect(14f, PadTop + 1f, 2.5f, .2f), new[] { "Crumb_1", "Crumb_2", "GD_5", "V3", "V4" }),
                new CheckpointSection("Crushers", new Vector2(31.25f, Ledge), new Rect(30.2f, Ledge, .3f, 1.5f), new[] { "Slam_1", "Slam_2", "V5", "Swing_1", "V6" }),
                new CheckpointSection("Last", new Vector2(50.5f, Ledge), new Rect(48.2f, Ledge, .3f, 1.5f), new[] { "Inv_1", "V7", "Spin_2", "V8", "Slam_D" }),
            };
            return new SoloRoomDefinition(0, 0f, Width, elements.ToArray(), System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>(), null, null, null, sections);
        }

        static void Block(List<SoloRoomElement> e, SoloRoomElementKind kind, string name, float x0, float x1, float y0, float y1) =>
            e.Add(E(kind, name, ((x0 + x1) * .5f, (y0 + y1) * .5f), (x1 - x0, y1 - y0)));

        // An honest spike strip on a floor's top.
        static void Spikes(List<SoloRoomElement> e, string name, float x0, float x1, float face) =>
            e.Add(E(SoloRoomElementKind.Hazard, name, ((x0 + x1) * .5f, face + .15f), (x1 - x0, .3f)));

        // A snap vine at x hanging from the roof down to VineLow, snapping Snap ticks after the cat first touches it.
        static void Vine(List<SoloRoomElement> e, string name, float x) =>
            e.Add(E(SoloRoomElementKind.Vine, name, (x, (VineLow + RoofU - .05f) * .5f), (.6f, RoofU - .05f - VineLow), settings: new SoloRoomTrapSettings(delayTicks: Snap)));

        // A geyser (defaults: tell 25, erupt 40) whose eruption starts at room tick `erupt`, and every `period` ticks.
        static SoloRoomTrapSettings Geyser(int period, int erupt, float column, GeyserDirection direction = GeyserDirection.Up) =>
            new(new GeyserSettings(direction, columnHeight: column), new SoloRoomTrapSettings(repeatMode: TrapRepeatMode.Periodic, periodTicks: period,
                phaseTicks: Mod(erupt - GeyserMath.DefaultTellTicks, period)));

        static int Mod(int a, int m) => ((a % m) + m) % m;
    }
}
