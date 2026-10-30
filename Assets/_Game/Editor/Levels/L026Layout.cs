using System.Collections.Generic;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // D-220 (replaces D-180; the developer, 2026-10-28: "I like that the level forces you to go down, to have to invert yourself
    // and go back to get to the door. What I dont like are the traps and dieing mecahnics. There are no puzzles. MAybe we can use
    // one of the already implemented mecahnic of stepping on something that triggers walls moving", and earlier "I want it to go
    // 3 floors down"): level 26, "Wrong Way", rebuilt as three corridors stacked under each other, crossed by pressure plates
    // (PAX-119) that move the walls, gates and hatches. Down floor by floor, flip onto the ceiling, and back up the same way upside
    // down, pressing plates on the ceilings, to the door that was in sight from the start.
    //  - F1 (top, walked east): the door is in sight to the west behind Gate_D. Plate_1 opens Hatch_1 in the floor further on:
    //    down through it.
    //  - F2 (walked west): Plate_2A lifts Gate_2 for a while (it slams shut again: not under it then); Plate_2B opens Hatch_2 in the
    //    floor near the west end for a while: get there in time, down through it.
    //  - F3 (the bottom): Flip_U puts the cat on the ceiling. Plate_C1, in the ceiling, opens Hatch_3 above: fall up through it.
    //  - F2 again, upside down (walked east): Plate_C2, in its ceiling, opens Hatch_4: fall up through it.
    //  - F1 again, upside down (walked west): Plate_D, in its ceiling, drops Gate_D; Flip_D turns the cat upright by the door.
    // D-221 (the developer, after a first play: "I completed with 0 deaths. There are no dead ends, and nothing that kills me. No
    // troll deaths"): the plates and the room's own pieces betray the cat.
    //  - Plate_S, in front of the door's gate (the first thing a cat walks to), drops Slam_S, a piece of the roof, onto it.
    //  (D-222: Plate_X and Riser_X are gone; the developer: "this one is totally pointless".)
    // D-223 (the developer, of the run back along F1's ceiling: "a really long stretch where NOTHING happens … Something that hasnt
    // been done until now", then of the ideas: the rising floor, with hidden inverters and "Spikes that appear out of the ground but
    // more as a wave you see it coming towards you. It comes out and retracts. In different intervals, but moving quite fast"): the
    // way back to the door is a run upside down along F1's ceiling, three things at once.
    //  - The floor comes up: west of Gate_2, F1's whole floor rises as one piece (Rising_Floor, D-230) slowly into the ceiling once
    //    the cat passes it: reach the door before it closes.
    //  - The spike wave: spikes pop out of the ceiling (the cat's floor) and retract in a wave running east, toward the cat, fast
    //    (SpikeWaveSpeed); two waves on different beats, so the gaps between them change. Jump it as it comes.
    //  - Inv_R1, hidden on the way, swaps left and right for a moment.
    //  - Plate_2T, just past Plate_2B on F2, opens Trap_2 under the way on: into Spikes_T2 on F3's floor. Jump it too.
    //  - Dead ends: F2's west end past Hatch_2 (Riser_W) and F3's west end (Riser_F3) rise under a cat into the ceiling.
    static class L026Layout
    {
        // The three corridors' floors and ceilings (F1 top, F3 bottom), the slabs between them, the roof.
        internal const float F3 = 0f, F3Ceil = 5f, F2 = 7.5f, F2Ceil = 12f, F1 = 14.5f, F1Ceil = 19f, Base = -1f, Width = 40f;
        // The hatches (each a lid in a slab's 2-wide hole): (name, slab: 1 between F1 and F2, 2 between F2 and F3, x0, the side it
        // slides to: +1 east, -1 west, the plate it answers, its delay after the plate, its hold before it slides back).
        // D-231 (the developer, of a dropped hatch lying over a floor prop: "Two arts are overlapping and it looks off", and of the
        // fixes: "I like 2."): a hatch no longer drops into the corridor below. It's a lid LidThick thick in the middle of its slab
        // (a recess HatchCap deep above and below it, read as a manhole), and its plate slides it sideways into a pocket hollowed
        // in the slab beside it (caps above and below, the floor and ceiling faces unbroken), and back.
        internal static readonly (string name, int slab, float x0, int side, string plate, int delay, int hold)[] Hatches = {
            ("Hatch_1", 1, 33f, +1, "Plate_1", 30, 250), ("Hatch_4", 1, 31f, -1, "Plate_C2", 10, 250),
            ("Hatch_2", 2, 8f, +1, "Plate_2B", 40, 160), ("Hatch_3", 2, 25f, -1, "Plate_C1", 25, 200),
            ("Trap_2", 2, 14.5f, -1, "Plate_2T", 3, 120),
        };
        internal const int HatchMove = 20;
        internal const float HatchW = 2f, HatchCap = 1f, LidThick = .5f;
        // The plates: (name, x, y of the face it's flush on, on a ceiling).
        internal static readonly (string name, float x, float face, bool ceiling)[] Plates = {
            ("Plate_S", 5.6f, F1, false), ("Plate_1", 26.6f, F1, false),
            ("Plate_2A", 29f, F2, false), ("Plate_2B", 18.6f, F2, false), ("Plate_2T", 17.2f, F2, false),
            ("Plate_C1", 19f, F3Ceil, true), ("Plate_C2", 30f, F2Ceil, true), ("Plate_D", 9f, F1Ceil, true),
        };
        // The gates: Gate_D over the door's way (it drops through slab 1 into F2's west end); Gate_2 across F2 (it lifts into slab 1).
        internal const float GateDX = 3.5f, Gate2X = 24f, GateW = .7f;
        internal const int Gate2Hold = 150;
        // D-230 (the developer: "the floor that rises is not clean in terms of the art, theres gaps. And dont just rise one piece of
        // the floor, move the whole floor up for that section"): one piece, F1's floor from x RiseFrom to RiseTo (west of Gate_2,
        // whose lift goes up through the floor), FloorThick of it, rises WaveRise over FloorMove once a cat passes FloorTrigger on
        // the ceiling (WaveDelay later), until 0.4 is left under the roof, and stays: reach the door first.
        internal const float RiseFrom = 7f, RiseTo = 23.5f, FloorThick = 1f, FloorTrigger = 23.2f, WaveRise = F1Ceil - F1 - .4f;
        internal const int WaveDelay = 10, FloorMove = 260;
        // D-223: the spike wave: one strip a unit wide at each x from SpikeFrom to SpikeTo on the ceiling, out SpikeOut ticks; each
        // wave runs east at SpikeWaveSpeed (u a tick), every SpikeBeats[i] ticks from SpikePhases[i].
        internal const float SpikeFrom = 10.5f, SpikeTo = 32f, SpikeWaveSpeed = .15f;   // D-229: was 0.3 (D-228), 0.4 before ("WAAAY too fast")
        internal const int SpikeOut = 8;
        internal static readonly int[] SpikeBeats = { 150, 230 }, SpikePhases = { 0, 70 };   // D-229: was 110 and 170

        public static SoloRoomDefinition Build()
        {
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,(Base - 1f + F1Ceil + 2f) * .5f),(1f,F1Ceil + 2f - Base + 1f)),
                E(SoloRoomElementKind.Wall,"Wall_R",(Width + .5f,(Base - 1f + F1Ceil + 2f) * .5f),(1f,F1Ceil + 2f - Base + 1f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(7f,F1),(0f,0f)),
                E(SoloRoomElementKind.Door,"Door",(2f,F1 + .75f),(.6f,1.5f)),
            };
            // The roof, with Slam_S's slot over Plate_S.
            Block(elements, SoloRoomElementKind.Ceiling, "Roof", 6.2f, Width, F1Ceil, F1Ceil + 1f);
            Block(elements, SoloRoomElementKind.Ceiling, "Roof_W", 0f, 5f, F1Ceil, F1Ceil + 1f);

            // ---- The floors and slabs, cut where a gate, a hatch or a riser sits ----
            // F3's floor (the ground): Riser_F3 (x 1.5-4.5) is its top, on a seat; Spikes_T2 under Trap_2. (D-227: Riser_3 is gone.)
            Cut(elements, "Ground", Base, F3, new[] { (1.5f, 4.5f, F3 - .5f) });
            elements.Add(E(SoloRoomElementKind.Hazard, "Spikes_T2", (15.5f, F3 + .15f), (2f, .3f)));
            // D-224 (the developer: "I need each deadend to lead to a definite death. Otherwise the cat is stuck"): no dead end leaves
            // a cat alive and stranded. F3 past Flip_U is spikes (Spikes_F3E); F1 past Hatch_1 is spikes (Spikes_E1);
            // F2 east of Hatch_1 and its west end, and F3's west end, are solid stone (Fill_*), so Riser_W and Riser_F3 have nothing
            // past them to escape to.
            elements.Add(E(SoloRoomElementKind.Hazard, "Spikes_F3E", (28.25f, F3 + .15f), (23.5f, .3f)));
            elements.Add(E(SoloRoomElementKind.Hazard, "Spikes_E1", (37.6f, F1 + .15f), (4.8f, .3f)));
            Block(elements, SoloRoomElementKind.Wall, "Fill_F2E", 35f, Width, F2, F2Ceil);
            Block(elements, SoloRoomElementKind.Wall, "Fill_F2W", 0f, GateDX, F2, F2Ceil);
            Block(elements, SoloRoomElementKind.Wall, "Fill_F3W", 0f, 1.5f, F3, F3Ceil);
            // D-226 (the developer, upside down on F2's ceiling west of Gate_2: "In a dead end stuck with nothing that kills me"): the
            // ceilings walked upside down are spiked wherever the way back doesn't go: F2's west of Gate_2, F3's west of Flip_U (but
            // Hatch_2's hole) and east of Hatch_3, F1's east of Hatch_4. A cat walking a floor below never reaches them.
            HangingSpikes(elements, "Spikes_C2W", GateDX + GateW, Gate2X - .1f, F2Ceil);
            HangingSpikes(elements, "Spikes_C3W", 1.5f, 8f, F3Ceil);
            HangingSpikes(elements, "Spikes_C3M", 10f, 12.2f, F3Ceil);
            HangingSpikes(elements, "Spikes_C3E", 27.5f, Width, F3Ceil);
            HangingSpikes(elements, "Spikes_C1E", 33.5f, Width, F1Ceil);
            // Slab 2 (F2's floor, F3's ceiling): Riser_W (its top at x 4.5-7.5, on a seat), Hatch_2, Trap_2 and Hatch_3. Pieces, in
            // order: Slab2_0 (x 0-4.5), the seat, Slab2_2, Slab2_3, Slab2_4, Slab2_5.
            // D-231: and each hatch's pocket (Slab2_Cap_n pieces, all east of Slab2_0).
            Cut(elements, "Slab2", F3Ceil, F2, new[] { (GateDX, 7.5f, F2 - .5f), (8f, 10f, F2), (14.5f, 16.5f, F2), (25f, 27f, F2) }, Pockets(2));
            // Slab 1 (F1's floor, F2's ceiling): Gate_D's slot, Gate_2's slot, Hatch_1, Hatch_4. Pieces, in order: Slab1_0, Slab1_1
            // (x 4.2-24, over Riser_W), …
            // D-223: and each floor-wave section (its top, on a seat).
            Cut(elements, "Slab1", F2Ceil, F1, new[] { (GateDX, GateDX + GateW, F1), (RiseFrom, RiseTo, F1 - FloorThick), (Gate2X, Gate2X + GateW, F1), (31f, 33f, F1), (33f, 35f, F1) }, Pockets(1));

            // ---------- The plates ----------
            foreach (var (name, x, face, ceiling) in Plates)
                elements.Add(E(SoloRoomElementKind.Plate, name, (x, ceiling ? face - .05f : face + .05f), (1.2f, .1f), settings: new SoloRoomTrapSettings(repeatMode: TrapRepeatMode.Rearm, cooldownTicks: 20)));

            // ---------- The hatches ----------
            // D-231: each a lid in the middle of its hole; its plate slides it into its pocket, it holds, and slides back.
            foreach (var (name, slab, x0, side, plate, delay, hold) in Hatches)
            {
                float bottom = slab == 1 ? F2Ceil : F3Ceil;
                elements.Add(E(SoloRoomElementKind.MovingTrap, name, (x0 + HatchW * .5f, bottom + HatchCap + LidThick * .5f), (HatchW, LidThick),
                    settings: Chained(plate, delay, new Vector2(side * HatchW, 0f), HatchMove, hold, HatchMove, false)));
            }

            // ---------- The gates ----------
            // Gate_D: across F1 in front of the door, filling its slot in slab 1; Plate_D drops it through the slot into F2's west end.
            elements.Add(E(SoloRoomElementKind.MovingTrap, "Gate_D", (GateDX + GateW * .5f, (F2Ceil + F1Ceil) * .5f), (GateW, F1Ceil - F2Ceil),
                settings: Chained("Plate_D", 10, new Vector2(0f, F2 - F2Ceil), 30, 9999, 30, true)));
            // Gate_2: across F2, filling its slot in slab 1; Plate_2A lifts it Gate2Hold, then it slams shut.
            elements.Add(E(SoloRoomElementKind.MovingTrap, "Gate_2", (Gate2X + GateW * .5f, (F2 + F1) * .5f), (GateW, F1 - F2),
                settings: Chained("Plate_2A", 4, new Vector2(0f, F1 - F2Ceil), 15, Gate2Hold, 12, true)));

            // ---------- The risers ----------
            // D-227 (the developer, the third time: "this weird platform moving to be removed"): Riser_3 is gone; Plate_C1 only opens
            // Hatch_3.
            // D-221: Slam_S, a piece of the roof over Plate_S, drops onto it 6 after it's pressed (0.4 left over the floor), holds, and
            // goes back up.
            elements.Add(E(SoloRoomElementKind.MovingTrap, "Slam_S", (5.6f, F1Ceil + .5f), (1.2f, 1f), settings: Chained("Plate_S", 6, new Vector2(0f, F1 + .4f - F1Ceil), 8, 60, 30, true)));
            // D-221: the dead ends. Riser_W (F2's floor past Hatch_2, x 4.5-7.5) and Riser_F3 (F3's floor at its west end, x 1.5-4.5)
            // rise under a cat into the ceiling 4 after it steps on.
            DeadEnd(elements, "Riser_W", GateDX, 7.5f, F2, F2Ceil, "Slab1_1");
            DeadEnd(elements, "Riser_F3", 1.5f, 4.5f, F3, F3Ceil, "Slab2_0");

            // ---------- The way back (D-223) ----------
            // The rising floor (D-230): one piece of F1's floor, rising into the roof (its crush partner) once a cat passes FloorTrigger.
            elements.Add(E(SoloRoomElementKind.MovingTrap, "Rising_Floor", ((RiseFrom + RiseTo) * .5f, F1 - FloorThick * .5f), (RiseTo - RiseFrom, FloorThick),
                (FloorTrigger, F1Ceil - .5f), (.4f, 1f), new SoloRoomTrapSettings(
                    new MovingFloorSettings(SurfaceMotion.Carry, crushPartner: "Roof"),
                    new SoloRoomTrapSettings(delayTicks: WaveDelay, offset: new Vector2(0f, WaveRise), moveTicks: FloorMove, holdTicks: 9999, movingKind: MovingTrapKind.Solid))));
            // The spike wave: strips under the roof (the cat's floor on the way back), each popping out SpikeOut ticks as each wave
            // passes it. Two waves on two beats; a strip for each.
            int strips = Mathf.RoundToInt(SpikeTo - SpikeFrom);
            for (int w = 0; w < SpikeBeats.Length; w++)
                for (int k = 0; k < strips; k++)
                {
                    float x = SpikeFrom + k + .5f;
                    int phase = (SpikePhases[w] + Mathf.RoundToInt((x - SpikeFrom) / SpikeWaveSpeed)) % SpikeBeats[w];
                    elements.Add(E(SoloRoomElementKind.HiddenSpikes, $"Spike{(char)('A' + w)}_{k + 1}", (x, F1Ceil - .15f), (1f, .3f), settings: new SoloRoomTrapSettings(
                        revealDelayTicks: 0, repeatMode: TrapRepeatMode.Periodic, periodTicks: SpikeBeats[w], phaseTicks: phase, cooldownTicks: SpikeOut)));
                }
            // Inv_R1, hidden in the ceiling's band, swaps left and right a moment (D-230: Inv_R2, nearer the door, is gone: "Its too hard").
            elements.Add(E(SoloRoomElementKind.Inverter, "Inv_R1", (22.3f, F1Ceil - 1f), (.4f, 2f), settings: new SoloRoomTrapSettings(new InverterSettings(45, disguised: true), new SoloRoomTrapSettings(delayTicks: 0))));

            // ---------- The orbs ----------
            elements.Add(E(SoloRoomElementKind.GravityFlip, "Flip_U", (13f, F3 + 1f), (1f, 2f), settings: new SoloRoomTrapSettings(gravityMode: GravityFlipMode.ForceUp, rearmOnExit: true, rendererEnabled: true)));
            elements.Add(E(SoloRoomElementKind.GravityFlip, "Flip_D", (2.9f, F1Ceil - .9f), (1f, 1.2f), settings: new SoloRoomTrapSettings(gravityMode: GravityFlipMode.ForceDown, rearmOnExit: true, rendererEnabled: true)));

            var sections = new[] {
                CheckpointSection.Start("Top", new Vector2(7f, F1), new[] { "Plate_S", "Slam_S", "Plate_1", "Hatch_1" }),
                new CheckpointSection("Middle", new Vector2(31f, F2), new Rect(33f, F2Ceil - .4f, 2f, .2f), new[] { "Plate_2A", "Gate_2", "Plate_2B", "Hatch_2", "Plate_2T", "Trap_2", "Riser_W" }),
                new CheckpointSection("Bottom", new Vector2(11.5f, F3), new Rect(8f, F3Ceil - .4f, 2f, .2f),
                    new[] { "Riser_F3", "Flip_U", "Plate_C1", "Hatch_3", "Plate_C2", "Hatch_4", "Plate_D", "Gate_D", "Flip_D", "Inv_R1", "Rising_Floor" }
                        .Concat(Enumerable.Range(0, SpikeBeats.Length).SelectMany(w => Enumerable.Range(1, Mathf.RoundToInt(SpikeTo - SpikeFrom)).Select(k => $"Spike{(char)('A' + w)}_{k}"))).ToArray()),
            };
            return new SoloRoomDefinition(0, 0f, Width, elements.ToArray(), System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>(), null, null, null, sections);
        }

        // D-221: a piece of a corridor's floor (x0-x1, its top at `floor`) that rises into the ceiling (`partner`, at `ceiling`) 4
        // ticks after a cat steps on it, leaving 0.4; its trigger is its top strip.
        static void DeadEnd(List<SoloRoomElement> e, string name, float x0, float x1, float floor, float ceiling, string partner) =>
            e.Add(E(SoloRoomElementKind.MovingTrap, name, ((x0 + x1) * .5f, floor - .25f), (x1 - x0, .5f), ((x0 + x1) * .5f, floor + .3f), (x1 - x0, .6f), new SoloRoomTrapSettings(
                new MovingFloorSettings(SurfaceMotion.Carry, crushPartner: partner),
                new SoloRoomTrapSettings(delayTicks: 4, offset: new Vector2(0f, ceiling - floor - .4f), moveTicks: 15, holdTicks: 60, returnTicks: 30, repeatMode: TrapRepeatMode.Rearm,
                    cooldownTicks: 15 + 60 + 30, movingKind: MovingTrapKind.Solid))));

        // A mover a plate sets off: `delay` after it, out `offset` over `move`, held `hold`, back over `back`; it can be set off again
        // once home. `slams`: coming down it crushes a cat under it.
        static SoloRoomTrapSettings Chained(string plate, int delay, Vector2 offset, int move, int hold, int back, bool slams) =>
            new(new MovingFloorSettings(SurfaceMotion.Carry, slams: slams),
                new SoloRoomTrapSettings(delayTicks: delay, offset: offset, moveTicks: move, holdTicks: hold, returnTicks: back, triggerSource: TrapTriggerSource.Chain,
                    chainSource: plate, repeatMode: TrapRepeatMode.Rearm, cooldownTicks: move + hold + back, movingKind: MovingTrapKind.Solid));

        // D-231: the pockets the hatches of slab `slab` slide into: (x0, x1), beside each hatch on its side.
        static (float x0, float x1)[] Pockets(int slab) =>
            Hatches.Where(h => h.slab == slab).Select(h => h.side > 0 ? (h.x0 + HatchW, h.x0 + 2f * HatchW) : (h.x0 - HatchW, h.x0)).ToArray();

        // A slab from y0 to y1 across the room, cut at each (x0, x1, seatTop): the slot keeps a seat from y0 up to seatTop (seatTop
        // equal to y0: an open slot, its mover fills it). D-231: each pocket (x0, x1) keeps a cap HatchCap thick at the bottom and
        // the top, hollow between them for a hatch's lid. Pieces are named name_0, name_1, …
        static void Cut(List<SoloRoomElement> e, string name, float y0, float y1, (float x0, float x1, float seatTop)[] slots, (float x0, float x1)[] pockets = null)
        {
            float cursor = 0f; int n = 0;
            var cuts = slots.Select(s => (s.x0, s.x1, s.seatTop, pocket: false))
                .Concat((pockets ?? System.Array.Empty<(float, float)>()).Select(p => (p.Item1, p.Item2, y1, pocket: true)));
            foreach (var (x0, x1, seatTop, pocket) in cuts.OrderBy(s => s.Item1))
            {
                if (x0 > cursor) Block(e, SoloRoomElementKind.Floor, $"{name}_{n++}", cursor, x0, y0, y1);
                if (pocket)
                {
                    Block(e, SoloRoomElementKind.Floor, $"{name}_Cap_{n++}", x0, x1, y0, y0 + HatchCap);
                    Block(e, SoloRoomElementKind.Floor, $"{name}_Cap_{n++}", x0, x1, y1 - HatchCap, y1);
                }
                else if (seatTop > y0 + 1e-3f && seatTop < y1) Block(e, SoloRoomElementKind.Floor, $"{name}_Seat_{n++}", x0, x1, y0, seatTop);
                cursor = x1;
            }
            Block(e, SoloRoomElementKind.Floor, $"{name}_{n}", cursor, Width, y0, y1);
        }

        // D-226: an honest spike strip hanging under a ceiling (its underside at `ceiling`), x0 to x1.
        static void HangingSpikes(List<SoloRoomElement> e, string name, float x0, float x1, float ceiling) =>
            e.Add(E(SoloRoomElementKind.Hazard, name, ((x0 + x1) * .5f, ceiling - .15f), (x1 - x0, .3f)));

        static void Block(List<SoloRoomElement> e, SoloRoomElementKind kind, string name, float x0, float x1, float y0, float y1) =>
            e.Add(E(kind, name, ((x0 + x1) * .5f, (y0 + y1) * .5f), (x1 - x0, y1 - y0)));
    }
}
