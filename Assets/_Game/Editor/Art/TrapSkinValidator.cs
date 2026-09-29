using System.Collections.Generic;
using Parallax.Core.Presentation;
using Parallax.Presentation;
using UnityEngine;

namespace Parallax.Editor.Art
{
    /// <summary>PAX-A13 (§9.1, P10): ValidateTrapSkins. Every disguised trap's pre-reveal art draws exactly its host's skin:
    /// the skin recorded at build time still matches the host's renderer (so a stale build fails), the art draws that skin
    /// (sprite, material, colour, draw mode, sorting layer, tiling phase), and it covers exactly the grey-box's extent.
    /// Checked on a freshly built room, before any tick: collapsing floors and fake platforms, falling blocks, moving and
    /// shrinking floors, and disguised launchers.</summary>
    public static class TrapSkinValidator
    {
        /// <summary>The errors for `roomRoot`'s disguised trap art, and how many skins were checked.</summary>
        public static List<string> ValidateTrapSkins(string levelId, Transform roomRoot, out int checkedCount)
        {
            checkedCount = 0;
            var errors = new List<string>();
            foreach (CollapsingFloorArt art in roomRoot.GetComponentsInChildren<CollapsingFloorArt>(true))
            {
                checkedCount++;
                Check(levelId, roomRoot, art.name, art.HostSkin, art.Skin, art.Bodies[0].Greybox, errors);
                // R8: its shards must keep the host's pattern on the reveal tick, which only world tiling guarantees; a Tiled
                // or bordered Sliced host drawn from each shard's own corner would jump.
                HostSkin s = art.HostSkin;
                bool patterned = s.DrawMode == SpriteDrawMode.Tiled || (s.DrawMode == SpriteDrawMode.Sliced && s.Sprite != null && s.Sprite.border != Vector4.zero);
                if (s.IsSet && patterned && !s.WorldTiled)
                    errors.Add($"{levelId}: {art.name} breaks into shards on a patterned host '{s.HostName}' that isn't world-tiled (R8); give the host the {HostSkin.WorldTileShader} material");
            }
            foreach (SolidArt art in roomRoot.GetComponentsInChildren<SolidArt>(true))
            {
                checkedCount++;
                Check(levelId, roomRoot, art.name, art.HostSkin, art.Skin, art.Bodies[0].Greybox, errors);
            }
            foreach (ArrowArt art in roomRoot.GetComponentsInChildren<ArrowArt>(true))
            {
                if (!art.Disguised) continue;
                checkedCount++;
                Check(levelId, roomRoot, art.name, art.HostSkin, art.LauncherArt, art.Bodies[0].Greybox, errors);
            }
            return errors;
        }

        static void Check(string levelId, Transform roomRoot, string art, HostSkin skin, SpriteRenderer drawn, SpriteRenderer greybox, List<string> errors)
        {
            if (!skin.IsSet) { errors.Add($"{levelId}: {art} has no host skin recorded"); return; }
            Transform hostTransform = roomRoot.Find(skin.HostName);
            SpriteRenderer host = hostTransform != null ? hostTransform.GetComponent<SpriteRenderer>() : null;
            if (host == null) errors.Add($"{levelId}: {art}'s host '{skin.HostName}' isn't in the room");
            else if (skin.Difference(host) is string stale) errors.Add($"{levelId}: {art}'s recorded skin no longer matches its host '{skin.HostName}' ({stale}); rebuild the level");
            if (skin.Difference(drawn) is string d) errors.Add($"{levelId}: {art} doesn't draw its host's skin before its reveal: {d}");
            if (drawn != null && greybox != null)
            {
                Bounds a = drawn.bounds, g = greybox.bounds;
                if (Vector2.Distance(a.min, g.min) > TrapArtMath.BoundsTolerance || Vector2.Distance(a.max, g.max) > TrapArtMath.BoundsTolerance)
                    errors.Add($"{levelId}: {art} covers {a}, its grey-box {g}: a disguise must cover exactly the trap");
            }
        }
    }
}
