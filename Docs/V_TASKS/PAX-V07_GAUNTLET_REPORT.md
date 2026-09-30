# PAX-V07 · Overnight gauntlet report

Started 2026-09-30. It runs unattended in batch mode on an APFS clone of the project (no Unity MCP). The report is
updated after every item, so it's current even if the run stopped early.

## Status

| Item | Status | Rounds | Commit |
|---|---|---|---|
| 0 · Capture harness | **BLOCKED** (usable; the death-hold visibility defect was named in two rounds) | 2 | item 0 commit |
| 1 · Ground | **BLOCKED** (+ art needs listed; the switch pops and turn strobe were named in two rounds) | 2 | item 1 commit |
| 2 · Air | pending | – | – |
| 3 · Climb | pending | – | – |
| 4 · Gravity | pending | – | – |
| 5 · Idle fidgets | pending | – | – |
| 6 · Deaths | pending | – | – |
| 7 · Door | pending | – | – |

## Phase 1

The trace and 16 rulings are in `PAX-V07.md` §11, "Rulings (overnight, decided by Claude Code)". The ones that change
what you do in the morning:
- **R10:** the setup menu's outputs were made on the clone only. Run **PARALLAX → Setup → Cat Visual** once, save,
  and commit `Cat_Player.prefab`, `CatA_VisualConfig.asset` and `CatA_DeathClips.asset` (with their `.meta` files).
- **R11:** hazard kinds reach saved scenes only through a rebuild: run **Rebuild All Levels** and the Trap Lab setup.
- **R1:** the presenter finds `RoomDeath`/`RoomManager` by a lookup in the cat's own scene, so no scene wiring.
- **R5:** "any input" cancels a fidget through the cat's motion (the driver exposes no control sample).
- **R7:** Climb uses the 25-frame loop from A08, not a 5-frame one.

## Baseline

Full EditMode suite in batch on a clone of HEAD 93a2bc6 (before any V07 change): **1517 total, 1517 passed, 0
failed** (matches A08's 1517). `CheckpointSectionPlayModeTests` stays out of batch, as always.

## Items

### Item 0 · Capture harness: BLOCKED (usable)

**Rounds:** 2. Round 1's critic rated the capture view **fair** (no world reference, apex crops, missing transitions,
mislabelled scale). Round 2 fixed all eight and rated it **good**. It stops here under the brief's rule: the critic named
the cat hidden behind the collapsing floor in the pit death in both rounds (round 1 #7, round 2 #5). That is how the
game draws it (the shards are sorted above the cat). The capture adds a cyan outline on hold frames, but not on the
frames just before the hold. The harness is complete and is used by every later item. The item 1 builder was asked to
fix the small tool gaps round 2 listed (the 4× strip width, the missing phone-scale page 2, the missing takeoff and
Land→Walk sheets, the truncated header, and a caption for the orange rim and the tool's own crop).

**What it is** (`Docs/V_TASKS/PAX-V07_gauntlet/HARNESS.md`):
- A batch entry, `Parallax.Editor.Art.CatCapture.Run -captureOut <dir> [-captureFilter re]`, plus the menu **PARALLAX
  → Art → Cat Capture**, which writes to `Logs/CatCapture/`.
- It builds its own rig from the route harness's parts: `SoloRoomBuilder`, the real `Cat_Player.prefab`, and scripted
  analog input through the real `CatInputRouter`. It runs 50 Hz ticks with 60 fps frames between them, at the pose
  Rigidbody2D interpolation would show, and records the presenter's state beside the motor's for every frame.
- A self-check replays every scenario with ticks only. The body's position and velocity match exactly on every tick.
- It renders on the GPU in batch mode, with a world grid and surface ticks drawn onto the images.
- `Tools/Art/cat_capture_sheet.py` makes an overview, one sheet per state change, and 4× paw strips.
- Automated checks: paws in the floor, float gap, foot slide, pose pop, and a rules table comparing the presenter's
  state with the motor's.
- New files: `Assets/_Game/Editor/Art/CatCapture*.cs` (7 files), `Tools/Art/cat_capture_sheet.py`, and
  `Tests/EditMode/CatCaptureTests.cs` (17 tests). The presenter gained `Present(dt)`, called from `LateUpdate` after its
  visibility check, and read-only `State`, `ClipName`, `FrameIndex` and `Facing` (§11 R9).

**Automated checks on today's presenter (the "before" for items 1–4).** sp = sprite px; pp = phone px at 80 px/u. All
four scenarios fail.

| Scenario | Paws in floor, max (sp) | Float gap, mean (sp) | Foot slide, max / mean (pp) | Pose pop, max (pp) | State rules broken |
|---|---|---|---|---|---|
| traplab0_walk_down | −0.9 | 1.6 | 36.1 / 12.6 | 15.4 (8 over) | 0 |
| traplab3_walk_up | −1.6 | 1.7 | 22.1 / 11.3 | 15.4 (10 over) | 0 |
| L001_solution_down | 0.3 | 1.8 | 15.9 / 11.2 | 15.4 (17 over) | 0 |
| traplab2_pit_down | 7.9 (2 frames over) | 1.6 | 13.2 / 10.5 | 15.4 (4 over) | 0 |

**Tests:** red first, with stubs (15 of 15 failed for the stubbed reason). Green: 86 of 86 across CatCaptureTests,
CatAnimStateMachineTests, ClimbPoseTests, ClimbInputTests and CatSheetImportTests. Two parts were written together
with their tests rather than red first: `SelectStepCuts` and `SelectEventCuts`.

**Decisions:**
- Gravity up uses Trap Lab room 3, because room 0's ceiling has falling blocks and rooms 1–2 have a ceiling hazard.
- The L001 inputs are taken tick for tick from the route harness's replay of its solution.
- The camera holds the last standing height so the floor stays in view during jumps.
- Checks use the body sprite's alpha, not the outline.
- The state rules skip the death hold and the tick after a respawn, when the motor's grounded flag is stale.

**Final sheets:** `Docs/V_TASKS/PAX-V07_gauntlet/item0/` (phone-scale overviews of all four scenarios, and a landing in
each gravity).

**Critic's last verdict (round 2), word for word:**

## 1. Defects in the capture view

1. **The 4x strips crop the tail tip in the Rise frames.** `traplab0_walk_down__strip_f00461_takeoff.png` f461–f469 and `traplab3_walk_up__strip_f00501_takeoff.png` f501–f509: the tail runs off the right edge of the panel and its tip is cut flat (confirmed at full resolution on f461, f462, f501 and f502). The Land frames in the landing strips (f411–f418, f450–f457) fit, but with only a few pixels to spare. The strip crop (about 1.6 u wide) is too narrow for the Rise pose. **Noticeable.**
2. **The phone-scale overviews for the two walk rooms are incomplete.** `traplab0_walk_down__overview_phone_p1.png` and `traplab3_walk_up__overview_phone_p1.png` both say "part 1/2" in their headers, but there is no p2 in the folder. So frames 384–540 (gravity down) and 384–576 (gravity up) never appear at phone scale: every jump-in-place and run-jump, both landings and the gravity-up takeoffs. **Noticeable.**
3. **Some transitions have no 2x sheet or strip** (traplab0 has no t02/t04/t06–t08; traplab3 has no t00/t01/t04/t07–t09): Idle→Rise takeoff for the jump in place, both gravities (traplab0 ~f366–f372, traplab3 ~f408–f414); Land→Walk (traplab0 f504→f510, L001 f84→f90); the first ceiling landing after spawn in gravity up (traplab3 f36→f42→f48); L001's later landings (f210/f216, f303, f348→f354 at the door). **Noticeable.**
4. **The pit header is truncated** in `traplab2_pit_down__overview_phone.png`. **Subtle.**
5. **The cat is hidden just before the death hold.** `traplab2_pit_down__t05_f00152_death.png` f146–f151 (and overview f144/f150): the cat is almost completely hidden behind the floor shards, and the cyan outline only starts at f152 (HOLD). At phone scale (pit phone f150–f186) the outlined cat is hard to make out among the shards. **Noticeable.**
6. **The orange rim isn't explained.** Every cat has a jagged orange halo, plus loose orange specks (takeoff strips f454, f457, f461, f463; gravity-up strip f495–f509); captions don't say whether it's in the game or a capture overlay. **Subtle** (labelling).
7. **The framing isn't identified.** Panels are a ~4.8 × 3.6 u crop following the cat with some lag (traplab0 t01 f150–f174 drift ~0.15 u); nothing says whether it's the level camera or the tool's crop. Within the crop the motion is smooth, no jerks. **Subtle.**

These worked: scale matches the stated px/u; paws sit on the floor and ceiling; gravity up correctly shown upside down; the grid, surface ticks, walls, pillar and door make any slide visible; changes framed in red and labelled with state and clip, 8 frames before and 24 after each change.

## 2. Animation defects noticed (not scored)
- Walk clip index scrambles when speed changes (t01 f150–f153 Walk[5]→[8]→[1]→[4]→[5]; t03 f241–f244 [1]→[5]→[9]→[6]; traplab3 overview f276→f282 [9]→[4], f372→f378 [9]→[2]; traplab0 overview f330→f336 [9]→[7]).
- Full run at 6 u/s still uses the Walk clip, no run gait (t01 from f150).
- Land→Idle pops from a low crouch to a tall stance, tail jumps (traplab0 strip f418→f419, traplab3 t11 f457→f458).
- Rise→Fall is a one-frame pose swap (t09 f392→f393). Rise[0], Fall[0], Land[0] each a single held frame.
- The Rise pose is a forward diagonal leap even when jumping in place (t09 f385–f392); same mirrored gravity up.
- Idle→Walk pose pops (t00 f60→f61, t05 f274→f275: tail snaps low curl → straight up).
- The cat walks on air past the ledge (L001 strip f283–f286, front paws past the edge 4 frames before Fall at f287).
- Land on flat ground mid-walk (traplab2 overview f114–f120).
- The death hold keeps the Fall pose, no death reaction (f152–f187).
- Sprite fringe: orange matte halo and stray specks; reddish pixels at tail tip and hind leg in Rise frames (f461–f462, f501–f502).

## 3. Rating of the capture view
**Good.** The view is faithful where it counts. Scale, contact with the floor and ceiling, upside-down gravity, a world reference that makes slides visible, and clear red-framed transitions labelled with state and clip all work. It falls short of excellent for three reasons: the Rise tail is cropped in the 4x strips, the jumps and landings are missing at phone scale, and several pop-prone transitions (jump takeoff, Land→Walk, the first ceiling landing) have no dense sheet. Those gaps leave exactly the frames most likely to hide a pop unjudged.



### Item 1 · Ground: BLOCKED (the rest needs art)

**Rounds:** 2. Round 1 was rated **poor**: every start launched mid-air, every run stop snapped to standing,
decelerating frames froze, and a one-frame Idle flashed. Round 2 fixed all of those in code and was rated **fair**. It
stops under the brief's rule because the critic named the same defects in both rounds: the silhouette pops at every
Walk↔Run switch, the Turn clip, the strobing on quick inputs, the specks, and the nose over the wall. All but the
strobing are art (`Docs/Art/NEEDED_ASSETS.md`, item 1, entries 1–6). The strobing on quick inputs comes from the
3-frame Turn meeting 2-frame minimum states, so it goes away with the Turn redraw. Round 2's code is kept.

**What it does now:**
- **Foundation for all later items.**
  - `CatAnimInput` / `CatAnimSettings` and `CatAnimStateMachine.Step(in CatAnimInput)`. The old overloads are
    untouched (§11 R4).
  - `CatClipSet` covers every wired A08 slot, with per-sheet cells and the shared world pivot.
  - `CatVisualSetup`'s clip list is a data table (639 lines, allowed by R12), and the setup was re-run on the clone.
  - A `CatPresentationSignals` skeleton, and `CatVisualConfig` gained the run, walk and brake fields.
- **Walk and Run play by distance.** Each frame lasts its own stride, measured from the sheets, so planted paws stay on
  their prints at any speed and a loop keeps its phase when the speed changes.
- **Thresholds.**
  - Run from 0.6 × MaxSpeed (3.6 u/s), back to Walk below 0.52 × (3.12 u/s). Analog 0.5 walks at 4.1 strides/s.
  - Walk enters above 0.6 u/s and leaves below 0.5 u/s (was 0.15/0.1). Below that a Walk frame would hold 6+ frames
    while the body glides.
- **Starts:** a Run can only start on its push-off frames, Run 1 or 2.
- **Stops:**
  - A run stop walks the brake out on Walk frames timed so the cat stops on a stance, Walk 1 or 7, which match Idle's
    paws. Walk→Idle then pops ≤ 2 pp.
  - A stop from walking speed ends on whatever frame is showing (art entry 4).
- **Minimum state length:** Idle, Walk and Run show for at least 2 frames, and a harness rule fails any one-frame
  state.
- **Turn:** 3 frames at 30 fps. The facing flips at the end, or at once if something interrupts it. There is no Turn in
  the air.

**Automated checks (round 2, 12 ground scenarios, both gravities):**
- Pass everywhere:
  - paws in floor (0 over; worst −0.9 sp);
  - state rules (0 broken, including the one-frame rule);
  - capture parity;
  - foot slide (0 plants over the allowance; round 1 had 8; item 0's baseline had 18/18/10/4).
- Pose pops over 3 pp, all at seams the art has no frames for:
  - Turn→Turn 26 (4.9–5.1)
  - Run→Walk 18 (5.9–9.5)
  - Turn→Walk 14 (5.2–5.5)
  - Turn→Run 11 (5.5–5.6)
  - Walk→Turn 10 (3.2–5.1)
  - Walk→Run 7 (7.8–9.0)

  Run→Idle pops fell from 13 to 0. The two idle scenarios pass; the other ten fail only on these pops.

**Tests:**
- Red first: 44 run, 23 failed for the ticket's reasons (for example `Expected: Turn But was: Idle`). The 16 legacy
  `CatAnimStateMachineTests` passed unchanged.
- Green after round 2: 164 of 164, no compiler warnings. That covers:
  - `CatAnimStateMachineTests` (38 cases);
  - the new `CatVisualOnlyParityTests`, bit-identical on every tick with the presenter stepped against not stepped,
    with jumps and reversals pressed during Turn, Walk, Run and Idle, in both gravities;
  - `CatClipTableTests`;
  - `CatCaptureTests`, `ClimbPoseTests`, `ClimbInputTests` and `CatSheetImportTests`.

**Decisions:**
- Stride tables are measured from the art.
- Loop entry frames are picked by pose.
- A hard brake hands Run to Walk at once.
- The gravity-up flip onto the ceiling is rendered but not checked (it's item 2's air).
- The foot-slide allowance is 3 pp plus one 60 fps frame of travel, because a sprite can only change on a frame.
- The setup-data test lives in `CatVisualOnlyParityTests.cs`, since §8 lists no other test file for it.
- The harness gained `CatCaptureParity.cs`.
- Out of scope, noted: pushing into a wall, the nose and outline sit 1–2 px over the wall face (the art reaches past
  the collider).

**Final sheets:** `Docs/V_TASKS/PAX-V07_gauntlet/item1/`.

**Critic's last verdict (round 2), word for word:**

Coverage (critic's note): every overview in both gravities and at least one strip or transition sheet of each transition kind in each gravity (Idle-to-Walk, Walk-to-Run, Run-to-Walk, Walk-to-Idle, Idle-to-Turn, Walk-to-Turn, Turn-to-Walk, Turn-to-Run); not all 200 files.

1. **Defects:**

1. **Every state change is a hard cut with a big silhouette pop.** Worst cases: 078 f30→f31 (Idle[3]→Walk[1]): the tail jumps from a low curl to straight up, the whole body pose changes in one frame (same 083 f30→f31). 079 f152→f153 (Walk[0]→Idle[7]): the tall upright walk pose snaps to the long, low idle pose, tail dropping from high to a low curl. 108/124 f168→f169 (Run[9]→Walk[0]): low stretched runner becomes a tall walker, tail flipping horizontal→vertical. 059 f142→f143 (Walk[2]→Run[1], gravity up): the same in reverse. 171 f56→f57 (Run→Walk) and f60→f61 (Walk→Idle). **Obvious** at phone scale; the tail flip alone reads as a pop in the 80 px/u overviews (101 f168→f174, 077 f150→f156).
2. **The Turn clip is 3 drawings in 6 frames (~0.1 s), each a different silhouette**: side (Turn[0]) → rear (Turn[1]) → front three-quarter crouch (Turn[2]) → full side walk or run (112 f205–f211, 108 f175–f181, 042 f229–f235, 146 f275–f281 up, 068 f107–f113 up, 096 f101–f107 up). The cat's width roughly halves for 4 frames and snaps back: reads as a blink or flicker rather than a turn (tiny upright cat in overviews 101 f84/f144/f180 and 134 f156/f216). **Noticeable to obvious.**
3. **Quick double turn flickers between clips.** 127 f205–f224: Turn (f205–210) → Walk[7] 2 frames (f211–212) → Turn (f213–218) → Run[1] 2 frames (f219–220) → Walk[7] 3 frames (f221–223) → Turn (f224). 130 f219–f224 repeats Run→Walk→Turn. 161 f268–f288 (up): Walk → Turn → Walk 4 frames (f281–284) → Run 3 frames (f285–287) → Walk (f288). Each 2–4 frame clip is a full silhouette change, so the cat strobes. **Obvious.**
4. **Walk→Turn and Run→Turn cut into a Turn[0] pose with a curled tail** (straight up to a curl in one frame): 112 f204→f205, 124 f174→f175, 165 f293→f294 (up), 158 f244→f245 (up). **Noticeable.**
5. **Walking into a wall flashes a frozen walk pose.** 171/178 f57–f60: already against the wall, Walk[7] held 4 frames with no leg motion before Idle; run→walk→idle in 5 frames at a standstill. 187 f127–f130 (up) the same. **Noticeable.**
6. **The cat's face overlaps the wall** (171 f59–f65; 187 f127–f135 up; 192 f267–f275 up). **Subtle** (~1 px at phone scale), every wall-push frame.
7. **Turn sprites don't match the rest of the art**: Turn[1] pale seam down the spine (112 f207–208, f215–216; 108 f177–178; 146 f277–278 up; 042 f231–232); Turn[0]/[1] softer and blurrier (112 f205–206, 108 f175–176). **Subtle** at phone scale, noticeable at 2×.
8. **Idle frames have a noisier, fuzzier outline halo than walk frames** (079 f153–161, 171 f61–65, 192 f267–275); stray single orange outline pixels off the sprite (next to the head 171 f61–f65, above the back 059 f141, below the belly 059 f142). **Subtle.**
9. **Planted paws skate slightly during held walk frames**: 078 f33–f35 (Walk[2] held 3 frames): front paws stay put relative to the cat while the floor ticks move ~10 px at 4× (~2.5 px phone); similar 042 f236–f238 (Walk[7]), 083 f41–f44 (Walk[6]). **Subtle.**

No paw-in-floor or paw-in-ceiling penetration, and no wrong facing within a steady loop. The Idle, Walk and Run loops read cleanly in both gravities, and gravity up matches gravity down.

2. **Rating: fair.** The cat's steady loops look good and hold up upside down. But every clip change is an unblended cut between very different silhouettes, and the turn is a 0.1 s strobe of side, rear and front drawings. Quick inputs (the double turn, a run into a wall) make the cat flicker through 2–4 frame clips. On a store-page video, where starts, stops and turns happen constantly, those pops would be the first thing a viewer notices.


## Full suite

(pending)

## Needed assets

See `Docs/Art/NEEDED_ASSETS.md` (created only if an item needs new or regenerated art).

## Your play checks (Editor, by hand)

(pending)
