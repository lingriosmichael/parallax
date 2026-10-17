# Obsidian Void (Reality B) · environment asset prompts (2026-10-16)

**Scope:** docs only, for the co-op update (Reality B, D-047). Nothing here goes into Unity until a co-op art ticket approves it. The developer asked for these on 2026-10-16. The source folder is `Art_Source/Environment/ObsidianVoid/` (it was `B`).

**Why:** Reality B's look, from `Docs/00_VISION.md`: obsidian, glass, grids, cyan edge light, void, monoliths, wireframes, cold mist, stars. These prompts give it the same kit Golden Ruins (Reality A) has. The item numbers mirror Golden Ruins' `ENV-*` (`Art_Source/Environment/GoldenRuins/ENV_ASSET_LIST.md`), so the same tools (`Tools/Art/env_kit.py`) can cut them.

**Not needed (code makes them):** ends, corners and broken edges (W3 cuts them from the fill, `env_bumpy.py`). Also not needed: pillar shafts, caps and bases, because narrow uprights are drawn as stone blocks (2026-10-16).

**Open:** the cats in this reality. The play-layer stone is kept mid-dark slate so a black cat reads against it. If Reality B's cat is a different colour, check its contrast against OV-10a before painting the rest.

## How to work

- **Chat:** use one ChatGPT chat. Paint **OV-01 (sky) first**, then **OV-10a (fill)**, and attach both to every later prompt that names them, so light and stone match.
- **Each prompt:** attach the references listed, paste the whole prompt, then check the reject list. Regenerate rather than fixing by hand.
- **Transparency:** "Transparent" means real alpha. If ChatGPT paints a checkerboard, regenerate, or ask for a flat pure magenta #FF00FF background. "On pure black" pieces are glows: the tools turn black into transparency.
- **Phone check:** shrink each result to phone size on screen before saving; the big shapes must still read.
- **Saving:** save to `Art_Source/Environment/ObsidianVoid/` with the exact file name. The `OV-` prefix keeps them apart from `PAX-A18_BACKGROUND_PROMPTS.md`, which uses B, C, D and E as biome letters.

**References:**
- **R1:** `Docs/Art/Reference/styleframe_A_v1.png` (technique only)
- **R2:** `DesignImages/14_environment-breakdown.png` (depth)
- **R3:** `Art_Source/Environment/GoldenRuins/A_GAME_Platform_Fill__raw_v1.png` (block size)
- **R4:** `Art_Source/Environment/ObsidianVoid/B_MG_01_Mid__raw_v1.png` (the old placeholder skyline: content only, not quality)
- **R5:** `Art_Source/Environment/ObsidianVoid/B_GAME_Platform_Fill__raw_v1.png` (the old placeholder fill: value and palette)
- **R6:** `Docs/Art/Reference/B_Door_Exit.png` (the old door: footprint)
- **OV-01 and OV-10a:** once painted, attach them where an item names them.

---

## Sky and background

### 1 · OV-01 · Sky plate: the void

**Save as:** `Art_Source/Environment/ObsidianVoid/OV-01_sky_v1.png`, landscape 3:2, opaque. **Attach:** R1, R2, R4.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their warm palette. This place is the OBSIDIAN VOID, the game's second reality: a mirror world of the ruins, built of black volcanic glass and polished obsidian monoliths floating in a starry void, thin cyan light running along their edges and through engraved grid lines, glass and crystal growths, cold mist, faint wireframe structures. Light: cold. A pale blue-white star glow is the key light; every accent light is cyan. Shadows are deep navy, never pure black.

Paint the furthest-back layer: a deep starry void. A dark navy-to-indigo gradient, darkest at the top, with a soft cold nebula band across the middle third, thousands of fine stars of different sizes, a few brighter ones with tiny cross glints. Calm and quiet: it sits behind everything. Leave the lower third slightly lighter (cold mist far below).

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector, not a neon sci-fi interface. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red, crimson, orange or gold.
```

**Reject if:** too bright; a planet or moon disc (the light source is its own piece); purple or pink nebula; stars too large or evenly spaced.

### 2 · OV-02 · Key light: the pale star

**Save as:** `Art_Source/Environment/ObsidianVoid/OV-02_star_v1.png`, square 1:1, on pure black (the game turns black into transparency). **Attach:** R1, OV-01.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their warm palette. This place is the OBSIDIAN VOID, the game's second reality: a mirror world of the ruins, built of black volcanic glass and polished obsidian monoliths floating in a starry void, thin cyan light running along their edges and through engraved grid lines, glass and crystal growths, cold mist, faint wireframe structures. Light: cold. A pale blue-white star glow is the key light; every accent light is cyan. Shadows are deep navy, never pure black. Match the attached Obsidian Void sky exactly in light and palette.

Paint the void's key light on a pure black background: a small pale blue-white star with a soft round glow fading to black, a faint cyan halo ring and four long, very thin light rays. Centred, with plenty of black around it.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector, not a neon sci-fi interface. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red, crimson, orange or gold.
```

**Reject if:** a planet or moon; a hard-edged disc; anything not on pure black.

### 3 · OV-03a · Near mist band

**Save as:** `Art_Source/Environment/ObsidianVoid/OV-03a_mist_near_v1.png`, landscape 3:1, transparent background (real alpha; if you can't, a flat pure magenta #FF00FF background), tiles sideways. **Attach:** R1, OV-01.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their warm palette. This place is the OBSIDIAN VOID, the game's second reality: a mirror world of the ruins, built of black volcanic glass and polished obsidian monoliths floating in a starry void, thin cyan light running along their edges and through engraved grid lines, glass and crystal growths, cold mist, faint wireframe structures. Light: cold. A pale blue-white star glow is the key light; every accent light is cyan. Shadows are deep navy, never pure black. Match the attached Obsidian Void sky exactly in light and palette.

Paint a long horizontal band of cold drifting mist, near the camera: soft rolling wisps, pale blue-grey with faint cyan where the starlight catches it, denser in the middle of the band and fading to fully transparent above and below. Seamlessly tileable left to right.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector, not a neon sci-fi interface. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red, crimson, orange or gold.
```

**Reject if:** hard edges; a visible seam; white instead of cold blue-grey.

### 4 · OV-03b · Mid mist band

**Save as:** `Art_Source/Environment/ObsidianVoid/OV-03b_mist_mid_v1.png`, landscape 3:1, transparent background (real alpha; if you can't, a flat pure magenta #FF00FF background), tiles sideways. **Attach:** R1, OV-01.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their warm palette. This place is the OBSIDIAN VOID, the game's second reality: a mirror world of the ruins, built of black volcanic glass and polished obsidian monoliths floating in a starry void, thin cyan light running along their edges and through engraved grid lines, glass and crystal growths, cold mist, faint wireframe structures. Light: cold. A pale blue-white star glow is the key light; every accent light is cyan. Shadows are deep navy, never pure black. Match the attached Obsidian Void sky exactly in light and palette.

As a near mist band but further away: thinner, softer, lower contrast, more navy, with a few faint star glints through it. Seamlessly tileable left to right; transparent above and below.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector, not a neon sci-fi interface. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red, crimson, orange or gold.
```

**Reject if:** as contrasty as the near band; a visible seam.

### 5 · OV-03c · Far mist band

**Save as:** `Art_Source/Environment/ObsidianVoid/OV-03c_mist_far_v1.png`, landscape 3:1, transparent background (real alpha; if you can't, a flat pure magenta #FF00FF background), tiles sideways. **Attach:** R1, OV-01.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their warm palette. This place is the OBSIDIAN VOID, the game's second reality: a mirror world of the ruins, built of black volcanic glass and polished obsidian monoliths floating in a starry void, thin cyan light running along their edges and through engraved grid lines, glass and crystal growths, cold mist, faint wireframe structures. Light: cold. A pale blue-white star glow is the key light; every accent light is cyan. Shadows are deep navy, never pure black. Match the attached Obsidian Void sky exactly in light and palette.

A very far, almost sky-coloured band of haze: barely visible navy mist with the faintest cyan, very low contrast, transparent above and below. Seamlessly tileable left to right.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector, not a neon sci-fi interface. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red, crimson, orange or gold.
```

**Reject if:** visible shapes; contrast.

### 6 · OV-04 · Far floating monoliths

**Save as:** `Art_Source/Environment/ObsidianVoid/OV-04_far_monoliths_v1.png`, landscape 3:2, transparent background (real alpha; if you can't, a flat pure magenta #FF00FF background). **Attach:** R1, R2, OV-01, R4.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their warm palette. This place is the OBSIDIAN VOID, the game's second reality: a mirror world of the ruins, built of black volcanic glass and polished obsidian monoliths floating in a starry void, thin cyan light running along their edges and through engraved grid lines, glass and crystal growths, cold mist, faint wireframe structures. Light: cold. A pale blue-white star glow is the key light; every accent light is cyan. Shadows are deep navy, never pure black. Match the attached Obsidian Void sky exactly in light and palette.

Paint FOUR separate distant floating islands of black glass, well spaced so each can be cut out: jagged inverted shards with one or two small obsidian monoliths on top, a thin cyan seam glowing along each monolith's edge, a few crystal shards drifting beneath. Far away, so hazy, low contrast, softened by cold mist.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector, not a neon sci-fi interface. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red, crimson, orange or gold.
```

**Reject if:** the islands touch; too much detail; warm light; one island repeated.

### 7 · OV-05 · Far architecture

**Save as:** `Art_Source/Environment/ObsidianVoid/OV-05_far_architecture_v1.png`, landscape 3:2, transparent background (real alpha; if you can't, a flat pure magenta #FF00FF background). **Attach:** R1, R2, OV-01, R4.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their warm palette. This place is the OBSIDIAN VOID, the game's second reality: a mirror world of the ruins, built of black volcanic glass and polished obsidian monoliths floating in a starry void, thin cyan light running along their edges and through engraved grid lines, glass and crystal growths, cold mist, faint wireframe structures. Light: cold. A pale blue-white star glow is the key light; every accent light is cyan. Shadows are deep navy, never pure black. Match the attached Obsidian Void sky exactly in light and palette.

Paint a sheet of SIX separate far-background structures, well spaced: two tall faceted obsidian spires with cyan rings near their tops, a long thin bridge of glass segments, two wireframe towers drawn as faint glowing cyan lines (half there, half void), and a broken arch of black monoliths. Far away: hazy, cold, low contrast.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector, not a neon sci-fi interface. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red, crimson, orange or gold.
```

**Reject if:** pieces overlap; neon brightness; detail that would flicker at phone size.

### 8 · OV-06 · Cold haze band

**Save as:** `Art_Source/Environment/ObsidianVoid/OV-06_haze_v1.png`, landscape 3:1, on pure black (light becomes transparency). **Attach:** R1, OV-01.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their warm palette. This place is the OBSIDIAN VOID, the game's second reality: a mirror world of the ruins, built of black volcanic glass and polished obsidian monoliths floating in a starry void, thin cyan light running along their edges and through engraved grid lines, glass and crystal growths, cold mist, faint wireframe structures. Light: cold. A pale blue-white star glow is the key light; every accent light is cyan. Shadows are deep navy, never pure black. Match the attached Obsidian Void sky exactly in light and palette.

Paint a soft horizontal band of cold light haze on pure black: a pale blue-cyan glow, brightest along the middle line and fading smoothly to black above and below, with gentle variation along its length. Seamlessly tileable left to right.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector, not a neon sci-fi interface. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red, crimson, orange or gold.
```

**Reject if:** hard edges; a seam; anything not on black.

### 9 · OV-07a · Mid architecture

**Save as:** `Art_Source/Environment/ObsidianVoid/OV-07a_mid_architecture_v1.png`, landscape 3:2, transparent background (real alpha; if you can't, a flat pure magenta #FF00FF background). **Attach:** R1, R2, OV-01, R4.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their warm palette. This place is the OBSIDIAN VOID, the game's second reality: a mirror world of the ruins, built of black volcanic glass and polished obsidian monoliths floating in a starry void, thin cyan light running along their edges and through engraved grid lines, glass and crystal growths, cold mist, faint wireframe structures. Light: cold. A pale blue-white star glow is the key light; every accent light is cyan. Shadows are deep navy, never pure black. Match the attached Obsidian Void sky exactly in light and palette.

Paint a sheet of FOUR separate mid-distance structures, well spaced: a tall obsidian gate of two monoliths and a lintel, cyan light along its inner edges; a stepped ziggurat of black glass blocks with engraved grid lines glowing faintly; a ring standing on its edge, made of glass segments; a leaning monolith split by a glowing crack. Rime and crystal growths at their feet. Mid distance: more detail than the far pieces, still soft.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector, not a neon sci-fi interface. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red, crimson, orange or gold.
```

**Reject if:** pieces overlap; flat neon outlines; warm light.

### 10 · OV-07b · Mid crystal growths

**Save as:** `Art_Source/Environment/ObsidianVoid/OV-07b_mid_crystals_v1.png`, landscape 3:2, transparent background (real alpha; if you can't, a flat pure magenta #FF00FF background). **Attach:** R1, R2, OV-01.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their warm palette. This place is the OBSIDIAN VOID, the game's second reality: a mirror world of the ruins, built of black volcanic glass and polished obsidian monoliths floating in a starry void, thin cyan light running along their edges and through engraved grid lines, glass and crystal growths, cold mist, faint wireframe structures. Light: cold. A pale blue-white star glow is the key light; every accent light is cyan. Shadows are deep navy, never pure black. Match the attached Obsidian Void sky exactly in light and palette.

Paint TWO separate mid-distance crystal growths, standing in for trees: tall clusters of dark smoked-glass crystals branching like trees, cyan light glowing faintly inside them, frost at their bases, a few shards floating near the tips. Well spaced, each on its own.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector, not a neon sci-fi interface. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red, crimson, orange or gold.
```

**Reject if:** pieces touch; bright saturated cyan; anything plant-like (no leaves).

### 11 · OV-07c · Bridge span (under floating floors)

**Save as:** `Art_Source/Environment/ObsidianVoid/OV-07c_span_v1.png`, landscape 3:1, transparent background (real alpha; if you can't, a flat pure magenta #FF00FF background), tiles sideways. **Attach:** R1, OV-01, OV-10a.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their warm palette. This place is the OBSIDIAN VOID, the game's second reality: a mirror world of the ruins, built of black volcanic glass and polished obsidian monoliths floating in a starry void, thin cyan light running along their edges and through engraved grid lines, glass and crystal growths, cold mist, faint wireframe structures. Light: cold. A pale blue-white star glow is the key light; every accent light is cyan. Shadows are deep navy, never pure black. Match the attached Obsidian Void sky exactly in light and palette.

Paint a long horizontal obsidian viaduct seen from the side: a row of tall narrow arches of black glass, a thin cyan line along the top of the span, the arches' undersides catching faint cyan light from below, rime on the ledges. Seamlessly tileable left to right; transparent above and below the span.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector, not a neon sci-fi interface. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red, crimson, orange or gold.
```

**Reject if:** a seam; perspective; the arches too wide (they read as a bridge, not a hall).

### 12 · OV-07d · Colonnade and towers

**Save as:** `Art_Source/Environment/ObsidianVoid/OV-07d_colonnade_v1.png`, landscape 3:1, transparent background (real alpha; if you can't, a flat pure magenta #FF00FF background). **Attach:** R1, R2, OV-01, R4.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their warm palette. This place is the OBSIDIAN VOID, the game's second reality: a mirror world of the ruins, built of black volcanic glass and polished obsidian monoliths floating in a starry void, thin cyan light running along their edges and through engraved grid lines, glass and crystal growths, cold mist, faint wireframe structures. Light: cold. A pale blue-white star glow is the key light; every accent light is cyan. Shadows are deep navy, never pure black. Match the attached Obsidian Void sky exactly in light and palette.

Paint a mid-distance colonnade of obsidian monolith pillars with two taller faceted towers rising behind it, cyan rings around the towers, a few pillars broken and floating out of line, cold mist at their feet. Transparent above.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector, not a neon sci-fi interface. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red, crimson, orange or gold.
```

**Reject if:** neon brightness; perspective; warm light.

### 13 · OV-41 · Far skyline band

**Save as:** `Art_Source/Environment/ObsidianVoid/OV-41_far_skyline_v1.png`, landscape 3:1, transparent background (real alpha; if you can't, a flat pure magenta #FF00FF background) above the skyline, tiles sideways. **Attach:** R1, R2, OV-01, R4.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their warm palette. This place is the OBSIDIAN VOID, the game's second reality: a mirror world of the ruins, built of black volcanic glass and polished obsidian monoliths floating in a starry void, thin cyan light running along their edges and through engraved grid lines, glass and crystal growths, cold mist, faint wireframe structures. Light: cold. A pale blue-white star glow is the key light; every accent light is cyan. Shadows are deep navy, never pure black. Match the attached Obsidian Void sky exactly in light and palette.

Paint a continuous distant skyline of the void city: obsidian spires, monolith blocks and wireframe towers of different heights in a single band along the bottom half, tiny cyan lights, soft cold haze. Very low contrast. Seamlessly tileable left to right; everything above the skyline transparent.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector, not a neon sci-fi interface. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red, crimson, orange or gold.
```

**Reject if:** a seam; a strong landmark that would repeat visibly; contrast.

### 14 · OV-32 · Back wall

**Save as:** `Art_Source/Environment/ObsidianVoid/OV-32_back_wall_v1.png`, landscape 3:1, opaque, tiles sideways. **Attach:** R1, OV-10a.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their warm palette. This place is the OBSIDIAN VOID, the game's second reality: a mirror world of the ruins, built of black volcanic glass and polished obsidian monoliths floating in a starry void, thin cyan light running along their edges and through engraved grid lines, glass and crystal growths, cold mist, faint wireframe structures. Light: cold. A pale blue-white star glow is the key light; every accent light is cyan. Shadows are deep navy, never pure black. Match the attached Obsidian Void sky exactly in light and palette.

Paint a long horizontal band of a recessed obsidian wall far behind the play area: large dark glass blocks in rows, faint engraved grid lines with a dim cyan glow in a few of them, soft cold light from above fading to navy shadow below. Low contrast: it sits behind gameplay. Seamlessly tileable left to right.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector, not a neon sci-fi interface. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red, crimson, orange or gold.
```

**Reject if:** a seam; contrast high enough to compete with the play layer; a standout feature.

---

## Play layer (what the cat walks on)

### 15 · OV-10a · Stone fill: obsidian blocks (tileable)

**Save as:** `Art_Source/Environment/ObsidianVoid/OV-10a_fill_v1.png`, square 1:1, opaque, seamless on all four sides. **Attach:** R1, R3, R5, OV-01.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their warm palette. This place is the OBSIDIAN VOID, the game's second reality: a mirror world of the ruins, built of black volcanic glass and polished obsidian monoliths floating in a starry void, thin cyan light running along their edges and through engraved grid lines, glass and crystal growths, cold mist, faint wireframe structures. Light: cold. A pale blue-white star glow is the key light; every accent light is cyan. Shadows are deep navy, never pure black. Match the attached Obsidian Void sky exactly in light and palette.

Paint a SEAMLESS TILEABLE square texture of the void's stone, seen straight on: large blocks of dark slate-grey obsidian about the size of the reference's blocks, glassy faces with soft cold reflections, thin joints between blocks, a faint engraved grid on a few blocks with the dimmest cyan glow in its lines. Mid-dark value, NOT black: a black cat must still stand out against it. LOW CONTRAST. No single standout feature that would visibly repeat.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector, not a neon sci-fi interface. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red, crimson, orange or gold.
```

**Reject if:** not seamless; too dark for a black cat to read; bright cyan lines (they flicker at phone size); a standout feature.

### 16 · OV-10a2 · Stone fill variant: fractured

**Save as:** `Art_Source/Environment/ObsidianVoid/OV-10a2_fill_fractured_v1.png`, square 1:1, opaque, seamless on all four sides. **Attach:** OV-10a, R1.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their warm palette. This place is the OBSIDIAN VOID, the game's second reality: a mirror world of the ruins, built of black volcanic glass and polished obsidian monoliths floating in a starry void, thin cyan light running along their edges and through engraved grid lines, glass and crystal growths, cold mist, faint wireframe structures. Light: cold. A pale blue-white star glow is the key light; every accent light is cyan. Shadows are deep navy, never pure black. Match the attached Obsidian Void sky exactly in light and palette.

The same obsidian block texture as the attached fill, same scale, value and palette, with fine glass fractures running across some blocks and a few chipped corners. Seamless on all four sides.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector, not a neon sci-fi interface. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red, crimson, orange or gold.
```

**Reject if:** a different scale or value from the attached fill; not seamless.

### 17 · OV-10a3 · Stone fill variant: rimed

**Save as:** `Art_Source/Environment/ObsidianVoid/OV-10a3_fill_rimed_v1.png`, square 1:1, opaque, seamless on all four sides. **Attach:** OV-10a, R1.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their warm palette. This place is the OBSIDIAN VOID, the game's second reality: a mirror world of the ruins, built of black volcanic glass and polished obsidian monoliths floating in a starry void, thin cyan light running along their edges and through engraved grid lines, glass and crystal growths, cold mist, faint wireframe structures. Light: cold. A pale blue-white star glow is the key light; every accent light is cyan. Shadows are deep navy, never pure black. Match the attached Obsidian Void sky exactly in light and palette.

The same obsidian block texture as the attached fill, same scale, value and palette, with pale frost rime gathered in the joints and on the upper edges of some blocks. Seamless on all four sides.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector, not a neon sci-fi interface. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red, crimson, orange or gold.
```

**Reject if:** a different scale or value; white snow instead of thin rime; not seamless.

### 18 · OV-10b · Top lip (walkable edge)

**Save as:** `Art_Source/Environment/ObsidianVoid/OV-10b_top_edge_v1.png`, landscape 4:1, transparent background (real alpha; if you can't, a flat pure magenta #FF00FF background) above the lip, tiles sideways. **Attach:** OV-10a, R1, R5.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their warm palette. This place is the OBSIDIAN VOID, the game's second reality: a mirror world of the ruins, built of black volcanic glass and polished obsidian monoliths floating in a starry void, thin cyan light running along their edges and through engraved grid lines, glass and crystal growths, cold mist, faint wireframe structures. Light: cold. A pale blue-white star glow is the key light; every accent light is cyan. Shadows are deep navy, never pure black. Match the attached Obsidian Void sky exactly in light and palette.

Paint the walkable top edge of the void's blocks, as a horizontal strip: the top course of the attached obsidian blocks, its upper edge polished and catching a thin cyan rim of light, small rime crystals and dark glass shards sitting on it here and there, and below it the stone continuing down into the block. The walking line (the top of the stone) must be one clear line across the whole strip. Everything above the crystals is transparent. Seamlessly tileable left to right.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector, not a neon sci-fi interface. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red, crimson, orange or gold.
```

**Reject if:** the walking line wavers by more than a few pixels; crystals taller than a fifth of the strip; a seam; a bright neon line.

### 19 · OV-10c · Underside edge

**Save as:** `Art_Source/Environment/ObsidianVoid/OV-10c_underside_v1.png`, landscape 4:1, transparent background (real alpha; if you can't, a flat pure magenta #FF00FF background) below, tiles sideways. **Attach:** OV-10a, R1.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their warm palette. This place is the OBSIDIAN VOID, the game's second reality: a mirror world of the ruins, built of black volcanic glass and polished obsidian monoliths floating in a starry void, thin cyan light running along their edges and through engraved grid lines, glass and crystal growths, cold mist, faint wireframe structures. Light: cold. A pale blue-white star glow is the key light; every accent light is cyan. Shadows are deep navy, never pure black. Match the attached Obsidian Void sky exactly in light and palette.

Paint the underside of the void's floating blocks as a horizontal strip: the bottom course of the attached obsidian blocks with a clear straight bottom line, and below it hanging shards of dark glass and thin icicle crystals of different lengths, a faint cyan glow inside a few of them. Everything below the shards is transparent. Seamlessly tileable left to right.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector, not a neon sci-fi interface. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red, crimson, orange or gold.
```

**Reject if:** shards so long they read as a lower floor; a seam; bright cyan.

### 20 · OV-11 · Thin floating slab

**Save as:** `Art_Source/Environment/ObsidianVoid/OV-11_slab_v1.png`, landscape 4:1, transparent background (real alpha; if you can't, a flat pure magenta #FF00FF background) around it, tiles sideways. **Attach:** OV-10a, OV-10b, R1.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their warm palette. This place is the OBSIDIAN VOID, the game's second reality: a mirror world of the ruins, built of black volcanic glass and polished obsidian monoliths floating in a starry void, thin cyan light running along their edges and through engraved grid lines, glass and crystal growths, cold mist, faint wireframe structures. Light: cold. A pale blue-white star glow is the key light; every accent light is cyan. Shadows are deep navy, never pure black. Match the attached Obsidian Void sky exactly in light and palette.

Paint a thin floating slab of obsidian seen from the side, as a horizontal strip: a single course of glass blocks with the same polished top lip and cyan rim as the attached top edge, a clean flat walking line, and a few short shards hanging under it. Seamlessly tileable left to right.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector, not a neon sci-fi interface. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red, crimson, orange or gold.
```

**Reject if:** the slab thicker than a quarter of the strip; a wavering top line; a seam.

### 21 · OV-12 · Side face of walls and blocks

**Save as:** `Art_Source/Environment/ObsidianVoid/OV-12_wall_edge_v1.png`, portrait 1:4, transparent background (real alpha; if you can't, a flat pure magenta #FF00FF background) on the left, tiles vertically. **Attach:** OV-10a, R1.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their warm palette. This place is the OBSIDIAN VOID, the game's second reality: a mirror world of the ruins, built of black volcanic glass and polished obsidian monoliths floating in a starry void, thin cyan light running along their edges and through engraved grid lines, glass and crystal growths, cold mist, faint wireframe structures. Light: cold. A pale blue-white star glow is the key light; every accent light is cyan. Shadows are deep navy, never pure black. Match the attached Obsidian Void sky exactly in light and palette.

Paint the left side face of the void's walls as a vertical strip: the edge column of the attached obsidian blocks, its outer face polished with a faint cyan rim where the light grazes it, chipped and fractured in places, rime in a few joints. Everything left of the face is transparent. Seamlessly tileable top to bottom.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector, not a neon sci-fi interface. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red, crimson, orange or gold.
```

**Reject if:** a seam; the face wavering by more than a tenth of the strip; bright cyan.

---

## Dressing, door, checkpoint, foreground

### 22 · OV-18 · Climbable light tether

**Save as:** `Art_Source/Environment/ObsidianVoid/OV-18_climb_tether_v1.png`, portrait 1:3, transparent background (real alpha; if you can't, a flat pure magenta #FF00FF background). **Attach:** R1, OV-01.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their warm palette. This place is the OBSIDIAN VOID, the game's second reality: a mirror world of the ruins, built of black volcanic glass and polished obsidian monoliths floating in a starry void, thin cyan light running along their edges and through engraved grid lines, glass and crystal growths, cold mist, faint wireframe structures. Light: cold. A pale blue-white star glow is the key light; every accent light is cyan. Shadows are deep navy, never pure black. Match the attached Obsidian Void sky exactly in light and palette.

Paint a climbable tether in three stacked parts with clear gaps between them: a top anchor (a small obsidian clamp holding the line), a tiling middle (a braided cable of dark glass fibres with a cyan light running inside it and small knot-like nodes every so often, made to tile vertically), and a bottom tip (the braid fraying into a few glowing strands). It must read clearly as something a cat can climb.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector, not a neon sci-fi interface. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red, crimson, orange or gold.
```

**Reject if:** the middle doesn't tile; the parts touch; it reads as a decoration or a laser (it must look solid and grippable).

### 23 · OV-20 · Hanging strands (not climbable)

**Save as:** `Art_Source/Environment/ObsidianVoid/OV-20_strands_v1.png`, landscape 3:2, transparent background (real alpha; if you can't, a flat pure magenta #FF00FF background). **Attach:** R1, OV-01.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their warm palette. This place is the OBSIDIAN VOID, the game's second reality: a mirror world of the ruins, built of black volcanic glass and polished obsidian monoliths floating in a starry void, thin cyan light running along their edges and through engraved grid lines, glass and crystal growths, cold mist, faint wireframe structures. Light: cold. A pale blue-white star glow is the key light; every accent light is cyan. Shadows are deep navy, never pure black. Match the attached Obsidian Void sky exactly in light and palette.

Paint a sheet of SIX separate hanging decorations of different lengths, each hanging from a flat line at its top: thin strands of cold mist-light, chains of tiny crystal beads and wisps of frost. Thin, soft and clearly decorative: they must not look like the climbable tether.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector, not a neon sci-fi interface. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red, crimson, orange or gold.
```

**Reject if:** anything braided or thick (it would read as climbable); pieces touch; bright cyan.

### 24 · OV-21 · Low clumps for tops

**Save as:** `Art_Source/Environment/ObsidianVoid/OV-21_clumps_v1.png`, landscape 3:2, transparent background (real alpha; if you can't, a flat pure magenta #FF00FF background). **Attach:** R1, OV-10b.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their warm palette. This place is the OBSIDIAN VOID, the game's second reality: a mirror world of the ruins, built of black volcanic glass and polished obsidian monoliths floating in a starry void, thin cyan light running along their edges and through engraved grid lines, glass and crystal growths, cold mist, faint wireframe structures. Light: cold. A pale blue-white star glow is the key light; every accent light is cyan. Shadows are deep navy, never pure black. Match the attached Obsidian Void sky exactly in light and palette.

Paint a sheet of SIX separate low clumps that sit on a walkable top, each on a flat base line: clusters of small rime crystals, broken glass shards and a few pale blue frost flowers, none taller than about a third of its width.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector, not a neon sci-fi interface. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red, crimson, orange or gold.
```

**Reject if:** tall pieces; pieces touch; anything that looks like a hazard (no sharp red or spikes pointing up in a row).

### 25 · OV-22 · Banners

**Save as:** `Art_Source/Environment/ObsidianVoid/OV-22_banners_v1.png`, portrait 3:4, transparent background (real alpha; if you can't, a flat pure magenta #FF00FF background). **Attach:** R1, OV-01.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their warm palette. This place is the OBSIDIAN VOID, the game's second reality: a mirror world of the ruins, built of black volcanic glass and polished obsidian monoliths floating in a starry void, thin cyan light running along their edges and through engraved grid lines, glass and crystal growths, cold mist, faint wireframe structures. Light: cold. A pale blue-white star glow is the key light; every accent light is cyan. Shadows are deep navy, never pure black. Match the attached Obsidian Void sky exactly in light and palette.

Paint TWO separate tall hanging banners, side by side with a gap: long strips of dark slate cloth hanging from a short obsidian rod, worn and tattered at the bottom, a single thin cyan geometric glyph woven down each.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector, not a neon sci-fi interface. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red, crimson, orange or gold.
```

**Reject if:** red or gold cloth; text or letters; the banners touch.

### 26 · OV-23 · Rubble and carved panels

**Save as:** `Art_Source/Environment/ObsidianVoid/OV-23_rubble_glyphs_v1.png`, landscape 3:2, transparent background (real alpha; if you can't, a flat pure magenta #FF00FF background). **Attach:** R1, OV-10a.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their warm palette. This place is the OBSIDIAN VOID, the game's second reality: a mirror world of the ruins, built of black volcanic glass and polished obsidian monoliths floating in a starry void, thin cyan light running along their edges and through engraved grid lines, glass and crystal growths, cold mist, faint wireframe structures. Light: cold. A pale blue-white star glow is the key light; every accent light is cyan. Shadows are deep navy, never pure black. Match the attached Obsidian Void sky exactly in light and palette.

Paint a sheet of SIX separate pieces, well spaced: three rubble piles of broken obsidian blocks and glass shards on a flat base line, and three carved wall panels of black glass with engraved geometric grids, their lines faintly glowing cyan.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector, not a neon sci-fi interface. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red, crimson, orange or gold.
```

**Reject if:** pieces touch; text or letters; bright neon lines.

### 27 · OV-24 · Level exit door

**Save as:** `Art_Source/Environment/ObsidianVoid/OV-24_door_v1.png`, landscape 2:1, transparent background (real alpha; if you can't, a flat pure magenta #FF00FF background). **Attach:** R1, R6, OV-01.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their warm palette. This place is the OBSIDIAN VOID, the game's second reality: a mirror world of the ruins, built of black volcanic glass and polished obsidian monoliths floating in a starry void, thin cyan light running along their edges and through engraved grid lines, glass and crystal growths, cold mist, faint wireframe structures. Light: cold. A pale blue-white star glow is the key light; every accent light is cyan. Shadows are deep navy, never pure black. Match the attached Obsidian Void sky exactly in light and palette.

Paint the level's exit door twice, side by side with a gap: CLOSED (a tall arched doorway framed by two obsidian monoliths, filled by a dark glass slab with a faint cyan seam down the middle) and OPEN (the same frame, the slab gone, a soft cyan-white glow filling the arch). It must read at a glance as the way out, brighter and clearer than anything around it.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector, not a neon sci-fi interface. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red, crimson, orange or gold.
```

**Reject if:** the two states differ in size or framing; red or gold light; text.

### 28 · OV-25 · Checkpoint

**Save as:** `Art_Source/Environment/ObsidianVoid/OV-25_checkpoint_v1.png`, landscape 2:1, transparent background (real alpha; if you can't, a flat pure magenta #FF00FF background). **Attach:** R1, OV-01.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their warm palette. This place is the OBSIDIAN VOID, the game's second reality: a mirror world of the ruins, built of black volcanic glass and polished obsidian monoliths floating in a starry void, thin cyan light running along their edges and through engraved grid lines, glass and crystal growths, cold mist, faint wireframe structures. Light: cold. A pale blue-white star glow is the key light; every accent light is cyan. Shadows are deep navy, never pure black. Match the attached Obsidian Void sky exactly in light and palette.

Paint a checkpoint marker twice, side by side with a gap: IDLE (a small knee-high obsidian obelisk with a dark crystal set in its top) and CROSSED (the same obelisk, the crystal glowing cyan-white with a soft halo and light running down its edges).

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector, not a neon sci-fi interface. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red, crimson, orange or gold.
```

**Reject if:** the two states differ in size; red or gold; text.

### 29 · OV-26 · Foreground shards

**Save as:** `Art_Source/Environment/ObsidianVoid/OV-26_foreground_v1.png`, landscape 3:2, transparent background (real alpha; if you can't, a flat pure magenta #FF00FF background). **Attach:** R1, OV-01.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their warm palette. This place is the OBSIDIAN VOID, the game's second reality: a mirror world of the ruins, built of black volcanic glass and polished obsidian monoliths floating in a starry void, thin cyan light running along their edges and through engraved grid lines, glass and crystal growths, cold mist, faint wireframe structures. Light: cold. A pale blue-white star glow is the key light; every accent light is cyan. Shadows are deep navy, never pure black. Match the attached Obsidian Void sky exactly in light and palette.

Paint a sheet of FOUR separate large foreground pieces that frame the bottom corners of the screen, well spaced: big dark glass shards and crystal clusters, softly out of focus, a faint cyan glint on their edges. Big shapes, little detail.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector, not a neon sci-fi interface. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red, crimson, orange or gold.
```

**Reject if:** pieces touch; sharp focus; bright light (they sit in front of gameplay and must stay dark and quiet).

### 30 · OV-26b · Foreground monolith edge

**Save as:** `Art_Source/Environment/ObsidianVoid/OV-26b_foreground_monolith_v1.png`, portrait 1:3, transparent background (real alpha; if you can't, a flat pure magenta #FF00FF background). **Attach:** R1, OV-01.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their warm palette. This place is the OBSIDIAN VOID, the game's second reality: a mirror world of the ruins, built of black volcanic glass and polished obsidian monoliths floating in a starry void, thin cyan light running along their edges and through engraved grid lines, glass and crystal growths, cold mist, faint wireframe structures. Light: cold. A pale blue-white star glow is the key light; every accent light is cyan. Shadows are deep navy, never pure black. Match the attached Obsidian Void sky exactly in light and palette.

Paint one tall foreground monolith edge for the side of the screen: the edge of a huge obsidian slab seen very close, softly out of focus, a long faint cyan seam, frost along one side.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector, not a neon sci-fi interface. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red, crimson, orange or gold.
```

**Reject if:** sharp focus; bright light; anything in the middle of the frame.

### 31 · OV-27 · Low fog band

**Save as:** `Art_Source/Environment/ObsidianVoid/OV-27_fog_v1.png`, landscape 3:1, transparent background (real alpha; if you can't, a flat pure magenta #FF00FF background), tiles sideways. **Attach:** R1, OV-01.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their warm palette. This place is the OBSIDIAN VOID, the game's second reality: a mirror world of the ruins, built of black volcanic glass and polished obsidian monoliths floating in a starry void, thin cyan light running along their edges and through engraved grid lines, glass and crystal growths, cold mist, faint wireframe structures. Light: cold. A pale blue-white star glow is the key light; every accent light is cyan. Shadows are deep navy, never pure black. Match the attached Obsidian Void sky exactly in light and palette.

Paint a low band of cold ground fog: soft rolling navy and blue-grey mist, densest along its bottom edge and fading to transparent above, a faint cyan sheen on its upper wisps. Seamlessly tileable left to right.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector, not a neon sci-fi interface. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red, crimson, orange or gold.
```

**Reject if:** hard edges; white fog; a seam.

### 32 · OV-28 · Light beam

**Save as:** `Art_Source/Environment/ObsidianVoid/OV-28_beam_v1.png`, portrait 1:2, on pure black (light becomes transparency). **Attach:** R1, OV-01.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their warm palette. This place is the OBSIDIAN VOID, the game's second reality: a mirror world of the ruins, built of black volcanic glass and polished obsidian monoliths floating in a starry void, thin cyan light running along their edges and through engraved grid lines, glass and crystal growths, cold mist, faint wireframe structures. Light: cold. A pale blue-white star glow is the key light; every accent light is cyan. Shadows are deep navy, never pure black. Match the attached Obsidian Void sky exactly in light and palette.

Paint one diagonal beam of cold light on pure black: a soft pale blue-cyan shaft falling from the top left towards the bottom right, widening as it falls, with a few tiny motes in it, fading out at both ends.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector, not a neon sci-fi interface. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red, crimson, orange or gold.
```

**Reject if:** hard edges; warm light; anything not on black.

### 33 · OV-30 · Pit floor: the void pool

**Save as:** `Art_Source/Environment/ObsidianVoid/OV-30_void_pool_v1.png`, landscape 4:1, transparent background (real alpha; if you can't, a flat pure magenta #FF00FF background) above the surface, tiles sideways. **Attach:** R1, OV-01.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their warm palette. This place is the OBSIDIAN VOID, the game's second reality: a mirror world of the ruins, built of black volcanic glass and polished obsidian monoliths floating in a starry void, thin cyan light running along their edges and through engraved grid lines, glass and crystal growths, cold mist, faint wireframe structures. Light: cold. A pale blue-white star glow is the key light; every accent light is cyan. Shadows are deep navy, never pure black. Match the attached Obsidian Void sky exactly in light and palette.

Paint the surface of the dark pool that lies at the bottom of the void's pits, seen from the side as a horizontal strip: a still black-glass liquid with a thin bright cyan meniscus line along its surface, faint reflections of stars just below it, and a few slow ripples. Seamlessly tileable left to right; everything above the surface is transparent.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector, not a neon sci-fi interface. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red, crimson, orange or gold.
```

**Reject if:** a seam; bright neon; waves (it is still).

---

## The rest of the kit

### 34 · OV-08 · Void fall (tiling)

**Save as:** `Art_Source/Environment/ObsidianVoid/OV-08_voidfall_v1.png`, portrait 1:4, transparent background (real alpha; if you can't, a flat pure magenta #FF00FF background) on both sides, tiles vertically. **Attach:** R1, OV-01.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their warm palette. This place is the OBSIDIAN VOID, the game's second reality: a mirror world of the ruins, built of black volcanic glass and polished obsidian monoliths floating in a starry void, thin cyan light running along their edges and through engraved grid lines, glass and crystal growths, cold mist, faint wireframe structures. Light: cold. A pale blue-white star glow is the key light; every accent light is cyan. Shadows are deep navy, never pure black. Match the attached Obsidian Void sky exactly in light and palette.

Paint a falling stream for the void, standing in for a waterfall: a column of glittering dark-glass particles and cold light pouring straight down, densest in the middle, soft at its sides, faint cyan streaks along its fall. Seamlessly tileable top to bottom.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector, not a neon sci-fi interface. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red, crimson, orange or gold.
```

**Reject if:** a seam; water (it is particles and light); warm colours.

### 35 · OV-09 · Void-fall spray

**Save as:** `Art_Source/Environment/ObsidianVoid/OV-09_voidfall_spray_v1.png`, landscape 2:1, on pure black (light becomes transparency). **Attach:** R1, OV-01.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their warm palette. This place is the OBSIDIAN VOID, the game's second reality: a mirror world of the ruins, built of black volcanic glass and polished obsidian monoliths floating in a starry void, thin cyan light running along their edges and through engraved grid lines, glass and crystal growths, cold mist, faint wireframe structures. Light: cold. A pale blue-white star glow is the key light; every accent light is cyan. Shadows are deep navy, never pure black. Match the attached Obsidian Void sky exactly in light and palette.

Paint the spray where the void fall meets the pool, on pure black: a soft burst of cold light particles and mist rising from a point at the bottom centre, pale blue-cyan, fading out upwards.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector, not a neon sci-fi interface. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red, crimson, orange or gold.
```

**Reject if:** hard edges; anything not on black.

### 36 · OV-15 · Broken monolith (short)

**Save as:** `Art_Source/Environment/ObsidianVoid/OV-15_broken_monolith_v1.png`, portrait 1:2, transparent background (real alpha; if you can't, a flat pure magenta #FF00FF background). **Attach:** R1, OV-10a.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their warm palette. This place is the OBSIDIAN VOID, the game's second reality: a mirror world of the ruins, built of black volcanic glass and polished obsidian monoliths floating in a starry void, thin cyan light running along their edges and through engraved grid lines, glass and crystal growths, cold mist, faint wireframe structures. Light: cold. A pale blue-white star glow is the key light; every accent light is cyan. Shadows are deep navy, never pure black. Match the attached Obsidian Void sky exactly in light and palette.

Paint one short broken monolith standing on a flat base line: a knee-high stump of an obsidian pillar snapped off at the top, the break jagged and glassy, a faint cyan seam down one face, rime at its foot.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector, not a neon sci-fi interface. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red, crimson, orange or gold.
```

**Reject if:** taller than twice its width; red or gold; text.

### 37 · OV-31 · Grip carpet (the void's moss)

**Save as:** `Art_Source/Environment/ObsidianVoid/OV-31_grip_carpet_v1.png`, portrait 1:4, transparent background (real alpha; if you can't, a flat pure magenta #FF00FF background) on the left, tiles vertically. **Attach:** R1, OV-12.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their warm palette. This place is the OBSIDIAN VOID, the game's second reality: a mirror world of the ruins, built of black volcanic glass and polished obsidian monoliths floating in a starry void, thin cyan light running along their edges and through engraved grid lines, glass and crystal growths, cold mist, faint wireframe structures. Light: cold. A pale blue-white star glow is the key light; every accent light is cyan. Shadows are deep navy, never pure black. Match the attached Obsidian Void sky exactly in light and palette.

Paint a vertical carpet that clings to a wall's face, standing in for the moss that marks a grippable wall: a dense mat of pale blue frost lichen and tiny cyan-tipped crystal fibres, growing thickly along the face from top to bottom, a few tufts reaching out from it. Seamlessly tileable top to bottom; transparent on the left beyond the tufts.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector, not a neon sci-fi interface. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red, crimson, orange or gold.
```

**Reject if:** a seam; patchy (it must read as a continuous mat the whole way down); red or gold.
