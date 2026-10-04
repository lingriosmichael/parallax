# PAX-V09 · The tiled skin's 1-px see-through column at pixel boundaries

**Status:** Logged 2026-10-04 (the developer's ruling on PAX-104 §7, option b). Not started; the plan below is a draft
for Phase 1.
**Order:** before PAX-A17, or as its first item.
**Wait after Phase 1:** post this plan and stop.
**Phase 1 size: Lite.**
**Depends on:** PAX-104 (the diagnosis). **Decisions:** D-085 (no tells: a disguised trap looks like its host until it
reveals), D-107 (art rulings).

## 1. Why

PAX-104 found that L010's Pit10_Cover (a FakePlatform x 3–6 wearing the world-tiled `ENV_Fill_A3` skin) draws one
semi-transparent column, 1 pixel wide, at world x = 4.0, exactly on a render-pixel boundary (pixel 128 at 128 px/u).
Drop_10, sinking behind it, shows through that column. The developer: this is a potential in-game tell (D-085: a moving
trap can be seen through the disguise), not only a test artifact.

Measured in PAX-104: half a pixel of camera shift removes the column; hiding Drop_10 removes the difference. Ruled
out: the texture's alpha (opaque; ASTC 2308², unpacked, Repeat, no mips), atlas packing, MSAA, the shard cuts (the
nearest at x 3.969), and a tile-wrap seam (tile 11.74 u). So the gap comes from how the skin is drawn: abutting quads (a
host's pieces, or a disguise's shards) whose edges meet on a pixel boundary, or the world-tile shader's UV/alpha at
that edge.

`TrapArtRevealFrameTests` no longer sees it: since PAX-104 it hides every other trap's bodies while it compares one
element's frames. This ticket needs its own check.

## 2. Scope

1. **Find every place it shows:** for each level (L001–L020) and the Trap Lab, every world-tiled host, disguise and
   shard edge; at each of the level camera's own poses (the poses `HazardContrastTests` uses, 4:3, 16:9 and 20:9),
   and at sub-pixel camera offsets, render the skinned surfaces over a contrasting backdrop and report every pixel whose
   alpha falls below opaque inside a surface that should be solid. Report: level, element, world x/y, pose.
2. **Find the cause** on the first case (Pit10_Cover): which edge, which quad, which shader path.
3. **Fix the skin drawing** so a solid surface is solid at any camera pose: e.g. overlap abutting quads by a fraction
   of a pixel, snap edges, or fix the shader's edge sampling. The choice is Phase 1's, with the numbers from 1–2.
4. **A permanent test:** the scan from 1, red on today's L010 Pit10_Cover first, green after the fix, on every level.

## 3. Tests

The new scan test (red first); `TrapArtRevealFrameTests`, `TrapArtParityTests`, `HazardContrastTests`,
`SavedSceneSyncTests`; the full suite only when the developer asks.

## 4. Allowed files (draft, confirmed in Phase 1)

- `Assets/_Game/Presentation/HostSkin.cs`, the trap art presenters that draw skinned bodies and shards
  (`Assets/_Game/Presentation/*Art.cs`).
- `Assets/_Game/Art/Traps/Shaders/TrapSpriteLitWorldTile.shader`, `Assets/_Game/Art/Shaders/EnvSpriteLitWorldUV.shader`.
- `Assets/_Game/Editor/Art/TrapArtSetup*.cs`, the environment skin setup if the host side needs it.
- The new test and its kit under `Assets/_Game/Tests/EditMode/`.
- The level rebuild and the Trap Lab through their setup menus.

## 5. Out of scope

Layouts, routes and trap timing (no moving Drop_10 to hide it); the biome work itself (PAX-A17); new Unity packages;
the reveal-frame test's rule (ruled in PAX-104).

## 6. Open questions

1. Should the scan also cover non-trap environment tiles (floors and walls drawn by the environment kit), or only skins
   a trap wears? Recommended: both, since a gap in a floor can show a hidden trap behind it too.
