# PAX-V07b · Fed-up reaction on respawn

**Status:** Draft 2026-09-29, **pending developer review**.
**Order:** after PAX-V07.
**Phase 1 size:** None (small and mechanical).
**Depends on:**
- PAX-V07: the Respawn state, `CatPresentationSignals`, the clip set.
- `RoomDeath.DeathsIn(room)`, the D-058 per-room counter.

**Decisions:**
- D-044: no lives, unlimited retries, a per-room death count.
- D-067: no randomness.
- D-041: death to control ≤ 0.75 s.

## What

After many deaths in the same room, the respawning cat shows a short fed-up reaction instead of the normal respawn.
It's a joke, and it never costs the player time.

## Trigger rule

- **When it plays:** on respawn, if the current room's death count is `n` and `n ≥ fedUpFirst` and
  `(n − fedUpFirst) % fedUpEvery == 0`. The defaults are 5 and 5, so the reaction plays on the 5th, 10th, 15th death
  and so on.
- **Where the values live:** both are in `CatVisualConfig`.
- **The count:** it comes from `RoomDeath.DeathsIn`, is per session, and is not persisted. How D-044's count is shown
  or persisted stays with the ticket that adds its UI.
- **The clip:** `CatA_FedUp.png`, a new A08 slot from `16c_fed_up`, frames 8–20 subsampled to about 6 frames, not a
  loop.

## Rules

- Visual only. Any movement input cancels the reaction on the same frame (the parity test from V07 is extended).
- It never plays during Death or Door, and never twice in a row for one death.

## Allowed files

- `CatAnimState.cs` (append `FedUp`).
- `CatAnimStateMachine.cs`, `CatAnimInput.cs`.
- `CatVisualConfig.cs`, `CatPresentationSignals.cs`.
- `CatVisualSetup.cs`.
- The `CatA_FedUp` slot in PAX-A08's manifest and sheet.
- `CatAnimStateMachineTests.cs`, `CatVisualOnlyParityTests.cs`.

## Tests

- It plays exactly on death counts 5, 10, 15 with the defaults, and never on 4 or 6.
- Movement input cancels it.
- It is never shown during Death.
- Parity: identical motion with and without it.

## YOU

In Trap Lab room 0, die 5 times. The 5th respawn shows the reaction, and pressing a direction ends it at once.
