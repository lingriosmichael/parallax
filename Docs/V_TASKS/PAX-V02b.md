# PAX-V02b · Seat object art on its supporting surface

**Status:** Approved, not started
**Date:** 2026-09-21
**Lane:** Visual
**Depends on:** PAX-V02 (committed)
**Location:** `Docs/V_TASKS/PAX-V02b.md` — same lane folder as PAX-V02
**Scene:** `Sandbox_Realities`

---

## 0. Why

Found during PAX-A03/V02 Play verification: in Reality A the checkpoint banner sinks into the ground
and the control station floats above it. Pre-existing since PAX-A02 Stage 2c; the placeholder boxes
hid it until PAX-V02 turned them off.

Likely cause: object art is placed relative to each object's gameplay origin (its trigger), while
sprites use differing pivots. To be confirmed by measurement, not assumed.

Rule, same as the PAX-V02 foreground fix: **art is anchored to the geometry it rests on, not to a
gameplay origin.**

---

## 1. Scope

**In:** the local y position of `Art` children of static grounded objects, in both realities.

**Out — do not touch:**

- Object roots, triggers, colliders, layers, physics, gameplay positions. **Art moves; gameplay does not.**
- Art x positions, scale, sprites, import settings.
- The vine (`Vine_A` — hangs from above).
- `Gate_B` and `Elevator_B` — their art rides their own moving body and is correct relative to it.
- Anything PAX-V02 already changed, except where this ticket's placement logic lives beside it.

---

## 2. Allowed files

- Edit: `Assets/_Game/Editor/Setup/PaxV02Setup.cs` — add the seating step to the existing idempotent
  clean-up setup rather than creating a new menu.
- Create: `Assets/_Game/Tests/EditMode/ArtSeatingTests.cs` — only if the lowest-opaque-row measurement
  is written as a pure function (it should be).
- Modified by the setup menu, not by hand: `Sandbox_Realities.unity` and any affected prefab.

Anything else: stop and ask.

---

## 3. Stage 0 — inventory. Report, then continue unless a stop condition hits.

Both realities. Every static grounded object with an `Art` child: at minimum checkpoints, the control
station, the pressure plate and hazards.

One row each:

| Object path | Root world pos | `Art` local pos | Sprite pivot (normalized) | Pivot → lowest opaque pixel (world u) | Supporting surface top (world y) | **Gap: art bottom − surface top** (world u, and screen px at current camera) |

- Lowest opaque pixel: alpha > 25, measured from the texture. **Not** `sprite.bounds` — FullRect
  bounds include transparent margin.
- Supporting surface: the top of the nearest collider directly below the object's root, on the
  object's own reality layer.

**Stop and report instead of fixing if:**

- an object has no supporting surface within 2 units below it, or
- zeroing its gap would require moving anything other than its `Art` child.

---

## 4. Fix

In `PaxV02Setup.cs`, set each listed object's `Art` local y so its gap is 0. Leave x unchanged.
Idempotent: a second run makes no changes.

---

## 5. Acceptance

**Editor (Claude Code):**

1. Compiles — `refresh_unity` + `validate_script` + `read_console`, quote the result.
2. The §3 table reported **before and after**. After: every gap 0 within one texel.
3. Setup run twice; second run reports zero changes.
4. EditMode total ≥ 189 (plus any new `ArtSeatingTests`).
5. Diff contains no root, trigger, collider, layer or physics change.

**Play (developer), both realities:**

6. Checkpoint banners stand on the ground, not in it.
7. Control station stands on the ground, not above it.
8. Plate and hazards sit on their surfaces.

---

## 6. Definition of done

- Numbers, not verdicts. The before/after gap table is the proof.
- Enumerate which requirements of §3–§5 are implemented and which are not.
- Claude Code does not commit. Review file → Architect review → commit.
  `{ git status; git --no-pager diff; cat <new files>; } > ~/Desktop/paxV02b_review.txt 2>&1`
- If Unity disconnects: stop, report, no retries.
- `FallResetTest/HazardArt` (both realities) — kill-zone marker below the arena, not a grounded object. Positioned by its trigger.