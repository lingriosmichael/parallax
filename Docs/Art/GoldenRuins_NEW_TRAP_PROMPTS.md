# Golden Ruins · trap paintings still to do (updated 2026-11-01)

**All eight are painted and in the game (D-292); the prompts stay for a repaint.**

**Only what's missing is listed here.** These traps are in L021–L030 and still draw as grey and red placeholders. Paint them in this order, the most
visible first. Finished items are removed; the full history is in git.

| Paint | What | Seen in levels |
|---|---|---|
| GT-01, GT-02 | Spinner arm and hub | 21, 22, 24, 27, 28 |
| GT-10, GT-11 | Aiming turret and its beam | 1, 5, 7, 10, 11, 15, 18, 20, 21, 22, 25, 30 |
| GT-07 | Pendulum blade | 21, 22, 27 |
| GT-05 | Falling spiked ceiling | 30 |
| GT-04 | Spiked underside strip (turning plates and spiked hinge floors) | 22, 30 |
| GT-08 | Pressure plate | 22, 26 |

**Dropped:** GT-03 (no glitch walls in Golden Ruins), GT-06 (conveyor withdrawn, D-199), GT-09 (portals are Obsidian only, L033: their art is D-281's ring and swirl).

**The world:** a sunlit ancient temple city of weathered sandstone, bronze and old iron, floating among golden clouds. There's a
warm backlit sun, vines and moss, and red cloth banners. See `Docs/Art/LOOK_AND_FEEL.md`.

**Colour rule (D-108), the most important thing here:**
- **Crimson** (about 0.80, 0.06, 0.14) marks lethal things only: spike tips, blade edges, a turret's eye and beam.
- Everything safe stays sandstone, bronze, gold, moss and pale light.
- Every lethal piece has a dark outer line and a light inner rim, so it reads against any background at phone size.

**The finish (the developer's GT-01 note: "Much better"):** simple shapes, rich materials. Every piece is one hand-made object that has been through centuries of use, so nothing repeats like a pattern, nothing looks clean or new, and crimson is painted into the metal, never stuck on like a gem.

## How to work

- **One ChatGPT chat** for the whole set. Attach the references each item names, then paste its whole prompt.
- **Backgrounds:** the prompts ask for flat pure magenta #FF00FF, which the tools turn into transparency. "On pure black" pieces are light: the tools turn black into transparency.
- **If it starts to look like clip-art,** start a new chat. An earlier image's look carries over.
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
> shadows (never black). Match the attached style image's painting technique and palette, and the attached references for their weight, wear and finish. DANGER IS CRIMSON: every edge or tip that
> can kill is crimson-red, with a dark outline and a thin light rim, and nothing safe is ever crimson.

**Every prompt ends with this paragraph (also already in each prompt):**

> Strict side view, orthographic: no perspective, no 3/4 angle, no top surfaces showing. It must look like a concept artist's
> painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon. Big simple shapes that read at
> phone size. No text, no letters, no characters, no animals.

---

## To paint

### GT-01 · Spinner arm (the blade that turns)

**Save as:** `Art_Source/Traps_ChatGPT/GT-01_spinner_arm_v2.png`, landscape 6:1, flat pure magenta #FF00FF background. **Attach:** R1, P1, P7.
**In game:** 1.2–2.6 u long, 0.3–0.4 u thick, turning about its LEFT end. Paint it at its longest: a shorter arm shows the same blade cut off at the tip, so it doesn't need to repeat.

v1 was rejected: a flat bar with one spike and one band repeated, like a ruler with plastic gems. It's one forged weapon, not a pattern.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). This place is the GOLDEN RUINS: a sunlit ancient temple city of weathered sandstone, aged bronze and old iron, floating among golden clouds, with moss and vines. Light is a low warm sun from behind (gold, around #FFD9A0), warm brown shadows (never black). Match the attached style image's painting technique and palette, and the attached spike row and crusher for their weight, wear and finish. DANGER IS CRIMSON: every edge or tip that can kill is crimson-red, with a dark outline and a thin light rim, and nothing safe is ever crimson.

Paint ONE great blade from an ancient temple machine, lying flat across the image: a single long, heavy slab of black forged iron, hammered by hand and centuries old, as thick as a sixth of its length, keeping one straight shape from end to end. It was made as one object, so nothing on it repeats. Both long edges are ground into big, uneven, hand-cut teeth: broad and narrow, chipped and worn, a few broken short, no two alike, and steel-bright where they were sharpened. The cutting edge of every tooth runs from bright steel into wet crimson at its very tip, painted into the metal as a stain of heat and old blood-red lacquer, never a separate red gem or cone. Down the blade's centre runs one dark fuller groove with faint worn engravings. There are hammer marks, deep scratches, rust blooms, verdigris and a few threads of moss caught near the left end. The LEFT end has no teeth: a thick collar of aged bronze wrapped round the iron, where it bolts to its pivot. The right end is a worn, squared-off tip. Lit from behind by the low sun: a warm rim along its upper edge, deep warm shadow below. It looks heavy, old and lethal, and that must be obvious at a glance.

Strict side view, orthographic: no perspective, no 3/4 angle, no top surfaces showing. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a flat game asset. Big simple shapes that read at phone size. The background is one flat pure magenta (#FF00FF), edge to edge, with no shadow on it. No text, no letters, no characters, no animals.
```

**Reject if:**
- any spike, band or rivet repeats like a pattern;
- the teeth look stuck on, or the red looks like plastic gems or cones;
- it looks flat or clean, with no wear;
- it reads as a sword, a saw from a shop, or an oar;
- it curves, tapers or isn't horizontal;
- there are teeth on the bronze collar;
- it's too thin to read when shrunk to 150 pixels wide.

---

### GT-02 · Spinner hub (the pivot it turns on)

**Save as:** `Art_Source/Traps_ChatGPT/GT-02_spinner_hub_v1.png`, square, magenta background. **Attach:** R1, P7, R4. **In game:** 0.5 u across,
drawn over the arm's pivot. It is harmless.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). This place is the GOLDEN RUINS: a sunlit ancient temple city of weathered sandstone, aged bronze and old iron, floating among golden clouds, with moss and vines. Light is a low warm sun from behind (gold, around #FFD9A0), warm brown shadows (never black). Match the attached style image's painting technique and palette, and the attached references for their weight, wear and finish. DANGER IS CRIMSON: every edge or tip that can kill is crimson-red, with a dark outline and a thin light rim, and nothing safe is ever crimson.

Paint ONE massive pivot hub, seen face-on, filling most of the square: a thick round boss of aged bronze cast by hand and darkened by centuries, with one great iron axle bolt driven through its centre. Around the bolt runs a single worn collar, its edge dented and rubbed bright where the blade has ground against it for a thousand years. One side carries the faint ghost of a carved sun, nearly worn away. There are hammer marks, deep scratches, verdigris in the recesses and a little moss in one crack. Nothing on it repeats evenly: no neat ring of rivets. Lit from behind by the low sun: a warm rim along its upper left, deep warm shadow lower right. It is the axle a spinning blade turns on: solid, heavy, old and harmless, with no spikes and no crimson.

Strict side view, orthographic: no perspective, no 3/4 angle, no top surfaces showing. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a flat game asset. Big simple shapes that read at phone size. The background is one flat pure magenta (#FF00FF), edge to edge, with no shadow on it. No text, no letters, no characters, no animals.
```

**Reject if:**
- it's shown at an angle, or it isn't round;
- it has spikes or crimson;
- it looks like a clean coin, a medal or a button;
- it has a neat ring of identical rivets;
- it's busy when shrunk to 64 pixels.

### GT-04 · Spiked underside strip (flip platforms, and the bottom of falling spiked ceilings)

**Save as:** `Art_Source/Traps_ChatGPT/GT-04_spiked_underside_v1.png`, landscape 4:1, magenta background, tiles sideways.
**Attach:** R1, P1, P7, R4.
**In game:** a strip 0.3 u tall along a platform's underside (a flip platform turns it to face up) and under a falling ceiling.
- **Optional:** the existing spike row (P1) turned upside down may already be enough. Paint this only for a distinct "hanging"
  look.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). This place is the GOLDEN RUINS: a sunlit ancient temple city of weathered sandstone, aged bronze and old iron, floating among golden clouds, with moss and vines. Light is a low warm sun from behind (gold, around #FFD9A0), warm brown shadows (never black). Match the attached style image's painting technique and palette, and the attached references for their weight, wear and finish. DANGER IS CRIMSON: every edge or tip that can kill is crimson-red, with a dark outline and a thin light rim, and nothing safe is ever crimson.

Paint a long horizontal strip, seamlessly tileable left to right: a battered band of black forged iron and dark bronze along the TOP edge, and hanging DOWN from it big, stout, hand-forged iron spikes in the spirit of the attached spike row. Within the strip there are about seven spikes and no two alike: thick and thin, long and short, a few chipped or bent, unevenly spaced, some scarred with rust. Each runs from dark iron through bright worn steel into wet crimson at its very tip, painted into the metal as a stain of heat and old lacquer, never a separate red cone. The left and right edges of the strip must meet exactly when it repeats, but the eye must not find a pattern inside it. Below the tips, only the background. It is fixed to the underside of a stone platform or a crushing block: deadly to touch.

Strict side view, orthographic: no perspective, no 3/4 angle, no top surfaces showing. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a flat game asset. Big simple shapes that read at phone size. The background is one flat pure magenta (#FF00FF), edge to edge, with no shadow on it. No text, no letters, no characters, no animals.
```

**Reject if:**
- there's a seam;
- the spikes look identical or evenly spaced;
- the red looks like plastic gems or cones;
- the spikes point up or sideways;
- the spikes are shorter than half the strip;
- the band is at the bottom.

### GT-05 · Falling spiked ceiling (the crusher block)

**Save as:** `Art_Source/Traps_ChatGPT/GT-05_spiked_crusher_v1.png`, landscape 3:1, magenta background. **Attach:** R1, P7, P1, R4.
**In game:** a block about 1.5–3 u wide and 0.5–0.6 u tall that slams down out of a ceiling, its spiked face (GT-04's look) under
it. Paint the block and its spikes together.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). This place is the GOLDEN RUINS: a sunlit ancient temple city of weathered sandstone, aged bronze and old iron, floating among golden clouds, with moss and vines. Light is a low warm sun from behind (gold, around #FFD9A0), warm brown shadows (never black). Match the attached style image's painting technique and palette, and the attached references for their weight, wear and finish. DANGER IS CRIMSON: every edge or tip that can kill is crimson-red, with a dark outline and a thin light rim, and nothing safe is ever crimson.

Paint ONE crusher that has torn loose from a temple ceiling: a single colossal, wide, low slab of ancient sandstone, cracked and pitted, its corners broken away, held together by two huge bands of black iron and aged bronze that were riveted on by hand and have since warped, like the attached crusher. It fills the upper two-thirds of the image. Its whole underside bristles with big hand-forged iron spikes pointing straight down, no two alike: thick and thin, some chipped or bent, unevenly spaced, each running from dark iron through worn steel into wet crimson at its very tip, painted into the metal and never a separate red cone. Moss clings in its cracks, and dust and stone chips crumble from its top edge as if it has just broken free. Lit from behind by the low sun: a warm rim along the top, deep warm shadow beneath. It must read instantly as a spiked ceiling that falls on you.

Strict side view, orthographic: no perspective, no 3/4 angle, no top surfaces showing. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a flat game asset. Big simple shapes that read at phone size. The background is one flat pure magenta (#FF00FF), edge to edge, with no shadow on it. No text, no letters, no characters, no animals.
```

**Reject if:**
- it's taller than it is wide;
- the spikes look identical, evenly spaced or stuck on;
- it looks clean or new, or the bands repeat like a pattern;
- there are spikes on its top or sides;
- it reads as a platform you could stand on;
- it's shown in perspective.

### GT-07 · Pendulum blade

**Save as:** `Art_Source/Traps_ChatGPT/GT-07_pendulum_blade_v1.png`, portrait 1:4, magenta background. **Attach:** R1, P1, P7, R4.
**In game:** it hangs from a pivot at the TOP and swings left and right. It is 3–5 u long, with the blade about 0.6 u at the bottom.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). This place is the GOLDEN RUINS: a sunlit ancient temple city of weathered sandstone, aged bronze and old iron, floating among golden clouds, with moss and vines. Light is a low warm sun from behind (gold, around #FFD9A0), warm brown shadows (never black). Match the attached style image's painting technique and palette, and the attached references for their weight, wear and finish. DANGER IS CRIMSON: every edge or tip that can kill is crimson-red, with a dark outline and a thin light rim, and nothing safe is ever crimson.

Paint ONE hanging temple pendulum, vertical, filling the tall image, made as one brutal object. At the very top is a thick worn bronze ring where it hangs. Below it runs one long shaft of black hammered iron, straight but uneven along its length, wrapped in two places by old bronze binding and a few threads of moss, with rust running down it. At the bottom is one huge crescent axe blade of forged iron, far wider than the shaft, thick at its spine, chipped along its edge, with hammer marks and a dark fuller groove. Its curved cutting edge faces down and runs from bright sharpened steel into wet crimson along the very edge, painted into the metal as a stain of heat and old lacquer, never a glowing outline or a separate strip. Lit from behind by the low sun: a warm rim down one side, deep warm shadow on the other. It is obviously deadly at the blade and plain at the shaft.

Strict side view, orthographic: no perspective, no 3/4 angle, no top surfaces showing. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a flat game asset. Big simple shapes that read at phone size. The background is one flat pure magenta (#FF00FF), edge to edge, with no shadow on it. No text, no letters, no characters, no animals.
```

**Reject if:**
- the shaft is crimson;
- the red looks like a neon outline;
- it looks clean or new;
- the blade isn't at the bottom, or it isn't vertical;
- the blade is too small to read at phone size.

### GT-08 · Pressure plate

**Save as:** `Art_Source/Traps_ChatGPT/GT-08_pressure_plate_v1.png`, landscape 6:1, magenta background. **Attach:** R1, P7, R4.
**In game:** 1.2 u wide, about 0.1 u tall, flush on a floor. It dips when stepped on. It is harmless.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). This place is the GOLDEN RUINS: a sunlit ancient temple city of weathered sandstone, aged bronze and old iron, floating among golden clouds, with moss and vines. Light is a low warm sun from behind (gold, around #FFD9A0), warm brown shadows (never black). Match the attached style image's painting technique and palette, and the attached references for their weight, wear and finish. DANGER IS CRIMSON: every edge or tip that can kill is crimson-red, with a dark outline and a thin light rim, and nothing safe is ever crimson.

Paint ONE ancient pressure plate set into a temple floor, seen from the side: a long, thin slab of heavy aged bronze, worn into a shallow dip in its middle by centuries of feet, its surface scratched and rubbed bright along the top edge and dark with verdigris toward its ends. One side has sunk slightly lower than the other. It sits in a rough stone seat with a little moss and grit in the gap. Nothing on it repeats. Lit from behind by the low sun: a warm rim along its top edge, deep warm shadow beneath. It invites a step and looks like a real part of an old machine, heavy and slightly out of true. Harmless: no crimson.

Strict side view, orthographic: no perspective, no 3/4 angle, no top surfaces showing. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a flat game asset. Big simple shapes that read at phone size. The background is one flat pure magenta (#FF00FF), edge to edge, with no shadow on it. No text, no letters, no characters, no animals.
```

**Reject if:**
- you can see its top surface;
- it's taller than a fifth of its width;
- there's any crimson;
- it looks clean, flat or new;
- it reads as a coin, a bar of gold or a button.

### GT-10 · Aiming turret (mount and eye)

**Save as:** `Art_Source/Traps_ChatGPT/GT-10_turret_v1.png`, landscape 2:1, magenta background. **Attach:** R1, P7, R4.
**In game:** 0.6 u, hanging from a ceiling. The mount stays put and the eye turns to track the cat, so they're separate, and the eye is painted twice: idle and locked on.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). This place is the GOLDEN RUINS: a sunlit ancient temple city of weathered sandstone, aged bronze and old iron, floating among golden clouds, with moss and vines. Light is a low warm sun from behind (gold, around #FFD9A0), warm brown shadows (never black). Match the attached style image's painting technique and palette, and the attached references for their weight, wear and finish. DANGER IS CRIMSON: every edge or tip that can kill is crimson-red, with a dark outline and a thin light rim, and nothing safe is ever crimson.

Paint TWO separate pieces side by side with a gap, for an ancient sentinel whose eye turns to follow the player. LEFT: the MOUNT, one heavy half-disc of carved sandstone bound in black iron and worn bronze, its flat side at the top (it bolts into a ceiling), cracked, chipped and hung with a few threads of moss. RIGHT: the EYE, a round ball of dark aged bronze with one short lens snout on ONE side only, pointing straight to the right, painted twice one above the other: IDLE (the lens dark and dull, a faint crimson ember deep inside) and LOCKED (the lens blazing crimson with a hot pale core, crimson light leaking around its rim). Scratches, hammer marks and verdigris on the bronze, and the same size in both. The eye's centre is the middle of its ball, so it can be rotated in the game, and it sits exactly in the mount's half-disc. Lit from behind by the low sun: a warm rim along the top edges. It is a machine older than the city, menacing and solid, never a character.

Strict side view, orthographic: no perspective, no 3/4 angle, no top surfaces showing. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a flat game asset. Big simple shapes that read at phone size. The background is one flat pure magenta (#FF00FF), edge to edge, with no shadow on it. No text, no letters, no characters, no animals.
```

**Reject if:**
- it has a face, a mouth or a second eye;
- it reads as a cute character;
- the mount and eye are fused into one shape;
- the eye is painted in perspective or turned toward the viewer;
- it has a long gun barrel;
- the two eyes differ in size;
- it looks clean or new.

### GT-11 · Turret beam

**Save as:** `Art_Source/Traps_ChatGPT/GT-11_turret_beam_v2.png`, landscape, on pure black, tiles sideways. **Attach:** R1. Start a new chat: v1 came back orange on magenta.
**In game:** stretched along the turret's aim; 0.06 u thin while aiming and locking, 0.3 u thick while firing.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). This place is the GOLDEN RUINS: a sunlit ancient temple city of weathered sandstone, aged bronze and old iron, floating among golden clouds, with moss and vines. Light is a low warm sun from behind (gold, around #FFD9A0), warm brown shadows (never black). Match the attached style image's painting technique and palette, and the attached references for their weight, wear and finish. DANGER IS CRIMSON: every edge or tip that can kill is crimson-red, with a dark outline and a thin light rim, and nothing safe is ever crimson.

Paint a long horizontal beam of raw energy on a PURE BLACK (#000000) background, edge to edge: never magenta, never any other colour. It runs straight from the left edge to the right edge at the same height, and it must tile: what leaves the right edge continues exactly at the left edge. A thin white-hot core melts straight into deep crimson (around #CC0F24) with a soft crimson glow close around it. There is NO yellow and NO orange anywhere, so it never reads as fire. Its edges are slightly rough and alive, and a few fine crimson embers drift off it, but it stays the same thickness everywhere. It looks dangerous and physical, not like a clean laser line. Deadly.

Strict side view, orthographic: no perspective, no 3/4 angle, no top surfaces showing. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a flat game asset. Big simple shapes that read at phone size. The background stays pure black. No text, no letters, no characters, no animals.
```

**Reject if:**
- there's a seam;
- the background isn't black;
- it has any yellow or orange, or reads as fire;
- it isn't crimson;
- it tapers, or looks like a clean UI line;
- the glow is wider than the core by more than three times.
