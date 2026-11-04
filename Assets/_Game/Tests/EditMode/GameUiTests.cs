using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Parallax.Tests.EditMode
{
    /// <summary>PAX-A09: the in-game UI's rules: the progress line's marks, what a level cell shows, the stick's knob, the safe area, and a play HUD that
    /// takes no touches. The look itself is judged in the captures.</summary>
    public sealed class GameUiTests
    {
        static readonly Type SetupType = Type.GetType("Parallax.Editor.Setup.GameUiSetup, Parallax.Editor");

        GameObject root;

        [SetUp]
        public void OpenScene() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        [TearDown]
        public void Cleanup()
        {
            if (root != null) Object.DestroyImmediate(root);
            Type.GetType("Parallax.Editor.Routes.RouteSession, Parallax.Editor").GetMethod("RecreateUntitledScene").Invoke(null, new object[] { true });
        }

        static List<string> BuildHud(Transform hud)
        {
            Assert.NotNull(SetupType, "Parallax.Editor.Setup.GameUiSetup not found.");
            var changes = new List<string>();
            SetupType.GetMethod("BuildPlayHud", BindingFlags.Public | BindingFlags.Static).Invoke(null, new object[] { hud, changes });
            return changes;
        }

        [TestCase(0, 3, 0f)]
        [TestCase(1, 3, 1f / 3f)]
        [TestCase(2, 3, 2f / 3f)]
        [TestCase(0, 0, 0f)]
        [TestCase(9, 3, 1f)]
        public void LineFraction_LeavesTheLastStretchForTheDoor(int section, int count, float expected) =>
            Assert.AreEqual(expected, SectionProgress.LineFraction(section, count), 1e-5f);

        [Test]
        public void CellState_PaywallWinsThenReachThenCompletion()
        {
            Assert.AreEqual(LevelCellState.Paywalled, new LevelSelectEntry("a", "a", "a", true, "2", true, true).State);
            Assert.AreEqual(LevelCellState.Locked, new LevelSelectEntry("a", "a", "a", false, "—").State);
            Assert.AreEqual(LevelCellState.Done, new LevelSelectEntry("a", "a", "a", true, "2", false, true).State);
            Assert.AreEqual(LevelCellState.Open, new LevelSelectEntry("a", "a", "a", true, "—").State);
        }

        [Test]
        public void Model_MarksACompletedLevelDone()
        {
            var progress = new LevelProgress(new[] { "L001", "L002" });
            progress.RecordCompletion("L001", 3);
            LevelSelectEntry[] entries = LevelSelectModel.Build(new (string, string, string)[] { ("L001", "a", "1"), ("L002", "b", "2") }, progress);
            Assert.AreEqual(LevelCellState.Done, entries[0].State);
            Assert.AreEqual(LevelCellState.Open, entries[1].State);
        }

        [Test]
        public void KnobOffset_FollowsTheThumbAndStopsAtTheRing()
        {
            Assert.AreEqual(new Vector2(25f, 0f), TouchControlsView.KnobOffset(new Vector2(100f, 100f), new Vector2(150f, 100f), 200f, 2f));
            Assert.AreEqual(new Vector2(100f, 0f), TouchControlsView.KnobOffset(new Vector2(100f, 100f), new Vector2(900f, 100f), 200f, 2f));
            Assert.AreEqual(Vector2.zero, TouchControlsView.KnobOffset(Vector2.zero, Vector2.one, 200f, 0f), "no canvas scale yet");
        }

        [Test]
        public void SafeArea_BecomesAnchors()
        {
            (Vector2 min, Vector2 max) = SafeAreaFit.Anchors(new Rect(120f, 0f, 2280f, 1080f), new Vector2(2400f, 1080f));
            Assert.AreEqual(new Vector2(0.05f, 0f), min);
            Assert.AreEqual(Vector2.one, max);
            Assert.AreEqual((Vector2.zero, Vector2.one), SafeAreaFit.Anchors(new Rect(0f, 0f, 0f, 0f), Vector2.zero));
        }

        // The stick and jump are TouchStickCatInput's zones, and a HUD button is a reserved region (D-023, D-073). The play HUD is neither: a graphic that
        // took a tap would eat a touch the zones never see in the Event System's click, and it would never be in reservedRegions.
        [Test]
        public void PlayHud_TakesNoTouches_AndIsIdempotent()
        {
            root = new GameObject("HUD", typeof(RectTransform));
            Assert.Greater(BuildHud(root.transform).Count, 0);

            foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true))
                Assert.IsFalse(graphic.raycastTarget, graphic.name + " must not take a touch");
            foreach (CanvasGroup group in root.GetComponentsInChildren<CanvasGroup>(true))
                Assert.IsFalse(group.blocksRaycasts, group.name + " must not block a touch");
            Assert.AreEqual(0, root.transform.Find("PlayHud").GetSiblingIndex(), "the play HUD goes under every panel");

            CollectionAssert.IsEmpty(BuildHud(root.transform), "a second build must report no changes");
        }

        [Test]
        public void PlayHud_StartsUnderPanelsAlreadyThere()
        {
            root = new GameObject("HUD", typeof(RectTransform));
            new GameObject("PausePanel", typeof(RectTransform)).transform.SetParent(root.transform, false);
            BuildHud(root.transform);
            Assert.AreEqual(0, root.transform.Find("PlayHud").GetSiblingIndex());
        }
    }
}
