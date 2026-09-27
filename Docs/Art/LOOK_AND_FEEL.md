# PARALLAX · Look and feel target (Reality A, solo v1)

**Owner:** the Architect. **Status:** the target for Phase E (2026-09-27, D-094).
**Why:** PARALLAX is a paid game (D-068). The developer's call: visually it must be spot on, with a real 2.5D feel, finished trap animations and a cat that feels alive. Today the rooms are flat colour blocks on three background strips; traps are greybox colours; the cat has five flipbook clips.

## 1 · The references (these are the target, not "inspiration")

| Reference | What to take from it |
|---|---|
| `DesignImages/14_environment-breakdown.png` | **The layer stack** (§2), and the six "how it comes to life" techniques: layered painted planes, parallax depth, sprite normal maps, atmospheric light (volumetric light, fog, aerial perspective), reflective water, selective particles |
| `Docs/Art/Reference/styleframe_A_v1.png` | The gameplay screen: warm backlit sun, floating ruined islands, vine-draped platforms with hanging vines under them, waterfalls, red banners, dark reflective water in front, blurred foreground leaves |
| `DesignImages/15_premium-mobile-ui.png` | The HUD: minimal, thin white line icons, circular translucent buttons, cat portrait + progress line, pause top-right; the world is the screen |
| `DesignImages/13_cat-character-sheet.png` | Cat A: slim dark-brown cat, amber eyes, long curled tail; poses: idle, curious, frightened, jumping, pulling, falling (mid-air), celebrating |

`D-015`'s "evokes the key art rather than reproducing its painterly depth" is superseded by D-094: the in-game frame should read like `styleframe_A_v1` on a phone, within the performance budget PAX-V03 measures.

## 2 · The layer stack (back to front)

| # | Layer | Content | Moves with camera | Notes |
|---|---|---|---|---|
| 01 | Sky | golden clouds, sun disc, a few distant floating islands | ~0.05 | sun is the key light's source; slow cloud drift |
| 02 | Far architecture | spires, aqueduct, floating ruins, haze | ~0.15 | aerial perspective: low contrast, warm haze |
| 03 | Mid arches | arches, towers, waterfalls (animated), trees | ~0.35 | waterfalls are looping flipbooks or scrolling UV |
| 04 | **Gameplay** | platforms, walls, ceilings, traps, cat, door | 1.0 | the only layer with colliders; full contrast, normal maps |
| 04b | Gameplay dressing | vine overhangs under platforms, moss caps, banners, rubble, glyphs | 1.0 | never overlaps a trap in a way that tells (P10); never looks like a surface |
| 05 | Interactive glow | door, checkpoint gate, inverter orbs, geyser vents | 1.0 | the only emissive things besides the sun |
| 06 | Foreground | roots, leaves, tree trunks, pre-blurred | ~1.3 | never covers the cat's path or a trap tell (camera tell rule) |
| 07 | Atmosphere | fog bands, light shafts, dust/pollen motes | varies | sparse; performance-budgeted |
| — | Water | dark reflective water under the play space where pits drop out | 1.0 | reflection of layers 01–04, subtle ripple |

## 3 · Rules the art must keep (gameplay first)

1. **Readability beats beauty.** The cat, every surface you can stand on, and every honest hazard must read instantly at phone scale on a 6" screen. Background layers are lower contrast and warmer than layer 04; the cat has a rim light.
2. **No tells (P10), in art too.** A disguised trap (collapsing floor, fake platform, flush falling block, disguised launcher, hidden spikes) must be **pixel-identical to its host** before it reveals. Trap art = the host's art until the reveal frame. Dressing (vines, moss) is placed by rule, never only on traps or never only on safe floors.
3. **Hitboxes don't move.** Art follows colliders (D-052); no animation or effect changes a collider, a route pin, or a tick. Presentation reads game state; it never writes it.
4. **Deterministic presentation.** Trap and cat animation frames are functions of game state and ticks where they show a reveal, so rewinds (D-091) and the route harness see the same thing. Ambient motion (clouds, water, leaves) may run on render time.
5. **Every death reads.** The death pose, a short impact effect and the frozen room during the hold (D-058) must show what killed you.
6. **Cheap enough.** 60 fps on the Pixel 8a is the bar; PAX-V03 sets the budget (draw calls, overdraw, lights, particles) and every later ticket stays inside it.
7. **Reality A only** (D-047). Reality B art stays as it is until the co-op update.

## 4 · Palette and light

- Key light: low warm sun from behind the scene (backlight), gold #FFD9A0-ish; fill: warm ambient; the cat gets a rim.
- Shadows: warm brown, never black; contrast highest on layer 04.
- Accent: the red-rust banners, the cyan-free world (cyan belongs to Reality B).
- Hazards: honest hazards keep a consistent danger language (bronze/rust spikes, dark water); a disguised trap borrows its host's palette until it fires.

## 5 · Feel

The cat squashes on landing, leaves dust on turns and landings, and its tail moves. Deaths hit hard (a brief flash, a small shake, the death pose held by the room freeze). Traps have weight: dust when a block falls, debris when a floor crumbles, a glint on a launcher's tell. Nothing slows control: death to control stays ≤ 0.75 s (D-041), respawn has no fade.

## 6 · Tickets (Phase E, after PAX-060)

PAX-V03 (look slice + budget) → PAX-A12 (environment kit) → PAX-A08 (cat animation set) → PAX-A13 (trap art kit) → PAX-V05 (trap presentation) → PAX-V06 (game feel) → PAX-A09 (UI) → PAX-A10 (icon, store). Audio and haptics (PAX-064/065) run alongside PAX-V06: sound is half of feel.
