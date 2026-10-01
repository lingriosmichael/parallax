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
        [TestCase("L011", 1614, new[] { 37, 27, 27, 30, 44, 16 },
            new[] { "Spear_1=8", "Foot_C=21", "Spear_2=8", "Spear_L=8", "Ledge_M=85", "V1=8", "Spear_K=8", "Spear_Top=8", "Spear_Gap=12", "Lip=26", "Block_Door=7", "Spikes_D1=9", "Spikes_D2=8" }, new string[0])]
        [TestCase("L012", 1323, new[] { 31, 23, 37, 17, 51, 15, 33, 30, 31 },
            new[] { "Cat.Inverted=17", "Floor_1=14", "Floor_2=14", "Collapse_B=22", "Orb_A=55", "Spikes_D=28", "Block_E=7", "Collapse_F=19", "Cat.Inverted=912", "FakeFloor_D=25", "Cat.Inverted=1207", "Slot_Floor=8", "Spikes_D2=10" }, new string[0])]
        [TestCase("L013", 1613, new[] { 31, 28, 28, 28, 28, 36, 32, 34 },
            new[] { "Ledge_2=25", "Spikes_A=13", "Block_C=10", "Spikes_P2=1087", "G_D=1265", "Ledge_5=12", "Spikes_G=13", "Spikes_C1=1331", "Spikes_C2=1410", "Spikes_D2=11" },
            new[] { "Alcove visible t1172, complete t1913" })]
        // PAX-060 (L014): built after PAX-093 and the vine-grab coverage fix (D-096); C1's trigger is its own column. Lip_1
        // gives way at the Top's edge (T1, its escape declared, D-097); Pit_1 and Pit_3 are honest pits; Spear_V2 crosses
        // V2's lane (T2c).
        [TestCase("L014", 1133, new[] { 13, 51, 42, 26, 38, 51, 14, 21, 35, 35, 35, 26 },
            new[] { "Lip_1=53", "V1=33", "Spikes_D2=29", "Spear_V2=22", "Block_T=13", "Spear_4=12", "Spear_L=16", "V5=35", "Shrink=86", "C2=9", "C1=19", "C3=30", "C4=30", "C5=30", "C6=30", "Spikes_8=153" },
            new[] { "V_Up visible t389, complete t1192" })]
        // PAX-060 (L015): the storm cloud. The Run's dance waits out D3's return on bare platforms (locks at t874 and t974);
        // Arrow_9 fires once from a cut on P1; the finale's Spear_10 runs under Floor_F1.
        // 2026-09-29 (the developer's play, D-093 (4c)): every roof under a cloud is real, so L015's T4 (Fake_4) and
        // L018's T1 (Fake_1) are gone; L018's launch jump window is 31 (was 29).
        [TestCase("L015", 1284, new[] { 21, 51, 45, 51, 51, 40, 24, 32, 51, 51 },
            new[] { "Cloud=74", "Cloud=174", "Collapse_2=62", "Arrow_3=12", "Mover_M=146", "Spear_P=86", "Cloud=674", "Block_1=24", "Block_3=24", "Cloud=974", "D3=282", "D3=191", "Arrow_9=50", "Spear_10=36" },
            new[] { "Cloud visible t25, complete t1411", "Mover_M visible t2, complete t1411" })]
        // PAX-060 half B (L016-L020, as built and accepted pending play; D-093 (4b)).
        [TestCase("L016", 1155, new[] { 26, 26, 26, 16, 44, 12 },
            new[] { "Lip_2=42", "Arrow_3=8", "Spikes_X=107", "Tile_6=8", "Spikes_X=8", "Spikes_7=19", "Spikes_8=416", "Block_S3=23", "Spikes_S1=32", "Spikes_H=25", "Spikes_S5=145", "Spikes_C=75" },
            new string[0])]
        [TestCase("L017", 1152, new[] { 45, 51, 29, 12, 15 },
            new[] { "Spear_1=8", "Spikes_D1=10", "Block_2=19", "Collapse_3=58", "Slide_4=41", "Arrow_5a=16", "Block_6=21", "Spikes_5e=15", "Arrow_8=60" },
            new[] { "Slide_4 visible t172, complete t1468" })]
        [TestCase("L018", 1143, new[] { 26, 51, 30, 48, 49, 33 },
            new[] { "Arrow_2=40", "Cloud=174", "Fake_A=9", "Spear_S=24", "SL_2=27", "SL_3=27", "Cloud=474", "Spikes_3=22", "Cloud=574", "Collapse_V=74", "Block_T=14", "G_D=762", "Arrow_B=10", "TL_3=21", "Block_7=24", "Spikes_7=18", "G_C=988" },
            new[] { "Cloud visible t6, complete t1143" })]
        [TestCase("L019", 1142, new[] { 43, 43, 43, 27, 49, 51, 51, 51, 9, 51 },
            new[] { "Spear_4=8", "Sweeper_2=8", "Sweeper_3=8", "Sweeper_4=8", "Sweeper_H=8", "Cat.Inverted=21", "P2=32", "Cat.Inverted=188", "Mover_1=633", "Cat.Inverted=449", "Mover_2=1010", "Mover_2=1109", "Spear_9=12", "Floor_C=19" },
            new[] { "Mover_1 visible t2, complete t1342" })]
        [TestCase("L020", 1143, new[] { 21, 51, 51, 33, 32, 38, 35, 34, 26 },
            new[] { "Arrow_1=8", "Push_3=153", "Collapse_2=29", "Spear_4a=8", "Spear_4b=8", "V_5=55", "Cat.Inverted=25", "Cloud=174", "Shrink_8=207", "Door=94" },
            new[] { "Cloud visible t593, complete t1143", "G_9 visible t61, complete t1293" })]
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
