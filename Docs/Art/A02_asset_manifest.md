# PAX-A02 environment asset manifest — Stage 1

All files are LFS PNGs when supplied. Environment paths are rooted at `Assets/_Game/Art/`.
Gameplay/objects use Cat A's imported PPU (196.667); Backgrounds use half PPU (98.333), Android ASTC 6×6, Default Normal Quality, and are excluded from the gameplay atlas. `useBounds = false` means there is no camera clamp. CatCameraFollow tracks its target on Y through a ±1.6u dead zone and has no separate follow offset; its `SnapToTarget` path puts the camera at the target. In both realities, the 0.8u-tall cat has centre Y −3.1u at floor contact (ground top −3.5u) and +3.1u at ceiling contact (ceiling bottom +3.5u, with rotated gravity). Including the maximum 1.6u dead-zone follow separation, `maxAbsCamY = 3.1 + 1.6 = 4.7u` relative to each reality origin. At 20:9 and orthographic size 5, `height_u = 2 × 5 + 2 × screenSpeed × 4.7 + 0.5`: Sky (.05) = 10.97u = 1079px, Far (.15) = 11.91u = 1171px, Mid (.35) = 13.79u = 1356px at 98.333 PPU. Width is 4096px. Backgrounds tile horizontally only. Levels whose vertical camera travel exceeds this must either enable CatCameraFollow bounds or get a vertical-tiling ticket. `⏳` means intentionally absent in Stage 1.

| Slot file name | Reality | What it is | Source | Reference image/spec | Draw mode | Target size (px) | Pivot | Pair motif | Notes |
|---|---|---|---|---|---|---:|---|---|---|
| A_BG_00_Sky.png | A | warm golden sky/cloud strip | Manual | 14_environment-breakdown | Tiled | 4096 × 1079 | centre | | ⏳ `Environment/Backgrounds/`, speed .05 |
| A_BG_01_Far.png | A | floating sandstone ruins strip | Manual | 14_environment-breakdown | Tiled | 4096 × 1171 | centre | | ⏳ `Environment/Backgrounds/`, speed .15 |
| A_MG_01_Mid.png | A | arches/roots strip | Manual | 14_environment-breakdown | Tiled | 4096 × 1356 | centre | | ⏳ `Environment/Backgrounds/`, speed .35 |
| A_GAME_Platform_Top.png | A | sandstone surface edge | Manual | 14_environment-breakdown | Tiled | 787 × 49 | top centre | | ⏳ 4 × .25u module; surface line = top pixel row; lip ≤ 10 px may rise above only as transparent-edged detail |
| A_GAME_Platform_Fill.png | A | sandstone platform body | Manual | 14_environment-breakdown | Tiled | 787 × 197 | centre | | ⏳ geometry fill |
| A_GAME_Wall.png | A | sandstone wall/ceiling body | Manual | 14_environment-breakdown | Tiled | 197 × 1770 | centre | | ⏳ 1 × 9u module |
| A_OBJ_Vine.png | A | pullable vine/knot | AutoSprite | 03_vine-elevator-anchor_v2 | Simple | 236 × 433 | bottom centre | Pathway01 leaf-notch glyph | ⏳ child of moving Knot; transparent side-on |
| A_OBJ_Plate.png | A | pressure plate surround | AutoSprite | 03_vine-elevator-anchor_v2 | Simple | 236 × 30 | bottom centre | Gate01 split-chevron glyph | ⏳ beside Indicator; no pressed-state swap |
| A_OBJ_Station.png | A | gravity control station | AutoSprite | 04_gravity-shift | Simple | 236 × 315 | bottom centre | | ⏳ beside Base/Glow/Pulse |
| A_OBJ_Checkpoint.png | A | checkpoint marker | AutoSprite | 14_environment-breakdown | Simple | 295 × 393 | bottom centre | | ⏳ no collider changes |
| A_OBJ_Hazard.png | A | dark reflective-water edge | Manual | 14_environment-breakdown | Tiled | 787 × 197 | top centre | | ⏳ visual-only fall-volume accent |
| A_FG_01.png | A | hanging roots/banner accent | AutoSprite | 14_environment-breakdown | Simple | 787 × 590 | top centre | | ⏳ optional |
| B_BG_00_Sky.png | B | starry void strip | Manual | 14_environment-breakdown | Tiled | 4096 × 1079 | centre | | ⏳ `Environment/Backgrounds/`, speed .05 |
| B_BG_01_Far.png | B | distant obsidian monoliths | Manual | 14_environment-breakdown | Tiled | 4096 × 1171 | centre | | ⏳ `Environment/Backgrounds/`, speed .15 |
| B_MG_01_Mid.png | B | wireframe-grid strip | Manual | 14_environment-breakdown | Tiled | 4096 × 1356 | centre | | ⏳ `Environment/Backgrounds/`, speed .35 |
| B_GAME_Platform_Top.png | B | cyan-lit obsidian surface edge | Manual | 14_environment-breakdown | Tiled | 787 × 49 | top centre | | ⏳ 4 × .25u module; surface line = top pixel row; lip ≤ 10 px may rise above only as transparent-edged detail |
| B_GAME_Platform_Fill.png | B | obsidian platform body | Manual | 14_environment-breakdown | Tiled | 787 × 197 | centre | | ⏳ geometry fill |
| B_GAME_Wall.png | B | obsidian wall/ceiling body | Manual | 14_environment-breakdown | Tiled | 197 × 1770 | centre | | ⏳ 1 × 9u module |
| B_OBJ_Elevator.png | B | raised platform | AutoSprite | 03_vine-elevator-anchor_v2 | Simple | 393 × 79 | centre | Pathway01 leaf-notch glyph | ⏳ child of Elevator_B |
| B_OBJ_Gate.png | B | vertical obsidian gate | AutoSprite | 03_vine-elevator-anchor_v2 | Simple | 98 × 1007 | bottom centre | Gate01 split-chevron glyph | ⏳ child of Gate_B |
| B_OBJ_Checkpoint.png | B | cold checkpoint monolith | AutoSprite | 14_environment-breakdown | Simple | 295 × 393 | bottom centre | | ⏳ no collider changes |
| B_OBJ_Hazard.png | B | void-edge accent | Manual | 14_environment-breakdown | Tiled | 787 × 197 | top centre | | ⏳ visual-only fall-volume accent |
| B_FG_01.png | B | floating shards/mist accent | AutoSprite | 14_environment-breakdown | Simple | 787 × 590 | top centre | | ⏳ optional |

Stage 2 prompt provenance is intentionally blank until an approved asset is generated. The `DesignImages/*.png.txt` files are written style specifications, not images.
