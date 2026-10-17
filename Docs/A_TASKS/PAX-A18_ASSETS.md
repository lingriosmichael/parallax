 # PAX-A18 · Assets to paint (ChatGPT) and the AutoSprite plan (Stop 1)

**Status:** for the developer's approval at Stop 1 (2026-10-15). Nothing below has been generated; no credits spent.

## How to work (the PAX-A16 §5 routine, per biome)

1. One new ChatGPT chat **per biome** (and one for the traps). Attach `Docs/Art/Reference/styleframe_A_v1.png`,
   `DesignImages/14_environment-breakdown.png` and `DesignImages/15_premium-mobile-ui.png`.
2. Paste the biome's **style header** (below) once at the start of the chat, then each item's prompt, then the
   **suffix**. For the next item in the same chat add: "Match the style and light of the image you just made exactly."
3. ChatGPT gives 1536 × 1024 (3:2), 1024 × 1536 (2:3) or 1024 × 1024 (1:1). Use the aspect given.
   "Transparent" = a PNG with real alpha (regenerate if it paints a checkerboard).
4. Save to the folder and exact name given, `_v1` (a redo is `_v2`). Folders are named for the biome, so they never mix
   with Reality B's `Art_Source/Environment/ObsidianVoid/`.
5. Reject and regenerate (don't fix by hand) if: perspective or a vanishing point, text, a border, a character or animal,
   any red/crimson (red belongs to hazards, D-108), saturated cyan or teal (Reality B's), or the place looks like the
   golden temple ruins of the references (biomes B–E must be **different places**, not recolours).

`env_kit.py` crops, blends seams, sizes and pre-hazes everything; the code cuts the bumpy outlines (W3), so the painted
stone only needs to look like the right rock.

### Shared opening (all style headers start with this)

> Painterly but clean 2D game art for a premium mobile puzzle platformer, strict side view, orthographic, no perspective.
> From the attached references take ONLY the painting technique: brushwork, edge quality, level of detail, value
> structure and phone-scale readability. Do NOT copy their place, buildings, palette or light. This is a different kind
> of place:

and all of them end with:

> Strong, simple silhouettes that are recognisable in black and white, restrained detail, readable at phone scale. No red
> or crimson anywhere. No saturated cyan or teal. No text, no characters, no animals, no UI, no border, no signature.

### Suffix (append to every prompt; `<PLACE>` from the biome)

> Same painterly style as the references, in the `<PLACE>` look described above. Strict side view, orthographic, no
> perspective vanishing point, no characters, no text, no watermark.

---

## A · Golden Ruins (`Art_Source/Environment/GoldenRuins/`) — keeps today's sky, far and mid art

**Style header:** the shared opening, then: "open-air ancient temple ruins in low warm golden sun: sandstone, ivy, spires,
aqueducts and colonnades, warm brown shadows, gold rim light." + the shared ending. `<PLACE>` = golden temple ruins.

**A-12 · Stone ends and corners** (`A-12_ends_corners_v1.png`, square 1:1, transparent, a sheet; the ends of every
floor, outer corners)
```text
A sprite sheet on a transparent background, four well-separated pieces of the same warm weathered sandstone masonry with a mossy top course: a left block end broken off irregularly (big chunks missing, a jagged profile, small stones about to fall), a right block end broken differently, an outer top corner that is rounded and chipped, and a cracked corner stone with a root curling over it. Each piece shows its top surface as a near-level walking edge with small grass tufts, and its outer side as broken, uneven stone.
```

**A-13 · Thin slab** (`A-13_thin_slab_v1.png`, landscape 3:2, transparent, not tiled; 1–3 u ledges and steps)
```text
One long thin floating stone slab seen from the side, filling the image width: warm sandstone, irregular, thicker at the right end and tapering at the left, a near-level mossy top walking edge, a cracked and broken underside with small chunks missing, a few short roots and ivy tendrils hanging from the bottom. Transparent around it.
```

**A-14 · Underside** (`A-14_underside_v1.png`, landscape 3:2, transparent below, tiles sideways; the bottom of every
platform and ceiling)
```text
The underside of a heavy sandstone platform seen straight from the side: the masonry in the upper third, its bottom edge broken and uneven, with hanging stones, short root tangles and short ivy fringes no longer than a quarter of the image. Below that, transparent. Seamlessly tileable left to right, with the left and right 15% quiet.
```

**A-20 · Ground clumps** (`A-20_ground_clumps_v1.png`, landscape 3:2, transparent, a sheet; placed one by one)
```text
A sprite sheet of four well-separated ground-hugging dressing clumps on a transparent background, each different in shape and size: a broad low fern clump, a tall narrow fern with two arching fronds, a fallen carved sandstone block overgrown with moss, a tangle of dry roots and small yellow flowers. Each sits on a flat base line.
```

**A-21 · Hanging pieces** (`A-21_hanging_v1.png`, landscape 3:2, transparent, a sheet)
```text
A sprite sheet of four well-separated hanging pieces on a transparent background, each hanging from a straight top line: a short ivy fringe, a root tangle with a few hanging stones, a short torn faded ochre cloth strip, a small cluster of dangling moss. None is longer than twice its width.
```

Signatures (one per level, mid distance, placed once): see the **Signatures** section.

---

## B · Sky Isles (`Art_Source/Environment/SkyIsles/`)

**Style header:** the shared opening, then: "high above the clouds, with nothing below: floating islands of pale weathered
limestone carrying small wind-worn shrines, long roots and broken chains hanging under every island, wind-bent grass, a
cloud sea far below the walkways, light from a dusk sky going from rose at the horizon to violet above. Cool violet
shadows, rose and pale gold highlights." + the shared ending. `<PLACE>` = Sky Isles dusk.

**B-01 · Sky plate** (`B-01_sky_v1.png`, landscape 3:2, opaque)
```text
A full sky plate for a side-view game background at dusk high above the clouds: a pale low sun just above a flat endless cloud sea at the bottom quarter, a rose and peach horizon band, the upper sky deepening to violet and dusky blue-purple, a few thin long wind-streaked clouds. No land, no buildings. Fills the image edge to edge.
```

**B-02 · Far isles band** (`B-02_far_isles_v1.png`, landscape 3:2, transparent above and below the isles, tiles sideways)
```text
A long horizontal band of distant floating islands over a cloud sea: small and large inverted-cone rock islands with flat grassy tops, tiny shrines and wind towers on them, long roots and chains trailing below each, very soft pale violet aerial haze, low contrast, no hard outlines. The islands sit in the middle of the image, the cloud sea fills the bottom fifth, everything above is transparent. Seamlessly tileable left to right, with the left and right 15% quiet.
```

**B-03 · Mid cloud sea and isles** (`B-03_mid_band_v1.png`, landscape 3:2, transparent above, tiles sideways)
```text
A mid-distance band: a billowing cloud sea filling the lower third, lit rose on top and violet in the folds, with three medium floating limestone islands rising out of it at different heights, each with a broken stone stair or arch and hanging roots. Everything above the islands is transparent. Seamlessly tileable left to right, with the left and right 15% quiet.
```

**B-04 · Hero: chained island** (`B-04_hero_chained_isle_v1.png`, portrait 2:3, transparent)
```text
One large floating island at mid distance: an inverted cone of pale layered limestone with a flat grassy top, a small round wind shrine with a dome on it, two heavy chains running from its sides up out of the frame as if it is moored to something above, long roots and a thin trickle of water falling from its tip into nothing. Transparent around it.
```

**B-05 · Hero: wind tower** (`B-05_hero_wind_tower_v1.png`, portrait 2:3, transparent)
```text
A tall slender wind tower standing on a small floating rock: pale limestone with tall pointed windows, ochre cloth sails wrapped around a wooden frame at its top, prayer flags in faded ochre and cream streaming sideways in the wind, roots hanging from the rock below. Transparent around it.
```

**B-06 · Hero: broken sky bridge** (`B-06_hero_sky_bridge_v1.png`, landscape 3:2, transparent)
```text
A long broken stone bridge between two small floating islands at mid distance: the span snapped in the middle, its two halves drooping, loose stones hanging on rope from the broken ends, wind-bent grass on the islands, a few clouds drifting under the bridge. Transparent around it.
```

**B-10 · Stone fill** (`B-10_fill_v1.png`, square 1:1, opaque, tiles both ways; the inside of every block)
```text
A seamless tileable texture of pale weathered limestone seen straight on: large irregular layered blocks, soft wind-eroded edges and small holes, faint horizontal strata, cool violet-grey shadows in the joints and pale rose highlights, very low contrast so it never competes with the game. Tiles perfectly on all four sides.
```

**B-11 · Top lip** (`B-11_top_lip_v1.png`, landscape 3:2, transparent above the grass and below the band, tiles sideways)
```text
The walkable top edge of a pale limestone platform seen from the side, as a horizontal band across the middle of the image: a near-level walking line, short wind-bent grass and tiny pale flowers leaning to the right above it, a lit rose-gold top course of stone below it, then the stone fading out. Above the grass and below the band: transparent. Seamlessly tileable left to right.
```

**B-12 · Ends and corners** (`B-12_ends_corners_v1.png`, square 1:1, transparent, a sheet)
```text
A sprite sheet on a transparent background, four well-separated pieces of the same pale weathered limestone with a grassy top: a left block end eroded by wind into a rounded, holed, irregular profile, a right end broken off with a jagged edge and a root hanging out of it, a rounded outer top corner, and a corner stone split by a crack with grass growing in it. Each piece shows a near-level walking edge on top.
```

**B-13 · Thin slab** (`B-13_thin_slab_v1.png`, landscape 3:2, transparent, not tiled)
```text
One long thin floating slab of pale limestone seen from the side, filling the image width: thicker at one end, tapering and slightly eroded at the other, a near-level grassy top walking edge, an underside that hangs down in a shallow uneven point like a small floating island, long thin roots trailing below it. Transparent around it.
```

**B-14 · Underside** (`B-14_underside_v1.png`, landscape 3:2, transparent below, tiles sideways)
```text
The underside of a floating limestone platform seen straight from the side: layered pale rock in the upper third, its bottom edge hanging down in uneven rounded lumps like the bottom of floating islands, long thin roots and a few dangling stones on short ropes below it, no longer than a third of the image. Below that, transparent. Seamlessly tileable left to right, with the left and right 15% quiet.
```

**B-20 · Ground clumps** (`B-20_ground_clumps_v1.png`, landscape 3:2, transparent, a sheet)
```text
A sprite sheet of four well-separated ground dressing clumps on a transparent background: a clump of tall wind-bent grass leaning right, a low cushion of pale flowers, a small cairn of stacked flat stones with an ochre prayer flag on a stick, a broken limestone urn. Each different in size and silhouette, each on a flat base line.
```

**B-21 · Hanging pieces** (`B-21_hanging_v1.png`, landscape 3:2, transparent, a sheet)
```text
A sprite sheet of four well-separated hanging pieces on a transparent background, each from a straight top line: a bundle of long thin roots, a short broken chain with a loose ring, a rope with two small stones tied to it, a short curtain of hanging grass. None longer than twice its width.
```

---

## C · Sunken Grotto (`Art_Source/Environment/SunkenGrotto/`)

**Style header:** the shared opening, then: "inside a vast cavern with a ceiling of rock: stalactites and stalagmites, cave
mouths, an underground lake, shafts of pale cool daylight falling from cracks far above, wet dark basalt that shines where
light hits it, glowing amber and warm green fungi and moss, dripping water. Deep cool blue-grey darkness, pale grey-blue
light shafts, warm amber fungus glow." + the shared ending. `<PLACE>` = Sunken Grotto cavern.

**C-01 · Sky plate (the cavern's far wall)** (`C-01_cavern_back_v1.png`, landscape 3:2, opaque)
```text
The far back of a huge cavern seen from inside, as a full background plate: deep blue-grey darkness, a jagged cave mouth high up on the right with pale daylight pouring in as a broad soft shaft, faint distant rock layers, a pale mist band low across the image, a few tiny amber glows of fungus deep in the dark. No ground in the foreground, no sky except through the cave mouth. Fills the image edge to edge.
```

**C-02 · Far band** (`C-02_far_cavern_v1.png`, landscape 3:2, transparent above, tiles sideways)
```text
A long horizontal band of a distant cavern: tall rock columns where stalactites meet stalagmites, a still underground lake reflecting them along the bottom, faint light shafts slanting down between the columns, low contrast, misty blue-grey, no hard outlines. Everything above the columns' upper ends is transparent. Seamlessly tileable left to right, with the left and right 15% quiet.
```

**C-03 · Mid band** (`C-03_mid_band_v1.png`, landscape 3:2, transparent above, tiles sideways)
```text
A mid-distance band of cavern rock: clusters of stalagmites of different heights rising from a dark wet floor, shelves of basalt with glowing amber mushrooms and moss, small pools reflecting the glow, drips falling. Everything above the rock is transparent. Seamlessly tileable left to right, with the left and right 15% quiet.
```

**C-04 · Hero: stalactite curtain** (`C-04_hero_stalactites_v1.png`, landscape 3:2, transparent below)
```text
A massive mass of rock hanging from the top of the image like a cavern ceiling: dozens of stalactites of many lengths, the longest reaching halfway down, wet and glinting, water dripping from their tips, warm fungus glowing in the cracks between them. Transparent below and between the stalactites.
```

**C-05 · Hero: cave mouth with light shaft** (`C-05_hero_light_shaft_v1.png`, portrait 2:3, transparent)
```text
A tall slab of cavern wall with a ragged opening near its top through which a narrow beam of pale daylight falls diagonally, lighting floating dust motes and a mossy ledge below it, ferns hanging from the opening's lip. Transparent around the rock and beside the beam.
```

**C-06 · Hero: fungus pillar** (`C-06_hero_fungus_pillar_v1.png`, portrait 2:3, transparent)
```text
A tall natural rock pillar in a cavern covered in shelves of glowing amber and warm green bracket fungi and tall thin luminous mushrooms, wet moss streaks, a small pool at its foot reflecting the glow. Transparent around it.
```

**C-10 · Stone fill** (`C-10_fill_v1.png`, square 1:1, opaque, tiles both ways)
```text
A seamless tileable texture of wet dark basalt seen straight on: rough columnar and blocky shapes, damp sheen on the upper edges of each block, thin streaks of dark moss and mineral stains, deep blue-grey with warm brown undertones, low contrast. Tiles perfectly on all four sides.
```

**C-11 · Top lip** (`C-11_top_lip_v1.png`, landscape 3:2, transparent above and below the band, tiles sideways)
```text
The walkable top edge of a wet dark basalt ledge seen from the side, as a horizontal band across the middle of the image: a near-level walking line, a thin cushion of damp moss with a few tiny glowing amber mushrooms above it, a wet glinting top course of rock below it, then the rock fading out. Transparent above the moss and below the band. Seamlessly tileable left to right.
```

**C-12 · Ends and corners** (`C-12_ends_corners_v1.png`, square 1:1, transparent, a sheet)
```text
A sprite sheet on a transparent background, four well-separated pieces of the same wet dark basalt with a mossy top: a left end that breaks off in rough columnar steps, a right end with a jagged dripping edge and a small stalactite under it, a rounded wet outer corner with a mushroom cluster, and a cracked corner block with water running out of the crack. Each shows a near-level walking edge on top.
```

**C-13 · Thin slab** (`C-13_thin_slab_v1.png`, landscape 3:2, transparent, not tiled)
```text
One long thin ledge of dark wet basalt seen from the side, filling the image width: thicker at one end, a near-level mossy top walking edge, a rough underside with three short stalactites of different lengths and water dripping from them. Transparent around it.
```

**C-14 · Underside** (`C-14_underside_v1.png`, landscape 3:2, transparent below, tiles sideways)
```text
The underside of a dark basalt ledge seen straight from the side: wet rock in the upper third, its bottom edge rough and uneven with short stalactites of irregular lengths and drips, a few faint glowing fungus spots, nothing longer than a third of the image. Below that, transparent. Seamlessly tileable left to right, with the left and right 15% quiet.
```

**C-20 · Ground clumps** (`C-20_ground_clumps_v1.png`, landscape 3:2, transparent, a sheet)
```text
A sprite sheet of four well-separated cavern floor clumps on a transparent background: a cluster of tall thin glowing amber mushrooms, a low mound of warm green moss with tiny glowing caps, a small stalagmite group, a broken carved stone with fungus on it. Different sizes and silhouettes, each on a flat base line.
```

**C-21 · Hanging pieces** (`C-21_hanging_v1.png`, landscape 3:2, transparent, a sheet)
```text
A sprite sheet of four well-separated hanging pieces on a transparent background, each from a straight top line: a short stalactite cluster, a curtain of dripping moss, a hanging root with glowing droplets, a single long thin stalactite with a water drop at its tip. None longer than twice its width.
```

---

## D · Jungle Falls (`Art_Source/Environment/JungleFalls/`)

**Style header:** the shared opening, then: "a deep canyon in dense jungle in cool morning haze: giant tree trunks and
buttress roots, sheer mossy cliff walls, tall waterfalls and rising mist, mossy green-grey granite, roots, lianas and broad
leaves. Cool green-grey shadows, pale soft morning light, warm highlights only where the sun touches the leaves." + the
shared ending. `<PLACE>` = Jungle Falls canyon.

**D-01 · Sky plate** (`D-01_sky_v1.png`, landscape 3:2, opaque)
```text
A full background plate of a jungle canyon's sky: a narrow strip of pale morning sky between two dark cliff rims at the top corners, the rest filled with soft layered morning mist, a hazy pale sun glow behind the mist upper left, cool green-grey tones. No foreground. Fills the image edge to edge.
```

**D-02 · Far band** (`D-02_far_canyon_v1.png`, landscape 3:2, transparent above, tiles sideways)
```text
A long horizontal band of a distant jungle canyon: tall sheer cliff walls with several thin waterfalls, the jungle canopy as soft rounded masses along their tops and bases, thick mist between the cliffs, low contrast, pale green-grey, no hard outlines. Everything above the cliff tops is transparent. Seamlessly tileable left to right, with the left and right 15% quiet.
```

**D-03 · Mid band** (`D-03_mid_band_v1.png`, landscape 3:2, transparent above, tiles sideways)
```text
A mid-distance band of jungle: giant straight tree trunks with buttress roots, lianas looping between them, broad leaves and palms filling the gaps, mist at the ground, a stream catching the light. Everything above the trunks' upper canopy is transparent. Seamlessly tileable left to right, with the left and right 15% quiet.
```

**D-04 · Hero: great waterfall** (`D-04_hero_waterfall_v1.png`, portrait 2:3, transparent)
```text
A tall waterfall pouring over a mossy granite cliff lip at mid distance, the water white and pale green, mist rising from its foot, ferns and roots growing on the wet cliff on both sides. Transparent around the cliff and water.
```

**D-05 · Hero: giant trunk** (`D-05_hero_giant_trunk_v1.png`, portrait 2:3, transparent)
```text
One colossal jungle tree trunk at mid distance, filling the image height: huge buttress roots gripping a boulder at its base, its bark covered with moss, epiphytes and hanging lianas, a broken rope bridge tied to it on one side. Transparent around it.
```

**D-06 · Hero: rope bridge** (`D-06_hero_rope_bridge_v1.png`, landscape 3:2, transparent)
```text
A long sagging rope and plank bridge across a misty gap at mid distance, a few planks missing, vines wound around its ropes, broad leaves hanging over both ends. Transparent around it.
```

**D-10 · Stone fill** (`D-10_fill_v1.png`, square 1:1, opaque, tiles both ways)
```text
A seamless tileable texture of mossy green-grey granite seen straight on: large rounded blocks, patches of moss in the joints, thin root lines crossing the stone, cool green-grey with soft warm highlights, low contrast. Tiles perfectly on all four sides.
```

**D-11 · Top lip** (`D-11_top_lip_v1.png`, landscape 3:2, transparent above and below the band, tiles sideways)
```text
The walkable top edge of a mossy granite ledge seen from the side, as a horizontal band across the middle of the image: a near-level walking line, a thick moss carpet with small ferns and broad leaf tips above it, a top course of rounded grey-green stone with a root running along it below, then the stone fading out. Transparent above the leaves and below the band. Seamlessly tileable left to right.
```

**D-12 · Ends and corners** (`D-12_ends_corners_v1.png`, square 1:1, transparent, a sheet)
```text
A sprite sheet on a transparent background, four well-separated pieces of the same mossy green-grey granite: a left end gripped by a thick root that curls over its edge, a right end broken into rounded boulders, a rounded mossy outer corner with a fern, and a split corner stone with a liana growing out of the crack. Each shows a near-level walking edge on top.
```

**D-13 · Thin slab** (`D-13_thin_slab_v1.png`, landscape 3:2, transparent, not tiled)
```text
One long thin ledge of mossy granite seen from the side, filling the image width, thicker at one end, a near-level mossy top walking edge, a cracked underside with roots and two short lianas hanging from it, a fern sprouting at the thick end. Transparent around it.
```

**D-14 · Underside** (`D-14_underside_v1.png`, landscape 3:2, transparent below, tiles sideways)
```text
The underside of a mossy granite ledge seen straight from the side: grey-green stone in the upper third, its bottom edge uneven with root mats, hanging moss and dripping water, a few short lianas, nothing longer than a third of the image. Below that, transparent. Seamlessly tileable left to right, with the left and right 15% quiet.
```

**D-20 · Ground clumps** (`D-20_ground_clumps_v1.png`, landscape 3:2, transparent, a sheet)
```text
A sprite sheet of four well-separated jungle floor clumps on a transparent background: a wide clump of broad elephant-ear leaves, a small palm sapling, a mossy boulder with ferns, a coil of thick roots with a few small orange-yellow flowers. Different sizes and silhouettes, each on a flat base line.
```

**D-21 · Hanging pieces** (`D-21_hanging_v1.png`, landscape 3:2, transparent, a sheet)
```text
A sprite sheet of four well-separated hanging pieces on a transparent background, each from a straight top line: a short looped liana, a mat of hanging moss with drips, a broad drooping leaf cluster, a short frayed rope. None longer than twice its width.
```

---

## E · Ember Night (`Art_Source/Environment/EmberNight/`)

**Style header:** the shared opening, then: "ancient ruins at night lit by fire: moonlit towers and broken walls in cool
silver-blue moonlight, columns of smoke, distant bonfires on the horizon, braziers with amber flames, glowing embers in the
air, dark charcoal-grey stone. The cool moon is the key light; every warm light is amber or orange, never red." + the
shared ending. `<PLACE>` = Ember Night.

**E-01 · Sky plate** (`E-01_sky_v1.png`, landscape 3:2, opaque)
```text
A full night sky plate: a large pale full moon upper right with a small cool halo, deep blue-black sky with faint stars, thin slow-moving clouds lit silver at their edges, a warm amber glow low on the horizon from distant fires, a few drifting smoke columns. No ground, no buildings. Fills the image edge to edge.
```

**E-02 · Far band** (`E-02_far_towers_v1.png`, landscape 3:2, transparent above, tiles sideways)
```text
A long horizontal band of distant ruined towers and walls at night: tall broken towers silhouetted against the moonlit sky, several small distant bonfires glowing amber at their feet, smoke rising and leaning in the wind, low contrast, cool blue-grey haze. Everything above the towers is transparent. Seamlessly tileable left to right, with the left and right 15% quiet.
```

**E-03 · Mid band** (`E-03_mid_band_v1.png`, landscape 3:2, transparent above, tiles sideways)
```text
A mid-distance band of night ruins: a broken fortified wall with crenellations and an arched gate, iron braziers burning amber along its top, banners hanging in tatters in charcoal grey, smoke drifting, moonlight on the stone edges. Everything above is transparent. Seamlessly tileable left to right, with the left and right 15% quiet.
```

**E-04 · Hero: brazier tower** (`E-04_hero_brazier_tower_v1.png`, portrait 2:3, transparent)
```text
A tall ruined watchtower at night at mid distance: dark charcoal stone, a great iron fire basket burning amber at its top throwing warm light down its face, embers rising, the moon rim-lighting its other side, ivy blackened by night. Transparent around it.
```

**E-05 · Hero: moon gate** (`E-05_hero_moon_gate_v1.png`, portrait 2:3, transparent)
```text
A huge round stone gate standing alone at mid distance, the moon visible through its opening, carved runes along its ring worn smooth, two small amber lanterns hanging at its sides, broken walls and smoke at its base. The opening is transparent.
```

**E-06 · Hero: smoking ruins** (`E-06_hero_smoking_ruins_v1.png`, landscape 3:2, transparent)
```text
A cluster of collapsed ruined walls at night with a fire still burning low behind them, thick smoke rising and catching the moonlight, sparks and embers in the smoke, scorched stone. Transparent around it.
```

**E-10 · Stone fill** (`E-10_fill_v1.png`, square 1:1, opaque, tiles both ways)
```text
A seamless tileable texture of dark charcoal-grey cut stone seen straight on at night: large worn blocks, soot stains, faint cool blue moonlight on the upper edges and a very faint warm bounce from below, low contrast. Tiles perfectly on all four sides.
```

**E-11 · Top lip** (`E-11_top_lip_v1.png`, landscape 3:2, transparent above and below the band, tiles sideways)
```text
The walkable top edge of a dark stone platform at night, as a horizontal band across the middle of the image: a near-level walking line, dry dark grass and a few small embers resting on it, a top course of stone lit silver by the moon on its edge, then the stone fading out. Transparent above the grass and below the band. Seamlessly tileable left to right.
```

**E-12 · Ends and corners** (`E-12_ends_corners_v1.png`, square 1:1, transparent, a sheet)
```text
A sprite sheet on a transparent background, four well-separated pieces of the same dark charcoal stone at night: a left end broken in a jagged step with soot marks, a right end with a fallen block leaning against it, a chipped outer corner with a small iron ring bolted to it, and a cracked corner stone glowing faintly amber in the crack. Each shows a near-level walking edge on top lit by the moon.
```

**E-13 · Thin slab** (`E-13_thin_slab_v1.png`, landscape 3:2, transparent, not tiled)
```text
One long thin slab of dark charcoal stone at night seen from the side, filling the image width, thicker at one end, a near-level moonlit top walking edge, a broken underside with a rusted iron bracket and a short chain hanging from it. Transparent around it.
```

**E-14 · Underside** (`E-14_underside_v1.png`, landscape 3:2, transparent below, tiles sideways)
```text
The underside of a dark stone platform at night seen straight from the side: charcoal masonry in the upper third, its bottom edge broken and uneven, soot-blackened, a few short hanging chains and dry roots, nothing longer than a third of the image. Below that, transparent. Seamlessly tileable left to right, with the left and right 15% quiet.
```

**E-20 · Ground clumps** (`E-20_ground_clumps_v1.png`, landscape 3:2, transparent, a sheet)
```text
A sprite sheet of four well-separated night ground pieces on a transparent background: a small iron brazier on three legs with an amber flame, a pile of broken charred timber, a clump of dry dark grass, a toppled stone urn with embers spilling out. Different sizes and silhouettes, each on a flat base line.
```

**E-21 · Hanging pieces** (`E-21_hanging_v1.png`, landscape 3:2, transparent, a sheet)
```text
A sprite sheet of four well-separated hanging pieces on a transparent background, each from a straight top line: a short rusted chain, an iron lantern with a small amber flame, a scorched cloth strip in charcoal grey, a tangle of dry blackened roots. None longer than twice its width.
```

---

## Signatures (one per level, mid distance, placed once; save in the biome's folder)

Each uses its biome's chat, style header and suffix. Portrait 2:3 unless noted; transparent around the subject; haze at the
base fading to transparency. L002 keeps its approved aqueduct (no new image).

| ID · file | Level | Prompt |
|---|---|---|
| `A-S01_sun_arch_v1.png` | L001 | A colossal ruined triumphal arch at mid distance, tall weathered sandstone piers draped in ivy, a broken keystone, the opening fully transparent so the sky shows through, gold rim light on its top edges. |
| `A-S03_great_tree_v1.png` | L003 | A huge ancient tree growing out of a ruined sandstone tower at mid distance, its roots splitting the masonry, a broad golden-lit crown, ivy hanging from its branches. |
| `A-S04_colonnade_hall_v1.png` (3:2) | L004 | The inside wall of a grand ruined colonnade hall seen straight on, tall columns with carved capitals, deep window openings between them fully transparent to the sky, ivy and light spilling in. |
| `A-S15_aqueduct_pools_v1.png` (3:2) | L015 | A tiered ruined aqueduct stepping down three levels at mid distance, water spilling from each tier into a still pool below it, ivy on the arches, gold light on the water. |
| `B-S05_banner_isle_v1.png` | L005 | A floating island carrying a tall pale limestone gateway hung with long faded ochre cloth banners streaming sideways in the wind, roots and chains below the island. |
| `B-S07_storm_spires_v1.png` | L007 | Three tall thin limestone spires on a floating rock, each topped with a bronze lightning rod, a dark storm cloud gathering over them lit rose underneath, thin rain falling to the side. |
| `B-S11_moored_citadel_v1.png` | L011 | A small walled citadel on a floating island, moored by four huge chains running to smaller rocks around it, a broken stair spiralling up its outside wall. |
| `B-S18_storm_eye_v1.png` (3:2) | L018 | A ring of towering storm clouds seen from the side with a calm bright eye in the middle, a tiny floating shrine in the eye lit pale gold, lightning flickering inside the cloud walls in pale cream. |
| `C-S08_sunken_temple_v1.png` (3:2) | L008 | A temple facade half-sunk into an underground lake, wide steps descending into the dark water, a light shaft from above falling on its doorway, stalactites above it. |
| `C-S12_pillar_hall_v1.png` (3:2) | L012 | A hall of colossal natural cave pillars, each carved at its base into rough steps, pools between them reflecting amber fungus glow. |
| `C-S16_mirror_lake_v1.png` (3:2) | L016 | A perfectly still cavern lake reflecting the stalactites above it so ceiling and reflection look the same, a single light shaft hitting the water's middle. |
| `C-S19_crystal_cascade_v1.png` | L019 | A tall crack in the cavern wall with a cascade of pale water and a cluster of large pale quartz crystals glinting where the light shaft hits them. |
| `D-S06_twin_falls_v1.png` (3:2) | L006 | Two tall waterfalls side by side pouring off a mossy granite cliff into one misty pool, a giant tree leaning between them. |
| `D-S13_fall_chamber_v1.png` | L013 | A round canyon chamber with a huge waterfall falling into it from a hole in its rim, mist filling its bottom, ferns and lianas on its walls. |
| `D-S14_canopy_v1.png` | L014 | Three colossal tree trunks with a dense canopy above, rope bridges and hanging lianas strung between them at different heights. |
| `D-S17_twin_heads_v1.png` (3:2) | L017 | Two identical giant carved stone heads half-swallowed by jungle facing each other across a stream, roots over their eyes, one of them cracked. |
| `E-S09_ember_field_v1.png` (3:2) | L009 | A wide plain of burning brazier towers at night stretching into the distance, smoke leaning in the wind, embers drifting under the moon. |
| `E-S10_moon_ruins_v1.png` | L010 | A tall broken tower framing a huge pale full moon behind it, its windows faintly amber, smoke drifting across the moon. |
| `E-S20_machine_tower_v1.png` | L020 | A huge stone and iron tower at night with a great wooden water wheel and chains on its side, fires burning in its windows, smoke from its top. |

---

## Trap bodies (`Art_Source/Traps_ChatGPT/`, one chat "Chat T3")

Attach the three references **and the biome's fill (`<X>-10_fill_v1.png`; for A: `Art_Source/Environment/GoldenRuins/A_GAME_Platform_Fill__raw_v1.png`)**
for the fittings sheets, so the carving is in that stone. Style header: the shared opening with "the trap parts of an ancient
temple-trap world; each object must look like a real, heavy, made thing: forged iron, cast bronze, carved stone, rope,
wood" + the shared ending. **Every lethal part (blade, point, mouth) is drawn dark (near-black to dark brown) with a pale
lit edge**; the crimson danger marks are added in code (D-108), so do not paint any red.

| ID · file | Aspect | Prompt |
|---|---|---|
| `TRAP2-01_fittings_A_v1.png` | 3:2, transparent, sheet | Three well-separated pieces carved into warm sandstone (match the attached stone): a launcher carved as a lion's open mouth with a dark square slot deep inside it, seen straight on as part of a wall; a low round floor vent ring with a dark mouth and wet moss at its lip; a long low stone socket strip with a row of dark holes along its top, seamless left to right. |
| `TRAP2-02_fittings_B_v1.png` | 3:2, transparent, sheet | The same three pieces carved into pale weathered limestone: a launcher shaped as a bird's open beak with a dark slot inside, a vent ring with wind-worn edges, a socket strip. |
| `TRAP2-03_fittings_C_v1.png` | 3:2, transparent, sheet | The same three pieces in wet dark basalt: a launcher shaped as an eel's gaping mouth with a dark slot inside, a fumarole vent crusted with minerals and fungus, a socket strip. |
| `TRAP2-04_fittings_D_v1.png` | 3:2, transparent, sheet | The same three pieces in mossy granite: a launcher carved as a serpent's open jaws with a dark slot inside, roots around it; a spring vent ring wrapped with roots; a socket strip of lashed wooden poles set into the stone. |
| `TRAP2-05_fittings_E_v1.png` | 3:2, transparent, sheet | The same three pieces in dark charcoal stone: a launcher as a forged iron grille shaped like a beast's mouth set into the stone, a vent of cracked stone with a faint amber glow inside, a socket strip of riveted iron. |
| `TRAP2-10_blades_iron_v1.png` | 3:2, transparent, sheet | Four well-separated single spikes pointing up, forged black iron, each a different shape and height: a long thin square-section spike, a short broad triangular blade, a slightly bent spike, a barbed spike. Pale steel catching light on one edge, dark outline. Each on a flat base line. |
| `TRAP2-11_blades_bronze_v1.png` | 3:2, transparent, sheet | The same four spike shapes in dark oxidised bronze, almost black-brown, a pale gold glint on one edge, worn and pitted. |
| `TRAP2-12_blades_crystal_v1.png` | 3:2, transparent, sheet | Four well-separated pointed spikes of black obsidian crystal growing up from a base, each a different shape and height, sharp facets, pale grey highlights on the facet edges, almost black inside. |
| `TRAP2-13_blades_wood_v1.png` | 3:2, transparent, sheet | Four well-separated sharpened stakes pointing up, fire-hardened charred wood almost black, lashed with frayed rope near the base, each a different height and lean, pale splintered wood showing only at the cut points. |
| `TRAP2-20_arrows_v1.png` | 3:2, transparent, sheet | Three well-separated weapons pointing right, very long and thin: an arrow with a dark forged iron head, a dark wooden shaft and dark brown feather fletching; a heavy spear with a broad dark iron blade, a thick dark wooden shaft and a leather wrap; the same arrow's shaft snapped short, as stuck in a wall. |
| `TRAP2-21_inverter_v1.png` | 1:1, transparent, sheet | Two well-separated objects: a floating sphere of warm gold light with a slow swirl of two interlocking spirals inside and a soft halo; a small gold ring of carved runic segments with two curved arrows chasing each other inside it. |
| `TRAP2-22_storm_cloud_v1.png` | 3:2, transparent | One heavy storm cloud, 2.5 times wider than tall, puffy top lit pale, a dark heavy churning belly, a flat ragged bottom edge, faint inner light flickering in its folds. |
| `TRAP2-23_mechanism_v1.png` | 3:2, transparent, sheet | Well-separated parts of heavy trap machinery in forged dark iron: a long riveted iron strap band, seamless left to right; a chain of heavy links, seamless top to bottom; a large hinge knuckle with its pin; a piston rod with a collar; four square rivet heads. |
| `TRAP2-24_breakage_v1.png` | 3:2, transparent, sheet | Well-separated breakage pieces: three crack overlays of thin jagged dark lines (one small, one spreading, one running across), a crumbling stone edge strip seamless left to right, a scatter of wood splinters, a scatter of small stone chips. |
| `TRAP2-25_liana_v1.png` | 2:3, transparent, sheet | A thick climbable jungle liana in pieces, well separated: its top knot wrapped around a branch stub, a straight middle section seamless top to bottom with leaves, its bottom tip, and two frayed snapped ends with torn fibres. |

---

## AutoSprite plan (Stop 1 credits)

AutoSprite's prop animations insert a person and its text-only statics miss the styleframe (PAX-A13 gate, 2026-09-29), so
every body above is painted, and every motion (tell, action, impact, rest) is a tick function in code over the painted
parts and the sheets below. AutoSprite is used only for soft effects on black, which passed the gate before (T04, T06).

| Line | Asset | Kind | Credits (estimate) | Line ceiling (with one retry) |
|---|---|---|---|---|
| FX-01 | stone dust and grit puffs, 6 pieces (tinted per biome in code) | static, on black | 1 | 2 |
| FX-02 | impact sparks, 6 bursts (iron on stone) | static, on black | 1 | 2 |
| FX-03 | steam and hiss wisps, 6 pieces (vent tells) | static, on black | 1 | 2 |
| FX-04 | embers and fire flecks, 8 pieces (Ember Night, braziers) | static, on black | 1 | 2 |
| FX-05 | crystal glints and shards, 6 pieces (Sunken Grotto spikes) | static, on black | 1 | 2 |
| **Total** | | | **5** | **10** |

No animation lines (5–10 credits each) are planned. Every line stays far under the 20-credit per-asset cap; the ledger
stays in `Art_Source/AutoSprite/Traps/FX-0N_*/ledger.json`. Last known balance: 1,498 (2026-09-29; not re-queried, to
spend nothing before approval).

## Count

| Group | Images |
|---|---|
| A · Golden Ruins (new stone pieces and dressing) | 5 |
| B–E · four new biomes, 13 each | 52 |
| Signatures (19; L002 keeps its own) | 19 |
| Trap bodies | 15 |
| **ChatGPT total** | **91** |
| AutoSprite | 5 lines, ≤ 10 credits |
