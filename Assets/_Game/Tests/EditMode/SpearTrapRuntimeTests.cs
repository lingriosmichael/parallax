using System.Reflection;
using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    /// <summary>PAX-084 (D-086) §5 and R2: a spear is an ArrowTrap with the spear flag and a shaft collider on its
    /// arrow child, in the PauseTestRig solo room (real ObserverSet, RoomManager, RoomDeath). EditMode doesn't
    /// simulate physics, so the cat stays where it's put. Its collider is a 1 x 0.56 box centred 0.3 below its root.
    /// Every spear here: launcher at x -5.25, mouth x -5, lane end x 5, 1.4 x 0.4, 1.2 u/tick, tell 8: travel 8.6,
    /// N = 8 flight ticks, so the stop tick is s = 16 and the stop + 1 check is s = 17. It fires on the first room tick
    /// (s = 0), from an overlap trigger around the cat's spawn.</summary>
    public sealed class SpearTrapRuntimeTests
    {
        const int Tell = 8, Flight = 8, StopTick = Tell + Flight, CheckTick = StopTick + 1;
        const float Length = 1.4f, Thickness = .4f;

        static ArrowTrap AddSpear(PauseTestRig rig, float laneY)
        {
            Transform reality = rig.CatGo.transform.parent;
            var go = new GameObject("Spear");
            go.transform.SetParent(reality, false);
            go.transform.localPosition = new Vector2(-5.25f, laneY);
            SpriteRenderer launcher = go.AddComponent<SpriteRenderer>();
            var arrowGo = new GameObject("Spear_Shaft");
            arrowGo.transform.SetParent(go.transform, false);
            SpriteRenderer arrow = arrowGo.AddComponent<SpriteRenderer>();
            BoxCollider2D shaft = arrowGo.AddComponent<BoxCollider2D>();
            shaft.size = new Vector2(Length, Thickness);
            shaft.enabled = false;

            var triggerGo = new GameObject("Trigger");
            triggerGo.transform.SetParent(go.transform, false);
            triggerGo.transform.position = reality.TransformPoint(Vector2.zero);
            BoxCollider2D trigger = triggerGo.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(2f, 2f);

            ArrowTrap trap = go.AddComponent<ArrowTrap>();
            PauseTestRig.SetPrivate(trap, "roomId", 0);
            PauseTestRig.SetPrivate(trap, "roomDeath", rig.Death);
            PauseTestRig.SetPrivate(trap, "rooms", rig.Rooms);
            PauseTestRig.SetPrivate(trap, "triggerSource", TrapTriggerSource.Overlap);
            PauseTestRig.SetPrivate(trap, "repeatMode", TrapRepeatMode.Once);
            PauseTestRig.SetPrivate(trap, "observers", rig.Observers);
            PauseTestRig.SetPrivate(trap, "trigger", trigger);
            PauseTestRig.SetPrivate(trap, "launcher", launcher);
            PauseTestRig.SetPrivate(trap, "arrow", arrow);
            PauseTestRig.SetPrivate(trap, "direction", ArrowDirection.Right);
            PauseTestRig.SetPrivate(trap, "mouth", new Vector2(.25f, 0f));
            PauseTestRig.SetPrivate(trap, "travel", 10f - Length);
            PauseTestRig.SetPrivate(trap, "arrowLength", Length);
            PauseTestRig.SetPrivate(trap, "arrowThickness", Thickness);
            PauseTestRig.SetPrivate(trap, "unitsPerTick", 1.2f);
            PauseTestRig.SetPrivate(trap, "tellTicks", Tell);
            PauseTestRig.SetPrivate(trap, "spear", true);
            PauseTestRig.SetPrivate(trap, "shaft", shaft);
            PauseTestRig.Invoke(trap, "Awake");
            PauseTestRig.Invoke(trap, "OnEnable");
            Physics2D.SyncTransforms();
            return trap;
        }

        static BoxCollider2D ShaftOf(ArrowTrap trap) => PauseTestRig.GetPrivate<BoxCollider2D>(trap, "shaft");
        static SpriteRenderer ArrowOf(ArrowTrap trap) => PauseTestRig.GetPrivate<SpriteRenderer>(trap, "arrow");

        // The kill box this tick, through the same function the route harness names killers with (R3).
        static bool KillBox(ArrowTrap trap, out Bounds box)
        {
            MethodInfo m = typeof(ArrowTrap).GetMethod("TryGetKillBox", BindingFlags.Public | BindingFlags.Instance);
            Assert.NotNull(m, "ArrowTrap.TryGetKillBox not found");
            object[] args = { null };
            bool lethal = (bool)m.Invoke(trap, args);
            box = (Bounds)args[0];
            return lethal;
        }

        static void MoveCat(PauseTestRig rig, Vector2 local)
        {
            Vector3 world = rig.CatGo.transform.parent.TransformPoint(local);
            rig.CatGo.transform.position = world;
            rig.CatBody.position = world;
            Physics2D.SyncTransforms();
        }

        // Lane y 3, far above the cat: nothing kills; the test reads the kill box and the shaft each tick.
        [Test]
        public void LethalOnTheFlightAndStopTicks_CheckedOnStopPlusOne_HarmlessAfter_AndSolidFromStopPlusOne()
        {
            PauseTestRig rig = PauseTestRig.Build(realInput: false, holdTicks: 30);
            try
            {
                ArrowTrap trap = AddSpear(rig, 3f);
                Assert.IsFalse(ShaftOf(trap).enabled, "unfired: the shaft is off");
                for (int s = 0; s <= CheckTick + 5; s++)
                {
                    rig.Tick();
                    Assert.AreEqual(s, rig.Rooms.RoomLifeTick - trap.LatestFireTick, "s");
                    bool lethal = KillBox(trap, out Bounds box);
                    Assert.AreEqual(s >= Tell && s <= CheckTick, lethal, $"s {s}: lethal");
                    if (lethal)
                    {
                        float shrink = s == CheckTick ? 2f * .02f : 0f;
                        Assert.AreEqual(Length - shrink, box.size.x, 1e-4f, $"s {s}: box width");
                        Assert.AreEqual(Thickness - shrink, box.size.y, 1e-4f, $"s {s}: box height");
                        Assert.AreEqual(ArrowOf(trap).transform.position.x, box.center.x, 1e-4f, $"s {s}: the box is the tick's pose");
                    }
                    Assert.AreEqual(s >= CheckTick, ShaftOf(trap).enabled, $"s {s}: shaft solid");
                }
                Assert.AreEqual(5f - Length * .5f, ArrowOf(trap).transform.position.x, 1e-4f, "stuck with its tip at the lane end x 5");
                Assert.AreEqual(0, rig.Death.DeathsIn(0));
            }
            finally { rig.Dispose(); }
        }

        // R2, seen red: the cat is moved into the stop pose after the stop tick (as if it walked in during that tick's
        // physics step). Stop + 1 kills it as a hazard, and the shaft stays off for the whole hold.
        [Test]
        public void ACatInTheShaft_OnStopPlusOne_IsKilled_AndTheShaftStaysOffThroughTheHold()
        {
            PauseTestRig rig = PauseTestRig.Build(realInput: false, holdTicks: 30);
            try
            {
                DeathCause? cause = null;
                rig.Death.Died += info => cause = info.Cause;
                ArrowTrap trap = AddSpear(rig, 1f);
                for (int s = 0; s <= StopTick; s++) rig.Tick();
                Assert.AreEqual(0, rig.Death.DeathsIn(0), "the cat was below the lane through the stop tick");
                // Stop pose x [3.6, 5] y [0.8, 1.2]; the cat's box x [3.8, 4.8] y [0.72, 1.28].
                MoveCat(rig, new Vector2(4.3f, 1.3f));
                rig.Tick();
                Assert.AreEqual(1, rig.Death.DeathsIn(0), "killed on stop + 1");
                Assert.IsTrue(rig.Death.IsHolding);
                for (int i = 0; i < 20; i++) { rig.Tick(); Assert.IsFalse(ShaftOf(trap).enabled, "the shaft stays off during the hold"); }
                for (int i = 0; i < 20; i++) rig.Tick();
                Assert.AreEqual(DeathCause.Hazard, cause);
                Assert.IsFalse(ShaftOf(trap).enabled, "after the reset the shaft is off");
            }
            finally { rig.Dispose(); }
        }

        // R2: a cat resting on the stop pose's top is outside the shaft shrunk by 0.02 a side: it survives stop + 1, and
        // the shaft switches on under it. "Resting" is where physics leaves a cat standing on a solid top: 0.005 above
        // it (SpearHarnessTests' drop lands the paw at 1.505 on the 1.5 top). Measured: Physics2D.OverlapBox reaches
        // about 0.02 across the two boxes' skins, so a mathematically exact touch (gap 0) still counts as inside.
        [Test]
        public void ACatRestingOnTheStopPoseTop_OnStopPlusOne_Survives_AndTheShaftSwitchesOn()
        {
            PauseTestRig rig = PauseTestRig.Build(realInput: false, holdTicks: 30);
            try
            {
                ArrowTrap trap = AddSpear(rig, 1f);
                for (int s = 0; s <= StopTick; s++) rig.Tick();
                // The cat's box bottom resting 0.005 over the stop pose's top, y 1.2.
                MoveCat(rig, new Vector2(4.3f, 1.2f + .005f + .58f));
                rig.Tick();
                Assert.AreEqual(0, rig.Death.DeathsIn(0), "touching the top is not inside the shaft");
                Assert.IsTrue(ShaftOf(trap).enabled, "solid from stop + 1");
                for (int i = 0; i < 10; i++) rig.Tick();
                Assert.AreEqual(0, rig.Death.DeathsIn(0), "never lethal once stuck");
            }
            finally { rig.Dispose(); }
        }

        [Test]
        public void RoomReset_PutsTheSpearBackInTheLauncher_Unfired_WithTheShaftOff()
        {
            PauseTestRig rig = PauseTestRig.Build(realInput: false, holdTicks: 0);
            try
            {
                ArrowTrap trap = AddSpear(rig, 3f);
                Vector3 start = ArrowOf(trap).transform.localPosition;
                for (int i = 0; i < CheckTick + 3; i++) rig.Tick();
                Assert.IsTrue(ShaftOf(trap).enabled);
                rig.Death.Kill(ObserverId.A, DeathCause.Hazard);
                Assert.IsFalse(ShaftOf(trap).enabled, "reset: shaft off");
                Assert.IsFalse(ArrowOf(trap).enabled, "reset: hidden");
                Assert.AreEqual(start, ArrowOf(trap).transform.localPosition, "reset: back at the mouth");
                Assert.AreEqual(-1, trap.LatestFireTick, "reset: unfired");
                Assert.IsFalse(KillBox(trap, out _), "reset: harmless");
            }
            finally { rig.Dispose(); }
        }

        // A spear with no shaft collider can't become a platform: it logs and disables itself, like a missing renderer.
        [Test]
        public void ASpearWithoutAShaft_DisablesItself()
        {
            PauseTestRig rig = PauseTestRig.Build(realInput: false, holdTicks: 30);
            try
            {
                UnityEngine.TestTools.LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("ArrowTrap 'Spear': .*shaft"));
                Transform reality = rig.CatGo.transform.parent;
                var go = new GameObject("Spear");
                go.transform.SetParent(reality, false);
                ArrowTrap trap = go.AddComponent<ArrowTrap>();
                PauseTestRig.SetPrivate(trap, "roomDeath", rig.Death);
                PauseTestRig.SetPrivate(trap, "rooms", rig.Rooms);
                PauseTestRig.SetPrivate(trap, "observers", rig.Observers);
                PauseTestRig.SetPrivate(trap, "triggerSource", TrapTriggerSource.Chain);
                PauseTestRig.SetPrivate(trap, "launcher", go.AddComponent<SpriteRenderer>());
                var arrowGo = new GameObject("Spear_Shaft");
                arrowGo.transform.SetParent(go.transform, false);
                PauseTestRig.SetPrivate(trap, "arrow", arrowGo.AddComponent<SpriteRenderer>());
                PauseTestRig.SetPrivate(trap, "spear", true);
                PauseTestRig.Invoke(trap, "Awake");
                Assert.IsFalse(trap.enabled);
            }
            finally { rig.Dispose(); }
        }
    }
}
