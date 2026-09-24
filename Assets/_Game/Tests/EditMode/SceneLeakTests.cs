using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Parallax.Tests.EditMode
{
    // PAX-075 §11 R21: the scene-reading tests open level scenes in Single mode and restore the Test
    // Runner's setup afterwards. When that setup is only the Runner's untitled scene (an empty list), they
    // used to return early and leave the last level loaded (and, once, dirty), which route sessions then
    // refused. After each such test, no level scene may still be loaded.
    public sealed class SceneLeakTests
    {
        [TestCase(typeof(LevelSceneTimingTests), nameof(LevelSceneTimingTests.EachListedLevelScene_TrapTickSettings_EqualTheirLayouts))]
        [TestCase(typeof(LevelSceneTriggerTests), nameof(LevelSceneTriggerTests.EachListedLevelScene_TrapTriggerBoxes_EqualTheirLayouts))]
        [TestCase(typeof(LevelSceneTests), nameof(LevelSceneTests.SceneReadBounds_EachLevelScenesBakedRoomManagerBounds_EqualsLevelSolo01RoomNTranslated))]
        [TestCase(typeof(LevelSceneTests), nameof(LevelSceneTests.EachListedLevelScene_HasLevelCameraOnCameraA_WiredToItsOwnRoomDeath))]
        [TestCase(typeof(LevelSceneTests), nameof(LevelSceneTests.EachListedLevelScene_HasALevelsButton_WiredIntoLevelCompleteScreenAndReservedRegions))]
        [TestCase(typeof(LevelSceneTests), nameof(LevelSceneTests.EachListedLevelScene_HasAPauseMenu_WiredIntoGateCompleteScreenAndReservedRegions))]
        public void AfterASceneReadingTest_NoLevelSceneIsStillLoaded(Type fixture, string test)
        {
            var leaked = new List<string>();
            try
            {
                MethodInfo method = fixture.GetMethod(test, BindingFlags.Public | BindingFlags.Instance);
                Assert.NotNull(method, fixture.Name + "." + test + " not found.");
                try { method.Invoke(Activator.CreateInstance(fixture), null); }
                catch (TargetInvocationException e) { ExceptionDispatchInfo.Capture(e.InnerException).Throw(); }
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    Scene s = SceneManager.GetSceneAt(i);
                    if (s.path.StartsWith("Assets/_Game/Scenes/", StringComparison.Ordinal)) leaked.Add($"{s.path} (dirty {s.isDirty})");
                }
            }
            finally
            {
                // This test must not leak either: put back a clean Test Runner scene if anything was left.
                if (leaked.Count > 0) EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            }
            Assert.IsEmpty(leaked, $"{fixture.Name}.{test} left these scenes loaded");
        }
    }
}
