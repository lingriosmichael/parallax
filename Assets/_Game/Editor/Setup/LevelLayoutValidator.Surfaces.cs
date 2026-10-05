using System.Collections.Generic;
using System.Linq;
using Parallax.Core;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Editor.Setup
{
    // PAX-080 (D-080): surfaces that betray without killing by themselves, and the fake platform's data rules.
    public static partial class LevelLayoutValidator
    {
        // R4 (b): named exemptions ("level/surface" -> reason). PAX-059: L004's FalseLanding, the one entry, went with the
        // redesign; the list stays for any later one, which needs the developer's approval by name.
        public static readonly IReadOnlyDictionary<string, string> SurfaceCoverageExemptions = new Dictionary<string, string>
        {
            // D-111 (approved by the developer, 2026-10-04): the cat steps onto the floor between the walls before Block_1's
            // trigger; it shrinks only 30 ticks after the walls set off, so the part before the trigger betrays nothing early.
            ["L001/Squeeze"] = "D-111: shrinks after the walls land; the strip before Block_1's trigger is harmless until then",
        };

        // A betraying surface's danger is its top strip: the element's width, one cat-collider height tall, on its
        // top. Fake platforms and Overlap, non-periodic, unchained collapsing floors are covered by their own touch, and a
        // surface whose root trigger holds its whole top strip is covered by that trigger (PAX-059a play).
        // Every other betraying surface (a chained one, checked against its root trigger, or a Solid MovingTrap that
        // moves down or off its own x span) needs that trigger to be D-074's cut: over its x span it covers the
        // cat's band, and the strip lies beyond its near edge as seen from where the cat reaches it. Periodic surfaces
        // are ValidatePeriodicSlack's, and are skipped here.
        // PAX-091 (Q6): per storey, like trigger coverage: the band is the trigger's storey (TriggerCoverage.StoreyLow to
        // TryStoreyCeiling), and the sides are the approach search's (TriggerCoverage.ApproachSides), not the checkpoint's.
        public static List<string> ValidateSurfaceCoverage(string levelId, SoloRoomDefinition room, CatMotorConfig motor)
        {
            var errors = new List<string>();
            if (motor == null) { errors.Add($"{levelId}: surface coverage: no CatMotorConfig; the top strip is undefined."); return errors; }
            const float eps = 1e-3f;
            var byName = room.Elements.ToDictionary(e => e.Name);
            Vector2 checkpoint = room.Elements.Where(e => e.Kind == SoloRoomElementKind.Checkpoint).Select(e => e.Position).DefaultIfEmpty(Vector2.zero).First();
            float gravity = GravityStrength();
            foreach (SoloRoomElement surface in room.Elements.Where(IsBetrayingSurface))
            {
                if (SurfaceCoverageExemptions.ContainsKey(levelId + "/" + surface.Name) || IsSelfCovered(surface)) continue;
                SoloRoomElement root = ChainRoot(surface, byName);
                if (root.Settings.RepeatMode == TrapRepeatMode.Periodic) continue;
                Rect strip = Rect.MinMaxRect(surface.Position.x - surface.Size.x * .5f, surface.Position.y + surface.Size.y * .5f,
                    surface.Position.x + surface.Size.x * .5f, surface.Position.y + surface.Size.y * .5f + motor.ColliderSize.y);
                string via = root.Name == surface.Name ? "" : $" (chained from {root.Name})";
                if (!TriggerCoverage.TryTrigger(root, out Rect trigger))
                { errors.Add($"{levelId}: surface coverage: {surface.Name}{via} has no trigger box to cut the cat's band."); continue; }
                // A trigger over the surface's whole top strip holds any cat standing on it, so the surface can't be stood on
                // unseen (and a jump from below doesn't set it off: ValidateTrapFloorHeadroom). After PAX-059a play.
                if (trigger.xMin <= strip.xMin + eps && trigger.xMax >= strip.xMax - eps && trigger.yMin <= strip.yMin + eps && trigger.yMax >= strip.yMax - eps) continue;
                float low = TriggerCoverage.StoreyLow(room, trigger);
                if (!TriggerCoverage.TryStoreyCeiling(room, trigger, low, out float ceiling))
                { errors.Add($"{levelId}: surface coverage: no ceiling over {root.Name}'s trigger; the band is undefined."); continue; }
                if (trigger.yMin > low + eps || trigger.yMax < ceiling - eps)
                { errors.Add($"{levelId}: surface coverage: {surface.Name}{via}: {root.Name}'s trigger {Describe(trigger)} does not cut the cat's band y [{low:F2}, {ceiling:F2}]: {TriggerCoverage.UncoveredBands(trigger, low, ceiling)}."); continue; }
                TriggerCoverage.Side sides = TriggerCoverage.ApproachSides(room, trigger, low, ceiling, checkpoint, motor, gravity, root);
                foreach (TriggerCoverage.Side side in new[] { TriggerCoverage.Side.Left, TriggerCoverage.Side.Right })
                {
                    if ((sides & side) == 0) continue;
                    float nearEdge = side == TriggerCoverage.Side.Left ? trigger.xMin : trigger.xMax;
                    bool beyond = side == TriggerCoverage.Side.Left ? strip.xMin >= nearEdge - eps : strip.xMax <= nearEdge + eps;
                    if (!beyond) errors.Add($"{levelId}: surface coverage: {surface.Name}{via} top strip {Describe(strip)} lies before {root.Name}'s trigger near edge x {nearEdge:F2}, {TriggerCoverage.SeenFrom(side, sides)}.");
                }
            }
            return errors;
        }

        // R1: the builder forces a fake platform's timing (Overlap, Once, delay 0) and gives it no trigger box, so its
        // data must say the same: default settings and no secondary box.
        public static List<string> ValidateFakePlatformSettings(string levelId, SoloRoomDefinition room)
        {
            var errors = new List<string>();
            foreach (SoloRoomElement e in room.Elements.Where(e => e.Kind == SoloRoomElementKind.FakePlatform))
            {
                bool settings = !e.Settings.IsConfigured || e.Settings.Equals(TrapKitSetup.FakePlatformSettings);
                if (!settings) errors.Add($"{levelId}: {e.Name} is a fake platform with non-default trap settings; the builder always uses Overlap, Once, delay 0 (D-080).");
                if (e.SecondarySize != Vector2.zero || e.SecondaryPosition != Vector2.zero) errors.Add($"{levelId}: {e.Name} is a fake platform with a secondary (trigger) box; it triggers on its own body (D-080).");
            }
            return errors;
        }

        static bool IsBetrayingSurface(SoloRoomElement e)
        {
            if (e.Kind == SoloRoomElementKind.FakePlatform || e.Kind == SoloRoomElementKind.CollapsingFloor || e.Kind == SoloRoomElementKind.ShrinkingFloor) return true;
            if (e.Kind != SoloRoomElementKind.MovingTrap || e.Settings.MovingKind != MovingTrapKind.Solid) return false;
            // The moved pose covers the strip's x span only when it doesn't move sideways.
            return e.Settings.Offset.y < 0f || !Mathf.Approximately(e.Settings.Offset.x, 0f);
        }

        static bool IsSelfCovered(SoloRoomElement e) =>
            e.Kind == SoloRoomElementKind.FakePlatform
            || (e.Kind == SoloRoomElementKind.ShrinkingFloor && e.SecondarySize == Vector2.zero && e.Settings.TriggerSource == TrapTriggerSource.Overlap && e.Settings.RepeatMode != TrapRepeatMode.Periodic)   // PAX-093: its own top
            || (e.Kind == SoloRoomElementKind.CollapsingFloor && e.Settings.TriggerSource == TrapTriggerSource.Overlap && e.Settings.RepeatMode != TrapRepeatMode.Periodic);

        static SoloRoomElement ChainRoot(SoloRoomElement e, Dictionary<string, SoloRoomElement> byName)
        {
            var seen = new HashSet<string> { e.Name };
            while (e.Settings.IsConfigured && e.Settings.TriggerSource == TrapTriggerSource.Chain
                && byName.TryGetValue(e.Settings.ChainSource ?? string.Empty, out SoloRoomElement source) && seen.Add(source.Name))
                e = source;
            return e;
        }

        // ValidateReach: a RequiredJump lands on a fake platform when it names one as its destination, or when its
        // landing point is on a fake platform's top (floor frame).
        static bool LandsOnFakePlatform(SoloRoomDefinition room, Dictionary<string, SoloRoomElement> byName, RequiredJump jump, out string fake)
        {
            fake = null;
            if (jump.DestinationName != null && byName.TryGetValue(jump.DestinationName, out SoloRoomElement named) && named.Kind == SoloRoomElementKind.FakePlatform)
            { fake = named.Name; return true; }
            if (jump.Frame != RequiredJumpFrame.Floor) return false;
            foreach (SoloRoomElement e in room.Elements.Where(e => e.Kind == SoloRoomElementKind.FakePlatform))
            {
                Rect r = Box(e, Vector2.zero);
                if (Mathf.Abs(r.yMax - jump.LandingPawHeight) < 1e-3f && jump.LandingX >= r.xMin && jump.LandingX <= r.xMax) { fake = e.Name; return true; }
            }
            return false;
        }
    }
}
