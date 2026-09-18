```text
TASK: PAX-V01a
TITLE: World labels: dedupe, no name heuristics, cached lookups, single toggle, no auto-save
PHASE / GATE: Visual lane (patch to PAX-V01) / none
TYPE: Patch. Small. Editor only. DebugTools + one Editor setup script.
```

## OBJECTIVE

Fix the review findings on PAX-V01 (commit b1f8eac) without changing what the labels show,
except removing the duplicate vine label.

## STEP 0 — BEFORE CLAUDE CODE STARTS (you)

1. `git show --stat b1f8eac` and `git status`. The V01 review file showed all of `Docs/TASKS/*`
   and `Docs/VISUAL_TASKS/*` deleted and `Docs/0_TASKS/`, `Docs/A_TASKS/`, `Docs/V_TASKS/`
   untracked. If the commit took the deletions but not the new folders, `git add Docs/` and commit
   `Docs: reorganize tasks into 0_/A_/V_TASKS` (git will record renames). Check that
   `Docs/A_TASKS/` holds the **amended** PAX-A01/A02, not the originals.
2. `ls -R Assets/Resources` (untracked, not part of V01). Anything under `Resources/` ships in
   every build. If it came from MCP for Unity or another tool, do not commit it; report what it is.
3. `git status` clean before Claude Code starts.

## CLAUDE — CODE

Allowed files: `Assets/_Game/DebugTools/WorldLabelOverlay.cs`, `DebugPanel.cs`,
`LabelFormatter.cs`, `Tests/EditMode/LabelFormatterTests.cs`,
`Editor/Setup/DebugLabelSetup.cs`, and the PAX-037 ticket (doc text only).

1. **One label per anchor per reality.** Today the vine gets two labels (the `Vine_A`
   manifestation and `VineZone_A` interactable, both `[K1]`). Rule: label the thing the cat
   touches (VineInteractable, PressurePlateSensor) and skip manifestations of the same anchor in
   the same reality. Manifestations in the other reality (elevator, gate) keep their label.
2. **No name heuristics.** Remove `manifestation.name.Contains("Gate")`. Choose the format from
   the anchor's writer: an anchor written by a `PressurePlateSensor` formats as open/closed;
   everything else uses the value/pending format. Determine this once during the scan.
3. **Cache at scan time, not per draw.** Per entry store the owning reality (`ObserverId`, from
   `GetComponentInParent<RealityRoot>()` once) and, for cats, the `CatSeat`. No
   `GetComponent*` inside `OnGUI` or the text lambdas.
4. **No `?.` / `??` on Unity objects** (project rule): replace
   `GetComponentInParent<RealityRoot>()?.Id` and `(transportHost as LocalTransportHost)?.Local`
   with explicit null checks / pattern matching. `?.` on the plain-C# driver is fine.
5. **Draw only on `EventType.Repaint`**, so text lambdas and string formatting run once per frame,
   not per GUI event.
6. **Scan once.** Scan in `OnEnable` only; drop the rescan on `Switched` (entries don't depend on
   the active Observer; the reality filter already runs at draw time).
7. **One visibility flag.** F1 toggles `DebugPanel`'s flag (add a setter or `ToggleLabels()`);
   remove the overlay's own `visible` field. The panel toggle and F1 always agree.
8. **Dead code:** remove the discarded `Tag(...)` calls. Rename
   `AnchorTags_AreStableRegardlessOfDiscoveryOrder` to `AnchorTag_UsesAnchorId` (that's all it
   tests now).
9. **`DebugLabelSetup`:** remove both `EditorSceneManager.SaveScene` calls. Mark the scene dirty
   only when something changed (project convention; the setup must never save unrelated edits).
10. **PAX-037 T2:** replace the LBL-button text with: labels toggle via the Debug Panel's
    "World labels" and F1; check the panel's grown rect for clipping and reserved-region overlap.

## DO NOT

- Change gameplay files or the getters V01 added.
- Add labels for new object types, a legend, or checkpoint/fall-volume labels.
- Commit. The Architect reviews first.

## ACCEPTANCE TEST (Editor)

1. EditMode: 165 green (same count; one test renamed).
2. Play: the vine shows exactly one label; elevator (B), plate (A), gate (B), station and both
   cats each show one.
3. Plate: step on → `pressed`; gate label in B → `open` after commit. Latency 300: vine label shows
   `→1` while pending, then `1`.
4. F1 hides labels and the panel's "World labels" unticks; ticking it shows them and F1 again hides.
5. Tab several times: labels follow the active reality, no errors.
6. Run `PARALLAX/Setup/Debug Labels (Sandbox)` twice: second run "no changes", scene not saved
   automatically (title bar still shows unsaved only if something changed).
7. Grep, verbatim in the deliverable:
   `grep -n "?\.\|??" Assets/_Game/DebugTools/WorldLabelOverlay.cs`
   `grep -n "GetComponent\|FindObjects" Assets/_Game/DebugTools/WorldLabelOverlay.cs`
   `grep -n "SaveScene" Assets/_Game/Editor/Setup/DebugLabelSetup.cs`

## DELIVERABLE

Standard CLAUDE.md output plus the grep output and the review file:
`{ git status; git --no-pager diff; cat <new files>; } > ~/Desktop/paxv01a_review.txt 2>&1`
