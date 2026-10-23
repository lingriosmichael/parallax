namespace Parallax.Core
{
    /// <summary>D-165 (PAX-110): the cat's voice sounds, rendered by Tools/Audio/cat_voice.py into Assets/_Game/Audio/CatVoice
    /// as {Role}_{NN}.wav. Append new roles at the end: the cue bank stores them by value.</summary>
    public enum CatVoiceRole { Mew, MewLow, Mrrow, Chirp, Mrp, Mrf, Trill, Purr, Death }

    /// <summary>D-165: one voice sound started with a cue, Delay seconds after it, at Volume (its own mastered level is 1).</summary>
    public readonly struct VoiceLayer
    {
        public readonly CatVoiceRole Role;
        public readonly float Delay;
        public readonly float Volume;

        public VoiceLayer(CatVoiceRole role, float delay, float volume)
        {
            Role = role;
            Delay = delay;
            Volume = volume;
        }
    }

    /// <summary>D-165 (the developer, picking reel S: "Lets go with Audio S"): which of the cat's voice sounds each cue
    /// carries. The synthesized cue (SfxPresets) is the accent: what hit the cat, the chime, the push; the voice is the cat.</summary>
    public static class CatVoice
    {
        /// <summary>One jump in this many is voiced, alternating a chirp and a "mrp" (every jump would be too much).</summary>
        public const int JumpEvery = 4;
        /// <summary>The ticks the cat stands still before it meows to itself, and between meows while it stays.</summary>
        public const int IdleAfterTicks = 300, IdleRepeatTicks = 900;

        static readonly VoiceLayer[] None = new VoiceLayer[0];

        /// <summary>The voice for the cue's `playIndex`-th start (0 first); only jumps depend on it, as a fixed pattern.</summary>
        public static VoiceLayer[] For(AudioCue cue, int playIndex) => cue switch
        {
            AudioCue.Jump => playIndex % JumpEvery == JumpEvery - 1
                ? new[] { new VoiceLayer((playIndex / JumpEvery) % 2 == 0 ? CatVoiceRole.Chirp : CatVoiceRole.Mrp, 0.01f, 0.7f) }
                : None,
            AudioCue.WallJump => new[] { new VoiceLayer(CatVoiceRole.Mrp, 0.01f, 0.8f) },
            AudioCue.HardLand => new[] { new VoiceLayer(CatVoiceRole.Mrf, 0.02f, 0.9f) },
            AudioCue.Respawn => new[] { new VoiceLayer(CatVoiceRole.Mew, 0.03f, 1f) },
            AudioCue.LevelComplete => new[] { new VoiceLayer(CatVoiceRole.Mrrow, 0.42f, 1f), new VoiceLayer(CatVoiceRole.Purr, 0.6f, 0.8f) },
            // A fall: the whoosh first, then the cat lands.
            AudioCue.DeathPit => new[] { new VoiceLayer(CatVoiceRole.Death, 0.34f, 1f) },
            >= AudioCue.DeathDefault and <= AudioCue.DeathArrow => new[] { new VoiceLayer(CatVoiceRole.Death, 0f, 1f) },
            AudioCue.TrapFlip => new[] { new VoiceLayer(CatVoiceRole.Chirp, 0f, 0.9f) },
            AudioCue.TrapInverter => new[] { new VoiceLayer(CatVoiceRole.Trill, 0.02f, 0.9f) },
            AudioCue.Idle => new[] { new VoiceLayer(CatVoiceRole.MewLow, 0f, 1f) },
            _ => None,
        };

        /// <summary>A cue that is only the cat's voice, with nothing synthesized under it.</summary>
        public static bool IsVoiceOnly(AudioCue cue) => cue == AudioCue.Idle;

        /// <summary>Whether the cat, still for `stillTicks` ticks (1 on the first still tick), meows to itself on this tick.</summary>
        public static bool IdleDue(int stillTicks) =>
            stillTicks >= IdleAfterTicks && (stillTicks - IdleAfterTicks) % IdleRepeatTicks == 0;
    }
}
