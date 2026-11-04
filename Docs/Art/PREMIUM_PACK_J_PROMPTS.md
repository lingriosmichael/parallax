# Premium Pack J · Readable hazards, Obsidian skies (2026-11-03)

Part of `Docs/V_TASKS/PREMIUM_VISUAL_PLAN.md`, the push to 9 (D-316). Four areas are held at 8 or under by two things:

- **Hazards that don't read on a phone.** 24 hazards in 12 levels fail `HazardContrastTests` (30 % of a hazard's pixels need 3:1 against what's behind them). 16 are briar thorns, 7 are metal blades on hinge floors, and 1 is an arrow. Tuning the current sprites got as far as it can: over mid-tone stone only bright pixels reach 3:1, and the thin briar and the slim blades have too few of them.
- **Obsidian levels look alike.** All ten share one sky (OV-01) and one set of mist bands (OV-03a/b/c). Those fill most of every frame, so a palette change barely shows.

Paint HZ-01 first: it fixes the most levels.

**What past rounds taught us:**
- **The briar is your look (HZ-01 v3).** Keep it: open, airy, olive branches, pink-red thorns. Only make it bolder.
- **Simple shapes win.** Detail is noise at 30 px on a phone.
- **Key colours:** blue for the briar, green for metal, magenta for Obsidian. Nothing in a piece may share its key colour.
- **No glow that fills the frame.** It washes out the cat and the hazards in front of it.

**How to work:**
- One new chat per image. Attach only what each prompt names.
- Paste the whole prompt block as it is. Each one is complete.
- Check it small first: shrink it to 300 px wide. Hazards must still read as hazards at that size; skies must still look different from each other.
- Save with the exact name, then tell Claude Code.

---

### HZ-01 v4 · Bolder briar thorns

**Save as:** `Art_Source/Traps_ChatGPT/HZ-01_thorn_branches_v4.png`, landscape 3:2, flat blue background.
**Attach:** `Docs/Art/Reference/thorn_reference_v1.png`, then `Art_Source/Traps_ChatGPT/HZ-01_thorn_branches_v3.png`.

**In game:** clusters about 2 u wide on pit floors, ledges and ceilings, seen at about 30 px tall on a phone, over bright sky and over mid-brown stone.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones. The first attached image is the briar I want; the second is the version in my game now. Paint the same briar, in the same style and colours as the second image, but bolder so it reads at a glance on a small screen.

Paint four separate briar clusters in a row, each with clear empty space around it, each about as wide as it is tall. Each is a tangle of a few thick, angular, woody branches, about twice as thick as in the second image, with long sharp thorns flushed pink-red at their base. Fewer branches and fewer, bigger thorns than the second image, so each cluster is a clear, open shape. The bark is a dark mottled olive with one clean, pale, sunlit edge along the top of every branch and a dark shaded edge underneath, so each branch reads both against a bright sky and against brown stone. The thorns are pale at their tips and deep pink-red at their base. Each cluster sits on an invisible flat ground line at its bottom.

Big simple shapes that read at phone size. Muted, natural colours, never neon. Strict flat side view, no perspective. Around the clusters the background is one flat pure blue (#0000FF), edge to edge, with no glow, no ground, no leaves and no shadow on the blue. No text, no flowers, no berries, no animals, no metal. Not a photo, not a 3D render, not a cartoon, no outlines.
```

**Reject if:** the branches are as thin as the second image's; the clusters touch; it reads as a hedge or a bush; the thorns are orange or brown; there's ground painted under it.

### HZ-02 v3 · Bolder metal spike row

**Save as:** `Art_Source/Traps_ChatGPT/HZ-02_metal_spikes_v3.png`, the widest landscape your tool gives, flat green background.
**Attach:** `Art_Source/Traps_ChatGPT/HZ-02_metal_spikes_v2.png`.

**In game:** the blades on hinge floors and pop-up spike traps, about 0.5 u tall, over brown stone and bright sky. They move, so they're metal (D-295).

```text
I'm making a premium, hand-painted 2D side-view platformer for phones. The attached image is the spike row in my game now. Paint the same row of ancient iron spikes in the same style, but bolder so each blade reads at a glance on a small screen.

Paint one straight row of twelve separate iron spike blades standing side by side on an invisible flat base line, with a small even gap between each. Each blade is a tall, sharp, slightly uneven triangle, a little wider than in the attached image. Each has a dark, almost black iron core, and one broad, bright, warm sunlit bevel down its left side, about a third of its width, ending in a bright point, so every blade reads both against a bright sky and against brown stone. A faint dark red stain near each tip. The bases are plain, dark and square-cut.

Big simple shapes that read at phone size. Strict flat side view, no perspective, no tilt. Around the blades the background is one flat pure green (#00FF00), edge to edge, with no glow, no ground and no shadow on the green. No text, no rivets, no rust patches, no ornament, nothing else. Not a photo, not a 3D render, not a cartoon, no outlines.
```

**Reject if:** the blades touch or overlap; the bright bevel is thin like the attached image's; they're chrome or silver all over; there's a base plate or ground painted.

### OV-01 b–e · Four Obsidian skies

**Save as:** `Art_Source/Environment/ObsidianVoid/OV-01b_sky_shattered_moon_v1.png`, `OV-01c_sky_aurora_v1.png`, `OV-01d_sky_ring_nebula_v1.png`, `OV-01e_sky_frozen_dawn_v1.png`, each wide 2:1. One chat each.
**Attach:** `Art_Source/Environment/ObsidianVoid/OV-01_sky_v1.png`.

**In game:** the sky behind L031–L040, filling the top two-thirds of every frame, behind the far skyline and the play layer. Claude Code gives each palette its own: the shattered moon to the cobalt and steel levels, the aurora to jade and teal, the ring nebula to violet and indigo, the frozen dawn to ice.

Each prompt below is complete. Only the middle paragraph differs.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones. Match the attached image's painting technique and its calm, dark mood exactly: it is the sky of the same world, at another place in it.

Paint a vast night sky over a void, with no ground, no buildings and no horizon line: a deep blue-black field with a few scattered small stars, and across it one great broken moon, cracked into three drifting pieces with thin cold light along their broken edges, low in the left half of the sky. Its light falls in a soft cool wash around it; the right half of the sky stays darker and calm.

Big, soft, simple shapes that read behind a game. Low contrast, dark, with the light kept to one area and never filling the sky. No mist or fog painted across the bottom. Cool colours only: blue, blue-black and white-blue. No text, no planets other than the moon, no people, no creatures, no red, no green. Not a photo, not a 3D render, not a cartoon.
```

```text
I'm making a premium, hand-painted 2D side-view platformer for phones. Match the attached image's painting technique and its calm, dark mood exactly: it is the sky of the same world, at another place in it.

Paint a vast night sky over a void, with no ground, no buildings and no horizon line: a deep teal-black field with a few small stars, and across its upper half two long, slow curtains of pale jade-green aurora, folding gently, brightest at their lower edge and fading upward. Below them the sky stays dark and calm.

Big, soft, simple shapes that read behind a game. Low contrast, dark, with the light kept to one area and never filling the sky. No mist or fog painted across the bottom. Cool colours only: teal, jade, blue-black and pale green. No text, no planets, no people, no creatures, no red. Not a photo, not a 3D render, not a cartoon.
```

```text
I'm making a premium, hand-painted 2D side-view platformer for phones. Match the attached image's painting technique and its calm, dark mood exactly: it is the sky of the same world, at another place in it.

Paint a vast night sky over a void, with no ground, no buildings and no horizon line: a deep violet-black field with a few small stars, and in its upper right one great faint ring nebula, a wide soft ring of violet and pale lilac dust with a small cold white star at its centre. The rest of the sky stays dark and calm.

Big, soft, simple shapes that read behind a game. Low contrast, dark, with the light kept to one area and never filling the sky. No mist or fog painted across the bottom. Cool colours only: violet, indigo, lilac and blue-black. No text, no planets, no people, no creatures, no red, no green. Not a photo, not a 3D render, not a cartoon.
```

```text
I'm making a premium, hand-painted 2D side-view platformer for phones. Match the attached image's painting technique and its calm, dark mood exactly: it is the sky of the same world, at another place in it.

Paint a vast sky over a void at a cold dawn, with no ground, no buildings and no horizon line: deep blue at the top, easing to a pale, icy blue-white low down, with a few long, thin, frosted streaks of high cloud lit from below and a single faint star high up. Quiet, clear and cold.

Big, soft, simple shapes that read behind a game. Low contrast, with the light kept low and never filling the sky. No mist or fog painted across the bottom. Cool colours only: blue, ice blue and white. No text, no sun disc, no planets, no people, no creatures, no red, no green. Not a photo, not a 3D render, not a cartoon.
```

**Reject if:** the light fills more than about a third of the sky; there's mist, ground or a horizon painted; it doesn't look clearly different from the other three at 300 px; anything is red.

### OV-03d · Streaked mist

**Save as:** `Art_Source/Environment/ObsidianVoid/OV-03d_mist_streaks_v1.png`, wide 4:1, flat black background.
**Attach:** `Art_Source/Environment/ObsidianVoid/OV-03b_mist_mid_v1.png`.

**In game:** the low mist band of every Obsidian level, replacing the soft milky band that makes them look alike. Claude Code turns the black into transparency and tints it with each level's colour.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones. The attached image is the mist band in my game now: it's too even and milky. Paint a replacement in the same technique.

Paint one long, low band of drifting mist seen from the side, made of separate, thin, horizontal streaks and wisps of pale grey-white vapour, with clear dark gaps between them, thicker and denser near the bottom of the band and breaking into a few loose threads at its top. Uneven along its length: in places almost clear, in places a few streaks bunch together. Its two ends fade out softly so it can repeat sideways.

Soft, simple shapes. Pale grey-white only, never coloured. Around it the background is one flat pure black (#000000), edge to edge. No text, no ground, no structures, no stars, no glow. Not a photo, not a 3D render, not a cartoon.
```

**Reject if:** it's one even sheet with no gaps; it's coloured; there are structures or stars in it; its ends end in a hard edge.
