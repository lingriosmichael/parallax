# PAX-A08 · The cat comes alive: register and import the Cat A animation set

**Status:** **Approved 2026-09-29** (revised text, reviewed by the developer); decisions re-confirmed 2026-09-30.
PAX-A13 is committed (2026-09-30), so this ticket can start. It replaces the earlier A08 text. The clips already exist
(generated 2026-09-29, see `Art_Source/AutoSprite/Cats/HANDOVER.md`), so this ticket no longer generates anything: it
registers, imports and reports. PAX-A11 stays absorbed (climb). The seamless vine (old item 15) is **not** in A08: it
gets its own ticket after PAX-V07 and PAX-V07b. It is no longer blocked, since A13 is committed.
**Order:** this ticket's Stage 0 (offline) → Stage 1 (import) → the developer's review → PAX-V07 → PAX-V07b → the
seamless vine. One ticket at a time.
**Phase 1 size:** Lite.
**Depends on:**
- PAX-A13, committed 2026-09-30 (`trap_process.normal_map`, a clean `Tools/Art/`).
- `CatSpriteImporter` and `CatVisualSetup`.
- PAX-V03's memory budget.

**Decisions:**
- D-036: code-driven flipbook, shared pivot.
- D-052: the collider stays 1.0 × 0.56, with the paw line at −0.4.
- D-041/D-058: death to control ≤ 0.75 s; the hold is 30 ticks (0.6 s).
- D-075: the tick is 50 Hz.
- D-089/D-092: climbing.

**Developer rulings (2026-09-29):**
- Keep the collider. PPU is set so the painted torso spans 1.0 u.
- Per-killer deaths are in, through a tag → clip table (PAX-V07).
- Menu idle, fed up, dizzy and launched are parked.
- ASTC compression.
- Loops are subsampled only evenly.

## 1. Why

The five Cat A sheets in `Assets/` (Idle, Walk, Rise, Fall, Land) show an older AutoSprite cat that no longer matches the
character sheet. The new clips were animated from the approved painting (`DesignImages/13_cat-character-sheet.png`).
The whole set is replaced at once, so no state shows the old cat next to the new one.

## 2. Slots (Cat A)

The **source** is the clip folder under `Art_Source/AutoSprite/Cats/A/`, using the sheet named in its sidecar's `chosen`
field. Exact frame indices are picked in Stage 0 from each sidecar's `suggest` field and recorded in the Stage 0
manifest; the counts below are the targets. **Loops** use a frame count that divides the source loop evenly (§3.2).

| Slot file (`Assets/_Game/Art/Cats/CatA/`) | Source clip | Frames | Loop | Used by (PAX-V07 state) |
|---|---|---:|---|---|
| `CatA_Idle.png` (replaces) | 01_idle, 20-frame loop f22–41 of the 56-frame re-cut | 10 | yes | Idle |
| `CatA_IdleLook.png` | 17_look_around | 8 | no | idle fidget 1 |
| `CatA_IdleEar.png` | 12b_fidget_ear | 8 | no | idle fidget 2 |
| `CatA_IdleSit.png` | 12c_fidget_sit, ending seated | 8 | no (last frame held) | idle fidget 3 (last; held until input) |
| `CatA_Walk.png` (replaces) | 03_walk, 22-frame loop f25–46 of the 56-frame re-cut | 11 | yes | Walk (slow) |
| `CatA_Run.png` | 02_run, 10-frame loop f45–54 of the 56-frame re-cut | 10 | yes | Run |
| `CatA_Turn.png` | 18_turn | 3 | no | Turn |
| `CatA_TakeOff.png` | 04_jump_rise v2 (crouch and push) | 2 | no | TakeOff |
| `CatA_Rise.png` (replaces) | 04_jump_rise v2 (airborne) | 3 | no | Rise |
| `CatA_Apex.png` | 05_jump_apex | 2 | no | Apex |
| `CatA_Fall.png` (replaces) | 06_jump_fall | 3 | no | Fall |
| `CatA_Land.png` (replaces) | 07_land (crouch and recover) | 3 | no | Land |
| `CatA_HardLand.png` | 19_hard_land (touchdown and squash) | 4 | no | HardLand |
| `CatA_Death.png` | 20_death_generic, ending on the held FRIGHTENED pose | 6 | no | Death (default) |
| `CatA_Death_Pit.png` | 13f_death_pit, rescaled | 6 | no | Death (cause Fall or OutOfBounds) |
| `CatA_Death_Spiked.png` | 13a_death_spiked | 6 | no | Death (tag Spiked) |
| `CatA_Death_Crushed.png` | 13b_death_crushed | 6 | no | Death (tag Crushed) |
| `CatA_Death_Zapped.png` | 13c_death_zapped | 6 | no | Death (tag Zapped) |
| `CatA_Death_Arrow.png` | 13e_death_arrow v2 | 6 | no | Death (tag Arrow) |
| `CatA_Respawn.png` | 15_respawn v2 | 4 | no | Respawn (≤ 0.2 s) |
| `CatA_Flip.png` | 21_gravity_twist, the tucked roll only (f7–12) | 4 | no | Flip |
| `CatA_Climb.png` | 08_vine_climb_up, 25-frame loop f24–48 of the 56-frame re-cut, vine keyed out | 5 | yes | Climb (reversed for down) |
| `CatA_Hang.png` | 10_vine_hang, vine keyed out | 1 | – | Hang |
| `CatA_Leap.png` | 11_vine_leap f10–20, vine keyed out | 3 | no | Leap |
| `CatA_Door.png` | 16b_celebrate (A08's CELEBRATING pose) | 8 | no | Door (level complete) |
| `CatA_DoorEnter.png` | 14_door_enter, frames 9–24 at an even stride | 8 | no | **imported, not wired** (V07 leaves it unwired) |

About 149 frames in all. The clips below are in `Art_Source` but have no slot here:
- **Parked:** 16a menu idle (PAX-A09), 16c fed up (PAX-V07b), 23 dizzy, 22 launched (redo first).
- **Unmapped:** 13d splash, until a water hazard exists.
- **Not imported:** 12a tail flick (its tail clips the cell edge; it stays unwired), 09 climb down (Climb plays reversed
  instead). Adding either later is a slot change in this table.
- 14 door enter **is** imported (`CatA_DoorEnter`) but not wired; its walk-out past the cell's right edge is clipped and
  reported like any slot.

## 3. Stage 0: registration (offline, no Unity)

A new script, `Tools/Art/cat_register.py`, reads a slot list (`Art_Source/AutoSprite/Cats/A/_import/slots.json`) and writes
one import-ready sheet per slot. It also writes a manifest and a review sheet to `Art_Source/AutoSprite/Cats/A/_import/`.
The script is deterministic (the same input gives the same bytes) and has offline tests.

### 3.1 Scale: one PPU for the whole set

- **The torso** is the horizontal extent from the front of the chest to the back of the rump, excluding the head, neck
  and tail. It is measured once, in pixels, on `CatA_Idle` frame 0 after registration.
- The value (`torsoPx`) and a marked-up image showing it go into the manifest and into `A02_asset_manifest.md`.
- **PPU = `torsoPx` / 1.0** (the collider length). The importer reads PPU from the manifest; it no longer derives it
  from Walk frame 0's opaque width.
- **Every clip is scaled to the same torso length.** A per-slot scale correction is allowed and recorded (the pit clip is
  drawn smaller).

### 3.2 Frames and loops

- **One-shot clips:** frames are picked from the sidecar's `suggest` range.
- **Loops:** the target count must divide the source loop length evenly, and frames are taken at a fixed stride:
  - idle 20 → 10 (stride 2);
  - run 10 → 10;
  - walk 22 → 11;
  - climb 25 → 5.
- If no divisor is within ±50% of the target, the script keeps the whole loop or re-picks the loop window from the
  56-frame re-cut. It never subsamples unevenly.
- A loop's last frame must flow into its first. The script reports the closing IoU (`loopCloseIoU`); below 0.85 it
  flags the slot for review.

### 3.3 Placement: one pivot for the whole set

- **Ground clips:** the lowest paw across the clip sits on the shared paw row, and the torso centre sits on the shared
  x position.
- **Air, climb and flip clips:** the torso centre of each frame's silhouette sits where the walk's torso centre sits.
  - These clips have no paw line, so the pivot is still the shared one. The body lands on the collider.
  - The climb clips are drawn vertical. They are registered so the body centre is on the collider centre, with no
    rotation (V07 removes the interim 90° climb pose).
- **Cleanup:** anything outside the 256 px cell is clipped and reported. Remaining background fringe is trimmed, meaning
  alpha below 10% is zeroed and grey or white halos are removed.
- **Vine clips** (Climb, Hang, Leap): the baked-in vine is keyed out by hue (olive green on dark brown), and each key is
  checked on the review sheet.

### 3.4 Normal maps

For each slot, `CatA_<Slot>_n.png` is made with `trap_process.normal_map`, using the same method as the trap kit.

### 3.5 Manifest and review sheet

- **A02 first.** Stage 0 adds the Cat A slot table (slot file, source clip and frames, loop, PPU, pivot, `torsoPx`) to
  `Docs/Art/A02_asset_manifest.md`. Stage 1 doesn't import until those slots are there.
- **Manifest** (`_import/manifest.json`). For each slot:
  - source clip, sheet and frame indices;
  - loop stride, `loopCloseIoU`;
  - scale correction, clipped pixels;
  - output paths.

  For the set:
  - `torsoPx`, PPU, pivot.
- **Review sheet** (`_import/review.png`), at phone scale, every slot's frames in a row, each with the 1.0 × 0.56
  collider outline and the paw line drawn in. Beside them:
  - the cat (Idle, Run, Rise, HardLand) under the **tightest ceiling gap** on a solution route in L001–L020;
  - the cat in the **tightest closed-crusher gap** in L001–L020;
  - the same set **with gravity flipped** (root rotated 180°, paws on the ceiling).

  Both gap values come from layout data, answered by `pax-room-auditor` in Phase 2 and recorded in the manifest. The
  developer judges ear and tail clipping on this sheet.
- **Climb preview** (`_import/review_climb_slowest.gif`, with `review_climb_typical.gif` beside it for comparison): the
  5-frame Climb loop played at the frame rate PAX-V07 will use at the **slowest climb speed**, so the developer can judge
  its choppiness.
  - That rate is `FlipbookMath.FpsForSpeed(slowestClimbSpeed, referenceSpeed, climbFps, minFps, maxFps)`, including
    the `minFps` clamp.
  - `slowestClimbSpeed` is read from the climb configuration (lowest non-zero climb speed), and the other values from
    `CatA_VisualConfig`. The values and the resulting fps are recorded in the manifest.
  - If the slowest rate looks choppy, the options are a longer climb loop (re-picked from the 56-frame re-cut, still an
    even stride) or a higher `minFps`. The developer chooses.

## 4. Stage 1: import (Unity)

### 4.1 Copying

- Copy the slot sheets and normal maps into `Assets/_Game/Art/Cats/CatA/`.
- **This ticket permits replacing** `CatA_Idle/Walk/Rise/Fall/Land.png` in place (their `.meta` files and GUIDs are kept).
- Unity creates the `.meta` files for the new files on import. None is written by hand.

### 4.2 `CatSpriteImporter`

- The sheet list is read from a slot table (paths are data, so Cat B reuses the code).
- PPU and pivot come from the Stage 0 manifest. Every sheet must match them, or the import stops with an error.
- The `_NormalMap` secondary texture is added, as in `TrapKitImport`.
- **Compression:** ASTC, block size chosen in Phase 2 against the budget. Mipmaps off, bilinear filtering.
- The menu items stay: `PARALLAX/Art/Import Cat A Sheets`, plus the selected-sheet import.

### 4.3 Setup menu

Re-run `PARALLAX/Setup/Cat Visual` (unchanged code). The existing five states pick up the new art. The new slots are
wired by PAX-V07.

**Interim until V07:**
- Walk plays at every speed.
- Climb still shows the rotated Idle and Walk frames.

## 5. Phase 1 (Lite): questions

1. May `cat_register.py` import `trap_process.normal_map` directly, or should it be moved to a shared module? PAX-A13 is
   committed, so the file is stable; the decision is to use the trap method either way.
2. Is the torso definition in §3.1 right (chest to rump, no head, no tail)?
3. Does any EditMode test pin the current PPU or pivot values, which will change? `CatColliderConfigTests` and the
   visual-seat tests are the candidates.

## 6. Phase 2: measured and reported

- `torsoPx`, PPU and pivot. Each slot's closing IoU and clipped pixels.
- **Memory** of the whole Cat A set under ASTC, **with and without normal maps**, per block size tried. The block size
  used is the one that fits PAX-V03's budget.
- **The cat's on-screen size:** pixels per unit = screen height ÷ (2 × orthographic size), using the level camera in
  L001–L020. Report the minimum and maximum over those levels at 1080 px screen height. Report the torso's on-screen
  pixel length against the 256 px cell's torso length, so the developer can judge whether a smaller cell (for example
  192 px) would do.
- The ceiling-gap and crusher-gap values used on the review sheet.
- The slowest climb speed and the Climb fps it gives (§3.5).

## 7. Allowed files

- `Tools/Art/cat_register.py` (new).
- `Tools/Art/tests/test_cat_register.py` (new, offline).
- `Art_Source/AutoSprite/Cats/A/_import/**`.
- `Docs/Art/A02_asset_manifest.md` (new section: Cat A animation slots).
- `Assets/_Game/Art/Cats/CatA/*.png`, and their Unity-generated `.meta` files.
- `Assets/_Game/Editor/Art/CatSpriteImporter.cs`.
- `Assets/_Game/Tests/EditMode/CatSheetImportTests.cs` (new): every sheet has the manifest's PPU, pivot, cell size and
  compression, and a normal map.
- `Cat_Player.prefab`, changed only by running `PARALLAX/Setup/Cat Visual`.
- `CLAUDE.md`: both D-044 references, in the A08 commit, per the developer (2026-09-30):
  - the scope line, updated to "no lives, unlimited retries, per-room death count (D-044)";
  - the Rooms section's "Whether/how it's shown, persisted, or turned into lives is still D-044 (undecided)", updated to
    say D-044 settled no lives and unlimited retries, and that the count's UI and persistence come with the ticket that
    shows it.
- This ticket file (results section).

## 8. Out of scope

- Any `CatAnimState` or presenter change (PAX-V07).
- The collider, motor, routes or levels.
- Cat B (a copy of this ticket after V07).
- The seamless vine.
- Generating new clips.

## 9. Stop conditions

- **The torso doesn't fit.** At the torso PPU, the torso plus legs don't fit the collider's height (0.56) within the
  paw-line offset.
- **Memory is over budget.** Even the coarsest acceptable ASTC block exceeds PAX-V03's budget.
- **A loop can't be subsampled evenly** without a visible hitch.
- **A vine key-out** eats the cat's fur.

## 10. Acceptance

**Editor:**
- `CatSheetImportTests` pass. The EditMode total is not below the baseline.
- `refresh_unity`, `validate_script` and `read_console` are clean.
- The developer approves `review.png`, including the ceiling, crusher and flipped panels, and the slowest-speed climb
  preview.
- Both CLAUDE.md D-044 lines are updated in the same commit.

**YOU (Editor):**
- Play Trap Lab rooms 0–3 and L001.
- Idle, walk, rise, fall and land show the new cat, with paws on the surface and no pop between the replaced states.
- Check both gravities.

**Device:** readability at phone scale is unverified until Phase H.
