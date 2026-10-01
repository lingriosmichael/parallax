# PAX-A15 · Environment art for levels 1–10, through the level builder

**Status:** Done on a batch-mode clone 2026-10-01; not committed. Waiting for the developer's Editor steps (§12) and review (results §17, tests §18).
**Folder:** `Docs/A_TASKS/` · **Phase:** E, look and feel · **Implementer:** Claude Code
**Phase 1 size:** Lite. The builder change is presentation only: no collider, trigger or timing change.
**Depends on:**
- The environment stills in `Art_Source/Environment/A/`: ENV-01…30 (committed in 5fd7ebd), plus the seven in §10.
- PAX-A13: the host-skin seam (`HostSkin`, `SolidArt`, `Sprite-Lit-WorldTile`), so disguised traps copy their host's
  new skin automatically.
- `EnvironmentSpriteImporter` (the fixed environment PPU, PAX-A08 §4.4).
**Decisions:** D-040, D-047 (Reality A only), D-058 (kill bounds), D-067 (no randomness), D-070/D-072 (levels are built
by `Rebuild All Levels` from `_LevelTemplate`), D-083 (reveals in view), D-094 (interim budgets until PAX-V03).
**Relation to other tickets (ruled):**
- **PAX-A12:** A15 takes over A12's scope for L001–L010 and the shared kit. A12 shrinks to the polish of L011–L020 and
  the Trap Lab.
- **PAX-V03:** A15 runs without it, on the interim budget in §5.
- **Seamless climb vine:** formerly PAX-A08 item 15, since given its own ticket. It is folded in here (§2.6).

**Developer rulings (survey request, 2026-09-30):**
- Everything goes in through the level builder (`PARALLAX/Setup/…` menus, `Rebuild All Levels`). Never hand-edit a scene.
- Environment PPU stays fixed at **196.667**, and at **98.333** for backgrounds and materials. Art is resampled to each
  slot's exact pixel size before import; the importer PPU never changes.
- Measure and report **draw calls and texture memory per level**.
- Acceptance is **before/after captures at phone scale** for the developer's review.
- Lean: **no critic or reviewer rounds** (this overrides the default `pax-reviewer` loop for this ticket). During the
  work, run only the fixtures the change touches; the full batch suite runs once at the end.

**Developer rulings (2026-10-01):**
1. **PAX-A12 and PAX-V03:** A15 takes A12's work for L001–L010 and runs without V03, on the interim budget.
2. **L011–L020:**
   - They get the new art with a default look.
   - They're in the after-captures, with a readability check only.
   - The full polish pass is L001–L010.
3. **Budget accepted:** at most 80 batches, 30 SetPass calls and 32 MB of environment textures on Android (§5).
4. **Background scale:**
   - The far background layers are scaled up to 1.5×.
   - Mid layers aren't scaled. Any mid layer that looks soft in the captures is flagged in the acceptance report.
5. **Door and checkpoint** switch to ENV-24/25. The door stays the most visible element in every level (§3, A6).
6. **Readability threshold:** set from the before/after measurements, with a hard rule that no level reads worse than
   its "before" capture (§3, A1).
- **Acceptance requirements, not notes (§13):**
  - background separation;
  - no walkable-looking background at walking height;
  - contrast for the gravity-flip rings;
  - the stray foreground piece removed;
  - the per-level time-of-day plan.
- **Doc fixes, as part of A15:**
  - the A02 manifest's status column;
  - both vine notes (PAX-V07b's "the seamless vine follows it" and PAX-A08's "its own ticket").

## 1. Why

The survey captures (Play Mode, level camera, 2400 × 1080) show every level on the same A02 v1 background strips. Every
wall, floor, ceiling and disguised trap is one flat tan (`#B88547`) rectangle:
- **The backgrounds don't cover the view.** The level camera shows 14.85–16 u vertically, but the A02 strips are
  11–13.8 u tall, sized for the old sandbox camera (`maxAbsCamY` 4.7). The result is stacked bands, a smeared
  clamped-edge stripe near the top, and a hard horizontal cut where the mid strip ends.
- **`FG_01`** (hanging roots and banner) floats detached at the top-left of every level.
- **Ten levels, one look:** the same arch strip, sky and grade in all ten.
- **No depth cues:** no haze, fog, water, light shafts, normal maps or foreground framing.

Levels 1–10 are the free hook. They must look like `DesignImages/15_premium-mobile-ui.png` and the final result in
`14_environment-breakdown.png`, and still read instantly.

**The "before" set:** the survey captures in `~/Parallax_batch_tools/env_before/` (`L001_play.png` … `L010_play.png`,
plus the whole-frame shots). The capture script `ScratchPlayCapture.cs` is kept beside them.
- The L011–L020 "before" captures are taken at the start of the work, with the same script, before any change.
- The survey report is https://claude.ai/artifact/8D8gLPdYH2k7bLft3qG1NW.

## 2. Scope

### 2.1 Kit processing (offline, Python)

`Tools/Art/env_kit.py`, a new tool on the pattern of `trap_process.py`. It is deterministic and reads
`Art_Source/Environment/A/` without changing it.
- **Crops** the multi-piece sheets into one file per piece:
  - 10c (2), 10d (4), 14b (2), 18 (3), 20 (3), 21 (3), 22 (2), 23 (4), 24 (2), 25 (2), 07a (2), 07b (2), 04 (4), 05 (4)
  - 07d (2) and 26 (3), once they're in.
- **Resamples** every piece to its slot's exact target pixel size (the slots are new rows in `A02_asset_manifest.md`).
- **Makes tiles seamless:**
  - Tiles with a measurable seam get an offset-and-crossfade on their tiling axis. Measured seam ratios (edge
    difference ÷ mean neighbour difference; 1 is seamless):
    - 10a: 2.1 / 1.8; 10a2: 1.9 / 2.8
    - 10b: 2.7; 10c: 2.0 / 2.6; 12: 2.7; 14a: 3.3; 18 (middle): 4.2; 30: 3.2
  - Sky, cloud, haze and fog bands (ENV-01 23.8, 03a 30.4, 06 6.3, 27 24.3) get a wide crossfade; soft art tolerates it.
  - The new tiling bands (03b, 03c, 07c, 32) and the fill 10a3 are measured the same way and blended where needed.
  - ENV-08 (waterfall, 13.4 vertical) gets the same. If the blend shows when scrolled, the developer regenerates it
    (§10, conditional item ENV-08b).
- **Converts the on-black effects to alpha** (luma to alpha, colour unpremultiplied): ENV-02 sun, 06 haze, 09 mist,
  27 fog, 28 light shaft. No new additive shader.
- **Foreground blur:** ENV-26 and 26b pieces are pre-blurred.
- **Normal maps** for the play-layer pieces, with the existing `trap_process.normal_map`.
- Writes to `Assets/_Game/Art/RealityA/Environment/Kit/` (sprites) and `…/Environment/Backgrounds/Kit/` (layers).
  It never overwrites an existing Unity asset; the A02 files stay until a later cleanup.

### 2.2 Importer

`EnvironmentSpriteImporter` learns the `Kit/` folders and the ENV slot classes:
- Materials (world-tiled fills) at 98.333.
- Edges, caps, ends and objects at 196.667.
- Backgrounds at 98.333, ASTC 6×6 on Android.
- Repeat wrap for tiles only.

The PPU constants don't change.

### 2.3 The background stack, on `_LevelTemplate` (a new setup menu)

`PARALLAX/Setup/Levels/Environment Stack` is idempotent and runs on `_LevelTemplate` only. It replaces the A02
strips under `RealityRoot_A/Background` and `/Foreground`. Reality B's objects are untouched. The stray `FG_01`
(roots and banner) is removed, not repositioned.

| Layer | Content |
|---|---|
| 01 Sky | A vertical gradient quad made in code (no texture), tinted per level, plus the ENV-02 sun. |
| 02 Clouds | ENV-03c far, 03b mid and 03a near bands. |
| 03 Far | ENV-04 islands and ENV-05 spires, as separate pieces. **Scaled 1.5×** (ruling 4). |
| 04 Haze | ENV-06. |
| 05 Mid | 07a arches, 07c aqueduct span, 07d colonnade and towers, 07b trees, 15 broken pillars, 08/09 waterfall and mist, and 32 back wall behind enclosed sections. **Not scaled.** |
| 06 Atmosphere | 27 low fog and 28 light shafts, behind the play layer only. |
| 07 Foreground | 26 roots and leaves and 26b trunk, only outside the room's kill bounds (D-058) and at the frame edges. |

`Rebuild All Levels` then lays each level out from its recipe (§4): which pieces go where, the tint, and the sun
position. The layer placement reuses `ParallaxLayer` and `LevelCameraBuilder.SetBackgroundVerticalPosition`.

- **Coverage rule (tested):** every background layer covers the level camera's whole view through the level's full
  camera travel, at 4:3, 16:9 and 20:9.
- **Coverage method:**
  - Bands tile horizontally, and the sky gradient scales freely.
  - Far layers are scaled at most 1.5×.
  - Mid layers aren't scaled. If a mid layer can't cover without scaling, that's a stop.
- **Separation (A1 in §3):**
  - Every layer behind the play layer carries the level's haze tint and a value shift away from the sandstone
    platforms.
  - The play layer keeps its full contrast.

### 2.4 The play layer, in the builder

`SoloRoomBuilder.BuildGeometry` (and the fake-platform, collapsing-floor and disguised-launcher hosts, through
`HostSkin`) draws each solid from the kit instead of the flat `Greybox`/`Square` sprite:
- **Fill:** 10a, 10a2 and 10a3, chosen per 4 u world cell from a hash of the level id and the cell. This is
  deterministic, with no `Random`. The fill is world-tiled with the `Sprite-Lit-WorldTile` material, so adjacent solids
  (and a trap beside its host) continue each other's masonry.
- **Edges and caps:**
  - 10b mossy top cap on every exposed top.
  - 10c underside on every exposed bottom, so a ceiling reads as a walkable surface for a flipped cat.
  - 10d ends and corners.
  - 12 wall faces.
- **Thin shapes:**
  - 0.5 u thin shapes (27 floors, 19 walls, 9 collapsing floors in L001–L010) use the ENV-11 slab (cap and underside
    only).
  - 1 u slabs and posts use 14a/14b where they're narrower than they are tall.
- **Dressing (04b) by rule, never hand-placed:**
  - Pieces: 21 moss caps, 20 drapes under overhangs, 22 banners on tall walls, 23 rubble and glyph panels.
  - The rule is a function of the surface only (its size and exposure), so a trap surface is dressed exactly like a
    real one (P10).
  - Dressing is never placed within 0.5 u of a hazard, a checkpoint, a trigger region or an arrow lane, and never
    within 1 u of the door.
- **Pit bottoms:** ENV-30 water surface over a dark water body (code), under the existing spike art.
- **Colliders:** no collider, trigger, sorting-layer or timing change. Every layout pin stays identical.

### 2.5 Door and checkpoint (ruling 5)

- **Door:** ENV-24 (closed/open) replaces `A_Door_Exit` on the level door. It keeps its footprint, and the open state
  shows on the completion tick.
- **Checkpoint:** ENV-25 (idle/crossed) replaces the section-gate marker (`A_OBJ_Checkpoint`).
- **Glow:** the door has a warm glow behind it. The glow layer (LOOK_AND_FEEL layer 05) is reserved for the door, a
  crossed checkpoint and trap tells; no dressing or background piece glows.
- **Visibility is measured (A6):** the door's local contrast ranks first among all environment elements in every level's
  capture.

### 2.6 The seamless climb vine (folded in from PAX-A08's old item 15)

ENV-18 covers what item 15 asked for: a top anchor, a tiling middle and a tip.
- `TrapKitSetup.BuildClimbVineCore` draws them as anchor + Tiled middle + tip, instead of the stacked `A_OBJ_Vine`
  segments.
- The grab box (0.6 wide, D-089) doesn't change.
- Vines exist only in L011+ (D-089): L014, L016, L018, L020 and Trap Lab room 9. This part is accepted on those
  captures.
- ENV-20 drapes stay thin and are never placed where a vine could be, so dressing never reads as climbable (PAX-A13
  :77).
- **Doc fixes:**
  - `PAX-V07b.md`'s "the seamless vine follows it" and `PAX-A08.md`'s "gets its own ticket" (header and out-of-scope)
    both point to A15 §2.6.

### 2.7 Lighting and the gravity-flip rings

- **Lights:**
  - The template's one Global Light2D stays, with its colour and intensity set per level from the recipe.
  - The play layer's normal maps light from the existing lights.
  - No new point lights. Off-screen Editor renders skip 2D point lights, so point-light shading is checked in the Scene
    view (`Trap Flip Light Check`), not in captures.
  - No Volume, bloom or colour grade. Those stay with PAX-V03.
- **Gravity-flip rings (A3):** each ring gets a contrast backing drawn by the builder: a dark outline, or a soft glow on
  the glow layer. The choice is made in Phase 1 from a test capture.
  - The ring's own art file (`TRAP-06`/`TRAP-08` bodies) isn't changed.
  - Its timing, trigger and footprint don't change.
  - Measured in L004, L009 and L010 against the gold sky at every grade.

## 3. Readability (a rage game: acceptance requirements)

Each item is a test or a measured number in the acceptance report. A1–A6 are the developer's acceptance
requirements (2026-10-01).

- **A1 · Background separation (measured, per level):**
  - **The step:** the mean luminance step between each walkable top (the 10b cap band) and the 0.5 u of background
    behind and above it.
  - **Background contrast:** each background layer's contrast (luminance standard deviation) stays below the play
    layer's.
  - **Threshold:**
    - The threshold is set from the before/after measurements and written into this ticket before acceptance.
    - **Hard rule:** no level's per-level minimum step is lower in its after capture than in its "before" capture.
    - Applies to L001–L020; L011–L020 are held to the rule only (ruling 2).
- **A2 · No walkable-looking background (tested from the recipe, confirmed in captures):**
  - No background or mid-layer arch top, ledge, lintel or pillar cap sits within 0.3 u of a real walkable top line at the
    same screen position, through the camera's travel.
  - Fixed in L001 and L006, where it happens today, and anywhere else the check finds it.
- **A3 · Gravity-flip rings** get an outline or glow (§2.7). Their luminance step against what's behind them is measured
  in L004, L009 and L010, and is higher after than before.
- **A4 · The stray foreground piece is gone.** No `FG_01` renderer remains in any level (test).
- **A5 · Time-of-day plan (§4):** every level's recipe matches the approved grade table (test on `LevelLookConfig`).
- **A6 · The door is the most visible element:**
  - In every level's after capture, the door's local contrast (door against the 1 u ring around it) ranks first among
    environment elements: dressing, background pieces, checkpoint.
  - Nothing is dressed within 1 u of it (test).
- **Other checks:**
  - **Foreground** never overlaps the room's kill bounds (test).
  - **Dressing** never overlaps hazards, checkpoints, triggers or arrow lanes (test), and never adds a collider (test).
  - **P10 no-tells:** a disguised trap's pre-reveal frame is pixel-identical to its host with the new skin
    (`TrapArtRevealFrameTests` and `TrapArtParityTests`, extended to the new skins).
  - **Honest hazards** (spike strips, arrow tells) keep a luminance step at least as high as before, measured like A1.
  - **Reveals** stay in view ≥ 6 ticks before they can kill (D-083). Unchanged, because no layout changes.

## 4. Variety: the time-of-day plan (approved 2026-10-01)

Each level gets a recipe in `LevelLookConfig` (a new ScriptableObject under `Assets/_Game/Data`, read by the builder).
A recipe sets:
- a grade (sky gradient, sun position, global light, haze and fog tint, with the play-layer tint limited so A1 holds);
- a skyline (which far and mid pieces go where);
- the stone mix;
- the level's one signature element.

Band 1 plays as one day passing.

| Level | Grade | Skyline and signature | Concept target |
|---|---|---|---|
| L001 | Morning gold | Great arch framing the sun; far islands | 15 |
| L002 | Morning gold | Aqueduct span (07c); water in the pits | 14 (final result) |
| L003 | Noon gold | Waterfall and mist in the mid layer; trees | 03 (A side) |
| L004 | Warm afternoon | Enclosed hall: back wall (32), drapes under the roofs | 04 (A side) |
| L005 | Afternoon | Banner hall: banners and glyph panels along the arrow walls; colonnade (07d) | 05 (A side) |
| L006 | Late afternoon | Aqueduct span, trees and broken pillars; low fog | 02 (A side) |
| L007 | Golden hour | Collapsed ruins: rubble, cracked fill (10a2) dominant | 10 (panel 1) |
| L008 | Golden hour | Back wall with light shafts; towers (07d) | 07 (A side) |
| L009 | Dusk amber | Low sun, long haze, spire silhouettes, aqueduct | 09 (A side) |
| L010 | Dusk rose | Back wall, water, fog, banners: band 1's finale | 06 (A side) |

L011–L020 share one default recipe (afternoon grade, generic skyline, no signature element). Their polish stays with
PAX-A12.

## 5. Budget and measurement (interim, D-094; accepted 2026-10-01)

**Limits, per level, at the capture frame:**
- ≤ 80 batches.
- ≤ 30 SetPass calls.
- Environment textures ≤ 32 MB on Android.
- The play kit in one 2048² atlas; background layers ≤ 2048 wide.

**Measured before and after, per level, in Play Mode at the phone view (2400 × 1080):**
- `ProfilerRecorder` render counters: Batches, Draw Calls and SetPass Calls. Sampled at the capture frame and as the
  max over the solution replay's first 300 frames.
- Texture memory:
  - Primary: computed from the Android import format of every texture the scene references (exact for ASTC and
    uncompressed).
  - Cross-check: Editor runtime size.
  - Environment textures are listed separately from cat, trap and UI textures.

**Scope of the limits:**
- Applies to L001–L020.
- **Reality B's textures are reported separately and not counted.** Every level scene also loads Reality B's A02
  backgrounds and atlas, inherited from `Level_Solo01` through the template. Removing them touches frozen Reality B
  objects and is a separate ruling.

## 6. Files (allowed list)

**New:**
- `Tools/Art/env_kit.py` and `Tools/Art/tests/test_env_kit.py`.
- `Assets/_Game/Art/RealityA/Environment/Kit/*.png` and `…/Backgrounds/Kit/*.png`, written by the tool. Unity creates
  their `.meta` files on import; none are hand-edited.
- `Assets/_Game/Editor/Setup/EnvironmentStackSetup.cs` (the §2.3 menu) and `Assets/_Game/Editor/Setup/SoloRoomSkin.cs`
  (the §2.4 skin and dressing rule, called from `SoloRoomBuilder`).
- `Assets/_Game/Editor/Levels/LevelLook.cs` (the recipe type) and `Assets/_Game/Data/LevelLookConfig.asset` (created
  by the menu).
- `Assets/_Game/Editor/Art/EnvironmentCapture.cs`: the capture and measurement menu, a committed version of the survey
  script. It covers captures, A1/A3/A6 measurements and budget counters, and runs in batch mode on a clone.
- Tests: `EnvironmentKitImportTests`, `EnvironmentStackCoverageTests`, `EnvironmentReadabilityTests`,
  `LevelLookConfigTests`.

**Changed:**
- `Assets/_Game/Editor/Art/EnvironmentSpriteImporter.cs`
- `Assets/_Game/Editor/Setup/SoloRoomBuilder.cs`: only the calls into `SoloRoomSkin`.
- `Assets/_Game/Editor/Setup/RoomSetup.cs`: the door art path and glow (§2.5).
- `Assets/_Game/Editor/Setup/TrapKitSetup.Climb.cs` (§2.6)
- `Assets/_Game/Editor/Setup/TrapKitSetup.cs`: the flip-ring backing (§2.7).
- `Assets/_Game/Editor/Setup/LevelCameraBuilder.cs`: background placement for the new layers.
- `Assets/_Game/Editor/Art/TrapArtSetup*.cs`: only if the host lookup needs the new renderer names.
- `TrapArtRevealFrameTests.cs` and `TrapArtParityTests.cs` (extended).
- **Scenes, regenerated by menus, never hand-edited:**
  - `_LevelTemplate.unity` (by the §2.3 menu), then `Level_001`–`Level_020` (by `Rebuild All Levels`).
  - `Sandbox_TrapLab` only if the vine change requires its setup to run.
- **Docs:**
  - `Docs/Art/A02_asset_manifest.md`: the ENV slot rows, and the stale ⏳ statuses corrected.
  - `Docs/Art/NEEDED_ASSETS.md`
  - `Docs/A_TASKS/PAX-A12.md`: scope note.
  - `Docs/A_TASKS/PAX-A08.md` and `Docs/V_TASKS/PAX-V07b.md`: the vine notes.
  - this ticket

`SetupUtility.SetVisual` is shared with frozen co-op setup and is **not** changed; the skin is applied after it. Trap
art files (`Assets/_Game/Art/Traps/`) are not changed.

## 7. Out of scope

- Reality B art.
- Co-op or frozen code.
- PAX-V03's Volume, bloom and post.
- New point lights.
- The cat, trap body art and UI/HUD.
- Any layout, route, collider or timing change.
- L011–L020 polish beyond the default recipe.
- The Trap Lab's look.
- The trap PPU: 128, unchanged (see §14).
- AutoSprite: no credits.
- Device validation (developer only).

## 8. Stop conditions

- Any need to change a collider, trigger, sorting layer or tick timing.
- A P10 parity failure the art can't fix.
- A level that reads worse than its "before" (A1 hard rule) with no art fix inside the kit.
- The door not ranking first (A6) with no fix inside the kit.
- A mid layer that can't cover the view unscaled.
- A far layer that needs more than 1.5×.
- The budget exceeded with no in-scope fix.
- Any conflict with the docs.

## 9. Tests and runs

**During the work (touched fixtures only):**
- `EnvironmentKitImportTests`, `EnvironmentStackCoverageTests`, `EnvironmentReadabilityTests`, `LevelLookConfigTests`
- `TrapArtRevealFrameTests`, `TrapArtParityTests`
- `LevelSceneTests`, `SavedSceneSyncTests`
- `CatSheetImportTests` (it pins the environment PPU)
- `pytest Tools/Art/tests`

**Not during the work:** the slow level fixtures (`Band2RouteResultsTests`, `Band2LevelTests`, `ValidateRoutesTests`),
since no layout, route or timing changes. They run in the final full batch suite, once, before the stop for review.

## 10. Assets

**Being made by the developer (2026-10-01), the start condition:**
- ENV-03b (mid cloud band), ENV-03c (far cloud band)
- ENV-10a3 (mossy stone fill)
- ENV-26 (foreground roots and leaves)
- ENV-07c (aqueduct span), ENV-07d (colonnade and towers)
- ENV-32 (back wall)

The prompts are in the survey report. On arrival, each one is checked before processing: size, alpha, seam ratio on its
tiling axis, strict side view, and block size matching 10a for the fill. A failed check is reported to the developer, not
worked around.

**Conditional (only if processing fails):**
- ENV-08b (seamless waterfall)
- ENV-14c (slim post)

## 11. Rulings

All six survey questions were ruled on 2026-10-01 (see the header). No questions are open.

## 12. YOU: Unity Editor

1. After Claude Code's work compiles:
   - Open `_LevelTemplate` and run `PARALLAX/Setup/Levels/Environment Stack`. Save (Cmd+S).
   - Run `PARALLAX/Setup/Levels/Rebuild All Levels`.
   - Run the trap-art setup that PAX-A13's handover names (it re-skins the disguised traps from their new hosts).
2. `git status` should list `_LevelTemplate.unity` and the rebuilt `Level_NNN.unity` files as modified.
   `Sandbox_Realities.unity` should not change.
3. Device (optional, yours): play L001, L005 and L010 on the Pixel 8a, and check readability, the door and frame rate.

## 13. Acceptance

**Captures:**
- **L001–L010:** before/after captures at 2400 × 1080, each beside its concept target. Level camera, Play Mode, batch
  mode on a clone.
- **L011–L020:** before/after captures, with the readability check only (ruling 2).
- **L014 and Trap Lab room 9:** the vine.
- **Coverage:** a 16:9 and a 4:3 coverage capture per level.
- **Soft mid layers:** any mid layer that looks soft at phone scale is flagged, per level (ruling 4).

**Readability:**
- **A1:** the per-level measurements, before and after, with the threshold set from them. No level reads worse than
  its before.
- **A2:** no walkable-looking background at walking height. L001 and L006 fixed, the rest checked, with the check's
  output per level.
- **A3:** the flip rings in L004, L009 and L010 have an outline or glow, and a higher luminance step than before.
- **A4:** no `FG_01` renderer in any level.
- **A5:** every level's recipe matches the §4 table.
- **A6:** the door ranks first in local contrast in every level, and nothing is dressed within 1 u of it.
- **Other §3 checks:** P10 parity; foreground and dressing tests green; honest hazards no worse than before.

**Budget:** the §5 table, before and after, for all twenty levels, within the limits.

**Doc fixes:** the A02 manifest statuses, and both vine notes pointing to A15.

**Pins and tests:**
- Every layout pin and collider unchanged (existing tests green).
- The full batch suite once at the end, reporting total, passed and failures by name.
- Zero new console warnings beyond CLAUDE.md's known noise.

**Device:** look and feel unverified unless the developer runs the device step.

## 14. Trap PPU (checked 2026-10-01; no trap art changed)

PAX-A13 R1 (line 175) says "Traps use 128 px per unit, the play layer's scale".
- **The 128 is real.** All 41 trap sprites in `Assets/_Game/Art/Traps/Bodies/` and `Effects/` import at
  `spritePixelsToUnits: 128`. `trap_kit.json` has `"ppu": 128`, and `TrapKitImport` applies it.
- **"The play layer's scale" is wrong.** The play layer imports at 196.667 (A02, `EnvironmentSpriteImporter`,
  PAX-A08 §4.4), and the cat at 143.3.
- **Effect:**
  - None on size or placement: each trap's world size comes from the builder (draw size and scale), and disguised skins
    copy the host's own sprite and material.
  - The only difference is texel density. Trap art carries 128 texture pixels per unit against the environment's
    196.667. Both are above what a phone shows: about 68 screen pixels per unit at 1080p and about 91 at 1440p.
- **Proposed:** a one-line wording fix in PAX-A13 R1, left for the developer to approve. Not part of A15 unless ruled.

## 15. Phase 1 (Lite), 2026-10-01

**Started:** the developer said the images were in. Nine arrived: the seven, plus the conditional ENV-08b and ENV-14c.
Unity MCP is not connected, so the work runs as PAX-A14 did: Python offline, and Unity in batch mode on an APFS clone
for compiling, menus, captures and tests. The developer runs the setup menus in the main Editor (§12).

**Image checks:** all nine pass with processing.

| Image | Size and alpha | Seam ratio | Processing |
|---|---|---|---|
| 03b | 1536², alpha | x 8.5 | crossfade |
| 03c | 1536², alpha | x 13.1 | crossfade |
| 07c | alpha | x 5.4 | cropped to whole arch periods so it tiles |
| 07d | 2 pieces | — | none |
| 10a3 | 1254² | 2.4 / 2.4 | blended like 10a; block size matches 10a by eye |
| 26 | 3 pieces | — | split by rows |
| 32 | alpha | x 2.1 | **darkened:** it came out as bright as the platform stone, not darker as the prompt asked; the background darkening (A1) handles it, no regeneration |
| 08b | alpha | y 4.4 | crossfade |
| 14c | alpha | y 2.7 | crossfade |

**Design found in Phase 1 (changes §2.3/§2.4 placement, not scope):**
- **The play-layer skin goes inside the room build.** `SavedSceneSyncTests` compares each saved level room
  (`Rooms_PAX043/Room_1`) with a fresh `SoloRoomBuilder.BuildRoom` of its layout. A skin added by a later level-only pass
  would make every level differ.
  - So the fill reskin, trims (caps, undersides, ends, wall faces), dressing, door art, checkpoint marker and flip-ring
    backing run inside `BuildRoomCore`'s art step (`buildArt: true`). That step is gated, like `TrapArtConfig`, by
    `LevelLookConfig` existing.
  - The play layer is deterministic from world cells and element names (no level id needed).
  - **Side effect:** the Trap Lab also gets the kit the next time its setup menu runs. It must run anyway for the vine,
    and to keep its sync test green.
- **Order inside the art step:**
  1. Reskin the fixed geometry's renderers with the world-tiled fill.
  2. `TrapArtSetup.BuildArt`, so disguised traps copy the new host skin (P10, unchanged code).
  3. The trims and dressing over the combined silhouette of real and disguised solids. A trap's trims are parented
     under its art skin, so they move with it.
- **Per-level layers** (background stack, grade, global light tint) live outside the room, under
  `RealityRoot_A/Environment`. They're built by the level rebuild from `LevelLookConfig`, after the room.

**Open questions (allowed list and one design choice):** see the developer's answers below.

**Developer's Phase 1 answers (2026-10-01):**
1. **Allowed list:** add `Assets/_Game/Editor/Setup/LevelSetup.cs` (one call to the per-level environment pass) and the
   new `Assets/_Game/Presentation/ShownWith.cs` (renderers that show, hide and move with an owner renderer: trims on
   disguised traps, the flip-ring halo). The flip-ring backing goes in `TrapArtSetup.Kit.cs` (`BuildFlip`), not
   `TrapKitSetup.cs`.
2. **Door:** ENV-24's lit (open) frame, always. No runtime open state.
3. **Trap Lab:** gets the kit on its next setup run (no polish).

**Before captures:** L011–L020 taken 2026-10-01 with the survey script, in `~/Parallax_batch_tools/env_before/`.

## 16. Phase 2 (work log, 2026-10-01)

**Developer approvals during the work (each asked when the need appeared):**
1. `Assets/_Game/Presentation/WorldTileSampling.cs`, new. A MaterialPropertyBlock isn't saved with a scene, so the world tiling a
   renderer needs is stored on it and reapplied when it's enabled (edit mode too). It's now used only by a disguised trap's own
   trims.
2. A fix for the P10 tell found in the first play capture: L001's fake platforms showed a 1 u stone pattern beside the real
   4 u masonry. Cause: A13's `TrapArtSetup.WriteSkin` saved a host skin without its world-tiling fields (`WorldTiled`, `Tile`,
   `UVRect`), so after a scene load the presenter never resampled. It was latent while every host was flat tan.
   - Fixed in `TrapArtSetup.cs` (3 lines).
   - `Assets/_Game/Art/Shaders/EnvSpriteLitWorldUV.shader`, new: A13's world-tile shader with the UV taken from the world
     position, so renderers need no per-renderer block and share materials.
   - One material per tile kind, created by the setup menu under `…/Environment/Kit/Materials/`.
   - The geometry's own renderer stays the (hidden) host the trap art copies; a `Fill` child draws the visible stone.
   - A sprite atlas, `…/Environment/Kit/Atlas_EnvKit.spriteatlasv2`, created by the setup menu, for dressing, door, checkpoint,
     glow, halo, post ends and the vine.

**Files added to §6 by these rulings:**
- `LevelSetup.cs`, `ShownWith.cs`, `WorldTileSampling.cs`, `EnvSpriteLitWorldUV.shader` (with its materials and the atlas, made
  by the menu).
- `TrapArtSetup.cs` (`WriteSkin`) and `TrapArtSetup.Kit.cs` (`BuildFlip`).
- `EnvironmentKit.cs`.
- The recipe files are named `LevelLookConfig.cs` (the ScriptableObject) and `LevelLooks.cs` (the recipes), not `LevelLook.cs`.

**Rules as implemented (deviations from the draft, each for a stated reason):**
- **Foreground (§2.3, §3):** "only outside the room's kill bounds" can't be met. The kill bounds are the content plus 2 u, and
  the camera frame is the content plus 0.5 u, so anything outside the kill bounds is never on screen. Implemented instead:
  - A foreground piece's box, over the camera's whole travel at 4:3/16:9/20:9, lies only over solids or outside the room's
    content, never over air the cat can reach. Tested.
  - Pieces that can't meet that are left out, and the build log names them.
  - Foreground speed is 1.1, not 1.25: at 1.25 the pieces slid into the room's air when the camera panned.
- **One stone fill per room**, not one per 4 u cell. A disguised trap copies one host's skin, so a fill change at a
  real/trap boundary would be a tell. The stone mix varies between levels instead (§4: A, A2 cracked, A3 mossy).
- **No drapes in rooms with gravity flips** (L004, L009, L010). Their ceilings are walkable when flipped, and drapes would hang
  into that walking space. §4 had drapes under L004's roofs; L004 gets banners and glyph panels instead.
- **The far layers scale 1.5×** (ruling 4); no mid layer, back wall or atmosphere piece is scaled. A2 is enforced by
  construction: the builder slides a flat-topped piece (aqueduct deck, colonnade lintel, arch lintel) down, then up, until it's
  0.3 u clear of every walkable top over the camera's travel, or leaves it out. The test re-checks the saved scenes.
- **Back walls sit under the haze veil** (order -48 below the veil's -45). On the first pass they were so dark that the black
  cat nearly disappeared in L004 and L008.
- **The door:** ENV-24's lit frame on a soft dark pocket (`ENV_Halo` at 0.85) with a warm glow at its opening. A glow alone
  brightened the door's surroundings and lowered its contrast.

**Found, not changed (not in scope):**
- Every level inherits the frozen sandbox's vine-elevator art (`VineZone_A/…/Art`, `A_OBJ_Vine`) from the template, at the
  geometry's sorting order. With the new fill it showed on the ground face. The visible fill now draws just in front of it
  (z -0.005), which hides it again; the object itself is untouched.
- `PARALLAX/Setup/Levels/Level Camera` (`LevelCameraSetup`, the one-time template bootstrap) still configures the A02 layers
  this ticket removes. If it is ever run again it logs "missing background layer". Not changed (not in the allowed list).
- Every level scene also loads Reality B's A02 backgrounds (6.3 MB on Android, measured), inherited from the template.
  They're reported, not counted (§5).
- **Budget semantics:**
  - The project uses the SRP Batcher with dynamic batching off (`UniversalRP.asset`). Every SpriteRenderer is its own draw
    call, cheap on state changes, so "Batches" is close to the number of renderers in view; SetPass is the state-change cost.
  - The first build drew 65–152 batches and 11–79 SetPass, measured.
  - Reductions so far: batchable materials (SetPass 11–30), one tiled renderer per cloud/haze band instead of seven tiles, no
    visible fill under slabs and posts (their trims cover them), at most 4 drapes and 2 rubble piles a room, slab ends only on
    slabs 2 u and wider.

**Developer rulings during Phase 2 (2026-10-01, continued):**
1. **A6 uses the peak measure:** the brightest tenth of each element (the lit doorway) against its 1 u surround, and the
   door must rank first among dressing, background pieces and checkpoint markers. The mean measure (the whole door box
   against its surround) is still reported. The stone frame dilutes the glow there, and small dark moss tufts and
   foreground silhouettes outrank the door.
2. **The budget is restated:** SetPass ≤ 30 and draws ("Batches") ≤ 130 per level at the capture frame. Environment
   textures stay ≤ 32 MB on Android. The device check on the Pixel 8a decides whether draws matter. Merging the fixed
   environment into one mesh per material is a follow-up ticket; it needs a `SavedSceneSyncTests` change, because that test
   compares references outside the room by instance.
3. **One A13 test is updated:** `TrapArtFireEffectTests.ValidateTrapSkins_RejectsACrumblingDisguiseOnAPatternedHostThatIsntWorldTiled`
   also gives its swapped-in host a plain sprite material. Level hosts are world-tiled now, so without it the test no
   longer builds the "patterned host that isn't world-tiled" it names. The validator is unchanged.

**Other fixes found in Phase 2:**
- **The reveal frame (A13 R8):** a collapsing floor's trims stay up on its reveal tick, while its first shard is still at
  rest, so that frame matches the one before (`TrapArtRevealFrameTests`). They go when the shards start to fall.
- **Trims on traps that never move** (collapsing floors, fake platforms, disguised launchers) use the batchable materials.
  Only falling blocks, movers and shrinking floors keep the rest-pose shader, so their trims' pixels travel with them.
- **The broken pillar's plinth** (two stepped tiers) read as stairs on L006's floor line. Its foot now sinks below the
  ground in L006 and L007, and its plinth lines are A2 flat lines.
- **Normal maps carry their sprite's PPU** (`CatSheetImportTests` checks every environment texture).
- **Collapsing-floor shards** (approved 2026-10-01): A13 drew them at the floor's order + 2, above the cat and above the
  floor's new cap and edge trims. On the reveal tick the shards, which carry only fill pixels, covered the cap, so the
  frame no longer matched the one before (R8). `TrapArtSetup.BuildCollapsingFloor` now draws the shards at the floor's
  own order: the trims stay on top for the reveal frame, and the falling shards pass behind the cat. The dust is unchanged.

## 17. Results (2026-10-01, final build; batch mode on a clone)

All measured in Play Mode on the level camera at 2400 × 1080 (`EnvironmentCapture`). "Before" is the untouched project; "after" is
the final build. Reality B's A02 textures add 6.3 MB to every level (reported, not counted).

| Level | Grade | A1 worst (before → after) | A1 median | A3 rings (before → after) | A6 door (peak / mean) | Draws | SetPass | Env MB |
|---|---|---:|---:|---|---|---:|---:|---:|
| L001 | MorningGold | 0.027 → 0.114 | 0.126 → 0.345 | — | 1 / 3 | 30 → 57 | 6 → 17 | 5.3 |
| L002 | MorningGold | 0.023 → 0.174 | 0.148 → 0.323 | — | 1 / 12 | 42 → 101 | 6 → 25 | 6.1 |
| L003 | NoonGold | 0.031 → 0.132 | 0.103 → 0.269 | — | 1 / 17 | 38 → 85 | 6 → 15 | 6.3 |
| L004 | WarmAfternoon | 0.034 → 0.152 | 0.119 → 0.171 | Flip_A 0.21→0.44, Flip_E 0.30→0.44, Flip_L 0.27→0.43 | 1 / 11 | 35 → 52 | 7 → 10 | 6.0 |
| L005 | Afternoon | 0.077 → 0.187 | 0.141 → 0.293 | — | 1 / 8 | 30 → 71 | 4 → 11 | 5.9 |
| L006 | LateAfternoon | 0.002 → 0.014 | 0.099 → 0.254 | — | 1 / 10 | 39 → 80 | 6 → 16 | 6.0 |
| L007 | GoldenHour | 0.017 → 0.069 | 0.149 → 0.202 | — | 1 / 8 | 44 → 82 | 6 → 15 | 5.7 |
| L008 | GoldenHour | 0.011 → 0.123 | 0.096 → 0.143 | — | 1 / 10 | 29 → 68 | 6 → 22 | 6.6 |
| L009 | DuskAmber | 0.008 → 0.149 | 0.142 → 0.223 | Flip_1 0.20→0.39, Flip_4 0.30→0.43, Flip_D1 0.22→0.40 | 1 / 8 | 46 → 85 | 8 → 13 | 5.8 |
| L010 | DuskRose | 0.044 → 0.096 | 0.116 → 0.162 | Flip_4 0.15→0.40, Flip_D2 0.28→0.40 | 1 / 9 | 39 → 72 | 8 → 18 | 6.5 |
| L011 | Afternoon | 0.005 → 0.047 | 0.137 → 0.246 | — | 1 / 8 | 42 → 74 | 8 → 12 | 5.6 |
| L012 | Afternoon | 0.024 → 0.146 | 0.144 → 0.270 | — | 1 / 8 | 50 → 110 | 8 → 18 | 5.6 |
| L013 | Afternoon | 0.072 → 0.226 | 0.151 → 0.414 | — | 1 / 13 | 31 → 50 | 8 → 15 | 5.3 |
| L014 | Afternoon | 0.153 → 0.353 | 0.216 → 0.393 | — | 1 / 8 | 49 → 39 | 6 → 13 | 5.6 |
| L015 | Afternoon | 0.021 → 0.265 | 0.098 → 0.379 | — | 1 / 7 | 38 → 66 | 8 → 16 | 5.7 |
| L016 | Afternoon | 0.078 → 0.190 | 0.141 → 0.314 | Flip_S2 0.27→0.44, Flip_VA 0.36→0.54, Flip_VB 0.39→0.47 | 1 / 6 | 80 → 87 | 8 → 12 | 5.5 |
| L017 | Afternoon | 0.002 → 0.118 | 0.054 → 0.214 | — | 1 / 7 | 47 → 92 | 8 → 21 | 5.6 |
| L018 | Afternoon | 0.005 → 0.094 | 0.171 → 0.387 | — | 1 / 8 | 60 → 121 | 10 → 25 | 5.6 |
| L019 | Afternoon | 0.033 → 0.213 | 0.108 → 0.348 | — | 1 / 6 | 36 → 56 | 8 → 20 | 5.6 |
| L020 | Afternoon | 0.088 → 0.097 | 0.124 → 0.291 | — | 1 / 8 | 38 → 68 | 6 → 20 | 5.6 |

**Acceptance against §3 and §13:**
- **A1:**
  - Every level's worst floor is better than before, and the medians are 2–3× higher.
  - **Threshold set from these numbers:** in band 1, the worst floor in view ≥ 0.01 and the median ≥ 0.14 (the lowest
    after values are 0.014 in L006 and 0.143 in L008), plus the hard rule that no level falls below its before.
  - The A1 sampler skips the door's area and trap and hazard boxes, which aren't background. The same tool measured
    before and after.
- **A2:**
  - The builder places every flat-topped piece (aqueduct deck, colonnade lintel, arch lintel, pillar plinth) at least
    0.3 u clear of every walkable top over the camera's travel. `EnvironmentReadabilityTests` re-checks every saved level.
  - L001's and L006's old problems are gone: the A02 arch strip is removed, and the pillar plinth sits below the floor.
- **A3:** every flip ring in L004, L009, L010 (and L016) is higher than before, by 0.08–0.25.
- **A4:** no `FG_01` or A02 strip remains under `RealityRoot_A` in any level (test).
- **A5:** every recipe matches the approved grade table (`LevelLookConfigTests`).
- **A6:** with the peak measure (ruled), the door ranks first in all 20 levels. The mean measure ranks it 3rd to 17th.
- **Budget (restated):** SetPass 10–25 (≤ 30); draws 39–121 (≤ 130); environment textures 5.3–6.6 MB (≤ 32).
- **Coverage:** no pixel of the camera's clear colour at 20:9, 16:9 or 4:3, in any level.
- **Soft mid layers:** none. Mid pieces draw at their native 98.3 texels per unit, against about 73 screen pixels per unit
  at 1080p. The far islands at 1.5× (about 65 texels per unit) are slightly soft, which suits their haze.

**Tests:**
- **Touched fixtures, final build:** 359 / 359.
  - `EnvironmentKitImportTests`, `EnvironmentReadabilityTests`, `EnvironmentStackCoverageTests`, `LevelLookConfigTests`
  - `TrapArtRevealFrameTests`, `TrapArtParityTests`, `TrapArtFireEffectTests`, `TrapArtBuildTests`
  - `LevelSceneTests`, `SavedSceneSyncTests`, `CatSheetImportTests`, `CatAnimationCheckTests`
- `pytest`-style run of `Tools/Art/tests/test_env_kit.py`: 8 / 8 (no pytest on this machine; the test functions are run
  directly).
- The full batch suite is in §18.

**Status:** done on the clone, not committed. The developer's Editor steps (§12) regenerate the scenes in the main
project. The device look is unverified.

## 18. Tests (final build, batch mode on the clone)

- **Full EditMode suite (batches, `~/Parallax_batch_tools/run_batches.py`):** 1,854 total, 1,693 passed, 161 failed. The
  baseline was V07's 1,835; the new fixtures are `EnvironmentKitImportTests`, `EnvironmentReadabilityTests`,
  `EnvironmentStackCoverageTests` and `LevelLookConfigTests`.
  - **All 161 failures are `CatAnimationCheckTests`, in its one-time setup:** "RouteSession: scene 'Untitled' has unsaved
    changes".
  - **They're pre-existing and not caused by A15:**
    - `Band1RulesTests.TrapFloor_HasTheFloorsColour_AndDrawsOverHazardsAndFloors` builds objects in the Test Runner's
      scene and leaves it dirty.
    - The four new fixtures shift where the batch runner splits `rest_a` from `rest_b`, so the two now share a batch.
    - Reproduced on a pristine HEAD clone (`9c70ca5`, none of A15's changes): `Band1RulesTests` followed by
      `CatAnimationCheckTests` gives the same 161 failures.
    - `CatAnimationCheckTests` passes when its batch has no `Band1RulesTests` in front of it (the touched run below,
      and the bisect).
  - **Fix (not in A15's scope):** `Band1RulesTests` should restore the scene (`RouteSession.RecreateUntitledScene`), as
    PAX-075 R21 asks.
- **Touched fixtures, final build: 359 / 359.**
- **Slow level fixtures in the full run:** `Band2RouteResultsTests` 10/10, `Band2LevelTests` 11/11, and the route-heavy
  batch (with `ValidateRoutesTests`) 111/111.
- **Out of batch, as agreed:** `CheckpointSectionPlayModeTests`, for the developer's Test Runner.
- **Local tooling:** the four new fixtures were added to `~/Parallax_batch_tools/fixtures.txt`.
