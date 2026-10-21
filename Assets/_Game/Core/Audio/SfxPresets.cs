using System;

namespace Parallax.Core
{
    /// <summary>PAX-064 (D-158): the game's sounds, designed in code. Soft and organic to sit with the painted art: plucked
    /// strings, filtered noise and low thumps rather than chip-tune beeps. A recording in AudioCueBank replaces any of them.</summary>
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
            // "I prefer more cat sounds than environment sounds": the rest of the room sits under the cat.
            _ => new CueSpec(AudioChannel.Sfx, 0.45f, 3, 0.04f),
        };

        public static SfxRecipe For(AudioCue cue) => cue switch
        {
            AudioCue.Jump => R(0.8f, Tone(SfxWave.Triangle, 0f, 0.11f, 0.06f, 260f, 520f, 1f, lp: 2400f), Hiss(0f, 0.06f, 0.03f, 0.25f, 1800f, 5000f)),
            // A wall jump: the push, and a little "hup".
            AudioCue.WallJump => R(0.8f, Hiss(0f, 0.09f, 0.04f, 0.45f, 900f, 3000f), Voice(0.01f, 0.1f, 560f, 0f, 760f, 0.5f, 0.55f, 600f, 0f, 900f, 1500f, 0f, 2200f)),
            AudioCue.Land => R(0.8f, Thump(0f, 0.1f, 140f, 70f, 1f), Hiss(0f, 0.09f, 0.03f, 0.6f, 900f, 250f)),
            // A hard landing: the thud and a grunted "mrf".
            AudioCue.HardLand => R(0.9f, Thump(0f, 0.18f, 120f, 50f, 1f), Hiss(0f, 0.12f, 0.05f, 0.5f, 1400f, 200f),
                Voice(0.02f, 0.13f, 330f, 0f, 250f, 0.5f, 0.6f, 500f, 0f, 450f, 1100f, 0f, 900f)),
            // Back at the checkpoint: a bright trilled "mrrp?" over a soft pluck.
            AudioCue.Respawn => R(0.75f, Pluck(0f, 0.45f, 523.25f, 0.3f, 0.35f, 3500f),
                Trill(0.02f, 0.24f, 400f, 640f, 0.9f)),
            // A level done: the chime, a happy "mrrrow!" and a purr under it.
            AudioCue.LevelComplete => R(0.85f,
                Pluck(0f, 0.9f, 392f, 0.6f, 0.7f, 4000f), Pluck(0.11f, 0.9f, 493.88f, 0.6f, 0.7f, 4000f),
                Pluck(0.22f, 0.9f, 587.33f, 0.6f, 0.75f, 4000f), Pluck(0.36f, 1.4f, 783.99f, 0.9f, 0.85f, 4500f),
                Tone(SfxWave.Sine, 0.36f, 1.4f, 0.5f, 196f, 196f, 0.3f, attack: 0.05f),
                Voice(0.42f, 0.55f, 520f, 820f, 700f, 0.55f, 0.9f, 650f, 1050f, 800f, 1500f, 2500f, 1900f, trill: 20f),
                Purr(0.5f, 1.2f, 0.5f)),
            AudioCue.UiTap => R(0.6f, Pluck(0f, 0.18f, 880f, 0.09f, 1f, 5000f), Hiss(0f, 0.012f, 0.005f, 0.3f, 6000f, 6000f, hp: 2500f)),

            // Deaths: the cat cries out (Vision §9: light, never punishing), with a little of what hit it under the cry.
            AudioCue.DeathDefault => R(0.85f, Yowl(0f, 0.45f, 600f, 900f, 430f, 1f), Poof(0.08f, 0.3f, 0.35f)),
            // A fall: a long descending "mraaooow" and the bump at the bottom.
            AudioCue.DeathPit => R(0.85f, Voice(0f, 0.5f, 880f, 960f, 360f, 0.15f, 1f, 750f, 1000f, 520f, 2100f, 2700f, 1000f, vib: 6f, vibSemis: 0.4f),
                Thump(0.46f, 0.2f, 110f, 50f, 0.6f), Poof(0.46f, 0.22f, 0.3f)),
            // Spikes: a sharp "MRAOW!" and a glint of steel.
            AudioCue.DeathSpiked => R(0.9f, Yowl(0f, 0.36f, 700f, 1100f, 560f, 1f), Hiss(0f, 0.04f, 0.015f, 0.35f, 7000f, 3000f, hp: 1500f),
                Thump(0f, 0.12f, 160f, 70f, 0.4f)),
            // Crushed: a "mrk!" cut short by the thud.
            AudioCue.DeathCrushed => R(0.95f, Voice(0f, 0.15f, 650f, 820f, 520f, 0.35f, 0.9f, 700f, 1000f, 650f, 1900f, 2500f, 1500f),
                Thump(0.1f, 0.35f, 95f, 38f, 1f), Hiss(0.1f, 0.3f, 0.09f, 0.6f, 1200f, 150f)),
            // Zapped: a buzzing yowl and a crackle.
            AudioCue.DeathZapped => R(0.85f, Voice(0f, 0.45f, 620f, 950f, 420f, 0.25f, 1f, 700f, 1050f, 560f, 1700f, 2700f, 1100f, vib: 31f, vibSemis: 1.6f),
                Hiss(0f, 0.25f, 0.1f, 0.45f, 8000f, 1500f, hp: 800f)),
            // An arrow: a startled "mew!" and the thunk.
            AudioCue.DeathArrow => R(0.85f, Voice(0.02f, 0.26f, 780f, 1080f, 660f, 0.3f, 1f, 800f, 1150f, 750f, 2300f, 3000f, 1800f),
                Pluck(0f, 0.18f, 130f, 0.08f, 0.6f, 1800f), Hiss(0f, 0.03f, 0.012f, 0.3f, 5000f, 2000f, hp: 1200f)),

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
            // Gravity flip: a surprised "mew!" riding a rising airy sweep.
            AudioCue.TrapFlip => R(0.8f, Voice(0f, 0.24f, 700f, 0f, 1150f, 0.5f, 1f, 800f, 0f, 1150f, 2100f, 0f, 3000f),
                Hiss(0f, 0.36f, 0.16f, 0.35f, 500f, 4000f, attack: 0.05f)),
            // Inverter: a confused "mrrow?" (its end rising) over a dizzy wobble.
            AudioCue.TrapInverter => R(0.8f, Voice(0f, 0.42f, 520f, 450f, 800f, 0.45f, 1f, 650f, 820f, 900f, 1500f, 1200f, 2400f, vib: 6f, vibSemis: 0.5f),
                Tone(SfxWave.Triangle, 0f, 0.5f, 0.25f, 420f, 380f, 0.25f, attack: 0.02f, vibHz: 7f, vibSemis: 2.2f)),
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

        // A cat's voice (2026-10-20, "I prefer more cat sounds"): a bright buzzing source through two vowel formants, pitch,
        // first formant and second formant each bending from a start through a middle (at `at`, 0 to 1) to an end (a middle of 0
        // sweeps straight). "Mee-ow" is the second formant rising then falling; a trill pulses the level.
        static SfxLayer[] Voice(float delay, float length, float f0, float fMid, float fEnd, float at, float gain,
            float f1From, float f1Mid, float f1To, float f2From, float f2Mid, float f2To, float vib = 5.5f, float vibSemis = 0.25f, float trill = 0f)
        {
            SfxLayer Formant(float from, float mid, float to, float q, float g) => new SfxLayer
            {
                Wave = SfxWave.Saw, Delay = delay, Length = length, Attack = Math.Min(0.03f, length * 0.2f), Gain = gain * g,
                FreqFrom = f0, FreqMid = fMid, FreqTo = fEnd, MidAt = at, VibratoHz = vib, VibratoSemitones = vibSemis,
                FormantFrom = from, FormantMid = mid, FormantTo = to, FormantQ = q, LowPassFrom = 5000f, LowPassTo = 3500f,
                TremoloHz = trill, TremoloDepth = trill > 0f ? 0.75f : 0f, Release = length * 0.35f,
            };
            return new[]
            {
                Formant(f1From, f1Mid, f1To, 3f, 1f),
                Formant(f2From, f2Mid, f2To, 5f, 1.1f),
                // A breath of air in the voice.
                new SfxLayer { Wave = SfxWave.Noise, Delay = delay, Length = length, Attack = 0.02f, Gain = gain * 0.12f, HighPass = 1500f,
                    FormantFrom = f2From, FormantMid = f2Mid, FormantTo = f2To, FormantQ = 3f, MidAt = at, Release = length * 0.4f },
            };
        }

        // A full cry: "mrr-AOW", bright in the middle.
        static SfxLayer[] Yowl(float delay, float length, float f0, float fPeak, float fEnd, float gain) =>
            Voice(delay, length, f0, fPeak, fEnd, 0.3f, gain, 650f, 1050f, 600f, 1500f, 2600f, 1000f, vib: 8f, vibSemis: 0.5f);

        // A rolled "mrrp", rising like a question.
        static SfxLayer[] Trill(float delay, float length, float from, float to, float gain) =>
            Voice(delay, length, from, 0f, to, 0.5f, gain, 450f, 0f, 750f, 1100f, 0f, 2000f, vib: 0f, vibSemis: 0f, trill: 24f);

        // A purr: a low buzz and a breathy rasp, pulsing about 25 times a second.
        static SfxLayer[] Purr(float delay, float length, float gain) => new[]
        {
            new SfxLayer { Wave = SfxWave.Saw, Delay = delay, Length = length, Attack = 0.15f, Gain = gain, FreqFrom = 52f, FreqTo = 50f,
                LowPassFrom = 600f, LowPassTo = 500f, TremoloHz = 25f, TremoloDepth = 0.9f, Release = 0.4f },
            new SfxLayer { Wave = SfxWave.Noise, Delay = delay, Length = length, Attack = 0.15f, Gain = gain * 0.5f,
                LowPassFrom = 1200f, LowPassTo = 900f, HighPass = 150f, TremoloHz = 25f, TremoloDepth = 0.95f, Release = 0.4f },
        };

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
