# PAX-026a — Seat release invariant (soft-lock fix) + missing EditMode tests

```text
TASK: PAX-026a
TITLE: Seat release invariant — no path may leave a cat or a station stuck
PHASE / GATE: Phase 5 (patch to PAX-026) / Gate 4 track
TYPE: Patch. Small. Editor only.
```

## OBJECTIVE

Close the seat soft-lock from PAX-026: a cat must never remain seated, and a Control Station must never remain occupied, once the occupant can no longer stand up by itself. Add the EditMode tests PAX-026 should have shipped for seat state and command filtering.

## CONTEXT

- `02_ARCHITECTURE.md` §5.3 Control Station, including the "As built (PAX-026)" note.
- `07_DECISIONS.md` D-029 (seat via interact, `SeatCommandFilter` in `LocalHumanDriver`, seat released on Deactivate, publish on change only, never on sit-down).
- `07_DECISIONS.md` D-030 and `02_ARCHITECTURE.md` checkpoints note: respawn goes through `CatRespawn.RespawnAt`; `FallResetVolume` ignores Echo-driven cats.
- D-028 / Echo hold-final-state rule: an Echo cat holds its last state after replay ends.

**The invariant this ticket enforces:**

> A seat is occupied only while its occupant is a live, active, driver-controlled cat that is physically at that station and can issue *interact* to stand. Every path that breaks one of those conditions releases the seat in the same frame.

Today only two release paths exist: interact-to-stand and driver Deactivate. The soft-lock lives in the paths that bypass both.

## CLAUDE — CODE

### Step 0 — Audit first, then fix (report findings in the deliverable)

Before changing anything, grep every place a cat's seat state can be entered or exited and every place that moves, disables, or re-drives a cat. For each path in the table below, report **currently releases: yes / no / n/a** with the file and line. Fix every "no".

| # | Path | Expected behaviour |
|---|------|--------------------|
| P1 | Interact while seated | Stand up (existing; keep) |
| P2 | `LocalHumanDriver` Deactivate (solo switch) | Release (existing; keep) |
| P3 | `CatRespawn.RespawnAt` (fall, checkpoint, debug reset) | Release **before** teleport |
| P4 | Cat GameObject/component `OnDisable` | Release |
| P5 | Station `OnDisable` / `OnDestroy` while occupied | Release occupant, clear occupant ref |
| P6 | Cat becomes Echo-driven (`EchoReplay` takes over) while seated | Release the live seat; Echo does not inherit a live seat |
| P7 | Echo replay ends / hold-final-state with Echo cat at a station | Echo never holds a live seat; station stays free for the live cat |
| P8 | Sit and stand requested in the same physics tick (interact edge seen twice) | Exactly one transition per interact press |
| P9 | Second cat tries to sit at an occupied station | Rejected cleanly; first occupant unaffected |

If the audit finds the actual reported soft-lock repro in a path not listed, add it as P10 and fix it.

### Implementation rules

- **One release method, called from everywhere.** All paths call the same `Release()` (or equivalent) on the seat owner. No path clears fields directly.
- `Release()` is **idempotent**: calling it on an unseated cat or empty station is a no-op, never an error or a warning.
- Release clears **both sides** in one call: the cat's seat reference and the station's occupant reference. No half-released state may be observable.
- Release **does not publish** a control sample (D-029: publish only on change, never on sit or stand). The other cat's gravity stays where it was.
- `SeatCommandFilter` must read seat state fresh each tick (pull), so a release takes effect on the next `FixedUpdate` without any extra signal.
- `OnDisable` on cat and station clears all seat-derived state (per the project rule: `OnDisable` must be comprehensive).
- If pure seat logic is currently tangled inside MonoBehaviours, extract a small pure class (e.g. `SeatState` in `Parallax.Core` or `Parallax.Gameplay` with no `UnityEngine` object dependencies) so it can be tested in EditMode. Keep the extraction minimal.

### EditMode tests (`Parallax.Tests.EditMode`) — the missing coverage

Required, one test per bullet at minimum:

1. Sit on an empty station → cat seated, station occupied by that cat.
2. Interact while seated → both sides cleared.
3. `Release()` on an unseated cat → no-op, no exception.
4. `Release()` called twice → second call is a no-op.
5. Second cat sits at an occupied station → rejected; first occupant unchanged.
6. Release clears both sides atomically (no state where one side still references the other).
7. `SeatCommandFilter`: while seated, move → zero and jump → false; interact passes through.
8. `SeatCommandFilter`: after release, the same input passes through unfiltered on the next read.
9. Sit and stand in the same tick (double interact edge) → exactly one transition.
10. Release emits no control publish (assert against a fake publisher / counter).

Paths P3–P7 depend on MonoBehaviour lifecycle and are covered by the manual acceptance below; do not fake Unity lifecycle in EditMode.

### Allowed files

- The seat, station, filter, driver, respawn and Echo-replay files identified in Step 0.
- New pure seat-state class if extraction is needed.
- New test file(s) under `Tests/EditMode/`.
- `02_ARCHITECTURE.md` §5.3 "As built" note (one-paragraph update).

## YOU — UNITY EDITOR

No new wiring expected. If Claude Code's audit shows a component needs a new reference, it must be added through the existing idempotent `PARALLAX/Setup` menu script, never by hand.

## DO NOT

- Do not add tilt (PAX-025), new station types, or new control mappings.
- Do not change D-029 publish semantics.
- Do not change respawn, checkpoint, or Echo behaviour beyond calling `Release()`.
- Do not add UI, labels, or visuals (that is PAX-V01).
- Do not touch networking code or anything that would reference Photon.

## REQUIREMENTS

- Every path P1–P9 (plus any P10) releases correctly per the table.
- A single idempotent release method is the only way seat state is cleared.
- Release never publishes a control sample.
- All existing EditMode tests stay green; the 10 new tests are green.
- No new warnings or errors in the console during acceptance.

## ACCEPTANCE TEST — Editor only

Run in `Sandbox_Realities`, Play Mode, 0 ms latency unless stated.

1. **EditMode suite:** Test Runner → all green, count = previous 110 + new tests.
2. **P1:** A sits at the station, turns the dial, stands via interact → A moves normally.
3. **P2:** A seated → solo-switch to B → switch back to A → A is standing and moves; station is free.
4. **P3:** A seated → trigger A's respawn (debug reset, or a fall volume if reachable) → A appears at spawn, standing, movable; station free; B's gravity unchanged.
5. **P4:** A seated → disable A's GameObject in the Hierarchy → re-enable → A standing and movable; station free.
6. **P5:** A seated → disable the station GameObject → A standing and movable immediately → re-enable station → A can sit again.
7. **P6/P7:** Record an Echo in which A sits at the station and turns the dial → play the Echo to the end (hold final state) → with the live cat, walk to the station and sit → it works; the Echo cat is not blocking the seat. Dial choreography from the Echo still replays correctly.
8. **P8:** Tap interact rapidly at the station ~10 times → state always matches the last press; never stuck seated with movement dead.
9. **P9:** (Only if both cats can physically reach the same station in the sandbox; otherwise covered by EditMode test 5.)
10. **Gravity hold:** In steps 3–6, confirm B's gravity direction never changes at the moment of release.
11. **Latency:** Repeat step 2 and step 4 at 300 ms artificial latency → same results.

## DELIVERABLE (from Claude Code)

- Step 0 audit table filled in: path, released before fix (yes/no), file:line, fix applied.
- Changed files, with a short explanation per file.
- New test names and count.
- Known limitations (anything in P3–P7 not verifiable in the Editor).
- Review file: `{ git status; git --no-pager diff; cat <new files>; } > ~/Desktop/pax026a_review.txt 2>&1`

## DEFINITION OF DONE

- Universal DoD (§8).
- Audit table delivered and every "no" fixed.
- Acceptance steps 1–8, 10, 11 pass in the Editor (9 if applicable).
- `02_ARCHITECTURE.md` §5.3 "As built" note updated to list all release paths.
- Reviewed by the Architect via the review file, then committed as `PAX-026a: seat release invariant + EditMode tests`.
