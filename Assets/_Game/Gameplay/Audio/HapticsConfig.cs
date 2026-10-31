using System;
using Parallax.Core;
using UnityEngine;

namespace Parallax.Gameplay.Audio
{
    /// <summary>PAX-065 (D-278): how each haptic cue feels, and how often traps may ask for one. One asset at AssetPath, made by
    /// PARALLAX/Setup/Haptics (PAX-065).</summary>
    [CreateAssetMenu(menuName = "PARALLAX/Haptics Config")]
    public sealed class HapticsConfig : ScriptableObject
    {
        public const string AssetPath = "Assets/_Game/Data/HapticsConfig.asset";

        /// <summary>One cue's pattern: step i runs Millis[i] at Amplitudes[i] (1 to 255; 0 is a rest). A phone without amplitude
        /// control plays Strong cues as one plain buzz and skips the rest.</summary>
        [Serializable]
        public struct Pattern
        {
            public HapticCue Cue;
            public int[] Millis;
            public int[] Amplitudes;
            public bool Strong;
        }

        [Tooltip("Seconds after any cue before a trap may buzz again.")]
        [SerializeField] float minGap = 0.25f;
        [Tooltip("A trap further than this from the cat (world units) doesn't buzz. Sound uses the same 9 u for full volume.")]
        [SerializeField] float trapWithin = 9f;
        [SerializeField] Pattern[] patterns =
        {
            new Pattern { Cue = HapticCue.TrapFire, Millis = new[] { 25 }, Amplitudes = new[] { 70 } },
            new Pattern { Cue = HapticCue.Death, Millis = new[] { 60 }, Amplitudes = new[] { 180 }, Strong = true },
            new Pattern { Cue = HapticCue.LevelComplete, Millis = new[] { 40, 60, 40, 60, 80 }, Amplitudes = new[] { 140, 0, 170, 0, 220 }, Strong = true },
        };

        public float MinGap => minGap;
        public float TrapWithin => trapWithin;

        public bool TryGetPattern(HapticCue cue, out Pattern found)
        {
            foreach (Pattern p in patterns)
                if (p.Cue == cue) { found = p; return true; }
            found = default;
            return false;
        }
    }
}
