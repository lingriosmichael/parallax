# PAX-A13 · AutoSprite prompts (approved 2026-09-29, rulings §11 R3)

Approved with R3's changes: T10 and T11 dropped; statics in ultra quality (1 credit); animations in turbo. The exact text below is what the tool sends; lengths are checked
against the API limits (static prompt ≤ 300 characters, custom animation prompt ≤ 600).

## How the style is locked

AutoSprite's API takes **no reference images** (checked 2026-09-29: `POST /assets` and `POST /characters` take text
only; an uploaded image is free but is used as-is). So the style is locked three ways:

1. **One fixed style block** on every static prompt, compressed from the environment prompts doc's style header
   (hand-painted, strict side view, golden backlight, gold rim, warm brown shade never black, no cyan, no text).
2. **Every animation starts from an approved still**, uploaded free as a character, so the frames inherit its painting;
   every animation prompt carries the same "static camera, keep the exact style" block.
3. **Post-processing** in the repo grades each output to the styleframe palette and flags cyan and black shadows.

## Backgrounds

- **M** (flat magenta #FF00FF): solid bodies (launchers, arrows, vents, spikes, blocks, debris). Keyed to alpha by the
  existing processor, with despill on the gold rims.
- **B** (pure black): light and soft effects (water jets, dust, spray, glyph glow, orbs). Brightness becomes alpha, or the
  sprite uses an additive material, as in the environment doc's type B.
- Animations request `removeBg`.

## Scale

Traps use the play layer's **128 px per unit**, as in the environment handoff. The exact pixel size of each piece comes
from its element's collider (art follows colliders, D-052), measured in Phase 2. AutoSprite animation frames are square
and at most 512 px (4 units at 128 PPU), so anything longer (a geyser's column, a lightning strike) is built from a
tiling piece plus animated ends.

## Style blocks

Static, background M:
```text
Hand-painted 2D game sprite, strict side view. Golden backlight, gold rim light, warm brown shade, no black. No cyan, no text. On flat magenta #FF00FF.
```
Static, background B:
```text
Hand-painted 2D game effect, strict side view. Warm gold and cream, soft edges. No cyan, no text. On pure black #000000.
```
Animation (appended to every motion prompt):
```text
Static camera: no zoom, pan or tilt; strict side view. Keep the exact painting style, colours, light and outline of the image. The base shape never moves or changes size; only the described motion happens.
```

## Style gate (CollapsingFloor, Arrow, Geyser): 16 planned / 32 ceiling

- **CollapsingFloor:** no AutoSprite frames. The crumble cuts the host's own rendered skin into shards (seeded pattern)
  that drop and spin in code, plus debris and dust from T05/T06: pixel-identical by construction (P10), right at any
  width, deterministic from ticks.
- **Arrow:** launcher and arrow bodies; the tell glint, flight streak and the stick-or-break impact are code, with
  splinters from T05.
- **Geyser:** the vent, a tiling water column, and two animations of the vent (tell and burst); the spray at the top of
  the column is code particles from T06.

#### T01 arrow_launcher (M)
static asset (ultra, R3), background M · credits planned / ceiling: 1 / 2 · 285 of 300 characters

```text
Carved sandstone arrow slot: square block face, dark mouth, bronze rim, bronze arrowhead just inside. Ancient temple trap, weathered. Hand-painted 2D game sprite, strict side view. Golden backlight, gold rim light, warm brown shade, no black. No cyan, no text. On flat magenta #FF00FF.
```
#### T02 arrow (M)
static asset (ultra, R3), background M · credits planned / ceiling: 1 / 2 · 286 of 300 characters

```text
One thin arrow pointing right: bronze leaf-shaped head, dark wood shaft, rust-red fletching. Very long and thin, 12x longer than tall. Hand-painted 2D game sprite, strict side view. Golden backlight, gold rim light, warm brown shade, no black. No cyan, no text. On flat magenta #FF00FF.
```
#### T03 geyser_vent (M)
static asset (ultra, R3), background M · credits planned / ceiling: 1 / 2 · 286 of 300 characters

```text
Stone floor vent: low carved sandstone ring, flat top, dark round mouth with faint warm glow, wet moss at the lip. 4x wider than tall. Hand-painted 2D game sprite, strict side view. Golden backlight, gold rim light, warm brown shade, no black. No cyan, no text. On flat magenta #FF00FF.
```
#### T04 geyser_column (B)
static asset (ultra, R3), background B · credits planned / ceiling: 1 / 2 · 269 of 300 characters

```text
Vertical jet of rushing water, straight, centred, fills the full height, seamless top to bottom: white-gold foam streaks, backlit spray, soft sides. Hand-painted 2D game effect, strict side view. Warm gold and cream, soft edges. No cyan, no text. On pure black #000000.
```
#### T05 debris_sheet (M)
static asset (ultra, R3), background M · credits planned / ceiling: 1 / 2 · 286 of 300 characters

```text
Sheet of 8 separate small pieces, spaced apart: 4 broken sandstone chunks, 2 grit clusters, 2 wood splinters. Weathered, sunlit edges. Hand-painted 2D game sprite, strict side view. Golden backlight, gold rim light, warm brown shade, no black. No cyan, no text. On flat magenta #FF00FF.
```
#### T06 softfx_sheet (B)
static asset (ultra, R3), background B · credits planned / ceiling: 1 / 2 · 258 of 300 characters

```text
Sheet of 6 separate pieces, spaced apart: 3 soft round puffs of warm sandy dust, 3 small splashes of water droplets. Soft, no hard edges. Hand-painted 2D game effect, strict side view. Warm gold and cream, soft edges. No cyan, no text. On pure black #000000.
```
#### T07 geyser_tell (anim of T03)
animation (custom, turbo, 25 frames, removeBg) · credits planned / ceiling: 5 / 10 · 367 of 600 characters

```text
The vent starts to stir: water wells up and bubbles in the mouth, small droplets pop, thin wisps of warm steam rise a little and fade. Gentle, building slightly. Static camera: no zoom, pan or tilt; strict side view. Keep the exact painting style, colours, light and outline of the image. The base shape never moves or changes size; only the described motion happens.
```
#### T08 geyser_burst (anim of T03)
animation (custom, turbo, 25 frames, removeBg) · credits planned / ceiling: 5 / 10 · 382 of 600 characters

```text
A powerful jet of water bursts straight up out of the vent's mouth and keeps gushing: foam and spray fly outward at the base and fall back. The jet leaves the top of the frame. Static camera: no zoom, pan or tilt; strict side view. Keep the exact painting style, colours, light and outline of the image. The base shape never moves or changes size; only the described motion happens.
```

## Full set (after the style gate): 24 planned / 48 ceiling (T10 and T11 dropped, R3)

T22 animates the developer's ENV-18 climbable vine segment (uploaded free) once it exists.

#### T09 spike_strip (M)
static asset (ultra, R3), background M · credits planned / ceiling: 1 / 2 · 295 of 300 characters

```text
Row of sharp bronze spikes, worn and rust-stained, on a dark stone socket strip, seamless left to right. Clearly dangerous. 6x wider than tall. Hand-painted 2D game sprite, strict side view. Golden backlight, gold rim light, warm brown shade, no black. No cyan, no text. On flat magenta #FF00FF.
```
#### T12 block_crack (M)
static asset (ultra, R3), background M · credits planned / ceiling: 1 / 2 · 257 of 300 characters

```text
Overlay of thin jagged dark-brown hairline cracks spreading from the centre, nothing else, crisp. Square. Hand-painted 2D game sprite, strict side view. Golden backlight, gold rim light, warm brown shade, no black. No cyan, no text. On flat magenta #FF00FF.
```
#### T13 spear (M)
static asset (ultra, R3), background M · credits planned / ceiling: 1 / 2 · 292 of 300 characters

```text
One heavy spear pointing right: broad bronze head, thick dark wooden shaft, leather wrap near the head. Long and thin, 10x longer than tall. Hand-painted 2D game sprite, strict side view. Golden backlight, gold rim light, warm brown shade, no black. No cyan, no text. On flat magenta #FF00FF.
```
#### T14 glyph_ring (B)
static asset (ultra, R3), background B · credits planned / ceiling: 1 / 2 · 243 of 300 characters

```text
Ancient circular glyph ring of warm gold light: thin carved runic arc segments, soft glow, hollow centre. Square, centred. Hand-painted 2D game effect, strict side view. Warm gold and cream, soft edges. No cyan, no text. On pure black #000000.
```
#### T15 inverter_orb (B)
static asset (ultra, R3), background B · credits planned / ceiling: 1 / 2 · 220 of 300 characters

```text
Glowing orb of warm gold light with a slow swirl inside and a soft halo, floating. Square, centred. Hand-painted 2D game effect, strict side view. Warm gold and cream, soft edges. No cyan, no text. On pure black #000000.
```
#### T16 cue_icon (B)
static asset (ultra, R3), background B · credits planned / ceiling: 1 / 2 · 241 of 300 characters

```text
Simple glyph of two curved arrows chasing each other in a circle, warm gold light, bold readable shape. Square, centred. Hand-painted 2D game effect, strict side view. Warm gold and cream, soft edges. No cyan, no text. On pure black #000000.
```
#### T17 storm_cloud (M)
static asset (ultra, R3), background M · credits planned / ceiling: 1 / 2 · 269 of 300 characters

```text
One heavy storm cloud, puffy golden-lit top, dark warm-grey belly, flat-ish bottom, soft edges. 2.5x wider than tall. Hand-painted 2D game sprite, strict side view. Golden backlight, gold rim light, warm brown shade, no black. No cyan, no text. On flat magenta #FF00FF.
```
#### T18 scorch (M)
static asset (ultra, R3), background M · credits planned / ceiling: 1 / 2 · 278 of 300 characters

```text
Flat scorch mark decal: dark warm-brown burnt streaks radiating from a centre, a few embers. Wide and low, 3x wider than tall. Hand-painted 2D game sprite, strict side view. Golden backlight, gold rim light, warm brown shade, no black. No cyan, no text. On flat magenta #FF00FF.
```
#### T19 leaves_sparks (M)
static asset (ultra, R3), background M · credits planned / ceiling: 1 / 2 · 255 of 300 characters

```text
Sheet of 8 separate small pieces, spaced apart: 4 green-gold leaves, 4 small bright gold sparks. Crisp. Hand-painted 2D game sprite, strict side view. Golden backlight, gold rim light, warm brown shade, no black. No cyan, no text. On flat magenta #FF00FF.
```
#### T20 orb_flare (anim of T15)
animation (custom, turbo, 25 frames, removeBg) · credits planned / ceiling: 5 / 10 · 335 of 600 characters

```text
The orb flares: its glow swells brightly with a quick pulse of light rings expanding outward, then settles back to its calm glow. Static camera: no zoom, pan or tilt; strict side view. Keep the exact painting style, colours, light and outline of the image. The base shape never moves or changes size; only the described motion happens.
```
#### T21 cloud_charge (anim of T17)
animation (custom, turbo, 25 frames, removeBg) · credits planned / ceiling: 5 / 10 · 336 of 600 characters

```text
The cloud darkens and churns: its belly roils, flickers of warm white light pulse inside it, and a gathering glow builds under it. Static camera: no zoom, pan or tilt; strict side view. Keep the exact painting style, colours, light and outline of the image. The base shape never moves or changes size; only the described motion happens.
```
#### T22 vine_snap (anim of ENV-18 segment)
animation (custom, turbo, 25 frames, removeBg) · credits planned / ceiling: 5 / 10 · 332 of 600 characters

```text
The vine frays in the middle: fibres split and unravel, then it snaps in two; both ends whip apart and a few leaves fall away. Static camera: no zoom, pan or tilt; strict side view. Keep the exact painting style, colours, light and outline of the image. The base shape never moves or changes size; only the described motion happens.
```

## Made in code (0 credits)

Crumble shards and the fake platform's dissolve; hidden and periodic spikes rising (slide out of a mask over 2–3 ticks while
already lethal, with grit; nothing drawn while down, R4); moving, sliding, shrinking and pushing floors (the host skin, dust at the leading edge, the shrinker's edge
eroding with debris); the falling block's release (T12 crack on the release frame, grit, a dust burst on landing); arrow
and spear flight, streak, stick and wobble; the flip's pulse and motes; the orb's 30-tick blink; the lightning bolt
(a seeded jagged strip, exactly as long as the strike column) with a flash and the T18 scorch; the retreating door's
dust; every death effect (particles from T05, T06 and T19).

## Totals

| | Planned | Ceiling (one retry per line) |
|---|---|---|
| Style gate | 16 | 32 |
| Full set | 24 | 48 |
| **A13** | **40** | **80** |

No line exceeds the 20-credit cap per asset.
