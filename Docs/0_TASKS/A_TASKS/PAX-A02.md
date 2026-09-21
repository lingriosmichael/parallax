# PAX-A02 — Environment pass: the sandbox gets a world

```text
TASK: PAX-A02
TITLE: Environment kit per reality — backgrounds, platforms, puzzle objects, light, parallax
PHASE / GATE: Art lane (D-031 pipeline test) / after PAX-A01 and PAX-V01; runs in parallel with PAX-028 → PAX-035
TYPE: Art pipeline + presentation wiring. Editor only (device FPS check deferred to the device session).
```

## OBJECTIVE

Replace the grey boxes in `Sandbox_Realities` with a first-pass environment kit for each reality, so Reality A reads as warm ruins and Reality B as cold obsidian geometry. The puzzle objects (vine, elevator, plate, gate, station, checkpoint) should be recognisable at a glance. No gameplay code, collider or tuning value changes.

## CONTEXT

- `00_VISION.md` art direction. **A:** sandstone, roots, ruins, cloth banners, vegetation, warm golden light, dark reflective water. **B:** obsidian, glass, grids, cyan edge light, void, monoliths, cold mist, stars. In-game style evokes the key art with fewer layers, strong silhouettes and phone-scale contrast.
- `07_DECISIONS.md` D-031: the art pipeline test starts now. Scope is one cat plus a small kit per reality. Placeholder-quality in-game art is allowed. ChatGPT concepts are the style reference.
- `02_ARCHITECTURE.md` principles:
  - §1.8: **artwork never decides collision.**
  - §2: `Geometry/` = invisible colliders + visual-only art. Per-reality sorting layers `A_*` / `B_*`. Light2Ds target their own reality's sorting layers only.
  - §9: changing art never changes anchor or network code.
- Plan §18.4: layer breakdown, parallax depths (Sky 0.05 · Far 0.15 · Mid 0.35 · Gameplay 1.00 · Foreground 1.20), per-reality atlases. Lighting: A = soft warm global light, B = dim global light + cyan emissive accents.
- PAX-A01: the cat sprite is already in. Its import settings (PPU, filter, compression) are the reference for everything here.
- PAX-V01: world-space **state labels** already sit on puzzle objects. **Division of work:** V01 labels say what *state* an object is in (pressed, open, seated, reached). A02 art says what an object *is*. A02 adds no state visuals and must never hide, cover or disable a V01 label.

## REFERENCES (in repo)

The design images in the repo's `DesignImages/` folder are the style reference for every asset in this ticket. The most relevant files are:

- `14_environment-breakdown`: primary reference for layers, materials and palette per reality.
- `02_core-concept_v2`: overall look of A vs B side by side.
- `03_vine-elevator-anchor_v2`: the vine (A) and elevator (B).
- `04_gravity-shift`: Control Station look.
- `06_echo-replay`, `05_solo-reality-switch`: readability of cats against the environment.
- `13_cat-character-sheet`: scale and style match with the A01 cat.
- Later tickets only (don't build now): `07_shadow-bridge`, `08_scale-contradiction_*`, `10_world-fold`, `11_reality-collapse`, `12_final-convergence`, `15_premium-mobile-ui`.

**Step 0 of Stage 1: verify the references are usable.** The files show as `*.png.txt`. Before anything else, Claude Code runs `file` and `head -c 200` on each one and reports which case applies:
- **Git LFS pointers** (text starting `version https://git-lfs`): run `git lfs pull` and re-check. If the real PNGs don't appear, stop and report.
- **Text descriptions / prompts** of the images: use them as written style specs, and ask for the actual PNGs to be added under `Docs/Art/Reference/`.
- **Real PNGs with a wrong extension**: rename them to `.png` in a separate commit, keeping them LFS-tracked.

Claude Code must open the actual images, not only file names, when writing the manifest and prompts.

## HOW IT WORKS (the core idea)

**Art follows colliders, never the reverse.** Level geometry stays authored as colliders (grey boxes). A Setup script gives each collider object a visual-only child sprite in **Tiled** (or Sliced) draw mode, sized to the collider's bounds, and hides the greybox renderer. Resizing or adding a platform later means re-running the Setup script, and the art follows.

**Assets are picked up by filename convention.** If a file for a slot is missing, that slot keeps its greybox. Art can arrive one piece at a time, and the scene is never broken.

## STAGES

This ticket runs in three stages in the normal linear loop. Stages 1 and 3 are Claude Code. Stage 2 is split: Claude Code generates props through AutoSprite, and you make tileables and backgrounds.

### Stage 1 — Claude Code: inventory, manifest, pipeline

**1a. Scene inventory.** List every visible object in `Sandbox_Realities` per reality: platforms, walls, ceilings, hazards/fall volumes, and every puzzle object with its manifestation class. Record its collider size in world units and whether it moves (elevator, gate, etc.). For each object, also name **exactly which renderer is the greybox**, which renderers are driven at runtime (station glow/pulse, etc.) and which belong to PAX-V01 labels. List every **anchor link** (which objects share an `AnchorId`, in which realities).

**1b. Write `Docs/Art/A02_asset_manifest.md`.** This is your shopping list for Stage 2. One row per slot:

| Slot file name | Reality | What it is | Source (AutoSprite/Manual) | Reference image | Draw mode | Target size (px) | Pivot | Pair motif | Notes |
|---|---|---|---|---|---|---|---|---|---|

Minimum slots per reality (Claude Code adjusts the list to the inventory and drops slots for objects that don't exist in a reality):

- **Backgrounds (parallax):** `BG_00_Sky`, `BG_01_Far`, `MG_01_Mid`. Each is a wide, horizontally tileable strip. A: golden sky with clouds, far floating ruins, mid arches. B: void with stars, far monoliths, mid wireframe grid.
- **Geometry:** `GAME_Platform_Top` (tileable surface edge), `GAME_Platform_Fill` (tileable body), `GAME_Wall` (tileable).
- **Puzzle objects** (only where they exist): `OBJ_Vine`, `OBJ_Elevator`, `OBJ_Plate`, `OBJ_Gate`, `OBJ_Station`, `OBJ_Checkpoint`, `OBJ_Hazard` (A: dark water; B: void edge).
- **Foreground:** one `FG_01` accent per reality. A: hanging roots or banner. B: mist or floating shards.

Naming: `Art/RealityA/Environment/A_<Slot>.png` and `Art/RealityB/Environment/B_<Slot>.png`.

**Pair motif:** objects linked by an anchor (e.g. vine in A ↔ elevator in B) share one simple, named motif (a shape or glyph, e.g. a leaf-shaped notch) drawn in each reality's own palette, so a player can tell what is linked to what without labels. Fill the column for every linked object; leave it empty for unlinked ones. Include the motif in the generation prompt for both sides.

Sizes: derive from the collider sizes and the **PPU used by the A01 cat** (read it from the cat's importer), so a platform tile and the cat share one pixel density.

**1c. Pipeline code and Setup.**
- **Import preset** matching the A01 cat, applied automatically by an `AssetPostprocessor` to everything under `Art/RealityA/Environment` and `Art/RealityB/Environment`: PPU, filter mode, compression, Full Rect mesh for tiled sprites, wrap mode.
- **Sprite Atlases**: `Atlas_RealityA_Env`, `Atlas_RealityB_Env`.
- **`ParallaxLayer`** runtime component, visual only. It offsets a background layer relative to **its own reality's Observer camera** and that reality's `RealityRoot`. The B offset (`RealityRoot.OffsetB`, never a hard-coded (0, 1000)) must not leak in: compute from camera position minus reality origin. It also tiles horizontally so the strip never runs out.
  - Runs in `LateUpdate` **after** `CatCameraFollow` (execution order), so layers don't lag the camera by a frame and jitter.
  - Does no work while its reality's camera is disabled (inactive Observer, PiP off), per the Performance rule for the inactive reality. When the debug PiP enables that camera, it runs again, and it snaps to the correct offset on its first active frame.
- **`PARALLAX/Setup/Environment Art`** menu, idempotent:
  1. For each geometry collider object: create or update a child `Art` with a `SpriteRenderer` (Tiled/Sliced, sized to collider bounds, on `<R>_Gameplay`), then disable **only the greybox `SpriteRenderer` named in the inventory**, never every renderer on the object or its children. Never touch or add colliders.
  2. For each puzzle object: add the `OBJ_*` sprite as a visual child of the **manifestation's moving transform**, so it rides the existing animation. **Preserve any renderer that gameplay or presenters drive** (station glow/pulse, anything else that changes color at runtime). Art goes beside or under it, never replacing it.
  3. Create or update the background layers under each `RealityRoot/Background` on `<R>_Background` / `<R>_Middle` with `ParallaxLayer` at the plan's depths. Put the FG accent on `<R>_Foreground`.
  4. Any slot with no file keeps its greybox. Log one info line per missing slot.
  4b. PAX-V01 labels are never disabled, re-parented or recolored. They must sort above all environment art (higher order within `<R>_Gameplay`, or above `<R>_Foreground`). The FG accent must not cover them.
  5. Print a summary: slots filled / missing per reality.
- **Lighting:** adjust the existing per-reality `Light2D`s. A: warm global light, soft. B: dim cool global light plus 1–3 cyan point or freeform lights near the platform edges and station. Each light targets its own reality's sorting layers only. Add lights via the Setup script, not by hand.

Stage 1 is done when the manifest exists and running the Setup menu with **zero art files** leaves the scene visually unchanged (every slot missing → greybox kept).

### Stage 2 — Generate the kit (two tracks)

Each manifest row gets a **Source** column: `AutoSprite` or `Manual`.

**Track 1 — AutoSprite via MCP (Claude Code): props and puzzle objects.**
AutoSprite's asset endpoints target props, items and effects. So this track covers `OBJ_Vine`, `OBJ_Elevator`, `OBJ_Plate`, `OBJ_Gate`, `OBJ_Station`, `OBJ_Checkpoint`, and the `FG_01` accents if they're prop-like.

Setup (you, once):
- AutoSprite MCP/API access requires a paid plan with API access. Create an API key and add the AutoSprite MCP server to Claude Code following autosprite.io/claude.
- The key lives in your local Claude Code config or an environment variable. **It is never written into the repo, a ticket, a Setup script or a commit.** Claude Code checks `git diff` for `vspk_` before every commit.

Rules for Claude Code:
- One asset per manifest row. The prompt is built from the manifest row plus the matching reference image (uploaded as the reference), the reality's material words from `00_VISION.md`, transparent background, side-on view, and the target size.
- **Credit cap:** generate at most one image per slot per run. For a retry, ask first and state the reason.
- Outputs land in `Art/_Incoming/A02/` (git-ignored), **never directly** in `Art/RealityA|B/Environment`. Claude Code posts a contact sheet (one PNG grid of all incoming assets, labeled by slot). You approve per slot. Only approved files move into place with their manifest names.
- Record per slot in the manifest: source, prompt used, reference used, and date. That's enough to regenerate consistently later.

**Track 2 — Manual (you, with ChatGPT): tileables and backgrounds.**
Seamless tiles and wide parallax strips aren't what AutoSprite is built for. Keep `GAME_Platform_Top`, `GAME_Platform_Fill`, `GAME_Wall`, `BG_00_Sky`, `BG_01_Far` and `MG_01_Mid` manual. If Claude Code's single test generation for one tile through AutoSprite comes out seamless and on-style, you may switch those rows to `AutoSprite`. Otherwise stay manual.

1. One prompt per slot, with the reference image attached. **Generate individual assets, never screenshots of a finished level** (plan §18.4).
2. Tileables: seamless horizontal, transparent background. Check the seam by placing two copies side by side.
3. Clean up: trim, set the canvas size from the manifest, set the pivot.

**Readability checks for both tracks** (view at roughly 25% zoom):
- Gameplay surfaces clearly separate from background. Background layers have lower contrast and saturation.
- The cat stays readable against every surface in both realities.
- Puzzle objects are the highest-contrast things on screen after the cat.
- Anchor-linked objects are recognisable as a pair by their shared motif.
- V01 state labels stay readable against the new art.

Partial sets are fine: run Stage 3 as often as you like.

### Stage 3 — Claude Code: wire, tune, verify

1. Run `PARALLAX/Setup/Environment Art`.
2. Fix import or pivot issues surfaced by the summary.
3. Tune only presentation values: parallax depths, light intensities and colors, sorting order within layers, tile sizes.
4. Update the manifest's status column (✅ in, ⏳ missing).

## YOU — UNITY EDITOR

- Stage 2: AutoSprite key setup (once), approve the contact sheet per slot, make the Manual-track assets.
- After each Setup run: play both realities and eyeball the readability checks.
- Commit art through Git LFS. Confirm `.png` under `Art/` is LFS-tracked before the first commit.

## DO NOT

- Do not change, resize, add or remove any collider, trigger, layer mask or physics setting.
- Do not change gameplay, anchor, Echo, transport or input code. Allowed code is limited to the `AssetPostprocessor`, `ParallaxLayer`, and the Setup/Editor scripts.
- Do not replace or recolor renderers that gameplay drives (station glow, etc.).
- Do not add animation to environment objects beyond what manifestations already do. No sprite swaps for pressed/open states in v1.
- Do not add audio, VFX systems, particle systems, post-processing or new UI.
- Do not touch the cat (A01) beyond sorting-layer order if it gets hidden behind art.
- Do not hand-edit scene YAML. All wiring goes through the Setup menu.
- Do not commit the AutoSprite API key anywhere, and do not move unapproved generated assets out of `Art/_Incoming/`.
- Do not generate assets for later mechanics (shadow bridge, scale, world fold, etc.).

## REQUIREMENTS

- Every collider-backed object has a visual-only art child. Greybox renderers are disabled where art exists and remain where it doesn't.
- Art is sized from collider bounds and re-running Setup after resizing a collider re-fits it.
- Moving objects (elevator, gate) carry their art with them with no lag or offset.
- Parallax works independently per reality with no drift from the B offset, and background strips never show an edge.
- Warm light never reaches Reality B sprites, and cyan light never reaches Reality A sprites.
- Setup is idempotent: a second run creates no duplicates.
- PAX-V01 labels stay enabled, on top and readable everywhere.
- `ParallaxLayer` does no work for a reality whose camera is disabled.
- All EditMode tests still green. No new console warnings or errors apart from the "missing slot" info lines.

## ACCEPTANCE TEST — Editor only

1. **Tests:** Test Runner → all green, same count as before plus any new ones for pure parallax math (if extracted).
2. **Empty run:** with no art files, run Setup → scene unchanged, summary shows all slots missing.
3. **Idempotency:** run Setup twice with art present → no duplicate children or lights.
4. **Collision untouched:** `git diff` shows no changes to any collider component or physics settings. Walk, jump and fall across every platform in both realities, with behavior identical to before.
5. **Art fits geometry:** temporarily widen one platform collider, rerun Setup → art re-fits → undo.
6. **Moving art:** pull the vine → the elevator's art rises with it. Trigger the plate → the gate's art moves with the gate.
7. **Station glow intact:** sit at the station and turn the dial → glow and pulse still visible over or around the station art.
8. **Parallax:** walk the full width of each reality → layers move at visibly different speeds with no gaps. Switch A↔B several times → no jumps or drift.
9. **Light separation:** in A, no cyan tint on anything. In B, no warm tint on anything.
10. **Readability:** take one screenshot per reality at the Game view's phone resolution. With V01 labels hidden for the screenshot only, the cat, platforms and each puzzle object must be identifiable (what it is; state is V01's job), and every anchor-linked pair must be recognisable by its motif. With labels shown, every V01 label is visible and on top.
11. **Echo:** record and replay an Echo → the Echo cat (A01 `echoAlpha`) is still distinguishable from background and live cat (if not, tune `echoAlpha` on the A01 config or note it, don't change code here).
11b. **Inactive reality:** with Observer A active and PiP off, Profiler shows no `ParallaxLayer` work for Reality B. Turn PiP on → B's layers are correct on the first frame.
12. **Deferred to the device session:** atlas FPS check on the Pixel 8a (plan pipeline DoD). Log it in the deferred-verification list.

## DELIVERABLE (from Claude Code)

- Stage 1: inventory, `A02_asset_manifest.md`, pipeline scripts, Setup menu, empty-run summary.
- Stage 3: filled/missing slot summary, tuned values (parallax depths, light settings), screenshots per reality.
- Changed files with a one-line purpose each, and an explicit statement that no collider, physics or gameplay file changed.
- Known limitations: missing slots, readability issues, anything needing A01 changes.
- Review file per stage: `{ git status; git --no-pager diff; cat <new files>; } > ~/Desktop/paxa02_stageN_review.txt 2>&1`

## DEFINITION OF DONE

- Universal DoD (§8).
- Acceptance 1–11 pass in the Editor. Step 12 is logged as deferred.
- At least the geometry, background and all existing puzzle-object slots filled for **both** realities. FG accents are optional.
- Manifest status column up to date.
- `02_ARCHITECTURE.md` gets a short "As built (PAX-A02)" note covering the art-follows-colliders Setup, filename convention and `ParallaxLayer`.
- Reviewed via the review file, then committed. Art goes through LFS. Suggested commits: `PAX-A02 stage 1: env art pipeline + manifest` and `PAX-A02 stage 3: env kit A/B wired`.
