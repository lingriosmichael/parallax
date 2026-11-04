# Obsidian Void · trap paintings still to do (updated 2026-11-01)

**All seven below are painted and in the game (D-292); the prompts stay for a repaint.**

**Only what's missing is listed here.** OT-01 to OT-17 are painted and in the game, so they've been removed; the full history is in git. These pieces are in L031–L040 and
still draw as placeholders (stone, grey or plain squares).

| Paint | What | Seen in levels | Note |
|---|---|---|---|
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

**The finish (the developer's GT-01 note: "Much better"):** simple shapes, rich materials. Every piece is one crafted object from a civilisation long dead: chipped glass, scored stone, circuits gone dim in places, so nothing repeats like a pattern and nothing looks like clean plastic. Light is carved into the material, never stuck on.

## How to work

- **One ChatGPT chat** for the whole set. Attach the references each item names, then paste its whole prompt.
- **Backgrounds:** the prompts ask for flat pure magenta #FF00FF, which the tools turn into transparency. If red light fringes on it, ask for a flat pure green #00FF00 background instead.
- **If it starts to look like clip-art,** start a new chat. An earlier image's look carries over.
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

**Save as:** `Art_Source/Traps_Obsidian/OT-18_aiming_turret_v1.png`, landscape 2:1, magenta background. **Attach:** R1, OV-10a, OT-04 (your laser turret, so the two match).

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style image take ONLY the painting technique: soft painterly brushwork, luminous light, crafted detail. From the other attached images take their weight, wear and finish. This place is the OBSIDIAN VOID: the remains of an ancient high-technology civilisation, machines and walls of black volcanic glass and polished obsidian with thin cyan light running in engraved circuit-like lines, floating in a starry void. Light is cold: a pale blue-white key light, cyan accents, deep navy shadows (never pure black). DANGER IS CRIMSON: every beam, field or light that can kill is crimson-red with a hot pale core, and nothing safe is ever red.

Paint TWO separate pieces side by side with a gap, for a turret whose eye turns to follow the player. LEFT: the MOUNT, one heavy half-disc of carved obsidian, its flat side the back (it bolts to a wall or a ceiling), chipped along its edges and scored by age, its engraved cyan circuit lines mostly dead, with a few still faintly lit and no two alike. RIGHT: the EYE, a round ball of black volcanic glass with hairline cracks and one short lens snout on ONE side only, drawn pointing straight to the right, painted twice one above the other: IDLE (the lens dark, a faint crimson ember deep inside the glass) and LOCKED (the lens blazing with a white-hot core melting into crimson, thin crimson light leaking through the cracks around its rim). The eye is the same size in both and sits exactly in the mount's half-disc. The eye's centre is the middle of its ball, so it can be rotated in the game. A cold rim of pale blue light along the upper edges, deep navy shadow beneath.

Strict side view, orthographic: no perspective, no 3/4 angle, no top surfaces showing. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a flat game asset, not a glossy sci-fi UI. Big simple shapes that read at phone size. The background is one flat pure magenta (#FF00FF), edge to edge, with no shadow on it. No text, no letters, no characters, no animals, no gold, no orange, no warm light.
```

**Reject if:** the eye painted in perspective or turned toward the viewer; a long gun barrel; the mount and eye fused into one shape; cyan on the locked eye; clean plastic with no wear.

### OT-20 · Phase platform (PAX-122)

**Save as:** `Art_Source/Traps_Obsidian/OT-20_phase_platform_v1.png`, landscape 3:1, magenta background. **Attach:** R1, OV-10a, OT-14 (your lift).

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style image take ONLY the painting technique: soft painterly brushwork, luminous light, crafted detail. From the other attached images take their weight, wear and finish. This place is the OBSIDIAN VOID: the remains of an ancient high-technology civilisation, machines and walls of black volcanic glass and polished obsidian with thin cyan light running in engraved circuit-like lines, floating in a starry void. Light is cold: a pale blue-white key light, cyan accents, deep navy shadows (never pure black). DANGER IS CRIMSON: every beam, field or light that can kill is crimson-red with a hot pale core, and nothing safe is ever red.

Paint the same small hard-light step three times, stacked one above the other with gaps, each about five times wider than tall. It is a slab of solid cyan light held between two thick end caps of chipped, ancient obsidian, its top a clean flat walkable line. The light has depth, brighter at its core and uneven inside, like a bar of frozen plasma, and the left cap is more worn than the right. LIT (the slab solid and bright), FADING (the same slab thinning to torn scanlines and flickering, half transparent: the warning) and OFF (only the two obsidian end caps left, dim, with empty space between them). Same size and position in all three, and the caps identical in all three.

Strict side view, orthographic: no perspective, no 3/4 angle, no top surfaces showing. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a flat game asset, not a glossy sci-fi UI. Big simple shapes that read at phone size. The background is one flat pure magenta (#FF00FF), edge to edge, with no shadow on it. No text, no letters, no characters, no animals, no gold, no orange, no warm light.
```

**Reject if:** red; a wavering top line; the end caps different between states; the off state still showing a slab; a flat neon bar with no depth.

### OT-21 · Flip plate (PAX-115)

**Save as:** `Art_Source/Traps_Obsidian/OT-21_flip_plate_v1.png`, landscape 2:1, magenta background. **Attach:** R1, OV-10a, OT-11 (your hinged panel).

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style image take ONLY the painting technique: soft painterly brushwork, luminous light, crafted detail. From the other attached images take their weight, wear and finish. This place is the OBSIDIAN VOID: the remains of an ancient high-technology civilisation, machines and walls of black volcanic glass and polished obsidian with thin cyan light running in engraved circuit-like lines, floating in a starry void. Light is cold: a pale blue-white key light, cyan accents, deep navy shadows (never pure black). DANGER IS CRIMSON: every beam, field or light that can kill is crimson-red with a hot pale core, and nothing safe is ever red.

Paint ONE plate twice, one above the other with a gap, about four times wider than tall, pivoting on an axle through its middle. It is a single slab of ancient polished obsidian, chipped at its corners and scored along its length, as heavy as a tomb door. SAFE SIDE UP: its top a flat walkable line with a few thin cyan circuit lines, some gone dark, and a line of closed vents along its underside, cut unevenly by hand and no two alike. DANGER SIDE UP: the same plate turned over, the vents now on top and open, each firing a short crimson energy blade upward, the blades different lengths with a white-hot core melting into crimson; the cyan lines now underneath. The worn axle cap is the same in both, in the exact centre.

Strict side view, orthographic: no perspective, no 3/4 angle, no top surfaces showing. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a flat game asset, not a glossy sci-fi UI. Big simple shapes that read at phone size. The background is one flat pure magenta (#FF00FF), edge to edge, with no shadow on it. No text, no letters, no characters, no animals, no gold, no orange, no warm light.
```

**Reject if:** perspective; the plate thicker than a quarter of its length; cyan on the blades; the two plates different sizes; identical blades in a neat row.

### OT-22 · Glitch platform (PAX-114)

**Save as:** `Art_Source/Traps_Obsidian/OT-22_glitch_platform_v1.png`, landscape 2:1, magenta background. **Attach:** R1, OV-10a, OT-08 (your hologram glitch).

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style image take ONLY the painting technique: soft painterly brushwork, luminous light, crafted detail. From the other attached images take their weight, wear and finish. This place is the OBSIDIAN VOID: the remains of an ancient high-technology civilisation, machines and walls of black volcanic glass and polished obsidian with thin cyan light running in engraved circuit-like lines, floating in a starry void. Light is cold: a pale blue-white key light, cyan accents, deep navy shadows (never pure black). DANGER IS CRIMSON: every beam, field or light that can kill is crimson-red with a hot pale core, and nothing safe is ever red.

Paint the same small floating platform three times side by side with gaps, each about three times wider than tall: one rough-cut block of black obsidian glass, chipped and cracked along its underside like a broken piece of a greater machine, with a flat walkable top edged in cyan light and a cyan core burning unevenly deep inside it. SOLID (steady, heavy and real), WARNING (the same block slicing into horizontal bands of different widths, shifted left and right with cyan chromatic fringes, about to jump away) and GHOST (a faint translucent cyan outline of the block, only its edges and scanlines, showing where it will appear next). Same size and position in all three.

Strict side view, orthographic: no perspective, no 3/4 angle, no top surfaces showing. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a flat game asset, not a glossy sci-fi UI. Big simple shapes that read at phone size. The background is one flat pure magenta (#FF00FF), edge to edge, with no shadow on it. No text, no letters, no characters, no animals, no gold, no orange, no warm light.
```

**Reject if:** red; the warning so broken it no longer reads as the same block; a ghost brighter than the solid block; a clean rectangle with no broken edges.

### OT-23 · Retracting hard-light floor (hatches and bridges)

**Save as:** `Art_Source/Traps_Obsidian/OT-23_retract_floor_v1.png`, landscape 4:1, magenta background. **Attach:** R1, OV-10a, OV-10b.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style image take ONLY the painting technique: soft painterly brushwork, luminous light, crafted detail. From the other attached images take their weight, wear and finish. This place is the OBSIDIAN VOID: the remains of an ancient high-technology civilisation, machines and walls of black volcanic glass and polished obsidian with thin cyan light running in engraved circuit-like lines, floating in a starry void. Light is cold: a pale blue-white key light, cyan accents, deep navy shadows (never pure black). DANGER IS CRIMSON: every beam, field or light that can kill is crimson-red with a hot pale core, and nothing safe is ever red.

Paint TWO separate pieces with a gap. LEFT: an EMITTER CAP, one short block of carved, age-scarred obsidian that sits flush in a floor, one face open on a cyan lens slot facing right, its circuit lines partly dead. RIGHT: a long FLOOR STRIP of solid cyan hard light, as thick as a floor slab, its top a clean flat walkable line. The light has depth: a brighter upper edge, a soft uneven glow inside it and a faint, irregular crackle of energy, with no regular grid or hexagon pattern, and it tiles left to right with no seam. In the game the strip shrinks back into the cap, so the strip has no end cap of its own.

Strict side view, orthographic: no perspective, no 3/4 angle, no top surfaces showing. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a flat game asset, not a glossy sci-fi UI. Big simple shapes that read at phone size. The background is one flat pure magenta (#FF00FF), edge to edge, with no shadow on it. No text, no letters, no characters, no animals, no gold, no orange, no warm light.
```

**Reject if:** red; perspective; the strip not tileable left to right; a visible repeating pattern in the strip; a strip that looks like glass that would shatter (that is OT-09).

---
### OT-24 · Trident emitter (PAX-123)

**Save as:** `Art_Source/Traps_Obsidian/OT-24_trident_emitter_v1.png`, landscape 2:1, magenta background. **Attach:** R1, OV-10a, OT-04 (your laser turret, so the two match).
**In game:** about 1.2 u wide, set into the roof. Three crimson beams leave its underside, one straight down and two at fixed angles either side; the game draws the beams.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style image take ONLY the painting technique: soft painterly brushwork, luminous light, crafted detail. From the other attached images take their weight, wear and finish. This place is the OBSIDIAN VOID: the remains of an ancient high-technology civilisation, machines and walls of black volcanic glass and polished obsidian with thin cyan light running in engraved circuit-like lines, floating in a starry void. Light is cold: a pale blue-white key light, cyan accents, deep navy shadows (never pure black). DANGER IS CRIMSON: every beam, field or light that can kill is crimson-red with a hot pale core, and nothing safe is ever red.

Paint ONE ceiling-mounted emitter, a single massive relic: a heavy carved block of black obsidian, wider than it is tall, chipped along its edges, whose flat top bolts into a roof. A few thin cyan circuit lines run across its body, some gone dead, none repeating. Its underside has three crimson lenses, each sunk in its own hand-cut socket, the middle one aimed straight down and the outer two angled outward, each with a white-hot core melting into crimson. It's dark and calm, and only the three lenses glow. Do NOT paint any beams.

Strict side view, orthographic: no perspective, no 3/4 angle, no top surfaces showing. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a flat game asset, not a glossy sci-fi UI. Big simple shapes that read at phone size. The background is one flat pure magenta (#FF00FF), edge to edge, with no shadow on it. No text, no letters, no characters, no animals, no gold, no orange, no warm light.
```

**Reject if:** beams are painted; the three lenses merge into one; it looks like a chandelier or a claw; any lens is cyan; clean plastic with no wear.

### OT-25 · Chaser housing (PAX-124)

**Save as:** `Art_Source/Traps_Obsidian/OT-25_chaser_housing_v1.png`, square, magenta background. **Attach:** R1, OT-04.
**In game:** about 0.6 u across, on the pivot of L031's chasing laser. It turns with the beam, so it must look right at any angle. The beam is pale blue-white, the one exception to the crimson rule (D-276).

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style image take ONLY the painting technique: soft painterly brushwork, luminous light, crafted detail. From the other attached images take their weight, wear and finish. This place is the OBSIDIAN VOID: the remains of an ancient high-technology civilisation, machines and walls of black volcanic glass and polished obsidian with thin cyan light running in engraved circuit-like lines, floating in a starry void. Light is cold: a pale blue-white key light, cyan accents, deep navy shadows (never pure black). DANGER IS CRIMSON: every beam, field or light that can kill is crimson-red with a hot pale core, and nothing safe is ever red.

Paint ONE small round machine core seen straight on, like a lens in a ring: a thick outer ring of black obsidian with a few fine engraved lines and tiny chips along its edge, and a pale blue-white glowing centre with a hot white core and real depth, like light deep inside glass. Its overall shape is perfectly round and even, so it looks the same when rotated, but the surface is old and hand-made, not smooth plastic. This one thing is pale blue-white, not crimson. Do NOT paint any beam.

Strict side view, orthographic: no perspective, no 3/4 angle, no top surfaces showing. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a flat game asset, not a glossy sci-fi UI. Big simple shapes that read at phone size. The background is one flat pure magenta (#FF00FF), edge to edge, with no shadow on it. No text, no letters, no characters, no animals, no gold, no orange, no warm light.
```

**Reject if:** it isn't round; it has a stalk or a mount; it reads as an eye with a pupil; the light is crimson; a smooth clean UI button.
