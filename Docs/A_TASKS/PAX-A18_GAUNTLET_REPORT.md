# PAX-A18 · Gauntlet report (kept current after every round)

**State:** Stop 2 again (2026-10-17): after D-153 (every edge bumpy), W3's fourth and last round on L002 scored 6/4/5/5
(§6b). The pit back wall's lightness needs a ruling, and the procedural look goes on the art list. Stop 1's assets and credits still wait for the developer. No credits spent, nothing committed. Work runs on the batch clone `scratchpad/a18` (synced to `c26099a`); outputs persist in
`~/Parallax_batch_tools/PAX-A18/`.

## 0 · Pre-flight

- PAX-107 committed and pushed (`c26099a`); the working tree is clean apart from this ticket's docs.
- EditMode baseline: **running** on the clone (`run_batches.py`, results in `~/Parallax_batch_tools/PAX-A18/baseline_runs`).
  Known reds before it: `SurfaceCoverageTests.EveryRoomInScope_Passes` (L012 Block_E, D-129), L001's route (no solution),
  `WallJumpShortcuts_L011`. The fixture list was regenerated (178 classes; new: CleanFalls, HazardContrast, MossSign, …).
- "Before" set: PAX-A16's capture on all 20 levels → `~/Parallax_batch_tools/PAX-A18/before/` (runs after the baseline).

## 1 · Phase 1 trace (Full)

**Where the look comes from today**
- `LevelPalettes.Levels` maps each level to (palette, signature); `LevelLooks.ById` adds a grade, a fill (A/A2/A3),
  a dressing profile and pieces for L001–L010 (L011–L020 use `Default`). Two "signature" notions overlap; only the
  palette's (an enum of 8 mid sets in `EnvironmentStackSetup.Compose.Signatures`) builds anything.
- `EnvironmentStackSetup.Build` → sky plate (`ENV_Sky_<palette>`), sun, `BuildTiers` (halo, rays, motes), `Compose`
  (clouds, `FarSet` of spires/islands, haze, the mid signature set, far lake, pit voids, lights), `NearTier`, `Outside`,
  post volume. Every slot is a sandstone-ruin piece from one painting set (`Art_Source/Environment/GoldenRuins`), so a "new" level
  is a new grade on the same shapes: the defect §0 names.
- Kit: `Tools/Art/env_kit.py` / `env_v2.py` / `env_tiers.py` cut the paintings into slots (`Kit/env_kit.json`, 124
  slots); `EnvironmentKit.cs` loads them; world-tiled materials use `Parallax/2D/Env-Sprite-Lit-WorldUV`.

**Play stone (W3)**
- All SpriteRenderers. Fill: a world-tiled sliced quad per block. Trims (`SoloRoomSkin.BuildTrims`): `ENV_Cap` on tops
  (art reaches `above` = 0.26 u over the line), `ENV_Under`/`ThickUnder` below, chipped side strips, `ENV_Slab`/`SlabEnd`
  for thin slabs, posts, then `FaceShade`.
- D-114's outline lives in **texture alpha**: `env_kit.py chip_strip` (`CHIP_REACH 0.06`), four variants per fill, and
  `SoloRoomSkin.ChipInset` (0.06) / `CheckSideStrips` must agree. Tops are ruler-straight `ENV_Cap` strips.
- P10 for free: disguised and moving traps copy their host's world-tiled skin and carry the host's trims
  (`HostSkin`, `ShownWith`), so whatever outline the host gets, its trap gets.

**Traps (W2)**
- `TrapArt` subclasses (`ArrowArt`, `SpikeArt`, `SolidArt`, `CollapsingFloorArt`, `GeyserArt`, `InverterArt`,
  `FlipArt`, `CloudArt`, `VineArt`, `DoorRetreatArt`, `DeathArt`) draw from one atlas (`TrapKit.spriteatlasv2`) and
  pose everything with `TrapArtMath` functions of `TicksSinceFire` — the determinism LOOK_AND_FEEL §3.4 wants is already
  there; what's missing is material and weight.
- What makes them cheap, by code: spikes are one tile with a whole number of identical teeth (the even picket); the
  launcher is one sprite pasted over the host from the fire tick; the geyser vent is tinted orange as its tell; arrows
  and spears are single thin sprites; impacts are the same three dust puffs everywhere.
- Lethal bodies draw unlit (D-108); `BodyGrowth` gives each body's allowance; `HazardContrastTests`,
  `TrapArtRevealFrameTests`, `TrapArtParityTests`, `TrapArtBudgetTests` (one atlas ≤ 2048) gate it.

**Plan, by workstream**
- **W1:** one table `LevelBiomes`: level → (biome, signature). A biome (`Biome` data class) owns its sky ramp, palette,
  far set, mid band, heroes, near palette, fill + trims (cap, under, slab, ends, side chips), dressing atlas and
  profile. `LevelPalettes.Levels`, `LevelLooks.ById` and the `Signatures` enum go (step 5). Each signature is one
  painted piece placed once (a slot per level). The A17 §4 fixes come along (lit lip + falloff, L013/L018 grades
  removed with the per-level grades, ≥ 3 clumps with no-repeat placement, L010's moon via biome E, whole repeating
  tiles per PAX-V09).
- **W2:** each trap's body is rebuilt from painted parts:
  - launchers from the biome fittings sheet, seated in the host course (the mouth is cut into the stone tile, not
    pasted on; disguised ones keep P10 until the reveal frame);
  - spikes from the material's 4 blade variants, placed per strip by a seeded hash with irregular spacing and height
    (within `BodyGrowth`), on a socket strip;
  - arrows and spears from the new sheet (head, shaft, fletching, the snapped stub that stays and quivers);
  - movers banded with iron from the mechanism sheet;
  - cracks and crumbling edges from the breakage sheet.

  Every phase is a tick function in `TrapArtMath` (tell: glow, creak, grit, hiss; action; impact: dust, sparks,
  chips, quiver; rest). Effects use the AutoSprite sheets, tinted per biome.
- **W3:** see §4.

**Risks**
- 91 paintings: the biome passes can only start as each biome's set arrives. The order is B first (Stop 3), then C, D, E.
- One trap atlas ≤ 2048 (`TrapArtBudgetTests`) with five fittings sets and four blade materials. Pack tight; if it
  overflows, ask before adding a second atlas (one test asserts one).
- Texture budget ≤ 32 MB per level: only one biome loads per level, so it is a per-biome cost (today 9.4–11.8 MB).
- The paw checks don't see art (they measure against colliders), so W3 needs a new capture check (§4).

**Conflicts with the docs (for your ruling)**
1. The ticket names the new decisions D-130 and D-131, but both numbers were taken by PAX-107 (L013 moving floors;
   clean falls). Proposed: **D-151** (biomes as place types), **D-114 amendment 2** (bumpy stone) and **D-152** (trap
   art v2).
2. D-105 says spikes are near-black iron. Per-biome materials (crystal, wood) would amend it; the prompts keep every
   material near-black with a pale edge, so the rule's intent (dark, readable blades) holds.
3. Ticket §1 W3 "never a dip under a paw's rest point": a cat can rest anywhere on a top, so a drawn dip anywhere is
   under some rest point. Proposed reading in §4.

## 2 · Biome-to-level split (proposed)

| Biome | Levels | Pilot | Signatures |
|---|---|---|---|
| **A · Golden Ruins** | L001, L002, L003, L004, L015 | L002 | sun arch · aqueduct (kept) · great tree · colonnade hall · aqueduct pools |
| **B · Sky Isles** | L005, L007, L011, L018 | L005 | banner isle · storm spires · moored citadel · the storm's eye |
| **C · Sunken Grotto** | L008, L012, L016, L019 | L008 | sunken temple · pillar hall · mirror lake · crystal cascade |
| **D · Jungle Falls** | L006, L013, L014, L017 | L006 | twin falls · fall chamber · the canopy · twin stone heads |
| **E · Ember Night** | L009, L010, L020 | L010 | ember field · moon ruins · the machine tower |

- Order: A A A A B D B C E E | B C D D A C D B C E. Every biome appears by L010, no biome runs three in a row, and
  each band ends at night.
- Why each fits the level's new shape:
  - L014 (vines and canopy), L013 (falls) and L006 (chimney beside falls) suit the jungle.
  - L016 and L019 (corridors under a ceiling) and L008 (the maze and sump) suit the cavern.
  - L011 and L018 (cloud chases) and L005 and L007 suit the sky.
  - L017 (mirrored lanes) gets the twin heads.
- Changes from A17: L011 A→B, L015 C→A, L017 C→D, L019 D→C, L013 C→D, L014 B→D, L016 B→C.

## 3 · Trap material per biome (proposed)

| Biome | Blades | Launcher mouth (carved into the host stone) | Vent | Movers |
|---|---|---|---|---|
| A | black forged iron | lion's mouth in sandstone | sandstone ring, wet moss | iron-banded sandstone |
| B | dark oxidised bronze | bird's beak in pale limestone | wind-worn limestone ring | bronze-banded limestone |
| C | black obsidian crystal | eel's mouth in basalt | mineral-crusted fumarole | iron-banded basalt |
| D | charred wooden stakes, rope-lashed | serpent's jaws in granite, roots | spring ring wrapped in roots | lashed timber and granite |
| E | black forged iron | iron beast grille in charcoal stone | cracked stone, amber glow | riveted iron |

Shared across biomes:
- arrows and spears (dark iron, crimson marks in code);
- the inverter orb and cue;
- the storm cloud;
- the jungle liana (biome D's vine; A–C and E keep ENV-18);
- the door retreat (motion only).

## 4 · W3 kit plan (bumpy stone; colliders untouched)

- **Tops** (both gravities, including undersides a gravity-up cat stands on): the cap strip's walk line gets lumps of
  0 to +0.05 u (wavelength 0.4–1.5 u) drawn **behind** the cat, with the front lip edge kept on the collider line within
  ±0.01 u. The silhouette against the sky is bumpy, and paws never look sunk or floating. Dips appear only as
  value (a shaded hollow), never as missing alpha under the line. That is how this plan reads "never a dip under a
  paw's rest point".
- **Side faces:** split by height band.
  - The walk band (top 0.6 u) gets chipped corners, a rounded or broken end and a protruding course within ±0.12 u.
  - Below it, large broken chunks, overhangs and stepped courses, with rubble at the foot.
  - These are separate side-strip slots per band, because one periodic strip can't vary its reach with height.
    `ChipInset`, `CheckSideStrips` and the fill inset become per-band.
- **Undersides no cat stands on** and stone outside the room are free: broken, overhanging, with roots.
- **Thin slabs:** irregular, thicker at one end, a cracked underside (painted per biome, cut by code).
- **Grip walls:** as side faces, with the moss still the grip sign.
- **Checks:** the capture's `paws_in_floor` / `float_gap` measure against colliders, so they can't see the art. I'll add
  `drawn_top_vs_paw` to `CatCaptureChecks`: per frame, the drawn stone's alpha line at the paw columns versus the paw
  row, with the same 2 sp threshold. It runs on every ground scenario and every `measure_L0NN_down`, in both gravities.
- If two critic rounds still read "straight": **stop** and set out real polygon collider tops (every validator, route
  and pin re-run), never without a ruling.

## 5 · Assets and credits

The full list with paste-ready prompts is in `Docs/A_TASKS/PAX-A18_ASSETS.md`:
- 91 ChatGPT images: A 5, B–E 13 each, 19 signatures, 15 trap bodies.
- AutoSprite: 5 effect statics, **≤ 10 credits** (estimate 5).

## 6 · Rounds (W3 pilot, L002)

Every round first passed the measured checks on the clone:
- `MossSignAndBrokenStoneTests`, `EnvironmentKitImportTests`, `WorldTileSkinTests`, `EnvironmentReadabilityTests`, `TrapArtRevealFrameTests`, `HazardContrastTests`, `LevelLookConfigTests`, `TrapArtParityTests`: 226 of 227 pass. The one failure is `Contrast:L008` (Hinge_S2_Spikes at 28%), which fails the same way on the build before W3.
- Cat-capture paws, 22 ground and air scenarios in both gravities: 0 frames over.
- Budget on L002:

  | Measure | Before | After | Limit |
  |---|---|---|---|
  | SetPass | 30 | 30 | 30 |
  | Draws | 178 | 187 | 130 |
  | Environment textures | 22.1 MB | 26.3 MB | 32 MB |

  Draws were already over the limit before W3.

| Round | Build | Readability | Variety | Material | Wow | Main finding |
|---|---|---|---|---|---|---|
| 1 | r7 (strips with their own stone) | 7 | 4 | 4 | 4 | side lumps look pasted (texture tears); tops straight |
| 2 | r8 (edge masks on the fill, EdgeMask shader) | 7 | 4 | 5 | 5 | main top unchanged (a bug: whole tops went flat near any hazard); corners square; chips black |
| 3 | r10 (bug fixed, corner ends visible, lit chips, pit void under walls) | 7 | 4 | 4 | 4 | tops still straight; side jag reads procedural (even sawtooth) |
| 4 | b27 (D-153: every edge bumpy; pit back walls; spike bed) | 6 | 4 | 5 | 5 | pit side faces now "properly broken"; the pale back wall reads as solid fill, not a drop; bumps read as one repeated shape; floating slabs and spike-bed slabs still boxes |

Measured fixes made between rounds:
- Three contrast regressions (L011 Pit_Foot, L015 Pit_2, L019 Thorns_0) fixed by keeping D-114's own strips beside hazards and flat tops over hazards.
- White slivers in pit walls fixed by widening the pit voids under the walls.

**Escape hatch (ticket §1 W3): triggered.** The tops read "straight" in all three rounds. ±0.05 u is about 7 capture px (4 phone px), and the
critics read it as zero. Options for the developer's ruling are in §7.

## 6b · Round 4 (2026-10-17, after D-153)

Measured before the critic (build b27, clone):
- **Straight-edge audit** (`tools/edge_audit.py`, runs of 0.5 u or more, L001–L020): 1155 → 51. Most of the 51 are lines
  painted inside the stone, moss carpets and translucent decoration; about 8 are small cut edges.
- **Hole audit** (`tools/hole_audit.py`): 2 holes, each under 70 px.
- **Tests:** 315/315 for the touched fixtures (contrast, trap size and parity, environment, moss, world-tile) on b21. They
  weren't re-run after b22–b27, which changed only depth order, step size, back-wall pieces and noise.
- **Budget on L002:** SetPass 40 (limit 30) and draws 261 (limit 130; 187 before W3), so **over**. The main cost is the trims
  under trap solids, kept so a face is broken when its trap leaves, about 78 draws on L003. Drawing them only once the
  trap has gone needs a runtime change to the trap art: a separate performance ticket.

**Round 4 verdict** (`critic/w3_r4/verdict.md`):
- Readability 6, Variety 4, Material 5, Wow 5.
- **Better:** the pit side faces now look properly broken, and the spikes read much better than the old picket.
- **Defects, ranked:**
  1. The pit back wall is pale and lit like the foreground, so the pits read as solid fill, not drops. This conflicts with
     the developer's ruling (lighter, softer back wall); it needs that ruling revisited.
  2. The bumps read as one repeated procedural shape: the ceiling scallop, the back wall's top wave, evenly spaced joints.
  3. Floating slabs and spike-bed slabs are still boxes.
  4. Hard steps at the pit lips.
  5. The main floor's walking line still reads near-straight at phone scale.

**Loop state:** W3 has used its four rounds (ticket §2). Defect 2 (procedural bumps) has been named in rounds 3 and 4 with
code fixes in between, so per §2 it goes on the art list: painted broken lips, slab ends and corner chunks per biome (the
top-lip paintings, option 2 below). Defect 1 needs the developer's ruling. Stop 2 again.

## 7 · Stop 2: options for the tops

1. **Real polygon tops (colliders).** Bumps of about ±0.1–0.15 u on walkable tops.
   - Cost: every validator, route, route pin and camera tell re-run on 20 levels.
   - Risks: CatMotor2D grounding on non-flat edges (slope handling, D-052), the reach model, and determinism checks on seams.
   - Size: a separate engineering ticket.
2. **Painted tops, no collider change.** The per-biome top-lip paintings (B-11, C-11, D-11, E-11; A gets one) carry an uneven painted lip and tufts.
   - The code adds denser tuft and moss clumps along every top, drawn behind the cat, and chipped lip stones.
   - Within the ±0.05 rule. Cheapest. Lands with W1.
3. **A wider drawn allowance for tops**, for example +0.12 u drawn behind the cat, with the lip edge on the line. A rule change to the ticket's ±0.05.

Recommended: 2, with 3 if the developer wants the lumps to show before the paintings arrive.

## 6c. Play-test fixes, 2026-10-17 (build b32, D-153 amendment 1, Proposed)

The developer, after playing L007: "sometimes it doesnt match the color of the tile it is attached to"; a weird empty spot
at the top left; "when things fall from the ceiling everything is straight. Whatever falls needs to crash and look like a
pile of rubble".

- **Colour seams.** A new check renders every level with each kit texture flattened to its mean colour, so a straight
  colour step is a tint or shade seam (`tools/flat.sh`, `seam_audit.py`, filtered by the real render). Causes found: each
  solid's own shadow ramp (a trap floor 3 u tall beside 4 u of ground was a darker box), two walls overlapping in L007 with
  different shading (a navy rectangle), and the navy shadow, which no multiply on a strip can match. Fix: one shadow map per
  room, read by the stone's shaders (`StoneShade`); the face shadow is now neutral dark. L007's visible seams went from 168
  to 95; most of the rest are the painted lit top course and underside strips, by design.
- **Falling blocks.** From its landing tick a falling block lies as a heap of broken stone cut from the room's fill; the hole
  it leaves has broken sides and hanging stones. Colliders unchanged.
- **The empty spot.** Not reproduced: the developer's saved Level_007 and a replay of its route both show sky there.
  Waiting for the object's name or a fuller screenshot.
- **Checks.** Straight runs 53 (was 51). Tests: the 13 trap and environment fixtures, 305/315 at b31 (the 10 failures were
  the parity test meeting the heap); parity, build and reveal-frame fixtures 138/138 at b32. L003 draws 277 → 232; SetPass
  33 (over 30); environment textures 32.0 MB (at the 32 MB limit).

## 6d. Second play-test round, 2026-10-17 (build b38, D-153 amendment 2 and D-154, Accepted)

- **Lances (D-154):** the developer's painted spear (PILOT-02D_spear_v1) is drawn whole, 1.4 x 0.26 u, everywhere. Hitboxes
  are kept. The wood is darkened to 0.65 and has a 4 px rim. A sunlit band along the top of the shaft becomes a thin steel
  glint on the collar and blade. Without the band, L019 Spear_1 read 28 % at 3:1 over the dark drapery; with it, every
  level passes.
- **Bumps:** about 40 % smaller (reach 0.08, deep 0.18, top lump 0.05). No edge stands proud of its collider.
- **Inverter orb:** 0.78 u (was 0.6).
- **ShownAtRest:** the stone a trap lends its neighbours stays behind when the trap moves (L012 Block_E, the L016 slams).
- **Vines:** every vine reaches the stone over it. The reach is environment art beside the vine, not part of it, with a
  second anchor when the gap holds one. The vine's own renderers stay as built, because the route harness reads them
  (TrapArtBuildTests caught the first version, which grew them).
- **Slams that stay down** lie as rubble.
- **Levels:** L012 Spikes_D 0.3 u west; L015 Spear_10 removed; L016 left ledge 0.4 u lower.
- **Tests (batch, on the clone):**
  - The 13 art fixtures: 314/315 at b34. The one failure was the L019 spear contrast, fixed at b38; contrast and size are
    24/24 there.
  - Level fixtures (ValidateRoutes, Band2RouteResults, Band2Level, WallJumpShortcut, CleanFalls, LevelLayout): 97/104 at
    b33.
    - Three route pins were updated for the layout changes: L012 (window 44 -> 45, Spikes_D lead 13 -> 15), L015 (1263 ->
      1238 ticks, Spear_10 gone) and L016 (window 14 -> 15). They pass at b33.
    - The four remaining failures are pre-existing and wait on the developer's rulings: L001's solution under Ledge, L011's
      dead ends / Ledge_M / NW, L011's wall-jump findings, and L012's Block_E rules.

## 6e. Visual QA and clean-up of L001–L005, 2026-10-17 (builds b39–b41, D-153 amendment 3)

- **Review:** five reviewers (one per level, run one at a time) went over captures and a grid of tour shots, at rest and with
  traps fired, at phone zoom. Report: `~/Parallax_batch_tools/PAX-A18/review1/REPORT.md`. A re-check compared old and new.
- **Fixed:**
  - pale pit walls (then, at b40, a too-dark felt look that hid the cat);
  - the pit lip above the floor line;
  - the bottom-of-screen band;
  - L001's buried lit course;
  - L003's lit strips under the collapsing floors (a trap tell);
  - L003's corridor backing band;
  - L004 Roof_4's floating posts;
  - L004/L005 sky slots in the outer walls;
  - dark rubble heaps;
  - chartreuse grip moss;
  - pink plaques;
  - pale launcher frames;
  - straight underside seams;
  - stamped ivy;
  - flat-topped landed grip pillars (L001).
- **Left:**
  - L004's ceiling spikes touch the door's keystone (the door art draws at its sprite's size);
  - L005's side-wall top against the masonry, and its slab ledges in the slab texture (both by design);
  - L003's shadow columns under the launcher towers (the column shade rule; changing it affects every level);
  - L004's mist columns over the background pillars;
  - the spikes' sticker look (D-108's readability style);
  - saturated blue banners.
- **Tests:** the 13 art fixtures pass 315/315 at b41. No layout, route or gameplay code changed.

## 6f. Launchers, pits and the L006–L010 visual QA, 2026-10-17 (builds b42–b47, D-153 amendment 4)

- **Review:** five reviewers, one per level, run one at a time. Report: `~/Parallax_batch_tools/PAX-A18/review6/REPORT.md`.
- **Fixed:**
  - square launchers;
  - pits too dark, and then, at the lighter tone, flat with no depth;
  - roof and ceiling holes showing sky (L006–L009);
  - L008's sky strip, sky window and haze edge;
  - L010's lavender ceiling strip;
  - L009's floating cap chunks;
  - grey slabs (an L006 tell);
  - box-like heaps;
  - the scenery-like storm cloud.
- **Left (art or design, not code):**
  - long uniform spike rows (a gap would lie about a lethal row);
  - background sprites' halo mattes;
  - L010's moon blob;
  - L008's door halo hue;
  - L007's near pillar seen through slabs;
  - mid-face seams and straight cut ends case by case;
  - L007's spike strip spanning a gap after a drop (a layout question).
- **Tests:** the 13 art fixtures plus RoomDeathHoldTests pass 326/326 at b47.
