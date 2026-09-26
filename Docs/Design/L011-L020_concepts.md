# Levels 11–20 · Concepts (draft for the developer's markup)

**Status:** Draft, 2026-09-26. Architect's concepts for PAX-060. Mark each level keep / change / cut; the kept ones become the PAX-060 ticket's sketch brief.
**Built from:** `LEVEL_DESIGN_GUIDE.md` (P1–P13), the Level Devil inventory, the kit as built (D-086–D-090, PAX-089).

## The idea behind the batch

- **Every level has its own verb.** Levels 1–10 taught "the room lies". From 11 on, each level asks for a *different way of thinking*: build your path, unlearn your hands, count a rhythm, go down instead of up, keep moving, think on two surfaces, plan the way back, get above the danger, execute under a mind trick, ride the machine.
- **Chaos by choreography.** Every chaotic moment is a fixed chain (D-040, D-067): pure panic on the first attempt, a dance you know by heart on the tenth. That's the Level Devil feeling, turned up.
- **Longer levels, in acts.** Each level is 2–3 acts, about 30–45 s once known (levels 1–10: 10–30 s). See the one open decision at the bottom: the retry cost.
- **11–15 introduce one new element each, 16–20 combine them.** Level 20 is the exam.

| # | Name | New element | The verb (how you solve it) | Shape | Chaos moment |
|---|---|---|---|---|---|
| 11 | Scaffold | Spear | **Build** your own staircase | climb, start middle | The volley: 5 spears in 1 s from both walls |
| 12 | Mirror, Mirror | Inverter | **Unlearn** your hands | right → left | The long inversion: a whole act played mirrored |
| 13 | Old Faithful | Geyser | **Count** two rhythms | climb | The sync: every geyser erupts at once |
| 14 | Root System | Vines | **Go down** when everything says up | descend | The canopy: a chain of snapping vines |
| 15 | Hunted | Storm cloud | **Keep moving**, find where you may stop | left → right, wide | The ceiling caves in behind you |
| 16 | Upside-Down Garden | Flip + vines | **Two surfaces**: floor and ceiling | start middle, ceiling | The gravity storm |
| 17 | Déjà Vu | Spears + chains | **Plan the way back** | double back | The room rearranges itself at the far end |
| 18 | Eye of the Storm | Cloud + geyser + vine | **Get above** the danger | climb, start right | Lightning below, collapsing sky-ledges above |
| 19 | Muscle Memory | Precision + inverter + spears | **Execute** a memorised input map | descend, wide | A bridge that builds itself under you |
| 20 | The Machine | Everything | **Ride** a room-wide chain reaction | spiral, double back | The whole room transforms while you're in it |

Direction mix (P12): plain left→right 1 (L15); climbs 3 (L11, L13, L18); descends 2 (L14, L19); start middle/right 5 (L11, L12, L16, L18, L20); double back 2 (L17, L20). ✓

---

## L11 · Scaffold (spear)

**Verb: build.** Spears are deadly while they fly and a floor once they stick. The only way up is a staircase you fire yourself, in the right order, from the right place.
**Shape:** 32 × 22, three storeys, start bottom-middle, door top-left, visible from the start (P9). ~35 s.

- **Act 1 · The first stair.**
  - T1: step onto the ledge by the shaft and a spear fires across it at your jump height. Jump (the natural move) and you die in its lane. Walk on, and it sticks: it's your first step. **NJ**
  - T2 ⟲T1: you've learned "let it stick first". The next ledge's floor gives way 30 ticks after you stop on it, so waiting kills you. The spear there fires only when you jump from the *far* end of the ledge. **NW**
- **Act 2 · The volley** (the chaos moment).
  - T3: one trigger chains five spears from both walls, 12 ticks apart, at rising heights. For one second the shaft is a crossfire, then a lattice. There's exactly one safe spot to stand: under the first stuck spear. **SS**
  - T4 ⟲T3: the lattice looks like an obvious staircase to the door. Its top spear is fine to stand on, but landing on it drops a block from the ceiling (P4). The real route zig-zags down one spear and across to the other wall. **LW**
- **Act 3 · The door.**
  - T5: the ledge in front of the door is a fake platform. You reach the door by the spear you fired back in Act 1, now at the right height from the other side (P3: the same spear, used twice). **OL**
- **Dead ends:** a high right ledge that looks like a shortcut (hidden spikes, Dies); an alcove behind the lattice (Recovers).
- **Kit:** spears are Once; stuck spears at least 1.0 long; the jumps onto them are ordinary `RequiredJump`s. No new kit.

## L12 · Mirror, Mirror (inverter)

**Verb: unlearn your hands.** Inversion stops being a trap you avoid and becomes a state you plan around.
**Shape:** 32 × 12, two storeys, start right, door far left on the lower storey, visible through a gap. ~35 s.

- **Act 1**
  - T1: a pillar with an orb in a corridor you can't avoid. Right after it, a gap to the left: you have to press right to jump it. A first-timer holds left and walks back into spikes. **J** (inverted)
  - T2 ⟲T1: the next inverter is hidden (an invisible trigger). You've learned to stop and wait inversions out, but the floor you wait on gives way after 40 ticks. Play it inverted. **NW**
- **Act 2 · The long inversion** (chaos): a hallway of Rearm orbs, spaced so each one restarts the timer. The whole act is played mirrored: arrows on jump triggers, a collapsing floor one step ahead, a falling block. Your hands lie for twelve seconds.
  - T3: an arrow fires on the jump; the answer is to walk under its lane. **NJ**
  - T4: a collapse one step ahead; the answer is to go back to the last orb, refresh the timer, and take the upper route. **B**
- **Act 3 · The handoff.**
  - T5: the inversion ends **mid-air** over the last gap: you jump inverted and must switch your held direction at the blink's end to land. It's the level's one precision beat (band 2 threshold), and it's fair because the blink shows exactly when.
  - T6: the door retreats right, back toward the orbs. It's the level's one retreat.
- **Dead ends:** an upper "shortcut" with an orb right at a pit edge (Dies); a quiet nook (Recovers).
- **Kit:** inverter chain sources (PAX-089); duration 25–500; cue plus blink. No new kit.

## L13 · Old Faithful (geyser)

**Verb: count.** Two geysers on different rhythms (100 and 150 ticks). The way up needs the rare tick when both blow together.
**Shape:** 32 × 24, four storeys, start bottom-right, door top-left. ~40 s.

- **Act 1**
  - T1: the first ride. The obvious steer lands on a ledge that collapses on touch (P4); the other ledge is the right one. **OL**
  - T2: two geysers side by side. The one erupting now launches you into ceiling spikes, revealed mid-flight with a lead of at least 6. The right one is the one you wait for. **W**
- **Act 2**
  - T3 ⟲T2: you've learned to wait, but waiting next to the vent drops a ceiling block after 60 ticks. There's one tile you can safely wait on. **SS**
  - T4 · **The sync** (chaos): three geysers stacked in a column. A ride from one enters the next's column and is launched again, a 12 u climb. It only works when they erupt together, every 300 ticks. Count through the bubbling tells.
- **Act 3**
  - T5: near the top, a Down geyser in the ceiling blows you back into the shaft. Leave the column early and steer to the door ledge. **B**
- **Dead ends:** a high alcove an early ride reaches (Recovers, drop back down); a ledge under ceiling spikes (Dies).
- **Kit:** geysers are periodic and can't start chains; each launch envelope must stay inside the frame; stacked columns need the envelope checked per geyser.

## L14 · Root System (vines)

**Verb: go down.** Every vine says "climb up". The door is at the bottom.
**Shape:** 32 × 26, start top-middle, door bottom-right. Descends. ~35 s.

- **Act 1**
  - T1: the start ledge ends at a shaft; falling straight down kills you on the spikes at the bottom. The answer: grab a vine mid-fall (an air grab, pushing up or down). **SS** (hold on)
  - T2: the obvious vine snaps as you climb down past its middle (Dies). The real one is further out: leap across from the first before it snaps. **J**
- **Act 2**
  - T3 ⟲T2: you've learned "snap vines: leap early". This vine is solid, and leaping early sets off an arrow lane. Stay on and climb to the bottom. **NJ**
  - T4: a spear fires across the vine's column as you climb down. Climb back **up** out of its lane, wait for it to stick, then leap from the vine onto it: the spear is the only way to the exit ledge. **B**
- **Act 3 · The canopy** (chaos): three vines in a row, chained to snap 40 ticks apart as soon as you grab the first. Swing from vine to vine faster than they fall. It's the level's one execution beat, with a window of at least 12.
- **Dead ends:** a vine that climbs *up* to a lit alcove (Recovers); a vine whose top meets a ledge with hidden spikes (Dies).
- **Kit:** snap vines as chain sources (PAX-089); full-storey vine roots for coverage. Air grab needs |Climb| ≥ 0.5.

## L15 · Hunted (storm cloud)

**Verb: keep moving.** The cloud wakes one step after the checkpoint, and from then on the question is where you're allowed to stop.
**Shape:** about 48 × 14, wide enough for camera follow, because it holds one precision section (D-069 allows wide rooms with one). Left → right. ~40 s.

- **Act 1**
  - T1: a gap to jump. Stopping to measure it gets you struck. **NW**
  - T2 ⟲T1: you've learned to keep moving, but a pit opens one step ahead and forces a stop. The only survivable place to stop is under the real overhang, 2 u back. **B**
- **Act 2**
  - T3: two overhangs. The nearer one is fake (lightning passes through it); the real one is further. **LW**
  - T4 · **The cave-in** (chaos): as you run, the ceiling behind you comes down block by block, chained to your passage. There's no going back to cover.
- **Act 3**
  - T5: the precision run: thin platforms over a pit while the cloud charges. The hops are timed so you pass under its target line between strikes.
  - T6: the door retreats, out of the cloud's range. It's safe once you're there.
- **Dead ends:** a covered alcove that's a dead end (Recovers, but the cloud is waiting when you come out); a fake overhang (Dies).
- **Kit:** one cloud per room; the dodge rule uses precision slack inside the section; door clearance over the whole cloud range.

## L16 · Upside-Down Garden (gravity flips + vines)

**Verb: two surfaces.** Floor and ceiling are both routes. A gravity flip makes you let go of a vine: it's a tool and a trap.
**Shape:** 32 × 14, start in the middle, door on the ceiling at the far left. ~35 s.

- **Act 1**
  - T1: a flip zone puts you on the ceiling. Vines grow up from the floor and their tops reach you. Climb them down, away from the ceiling, to cross a gap.
  - T2: a flip zone crosses a vine halfway. Climbing through it makes you let go and fall "up" into ceiling spikes. Leap off before you reach it. **NJ** (don't climb on)
- **Act 2**
  - T3 ⟲T2: you've learned to leap before a flip zone. The leap lands in a second flip zone that drops you onto a snapping vine. **OL**
  - T4: a Down geyser in the ceiling, which for an upside-down cat is a launch (geysers work upside down). **J**
- **Act 3 · The gravity storm** (chaos): a corridor of flip zones and vines. You bounce between floor and ceiling, grabbing a vine in each gravity, in a fixed order.
- **Dead ends:** a ceiling nook (Recovers); a vine to a fake ledge (Dies).
- **⚠ Kit question:** PAX-087's rule "a grounded cat grabs only by pushing up" assumes gravity down. For a cat standing on the ceiling, "up" points into the ceiling. It needs a small ruling (grab by pushing away from the ground) and a test, before or inside PAX-060.

## L17 · Déjà Vu (spears + chains)

**Verb: plan the way back.** The door hangs right above the start, in view and out of reach. You cross the room, and the way back is built out of what your trip did.
**Shape:** 32 × 16, two storeys, start left on the lower storey, door above the start. Double back. ~40 s.

- **Act 1 · Out** (lower storey): spears, collapses and hidden spikes, each fired by your passage. Some leave useful results (a stuck spear as a step); some leave bad ones (spikes that stay up, a block that plugs a climb).
- **Act 2 · The rearrangement** (chaos): at the far end, one trigger fires a chain that rearranges the room while you watch: arrows, collapses, a volley. It's the level's one BAIT: step back into the safe nook and let it finish.
- **Act 3 · Back** (upper storey): the return route exists only if the way out left the right steps behind. The memory twist: one trap on the way out **must not** be fired (it plugs the return), and one must be fired on purpose (its spear is the last step to the door).
- **Traps:**
  - the unavoidable spear-stair (NJ);
  - "don't fire the block" (take the ceiling-hugging route out, **LW**);
  - fire the spear on purpose (**J**);
  - on the return, the spikes you saw come up are still up: the route jumps them (≤ 1.5 u, guide §4b);
  - the one BAIT.
- **Dead ends:** an upper-storey shortcut back that's cut off (Dies); a nook (Recovers).
- **Kit:** chain results stay until the room resets; no new kit. It needs careful route validation, since both directions go through the same traps.

## L18 · Eye of the Storm (cloud + geyser + vine)

**Verb: get above the danger.** Lightning only strikes downward. A cat above the cloud can't be hit.
**Shape:** 32 × 22, start bottom-right, door top-left above the cloud's height. Climb. ~40 s.

- **Act 1**
  - T1: the cloud wakes; the ground has no real cover. The obvious overhangs are fakes. **NW**
  - T2: the answer: ride a geyser past the cloud's height onto the sky ledges. **J**
- **Act 2 · Above the storm**
  - T3: the sky ledges collapse on touch, dropping you back under the cloud, straight into its charge. Keep moving between them (P4 landings). **NW**
  - T4 ⟲T2: the vine up to the next sky storey climbs *through* the cloud's height. Start climbing just after a strike, never during a charge. **W**
- **Act 3 · The downdraft**
  - T5: a Down geyser at the top blows you back below the cloud line. Its rhythm and the cloud's cycle have to line up. **Chaos:** lightning below you, collapsing ledges around you, the downdraft above.
- **Dead ends:** a cloud-height ledge (Dies to a strike); a sky alcove (Recovers).
- **Kit:** a cat above the cloud's bottom is safe (D-090 Q4); geyser envelope inside the frame; one cloud.

## L19 · Muscle Memory (precision + inverter + spears)

**Verb: execute a memorised input map.** The level's one real execution test, with a mind trick on top.
**Shape:** about 48 × 16, camera follows. A precision section covers most of it. Right → left, descending. ~35 s.

- **Act 1 · The self-building bridge** (chaos): you stand on a spear. The next one flies *below* you and sticks, becoming the next step down. Descend spear by spear. Wait too long and the floor drops away; go too early and there's no step yet.
  - One spear fires at *your* height: jump its lane at its fixed tick (memory, not reaction; the tell is at least 8).
- **Act 2 · The input map:** inverter orbs at fixed points on the run. You learn "inverted from platform 3 to platform 7" and play the run as a memorised map.
- **Act 3**
  - T5: the last jump crosses the inversion's end (as in L12, but now inside a precision section).
  - T6: a hidden-spike landing. The door is on a lower ledge you reach by dropping, not jumping. **NJ**
- **Dead ends:** an upper line of thin platforms that ends at a wall (Recovers); a spear stair that leads into a lane (Dies).
- **Kit:** precision thresholds (0.85 reach / 8 slack, provisional); spear lanes below the stander; inverter windows. Watch the retry cost: this one hurts.

## L20 · The Machine (exam)

**Verb: ride the chain reaction.** Stepping off the checkpoint starts a room-wide machine that runs for about 40 s. You don't dodge the machine; you move with it.
**Shape:** 32 × 24, start in the middle, a spiral: out right, up, back left over the start; the door is above the start. Double back. ~45 s.

- **The machine**, one chain, in a fixed order (every earlier lesson once, in a new order; the exam is exempt from P8):
  1. an arrow sweeps the start floor;
  2. the floor ahead collapses one step early;
  3. a spear volley builds a stair up the right wall;
  4. at the top, grabbing the vine snaps it and triggers the inverter below;
  5. falling blocks close the way back;
  6. the storm cloud wakes over the top storey;
  7. a geyser, erupting on its own rhythm the whole time, is the only way up to the door.
- **The trick:** every element is only passable at one moment. Too early and you're in a lane; too late and the floor's gone. Moving *with* the chain, the room clears in front of you like a key turning.
- **The finale:** the door retreats once, into the geyser's column. Count the rhythm you've been hearing for 40 seconds.
- **Dead ends:** two, one per storey (Dies and Recovers).
- **Kit:** inverters and snap vines as chain sources (PAX-089); geysers and clouds can't start chains, so they run on their own clocks and the machine is timed around them.

---

## Band 2 targets (proposed for PAX-060; you confirm)

- Lethal betrayals ≥ 8 per level, ≥ 6 in sequence; tempting dead ends ≥ 2.
- Solution 30–45 s once known, in 2–3 acts; late-death replay per act ≤ ~15 s *if gateways are approved* (below).
- Precision sections: in L12 (one beat), L15, L19 only; everything else stays troll plus memory.
- Every rule in guide §2 still applies with band-2 thresholds (lead ≥ 6, windows ≥ 8 inside precision sections, ≥ 12 outside).

## The one open decision: the retry cost

Long, chaotic levels plus "death resets the room" means a death in act 3 replays 30+ seconds of solved chaos. Level Devil avoids this with 5 short stages per door. Options:
- **(a) Gateways (recommended):** each act is its own room, with a checkpoint between acts. Death restarts only the current act. The runtime already supports several rooms per level (`RoomManager`, door → checkpoint N+1), but the level pipeline (one room per `LevelLayouts` entry, one baked camera frame, D-063) needs a kit ticket first. It would reopen D-063.
- **(b) One long room per level,** as now. Late deaths replay 30–45 s; the playtest (PAX-070) judges it.
- **(c) Shorter levels** (~20 s), losing the "longer" you asked for.
