using System;
using Parallax.Core;
using UnityEngine;

namespace Parallax.Gameplay.Audio
{
    /// <summary>PAX-064 (D-158): the game's mix. The sounds themselves are synthesized from SfxPresets; this asset holds the
    /// channel volumes and, per cue, an optional recording that replaces the synthesized sound, a volume trim, or a mute.
    /// One asset at AssetPath, made by PARALLAX/Setup/Audio (PAX-064). D-165: it also holds the cat's voice (Tools/Audio/cat_voice.py's
    /// sounds), filled by PARALLAX/Setup/Cat Voice (PAX-110).</summary>
    [CreateAssetMenu(menuName = "PARALLAX/Audio Cue Bank")]
    public sealed class AudioCueBank : ScriptableObject
    {
        public const string AssetPath = "Assets/_Game/Data/AudioCueBank.asset";

        [Serializable]
        public struct Override
        {
            public AudioCue Cue;
            [Tooltip("A recording to play instead of the synthesized sound (optional).")]
            public AudioClip Clip;
            [Tooltip("A trim on the cue's own volume; 0 leaves it as designed.")]
            [Range(0f, 2f)] public float Volume;
            public bool Mute;
        }

        /// <summary>D-165: one of the cat's voice sounds and its variants, played in turn.</summary>
        [Serializable]
        public struct VoiceClips
        {
            public CatVoiceRole Role;
            public AudioClip[] Variants;
        }

        [Header("Channel volumes (0 to 1)")]
        [SerializeField, Range(0f, 1f)] float music = 0.5f;
        [SerializeField, Range(0f, 1f)] float sfx = 0.85f;
        [SerializeField, Range(0f, 1f)] float ui = 0.7f;

        [Header("Ambient bed")]
        [Tooltip("A recording to loop instead of the synthesized bed (optional).")]
        [SerializeField] AudioClip bedClip;
        [Tooltip("The bed's level while the game is paused, as a fraction of its normal level.")]
        [SerializeField, Range(0f, 1f)] float pausedBed = 0.4f;
        [Tooltip("Seconds the bed takes to fade in when a scene starts.")]
        [SerializeField] float bedFadeIn = 1.2f;

        [Header("Per cue (optional)")]
        [SerializeField] Override[] overrides = new Override[0];

        [Header("Cat voice (D-165)")]
        [Tooltip("Written by PARALLAX/Setup/Cat Voice (PAX-110) from Assets/_Game/Audio/CatVoice.")]
        [SerializeField] VoiceClips[] catVoice = new VoiceClips[0];

        public AudioClip BedClip => bedClip;
        public float PausedBed => pausedBed;
        public float BedFadeIn => bedFadeIn;

        public float VolumeOf(AudioChannel channel) => channel switch
        {
            AudioChannel.Music => music,
            AudioChannel.Ui => ui,
            _ => sfx,
        };

        public bool TryGetOverride(AudioCue cue, out Override found)
        {
            foreach (Override o in overrides)
                if (o.Cue == cue) { found = o; return true; }
            found = default;
            return false;
        }

        /// <summary>The role's `turn`-th variant (they repeat in order), or null when the role has none.</summary>
        public AudioClip VoiceClip(CatVoiceRole role, int turn)
        {
            foreach (VoiceClips v in catVoice)
                if (v.Role == role && v.Variants != null && v.Variants.Length > 0)
                    return v.Variants[((turn % v.Variants.Length) + v.Variants.Length) % v.Variants.Length];
            return null;
        }

        /// <summary>How many variants the role has (0 before the Cat Voice menu has run).</summary>
        public int VoiceVariants(CatVoiceRole role)
        {
            foreach (VoiceClips v in catVoice)
                if (v.Role == role) return v.Variants != null ? v.Variants.Length : 0;
            return 0;
        }
    }
}
