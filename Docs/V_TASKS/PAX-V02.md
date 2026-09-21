# PAX-V02 · Clean-up pass: D-038 demotion, placeholder visuals, foreground placement

**Status:** Approved, not started
**Date:** 2026-09-21
**Lane:** Visual
**Depends on:** PAX-A03 closed (Play acceptance reported, committed)
**Implements:** D-038 (player-facing half only)
**Does not implement:** D-037 gravity up/down — that waits for the D-021 device test and gets its own ticket
**Docs to read first:** `Docs/07_DECISIONS.md` (newest wins; read D-034 to D-039), `CLAUDE.md`
**Scene:** `Sandbox_Realities`

---

## 0. Why

The sandbox currently shows three kinds of thing that no longer belong to the game:

1. **Player-facing solo controls** (`REC`, `a | B`) for a solo mode D-038 removed. Echo and the
   reality switch stay — as dev tooling, not as player UI.
2. **White and translucent placeholder boxes** from PAX-024/026 sitting on top of finished art, in
   both realities.
3. **The foreground layer** (FG_01, B_FG_01) hanging from nothing at mid-screen instead of from the
   top of the view, and washing the lower half of the scene.

None of this is new mechanics. It is removing what is wrong, without removing what works.

---

## 1. Scope

**In:** hiding or dev-gating the three items above; an inventory of every placeholder renderer; an
inventory of documentation lines that contradict D-037 to D-039.

**Out — do not touch:**

- Echo recording, replay, `EchoReplay` driver, reality switching logic. D-038 keeps all of it.
  Only the *player-facing entry points* move.
- Any collider, trigger, layer mask, physics or motor value. **Hiding a renderer never changes
  collision.**
- Gravity (D-037). Four directions stay working until the gravity ticket.
- V01 dev labels (`B · LIVE`, `[K2] closed`, `[K1] 0`). They stay; they are now the state
  indicators for testing.
- Documentation files. Report conflicts in §2 item 5; the Architect edits the docs.
- Platform fill variation, parallax layering, new art. Separate tickets.

---

## 2. Stage 0 — inventory. Report, then stop.

No code changes in this stage. This is where the Architect decides what gets hidden.

1. **Solo HUD.** For `REC`, the `a | B` switch, and any keyboard shortcut or touch region that
   triggers recording or switching: file path, class, assembly, and whether it already sits in
   `Parallax.DebugTools` behind `defineConstraints`. Name the define.
2. **Reserved touch regions.** PAX-017 reserves screen regions for the switch so the stick ignores
   them. Report what reserves them and whether removing the switch from a build also releases its
   region. A hidden button with a live reserved region is a dead patch of screen.
3. **Placeholder renderers — both realities.** Every renderer that draws a default/white sprite, a
   built-in square, or a flat colour with no authored art. One row each:

   | Object path | Owner component | Created by (scene / setup menu / runtime code) | What state it shows | Covered by art or a V01 label? | Is the object physically blocking? |

   At minimum this must account for: station `Base`, `Glow`, `Pulse`; plate `Indicator`; the tall
   white bar at the Reality B gate (`[K2]`); the white box at `[K1]`; the green bar in Reality B;
   the large translucent teal box in Reality B; the translucent box at the base of the vine; the
   blue capsule seen near the top of Reality A. If any of these is not a placeholder renderer,
   say what it is.
4. **Foreground.** For `RealityRoot_A/Foreground` and its B counterpart: local and world position,
   sprite bounds in world units, and the camera's view rect (world units, top edge) at its spawn
   framing. Then measure the FG_01 and B_FG_01 PNGs: fraction of pixels with alpha strictly
   between 0 and 255, and mean alpha of those pixels, reported for the lower half of the image
   separately. That decides whether the wash is placement or baked-in haze.
5. **Doc conflicts.** Every line in `CLAUDE.md`, `Docs/02_ARCHITECTURE.md` and `Docs/00_VISION.md`
   that contradicts D-037 (four-direction gravity), D-038 (solo via Echo, player-facing switch) or
   D-039. Quote the line, give file and line number. Do not edit.

Report numbers, not verdicts. Then stop.

---

## 3. Stage 1 — implementation (after Architect review of Stage 0)

### 3.1 D-038: solo HUD out of the player's game

- `REC`, `a | B` and their input bindings must exist only in builds where `Parallax.DebugTools` is
  compiled. If Stage 0 shows they already are, this item is verification only — say so.
- If they are not: move the **entry points** (buttons, bindings, reserved regions) into
  `Parallax.DebugTools`. The Echo and switching systems they call stay where they are.
- Assembly rule holds: nothing in `Parallax.Core` or `Parallax.Gameplay` may reference
  `Parallax.DebugTools`. Prove it with a search, not a reading.
- In dev builds, keep them usable. Mark them visibly as dev controls (a `DEV` prefix on the label
  is enough) so a screenshot never again suggests they are the game's UI.

### 3.2 Placeholder renderers

Rule, per inventory row:

- **Hide** if the state it shows is already conveyed by art or by a V01 label.
- **Do not hide — stop and report** if the object is physically blocking (gate, elevator, any
  collider the cat can hit) and the placeholder is its only visual. An invisible wall is worse
  than a white box.
- **Do not hide — stop and report** if it is the only indicator of a state and no V01 label covers it.

Mechanism, least invasive first:

- Scene or setup-menu objects: disable the renderer through the idempotent `PARALLAX/Setup/...`
  menu. Do not delete the object.
- Runtime-created objects: add a serialized `bool showPlaceholderVisuals` (default `false`) to the
  owning presentation component and skip creating or enabling the renderer when false. Do not
  delete the creation code.

Report which mechanism each row used.

### 3.3 Foreground placement

- In the setup menu, place each reality's Foreground so the sprite's top edge sits on the top edge
  of that reality's camera view at spawn framing. World-space positioning, **not** parented to the
  camera — parenting makes it a screen overlay and blocks the parallax work that comes later.
- Idempotent: second run reports no changes.
- If Stage 0 item 4 shows the wash is baked-in haze rather than placement, fix placement only and
  report the haze numbers. Art is not edited in this ticket.

---

## 4. Acceptance

**Editor (Claude Code):**

1. Compiles — `refresh_unity` + `validate_script` + `read_console`, quote the result.
2. EditMode green, **total ≥ 186**. A lower total needs an explanation per removed test.
3. Setup menu run twice: second run reports zero changes.
4. Search proof that `Parallax.Core` and `Parallax.Gameplay` have no reference to `Parallax.DebugTools`.
5. Unassigned renderers: 0. No collider, trigger, layer or physics change in the diff.

**Play (developer), both realities:**

6. No white or translucent placeholder boxes over art.
7. Gate and elevator still visible and still block the cat.
8. V01 labels still show plate, gate and elevator state.
9. Foreground roots hang from the top of the view; lower half of the scene is not washed.
10. `REC` and `a | B` still work in the Editor, visibly marked as dev controls.
11. Echo record, replay and reality switching still behave as before PAX-V02.

---

## 5. Definition of done

- Stage 0 inventory table complete, every listed object accounted for.
- **Enumerate, point by point, which requirements of §3 are implemented and which are not.**
- Doc-conflict list delivered for the Architect.
- Numbers, not verdicts. No self-graded rules.
- Claude Code does not commit. Review file → Architect review → commit.
  `{ git status; git --no-pager diff; cat <new files>; } > ~/Desktop/paxV02_review.txt 2>&1`
- After any clone or history rewrite: `git lfs checkout` before opening Unity.
- If Unity disconnects: stop, report, no retries.
