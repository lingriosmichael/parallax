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
        public void V3_EveryFallColumnHasPermanentGroundOrADeclaredClosedPit()
        {
            const float epsilon = .001f;
            foreach (object room in Rooms())
            {
                Element[] elements = Elements(room);
                float width = (float)Field(room, "Width");
                float floorTop = (float)LayoutType.GetField("FloorTop", BindingFlags.Public | BindingFlags.Static).GetValue(null);
                object[] pits = ((IEnumerable)Field(room, "Openings")).Cast<object>()
                    .Where(o => Field(o, "Kind").ToString() == "Pit").ToArray();
                var cuts = new System.Collections.Generic.List<float> { 0f, width };

                void AddEdges(Rect bounds)
                {
                    cuts.Add(Mathf.Clamp(bounds.xMin, 0f, width));
                    cuts.Add(Mathf.Clamp(bounds.xMax, 0f, width));
                }

                Rect PoseBounds(Element element, int pose)
                {
                    Rect bounds = Bounds(element);
                    if (pose == 1) bounds.position += element.Offset;
                    return bounds;
                }

                void RequirePit(float minX, float maxX, string reason)
                {
                    object pit = pits.FirstOrDefault(o => (float)Field(o, "MinX") <= minX + epsilon
                        && (float)Field(o, "MaxX") >= maxX - epsilon);
                    Assert.NotNull(pit, $"Room {Field(room, "Id")}: {reason} x={minX:F2}–{maxX:F2} has no declared pit.");
                    Element bottom = ElementByName(room, (string)Field(pit, "ClosureName"));
                    Element hazard = ElementByName(room, (string)Field(pit, "HazardName"));
                    Element leftWall = ElementByName(room, (string)Field(pit, "LeftWallName"));
                    Element rightWall = ElementByName(room, (string)Field(pit, "RightWallName"));
                    Assert.AreEqual("PitBottom", bottom.Kind, reason);
                    Assert.AreEqual("Hazard", hazard.Kind, reason);
                    Assert.AreEqual("OpeningBottom", hazard.HazardRole, reason);
                    Assert.AreEqual("Wall", leftWall.Kind, reason);
                    Assert.AreEqual("Wall", rightWall.Kind, reason);
                    Assert.LessOrEqual(Bounds(bottom).xMin, minX + epsilon, reason);
                    Assert.GreaterOrEqual(Bounds(bottom).xMax, maxX - epsilon, reason);
                    Assert.LessOrEqual(Bounds(hazard).xMin, minX + epsilon, reason);
                    Assert.GreaterOrEqual(Bounds(hazard).xMax, maxX - epsilon, reason);
                    Assert.GreaterOrEqual(Bounds(hazard).yMin, Bounds(bottom).yMax - epsilon, reason);
                    Assert.Less(Bounds(hazard).yMax, 0f, reason);
                    Assert.LessOrEqual(Bounds(leftWall).xMax, minX + epsilon, reason);
                    Assert.GreaterOrEqual(Bounds(rightWall).xMin, maxX - epsilon, reason);
                }

                foreach (Element element in elements)
                {
                    if (element.Kind != "Floor" && element.Kind != "CollapsingFloor"
                        && !(element.Kind == "MovingTrap" && element.MovingKind == "Solid")) continue;
                    Rect start = Bounds(element);
                    AddEdges(start);
                    if (element.Kind == "CollapsingFloor") RequirePit(start.xMin, start.xMax, element.Name + " after collapse");
                    if (element.Kind != "MovingTrap") continue;
                    Rect end = start;
                    end.position += element.Offset;
                    AddEdges(end);
                    RequirePit(Mathf.Min(start.xMin, end.xMin), Mathf.Max(start.xMax, end.xMax), element.Name + " swept support");
                }
                foreach (object pit in pits)
                {
                    cuts.Add((float)Field(pit, "MinX"));
                    cuts.Add((float)Field(pit, "MaxX"));
                }

                float[] edges = cuts.Distinct().OrderBy(x => x).ToArray();
                for (int pose = 0; pose < 2; pose++)
                for (int i = 0; i < edges.Length - 1; i++)
                {
                    float left = edges[i], right = edges[i + 1];
                    if (right - left <= epsilon) continue;
                    float x = (left + right) * .5f;
                    bool permanentGround = elements.Any(e => e.Kind == "Floor" && Bounds(e).yMax <= floorTop + epsilon
                        && Bounds(e).xMin <= x && Bounds(e).xMax >= x);
                    bool movingGround = elements.Any(e => e.Kind == "MovingTrap" && e.MovingKind == "Solid"
                        && PoseBounds(e, pose).yMax <= floorTop + epsilon
                        && PoseBounds(e, pose).xMin <= x && PoseBounds(e, pose).xMax >= x);
                    if (!permanentGround && !movingGround)
                        RequirePit(left, right, $"pose {pose} unsupported column");
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
        public void V3_AllRoomsPassTrapLayoutValidator()
        {
            Type validator = Type.GetType("Parallax.Editor.Setup.TrapLayoutValidator, Parallax.Editor");
            Assert.NotNull(validator);
            MethodInfo validate = validator.GetMethod("TryValidate", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(validate);
            foreach (object room in Rooms())
            {
                object[] args = { room, null };
                Assert.IsTrue((bool)validate.Invoke(null, args), $"Room {Field(room, "Id")}: {args[1]}");
                Element[] elements = Elements(room);
                for (int target = 0; target < elements.Length; target++)
                {
                    if (elements[target].TriggerSource != "Chain") continue;
                    int source = Array.FindIndex(elements, e => e.Name == elements[target].ChainSource);
                    Assert.GreaterOrEqual(source, 0, elements[target].Name + " source exists");
                    Assert.Less(source, target, elements[target].Name + " source must be built first by SoloRoomsSetup");
                }
            }
        }

        [Test]
        public void V3_EachRoomHasTwoKitV2TrapsAndLevelCoversEveryFeature()
        {
            bool solid = false, movingHazard = false, periodic = false, rearm = false, flip = false, longChain = false;
            foreach (object room in Rooms())
            {
                Element[] elements = Elements(room);
                int kitV2 = elements.Count(e => e.IsConfigured && (e.TriggerSource == "Chain" || e.Kind == "MovingTrap" || e.RepeatMode != "Once"));
                Assert.GreaterOrEqual(kitV2, 2, $"Room {Field(room, "Id")} kit v2 count");
                solid |= elements.Any(e => e.Kind == "MovingTrap" && e.MovingKind == "Solid");
                movingHazard |= elements.Any(e => e.Kind == "MovingTrap" && e.MovingKind == "Hazard");
                periodic |= elements.Any(e => e.RepeatMode == "Periodic");
                rearm |= elements.Any(e => e.RepeatMode == "Rearm");
                flip |= elements.Any(e => e.Kind == "GravityFlip" && e.RendererEnabled);
                foreach (Element chain in elements.Where(e => e.TriggerSource == "Chain"))
                {
                    int links = 0;
                    Element source = chain;
                    while (source.TriggerSource == "Chain")
                    {
                        source = elements.Single(e => e.Name == source.ChainSource);
                        links++;
                    }
                    longChain |= links >= 3;
                }
            }
            Assert.IsTrue(solid && movingHazard && periodic && rearm && flip && longChain,
                $"Coverage: Solid={solid}, moving Hazard={movingHazard}, Periodic={periodic}, Rearm={rearm}, FLIP={flip}, three-link chain={longChain}");
        }

        [Test]
        public void V3_EachRoomHasTwoHeightChangingPlatformJumps()
        {
            foreach (object room in Rooms())
            {
                int count = 0;
                foreach (object jump in (IEnumerable)Field(room, "RequiredJumps"))
                {
                    string source = (string)Field(jump, "SourceName"), destination = (string)Field(jump, "DestinationName");
                    if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(destination)) continue;
                    Element from = ElementByName(room, source), to = ElementByName(room, destination);
                    if (from.Kind != "Floor" || to.Kind != "Floor") continue;
                    float sourceTop = Bounds(from).yMax, destinationTop = Bounds(to).yMax;
                    Assert.AreEqual(sourceTop, (float)Field(jump, "TakeoffPawHeight"), .001f, source);
                    Assert.AreEqual(destinationTop, (float)Field(jump, "LandingPawHeight"), .001f, destination);
                    if (!Mathf.Approximately(sourceTop, destinationTop)) count++;
                }
                Assert.GreaterOrEqual(count, 2, $"Room {Field(room, "Id")} height-changing jumps");
            }
        }

        [Test]
        public void V3_PeriodicRouteHasTwelveTicksBeyondFromRestCrossing()
        {
            CatMotorConfig config = Config();
            float tickAcceleration = config.Acceleration / 3600f;
            float tickSpeed = config.MaxSpeed / 60f;
            float accelerateTicks = tickSpeed / tickAcceleration;
            float accelerationDistance = tickSpeed * tickSpeed / (2f * tickAcceleration);
            foreach (object room in Rooms())
            foreach (Element trap in Elements(room).Where(e => e.RepeatMode == "Periodic"))
            {
                // The waiting cat stands half a unit clear of the block's first-contact line.
                float distance = trap.Size.x + config.ColliderSize.x + .5f;
                float crossing = distance <= accelerationDistance
                    ? Mathf.Sqrt(2f * distance / tickAcceleration)
                    : accelerateTicks + (distance - accelerationDistance) / tickSpeed;
                Assert.GreaterOrEqual(trap.PeriodTicks - trap.CooldownTicks, crossing + 12f, trap.Name);
            }
        }

        [Test]
        public void V3_MovingSolidsSweepOnlyFreeSpaceAndLaunchClearOfHazards()
        {
            CatMotorConfig config = Config();
            float gravity = GravityStrength();
            foreach (object room in Rooms())
            {
                Element[] elements = Elements(room);
                foreach (Element mover in elements.Where(e => e.Kind == "MovingTrap" && e.MovingKind == "Solid"))
                {
                    Rect start = Bounds(mover);
                    Vector2 offset = mover.Offset;
                    Rect sweep = Rect.MinMaxRect(Mathf.Min(start.xMin, start.xMin + offset.x), Mathf.Min(start.yMin, start.yMin + offset.y),
                        Mathf.Max(start.xMax, start.xMax + offset.x), Mathf.Max(start.yMax, start.yMax + offset.y));
                    foreach (Element fixedElement in elements.Where(e => e.Kind == "Floor" || e.Kind == "Ceiling" || e.Kind == "Wall" || e.Kind == "PitBottom"))
                        Assert.IsFalse(sweep.Overlaps(Bounds(fixedElement)), $"{mover.Name} sweeps through {fixedElement.Name}");

                    // A named CrushPartner_<mover> may touch this sweep, but may not
                    // overlap it. Rect.Overlaps above is strict, so touching passes.
                    if (offset.y == 0f || (offset.y < 0f && mover.ReturnTicks == 0)) continue;
                    int upwardTicks = offset.y > 0f ? mover.MoveTicks : mover.ReturnTicks;
                    Assert.Greater(upwardTicks, 0, mover.Name + " upward leg duration");
                    float speed = Mathf.Abs(offset.y) / upwardTicks * 60f;
                    float rise = speed * speed / (2f * gravity);
                    float stoppedTop = start.yMax + Mathf.Max(offset.y, 0f);
                    Rect launch = Rect.MinMaxRect(sweep.xMin - config.ColliderSize.x * .5f, stoppedTop,
                        sweep.xMax + config.ColliderSize.x * .5f, stoppedTop + config.ColliderSize.y + rise);
                    foreach (Element hazard in elements.Where(e => e.Kind == "Hazard" || e.Kind == "HiddenSpikes" || (e.Kind == "MovingTrap" && e.MovingKind == "Hazard")))
                        Assert.IsFalse(launch.Overlaps(Bounds(hazard)), $"{mover.Name} launch meets {hazard.Name}");
                }
            }
        }

        [Test]
        public void V3_DisguiseAndVisibleBeforeKillAreDeclared()
        {
            foreach (object room in Rooms())
            {
                Element[] elements = Elements(room);
                foreach (Element trap in elements.Where(e => e.IsConfigured))
                {
                    if (trap.TriggerSource == "Chain" || trap.RepeatMode == "Periodic")
                        Assert.AreEqual(Vector2.zero, trap.SecondarySize, trap.Name + " must have no trigger box");
                    if (trap.Kind == "HiddenSpikes")
                    {
                        Assert.IsFalse(trap.RendererEnabled, trap.Name + " is not visibly presented before reveal");
                        Assert.GreaterOrEqual(trap.RevealDelayTicks, 6, trap.Name + " D-057 reveal lead");
                    }
                    if (trap.Kind == "FallingBlock" && trap.TriggerSource == "Chain")
                    {
                        Rect block = Bounds(trap);
                        float supportTop = elements.Where(e => e.Kind == "Floor" && Bounds(e).xMax > block.xMin && Bounds(e).xMin < block.xMax)
                            .Select(e => Bounds(e).yMax).DefaultIfEmpty(0f).Max();
                        float firstContactTicks = (trap.Direction == "Down" ? block.yMin - supportTop - .56f : 7f - .56f - block.yMax) / trap.UnitsPerTick;
                        Assert.GreaterOrEqual(firstContactTicks, 6f, trap.Name + " D-057 visible-motion lead");
                    }
                    if (trap.Kind == "CollapsingFloor" || (trap.Kind == "MovingTrap" && trap.MovingKind == "Solid"))
                        Assert.IsFalse(trap.RendererEnabled, trap.Name + " uses ordinary ground presentation");
                }
                foreach (Element collapse in elements.Where(e => e.Kind == "CollapsingFloor"))
                {
                    Rect slab = Bounds(collapse);
                    foreach (Element groundHazard in elements.Where(e => e.Kind == "Hazard" && e.HazardRole != "OpeningBottom" && Bounds(e).yMax >= 0f))
                    {
                        Rect hazard = Bounds(groundHazard);
                        if (hazard.xMin < slab.xMax) continue;
                        float separationTicks = (hazard.xMin - slab.xMax) / .1f;
                        Assert.GreaterOrEqual(separationTicks, 6f, collapse.Name + " opens too close to " + groundHazard.Name);
                    }
                }
            }
        }

        [Test]
        public void V3_RoomOpeningsAreDistinctAndThirdRoomSubvertsTheCollapse()
        {
            object[] rooms = Rooms().Cast<object>().ToArray();
            Assert.AreEqual(4, rooms.Length);
            string[] openings = rooms.Select(room => string.Join(";", ((IEnumerable)Field(room, "Openings")).Cast<object>()
                .Take(2).Select(o => $"{Field(o, "MinX")}-{Field(o, "MaxX")}"))).ToArray();
            Assert.AreEqual(4, openings.Distinct().Count(), "Each room needs its own approach geometry.");
            Rect firstCollapse = Bounds(ElementByName(rooms[0], "Collapse_C"));
            Rect thirdCollapse = Bounds(ElementByName(rooms[2], "Collapse_C"));
            Rect thirdSafe = Bounds(ElementByName(rooms[2], "Floor_Safe"));
            Assert.That(firstCollapse.xMin, Is.EqualTo(18f));
            Assert.That(thirdCollapse.xMax, Is.LessThanOrEqualTo(firstCollapse.xMin));
            Assert.That(thirdSafe.xMin, Is.EqualTo(firstCollapse.xMin));
            Assert.That(thirdSafe.xMax, Is.GreaterThanOrEqualTo(firstCollapse.xMax));
        }

        [Test]
        public void V3_RoomTwoLandingIsClearAndSweepRearmsVisibly()
        {
            object room = Rooms().Cast<object>().Single(r => (int)Field(r, "Id") == 1);
            object jump = ((IEnumerable)Field(room, "RequiredJumps")).Cast<object>()
                .Single(j => (string)Field(j, "ReferenceName") == "Collapse_C");
            float landingX = (float)Field(jump, "LandingX");
            Rect landingCat = Rect.MinMaxRect(landingX - .5f, 0f, landingX + .5f, .56f);
            Element sweep = ElementByName(room, "Sweep"), spikes = ElementByName(room, "Spikes_A");
            foreach (Element trap in new[] { sweep, spikes })
            {
                Rect trigger = new(trap.SecondaryPosition - trap.SecondarySize * .5f, trap.SecondarySize);
                Assert.IsFalse(trigger.Overlaps(landingCat), trap.Name + " fires on the required landing");
            }
            Rect sweepTrigger = new(sweep.SecondaryPosition - sweep.SecondarySize * .5f, sweep.SecondarySize);
            Rect spikeTrigger = new(spikes.SecondaryPosition - spikes.SecondarySize * .5f, spikes.SecondarySize);
            Assert.IsFalse(sweepTrigger.Overlaps(spikeTrigger), "Sweep and spikes need separate trigger zones.");
            Rect sweepAtHold = Bounds(sweep);
            sweepAtHold.position += sweep.Offset;
            Assert.IsFalse(sweepAtHold.Overlaps(landingCat), "The Sweep must not occupy the landing cat.");
            Assert.AreEqual("Rearm", sweep.RepeatMode);
            Assert.GreaterOrEqual(sweep.CooldownTicks, sweep.MoveTicks + sweep.HoldTicks + sweep.ReturnTicks);
            Assert.AreEqual(92, sweep.CooldownTicks);
            Assert.AreEqual("Once", ElementByName(Rooms().Cast<object>().Single(r => (int)Field(r, "Id") == 2), "CeilingSpikes").RepeatMode);
            Assert.That(Bounds(ElementByName(room, "Receiver")).yMin,
                Is.LessThanOrEqualTo(Bounds(ElementByName(room, "Pit1_L")).yMax));
            Element floorC = ElementByName(room, "Floor_C"), lift = ElementByName(room, "Lift");
            Rect liftTrigger = new(lift.SecondaryPosition - lift.SecondarySize * .5f, lift.SecondarySize);
            Assert.GreaterOrEqual(liftTrigger.xMin - Config().ColliderSize.x,
                Bounds(floorC).xMax + .5f,
                "The cat must have left Floor_C before the Lift trigger can fire.");
            Assert.GreaterOrEqual(liftTrigger.xMin, Bounds(lift).xMin + Config().ColliderSize.x,
                "The cat must be fully over the Lift at trigger entry.");
            float firstTriggerCentre = liftTrigger.xMin - Config().ColliderSize.x * .5f + .001f;
            Rect standingCat = Rect.MinMaxRect(firstTriggerCentre - Config().ColliderSize.x * .5f,
                Bounds(lift).yMax, firstTriggerCentre + Config().ColliderSize.x * .5f,
                Bounds(lift).yMax + Config().ColliderSize.y);
            Assert.IsTrue(liftTrigger.Overlaps(standingCat), "A cat standing on the Lift must trigger it.");
            float fullyOffFloorCentre = Bounds(floorC).xMax + Config().ColliderSize.x * .5f;
            float approachTicks = (firstTriggerCentre - fullyOffFloorCentre) / (Config().MaxSpeed / 60f);
            float fallTicks = Mathf.Sqrt(2f * (Bounds(floorC).yMax - Bounds(lift).yMax)
                / GravityStrength()) * 60f;
            Assert.GreaterOrEqual(approachTicks - fallTicks, 12f,
                "Even at full speed, the cat must land on the Lift with timing slack before its trigger fires.");
        }

        [Test]
        public void V3_ThirdRoomExitPunishesStraightDropButLeavesAnAirborneDoorRoute()
        {
            object room = Rooms().Cast<object>().Single(r => (int)Field(r, "Id") == 2);
            Element[] elements = Elements(room);
            foreach (string name in new[] { "Collapse_C", "CeilingSpikes", "Retreat", "ExitSpikes" })
                Assert.IsTrue(elements.Any(e => e.Name == name), name + " is one of the four Room 3 betrayals");
            Element flip = ElementByName(room, "Flip_B"), spikes = ElementByName(room, "ExitSpikes");
            Element door = ElementByName(room, "Door"), retreat = ElementByName(room, "Retreat");
            Assert.AreEqual("HiddenSpikes", spikes.Kind);
            Assert.GreaterOrEqual(spikes.RevealDelayTicks, 6);
            Assert.AreEqual(flip.Position, spikes.SecondaryPosition);
            Assert.AreEqual(flip.Size, spikes.SecondarySize);
            Rect straightDrop = Rect.MinMaxRect(flip.Position.x - .5f, 0f, flip.Position.x + .5f, .56f);
            Rect finalDoor = Bounds(door);
            finalDoor.position += retreat.Offset;
            Assert.IsTrue(straightDrop.Overlaps(Bounds(spikes)), "A straight drop must meet the revealed spikes.");
            Assert.IsFalse(straightDrop.Overlaps(finalDoor), "The door must not save a straight drop.");
            float safePocketMinCentre = Bounds(spikes).xMax + Config().ColliderSize.x * .5f;
            float safePocketMaxCentre = (float)Field(room, "Width") - Config().ColliderSize.x * .5f;
            Assert.GreaterOrEqual(safePocketMaxCentre - safePocketMinCentre, .75f,
                "The cat needs a full-width safe pocket to the right of the revealed spikes.");
            Assert.LessOrEqual(safePocketMinCentre - (flip.Position.x + .25f), .001f,
                "A cat using the right side of Flip_B can reach the pocket before floor contact.");
            Assert.IsTrue(((IEnumerable)Field(room, "RequiredJumps")).Cast<object>()
                .Any(j => (string)Field(j, "ReferenceName") == "ExitSpikes"),
                "The learned jump back to the door must be recorded.");
            float gravity = GravityStrength(), jumpSpeed = Mathf.Sqrt(2f * gravity * Config().JumpHeight);
            float dropIntoFlip = (float)LayoutType.GetField("CeilingUnderside", BindingFlags.Public | BindingFlags.Static).GetValue(null)
                - Config().ColliderSize.y - Bounds(flip).yMax;
            float timeIntoFlip = (jumpSpeed - Mathf.Sqrt(jumpSpeed * jumpSpeed - 2f * gravity * dropIntoFlip)) / gravity;
            float downwardSpeedAtFlip = jumpSpeed - gravity * timeIntoFlip;
            float dropToSpikes = Bounds(flip).yMax - Bounds(spikes).yMax;
            float timeToSpikes = (-downwardSpeedAtFlip
                + Mathf.Sqrt(downwardSpeedAtFlip * downwardSpeedAtFlip + 2f * gravity * dropToSpikes)) / gravity;
            Assert.GreaterOrEqual(timeToSpikes * 60f - spikes.RevealDelayTicks, 6f,
                "ExitSpikes must visibly reveal before a straight drop can die.");
        }

        [Test]
        public void V3_CeilingJumpsLandBeforeFlipBAndFastFlipLandingsRemainInsideRoom()
        {
            foreach (object room in Rooms().Cast<object>().Where(r => (int)Field(r, "Id") >= 2))
            {
                object ceilingJump = ((IEnumerable)Field(room, "RequiredJumps")).Cast<object>()
                    .Single(j => Field(j, "Frame").ToString() == "Ceiling");
                float landingX = (float)Field(ceilingJump, "LandingX");
                Element flipB = ElementByName(room, "Flip_B");
                Assert.GreaterOrEqual(Bounds(flipB).xMin, landingX + .5f,
                    "The jump must land on the ceiling before entering Flip_B.");
                Assert.LessOrEqual(Bounds(flipB).xMax, (float)Field(room, "Width"));
                Element flipA = ElementByName(room, "Flip_A");
                float drift = (float)LayoutType.GetField("ForceUpDrift", BindingFlags.Public | BindingFlags.Static).GetValue(null);
                float fastMin = Mathf.Min((float)Field(room, "Width") - .5f, Bounds(flipA).xMin + drift);
                float fastMax = Mathf.Min((float)Field(room, "Width") - .5f, Bounds(flipA).xMax + drift);
                Assert.GreaterOrEqual(fastMin, Bounds(flipA).xMin);
                Assert.LessOrEqual(fastMax, (float)Field(room, "Width") - .5f);
                if ((int)Field(room, "Id") == 2)
                    Assert.GreaterOrEqual(Bounds(ElementByName(room, "PeriodicUp")).xMin - (fastMax + .5f), .5f,
                        "The full-speed flip range must stop clear of the periodic block.");
                if ((int)Field(room, "Id") == 3)
                    Assert.GreaterOrEqual(fastMin - .5f - Bounds(ElementByName(room, "CeilingHiddenSpikes")).xMax, .5f,
                        "A full-speed flip needs clearance beyond the revealed ceiling spikes.");
            }
        }

        [Test]
        public void V3_RearmBelongsToVisibleSweepAndEachRoomHasTwoKitBetrayals()
        {
            string[][] kitBetrayals = {
                new[] { "Block_A", "Retreat" },
                new[] { "ReceiverBlock", "Block_A" },
                new[] { "PeriodicUp", "Retreat" },
                new[] { "FalseLanding", "Block_1", "Block_2", "CeilingHiddenSpikes" }
            };
            foreach (object room in Rooms())
            {
                int id = (int)Field(room, "Id");
                Element[] elements = Elements(room);
                Assert.GreaterOrEqual(kitBetrayals[id].Count(name => elements.Any(e => e.Name == name)), 2, $"Room {id}");
                foreach (Element rearm in elements.Where(e => e.RepeatMode == "Rearm"))
                {
                    Assert.AreEqual("MovingTrap", rearm.Kind, rearm.Name);
                    Assert.AreEqual("Hazard", rearm.MovingKind, rearm.Name);
                    Assert.IsTrue(rearm.RendererEnabled || rearm.Name == "Sweep",
                        rearm.Name + " must be a visibly moving hazard, not a re-hiding betrayal");
                }
            }
        }

        [Test]
        public void V3_LiftReceiverHeightAndLaunchAreExplicit()
        {
            object room = Rooms().Cast<object>().Single(r => (int)Field(r, "Id") == 1);
            Element lift = ElementByName(room, "Lift"), receiver = ElementByName(room, "Receiver"), block = ElementByName(room, "ReceiverBlock");
            float movedTop = Bounds(lift).yMax + lift.Offset.y;
            Assert.That(movedTop, Is.EqualTo(Bounds(receiver).yMax).Within(.001f));
            float launchSpeed = lift.Offset.y / lift.MoveTicks * 60f;
            float rise = launchSpeed * launchSpeed / (2f * GravityStrength());
            Assert.That(launchSpeed, Is.EqualTo(2.5f).Within(.001f));
            Assert.That(rise, Is.EqualTo(.10417f).Within(.002f));
            Assert.That(Bounds(block).yMin - 4f, Is.EqualTo(movedTop).Within(.001f),
                "The chained block must land flush on the Receiver.");
        }

        [Test]
        public void V3_FinalFalseLandingMovesBeforeAFullSpeedCatCanReachPermanentFloor()
        {
            object room = Rooms().Cast<object>().Single(r => (int)Field(r, "Id") == 3);
            Element falseLanding = ElementByName(room, "FalseLanding"), floor = ElementByName(room, "Floor_D");
            Assert.AreEqual("Once", falseLanding.RepeatMode, "The false floor must remain displaced until reset.");
            Assert.AreEqual(0, falseLanding.ReturnTicks);
            float triggerEntryCentre = falseLanding.SecondaryPosition.x - falseLanding.SecondarySize.x * .5f - .5f;
            float initialSupport = Bounds(falseLanding).xMax - (triggerEntryCentre - .5f);
            float retreatPerTick = -falseLanding.Offset.x / falseLanding.MoveTicks;
            float firstLossTick = initialSupport / (retreatPerTick + .1f);
            float firstPermanentFloorTick = (Bounds(floor).xMin - .5f - triggerEntryCentre) / .1f;
            Assert.GreaterOrEqual(firstLossTick, 6f, "The movement needs D-057 visible lead.");
            Assert.Less(firstLossTick, firstPermanentFloorTick, "A runner must lose the false floor before reaching permanent ground.");
            Element highPlatform = ElementByName(room, "Platform_C");
            float gravity = GravityStrength(), vy = Mathf.Sqrt(2f * gravity * Config().JumpHeight);
            float directFlight = (vy + Mathf.Sqrt(vy * vy + 4f * gravity)) / gravity;
            float directDistance = Bounds(floor).xMin + .5f - (Bounds(highPlatform).xMax - .5f);
            Assert.Greater(directDistance, Config().MaxSpeed * directFlight,
                "A full-speed leap must not bypass the false landing altogether.");
            Rect trigger = new(falseLanding.SecondaryPosition - falseLanding.SecondarySize * .5f, falseLanding.SecondarySize);
            Assert.Greater(trigger.yMin, Bounds(falseLanding).yMax + Config().ColliderSize.y,
                "A cat standing on FalseLanding must not activate lateral motion under itself.");
            float takeoffX = Bounds(highPlatform).xMax - Config().ColliderSize.x * .5f;
            float triggerCrossingX = trigger.xMin;
            float crossingTime = (triggerCrossingX - takeoffX) / Config().MaxSpeed;
            float pawAtTrigger = Bounds(highPlatform).yMax + vy * crossingTime - gravity * crossingTime * crossingTime * .5f;
            Assert.Less(pawAtTrigger, trigger.yMax, "The full-speed leap must enter the trigger before landing.");
            Assert.Greater(pawAtTrigger + Config().ColliderSize.y, trigger.yMin);
            float landingX = takeoffX + Config().MaxSpeed * directFlight;
            float remainingTicks = (directFlight - crossingTime) * 60f;
            float movingRightEdge = Bounds(falseLanding).xMax
                + falseLanding.Offset.x * Mathf.Min(1f, remainingTicks / falseLanding.MoveTicks);
            Assert.Greater(remainingTicks, 6f, "The slide must be visible before the unsupported landing.");
            Assert.Greater(landingX - Config().ColliderSize.x * .5f, movingRightEdge,
                "The full-speed landing must miss the sliding platform.");
            object learnedJump = ((IEnumerable)Field(room, "RequiredJumps")).Cast<object>()
                .Single(j => (string)Field(j, "DestinationName") == "FalseLanding");
            float learnedLandingX = (float)Field(learnedJump, "LandingX");
            Rect learnedCat = Rect.MinMaxRect(learnedLandingX - Config().ColliderSize.x * .5f, 0f,
                learnedLandingX + Config().ColliderSize.x * .5f, Config().ColliderSize.y);
            Assert.AreEqual(Bounds(falseLanding).yMax, learnedCat.yMin, .001f);
            Assert.Greater(learnedCat.xMax, Bounds(falseLanding).xMin,
                "The braked landing must be supported before the slide.");
            Assert.Less(learnedCat.xMin, Bounds(falseLanding).xMax);
            Assert.LessOrEqual(learnedCat.xMax, trigger.xMin,
                "The learned trajectory can stay left of the airborne slide trigger.");
        }

        [Test]
        public void V3_FinalFastFlipPassesAboveTheFallingSecondBlock()
        {
            object room = Rooms().Cast<object>().Single(r => (int)Field(r, "Id") == 3);
            Element source = ElementByName(room, "SourceSpikes"), first = ElementByName(room, "Block_1");
            Element second = ElementByName(room, "Block_2"), flip = ElementByName(room, "Flip_A");
            float sourceToFlip = (flip.Position.x - (source.SecondaryPosition.x - source.SecondarySize.x * .5f - .5f)) / .1f;
            float flipToBlock = (Bounds(second).xMin - .5f - flip.Position.x) / .1f;
            float secondFire = source.RevealDelayTicks + first.DelayTicks + second.DelayTicks;
            float elapsedFall = sourceToFlip + flipToBlock - secondFire;
            float blockTopAtContact = Bounds(second).yMax - Mathf.Min(second.TravelDistance, elapsedFall * second.UnitsPerTick);
            float minimumFlipPaw = Bounds(flip).yMin - Config().ColliderSize.y;
            float minimumPawAtContact = minimumFlipPaw + GravityStrength() * Mathf.Pow(flipToBlock / 60f, 2f) * .5f;
            float firstTopAtFlip = Bounds(first).yMax - Mathf.Min(first.TravelDistance,
                (sourceToFlip - source.RevealDelayTicks - first.DelayTicks) * first.UnitsPerTick);
            Assert.Greater(minimumFlipPaw, firstTopAtFlip,
                "The player must be above the already-landed first block on entering Flip_A.");
            Assert.GreaterOrEqual(elapsedFall, 6f, "The visible block motion precedes this crossing.");
            Assert.Greater(minimumPawAtContact, blockTopAtContact,
                "A full-speed upward flip, even from zero vertical speed, must clear Block_2.");
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
        static bool IsGameplayElement(Element e) => e.Kind == "Door" || e.Kind == "Hazard" || e.Kind == "CollapsingFloor" || e.Kind == "HiddenSpikes" || e.Kind == "FallingBlock" || e.Kind == "GravityFlip" || e.Kind == "DoorRetreat" || e.Kind == "MovingTrap";
        static Rect Bounds(Element e) => new(e.Position - e.Size * .5f, e.Size);
        static object Field(object value, string name) => value.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance).GetValue(value);

        readonly struct Element
        {
            readonly object value; public string Kind => Field(value, "Kind").ToString(); public string Name => (string)Field(value, "Name"); public Vector2 Position => (Vector2)Field(value, "Position"); public Vector2 Size => (Vector2)Field(value, "Size"); public Vector2 SecondaryPosition => (Vector2)Field(value, "SecondaryPosition"); public Vector2 SecondarySize => (Vector2)Field(value, "SecondarySize");
            object Settings => Field(value, "Settings"); public bool IsConfigured => (bool)Field(Settings, "IsConfigured"); public bool RearmOnExit => (bool)Field(Settings, "RearmOnExit"); public bool RendererEnabled => (bool)Field(Settings, "RendererEnabled"); public string GravityMode => Field(Settings, "GravityMode").ToString(); public string HazardRole => Field(value, "HazardRole").ToString();
            public string TriggerSource => Field(Settings, "TriggerSource").ToString(); public string ChainSource => (string)Field(Settings, "ChainSource"); public string RepeatMode => Field(Settings, "RepeatMode").ToString(); public string MovingKind => Field(Settings, "MovingKind").ToString();
            public int PeriodTicks => (int)Field(Settings, "PeriodTicks"); public int CooldownTicks => (int)Field(Settings, "CooldownTicks"); public int MoveTicks => (int)Field(Settings, "MoveTicks"); public int HoldTicks => (int)Field(Settings, "HoldTicks"); public int ReturnTicks => (int)Field(Settings, "ReturnTicks"); public int DelayTicks => (int)Field(Settings, "DelayTicks"); public int RevealDelayTicks => (int)Field(Settings, "RevealDelayTicks"); public float UnitsPerTick => (float)Field(Settings, "UnitsPerTick"); public float TravelDistance => (float)Field(Settings, "TravelDistance"); public Vector2 Offset => (Vector2)Field(Settings, "Offset"); public string Direction => Field(Settings, "Direction").ToString();
            public Element(object value) { this.value = value; }
        }
    }
}
