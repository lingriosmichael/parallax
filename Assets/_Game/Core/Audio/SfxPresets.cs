using System;

namespace Parallax.Core
{
    /// <summary>PAX-064 (D-158): the game's sounds, designed in code. Soft and organic to sit with the painted art: plucked
    /// strings, filtered noise and low thumps rather than chip-tune beeps. A recording in AudioCueBank replaces any of them.
    /// D-165: the cat's voice is no longer here; CatVoice adds it over these (the accent: what hit the cat, the chime, the push).</summary>
    public static class SfxPresets
    {
        public static CueSpec Spec(AudioCue cue) => cue switch
        {
            AudioCue.Jump or AudioCue.WallJump => new CueSpec(AudioChannel.Sfx, 0.35f, 2, 0.05f),
            AudioCue.Land => new CueSpec(AudioChannel.Sfx, 0.4f, 2, 0.08f),
            AudioCue.HardLand => new CueSpec(AudioChannel.Sfx, 0.6f, 2, 0.08f),
            AudioCue.Respawn => new CueSpec(AudioChannel.Sfx, 0.55f, 1, 0.2f),
            AudioCue.LevelComplete => new CueSpec(AudioChannel.Sfx, 0.8f, 1, 0.5f),
            AudioCue.UiTap => new CueSpec(AudioChannel.Ui, 0.6f, 2, 0.04f),
            >= AudioCue.DeathDefault and <= AudioCue.DeathArrow => new CueSpec(AudioChannel.Sfx, 0.85f, 1, 0.1f),
            // The developer (2026-10-20): "When too many arrows launching at a time is too bleh". One soft shot at a time.
            AudioCue.TrapArrow => new CueSpec(AudioChannel.Sfx, 0.22f, 1, 0.22f),
            AudioCue.TrapStorm => new CueSpec(AudioChannel.Sfx, 0.45f, 1, 0.5f),
            AudioCue.TrapStormStrike => new CueSpec(AudioChannel.Sfx, 0.75f, 2, 0.4f),
            AudioCue.TrapCollapse or AudioCue.TrapFallingBlock => new CueSpec(AudioChannel.Sfx, 0.6f, 3, 0.06f),
            AudioCue.TrapFlip or AudioCue.TrapInverter => new CueSpec(AudioChannel.Sfx, 0.6f, 2, 0.1f),
            AudioCue.Idle => new CueSpec(AudioChannel.Sfx, 0.5f, 1, 2f),
            // "I prefer more cat sounds than environment sounds": the rest of the room sits under the cat.
            _ => new CueSpec(AudioChannel.Sfx, 0.45f, 3, 0.04f),
        };

        public static SfxRecipe For(AudioCue cue) => cue switch
        {
            AudioCue.Jump => R(0.8f, Tone(SfxWave.Triangle, 0f, 0.11f, 0.06f, 260f, 520f, 1f, lp: 2400f), Hiss(0f, 0.06f, 0.03f, 0.25f, 1800f, 5000f)),
            // A wall jump: the push off the stone (the cat's "mrp" is CatVoice's).
            AudioCue.WallJump => R(0.6f, Hiss(0f, 0.09f, 0.04f, 0.45f, 900f, 3000f)),
            AudioCue.Land => R(0.8f, Thump(0f, 0.1f, 140f, 70f, 1f), Hiss(0f, 0.09f, 0.03f, 0.6f, 900f, 250f)),
            // A hard landing: the thud (and CatVoice's "mrf").
            AudioCue.HardLand => R(0.9f, Thump(0f, 0.18f, 120f, 50f, 1f), Hiss(0f, 0.12f, 0.05f, 0.5f, 1400f, 200f)),
            // Back at the checkpoint: a soft pluck under CatVoice's mew.
            AudioCue.Respawn => R(0.45f, Pluck(0f, 0.45f, 523.25f, 0.3f, 0.35f, 3500f)),
            // A level done: the chime (CatVoice's "mrrow" and purr come in on its last note).
            AudioCue.LevelComplete => R(0.85f,
                Pluck(0f, 0.9f, 392f, 0.6f, 0.7f, 4000f), Pluck(0.11f, 0.9f, 493.88f, 0.6f, 0.7f, 4000f),
                Pluck(0.22f, 0.9f, 587.33f, 0.6f, 0.75f, 4000f), Pluck(0.36f, 1.4f, 783.99f, 0.9f, 0.85f, 4500f),
                Tone(SfxWave.Sine, 0.36f, 1.4f, 0.5f, 196f, 196f, 0.3f, attack: 0.05f)),
            AudioCue.UiTap => R(0.6f, Pluck(0f, 0.18f, 880f, 0.09f, 1f, 5000f), Hiss(0f, 0.012f, 0.005f, 0.3f, 6000f, 6000f, hp: 2500f)),

            // Deaths (D-165): CatVoice's thud with a muffled yelp is the death; these are what hit the cat, under it (Vision §9:
            // light, never punishing).
            AudioCue.DeathDefault => R(0.35f, Poof(0.03f, 0.3f, 1f)),
            // A fall: the rush of air on the way down (the thud lands at its end).
            AudioCue.DeathPit => R(0.45f, Hiss(0f, 0.42f, 0.25f, 0.7f, 2600f, 500f, attack: 0.06f, hp: 200f), Poof(0.36f, 0.22f, 0.5f)),
            // Spikes: a glint of steel.
            AudioCue.DeathSpiked => R(0.45f, Hiss(0f, 0.04f, 0.015f, 0.35f, 7000f, 3000f, hp: 1500f),
                Tone(SfxWave.Sine, 0.005f, 0.3f, 0.1f, 2350f, 2300f, 0.2f)),
            // Crushed: the slam of stone.
            AudioCue.DeathCrushed => R(0.6f, Thump(0f, 0.35f, 95f, 38f, 1f), Hiss(0f, 0.3f, 0.09f, 0.6f, 1200f, 150f)),
            // Zapped: a crackle and a buzz.
            AudioCue.DeathZapped => R(0.5f, Hiss(0f, 0.25f, 0.1f, 0.45f, 8000f, 1500f, hp: 800f),
                Tone(SfxWave.Saw, 0f, 0.3f, 0.12f, 120f, 110f, 0.3f, lp: 3000f, vibHz: 31f, vibSemis: 1.6f)),
            // An arrow: the thunk.
            AudioCue.DeathArrow => R(0.5f, Pluck(0f, 0.18f, 130f, 0.08f, 0.6f, 1800f), Hiss(0f, 0.03f, 0.012f, 0.3f, 5000f, 2000f, hp: 1200f)),
            // D-165: the cat meowing to itself is all voice (CatVoice.IsVoiceOnly).
            AudioCue.Idle => R(0f),

            // Hidden spikes: a bright metallic shing as they spring.
            AudioCue.TrapSpikes => R(0.8f, Hiss(0f, 0.08f, 0.03f, 0.6f, 9000f, 4000f, hp: 2500f),
                Tone(SfxWave.Saw, 0f, 0.1f, 0.04f, 950f, 1500f, 0.35f, lp: 4000f), Tone(SfxWave.Sine, 0.01f, 0.35f, 0.12f, 2350f, 2300f, 0.25f)),
            // An arrow: a soft "fft" as it leaves (the tell is the launcher's glow), no click, no ring.
            AudioCue.TrapArrow => R(0.6f, Pluck(0f, 0.07f, 190f, 0.03f, 0.25f, 1600f),
                Hiss(0.02f, 0.16f, 0.05f, 1f, 2600f, 900f, attack: 0.025f, hp: 600f)),
            // A floor giving way: a crack, a crash of stone with a low boom, and debris pattering after it.
            AudioCue.TrapCollapse => R(0.9f, Crash(0f, 1f)),
            // A falling block (bridge tiles, dropping roofs): the same crash, a little heavier, with a rush of air.
            AudioCue.TrapFallingBlock => R(0.9f, Crash(0f, 1.15f), Hiss(0.05f, 0.4f, 0f, 0.35f, 300f, 1300f, attack: 0.1f)),
            // A mover: stone grinding into motion.
            AudioCue.TrapMover => R(0.8f, Tone(SfxWave.Saw, 0f, 0.5f, 0.3f, 55f, 48f, 0.6f, lp: 380f, vibHz: 11f, vibSemis: 0.6f),
                Hiss(0f, 0.5f, 0.3f, 0.8f, 700f, 500f, attack: 0.04f, hp: 120f)),
            // Gravity flip: a rising airy sweep (under CatVoice's chirp).
            AudioCue.TrapFlip => R(0.6f, Hiss(0f, 0.36f, 0.16f, 0.35f, 500f, 4000f, attack: 0.05f)),
            // Inverter: a dizzy wobble (under CatVoice's questioning trill).
            AudioCue.TrapInverter => R(0.6f, Tone(SfxWave.Triangle, 0f, 0.5f, 0.25f, 420f, 380f, 0.25f, attack: 0.02f, vibHz: 7f, vibSemis: 2.2f)),
            // Geyser: a burst of steam over a low rumble.
            AudioCue.TrapGeyser => R(0.8f, Hiss(0f, 0.6f, 0.3f, 1f, 6000f, 2500f, attack: 0.03f, hp: 900f), Thump(0f, 0.3f, 70f, 45f, 0.5f)),
            // Storm cloud waking: far thunder rolling in.
            AudioCue.TrapStorm => R(0.8f, Rumble(0f, 1.2f, 1f, attack: 0.3f), Rumble(0.35f, 0.8f, 0.7f, attack: 0.2f)),
            // A strike: the lightning's crack and the thunder breaking over it, then rolling away.
            AudioCue.TrapStormStrike => R(0.95f,
                Hiss(0f, 0.05f, 0.012f, 1f, 12000f, 8000f, hp: 1200f),
                Hiss(0.01f, 0.35f, 0.08f, 0.9f, 6000f, 1200f, hp: 150f),
                Thump(0.01f, 0.7f, 70f, 34f, 0.3f),
                Rumble(0.05f, 1.6f, 1f), Rumble(0.3f, 1.3f, 0.8f, attack: 0.2f), Rumble(0.7f, 1f, 0.6f, attack: 0.25f)),
            // A shrinking floor: a short grinding slide.
            AudioCue.TrapShrink => R(0.75f, Hiss(0f, 0.3f, 0.2f, 0.9f, 1600f, 700f, hp: 200f), Tone(SfxWave.Saw, 0f, 0.3f, 0.2f, 90f, 60f, 0.4f, lp: 500f)),
            // The door backing away: a stone scrape.
            AudioCue.TrapDoor => R(0.75f, Hiss(0f, 0.4f, 0.25f, 0.9f, 1300f, 600f, attack: 0.03f, hp: 150f), Tone(SfxWave.Saw, 0f, 0.4f, 0.25f, 80f, 55f, 0.4f, lp: 420f)),
            // A vine snapping: a twig crack and a rustle of leaves.
            AudioCue.TrapVine => R(0.8f, Hiss(0f, 0.02f, 0.008f, 1f, 7000f, 7000f, hp: 2500f), Pluck(0f, 0.12f, 240f, 0.05f, 0.6f, 2000f),
                Hiss(0.02f, 0.3f, 0.12f, 0.5f, 4000f, 2000f, hp: 1500f)),
            _ => R(0.7f, Pluck(0f, 0.2f, 330f, 0.12f, 1f, 2500f), Hiss(0f, 0.05f, 0.02f, 0.4f, 3000f, 1500f)),
        };

        public static BedRecipe Bed() => new BedRecipe();

        static SfxRecipe R(float peak, params object[] parts)
        {
            var layers = new System.Collections.Generic.List<SfxLayer>();
            foreach (object p in parts)
                if (p is SfxLayer l) layers.Add(l);
                else if (p is SfxLayer[] many) layers.AddRange(many);
            return new SfxRecipe { Peak = peak, Seed = layers.Count * 13 + (int)(peak * 100), Layers = layers.ToArray() };
        }

        static SfxLayer Tone(SfxWave wave, float delay, float length, float decay, float from, float to, float gain,
            float attack = 0.004f, float lp = 0f, float vibHz = 0f, float vibSemis = 0f) => new SfxLayer
        {
            Wave = wave, Delay = delay, Length = length, Attack = attack, Decay = decay, FreqFrom = from, FreqTo = to, Gain = gain,
            LowPassFrom = lp, LowPassTo = lp, VibratoHz = vibHz, VibratoSemitones = vibSemis,
        };

        // Filtered noise; its low-pass sweeps from lpFrom to lpTo.
        static SfxLayer Hiss(float delay, float length, float decay, float gain, float lpFrom, float lpTo, float attack = 0.002f, float hp = 0f) => new SfxLayer
        {
            Wave = SfxWave.Noise, Delay = delay, Length = length, Attack = attack, Decay = decay, Gain = gain,
            LowPassFrom = lpFrom, LowPassTo = lpTo, HighPass = hp,
        };

        static SfxLayer Thump(float delay, float length, float from, float to, float gain) =>
            Tone(SfxWave.Sine, delay, length, length * 0.35f, from, to, gain, attack: 0.002f);

        // A soft puff of air: Vision §9's light, never punishing death.
        static SfxLayer Poof(float delay, float length, float gain) => Hiss(delay, length, length * 0.4f, gain, 1600f, 300f, attack: 0.01f);

        // Stone breaking (the developer: "a bit more of a crash sound"): a crack, the crash with a low boom, then debris.
        static SfxLayer[] Crash(float delay, float weight) => new[]
        {
            Hiss(delay, 0.035f, 0.01f, 1f, 7000f, 5000f, hp: 1500f),
            Tone(SfxWave.Saw, delay, 0.06f, 0.015f, 420f, 160f, 0.3f, lp: 3000f),
            Hiss(delay + 0.01f, 0.6f, 0.14f, 1f, 5000f, 900f),
            Hiss(delay + 0.01f, 0.45f, 0.12f, 0.6f * weight, 900f, 350f, hp: 120f),
            Thump(delay + 0.01f, 0.5f, 110f / weight, 45f, 0.35f * weight),
            Hiss(delay + 0.12f, 0.06f, 0.02f, 0.55f, 3500f, 2000f, hp: 400f), Thump(delay + 0.12f, 0.08f, 230f, 150f, 0.25f),
            Hiss(delay + 0.19f, 0.05f, 0.018f, 0.45f, 3000f, 1800f, hp: 400f),
            Hiss(delay + 0.27f, 0.07f, 0.02f, 0.4f, 2600f, 1400f, hp: 300f), Thump(delay + 0.27f, 0.08f, 190f, 120f, 0.2f),
            Hiss(delay + 0.37f, 0.05f, 0.02f, 0.3f, 2400f, 1200f, hp: 300f),
            Hiss(delay + 0.5f, 0.06f, 0.02f, 0.22f, 2000f, 1000f, hp: 300f),
        };

        // Thunder rolling: filtered noise that swells in and dies away unevenly. It stays above about 100 Hz, since a phone's
        // speaker plays little below 200 Hz.
        static SfxLayer Rumble(float delay, float length, float gain, float attack = 0.06f) => new SfxLayer
        {
            Wave = SfxWave.Noise, Delay = delay, Length = length, Attack = attack, Decay = length * 0.45f, Gain = gain,
            LowPassFrom = 1000f, LowPassMid = 600f, LowPassTo = 280f, MidAt = 0.4f, HighPass = 90f,
            TremoloHz = 3.1f + delay * 2f, TremoloDepth = 0.7f, Release = length * 0.3f,
        };

        static SfxLayer Pluck(float delay, float length, float freq, float ring, float gain, float lp) => new SfxLayer
        {
            Wave = SfxWave.Pluck, Delay = delay, Length = length, Decay = ring, FreqFrom = freq, FreqTo = freq, Gain = gain,
            LowPassFrom = lp, LowPassTo = lp,
        };
    }
}
