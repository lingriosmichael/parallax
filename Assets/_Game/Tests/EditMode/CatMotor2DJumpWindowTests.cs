using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Reality;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Parallax.Tests.EditMode
{
    /// <summary>PAX-079 (D-077) §5.1, §5.2, §5.4: coyote and jump buffer through the real CatMotor2D,
    /// on a real floor. EditMode doesn't simulate physics, so the cat never moves by itself: a
    /// walk-off or a landing is a teleport plus Physics2D.SyncTransforms() (the RoomDeathHoldTests
    /// pattern), and the motor's own ground cast decides IsGrounded. A jump shows as velocity
    /// against gravity after the step. Every other input combination is tested on JumpWindows.
    ///
    /// Cases: "Committed" runs at the real rate (Time.fixedDeltaTime as dt, TickTime's default
    /// source); "Exact002" passes 0.02f as dt and installs 0.02f as TickTime's source.</summary>
    public sealed class CatMotor2DJumpWindowTests
    {
        public enum Rate { Committed, Exact002 }

        static readonly FieldInfo CoyoteField = typeof(CatMotor2D).GetField("coyoteTimer", BindingFlags.NonPublic | BindingFlags.Instance);
        static readonly FieldInfo BufferField = typeof(CatMotor2D).GetField("jumpBufferTimer", BindingFlags.NonPublic | BindingFlags.Instance);

        const float FloorTop = 0f;
        const float CatHalfHeight = .28f;
        const float Gap = .01f; // inside the config's 0.05 ground probe
        static readonly Vector2 OnFloor = new Vector2(0f, FloorTop + CatHalfHeight + Gap);
        static readonly Vector2 OffTheEdge = new Vector2(20f, FloorTop + CatHalfHeight + Gap);

        [TearDown] public void TearDown() => TickTime.ResetToDefault();

        sealed class Rig
        {
            public GameObject Root;
            public GameObject CatGo;
            public CatMotor2D Motor;
            public Rigidbody2D Body;
            public float Dt;

            public bool Step(bool press)
            {
                var cmd = new CatCommand { JumpPressed = press };
                Motor.Step(in cmd, Dt);
                return Vector2.Dot(Body.linearVelocity, Vector2.up) > 0f;
            }

            public void MoveTo(Vector2 position)
            {
                CatGo.transform.position = position;
                Physics2D.SyncTransforms();
            }

            public void Dispose() => Object.DestroyImmediate(Root);
        }

        static Rig Build(Rate rate)
        {
            float dt = Time.fixedDeltaTime;
            if (rate == Rate.Exact002)
            {
                dt = .02f;
                TickTime.SecondsPerTickSource = () => .02f;
            }

            var rig = new Rig { Dt = dt, Root = new GameObject("RealityRoot_A") };
            RealityRoot reality = rig.Root.AddComponent<RealityRoot>();
            CatMotor2DFreezeTests.SetPrivate(reality, "id", ObserverId.A);
            CatMotor2DFreezeTests.Invoke(reality, "Awake");

            int layer = LayerMask.NameToLayer("RealityA");

            var floor = new GameObject("Floor");
            floor.transform.SetParent(rig.Root.transform, false);
            floor.layer = layer;
            floor.transform.position = new Vector2(0f, FloorTop - .5f);
            floor.AddComponent<BoxCollider2D>().size = new Vector2(10f, 1f);

            rig.CatGo = new GameObject("Cat");
            rig.CatGo.transform.SetParent(rig.Root.transform, false);
            rig.CatGo.layer = layer;
            rig.Body = rig.CatGo.AddComponent<Rigidbody2D>();
            rig.CatGo.AddComponent<BoxCollider2D>().size = new Vector2(1f, CatHalfHeight * 2f);
            GravityReceiver gravity = rig.CatGo.AddComponent<GravityReceiver>();
            CatMotor2DFreezeTests.Invoke(gravity, "Awake");
            rig.Motor = rig.CatGo.AddComponent<CatMotor2D>();
            CatMotor2DFreezeTests.SetPrivate(rig.Motor, "config", ScriptableObject.CreateInstance<CatMotorConfig>());
            CatMotor2DFreezeTests.Invoke(rig.Motor, "Awake");

            rig.MoveTo(OnFloor);
            return rig;
        }

        // Rig reliability (R7): the cat must ground on the floor and not ground off it; any doubt
        // fails the test rather than being tuned away.
        static void StandThenWalkOff(Rig rig)
        {
            rig.MoveTo(OnFloor);
            for (int i = 0; i < 2; i++)
            {
                Assert.IsFalse(rig.Step(false), "rig: the cat jumped with no press");
                Assert.IsTrue(rig.Motor.IsGrounded, "rig: the cat did not ground on the floor");
            }
            rig.MoveTo(OffTheEdge);
        }

        static void AirborneStep(Rig rig, bool press, string context)
        {
            bool jumped = rig.Step(press);
            Assert.IsFalse(rig.Motor.IsGrounded, $"rig: the cat grounded off the floor ({context})");
            Assert.IsFalse(jumped, $"unexpected jump ({context})");
        }

        // ---------- §5.1 ----------

        [Test]
        public void Coyote_AllowsAirborneSteps1To5_RefusesStep6([Values] Rate rate)
        {
            var allowed = new List<int>();
            for (int n = 1; n <= 6; n++)
            {
                Rig rig = Build(rate);
                try
                {
                    StandThenWalkOff(rig);
                    for (int i = 1; i < n; i++) AirborneStep(rig, false, $"airborne step {i} before a press on step {n}");
                    bool jumped = rig.Step(true);
                    Assert.IsFalse(rig.Motor.IsGrounded, $"rig: the cat grounded off the floor (airborne step {n})");
                    if (jumped) allowed.Add(n);
                }
                finally { rig.Dispose(); }
            }

            Assert.AreEqual(new[] { 1, 2, 3, 4, 5 }, allowed.ToArray(),
                $"[{rate}] a jump was allowed on airborne steps [{string.Join(", ", allowed)}]; expected steps 1-5 and not 6");
        }

        // ---------- §5.2 ----------

        [Test]
        public void Buffer_PressFiveBeforeLandingJumps_SixBeforeDoesNot([Values] Rate rate)
        {
            var jumpedOnLanding = new Dictionary<int, bool>();
            foreach (int ticksBefore in new[] { 5, 6 })
            {
                Rig rig = Build(rate);
                try
                {
                    rig.MoveTo(OffTheEdge);
                    for (int i = 0; i < 10; i++) AirborneStep(rig, false, "falling before the press");
                    AirborneStep(rig, true, "the press step, coyote long closed");
                    for (int i = 1; i < ticksBefore; i++) AirborneStep(rig, false, $"step {i} after the press");

                    rig.MoveTo(OnFloor);
                    bool jumped = rig.Step(false);
                    Assert.IsTrue(rig.Motor.IsGrounded, "rig: the cat did not ground on landing");
                    jumpedOnLanding[ticksBefore] = jumped;
                }
                finally { rig.Dispose(); }
            }

            Assert.IsTrue(jumpedOnLanding[5], $"[{rate}] a press 5 ticks before landing did not jump on landing");
            Assert.IsFalse(jumpedOnLanding[6], $"[{rate}] a press 6 ticks before landing jumped on landing");
        }

        // ---------- §5.4 ----------

        // Both windows can't be open together through real input: a press while coyote is open
        // fires at once and closes both. So each is opened on its own, frozen for 10 steps, and
        // must hold its exact count and then carry on as if the frozen steps never happened.
        [Test]
        public void Freeze_OpenWindowsDoNotAdvance_AndContinueAfterUnfreeze()
        {
            // Coyote: 2 airborne steps, freeze, then steps 3-4 plain and a press on step 5 jumps.
            Rig rig = Build(Rate.Committed);
            try
            {
                StandThenWalkOff(rig);
                AirborneStep(rig, false, "coyote, airborne step 1");
                AirborneStep(rig, false, "coyote, airborne step 2");
                float coyoteAtFreeze = (float)CoyoteField.GetValue(rig.Motor);
                Assert.Greater(coyoteAtFreeze, 0f, "coyote window should be open at the freeze");

                rig.Motor.Freeze();
                for (int i = 0; i < 10; i++) rig.Step(true);
                rig.Motor.Unfreeze();

                Assert.AreEqual(coyoteAtFreeze, (float)CoyoteField.GetValue(rig.Motor), "coyote advanced while frozen");
                AirborneStep(rig, false, "coyote, airborne step 3");
                AirborneStep(rig, false, "coyote, airborne step 4");
                Assert.IsTrue(rig.Step(true), "coyote did not continue after unfreeze: no jump on airborne step 5");
            }
            finally { rig.Dispose(); }

            // Buffer: press, 2 more airborne steps, freeze, then 2 more and a landing 5 steps after the press jumps.
            rig = Build(Rate.Committed);
            try
            {
                rig.MoveTo(OffTheEdge);
                for (int i = 0; i < 10; i++) AirborneStep(rig, false, "buffer, falling before the press");
                AirborneStep(rig, true, "buffer, the press step");
                AirborneStep(rig, false, "buffer, step 1 after the press");
                AirborneStep(rig, false, "buffer, step 2 after the press");
                float bufferAtFreeze = (float)BufferField.GetValue(rig.Motor);
                Assert.Greater(bufferAtFreeze, 0f, "buffer window should be open at the freeze");

                rig.Motor.Freeze();
                for (int i = 0; i < 10; i++) rig.Step(false);
                rig.Motor.Unfreeze();

                Assert.AreEqual(bufferAtFreeze, (float)BufferField.GetValue(rig.Motor), "buffer advanced while frozen");
                AirborneStep(rig, false, "buffer, step 3 after the press");
                AirborneStep(rig, false, "buffer, step 4 after the press");
                rig.MoveTo(OnFloor);
                bool jumped = rig.Step(false);
                Assert.IsTrue(rig.Motor.IsGrounded, "rig: the cat did not ground on landing");
                Assert.IsTrue(jumped, "buffer did not continue after unfreeze: no jump on landing 5 steps after the press");
            }
            finally { rig.Dispose(); }
        }
    }
}
