using System.Collections.Generic;
using System.Linq;
using Parallax.Gameplay.Player;
using UnityEngine;

namespace Parallax.Editor.Setup
{
    // PAX-103 (the developer: "the exit shouldn't be reachable while I am inverted"; L004, L009 and L010 each let a cat
    // walking a roof upside down finish). In a room with a gravity flip, no door pose (its authored one and each retreat's)
    // may be touched by a gravity-up cat:
    //  - from an underside it stands on: it jumps "down" (away from the roof) JumpHeight, so its body reaches
    //    ColliderSize.y + JumpHeight below the underside; across, it reaches the run speed times the time it spends below the
    //    door's top, plus coyote time, from anywhere its collider still overlaps the underside. Undersides covered by spikes
    //    (a Hazard or HiddenSpikes strip against them) aren't stood on; nor are pit bottoms or fake platforms;
    //  - by falling up into it: a flip below the door, with the door between the flip and the first underside above it,
    //    within the drift a cat can steer while it rises.
    // Static and conservative: any underside counts, wherever it is, once the room has a flip.
    public static partial class LevelLayoutValidator
    {
        public static List<string> ValidateDoorUpright(string levelId, SoloRoomDefinition room, CatMotorConfig motor, float gravity)
        {
            var errors = new List<string>();
            if (motor == null || gravity <= 0f) { errors.Add($"{levelId}: door upright: no CatMotorConfig or gravity."); return errors; }
            List<SoloRoomElement> flips = room.Elements.Where(e => e.Kind is SoloRoomElementKind.GravityFlip or SoloRoomElementKind.Inverter).ToList();
            if (flips.Count == 0) return errors;
            List<(string pose, Rect box)> doors = DoorPoses(room);
            if (doors.Count == 0) return errors;

            float height = motor.ColliderSize.y, halfWidth = motor.ColliderSize.x * 0.5f, speed = motor.MaxSpeed;
            float launch = Mathf.Sqrt(2f * gravity * motor.JumpHeight), coyote = motor.CoyoteTime * speed;
            List<Rect> spikes = room.Elements.Where(e => e.Kind is SoloRoomElementKind.Hazard or SoloRoomElementKind.HiddenSpikes).Select(e => Box(e, room.Origin)).ToList();
            List<SoloRoomSkin.Edge> undersides = SoloRoomSkin.ExposedEdges(SoloRoomSkin.Solids(room))
                .Where(e => e.Side == SoloRoomSkin.Side.Bottom && e.Owner.Shape != SoloRoomSkin.Shape.PitBottom && e.Owner.Kind != SoloRoomElementKind.FakePlatform && e.Length >= 0.4f).ToList();

            foreach ((string pose, Rect door) in doors)
            {
                // From an underside: the time the jumping cat's body is below the door's top, and how far it gets across then.
                foreach (SoloRoomSkin.Edge u in undersides)
                {
                    float depth = u.Line - height - door.yMax;            // how far below its standing pose the body must go
                    if (door.yMin >= u.Line) continue;                     // the door is above the underside: not this way
                    float t;
                    if (depth <= 0f) t = 2f * launch / gravity;           // level with it already: the whole jump
                    else
                    {
                        float disc = launch * launch - 2f * gravity * depth;
                        if (disc < 0f) continue;                           // out of reach below
                        t = (launch + Mathf.Sqrt(disc)) / gravity;        // the later of the two crossings
                    }
                    float across = speed * t + coyote + halfWidth;
                    foreach ((float from, float to) in Standable(u, spikes))
                        if (door.xMax > from - halfWidth - across && door.xMin < to + halfWidth + across)
                        {
                            errors.Add($"{levelId}: door upright: {pose} {Describe(door, room.Origin)} is reachable by a gravity-up cat on {u.Owner.Name}'s underside (x {from - room.Origin.x:F2}-{to - room.Origin.x:F2}, y {u.Line - room.Origin.y:F2}); a door is only reached standing (PAX-103).");
                            goto nextDoor;
                        }
                }
                // Falling up: from a flip below the door, with nothing in between.
                foreach (SoloRoomElement f in flips)
                {
                    Rect zone = Box(f, room.Origin);
                    if (door.yMax <= zone.yMin) continue;
                    float ceiling = undersides.Where(u => u.Line >= zone.yMax - 1e-3f && u.From < zone.xMax && u.To > zone.xMin).Select(u => u.Line).DefaultIfEmpty(float.PositiveInfinity).Min();
                    if (door.yMin >= ceiling) continue;
                    float rise = Mathf.Max(0f, door.yMin - zone.yMin);
                    float drift = speed * Mathf.Sqrt(2f * rise / gravity) + halfWidth;
                    if (door.xMax > zone.xMin - drift && door.xMin < zone.xMax + drift)
                    {
                        errors.Add($"{levelId}: door upright: {pose} {Describe(door, room.Origin)} is reachable by a cat falling up from {f.Name}; a door is only reached standing (PAX-103).");
                        break;
                    }
                }
                nextDoor: ;
            }
            return errors;
        }

        // The door's authored box, and the box at the end of each retreat that moves it.
        static List<(string pose, Rect box)> DoorPoses(SoloRoomDefinition room)
        {
            var poses = new List<(string, Rect)>();
            foreach (SoloRoomElement door in room.Elements.Where(e => e.Kind == SoloRoomElementKind.Door))
            {
                Rect box = Box(door, room.Origin);
                poses.Add((door.Name, box));
                foreach (SoloRoomElement retreat in room.Elements.Where(e => e.Kind == SoloRoomElementKind.DoorRetreat && e.Settings.IsConfigured))
                {
                    Rect moved = box; moved.position += retreat.Settings.Offset;
                    poses.Add(($"{door.Name} (after {retreat.Name})", moved));
                }
            }
            return poses;
        }

        // The stretches of an underside a gravity-up cat can stand on: the edge less any spike strip against it.
        static IEnumerable<(float from, float to)> Standable(SoloRoomSkin.Edge u, List<Rect> spikes)
        {
            var cuts = spikes.Where(s => Mathf.Abs(s.yMax - u.Line) < 0.05f && s.xMax > u.From && s.xMin < u.To).Select(s => (s.xMin, s.xMax)).OrderBy(c => c.xMin).ToList();
            float at = u.From;
            foreach ((float x0, float x1) in cuts)
            {
                if (x0 - at >= 0.4f) yield return (at, x0);
                at = Mathf.Max(at, x1);
            }
            if (u.To - at >= 0.4f) yield return (at, u.To);
        }

        static string Describe(Rect r, Vector2 origin) => Describe(new Rect(r.position - origin, r.size));
    }
}
