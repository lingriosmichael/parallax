# PAX-A08 · The cat comes alive: register and import the Cat A animation set

**Status:** **Approved 2026-09-29** (revised text, reviewed by the developer); decisions re-confirmed 2026-09-30.
**Implemented 2026-09-30:** Stage 0 in ebe53f1, Stage 1 in the commit after it. Open: the developer's Editor play
check (§10, YOU) and the device check of the rim (§11).
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
- Keep the collider. ~~PPU is set so the painted torso spans 1.0 u.~~ **Superseded 2026-09-30 (option B):** PPU is set so
  the standing body height (paws to back line, Idle frame 0) is 0.56 u, PPU ≈ 196. The torso rule made the body 0.86 u
  tall, well above the 0.56 collider, and the cat cut into the tightest solution-route ceilings (0.70 u, L017/L012).
- Per-killer deaths are in, through a tag → clip table (PAX-V07).
- Menu idle, fed up, dizzy and launched are parked.
- ASTC compression. **6×6** (developer, 2026-09-30).
- **Stage 0 rulings (2026-09-30):**
  - The shared pivot sits 0.06 u behind the torso centre, so the collider stays inside the painted nose and tail.
  - Per-sheet cell sizes with the same world pivot, recorded per sheet in the manifest. The base cell is **192 px** (the
    source is scaled by 0.75, and PPU with it), since the cat is drawn at 90–119 px on screen.
  - A loop is flagged only when its closing step is below its lowest ordinary step.
  - The Climb preview comes in two versions, the 5-frame loop and the full 25-frame loop, each at the slowest and full
    climb speed; the developer picks.
  - The Stage 0 tool is split into modules under ~400 lines each: `cat_register.py` (the pipeline), `cat_frames.py`
    (the frame work), `cat_review.py` (the review sheet and previews).
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
| `CatA_Idle.png` (replaces) | 01_idle, 30-frame window f19–48 of the 56-frame re-cut at stride 3 (re-picked 2026-09-30: the only window that passes the loop check) | 10 | yes | Idle |
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
| `CatA_Climb.png` | 08_vine_climb_up, 25-frame loop f24–48 of the 56-frame re-cut, vine keyed out | 25 (developer, 2026-09-30) | yes | Climb (reversed for down) |
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

### 3.1 Scale: one PPU for the whole set (option B, ruled 2026-09-30)

- **The standing body height** is the vertical extent from the paw row to the back line on `CatA_Idle` frame 0 (source
  f19 of the idle re-cut since the loop was re-picked on 2026-09-30), excluding the head, neck and tail. The back line is the median top of the silhouette over the
  torso's middle columns (from the rump plus 15% to the chest minus 35% of the torso length, so neither the tail base nor
  the rising neck counts). The rump and chest columns are measured once by hand and recorded.
- The value (`bodyHeightPx`) and a marked-up image showing it go into the manifest and into `A02_asset_manifest.md`.
- **PPU = `bodyHeightPx` / 0.56** (the collider height). The importer reads PPU from the manifest; it no longer derives
  it from Walk frame 0's opaque width.
- **Every clip is drawn at the same scale.** A per-slot scale correction is allowed and recorded (the pit clip is drawn
  smaller).
- **Accepted (developer, 2026-09-30):** at this scale the ear tips touch the 0.70 u ceilings in L017 and L012.

### 3.2 Frames and loops

- **One-shot clips:** frames are picked from the sidecar's `suggest` range.
- **Loops:** the target count must divide the source loop length evenly, and frames are taken at a fixed stride:
  - idle 30 → 10 (stride 3; the window f19–48, re-picked 2026-09-30);
  - run 10 → 10;
  - walk 22 → 11;
  - climb 25 → 25 (every frame; developer, 2026-09-30).
- If no divisor is within ±50% of the target, the script keeps the whole loop or re-picks the loop window from the
  56-frame re-cut. It never subsamples unevenly.
- A loop's last frame must flow into its first. The script reports the closing IoU (`loopCloseIoU`) and the range of the
  loop's ordinary steps; it flags the slot only when the closing step is below the lowest ordinary step (ruled
  2026-09-30; the earlier fixed 0.85 flagged fast loops whose every step is below it).

### 3.3 Placement: one pivot for the whole set

- **Ground clips:** the lowest paw across the clip sits on the shared paw row, and the torso centre sits on the shared
  x position.
- **Air, climb and flip clips:** the torso centre of each frame's silhouette sits where the walk's torso centre sits.
  - These clips have no paw line, so the pivot is still the shared one. The body lands on the collider.
  - The climb clips are drawn vertical. They are registered so the body centre is on the collider centre, with no
    rotation (V07 removes the interim 90° climb pose).
- **Cells (ruled 2026-09-30):** frames are placed at source scale in a large working canvas, scaled to the **192 px
  base** (0.75), and cropped to each sheet's cell: the base cell around the shared pivot, grown on any side the clip
  reaches past it, in whole ASTC 6×6 blocks. Each sheet records its cell and where the pivot falls in it; the world
  pivot is the same for every sheet. Nothing is clipped; a clip that the source itself cut at its cell edge is reported.
- **The pivot (ruled 2026-09-30):** the paw row, 0.06 u behind Idle's median torso centre. Ground clips put their torso
  centre 0.06 u ahead of the pivot, so the collider sits inside the painted nose and tail.
- **Cleanup:** Remaining background fringe is trimmed, meaning
  alpha below 10% is zeroed and grey or white halos are removed.
- **Colour bleed (ruled 2026-09-30):** on the colour sheets, every pixel that is transparent in the 8-bit PNG takes the
  alpha-weighted colour of its nearest visible neighbours, ring by ring out to 12 px (two ASTC 6×6 blocks); alpha is
  never changed. Bilinear filtering and ASTC blocks at the silhouette then never mix in black. The normal maps are
  unaffected (`normal_map` weights luminance by alpha). The manifest records `colourBleedPx`.
- **Vine clips** (Climb, Hang, Leap): the baked-in vine is keyed out by hue (olive green on dark brown), and each key is
  checked on the review sheet.

### 3.4 Normal maps

For each slot, `CatA_<Slot>_n.png` is made with `trap_process.normal_map`, using the same method as the trap kit.

### 3.5 Manifest and review sheet

- **A02 first.** Stage 0 adds the Cat A slot table (slot file, source clip and frames, loop, PPU, pivot, `bodyHeightPx`) to
  `Docs/Art/A02_asset_manifest.md`. Stage 1 doesn't import until those slots are there.
- **Manifest** (`_import/manifest.json`). For each slot:
  - source clip, sheet and frame indices;
  - loop stride, `loopCloseIoU`;
  - scale correction, clipped pixels;
  - output paths.

  For the set:
  - `bodyHeightPx`, PPU, pivot.
- **Review sheet** (`_import/review.png`), at phone scale, every slot's frames in a row, each with the 1.0 × 0.56
  collider outline and the paw line drawn in. Beside them:
  - the cat (Idle, Run, Rise, HardLand) under the **tightest ceiling gap** on a solution route in L001–L020;
  - the cat in the **tightest closed-crusher gap** in L001–L020;
  - the same set **with gravity flipped** (root rotated 180°, paws on the ceiling);
  - **the collider outline over Idle, Walk and Run, facing both ways.** The collider's front and back edges must not
    stick out past the painted nose or the tail tip (checked on every frame of the three clips, and reported).

  Both gap values come from layout data, answered by `pax-room-auditor` in Phase 2 and recorded in the manifest. The
  developer judges ear and tail clipping on this sheet.
- **Climb previews** (`_import/review_climb5_slowest.gif`, `review_climb5_full.gif`, `review_climb25_slowest.gif`,
  `review_climb25_full.gif`): the 5-frame and the full 25-frame Climb loop, each at the frame rate PAX-V07 will use at
  the **slowest** and at full climb speed, so the developer can pick.
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
- PPU comes from the Stage 0 manifest, and each sheet's cell size and pivot from its manifest entry (per-sheet cells,
  one world pivot). Every sheet must match its entry, or the import stops with an error.
- The `_NormalMap` secondary texture is added, as in `TrapKitImport`.
- **Compression:** ASTC 6×6 (ruled 2026-09-30). Mipmaps off, bilinear filtering.
- The menu items stay: `PARALLAX/Art/Import Cat A Sheets`, plus the selected-sheet import.

### 4.3 No Cat Visual re-run in A08 (ruled 2026-09-30)

`PARALLAX/Setup/Cat Visual` is **not** re-run: its sheet check expects one 256 px cell and Walk's pivot, and V07 rewrites
it for the manifest's per-sheet cells. The five existing sheets keep their sprite names, so `Cat_Player`'s existing clip
references pick up the new art. After the import, every sprite reference in `Cat_Player.prefab` is checked to resolve
to a non-blank frame of the new sheets; a missing or blank frame is a stop.

**Interim until V07:**
- Idle, Walk, Rise, Fall and Land play their existing frame counts with the new art; the extra frames wait for V07.
- Climb still shows the rotated Idle and Walk frames.

### 4.4 The environment's PPU is fixed (ruled 2026-09-30)

`EnvironmentSpriteImporter` set every environment sprite's PPU from `CatA_Walk`'s (half of it for backgrounds and
materials), so the new cat PPU would have rescaled every later environment import by about 1.37. It now uses fixed
values, the ones A02 records: **196.667** for gameplay art and **98.333** for backgrounds and materials. A test pins them.

## 5. Phase 1 (Lite): questions

1. May `cat_register.py` import `trap_process.normal_map` directly, or should it be moved to a shared module? PAX-A13 is
   committed, so the file is stable; the decision is to use the trap method either way.
2. ~~Is the torso definition in §3.1 right?~~ Replaced by option B (2026-09-30): the standing body height, §3.1.
3. Does any EditMode test pin the current PPU or pivot values, which will change? `CatColliderConfigTests` and the
   visual-seat tests are the candidates.

## 6. Phase 2: measured and reported

- `bodyHeightPx`, PPU and pivot (the torso length at that PPU is reported too). Each slot's closing IoU and clipped
  pixels. The collider's margin to the nose and the tail tip on Idle, Walk and Run, both facings.
- **Memory** of the whole Cat A set under ASTC, **with and without normal maps**, per block size tried. The block size
  used is the one that fits PAX-V03's budget.
- **The cat's on-screen size:** pixels per unit = screen height ÷ (2 × orthographic size), using the level camera in
  L001–L020. Report the minimum and maximum over those levels at 1080 px screen height. Report the torso's on-screen
  pixel length against the 256 px cell's torso length, so the developer can judge whether a smaller cell (for example
  192 px) would do.
- The ceiling-gap and crusher-gap values used on the review sheet.
- The slowest climb speed and the Climb fps it gives (§3.5).

## 7. Allowed files

- `Tools/Art/cat_register.py`, `cat_frames.py`, `cat_review.py` (new; split per the developer, 2026-09-30).
- `Tools/Art/tests/test_cat_register.py` (new, offline).
- `Art_Source/AutoSprite/Cats/A/_import/**`.
- `Docs/Art/A02_asset_manifest.md` (new section: Cat A animation slots; line 4, the fixed environment PPU, §4.4).
- `Docs/V_TASKS/PAX-V07.md`: the takeoff-paws check (ruled 2026-09-30).
- `Assets/_Game/Art/Cats/CatA/*.png`, and their Unity-generated `.meta` files.
- `Assets/_Game/Editor/Art/CatSpriteImporter.cs`.
- `Assets/_Game/Editor/Art/EnvironmentSpriteImporter.cs` (the fixed environment PPU, §4.4; ruled 2026-09-30), and a test
  pinning it.
- `Assets/_Game/Tests/EditMode/CatSheetImportTests.cs` (new): every sheet has the manifest's PPU, pivot, cell size and
  compression, and a normal map.
- ~~`Cat_Player.prefab`, changed only by running `PARALLAX/Setup/Cat Visual`.~~ Not wired in A08 (§4.3).
  `Cat_Player.prefab`: **Unity-derived `m_Size` only** (ruled 2026-09-30). Unity rewrites both SpriteRenderers'
  `m_Size` (1.3017 → 1.3398 u, 192 px ÷ 143.30) when the referenced sprites change size; the draw mode is Simple, so
  it isn't used.
- `CLAUDE.md`: both D-044 references, in the A08 commit, per the developer (2026-09-30):
  - the scope line, updated to "no lives, unlimited retries, per-room death count (D-044)";
  - the Rooms section's "Whether/how it's shown, persisted, or turned into lives is still D-044 (undecided)", updated to
    say D-044 settled no lives and unlimited retries, and that D-061 shows the per-room count once, on the
    level-complete screen, without saving it; "with no UI" is dropped from the line above (ruled 2026-09-30, after the
    review found the first wording contradicted D-061).
- This ticket file (results section).

## 8. Out of scope

- Any `CatAnimState` or presenter change (PAX-V07).
- The collider, motor, routes or levels.
- Cat B (a copy of this ticket after V07).
- The seamless vine.
- Generating new clips.

## 9. Stop conditions

- **The body doesn't fit (rewritten 2026-09-30 for option B):** at the chosen PPU, the standing body height on Idle
  frame 0 is not 0.56 u within one pixel.
- **The collider sticks out:** on any frame of Idle, Walk or Run, facing either way, the collider's front or back edge
  sticks out past the painted nose or the tail tip. Stop and show the developer.
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

## 11. Results (Stage 1, 2026-09-30)

- **Import log:** 26 sheets, PPU 143.3036. Android ASTC 6×6: colour 2.76 MB, normals 2.76 MB, total 5.52 MB. The pivot
  falls at 104.65 px across on every sheet (the shared world pivot); each sheet's own cell and pivot are in the manifest.
- **Cat_Player:** every sprite reference (Idle_00 ×3, Walk_00–09, Rise_00, Fall_00, Land_00) resolves to a non-blank
  frame of the new art. The prefab diff is Unity's `m_Size` only (ruled 2026-09-30).
- **Tests:** `CatSheetImportTests` 35/35 (red before the import). The full EditMode suite in batch on the clone:
  1510/1510, which is the PAX-096 baseline 1482 plus the first 28 `CatSheetImportTests`; the 7 cases added after the
  review passed through MCP, so the total is 1517. Offline Stage 0 tests 30/30; `cat_register.py --check` identical.
- **Colour bleed (ruling 3):** Stage 0 had none (transparent pixels were black), so it was added (§3.3). Only the 26
  colour sheets changed; alpha and visible pixels are identical. Unity's Alpha Is Transparency already dilated on
  import, so the Editor renders didn't change.
- **Rim and specks:** the warm rim is painted rim light in the source art. The faint specks outside the silhouette are
  resampling residue (alpha ≤ 7/255, within 2 px of the body). Both are visible only magnified about 6× past game scale.
  **Check on device.**
- **Rise_00** hangs 0.25 u below the paw line: recorded as a V07 check (ruled 2026-09-30).
- **Review (pax-reviewer, one round):** accept after fixes, no Blockers. Fixed: the environment PPU is now tested by
  path and against every imported Reality A environment sprite; the normal maps' own import settings, the slot count
  and each sprite's rect are asserted; the sheets' default max size is 8192 (Walk and Climb were capped at 2048 off
  Android); A02 line 4 and §7 were corrected; the D-044 line follows D-061 (ruled 2026-09-30).
- **Pre-existing, not A08:** frozen Reality B's `B_GAME_Platform_Fill` and `B_GAME_Wall` carry PPU 196.667, against the
  importer's 98.333 rule for materials.
