# PAX-A02 environment asset manifest — Stage 2

All files are LFS PNGs when supplied. Environment paths are rooted at `Assets/_Game/Art/`.
Gameplay/objects use a fixed PPU of 196.667 (the old Cat A PPU; `EnvironmentSpriteImporter.WorldPixelsPerUnit`, no longer read from the cat since PAX-A08 gave Cat A its own PPU of 143.304); Backgrounds and materials use half PPU (98.333); Backgrounds also use Android ASTC 6×6, Default Normal Quality, and are excluded from the gameplay atlas. `useBounds = false` means no clamp; CatCameraFollow has a ±1.6u Y dead zone and no separate offset. The cat centre reaches ±3.1u at floor/ceiling contact, therefore the conservative camera envelope is `maxAbsCamY = 3.1 + 1.6 = 4.7u`. At orthographic size 5, `height_u = 2 × 5 + 2 × screenSpeed × 4.7 + 0.5`: Sky (.05) = 10.97u = 1079px, Far (.15) = 11.91u = 1171px, Mid (.35) = 13.79u = 1356px. Stage 2 M1 changes background width to 2048px. M2 makes `GAME_Wall` a 394 × 394px two-axis material; M3 makes Platform Fill 394 × 394px and seamless on both axes; M4 requires a Platform Top with no lip above its top row. Backgrounds tile horizontally only. Levels whose vertical camera travel exceeds this must either enable CatCameraFollow bounds or get a vertical-tiling ticket. `⏳` means intentionally absent until approved art is processed.

| Slot file name | Reality | What it is | Source | Reference image/spec | Draw mode | Target size (px) | Pivot | Pair motif | Raw source | Notes |
|---|---|---|---|---|---|---:|---|---|---|---|
| A_BG_00_Sky.png | A | warm golden sky/cloud strip | Manual | 14_environment-breakdown | Tiled | 2048 × 1079 | centre |  | Art_Source/Environment/A/A_BG_00_Sky__raw_v1.png · P-A1 · 2026-09-20 | ⏳ `Environment/Backgrounds/`, speed .05 |
| A_BG_01_Far.png | A | floating sandstone ruins strip | Manual | 14_environment-breakdown | Tiled | 2048 × 1171 | centre |  | Art_Source/Environment/A/A_BG_01_Far__raw_v1.png · P-A2 · 2026-09-20 · haze 0.6 | ⏳ `Environment/Backgrounds/`, speed .15 |
| A_MG_01_Mid.png | A | arches/roots strip | Manual | 14_environment-breakdown | Tiled | 2048 × 1356 | centre |  | Art_Source/Environment/A/A_MG_01_Mid__raw_v1.png · P-A3 · 2026-09-20 · haze 0.45 | ⏳ `Environment/Backgrounds/`, speed .35 |
| A_GAME_Platform_Top.png | A | sandstone surface edge | Manual | 14_environment-breakdown | Tiled | 787 × 49 | top centre |  | Art_Source/Environment/A/A_GAME_Platform_Top__raw_v1.png · P-A4 · 2026-09-20 · tint #FFD9A0 0.45 | ⏳ 4 × .25u module; surface line = top pixel row; no lip above it |
| A_GAME_Platform_Fill.png | A | sandstone platform body | Manual | 14_environment-breakdown | Tiled | 394 × 394 | centre |  | Art_Source/Environment/A/A_GAME_Platform_Fill__raw_v1.png · P-A5 · 2026-09-20 · tint #E8B87A 0.35 | ⏳ geometry fill, seamless both axes |
| A_GAME_Wall.png | A | sandstone wall/ceiling body | Manual | 14_environment-breakdown | Tiled | 394 × 394 | centre |  | Art_Source/Environment/A/A_GAME_Wall__raw_v1.png · P-A6 · 2026-09-20 · tint #E8B87A 0.35 | ⏳ 2 × 2u module, seamless both axes |
| A_OBJ_Vine.png | A | pullable vine/knot | AutoSprite | 03_vine-elevator-anchor_v2 | Simple | 236 × 433 | bottom centre | Pathway01 leaf-notch glyph | Art_Source/Environment/A/A_OBJ_Vine.png · O-A1 · 2026-09-20 | ⏳ child of moving Knot; transparent side-on |
| A_OBJ_Plate.png | A | pressure plate surround | AutoSprite | 03_vine-elevator-anchor_v2 | Simple | 236 × 30 | bottom centre | Gate01 split-chevron glyph | Art_Source/Environment/A/A_OBJ_Plate.png · O-A2 · 2026-09-20 | ⏳ beside Indicator; no pressed-state swap; actual object aspect 14.34:1 vs spec 7.87:1, accepted 2026-09-20 |
| A_OBJ_Station.png | A | gravity control station | AutoSprite | 04_gravity-shift | Simple | 236 × 315 | bottom centre |  | Art_Source/Environment/A/A_OBJ_Station.png · O-A3 · 2026-09-20 | ⏳ beside Base/Glow/Pulse; actual object aspect 1:1.97 vs spec 1:1.33, accepted 2026-09-20 |
| A_OBJ_Checkpoint.png | A | checkpoint marker | AutoSprite | 14_environment-breakdown | Simple | 295 × 393 | bottom centre |  | Art_Source/Environment/A/A_OBJ_Checkpoint.png · O-A4 · 2026-09-20 | ⏳ no collider changes; actual object aspect 1:2.62 vs spec 1:1.33, accepted 2026-09-20 |
| A_OBJ_Hazard.png | A | dark reflective-water edge | Manual | 14_environment-breakdown | Tiled | 787 × 197 | top centre |  | Art_Source/Environment/A/A_OBJ_Hazard__raw_v1.png · P-A7 · 2026-09-20 | ⏳ visual-only fall-volume accent |
| A_FG_01.png | A | hanging roots/banner accent | AutoSprite | 14_environment-breakdown | Simple | 787 × 590 | top centre |  | Art_Source/Environment/A/A_FG_01.png · O-A5 · 2026-09-20 | ⏳ optional; actual object aspect 2.06:1 vs spec 1.33:1, accepted 2026-09-20 |
| B_BG_00_Sky.png | B | starry void strip | Manual | 14_environment-breakdown | Tiled | 2048 × 1079 | centre |  | Art_Source/Environment/B/B_BG_00_Sky__raw_v1.png · P-B1 · 2026-09-20 | ⏳ `Environment/Backgrounds/`, speed .05 |
| B_BG_01_Far.png | B | distant obsidian monoliths | Manual | 14_environment-breakdown | Tiled | 2048 × 1171 | centre |  | Art_Source/Environment/B/B_BG_01_Far__raw_v1.png · P-B2 · 2026-09-20 · haze 0.45 | ⏳ `Environment/Backgrounds/`, speed .15 |
| B_MG_01_Mid.png | B | wireframe-grid strip | Manual | 14_environment-breakdown | Tiled | 2048 × 1356 | centre |  | Art_Source/Environment/B/B_MG_01_Mid__raw_v1.png · P-B3 · 2026-09-20 · haze 0.25 | ⏳ `Environment/Backgrounds/`, speed .35 |
| B_GAME_Platform_Top.png | B | cyan-lit obsidian surface edge | Manual | 14_environment-breakdown | Tiled | 787 × 49 | top centre |  | Art_Source/Environment/B/B_GAME_Platform_Top__raw_v1.png · P-B4 · 2026-09-20 | ⏳ 4 × .25u module; surface line = top pixel row; no lip above it |
| B_GAME_Platform_Fill.png | B | obsidian platform body | Manual | 14_environment-breakdown | Tiled | 394 × 394 | centre |  | Art_Source/Environment/B/B_GAME_Platform_Fill__raw_v1.png · P-B5 · 2026-09-20 | ⏳ geometry fill, seamless both axes |
| B_GAME_Wall.png | B | obsidian wall/ceiling body | Manual | 14_environment-breakdown | Tiled | 394 × 394 | centre |  | Art_Source/Environment/B/B_GAME_Wall__raw_v1.png · P-B6 · 2026-09-20 | ⏳ 2 × 2u module, seamless both axes |
| B_OBJ_Elevator.png | B | raised platform | AutoSprite | 03_vine-elevator-anchor_v2 | Simple | 393 × 79 | centre | Pathway01 leaf-notch glyph | Art_Source/Environment/B/B_OBJ_Elevator.png · O-B1 · 2026-09-20 | ⏳ child of Elevator_B; actual object aspect 8.04:1 vs spec 4.97:1, accepted 2026-09-20 |
| B_OBJ_Gate.png | B | vertical obsidian gate | AutoSprite | 03_vine-elevator-anchor_v2 | Simple | 98 × 1007 | bottom centre | Gate01 split-chevron glyph | Art_Source/Environment/B/B_OBJ_Gate.png · O-B2 · 2026-09-20 | ⏳ child of Gate_B; actual object aspect 1:11.98 vs spec 1:10.28, accepted 2026-09-20 |
| B_OBJ_Checkpoint.png | B | cold checkpoint monolith | AutoSprite | 14_environment-breakdown | Simple | 295 × 393 | bottom centre |  | Art_Source/Environment/B/B_OBJ_Checkpoint.png · O-B3 · 2026-09-20 | ⏳ no collider changes; actual object aspect 1:2.52 vs spec 1:1.33, accepted 2026-09-20 |
| B_OBJ_Hazard.png | B | void-edge accent | Manual | 14_environment-breakdown | Tiled | 787 × 197 | top centre |  | Art_Source/Environment/B/B_OBJ_Hazard__raw_v1.png · P-B7 · 2026-09-20 | ⏳ visual-only fall-volume accent |
| B_FG_01.png | B | floating shards/mist accent | AutoSprite | 14_environment-breakdown | Simple | 787 × 590 | top centre |  | Art_Source/Environment/B/B_FG_01.png · O-B4 · 2026-09-20 | ⏳ optional; actual object aspect 2.83:1 vs spec 1.33:1, accepted 2026-09-20 |

Stage 2 prompt provenance is intentionally blank until an approved asset is generated. The `DesignImages/*.png.txt` files are written style specifications, not images.

## Cat A animation slots (PAX-A08 Stage 0, 2026-09-30)

Generated by `Tools/Art/cat_register.py` from `Art_Source/AutoSprite/Cats/A/_import/slots.json`; full data in `_import/manifest.json`. Scale (option B): standing body height on Idle frame 0 (01_idle f19) = 107 px at the 256 px source = 0.56 u, so **PPU 143.30** at the **192 px base cell** (191.07 at source scale). Torso 0.701 u. Shared world pivot: the paw row, 0.06 u behind Idle's median torso centre; each sheet has its own cell (whole ASTC 6×6 blocks) and records where the pivot falls in it. ASTC 6×6: 2.76 MB colour + 2.76 MB normals = **5.52 MB**. Normal maps: `trap_process.normal_map`. Destination `Assets/_Game/Art/Cats/CatA/` (Idle, Walk, Rise, Fall and Land replace their sheets in place, keeping GUIDs).

| Slot file | Source clip (frames) | Frames | Loop | Cell (px) | Pivot (normalized) | Notes |
|---|---|---:|---|---|---|---|
| `CatA_Idle.png` | 01_idle (19–46) | 10 | yes, /3, close 0.96 | 192×192 | (0.5451, 0.0605) |  |
| `CatA_IdleLook.png` | 17_look_around (6–20) | 8 | no | 198×192 | (0.5386, 0.0605) | source cut at its cell edge |
| `CatA_IdleEar.png` | 12b_fidget_ear (5–19) | 8 | no | 192×192 | (0.5451, 0.0605) |  |
| `CatA_IdleSit.png` | 12c_fidget_sit (5–24) | 8 | no | 198×192 | (0.5285, 0.0605) | source cut at its cell edge |
| `CatA_Walk.png` | 03_walk (25–45) | 11 | yes, /2, close 0.64 | 192×192 | (0.5451, 0.0605) |  |
| `CatA_Run.png` | 02_run (45–54) | 10 | yes, /1, close 0.68 | 198×192 | (0.5336, 0.0605) |  |
| `CatA_Turn.png` | 18_turn (12–16) | 3 | no | 192×192 | (0.5451, 0.0605) |  |
| `CatA_TakeOff.png` | 04_jump_rise (6–9) | 2 | no | 192×192 | (0.5451, 0.0605) | source cut at its cell edge |
| `CatA_Rise.png` | 04_jump_rise (12–18) | 3 | no | 204×216 | (0.5669, 0.1649) | source cut at its cell edge |
| `CatA_Apex.png` | 05_jump_apex (13–17) | 2 | no | 198×192 | (0.5386, 0.0605) |  |
| `CatA_Fall.png` | 06_jump_fall (4–20) | 3 | no | 192×192 | (0.5451, 0.0605) |  |
| `CatA_Land.png` | 07_land (9–16) | 3 | no | 198×192 | (0.5386, 0.0605) | source cut at its cell edge |
| `CatA_HardLand.png` | 19_hard_land (7–13) | 4 | no | 198×192 | (0.5285, 0.0605) | source cut at its cell edge |
| `CatA_Death.png` | 20_death_generic (9–16) | 6 | no | 198×192 | (0.5285, 0.0605) |  |
| `CatA_Death_Pit.png` | 13f_death_pit (2–12) | 6 | no | 198×192 | (0.5285, 0.0605) | scale 1.274 |
| `CatA_Death_Spiked.png` | 13a_death_spiked (13–24) | 6 | no | 198×204 | (0.5285, 0.0619) | source cut at its cell edge |
| `CatA_Death_Crushed.png` | 13b_death_crushed (9–19) | 6 | no | 198×192 | (0.5285, 0.0605) | source cut at its cell edge |
| `CatA_Death_Zapped.png` | 13c_death_zapped (5–15) | 6 | no | 192×192 | (0.5451, 0.0605) |  |
| `CatA_Death_Arrow.png` | 13e_death_arrow (9–17) | 6 | no | 204×198 | (0.5130, 0.0789) | source cut at its cell edge |
| `CatA_Respawn.png` | 15_respawn (4–19) | 4 | no | 192×192 | (0.5451, 0.0605) |  |
| `CatA_Flip.png` | 21_gravity_twist (7–12) | 4 | no | 192×204 | (0.5451, 0.1158) |  |
| `CatA_Climb.png` | 08_vine_climb_up (24–48) | 25 | yes, /1, close 0.89 | 192×246 | (0.5451, 0.2668) | vine keyed, source cut at its cell edge |
| `CatA_Hang.png` | 10_vine_hang (14–14) | 1 | no | 192×228 | (0.5451, 0.2089) | vine keyed |
| `CatA_Leap.png` | 11_vine_leap (10–20) | 3 | no | 198×192 | (0.5386, 0.0605) | vine keyed, source cut at its cell edge |
| `CatA_Door.png` | 16b_celebrate (5–19) | 8 | no | 198×192 | (0.5386, 0.0605) | source cut at its cell edge |
| `CatA_DoorEnter.png` | 14_door_enter (9–23) | 8 | no | 228×192 | (0.6169, 0.0605) | imported, not wired, source cut at its cell edge |
