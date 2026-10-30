# Golden Ruins · prompts for the new traps (2026-10-24, D-187, D-197)

**Scope:** art for the mechanics added on 2026-10-24, in the Golden Ruins look (L001–L030, D-172). The game uses grey-box
placeholders for them today.
- **Part A, now:** GT-01 to GT-05, the pieces already in L022–L030 (D-187): spinners, glitch-wall ghosts, flip platforms and
  falling spiked ceilings.
- **Part B, later:** GT-07 to GT-11, for D-197's mechanics (pendulum, plate, portal, turret; GT-06, the conveyor, is withdrawn, D-199). Paint those once they are
  placed in a level, so the sizes match.

**The world:** a sunlit ancient temple city of weathered sandstone, bronze and old iron, floating among golden clouds. There's a
warm backlit sun, vines and moss, and red cloth banners. See `Docs/Art/LOOK_AND_FEEL.md`.

**Colour rule (D-108), the most important thing here:**
- **Crimson** (about 0.80, 0.06, 0.14) marks lethal things only: spike tips, blade edges, a turret's eye and beam.
- Everything safe stays sandstone, bronze, gold, moss and pale light.
- Every lethal piece has a dark outer line and a light inner rim, so it reads against any background at phone size.

## How to work

- **One ChatGPT chat** for the whole set. Attach the references each item names, then paste its whole prompt.
- **Transparency:** "transparent" means real alpha. If ChatGPT paints a checkerboard, regenerate or ask for a flat pure magenta
  #FF00FF background. "On pure black" pieces are light: the tools turn black into transparency.
- **Checking:** use each item's reject list, and regenerate rather than fix by hand. Shrink to phone size before saving.
- **Saving:** save to `Art_Source/Traps_ChatGPT/` with the exact file name. Tell Claude Code when a batch is in, and it processes
  them into the trap kit under an art ticket.

**References:**
- **R1:** `Docs/Art/Reference/styleframe_A_v1.png` (the Golden Ruins look and painting technique)
- **P1:** `Art_Source/Traps_ChatGPT/PILOT-01R_spike_row_v1.png` (how this game's spikes look: match it)
- **P7:** `Art_Source/Traps_ChatGPT/PILOT-07_slam_v2.png` (how this game's crushers look)
- **R4:** `~/Parallax_batch_tools/PAX-A18/stop2/L002_play_after.png` (how small things are next to the cat on a phone)

**Every prompt starts with this paragraph (already written into each prompt below):**

> I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly,
> never cheap). This place is the GOLDEN RUINS: a sunlit ancient temple city of weathered sandstone, aged bronze and old iron,
> floating among golden clouds, with moss and vines. Light is a low warm sun from behind (gold, around #FFD9A0), warm brown
> shadows (never black). Match the attached style image's painting technique and palette. DANGER IS CRIMSON: every edge or tip that
> can kill is crimson-red, with a dark outline and a thin light rim, and nothing safe is ever crimson.

**Every prompt ends with this paragraph (also already in each prompt):**

> Strict side view, orthographic: no perspective, no 3/4 angle, no top surfaces showing. It must look like a concept artist's
> painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon. Big simple shapes that read at
> phone size. No text, no letters, no characters, no animals.

---

## Part A · now (in L022–L030)

### GT-01 · Spinner arm (the spiked arm that turns)

**Save as:** `Art_Source/Traps_ChatGPT/GT-01_spinner_arm_v1.png`, landscape 8:1, transparent. **Attach:** R1, P1, R4.
**In game:** 1.2–2.6 u long, 0.3–0.4 u thick. It turns about its LEFT end, and the game stretches it to the arm's length, so keep
it even along its length.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). This place is the GOLDEN RUINS: a sunlit ancient temple city of weathered sandstone, aged bronze and old iron, floating among golden clouds, with moss and vines. Light is a low warm sun from behind (gold, around #FFD9A0), warm brown shadows (never black). Match the attached style image's painting technique and palette. DANGER IS CRIMSON: every edge or tip that can kill is crimson-red, with a dark outline and a thin light rim, and nothing safe is ever crimson.

Paint ONE long horizontal spinning blade-arm, lying flat across the image: a straight bar of dark aged bronze, as thick as a tenth of its length, banded with old iron. Its whole top edge and whole bottom edge are lined with short stout iron spikes like the attached spike row, each spike tip crimson. The LEFT end is a plain round bronze socket (it turns there, so no spikes on it); the right end is a blunt iron cap. Keep the bar the same along its length so it can be stretched. It is a deadly trap: a cat that touches it dies, and that must be obvious at a glance.

Strict side view, orthographic: no perspective, no 3/4 angle, no top surfaces showing. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon. Big simple shapes that read at phone size. No text, no letters, no characters, no animals.
```

**Reject if:**
- it curves, tapers or isn't horizontal;
- there are spikes on the left socket;
- the tips aren't crimson;
- it reads as a sword, an oar or decoration;
- it's too thin to read at phone size.

### GT-02 · Spinner hub (the pivot it turns on)

**Save as:** `Art_Source/Traps_ChatGPT/GT-02_spinner_hub_v1.png`, square, transparent. **Attach:** R1, R4. **In game:** 0.5 u across,
drawn over the arm's pivot. It is harmless.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). This place is the GOLDEN RUINS: a sunlit ancient temple city of weathered sandstone, aged bronze and old iron, floating among golden clouds, with moss and vines. Light is a low warm sun from behind (gold, around #FFD9A0), warm brown shadows (never black). Match the attached style image's painting technique and palette. DANGER IS CRIMSON: every edge or tip that can kill is crimson-red, with a dark outline and a thin light rim, and nothing safe is ever crimson.

Paint ONE round bronze hub, seen face-on, filling most of the square: a heavy aged-bronze medallion like a temple gear, with a carved sun motif, a ring of rivets and a big iron bolt at its centre, worn gold highlights on its top-left from the warm light. It is the axle a spinning blade turns on: solid, mechanical, harmless (no spikes, no crimson).

Strict side view, orthographic: no perspective, no 3/4 angle, no top surfaces showing. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon. Big simple shapes that read at phone size. No text, no letters, no characters, no animals.
```

**Reject if:**
- it's shown at an angle;
- it has spikes or crimson;
- it's busy at phone size;
- it isn't round.

### GT-03 · Glitch ghost (where a glitch wall is about to appear)

**Save as:** `Art_Source/Traps_ChatGPT/GT-03_glitch_ghost_v1.png`, square, transparent. **Attach:** R1, P7, R4.
**In game:** for about a quarter of a second before a glitch wall teleports, this shows where it will appear. It's stretched as a
9-slice to 1 × 2.5 u (a wall), 1 × 3 u (a half-hall block) or 2 × 0.5 u (a platform), so its border must be even all round.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). This place is the GOLDEN RUINS: a sunlit ancient temple city of weathered sandstone, aged bronze and old iron, floating among golden clouds, with moss and vines. Light is a low warm sun from behind (gold, around #FFD9A0), warm brown shadows (never black). Match the attached style image's painting technique and palette. DANGER IS CRIMSON: every edge or tip that can kill is crimson-red, with a dark outline and a thin light rim, and nothing safe is ever crimson.

Paint the GHOST of a carved sandstone block, filling the square: a see-through mirage of the block, as if an ancient enchantment is about to drop it into the world. It's made of shimmering pale-gold heat-haze light, with faint carved-stone lines inside it and a few thin horizontal slices offset sideways like a flickering mirage. It has a bright, even pale-white glowing outline all the way round, the same thickness on every side, and its inside is mostly transparent. It warns "something solid is about to appear here": it must stand out, but it is not deadly, so no crimson.

Strict side view, orthographic: no perspective, no 3/4 angle, no top surfaces showing. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon. Big simple shapes that read at phone size. No text, no letters, no characters, no animals.
```

**Reject if:**
- it's opaque;
- the outline is uneven, so it can't stretch;
- there's any crimson;
- it reads as a door, a window or a portal;
- it's too faint to see over a bright sky.

### GT-04 · Spiked underside strip (flip platforms, and the bottom of falling spiked ceilings)

**Save as:** `Art_Source/Traps_ChatGPT/GT-04_spiked_underside_v1.png`, landscape 4:1, transparent, tiles sideways.
**Attach:** R1, P1, R4.
**In game:** a strip 0.3 u tall along a platform's underside (a flip platform turns it to face up) and under a falling ceiling.
- **Optional:** the existing spike row (P1) turned upside down may already be enough. Paint this only for a distinct "hanging"
  look.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). This place is the GOLDEN RUINS: a sunlit ancient temple city of weathered sandstone, aged bronze and old iron, floating among golden clouds, with moss and vines. Light is a low warm sun from behind (gold, around #FFD9A0), warm brown shadows (never black). Match the attached style image's painting technique and palette. DANGER IS CRIMSON: every edge or tip that can kill is crimson-red, with a dark outline and a thin light rim, and nothing safe is ever crimson.

Paint a long horizontal strip, seamlessly tileable left to right: an aged bronze rail along the TOP edge, and hanging DOWN from it a dense row of short, stout iron spikes like the attached spike row, every tip pointing straight down and coloured crimson. Below the tips, transparent. It is fixed to the underside of a stone platform or a crushing block: deadly to touch.

Strict side view, orthographic: no perspective, no 3/4 angle, no top surfaces showing. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon. Big simple shapes that read at phone size. No text, no letters, no characters, no animals.
```

**Reject if:**
- there's a seam;
- the spikes point up or sideways;
- the tips aren't crimson;
- the spikes are taller than half the strip;
- the rail is at the bottom.

### GT-05 · Falling spiked ceiling (the crusher block)

**Save as:** `Art_Source/Traps_ChatGPT/GT-05_spiked_crusher_v1.png`, landscape 3:1, transparent. **Attach:** R1, P7, P1, R4.
**In game:** a block about 1.5–3 u wide and 0.5–0.6 u tall that slams down out of a ceiling, its spiked face (GT-04's look) under
it. Paint the block and its spikes together.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). This place is the GOLDEN RUINS: a sunlit ancient temple city of weathered sandstone, aged bronze and old iron, floating among golden clouds, with moss and vines. Light is a low warm sun from behind (gold, around #FFD9A0), warm brown shadows (never black). Match the attached style image's painting technique and palette. DANGER IS CRIMSON: every edge or tip that can kill is crimson-red, with a dark outline and a thin light rim, and nothing safe is ever crimson.

Paint ONE heavy crusher block that drops out of a temple ceiling: a wide, low slab of carved sandstone bound with bronze bands (like the attached crusher), filling the upper two-thirds of the image. Its whole underside is lined with short stout iron spikes pointing straight down, every tip crimson, like the attached spike row. Small dust and chips crumble from its top edge, as if it has just broken loose. It must read instantly as "a spiked ceiling that falls on you".

Strict side view, orthographic: no perspective, no 3/4 angle, no top surfaces showing. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon. Big simple shapes that read at phone size. No text, no letters, no characters, no animals.
```

**Reject if:**
- it's taller than it is wide;
- there are spikes on its top or sides;
- the tips aren't crimson;
- it reads as a platform you could stand on;
- it's shown in perspective.

---

## Part B · later (D-197's mechanics, paint once they are placed in a level)

### GT-06 · (withdrawn)

The conveyor was removed from the kit (D-199), so this slot isn't painted.

### GT-07 · Pendulum blade

**Save as:** `Art_Source/Traps_ChatGPT/GT-07_pendulum_blade_v1.png`, portrait 1:4, transparent. **Attach:** R1, P1, R4.
**In game:** it hangs from a pivot at the TOP and swings left and right. It is 3–5 u long, with the blade about 0.6 u at the bottom.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). This place is the GOLDEN RUINS: a sunlit ancient temple city of weathered sandstone, aged bronze and old iron, floating among golden clouds, with moss and vines. Light is a low warm sun from behind (gold, around #FFD9A0), warm brown shadows (never black). Match the attached style image's painting technique and palette. DANGER IS CRIMSON: every edge or tip that can kill is crimson-red, with a dark outline and a thin light rim, and nothing safe is ever crimson.

Paint ONE hanging pendulum blade, vertical, filling the tall image: at the very top a bronze ring (where it hangs), then a long straight iron rod, and at the bottom a big crescent-shaped iron axe blade, its curved cutting edge facing down and glowing crimson along the edge. It swings like a temple trap: obviously deadly at the blade, plain at the rod.

Strict side view, orthographic: no perspective, no 3/4 angle, no top surfaces showing. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon. Big simple shapes that read at phone size. No text, no letters, no characters, no animals.
```

**Reject if:**
- the rod is crimson;
- the blade isn't at the bottom;
- it isn't vertical;
- the blade is too small to read on a phone.

### GT-08 · Pressure plate

**Save as:** `Art_Source/Traps_ChatGPT/GT-08_pressure_plate_v1.png`, landscape 6:1, transparent. **Attach:** R1, R4.
**In game:** 1.2 u wide, about 0.1 u tall, flush on a floor. It dips when stepped on. It is harmless.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). This place is the GOLDEN RUINS: a sunlit ancient temple city of weathered sandstone, aged bronze and old iron, floating among golden clouds, with moss and vines. Light is a low warm sun from behind (gold, around #FFD9A0), warm brown shadows (never black). Match the attached style image's painting technique and palette. DANGER IS CRIMSON: every edge or tip that can kill is crimson-red, with a dark outline and a thin light rim, and nothing safe is ever crimson.

Paint ONE low, wide temple pressure plate set into a floor: a thin slab of polished bronze with a carved sun glyph in its middle and a worn gold rim, slightly raised above the floor line, warm light catching its top edge. It invites a step. Harmless: no crimson.

Strict side view, orthographic: no perspective, no 3/4 angle, no top surfaces showing. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon. Big simple shapes that read at phone size. No text, no letters, no characters, no animals.
```

**Reject if:**
- you can see its top surface;
- it's taller than a fifth of its width;
- there's any crimson;
- it reads as a coin or a button icon.

### GT-09 · Portal pair (entry and exit)

**Save as:** `Art_Source/Traps_ChatGPT/GT-09_portals_v1.png`, landscape 2:1, transparent: the entry on the left, the exit on the
right. **Attach:** R1, R4.
**In game:** each is about 0.6–1 u wide and 1.5 u tall. You walk into the entry and come out of the exit. The exit can flicker and
move.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). This place is the GOLDEN RUINS: a sunlit ancient temple city of weathered sandstone, aged bronze and old iron, floating among golden clouds, with moss and vines. Light is a low warm sun from behind (gold, around #FFD9A0), warm brown shadows (never black). Match the attached style image's painting technique and palette. DANGER IS CRIMSON: every edge or tip that can kill is crimson-red, with a dark outline and a thin light rim, and nothing safe is ever crimson.

Paint TWO matching ancient portals side by side, each a tall narrow arch of carved sandstone and bronze framing a swirling oval of light. The LEFT one (the way in) swirls with cool pale blue light; the RIGHT one (the way out) swirls with soft violet light. Each is twice as tall as it is wide, and empty space separates them. They are magic doorways, inviting and harmless: no crimson.

Strict side view, orthographic: no perspective, no 3/4 angle, no top surfaces showing. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon. Big simple shapes that read at phone size. No text, no letters, no characters, no animals.
```

**Reject if:**
- the two look the same colour;
- either is red or orange;
- they touch;
- they read as the level's exit door (which is gold).

### GT-10 · Aiming turret (body)

**Save as:** `Art_Source/Traps_ChatGPT/GT-10_turret_v1.png`, square, transparent. **Attach:** R1, R4.
**In game:** 0.6 u, set into a wall or ceiling. It tracks the cat, locks on and fires.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). This place is the GOLDEN RUINS: a sunlit ancient temple city of weathered sandstone, aged bronze and old iron, floating among golden clouds, with moss and vines. Light is a low warm sun from behind (gold, around #FFD9A0), warm brown shadows (never black). Match the attached style image's painting technique and palette. DANGER IS CRIMSON: every edge or tip that can kill is crimson-red, with a dark outline and a thin light rim, and nothing safe is ever crimson.

Paint ONE ancient sentinel turret, seen face-on, filling most of the square: a round bronze head like a temple guardian idol set in a stone socket, with a single large glowing CRIMSON eye-lens in its centre that watches you. It is a machine that shoots a deadly beam from that eye: menacing, solid, readable at phone size.

Strict side view, orthographic: no perspective, no 3/4 angle, no top surfaces showing. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon. Big simple shapes that read at phone size. No text, no letters, no characters, no animals.
```

**Reject if:**
- it has a face with a mouth or other eyes;
- the eye isn't crimson;
- it reads as a cute character;
- it's shown at an angle.

### GT-11 · Turret beam

**Save as:** `Art_Source/Traps_ChatGPT/GT-11_turret_beam_v1.png`, landscape 8:1, on pure black, tiles sideways. **Attach:** R1.
**In game:** stretched along the turret's aim; 0.06 u thin while aiming and locking, 0.3 u thick while firing.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). This place is the GOLDEN RUINS: a sunlit ancient temple city of weathered sandstone, aged bronze and old iron, floating among golden clouds, with moss and vines. Light is a low warm sun from behind (gold, around #FFD9A0), warm brown shadows (never black). Match the attached style image's painting technique and palette. DANGER IS CRIMSON: every edge or tip that can kill is crimson-red, with a dark outline and a thin light rim, and nothing safe is ever crimson.

Paint a long horizontal energy beam on a pure black background, seamlessly tileable left to right: a straight crimson ray with a hot pale core and a soft crimson glow fading outward, with a few faint sparks. It is the same thickness everywhere. Deadly.

Strict side view, orthographic: no perspective, no 3/4 angle, no top surfaces showing. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon. Big simple shapes that read at phone size. No text, no letters, no characters, no animals.
```

**Reject if:**
- there's a seam;
- it isn't crimson;
- it tapers;
- the glow is wider than the core by more than three times.
