# PARALLAX · Trap bodies: ChatGPT prompts (PAX-A13, R3 fallback)

Sep 29, 2026 · for the developer

## Why this exists

The style gate's AutoSprite statics (T01 launcher, T02 arrow, T03 vent, T05 debris) missed the styleframe. They came out
saturated orange-gold with a clean "casual icon" finish; the styleframe's stone is dark, weathered, mossy grey-beige.
Measured on the platform band of `styleframe_A_v1.png`: hue 35°, saturation 0.67, brightness 0.25; the AutoSprite bodies
were hue 35–37°, saturation 0.88–0.90, brightness about 0.6. T03 also broke the strict side view (seen from above). A
palette grade didn't close the gap (`style_gate/grade_test_v1.png`): the problem is the rendering, not only the colours.
AutoSprite's API can't take the styleframe as a reference; ChatGPT can.

Per rulings §11 R3: you paint the bodies in ChatGPT with the environment style header; Claude Code uploads each one to
AutoSprite as a character (free) and animates it there. T04 (water column) and T06 (dust and splashes) passed and stay.

## How to work

Exactly as the environment prompts doc: attach `styleframe_A_v1.png` and `14_environment-breakdown.png`, paste the
**environment style header**, then the item's prompt. If you already have your accepted `ENV-10a` stone fill, attach it
too: every trap body is made of that same stone. Save to `~/Parallax_Game/Art_Source/Traps_ChatGPT/` with the exact name.
Same checks before saving (strict side view, real transparency, no text, warm shadows, no cyan).

Do these in one chat (**Chat T**), after Batch 1's play layer, so the stone matches.

## TRAP-01 · Arrow launcher

`TRAP-01_arrow_launcher_v1.png` · Square 1:1 · Background T · in game: a disguised launcher looks exactly like its wall
until it fires (the game copies the wall's own tiles for that); this is what it becomes on the fire tick

```text
Asset TRAP-01: arrow trap launcher. Square 1:1. Background type T: transparent background, PNG with alpha.
One square block of the same weathered grey-beige sandstone masonry as the stone fill, strictly side view, filling most of the image, centred.
A narrow dark horizontal slot is cut into its right edge at mid height, with a worn bronze rim; a bronze arrowhead glints just inside the slot, pointing right.
Flat, straight top and bottom edges and a straight left edge. Small moss in the joints. Gold rim light on the top edge, warm brown shade.
No perspective: we see only the block's front face, and the slot as a notch in its right edge.
```

## TRAP-02 · Arrow

`TRAP-02_arrow_v1.png` · Landscape 3:2 · Background T

```text
Asset TRAP-02: arrow. Landscape 3:2. Background type T: transparent background, PNG with alpha.
One arrow, strictly side view, pointing right, centred, spanning about two thirds of the image width.
Bold, readable proportions for a small phone screen: the whole arrow is about 6 times longer than it is tall at the fletching.
A broad leaf-shaped bronze head, a dark wooden shaft, rust-red feather fletching at the back.
Gold rim light along its top edge. Transparency all around.
```

## TRAP-03 · Geyser vent

`TRAP-03_geyser_vent_v1.png` · Landscape 3:2 · Background T · in game: set flush into a floor (or a ceiling, flipped)

```text
Asset TRAP-03: geyser vent. Landscape 3:2. Background type T: transparent background, PNG with alpha.
A stone floor vent seen strictly from the side, centred, about 4 times wider than tall: a low carved sandstone lip set flush into the ground, made of the same weathered stone as the stone fill.
Its top is one straight, level line; the vent's mouth is a narrow dark opening in the middle of that top line, with a faint warm glow deep inside. We never see the top surface or a hole from above.
Wet moss and a few water stains around the mouth. Gold rim light on the top edge, warm brown shade below.
```

## TRAP-05 · Debris (not needed)

Dropped from AutoSprite and not needed from ChatGPT: crumble shards and stone chips are cut in code from the host's own
tiles (your ENV-10a/10b), so they always match the floor that broke. Wood splinters come from TRAP-02.

## After you save them

Tell Claude Code which files are in. It then:
1. post-processes them (alpha check, trim, pivot, 128 px per unit, palette check) with no credits;
2. uploads TRAP-03 to AutoSprite as a character (free) and animates it: T07 geyser tell and T08 burst, 5 credits each,
   within the approved style-gate budget;
3. finishes the style gate (Arrow, Geyser, CollapsingFloor, contact sheet, phone-scale screenshot) and stops for your look.

The full set's solid bodies (spike strip, spear, storm cloud) will likely need the same route; that's a question for
the style-gate rulings (§12).

---

## Batch 2 · TRAP-04 onward (rulings §12, R7 and R10)

Every remaining trap body and painted frame that code can't make. Same routine as TRAP-01 to 03: one new ChatGPT chat
(**Chat T2**), attach `styleframe_A_v1.png`, `14_environment-breakdown.png` and your accepted `ENV-10a` stone fill, paste
the **environment style header**, then each prompt. For the next item in the same chat add: "Match the style of the
image you just made exactly." Save each keeper to the exact path given, with `_v1` (a regenerated keeper is `_v2`).
Claude Code cuts, sizes, flips and lights them; until an image arrives, the game uses a code-drawn stand-in of the same
size, facing and palette, marked PLACEHOLDER on the contact sheet.

Sizes are the in-game size in world units (the cat is about 1 unit long); the art is scaled to 128 px per unit.

### TRAP-04 · Spike strip

`~/Parallax_Game/Art_Source/Traps_ChatGPT/TRAP-04_spike_strip_v1.png` · Landscape 3:2 · Background T · for every honest
spike hazard (floors, pit bottoms, and ceilings, flipped in code), hidden spikes rising, periodic spikes and sliding
spikes · in game about 0.35 units tall, tiled sideways at 1 unit per repeat · spikes point **up**

```text
Asset TRAP-04: spike strip. Landscape 3:2. Background type T: transparent background, PNG with alpha.
A horizontal strip of sharp ancient bronze spikes, pointing straight up, seamless left to right: the left and right edges continue into each other.
The spikes are worn and rust-stained bronze, set in a low dark stone socket strip along the bottom. Clearly dangerous: sharp tips, no rounded shapes.
The strip is about 3 times wider than tall and fills the full width. Gold rim light on the tips, warm brown shade at the base. Transparency above.
```

### TRAP-05 · Spear

`~/Parallax_Game/Art_Source/Traps_ChatGPT/TRAP-05_spear_v1.png` · Landscape 3:2 · Background T · the spear that shoots
out of a wall and sticks as a pole the cat stands on · in game 0.4 units thick, 1 to 3.6 units long (the shaft repeats)
· faces **right**

```text
Asset TRAP-05: spear, three pieces. Landscape 3:2. Background type T: transparent background, PNG with alpha.
Three separate pieces in a row, spaced apart, none touching, all the same thickness, strictly side view, pointing right:
1. The spearhead: a broad, heavy bronze blade with a short socket, pointing right.
2. A straight middle section of thick dark wooden shaft, seamless left to right, with a leather wrap band.
3. The butt end of the shaft, with a small bronze cap.
Sturdy enough to stand on. Gold rim light along the top edge, warm brown shade underneath. Transparency around each.
```

### TRAP-06 · Inverter orb

`~/Parallax_Game/Art_Source/Traps_ChatGPT/TRAP-06_inverter_orb_v1.png` · Square 1:1 · Background B · the honest
inverter (touching it swaps left and right) · in game 0.6 units across, floating · no facing

```text
Asset TRAP-06: inverter orb. Square 1:1. Background type B: pure black background, #000000.
One floating orb of warm golden light, centred, about half the image width: a bright gold core with a slow swirl of two interlocking curls inside it, and a soft warm halo fading to pure black.
No cyan, no blue. No frame, no pedestal.
```

### TRAP-07 · Inverter cue

`~/Parallax_Game/Art_Source/Traps_ChatGPT/TRAP-07_inverter_cue_v1.png` · Square 1:1 · Background B · shown on the cat
while its left and right are swapped · in game a ring 1.5 × 1.1 units behind the cat and a mark 0.7 × 0.2 units over it

```text
Asset TRAP-07: inverter cue, two pieces. Square 1:1. Background type B: pure black background, #000000.
Two separate pieces, one above the other, spaced apart:
1. Top: a thin, soft, warm-gold elliptical ring of light, twice as wide as tall, open in the middle, like a halo seen from the side.
2. Bottom: a small bold glyph of two curved arrows chasing each other left and right, warm gold, 3 times wider than tall.
Everything fades to pure black. No cyan, no text.
```

### TRAP-08 · Glyph ring (visible gravity flip)

`~/Parallax_Game/Art_Source/Traps_ChatGPT/TRAP-08_glyph_ring_v1.png` · Square 1:1 · Background B · marks a visible
gravity flip zone · in game about 1 unit across, centred in the zone · symmetric

```text
Asset TRAP-08: gravity glyph ring. Square 1:1. Background type B: pure black background, #000000.
One ancient circular glyph ring of warm gold light, centred, about two thirds of the image width: thin carved rune segments around a circle, with a small up-and-down double arrow motif at the top and bottom. Soft glow, hollow centre, fading to pure black.
No cyan, no text or letters.
```

### TRAP-09 · Storm cloud (asleep)

`~/Parallax_Game/Art_Source/Traps_ChatGPT/TRAP-09_storm_cloud_v1.png` · Landscape 3:2 · Background T · the storm cloud
while it's asleep: it must pass for a sky cloud, only a little denser, with a darker belly (§11 R2); code darkens and
flickers it when it wakes · in game 2 × 0.8 units · no facing

```text
Asset TRAP-09: heavy cloud. Landscape 3:2. Background type T: transparent background, PNG with alpha.
One single cloud, centred, about 2.5 times wider than tall: puffy golden-lit top like the sky clouds in the reference, and a noticeably darker, heavier warm-grey belly underneath with a flat-ish bottom.
Soft edges, but a clear silhouette. No lightning, no rain. Transparency around it.
```

### TRAP-10 · Scorch mark

`~/Parallax_Game/Art_Source/Traps_ChatGPT/TRAP-10_scorch_v1.png` · Landscape 3:2 · Background T · where lightning struck,
fading out after the strike · in game about 1.2 × 0.35 units, lying on a floor top · no facing

```text
Asset TRAP-10: scorch mark. Landscape 3:2. Background type T: transparent background, PNG with alpha.
A flat scorch mark as seen from the side, lying on an invisible straight level line: dark warm-brown burnt streaks spreading sideways from a centre, a few tiny glowing embers. 4 times wider than tall. Never pure black. Transparency around it.
```

### TRAP-11 · Block crack

`~/Parallax_Game/Art_Source/Traps_ChatGPT/TRAP-11_block_crack_v1.png` · Square 1:1 · Background T · drawn over a falling
block on the tick it lets go · in game laid over the block's face, 0.5 to 1.5 units · no facing

```text
Asset TRAP-11: crack overlay. Square 1:1. Background type T: transparent background, PNG with alpha.
Only thin jagged hairline cracks, dark warm brown, spreading from the centre towards the edges like breaking stone, with a few tiny chips of grit. Nothing else: no stone, no background. Crisp lines. Transparency everywhere else.
```

### TRAP-12 · Geyser steam

`~/Parallax_Game/Art_Source/Traps_ChatGPT/TRAP-12_geyser_steam_v1.png` · Landscape 3:2 · Background B · steam rising from
a geyser vent in its tell (replaces the code glow's blob; §12 R9) · in game about 0.5 × 1 unit each · rising **up**

```text
Asset TRAP-12: steam wisps. Landscape 3:2. Background type B: pure black background, #000000.
Three separate wisps of warm cream-white steam side by side, spaced apart, each rising straight up from a narrow base and curling softly, twice as tall as wide. Soft edges, fading to pure black.
```

### TRAP-13 · Leaves

`~/Parallax_Game/Art_Source/Traps_ChatGPT/TRAP-13_leaves_v1.png` · Square 1:1 · Background T · falling leaves when a
snap vine breaks, and leaf debris · in game about 0.12 units each · any facing

```text
Asset TRAP-13: leaves. Square 1:1. Background type T: transparent background, PNG with alpha.
Six separate small leaves in a 3 x 2 grid, spaced apart, none touching: broad green-gold vine leaves at different angles, like those on the climbable vine. Gold rim light on the edges. Transparency around each.
```
