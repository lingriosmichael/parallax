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
