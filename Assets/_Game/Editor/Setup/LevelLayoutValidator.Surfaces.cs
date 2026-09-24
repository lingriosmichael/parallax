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
        // R4 (b): FalseLanding's trigger doesn't cut the band, and no route is betrayed by it (D-079 (8)). A
        // learned-bypass reason on the layout would break TriggerCoverageTests' "no learned bypass in L001-L004".
        public static readonly IReadOnlyDictionary<string, string> SurfaceCoverageExemptions = new Dictionary<string, string>
        {
            ["L004/FalseLanding"] = "PAX-075: the solution's braked landing stays below the trigger; no route is betrayed (D-079, D-080).",
        };

        // A betraying surface's danger is its top strip: the element's width, one cat-collider height tall, on its
        // top. Fake platforms and Overlap, non-periodic, unchained collapsing floors are covered by their own touch.
        // Every other betraying surface (a chained one, checked against its root trigger, or a Solid MovingTrap that
        // moves down or off its own x span) needs that trigger to be D-074's cut: over its x span it covers the
        // cat's band from the lowest standable top to the ceiling underside, and the strip lies beyond its near
        // edge as seen from the checkpoint. Periodic surfaces are ValidatePeriodicSlack's, and are skipped here.
        public static List<string> ValidateSurfaceCoverage(string levelId, SoloRoomDefinition room, CatMotorConfig motor)
        {
            var errors = new List<string>();
            if (motor == null) { errors.Add($"{levelId}: surface coverage: no CatMotorConfig; the top strip is undefined."); return errors; }
            const float eps = 1e-3f;
            var byName = room.Elements.ToDictionary(e => e.Name);
            float checkpointX = room.Elements.Where(e => e.Kind == SoloRoomElementKind.Checkpoint).Select(e => e.Position.x).DefaultIfEmpty(0f).First();
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
                if (!TriggerCoverage.TryCeilingUnderside(room, trigger, out float ceiling))
                { errors.Add($"{levelId}: surface coverage: no ceiling over {root.Name}'s trigger; the band is undefined."); continue; }
                float low = TriggerCoverage.BandLow(room, trigger);
                if (trigger.yMin > low + eps || trigger.yMax < ceiling - eps)
                { errors.Add($"{levelId}: surface coverage: {surface.Name}{via}: {root.Name}'s trigger {Describe(trigger)} does not cut the cat's band y [{low:F2}, {ceiling:F2}]: {TriggerCoverage.UncoveredBands(trigger, low, ceiling)}."); continue; }
                bool fromLeft = checkpointX <= trigger.center.x;
                float nearEdge = fromLeft ? trigger.xMin : trigger.xMax;
                bool beyond = fromLeft ? strip.xMin >= nearEdge - eps : strip.xMax <= nearEdge + eps;
                if (!beyond) errors.Add($"{levelId}: surface coverage: {surface.Name}{via} top strip {Describe(strip)} lies before {root.Name}'s trigger near edge x {nearEdge:F2}, seen from the checkpoint.");
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
            if (e.Kind == SoloRoomElementKind.FakePlatform || e.Kind == SoloRoomElementKind.CollapsingFloor) return true;
            if (e.Kind != SoloRoomElementKind.MovingTrap || e.Settings.MovingKind != MovingTrapKind.Solid) return false;
            // The moved pose covers the strip's x span only when it doesn't move sideways.
            return e.Settings.Offset.y < 0f || !Mathf.Approximately(e.Settings.Offset.x, 0f);
        }

        static bool IsSelfCovered(SoloRoomElement e) =>
            e.Kind == SoloRoomElementKind.FakePlatform
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
