using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Audio;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using UnityEngine.TestTools;

namespace Parallax.Tests.EditMode
{
    /// <summary>PAX-064 (D-158): RoomTrap.Fired, in the PauseTestRig solo room. It is raised exactly once per fire, on the
    /// fire tick, never by a reset, and a listener's exception is logged without stopping the trap.</summary>
    public sealed class TrapFiredEventTests : PauseTestBase
    {
        PauseTestRig rig;
        CheckpointSectionRuntimeTests.FireTrap trap;
        readonly List<int> heard = new List<int>();

        void Build(TrapRepeatMode mode = TrapRepeatMode.Once)
        {
            rig = PauseTestRig.Build(realInput: true, holdTicks: 30);
            var go = new GameObject("FireTrap");
            go.transform.SetParent(rig.CatGo.transform.parent, false);
            trap = go.AddComponent<CheckpointSectionRuntimeTests.FireTrap>();
            PauseTestRig.SetPrivate(trap, "roomId", 0);
            PauseTestRig.SetPrivate(trap, "roomDeath", rig.Death);
            PauseTestRig.SetPrivate(trap, "rooms", rig.Rooms);
            PauseTestRig.SetPrivate(trap, "repeatMode", mode);
            PauseTestRig.SetPrivate(trap, "cooldownTicks", 20);
            PauseTestRig.Invoke(trap, "Awake");
            PauseTestRig.Invoke(trap, "OnEnable");
            heard.Clear();
            trap.Fired += t => heard.Add(t.LatestFireTick);
        }

        [TearDown]
        public void Teardown() => rig?.Dispose();

        void Ticks(int n) { for (int i = 0; i < n; i++) rig.Tick(); }

        [Test]
        public void AFire_IsHeardOnce_OnItsFireTick()
        {
            Build();
            trap.Pressed = true;
            Ticks(40);
            Assert.AreEqual(1, heard.Count, "a Once trap fires once");
            Assert.AreEqual(trap.LatestFireTick, heard[0], "heard on the tick it fired");
            Assert.GreaterOrEqual(heard[0], 0);
        }

        [Test]
        public void ARearmingTrap_IsHeardEachTimeItFires()
        {
            Build(TrapRepeatMode.Rearm);
            trap.Pressed = true;
            Ticks(5);
            Assert.AreEqual(1, heard.Count);
            trap.Pressed = false;
            Ticks(30);
            trap.Pressed = true;
            Ticks(5);
            Assert.AreEqual(2, heard.Count, "fired again after its cooldown");
            Assert.Less(heard[0], heard[1]);
        }

        [Test]
        public void TheRoomsReset_IsNotAFire()
        {
            Build();
            trap.Pressed = true;
            Ticks(10);
            Assert.AreEqual(1, heard.Count);
            trap.Pressed = false;
            rig.Death.Kill(ObserverId.A, DeathCause.Hazard);
            for (int i = 0; i < 200 && rig.Death.IsHolding; i++) rig.Tick();
            Assert.AreEqual(-1, trap.LatestFireTick, "precondition: the trap was reset");
            Assert.AreEqual(1, heard.Count, "resetting raises nothing");
        }

        [Test]
        public void AListenersException_IsLogged_AndTheTrapStillFires()
        {
            Build();
            trap.Fired += _ => throw new System.InvalidOperationException("listener broke");
            trap.Pressed = true;
            LogAssert.Expect(LogType.Exception, new Regex("listener broke"));
            Ticks(10);
            Assert.AreEqual(TrapState.Fired, trap.State);
            Assert.AreEqual(1, heard.Count, "the other listener still heard it");
        }

        [Test]
        public void EachTrapKind_HasItsOwnCue()
        {
            var go = new GameObject("Kinds");
            try
            {
                Assert.AreEqual(AudioCue.TrapSpikes, TrapCues.For(go.AddComponent<HiddenSpikesTrap>()));
                Assert.AreEqual(AudioCue.TrapArrow, TrapCues.For(go.AddComponent<ArrowTrap>()));
                Assert.AreEqual(AudioCue.TrapCollapse, TrapCues.For(go.AddComponent<CollapsingFloorTrap>()));
                Assert.AreEqual(AudioCue.TrapFallingBlock, TrapCues.For(go.AddComponent<FallingBlockTrap>()));
                Assert.AreEqual(AudioCue.TrapFlip, TrapCues.For(go.AddComponent<GravityFlipTrap>()));
                Assert.AreEqual(AudioCue.TrapStorm, TrapCues.For(go.AddComponent<StormCloudTrap>()));
                Assert.AreEqual(AudioCue.TrapDoor, TrapCues.For(go.AddComponent<DoorRetreatTrap>()));
                Assert.AreEqual(AudioCue.TrapOther, TrapCues.For(go.AddComponent<CheckpointSectionRuntimeTests.FireTrap>()));
            }
            finally { Object.DestroyImmediate(go); }
        }
    }
}
