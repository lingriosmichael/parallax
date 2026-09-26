using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    // PAX-059 (§2.1 item 6, D-085; half B amendment 1 §1.1): the ten-level direction mix. Each level's shape is declared
    // here and checked against its layout: a climb ends a storey (≥ 2.5 u) above its start, a descent a storey below, and a
    // plain left-to-right level (and only one) starts in the left third and ends in the right third on the same storey.
    // "Doubles back" (heads away from the door first) is read from the route and stays declared. Where the start
    // sits is read from the layout: "middle" is the middle third of the width, so L004 (x 9.5 of 32) doesn't count.
    public sealed class Band1DirectionMixTests
    {
        const float Storey = 2.5f;

        // id, plain left to right, climbs, descends, doubles back (heads away from the door first).
        static readonly (string id, bool plain, bool climb, bool descend, bool doublesBack)[] Shapes = {
            ("L001", true, false, false, false),
            ("L002", false, true, false, true),
            ("L003", false, true, false, false),
            ("L004", false, true, false, true),
            ("L005", false, false, true, false),
            ("L006", false, false, true, true),   // S1 down to the ground
            ("L007", false, true, false, false),
            ("L008", false, false, true, false),
            ("L009", false, true, false, true),
            ("L010", false, true, false, true),
        };

        static object Room(string id) => RouteValidatorTests.Room(id);
        static object Field(object o, string name) => o.GetType().GetField(name).GetValue(o);

        static (Vector2 start, Vector2 door, float width) Ends(string id)
        {
            object room = Room(id);
            Assert.NotNull(room, id + " is not in LevelLayouts.");
            Vector2 start = default, door = default;
            foreach (object e in (IEnumerable)Field(room, "Elements"))
            {
                string kind = Field(e, "Kind").ToString();
                if (kind == "Checkpoint") start = (Vector2)Field(e, "Position");
                if (kind == "Door") door = (Vector2)Field(e, "Position");
            }
            return (start, door, (float)Field(room, "Width"));
        }

        [Test]
        public void EachDeclaredShape_MatchesItsLayout()
        {
            foreach (var s in Shapes)
            {
                (Vector2 start, Vector2 door, float width) = Ends(s.id);
                float dy = door.y - start.y;
                Assert.AreEqual(s.climb, dy >= Storey, $"{s.id}: declared climb {s.climb}, but the door is {dy:F2} u above the start.");
                Assert.AreEqual(s.descend, dy <= -Storey, $"{s.id}: declared descent {s.descend}, but the door is {dy:F2} u above the start.");
                bool plain = start.x < width / 3f && door.x > width * 2f / 3f && Mathf.Abs(dy) < Storey;
                Assert.AreEqual(s.plain, plain, $"{s.id}: declared plain left to right {s.plain}, but it starts at {start} and ends at {door} ({width} wide).");
            }
        }

        [Test]
        public void TheTenLevels_HoldTheDirectionMix()
        {
            Assert.AreEqual(10, Shapes.Length);
            int plain = Shapes.Count(s => s.plain), climb = Shapes.Count(s => s.climb), descend = Shapes.Count(s => s.descend), back = Shapes.Count(s => s.doublesBack);
            int middleOrRight = Shapes.Count(s => { (Vector2 start, _, float width) = Ends(s.id); return start.x >= width / 3f; });
            string table = $"plain {plain} (≤ 3), climb {climb} (≥ 2), descend {descend} (≥ 2), start middle or right {middleOrRight} (≥ 3), doubles back {back} (≥ 2)";
            TestContext.Out.WriteLine(table);
            Assert.IsTrue(plain <= 3 && climb >= 2 && descend >= 2 && middleOrRight >= 3 && back >= 2, table);
        }

        [Test]
        public void L004_DoesNotCountAsAMiddleStart()
        {
            (Vector2 start, _, float width) = Ends("L004");
            Assert.Less(start.x, width / 3f, "L004 starts at x 9.5 of 32: the left third (amendment 1 §1.1).");
        }
    }
}
