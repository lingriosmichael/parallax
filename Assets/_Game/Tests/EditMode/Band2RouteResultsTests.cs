using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using static Parallax.Tests.EditMode.RouteTestApi;

namespace Parallax.Tests.EditMode
{
    // PAX-060 (D-093): the band-2 levels' route results as the route harness measured them when they were built: the
    // solution's length, every timed step's window, every betrayal's lead (by what reveals it) and every Recovers route.
    // ShippedRouteResultsTests keeps L001-L010.
    // Each case measures its own level in its own route session (PAX-060): measuring all five in one setup overflowed the
    // Editor's undo stack (every replay's rig registers its created objects for undo; the harness fix is PAX-092's), which
    // failed every case at once.
    public sealed class Band2RouteResultsTests
    {
        static string Summary(object report) => (string)report.GetType().GetMethod("Summary").Invoke(report, null);

        static (object report, int ticks) Measure(string id)
        {
            using IDisposable session = OpenSession();
            object room = RouteValidatorTests.Room(id), routes = RouteValidatorTests.Routes(id);
            object report = Call(T("RouteValidator"), "Run", session, id, room, routes);
            object solution = ReplayRoute(session, room, F(routes, "Solution"));
            List<Rec> records = Records(solution);
            return (report, (bool)F(solution, "Completed") ? records[records.Count - 1].Tick : -1);
        }

        // §14 R2 (the padding pass) re-pinned all three: L011 gained Spear_L, Ledge_M and Spear_K; L012's inversions are no
        // longer waited out (1602 → 1323 ticks); L013 gained the Spikes_S count on S1. PAX-091: L011's Spikes_D2 trigger holds
        // Ledge_Hi's whole top strip, so D2's lead is 8 (was 7).
        // PAX-A16 (D-100): at the 1.2x camera, Spear_1 fires from under Core_W (the first jump's window is 37, was 32) and
        // Ledge_M sits a storey lower (its fall's lead is 85, was 93); L018's Spear_S fires from a post under SB_W (the jump
        // over it has a window of 30, was 31).
        // PAX-107 round eight (D-127): L011 starts on the Riser, runs the falling slab, climbs the chimney and slides down Core_W
        // (T0 Riser, T0b Slab_1; the Riser and the slab at half speed); Spear_L is gone; Spear_1 fires from a cut by the gap, 0.2 higher (window 34); the developer's
        // play-test: Core_W's west face mossy (window 32), Spear_G an arrow, Spikes_D1 removed.
        // PAX-107 round thirteen: the Cloud chases the cat along the gallery (T3d) and the bridge falls piece by piece behind
        // it (T3e, Bridge_4); the jump over Spear_G has a window of 30 (was 29).
        // PAX-107 round seventeen: T_Rise fills the dip between Post_D and Curb_T (one hop up from Spear_Gap, a drop down to
        // T_West: 1813 ticks); Stop_Hi is gone (the high ledge's lead is 6, the Lip's 27).
        // D-153 amendment 7: the Gallery's underside meets the risen Riser, so a rider is crushed 3 ticks sooner (lead 85).
        [TestCase("L011", 1813, new[] { 34, 32, 30, 30, 44, 16 },
            new[] { "Riser=85", "Slab_1=14", "Spear_1=8", "Foot_C=21", "Spear_2=8", "Ledge_M=133", "Cloud=72", "Bridge_4=55", "V1=8", "Spear_K=8", "Spear_Top=8", "Spear_Gap=12", "Lip=27", "Block_Door=7", "Spikes_D2=6" }, new string[0])]
        // PAX-107 (the developer: "Level 12 to have some more traps"): Drop_3 (T11), Hinge_2 (T12) and Piston_1 (T13); the
        // Handoff checkpoint moves to x 8.3 (gate 7.9), off Hinge_2.
        // The developer's next notes: Block_E slides on into PB after it lands (T3, and T6 is now pushed); hall 4's run gains
        // Hinge_4 (T12, into P4) and Drop_4 (T11), and the jump over P4 (window 20) replaces the switch of hands (51); the
        // Shelf's west half, Shelf_W, rises (T13). Orb_A's flip is 105 ticks, Orb_B's 295.
        // PAX-107 round thirteen: PB is 2.2 u wide and Block_E stops at its edge (a cat pushed while running forward falls in,
        // T3); hall 2's run gains Slam_H2 (T14) and Lift_H2 (T13): the solution waits for each (1538 ticks, was 1456).
        // Round eighteen: Spikes_D a unit east (x 14-15.3), up at Orb_A + 70 (T5 leads 70 and 19).
        // Round nineteen: Orb_A's flip ends over Hinge_4's notch (30 ticks); Orb_P flips the hands again before P4 (T10); Hinge_4
        // stands up and slides faster (1541 ticks).
        // D-146: Hinge_2 stands up in 3 ticks and slides its 8 u in 40 (was 6 and 74): the hand switch over Pit_H has a window of
        // 17 (was 15) and the solution is 1538 ticks.
        // D-154: Spikes_D 0.3 u west (x 13.7-15.0) to leave a landing past it: the run-up's window is 45 (was 44) and Spikes_D's
        // lead 15 (was 13).
        [TestCase("L012", 1538, new[] { 31, 23, 37, 17, 45, 33, 20, 17, 33, 30, 30 },
            new[] { "Cat.Inverted=17", "Floor_1=14", "Floor_2=14", "Collapse_B=20", "Block_E=29", "Block_E=29", "Orb_A=70", "Spikes_D=15", "Block_E=29", "Hinge_4=35", "Orb_P=37", "Drop_4=7", "Collapse_F=19", "Shelf_W=7", "Drop_3=7", "Hinge_2=55", "Cat.Inverted=969", "Slam_H2=6", "Lift_H2=6", "Piston_1=6", "FakeFloor_D=25", "Cat.Inverted=1422", "Slot_Floor=8", "Spikes_D2=9" }, new string[0])]
        // PAX-107 (the developer's L013 notes, D-130/D-131): the corner trap removed; on the ground Collapse_1 (T12, Pit_1),
        // Slam_1 (T11), Lift_1 (T1), Ferry_1 over Pit_M (T13); on S2 Slam_2 and Collapse_2 (T14), Lift_2 (T11), Spike_2 by
        // G_B's vent (T3); on S3 Hinge_3 and Spikes_3 (T10); on S4 Lift_4 (T11); Block_C a section of S4 that drops (D-131). G_A erupts at 530
        // (was 455). The developer's play (2026-10-12): Lift_1 on its own 100-tick beat (window 25), Spike_2 faces the vent.
        // PAX-107 round fifteen: Hinge_1 behind the start and Hinge_S4 on S4 stand up and push a cat that stops (T10); the jump
        // over Spikes_4 has a window of 14.
        // D-140: Slam_1 and Slam_2 kill a cat they land on (a slam's own test), 2 ticks before physics pinned it (leads 6, were 8).
        // D-146: every hinge floor stands up in 3 ticks (Spike_2 in 6) and slides at about 0.2 u a tick; Spike_2 still arrives
        // over G_B's vent just after the launch (delay 59, was 30).
        // D-141: the ground is played with left and right swapped (Invert at the start, until G_1's launch); the swap's first
        // tick walks the cat a tick west, which moves the first windows and a few leads by a tick or two.
        [TestCase("L013", 1613, new[] { 33, 25, 33, 29, 14, 28, 14, 29, 35, 32, 33 },
            new[] { "Collapse_1=12", "Slam_1=6", "Lift_1=9", "Ferry_1=24", "Slam_2=6", "Collapse_2=28", "Lift_2=8", "Spike_2=7", "Spikes_A=12", "Hinge_1=36", "Hinge_S4=9", "Hinge_3=12", "Block_C=11", "Lift_4=7", "Spikes_P2=36", "G_D=1264", "Ledge_5=12", "Spikes_G=13", "Spikes_C1=29", "Spikes_C2=59", "Spikes_D2=11" },
            new[] { "Alcove visible t1171, complete t1913" })]
        // PAX-107 (D-143): L014 rebuilt as "Snapping Canopy": three trenches crossed vine to vine as the vines snap behind the
        // cat, two launchers per lower trench (dodged by climbing), lifts, slams and hinge floors on the walks, a mossy chimney.
        // D-144: the hinge floors are faster (a 3-tick swing; Hinge_0 0.24, Hinge_3 0.25, Hinge_5 0.14 u a tick) and Hinge_5's
        // spikes face the cat it pushes.
        [TestCase("L014", 1369, new[] { 25, 26, 26, 19, 40, 23, 26, 26, 39, 12, 14, 26, 26, 37 },
            new[] { "Hinge_0=38", "V1=23", "V2=21", "V3=21", "V4=21", "Arrow_1Hi=10", "Arrow_1Lo=10", "Lift_1=9", "Collapse_1=11", "V5=23", "V6=21", "V7=21", "Arrow_2Hi=10", "Arrow_2Lo=10", "Lift_2=8", "Hinge_3=9", "V8=23", "V9=21", "V10=21", "V11=21", "Slam_10=33", "Slam_3=8", "Hinge_5=7", "Spikes_P=34", "V_Lure=24" },
            new string[0])]
        // PAX-060 (L015): the storm cloud. The Run's dance waits out D3's return on bare platforms (locks at t874 and t974);
        // Arrow_9 fires once from a cut on P1; the finale's Spear_10 runs under Floor_F1.
        // 2026-09-29 (the developer's play, D-093 (4c)): every roof under a cloud is real, so L015's T4 (Fake_4) and
        // L018's T1 (Fake_1) are gone; L018's launch jump window is 31 (was 29).
        // D-119: the motor's seam tie-break moves L015 and L018 by a tick here and there. PAX-107: the clouds 20 % faster, zapping
        // 10 % faster (L015, L018, L020 re-pinned).
        // PAX-107 (D-125): the clouds at the cat's run; L015's dance on the cloud's 90-tick clock (three hops), Roof_R over the Run
        // checkpoint; L018's first strike 58 ticks after the wake.
        // D-154 (the developer's play, 2026-10-17): the finale's Spear_10 removed; the solution no longer waits it out (1238 ticks).
        [TestCase("L015", 1238, new[] { 21, 51, 34, 51, 51, 40, 22, 25, 39, 51, 51 },
            new[] { "Cloud=74", "Cloud=164", "Collapse_2=62", "Arrow_3=12", "Mover_M=146", "Spear_P=86", "Cloud=524", "Block_1=24", "Block_3=24", "Cloud=884", "D3=181", "D3=113", "Arrow_9=50" },
            new[] { "Cloud visible t25, complete t1408", "Mover_M visible t2, complete t1408" })]
        // PAX-060 half B (L016-L020, as built and accepted pending play; D-093 (4b)).
        // PAX-107 (D-142): L016 replaced by "Flipside" (three corridors, flips between floor and ceiling).
        // D-145: the hinge floors are faster (Hinge_1 4 u in 20 ticks, Hinge_3 3 u in 36, both a 3-tick swing), Hinge_3's spikes
        // face the cat, and the finale's Slam_3a comes down sooner (4 ticks after Hinge_3, over 8) so the hop still beats it.
        // D-154: the left ledge (Hang_W1, Moss_W1) reaches 0.4 u lower; step #29's jump has a window of 15 (was 14).
        [TestCase("L016", 1173, new[] { 26, 14, 15, 26, 25, 14, 30, 31 },
            new[] { "Slam_1=7", "Spikes_CW=55", "CC_1=13", "Lift_1=8", "Lift_1b=8", "Hinge_1=20", "Piston_2=40", "Slam_2=15", "Arrow_M=10", "CC_2=11", "Push_3=18", "Spikes_D2=169", "Cat.Gravity=884", "Hinge_3=27", "Slam_3a=7", "Spikes_3=32", "Spikes_3b=85" },
            new string[0])]
        // D-119: L017's and L018's arrows fire non-stop (windows re-pinned; leads and recoveries unchanged).
        // PAX-107 (D-131): L017's Block_2 hangs 0.25 deep in UF_E1's underside (it was the floor's whole 0.7), so it no longer
        // touches Collapse_3 as it lands: Collapse_3 shows only on the cat's touch (lead 21, was 58). L018's Block_7 is a
        // section of the split Summit (0.37 u a tick).
        // PAX-107 (D-147): L017 rebuilt as three lanes 5 u apart: bridges that crumble behind the cat, slams, jump-height arrows,
        // spiked hinge floors, a spear stair at each lane's end, lane 2 played with the hands swapped (Inv_2), Inv_3 ending in
        // the jump over Pit_3, and Push_3 chasing the cat onto Slam_3.
        [TestCase("L017", 1205, new[] { 51, 51, 13, 27, 27, 27, 33, 31, 51, 13, 27, 27, 27, 33, 21, 16 },
            new[] { "Br_2=35", "Br_2=35", "Slam_1=7", "Arrow_1=10", "Hinge_1=7", "V1=8", "Br2_1=27", "Slam_2=7", "Arrow_2=10", "Hinge_2=7", "W1=8", "Inv_3=57", "Slam_3=7", "Push_3=29" },
            new string[0])]
        // PAX-107 (D-148): L018 rebuilt ("Eye of the Storm": three corridors, the cloud floor to floor, three inverters). V_6 hangs
        // from Roof_A, so a cat resting at its top falls further (lead 37).
        [TestCase("L018", 1139, new[] { 12, 39, 13, 16, 38, 17, 35, 16, 27 },
            new[] { "Hinge_0=35", "Collapse_1=11", "Slam_1=6", "Cloud=164", "Lift_1=8", "Arrow_1=10", "Cloud=344", "Spikes_E=89", "Hinge_2=15", "Slam_2=6", "G_D=470", "Arrow_2=10", "V_6=37", "TL_2=11", "Piston_3=62", "Slam_3=6", "Hinge_3=26", "Cloud=1064", "Spikes_7=53", "V_6=11" },
            new string[0])]
        // PAX-107 (D-149): L019 rebuilt on L016's frame (three corridors), most of it played with the hands swapped: Invert from
        // the start to its end box in the jump off Crumb_1, Orb_2 on its timer (it runs out in the jump onto the bridge), Orb_L on
        // Lift_U to its end box in the last jump; the spear stair builds itself ahead of the climbing cat.
        [TestCase("L019", 1158, new[] { 37, 51, 49, 37, 32, 32, 32, 29, 36, 14, 28, 18, 51 },
            new[] { "Hinge_0=38", "Hinge_0=31", "Lift_1=42", "Slam_1=8", "Crumb_1=14", "Cat.Inverted=294", "Crumb_1=27", "Spear_3=8", "Cat.Inverted=513", "Slam_2=7", "Lift_2=8", "Cat.Inverted=704", "Bridge_3=14", "Arrow_3Hi=10", "Arrow_3Lo=10", "Collapse_3=18", "Arrow_3Lo=10", "Slam_3a=6", "Hinge_3=22", "Spikes_3=35", "Cat.Inverted=1141" },
            new string[0])]
        // PAX-107 (D-150): L020 rebuilt ("The Machine" on the L014/L016 model: four storeys 4.5 u apart, two inverters).
        [TestCase("L020", 1224, new[] { 16, 39, 19, 35, 31, 51, 20, 50, 21 },
            new[] { "Hinge_0=7", "Lift_1=8", "Bridge_1=17", "Arrow_Lo=10", "Spikes_A=7", "Cat.Inverted=28", "Spikes_N=16", "Slam_2=7", "CC_2=8", "Cat.Inverted=214",
                "V_3=34", "Cat.Inverted=373", "Cloud=74", "Arrow_Hi=10", "Hinge_F=24", "Slam_Fa=6", "Spikes_F=26", "Spikes_D=25" },
            new string[0])]
        // D-200-D-203: L021 "Vent Tower": four storeys 5.5 apart climbed on vents, each with a ceiling turret (Eye_A, Eye_B, Eye_C);
        // Swing_A, Swing_B and Spin_B; Stone_C, a phase platform over Spikes_C; V4 under GD, the lie, watched by Eye; the falling
        // bridge over the trench and Ferry under Eye.
        [TestCase("L021", 1721, new[] { 28, 34, 27, 33, 14, 28, 30, 19 },
            new[] { "Eye_A=62", "Swing_A=450", "Eye_B=62", "Swing_B=678", "Spin_B=807", "Stone_C=1213", "GD=1251", "Eye_C=62", "Bridge_1=12", "Eye=62" },
            new string[0])]
        // D-192, D-204: L022 "Clockwork Ridge": Spin_1/Spin_2 over the plain; the Step_1-3 phase stair under Eye_S; the Flip_1-3
        // descent under Swing_1/Swing_2; Spin_V, Plate_V calling Lift_P (a slam into its slot), Swing_C on the cliff; the clock tower's
        // Cog_1/Cog_2 loops round the pillar under Hand.
        [TestCase("L022", 1453, new[] { 48, 30, 40, 26 },
            new[] { "Spin_1=130", "Spin_2=164", "Eye_S=62", "Step_1=233", "Swing_2=110", "Flip_1=12", "Spin_V=648", "Lift_P=55", "Swing_C=1056", "Cog_1=1299", "Hand=1382", "Hand=1534" },
            new string[0])]
        // D-206-D-208: L023 "Ferryman": one very long spike lake crossed in one go on fifteen platforms at one speed (plus five dead
        // ends); Climb, Skyway, Hopper and Lambda carry their riders into hanging spikes; Press_1 and Press_2 keep their beat.
        [TestCase("L023", 1439, new[] { 51, 36 },
            new[] { "Climb=83", "Skyway=96", "Press_1=40", "Hopper=76", "Lambda=48", "Press_2=18" },
            new string[0])]
        // D-196: L024 "Ceiling Town": the braid, floor and ceiling by turns through orbs; Inv_1, Spin_1-3, GB_1-3 (half-hall glitch
        // blocks), the hidden Flip_H2 and Inv_F.
        [TestCase("L024", 950, new[] { 45, 51, 51, 44, 51, 42 },
            new[] { "Cat.Inverted=32", "Spin_1=146", "GB_1=119", "GB_2=119", "Spin_2=505", "Cat.Gravity=572", "Spin_3=921", "Cat.Inverted=905" },
            new string[0])]
        // D-195, D-209: L025 "Rhythm": the V; Andante (Piston_1, Spin_2, Flip_3, Tile_4), Allegro (Spin_V1/V2), Presto (the stair's spinners).
        [TestCase("L025", 863, new[] { 51, 51, 51, 51, 48, 23 },
            new[] { "Piston_1=7", "Spin_2=164", "Flip_3=12", "Tile_4=239", "Spin_V1=458", "Spin_R1=584" },
            new string[0])]
        // D-180: L026 "Wrong Way": the door sits behind the start; east is bait (Fake_U, Vine_E); Drop_W drops the cat into the
        // underpass (Hinge_L chasing, Slam_L), Flip_U puts it on the ceiling (Piston_P, CC_C) and it falls up and over to the door.
        [TestCase("L026", 585, new[] { 51, 51, 40 },
            new[] { "Fake_U=14", "Vine_E=33", "Hinge_L=29", "Slam_L=7", "Piston_P=8", "CC_C=11" },
            new string[0])]
        // D-194: L027 "Updraft": the canyon; V1/V2 by Spin_1, G_3 to V4, GD_5, V5, Ledge_G, Rest_6, V7-V10 with Cloud chasing.
        [TestCase("L027", 701, new[] { 19, 26, 43, 47, 25, 26, 26, 26 },
            new[] { "V1=33", "V5=36", "Ledge_G=89", "V2=32", "V4=33", "Cloud=244", "V7=33", "V8=36", "V9=36", "V10=9" },
            new string[0])]
        // D-191, D-209: L028 "Glitch": the spiral; Press_A, Spin_A, Loop_A, Spin_B, Crush_1-3 sliding out of the ceiling (holes in the
        // inner corridor's floor while they're down), Lift_L, Spin_E.
        [TestCase("L028", 2135, new[] { 51, 51, 50, 51, 51, 51, 51, 51, 51, 51 },
            new[] { "Press_A=11", "Spin_A=317", "Crush_1=13", "Spin_E=2181" },
            new string[0])]
        // D-190: L029 "Freefall": seven floors down; Drop_0, Spin_1, Pad_2's glitch, Ferry_3's loop, Flip_A-C, the volley, the door down
        // the shaft.
        [TestCase("L029", 1269, new[] { 14, 26, 15, 39, 41 },
            new[] { "Spin_1=226", "Pad_2=366", "Ferry_3=129", "Flip_A=10", "Arrow_5a=6", "Lift_D=57" },
            new string[0])]
        // D-183 + D-189: L030 "The Elevator": the slow ride under spiked slams, Arrow_B/Arrow_F and Spin_1/Spin_2; the climb past Jut_W
        // and Jut_E, Spikes_V, Fake_V, Lift_V2 and Spikes_D, Lift_P, Spin_D and Arrow_D at the door.
        [TestCase("L030", 1229, new[] { 28, 41, 13, 28 },
            new[] { "Spikes_C1=22", "Arrow_B=10", "Spin_1=357", "Spikes_C2b=22", "Spikes_C2a=22", "Arrow_F=10", "Spin_2=701", "Lift_V1=79", "Lift_V1=139", "Spikes_V=29", "Fake_V=12", "Spikes_D=19", "Spin_D=1230" },
            new string[0])]
        // A level's whole route measurement runs inside its case, over NUnit's default 180 s for the larger levels (L014).
        [Timeout(600000)]
        public void RouteResults_AreAsBuilt(string id, int solutionTicks, int[] windows, string[] leads, string[] recoveries)
        {
            (object report, int ticks) = Measure(id);
            TestContext.Out.WriteLine(Summary(report));
            CollectionAssert.IsEmpty((IEnumerable)F(report, "Errors"), Summary(report));
            Assert.AreEqual(solutionTicks, ticks, "solution ticks\n" + Summary(report));
            CollectionAssert.AreEquivalent(windows, ((IEnumerable)F(report, "Windows")).Cast<object>().Select(w => (int)F(w, "Count")).ToArray(), Summary(report));
            CollectionAssert.AreEquivalent(leads, ((IEnumerable)F(report, "Leads")).Cast<object>().Select(l => F(l, "RevealedBy") + "=" + F(l, "Lead")).ToArray(), Summary(report));
            CollectionAssert.AreEqual(recoveries, ((IEnumerable)F(report, "Recoveries")).Cast<object>()
                .Select(r => $"{F(r, "RevealedBy")} visible t{F(r, "FirstVisibleTick")}, complete t{F(r, "CompletionTick")}").ToArray(), Summary(report));
        }
    }
}
