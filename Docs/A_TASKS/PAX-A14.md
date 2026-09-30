# PAX-A14 · Cat A animation polish, offline and lean

**Status:** Approved and run 2026-09-30 (the developer ran `/pax-ticket`); results in §9, for the developer's review.
**Folder:** `Docs/A_TASKS/` · **Phase:** E, look and feel · **Implementer:** Claude Code
**Phase 1 size:** None. The open choices are defaults in §6; the developer can override them before the run.
**Depends on:** PAX-V07 (the clip table, `CatVisualSetup`, the capture harness, `CatAnimationCheckTests`) and PAX-A08
(`cat_register.py`, the manifest, `CatSpriteImporter`).
**Decisions:** D-036 (flipbook, shared pivot), D-052 (the 1.0 × 0.56 collider), D-067, D-075.

**Developer rulings (2026-09-30):**
- **Offline only.** No AutoSprite, no image generation of any kind, no credits. New frames come from the existing Cat A
  frames through deterministic tools.
- **Lean.** No Unity MCP, no open Editor, no critic loops. Work and measure in Python. Unity runs once, at the end, in
  batch mode on a clone.
- Art defects aren't hidden in code (V07 §12). This ticket fixes them in the art.
- The clip retirements are in §2.

## 1. Why

V07's final critic pass rated all four areas poor. Almost none of it is code:
- the clips disagree with each other (scale, style, specks);
- the art has no frames between clips.

Generating again won't make AutoSprite's separately generated clips meet. This ticket makes one consistent cat and a
few in-betweens from the frames we already have.

## 2. Retired and kept clips (ruled 2026-09-30)

**Retired:**
- tail flick
- dizzy
- splash death (v1 has no water)
- Turn (`CatA_Turn`; it isn't played since V07)
- the current Launched clip. The Launched state stays on the backlog; a geyser shows Rise meanwhile.

**Kept:**
- climb down: A14 target 5
- fed up → PAX-V07b
- menu idle → PAX-A09
- door enter: imported, not wired

Retired means recorded here and in the A02 manifest's Cat A notes. Nothing in `Art_Source/` is deleted or changed.

## 3. The work

### A. The consistency pass: a new stage in `cat_register.py`, one module `cat_consistency.py`

1. **Scale.** Each clip is normalised to the reference body, recorded per slot in the manifest.
   - Idle frame 0's standing height stays at 0.56 u (A08 §3.1).
   - Run is matched by torso length, so the gallop stays low.
2. **Style.** Every clip's colour and contrast is matched to Walk's, on opaque pixels only; alpha is untouched. This
   covers Idle, IdleLook, the deaths, Door and Respawn.
3. **Specks.** Detached low-alpha islands are removed.
4. **The spiked death's reach.** It is re-registered so its paws meet the spikes' edge.

### B. In-betweens, top targets only: one module `cat_inbetween.py`

Techniques, simplest first:
- re-use an existing frame;
- rotate or shift a cel about the pivot;
- splice two frames along a feathered seam;
- a numpy mesh-warp morph between two frames.

| # | Seam | Wanted |
|---|---|---|
| 1 | Walk ↔ Run | 1–2 frames each way: the body lowers, the tail levels |
| 2 | Fall → Land / HardLand | 1 contact frame with compression |
| 3 | Flip | 2–4 roll cels between the existing 4 (rotations), plus a tuck and an uncurl |
| 4 | Climb grab | 1 frame between the side and back views |
| 5 | Climb down | Wire `09_vine_climb_down` if it pops and slides less than Climb played in reverse |

Each target gets a **time box of about 30 minutes**. One that doesn't come out clean is written down with its number and
dropped, not chased.

Bridge frames go in the clip table as frames played at a seam, never inside a loop. The presenter and setup take the
smallest change that plays them.

## 4. How it's checked

- **While working:**
  - The pop numbers (the silhouette centroid jump at a seam, in phone px) come from a Python script on the sheets, in
    seconds. It is the metric V07's harness uses.
  - Preview GIFs and one contact sheet per target, for the developer to look at.
- **Once at the end, in batch on a clone (unattended):**
  1. the import;
  2. the Cat Visual setup;
  3. the cat fixtures: `CatSheetImportTests`, `CatClipTableTests`, `CatAnimationCheckTests`, `CatAnimStateMachineTests`,
     `CatVisualOnlyParityTests`;
  4. one full suite;
  5. one capture of the affected scenarios, for before/after videos.
- **Done when:**
  - the targets' seams are under 4 phone px, or each one that isn't is reported with its number;
  - Idle, Walk and Run body heights are within 2 % of each other;
  - there are 0 detached islands;
  - the tests are green.
- **The developer:**
  - looks at the GIFs and videos;
  - runs PARALLAX → Setup → Cat Visual in the Editor;
  - does the play check.

  Device feel stays with Phase H.

## 5. Allowed files

- `Tools/Art/cat_register.py`, the new `cat_consistency.py` and `cat_inbetween.py`, and their tests.
- `Art_Source/AutoSprite/Cats/A/_import/**`.
- `Assets/_Game/Art/Cats/CatA/*.png`, through `CatSpriteImporter` only; the `.meta` files are Unity's.
- `Assets/_Game/Editor/Art/CatVisualSetup.cs`, `CatSpriteImporter.cs`.
- `Assets/_Game/Gameplay/Presentation/CatClipSet.cs`, `CatVisualPresenter.cs`: bridge frames only.
- The cat test files named in §4.
- `Docs/Art/A02_asset_manifest.md`, `Docs/Art/NEEDED_ASSETS.md`, and this ticket.
- `Cat_Player.prefab` and `CatA_VisualConfig.asset`, through the setup menu only.

## 6. Defaults (override before the run if you disagree)

- The morph uses numpy only: no new Python dependency.
- **No presentation tricks.** No cross-fade, no procedural squash, no scale ease.
- **No stand-up from the sit.** §3 says input moves the drawing on the same frame; the sit still cuts straight to Walk.
- Run is matched to Walk by torso length.
- The style reference is Walk.
- Bridge frames go on their own small sheet per seam, as a new slot in the manifest.

## 7. Out of scope

- Any generation.
- The motor, collider, routes, levels and trap timing.
- Cat B.
- Draw order against trap art.
- The flip's 90° body rotation.
- The Launched state, menu idle, fed up, door enter.
- Targets beyond §3 B's five. They stay in `NEEDED_ASSETS.md` for later.

## 8. Stop conditions

- The scale pass moves Idle's standing height off 0.56 u by more than a pixel.
- A bridge frame changes the cat's motion (a parity test differs) or delays input.
- A consistency result reads worse than the original. That clip stays as it was and is reported.

## 9. Run results (2026-09-30)

Offline throughout: no AutoSprite call, no generation, no MCP, nothing in `Art_Source/AutoSprite/Cats/A/<clip>/` changed.
Unity ran only at the end, in batch on the work clone.

### A. Consistency pass (`cat_consistency.py`, a stage of `cat_register.py`)

| Item | Result |
|---|---|
| Scale | Run was drawn about 18 % small (head 27 px against Walk's 33; eroded torso 81.5 against 100 sheet px). Scaled by its torso length to Walk's: **1.183**. Idle's standing height is unchanged (107 source px, PPU 143.3036). Body height medians at the sheet: Idle 81, Walk 81, Run 76 px: Idle and Walk are within 2 %; Run's gallop stays 6 % lower, as §3 A asks ("the gallop stays low"). No other clip measured off-scale (standing frames 78-80 px against Idle's 80). |
| Style | 25 clips matched to Walk's luma distribution (opaque pixels; RGB scaled per pixel, hue kept, alpha untouched). **Death_Zapped is not** (§8 stop condition: matched, its electric glow turned into a grey smudge; it stays as it was). |
| Specks | 1,371 detached never-drawn islands removed; **0** remain. Detached pieces with drawn pixels (zap sparks, an arrow frame's tail tip) stay. |
| Spiked reach | Re-registered +0.44 u: in Trap Lab room 0 the kill happens with the spike edge at x 5.00 and the cat's root at 4.56; frame 0's paws now meet that edge. |

### B. In-betweens (`cat_inbetween.py`); seams in phone px (80 px/u, the V07 harness's centroid metric)

| # | Seam | Before | After | How |
|---|---|---|---|---|
| 1 | Walk ↔ Run | 7.7-8.8 | **2.6-3.8** | No bridge needed: the scale pass. New foot-matched switch tables (leg overlap ≥ 0.40): Walk 0/1/3/4/5/8/9/10 → Run 6/1/5/6/6/5/5/6; Run 1/5/6 → Walk 1/3/10. |
| 2 | Fall → Land | 6.0 (into the stand, Land 2; no squash) / 15.3 into the crouch | 8.7 → 6.7 | `LandContact`: Land 0's crouch raised 20 sheet px over its paws. Land = [contact, 0, 1, 2], entries {contact, Land 0}. |
| 2 | Fall → HardLand | 18.8 | 11.7-12.2 → 7.9 | `HardLandContact`: HardLand 1 raised 28 px. HardLand = [0, contact, 1, 2, 3], entries {contact, 1, 2, 3}. |
| 3 | Flip roll | 10.0 / 4.2 / 6.0 | **1.9-3.4** | `FlipRoll`: 4 cels turned between the drawn ones (-102°: two, -45° and -57°: one each); 8 cels at 24 fps, still 0.33 s. The tuck and uncurl are **dropped** (the flip starts from any pose; one generic tuck needs drawing). |
| 4 | Climb grab | 14.2 (Walk), 18.5 (Fall) | not done | **Dropped.** A rearing grab (Walk 1 turned upright) splits it 7.0 + 7.1, but placed between the poses its hind paws draw ~0.3 u into the floor at a vine's foot, and it needs new timed-bridge code in the presenter (403 lines). |
| 5 | Climb down | Climb reversed: max step 1.5, loop close 0.89 | not wired | `09_vine_climb_down` measured worse: max step 5.6, loop close 0.75, and keying out the vine cuts the body and head. |

Bridge frames live on their own sheets (§6), as manifest slots with `bridgeFor` and `bridgePositions`; `CatVisualSetup`
plays them inside their host clip. The presenter and `CatClipSet` are unchanged.

### Developer ruling during the run (2026-09-30)

The new switch tables exposed an asymmetry in the V07 harness: `walk-above-run` allows Walk to wait for a switch frame,
`run-below-exit` allowed only the 2-frame lag, so `ground_ramp` failed. Ruled: mirror the rule. `CatCaptureParity.cs`,
`CatCaptureChecks.cs` and `CatCaptureReport.cs` (outside §5) now give Run the same wait for its switch frames at the
run-exit speed.

### C. The moving-floor glitch (developer's report on L017's `Slide_4`, added to A14 by the developer, 2026-09-30)

Two causes, both confirmed:
1. **The cat walked on the spot.** The presenter measured the cat's speed from its drawn motion, the floor's included: a
   still cat on `Slide_4` (1.7 u/s) read as walking (walkEnter 0.6); on Trap Lab room 12's Mover (5 u/s) it flickered
   Walk/Run/Walk (capture: 209 frames of `walk-while-carried`).
2. **The cat and the floor juddered.** Measured in play mode (a throwaway probe on the clone): writing
   `Rigidbody2D.position` drops that body's interpolation for the tick (the body shows each tick's pose: 0.1, 0.1, ..., 0
   u a frame at 5 u/s and 50 on 60 Hz), while a velocity or kinematic `MovePosition` body glides (0.0833 u every frame);
   a zero write keeps it. `ApplyCarry` writes the position every tick a floor moves, and moving floors didn't interpolate:
   both snapped together, 6.7 phone px of judder against the background.

Fix (presentation only; the motion is unchanged, the parity and rewind fixtures green):
- `MovingTrap.Awake` sets its body to interpolate (no scene rebuild; kinematic `MovePosition`, measured smooth).
- `CatMotor2D` exposes read-only `CarrierVelocity`, `CarryShift` and `StepStartPosition` (written in `Step`/`ApplyCarry`,
  never read by motion).
- `CatVisualPresenter` draws Visual where interpolation would have on a tick the carry wrote the position
  (`StepStartPosition` + interpolation × the tick's move), and measures the cat's own motion against the Carry floor's
  drawn motion. `LateUpdate` passes Unity's interpolation fraction; the harness passes its own.
- The harness draws the room's other bodies as the player loop does (interpolated only when their body interpolates), shows
  the tick pose on a carry tick, and has a `carry_stand_down` scenario, a `walk-while-carried` rule and a `carried` check
  (the cat's and the floor's judder against their steady speed, and the cat against the floor).

Result on `carry_stand_down` (212 steady frames): judder 6.7 → **0.005** phone px (cat and floor), cat against the floor
0.00, `walk-while-carried` 209 → **0** frames. Not covered: a vertically moving Carry floor (its lift goes through the
body's velocity, which interpolates; not measured), and L017 itself (the Trap Lab Mover is the same trap kind, 3x faster).

### Deviations

- The presenter is 441 lines (it was 403): the carry drawing is one self-contained presentation concern.
- Files outside §5, approved by the developer during the run: `CatCaptureParity.cs`, `CatCaptureChecks.cs`,
  `CatCaptureReport.cs`, `CatCapture.cs`, `CatCaptureScenarios.cs` (harness), `CatMotor2D.cs` (read-only getters),
  `MovingTrap.cs` (interpolation), `CatAnimationCheckTests.cs`.

- `CatSpriteImporter.cs`, `CatClipSet.cs` and `CatVisualPresenter.cs` needed no change.
- The cat test limits for the landings are measured values (8.7 / 6.7 and 11.7-12.2 / 7.9), not the 4 px target, which
  one frame can't reach; §4's "reported with its number".

