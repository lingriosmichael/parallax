using System;
using Parallax.Core;
using Parallax.Gameplay.Levels;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Parallax.Editor.Routes
{
    // PAX-075 (D-079, R1/R13): the only place the route harness touches global state. Traps, hazards and
    // the door query the default physics scene (static Physics2D.OverlapBox), so replays can't run in an
    // isolated physics scene; instead the open scenes are swapped for one empty scene for the session and
    // restored from disk in Dispose. Refuses before touching anything if a loaded scene is dirty, because
    // the swap would discard it. Also owns Physics2D.simulationMode (Script while open)
    // and a log filter that drops LogType.Log only. TickTime and RunningState are never written.
    public sealed class RouteSession : IDisposable
    {
        readonly SceneSetup[] setup;
        readonly SimulationMode2D simulationMode;
        readonly ILogHandler logHandler;
        readonly bool untitledHadObjects;
        bool disposed;

        public Scene Scene { get; private set; }

        public RouteSession()
        {
            // The Test Runner runs EditMode tests in its own clean untitled scene, so a clean untitled scene
            // is allowed: it holds nothing unsaved, and Restore recreates it. Anything dirty is refused.
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene loaded = SceneManager.GetSceneAt(i);
                if (loaded.isLoaded && loaded.isDirty)
                    throw new InvalidOperationException($"RouteSession: scene '{(string.IsNullOrEmpty(loaded.path) ? "Untitled" : loaded.path)}' has unsaved changes; save or discard them before running route replays.");
            }

            setup = EditorSceneManager.GetSceneManagerSetup();
            Scene active = SceneManager.GetActiveScene();
            untitledHadObjects = string.IsNullOrEmpty(active.path) && active.rootCount > 0;
            simulationMode = Physics2D.simulationMode;
            logHandler = Debug.unityLogger.logHandler;
            try
            {
                Scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                Physics2D.simulationMode = SimulationMode2D.Script;
                Debug.unityLogger.logHandler = new DropInfoLogs(logHandler);
            }
            catch
            {
                Restore();
                throw;
            }
        }

        public void Clear()
        {
            if (!Scene.IsValid() || !Scene.isLoaded) return;
            foreach (GameObject root in Scene.GetRootGameObjects()) Object.DestroyImmediate(root);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            Restore();
        }

        void Restore()
        {
            try { Clear(); }
            finally
            {
                Debug.unityLogger.logHandler = logHandler;
                Physics2D.simulationMode = simulationMode;
                // Saved scenes come back from disk. GetSceneManagerSetup lists no untitled scene, so a clean
                // untitled scene (the Test Runner's) is recreated as a fresh one, with its default objects if it had any.
                // PAX-083 (PAX-075 nit 2): with saved scenes open too, a clean untitled scene is dropped rather than
                // recreated. Nothing is lost: it was clean (the constructor refuses a dirty one), and outside the Test
                // Runner an untitled scene beside saved ones only exists while the developer is setting scenes up.
                SceneSetup[] saved = setup == null ? Array.Empty<SceneSetup>() : Array.FindAll(setup, s => !string.IsNullOrEmpty(s.path));
                if (saved.Length == 0) RecreateUntitledScene(untitledHadObjects);
                else
                {
                    if (!Array.Exists(saved, s => s.isActive)) saved[0].isActive = true;
                    EditorSceneManager.RestoreSceneManagerSetup(saved);
                }
            }
        }

        // R20/R21: the Test Runner's clean untitled scene, as it had it (Main Camera and light by default).
        public static void RecreateUntitledScene(bool withDefaultObjects = true) =>
            EditorSceneManager.NewScene(withDefaultObjects ? NewSceneSetup.DefaultGameObjects : NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // R13: replays log "Room 0 complete", respawns and PARALLAX_STATS on every run; only plain logs
        // are dropped. Warnings, errors, asserts and exceptions are forwarded unchanged.
        sealed class DropInfoLogs : ILogHandler
        {
            readonly ILogHandler inner;
            public DropInfoLogs(ILogHandler inner) => this.inner = inner;
            public void LogFormat(LogType logType, Object context, string format, params object[] args)
            {
                if (logType == LogType.Log) return;
                inner.LogFormat(logType, context, format, args);
            }
            public void LogException(Exception exception, Object context) => inner.LogException(exception, context);
        }

        // For the hygiene test: everything the session must leave as it found it. PAX-083 (PAX-075 nit 1): objects and
        // delegates by identity (method and target), not by GetHashCode, and every loaded scene's roots, so a
        // recreated untitled scene (which GetSceneManagerSetup doesn't list) is compared too.
        public static string GlobalStateFingerprint()
        {
            var parts = new System.Collections.Generic.List<string>();
            foreach (SceneSetup s in EditorSceneManager.GetSceneManagerSetup()) parts.Add($"{s.path}|{s.isActive}|{s.isLoaded}");
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                string roots = scene.isLoaded ? string.Join(",", Array.ConvertAll(scene.GetRootGameObjects(), g => g.name)) : "-";
                parts.Add($"scene:{(string.IsNullOrEmpty(scene.path) ? "Untitled" : scene.path)}|dirty:{scene.isDirty}|roots:{roots}");
            }
            parts.Add("sim:" + Physics2D.simulationMode);
            parts.Add("log:" + Identity(Debug.unityLogger.logHandler));
            parts.Add("tick:" + Identity(TickTime.SecondsPerTickSource) + ":" + TickTime.SecondsPerTick.ToString("R"));
            parts.Add("run:" + Identity(RunningState.SetTimeScale));
            parts.Add("scale:" + Time.timeScale.ToString("R"));
            parts.Add("fixed:" + Time.fixedDeltaTime.ToString("R"));
            return string.Join("; ", parts);
        }

        // A delegate is its method plus its target; an object is its type plus an id that only reference equality
        // shares. The ids hold on to what they've seen (a few handlers and delegates per Editor session).
        static readonly System.Collections.Generic.List<object> seen = new();
        static string Identity(object value)
        {
            if (value == null) return "null";
            if (value is Delegate d) return $"{d.Method.DeclaringType?.FullName}.{d.Method.Name}@{(d.Target == null ? "static" : Identity(d.Target))}";
            int id = seen.FindIndex(o => ReferenceEquals(o, value));
            if (id < 0) { seen.Add(value); id = seen.Count - 1; }
            return $"{value.GetType().FullName}#{id}";
        }
    }
}
