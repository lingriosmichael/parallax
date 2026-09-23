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
    // LevelLayouts by reflection, as LevelLayoutTests does.
    public sealed class ShippedLevelTimingTests
    {
        static readonly Type LayoutsType = Type.GetType("Parallax.Editor.Levels.LevelLayouts, Parallax.Editor");
        // Worst-case sub-tick phases of the SourceSpikes trigger crossing, as offsets past its near edge.
        static readonly float[] TriggerPhases = { .01f, .05f, .09f, .12f };
        const float TakeoffStep = .02f;
        const int MaxTicks = 300;

        // D-056 (1): a full-speed cat dropping from Floor_C lands on the Lift at least 12 ticks before
        // the Lift trigger fires (continuous model of V3_RoomTwoLanding..., at the real rate).
        [Test]
        public void L002_Lift_FullSpeedLandingHasTwelveTicksBeforeItsTrigger_AtTheRealRate()
        {
            object room = Room("L002");
            CatMotorConfig config = Config();
            Rect floorC = Bounds(Element(room, "Floor_C")), lift = Bounds(Element(room, "Lift"));
            object liftElement = Element(room, "Lift");
            Rect trigger = Box((Vector2)Field(liftElement, "SecondaryPosition"), (Vector2)Field(liftElement, "SecondarySize"));
            float firstTriggerCentre = trigger.xMin - config.ColliderSize.x * .5f + .001f;
            float fullyOffFloorCentre = floorC.xMax + config.ColliderSize.x * .5f;
            float approachTicks = (firstTriggerCentre - fullyOffFloorCentre) / (config.MaxSpeed * TickTime.SecondsPerTick);
            float fallTicks = TickTime.ToTicks(Mathf.Sqrt(2f * (floorC.yMax - lift.yMax) / GravityStrength()));
            Assert.GreaterOrEqual(approachTicks - fallTicks, 12f,
                $"L002: a full-speed cat lands on the Lift {approachTicks - fallTicks:F2} ticks before its trigger fires at {TickTime.TicksPerSecond:F0} Hz; D-056 (1) needs 12.");
        }

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

        // D-076 (R15): the betrayal is Block_1 killing the early flipper. From the earliest full-speed
        // take-off (at the SourceSpikes trigger crossing), a contiguous band of take-offs flips and is
        // killed by Block_1, at least MinEarlyFlipBandTicks wide at every trigger phase (measured 5.67 at the worst phase).
        const float MinEarlyFlipBandTicks = 5f;

        [Test]
        public void L004_EarlyFlipBand_IsKilledByBlock1()
        {
            float runPerTick = Config().MaxSpeed * TickTime.SecondsPerTick;
            var bands = new List<string>(); float narrowest = float.MaxValue;
            foreach (IGrouping<float, (float phase, float takeoff, RoomStepper run, RoomStepper.End end)> byPhase in L004Runs().GroupBy(r => r.phase))
            {
                float first = float.NaN, last = float.NaN; string endedBy = "no take-off";
                foreach ((float _, float takeoff, RoomStepper run, RoomStepper.End end) in byPhase)
                {
                    if (!(run.Flipped && end == RoomStepper.End.Killed && run.Killer == "Block_1")) { endedBy = $"take-off {takeoff:F2}: {(run.Flipped ? "flipped" : "no flip")}, {end} {run.Killer}"; break; }
                    if (float.IsNaN(first)) first = takeoff;
                    last = takeoff;
                }
                float ticks = float.IsNaN(first) ? 0f : (last - first) / runPerTick;
                narrowest = Mathf.Min(narrowest, ticks);
                bands.Add(float.IsNaN(first) ? $"phase +{byPhase.Key:F2}: no early flip is killed by Block_1 (first {endedBy})" : $"phase +{byPhase.Key:F2}: take-off {first:F2}..{last:F2} = {ticks:F2} ticks, ended by {endedBy}");
            }
            Assert.GreaterOrEqual(narrowest, MinEarlyFlipBandTicks, "L004 early-flip band killed by Block_1 (D-076):\n" + string.Join("\n", bands));
        }

        // D-076 (R10): the right-holding fast-flip window. A take-off is in it when the cat enters
        // Flip_A and is still alive when the stretch ends (it lands on the ceiling once Block_2 has fired
        // and is below the cat, or passes Block_2's column), so it has cleared both blocks. The longest contiguous run of such take-offs, in
        // ticks at run speed, must be at least 12 at every trigger phase (D-056 (1)).
        [Test]
        public void L004_FastFlip_RightHoldingWindowIsAtLeastTwelveTicks_AndClearsBothBlocks()
        {
            float runPerTick = Config().MaxSpeed * TickTime.SecondsPerTick;
            var windows = new List<string>(); float narrowest = float.MaxValue;
            foreach (IGrouping<float, (float phase, float takeoff, RoomStepper run, RoomStepper.End end)> byPhase in L004Runs().GroupBy(r => r.phase))
            {
                float bestStart = 0f, bestEnd = 0f, start = float.NaN, last = float.NaN; bool any = false;
                foreach ((float _, float takeoff, RoomStepper run, RoomStepper.End end) in byPhase)
                {
                    // Landing on the ceiling counts only once Block_2 has fired and is below the cat (R14).
                    bool clears = run.Flipped && (end == RoomStepper.End.PastX || (end == RoomStepper.End.OnCeiling && run.IsFiredAndBelowCat("Block_2")));
                    if (!clears) { start = float.NaN; continue; }
                    if (float.IsNaN(start)) start = takeoff;
                    last = takeoff;
                    if (!any || last - start > bestEnd - bestStart) { bestStart = start; bestEnd = last; any = true; }
                }
                float ticks = any ? (bestEnd - bestStart) / runPerTick : 0f;
                narrowest = Mathf.Min(narrowest, ticks);
                windows.Add(any ? $"phase +{byPhase.Key:F2}: take-off {bestStart:F2}..{bestEnd:F2} = {ticks:F2} ticks" : $"phase +{byPhase.Key:F2}: no full-speed flip clears both blocks");
            }
            Assert.GreaterOrEqual(narrowest, 12f, "L004 fast-flip window (D-056 (1), D-076):\n" + string.Join("\n", windows));
        }

        static IEnumerable<(float phase, float takeoff, RoomStepper run, RoomStepper.End end)> L004Runs()
        {
            object room = Room("L004");
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

        static object Room(string id)
        {
            Assert.NotNull(LayoutsType, "Parallax.Editor.Levels.LevelLayouts not found.");
            var registry = (IDictionary)LayoutsType.GetField("ById", BindingFlags.Public | BindingFlags.Static).GetValue(null);
            Assert.IsTrue(registry.Contains(id), id + " is not in LevelLayouts.");
            return registry[id];
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
