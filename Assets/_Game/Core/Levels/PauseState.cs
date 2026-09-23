namespace Parallax.Core
{
    /// <summary>PAX-054 (D-073): the level's pause state. Pause takes effect immediately. Resume
    /// is only a request: the level stays paused until ApplyPendingResume runs, which LevelPause
    /// does from LateUpdate, after every Update in the frame has read its input. All calls are
    /// idempotent. A Pause after a Resume in the same frame cancels that Resume (the latest
    /// request wins), and a Resume while not paused leaves nothing pending.</summary>
    public sealed class PauseState
    {
        public bool IsPaused { get; private set; }
        public bool ResumePending { get; private set; }

        public void Pause()
        {
            ResumePending = false;
            IsPaused = true;
        }

        public void Resume()
        {
            if (IsPaused) ResumePending = true;
        }

        /// <summary>Returns true only when a pending resume actually unpaused the state.</summary>
        public bool ApplyPendingResume()
        {
            if (!ResumePending) return false;
            ResumePending = false;
            IsPaused = false;
            return true;
        }
    }
}
