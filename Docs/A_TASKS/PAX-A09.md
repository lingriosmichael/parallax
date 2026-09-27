# PAX-A09 · UI art: title, level select, in-level UI, font

**Status:** Approved (Phase E, after PAX-V06). Updated 2026-09-27 to the premium target.
**Depends on:** PAX-053, PAX-054, PAX-A06 (Reality A look)
**Decisions:** Vision §7–8 (serious, surreal, warm; not childish), D-094.
**Target:** `DesignImages/15_premium-mobile-ui.png` and `Docs/Art/LOOK_AND_FEEL.md`: the world is the screen; minimal thin white line icons; circular translucent buttons; a cat portrait with a thin progress line top-left; pause top-right; no heavy panels during play.

## Scope

1. Title screen: a live, slowly panning parallax scene of Reality A (the V03 layer stack, not a static image), the "PARALLAX" wordmark in the key-art letter-spacing, the cat idling on a ledge.
2. Level select: cell frame (locked / unlocked / completed), lock icon, death-count styling,
   scroll background.
3. In-level: the virtual stick styled as a thin translucent ring and knob (it stays a floating stick, D-021/D-049), the jump button as a circular translucent button with a thin chevron, pause top-right; cat portrait + level progress line (sections from D-091) top-left; pause panel, level-complete panel (per-section deaths optional), settings (sliders, toggles incl. "reduce motion" from PAX-V06).
4. One font family with a licence that allows embedding in a commercial app (record the licence
   file in `Docs/Art/Licences/`).
5. Every image as a 9-slice or fixed-size sprite with defined size/PPU in the manifest.

## Code

Swapping placeholder UI for sprites runs through the existing setup menus (extend them in a
small engineering ticket, not by hand-editing scenes).

## Acceptance (Editor)

Device Simulator at 20:9 and 16:9 and a notched profile: all text readable, buttons ≥ 48 dp
equivalent, nothing under the cutout. Touch comfort is **unverified (Phase H)**.
