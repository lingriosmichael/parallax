# PAX-A04 · Cat collider matches the art (settles Q-10)

**Status:** Approved for implementation
**Phase:** Solo v1 · blocks PAX-043
**Depends on:** PAX-042 committed
**Decisions:** D-037, D-048, D-049, D-050, D-051 · proposes **D-052** (§8)
**Implementer reads first:** `Docs/07_DECISIONS.md` (newest wins), `CLAUDE.md`, then the cat's
collider setup as built (PAX-A01 cat sprite / outline, the cat setup menu, any cat or motor config
`ScriptableObject`), `CatMotor2D` grounding and ceiling checks, `SpawnPoint` / checkpoint spawn
placement. Where this ticket assumes a name or shape that differs from the code, **the code wins:
adapt and report it.** Where the difference changes behaviour, stop and report.

---

## 1. Goal

The cat's physics box is about 0.8 u, while the visible cat is about 0.6 u. Players judge hits by
the art, so hazards and ledges feel unfair: the cat dies when the art is clear of the spikes and
bumps into ceilings it visibly fits under. In a troll platformer, every death has to feel earned.

Make the collider match the cat's **body** (torso and legs, not the tail or ear tips), with its
bottom on the paw line. The working target is **≈ 0.62 u wide**; the height is measured, not
guessed.

## 2. Measure first (read-only, report before changing anything)

1. Collider as built: type (box/capsule), size, offset, and **where the value lives** (config SO,
   setup script constant, or scene/prefab serialized value).
2. The art's opaque bounds in world units: take the **union across all `CatA_Walk` frames**
   (alpha > 25), then give the body box (tail and ear tips excluded), with the paw line's
   offset from the pivot.
3. Everything that depends on the cat's size:
   - `CatMotor2D` ground/ceiling probes and skin values;
   - spawn height offsets;
   - the outline sprite (PAX-A01);
   - camera follow offsets;
   - tests with hard-coded 0.8 / 0.4.

   `grep` for `0.8f`, `.8f`, `0.4f`, `.4f` in `Gameplay`, `Core` and `Tests`, then triage the hits.

**Stop and report the numbers.** The Architect confirms the final width/height before step 3.

## 3. Change

- **One source of truth** for the cat collider size and offset. If a cat/motor config SO exists,
  it goes there. If not, add the fields to the existing motor config (not a new config type). The
  cat setup menu applies it; if the value currently lives in the scene or prefab, the setup menu
  becomes the only thing that sets it.
- **Both cats get the same value** (Vision §8: identical gameplay dimensions). Cat B's code stays
  frozen (D-047); only its collider data changes, through the same setup path.
- **Bottom stays on the paw line:** adjust the offset so the art does not float or sink.
- **Probes follow the collider:** any grounding, ceiling or wall probe that uses a fixed size or
  offset derives it from the collider (or the config) instead. With gravity **up**, the "ceiling"
  probe becomes the ground probe; check both directions (D-048).
- **Spawns:** if spawn placement adds a fixed half-height, derive it from the config.
- **Outline:** the visible-cat outline stays aligned with the art. Do not scale the art.

## 4. Not changed

Hazard and trap sizes (0.5 × 0.3 spikes and so on) stay as authored. PAX-043 authors rooms against
the new cat. Report any sandbox gap or tunnel that now fits the cat but didn't before, and leave it.
Also unchanged: motor tuning (speed, jump), the camera, Reality B art, and co-op code.

## 5. Tests (EditMode)

- Update the tests that hard-code the old size so they read from the config. That is a fix, not a
  regression; list each one.
- Add tests: the setup applies the configured size to both cats' data (if testable in EditMode),
  and the probe distances are derived from the collider size (pure-math helper, if one is
  extracted).
- **Baseline:** the PAX-042 total. Report baseline → new.

## 6. Allowed files

- The cat/motor config script and its `.asset` (asset changes via the setup menu or by the
  developer in the Inspector, never by hand)
- The cat setup menu script (PAX-A01 / cat setup)
- `CatMotor2D.cs` (probe derivation only)
- The spawn placement script, only if it hard-codes the half-height
- Tests touched by §5
- `Sandbox_Realities.unity`: only by the developer running setup menus

Anything else: stop and ask.

## 7. YOU — UNITY EDITOR

1. After the §2 report and the Architect's size confirmation: Cmd+R. The console should show
   only known noise.
2. Run the cat setup menu, then Cmd+S. Select both cats and check that the collider gizmo
   hugs the body with its bottom on the paws.
3. EditMode → Run All. Report the total.

## 8. Acceptance (Editor)

1. **Grounding, gravity down:** walk, jump and land on ground and on both ledges. No floating, no
   sinking, no jitter, and grounded jumps always work.
2. **Grounding, gravity up** (flip trap or debug flip): the same checks on ceilings and ledge
   undersides.
3. **Edges:** walking off a ledge edge, the cat drops when the art is visibly past the edge (not
   0.1 u later).
4. **Hazards:** `Hazard_0` and `Hazard_1` kill only when the art visibly touches them. Jumping over
   them clears when the art clears.
5. **Traps (PAX-042):** all five still trigger and kill as before. Stand under the falling block's
   path with the art just clear: no kill.
6. **Spawn/respawn:** the cat appears on the ground with no drop and no embed, for both gravity
   directions if the checkpoint stores up.
7. **Doors:** the room completes as before.
8. **Console:** only known noise.

## 9. Proposed decision

**D-052 · Cat hitbox follows the art body** *(resolves Q-10)*
The cat's collider matches the visible body (torso and legs, excluding tail and ear tips), with its
bottom on the paw line, and it is the same for both cats. The size has a single source of truth in
config; probes and spawns derive from it. Hazards are authored against the art, so a hit that looks
clear is clear.

## 10. Required output

As `CLAUDE.md` requires, plus the §2 measurements, the list of size-dependent sites and what
changed at each, and the review file:

```
{ git status; git --no-pager diff -- . ':(exclude)*.unity'; for f in $(git ls-files --others --exclude-standard | grep -v '\.meta$'); do echo "===== $f"; cat "$f"; done; } > ~/Desktop/paxa04_review.txt 2>&1
```

Commit after Architect review: `PAX-A04: Cat collider matches art (D-052)`.
Amended 2026-09-21: 0.62 was an axis error. Final: horizontal capsule 1.0 × 0.56, offset (0, −0.12), confirmed after the §2 measurement.