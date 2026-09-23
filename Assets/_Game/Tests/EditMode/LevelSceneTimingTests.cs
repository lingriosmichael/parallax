using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Parallax.Gameplay.Levels;
using Parallax.Gameplay.Reality;
using Parallax.Gameplay.Rooms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Parallax.Tests.EditMode
{
    // PAX-078 (D-076, R6): in each listed level scene, every configured trap's serialized tick
    // settings (its delay, plus cooldown/period/phase) equal its layout's. A retime changes no
    // geometry, so without this a stale Level_00N.unity would pass every other test. Red until the
    // developer runs Rebuild All Levels after a layout's tick settings change. Reads scenes read-only
    // and restores the editor's scene setup, as LevelSceneTriggerTests does.
    public sealed class LevelSceneTimingTests
    {
        const string LevelsFolder = "Assets/_Game/Scenes/Levels";
        static readonly Type LayoutsType = Type.GetType("Parallax.Editor.Levels.LevelLayouts, Parallax.Editor");

        [Test]
        public void EachListedLevelScene_TrapTickSettings_EqualTheirLayouts()
        {
            var config = AssetDatabase.LoadAssetAtPath<LevelListConfig>("Assets/_Game/Data/LevelListConfig.asset");
            Assert.NotNull(config, "LevelListConfig asset must exist.");
            Assert.NotNull(LayoutsType, "Parallax.Editor.Levels.LevelLayouts not found.");
            var registry = (IDictionary)LayoutsType.GetField("ById", BindingFlags.Public | BindingFlags.Static).GetValue(null);

            string[] setup = EditorSceneManager.GetSceneManagerSetup().Select(s => s.path).ToArray();
            var failures = new List<string>();
            int compared = 0;
            try
            {
                foreach (LevelEntry entry in config.Levels)
                {
                    if (!registry.Contains(entry.Id)) continue;
                    string scenePath = $"{LevelsFolder}/{entry.SceneName}.unity";
                    if (AssetDatabase.LoadAssetAtPath<Object>(scenePath) == null) continue;
                    Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                    RealityRoot root = scene.GetRootGameObjects().Select(g => g.GetComponentInChildren<RealityRoot>(true)).FirstOrDefault(r => r != null && r.name == "RealityRoot_A");
                    Assert.NotNull(root, entry.Id + ": no RealityRoot_A.");

                    foreach (object e in (IEnumerable)Field(registry[entry.Id], "Elements"))
                    {
                        object settings = Field(e, "Settings");
                        if (!(bool)Field(settings, "IsConfigured")) continue;
                        string name = (string)Field(e, "Name"), kind = Field(e, "Kind").ToString();
                        RoomTrap trap = root.GetComponentsInChildren<RoomTrap>(true).FirstOrDefault(t => t.name == name);
                        if (trap == null) { failures.Add($"{entry.Id}: no RoomTrap named {name} in {entry.SceneName}."); continue; }

                        var serialized = new SerializedObject(trap);
                        var expected = new List<(string field, int value)>
                        {
                            ("cooldownTicks", (int)Field(settings, "CooldownTicks")),
                            ("periodTicks", (int)Field(settings, "PeriodTicks")),
                            ("phaseTicks", (int)Field(settings, "PhaseTicks")),
                        };
                        if (kind == "HiddenSpikes") expected.Add(("revealDelayTicks", (int)Field(settings, "RevealDelayTicks")));
                        else if (serialized.FindProperty("delayTicks") != null) expected.Add(("delayTicks", (int)Field(settings, "DelayTicks")));

                        foreach ((string field, int value) in expected)
                        {
                            SerializedProperty property = serialized.FindProperty(field);
                            if (property == null) { failures.Add($"{entry.Id}: {name} has no serialized {field}."); continue; }
                            compared++;
                            if (property.intValue != value)
                                failures.Add($"{entry.Id}: {name}.{field} is {property.intValue} in {entry.SceneName}, layout says {value}. Run Rebuild All Levels.");
                        }
                    }
                }
            }
            finally
            {
                RestoreSetup(setup);
            }
            Assert.Greater(compared, 0, "no trap tick settings were compared; is LevelListConfig empty?");
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

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
