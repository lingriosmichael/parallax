using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Rooms;
using UnityEditor;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    // PAX-077 (D-075): the game ticks at 50 Hz and every seconds<->ticks conversion goes through
    // TickTime. The validator lives in the Editor assembly, which this test assembly does not
    // reference, so it is reached through reflection (the LevelLayoutTests pattern), and the
    // synthetic rooms are built here the same way.
    public sealed class TickTimeTests
    {
        const float ProjectSecondsPerTick = .02f;
        const string TimeManagerPath = "ProjectSettings/TimeManager.asset";
        static readonly Type ValidatorType = Type.GetType("Parallax.Editor.Setup.LevelLayoutValidator, Parallax.Editor");
        static readonly Type ElementType = Type.GetType("Parallax.Editor.Setup.SoloRoomElement, Parallax.Editor");
        static readonly Type SettingsType = Type.GetType("Parallax.Editor.Setup.SoloRoomTrapSettings, Parallax.Editor");
        static readonly Type DefinitionType = Type.GetType("Parallax.Editor.Setup.SoloRoomDefinition, Parallax.Editor");

        [TearDown] public void TearDown() => TickTime.ResetToDefault();

        // ---------- §5.1 guard (R7) ----------

        [Test]
        public void FixedTimestep_OnDiskAndLoaded_IsTwentyMilliseconds()
        {
            string path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, TimeManagerPath);
            Assert.IsTrue(File.Exists(path), $"{TimeManagerPath} not found at {path}.");
            string text = File.ReadAllText(path);
            Assert.IsTrue(TryParseFixedTimestep(text, out double parsed),
                $"{TimeManagerPath}: unrecognised Fixed Timestep format; the D-075 guard cannot read it.");

            Assert.AreEqual(ProjectSecondsPerTick, parsed, 1e-6,
                $"{TimeManagerPath} Fixed Timestep is {parsed:R} s, expected {ProjectSecondsPerTick} s (50 Hz, D-075). Every trap speed is per tick, so a tick-rate change needs a new decision.");
            Assert.AreEqual(parsed, Time.fixedDeltaTime, 1e-7,
                $"Time.fixedDeltaTime is {Time.fixedDeltaTime:R} s but {TimeManagerPath} says {parsed:R} s: the project setting did not load (D-075).");
        }

        // Unity 6 stores Fixed Timestep as a rational (m_Count ticks of m_Rate = Numerator/Denominator
        // per second); older versions store a plain float. Anything else is unrecognised.
        static bool TryParseFixedTimestep(string text, out double seconds)
        {
            Match rational = Regex.Match(text, @"Fixed Timestep:\s*\r?\n\s*m_Count:\s*(\d+)\s*\r?\n\s*m_Rate:\s*\r?\n\s*m_Denominator:\s*(\d+)\s*\r?\n\s*m_Numerator:\s*(\d+)");
            if (rational.Success)
            {
                double count = double.Parse(rational.Groups[1].Value, CultureInfo.InvariantCulture);
                double denominator = double.Parse(rational.Groups[2].Value, CultureInfo.InvariantCulture);
                double numerator = double.Parse(rational.Groups[3].Value, CultureInfo.InvariantCulture);
                seconds = numerator > 0 ? count * denominator / numerator : double.NaN;
                return numerator > 0;
            }
            Match plain = Regex.Match(text, @"Fixed Timestep:[ \t]*([0-9.eE+-]+)[ \t]*\r?\n");
            if (plain.Success) return double.TryParse(plain.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out seconds);
            seconds = double.NaN;
            return false;
        }

        // ---------- §5.2 seam conversions (R6) ----------

        [TestCase(50f, 12f, .6f)]
        [TestCase(60f, 14.4f, .5f)]
        public void Conversions_FollowTheSwappedRate(float hz, float ticksIn024Seconds, float secondsIn30Ticks)
        {
            TickTime.SecondsPerTickSource = () => 1f / hz;

            Assert.AreEqual(1f / hz, TickTime.SecondsPerTick, 1e-7f);
            Assert.AreEqual(hz, TickTime.TicksPerSecond, 1e-3f);
            Assert.AreEqual(ticksIn024Seconds, TickTime.ToTicks(.24f), 1e-4f);
            Assert.AreEqual(secondsIn30Ticks, TickTime.ToSeconds(30f), 1e-5f);
        }

        // ---------- §5.3 the validator uses the seam ----------

        // PeriodicSpikes is 1 u wide: its from-rest crossing is 2.5 u, 0.4667 s = 23.33 ticks at
        // 50 Hz and 28.00 at 60 Hz. A 38-tick safe window clears 23.33 + 12 but not 28 + 12.
        [Test]
        public void PeriodicSlack_PassesAtTheRealRate_FailsWithTheSeamAt60Hz()
        {
            object room = Room(
                Element("HiddenSpikes", "PeriodicSpikes", new Vector2(9f, .15f), new Vector2(1f, .3f),
                    Settings(("revealDelayTicks", 6), ("repeatMode", "Periodic"), ("periodTicks", 40), ("cooldownTicks", 2))));
            AssertFixtureMotor();

            Assert.IsEmpty(Errors(room, "periodic slack"), "at the real 50 Hz rate a 38-tick window clears the 23.33-tick crossing by 12.");

            TickTime.SecondsPerTickSource = () => 1f / 60f;
            Assert.IsNotEmpty(Errors(room, "periodic slack"), "at 60 Hz the crossing is 28 ticks, so 38 < 28 + 12 must fail.");
        }

        // R3: the door margin is one tick at run speed, MaxSpeed x SecondsPerTick: 0.12 u at 50 Hz,
        // 0.10 u at 60 Hz. The door's right edge is x 3.30 and the hazard starts at x 3.41.
        [Test]
        public void DoorClearance_ElevenHundredthsFromAHazard_FailsAtTheRealRate_PassesWithTheSeamAt60Hz()
        {
            object room = Room(Element("Hazard", "NearHazard", new Vector2(3.91f, .15f), new Vector2(1f, .3f), Settings()));
            AssertFixtureMotor();

            Assert.IsNotEmpty(Errors(room, "door (authored pose)"), "at 50 Hz one tick at run speed is 0.12 u, so a door 0.11 u from a kill volume must fail.");

            TickTime.SecondsPerTickSource = () => 1f / 60f;
            Assert.IsEmpty(Errors(room, "door (authored pose)"), "at 60 Hz one tick at run speed is 0.10 u, so 0.11 u must pass.");
        }

        // ---------- §5.4 D-041 bound (R9) ----------

        // Kill at tick T; the hold steps T+1..T+HoldTicks and respawns at T+HoldTicks; the first
        // input-driven step is one tick later; a FallResetVolume kill can land one tick later still.
        [Test]
        public void DeathToRegainedControl_WorstCase_IsWithinThreeQuartersOfASecond()
        {
            RoomSafetyConfig config = AssetDatabase.LoadAssetAtPath<RoomSafetyConfig>("Assets/_Game/Data/RoomSafetyConfig.asset");
            Assert.NotNull(config, "Assets/_Game/Data/RoomSafetyConfig.asset not found.");
            int worstTicks = config.HoldTicks + 2;
            float seconds = TickTime.ToSeconds(worstTicks);
            Assert.LessOrEqual(seconds, .75f, $"D-041: death to regained control is {worstTicks} ticks = {seconds:F3} s at {TickTime.TicksPerSecond:F2} Hz.");
        }

        // ---------- helpers ----------

        // The validator reads the real CatMotorConfig; both seam fixtures above are sized for its
        // current MaxSpeed 6 u/s and Acceleration 60 u/s^2. A retune must fail here, not look like a seam bug.
        static void AssertFixtureMotor()
        {
            Parallax.Gameplay.Player.CatMotorConfig motor = AssetDatabase.LoadAssetAtPath<Parallax.Gameplay.Player.CatMotorConfig>("Assets/_Game/Data/CatMotorConfig_Default.asset");
            Assert.NotNull(motor, "Assets/_Game/Data/CatMotorConfig_Default.asset not found.");
            Assert.AreEqual(6f, motor.MaxSpeed, 1e-4f, "fixture assumes CatMotorConfig_Default MaxSpeed 6; resize the synthetic room.");
            Assert.AreEqual(60f, motor.Acceleration, 1e-4f, "fixture assumes CatMotorConfig_Default Acceleration 60; resize the synthetic room.");
        }

        static string[] Errors(object room, string mention)
        {
            Assert.NotNull(ValidatorType, "Parallax.Editor.Setup.LevelLayoutValidator not found.");
            MethodInfo validate = ValidatorType.GetMethod("Validate", BindingFlags.Public | BindingFlags.Static);
            string[] errors = ((IList)validate.Invoke(null, new[] { "FIX", room })).Cast<string>().ToArray();
            return errors.Where(e => e.Contains(mention)).ToArray();
        }

        // A 20 u room: ceiling, checkpoint, floor, and a door at x 3 (x 2.70..3.30), plus the traps.
        static object Room(params object[] extra)
        {
            object[] frame = {
                Element("Ceiling", "Ceiling", new Vector2(10f, 7.5f), new Vector2(20f, 1f), Settings()),
                Element("Checkpoint", "Checkpoint_0", new Vector2(1f, 0f), Vector2.zero, Settings()),
                Element("Floor", "Floor_A", new Vector2(10f, -.5f), new Vector2(20f, 1f), Settings()),
                Element("Door", "Door_A", new Vector2(3f, .75f), new Vector2(.6f, 1.5f), Settings()),
            };
            object[] all = frame.Concat(extra).ToArray();
            Array elements = Array.CreateInstance(ElementType, all.Length);
            for (int i = 0; i < all.Length; i++) elements.SetValue(all[i], i);
            return Construct(DefinitionType, 7, ("id", 0), ("originX", 0f), ("width", 20f), ("elements", elements));
        }

        static object Element(string kind, string name, Vector2 position, Vector2 size, object settings) =>
            Construct(ElementType, 8, ("kind", kind), ("name", name), ("position", position), ("size", size), ("settings", settings));

        static object Settings(params (string name, object value)[] values) => Construct(SettingsType, -1, values);

        // Invokes the constructor with the given parameter count (-1: the longest), filling every
        // unnamed parameter with its default (arrays empty) and parsing enum values by name.
        static object Construct(Type type, int parameterCount, params (string name, object value)[] values)
        {
            Assert.NotNull(type, "room type not found in Parallax.Editor.");
            ConstructorInfo[] constructors = type.GetConstructors();
            ConstructorInfo ctor = parameterCount < 0
                ? constructors.OrderByDescending(c => c.GetParameters().Length).First()
                : constructors.Single(c => c.GetParameters().Length == parameterCount);
            object[] args = ctor.GetParameters().Select(p =>
            {
                (string name, object value) given = values.FirstOrDefault(v => v.name == p.Name);
                if (given.name == null)
                {
                    if (p.HasDefaultValue && p.DefaultValue != null && !(p.DefaultValue is DBNull))
                        return p.ParameterType.IsEnum ? Enum.ToObject(p.ParameterType, p.DefaultValue) : p.DefaultValue;
                    if (p.ParameterType.IsArray) return Array.CreateInstance(p.ParameterType.GetElementType(), 0);
                    return p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null;
                }
                if (p.ParameterType.IsEnum && given.value is string enumName) return Enum.Parse(p.ParameterType, enumName);
                return given.value;
            }).ToArray();
            return ctor.Invoke(args);
        }
    }
}
