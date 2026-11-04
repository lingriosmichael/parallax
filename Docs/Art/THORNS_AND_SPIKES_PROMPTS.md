# Thorns and spikes · test prompts, round 3 (2026-11-01, D-295)

Three paintings to test the new hazard look in one level before anything else is painted.

**The rule (D-295):** what a hazard looks like tells you how it behaves.
- **Thorns and crystal never move.** They mark places to avoid: pit floors, fixed walls and ceilings.
- **Metal spikes are traps.** Everything that pops up, slams or slides stays metal.

**What went wrong in round 1 (the developer: "It's not premium"):** the prompts asked for hero illustrations, so each one came back as a loud, close-up poster.
- **Too much saturation:** the attached GT-05 is bright orange, and every painting copied it.
- **Too much detail:** bark, cracks, rivets and stains are noise at 30 px on a phone.
- **Too big:** a few giant shapes filled the strip, where the game needs many small ones.
- **Magenta background:** it tinted every edge pink.

**Round 2 was silhouette first** (a dark, elegant shape with one thin line of light). The developer rejected that for the thorns too and gave a reference (HZ-01 below). HZ-02 and HZ-03 are still round 2 until they've been judged.

**How to work:** one new chat per image. Attach only what each item names, and **don't attach GT-04 or GT-05**. HZ-02 and HZ-03 use a flat green background and HZ-01 flat blue (green would eat its olive branches). Claude Code keys both out.

**Check it small first:** shrink the image to about 300 px wide. If it isn't a clean, elegant shape at that size, reject it.

---

### HZ-01 · Thorn branches (round 3)

**The developer's reference is the target:** `Docs/Art/Reference/thorn_reference_v1.png`. A real briar: open and airy, with thin, angular, mottled olive branches and long sharp thorns flushed pink-red at the base. It replaces rounds 1 and 2 (the dense hedge and the black silhouette).

**Save as:** `Art_Source/Traps_ChatGPT/HZ-01_thorn_branches_v3.png`, landscape 3:2, blue background.
**Attach:** `Docs/Art/Reference/thorn_reference_v1.png`, and nothing else.

```text
Use the attached image as the exact reference for subject, colour and finish: a real thorny briar branch. Match it closely.

Paint FOUR separate thorny branch clusters on one image, side by side with clear space between them, none touching, each different in shape. Each cluster is an open, airy tangle of a few thin, angular, crooked woody branches that bend at sharp joints and cross each other, with plenty of empty space showing between them, exactly like the reference. The branches are mottled olive green and sage, with dark green-black blotches and a little tan bark, and they taper to fine points at their ends. Long, straight, needle-sharp thorns stick out from the branches at wide angles: each thorn has a wide flared base flushed dusty pink and muted red-brown, fading to a pale tan point. There are about ten thorns per cluster. Each cluster is wider than it is tall, about twice as wide, and spreads low, with its heaviest branches near the bottom, as if growing along the ground.

The look is natural and realistic, softly lit, with subtle shading and muted, slightly desaturated colour, as in the reference. Keep the light soft and even, coming from the upper left.

No leaves, no flowers, no berries, no ground, no stone, no shadow on the background. No black outlines. Not a cartoon, not a vector icon, not stylised, not glossy, not bright or saturated. The background is one flat pure blue (#0000FF), edge to edge, with no glow. No text.
```

**Reject if:** it's dense like a hedge; the branches are thick or curvy like tentacles; the thorns are horns or claws; it's bright, glossy or cartoonish; it has leaves; the clusters touch; it has outlines; there's a brick or stone base.

**How it goes into the game:** Claude Code cuts the four clusters and overlaps them along each hazard, so no two stretches repeat. That's why it isn't a tiling strip.

---

### HZ-02 · Metal spike row

**Save as:** `Art_Source/Traps_ChatGPT/HZ-02_metal_spikes_v2.png`, the widest landscape your tool gives, green background, tiles sideways.
**Attach:** `Docs/Art/Reference/styleframe_A_v1.png`.

```text
Use the attached image only for its soft, muted, atmospheric painting technique and warm evening light.

Paint a thin horizontal row of ancient iron stakes for a premium hand-painted 2D platformer, seen from a distance, running the full width of the image and seamlessly tileable left to right. Think of the restrained, elegant art of Hollow Knight, Ori and the Blind Forest, and Inside.

There are about fifteen tall, very slim, straight spikes, each about eight times taller than it is wide, like long narrow spearheads or needles, tapering to a true point. They stand close together and evenly spaced, in two heights that alternate in a calm rhythm. They are matte dark iron (about #2A2526), nearly a silhouette, flat and simple. There is one single light effect: a thin, soft line of muted warm light (about #C9A27A) down the left edge of each spike. Their feet simply end in a straight line along the bottom edge.

Keep it simple. No rivets, no studs, no circles, no ornaments, no base, no plinth, no stone, no bricks, no cracks, no rust, no red, no blood, no glow. Fewer details always. Low contrast inside the shapes, strong clean outer silhouette, and low saturation.

Strict side view, orthographic. Painted, not a 3D render, not a cartoon, not a vector icon. The background is one flat pure green (#00FF00), edge to edge, with no glow, no shadow, and no green light on the subject. No text.
```

**Reject if:** the spikes are wide cones; they look like stone; there's any base or ornament; there's red or orange; they're spaced far apart; they look shiny.

---

### HZ-03 · Crystal shards

**Save as:** `Art_Source/Traps_Obsidian/HZ-03_crystal_shards_v2.png`, the widest landscape your tool gives, green background, tiles sideways.
**Attach:** `Docs/Art/Reference/styleframe_A_v1.png`, `Art_Source/Environment/ObsidianVoid/OV-10a3_fill_rimed_v1.png`.

```text
Use the first attached image only for its soft, muted painting technique, and the second for its dark obsidian colour.

Paint a thin horizontal strip of dark crystal shards for a premium hand-painted 2D platformer, seen from a distance, running the full width of the image and seamlessly tileable left to right. Think of the restrained, elegant art of Hollow Knight, Ori and the Blind Forest, and Inside.

There are about twenty slim, sharp shards of black volcanic glass, each small, leaning a little left or right, with heights varying only a little so the top edge forms one even, low line. Each shard has at most two plain facets. They are matte near-black (about #12141C), nearly a silhouette. There are only two light effects: a very thin, cold pale blue line along the upper left edge of each shard, and a faint, dim crimson (about #7A1420) deep inside only the tip of each shard, as if a coal were dying in it. The rest of each shard stays dark. Their feet end in a straight line along the bottom edge.

Keep it simple. No neon, no bright red, no orange, no fire, no glow around the shards, no rocks, no bricks, no ground under them. Fewer details always. Low contrast inside the shapes, strong clean outer silhouette, and low saturation.

Strict side view, orthographic. Painted, not a 3D render, not a glossy sci-fi UI, not a vector icon. The background is one flat pure green (#00FF00), edge to edge, with no glow, no shadow, and no green light on the subject. No text.
```

**Reject if:** it looks like neon or fire; red fills the shards; the shards are huge boulders; there's stone under them; there are many tiny facets.
