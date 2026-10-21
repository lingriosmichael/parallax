using System;

namespace Parallax.Core
{
    /// <summary>PAX-064 (D-158): the warm, organic bed under every scene (Vision §9): soft wind, a slow low chord that breathes,
    /// and a few far-off birds. It loops: every tone and swell completes a whole number of cycles in Seconds, and the wind's
    /// end is cross-faded into its start.</summary>
    [Serializable]
    public sealed class BedRecipe
    {
        public float Seconds = 24f;
        public float Peak = 0.55f;
        public int Seed = 11;

        public float WindGain = 0.55f;
        public float WindLowPass = 420f;
        public float WindSwing = 0.55f;   // how far the wind's filter opens and closes, as a fraction of WindLowPass
        public int WindCycles = 3;        // wind gusts per loop

        public float[] Chord = { 98f, 146.83f, 196f, 246.94f, 293.66f };
        public float PadGain = 0.32f;
        public float Detune = 0.35f;      // Hz between each tone's two voices: a slow, warm beat
        public int SwellCycles = 2;

        public int Birds = 4;
        public float BirdGain = 0.07f;
    }

    public static class AmbientBed
    {
        const float Fade = 1.5f;

        public static float[] Render(BedRecipe recipe, int rate)
        {
            if (recipe == null || recipe.Seconds <= 0f || rate <= 0) return new float[0];
            int n = (int)Math.Round(recipe.Seconds * rate), fade = Math.Min(n / 2, (int)(Fade * rate));
            float seconds = (float)n / rate;
            var wind = new float[n + fade];
            Wind(recipe, seconds, rate, wind);

            var loop = new float[n];
            for (int i = 0; i < n; i++) loop[i] = wind[i];
            // Equal-power cross-fade: the wind that would have followed the loop's end fades out over its start.
            for (int i = 0; i < fade; i++)
            {
                double w = (double)i / fade;
                loop[i] = (float)(wind[i] * Math.Sqrt(w) + wind[n + i] * Math.Sqrt(1 - w));
            }

            Pad(recipe, seconds, rate, loop);
            Birds(recipe, rate, loop);
            SfxSynth.Normalize(loop, recipe.Peak);
            return loop;
        }

        /// <summary>A frequency moved to the nearest one that fits a whole number of cycles in the loop.</summary>
        public static float Looping(float freq, float seconds) => Math.Max(1f, (float)Math.Round(freq * seconds)) / seconds;

        static void Wind(BedRecipe r, float seconds, int rate, float[] into)
        {
            var noise = new SfxSynth.Noise(unchecked((uint)(r.Seed * 31 + 7)));
            var low = new SfxSynth.LowPass();
            float brown = 0f;
            for (int i = 0; i < into.Length; i++)
            {
                double t = (double)i / rate;
                brown = brown * 0.985f + noise.Next() * 0.15f;
                double gust = Math.Sin(2 * Math.PI * r.WindCycles * t / seconds);
                float cutoff = r.WindLowPass * (float)(1 + r.WindSwing * gust);
                float level = (float)(0.7 + 0.3 * Math.Sin(2 * Math.PI * r.WindCycles * t / seconds - 0.6));
                into[i] = low.Step(brown, Math.Max(40f, cutoff), rate) * level * r.WindGain * 4f;
            }
        }

        static void Pad(BedRecipe r, float seconds, int rate, float[] into)
        {
            if (r.Chord == null || r.Chord.Length == 0) return;
            for (int c = 0; c < r.Chord.Length; c++)
            {
                double f1 = Looping(r.Chord[c], seconds), f2 = Looping(r.Chord[c] + r.Detune, seconds);
                double swellPhase = c * 2.1;
                float gain = r.PadGain / r.Chord.Length * (c == 0 ? 1.3f : 1f);
                for (int i = 0; i < into.Length; i++)
                {
                    double t = (double)i / rate;
                    double swell = 0.55 + 0.45 * Math.Sin(2 * Math.PI * r.SwellCycles * t / seconds + swellPhase);
                    into[i] += (float)((Math.Sin(2 * Math.PI * f1 * t) + Math.Sin(2 * Math.PI * f2 * t)) * 0.5 * swell * gain);
                }
            }
        }

        // A few short chirps at fixed places inside the loop, clear of its seam.
        static void Birds(BedRecipe r, int rate, float[] into)
        {
            var noise = new SfxSynth.Noise(unchecked((uint)(r.Seed * 131 + 3)));
            float seconds = (float)into.Length / rate;
            for (int b = 0; b < r.Birds; b++)
            {
                float at = seconds * (b + 0.5f + 0.3f * noise.Next()) / Math.Max(1, r.Birds);
                at = Math.Max(Fade + 0.2f, Math.Min(seconds - 1.2f, at));
                float pitch = 2600f + 700f * noise.Next();
                int notes = 2 + (b % 3);
                for (int k = 0; k < notes; k++)
                {
                    var chirp = new SfxLayer
                    {
                        Wave = SfxWave.Sine, Delay = at + k * 0.11f, Length = 0.07f, Attack = 0.01f, Decay = 0.03f,
                        FreqFrom = pitch * (1f + 0.04f * k), FreqTo = pitch * 1.35f, Gain = r.BirdGain * (1f - 0.15f * k),
                    };
                    SfxSynth.AddLayer(chirp, 1u, rate, into);
                }
            }
        }
    }
}
