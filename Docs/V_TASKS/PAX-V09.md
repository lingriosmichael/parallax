# PAX-V09 · The tiled skin's 1-px see-through column at pixel boundaries

**Status:** Done 2026-10-04: ruled (B) after the Phase 2 stop (§8), built and committed (§9).
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

## 7. Phase 1 (2026-10-04)

The developer said to proceed and didn't answer §6, so Phase 1 took the recommendation (cover the environment tiles too).

### The cause, measured

PAX-104 measured the column under `TrapArtRevealFrameTests`, and that test **re-skins every host with the A02 fill**
(`TrapArtPreview.ReskinHostsWithA02`) before it renders. The A02 fill (`A_GAME_Platform_Fill`, 394 px at 98.33 px/u)
is packed in `Atlas_RealityA_Env`. An atlased sprite gets a sub-rectangle `_UVRect`, and the world-tile shader wraps it
with `frac()` inside that rectangle (`WrapUV`). At the wrap, bilinear filtering samples half a texel past the
rectangle's edge into the atlas padding, which is transparent: a 1-px column about 30% see-through at every tile wrap. The
tile is 4.007 u, so the wrap falls at x ≈ 4.007, inside render pixel 128. (PAX-104 ruled out a wrap seam on the wrong
sprite, ENV_Fill_A3, with its 11.74 u tile.)

Probe (batch clone, L010 T7, tick 220, Pit10_Cover's skin rendered alone over a clear backdrop at 128 px/u, at camera
offsets of 0, ¼ and ½ px):

| Skin | Sprite | Columns not solid |
|---|---|---|
| Shipped | ENV_Fill_A3: whole 2308² texture, not packed, Repeat, `_UVRect` (0,0,1,1), tile 11.74 | none (at ½ px the camera sees past the skin's own right edge, not a gap) |
| Test re-skin | A02 fill: packed in a 1024² atlas, Clamp, `_UVRect` (0.39, 0, 0.38, 0.38), tile 4.007 | px 128, x 4.000, alpha 0.70 (the test's 77/255) |

### The shipped levels don't have it

- Every world-tiled trap skin saved in the 20 level scenes is a whole texture: 159 `HostSkin.UVRect` values are
  (0,0,1,1), and 54 are unset (not world-tiled). So the `frac()` path never runs in a shipped level.
- No skin sprite (ENV_Fill_*, ENV_Cap, ENV_Under) is in any sprite atlas. The atlases pack decor (moss, drapes, ferns,
  banners, glyphs), the door and checkpoint, and the trap bodies and effects.
- The environment's own world-UV shader (`EnvSpriteLitWorldUV`) has no `frac()` path; it relies on the texture's
  Repeat wrap, and its textures are fully opaque.

So today it isn't an in-game tell. It would become one the day a world-tiled sprite is atlased, for example a new tile
added for a biome in PAX-A17.

### Proposed scope (replaces §2)

1. **Fix the wrap for atlased sprites:** `HostSkin.WorldTiledFor` insets an atlased sprite's `_UVRect` by half a texel on
   each side, so `frac()` never samples outside the sprite. Whole textures are unchanged (they wrap by Repeat), so no
   saved scene, and nothing in the shipped levels, changes. No shader edit.
2. **A guard test** (`WorldTileSkinTests`, static, no rendering): in every level scene and the Trap Lab, every renderer
   drawn with either world-tile shader (trap skins and environment) uses either a whole, unpacked texture with Repeat
   wrap, or an atlased sprite whose `_UVRect` is inset. It's red first on a fixture with an atlased sprite and the old
   rectangle.
3. **A render check on the A02 case:** the probe above becomes a test: an atlased world-tiled skin rendered alone over a
   clear backdrop is solid at camera offsets of 0, ¼ and ½ px (red today at px 128, green after 1).
4. **Dropped:** the render scan of every level at every camera pose (§2.1). The static guard covers the cause, and the
   shipped skin measured solid.

`TrapArtRevealFrameTests` keeps PAX-104's rule (other traps hidden). With 1 in place it should also pass without that
rule (measured in Phase 2, not yet); whether to go back is your call (recommended: keep it, since the test checks one element's disguise).

**Allowed files (revised):** `Assets/_Game/Presentation/HostSkin.cs`; the new `Assets/_Game/Tests/EditMode/WorldTileSkinTests.cs`;
this ticket. No scene, asset or shader changes. Tests in Phase 2: the new fixture, `TrapArtRevealFrameTests`,
`TrapArtParityTests`, `HazardContrastTests` (unaffected, since the shipped skins are whole textures).

### Questions

1. Accept the smaller scope (1–3, drop the full render scan)?
2. Keep `TrapArtRevealFrameTests`' PAX-104 rule after the fix (recommended), or go back to rendering other traps?
3. PAX-A17 step 0 stays as is (V09 first, now small), or should V09 fold into A17's first step?

## 8. Phase 2: stopped (2026-10-04)

The developer's rulings: the smaller scope (half-texel inset, `WorldTileSkinTests` with the environment tiles and
"atlased tiled sprites have mipmaps off", the render check at 0, ¼ and ½ px); `TrapArtRevealFrameTests` renders the
shipped skins (no A02 re-skin) and keeps hiding other traps; V09 its own ticket before PAX-A17; PAX-104 §7 corrected in
this commit.

**Built (batch clone, uncommitted):**
- `HostSkin.WrapRect` (the half-texel inset for an atlased sprite; whole textures unchanged), used by `WorldTiledFor`.
- `WorldTileSkinTests`: the inset (unit), the guard's own check, a guard over the 20 level scenes and the Trap Lab
  (every trap host skin, every rest-pose tile, every renderer on either world shader: on the world-tile shader an
  atlased sprite needs the inset and no mipmaps; on the environment's world-UV shader, materials anchored to the world
  (`_Axis` 0–2) need a whole texture that repeats on those axes, while `_Axis` 3, the dressing, has no wrap), and the render check.
- `TrapArtRevealFrameTests`: the shipped skins. Every compared skin in the saved scenes is kit stone (ENV_Fill_A, A2,
  A3: world-tiled whole textures), because every room build applies `SoloRoomSkin.SkinFill` before the trap art copies
  it. The A02 re-skin dates from before the ENV-10 tiles; nothing needs it now. A compared skin that isn't world-tiled
  fails (a flat skin would pass anything).

**Red first** (`WrapRect` returning the old rectangle): 3 of 25 failed (the inset, the guard check, the render check at
column 512, x ≈ 4.004, alpha 0.70); the 22 scene guards passed, environment included.

**The stop: the inset doesn't remove the column.** With the fix, the render check still fails (alpha 0.73). A sweep of
the inset (A02 fill: tight-packed, unrotated, ETC2, no mipmaps, bilinear):

| Anisotropic filtering | inset 0 | 0.5 (the fix) | 1 | 2 | 4 | 6 texels |
|---|---|---|---|---|---|---|
| Forced on (the Editor's quality, Ultra) | 0.70 | 0.73 | 0.77 | 0.83 | 0.96 | solid |
| Off | solid | solid | solid | solid | solid | solid |

So the cause is **anisotropic filtering across `frac()`'s jump**: between two neighbouring pixels at the wrap the UV leaps
across the whole sprite, the derivative is huge, and an anisotropic sampler takes taps along it, some of them in the atlas
padding. Bilinear alone (anisotropic off) showed no column even without the inset. The Editor runs at Ultra
(`anisotropicTextures` Forced On); Android's default quality, Medium, uses each texture's own setting, and the atlas's
`anisoLevel` is 1 (off). So on a phone at the default quality it probably wouldn't show (untested on a device).

**Options:**
- **(A) Sample with continuous gradients:** the world-tile shader samples with the derivatives of the unwrapped UV
  (`SAMPLE_TEXTURE2D_GRAD`) instead of the wrapped one, so neither anisotropic filtering nor mipmaps see the jump. This is the
  real fix for atlased tiles. It needs a local copy of URP's `CommonLitFragment` (it samples internally) in
  `TrapSpriteLitWorldTile.shader`, which isn't in the allowed files; about 30 lines of shader.
- **(B) Forbid atlased world-tiled sprites:** the guard requires a whole texture for every world-tiled draw (the shipped
  levels already comply), so `frac()` never runs in the game. `WrapRect`'s inset is then unproven: drop it, or keep it
  as harmless. The render check becomes "the A02 fill isn't allowed as a tile" (a guard case), not a render.
- (C) Turn off forced anisotropic filtering in the quality settings: a ProjectSettings change, and it hides rather
  than fixes. Not recommended.

Recommended: (B) now (cheap, and the shipped levels comply), with (A) only if PAX-A17 wants atlased tiles (e.g. for
build size).

## 9. As built: option (B) (2026-10-04)

The developer's ruling: (B). Remove the half-texel inset (it doesn't fix the column; no non-fix stays in place); every
tiled draw must use a whole repeating texture, an atlased tiled sprite fails the guard, and the render check becomes a
guard case; record (A) in PAX-A17's open items; correct PAX-104 §7.

- **`HostSkin.cs`:** unchanged (the inset was removed; the file is as committed before V09).
- **`WorldTileSkinTests`** (new):
  - `AnAtlasedSprite_FailsAsATile` (the former render check): the A02 fill, atlased, fails as a tile.
  - `AWholeRepeatingTexture_PassesAsATile`: `ENV_Fill_A3` passes; a part rectangle on a whole texture fails.
  - `EveryWorldTiledDraw_UsesAWholeRepeatingTexture`, per level scene and the Trap Lab: every trap host skin (whole
    texture, whole rectangle, repeating both ways), every rest-pose tile (`WorldTileSampling`), and every renderer on the
    world-tile shader or on a world-anchored environment material (`_Axis` 0–2; `_Axis` 3, the dressing, has no wrap).
    "Repeating" is per axis: a texture repeats along every axis the draw is longer than one tile. A trim one tile tall
    (cap, underside: Repeat/Clamp) or one tile wide (side, post: Clamp/Repeat) may clamp the other way, since it never
    wraps there. 3,357 draws checked, all pass.
- **`TrapArtRevealFrameTests`:** renders the shipped skins (no A02 re-skin). There's no remaining reason for the re-skin:
  every room build gives the hosts the kit's world-tiled stone (`SoloRoomSkin.SkinFill`) and the trap art copies it;
  every skin the test compares is ENV_Fill_A, A2 or A3 (146 in the levels, 21 in the Trap Lab). A compared skin that
  isn't world-tiled fails (a flat skin would pass anything). It keeps hiding the other traps (PAX-104).
- **Docs:** PAX-104 §7 corrected; (A) and the gauntlet's quality level recorded in PAX-A17's open items.
- **Tests** (batch clone, identical code): `WorldTileSkinTests` and `TrapArtRevealFrameTests`, 54 of 54. Not run:
  `TrapArtParityTests`, `HazardContrastTests` (no runtime code changed).
- **Left as is:** `TrapArtPreview.ReskinHostsWithA02` (the contact sheet's re-skin) still draws the atlased A02 fill, so
  the column can appear on a contact sheet rendered in the Editor. It's a preview tool, not the game.
