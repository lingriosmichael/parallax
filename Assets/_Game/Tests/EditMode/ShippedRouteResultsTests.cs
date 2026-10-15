using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using static Parallax.Tests.EditMode.RouteTestApi;

namespace Parallax.Tests.EditMode
{
    // PAX-080 §5.5: the route validator's changes (betrayal outcomes, soft-lock reporting, recording after a death)
    // leave every L001-L004 result exactly as D-079 (5) measured it. PAX-082 (D-082): re-pinned once to the new cat
    // (jump apex 1.6), the 20% faster traps and the refitted L001-L004 layouts and routes. PAX-083 (§12 R9): L001 declares
    // one Recovers betrayal (Retreat, revealed by the Door it moves), pinned per level with its ticks. PAX-059 (D-085):
    // re-pinned to the band-1 levels L001-L005 (L001 last), measured by the route harness; half B adds L006-L010 and
    // re-pins L005 (T3's second variant, T6's jump as a timed step). PAX-102: re-pinned L002-L004 and L007-L010 (the band 1
    // difficulty pass: arrows, riders, sinking sections, real holes, the upright doors; L010 no longer has a Recovers).
    // PAX-103: re-pinned L003 (the arrow floor), L004 (the climb), L007 and L010 (the storm, the gaps' spikes), L009 (the
    // roof's spikes).
    // PAX-104: re-pinned L007 (one ground arrow timed to the cat; the storm over all of S2).
    public sealed class ShippedRouteResultsTests
    {
        IDisposable session;
        readonly Dictionary<string, object> reports = new();

        [OneTimeSetUp]
        public void Open()
        {
            session = OpenSession();
            foreach (string id in new[] { "L001", "L002", "L003", "L004", "L005", "L006", "L007", "L008", "L009", "L010" })
                reports[id] = Call(T("RouteValidator"), "Run", session, id, RouteValidatorTests.Room(id), RouteValidatorTests.Routes(id));
        }

        [OneTimeTearDown] public void Close() => session?.Dispose();

        static string Summary(object report) => (string)report.GetType().GetMethod("Summary").Invoke(report, null);

        // PAX-105 (D-110 amendment 3): L001 re-pinned to its falling grip walls (the climb out replaced the timed hop over Block_1).
        [TestCase("L001", new int[0], new int[0], new[] { "Floor_2=26", "Block_1=20", "Block_2=20", "Spikes_1=10", "Floor_7=27", "Spikes_2=7" }, new[] { "Door visible t262, complete t330" })]
        // D-112: L002 re-pinned to its ferry crossing (the drop onto Ferry, the jump up to the door, Stone and Tread_3).
        [TestCase("L002", new[] { 26, 24, 26, 21, 20, 23, 29, 15, 32, 28, 13, 15, 17 }, new int[0], new[] { "Block_1=10", "Block_2=10", "Block_3=10", "Spikes_4=50", "Door=90", "Ferry=773", "Stone=16", "Tread_3=23", "Tread_2=45", "Spikes_6=20" }, new string[0])]
        // D-113: L003 re-pinned: S1 the arrow floor (the jumps past Post_R, Lift_1, Shrink_1 and onto Post_L), S2 the spike bed
        // (D-113 amendment: Hop_1, Ride_1, Lift_2, Hop_2, Ride_2, the fakes False_1 and False_2); no Recovers (S1_Mid is gone).
        // D-113 amendment 2: the perch on Post_R (1.2 u), raised S1_A, Hop_2 moving.
        [TestCase("L003", new[] { 31, 34, 20, 25, 21, 32, 16, 13, 27, 12, 16, 17, 18, 20, 13, 18 }, new int[0], new[] { "Floor_1=26", "Floor_3=21", "Spikes_B=18", "Arrow_L=6", "Lift_1=659", "Shrink_1=47", "False_1=8", "Ride_1=898", "False_2=7", "Ride_2=1043", "Spikes_R=20" }, new string[0])]
        // D-117-D-119, PAX-107: L004 played inverted; Left_Post reaches the floor, so the way is the climb (flip up, down the
        // mossy gap, up through Flip_A); the start's launchers fire from the start; Arrow_7 first fires at 420. PAX-107: the
        // corbel 1 u higher, one wall jump under it (T2's lead from 60).
        [TestCase("L004", new[] { 12, 13, 13, 12, 31 }, new int[0], new[] { "Arrow_Run=6", "Arrow_Jump=6", "Sink_R=44", "Arrow_7=18", "Spikes_A=68", "Spikes_3=7", "Roof_4=21", "Ride_2=823", "Ride_4=833", "Spikes_L=26" }, new string[0])]
        // D-116/D-119: L005 re-pinned: non-stop arrows, the corbel's 45-degree Arrow_X, S1's and the ground's riders, the crush
        // ledge (Ledge_D) at the ground's right end, the storm cloud on every floor.
        // D-126: Arrow_A every 100 ticks; the cat jumps its second shot where it waits for Arrow_X (window 25).
        [TestCase("L005", new[] { 24, 25, 13, 19, 12, 32 }, new int[0], new[] { "Arrow_A=6", "Arrow_Drip=6", "Arrow_X=6", "Arrow_B=6", "Ledge_Lo2=21", "Ledge_Lo2=25", "Arrow_C=6", "Arrow_D=6", "Floor_6=27", "Spikes_D=14", "Ledge_D=12" }, new string[0])]
        // D-119: L006 re-pinned: Ride_7 over Pit_7 and the hinge floor Flip_8 (window 21; T7, T8). PAX-107: the lid 1.5 u longer
        // in all (the step-off dead end now lands on its spikes), the hinge 20 % faster, twice.
        // D-124: the way down is the climb onto Lift_G (no sweep, so no T3); the lid's gap (the step-off dead end into Pit_5).
        // D-126: the drop off S1's west end, through Invert_Drop onto Spikes_G (two dead ends); Floor_4 in 3 ticks.
        // D-146: the hinge floors stand up in 3 ticks and slide faster (leads re-pinned).
        [TestCase("L006", new[] { 20, 26 }, new int[0], new[] { "Spikes_1=11", "Block_2=18", "Floor_4=21", "Ride_7=532", "Flip_8=33", "Floor_5=21", "Ride_6=803", "Spikes_L=56", "Invert_Drop=19", "Invert_Drop=19", "Floor_5=21", "Spikes_L=35" }, new string[0])]
        // PAX-107: Arrow_G gone; the storm 20 % faster, zapping 10 % faster (T7 now dies to it). D-116: L007's storm toned down
        // and awake from the start, following the cat floor to floor
        // (so it shows from the start: T14's lead is from then).
        // D-125: the ground walk's spikes and block (T15, T16); the storm at the cat's run, first strike 130, kept out of the right
        // tower (T7 recovers again); Ride_A and Ride_B 27 ticks later. Then Spikes_1 are shaft 1's spikes (no second row; T1 and
        // T10 a tick longer).
        [TestCase("L007", new[] { 26, 13, 31 }, new int[0], new[] { "Floor_1=30", "Floor_2=21", "S2_End=39", "Spikes_G1=40", "Block_G=13", "S1_T4=30", "Spikes_5=9", "S1_T6=51", "Spikes_6b=32", "Tread_LD=19", "Tread_RF=19", "Ride_A=844", "Sink_S=19", "Ride_B=928", "Cloud=964", "Arrow_S=6" }, new[] { "Tread_RC visible t675, complete t1206" })]
        // PAX-107: S1 runs to the east wall (no ledge down to the door, no pillar); Spikes_E is the dead end on its end. Then the
        // rebuild: S2's hinge floor and the waterfall shaft (window 13), S1's hinge floor, the chimney climb; no arrows, no Ride_B.
        // PAX-107 round four (D-123): the shaft 2 u wide under a mossy chimney, the maze east of it (the climb out recovers: M1-M4),
        // Press_S1 and Piston_G (P1, P2) on the way, S1's hinge floor faster.
        // D-124: Hinge_S2 spiked; T3 dies on it (lead 133 -> 109).
        // D-125: both hinge floors 20 % faster (T3's lead 109 -> 91, T6's 56 -> 47).
        // D-125 (8): Hinge_S2 at the cat's run, standing up 38 ticks after its trigger (T3's lead 91 -> 55); Maze_3 1 u deep.
        // D-146: the hinge floors stand up in 3 ticks and slide faster (leads re-pinned).
        [TestCase("L008", new[] { 26, 13, 26, 26, 26 }, new int[0], new[] { "Block_1=10", "Spikes_2=8", "Hinge_S2=32", "Fall_E0=20", "Spikes_4=187", "Hinge_S1=28", "Press_S1=12", "Piston_G=24", "Block_6=10", "Ride_A=735", "Press_1=11", "Spikes_M2=48", "Piston_3=16", "Spikes_E=300", "Spikes_E=12", "Floor_9=7" }, new[] { "Hinge_S2 visible t233, complete t1417" })]
        // PAX-107 Part C (D-121): L009 rebuilt as the mirror-spike gauntlet: no launchers, three mirror pairs, two lifts over a
        // chasm under spikes, a hinge floor onto F3. PAX-107 round four (D-123): the launchers along S1_A (A1, A2; the jumps
        // over them), the moving-left dead end after them.
        // D-125: inverted from the start to Flip_4 (the route's moves are inverted stick input until then).
        // D-125 (8): the chasm's spikes 1.2 u under S1 (was 3; L3 dies sooner), the slabs' spikes set off by a cat rising
        // toward them, not one walking under them (T1, the flip dead ends: shorter leads).
        // D-146: the hinge floors stand up in 3 ticks and slide faster (leads re-pinned).
        [TestCase("L009", new[] { 16, 14 }, new int[0], new[] { "Spikes_1=16", "Arrow_W1=6", "Arrow_W2=6", "Spikes_1=6", "Spikes_F1=46", "Spikes_F2=46", "Spikes_LA=8", "Spikes_LB=8", "Lift_A=126", "Hinge_9=84", "Spikes_R3=78", "Lift_B=439", "Roof_5=21", "Spikes_R1=84", "Spikes_D1=8", "Ledge_D2=13", "Spikes_D1=8", "Spikes_D1=8" }, new string[0])]
        // PAX-107: L010 starts at S1's east end over a chasm (Ride_A, Sink_S), no stair, the storm faster.
        // D-125: Step_A and Hop_B over the start's hole (no Ride_A); the storm from the start at the cat's run, every 145; Drop_10
        // faster; Ride_E leaves as the cat boards (T8: the storm strikes a cat that waits for the next ride).
        [TestCase("L010", new[] { 15 }, new int[0], new[] { "Step_A=34", "Hop_B=28", "Sink_S=57", "Block_1=10", "S1_2=31", "Drop_10=36", "Spikes_3=15", "Arrow_4=6", "Spikes_5=390", "Cloud=613", "Sink_E=48", "Cloud=758", "Spikes_D2=15", "Spikes_D2=24" }, new string[0])]
        public void RouteResults_AreUnchangedFromD079(string id, int[] windows, int[] margins, string[] leads, string[] recoveries)
        {
            object report = reports[id];
            TestContext.Out.WriteLine(Summary(report));
            CollectionAssert.IsEmpty((IEnumerable)F(report, "Errors"), Summary(report));
            CollectionAssert.AreEquivalent(windows, ((IEnumerable)F(report, "Windows")).Cast<object>().Select(w => (int)F(w, "Count")).ToArray(), Summary(report));
            CollectionAssert.AreEquivalent(margins, ((IEnumerable)F(report, "Margins")).Cast<object>().Select(m => (int)F(m, "Value")).ToArray(), Summary(report));
            CollectionAssert.AreEquivalent(leads, ((IEnumerable)F(report, "Leads")).Cast<object>().Select(l => F(l, "RevealedBy") + "=" + F(l, "Lead")).ToArray(), Summary(report));
            CollectionAssert.AreEqual(recoveries, ((IEnumerable)F(report, "Recoveries")).Cast<object>()
                .Select(r => $"{F(r, "RevealedBy")} visible t{F(r, "FirstVisibleTick")}, complete t{F(r, "CompletionTick")}").ToArray(),
                "Recovers betrayals (D-080; PAX-059: L001 the Door; PAX-107: L008 the climb and the maze)\n" + Summary(report));
        }
    }
}
