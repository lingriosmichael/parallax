# PAX-V05 · Trap presentation: every trap animated, driven by game state

**Status:** Approved (after PAX-A13's art exists). **Wait after Phase 1:** post the trace and stop.
**Phase 1 size: Full** (touches every trap kind's visuals and the rewind/harness path).
**Depends on:** PAX-A13 (the art), PAX-A12 (host pieces), PAX-V03 (budget). **Decisions:** D-051/D-055 (tick-driven traps), D-057/D-079 (the 6-tick visible lead is measured on renderers), D-080 (fake platforms), D-091 (rewind), LOOK_AND_FEEL §3.

## 1. Why

Replace every greybox colour with the A13 art and animate each state, without touching gameplay, and keep every reveal measurable by the route validator.

## 2. What to build

1. **One presentation component per trap kind family**, e.g. `TrapFlipbook` (pure frame selection in Core: `(state, ticksSinceChange) → frame`), attached by the setup menus. It **reads** trap state and ticks; it never writes a collider, a body, a tick or a trap field.
2. **Reveal frames are tick-driven** (deterministic) so rewinds restore them exactly and the route harness sees them. Ambient loops (vent bubbling, orb glow, cloud drift) may run on render time but must not be a trap's first visible change.
3. **The lead stays measurable:** the first visible change of every betrayal must still happen at the same tick as today (or earlier). If art moves a reveal later, that's a stop.
4. **Disguise:** each disguised kind's idle is the host's pieces; the no-tell tests compare sprites, not colours.
5. **Death effects** per cause (A13), spawned from a pool at the kill, visible during the hold, cleared on reset/rewind.
6. **Particles:** debris, dust, splash, sparks from pooled systems inside the V03 budget.

## 3. Phase 1 (at most 6 questions)

1. Every trap's current visual code (colour writes, renderer toggles) with file:line; what the flipbook replaces.
2. How the route harness's "visible change" (sprite, colour, enable, pose) sees flipbook frames; does anything need a harness change?
3. Rewind (D-091): which presentation state must be restored, and how (derived from ticks vs. by value)?
4. Pooling for effects and particles within the budget.
5. The no-tell test extended to sprites (how to compare pre-reveal looks).
6. Files.

## 4. Rules and stop conditions

Every route pin, lead and window identical (or a lead grows, never shrinks). No gameplay change. Setup menus deliver scene changes; `Rebuild All Levels`. A reveal that becomes later or invisible → stop.

## 5. YOU: Unity Editor

Rerun the Trap Lab menu and `Rebuild All Levels`; play Trap Lab rooms 0–11 and five levels of your choice; for each trap kind say whether the reveal reads and whether it feels heavy enough.
