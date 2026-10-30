# PAX-A20 · Biome switch and the Obsidian Void trap set (D-163, D-172)

**Status:** Approved 2026-10-23 (the developer: "Well lets set up everything you need to start level 31+"). Built one at a time: PAX-112, PAX-111, PAX-113, then PAX-A20.
**Phase 1 size:** Full (a per-level-range environment and trap-art selector touches the skin, the trap-art setup and every
level's rebuild).
**Decisions:** D-151 (biomes by level range), D-163 (2) as amended by D-172 (Golden Ruins L001–L030, Obsidian L031–L060).

## 1. Why

L031–L060 wear Obsidian Void. Its world is ancient technology (obsidian, circuit lines, cyan light), so its traps look like
lasers, drones and holograms, not spikes, clouds and arrows (the developer, 2026-10-23). The behaviour is unchanged; only the
art is chosen by biome.

## 2. What to build

1. **Biome by level range.** One table (in a ScriptableObject config under `Assets/_Game/Data`) maps level ranges to a biome:
   Golden Ruins 1–30, Obsidian Void 31–60. `Rebuild All Levels` skins each level from its biome: the environment layers and
   play-layer stone (the OV slots), plus the trap-art set.
2. **Per-biome trap art.** Each biome has its own trap-art set (a second `TrapArtConfig`, or biome entries in it; Phase 1
   decides). The Obsidian set, by element:
   - `Hazard` and the pit floors: an energy field and a laser grid.
   - `HiddenSpikes`: floor emitters.
   - `StormCloud`: a security drone, with its strike drawn as a vertical laser.
   - `FakePlatform`: a hologram that glitches when it gives way.
   - `CollapsingFloor`: glass.
   - `ShrinkingFloor`: retracting hard-light.
   - `FallingBlock`: a monolith slab.
   - `MovingTrap`: an obsidian crusher, piston, elevator and hinged panel.
   - `Geyser`: a gravity vent.
   - `Vine`: a light tether (OV-18).
   - `GripWall`: a lit circuit panel.
   - Gravity flip and inverter: ring glyphs.
   - Door retreat: the gate on its rail.
   - The laser beam, hard-light bar, pulse gate and scanner bodies come from PAX-111 to PAX-113.
3. **Prompts (written 2026-10-23).**
   - `Docs/Art/ObsidianVoid_TRAP_PROMPTS.md` has OT-01 to OT-17, one per trap body above. OT-04 to OT-06, OT-16 and OT-17
     wait for PAX-111 to PAX-113.
   - In `Docs/Art/ObsidianVoid_PROMPTS.md`, OV-20 to OV-23 are v2 tech props (ceiling fixtures, low tech props, holo-banners,
     wreckage and circuit panels) and OV-31 is the grip panel.
   - Colour rule: crimson is danger only (beams, fields, a drone's eye, crushing faces), and cyan is the world's safe light.
   - Raw trap paintings go to `Art_Source/Traps_Obsidian/`.

## 3. Allowed files (Phase 1 confirms)

- `Assets/_Game/Data/` (the biome table config, new)
- The skin and environment setup files, and the trap-art setup and import files (named in Phase 1)
- `Docs/Art/ObsidianVoid_PROMPTS.md`, `Docs/Art/ObsidianVoid_TRAP_PROMPTS.md` (new)
- EditMode tests for the biome selection and trap-art parity per biome
- `Docs/07_DECISIONS.md`, this ticket

## 4. Out of scope

- Any change to L001–L030's look.
- Reality B's look (co-op, frozen).
- New trap behaviour (PAX-111 to PAX-113).
- Generating art beyond the approved prompts (20 AutoSprite credits per asset, CLAUDE.md).

## 5. YOU — UNITY EDITOR

Paint the trap bodies and props from the prompts. Then run the import and trap-kit menus that Phase 1 names, run
`Rebuild All Levels`, and save.

## 6. Phase 1 trace (2026-10-23)

**How a level gets its look today (Golden Ruins only).**
- **Play layer:** `Tools/Art/env_kit.py` (+ `env_bumpy.py`, `env_tiers.py`, `env_v2.py`) cuts `Art_Source/Environment/GoldenRuins/ENV-*`
  into 171 named slots (`ENV_Fill_A`, `ENV_Cap`, `ENV_Side*`, `ENV_Drape_*`, …) in `Assets/_Game/Art/RealityA/Environment/Kit/`
  and `…/Backgrounds/Kit/`, with `env_kit.json` (sizes and alignment offsets). `EnvironmentKit` (Editor) reads that one folder;
  `SoloRoomSkin*` draws every solid from those slot names.
- **Background:** `EnvironmentStackSetup*` builds the layer stack from `LevelLookConfig` (grades) and `LevelLooks` (per-level
  recipes naming `ENV_*` pieces). Unlisted levels use `LevelLooks.Default`.
- **Trap art:** `TrapArtSetup*` dresses every trap from the one `TrapArtConfig` at `TrapArtSetup.ConfigPath`
  (`Assets/_Game/Data/TrapArtConfig.asset`), filled by `PARALLAX/Art/Import Trap Kit` (`TrapKitImport`) from `Tools/Art/trap_*.py`
  output. `SoloRoomSkin` also reads it (line ~399).

**Plan (recommended): a biome is a folder and a config, chosen by level number.**
1. **`BiomeTable`** (pure, `Parallax.Core` or Editor): Golden Ruins 1–30, Obsidian Void 31–60; one function `BiomeFor(levelId)`.
   A table in code with a test, not a ScriptableObject: it changes only with a decision, and no `.asset` is needed (the ticket
   asked for an asset; this is simpler and equally reviewable).
2. **Environment kit per biome, same slot names.** `env_kit.py --biome obsidian` reads `Art_Source/Environment/ObsidianVoid/OV-*`
   (numbered to mirror `ENV-*` on purpose) and writes the same slot names into `Assets/_Game/Art/ObsidianVoid/Environment/Kit/`
   and `…/Backgrounds/Kit/`. `EnvironmentKit` gets a current biome (set by the builder before it skins a level); every
   `SoloRoomSkin` slot lookup is unchanged. Crop constants tuned for the Golden Ruins paintings get Obsidian counterparts.
   Dressing maps by role: drapes → OV-20 fixtures, moss and ferns → OV-21 tech props, banners → OV-22 holo-banners, rubble and
   glyphs → OV-23 wreckage and circuit panels; vine → OV-18 tether; grip moss → OV-31 circuit panel.
3. **Trap art per biome.** A second asset, `TrapArtConfig_Obsidian.asset`, the same class with three new fields for the new
   bodies (pulse gate, beam and ray, scanner). `TrapArtSetup.ConfigFor(levelId)` replaces the fixed path; `Import Trap Kit` fills
   both. `trap_kit.py --biome obsidian` cuts `Art_Source/Traps_Obsidian/OT-*`.
4. **Looks.** `LevelLooks.DefaultObsidian` (a cold "Void" grade and OV sky, mist, far and mid pieces) for every L031+ level
   until per-level recipes are written.
5. **Golden Ruins is untouched:** every L001–L030 scene rebuilds byte-identical (checked on the batch clone).

**Developer steps it will need:** run `Import Trap Kit` and the environment import menu (they create the Obsidian sprites'
`.meta` files and the new config asset), then `Rebuild All Levels`, save, and commit.

**Measured in Phase 2:** tile seams and sizes of each OV/OT slot, the phone-scale read of the new trap bodies.
