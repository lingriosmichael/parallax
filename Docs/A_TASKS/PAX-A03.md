# PAX-A03 · Amendment 1

**Date:** 2026-09-21
**Status:** Approved. Supersedes the named sections of PAX-A03; everything else stands.
**Reason:** §3.0 findings contradict §3.1 and §3.3. The stop was correct. The ticket was wrong.

---

## A1-0 · Verdict on the conflict

`airFrame` is a field of `CatVisualConfig`, not of the cat sprite component. The ticket said
otherwise. **Yes — "delete `airFrame`" means editing `Assets/_Game/Gameplay/Presentation/CatVisualConfig.cs`.**

`idleFrame` goes with it. Both are single-frame stand-ins for the states this ticket replaces with
clips. Leaving either behind leaves a second, contradictory source of truth for what the cat shows.

---

## A1-1 · §3.1 allowed files — amended

Replaces the §3.1 list:

- Create: `Assets/_Game/Core/Presentation/CatAnimState.cs`, `.../CatAnimStateMachine.cs`
- Create: EditMode tests for the state machine
- Edit: `Assets/_Game/Gameplay/Presentation/CatVisualConfig.cs`
- Edit: `Assets/_Game/Gameplay/Presentation/CatVisualPresenter.cs`
- Create/extend: the idempotent `PARALLAX/Setup/…` menu entry that wires cat visuals
- Edit: `Assets/_Game/Editor/Art/CatSpriteImporter.cs` — **only** if A1-5 shows it necessary

**Do not create `CatAnimationSet`.** §3.1's "Create: SO type `CatAnimationSet`" is withdrawn. The
tuning SO already exists and is `CatVisualConfig`.

---

## A1-2 · `CatVisualConfig` — reuse before you add

Several §3.2 inputs already exist under different names. Before adding any field:

**Report, as a table: each §3.2 threshold, the existing field you believe corresponds to it, that
field's current serialized value, and its unit.** Then:

- Reuse a field where the semantics match exactly. Say so.
- Add a field only where nothing matches. Say so.
- If a field nearly matches but not exactly, **do not stretch it.** Report it and stop.

Likely reuses, to be confirmed by you, not assumed by me: `airThreshold` → the Rise/Fall split,
`idleSpeedThreshold` → `walkEnter`, `velocitySmoothingTime`, `flipHysteresis`, `idleDwell`.

Certainly missing and to be added: `landDuration`, the Walk→Idle exit threshold (§3.2 rule 3
requires `walkExit < walkEnter`; a single `idleSpeedThreshold` cannot do both), and an apex band if
`airThreshold` turns out to be a single unsigned value rather than a pair.

Removed: `airFrame`, `idleFrame`, and — if clips subsume them — `walkLoopStart`, `walkLoopEnd`,
`walkFps`. Report which of those three you removed and which you kept, with the reason.

---

## A1-3 · Clips replace frame indices

§3.3's clip model stands, but it lives on `CatVisualPresenter` and replaces the index-into-`frames`
scheme rather than sitting beside it.

- A clip is `Sprite[] frames`, `float fps`, `bool loop`; one per `CatAnimState`. Non-looping clips
  hold the last frame.
- Populated by the setup menu from the per-state sheets, **not** by index ranges into one array.
  Index ranges break the moment a sheet's frame count changes, and the whole point of the art pass
  is that frame counts change.
- Keep the existing speed-scaled playback rate (`minFps`/`maxFps`/`referenceSpeed`) for Walk only.
  Idle, Rise, Fall and Land play at their clip's own fps. Report if that contradicts how
  `CatVisualPresenter` currently drives rate.

**`teleportDistance` and `velocitySmoothingTime` suggest the presenter already derives velocity
from a transform delta with teleport rejection.** If so, §3.3's velocity requirement is already
satisfied — confirm it in one sentence and change nothing. If it reads `Rigidbody2D.linearVelocity`
instead, change it, and say so explicitly in the changed-files explanation.

`echoAlpha` exists, so Echo cats are already a considered case here. §3.3's Echo report still
applies: state what the Echo cat and the Inactive cat display.

---

## A1-4 · Cat B

Only `CatA_Walk.png` exists. **Report what Reality B's cat currently renders** — the A sheet, a
placeholder, or nothing — before writing the setup menu. §3.4 requires both realities be wired and
report N/N; what "N" means for B depends on that answer. Do not generate or invent B art.

---

## A1-5 · Importer

`SetSpriteRects(...)` replacing the complete slice table is the correct behaviour and needs no
change *if* it genuinely replaces rather than merges. **Verify with a number, not a reading of the
code:** import a sheet, note the resulting `spriteSheet.sprites` count, re-import the same path
with a different cell count, and report both counts. The stale-slice-table failure in Stage 2c
looked exactly like working code.

---

## A1-6 · Stage 1 — reduced

Replaces §2's frame table. The broken thing is the air pose; that is what this version fixes.

| Sheet | Frames | Loop | Note |
|---|---|---|---|
| `CatA_Rise.png` | 1 | no | Pushing up, legs gathered. |
| `CatA_Fall.png` | 1 | no | Legs reaching. Distinct silhouette from Rise. |
| `CatA_Land.png` | 1–2 | no | Compression. `landDuration` ≈ 0.12 s. |
| `CatA_Idle.png` | 1 now, 4–6 later | yes | May be the existing `idleFrame` pose re-exported. |

**Every sheet uses the same cell size as `CatA_Walk.png`.** The importer slices a uniform grid;
a different cell size produces silently wrong sprites, not an error. Same pivot, same PPU, same
canvas, same character and camera distance. Raw output to `Art_Source/`.

Sliced sprite names follow the existing convention: `CatA_Rise_00`, `CatA_Idle_00`, and so on.
The setup menu resolves clips by these names — **pin them before writing the menu.**

§2's measurement table still applies to whatever frames you produce: per frame, canvas w×h, trimmed
opaque bbox, and baseline offset in px. The ≤ 2 px grounded-frame criterion stands. The tallest
opaque height and its world height at the shared PPU still go in the handoff for Q-10.

Multi-frame Idle, and any later expansion of Rise/Fall/Land, drop into the same clips with no code
change. That is the point of A1-3.

---

## A1-7 · Order of work

Stage 2 does not need the art. `CatAnimStateMachine`, its tests, the `CatVisualConfig` and
`CatVisualPresenter` changes, and the setup menu can all be written and compiled against empty
clips. §4 items 1–4 (compile, EditMode ≥ 174 plus new tests, setup idempotency, unassigned
renderers 0) are all reachable now.

§4 items 5–12 (Play) wait for Stage 1. An empty clip must hit the §3.3 fallback — previous state's
frame, logged **once** — not throw and not spam.

Proceed on that basis. Stop again if anything in this amendment contradicts what you find.