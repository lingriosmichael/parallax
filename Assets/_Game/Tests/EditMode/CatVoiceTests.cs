using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Audio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Parallax.Tests.EditMode
{
    /// <summary>D-165 (PAX-110): the cat's voice. Which cues carry it (CatVoice), the generated sounds on disk
    /// (Tools/Audio/cat_voice.py), the bank's clips after PARALLAX/Setup/Cat Voice (inconclusive until the menu has run), and a
    /// GameAudio woken in the template playing a cue with its voice. Restores the Test Runner's scene afterwards.</summary>
    public sealed class CatVoiceTests
    {
        const string Folder = "Assets/_Game/Audio/CatVoice";
        const string TemplatePath = "Assets/_Game/Scenes/Levels/_LevelTemplate.unity";

        static AudioCue[] AllCues() => (AudioCue[])Enum.GetValues(typeof(AudioCue));
        static CatVoiceRole[] AllRoles() => (CatVoiceRole[])Enum.GetValues(typeof(CatVoiceRole));

        static string[] FilesFor(CatVoiceRole role) => Directory.Exists(Folder)
            ? Directory.GetFiles(Folder, role + "_*.wav").Select(p => p.Replace('\\', '/')).OrderBy(p => p, StringComparer.Ordinal).ToArray()
            : new string[0];

        [TearDown]
        public void Restore()
        {
            TickTime.ResetToDefault();
            CatDeathKindTagTests.RecreateUntitledScene();
        }

        [Test]
        public void EveryVoiceLayer_StartsWithinASecond_AtAVolumeItCanPlay()
        {
            foreach (AudioCue cue in AllCues())
                for (int n = 0; n < 8; n++)
                    foreach (VoiceLayer layer in CatVoice.For(cue, n))
                    {
                        Assert.That(layer.Delay, Is.InRange(0f, 0.999f), cue + "'s voice starts within a second");
                        Assert.That(layer.Volume, Is.GreaterThan(0f).And.LessThanOrEqualTo(1f), cue + "'s voice volume");
                    }
            Assert.AreEqual(new[] { AudioCue.Idle }, AllCues().Where(CatVoice.IsVoiceOnly).ToArray(), "only Idle is voice only");
        }

        [Test]
        public void OneJumpInFour_IsVoiced_AlternatingAChirpAndAMrp()
        {
            var roles = new List<CatVoiceRole>();
            for (int n = 0; n < 16; n++)
            {
                VoiceLayer[] voice = CatVoice.For(AudioCue.Jump, n);
                Assert.AreEqual(n % CatVoice.JumpEvery == CatVoice.JumpEvery - 1 ? 1 : 0, voice.Length, "jump " + n);
                roles.AddRange(voice.Select(v => v.Role));
            }
            CollectionAssert.AreEqual(new[] { CatVoiceRole.Chirp, CatVoiceRole.Mrp, CatVoiceRole.Chirp, CatVoiceRole.Mrp }, roles);
            CollectionAssert.AreEqual(CatVoice.For(AudioCue.Jump, 7).Select(v => v.Role), CatVoice.For(AudioCue.Jump, 7).Select(v => v.Role), "a fixed pattern");
        }

        [Test]
        public void EveryDeath_IsTheThudAndYelp_AndAFallLandsAtTheEndOfItsWhoosh()
        {
            foreach (AudioCue cue in AllCues().Where(c => c >= AudioCue.DeathDefault && c <= AudioCue.DeathArrow))
                Assert.IsTrue(CatVoice.For(cue, 0).Any(v => v.Role == CatVoiceRole.Death), cue + " carries the death voice");
            Assert.Greater(CatVoice.For(AudioCue.DeathPit, 0)[0].Delay, 0.3f);
            CollectionAssert.AreEquivalent(new[] { CatVoiceRole.Mrrow, CatVoiceRole.Purr }, CatVoice.For(AudioCue.LevelComplete, 0).Select(v => v.Role));
        }

        [Test]
        public void TheIdleMeow_ComesAfterSixStillSeconds_ThenEveryEighteen()
        {
            TickTime.SecondsPerTickSource = () => 0.02f;
            Assert.AreEqual(TickTime.ToWholeTicks(6f), CatVoice.IdleAfterTicks, "6 s at the 50 Hz tick");
            Assert.AreEqual(TickTime.ToWholeTicks(18f), CatVoice.IdleRepeatTicks, "then every 18 s");
            var due = Enumerable.Range(0, 3000).Where(CatVoice.IdleDue).ToArray();
            CollectionAssert.AreEqual(new[] { 300, 1200, 2100 }, due);
        }

        [Test]
        public void EveryRole_HasItsGeneratedSounds_MonoAt44100_WithoutClicks()
        {
            foreach (CatVoiceRole role in AllRoles())
            {
                string[] files = FilesFor(role);
                Assert.Greater(files.Length, 0, role + " has sounds in " + Folder + " (run Tools/Audio/cat_voice.py)");
                foreach (string file in files)
                {
                    byte[] wav = File.ReadAllBytes(file);
                    Assert.AreEqual(1, BitConverter.ToInt16(wav, 22), file + " is mono");
                    Assert.AreEqual(44100, BitConverter.ToInt32(wav, 24), file + " is 44.1 kHz");
                    Assert.AreEqual(16, BitConverter.ToInt16(wav, 34), file + " is 16-bit");
                    int samples = (wav.Length - 44) / 2;
                    Assert.That(samples / 44100f, Is.InRange(0.1f, 3f), file + "'s length");
                    float first = Math.Abs(BitConverter.ToInt16(wav, 44)) / 32767f, last = Math.Abs(BitConverter.ToInt16(wav, wav.Length - 2)) / 32767f;
                    Assert.Less(first, 0.05f, file + " starts without a click");
                    Assert.Less(last, 0.02f, file + " ends without a click");
                }
            }
            Assert.IsTrue(File.Exists(Folder + "/cat_voice.json"), "the generator's manifest is there");
        }

        [Test]
        public void TheBank_HoldsEveryRolesSounds_DecompressedOnLoad()
        {
            var bank = AssetDatabase.LoadAssetAtPath<AudioCueBank>(AudioCueBank.AssetPath);
            if (bank == null) Assert.Ignore("no AudioCueBank");
            if (bank.VoiceVariants(CatVoiceRole.Mew) == 0) Assert.Inconclusive("run PARALLAX/Setup/Cat Voice (PAX-110) first");
            foreach (CatVoiceRole role in AllRoles())
            {
                string[] files = FilesFor(role);
                Assert.AreEqual(files.Length, bank.VoiceVariants(role), role + "'s variants are in the bank");
                for (int i = 0; i < files.Length; i++)
                {
                    AudioClip clip = bank.VoiceClip(role, i);
                    Assert.AreEqual(AssetDatabase.LoadAssetAtPath<AudioClip>(files[i]), clip, role + " variant " + i + " in file order");
                    Assert.AreEqual(1, clip.channels, files[i] + " is mono");
                    var importer = (AudioImporter)AssetImporter.GetAtPath(files[i]);
                    Assert.AreEqual(AudioClipLoadType.DecompressOnLoad, importer.defaultSampleSettings.loadType, files[i]);
                }
                Assert.AreEqual(bank.VoiceClip(role, 0), bank.VoiceClip(role, files.Length), role + "'s variants repeat in turn");
            }
        }

        [Test]
        public void AWokenGameAudio_StartsACueWithItsVoice_AndTheIdleMeowAlone()
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(TemplatePath) == null) Assert.Ignore("no template");
            EditorSceneManager.OpenScene(TemplatePath, OpenSceneMode.Single);
            GameObject root = GameObject.Find("GameAudio");
            if (root == null) Assert.Inconclusive("run PARALLAX/Setup/Audio (PAX-064) first");
            GameAudio gameAudio = root.GetComponent<GameAudio>();
            if (gameAudio.Bank == null || gameAudio.Bank.VoiceVariants(CatVoiceRole.Mew) == 0) Assert.Inconclusive("run PARALLAX/Setup/Cat Voice (PAX-110) first");
            typeof(GameAudio).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(gameAudio, null);
            var pool = (AudioSource[])typeof(GameAudio).GetField("pool", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(gameAudio);
            var clips = (Dictionary<AudioCue, AudioClip>)typeof(GameAudio).GetField("clips", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(gameAudio);

            Assert.IsTrue(gameAudio.Play(AudioCue.Respawn));
            Assert.IsTrue(pool.Any(s => s.clip == clips[AudioCue.Respawn]), "the pluck started");
            Assert.IsTrue(pool.Any(s => s.clip == gameAudio.Bank.VoiceClip(CatVoiceRole.Mew, 0)), "and the mew with it, on its own voice");

            Assert.IsFalse(clips.ContainsKey(AudioCue.Idle), "Idle has nothing synthesized");
            Assert.IsTrue(gameAudio.Play(AudioCue.Idle), "the idle meow plays on its own");
            Assert.IsTrue(pool.Any(s => s.clip == gameAudio.Bank.VoiceClip(CatVoiceRole.MewLow, 0)));
            Assert.IsFalse(gameAudio.Play(AudioCue.Idle), "and not again at once");
        }
    }
}
