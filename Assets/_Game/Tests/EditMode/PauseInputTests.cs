using System.Collections.Generic;
using NUnit.Framework;
using Parallax.Gameplay.Input;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    /// <summary>PAX-054 (D-073) §5.3: no input leaks through the pause. "Not applied" (Phase 1,
    /// developer ruling): latches the motor hasn't read yet - left over from before the pause,
    /// made during it, or made in the frame Resume is clicked - are dropped when the resume is
    /// applied at the end of that frame. The motor's own coyote/jump-buffer timers are game state
    /// and continue. The latches are set on the real input sources (by reflection, since EditMode
    /// can't drive real touches or keys), and the first step after resume runs through the real
    /// router, LocalHumanDriver and CatMotor2D.</summary>
    public sealed class PauseInputTests : PauseTestBase
    {
        public enum Source { TouchJump, TouchStick, KeyboardJump }
        public enum When { BeforePause, DuringPause, InTheResumeFrame }

        static void Latch(PauseTestRig rig, Source source)
        {
            switch (source)
            {
                case Source.TouchJump:
                    PauseTestRig.SetPrivate(rig.Touch, "jumpPressedLatch", true);
                    break;
                case Source.TouchStick:
                    PauseTestRig.SetPrivate(rig.Touch, "stickFingerId", 0);
                    PauseTestRig.SetPrivate(rig.Touch, "currentMove", 1f);
                    break;
                case Source.KeyboardJump:
                    PauseTestRig.SetPrivate(rig.Keyboard, "jumpPressedLatched", true);
                    break;
            }
        }

        [Test]
        public void LatchedInput_IsNotAppliedOnTheFirstStepAfterResume([Values] Source source, [Values] When when)
        {
            PauseTestRig rig = PauseTestRig.Build(realInput: true);
            try
            {
                rig.Tick();

                if (when == When.BeforePause) Latch(rig, source);
                rig.Pause.Pause();
                if (when == When.DuringPause) Latch(rig, source);
                rig.Pause.Resume(); // the Resume click, handled by the EventSystem's Update
                if (when == When.InTheResumeFrame) Latch(rig, source); // input read later in that same frame
                rig.Pause.ApplyPendingResume(); // LateUpdate, end of the frame

                // Give the motor a live coyote window so a leaked jump would actually fire, and
                // start from rest so a leaked stick would show up as horizontal speed.
                PauseTestRig.SetPrivate(rig.Cat, "coyoteTimer", 1f);
                rig.CatBody.linearVelocity = Vector2.zero;

                rig.Tick();

                Vector2 v = rig.CatBody.linearVelocity;
                if (source == Source.TouchStick)
                    Assert.AreEqual(0f, v.x, 1e-5f, $"a stick held {when} was applied on the first step after resume");
                else
                    Assert.LessOrEqual(v.y, 0f, $"a {source} latched {when} was applied on the first step after resume (the cat jumped)");
            }
            finally { rig.Dispose(); }
        }

        // Option B (developer ruling): the Resume click only requests; until the end of the frame
        // the level stays paused and the full-screen panel stays active and reserved, so a touch
        // whose Began TouchStickCatInput.Update reads later in that same frame is still ignored.
        [Test]
        public void ResumeClick_KeepsThePanelActiveAndReserved_UntilTheEndOfTheFrame()
        {
            PauseTestRig rig = PauseTestRig.Build(realInput: true);
            try
            {
                LayOutFullScreen(rig);
                var regions = new List<ITouchReservedRegion> { rig.Panel };
                Vector2 jumpZonePoint = new Vector2(2200f, 200f);

                rig.Pause.Pause();
                rig.Pause.Resume();
                Assert.IsTrue(rig.Pause.IsPaused, "the level must stay paused until the end of the Resume frame");
                Assert.IsTrue(rig.PanelGo.activeSelf, "the panel must stay visible until the end of the Resume frame");
                Assert.IsTrue(ReservedRegionCheck.IsReserved(regions, jumpZonePoint), "a touch beginning in the Resume frame must still be reserved");

                rig.Pause.ApplyPendingResume();
                Assert.IsFalse(rig.Pause.IsPaused);
                Assert.IsFalse(rig.PanelGo.activeSelf, "the panel must hide once the resume is applied");
                Assert.IsFalse(ReservedRegionCheck.IsReserved(regions, jumpZonePoint), "once hidden, the panel must no longer reserve the screen");
            }
            finally { rig.Dispose(); }
        }

        // A touch that begins on the pause button, or anywhere on the (full-screen) panel while
        // it's shown, is never claimed: TouchStickCatInput ignores a finger for its whole life
        // when its Began is reserved (TouchStickCatInput.cs:90-94, 115-119).
        [Test]
        public void TouchBeginningOnPauseButtonOrAnywhereOnThePanel_IsReserved()
        {
            PauseTestRig rig = PauseTestRig.Build(realInput: true);
            try
            {
                LayOutFullScreen(rig);
                var regions = new List<ITouchReservedRegion> { rig.PauseButton, rig.Panel };
                Vector2 onPauseButton = new Vector2(2330f, 960f);
                Vector2[] onPanel =
                {
                    new Vector2(1200f, 540f), // centre
                    new Vector2(5f, 5f), // bottom-left: stick zone
                    new Vector2(2395f, 5f), // bottom-right: jump zone
                    new Vector2(5f, 1075f), new Vector2(2395f, 1075f),
                };

                Assert.IsTrue(ReservedRegionCheck.IsReserved(regions, onPauseButton), "the pause button must reserve its own area while playing");
                Assert.IsFalse(ReservedRegionCheck.IsReserved(regions, onPanel[0]), "while playing, the hidden panel must reserve nothing");

                rig.Pause.Pause();
                Assert.IsTrue(ReservedRegionCheck.IsReserved(regions, onPauseButton), "the pause button must stay reserved while paused");
                foreach (Vector2 point in onPanel)
                    Assert.IsTrue(ReservedRegionCheck.IsReserved(regions, point), $"a touch beginning at {point} on the shown panel must be reserved");
            }
            finally { rig.Dispose(); }
        }

        // With no canvas, a RectTransform's world rect is its screen rect (null camera), so this
        // places the panel over a 2400x1080 screen and the pause button in its top-right corner.
        static void LayOutFullScreen(PauseTestRig rig)
        {
            var panel = (RectTransform)rig.PanelGo.transform;
            panel.anchoredPosition = new Vector2(1200f, 540f);
            panel.sizeDelta = new Vector2(2400f, 1080f);
            var button = (RectTransform)rig.PauseButtonGo.transform;
            button.anchoredPosition = new Vector2(2330f, 960f);
            button.sizeDelta = new Vector2(110f, 110f);
        }
    }
}
