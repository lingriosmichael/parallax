# Premium Pack B · Golden Ruins, three districts (2026-11-02)

Part of `Docs/V_TASKS/PREMIUM_VISUAL_PLAN.md`, Phase 3. L001–L030 stay Golden Ruins, so these make one world feel like a journey through one great city:

| District | Levels | Feel |
|---|---|---|
| **The Gardens** | L001–L010 | Sunlit gates, terraces and old trees |
| **The Overgrown Quarter** | L011–L020 | The jungle has taken the streets: roots, canopy, shade |
| **The High Citadel** | L021–L030 | Bronze, towers and a long climb to the summit |

**Paint in this order:** Group 1, then 2, then 3. If a group comes back wrong, stop and tell Claude Code before the next one.

**How to work:**
- One new chat per image. Attach only what each item names, never saturated trap art.
- Check it small first: shrink to 400 px wide. If it isn't one clear shape, reject it.
- Save to `Art_Source/Environment/GoldenRuins/` with the exact name, then tell Claude Code when a group is in.

**Background:** flat pure blue (#0000FF). Green would eat the moss, and magenta tints the edges pink.

**No banners and no red cloth in any of these,** because red is the danger colour and the banner colour is still being decided.

**References:**
- **R1:** `Docs/Art/Reference/styleframe_A_v1.png` (painting technique and light)
- **R2:** `Assets/_Game/Art/RealityA/Environment/Kit/ENV_Fill_A.png` (the play layer's stone)
- **R3:** `Assets/_Game/Art/RealityA/Environment/Backgrounds/Kit/ENV_MidTowers.png` (how a distant piece sits in the game)

**In game:** the view is about 20 u wide. Background pieces show about 8–12 u tall behind the play layer, and play-layer pieces sit on stone.

---

## Group 1 · One landmark per district (background)

Hard, clean outer edges on all three: no mist or glow around them, because the game adds its own fog.

### DL-01 · The Garden Gate (The Gardens, ends at L010)

**Save as:** `DL-01_garden_gate_v1.png`, portrait 3:4, blue background. **Attach:** R1, R3.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones. Match the attached images' painting technique, warm light and level of detail.

Paint one distant landmark: a colossal ruined ceremonial gate of weathered sandstone, two great towers joined by a broken arch, with terraced gardens and old trees spilling down its base. It stands on its own floating rock with a rough underside. Low warm sun from behind and to one side, warm brown shadows, never black.

One big simple silhouette that reads at phone size. Muted, warm and hazy with distance. Strict side view, no perspective. The whole piece is fully inside the image with space around it.

The background is one flat pure blue (#0000FF), edge to edge. The piece has clean hard edges against the blue, with no mist, no glow, no clouds and no shadow on the blue. No banners, no red cloth, no text, no people, no animals. Not a photo, not a 3D render, not a cartoon.
```

**Reject if:** it has fog around it; it has red; it's more than one shape; it looks like a castle from a fairy tale.

### DL-02 · The Great Fig (The Overgrown Quarter, ends at L020)

**Save as:** `DL-02_great_fig_v1.png`, portrait 3:4, blue background. **Attach:** R1, R3.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones. Match the attached images' painting technique, warm light and level of detail.

Paint one distant landmark: a gigantic ancient tree that has swallowed a ruined temple. Its pale roots pour down over the broken stone walls and arches like wax, and its huge dark green canopy spreads wide above. It stands on its own floating rock with a rough underside. Low warm sun from behind and to one side, warm brown shadows, never black.

One big simple silhouette that reads at phone size. Muted, deep green and warm stone, hazy with distance. Strict side view, no perspective. The whole piece is fully inside the image with space around it.

The background is one flat pure blue (#0000FF), edge to edge. The piece has clean hard edges against the blue, with no mist, no glow, no clouds and no shadow on the blue. No banners, no red cloth, no text, no people, no animals. Not a photo, not a 3D render, not a cartoon.
```

**Reject if:** the leaves are a fuzzy cloud with no shape; the temple has vanished; it has fog around it.

### DL-03 · The Summit Citadel (The High Citadel, ends at L030)

**Save as:** `DL-03_summit_citadel_v1.png`, portrait 3:4, blue background. **Attach:** R1, R3.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones. Match the attached images' painting technique, warm light and level of detail.

Paint one distant landmark: a soaring ancient citadel of pale sandstone and aged bronze, climbing in tiers to one tall spire with a great bronze dome, the highest place in a floating city. It stands on its own floating rock with a rough underside. Low warm sun from behind and to one side, warm brown shadows, never black.

One big simple silhouette that reads at phone size. Muted and warm, hazy with distance, with the bronze dull and old, never shiny. Strict side view, no perspective. The whole piece is fully inside the image with space around it.

The background is one flat pure blue (#0000FF), edge to edge. The piece has clean hard edges against the blue, with no mist, no glow, no clouds and no shadow on the blue. No banners, no red cloth, no text, no people, no animals. Not a photo, not a 3D render, not a cartoon.
```

**Reject if:** it looks like a modern tower; the dome shines like gold; it has fog around it.

---

## Group 2 · Plants for the play layer

These replace the repeated look along ledges. Each image holds separate pieces, so one painting gives several.

### FL-01 · Leaf clusters on ledges

**Save as:** `FL-01_leaf_clusters_v1.png`, landscape 3:2, blue background. **Attach:** R1.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones. Match the attached image's painting technique and warm light.

Paint FOUR separate clusters of broad jungle leaves and ferns on one image, side by side with clear space between them, none touching, each a different shape and width. Each cluster has a flat base, as if growing from the top of a stone ledge, and leaves that arch up and droop over.

Warm olive and deep green, softly lit from behind and above with a thin warm rim of light on the leaf edges. Warm dark shadows, never black. Big simple leaf shapes that read at phone size. Strict side view.

The background is one flat pure blue (#0000FF), edge to edge, with no glow and no shadow on the blue. No stone, no soil, no flowers, no text, no animals. Not a photo, not a 3D render, not a cartoon, no outlines.
```

**Reject if:** the clusters touch; the leaves are tiny; it's bright lime green; it has no flat base.

### FL-02 · Hanging ivy curtains

**Save as:** `FL-02_ivy_curtains_v1.png`, landscape 3:2, blue background. **Attach:** R1.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones. Match the attached image's painting technique and warm light.

Paint FOUR separate hanging curtains of ivy and thin vines on one image, side by side with clear space between them, none touching, each a different width and length. Each hangs from a flat straight top edge and ends in uneven trailing strands, so no two bottoms line up.

Warm olive green with some dry golden leaves, softly lit, with warm dark shadows, never black. Airy, with gaps between the strands. Big simple shapes that read at phone size. Strict side view.

The background is one flat pure blue (#0000FF), edge to edge, with no glow and no shadow on the blue. No stone, no flowers, no text, no animals. Not a photo, not a 3D render, not a cartoon, no outlines.
```

**Reject if:** the strands end level, like a comb; it's a solid mass; the curtains touch.

### FL-03 · Great roots over stone

**Save as:** `FL-03_ledge_roots_v1.png`, landscape 3:2, blue background. **Attach:** R1, R2.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones. Match the attached images' painting technique, warm light and stone colour.

Paint THREE separate groups of old tree roots on one image, side by side with clear space between them, none touching, each a different width. Each group hangs from a flat straight top edge and spreads down in about four thick roots that fork into thinner ones, with a little moss along them.

Warm grey-brown, softly lit from the upper left, with warm brown shadows, never black. Big simple shapes that read at phone size. Strict side view. Only the roots and moss are painted, not the stone.

The background is one flat pure blue (#0000FF), edge to edge, with no glow and no shadow on the blue. No leaves, no stone, no text, no animals. Not a photo, not a 3D render, not a cartoon, no outlines.
```

**Reject if:** the roots look like tentacles; it's a dense tangle; there's a stone wall behind them.

---

## Group 3 · Stone and bronze for the play layer

### SB-01 · Broken slab undersides

Replaces the same torn-paper edge under every platform.

**Save as:** `SB-01_slab_undersides_v1.png`, landscape 3:2, blue background. **Attach:** R1, R2.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones. Match the attached images' painting technique, warm light and stone exactly.

Paint FOUR separate pieces of the underside of an ancient stone platform on one image, stacked one above the other with clear space between them, none touching, each a wide horizontal strip about six times as wide as it is tall. Each has a flat straight top edge, and below it broken stone: uneven hanging blocks, a few carved corbels, cracked courses and missing stones, with a very different outline on each strip.

Warm sandstone, lit from above so the lower edges fall into warm brown shadow, never black. Big simple shapes that read at phone size. Strict flat side view, no perspective.

The background is one flat pure blue (#0000FF), edge to edge, with no glow and no shadow on the blue. No plants, no text, no animals. Not a photo, not a 3D render, not a cartoon, no outlines.
```

**Reject if:** the bottom edge is an even wave; the strips touch; the stone doesn't match R2.

### SB-02 · Bronze machinery for The High Citadel

These go on honest moving pieces only, in L021–L030. Disguised traps keep their host's stone.

**Save as:** `SB-02_bronze_faces_v1.png`, landscape 3:2, blue background. **Attach:** R1, R2.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones. Match the attached images' painting technique and warm light, and the second image's stone.

Paint THREE separate square panels on one image, side by side with clear space between them, none touching: heavy ancient temple machinery of worn sandstone blocks strapped with dull aged bronze bands and a few large rivets. The first is a plain crushing block, the second has a simple round bronze boss at its centre, and the third has two bronze rails running top to bottom.

Dull, dark, centuries old, softly lit from above, warm brown shadows, never black. Simple shapes that read at phone size, with no fine detail. Strict flat front view, no perspective.

The background is one flat pure blue (#0000FF), edge to edge, with no glow and no shadow on the blue. No red, no gems, no glowing parts, no plants, no text, no animals. Not a photo, not a 3D render, not a cartoon, no outlines.
```

**Reject if:** the bronze shines; it has gears like a clock; it has red or gems; it looks like a sci-fi panel.

---

## Group 3, round 2 (2026-11-02)

**What went wrong in round 1:** SB-01 and SB-02 came back as smooth, pillowy, bright orange blocks, like a casual mobile game, about twice as bright as the game's stone. That was the prompts' fault: "simple shapes" and "no fine detail" asked for it. These two sit right beside the real stone, so they have to match its grit.

**Attach R2 only, in a new chat.** Don't attach round 1 or the styleframe, and name the files `_v2`.

### SB-01 · Broken slab undersides, round 2

**Save as:** `SB-01_slab_undersides_v2.png`, landscape 3:2, blue background. **Attach:** R2.

```text
The attached image is the exact stone of my game: rough, dark, pitted, weathered masonry with sharp broken corners, deep dark joints, lichen and grime. Paint in exactly this material, colour and brightness. Keep it as dark as the attached image, and keep its gritty painted texture.

Paint FOUR separate pieces of the underside of an ancient stone platform on one image, stacked one above the other with clear space between them, none touching, each a wide horizontal strip about six times as wide as it is tall. Each has a flat straight top edge. Below it is heavy broken masonry: blocks that have slipped and hang unevenly, snapped carved corbels, cracked courses, deep dark gaps where stones are missing, grime running down, and dark moss in the cracks. Every strip has a very different outline.

Lit softly from above, so the lower edges fall into deep warm brown shadow. Corners are sharp, chipped and irregular, never rounded or soft. Strict flat side view, no perspective.

The background is one flat pure blue (#0000FF), edge to edge, with no glow and no shadow on the blue. No plants, no text, no animals. Not smooth, not clean, not a casual game, not a cartoon, no outlines, no orange.
```

**Reject if:** it's brighter than R2; the corners are rounded; the blocks look puffy or clean; the bottom edge is an even wave.

### SB-02 · Bronze machinery, round 2

**Save as:** `SB-02_bronze_faces_v2.png`, landscape 3:1, blue background. **Attach:** R2.

```text
The attached image is the exact stone of my game: rough, dark, pitted, weathered masonry with sharp broken corners, deep dark joints, lichen and grime. Paint the stone parts in exactly this material, colour and brightness. Keep them as dark as the attached image.

Paint THREE separate square panels on one image, side by side with clear space between them, none touching: heavy ancient temple crushing machinery. Massive blocks of that dark stone, chipped at the corners, are bound by thick black-green corroded bronze straps, with heavy worn bolts, verdigris streaks running down the stone, rust stains and old scrape marks from centuries of grinding. The first is a plain crushing block, the second has a great corroded bronze boss at its centre, and the third has two bronze guide rails running top to bottom.

Dark, heavy and dangerous-looking, softly lit from above, with deep warm brown shadows. Strict flat front view, no perspective. The outer corners are chipped and uneven, never rounded.

The background is one flat pure blue (#0000FF), edge to edge, with no glow and no shadow on the blue. No red, no gems, no glowing parts, no plants, no text, no animals. Not shiny, not clean, not a casual game, not a cartoon, no outlines, no orange.
```

**Reject if:** it looks like a treasure chest or a crate; it's orange; the bronze is shiny or gold; the corners are rounded.

