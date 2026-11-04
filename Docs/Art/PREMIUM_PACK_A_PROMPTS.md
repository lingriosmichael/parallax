# Premium Pack A · Hero walls, pilot (2026-11-02)

Part of `Docs/V_TASKS/PREMIUM_VISUAL_PLAN.md`, Phase 3. **Nothing else in the pack gets painted until these three work in L022.**

**The problem they solve:** big walls in L022, L025 and L030 are one repeating texture for screen after screen. Each piece is one large feature set into that wall, so the eye gets a landmark.

**What past rounds taught us:**
- **Don't attach saturated art.** Bright references turned everything orange.
- **Simple shapes win.** Fine cracks and rivets turn to noise on a phone.
- **Blue background, not magenta or green.** Magenta tinted the edges pink, and green would eat the moss and ivy. Claude Code keys the blue out.

**How to work:**
- One new chat per image. Attach only what each item names.
- Check it small first: shrink to 400 px wide. If it isn't one clear shape at that size, reject it.
- Save to `Art_Source/Environment/GoldenRuins/` with the exact name, then tell Claude Code.

**References:**
- **R1:** `Docs/Art/Reference/styleframe_A_v1.png` (painting technique and light)
- **R2:** `Assets/_Game/Art/RealityA/Environment/Kit/ENV_Fill_A.png` (the wall it has to sit inside: match this stone)

**In game:** each piece sits on the wall at about 7 u wide (the view is about 20 u wide), behind the cat, and never over a trap, door or lane. No part of it is a surface or a hazard.

---

### HW-01 · Great carved relief

**Save as:** `HW-01_relief_v1.png`, landscape 4:3, flat pure blue background. **Attach:** R1, R2.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones. Match the first attached image's painting technique and light, and match the second attached image's stone exactly: its colour, its block size and its surface.

Paint one large ancient carved stone relief, as if cut into a temple wall centuries ago: a calm seated guardian figure with no face detail, worn almost smooth, framed by a simple arch. The relief is shallow, softly lit from the upper left, in the same warm sandstone as the wall. Moss and a few thin vines grow in its grooves. Time has broken one corner away.

Big simple shapes that read at phone size. Muted, warm, low contrast, never saturated. Warm brown shadows, never black. Strict flat front view, no perspective.

The outer edge of the piece is rough and irregular, like broken masonry blocks, never a straight rectangle. Around it the background is one flat pure blue (#0000FF), edge to edge, with no glow and no shadow on the blue. No text, no letters, no animals, no gold, no red, no gems. Not a photo, not a 3D render, not a cartoon.
```

**Reject if:** it's a rectangle; it glows; it's brighter than the wall in R2; it has a detailed face; it looks like a poster or a gem-covered idol.

### HW-02 · Collapsed wall with a view

**Save as:** `HW-02_breach_v1.png`, landscape 4:3, flat pure blue background. **Attach:** R1, R2.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones. Match the first attached image's painting technique and light, and match the second attached image's stone exactly: its colour, its block size and its surface.

Paint one large collapsed section of an ancient stone wall: a ragged hole where the blocks have fallen away, its broken stone edges thick and catching warm light on their upper left sides. Loose blocks lean at the bottom of the hole, and long roots and ivy hang across it from above.

Inside the hole there is NOTHING painted: it is flat pure blue (#0000FF), because the game shows its own sky through it. Keep the hanging roots thin and few so the sky shows between them.

Big simple shapes that read at phone size. Muted and warm, never saturated. Warm brown shadows, never black. Strict flat front view, no perspective.

The outer edge is rough and irregular, like broken masonry, never a straight rectangle. Outside the piece the background is the same flat pure blue, with no glow and no shadow on the blue. No text, no people, no animals.
```

**Reject if:** the hole has sky or clouds painted in it; the edge is straight; the roots fill the hole; the stone doesn't match R2.

### HW-03 · Root-choked wall

**Save as:** `HW-03_roots_v1.png`, landscape 4:3, flat pure blue background. **Attach:** R1, R2.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones. Match the first attached image's painting technique and light.

Paint one great ancient tree root system spreading down across a wall, as if the tree above has been gripping the ruin for a thousand years: a few thick roots that fork into thinner ones, with moss and ivy along them. Only the roots and plants are painted, not the wall behind them.

The roots are warm grey-brown, softly lit from the upper left, with warm brown shadows, never black. Big simple shapes that read at phone size: about five main roots, not a tangle. Muted and natural, never saturated. Strict flat front view, no perspective.

The background is one flat pure blue (#0000FF), edge to edge, with no glow and no shadow on the blue. No leaves larger than a hand, no flowers, no stone, no text, no animals. Not a photo, not a 3D render, not a cartoon, no outlines.
```

**Reject if:** it's a dense tangle; the roots look like tentacles; it has outlines; it's darker than a silhouette or brighter than the wall.
