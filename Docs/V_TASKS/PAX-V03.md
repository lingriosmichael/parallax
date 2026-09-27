# PAX-V03 · Look slice: one room at the target quality, and the performance budget

**Status:** Approved for implementation (after PAX-060). **Wait after Phase 1:** post the trace and stop.
**Phase 1 size: Full.** New rendering techniques; expensive to undo if the budget is wrong.
**Phase:** E (look and feel), first ticket. **Target:** `Docs/Art/LOOK_AND_FEEL.md` (read it first).
**Depends on:** PAX-060 committed. EditMode baseline = the total after PAX-060.
**Decisions:** D-020 (world-aligned camera), D-036 (no Animator), D-064 (validation at the end: this ticket asks for **one** early device check, see §6), D-071 (level camera, background placement), D-075 (8) (50 Hz judder), new **D-094** (visual production starts; the target is `LOOK_AND_FEEL.md`; supersedes D-015's "evokes, doesn't reproduce" and lifts CLAUDE.md's "no final art" for Reality A).

**Menus are the developer's job.** New art comes from the developer (ChatGPT, manual) or AutoSprite within the 20-credit cap (CLAUDE.md).

---

## 1. Why

Before 50 levels get dressed, one room has to look like `styleframe_A_v1.png` on a phone, and we need to know what that costs. Every later art ticket uses the techniques and the budget this ticket proves.

## 2. What to build

**The slice room:** L011 (Scaffold), which has a tall shaft, floors, walls and ceilings, spears and a pit. Its gameplay doesn't change at all.

1. **Layer stack** (LOOK_AND_FEEL §2): extend the existing `ParallaxLayer`/`ParallaxMath` setup from 3 background layers to layers 01, 02, 03, 06 and 07, with per-layer factors, **vertical** parallax for tall rooms (today `maxAbsCameraY` is 0), and placement baked per level (D-071).
2. **Platform look:** platforms, walls and ceilings drawn with a top cap, a fill, end caps and an underside with hanging vines, from tiling pieces (A12 makes the final set; this ticket may use the existing A02 pieces plus up to 3 new ones). Dressing layer 04b placed by a rule, not by hand.
3. **Light:** URP 2D Global Light (warm ambient), one sun key light (Freeform or Sprite light behind the scene), a rim light that follows the cat, and **normal maps** on platforms and the cat (secondary textures). Light shafts as additive sprites, not volumetrics.
4. **Atmosphere:** fog bands between layers 02/03 and 03/04 (sprites), a colour grade and gentle bloom through one URP Volume, sparse dust motes (one pooled particle system).
5. **Water:** a reflective water strip where the room has a pit bottom along its lower edge: a cheap reflection (a flipped, darkened, rippled copy via a shader or a second low-res camera; Phase 1 picks the cheaper) with a highlight line.
6. **50 Hz judder (D-075 (8)):** trap bodies (falling blocks, moving traps, spears, the retreating door) render smoothly on 60/120 Hz screens. Presentation-only interpolation; gameplay poses stay per tick. **Every route pin identical.**
7. **Budget:** measure the slice in the Editor (Profiler, Frame Debugger: draw calls, SetPass calls, overdraw, particle count, light count) and, with the developer's OK, on the Pixel 8a (§6). Write the budget into `LOOK_AND_FEEL.md` §7 ("per room: ≤ N draw calls, ≤ N lights, ≤ N particles, fps").

## 3. Phase 1 (Full, ≤ 150 lines; at most 6 questions)

1. The current rendering setup: URP 2D Renderer asset, lights in `_LevelTemplate`, sorting layers, `ParallaxLayer`, `EnvironmentArtSetup`. What changes, where (file:line)?
2. Normal maps: can the existing importer/setup attach secondary textures to the tiled platform sprites and the cat sheets without touching `.meta` by hand? If not, what developer step?
3. Water reflection: the two options (shader vs. second camera) with their cost; recommend one.
4. Judder: where does interpolation go (a presentation child that lerps between tick poses, or `Rigidbody2D.interpolation` on kinematic bodies)? Prove it can't change a route result.
5. Vertical parallax and the camera tell rule: the tell rule reads layer 04 only; confirm foreground layer 06 can't hide a tell (and if it can, the rule that prevents it).
6. Files, and whether any Unity package is needed (none may be installed without the developer's approval).

## 4. Rules and stop conditions

- **No gameplay change:** colliders, ticks, routes, validators untouched; every EditMode test and pin identical.
- Setup menus deliver every scene change; `_LevelTemplate` first, then `Rebuild All Levels` (D-070).
- A package install, a ProjectSettings change, or a shader that doesn't compile for Android/Vulkan+GLES3 → stop and report.
- Frozen co-op scenes untouched.

## 5. Tests

- Every existing test identical. New: parallax math (vertical factor, placement), the interpolation presenter (poses between ticks, exact on ticks), a guard that no presentation component writes a collider or `Rigidbody2D` pose.

## 6. YOU: Unity Editor (and one phone check)

1. Run the new setup menus on `_LevelTemplate`, then `Rebuild All Levels`; open `Level_011` and compare it side by side with `styleframe_A_v1.png`. Say what's missing.
2. **Recommended device check (10 minutes, D-064 exception for rendering cost only):** build to the Pixel 8a, play L011 for 5 minutes, report the fps overlay and whether the phone gets warm. Without it, the budget is an Editor estimate and the risk moves to Phase H.
3. Judder: play L011 and a falling-block level; blocks and spears should glide, not step.

## 7. Required output

`CLAUDE.md`'s list, plus: before/after screenshots of L011 at 16:9 and 20:9, the measured budget table, and D-094 as built.
