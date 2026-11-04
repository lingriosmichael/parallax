# Cat and store art · prompts (2026-11-01)

These cover the two gaps that had no prompts: the cat's animation (from `NEEDED_ASSETS.md`) and the store art (`PAX-A10`). Same standard as the trap prompts: simple shapes, rich finish.

**Environment leftovers and the `GripWall` texture in `NEEDED_ASSETS.md` aren't here:** they aren't cat art. `CatA_Idle`'s coat match isn't either: that's a tone adjustment, not a painting.

## Cat

### What the game already covers (D-293)

Built from the cat's own frames, so these prompts are now upgrades, not gaps: C-03, C-04, C-06, C-07, C-08, C-09, C-10, C-11, C-12, C-13, C-14, C-16, C-18, C-20, C-21, C-22, C-23, C-24. C-01 was skipped because a stop lasts 0.075 s, C-02 stays the stance stop, C-05 already exists, and C-15 and C-19 are covered by the existing climb.

**Nothing to build from, so only painting can fix it:** C-17, hanging from the hands.

### Order

**Paint C-10 first and send it to Claude Code before doing the rest.** The risk with AI frames is that the cat changes from frame to frame and doesn't match the existing strips. C-10 proves whether this method works, and it stands alone, so nothing else depends on it. If it passes, this order gives the most for the least:

1. **Most visible:** C-10 falling loop, C-01 run brake, C-07 running takeoff, C-24 door celebration, C-05 landing contact.
2. **Next:** C-02, C-09, C-08, C-13, C-20, C-21, C-22, C-06, C-11.
3. **Climbing:** C-14 to C-19 (vines and grip walls).
4. **Optional:** C-03, C-04, C-12, C-23.

### How to work

- **Image:** landscape 3:2, the largest your tool gives. Each frame must come out at least 300 px tall: the game's cells are about 192 px.
- **One clip per image,** so all its frames are the same cat. Start a new chat for each clip, and attach the references each time.
- **References:** **CS** is `DesignImages/13_cat-character-sheet.png`. Strips are in `Assets/_Game/Art/Cats/CatA/`. They're your real frames: attach the ones named, because they set the size and style.
- **Saving:** `Art_Source/CatA/` with the exact name. Claude Code cuts, registers and matches the tone (`cat_register.py`), and wires each clip in under an art ticket. Cat animation stays a flipbook: no Animator.

**Reject any image if:**
- it isn't the same cat as the strip, or the coat is pale, grey or flat black;
- frames differ in size, head shape or tail length;
- any frame has five legs, a missing leg, a merged paw or two tails;
- a tail or ear is cut off, or two frames touch;
- there's ground, a shadow, dust or motion lines;
- it looks like a cartoon.

## To paint: cat

### C-01 · Run brake (4 frames)

**Save as:** `Art_Source/CatA/C-01_run_brake_v1.png`, landscape 3:2, magenta background. **Attach:** CS, `CatA_Run.png`, `CatA_Walk.png`.
**Why:** a run stops with a pop into Walk.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). These are animation frames for its hero, a lean black cat. From the attached character sheet and animation strip copy this exact cat: the same body, proportions, head, long S-curved tail and painting technique. Its coat is near-black, a deep warm brown (around #1E1410), short soft fur painted with fine directional strokes so the form always reads, never a flat black silhouette. Its eyes are amber-orange (around #E8942A), the insides of its ears are dusty pink, and a thin warm rim of sunlit gold runs along its back, ears and tail. It is elegant, athletic and real, a feline with true weight, balance and muscle, never a cartoon mascot. The cat faces RIGHT in every frame.

Paint 4 animation frames in one row, left to right, evenly spaced. Together they are a galloping cat skidding to a stop. Frame 1: the last gallop stride, hind legs sweeping forward under the body, front legs reaching. Frame 2: the brake, front paws planted far ahead, shoulders dropped, hindquarters sinking, weight thrown back and tail swinging up for balance. Frame 3: the body rising and settling back over its legs, tail still high. Frame 4: a calm walking stance, exactly the first frame of the attached Walk strip. Across all frames the paws meet one invisible level ground line.

Every frame shows the SAME cat at the SAME size, drawn with the same detail and the same light. Strict side view, orthographic: no perspective and no 3/4 angle unless a frame above asks for it. Exactly four legs and one tail in every frame, and the whole cat, ears to tail tip, is inside its own space with clear empty space around it, so no two frames touch and nothing is cut off. Nothing else is in the image: no ground, no shadow, no dust, no motion lines, no effects, no props, no numbers. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a flat game asset. The background is one flat pure magenta (#FF00FF), edge to edge. No text, no letters, no other characters.
```

**Reject if:** frame 4 doesn't match Walk's first pose; it looks like a jump (and the general list above).

### C-02 · Walk to stand (6 frames)

**Save as:** `Art_Source/CatA/C-02_walk_to_stand_v1.png`, landscape 3:2, magenta background. **Attach:** CS, `CatA_Walk.png`, `CatA_Idle.png`.
**Why:** a walking cat snaps its legs into Idle in one frame.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). These are animation frames for its hero, a lean black cat. From the attached character sheet and animation strip copy this exact cat: the same body, proportions, head, long S-curved tail and painting technique. Its coat is near-black, a deep warm brown (around #1E1410), short soft fur painted with fine directional strokes so the form always reads, never a flat black silhouette. Its eyes are amber-orange (around #E8942A), the insides of its ears are dusty pink, and a thin warm rim of sunlit gold runs along its back, ears and tail. It is elegant, athletic and real, a feline with true weight, balance and muscle, never a cartoon mascot. The cat faces RIGHT in every frame.

Paint 6 animation frames in rows of four, reading left to right, top to bottom, evenly spaced. They are two short stops from a walk. Frames 1 to 3 start with the left legs leading, and frames 4 to 6 start with the right legs leading. In each group: a slowing mid-stride, then the legs gathering under the body, then the relaxed standing pose exactly as in the first frame of the attached Idle strip. Frames 3 and 6 are identical. Across all frames the paws meet one invisible level ground line.

Every frame shows the SAME cat at the SAME size, drawn with the same detail and the same light. Strict side view, orthographic: no perspective and no 3/4 angle unless a frame above asks for it. Exactly four legs and one tail in every frame, and the whole cat, ears to tail tip, is inside its own space with clear empty space around it, so no two frames touch and nothing is cut off. Nothing else is in the image: no ground, no shadow, no dust, no motion lines, no effects, no props, no numbers. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a flat game asset. The background is one flat pure magenta (#FF00FF), edge to edge. No text, no letters, no other characters.
```

**Reject if:** frames 3 and 6 differ or don't match Idle; the legs are the same in both groups (and the general list above).

### C-03 · Turn (5 frames)

**Save as:** `Art_Source/CatA/C-03_turn_v1.png`, landscape 3:2, magenta background. **Attach:** CS, `CatA_Walk.png`, `CatA_Turn.png`.
**Why:** turning is an instant mirror (optional).

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). These are animation frames for its hero, a lean black cat. From the attached character sheet and animation strip copy this exact cat: the same body, proportions, head, long S-curved tail and painting technique. Its coat is near-black, a deep warm brown (around #1E1410), short soft fur painted with fine directional strokes so the form always reads, never a flat black silhouette. Its eyes are amber-orange (around #E8942A), the insides of its ears are dusty pink, and a thin warm rim of sunlit gold runs along its back, ears and tail. It is elegant, athletic and real, a feline with true weight, balance and muscle, never a cartoon mascot. This one clip goes from facing right to facing left.

Paint 5 animation frames in rows of four, reading left to right, top to bottom, evenly spaced. Together they are the cat turning round on the spot, its paws staying on one spot. Frame 1: side view facing right. Frame 2: three-quarter view turning toward the viewer. Frame 3: facing the viewer, tail swinging behind. Frame 4: three-quarter view turning to the left. Frame 5: side view facing left, a perfect mirror of frame 1. The body stays the same size throughout: it is a turn, not a step toward the camera. Across all frames the paws meet one invisible level ground line.

Every frame shows the SAME cat at the SAME size, drawn with the same detail and the same light. Strict side view, orthographic: no perspective and no 3/4 angle unless a frame above asks for it. Exactly four legs and one tail in every frame, and the whole cat, ears to tail tip, is inside its own space with clear empty space around it, so no two frames touch and nothing is cut off. Nothing else is in the image: no ground, no shadow, no dust, no motion lines, no effects, no props, no numbers. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a flat game asset. The background is one flat pure magenta (#FF00FF), edge to edge. No text, no letters, no other characters.
```

**Reject if:** the cat grows toward the viewer, as `CatA_Turn` did; frame 5 isn't frame 1 mirrored (and the general list above).

### C-04 · Push against a wall (3 frames)

**Save as:** `Art_Source/CatA/C-04_wall_push_v1.png`, landscape 3:2, magenta background. **Attach:** CS, `CatA_Run.png`.
**Why:** running into a wall ends in a relaxed Idle (optional).

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). These are animation frames for its hero, a lean black cat. From the attached character sheet and animation strip copy this exact cat: the same body, proportions, head, long S-curved tail and painting technique. Its coat is near-black, a deep warm brown (around #1E1410), short soft fur painted with fine directional strokes so the form always reads, never a flat black silhouette. Its eyes are amber-orange (around #E8942A), the insides of its ears are dusty pink, and a thin warm rim of sunlit gold runs along its back, ears and tail. It is elegant, athletic and real, a feline with true weight, balance and muscle, never a cartoon mascot. The cat faces RIGHT in every frame.

Paint 3 animation frames in one row, left to right, evenly spaced. They are a short loop of a cat straining against an invisible wall just past its nose on the right. Its chest and forehead press forward, front paws high and flat against the wall, hind legs digging and driving, tail low and stiff. Only the hind legs shift between frames, step by step, and frame 3 leads back into frame 1. Across all frames the paws meet one invisible level ground line.

Every frame shows the SAME cat at the SAME size, drawn with the same detail and the same light. Strict side view, orthographic: no perspective and no 3/4 angle unless a frame above asks for it. Exactly four legs and one tail in every frame, and the whole cat, ears to tail tip, is inside its own space with clear empty space around it, so no two frames touch and nothing is cut off. Nothing else is in the image: no ground, no shadow, no dust, no motion lines, no effects, no props, no numbers. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a flat game asset. The background is one flat pure magenta (#FF00FF), edge to edge. No text, no letters, no other characters.
```

**Reject if:** it's standing relaxed; a wall is painted (and the general list above).

### C-05 · Landing contact (2 frames)

**Save as:** `Art_Source/CatA/C-05_landing_contact_v1.png`, landscape 3:2, magenta background. **Attach:** CS, `CatA_Fall.png`, `CatA_Land.png`, `CatA_HardLand.png`.
**Why:** landings compress too fast between frames.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). These are animation frames for its hero, a lean black cat. From the attached character sheet and animation strip copy this exact cat: the same body, proportions, head, long S-curved tail and painting technique. Its coat is near-black, a deep warm brown (around #1E1410), short soft fur painted with fine directional strokes so the form always reads, never a flat black silhouette. Its eyes are amber-orange (around #E8942A), the insides of its ears are dusty pink, and a thin warm rim of sunlit gold runs along its back, ears and tail. It is elegant, athletic and real, a feline with true weight, balance and muscle, never a cartoon mascot. The cat faces RIGHT in every frame.

Paint 2 animation frames in one row, left to right, evenly spaced. Both show the instant a falling cat touches down: fore paws just touching the ground, elbows starting to bend, hind legs still in the air, back arched and tail high. Frame 1 is a normal landing from a short jump. Frame 2 is a hard landing from a long fall: fore legs more compressed, chest lower, hindquarters higher, ears back.

Every frame shows the SAME cat at the SAME size, drawn with the same detail and the same light. Strict side view, orthographic: no perspective and no 3/4 angle unless a frame above asks for it. Exactly four legs and one tail in every frame, and the whole cat, ears to tail tip, is inside its own space with clear empty space around it, so no two frames touch and nothing is cut off. Nothing else is in the image: no ground, no shadow, no dust, no motion lines, no effects, no props, no numbers. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a flat game asset. The background is one flat pure magenta (#FF00FF), edge to edge. No text, no letters, no other characters.
```

**Reject if:** all four paws are down; it looks relaxed (and the general list above).

### C-06 · Hard landing (redraw) (4 frames)

**Save as:** `Art_Source/CatA/C-06_hard_land_v1.png`, landscape 3:2, magenta background. **Attach:** CS, `CatA_HardLand.png`, `CatA_Fall.png`.
**Why:** the current sheet has a cut tail tip, a stray outline tick and a paler body.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). These are animation frames for its hero, a lean black cat. From the attached character sheet and animation strip copy this exact cat: the same body, proportions, head, long S-curved tail and painting technique. Its coat is near-black, a deep warm brown (around #1E1410), short soft fur painted with fine directional strokes so the form always reads, never a flat black silhouette. Its eyes are amber-orange (around #E8942A), the insides of its ears are dusty pink, and a thin warm rim of sunlit gold runs along its back, ears and tail. It is elegant, athletic and real, a feline with true weight, balance and muscle, never a cartoon mascot. The cat faces RIGHT in every frame.

Paint 4 animation frames in one row, left to right, evenly spaced. They are one hard landing from a long fall, matching the attached strip's poses. Frame 1: fore paws hit and the chest drops. Frame 2: the deepest crouch, belly nearly on the ground, legs splayed, ears flat, tail flung high in one unbroken curve. Frame 3: pushing up, shaking off the shock. Frame 4: back on all fours, head low, ready to go. Across all frames the paws meet one invisible level ground line.

Every frame shows the SAME cat at the SAME size, drawn with the same detail and the same light. Strict side view, orthographic: no perspective and no 3/4 angle unless a frame above asks for it. Exactly four legs and one tail in every frame, and the whole cat, ears to tail tip, is inside its own space with clear empty space around it, so no two frames touch and nothing is cut off. Nothing else is in the image: no ground, no shadow, no dust, no motion lines, no effects, no props, no numbers. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a flat game asset. The background is one flat pure magenta (#FF00FF), edge to edge. No text, no letters, no other characters.
```

**Reject if:** the tail is cut or broken anywhere; the coat is paler than the Run strip (and the general list above).

### C-07 · Running takeoff (3 frames)

**Save as:** `Art_Source/CatA/C-07_running_takeoff_v1.png`, landscape 3:2, magenta background. **Attach:** CS, `CatA_Run.png`, `CatA_Rise.png`.
**Why:** from a run, takeoff is an upright squat that reads as braking.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). These are animation frames for its hero, a lean black cat. From the attached character sheet and animation strip copy this exact cat: the same body, proportions, head, long S-curved tail and painting technique. Its coat is near-black, a deep warm brown (around #1E1410), short soft fur painted with fine directional strokes so the form always reads, never a flat black silhouette. Its eyes are amber-orange (around #E8942A), the insides of its ears are dusty pink, and a thin warm rim of sunlit gold runs along its back, ears and tail. It is elegant, athletic and real, a feline with true weight, balance and muscle, never a cartoon mascot. The cat faces RIGHT in every frame.

Paint 3 animation frames in one row, left to right, evenly spaced. They take a galloping cat straight into a jump with no slowing. Frame 1: last stride, body low and long, hind legs bunched under it. Frame 2: hind legs driving hard off the ground, fore legs reaching up and forward. Frame 3: full stretch leaving the ground, matching the first frame of the attached Rise strip, tail streaming behind.

Every frame shows the SAME cat at the SAME size, drawn with the same detail and the same light. Strict side view, orthographic: no perspective and no 3/4 angle unless a frame above asks for it. Exactly four legs and one tail in every frame, and the whole cat, ears to tail tip, is inside its own space with clear empty space around it, so no two frames touch and nothing is cut off. Nothing else is in the image: no ground, no shadow, no dust, no motion lines, no effects, no props, no numbers. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a flat game asset. The background is one flat pure magenta (#FF00FF), edge to edge. No text, no letters, no other characters.
```

**Reject if:** it stops or squats; frame 3 doesn't match Rise (and the general list above).

### C-08 · Straight up and down (4 frames)

**Save as:** `Art_Source/CatA/C-08_straight_jump_v1.png`, landscape 3:2, magenta background. **Attach:** CS, `CatA_Rise.png`, `CatA_Fall.png`.
**Why:** a jump in place uses a forward pounce, so it looks like it should travel.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). These are animation frames for its hero, a lean black cat. From the attached character sheet and animation strip copy this exact cat: the same body, proportions, head, long S-curved tail and painting technique. Its coat is near-black, a deep warm brown (around #1E1410), short soft fur painted with fine directional strokes so the form always reads, never a flat black silhouette. Its eyes are amber-orange (around #E8942A), the insides of its ears are dusty pink, and a thin warm rim of sunlit gold runs along its back, ears and tail. It is elegant, athletic and real, a feline with true weight, balance and muscle, never a cartoon mascot. The cat faces RIGHT in every frame.

Paint 4 animation frames in one row, left to right, evenly spaced. They are a jump straight up on the spot, with no forward travel. Frames 1 and 2: rising, body upright and long, head up, fore legs reaching overhead, hind legs trailing straight down, tail hanging below. Frames 3 and 4: dropping, body compact and level, all four legs reaching down, tail rising above it.

Every frame shows the SAME cat at the SAME size, drawn with the same detail and the same light. Strict side view, orthographic: no perspective and no 3/4 angle unless a frame above asks for it. Exactly four legs and one tail in every frame, and the whole cat, ears to tail tip, is inside its own space with clear empty space around it, so no two frames touch and nothing is cut off. Nothing else is in the image: no ground, no shadow, no dust, no motion lines, no effects, no props, no numbers. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a flat game asset. The background is one flat pure magenta (#FF00FF), edge to edge. No text, no letters, no other characters.
```

**Reject if:** the body is diagonal, as in a forward leap (and the general list above).

### C-09 · Step down (2 frames)

**Save as:** `Art_Source/CatA/C-09_step_down_v1.png`, landscape 3:2, magenta background. **Attach:** CS, `CatA_Walk.png`, `CatA_Fall.png`.
**Why:** walking off a 1 u ledge shows a full leap.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). These are animation frames for its hero, a lean black cat. From the attached character sheet and animation strip copy this exact cat: the same body, proportions, head, long S-curved tail and painting technique. Its coat is near-black, a deep warm brown (around #1E1410), short soft fur painted with fine directional strokes so the form always reads, never a flat black silhouette. Its eyes are amber-orange (around #E8942A), the insides of its ears are dusty pink, and a thin warm rim of sunlit gold runs along its back, ears and tail. It is elegant, athletic and real, a feline with true weight, balance and muscle, never a cartoon mascot. The cat faces RIGHT in every frame.

Paint 2 animation frames in one row, left to right, evenly spaced. The cat has just walked off a low ledge and drops a short way, not jumping. Frame 1: front paws reaching down and forward past the edge, shoulders dipping, hind legs still extended behind it as in a walk. Frame 2: body level in the air, all four legs reaching down for the ground, tail lifting. It looks casual, a step not a leap.

Every frame shows the SAME cat at the SAME size, drawn with the same detail and the same light. Strict side view, orthographic: no perspective and no 3/4 angle unless a frame above asks for it. Exactly four legs and one tail in every frame, and the whole cat, ears to tail tip, is inside its own space with clear empty space around it, so no two frames touch and nothing is cut off. Nothing else is in the image: no ground, no shadow, no dust, no motion lines, no effects, no props, no numbers. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a flat game asset. The background is one flat pure magenta (#FF00FF), edge to edge. No text, no letters, no other characters.
```

**Reject if:** it looks like a powerful jump (and the general list above).

### C-10 · Falling loop (4 frames)

**Save as:** `Art_Source/CatA/C-10_fall_loop_v1.png`, landscape 3:2, magenta background. **Attach:** CS, `CatA_Fall.png`.
**Why:** long falls hold one frozen frame for 15 to 20 frames. **Paint this one first, as the test.**

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). These are animation frames for its hero, a lean black cat. From the attached character sheet and animation strip copy this exact cat: the same body, proportions, head, long S-curved tail and painting technique. Its coat is near-black, a deep warm brown (around #1E1410), short soft fur painted with fine directional strokes so the form always reads, never a flat black silhouette. Its eyes are amber-orange (around #E8942A), the insides of its ears are dusty pink, and a thin warm rim of sunlit gold runs along its back, ears and tail. It is elegant, athletic and real, a feline with true weight, balance and muscle, never a cartoon mascot. The cat faces RIGHT in every frame.

Paint 4 animation frames in one row, left to right, evenly spaced. They are a seamless loop of a long fall. The body stays level and the same height, all four legs spread and reaching down, while the tail whips slowly up in a long S and the ears and fur stream upward. Each frame changes only slightly: leg angles shift, the tail bends the other way, and frame 4 leads smoothly back into frame 1. It should feel like wind rushing past, tense and helpless.

Every frame shows the SAME cat at the SAME size, drawn with the same detail and the same light. Strict side view, orthographic: no perspective and no 3/4 angle unless a frame above asks for it. Exactly four legs and one tail in every frame, and the whole cat, ears to tail tip, is inside its own space with clear empty space around it, so no two frames touch and nothing is cut off. Nothing else is in the image: no ground, no shadow, no dust, no motion lines, no effects, no props, no numbers. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a flat game asset. The background is one flat pure magenta (#FF00FF), edge to edge. No text, no letters, no other characters.
```

**Reject if:** the body changes size or tilt between frames; the loop can't close (and the general list above).

### C-11 · Apex in-betweens (2 frames)

**Save as:** `Art_Source/CatA/C-11_apex_v1.png`, landscape 3:2, magenta background. **Attach:** CS, `CatA_Rise.png`, `CatA_Apex.png`, `CatA_Fall.png`.
**Why:** the jump snaps at the top of the arc.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). These are animation frames for its hero, a lean black cat. From the attached character sheet and animation strip copy this exact cat: the same body, proportions, head, long S-curved tail and painting technique. Its coat is near-black, a deep warm brown (around #1E1410), short soft fur painted with fine directional strokes so the form always reads, never a flat black silhouette. Its eyes are amber-orange (around #E8942A), the insides of its ears are dusty pink, and a thin warm rim of sunlit gold runs along its back, ears and tail. It is elegant, athletic and real, a feline with true weight, balance and muscle, never a cartoon mascot. The cat faces RIGHT in every frame.

Paint 2 animation frames in one row, left to right, evenly spaced. Frame 1 is exactly halfway between the last Rise frame and the first Apex frame: the body tilting from about 40 degrees toward level. Frame 2 is exactly halfway between the last Apex frame and the first Fall frame: the long stretch starting to gather into a compact pose.

Every frame shows the SAME cat at the SAME size, drawn with the same detail and the same light. Strict side view, orthographic: no perspective and no 3/4 angle unless a frame above asks for it. Exactly four legs and one tail in every frame, and the whole cat, ears to tail tip, is inside its own space with clear empty space around it, so no two frames touch and nothing is cut off. Nothing else is in the image: no ground, no shadow, no dust, no motion lines, no effects, no props, no numbers. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a flat game asset. The background is one flat pure magenta (#FF00FF), edge to edge. No text, no letters, no other characters.
```

**Reject if:** either frame isn't clearly between its two neighbours (and the general list above).

### C-12 · Geyser launch (3 frames)

**Save as:** `Art_Source/CatA/C-12_launch_v1.png`, landscape 3:2, magenta background. **Attach:** CS, `CatA_Rise.png`, `CatA_Idle.png`.
**Why:** a geyser cuts from Idle to Rise (optional: the state is parked).

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). These are animation frames for its hero, a lean black cat. From the attached character sheet and animation strip copy this exact cat: the same body, proportions, head, long S-curved tail and painting technique. Its coat is near-black, a deep warm brown (around #1E1410), short soft fur painted with fine directional strokes so the form always reads, never a flat black silhouette. Its eyes are amber-orange (around #E8942A), the insides of its ears are dusty pink, and a thin warm rim of sunlit gold runs along its back, ears and tail. It is elegant, athletic and real, a feline with true weight, balance and muscle, never a cartoon mascot. The cat faces RIGHT in every frame.

Paint 3 animation frames in one row, left to right, evenly spaced. They show a standing cat blasted upward from below. Frame 1: thrown off balance, legs buckling, eyes wide, body starting to lift. Frame 2: hurled upward, body arched back, all legs splayed, tail bent down by the force. Frame 3: rising fast, gathering itself, ears flat.

Every frame shows the SAME cat at the SAME size, drawn with the same detail and the same light. Strict side view, orthographic: no perspective and no 3/4 angle unless a frame above asks for it. Exactly four legs and one tail in every frame, and the whole cat, ears to tail tip, is inside its own space with clear empty space around it, so no two frames touch and nothing is cut off. Nothing else is in the image: no ground, no shadow, no dust, no motion lines, no effects, no props, no numbers. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a flat game asset. The background is one flat pure magenta (#FF00FF), edge to edge. No text, no letters, no other characters.
```

**Reject if:** it looks like a deliberate jump; water is painted (and the general list above).

### C-13 · Gravity flip: tuck and uncurl (6 frames)

**Save as:** `Art_Source/CatA/C-13_flip_v1.png`, landscape 3:2, magenta background. **Attach:** CS, `CatA_FlipRoll.png`, `CatA_Fall.png`.
**Why:** a stretched pose turns into a ball in one frame.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). These are animation frames for its hero, a lean black cat. From the attached character sheet and animation strip copy this exact cat: the same body, proportions, head, long S-curved tail and painting technique. Its coat is near-black, a deep warm brown (around #1E1410), short soft fur painted with fine directional strokes so the form always reads, never a flat black silhouette. Its eyes are amber-orange (around #E8942A), the insides of its ears are dusty pink, and a thin warm rim of sunlit gold runs along its back, ears and tail. It is elegant, athletic and real, a feline with true weight, balance and muscle, never a cartoon mascot. The cat faces RIGHT in every frame.

Paint 6 animation frames in rows of four, reading left to right, top to bottom, evenly spaced. Frames 1 to 3 are a tuck from a stretched running pose into the tight ball of the attached Roll strip: legs drawing in, spine curving, tail wrapping round, until frame 3 matches the strip's first frame. Frames 4 to 6 are the uncurl from that ball into a spread falling pose, with frame 4 matching the strip's last frame and frame 6 matching the first frame of the attached Fall strip.

Every frame shows the SAME cat at the SAME size, drawn with the same detail and the same light. Strict side view, orthographic: no perspective and no 3/4 angle unless a frame above asks for it. Exactly four legs and one tail in every frame, and the whole cat, ears to tail tip, is inside its own space with clear empty space around it, so no two frames touch and nothing is cut off. Nothing else is in the image: no ground, no shadow, no dust, no motion lines, no effects, no props, no numbers. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a flat game asset. The background is one flat pure magenta (#FF00FF), edge to edge. No text, no letters, no other characters.
```

**Reject if:** frames 3 and 4 don't match the Roll strip; frame 6 doesn't match Fall (and the general list above).

### C-14 · Vine grab and release (4 frames)

**Save as:** `Art_Source/CatA/C-14_vine_grab_v1.png`, landscape 3:2, magenta background. **Attach:** CS, `CatA_Climb.png`, `CatA_Hang.png`, `CatA_Walk.png`.
**Why:** side view and back-view climb have nothing between them.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). These are animation frames for its hero, a lean black cat. From the attached character sheet and animation strip copy this exact cat: the same body, proportions, head, long S-curved tail and painting technique. Its coat is near-black, a deep warm brown (around #1E1410), short soft fur painted with fine directional strokes so the form always reads, never a flat black silhouette. Its eyes are amber-orange (around #E8942A), the insides of its ears are dusty pink, and a thin warm rim of sunlit gold runs along its back, ears and tail. It is elegant, athletic and real, a feline with true weight, balance and muscle, never a cartoon mascot. Frames turn between the side view and the back view as described.

Paint 4 animation frames in one row, left to right, evenly spaced. The cat turns between the side view and the back view of the attached Climb strip, as if on a vertical vine at the centre of its body. Do not paint any vine. Frames 1 and 2 are a grab: rearing up from the side, then turning its back to us, fore paws reaching up to grip. Frames 3 and 4 are a release: turning from the back view to the side, then dropping away with its paws open.

Every frame shows the SAME cat at the SAME size, drawn with the same detail and the same light. Strict side view, orthographic: no perspective and no 3/4 angle unless a frame above asks for it. Exactly four legs and one tail in every frame, and the whole cat, ears to tail tip, is inside its own space with clear empty space around it, so no two frames touch and nothing is cut off. Nothing else is in the image: no ground, no shadow, no dust, no motion lines, no effects, no props, no numbers. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a flat game asset. The background is one flat pure magenta (#FF00FF), edge to edge. No text, no letters, no other characters.
```

**Reject if:** a vine or rope is painted; frames 2 and 3 don't match Climb's back view (and the general list above).

### C-15 · Climb down (redraw) (8 frames)

**Save as:** `Art_Source/CatA/C-15_climb_down_v1.png`, landscape 3:2, magenta background. **Attach:** CS, `CatA_Climb.png`, `CatA_Hang.png`.
**Why:** the old clip had vine painted through the body, so it was cut apart.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). These are animation frames for its hero, a lean black cat. From the attached character sheet and animation strip copy this exact cat: the same body, proportions, head, long S-curved tail and painting technique. Its coat is near-black, a deep warm brown (around #1E1410), short soft fur painted with fine directional strokes so the form always reads, never a flat black silhouette. Its eyes are amber-orange (around #E8942A), the insides of its ears are dusty pink, and a thin warm rim of sunlit gold runs along its back, ears and tail. It is elegant, athletic and real, a feline with true weight, balance and muscle, never a cartoon mascot. The cat is seen from behind, as in the attached Climb strip.

Paint 8 animation frames in rows of four, reading left to right, top to bottom, evenly spaced. They are a seamless loop of the cat climbing DOWN a vertical vine, seen from behind, tail hanging down. Do not paint any vine, but leave the body whole, with paws gripping an invisible line down its centre. Each frame moves one paw: left fore, right hind, right fore, left hind, in a steady rhythm, with the body shifting between them, head turned a little to look down. Frame 8 leads back into frame 1.

Every frame shows the SAME cat at the SAME size, drawn with the same detail and the same light. Strict side view, orthographic: no perspective and no 3/4 angle unless a frame above asks for it. Exactly four legs and one tail in every frame, and the whole cat, ears to tail tip, is inside its own space with clear empty space around it, so no two frames touch and nothing is cut off. Nothing else is in the image: no ground, no shadow, no dust, no motion lines, no effects, no props, no numbers. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a flat game asset. The background is one flat pure magenta (#FF00FF), edge to edge. No text, no letters, no other characters.
```

**Reject if:** a vine is painted or body parts are missing; the loop can't close (and the general list above).

### C-16 · Vine dismounts (3 frames)

**Save as:** `Art_Source/CatA/C-16_vine_dismount_v1.png`, landscape 3:2, magenta background. **Attach:** CS, `CatA_Climb.png`, `CatA_Idle.png`.
**Why:** climbing ends in a pop to Idle (top or foot of a vine).

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). These are animation frames for its hero, a lean black cat. From the attached character sheet and animation strip copy this exact cat: the same body, proportions, head, long S-curved tail and painting technique. Its coat is near-black, a deep warm brown (around #1E1410), short soft fur painted with fine directional strokes so the form always reads, never a flat black silhouette. Its eyes are amber-orange (around #E8942A), the insides of its ears are dusty pink, and a thin warm rim of sunlit gold runs along its back, ears and tail. It is elegant, athletic and real, a feline with true weight, balance and muscle, never a cartoon mascot. It turns from the back view to the side view, facing right.

Paint 3 animation frames in one row, left to right, evenly spaced. The cat steps off a vertical vine at its foot onto the ground. Do not paint any vine. Frame 1: back view, hind paws reaching down to touch the floor. Frame 2: turning toward the side, paws releasing. Frame 3: side view facing right, standing exactly as in the first frame of the attached Idle strip. Across all frames the paws meet one invisible level ground line.

Every frame shows the SAME cat at the SAME size, drawn with the same detail and the same light. Strict side view, orthographic: no perspective and no 3/4 angle unless a frame above asks for it. Exactly four legs and one tail in every frame, and the whole cat, ears to tail tip, is inside its own space with clear empty space around it, so no two frames touch and nothing is cut off. Nothing else is in the image: no ground, no shadow, no dust, no motion lines, no effects, no props, no numbers. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a flat game asset. The background is one flat pure magenta (#FF00FF), edge to edge. No text, no letters, no other characters.
```

**Reject if:** a vine is painted; hind paws sink below the ground line; frame 3 doesn't match Idle (and the general list above).

### C-17 · Hang from the hands (3 frames)

**Save as:** `Art_Source/CatA/C-17_hang_v1.png`, landscape 3:2, magenta background. **Attach:** CS, `CatA_Hang.png`, `CatA_Climb.png`.
**Why:** grabbing a vine's end, the climb cycle steps on air, and the old Hang has stem residue on it.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). These are animation frames for its hero, a lean black cat. From the attached character sheet and animation strip copy this exact cat: the same body, proportions, head, long S-curved tail and painting technique. Its coat is near-black, a deep warm brown (around #1E1410), short soft fur painted with fine directional strokes so the form always reads, never a flat black silhouette. Its eyes are amber-orange (around #E8942A), the insides of its ears are dusty pink, and a thin warm rim of sunlit gold runs along its back, ears and tail. It is elegant, athletic and real, a feline with true weight, balance and muscle, never a cartoon mascot. The cat is seen from behind.

Paint 3 animation frames in one row, left to right, evenly spaced. They are a short loop of the cat hanging from a high grip by its fore paws only, seen from behind. Its fore legs reach straight up to one invisible point, its body hangs long and heavy, hind legs dangling free, tail swaying. The hind legs and tail swing slightly between frames, and frame 3 leads back into frame 1. Do not paint any vine.

Every frame shows the SAME cat at the SAME size, drawn with the same detail and the same light. Strict side view, orthographic: no perspective and no 3/4 angle unless a frame above asks for it. Exactly four legs and one tail in every frame, and the whole cat, ears to tail tip, is inside its own space with clear empty space around it, so no two frames touch and nothing is cut off. Nothing else is in the image: no ground, no shadow, no dust, no motion lines, no effects, no props, no numbers. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a flat game asset. The background is one flat pure magenta (#FF00FF), edge to edge. No text, no letters, no other characters.
```

**Reject if:** a vine is painted; hind legs touch anything (and the general list above).

### C-18 · Leap push-off (2 frames)

**Save as:** `Art_Source/CatA/C-18_leap_v1.png`, landscape 3:2, magenta background. **Attach:** CS, `CatA_Leap.png`, `CatA_Hang.png`.
**Why:** the leap snaps from a vertical hang to a stretched pose.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). These are animation frames for its hero, a lean black cat. From the attached character sheet and animation strip copy this exact cat: the same body, proportions, head, long S-curved tail and painting technique. Its coat is near-black, a deep warm brown (around #1E1410), short soft fur painted with fine directional strokes so the form always reads, never a flat black silhouette. Its eyes are amber-orange (around #E8942A), the insides of its ears are dusty pink, and a thin warm rim of sunlit gold runs along its back, ears and tail. It is elegant, athletic and real, a feline with true weight, balance and muscle, never a cartoon mascot. It turns from the back view toward the right.

Paint 2 animation frames in one row, left to right, evenly spaced. They take the cat from clinging to a vertical vine into a leap off it to the right. Do not paint any vine. Frame 1: turning from the hang toward the side, hind legs bunched as if braced against the vine, fore legs releasing. Frame 2: springing away, hind legs driving, body opening out, matching the second frame of the attached Leap strip.

Every frame shows the SAME cat at the SAME size, drawn with the same detail and the same light. Strict side view, orthographic: no perspective and no 3/4 angle unless a frame above asks for it. Exactly four legs and one tail in every frame, and the whole cat, ears to tail tip, is inside its own space with clear empty space around it, so no two frames touch and nothing is cut off. Nothing else is in the image: no ground, no shadow, no dust, no motion lines, no effects, no props, no numbers. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a flat game asset. The background is one flat pure magenta (#FF00FF), edge to edge. No text, no letters, no other characters.
```

**Reject if:** a vine is painted; the cat is diving head down (and the general list above).

### C-19 · Climb key poses (6 frames)

**Save as:** `Art_Source/CatA/C-19_climb_keys_v1.png`, landscape 3:2, magenta background. **Attach:** CS, `CatA_Climb.png`.
**Why:** at full speed the 25-frame climb reads as a blur.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). These are animation frames for its hero, a lean black cat. From the attached character sheet and animation strip copy this exact cat: the same body, proportions, head, long S-curved tail and painting technique. Its coat is near-black, a deep warm brown (around #1E1410), short soft fur painted with fine directional strokes so the form always reads, never a flat black silhouette. Its eyes are amber-orange (around #E8942A), the insides of its ears are dusty pink, and a thin warm rim of sunlit gold runs along its back, ears and tail. It is elegant, athletic and real, a feline with true weight, balance and muscle, never a cartoon mascot. The cat is seen from behind.

Paint 6 animation frames in rows of four, reading left to right, top to bottom, evenly spaced. They are a fast loop of the cat climbing UP a vertical vine, seen from behind, and each pose must read clearly on its own at phone size. Do not paint any vine. Big bold reaches: the left fore leg stretched high with the right hind leg driving, then a gathered pose, then the right fore leg stretched high with the left hind leg driving, then a gathered pose, with two in-betweens. Frame 6 leads back into frame 1.

Every frame shows the SAME cat at the SAME size, drawn with the same detail and the same light. Strict side view, orthographic: no perspective and no 3/4 angle unless a frame above asks for it. Exactly four legs and one tail in every frame, and the whole cat, ears to tail tip, is inside its own space with clear empty space around it, so no two frames touch and nothing is cut off. Nothing else is in the image: no ground, no shadow, no dust, no motion lines, no effects, no props, no numbers. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a flat game asset. The background is one flat pure magenta (#FF00FF), edge to edge. No text, no letters, no other characters.
```

**Reject if:** a vine is painted; the poses are too alike to read small (and the general list above).

### C-20 · Stand up from the sit (3 frames)

**Save as:** `Art_Source/CatA/C-20_stand_up_v1.png`, landscape 3:2, magenta background. **Attach:** CS, `CatA_IdleSit.png`, `CatA_Walk.png`.
**Why:** any input cuts straight from sitting to walking.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). These are animation frames for its hero, a lean black cat. From the attached character sheet and animation strip copy this exact cat: the same body, proportions, head, long S-curved tail and painting technique. Its coat is near-black, a deep warm brown (around #1E1410), short soft fur painted with fine directional strokes so the form always reads, never a flat black silhouette. Its eyes are amber-orange (around #E8942A), the insides of its ears are dusty pink, and a thin warm rim of sunlit gold runs along its back, ears and tail. It is elegant, athletic and real, a feline with true weight, balance and muscle, never a cartoon mascot. The cat faces RIGHT in every frame.

Paint 3 animation frames in one row, left to right, evenly spaced. They take the cat from the seated pose in the attached Sit strip to walking. Frame 1: haunches lifting, front legs straightening. Frame 2: rising onto all fours, back level, tail lifting. Frame 3: the first frame of the attached Walk strip. Across all frames the paws meet one invisible level ground line.

Every frame shows the SAME cat at the SAME size, drawn with the same detail and the same light. Strict side view, orthographic: no perspective and no 3/4 angle unless a frame above asks for it. Exactly four legs and one tail in every frame, and the whole cat, ears to tail tip, is inside its own space with clear empty space around it, so no two frames touch and nothing is cut off. Nothing else is in the image: no ground, no shadow, no dust, no motion lines, no effects, no props, no numbers. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a flat game asset. The background is one flat pure magenta (#FF00FF), edge to edge. No text, no letters, no other characters.
```

**Reject if:** frame 3 doesn't match Walk (and the general list above).

### C-21 · Death by arrow (redraw) (5 frames)

**Save as:** `Art_Source/CatA/C-21_death_arrow_v1.png`, landscape 3:2, magenta background. **Attach:** CS, `CatA_Death_Arrow.png`, `CatA_Fall.png`.
**Why:** the cat hangs in mid-air, then is suddenly on the floor.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). These are animation frames for its hero, a lean black cat. From the attached character sheet and animation strip copy this exact cat: the same body, proportions, head, long S-curved tail and painting technique. Its coat is near-black, a deep warm brown (around #1E1410), short soft fur painted with fine directional strokes so the form always reads, never a flat black silhouette. Its eyes are amber-orange (around #E8942A), the insides of its ears are dusty pink, and a thin warm rim of sunlit gold runs along its back, ears and tail. It is elegant, athletic and real, a feline with true weight, balance and muscle, never a cartoon mascot. The cat faces RIGHT in every frame.

Paint 5 animation frames in rows of four, reading left to right, top to bottom, evenly spaced. They are a cat struck in the side and knocked down, with no arrow and no wound shown. Frame 1: jolted upright, mouth open, eyes wide, fur bristling. Frame 2: thrown backward off its paws. Frame 3: falling sideways through the air, legs loose. Frame 4: hitting the ground on its side, tail flung up. Frame 5: lying still, one paw out, eyes shut. It is comic and dramatic, not gory, with no blood. Across all frames the paws meet one invisible level ground line.

Every frame shows the SAME cat at the SAME size, drawn with the same detail and the same light. Strict side view, orthographic: no perspective and no 3/4 angle unless a frame above asks for it. Exactly four legs and one tail in every frame, and the whole cat, ears to tail tip, is inside its own space with clear empty space around it, so no two frames touch and nothing is cut off. Nothing else is in the image: no ground, no shadow, no dust, no motion lines, no effects, no props, no numbers. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a flat game asset. The background is one flat pure magenta (#FF00FF), edge to edge. No text, no letters, no other characters.
```

**Reject if:** there's blood or a wound; frames 3 and 4 don't connect (and the general list above).

### C-22 · Death on spikes (in-betweens) (3 frames)

**Save as:** `Art_Source/CatA/C-22_death_spiked_v1.png`, landscape 3:2, magenta background. **Attach:** CS, `CatA_Death_Spiked.png`.
**Why:** it goes from upright to lying in one frame.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). These are animation frames for its hero, a lean black cat. From the attached character sheet and animation strip copy this exact cat: the same body, proportions, head, long S-curved tail and painting technique. Its coat is near-black, a deep warm brown (around #1E1410), short soft fur painted with fine directional strokes so the form always reads, never a flat black silhouette. Its eyes are amber-orange (around #E8942A), the insides of its ears are dusty pink, and a thin warm rim of sunlit gold runs along its back, ears and tail. It is elegant, athletic and real, a feline with true weight, balance and muscle, never a cartoon mascot. The cat faces RIGHT in every frame.

Paint 3 animation frames in one row, left to right, evenly spaced. They fit between the last upright frame and the lying frame of the attached strip. Frame 1: the cat rearing up on its hind legs, stiff with shock, starting to tip. Frame 2: toppling sideways, legs loosening. Frame 3: nearly down, one last paw reaching, tail dropping. Comic and dramatic, with no blood and no spikes painted. Across all frames the paws meet one invisible level ground line.

Every frame shows the SAME cat at the SAME size, drawn with the same detail and the same light. Strict side view, orthographic: no perspective and no 3/4 angle unless a frame above asks for it. Exactly four legs and one tail in every frame, and the whole cat, ears to tail tip, is inside its own space with clear empty space around it, so no two frames touch and nothing is cut off. Nothing else is in the image: no ground, no shadow, no dust, no motion lines, no effects, no props, no numbers. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a flat game asset. The background is one flat pure magenta (#FF00FF), edge to edge. No text, no letters, no other characters.
```

**Reject if:** there's blood; spikes are painted (and the general list above).

### C-23 · Crushed from the side (4 frames)

**Save as:** `Art_Source/CatA/C-23_death_crushed_side_v1.png`, landscape 3:2, magenta background. **Attach:** CS, `CatA_Death_Crushed.png`.
**Why:** the crusher closes from the side, but the art is a pancake from above (optional).

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). These are animation frames for its hero, a lean black cat. From the attached character sheet and animation strip copy this exact cat: the same body, proportions, head, long S-curved tail and painting technique. Its coat is near-black, a deep warm brown (around #1E1410), short soft fur painted with fine directional strokes so the form always reads, never a flat black silhouette. Its eyes are amber-orange (around #E8942A), the insides of its ears are dusty pink, and a thin warm rim of sunlit gold runs along its back, ears and tail. It is elegant, athletic and real, a feline with true weight, balance and muscle, never a cartoon mascot. The cat faces RIGHT in every frame.

Paint 4 animation frames in one row, left to right, evenly spaced. They are a cat squeezed from left and right by two invisible walls. Frame 1: startled, standing. Frame 2: its body pressed narrower and taller, eyes bulging, fur puffed up, tail straight up. Frame 3: squashed into a tall thin shape. Frame 4: slumped and dazed, sliding down. Comic, in the spirit of a classic cartoon gag but painted with full realism, with no blood. Across all frames the paws meet one invisible level ground line.

Every frame shows the SAME cat at the SAME size, drawn with the same detail and the same light. Strict side view, orthographic: no perspective and no 3/4 angle unless a frame above asks for it. Exactly four legs and one tail in every frame, and the whole cat, ears to tail tip, is inside its own space with clear empty space around it, so no two frames touch and nothing is cut off. Nothing else is in the image: no ground, no shadow, no dust, no motion lines, no effects, no props, no numbers. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a flat game asset. The background is one flat pure magenta (#FF00FF), edge to edge. No text, no letters, no other characters.
```

**Reject if:** there's blood; it looks like a different cartoon cat (and the general list above).

### C-24 · Door celebration (6 frames)

**Save as:** `Art_Source/CatA/C-24_door_v1.png`, landscape 3:2, magenta background. **Attach:** CS, `CatA_Door.png`, `CatA_Idle.png`.
**Why:** it reads as pawing at the door, not winning.

```text
I'm making a premium, hand-painted 2D side-view platformer for phones (a paid game: the art must be rich, crafted and painterly, never cheap). These are animation frames for its hero, a lean black cat. From the attached character sheet and animation strip copy this exact cat: the same body, proportions, head, long S-curved tail and painting technique. Its coat is near-black, a deep warm brown (around #1E1410), short soft fur painted with fine directional strokes so the form always reads, never a flat black silhouette. Its eyes are amber-orange (around #E8942A), the insides of its ears are dusty pink, and a thin warm rim of sunlit gold runs along its back, ears and tail. It is elegant, athletic and real, a feline with true weight, balance and muscle, never a cartoon mascot. The cat faces RIGHT in every frame.

Paint 6 animation frames in rows of four, reading left to right, top to bottom, evenly spaced. They are the cat's victory at the end of a level, proud and a little smug: it has beaten everything the room threw at it. Frames 1 and 2: stopping and straightening up, chest out, tail rising into a tall S. Frames 3 and 4: a slow satisfied blink with head held high, tail tip flicking one way, then the other. Frames 5 and 6: one neat paw lifted, a confident step forward. All four paws stay on the ground except that one lifted paw. Across all frames the paws meet one invisible level ground line.

Every frame shows the SAME cat at the SAME size, drawn with the same detail and the same light. Strict side view, orthographic: no perspective and no 3/4 angle unless a frame above asks for it. Exactly four legs and one tail in every frame, and the whole cat, ears to tail tip, is inside its own space with clear empty space around it, so no two frames touch and nothing is cut off. Nothing else is in the image: no ground, no shadow, no dust, no motion lines, no effects, no props, no numbers. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector icon, not a flat game asset. The background is one flat pure magenta (#FF00FF), edge to edge. No text, no letters, no other characters.
```

**Reject if:** it stands on its hind legs; it paws at anything (and the general list above).

---

## Store art

**Reference:** **R1** is `Docs/Art/Reference/styleframe_A_v1.png`, and **CS** is the character sheet above.

**Specs checked 2026-11-01** (check the live Play Console page before submitting):

| Asset | Spec | Made by |
|---|---|---|
| Adaptive icon | Foreground and background layers, 108 × 108 dp each (Claude Code exports 432 px and up). Logo inside the central 66 × 66 dp | S-01 + S-02 |
| Play icon | 512 × 512, 32-bit PNG, under 1024 KB, a full square with no rounded corners or shadow: Google adds them. No text or badges | Claude Code composes it from S-01 + S-02 |
| Feature graphic | 1024 × 500, JPEG or 24-bit PNG, no alpha. No device frames, no small text | S-03 |
| Screenshots | 1920 × 1080 or larger, long side at most twice the short side, up to 8. Games need at least 3 to be eligible for recommendations | Captured from the game once the art is final, no prompt |

### S-01 · App icon, foreground (the cat)

**Save as:** `Art_Source/Store/S-01_icon_foreground_v1.png`, square, magenta background. **Attach:** CS, R1.

```text
I'm making the store art for a premium, hand-painted 2D platformer for phones, a paid game about a black cat escaping deadly traps through two worlds. This must look like prestige key art from an award-winning indie game: rich, crafted, painterly and confident, never a casual mobile icon. Match the attached character sheet's cat exactly and the attached style image's painting technique.

Paint ONE app icon subject, seen straight on: the head of the black cat, and nothing else, centred, facing the viewer, ears tall and alert. It fills the central 60 percent of the square, leaving an empty margin all the way round, because the corners get cut off. The face is a bold, simple shape that reads at 48 pixels: a clean silhouette, two huge amber-orange eyes (around #E8942A) with narrow vertical pupils, glowing with intelligence and a little mischief, and a tiny crimson glint in each pupil, the reflection of a trap it has just seen. Its coat is near-black, a deep warm brown, painted in fine soft fur. Its left side is rim-lit in warm sunset gold and its right side in cold cyan: the two worlds of the game meeting on its face. It looks clever, cool and unbothered, never cute or kawaii.

Perfectly straight on and symmetrical in pose. Big simple shapes that read at very small size, with rich painted fur up close. It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a cartoon, not a vector logo, not a mascot. The background is one flat pure magenta (#FF00FF), edge to edge, with no shadow on it. No body, no text, no letters, no badge, no border.
```

**Reject if:** it reads as cute; the ears or face leave the central 60 percent; the eyes aren't the first thing you see; it stops reading when shrunk to 48 pixels; the gold and cyan rim lights are missing.

### S-02 · App icon, background

**Save as:** `Art_Source/Store/S-02_icon_background_v1.png`, square, fills the whole image. **Attach:** R1.

```text
I'm making the store art for a premium, hand-painted 2D platformer for phones, a paid game about a black cat escaping deadly traps through two worlds. This must look like prestige key art from an award-winning indie game: rich, crafted, painterly and confident, never a casual mobile icon. Match the attached character sheet's cat exactly and the attached style image's painting technique.

Paint ONE simple, luminous background that fills the whole square, edge to edge, for an app icon, with a black cat's head to be placed over its centre. The left side is a warm golden sunset haze (around #F2B45E), and the right side is a deep cold void of teal and navy with a few fine stars (around #0C2B3A). They meet in a soft diagonal band of pale light through the centre. Faint hints only: a ghost of ancient ruins low on the left, a ghost of a crystal shard low on the right. It is soft and low in detail, with no sharp edges and nothing to compete with what sits on top.

It must look like a concept artist's painting for a premium game: not a photo, not a 3D render, not a vector gradient. No cat, no characters, no text, no letters, no border.
```

**Reject if:** it's busy at 48 pixels; there's a clear object in the centre; it looks like a plain digital gradient.

### S-03 · Feature graphic

**Save as:** `Art_Source/Store/S-03_feature_graphic_v1.png`, landscape, the widest your tool gives. **Attach:** CS, R1, `Art_Source/Traps_ChatGPT/GT-01_spinner_arm_v2.png`, `Art_Source/Traps_ChatGPT/GT-04_spiked_underside_v1.png`.
**In game:** none: it's the banner at the top of your Play listing. It's cropped to 1024 × 500, so everything important stays in the middle band. Claude Code crops it, and adds the title from the title screen's own type if you want it.

```text
I'm making the store art for a premium, hand-painted 2D platformer for phones, a paid game about a black cat escaping deadly traps through two worlds. This must look like prestige key art from an award-winning indie game: rich, crafted, painterly and confident, never a casual mobile icon. Match the attached character sheet's cat exactly and the attached style image's painting technique.

Paint ONE cinematic wide banner: the black cat caught at the peak of a desperate leap across the exact centre, seen from the side, facing right, body at full stretch, legs reaching, long tail streaming behind, amber eyes fixed on where it must land. Behind it the world splits in two. The left half is the GOLDEN RUINS: a sunlit temple city of weathered sandstone and old bronze, floating among golden clouds, with moss and vines and a low warm sun. The right half is the OBSIDIAN VOID: black volcanic glass machines threaded with thin cyan light, floating in a starry teal void. The two worlds meet behind the cat in a soft diagonal seam of pale light. Around it, the danger: a thick crimson laser beam with a white-hot core tears through the air just beneath its belly, and the battered iron spikes and toothed blade from the attached images, their tips crimson, rise out of the shadows below. The cat is the darkest, sharpest shape in the picture, rim-lit gold on one side and cyan on the other. Keep the cat and the beam in the middle third of the image's height, with only atmosphere above and below, because the top and bottom will be cropped away. It feels fast, tense and cool, one frozen instant before everything goes wrong.

Strict side view of the cat. It must look like a concept artist's key art for a premium game: not a photo, not a 3D render, not a cartoon, not a vector illustration, not a collage. Big bold shapes that still read when small. No text, no letters, no logo, no phones or devices, no badges, no other characters.
```

**Reject if:** the cat isn't the clear focus; it looks like a different cat; the cat or beam is outside the middle third; the two worlds don't both read; there's any text; it looks like a mobile ad.
