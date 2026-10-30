# Obsidian Void (levels 31–60) · environment asset prompts (2026-10-16, revised 2026-10-23)

**Scope:** the environment for **L031–L060**, the game's second world (D-163, moved to L031 by D-172; L001–L030 stay Golden Ruins). PAX-A20 takes these into Unity. The developer asked for these on 2026-10-16. The source folder is `Art_Source/Environment/ObsidianVoid/` (it was `B`). The traps have their own prompts: `Docs/Art/ObsidianVoid_TRAP_PROMPTS.md`.

**Why:** obsidian, glass, grids, cyan edge light, void, monoliths, wireframes, cold mist, stars (first written for Reality B in `Docs/00_VISION.md`; Reality B gets its own look when co-op is built). Since D-172 the world reads as the remains of an **ancient high-technology civilisation**: machines of obsidian and light, not ruins with plants. These prompts give it the same kit Golden Ruins has. The item numbers mirror Golden Ruins' `ENV-*` (`Art_Source/Environment/GoldenRuins/ENV_ASSET_LIST.md`), so the same tools (`Tools/Art/env_kit.py`) can cut them.

**Not needed (code makes them):** ends, corners and broken edges (W3 cuts them from the fill, `env_bumpy.py`). Also not needed: pillar shafts, caps and bases, because narrow uprights are drawn as stone blocks (2026-10-16).

**Colour rule (D-108, D-172):** cyan is the world's light, and safe things wear it (tethers, grip panels, the door, checkpoints). **Crimson is danger only**, so it appears in no prop or background piece here. Lasers and energy fields wear it (the trap prompts).

**Open:** the cats in this reality. The play-layer stone is kept mid-dark slate so a black cat reads against it. If Reality B's cat is a different colour, check its contrast against OV-10a before painting the rest.

**Status (2026-10-23): every item is painted**, kept and named in `Art_Source/Environment/ObsidianVoid/` (37 `OV-*` files; the
prompts are in this file's git history). `Inspiration.png` and the `B_*` files are references; keep them. Next: PAX-A20 cuts and
imports them.

## How to work

- **Chat:** use one ChatGPT chat. OV-01 (sky) and OV-10a (fill) are painted: attach them wherever an item names them, so light and stone match.
- **Each prompt:** attach the references listed, paste the whole prompt, then check the reject list. Regenerate rather than fixing by hand.
- **Transparency:** "Transparent" means real alpha. If ChatGPT paints a checkerboard, regenerate, or ask for a flat pure magenta #FF00FF background. "On pure black" pieces are glows: the tools turn black into transparency.
- **Phone check:** shrink each result to phone size on screen before saving; the big shapes must still read.
- **Saving:** save to `Art_Source/Environment/ObsidianVoid/` with the exact file name. The `OV-` prefix keeps them apart from `PAX-A18_BACKGROUND_PROMPTS.md`, which uses B, C, D and E as biome letters.

**References:**
- **R1:** `Docs/Art/Reference/styleframe_A_v1.png` (technique only)
- **R2:** `DesignImages/14_environment-breakdown.png` (depth)
- **R3:** `Art_Source/Environment/GoldenRuins/A_GAME_Platform_Fill__raw_v1.png` (block size)
- **R4:** `Art_Source/Environment/ObsidianVoid/B_MG_01_Mid__raw_v1.png` (the old placeholder skyline: content only, not quality)
- **R5:** `Art_Source/Environment/ObsidianVoid/B_GAME_Platform_Fill__raw_v1.png` (the old placeholder fill: value and palette)
- **R6:** `Docs/Art/Reference/B_Door_Exit.png` (the old door: footprint)
- **OV-01:** `Art_Source/Environment/ObsidianVoid/OV-01_sky_v1.png`
- **OV-10a:** `Art_Source/Environment/ObsidianVoid/OV-10a_fill_v1.png`
- **OV-10b:** `Art_Source/Environment/ObsidianVoid/OV-10b_top_edge_v1.png`
- **OV-12:** `Art_Source/Environment/ObsidianVoid/OV-12_wall_edge_v1.png`
