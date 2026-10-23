using System;
using NUnit.Framework;
using Parallax.Core;

namespace Parallax.Tests.EditMode
{
    /// <summary>PAX-064 (D-158): the synthesized sounds, the ambient bed, the cue limiter and the volume mapping.</summary>
    public sealed class SfxSynthTests
    {
        const int Rate = 44100;

        static AudioCue[] AllCues() => (AudioCue[])Enum.GetValues(typeof(AudioCue));

        [Test]
        public void EveryCue_RendersASound_AtItsPeak_StartingAndEndingSilently()
        {
            foreach (AudioCue cue in AllCues())
            {
                if (CatVoice.IsVoiceOnly(cue)) { Assert.AreEqual(0, SfxPresets.For(cue).Layers.Length, cue + " is voice only"); continue; }
                SfxRecipe recipe = SfxPresets.For(cue);
                float[] s = SfxSynth.Render(recipe, Rate);
                Assert.Greater(s.Length, Rate / 50, cue + " is at least 20 ms long");
                Assert.LessOrEqual(s.Length, Rate * 2, cue + " is under 2 s");
                float peak = 0f;
                foreach (float x in s)
                {
                    Assert.IsFalse(float.IsNaN(x) || float.IsInfinity(x), cue + " has a bad sample");
                    peak = Math.Max(peak, Math.Abs(x));
                }
                Assert.AreEqual(recipe.Peak, peak, 1e-4f, cue + "'s loudest sample is its peak");
                Assert.LessOrEqual(peak, 1f, cue + " never clips");
                Assert.Less(Math.Abs(s[0]), 0.05f, cue + " starts without a click");
                Assert.Less(Math.Abs(s[s.Length - 1]), 0.02f, cue + " ends without a click");
            }
        }

        [Test]
        public void Rendering_IsDeterministic()
        {
            foreach (AudioCue cue in new[] { AudioCue.TrapCollapse, AudioCue.LevelComplete, AudioCue.DeathZapped })
                CollectionAssert.AreEqual(SfxSynth.Render(SfxPresets.For(cue), Rate), SfxSynth.Render(SfxPresets.For(cue), Rate), cue.ToString());
        }

        [Test]
        public void Sweep_IsExponential_AndAZeroEndHolds()
        {
            Assert.AreEqual(200f, SfxSynth.Sweep(200f, 800f, 0f), 1e-3f);
            Assert.AreEqual(400f, SfxSynth.Sweep(200f, 800f, 0.5f), 1e-2f, "halfway is the geometric mean");
            Assert.AreEqual(800f, SfxSynth.Sweep(200f, 800f, 1f), 1e-2f);
            Assert.AreEqual(300f, SfxSynth.Sweep(300f, 0f, 0.7f));
        }

        [Test]
        public void Bend_PassesThroughItsMiddle_AndWithoutOneIsASweep()
        {
            Assert.AreEqual(500f, SfxSynth.Bend(500f, 800f, 400f, 0.3f, 0f), 1e-3f);
            Assert.AreEqual(800f, SfxSynth.Bend(500f, 800f, 400f, 0.3f, 0.3f), 1e-2f, "the middle at its point");
            Assert.AreEqual(400f, SfxSynth.Bend(500f, 800f, 400f, 0.3f, 1f), 1e-2f);
            Assert.AreEqual(SfxSynth.Sweep(200f, 800f, 0.4f), SfxSynth.Bend(200f, 0f, 800f, 0.5f, 0.4f), 1e-4f, "no middle, a plain sweep");
        }

        [Test]
        public void TheFormant_PassesItsCentre_AndCutsFarFromIt()
        {
            float Level(float hz)
            {
                var bp = new SfxSynth.BandPass();
                float peak = 0f;
                for (int k = 0; k < Rate / 5; k++)
                {
                    float y = bp.Step((float)Math.Sin(2 * Math.PI * hz * k / Rate), 1000f, 5f, Rate);
                    if (k > Rate / 10) peak = Math.Max(peak, Math.Abs(y));
                }
                return peak;
            }
            Assert.AreEqual(1f, Level(1000f), 0.05f, "unity at the centre");
            Assert.Less(Level(250f), 0.1f);
            Assert.Less(Level(4000f), 0.1f);
        }

        [Test]
        public void AWorldSound_FadesWithDistance()
        {
            Assert.AreEqual(1f, AudioCues.Falloff(3f, 9f, 17f));
            Assert.AreEqual(0.5f, AudioCues.Falloff(13f, 9f, 17f), 1e-5f);
            Assert.AreEqual(0f, AudioCues.Falloff(17f, 9f, 17f));
            Assert.AreEqual(0f, AudioCues.Falloff(40f, 9f, 17f));
        }

        [Test]
        public void TheCatsVoice_IsInTheCatCues()
        {
            // 2026-10-20 ("I prefer more cat sounds than environment sounds"): deaths, respawn, flips and the finish carry a voice,
            // and the room's own sounds sit under the cat's. D-165: the voice is CatVoice's (reel S), and the formant voice that
            // was "too cartoony" is gone from every cue.
            foreach (AudioCue cue in new[] { AudioCue.DeathDefault, AudioCue.DeathPit, AudioCue.DeathSpiked, AudioCue.DeathCrushed, AudioCue.DeathZapped,
                AudioCue.DeathArrow, AudioCue.Respawn, AudioCue.LevelComplete, AudioCue.TrapFlip, AudioCue.TrapInverter, AudioCue.HardLand, AudioCue.WallJump })
                Assert.Greater(CatVoice.For(cue, 0).Length, 0, cue + " has the cat's voice");
            foreach (AudioCue cue in AllCues())
                Assert.IsFalse(Array.Exists(SfxPresets.For(cue).Layers, l => l.FormantFrom > 0f), cue + " has no formant voice left");
            Assert.Greater(SfxPresets.Spec(AudioCue.DeathSpiked).Volume, SfxPresets.Spec(AudioCue.TrapMover).Volume);
            CueSpec arrow = SfxPresets.Spec(AudioCue.TrapArrow);
            Assert.AreEqual(1, arrow.MaxVoices, "one arrow sound at a time");
            Assert.Less(arrow.Volume, SfxPresets.Spec(AudioCue.TrapMover).Volume);
        }

        [Test]
        public void TheBed_LoopsWithoutASeam_AndHasItsLength()
        {
            BedRecipe recipe = SfxPresets.Bed();
            const int bedRate = 22050;
            float[] s = AmbientBed.Render(recipe, bedRate);
            Assert.AreEqual((int)Math.Round(recipe.Seconds * bedRate), s.Length);
            float peak = 0f, biggestStep = 0f;
            for (int i = 0; i < s.Length; i++)
            {
                peak = Math.Max(peak, Math.Abs(s[i]));
                biggestStep = Math.Max(biggestStep, Math.Abs(s[(i + 1) % s.Length] - s[i]));
            }
            Assert.AreEqual(recipe.Peak, peak, 1e-4f);
            float seam = Math.Abs(s[0] - s[s.Length - 1]);
            Assert.LessOrEqual(seam, biggestStep + 1e-6f, "the jump from the loop's end to its start is no bigger than any step inside it");
            Assert.Less(seam, 0.05f);
        }

        [Test]
        public void LoopingFrequencies_FitWholeCycles()
        {
            float f = AmbientBed.Looping(146.83f, 24f);
            Assert.AreEqual(Math.Round(f * 24f), f * 24f, 1e-3f);
            Assert.AreEqual(146.83f, f, 0.05f);
        }

        [Test]
        public void TheLimiter_CapsVoices_AndSpacesStarts()
        {
            var limiter = new CueLimiter();
            Assert.IsTrue(limiter.TryStart(1, 0f, 1f, 2, 0.05f));
            Assert.IsFalse(limiter.TryStart(1, 0.02f, 1f, 2, 0.05f), "too soon after the last start");
            Assert.IsTrue(limiter.TryStart(1, 0.1f, 1f, 2, 0.05f));
            Assert.IsFalse(limiter.TryStart(1, 0.2f, 1f, 2, 0.05f), "two already sounding");
            Assert.AreEqual(2, limiter.Sounding(1, 0.2f));
            Assert.IsTrue(limiter.TryStart(2, 0.2f, 1f, 2, 0.05f), "another cue has its own voices");
            Assert.IsTrue(limiter.TryStart(1, 1.01f, 1f, 2, 0.05f), "the first has finished");
            Assert.IsFalse(limiter.TryStart(3, 0f, 1f, 0, 0f), "no voices, no sound");
        }

        [Test]
        public void Volumes_MapToDecibels()
        {
            Assert.AreEqual(0f, AudioCues.ToDecibels(1f), 1e-4f);
            Assert.AreEqual(-6.02f, AudioCues.ToDecibels(0.5f), 0.01f);
            Assert.AreEqual(-80f, AudioCues.ToDecibels(0f));
        }

        [Test]
        public void ThePitchWalk_IsAFixedPattern_CentredOnTheRealPitch()
        {
            Assert.AreEqual(1f, AudioCues.PitchFor(0, 0.35f), 1e-6f);
            Assert.AreEqual(AudioCues.PitchFor(3, 0.35f), AudioCues.PitchFor(10, 0.35f), 1e-6f, "it repeats every 7 plays");
            for (int i = 0; i < 14; i++)
            {
                Assert.AreEqual(1f, AudioCues.PitchFor(i, 0f), 1e-6f, "no walk, no change");
                Assert.AreEqual(1f, AudioCues.PitchFor(i, 0.35f), 0.0185f, "within a third of a semitone (2^(0.315/12) = 1.0183)");
            }
        }

        [Test]
        public void EachDeathKind_HasItsCue()
        {
            Assert.AreEqual(AudioCue.DeathPit, AudioCues.ForDeath(CatDeathKind.Pit));
            Assert.AreEqual(AudioCue.DeathSpiked, AudioCues.ForDeath(CatDeathKind.Spiked));
            Assert.AreEqual(AudioCue.DeathCrushed, AudioCues.ForDeath(CatDeathKind.Crushed));
            Assert.AreEqual(AudioCue.DeathZapped, AudioCues.ForDeath(CatDeathKind.Zapped));
            Assert.AreEqual(AudioCue.DeathArrow, AudioCues.ForDeath(CatDeathKind.Arrow));
            Assert.AreEqual(AudioCue.DeathDefault, AudioCues.ForDeath(CatDeathKind.Default));
        }
    }
}
