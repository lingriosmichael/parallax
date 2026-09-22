using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Parallax.Gameplay.Player;
using UnityEditor;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    public sealed class SoloRoomsLayoutTests
    {
        static readonly Type LayoutType = Type.GetType("Parallax.Editor.Setup.SoloRoomsLayout, Parallax.Editor");

        [Test]
        public void Rule6_TriggersStayClearOfCheckpointSpawnFootprints()
        {
            CatMotorConfig config = Config();
            Vector2 footprintSize = config.ColliderSize;
            foreach (object room in Rooms())
            {
                Vector2 checkpoint = ElementOfKind(room, "Checkpoint").Position;
                Vector2 footprintCentre = checkpoint + new Vector2(0f, -config.ColliderBottom) + config.ColliderOffset;
                Rect footprint = new(footprintCentre - footprintSize * .5f, footprintSize);
                foreach (Element element in Elements(room).Where(IsGameplayElement))
                {
                    Assert.IsFalse(Bounds(element).Overlaps(footprint), $"{element.Name} overlaps its checkpoint footprint.");
                    if (element.SecondarySize != Vector2.zero)
                        Assert.IsFalse(new Rect(element.SecondaryPosition - element.SecondarySize * .5f, element.SecondarySize).Overlaps(footprint), $"{element.Name} trigger overlaps its checkpoint footprint.");
                }
            }
        }

        [Test]
        public void Containment_AllCheckpointDoorsAndTrapsStayInsideTheirRooms()
        {
            foreach (object room in Rooms())
            {
                float width = (float)Field(room, "Width");
                (float minY, float maxY) = VerticalBounds(room);
                foreach (Element element in Elements(room).Where(IsGameplayElement))
                {
                    Assert.That(Bounds(element).xMin, Is.GreaterThanOrEqualTo(0f), element.Name);
                    Assert.That(Bounds(element).xMax, Is.LessThanOrEqualTo(width), element.Name);
                    Assert.That(Bounds(element).yMin, Is.GreaterThanOrEqualTo(minY), element.Name);
                    Assert.That(Bounds(element).yMax, Is.LessThanOrEqualTo(maxY), element.Name);
                    if (element.SecondarySize != Vector2.zero)
                    {
                        Rect trigger = new(element.SecondaryPosition - element.SecondarySize * .5f, element.SecondarySize);
                        Assert.That(trigger.xMin, Is.GreaterThanOrEqualTo(0f), element.Name + " trigger");
                        Assert.That(trigger.xMax, Is.LessThanOrEqualTo(width), element.Name + " trigger");
                        Assert.That(trigger.yMin, Is.GreaterThanOrEqualTo(minY), element.Name + " trigger");
                        Assert.That(trigger.yMax, Is.LessThanOrEqualTo(maxY), element.Name + " trigger");
                    }
                }
            }
        }

        [Test]
        public void Separation_EachRoomHasOneCheckpointAndDoorAndOriginsAreSpaced()
        {
            object previous = null;
            foreach (object room in Rooms())
            {
                Assert.AreEqual(1, Elements(room).Count(e => e.Kind == "Checkpoint"));
                Assert.AreEqual(1, Elements(room).Count(e => e.Kind == "Door"));
                if (previous != null)
                {
                    float gap = ((Vector2)Field(room, "Origin")).x - ((Vector2)Field(previous, "Origin")).x - (float)Field(previous, "Width");
                    Assert.GreaterOrEqual(gap, (float)LayoutType.GetField("MinimumRoomGap", BindingFlags.Public | BindingFlags.Static).GetValue(null));
                }
                previous = room;
            }
        }

        [Test]
        public void Rule3_PitsAndRecessesAreClosed()
        {
            foreach (object room in Rooms())
            foreach (object opening in (IEnumerable)Field(room, "Openings"))
            {
                Element closure = ElementByName(room, (string)Field(opening, "ClosureName"));
                Element hazard = ElementByName(room, (string)Field(opening, "HazardName"));
                Assert.That(closure.Size.x, Is.EqualTo((float)Field(opening, "MaxX") - (float)Field(opening, "MinX")));
                Assert.That(hazard.Size.x, Is.EqualTo((float)Field(opening, "MaxX") - (float)Field(opening, "MinX")));
                if (Field(opening, "Kind").ToString() == "Pit")
                {
                    Assert.AreEqual("PitBottom", closure.Kind);
                    Assert.AreEqual("OpeningBottom", hazard.HazardRole);
                }
                else
                {
                    Assert.AreEqual("Ceiling", closure.Kind);
                    Assert.AreEqual("OpeningCap", hazard.HazardRole);
                }
            }
        }

        [Test]
        public void Rule1_CollapsingSectionsAreFullSurfaceSlabs()
        {
            float floorTop = (float)LayoutType.GetField("FloorTop", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            float ceilingUnderside = (float)LayoutType.GetField("CeilingUnderside", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            float thickness = (float)LayoutType.GetField("SurfaceThickness", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            foreach (object room in Rooms())
            foreach (Element collapse in Elements(room).Where(e => e.Kind == "CollapsingFloor"))
            {
                Assert.AreEqual(thickness, collapse.Size.y, .001f, collapse.Name);
                Rect bounds = Bounds(collapse);
                Assert.IsTrue(Mathf.Approximately(bounds.yMax, floorTop) || Mathf.Approximately(bounds.yMin, ceilingUnderside), collapse.Name + " is not flush with its surface.");
            }
        }

        [Test]
        public void Rule2_WallsStayOutsideOpeningRanges()
        {
            foreach (object room in Rooms())
            foreach (object opening in (IEnumerable)Field(room, "Openings"))
            {
                float minX = (float)Field(opening, "MinX"), maxX = (float)Field(opening, "MaxX");
                Assert.LessOrEqual(Bounds(ElementByName(room, (string)Field(opening, "LeftWallName"))).xMax, minX);
                Assert.GreaterOrEqual(Bounds(ElementByName(room, (string)Field(opening, "RightWallName"))).xMin, maxX);
            }
        }

        [Test]
        public void Rule4_HiddenForceUpZonesHaveCeilingHazardCoverage()
        {
            float coverage = (float)LayoutType.GetField("HiddenForceUpCoverage", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            foreach (object room in Rooms())
            {
                float width = (float)Field(room, "Width");
                foreach (Element forceUp in Elements(room).Where(e => e.Kind == "GravityFlip" && e.GravityMode == "ForceUp" && !e.RendererEnabled))
                {
                    float requiredMin = Mathf.Max(0f, Bounds(forceUp).xMin - coverage);
                    float requiredMax = Mathf.Min(width, Bounds(forceUp).xMax + coverage);
                    Assert.IsTrue(Elements(room).Where(e => e.HazardRole == "CeilingForceUpCoverage").Any(h => Bounds(h).xMin <= requiredMin && Bounds(h).xMax >= requiredMax), forceUp.Name);
                }
            }
        }

        [Test]
        public void Rule5_VisibleFlipToolsRearmOnExit()
        {
            foreach (object room in Rooms())
            foreach (Element tool in Elements(room).Where(e => e.Kind == "GravityFlip" && e.RendererEnabled))
            {
                Assert.AreEqual("Flip", tool.GravityMode, tool.Name);
                Assert.IsTrue(tool.RearmOnExit, tool.Name);
            }
        }

        [Test]
        public void Reachability_RequiredJumpsAndUnjumpableHazardsFitMeasuredLimits()
        {
            CatMotorConfig config = Config();
            float gravity = GravityStrength();
            float fullSpeedReach = config.MaxSpeed * 2f * Mathf.Sqrt(2f * gravity * config.JumpHeight) / gravity;
            float fraction = (float)LayoutType.GetField("RequiredJumpReachFraction", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            foreach (object room in Rooms())
            foreach (object jump in (IEnumerable)Field(room, "RequiredJumps"))
            {
                float takeoff = (float)Field(jump, "TakeoffX"), landing = (float)Field(jump, "LandingX");
                string referenceName = (string)Field(jump, "ReferenceName");
                string sourceName = (string)Field(jump, "SourceName");
                string destinationName = (string)Field(jump, "DestinationName");
                float direction = Field(jump, "Direction").ToString() == "Right" ? 1f : -1f;
                Element reference = ElementByName(room, referenceName);
                float expectedTakeoff = string.IsNullOrEmpty(sourceName)
                    ? reference.Position.x - direction * (reference.Size.x + config.ColliderSize.x) * .5f
                    : SupportedEdge(ElementByName(room, sourceName), direction) - direction * config.ColliderSize.x * .5f;
                float expectedLanding = string.IsNullOrEmpty(destinationName)
                    ? reference.Position.x + direction * (reference.Size.x + config.ColliderSize.x) * .5f
                    : SupportedEdge(ElementByName(room, destinationName), -direction) + direction * config.ColliderSize.x * .5f;
                Assert.That(takeoff, Is.EqualTo(expectedTakeoff).Within(.001f), referenceName + " take-off");
                Assert.That(landing, Is.EqualTo(expectedLanding).Within(.001f), referenceName + " landing");
                float runway = (float)Field(jump, "Runway"), deltaHeight = (float)Field(jump, "LandingPawHeight") - (float)Field(jump, "TakeoffPawHeight");
                float vy = Mathf.Sqrt(2f * gravity * config.JumpHeight);
                Assert.GreaterOrEqual(vy * vy, 2f * gravity * deltaHeight, $"{referenceName} height");
                float vx = Mathf.Min(config.MaxSpeed, Mathf.Sqrt(2f * config.Acceleration * runway));
                float flight = (vy + Mathf.Sqrt(vy * vy - 2f * gravity * deltaHeight)) / gravity;
                float reach = vx * flight;
                float distance = Mathf.Abs(landing - takeoff);
                float window = 0f;
                if (Field(jump, "Kind").ToString() == "Hazard")
                {
                    float height = (float)Field(jump, "HazardHeight");
                    window = vx * 2f * Mathf.Sqrt(vy * vy - 2f * gravity * height) / gravity;
                    Assert.LessOrEqual(reference.Size.x, fraction * window, $"{referenceName} clearance");
                }
                TestContext.WriteLine($"Room {Field(room, "Id")} | {referenceName} | D={distance:F3} | vx={vx:F3} | T={flight:F3} | reach={reach:F3} | ratio={distance / reach:F3} | window={(Field(jump, "Kind").ToString() == "Hazard" ? window.ToString("F3") : "n/a")}");
                Assert.LessOrEqual(distance, fraction * reach, $"{referenceName} reach");
            }
            foreach (object room in Rooms())
            foreach (Element hazard in Elements(room).Where(e => e.HazardRole == "UnjumpableFloor"))
                Assert.Greater(hazard.Size.x + config.ColliderSize.x, fullSpeedReach, hazard.Name + " must be unjumpable.");
            float stepFraction = (float)LayoutType.GetField("RequiredStepHeightFraction", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            foreach (object room in Rooms())
            foreach (object step in (IEnumerable)Field(room, "RequiredSteps"))
                Assert.LessOrEqual((float)Field(step, "Height"), stepFraction * config.JumpHeight);
        }

        [Test]
        public void Rule7_TrapSettingsAreDeclaredInLayoutData()
        {
            foreach (object room in Rooms())
            foreach (Element trap in Elements(room).Where(e => e.Kind == "CollapsingFloor" || e.Kind == "HiddenSpikes" || e.Kind == "FallingBlock" || e.Kind == "GravityFlip" || e.Kind == "DoorRetreat"))
                Assert.IsTrue(trap.IsConfigured, trap.Name + " has settings outside SoloRoomsLayout.");
        }

        [Test]
        public void Rule8_JumpedHiddenSpikesHaveRunwayAndMaximumWidth()
        {
            foreach (object room in Rooms())
            foreach (object jump in (IEnumerable)Field(room, "RequiredJumps"))
            {
                if (Field(jump, "Direction").ToString() != "Right") continue;
                string reference = (string)Field(jump, "ReferenceName");
                Element[] spikes = Elements(room).Where(e => e.Name == reference && e.Kind == "HiddenSpikes").ToArray();
                if (spikes.Length == 0) continue;
                Element spike = spikes[0];
                Assert.LessOrEqual(spike.Size.x, 2f, spike.Name);
                float triggerFar = (Field(jump, "Direction").ToString() == "Right") ? spike.SecondaryPosition.x + spike.SecondarySize.x * .5f : spike.SecondaryPosition.x - spike.SecondarySize.x * .5f;
                float spikeNear = (Field(jump, "Direction").ToString() == "Right") ? spike.Position.x - spike.Size.x * .5f : spike.Position.x + spike.Size.x * .5f;
                Assert.GreaterOrEqual(Mathf.Abs(spikeNear - triggerFar), 2.5f, spike.Name);
            }
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
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            Assert.AreEqual(SpriteMeshType.FullRect, settings.spriteMeshType);
        }

        static CatMotorConfig Config() { CatMotorConfig config = AssetDatabase.LoadAssetAtPath<CatMotorConfig>("Assets/_Game/Data/CatMotorConfig_Default.asset"); Assert.NotNull(config); return config; }
        static float GravityStrength() { GameObject cat = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Gameplay/Player/Cat_Player.prefab"); Assert.NotNull(cat); GravityReceiver gravity = cat.GetComponent<GravityReceiver>(); Assert.NotNull(gravity); return gravity.Strength; }
        static IEnumerable Rooms() { Assert.NotNull(LayoutType); return (IEnumerable)LayoutType.GetField("Rooms", BindingFlags.Public | BindingFlags.Static).GetValue(null); }
        static Element ElementOfKind(object room, string kind) => Elements(room).Single(e => e.Kind == kind);
        static Element ElementByName(object room, string name) => Elements(room).Single(e => e.Name == name);
        static Element[] Elements(object room) => ((IEnumerable)Field(room, "Elements")).Cast<object>().Select(e => new Element(e)).ToArray();
        static float SupportedEdge(Element element, float direction) => element.Position.x + direction * element.Size.x * .5f;
        static (float minY, float maxY) VerticalBounds(object room)
        {
            float floorTop = (float)LayoutType.GetField("FloorTop", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            float ceilingTop = (float)LayoutType.GetField("CeilingUnderside", BindingFlags.Public | BindingFlags.Static).GetValue(null) + (float)LayoutType.GetField("SurfaceThickness", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            float minY = floorTop, maxY = ceilingTop;
            foreach (object opening in (IEnumerable)Field(room, "Openings"))
            {
                Element closure = ElementByName(room, (string)Field(opening, "ClosureName"));
                if (Field(opening, "Kind").ToString() == "Pit") minY = Mathf.Min(minY, Bounds(closure).yMin);
                else maxY = Mathf.Max(maxY, Bounds(closure).yMax);
            }
            return (minY, maxY);
        }
        static bool IsGameplayElement(Element e) => e.Kind == "Door" || e.Kind == "Hazard" || e.Kind == "CollapsingFloor" || e.Kind == "HiddenSpikes" || e.Kind == "FallingBlock" || e.Kind == "GravityFlip" || e.Kind == "DoorRetreat";
        static Rect Bounds(Element e) => new(e.Position - e.Size * .5f, e.Size);
        static object Field(object value, string name) => value.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance).GetValue(value);

        readonly struct Element
        {
            readonly object value; public string Kind => Field(value, "Kind").ToString(); public string Name => (string)Field(value, "Name"); public Vector2 Position => (Vector2)Field(value, "Position"); public Vector2 Size => (Vector2)Field(value, "Size"); public Vector2 SecondaryPosition => (Vector2)Field(value, "SecondaryPosition"); public Vector2 SecondarySize => (Vector2)Field(value, "SecondarySize");
            object Settings => Field(value, "Settings"); public bool IsConfigured => (bool)Field(Settings, "IsConfigured"); public bool RearmOnExit => (bool)Field(Settings, "RearmOnExit"); public bool RendererEnabled => (bool)Field(Settings, "RendererEnabled"); public string GravityMode => Field(Settings, "GravityMode").ToString(); public string HazardRole => Field(value, "HazardRole").ToString(); public Element(object value) { this.value = value; }
        }
    }
}
