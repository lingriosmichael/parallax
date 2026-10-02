using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Parallax.Gameplay.Cameras;
using Parallax.Gameplay.Levels;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Rooms;
using UnityEditor;
using UnityEngine;
using static Parallax.Tests.EditMode.RouteTestApi;

namespace Parallax.Tests.EditMode
{
    // PAX-060 (D-093): every listed level numbered 11 or more (LevelListConfig's order), against every layout rule, its
    // routes (replayed through the real game code, with the checkpoint-section rules), band 2's content, length, chaos and
    // element rules, and the camera tell rule. ValidateKit runs on every level in LevelKitRulesTests. The output is the
    // per-level report the ticket asks for.
    public sealed class Band2LevelTests
    {
        static readonly Type Validator = Type.GetType("Parallax.Editor.Setup.LevelLayoutValidator, Parallax.Editor");
        static readonly string[] LayoutRules = { "Validate", "ValidateFakePlatformSettings", "ValidateBand1Tells",
            "ValidateArrowTell", "ValidateArrowSpeed", "ValidateArrowLane", "ValidateArrowDoorClearance", "ValidateArrowCooldown", "ValidateArrowPeriodicSlack" };
        const string ConfigPath = "Assets/_Game/Data/LevelListConfig.asset";

        static LevelListConfig Config() => AssetDatabase.LoadAssetAtPath<LevelListConfig>(ConfigPath);

        // Every listed level from number 11 on; a later batch joins by being listed.
        public static IEnumerable<TestCaseData> Band2Levels()
        {
            LevelListConfig config = Config();
            if (config == null) yield break;
            for (int i = 10; i < config.Levels.Count; i++) yield return new TestCaseData(config.Levels[i].Id, i + 1).SetName($"Band2Level_{config.Levels[i].Id}");
        }

        // The count guard: the source above yields one case per listed level from number 11 on. With none listed, Unity's
        // runner fails Level_MeetsEveryBand2Rule ("No arguments were provided") until the first band-2 level is listed.
        [Test]
        public void EveryListedLevelFrom11_HasACase()
        {
            LevelListConfig config = Config();
            Assert.NotNull(config, ConfigPath + " is missing");
            Assert.AreEqual(Math.Max(0, config.Levels.Count - 10), Band2Levels().Count());
        }

        // Each level in its own route session (as Band2RouteResultsTests): sharing one across the levels overflowed the
        // Editor's undo stack by the fourth (PAX-092), which failed that case. A larger level runs over NUnit's default 180 s.
        [TestCaseSource(nameof(Band2Levels))]
        [Timeout(600000)]
        public void Level_MeetsEveryBand2Rule(string id, int level)
        {
            using IDisposable session = OpenSession();
            List<string> errors = Check(session, id, level, out string report);
            TestContext.Out.WriteLine(report);
            errors = LevelZoomAccepted.Unaccepted(errors);   // D-104 amendment: the findings accepted with the 1.8× zoom
            Assert.IsEmpty(errors, string.Join("\n", errors) + "\n\n" + report);
        }

        static IEnumerable<string> Rule(string name, params object[] args)
        {
            MethodInfo m = Validator.GetMethods(BindingFlags.Public | BindingFlags.Static).Single(x => x.Name == name && x.GetParameters().Length == args.Length);
            return ((IList)m.Invoke(null, args)).Cast<string>();
        }

        static CatMotorConfig Motor() => AssetDatabase.LoadAssetAtPath<CatMotorConfig>("Assets/_Game/Data/CatMotorConfig_Default.asset");
        static float Gravity() => AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Gameplay/Player/Cat_Player.prefab").GetComponent<GravityReceiver>().Strength;
        static Vector2 CatSize() => Motor().ColliderSize;

        // One band-2 level, whether listed or not (the level number is given): every error, and the report.
        public static List<string> Check(IDisposable session, string id, int level, out string report) =>
            Check(session, id, level, RouteValidatorTests.Room(id), RouteValidatorTests.Routes(id), out report);

        // The same, for a layout and routes given directly (a level built before it's registered and listed).
        public static List<string> Check(IDisposable session, string id, int level, object room, object routes, out string report)
        {
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
            errors.AddRange(Rule("ValidateBand2Content", id, level, room, routes));

            object routeReport = Call(T("RouteValidator"), "Run", session, id, room, routes);
            errors.AddRange(((IEnumerable)F(routeReport, "Errors")).Cast<string>());
            object solution = ReplayRoute(session, room, F(routes, "Solution"));
            List<Rec> records = Records(solution);
            int ticks = (bool)F(solution, "Completed") ? records[records.Count - 1].Tick : -1;
            errors.AddRange(Rule("ValidateBand2Duration", id, level, ticks));

            var camera = AssetDatabase.LoadAssetAtPath<LevelCameraConfig>("Assets/_Game/Data/LevelCameraConfig.asset");
            object[] args = { session, id, level, room, routes, camera, CatSize(), null };
            MethodInfo replays = Validator.GetMethod("ValidateBand2Replays", BindingFlags.Public | BindingFlags.Static);
            errors.AddRange(((IEnumerable)replays.Invoke(null, args)).Cast<string>());
            object band2 = args[7];

            var cameraErrors = new List<string>();
            IEnumerable table = (IEnumerable)Validator.GetMethod("CameraTell").Invoke(null, new object[] { session, id, room, routes, camera, cameraErrors });
            errors.AddRange(cameraErrors);

            object sections = Call(T("RouteValidator"), "ValidateSections", session, id, room, routes, null);
            report = (string)routeReport.GetType().GetMethod("Summary").Invoke(routeReport, null)
                + $"  solution: {ticks} ticks ({ticks * .02f:F1} s)\n"
                + "  sections (the worst late-death replay is each section's time):\n    " + string.Join("\n    ", ((IEnumerable)F(sections, "Timings")).Cast<object>())
                + "\n    " + string.Join("\n    ", ((IEnumerable)F(sections, "Respawns")).Cast<object>())
                + "\n    " + string.Join("\n    ", ((IEnumerable)F(sections, "Rewinds")).Cast<object>()) + "\n"
                + Counts(routes) + band2 + "\n  camera:\n    " + string.Join("\n    ", table.Cast<object>());
            return errors;
        }

        // Distinct killers over Dies routes, the sequence (LevelLayoutValidator.SequentialChain), dead ends, and the answer mix.
        static string Counts(object routes)
        {
            var betrayals = ((IEnumerable)F(routes, "Betrayals")).Cast<object>().ToList();
            var dies = betrayals.Where(b => F(b, "Outcome").ToString() == "Dies").ToList();
            int killers = dies.Select(b => (string)F(b, "Killer")).Distinct().Count();
            var chain = ((IEnumerable)Validator.GetMethod("SequentialChain").Invoke(null, new[] { routes })).Cast<object>().Select(b => (string)F(b, "Killer")).ToList();
            int deadEnds = betrayals.Count(b => (bool)Validator.GetMethod("IsDeadEnd").Invoke(null, new[] { b }));
            var answers = betrayals.Select(b => (string)F(b, "Name")).Where(n => !n.StartsWith("Dead end"))
                .Select(n => System.Text.RegularExpressions.Regex.Match(n, @"^(.+?) \[([A-Z]+)\]: ")).Where(m => m.Success)
                .Select(m => (trap: m.Groups[1].Value, code: m.Groups[2].Value)).Distinct()
                .GroupBy(x => x.code).OrderBy(g => g.Key).Select(g => $"{g.Key} {g.Count()}");
            return $"  counts: {killers} distinct killers ({string.Join(", ", dies.Select(b => (string)F(b, "Killer")).Distinct())}); "
                + $"{chain.Count} in sequence ({string.Join(" > ", chain)}); {deadEnds} dead ends\n  answers: {string.Join(", ", answers)}\n";
        }
    }
}
