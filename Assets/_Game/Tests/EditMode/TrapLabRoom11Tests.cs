using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Checkpoints;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Reality;
using Parallax.Gameplay.Rooms;
using UnityEditor;
using UnityEngine;
using static Parallax.Tests.EditMode.RouteTestApi;
using static Parallax.Tests.EditMode.PrecisionTestApi;

namespace Parallax.Tests.EditMode
{
    // PAX-090 (D-091) §4: Trap Lab room 11, three checkpoint sections in one continuous room, through the route harness.
    // Its routes and section rules are validated once per fixture; the tests read the cached reports. Act1: Collapse_1 over
    // the pit (x 8-10), Spear_1 into the Step (x 18); Act2 (gate x 23.5, checkpoint x 28.2 under the Overhang): the
    // StormCloud (awake from the Step) and the Geyser (vent x 32) under Vent_Roof; Act3 (gate x 41.5, checkpoint x 42.5):
    // Spikes_Back, the Inverter (x 50), Vine_Real (x 53.9) up to the Cliff and the door.
    public sealed class TrapLabRoom11Tests
    {
        IDisposable session;
        object report, sections;

        internal static object Room() => ((IList)Type.GetType("Parallax.Editor.Setup.TrapLabLayout, Parallax.Editor").GetField("Rooms").GetValue(null))[11];
        internal static object Routes() => Call(Type.GetType("Parallax.Editor.Levels.TrapLabRoutes, Parallax.Editor"), "Room11");
        static object Solution() => F(Routes(), "Solution");
        static string Summary(object r) => (string)r.GetType().GetMethod("Summary").Invoke(r, null);
        static IList List(object o, string field) => (IList)F(o, field);

        [OneTimeSetUp]
        public void Open()
        {
            session = OpenSession();
            report = Call(T("RouteValidator"), "Run", session, "TrapLab11", Room(), Routes());
            sections = Call(T("RouteValidator"), "ValidateSections", session, "TrapLab11", Room(), Routes(), null);
            TestContext.Out.WriteLine(Summary(report));
            foreach (string list in new[] { "Timings", "Respawns", "Rewinds" })
                foreach (object o in List(sections, list)) TestContext.Out.WriteLine(o.ToString());
        }

        [OneTimeTearDown] public void Close() => session?.Dispose();

        // ---------- the layout ----------

        [Test]
        public void Layout_PassesEveryLayoutRule()
        {
            object room = Room();
            PlatformSizeConfig sizes = AssetDatabase.LoadAssetAtPath<PlatformSizeConfig>("Assets/_Game/Data/PlatformSizeConfig.asset");
            var errors = new List<string>();
            errors.AddRange(Rule("Validate", "TrapLab11", room));
            errors.AddRange(Rule("ValidateSpear", "TrapLab11", room, sizes));
            errors.AddRange(Rule("ValidateInverter", "TrapLab11", room));
            errors.AddRange(Rule("ValidateGeyser", "TrapLab11", room, Motor()));
            errors.AddRange(Rule("ValidateVine", "TrapLab11", room, Motor()));
            errors.AddRange(Rule("ValidateStormCloud", "TrapLab11", room, Motor()));
            errors.AddRange(Rule("ValidateSections", "TrapLab11", room));
            var bypasses = new List<string>();
            errors.AddRange(Rule("ValidateTriggerCoverage", "TrapLab11", room, Motor(), Gravity(), bypasses));
            errors.AddRange(Rule("ValidateSurfaceCoverage", "TrapLab11", room, Motor()));
            errors.AddRange(Rule("ValidatePlatformSizes", "TrapLab11", room, sizes));
            CollectionAssert.IsEmpty(errors, string.Join("\n", errors));
            CollectionAssert.IsEmpty(bypasses, "no learned bypass");
        }

        [Test]
        public void Layout_HasThreeSections_EveryTrapOwnedOnce()
        {
            IList declared = List(Room(), "CheckpointSections");
            Assert.AreEqual(3, declared.Count);
            CollectionAssert.AreEqual(new[] { "Act1", "Act2", "Act3" }, declared.Cast<object>().Select(s => (string)F(s, "Name")).ToArray());
        }

        // ---------- the routes and the section rules ----------

        [Test]
        public void Routes_PassEveryRouteRule_IncludingTheSectionRules()
        {
            var errors = (List<string>)F(report, "Errors");
            CollectionAssert.IsEmpty(errors, string.Join("\n", errors) + "\n" + Summary(report));
            Assert.IsTrue((bool)F(report, "SolutionCompleted"));
            Assert.IsTrue((bool)F(report, "Deterministic"));
        }

        // R5: the numbers PAX-060's review reads (per-section time, respawn survival, rewind checks), in the test's message.
        [Test]
        public void Measurements_AreReported()
        {
            var lines = new List<string> { Summary(report) };
            foreach (string list in new[] { "Timings", "Respawns", "Rewinds" })
                foreach (object o in List(sections, list)) lines.Add(o.ToString());
            Assert.Pass(string.Join("\n", lines));
        }

        [Test]
        public void EverySection_IsAtMost1000Ticks_OnTheSolution()
        {
            IList timings = List(sections, "Timings");
            Assert.AreEqual(3, timings.Count, string.Join("\n", timings.Cast<object>()));
            foreach (object t in timings) Assert.LessOrEqual((int)F(t, "Ticks"), 1000, t.ToString());
        }

        [Test]
        public void StandingStillAtEachCheckpoint_Survives50Ticks()
        {
            IList respawns = List(sections, "Respawns");
            Assert.AreEqual(2, respawns.Count);
            foreach (object r in respawns) Assert.GreaterOrEqual((int)F(r, "Survived"), 50, r.ToString());
        }

        [Test]
        public void TheRewindToEachGate_IsExact()
        {
            IList rewinds = List(sections, "Rewinds");
            Assert.AreEqual(2, rewinds.Count);
            foreach (object r in rewinds)
            {
                Assert.IsTrue((bool)F(r, "Rewound"), r.ToString());
                Assert.IsTrue((bool)F(r, "StateMatchesGate"), r.ToString());
                Assert.IsTrue((bool)F(r, "Equal"), r.ToString());
            }
        }

        // §9 (3): after a death in Act2, the spear stuck in Act1 is still stuck, the cloud is awake where it was at the gate
        // (not back at its authored pose, asleep), and the room clock is the gate's.
        [Test]
        public void AfterTheAct2Rewind_TheSpearStaysStuck_AndTheCloudIsAwakeWhereItWas()
        {
            Type step = T("RouteStep");
            Array then = Array.CreateInstance(step, 2);
            then.SetValue(Call(T("R"), "Release"), 0);
            then.SetValue(Call(T("R"), "For", 60), 1);
            object route = Call(T("Route"), "FromSection", Solution(), "Act2", 30, "die in Act2", then);
            object replay = ReplayRoute(session, Room(), route);
            List<Rec> r = Records(replay);
            int rewind = (int)Call(T("RouteValidator"), "RewindIndex", replay);
            Assert.Greater(rewind, 0, Dump(replay));
            int marker = ElementIndex(replay, "Act2_Marker"), cloud = ElementIndex(replay, "StormCloud"), spear = ElementIndex(replay, "Spear_1");
            int gate = r.FindIndex(x => x.FireTick != null && x.Tick > 0 && Signature(replay, x.Tick, marker) != Signature(replay, 0, marker));
            Assert.Greater(gate, 0, "the Act2 marker lights");
            Assert.GreaterOrEqual(r[rewind].FireTick[spear], 0, "Spear_1 has fired: it stays stuck");
            Assert.AreEqual(r[gate].FireTick[spear], r[rewind].FireTick[spear]);
            Assert.GreaterOrEqual(r[gate].FireTick[cloud], 0, "precondition: the cloud is awake at the gate");
            int beforeDeath = r.FindIndex(gate, x => x.Dead) - 1;
            Assert.AreNotEqual(Signature(replay, r[gate].Tick, cloud), Signature(replay, r[beforeDeath].Tick, cloud),
                "precondition: the cloud moved between the gate and the death, so the rewind has something to undo");
            Assert.AreEqual(r[gate].FireTick[cloud], r[rewind].FireTick[cloud], "the cloud is still awake, with its wake tick");
            Assert.AreEqual(Signature(replay, r[gate].Tick, cloud), Signature(replay, r[rewind].Tick, cloud), "the cloud is drawn where it was at the gate");
            Assert.AreNotEqual(Signature(replay, 0, cloud), Signature(replay, r[rewind].Tick, cloud), "not back at its authored pose");
            Assert.AreEqual(r[gate].RoomLifeTick, r[rewind].RoomLifeTick, "the room clock is back at the gate");
            Assert.IsNull(F(replay, "Kill"), "standing at the checkpoint after the rewind survives\n" + Dump(replay));
        }

        static int Signature(object replay, int tick, int element)
        {
            foreach (object rec in (IEnumerable)F(replay, "Records"))
                if ((int)F(rec, "Tick") == tick) return ((int[])F(rec, "Signature"))[element];
            throw new ArgumentException("no record at t" + tick);
        }

        // ---------- the builder ----------

        // TrapLabSetup compares each existing room against a scratch build of it wired to the same RoomManager: the room's
        // section entries must keep pointing at the real markers, not the scratch's (destroyed) ones.
        [Test]
        public void TrapLabSync_TwiceOver_KeepsTheSectionEntriesOnTheRealMarkers()
        {
            int layer = LayerMask.NameToLayer(RealitySpace.PhysicsLayerName(ObserverId.A));
            var rootGo = new GameObject("RealityRoot_A") { layer = layer };
            RealityRoot root = rootGo.AddComponent<RealityRoot>();
            typeof(RealityRoot).GetField("id", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(root, ObserverId.A);
            Transform parent = new GameObject("Rooms_PAX045") { layer = layer }.transform;
            parent.SetParent(rootGo.transform, false);
            var systems = new GameObject("Systems");
            RoomManager rooms = systems.AddComponent<RoomManager>();
            object[] wiring = { systems.AddComponent<CheckpointManager>(), rooms, systems.AddComponent<RoomDeath>(), new GameObject("Observers").AddComponent<ObserverSet>(), Motor() };
            Type setup = Type.GetType("Parallax.Editor.Setup.TrapLabSetup, Parallax.Editor");
            Array list = Array.CreateInstance(Room().GetType(), 1);
            list.SetValue(Room(), 0);
            try
            {
                for (int run = 0; run < 2; run++)
                {
                    var changes = new List<string>();
                    Invoke(setup, "SyncRooms", parent, root, list, wiring[0], wiring[1], wiring[2], wiring[3], wiring[4], changes);
                    if (run == 1) CollectionAssert.IsEmpty(changes, "a second run changes nothing");
                    var entries = PauseTestRig.GetPrivate<RoomSectionEntry[]>(rooms, "sections");
                    Assert.AreEqual(3, entries.Length, $"run {run}");
                    Assert.IsTrue(entries[0].Marker == null, "section 0 has no marker");
                    for (int i = 1; i < 3; i++)
                    {
                        Assert.IsTrue(entries[i].Marker != null, $"run {run}: {entries[i].Name}'s marker is alive");
                        Assert.AreSame(parent.Find("Room_12/" + entries[i].Name + "_Marker")?.GetComponent<SpriteRenderer>(), entries[i].Marker, $"run {run}: the real room's marker");
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(rootGo);
                UnityEngine.Object.DestroyImmediate(systems);
                UnityEngine.Object.DestroyImmediate(((ObserverSet)wiring[3]).gameObject);
            }
        }
    }
}
