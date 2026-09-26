using System.Collections.Generic;
using System.Linq;
using Parallax.Core;
using Parallax.Gameplay.Player;
using UnityEngine;

namespace Parallax.Editor.Setup
{
    public static partial class LevelLayoutValidator
    {
        // ---------- PAX-087 (D-089): climbable vines ----------
        // A vine's box is its grab box: width VineWidth, height ≥ MinVineHeight. Separately named, not part of Validate(),
        // like ValidateGeyser. The level band (levels 11+ only) is ValidateBand's. A leap off a vine is proven by the room's
        // routes (a timed step), never a RequiredJump. A vine is never a kill volume or a surface.

        public const float VineWidth = .6f, MinVineHeight = 1.5f;

        internal static bool IsVine(SoloRoomElement e) => e.Kind == SoloRoomElementKind.Vine;

        public static List<string> ValidateVine(string levelId, SoloRoomDefinition room, CatMotorConfig config)
        {
            var errors = new List<string>();
            SoloRoomElement[] vines = room.Elements.Where(IsVine).ToArray();
            if (vines.Length == 0) return errors;
            if (config == null) { errors.Add($"{levelId}: no CatMotorConfig; a vine's climbing clearance is undefined."); return errors; }
            (float minY, float maxY) = VerticalBounds(room);
            SoloRoomElement[] solids = room.Elements.Where(IsGeyserSolid).ToArray();
            Vector2 cat = config.ColliderSize;
            foreach (SoloRoomElement e in vines)
            {
                Rect box = Box(e, Vector2.zero);
                if (Mathf.Abs(e.Size.x - VineWidth) > 1e-3f) errors.Add($"{levelId}: {e.Name} is {e.Size.x:F2} u wide; a vine's grab box is {VineWidth:F2} u (D-089).");
                if (e.Size.y < MinVineHeight - 1e-3f) errors.Add($"{levelId}: {e.Name} is {e.Size.y:F2} u tall; a vine is at least {MinVineHeight:F2} u (D-089).");
                CheckFrame(levelId, e.Name, box, room.Width, minY, maxY, errors);
                // The cat snapped to the vine's centre, from standing at its bottom to its collider's top at its top (R5, as
                // amended: the cat stays on the vine).
                Rect climber = Rect.MinMaxRect(e.Position.x - cat.x * .5f, box.yMin, e.Position.x + cat.x * .5f, box.yMax);
                CheckFrame(levelId, $"{e.Name}'s climbing cat", climber, room.Width, minY, maxY, errors);
                foreach (SoloRoomElement solid in solids)
                {
                    Rect s = Box(solid, Vector2.zero);
                    if (s.Overlaps(box)) errors.Add($"{levelId}: {e.Name} {Describe(box)} overlaps {solid.Name}; a vine never overlaps a solid.");
                    else if (s.Overlaps(climber)) errors.Add($"{levelId}: the cat snapped to {e.Name}'s centre {Describe(climber)} overlaps {solid.Name}; the grab would push it into the solid (R6).");
                }
                if (!BottomReachable(e, box, solids, cat, config.JumpHeight))
                    errors.Add($"{levelId}: {e.Name}'s bottom (y {box.yMin:F2}) is neither on a floor nor within a jump's reach ({config.JumpHeight:F2} u) of one beside it (R5).");
                CheckSnap(levelId, e, errors);
            }
            return errors;
        }

        // A snap vine is a reveal (D-080 (1)): its routes must declare a betrayal revealed by it, so its lead (Dies) or
        // its recovery (Recovers) is measured.
        public static List<string> ValidateVineRoutes(string levelId, SoloRoomDefinition room, Routes.RoomRoutes routes)
        {
            var errors = new List<string>();
            foreach (SoloRoomElement e in room.Elements.Where(e => IsVine(e) && e.Settings.IsConfigured))
                if (routes == null || !routes.Betrayals.Any(b => b.RevealedBy == e.Name))
                    errors.Add($"{levelId}: {e.Name} is a snap vine, but no declared betrayal route is revealed by it; its reveal must be measured (D-080, D-089).");
            return errors;
        }

        // R5 (1): some solid top the cat can stand on beside the vine (its centre within a collider's half width plus the
        // vine's), below the vine's top, and no more than the cat's height plus a jump below the vine's bottom.
        static bool BottomReachable(SoloRoomElement vine, Rect box, SoloRoomElement[] solids, Vector2 cat, float jumpHeight)
        {
            float reachX = (cat.x + VineWidth) * .5f;
            foreach (SoloRoomElement solid in solids)
            {
                Rect s = Box(solid, Vector2.zero);
                if (s.xMax <= vine.Position.x - reachX || s.xMin >= vine.Position.x + reachX) continue;
                if (s.yMax >= box.yMax || s.yMax + cat.y + jumpHeight <= box.yMin) continue;
                return true;
            }
            return false;
        }

        static void CheckSnap(string levelId, SoloRoomElement e, List<string> errors)
        {
            if (!e.Settings.IsConfigured) return;
            SoloRoomTrapSettings s = e.Settings;
            if (s.RepeatMode != TrapRepeatMode.Once) errors.Add($"{levelId}: {e.Name} is a snap vine set to {s.RepeatMode}; a snap vine is Once (D-089).");
            if (s.DelayTicks < 0) errors.Add($"{levelId}: {e.Name} has a negative snap delay.");
            if (s.TriggerSource == TrapTriggerSource.Overlap && e.SecondarySize != Vector2.zero
                && !new Rect(e.SecondaryPosition - e.SecondarySize * .5f, e.SecondarySize).Overlaps(Box(e, Vector2.zero)))
                errors.Add($"{levelId}: {e.Name}'s snap trigger doesn't overlap the vine; only a climbing cat should set it off.");
        }
    }
}
