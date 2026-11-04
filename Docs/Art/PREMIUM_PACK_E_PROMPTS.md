# Premium Pack E · Trap refresh (2026-11-03)

Part of `Docs/V_TASKS/PREMIUM_VISUAL_PLAN.md`, Phase 3. Two things still look below the rest of the game:

- **The spear** (`TRAP-05_spear.png`) is a small, rough cut-out with a pink fringe on its tip.
- **The exit door** is the same Gardens arch in all 30 Golden Ruins levels.

Pack E repaints the spear and gives The Overgrown Quarter (L011–L020) and The High Citadel (L021–L030) a door of their own. The orbs were redrawn in D-284, so they aren't in this pack.

**What past rounds taught us:**
- **Don't attach saturated art.** Bright references turned everything orange.
- **Simple shapes win.** Fine cracks and rivets turn to noise on a phone.
- **Blue background.** Claude Code keys the blue out, so nothing in these pieces may be blue.
- **Red only means danger.** The spear may carry a little dark red; the doors never do.

**How to work:**
- One new chat per image. Attach only the references the prompt names.
- Paste the whole prompt block as it is. Each one is complete.
- Check it small first: shrink it to 400 px wide. If it isn't one clear shape at that size, reject it.
- Save to `Art_Source/Traps_ChatGPT/` (the spear) or `Art_Source/Environment/GoldenRuins/` (the doors) with the exact name, then tell Claude Code.

**References:**
- **R1:** `Docs/Art/Reference/styleframe_A_v1.png` (painting technique and light)
- **R2:** `Assets/_Game/Art/RealityA/Environment/Kit/ENV_Fill_A.png` (the stone the doors stand in)
- **R3:** `Assets/_Game/Art/RealityA/Environment/Kit/ENV_Door.png` (today's door: its size, its proportions and its glowing opening)

---

### TE-01 · Spear

**Save as:** `Art_Source/Traps_ChatGPT/TE-01_spear_v1.png`, wide 5:1. **Attach:** R1.

**In game:** shot out of a wall, flying flat, about 1.4 u long and 0.3 u thick (the view is about 20 u wide), always over stone or sky. Claude Code cuts it into a head and a shaft, so the shaft can be any length.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones. Match the attached image's painting technique and light.

Paint one ancient spear lying perfectly horizontal, pointing right, seen exactly from the side. A long, straight, plain shaft of dark old wood, the same thickness all the way along, with no bend, no carving and no wrapping in its middle, so any length of it looks the same. At its right end a heavy leaf-shaped spearhead of dark worn steel with one clean bright edge catching the light and a hint of dark dried red near its point. At its left end a short bronze collar. It is a weapon from the same ruined city as the attached image: old, heavy, well made.

Big simple shapes that read at phone size. A clear silhouette with a crisp outline, so it reads at a glance against both stone and sky. Muted, warm, never saturated. Warm brown shadows, never black. Strict flat side view, no perspective, no tilt. Around it the background is one flat pure blue (#0000FF), edge to edge, with no glow and no shadow on the blue. No text, no letters, no gems, no feathers, no ribbons, no hand holding it. Not a photo, not a 3D render, not a cartoon.
```

**Reject if:** the shaft bends, tapers or has anything on its middle; the spear is tilted; the head is shiny chrome; there's a hand or a wall in it.

### TE-02 · Door of The Overgrown Quarter

**Save as:** `Art_Source/Environment/GoldenRuins/TE-02_door_overgrown_v1.png`, portrait 4:5. **Attach:** R1, R2, R3.

**In game:** the level's exit in L011–L020, about 1.5 u wide and 1.8 u tall, standing on the floor. The cat runs into its glowing opening, so the opening has to read as "the way out" at once.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones. Match the first attached image's painting technique and light, match the second attached image's stone exactly (its colour, its block size and its surface), and match the third attached image's size, proportions and glowing opening: it is the same kind of exit door, from another part of the same city.

Paint one ancient stone archway standing on the ground, half swallowed by a giant fig tree: thick pale roots pour over the top of the arch and grip its sides, ferns and moss grow from its base, and one root has split a stone of its frame. Inside the arch is the same warm golden light as the third image, bright and calm, filling the opening from edge to edge, clearly a way through. The roots frame the light but never cross the opening.

Big simple shapes that read at phone size. Muted, warm, low contrast in the stone, never saturated. Warm brown shadows, never black. Strict flat front view, no perspective. The base of the arch is flat, standing on nothing. Around it the background is one flat pure blue (#0000FF), edge to edge, with no glow and no shadow on the blue. No text, no letters, no animals, no gems, no red, no banners. Not a photo, not a 3D render, not a cartoon.
```

**Reject if:** a root crosses the light; the light isn't the third image's golden light; it's bigger or narrower than the third image's door; its base isn't flat.

### TE-03 · Door of The High Citadel

**Save as:** `Art_Source/Environment/GoldenRuins/TE-03_door_citadel_v1.png`, portrait 4:5. **Attach:** R1, R2, R3.

**In game:** the level's exit in L021–L030, about 1.5 u wide and 1.8 u tall, standing on the floor. The cat runs into its glowing opening, so the opening has to read as "the way out" at once.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones. Match the first attached image's painting technique and light, match the second attached image's stone exactly (its colour, its block size and its surface), and match the third attached image's size, proportions and glowing opening: it is the same kind of exit door, from another part of the same city.

Paint one tall ancient stone gateway standing on the ground, from the proud upper citadel of the city: a pointed arch of large dressed stones, a heavy carved keystone, two square columns with bronze-banded bases, and worn bronze bands across the frame, darkened with age. Inside the arch is the same warm golden light as the third image, bright and calm, filling the opening from edge to edge, clearly a way through.

Big simple shapes that read at phone size. Muted, warm, low contrast in the stone, never saturated. Warm brown shadows, never black. Strict flat front view, no perspective. The base of the gateway is flat, standing on nothing. Around it the background is one flat pure blue (#0000FF), edge to edge, with no glow and no shadow on the blue. No text, no letters, no symbols, no animals, no gems, no red, no banners, no doors or bars across the opening. Not a photo, not a 3D render, not a cartoon.
```

**Reject if:** anything crosses the light; the bronze is shiny or orange; it's bigger or narrower than the third image's door; its base isn't flat.
