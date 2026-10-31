using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Levels;
using Parallax.Gameplay.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Parallax.Tests.EditMode
{
    /// <summary>PAX-108 (D-157 amendment 1): the title's START GAME. Clear erases the progress, so the title offers PLAY at
    /// the first level again. With progress, START GAME asks first and its confirm loads the first level; with none it is
    /// hidden and LEVELS takes its place. Uses its own level ids in PlayerPrefs and erases them afterwards.</summary>
    public sealed class TitleStartGameTests
    {
        static readonly string[] Ids = { "TitleStart_A", "TitleStart_B", "TitleStart_C" };

        readonly List<Object> made = new List<Object>();
        System.Action<string> originalLoad;
        System.Func<string, bool> originalCanLoad;
        System.Action<float> originalSetTimeScale;
        readonly List<string> loaded = new List<string>();

        [SetUp]
        public void SwapLoader()
        {
            originalLoad = LevelSceneLoader.LoadScene;
            originalCanLoad = LevelSceneLoader.CanLoad;
            LevelSceneLoader.LoadScene = loaded.Add;
            LevelSceneLoader.CanLoad = _ => true;
            originalSetTimeScale = RunningState.SetTimeScale;
            RunningState.SetTimeScale = _ => { };   // D-073: tests never write the real Time.timeScale
            loaded.Clear();
        }

        [TearDown]
        public void Restore()
        {
            LevelSceneLoader.LoadScene = originalLoad;
            LevelSceneLoader.CanLoad = originalCanLoad;
            RunningState.SetTimeScale = originalSetTimeScale;
            LevelProgressStore.Clear(Ids);
            foreach (Object o in made) if (o != null) Object.DestroyImmediate(o);
            made.Clear();
        }

        static void SaveProgress()
        {
            var progress = new LevelProgress(Ids);
            progress.RecordCompletion(Ids[0], 2);
            progress.RecordCompletion(Ids[1], 5);
            LevelProgressStore.Save(progress, Ids);
        }

        [Test]
        public void Clear_ErasesTheProgress_SoTheTitleStartsAtTheFirstLevel()
        {
            SaveProgress();
            Assert.IsTrue(TitleContinue.HasProgress(Ids, LevelProgressStore.Load(Ids)), "precondition");
            Assert.AreEqual(Ids[2], TitleContinue.Target(Ids, LevelProgressStore.Load(Ids)));

            LevelProgressStore.Clear(Ids);
            LevelProgress fresh = LevelProgressStore.Load(Ids);
            Assert.IsFalse(TitleContinue.HasProgress(Ids, fresh));
            Assert.AreEqual(Ids[0], TitleContinue.Target(Ids, fresh));
            Assert.IsTrue(fresh.IsUnlocked(Ids[0]));
            Assert.IsFalse(fresh.IsUnlocked(Ids[1]), "unlocks are erased too");
            Assert.AreEqual(LevelProgress.NeverCompleted, fresh.BestDeaths(Ids[0]));
        }

        [Test]
        public void WithProgress_StartGameAsksFirst_ThenErasesAndLoadsTheFirstLevel()
        {
            SaveProgress();
            (TitleScreen screen, Button start, _, GameObject confirm) = Build();
            Assert.IsTrue(start.gameObject.activeSelf, "START GAME shows once there is progress");

            screen.StartGame();
            Assert.IsTrue(confirm.activeSelf, "it asks first");
            CollectionAssert.IsEmpty(loaded);
            Assert.IsTrue(TitleContinue.HasProgress(Ids, LevelProgressStore.Load(Ids)), "nothing erased yet");

            screen.CancelStartGame();
            Assert.IsFalse(confirm.activeSelf);
            Assert.IsTrue(TitleContinue.HasProgress(Ids, LevelProgressStore.Load(Ids)), "cancel keeps the progress");

            screen.StartGame();
            screen.ConfirmStartGame();
            CollectionAssert.AreEqual(new[] { "Scene_A" }, loaded, "the first level loads");
            Assert.IsFalse(TitleContinue.HasProgress(Ids, LevelProgressStore.Load(Ids)), "the progress is gone");
        }

        [Test]
        public void WithoutProgress_StartGameIsHidden_AndTheRowClosesUp()
        {
            LevelProgressStore.Clear(Ids);
            (TitleScreen screen, Button start, Button levels, _) = Build();
            Button play = (Button)typeof(TitleScreen).GetField("playButton", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(screen);
            Assert.IsFalse(start.gameObject.activeSelf);
            // D-280: the buttons are one row; with START GAME gone, PLAY and LEVELS close up, centred.
            Assert.AreEqual(new Vector2(200f, 0f), ((RectTransform)levels.transform).anchoredPosition);
            Assert.AreEqual(new Vector2(-200f, 0f), ((RectTransform)play.transform).anchoredPosition);
        }

        (TitleScreen, Button, Button, GameObject) Build()
        {
            var list = ScriptableObject.CreateInstance<LevelListConfig>();
            made.Add(list);
            typeof(LevelListConfig).GetField("levels", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(list, new[]
            {
                new LevelEntry { Id = Ids[0], SceneName = "Scene_A" },
                new LevelEntry { Id = Ids[1], SceneName = "Scene_B" },
                new LevelEntry { Id = Ids[2], SceneName = "Scene_C" },
            });
            var root = new GameObject("Title", typeof(RectTransform));
            made.Add(root);
            Button play = NewButton(root.transform, "Play", Vector2.zero);
            Button start = NewButton(root.transform, "StartGame", new Vector2(-400f, 0f));
            Button levels = NewButton(root.transform, "Levels", new Vector2(400f, 0f));
            var confirm = new GameObject("Confirm");
            confirm.transform.SetParent(root.transform);
            var screen = root.AddComponent<TitleScreen>();
            Set(screen, "levelList", list);
            Set(screen, "playButton", play);
            Set(screen, "startButton", start);
            Set(screen, "levelsButton", levels);
            Set(screen, "confirmPanel", confirm);
            LogAssertNoConfig();
            typeof(TitleScreen).GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(screen, null);
            return (screen, start, levels, confirm);
        }

        // Start logs that the intro has no TitleScreenConfig; the menu logic under test doesn't need one.
        static void LogAssertNoConfig() =>
            UnityEngine.TestTools.LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("no TitleScreenConfig"));

        static Button NewButton(Transform parent, string name, Vector2 at)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            ((RectTransform)go.transform).anchoredPosition = at;
            return go.AddComponent<Button>();
        }

        static void Set(object target, string field, object value) =>
            typeof(TitleScreen).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
