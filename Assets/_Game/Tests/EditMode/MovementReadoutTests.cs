using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Parallax.DebugTools;
using Parallax.Gameplay.Player;
using UnityEditor;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    // PAX-082: the movement readout reports the same reach the validator enforces. The validator
    // lives in the Editor assembly, which this assembly doesn't reference, so it is reached by
    // reflection (the LevelLayoutTests pattern).
    public sealed class MovementReadoutTests
    {
        static readonly Type ValidatorType = Type.GetType("Parallax.Editor.Setup.LevelLayoutValidator, Parallax.Editor");
        static readonly Type FixturesType = Type.GetType("Parallax.Editor.Setup.LevelLayoutValidatorFixtures, Parallax.Editor");

        [Test]
        public void Readout_AllowedReach_EqualsTheValidators_ForTheCommittedConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<CatMotorConfig>("Assets/_Game/Data/CatMotorConfig_Default.asset");
            var cat = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Gameplay/Player/Cat_Player.prefab");
            Assert.NotNull(config, "CatMotorConfig_Default.asset");
            Assert.NotNull(cat, "Cat_Player.prefab");
            float gravity = cat.GetComponent<GravityReceiver>().Strength;

            // JumpBeyondReach: a flat 100 u jump off a 3 u runway, so the take-off is at full speed and the
            // validator's error prints its own allowed reach: "(distance 100.00 > <allowed>)".
            object room = FixturesType.GetMethod("JumpBeyondReach", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
            var errors = (List<string>)ValidatorType.GetMethod("Validate", BindingFlags.Public | BindingFlags.Static).Invoke(null, new[] { "FIX", room });
            string reachError = errors.SingleOrDefault(e => e.Contains("jump beyond"));
            Assert.NotNull(reachError, "the fixture's reach error: " + string.Join(" | ", errors));
            Match m = Regex.Match(reachError, @"> (\d+\.\d+)\)");
            Assert.IsTrue(m.Success, reachError);
            float validatorAllowed = float.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);

            Assert.AreEqual(validatorAllowed, MovementReadoutValues.From(config, gravity).AllowedReach, .005f, "readout vs validator (F2 message)");
        }

        // Review SF-2: the same agreement for a second config, through the validator's own code (ValidateWithMotor).
        [Test]
        public void Readout_AllowedReach_EqualsTheValidators_ForThePreRetuneConfig()
        {
            CatMotorConfig preRetune = SoloRoomsLayoutTests.PreRetuneConfig();
            object room = FixturesType.GetMethod("JumpBeyondReach", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
            var errors = (List<string>)ValidatorType.GetMethod("ValidateWithMotor", BindingFlags.Public | BindingFlags.Static)
                .Invoke(null, new object[] { "FIX", room, preRetune, SoloRoomsLayoutTests.PreRetuneGravity });
            string reachError = errors.SingleOrDefault(e => e.Contains("jump beyond"));
            Assert.NotNull(reachError, "the fixture's reach error: " + string.Join(" | ", errors));
            Match m = Regex.Match(reachError, @"> (\d+\.\d+)\)");
            Assert.IsTrue(m.Success, reachError);
            float validatorAllowed = float.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
            Assert.AreEqual(validatorAllowed, MovementReadoutValues.From(preRetune, SoloRoomsLayoutTests.PreRetuneGravity).AllowedReach, .005f, "readout vs validator (F2 message)");
        }

        // §12 R1: only the "off" path is tested here; the spawn itself is unproven (Editor Play).
        [Test]
        public void SpawnHook_WithThePrefOff_CreatesNothing()
        {
            Func<bool> saved = MovementReadout.IsEnabledSource;
            int before = UnityEngine.Object.FindObjectsByType<MovementReadout>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            try
            {
                MovementReadout.IsEnabledSource = () => false;
                MovementReadout.SpawnIfEnabled();
                int after = UnityEngine.Object.FindObjectsByType<MovementReadout>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
                Assert.AreEqual(before, after, "the hook created a readout with the pref off");
            }
            finally
            {
                MovementReadout.IsEnabledSource = saved;
                // Only the hook names an object this way; remove it if a regression created one.
                foreach (MovementReadout r in UnityEngine.Object.FindObjectsByType<MovementReadout>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (r.gameObject.name == "MovementReadout (PAX-082)") UnityEngine.Object.DestroyImmediate(r.gameObject);
            }
        }

        [Test]
        public void Readout_Values_ForAnotherConfig_MatchTheContinuousJump()
        {
            // max speed 5 u/s, acceleration 50 u/s², apex 2 u, gravity 30 u/s²: launch sqrt(120) u/s.
            var v = new MovementReadoutValues(5f, 50f, 2f, 30f);
            float airtime = 2f * Mathf.Sqrt(120f) / 30f;
            Assert.AreEqual(2f, v.Apex, 1e-5f);
            Assert.AreEqual(airtime, v.AirtimeSeconds, 1e-5f);
            Assert.AreEqual(Parallax.Core.TickTime.ToTicks(airtime), v.AirtimeTicks, 1e-3f);
            Assert.AreEqual(5f * airtime, v.Reach, 1e-4f);
            Assert.AreEqual(.75f * 5f * airtime, v.AllowedReach, 1e-4f);
            Assert.AreEqual(.1f, v.RunPerTick, 1e-6f);
        }
    }
}
