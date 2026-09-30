using System;
using System.Collections.Generic;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Setup;
using UnityEngine;

namespace Parallax.Editor.Art
{
    // PAX-V07 item 3: the climbing scenarios (grab, climb at full and at the slowest analog speed, hang, resume, climb down,
    // leap, release), each in both gravities, in a capture-only climb bench (never a level, never saved): a floor and a
    // ceiling 7 u apart with two vines. Gravity down: Vine_Base stands on the floor at x 8 (0 to 5.1, Trap Lab room 9's
    // Vine_Real) and Vine_High hangs from the ceiling at x 14 with its end at 2.0. Gravity up mirrors Vine_Base (it hangs from
    // the ceiling the cat stands on, 1.9 to 7); Vine_High becomes a 2 u vine hanging from that ceiling (5.0 to 7): the motor
    // releases a cat that climbs past a vine's screen-bottom end (ClimbState.ReleasesAtBottom), so with gravity up the
    // release is climbing away from the ceiling past that end, and with gravity down climbing down past Vine_High's end.
    static partial class CatCaptureScenarios
    {
        const float VineBaseX = 8f, VineHighX = 14f;

        static SoloRoomDefinition ClimbBench(bool up)
        {
            float Y(float y) => up ? BenchHeight - y : y;
            var elements = new List<SoloRoomElement>
            {
                Element(SoloRoomElementKind.Floor, "Floor", new Vector2(BenchWidth * .5f, -.5f), new Vector2(BenchWidth, 1f)),
                Element(SoloRoomElementKind.Ceiling, "Ceiling", new Vector2(BenchWidth * .5f, BenchHeight + .5f), new Vector2(BenchWidth, 1f)),
                Element(SoloRoomElementKind.Checkpoint, "Checkpoint", new Vector2(2f, 0f), Vector2.zero),
                Element(SoloRoomElementKind.Door, "Door", new Vector2(BenchWidth - .5f, .75f), new Vector2(.6f, 1.5f)),
                Element(SoloRoomElementKind.Vine, "Vine_Base", new Vector2(VineBaseX, Y(2.55f)), new Vector2(.6f, 5.1f)),
                up ? Element(SoloRoomElementKind.Vine, "Vine_High", new Vector2(VineHighX, 6f), new Vector2(.6f, 2f))
                   : Element(SoloRoomElementKind.Vine, "Vine_High", new Vector2(VineHighX, 4.5f), new Vector2(.6f, 5f)),
            };
            return new SoloRoomDefinition(0, 0f, BenchWidth, elements.ToArray(), Array.Empty<SoloRoomOpening>(), Array.Empty<RequiredJump>());
        }

        // ---------- steps with a Climb command ----------

        /// <summary>Holds a move and a climb (screen-up positive, as CatCommand.Climb) for whole ticks.</summary>
        sealed class ClimbHold : CaptureStep
        {
            readonly float move, climb; readonly int ticks;
            public ClimbHold(float move, float climb, float seconds, string label) { this.move = move; this.climb = climb; ticks = TickTime.ToWholeTicks(seconds); Label = label; }
            public override bool Next(CatCaptureRig rig, int done, out CatCommand c) { c = new CatCommand { Move = move, Climb = climb }; return done < ticks; }
        }

        sealed class ClimbUntil : CaptureStep
        {
            readonly float move, climb; readonly Func<CatCaptureRig, bool> condition; readonly int maxTicks;
            public ClimbUntil(float move, float climb, Func<CatCaptureRig, bool> condition, float maxSeconds, string label)
            { this.move = move; this.climb = climb; this.condition = condition; maxTicks = TickTime.ToWholeTicks(maxSeconds); Label = label; }
            public override bool Next(CatCaptureRig rig, int done, out CatCommand c)
            {
                c = new CatCommand { Move = move, Climb = climb };
                if (condition(rig)) return false;
                if (done < maxTicks) return true;
                Debug.LogWarning($"CatCapture: step '{Label}' timed out after {maxTicks} ticks at tick {rig.Tick}.");
                return false;
            }
        }

        static bool Climbing(CatCaptureRig r) => r.Cat.IsClimbing;
        static bool NotClimbing(CatCaptureRig r) => !r.Cat.IsClimbing;
        static Func<CatCaptureRig, bool> Near(float x) => r => Mathf.Abs(r.ColliderCentre.x - r.Origin.x - x) <= .12f;

        // ---------- the scripts (screen-relative; `g` = +1 gravity down, -1 gravity up: Climb * g pushes away from the
        // ground the cat stands on, i.e. up the vine in the cat's own frame) ----------

        // Walk to Vine_Base from the `dir` side, grab from the ground, climb at full speed, hang, the slowest analog climb
        // (the stick just past the 0.35 dead zone: Climb 0.05) for a full cycle, a slow climb (0.1), hang, resume at full
        // speed, then climb down at full speed until the ground releases the cat.
        static List<CaptureStep> GroundClimb(float g, float dir) => new()
        {
            new Hold(0f, .4f, "stand 0.4 s"),
            new Until(.5f * dir, Near(VineBaseX), 3f, "walk to the vine"),
            new ClimbUntil(.5f * dir, g, Climbing, .3f, "push up the vine: grab from the ground") { Cut = true },
            new ClimbHold(0f, g, .5f, "climb at full speed 0.5 s"),
            new ClimbHold(0f, 0f, .5f, "stop: hang 0.5 s") { Cut = true },
            new ClimbHold(0f, .05f * g, 2.8f, "the slowest climb (Climb 0.05, 0.2 u/s) 2.8 s: a full cycle") { Cut = true },
            new ClimbHold(0f, .1f * g, 1.6f, "slow climb (Climb 0.1) 1.6 s"),
            new ClimbHold(0f, 0f, .4f, "hang 0.4 s"),
            new ClimbHold(0f, g, .3f, "resume at full speed 0.3 s") { Cut = true },
            new ClimbUntil(0f, -g, NotClimbing, 3f, "climb down at full speed to the ground: the release") { Cut = true },
            new Hold(0f, .6f, "stand 0.6 s"),
        };

        // A shorter climb from the `dir` side (the facing on the vine): grab from the ground, climb 0.8 s, hang, climb down.
        static List<CaptureStep> SideClimb(float g, float dir) => new()
        {
            new Hold(0f, .3f, "stand 0.3 s"),
            new Until(.5f * dir, Near(VineBaseX), 3f, "walk to the vine"),
            new ClimbUntil(.5f * dir, g, Climbing, .3f, "push up the vine: grab from the ground") { Cut = true },
            new ClimbHold(0f, g, .8f, "climb at full speed 0.8 s"),
            new ClimbHold(0f, 0f, .4f, "hang 0.4 s") { Cut = true },
            new ClimbUntil(0f, -g, NotClimbing, 3f, "climb down to the ground: the release") { Cut = true },
            new Hold(0f, .5f, "stand 0.5 s"),
        };

        // A jump toward Vine_Base with the climb pressed in the air (the grab from a jump), climb, hang, a leap right (with
        // the facing); land, walk back from the right, grab from the ground (now facing left), climb, a leap right again
        // (against the facing).
        static List<CaptureStep> JumpGrabLeap(float g) => new()
        {
            new Hold(0f, .4f, "stand 0.4 s"),
            new Press(.6f, "jump right toward the vine"),
            new Until(.6f, Airborne, .2f, "take off"),
            new Until(.6f, r => r.ColliderCentre.x - r.Origin.x >= VineBaseX - .2f, 1f, "fly toward the vine"),
            new ClimbUntil(.6f, g, Climbing, 1f, "climb pressed in the air: the grab from a jump") { Cut = true },
            new ClimbHold(0f, g, .4f, "climb at full speed 0.4 s"),
            new ClimbHold(0f, 0f, .4f, "hang 0.4 s"),
            new Press(1f, "leap right off the vine") { Cut = true },
            new Until(1f, Airborne, .2f, "the leap"),
            new Until(1f, Grounded, 3f, "fly, land running"),
            new Until(0f, Still, 1f, "release: stop"),
            new Hold(0f, .4f, "stand 0.4 s"),
            new Until(-.5f, Near(VineBaseX), 4f, "walk back left to the vine"),
            new ClimbUntil(-.5f, g, Climbing, .3f, "push up the vine: grab (facing left)") { Cut = true },
            new ClimbHold(0f, g, .5f, "climb at full speed 0.5 s"),
            new ClimbHold(0f, 0f, .3f, "hang 0.3 s"),
            new Press(1f, "leap right, against the facing") { Cut = true },
            new Until(1f, Airborne, .2f, "the leap"),
            new Until(0f, Grounded, 3f, "released in the air: land"),
            new Hold(0f, .6f, "stand 0.6 s"),
        };

        // Gravity down: a jump up at Vine_High (its end at 2.0) with the climb pressed, climb, hang, then climb down past its
        // end: released in the air, a 1.7 u fall. Gravity up: grab Vine_High from the ceiling, climb away from it, hang, then on
        // past the vine's end: released, a 1.7 u fall back to the ceiling.
        static List<CaptureStep> Release(float g) => g > 0f
            ? new List<CaptureStep>
            {
                new Hold(0f, .3f, "stand 0.3 s"),
                new Until(.5f, Near(VineHighX), 3f, "walk under the hanging vine"),
                new Until(0f, Still, 1f, "stop"),
                new Press(0f, "jump up at the vine"),
                new Until(0f, Airborne, .2f, "take off"),
                new ClimbUntil(0f, 1f, Climbing, 1f, "climb pressed in the air: the grab") { Cut = true },
                new ClimbHold(0f, 1f, .3f, "climb 0.3 s"),
                new ClimbHold(0f, 0f, .4f, "hang 0.4 s"),
                new ClimbUntil(0f, -1f, NotClimbing, 3f, "climb down past the vine's end: the release") { Cut = true },
                new Until(0f, Grounded, 3f, "the fall"),
                new Hold(0f, .8f, "stand 0.8 s"),
            }
            : new List<CaptureStep>
            {
                new Hold(0f, .3f, "stand 0.3 s"),
                new Until(.5f, Near(VineHighX), 3f, "walk under the vine"),
                new ClimbUntil(0f, -1f, Climbing, .3f, "push away from the ceiling: grab from the ground") { Cut = true },
                new ClimbHold(0f, -1f, .2f, "climb 0.2 s"),
                new ClimbHold(0f, 0f, .4f, "hang 0.4 s"),
                new ClimbUntil(0f, -1f, NotClimbing, 3f, "climb on past the vine's end: the release") { Cut = true },
                new Until(0f, Grounded, 3f, "the fall back to the ceiling"),
                new Hold(0f, .8f, "stand 0.8 s"),
            };

        // PAX-V07 §3, §6: inputs pressed during Climb, Hang and Leap (CatVisualOnlyParityTests; not for the critic).
        static List<CaptureStep> ParityClimb(float g) => new()
        {
            new Hold(0f, .2f, "stand"),
            new Until(.5f, Near(VineBaseX), 3f, "walk to the vine"),
            new ClimbUntil(.5f, g, Climbing, .3f, "grab"),
            new ClimbHold(1f, g, .2f, "a move pressed while climbing"),
            new ClimbHold(-1f, -g, .1f, "reversed while climbing"),
            new ClimbHold(0f, 0f, .2f, "hang"),
            new ClimbHold(-1f, 0f, .1f, "a move pressed while hanging"),
            new ClimbHold(0f, g, .2f, "climb"),
            new ClimbHold(0f, 0f, .1f, "hang"),
            new Press(1f, "leap from Hang"),
            new ClimbHold(-1f, g, .1f, "reverse and climb pressed during Leap"),
            new Press(-1f, "jump pressed during Leap"),
            new Until(0f, Grounded, 3f, "land"),
            new Until(0f, Still, 1f, "stop"),
            new Until(-.5f, Near(VineBaseX), 4f, "walk back to the vine"),
            new ClimbUntil(-.5f, g, Climbing, .3f, "grab"),
            new ClimbHold(0f, g, .3f, "climb"),
            new Press(-1f, "leap while climbing"),
            new ClimbHold(1f, 0f, .1f, "a move pressed during Leap"),
            new Until(0f, Grounded, 3f, "land"),
            new Hold(0f, .3f, "stand"),
        };

        static IEnumerable<CaptureScenario> ClimbPair(string name, string what, Func<float, List<CaptureStep>> script, float startX)
        {
            yield return new CaptureScenario { Name = name + "_down", Description = $"the climb bench, gravity down: {what}", Room = _ => ClimbBench(false), StartX = startX, Steps = _ => script(1f) };
            yield return new CaptureScenario { Name = name + "_up", Description = $"the climb bench mirrored, gravity up (on the ceiling): {what}", Room = _ => ClimbBench(true), StartX = startX, Steps = _ => UpAir(script(-1f)) };
        }

        static IEnumerable<CaptureScenario> ClimbScenarios() =>
            ClimbPair("climb_ground", "from the left, a grab from the ground, full speed, hang, the slowest analog climb, a slow climb, hang, resume, climb down to the ground", g => GroundClimb(g, 1f), 5.5f)
            .Concat(ClimbPair("climb_side_right", "a grab from the ground coming from the left (facing right on the vine), climb, hang, climb down", g => SideClimb(g, 1f), 5.5f))
            .Concat(ClimbPair("climb_side_left", "a grab from the ground coming from the right (facing left on the vine), climb, hang, climb down", g => SideClimb(g, -1f), 10.5f))
            .Concat(ClimbPair("climb_jump_leap", "a grab from a jump, climb, hang, a leap right with the facing; walk back, grab facing left, a leap right against the facing", JumpGrabLeap, 6.2f))
            .Concat(ClimbPair("climb_release", "grab the hanging vine, climb, hang, climb past its end: released in the air, the fall", Release, 12f))
            .Concat(ClimbPair("parity_climb", "parity script: inputs during Climb, Hang and Leap", ParityClimb, 5.5f));
    }
}
