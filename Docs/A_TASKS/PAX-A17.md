# PAX-A17 · Shared biomes instead of 20 bespoke looks (plan, draft)

**Status:** Draft plan for the developer (2026-10-02). Not approved; no work started. The developer will send their own
list of the levels they like and don't, which may move levels between biomes.
**Follows:** PAX-A16 (checkpoint committed; D-107). **Decisions it would need:** a new one replacing the per-level
palettes and looks, and one amending D-105 for corner framing.

## 1. Why

Six rounds of PAX-A16 lifted the average from 4.62 to 5.44, but the critics' remaining notes are mostly about the
same thing: 20 looks, each tuned a little, none finished. Levels read as recolours of each other ("L011 is L002 regraded"),
and fixes land one level at a time. L002 is the one look the developer approved (D-103). The new approach is a few looks,
each finished to L002's standard, and shared.

## 2. The approach

- **3–4 biomes, each polished to L002's standard.** A biome is one sky plate set, one palette (light, haze, fog), one
  stone fill and trim set, one mid-distance set, and one dressing set (ferns, drapes, glyphs, banners). No per-level grade.
- **Each level = its biome + one signature element.** The signature is the level's own landmark (its tree, its
  aqueduct, its temple, its waterfalls), placed once by hand. It's the only per-level art.
- **Corner framing, screen corners only.** Dark, soft silhouettes (leaves, roots, broken masonry) in the four corners of
  the screen, never over walkable space or a hazard. A validator samples the camera's travel and drops a corner piece
  wherever it would overlap a walkable top's band, a hazard, a trigger or a reveal area (the D-083 camera tell
  footprints). This reverses part of D-105 ("nothing is drawn in front of the play layer"), so it needs a ruling.
  **Open question:** pinned to the screen (they move with the camera, which the developer disliked in the play-test), or
  fixed in the world at each room's corners (they scroll, so they sit in the screen corners only where the view allows)?
- **The loop works per biome, not per level.** One pilot level per biome goes through the critic loop against L002 and the
  concept; the biome is then applied to its other levels and checked with one shared pass.

## 3. Proposed biomes

| Biome | Look | Levels | Signature per level |
|---|---|---|---|
| **A · Golden Ruins** (L002's look) | warm low sun, sandstone, ivy, aqueducts and colonnades | L001, L002, L003, L004, L007, L011 | L001 colonnade · L002 aqueduct · L003 great tree · L004 ruined hall · L007 storm towers · L011 the climbing wall |
| **B · Sky Isles** | dusk rose to violet, floating islands, a cloud sea below the walks | L005, L008, L012, L014, L016 | L005 banner isles · L008 temple steps · L012 colonnade over the clouds · L014 chained bridges · L016 the sky beams |
| **C · Misty Falls** | cool morning, mist, waterfalls, jade canopy | L006, L013, L015, L017, L018 | L006 twin falls · L013 the fall chamber · L015 aqueduct pools · L017 twilight ruins · L018 the storm tree |
| **D · Ember Night** | night with a cool moon key, warm braziers and embers | L009, L010, L019, L020 | L009 ember field · L010 moonlit ruins · L019 the temple · L020 the aqueduct exam |

The order alternates biomes (A A A A B C A B D D | A B C B C B C C D D), so each biome appears by L010 and the last
level of each band is a night level. Pilots: L002 (A, already approved), L014 (B), L013 (C), L010 (D).

## 4. Open items folded in

- **The floor's evenly lit front face (all biomes):** one kit change to the play stone: a lit lip, a shadow band under
  it, and a value falloff down the face (about 30–40% darker at the base, cooler). Done once, before the biomes.
- **L013's sky (olive mud):** goes to Misty Falls, a cool grey-teal sky with a pale sun whose halo takes the sky's hue,
  so blue and yellow never mix into olive.
- **L018's grade (muddy grey-sepia):** goes to Misty Falls with storm weather as its signature. No per-level grade.
- **The repeated ferns in L017 and L018:** the dressing set gets at least three fern clumps per biome, with scale, flip
  and spacing rules (no two the same within a view, no even pitch). This may need new art (ENV-26 variants).
- **L010's moon and warm stone at night:** Ember Night lights the stone with a cool moon key and keeps warm light for
  braziers and the door. A painted moon (new art) sits behind the ruins, with a halo in the sky only.
- **Atlased tiles (PAX-V09, the developer's ruling (B)):** every world-tiled draw must use a whole repeating texture;
  `WorldTileSkinTests` fails an atlased tile. If a biome tile must be atlased (for build size, say), the fix is in the
  world-tile shader: sample with the gradients of the unwrapped UV (`SAMPLE_TEXTURE2D_GRAD`), so anisotropic filtering
  never sees `frac()`'s jump at the wrap, which made the 1-px see-through column. It needs a local copy of URP's
  `CommonLitFragment`. Only then, and the guard changes with it.
- **The gauntlet's captures render at Android's default quality level (Medium), with the 2D lights** (the developer,
  2026-10-04). The Editor runs at Ultra. For this 2D URP game the only difference that shows is anisotropic filtering:
  Ultra forces it on for every texture, Medium leaves it to each texture (the kit and atlases have it off). Both levels
  use the same URP asset, anti-aliasing (off), vSync, texture limit and DPI scaling. The rest of the differences are 3D
  settings with nothing to act on here (shadows, pixel lights, LOD bias, reflection probes, soft particles and
  vegetation, skin weights, billboards, GI CPU, particle raycast budget).

## 5. Steps (each a stop for the developer)

0. First: PAX-V09, the tiled skin's 1-px see-through column, committed before this ticket (the developer, 2026-10-04;
   ruled (B): tiles are whole repeating textures, guarded by `WorldTileSkinTests`).
1. Rule the biome list (this table, after the developer's like/dislike list) and the framing question.
2. The kit change for the floor face, checked on L002.
3. Biome by biome: build it, run the critic loop on its pilot, then apply it to its levels. A first, D last.
4. Corner framing, with its validator, once the biomes are in.
5. Remove the 20 bespoke palettes and looks from `LevelPalettes`/`LevelLooks`; each level keeps a biome id and its
   signature.

## 6. Out of scope

Gameplay, layouts, colliders, routes, trap timing and the camera (1.8×, D-104 amendment); the cat sprite and its
animation; new Unity packages.

## Palette rule (D-108, 2026-10-03)

The danger colour, crimson (hue about 353°), belongs to hazards only. No biome uses it: Ember Night's braziers and Golden
Ruins' gold stay amber/yellow (25–55°). `env_kit.py` moves any crimson pixel to amber as the kit is built, and
`test_hazard_readable.py` fails on any that's left; a biome's lights and tints keep out of the band too.

## Hazard lighting (the developer's ruling, 2026-10-04)

Hazards draw unlit since PAX-V08 (D-108). In the biome pilots, try "lit with a minimum brightness" for hazards (they take
the biome's light but never fall below a floor); keep it only if `HazardContrastTests` (3:1 on 30% of pixels, every level,
every camera pose) still passes, otherwise stay unlit.

