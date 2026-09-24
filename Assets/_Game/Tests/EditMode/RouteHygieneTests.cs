using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using static Parallax.Tests.EditMode.RouteTestApi;

namespace Parallax.Tests.EditMode
{
    // PAX-075 (D-079) §5.6, R1/R13: a route session leaves the open scenes, physics simulation mode, the
    // log handler, TickTime and RunningState exactly as it found them, including after a failed assert,
    // and refuses to start over a dirty scene.
    public sealed class RouteHygieneTests
    {
        static string Fingerprint() => (string)Call(T("RouteSession"), "GlobalStateFingerprint");

        [SetUp] public void FreshScene() => FreshScratchScene();

        [Test]
        public void AfterAFailedAssertInsideASession_GlobalStateAndTheOpenScenesAreRestored()
        {
            string before = Fingerprint();
            int ticks = 0;
            Assert.Throws<AssertionException>(() =>
            {
                using IDisposable session = OpenSession();
                ticks = Records(Replay(session, Case("FlatRun"))).Count - 1;
                Assert.AreNotEqual(Fingerprint(), before, "inside a session the state is swapped");
                Assert.Fail("forced failure inside a session");
            });
            Assert.Greater(ticks, 0, "the replay inside the session ran");
            Assert.AreEqual(before, Fingerprint(), "scene setup, dirty flags, simulation mode, log handler, TickTime, RunningState, time scale");
            for (int i = 0; i < SceneManager.sceneCount; i++) Assert.IsFalse(SceneManager.GetSceneAt(i).isDirty, SceneManager.GetSceneAt(i).path + " is dirty");
        }

        [Test]
        public void LogFilter_DropsPlainLogsOnly_AndIsRemovedAfterwards()
        {
            ILogHandler original = Debug.unityLogger.logHandler;
            var probe = new Probe(original);
            Debug.unityLogger.logHandler = probe;
            try
            {
                using (OpenSession())
                {
                    Debug.Log("route-hygiene plain log");
                    Debug.LogWarning("route-hygiene warning");
                }
                Assert.AreSame(probe, Debug.unityLogger.logHandler, "the session restores the handler it found");
            }
            finally { Debug.unityLogger.logHandler = original; }
            LogType[] seen = probe.Types.Where((t, i) => probe.Messages[i].StartsWith("route-hygiene")).ToArray();
            CollectionAssert.AreEqual(new[] { LogType.Warning }, seen, "inside the session a plain log is dropped and a warning is forwarded");
        }

        [Test]
        public void RefusesADirtyScene_NamingIt_BeforeTouchingAnything()
        {
            const string path = "Assets/_Game/Scenes/Levels/_LevelTemplate.unity";
            string before = Fingerprint();
            Scene extra = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try
            {
                EditorSceneManager.MarkSceneDirty(extra);
                Assert.IsTrue(extra.isDirty);
                string simulation = Physics2D.simulationMode.ToString();
                var e = Assert.Throws<InvalidOperationException>(() => OpenSession().Dispose());
                StringAssert.Contains(path, e.Message);
                Assert.AreEqual(simulation, Physics2D.simulationMode.ToString(), "nothing was touched");
            }
            finally { EditorSceneManager.CloseScene(extra, true); }
            Assert.AreEqual(before, Fingerprint());
        }

        // R23: the recreate-untitled path lives only in the test helper. RouteSession itself refuses a dirty
        // untitled scene, since outside the Test Runner it may be the developer's unsaved work.
        [Test]
        public void RefusesADirtyUntitledScene_WithoutTheTestHelper()
        {
            Call(T("RouteSession"), "RecreateUntitledScene", true);
            object created = null;
            try
            {
                Scene scratch = SceneManager.GetActiveScene();
                EditorSceneManager.MarkSceneDirty(scratch);
                Assert.IsTrue(scratch.isDirty && string.IsNullOrEmpty(scratch.path));
                string simulation = Physics2D.simulationMode.ToString();
                var e = Assert.Throws<System.Reflection.TargetInvocationException>(() => created = Activator.CreateInstance(T("RouteSession")));
                Assert.IsInstanceOf<InvalidOperationException>(e.InnerException);
                StringAssert.Contains("'Untitled'", e.InnerException.Message);
                Assert.AreEqual(simulation, Physics2D.simulationMode.ToString(), "nothing was touched");
            }
            finally
            {
                (created as IDisposable)?.Dispose();   // a guard that failed to refuse still restores what it swapped
                Call(T("RouteSession"), "RecreateUntitledScene", true);
            }
        }

        sealed class Probe : ILogHandler
        {
            readonly ILogHandler inner;
            public readonly List<LogType> Types = new();
            public readonly List<string> Messages = new();
            public Probe(ILogHandler inner) => this.inner = inner;
            public void LogFormat(LogType logType, Object context, string format, params object[] args)
            {
                Types.Add(logType);
                Messages.Add(args != null && args.Length > 0 ? string.Format(format, args) : format);
                inner.LogFormat(logType, context, format, args);
            }
            public void LogException(Exception exception, Object context) => inner.LogException(exception, context);
        }
    }
}
