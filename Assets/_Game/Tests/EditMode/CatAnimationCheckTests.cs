using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Parallax.Tests.EditMode
{
    // PAX-V07: the capture harness's checks as ordinary tests (ruled 2026-09-30). Each scenario runs once through the real
    // game code (CatCapture.CheckJson: the rig, 50 Hz ticks, 60 fps presenter frames at the interpolated pose, no images) and
    // every check is asserted on its checks.json:
    // - no paws in the floor after takeoff or on landing (paws_in_floor: every frame near a surface, drawn pixels 2 sprite px
    //   deep at most);
    // - no foot slide on planted frames (foot_slide: 3 phone px plus one 60 fps frame of travel per plant);
    // - the presenter's state matches the motor's on every frame (state_vs_motor: every rule);
    // - every death hold reaches the death clip's held frame within the hold (hold_timing).
    // Thresholds and rules: Docs/V_TASKS/PAX-V07_gauntlet/HARNESS.md.
    public sealed class CatAnimationCheckTests
    {
        static readonly string[] Ground =
        {
            "ground_idle_down", "ground_idle_up", "ground_slow_down", "ground_slow_up", "ground_ramp_down", "ground_ramp_up",
            "ground_digital_down", "ground_digital_up", "ground_turns_down", "ground_turns_up", "ground_wall_down", "ground_wall_up",
            "ground_fidget_down", "ground_fidget_up",
        };

        static readonly string[] Air =
        {
            "air_standing_jump_down", "air_standing_jump_up", "air_running_jump_down", "air_running_jump_up",
            "air_ledge_down", "air_ledge_up", "air_tower_drop_down", "air_tower_drop_up", "air_tower_run_down", "air_tower_run_up",
            "air_tower_jump_down", "air_tower_jump_up", "air_low_ceiling_down", "air_low_ceiling_up", "air_geyser_down", "air_geyser_up",
        };

        static readonly string[] Climb =
        {
            "climb_ground_down", "climb_ground_up", "climb_side_right_down", "climb_side_right_up", "climb_side_left_down", "climb_side_left_up",
            "climb_jump_leap_down", "climb_jump_leap_up", "climb_release_down", "climb_release_up",
        };

        static readonly string[] Flip = { "flip_stand_down", "flip_walk_down", "flip_jump_down" };

        // PAX-A14: a cat standing still on a moving floor (Trap Lab room 12's Mover).
        static readonly string[] Carry = { "carry_stand_down" };

        // The rooms the developer plays by hand (Trap Lab 0-3 and L001), both gravities where the capture has them.
        static readonly string[] Rooms = { "traplab0_walk_down", "traplab3_walk_up", "L001_solution_down", "traplab2_pit_down" };

        // Scenarios with a death hold: one per death kind (the pit, spikes, the crusher, an arrow, the storm cloud).
        static readonly string[] Deaths = { "traplab2_pit_down", "death_spiked_down", "death_crushed_down", "death_arrow_down", "death_zapped_down" };

        static IEnumerable<string> All => Ground.Concat(Air).Concat(Climb).Concat(Flip).Concat(Carry).Concat(Rooms).Concat(Deaths.Skip(1));
        static IEnumerable<string> Moving => All.Where(n => n != "ground_idle_down" && n != "ground_idle_up");

        [Serializable] sealed class Paws { public int frames_checked; public int frames_over; public float max_depth_sprite_px; public int[] first_frames_over; }
        [Serializable] sealed class Slide { public int planted_paws; public int paws_over; public float max_phone_px; }
        [Serializable] sealed class Rule { public string name; public int frames; public int[] first_frames; }
        [Serializable] sealed class Rules { public Rule[] rules; }
        [Serializable] sealed class Span { public int first; public int last; public int held_frame_at; }
        [Serializable] sealed class Hold { public int holds; public Span[] spans; public bool pass; }
        [Serializable] sealed class Door { public int first_frame = -1; public string last_clip; public int last_clip_frame; public bool pass; }
        [Serializable] sealed class Carried { public int frame_pairs; public float max_jitter_phone_px, max_cat_judder_phone_px, max_floor_judder_phone_px; public bool pass; }
        [Serializable] sealed class Checks { public string scenario; public bool completed; public Paws paws_in_floor; public Slide foot_slide; public Rules state_vs_motor; public Hold hold_timing; public Door door; public Carried carried; }

        static Dictionary<string, Checks> results;

        [OneTimeSetUp]
        public void RunEveryScenarioOnce()
        {
            Type capture = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Parallax.Editor.Art.CatCapture")).FirstOrDefault(t => t != null);
            Assert.NotNull(capture, "Parallax.Editor.Art.CatCapture not found");
            MethodInfo check = capture.GetMethod("CheckJson", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(check, "CatCapture.CheckJson not found");
            string[] names = All.ToArray();
            var json = (string[])check.Invoke(null, new object[] { names });
            results = new Dictionary<string, Checks>();
            for (int i = 0; i < names.Length; i++) results[names[i]] = JsonUtility.FromJson<Checks>(json[i]);
        }

        static Checks Of(string scenario)
        {
            Assert.IsTrue(results != null && results.TryGetValue(scenario, out Checks c), scenario + " didn't run");
            return results[scenario];
        }

        [TestCaseSource(nameof(All))]
        public void NoPawsInTheFloor_AfterTakeoffOrOnLanding(string scenario)
        {
            Paws p = Of(scenario).paws_in_floor;
            Assert.Greater(p.frames_checked, 0, "no frame near a surface was checked");
            Assert.AreEqual(0, p.frames_over, $"frames with drawn pixels in the floor (deepest {p.max_depth_sprite_px:F1} sprite px): {string.Join(", ", p.first_frames_over ?? new int[0])}");
        }

        [TestCaseSource(nameof(Moving))]
        public void NoFootSlide_OnPlantedFrames(string scenario)
        {
            Slide s = Of(scenario).foot_slide;
            Assert.AreEqual(0, s.paws_over, $"plants sliding past the allowance (worst {s.max_phone_px:F1} phone px of {s.planted_paws} plants)");
        }

        [TestCaseSource(nameof(All))]
        public void ThePresentersState_MatchesTheMotors_OnEveryFrame(string scenario)
        {
            Rule[] broken = Of(scenario).state_vs_motor.rules.Where(r => r.frames > 0).ToArray();
            Assert.IsEmpty(broken, string.Join("; ", broken.Select(r => $"{r.name}: {r.frames} frames ({string.Join(",", r.first_frames ?? new int[0])})")));
        }

        // Item 7: completing the level (L001's solution through its last door) shows the celebration and holds its last frame.
        [Test]
        public void LevelComplete_ShowsTheDoorCelebration_AndHoldsItsLastFrame()
        {
            Checks c = Of("L001_solution_down");
            Assert.IsTrue(c.completed, "L001's solution didn't complete");
            Assert.NotNull(c.door, "no door check");
            Assert.IsTrue(c.door.pass, $"Door from frame {c.door.first_frame}, last clip {c.door.last_clip}[{c.door.last_clip_frame}]");
        }

        // PAX-A14 (L017's Slide_4 report): a cat standing on a moving floor rides it smoothly: at the floor's steady speed the drawn
        // cat and the drawn floor each move speed x 1/60 s every frame (no 50-on-60 judder), and the cat stays put on the floor.
        // The floor's body interpolates, and the presenter draws the cat where interpolation would, though the carry's position
        // write drops it (measured in play mode, 2026-09-30).
        [TestCaseSource(nameof(Carry))]
        public void ACarriedCat_StandsStillOnItsFloor_WithoutShaking(string scenario)
        {
            Carried c = Of(scenario).carried;
            Assert.NotNull(c, "no carried check");
            Assert.Greater(c.frame_pairs, 100, "the cat wasn't carried standing still");
            Assert.LessOrEqual(c.max_cat_judder_phone_px, 0.5f, "the drawn cat's judder at the floor's steady speed (phone px)");
            Assert.LessOrEqual(c.max_floor_judder_phone_px, 0.5f, "the drawn floor's judder at its steady speed (phone px)");
            Assert.LessOrEqual(c.max_jitter_phone_px, 0.5f, "the drawn cat against the drawn floor, frame to frame (phone px)");
        }

        [TestCaseSource(nameof(Deaths))]
        public void EveryDeathHold_ReachesTheHeldFrame_WithinTheHold(string scenario)
        {
            Hold h = Of(scenario).hold_timing;
            Assert.Greater(h.holds, 0, "the scenario has no death hold");
            Span[] late = h.spans.Where(s => s.held_frame_at < 0).ToArray();
            Assert.IsEmpty(late, "holds whose death clip never reached its held frame: " + string.Join(", ", late.Select(s => $"frames {s.first}-{s.last}")));
        }
    }
}
