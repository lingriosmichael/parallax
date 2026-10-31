# Obsidian Void · trap paintings still to do (updated 2026-10-31)

**Only what's missing is listed here.** OT-01 to OT-17 are painted and in the game, so they've been removed; the full history is in git. These pieces are in L031–L040 and
still draw as placeholders (stone, grey or plain squares).

| Paint | What | Seen in levels | Note |
|---|---|---|---|
| OT-19 | Portal pair | 33 | Most needed: L033's look can't be finished without it |
| OT-18 | Aiming turret | 31, 33, 36 | |
| OT-24 | Trident emitter | 31 | New |
| OT-25 | Chaser housing | 31 | New |
| OT-23 | Retracting hard-light floor | 32, 40 | |
| OT-22 | Glitch platform | 33, 37 | |
| OT-21 | Flip plate | 31 | Its spikes are still Golden Ruins art |
| OT-20 | Phase platform | 31, 34 | Lowest: it borrows the stone look well enough |

**The world:** the remains of an ancient high-technology civilisation (obsidian, black glass, circuit-like lines, cyan light),
floating in a starry void.

**Colour rule (D-108, D-172), the most important thing in this file:**
- **Crimson** (deep red pushed toward crimson, about hue 353°) is **danger only**: laser beams, warning rays, energy fields, a
  drone's eye, the edge of a crushing face.
- **Cyan** is the world's light and means safe: tethers, grip panels, vents, platforms, the door.
- A player must be able to tell at a glance on a phone that red light kills and blue light doesn't.

## How to work

- **One ChatGPT chat** for the whole set. Attach the references each item names, then paste its whole prompt.
- **Transparency:** "transparent" means real alpha. If ChatGPT paints a checkerboard, regenerate, or ask for a flat pure magenta
  #FF00FF background. "On pure black" pieces are light: the tools turn black into transparency.
- **Checking:** use each item's reject list, and regenerate rather than fix by hand. Shrink to phone size before saving.
- **Saving:** save to `Art_Source/Traps_Obsidian/` with the exact file name. Tell Claude Code when a batch is in, and it processes
  them through the trap kit (`Tools/Art/obsidian_traps.py`).

**References:**
- **R1:** `Docs/Art/Reference/styleframe_A_v1.png` (painting technique only)
- **OV-10a:** `Art_Source/Environment/ObsidianVoid/OV-10a_fill_v1.png`, your painted Obsidian stone fill (traps sit in it)
- **OV-01:** your painted Obsidian sky in the same folder (light and palette)
- **R4:** `~/Parallax_batch_tools/PAX-A18/stop2/L002_play_after.png` (how small things are next to the cat on a phone)
- **P:** the matching Golden Ruins painting in `Art_Source/Traps_ChatGPT/` (footprint and proportions only, never its look)

**Every prompt starts with this paragraph (already written into each prompt below):**

> I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly,
> never cheap). From the attached style image take ONLY the painting technique: soft painterly brushwork, luminous light, crafted
> detail. This place is the OBSIDIAN VOID: the remains of an ancient high-technology civilisation, machines and walls of black
> volcanic glass and polished obsidian with thin cyan light running in engraved circuit-like lines, floating in a starry void.
> Light is cold: a pale blue-white key light, cyan accents, deep navy shadows (never pure black). DANGER IS CRIMSON: every beam,
> field or light that can kill is crimson-red with a hot pale core, and nothing safe is ever red.

**Every prompt ends with this paragraph (also already in each prompt):**

> Strict side view, orthographic: no perspective, no 3/4 angle, no top surfaces showing. It must look like a concept artist's
> painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a glossy sci-fi UI. Big simple
> shapes that read at phone size. No text, no letters, no characters, no animals, no gold, no orange, no warm light.

---

## To paint

### OT-18 · Aiming turret (PAX-121)

**Save as:** `Art_Source/Traps_Obsidian/OT-18_aiming_turret_v1.png`, landscape 2:1, transparent. **Attach:** R1, OV-10a, OT-04 (your laser turret, so the two match).

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style image take ONLY the painting technique: soft painterly brushwork, luminous light, crafted detail. This place is the OBSIDIAN VOID: the remains of an ancient high-technology civilisation, machines and walls of black volcanic glass and polished obsidian with thin cyan light running in engraved circuit-like lines, floating in a starry void. Light is cold: a pale blue-white key light, cyan accents, deep navy shadows (never pure black). DANGER IS CRIMSON: every beam, field or light that can kill is crimson-red with a hot pale core, and nothing safe is ever red.

Paint TWO separate pieces side by side with a gap, for a turret whose eye turns to follow the player. LEFT: the MOUNT, a heavy carved obsidian socket shaped like a half-disc, its flat side the back (it bolts to a wall or a ceiling), engraved cyan circuit lines dim. RIGHT: the EYE, a round obsidian ball with a short lens snout on ONE side only, drawn pointing straight to the right, painted twice one above the other: IDLE (lens dark, a faint crimson ember deep inside) and LOCKED (lens blazing crimson-white, thin crimson light leaking around its rim). The eye is the same size in both and sits exactly in the mount's half-disc. The eye's centre is the middle of its ball, so it can be rotated in the game.

Strict side view, orthographic: no perspective, no 3/4 angle, no top surfaces showing. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a glossy sci-fi UI. Big simple shapes that read at phone size. No text, no letters, no characters, no animals, no gold, no orange, no warm light.
```

**Reject if:** the eye painted in perspective or turned toward the viewer; a long gun barrel; the mount and eye fused into one shape; cyan on the locked eye.

### OT-19 · Portal pair (PAX-120)

**Save as:** `Art_Source/Traps_Obsidian/OT-19_portal_v1.png`, landscape 3:1, transparent. **Attach:** R1, OV-10a, OV-01.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style image take ONLY the painting technique: soft painterly brushwork, luminous light, crafted detail. This place is the OBSIDIAN VOID: the remains of an ancient high-technology civilisation, machines and walls of black volcanic glass and polished obsidian with thin cyan light running in engraved circuit-like lines, floating in a starry void. Light is cold: a pale blue-white key light, cyan accents, deep navy shadows (never pure black). DANGER IS CRIMSON: every beam, field or light that can kill is crimson-red with a hot pale core, and nothing safe is ever red.

Paint THREE upright oval gateways side by side with gaps, each twice as tall as wide, each a thin ring of carved obsidian with cyan circuit lines framing a swirling window of starry void. ENTRY: the window a calm deep-blue swirl drawing inward, the ring lit steady cyan. EXIT: the same ring, the swirl pushing outward with a brighter cyan core. EXIT GLITCHING: the exit broken into horizontal slices offset sideways with cyan chromatic fringes and faint flicker, the same outline. All three the same size. Safe light, so no red anywhere.

Strict side view, orthographic: no perspective, no 3/4 angle, no top surfaces showing. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a glossy sci-fi UI. Big simple shapes that read at phone size. No text, no letters, no characters, no animals, no gold, no orange, no warm light.
```

**Reject if:** red or crimson; a 3/4 view of the ring; the three different sizes; a window painted as solid colour with no void in it.

### OT-20 · Phase platform (PAX-122)

**Save as:** `Art_Source/Traps_Obsidian/OT-20_phase_platform_v1.png`, landscape 3:1, transparent. **Attach:** R1, OV-10a, OT-14 (your lift).

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style image take ONLY the painting technique: soft painterly brushwork, luminous light, crafted detail. This place is the OBSIDIAN VOID: the remains of an ancient high-technology civilisation, machines and walls of black volcanic glass and polished obsidian with thin cyan light running in engraved circuit-like lines, floating in a starry void. Light is cold: a pale blue-white key light, cyan accents, deep navy shadows (never pure black). DANGER IS CRIMSON: every beam, field or light that can kill is crimson-red with a hot pale core, and nothing safe is ever red.

Paint the same small hard-light step three times, stacked one above the other with gaps, each about five times wider than tall: a thin obsidian frame at each end holding a slab of solid cyan light between them, its top a clean flat walkable line. LIT (the slab solid and bright), FADING (the same slab thinning to scanlines and flickering, half transparent: the warning) and OFF (only the two obsidian end caps left, dim, with empty space between them). Same size and position in all three.

Strict side view, orthographic: no perspective, no 3/4 angle, no top surfaces showing. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a glossy sci-fi UI. Big simple shapes that read at phone size. No text, no letters, no characters, no animals, no gold, no orange, no warm light.
```

**Reject if:** red; a wavering top line; the end caps different between states; the off state still showing a slab.

### OT-21 · Flip plate (PAX-115)

**Save as:** `Art_Source/Traps_Obsidian/OT-21_flip_plate_v1.png`, landscape 2:1, transparent. **Attach:** R1, OV-10a, OT-11 (your hinged panel).

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style image take ONLY the painting technique: soft painterly brushwork, luminous light, crafted detail. This place is the OBSIDIAN VOID: the remains of an ancient high-technology civilisation, machines and walls of black volcanic glass and polished obsidian with thin cyan light running in engraved circuit-like lines, floating in a starry void. Light is cold: a pale blue-white key light, cyan accents, deep navy shadows (never pure black). DANGER IS CRIMSON: every beam, field or light that can kill is crimson-red with a hot pale core, and nothing safe is ever red.

Paint ONE plate twice, one above the other with a gap, about four times wider than tall, pivoting on an axle through its middle: SAFE SIDE UP (a polished obsidian plate, its top a flat walkable line with thin cyan circuit lines, a dark row of closed vents along its underside) and DANGER SIDE UP (the same plate turned over: the vents now on top, open, each firing a short crimson energy blade upward, a hot pale core in every blade; the cyan lines now underneath). The axle cap the same in both, in the exact centre.

Strict side view, orthographic: no perspective, no 3/4 angle, no top surfaces showing. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a glossy sci-fi UI. Big simple shapes that read at phone size. No text, no letters, no characters, no animals, no gold, no orange, no warm light.
```

**Reject if:** perspective; the plate thicker than a quarter of its length; cyan on the blades; the two plates different sizes.

### OT-22 · Glitch platform (PAX-114)

**Save as:** `Art_Source/Traps_Obsidian/OT-22_glitch_platform_v1.png`, landscape 2:1, transparent. **Attach:** R1, OV-10a, OT-08 (your hologram glitch).

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style image take ONLY the painting technique: soft painterly brushwork, luminous light, crafted detail. This place is the OBSIDIAN VOID: the remains of an ancient high-technology civilisation, machines and walls of black volcanic glass and polished obsidian with thin cyan light running in engraved circuit-like lines, floating in a starry void. Light is cold: a pale blue-white key light, cyan accents, deep navy shadows (never pure black). DANGER IS CRIMSON: every beam, field or light that can kill is crimson-red with a hot pale core, and nothing safe is ever red.

Paint the same small floating platform three times side by side with gaps, each about three times wider than tall: a block of black obsidian glass with a flat walkable top lined with cyan light and a cyan core glowing under it. SOLID (steady and clean), WARNING (the same block breaking into horizontal slices shifted left and right with cyan chromatic fringes, it is about to jump away) and GHOST (a faint translucent cyan outline of the block, only the edges and scanlines, showing where it will appear next). Same size and position in all three.

Strict side view, orthographic: no perspective, no 3/4 angle, no top surfaces showing. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a glossy sci-fi UI. Big simple shapes that read at phone size. No text, no letters, no characters, no animals, no gold, no orange, no warm light.
```

**Reject if:** red; the warning so broken it no longer reads as the same block; a ghost brighter than the solid block.

### OT-23 · Retracting hard-light floor (hatches and bridges)

**Save as:** `Art_Source/Traps_Obsidian/OT-23_retract_floor_v1.png`, landscape 4:1, transparent. **Attach:** R1, OV-10a, OV-10b.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style image take ONLY the painting technique: soft painterly brushwork, luminous light, crafted detail. This place is the OBSIDIAN VOID: the remains of an ancient high-technology civilisation, machines and walls of black volcanic glass and polished obsidian with thin cyan light running in engraved circuit-like lines, floating in a starry void. Light is cold: a pale blue-white key light, cyan accents, deep navy shadows (never pure black). DANGER IS CRIMSON: every beam, field or light that can kill is crimson-red with a hot pale core, and nothing safe is ever red.

Paint TWO separate pieces with a gap. LEFT: an EMITTER CAP, a short carved obsidian block that sits flush in a floor, one face open with a cyan lens slot facing right. RIGHT: a long FLOOR STRIP of solid cyan hard light, as thick as a floor slab, its top a clean flat walkable line, with faint hexagonal texture inside and a brighter edge on top, tileable left to right. In the game the strip shrinks back into the cap, so the strip has no end cap of its own.

Strict side view, orthographic: no perspective, no 3/4 angle, no top surfaces showing. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a glossy sci-fi UI. Big simple shapes that read at phone size. No text, no letters, no characters, no animals, no gold, no orange, no warm light.
```

**Reject if:** red; perspective; the strip not tileable left to right; a strip that looks like glass that would shatter (that is OT-09).

---
### OT-24 · Trident emitter (PAX-123)

**Save as:** `Art_Source/Traps_Obsidian/OT-24_trident_emitter_v1.png`, landscape 2:1, transparent. **Attach:** R1, OV-10a, OT-04 (your laser turret, so the two match).
**In game:** about 1.2 u wide, set into the roof. Three crimson beams leave its underside, one straight down and two at fixed angles either side; the game draws the beams.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style image take ONLY the painting technique: soft painterly brushwork, luminous light, crafted detail. This place is the OBSIDIAN VOID: the remains of an ancient high-technology civilisation, machines and walls of black volcanic glass and polished obsidian with thin cyan light running in engraved circuit-like lines, floating in a starry void. Light is cold: a pale blue-white key light, cyan accents, deep navy shadows (never pure black). DANGER IS CRIMSON: every beam, field or light that can kill is crimson-red with a hot pale core, and nothing safe is ever red.

Paint ONE ceiling-mounted emitter: a heavy carved block of black obsidian, wider than it is tall, whose flat top bolts into a roof. Thin cyan circuit lines run across its body. Its underside has three small crimson lenses side by side, the middle one aimed straight down and the outer two angled outward, each with a hot pale core. It's dark and calm, and only the three lenses glow. Leave the area around it fully transparent. Do NOT paint any beams.

Strict side view, orthographic: no perspective, no 3/4 angle, no top surfaces showing. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a glossy sci-fi UI. Big simple shapes that read at phone size. No text, no letters, no characters, no animals, no gold, no orange, no warm light.
```

**Reject if:** beams are painted; the three lenses merge into one; it looks like a chandelier or a claw; any lens is cyan.

### OT-25 · Chaser housing (PAX-124)

**Save as:** `Art_Source/Traps_Obsidian/OT-25_chaser_housing_v1.png`, square, transparent. **Attach:** R1, OT-04.
**In game:** about 0.6 u across, on the pivot of L031's chasing laser. It turns with the beam, so it must look right at any angle. The beam is pale blue-white, the one exception to the crimson rule (D-276).

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style image take ONLY the painting technique: soft painterly brushwork, luminous light, crafted detail. This place is the OBSIDIAN VOID: the remains of an ancient high-technology civilisation, machines and walls of black volcanic glass and polished obsidian with thin cyan light running in engraved circuit-like lines, floating in a starry void. Light is cold: a pale blue-white key light, cyan accents, deep navy shadows (never pure black). DANGER IS CRIMSON: every beam, field or light that can kill is crimson-red with a hot pale core, and nothing safe is ever red.

Paint ONE small round machine core seen straight on, like a lens in a ring: an outer ring of black obsidian with thin engraved lines, and a pale blue-white glowing centre with a hot white core. It's symmetrical, so it looks the same when rotated. This one thing is pale blue-white, not crimson. Leave the area around it fully transparent. Do NOT paint any beam.

Strict side view, orthographic: no perspective, no 3/4 angle, no top surfaces showing. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a glossy sci-fi UI. Big simple shapes that read at phone size. No text, no letters, no characters, no animals, no gold, no orange, no warm light.
```

**Reject if:** it isn't round and symmetrical; it has a stalk or a mount; it reads as an eye with a pupil; the light is crimson.
