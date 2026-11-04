using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Presentation;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    /// <summary>D-293: the moments with no state of their own.</summary>
    public sealed class CatTransitionClipsTests
    {
        const float Dt = 1f / 60f;

        static CatClip Clip(string slot, CatAnimState state, int frames, float fps, bool loop = false) =>
            new(slot, state, new Sprite[frames], fps, loop, null, null);

        readonly CatClip fall = Clip("Fall", CatAnimState.Fall, 3, 12f), fallLoop = Clip("FallLoop", CatAnimState.Fall, 4, 12f, true),
            stepDown = Clip("StepDown", CatAnimState.Fall, 3, 24f), sit = Clip("IdleSit", CatAnimState.IdleFidget, 8, 12f),
            standUp = Clip("StandUp", CatAnimState.Idle, 3, 30f), walk = Clip("Walk", CatAnimState.Walk, 11, 10f, true),
            push = Clip("WallPush", CatAnimState.Idle, 3, 6f, true), reach = Clip("Reach", CatAnimState.Rise, 2, 24f),
            land = Clip("Land", CatAnimState.Land, 4, 24f), climb = Clip("Climb", CatAnimState.Climb, 25, 21f, true);

        static CatClipSet Set(params CatClip[] clips)
        {
            var set = new CatClipSet();
            typeof(CatClipSet).GetField("clips", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(set, clips);
            return set;
        }

        CatTransitionClips Transitions() => new(Set(fall, fallLoop, stepDown, sit, standUp, walk, push, reach, land, climb));

        [Test]
        public void ALongFall_LoopsFromFallsLastFrame_AndNotBefore()
        {
            CatTransitionClips t = Transitions();
            Assert.IsFalse(t.Step(CatAnimState.Fall, CatAnimState.Fall, fall, 1, Dt), "still the arc's frames");
            Assert.IsTrue(t.Step(CatAnimState.Fall, CatAnimState.Fall, fall, 2, Dt));
            Assert.AreSame(fallLoop, t.Clip);
            for (int i = 0; i < 120; i++) Assert.IsTrue(t.Step(CatAnimState.Fall, CatAnimState.Fall, fallLoop, t.Frame, Dt), "it keeps looping");
            Assert.That(t.Frame, Is.InRange(0, 3));
            Assert.IsFalse(t.Step(CatAnimState.Fall, CatAnimState.Land, fallLoop, t.Frame, Dt), "a landing ends it");
        }

        [Test]
        public void WalkingOffALedge_PlaysStepDownOnce_ThenTheFall()
        {
            CatTransitionClips t = Transitions();
            Assert.IsTrue(t.Step(CatAnimState.Walk, CatAnimState.Fall, walk, 4, Dt));
            Assert.AreSame(stepDown, t.Clip);
            int shown = 0;
            while (t.Step(CatAnimState.Fall, CatAnimState.Fall, stepDown, t.Frame, Dt)) Assert.Less(++shown, 20);
            Assert.GreaterOrEqual(shown, 6, "0.125 s at 60 fps");
            Assert.IsFalse(t.Step(CatAnimState.Fall, CatAnimState.Fall, stepDown, 2, Dt), "and never again in that fall");
        }

        [Test]
        public void AJump_IsNeverAStepDown() =>
            Assert.IsFalse(Transitions().Step(CatAnimState.TakeOff, CatAnimState.Fall, walk, 0, Dt));

        [Test]
        public void LeavingTheSit_StandsUpWhileMoving_FromHowFarDownItWas()
        {
            CatTransitionClips t = Transitions();
            Assert.IsTrue(t.Step(CatAnimState.IdleFidget, CatAnimState.Walk, sit, 7, Dt));
            Assert.AreSame(standUp, t.Clip);
            Assert.AreEqual(0, t.Frame, "seated: the whole rise");
            t = Transitions();
            Assert.IsTrue(t.Step(CatAnimState.IdleFidget, CatAnimState.Walk, sit, 3, Dt));
            Assert.AreEqual(1, t.Frame, "half way down: the rest of it");
            Assert.IsFalse(Transitions().Step(CatAnimState.IdleFidget, CatAnimState.Walk, sit, 1, Dt), "barely down: straight into the walk");
            Assert.IsFalse(Transitions().Step(CatAnimState.IdleFidget, CatAnimState.TakeOff, sit, 7, Dt), "a jump wins");
        }

        [Test]
        public void PressingWhileStill_PushesAfterAMoment_AndLettingGoEndsIt()
        {
            CatTransitionClips t = Transitions();
            Assert.IsFalse(t.Step(CatAnimState.Idle, CatAnimState.Idle, walk, 0, Dt, pressing: true), "a walk starting isn't a push");
            int frames = 1;
            while (!t.Step(CatAnimState.Idle, CatAnimState.Idle, walk, 0, Dt, pressing: true)) Assert.Less(++frames, 20);
            Assert.AreSame(push, t.Clip);
            for (int i = 0; i < 90; i++) Assert.IsTrue(t.Step(CatAnimState.Idle, CatAnimState.Idle, push, t.Frame, Dt, pressing: true));
            Assert.IsFalse(t.Step(CatAnimState.Idle, CatAnimState.Idle, push, t.Frame, Dt, pressing: false));
        }

        [Test]
        public void OntoAVine_Reaches_AndOffIt_LandsOnTheCrouch()
        {
            CatTransitionClips t = Transitions();
            Assert.IsTrue(t.Step(CatAnimState.Fall, CatAnimState.Climb, fall, 2, Dt));
            Assert.AreSame(reach, t.Clip);
            t = Transitions();
            Assert.IsTrue(t.Step(CatAnimState.Hang, CatAnimState.Leap, climb, 0, Dt));
            Assert.AreSame(reach, t.Clip);
            t = Transitions();
            Assert.IsTrue(t.Step(CatAnimState.Climb, CatAnimState.Idle, climb, 3, Dt));
            Assert.AreSame(land, t.Clip);
            Assert.AreEqual(1, t.Frame);
            Assert.IsFalse(Transitions().Step(CatAnimState.Climb, CatAnimState.Climb, climb, 3, Dt), "climbing on");
        }

        [Test]
        public void AGroundTurn_OpensFromNarrow_ToFullWidth()
        {
            Assert.AreEqual(.35f, CatTransitionClips.TurnWidth(0f, .35f, .08f), 1e-5f);
            Assert.AreEqual(1f, CatTransitionClips.TurnWidth(.08f, .35f, .08f), 1e-5f);
            Assert.AreEqual(1f, CatTransitionClips.TurnWidth(float.PositiveInfinity, .35f, .08f), 1e-5f, "at rest it's the full width");
            Assert.AreEqual(1f, CatTransitionClips.TurnWidth(0f, .35f, 0f), 1e-5f);
        }

        [Test]
        public void WithoutTheClips_NothingShows()
        {
            var t = new CatTransitionClips(Set(fall, walk));
            Assert.IsFalse(t.Step(CatAnimState.Fall, CatAnimState.Fall, fall, 2, Dt));
            Assert.IsFalse(t.Step(CatAnimState.Walk, CatAnimState.Fall, walk, 0, Dt));
        }
    }
}
