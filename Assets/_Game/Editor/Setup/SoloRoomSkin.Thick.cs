using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Parallax.Editor.Setup
{
    /// <summary>PAX-A16 round 2 (the developer: "make them thicker, with more vines and moss hanging"): which floors get the
    /// thick underside, and where its art must never go.</summary>
    public static partial class SoloRoomSkin
    {
        // ---------- the thick underside (A16 round 2) ----------

        /// <summary>How deep the thick underside's opaque part (stone band and cornice) reaches below the floor, and the clear
        /// height it must leave above whatever is below (a jump's apex 1.6 plus the cat's 0.56, and a margin), so a jumping
        /// cat never looks as if it passes through the drawn stone. Its ivy hangs behind the cat.</summary>
        public const float ThickSolidDepth = 0.95f, ThickHeadroom = 2.35f, ThickDepth = 2.05f;

        /// <summary>Where the cat never meets the thick underside's art: what the player has to see (hazards, lanes, the door,
        /// checkpoints, flip zones), climbable vines, a falling block's drop and a mover's sweep. Trigger boxes aren't drawn,
        /// so they don't count.</summary>
        public static List<Rect> VisualKeepOut(SoloRoomDefinition room)
        {
            List<Rect> list = KeepOut(room, false);
            foreach (SoloRoomElement e in room.Elements)
            {
                var r = new Rect(room.Origin + e.Position - e.Size * 0.5f, e.Size);
                switch (e.Kind)
                {
                    case SoloRoomElementKind.Vine: list.Add(Grow(r, 0.25f)); break;
                    case SoloRoomElementKind.FallingBlock:
                        list.Add(Grow(Rect.MinMaxRect(r.xMin, r.yMin - Mathf.Max(0f, e.Settings.TravelDistance), r.xMax, r.yMax), 0.2f)); break;
                    case SoloRoomElementKind.MovingTrap:
                    {
                        Rect moved = r; moved.position += e.Settings.Offset;
                        list.Add(Grow(Rect.MinMaxRect(Mathf.Min(r.xMin, moved.xMin), Mathf.Min(r.yMin, moved.yMin), Mathf.Max(r.xMax, moved.xMax), Mathf.Max(r.yMax, moved.yMax)), 0.2f));
                        break;
                    }
                }
            }
            return list;
        }

        /// <summary>True when this exposed bottom gets the thick underside: a floor or slab (real or disguised, P10; never a
        /// ceiling, wall, post or falling block, whose look is its host's) at least 1.5 u long, in a room without gravity flips
        /// (a flipped cat walks on undersides), with the headroom below and nothing the player must see in the way.</summary>
        public static bool ThickUnderFits(Edge e, List<Solid> solids, bool flips, List<Rect> visualKeepOut)
        {
            if (flips || e.Side != Side.Bottom || e.Length < 1.5f) return false;
            if (e.Owner.Kind is not (SoloRoomElementKind.Floor or SoloRoomElementKind.CollapsingFloor or SoloRoomElementKind.FakePlatform
                or SoloRoomElementKind.ShrinkingFloor or SoloRoomElementKind.MovingTrap)) return false;
            if (e.Owner.Shape is not (Shape.Block or Shape.Slab)) return false;
            if (ClearBelow(e, solids) < ThickSolidDepth + ThickHeadroom) return false;
            var r = Rect.MinMaxRect(e.From, e.Line - ThickDepth, e.To, e.Line);
            return !visualKeepOut.Any(k => k.Overlaps(r));
        }
    }
}
