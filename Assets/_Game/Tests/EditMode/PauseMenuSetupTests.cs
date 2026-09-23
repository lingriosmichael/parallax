using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Parallax.Gameplay.UI;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Parallax.Tests.EditMode
{
    /// <summary>PAX-054 (D-073): PauseMenuSetup.BuildPauseUI, the public seam the Pause Menu menu
    /// builds the pause UI through (same approach as PAX-053's LevelSelectSetup.BuildRowTemplate).
    /// Called by reflection on a throwaway HUD, since the test assembly doesn't reference
    /// Parallax.Editor. Pause UI buttons must never be selectable (Navigation None, as on the
    /// complete screen), so keyboard/gamepad Submit and Navigate can't reach them.</summary>
    public sealed class PauseMenuSetupTests
    {
        static readonly Type SetupType = Type.GetType("Parallax.Editor.Setup.PauseMenuSetup, Parallax.Editor");
        static readonly Type ButtonType = Type.GetType("UnityEngine.UI.Button, UnityEngine.UI");

        static readonly string[] ButtonPaths =
        {
            "PauseButton", "PausePanel/PauseResumeButton", "PausePanel/PauseRestartButton", "PausePanel/PauseLevelsButton",
        };

        static List<string> Build(Transform hud)
        {
            Assert.NotNull(SetupType, "Parallax.Editor.Setup.PauseMenuSetup not found.");
            MethodInfo build = SetupType.GetMethod("BuildPauseUI", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(build, "PauseMenuSetup.BuildPauseUI not found.");
            var changes = new List<string>();
            build.Invoke(null, new object[] { hud, changes });
            return changes;
        }

        [Test]
        public void BuildPauseUI_BuildsFourButtonsWithNavigationNone_WiredIntoPauseButtonAndPanel()
        {
            var hud = new GameObject("HUD", typeof(RectTransform));
            try
            {
                Build(hud.transform);

                foreach (string path in ButtonPaths)
                {
                    Transform t = hud.transform.Find(path);
                    Assert.NotNull(t, $"BuildPauseUI must create HUD/{path}.");
                    Component button = t.GetComponent(ButtonType);
                    Assert.NotNull(button, $"HUD/{path} has no Button.");
                    int mode = new SerializedObject(button).FindProperty("m_Navigation.m_Mode").intValue;
                    Assert.AreEqual(0, mode, $"HUD/{path} must use Navigation None, so a click never selects it.");
                }

                var pauseButton = hud.transform.Find("PauseButton").GetComponent<PauseButton>();
                Assert.NotNull(pauseButton, "HUD/PauseButton has no PauseButton component.");
                Assert.AreEqual(hud.transform.Find("PauseButton").GetComponent(ButtonType), PauseTestRig.GetPrivate<Object>(pauseButton, "button"));

                Transform panelT = hud.transform.Find("PausePanel");
                var panel = panelT.GetComponent<PausePanel>();
                Assert.NotNull(panel, "HUD/PausePanel has no PausePanel component.");
                Assert.IsFalse(panelT.gameObject.activeSelf, "the pause panel must start hidden");
                var panelRect = (RectTransform)panelT;
                Assert.AreEqual(Vector2.zero, panelRect.anchorMin, "the panel must stretch over the whole screen");
                Assert.AreEqual(Vector2.one, panelRect.anchorMax, "the panel must stretch over the whole screen");
                Assert.AreEqual(Vector2.zero, panelRect.offsetMin);
                Assert.AreEqual(Vector2.zero, panelRect.offsetMax);
                Assert.AreEqual(panelT.Find("PauseResumeButton").GetComponent(ButtonType), PauseTestRig.GetPrivate<Object>(panel, "resumeButton"));
                Assert.AreEqual(panelT.Find("PauseRestartButton").GetComponent(ButtonType), PauseTestRig.GetPrivate<Object>(panel, "restartButton"));
                Assert.AreEqual(panelT.Find("PauseLevelsButton").GetComponent(ButtonType), PauseTestRig.GetPrivate<Object>(panel, "levelsButton"));
                Assert.IsNull(panelT.GetComponentInChildren<LevelsButton>(true), "the pause panel must not reuse LevelsButton (it records progress)");

                var buttonRect = (RectTransform)hud.transform.Find("PauseButton");
                Assert.AreEqual(Vector2.one, buttonRect.anchorMin, "the pause button must sit in the top-right corner");
                Assert.AreEqual(Vector2.one, buttonRect.anchorMax, "the pause button must sit in the top-right corner");

                List<string> second = Build(hud.transform);
                CollectionAssert.IsEmpty(second, "a second BuildPauseUI must report no changes (idempotent)");
            }
            finally { Object.DestroyImmediate(hud); }
        }
    }
}
