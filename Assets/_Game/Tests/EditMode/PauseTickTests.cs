using NUnit.Framework;
using Parallax.Core;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    /// <summary>PAX-054 (D-073) §5.2: nothing advances while paused. Every piece of level state the
    /// ticket lists (cat movement, the observer tick, traps and their timers, the death hold,
    /// respawn and the death count) runs from ObserverSet.FixedUpdate, so these tests invoke it
    /// directly while paused. That bypasses Time.timeScale entirely: what they prove is our tick
    /// owner honouring the pause gate, not Unity skipping FixedUpdate at time scale 0.</summary>
    public sealed class PauseTickTests : PauseTestBase
    {
        const int PausedTicks = 20;

        static void ResumeNow(PauseTestRig rig)
        {
            rig.Pause.Resume();
            rig.Pause.ApplyPendingResume();
        }

        [Test]
        public void ObserverTick_WhilePaused_NeitherAdvancesNorSteps_ThenContinues()
        {
            PauseTestRig rig = PauseTestRig.Build(realInput: false);
            try
            {
                for (int i = 0; i < 3; i++) rig.Tick();
                int stepped = 0;
                rig.Observers.Stepped += _ => stepped++;

                rig.Pause.Pause();
                for (int i = 0; i < PausedTicks; i++) rig.Tick();
                Assert.AreEqual(3, rig.Observers.Tick, "ObserverSet.Tick advanced while paused");
                Assert.AreEqual(0, stepped, "ObserverSet raised Stepped while paused");

                ResumeNow(rig);
                rig.Tick();
                Assert.AreEqual(4, rig.Observers.Tick, "the first tick after resume must continue from the same tick count");
                Assert.AreEqual(1, stepped);
            }
            finally { rig.Dispose(); }
        }

        [Test]
        public void Cat_WhilePaused_KeepsVelocityAndPosition_ThenContinuesAsIfNeverPaused()
        {
            Vector2 expectedVelocity;
            PauseTestRig control = PauseTestRig.Build(realInput: false);
            try
            {
                for (int i = 0; i < 4; i++) control.Tick();
                expectedVelocity = control.CatBody.linearVelocity;
            }
            finally { control.Dispose(); }

            PauseTestRig rig = PauseTestRig.Build(realInput: false);
            try
            {
                for (int i = 0; i < 3; i++) rig.Tick();
                Vector2 velocityAtPause = rig.CatBody.linearVelocity;
                Vector2 positionAtPause = rig.CatBody.position;

                rig.Pause.Pause();
                for (int i = 0; i < PausedTicks; i++) rig.Tick();
                Assert.AreEqual(velocityAtPause, rig.CatBody.linearVelocity, "the cat's velocity changed while paused");
                Assert.AreEqual(positionAtPause, rig.CatBody.position, "the cat moved while paused");

                ResumeNow(rig);
                rig.Tick();
                Assert.AreEqual(expectedVelocity.x, rig.CatBody.linearVelocity.x, 1e-5f, "resume must continue from the paused values (x)");
                Assert.AreEqual(expectedVelocity.y, rig.CatBody.linearVelocity.y, 1e-5f, "resume must continue from the paused values (y)");
            }
            finally { rig.Dispose(); }
        }

        [Test]
        public void Traps_WhilePaused_DoNotStep_AndRoomLifeTickFreezes()
        {
            PauseTestRig rig = PauseTestRig.Build(realInput: false, withTrap: true);
            try
            {
                for (int i = 0; i < 3; i++) rig.Tick();
                int trapSteps = rig.Trap.Steps;
                int roomLifeTick = rig.Rooms.RoomLifeTick;
                int latestFire = rig.Trap.LatestFireTick;
                Assert.AreEqual(3, trapSteps, "rig sanity: the trap steps once per live tick");

                rig.Pause.Pause();
                for (int i = 0; i < PausedTicks; i++) rig.Tick();
                Assert.AreEqual(trapSteps, rig.Trap.Steps, "a trap stepped while paused");
                Assert.AreEqual(roomLifeTick, rig.Rooms.RoomLifeTick, "RoomLifeTick (every trap timer's clock) advanced while paused");
                Assert.AreEqual(latestFire, rig.Trap.LatestFireTick, "a trap timer changed while paused");

                ResumeNow(rig);
                rig.Tick();
                Assert.AreEqual(trapSteps + 1, rig.Trap.Steps);
                Assert.AreEqual(roomLifeTick + 1, rig.Rooms.RoomLifeTick, "RoomLifeTick must continue from the same value");
            }
            finally { rig.Dispose(); }
        }

        [Test]
        public void DeathHold_WhilePaused_KeepsItsRemainingTicks_AndCountsNothingNew()
        {
            const int holdTicks = 10;
            const int ticksBeforePause = 4;
            Vector2 deathSpot = new Vector2(20f, 0f);
            PauseTestRig rig = PauseTestRig.Build(realInput: false, holdTicks: holdTicks);
            try
            {
                rig.Tick();
                rig.CatGo.transform.position = deathSpot;
                rig.Death.Kill(ObserverId.A, DeathCause.Hazard);
                for (int i = 0; i < ticksBeforePause; i++) rig.Tick();
                Assert.IsTrue(rig.Death.IsHolding, "rig sanity: still inside the hold");

                rig.Pause.Pause();
                for (int i = 0; i < PausedTicks; i++) rig.Tick();
                Assert.IsTrue(rig.Death.IsHolding, "the death hold ran out while paused");
                Assert.AreEqual(1, rig.Death.DeathsIn(0), "the death count changed while paused");
                Assert.AreEqual(deathSpot, (Vector2)rig.CatGo.transform.position, "the cat respawned while paused");

                ResumeNow(rig);
                int remaining = holdTicks - ticksBeforePause;
                for (int i = 1; i < remaining; i++)
                {
                    rig.Tick();
                    Assert.IsTrue(rig.Death.IsHolding, $"the hold ended early after resume (tick {i} of {remaining})");
                }
                rig.Tick();
                Assert.IsFalse(rig.Death.IsHolding, "the hold must end exactly when its remaining ticks run out");
                Assert.AreEqual(Vector2.zero, (Vector2)rig.CatGo.transform.position, "the respawn must continue normally after resume");
                Assert.AreEqual(1, rig.Death.DeathsIn(0), "resuming must never add or remove a death");
            }
            finally { rig.Dispose(); }
        }
    }
}
