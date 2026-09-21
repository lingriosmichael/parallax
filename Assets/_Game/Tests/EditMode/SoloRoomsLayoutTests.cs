using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    public sealed class SoloRoomsLayoutTests
    {
        static readonly Type LayoutType = Type.GetType("Parallax.Editor.Setup.SoloRoomsLayout, Parallax.Editor");

        [Test]
        public void SpawnSafety_CheckpointsAreOutsideHazardsAndTriggers()
        {
            foreach (object room in Rooms())
            {
                Vector2 checkpoint = ElementPosition(room, "Checkpoint");
                foreach (object element in Elements(room))
                {
                    string kind = Field(element, "Kind").ToString();
                    if (kind != "Hazard" && kind != "HiddenSpikes" && kind != "DoorRetreat") continue;
                    Vector2 position = (Vector2)Field(element, "Position");
                    Vector2 size = (Vector2)Field(element, "Size");
                    Assert.IsFalse(new Rect(position - size * .5f, size).Overlaps(new Rect(checkpoint - new Vector2(.5f, .28f), new Vector2(1f, .56f))), $"{kind} overlaps checkpoint.");
                }
            }
        }

        [Test]
        public void Containment_AllCheckpointDoorsAndTrapsStayInsideTheirRooms()
        {
            foreach (object room in Rooms())
            foreach (object element in Elements(room))
            {
                int roomId = (int)Field(room, "Id");
                Vector2 p = (Vector2)Field(element, "Position");
                string kind = Field(element, "Kind").ToString();
                if (kind == "Floor" || kind == "Ceiling" || kind == "Wall" || kind == "PitBottom") continue;
                Assert.That(p.x, Is.InRange(0f, 24f));
                float minY = roomId == 0 ? -4f : 0f;
                float maxY = roomId == 3 ? 11f : 10f;
                Assert.That(p.y, Is.InRange(minY, maxY));
            }
        }

        [Test]
        public void Separation_EachRoomHasOneCheckpointAndDoorAndOriginsAreSpaced()
        {
            object previous = null;
            foreach (object room in Rooms())
            {
                int checkpoints = 0, doors = 0;
                foreach (object element in Elements(room)) { string kind = Field(element, "Kind").ToString(); if (kind == "Checkpoint") checkpoints++; if (kind == "Door") doors++; }
                Assert.AreEqual(1, checkpoints); Assert.AreEqual(1, doors);
                if (previous != null) Assert.GreaterOrEqual(((Vector2)Field(room, "Origin")).x - ((Vector2)Field(previous, "Origin")).x - 24f, 10f);
                previous = room;
            }
        }

        [Test]
        public void Containment_PitAndRecessHaveClosedSideWallsAndCaps()
        {
            object room1 = Room(0);
            AssertElement(room1, "PitWall_Left", new Vector2(13.5f, -2.5f), new Vector2(1f, 3f));
            AssertElement(room1, "PitWall_Right", new Vector2(17.5f, -2.5f), new Vector2(1f, 3f));
            AssertWallsOutsideOpening(room1, "PitWall_Left", "PitWall_Right", 14f, 17f);
            object room4 = Room(3);
            AssertElement(room4, "RecessWall_Left", new Vector2(10.5f, 9f), new Vector2(1f, 2f));
            AssertElement(room4, "RecessWall_Right", new Vector2(14.5f, 9f), new Vector2(1f, 2f));
            AssertElement(room4, "RecessCap", new Vector2(12.5f, 10.5f), new Vector2(3f, 1f));
            AssertWallsOutsideOpening(room4, "RecessWall_Left", "RecessWall_Right", 11f, 14f);
        }

        [Test]
        public void Reachability_ThreeUnitSolutionJumpsAndTwoUnitStepFitMeasuredLimits()
        {
            const float gravity = 30f, jumpHeight = 3.2f, speed = 6f;
            float airTime = 2f * Mathf.Sqrt(2f * gravity * jumpHeight) / gravity;
            float reach = speed * airTime;
            Assert.LessOrEqual(3f, .6f * reach);
            Assert.LessOrEqual(2f, .8f * jumpHeight);
        }

        [Test]
        public void GreyboxFallback_CreatesAOneUnitPointFilteredFullRectSprite()
        {
            Type utility = Type.GetType("Parallax.Editor.Setup.SetupUtility, Parallax.Editor");
            Assert.NotNull(utility);
            Sprite sprite = (Sprite)utility.GetMethod("GetGreyboxSprite", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
            Assert.NotNull(sprite);
            Assert.AreEqual(1f, sprite.bounds.size.x, .001f);
            TextureImporter importer = AssetImporter.GetAtPath("Assets/_Game/Art/Greybox/Greybox_Square.png") as TextureImporter;
            Assert.NotNull(importer);
            Assert.AreEqual(FilterMode.Point, importer.filterMode);
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            Assert.AreEqual(SpriteMeshType.FullRect, settings.spriteMeshType);
        }

        static IEnumerable Rooms()
        {
            Assert.NotNull(LayoutType, "SoloRoomsLayout must be available from Parallax.Editor.");
            return (IEnumerable)LayoutType.GetField("Rooms", BindingFlags.Public | BindingFlags.Static).GetValue(null);
        }
        static IEnumerable Elements(object room) => (IEnumerable)Field(room, "Elements");
        static object Room(int id) { foreach (object room in Rooms()) if ((int)Field(room, "Id") == id) return room; throw new AssertionException($"Missing room {id}"); }
        static void AssertElement(object room, string name, Vector2 position, Vector2 size) { foreach (object element in Elements(room)) if ((string)Field(element, "Name") == name) { Assert.AreEqual(position, Field(element, "Position")); Assert.AreEqual(size, Field(element, "Size")); return; } throw new AssertionException($"Missing {name}"); }
        static void AssertWallsOutsideOpening(object room, string leftName, string rightName, float openingLeft, float openingRight) { Rect left = Bounds(Element(room, leftName)); Rect right = Bounds(Element(room, rightName)); Assert.LessOrEqual(left.xMax, openingLeft); Assert.GreaterOrEqual(right.xMin, openingRight); }
        static object Element(object room, string name) { foreach (object element in Elements(room)) if ((string)Field(element, "Name") == name) return element; throw new AssertionException($"Missing {name}"); }
        static Rect Bounds(object element) { Vector2 position = (Vector2)Field(element, "Position"); Vector2 size = (Vector2)Field(element, "Size"); return new Rect(position - size * .5f, size); }
        static Vector2 ElementPosition(object room, string kind) { foreach (object element in Elements(room)) if (Field(element, "Kind").ToString() == kind) return (Vector2)Field(element, "Position"); throw new AssertionException($"Missing {kind}"); }
        static object Field(object value, string name) => value.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance).GetValue(value);
    }
}
