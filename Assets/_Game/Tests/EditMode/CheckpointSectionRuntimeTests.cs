using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using UnityEngine.TestTools;

namespace Parallax.Tests.EditMode
{
    /// <summary>PAX-090 (D-091): checkpoint sections in the PauseTestRig solo room (real ObserverSet, RoomManager, RoomDeath,
    /// CheckpointManager, CatRespawn). EditMode runs no physics here, so the cat stays where a test puts it. The room's
    /// checkpoint 0 is at the origin; section "Gate_1" has its gate at x [4.5, 5.5] and its checkpoint at (6, 0).
    /// PauseTestRig's cat has CatRespawn's same-frame guard, so each test respawns the cat once.</summary>
    public sealed class CheckpointSectionRuntimeTests : PauseTestBase
    {
        static readonly Vector2 Spawn1 = new(6f, 0f);

        PauseTestRig rig;
        FireTrap trap;
        SpriteRenderer marker;

        // A trap the test fires: its timing (Overlap, Rearm, delay 3, cooldown 20) sees `Pressed` as its overlap.
        public sealed class FireTrap : RoomTrap
        {
            public bool Pressed;
            public int RestoredAt = -1, Resets;
            protected override int DelayTicks => 3;
            protected override void OnLiveRoomStep() => StepTiming(Pressed);
            protected override void OnReset() => Resets++;
            protected override void OnRestore(in TrapSnapshot snapshot, int roomTick) => RestoredAt = roomTick;
        }

        void Build(int holdTicks, bool sections = true)
        {
            rig = PauseTestRig.Build(realInput: true, holdTicks: holdTicks);
            Transform reality = rig.CatGo.transform.parent;
            var go = new GameObject("FireTrap");
            go.transform.SetParent(reality, false);
            trap = go.AddComponent<FireTrap>();
            PauseTestRig.SetPrivate(trap, "roomId", 0);
            PauseTestRig.SetPrivate(trap, "roomDeath", rig.Death);
            PauseTestRig.SetPrivate(trap, "rooms", rig.Rooms);
            PauseTestRig.SetPrivate(trap, "repeatMode", TrapRepeatMode.Rearm);
            PauseTestRig.SetPrivate(trap, "cooldownTicks", 20);
            PauseTestRig.Invoke(trap, "Awake");
            PauseTestRig.Invoke(trap, "OnEnable");

            if (!sections) return;
            marker = new GameObject("Gate_1_Marker").AddComponent<SpriteRenderer>();
            marker.transform.SetParent(reality, false);
            marker.color = Color.gray;
            PauseTestRig.SetPrivate(rig.Rooms, "sections", new[]
            {
                new RoomSectionEntry(0, "Start", Vector2.zero, Vector2.zero, Vector2.zero, Vector2.down, null),
                new RoomSectionEntry(0, "Gate_1", new Vector2(5f, 0f), new Vector2(1f, 4f), Spawn1, Vector2.down, marker),
            });
        }

        [TearDown]
        public void Teardown() => rig?.Dispose();

        void MoveCat(Vector2 p)
        {
            rig.CatBody.position = p;
            rig.CatGo.transform.position = p;
            Physics2D.SyncTransforms();
        }

        void Ticks(int n) { for (int i = 0; i < n; i++) rig.Tick(); }

        void Die()
        {
            rig.Death.Kill(ObserverId.A, DeathCause.Hazard);
            for (int i = 0; i < 200 && rig.Death.IsHolding; i++) rig.Tick();
        }

        [Test]
        public void TheGate_EntersTheSection_LightsTheMarker_AndIsSeenOnce()
        {
            Build(30);
            rig.Tick();
            Assert.AreEqual(0, rig.Rooms.CurrentSection);
            MoveCat(new Vector2(5f, 0f));
            rig.Tick();
            Assert.AreEqual(1, rig.Rooms.CurrentSection);
            Assert.AreEqual(Color.white, marker.color, "the marker lights up");
            Assert.AreEqual(0, rig.Checkpoints.Current, "a gate is not a room checkpoint: the room stays live");
            Assert.IsTrue(rig.Rooms.IsLive(0));
        }

        [Test]
        public void DeathBeforeTheGate_IsTodaysReset()
        {
            Build(30);
            trap.Pressed = true;
            Ticks(10);
            Assert.GreaterOrEqual(trap.LatestFireTick, 0, "precondition: fired");
            MoveCat(new Vector2(1f, 0f));
            Die();
            Assert.AreEqual(-1, trap.LatestFireTick, "reset to its initial state");
            Assert.AreEqual(1, trap.Resets);
            Assert.AreEqual(-1, trap.RestoredAt, "no rewind");
            Assert.AreEqual(Vector2.zero, rig.CatBody.position, "respawned at the room's checkpoint");
            trap.Pressed = false;
            rig.Tick();
            Assert.AreEqual(0, rig.Rooms.RoomLifeTick, "the room starts again");
            Assert.AreEqual(1, rig.Rooms.SectionDeaths(0));
        }

        [TestCase(30)]
        [TestCase(0)]
        public void DeathAfterTheGate_RewindsToTheGateTick(int holdTicks)
        {
            Build(holdTicks);
            trap.Pressed = true;
            Ticks(4);                                   // room ticks 0-3: pressed at 0, fires at 3
            trap.Pressed = false;
            Ticks(2);                                   // room ticks 4, 5
            MoveCat(new Vector2(5f, 0f));
            rig.Tick();                                 // room tick 6: the gate
            Assert.AreEqual(1, rig.Rooms.CurrentSection);
            int gateTick = rig.Rooms.RoomLifeTick;
            TrapSnapshot atGate = trap.Capture();
            Assert.AreEqual(3, trap.LatestFireTick, "precondition");

            trap.Pressed = true;
            Ticks(40);                                  // rearms at 23, fires again at 26 and later
            Assert.AreNotEqual(atGate, trap.Capture(), "precondition: the trap moved on");
            MoveCat(new Vector2(9f, 0f));
            trap.Pressed = false;
            Die();

            Assert.AreEqual(atGate, trap.Capture(), "the trap is back as it was at the gate");
            Assert.AreEqual(gateTick, trap.RestoredAt);
            Assert.AreEqual(gateTick, rig.Rooms.RoomLifeTick, "the room clock is back at the gate tick");
            Assert.AreEqual(Spawn1, rig.CatBody.position, "respawned at the section's checkpoint");
            Assert.AreEqual(Vector2.zero, rig.CatBody.linearVelocity, "at rest");
            Assert.AreEqual(1, rig.Rooms.CurrentSection, "the section is kept");
            rig.Tick();
            Assert.AreEqual(gateTick + 1, rig.Rooms.RoomLifeTick, "the next live tick continues from the gate");
            Assert.AreEqual(1, rig.Rooms.SectionDeaths(1));
            Assert.AreEqual(0, rig.Rooms.SectionDeaths(0));
            Assert.AreEqual(1, rig.Death.DeathsIn(0), "the room's total is unchanged (D-061)");
        }

        // §11 Q1: a sectioned room must not own anchors. The rewind can't restore one (it resets to its initial value), so
        // RoomDeath logs an error. (The missing TransportHost error that follows is this rig's, as for any anchor reset.)
        [Test]
        public void ARewindInARoomThatOwnsAnAnchor_LogsAnError()
        {
            Build(30);
            var go = new GameObject("Anchor");
            go.transform.SetParent(rig.CatGo.transform.parent, false);
            RoomAnchorBinding binding = go.AddComponent<RoomAnchorBinding>();
            PauseTestRig.SetPrivate(binding, "roomId", 0);
            PauseTestRig.SetPrivate(binding, "anchorId", (ushort)7);
            PauseTestRig.SetPrivate(binding, "roomDeath", rig.Death);
            PauseTestRig.Invoke(binding, "OnEnable");
            rig.Tick();
            MoveCat(new Vector2(5f, 0f));
            rig.Tick();
            Assert.AreEqual(1, rig.Rooms.CurrentSection, "precondition");
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("has checkpoint sections and owns 1 anchor"));
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("missing TransportHost"));
            Die();
        }

        // PAX-090 review: a rearmOnExit flip keeps its state in its TrapCountdown, which the timing (and so the route
        // harness's FireTick) never shows. Fired while the cat stands in it, snapshot, rearmed by the cat leaving, restored:
        // it's fired again, so the cat walking back in isn't flipped a second time (unrestored, it would be).
        [Test]
        public void ARearmOnExitFlip_RestoresItsCountdown()
        {
            Build(30, sections: false);
            var go = new GameObject("Flip");
            go.transform.SetParent(rig.CatGo.transform.parent, false);
            go.transform.localPosition = new Vector2(0f, -.3f);
            go.layer = rig.CatGo.layer;
            go.AddComponent<BoxCollider2D>().size = new Vector2(1f, 1f);
            GravityFlipTrap flip = go.AddComponent<GravityFlipTrap>();
            PauseTestRig.SetPrivate(flip, "roomId", 0);
            PauseTestRig.SetPrivate(flip, "roomDeath", rig.Death);
            PauseTestRig.SetPrivate(flip, "rooms", rig.Rooms);
            PauseTestRig.SetPrivate(flip, "observers", rig.Observers);
            PauseTestRig.SetPrivate(flip, "rearmOnExit", true);
            PauseTestRig.Invoke(flip, "Awake");
            PauseTestRig.Invoke(flip, "OnEnable");
            Physics2D.SyncTransforms();
            var gravity = rig.CatGo.GetComponent<Parallax.Gameplay.Player.GravityReceiver>();

            rig.Tick();
            Assert.AreEqual(Vector2.up, gravity.TargetDirection, "precondition: the flip fired");
            TrapSnapshot fired = flip.Capture();
            MoveCat(new Vector2(20f, 0f));
            rig.Tick();
            Assert.AreEqual(TrapState.Armed, flip.State, "precondition: leaving it rearmed it");

            flip.Restore(fired, rig.Rooms.RoomLifeTick);
            Assert.AreEqual(fired, flip.Capture());
            Assert.AreEqual(TrapState.Fired, flip.State);
            gravity.SetTargetDirection(Vector2.down, snap: true);
            MoveCat(Vector2.zero);
            rig.Tick();
            Assert.AreEqual(Vector2.down, gravity.TargetDirection, "restored as fired: standing in it again doesn't flip");
        }

        [Test]
        public void NoSections_NothingChanges()
        {
            Build(30, sections: false);
            MoveCat(new Vector2(5f, 0f));
            rig.Tick();
            Assert.AreEqual(0, rig.Rooms.CurrentSection);
            Assert.AreEqual(0, rig.Rooms.SectionCount);
            trap.Pressed = true;
            Ticks(10);
            Die();
            Assert.AreEqual(-1, trap.LatestFireTick);
            Assert.AreEqual(-1, trap.RestoredAt);
            rig.Tick();
            Assert.AreEqual(0, rig.Rooms.RoomLifeTick);
        }
    }
}
