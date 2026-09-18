# PAX-V01 — Dev-only state labels (minimal)

```text
TASK: PAX-V01
TITLE: Dev-only live state labels on cats and puzzle objects
PHASE / GATE: Tooling / supports acceptance runs to Gate 4
TYPE: Dev tooling. Editor only. Zero effect on release builds. Small.
```

## OBJECTIVE

Show live state that art can't show: which driver runs each cat, what each anchor and sensor is doing, and which objects are linked across realities. Nothing else. Telling the two realities apart and recognising objects is PAX-A02's job.

## CONTEXT

- `02_ARCHITECTURE.md` §1: `Parallax.DebugTools` is dev-only via `defineConstraints`.
- As-built notes: PAX-017 (debug panel, `ITouchReservedRegion`), PAX-021 (anchors), PAX-023/024 (Echo, plate, gate), PAX-026 (station).

## CLAUDE — CODE

One `OnGUI` overlay in `Parallax.DebugTools`, `WorldLabelOverlay.cs`. It draws text on a dark backing box above each object, projected through the **active Observer's camera** only. Objects are discovered by component type, so no component is added to any gameplay prefab or scene object. It rescans on `SoloSwitchController.Switched` and on enable, never per frame.

| Object | Label |
|---|---|
| Cat | `A · LIVE` / `B · IDLE` / `A · ECHO 4.2s` / `A · ECHO HOLD`, plus `· seated` when seated |
| Anchor manifestation / interactable | `[K1] 0` / `[K1] →1` while pending |
| Pressure plate | `[K2] pressed (echo)` / `[K2] free` |
| Gate | `[K2] open` / `[K2] closed` |
| Control Station | `station: empty` / `station: A 0.42` |

- `[Kn]` = a stable tag per anchor id. The same tag appears on both linked objects in both realities. Text only, no color palette.
- Toggle: `F1` in the Editor, plus one toggle in the existing debug panel. Default **on**.
- A pure `LabelFormatter` builds the strings from plain values.
- If a needed value is private, add a read-only getter to the gameplay class and list each one in the deliverable. No other gameplay changes.
- Setup: add the overlay via the existing `PARALLAX/Setup` pattern, on the same GameObject the debug panel uses (idempotent).

**EditMode tests:** `LabelFormatter` covers each cat driver state, seated, pending anchor, plate states and station states. Anchor tags are stable regardless of discovery order.

## DO NOT

- No reality tinting, legend, frame, watermark, colors or icons.
- No components on gameplay prefabs. No gameplay behavior changes.
- No PlayerPrefs. No Photon references.

## ACCEPTANCE TEST — Editor only

1. Test Runner → all green.
2. Setup menu run twice → one overlay.
3. Cat labels switch LIVE/IDLE on Tab. An Echo shows a counting `ECHO` label, then `ECHO HOLD`.
4. Pull the vine → `[K1] →1` then `1`. Switch → the elevator shows `[K1] 1`. Run once at 300 ms latency.
5. Plate shows `pressed (echo)` when the Echo stands on it. The linked gate shows `open` with the same tag.
6. Station shows the seated cat and the dial value.
7. `F1` and the panel toggle hide and show the labels. Tapping the toggle never moves the cat.
8. Non-development build (build only, no install) succeeds with no missing-script warnings.

## DELIVERABLE / DONE

Changed files, list of added getters, test names. Review file: `{ git status; git --no-pager diff; cat <new files>; } > ~/Desktop/paxv01_review.txt 2>&1`. Universal DoD (§8). Commit `PAX-V01: dev-only state labels`.
