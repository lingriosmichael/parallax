using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Parallax.Editor.Setup
{
    public static partial class LevelLayoutValidator
    {
        // ---------- PAX-090 (D-091): checkpoint sections, the layout half ----------
        // Separately named and part of ValidateKit. The route half (the solution passes the gates in order, each section's
        // time ≤ 1000 ticks, respawn safety, exact rewinds) is RouteValidator.ValidateSections, run by RouteValidator.Run.
        // Allowed in every level (§11 Q6). No layout element can own an anchor, so "no anchors in a sectioned room" holds by
        // construction here; RoomDeath logs an error if a scene ever pairs them.
        //  - at least two sections, names unique and not an element's (nor a marker's);
        //  - section 0 is the start: no gate, its checkpoint is the room's Checkpoint element, gravity down;
        //  - every later section has a gate, and a checkpoint that is inside the room, standable (on a fixed Floor, Wall or
        //    PitBottom top with gravity down, under a fixed Ceiling or Wall underside with it up), outside its gate, and on
        //    the gate's far side from the previous checkpoint (along the gate's thin axis);
        //  - every trap element (anything with runtime state) is owned by exactly one section; owned names are trap elements.

        const float StandTolerance = .01f;

        internal static bool IsSectionOwned(SoloRoomElement e) => e.Kind switch
        {
            SoloRoomElementKind.Floor or SoloRoomElementKind.Ceiling or SoloRoomElementKind.Wall or SoloRoomElementKind.GripWall or SoloRoomElementKind.PitBottom
                or SoloRoomElementKind.Checkpoint or SoloRoomElementKind.Door or SoloRoomElementKind.Hazard => false,
            _ => true,
        };

        public static List<string> ValidateSections(string levelId, SoloRoomDefinition room)
        {
            var errors = new List<string>();
            CheckpointSection[] sections = room.CheckpointSections ?? System.Array.Empty<CheckpointSection>();
            if (sections.Length == 0) return errors;
            if (sections.Length == 1) errors.Add($"{levelId}: one checkpoint section is the room itself; declare none instead.");

            var elementNames = new HashSet<string>(room.Elements.Select(e => e.Name));
            var names = new HashSet<string>();
            foreach (CheckpointSection s in sections)
            {
                if (string.IsNullOrEmpty(s.Name)) { errors.Add($"{levelId}: a checkpoint section has no name."); continue; }
                if (!names.Add(s.Name)) errors.Add($"{levelId}: two checkpoint sections are named '{s.Name}'.");
                if (elementNames.Contains(s.Name) || elementNames.Contains(s.MarkerName)) errors.Add($"{levelId}: checkpoint section '{s.Name}' (or its marker '{s.MarkerName}') has an element's name.");
            }

            SoloRoomElement[] checkpoints = room.Elements.Where(e => e.Kind == SoloRoomElementKind.Checkpoint).ToArray();
            CheckpointSection start = sections[0];
            if (start.HasGate) errors.Add($"{levelId}: section 0 '{start.Name}' is the start and has no gate.");
            if (start.GravityUp) errors.Add($"{levelId}: section 0 '{start.Name}' starts with gravity down, like the room's checkpoint.");
            if (checkpoints.Length != 1 || (checkpoints[0].Position - start.Checkpoint).sqrMagnitude > 1e-6f)
                errors.Add($"{levelId}: section 0 '{start.Name}''s checkpoint {start.Checkpoint} is not the room's Checkpoint element.");

            (float minY, float maxY) = VerticalBounds(room);
            for (int i = 1; i < sections.Length; i++)
            {
                CheckpointSection s = sections[i];
                string name = $"{levelId}: section '{s.Name}'";
                if (!s.HasGate) { errors.Add($"{name} has no gate."); continue; }
                Vector2 c = s.Checkpoint;
                if (c.x < 0f || c.x > room.Width || c.y < minY || c.y > maxY) errors.Add($"{name}: checkpoint {c} is outside the room (x 0-{room.Width}, y {minY:F2}-{maxY:F2}).");
                if (!Standable(room, c, s.GravityUp)) errors.Add($"{name}: checkpoint {c} isn't standable: no fixed {(s.GravityUp ? "underside" : "top")} at y {c.y:F2} under x {c.x:F2}.");
                if (s.Gate.Contains(c)) errors.Add($"{name}: checkpoint {c} is inside its own gate {Describe(s.Gate)}.");
                else if (!FarSide(s.Gate, sections[i - 1].Checkpoint, c)) errors.Add($"{name}: checkpoint {c} is on the same side of its gate {Describe(s.Gate)} as the previous checkpoint {sections[i - 1].Checkpoint}.");
            }

            var owner = new Dictionary<string, string>();
            foreach (CheckpointSection s in sections)
                foreach (string owned in s.Owns ?? System.Array.Empty<string>())
                {
                    SoloRoomElement[] match = room.Elements.Where(e => e.Name == owned).ToArray();
                    if (match.Length == 0) errors.Add($"{levelId}: section '{s.Name}' owns '{owned}', which isn't an element.");
                    else if (!IsSectionOwned(match[0])) errors.Add($"{levelId}: section '{s.Name}' owns '{owned}', a {match[0].Kind} with no runtime state.");
                    if (owner.TryGetValue(owned, out string first)) errors.Add($"{levelId}: '{owned}' is owned by both '{first}' and '{s.Name}'.");
                    else owner[owned] = s.Name;
                }
            foreach (SoloRoomElement e in room.Elements)
                if (IsSectionOwned(e) && !owner.ContainsKey(e.Name)) errors.Add($"{levelId}: {e.Kind} '{e.Name}' is owned by no checkpoint section.");
            return errors;
        }

        static bool Standable(SoloRoomDefinition room, Vector2 paw, bool gravityUp)
        {
            foreach (SoloRoomElement e in room.Elements)
            {
                if (!(IsFixedSolid(e))) continue;
                Rect r = new(e.Position - e.Size * .5f, e.Size);
                float face = gravityUp ? r.yMin : r.yMax;
                if (Mathf.Abs(face - paw.y) <= StandTolerance && paw.x >= r.xMin && paw.x <= r.xMax) return true;
            }
            return false;
        }

        // Along the gate's thin axis (x for a gate taller than wide), the previous checkpoint and this one lie on opposite
        // sides of the gate's centre.
        static bool FarSide(Rect gate, Vector2 previous, Vector2 checkpoint)
        {
            bool vertical = gate.width <= gate.height;
            float centre = vertical ? gate.center.x : gate.center.y;
            float a = (vertical ? previous.x : previous.y) - centre, b = (vertical ? checkpoint.x : checkpoint.y) - centre;
            return a * b < 0f;
        }
    }
}
