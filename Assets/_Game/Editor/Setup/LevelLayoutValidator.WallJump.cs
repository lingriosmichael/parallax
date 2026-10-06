using System.Collections.Generic;
using System.Linq;
using Parallax.Gameplay.Player;
using UnityEngine;

namespace Parallax.Editor.Setup
{
    // PAX-105 (D-110 (7), amendment 2): wall jumps must not skip a level's trolls. Only grip walls can be grabbed, so only
    // their faces count: a level with none passes by construction. The reach search (TriggerCoverage: surfaces, vines,
    // geysers, movers, and grip faces clung to and jumped off, WallJumpReach's arc) is run with and without wall jumps, and
    // only what wall jumps add is reported, by kind (and (g), every grip wall's own rules, ValidateGripWall):
    // (a) the door, reached without passing through an overlap trap's trigger;
    // (b) a side of an overlap trap's trigger reached by a wall jump, with a danger before the trigger's near edge from there
    //     (D-074's cut, with wall approaches; ValidateTriggerCoverage reports the same as an error);
    // (c) a BaitGap's target reached from its take-off (D-083; ValidateBaitGaps);
    // (d) a trap floor that gives way on a touch, its underside within reach from below (D-085; ValidateTrapFloorHeadroom);
    // (e) a checkpoint section's checkpoint reached without passing through its gate (D-091).
    // Gravity up is never checked. D-118 lets a cat cling with gravity up; its wall jumps are not modelled here (only L004 has a
    // place for them, and its escape route proves the climb through the real game code).
    public static partial class LevelLayoutValidator
    {
        // (a)'s named exemptions ("level/trap" -> reason), each approved by the developer by name.
        public static readonly IReadOnlyDictionary<string, string> WallJumpDoorExemptions = new Dictionary<string, string>
        {
            // D-111 (approved by the developer, 2026-10-04): L001's shelf, the way over Spikes_1, is reached only by the climb
            // out between the falling walls; that climb is the level's route, not a shortcut.
            ["L001/Spikes_1"] = "D-111: the climb between the walls is the route onto the shelf over the spikes",
        };

        public static List<string> ValidateWallJumpShortcuts(string levelId, SoloRoomDefinition room)
        {
            var findings = new List<string>();
            CatMotorConfig motor = Config();
            float gravity = GravityStrength();
            if (motor == null || gravity <= 0f) { findings.Add($"{levelId}: no CatMotorConfig or gravity; wall jumps can't be checked (D-110)."); return findings; }
            findings.AddRange(ValidateGripWall(levelId, room, motor).Select(e => "(g) " + e));
            if (!TriggerCoverage.ClingSolids(room).Any()) return findings;
            TriggerCoverage.WallFindings(levelId, room, motor, gravity, findings);
            findings.AddRange(ValidateBaitGaps(levelId, room, motor, gravity).Where(e => e.Contains("D-110")).Select(e => "(c) " + e));
            findings.AddRange(ValidateTrapFloorHeadroom(levelId, room, motor).Where(e => e.Contains("D-110")).Select(e => "(d) " + e));
            findings.AddRange(SectionGatesByWall(levelId, room, motor, gravity));
            return findings;
        }

        static IEnumerable<string> SectionGatesByWall(string levelId, SoloRoomDefinition room, CatMotorConfig motor, float gravity)
        {
            if (!room.HasCheckpointSections) yield break;
            Vector2 start = room.Elements.Where(e => e.Kind == SoloRoomElementKind.Checkpoint).Select(e => e.Position).DefaultIfEmpty(Vector2.zero).First();
            foreach (CheckpointSection s in room.CheckpointSections.Skip(1))
            {
                if (s.GravityUp) continue;
                var target = new Rect(s.Checkpoint.x - motor.ColliderSize.x * .5f, s.Checkpoint.y, motor.ColliderSize.x, motor.ColliderSize.y);
                if (TriggerCoverage.ReachesBox(room, s.Gate, float.NegativeInfinity, float.PositiveInfinity, start, motor, gravity, target, walls: true)
                    && !TriggerCoverage.ReachesBox(room, s.Gate, float.NegativeInfinity, float.PositiveInfinity, start, motor, gravity, target, walls: false))
                    yield return $"(e) {levelId}: wall jumps reach section {s.Name}'s checkpoint ({s.Checkpoint.x:F2}, {s.Checkpoint.y:F2}) without passing through its gate {s.Gate}.";
            }
        }
    }
}
