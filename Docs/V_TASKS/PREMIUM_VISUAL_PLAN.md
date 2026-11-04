# PARALLAX · Premium visual plan (draft, 2026-11-02)

**Status:** Phases 1 and 2 done (D-301, D-303, D-304). Phase 3: Packs A, B and F in (D-305, D-306); decisions in §8 still open. Ticket numbers are given on approval.
**Goal:** every area at 9 or above, every level rated Strong, no doubt it's a premium game.
**Evidence:** the 2026-11-02 review of L001–L040 (604 sweep frames, 551 route frames, sprite dumps).

## 1. Scorecard

| Area | Now | Target | What 9 means |
|---|---|---|---|
| Title screen | 9 | 9.5 | Unchanged look, with motion approved on a phone |
| Menus | 8 | 9 | Chapter art in level select, polished results screen, settings, no text flaws |
| Painted backgrounds | 8.5 | 9.5 | No two neighbouring levels share a composition; each chapter has its own skyline |
| Golden Ruins play layer | 6 | 9 | Stone looks lit by the level's sky; no flat pieces; no screen of plain wallpaper |
| Traps | 7 | 9 | One art style, one danger language, weight in every move |
| Obsidian play layer | 5.5 | 9 | Walls read as dark glass and machinery, never tile; platforms always read |
| Consistency | 5 | 9 | All gates in §2 pass on all 40 levels |

Scores are judgment calls, so the developer has the final say, on the phone. The gates below are the checks that come before that.

## 2. Gates: a level is Strong only when all pass

1. **No flat piece:** every moving or solid piece has edge, shade and dressing that match its neighbours.
2. **No hidden trap tells:** a disguised piece matches its host (P10). Some don't today, see P1.1.
3. **Lit by its sky:** stone, moss and cat take colour from the level's light. The night levels aren't sunlit.
4. **No wallpaper:** no real gameplay frame is more than 50 % repeating wall without a feature in it.
5. **A hero moment:** one composed view that would work as a store screenshot.
6. **Readable:** the cat, every standing surface and every hazard read at phone size. All `HazardContrastTests` pass, including the 14 failing today.
7. **Motion:** something moves in every view besides the player (clouds, vines, dust, water, machinery).
8. **Phone:** holds the PAX-V03 budget on the Pixel 8a.
9. **Clean captures:** no defect in a full route replay, solution and betrayals, at 4:3, 16:9 and 20:9.

## 3. Rules for all of this

- **Presentation only.** No collider, trigger, tick, route or layout changes. If art needs one, stop and ask.
- **Rebuild, never hand-edit:** everything goes through setup menus and `Rebuild All`, on a clone first.
- **One pilot first.** New looks go on the pilot levels and get approved on a phone before rollout (L001, L011, L023, L039).
- **Standing rules stay:** no clouds, no glitch walls, no pop spikes, nothing in front of the play path, thorns never move.

## 4. Root causes found

| Defect | Cause | Fix type |
|---|---|---|
| Flat crates, posts, plugs | **Fixed (D-301).** D-300's mover shade flattened them; now a tint on free-standing movers only | Code |
| Grey ceiling strip, mist on wall feet, flat side columns | **Fixed (D-301).** Stone shadows blended to one colour; they multiply now | Code |
| Comb at the level floor | **Fixed for the sampled levels (D-301).** Ground now fills to the room's bottom. L003, L007, L021 and L025 still have some strips reaching the view's bottom: check them | Code |
| Black pit with a lit floor | L010: traced to the pit's depth shade and a lit floor under it. The earlier "tan boxes" reading was wrong. Belongs to P2.1 night stone | Code |
| Stone doesn't match the sky | Stone keeps one tint at 0.90, 0.87, 0.82 in every traced level. L011–L020 are locked to the default look by `BandTwo_UsesTheDefaultLook` | Code + decision |
| Wallpaper | One fill per room by rule (keeps disguises identical), and no large wall features | New art |
| Plain moments | `PAX-V06` game feel (dust, squash, impact, death and checkpoint beats) is still on the roadmap | Code + FX art |
| Unknown phone cost | `PAX-V03` budget never measured; no device session has happened | Developer + phone |

## 5. Phases

Each phase ends with fresh captures, the touched test fixtures, and the developer's look on a phone or in the Editor.

### Phase 0 · Groundwork

- **P0.1 Phone baseline (PAX-V03).** Measure L001, L023 and L040 on the Pixel 8a. This sets how much light, foliage and FX we can afford, so it comes first. Needs the developer and the phone.
- **P0.2 Capture rig.** Add hero frames and 4:3/16:9/20:9 runs. Re-pin the short replays (L004, L005, L024, L025, L027–L029, also fixes `ValidateRoutes` coverage if they're stale). Ask before running it across all levels.
- **P0.3 Decisions.** Record §8 as a new D-entry, and lift the locks the plan needs (`BandTwo_UsesTheDefaultLook`, approved grades).

### Phase 1 · Remove every defect (no new art)

- **P1.1 One stone skin for everything:** done (D-301).
- **P1.2** Grey strip, comb strip and L001's pale patch: done (D-301). The remaining strips are correct: they hang under real floating pieces.
- **P1.3** The grey veil was the face shadow: done (D-301). `ViewEdgeShade` calmed: done (D-303).
- **P1.4** Orbs calmer; Obsidian door glow cold: done (D-303).
- **P1.5** `Parity:L024` fixed. Contrast: 41 hazards down to 28 (D-303); re-measure after Phase 2.
- **P1.6** "No deaths": done (D-303).

**Exit:** Consistency 7, Golden Ruins play layer 7.

### Phase 2 · Light and colour (the biggest quality jump)

- **P2.1 Sky-matched stone:** done (D-304).
- **P2.2 Contact shadows:** the door is done (D-304). Dressing, trap mounts and wall feet move to Phase 4.
- **P2.3 Cat:** rim and Obsidian light done (D-304).
- **P2.4 Sky arc:** done (D-304). §7's sky column is superseded by `LevelPalettes.Levels`.
- **P2.5 Obsidian light:** moved to Pack F, as it needs new wall art.

**Exit:** Golden Ruins play layer 8, Obsidian 7, Consistency 8.

### Phase 3 · New art (prompts from Claude, painting per the art route in §8)

| Pack | Pieces | Lifts |
|---|---|---|
| A. Hero walls | 8–10: collapsed sections, carved reliefs, window arches, root-choked walls, great doors | Wallpaper (Golden Ruins) |
| B. Slab undersides | 4 variants + ends: corbels, broken courses, roots | Torn-paper edge |
| C. Foliage | 6–8: leaf clusters and vine masses, matching the styleframe | Styleframe richness |
| D. Machinery | 6: press face, hatch frame, piston head, lift frame, launcher, steam vent. Honest pieces only; disguised ones keep their host | Traps |
| E. Trap refresh | Spear, orbs, door variants per chapter | Traps |
| F. Obsidian kit | 2 wall materials, seam trim, thicker platforms, 4–6 hero machines | Bathroom tile |
| G. Finale vistas | Golden Ruins: Pack B landmarks (`Docs/Art/PREMIUM_PACK_B_PROMPTS.md`). L040 backdrop still to write | Band endings |
| H. Cat | C-17 and the most visible gaps from `NEEDED_ASSETS.md` | Character polish |
| I. FX | Dust, debris, sparks, impact and door burst sheets | Weight |

A, B and F are in the game (D-305, D-306), and so is A2, with four more hero walls (D-311). Packs E (spear, two district doors) and I (dust, debris, sparks, impact, door burst, Obsidian shards) are in (D-312); Pack H is the cat list in `Docs/Art/CAT_AND_STORE_PROMPTS.md` (C-10 first); L040's backdrop is Pack G (`Docs/Art/PREMIUM_PACK_G_PROMPTS.md`), waiting on its painting. Phase 4's first pass is built (D-307); §7's individual hero moments are still open. Each pack is piloted on one level before more is painted (thorn round 1 taught us that).

**Exit:** Golden Ruins 8.5, Traps 8.5, Obsidian 8.

### Phase 4 · Level-by-level art direction

Work through §7 one level at a time: hero moment, wall features, dressing and skyline, then a capture check against §2. L011 and L010 go first because they sit at the paywall.

**Exit:** Backgrounds 9, Golden Ruins 9, Obsidian 9, every level Strong in stills.

Built (D-307–D-311). Pack A2's four hero walls are in (D-311), which covers the bare masonry in L022, L025 and L030. Still open: contact shadows on trap mounts, and the Heart of the Void in L031–L039 once the developer has seen it in L040.

### Phase 5 · Motion and feel

- **P5.1 `PAX-V06`:** squash, dust, camera impulse, death and checkpoint beats. Built (D-312), waiting on the developer's hand tuning.
- **P5.2 Living world:** vine sway, cloud drift, shimmer, falling leaves, birds, water. Built (D-313): leaves and motes in the air; the background already moved; no birds or water (rulings); ivy doesn't sway (centre pivots).
- **P5.3 Trap weight:** dust on impact, debris on collapse, sparks on metal, every reveal readable. Built (D-312, D-313): rings, chips, sparks and shards where a heavy mover stops.

**Exit:** Traps 9, Consistency 9.

### Phase 6 · Menus and title

- Level select in chapters with their own art, a results beat with the cat, settings (Roadmap Decide 2), and a transition between screens. Built (D-314), not yet compiled or seen: chapter tabs over each district's landmark, Settings from the title, the cat on the results plate, Level Select's reveal.

**Exit:** Menus 9, Title 9.5.

### Phase 7 · Final proof

- Full capture of all routes on all levels, and every gate in §2.
- One whole-game session on the Pixel 8a, then the store screenshots (`PAX-A10`).
- The developer scores all seven areas. Anything under 9 goes back to its phase.

Toward 9 (D-316): the thorn halo, the menu fixes and Obsidian's fog are in. What's left needs Pack J (`Docs/Art/PREMIUM_PACK_J_PROMPTS.md`): a bolder briar (HZ-01 v4), a bolder metal spike row (HZ-02 v3), four Obsidian skies (OV-01 b–e) and a streaked mist (OV-03d).

Run on the work copy (D-315): gates 2, 3 and 7 pass everywhere. Gate 6 fails in 12 levels (spikes and thorns, waiting on D-295's paintings). Gate 9 covers solution routes at 20:9 only. Gate 8 and the scores are the developer's.

Consistency round (D-320): gate 6 passes on all 20 measured levels (dark rims behind the thorns, blades and arrows). Gate 9 plays every route at 20:9, 16:9 and 4:3 with a hole check, which found and fixed Obsidian's square-cut skyline. Gate 4 is better (carved walls on tall faces) but L002's corridor ceiling and some Obsidian ceilings are still mostly plain. Open: re-pin the short replays (L004, L005, L024, L025, L027–L029), the three reveal-frame failures (L022, L036, L039), gate 8.

## 6. Risks

- **Art decides the top score.** Code can reach about 7–8. Reaching 9 needs paintings the developer approves, and past rounds have been rejected.
- **Phone cost.** Light, foliage and FX all add up. If P0.1 shows little room, foliage and live lights get baked into art.
- **Disguises.** New stone detail must stay pixel-identical across a trap and its host.
- **AutoSprite** needs a paid plan and keeps the 20-credit cap per asset.
- **One world, 30 levels.** Other worlds are held for later levels (D-302), so Golden Ruins has to vary from within, through its three districts and Pack B.

## 7. Level by level

All 30 Golden Ruins levels stay in Golden Ruins (D-302), split into three districts of one city. Hero moments only use what the level already contains; §2 applies to every row. Moments that need water depend on §8.3.

### District 1 · The Gardens, morning to night

| Level | Sky | Main fix | Hero moment |
|---|---|---|---|
| L001 | Dawn | Warm sky to match the stone; feature on the first wall | First view of the door through a broken wall |
| L002 | Gold | Mover skin | Gold sky over thorn field, already the colour benchmark |
| L003 | Gold | Paint the small blocks as real ruin chunks | Thick vine curtain at the start |
| L004 | Noon | Replace the blank end wall | The arcade, the game's best scene, carried to the end |
| L005 | Noon | Mover skin on `Rise_D` | Rising column against a tower |
| L006 | Mist | More contrast, less wash | Ruined skyline from the high slab |
| L007 | Storm | Slabs lose the ghost look | Far city under the storm light |
| L008 | Rose | Press art; posts get real stone | Arches over the pit |
| L009 | Ember | `Lift_A` skin | Great tree at sunset |
| L010 | Moon | Real night stone; pit fix; finale vista | Moonlit citadel, chapter finale before the paywall |

### District 2 · The Overgrown Quarter (the paid game opens here)

| Level | Sky | Main fix | Hero moment |
|---|---|---|---|
| L011 | Mist | Rebuild the look: walls, vines, big falls; `Riser` skin | Falls beside the climb, the first paid impression |
| L012 | Jade | Mover skin | Vine-choked halls |
| L013 | Jade | Fill the empty sky; proper vents for steam | Long bridges across a canyon |
| L014 | Teal | Replace repeated root stamps | Hanging vines in front of a waterfall |
| L015 | Teal | Pit depth | Giant trunk beside the towers |
| L016 | Noon | Floating crate art | Arcade overgrown |
| L017 | Mist | New layout feel through walls; palette match | Twin levels split by falling water |
| L018 | Storm | Same; laser glow on wet stone | Laser through rain-dark ruins |
| L019 | Storm | Posts get real stone | Storm light through canopy |
| L020 | Moon | Finale vista | Moonlit waterfall |

### District 3 · The High Citadel

| Level | Sky | Main fix | Hero moment |
|---|---|---|---|
| L021 | Dusk | Keep the strong trap read; warm the stone | Swinging axes against the citadel's lower towers |
| L022 | Dusk | Hero walls replace the brick; comb strip | Clock tower interior |
| L023 | Ember | Platforms as real ruin chunks; lake reflection | Sunset lake below the citadel |
| L024 | Ember | `Piston_2` skin; grey strip | Spinners in the citadel yard |
| L025 | Ember | Hero walls; less heavy brick | Great bronze dome above the rope swing |
| L026 | Rose | Painted hatches | Hatch floor over the void |
| L027 | Rose | Press art | Thick vines against the citadel's towers |
| L028 | Ember | Keep the orange | Sunset sea of cloud behind the slabs |
| L029 | Moon | Ask to revisit D-249's plain band | Trapdoor tower at night |
| L030 | Moon | Hero walls; finale vista | Summit citadel at night, the Golden Ruins finale |

### Obsidian Void

| Level | Sky | Main fix | Hero moment |
|---|---|---|---|
| L031 | Blue | New walls | Lasers against the crystal spire |
| L032 | Teal | Feature on the opening wall | Hard-light bridge under the planet |
| L033 | Violet | New walls | Portal pair in violet fog |
| L034 | Teal | Thicker platforms; calmer orbs | Open void with the planet |
| L035 | Blue | Pits get depth and glow | White beams through the citadel |
| L036 | Grey-blue | New walls | Red lasers over machinery |
| L037 | Teal | Less fog, more contrast | Crystal towers in the mist |
| L038 | Blue | Lift the dark, reduce wall | Long corridor with hero machines |
| L039 | Violet | Lift the dark, machines fill the walls | Reactor core seen through the wall |
| L040 | Deep | Finale vista | Hard light, lasers and beams at the heart of the void |

## 8. Decisions needed

1. **Worlds: decided (D-302).** 150 levels, 30 per world. L001–L030 stay Golden Ruins; the other worlds aren't used yet. Open: 150 at 30 each is five worlds, and six are painted (Golden Ruins, Obsidian Void, EmberNight, JungleFalls, SkyIsles, SunkenGrotto), so one has to go or the total grows to 180.
2. **Banners: decided (D-308).** Muted bronze-ochre.
3. **Water and waterfalls: decided (D-308).** Allowed where it fits the background and never touches a walkable floor; not forced. Tried in eight levels; none had room, so §7's water moments (L011, L014, L017, L020) need another hero moment.
4. **Foreground:** recommended a few blurred pieces at frame corners only, never over the play path. You removed these once, so this is your call.
5. **D-249 plain band (L029, L030): decided (D-308).** Redrawn with real stone; plugs and timing kept.
6. **Art route:** recommended ChatGPT with prompts from Claude, one painting pack at a time. AutoSprite only if you buy a plan.
7. **Phone session:** P0.1 needs the Pixel 8a before any heavy work.
