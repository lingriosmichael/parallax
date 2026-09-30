using System;
using System.Collections.Generic;
using System.Linq;
using Parallax.Core;
using Parallax.Editor.Levels;
using Parallax.Editor.Routes;
using Parallax.Editor.Setup;
using UnityEngine;

namespace Parallax.Editor.Art
{
    /// <summary>One scripted step: returns this tick's screen-relative command, or false when the step is over (the next
    /// step then starts on the same tick). Conditions read the rig after the previous tick.</summary>
    abstract class CaptureStep
    {
        public string Label;
        public bool Cut;   // the contact sheets cut a transition sheet where this step starts
        public bool Setup; // PAX-V07 item 1: getting into place (e.g. the fall to the ceiling); rendered, left out of the checks
        public abstract bool Next(CatCaptureRig rig, int ticksDone, out CatCommand command);
    }

    /// <summary>PAX-V07 gauntlet item 0: a capture scenario. The name ends in its gravity (_down / _up) so every file it
    /// writes says which. Later items append scenarios to <see cref="CatCaptureScenarios.All"/>.</summary>
    sealed class CaptureScenario
    {
        public string Name, Description;
        public Func<RouteSession, SoloRoomDefinition> Room;
        public float? StartX;                 // room-local x of the cat's collider centre, standing; null = the checkpoint
        public bool ExpectDeath;
        public Func<RouteSession, List<CaptureStep>> Steps;
    }

    static partial class CatCaptureScenarios
    {
        // ---------- steps ----------

        sealed class Hold : CaptureStep
        {
            readonly float move; readonly int ticks;
            public Hold(float move, float seconds, string label) { this.move = move; ticks = TickTime.ToWholeTicks(seconds); Label = label; }
            public override bool Next(CatCaptureRig rig, int done, out CatCommand c) { c = new CatCommand { Move = move }; return done < ticks; }
        }

        sealed class Press : CaptureStep
        {
            readonly float move;
            public Press(float move, string label) { this.move = move; Label = label; }
            public override bool Next(CatCaptureRig rig, int done, out CatCommand c) { c = new CatCommand { Move = move, JumpPressed = true }; return done < 1; }
        }

        sealed class Until : CaptureStep
        {
            readonly float move; readonly Func<CatCaptureRig, bool> condition; readonly int maxTicks;
            public Until(float move, Func<CatCaptureRig, bool> condition, float maxSeconds, string label)
            { this.move = move; this.condition = condition; maxTicks = TickTime.ToWholeTicks(maxSeconds); Label = label; }
            public override bool Next(CatCaptureRig rig, int done, out CatCommand c)
            {
                c = new CatCommand { Move = move };
                if (condition(rig)) return false;
                if (done < maxTicks) return true;
                Debug.LogWarning($"CatCapture: step '{Label}' timed out after {maxTicks} ticks at tick {rig.Tick}.");
                return false;
            }
        }

        /// <summary>An analog ramp: the move goes linearly from `from` to `to` over whole ticks (the last tick at `to`).</summary>
        sealed class Ramp : CaptureStep
        {
            readonly float from, to; readonly int ticks;
            public Ramp(float from, float to, float seconds, string label) { this.from = from; this.to = to; ticks = TickTime.ToWholeTicks(seconds); Label = label; }
            public override bool Next(CatCaptureRig rig, int done, out CatCommand c)
            {
                c = new CatCommand { Move = from + (to - from) * Mathf.Min(1f, (done + 1) / (float)ticks) };
                return done < ticks;
            }
        }

        sealed class Act : CaptureStep
        {
            readonly Action<CatCaptureRig> action;
            public Act(Action<CatCaptureRig> action, string label) { this.action = action; Label = label; }
            public override bool Next(CatCaptureRig rig, int done, out CatCommand c) { c = default; action(rig); return false; }
        }

        sealed class Replay : CaptureStep
        {
            readonly CatCommand[] commands;
            public Replay(CatCommand[] commands, string label) { this.commands = commands; Label = label; }
            public override bool Next(CatCaptureRig rig, int done, out CatCommand c) { c = done < commands.Length ? commands[done] : default; return done < commands.Length; }
        }

        static bool Grounded(CatCaptureRig r) => r.Cat.IsGrounded;
        static bool Airborne(CatCaptureRig r) => !r.Cat.IsGrounded;
        static bool Still(CatCaptureRig r) => r.Cat.IsGrounded && r.Body.linearVelocity.sqrMagnitude < 1e-4f;
        static Func<CatCaptureRig, bool> XAtLeast(float x) => r => r.ColliderCentre.x - r.Origin.x >= x;

        // ---------- the walk script (screen-relative, so the same in both gravities) ----------

        static List<CaptureStep> Walk() => new()
        {
            new Hold(0f, 1f, "stand 1 s"),
            new Hold(0.4f, 1.5f, "slow walk right (0.4) 1.5 s"),
            new Hold(1f, 1.5f, "run right 1.5 s") { Cut = true },
            new Until(0f, Still, 1f, "release: stop") { Cut = true },
            new Hold(0f, 0.5f, "stand 0.5 s"),
            new Hold(-1f, 1f, "run left 1 s") { Cut = true },
            new Until(0f, Still, 1f, "release: stop"),
            new Hold(0f, 0.5f, "stand 0.5 s"),
            new Press(0f, "jump in place"),
            new Until(0f, Airborne, 0.2f, "take off"),
            new Until(0f, Grounded, 2f, "until landed"),
            new Hold(0f, 0.5f, "stand 0.5 s"),
            new Hold(-1f, 0.3f, "run left 0.3 s"),
            new Press(-1f, "jump while running left"),
            new Until(-1f, Airborne, 0.2f, "take off"),
            new Until(-1f, Grounded, 2f, "until landed"),
            new Hold(-1f, 0.1f, "run on 0.1 s"),
            new Until(0f, Still, 1f, "release: stop"),
            new Hold(0f, 0.5f, "stand 0.5 s"),
        };

        // ---------- PAX-V07 item 1: the ground scripts (screen-relative; the same in both gravities) ----------

        static List<CaptureStep> IdleHold() => new() { new Hold(0f, 3f, "stand 3 s") };

        // PAX-V07 item 5: the fidget cycle (fidgetDelay 3 s; each fidget 0.67 s): look around at 3 s, ear twitch at 6.7 s, sit
        // down at 10.3 s; seated 1.2 s, then a walk (the stand-up), a stop, look around again and a move during it, then a
        // jump during the ear twitch.
        static List<CaptureStep> Fidgets() => new()
        {
            new Hold(0f, 12f, "stand 12 s: look around, ear twitch, sit down") { Cut = true },
            new Hold(0.4f, 0.4f, "walk: the stand-up from the sit") { Cut = true },
            new Until(0f, Still, 1f, "release: stop"),
            new Hold(0f, 3.3f, "stand 3.3 s: look around starts") { Cut = true },
            new Hold(-0.4f, 0.4f, "walk left during look around") { Cut = true },
            new Until(0f, Still, 1f, "release: stop"),
            new Hold(0f, 3.3f, "stand: look around again"),
            new Hold(0f, 3.8f, "stand: ear twitch starts") { Cut = true },
            new Press(0f, "jump during the ear twitch"),
            new Until(0f, Airborne, 0.2f, "take off"),
            new Until(0f, Grounded, 2f, "until landed"),
            new Hold(0f, 0.5f, "stand"),
        };

        static List<CaptureStep> SlowWalk() => new()
        {
            new Hold(0f, 0.5f, "stand 0.5 s"),
            new Hold(0.3f, 2f, "analog 0.3 right 2 s") { Cut = true },
            new Until(0f, Still, 1f, "release: stop") { Cut = true },
            new Hold(0f, 0.5f, "stand 0.5 s"),
            new Hold(-0.5f, 1.5f, "analog 0.5 left 1.5 s") { Cut = true },
            new Until(0f, Still, 1f, "release: stop") { Cut = true },
            new Hold(0f, 0.5f, "stand 0.5 s"),
        };

        static List<CaptureStep> Ramps() => new()
        {
            new Hold(0f, 0.5f, "stand 0.5 s"),
            new Ramp(0f, 1f, 1.2f, "ramp 0 to 1 right 1.2 s") { Cut = true },
            new Hold(1f, 0.3f, "full right 0.3 s"),
            new Ramp(1f, 0f, 1.2f, "ramp 1 to 0 1.2 s") { Cut = true },
            new Until(0f, Still, 1f, "stop"),
            new Hold(0f, 0.5f, "stand 0.5 s"),
            new Ramp(0f, -1f, 1.2f, "ramp 0 to 1 left 1.2 s") { Cut = true },
            new Hold(-1f, 0.3f, "full left 0.3 s"),
            new Ramp(-1f, 0f, 1.2f, "ramp 1 to 0 left 1.2 s") { Cut = true },
            new Until(0f, Still, 1f, "stop"),
            new Hold(0f, 0.5f, "stand 0.5 s"),
        };

        static List<CaptureStep> Digital() => new()
        {
            new Hold(0f, 0.5f, "stand 0.5 s"),
            new Hold(1f, 0.6f, "digital right 0.6 s") { Cut = true },
            new Until(0f, Still, 1f, "release: stop") { Cut = true },
            new Hold(0f, 0.5f, "stand 0.5 s"),
            new Hold(-1f, 0.6f, "digital left 0.6 s") { Cut = true },
            new Until(0f, Still, 1f, "release: stop") { Cut = true },
            new Hold(0f, 0.5f, "stand 0.5 s"),
        };

        static List<CaptureStep> Turns() => new()
        {
            new Hold(0f, 0.3f, "stand 0.3 s"),
            new Hold(0.3f, 1f, "walk right (0.3) 1 s"),
            new Hold(-0.3f, 1f, "turn at walk speed: walk left (0.3) 1 s") { Cut = true },
            new Hold(1f, 0.5f, "turn: run right 0.5 s") { Cut = true },
            new Hold(-1f, 0.5f, "turn at run speed: run left 0.5 s") { Cut = true },
            new Hold(1f, 0.15f, "double turn: right 0.15 s") { Cut = true },
            new Hold(-1f, 0.15f, "double turn: left 0.15 s"),
            new Hold(1f, 0.4f, "double turn: right again 0.4 s"),
            new Until(0f, Still, 1f, "release: stop"),
            new Hold(0f, 0.5f, "stand 0.5 s"),
        };

        static List<CaptureStep> Wall() => new()
        {
            new Hold(0f, 0.5f, "stand 0.5 s"),
            new Hold(1f, 1.2f, "run right into the wall, keep pushing") { Cut = true },
            new Until(0f, Still, 1f, "release") { Cut = true },
            new Hold(0f, 0.3f, "stand 0.3 s"),
            new Hold(-0.3f, 0.6f, "walk away (0.3) 0.6 s") { Cut = true },
            new Hold(0.3f, 1.5f, "walk back into the wall (0.3), keep pushing") { Cut = true },
            new Until(0f, Still, 1f, "release") { Cut = true },
            new Hold(0f, 0.5f, "stand 0.5 s"),
        };

        // PAX-V07 §6: inputs pressed during Turn and the other ground states (Idle, Walk, Run). Not a capture for the critic:
        // CatVisualOnlyParityTests replays it with the presenter stepped and not.
        static List<CaptureStep> ParityGround() => new()
        {
            new Hold(0f, 0.3f, "stand"),
            new Press(0f, "jump from Idle"),
            new Until(0f, Airborne, 0.2f, "take off"),
            new Until(0f, Grounded, 2f, "until landed"),
            new Hold(0.3f, 0.6f, "walk right"),
            new Hold(-0.6f, 0.04f, "reverse: Turn"),
            new Press(-0.6f, "jump during Turn"),
            new Until(-0.6f, Airborne, 0.2f, "take off"),
            new Until(-0.6f, Grounded, 2f, "until landed"),
            new Hold(-1f, 0.4f, "run left"),
            new Hold(-0.3f, 0.3f, "walk left"),
            new Hold(0.3f, 0.03f, "reverse at walk speed: Turn"),
            new Press(0.3f, "jump during Turn"),
            new Until(0.3f, Airborne, 0.2f, "take off"),
            new Until(0.3f, Grounded, 2f, "until landed"),
            new Hold(-1f, 0.4f, "run left"),
            new Hold(1f, 0.04f, "reverse: Turn"),
            new Hold(-1f, 0.04f, "reverse again during Turn"),
            new Hold(1f, 0.02f, "and again"),
            new Hold(1f, 0.3f, "run right"),
            new Press(1f, "jump while running"),
            new Until(1f, Airborne, 0.2f, "take off"),
            new Until(1f, Grounded, 2f, "until landed"),
            new Hold(0.5f, 0.3f, "slower"),
            new Hold(0f, 0.04f, "release"),
            new Press(0f, "jump while stopping"),
            new Until(0f, Airborne, 0.2f, "take off"),
            new Until(0f, Grounded, 2f, "until landed"),
            new Hold(0f, 0.3f, "stand"),
        };

        /// <summary>Gravity up: flip at the start, fall to the ceiling, stand, then `steps` (screen-relative).</summary>
        static List<CaptureStep> Up(List<CaptureStep> steps)
        {
            var list = new List<CaptureStep>
            {
                new Hold(0f, 0.1f, "stand 0.1 s") { Setup = true },   // the presenter sees gravity down first, so the flip rolls
                new Act(r => r.Gravity.Flip(), "flip gravity") { Setup = true },
                new Until(0f, Airborne, 0.5f, "take off") { Setup = true },
                new Until(0f, Grounded, 3f, "fall to the ceiling") { Setup = true },
                new Hold(0f, 0.5f, "settle 0.5 s") { Setup = true },
            };
            list.AddRange(steps);
            return list;
        }

        // Trap Lab room 0's floor is clear from x 5.75 to 20.9 (the cat's collider centre), room 3's ceiling from 0.5 to
        // 14.5 (the Backboard at 15.0); room 1's FixedPillar (x 21.5) is a wall on a clear floor from x 17.5.
        static IEnumerable<CaptureScenario> Ground(string name, string what, Func<List<CaptureStep>> script, float downX, float upX,
            Func<RouteSession, SoloRoomDefinition> downRoom = null, string downRoomName = "Trap Lab room 0")
        {
            yield return new CaptureScenario
            {
                Name = name + "_down", Description = $"{downRoomName}, gravity down: {what}",
                Room = downRoom ?? (_ => TrapLabLayout.Rooms[0]), StartX = downX, Steps = _ => script(),
            };
            yield return new CaptureScenario
            {
                Name = name + "_up", Description = $"Trap Lab room 3, gravity up (walking on the ceiling): {what}",
                Room = _ => TrapLabLayout.Rooms[3], StartX = upX, Steps = _ => Up(script()),
            };
        }

        static IEnumerable<CaptureScenario> GroundScenarios() =>
            Ground("ground_idle", "standing still 3 s", IdleHold, 10f, 5f)
            .Concat(Ground("ground_fidget", "the idle fidgets: look around, ear twitch, sit down, a stand-up, input during a fidget", Fidgets, 10f, 5f))
            .Concat(Ground("ground_slow", "analog 0.3 right, stop, analog 0.5 left, stop", SlowWalk, 8f, 5f))
            .Concat(Ground("ground_ramp", "analog ramps 0 to 1 and back, right then left (walk, run, walk, idle)", Ramps, 7f, 3f))
            .Concat(Ground("ground_digital", "digital start and stop, right then left", Digital, 9f, 5f))
            .Concat(Ground("ground_turns", "turns at walk speed and at run speed, then a quick double turn", Turns, 9f, 5f))
            .Concat(Ground("ground_wall", "run into a wall and keep pushing, release, walk away, walk back into it", Wall, 18.5f, 12f,
                _ => TrapLabLayout.Rooms[1], "Trap Lab room 1 (FixedPillar)"))
            .Concat(Ground("parity_ground", "parity script: jumps and reversals pressed during Turn, Walk, Run and Idle", ParityGround, 12f, 7f));

        // ---------- the scenarios ----------

        public static readonly List<CaptureScenario> All = new List<CaptureScenario>
        {
            // Trap Lab room 0's floor is clear from x 5.3 (SourceSpikes' trigger) to the door (21.7): the cat starts at x 7,
            // so the script's ~12.8 u right and back never fires a trap.
            new CaptureScenario
            {
                Name = "traplab0_walk_down", Description = "Trap Lab room 0, gravity down: stand, slow walk, run, stop, run left, stop, jump in place, jump while running",
                Room = _ => TrapLabLayout.Rooms[0], StartX = 7f, Steps = _ => Walk(),
            },
            // Room 0's ceiling has the three falling blocks hanging 0.5 below it (they'd stop a cat walking on it); rooms 1 and 2
            // have a ceiling hazard and a block. Room 3's ceiling is clear from x 0 to the Backboard (15.0): the cat starts
            // at x 1.5, flips, and walks on the ceiling's underside.
            new CaptureScenario
            {
                Name = "traplab3_walk_up", Description = "Trap Lab room 3, gravity flipped at the start: the fall to the ceiling, then the same inputs as traplab0_walk_down (screen-relative)",
                Room = _ => TrapLabLayout.Rooms[3], StartX = 1.5f,
                Steps = _ =>
                {
                    var steps = new List<CaptureStep>
                    {
                        new Act(r => r.Gravity.Flip(), "flip gravity"),
                        new Until(0f, Airborne, 0.5f, "take off"),
                        new Until(0f, Grounded, 3f, "fall to the ceiling"),
                    };
                    steps.AddRange(Walk());
                    return steps;
                },
            },
            new CaptureScenario
            {
                Name = "L001_solution_down", Description = "L001's solution route (L001Routes), replayed tick for tick from the route harness's commands",
                Room = _ => LevelLayouts.ById["L001"],
                Steps = session => new List<CaptureStep>
                {
                    new Replay(RouteCommands(session, "L001"), "L001 solution"),
                    new Hold(0f, 1.2f, "after the door: the celebration plays and holds") { Cut = true },
                },
            },
            // Room 2: the cat starts at x 19 (past RearmBlock's trigger), walks slowly onto RearmCollapse over the pit and
            // stops; the floor goes, the cat falls into the pit and dies on its hazard, the hold runs, it respawns.
            new CaptureScenario
            {
                Name = "traplab2_pit_down", Description = "Trap Lab room 2: walk onto the collapsing floor, fall into the pit, the death hold, the respawn",
                Room = _ => TrapLabLayout.Rooms[2], StartX = 19f, ExpectDeath = true,
                Steps = _ => new List<CaptureStep>
                {
                    new Hold(0f, 0.5f, "stand 0.5 s"),
                    new Until(0.4f, XAtLeast(23.6f), 3f, "slow walk onto RearmCollapse"),
                    new Until(0f, r => r.Respawns > 0, 4f, "stand: collapse, fall, death hold, respawn"),
                    new Hold(0f, 1f, "after the respawn"),
                },
            },
        }.Concat(GroundScenarios()).Concat(AirScenarios()).Concat(ClimbScenarios()).Concat(FlipScenarios()).Concat(DeathScenarios()).ToList();

        // PAX-V07 item 6: a death of each kind through a real trap (the pit is traplab2_pit_down), the hold, the respawn, and a
        // move pressed right after the respawn (it ends the Respawn clip). Gravity down: the kill sources sit on the floor.
        static IEnumerable<CaptureScenario> DeathScenarios()
        {
            List<CaptureStep> DieThenMove(string approach, float move, Func<CatCaptureRig, bool> until = null) => new()
            {
                new Hold(0f, 0.3f, "stand"),
                new Until(move, until ?? (r => r.Death.IsHolding), 6f, approach) { Cut = true },
                new Until(0f, r => r.Respawns > 0, 3f, "the death hold, the respawn") { Cut = true },
                new Hold(0.6f, 0.4f, "walk right at once after the respawn") { Cut = true },
                new Until(0f, Still, 1f, "release: stop"),
                new Hold(0f, 0.5f, "stand"),
            };
            // Room 0: walking right into SourceSpikes' trigger (x 4.75-5.25) reveals the spikes under the cat (x 5-7).
            yield return new CaptureScenario { Name = "death_spiked_down", Description = "Trap Lab room 0: walk into the hidden spikes (Spiked)", Room = _ => TrapLabLayout.Rooms[0], StartX = 3f, ExpectDeath = true, Steps = _ => DieThenMove("walk into the spikes", 0.4f) };
            // Room 1: standing between FixedPillar (right edge x 22.5) and the Crusher's trigger (x 21.8-24.8): it slides 3 u left.
            yield return new CaptureScenario { Name = "death_crushed_down", Description = "Trap Lab room 1: the crusher pins the cat against the pillar (Crushed)", Room = _ => TrapLabLayout.Rooms[1], StartX = 23.4f, ExpectDeath = true, Steps = _ => DieThenMove("stand in the crusher's path", 0f) };
            // Room 3: past StopA, walking right into ArrowB's trigger (x 17.25): the disguised arrow fires left along the floor.
            yield return new CaptureScenario { Name = "death_arrow_down", Description = "Trap Lab room 3: walk into the disguised arrow's trigger (Arrow)", Room = _ => TrapLabLayout.Rooms[3], StartX = 15.6f, ExpectDeath = true, Steps = _ => DieThenMove("walk into the arrow's lane", 0.4f) };
            // Room 10: the storm cloud's betrayal route (wake it, stand still), replayed tick for tick.
            yield return new CaptureScenario
            {
                Name = "death_zapped_down", Description = "Trap Lab room 10: stand still after waking the storm cloud (Zapped)", Room = _ => TrapLabLayout.Rooms[10], ExpectDeath = true,
                Steps = session => new List<CaptureStep>
                {
                    new Replay(RouteCommands(session, TrapLabLayout.Rooms[10], TrapLabRoutes.Room10().Betrayals[0].Route), "stand still after waking the cloud") { Cut = true },
                    new Until(0f, r => r.Respawns > 0, 3f, "the death hold, the respawn") { Cut = true },
                    new Hold(0.6f, 0.4f, "walk right at once after the respawn") { Cut = true },
                    new Until(0f, Still, 1f, "release: stop"),
                    new Hold(0f, 0.5f, "stand"),
                },
            };
        }

        // PAX-V07 item 4: gravity flips in Trap Lab room 3 (floor clear from x 0.5 to ShieldA at 7.0, ceiling to 14.5; the
        // cat starts at x 3). Each flips down→up and back up→down, so both directions of the roll are captured.
        static IEnumerable<CaptureScenario> FlipScenarios()
        {
            CaptureScenario Flip(string name, string what, Func<List<CaptureStep>> steps) => new CaptureScenario
            {
                Name = name + "_down", Description = $"Trap Lab room 3, starting with gravity down: {what}", Room = _ => TrapLabLayout.Rooms[3], StartX = 3f, Steps = _ => steps(),
            };
            CaptureStep FlipNow(string label) => new Act(r => r.Gravity.Flip(), label) { Cut = true };
            yield return Flip("flip_stand", "standing: flip up, fall to the ceiling, stand, flip down, fall to the floor, stand", () => new List<CaptureStep>
            {
                new Hold(0f, 0.5f, "stand 0.5 s"), FlipNow("flip up"), new Until(0f, Airborne, 0.5f, "leave the floor"),
                new Until(0f, Grounded, 3f, "fall to the ceiling"), new Hold(0f, 0.8f, "stand on the ceiling"),
                FlipNow("flip down"), new Until(0f, Airborne, 0.5f, "leave the ceiling"), new Until(0f, Grounded, 3f, "fall to the floor"),
                new Hold(0f, 0.8f, "stand"),
            });
            yield return Flip("flip_walk", "walking right: flip up mid-walk, keep walking on the ceiling, flip down mid-walk", () => new List<CaptureStep>
            {
                new Hold(0f, 0.3f, "stand"), new Hold(0.4f, 0.5f, "walk right"), FlipNow("flip up while walking"),
                new Until(0.4f, Grounded, 3f, "walking: fall to the ceiling"), new Hold(0.4f, 0.4f, "walk on the ceiling"),
                FlipNow("flip down while walking"), new Until(0.4f, Grounded, 3f, "walking: fall to the floor"), new Hold(0.4f, 0.3f, "walk"),
                new Until(0f, Still, 1f, "release: stop"), new Hold(0f, 0.3f, "stand"),
            });
            yield return Flip("flip_jump", "a jump in place, flipped at the apex: up to the ceiling", () => new List<CaptureStep>
            {
                new Hold(0f, 0.3f, "stand"), new Press(0f, "jump"), new Until(0f, Airborne, 0.2f, "take off"),
                new Until(0f, r => r.Body.linearVelocity.y <= 0f, 1f, "rise to the apex"), FlipNow("flip at the apex"),
                new Until(0f, Grounded, 3f, "fall to the ceiling"), new Hold(0f, 0.8f, "stand on the ceiling"),
            });
        }

        /// <summary>A level's solution as the route harness plays it: one screen-relative command per tick, read from the
        /// replay's own records (Move, Jump, Climb), so this rig plays the same inputs on the same ticks.</summary>
        static CatCommand[] RouteCommands(RouteSession session, string level)
        {
            ReplayResult result = RouteHarness.Replay(session, LevelLayouts.ById[level], LevelRoutes.ById[level].Solution, new ReplayOptions { ResolveCause = false });
            if (!result.Completed) Debug.LogWarning($"CatCapture: {level}'s solution didn't complete in the route harness ({result.Failure ?? "died"}).");
            return Commands(result);
        }

        // Item 6: any route in any room, up to its end (a betrayal's kill tick).
        static CatCommand[] RouteCommands(RouteSession session, SoloRoomDefinition room, Route route) =>
            Commands(RouteHarness.Replay(session, room, route, new ReplayOptions { ResolveCause = false }));

        static CatCommand[] Commands(ReplayResult result)
        {
            var commands = new CatCommand[result.Records.Count - 1];
            for (int i = 1; i < result.Records.Count; i++)
            {
                TickRecord r = result.Records[i];
                commands[i - 1] = new CatCommand { Move = r.Move, JumpPressed = r.JumpPressed, Climb = r.Climb };
            }
            return commands;
        }
    }
}
