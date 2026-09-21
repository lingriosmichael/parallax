# PAX-A02 — Stage 2: Art generation and processing

**Date:** 2026-09-18
**Precondition:** Stage 1 committed (slots, manifest, importer, parallax, V2 atlases, lights).
**Goal:** Fill all 23 slots in `Docs/Art/PAX-A02_manifest` (FG_01 may be deferred) with approved art,
processed to exact slot size by a script. No hand-cropping, no hand-editing of scene files.

Stage 2 has three parts:

| Part | Who | What |
|---|---|---|
| 2a | Claude Code | Build the slot-processing tool, apply the manifest amendments, calibrate the lights |
| 2b | You (ChatGPT) and Claude Code | 14 manual slots: you generate raws, the tool processes them, you approve each contact sheet |
| 2c | Claude Code (AutoSprite) and you | 9 object slots: candidates, contact sheet, your approval, then processing |

Stage 3 (wiring verification, Editor screenshots, review, commit) follows the ticket as written.

---

## 0. Before you start

1. **Reference images** in `Docs/Art/Reference/` (LFS): at least `03_vine-elevator-anchor_v2.png`,
   `04_gravity-shift.png`, `14_environment-breakdown.png`, and the key art if you have it.
   Attach the matching reference to **every** ChatGPT prompt below.
2. **AutoSprite key** only in your local Claude Code config. Never in the repo. Before each commit:
   `git diff --cached | grep -n "vspk_" || echo "no keys"`.
3. **Raw files** go to `Art_Source/Environment/A/` and `Art_Source/Environment/B/` (outside `Assets/`,
   so Unity never imports them; `*.png` is LFS-tracked). Naming: `<SlotFile>__raw_v<N>.png`,
   e.g. `A_BG_00_Sky__raw_v1.png`. Keep rejected versions; they cost nothing and document the choice.

---

## 1. Manifest amendments (Architect decisions, applied by Claude Code in 2a)

These change Stage 1's manifest. Each one comes from what the generator can actually produce.

| # | Change | Why |
|---|---|---|
| M1 | **Background strips 4096 → 2048 px wide** (heights stay 1079 / 1171 / 1356) | The generator's widest output is 1536 px. 4096 would need ~2.7× upscaling and look soft. 2048 px at 98.333 PPU = 20.8u per tile; with 3 tiles centred on the camera, coverage is at least one tile width on each side of the camera, which beats the 11.1u half-view. Also halves memory. |
| M2 | **GAME_Wall 197 × 1770 → 394 × 394 px** (2 × 2u, seamless on both axes) | A 1:9 image can't be generated. Tiled draw mode repeats it anyway, so a seamless square material looks identical and weighs a tenth. |
| M3 | **GAME_Platform_Fill 787 × 197 → 394 × 394 px** (2 × 2u, seamless on both axes) | Same reason as M2: materials are generated square. Squashing a square into 4:1 distorts it; Tiled draw mode crops the square cleanly to 1u (ground) or 0.5u (platforms). |
| M4 | **Platform_Top: no lip.** Surface line = top pixel row, nothing above it. Grass, highlights and chips are drawn *below* the line. | Setup places the sprite's top edge on the collider top. A lip above the top row can't exist inside the sprite, and a custom pivot would still be placed top-to-collider. Keeps "visible surface = walkable surface" exact. |
| M5 | New manifest column **Raw source** (raw file path + prompt ID from this document) | Provenance for every approved slot. |

---

## 2. Global art rules (apply to every slot, both tools)

- **View:** strict side view, orthographic, no perspective, no vanishing points, no camera tilt.
- **Readability over detail:** strong silhouettes, restrained texture, readable at phone scale.
  In-game style evokes the key art with fewer layers; painterly depth is not required.
- **Value structure:** backgrounds are lower contrast and less saturated than gameplay surfaces.
  Gameplay surfaces are mid-value so the cat and its outline read against them.
- **Protect the cat outlines:** Cat A's outline is orange (≈ #FFBF4D), Cat B's is cyan (≈ #59D9FF).
  - Reality A gameplay surfaces: sandstone in **browns and dusty ochres**, darker than the outline. Bright gold stays in the sky.
  - Reality B gameplay surfaces: **dark obsidian**. Cyan edge light is a thin line only; the cat's outline must stay the brightest cyan on screen.
- **Nothing that reads as gameplay** in backgrounds: no platforms, ledges or doors at gameplay depth,
  nothing the player could mistake for a surface.
- **Never:** text, letters, numbers, logos, UI, frames, borders, signatures, characters, animals, cats.
- **Transparency via key colour:** where a slot needs transparency, the prompt asks for a flat
  **pure magenta #FF00FF** background. The tool keys it out. Magenta is far from both palettes.
- **Quiet edges for horizontal tiling:** tileable strips keep the left and right 15 % "quiet"
  (sky, mist, soft rock), with no distinct object cut by the edge. The tool blends the seam there.

### Style blocks (paste at the start of each prompt)

**STYLE A**
> Painterly but clean 2D game art for a mobile puzzle platformer, strict side view, orthographic, no perspective. Reality A is warm and organic: sandstone, roots, ruins, cloth banners, vegetation, floating architecture, clouds, warm golden light, dark reflective water. Strong simple silhouettes, restrained detail, readable at phone scale. Match the style and palette of the attached reference. No text, no characters, no animals, no UI, no border, no signature.

**STYLE B**
> Painterly but clean 2D game art for a mobile puzzle platformer, strict side view, orthographic, no perspective. Reality B is cold and geometric: obsidian, glass, grids, thin cyan edge light, void, monoliths, wireframes, cold mist, stars. Strong simple silhouettes, restrained detail, readable at phone scale. Match the style and palette of the attached reference. No text, no characters, no animals, no UI, no border, no signature.

### ChatGPT settings

- Format: **landscape 3:2 (1536 × 1024)** unless the prompt says square.
- Attach the reference named in the prompt.
- If the result has perspective, text or a border: regenerate, don't fix by hand.
- Save to `Art_Source/Environment/<A|B>/<SlotFile>__raw_v<N>.png`.

---

## 3. The 14 manual prompts (Part 2b)

Mode = how the tool processes the raw (see §4). Target = slot size after processing.

### Reality A

**P-A1 · A_BG_00_Sky.png** · Mode `sky` · Target 2048 × 1079 · Ref `14_environment-breakdown`
> STYLE A. A wide, calm sky backdrop: warm golden late-afternoon light, soft layered clouds, gentle gradient from pale gold at the horizon to warm peach higher up. Very low contrast, soft focus, no objects, no ruins, no ground, no horizon line. The left and right edges are plain sky of the same tone so the image can repeat horizontally. Fill the whole frame.

**P-A2 · A_BG_01_Far.png** · Mode `strip` · Target 2048 × 1171 · Ref `14_environment-breakdown`
> STYLE A. Distant floating sandstone ruins seen far away through haze: a loose row of small floating islands with broken arches and a few hanging roots, all in the lower two thirds of the frame. Low contrast, hazy, desaturated warm tones, soft edges. Keep the left and right 15 % of the image empty. Everything that is not ruins is flat pure magenta #FF00FF, with no gradient and no shadow on the background.

**P-A3 · A_MG_01_Mid.png** · Mode `strip` · Target 2048 × 1356 · Ref `14_environment-breakdown`
> STYLE A. Mid-distance layer: tall sandstone arches and columns with roots and a few cloth banners, standing along the bottom half of the frame, tops rising to about two thirds of the height. Medium-low contrast, slightly hazy, warmer and more detailed than the far layer but clearly behind the gameplay. No walkable-looking ledges. Keep the left and right 15 % free of arches (low bushes or mist are fine there). Everything else is flat pure magenta #FF00FF.

**P-A4 · A_GAME_Platform_Top.png** · Mode `edge` · Target 787 × 49 · Ref `14_environment-breakdown`
> STYLE A. A long horizontal sandstone ledge edge seen exactly from the side, running straight across the full width of the image. The top edge is one perfectly straight, flat horizontal line: the walking surface. Nothing rises above that line — no grass, no stones, no roots above it. Directly under the line: a thin band of worn sandstone with a few small moss patches and hairline cracks, then it ends in a slightly irregular lower edge. The band is about one twentieth of the image height, centred vertically. Dusty ochre and brown, darker than bright gold. Everything outside the band is flat pure magenta #FF00FF.

**P-A5 · A_GAME_Platform_Fill.png** · Mode `material` · Target 394 × 394 · Ref `14_environment-breakdown`
> STYLE A. Seamless square texture of weathered sandstone blocks seen from the side: large soft-edged blocks, faint horizontal strata, a few small root threads. Even lighting, no strong shadows, no highlights at the edges, no border. Mid-value dusty brown and ochre. The texture must tile seamlessly in both directions. Fill the whole frame. **Format: square 1024 × 1024.**

**P-A6 · A_GAME_Wall.png** · Mode `material` · Target 394 × 394 · Ref `14_environment-breakdown`
> STYLE A. Seamless square texture of an old sandstone wall seen from the side: stacked worn blocks with a few vertical root strands and small vegetation in the joints. Even lighting, no strong shadows, no border. Mid-to-dark warm brown, slightly darker than the platform texture. Must tile seamlessly in both directions. Fill the whole frame. **Format: square 1024 × 1024.**

**P-A7 · A_OBJ_Hazard.png** · Mode `hazard` · Target 787 × 197 · Ref `14_environment-breakdown`
> STYLE A. The edge of dark reflective water seen from the side: the water surface is one straight horizontal line across the full width near the top of a band; below it, deep dark water with a faint warm reflection of golden light and soft ripples, fading to transparent at the bottom. The band is about one fifth of the image height, centred. Keep the left and right 15 % calm and even. Everything outside the band is flat pure magenta #FF00FF.

### Reality B

**P-B1 · B_BG_00_Sky.png** · Mode `sky` · Target 2048 × 1079 · Ref `14_environment-breakdown`
> STYLE B. A wide, calm void backdrop: deep blue-black space with sparse small stars and a very faint cold nebula haze. Very low contrast, no objects, no monoliths, no grid, no horizon. The left and right edges are plain dark void of the same tone so the image can repeat horizontally. Fill the whole frame.

**P-B2 · B_BG_01_Far.png** · Mode `strip` · Target 2048 × 1171 · Ref `14_environment-breakdown`
> STYLE B. Distant obsidian monoliths floating far away in cold mist: a loose row of tall dark slabs of different heights in the lower two thirds of the frame, each with a very faint thin cyan edge. Low contrast, hazy, desaturated. Keep the left and right 15 % of the image empty. Everything that is not monoliths or mist is flat pure magenta #FF00FF.

**P-B3 · B_MG_01_Mid.png** · Mode `strip` · Target 2048 × 1356 · Ref `14_environment-breakdown`
> STYLE B. Mid-distance layer: a faint wireframe grid structure and a few glass-and-obsidian pillars along the bottom half of the frame, tops at about two thirds of the height, with thin muted cyan lines. Medium-low contrast, clearly behind the gameplay, no walkable-looking ledges. Keep the left and right 15 % free of pillars (thin mist is fine). Everything else is flat pure magenta #FF00FF.

**P-B4 · B_GAME_Platform_Top.png** · Mode `edge` · Target 787 × 49 · Ref `14_environment-breakdown`
> STYLE B. A long horizontal obsidian ledge edge seen exactly from the side, running straight across the full width of the image. The top edge is one perfectly straight, flat horizontal line: the walking surface, marked by a single thin cyan edge-light line exactly on it. Nothing rises above that line. Below it: a thin band of polished black obsidian with a subtle glassy sheen and a few faint geometric facets, ending in a straight lower edge. The band is about one twentieth of the image height, centred vertically. Everything outside the band is flat pure magenta #FF00FF.

**P-B5 · B_GAME_Platform_Fill.png** · Mode `material` · Target 394 × 394 · Ref `14_environment-breakdown`
> STYLE B. Seamless square texture of dark obsidian blocks seen from the side: large clean geometric blocks, very subtle glassy reflections, faint thin grid seams. Even lighting, no bright highlights, no border, no cyan glow. Dark blue-black but not pure black, so details stay visible. Must tile seamlessly in both directions. Fill the whole frame. **Format: square 1024 × 1024.**

**P-B6 · B_GAME_Wall.png** · Mode `material` · Target 394 × 394 · Ref `14_environment-breakdown`
> STYLE B. Seamless square texture of an obsidian wall seen from the side: tall stacked slabs with sharp faceted joints and a faint wireframe pattern etched in. Even lighting, no glow, no border. Slightly darker than the platform texture. Must tile seamlessly in both directions. Fill the whole frame. **Format: square 1024 × 1024.**

**P-B7 · B_OBJ_Hazard.png** · Mode `hazard` · Target 787 × 197 · Ref `14_environment-breakdown`
> STYLE B. The edge of the void seen from the side: a straight horizontal boundary line across the full width near the top of a band, drawn as a thin broken cyan line; below it, cold mist and faint falling particles dissolving into darkness, fading to transparent at the bottom. The band is about one fifth of the image height, centred. Keep the left and right 15 % calm and even. Everything outside the band is flat pure magenta #FF00FF.

---

## 4. Part 2a — Claude Code prompt: tooling, manifest, lights

```
PAX-A02 — STAGE 2a. Tooling, manifest amendments, light calibration. No art generation. Do not commit.
Read Docs/A_TASKS/PAX-A02.md, the Stage 1 manifest in Docs/Art/, and this file (PAX-A02_Stage2.md,
place it at Docs/A_TASKS/PAX-A02_Stage2.md).

1. Manifest amendments M1–M5 from §1 of this file. Update the targets, the paragraph and add the
   "Raw source" column (empty for now).

2. Slot-processing tool: Tools/Art/prepare_slot.py (Python 3, Pillow + numpy), local venv at
   Tools/Art/.venv (gitignored), requirements.txt with pinned versions.
   CLI: prepare_slot.py --raw <path> --slot <SlotFile> --mode <sky|strip|material|edge|hazard|object>
        [--width W --height H] (default: read from the manifest) [--dry-run]
   Pipeline:
   a. Load RGBA. For keyed modes (strip, edge, hazard, object): key out magenta #FF00FF with a soft
      threshold (distance in RGB), alpha ramp over the edge, and despill (remove magenta tint from edge
      pixels). Report the % of pixels keyed.
   b. Crop:
      - sky/strip: largest centred crop with the target aspect; for strip, align the bottom of the
        content to the bottom of the crop.
      - material: centre square, then scale.
      - edge: detect the surface line = the first row from the top where ≥ 60 % of the columns are opaque.
        Trim everything above it (M4: no lip). Crop horizontally to the opaque span, then scale so the
        band fills the target height with the surface line on row 0.
      - hazard: same surface-line rule, surface on row 0; content fades below.
      - object: trim to the alpha bounding box, scale to fit the target, keep the aspect, pad transparent,
        place according to the manifest pivot (bottom centre / centre / top centre).
   c. Resize with Lanczos to the exact target size.
   d. Seams: sky/strip/edge/hazard — make horizontally seamless by cross-fading a band of 12 % of the
      width (wrap-around blend inside the quiet edges). material — seamless on both axes (same blend on X
      and Y). Blend RGB and alpha.
   e. Checks (fail loudly, write nothing): exact size; seam metric = mean absolute difference between the
      first and last column (and row for material) below a threshold (report the value); for keyed modes no
      remaining pixel with >10 % magenta tint; edge/hazard: row 0 is ≥ 60 % opaque and no opaque pixel
      exists above it (trivially true after trim, but assert it).
   f. Write to the slot path in Assets/_Game/Art/Reality<A|B>/Environment/[Backgrounds/].
   g. Contact sheet for approval: ~/Desktop/PAX-A02_contact/<SlotFile>.png showing the raw (scaled down),
      the processed slot, and a 3× tiled preview (tileable modes: 3 × 1; material: 3 × 3) on mid-grey and
      on a checkerboard; for edge/hazard, a red 1-px line marking row 0.
   h. Append provenance to the manifest's Raw source column (raw path, prompt ID from this file, date).
   Unit tests: Tools/Art/tests/ (pytest) for keying, surface-line detection, seam blend (first and last
   column equal after blending), exact output sizes, and object pivot placement. Use generated synthetic
   images, not real art.

3. Light calibration (both global lights, via EnvironmentArtSetup, idempotent):
   - Rationale: the art is painted with its final palette; the global light should not multiply
     Reality B down to 35 %.
   - A global: colour (1.00, 0.95, 0.88), intensity 1.0. B global: colour (0.78, 0.86, 1.00), intensity 0.9.
     Cyan accents unchanged.
   - Put these values in named constants at the top of EnvironmentArtSetup with a comment pointing to this file.

4. .gitignore: Tools/Art/.venv/ and __pycache__. Confirm Art_Source/**/*.png is LFS
   (git check-attr filter).

5. Run pytest and EditMode (baseline 173). Run the Environment Art setup twice (the second run reports
   no changes), validator, save the scene.

OUTPUT: ~/Desktop/PAX-A02_stage2a_review.txt =
{ git status; git --no-pager diff; cat <new files>; } plus pytest output, setup logs, validator,
EditMode count. Then stop.
```

---

## 5. Part 2b — Per-slot loop for the 14 manual slots

For each prompt P-A1 … P-B7:

1. You generate in ChatGPT with the reference attached and save the raw to `Art_Source/Environment/...`.
2. You tell Claude Code: `Process slot <SlotFile> from <raw path>, prompt <ID>.`
   It runs `prepare_slot.py` and opens the contact sheet.
3. You approve or reject on the contact sheet. The criteria:
   - The seam is invisible in the 3× tiled preview.
   - No magenta fringe on the checkerboard.
   - Edge/hazard: the red row-0 line sits exactly on the surface, and nothing is above it.
   - No perspective, no text, no objects that look walkable in backgrounds.
   - Value: gameplay surfaces darker than the cat outline colour for that reality.
4. Rejected: generate v2 (adjust the prompt; record the adjustment next to the prompt in this file).
5. Approved: leave it. Claude Code does not commit per slot.

Suggested order (each step makes the next easier to judge): Fill → Wall → Top → Hazard → Sky → Far → Mid,
first A, then B.

---

## 6. Part 2c — AutoSprite object slots (9 slots)

Follow the AutoSprite procedure in `Docs/A_TASKS/PAX-A02.md` for generation. This section adds the
subject lines, pair-motif rules and the approval gate.

**Pair motifs (the cross-reality language).** Anchor-linked objects share one glyph, in each reality's
own material. This is how the player learns "these two are connected":

| Anchor | Reality A object | Reality B object | Glyph |
|---|---|---|---|
| Pathway01 | A_OBJ_Vine | B_OBJ_Elevator | **Leaf-notch**: a simple leaf shape with one V-notch cut into its tip |
| Gate01 | A_OBJ_Plate | B_OBJ_Gate | **Split-chevron**: an upward chevron split vertically by a thin gap |

The glyph is carved or inlaid in A (sandstone, wood, fibre) and edge-lit in B (thin cyan lines).
Same proportions in both; it must be recognisable at 30 px.

**Subject lines** (append STYLE A or STYLE B from §2; transparent background, or magenta if the
tool can't output transparency; strict side view):

| Slot | Target | Pivot | Subject |
|---|---|---|---|
| A_OBJ_Vine | 236 × 433 | bottom centre | A thick hanging jungle vine with a large knot at the bottom that looks pullable; the leaf-notch glyph carved into the knot. |
| A_OBJ_Plate | 236 × 30 | bottom centre | A flat sandstone pressure-plate surround, very low, seen exactly from the side; split-chevron glyph inlaid on its front face. |
| A_OBJ_Station | 236 × 315 | bottom centre | A small sandstone pedestal with a round bronze dial on top, a gravity control shrine; roots around the base. |
| A_OBJ_Checkpoint | 295 × 393 | bottom centre | A short standing sandstone post with a small cloth banner, warm and inviting. |
| A_FG_01 (optional) | 787 × 590 | top centre | Hanging roots and a torn cloth banner hanging from the top edge, dark silhouette, very simple. |
| B_OBJ_Elevator | 393 × 79 | centre | A thin floating obsidian platform slab with a thin cyan edge line; the leaf-notch glyph edge-lit in the centre of its front face. |
| B_OBJ_Gate | 98 × 1007 | bottom centre | A tall, narrow vertical obsidian gate bar with faceted edges; split-chevron glyph edge-lit near the top. |
| B_OBJ_Checkpoint | 295 × 393 | bottom centre | A short obsidian monolith with a thin cyan vertical line, cold and calm. |
| B_FG_01 (optional) | 787 × 590 | top centre | Floating glass shards and cold mist hanging from the top edge, dark silhouette, very simple. |

**Approval gate per slot:** Claude Code generates 3–4 candidates, runs them through
`prepare_slot.py --mode object` (dry run) and builds one contact sheet with the candidates numbered
side by side, each on mid-grey and checkerboard, at 1× and at phone scale. You answer with a number
or "reject". Only the approved candidate is written to the slot. Rejected candidates are not saved in
the repo.

Readability check for objects: at phone scale, the glyph is recognisable and the object's silhouette
does not merge with the platform texture behind it.

**Claude Code prompt for 2c** (after 2a is committed or at least reviewed):

```
PAX-A02 — STAGE 2c. AutoSprite object slots. Do not commit.
Follow the AutoSprite procedure in Docs/A_TASKS/PAX-A02.md and §6 of Docs/A_TASKS/PAX-A02_Stage2.md.
The key is in the local config only; never print it, never write it to a file in the repo.
For each slot in §6 (skip the two FG_01 slots unless told otherwise):
  generate 3–4 candidates → prepare_slot.py --mode object --dry-run → one numbered contact sheet in
  ~/Desktop/PAX-A02_contact/<SlotFile>_candidates.png → STOP and wait for my number or "reject".
On approval: process the chosen candidate for real (writes the slot and the provenance), then continue
with the next slot. Do not delete the approved candidate's raw; keep it in Art_Source/Environment/<A|B>/.
Before finishing: git diff | grep -n "vspk_" must print nothing.
```

---

## 7. Stage 2 done when

- All 21 required slots are present (the two FG_01 slots are optional) and each has a Raw source entry.
- `PARALLAX/Setup/Environment Art`: all slots found, greybox renderers disabled for filled geometry,
  the second run reports no changes, `Reality isolation: OK`.
- `pytest` and EditMode green.
- `git lfs status` lists every new PNG as LFS; no `vspk_` anywhere in the diff.

Then Stage 3 as in the ticket: Editor screenshots per reality (F1 hides the labels), acceptance run,
review file, commit. Deferred to the PAX-037 device session: atlas FPS, background ASTC banding,
readability of both cat outlines against the new surfaces.
