using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Parallax.Gameplay.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Parallax.Tests.EditMode
{
    // PAX-053 review fix: the original version of this test asserted a bare Unity
    // VerticalLayoutGroup/ContentSizeFitter collapses a row with no LayoutElement to zero height -
    // that's Unity's own documented layout behaviour, not anything this ticket's code decides, so
    // it can never fail "for our reason" (only if Unity itself changed). This tests the actual
    // production seam instead: LevelSelectSetup.BuildRowTemplate (the exact method BuildUI calls)
    // must build a row carrying a LayoutElement with a positive preferredHeight, since that is
    // what LevelSelectSetup.BuildUI:169-183's VerticalLayoutGroup (childControlHeight = true)
    // needs to size the row from - not Unity's layout system in the abstract. PAX-A09: Content is a grid of cells now, so the height is no longer what
    // sizes a row, but the seam stays.
    public sealed class LevelSelectLayoutTests
    {
        static readonly Type LevelSelectSetupType = Type.GetType("Parallax.Editor.Setup.LevelSelectSetup, Parallax.Editor");

        GameObject contentGo;

        // PAX-083 (items 3-4): BuildRowTemplate registers its objects with Undo, which dirties the scene they're built
        // in (before PAX-075 R21, the leaked Level_004). Build in a scene the test creates, then put back a clean Test
        // Runner scene.
        [SetUp]
        public void OpenScene() => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        [TearDown]
        public void Cleanup()
        {
            if (contentGo != null) Object.DestroyImmediate(contentGo);
            Type.GetType("Parallax.Editor.Routes.RouteSession, Parallax.Editor").GetMethod("RecreateUntitledScene").Invoke(null, new object[] { true });
        }

        [Test]
        public void BuildRowTemplate_ProducesARowWithAPositivePreferredHeight()
        {
            Assert.NotNull(LevelSelectSetupType, "Parallax.Editor.Setup.LevelSelectSetup not found.");
            MethodInfo method = LevelSelectSetupType.GetMethod("BuildRowTemplate", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method, "LevelSelectSetup.BuildRowTemplate not found.");

            contentGo = new GameObject("Content", typeof(RectTransform));
            var changes = new List<string>();

            var row = (LevelSelectRow)method.Invoke(null, new object[] { contentGo.transform, changes });

            Assert.NotNull(row, "BuildRowTemplate must return the built LevelSelectRow.");
            var layoutElement = row.GetComponent<LayoutElement>();
            Assert.NotNull(layoutElement, "the row LevelSelectSetup builds must carry a LayoutElement, or Content's childControlHeight VerticalLayoutGroup collapses it to zero height.");
            Assert.Greater(layoutElement.preferredHeight, 0f, "the row's LayoutElement.preferredHeight must be positive.");
        }

        // The rows were 0 wide, so Level Select drew nothing. PAX-A09: they're cells now: Content's grid sizes every one, whatever the template's own size.
        // Builds Content through the menu's own seams, lays it out, and reads the cell's size.
        [Test]
        public void ContentLayout_SizesEachCellFromTheGrid()
        {
            Assert.NotNull(LevelSelectSetupType, "Parallax.Editor.Setup.LevelSelectSetup not found.");
            MethodInfo configure = LevelSelectSetupType.GetMethod("ConfigureContentLayout", BindingFlags.Public | BindingFlags.Static);
            MethodInfo buildRow = LevelSelectSetupType.GetMethod("BuildRowTemplate", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(configure, "LevelSelectSetup.ConfigureContentLayout not found.");
            Assert.NotNull(buildRow, "LevelSelectSetup.BuildRowTemplate not found.");

            contentGo = new GameObject("Content", typeof(RectTransform));
            var contentRect = (RectTransform)contentGo.transform;
            contentRect.sizeDelta = new Vector2(800f, 600f);
            var changes = new List<string>();
            var grid = (GridLayoutGroup)configure.Invoke(null, new object[] { contentGo, changes });
            var row = (LevelSelectRow)buildRow.Invoke(null, new object[] { contentGo.transform, changes });
            row.gameObject.SetActive(true);   // the runtime clones are active; the template itself is hidden

            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

            var rowRect = (RectTransform)row.transform;
            Assert.AreEqual(grid.cellSize.x, rowRect.rect.width, .01f, "each cell takes the grid's width (a row was once 0 wide, so Level Select showed nothing).");
            Assert.AreEqual(grid.cellSize.y, rowRect.rect.height, .01f, "each cell takes the grid's height.");
        }

        // A scene built before PAX-A09 has the old list on Content; two layout groups on one object fight.
        [Test]
        public void ContentLayout_ReplacesTheOldList()
        {
            MethodInfo configure = LevelSelectSetupType.GetMethod("ConfigureContentLayout", BindingFlags.Public | BindingFlags.Static);
            contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.AddComponent<VerticalLayoutGroup>();

            configure.Invoke(null, new object[] { contentGo, new List<string>() });

            Assert.IsNull(contentGo.GetComponent<VerticalLayoutGroup>());
            Assert.IsNotNull(contentGo.GetComponent<GridLayoutGroup>());
        }

        [Test]
        public void BuildRowTemplate_CalledTwice_IsIdempotent_SecondCallReportsNoChanges()
        {
            Assert.NotNull(LevelSelectSetupType, "Parallax.Editor.Setup.LevelSelectSetup not found.");
            MethodInfo method = LevelSelectSetupType.GetMethod("BuildRowTemplate", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method, "LevelSelectSetup.BuildRowTemplate not found.");

            contentGo = new GameObject("Content", typeof(RectTransform));
            var firstChanges = new List<string>();
            method.Invoke(null, new object[] { contentGo.transform, firstChanges });
            Assert.Greater(firstChanges.Count, 0, "the first call must report what it built.");

            var secondChanges = new List<string>();
            method.Invoke(null, new object[] { contentGo.transform, secondChanges });
            Assert.AreEqual(0, secondChanges.Count, "a second call against the same parent must report no changes (find-or-create, per the other PARALLAX/Setup menus).");
        }
    }
}
