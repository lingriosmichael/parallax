# PAX-A02 environment asset manifest — Stage 2

All files are LFS PNGs when supplied. Environment paths are rooted at `Assets/_Game/Art/`.
Gameplay/objects use a fixed PPU of 196.667 (the old Cat A PPU; `EnvironmentSpriteImporter.WorldPixelsPerUnit`, no longer read from the cat since PAX-A08 gave Cat A its own PPU of 143.304); Backgrounds and materials use half PPU (98.333); Backgrounds also use Android ASTC 6×6, Default Normal Quality, and are excluded from the gameplay atlas. `useBounds = false` means no clamp; CatCameraFollow has a ±1.6u Y dead zone and no separate offset. The cat centre reaches ±3.1u at floor/ceiling contact, therefore the conservative camera envelope is `maxAbsCamY = 3.1 + 1.6 = 4.7u`. At orthographic size 5, `height_u = 2 × 5 + 2 × screenSpeed × 4.7 + 0.5`: Sky (.05) = 10.97u = 1079px, Far (.15) = 11.91u = 1171px, Mid (.35) = 13.79u = 1356px. Stage 2 M1 changes background width to 2048px. M2 makes `GAME_Wall` a 394 × 394px two-axis material; M3 makes Platform Fill 394 × 394px and seamless on both axes; M4 requires a Platform Top with no lip above its top row. Backgrounds tile horizontally only. Levels whose vertical camera travel exceeds this must either enable CatCameraFollow bounds or get a vertical-tiling ticket. `✅` means imported (Stage 2b, 07c541e; Stage 2c, bf49517 and 6b8341c); the column showed `⏳` until PAX-A15 corrected it (2026-10-01). Levels no longer draw the Reality A strips or `A_FG_01` (PAX-A15 replaced them with the environment kit below); the files stay for the sandbox and a later cleanup.

| Slot file name | Reality | What it is | Source | Reference image/spec | Draw mode | Target size (px) | Pivot | Pair motif | Raw source | Notes |
|---|---|---|---|---|---|---:|---|---|---|---|
| A_BG_00_Sky.png | A | warm golden sky/cloud strip | Manual | 14_environment-breakdown | Tiled | 2048 × 1079 | centre |  | Art_Source/Environment/A/A_BG_00_Sky__raw_v1.png · P-A1 · 2026-09-20 | ✅ `Environment/Backgrounds/`, speed .05 |
| A_BG_01_Far.png | A | floating sandstone ruins strip | Manual | 14_environment-breakdown | Tiled | 2048 × 1171 | centre |  | Art_Source/Environment/A/A_BG_01_Far__raw_v1.png · P-A2 · 2026-09-20 · haze 0.6 | ✅ `Environment/Backgrounds/`, speed .15 |
| A_MG_01_Mid.png | A | arches/roots strip | Manual | 14_environment-breakdown | Tiled | 2048 × 1356 | centre |  | Art_Source/Environment/A/A_MG_01_Mid__raw_v1.png · P-A3 · 2026-09-20 · haze 0.45 | ✅ `Environment/Backgrounds/`, speed .35 |
| A_GAME_Platform_Top.png | A | sandstone surface edge | Manual | 14_environment-breakdown | Tiled | 787 × 49 | top centre |  | Art_Source/Environment/A/A_GAME_Platform_Top__raw_v1.png · P-A4 · 2026-09-20 · tint #FFD9A0 0.45 | ✅ 4 × .25u module; surface line = top pixel row; no lip above it |
| A_GAME_Platform_Fill.png | A | sandstone platform body | Manual | 14_environment-breakdown | Tiled | 394 × 394 | centre |  | Art_Source/Environment/A/A_GAME_Platform_Fill__raw_v1.png · P-A5 · 2026-09-20 · tint #E8B87A 0.35 | ✅ geometry fill, seamless both axes |
| A_GAME_Wall.png | A | sandstone wall/ceiling body | Manual | 14_environment-breakdown | Tiled | 394 × 394 | centre |  | Art_Source/Environment/A/A_GAME_Wall__raw_v1.png · P-A6 · 2026-09-20 · tint #E8B87A 0.35 | ✅ 2 × 2u module, seamless both axes |
| A_OBJ_Vine.png | A | pullable vine/knot | AutoSprite | 03_vine-elevator-anchor_v2 | Simple | 236 × 433 | bottom centre | Pathway01 leaf-notch glyph | Art_Source/Environment/A/A_OBJ_Vine.png · O-A1 · 2026-09-20 | ✅ child of moving Knot; transparent side-on |
| A_OBJ_Plate.png | A | pressure plate surround | AutoSprite | 03_vine-elevator-anchor_v2 | Simple | 236 × 30 | bottom centre | Gate01 split-chevron glyph | Art_Source/Environment/A/A_OBJ_Plate.png · O-A2 · 2026-09-20 | ✅ beside Indicator; no pressed-state swap; actual object aspect 14.34:1 vs spec 7.87:1, accepted 2026-09-20 |
| A_OBJ_Station.png | A | gravity control station | AutoSprite | 04_gravity-shift | Simple | 236 × 315 | bottom centre |  | Art_Source/Environment/A/A_OBJ_Station.png · O-A3 · 2026-09-20 | ✅ beside Base/Glow/Pulse; actual object aspect 1:1.97 vs spec 1:1.33, accepted 2026-09-20 |
| A_OBJ_Checkpoint.png | A | checkpoint marker | AutoSprite | 14_environment-breakdown | Simple | 295 × 393 | bottom centre |  | Art_Source/Environment/A/A_OBJ_Checkpoint.png · O-A4 · 2026-09-20 | ✅ no collider changes; actual object aspect 1:2.62 vs spec 1:1.33, accepted 2026-09-20 |
| A_OBJ_Hazard.png | A | dark reflective-water edge | Manual | 14_environment-breakdown | Tiled | 787 × 197 | top centre |  | Art_Source/Environment/A/A_OBJ_Hazard__raw_v1.png · P-A7 · 2026-09-20 | ✅ visual-only fall-volume accent |
| A_FG_01.png | A | hanging roots/banner accent | AutoSprite | 14_environment-breakdown | Simple | 787 × 590 | top centre |  | Art_Source/Environment/A/A_FG_01.png · O-A5 · 2026-09-20 | ✅ optional; actual object aspect 2.06:1 vs spec 1.33:1, accepted 2026-09-20 |
| B_BG_00_Sky.png | B | starry void strip | Manual | 14_environment-breakdown | Tiled | 2048 × 1079 | centre |  | Art_Source/Environment/B/B_BG_00_Sky__raw_v1.png · P-B1 · 2026-09-20 | ✅ `Environment/Backgrounds/`, speed .05 |
| B_BG_01_Far.png | B | distant obsidian monoliths | Manual | 14_environment-breakdown | Tiled | 2048 × 1171 | centre |  | Art_Source/Environment/B/B_BG_01_Far__raw_v1.png · P-B2 · 2026-09-20 · haze 0.45 | ✅ `Environment/Backgrounds/`, speed .15 |
| B_MG_01_Mid.png | B | wireframe-grid strip | Manual | 14_environment-breakdown | Tiled | 2048 × 1356 | centre |  | Art_Source/Environment/B/B_MG_01_Mid__raw_v1.png · P-B3 · 2026-09-20 · haze 0.25 | ✅ `Environment/Backgrounds/`, speed .35 |
| B_GAME_Platform_Top.png | B | cyan-lit obsidian surface edge | Manual | 14_environment-breakdown | Tiled | 787 × 49 | top centre |  | Art_Source/Environment/B/B_GAME_Platform_Top__raw_v1.png · P-B4 · 2026-09-20 | ✅ 4 × .25u module; surface line = top pixel row; no lip above it |
| B_GAME_Platform_Fill.png | B | obsidian platform body | Manual | 14_environment-breakdown | Tiled | 394 × 394 | centre |  | Art_Source/Environment/B/B_GAME_Platform_Fill__raw_v1.png · P-B5 · 2026-09-20 | ✅ geometry fill, seamless both axes |
| B_GAME_Wall.png | B | obsidian wall/ceiling body | Manual | 14_environment-breakdown | Tiled | 394 × 394 | centre |  | Art_Source/Environment/B/B_GAME_Wall__raw_v1.png · P-B6 · 2026-09-20 | ✅ 2 × 2u module, seamless both axes |
| B_OBJ_Elevator.png | B | raised platform | AutoSprite | 03_vine-elevator-anchor_v2 | Simple | 393 × 79 | centre | Pathway01 leaf-notch glyph | Art_Source/Environment/B/B_OBJ_Elevator.png · O-B1 · 2026-09-20 | ✅ child of Elevator_B; actual object aspect 8.04:1 vs spec 4.97:1, accepted 2026-09-20 |
| B_OBJ_Gate.png | B | vertical obsidian gate | AutoSprite | 03_vine-elevator-anchor_v2 | Simple | 98 × 1007 | bottom centre | Gate01 split-chevron glyph | Art_Source/Environment/B/B_OBJ_Gate.png · O-B2 · 2026-09-20 | ✅ child of Gate_B; actual object aspect 1:11.98 vs spec 1:10.28, accepted 2026-09-20 |
| B_OBJ_Checkpoint.png | B | cold checkpoint monolith | AutoSprite | 14_environment-breakdown | Simple | 295 × 393 | bottom centre |  | Art_Source/Environment/B/B_OBJ_Checkpoint.png · O-B3 · 2026-09-20 | ✅ no collider changes; actual object aspect 1:2.52 vs spec 1:1.33, accepted 2026-09-20 |
| B_OBJ_Hazard.png | B | void-edge accent | Manual | 14_environment-breakdown | Tiled | 787 × 197 | top centre |  | Art_Source/Environment/B/B_OBJ_Hazard__raw_v1.png · P-B7 · 2026-09-20 | ✅ visual-only fall-volume accent |
| B_FG_01.png | B | floating shards/mist accent | AutoSprite | 14_environment-breakdown | Simple | 787 × 590 | top centre |  | Art_Source/Environment/B/B_FG_01.png · O-B4 · 2026-09-20 | ✅ optional; actual object aspect 2.83:1 vs spec 1.33:1, accepted 2026-09-20 |

Stage 2 prompt provenance is intentionally blank until an approved asset is generated. The `DesignImages/*.png.txt` files are written style specifications, not images.

## Cat A animation slots (PAX-A08 Stage 0, 2026-09-30)

Generated by `Tools/Art/cat_register.py` from `Art_Source/AutoSprite/Cats/A/_import/slots.json`; full data in `_import/manifest.json`. Scale (option B): standing body height on Idle frame 0 (01_idle f19) = 107 px at the 256 px source = 0.56 u, so **PPU 143.30** at the **192 px base cell** (191.07 at source scale). Torso 0.701 u. Shared world pivot: the paw row, 0.06 u behind Idle's median torso centre; each sheet has its own cell (whole ASTC 6×6 blocks) and records where the pivot falls in it. ASTC 6×6: 2.91 MB colour + 2.91 MB normals = **5.83 MB** (PAX-A14). Normal maps: `trap_process.normal_map`. Destination `Assets/_Game/Art/Cats/CatA/` (Idle, Walk, Rise, Fall and Land replace their sheets in place, keeping GUIDs).

| Slot file | Source clip (frames) | Frames | Loop | Cell (px) | Pivot (normalized) | Notes |
|---|---|---:|---|---|---|---|
| `CatA_Idle.png` | 01_idle (19–46) | 10 | yes, /3, close 0.96 | 192×192 | (0.5451, 0.0605) |  |
| `CatA_IdleLook.png` | 17_look_around (6–20) | 8 | no | 198×192 | (0.5386, 0.0605) | source cut at its cell edge |
| `CatA_IdleEar.png` | 12b_fidget_ear (5–19) | 8 | no | 192×192 | (0.5451, 0.0605) |  |
| `CatA_IdleSit.png` | 12c_fidget_sit (5–24) | 8 | no | 198×192 | (0.5285, 0.0605) | source cut at its cell edge |
| `CatA_Walk.png` | 03_walk (25–45) | 11 | yes, /2, close 0.64 | 192×192 | (0.5451, 0.0605) |  |
| `CatA_Run.png` | 02_run (45–54) | 10 | yes, /1, close 0.68 | 228×192 | (0.5511, 0.0605) | PAX-A14: scale 1.183 (Walk's torso length) |
| `CatA_Turn.png` | 18_turn (12–16) | 3 | no | 192×192 | (0.5451, 0.0605) |  |
| `CatA_TakeOff.png` | 04_jump_rise (6–9) | 2 | no | 192×192 | (0.5451, 0.0605) | source cut at its cell edge |
| `CatA_Rise.png` | 04_jump_rise (12–18) | 3 | no | 204×216 | (0.5669, 0.1649) | source cut at its cell edge |
| `CatA_Apex.png` | 05_jump_apex (13–17) | 2 | no | 198×192 | (0.5386, 0.0605) |  |
| `CatA_Fall.png` | 06_jump_fall (4–20) | 3 | no | 192×192 | (0.5451, 0.0605) |  |
| `CatA_Land.png` | 07_land (9–16) | 3 | no | 198×192 | (0.5386, 0.0605) | source cut at its cell edge |
| `CatA_HardLand.png` | 19_hard_land (7–13) | 4 | no | 198×192 | (0.5285, 0.0605) | source cut at its cell edge |
| `CatA_Death.png` | 20_death_generic (9–16) | 6 | no | 198×192 | (0.5285, 0.0605) |  |
| `CatA_Death_Pit.png` | 13f_death_pit (2–12) | 6 | no | 198×192 | (0.5285, 0.0605) | scale 1.274 |
| `CatA_Death_Spiked.png` | 13a_death_spiked (13–24) | 6 | no | 264×204 | (0.3964, 0.0619) | source cut at its cell edge; PAX-A14: shifted +0.44 u so frame 0's paws meet the spikes |
| `CatA_Death_Crushed.png` | 13b_death_crushed (9–19) | 6 | no | 198×192 | (0.5285, 0.0605) | source cut at its cell edge |
| `CatA_Death_Zapped.png` | 13c_death_zapped (5–15) | 6 | no | 192×192 | (0.5451, 0.0605) | PAX-A14: not style-matched (keeps its glow) |
| `CatA_Death_Arrow.png` | 13e_death_arrow (9–17) | 6 | no | 204×198 | (0.5130, 0.0789) | source cut at its cell edge |
| `CatA_Respawn.png` | 15_respawn (4–19) | 4 | no | 192×192 | (0.5451, 0.0605) |  |
| `CatA_Flip.png` | 21_gravity_twist (7–12) | 4 | no | 192×204 | (0.5451, 0.1158) |  |
| `CatA_Climb.png` | 08_vine_climb_up (24–48) | 25 | yes, /1, close 0.89 | 192×246 | (0.5451, 0.2668) | vine keyed, source cut at its cell edge |
| `CatA_Hang.png` | 10_vine_hang (14–14) | 1 | no | 192×228 | (0.5451, 0.2089) | vine keyed |
| `CatA_Leap.png` | 11_vine_leap (10–20) | 3 | no | 198×192 | (0.5386, 0.0605) | vine keyed, source cut at its cell edge |
| `CatA_Door.png` | 16b_celebrate (5–19) | 8 | no | 198×192 | (0.5386, 0.0605) | source cut at its cell edge |
| `CatA_DoorEnter.png` | 14_door_enter (9–23) | 8 | no | 228×192 | (0.6169, 0.0605) | imported, not wired, source cut at its cell edge |
| `CatA_LandContact.png` | bridge: Land 0 raised | 1 | no | 198×192 | (0.5386, 0.0605) | PAX-A14: plays inside Land at position 0 |
| `CatA_HardLandContact.png` | bridge: HardLand 1 raised | 1 | no | 198×192 | (0.5285, 0.0605) | PAX-A14: plays inside HardLand at position 1 |
| `CatA_FlipRoll.png` | bridge: Flip cels turned | 4 | no | 192×192 | (0.5451, 0.0605) | PAX-A14: plays inside Flip at positions 1, 2, 4, 6 |

**PAX-A14 consistency pass (2026-09-30), offline:** every clip's tones are matched to Walk's (luma quantiles over opaque pixels, RGB scaled per pixel, alpha untouched), except Death_Zapped; Run is scaled to Walk's torso length (1.183); Death_Spiked is re-registered onto the spike edge; detached never-drawn specks (1,371) are removed from every sheet. Idle's standing height and the PPU are unchanged. Bridge sheets (`bridgeFor` in the manifest) are imported like any sheet; `CatVisualSetup` plays their frames inside their host clip at `bridgePositions`.

**Retired Cat A clips (PAX-A14 §2, ruled 2026-09-30):** tail flick (12a), dizzy (23), splash death (13d; v1 has no water), Turn (`CatA_Turn` stays in the table but isn't played), and the current Launched clip (22; the Launched state stays on the backlog, a geyser shows Rise). Kept for later: climb down (09; A14 found it unusable as drawn, see NEEDED_ASSETS), fed up (PAX-V07b), menu idle (PAX-A09), door enter (imported, not wired). Nothing in `Art_Source/` is deleted.

## Environment kit (PAX-A15, 2026-10-01)

Made by `Tools/Art/env_kit.py` from the developer's ChatGPT stills in `Art_Source/Environment/A/` (never changed), into
`Assets/_Game/Art/RealityA/Environment/Kit/` (play layer) and `…/Backgrounds/Kit/` (layers). Every piece is resampled to
the size below, so the importer's PPU never changes: 196.667 for the play layer, 98.333 for fills (materials) and
layers. Tiles are seamless on their tiling axis (offset-and-crossfade; seam ratios in the A15 report). Pivot: centre
for all. `env_kit.json` beside the sprites records each slot's size in units and the offsets the builder aligns by.
Android: the play kit ASTC 4×4, layers and normal maps ASTC 6×6. Normal maps (`<slot>_n.png`) are attached as
`_NormalMap` by `PARALLAX/Setup/Levels/Environment Stack`. Not used yet: ENV-01 (the sky is a gradient made in
code), ENV-10d (corners: the wall faces and caps meet instead), ENV-10c's upper strip, `ENV_CheckpointLit` (the
section marker lights by colour), `ENV_FG_Branch` and `ENV_FG_Trunk` (no level has solid ground wide enough at a frame
edge to hold them without covering the room's air).

| Slot file | Source | Kind | Size (px) | Size (u) | PPU | Tiles | Normal map |
|---|---|---|---:|---:|---:|---|---|
| `ENV_BackWall.png` | ENV-32 | band | 1416 × 938 | 14.40 × 9.54 | 98.333 | sideways |  |
| `ENV_Banner_0.png` | ENV-22 | dressing | 197 × 571 | 1.00 × 2.90 | 196.667 | — |  |
| `ENV_Banner_1.png` | ENV-22 | dressing | 197 × 374 | 1.00 × 1.90 | 196.667 | — |  |
| `ENV_Cap.png` | ENV-10b | strip | 787 × 90 | 4.00 × 0.46 | 196.667 | sideways | yes |
| `ENV_Checkpoint.png` | ENV-25 | object | 295 × 240 | 1.50 × 1.22 | 196.667 | — |  |
| `ENV_CheckpointLit.png` | ENV-25 | object | 295 × 240 | 1.50 × 1.22 | 196.667 | — |  |
| `ENV_CloudsFar.png` | ENV-03c | band | 1276 × 343 | 12.98 × 3.49 | 98.333 | sideways |  |
| `ENV_CloudsMid.png` | ENV-03b | band | 1276 × 481 | 12.98 × 4.89 | 98.333 | sideways |  |
| `ENV_CloudsNear.png` | ENV-03a | band | 1276 × 573 | 12.98 × 5.83 | 98.333 | sideways |  |
| `ENV_Door.png` | ENV-24 (lit) | object | 334 × 362 | 1.70 × 1.84 | 196.667 | — |  |
| `ENV_Drape_0.png` | ENV-20 | dressing | 177 × 187 | 0.90 × 0.95 | 196.667 | — |  |
| `ENV_Drape_1.png` | ENV-20 | dressing | 177 × 256 | 0.90 × 1.30 | 196.667 | — |  |
| `ENV_Drape_2.png` | ENV-20 | dressing | 177 × 332 | 0.90 × 1.69 | 196.667 | — |  |
| `ENV_FG_Branch.png` | ENV-26 | foreground | 512 × 290 | 5.21 × 2.95 | 98.333 | — |  |
| `ENV_FG_Ferns.png` | ENV-26 | foreground | 512 × 239 | 5.21 × 2.43 | 98.333 | — |  |
| `ENV_FG_Roots.png` | ENV-26 | foreground | 512 × 234 | 5.21 × 2.38 | 98.333 | — |  |
| `ENV_FG_Trunk.png` | ENV-26b | foreground | 512 × 768 | 5.21 × 7.81 | 98.333 | — |  |
| `ENV_FarIsland_0.png` | ENV-04 | piece | 298 × 339 | 3.03 × 3.45 | 98.333 | — |  |
| `ENV_FarIsland_1.png` | ENV-04 | piece | 318 × 355 | 3.23 × 3.61 | 98.333 | — |  |
| `ENV_FarIsland_2.png` | ENV-04 | piece | 261 × 373 | 2.65 × 3.79 | 98.333 | — |  |
| `ENV_FarIsland_3.png` | ENV-04 | piece | 300 × 351 | 3.05 × 3.57 | 98.333 | — |  |
| `ENV_FarSpire_0.png` | ENV-05 | piece | 232 × 850 | 2.36 × 8.64 | 98.333 | — |  |
| `ENV_FarSpire_1.png` | ENV-05 | piece | 210 × 711 | 2.14 × 7.23 | 98.333 | — |  |
| `ENV_FarSpire_2.png` | ENV-05 | piece | 753 × 496 | 7.66 × 5.04 | 98.333 | — |  |
| `ENV_FarSpire_3.png` | ENV-05 | piece | 165 × 361 | 1.68 × 3.67 | 98.333 | — |  |
| `ENV_Fill_A.png` | ENV-10a | fill | 394 × 394 | 4.01 × 4.01 | 98.333 | both axes | yes |
| `ENV_Fill_A2.png` | ENV-10a2 | fill | 394 × 394 | 4.01 × 4.01 | 98.333 | both axes | yes |
| `ENV_Fill_A3.png` | ENV-10a3 | fill | 394 × 394 | 4.01 × 4.01 | 98.333 | both axes | yes |
| `ENV_Fog.png` | ENV-27 | band | 1236 × 514 | 12.57 × 5.23 | 98.333 | sideways |  |
| `ENV_Glow.png` | made by the tool | object | 256 × 256 | 1.30 × 1.30 | 196.667 | — |  |
| `ENV_Glyph_0.png` | ENV-23 | dressing | 275 × 217 | 1.40 × 1.10 | 196.667 | — |  |
| `ENV_Glyph_1.png` | ENV-23 | dressing | 275 × 226 | 1.40 × 1.15 | 196.667 | — |  |
| `ENV_Halo.png` | made by the tool | object | 256 × 256 | 1.30 × 1.30 | 196.667 | — |  |
| `ENV_Haze.png` | ENV-06 | band | 1236 × 561 | 12.57 × 5.71 | 98.333 | sideways |  |
| `ENV_MidAqueduct.png` | ENV-07c | band | 1026 × 642 | 10.43 × 6.53 | 98.333 | sideways |  |
| `ENV_MidArch_0.png` | ENV-07a | piece | 403 × 595 | 4.10 × 6.05 | 98.333 | — |  |
| `ENV_MidArch_1.png` | ENV-07a | piece | 321 × 704 | 3.26 × 7.16 | 98.333 | — |  |
| `ENV_MidColonnade.png` | ENV-07d | piece | 865 × 634 | 8.80 × 6.45 | 98.333 | — |  |
| `ENV_MidPillar.png` | ENV-15 | piece | 575 × 1105 | 5.85 × 11.24 | 98.333 | — |  |
| `ENV_MidTowers.png` | ENV-07d | piece | 575 × 903 | 5.85 × 9.18 | 98.333 | — |  |
| `ENV_MidTree_0.png` | ENV-07b | piece | 629 × 950 | 6.40 × 9.66 | 98.333 | — |  |
| `ENV_MidTree_1.png` | ENV-07b | piece | 715 × 880 | 7.27 × 8.95 | 98.333 | — |  |
| `ENV_Mist.png` | ENV-09 | piece | 1434 × 900 | 14.58 × 9.15 | 98.333 | — |  |
| `ENV_Moss_0.png` | ENV-21 | dressing | 216 × 92 | 1.10 × 0.47 | 196.667 | — |  |
| `ENV_Moss_1.png` | ENV-21 | dressing | 216 × 74 | 1.10 × 0.38 | 196.667 | — |  |
| `ENV_Moss_2.png` | ENV-21 | dressing | 216 × 80 | 1.10 × 0.41 | 196.667 | — |  |
| `ENV_Post.png` | ENV-14a | vstrip | 230 × 1232 | 1.17 × 6.26 | 196.667 | downward | yes |
| `ENV_PostBase.png` | ENV-14b | object | 246 × 230 | 1.25 × 1.17 | 196.667 | — | yes |
| `ENV_PostCap.png` | ENV-14b | object | 295 × 238 | 1.50 × 1.21 | 196.667 | — | yes |
| `ENV_Rubble_0.png` | ENV-23 | dressing | 275 × 109 | 1.40 × 0.55 | 196.667 | — |  |
| `ENV_Rubble_1.png` | ENV-23 | dressing | 275 × 103 | 1.40 × 0.52 | 196.667 | — |  |
| `ENV_Shaft.png` | ENV-28 | piece | 1024 × 1519 | 10.41 × 15.45 | 98.333 | — |  |
| `ENV_Side.png` | ENV-12 | vstrip | 120 × 787 | 0.61 × 4.00 | 196.667 | downward | yes |
| `ENV_SkyFade.png` | made by the tool | sky | 8 × 256 | 0.08 × 2.60 | 98.333 | — |  |
| `ENV_Slab.png` | ENV-11 | strip | 578 × 193 | 2.94 × 0.98 | 196.667 | sideways | yes |
| `ENV_SlabEnd.png` | ENV-11 | end | 205 × 193 | 1.04 × 0.98 | 196.667 | — | yes |
| `ENV_SlimPost.png` | ENV-14c | vstrip | 140 × 1698 | 0.71 × 8.63 | 196.667 | downward | yes |
| `ENV_Sun.png` | ENV-02 | piece | 640 × 640 | 6.51 × 6.51 | 98.333 | — |  |
| `ENV_Under.png` | ENV-10c | strip | 709 × 162 | 3.61 × 0.82 | 196.667 | sideways | yes |
| `ENV_VineAnchor.png` | ENV-18 | object | 211 × 352 | 1.07 × 1.79 | 196.667 | — |  |
| `ENV_VineMid.png` | ENV-18 | vstrip | 118 × 763 | 0.60 × 3.88 | 196.667 | downward |  |
| `ENV_VineTip.png` | ENV-18 | object | 128 × 261 | 0.65 × 1.33 | 196.667 | — |  |
| `ENV_Water.png` | ENV-30 | strip | 619 × 162 | 3.15 × 0.82 | 196.667 | sideways |  |
| `ENV_Waterfall.png` | ENV-08b | vband | 422 × 1356 | 4.29 × 13.79 | 98.333 | downward |  |
| `ENV_White.png` | made by the tool | sky | 8 × 8 | 0.08 × 0.08 | 98.333 | — |  |

## Environment depth tiers (PAX-A16, 2026-10-01)

Made by `Tools/Art/env_tiers.py` (called from `env_kit.py`) from the same ENV stills: no new paintings. Same folders, PPUs,
pivots and Android formats as the A15 kit. A16 also changed four A15 slots in place:
- the fills: mortar contrast softened 28% and 25% less orange;
- `ENV_Haze` and `ENV_Fog`: cream rather than orange;
- the far and mid pieces: their lowest 14% fades out, so a cut base never draws a line.

ENV-01 (the painted sky) is now used.

| Slot file | Source | Kind | Size (px) | Size (u) | PPU | Tiles | Normal map |
|---|---|---|---:|---:|---:|---|---|
| `ENV_ArchFringe.png` | ENV-07c (arch tops) | strip | 675 × 177 | 3.43 × 0.90 | 196.667 | sideways |  |
| `ENV_ArchShade.png` | ENV-07c (openings) | strip | 2077 × 511 | 10.56 × 2.60 | 196.667 | sideways |  |
| `ENV_CapWash.png` | ENV-10b + a made wash | strip | 787 × 228 | 4.00 × 1.16 | 196.667 | sideways |  |
| `ENV_Chain.png` | made by the tool | vstrip | 26 × 320 | 0.13 × 1.63 | 196.667 | downward |  |
| `ENV_FarCity.png` | ENV-05, ENV-07a, ENV-07d | band | 3200 × 760 | 32.54 × 7.73 | 98.333 | sideways |  |
| `ENV_FrameL.png` | ENV-26, ENV-26b | foreground | 936 × 789 | 9.52 × 8.02 | 98.333 | — |  |
| `ENV_FrameTop.png` | ENV-20 | foreground | 2400 × 560 | 24.41 × 5.69 | 98.333 | sideways |  |
| `ENV_LipVines.png` | ENV-20 | strip | 650 × 122 | 3.31 × 0.62 | 196.667 | sideways |  |
| `ENV_Mote.png` | made by the tool | piece | 32 × 32 | 0.33 × 0.33 | 98.333 | — |  |
| `ENV_Ray.png` | ENV-28 | piece | 1120 × 1615 | 11.39 × 16.42 | 98.333 | — |  |
| `ENV_SkyPlate.png` | ENV-01 | sky | 1536 × 1024 | 15.62 × 10.41 | 98.333 | — |  |
| `ENV_WaterPlane.png` | ENV-30, ENV_FarCity | band | 1480 × 375 | 15.05 × 3.81 | 98.333 | sideways |  |

