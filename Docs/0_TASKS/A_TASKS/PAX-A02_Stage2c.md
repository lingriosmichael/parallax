# PAX-A02 — Stage 2c (revised): object slots via ChatGPT

**Change from the original plan:** AutoSprite is an animation tool and has no API in this session. The nine
object slots are static images, so they are generated exactly like the Stage 2b manual slots: you generate in
ChatGPT on a magenta background, Claude Code processes with `prepare_slot.py --mode object`.

Raws go to `Art_Source/Environment/<A|B>/<SlotFile>__raw_v<N>.png`, e.g. `A_OBJ_Vine__raw_v1.png`.

---

## Rules for every object prompt

- Start with: **"Use the attached image only as a style, palette and material reference. Do not copy its
  composition."** Attach for A: your processed `A_GAME_Platform_Fill.png` (after the tint) or the breakdown
  sheet. For B: your `B_GAME_Platform_Fill.png`.
- Strict side view, orthographic, no perspective, no ground shadow, no cast light on the background.
- Background: **flat, solid, pure magenta #FF00FF**, no gradient, no glow, no shadow.
- No text, no characters, no animals, no UI, no border, no signature.
- **Draw the object at its own proportions** (see "Shape" per slot). The tool trims to the visible shape and
  scales it, so an object drawn too square stays too square.
- Simple, readable silhouette. Every object has to be recognisable at phone scale, which is roughly a third of
  the size it looks in the image.

### Pair motifs (the cross-reality language)

| Anchor | Reality A | Reality B | Glyph |
|---|---|---|---|
| Pathway01 | A_OBJ_Vine | B_OBJ_Elevator | **Leaf-notch**: a simple leaf shape with a V-notch cut into its tip |
| Gate01 | A_OBJ_Plate | B_OBJ_Gate | **Split-chevron**: an upward chevron split vertically by a thin gap |

The glyph is carved or inlaid in A, and edge-lit with a thin cyan line in B. Same proportions in both, and it
must still be recognisable at 30 px.

---

## Reality A (attach the A reference)

**O-A1 · A_OBJ_Vine.png** · 236 × 433 px (1.2 × 2.2u) · pivot bottom centre · Shape: tall, about 1:2
> A thick hanging jungle vine seen from the side, running from the top of the image downward, ending in a large
> round knot at the bottom that clearly looks pullable. A few leaves along the vine. Carved into the front of the
> knot: a simple leaf shape with a V-shaped notch cut into its tip. Warm sandstone-and-green palette like the
> reference. Landscape or portrait format, but draw the vine tall and narrow, roughly twice as tall as it is wide.
> Everything around it is flat pure magenta #FF00FF.

**O-A2 · A_OBJ_Plate.png** · 236 × 30 px (1.2 × 0.15u) · pivot bottom centre · Shape: very flat, about 8:1
> A flat sandstone pressure plate seen exactly from the side: a very low, wide stone slab lying on the ground,
> with a slightly raised rim. On its front face, inlaid in darker stone: an upward-pointing chevron split
> vertically by a thin gap. The slab is extremely flat, about eight times as wide as it is tall, and fills the
> width of the image. No ground below it, no shadow. Everything around it is flat pure magenta #FF00FF.

**O-A3 · A_OBJ_Station.png** · 236 × 315 px (1.2 × 1.6u) · pivot bottom centre · Shape: slightly tall, 3:4
> A small sandstone pedestal seen from the side, a gravity control shrine: a weathered stone column about as wide
> as a cat, with a round bronze dial mounted on top, and a few roots around its base. Simple, solid silhouette,
> slightly taller than wide. Warm palette like the reference. Everything around it is flat pure magenta #FF00FF.

**O-A4 · A_OBJ_Checkpoint.png** · 295 × 393 px (1.5 × 2u) · pivot bottom centre · Shape: tall, 3:4
> A short standing sandstone post seen from the side, a waypoint marker: a carved stone pillar about waist height
> with a small triangular cloth banner hanging from its top, warm and inviting. Simple silhouette, clearly taller
> than wide. Everything around it is flat pure magenta #FF00FF.

**O-A5 · A_FG_01.png** (optional) · 787 × 590 px (4 × 3u) · pivot top centre · Shape: wide, 4:3
> Hanging roots and a torn cloth banner hanging down from the top edge of the image, seen from the side, as a
> dark foreground silhouette. Very simple, little internal detail, since this sits in front of everything. The
> shapes touch the top edge and hang down into the upper half. Everything else is flat pure magenta #FF00FF.

---

## Reality B (attach the B reference, new chat)

**O-B1 · B_OBJ_Elevator.png** · 393 × 79 px (2 × 0.4u) · pivot centre · Shape: wide and flat, 5:1
> A thin floating obsidian platform slab seen exactly from the side: a dark, polished, slightly tapered slab with
> a single thin cyan edge-light line along its top. Edge-lit in the centre of its front face: a simple leaf shape
> with a V-shaped notch cut into its tip, drawn in thin cyan lines. The slab is about five times as wide as it is
> tall and fills the width of the image. No glow spilling outward. Everything around it is flat pure magenta
> #FF00FF.

**O-B2 · B_OBJ_Gate.png** · 98 × 1007 px (0.5 × 5.12u) · pivot bottom centre · Shape: very tall and narrow, 1:10
> A tall, narrow vertical obsidian gate bar seen from the side: a slim dark pillar with sharp faceted edges and a
> faint cyan edge line down one side. Edge-lit near its top: an upward-pointing chevron split vertically by a thin
> gap, in thin cyan lines. The bar is extremely slender, about ten times as tall as it is wide, and runs the full
> height of the image. Portrait format. No glow spilling outward. Everything around it is flat pure magenta
> #FF00FF.

**O-B3 · B_OBJ_Checkpoint.png** · 295 × 393 px (1.5 × 2u) · pivot bottom centre · Shape: tall, 3:4
> A short obsidian monolith seen from the side, a waypoint marker: a dark faceted stone slab about waist height
> with a single thin vertical cyan line down its centre. Cold and calm, simple silhouette, clearly taller than
> wide. Everything around it is flat pure magenta #FF00FF.

**O-B4 · B_FG_01.png** (optional) · 787 × 590 px (4 × 3u) · pivot top centre · Shape: wide, 4:3
> Floating glass shards and cold mist hanging down from the top edge of the image, seen from the side, as a dark
> foreground silhouette. Very simple, little internal detail. The shapes touch the top edge and hang into the
> upper half. Everything else is flat pure magenta #FF00FF.

---

## Approval

For each slot, either generate one version and have it processed, or generate 2–3 and let Claude Code build a
candidate sheet (`prepare_slot.py candidates --slot X --raw a.png --raw b.png`), then pick a number.

Check on the contact sheet:
- **Checkerboard:** no magenta or pink fringe, especially around the cyan lines in B.
- **Proportion:** does the object have the shape the slot calls for? A gate drawn too wide stays too wide.
- **Phone scale:** is the glyph still recognisable? Is the silhouette readable against the platform texture?

---

## Claude Code prompt (processing)

```
PAX-A02 — STAGE 2c PROCESSING. No AutoSprite; the object raws are generated in ChatGPT, as in 2b.
Do not commit.
For each raw present in Art_Source/Environment/<A|B>/ whose slot is still empty, run
prepare_slot.py --mode object with the highest _raw_vN and these prompt IDs:
  A_OBJ_Vine O-A1 · A_OBJ_Plate O-A2 · A_OBJ_Station O-A3 · A_OBJ_Checkpoint O-A4 · A_FG_01 O-A5 ·
  B_OBJ_Elevator O-B1 · B_OBJ_Gate O-B2 · B_OBJ_Checkpoint O-B3 · B_FG_01 O-B4
First --dry-run for all of them and report per slot: K, background/core/fringe %, the trimmed bounding box,
the scale factor, the resulting object size within the slot (px and % of the target), PASS/FAIL.
STOP and report if an object fills less than 80 % of the target in its longer dimension (drawn at the wrong
proportion, so a new raw is needed).
Then, for the slots I approve, run for real and check in Unity: setup twice (the second run reports
"no changes"), validator, save the scene, EditMode, and report which manifestation each Art child landed under
and with which sortingOrder.
OUTPUT: ~/Desktop/PAX-A02_stage2c.txt. Then stop.
```

Note: the objects are placed as an `Art` child of the existing manifestation (Knot, Plate_A, Station_A,
Elevator_B, Gate_B, Checkpoint_0) at sortingOrder −2, so they sit behind the cat and in front of the geometry
art. The existing runtime visuals (indicator, glow, pulse) stay untouched.
