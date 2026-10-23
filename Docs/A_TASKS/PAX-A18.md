# PAX-A18 · Visual gauntlet 2: place types, trap rehaul, bumpy stone

**Status:** Approved brief for a gauntlet run (the developer, 2026-10-15). Decisions it creates are written as
**Proposed** and ruled at the stops.
**Folder:** `Docs/A_TASKS/` · **Phase:** E, look and feel · **Implementer:** Claude Code (gauntlet loop, batch-mode clone)
**Phase 1 size:** Full (three workstreams, new art, a kit change).

> **Progress (2026-10-16):**
> - **Stop 1:** the plan is in `PAX-A18_GAUNTLET_REPORT.md`.
> - **Stop 2 (W3 on L002):** reached. The tops' ruling is pending; option 2, painted lips, is under way via the pilot's
>   top lip.
> - **The pilot is painted:** `PAX-A18_PILOT_PROMPTS.md`, 19 trap and object paintings plus the Sky Isles sky, fill and isle.
>   It replaces most of the trap list.
> - **Next:** the 30 background paintings for variety, in `PAX-A18_BACKGROUND_PROMPTS.md`. They supersede the B–E background
>   entries in `PAX-A18_ASSETS.md`.
> - Ends, slabs and undersides are no longer painted: the W3 code cuts them from each fill.
> - **D-151 (the developer, 2026-10-16):**
>   - Biomes go by level range: L021–L040 are Ember Night, L041–L060 are Sky Isles, and so on. L001–L020 keep today's look.
>     **Superseded by D-163 and D-172 (2026-10-23):** L001–L030 are Golden Ruins and L031–L060 Obsidian Void; the other biomes
>     take later ranges.
>   - W1 and the gauntlet loop are paused.
>   - **Now:** W2 only. The painted traps (`Art_Source/Traps_ChatGPT/PILOT-*`) go into L001–L020, each kind at one fixed
>     size everywhere.
**Depends on:** PAX-A16 (D-103, D-105, D-107), PAX-V08 (D-108), PAX-V09 (whole repeating tiles), PAX-106 (D-114 broken
stone), PAX-107 committed. Supersedes PAX-A17's draft (its biome table and open items are folded in below).

## 0. Why (the developer, 2026-10-15)

> "It did a pretty good job with the backgrounds, but now I need a bit more variety. I need different types of
> backgrounds, not just colours. I need new generation of assets. The traps look very cheap, I need a massive rehaul of
> them. The floors and walls look all straight, no floor is like that. I want them bumpy."

What the current frames show (L004, L006, L008, L010 play-test screens; `Docs/Art/Traps/contact_sheet.png`):
- **Backgrounds:** every level is the same place (sandstone ruins, ivy, colonnades) under a different sky. L010 is L008
  at night. The variety is in the grade, not in the shapes.
- **Traps:**
  - the arrow launcher is a small square icon with a red fill pasted on the stone;
  - arrows and spears are thin vector-looking strokes;
  - spikes are a uniform black-and-white zigzag picket that reads as UI, not iron;
  - the geyser tell is a flat orange blob;
  - nothing has weight: no anticipation, recoil, dust or debris that belongs to the material.
- **Stone:** every block is a rectangle. D-114's ±0.06 u chipping doesn't read at phone scale. Tops are ruler-flat,
  ends are cut square, slabs are boxes.

## 1. Workstreams

### W1 · Background variety: biomes are place types, not palettes

A biome is a **different kind of place**, with its own silhouettes at every depth, its own stone, its own dressing and its
own light. A palette change alone never counts as a new biome.

| Biome | What kind of place | Far / mid silhouettes | Play stone and dressing | Light |
|---|---|---|---|---|
| **A · Golden Ruins** (today's L002, keep) | open-air temple ruins | spires, aqueducts, colonnades | sandstone, ivy, banners | low warm sun |
| **B · Sky Isles** | high above the clouds, nothing below | floating islands, chains, a cloud sea under the walks, far isles | weathered pale limestone, roots hanging under every island, wind-bent grass | dusk rose → violet |
| **C · Sunken Grotto** | inside a cavern, ceiling of rock | stalactites, cave mouths, an underground lake, light shafts from cracks above | wet dark basalt, glowing fungi and moss, dripping water | cool shafts + warm fungus glow |
| **D · Jungle Falls** | a canyon in dense jungle | giant trunks, cliff walls, waterfalls, mist | mossy green-grey granite, roots, broad leaves | cool morning haze |
| **E · Ember Night** | ruins at night with fire | moonlit towers, smoke, distant fires | dark stone, braziers, embers | cool moon key, warm braziers and door |

- Level assignment: start from PAX-A17 §3 (adding C, renaming its Misty Falls to D) and propose the split at Stop 1. Every
  biome appears by L010.
- **Each level = its biome + one signature landmark**, placed once by hand (PAX-A17 §2).
- 20 per-level palettes and looks (`LevelPalettes`, `LevelLooks`) become a biome id plus a signature per level.
- The fixes PAX-A17 §4 listed come along: the floor's evenly lit face, L013's olive sky, L018's grade, repeated ferns,
  L010's moon, atlased tiles.

### W2 · Trap rehaul (every lethal and trick element)

Scope: arrow launcher and arrow (straight, angled, mirrored), spear, spikes (floor, ceiling, hidden, sweep), falling
block, crusher/mover, collapsing floor, fake platform, shrinking floor, drop-and-return, push wall, geyser vent and column,
inverter orb and cue, storm cloud, vine, door retreat. Each one gets:
1. **New body art** that looks like a real object made of a real material (forged iron, bronze, carved stone, rope, wood),
   lit like its biome.
   - **Integrated into its host:** a launcher is carved into the masonry (a beast mouth or slot in the stone course), not a
     tile pasted on it; spikes sit in a stone or iron socket strip.
   - **Variation:** spikes use 3–4 blade variants with irregular spacing and height inside the hitbox allowance, never an even
     picket.
   - **Per biome where the material differs:** iron in A/E, bone or crystal in C, wood stakes in D, for example. Propose
     it at Stop 1.
2. **Animation with weight**, as frames that are a pure function of ticks (LOOK_AND_FEEL §3.4):
   - a tell: anticipation (the mouth glows, the slab creaks and sheds grit, the vent hisses);
   - the action;
   - the impact: dust, sparks, debris, an arrow quivering where it sticks;
   - the at-rest state.
3. **A death read** where the trap kills: the frozen hold shows clearly what killed the cat (LOOK_AND_FEEL §3.5).

**Hard rules, unchanged:**
- D-108: crimson for lethal parts only; dark outer line and light inner rim; `HazardContrastTests` 3:1 on 30% of pixels,
  every level and every pose. The PAX-A17 trial "lit with a minimum brightness" is allowed only if this still passes.
- P10: a disguised trap is pixel-identical to its host until its reveal frame (`TrapArtRevealFrameTests`), now in every
  biome's stone.
- D-052 and D-105: hitboxes never move. Drawn growth only in the forgiving direction (`TrapArt.BodyGrowth`).
- Unlit lethal bodies (D-108) unless the trial above passes.

**Generation:**
- AutoSprite via `Tools/Art/autosprite_pipeline.py` and `trap_specs.json` (style blocks M/B and the animation negative),
  at most 20 credits per asset.
- **No credits are spent before Stop 1 approves the total.**
- Static bodies the developer prefers to paint in ChatGPT go on the Stop 1 asset list instead.

### W3 · Bumpy stone (amends D-114)

No surface the cat can see reads as a straight line. Today's rule (±0.06 u on side faces, tops unchanged) is too small to
read, so it is replaced. **Default, visual only, colliders untouched:**

| Part | Allowed outline deviation from the collider | Why |
|---|---|---|
| Walkable top (both gravities: undersides a gravity-up cat stands on count as tops) | lumps and dips within **±0.05 u**, wavelength 0.4–1.5 u, never a dip under a paw's rest point | paws must still look planted |
| Side faces in the walk band (top 0.6 u of a face) | **±0.12 u**: chipped corners, a rounded or broken end, a protruding course | the cat touches these |
| Side faces below the walk band, undersides no cat can stand on, and all stone outside the room | **free**: big broken chunks, overhangs, stepped courses, rubble at the foot, hanging roots | unreachable |
| Thin slabs | an irregular slab, thicker at one end, a cracked underside | not a box |
| Grip walls (D-110 amendment 4) | as side faces; the moss stays the grip sign | unchanged |

- **Measured, not judged:** the cat capture's `paws_in_floor` and `float_gap` (HARNESS.md) must stay inside their
  thresholds on every ground scenario and on every level's solution replay (`measure_L0NN_down`). Tops are checked in
  both gravities.
- Fills, lips and edge pieces are made per biome (W1's stone).
- The same rule applies to real and disguised blocks (P10).
- **If visual-only can't look bumpy enough** (the critic still reads "straight" after two rounds): stop and set out the
  option of real polygon colliders on tops, with its cost (every validator, route and pin re-run). Never do that without
  a ruling.

## 2. Process (lean: the PAX-V07 gauntlet was too slow and token-heavy)

- **Where:** a batch-mode APFS clone, as in PAX-A16 §10 and `PAX-V07_gauntlet/HARNESS.md`. Unity MCP is not required.
- **What you never do:** commit, run setup menus on the main project, touch the main project's scenes, or spend credits
  before Stop 1.
- **Checks before critics.** Every round first runs the cheap measured checks:
  - `EnvironmentCapture` (§2 numbers of PAX-A16), `HazardContrastTests`, `TrapArtRevealFrameTests`, `WorldTileSkinTests`;
  - the cat capture paw checks (W3);
  - the budget (SetPass ≤ 30, draws ≤ 130, environment textures ≤ 32 MB).

  A critic only sees a build that passes them.
- **One critic per round, not four.** A fresh sub-agent with no history. It sees:
  - the phone-scale captures (2400 × 1080, 2D lights on, Android Medium quality, PAX-A17 §4);
  - the concept images (`Docs/Art/Reference/styleframe_A_v1.png`, `DesignImages/14_environment-breakdown.png`, `DesignImages/15_premium-mobile-ui.png`).

  It scores Readability, Variety, Material/craft and Wow out of 10, and lists defects, ranked. It never sees the code.
- **At most 4 rounds per workstream pilot.** Stop early when every score is ≥ 7 and Readability ≥ 8, or when the same
  defect is named in two rounds with no fix in code (list it as an art need).
- **Variety test (W1), measured:** each biome's pilot is shown as a 25% **greyscale** thumbnail beside the other biomes'.
  The critic must name which kind of place each is, with no colour to lean on. A biome that's only identifiable by colour
  fails.
- **"Cheap" test (W2):** the critic judges each trap at phone scale, from a strip of before / tell / action / impact /
  rest in two biomes. It answers: "Does it look like a real object with weight, made for this world, or like a placeholder
  or icon?" Pass = "real" for every trap.
- **Test runs:** only the affected fixtures during the work; the full batch suite once at the end (CLAUDE.md).

## 3. Steps and stops

0. **Pre-flight.**
   - PAX-107's open rounds are committed (the working tree is clean apart from untracked tooling).
   - Record the EditMode baseline.
   - Run PAX-A16's capture on all 20 levels as the "before" set.
1. **Phase 1 trace and Stop 1 (asset and credit plan).** Post:
   - the trace;
   - the biome-to-level split;
   - the per-biome trap material plan;
   - the W3 kit plan;
   - **the full ChatGPT asset list**: one entry per image, with file name, aspect, transparency, tiling axis, the biome, and
     a paste-ready prompt in the PAX-A16 §5 format (attach the three references, the environment style header, the
     prompt, the strict-side-view suffix). Every biome needs at least:
     - a sky plate;
     - a far band;
     - a mid band;
     - 2–3 mid hero pieces;
     - the play stone (fill, top lip, broken end and corner pieces, slab, underside);
     - 3+ dressing clumps;
     - 1 signature per level;
   - **the AutoSprite plan**: per trap, the assets, credits per line and the total.

   Then **stop** until the developer paints the images (saved to `Art_Source/Environment/<biome>/`) and approves the
   credits.

   **No biome is built from today's stills recoloured: the point is new shapes.** W3's code (outline generation and its
   checks) may start during the wait, on today's stone.
2. **W3 on biome A** (L002), checked, then **Stop 2** for the developer's verdict.
3. **W1, biome by biome:**
   - build the biome and run the loop on its pilot (A: L002, then one pilot each for B–E);
   - apply it to the biome's levels with one shared check pass;
   - **Stop 3** after the first new biome (B), so the direction is confirmed before C–E.
4. **W2:**
   - generate, process (`trap_process.py`, `trap_bodies.py`), wire and run the loop on the Trap Lab plus one level per
     biome;
   - **Stop 4** with the new contact sheet (`TrapContactSheet`, every trap in every biome at phone scale).
5. **Finish:**
   - remove the per-level palettes and looks;
   - run the full suite;
   - write the report `Docs/A_TASKS/PAX-A18_GAUNTLET_REPORT.md`, updated after every round so it's current if the run
     stops;
   - write the decisions as Proposed: D-130 biomes as place types (amends D-103's per-level palette), a D-114
     amendment (bumpy stone), D-131 trap art v2;
   - write the developer's Editor steps (setup menus, Rebuild All Levels, Trap Lab setup, what to commit).

## 4. Out of scope

- Gameplay, layouts, colliders (unless ruled at W3's escape hatch), triggers, routes, timing, the camera (1.8×).
- The cat's sprites and animation.
- UI.
- Reality B.
- New Unity packages.
- Device validation (the developer's).
