using System.Collections.Generic;
using System.Linq;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Editor.Setup
{
    // PAX-105 (D-110 amendment 2, §14 G3): a grip wall is a fair, readable place to grab.
    // - At least the motor's MinClingFaceHeight (1.0 u) tall: a shorter one is never grabbed (D-110 (2)).
    // - Inside the frame: x within the room (0 to its width), y no lower than the room's side walls (-4) and under the ceiling
    //   above it, if there is one.
    // - It overlaps no other solid (geometry, trap floors, blocks, movers, other grip walls): it stands on its own, so the face
    //   the cat sees is the face it grabs.
    // - Not over a hazard: no hazard (spikes, a moving hazard, hidden spikes) overlaps it or the column a clinging cat fills
    //   beside either face, from its top down to the first solid top under that column (where a sliding cat drops to).
    // No band restriction. Gravity-up checks don't apply (no cling with gravity up).
    public static partial class LevelLayoutValidator
    {
        public static List<string> ValidateGripWall(string levelId, SoloRoomDefinition room, CatMotorConfig motor)
        {
            var errors = new List<string>();
            // D-110 amendment 3: a grip falling block is checked at its landed pose (where it can be grabbed).
            var landed = TriggerCoverage.GripBlocksLanded(room).ToDictionary(b => b.name, b => b.box);
            SoloRoomElement[] grips = room.Elements.Where(e => e.Kind == SoloRoomElementKind.GripWall || landed.ContainsKey(e.Name)).ToArray();
            if (grips.Length == 0) return errors;
            if (motor == null) { errors.Add($"{levelId}: no CatMotorConfig; grip walls can't be checked (D-110)."); return errors; }
            const float eps = 1e-3f;
            float catWidth = motor.ColliderSize.x;
            foreach (SoloRoomElement g in grips)
            {
                Rect r = landed.TryGetValue(g.Name, out Rect at) ? at : Box(g, Vector2.zero);
                if (r.height < motor.MinClingFaceHeight - eps)
                    errors.Add($"{levelId}: grip wall {g.Name} is {r.height:F2} u tall; a grip face needs at least {motor.MinClingFaceHeight:F2} u (D-110 (2)).");
                if (r.xMin < -eps || r.xMax > room.Width + eps || r.yMin < -4f - eps)
                    errors.Add($"{levelId}: grip wall {g.Name} {Describe(r)} lies outside the room's frame (x 0 to {room.Width:F2}, y from -4).");
                float ceiling = room.Elements.Where(e => e.Kind == SoloRoomElementKind.Ceiling).Select(e => Box(e, Vector2.zero))
                    .Where(c => c.xMax > r.xMin + eps && c.xMin < r.xMax - eps && c.yMin >= r.center.y).Select(c => c.yMin).DefaultIfEmpty(float.PositiveInfinity).Min();
                if (r.yMax > ceiling + eps)
                    errors.Add($"{levelId}: grip wall {g.Name} {Describe(r)} reaches past the ceiling above it (underside {ceiling:F2}).");
                foreach (SoloRoomElement o in room.Elements)
                {
                    if (o.Name == g.Name || !IsSolidForGrip(o)) continue;
                    Rect b = Box(o, Vector2.zero);
                    if (Mathf.Min(r.xMax, b.xMax) - Mathf.Max(r.xMin, b.xMin) > eps && Mathf.Min(r.yMax, b.yMax) - Mathf.Max(r.yMin, b.yMin) > eps)
                        errors.Add($"{levelId}: grip wall {g.Name} {Describe(r)} overlaps {o.Name} {Describe(b)}; a grip wall stands on its own (D-110 amendment 2).");
                }
                var columns = new[] { Rect.MinMaxRect(r.xMin - catWidth, r.yMin, r.xMin, r.yMax), r, Rect.MinMaxRect(r.xMax, r.yMin, r.xMax + catWidth, r.yMax) };
                // D-117: a face a solid covers from its foot to its top (L004's Corbel_Moss against Corbel_O) has no cat beside it.
                bool Covered(float line, int side) => room.Elements.Where(o => o.Name != g.Name && IsSolidForGrip(o)).Select(o => Box(o, Vector2.zero))
                    .Any(b => Mathf.Abs((side < 0 ? b.xMax : b.xMin) - line) <= eps && b.yMin <= r.yMin + eps && b.yMax >= r.yMax - eps);
                if (Covered(r.xMin, -1)) columns[0] = Rect.zero;
                if (Covered(r.xMax, 1)) columns[2] = Rect.zero;
                foreach (Rect column in columns.Where(c => c != Rect.zero))
                {
                    float landing = room.Elements.Where(o => o.Name != g.Name && IsSolidForGrip(o)).Select(o => Box(o, Vector2.zero))
                        .Where(b => b.xMax > column.xMin + eps && b.xMin < column.xMax - eps && b.yMax <= column.yMin + eps).Select(b => b.yMax).DefaultIfEmpty(-4f).Max();
                    var reach = Rect.MinMaxRect(column.xMin, landing, column.xMax, column.yMax);
                    foreach (SoloRoomElement h in room.Elements.Where(IsHazardForGrip))
                    {
                        Rect b = Box(h, Vector2.zero);
                        if (Mathf.Min(reach.xMax, b.xMax) - Mathf.Max(reach.xMin, b.xMin) > eps && Mathf.Min(reach.yMax, b.yMax) - Mathf.Max(reach.yMin, b.yMin) > -eps
                            && !errors.Any(e => e.Contains($"grip wall {g.Name} ") && e.Contains($"hazard {h.Name} ")))
                            errors.Add($"{levelId}: grip wall {g.Name} {Describe(r)} is over the hazard {h.Name} {Describe(b)}: a cat clinging to it would touch it or slide onto it.");
                    }
                }
            }
            return errors;
        }

        static bool IsSolidForGrip(SoloRoomElement e) =>
            e.Kind is SoloRoomElementKind.Floor or SoloRoomElementKind.Ceiling or SoloRoomElementKind.Wall or SoloRoomElementKind.GripWall
                or SoloRoomElementKind.PitBottom or SoloRoomElementKind.CollapsingFloor or SoloRoomElementKind.ShrinkingFloor or SoloRoomElementKind.FallingBlock
                or SoloRoomElementKind.FakePlatform
            || (e.Kind == SoloRoomElementKind.MovingTrap && e.Settings.MovingKind == MovingTrapKind.Solid);

        static bool IsHazardForGrip(SoloRoomElement e) =>
            e.Kind is SoloRoomElementKind.Hazard or SoloRoomElementKind.HiddenSpikes
            || (e.Kind == SoloRoomElementKind.MovingTrap && e.Settings.MovingKind == MovingTrapKind.Hazard);
    }
}
