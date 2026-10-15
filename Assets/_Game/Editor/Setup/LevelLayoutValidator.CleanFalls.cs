using System.Collections.Generic;
using System.Linq;
using Parallax.Gameplay.Rooms;
using UnityEngine;

namespace Parallax.Editor.Setup
{
    public static partial class LevelLayoutValidator
    {
        // ---------- PAX-107 (D-131): a falling piece leaves empty space ----------
        // The developer (2026-10-11): "some falling pieces still show the piece is there ... This needs to be a general rule and
        // always display clean visuals." A falling block that overlaps a fixed solid must leave a hole where it was: its host's
        // art is drawn around it (SoloRoomSkin.LeavesHole: a ceiling, or a slab's underside). A block in a floor's walkable top
        // would leave the floor's collider, and its stone, where it fell from: the floor is split around it instead (a slab
        // section, D-085 amendment), so the hole is real. Part of ValidateKit.
        public static List<string> ValidateCleanFalls(string levelId, SoloRoomDefinition room)
        {
            const float eps = 1e-3f;
            var errors = new List<string>();
            SoloRoomElement[] solids = room.Elements.Where(IsFixedSolid).ToArray();
            foreach (SoloRoomElement block in room.Elements.Where(e => e.Kind == SoloRoomElementKind.FallingBlock))
            {
                Rect b = Box(block, Vector2.zero);
                string[] hosts = solids.Where(f => Overlap(b, Box(f, Vector2.zero)) > eps).Select(f => f.Name).ToArray();
                if (hosts.Length == 0 || SoloRoomSkin.LeavesHole(room, block)) continue;
                errors.Add($"{levelId}: {block.Name} {Describe(b)} lies inside {string.Join(", ", hosts)}, whose art and collider stay where it fell from; split the floor around it, or hang it from a ceiling or a slab's underside (D-131).");
            }
            return errors;
        }
    }
}
