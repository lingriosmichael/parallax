using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Cameras;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Rooms;
using UnityEditor;
using UnityEngine;
using static Parallax.Tests.EditMode.RouteTestApi;
using static Parallax.Tests.EditMode.PrecisionTestApi;

namespace Parallax.Tests.EditMode
{
    // PAX-087 (D-089) §5, §2.8: Trap Lab room 9, the vine room, through the route harness (the real input router, motor,
    // room step and Physics2D.Simulate). Its routes are validated once per fixture and the tests read the cached report.
    // The other replays put the cat at a vine and read the per-tick records (IsClimbing, positions, velocities).
    // Vine_Real: x 7.9, y [0, 5.1] on Floor_Left. Vine_Obvious: x 9.6, y [0, 5.1] over the Pit, snap Trigger y [2, 2.5],
    // delay 12. The cat's collider is 1 x 0.56; standing on the floor its centre is y 0.28.
    public sealed class TrapLabRoom9Tests
    {
        const float RealX = 7.9f, ObviousX = 9.6f, VineTop = 5.1f, HalfHeight = .28f;
        IDisposable session;
        object report;

        internal static object Room() => ((IList)GeyserValidatorTests.EditorType("TrapLabLayout").GetField("Rooms").GetValue(null))[9];
        internal static object Routes() => Call(Type.GetType("Parallax.Editor.Levels.TrapLabRoutes, Parallax.Editor"), "Room9");
        static string Summary(object r) => (string)r.GetType().GetMethod("Summary").Invoke(r, null);
        static float Dt => TickTime.SecondsPerTick;

        [OneTimeSetUp]
        public void Open()
        {
            session = OpenSession();
            report = Call(T("RouteValidator"), "Run", session, "TrapLab9", Room(), Routes());
        }

        [OneTimeTearDown] public void Close() => session?.Dispose();

        // ---------- route vocabulary, by reflection ----------

        static object S(string method, params object[] args) => Call(T("R"), method, args);
        static object Until(string condition, params object[] args) => S("Until", S(condition, args));
        static object Up => Enum.Parse(T("Vertical"), "Up");
        static object Down => Enum.Parse(T("Vertical"), "Down");

        static object MakeRoute(string name, IEnumerable<object> steps)
        {
            object[] list = steps.ToArray();
            Array array = Array.CreateInstance(T("RouteStep"), list.Length);
            for (int i = 0; i < list.Length; i++) array.SetValue(list[i], i);
            return Activator.CreateInstance(T("Route"), name, array);
        }

        static object StartAt(Vector2 centre)
        {
            object o = Activator.CreateInstance(T("ReplayOptions"));
            Set(o, "StartCentre", (Vector2?)centre);
            return o;
        }

        object ReplayFrom(Vector2 centre, params object[] steps) => ReplayRoute(session, Room(), MakeRoute("probe", steps), StartAt(centre));

        static List<bool> Climbing(object replay) => ((IEnumerable)F(replay, "Records")).Cast<object>().Select(r => (bool)F(r, "IsClimbing")).ToList();

        // ---------- the layout and the routes ----------

        [Test]
        public void Layout_PassesEveryLayoutRule()
        {
            object room = Room();
            PlatformSizeConfig sizes = AssetDatabase.LoadAssetAtPath<PlatformSizeConfig>("Assets/_Game/Data/PlatformSizeConfig.asset");
            var errors = new List<string>();
            errors.AddRange(Rule("Validate", "TrapLab9", room));
            errors.AddRange(Rule("ValidateVine", "TrapLab9", room, Motor()));
            errors.AddRange(Rule("ValidateVineRoutes", "TrapLab9", room, Routes()));
            var bypasses = new List<string>();
            errors.AddRange(Rule("ValidateTriggerCoverage", "TrapLab9", room, Motor(), Gravity(), bypasses));
            errors.AddRange(Rule("ValidateSurfaceCoverage", "TrapLab9", room, Motor()));
            errors.AddRange(Rule("ValidatePlatformSizes", "TrapLab9", room, sizes));
            CollectionAssert.IsEmpty(errors, string.Join("\n", errors));
            CollectionAssert.IsEmpty(bypasses, "no learned bypass");
        }

        [Test]
        public void Solution_ClimbsTheRealVine_LeapsOntoTheCliff_WithEveryTimedWindowAtLeastTwelve_AndIsDeterministic()
        {
            TestContext.Out.WriteLine(Summary(report));
            Assert.IsTrue((bool)F(report, "SolutionCompleted"), Summary(report));
            Assert.IsTrue((bool)F(report, "Deterministic"), Summary(report));
            IList windows = (IList)F(report, "Windows");
            Assert.Greater(windows.Count, 0, "no timed step");
            foreach (object w in windows) Assert.GreaterOrEqual((int)F(w, "Count"), 12, w + "\n" + Summary(report));
        }

        [Test]
        public void TheDiesBetrayal_DiesOnThePitHazard_WithALeadOfAtLeastSix_AndTheRecoversBetrayalRecovers()
        {
            IList leads = (IList)F(report, "Leads");
            Assert.AreEqual(1, leads.Count, Summary(report));
            object l = leads[0];
            Assert.AreEqual("PitHazard", F(l, "Killer"), l.ToString());
            Assert.AreEqual("Vine_Obvious", F(l, "RevealedBy"), l.ToString());
            Assert.IsTrue((bool)F(l, "CauseKnown"), l.ToString());
            Assert.AreEqual(DeathCause.Hazard, F(l, "Cause"), l.ToString());
            Assert.GreaterOrEqual((int)F(l, "Lead"), 6, l.ToString());
            IList recoveries = (IList)F(report, "Recoveries");
            Assert.AreEqual(1, recoveries.Count, Summary(report));
            Assert.IsTrue((bool)F(recoveries[0], "Passed"), recoveries[0].ToString());
            Assert.AreEqual("Vine_Obvious", F(recoveries[0], "RevealedBy"));
            CollectionAssert.IsEmpty((IEnumerable)F(report, "Errors"), Summary(report));
        }

        [Test]
        public void RouteResults_ArePinned()
        {
            TestContext.Out.WriteLine(Summary(report));
            CollectionAssert.AreEqual(Room9Windows, ((IEnumerable)F(report, "Windows")).Cast<object>().Select(w => (int)F(w, "Count")).ToArray(), Summary(report));
            CollectionAssert.AreEquivalent(Room9Leads, ((IEnumerable)F(report, "Leads")).Cast<object>().Select(l => F(l, "RevealedBy") + "=" + F(l, "Lead")).ToArray(), Summary(report));
        }

        // The leap off Vine_Real can start 22 ticks early (1.76 u below the top) and any time later (open high: the cat waits
        // at the top). The lead is the snap (t88) to the kill on the PitHazard (t126).
        static readonly int[] Room9Windows = { 48 };
        static readonly string[] Room9Leads = { "Vine_Obvious=38" };

        [Test]
        public void PassesTheCameraTellRule_AtEveryAspect()
        {
            var errors = new List<string>();
            var camera = AssetDatabase.LoadAssetAtPath<LevelCameraConfig>("Assets/_Game/Data/LevelCameraConfig.asset");
            List<object> results = ((IEnumerable)Invoke(Validator, "CameraTell", session, "TrapLab9", Room(), Routes(), camera, errors)).Cast<object>().ToList();
            TestContext.Out.WriteLine(string.Join("\n", results));
            Assert.Greater(results.Count, 0, "no dying betrayal was measured");
            CollectionAssert.IsEmpty(errors, string.Join("\n", errors));
        }

        // ---------- climbing in the harness ----------

        // Standing at Vine_Real's foot, pushing up: grabbed on tick 1 from the ground, x at the vine's centre, 0.08 u a tick
        // up, stopped with the collider's top at the vine's top (R5 (2) as amended: the cat stays on the vine).
        [Test]
        public void FromTheGround_PushUp_Grabs_Climbs008PerTick_AndStopsWithItsTopAtTheVinesTop()
        {
            object replay = ReplayFrom(new Vector2(RealX + .3f, HalfHeight), S("Hold", Up), S("For", 80));
            List<Rec> r = Records(replay);
            List<bool> climbing = Climbing(replay);
            Assert.IsTrue(climbing[1], "grabbed from the ground on tick 1\n" + Dump(replay));
            Assert.AreEqual(RealX, r[1].X, 1e-3f, "x snapped to the vine's centre");
            Assert.AreEqual(4f, r[2].Vy, 1e-3f, "4 u/s");
            Assert.AreEqual(4f * Dt, r[10].Y - r[9].Y, 1e-4f, "0.08 u a tick\n" + Dump(replay));
            Assert.IsTrue(climbing.Skip(1).All(c => c), "still on the vine at the top\n" + Dump(replay));
            Assert.AreEqual(VineTop - HalfHeight, r.Last().Y, 2e-3f, "the collider's top is the vine's top\n" + Dump(replay));
            Assert.AreEqual(0f, r.Last().Vy, 1e-4f);
            int reached = r.First(x => x.Y >= VineTop - HalfHeight - 1e-3f).Tick;
            TestContext.Out.WriteLine($"PAX-087 ground to top: {reached} ticks");
        }

        [Test]
        public void FromTheGround_PushDown_DoesNothing()
        {
            object replay = ReplayFrom(new Vector2(RealX + .3f, HalfHeight), S("Hold", Down), S("For", 20));
            Assert.IsFalse(Climbing(replay).Any(c => c), "down at a vine's foot does nothing (R5 (1))\n" + Dump(replay));
        }

        [Test]
        public void FromAJump_PushUp_GrabsTheObviousVine_InTheAir()
        {
            // Collider x [8.4, 9.4]: clear of Vine_Real (x [7.6, 8.2]), under Vine_Obvious's edge (x 9.3).
            object replay = ReplayFrom(new Vector2(8.9f, HalfHeight), S("Jump"), Until("Airborne"), S("Hold", Up), Until("Climbing"), S("For", 1));
            List<Rec> r = Records(replay);
            List<bool> climbing = Climbing(replay);
            int grab = climbing.IndexOf(true);
            Assert.Greater(grab, 1, Dump(replay));
            Assert.IsFalse(r[grab].Grounded, "grabbed airborne");
            Assert.AreEqual(ObviousX, r[grab].X, 1e-3f);
        }

        // At the top, a leap right with Move: 6 u/s and the jump's launch on the leap tick; it lands on the Cliff.
        [Test]
        public void AtTheTop_ALeapRight_LandsOnTheCliff()
        {
            object replay = ReplayFrom(new Vector2(RealX, VineTop - HalfHeight - .001f),
                S("Hold", Up), Until("Climbing"), S("Hold", 1), S("Jump"), S("ReleaseClimb"), Until("GroundedOn", "Cliff"));
            List<Rec> r = Records(replay);
            int leap = Climbing(replay).LastIndexOf(true) + 1;
            Assert.AreEqual(6f, r[leap].Vx, 1e-3f, Dump(replay));
            Assert.AreEqual(JumpMath.SpeedForHeight(Motor().JumpHeight, Gravity()), r[leap].Vy, .01f, "the jump's launch, set this tick (no gravity applied in it)");
            Assert.IsTrue(r.Last().Grounded && r.Last().Ground == "Cliff", Dump(replay));
            TestContext.Out.WriteLine($"PAX-087 leap from Vine_Real's top: landed on the Cliff at x {r.Last().X:F3}, {r.Last().Tick - leap} ticks after the leap");
        }

        // A keyboard player still holding W and Right through the leap (no ReleaseClimb) passes over Vine_Obvious without
        // grabbing it: the cat's collider is above that vine's top while it overlaps it in x. It lands on the Cliff.
        [Test]
        public void ALeapWithUpStillHeld_NeverGrabsTheObviousVine_AndLandsOnTheCliff()
        {
            object replay = ReplayFrom(new Vector2(RealX, VineTop - HalfHeight - .001f),
                S("Hold", Up), Until("Climbing"), S("Hold", 1), S("Jump"), Until("GroundedOn", "Cliff"));
            List<bool> climbing = Climbing(replay);
            int leap = climbing.LastIndexOf(true) + 1;
            Assert.Greater(leap, 1, Dump(replay));
            Assert.IsFalse(climbing.Skip(leap).Any(c => c), "regrabbed mid-air\n" + Dump(replay));
            Assert.AreEqual("Cliff", Records(replay).Last().Ground, Dump(replay));
        }

        // A leap straight up keeps the cat inside the vine's box; with up still held it regrabs on the eleventh tick.
        [Test]
        public void AStraightUpLeap_WithUpHeld_RegrabsTheSameVineOnlyAfterTenTicks()
        {
            object replay = ReplayFrom(new Vector2(RealX, 2f), S("Hold", Up), Until("Climbing"), S("Jump"), S("For", 30));
            List<bool> climbing = Climbing(replay);
            int leap = climbing.IndexOf(true);
            while (climbing[leap]) leap++;
            int regrab = climbing.IndexOf(true, leap);
            Assert.AreEqual(Motor().RegrabLockTicks + 1, regrab - leap, "released on the leap tick, locked for the next 10\n" + Dump(replay));
        }

        // The snap: climbing Vine_Obvious, the cat touches the Trigger (y 2) and 12 ticks later the vine vanishes; that room
        // step releases the cat and it falls into the pit.
        [Test]
        public void TheObviousVine_Snaps_ReleasesTheCatOnItsTick_AndTheCatFalls()
        {
            int obvious = -1;
            object replay = ReplayFrom(new Vector2(ObviousX, 1f), S("Hold", Up), Until("Climbing"), Until("Fired", "Vine_Obvious"), S("For", 10));
            obvious = ElementIndex(replay, "Vine_Obvious");
            List<Rec> r = Records(replay);
            List<bool> climbing = Climbing(replay);
            int fire = r.First(x => x.FireTick[obvious] >= 0).Tick;
            Assert.IsFalse(climbing[fire], "released on the snap's tick\n" + Dump(replay));
            Assert.IsTrue(climbing[fire - 1]);
            Assert.Less(r.Last().Vy, 0f, "falling");
            Assert.IsFalse(climbing.Skip(fire).Any(c => c), "a snapped vine is never grabbed again");
        }
    }
}
