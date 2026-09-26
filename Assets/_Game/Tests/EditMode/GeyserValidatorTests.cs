using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Levels;
using UnityEngine;
using static Parallax.Tests.EditMode.PrecisionTestApi;
using static Parallax.Tests.EditMode.RouteTestApi;
using Object = UnityEngine.Object;

namespace Parallax.Tests.EditMode
{
    // PAX-086 (D-088) §5, R2, R5, R6: ValidateGeyser's and ValidateGeyserEnvelope's clauses, ValidateBand's geyser clause,
    // and "a geyser is never a kill volume". Every room is Trap Lab room 8 with its Geyser element replaced (or one element
    // added).
    public sealed class GeyserValidatorTests
    {
        LevelListConfig list;

        [TearDown] public void Cleanup() { if (list != null) Object.DestroyImmediate(list); }

        internal static Type EditorType(string name)
        {
            Type type = Type.GetType("Parallax.Editor.Setup." + name + ", Parallax.Editor");
            Assert.NotNull(type, name + " not found");
            return type;
        }

        internal static object Room8() => ((IList)EditorType("TrapLabLayout").GetField("Rooms").GetValue(null))[8];
        static object Routes8() => Call(Type.GetType("Parallax.Editor.Levels.TrapLabRoutes, Parallax.Editor"), "Room8");

        internal static object Timing(TrapRepeatMode repeat, int period, int phase)
        {
            ConstructorInfo ctor = EditorType("SoloRoomTrapSettings").GetConstructors().OrderByDescending(c => c.GetParameters().Length).First();
            object[] values = ctor.GetParameters().Select(p => p.Name switch
            {
                "repeatMode" => (object)repeat, "periodTicks" => period, "phaseTicks" => phase, _ => p.DefaultValue,
            }).ToArray();
            return ctor.Invoke(values);
        }

        internal static object GeyserSettings(GeyserDirection direction = GeyserDirection.Up, float width = 1f, float height = 1.5f, int tell = 25, int erupt = 40,
            float speed = 14f, TrapRepeatMode repeat = TrapRepeatMode.Periodic, int period = 100, int phase = 80)
        {
            Type geyser = EditorType("GeyserSettings");
            object settings = geyser.GetConstructors().Single(c => c.GetParameters().Length == 6).Invoke(new object[] { direction, width, height, tell, erupt, speed });
            ConstructorInfo ctor = EditorType("SoloRoomTrapSettings").GetConstructors().Single(c => c.GetParameters().Length == 2 && c.GetParameters()[0].ParameterType == geyser);
            return ctor.Invoke(new[] { settings, Timing(repeat, period, phase) });
        }

        internal static object Element(string kind, string name, Vector2 position, Vector2 size, Vector2 secondaryPosition = default, Vector2 secondarySize = default, object settings = null)
        {
            Type elementType = EditorType("SoloRoomElement");
            ConstructorInfo ctor = elementType.GetConstructors().First(c => c.GetParameters().Length == 7);
            return ctor.Invoke(new[] { Enum.Parse(EditorType("SoloRoomElementKind"), kind), name, position, size, secondaryPosition, secondarySize,
                settings ?? Activator.CreateInstance(EditorType("SoloRoomTrapSettings")) });
        }

        static object Rebuild(object room, IEnumerable<object> elements)
        {
            Type elementType = EditorType("SoloRoomElement");
            object[] list = elements.ToArray();
            Array array = Array.CreateInstance(elementType, list.Length);
            for (int i = 0; i < list.Length; i++) array.SetValue(list[i], i);
            ConstructorInfo roomCtor = EditorType("SoloRoomDefinition").GetConstructors().First(c => c.GetParameters().Length == 6);
            return roomCtor.Invoke(new[] { F(room, "Id"), ((Vector2)F(room, "Origin")).x, F(room, "Width"), array, F(room, "Openings"), F(room, "RequiredJumps") });
        }

        static IEnumerable<object> Elements(object room) => ((IEnumerable)F(room, "Elements")).Cast<object>();

        // Room 8 with its Geyser replaced (same name, and the same box unless given); the rest of the room unchanged.
        internal static object WithGeyser(object settings, Vector2? position = null, Vector2? size = null)
        {
            object room = Room8();
            return Rebuild(room, Elements(room).Select(e => (string)F(e, "Name") != "Geyser" ? e
                : Element("Geyser", "Geyser", position ?? (Vector2)F(e, "Position"), size ?? (Vector2)F(e, "Size"), settings: settings)));
        }

        static object WithExtra(object room, object element) => Rebuild(room, Elements(room).Append(element));

        static List<string> Geyser(object room) => Rule("ValidateGeyser", "FIX", room, Motor());
        static List<string> Envelope(object room, object routes) => Rule("ValidateGeyserEnvelope", "FIX", room, Motor(), Gravity(), routes);

        static void AssertRejected(List<string> errors, params string[] fragments)
        {
            TestContext.Out.WriteLine(string.Join("\n", errors));
            Assert.AreEqual(1, errors.Count, string.Join("\n", errors));
            foreach (string fragment in fragments) StringAssert.Contains(fragment, errors[0]);
        }

        [Test] public void Room8sGeyser_PassesValidateGeyser() => CollectionAssert.IsEmpty(Geyser(Room8()));
        [Test] public void Room8sGeyser_PassesTheEnvelope_WithItsRoutes() => CollectionAssert.IsEmpty(Envelope(Room8(), Routes8()));
        [Test] public void AGeyserWithDefaultSettings_ReadsAsTheDefaults() => CollectionAssert.IsEmpty(Geyser(WithGeyser(GeyserSettings())));

        // ---------- ValidateGeyser: the cycle ----------

        [Test] public void ATellOfSix_Passes() => CollectionAssert.IsEmpty(Geyser(WithGeyser(GeyserSettings(tell: 6))));
        [Test] public void ATellBelowSix_IsRejected() => AssertRejected(Geyser(WithGeyser(GeyserSettings(tell: 5))), "Geyser", "tell 5", "below 6");
        [Test] public void AnEruptOfZero_IsRejected() => AssertRejected(Geyser(WithGeyser(GeyserSettings(erupt: 0))), "Geyser", "erupt 0", "below 1");
        [Test] public void TellPlusErupt_NotShorterThanThePeriod_IsRejected() =>
            AssertRejected(Geyser(WithGeyser(GeyserSettings(tell: 30, erupt: 70, period: 100))), "Geyser", "100", "shorter than its period");
        [Test] public void TellPlusErupt_OneShorterThanThePeriod_Passes() => CollectionAssert.IsEmpty(Geyser(WithGeyser(GeyserSettings(tell: 30, erupt: 69, period: 100))));
        [Test] public void AGeyserThatIsntPeriodic_IsRejected() =>
            AssertRejected(Geyser(WithGeyser(GeyserSettings(repeat: TrapRepeatMode.Once))), "Geyser", "Once", "Periodic");

        // ---------- ValidateGeyser: the vent and its direction (R6) ----------

        [Test] public void AVentAboveTheFloor_SitsInNoFace_IsRejected() =>
            AssertRejected(Geyser(WithGeyser(GeyserSettings(), new Vector2(9f, .15f))), "Geyser", "no Floor or Ceiling face");

        [Test] public void ADownGeyser_InAFloor_IsRejected() =>
            AssertRejected(Geyser(WithGeyser(GeyserSettings(GeyserDirection.Down))), "Geyser", "Down", "doesn't match");

        [Test]
        public void ADownGeyser_InTheCeilingsUnderside_Passes()
        {
            // The main ceiling's underside is y 7.0: a vent flush in it at x 9, its column y [5.5, 7.0].
            object room = WithGeyser(GeyserSettings(GeyserDirection.Down), new Vector2(9f, 7.15f));
            CollectionAssert.IsEmpty(Geyser(room));
        }

        [Test] public void AnUpGeyser_InACeiling_IsRejected() =>
            AssertRejected(Geyser(WithGeyser(GeyserSettings(), new Vector2(9f, 7.15f))), "Geyser", "Up", "doesn't match");

        // ---------- ValidateGeyser: the column is clear of solids, with the cat's height above it (R5) ----------
        // At x 7 the column is x [6.5, 7.5], under the Ledge (underside y 3.0). Height 2.3: the top plus the cat's 0.56 is
        // 2.86, clear. Height 2.6: 3.16 reaches the Ledge (R5). Height 3.2: the column itself reaches it.

        [Test] public void AColumnClearOfTheLedge_WithTheCatsHeight_Passes() =>
            CollectionAssert.IsEmpty(Geyser(WithGeyser(GeyserSettings(height: 2.3f), new Vector2(7f, -.15f))));

        [Test] public void AColumnWhoseClearanceReachesTheLedge_IsRejected() =>
            AssertRejected(Geyser(WithGeyser(GeyserSettings(height: 2.6f), new Vector2(7f, -.15f))), "Geyser", "overlaps Ledge");

        [Test] public void AColumnReachingTheLedge_IsRejected() =>
            AssertRejected(Geyser(WithGeyser(GeyserSettings(height: 3.2f), new Vector2(7f, -.15f))), "Geyser", "overlaps Ledge");

        // ---------- ValidateGeyserEnvelope (R2) ----------

        [Test] public void AnEnvelopeOutThroughTheTop_IsRejected() =>
            AssertRejected(Envelope(WithGeyser(GeyserSettings(speed: 20f)), Routes8()), "Geyser", "launch envelope", "outside the room's frame");

        [Test] public void AnEnvelopeOutThroughTheSide_IsRejected() =>
            AssertRejected(Envelope(WithGeyser(GeyserSettings(), new Vector2(18.5f, -.15f)), Routes8()), "Geyser", "launch envelope", "outside the room's frame");

        [Test] public void ADisguisedHazardInTheEnvelope_WithoutADyingRoute_IsRejected() =>
            AssertRejected(Envelope(Room8(), null), "Ceiling_Spikes", "disguised", "Geyser", "betrayal");

        [Test]
        public void AnHonestHazardInTheEnvelope_NeedsNoRoute()
        {
            object room = WithExtra(Room8(), Element("Hazard", "Honest_Spikes", new Vector2(6f, 4.6f), new Vector2(.5f, .3f)));
            CollectionAssert.IsEmpty(Envelope(room, Routes8()));
        }

        [Test]
        public void ADisguisedArrowInTheEnvelope_WithoutADyingRoute_IsRejected()
        {
            Type arrowLane = EditorType("ArrowLane");
            object lane = arrowLane.GetConstructors().First(c => c.GetParameters().Length == 9).Invoke(new object[] { ArrowDirection.Left, 4.5f, 5.2f, .8f, .16f, .3f, 6, true, false });
            ConstructorInfo withLane = EditorType("SoloRoomTrapSettings").GetConstructors().Single(c => c.GetParameters().Length == 2 && c.GetParameters()[0].ParameterType == arrowLane);
            object settings = withLane.Invoke(new[] { lane, Timing(TrapRepeatMode.Once, 1, 0) });
            object room = WithExtra(Room8(), Element("Arrow", "Hidden_Arrow", new Vector2(19.75f, 4.5f), new Vector2(.5f, .4f), new Vector2(15f, 3.5f), new Vector2(.5f, 7f), settings));
            List<string> errors = Envelope(room, Routes8());
            TestContext.Out.WriteLine(string.Join("\n", errors));
            Assert.IsTrue(errors.Any(e => e.Contains("Hidden_Arrow") && e.Contains("disguised")), string.Join("\n", errors));
        }

        [Test]
        public void AGeyser_IsNeverAKillVolume()
        {
            MethodInfo kill = Validator.GetMethod("KillVolumes", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(kill, "KillVolumes not found");
            object geyser = Elements(Room8()).Single(e => (string)F(e, "Name") == "Geyser");
            CollectionAssert.IsEmpty(((IEnumerable)kill.Invoke(null, new[] { geyser, (object)Vector2.zero })).Cast<object>().ToList());
        }

        // ---------- ValidateBand (levels 11+ only) ----------

        LevelListConfig ElevenLevels()
        {
            list = ScriptableObject.CreateInstance<LevelListConfig>();
            LevelEntry[] entries = Enumerable.Range(1, 11).Select(i => new LevelEntry { Id = $"B{i:00}", SceneName = $"Level_B{i:00}", DisplayName = i.ToString() }).ToArray();
            typeof(LevelListConfig).GetField("levels", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(list, entries);
            return list;
        }

        [Test]
        public void AGeyser_InLevels1To10_IsAnErrorNamingTheLevel()
        {
            LevelListConfig levels = ElevenLevels();
            foreach (string id in new[] { "B01", "B10" })
            {
                List<string> errors = Rule("ValidateBand", id, Room8(), levels);
                Assert.AreEqual(1, errors.Count, string.Join("\n", errors));
                StringAssert.Contains($"{id}: geyser 'Geyser' in level {int.Parse(id.Substring(1))}", errors[0]);
                StringAssert.Contains("D-088", errors[0]);
            }
        }

        [Test] public void TheSameGeyser_InLevel11_Passes() => CollectionAssert.IsEmpty(Rule("ValidateBand", "B11", Room8(), ElevenLevels()));

        [Test] public void AGeyserInAnUnlistedRoom_IsExempt() => CollectionAssert.IsEmpty(Rule("ValidateBand", "TrapLab8", Room8(), ElevenLevels()));
    }
}
