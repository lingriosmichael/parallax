using Parallax.Core;
using Parallax.Gameplay.Input;
using Parallax.Gameplay.Rooms;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Parallax.Gameplay.Levels
{
    /// <summary>PAX-054 (D-073): the one pause state per level scene. Pausing freezes the running
    /// state (time scale 0, through RunningState) and shows the full-screen pause panel; the
    /// ObserverSet reads IsPaused as its pause gate, so no level tick runs while paused even if
    /// FixedUpdate is called. Resume is deferred: it only requests, and LateUpdate applies it at
    /// the end of the frame, after every Update has read that frame's input. Applying it hides
    /// the panel, restores the running state and drops every input latch (a tap on Resume, or a
    /// key pressed in that frame, can't become a jump). Auto-pauses when the app goes to the
    /// background, and on focus loss unless IgnoreFocusLoss (set in the Editor). Never pauses
    /// once the level is complete.</summary>
    public sealed class LevelPause : MonoBehaviour, IPauseGate
    {
        [SerializeField] RoomManager rooms;
        [SerializeField] CatInputRouter router;
        [SerializeField] GameObject panel;
        [SerializeField] EventSystem eventSystem;

        readonly PauseState state = new PauseState();
        bool heldRunningState;

        public bool IsPaused => state.IsPaused;
        public bool IsResumePending => state.ResumePending;

        /// <summary>True in the Editor, where clicking any other window loses focus.</summary>
        public bool IgnoreFocusLoss { get; set; }

        void Awake() => IgnoreFocusLoss = Application.isEditor;

        /// <summary>Returns false (and changes nothing) once the level is complete.</summary>
        public bool Pause()
        {
            if (rooms != null && rooms.LevelComplete) return false;

            bool wasPaused = state.IsPaused;
            state.Pause();
            if (!wasPaused)
            {
                RunningState.Freeze();
                heldRunningState = true;
            }
            if (panel != null) panel.SetActive(true);
            ClearSelection();
            return true;
        }

        public void Resume() => state.Resume();

        /// <summary>Called from LateUpdate; public so tests can apply the end of a frame directly.</summary>
        public void ApplyPendingResume()
        {
            if (!state.ApplyPendingResume()) return;

            if (panel != null) panel.SetActive(false);
            RunningState.Restore();
            heldRunningState = false;
            if (router != null) router.ResetTransientState();
            ClearSelection();
        }

        public void HandleApplicationPause(bool paused)
        {
            if (paused && AutoPausePolicy.ShouldPause(AppLifecycleEvent.Background, IgnoreFocusLoss)) Pause();
        }

        public void HandleApplicationFocus(bool focused)
        {
            if (!focused && AutoPausePolicy.ShouldPause(AppLifecycleEvent.FocusLost, IgnoreFocusLoss)) Pause();
        }

        void LateUpdate() => ApplyPendingResume();

        void OnApplicationPause(bool paused) => HandleApplicationPause(paused);

        void OnApplicationFocus(bool focused) => HandleApplicationFocus(focused);

        // Safety net: a paused level torn down without going through LevelSceneLoader (e.g. leaving
        // Play mode) must not leave time frozen. Only the instance that froze it restores it.
        void OnDestroy()
        {
            if (!heldRunningState) return;
            heldRunningState = false;
            RunningState.Restore();
        }

        // Pause UI buttons use Navigation None, so a click never selects them; clearing here also
        // covers anything else selected, so keyboard/gamepad Submit and Navigate reach nothing.
        void ClearSelection()
        {
            if (eventSystem != null) eventSystem.SetSelectedGameObject(null);
        }
    }
}
