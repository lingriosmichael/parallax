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
                EditorUtility.SetDirty(art);
            }
            changes.Add($"baked ceiling fills ({levelId})");
        }
    }
}
