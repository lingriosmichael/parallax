using System;

namespace Parallax.Core
{
    public enum SfxWave : byte { Sine, Triangle, Square, Saw, Noise, Pluck }

    /// <summary>PAX-064 (D-158): one voice of a synthesized sound. A tone or noise that starts Delay seconds in, lasts Length,
    /// rises over Attack and then decays exponentially (time constant Decay; 0 holds), sweeping its pitch and filters from
    /// their From to their To values across its length. Pluck is a plucked string (Karplus-Strong): Decay is how long it rings.
    /// A Mid value (FreqMid, LowPassMid, FormantMid) bends that sweep through it at MidAt (0 to 1 of the length), so a pitch can
    /// rise and fall again like a meow; Formant is a resonant band-pass (a vowel); Tremolo pulses the level (a purr or a trill);
    /// Release fades the last seconds out.</summary>
    [Serializable]
    public struct SfxLayer
    {
        public SfxWave Wave;
        public float Delay, Length, Attack, Decay;
        public float FreqFrom, FreqTo;
        public float Gain;
        public float LowPassFrom, LowPassTo; // Hz; 0 is off
        public float HighPass;               // Hz; 0 is off
        public float VibratoHz, VibratoSemitones;
        public float FreqMid, LowPassMid, MidAt;
        public float FormantFrom, FormantMid, FormantTo, FormantQ;  // Hz; 0 is off
        public float TremoloHz, TremoloDepth;
        public float Release;
    }

    /// <summary>PAX-064: a sound as layers, scaled so its loudest sample is Peak.</summary>
    [Serializable]
    public sealed class SfxRecipe
    {
        public float Peak = 0.8f;
        public int Seed = 1;
        public SfxLayer[] Layers = new SfxLayer[0];

        public float Length
        {
            get
            {
                float end = 0f;
                foreach (SfxLayer l in Layers) end = Math.Max(end, l.Delay + l.Length);
                return end;
            }
        }
    }

    /// <summary>PAX-064 (D-158): renders recipes to mono samples. Pure and deterministic: the noise comes from a seeded
    /// generator, so the same recipe always gives the same samples. Presentation only; nothing here touches gameplay.</summary>
    public static class SfxSynth
    {
        const float TailFade = 0.004f, MinAttack = 0.0015f;

        public static float[] Render(SfxRecipe recipe, int sampleRate)
        {
            if (recipe == null || recipe.Layers == null || sampleRate <= 0) return new float[0];
            var samples = new float[(int)Math.Ceiling(recipe.Length * sampleRate)];
            for (int i = 0; i < recipe.Layers.Length; i++)
                AddLayer(recipe.Layers[i], unchecked((uint)(recipe.Seed * 7919 + i * 104729 + 1)), sampleRate, samples);
            Normalize(samples, recipe.Peak);
            return samples;
        }

        public static void AddLayer(in SfxLayer layer, uint seed, int rate, float[] into)
        {
            int start = (int)Math.Round(layer.Delay * rate), count = (int)Math.Round(layer.Length * rate);
            if (count <= 1 || start >= into.Length) return;
            count = Math.Min(count, into.Length - start);
            var noise = new Noise(seed);
            var low = new LowPass();
            var high = new HighPass();
            var formant = new BandPass();
            float[] ring = layer.Wave == SfxWave.Pluck ? PluckRing(layer.FreqFrom, rate, ref noise) : null;
            float ringLoss = ring == null ? 1f : (float)Math.Exp(-1.0 / Math.Max(1e-3, layer.FreqFrom * Math.Max(1e-3f, layer.Decay)));
            int ringAt = 0;
            double phase = 0;
            float attack = Math.Max(MinAttack, layer.Attack);

            for (int k = 0; k < count; k++)
            {
                float t = (float)k / rate, u = count > 1 ? (float)k / (count - 1) : 0f;
                float freq = Bend(layer.FreqFrom, layer.FreqMid, layer.FreqTo, layer.MidAt, u);
                if (layer.VibratoSemitones != 0f && layer.VibratoHz > 0f)
                    freq *= (float)Math.Pow(2.0, layer.VibratoSemitones * Math.Sin(2 * Math.PI * layer.VibratoHz * t) / 12.0);

                float x;
                if (ring != null)
                {
                    int next = (ringAt + 1) % ring.Length;
                    x = ring[ringAt];
                    ring[ringAt] = ringLoss * 0.5f * (ring[ringAt] + ring[next]);
                    ringAt = next;
                }
                else
                {
                    phase += freq / rate;
                    phase -= Math.Floor(phase);
                    x = Wave(layer.Wave, (float)phase, ref noise);
                }

                if (layer.LowPassFrom > 0f) x = low.Step(x, Bend(layer.LowPassFrom, layer.LowPassMid, layer.LowPassTo, layer.MidAt, u), rate);
                if (layer.HighPass > 0f) x = high.Step(x, layer.HighPass, rate);
                if (layer.FormantFrom > 0f) x = formant.Step(x, Bend(layer.FormantFrom, layer.FormantMid, layer.FormantTo, layer.MidAt, u), layer.FormantQ, rate);

                float env = t < attack ? t / attack : ring != null || layer.Decay <= 0f ? 1f : (float)Math.Exp(-(t - attack) / layer.Decay);
                if (layer.TremoloHz > 0f) env *= 1f - layer.TremoloDepth * 0.5f * (1f - (float)Math.Cos(2 * Math.PI * layer.TremoloHz * t));
                float tail = (count - 1 - k) / (float)rate;
                if (tail < layer.Release) env *= tail / layer.Release;
                if (tail < TailFade) env *= tail / TailFade;
                into[start + k] += x * env * layer.Gain;
            }
        }

        /// <summary>Scales the samples so the loudest is peak (silence stays silence).</summary>
        public static void Normalize(float[] samples, float peak)
        {
            float max = 0f;
            foreach (float s in samples) max = Math.Max(max, Math.Abs(s));
            if (max < 1e-6f) return;
            float scale = peak / max;
            for (int i = 0; i < samples.Length; i++) samples[i] *= scale;
        }

        // An exponential sweep from a to b (b of 0 holds a), the way pitch and filter sweeps sound even.
        public static float Sweep(float a, float b, float u)
        {
            if (b <= 0f || a <= 0f || a == b) return a;
            return a * (float)Math.Pow(b / a, u);
        }

        /// <summary>A sweep from a to b that passes through mid at `at` (0 to 1); a mid of 0 is a plain Sweep.</summary>
        public static float Bend(float a, float mid, float b, float at, float u)
        {
            if (mid <= 0f || at <= 0f || at >= 1f) return Sweep(a, b, u);
            return u < at ? Sweep(a, mid, u / at) : Sweep(mid, b > 0f ? b : mid, (u - at) / (1f - at));
        }

        static float Wave(SfxWave wave, float phase, ref Noise noise) => wave switch
        {
            SfxWave.Sine => (float)Math.Sin(2 * Math.PI * phase),
            SfxWave.Triangle => 4f * Math.Abs(phase - 0.5f) - 1f,
            SfxWave.Square => phase < 0.5f ? 1f : -1f,
            SfxWave.Saw => 2f * phase - 1f,
            _ => noise.Next(),
        };

        static float[] PluckRing(float freq, int rate, ref Noise noise)
        {
            var ring = new float[Math.Max(2, (int)Math.Round(rate / Math.Max(20f, freq)))];
            for (int i = 0; i < ring.Length; i++) ring[i] = noise.Next();
            return ring;
        }

        /// <summary>xorshift32 white noise in [-1, 1].</summary>
        public struct Noise
        {
            uint state;
            public Noise(uint seed) { state = seed == 0 ? 2463534242u : seed; }
            public float Next()
            {
                state ^= state << 13;
                state ^= state >> 17;
                state ^= state << 5;
                return state / (float)uint.MaxValue * 2f - 1f;
            }
        }

        // Two one-pole stages: a gentle 12 dB/octave low-pass.
        public struct LowPass
        {
            float a1, a2;
            public float Step(float x, float cutoff, int rate)
            {
                float a = 1f - (float)Math.Exp(-2 * Math.PI * Math.Min(cutoff, rate * 0.45f) / rate);
                a1 += a * (x - a1);
                a2 += a * (a1 - a2);
                return a2;
            }
        }

        /// <summary>A resonant band-pass (Chamberlin state-variable filter), unity gain at its centre. Q of 0 means 4.</summary>
        public struct BandPass
        {
            float lowOut, bandOut;
            public float Step(float x, float centre, float q, int rate)
            {
                float f = 2f * (float)Math.Sin(Math.PI * Math.Min(centre, rate / 6f) / rate), damp = 1f / (q > 0f ? q : 4f);
                lowOut += f * bandOut;
                float highOut = x - lowOut - damp * bandOut;
                bandOut += f * highOut;
                return bandOut * damp;
            }
        }

        public struct HighPass
        {
            float lastIn, lastOut;
            public float Step(float x, float cutoff, int rate)
            {
                float rc = 1f / (2f * (float)Math.PI * cutoff), dt = 1f / rate, a = rc / (rc + dt);
                lastOut = a * (lastOut + x - lastIn);
                lastIn = x;
                return lastOut;
            }
        }
    }
}
