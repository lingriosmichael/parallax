# Needed assets (from the PAX-V07 overnight gauntlet)

Art that code can't fix. Each entry names the clip, the frames and the problem, with the measurements behind it.
Nothing here was generated: the overnight run spent no AutoSprite credits and changed nothing in `Art_Source/`.
Numbers are phone px at 80 px/u (pp) or sprite px at PPU 143.3 (sp). The evidence sheets are in
`Docs/V_TASKS/PAX-V07_gauntlet/item<N>/`.

## Item 1 · Ground (CatA_Idle, CatA_Walk, CatA_Run, CatA_Turn)

1. **Walk↔Run bridge frames (new, 2–3 frames each way).**
   - Problem: `CatA_Walk`'s body stands about 12 sp higher than `CatA_Run`'s (centroid 57–60 sp above the paw row
     against 44–49), and Walk's tail is vertical while Run's is level.
   - Effect: every switch pops.
     - Walk → Run 1: 7.8–9.0 pp.
     - Run → Walk while braking: 5.9–9.5 pp.
     - No pair among the 110 frame pairs pops less than 5.6 pp.
   - The critic rated this "obvious" in both rounds.
   - Wanted: the body lowers and the tail drops from vertical to level, registered to Walk 2 and Run 1 at the ends.
2. **A run brake (new, 3–4 frames).**
   - A gallop-to-walk stop that ends on a planted stance (Walk 1 or Walk 7's pose).
   - The code already routes every run stop through Walk 1/7, so this removes the last Run→Walk height pop on stops.
3. **Turn redraw (`CatA_Turn`, today 3 frames → 5–7).** The current clip:
   - pops against itself: Turn 0→1 is 4.9–5.1 pp, 26 times in the captures;
   - pops at its seams: Walk→Turn 3.2–5.1 pp, Turn→Walk 5.2–5.5 pp, Turn→Run 5.5–5.6 pp;
   - is 20–35 % smaller in opaque area than Walk, so the cat "blinks" to half its width for 0.1 s;
   - has a pale inner rim along the spine (Turn 1), is softer, and curls the tail down;
   - is drawn as side → rear → front three-quarter, so at phone scale it reads as a strobe, not a turn.
   - Wanted: 5–7 frames in Walk's style and size, starting from Walk's pose and ending on the mirrored Walk pose, with
     the paws stepping (not fixed).
4. **Walk-to-stand (new, 2–3 frames for each half of the walk cycle).**
   - Why: a release at 1.8–3.0 u/s stops the body within 0.02–0.056 u, less than one Walk frame's stride. So a walk
     stop ends on whatever frame is showing.
   - Only Walk 1 and 7 match Idle's paws (within 1.5–5.5 sp). The other frames miss by 16.5–41.5 sp, so Idle moves
     the legs in one frame (the critic's Walk→Idle pop).
5. **Idle to match Walk/Run (`CatA_Idle`), or the reverse.**
   - Idle is drawn differently: textured coat, pink ears, no visible eye. Walk and Run have a flatter coat and glowing
     orange eyes.
   - Idle is about 9 % bigger (8349–8392 opaque sp² against 7159–7839).
   - Every start and stop shows the style change, and Idle's rim is noisier.
6. **Detached specks (Idle, Walk, Run, Turn).**
   - Every frame has 65–188 detached low-alpha pixel groups (whisker and fur fringe).
   - The outline shader rims them, so single orange pixels float next to the face and back.
   - Fix: clean the sheets' alpha, for example by dropping islands under a size and alpha threshold in
     `cat_register.py`, or add an alpha cutoff to the outline shader. The shader is outside V07's file list.

## Item 2 · Air (CatA_TakeOff, Rise, Apex, Fall, Land, HardLand)

1. **Landing impact and recovery.**
   - Fall→Land pops 15.3 pp and Fall→HardLand 18.8 pp. There is no impact/contact frame between the air pose and the
     crouch.
   - Land's own 3 frames pop 4.8–6.5 pp at each step.
   - HardLand 3 → Land 0 pops 7.8–8.3 pp, 14 times: no frame bridges the flat crouch to the raised crouch.
   - Land 1's curled tail meets Walk's upright tail at 8.1–10.5 pp (the quick-land bridge into Walk).
   - Wanted: 1–2 contact frames per landing kind, and a HardLand recovery that ends on Land 0's pose (or on Idle).
2. **HardLand sprite.**
   - The tail tip is cut square at the cell edge in HardLand 2. A08 recorded the source as `sourceCutAtCellEdge: true`
     with `clippedPx: 0`: the AutoSprite source was already cut.
   - HardLand 1 has a loose outline tick right of the tail, and a lighter, softer body than Fall and Run.
   - HardLand 0 can't be used after a dive.
3. **Takeoff.**
   - TakeOff 1 → Rise 0 pops 9 pp.
   - From a full run, TakeOff 0 is an upright squat that reads as braking; a running takeoff (a stride push-off) is
     missing.
4. **A straight-up Rise/Fall.** Rise is a forward pounce (Rise 0 is a diagonal leap), so a jump in place looks like it
   should travel.
5. **Rise→Apex (4.3–4.8 pp) and Apex→Fall (4.6–5.4 pp) in-betweens.**
6. **A step-down / walk-off pose.** A walk-off shows the full leap pose on its first air frame, so a 1 u step-down reads
   as a jump.
7. **A longer Fall loop.**
   - Long falls (3.2–4.8 u) hold Fall 2 for 15–20+ frames, a frozen cut-out.
   - Wanted: a 2–4 frame falling loop (fur and tail flutter).
8. **Geyser launch.**
   - Idle → Rise 0 cuts, then Rise 0 holds for about 20 frames while the cat shoots up.
   - This belongs to the parked `Launched` state (22 launched, "redo first"). Until that ticket, it shows Rise.
9. **Specks.** A detached outline speck on Rise 0 (it stays at the takeoff spot for 5–8 frames) and on Land 1. The same
   cause as item 1 entry 6.
10. **Pose size against the collider (not art-only).** Every pose is taller than the 0.56 u collider. The ear tips touch
    the 0.7 u slabs during Apex, and an Idle cat's ears draw into L011's 0.7 u gap for 59 frames. A08's accepted
    trade-off (the ear tips touch the 0.70 u ceilings) covers it; listed so it isn't forgotten.
