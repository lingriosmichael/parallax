using System;
using System.Collections.Generic;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Routes;
using Parallax.Editor.Setup;
using UnityEngine;

namespace Parallax.Editor.Art
{
    // PAX-105 (D-110, §2.4): the wall scenarios, gravity down only (no cling with gravity up), in a capture-only wall bench
    // (never a level, never saved): a floor and a ceiling 7 u apart, grip walls Left_Wall x 8-9 and Right_Wall x 11-12 (y 0-6.5), so the
    // gap between them is a 2 u shaft and each has an outer face. wall_right_down jumps at Left_Wall's outer face (a wall on
    // the cat's right), wall_left_down at Right_Wall's (on its left); wall_shaft_down climbs the shaft. For the developer's
    // contact sheet and the art check (WallClingPoseTests, CatCapture.WallPlacement).
    static partial class CatCaptureScenarios
    {
        static SoloRoomDefinition WallBench() => new(0, 0f, BenchWidth, new[]
        {
            Element(SoloRoomElementKind.Floor, "Floor", new Vector2(BenchWidth * .5f, -.5f), new Vector2(BenchWidth, 1f)),
            Element(SoloRoomElementKind.Ceiling, "Ceiling", new Vector2(BenchWidth * .5f, BenchHeight + .5f), new Vector2(BenchWidth, 1f)),
            Element(SoloRoomElementKind.Checkpoint, "Checkpoint", new Vector2(2f, 0f), Vector2.zero),
            Element(SoloRoomElementKind.Door, "Door", new Vector2(BenchWidth - .5f, .75f), new Vector2(.6f, 1.5f)),
            Element(SoloRoomElementKind.GripWall, "Left_Wall", new Vector2(8.5f, 3.25f), new Vector2(1f, 6.5f)),
            Element(SoloRoomElementKind.GripWall, "Right_Wall", new Vector2(11.5f, 3.25f), new Vector2(1f, 6.5f)),
        }, Array.Empty<SoloRoomOpening>(), Array.Empty<RequiredJump>());

        /// <summary>One tick with Grab pressed (and a move).</summary>
        sealed class GrabPress : CaptureStep
        {
            readonly float move;
            public GrabPress(float move, string label) { this.move = move; Label = label; }
            public override bool Next(CatCaptureRig rig, int done, out CatCommand c) { c = new CatCommand { Move = move, GrabPressed = true }; return done < 1; }
        }

        static bool Falling(CatCaptureRig r) => !r.Cat.IsGrounded && Vector2.Dot(r.Body.linearVelocity, r.Gravity.Direction) > 0f;
        static bool Clinging(CatCaptureRig r) => r.Cat.IsClinging;
        static bool NotClinging(CatCaptureRig r) => !r.Cat.IsClinging;

        // `dir` +1: the wall is on the cat's right. A running jump at it, Grab at the top of the rise, hang, slide to the floor;
        // walk back, jump at it again, Grab, a wall jump away, land.
        static List<CaptureStep> WallScript(float dir) => new()
        {
            new Hold(0f, .3f, "stand 0.3 s"),
            new Press(dir, "a running jump at the wall"),
            new Until(dir, Falling, 1f, "rise against the wall"),
            new GrabPress(0f, "Grab: the latch") { Cut = true },
            new Until(0f, Grounded, 3f, "slide down to the floor"),
            new Hold(0f, .4f, "stand 0.4 s"),
            new Until(-dir, r => Mathf.Abs(r.ColliderCentre.x - r.Origin.x - (dir > 0f ? 6.2f : 13.8f)) <= .15f, 3f, "walk back"),
            new Until(0f, Still, 1f, "stop"),
            new Press(dir, "a running jump at the wall"),
            new Until(dir, Falling, 1f, "rise against the wall"),
            new GrabPress(0f, "Grab"),
            new Until(0f, Clinging, .2f, "the latch"),
            new Hold(0f, .25f, "slide 0.25 s"),
            new Press(0f, "wall jump") { Cut = true },
            new Until(-dir, Grounded, 3f, "the wall jump: land"),
            new Hold(0f, .5f, "stand 0.5 s"),
        };

        // From the shaft's middle: a jump at Left_Wall, Grab, then two wall jumps from side to side (latch mode), then the
        // stick pushed away from the wall: the fall.
        static List<CaptureStep> ShaftScript() => new()
        {
            new Hold(0f, .3f, "stand 0.3 s"),
            new Press(-1f, "a jump at Left_Wall"),
            new Until(-1f, Falling, 1f, "rise against it"),
            new GrabPress(0f, "Grab: the latch") { Cut = true },
            new Until(0f, Clinging, .2f, "latched"),
            new Press(0f, "wall jump right"),
            new Until(1f, Clinging, 1f, "latch Right_Wall") { Cut = true },
            new Press(0f, "wall jump left"),
            new Until(-1f, Clinging, 1f, "latch Left_Wall"),
            new Hold(0f, .3f, "slide 0.3 s"),
            new Until(1f, NotClinging, .2f, "push away: let go") { Cut = true },
            new Until(0f, Grounded, 3f, "fall"),
            new Hold(0f, .5f, "stand 0.5 s"),
        };

        static IEnumerable<CaptureScenario> WallScenarios()
        {
            yield return new CaptureScenario { Name = "wall_right_down", Description = "the wall bench: a wall on the right (Left_Wall's outer face): grab, hang, slide to the floor; again, a wall jump away", Room = _ => WallBench(), StartX = 6.2f, Steps = _ => WallScript(1f) };
            yield return new CaptureScenario { Name = "wall_left_down", Description = "the wall bench: a wall on the left (Right_Wall's outer face): grab, hang, slide to the floor; again, a wall jump away", Room = _ => WallBench(), StartX = 13.8f, Steps = _ => WallScript(-1f) };
            yield return new CaptureScenario { Name = "wall_shaft_down", Description = "the wall bench's 2 u shaft: grab, two wall jumps from side to side, push away, fall", Room = _ => WallBench(), StartX = 10f, Steps = _ => ShaftScript() };
        }

        public static string[] WallScenarioNames => WallScenarios().Select(s => s.Name).ToArray();
    }

    public static partial class CatCapture
    {
        /// <summary>PAX-105 (§2.4, WallClingPoseTests): plays a wall scenario (no rendering) at 60 fps with the interpolated
        /// pose, and measures every frame the presenter shows WallCling, WallSlide or WallJump: d = how far the drawing's edge
        /// toward the wall lies past the face (units; negative = short of it). Clinging frames: the paws should be within
        /// 0.03 u of the face and nothing more than 0.02 u inside (-0.03 ≤ d ≤ 0.02). WallJump frames: nothing more than 0.02 u
        /// inside; the push-off frame (the first) also within 0.03 u. `sides` lists the wall sides seen (+1 right, -1 left).
        /// Null when it ran.</summary>
        public static string WallPlacement(string scenarioName, out int clingFrames, out float minClingD, out float maxClingD,
            out int jumpFrames, out float pushOffD, out float maxJumpD, out int[] sides)
        {
            clingFrames = jumpFrames = 0; minClingD = float.PositiveInfinity; maxClingD = maxJumpD = pushOffD = float.NegativeInfinity;
            var seen = new SortedSet<int>();
            sides = Array.Empty<int>();
            CaptureScenario scenario = CatCaptureScenarios.All.FirstOrDefault(s => s.Name == scenarioName);
            if (scenario == null) return $"no scenario '{scenarioName}'";
            using var session = new RouteSession();
            using var alpha = new SpriteAlphaCache();
            List<CaptureStep> steps = scenario.Steps(session);
            session.Clear();
            CatCaptureRig rig = CatCaptureRig.Build(scenario.Room(session), scenario.StartX);
            try
            {
                Transform root = rig.Cat.transform;
                var runner = new Runner(steps);
                float tickLength = TickTime.SecondsPerTick;
                Vector2 prevPos = rig.Body.position, curPos = prevPos; float prevRot = rig.Body.rotation, curRot = prevRot;
                Vector3 tickPosition = root.position; Quaternion tickRotation = root.rotation;
                float faceX = 0f; int side = 0; bool inJump = false;
                for (int n = 0; ; n++)
                {
                    double loopTime = n / (double)FrameRate + tickLength;
                    int due = CatCaptureMath.TicksDueBy(loopTime, tickLength);
                    bool finished = false;
                    while (rig.Tick < due)
                    {
                        root.SetPositionAndRotation(tickPosition, tickRotation);
                        Physics2D.SyncTransforms();
                        if (!runner.Next(rig, out CatCommand command)) { finished = true; break; }
                        rig.Step(command);
                        prevPos = curPos; prevRot = curRot;
                        curPos = rig.Body.position; curRot = rig.Body.rotation;
                        tickPosition = root.position; tickRotation = root.rotation;
                        if (rig.Cat.ClingFace != null)
                        {
                            side = rig.Cat.ClingSide;
                            Bounds b = rig.Cat.ClingFace.bounds;
                            faceX = side > 0 ? b.min.x : b.max.x;
                        }
                    }
                    if (finished) break;
                    float a = CatCaptureMath.InterpolationAlpha(loopTime, rig.Tick, tickLength);
                    CatCaptureMath.InterpolatedPose(prevPos, prevRot, curPos, curRot, a, InterpolationTeleportUnits, out Vector2 pos, out float rot);
                    root.SetPositionAndRotation(new Vector3(pos.x, pos.y, tickPosition.z), Quaternion.Euler(0f, 0f, rot));
                    rig.Presenter.Present(1f / FrameRate);
                    CatAnimState state = rig.Presenter.State;
                    bool cling = state == CatAnimState.WallCling || state == CatAnimState.WallSlide, jump = state == CatAnimState.WallJump;
                    if (!jump) inJump = false;
                    if ((!cling && !jump) || side == 0) continue;
                    float d = PastTheFace(alpha, rig.BodyRenderer, faceX, side);
                    seen.Add(side);
                    if (cling) { clingFrames++; minClingD = Mathf.Min(minClingD, d); maxClingD = Mathf.Max(maxClingD, d); }
                    else
                    {
                        if (!inJump) pushOffD = Mathf.Max(pushOffD, d);
                        inJump = true;
                        jumpFrames++;
                        maxJumpD = Mathf.Max(maxJumpD, d);
                    }
                }
                root.SetPositionAndRotation(tickPosition, tickRotation);
            }
            finally
            {
                session.Clear();
            }
            sides = seen.ToArray();
            return null;
        }

        // How far the drawn pixels reach past the face x toward a wall on `side` (units; negative = short of it).
        static float PastTheFace(SpriteAlphaCache cache, SpriteRenderer body, float faceX, int side)
        {
            if (body.sprite == null) return float.NegativeInfinity;
            SpriteAlphaCache.Entry e = cache.Get(body.sprite);
            Matrix4x4 toWorld = body.transform.localToWorldMatrix;
            float best = float.NegativeInfinity;
            for (int c = 0; c < e.Columns.Length; c++)
                foreach (int y in e.Columns[c])
                    best = Mathf.Max(best, side * (CatCaptureMath.PixelToWorld(e.ColumnX[c], y, e.PivotPx, e.Ppu, toWorld).x - faceX));
            return best;
        }
    }
}
