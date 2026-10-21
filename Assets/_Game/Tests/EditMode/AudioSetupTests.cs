using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Audio;
using Parallax.Gameplay.Levels;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Parallax.Tests.EditMode
{
    /// <summary>PAX-064 (D-158): PARALLAX/Setup/Audio's wiring. AddTo is idempotent; the saved _LevelTemplate, Title and
    /// LevelSelect carry a wired GameAudio (so these fail until the developer runs the menu, then Rebuild All Levels); and a
    /// GameAudio woken in the template builds every cue and plays one. Restores the Test Runner's scene afterwards.</summary>
    public sealed class AudioSetupTests
    {
        const string TemplatePath = "Assets/_Game/Scenes/Levels/_LevelTemplate.unity";
        const string LevelSelectPath = "Assets/_Game/Scenes/" + LevelSceneLoader.LevelSelectSceneName + ".unity";
        const string RootName = "GameAudio";

        // The setup lives in Parallax.Editor, which this assembly reaches by reflection (as the other setup tests do).
        static void AddTo(Scene scene, AudioCueBank bank, List<string> changes) =>
            System.Type.GetType("Parallax.Editor.Setup.AudioSetup, Parallax.Editor").GetMethod("AddTo").Invoke(null, new object[] { scene, bank, changes });

        [TearDown]
        public void RestoreScene() => CatDeathKindTagTests.RecreateUntitledScene();

        [Test]
        public void AddTo_IsIdempotent_AndSkipsLevelEventsWithoutARoomManager()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var bank = ScriptableObject.CreateInstance<AudioCueBank>();
            try
            {
                var first = new List<string>();
                AddTo(scene, bank, first);
                Assert.IsNotEmpty(first);
                var second = new List<string>();
                AddTo(scene, bank, second);
                CollectionAssert.IsEmpty(second, "a second run changes nothing");

                GameObject root = GameObject.Find(RootName);
                Assert.IsNotNull(root);
                Assert.AreSame(bank, root.GetComponent<GameAudio>().Bank);
                Assert.IsNull(root.GetComponent<LevelAudioEvents>(), "no RoomManager, no level events");
            }
            finally { Object.DestroyImmediate(bank); }
        }

        [TestCase(TemplatePath, true)]
        [TestCase("Assets/_Game/Scenes/Title.unity", false)]
        [TestCase(LevelSelectPath, false)]
        public void TheSavedScene_CarriesAWiredGameAudio(string path, bool level)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(path) == null) Assert.Ignore(path + " doesn't exist yet.");
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            GameObject root = GameObject.Find(RootName);
            Assert.IsNotNull(root, path + " has no GameAudio root: run PARALLAX/Setup/Audio (PAX-064)");
            GameAudio gameAudio = root.GetComponent<GameAudio>();
            Assert.IsNotNull(gameAudio);
            Assert.IsNotNull(gameAudio.Bank, "the bank is assigned");
            LevelAudioEvents events = root.GetComponent<LevelAudioEvents>();
            if (!level) { Assert.IsNull(events); return; }
            Assert.IsNotNull(events);
            var serialized = new SerializedObject(events);
            foreach (string field in new[] { "gameAudio", "rooms", "death", "observers" })
                Assert.IsNotNull(serialized.FindProperty(field).objectReferenceValue, "LevelAudioEvents." + field + " is wired");
        }

        [Test]
        public void AWokenGameAudio_BuildsEveryCue_AndPlaysOne()
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(TemplatePath) == null) Assert.Ignore("no template");
            EditorSceneManager.OpenScene(TemplatePath, OpenSceneMode.Single);
            GameObject root = GameObject.Find(RootName);
            if (root == null) Assert.Inconclusive("run PARALLAX/Setup/Audio (PAX-064) first");
            GameAudio gameAudio = root.GetComponent<GameAudio>();
            typeof(GameAudio).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(gameAudio, null);
            var clips = (Dictionary<AudioCue, AudioClip>)typeof(GameAudio).GetField("clips", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(gameAudio);
            Assert.AreEqual(System.Enum.GetValues(typeof(AudioCue)).Length, clips.Count, "every cue has a clip");
            Assert.IsTrue(gameAudio.Play(AudioCue.TrapSpikes, 0f));
            Assert.IsFalse(gameAudio.Play(AudioCue.TrapSpikes, 0f), "the same cue again at once is held off (MinGap)");
            Assert.AreEqual(1, gameAudio.Plays);
            // 2026-10-20: a sound far off-screen (an arrow launcher two screens away) isn't played at all.
            var listener = (Transform)typeof(GameAudio).GetField("listener", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(gameAudio);
            Assert.IsFalse(gameAudio.Play(AudioCue.TrapArrow, listener.position.x + 40f), "out of earshot");
            Assert.IsTrue(gameAudio.Play(AudioCue.TrapArrow, listener.position.x + 2f), "near the camera");
        }
    }
}
