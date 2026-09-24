using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Reality;
using Parallax.Gameplay.Rooms;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Parallax.Tests.EditMode
{
    // PAX-073 (D-074): thin, narrow floating platforms are ordinary Floors (R9). Size limits come
    // from PlatformSizeConfig: synthetic tests use CreateInstance; only the real-levels, tunnelling
    // and TrapLab checks load the asset (created by PARALLAX/Setup/Levels/Platform Size Config).
    public sealed class ThinPlatformTests
    {
        const string ConfigPath = "Assets/_Game/Data/PlatformSizeConfig.asset";
        static readonly Type ValidatorType = Type.GetType("Parallax.Editor.Setup.LevelLayoutValidator, Parallax.Editor");
        static readonly Type FixturesType = Type.GetType("Parallax.Editor.Setup.TriggerCoverageFixtures, Parallax.Editor");
        static readonly Type LayoutsType = Type.GetType("Parallax.Editor.Levels.LevelLayouts, Parallax.Editor");
        static readonly Type BuilderType = Type.GetType("Parallax.Editor.Setup.SoloRoomBuilder, Parallax.Editor");

        readonly List<UnityEngine.Object> created = new();

        [TearDown] public void TearDown() { foreach (UnityEngine.Object o in created) if (o != null) UnityEngine.Object.DestroyImmediate(o); created.Clear(); }

        PlatformSizeConfig Limits(float minThickness, float minWidth)
        {
            var config = ScriptableObject.CreateInstance<PlatformSizeConfig>();
            created.Add(config);
            var so = new SerializedObject(config);
            so.FindProperty("minThickness").floatValue = minThickness;
            so.FindProperty("minWidth").floatValue = minWidth;
            so.ApplyModifiedPropertiesWithoutUndo();
            return config;
        }

        static PlatformSizeConfig RealLimits()
        {
            var config = AssetDatabase.LoadAssetAtPath<PlatformSizeConfig>(ConfigPath);
            Assert.NotNull(config, ConfigPath + " is missing: run PARALLAX/Setup/Levels/Platform Size Config first.");
            return config;
        }

        static object Fixture(string name, params object[] args)
        {
            Assert.NotNull(FixturesType, "Parallax.Editor.Setup.TriggerCoverageFixtures not found.");
            MethodInfo method = FixturesType.GetMethod(name, BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method, "TriggerCoverageFixtures." + name + " not found.");
            return method.Invoke(null, args);
        }

        static string[] Sizes(string levelId, object room, PlatformSizeConfig limits)
        {
            Assert.NotNull(ValidatorType, "Parallax.Editor.Setup.LevelLayoutValidator not found.");
            MethodInfo method = ValidatorType.GetMethod("ValidatePlatformSizes", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(method, "LevelLayoutValidator.ValidatePlatformSizes not found.");
            return ((IList)method.Invoke(null, new[] { levelId, room, limits })).Cast<string>().ToArray();
        }

        // ---------- §5.4 size validation ----------

        [Test]
        public void Platform_BelowMinimumThickness_IsRejectedByName()
        {
            string[] errors = Sizes("FIX", Fixture("WithPlatform", 1f, .49f), Limits(.5f, 1f));
            Assert.IsTrue(errors.Any(e => e.StartsWith("FIX:") && e.Contains("Thin") && e.Contains("thickness")), "expected a thickness error naming 'Thin':\n" + string.Join("\n", errors));
        }

        [Test]
        public void Platform_BelowMinimumWidth_IsRejectedByName()
        {
            string[] errors = Sizes("FIX", Fixture("WithPlatform", .99f, .5f), Limits(.5f, 1f));
            Assert.IsTrue(errors.Any(e => e.StartsWith("FIX:") && e.Contains("Thin") && e.Contains("width")), "expected a width error naming 'Thin':\n" + string.Join("\n", errors));
        }

        [Test]
        public void Platform_ExactlyAtTheMinimum_Passes() =>
            Assert.IsEmpty(Sizes("FIX", Fixture("WithPlatform", 1f, .5f), Limits(.5f, 1f)));

        [Test]
        public void Limits_AreReadFromTheConfig_NotFromConstants()
        {
            object platform = Fixture("WithPlatform", 1f, .5f);
            Assert.IsEmpty(Sizes("FIX", Fixture("WithPlatform", .6f, .3f), Limits(.3f, .6f)), "a 0.6 x 0.3 platform passes when the config allows it.");
            Assert.IsNotEmpty(Sizes("FIX", platform, Limits(.8f, 1f)), "a 1.0 x 0.5 platform fails when the config asks for 0.8 thickness.");
            Assert.IsNotEmpty(Sizes("FIX", platform, Limits(.5f, 1.5f)), "a 1.0 x 0.5 platform fails when the config asks for 1.5 width.");
        }

        [Test]
        public void EveryLevelLayoutsFloor_PassesTheRealPlatformSizeConfig()
        {
            PlatformSizeConfig limits = RealLimits();
            Assert.NotNull(LayoutsType, "Parallax.Editor.Levels.LevelLayouts not found.");
            var registry = (IDictionary)LayoutsType.GetField("ById", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            var failures = new List<string>();
            foreach (DictionaryEntry entry in registry) failures.AddRange(Sizes((string)entry.Key, entry.Value, limits));
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        // ---------- §5.6 no tunnelling ----------

        [Test]
        public void NoTunnelling_MinimumThickness_IsAtLeastTheMaxFallPerPhysicsStep()
        {
            CatMotorConfig motor = AssetDatabase.LoadAssetAtPath<CatMotorConfig>("Assets/_Game/Data/CatMotorConfig_Default.asset");
            Assert.NotNull(motor, "CatMotorConfig_Default asset missing.");
            float perStep = motor.MaxFallSpeed * Time.fixedDeltaTime;
            Assert.GreaterOrEqual(RealLimits().MinThickness, perStep,
                $"min thickness must be >= max fall {motor.MaxFallSpeed} u/s x fixed dt {Time.fixedDeltaTime} s = {perStep} u per step.");
        }

        // ---------- §5.5 thin platform build (R10: the TrapLab platform) ----------

        [Test]
        public void TrapLabThinPlatform_BuildsASolidTwoSidedFloorOfItsAuthoredSizeAndPosition()
        {
            object room = Fixture("TrapLabThinPlatformOnly");
            object[] elements = ((IEnumerable)Field(room, "Elements")).Cast<object>().ToArray();
            Assert.AreEqual(1, elements.Length, "TrapLabLayout room 0 must have exactly one Floor named 'ThinPlatform'.");
            object platform = elements[0];
            Assert.AreEqual("Floor", Field(platform, "Kind").ToString(), "the thin platform reuses the Floor kind (R9).");
            Vector2 position = (Vector2)Field(platform, "Position"), size = (Vector2)Field(platform, "Size");

            // PAX-075 R22: Single mode, since Unity refuses an additive scene next to the Test Runner's unsaved untitled one.
            Scene temp = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            try
            {
                SceneManager.SetActiveScene(temp);
                var rootGO = new GameObject("RealityRoot_A");
                RealityRoot root = rootGO.AddComponent<RealityRoot>();
                CatMotorConfig motor = AssetDatabase.LoadAssetAtPath<CatMotorConfig>("Assets/_Game/Data/CatMotorConfig_Default.asset");
                MethodInfo build = BuilderType.GetMethod("BuildRoom", BindingFlags.Public | BindingFlags.Static);
                build.Invoke(null, new object[] { rootGO.transform, root, room, null, null, null, null, motor, new List<string>() });

                Transform built = rootGO.transform.Find("Room_1/ThinPlatform");
                Assert.NotNull(built, "ThinPlatform was not built.");
                Assert.AreEqual((Vector2)Field(room, "Origin") + position, (Vector2)built.localPosition, "authored position");
                BoxCollider2D box = built.GetComponent<BoxCollider2D>();
                Assert.NotNull(box, "a thin platform has a BoxCollider2D.");
                Assert.IsFalse(box.isTrigger, "a thin platform is solid.");
                Assert.AreEqual(size, box.size, "collider size equals the authored size.");
                Assert.IsFalse(box.usedByEffector, "no one-way effector: landable from above (gravity down) and from below (gravity up).");
                Assert.IsNull(built.GetComponent<PlatformEffector2D>(), "no one-way effector.");
                SpriteRenderer visual = built.GetComponent<SpriteRenderer>();
                Assert.NotNull(visual, "a thin platform has the floor look.");
                Assert.AreEqual(SpriteDrawMode.Sliced, visual.drawMode, "floor look: sliced sprite");
                Assert.AreEqual(size, visual.size, "floor look: sprite size equals the authored size");
                Transform floor = rootGO.transform.Find("Room_1/Wall_Left");
                Assert.NotNull(floor, "the room frame was built.");
                Assert.AreEqual(floor.GetComponent<SpriteRenderer>().color, visual.color, "floor look: the same ground colour as the room's other geometry.");
            }
            finally
            {
                // Put back the Test Runner's untitled scene with the route harness's helper.
                Type.GetType("Parallax.Editor.Routes.RouteSession, Parallax.Editor").GetMethod("RecreateUntitledScene").Invoke(null, new object[] { true });
            }
        }

        [Test]
        public void TrapLabThinPlatform_SitsExactlyAtTheRealMinimumSize()
        {
            object[] elements = ((IEnumerable)Field(Fixture("TrapLabThinPlatformOnly"), "Elements")).Cast<object>().ToArray();
            Assert.AreEqual(1, elements.Length, "TrapLabLayout room 0 must have exactly one Floor named 'ThinPlatform'.");
            PlatformSizeConfig limits = RealLimits();
            Assert.AreEqual(new Vector2(limits.MinWidth, limits.MinThickness), (Vector2)Field(elements[0], "Size"), "the lab platform sits exactly at the minimum size.");
            Assert.IsEmpty(Sizes("TrapLab", Fixture("TrapLabThinPlatformOnly"), limits), "exactly at the minimum passes the size check.");
        }

        static object Field(object value, string name) => value.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance).GetValue(value);
    }
}
