# PAX-A20 · Biome switch and the Obsidian Void trap set (D-163, D-172)

**Status:** Draft 2026-10-23, waiting for the developer's approval. Not built.
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
