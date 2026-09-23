using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Parallax.Gameplay.Levels;
using Parallax.Gameplay.Reality;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Parallax.Tests.EditMode
{
    // PAX-073 §5.7 (D-074): in each listed level scene, every Overlap trap's trigger box equals its
    // layout's. Red until the developer runs Rebuild All Levels after a layout's trigger box changes.
    // Kept apart from LevelSceneTests.cs (already over 400 lines, R8); reads scenes read-only and
    // restores the editor's scene setup, exactly as LevelSceneTests does.
    public sealed class LevelSceneTriggerTests
    {
        const string LevelsFolder = "Assets/_Game/Scenes/Levels";
        static readonly Type LayoutsType = Type.GetType("Parallax.Editor.Levels.LevelLayouts, Parallax.Editor");

        [Test]
        public void EachListedLevelScene_TrapTriggerBoxes_EqualTheirLayouts()
        {
            var config = AssetDatabase.LoadAssetAtPath<LevelListConfig>("Assets/_Game/Data/LevelListConfig.asset");
            Assert.NotNull(config, "LevelListConfig asset must exist.");
            Assert.NotNull(LayoutsType, "Parallax.Editor.Levels.LevelLayouts not found.");
            var registry = (IDictionary)LayoutsType.GetField("ById", BindingFlags.Public | BindingFlags.Static).GetValue(null);

            string[] setup = EditorSceneManager.GetSceneManagerSetup().Select(s => s.path).ToArray();
            var failures = new List<string>();
            try
            {
                foreach (LevelEntry entry in config.Levels)
                {
                    if (!registry.Contains(entry.Id)) continue;
                    string scenePath = $"{LevelsFolder}/{entry.SceneName}.unity";
                    if (AssetDatabase.LoadAssetAtPath<Object>(scenePath) == null) continue;
                    Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                    object room = registry[entry.Id];
                    RealityRoot root = scene.GetRootGameObjects().Select(g => g.GetComponentInChildren<RealityRoot>(true)).FirstOrDefault(r => r != null && r.name == "RealityRoot_A");
                    Assert.NotNull(root, entry.Id + ": no RealityRoot_A.");
                    Vector2 origin = (Vector2)Field(room, "Origin");

                    foreach (object e in (IEnumerable)Field(room, "Elements"))
                    {
                        Vector2 size = (Vector2)Field(e, "SecondarySize");
                        object settings = Field(e, "Settings");
                        if (size == Vector2.zero || !(bool)Field(settings, "IsConfigured")) continue;
                        if (Field(settings, "TriggerSource").ToString() != "Overlap" || Field(settings, "RepeatMode").ToString() == "Periodic") continue;
                        string name = (string)Field(e, "Name"), triggerName = (string)Field(settings, "TriggerName");
                        Transform trap = Find(root.transform, name);
                        Transform trigger = trap == null ? null : trap.Find(triggerName);
                        BoxCollider2D box = trigger == null ? null : trigger.GetComponent<BoxCollider2D>();
                        if (box == null) { failures.Add($"{entry.Id}: {name}/{triggerName} has no trigger BoxCollider2D."); continue; }

                        Vector2 expectedCentre = root.ToWorld(origin + (Vector2)Field(e, "SecondaryPosition"));
                        Vector2 actualCentre = trigger.TransformPoint(box.offset);
                        Vector2 actualSize = Vector2.Scale(box.size, trigger.lossyScale);
                        if (Vector2.Distance(expectedCentre, actualCentre) > 1e-3f || Vector2.Distance(size, actualSize) > 1e-3f)
                            failures.Add($"{entry.Id}: {name} trigger is centre {actualCentre} size {actualSize} in the scene, layout says centre {expectedCentre} size {size}. Run Rebuild All Levels.");
                    }
                }
            }
            finally
            {
                RestoreSetup(setup);
            }
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        static Transform Find(Transform root, string name) =>
            root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name);

        static object Field(object value, string name) => value.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance).GetValue(value);

        static void RestoreSetup(string[] paths)
        {
            if (paths.Length == 0) return;
            var setup = new SceneSetup[paths.Length];
            for (int i = 0; i < paths.Length; i++) setup[i] = new SceneSetup { path = paths[i], isActive = i == 0, isLoaded = true };
            EditorSceneManager.RestoreSceneManagerSetup(setup);
        }
    }
}
