using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using NUnit.Framework;
using Parallax.Gameplay.Player;
using UnityEditor;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    // PAX-076 (D-083): reflection access to the KIT-4 rules and fixtures in Parallax.Editor (this assembly doesn't
    // reference it), shared by PrecisionSectionTests, DifficultyBandTests, BaitGapTests, CameraTellTests and
    // TrapLabRoom5Tests.
    public static class PrecisionTestApi
    {
        public static readonly Type Validator = Type.GetType("Parallax.Editor.Setup.LevelLayoutValidator, Parallax.Editor");
        static readonly Type Fixtures = Type.GetType("Parallax.Editor.Setup.PrecisionFixtures, Parallax.Editor");
        static readonly Type ThresholdsType = Type.GetType("Parallax.Editor.Setup.PrecisionThresholds, Parallax.Editor");

        public static object Fixture(string name, params object[] args) => Invoke(Fixtures, name, args);

        public static List<string> Rule(string name, params object[] args) => (List<string>)Invoke(Validator, name, args);

        public static object Invoke(Type type, string method, params object[] args)
        {
            Assert.NotNull(type, "type not found in Parallax.Editor.");
            MethodInfo m = type.GetMethod(method, BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(m, type.Name + "." + method + " not found.");
            try { return m.Invoke(null, args); }
            catch (TargetInvocationException e) { ExceptionDispatchInfo.Capture(e.InnerException).Throw(); throw; }
        }

        // A PrecisionThresholds instance (not the asset) with the given values; the class defaults are the provisional
        // 0.85 / 8.
        public static ScriptableObject Thresholds(float reach = .85f, int slack = 8)
        {
            Assert.NotNull(ThresholdsType, "Parallax.Editor.Setup.PrecisionThresholds not found.");
            ScriptableObject t = ScriptableObject.CreateInstance(ThresholdsType);
            ThresholdsType.GetField("reachFraction", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(t, reach);
            ThresholdsType.GetField("slackTicks", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(t, slack);
            return t;
        }

        public static CatMotorConfig Motor() => AssetDatabase.LoadAssetAtPath<CatMotorConfig>("Assets/_Game/Data/CatMotorConfig_Default.asset");
        public static float Gravity() => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Gameplay/Player/Cat_Player.prefab").GetComponent<GravityReceiver>().Strength;
    }
}
