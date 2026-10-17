# PAX-A18 · Pilot: five paintings before the full set (2026-10-16)

> **Status (2026-10-16):** every prompt in this file is **painted** (✅), except 12, the vine, which is **skipped** (⏭). The developer is saving them to the paths given.
> **Don't paint them again.** Claude Code processes them once they're saved. The next prompts are the background set in
> `PAX-A18_BACKGROUND_PROMPTS.md`.

These five images test whether ChatGPT can produce art good enough for the game before anyone paints 91. Claude Code
processes them and renders them in game at phone scale, beside today's art.

## How to work

- One new ChatGPT chat for 1–2 (the traps), one for 3–5 (Sky Isles).
- Attach the reference images listed for each item, then paste its whole prompt as one message. Each prompt is
  self-contained.
- Ask for the aspect given. "Transparent" means a PNG with real alpha. If ChatGPT paints a grey-and-white checkerboard,
  say "The background must be truly transparent, not a checkerboard pattern" and regenerate.
- Check the result against its "Reject if" list. Regenerate rather than fixing it by hand; two or three tries is normal.
  Keep the best one.
- Save to the exact path given.

## Reference images

| Ref | File | Why |
|---|---|---|
| R1 | `Docs/Art/Reference/styleframe_A_v1.png` | the painting technique and detail level |
| R2 | `DesignImages/14_environment-breakdown.png` | the layered depth and the light |
| R3 | `Art_Source/Environment/GoldenRuins/A_GAME_Platform_Fill__raw_v1.png` | today's sandstone (traps sit in it) |
| R4 | `~/Parallax_batch_tools/PAX-A18/stop2/L002_play_after.png` | the real in-game scale: how small things are on a phone |

---

## ~~1 · Spike blades, black iron~~ ✅ painted (2026-10-16)

**Save as:** `Art_Source/Traps_ChatGPT/PILOT-01_spike_blades_iron_v1.png`, landscape 3:2, transparent.
**Attach:** R1, R3, R4.

```text
I'm making a premium hand-painted 2D side-view platformer for phones. The first image shows the painting style I want (brushwork, edges, detail level). The second is the sandstone the traps sit in. The third is a real gameplay screenshot at phone scale: notice how small things are next to the black cat.

Paint a sprite sheet of FIVE separate floor spikes on a fully transparent background, spaced well apart in one row, each standing upright on the same flat baseline, seen exactly from the side (orthographic, no perspective, no ground, no shadow on the ground).

They are forged black iron trap spikes, old and heavy, hammered by hand, each a DIFFERENT shape so a row of them never looks like an even picket fence:
1. a tall thin square-section spike, slightly bent near the tip;
2. a short broad flat triangular blade with a notch in one edge;
3. a medium spike with a barb halfway up;
4. a tall spike with a thick riveted collar at its base;
5. a short stubby spike, its tip blunted and chipped.
Heights vary: the tallest about 2 times the shortest.

Material and light: near-black iron (very dark, not grey), hammered facets, a few flakes of rust in dark brown, NO red. A warm golden light from the upper left catches one edge of each spike as a thin bright line, so the silhouette reads against any background. The base of each spike widens into a rough forged foot.

In the game each spike is only about 40-50 pixels tall on a phone, so: bold simple silhouettes, big clear shapes, the bright edge line strong; detail only inside the shapes, never fine spiky noise on the outline.

It must look like a real heavy iron object made for an ancient temple trap, never like a vector icon, a cartoon, a UI symbol or a clean 3D render. No text, no border, no characters, no red or crimson anywhere, no glow, no background.
```

**Reject if:**
- the spikes are grey or silver rather than near-black;
- they are identical copies;
- any red appears;
- there is perspective, a ground plane or a shadow;
- they read as vector or flat icons;
- the background isn't transparent.

---

## 2 · Trap fittings (revised 2026-10-16: one object per image, flat front elevation)

**Why it was revised:** the first try produced three-quarter-view boxed props with leaves on every edge and a generic
pebble texture. They looked like mobile-game icons, not parts of a wall. Each piece is now its own image, a flat front
elevation built into the masonry, with a designed history. The spike socket comes later, with the spikes.

**Tips:**
- If it shows perspective, reply: "Make it a perfectly flat front elevation. I must not see any top surface."
- If it's too busy, reply: "Simplify: fewer, bigger shapes, it's tiny on a phone."
- Before saving, shrink it to about 60 px on screen: the main shapes must still read.

### ~~2A · Arrow launcher~~ ✅ painted (2026-10-16)

**Save as:** `Art_Source/Traps_ChatGPT/PILOT-02A_launcher_v1.png`, square 1:1, transparent (or flat #FF00FF).
**Attach:** R1, R3.

```text
I'm making a premium, hand-painted 2D side-view platformer (a paid game, so the art must be of the highest quality: painterly, rich, crafted). The first image shows my painting style: soft painterly brushwork, warm golden backlight, deep warm shadows, every surface weathered and alive. The second image is the exact sandstone masonry of my level's walls.

Paint a single ancient ARROW TRAP built into a temple wall, as a FLAT FRONT ELEVATION: like an architect's front view, the wall plane faces the viewer squarely. No perspective at all, no top or side surfaces visible, no 3/4 angle, nothing sticking out toward the camera.

The design (one object, centred, filling about 70% of the image):
- A heavy carved stone FRAME set flush into the masonry: a thick lintel stone on top and two jamb stones at the sides, carved in low relief with a worn, stylised serpent motif that coils around the opening (simple bold forms, eroded smooth by centuries, chipped at the corners, one hairline crack).
- In the centre, a deep horizontal SLOT going far back into darkness. Its lips are lined with dark tarnished bronze plates held by square rivets, the bronze worn to a dull warm brown with pale gold on its edges.
- Deep inside the slot, barely visible in the shadow, the dark iron points of three arrows waiting, lined up in a row.
- Below the slot, long dark SOOT and scratch marks streaking down the stone where arrows have fired thousands of times, and a few small stones missing.
- A little dry moss in the deepest grooves of the carving only. No leaves, no vines, no flowers sticking out.
- Around the frame, only a thin margin of the same sandstone blocks as the second image, fading out to transparent at the edges (so it blends into my wall).

Light: warm golden light from the upper left grazing across the relief, so the carving reads through light and shadow; warm brown shadows; the slot's interior the darkest part of the image.

It will be small on a phone (about 50 pixels square), so the big shapes must read instantly: the dark slot, the pale lit frame and the serpent's bold coil. Fine detail lives inside those shapes.

Background: fully transparent (real alpha, not a checkerboard pattern). If you can't make it transparent, use a flat pure magenta #FF00FF background instead.

Not a game icon, not a cartoon, not a 3D render, not a separate block or tile: a piece of an ancient wall painted by a concept artist. No text, no characters, no red or crimson anywhere, no glowing effects.
```

**Reject if:**
- a top surface or any 3D depth shows;
- it's a separate cube or block;
- leaves or vines stick out;
- the slot isn't the darkest, clearest shape;
- the stone doesn't look like R3;
- the arrows are shiny or silver.

### ~~2B · Geyser vent~~ ✅ painted (2026-10-16)

**Save as:** `Art_Source/Traps_ChatGPT/PILOT-02B_vent_v1.png`, landscape 3:2, transparent (or flat #FF00FF).
**Attach:** R1, R3.

```text
Same game, same style and stone as before: premium hand-painted art, warm golden backlight.

Paint a single ancient GEYSER VENT set into a temple floor, as a STRICT SIDE ELEVATION: exactly level with the floor, so we see only the front face of the stone and the vent's silhouette on top. No perspective, no top surface visible, no looking down into the hole.

The design (centred, about 4 times wider than tall, filling about 70% of the width):
- A low carved sandstone collar rising slightly above the floor line, its front face carved in low relief with worn wave-like ridges, eroded and chipped.
- On top, a dark gap where the water erupts from, shown as a dark notch in the collar's silhouette with wet, darker stone around it.
- Long mineral stains, pale cream and rust-brown, streaking down the front face from the top, and a darker wet band where water splashes back.
- Thin dry moss only in the deepest grooves. No leaves, no vines sticking out.
- To the left and right, only a short stretch of the same floor stone, fading out to transparent.

Light from the upper left, warm brown shadows. Small on a phone (about 50x15 pixels), so keep big clear shapes: the pale lit collar, the dark notch and the wet band.

Background: fully transparent (real alpha). If you can't, use a flat pure magenta #FF00FF background.

Not an icon, not a 3D render, not a separate block: part of an ancient floor painted by a concept artist. No text, no characters, no red or crimson, no glow.
```

**Reject if:**
- you can see into the hole from above;
- it's a separate block;
- leaves stick out;
- it doesn't read as a dark notch in a pale collar at small size.

---

### ~~2C · Arrow~~ ✅ painted (2026-10-16)

**Save as:** `Art_Source/Traps_ChatGPT/PILOT-02C_arrow_v1.png`, landscape 3:2, transparent (or flat #FF00FF).
**Attach:** R1 and your accepted 2A launcher.
In game it is about 70 × 8 px on a phone. The code adds the crimson danger marks to the head and fletching (D-108), so the
painting has no red.

```text
I'm making a premium, hand-painted 2D side-view platformer (a paid game: the art must be rich, crafted and painterly, never cheap). The first image is my painting style. The second image is the arrow trap this arrow is fired from: match its materials, its aged bronze and dark iron, and its warm golden light.

Paint ONE single heavy trap ARROW, perfectly horizontal, pointing RIGHT, seen in strict side view (orthographic: no perspective, no tilt, no foreshortening). It fills the image's width, centred vertically, and is very long and thin: about 14 times longer than it is tall, including the fletching.

The design, from right to left:
- HEAD: a broad, leaf-shaped forged iron arrowhead with a strong central ridge and two slightly flared barbs at its back, hand-hammered with visible facets. The metal is very dark (near-black iron), with a thin bright warm edge where the light grazes the cutting edges, and a little dull brown rust in the hollows.
- A tight binding of dark leather cord where the head meets the shaft.
- SHAFT: a straight, dark, oiled hardwood shaft with a subtle wood grain and a few nicks, the same thickness all the way.
- FLETCHING: three stiff, dark brown-grey feathers (not red, not bright), slightly ragged and worn, bound with cord, with a notched nock at the very end.

Light from the upper left: a thin warm highlight along the top edge of the whole arrow, the underside in warm shadow, so it reads as a solid round object. The silhouette must be crisp and clean.

In the game it's only about 70 pixels long and 8 pixels tall on a phone, so the big shapes must read instantly: the broad dark head, the long shaft line and the fan of the fletching. No fine noise on the outline.

Background: fully transparent (real alpha, not a checkerboard). If you can't, use a flat pure magenta #FF00FF background.

Not a vector icon, not a cartoon, not a clean 3D render, and not shiny silver metal: a real, heavy, old weapon painted by a concept artist. No text, no characters, no motion lines, no glow, no red or crimson anywhere.
```

**Reject if:**
- it's tilted or in perspective;
- the head is silver or chrome;
- the fletching is red or bright;
- it's stubby;
- it's vector-like;
- there are motion lines or a glow.

### ~~2D · Spear (lance)~~ ✅ painted (2026-10-16)

**Save as:** `Art_Source/Traps_ChatGPT/PILOT-02D_spear_v1.png`, landscape 3:2, transparent (or flat #FF00FF).
**Attach:** R1 and your accepted 2C arrow.
In game it is about 120 × 12 px. The shaft is repeated along its length (a spear stuck in a wall is a step), so it must be even.

```text
Same game, same painting style and materials as the arrow I attached: premium hand-painted art, warm golden light, near-black forged iron, dark oiled wood.

Paint ONE single heavy temple-trap SPEAR, perfectly horizontal, pointing RIGHT, in strict side view (orthographic: no perspective, no tilt). It fills the image's width, centred vertically, about 12 times longer than it is tall. It is much heavier and thicker than an arrow: a weapon that, when it sticks in a wall, is strong enough to stand on.

The design, from right to left:
- BLADE: a long, broad, heavy spearhead of forged near-black iron, a diamond-section blade with a strong central ridge, faceted by hammer blows, with a thin bright warm highlight along its cutting edges and dull brown rust in the hollows.
- SOCKET: a thick iron collar where the blade meets the shaft, with two square rivets and a wrap of dark leather strips just behind it.
- SHAFT: a long, thick, straight shaft of dark oiled hardwood, the SAME thickness and the SAME look along its whole length (it must be possible to repeat a section of it seamlessly): subtle grain, a few small nicks, no knots or marks that stand out.
- BUTT: a short iron cap at the left end.
No fletching, no ribbons, no decorations.

Light from the upper left: a warm highlight along the top edge of the shaft and blade, the underside in warm shadow, so it reads as solid and round.

In the game it's about 120 pixels long and 12 pixels tall on a phone, so keep the big shapes clean and readable: the broad dark blade, the collar and the long even shaft.

Background: fully transparent (real alpha, not a checkerboard). If you can't, use a flat pure magenta #FF00FF background.

Not a vector icon, not a cartoon, not a clean 3D render, not shiny silver: a real, heavy, old weapon painted by a concept artist. No text, no characters, no motion lines, no glow, no red or crimson anywhere.
```

**Reject if:**
- it's tilted;
- the shaft changes thickness or has a standout mark;
- the blade is silver;
- there are ribbons;
- it's as thin as an arrow.

---

## ~~3 · Sky Isles · sky plate~~ ✅ painted (2026-10-16)

**Save as:** `Art_Source/Environment/SkyIsles/B-01_sky_v1.png`, landscape 3:2, opaque.
**Attach:** R1, R2.

```text
I'm making a premium hand-painted 2D side-view platformer for phones. From the attached images take ONLY the painting technique: soft painterly brushwork, layered atmosphere, luminous light. Do NOT copy their golden-hour palette or their ruins. This is a different place: HIGH ABOVE THE CLOUDS AT DUSK, with nothing below.

Paint the full background sky for this level, filling the whole image edge to edge:
- the bottom quarter is an endless flat sea of soft clouds far below, lit rose and peach on top, violet in the folds;
- a pale, low sun just above the cloud sea, slightly left of centre, with a soft halo that takes the sky's colour (no hard disc outline);
- the sky going from a warm rose-peach band at the horizon up through dusty lilac to deep violet and blue-purple at the top;
- a few long thin wind-streaked clouds high up, lit pink on their undersides;
- tiny faint stars just appearing in the darkest top corner.

No land, no islands, no buildings, no birds, no characters, no text. Strict side view, no perspective tricks. Smooth gradients with painterly cloud texture, nothing busy in the middle band (gameplay sits on top of it). No red or crimson, no saturated cyan or teal.
```

**Reject if:**
- there is land, an island or a building;
- the light is golden orange like the references rather than rose and violet;
- the middle band is busy;
- the sun is a hard white disc;
- it looks like a photo rather than a painting.

---

## ~~4 · Sky Isles · stone fill (tileable)~~ ✅ painted (2026-10-16)

**Save as:** `Art_Source/Environment/SkyIsles/B-10_fill_v1.png`, square 1:1, opaque, seamless on all four sides.
**Attach:** R1, R3.

```text
Same game. The second image is the stone texture of another level, for reference ONLY in scale and level of detail (block size, painterly finish, how busy it is). Do not copy its colour or its sandstone.

Paint a SEAMLESS TILEABLE square texture of the stone for a level of floating islands high in the sky at dusk, seen straight on (flat, front view, no perspective, no lighting direction baked in strongly):
- pale weathered limestone, cool light grey with a faint rose-cream tint;
- large irregular stacked blocks of different sizes, about the same block size as the reference, their edges rounded and eroded by wind, small pits and holes;
- faint horizontal strata lines through the blocks;
- cool violet-grey shadow in the joints, very soft;
- a few hairline cracks; a little pale lichen; NO moss, NO plants, NO bright spots;
- LOW CONTRAST overall: this fills every wall and floor and must sit quietly behind the cat and the traps.

It must tile perfectly on all four sides: no visible seam, and no single distinctive feature (a big crack, a dark hole) that would show as a repeating pattern. No text, no border, no red, no cyan.
```

**Reject if:**
- it isn't seamless (I'll test the seam);
- it's warm sandstone rather than cool pale limestone;
- it's high-contrast or busy;
- there is moss or there are plants;
- one feature stands out and would repeat visibly.

---

## ~~5 · Sky Isles · hero: a moored floating island~~ ✅ painted (2026-10-16)

**Save as:** `Art_Source/Environment/SkyIsles/B-04_hero_chained_isle_v1.png`, portrait 2:3, transparent.
**Attach:** R1, R2, and your B-01 sky from item 3 (so the light matches).

```text
Same game and the same dusk sky as the third image I attached (match its light and palette exactly: rose and lilac light from a low sun on the left, violet shadows). From the first two images take ONLY the painting technique, not their golden ruins.

Paint ONE large floating island at middle distance on a fully transparent background, seen exactly from the side:
- an upside-down cone of pale layered limestone, its top flat and grassy with wind-bent grass leaning right;
- on top, a small round wind shrine with a weathered dome and two tall narrow arched openings (you can see the sky through them; leave them transparent);
- two heavy iron chains running from the island's sides up and out of the top of the image, as if it's moored to something above;
- long roots and a few broken chains hanging under the cone; a thin trickle of water falling from its tip and fading into mist;
- soft aerial haze: a little lighter and less contrasted than something close up, but still clearly readable;
- rim light: the left edges lit rose-gold by the low sun.

Strong, simple silhouette that reads instantly even as a small black shape. No other islands, no ground, no characters, no birds, no text, no border, no red or crimson, no cyan.
```

**Reject if:**
- it looks golden or orange rather than rose and violet;
- there are several islands;
- the openings or background aren't transparent;
- the silhouette is cluttered;
- it has perspective (seen from above or below).

---

## Batch 2: high-impact assets (2026-10-16)

The same routine: one object per image, flat elevation, in-game size and light stated. Files and attachments are in the
table; the full prompts follow it.

| # | File | Format | Attach | Why |
|---|---|---|---|---|
| 1R | `Art_Source/Traps_ChatGPT/PILOT-01R_spike_row_v1.png` | 3:2 | R1, R3, 2A | replaces pilot 1: spikes set into a socket course, irregular, seamless |
| 6 | `Art_Source/Environment/GoldenRuins/A-11_top_lip_v1.png` | 3:2 | R1, R3 | the walkable top (Stop 2's option 2): an uneven painted lip, no collider change |
| 7 | `Art_Source/Traps_ChatGPT/PILOT-07_slam_v2.png` | 1:1 | R1, R3 | slams out of the ceiling (and lifts, flipped); no chains |
| 8 | `Art_Source/Traps_ChatGPT/PILOT-08_storm_cloud_v1.png` | 3:2 | R1 (+ an L011/L018 capture) | the chasing cloud |
| 9 | `Art_Source/Traps_ChatGPT/PILOT-09_inverter_orb_v1.png` | 1:1, black or transparent | R1 | the inverter orb |
| 10 | `Art_Source/Doors/PILOT-10_exit_door_v1.png` | 2:3 | R1, R3 | the exit door |

### ~~1R · Spike row set in a stone socket~~ ✅ painted (2026-10-16)

```text
I'm making a premium, hand-painted 2D side-view platformer (a paid game: the art must be rich and crafted). The first image is my painting style, the second my level's sandstone, the third an arrow trap from the same temple: match its materials and light.

Paint ONE row of temple trap spikes set into the floor, as a STRICT SIDE ELEVATION: exactly level with the floor, no perspective, no top surface visible. It fills the image's width and is about 5 times wider than tall.

- At the bottom, a low strip of the same sandstone (a socket course, about a third of the height), worn and chipped, with dark square sockets along its top that the spikes rise from; dark grime and old stains around each socket.
- Rising from it, 7 forged near-black iron spikes, irregularly spaced, all DIFFERENT: tall thin square-section ones, a broad flat triangular blade, one with a barb, one slightly bent, one blunted and chipped; heights varying between 60% and 100% of the tallest. Never an even picket fence.
- Hammered facets, a thin bright warm highlight along one edge of each spike (light from the upper left), dull brown rust only in the hollows.
- The strip and the spikes are seamless left to right: the stone pattern and the spike rhythm can repeat without a visible seam.

In the game each spike is about 40-50 pixels tall on a phone: bold clean silhouettes, strong light edge, no noisy outline.

Background: fully transparent (real alpha), or flat pure magenta #FF00FF if you can't. Not a vector icon, not a cartoon, not a 3D render. No text, no characters, no red or crimson anywhere, no glow.
```

### ~~6 · Golden Ruins top lip~~ ✅ painted (2026-10-16)

```text
Same premium hand-painted game, same style and the exact sandstone of the second image.

Paint the WALKABLE TOP EDGE of a temple platform as a long horizontal strip in STRICT SIDE ELEVATION (no perspective, no top surface visible): it fills the image's width and sits in the middle third of its height.

- The top line is where a cat walks: overall level, but NEVER ruler-straight: worn, slightly uneven capstones of different lengths, some a little higher, some a little lower (very small differences), with rounded worn edges and small chips at their joints.
- Along the top, irregular clumps of low moss, short grass tufts and a few tiny pale flowers growing in the joints, in clusters with bare stone between them (not an even fringe).
- Under the capstones, the first course of the wall: the sandstone blocks of the reference, lit warm on their upper faces by low golden light from the upper left, falling into warm shadow below, then fading out to transparent at the bottom.
- Above the grass and below the stone: fully transparent (real alpha).

It must tile seamlessly left to right, with no single standout feature that would visibly repeat. Seen on a phone the strip is about 25 pixels tall, so the uneven skyline of capstones and tufts must read clearly. No text, no characters, no red or crimson, no glow.
```

### ~~7 · Slam block (revised 2026-10-16: no chains)~~ ✅ painted (2026-10-16)

**Why it was revised:** nothing in the levels hangs on chains. Slams (D-140) are part of the ceiling slab and drop out of
it; lifts and pistons rise out of the floor. The developer also wants traps to surprise, so the block wears the ceiling's own
stone, and only its iron striking face along the bottom tells (a thin dark line at rest).

**Save as:** `Art_Source/Traps_ChatGPT/PILOT-07_slam_v2.png`, square 1:1, transparent (or flat #FF00FF).
**Attach:** R1, R3.

```text
I'm making a premium, hand-painted 2D side-view platformer (a paid game: the art must be rich and crafted). The first image is my painting style. The second is the exact sandstone of my level's walls and ceilings.

Paint ONE heavy SLAM BLOCK: a section of a temple CEILING that is really a trap and drops down to crush whatever walks below. FLAT FRONT ELEVATION: no perspective, no top or side faces visible. It fills about 75% of the image, a little wider than tall.

- The block is made of the SAME sandstone masonry as the second image, the same blocks, colour and weathering, so that set into the ceiling it looks exactly like the rest of the ceiling.
- Its TOP edge is cut perfectly straight and clean (that's where it sits flush in the ceiling).
- Its two SIDE edges are straight but worn smooth and darkened, polished by centuries of sliding up and down in their slot, with a few long scrape marks.
- Its BOTTOM edge is a heavy striking face: a thick plate of dark forged iron bolted along the underside with a row of big square rivet heads, dented and scarred from impacts, with dark stains and a little chipped stone just above it.
- No chains, no rings, no hooks, no flowers, no leaves, no moss on the faces.

Light from the upper left, warm brown shadows; the iron plate near-black with a thin warm highlight along its lower edge. On a phone it's about 80 pixels wide: the sandstone must match the ceiling exactly, and the dark iron striking face must read clearly along the bottom.

Background: fully transparent (real alpha), or flat pure magenta #FF00FF. Not an icon, not a cartoon, not a 3D render. No text, no characters, no red or crimson, no glow.
```

**Reject if:**
- there are chains, rings, flowers or leaves;
- the stone isn't R3's;
- the top edge isn't straight;
- the iron plate is missing or shiny;
- there's perspective.

### ~~8 · Storm cloud~~ ✅ painted (2026-10-16)

```text
Same premium hand-painted game and painting style.

Paint ONE heavy STORM CLOUD that hunts the player, in strict side view, about 2.5 times wider than tall, filling the image's width.

- A dense, menacing thundercloud: billowing rounded tops lit warm cream and pale gold from above, a dark, heavy, churning belly in deep warm charcoal and bruised violet-grey.
- Its underside is ragged and dragging: torn wisps and a short curtain of rain streaks falling from its belly.
- Inside the belly, faint pale cream light flickering in the folds, as if charged and about to strike (subtle, no lightning bolt).
- It has weight and volume: clear big masses, not flat fluff.

On a phone it's about 160 pixels wide: the dark belly against the lit top must read instantly against bright and dark skies alike.

Background: fully transparent (real alpha), or flat pure magenta #FF00FF. Not a cartoon, not a vector cloud, no face. No text, no characters, no red or crimson, no saturated cyan or blue, no lightning bolt.
```

### ~~9 · Inverter orb~~ ✅ painted (2026-10-16)

```text
Same premium hand-painted game and painting style.

Paint ONE magical INVERTER ORB that swaps the player's left and right, floating, seen straight on, centred, filling about 60% of the image.

- A sphere of glassy, slightly translucent amber-gold light, with two interlocking spiral currents swirling inside it in opposite directions (the idea of left and right swapping), brighter where they cross.
- Around it, a thin ring of ancient carved bronze with small worn runes, slightly tilted, floating with the orb, its edges catching the orb's light.
- A soft warm glow around the whole, fading smoothly to transparent (not a hard halo disc).

On a phone it's about 50 pixels across: the bright core, the two spirals and the bronze ring must read at once.

Background: fully transparent (real alpha), or flat pure black #000000. Not a cartoon, not a vector icon, not an emoji. No text, no characters, no red or crimson, no cyan or blue.
```

### ~~10 · Exit door~~ ✅ painted (2026-10-16)

```text
Same premium hand-painted game, same style and sandstone.

Paint the level's EXIT DOOR, a small ancient temple gateway, as a FLAT FRONT ELEVATION (no perspective), standing on a flat base line at the bottom, filling about 80% of the height.

- A narrow arched doorway of carved sandstone: two pillars with worn carved capitals and a rounded arch with a keystone, ivy trailing from one side only.
- Inside the arch, a warm golden light pouring out from beyond, as if the way out is a sunlit place: bright pale gold at the centre fading to warm amber at the arch's edges, with soft light rays spilling outward and a few floating motes.
- The stone around the opening is lit by that inner light (warm rim on the inner edges), the outer faces in warm shadow.

On a phone it's about 80 pixels tall: the glowing arch must be the brightest, most inviting shape in the level.

Background: fully transparent (real alpha), or flat pure magenta #FF00FF. Not a cartoon, not a 3D render, not a fantasy portal swirl. No text, no characters, no red or crimson, no cyan.
```

---

## Batch 3: the remaining traps and objects (2026-10-16)

| # | File | Format | Attach |
|---|---|---|---|
| 11 | `Art_Source/Traps_ChatGPT/PILOT-11_hinge_floor_v1.png` | 3:2 | R1, R3, 1R |
| 12 | `Art_Source/Environment/GoldenRuins/PILOT-12_vine_v1.png` | 2:3 | R1 |
| 13 | `Art_Source/Traps_ChatGPT/PILOT-13_flip_orb_v1.png` | 1:1 | R1, 9 |
| 14 | `Art_Source/Environment/GoldenRuins/ENV-31_v1.png` | 2:3 | R1, R3 |
| 15 | `Art_Source/Traps_ChatGPT/PILOT-15_crumble_v1.png` | 3:2 | R1, R3 |

### ~~11 · Hinge floor (stands up and pushes, spikes facing the cat)~~ ✅ painted (2026-10-16)

```text
I'm making a premium, hand-painted 2D side-view platformer (a paid game: the art must be rich and crafted). The first image is my painting style, the second my level's sandstone, the third the iron spikes of the same temple: match them exactly.

Paint ONE HINGED FLOOR SLAB trap, lying flat, as a FLAT FRONT ELEVATION (no perspective, no top surface visible). In the game it lies in the floor looking like normal floor, then swings up on its hinge and slides toward the player with its spikes facing them.

- A long slab about 5 times wider than tall. Its TOP edge is the floor's normal walking edge: the same sandstone with a slightly uneven worn top, a few small tufts of moss (it must look like ordinary floor).
- Its body is a thick slab of the same sandstone, bound at both ends with dark riveted iron straps.
- At its LEFT end, a massive iron HINGE: two thick round iron knuckles around a heavy pin, bolted into the slab, dark and worn, a little rust.
- Along its BOTTOM face, a row of short, heavy, forged near-black iron spikes pointing DOWN, irregular in shape and spacing like the spikes in the third image (these face the player when the slab stands up).

Light from the upper left, warm brown shadows, a thin warm highlight on the iron edges. On a phone it's about 120 pixels wide: the floor-like top, the iron hinge and the row of spikes underneath must all read.

Background: fully transparent (real alpha), or flat pure magenta #FF00FF. Not an icon, not a cartoon, not a 3D render. No chains, no text, no characters, no red or crimson, no glow.
```

### ~~12 · Climbable vine~~ ⏭ skipped (the developer, 2026-10-16: today's vines are fine as they are)

```text
Same premium hand-painted game and painting style.

Paint ONE thick CLIMBABLE JUNGLE VINE as a vertical sprite sheet on a transparent background, in strict side view, four separate pieces stacked with clear gaps between them:
1. TOP ANCHOR: the vine's top, knotted twice around a short broken stone bracket stub, its fibres gripping the stone.
2. MIDDLE: a long straight section of the vine that can repeat seamlessly top to bottom: two or three thick woody strands twisted around each other, with a few leaves along it and small knots and nodes every so often that read as hand and foot holds (no single standout feature).
3. BOTTOM TIP: the vine thinning out into a frayed end with a few hanging leaves.
4. SNAPPED ENDS: two short broken ends of the same vine, fibres torn and splayed, as it looks right after it snaps.

The vine is about one third the width of the cat (thick enough to climb), living green-brown with warm highlights from the upper left, darker in the twists. On a phone it's about 10 pixels wide: the twisted strands and the holds must still read.

Background: fully transparent (real alpha), or flat pure magenta #FF00FF. Not a cartoon, not a vector. No text, no characters, no red or crimson, no flowers.
```

### ~~13 · Gravity flip orb~~ ✅ painted (2026-10-16)

```text
Same premium hand-painted game and painting style. The second image is the game's INVERTER orb (it swaps left and right): this new orb must clearly belong to the same family but be instantly distinguishable from it.

Paint ONE GRAVITY FLIP ORB that turns gravity upside down, floating, seen straight on, centred, filling about 60% of the image.

- A sphere of soft pale moonlight-white light with a faint lilac-violet tint (NOT gold, so it can never be confused with the gold inverter).
- Inside it, two arrow-like light shapes, one pointing up and one pointing down, gently curving past each other: the idea of up and down swapping.
- Around it, a thin ring of ancient carved pale stone with small worn runes, standing VERTICALLY (unlike the inverter's tilted bronze ring).
- A soft glow fading smoothly to transparent (not a hard disc).

On a phone it's about 50 pixels across: the pale core, the up/down shapes and the vertical ring must read at once.

Background: fully transparent (real alpha), or flat pure black #000000. Not a cartoon, not a vector icon, not an emoji. No text, no characters, no red or crimson, no saturated cyan or blue, no gold.
```

### ~~14 · Grip-wall moss carpet~~ ✅ painted (2026-10-16)

```text
Same premium hand-painted game, same style and sandstone.

Paint a SEAMLESS VERTICAL strip of thick living MOSS growing on the side face of a sandstone wall, seen straight on (flat front elevation), filling the image's height, about one third of its width.

- A dense cushion of bright, healthy green moss, clearly brighter and greener than the stone, in soft rounded clumps, with tiny pale seed heads and small clinging roots.
- Its left and right edges are ragged and irregular, fading to transparent (so it sits on any wall).
- A little of the sandstone shows through in a few small gaps.
- It must repeat top to bottom seamlessly, with no clump that would visibly repeat.

Light from the upper left. On a phone it's about 30 pixels wide: it must read instantly as "soft moss you can grab", against warm sandstone.

Background: fully transparent (real alpha), or flat pure magenta #FF00FF. No flowers, no vines, no leaves sticking out, no text, no characters, no red or crimson.
```

### ~~15 · Crumbling floor: cracks and broken edge~~ ✅ painted (2026-10-16)

```text
Same premium hand-painted game, same style and sandstone.

Paint a sprite sheet of FOUR separate overlay pieces on a transparent background, spaced well apart, in flat front view. They are painted ON TOP of existing sandstone in the game, so paint only the cracks and breakage, not the stone around them:
1. a small fresh crack: a few thin dark hairlines branching from one point, with a little pale dust along them;
2. a spreading crack: longer jagged dark fissures across a block, slightly opened, edges chipped pale;
3. a shattering crack: a web of deep cracks with small pieces about to fall out, dark gaps between them;
4. a long horizontal CRUMBLING EDGE strip, seamless left to right: the broken, ragged top edge of a floor slab giving way, with small loose stones and grit.

Cracks are dark warm brown-black (never pure black) with thin pale lit edges where the stone has split (light from the upper left). On a phone a crack is only 20-60 pixels long, so keep the main fissures bold and readable.

Background: fully transparent (real alpha), or flat pure magenta #FF00FF. No stone fill around the cracks, no text, no characters, no red or crimson.
```

---

## After you save them

Tell Claude Code "pilot images are in". It will:
1. check transparency, seams, colour and the danger colour;
2. process them through the kit;
3. put the spikes and launcher into L002 and the Trap Lab, and build a Sky Isles test look on L005;
4. render phone-scale captures beside today's art.

You then decide whether the quality is good enough for the full set.
