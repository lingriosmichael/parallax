# Obsidian Void (levels 31–60) · trap prompts (2026-10-23, D-172)

**Scope:** the trap and machine art for **L031–L060**, Obsidian Void's levels. The behaviour stays exactly as it is in Golden
Ruins; only the look changes, chosen by level range (PAX-A20). Five items belong to new trap behaviour tickets: OT-04, OT-05 and
OT-06 (PAX-111), OT-16 (PAX-112) and OT-17 (PAX-113). **Paint those after their ticket is approved**, so the shapes match what
gets built. Everything else can be painted now.

**The world:** the remains of an ancient high-technology civilisation (obsidian, black glass, circuit-like lines, cyan light),
floating in a starry void. The environment prompts are in `Docs/Art/ObsidianVoid_PROMPTS.md`.

**Colour rule (D-108, D-172), the most important thing in this file:**
- **Crimson** (deep red pushed toward crimson, about hue 353°) is **danger only**: laser beams, warning rays, energy fields, a
  drone's eye, the edge of a crushing face.
- **Cyan** is the world's light and means safe: tethers, grip panels, vents, platforms, the door.
- A player must be able to tell at a glance on a phone that red light kills and blue light doesn't.

**Status (2026-10-23):** all 17 painted and saved in `Art_Source/Traps_Obsidian/` (`OT-01` to `OT-17`; OT-06 and OT-07 share one
sheet, `OT-06-07_hard_light_bar_and_drone_v1.png`). Most are on real alpha; OT-04, OT-10, OT-13, OT-14, OT-15 and OT-16 are on flat
magenta (keyed out). OT-04/05/06, OT-16 and OT-17 were painted ahead of their tickets (PAX-111–113): if review changes a shape,
redo that one from its prompt below. Processing into Unity is PAX-A20.

## How to work

- **One ChatGPT chat** for the whole set. Attach the references each item names, then paste its whole prompt.
- **Transparency:** "transparent" means real alpha. If ChatGPT paints a checkerboard, regenerate, or ask for a flat pure magenta
  #FF00FF background. "On pure black" pieces are light: the tools turn black into transparency.
- **Checking:** use each item's reject list, and regenerate rather than fix by hand. Shrink to phone size before saving.
- **Saving:** save to `Art_Source/Traps_Obsidian/` with the exact file name. Tell Claude Code when a batch is in, and it processes
  them through the trap kit (PAX-A20).

**References:**
- **R1:** `Docs/Art/Reference/styleframe_A_v1.png` (painting technique only)
- **OV-10a:** your painted Obsidian stone fill (traps sit in it)
- **OV-01:** your painted Obsidian sky (light and palette)
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

## Hazards (always or sometimes deadly)

### OT-01 · Energy field (replaces the spike strip)

**Save as:** `Art_Source/Traps_Obsidian/OT-01_energy_field_v1.png`, landscape 4:1, transparent, tiles sideways. **Attach:** R1, OV-10a, R4.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style image take ONLY the painting technique: soft painterly brushwork, luminous light, crafted detail. This place is the OBSIDIAN VOID: the remains of an ancient high-technology civilisation, machines and walls of black volcanic glass and polished obsidian with thin cyan light running in engraved circuit-like lines, floating in a starry void. Light is cold: a pale blue-white key light, cyan accents, deep navy shadows (never pure black). DANGER IS CRIMSON: every beam, field or light that can kill is crimson-red with a hot pale core, and nothing safe is ever red.

Paint a long horizontal strip, seamlessly tileable left to right: a low obsidian EMITTER RAIL set into a floor (its top flush with the floor line, a row of small dark lenses along it), and rising from it a thin crackling SHEET OF CRIMSON ENERGY about as tall as the rail is wide, with a hot pale core along the bottom and flickering filaments at its top edge fading to transparent. It replaces a row of floor spikes: a cat that touches it dies, and that must be obvious instantly.

Strict side view, orthographic: no perspective, no 3/4 angle, no top surfaces showing. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a glossy sci-fi UI. Big simple shapes that read at phone size. No text, no letters, no characters, no animals, no gold, no orange, no warm light.
```

**Reject if:** a seam; cyan or blue energy (it must be crimson); taller than a quarter of the strip; it reads as decoration or fire.

### OT-02 · Laser grid (the deadly floor of a pit)

**Save as:** `Art_Source/Traps_Obsidian/OT-02_laser_grid_v1.png`, landscape 4:1, transparent above the grid, tiles sideways. **Attach:** R1, OV-10a, OV-01.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style image take ONLY the painting technique: soft painterly brushwork, luminous light, crafted detail. This place is the OBSIDIAN VOID: the remains of an ancient high-technology civilisation, machines and walls of black volcanic glass and polished obsidian with thin cyan light running in engraved circuit-like lines, floating in a starry void. Light is cold: a pale blue-white key light, cyan accents, deep navy shadows (never pure black). DANGER IS CRIMSON: every beam, field or light that can kill is crimson-red with a hot pale core, and nothing safe is ever red.

Paint the deadly floor at the bottom of a pit, as a horizontal strip, seamlessly tileable left to right: a heavy obsidian emitter bed along the bottom, and above it a lattice of thin crimson laser lines crossing in a low diagonal grid, glowing hot where they cross, with a faint crimson haze over it fading upward to transparent. Below the bed, only darkness. A fall into it is death, and it must read that way from far away.

Strict side view, orthographic: no perspective, no 3/4 angle, no top surfaces showing. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a glossy sci-fi UI. Big simple shapes that read at phone size. No text, no letters, no characters, no animals, no gold, no orange, no warm light.
```

**Reject if:** a seam; cyan lasers; a busy pattern that flickers when small; anything pool- or water-like.

### OT-03 · Floor emitters (replaces pop-up spikes)

**Save as:** `Art_Source/Traps_Obsidian/OT-03_floor_emitters_v1.png`, landscape 2:1, transparent. **Attach:** R1, OV-10a, P (`PILOT-01`).

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style image take ONLY the painting technique: soft painterly brushwork, luminous light, crafted detail. This place is the OBSIDIAN VOID: the remains of an ancient high-technology civilisation, machines and walls of black volcanic glass and polished obsidian with thin cyan light running in engraved circuit-like lines, floating in a starry void. Light is cold: a pale blue-white key light, cyan accents, deep navy shadows (never pure black). DANGER IS CRIMSON: every beam, field or light that can kill is crimson-red with a hot pale core, and nothing safe is ever red.

Paint the same floor trap twice, side by side with a gap, both flush with a flat floor line: OFF (a plate of obsidian set into the floor with a row of five narrow dark slits, almost invisible, just a hairline seam: the player should barely notice it) and ON (the same plate with five short, sharp crimson laser blades standing straight up out of the slits like a fence, each with a hot pale core, light spilling onto the plate). Same width and position in both.

Strict side view, orthographic: no perspective, no 3/4 angle, no top surfaces showing. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a glossy sci-fi UI. Big simple shapes that read at phone size. No text, no letters, no characters, no animals, no gold, no orange, no warm light.
```

**Reject if:** the OFF state stands out; the two states differ in size; the blades are cyan; anything below the floor line.

### OT-04 · Laser turret (replaces the arrow launcher; after PAX-111)

**Save as:** `Art_Source/Traps_Obsidian/OT-04_laser_turret_v1.png`, landscape 2:1, transparent. **Attach:** R1, OV-10a, P (`PILOT-02A`).

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style image take ONLY the painting technique: soft painterly brushwork, luminous light, crafted detail. This place is the OBSIDIAN VOID: the remains of an ancient high-technology civilisation, machines and walls of black volcanic glass and polished obsidian with thin cyan light running in engraved circuit-like lines, floating in a starry void. Light is cold: a pale blue-white key light, cyan accents, deep navy shadows (never pure black). DANGER IS CRIMSON: every beam, field or light that can kill is crimson-red with a hot pale core, and nothing safe is ever red.

Paint a laser TURRET built into an obsidian wall, twice, side by side with a gap, as a FLAT FRONT ELEVATION (the wall faces the viewer squarely): IDLE (a heavy carved obsidian housing set flush into the wall, engraved circuit lines dim, a round dark lens in its centre like a closed eye) and CHARGING (the same housing, its circuit lines lit crimson and the lens glowing hot crimson-white, a little crimson light spilling onto the stone). Thin margin of obsidian blocks around each, fading to transparent.

Strict side view, orthographic: no perspective, no 3/4 angle, no top surfaces showing. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a glossy sci-fi UI. Big simple shapes that read at phone size. No text, no letters, no characters, no animals, no gold, no orange, no warm light.
```

**Reject if:** a barrel sticking out toward the camera; any 3D depth; the two states differ in size; cyan on the charging state.

### OT-05 · Warning ray and laser beam (on black; after PAX-111)

**Save as:** `Art_Source/Traps_Obsidian/OT-05_ray_beam_v1.png`, landscape 4:1, on pure black. **Attach:** R1, OV-01.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style image take ONLY the painting technique: soft painterly brushwork, luminous light, crafted detail. This place is the OBSIDIAN VOID: the remains of an ancient high-technology civilisation, machines and walls of black volcanic glass and polished obsidian with thin cyan light running in engraved circuit-like lines, floating in a starry void. Light is cold: a pale blue-white key light, cyan accents, deep navy shadows (never pure black). DANGER IS CRIMSON: every beam, field or light that can kill is crimson-red with a hot pale core, and nothing safe is ever red.

Paint THREE separate horizontal light pieces on pure black, stacked with clear black gaps between them, each running the full width and tileable left to right:
1. the WARNING RAY: a hair-thin, faint crimson line with a soft glow and tiny flickering breaks, clearly a warning, not yet dangerous;
2. the BEAM: a thick, intense laser beam with a blinding pale-pink-white core, crimson edges and a soft crimson glow, with faint crackling filaments along it;
3. the BEAM'S END: a short piece showing where the beam strikes a wall, a bright crimson-white splash of light and sparks (not tileable; at the right end of its row).

Strict side view, orthographic: no perspective, no 3/4 angle, no top surfaces showing. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a glossy sci-fi UI. Big simple shapes that read at phone size. No text, no letters, no characters, no animals, no gold, no orange, no warm light.
```

**Reject if:** anything not on pure black; cyan or blue; the ray as thick or bright as the beam; a seam.

### OT-06 · Hard-light bar (replaces the lance; after PAX-111)

**Save as:** `Art_Source/Traps_Obsidian/OT-06_hard_light_bar_v1.png`, landscape 3:1, transparent. **Attach:** R1, OV-10a, P (`PILOT-02D`).

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style image take ONLY the painting technique: soft painterly brushwork, luminous light, crafted detail. This place is the OBSIDIAN VOID: the remains of an ancient high-technology civilisation, machines and walls of black volcanic glass and polished obsidian with thin cyan light running in engraved circuit-like lines, floating in a starry void. Light is cold: a pale blue-white key light, cyan accents, deep navy shadows (never pure black). DANGER IS CRIMSON: every beam, field or light that can kill is crimson-red with a hot pale core, and nothing safe is ever red.

Paint ONE solid bar of HARD LIGHT seen from the side, about eight times longer than it is thick: a straight beam of solid, glassy pale-cyan light with sharp clean edges, an inner bright core and faint geometric facets inside it, capped at both ends by small obsidian emitter collars. It must look SOLID, like something a cat can stand on, not like a laser. A thin crimson leading edge on its right-hand tip only (it kills while it flies).

Strict side view, orthographic: no perspective, no 3/4 angle, no top surfaces showing. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a glossy sci-fi UI. Big simple shapes that read at phone size. No text, no letters, no characters, no animals, no gold, no orange, no warm light.
```

**Reject if:** it looks soft or gaseous; red along the whole bar; bent or tapering; perspective.

### OT-07 · Security drone (replaces the storm cloud)

**Save as:** `Art_Source/Traps_Obsidian/OT-07_drone_v1.png`, landscape 2:1, transparent. **Attach:** R1, OV-01, P (`PILOT-08`).

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style image take ONLY the painting technique: soft painterly brushwork, luminous light, crafted detail. This place is the OBSIDIAN VOID: the remains of an ancient high-technology civilisation, machines and walls of black volcanic glass and polished obsidian with thin cyan light running in engraved circuit-like lines, floating in a starry void. Light is cold: a pale blue-white key light, cyan accents, deep navy shadows (never pure black). DANGER IS CRIMSON: every beam, field or light that can kill is crimson-red with a hot pale core, and nothing safe is ever red.

Paint ONE hovering security DRONE twice, side by side with a gap, in strict side view, each about twice as wide as it is tall: HUNTING (a sleek, heavy hovering machine of black glass and obsidian plates, an angular hull with a single crimson eye-lens in its front, a ring of faint cyan hover-light beneath it, an emitter nozzle pointing straight down from its belly, dark) and CHARGING (the same drone, its eye blazing crimson, the belly emitter glowing crimson-white with sparks gathering, as if about to fire straight down). Menacing and readable as a hunter.

Strict side view, orthographic: no perspective, no 3/4 angle, no top surfaces showing. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a glossy sci-fi UI. Big simple shapes that read at phone size. No text, no letters, no characters, no animals, no gold, no orange, no warm light.
```

**Reject if:** cute or round like a toy; propellers; the two states differ in size or pose; the eye or emitter not crimson.

### OT-08 · Hologram glitch (the fake platform's reveal)

**Save as:** `Art_Source/Traps_Obsidian/OT-08_hologram_glitch_v1.png`, landscape 3:1, transparent. **Attach:** R1, OV-10a.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style image take ONLY the painting technique: soft painterly brushwork, luminous light, crafted detail. This place is the OBSIDIAN VOID: the remains of an ancient high-technology civilisation, machines and walls of black volcanic glass and polished obsidian with thin cyan light running in engraved circuit-like lines, floating in a starry void. Light is cold: a pale blue-white key light, cyan accents, deep navy shadows (never pure black). DANGER IS CRIMSON: every beam, field or light that can kill is crimson-red with a hot pale core, and nothing safe is ever red.

The game's fake platforms look exactly like real obsidian floor until the cat steps on them; then they reveal they were holograms. Paint a sheet of THREE separate overlay pieces on transparency, spaced well apart, painted ON TOP of the existing stone (only the effect, no stone around it):
1. a faint flicker: thin horizontal cyan-white scanlines and a slight double-image edge;
2. a breaking glitch: the stone shape torn into offset horizontal slices with blocky cyan-white digital noise between them;
3. a dissolve: the shape breaking into drifting square pixels and scanline fragments fading out.

Strict side view, orthographic: no perspective, no 3/4 angle, no top surfaces showing. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a glossy sci-fi UI. Big simple shapes that read at phone size. No text, no letters, no characters, no animals, no gold, no orange, no warm light.
```

**Reject if:** stone painted around the effect; red (a hologram isn't the killer; the fall is); too busy at phone size.

### OT-09 · Glass crumble (replaces the crumbling floor's cracks)

**Save as:** `Art_Source/Traps_Obsidian/OT-09_glass_cracks_v1.png`, landscape 3:2, transparent. **Attach:** R1, OV-10a, P (`PILOT-15`).

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style image take ONLY the painting technique: soft painterly brushwork, luminous light, crafted detail. This place is the OBSIDIAN VOID: the remains of an ancient high-technology civilisation, machines and walls of black volcanic glass and polished obsidian with thin cyan light running in engraved circuit-like lines, floating in a starry void. Light is cold: a pale blue-white key light, cyan accents, deep navy shadows (never pure black). DANGER IS CRIMSON: every beam, field or light that can kill is crimson-red with a hot pale core, and nothing safe is ever red.

Paint a sprite sheet of FOUR separate overlay pieces on transparency, spaced well apart, flat front view, painted ON TOP of existing obsidian floor (only the cracks, no stone around them):
1. a small fresh crack in black glass: a few thin bright hairlines branching from one point, catching cold light;
2. a spreading crack: a starburst of glassy fractures across a block, pale-cyan edges where light enters;
3. a shattering crack: a dense web of fractures with glass shards about to fall out, dark gaps between them;
4. a long horizontal SHATTERING EDGE strip, seamless left to right: the broken edge of a glass slab giving way, with falling shards and glitter.

Strict side view, orthographic: no perspective, no 3/4 angle, no top surfaces showing. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a glossy sci-fi UI. Big simple shapes that read at phone size. No text, no letters, no characters, no animals, no gold, no orange, no warm light.
```

**Reject if:** stone around the cracks; warm brown cracks (this is glass); red.

---

## Machines (moving, carrying or pushing)

### OT-10 · Crusher block (replaces the slam block)

**Save as:** `Art_Source/Traps_Obsidian/OT-10_crusher_v1.png`, square 1:1, transparent. **Attach:** R1, OV-10a, P (`PILOT-07`).

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style image take ONLY the painting technique: soft painterly brushwork, luminous light, crafted detail. This place is the OBSIDIAN VOID: the remains of an ancient high-technology civilisation, machines and walls of black volcanic glass and polished obsidian with thin cyan light running in engraved circuit-like lines, floating in a starry void. Light is cold: a pale blue-white key light, cyan accents, deep navy shadows (never pure black). DANGER IS CRIMSON: every beam, field or light that can kill is crimson-red with a hot pale core, and nothing safe is ever red.

Paint ONE heavy CRUSHER BLOCK as a flat front elevation, about one and a half times wider than tall: a massive slab of polished obsidian in a dark metal frame, engraved circuit lines glowing faint cyan on its face, and along its BOTTOM edge (the crushing face) a band of crimson warning light with a row of short hot crimson teeth. It has to look incredibly heavy and dangerous from below.

Strict side view, orthographic: no perspective, no 3/4 angle, no top surfaces showing. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a glossy sci-fi UI. Big simple shapes that read at phone size. No text, no letters, no characters, no animals, no gold, no orange, no warm light.
```

**Reject if:** chains or ropes; perspective; no crimson on the crushing face; red over the whole block.

### OT-11 · Hinged panel (replaces the hinge floor)

**Save as:** `Art_Source/Traps_Obsidian/OT-11_hinge_panel_v1.png`, landscape 2:1, transparent. **Attach:** R1, OV-10a, P (`PILOT-11`).

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style image take ONLY the painting technique: soft painterly brushwork, luminous light, crafted detail. This place is the OBSIDIAN VOID: the remains of an ancient high-technology civilisation, machines and walls of black volcanic glass and polished obsidian with thin cyan light running in engraved circuit-like lines, floating in a starry void. Light is cold: a pale blue-white key light, cyan accents, deep navy shadows (never pure black). DANGER IS CRIMSON: every beam, field or light that can kill is crimson-red with a hot pale core, and nothing safe is ever red.

Paint the same floor panel twice, side by side with a gap: FLAT (a long obsidian floor panel with a hinge cylinder at its left end, its top a flat walkable line with faint cyan circuit lines) and SPIKED EDGE (the same panel, its right-hand edge lined with a strip of crimson energy blades, for the version that kills). Same size in both.

Strict side view, orthographic: no perspective, no 3/4 angle, no top surfaces showing. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a glossy sci-fi UI. Big simple shapes that read at phone size. No text, no letters, no characters, no animals, no gold, no orange, no warm light.
```

**Reject if:** perspective; the panel thicker than a fifth of its length; red on the plain panel.

### OT-12 · Gravity vent (replaces the geyser)

**Save as:** `Art_Source/Traps_Obsidian/OT-12_gravity_vent_v1.png`, landscape 3:2, transparent. **Attach:** R1, OV-10a, P (`PILOT-02B`).

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style image take ONLY the painting technique: soft painterly brushwork, luminous light, crafted detail. This place is the OBSIDIAN VOID: the remains of an ancient high-technology civilisation, machines and walls of black volcanic glass and polished obsidian with thin cyan light running in engraved circuit-like lines, floating in a starry void. Light is cold: a pale blue-white key light, cyan accents, deep navy shadows (never pure black). DANGER IS CRIMSON: every beam, field or light that can kill is crimson-red with a hot pale core, and nothing safe is ever red.

Paint TWO separate pieces with a gap: a VENT PLATE (a flat obsidian grille set flush into a floor, concentric engraved rings around a round grate, the rings faintly cyan; seen from the side, so only a thin front face shows above the floor line) and its COLUMN (a tall rising column of anti-gravity light above an identical plate: pale cyan particles and rings of light streaming straight up, strongest at the bottom, fading out at the top). Safe-looking (it lifts the cat), so cyan, never red.

Strict side view, orthographic: no perspective, no 3/4 angle, no top surfaces showing. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a glossy sci-fi UI. Big simple shapes that read at phone size. No text, no letters, no characters, no animals, no gold, no orange, no warm light.
```

**Reject if:** water or steam; red; the plate sticking up above the floor line.

### OT-13 · Gravity rings (replace the flip orb and the inverter orb)

**Save as:** `Art_Source/Traps_Obsidian/OT-13_gravity_rings_v1.png`, landscape 2:1, transparent. **Attach:** R1, OV-01, P (`PILOT-09`, `PILOT-13`).

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style image take ONLY the painting technique: soft painterly brushwork, luminous light, crafted detail. This place is the OBSIDIAN VOID: the remains of an ancient high-technology civilisation, machines and walls of black volcanic glass and polished obsidian with thin cyan light running in engraved circuit-like lines, floating in a starry void. Light is cold: a pale blue-white key light, cyan accents, deep navy shadows (never pure black). DANGER IS CRIMSON: every beam, field or light that can kill is crimson-red with a hot pale core, and nothing safe is ever red.

Paint TWO separate floating devices with a gap, the same size: a GRAVITY RING (an obsidian ring standing on its edge with an arrow glyph inside it pointing up and down, cyan light running around the ring) and an INVERTER RING (the same ring, its glyph two arrows crossing left and right, its light pale violet-white instead of cyan, so the two never look alike).

Strict side view, orthographic: no perspective, no 3/4 angle, no top surfaces showing. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a glossy sci-fi UI. Big simple shapes that read at phone size. No text, no letters, no characters, no animals, no gold, no orange, no warm light.
```

**Reject if:** the two look alike; red; text or letters.

### OT-14 · Lift platform (replaces the elevator)

**Save as:** `Art_Source/Traps_Obsidian/OT-14_lift_v1.png`, landscape 4:1, transparent. **Attach:** R1, OV-10a, OV-10b.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style image take ONLY the painting technique: soft painterly brushwork, luminous light, crafted detail. This place is the OBSIDIAN VOID: the remains of an ancient high-technology civilisation, machines and walls of black volcanic glass and polished obsidian with thin cyan light running in engraved circuit-like lines, floating in a starry void. Light is cold: a pale blue-white key light, cyan accents, deep navy shadows (never pure black). DANGER IS CRIMSON: every beam, field or light that can kill is crimson-red with a hot pale core, and nothing safe is ever red.

Paint ONE moving lift platform seen from the side, about four times wider than tall: a slab of polished obsidian with a clean flat walkable top and a thin cyan rim of light along it, and under it a compact engine housing with two faint cyan hover-glows. Solid and safe-looking.

Strict side view, orthographic: no perspective, no 3/4 angle, no top surfaces showing. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a glossy sci-fi UI. Big simple shapes that read at phone size. No text, no letters, no characters, no animals, no gold, no orange, no warm light.
```

**Reject if:** a wavering top line; red; chains or cables.

### OT-15 · Falling monolith (replaces the falling block)

**Save as:** `Art_Source/Traps_Obsidian/OT-15_monolith_slab_v1.png`, square 1:1, transparent. **Attach:** R1, OV-10a.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style image take ONLY the painting technique: soft painterly brushwork, luminous light, crafted detail. This place is the OBSIDIAN VOID: the remains of an ancient high-technology civilisation, machines and walls of black volcanic glass and polished obsidian with thin cyan light running in engraved circuit-like lines, floating in a starry void. Light is cold: a pale blue-white key light, cyan accents, deep navy shadows (never pure black). DANGER IS CRIMSON: every beam, field or light that can kill is crimson-red with a hot pale core, and nothing safe is ever red.

Paint ONE square-ish block of the void's architecture that will fall: a heavy obsidian slab with chipped glassy edges, one engraved circuit line along its face glowing faint cyan, a thin crimson crack of light along its BOTTOM edge only (it kills what it lands on).

Strict side view, orthographic: no perspective, no 3/4 angle, no top surfaces showing. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a glossy sci-fi UI. Big simple shapes that read at phone size. No text, no letters, no characters, no animals, no gold, no orange, no warm light.
```

**Reject if:** red over the whole block; perspective; looks light or hollow.

### OT-16 · Pulse gate (after PAX-112)

**Save as:** `Art_Source/Traps_Obsidian/OT-16_pulse_gate_v1.png`, landscape 3:1, transparent. **Attach:** R1, OV-10a.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style image take ONLY the painting technique: soft painterly brushwork, luminous light, crafted detail. This place is the OBSIDIAN VOID: the remains of an ancient high-technology civilisation, machines and walls of black volcanic glass and polished obsidian with thin cyan light running in engraved circuit-like lines, floating in a starry void. Light is cold: a pale blue-white key light, cyan accents, deep navy shadows (never pure black). DANGER IS CRIMSON: every beam, field or light that can kill is crimson-red with a hot pale core, and nothing safe is ever red.

Paint the same laser gate three times, side by side with gaps, each a vertical barrier between a floor emitter and a ceiling emitter (two short obsidian posts with lenses facing each other, one at the bottom and one at the top of the frame): OFF (the emitters dim, nothing between them), WARNING (thin flickering crimson threads crossing the gap, faint and broken) and ON (a solid sheet of intense crimson laser light between the emitters with a pale hot core). Same size and position in all three.

Strict side view, orthographic: no perspective, no 3/4 angle, no top surfaces showing. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a glossy sci-fi UI. Big simple shapes that read at phone size. No text, no letters, no characters, no animals, no gold, no orange, no warm light.
```

**Reject if:** the three differ in size; cyan barrier; the warning as strong as ON.

### OT-17 · Scanner (after PAX-113)

**Save as:** `Art_Source/Traps_Obsidian/OT-17_scanner_v1.png`, portrait 1:2, transparent. **Attach:** R1, OV-10a.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). From the attached style image take ONLY the painting technique: soft painterly brushwork, luminous light, crafted detail. This place is the OBSIDIAN VOID: the remains of an ancient high-technology civilisation, machines and walls of black volcanic glass and polished obsidian with thin cyan light running in engraved circuit-like lines, floating in a starry void. Light is cold: a pale blue-white key light, cyan accents, deep navy shadows (never pure black). DANGER IS CRIMSON: every beam, field or light that can kill is crimson-red with a hot pale core, and nothing safe is ever red.

Paint TWO separate pieces with a gap: a SCANNER HEAD (a compact obsidian emitter riding on a short rail section, its lens pointing down, crimson glow inside) and the SCAN BEAM (a tall vertical sheet of crimson laser light with a bright core and soft scanline texture, tileable top to bottom, between two identical heads, one upside down at the top and one at the bottom).

Strict side view, orthographic: no perspective, no 3/4 angle, no top surfaces showing. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a glossy sci-fi UI. Big simple shapes that read at phone size. No text, no letters, no characters, no animals, no gold, no orange, no warm light.
```

**Reject if:** cyan beam; perspective; the beam not tileable vertically.

---

## Already covered elsewhere

- **Light tether** (the vine): OV-18 in the environment prompts. A snapping tether simply switches off, so it needs no new painting.
- **Grip panel** (the grip wall): OV-31.
- **Door:** OV-24. **Checkpoint:** OV-25.
- **Retreating door:** the door on its lift, so OV-24 plus OT-14.
