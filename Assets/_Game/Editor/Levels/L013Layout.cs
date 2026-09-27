using System.Collections.Generic;
using Parallax.Core;
using Parallax.Editor.Setup;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using static Parallax.Editor.Levels.LevelElementFactory;

namespace Parallax.Editor.Levels
{
    // PAX-060 (D-093): level 13, "Old Faithful" (the geyser). The verb is COUNT: two rhythms (100 and 150 ticks) run through
    // the whole level, and the way up is the eruption you waited for, never the one erupting now. A climb from the bottom
    // left to the door at the top right over five storeys (floors 0, 4, 8, 12, 21; pads at 15 and 18), walked right, left,
    // right, left, right.
    //  - Rhythm (section 0, S1 and S2): G_1 launches the cat off S1. Steered right (towards the door) it lands on Ledge_2,
    //    which gives way over Spikes_1; steered left, S2. On S2, G_A erupts as the cat arrives and throws it into hidden
    //    spikes under S3; wait for it to stop, pass it, and wait on G_B's vent for G_B.
    //  - Sync (section 1, S3 and S4): Block_C drops beside G_C 60 ticks after the cat comes near; the one safe place to
    //    wait is the vent itself. G_C lifts the cat to S4. Every 300 ticks G_S1, G_S2 and G_S3 erupt within 55 ticks of each
    //    other and the pads' spikes sink with their vents: ridden on that beat, the three launches carry the cat to S5
    //    (the chaos moment, with G_D). Off the beat, the cat lands on Spikes_P2. Ridden straight up, G_D blows it back down
    //    onto Spikes_P3; the nearer landing, Ledge_5, isn't there.
    //  - Top (section 2, S5): the direct walk crosses G_G, which throws the cat into hidden spikes; the notch drops into a
    //    corridor with two spike rhythms (Spikes_C1 every 100, a safe tile, Spikes_C2 every 150), and G_H lifts the cat
    //    back to S5 by the door.
    //  - Dead ends: the Alcove (an off-beat ride steered left; it gives way and drops the cat back to S4) and S3's west
    //    ledge (G_B's launch steered left, onto hidden spikes).
    static class L013Layout
    {
        // The timeline, in room ticks: each geyser's eruption start (its tell is the 25 ticks before) and each spike strip's
        // down window. The routes wait on the same ticks.
        internal const int S1Down = 170, S1Up = 230, G1Erupt = 310, GAErupt = 455, GBErupt = 640, GCErupt = 840;
        // The sync: G_S1 erupts at SyncTick (and every 100); G_S2 20 ticks later (every 150), so the two line up every 300;
        // G_S3 55 and G_D 62 ticks later (every 300). Each pad's spikes are down from 15 ticks before its vent erupts until
        // 45 after.
        internal const int SyncTick = 1210, S2Lag = 20, S3Lag = 55, GDLag = 62;
        internal const int GGErupt = 1350, C1Down = 1395, C1Up = 1455, C2Down = 1470, C2Up = 1530, GHErupt = 1580;

        public static SoloRoomDefinition Build()
        {
            int e2 = SyncTick + S2Lag, e3 = SyncTick + S3Lag;
            var elements = new List<SoloRoomElement> {
                E(SoloRoomElementKind.Wall,"Wall_L",(-.5f,12.5f),(1f,28f)),
                E(SoloRoomElementKind.Wall,"Wall_R",(32.5f,12.5f),(1f,28f)),
                E(SoloRoomElementKind.Ceiling,"Ceiling",(16f,25.4f),(32f,2.2f)),
                E(SoloRoomElementKind.Checkpoint,"Checkpoint_0",(2.5f,0f),(0f,0f)),
                E(SoloRoomElementKind.Door,"Door",(30.8f,21.75f),(.6f,1.5f)),
                // Storeys: S1 (the ground), S2 (4), S3 (8; S3_W west of G_B's shaft), S4 (12; G_C's shaft at x 19-21.5).
                E(SoloRoomElementKind.Floor,"Ground",(16f,-.5f),(32f,1f)),
                E(SoloRoomElementKind.Floor,"S2",(13.25f,3.75f),(26.5f,.5f)),
                E(SoloRoomElementKind.Floor,"S3_W",(2.5f,7.75f),(5f,.5f)),
                E(SoloRoomElementKind.Floor,"S3",(19.75f,7.75f),(24.5f,.5f)),
                E(SoloRoomElementKind.Floor,"S4_W",(9.5f,11.75f),(19f,.5f)),
                E(SoloRoomElementKind.Floor,"S4_E",(26.75f,11.75f),(10.5f,.5f)),
                // The stack's pads, and the corridor's floor (with the pit under Ledge_5 at its west end) under S5.
                E(SoloRoomElementKind.Floor,"Pad_2",(7.5f,14.75f),(2f,.5f)),
                E(SoloRoomElementKind.Floor,"Pad_3",(11f,17.75f),(2.5f,.5f)),
                E(SoloRoomElementKind.Floor,"Sill_F",(13.55f,17.25f),(2.1f,.5f)),
                E(SoloRoomElementKind.Hazard,"Pit_F",(13.55f,17.65f),(2.1f,.3f)),
                E(SoloRoomElementKind.Floor,"Corr_Floor",(23.3f,17.25f),(17.4f,.5f)),
                E(SoloRoomElementKind.Wall,"Corr_W",(14.85f,19f),(.5f,3f)),
                // S5 (21): the notch x 18.5-20, G_H's shaft x 27.25-28.75.
                E(SoloRoomElementKind.Floor,"S5_A",(16.55f,20.75f),(3.9f,.5f)),
                E(SoloRoomElementKind.Floor,"S5_B",(23.625f,20.75f),(7.25f,.5f)),
                E(SoloRoomElementKind.Floor,"S5_C",(30.375f,20.75f),(3.25f,.5f)),
            };

            // Rhythm. T1: Ledge_2 gives way on a touch over the honest Spikes_1. T2: G_A (every 100) under hidden Spikes_A,
            // revealed by a cut over x 7.6-11, which a cat crosses on its way in from either side (from the west after
            // dropping back down G_B's shaft); G_B (every 150) is the ride.
            // §14 R2: Spikes_S on the walk to G_1 (every 100, down 170-230): the first count, before the first ride.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_S",(20f,.15f),(2f,.3f),settings:Rhythm(100, S1Down, S1Up)));
            elements.Add(E(SoloRoomElementKind.Geyser,"G_1",(28f,-.15f),(1f,.3f),settings:Geyser(150, G1Erupt)));
            elements.Add(E(SoloRoomElementKind.Hazard,"Spikes_1",(30.35f,.15f),(3.3f,.3f)));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Ledge_2",(30.75f,3.75f),(2.5f,.5f),settings:new SoloRoomTrapSettings(delayTicks:4)));
            elements.Add(E(SoloRoomElementKind.Geyser,"G_A",(9.5f,3.85f),(1f,.3f),settings:Geyser(100, GAErupt)));
            elements.Add(E(SoloRoomElementKind.Geyser,"G_B",(6f,3.85f),(1f,.3f),settings:Geyser(150, GBErupt)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_A",(9.5f,7.35f),(1f,.3f),(9.3f,5.75f),(3.4f,3.5f),new SoloRoomTrapSettings(revealDelayTicks:6)));

            // Sync. Spikes_D2 cover S3's west ledge; their trigger is the ledge's whole storey (§13 R3: a cut the coverage search
            // accepts without seeing G_B's launch; every way in crosses it at x 5 as before). Block_C, flush in S4_W beside G_C's shaft, drops 60 ticks after the cat
            // crosses its cut. The Alcove gives way 10 ticks after a touch.
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_D2",(2.5f,8.15f),(5f,.3f),(2.5f,9.75f),(5f,3.5f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            elements.Add(E(SoloRoomElementKind.Geyser,"G_C",(20f,7.85f),(1f,.3f),settings:Geyser(150, GCErupt)));
            elements.Add(E(SoloRoomElementKind.FallingBlock,"Block_C",(17.75f,11.75f),(2.5f,.5f),(15.8f,9.75f),(.4f,3.5f),new SoloRoomTrapSettings(delayTicks:60,unitsPerTick:.3f,travelDistance:3.5f)));
            elements.Add(E(SoloRoomElementKind.Geyser,"G_S1",(4f,11.85f),(1f,.3f),settings:Geyser(100, SyncTick)));
            elements.Add(E(SoloRoomElementKind.CollapsingFloor,"Alcove",(1f,15.25f),(2f,.5f),settings:new SoloRoomTrapSettings(delayTicks:10)));
            elements.Add(E(SoloRoomElementKind.Geyser,"G_S2",(7.5f,14.85f),(1f,.3f),settings:Geyser(150, e2)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_P2",(7.5f,15.15f),(2f,.3f),settings:Rhythm(150, e2 - 15, e2 + 45)));
            elements.Add(E(SoloRoomElementKind.Geyser,"G_S3",(11f,17.85f),(1f,.3f),settings:Geyser(300, e3)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_P3",(11f,18.15f),(2.5f,.3f),settings:Rhythm(300, e3 - 15, e3 + 45)));
            elements.Add(E(SoloRoomElementKind.Geyser,"G_D",(11f,24.45f),(1f,.3f),settings:Geyser(300, SyncTick + GDLag, GeyserDirection.Down)));
            elements.Add(E(SoloRoomElementKind.FakePlatform,"Ledge_5",(13.5f,20.75f),(1.4f,.5f)));

            // Top. G_G under hidden Spikes_G (a cut over its whole span, so it's covered from both sides); the corridor's two
            // spike rhythms; G_H.
            elements.Add(E(SoloRoomElementKind.Geyser,"G_G",(22.5f,20.85f),(1f,.3f),settings:Geyser(100, GGErupt)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_G",(22.5f,24.15f),(1f,.3f),(22f,22.65f),(2.8f,3.3f),new SoloRoomTrapSettings(revealDelayTicks:6)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_C1",(22f,17.65f),(2f,.3f),settings:Rhythm(100, C1Down, C1Up)));
            elements.Add(E(SoloRoomElementKind.HiddenSpikes,"Spikes_C2",(25.6f,17.65f),(2f,.3f),settings:Rhythm(150, C2Down, C2Up)));
            elements.Add(E(SoloRoomElementKind.Geyser,"G_H",(28f,17.35f),(1f,.3f),settings:Geyser(150, GHErupt)));

            var sections = new[] {
                CheckpointSection.Start("Rhythm", new Vector2(2.5f, 0f), new[] { "Spikes_S", "G_1", "Ledge_2", "G_A", "G_B", "Spikes_A" }),
                new CheckpointSection("Sync", new Vector2(9f, 8f), new Rect(5f, 7.55f, 2.5f, .2f),
                    new[] { "Spikes_D2", "G_C", "Block_C", "G_S1", "Alcove", "G_S2", "Spikes_P2", "G_S3", "Spikes_P3", "G_D", "Ledge_5" }),
                new CheckpointSection("Top", new Vector2(18f, 21f), new Rect(17.6f, 21f, .2f, 3.3f), new[] { "G_G", "Spikes_G", "Spikes_C1", "Spikes_C2", "G_H" }),
            };
            return new SoloRoomDefinition(0, 0f, 32f, elements.ToArray(), System.Array.Empty<SoloRoomOpening>(), System.Array.Empty<RequiredJump>(), null, null, null, sections);
        }

        // A geyser (defaults: tell 25, erupt 40) whose eruption starts at room tick `erupt`, and every `period` ticks.
        static SoloRoomTrapSettings Geyser(int period, int erupt, GeyserDirection direction = GeyserDirection.Up) =>
            new(new GeyserSettings(direction), new SoloRoomTrapSettings(repeatMode: TrapRepeatMode.Periodic, periodTicks: period,
                phaseTicks: Mod(erupt - GeyserMath.DefaultTellTicks, period)));

        // Periodic spikes that are down over [down, up) and up for the rest of every period: a periodic trap shows on its
        // fire tick (measured; the 6-tick reveal is the overlap delay) and hides `cooldown` ticks later.
        static SoloRoomTrapSettings Rhythm(int period, int down, int up)
        {
            int cooldown = period - (up - down);
            return new SoloRoomTrapSettings(revealDelayTicks: 6, repeatMode: TrapRepeatMode.Periodic, periodTicks: period, phaseTicks: Mod(up, period), cooldownTicks: cooldown);
        }

        static int Mod(int a, int m) => ((a % m) + m) % m;
    }
}
