using System.Collections.Generic;
using System.Linq;
using Parallax.Editor.Art;
using Parallax.Gameplay.Rooms;
using Parallax.Presentation;
using UnityEditor;
using UnityEngine;

namespace Parallax.Editor.Setup
{
    /// <summary>D-248: gives each descending ceiling plate of the listed levels (L029's Ceiling_*) a CeilingFillArt: a sibling
    /// "&lt;name&gt;_Fill" drawing the room's stone (the fixed solids' fill sprite, world-UV material and tint) between the plate
    /// and its home, so the ceiling comes down filled. Run by LevelSetup after the room is built. D-249: the plugs too, and the
    /// block is the piece's whole look (its own art hidden).</summary>
    public static class CeilingFillSetup
    {
        // Each level's descending pieces. D-254: L030's Ceiling_F. D-256 (the developer, of D-255's slams coming down as columns of
        // stone: "Hate that look. Why is it so long. Just a portion of it should come off"): its slams are chunks of the roof again.
        static readonly Dictionary<string, string[]> Levels = new()
        {
            ["L029"] = new[] { "Ceiling_", "Plug_" },   // D-249: the plugs too
            ["L030"] = new[] { "Ceiling_" },
        };
        const string Suffix = "_Fill";

        public static void Wire(Transform roomsParent, string levelId, SoloRoomDefinition layout, List<string> changes)
        {
            if (levelId == null || !Levels.TryGetValue(levelId, out string[] prefixes)) return;
            Sprite sprite = SoloRoomSkin.FillSprite(layout);
            Material material = EnvironmentKit.TileMaterial("Fill");
            if (sprite == null || material == null) { Debug.LogError($"CeilingFillSetup: {levelId} has no fill sprite or material; its ceilings come down unfilled."); return; }
            foreach (MovingTrap trap in roomsParent.GetComponentsInChildren<MovingTrap>(true).Where(t => prefixes.Any(x => t.name.StartsWith(x, System.StringComparison.Ordinal)) && !t.name.EndsWith(Suffix, System.StringComparison.Ordinal)))
            {
                SoloRoomElement e = layout.Elements.FirstOrDefault(x => x.Name == trap.name);
                if (e.Name == null) { Debug.LogError($"CeilingFillSetup: {levelId} has no element '{trap.name}'."); continue; }
                Transform t = SetupUtility.EnsureChild(trap.transform.parent, trap.name + Suffix, trap.gameObject.layer, changes);
                t.position = trap.transform.position;
                SpriteRenderer r = SetupUtility.Ensure<SpriteRenderer>(t.gameObject, changes);
                SpriteRenderer grey = trap.GetComponent<SpriteRenderer>();
                r.sprite = sprite; r.sharedMaterial = material; r.color = SoloRoomSkin.Body;
                r.drawMode = SpriteDrawMode.Sliced; r.size = e.Size; r.enabled = true;
                // D-249: over the side walls' broken strips where it meets them.
                if (grey != null) { r.sortingLayerName = grey.sortingLayerName; r.sortingOrder = grey.sortingOrder + 5; }
                // D-249: the piece's own skin and trims (Art/<name>) are hidden; this block is its whole look.
                Transform skinArt = trap.transform.parent.Find(TrapArtSetup.ArtRootName)?.Find(trap.name);
                if (skinArt != null && skinArt.gameObject.activeSelf) { skinArt.gameObject.SetActive(false); changes.Add("hid art " + trap.name); }
                if (grey != null && grey.enabled) { grey.enabled = false; changes.Add("hid " + trap.name + "'s grey box"); }
                CeilingFillArt art = SetupUtility.Ensure<CeilingFillArt>(t.gameObject, changes);
                art.Configure(trap.transform, r, e.Size.x, e.Size.y * .5f);
                // D-300: a block alone ended in a ruler line under every slab in L029 and L030: the broken underside every other slab has.
                EnvironmentKit.Slot under = EnvironmentKit.Get("ENV_Under");
                Sprite underSprite = EnvironmentKit.Sprite("ENV_Under");
                Material underMaterial = EnvironmentKit.TileMaterial("Under");
                if (under != null && underSprite != null && underMaterial != null)
                {
                    Transform l = SetupUtility.EnsureChild(trap.transform.parent, trap.name + Suffix + "_Lip", trap.gameObject.layer, changes);
                    l.position = trap.transform.position;
                    SpriteRenderer lip = SetupUtility.Ensure<SpriteRenderer>(l.gameObject, changes);
                    lip.sprite = underSprite; lip.sharedMaterial = underMaterial; lip.color = SoloRoomSkin.Body;
                    lip.drawMode = SpriteDrawMode.Tiled; lip.size = new Vector2(e.Size.x, under.height);
                    lip.sortingLayerName = r.sortingLayerName; lip.sortingOrder = r.sortingOrder + 1;
                    art.ConfigureLip(lip, under.height * .5f - under.below);
                }
                // D-308 (the developer: "yes ceilings"): a piece's open ends are broken stone like every face (the block's straight
                // ends stood out past the broken ends of the floor above), growing with it down to its lip. A plug is hidden inside its
                // piece at home and closes a hole, so it has none.
                if (trap.name.StartsWith("Plug_", System.StringComparison.Ordinal)) art.ConfigureSlideGrow(EndPull);
                else BrokenSides(trap, e, layout, sprite, r, trap.transform.parent.Find(trap.name + Suffix + "_Lip")?.GetComponent<SpriteRenderer>(), art, under != null ? under.below : 0f, changes);
                EditorUtility.SetDirty(art);
            }
            HideCoveredUndersides(roomsParent, layout, prefixes, changes);
            changes.Add($"baked ceiling fills ({levelId})");
        }

        // How far a broken end pulls the block's straight end in, under the strip's solid stone (its notches reach about 0.12 u in).
        const float EndPull = .14f;

        static void BrokenSides(MovingTrap trap, SoloRoomElement e, SoloRoomDefinition layout, Sprite fill, SpriteRenderer block, SpriteRenderer lip, CeilingFillArt art, float below, List<string> changes)
        {
            if (!fill.name.StartsWith("ENV_Fill_", System.StringComparison.Ordinal)) return;
            string suffix = fill.name.Substring("ENV_Fill_".Length);
            // The strip is a mask over the world's fill (its own pixels are grey), as on every fixed face; the block is world-tiled fill too.
            Material mask = EnvironmentKit.TileMaterial(EnvironmentKit.EdgeKind("SideChip", suffix));
            if (mask == null) return;
            var renderers = new List<SpriteRenderer>();
            var offsets = new List<float>();
            float from = e.Position.x - e.Size.x * .5f, to = e.Position.x + e.Size.x * .5f;
            foreach (bool left in new[] { true, false })
            {
                float line = left ? e.Position.x - e.Size.x * .5f : e.Position.x + e.Size.x * .5f;
                // An end against the room's wall or other fixed stone shows no face.
                Vector2 beside = new(line + (left ? -.05f : .05f), e.Position.y);
                if (layout.Elements.Any(o => o.Kind is SoloRoomElementKind.Wall or SoloRoomElementKind.Floor or SoloRoomElementKind.Ceiling
                    && new Rect(o.Position - o.Size * .5f, o.Size).Contains(beside))) continue;
                // The broken strip every fixed face draws (its notches cut into the stone).
                int variant = (e.Name.Sum(ch => ch) + (left ? 0 : 1)) % 4;
                string slot = $"ENV_SideChip_{suffix}_{variant}";
                EnvironmentKit.Slot def = EnvironmentKit.Get(slot);
                Sprite sprite = EnvironmentKit.Sprite(slot);
                if (def == null || sprite == null) continue;
                Transform t = SetupUtility.EnsureChild(trap.transform.parent, trap.name + Suffix + (left ? "_SideL" : "_SideR"), trap.gameObject.layer, changes);
                SpriteRenderer side = SetupUtility.Ensure<SpriteRenderer>(t.gameObject, changes);
                // The kit's strips face left (their air side at x = 0).
                side.sprite = sprite; side.sharedMaterial = mask; side.color = SoloRoomSkin.Body; side.flipX = !left;
                side.drawMode = SpriteDrawMode.Tiled; side.size = new Vector2(def.width, e.Size.y + below);
                side.sortingLayerName = block.sortingLayerName; side.sortingOrder = block.sortingOrder + 2;
                float xMin = left ? line - def.outside : line + def.outside - def.width;
                offsets.Add(xMin + def.width * .5f - e.Position.x);
                t.position = new Vector3(xMin + def.width * .5f, e.Position.y, trap.transform.position.z);
                renderers.Add(side);
                if (left) from += EndPull; else to -= EndPull;
            }
            float width = to - from, shift = (from + to) * .5f - e.Position.x;
            block.size = new Vector2(width, block.size.y);
            if (lip != null) lip.size = new Vector2(width, lip.size.y);
            art.ConfigureSides(renderers.ToArray(), offsets.ToArray(), below, width, shift);
        }

        /// <summary>D-308: the floor above's own broken underside drew a dark seam across the ceiling hung flush under it, which
        /// always covers that line (its block reaches up to its home top); hidden over each piece.</summary>
        static void HideCoveredUndersides(Transform roomsParent, SoloRoomDefinition layout, string[] prefixes, List<string> changes)
        {
            var pieces = layout.Elements.Where(x => x.Kind == SoloRoomElementKind.MovingTrap && prefixes.Any(p => x.Name.StartsWith(p, System.StringComparison.Ordinal)) && !x.Name.StartsWith("Plug_", System.StringComparison.Ordinal))
                .Select(x => new Rect(x.Position - x.Size * .5f, x.Size)).ToList();
            foreach (SpriteRenderer under in roomsParent.GetComponentsInChildren<SpriteRenderer>(true).Where(u => u.name.StartsWith("Under_", System.StringComparison.Ordinal) && u.transform.parent != null && u.transform.parent.parent != null && u.transform.parent.parent.name == SoloRoomSkin.EnvRootName))
            {
                Bounds b = under.bounds;
                if (!pieces.Any(p => b.min.y < p.yMax && b.max.y > p.yMax && b.min.x >= p.xMin - .25f && b.max.x <= p.xMax + .25f)) continue;
                if (under.gameObject.activeSelf) { under.gameObject.SetActive(false); changes.Add("hid " + under.transform.parent.name + "/" + under.name + " over its ceiling"); }
            }
        }
    }
}
