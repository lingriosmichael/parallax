# Cat capture harness (PAX-V07 gauntlet item 0)

Sees the cat the way a phone shows it, and checks it automatically. Editor tooling only (§11 R8): nothing in it ships,
it saves no scene or asset, and the open scenes are restored afterwards.

## Files

| File | What it is |
|---|---|
| `Assets/_Game/Editor/Art/CatCapture.cs` | Entry points (batch `Run`, menu `PARALLAX/Art/Cat Capture`), the frame loop, the camera, the per-frame record, the outputs. |
| `Assets/_Game/Editor/Art/CatCaptureRig.cs` | The rig: the room built by `SoloRoomBuilder`, the real `Cat_Player.prefab`, the gameplay part of `_LevelTemplate`, a scripted `ICatCommandSource` in the real `CatInputRouter`. Built from the same public parts as `RouteHarness` (RouteReplay.cs is not edited). |
| `Assets/_Game/Editor/Art/CatCaptureScenarios.cs` | The scenario list and the step kinds. |
| `Assets/_Game/Editor/Art/CatCaptureChecks.cs` | Thresholds (`CaptureThresholds`), the sprite-alpha cache, the per-frame measurement, the state-rule types. |
| `Assets/_Game/Editor/Art/CatCaptureReport.cs` | The checks over a scenario's frames (`checks.json`), including the state-vs-motor rule table. |
| `Assets/_Game/Editor/Art/CatCaptureParity.cs` | (item 1) `CatCapture.VisualOnlyParity` for `CatVisualOnlyParityTests`, and the presenter thresholds the state rules read. |
| `Assets/_Game/Editor/Art/CatCaptureGrid.cs` | Capture-only overlays drawn onto each rendered frame: the world grid and surface ticks, and the death-hold silhouette. |
| `Assets/_Game/Editor/Art/CatCaptureMath.cs` | Pure math (tick schedule, interpolation, pixel projection, depth, clusters, transition selection). |
| `Tools/Art/cat_capture_sheet.py` | The contact sheets (Python 3 + Pillow). |
| `Assets/_Game/Tests/EditMode/CatCaptureTests.cs` | EditMode tests: the math, and `CatVisualPresenter.Present` stepped in EditMode. |

`CatVisualPresenter` gained `Present(float dt)` (LateUpdate keeps its `isVisible` check and calls it) and read-only
`State`, `ClipName`, `FrameIndex`, `Facing` (§11 R9). Nothing else in it changed.

## Running it

Batch, on the work clone (never on the main project while its Editor is open):

```
$S/tools/sync.sh
$S/tools/unity_exec.sh Parallax.Editor.Art.CatCapture.Run <tag> -captureOut <dir> [-captureFilter <regex>] [-captureSheets] [-captureNoImages]
python3 Tools/Art/cat_capture_sheet.py <dir> [--only <regex>]     # if -captureSheets wasn't given, or to redo sheets
```

- `-captureOut` (required): the output folder (absolute, or relative to the project).
- `-captureFilter`: a .NET regex on scenario names; default `.*` (all). E.g. `-captureFilter 'traplab0|L001'`.
- `-captureSheets`: run the sheet script at the end (`python3` on PATH, or set `PARALLAX_PYTHON`).
- `-captureNoImages`: skip rendering (the checks still run; the sheets are then skipped).
- All four scenarios with images and sheets take about 70 s including the compile check (warm Library).
- Exit code 0 = ran (the checks may still FAIL, see `summary.txt`), 1 = crashed, 2 = no scenario matched / a dirty scene.
- The log lines start with `CAPTURE:` (`unity_exec.sh` greps them).

In the Editor: `PARALLAX/Art/Cat Capture` runs every scenario into `Logs/CatCapture/` (git-ignored) and the sheets.
The open scene must be saved first (the capture restores it from disk).

## What one frame is (§11 R14)

- A tick is exactly the player loop's: the scripted command is queued, `ObserverSet.FixedUpdate` runs (reflection, as the
  route harness), then `Physics2D.Simulate(Time.fixedDeltaTime)`; `RouteSession` sets `simulationMode = Script`.
- Frames are at t = n / 60 s. As Unity does, the first FixedUpdate runs in the first frame, so the frame at t has run
  every tick k with (k − 1) × tick ≤ t.
- Each frame places the cat root at the pose `Rigidbody2D` interpolation shows: lerp from the previous tick's body pose
  to the current one by (t + tick − k × tick) / tick (rotation by `LerpAngle`). A tick move of 1 u or more (a respawn)
  shows the new pose at once. Then `CatVisualPresenter.Present(1/60)`, every `TrapArt.Apply()` (their LateUpdate), the
  measurements, and the render.
- Before the next tick the root's exact post-physics transform is put back and `Physics2D.SyncTransforms()` runs, so
  physics never sees the interpolated pose. **Self-check:** every scenario is also run with ticks only (no frames); the
  body position and velocity must be bit-identical on every tick (`capture_parity` in `checks.json`; a failure also
  fails the scenario).
- The presenter runs without its `observer` field (prefab instance, no scene wiring); its `Awake` is invoked after the
  motor's.

## Images

- Camera: orthographic, `RenderPixelsPerUnit` = 160 px/u (2x phone scale; a phone is ~80 px/u), window 4.8 × 3.6 u =
  768 × 576 px, warm backdrop, one global `Light2D` (intensity 1) on every sorting layer. Rendered by `TrapShots.Rig`
  (Camera → RenderTexture → ReadPixels), which works in batch mode on macOS (Metal).
- Point lights are not rendered off-screen (known, see the project memory); the level's own lights and backdrop aren't in
  the rig, so colours are flat. The cat's pixels are where the game draws them (same sprites, transforms and renderers).
- The body's unlit **outline** renderer is drawn (1.5 texels around the body), so the drawn cat reaches about 1.5 sprite
  px beyond its alpha. The checks measure the body sprite's alpha only.
- Framing: horizontally the cat; vertically the height where the cat last stood (+1.0 u against gravity, which puts the
  floor line ~0.5 u from the window's edge and leaves room for a full 1.6 u jump); a critically damped spring (0.1 s);
  then **the cat's drawn pixels (its body sprite's opaque box) are kept 0.12 u inside the window on every frame**, so the
  cat is never cropped (`cat_box` in frames.jsonl; verified: 0 frames outside in all scenarios). A respawn snaps. The
  camera never rotates (D-020).
- **World reference (capture only, drawn onto the rendered image, never into the scene):** a faint grid every 1 u fixed in
  world space, and dark tick marks every 0.25 u (longer every 1 u) into every solid's top and bottom faces, from the
  current collider poses (a moving floor carries its ticks; a tilted shard gets none), starting 0.02 u inside the face so
  a paw on the surface is never covered. Nothing is drawn over the cat's own pixels (its body mask grown 3 px to cover the
  outline). Motion and foot slide read against the ticks.
- **Death hold:** the game itself sorts some room sprites above the cat (in `traplab2_pit_down`: RearmCollapse's shards
  and dust, and the death puff; listed per scenario in `scenario.json` → `drawn_above_cat`, taken on the first hold
  frame). The capture draws what the game draws, and on death-hold frames adds a **cyan outline of the cat's silhouette**
  (its body sprite's drawn pixels, projected exactly as the checks project them) on top, so the pose can be judged. Tile
  labels on those frames say `HOLD+outline`.

## Output layout

```
<out>/summary.txt                   one line per scenario (the headline numbers, PASS/FAIL)
<out>/summary.json                  every scenario's checks.json, as an array
<out>/<scenario>/scenario.json      name, description, gravity, frame rate, render/phone px per unit, view size, grid steps,
                                    drawn_above_cat (room sprites the game sorts above the cat)
<out>/<scenario>/frames.jsonl       one row per 60 fps frame (fields below)
<out>/<scenario>/checks.json        the checks, thresholds and worst frames
<out>/<scenario>/transitions.json   the cuts the sheets are made around
<out>/<scenario>/frames/fNNNNN.png  every frame (768 × 576, 160 px/u)
<out>/<scenario>/sheets/
    <scenario>__overview[_pN].png            every 6th frame (10/s), 160 px/u (2x phone), 36 tiles per part
    <scenario>__overview_phone[_pN].png      the same frames at 80 px/u (phone scale), 64 tiles per part
    <scenario>__tNN_fFFFFF_<kinds>.png       a transition: 8 frames before to 24 after (a death: to 44 after, the whole
                                             hold and the respawn), 160 px/u, the change framed in red; transitions on
                                             the same frame share one sheet, named for all of them
    <scenario>__strip_fFFFFF_<kind>.png      takeoff / landing / walk-off, and (item 1) every ground change (Idle / Walk /
                                             Run / Turn, a turn, a Cut step): 10 before to 8 after, a 2.2 × 2.0 u crop at
                                             320 px/u (4x phone) that follows the paws: the floor line at 80 % down
                                             (gravity up: 20 %) while the paws are near it, else the paws kept within
                                             70–90 % (up: 10–30 %), so no frame is empty
```

Transition kinds: every presenter state change (`From-to-To`, at most 8 of each kind per scenario since item 1), `takeoff` /
`landing` (the motor's grounded flag; not on the frames around a respawn), `turn` (the facing flips on the ground),
`walk-off` (leaving the ground with no jump in the last 3 frames), `death` (a hold starts), `respawn` (the hold ends),
and `step: <label>` for scenario steps marked `Cut` (walk → run, run → stop).

Scenario names end in their gravity (`_down` / `_up`), so every file name says which. Tile labels:
`f<frame>  state=<presenter state>  clip=<clip>[<frame index>]`, plus `HOLD+outline` on death-hold frames. Every sheet
title states its scale in px/u and relative to the phone (80 px/u).

`frames.jsonl` fields: `frame`, `time`, `tick` (ticks run), `step` (the scenario step), `state`, `clip`, `clip_frame`,
`sprite`, `facing` (+1 = the cat's local right), `grounded`, `climbing`, `jumped` (`JumpedThisStep`), `frozen`,
`holding` (`RoomDeath.IsHolding`), `gravity`, `v_along` (surface speed, cat-right positive), `v_gravity` (along gravity,
positive = falling), `root` (interpolated, world), `cam`, `paw` (the Visual's pivot = the paw row, world), `paw_img`
(the same in image pixels, from the top-left), `depth_sprite_px`, `cat_box` (the body sprite's drawn pixels, world box: min x, min y, max x, max y), `surface` (the solid under the deepest column),
`contact_px` (contact pixel count), `contacts` (item 1: each contact cluster's [first, last] position along the surface,
world units), `pop_phone_px`, `box_pop_phone_px` (null unless the sprite changed), `image`.

## The checks and their thresholds

All are computed analytically from the body sprite's alpha (the sheet PNG on disk), its rect, pivot and PPU, and the body
renderer's `localToWorldMatrix` on that frame (facing and the 180° root rotation in gravity up included); never from the
rendered image. A pixel is drawn at alpha ≥ 0.5. "sp" = sprite px (1 / PPU = 1 / 143.3 u); "pp" = phone px (1/80 u).

| Check | How | Threshold | Pass/fail |
|---|---|---|---|
| **paws_in_floor** | Every drawn pixel projected to the world. Per pixel column, a ray along gravity from the collider centre's height (the reality's physics mask, no triggers, not the cat; a ray that starts inside a solid is ignored) finds the surface under that column; depth = how far the pixel is past it along gravity. Frames checked: grounded, or airborne within 3 ticks of a grounded tick (takeoff / landing), and (item 2) every airborne frame whose drawn pixels come within 0.5 u of a surface along gravity (a surface is only searched that far past each column's lowest pixel), so a takeoff until 0.5 u clear and the last 0.5 u before touchdown are covered; not during a death hold, climbing, or the 3 ticks after a gravity flip (item 5's Flip; `head_in_ceiling` reports how many). | max depth ≤ **2 sp** | yes |
| **head_in_ceiling** (item 2) | The same against gravity: per column, a ray against gravity from the collider centre's height finds the solid above (a low ceiling); depth = how far the column's highest drawn pixel is inside it. Frames: all but a death hold, climbing and the 3 ticks after a gravity flip (`flip_frames_unchecked`). | max depth ≤ **2 sp** | yes |
| **float_gap** | On grounded frames: how far the lowest drawn pixel is above the surface. The frame of a takeoff shows a large gap (the motor's grounded flag is from the start of that tick). | 2 sp (frames over are listed) | reported only |
| **foot_slide** | At each sprite change during ground locomotion (grounded, and Walk, Run or moving), contact pixels (within 2 sp of the surface) are clustered (a gap > 3 sp splits paws). **Item 1:** a cluster continues a planted paw only where its contact pixels still overlap the ground that paw covered at the previous sample (`CatCaptureMath.MatchPlanted`); item 0 matched any cluster within 0.25 u, which chained a cat's hind paw landing just ahead of its lifting fore paw (direct register) into one "paw" and reported the gap between them as a slide. A paw's drift is its largest distance along the surface from where it planted, until it lifts or locomotion ends; paws seen at ≥ 2 samples. A slide of more than a paw's width (~12 sp) in one sample counts as a lift and a new plant. **Item 1:** a sprite can only change on a 60 fps frame, so a paw exactly on its print in continuous time shows up to one frame's travel (speed / 60) off it at the sample; the allowance is 3 pp plus that at the plant's top speed (`allowed_phone_px` per paw; `paws_over_3pp_without_allowance` is reported beside it). | drift ≤ **3 pp + speed/60** per paw | yes |
| **pose_pop** | At every sprite change (not in a hold, not a respawn): the silhouette centroid's move minus the move of the sprite's carrier (item 2: the Visual's pivot, which is the root's except while the presenter draws an air pose off the root: TakeOff's paws kept on the ground, an air pose kept clear of a low ceiling or a step; `carrier_offset_max_phone_px` reports the largest one-frame difference). The bounding box's largest edge move minus the carrier's is reported beside it. | centroid ≤ **3 pp**; box 6 pp (reported) | centroid only |
| **state_vs_motor** | A rule table (`CatCaptureReport.Rules`), evaluated per frame; frames in a death hold are skipped and a hold or a respawn resets the counts. Rows today: `air-state-while-grounded` (Rise/Fall while grounded > 1 frame), `ground-state-while-airborne` (Idle/Walk/Run while airborne, not climbing, > 1 frame), `climb-without-climbing`, `land-late` (after ≥ 3 airborne frames, Land not shown by the frame after touchdown); item 1: `turn-off-ground` (Turn on any frame not grounded), `run-below-exit` / `walk-above-run` (Run below runExitFraction × MaxSpeed, Walk at or above runFraction × MaxSpeed, judged on the drawn (interpolated) speed, > 2 frames), `facing-late` (moving against the facing above walkEnter, grounded, longer than the Turn clip + 2 frames); item 1 round 2: `one-frame-state` (any state shown for exactly one frame between frames of other states: a flicker); item 2: `air-state-while-grounded` covers Rise, Apex and Fall (and TakeOff after the jump tick), `ground-state-while-airborne` allows a ground state while the cat has dropped no more than the presenter's `airGraceDrop` below where it stood (a one-tick ground blip, the first moment off a ledge), `takeoff-late` (a jump from the ground with TakeOff shown neither on the jump tick's first frame nor the next), `apex-outside-band` (Apex while the drawn speed along gravity is outside ±airThreshold, > 2 frames), and `land-late` now fails any air state (TakeOff, Rise, Apex, Fall) still shown on the frame after touchdown (a running landing may show the gait there). "Grounded" in the rules is the motor's flag less `sunk` (item 2: grounded, but the cat's bottom is more than the presenter's `groundSinkTolerance` below its ground's top, a capsule rolling off a ledge's corner, which the presenter shows as off the ground). Frames of a step marked `Setup` (item 1: the gravity-up scenarios' flip and fall to the ceiling) are rendered but left out of every check (`setup_frames_unchecked`). | 0 frames | yes |
| **hold_timing** | Placeholder for item 6 (a death clip's held frame is shown by the end of the hold). | — | no |
| **capture_parity** | Ticks with frames vs ticks alone: body position and velocity per tick. | identical | yes |
| expected death | The scenario's `ExpectDeath` matches whether `RoomDeath.IsHolding` was ever seen. | — | yes |

Tunables: `CaptureThresholds` (CatCaptureChecks.cs), the camera and sheet constants at the top of CatCapture.cs, the overlay constants in CatCaptureGrid.cs, and
the sheet constants at the top of cat_capture_sheet.py.

## Scenarios (today)

| Name | Room | Script |
|---|---|---|
| `traplab0_walk_down` | Trap Lab room 0, cat at x 7 (clear of SourceSpikes' trigger at x 5 and the door at 21.7) | stand 1 s, slow walk right (0.4) 1.5 s, run 1.5 s, release to a stop, stand 0.5 s, run left 1 s, stop, stand 0.5 s, jump in place, stand 0.5 s, run left 0.3 s, jump while running, land, run 0.1 s, stop, stand 0.5 s |
| `traplab3_walk_up` | Trap Lab room 3, cat at x 1.5 | `GravityReceiver.Flip()` at the start, the fall to the ceiling, then the same script (screen-relative). Room 3 because room 0's ceiling has three falling blocks hanging 0.5 u below it (they stop a cat walking on the ceiling) and rooms 1 and 2 have a ceiling hazard / a block; room 3's ceiling is clear from x 0 to the Backboard (15.0). |
| `L001_solution_down` | L001 | L001's solution (`L001Routes`) replayed first by `RouteHarness.Replay`; its per-tick commands (Move, Jump, Climb) from the replay's records are played tick for tick, then 0.3 s of nothing. |
| `traplab2_pit_down` | Trap Lab room 2, cat at x 19 (past RearmBlock's trigger) | stand 0.5 s, slow walk onto RearmCollapse until x ≥ 23.6, stand: the floor collapses, the fall into the pit, the death on its hazard, the hold, the respawn; 1 s after. Expected to die. |

Item 1 added the ground scenarios, each in both gravities (`_down`: Trap Lab room 0, or room 1 for the wall; `_up`: Trap Lab
room 3's ceiling, after a `Setup` flip and fall): `ground_idle` (stand 3 s), `ground_slow` (analog 0.3 right, stop, analog 0.5
left, stop), `ground_ramp` (analog ramps 0→1→0 right, then left), `ground_digital` (digital start and stop, right then left),
`ground_turns` (turn at walk speed, at run speed, a quick double turn), `ground_wall` (run into room 1's FixedPillar / room 3's
Backboard and keep pushing, release, walk away, walk back into it), and `parity_ground` (jumps and reversals pressed during Turn,
Walk, Run and Idle; for `CatVisualOnlyParityTests`, not the critic). New step kind: `Ramp(from, to, seconds, label)`.

Item 2 added the air scenarios, each in both gravities, in a capture-only **air bench** room (`CatCaptureAirScenarios.cs`; a
floor and a ceiling 7 u apart, a low slab over x 3–6 with its underside at 1.3, a 1 u ledge over x 14–17, a 3.2 u tower over
x 28–31; its `_up` twin mirrors every block top to bottom, and the cat flips on the floor and falls onto the mirrored surface
above it — that flip, fall and landing are checked, unlike the ground scenarios' `Setup`): `air_standing_jump`,
`air_running_jump` (landing running, released in the air, a walking jump), `air_ledge` (onto the 1 u ledge, then a walk off
its end), `air_tower_drop` (a 3.2 u walk-off, released: a hard landing), `air_tower_run` (run off it: a running landing),
`air_tower_jump` (facing screen-right, a jump left off it, against the facing: a 4.8 u fall), `air_low_ceiling` (a jump into the slab, a walking jump out), `air_geyser_down` (Trap Lab
room 8's vent) and `air_geyser_up` (the bench with a ceiling vent erupting downward), and `parity_air` (inputs during TakeOff,
Land and HardLand; for `CatVisualOnlyParityTests`). New frame fields: `sunk`, `ceiling_depth_sprite_px`,
`carrier_offset_phone_px`. `-captureFilter 'measure_'` also offers `measure_L0NN_down` (every level's solution, no critic
use: item 2 measured the landings' fall distances with it).

## Adding a scenario

Append a `CaptureScenario` to `CatCaptureScenarios.All`: `Name` (ending in `_down` or `_up`), `Description`, `Room`
(a `SoloRoomDefinition`, e.g. `TrapLabLayout.Rooms[n]` or `LevelLayouts.ById["L0NN"]`), `StartX` (room-local collider
centre x, standing on the checkpoint's floor height; null = the checkpoint), `ExpectDeath`, and `Steps`:

- `Hold(move, seconds, label)`: hold an analog move (−1…1, screen-relative) for whole ticks (`TickTime.ToWholeTicks`).
- `Press(move, label)`: one tick with a jump press.
- `Until(move, condition, maxSeconds, label)`: hold until the condition (read after the previous tick) is true; logs a
  warning on time-out. Conditions: `Grounded`, `Airborne`, `Still`, `XAtLeast(x)`, or any `Func<CatCaptureRig, bool>`.
- `Act(action, label)`: a zero-tick setup action (e.g. `r => r.Gravity.Flip()`).
- `Replay(commands, label)`: a per-tick command array (see `RouteCommands`).
- `{ Cut = true }` on a step makes the sheets cut a transition where that step starts (e.g. walk → run, run → stop).

A climb scenario needs `CatCommand.Climb`: add a step kind that sets it (the rig passes Climb through unchanged).

## Adding a check

- A state rule: append a `StateRule(name, description, context => broken)` to `CatCaptureReport.Rules`. The context has
  this frame's row, the previous row, and the consecutive grounded / airborne frame counts.
- A new measurement: add it to `FrameMeasure` / `CatCaptureMeasure.Measure` (per frame) or to `FrameRow`, evaluate it in
  `CatCaptureReport.Evaluate` with a threshold in `CaptureThresholds`, and document the threshold here.
- Per-frame fields go into `CatCapture.Jsonl` too, so the sheets and later items can read them.

## Baseline (today's presenter, before items 1–4; 2026-09-30)

| Scenario | paws max (sp) | float gap mean / max (sp) | foot slide max / mean (pp), paws over | pose pop max (pp), pops over 3 pp | state rules | result |
|---|---|---|---|---|---|---|
| traplab0_walk_down | −0.9 (0 over) | 1.6 / 11.0 | 36.1 / 12.6, 18 of 19 | 15.4, 8 of 55 changes | 0 | FAIL |
| traplab3_walk_up | −1.6 (0 over) | 1.7 / 20.3 | 22.1 / 11.3, 18 of 19 | 15.4, 10 of 58 | 0 | FAIL |
| L001_solution_down | 0.3 (0 over) | 1.8 / 15.6 | 15.9 / 11.2, 10 of 10 | 15.4, 17 of 38 | 0 | FAIL |
| traplab2_pit_down | 7.9 (2 over: frames 128–129) | 1.6 / 2.7 | 13.2 / 10.5, 4 of 4 | 15.4, 4 of 10 | 0 | FAIL |

The biggest pops are the same sprite pairs everywhere: Fall_00 → Land_00 (15.4 pp), Land_00 → Walk_00 (11.9),
Land_00 → Idle_00 (11.2), Rise_00 → Fall_00 (10.1). Capture parity holds in every scenario.
