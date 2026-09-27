# PAX-V07 · Wire the cat animation set

**Status:** Approved (after PAX-A08's clips exist). **Phase 1 size: Lite.**
**Depends on:** PAX-A08. **Decisions:** D-036 (pure `CatAnimStateMachine`, no Animator), D-034 (the presenter owns Visual scale and facing), D-089/D-092 (climb).

## What to build

- Append the new states to `CatAnimState` (never renumber) and extend the pure `CatAnimStateMachine`: run vs walk by stick magnitude, turn, take-off/rise/apex/fall by vertical speed, land vs hard land by fall distance, death (held through the hold), respawn, flip, climb/hang, leap off, launched (geyser), dizzy (inverted), door enter. Deterministic idle variants by an idle timer (no randomness, D-067).
- `CatSpriteImporter` gets the new sheets; `CatVisualPresenter` gets the clips; the interim climb pose goes.
- The seamless vine (A08 item 15) in `TrapKitSetup`.
- Setup menus wire the prefab; never a hand edit.

## Rules and tests

No collider, motor or route change: every pin identical. EditMode tests for every transition in the state machine (red-first).

## YOU

Play Trap Lab rooms 0–11: every transition reads, no pops, both gravities.
