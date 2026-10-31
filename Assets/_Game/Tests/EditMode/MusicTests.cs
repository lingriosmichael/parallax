using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Audio;
using UnityEditor;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    /// <summary>PAX-125 (D-277): the music. Each world has a loop on disk, the import fills the bank with streamed clips and is
    /// idempotent, a scene plays its world's loop in the bed's place (the bed stays the fallback), a level's track follows its
    /// biome, and the next scene picks the loop up where the last one left it.</summary>
    public sealed class MusicTests
    {
        const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        const BindingFlags HiddenStatic = BindingFlags.Static | BindingFlags.NonPublic;

        static readonly System.Type Import = System.Type.GetType("Parallax.Editor.Setup.MusicImport, Parallax.Editor");

        readonly List<Object> made = new List<Object>();

        T Made<T>(T thing) where T : Object { made.Add(thing); return thing; }

        [SetUp, TearDown]
        public void Clean()
        {
            foreach (Object o in made) if (o != null) Object.DestroyImmediate(o);
            made.Clear();
            SetResume(null, 0);
        }

        static void SetResume(AudioClip clip, int sample)
        {
            typeof(GameAudio).GetField("resumeClip", HiddenStatic).SetValue(null, clip);
            typeof(GameAudio).GetField("resumeSample", HiddenStatic).SetValue(null, sample);
        }

        static string FileFor(MusicTrack track) => (string)Import.GetMethod("FileFor").Invoke(null, new object[] { track });

        static void RunImport(AudioCueBank bank, List<string> changes) => Import.GetMethod("Import").Invoke(null, new object[] { bank, changes });

        AudioClip Tone(string name) => Made(AudioClip.Create(name, 44100, 2, 44100, false));

        AudioCueBank BankWith(MusicTrack track, AudioClip clip)
        {
            AudioCueBank bank = Made(ScriptableObject.CreateInstance<AudioCueBank>());
            var serialized = new SerializedObject(bank);
            SerializedProperty list = serialized.FindProperty("tracks");
            list.arraySize = 1;
            list.GetArrayElementAtIndex(0).FindPropertyRelative("Track").enumValueIndex = (int)track;
            list.GetArrayElementAtIndex(0).FindPropertyRelative("Clip").objectReferenceValue = clip;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return bank;
        }

        // A GameAudio woken on the bank, with the bed started; returns the bed's source.
        AudioSource StartBed(AudioCueBank bank, MusicTrack track)
        {
            GameObject go = Made(new GameObject("GameAudio"));
            GameAudio gameAudio = go.AddComponent<GameAudio>();
            var serialized = new SerializedObject(gameAudio);
            serialized.FindProperty("bank").objectReferenceValue = bank;
            serialized.FindProperty("track").enumValueIndex = (int)track;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            typeof(GameAudio).GetMethod("StartBed", Hidden).Invoke(gameAudio, null);
            return (AudioSource)typeof(GameAudio).GetField("bed", Hidden).GetValue(gameAudio);
        }

        [Test]
        public void EveryTrack_HasALongStereoLoop()
        {
            foreach (MusicTrack track in System.Enum.GetValues(typeof(MusicTrack)))
            {
                string path = FileFor(track);
                Assert.IsTrue(File.Exists(path), path + ": run Tools/Audio/music.py");
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                Assert.IsNotNull(clip, path + " didn't import");
                Assert.AreEqual(2, clip.channels, path);
                Assert.Greater(clip.length, 60f, path + " is long enough not to wear thin");
            }
        }

        [Test]
        public void TheImport_FillsTheBank_WithStreamedClips_AndIsIdempotent()
        {
            AudioCueBank bank = Made(ScriptableObject.CreateInstance<AudioCueBank>());
            Assert.IsNull(bank.MusicFor(MusicTrack.GoldenRuins), "a new bank has no music");
            RunImport(bank, new List<string>());
            foreach (MusicTrack track in System.Enum.GetValues(typeof(MusicTrack)))
            {
                Assert.AreEqual(Path.GetFileNameWithoutExtension(FileFor(track)), bank.MusicFor(track).name);
                var importer = (AudioImporter)AssetImporter.GetAtPath(FileFor(track));
                Assert.AreEqual(AudioClipLoadType.Streaming, importer.defaultSampleSettings.loadType, track.ToString());
                Assert.AreEqual(AudioCompressionFormat.Vorbis, importer.defaultSampleSettings.compressionFormat, track.ToString());
                Assert.IsFalse(importer.forceToMono, track.ToString());
            }
            var second = new List<string>();
            RunImport(bank, second);
            CollectionAssert.IsEmpty(second, "a second run changes nothing");
        }

        [TestCase("L001", MusicTrack.GoldenRuins)]
        [TestCase("L030", MusicTrack.GoldenRuins)]
        [TestCase("L031", MusicTrack.ObsidianVoid)]
        [TestCase("L060", MusicTrack.ObsidianVoid)]
        [TestCase(null, MusicTrack.GoldenRuins)]
        public void ALevelsTrack_FollowsItsWorld(string levelId, MusicTrack expected) =>
            Assert.AreEqual(expected, Import.GetMethod("TrackForLevel").Invoke(null, new object[] { levelId }));

        [Test]
        public void AScene_PlaysItsWorldsLoop_InTheBedsPlace()
        {
            AudioClip loop = Tone("Loop");
            AudioSource bed = StartBed(BankWith(MusicTrack.ObsidianVoid, loop), MusicTrack.ObsidianVoid);
            Assert.AreSame(loop, bed.clip);
            Assert.IsTrue(bed.loop);
        }

        [Test]
        public void WithoutALoop_TheSceneKeepsTheAmbientBed()
        {
            AudioSource bed = StartBed(BankWith(MusicTrack.ObsidianVoid, Tone("Loop")), MusicTrack.GoldenRuins);
            Assert.AreEqual("AmbientBed", bed.clip.name);
            Made(bed.clip);
        }

        [Test]
        public void TheNextScene_PicksUpTheLoop_OnlyIfItIsTheSameClip()
        {
            AudioClip loop = Tone("Loop");
            AudioCueBank bank = BankWith(MusicTrack.GoldenRuins, loop);
            SetResume(loop, 12000);
            Assert.AreEqual(12000, StartBed(bank, MusicTrack.GoldenRuins).timeSamples);
            SetResume(Tone("Other"), 12000);
            Assert.AreEqual(0, StartBed(bank, MusicTrack.GoldenRuins).timeSamples);
        }
    }
}
