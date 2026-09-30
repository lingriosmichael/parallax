using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Parallax.Core;
using UnityEngine;

namespace Parallax.Editor.Art
{
    /// <summary>PAX-V07 gauntlet item 0: turns a scenario's frame rows into checks.json (and one summary line). Every check
    /// is computed from the analytic measurements in FrameRow.M, never from the rendered images.</summary>
    static class CatCaptureReport
    {
        // State vs motor: one row per rule. A rule is broken on a frame when its test is true. Later items append rows
        // (e.g. no Run below the run threshold, TakeOff within a frame of the jump).
        public static readonly List<StateRule> Rules = new()
        {
            new StateRule("air-state-while-grounded", "Rise, Apex or Fall (or TakeOff after the jump tick) shown while the motor has been grounded for more than one frame",
                c => !c.Excluded && c.OnGround && ((c.Air(c.Row.State) && c.GroundedFrames > 1) || (c.Row.State == CatAnimState.TakeOff && !c.Row.Jumped && c.GroundedFrames > 1))),
            // Item 2: a ground state may hold through a one-tick ground blip or the first moment off a ledge (the presenter's
            // airGraceDrop), never once the cat has dropped past it.
            new StateRule("ground-state-while-airborne", "Idle, Walk or Run shown while the motor has been airborne (not climbing) for more than one frame, once the cat has dropped more than airGraceDrop below where it stood",
                c => !c.Excluded && (c.Row.State == CatAnimState.Idle || c.Row.State == CatAnimState.Walk || c.Row.State == CatAnimState.Run) && !c.OnGround && !c.Row.Climbing && c.AirborneFrames > 1
                     && (c.Speeds == null || c.DropSinceGround > c.Speeds.AirGraceDrop + CaptureSpeeds.DropTolerance)),
            // Item 2: the air rows.
            new StateRule("takeoff-late", "a jump from the ground (JumpedThisStep's first frame) with TakeOff shown neither on that frame nor the next",
                c => !c.Excluded && c.Prev != null && c.Prev.Jumped && c.Prev.Grounded && !c.Prev.Climbing && !c.PrevPrevJumped
                     && c.Prev.State != CatAnimState.TakeOff && c.Row.State != CatAnimState.TakeOff),
            new StateRule("apex-outside-band", "Apex shown while the drawn speed along gravity is outside the apex band (airThreshold) for more than 2 frames",
                c => !c.Excluded && c.ApexOutsideFrames > CaptureSpeeds.LagFrames),
            // PAX-V07 item 1: the ground rows.
            new StateRule("turn-off-ground", "Turn shown on a frame the motor isn't grounded (no Turn in the air)",
                c => !c.Excluded && c.Row.State == CatAnimState.Turn && (!c.OnGround || c.Row.Climbing)),
            // PAX-A14 (developer, 2026-09-30): the mirror of walk-above-run: Run waits for one of its switch frames to hand back.
            new StateRule("run-below-exit", "Run shown on the ground below runExitFraction x MaxSpeed (the drawn speed) for more than the wait for a switch frame plus 2 frames",
                c => !c.Excluded && c.RunSlowFrames > CaptureSpeeds.LagFrames + (c.Speeds != null ? c.Speeds.RunSwitchWaitFrames : 0)),
            // Walk waits for a switch frame where the paws of both gaits match (ruled 2026-09-30): up to the longest run of Walk
            // frames between two switch frames (CaptureSpeeds.SwitchWaitFrames at the run threshold) on top of the lag.
            new StateRule("walk-above-run", "Walk shown on the ground at or above runFraction x MaxSpeed (the drawn speed) for more than the wait for a switch frame plus 2 frames",
                c => !c.Excluded && c.WalkFastFrames > CaptureSpeeds.LagFrames + (c.Speeds != null ? c.Speeds.SwitchWaitFrames : 0)),
            // A flicker is a drawing shown for one frame: the clip on screen, not the state name. Turn's one-frame hold draws the
            // gait's own flip frame (ruled 2026-09-30), so it isn't one.
            new StateRule("one-frame-state", "a clip shown for exactly one frame between two frames of other clips (a flicker)",
                // A Respawn that input ends on the next frame is §3 (any input ends it on that frame), not a flicker.
                c => !c.Excluded && c.Prev != null && c.Next != null && !c.Next.Holding && !c.Next.Frozen
                     && c.Row.Clip != c.Prev.Clip && c.Row.Clip != c.Next.Clip
                     && !(c.Row.State == CatAnimState.Respawn && Mathf.Abs(c.Next.VAlong) > 0f)),
            // PAX-A14: a cat standing still on a moving floor stands; the floor's motion isn't its own.
            new StateRule("walk-while-carried", "Walk or Run shown while a moving floor carries the cat and it has no speed of its own, for more than 2 frames",
                c => !c.Excluded && c.CarriedWalkFrames > CaptureSpeeds.LagFrames),
            new StateRule("facing-late", "on the ground, moving against the facing above walkEnter for longer than the Turn clip plus the lag",
                c => !c.Excluded && c.Speeds != null && c.AgainstFacingFrames > Mathf.CeilToInt(c.Speeds.TurnSeconds * CatCapture.FrameRate) + CaptureSpeeds.LagFrames),
            new StateRule("climb-without-climbing", "Climb or Hang shown while the motor isn't climbing",
                c => (c.Row.State == CatAnimState.Climb || c.Row.State == CatAnimState.Hang) && !c.Row.Climbing),
            // Item 3: the vine rows.
            new StateRule("vine-state-missing", "the motor climbing (not in a hold) with neither Climb nor Hang shown for more than 2 frames",
                c => !c.Excluded && c.OffVineStateFrames > CaptureSpeeds.LagFrames),
            new StateRule("hang-while-moving", "Hang shown while the cat moves along the vine (the drawn speed above climbStillSpeed) for more than 2 frames",
                c => !c.Excluded && c.HangMovingFrames > CaptureSpeeds.LagFrames),
            new StateRule("climb-while-still", "Climb shown while the cat is still on the vine for more than MinStateFrames + 2 frames",
                c => !c.Excluded && c.Speeds != null && c.ClimbStillFrames > c.Speeds.MinStateFrames + CaptureSpeeds.LagFrames),
            new StateRule("leap-late", "climbing ended with the leap's launch (the body against gravity within the leap band) and Leap shown neither on that frame nor the next",
                c => !c.Excluded && c.Speeds != null && c.Prev != null && c.Prev.Climbing && !c.Row.Climbing && -c.Row.VGravity >= c.Speeds.LeapMin && -c.Row.VGravity <= c.Speeds.LeapMax
                     && c.Row.State != CatAnimState.Leap && (c.Next == null || c.Next.State != CatAnimState.Leap)),
            // Item 2: a landing shows Land or HardLand, or (moving at touchdown, or acting on it) the gait, Turn or a TakeOff:
            // by the frame after touchdown, never an air state.
            new StateRule("land-late", "an air state (TakeOff, Rise, Apex, Fall) still shown on the frame after touchdown (after at least 3 airborne frames)",
                c => !c.Excluded && c.OnGround && c.GroundedFrames == 2 && c.LastAirRun >= 3 && !c.Row.Jumped
                     && (c.Air(c.Row.State) || c.Row.State == CatAnimState.TakeOff)),
        };

        public sealed class Result { public string Json; public string Line; public bool Pass; }

        public static Result Evaluate(string scenario, List<FrameRow> rows, bool died, bool completed, bool expectDeath, string parity, CaptureSpeeds speeds = null)
        {
            // PAX-V07 item 1: frames of a scenario's setup steps (getting into place) are rendered but not checked.
            List<FrameRow> all = rows;
            int setupFrames = rows.Count(r => r.Setup);
            rows = rows.Where(r => !r.Setup).ToList();
            var j = new Json();
            j.Open().Str("scenario", scenario).Num("setup_frames_unchecked", setupFrames).Num("frames", rows.Count).Num("ticks", rows.Count > 0 ? rows[^1].Tick : 0)
                .Bool("died", died).Bool("expectDeath", expectDeath).Bool("completed", completed);
            bool pass = true;
            var line = new StringBuilder(scenario);

            // --- paws in floor ---
            var groundedTicks = new HashSet<int>(rows.Where(r => r.Grounded).Select(r => r.Tick));
            bool NearGround(FrameRow r)
            {
                for (int d = -CaptureThresholds.NearGroundTicks; d <= CaptureThresholds.NearGroundTicks; d++) if (groundedTicks.Contains(r.Tick + d)) return true;
                return false;
            }
            // Item 2: the ticks right after a gravity flip (the body turns 180 degrees in one tick, drawn half-turned into the
            // surface it leaves) are the Flip state's (item 5), left out of the paws and head checks and counted apart.
            var flipTicks = new HashSet<int>();
            for (int i = 0; i < rows.Count; i++)
                if (rows[i].GravityDown != (i > 0 ? rows[i - 1].GravityDown : true))   // every room starts with gravity down
                    for (int d = 0; d <= CaptureThresholds.NearGroundTicks; d++) flipTicks.Add(rows[i].Tick + d);
            bool Flipping(FrameRow r) => flipTicks.Contains(r.Tick);
            bool Checkable(FrameRow r) => r.M.HasSprite && r.M.SurfaceFound && !r.Holding && !r.Frozen && !Flipping(r);
            // Item 2: every frame whose drawn pixels come within RayMarginUnits (0.5 u) of a surface along gravity (a surface is
            // only found that close: Checkable), grounded or not, so the takeoff until 0.5 u clear and the last 0.5 u before
            // touchdown are covered, plus air frames within NearGroundTicks of a grounded tick.
            // Climbing frames are reported apart, not failed: on the vine the vertical pose centred on the collider hangs below
            // it, into the ground at a vine's foot (art: NEEDED_ASSETS), and V07 doesn't draw poses off their pivot.
            var climbFloor = rows.Where(r => Checkable(r) && r.Climbing).ToList();
            var pawFrames = rows.Where(r => Checkable(r) && !r.Climbing).ToList();
            FrameRow worstPaw = pawFrames.OrderByDescending(r => r.M.PenetrationSpritePx).FirstOrDefault();
            float worstPawPx = worstPaw != null ? worstPaw.M.PenetrationSpritePx : 0f;
            var pawFails = pawFrames.Where(r => r.M.PenetrationSpritePx > CaptureThresholds.PawsInFloorSpritePx).ToList();
            bool pawPass = pawFails.Count == 0;
            pass &= pawPass;
            j.Key("paws_in_floor").Open().Num("threshold_sprite_px", CaptureThresholds.PawsInFloorSpritePx).Num("frames_checked", pawFrames.Count)
                .Num("max_depth_sprite_px", worstPawPx).Num("worst_frame", worstPaw?.Frame ?? -1)
                .Num("max_depth_grounded_sprite_px", Max(pawFrames.Where(r => r.Grounded), r => r.M.PenetrationSpritePx))
                .Num("max_depth_air_near_ground_sprite_px", Max(pawFrames.Where(r => !r.Grounded), r => r.M.PenetrationSpritePx))
                .Num("frames_over", pawFails.Count).Ints("first_frames_over", pawFails.Take(20).Select(r => r.Frame))
                .Num("climb_frames_over_reported", climbFloor.Count(r => r.M.PenetrationSpritePx > CaptureThresholds.PawsInFloorSpritePx))
                .Num("climb_max_depth_sprite_px", Max(climbFloor, r => r.M.PenetrationSpritePx)).Bool("pass", pawPass).Close();
            line.Append($" | paws max {worstPawPx:F1}sp ({pawFails.Count} over)");

            // --- head in ceiling (item 2): no drawn pixel inside a solid above the cat (a low ceiling), any frame ---
            var headFrames = rows.Where(r => r.M.HasSprite && r.M.CeilingFound && !r.Holding && !r.Frozen && !Flipping(r)).ToList();
            FrameRow worstHead = headFrames.OrderByDescending(r => r.M.CeilingPenetrationSpritePx).FirstOrDefault();
            var headFails = headFrames.Where(r => r.M.CeilingPenetrationSpritePx > CaptureThresholds.HeadInCeilingSpritePx).ToList();
            pass &= headFails.Count == 0;
            j.Key("head_in_ceiling").Open().Num("threshold_sprite_px", CaptureThresholds.HeadInCeilingSpritePx).Num("frames_checked", headFrames.Count)
                .Num("max_depth_sprite_px", worstHead != null ? worstHead.M.CeilingPenetrationSpritePx : 0f).Num("worst_frame", worstHead?.Frame ?? -1)
                .Num("frames_over", headFails.Count).Ints("first_frames_over", headFails.Take(20).Select(r => r.Frame))
                .Num("flip_frames_unchecked", rows.Count(Flipping)).Bool("pass", headFails.Count == 0).Close();
            if (headFrames.Count > 0) line.Append($" | head max {worstHead.M.CeilingPenetrationSpritePx:F1}sp ({headFails.Count} over)");

            // --- float gap on grounded frames ---
            var ground = rows.Where(r => Checkable(r) && r.Grounded).ToList();
            float Gap(FrameRow r) => Mathf.Max(0f, -r.M.PenetrationSpritePx);
            float gapMax = ground.Count > 0 ? ground.Max(Gap) : 0f, gapMean = ground.Count > 0 ? ground.Average(Gap) : 0f;
            int gapOver = ground.Count(r => Gap(r) > CaptureThresholds.FloatGapSpritePx);
            j.Key("float_gap").Open().Num("threshold_sprite_px", CaptureThresholds.FloatGapSpritePx).Num("frames_checked", ground.Count)
                .Num("max_sprite_px", gapMax).Num("mean_sprite_px", gapMean).Num("frames_over", gapOver)
                .Ints("first_frames_over", ground.Where(r => Gap(r) > CaptureThresholds.FloatGapSpritePx).Take(20).Select(r => r.Frame))
                .Str("note", "reported, not pass/fail").Close();
            line.Append($" | gap max {gapMax:F1}sp mean {gapMean:F1}");

            // --- foot slide ---
            // A frame can only change on a 60 fps frame, so at a sprite change a paw that is exactly on its print in
            // continuous time shows up to one frame's travel (speed / 60) off it; a slide keeps growing past that. The
            // allowance per plant is 3 phone px plus that quantum at the plant's top speed (HARNESS.md).
            var plants = FootSlide(rows);
            float Quantum(float speed) => speed / CatCapture.FrameRate * CaptureThresholds.PhonePixelsPerUnit;
            float Allowed((int plant, int lift, float drift, string sprites, float speed) p) => CaptureThresholds.FootSlidePhonePx + Quantum(p.speed);
            float slideMax = plants.Count > 0 ? plants.Max(p => p.drift) : 0f, slideMean = plants.Count > 0 ? plants.Average(p => p.drift) : 0f;
            int slideOver = plants.Count(p => p.drift > Allowed(p));
            int strictOver = plants.Count(p => p.drift > CaptureThresholds.FootSlidePhonePx);
            bool slidePass = slideOver == 0;
            pass &= slidePass;
            j.Key("foot_slide").Open().Num("threshold_phone_px", CaptureThresholds.FootSlidePhonePx)
                .Str("allowance", "threshold + one 60 fps frame of travel at the plant's top speed").Num("planted_paws", plants.Count)
                .Num("max_phone_px", slideMax).Num("mean_phone_px", slideMean).Num("paws_over", slideOver).Num("paws_over_3pp_without_allowance", strictOver)
                .Raw("worst", "[" + string.Join(",", plants.OrderByDescending(p => p.drift - Allowed(p)).Take(10).Select(p =>
                    $"{{\"plant_frame\":{p.plant},\"lift_frame\":{p.lift},\"drift_phone_px\":{F(p.drift)},\"allowed_phone_px\":{F(Allowed(p))},\"top_speed\":{F(p.speed)},\"sprites\":\"{p.sprites}\"}}")) + "]")
                .Bool("pass", slidePass).Close();
            line.Append($" | slide max {slideMax:F1}pp mean {slideMean:F1} ({slideOver} over allowance, {strictOver} over 3pp)");

            // --- pose pop ---
            var changes = rows.Where(r => r.PopPhonePx >= 0f).ToList();
            var pops = changes.Where(r => r.PopPhonePx > CaptureThresholds.PosePopPhonePx).ToList();
            var boxPops = changes.Where(r => r.BoxPopPhonePx > CaptureThresholds.PoseBoxPhonePx).ToList();
            bool popPass = pops.Count == 0;
            pass &= popPass;
            string PopList(IEnumerable<FrameRow> list) => "[" + string.Join(",", list.Take(20).Select(r =>
                $"{{\"frame\":{r.Frame},\"sprite\":\"{r.Sprite}\",\"from\":\"{Prev(all, r)?.Sprite}\",\"centroid_phone_px\":{F(r.PopPhonePx)},\"box_phone_px\":{F(r.BoxPopPhonePx)}}}")) + "]";
            j.Key("pose_pop").Open().Num("threshold_phone_px", CaptureThresholds.PosePopPhonePx).Num("box_threshold_phone_px", CaptureThresholds.PoseBoxPhonePx)
                .Num("sprite_changes", changes.Count).Num("centroid_max_phone_px", changes.Count > 0 ? changes.Max(r => r.PopPhonePx) : 0f)
                .Num("box_max_phone_px", changes.Count > 0 ? changes.Max(r => r.BoxPopPhonePx) : 0f)
                .Num("centroid_pops", pops.Count).Num("box_pops", boxPops.Count)
                .Num("carrier_offset_max_phone_px", rows.Count > 0 ? rows.Max(r => r.CarrierOffsetPhonePx) : 0f)
                .Str("carrier_note", "item 2: pops are measured against the Visual pivot's move; carrier_offset is how far that pivot moved beyond the root in one frame (the TakeOff anchor and its release)")
                .Raw("worst", PopList(changes.OrderByDescending(r => r.PopPhonePx))).Bool("pass", popPass).Close();
            line.Append($" | pop max {(changes.Count > 0 ? changes.Max(r => r.PopPhonePx) : 0f):F1}pp ({pops.Count} over)");

            // --- the body on the collider (item 3): every Climb or Hang frame on the vine ---
            var vine = rows.Where(r => r.ClimbBodyOffsetSpritePx >= 0f).ToList();
            var vineOver = vine.Where(r => r.ClimbBodyOffsetSpritePx > CaptureThresholds.ClimbBodyOffsetSpritePx).ToList();
            pass &= vineOver.Count == 0;
            j.Key("climb_body_on_collider").Open().Num("threshold_sprite_px", CaptureThresholds.ClimbBodyOffsetSpritePx).Num("frames_checked", vine.Count)
                .Num("max_sprite_px", vine.Count > 0 ? vine.Max(r => r.ClimbBodyOffsetSpritePx) : 0f).Num("frames_over", vineOver.Count)
                .Ints("first_frames_over", vineOver.Take(20).Select(r => r.Frame))
                .Num("lifted_frames", vine.Count(r => r.ClimbLiftUnits > 1e-4f)).Num("max_lift_units", vine.Count > 0 ? vine.Max(r => r.ClimbLiftUnits) : 0f)
                .Str("note", "the body centre (the drawn pixels' centroid after a 5 px erosion) against the collider centre plus the Visual's lift off its rest pose (reported: the lift keeps the pose out of the ground at a vine's foot)")
                .Bool("pass", vineOver.Count == 0).Close();
            if (vine.Count > 0) line.Append($" | vine body max {vine.Max(r => r.ClimbBodyOffsetSpritePx):F1}sp ({vineOver.Count} over), lift max {vine.Max(r => r.ClimbLiftUnits):F2}u");

            // --- a still cat on a moving floor (PAX-A14), frame to frame at the floor's steady speed: the drawn cat's and the drawn
            // floor's judder (each frame's move against speed x 1/60 s) and the cat's move against the floor ---
            float jitter = 0f, catJudder = 0f, floorJudder = 0f; int carried = 0;
            float pp = CaptureThresholds.PhonePixelsPerUnit, frameDt = 1f / CatCapture.FrameRate;
            for (int i = 1; i < rows.Count; i++)
            {
                FrameRow a = rows[i - 1], b = rows[i];
                if (!(a.Carried && b.Carried && (a.CarrierVelocity - b.CarrierVelocity).sqrMagnitude < 1e-4f && Mathf.Abs(a.VAlong) < 1e-3f && Mathf.Abs(b.VAlong) < 1e-3f
                      && a.Grounded && b.Grounded)) continue;
                carried++;
                Vector2 step = b.CarrierVelocity * frameDt;
                catJudder = Mathf.Max(catJudder, (b.Paw - a.Paw - step).magnitude * pp);
                floorJudder = Mathf.Max(floorJudder, (b.FloorDrawn - a.FloorDrawn - step).magnitude * pp);
                jitter = Mathf.Max(jitter, ((b.Paw - b.FloorDrawn) - (a.Paw - a.FloorDrawn)).magnitude * pp);
            }
            float limit = CaptureThresholds.CarriedJitterPhonePx;
            bool carriedPass = jitter <= limit && catJudder <= limit && floorJudder <= limit;
            pass &= carriedPass;
            j.Key("carried").Open().Num("threshold_phone_px", limit).Num("frame_pairs", carried).Num("max_jitter_phone_px", jitter)
                .Num("max_cat_judder_phone_px", catJudder).Num("max_floor_judder_phone_px", floorJudder).Bool("pass", carriedPass).Close();
            if (carried > 0) line.Append($" | carried: against the floor {jitter:F2}pp, cat judder {catJudder:F2}pp, floor judder {floorJudder:F2}pp over {carried} frames");

            // --- state vs motor ---
            var broken = StateRules(rows, speeds);
            bool rulesPass = broken.All(b => b.Value.Count == 0);
            pass &= rulesPass;
            j.Key("state_vs_motor").Open().Raw("rules", "[" + string.Join(",", Rules.Select(rule =>
                $"{{\"name\":\"{rule.Name}\",\"description\":\"{rule.Description}\",\"frames\":{broken[rule.Name].Count},\"first_frames\":[{string.Join(",", broken[rule.Name].Take(20))}]}}")) + "]")
                .Bool("pass", rulesPass).Close();
            line.Append($" | rules {broken.Sum(b => b.Value.Count)} frames broken");

            // --- hold timing (item 6): in every death hold, Death shows and reaches its clip's last (held) frame by the hold's
            // last frame (the hold is RoomSafetyConfig.HoldTicks: 0.6 s at 50 Hz) ---
            var holds = new List<(int first, int last, int reached)>();
            for (int i = 0; i < rows.Count; i++)
            {
                if (!rows[i].Holding || (i > 0 && rows[i - 1].Holding)) continue;
                int k = i; while (k + 1 < rows.Count && rows[k + 1].Holding) k++;
                int reached = -1;
                for (int f = i; f <= k && reached < 0; f++)
                    if (rows[f].State == CatAnimState.Death && rows[f].ClipFrames > 0 && rows[f].ClipFrame == rows[f].ClipFrames - 1) reached = rows[f].Frame;
                holds.Add((rows[i].Frame, rows[k].Frame, reached));
            }
            bool holdPass = holds.All(h => h.reached >= 0);
            pass &= holdPass;
            j.Key("hold_timing").Open().Num("holds", holds.Count)
                .Raw("spans", "[" + string.Join(",", holds.Select(h => $"{{\"first\":{h.first},\"last\":{h.last},\"held_frame_at\":{h.reached}}}")) + "]")
                .Bool("pass", holdPass).Close();
            if (holds.Count > 0) line.Append(holdPass ? $" | holds {holds.Count} ok" : $" | HOLD NOT REACHED ({holds.Count(h => h.reached < 0)} of {holds.Count})");

            // --- door (item 7): a completed level shows Door from completion and ends on its last (held) frame ---
            if (completed)
            {
                FrameRow first = rows.FirstOrDefault(r => r.State == CatAnimState.Door), last = rows.Count > 0 ? rows[^1] : null;
                bool doorPass = first != null && last != null && last.State == CatAnimState.Door && last.ClipFrames > 0 && last.ClipFrame == last.ClipFrames - 1
                                && rows.SkipWhile(r => r != first).All(r => r.State == CatAnimState.Door);
                pass &= doorPass;
                j.Key("door").Open().Num("first_frame", first?.Frame ?? -1).Str("last_clip", last?.Clip ?? "").Num("last_clip_frame", last?.ClipFrame ?? -1)
                    .Bool("pass", doorPass).Close();
                line.Append(doorPass ? " | door ok" : " | DOOR MISSING OR NOT HELD");
            }

            // --- the harness itself: stepping frames between ticks must not change physics ---
            bool parityPass = parity == null;
            pass &= parityPass;
            j.Key("capture_parity").Open().Str("note", "ticks with 60 fps frames between them vs ticks alone: body position and velocity identical").Str("first_difference", parity ?? "none").Bool("pass", parityPass).Close();
            if (!parityPass) line.Append(" | PARITY BROKEN");
            if (died != expectDeath) { pass = false; line.Append(died ? " | UNEXPECTED DEATH" : " | EXPECTED DEATH MISSING"); }

            j.Bool("pass", pass).Close();
            line.Append(pass ? " | PASS" : " | FAIL");
            return new Result { Json = j.ToString(), Line = line.ToString(), Pass = pass };
        }

        static FrameRow Prev(List<FrameRow> rows, FrameRow r) => r.Frame > 0 && r.Frame - 1 < rows.Count ? rows[r.Frame - 1] : null;

        static float Max(IEnumerable<FrameRow> rows, Func<FrameRow, float> f) { float m = 0f; bool any = false; foreach (FrameRow r in rows) { m = any ? Mathf.Max(m, f(r)) : f(r); any = true; } return m; }

        static Dictionary<string, List<int>> StateRules(List<FrameRow> rows, CaptureSpeeds speeds)
        {
            var broken = Rules.ToDictionary(r => r.Name, _ => new List<int>());
            var c = new StateContext { Speeds = speeds };
            for (int i = 0; i < rows.Count; i++)
            {
                FrameRow r = rows[i];
                c.Prev = i > 0 ? rows[i - 1] : null;
                c.Next = i + 1 < rows.Count ? rows[i + 1] : null;
                c.Row = r;
                // A death hold (frozen) or a respawn (a teleport) starts the counts again: the motor's flags are stale there.
                bool teleport = c.Prev != null && (r.Root - c.Prev.Root).magnitude >= 1f;
                c.PrevPrevJumped = i > 1 && rows[i - 2].Jumped;
                if (c.Excluded || teleport) { c.GroundedFrames = c.AirborneFrames = c.LastAirRun = 0; if (c.Excluded) continue; }
                // Item 2: the drop below where the cat last stood, and Apex outside the band (the drawn speed along gravity).
                float height = r.GravityDown ? r.Root.y : -r.Root.y;
                if ((r.Grounded && !r.Sunk) || r.Climbing || teleport || c.Prev == null || c.Prev.GravityDown != r.GravityDown) c.GroundHeight = height;
                c.DropSinceGround = c.GroundHeight - height;
                float drawnGravity = c.Prev != null && c.Prev.GravityDown == r.GravityDown ? -(height - (r.GravityDown ? c.Prev.Root.y : -c.Prev.Root.y)) * CatCapture.FrameRate : r.VGravity;
                c.ApexOutsideFrames = speeds != null && r.State == CatAnimState.Apex && Mathf.Abs(drawnGravity) > speeds.AirThreshold ? c.ApexOutsideFrames + 1 : 0;
                if (r.Grounded && !r.Sunk)
                {
                    if (c.GroundedFrames == 0) c.LastAirRun = c.AirborneFrames;
                    c.GroundedFrames++; c.AirborneFrames = 0;
                }
                else { c.AirborneFrames++; c.GroundedFrames = 0; }
                // Run and Walk are judged against the speed drawn on screen (the interpolated root, what the presenter and
                // the viewer see; it trails the body by up to a tick), the facing against the body's.
                float drawn = c.Prev != null ? Mathf.Abs(r.Root.x - c.Prev.Root.x) * CatCapture.FrameRate : Mathf.Abs(r.VAlong);
                float speed = Mathf.Abs(r.VAlong);
                bool ground = r.Grounded && !r.Climbing;
                c.RunSlowFrames = speeds != null && ground && r.State == CatAnimState.Run && drawn < speeds.RunExit ? c.RunSlowFrames + 1 : 0;
                c.WalkFastFrames = speeds != null && ground && r.State == CatAnimState.Walk && drawn >= speeds.RunEnter ? c.WalkFastFrames + 1 : 0;
                c.AgainstFacingFrames = speeds != null && ground && speed > speeds.WalkEnter && (r.VAlong > 0f ? 1 : -1) != r.Facing ? c.AgainstFacingFrames + 1 : 0;
                c.CarriedWalkFrames = ground && r.Carried && speed < 1e-3f && (r.State == CatAnimState.Walk || r.State == CatAnimState.Run) ? c.CarriedWalkFrames + 1 : 0;
                // Item 3: on the vine, judged on the drawn speed along it.
                bool vineState = r.State == CatAnimState.Climb || r.State == CatAnimState.Hang;
                bool moving = speeds != null && Mathf.Abs(drawnGravity) > speeds.ClimbStill;
                c.OffVineStateFrames = r.Climbing && !vineState ? c.OffVineStateFrames + 1 : 0;
                c.HangMovingFrames = r.Climbing && r.State == CatAnimState.Hang && moving ? c.HangMovingFrames + 1 : 0;
                c.ClimbStillFrames = speeds != null && r.Climbing && r.State == CatAnimState.Climb && !moving ? c.ClimbStillFrames + 1 : 0;
                foreach (StateRule rule in Rules) if (rule.Broken(c)) broken[rule.Name].Add(r.Frame);
            }
            return broken;
        }

        // Planted paws: at each sprite change during ground locomotion, the contact pixels' clusters are matched to the
        // paws planted at the previous sample (CatCaptureMath.MatchPlanted: the same ground pixels, overlapping); a paw's
        // drift is how far it moved (world, along the surface) from its first sample to its last. Any frame off the ground
        // (or not moving) ends every plant. `sprites` names the first and last sprite of the plant.
        static List<(int plant, int lift, float drift, string sprites, float speed)> FootSlide(List<FrameRow> rows)
        {
            var done = new List<(int, int, float, string, float)>();
            var active = new List<(float plantAlong, Vector3 last, int plantFrame, int lastFrame, int samples, float drift, string plantSprite, string lastSprite)>();
            float MaxSpeed(int from, int to) { float m = 0f; foreach (FrameRow row in rows) if (row.Frame >= from && row.Frame <= to) m = Mathf.Max(m, Mathf.Abs(row.VAlong)); return m; }
            void End((float plantAlong, Vector3 last, int plantFrame, int lastFrame, int samples, float drift, string plantSprite, string lastSprite) p)
            {
                if (p.samples >= 2) done.Add((p.plantFrame, p.lastFrame, p.drift * CaptureThresholds.PhonePixelsPerUnit, p.plantSprite + ".." + p.lastSprite, MaxSpeed(p.plantFrame, p.lastFrame)));
            }
            for (int i = 0; i < rows.Count; i++)
            {
                FrameRow r = rows[i];
                bool loco = r.Grounded && !r.Holding && !r.Frozen && !r.Climbing && r.M.SurfaceFound
                    && (r.State == CatAnimState.Walk || r.State == CatAnimState.Run || Mathf.Abs(r.VAlong) > 0.1f);
                // A turn mirrors the legs (ruled 2026-09-30: the flip on the most symmetrical frame): no paw stays planted across
                // a facing change, so plants end there (the flip itself is judged by the pose-pop check).
                bool flipped = i > 0 && rows[i - 1].Facing != r.Facing;
                if (!loco || flipped) { foreach (var p in active) End(p); active.Clear(); if (!loco) continue; }
                bool change = i == 0 || rows[i - 1].Sprite != r.Sprite || active.Count == 0;
                if (!change) continue;
                Vector3[] clusters = CatCaptureMath.ClusterRanges(r.M.ContactAlong, CaptureThresholds.ClusterGapSpritePx / r.M.Ppu);
                int[] match = CatCaptureMath.MatchPlanted(active.Select(p => p.last).ToArray(), clusters);
                var next = new List<(float, Vector3, int, int, int, float, string, string)>();
                var used = new bool[clusters.Length];
                for (int a = 0; a < active.Count; a++)
                {
                    var p = active[a];
                    if (match[a] < 0) { End(p); continue; }
                    used[match[a]] = true;
                    Vector3 c = clusters[match[a]];
                    next.Add((p.plantAlong, c, p.plantFrame, r.Frame, p.samples + 1, Mathf.Max(p.drift, Mathf.Abs(c.z - p.plantAlong)), p.plantSprite, r.Sprite));
                }
                for (int k = 0; k < clusters.Length; k++) if (!used[k]) next.Add((clusters[k].z, clusters[k], r.Frame, r.Frame, 1, 0f, r.Sprite, r.Sprite));
                active = next;
            }
            foreach (var p in active) End(p);
            return done;
        }

        public static string F(float v) => float.IsInfinity(v) || float.IsNaN(v) ? "null" : v.ToString("0.###", CultureInfo.InvariantCulture);

        /// <summary>A tiny JSON writer (objects only; arrays come in as raw strings).</summary>
        public sealed class Json
        {
            readonly StringBuilder sb = new();
            bool first = true;
            public Json Open() { sb.Append('{'); first = true; return this; }
            public Json Close() { sb.Append('}'); first = false; return this; }
            public Json Key(string k) { if (!first) sb.Append(','); sb.Append('"').Append(k).Append("\":"); first = true; return this; }
            Json Put(string k, string v) { if (!first) sb.Append(','); sb.Append('"').Append(k).Append("\":").Append(v); first = false; return this; }
            public Json Str(string k, string v) => Put(k, "\"" + (v ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"");
            public Json Num(string k, float v) => Put(k, F(v));
            public Json Num(string k, int v) => Put(k, v.ToString(CultureInfo.InvariantCulture));
            public Json Bool(string k, bool v) => Put(k, v ? "true" : "false");
            public Json Ints(string k, IEnumerable<int> v) => Put(k, "[" + string.Join(",", v) + "]");
            public Json Raw(string k, string raw) => Put(k, raw);
            public override string ToString() => sb.ToString();
        }
    }
}
