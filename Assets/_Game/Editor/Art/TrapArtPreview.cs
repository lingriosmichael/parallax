using Parallax.Gameplay.Rooms;
using Parallax.Presentation;
using UnityEditor;
using UnityEngine;

namespace Parallax.Editor.Art
{
    /// <summary>PAX-A13 (§12 R8): re-skins the open scenes' fixed geometry with a textured tile (the A02 fill until the
    /// ENV-10 tiles land), and every disguised trap's art through the host-skin seam, so the contact sheet and the reveal
    /// frame test see a textured host. Used in throwaway scenes only (the route session's, the sheet's); never saved.</summary>
    public static class TrapArtPreview
    {
        public const string A02Fill = "Assets/_Game/Art/RealityA/Environment/A_GAME_Platform_Fill.png";

        /// <summary>Re-skins every fixed geometry renderer (a solid, non-trigger box with no trap, drawn Sliced) with `fill`,
        /// world-tiled (§12 R8: the way a host skin must be drawn for its disguised traps to match it at any cut), then points
        /// every disguised trap's art at its host's new skin. Returns the number of hosts re-skinned.</summary>
        public static int ReskinHosts(Sprite fill)
        {
            if (fill == null) { Debug.LogError("TrapArtPreview: no fill sprite."); return 0; }
            var config = AssetDatabase.LoadAssetAtPath<TrapArtConfig>(TrapArtSetup.ConfigPath);
            Material worldTile = config != null ? config.WorldTileMaterial : null;
            if (worldTile == null) { Debug.LogError("TrapArtPreview: no world-tile material; run PARALLAX/Art/Import Trap Kit."); return 0; }
            int count = 0;
            foreach (SpriteRenderer r in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                bool geometry = r.TryGetComponent(out BoxCollider2D box) && !box.isTrigger && !r.TryGetComponent(out RoomTrap _) && r.drawMode == SpriteDrawMode.Sliced;
                if (!geometry) continue;
                HostSkin.WorldTiledFor(r.name, fill, worldTile, Color.white, r.sortingLayerName).ApplyTo(r, r.size);
                count++;
            }
            foreach (TrapArt art in Object.FindObjectsByType<TrapArt>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (art is IHostSkinned skinned && skinned.HostSkin.IsSet)
                {
                    Transform host = FindHost(art, skinned.HostSkin.HostName);
                    if (host != null && host.TryGetComponent(out SpriteRenderer hostRenderer)) skinned.Reskin(HostSkin.Of(hostRenderer));
                }
            return count;
        }

        public static int ReskinHostsWithA02() => ReskinHosts(AssetDatabase.LoadAssetAtPath<Sprite>(A02Fill));

        static Transform FindHost(TrapArt art, string hostName)
        {
            // The art root sits under the room's root, beside the traps and the geometry.
            Transform room = art.transform.parent != null ? art.transform.parent.parent : null;
            return room != null ? room.Find(hostName) : null;
        }
    }
}
