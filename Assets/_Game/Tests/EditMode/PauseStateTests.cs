using NUnit.Framework;
using Parallax.Core;

namespace Parallax.Tests.EditMode
{
    /// <summary>PAX-054 (D-073) §5.1: the pure pause state, and the auto-pause rule (§2.1.4, 3.7).
    /// Resume is a request: it only takes effect when ApplyPendingResume runs (LevelPause calls
    /// it from LateUpdate, after every Update in the frame has read its input).</summary>
    public sealed class PauseStateTests
    {
        [Test]
        public void StartsUnpaused_ThenPauseTakesEffectImmediately()
        {
            var state = new PauseState();
            Assert.IsFalse(state.IsPaused, "a new pause state must start unpaused");
            Assert.IsFalse(state.ResumePending);

            state.Pause();
            Assert.IsTrue(state.IsPaused, "Pause must take effect immediately");
        }

        [Test]
        public void Pause_IsIdempotent()
        {
            var state = new PauseState();
            state.Pause();
            state.Pause();
            Assert.IsTrue(state.IsPaused);

            state.Resume();
            Assert.IsTrue(state.ApplyPendingResume(), "one resume must undo any number of pauses");
            Assert.IsFalse(state.IsPaused);
        }

        [Test]
        public void Resume_StaysPausedUntilApplied()
        {
            var state = new PauseState();
            state.Pause();
            state.Resume();
            Assert.IsTrue(state.IsPaused, "Resume is only a request; the state stays paused until it's applied");
            Assert.IsTrue(state.ResumePending, "Resume must leave a pending request");

            Assert.IsTrue(state.ApplyPendingResume());
            Assert.IsFalse(state.IsPaused);
            Assert.IsFalse(state.ResumePending);
        }

        [Test]
        public void Resume_TwiceInOneFrame_AppliesOnce()
        {
            var state = new PauseState();
            state.Pause();
            state.Resume();
            state.Resume();

            Assert.IsTrue(state.ApplyPendingResume(), "the first apply resumes");
            Assert.IsFalse(state.ApplyPendingResume(), "the second request was the same request; nothing is left to apply");
            Assert.IsFalse(state.IsPaused);
        }

        [Test]
        public void PauseAfterResume_InTheSameFrame_CancelsTheResume()
        {
            var state = new PauseState();
            state.Pause();
            state.Resume();
            state.Pause();

            Assert.IsFalse(state.ResumePending, "a later Pause in the same frame must cancel the pending resume");
            Assert.IsFalse(state.ApplyPendingResume());
            Assert.IsTrue(state.IsPaused, "the level must still be paused at the end of the frame");
        }

        [Test]
        public void Resume_WhenNotPaused_LeavesNothingPending_AndPauseStillWorks()
        {
            var state = new PauseState();
            state.Resume();
            Assert.IsFalse(state.ResumePending, "resuming an unpaused state must not leave a request that could cancel a later pause");
            Assert.IsFalse(state.ApplyPendingResume());

            state.Pause();
            Assert.IsFalse(state.ApplyPendingResume(), "a stale resume must never unpause a later pause");
            Assert.IsTrue(state.IsPaused);
        }

        // D-073: background always pauses; focus loss pauses unless focus loss is ignored (the
        // Editor, where clicking any other window loses focus). The flag is passed in, so both
        // rows are covered without depending on Application.isEditor.
        [Test]
        public void AutoPausePolicy_TruthTable()
        {
            Assert.IsTrue(AutoPausePolicy.ShouldPause(AppLifecycleEvent.Background, ignoreFocusLoss: false), "player: background pauses");
            Assert.IsTrue(AutoPausePolicy.ShouldPause(AppLifecycleEvent.Background, ignoreFocusLoss: true), "Editor: background still pauses");
            Assert.IsTrue(AutoPausePolicy.ShouldPause(AppLifecycleEvent.FocusLost, ignoreFocusLoss: false), "player: focus loss pauses");
            Assert.IsFalse(AutoPausePolicy.ShouldPause(AppLifecycleEvent.FocusLost, ignoreFocusLoss: true), "Editor: focus loss does not pause");
        }
    }
}
