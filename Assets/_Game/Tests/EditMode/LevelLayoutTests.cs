using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Parallax.Gameplay.Levels;
using UnityEditor;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    // PAX-051 (D-066): tests the level authoring pipeline's data layer (LevelLayouts registry,
    // LevelListConfig agreement, LevelLayoutValidator). Everything under test lives in the
    // Editor assembly, which this test assembly does not reference, so access goes through
    // reflection, matching the existing pattern in SoloRoomsLayoutTests.cs. Does not modify or
    // duplicate any existing test.
    public sealed class LevelLayoutTests
    {
        static readonly Type LayoutsType = Type.GetType("Parallax.Editor.Levels.LevelLayouts, Parallax.Editor");
        static readonly Type ValidatorType = Type.GetType("Parallax.Editor.Setup.LevelLayoutValidator, Parallax.Editor");
        static readonly Type FixturesType = Type.GetType("Parallax.Editor.Setup.LevelLayoutValidatorFixtures, Parallax.Editor");
        static readonly Type SoloLayoutType = Type.GetType("Parallax.Editor.Setup.SoloRoomsLayout, Parallax.Editor");
        static readonly Type BuilderType = Type.GetType("Parallax.Editor.Setup.SoloRoomBuilder, Parallax.Editor");

        static LevelListConfig Config()
        {
            var config = AssetDatabase.LoadAssetAtPath<LevelListConfig>("Assets/_Game/Data/LevelListConfig.asset");
            Assert.NotNull(config, "LevelListConfig asset must exist (run PARALLAX/Setup/Level List first).");
            return config;
        }

        static IDictionary Registry()
        {
            Assert.NotNull(LayoutsType, "Parallax.Editor.Levels.LevelLayouts not found.");
            object byId = LayoutsType.GetField("ById", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            return (IDictionary)byId;
        }

        static object RoomFor(string id)
        {
            IDictionary registry = Registry();
            Assert.IsTrue(registry.Contains(id), id + " is not registered in LevelLayouts.");
            return registry[id];
        }

        static string[] Validate(string id, object room)
        {
            Assert.NotNull(ValidatorType, "Parallax.Editor.Setup.LevelLayoutValidator not found.");
            MethodInfo method = ValidatorType.GetMethod("Validate", BindingFlags.Public | BindingFlags.Static);
            var errors = (IList)method.Invoke(null, new[] { id, room });
            return errors.Cast<string>().ToArray();
        }

        // ---------- 1. Registry <-> LevelListConfig ----------

        [Test]
        public void Registry_IdsAreUniqueAndMatchLevelListConfigBothWays()
        {
            IDictionary registry = Registry();
            string[] registryIds = registry.Keys.Cast<string>().ToArray();
            string[] listedIds = Config().Levels.Select(l => l.Id).ToArray();

            Assert.AreEqual(listedIds.Length, listedIds.Distinct().Count(), "LevelListConfig ids must be unique.");
            foreach (string id in listedIds) Assert.Contains(id, registryIds, $"{id} is listed in LevelListConfig but not registered in LevelLayouts.");
            foreach (string id in registryIds) Assert.Contains(id, listedIds, $"{id} is registered in LevelLayouts but not listed in LevelListConfig.");
        }

        [Test]
        public void Registry_EverySceneNameIsInBuildSettings()
        {
            string[] scenes = EditorBuildSettings.scenes.Select(s => Path.GetFileNameWithoutExtension(s.path)).ToArray();
            foreach (LevelEntry entry in Config().Levels)
                Assert.Contains(entry.SceneName, scenes, $"{entry.Id}: scene '{entry.SceneName}' is not in Build Settings.");
        }

        // ---------- 2. One checkpoint, one door ----------

        [Test]
        public void EachListedLayout_HasExactlyOneCheckpointAndOneDoor()
        {
            foreach (LevelEntry entry in Config().Levels)
            {
                Element[] elements = Elements(RoomFor(entry.Id));
                Assert.AreEqual(1, elements.Count(e => e.Kind == "Checkpoint"), entry.Id + " checkpoint count");
                Assert.AreEqual(1, elements.Count(e => e.Kind == "Door"), entry.Id + " door count");
            }
        }

        // ---------- 3. LevelLayoutValidator passes every listed layout ----------

        [Test]
        public void EachListedLayout_PassesLevelLayoutValidator()
        {
            foreach (LevelEntry entry in Config().Levels)
            {
                string[] errors = Validate(entry.Id, RoomFor(entry.Id));
                Assert.IsEmpty(errors, entry.Id + ":\n" + string.Join("\n", errors));
            }
        }

        // ---------- 4. Parity with SoloRoomsLayout's four rooms ----------

        [Test]
        public void Parity_ValidatorAgreesWithExistingTestsOnSoloRoomsLayoutRooms()
        {
            foreach (object room in SoloRooms())
            {
                int id = (int)Field(room, "Id");
                string[] errors = Validate("SoloRoomsLayout room " + id, room);
                Assert.IsEmpty(errors, "SoloRoomsLayout room " + id + ":\n" + string.Join("\n", errors));
            }
        }

        // ---------- 5. Negative cases ----------

        [Test] public void Negative_TwoDoorsFailsWithLevelIdAndDoorMention() => AssertFails("TwoDoors", "door");
        [Test] public void Negative_DoorOutsideBoundsFailsWithLevelIdAndFrameMention() => AssertFails("DoorOutsideBounds", "frame");
        [Test] public void Negative_JumpBeyondReachFailsWithLevelIdAndReachMention() => AssertFails("JumpBeyondReach", "reach");
        [Test] public void Negative_SlackBelowTwelveTicksFailsWithLevelIdAndSlackMention() => AssertFails("SlackBelowTwelveTicks", "slack");
        [Test] public void Negative_RevealLeadBelowSixFailsWithLevelIdAndRevealLeadMention() => AssertFails("RevealLeadBelowSix", "reveal lead");
        [Test] public void Negative_ChainCycleFailsWithLevelIdAndCycleMention() => AssertFails("ChainCycle", "chain cycle");

        static void AssertFails(string fixtureName, string expectedSubstring)
        {
            Assert.NotNull(FixturesType, "Parallax.Editor.Setup.LevelLayoutValidatorFixtures not found.");
            object room = FixturesType.GetMethod(fixtureName, BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
            string levelId = "FIX_" + fixtureName;
            string[] errors = Validate(levelId, room);
            Assert.IsNotEmpty(errors, fixtureName + " should fail validation.");
            Assert.IsTrue(errors.All(e => e.StartsWith(levelId + ":")), fixtureName + ": every error must name the level id.\n" + string.Join("\n", errors));
            Assert.IsTrue(errors.Any(e => e.ToLowerInvariant().Contains(expectedSubstring)), fixtureName + ": expected an error mentioning '" + expectedSubstring + "'.\n" + string.Join("\n", errors));
        }

        // ---------- 6. Split fidelity ----------

        static readonly (string levelId, int soloRoomIndex)[] SeedPairs = { ("L001", 0), ("L002", 1), ("L003", 2), ("L004", 3) };

        // PAX-073 (D-074, R7): the only L00N trigger boxes allowed to differ from SoloRoomsLayout.
        static readonly (string levelId, string name)[] TriggerBoxExceptions = { ("L001", "Spikes_A"), ("L002", "Lift") };
        // PAX-078 (D-076, R9): the approved 50 Hz fix moves these elements, so only their Position may
        // differ from SoloRoomsLayout's, and it must differ (no stale entries).
        static readonly (string levelId, string name)[] PositionExceptions = { ("L004", "Flip_A") };

        [Test]
        public void SplitFidelity_L00NEqualsSoloRoomsLayoutRoomNUpToTranslationAndRoomId()
        {
            object[] soloRooms = SoloRooms().Cast<object>().ToArray();
            foreach ((string levelId, int index) in SeedPairs)
            {
                object levelRoom = RoomFor(levelId);
                object soloRoom = soloRooms[index];

                Assert.AreEqual(0, (int)Field(levelRoom, "Id"), levelId + " room id");
                Assert.AreEqual(Vector2.zero, (Vector2)Field(levelRoom, "Origin"), levelId + " origin");
                Assert.AreEqual((float)Field(soloRoom, "Width"), (float)Field(levelRoom, "Width"), .001f, levelId + " width");

                // Element positions are local offsets from Origin already (see SoloRoomBuilder.
                // BuildElement: position = room.Origin + e.Position), so translating Origin to
                // (0,0) requires no change to any element's stored position/size.
                Element[] soloElements = Elements(soloRoom), levelElements = Elements(levelRoom);
                Assert.AreEqual(soloElements.Length, levelElements.Length, levelId + " element count");
                for (int i = 0; i < soloElements.Length; i++)
                {
                    Assert.AreEqual(soloElements[i].Name, levelElements[i].Name, levelId + " element " + i + " name");
                    Assert.AreEqual(soloElements[i].Kind, levelElements[i].Kind, levelId + " element " + i + " kind");
                    if (PositionExceptions.Contains((levelId, levelElements[i].Name)))
                        Assert.AreNotEqual(soloElements[i].Position, levelElements[i].Position,
                            levelId + " " + levelElements[i].Name + " is in PositionExceptions but its position still equals SoloRoomsLayout's (stale entry).");
                    else
                        Assert.AreEqual(soloElements[i].Position, levelElements[i].Position, levelId + " element " + i + " position");
                    Assert.AreEqual(soloElements[i].Size, levelElements[i].Size, levelId + " element " + i + " size");
                    // PAX-073 (D-074, R7): the approved trigger-coverage fixes change only these traps'
                    // trigger boxes, so only their SecondaryPosition/SecondarySize may differ, and they
                    // must differ (no stale entries).
                    if (TriggerBoxExceptions.Contains((levelId, levelElements[i].Name)))
                    {
                        Assert.IsFalse(soloElements[i].SecondaryPosition == levelElements[i].SecondaryPosition && soloElements[i].SecondarySize == levelElements[i].SecondarySize,
                            levelId + " " + levelElements[i].Name + " is in TriggerBoxExceptions but its trigger box still equals SoloRoomsLayout's (stale entry).");
                        continue;
                    }
                    Assert.AreEqual(soloElements[i].SecondaryPosition, levelElements[i].SecondaryPosition, levelId + " element " + i + " secondary position");
                    Assert.AreEqual(soloElements[i].SecondarySize, levelElements[i].SecondarySize, levelId + " element " + i + " secondary size");
                }
                foreach ((string _, string exceptionName) in TriggerBoxExceptions.Where(x => x.levelId == levelId))
                    Assert.IsTrue(levelElements.Any(e => e.Name == exceptionName), levelId + " has no element " + exceptionName + " listed in TriggerBoxExceptions.");
                foreach ((string _, string exceptionName) in PositionExceptions.Where(x => x.levelId == levelId))
                    Assert.IsTrue(levelElements.Any(e => e.Name == exceptionName), levelId + " has no element " + exceptionName + " listed in PositionExceptions.");
            }
        }

        // ---------- 7. Baked bounds translate with the room ----------

        [Test]
        public void BakedBounds_L00NEqualsSoloRoomsLayoutRoomNBoundsTranslated()
        {
            Assert.NotNull(BuilderType, "Parallax.Editor.Setup.SoloRoomBuilder not found.");
            MethodInfo computeBounds = BuilderType.GetMethod("ComputeRoomBounds", BindingFlags.Public | BindingFlags.Static);
            object[] soloRooms = SoloRooms().Cast<object>().ToArray();
            const float margin = 2f;

            foreach ((string levelId, int index) in SeedPairs)
            {
                object levelRoom = RoomFor(levelId);
                object soloRoom = soloRooms[index];
                var soloBounds = (Bounds)computeBounds.Invoke(null, new object[] { soloRoom, margin });
                var levelBounds = (Bounds)computeBounds.Invoke(null, new object[] { levelRoom, margin });
                Vector2 translation = (Vector2)Field(levelRoom, "Origin") - (Vector2)Field(soloRoom, "Origin");

                Assert.That(levelBounds.center.x, Is.EqualTo(soloBounds.center.x + translation.x).Within(.001f), levelId + " bounds centre x");
                Assert.That(levelBounds.center.y, Is.EqualTo(soloBounds.center.y + translation.y).Within(.001f), levelId + " bounds centre y");
                Assert.That(levelBounds.size.x, Is.EqualTo(soloBounds.size.x).Within(.001f), levelId + " bounds size x");
                Assert.That(levelBounds.size.y, Is.EqualTo(soloBounds.size.y).Within(.001f), levelId + " bounds size y");
            }
        }

        static IEnumerable SoloRooms()
        {
            Assert.NotNull(SoloLayoutType, "Parallax.Editor.Setup.SoloRoomsLayout not found.");
            return (IEnumerable)SoloLayoutType.GetField("Rooms", BindingFlags.Public | BindingFlags.Static).GetValue(null);
        }

        static object Field(object value, string name) => value.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance).GetValue(value);
        static Element[] Elements(object room) => ((IEnumerable)Field(room, "Elements")).Cast<object>().Select(e => new Element(e)).ToArray();

        readonly struct Element
        {
            readonly object value;
            public string Kind => Field(value, "Kind").ToString();
            public string Name => (string)Field(value, "Name");
            public Vector2 Position => (Vector2)Field(value, "Position");
            public Vector2 Size => (Vector2)Field(value, "Size");
            public Vector2 SecondaryPosition => (Vector2)Field(value, "SecondaryPosition");
            public Vector2 SecondarySize => (Vector2)Field(value, "SecondarySize");
            public Element(object value) { this.value = value; }
        }
    }
}
