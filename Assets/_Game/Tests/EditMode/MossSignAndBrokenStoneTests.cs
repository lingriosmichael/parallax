using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Parallax.Tests.EditMode
{
    // PAX-106 (D-110 amendment 4 (2), D-114, ruling R10): moss means grip, and sides are broken stone, on every level and Trap
    // Lab room, checked from the room's plan (SoloRoomSkin.CheckMossSign, CheckSideStrips): no scene needed.
    public sealed class MossSignAndBrokenStoneTests
    {
        static readonly Type Skin = Type.GetType("Parallax.Editor.Setup.SoloRoomSkin, Parallax.Editor");

        static IEnumerable<TestCaseData> Rooms()
        {
            foreach (DictionaryEntry entry in (IDictionary)Type.GetType("Parallax.Editor.Levels.LevelLayouts, Parallax.Editor").GetField("ById").GetValue(null))
                yield return new TestCaseData((string)entry.Key, entry.Value).SetName("Room:" + entry.Key);
            var lab = (IList)Type.GetType("Parallax.Editor.Setup.TrapLabLayout, Parallax.Editor").GetField("Rooms").GetValue(null);
            for (int i = 0; i < lab.Count; i++) yield return new TestCaseData("TrapLab" + i, lab[i]).SetName("Room:TrapLab" + i);
        }

        static List<string> Check(string method, string id, object room) =>
            (List<string>)Skin.GetMethod(method).Invoke(null, new[] { id, room });

        [TestCaseSource(nameof(Rooms))]
        public void GripFacesAreMossy_AndOrdinaryFacesKeepMossAndIvyInTheirTopBand(string id, object room) =>
            CollectionAssert.IsEmpty(Check("CheckMossSign", id, room));

        [TestCaseSource(nameof(Rooms))]
        public void EverySideFace_IsBrokenStone_WithinItsOutline(string id, object room) =>
            CollectionAssert.IsEmpty(Check("CheckSideStrips", id, room));

        // The sign's positive half has to be exercised somewhere: L001's falling grip walls and room 14's grip walls.
        [Test]
        public void TheGripRooms_HaveGripMoss()
        {
            object lab14 = ((IList)Type.GetType("Parallax.Editor.Setup.TrapLabLayout, Parallax.Editor").GetField("Rooms").GetValue(null))[14];
            object l001 = ((IDictionary)Type.GetType("Parallax.Editor.Levels.LevelLayouts, Parallax.Editor").GetField("ById").GetValue(null))["L001"];
            foreach (object room in new[] { lab14, l001 })
            {
                var plan = (IEnumerable)Skin.GetMethod("PlanDressing").Invoke(null, new[] { room });
                int moss = plan.Cast<object>().Count(p => ((string)p.GetType().GetField("Item2").GetValue(p)).StartsWith("GripMoss_"));
                Assert.Greater(moss, 10, "grip moss placed");
            }
        }

        [Test]
        public void TheTopBand_IsThirtyPercentOfTheFace_AtMostSixTenths()
        {
            Func<float, float> band = h => (float)Skin.GetMethod("TopBand").Invoke(null, new object[] { h });
            Assert.AreEqual(.3f, band(1f), 1e-5f);
            Assert.AreEqual(.6f, band(2f), 1e-5f);
            Assert.AreEqual(.6f, band(5f), 1e-5f);
        }
    }
}
