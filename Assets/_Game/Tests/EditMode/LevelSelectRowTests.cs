using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Levels;
using Parallax.Gameplay.UI;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Parallax.Tests.EditMode
{
    // PAX-053 §2.3.3-4: a locked level select row can't be tapped; an unlocked one loads its
    // scene through the shared loader (§2.2), never a direct SceneManager call.
    public sealed class LevelSelectRowTests
    {
        GameObject rowGo;
        System.Func<string, bool> originalCanLoad;
        System.Action<string> originalLoadScene;

        [SetUp]
        public void SaveLoader()
        {
            originalCanLoad = LevelSceneLoader.CanLoad;
            originalLoadScene = LevelSceneLoader.LoadScene;
        }

        [TearDown]
        public void Cleanup()
        {
            if (rowGo != null) Object.DestroyImmediate(rowGo);
            LevelSceneLoader.CanLoad = originalCanLoad;
            LevelSceneLoader.LoadScene = originalLoadScene;
        }

        LevelSelectRow BuildRow()
        {
            rowGo = new GameObject("Row", typeof(RectTransform));
            return rowGo.AddComponent<LevelSelectRow>();
        }

        [Test]
        public void Tap_Locked_RequestsNoSceneLoad()
        {
            LevelSelectRow row = BuildRow();
            row.Configure(new LevelSelectEntry("L002", "Level_002", "Level 2", isUnlocked: false, bestDeathsText: "—"));

            LevelSceneLoader.CanLoad = _ => true;
            bool loadInvoked = false;
            LevelSceneLoader.LoadScene = _ => loadInvoked = true;

            row.Tap();

            Assert.IsFalse(loadInvoked, "a locked level must never request a scene load");
        }

        [Test]
        public void Tap_Unlocked_RequestsItsOwnSceneNameThroughTheLoader()
        {
            LevelSelectRow row = BuildRow();
            row.Configure(new LevelSelectEntry("L003", "Level_003", "Level 3", isUnlocked: true, bestDeathsText: "2"));

            LevelSceneLoader.CanLoad = _ => true;
            string loadedName = null;
            LevelSceneLoader.LoadScene = name => loadedName = name;

            row.Tap();

            Assert.AreEqual("Level_003", loadedName);
        }
    }
}
