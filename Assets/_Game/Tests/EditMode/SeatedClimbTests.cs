using System.Collections.Generic;
using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.GravityControl;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    /// <summary>PAX-089 C (D-089 limitation): a seated cat never climbs. LocalHumanDriver zeroes the motor's Climb while its
    /// CatSeat is seated. The cat is seated through the frozen ControlStation's public Interact, unchanged, with a
    /// test-only gravity input that reports itself unavailable, so the station publishes nothing. The rig is
    /// ClimbRuntimeTests': the PauseTestRig solo room with the real CatInputRouter, LocalHumanDriver and CatMotor2D, no
    /// floor, the cat airborne inside a vine's grab box.</summary>
    public sealed class SeatedClimbTests : PauseTestBase
    {
        PauseTestRig rig;
        Scripted input;
        CatSeat seat;
        ControlStation station;

        sealed class Scripted : ICatCommandSource
        {
            public float Climb;
            public CatCommand Read() => new CatCommand { Climb = Climb };
            public void ResetTransientState() { }
        }

        sealed class UnavailableGravityInput : MonoBehaviour, IGravityControlInput
        {
            public bool IsAvailable => false;
            public void Calibrate() { }
            public float ReadNormalized() => 0f;
        }

        [SetUp]
        public void Build()
        {
            rig = PauseTestRig.Build(realInput: true);
            input = new Scripted();
            PauseTestRig.GetPrivate<List<ICatCommandSource>>(rig.Router, "validSources").Add(input);
            seat = rig.CatGo.AddComponent<CatSeat>();

            var go = new GameObject("Station");
            UnavailableGravityInput gravityInput = go.AddComponent<UnavailableGravityInput>();
            station = go.AddComponent<ControlStation>();
            PauseTestRig.SetPrivate(station, "observers", rig.Observers);
            PauseTestRig.SetPrivate(station, "inputSource", (MonoBehaviour)gravityInput);
            PauseTestRig.Invoke(station, "Awake");

            AddVine();
            // Activated after the seat and the vine exist, so the driver finds both (as a scene load would).
            rig.Observer.SetDriver(new LocalHumanDriver(rig.Router));
        }

        [TearDown]
        public void Teardown()
        {
            if (station != null) Object.DestroyImmediate(station.gameObject);
            rig?.Dispose();
        }

        // A vine x [-0.1, 0.5], y [-1, 3] around the cat's collider (x [-0.5, 0.5], y [-0.58, -0.02]).
        void AddVine()
        {
            var go = new GameObject("Vine");
            go.transform.SetParent(rig.CatGo.transform.parent, false);
            go.transform.localPosition = new Vector2(.2f, 1f);
            go.layer = rig.CatGo.layer;
            BoxCollider2D box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(.6f, 4f);
            ClimbVine vine = go.AddComponent<ClimbVine>();
            PauseTestRig.SetPrivate(vine, "roomId", 0);
            PauseTestRig.SetPrivate(vine, "roomDeath", rig.Death);
            PauseTestRig.SetPrivate(vine, "rooms", rig.Rooms);
            PauseTestRig.SetPrivate(vine, "observers", rig.Observers);
            PauseTestRig.Invoke(vine, "Awake");
            PauseTestRig.Invoke(vine, "OnEnable");
            Physics2D.SyncTransforms();
        }

        void Step(int ticks)
        {
            for (int i = 0; i < ticks; i++)
            {
                rig.CatBody.linearVelocity = Vector2.zero;
                rig.Tick();
                Physics2D.SyncTransforms();
            }
        }

        [Test]
        public void ASeatedCat_PushingUpInsideAVine_NeverGrabsIt()
        {
            station.Interact(ObserverId.A, null);
            Assert.IsTrue(seat.IsSeated, "precondition: seated through the Control Station");
            input.Climb = 1f;
            Step(5);
            Assert.IsFalse(rig.Cat.IsClimbing, "a seated cat never climbs (D-089)");
        }

        // The control: the same push, the same vine, standing up first: it grabs, so the seat alone stops the climb.
        [Test]
        public void TheSameCatStoodUp_PushingUpInsideTheVine_GrabsIt()
        {
            station.Interact(ObserverId.A, null);
            station.Release();
            Assert.IsFalse(seat.IsSeated, "precondition: stood up");
            input.Climb = 1f;
            Step(1);
            Assert.IsTrue(rig.Cat.IsClimbing, "a cat that isn't seated grabs");
        }
    }
}
