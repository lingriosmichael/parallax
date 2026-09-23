using System;
using System.Reflection;
using NUnit.Framework;
using Parallax.Gameplay.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Parallax.Tests.EditMode
{
    /// <summary>PAX-054 (D-073): LevelPause itself - setting and restoring the running state
    /// through the RunningState seam, the deferred resume (option B), same-frame Pause/Resume
    /// ordering, the teardown restore, selection clearing, the pause button and panel buttons,
    /// auto-pause (§5.7) and the complete-screen hook (§5.6).</summary>
    public sealed class LevelPauseTests : PauseTestBase
    {
        // ---------- running state and the deferred resume ----------

        [Test]
        public void Pause_FreezesRunningStateAndShowsPanel_ResumeWaitsForApply()
        {
            PauseTestRig rig = PauseTestRig.Build(realInput: true);
            try
            {
                Assert.IsTrue(rig.Pause.Pause(), "Pause must succeed on a live level");
                Assert.IsTrue(rig.Pause.IsPaused);
                Assert.IsTrue(rig.PanelGo.activeSelf, "Pause must show the panel");
                CollectionAssert.AreEqual(new[] { 0f }, TimeScaleWrites, "Pause must freeze the running state exactly once");

                rig.Pause.Resume();
                Assert.IsTrue(rig.Pause.IsPaused, "Resume must not take effect before ApplyPendingResume");
                Assert.IsTrue(rig.Pause.IsResumePending);
                Assert.IsTrue(rig.PanelGo.activeSelf);
                CollectionAssert.AreEqual(new[] { 0f }, TimeScaleWrites, "the running state must not be restored before ApplyPendingResume");

                rig.Pause.ApplyPendingResume();
                Assert.IsFalse(rig.Pause.IsPaused);
                Assert.IsFalse(rig.PanelGo.activeSelf, "the applied resume must hide the panel");
                CollectionAssert.AreEqual(new[] { 0f, 1f }, TimeScaleWrites, "the applied resume must restore the running state");
            }
            finally { rig.Dispose(); }
        }

        [Test]
        public void Pause_Twice_FreezesOnce()
        {
            PauseTestRig rig = PauseTestRig.Build(realInput: true);
            try
            {
                rig.Pause.Pause();
                rig.Pause.Pause();
                Assert.IsTrue(rig.Pause.IsPaused);
                CollectionAssert.AreEqual(new[] { 0f }, TimeScaleWrites);
            }
            finally { rig.Dispose(); }
        }

        [Test]
        public void Resume_TwiceInOneFrame_RestoresOnce()
        {
            PauseTestRig rig = PauseTestRig.Build(realInput: true);
            try
            {
                rig.Pause.Pause();
                rig.Pause.Resume();
                rig.Pause.Resume();
                rig.Pause.ApplyPendingResume();
                rig.Pause.ApplyPendingResume(); // a second LateUpdate in a later frame
                Assert.IsFalse(rig.Pause.IsPaused);
                CollectionAssert.AreEqual(new[] { 0f, 1f }, TimeScaleWrites, "two Resume clicks in one frame must restore exactly once");
            }
            finally { rig.Dispose(); }
        }

        [Test]
        public void PauseAfterResume_InTheSameFrame_StaysPaused()
        {
            PauseTestRig rig = PauseTestRig.Build(realInput: true);
            try
            {
                rig.Pause.Pause();
                rig.Pause.Resume();
                rig.Pause.Pause(); // e.g. focus lost later in the same frame
                rig.Pause.ApplyPendingResume();

                Assert.IsTrue(rig.Pause.IsPaused, "a Pause after Resume in the same frame must win");
                Assert.IsTrue(rig.PanelGo.activeSelf, "the panel must stay shown");
                CollectionAssert.AreEqual(new[] { 0f }, TimeScaleWrites, "the running state must stay frozen, frozen only once");
            }
            finally { rig.Dispose(); }
        }

        [Test]
        public void LateUpdate_AppliesThePendingResume()
        {
            PauseTestRig rig = PauseTestRig.Build(realInput: true);
            try
            {
                rig.Pause.Pause();
                rig.Pause.Resume();
                PauseTestRig.Invoke(rig.Pause, "LateUpdate");
                Assert.IsFalse(rig.Pause.IsPaused, "LateUpdate must apply the pending resume");
                CollectionAssert.AreEqual(new[] { 0f, 1f }, TimeScaleWrites);
            }
            finally { rig.Dispose(); }
        }

        // ---------- teardown restore (developer ruling 3) ----------

        [Test]
        public void OnDestroy_WhilePausedByThisInstance_RestoresRunningState()
        {
            PauseTestRig rig = PauseTestRig.Build(realInput: true);
            try
            {
                rig.Pause.Pause();
                PauseTestRig.Invoke(rig.Pause, "OnDestroy");
                CollectionAssert.AreEqual(new[] { 0f, 1f }, TimeScaleWrites, "a LevelPause destroyed while it holds the pause must restore the running state");
            }
            finally { rig.Dispose(); }
        }

        [Test]
        public void OnDestroy_WhenThisInstanceNeverPausedOrAlreadyResumed_WritesNothing()
        {
            PauseTestRig never = PauseTestRig.Build(realInput: true);
            try
            {
                PauseTestRig.Invoke(never.Pause, "OnDestroy");
                CollectionAssert.IsEmpty(TimeScaleWrites, "a LevelPause that never paused must not touch the running state");
            }
            finally { never.Dispose(); }

            PauseTestRig resumed = PauseTestRig.Build(realInput: true);
            try
            {
                resumed.Pause.Pause();
                resumed.Pause.Resume();
                resumed.Pause.ApplyPendingResume();
                PauseTestRig.Invoke(resumed.Pause, "OnDestroy");
                CollectionAssert.AreEqual(new[] { 0f, 1f }, TimeScaleWrites, "an already-resumed LevelPause must not restore again");
            }
            finally { resumed.Dispose(); }
        }

        // ---------- selection (developer ruling on Phase 1 point 4b) ----------

        [Test]
        public void Selection_IsClearedOnPause_AndOnAppliedResume()
        {
            PauseTestRig rig = PauseTestRig.Build(realInput: true);
            var eventSystemGo = new GameObject("EventSystem");
            var selectedGo = new GameObject("PreviouslySelected");
            try
            {
                Type eventSystemType = Type.GetType("UnityEngine.EventSystems.EventSystem, UnityEngine.UI");
                Assert.NotNull(eventSystemType, "UnityEngine.EventSystems.EventSystem not found");
                Component eventSystem = eventSystemGo.AddComponent(eventSystemType);
                PauseTestRig.SetPrivate(rig.Pause, "eventSystem", eventSystem);
                MethodInfo select = eventSystemType.GetMethod("SetSelectedGameObject", new[] { typeof(GameObject) });
                PropertyInfo selected = eventSystemType.GetProperty("currentSelectedGameObject");

                select.Invoke(eventSystem, new object[] { selectedGo });
                Assert.AreEqual(selectedGo, selected.GetValue(eventSystem), "rig sanity: something is selected");
                rig.Pause.Pause();
                Assert.IsNull(selected.GetValue(eventSystem), "Pause must clear the EventSystem selection");

                select.Invoke(eventSystem, new object[] { selectedGo });
                rig.Pause.Resume();
                rig.Pause.ApplyPendingResume();
                Assert.IsNull(selected.GetValue(eventSystem), "the applied resume must clear the EventSystem selection");
            }
            finally
            {
                Object.DestroyImmediate(selectedGo);
                Object.DestroyImmediate(eventSystemGo);
                rig.Dispose();
            }
        }

        // ---------- buttons ----------

        [Test]
        public void PauseButton_Pauses_AndPanelResume_RequestsResume()
        {
            PauseTestRig rig = PauseTestRig.Build(realInput: true);
            try
            {
                rig.PauseButton.Press();
                Assert.IsTrue(rig.Pause.IsPaused, "the pause button must pause the level");

                rig.Panel.Resume();
                Assert.IsTrue(rig.Pause.IsResumePending, "the panel's Resume must request a resume");
                Assert.IsTrue(rig.Pause.IsPaused, "... which only takes effect at the end of the frame");
            }
            finally { rig.Dispose(); }
        }

        // ---------- §5.7 auto-pause ----------

        [Test]
        public void AutoPause_PlayerRule_FocusLossAndBackgroundPause_ComingBackNeverResumes()
        {
            PauseTestRig rig = PauseTestRig.Build(realInput: true);
            try
            {
                rig.Pause.IgnoreFocusLoss = false; // player build
                rig.Pause.HandleApplicationFocus(false);
                Assert.IsTrue(rig.Pause.IsPaused, "player: focus loss must pause");
                Assert.IsTrue(rig.PanelGo.activeSelf, "auto-pause must show the panel");

                rig.Pause.HandleApplicationPause(true);
                rig.Pause.HandleApplicationFocus(true);
                rig.Pause.HandleApplicationPause(false);
                rig.Pause.ApplyPendingResume();
                Assert.IsTrue(rig.Pause.IsPaused, "coming back must never resume by itself");
                CollectionAssert.AreEqual(new[] { 0f }, TimeScaleWrites);
            }
            finally { rig.Dispose(); }

            PauseTestRig background = PauseTestRig.Build(realInput: true);
            try
            {
                background.Pause.IgnoreFocusLoss = false;
                background.Pause.HandleApplicationPause(true);
                Assert.IsTrue(background.Pause.IsPaused, "player: going to the background must pause");
            }
            finally { background.Dispose(); }
        }

        [Test]
        public void AutoPause_EditorRule_FocusLossIgnored_BackgroundStillPauses()
        {
            PauseTestRig rig = PauseTestRig.Build(realInput: true);
            try
            {
                rig.Pause.IgnoreFocusLoss = true; // Editor
                rig.Pause.HandleApplicationFocus(false);
                Assert.IsFalse(rig.Pause.IsPaused, "Editor: focus loss must not pause");

                rig.Pause.HandleApplicationPause(true);
                Assert.IsTrue(rig.Pause.IsPaused, "Editor: background must still pause");
                rig.Pause.HandleApplicationPause(false);
                rig.Pause.HandleApplicationFocus(true);
                rig.Pause.ApplyPendingResume();
                Assert.IsTrue(rig.Pause.IsPaused, "coming back must never resume by itself");
            }
            finally { rig.Dispose(); }
        }

        [Test]
        public void UnityLifecycleMessages_ForwardToTheHandlers()
        {
            PauseTestRig focus = PauseTestRig.Build(realInput: true);
            try
            {
                focus.Pause.IgnoreFocusLoss = false;
                PauseTestRig.Invoke(focus.Pause, "OnApplicationFocus", false);
                Assert.IsTrue(focus.Pause.IsPaused, "OnApplicationFocus(false) must reach HandleApplicationFocus");
            }
            finally { focus.Dispose(); }

            PauseTestRig pause = PauseTestRig.Build(realInput: true);
            try
            {
                PauseTestRig.Invoke(pause.Pause, "OnApplicationPause", true);
                Assert.IsTrue(pause.Pause.IsPaused, "OnApplicationPause(true) must reach HandleApplicationPause");
            }
            finally { pause.Dispose(); }
        }

        // ---------- §5.6 complete screen ----------

        [Test]
        public void LevelComplete_HidesThePauseButton_AndPauseIsRefusedAfterwards()
        {
            PauseTestRig rig = PauseTestRig.Build(realInput: false);
            var screenGo = new GameObject("LevelCompleteScreen");
            try
            {
                var screen = screenGo.AddComponent<LevelCompleteScreen>();
                PauseTestRig.SetPrivate(screen, "rooms", rig.Rooms);
                PauseTestRig.SetPrivate(screen, "roomDeath", rig.Death);
                PauseTestRig.SetPrivate(screen, "checkpoints", rig.Checkpoints);
                PauseTestRig.SetPrivate(screen, "observers", rig.Observers);
                PauseTestRig.SetPrivate(screen, "pauseButton", rig.PauseButtonGo);
                PauseTestRig.Invoke(screen, "OnEnable");
                PauseTestRig.Invoke(screen, "Start");
                Assert.IsTrue(rig.PauseButtonGo.activeSelf, "rig sanity: the pause button starts shown");

                rig.CompleteLevel();
                Assert.IsTrue(rig.Rooms.LevelComplete, "rig sanity: the level completed");
                Assert.IsFalse(rig.PauseButtonGo.activeSelf, "the complete screen must hide the pause button");

                Assert.IsFalse(rig.Pause.Pause(), "Pause must be refused once the level is complete");
                rig.Pause.IgnoreFocusLoss = false;
                rig.Pause.HandleApplicationPause(true);
                rig.Pause.HandleApplicationFocus(false);
                Assert.IsFalse(rig.Pause.IsPaused, "auto-pause must be refused once the level is complete");
                Assert.IsFalse(rig.PanelGo.activeSelf, "the pause panel must never show over the complete screen");
                CollectionAssert.IsEmpty(TimeScaleWrites, "a refused pause must not touch the running state");
            }
            finally
            {
                Object.DestroyImmediate(screenGo);
                rig.Dispose();
            }
        }
    }
}
