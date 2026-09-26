using System.Collections.Generic;
using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    /// <summary>PAX-086 (D-088) §5, Q1, Q2: a GeyserTrap in the PauseTestRig solo room (real ObserverSet, RoomManager,
    /// RoomDeath, LevelPause, CatInputRouter, LocalHumanDriver and CatMotor2D). EditMode doesn't simulate physics here, so
    /// the cat stays where it's put and each tick starts from zero velocity: the velocity after a tick is what that tick's
    /// motor step and room step set. The rig has no floor, so the motor's own step always falls (vy = -g dt). ObserverSet
    /// tick k is room tick k - 1 (no deaths). The cat's collider is y [-0.58, -0.02], x [-0.5, 0.5].</summary>
    public sealed class GeyserRuntimeTests : PauseTestBase
    {
        const int Period = 60, Phase = 10, Tell = 6, Erupt = 10;
        const float Speed = 14f;

        PauseTestRig rig;
        Scripted input;
        GeyserTrap trap;

        sealed class Scripted : ICatCommandSource
        {
            public float Move;
            public bool Jump;
            public CatCommand Read() { var c = new CatCommand { Move = Move, JumpPressed = Jump, JumpHeld = Jump }; Jump = false; return c; }
            public void ResetTransientState() => Jump = false;
        }

        [SetUp]
        public void Build()
        {
            rig = PauseTestRig.Build(realInput: true);
            input = new Scripted();
            PauseTestRig.GetPrivate<List<ICatCommandSource>>(rig.Router, "validSources").Add(input);
        }

        [TearDown]
        public void Teardown() => rig?.Dispose();

        // Up: the vent's top face is the cat's collider bottom (y -0.58), so the column is y [-0.58, 0.92], x [-0.5, 0.5].
        // Down: the vent's bottom face is at y 0.25, so the column is y [-1.25, 0.25].
        GeyserTrap AddGeyser(GeyserDirection direction = GeyserDirection.Up, float x = 0f)
        {
            Transform reality = rig.CatGo.transform.parent;
            var go = new GameObject("Geyser");
            go.transform.SetParent(reality, false);
            go.transform.localPosition = direction == GeyserDirection.Up ? new Vector2(x, -.73f) : new Vector2(x, .4f);
            go.layer = rig.CatGo.layer;
            BoxCollider2D box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(1f, .3f);
            SpriteRenderer vent = go.AddComponent<SpriteRenderer>();
            SpriteRenderer column = new GameObject("Column").AddComponent<SpriteRenderer>();
            column.transform.SetParent(go.transform, false);

            GeyserTrap t = go.AddComponent<GeyserTrap>();
            PauseTestRig.SetPrivate(t, "roomId", 0);
            PauseTestRig.SetPrivate(t, "roomDeath", rig.Death);
            PauseTestRig.SetPrivate(t, "rooms", rig.Rooms);
            PauseTestRig.SetPrivate(t, "observers", rig.Observers);
            PauseTestRig.SetPrivate(t, "repeatMode", TrapRepeatMode.Periodic);
            PauseTestRig.SetPrivate(t, "periodTicks", Period);
            PauseTestRig.SetPrivate(t, "phaseTicks", Phase);
            PauseTestRig.SetPrivate(t, "vent", vent);
            PauseTestRig.SetPrivate(t, "column", column);
            PauseTestRig.SetPrivate(t, "direction", direction);
            PauseTestRig.SetPrivate(t, "tellTicks", Tell);
            PauseTestRig.SetPrivate(t, "eruptTicks", Erupt);
            PauseTestRig.SetPrivate(t, "launchSpeed", Speed);
            PauseTestRig.Invoke(t, "Awake");
            PauseTestRig.Invoke(t, "OnEnable");
            Physics2D.SyncTransforms();
            return t;
        }

        void MoveCat(Vector2 local)
        {
            rig.CatGo.transform.position = rig.CatGo.transform.parent.TransformPoint(local);
            Physics2D.SyncTransforms();
        }

        // One tick from rest; returns the velocity the motor and the room step set.
        Vector2 Step()
        {
            rig.CatBody.linearVelocity = Vector2.zero;
            rig.Tick();
            return rig.CatBody.linearVelocity;
        }

        // Every wait is bounded: a phase that never comes fails the test instead of hanging the Editor.
        void StepUntil(GeyserPhase phase)
        {
            for (int i = 0; i <= 2 * Period; i++)
            {
                if (trap.Phase == phase) return;
                Step();
            }
            Assert.Fail($"the geyser never reached {phase} within {2 * Period} ticks");
        }

        static GeyserPhase Expected(int roomTick) => GeyserMath.PhaseAt(roomTick, Period, Phase, Tell, Erupt);

        [Test]
        public void WhileErupting_TheCatInTheColumn_IsSetToLaunchSpeed_AtNoOtherTick_AndTheHorizontalIsTheMotors()
        {
            trap = AddGeyser();
            input.Move = 1f;
            float fall = -G() * TickTime.SecondsPerTick;
            int pushes = 0;
            for (int i = 0; i < 2 * Period; i++)
            {
                Vector2 v = Step();
                int roomTick = rig.Rooms.RoomLifeTick;
                Assert.AreEqual(Expected(roomTick), trap.Phase, $"room tick {roomTick}");
                if (trap.Phase == GeyserPhase.Erupt) { Assert.AreEqual(Speed, v.y, 1e-4f, $"pushed at room tick {roomTick}"); pushes++; }
                else Assert.AreEqual(fall, v.y, 1e-4f, $"the motor's own step at room tick {roomTick} (the tell is harmless)");
                Assert.Greater(v.x, 0f, "steering is the motor's");
                Assert.AreEqual(PauseTestRig.GetPrivate<CatMotorConfig>(rig.Cat, "config").Acceleration * TickTime.SecondsPerTick, v.x, 1e-3f, "the push leaves the horizontal alone");
            }
            Assert.AreEqual(2 * Erupt, pushes);
        }

        [Test]
        public void ACatBesideTheColumn_IsNeverPushed()
        {
            trap = AddGeyser();
            MoveCat(new Vector2(1.2f, 0f));   // collider x [0.7, 1.7]; the column is x [-0.5, 0.5]
            for (int i = 0; i < Period + Phase + Tell + Erupt; i++) Assert.LessOrEqual(Step().y, 0f, $"room tick {rig.Rooms.RoomLifeTick}");
        }

        [Test]
        public void ADownGeyser_PushesAlongDown()
        {
            trap = AddGeyser(GeyserDirection.Down);
            bool pushed = false;
            for (int i = 0; i < Phase + Tell + Erupt + 2; i++)
            {
                Vector2 v = Step();
                if (trap.Phase != GeyserPhase.Erupt) continue;
                Assert.AreEqual(-Speed, v.y, 1e-4f);
                pushed = true;
            }
            Assert.IsTrue(pushed);
        }

        [Test]
        public void TheLook_TellChangesTheVent_EruptShowsTheColumn_IdleShowsNeither()
        {
            trap = AddGeyser();
            SpriteRenderer vent = trap.Vent, column = trap.Column;
            Step();
            Color idle = vent.color;
            Assert.IsFalse(column.enabled, "idle: no column");
            StepUntil(GeyserPhase.Tell);
            Assert.AreNotEqual(idle, vent.color, "the tell is a visible change of the vent");
            Assert.IsFalse(column.enabled, "tell: no column");
            StepUntil(GeyserPhase.Erupt);
            Assert.IsTrue(column.enabled, "erupt: the column shows");
            StepUntil(GeyserPhase.Idle);
            Assert.IsFalse(column.enabled);
            Assert.AreEqual(idle, vent.color);
        }

        [Test]
        public void TheDeathHold_FreezesTheCycle_AndTheResetRestartsItFromRoomStart()
        {
            trap = AddGeyser();
            StepUntil(GeyserPhase.Erupt);
            Step(); Step();
            int roomTick = rig.Rooms.RoomLifeTick;
            rig.Death.Kill(ObserverId.A, DeathCause.Hazard);
            Assert.IsTrue(rig.Death.IsHolding);
            for (int i = 0; ; i++)
            {
                Assert.Less(i, 100, "the hold never ended");
                Vector2 v = Step();
                if (!rig.Death.IsHolding) break;
                Assert.AreEqual(roomTick, rig.Rooms.RoomLifeTick, "the room tick is frozen");
                Assert.AreEqual(GeyserPhase.Erupt, trap.Phase, "the room shows exactly what killed you");
                Assert.AreEqual(Vector2.zero, v, "the frozen cat is not pushed");
            }
            Assert.AreEqual(GeyserPhase.Idle, trap.Phase, "the reset returns the geyser to idle");
            Assert.AreEqual(-1, trap.LatestFireTick);
            // The next live tick is room tick 0: the cycle starts over from the phase.
            for (int i = 0; i < Phase + Tell + 1; i++)
            {
                Step();
                Assert.AreEqual(Expected(rig.Rooms.RoomLifeTick), trap.Phase, $"room tick {rig.Rooms.RoomLifeTick}");
            }
            Assert.AreEqual(GeyserPhase.Erupt, trap.Phase);
        }

        [Test]
        public void ThePause_FreezesTheCycle()
        {
            trap = AddGeyser();
            StepUntil(GeyserPhase.Erupt);
            int roomTick = rig.Rooms.RoomLifeTick;
            Assert.IsTrue(rig.Pause.Pause());
            for (int i = 0; i < 40; i++) rig.Tick();
            Assert.AreEqual(roomTick, rig.Rooms.RoomLifeTick, "paused: no room tick ran");
            Assert.AreEqual(GeyserPhase.Erupt, trap.Phase);
            rig.Pause.Resume();
            rig.Pause.ApplyPendingResume();
            Step();
            Assert.AreEqual(roomTick + 1, rig.Rooms.RoomLifeTick);
            Assert.AreEqual(Expected(roomTick + 1), trap.Phase);
        }

        // ---------- Q2: the ApplyLaunch seam ----------

        [Test]
        public void ApplyLaunch_SetsTheAlongComponent_KeepsTheCross_ClearsCoyoteAndGrounded_AndKeepsTheBuffer()
        {
            rig.CatBody.linearVelocity = new Vector2(3f, -7f);
            PauseTestRig.SetPrivate(rig.Cat, "coyoteTimer", 5f);
            PauseTestRig.SetPrivate(rig.Cat, "jumpBufferTimer", 4f);
            rig.Cat.ApplyLaunch(Vector2.up, Speed);
            Assert.AreEqual(new Vector2(3f, Speed), rig.CatBody.linearVelocity);
            Assert.AreEqual(0f, PauseTestRig.GetPrivate<float>(rig.Cat, "coyoteTimer"));
            Assert.AreEqual(4f, PauseTestRig.GetPrivate<float>(rig.Cat, "jumpBufferTimer"));
            Assert.IsFalse(rig.Cat.IsGrounded);
        }

        [Test]
        public void ApplyLaunch_WhileFrozen_DoesNothing()
        {
            rig.Cat.Freeze();
            PauseTestRig.SetPrivate(rig.Cat, "coyoteTimer", 5f);
            rig.Cat.ApplyLaunch(Vector2.up, Speed);
            Assert.AreEqual(Vector2.zero, rig.CatBody.linearVelocity);
            Assert.AreEqual(5f, PauseTestRig.GetPrivate<float>(rig.Cat, "coyoteTimer"));
            rig.Cat.Unfreeze();
        }

        // The control: with coyote left (as on the first airborne steps after a walk-off), a press jumps. After a launch
        // the same press only fills the buffer: no coyote jump mid-air.
        [Test]
        public void APressAfterALaunch_IsNoCoyoteJump_ItOnlyFillsTheBuffer()
        {
            int bufferTicks = TickTime.ToWholeTicks(PauseTestRig.GetPrivate<CatMotorConfig>(rig.Cat, "config").JumpBufferTime);

            PauseTestRig.SetPrivate(rig.Cat, "coyoteTimer", 5f);
            input.Jump = true;
            rig.CatBody.linearVelocity = Vector2.zero;
            rig.Tick();
            Assert.Greater(rig.CatBody.linearVelocity.y, 1f, "control: coyote allows the jump");
            Assert.AreEqual(0f, PauseTestRig.GetPrivate<float>(rig.Cat, "jumpBufferTimer"), "control: the jump used the buffer");

            PauseTestRig.SetPrivate(rig.Cat, "coyoteTimer", 5f);
            rig.CatBody.linearVelocity = Vector2.zero;
            rig.Cat.ApplyLaunch(Vector2.up, Speed);
            input.Jump = true;
            rig.Tick();
            float g = G();
            Assert.AreEqual(Speed - g * TickTime.SecondsPerTick, rig.CatBody.linearVelocity.y, 1e-4f, "no jump: only gravity acted");
            Assert.AreEqual(bufferTicks - 1, (int)PauseTestRig.GetPrivate<float>(rig.Cat, "jumpBufferTimer"), "the press waits in the buffer");
        }

        float G() => rig.CatGo.GetComponent<GravityReceiver>().Strength;
    }
}
