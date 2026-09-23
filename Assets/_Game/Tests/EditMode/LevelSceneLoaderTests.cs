using NUnit.Framework;
using Parallax.Gameplay.Levels;
using UnityEngine;
using UnityEngine.TestTools;

namespace Parallax.Tests.EditMode
{
    // PAX-053 §2.2/§5.4: the single runtime scene loader every level/menu scene change goes
    // through. CanLoad/LoadScene are test seams (production defaults are
    // Application.CanStreamedLevelBeLoaded and SceneManager.LoadScene) - swapped here so the
    // "can't be loaded" path is provable without actually loading a scene in EditMode, and so a
    // real Editor-only fallback (which the loader must never grow) has nothing to hide behind.
    public sealed class LevelSceneLoaderTests
    {
        System.Func<string, bool> originalCanLoad;
        System.Action<string> originalLoadScene;

        [SetUp]
        public void SaveOriginals()
        {
            originalCanLoad = LevelSceneLoader.CanLoad;
            originalLoadScene = LevelSceneLoader.LoadScene;
        }

        [TearDown]
        public void RestoreOriginals()
        {
            LevelSceneLoader.CanLoad = originalCanLoad;
            LevelSceneLoader.LoadScene = originalLoadScene;
        }

        [Test]
        public void SceneNotLoadable_LogsErrorNamingScene_NeverInvokesLoadScene()
        {
            LevelSceneLoader.CanLoad = _ => false;
            bool loadInvoked = false;
            LevelSceneLoader.LoadScene = _ => loadInvoked = true;

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*Level_003.*"));
            bool result = LevelSceneLoader.Load("Level_003");

            Assert.IsFalse(result);
            Assert.IsFalse(loadInvoked, "a scene that can't be loaded must never reach LoadScene");
        }

        [Test]
        public void SceneLoadable_InvokesLoadSceneWithTheGivenName_NoError()
        {
            LevelSceneLoader.CanLoad = _ => true;
            string loadedName = null;
            LevelSceneLoader.LoadScene = name => loadedName = name;

            bool result = LevelSceneLoader.Load("Level_002");

            Assert.IsTrue(result);
            Assert.AreEqual("Level_002", loadedName);
        }
    }
}
