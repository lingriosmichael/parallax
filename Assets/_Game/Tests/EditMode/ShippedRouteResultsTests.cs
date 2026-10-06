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
        // D-117/D-118/D-119: L004 played inverted, Spikes_1 from x 15.12 (lead 12), the way out from under the slab recovers, and
        // the left wall's non-stop launchers (the hop over Arrow_Run, window 21).
        [TestCase("L004", new[] { 12, 21, 13, 13, 14, 13, 31 }, new int[0], new[] { "Spikes_1=12", "Arrow_O=6", "Sink_R=44", "Arrow_7=18", "Spikes_A=27", "Spikes_3=7", "Roof_4=21", "Ride_2=673", "Ride_4=683", "Arrow_Run=6", "Arrow_Jump=6", "Spikes_L=26" }, new[] { "Cat.Gravity visible t32, complete t1055" })]
        // D-116/D-119: L005 re-pinned: non-stop arrows, the corbel's 45-degree Arrow_X, S1's and the ground's riders, the crush
        // ledge (Ledge_D) at the ground's right end, the storm cloud on every floor.
        [TestCase("L005", new[] { 24, 13, 19, 12, 32 }, new int[0], new[] { "Arrow_A=6", "Arrow_Drip=6", "Arrow_X=6", "Arrow_B=6", "Ledge_Lo2=21", "Ledge_Lo2=25", "Arrow_C=6", "Arrow_D=6", "Floor_6=27", "Spikes_D=14", "Ledge_D=12" }, new string[0])]
        // D-119: L006 re-pinned: Ride_7 over Pit_7 and the hinge floor Flip_8 (the jump over it, window 21; T7, T8).
        [TestCase("L006", new[] { 20, 26, 21 }, new int[0], new[] { "Spikes_1=11", "Block_2=18", "Sweep_3=42", "Floor_4=21", "Floor_5=23", "Ride_6=648", "Ride_7=362", "Flip_8=68", "Spikes_L=44", "Floor_5=21", "Spikes_L=24" }, new string[0])]
        // D-116: L007's storm toned down (slower, every 150 ticks) and awake from the start, following the cat floor to floor
        // (so it shows from the start: T14's lead is from then).
        [TestCase("L007", new[] { 20, 13, 31 }, new int[0], new[] { "Floor_1=29", "Floor_2=21", "S2_End=39", "S1_T4=30", "Spikes_5=9", "S1_T6=51", "Spikes_6b=32", "Tread_LD=19", "Tread_RF=19", "Arrow_G=6", "Ride_A=843", "Sink_S=19", "Ride_B=778", "Cloud=1154", "Arrow_S=6" }, new[] { "Tread_RC visible t659, complete t1356" })]
        [TestCase("L008", new[] { 26, 26 }, new int[0], new[] { "Block_1=10", "Spikes_2=8", "Lift_3=9", "Floor_4=28", "Spikes_5=129", "Block_6=10", "Arrow_S=6", "Arrow_O=6", "Ride_A=666", "Ride_B=773", "Ledge_D=28", "Ledge_D=32", "Floor_9=7" }, new string[0])]
        [TestCase("L009", new[] { 51 }, new int[0], new[] { "Spikes_1=16", "Spikes_1=24", "Arrow_2=6", "Spikes_3=7", "Arrow_4=6", "Roof_5=21", "Spikes_D1=30", "Ledge_D2=15", "Spikes_D1=35", "Spikes_D1=58" }, new string[0])]
        [TestCase("L010", new[] { 26 }, new int[0], new[] { "Block_1=10", "S1_2=25", "Drop_10=57", "Spikes_3=33", "Arrow_4=6", "Spikes_5=393", "Ride_E=537", "Sink_E=33", "Cloud=174", "Step_C=19", "Spikes_D2=15", "Spikes_D2=25" }, new string[0])]
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
                "Recovers betrayals (D-080; PAX-059: L001 the Door; the other levels declare none)\n" + Summary(report));
        }
    }
}
