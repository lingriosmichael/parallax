using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Parallax.Core;
using Parallax.Gameplay.Player;
using UnityEditor;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    // PAX-078 (D-076): the shipped levels' timing at the real tick rate, with no 60 Hz pin. The
    // SoloRoomsLayout narrative tests stay pinned (D-076); these read L002/L004 through
    // LevelLayouts by reflection, as LevelLayoutTests does. PAX-082 (§12 R6): the Lift, EarlyFlipBand and
    // FastFlip box-model tests are removed (D-079 (7)); the route harness measures them. PAX-059: L004 was redesigned;
    // this reads the frozen pre-PAX-059 L004 (RouteFixtures.FastFlipRoom), since it tests the stepper's floor-bypass check.
    public sealed class ShippedLevelTimingTests
    {
        static readonly Type FixturesType = Type.GetType("Parallax.Editor.Routes.RouteFixtures, Parallax.Editor");
        // Worst-case sub-tick phases of the SourceSpikes trigger crossing, as offsets past its near edge.
        static readonly float[] TriggerPhases = { .01f, .05f, .09f, .12f };
        const float TakeoffStep = .02f;
        const int MaxTicks = 300;

        // D-076 (R15): every spike hop is forced into Flip_A, so there is no floor bypass. A full-speed
        // take-off that doesn't flip must die on SourceSpikes (or in the pit) before it gets past them.
        [Test]
        public void L004_NoFullSpeedTakeoff_CrossesSourceSpikesWithoutFlipping()
        {
            var crossings = new List<string>();
            foreach ((float phase, float takeoff, RoomStepper run, RoomStepper.End end) in L004Runs())
            {
                if (run.Flipped) continue;
                if (end == RoomStepper.End.Killed && (run.Killer == "SourceSpikes" || run.Killer.EndsWith("_Hazard"))) continue;
                crossings.Add($"phase +{phase:F2} take-off {takeoff:F2}: {end} {run.Killer}");
            }
            Assert.IsEmpty(crossings, "L004 full-speed take-offs that cross SourceSpikes without flipping (a floor bypass, D-076):\n" + string.Join("\n", crossings.Take(20)));
        }

        static IEnumerable<(float phase, float takeoff, RoomStepper run, RoomStepper.End end)> L004Runs()
        {
            object room = FastFlipRoom();
            CatMotorConfig config = Config();
            float gravity = GravityStrength();
            object source = Element(room, "SourceSpikes");
            Rect sourceTrigger = Box((Vector2)Field(source, "SecondaryPosition"), (Vector2)Field(source, "SecondarySize"));
            float firstTake = sourceTrigger.xMin - config.ColliderSize.x * .5f;
            float lastTake = Bounds(source).xMin;
            float stopAtX = Bounds(Element(room, "Block_2")).xMax + config.ColliderSize.x * .5f;
            float startPaw = Bounds(Element(room, "FalseLanding")).yMax;
            foreach (float phase in TriggerPhases)
                for (float takeoff = firstTake; takeoff <= lastTake + 1e-4f; takeoff += TakeoffStep)
                {
                    var run = new RoomStepper(room, config, gravity, firstTake + phase, startPaw);
                    RoomStepper.End end = run.RunRightHolding(takeoff, stopAtX, MaxTicks);
                    yield return (phase, takeoff, run, end);
                }
        }

        static object FastFlipRoom()
        {
            Assert.NotNull(FixturesType, "Parallax.Editor.Routes.RouteFixtures not found.");
            return FixturesType.GetMethod("FastFlipRoom", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);
        }

        static object Element(object room, string name)
        {
            object e = ((IEnumerable)Field(room, "Elements")).Cast<object>().FirstOrDefault(x => (string)Field(x, "Name") == name);
            Assert.NotNull(e, "no element " + name);
            return e;
        }

        static Rect Bounds(object e) => Box((Vector2)Field(e, "Position"), (Vector2)Field(e, "Size"));
        static Rect Box(Vector2 centre, Vector2 size) => new Rect(centre - size * .5f, size);
        static CatMotorConfig Config() { var c = AssetDatabase.LoadAssetAtPath<CatMotorConfig>("Assets/_Game/Data/CatMotorConfig_Default.asset"); Assert.NotNull(c); return c; }
        static float GravityStrength() { GameObject cat = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Gameplay/Player/Cat_Player.prefab"); Assert.NotNull(cat); GravityReceiver g = cat.GetComponent<GravityReceiver>(); Assert.NotNull(g); return g.Strength; }
        static object Field(object value, string name) => value.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance).GetValue(value);
    }
}
