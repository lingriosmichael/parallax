using Parallax.Core;
using UnityEngine;

namespace Parallax.Gameplay.Audio
{
    /// <summary>PAX-065 (D-278): where the game has no vibrator. In the Editor it logs each cue, so a level's haptics can be seen; in
    /// any other player it does nothing.</summary>
    public sealed class EditorHaptics : IHaptics
    {
        public void Play(HapticCue cue)
        {
            if (Application.isEditor) Debug.Log("Haptics: " + cue);
        }
    }
}
