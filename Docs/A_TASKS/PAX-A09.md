# PAX-A09 · UI art: title, level select, in-level UI, font

**Status:** Draft. Starts after PAX-054 (all screens exist).
**Depends on:** PAX-053, PAX-054, PAX-A06 (Reality A look)
**Decisions:** Vision §7–8 (serious, surreal, warm; not childish)

## Scope

1. Title screen: key-art-based background (from `Docs/Art/keyart_north_star.png` direction),
   logo wordmark "PARALLAX".
2. Level select: cell frame (locked / unlocked / completed), lock icon, death-count styling,
   scroll background.
3. In-level: stick base and knob, jump, FLIP, pause buttons; pause panel; level-complete panel;
   settings sliders and toggle.
4. One font family with a licence that allows embedding in a commercial app (record the licence
   file in `Docs/Art/Licences/`).
5. Every image as a 9-slice or fixed-size sprite with defined size/PPU in the manifest.

## Code

Swapping placeholder UI for sprites runs through the existing setup menus (extend them in a
small engineering ticket, not by hand-editing scenes).

## Acceptance (Editor)

Device Simulator at 20:9 and 16:9 and a notched profile: all text readable, buttons ≥ 48 dp
equivalent, nothing under the cutout. Touch comfort is **unverified (Phase H)**.
