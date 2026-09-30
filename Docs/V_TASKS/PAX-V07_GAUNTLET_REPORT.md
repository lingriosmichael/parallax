# PAX-V07 · Overnight gauntlet report

Started 2026-09-30. It runs unattended in batch mode on an APFS clone of the project (no Unity MCP). The report is
updated after every item, so it's current even if the run stopped early.

## Status

| Item | Status | Rounds | Commit |
|---|---|---|---|
| 0 · Capture harness | **BLOCKED** (usable; the death-hold visibility defect was named in two rounds) | 2 | item 0 commit |
| 1 · Ground | pending | – | – |
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



## Full suite

(pending)

## Needed assets

See `Docs/Art/NEEDED_ASSETS.md` (created only if an item needs new or regenerated art).

## Your play checks (Editor, by hand)

(pending)
