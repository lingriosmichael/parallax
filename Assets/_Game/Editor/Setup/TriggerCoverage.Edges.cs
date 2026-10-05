using System.Collections.Generic;
using System.Linq;
using Parallax.Core;
using Parallax.Gameplay.Player;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Editor.Setup
{
    // PAX-091 (D-074, D-080): the ways up the approach search couldn't see, and closed pits.
    // - Stuck spears (D-086, Q2): a spear's stuck shaft is a floor, for every trap except those in the spear's chain
    //   family (its chain root and every trap chained from that root, the spear included). Static, so only stricter.
    // - Vines (D-089, Q3): the cat reaches a vine by grabbing it (grounded or airborne), climbs anywhere on it, and leaves by
    //   a leap (an ordinary jump: full rise, flat reach) or a release (a fall) from any height, the top stop included. Where
    //   the climbing cat's collider would touch the trigger, the vine is cut.
    // - Geysers (D-088, Q5): a cat on a vent's surface is launched through the vent's envelope (LaunchEnvelope) and reaches
    //   any surface or vine it can fall onto from the apex. Down vents launch a flipped cat off the ceiling, in rooms with a
    //   gravity flip. A launch whose path (straight up the column to the apex, then the fall to its landing) passes through
//   the trigger inside its band isn't taken.
    // - Closed pits (Q4): a drop is blocked where fixed solids (Floor, Wall, PitBottom, Ceiling) between the two heights
    //   cover every x the cat passes on its way to any landing on the lower surface. Collapsing floors and fake platforms
//   never block.
    // - PAX-093 (D-095): a Carry mover carries the cat between the ends of its path (both ways when it returns), unless its
    //   swept top passes through the trigger inside its band; and a drop is also blocked by a vertical wall: fixed solids
    //   spanning its full height between where the cat leaves and where it could land.
    // - PAX-095 (D-098): gravity-up vines and launches. In a room with a gravity flip, each vine also has gravity-up nodes:
    //   a cat keeps its gravity on a vine (D-089 (3), D-092), so a gravity-up cat grabs one from an underside and leaves it
    //   falling up onto an underside. Those edges are the gravity-down ones mirrored top to bottom. A cat climbing a vine
    //   inside an erupting column is launched as one standing on the vent is (the launch releases the climb).
    // Everything here only adds reach, except the closed pit and the vertical wall, which block only where no way down is left.
    static partial class TriggerCoverage
    {
        // ---------- stuck spears ----------

        static void AddStuckShafts(List<Piece> pieces, SoloRoomDefinition room, SoloRoomElement trap, Rect trigger, float low, float ceiling, float catHeight)
        {
            foreach (SoloRoomElement spear in room.Elements.Where(LevelLayoutValidator.IsSpear))
            {
                if (ChainFamily(room, ChainRoot(room, spear)).Contains(trap.Name)) continue;
                Rect shaft = LevelLayoutValidator.StuckShaftBox(spear, Vector2.zero);
                AddPiece(pieces, shaft.xMin, shaft.xMax, shaft.yMax, false, trigger, low, ceiling, catHeight, room.Width);
            }
        }

        static SoloRoomElement ChainRoot(SoloRoomDefinition room, SoloRoomElement e)
        {
            var seen = new HashSet<string> { e.Name };
            while (e.Settings.IsConfigured && e.Settings.TriggerSource == TrapTriggerSource.Chain)
            {
                string source = e.Settings.ChainSource;
                SoloRoomElement? parent = room.Elements.Where(x => x.Name == source).Cast<SoloRoomElement?>().FirstOrDefault();
                if (!parent.HasValue || !seen.Add(source)) break;
                e = parent.Value;
            }
            return e;
        }

        static HashSet<string> ChainFamily(SoloRoomDefinition room, SoloRoomElement root)
        {
            var family = new HashSet<string> { root.Name };
            var frontier = new Queue<string>(); frontier.Enqueue(root.Name);
            while (frontier.Count > 0)
            {
                string source = frontier.Dequeue();
                foreach (SoloRoomElement e in room.Elements)
                    if (e.Settings.IsConfigured && e.Settings.TriggerSource == TrapTriggerSource.Chain && e.Settings.ChainSource == source && family.Add(e.Name))
                        frontier.Enqueue(e.Name);
            }
            return family;
        }

        // ---------- vines ----------

        // One node per stretch of a vine the climbing cat can use: its collider [XMin, XMax] x [paws, paws + height], paws
        // from the vine's bottom (P0) to the top stop (P1), minus the paws where it would touch the trigger.
        static void AddVines(List<Piece> pieces, SoloRoomDefinition room, Rect trigger, float low, float ceiling, Reach reach)
        {
            bool flips = room.Elements.Any(e => e.Kind == SoloRoomElementKind.GravityFlip);
            foreach (SoloRoomElement v in room.Elements.Where(LevelLayoutValidator.IsVine))
            {
                AddVineNodes(pieces, Box(v), v.Position.x, trigger, low, ceiling, reach);
                // PAX-095: a gravity-up cat's nodes are the mirror image's: built on the mirrored vine and trigger, then mirrored back.
                if (flips)
                {
                    var mirrored = new List<Piece>();
                    AddVineNodes(mirrored, Mirror(Box(v)), v.Position.x, Mirror(trigger), -ceiling, -low, reach);
                    pieces.AddRange(mirrored.Select(Mirror));
                }
            }
        }

        static void AddVineNodes(List<Piece> pieces, Rect box, float centreX, Rect trigger, float low, float ceiling, Reach reach)
        {
            float x0 = centreX - reach.HalfWidth, x1 = centreX + reach.HalfWidth;
            var spans = new List<(float from, float to)> { (box.yMin, Mathf.Max(box.yMin, box.yMax - reach.Height)) };
            if (x1 > trigger.xMin + Epsilon && x0 < trigger.xMax - Epsilon)
            {
                float cut0 = trigger.yMin - reach.Height, cut1 = trigger.yMax;
                spans = spans.SelectMany(s => new[] { (s.from, Mathf.Min(s.to, cut0)), (Mathf.Max(s.from, cut1), s.to) }).Where(s => s.Item2 >= s.Item1 - Epsilon).ToList();
            }
            foreach ((float from, float to) in spans)
                pieces.Add(new Piece { XMin = x0, XMax = x1, Y = from, P0 = from, P1 = to, Vine = true, InBand = to >= low - Epsilon && from <= ceiling + Epsilon });
        }

        // A grab onto a vine from a surface, a leap or release from a vine onto a surface, or a leap from one vine to another.
        static bool Climbs(Piece a, Piece b, Rect trigger, float low, float ceiling, Reach reach, float width, Rect[] fixedSolids)
        {
            if ((!a.Vine && !b.Vine) || a.Wall || b.Wall) return false;
            // PAX-095: a grab or a leap keeps the cat's gravity; a gravity-up one is the gravity-down one mirrored.
            if (a.Up != b.Up) return false;
            if (a.Up) return Climbs(Mirror(a), Mirror(b), Mirror(trigger), -ceiling, -low, reach, width, fixedSolids.Select(Mirror).ToArray());
            if (!a.Vine) return Grabs(a, b, trigger, low, ceiling, reach, fixedSolids, width);
            foreach (float h in LeapHeights(a, b, trigger, reach))
            {
                var from = new Piece { XMin = a.XMin, XMax = a.XMax, Y = h };
                if (b.Vine ? Grabs(from, b, trigger, low, ceiling, reach, fixedSolids, width)
                    : Walks(from, b, trigger, low, ceiling, reach, width, fixedSolids) || Releases(from, b, trigger, reach, fixedSolids, width)) return true;
            }
            return false;
        }

        // The heights worth trying on vine a: its ends, b's height, and just clear of the trigger on either side.
        static IEnumerable<float> LeapHeights(Piece a, Piece b, Rect trigger, Reach reach) =>
            new[] { a.P0, a.P1, b.Y, b.Y - reach.Rise, trigger.yMax, trigger.yMin - reach.Height - reach.Rise }
                .Select(h => Mathf.Clamp(h, a.P0, a.P1)).Distinct();

        // From surface (or vine height) a, the cat's collider reaches vine b's climbing range: its easiest paw height is the
        // lowest whose collider still overlaps the range, and the vine counts as a surface that wide.
        // PAX-060 (L014 finding): with fixedSolids given (a grab from a surface, or a leap from another vine), a grab that drops
        // onto the vine is blocked as a walk or a drop is: by fixed solids that close off the drop between them (ClosedPit: a slab
        // over the vine) or by a wall the drop's full height (Walled). Before, a cat on a floor slab could grab a vine under it.
        static bool Grabs(Piece a, Piece b, Rect trigger, float low, float ceiling, Reach reach, Rect[] fixedSolids = null, float width = 0f)
        {
            float paws = b.P0 - reach.Height + 2f * Epsilon;
            if (a.Y >= paws && a.Y <= b.P1 + reach.Height - Epsilon) paws = a.Y;
            float gap = Mathf.Max(0f, Mathf.Max(b.XMin - a.XMax, a.XMin - b.XMax));
            float limit = reach.Gap(paws - a.Y);
            if (limit < 0f || gap > limit + Epsilon) return false;
            (float x0, float x1) = Stretch(a, b);
            var at = new Piece { XMin = b.XMin, XMax = b.XMax, Y = paws };
            if (Crosses(x0, x1, a, at, trigger, low, ceiling, reach)) return false;
            if (fixedSolids == null || paws >= a.Y - Epsilon) return true;
            if (ClosedPit(x0, x1, a, at, reach, fixedSolids, width)) return false;
            if (b.XMin >= a.XMax - Epsilon) return !Walled(a, at, a.XMax, 1, reach, fixedSolids);
            if (b.XMax <= a.XMin + Epsilon) return !Walled(a, at, a.XMin, -1, reach, fixedSolids);
            return true;
        }

        // Letting go at height a.Y: a fall, drifting at full speed, onto a lower surface b.
        static bool Releases(Piece a, Piece b, Rect trigger, Reach reach, Rect[] fixedSolids, float width)
        {
            if (b.Y > a.Y - Epsilon) return false;
            float gap = Mathf.Max(0f, Mathf.Max(b.XMin - a.XMax, a.XMin - b.XMax));
            if (gap > reach.Drift(a.Y - b.Y) + Epsilon) return false;
            (float x0, float x1) = Stretch(a, b);
            bool crosses = x1 > trigger.xMin + Epsilon && x0 < trigger.xMax - Epsilon && a.Y + reach.Height > trigger.yMin + Epsilon && b.Y < trigger.yMax - Epsilon;
            return !crosses && !ClosedPit(x0, x1, a, b, reach, fixedSolids, width);
        }

        // The horizontal stretch between two spans: the gap between them, or their overlap.
        static (float, float) Stretch(Piece a, Piece b)
        {
            if (b.XMin >= a.XMax - Epsilon) return (a.XMax, b.XMin);
            if (b.XMax <= a.XMin + Epsilon) return (b.XMax, a.XMin);
            return (Mathf.Max(a.XMin, b.XMin), Mathf.Min(a.XMax, b.XMax));
        }

        // ---------- geysers ----------

        readonly struct Vent
        {
            public readonly float X0, X1, ColumnX0, ColumnX1, FaceY, ColumnHeight; public readonly bool Down; public readonly Rect Envelope;
            public Vent(float x0, float x1, float columnX0, float columnX1, float faceY, bool down, Rect envelope, float columnHeight)
            { X0 = x0; X1 = x1; ColumnX0 = columnX0; ColumnX1 = columnX1; FaceY = faceY; Down = down; Envelope = envelope; ColumnHeight = columnHeight; }
        }

        // Up vents always; Down vents only in a room with a gravity flip (only a flipped cat stands under them).
        static Vent[] Vents(SoloRoomDefinition room, CatMotorConfig motor, float gravity)
        {
            bool flips = room.Elements.Any(e => e.Kind == SoloRoomElementKind.GravityFlip);
            var vents = new List<Vent>();
            foreach (SoloRoomElement e in room.Elements.Where(LevelLayoutValidator.IsGeyser))
            {
                GeyserSettings g = e.Settings.Geyser.Resolved;
                bool down = g.Direction == GeyserDirection.Down;
                if (down && !flips) continue;
                Rect vent = Box(e);
                float face = down ? vent.yMin : vent.yMax;
                vents.Add(new Vent(vent.xMin, vent.xMax, e.Position.x - g.ColumnWidth * .5f, e.Position.x + g.ColumnWidth * .5f, face, down,
                    LevelLayoutValidator.LaunchEnvelope(e, g, face, motor, gravity), g.ColumnHeight));
            }
            return vents.ToArray();
        }

        // From surface a, standing on a vent flush in it, to surface or vine b: b is at or below the apex (mirrored for a
        // Down vent) and within the fall's drift of the envelope, and neither the rise up the column nor the fall across to
        // b passes through the trigger in its band.
        // PAX-095: or from vine a, when the climbing cat's collider overlaps the column: launched with the same envelope.
        static bool Launches(Piece a, Piece b, Vent[] vents, Rect trigger, float low, float ceiling, Reach reach)
        {
            if (b.Up != a.Up || a.Wall || b.Wall) return false;
            foreach (Vent v in vents)
            {
                if (v.Down != a.Up) continue;
                if (a.Vine ? !InColumn(a, v, reach) : Mathf.Abs(v.FaceY - a.Y) > Epsilon || v.X1 <= a.XMin + Epsilon || v.X0 >= a.XMax - Epsilon) continue;
                Rect env = v.Envelope;
                float apex = v.Down ? env.yMin + reach.Height : env.yMax - reach.Height;
                float target = b.Vine ? (b.Up ? b.P1 + reach.Height : b.P0 - reach.Height) : b.Y;
                float fall = v.Down ? target - apex : apex - target;
                if (fall < -Epsilon) continue;
                float gap = Mathf.Max(0f, Mathf.Max(b.XMin - env.xMax, env.xMin - b.XMax));
                if (gap > reach.Drift(fall) + Epsilon) continue;
                Rect band = Rect.MinMaxRect(trigger.xMin, Mathf.Max(trigger.yMin, low), trigger.xMax, Mathf.Min(trigger.yMax, ceiling));
                var column = new Piece { XMin = v.ColumnX0 - reach.HalfWidth, XMax = v.ColumnX1 + reach.HalfWidth };
                (float x0, float x1) = Stretch(column, b);
                float top = v.Down ? apex - reach.Height : apex + reach.Height;
                bool rises = Overlaps(band, column.XMin, column.XMax, Mathf.Min(v.FaceY, top), Mathf.Max(v.FaceY, top));
                bool falls = Overlaps(band, x0, x1, Mathf.Min(target, top), Mathf.Max(target, top));
                if (!rises && !falls) return true;
            }
            return false;
        }

        // A climbing cat (vine node a: paws P0-P1 gravity down, its top P0-P1 gravity up) overlaps v's column somewhere.
        static bool InColumn(Piece a, Vent v, Reach reach)
        {
            float c0 = v.Down ? v.FaceY - v.ColumnHeight : v.FaceY, c1 = v.Down ? v.FaceY : v.FaceY + v.ColumnHeight;
            float y0 = a.Up ? a.P0 - reach.Height : a.P0, y1 = a.Up ? a.P1 : a.P1 + reach.Height;
            return a.XMax > v.ColumnX0 + Epsilon && a.XMin < v.ColumnX1 - Epsilon && y1 > c0 + Epsilon && y0 < c1 - Epsilon;
        }

        // ---------- PAX-095: the mirror (top to bottom) ----------

        // y -> -y: an underside becomes a top, a gravity-up vine node a gravity-down one (its top range becomes a paws range).
        static Piece Mirror(Piece p) => new Piece
        {
            XMin = p.XMin, XMax = p.XMax, Y = -p.Y, P0 = -p.P1, P1 = -p.P0, Up = !p.Up, InBand = p.InBand, Vine = p.Vine,
            Mover = p.Mover, MoverEnd = p.MoverEnd, MoverReturns = p.MoverReturns, MoverSweep = Mirror(p.MoverSweep),
        };

        static Rect Mirror(Rect r) => Rect.MinMaxRect(r.xMin, -r.yMax, r.xMax, -r.yMin);

        static bool Overlaps(Rect band, float x0, float x1, float y0, float y1) =>
            band.width > Epsilon && band.height > Epsilon && x1 > band.xMin + Epsilon && x0 < band.xMax - Epsilon && y1 > band.yMin + Epsilon && y0 < band.yMax - Epsilon;

        // ---------- PAX-093: movers ----------

        // A Carry MovingTrap Solid's pieces: its authored top is already a surface (IsSolid); its end pose's top is added. Both
        // are tagged with the mover, its swept top strip (the cat on it, start pose to end pose) and whether it comes back.
        static void AddMovers(List<Piece> pieces, SoloRoomDefinition room, Rect trigger, float low, float ceiling, float catHeight)
        {
            foreach (SoloRoomElement e in room.Elements)
            {
                if (e.Kind != SoloRoomElementKind.MovingTrap || e.Settings.MovingKind != MovingTrapKind.Solid || e.Settings.Floor.Motion != SurfaceMotion.Carry) continue;
                Vector2 offset = e.Settings.Offset;
                if (offset.sqrMagnitude < Epsilon * Epsilon) continue;
                Rect start = Box(e), end = start;
                end.position += offset;
                Rect sweep = Rect.MinMaxRect(Mathf.Min(start.xMin, end.xMin), Mathf.Min(start.yMax, end.yMax), Mathf.Max(start.xMax, end.xMax), Mathf.Max(start.yMax, end.yMax) + catHeight);
                bool returns = e.Settings.RepeatMode == TrapRepeatMode.Periodic || e.Settings.ReturnTicks > 0;
                foreach (Piece p in pieces.Where(p => !p.Up && !p.Vine && p.Mover == null && Mathf.Abs(p.Y - start.yMax) < Epsilon && p.XMin >= start.xMin - Epsilon && p.XMax <= start.xMax + Epsilon))
                { p.Mover = e.Name; p.MoverSweep = sweep; p.MoverReturns = returns; }
                int before = pieces.Count;
                AddPiece(pieces, end.xMin, end.xMax, end.yMax, false, trigger, low, ceiling, catHeight, room.Width);
                for (int i = before; i < pieces.Count; i++) { pieces[i].Mover = e.Name; pieces[i].MoverEnd = true; pieces[i].MoverSweep = sweep; pieces[i].MoverReturns = returns; }
            }
        }

        // Riding a Carry mover from one end of its path to the other (back only when it returns), unless its swept top passes
        // through the trigger inside its band.
        static bool Rides(Piece a, Piece b, Rect trigger, float low, float ceiling)
        {
            if (a.Mover == null || a.Mover != b.Mover || a.MoverEnd == b.MoverEnd || (a.MoverEnd && !a.MoverReturns)) return false;
            Rect band = Rect.MinMaxRect(trigger.xMin, Mathf.Max(trigger.yMin, low), trigger.xMax, Mathf.Min(trigger.yMax, ceiling));
            return !Overlaps(band, a.MoverSweep.xMin, a.MoverSweep.xMax, a.MoverSweep.yMin, a.MoverSweep.yMax);
        }

        // ---------- PAX-093: the vertical wall ----------

        // A drop leaving a's end at departX on `side` (-1: off its west end, +1: off its east end) starts with the cat's body just
        // off the edge and ends with its centre over b. Fixed solids whose union spans the whole drop height (between the two
        // surfaces) over some x are a wall the falling cat can't pass. The landing nearest the start is walled off when such a
        // wall overlaps the span from the start body to the landing body, and every farther landing is behind the same wall.
        static bool Walled(Piece a, Piece b, float departX, int side, Reach reach, Rect[] fixedSolids)
        {
            float drop = a.Up ? b.Y - a.Y : a.Y - b.Y;
            if (drop <= Epsilon) return false;
            float hw = reach.HalfWidth;
            float s0 = side < 0 ? departX - 2f * hw : departX, s1 = side < 0 ? departX : departX + 2f * hw;
            float landing = Mathf.Clamp((s0 + s1) * .5f, b.XMin, b.XMax);
            float h0 = Mathf.Min(s0, landing - hw), h1 = Mathf.Max(s1, landing + hw);
            float y0 = Mathf.Min(a.Y, b.Y), y1 = Mathf.Max(a.Y, b.Y);
            return FullHeightWalls(fixedSolids, y0, y1, h0, h1).Any(w => Mathf.Min(w.y, h1) - Mathf.Max(w.x, h0) > Epsilon);
        }

        // The x-intervals over [x0, x1] where fixed solids together cover y (y0, y1) from bottom to top.
        static IEnumerable<Vector2> FullHeightWalls(Rect[] fixedSolids, float y0, float y1, float x0, float x1)
        {
            Rect[] near = fixedSolids.Where(r => r.xMax > x0 && r.xMin < x1 && r.yMax > y0 + Epsilon && r.yMin < y1 - Epsilon).ToArray();
            var edges = new SortedSet<float> { x0, x1 };
            foreach (Rect r in near) { if (r.xMin > x0 && r.xMin < x1) edges.Add(r.xMin); if (r.xMax > x0 && r.xMax < x1) edges.Add(r.xMax); }
            float[] xs = edges.ToArray();
            for (int i = 0; i + 1 < xs.Length; i++)
            {
                float a = xs[i], b = xs[i + 1];
                if (b - a <= Epsilon) continue;
                float covered = y0;
                foreach (Rect r in near.Where(r => r.xMin <= a + Epsilon && r.xMax >= b - Epsilon).OrderBy(r => r.yMin))
                {
                    if (r.yMin > covered + Epsilon) break;
                    covered = Mathf.Max(covered, r.yMax);
                }
                if (covered >= y1 - Epsilon) yield return new Vector2(a, b);
            }
        }

        // ---------- closed pits ----------

        // A drop (to a lower surface; mirrored for undersides) leaving from the stretch [x0, x1] is blocked when fixed solids
        // between the two heights cover every x the cat passes on its way to any landing on b: from the stretch it leaves
        // (widened by the cat's half width) to the furthest point of b it can land on (a jump off the edge and the fall),
        // kept inside the room. The surface it drops from counts, since the cat can't fall back under it.
        static bool ClosedPit(float x0, float x1, Piece a, Piece b, Reach reach, Rect[] fixedSolids, float width)
        {
            float drop = a.Up ? b.Y - a.Y : a.Y - b.Y;
            if (drop <= Epsilon) return false;
            float d0 = Mathf.Min(x0, x1), d1 = Mathf.Max(x0, x1), r = Mathf.Max(0f, reach.Gap(-drop));
            float l0 = Mathf.Max(b.XMin, d0 - r), l1 = Mathf.Min(b.XMax, d1 + r);
            if (l1 < l0) return false;
            float from = Mathf.Max(0f, Mathf.Min(d0 - reach.HalfWidth, l0)), to = Mathf.Min(width, Mathf.Max(d1 + reach.HalfWidth, l1));
            // PAX-060 (L014 finding): every fixed solid that reaches into the drop's height band counts, not only those whose top
            // lies in it: a floor block beside a trough rises above the trough's floor and still closes a drop off its end.
            var layer = fixedSolids.Where(r2 => a.Up ? r2.yMin < b.Y - Epsilon && r2.yMax > a.Y + Epsilon : r2.yMax > b.Y + Epsilon && r2.yMin < a.Y - Epsilon)
                .Where(r2 => r2.xMax > from && r2.xMin < to).OrderBy(r2 => r2.xMin);
            float covered = from;
            foreach (Rect r2 in layer)
            {
                if (r2.xMin > covered + Epsilon) return false;
                covered = Mathf.Max(covered, r2.xMax);
                if (covered >= to - Epsilon) return true;
            }
            return false;
        }

        // ---------- PAX-105 (D-110): wall faces ----------

        // Every solid a cat can cling to: grip walls only (D-110 amendment 2). Faces under the motor's MinClingFaceHeight are
        // left out where they're used.
        internal static IEnumerable<(string name, Rect box)> ClingSolids(SoloRoomDefinition room)
        {
            foreach (SoloRoomElement e in room.Elements)
                if (e.Kind == SoloRoomElementKind.GripWall) yield return (e.Name, Box(e));
            foreach ((string name, Rect box) b in GripBlocksLanded(room)) yield return b;
        }

        /// <summary>D-110 amendment 3: every grip falling block at its landed pose (its authored box moved by its travel). It
        /// can only be grabbed once it has landed (a moving solid never latches), so the reach model takes it there, top
        /// included, from the start: more reach, never less.</summary>
        internal static IEnumerable<(string name, Rect box)> GripBlocksLanded(SoloRoomDefinition room)
        {
            foreach (SoloRoomElement e in room.Elements)
            {
                if (e.Kind != SoloRoomElementKind.FallingBlock || !e.Settings.Grip.IsConfigured) continue;
                Rect r = Box(e);
                r.y += (e.Settings.Direction == FallingBlockDirection.Up ? 1f : -1f) * e.Settings.TravelDistance;
                yield return (e.Name, r);
            }
        }

        // Everything that fills a clinging cat's column: every solid and the room's own side walls (x -1 to 0 and width to
        // width + 1, y -4 to 8, as SoloRoomBuilder builds them).
        static IEnumerable<(string name, Rect box)> ColumnBlockers(SoloRoomDefinition room)
        {
            foreach (SoloRoomElement e in room.Elements)
                if (IsSolid(e) || e.Kind == SoloRoomElementKind.Ceiling || (e.Kind == SoloRoomElementKind.FallingBlock && !e.Settings.Grip.IsConfigured)) yield return (e.Name, Box(e));
            foreach ((string name, Rect box) b in GripBlocksLanded(room)) yield return b;
            yield return ("Wall_Left", Rect.MinMaxRect(-1f, -4f, 0f, 8f));
            yield return ("Wall_Right", Rect.MinMaxRect(room.Width, -4f, room.Width + 1f, 8f));
        }

        // One node per stretch of a face the clinging cat can use: its collider beside the face, its paws from the face's
        // bottom to its top less half the cat (the face must span the collider's centre, where the side cast meets it), minus
        // where another solid fills the cat's column (it can't be there) and where its collider would touch the trigger.
        static void AddFaces(List<Piece> pieces, SoloRoomDefinition room, Rect trigger, WallJumpReach arc)
        {
            (string name, Rect box)[] solids = ColumnBlockers(room).ToArray();
            foreach ((string name, Rect r) in ClingSolids(room))
            {
                if (r.height < arc.MinFaceHeight - Epsilon) continue;
                foreach (int side in new[] { 1, -1 })
                {
                    float face = side > 0 ? r.xMin : r.xMax;
                    float x0 = side > 0 ? face - arc.Width : face, x1 = x0 + arc.Width;
                    if (x0 < -Epsilon || x1 > room.Width + Epsilon) continue;
                    var spans = new List<(float from, float to)> { (r.yMin - arc.HalfHeight, r.yMax - arc.HalfHeight) };
                    var blocks = solids.Where(s => s.name != name && s.box.xMax > x0 + Epsilon && s.box.xMin < x1 - Epsilon).Select(s => (s.box.yMin - arc.Height, s.box.yMax)).ToList();
                    if (trigger.width > 0f && trigger.xMax > x0 + Epsilon && trigger.xMin < x1 - Epsilon) blocks.Add((trigger.yMin - arc.Height, trigger.yMax));
                    foreach ((float b0, float b1) in blocks)
                        spans = spans.SelectMany(s => new[] { (s.from, Mathf.Min(s.to, b0)), (Mathf.Max(s.from, b1), s.to) }).Where(s => s.Item2 >= s.Item1 - Epsilon).ToList();
                    foreach ((float from, float to) in spans)
                        pieces.Add(new Piece { Wall = true, FaceSide = side, FaceX = face, Face = name + (side > 0 ? ":W" : ":E"), XMin = x0, XMax = x1, Y = from, P0 = from, PMax = to, P1 = float.NegativeInfinity });
                }
            }
        }

        // The highest paw height on face b the cat clings to from a (-infinity: none). From a surface (or a vine height: a leap,
        // an ordinary jump), the face is grabbed where the jump's envelope meets it (any height up to a jump above a, at most
        // the gap the jump reaches at that height: the highest that qualifies), as a vine is grabbed. From another face, the
        // wall jump's arc (WallJumpReach) from the highest point clung to: where the collider meets b's column, at the top of
        // the rise if it gets there rising. Never b itself (the face lock), never gravity up, never through the trigger.
        static float ClingHeight(Piece a, Piece b, Rect trigger, float low, float ceiling, Reach reach, WallJumpReach arc, float width, Rect[] fixedSolids)
        {
            if (arc == null || a.Up || b.Up || a == b) return float.NegativeInfinity;
            if (a.Wall)
            {
                if (float.IsNegativeInfinity(a.P1) || a.Face == b.Face) return float.NegativeInfinity;
                float best = float.NegativeInfinity, cx = (a.XMin + a.XMax) * .5f, bx = (b.XMin + b.XMax) * .5f;
                int away = -a.FaceSide;
                for (int k = 1; k < arc.Y.Length; k++)
                {
                    float near = cx + away * arc.XBack[k], far = cx + away * arc.XAway[k];
                    if (bx < Mathf.Min(near, far) - Epsilon || bx > Mathf.Max(near, far) + Epsilon) continue;
                    float y = a.P1 + (k < arc.ApexTick ? arc.Y[arc.ApexTick] : arc.Y[k]);
                    float h = Mathf.Min(b.PMax, y);
                    if (h < b.P0 - Epsilon || h <= best) continue;
                    if (!ArcCrosses(cx, bx, Mathf.Min(h, a.P1), a.P1 + arc.Apex + reach.Height, trigger, low, ceiling)) best = h;
                }
                return best;
            }
            if (a.Vine)
            {
                float best = float.NegativeInfinity;
                foreach (float h in new[] { a.P0, a.P1, b.P0, b.PMax - reach.Rise }.Select(h => Mathf.Clamp(h, a.P0, a.P1)).Distinct())
                    best = Mathf.Max(best, ClingFromSurface(new Piece { XMin = a.XMin, XMax = a.XMax, Y = h }, b, trigger, low, ceiling, reach, width, fixedSolids));
                return best;
            }
            return ClingFromSurface(a, b, trigger, low, ceiling, reach, width, fixedSolids);
        }

        static float ClingFromSurface(Piece a, Piece b, Rect trigger, float low, float ceiling, Reach reach, float width, Rect[] fixedSolids)
        {
            float gap = Mathf.Max(0f, Mathf.Max(b.XMin - a.XMax, a.XMin - b.XMax));
            float hi = Mathf.Min(b.PMax, a.Y + reach.Rise);
            if (hi < b.P0 - Epsilon) return float.NegativeInfinity;
            bool Fits(float h) { float limit = reach.Gap(h - a.Y); return limit >= 0f && gap <= limit + Epsilon; }
            float h0 = b.P0;
            if (!Fits(h0)) return float.NegativeInfinity;
            float best = hi;
            if (!Fits(hi))
            {
                // The gap a jump reaches only shrinks as the target rises: the highest that fits, by bisection.
                float lo = h0; best = hi;
                for (int i = 0; i < 40; i++) { float mid = (lo + best) * .5f; if (Fits(mid)) lo = mid; else best = mid; }
                best = lo;
            }
            (float x0, float x1) = Stretch(a, b);
            var at = new Piece { XMin = b.XMin, XMax = b.XMax, Y = best };
            if (Crosses(x0, x1, a, at, trigger, low, ceiling, reach)) return float.NegativeInfinity;
            if (best < a.Y - Epsilon)
            {
                if (ClosedPit(x0, x1, a, at, reach, fixedSolids, width)) return float.NegativeInfinity;
                if (b.XMin >= a.XMax - Epsilon && Walled(a, at, a.XMax, 1, reach, fixedSolids)) return float.NegativeInfinity;
                if (b.XMax <= a.XMin + Epsilon && Walled(a, at, a.XMin, -1, reach, fixedSolids)) return float.NegativeInfinity;
            }
            return best;
        }

        // From face a (clung to up to a.P1) onto surface or vine b: the wall jump's arc lands on b (with any input between the
        // stick held away and held back: the cat's collider overlaps b's span as it comes down through b's height), or grabs a
        // vine as a leap from that height would; or letting go (a push-away) drops the cat onto b.
        static bool JumpsOffWall(Piece a, Piece b, Rect trigger, float low, float ceiling, Reach reach, WallJumpReach arc, float width, Rect[] fixedSolids)
        {
            if (arc == null || !a.Wall || b.Wall || a.Up || b.Up || float.IsNegativeInfinity(a.P1)) return false;
            var from = new Piece { XMin = a.XMin, XMax = a.XMax, Y = a.P1 };
            if (b.Vine) return Grabs(from, b, trigger, low, ceiling, reach);
            if (Releases(from, b, trigger, reach, fixedSolids, width)) return true;
            float cx = (a.XMin + a.XMax) * .5f;
            int away = -a.FaceSide;
            // The capsule rides up onto a top whose corner it meets up to CornerAllowance below it.
            float corner = b.Y - arc.CornerAllowance;
            for (int k = 1; k < arc.Y.Length; k++)
            {
                float y0 = a.P1 + arc.Y[k - 1], y1 = a.P1 + arc.Y[k];
                if (!(y1 < y0 && y1 <= b.Y + Epsilon && y0 >= corner - Epsilon)) continue;   // coming down through [corner, top]
                float near = cx + away * arc.XBack[k], far = cx + away * arc.XAway[k];
                float c0 = Mathf.Min(near, far) - arc.HalfWidth, c1 = Mathf.Max(near, far) + arc.HalfWidth;
                if (c1 <= b.XMin + Epsilon || c0 >= b.XMax - Epsilon) continue;
                float landX = Mathf.Clamp(cx, b.XMin + arc.HalfWidth, b.XMax - arc.HalfWidth);
                if (!ArcCrosses(cx, landX, b.Y, a.P1 + arc.Apex + reach.Height, trigger, low, ceiling)) return true;
            }
            return false;
        }

        // A wall jump's path from the face (centre cx) to x, between heights y0 and y1, passes through the trigger in its band.
        static bool ArcCrosses(float cx, float x, float y0, float y1, Rect trigger, float low, float ceiling)
        {
            Rect band = Rect.MinMaxRect(trigger.xMin, Mathf.Max(trigger.yMin, low), trigger.xMax, Mathf.Min(trigger.yMax, ceiling));
            return Overlaps(band, Mathf.Min(cx, x) - .5f, Mathf.Max(cx, x) + .5f, y0, y1);
        }

        // ---------- PAX-105: the audit's questions (LevelLayoutValidator.WallJump.cs) ----------

        /// <summary>Whether the cat, from the checkpoint and without passing through `blocker` in band [low, ceiling], gets its
        /// collider into `target` (standing on a surface, on a vine, or clinging to a face), with or without wall jumps.</summary>
        internal static bool ReachesBox(SoloRoomDefinition room, Rect blocker, float low, float ceiling, Vector2 checkpoint, CatMotorConfig motor, float gravity, Rect target, bool walls)
        {
            if (motor == null || gravity <= 0f) return false;
            HashSet<Piece> reached = Reached(room, blocker, low, ceiling, checkpoint, motor, gravity, default, walls);
            if (reached == null) return false;
            float h = motor.ColliderSize.y;
            foreach (Piece p in reached)
            {
                Rect body = p.Wall ? Rect.MinMaxRect(p.XMin, p.P0, p.XMax, p.P1 + h)
                    : p.Vine ? Rect.MinMaxRect(p.XMin, p.P0, p.XMax, p.P1 + h)
                    : p.Up ? Rect.MinMaxRect(p.XMin, p.Y - h, p.XMax, p.Y) : Rect.MinMaxRect(p.XMin, p.Y, p.XMax, p.Y + h);
                if (body.Overlaps(target)) return true;
            }
            return false;
        }

        /// <summary>The highest the cat's collider top gets over the x-span [x0, x1] from below `underside` (gravity down; any
        /// way it gets there from the checkpoint, no trigger), with or without wall jumps: a jump from a surface or vine stretch
        /// under it, or a wall jump from a face (clung to below it) within a wall jump's reach of it.</summary>
        internal static float HighestTopOver(SoloRoomDefinition room, float x0, float x1, float underside, Vector2 checkpoint, CatMotorConfig motor, float gravity, bool walls)
        {
            if (motor == null || gravity <= 0f) return float.NegativeInfinity;
            var none = new Rect(-1e4f, -1e4f, 0f, 0f);
            HashSet<Piece> reached = Reached(room, none, float.NegativeInfinity, float.PositiveInfinity, checkpoint, motor, gravity, default, walls);
            if (reached == null) return float.NegativeInfinity;
            float top = float.NegativeInfinity, lift = motor.JumpHeight + motor.ColliderSize.y;
            WallJumpReach arc = walls ? new WallJumpReach(motor, gravity) : null;
            foreach (Piece p in reached.Where(p => !p.Up))
            {
                if (p.Wall)
                {
                    float h = Mathf.Min(p.P1, underside - arc.Height);
                    if (h < p.P0) continue;
                    float cx = (p.XMin + p.XMax) * .5f;
                    for (int k = 1; k < arc.Y.Length; k++)
                    {
                        float near = cx - p.FaceSide * arc.XBack[k], far = cx - p.FaceSide * arc.XAway[k];
                        if (Mathf.Max(near, far) + arc.HalfWidth > x0 && Mathf.Min(near, far) - arc.HalfWidth < x1) top = Mathf.Max(top, h + arc.Y[k] + arc.Height);
                    }
                }
                else if (p.XMax > x0 && p.XMin < x1)
                {
                    float paws = p.Vine ? Mathf.Min(p.P1, underside - motor.ColliderSize.y) : p.Y;
                    if (paws + motor.ColliderSize.y <= underside + Epsilon && (!p.Vine || paws >= p.P0)) top = Mathf.Max(top, paws + lift);
                }
            }
            return top;
        }

        /// <summary>PAX-105 (the audit's kinds (a) and (b)): for every overlap trap's trigger, what wall jumps add. (b) a side
        /// from which the cat now reaches the trigger's band, with a danger lying before the trigger's near edge seen from there
        /// (D-074's cut, with wall approaches); (a) the door, now reached without passing through the trigger.</summary>
        internal static void WallFindings(string levelId, SoloRoomDefinition room, CatMotorConfig motor, float gravity, List<string> findings)
        {
            if (motor == null || gravity <= 0f) return;
            Vector2 checkpoint = room.Elements.Where(e => e.Kind == SoloRoomElementKind.Checkpoint).Select(e => e.Position).DefaultIfEmpty(Vector2.zero).First();
            Rect[] doors = room.Elements.Where(e => e.Kind == SoloRoomElementKind.Door).Select(Box).ToArray();
            foreach (SoloRoomElement trap in room.Elements)
            {
                if (!IsOverlapTrap(trap) || trap.Settings.LearnedBypassReason != null || !TryTrigger(trap, out Rect trigger)) continue;
                float low = StoreyLow(room, trigger);
                if (!TryStoreyCeiling(room, trigger, low, out float ceiling)) continue;
                Side with = ApproachSides(room, trigger, low, ceiling, checkpoint, motor, gravity, trap, walls: true);
                Side without = ApproachSides(room, trigger, low, ceiling, checkpoint, motor, gravity, trap, walls: false);
                Side added = with & ~without;
                foreach (Side side in new[] { Side.Left, Side.Right })
                {
                    if ((added & side) == 0) continue;
                    float nearEdge = side == Side.Left ? trigger.xMin : trigger.xMax;
                    foreach ((string owner, Rect volume) in Dangers(room, trap))
                    {
                        bool beyond = side == Side.Left ? volume.xMin >= nearEdge - Epsilon : volume.xMax <= nearEdge + Epsilon;
                        if (!beyond) findings.Add($"(b) {levelId}: {trap.Name}: a wall jump reaches its band from the {(side == Side.Left ? "left" : "right")}, where {owner}'s danger {Describe(volume)} lies before the trigger's near edge x {nearEdge:F2}.");
                    }
                }
                if (LevelLayoutValidator.WallJumpDoorExemptions.ContainsKey(levelId + "/" + trap.Name)) continue;
                foreach (Rect door in doors)
                    if (ReachesBox(room, trigger, low, ceiling, checkpoint, motor, gravity, door, walls: true)
                        && !ReachesBox(room, trigger, low, ceiling, checkpoint, motor, gravity, door, walls: false))
                        findings.Add($"(a) {levelId}: {trap.Name}: wall jumps reach the door {Describe(door)} without passing through its trigger {Describe(trigger)}.");
            }
        }

        static Rect[] FixedSolids(SoloRoomDefinition room) =>
            room.Elements.Where(e => e.Kind == SoloRoomElementKind.Floor || e.Kind == SoloRoomElementKind.Wall || e.Kind == SoloRoomElementKind.GripWall
                || e.Kind == SoloRoomElementKind.PitBottom || e.Kind == SoloRoomElementKind.Ceiling).Select(Box).ToArray();
    }
}
