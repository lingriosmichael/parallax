using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using static Parallax.Tests.EditMode.RouteTestApi;

namespace Parallax.Tests.EditMode
{
    /// <summary>PAX-090 (D-091) Q1/Q2: every trap kind rewinds exactly. Each shipped level (1-10) and each Trap Lab room 0-10
    /// gets one checkpoint section injected, its gate where the most of its traps have fired on a replay of its solution (rooms 0-2, which have no
    /// routes, just run right). RouteValidator.CheckRewind then replays to the gate twice, dying at once and 5 ticks later;
    /// the two rewinds must give the same record for the next 80 ticks, and every element must look exactly as it did at
    /// the gate. Between them these rooms hold every RoomTrap kind: arrows and spears, collapses and fake platforms, hidden
    /// spikes, falling blocks, moving hazards and solids, door retreats, gravity flips, the inverter, the geyser, vines and
    /// the storm cloud.</summary>
    public sealed class CheckpointSectionRewindTests
    {
        const float HalfCatHeight = .28f;   // D-082's 1 x 0.56 collider

        static readonly string[] Rooms =
        {
            "L001", "L002", "L003", "L004", "L005", "L006", "L007", "L008", "L009", "L010",
            "TrapLab0", "TrapLab1", "TrapLab2", "TrapLab3", "TrapLab4", "TrapLab5", "TrapLab6", "TrapLab7", "TrapLab8", "TrapLab9", "TrapLab10",
            // Room 11's solution passes its geyser after a fire; room 9's Recovers route snaps Vine_Obvious and completes.
            "TrapLab11", "TrapLab9Recover",
            // PAX-100: L006's Floor_6 (a FakePlatform) became a rider; TrapLab4's Recovers route fires Ledge_End before its gate.
            "TrapLab4Recover",
            // PAX-099 (D-106): the angled-arrow room (its Periodic arrow in flight at the gate's rewind).
            "TrapLab13",
        };

        static Type Setup(string name) => Type.GetType("Parallax.Editor.Setup." + name + ", Parallax.Editor");

        static object Room(string id)
        {
            if (id == "TrapLab9Recover") id = "TrapLab9";
            if (id == "TrapLab4Recover") id = "TrapLab4";
            if (id.StartsWith("TrapLab")) return ((IList)Setup("TrapLabLayout").GetField("Rooms").GetValue(null))[int.Parse(id.Substring(7))];
            return ((IDictionary)Type.GetType("Parallax.Editor.Levels.LevelLayouts, Parallax.Editor").GetField("ById").GetValue(null))[id];
        }

        static object Route(string id)
        {
            if (id == "TrapLab9Recover")
                return F(((IList)F(Call(Type.GetType("Parallax.Editor.Levels.TrapLabRoutes, Parallax.Editor"), "Room9"), "Betrayals"))[1], "Route");
            if (id == "TrapLab4Recover")
                return F(((IList)F(Call(Type.GetType("Parallax.Editor.Levels.TrapLabRoutes, Parallax.Editor"), "Room4"), "Betrayals"))[2], "Route");
            if (id.StartsWith("TrapLab"))
            {
                int n = int.Parse(id.Substring(7));
                if (n >= 3) return F(Call(Type.GetType("Parallax.Editor.Levels.TrapLabRoutes, Parallax.Editor"), "Room" + n), "Solution");
                Type step = T("RouteStep");
                Array steps = Array.CreateInstance(step, 2);
                steps.SetValue(Call(T("R"), "Hold", 1), 0);
                steps.SetValue(Call(T("R"), "For", 300), 1);
                return Activator.CreateInstance(T("Route"), "run right", steps);
            }
            return F(((IDictionary)Type.GetType("Parallax.Editor.Levels.LevelRoutes, Parallax.Editor").GetField("ById").GetValue(null))[id], "Solution");
        }

        [TearDown]
        public void RestoreScene() => FreshScratchScene();

        // Every element kind that is a RoomTrap (has runtime state), by SoloRoomElementKind name.
        // GravityFlip is left out on purpose: every flip in these rooms is rearmOnExit, whose state is its TrapCountdown,
        // not the timing, so its FireTick never moves; CheckpointSectionRuntimeTests.ARearmOnExitFlip_… proves its restore.
        static readonly string[] TrapKinds =
        {
            "CollapsingFloor", "HiddenSpikes", "FallingBlock", "DoorRetreat", "MovingTrap", "Arrow", "FakePlatform",
            "Inverter", "Geyser", "Vine", "StormCloud",
        };

        // The gate is a 0.1 box at the chosen record's collider centre: the cat first overlaps it on the first record within
        // half a collider (plus the box's half) of that point, so places first reached late (a cliff top, a ceiling) count.
        sealed class Gate { public Rec At; public Rec Crossed; public HashSet<string> FiredKinds = new(); }
        static bool Overlaps(Rec r, Rec at) => Math.Abs(r.X - at.X) < .55f && Math.Abs(r.Y - at.Y) < .33f;

        // PAX-090 review: the gate goes where the most traps have already fired when the cat first reaches it (the earliest such grounded record, at least
        // 10 ticks before the probe ends, so the delayed death still comes before the room completes); `Crossed` is the first
        // record whose cat overlaps the gate, whose fire ticks are the ones the snapshot holds.
        static Gate PickGate(IDisposable session, object room, object route, out object probe)
        {
            probe = ReplayRoute(session, room, route);
            List<Rec> records = Records(probe);
            var kinds = new Dictionary<string, string>();
            foreach (object e in (IEnumerable)F(room, "Elements")) kinds[(string)F(e, "Name")] = F(e, "Kind").ToString();
            var names = (List<string>)F(probe, "Elements");
            var traps = new List<int>();
            for (int i = 0; i < names.Count; i++) if (kinds.TryGetValue(names[i], out string k) && Array.IndexOf(TrapKinds, k) >= 0) traps.Add(i);
            int last = records[records.Count - 1].Tick;
            int Fired(Rec r) => traps.Count(i => r.FireTick[i] >= 0);

            Rec CrossedAt(Rec at) => records.First(r => r.Tick > 0 && Overlaps(r, at));
            Gate gate = null;
            foreach (Rec r in records)
            {
                if (r.Tick <= 0 || r.Tick > last - 10 || !r.Grounded || r.Dead || r.Complete) continue;
                Rec crossed = CrossedAt(r);
                if (gate == null || Fired(crossed) > Fired(gate.Crossed)) gate = new Gate { At = r, Crossed = crossed };
            }
            if (gate == null) return null;
            foreach (int i in traps) if (gate.Crossed.FireTick[i] >= 0) gate.FiredKinds.Add(kinds[names[i]]);
            return gate;
        }

        [TestCaseSource(nameof(Rooms))]
        public void OneGateAfterTheMostFires_RewindsExactly(string id)
        {
            using IDisposable session = OpenSession();
            object room = Room(id), route = Route(id);
            Gate g = PickGate(session, room, route, out object probe);
            Assert.IsNotNull(g, "the probe replay has somewhere to stand\n" + Dump(probe));
            Rec at = g.At;

            Vector2 start = default;
            foreach (object e in (IEnumerable)F(room, "Elements"))
                if (F(e, "Kind").ToString() == "Checkpoint") start = (Vector2)F(e, "Position");
            Type sectionType = Setup("CheckpointSection");
            Array sections = Array.CreateInstance(sectionType, 2);
            sections.SetValue(Call(sectionType, "Start", "Start", start, new string[0]), 0);
            sections.SetValue(Activator.CreateInstance(sectionType, "Gate_1", new Vector2(at.X, at.GravityUp ? at.Y + HalfCatHeight : at.Y - HalfCatHeight),
                Rect.MinMaxRect(at.X - .05f, at.Y - .05f, at.X + .05f, at.Y + .05f), new string[0], at.GravityUp), 1);
            object sectioned = room.GetType().GetMethod("WithCheckpointSections").Invoke(room, new object[] { sections });

            object check = Call(T("RouteValidator"), "CheckRewind", session, sectioned, route, "Gate_1", 5, 80);
            string why = $"{id}, gate at x {at.X:F2} (probe t{at.Tick}, fired there: {string.Join(", ", g.FiredKinds)}): {check}";
            Assert.IsTrue((bool)F(check, "Rewound"), why);
            Assert.IsTrue((bool)F(check, "StateMatchesGate"), why);
            Assert.IsTrue((bool)F(check, "Equal"), why);
        }

        // PAX-090 review: the cases above only prove a kind's restore if that kind has fired by its gate. Every trap kind
        // these rooms hold must be fired at the gate in at least one case (a kind no solution ever fires is named).
        [Test]
        public void EveryTrapKind_HasFiredAtTheGate_InSomeCase()
        {
            using IDisposable session = OpenSession();
            var present = new HashSet<string>();
            var covered = new Dictionary<string, string>();
            foreach (string id in Rooms)
            {
                object room = Room(id);
                foreach (object e in (IEnumerable)F(room, "Elements")) { string k = F(e, "Kind").ToString(); if (Array.IndexOf(TrapKinds, k) >= 0) present.Add(k); }
                Gate g = PickGate(session, room, Route(id), out _);
                if (g == null) continue;
                foreach (string k in g.FiredKinds) if (!covered.ContainsKey(k)) covered[k] = id;
            }
            TestContext.Out.WriteLine(string.Join("\n", covered.Select(c => $"{c.Key}: {c.Value}")));
            CollectionAssert.IsEmpty(present.Where(k => !covered.ContainsKey(k)).ToList(),
                "trap kinds never fired at a gate; covered: " + string.Join(", ", covered.Select(c => $"{c.Key} ({c.Value})")));
        }
    }
}
