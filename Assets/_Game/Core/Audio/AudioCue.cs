namespace Parallax.Core
{
    /// <summary>PAX-064 (D-158): every sound the game plays. Serialized by value in AudioCueBank's overrides: append only.</summary>
    public enum AudioCue
    {
        Jump,
        WallJump,
        Land,
        HardLand,
        Respawn,
        LevelComplete,
        UiTap,
        DeathDefault,
        DeathPit,
        DeathSpiked,
        DeathCrushed,
        DeathZapped,
        DeathArrow,
        TrapSpikes,
        TrapArrow,
        TrapCollapse,
        TrapFallingBlock,
        TrapMover,
        TrapFlip,
        TrapInverter,
        TrapGeyser,
        TrapStorm,
        TrapShrink,
        TrapDoor,
        TrapVine,
        TrapOther,
        TrapStormStrike,
        // D-165: the cat meowing to itself while it stands still (voice only, CatVoice).
        Idle,
    }

    /// <summary>PAX-064: the mixer group a cue plays through; each has its own volume.</summary>
    public enum AudioChannel { Music, Sfx, Ui }

    /// <summary>PAX-064: how a cue plays: its channel and volume, how many copies may sound at once, and the shortest gap
    /// between two starts (a burst of the same event plays once).</summary>
    public readonly struct CueSpec
    {
        public readonly AudioChannel Channel;
        public readonly float Volume;
        public readonly int MaxVoices;
        public readonly float MinGap;

        public CueSpec(AudioChannel channel, float volume, int maxVoices, float minGap)
        {
            Channel = channel;
            Volume = volume;
            MaxVoices = maxVoices;
            MinGap = minGap;
        }
    }

    public static class AudioCues
    {
        /// <summary>The death cue for a held death's kind (PAX-V07's CatDeathKind).</summary>
        public static AudioCue ForDeath(CatDeathKind kind) => kind switch
        {
            CatDeathKind.Pit => AudioCue.DeathPit,
            CatDeathKind.Spiked => AudioCue.DeathSpiked,
            CatDeathKind.Crushed => AudioCue.DeathCrushed,
            CatDeathKind.Zapped => AudioCue.DeathZapped,
            CatDeathKind.Arrow => AudioCue.DeathArrow,
            _ => AudioCue.DeathDefault,
        };

        /// <summary>A volume setting from 0 to 1 in decibels: 20·log10(v), and 0 (or less) is -80 dB.</summary>
        public static float ToDecibels(float volume) => volume <= 0.0001f ? -80f : 20f * (float)System.Math.Log10(volume);

        /// <summary>A world sound's level by its distance from the camera: 1 within `full`, falling linearly to 0 at `silent`.</summary>
        public static float Falloff(float distance, float full, float silent)
        {
            if (distance <= full) return 1f;
            if (silent <= full || distance >= silent) return 0f;
            return 1f - (distance - full) / (silent - full);
        }

        /// <summary>A small, repeating pitch walk so the same cue played twice in a row doesn't sound mechanical. A fixed
        /// pattern, not randomness: presentation only, and the same on every run.</summary>
        public static float PitchFor(int playIndex, float semitones)
        {
            float step = Pattern[((playIndex % Pattern.Length) + Pattern.Length) % Pattern.Length] * semitones;
            return (float)System.Math.Pow(2.0, step / 12.0);
        }

        static readonly float[] Pattern = { 0f, 0.6f, -0.5f, 0.25f, -0.8f, 0.9f, -0.2f };
    }
}
