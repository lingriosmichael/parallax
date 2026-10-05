using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.GravityControl;
using Parallax.Gameplay.Observers;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    /// <summary>PAX-105 (D-110 amendment (1)): the Grab press on its way to the motor. Keyboard Left Shift and the touch Grab
    /// zone (above jump) latch like jump: read once, merged by the router, dropped by the pause and while seated. The latches
    /// are set on the real input sources by reflection (EditMode can't drive real keys or touches, as PauseInputTests), and
    /// the step runs through the real router, LocalHumanDriver and CatMotor2D in the PauseTestRig with a wall on the cat's
    /// right (collider x [-0.5, 0.5]; a grip wall x [0.52, 1.52], y [-3, 3]).</summary>
    public sealed class GrabInputTests : PauseTestBase
    {
        public enum Source { Keyboard, Touch }

        static void Latch(PauseTestRig rig, Source source)
        {
            if (source == Source.Keyboard) PauseTestRig.SetPrivate(rig.Keyboard, "grabPressedLatched", true);
            else PauseTestRig.SetPrivate(rig.Touch, "grabPressedLatch", true);
        }

        static void AddWall(PauseTestRig rig)
        {
            var go = new GameObject("Wall");
            go.transform.SetParent(rig.CatGo.transform.parent, false);
            go.transform.localPosition = new Vector2(1.02f, 0f);
            go.layer = rig.CatGo.layer;
            go.AddComponent<BoxCollider2D>().size = new Vector2(1f, 6f);
            go.AddComponent<Parallax.Gameplay.Rooms.GripSurface>();
            Physics2D.SyncTransforms();
        }

        static void Step(PauseTestRig rig)
        {
            rig.CatBody.linearVelocity = Vector2.zero;
            rig.Tick();
            Physics2D.SyncTransforms();
        }

        [Test]
        public void APress_ReachesTheMotor_AndIsReadOnce([Values] Source source)
        {
            PauseTestRig rig = PauseTestRig.Build(realInput: true);
            try
            {
                AddWall(rig);
                Latch(rig, source);
                Assert.IsTrue(rig.Router.Read().GrabPressed, "merged by the router");
                Assert.IsFalse(rig.Router.Read().GrabPressed, "an edge: read once");
                Latch(rig, source);
                Step(rig);
                Assert.IsTrue(rig.Cat.IsClinging, "the motor latched");
            }
            finally { rig.Dispose(); }
        }

        [Test]
        public void APress_IsDroppedByThePause([Values] Source source)
        {
            PauseTestRig rig = PauseTestRig.Build(realInput: true);
            try
            {
                AddWall(rig);
                rig.Tick();
                Latch(rig, source);
                rig.Pause.Pause();
                rig.Pause.Resume();
                rig.Pause.ApplyPendingResume();
                Step(rig);
                Assert.IsFalse(rig.Cat.IsClinging, "a press made before the pause was applied after it");
            }
            finally { rig.Dispose(); }
        }

        [Test]
        public void ResetTransientState_ClearsAPress([Values] Source source)
        {
            PauseTestRig rig = PauseTestRig.Build(realInput: true);
            try
            {
                Latch(rig, source);
                rig.Router.ResetTransientState();
                Assert.IsFalse(rig.Router.Read().GrabPressed);
            }
            finally { rig.Dispose(); }
        }

        // The Grab zone sits above the jump zone and overlaps neither it, the interact zone nor the stick zone.
        [Test]
        public void TheGrabZone_IsAboveJump_AndOverlapsNoOtherZone()
        {
            PauseTestRig rig = PauseTestRig.Build(realInput: true);
            try
            {
                Rect grab = PauseTestRig.GetPrivate<Rect>(rig.Touch, "grabZone");
                Rect jump = PauseTestRig.GetPrivate<Rect>(rig.Touch, "jumpZone");
                Assert.IsFalse(grab.Overlaps(jump), "jump");
                Assert.IsFalse(grab.Overlaps(PauseTestRig.GetPrivate<Rect>(rig.Touch, "interactZone")), "interact");
                Assert.IsFalse(grab.Overlaps(PauseTestRig.GetPrivate<Rect>(rig.Touch, "stickZone")), "stick");
                Assert.AreEqual(jump.xMin, grab.xMin, 1e-5f, "in line with jump");
                Assert.AreEqual(jump.yMax, grab.yMin, 1e-5f, "directly above it");
            }
            finally { rig.Dispose(); }
        }

        sealed class UnavailableGravityInput : MonoBehaviour, IGravityControlInput
        {
            public bool IsAvailable => false;
            public void Calibrate() { }
            public float ReadNormalized() => 0f;
        }

        // A seated cat never grabs (LocalHumanDriver zeroes it, as Climb; SeatedClimbTests' frozen-station seating).
        [Test]
        public void ASeatedCat_NeverGrabs()
        {
            PauseTestRig rig = PauseTestRig.Build(realInput: true);
            GameObject stationGo = null;
            try
            {
                CatSeat seat = rig.CatGo.AddComponent<CatSeat>();
                stationGo = new GameObject("Station");
                UnavailableGravityInput gravityInput = stationGo.AddComponent<UnavailableGravityInput>();
                ControlStation station = stationGo.AddComponent<ControlStation>();
                PauseTestRig.SetPrivate(station, "observers", rig.Observers);
                PauseTestRig.SetPrivate(station, "inputSource", (MonoBehaviour)gravityInput);
                PauseTestRig.Invoke(station, "Awake");
                AddWall(rig);
                rig.Observer.SetDriver(new LocalHumanDriver(rig.Router));
                station.Interact(ObserverId.A, null);
                Assume.That(seat.IsSeated, "precondition: seated");
                PauseTestRig.SetPrivate(rig.Keyboard, "grabPressedLatched", true);
                Step(rig);
                Assert.IsFalse(rig.Cat.IsClinging);
            }
            finally
            {
                if (stationGo != null) Object.DestroyImmediate(stationGo);
                rig.Dispose();
            }
        }
    }
}
