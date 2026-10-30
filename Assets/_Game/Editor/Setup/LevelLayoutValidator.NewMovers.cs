using System.Collections.Generic;
using System.Linq;
using Parallax.Core;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Editor.Setup
{
    public static partial class LevelLayoutValidator
    {
        // ---------- PAX-114/115/116 (D-187): path movers, glitch walls, flip platforms, spinners ----------
        // Separately named, part of ValidateKit.
        //  Path (PAX-114): every point inside the room; a smooth path's legs (home to point 1, point 1 to point 2, ...) and a
        //   glitch wall's poses overlap no fixed solid (touching is allowed, D-056 (3)); a glitch wall warns >= MinGlitchWarnTicks,
        //   and each of its steps (moveTicks / points out, returnTicks / points back) is longer than the warning.
        //  Flip (PAX-115): a Solid; flipTicks >= MinFlipTicks; the disc it turns through overlaps no fixed solid; not a hinge, not
        //   on a path, no offset.
        //  Plate, portal, turret (PAX-119/120/121, D-197), phase platform (PAX-122, D-201): see each check.
        //  Spinner (PAX-116): 1-4 arms; length > 0; width MinArmWidth-MaxArmWidth; turning; tip speed <= MaxTipSpeed; the disc inside
        //   the room, overlapping no fixed solid, the door or a checkpoint.
        internal const int MinGlitchWarnTicks = 6, MinFlipTicks = 6;
        // PAX-122 (D-201): the phase platform.
        internal const int MinPhaseWarnTicks = 6, PhaseStandTicks = 24, MinPhaseOffTicks = 12;
        internal const float MinArmWidth = .2f, MaxArmWidth = .6f, MaxTipSpeed = .5f;
        // PAX-119/120/121 (D-197).
        internal const float MinPlateWidth = .6f;
        internal const int MinTurretLock = 6;
        internal static readonly Vector2 MinPortalSize = new(.6f, .7f);

        /// <summary>PAX-114 (D-187): a mover's offsets from home, home first: home and each path point, or home and its offset.</summary>
        public static IEnumerable<Vector2> MoverPoints(SoloRoomElement e)
        {
            yield return Vector2.zero;
            if (e.Settings.Path.HasPoints) { foreach (Vector2 p in e.Settings.Path.Points) yield return p; }
            else if (e.Settings.Offset != Vector2.zero) yield return e.Settings.Offset;
        }

        /// <summary>PAX-114 (D-187): the stretches a mover's box sweeps: each leg's envelope (a glitch wall: each pose alone, it
        /// never passes between them).</summary>
        internal static IEnumerable<Rect> MoverLegs(Rect home, SoloRoomElement e)
        {
            Vector2[] points = MoverPoints(e).ToArray();
            if (e.Settings.Path.HasPoints && e.Settings.Path.Glitch) { foreach (Vector2 p in points) yield return Shift(home, p); yield break; }
            if (points.Length == 1) { yield return home; yield break; }
            for (int i = 1; i < points.Length; i++) yield return Envelope(Shift(home, points[i - 1]), Shift(home, points[i]));
        }

        /// <summary>PAX-114 (D-187): everywhere a mover's box goes: the envelope of every pose.</summary>
        internal static Rect MoverSweep(Rect home, SoloRoomElement e)
        {
            Rect swept = home;
            foreach (Vector2 p in MoverPoints(e)) swept = Envelope(swept, Shift(home, p));
            return swept;
        }

        static Rect Shift(Rect r, Vector2 by) { r.position += by; return r; }

        /// <summary>PAX-116 (D-187): the box around the disc a spinner's arms turn through. PAX-118 (D-197): a pendulum's, around the arc
        /// it swings through (sampled every 2 degrees), its pivot included.</summary>
        internal static Rect SpinnerDisc(SoloRoomElement e, Vector2 origin)
        {
            SpinnerSettings k = e.Settings.Spinner;
            if (k.Swings)
            {
                Vector2 pivot = origin + e.Position;
                float minX = pivot.x, maxX = pivot.x, minY = pivot.y, maxY = pivot.y, half = k.ArmWidth * .5f;
                for (float a = -k.SwingAmplitude; a <= k.SwingAmplitude + 1e-3f; a += 2f)
                {
                    float rad = (k.StartAngle + a) * Mathf.Deg2Rad;
                    Vector2 tip = pivot + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * k.ArmLength;
                    minX = Mathf.Min(minX, tip.x); maxX = Mathf.Max(maxX, tip.x); minY = Mathf.Min(minY, tip.y); maxY = Mathf.Max(maxY, tip.y);
                }
                return Rect.MinMaxRect(minX - half, minY - half, maxX + half, maxY + half);
            }
            float r = e.Settings.Spinner.ArmLength + e.Settings.Spinner.ArmWidth * .5f;
            Vector2 c = origin + e.Position;
            return Rect.MinMaxRect(c.x - r, c.y - r, c.x + r, c.y + r);
        }

        public static List<string> ValidateNewMovers(string levelId, SoloRoomDefinition room)
        {
            const float eps = 1e-3f;
            var errors = new List<string>();
            SoloRoomElement[] fixedSolids = room.Elements.Where(IsFixedSolid).ToArray();
            foreach (SoloRoomElement e in room.Elements)
            {
                SoloRoomTrapSettings s = e.Settings;
                if (e.Kind == SoloRoomElementKind.MovingTrap && s.Path.IsConfigured) CheckPath(levelId, room, e, fixedSolids, errors);
                if (e.Kind == SoloRoomElementKind.MovingTrap && s.Floor.Flips) CheckFlip(levelId, e, fixedSolids, errors);
                if (e.Kind == SoloRoomElementKind.MovingTrap && s.Floor.Phases) CheckPhase(levelId, e, fixedSolids, errors);
                if (e.Kind == SoloRoomElementKind.Spinner) CheckSpinner(levelId, room, e, fixedSolids, errors);
                if (e.Kind == SoloRoomElementKind.Plate) CheckPlate(levelId, e, fixedSolids, errors);
                if (e.Kind == SoloRoomElementKind.Portal) CheckPortal(levelId, room, e, fixedSolids, errors);
                if (e.Kind == SoloRoomElementKind.Turret) CheckTurret(levelId, e, fixedSolids, errors);
            }
            return errors;

            // PAX-119 (D-197): flush on one fixed top under its whole width, at least the cat's width. D-220: or flush under one fixed
            // underside (a ceiling plate, pressed by a cat walking upside down).
            void CheckPlate(string id, SoloRoomElement e, SoloRoomElement[] solids, List<string> list)
            {
                Rect b = Box(e, Vector2.zero);
                if (b.width < MinPlateWidth - eps) list.Add($"{id}: {e.Name} is {b.width:F2} u wide; a plate is at least {MinPlateWidth} (PAX-119).");
                bool flush = solids.Any(f => { Rect r = Box(f, Vector2.zero); return (Mathf.Abs(r.yMax - b.yMin) <= .02f || Mathf.Abs(r.yMin - b.yMax) <= .02f) && r.xMin <= b.xMin + eps && r.xMax >= b.xMax - eps; });
                if (!flush) list.Add($"{id}: {e.Name} {Describe(b)} isn't flush on one fixed top or under one fixed underside, under its whole width (PAX-119, D-220).");
            }

            // PAX-120 (D-197): both boxes cat-sized, inside the room, clear of fixed solids and hazards (the exit at every pose).
            void CheckPortal(string id, SoloRoomDefinition r, SoloRoomElement e, SoloRoomElement[] solids, List<string> list)
            {
                Rect entry = Box(e, Vector2.zero), home = new(e.SecondaryPosition - e.SecondarySize * .5f, e.SecondarySize);
                var poses = new List<(Rect box, string what)> { (entry, "entry"), (home, "exit") };
                if (e.Settings.Path.HasPoints) foreach (Vector2 p in e.Settings.Path.Points) poses.Add((Shift(home, p), "exit"));
                Rect[] hazards = r.Elements.Where(h => h.Kind == SoloRoomElementKind.Hazard || h.Kind == SoloRoomElementKind.HiddenSpikes).Select(h => Box(h, Vector2.zero)).ToArray();
                foreach ((Rect box, string what) in poses)
                {
                    if (box.width < MinPortalSize.x - eps || box.height < MinPortalSize.y - eps) list.Add($"{id}: {e.Name}'s {what} {Describe(box)} is smaller than {MinPortalSize.x} x {MinPortalSize.y} (PAX-120).");
                    if (box.xMin < -eps || box.xMax > r.Width + eps) list.Add($"{id}: {e.Name}'s {what} {Describe(box)} leaves the room (PAX-120).");
                    foreach (SoloRoomElement f in solids) if (Overlap(box, Box(f, Vector2.zero)) > eps) list.Add($"{id}: {e.Name}'s {what} {Describe(box)} overlaps {f.Name} (PAX-120).");
                    if (what == "exit") foreach (Rect h in hazards) if (Overlap(box, h) > eps) list.Add($"{id}: {e.Name}'s exit {Describe(box)} overlaps a hazard (PAX-120).");
                }
            }

            // PAX-121 (D-197): a lock (its tell) of at least MinTurretLock, every phase at least a tick, Periodic, a cooldown covering
            // the cycle, hosted in a fixed solid.
            void CheckTurret(string id, SoloRoomElement e, SoloRoomElement[] solids, List<string> list)
            {
                TurretSettings k = e.Settings.Turret;
                if (!k.IsConfigured) { list.Add($"{id}: {e.Name} is a turret with no turret settings (PAX-121)."); return; }
                if (k.LockTicks < MinTurretLock) list.Add($"{id}: {e.Name} locks for {k.LockTicks} ticks; at least {MinTurretLock}, its tell (D-057, PAX-121).");
                if (k.AimTicks < 1 || k.FireTicks < 1) list.Add($"{id}: {e.Name} aims {k.AimTicks} and fires {k.FireTicks} ticks; at least 1 each (PAX-121).");
                if (e.Settings.RepeatMode != TrapRepeatMode.Periodic) list.Add($"{id}: {e.Name} isn't Periodic; a turret keeps its own cycle (PAX-121).");
                if (e.Settings.CooldownTicks < k.Cycle) list.Add($"{id}: {e.Name}'s cooldown {e.Settings.CooldownTicks} is below its cycle {k.Cycle} (PAX-121).");
                Rect b = Box(e, Vector2.zero);
                if (!solids.Any(f => Box(f, Vector2.zero).Overlaps(b))) list.Add($"{id}: {e.Name} {Describe(b)} isn't hosted in a fixed solid (PAX-121).");
                if (k.AimZone.width < 0f || k.AimZone.height < 0f) list.Add($"{id}: {e.Name}'s aim zone {k.AimZone} has a negative size (D-203).");
                // D-203: a ceiling turret's muzzle hangs just under its host; inside it, its beam would reach through to the floor above.
                if (k.CeilingMount && solids.Any(f => Box(f, Vector2.zero).Contains(e.Position)))
                    list.Add($"{id}: {e.Name} is ceiling-mounted with its muzzle {e.Position} inside a fixed solid (D-203).");
            }

            void CheckPath(string id, SoloRoomDefinition r, SoloRoomElement e, SoloRoomElement[] solids, List<string> list)
            {
                PathSettings p = e.Settings.Path;
                if (!p.HasPoints) { list.Add($"{id}: {e.Name} has a path with no points (PAX-114)."); return; }
                Rect home = Box(e, Vector2.zero);
                foreach (Vector2 point in p.Points)
                {
                    Rect pose = Shift(home, point);
                    if (pose.xMin < -eps || pose.xMax > r.Width + eps) list.Add($"{id}: {e.Name}'s pose {Describe(pose)} leaves the room's x range [0, {r.Width:F2}] (PAX-114).");
                }
                foreach (Rect leg in MoverLegs(home, e))
                foreach (SoloRoomElement f in solids)
                    if (Overlap(leg, Box(f, Vector2.zero)) > eps)
                        list.Add($"{id}: {e.Name}'s {(p.Glitch ? "pose" : "path")} {Describe(leg)} runs into {f.Name}; a moving Solid's path overlaps no fixed geometry (D-056 (3), PAX-114).");
                if (!p.Glitch) return;
                if (p.WarnTicks < MinGlitchWarnTicks) list.Add($"{id}: {e.Name}'s glitch warning is {p.WarnTicks} ticks; it needs at least {MinGlitchWarnTicks} (D-057, PAX-114).");
                int n = p.Points.Length;
                if (e.Settings.MoveTicks / n <= p.WarnTicks) list.Add($"{id}: {e.Name}'s glitch step ({e.Settings.MoveTicks} / {n} points) is no longer than its warning {p.WarnTicks} (PAX-114).");
                if (e.Settings.ReturnTicks > 0 && e.Settings.ReturnTicks / n <= p.WarnTicks) list.Add($"{id}: {e.Name}'s glitch return step ({e.Settings.ReturnTicks} / {n} points) is no longer than its warning {p.WarnTicks} (PAX-114).");
            }

            // PAX-122 (D-201): a Solid on Periodic timing that only appears and goes (no path, move, hinge or flip); its flicker is a
            // tell (>= MinPhaseWarnTicks); it is there long enough to land on and leave (its flicker plus PhaseStandTicks) and gone a
            // while every period; its timing's effect lasts its time there; where it appears overlaps no fixed solid.
            void CheckPhase(string id, SoloRoomElement e, SoloRoomElement[] solids, List<string> list)
            {
                SoloRoomTrapSettings s = e.Settings;
                MovingFloorSettings f = s.Floor;
                if (s.MovingKind != MovingTrapKind.Solid) list.Add($"{id}: {e.Name} is a phase platform but a {s.MovingKind} mover; a phase platform is a Solid (PAX-122).");
                if (s.RepeatMode != TrapRepeatMode.Periodic) list.Add($"{id}: {e.Name} is a phase platform set to {s.RepeatMode}; it is Periodic (PAX-122).");
                if (f.Hinges || f.Flips || s.Path.IsConfigured || s.Offset != Vector2.zero) list.Add($"{id}: {e.Name} phases and also hinges, flips, follows a path or moves; a phase platform only appears and goes (PAX-122).");
                if (f.PhaseWarnTicks < MinPhaseWarnTicks) list.Add($"{id}: {e.Name}'s flicker is {f.PhaseWarnTicks} ticks; at least {MinPhaseWarnTicks}, its tell (D-057, PAX-122).");
                if (f.PhaseOnTicks < f.PhaseWarnTicks + PhaseStandTicks) list.Add($"{id}: {e.Name} is there {f.PhaseOnTicks} ticks; at least its flicker + {PhaseStandTicks} to land on and leave (PAX-122).");
                if (s.RepeatMode == TrapRepeatMode.Periodic && s.PeriodTicks - f.PhaseOnTicks < MinPhaseOffTicks) list.Add($"{id}: {e.Name} is gone {s.PeriodTicks - f.PhaseOnTicks} ticks a period; at least {MinPhaseOffTicks} (PAX-122).");
                if (s.CooldownTicks < f.PhaseOnTicks) list.Add($"{id}: {e.Name}'s cooldown {s.CooldownTicks} ends before its time there ({f.PhaseOnTicks}) (PAX-122).");
                foreach (SoloRoomElement o in solids)
                    if (Overlap(Box(e, Vector2.zero), Box(o, Vector2.zero)) > eps) list.Add($"{id}: {e.Name} appears inside {o.Name} (PAX-122).");
            }

            void CheckFlip(string id, SoloRoomElement e, SoloRoomElement[] solids, List<string> list)
            {
                SoloRoomTrapSettings s = e.Settings;
                if (s.MovingKind != MovingTrapKind.Solid) list.Add($"{id}: {e.Name} is a flip platform but a {s.MovingKind} mover; a flip platform is a Solid (PAX-115).");
                if (s.Floor.FlipTicks < MinFlipTicks) list.Add($"{id}: {e.Name} flips over {s.Floor.FlipTicks} ticks; at least {MinFlipTicks} (PAX-115).");
                if (s.Floor.FlipWarnTicks < MinFlipTicks) list.Add($"{id}: {e.Name}'s flip tell is {s.Floor.FlipWarnTicks} ticks; at least {MinFlipTicks} (D-057, PAX-115).");
                if (s.Floor.Hinges || s.Path.IsConfigured || s.Offset != Vector2.zero) list.Add($"{id}: {e.Name} flips and also hinges, follows a path or moves; a flip platform only turns (PAX-115).");
                int cycle = s.Floor.FlipWarnTicks + s.Floor.FlipTicks + s.HoldTicks + s.ReturnTicks;
                if (s.RepeatMode != TrapRepeatMode.Once && s.CooldownTicks < cycle)
                    list.Add($"{id}: {e.Name}'s cooldown {s.CooldownTicks} is below its tell + flip + hold + back ({cycle}) (PAX-115).");
                Rect disc = MovingFloorSettings.FlipDisc(Box(e, Vector2.zero));
                foreach (SoloRoomElement f in solids)
                    if (Overlap(disc, Box(f, Vector2.zero)) > eps)
                        list.Add($"{id}: {e.Name} turns through {Describe(disc)}, which runs into {f.Name} (PAX-115).");
            }

            void CheckSpinner(string id, SoloRoomDefinition r, SoloRoomElement e, SoloRoomElement[] solids, List<string> list)
            {
                SpinnerSettings k = e.Settings.Spinner;
                if (!k.IsConfigured) { list.Add($"{id}: {e.Name} is a spinner with no spinner settings (PAX-116)."); return; }
                if (k.Arms < 1 || k.Arms > 4) list.Add($"{id}: {e.Name} has {k.Arms} arms; 1-4 (PAX-116).");
                if (k.ArmLength <= 0f) list.Add($"{id}: {e.Name}'s arm length {k.ArmLength:F2} isn't positive (PAX-116).");
                if (k.ArmWidth < MinArmWidth - eps || k.ArmWidth > MaxArmWidth + eps) list.Add($"{id}: {e.Name}'s arm width {k.ArmWidth:F2} is outside [{MinArmWidth}, {MaxArmWidth}] (PAX-116).");
                if (Mathf.Abs(k.DegreesPerTick) <= eps && !k.Swings) list.Add($"{id}: {e.Name} doesn't turn or swing (PAX-116, PAX-118).");
                if (k.TipSpeed > MaxTipSpeed + eps) list.Add($"{id}: {e.Name}'s tip moves {k.TipSpeed:F2} u a tick; at most {MaxTipSpeed} (PAX-116).");
                Rect disc = SpinnerDisc(e, Vector2.zero);
                if (disc.xMin < -eps || disc.xMax > r.Width + eps) list.Add($"{id}: {e.Name}'s disc {Describe(disc)} leaves the room's x range [0, {r.Width:F2}] (PAX-116).");
                // The arms are a circle: a solid's box is clear when its nearest point is outside the arm's reach. PAX-118: a pendulum's
                // swept arc is its box.
                float reach = k.ArmLength + k.ArmWidth * .5f;
                if (k.Swings)
                {
                    foreach (SoloRoomElement f in solids) if (Overlap(disc, Box(f, Vector2.zero)) > eps) list.Add($"{id}: {e.Name}'s swing {Describe(disc)} sweeps into {f.Name} (PAX-118).");
                    return;
                }
                foreach (SoloRoomElement f in solids)
                {
                    Rect b = Box(f, Vector2.zero);
                    Vector2 nearest = new(Mathf.Clamp(e.Position.x, b.xMin, b.xMax), Mathf.Clamp(e.Position.y, b.yMin, b.yMax));
                    if ((nearest - e.Position).magnitude < reach - eps) list.Add($"{id}: {e.Name}'s arms (reach {reach:F2}) sweep into {f.Name} (PAX-116).");
                }
                foreach (SoloRoomElement o in r.Elements.Where(o => o.Kind == SoloRoomElementKind.Door || o.Kind == SoloRoomElementKind.Checkpoint))
                {
                    Rect b = o.Kind == SoloRoomElementKind.Checkpoint ? new Rect(o.Position.x - .4f, o.Position.y, .8f, 1f) : Box(o, Vector2.zero);
                    Vector2 nearest = new(Mathf.Clamp(e.Position.x, b.xMin, b.xMax), Mathf.Clamp(e.Position.y, b.yMin, b.yMax));
                    if ((nearest - e.Position).magnitude < reach - eps) list.Add($"{id}: {e.Name}'s arms (reach {reach:F2}) sweep over {o.Name} (PAX-116).");
                }
            }
        }
    }
}
