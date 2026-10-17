# PAX-A18 · Background prompts: 30 for variety (2026-10-16)

**Why:** the developer is happy with today's background (Golden Ruins, L001–L004, L015) but wants **variety: different kinds
of place, not more buildings**. These 30 paintings make four new biomes:

| Biome | Levels | Its own |
|---|---|---|
| Sky Isles | L005, L007, L011, L018 | the sky, the floating-isle bands, landmark pieces, lip and dressing; the sky, stone fill and moored isle are done (pilot) |
| Sunken Grotto | L008, L012, L016, L019 | the full set, inside a cavern |
| Jungle Falls | L006, L013, L014, L017 | the full set, a canyon with falls |
| Ember Night | L009, L010, L020 | the full set, fire and moonlight |

Each biome gets:
- its sky plate;
- a far band and a mid band;
- two landmark pieces;
- its stone fill;
- its walkable top lip;
- one dressing sheet.

**Not needed (code makes them):** stone ends, corners, slabs and undersides (W3 cuts them from the fill, `env_bumpy.py`).

**Later:** the per-level signatures in `PAX-A18_ASSETS.md`, once a biome is built.

## How to work (as the pilot)

- One ChatGPT chat per biome, in this order: **B → C → D → E**.
- Start each biome with its sky plate (C: the cavern back plate), then attach that sky to every later prompt of the biome, so
  the light matches.
- Attach the references listed, paste the whole prompt, then check the reject list. Regenerate rather than fixing by hand.
- "Transparent" means real alpha. If ChatGPT paints a checkerboard, say so and regenerate, or ask for a flat pure magenta
  #FF00FF background.
- Shrink each result to phone size on screen before saving: the big shapes must still read.
- Save to `Art_Source/Environment/<Biome>/` with the exact file name. The folders exist.

**References:**
- **R1:** `Docs/Art/Reference/styleframe_A_v1.png` (technique)
- **R2:** `DesignImages/14_environment-breakdown.png` (depth)
- **R3:** `Art_Source/Environment/GoldenRuins/A_GAME_Platform_Fill__raw_v1.png` (block size)
- The biome's own sky and fill once painted (named in each item). For Sky Isles they are already painted in the pilot:
  **B-01 sky** = `Art_Source/Environment/SkyIsles/B-01_sky_v1.png` (pilot item 3) and **B-10 fill** =
  `Art_Source/Environment/SkyIsles/B-10_fill_v1.png` (pilot item 4).

---

## Sky Isles (L005, L007, L011, L018)

### 1 · Far floating isles band

**Save as:** `Art_Source/Environment/SkyIsles/B-02_far_isles_v1.png`, landscape 3:2, transparent above and below the isles, tiles sideways. **Attach:** R1, R2, B-01 sky.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their palette. This place is SKY ISLES: high above the clouds at dusk, with nothing below: floating islands of pale weathered limestone, long roots and broken chains trailing under them, wind-bent grass, a cloud sea far below. Light: a low dusk sun on the left; rose and peach highlights, lilac and violet shadows. Match the attached Sky Isles sky exactly in light and palette.

Paint a long horizontal band of DISTANT floating islands for the far background: six or seven small and large inverted-cone rock islands at different heights in the middle of the image, flat grassy tops, a few tiny wind shrines, thin roots and chains trailing below each, very soft violet aerial haze, low contrast, no hard outlines. The cloud sea fills the bottom fifth; everything above the islands is transparent. Seen far away behind gameplay, so pale and quiet. Seamlessly tileable left to right, with the left and right 15% quiet (no island cut by the edge).

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red or crimson, no saturated cyan or teal.
```

**Reject if:** islands cut by the left or right edge; too much contrast; golden light; one island repeated.

### 2 · Mid cloud-sea band

**Save as:** `Art_Source/Environment/SkyIsles/B-03_mid_cloud_sea_v1.png`, landscape 3:2, transparent above, tiles sideways. **Attach:** R1, R2, B-01 sky.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their palette. This place is SKY ISLES: high above the clouds at dusk, with nothing below: floating islands of pale weathered limestone, long roots and broken chains trailing under them, wind-bent grass, a cloud sea far below. Light: a low dusk sun on the left; rose and peach highlights, lilac and violet shadows. Match the attached Sky Isles sky exactly in light and palette.

Paint a MIDDLE-DISTANCE band of billowing cloud sea filling the lower third of the image, rose-lit on its tops and violet in its folds, with three floating limestone islands rising out of it at different heights, each with a broken stone stair or a single arch on it and long hanging roots. Clearer and more detailed than the far band, but still softer than the gameplay layer. Everything above the islands is transparent. Seamlessly tileable left to right, with the left and right 15% quiet.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red or crimson, no saturated cyan or teal.
```

**Reject if:** the cloud sea is flat or grey; islands cut by the edges; too busy.

### 3 · Hero: great tree on a floating rock

**Save as:** `Art_Source/Environment/SkyIsles/B-05_hero_tree_isle_v1.png`, portrait 2:3, transparent. **Attach:** R1, R2, B-01 sky.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their palette. This place is SKY ISLES: high above the clouds at dusk, with nothing below: floating islands of pale weathered limestone, long roots and broken chains trailing under them, wind-bent grass, a cloud sea far below. Light: a low dusk sun on the left; rose and peach highlights, lilac and violet shadows. Match the attached Sky Isles sky exactly in light and palette.

Paint ONE large mid-distance landmark: a huge ancient tree growing on a small floating rock, its massive roots wrapping the whole rock and hanging far below it like a beard, its crown wide and wind-swept to the right, a few leaves drifting off on the wind, rim light on its left edges from the low sun. Strong simple silhouette, readable as a small dark shape.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red or crimson, no saturated cyan or teal.
```

**Reject if:** more than one tree or island; cluttered silhouette; golden-orange light.

### 4 · Hero: broken sky bridge

**Save as:** `Art_Source/Environment/SkyIsles/B-06_hero_sky_bridge_v1.png`, landscape 3:2, transparent. **Attach:** R1, R2, B-01 sky.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their palette. This place is SKY ISLES: high above the clouds at dusk, with nothing below: floating islands of pale weathered limestone, long roots and broken chains trailing under them, wind-bent grass, a cloud sea far below. Light: a low dusk sun on the left; rose and peach highlights, lilac and violet shadows. Match the attached Sky Isles sky exactly in light and palette.

Paint ONE mid-distance landmark: a long ancient stone bridge between two small floating islands, snapped in the middle, its two halves drooping, loose blocks dangling from the broken ends on old ropes, wind-bent grass on the islands, a few wisps of cloud drifting under the gap.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red or crimson, no saturated cyan or teal.
```

**Reject if:** the bridge is intact; perspective; cluttered.

### 5 · Top lip (walkable edge, limestone)

**Save as:** `Art_Source/Environment/SkyIsles/B-11_top_lip_v1.png`, landscape 3:2, transparent above and below the band, tiles sideways. **Attach:** R1, B-01 sky, B-10 fill.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their palette. This place is SKY ISLES: high above the clouds at dusk, with nothing below: floating islands of pale weathered limestone, long roots and broken chains trailing under them, wind-bent grass, a cloud sea far below. Light: a low dusk sun on the left; rose and peach highlights, lilac and violet shadows. Match the attached Sky Isles sky exactly in light and palette.

Paint the WALKABLE TOP EDGE of a pale limestone platform as a long horizontal strip in strict side elevation, in the middle third of the image: the stone of the attached fill. The top line is overall level but never ruler-straight: worn, slightly uneven capstones of different lengths, rounded by wind, small chips at their joints. Along the top, irregular clumps of short wind-bent grass leaning right and a few tiny pale flowers, in clusters with bare stone between (not an even fringe). Under it the first course of stone, lit rose on its upper faces, fading to transparent below. On a phone it is about 25 pixels tall, so the uneven skyline must read. Seamlessly tileable left to right.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red or crimson, no saturated cyan or teal.
```

**Reject if:** the top is a straight line; an even fringe of grass; it doesn't match B-10; it doesn't tile.

### 6 · Dressing: ground and hanging pieces

**Save as:** `Art_Source/Environment/SkyIsles/B-20_dressing_v1.png`, landscape 3:2, transparent, a sheet. **Attach:** R1, B-01 sky, B-10 fill.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their palette. This place is SKY ISLES: high above the clouds at dusk, with nothing below: floating islands of pale weathered limestone, long roots and broken chains trailing under them, wind-bent grass, a cloud sea far below. Light: a low dusk sun on the left; rose and peach highlights, lilac and violet shadows. Match the attached Sky Isles sky exactly in light and palette.

Paint a sprite sheet of SIX well-separated dressing pieces, each different in size and silhouette. Ground pieces, each on a flat base line: a clump of tall wind-bent grass, a small cairn of stacked flat stones with a faded ochre cloth tied to a stick, a broken limestone urn. Hanging pieces, each from a straight top line: a bundle of long thin roots, a short broken chain with a loose ring, a rope with two stones tied to it. None longer than twice its width.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red or crimson, no saturated cyan or teal.
```

**Reject if:** pieces overlap; any piece repeats another; a red cloth.

---

## Sunken Grotto (L008, L012, L016, L019)

### 7 · Cavern back plate (the sky of this biome)

**Save as:** `Art_Source/Environment/SunkenGrotto/C-01_cavern_back_v1.png`, landscape 3:2, opaque. **Attach:** R1, R2.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their palette. This place is the SUNKEN GROTTO: inside a vast cavern with a ceiling of rock: stalactites and stalagmites, wet dark basalt that glints where light touches it, an underground lake, glowing fungi. Light: pale cool shafts of daylight from cracks far above (pale grey-blue, NEVER saturated cyan or teal) and a warm amber glow from fungi; deep blue-grey darkness in between.

Paint the full background plate of a vast cavern seen from inside, filling the image edge to edge: deep blue-grey darkness; a jagged cave mouth high up on the right with pale daylight pouring in as a broad soft shaft that fades as it falls; faint layered rock far away; a pale mist band low across the image; a few tiny amber fungus glows deep in the dark. Nothing busy in the middle band (gameplay sits on top of it).

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red or crimson, no saturated cyan or teal.
```

**Reject if:** saturated cyan or teal; it looks like an outdoor sky; the middle is busy.

### 8 · Far cavern band

**Save as:** `Art_Source/Environment/SunkenGrotto/C-02_far_cavern_v1.png`, landscape 3:2, transparent above, tiles sideways. **Attach:** R1, R2, C-01.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their palette. This place is the SUNKEN GROTTO: inside a vast cavern with a ceiling of rock: stalactites and stalagmites, wet dark basalt that glints where light touches it, an underground lake, glowing fungi. Light: pale cool shafts of daylight from cracks far above (pale grey-blue, NEVER saturated cyan or teal) and a warm amber glow from fungi; deep blue-grey darkness in between.

Paint a long horizontal band of a DISTANT cavern: tall rock columns where stalactites meet stalagmites, a still underground lake reflecting them along the bottom, faint light shafts slanting between the columns, low contrast, misty blue-grey, no hard outlines. Everything above the columns' upper ends is transparent. Seamlessly tileable left to right, with the left and right 15% quiet.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red or crimson, no saturated cyan or teal.
```

**Reject if:** high contrast; a column cut by the edge; blue-cyan.

### 9 · Mid band: stalagmites and fungus pools

**Save as:** `Art_Source/Environment/SunkenGrotto/C-03_mid_band_v1.png`, landscape 3:2, transparent above, tiles sideways. **Attach:** R1, R2, C-01.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their palette. This place is the SUNKEN GROTTO: inside a vast cavern with a ceiling of rock: stalactites and stalagmites, wet dark basalt that glints where light touches it, an underground lake, glowing fungi. Light: pale cool shafts of daylight from cracks far above (pale grey-blue, NEVER saturated cyan or teal) and a warm amber glow from fungi; deep blue-grey darkness in between.

Paint a MIDDLE-DISTANCE band of cavern floor: clusters of stalagmites of very different heights rising from a dark wet floor, shelves of basalt carrying glowing amber mushrooms, small still pools reflecting that glow, drips falling. Everything above the rock is transparent. Seamlessly tileable left to right, with the left and right 15% quiet.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red or crimson, no saturated cyan or teal.
```

**Reject if:** stalagmites all the same height; the fungi glow cyan or green-neon; cut by the edges.

### 10 · Hero: stalactite curtain

**Save as:** `Art_Source/Environment/SunkenGrotto/C-04_hero_stalactites_v1.png`, landscape 3:2, transparent below. **Attach:** R1, R2, C-01.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their palette. This place is the SUNKEN GROTTO: inside a vast cavern with a ceiling of rock: stalactites and stalagmites, wet dark basalt that glints where light touches it, an underground lake, glowing fungi. Light: pale cool shafts of daylight from cracks far above (pale grey-blue, NEVER saturated cyan or teal) and a warm amber glow from fungi; deep blue-grey darkness in between.

Paint ONE mid-distance landmark hanging from the top edge: a massive mass of cavern ceiling rock with dozens of stalactites of very different lengths, the longest reaching halfway down the image, wet and glinting, water dripping from the tips, warm amber fungus glowing in the cracks between them. Transparent below and between the stalactites.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red or crimson, no saturated cyan or teal.
```

**Reject if:** stalactites the same length; no transparency between them; cyan.

### 11 · Hero: glowing fungus pillar

**Save as:** `Art_Source/Environment/SunkenGrotto/C-06_hero_fungus_pillar_v1.png`, portrait 2:3, transparent. **Attach:** R1, R2, C-01.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their palette. This place is the SUNKEN GROTTO: inside a vast cavern with a ceiling of rock: stalactites and stalagmites, wet dark basalt that glints where light touches it, an underground lake, glowing fungi. Light: pale cool shafts of daylight from cracks far above (pale grey-blue, NEVER saturated cyan or teal) and a warm amber glow from fungi; deep blue-grey darkness in between.

Paint ONE mid-distance landmark: a tall natural rock pillar in the cavern covered in shelves of glowing amber and warm green bracket fungi and tall thin luminous mushrooms, wet streaks of moss, a small still pool at its foot reflecting the glow.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red or crimson, no saturated cyan or teal.
```

**Reject if:** neon colours; more than one pillar; cluttered silhouette.

### 12 · Stone fill: wet basalt (tileable)

**Save as:** `Art_Source/Environment/SunkenGrotto/C-10_fill_v1.png`, square 1:1, opaque, seamless on all four sides. **Attach:** R1, R3.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their palette. This place is the SUNKEN GROTTO: inside a vast cavern with a ceiling of rock: stalactites and stalagmites, wet dark basalt that glints where light touches it, an underground lake, glowing fungi. Light: pale cool shafts of daylight from cracks far above (pale grey-blue, NEVER saturated cyan or teal) and a warm amber glow from fungi; deep blue-grey darkness in between.

Paint a SEAMLESS TILEABLE square texture of the cavern's stone, seen straight on (flat front view): wet dark basalt in rough columnar and blocky shapes about the size of the reference's blocks, a damp sheen on the upper edges of each block, thin streaks of dark moss and pale mineral stains, deep blue-grey with warm brown undertones. LOW CONTRAST: it fills every wall and floor behind the cat and the traps. No single standout feature that would visibly repeat.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red or crimson, no saturated cyan or teal.
```

**Reject if:** not seamless; high contrast; a standout feature; cyan.

### 13 · Top lip (walkable edge, basalt)

**Save as:** `Art_Source/Environment/SunkenGrotto/C-11_top_lip_v1.png`, landscape 3:2, transparent above and below the band, tiles sideways. **Attach:** R1, C-01, C-10 fill.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their palette. This place is the SUNKEN GROTTO: inside a vast cavern with a ceiling of rock: stalactites and stalagmites, wet dark basalt that glints where light touches it, an underground lake, glowing fungi. Light: pale cool shafts of daylight from cracks far above (pale grey-blue, NEVER saturated cyan or teal) and a warm amber glow from fungi; deep blue-grey darkness in between.

Paint the WALKABLE TOP EDGE of a wet basalt ledge as a long horizontal strip in strict side elevation, in the middle third of the image, in the stone of the attached fill: overall level but never ruler-straight, slightly uneven worn blocks glinting wet on top. Along the top, irregular cushions of damp moss with a few tiny glowing amber mushrooms in clusters, bare stone between. Under it the first course of basalt, fading to transparent below. About 25 pixels tall on a phone: the uneven skyline must read. Seamlessly tileable left to right.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red or crimson, no saturated cyan or teal.
```

**Reject if:** a straight top; an even fringe; it doesn't match C-10; it doesn't tile.

### 14 · Dressing: cavern pieces

**Save as:** `Art_Source/Environment/SunkenGrotto/C-20_dressing_v1.png`, landscape 3:2, transparent, a sheet. **Attach:** R1, C-01, C-10 fill.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their palette. This place is the SUNKEN GROTTO: inside a vast cavern with a ceiling of rock: stalactites and stalagmites, wet dark basalt that glints where light touches it, an underground lake, glowing fungi. Light: pale cool shafts of daylight from cracks far above (pale grey-blue, NEVER saturated cyan or teal) and a warm amber glow from fungi; deep blue-grey darkness in between.

Paint a sprite sheet of SIX well-separated cavern dressing pieces, each different. Ground pieces on a flat base line: a cluster of tall thin glowing amber mushrooms, a low mound of warm green moss with tiny glowing caps, a small group of stalagmites. Hanging pieces from a straight top line: a short stalactite cluster, a curtain of dripping moss, a single long thin stalactite with a water drop at its tip. None longer than twice its width.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red or crimson, no saturated cyan or teal.
```

**Reject if:** pieces overlap; neon colours; cyan.

---

## Jungle Falls (L006, L013, L014, L017)

### 15 · Canyon sky and mist plate

**Save as:** `Art_Source/Environment/JungleFalls/D-01_sky_v1.png`, landscape 3:2, opaque. **Attach:** R1, R2.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their palette. This place is JUNGLE FALLS: a deep canyon in dense jungle on a cool misty morning: sheer mossy green-grey granite cliffs, colossal tree trunks, tall waterfalls, rising mist, lianas and broad leaves. Light: soft pale morning light from the upper left through mist; cool green-grey shadows; warm only where sun touches the leaves.

Paint the full background plate of a jungle canyon's sky, filling the image edge to edge: a narrow strip of pale morning sky between two dark cliff rims at the top corners, the rest filled with soft layered morning mist, a hazy pale sun glow behind the mist upper left, cool green-grey tones. Nothing busy in the middle band.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red or crimson, no saturated cyan or teal.
```

**Reject if:** golden-orange light; a busy middle; no cliff rims.

### 16 · Far canyon band with waterfalls

**Save as:** `Art_Source/Environment/JungleFalls/D-02_far_canyon_v1.png`, landscape 3:2, transparent above, tiles sideways. **Attach:** R1, R2, D-01.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their palette. This place is JUNGLE FALLS: a deep canyon in dense jungle on a cool misty morning: sheer mossy green-grey granite cliffs, colossal tree trunks, tall waterfalls, rising mist, lianas and broad leaves. Light: soft pale morning light from the upper left through mist; cool green-grey shadows; warm only where sun touches the leaves.

Paint a long horizontal band of a DISTANT jungle canyon: tall sheer cliff walls with three or four thin waterfalls of different heights, the jungle canopy as soft rounded masses along their tops and bases, thick mist between the cliffs, low contrast, pale green-grey, no hard outlines. Everything above the cliff tops is transparent. Seamlessly tileable left to right, with the left and right 15% quiet.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red or crimson, no saturated cyan or teal.
```

**Reject if:** high contrast; waterfalls all the same; cut by the edges.

### 17 · Mid jungle band

**Save as:** `Art_Source/Environment/JungleFalls/D-03_mid_band_v1.png`, landscape 3:2, transparent above, tiles sideways. **Attach:** R1, R2, D-01.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their palette. This place is JUNGLE FALLS: a deep canyon in dense jungle on a cool misty morning: sheer mossy green-grey granite cliffs, colossal tree trunks, tall waterfalls, rising mist, lianas and broad leaves. Light: soft pale morning light from the upper left through mist; cool green-grey shadows; warm only where sun touches the leaves.

Paint a MIDDLE-DISTANCE band of jungle: giant straight tree trunks with buttress roots at different spacings, lianas looping between them, broad leaves and palms filling the gaps, mist at the ground, a stream catching the light. Everything above the canopy is transparent. Seamlessly tileable left to right, with the left and right 15% quiet.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red or crimson, no saturated cyan or teal.
```

**Reject if:** evenly spaced trunks; too busy; cut by the edges.

### 18 · Hero: great waterfall

**Save as:** `Art_Source/Environment/JungleFalls/D-04_hero_waterfall_v1.png`, portrait 2:3, transparent. **Attach:** R1, R2, D-01.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their palette. This place is JUNGLE FALLS: a deep canyon in dense jungle on a cool misty morning: sheer mossy green-grey granite cliffs, colossal tree trunks, tall waterfalls, rising mist, lianas and broad leaves. Light: soft pale morning light from the upper left through mist; cool green-grey shadows; warm only where sun touches the leaves.

Paint ONE mid-distance landmark: a tall waterfall pouring over a mossy granite cliff lip, the water white and pale green, breaking into mist at its foot, ferns and roots clinging to the wet cliff on both sides, a soft sheen in the spray. Transparent around the cliff and water.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red or crimson, no saturated cyan or teal.
```

**Reject if:** more than one fall; a rainbow; cluttered.

### 19 · Hero: colossal trunk

**Save as:** `Art_Source/Environment/JungleFalls/D-05_hero_giant_trunk_v1.png`, portrait 2:3, transparent. **Attach:** R1, R2, D-01.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their palette. This place is JUNGLE FALLS: a deep canyon in dense jungle on a cool misty morning: sheer mossy green-grey granite cliffs, colossal tree trunks, tall waterfalls, rising mist, lianas and broad leaves. Light: soft pale morning light from the upper left through mist; cool green-grey shadows; warm only where sun touches the leaves.

Paint ONE mid-distance landmark: a single colossal jungle tree trunk filling the image height, huge buttress roots gripping a boulder at its base, bark covered in moss, epiphytes and hanging lianas, a frayed old rope tied around it with a broken plank hanging from it. Transparent around it.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red or crimson, no saturated cyan or teal.
```

**Reject if:** several trees; the crown visible and cluttered; flat.

### 20 · Stone fill: mossy granite (tileable)

**Save as:** `Art_Source/Environment/JungleFalls/D-10_fill_v1.png`, square 1:1, opaque, seamless on all four sides. **Attach:** R1, R3.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their palette. This place is JUNGLE FALLS: a deep canyon in dense jungle on a cool misty morning: sheer mossy green-grey granite cliffs, colossal tree trunks, tall waterfalls, rising mist, lianas and broad leaves. Light: soft pale morning light from the upper left through mist; cool green-grey shadows; warm only where sun touches the leaves.

Paint a SEAMLESS TILEABLE square texture of the canyon's stone, seen straight on (flat front view): mossy green-grey granite in large rounded blocks about the size of the reference's blocks, patches of moss in the joints, thin root lines crossing the stone, cool green-grey with soft warm highlights. LOW CONTRAST. No single standout feature that would visibly repeat.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red or crimson, no saturated cyan or teal.
```

**Reject if:** not seamless; high contrast; too green (the moss must stay in the joints).

### 21 · Top lip (walkable edge, granite)

**Save as:** `Art_Source/Environment/JungleFalls/D-11_top_lip_v1.png`, landscape 3:2, transparent above and below the band, tiles sideways. **Attach:** R1, D-01, D-10 fill.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their palette. This place is JUNGLE FALLS: a deep canyon in dense jungle on a cool misty morning: sheer mossy green-grey granite cliffs, colossal tree trunks, tall waterfalls, rising mist, lianas and broad leaves. Light: soft pale morning light from the upper left through mist; cool green-grey shadows; warm only where sun touches the leaves.

Paint the WALKABLE TOP EDGE of a mossy granite ledge as a long horizontal strip in strict side elevation, in the middle third of the image, in the stone of the attached fill: overall level but never ruler-straight, slightly uneven rounded capstones with a root running along them. Along the top, irregular clumps of thick moss, small ferns and broad leaf tips, in clusters with bare stone between. Under it the first course of granite, fading to transparent below. About 25 pixels tall on a phone. Seamlessly tileable left to right.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red or crimson, no saturated cyan or teal.
```

**Reject if:** a straight top; an even fringe; it doesn't match D-10; it doesn't tile.

### 22 · Dressing: jungle pieces

**Save as:** `Art_Source/Environment/JungleFalls/D-20_dressing_v1.png`, landscape 3:2, transparent, a sheet. **Attach:** R1, D-01, D-10 fill.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their palette. This place is JUNGLE FALLS: a deep canyon in dense jungle on a cool misty morning: sheer mossy green-grey granite cliffs, colossal tree trunks, tall waterfalls, rising mist, lianas and broad leaves. Light: soft pale morning light from the upper left through mist; cool green-grey shadows; warm only where sun touches the leaves.

Paint a sprite sheet of SIX well-separated jungle dressing pieces, each different. Ground pieces on a flat base line: a wide clump of broad elephant-ear leaves, a small palm sapling, a mossy boulder with ferns. Hanging pieces from a straight top line: a short looped liana, a mat of hanging moss with drips, a broad drooping leaf cluster. None longer than twice its width.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red or crimson, no saturated cyan or teal.
```

**Reject if:** pieces overlap; flowers in red.

---

## Ember Night (L009, L010, L020)

### 23 · Night sky with the moon

**Save as:** `Art_Source/Environment/EmberNight/E-01_sky_v1.png`, landscape 3:2, opaque. **Attach:** R1, R2.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their palette. This place is EMBER NIGHT: ancient ruins at night lit by fire: dark charcoal stone, columns of smoke, glowing embers drifting, iron braziers. Light: a cool silver-blue moon is the key light; every warm light is amber or orange from fire (NEVER red).

Paint the full night sky, filling the image edge to edge: a large pale full moon upper right with a small cool halo, a deep blue-black sky with faint stars, thin slow clouds lit silver at their edges, a warm amber glow low on the horizon from distant fires, a few drifting columns of smoke. No ground, no buildings. Nothing busy in the middle band.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red or crimson, no saturated cyan or teal.
```

**Reject if:** a red glow; no moon; a busy middle.

### 24 · Far band: distant fires and smoke

**Save as:** `Art_Source/Environment/EmberNight/E-02_far_fires_v1.png`, landscape 3:2, transparent above, tiles sideways. **Attach:** R1, R2, E-01.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their palette. This place is EMBER NIGHT: ancient ruins at night lit by fire: dark charcoal stone, columns of smoke, glowing embers drifting, iron braziers. Light: a cool silver-blue moon is the key light; every warm light is amber or orange from fire (NEVER red).

Paint a long horizontal band of a DISTANT night landscape: low dark hills and broken ridgelines silhouetted against the moonlit sky, several small bonfires glowing amber along them, tall columns of smoke rising and leaning in the wind and catching the moonlight. Low contrast, cool blue-grey haze. Everything above the smoke is transparent. Seamlessly tileable left to right, with the left and right 15% quiet.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red or crimson, no saturated cyan or teal.
```

**Reject if:** buildings (keep it to land, fire and smoke); red fire; cut by the edges.

### 25 · Mid band: ember field

**Save as:** `Art_Source/Environment/EmberNight/E-03_mid_ember_field_v1.png`, landscape 3:2, transparent above, tiles sideways. **Attach:** R1, R2, E-01.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their palette. This place is EMBER NIGHT: ancient ruins at night lit by fire: dark charcoal stone, columns of smoke, glowing embers drifting, iron braziers. Light: a cool silver-blue moon is the key light; every warm light is amber or orange from fire (NEVER red).

Paint a MIDDLE-DISTANCE band of a scorched ruin field at night: low broken wall stubs and fallen columns half sunk in ash, iron fire baskets on poles burning amber at different heights, embers drifting up, smoke curling, moonlight on the upper edges. Everything above is transparent. Seamlessly tileable left to right, with the left and right 15% quiet.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red or crimson, no saturated cyan or teal.
```

**Reject if:** tall buildings; red fire; evenly spaced fires.

### 26 · Hero: great brazier

**Save as:** `Art_Source/Environment/EmberNight/E-04_hero_brazier_v1.png`, portrait 2:3, transparent. **Attach:** R1, R2, E-01.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their palette. This place is EMBER NIGHT: ancient ruins at night lit by fire: dark charcoal stone, columns of smoke, glowing embers drifting, iron braziers. Light: a cool silver-blue moon is the key light; every warm light is amber or orange from fire (NEVER red).

Paint ONE mid-distance landmark: a huge ancient iron fire basket on a tall stone pedestal, burning with a big amber-orange flame that throws warm light down the stone, sparks and embers rising into the night, the moon rim-lighting its far side, faded strips of charcoal-grey cloth offerings tied around the pedestal.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red or crimson, no saturated cyan or teal.
```

**Reject if:** a red flame; a building; cluttered.

### 27 · Hero: smoking ruin

**Save as:** `Art_Source/Environment/EmberNight/E-06_hero_smoking_ruin_v1.png`, landscape 3:2, transparent. **Attach:** R1, R2, E-01.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their palette. This place is EMBER NIGHT: ancient ruins at night lit by fire: dark charcoal stone, columns of smoke, glowing embers drifting, iron braziers. Light: a cool silver-blue moon is the key light; every warm light is amber or orange from fire (NEVER red).

Paint ONE mid-distance landmark: a cluster of collapsed walls at night with a low fire still burning behind them, thick smoke rising and catching the moonlight, sparks and embers in the smoke, scorched stone with amber light licking its edges.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red or crimson, no saturated cyan or teal.
```

**Reject if:** red fire; flat smoke; an intact building.

### 28 · Stone fill: charcoal stone (tileable)

**Save as:** `Art_Source/Environment/EmberNight/E-10_fill_v1.png`, square 1:1, opaque, seamless on all four sides. **Attach:** R1, R3.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their palette. This place is EMBER NIGHT: ancient ruins at night lit by fire: dark charcoal stone, columns of smoke, glowing embers drifting, iron braziers. Light: a cool silver-blue moon is the key light; every warm light is amber or orange from fire (NEVER red).

Paint a SEAMLESS TILEABLE square texture of the night ruins' stone, seen straight on (flat front view): dark charcoal-grey cut stone in large worn blocks about the size of the reference's blocks, soot stains, faint cool blue moonlight on the upper edges, a very faint warm bounce from below. LOW CONTRAST. No single standout feature that would visibly repeat.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red or crimson, no saturated cyan or teal.
```

**Reject if:** not seamless; too dark to read (the black cat must still stand out against it); a standout feature.

### 29 · Top lip (walkable edge, night stone)

**Save as:** `Art_Source/Environment/EmberNight/E-11_top_lip_v1.png`, landscape 3:2, transparent above and below the band, tiles sideways. **Attach:** R1, E-01, E-10 fill.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their palette. This place is EMBER NIGHT: ancient ruins at night lit by fire: dark charcoal stone, columns of smoke, glowing embers drifting, iron braziers. Light: a cool silver-blue moon is the key light; every warm light is amber or orange from fire (NEVER red).

Paint the WALKABLE TOP EDGE of a dark stone platform at night as a long horizontal strip in strict side elevation, in the middle third of the image, in the stone of the attached fill: overall level but never ruler-straight, slightly uneven worn capstones with a silver moonlit edge. Along the top, irregular clumps of dry dark grass and a few small glowing embers resting in the joints, bare stone between. Under it the first course of stone, fading to transparent below. About 25 pixels tall on a phone. Seamlessly tileable left to right.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red or crimson, no saturated cyan or teal.
```

**Reject if:** a straight top; an even fringe; red embers; it doesn't tile.

### 30 · Dressing: night pieces

**Save as:** `Art_Source/Environment/EmberNight/E-20_dressing_v1.png`, landscape 3:2, transparent, a sheet. **Attach:** R1, E-01, E-10 fill.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden ruins or their palette. This place is EMBER NIGHT: ancient ruins at night lit by fire: dark charcoal stone, columns of smoke, glowing embers drifting, iron braziers. Light: a cool silver-blue moon is the key light; every warm light is amber or orange from fire (NEVER red).

Paint a sprite sheet of SIX well-separated night dressing pieces, each different. Ground pieces on a flat base line: a small iron brazier on three legs with an amber flame, a pile of broken charred timber, a toppled stone urn with embers spilling out. Hanging pieces from a straight top line: a short rusted chain, an iron lantern with a small amber flame, a tangle of dry blackened roots. None longer than twice its width.

Strict side view, orthographic: no perspective, no vanishing point, no 3/4 angle. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector. Strong, simple silhouettes that are recognisable in black and white. No text, no characters, no animals, no birds, no UI, no border, no red or crimson, no saturated cyan or teal.
```

**Reject if:** pieces overlap; a red flame.

