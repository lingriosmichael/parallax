using System;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using NUnit.Framework;
using UnityEngine.SceneManagement;

namespace Parallax.Tests.EditMode
{
    // PAX-083 (items 3-4): a test that builds objects in the active scene must not leave it dirty, or the developer is
    // asked to save a scene after a test run (before PAX-075 R21 that scene was the leaked Level_004). Runs each named
    // test with its fixture's SetUp and TearDown, from a clean Test Runner scene, then checks the active scene.
    public sealed class SceneDirtyTests
    {
        [TestCase(typeof(LevelSelectLayoutTests), nameof(LevelSelectLayoutTests.BuildRowTemplate_ProducesARowWithAPositivePreferredHeight))]
        [TestCase(typeof(LevelSelectLayoutTests), nameof(LevelSelectLayoutTests.BuildRowTemplate_CalledTwice_IsIdempotent_SecondCallReportsNoChanges))]
        public void AfterASceneBuildingTest_TheActiveSceneIsClean(Type fixture, string test)
        {
            RecreateUntitledScene();
            try
            {
                Assert.IsFalse(SceneManager.GetActiveScene().isDirty, "precondition: a clean Test Runner scene");
                object instance = Activator.CreateInstance(fixture);
                Run<SetUpAttribute>(instance);
                try { Invoke(fixture.GetMethod(test), instance); }
                finally { Run<TearDownAttribute>(instance); }
                Scene active = SceneManager.GetActiveScene();
                Assert.IsFalse(active.isDirty, $"{fixture.Name}.{test} left the active scene '{(string.IsNullOrEmpty(active.path) ? "Untitled" : active.path)}' dirty.");
            }
            finally { RecreateUntitledScene(); }
        }

        // Public [SetUp]/[TearDown] only: no [OneTimeSetUp], inherited non-public setup or [TestCase] arguments, so list
        // parameterless tests only.
        static void Run<T>(object instance) where T : Attribute
        {
            foreach (MethodInfo m in instance.GetType().GetMethods().Where(m => m.GetCustomAttribute<T>() != null)) Invoke(m, instance);
        }

        static void Invoke(MethodInfo method, object instance)
        {
            try { method.Invoke(instance, null); }
            catch (TargetInvocationException e) { ExceptionDispatchInfo.Capture(e.InnerException).Throw(); }
        }

        static void RecreateUntitledScene() =>
            Type.GetType("Parallax.Editor.Routes.RouteSession, Parallax.Editor").GetMethod("RecreateUntitledScene").Invoke(null, new object[] { true });
    }
}
