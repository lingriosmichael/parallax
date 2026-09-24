using UnityEngine;

namespace Parallax.Editor.Setup
{
    // PAX-076 (D-083): the validator's thresholds inside a precision section (D-069). Provisional (reach 0.85,
    // slack 8 ticks); PAX-069 replaces them with device values. Editor-only: only LevelLayoutValidator and the
    // route validator read it, nothing at runtime (R6). The asset is made by PARALLAX/Setup/Precision Thresholds.
    public sealed class PrecisionThresholds : ScriptableObject
    {
        [Tooltip("A precision jump's distance stays within this fraction of the measured reach (D-056's 0.75 outside sections). 0.75 <= value < 1.")]
        [SerializeField] float reachFraction = .85f;

        [Tooltip("Timing slack in ticks inside a section: a periodic trap's or arrow's margin, and a timed route step's window (D-056's 12 outside sections). 0 < value <= 12.")]
        [SerializeField] int slackTicks = 8;

        public float ReachFraction => reachFraction;
        public int SlackTicks => slackTicks;
    }
}
