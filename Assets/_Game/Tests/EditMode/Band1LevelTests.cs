using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Parallax.Gameplay.Cameras;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Rooms;
using UnityEditor;
using UnityEngine;
using static Parallax.Tests.EditMode.RouteTestApi;

namespace Parallax.Tests.EditMode
{
    // PAX-059 (D-085): every band-1 level against every layout rule, its routes (replayed through the real game code),
    // the band-1 content, length and tell rules, and the camera tell rule. The output lists the route report, the
    // solution's length and the camera table, so a failure shows the whole level at once.
    public sealed class Band1LevelTests
    {
        static readonly Type Validator = Type.GetType("Parallax.Editor.Setup.LevelLayoutValidator, Parallax.Editor");
        static readonly string[] LayoutRules = { "Validate", "ValidateFakePlatformSettings", "ValidateBand1Tells",
            "ValidateArrowTell", "ValidateArrowSpeed", "ValidateArrowLane", "ValidateArrowDoorClearance", "ValidateArrowCooldown", "ValidateArrowPeriodicSlack" };

        IDisposable session;

        [OneTimeSetUp] public void Open() => session = OpenSession();
        [OneTimeTearDown] public void Close() => session?.Dispose();

        static IEnumerable<string> Rule(string name, params object[] args)
        {
            MethodInfo m = Validator.GetMethods(BindingFlags.Public | BindingFlags.Static).Single(x => x.Name == name && x.GetParameters().Length == args.Length);
            return ((IList)m.Invoke(null, args)).Cast<string>();
        }

        static CatMotorConfig Motor() => AssetDatabase.LoadAssetAtPath<CatMotorConfig>("Assets/_Game/Data/CatMotorConfig_Default.asset");
        static float Gravity() => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Gameplay/Player/Cat_Player.prefab").GetComponent<GravityReceiver>().Strength;

        [TestCase("L001", 1)]
        [TestCase("L002", 2)]
        [TestCase("L003", 3)]
        [TestCase("L004", 4)]
        [TestCase("L005", 5)]
        public void Level_MeetsEveryBand1Rule(string id, int level)
        {
            object room = RouteValidatorTests.Room(id), routes = RouteValidatorTests.Routes(id);
            Assert.NotNull(room, id + " has no layout"); Assert.NotNull(routes, id + " has no routes");
            var errors = new List<string>();
            foreach (string rule in LayoutRules) errors.AddRange(Rule(rule, id, room));
            var bypasses = new List<string>();
            errors.AddRange(Rule("ValidateTriggerCoverage", id, room, Motor(), Gravity(), bypasses));
            errors.AddRange(bypasses.Select(b => "not approved: " + b));
            errors.AddRange(Rule("ValidateSurfaceCoverage", id, room, Motor()));
            errors.AddRange(Rule("ValidateFallingBlockLanding", id, room, Motor()));
            errors.AddRange(Rule("ValidateTrapFloorHeadroom", id, room, Motor()));
            errors.AddRange(Rule("ValidateTriggerNearTrap", id, room));
            errors.AddRange(Rule("ValidatePlatformSizes", id, room, AssetDatabase.LoadAssetAtPath<PlatformSizeConfig>("Assets/_Game/Data/PlatformSizeConfig.asset")));
            errors.AddRange(Rule("ValidateBand1Content", id, level, room, routes));

            object report = Call(T("RouteValidator"), "Run", session, id, room, routes);
            errors.AddRange(((IEnumerable)F(report, "Errors")).Cast<string>());
            object solution = ReplayRoute(session, room, F(routes, "Solution"));
            List<Rec> records = Records(solution);
            int ticks = (bool)F(solution, "Completed") ? records[records.Count - 1].Tick : -1;
            errors.AddRange(Rule("ValidateBand1Duration", id, level, ticks));

            var cameraErrors = new List<string>();
            var camera = AssetDatabase.LoadAssetAtPath<LevelCameraConfig>("Assets/_Game/Data/LevelCameraConfig.asset");
            IEnumerable table = (IEnumerable)Validator.GetMethod("CameraTell").Invoke(null, new object[] { session, id, room, routes, camera, cameraErrors });
            errors.AddRange(cameraErrors);

            string summary = (string)report.GetType().GetMethod("Summary").Invoke(report, null)
                + $"  solution: {ticks} ticks\n  camera:\n    " + string.Join("\n    ", table.Cast<object>());
            TestContext.Out.WriteLine(summary);
            Assert.IsEmpty(errors, string.Join("\n", errors) + "\n\n" + summary);
        }
    }
}
