using System.Collections.Generic;
using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Audio;
using Parallax.Gameplay.Levels;
using Parallax.Gameplay.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Parallax.Tests.EditMode
{
    /// <summary>PAX-065 (D-278): events to cues, the minimum gap, off means no calls, the patterns are valid, the setting saves, the pause
    /// panel's Vibration button flips it, and the saved template is wired (inconclusive until PARALLAX/Setup/Haptics has run).</summary>
    public sealed class HapticsTests
    {
        const string SettingKey = "PARALLAX_HAPTICS_ENABLED";
        const string TemplatePath = "Assets/_Game/Scenes/Levels/_LevelTemplate.unity";

        sealed class FakeHaptics : IHaptics
        {
            public readonly List<HapticCue> Played = new List<HapticCue>();
            public void Play(HapticCue cue) => Played.Add(cue);
        }

        readonly FakeHaptics fake = new FakeHaptics();
        bool hadSetting;
        int savedSetting;

        HapticDispatcher Dispatcher(bool on = true) => new HapticDispatcher(fake, () => on, 0.25f, 9f);

        [SetUp]
        public void SaveSetting()
        {
            fake.Played.Clear();
            hadSetting = PlayerPrefs.HasKey(SettingKey);
            savedSetting = PlayerPrefs.GetInt(SettingKey, 1);
            PlayerPrefs.DeleteKey(SettingKey);
        }

        [TearDown]
        public void RestoreSetting()
        {
            if (hadSetting) PlayerPrefs.SetInt(SettingKey, savedSetting);
            else PlayerPrefs.DeleteKey(SettingKey);
            CatDeathKindTagTests.RecreateUntitledScene();
        }

        [Test]
        public void EachEvent_PlaysItsCue()
        {
            HapticDispatcher d = Dispatcher();
            d.TrapFired(2f, 0f);
            d.Died(1f);
            d.LevelCompleted(2f);
            CollectionAssert.AreEqual(new[] { HapticCue.TrapFire, HapticCue.Death, HapticCue.LevelComplete }, fake.Played);
        }

        [Test]
        public void ATrapFarFromTheCat_IsNotFelt()
        {
            HapticDispatcher d = Dispatcher();
            d.TrapFired(9.5f, 0f);
            CollectionAssert.IsEmpty(fake.Played);
            d.TrapFired(9f, 0f);
            Assert.AreEqual(1, fake.Played.Count, "the edge counts");
        }

        [Test]
        public void TrapsWithinTheGap_AreSkipped()
        {
            HapticDispatcher d = Dispatcher();
            d.TrapFired(1f, 0f);
            d.TrapFired(1f, 0.24f);
            Assert.AreEqual(1, fake.Played.Count);
            d.TrapFired(1f, 0.25f);
            Assert.AreEqual(2, fake.Played.Count);
        }

        [Test]
        public void ADeath_IsAlwaysFelt_EvenRightAfterTheTrapThatKilled()
        {
            HapticDispatcher d = Dispatcher();
            d.TrapFired(0f, 1f);
            d.Died(1f);
            CollectionAssert.AreEqual(new[] { HapticCue.TrapFire, HapticCue.Death }, fake.Played);
        }

        [Test]
        public void Off_MakesNoCalls_AndHoldsNoGap()
        {
            bool on = false;
            var d = new HapticDispatcher(fake, () => on, 0.25f, 9f);
            d.TrapFired(1f, 0f);
            d.Died(0f);
            d.LevelCompleted(0f);
            CollectionAssert.IsEmpty(fake.Played);
            on = true;
            d.TrapFired(1f, 0.01f);
            Assert.AreEqual(1, fake.Played.Count, "cues skipped while off don't start the gap");
        }

        [Test]
        public void EveryCue_HasAValidPattern_OfTheSameLengths()
        {
            HapticsConfig config = ScriptableObject.CreateInstance<HapticsConfig>();
            try
            {
                foreach (HapticCue cue in System.Enum.GetValues(typeof(HapticCue)))
                {
                    Assert.IsTrue(config.TryGetPattern(cue, out HapticsConfig.Pattern p), cue.ToString());
                    Assert.IsNotEmpty(p.Millis, cue.ToString());
                    Assert.AreEqual(p.Millis.Length, p.Amplitudes.Length, cue.ToString());
                    foreach (int ms in p.Millis) Assert.Greater(ms, 0, cue.ToString());
                    foreach (int amplitude in p.Amplitudes) Assert.That(amplitude, Is.InRange(0, 255), cue.ToString());
                }
            }
            finally { Object.DestroyImmediate(config); }
        }

        [Test]
        public void TheSetting_IsOnByDefault_AndSaves()
        {
            Assert.IsTrue(LevelProgressStore.HapticsEnabled);
            LevelProgressStore.HapticsEnabled = false;
            Assert.IsFalse(LevelProgressStore.HapticsEnabled);
            LevelProgressStore.HapticsEnabled = true;
            Assert.IsTrue(LevelProgressStore.HapticsEnabled);
        }

        [Test]
        public void TheVibrationButton_FlipsTheSetting_AndItsLabel()
        {
            var go = new GameObject("Panel", typeof(RectTransform));
            try
            {
                PausePanel panel = go.AddComponent<PausePanel>();
                Text label = new GameObject("Label").AddComponent<Text>();
                label.transform.SetParent(go.transform);
                var serialized = new SerializedObject(panel);
                serialized.FindProperty("vibrationLabel").objectReferenceValue = label;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                panel.ToggleVibration();
                Assert.IsFalse(LevelProgressStore.HapticsEnabled);
                Assert.AreEqual("Vibration: Off", label.text);
                panel.ToggleVibration();
                Assert.IsTrue(LevelProgressStore.HapticsEnabled);
                Assert.AreEqual("Vibration: On", label.text);
            }
            finally { Object.DestroyImmediate(go); }
        }

        [Test]
        public void TheSavedTemplate_IsWired()
        {
            EditorSceneManager.OpenScene(TemplatePath, OpenSceneMode.Single);
            GameObject root = GameObject.Find("GameAudio");
            LevelHaptics haptics = root != null ? root.GetComponent<LevelHaptics>() : null;
            if (haptics == null) Assert.Inconclusive("run PARALLAX/Setup/Haptics (PAX-065) first");
            var serialized = new SerializedObject(haptics);
            foreach (string field in new[] { "config", "rooms", "death", "observers" })
                Assert.IsNotNull(serialized.FindProperty(field).objectReferenceValue, "LevelHaptics." + field + " is wired");
            PausePanel panel = Object.FindAnyObjectByType<PausePanel>(FindObjectsInactive.Include);
            Assert.IsNotNull(new SerializedObject(panel).FindProperty("vibrationButton").objectReferenceValue, "the pause panel has its Vibration button");
        }
    }
}
