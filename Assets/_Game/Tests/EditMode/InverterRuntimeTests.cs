using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Observers;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    /// <summary>PAX-085 (D-087) §5, R1, R3, R4: an InverterTrap in the PauseTestRig solo room (real ObserverSet,
    /// RoomManager, RoomDeath, LevelPause, CatInputRouter and LocalHumanDriver). Input comes from a scripted source added
    /// to the router, as the route harness does. EditMode doesn't simulate physics, so the cat stays where it's put: each
    /// tick starts from zero velocity, and the sign of the velocity the motor sets is the sign of the Move it received.
    /// ObserverSet tick k is room tick k - 1 (no deaths). The trap's trigger is a 1 x 1 box at x 10; the cat touches it
    /// when a test moves it there.</summary>
    public sealed class InverterRuntimeTests : PauseTestBase
    {
        const int Duration = 150;
        static readonly Vector2 TriggerAt = new(10f, 0f);

        PauseTestRig rig;
        Scripted input;
        InverterTrap trap;

        sealed class Scripted : ICatCommandSource
        {
            public float Move;
            public bool Jump;
            public CatCommand Read() { var c = new CatCommand { Move = Move, JumpPressed = Jump, JumpHeld = Jump }; Jump = false; return c; }
            public void ResetTransientState() => Jump = false;   // a held stick stays held (the rig's own reset)
        }

        [SetUp]
        public void Build()
        {
            rig = PauseTestRig.Build(realInput: true);
            input = new Scripted { Move = 1f };
            PauseTestRig.GetPrivate<List<ICatCommandSource>>(rig.Router, "validSources").Add(input);
        }

        [TearDown]
        public void Teardown() => rig?.Dispose();

        InverterTrap AddInverter(TrapRepeatMode repeat = TrapRepeatMode.Once, int cooldown = 0)
        {
            Transform reality = rig.CatGo.transform.parent;
            var go = new GameObject("Inverter");
            go.transform.SetParent(reality, false);
            go.transform.localPosition = TriggerAt;
            go.layer = rig.CatGo.layer;
            BoxCollider2D box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = Vector2.one;
            SpriteRenderer ring = new GameObject("Cue_Ring").AddComponent<SpriteRenderer>();
            ring.transform.SetParent(go.transform, false);
            SpriteRenderer mark = new GameObject("Cue_Mark").AddComponent<SpriteRenderer>();
            mark.transform.SetParent(go.transform, false);

            InverterTrap t = go.AddComponent<InverterTrap>();
            PauseTestRig.SetPrivate(t, "roomId", 0);
            PauseTestRig.SetPrivate(t, "roomDeath", rig.Death);
            PauseTestRig.SetPrivate(t, "rooms", rig.Rooms);
            PauseTestRig.SetPrivate(t, "observers", rig.Observers);
            PauseTestRig.SetPrivate(t, "triggerSource", TrapTriggerSource.Overlap);
            PauseTestRig.SetPrivate(t, "repeatMode", repeat);
            PauseTestRig.SetPrivate(t, "cooldownTicks", cooldown);
            PauseTestRig.SetPrivate(t, "trigger", box);
            PauseTestRig.SetPrivate(t, "ring", ring);
            PauseTestRig.SetPrivate(t, "mark", mark);
            PauseTestRig.SetPrivate(t, "durationTicks", Duration);
            PauseTestRig.Invoke(t, "Awake");
            PauseTestRig.Invoke(t, "OnEnable");
            // The driver collects modifiers when it activates (R3), as it does in a loaded level.
            rig.Observer.SetDriver(new LocalHumanDriver(rig.Router));
            Physics2D.SyncTransforms();
            return t;
        }

        void MoveCat(Vector2 local)
        {
            rig.CatGo.transform.position = rig.CatGo.transform.parent.TransformPoint(local);
            Physics2D.SyncTransforms();
        }

        // One tick from rest; returns the horizontal velocity the motor set (its sign is the Move it received).
        float Step()
        {
            rig.CatBody.linearVelocity = Vector2.zero;
            rig.Tick();
            return rig.CatBody.linearVelocity.x;
        }

        // Ticks until the trap fires on the tick the cat is moved onto its trigger; returns that ObserverSet tick.
        int FireAt(int ticksBefore)
        {
            for (int i = 0; i < ticksBefore; i++) Assert.Greater(Step(), 0f, "not inverted before the fire");
            MoveCat(TriggerAt);
            float vx = Step();
            Assert.Greater(vx, 0f, "R4: the fire tick's own step still moves right");
            Assert.AreEqual(rig.Rooms.RoomLifeTick, trap.LatestFireTick, "fired in this tick's room step");
            MoveCat(Vector2.zero);
            return rig.Observers.Tick;
        }

        static IList<IControlModifier> Modifiers(LocalHumanDriver driver) => PauseTestRig.GetPrivate<IControlModifier[]>(driver, "modifiers");

        [Test]
        public void TheDriver_CollectsTheInverterUnderItsReality_WhenItActivates()
        {
            trap = AddInverter();
            var driver = (LocalHumanDriver)rig.Observer.Driver;
            CollectionAssert.Contains(Modifiers(driver).ToList(), trap);
        }

        [Test]
        public void ARealityWithNoInverter_GivesAnEmptyList_AndMoveIsUnchanged()
        {
            rig.Observer.SetDriver(new LocalHumanDriver(rig.Router));
            CollectionAssert.IsEmpty(Modifiers((LocalHumanDriver)rig.Observer.Driver).ToList());
            for (int i = 0; i < 20; i++) Assert.Greater(Step(), 0f);
        }

        [Test]
        public void HoldRight_MovesRightAtTheFireTick_LeftFromTheNext_ForExactlyTheDuration_ThenRightAgain()
        {
            trap = AddInverter();
            int t = FireAt(5);
            for (int k = 1; k <= Duration; k++)
            {
                Assert.IsTrue(trap.InvertsMove, $"T+{k} inverted");
                Assert.Less(Step(), 0f, $"T+{k} (tick {t + k}) moves left");
            }
            Assert.IsFalse(trap.InvertsMove, "the window has ended");
            Assert.Greater(Step(), 0f, $"T+{Duration + 1} moves right again");
        }

        [Test]
        public void HoldLeft_WhileInverted_MovesRight()
        {
            trap = AddInverter();
            FireAt(3);
            input.Move = -1f;
            for (int k = 1; k <= 10; k++) Assert.Greater(Step(), 0f, $"T+{k}");
        }

        [Test]
        public void TheCue_IsOnFromTheFireTick_ThroughTheLastInvertedStep_BlinkingOverTheLastThirty()
        {
            trap = AddInverter();
            Assert.IsFalse(trap.IsCueShown, "off before the fire");
            FireAt(4);
            Assert.IsTrue(trap.Ring.enabled && trap.Mark.enabled, "on at the fire tick");
            var seen = new List<bool>();
            for (int k = 1; k <= Duration + 1; k++)
            {
                Step();
                Assert.AreEqual(trap.Ring.enabled, trap.Mark.enabled, "ring and mark switch together");
                seen.Add(trap.Ring.enabled);
            }
            for (int k = 1; k <= Duration - 30; k++) Assert.IsTrue(seen[k - 1], $"steady at T+{k}");
            for (int j = 0; j < 30; j++) Assert.AreEqual((j / 5) % 2 == 1, seen[Duration - 30 + j], $"blink at T+{Duration - 29 + j}");
            Assert.IsTrue(seen[Duration - 1], "on at the last inverted step");
            Assert.IsFalse(seen[Duration], "off at T+151");
        }

        [Test]
        public void TheDeathHold_FreezesTheTimer_AndTheResetClearsIt()
        {
            trap = AddInverter();
            FireAt(2);
            for (int k = 1; k <= 10; k++) Assert.Less(Step(), 0f);
            int roomTick = rig.Rooms.RoomLifeTick;
            bool cue = trap.IsCueShown;
            rig.Death.Kill(ObserverId.A, DeathCause.Hazard);
            Assert.IsTrue(rig.Death.IsHolding);
            for (int i = 0; i < 29; i++)
            {
                rig.Tick();
                if (!rig.Death.IsHolding) break;
                Assert.IsFalse(trap.InvertsMove, "R3: never inverted during the hold");
                Assert.AreEqual(roomTick, rig.Rooms.RoomLifeTick, "the room tick is frozen");
                Assert.AreEqual(cue, trap.IsCueShown, "the room shows exactly what killed you");
            }
            for (int i = 0; i < 10 && rig.Death.IsHolding; i++) rig.Tick();
            Assert.IsFalse(rig.Death.IsHolding);
            Assert.IsFalse(trap.IsCueShown, "the reset clears the cue");
            Assert.IsFalse(trap.InvertsMove, "the reset clears the window");
            for (int i = 0; i < 5; i++) Assert.Greater(Step(), 0f, "right is right again after the respawn");
        }

        [Test]
        public void ThePause_FreezesTheTimer_AndTheResumeKeepsIt()
        {
            trap = AddInverter();
            FireAt(2);
            for (int k = 1; k <= 10; k++) Assert.Less(Step(), 0f);
            Assert.IsTrue(rig.Pause.Pause());
            int tick = rig.Observers.Tick;
            for (int i = 0; i < 40; i++) rig.Tick();
            Assert.AreEqual(tick, rig.Observers.Tick, "paused: no tick ran");
            rig.Pause.Resume();
            rig.Pause.ApplyPendingResume();
            int inverted = 0;
            while (Step() < 0f && inverted < 1000) inverted++;
            Assert.AreEqual(Duration - 10, inverted, "resume (router.ResetTransientState) doesn't clear it, and the pause didn't use any of it");
        }

        [Test]
        public void TheRoomEnding_ClearsTheWindowAndTheCue()
        {
            trap = AddInverter();
            FireAt(2);
            Step();
            rig.CompleteLevel();
            rig.Tick();
            Assert.IsTrue(rig.Rooms.LevelComplete);
            Assert.IsFalse(trap.InvertsMove);
            Assert.IsFalse(trap.IsCueShown, "no cue left over once the room is no longer live");
        }

        // Review round 1: OnDisable clears latched state, so a disabled trap leaves no cue behind and no window to resume.
        [Test]
        public void DisablingTheTrapMidWindow_HidesTheCue_AndClearsTheWindow()
        {
            trap = AddInverter();
            FireAt(2);
            for (int k = 1; k <= 10; k++) Step();
            Assert.IsTrue(trap.IsCueShown);
            trap.enabled = false;
            PauseTestRig.Invoke(trap, "OnDisable");
            Assert.IsFalse(trap.IsCueShown, "no cue left on the cat");
            trap.enabled = true;
            PauseTestRig.Invoke(trap, "OnEnable");
            Assert.IsFalse(trap.InvertsMove, "the window doesn't resume");
            Assert.Greater(Step(), 0f);
        }

        [Test]
        public void ARearmRefire_RestartsTheWindow()
        {
            trap = AddInverter(TrapRepeatMode.Rearm, cooldown: 20);
            int t = FireAt(2);
            for (int k = 1; k <= 60; k++) Assert.Less(Step(), 0f);
            MoveCat(TriggerAt);
            Step();
            int refire = rig.Observers.Tick;
            Assert.AreEqual(rig.Rooms.RoomLifeTick, trap.LatestFireTick, "refired");
            MoveCat(Vector2.zero);
            for (int k = 1; k <= Duration; k++) Assert.Less(Step(), 0f, $"T'+{k} (the first window ended at T+{Duration}, tick {t + Duration})");
            Assert.Greater(Step(), 0f, $"T'+{Duration + 1} (tick {refire + Duration + 1}) moves right");
        }
    }
}
