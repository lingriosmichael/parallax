# Needed assets (PAX-V07, updated by PAX-A14)

This lists only art that is still visibly wrong after V07's code fixes and A14's offline art pass (2026-09-30: the
consistency pass and in-betweens made from the existing frames; nothing generated). V07 doesn't hide art in code (ruled 2026-09-30):
no pose is drawn off its registered pivot, and every entry below shows in game as described.

Nothing here was generated: no AutoSprite credits were spent and nothing in `Art_Source/` changed.

Units: pp = phone px at 80 px/u; sp = sprite px at PPU 143.3.

## Ground

1. ~~Walk↔Run bridge~~ **Fixed by A14:** Run was drawn about 18 % small (its head 27 px against Walk's 33). Scaled to Walk's
   torso length, every foot-matched switch moves the body 2.6–3.8 pp (it was 7.7–8.8). No bridge frames needed.
2. **Run brake (new art, 3–4 frames).** A gallop-to-walk stop that ends on a stance (Walk 1 or 7's pose). A run stop
   still pops 6–9 pp where Run hands over to Walk.
3. **Walk-to-stand (new art, 2–3 frames per half cycle).**
   - A stop from walking speed ends on whatever Walk frame is showing, because the stop takes less than one stride.
   - Only Walk 1 and 7 match Idle's paws. Every other frame misses by 16–42 sp, so Idle moves the legs in one frame.
4. **A turn drawing (3–5 frames, optional).**
   - The turn now flips on the most symmetrical frame of the gait on screen and holds it one frame (ruled 2026-09-30).
   - It no longer strobes, but the final critic still reads it as an instant mirror.
   - `CatA_Turn`'s three drawings (side, rear, front three-quarter) aren't symmetric and are 20–35 % smaller than Walk, so
     they aren't played.
5. **A push-against-a-wall pose.** Running into a wall ends in a relaxed Idle while the stick still pushes.
6. **Idle to match Walk/Run (`CatA_Idle`), or the reverse.** A14 matched Idle's tones to Walk's (darker, less red), and its
   body height already matches (81 px at the sheet for both). What remains is the drawing: Idle's coat is more textured and
   its ears pinker than Walk's flat coat.

## Air

7. **Landing contact.** A14 added one contact frame per landing (the crouch with its legs part extended): a normal landing
   now goes Fall 2 → contact (8.7 pp) → Land 0's crouch (6.7 pp) → stand, and a hard landing Fall → contact (11.7–12.2 pp)
   → HardLand 1 (7.9 pp); it recovers HardLand 3 → Land 0 (8.3 pp). One frame can only halve the 15–19 pp compression.
   Wanted: a second, drawn contact frame for each (fore paws touching, rear still up), to get every step under 4 pp.
8. **HardLand sprite.**
   - HardLand 2's tail tip is cut square at the cell edge. The source was already cut (A08: `sourceCutAtCellEdge`).
   - HardLand 1 has a loose outline tick right of the tail, and a lighter, softer body than Fall and Run.
9. **Takeoff.**
   - TakeOff 1 → Rise 0 pops 9 pp.
   - From a full run, TakeOff 0 is an upright squat that reads as braking. A running takeoff is missing.
10. **A straight-up Rise/Fall.** Rise 0 is a diagonal forward pounce, so a jump in place looks like it should travel.
11. **A step-down / walk-off pose.** A walk-off shows the full leap pose on its first air frame, so a 1 u step-down reads
   as a jump.
12. **A falling loop (2–4 frames).** Long falls (3–5 u) hold Fall 2 for 15–20+ frames, a frozen cut-out.
13. **Geyser launch.** Idle → Rise 0 cuts, then Rise 0 holds for about 20 frames. This belongs to the parked `Launched`
    state (22 launched, "redo first").

## Air (continued)

- **Apex in-betweens.** Rise 2 → Apex 0 snaps from about 40° to horizontal, and Apex 1 → Fall 0 from a long stretch to a
  compact pose.

## Gravity flip (`CatA_Flip`)

- **A tuck and an uncurl.** A14 turned 4 in-between roll cels out of the drawn ones (8 cels at 24 fps, still 0.33 s; every
  step 1.9–3.4 pp, it was 4.2–10). The ends remain: a stretched Apex/Fall/Walk pose still becomes a ball in one frame, and
  the ball a spread Fall in one frame. The flip starts from any pose, so one generic tuck needs drawing, not a turn.

## Climb (`CatA_Climb`, `CatA_Hang`, `CatA_Leap`)

- **Grab and release in-betweens.** The side-view cat and the back-view climb have nothing between them. Grabbing from
  the ground or a jump, and letting go at the top or bottom, are one-frame pops (Walk → Climb 14.2 pp, Fall → Climb
  18.5 pp). A14 tried a rearing grab (Walk 1 turned upright, 7.0 + 7.1 pp) and dropped it: placed between the poses, its
  hind paws draw about 0.3 u into the floor at a vine's foot, and it needs new timed-bridge code.
- **Climb down (`09_vine_climb_down`, kept).** A14 measured it against Climb played in reverse (the current climb down) and
  didn't wire it: keying out the vine, which runs through the cat's body, cuts the body and head; its largest step is
  5.6 pp against Climb's 1.5, and its loop closes at IoU 0.75 against 0.89. Wanted: a redraw on a clinging-to-vine base.
- **A dismount at the vine's foot and at the top.** Climbing down ends in the floor, then pops to Idle. In gravity up, it
  pops to the upside-down Idle.
- **The climb pose against the collider.**
  - The vertical pose is centred on the 0.56 u collider, so at a vine's foot its hind paws and tail are drawn up to
    55–58 sp into the floor. In gravity up, it's drawn into the ceiling.
  - V07 no longer lifts it: its checks report these frames without failing them.
- **A hang-from-the-hands pose.** Grabbing a vine's bottom end from a jump, the climb cycle steps on air.
- **A leap push-off.** Leap 0 is a head-down dive with vine residue, so Leap starts on frame 1: the vertical hang snaps
  to the stretched leap.
- **Fewer, bigger key poses for full speed.** At 4 u/s the 25-frame loop advances about 3 frames per display frame. V07
  now steps through every third frame, regularly, but the limbs still read as a blur.

## Idle, deaths and door

- **A stand-up from the sit (2–3 frames).** Any input ends the sit on the same frame (§3), so the cat goes from seated
  straight to Walk.
- **`CatA_Death_Arrow`.** Frames 2–3 hold the cat about 0.5 u in the air, then frame 4 lies on the floor: a mid-air hang,
  then a drop, with no fall in-between. The arrow isn't carried in the drawing.
- **`CatA_Death_Spiked`.** A14 re-registered it onto the spike edge (+0.44 u: the upright frames' paws now meet the spikes
  where the kill happens in Trap Lab room 0). It still collapses from an upright 1.1 u pose to lying in one frame.
- **`CatA_Death_Crushed`.** A pancake squashed from above; the crusher closes from the side.
- **`CatA_Door`.** It reads as pawing at the door rather than celebrating, and frames 0–1 rear with the hind paws off the
  floor.
- **Style.** A14 matched the death, door, respawn, fidget and air clips' tones to Walk's; their eyes are dimmer now, and
  their fluffier drawing stays. `CatA_Death_Zapped` keeps its own lighter shading: matched, its electric glow turned into a
  grey smudge.

## Not art, and not V07 code (for the developer)

- **Draw order.**
  - The pit death is hidden behind the collapsing floor's 24 debris sprites.
  - The crusher death is mostly hidden by the room's sprites.
  - The storm cloud's glow turns the cat into a pale ghost for about 0.3 s.
  - The cat's sorting against the trap art is room and trap-art setup (PAX-A13), outside V07.
- **The body's rotation on a gravity flip.** On the first frames of a flip, the cat is drawn at about 90° for 3–4
  frames, so the roll's first cel appears turned. This comes from the body's own rotation (the motor and Rigidbody2D
  interpolation), which V07 doesn't touch (`CatMotor2D` is out of scope).

## Every clip

14. ~~Detached specks~~ **Fixed by A14:** `cat_register.py` removes every detached island with no drawn pixel (1,371 across
    the sheets); 0 remain. Detached pieces with drawn pixels (the zap's sparks, an arrow frame's tail tip) stay.
15. **Pose size against the collider.**
    - Every pose stands 0.1–0.5 u taller than the 0.56 u collider, and the tail and hind paws reach past it.
    - Now that no pose is drawn clear of solids:
      - ears touch 0.7 u ceilings and slabs (up to 7.6 sp in the gravity-up standing jump);
      - the tail tip enters walls the cat stands against;
      - Rise 0's hind legs (0.25 u below the paws) can meet a step behind a cat jumping up.
    - A08 accepted the ear case. Listed so the rest isn't forgotten.
