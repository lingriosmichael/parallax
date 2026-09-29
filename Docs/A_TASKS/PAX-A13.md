# PAX-A13 · Trap art kit and trap animation (absorbs PAX-V05)

**Status:** Phase 1 ruled 2026-09-29; style gate ruled 2026-09-29 (§12); full kit in progress

**Folder:** `Docs/A_TASKS/` · **Phase:** E, look & feel · **Author:** the Architect · **Implementer:** Claude Code
**Depends on:** PAX-V03 (look slice, performance budget) and the environment art: still images the developer generates in ChatGPT and adds to the repo. See §8.1 for what happens if either isn't in yet.
**Target:** `Docs/Art/LOOK_AND_FEEL.md` (D-094), `styleframe_A_v1.png`, `14_environment-breakdown.png`.
**Absorbs:** PAX-V05 (trap presentation). The assets and the code that plays them ship together, because a trap animation is only right when it's driven by the trap's real state.

---

## 1 · Goal

Every trap in the kit looks finished and animates, in Reality A's style, on all 20 shipped levels and in the Trap Lab:

- **AutoSprite makes the trap art and its animation frames**, through its API, driven by Claude Code: painted trap bodies and the flipbooks for anything that moves or changes shape. The developer pays for AutoSprite for exactly this.
- **Claude Code does everything around it**: the prompts and reference images sent to AutoSprite, a post-processing step that makes the output game-ready, the disguise seam, shaders, particles, the presenters that play the frames from game state, and the tests.
- **Each trap has its full life on screen**: idle, tell, fire/reveal, motion, rest/stuck, reset (checkpoint rewind), and a death effect when it kills.
- **Disguised traps stay invisible until they fire** (P10), by construction, not by eye.
- **Nothing about the game changes**: no collider, tick, route pin or validator result moves.

The developer's bar: it's a paid game and the traps must look "spot on". Traps are most of what the player watches; they should have weight (LOOK_AND_FEEL §5: dust when a block falls, debris when a floor crumbles, a glint on a launcher's tell).

## 2 · Scope

**In:** every element that fires, reveals, moves, changes a rule or kills, including (to be verified in §8, not trusted from here):

| Group | Elements |
|---|---|
| Floors that betray | CollapsingFloor, FakePlatform, KIT-10 floors (mover, slide-away Carry/Slip, drop-and-return, shrinker, push wall) |
| From above / below | FallingBlock (Down, flush in the ceiling; Up), MovingTrap Solid (Crusher, lifts) |
| Spikes | static spikes, HiddenSpikes, periodic spikes, MovingTrap Hazard (SlidingSpikes, Sweep), `UnjumpableFloor` hazards |
| Projectiles | Arrow (disguised launcher, both directions), Spear (tell, flight, stuck shaft that becomes solid) |
| Rule changers | GravityFlip (visible and hidden variants), Inverter (orb, the cue on the cat, the 30-tick blink), DoorRetreat (the retreat move only) |
| Nature | Geyser (vent, tell, eruption column), Storm cloud (dormant, wake, follow, charge, strike), climbable vines and snap vines |
| Deaths | one short impact effect per killer kind (spikes, crush, arrow/spear, lightning, fall into pit/water) |

**Out:** the environment stills (floors, walls, ceilings, dressing, background layers, water, the door and checkpoint gate at rest): the developer makes those in ChatGPT. Cat clips and death poses (PAX-A08/V07); screen shake, flash, hit-stop and haptics (PAX-V06); sound (PAX-064); Reality B (D-047); the dev-only state labels of PAX-V01 (keep them working, don't restyle them).

Where the line with the environment stills is unclear (climbable vines vs hanging vine dressing, the door while retreating, spike floors), Phase 1 looks at what's in the repo and proposes the split. Don't make a second version of anything the environment art already covers.

## 3 · Hard rules

1. **P10, pixel-identical until the reveal frame.** A disguised trap (collapsing floor, fake platform, flush falling block, disguised launcher, hidden spikes, hidden flip, and any KIT-10 floor that looks like floor) renders with **its host's exact skin** until it fires: same sprite or material, same world-space tiling, same edge/cap treatment, same dressing rule, so no seam, offset or tint gives it away. Its pre-reveal look is **never** an AutoSprite frame; it's the host's own art, copied through a seam (`HostSkin` or similar) when the level is built, so it stays right when the environment art changes. AutoSprite frames take over from the reveal frame. A validator enforces it (§9).
2. **Hitboxes don't move (D-052).** Art follows colliders. No sprite, animation, shader or effect changes a collider, a trigger, a body, a tick, a route pin or a validator number. Presentation **reads** game state and never writes it; the gameplay assemblies don't reference presentation code.
3. **Deterministic where it shows state.** Anything that shows a tell, a reveal, a charge, a strike or a position is a pure function of the trap's state and ticks since its last event (50 Hz): an AutoSprite flipbook's frame index is computed from ticks, never from `Animator` time or `Time.time`. A checkpoint rewind (D-091) restores the visual state exactly, including particles already in flight (clear or re-seed them). Where there are variants (lightning bolts, crumble patterns), the variant is picked by a seed tied to the trap and its event count, never `Random` at render time. Ambient motion (a vine's sway, a dormant cloud's drift) may run on render time.
4. **Tells are the tell, and nothing else is.** A trap with a declared tell (arrow and spear ≥ 6 ticks, D-057; the storm's 25-tick charge; the geyser's tell; the inverter's 30-tick blink) shows it clearly for exactly that window. No trap shows anything earlier: no idle shimmer, particle, glint or animation on a disguised trap before its trigger.
5. **Honest hazards read as danger at phone scale**: bronze/rust spikes, one consistent danger language across every honest hazard (LOOK_AND_FEEL §4). A stuck spear shaft must read as something you can stand on.
6. **Every death reads.** The killer is identifiable during the death hold (D-058), and its effect never delays control: death to control stays ≤ 0.75 s (D-041), respawn has no fade.
7. **Palette and light:** warm, backlit, shadows warm brown and never black, no cyan (Reality B's colour). Emissive only where LOOK_AND_FEEL layer 05 allows (inverter orbs, geyser vents) plus the storm's charge and strike as additive effects; no extra 2D lights unless the V03 budget has room.
8. **Inside the PAX-V03 budget** (draw calls, overdraw, lights, particles, texture memory), measured with the chaos moment of the busiest band-2 level on screen. Traps go in an atlas; one material per group where possible.
9. **Built by the pipeline.** Trap visuals come through the level builder and `PARALLAX/Setup` menus, so `Rebuild All Levels` reproduces them. No hand-edited scenes or YAML. Sandbox_Realities stays untouched; `Band2DevTests.cs` stays local-only.
10. **Credits are the developer's money.** No AutoSprite call without an approved budget (§6). The API key is read from an environment variable and never written to the repo, a log, a report or the review file.

## 4 · Inventory table (Phase 1 fills it from the code)

One row per element **and variant** that exists in the kit, with the states it can be in. The Architect's list above is a starting point, not the truth; the code is. Columns:

| Element / variant | Disguised or honest | Host (if disguised) | States (idle → tell → fire → move → rest → reset) | State source (file:line) | Used in levels | AutoSprite asset(s) and frame count, or code effect | Death effect |
|---|---|---|---|---|---|---|---|

## 5 · Art direction per group

The look is `styleframe_A_v1`: painted, warm, weathered stone ruins overgrown with vines, bronze and rust metal, red banners, gold backlight. Traps are part of that world, not stuck on top of it: ancient mechanisms in the ruins. These notes are the brief for the AutoSprite prompts.

- **Collapsing / fake floors:** host art until the reveal; then the piece cracks into shards that drop and spin, with dust and small debris. A fake platform may simply dissolve with a puff if crumbling reads wrong for it; Phase 1 proposes.
- **KIT-10 floors:** the host's look, plus weight when they move: dust at the leading edge, a grinding track or seam only if the floor is honest. Drop-and-return: a soft settle and a dust ring when it comes back. Shrinker: the edge crumbles away as it shrinks, so the shrinking reads as erosion, not scaling.
- **Falling blocks:** flush in the ceiling with the host's art; on release, a hairline crack and a sprinkle of grit on the release frame (not before), then a heavy fall and a dust burst on landing. Up variant mirrored.
- **Crushers and lifts (MovingTrap Solid):** heavy carved stone or bronze, a visible mechanism if honest.
- **Spikes:** bronze/rust, slightly worn, with a dark socket strip. Hidden spikes: nothing until they come up, then a fast rise over 2–3 ticks with a grit puff. Periodic spikes: honest, sockets always visible, a short rattle frame before each rise if the tick rules allow it without changing the count players learn.
- **Arrow launcher:** a carved slot in the wall that matches the host; the tell is a glint and a small click-back of the arrowhead in the slot for the tell window; the arrow is thin, fletched, with a faint motion streak; it breaks or sticks on impact.
- **Spear:** heavier than the arrow, bronze head, wooden shaft; same kind of tell; on stopping, a wobble of the shaft (visual only) and a bit of dust at the wall. Stuck, it reads as a pole to stand on.
- **Gravity flip:** a visible flip is an old glyph ring with slow drifting motes (warm, not cyan); on firing, a ring pulse. Hidden flips: nothing at all until they fire, then the same pulse.
- **Inverter:** a glowing orb (layer 05); on touch, it flares, and the cat carries a small cue for 150 ticks, with the 30-tick blink at the end. The cue is on or next to the cat and readable at phone scale.
- **Geyser:** a stone vent in the floor (layer 05 glow in the vent); the tell is bubbling and wisps of steam; the eruption a column of water and spray exactly as tall as the gameplay column, with a splash at the top.
- **Storm cloud:** dormant, a heavy dark-bellied cloud; waking, it darkens and churns; charging (25 ticks), internal flicker and a gathering glow under it; strike (6 ticks), a jagged bolt down to the first static top (several bolt variants, picked by seed), a flash and a scorch mark that fades.
- **Vines:** the environment stills own hanging vines as dressing; climbable vines must be distinguishable from dressing without breaking P10 for anything else (Phase 1 proposes how). Snap vine: fibres fray and it snaps with falling leaves.
- **DoorRetreat:** the door slides with a stone scrape of dust; no new door art.
- **Deaths:** spikes, a short impact spark and grit (no blood or gore); crush, a dust burst; arrow/spear, a hit spark; lightning, a flash and scorch; pit/water, a splash.

## 6 · How the art is made

**AutoSprite for trap bodies and animation frames; code for the rest.**

| Made by AutoSprite | Made in code |
|---|---|
| Painted trap bodies (launchers, spikes, crushers, vents, orbs, glyph rings, the cloud, spears, arrows) | The pre-reveal disguise (the host's own art, §3.1) |
| Frame animations: crumble, spike rise, arrow and spear flight/impact, spear wobble, geyser eruption, cloud churn/charge, lightning bolts, snap vine, orb flare, flip pulse | Motion that is only movement (slides, rises, falls, shrink masks), UV scrolls, flashes, glows |
| Death impact frames, if they read better painted than as particles | Particles: dust, grit, debris, spray, leaves (seeded) |

1. **Learn the API first, don't guess it.** Phase 1 reads AutoSprite's API documentation and reports what it actually offers: inputs (text, reference images, a base sprite to animate), outputs (sheet or frames, resolution, transparency, frame count), cost per call in credits, rate limits, and whether a re-run gives the same result (assume not).
2. **A credit budget before any call.** Phase 1 gives a table: asset, frames, calls, credits, with the developer's cap of **20 credits per asset** respected, and a total. The developer approves the total in §11; the style gate (§9.2) spends only its share. If an element needs a retry beyond its line, stop and ask rather than spend.
3. **Style locking.** Every request carries the same style block (derived from §5 and LOOK_AND_FEEL §4) and the same reference images: `styleframe_A_v1.png` plus the developer's environment stills, so traps and environment look like one world. Where AutoSprite can animate from a base sprite, generate the body once, approve it, then animate that body, so the frames match the still.
4. **Everything is kept.** For each asset, the raw AutoSprite output and a small sidecar file with the prompt, reference images and parameters are committed under the trap art folder (Phase 1 confirms the path), so any asset can be regenerated or handed to the developer for a touch-up.
5. **A post-processing step in the repo** (Phase 1 proposes a C# Editor menu, e.g. `PARALLAX/Art/Import Trap Kit`, or a script under `Tools/ArtGen/`), deterministic and idempotent over the committed raw files: trim, set pivots to match colliders, scale to the environment's pixels-per-unit, check the palette (flag cyan and black shadows), generate normal maps so the sun lights traps like layer 04, pack atlases, and write sprite and frame data. Re-running it produces no diff.
6. **Scale matches the environment**: the same pixels-per-unit and texture density as the developer's environment art, chosen so a texel is about a screen pixel on the Pixel 8a at the level camera's size.
7. **Self-review before anyone else looks.** Render every element's states to a contact sheet (§9) and read it back (you can view PNGs). Compare against `styleframe_A_v1.png` and the environment stills, and fix what doesn't match (post-processing, prompt, or one approved retry) before reporting.
8. **Honest about misses.** If an element doesn't come out right within its credits, say so, show it, and propose the fix (a better prompt, a code effect instead, or a paint-over by the developer), rather than shipping it weak.

## 7 · Presentation contract (the absorbed PAX-V05)

- One presentation component per element kind, in a presentation assembly (Phase 1 proposes: a new `Parallax.Presentation` referencing `Parallax.Gameplay`, never the reverse).
- Each reads the trap's state and tick counters and maps them to a frame index, a transform or a shader parameter through a **pure function** that EditMode tests can call without a scene.
- It hooks into the checkpoint snapshot/rewind (D-091) so a rewind restores the visuals.
- The route harness and headless replays run without presentation, or with it inert; either way the replays and pins are identical.
- It exposes read-only events (`Revealed`, `Fired`, `Landed`, `Killed(kind)`, …) that PAX-V06 (shake, flash) and PAX-064 (sound) will subscribe to later. No subscribers in this ticket.

## 8 · Phase 1 · Trace and questions → STOP

### 8.1 Pre-flight
- Git status clean apart from `Band2DevTests.cs`; HEAD recorded.
- Has **PAX-V03** landed (budget numbers)? Are the developer's **environment stills** in the repo (where, what PPU, which hosts have a skin yet)? For each: yes, partly (what), or no. If the stills aren't in yet, the `HostSkin` seam still gets built against today's grey-box look, and the style samples use `styleframe_A_v1.png` alone as the reference; say which parts can go ahead.
- Is the AutoSprite key available as an environment variable? Report its name only, never its value.

### 8.2 As-built trace
- The inventory table (§4), complete for the whole kit and L001–L020 plus the Trap Lab rooms, with file:line for each state source.
- How each element's state and ticks can be read (existing fields, or accessors to add without changing behaviour).
- Where the level builder creates trap renderers today, and where the host skin can be copied from.
- How the checkpoint snapshot (D-091) stores and restores trap state, and the hook a presenter needs.
- Whether the death event knows the killer's kind; if not, the smallest read-only way to add it.
- Whether the route harness instantiates renderers or runs headless.
- The AutoSprite API report (§6.1) and the credit budget table (§6.2).

### 8.3 Questions (≤ 6, each with your recommendation)
Must include:
- **Q1** The split with the environment stills (climbable vines, the retreating door, spike floors).
- **Q2** The storm cloud while dormant: an obviously dangerous cloud, or one that passes for a sky cloud until it wakes? (It's on the gameplay layer; P10 doesn't strictly apply because it's not a surface.)
- **Q3** The AutoSprite plan: which rows go to AutoSprite and which to code, and the credit total for the style gate and for the full set.
- **Q4** Any element where the reveal frame or tell can't be shown without changing a tick (e.g. a rattle before periodic spikes): propose to drop the effect, never to move the tick.

Stop and post the trace and the questions. **No AutoSprite calls in Phase 1.** The Architect replies with rulings appended as §11.

## 9 · Phase 2 · Build (red first)

### 9.1 Tests first, seen red
- **`ValidateTrapSkins`** (validator, runs over L001–L020 and the Trap Lab): every disguised element's pre-reveal skin equals its host's (sprite/material, tiling mode and world-space UV origin, colour, sorting, dressing rule). Seen red on a deliberately mismatched fixture.
- **Collider invariance:** for every level, the set of colliders and triggers (type, bounds, layer, isTrigger) is identical with and without presentation built.
- **Determinism:** each presenter's pure function returns the same frame/transform for the same (state, ticks); running a level's solution route to a checkpoint, dying, rewinding and replaying gives the same visual state at the same tick as the first pass.
- **Tells:** for arrow, spear, geyser, storm and inverter, the tell visuals are on exactly for the declared window and off before it; for every disguised trap, nothing differs from the host before the trigger tick.
- **Budget:** atlas count, texture sizes and per-level draw-call/particle counts against V03's numbers (or the interim numbers ruled in §11).
- The whole existing suite stays green: **1,334 / 1,334** before your additions, and every route pin unchanged.

### 9.2 Style gate → STOP
Build three elements end to end first: **CollapsingFloor** (disguise + crumble), **Arrow** (tell + flight + impact) and **Geyser** (tell + eruption), with AutoSprite inside the approved style-gate credits. Render them into a first contact sheet and a phone-sized screenshot inside L013 or L011. Report the credits spent. Stop for the developer's look. His notes become rulings (§12), including any change to the style block, before the rest is generated.

### 9.3 The full set
- Every row of the inventory: assets, presenter, death effect, within the approved total.
- `PARALLAX/Art/Trap Contact Sheet` menu: renders every element in every state (idle, tell at mid-window, reveal frame, mid-motion, rest/stuck, one death effect) onto one labelled grid at phone scale against the environment background, saved to `Docs/Art/Traps/contact_sheet.png`. Plus a small strip per animated element (8–12 frames at even ticks).
- `Rebuild All Levels` run; all 20 levels and the Trap Lab rebuilt with the new visuals.
- The Trap Lab rooms each show their element finished, for the developer's play.

## 10 · Done and handover

**Done when:**
- Every inventory row has its art, its animation and its death effect, or a named reason and a proposed fix.
- `ValidateTrapSkins`, the invariance, determinism, tell and budget tests pass; the full suite passes with the total reported (batch mode if the Editor's Run All won't finish; the command is in HANDOFF_3).
- Raw AutoSprite outputs and their sidecars are in the repo; the post-processing re-run produces no diff.
- Credits spent reported per asset and in total, against the approved budget.
- Contact sheet and phone-scale screenshots under `Docs/Art/Traps/`.
- A decision drafted (next free D-number): the trap presentation contract (§7), the P10 skin rule (§3.1) and the AutoSprite pipeline (§6), Proposed.
- Nothing committed: the developer plays and commits.

**Handover for the developer's play** (as for the level work):
- Which Trap Lab rooms and levels to watch, and the two or three moments most likely to look wrong at phone scale.
- A per-level list of any disguised trap whose reveal was hard to keep identical, with where to look.
- What wasn't verified: device (Phase H), and any element left with a proposed fix.
- The review file: `{ git status; git --no-pager diff --stat; git --no-pager diff -- '*.cs' '*.md'; } > ~/Desktop/paxA13_review.txt 2>&1` (art is reviewed from the contact sheet, not the diff; the key must not appear in it).

---

*Rulings go below as `## 11 · Rulings (Phase 1)`, then `## 12 · Rulings (style gate)`.*

## 11 · Rulings (Phase 1)

**R1 · Split with the environment stills (Q1): accepted as your §3.** Vines, door, checkpoint and water are the developer's. The kit adds the snap, the retreat dust, every spike and every trap body. The slimmer post shaft for 0.4–0.5 u posts goes to the developer's environment list; until it exists, disguised launchers in those posts use whatever host skin the post has. Traps use 128 px per unit, the play layer's scale.

**R2 · Storm cloud asleep (Q2): accepted.** It passes for a sky cloud: slightly denser than the layer-01 clouds, with a darker belly. It darkens and churns only from the wake tick.

**R3 · AutoSprite plan and budget (Q3): accepted with changes.**
- The AutoSprite-versus-code split in your §6 stands. The crumble shards are cut from the host's own pixels; impacts, lightning, rigid motion and particles are code.
- **T10 (socket strip) dropped** (see R4). **T11 (crusher body) dropped**: every moving solid keeps the host skin.
- Approved budget: **style gate 16 planned / 32 ceiling; all of A13 40 / 80.** Each line's ceiling allows one retry; beyond it, stop and ask. Re-cuts through `regenerate` are free and don't count.
- Call the free `GET /account` first and report the balance before the first paid call.
- Static bodies use the **ultra** quality (same 1 credit). Animations use turbo; ask before any pro call.
- Add "character from an uploaded image" to `Tools/Art/autosprite_client.py`. Every animation starts from an approved still uploaded free.
- **Fallback for bodies:** if the style gate shows AutoSprite's text-only statics don't match the styleframe, say so. The developer then paints those bodies in ChatGPT with the environment style header and they're uploaded as characters for AutoSprite to animate. Don't spend retries trying to prompt your way there.
- FallingBlock Up has no level or Trap Lab use: the Down art mirrored in code, no credits.

**R4 · Visibility parity (Q4 and Q5): the governing rule.**
- **The art never shows more, earlier or elsewhere than the grey-box does.** Tick for tick, a trap's art is visible exactly when its grey-box renderer would be enabled, and it sits within the grey-box's rendered bounds, which follow the collider. Only **effects** (dust, grit, debris, spray, flashes, pulses) may extend past the bounds, and they may start no earlier than the tick of the event they show.
- So: periodic spikes draw nothing while down, as today (no socket strip, no rattle). A crumble starts on the tick the floor disappears. A block's crack appears on its release tick. Hidden spikes rise over 2–3 ticks while already lethal. The arrow and spear glint runs exactly over the declared tell. The storm charge runs over its 25 ticks; the inverter blink over its last 30.
- Any effect that would need a tick changed is dropped, never the tick.

**R5 · Route pins and gameplay touch points (Q5): accepted, plus a parity test.**
- Art on a separate "Art" root beside each trap, never under it, in a new `Parallax.Presentation` assembly (references Gameplay, never the reverse). Grey-box renderers keep every hashed value and only stop drawing (`forceRenderingOff`).
- Tests, written first and seen red:
  - the harness signatures, rendered bounds and every collider are identical with and without the art, on all 20 levels and the Trap Lab;
  - **art parity:** on every level's declared routes (solution and betrayals), for every trap and every tick, art-visible equals grey-box-enabled, and the art's non-effect bounds sit inside the grey-box bounds (tolerance 1 px at 128 px per unit). A trap whose art would turn visible even one tick early fails, naming the level, trap and tick.
- The gameplay changes are read-only only: accessors for the private countdown, a block's lethal window, a mover's phase and the inverter's fire tick; a killer field on `DeathInfo`; a `Restored` event after a checkpoint rewind. No behaviour, timing or ordering change; the 1,334 must stay green before your additions.

**R6 · Prerequisites (Q6): accepted.**
- Interim budget until V03: trap art in one 2048² atlas at most, one material per group, ≤ 150 live particles in the busiest chaos moment, no new 2D lights. Also measure L020's draw calls in its chaos moment before and after the art, and report both.
- Write **D-094** into `07_DECISIONS.md` now (visual production starts; the target is `LOOK_AND_FEEL.md`; the budget will be set by PAX-V03), Accepted.
- The allowed-files list in your Q6 is accepted.
- The inverter orb is warm gold (layer-05 glow); the cyan goes.

**Style gate order:** balance check → tests red → T01, T02, T03, T04, T05, T06 statics → your self-review of the statics against the styleframe (if they miss, stop here with R3's fallback rather than animating them) → T07, T08 → Arrow, Geyser, then CollapsingFloor (on the A02 tiles, labelled, if the ENV-10 tiles aren't in yet) → contact sheet and phone-scale screenshot in L011 or L013 → report credits spent → stop.

**Allowed files** (the Q6 list R6 accepts, copied here for reference): a new `Parallax.Presentation` assembly; `Core/Presentation/*`; the read-only accessors in `Gameplay/Rooms/*`; `SoloRoomBuilder` and `TrapKitSetup*`; a new `Editor/Art/*`; `Tools/Art/*`; `Art_Source/AutoSprite/Traps/`; `Assets/_Game/Art/Traps/`; `Docs/Art/Traps/`; tests.
Added 2026-09-29 (review round 1 Blocker, ruled by the developer; kept): `Editor/Routes/RouteReplay.cs` (the
`ReplayOptions.AfterTick` hook only); `Editor/Parallax.Editor.asmdef` and `Tests/EditMode/Parallax.Tests.EditMode.asmdef`
(the `Parallax.Presentation` reference only); `Assets/_Game/Data/TrapArtConfig.asset` (written by
`PARALLAX/Art/Import Trap Kit`); `Art_Source/Traps_ChatGPT/`.

## 12 · Rulings (style gate)

**R7 · AutoSprite is not used for trap motion.** The T07/T08 result (a human figure on the vent) shows its animation pipeline assumes a character. No further AutoSprite animation calls in A13; T20 (orb flare), T21 (cloud churn) and T22 (vine snap) are cancelled.
- All trap motion is code: transforms, shaders, seeded particles, and flipbooks of painted frames where needed, always picked from ticks.
- Solid bodies come from the developer in ChatGPT, as with TRAP-01 to 03.
- AutoSprite stays only for effect stills on black (as T04 and T06 passed), 1 credit each, within the approved total (40 planned / 80 ceiling for all of A13; 16 spent so far). One retry per line; if a line would go past its ceiling, drop it and use a code effect instead of stopping.
- Record in the A13 decision draft: AutoSprite is kept for the cat's animations (PAX-A08), not props.

**R8 · The collapsing floor's pattern jump is a P10 failure; fix it now, not in V03.** §3.1 already requires the host skin's world-space tiling.
- The host skin and every shard sample the host art in world space. Each shard freezes its world-space offset on the reveal tick, so the reveal frame is pixel-identical to the frame before it, and only then do the shards move.
- Add a test, seen red on today's A02 row first: for every disguised element, the frame on the reveal tick is identical to the frame on the tick before, except for effects that start on that tick.
- Keep the A02 row on the contact sheet as the proof until the ENV-10 tiles land.

**R9 · Smaller fixes.**
- Geyser spray at the column's top: larger and denser, in code.
- Geyser tell: keep the warm vent glow; swap in the painted steam from the ChatGPT batch when it arrives.
- Arrow glint: make it clearly readable within the tell window in the Game view at 2400×1080.
- The Flip Light Check scene has no camera, so the Game view is empty. Add a camera framing the three launchers. The developer judges the flip in the final look.
- The background layers: in L011 the background painting covers only part of the view and the rest is flat tan. That's environment work, not A13; list it in the handover and don't fix it here.
- Running PARALLAX/Art/* menus is fine. PARALLAX/Setup/* stays the developer's, except on the batch clone.

**R10 · Finish the whole kit in one run. No stops.**
- First, write the ChatGPT batch (A13_ChatGPT_trap_bodies.md, TRAP-04 onward, same format and style header as TRAP-01 to 03) with every body and painted frame code can't make: spike strip, spear, inverter orb (warm gold), inverter cue, glyph ring, storm cloud (asleep, with a darker belly), scorch mark, block crack, geyser steam. For each: what it's for, its size in units, its facing, and the exact file path to save it to. Post a one-line message that the batch is ready, then carry on without waiting.
- Apply R8 and R9 to the three built traps.
- Build every remaining inventory row, each with its parity, determinism and tell tests seen red first: spikes (static, hidden, periodic, sliding), spear, falling blocks, moving and KIT-10 floors, push wall, shrinker, DoorRetreat, visible and hidden flips, inverter, storm cloud (asleep, following, charge, strike, lightning bolt, flash, scorch), snap vine, and every death effect.
- Where a row needs a ChatGPT body not saved yet, build it completely with a temporary code-drawn body in the right size, facing and palette, marked PLACEHOLDER on the contact sheet. When the images arrive, post-process them and swap them in; the tests must not change.
- At the end: rebuild all levels and the Trap Lab on the batch clone; run the full suite and report the total; run pax-reviewer and fix what it finds; regenerate the contact sheet with every element in every state, and phone-scale screenshots from L013, L015, L016 and L020; write the §10 handover.
- Only stop early for: a change that would move a tick, collider or route pin; a test that can't pass without breaking a rule in §3; or anything that would spend credits past the approved total. Everything else: decide, note the decision in the handover, and keep going.
- The handover lists: every row and whether its body is final or PLACEHOLDER; credits spent per line and the balance; the full-suite total before and after the developer's rebuild; the reveals hardest to keep pixel-identical; what to watch in the Trap Lab and which levels; and exactly which PARALLAX/Setup menus the developer runs and saves afterwards.
- Don't commit.
