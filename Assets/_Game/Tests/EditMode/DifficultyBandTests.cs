using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Parallax.Gameplay.Levels;
using UnityEditor;
using UnityEngine;
using static Parallax.Tests.EditMode.PrecisionTestApi;

namespace Parallax.Tests.EditMode
{
    // PAX-076 (D-083) §6: the band (D-065). The level number is the place in LevelListConfig plus one (R5).
    public sealed class DifficultyBandTests
    {
        LevelListConfig list;

        [TearDown] public void Cleanup() { if (list != null) Object.DestroyImmediate(list); }

        // Eleven listed levels, "B01".."B11".
        LevelListConfig ElevenLevels()
        {
            list = ScriptableObject.CreateInstance<LevelListConfig>();
            LevelEntry[] entries = Enumerable.Range(1, 11).Select(i => new LevelEntry { Id = $"B{i:00}", SceneName = $"Level_B{i:00}", DisplayName = i.ToString() }).ToArray();
            typeof(LevelListConfig).GetField("levels", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(list, entries);
            return list;
        }

        [Test]
        public void APrecisionSection_InLevels1To10_IsAnErrorNamingTheLevel()
        {
            LevelListConfig levels = ElevenLevels();
            foreach (string id in new[] { "B01", "B05", "B10" })
            {
                List<string> errors = Rule("ValidateBand", id, Fixture("JumpRoom", "both"), levels);
                Assert.AreEqual(1, errors.Count, id);
                StringAssert.Contains($"{id}: precision section 'Gap' in level {int.Parse(id.Substring(1))}", errors[0]);
            }
        }

        [Test]
        public void TheSameSection_InLevel11_Passes() =>
            CollectionAssert.IsEmpty(Rule("ValidateBand", "B11", Fixture("JumpRoom", "both"), ElevenLevels()));

        [Test]
        public void ATrapLabRoom_IsExempt()
        {
            object room5 = ((IList)System.Type.GetType("Parallax.Editor.Setup.TrapLabLayout, Parallax.Editor").GetField("Rooms").GetValue(null))[5];
            CollectionAssert.IsEmpty(Rule("ValidateBand", "TrapLab5", room5, ElevenLevels()));
            CollectionAssert.IsEmpty(Rule("ValidateBand", "TrapLab5", room5, AssetDatabase.LoadAssetAtPath<LevelListConfig>("Assets/_Game/Data/LevelListConfig.asset")));
        }

        [Test]
        public void ARoomWithoutSections_NeedsNoList() =>
            CollectionAssert.IsEmpty(Rule("ValidateBand", "B01", Fixture("JumpRoom", "none"), null));

        // R5: the exemption is "not listed", so no dev room may ever be listed. Every listed id is a shipped layout
        // (LevelLayouts), and none is a Trap Lab room's id.
        [Test]
        public void NoDevRoomIsAListedLevel()
        {
            var levels = AssetDatabase.LoadAssetAtPath<LevelListConfig>("Assets/_Game/Data/LevelListConfig.asset");
            Assert.NotNull(levels, "LevelListConfig asset must exist.");
            var layouts = (IDictionary)System.Type.GetType("Parallax.Editor.Levels.LevelLayouts, Parallax.Editor").GetField("ById").GetValue(null);
            int labRooms = ((IList)System.Type.GetType("Parallax.Editor.Setup.TrapLabLayout, Parallax.Editor").GetField("Rooms").GetValue(null)).Count;
            foreach (string id in levels.OrderedIds())
            {
                Assert.IsTrue(layouts.Contains(id), $"listed level {id} has no LevelLayouts entry");
                StringAssert.DoesNotContain("TrapLab", id);
                StringAssert.DoesNotContain("Sandbox", id);
                for (int i = 0; i < labRooms; i++) Assert.AreNotEqual("TrapLab" + i, id);
            }
        }
    }
}
