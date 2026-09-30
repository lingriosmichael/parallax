# PAX-A14 · Cat A animation polish, offline and lean

**Status:** Draft 2026-09-30 (lean rewrite), for the developer's approval.
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
