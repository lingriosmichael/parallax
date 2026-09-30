# Needed assets (PAX-V07)

This lists only art that is still visibly wrong after V07's code fixes. V07 doesn't hide art in code (ruled 2026-09-30):
no pose is drawn off its registered pivot, and every entry below shows in game as described.

Nothing here was generated: no AutoSprite credits were spent and nothing in `Art_Source/` changed.

Units: pp = phone px at 80 px/u; sp = sprite px at PPU 143.3.

## Ground

1. **Walk↔Run bridge (new art, 2–3 frames each way).**
   - Run's body is drawn about 12 sp lower than Walk's, and its tail is level where Walk's is upright.
   - V07 switches gait only on frames where the feet match (Walk 3/4/9/10 → Run 5, Run 3/5/6 → Walk 3/10). The body
     still drops or rises 7.7–8.8 pp at every switch.
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
6. **Idle to match Walk/Run (`CatA_Idle`), or the reverse.**
   - Idle has a textured coat, pink ears and no visible eye; Walk and Run have a flatter coat and glowing orange eyes.
   - Idle is about 9 % bigger.
   - The style changes on every start and stop.

## Air

7. **Landing contact.**
   - A landing now enters on the frame matching the fall (Land 2: a 6 pp step), so a normal landing has no squash.
   - A hard landing enters HardLand's impact crouch, frame 1 (an 18.8 pp step), and recovers HardLand 3 → Land 0
     (8.3 pp). HardLand 0 is a tall pre-impact stand: it matches the fall better (12 pp) but pops the cat up stiffly, so
     it isn't an entry frame.
   - Wanted: 1–2 contact frames between Fall 2 and each landing clip.
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

- **The roll needs more drawings, plus a tuck and an uncurl.**
  - It is 4 cels at 12 fps, so it steps 45–90° at a time.
  - A stretched Apex/Fall/Walk pose becomes a ball in one frame, and the ball becomes a spread Fall in one frame.
  - Wanted: 6–8 roll frames, with a tuck at the start and an uncurl at the end.

## Climb (`CatA_Climb`, `CatA_Hang`, `CatA_Leap`)

- **Grab and release in-betweens.** The side-view cat and the back-view climb have nothing between them. Grabbing from
  the ground or a jump, and letting go at the top or bottom, are one-frame pops.
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
- **`CatA_Death_Spiked`.**
  - The cat dies 0.1–0.2 u short of the spikes, because of the pose's reach against the pivot.
  - It collapses from an upright 1.1 u pose to lying in one frame.
- **`CatA_Death_Crushed`.** A pancake squashed from above; the crusher closes from the side.
- **`CatA_Door`.** It reads as pawing at the door rather than celebrating, and frames 0–1 rear with the hind paws off the
  floor.
- **Style.** The death and door clips use lighter, fluffier shading and bright orange eyes, while Walk, Idle and Run are
  near-flat black.

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

14. **Detached specks.**
    - Idle, Walk, Run, Turn, Rise 0 and Land 1 carry 65–188 detached low-alpha pixel groups (whisker and fur fringe).
    - The outline shader rims them, so single orange pixels float next to the cat.
    - Fix: clean the sheets' alpha (drop small islands under an alpha threshold in `cat_register.py`), or add an alpha
      cutoff to the outline shader, which is outside V07.
15. **Pose size against the collider.**
    - Every pose stands 0.1–0.5 u taller than the 0.56 u collider, and the tail and hind paws reach past it.
    - Now that no pose is drawn clear of solids:
      - ears touch 0.7 u ceilings and slabs (up to 7.6 sp in the gravity-up standing jump);
      - the tail tip enters walls the cat stands against;
      - Rise 0's hind legs (0.25 u below the paws) can meet a step behind a cat jumping up.
    - A08 accepted the ear case. Listed so the rest isn't forgotten.
