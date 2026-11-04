# Premium Pack I · Effects (2026-11-03)

Part of `Docs/V_TASKS/PREMIUM_VISUAL_PLAN.md`, Phases 3 and 5. These are the effects `PAX-V06` (Phase 5's game feel) shows when the cat runs and lands, when traps hit and break, and when a level ends. Today's dust (`Traps/Effects/dust_0`–`2`) is three flat cartoon clouds, and there are no debris chunks, sparks or door burst.

Each image is one sheet of separate pieces. Claude Code cuts them apart, so each piece must stand alone with clear space around it.

**What past rounds taught us:**
- **Simple shapes win.** Effects show for a fraction of a second at phone size. Soft, clear shapes read; fine detail is lost.
- **Key colour by piece:**
  - Dust and stone go on flat blue, which Claude Code keys out.
  - Light (sparks, the door burst) goes on flat black: the game adds it as light, so black drops out cleanly.
  - Obsidian's cyan pieces go on flat magenta.
- **Keep the glow small.** A big glow washes out the cat and the hazards behind it.

**How to work:**
- One new chat per image. Attach only the references the prompt names.
- Paste the whole prompt block as it is. Each one is complete.
- Check it small first: shrink it to 400 px wide. If the pieces don't read as separate clear shapes at that size, reject it.
- Save to `Art_Source/Effects/` (a new folder: create it) with the exact name, then tell Claude Code.

**References:**
- **R1:** `Docs/Art/Reference/styleframe_A_v1.png` (painting technique and light)
- **R2:** `Assets/_Game/Art/RealityA/Environment/Kit/ENV_Fill_A.png` (Golden Ruins stone)
- **R3:** `Art_Source/Environment/ObsidianVoid/OV-41_far_skyline_v1.png` (Obsidian Void's colours)

---

### FX-01 · Dust puffs

**Save as:** `Art_Source/Effects/FX-01_dust_v1.png`, wide 3:1, flat blue background. **Attach:** R1.

**In game:** behind the cat's paws when it runs, skids, lands and touches a wall, from about 0.2 u to 1 u wide. They grow and fade in a fraction of a second.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones. Match the attached image's painting technique, light and warm colours.

Paint a sheet of eight separate soft puffs of dry dust kicked up from old stone, in two rows of four, each with clear empty space around it. Top row: four small round puffs, each a little bigger than the last. Bottom row: two low wide puffs spreading sideways along the ground, as from a landing, and two thin trailing wisps, as from a skid. Pale warm sandy dust, lit from the upper left, soft and slightly transparent at the edges, with a gentle denser core. Every puff sits on an invisible flat ground line at its bottom.

Big simple soft shapes that read at phone size. Muted, warm, never saturated, never white. Strict flat side view. Around them the background is one flat pure blue (#0000FF), edge to edge, with no glow and no shadow on the blue. No text, no ground, no stones, no sparkles, no faces. Not a photo, not a 3D render, not a cartoon, no outlines.
```

**Reject if:** the puffs have outlines or look like cartoon clouds; they touch each other; any is white or grey; there's ground painted under them.

### FX-02 · Golden Ruins debris

**Save as:** `Art_Source/Effects/FX-02_debris_golden_v1.png`, wide 2:1, flat blue background. **Attach:** R1, R2.

**In game:** chunks that fly and tumble when a floor collapses or a block breaks, from about 0.1 u to 0.4 u. They spin, so each is seen at every angle.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones. Match the first attached image's painting technique and light, and match the second attached image's stone exactly: its colour and its surface.

Paint a sheet of twelve separate broken chunks of that stone, in three rows of four, each with clear empty space around it: four small chips, four mid-sized broken pieces with one sharp fresh face, and four flat shards. Each has a clear silhouette and a little moss on some of them, lit from the upper left, with a soft dark side. They are seen from the side, loose and falling, not resting on anything.

Big simple shapes that read at phone size. Muted, warm, never saturated. Warm brown shadows, never black. Around them the background is one flat pure blue (#0000FF), edge to edge, with no glow, no dust and no shadow on the blue. No text, no ground, no gems, no gold, no red. Not a photo, not a 3D render, not a cartoon.
```

**Reject if:** the chunks touch; they look smooth like pebbles; there's dust or ground painted around them; the stone doesn't match the second image.

### FX-03 · Sparks

**Save as:** `Art_Source/Effects/FX-03_sparks_v1.png`, wide 3:1, flat black background. **Attach:** R1.

**In game:** added as light where metal meets metal (a spear in a wall, a slam, a hinge), about 0.1 u to 0.6 u. They show for a few frames.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones. Match the attached image's warm light.

Paint a sheet of eight separate bursts of hot metal sparks, in two rows of four, each with clear empty space around it. Top row: four small bursts of a few short bright streaks flying out from one point, each a little bigger than the last. Bottom row: four single fan-shaped sprays thrown to one side, as when metal scrapes. The streaks are thin and straight, hot white at their heads fading to warm orange and then to nothing at their tails, with a tiny soft glow only at the burst's centre.

Simple clean shapes that read at phone size. Around them the background is one flat pure black (#000000), edge to edge, with no glow spreading onto it beyond a few pixels, no smoke and no haze. No text, no metal, no objects, no stars, no red. Not a photo, not a 3D render, not a cartoon.
```

**Reject if:** the glow fills the space between bursts; there's smoke; the streaks are curly or look like fireworks; they're red.

### FX-04 · Impact rings

**Save as:** `Art_Source/Effects/FX-04_impact_v1.png`, wide 3:1, flat blue background. **Attach:** R1.

**In game:** a flat burst of dust along the ground where something heavy lands: a hard landing, a falling block, a slam. About 1 u to 3 u wide and low.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones. Match the attached image's painting technique, light and warm colours.

Paint a sheet of three separate ground impacts seen exactly from the side, one under the other, each with clear empty space around it, from small to large. Each is a low, wide ring of pale warm dust bursting outward to both sides along a flat ground line, thick and dense near the ground and thinning into soft wisps at its two ends and its top, with a clear gap in the middle where the impact was. Lit from the upper left.

Big simple soft shapes that read at phone size. Muted, warm, never saturated, never white. Strict flat side view, no perspective. Around them the background is one flat pure blue (#0000FF), edge to edge, with no glow and no shadow on the blue. No text, no ground, no stones, no objects. Not a photo, not a 3D render, not a cartoon, no outlines.
```

**Reject if:** it's seen from above (an oval); it has outlines; it's taller than about a third of its width; there's ground painted under it.

### FX-05 · Door burst

**Save as:** `Art_Source/Effects/FX-05_door_burst_v1.png`, square 1:1, flat black background. **Attach:** R1, `Assets/_Game/Art/RealityA/Environment/Kit/ENV_Door.png`.

**In game:** added as light over the exit door when the cat reaches it, about 3 u across, swelling and fading in half a second before the level-complete screen. It's also used, smaller, when a checkpoint lights.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones. Match the first attached image's warm light, and match the warm golden light inside the second attached image's doorway.

Paint one soft radiant burst of warm golden light, centred, as if the light inside that doorway has just flared out: a bright, small, warm white core, a few soft broad rays of golden light spreading out from it, of uneven lengths, and a scatter of tiny warm motes drifting outward between the rays. The light fades smoothly to nothing well before the edges of the image.

Simple soft shapes that read at phone size. Warm gold, never orange, never saturated. Around it the background is one flat pure black (#000000), edge to edge, reached well inside the image's edges. No text, no door, no frame, no stone, no stars, no lens flare rings, no red. Not a photo, not a 3D render, not a cartoon.
```

**Reject if:** the light reaches the image's edges; there are lens-flare rings or hexagons; it's orange; the door is painted in it.

### FX-06 · Obsidian shards and sparks

**Save as:** `Art_Source/Effects/FX-06_obsidian_v1.png`, wide 2:1, flat magenta background. **Attach:** R3.

**In game:** Obsidian Void's debris and sparks (L031–L040): black glass shards when a tile shatters, and cold cyan sparks where hard light strikes, about 0.1 u to 0.5 u.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones. Match the attached image's painting technique and colours exactly.

Paint a sheet of twelve separate pieces, in three rows of four, each with clear empty space around it. Top two rows: eight broken shards of black obsidian glass, sharp and angular, from small chips to long splinters, each with one thin cold cyan edge catching the light and a dark blue-black body. Bottom row: four small bursts of short, straight, cold white-cyan spark streaks flying out from one point, with only a tiny glow at the centre.

Simple clean shapes that read at phone size. Dark, cool and calm, the cyan kept to thin edges and the sparks. Around them the background is one flat pure magenta (#FF00FF), edge to edge, with no glow, no mist and no shadow on the magenta. No text, no stars, no objects, no red, no green. Not a photo, not a 3D render, not a cartoon.
```

**Reject if:** the pieces touch; the cyan floods the shards; there's glow or mist on the magenta; anything is red.
