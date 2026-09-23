using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Parallax.Gameplay.Levels
{
    /// <summary>PAX-053 (D-072) §2.2: the single place every level/menu scene change goes
    /// through (Restart, Next level, Levels, and loading from LevelSelect). Loads by scene name
    /// with SceneManager and nothing else - no Editor-only fallback. CanLoad/LoadScene are public
    /// so tests can swap in a fake without a real scene load; production code never reassigns
    /// them, so the defaults (Application.CanStreamedLevelBeLoaded, SceneManager.LoadScene) are
    /// always what actually runs outside tests.</summary>
    public static class LevelSceneLoader
    {
        // The one place the level select scene's name is spelled out; BuildSceneList (Editor) and
        // LevelSelectSetup (Editor) both reference this instead of repeating the literal.
        public const string LevelSelectSceneName = "LevelSelect";

        public static Func<string, bool> CanLoad = Application.CanStreamedLevelBeLoaded;
        public static Action<string> LoadScene = SceneManager.LoadScene;

        // A test that crashes before its own teardown restores these would otherwise leave a fake
        // installed into whatever runs next (including, with domain reload disabled, a Play mode
        // session) - reset to the real production defaults on every domain load/Play mode entry.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetToProductionDefaults()
        {
            CanLoad = Application.CanStreamedLevelBeLoaded;
            LoadScene = SceneManager.LoadScene;
        }

        /// <summary>Returns true and loads the scene if it's in Build Settings; otherwise logs an
        /// error naming it and leaves the current screen untouched - in the Editor this fails the
        /// same way a real (IL2CPP/device) player does, instead of relying on the Editor's more
        /// permissive in-memory scene resolution.</summary>
        public static bool Load(string sceneName)
        {
            if (!CanLoad(sceneName))
            {
                Debug.LogError($"LevelSceneLoader: '{sceneName}' is not in Build Settings; staying on the current screen.");
                return false;
            }
            LoadScene(sceneName);
            return true;
        }
    }
}
