# Cat sprites generated with AutoSprite (2026-09-29): handover

Generation only. Nothing is in Unity, nothing is committed, and no repo file outside this folder was changed.
Wiring is PAX-A08 (import) and PAX-V07 (code).

## Source and credits

- **Base paintings:** the side views cut from `DesignImages/13_cat-character-sheet.png`, uploaded free as AutoSprite characters.
  - Cat A: `_base/catA_side_cutout.png`, character `cmumxgmgx0001grljze4ko7qt`.
  - Cat B: `_base/catB_side_cutout.png`, character `cmun0s5070005gtkenjnhc62i`.
- **Settings:** every clip is `kind: custom`, turbo tier (5 credits), 256 px frames, 25 frames, removeBg `ultra`, facing right.
- **Credits:** 350 spent of a 400 cap (raised from 300 by the developer). Balance 1,498 before, 1,148 after. Details in `ledger.json`.
- **Free re-cuts** (`regenerate`) were used for:
  - 56-frame versions of the loops, for smoother cycles. AutoSprite returns 56 frames when asked for 64.
  - `removeBg=default` versions. These work better on white Cat B, and on splash, where they keep the water.

## Folder layout

`<cat>/<nn>_<clip>/` holds:

- `raw_vN.png` and `raw_vN_atlas.json`: exactly as AutoSprite returned them.
- `recut_*`: free re-cuts.
- `preview_*.gif` and `loop*_strip.png` / `.gif`: previews and extracted loops.
- `sidecar.json`: prompt, jobs, credits, verdict, **`chosen`** (the sheet to use) and **`suggest`** (frames and fps).

Other files:

- `contact_sheet.png`: the middle frame of every clip, both cats.
- `_fx/contact_fx.png`: the effect sheets.
- `_tools/`: `cats.py` (upload, clip, resume, recut, contact), `clips.py` (prompts), `fx.py`, `loops.py`, `review.py`, and the cutout scripts.

## Clips (Cat A / Cat B verdict)

| # | Clip | A | B | Notes |
|---|---|---|---|---|
| 01 | idle (loop) | good | good | A: 20-frame loop in the 56-frame re-cut (f22–41) |
| 02 | run (loop) | good | good | 10-frame full stride at ~32 fps (A f45–54, B f17–26 of the 56-frame re-cut); scale fps by \|vx\|/6 |
| 03 | walk (loop) | good | good | full stride: A 22 frames, B 26 frames |
| 04 | jump rise | good (retry) | good | A's first try failed (reared up, never left the ground) |
| 05 | jump apex | good | good (default re-cut) | |
| 06 | jump fall (loop) | good | good | tilts steeper; ping-pong the end |
| 07 | land | usable | good | A has no touchdown from the air (use 19 for that); B has one |
| 08 | vine climb up (loop) | usable | usable (retry) | vine painted in, key it out; B's first try showed its back with the marking redrawn |
| 09 | vine climb down (loop) | weak | usable | A: play 08 reversed instead |
| 10 | vine hang (loop) | good | good | vine painted in |
| 11 | vine leap | usable | usable | starts head-down on the vine; vine painted in early |
| 12a | fidget: tail flick | usable | usable | tail clips the edge |
| 12b | fidget: ear twitch | good | good | |
| 12c | fidget: sit down | good | good | |
| 13a | death: spiked | usable | good | |
| 13b | death: crushed | usable | usable | A runs off both edges |
| 13c | death: zapped | good | usable (default re-cut) | |
| 13d | death: splash | usable (retry, default re-cut) | good (default re-cut) | A's water is blue: warm hue shift for Reality A |
| 13e | death: arrow | good (retry) | good | A's first try had blood and a stuck arrow; no arrow is drawn |
| 13f | death: pit fall | usable | usable | A is drawn smaller: rescale |
| 14 | door enter | good | good | walks out past the right edge; no door drawn |
| 15 | respawn | good (retry) | good | A gold glow, B cyan glow; trim to ≤ 0.2 s |
| 16a | menu idle (loop) | usable | good | for PAX-A09, not gameplay |
| 16b | celebrate | good | good | |
| 16c | fed up | good | good | depends on D-044 (death count), undecided |
| 17 | look around (A08 curious) | good | good | |
| 18 | turn | good | good | pivots to face left; A08 wants 3 frames |
| 19 | hard land | good | good | real touchdown and squash |
| 20 | death: frightened (A08 generic) | good | good | holds the sheet's FRIGHTENED pose |
| 21 | gravity twist | usable | usable | full somersault: use only the tucked roll, since code rotates the root |
| 22 | launched (geyser) | usable | usable | stands upright, looks human-like; redo candidate |
| 23 | dizzy (inverter) | weak | weak | style lost; optional in A08 |

Exact frame ranges and fps for each clip are in its `sidecar.json` `suggest` field.

## V06 effect sheets (`_fx/`, statics, 1 credit each, 1024², on black: use additive blending or luminance-to-alpha)

| ID | What | Verdict |
|---|---|---|
| FX01 | dust puffs, 6-step lifecycle (run and landing) | good |
| FX02 | skid dust, 4 streaks (trail right, mirror in code) | good |
| FX03 | landing ground cloud, 4 steps | usable (steps barely differ; scale and fade in code) |
| FX04 | wall dust, 4 vertical plumes | good |
| FX05 | checkpoint burst: spark, flare, leaf ring, sparkles | good |

## Known issues for wiring

- **Scale and proportion.** The painted cat stands 231 × 169 px (about 1.37:1), taller than the old CatA art (236 × 112). The ears and tail stand well above the 1.0 × 0.56 collider (D-052 covers torso and legs only). The PPU rule needs a decision.
- **Placement.** Clips place the cat at different sizes and heights: the paw row sits between 197 and 240 px. An offline registration pass (one scale, one paw line) is needed before `CatSpriteImporter`, which gives every sheet Walk's PPU and pivot.
- **Replace everything at once.** The five existing sheets (`Assets/_Game/Art/Cats/CatA/CatA_{Idle,Walk,Rise,Fall,Land}.png`) show a different cat. Replace the whole set together.
- **Painted-in vines:** clips 08–11 (olive green on A). They differ between clips on B.
- **Normal maps:** none generated yet. A08 requires them; the trap method is `Tools/Art/trap_process.normal_map`.
- **Redo candidates** with a better base painting or prompt: vine clips (a head-up clinging painting), launched (try "curled in a ball, tumbling upward"), and dizzy.
- **Lessons:**
  - Every clip starts from the base standing pose.
  - "Arrow" prompts invite blood; describe the reaction only.
  - `removeBg=default` beats `ultra` on white fur.
  - Prompts over 600 characters are rejected before any credit is charged.
