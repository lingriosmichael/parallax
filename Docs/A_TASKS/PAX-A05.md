```text
TASK: PAX-A05
TITLE: Exit door art (Reality A) + shared greybox sprite Full Rect
PHASE / GATE: Solo v1 / art pipeline (D-031)
DECISIONS: D-031, D-050
RUN AFTER: PAX-041 is committed
```

## OBJECTIVE

Replace the pink greybox on the Reality A exit doors with `A_Door_Exit.png`, without changing any
door behaviour or collider, and clear the sliced-sprite warning by setting the shared greybox
sprite's Mesh Type to Full Rect.

## CONTEXT

- `Docs/00_VISION.md` §8: artwork never decides collision.
- PAX-040 / D-050: doors are `Door_0` at (3.00, −2.75) and `Door_1` at (8.00, −2.75), zone
  0.6 × 1.5, built by `RoomSetup`. Ground top is y = −3.50.
- PAX-A01: the cat sprite import, for folder layout and sorting-layer conventions.
- The sliced-sprite warning ("Sprite Tiling might not appear correctly … Full Rect") appears on the
  first run of setups that use `SetupUtility.SetVisual`.

## CLAUDE — CODE

**Step 0 — read before writing.** Read `RoomSetup`, `SetupUtility`, and the PAX-A01 art setup.
Report:
- the art folder the cat sprite lives in; the door sprite goes in the matching folder, e.g.
  `…/Art/Doors/`;
- which sorting layer the door greybox uses now, and which `A_*` layer the art should use (behind
  the cat);
- **which sprite asset** `SetupUtility.SetVisual` assigns (path), so the developer can set it to
  Full Rect.

If the real names differ from this ticket, use the real ones and list the adjustments. If anything
conflicts with what is built, stop and report.

**Modify:**

- `RoomSetup.cs` — doors get a child `Art` with a `SpriteRenderer` using `A_Door_Exit`, on the
  sorting layer from Step 0, at local position `(0, −0.75)`, so the sprite's bottom-centre pivot
  sits on the ground (y = −3.50). The door's own greybox renderer is removed or disabled, and the
  door's zone, position and components are unchanged. Idempotent: running it twice leaves one `Art`
  child per door. If the sprite asset cannot be found, log an error with the expected path and leave
  that door's greybox as it is.
- `SetupUtility.cs` — only if a helper is needed for the above. No behaviour change to
  `SetVisual`.

No new runtime scripts, and no tests needed (no `Core` / `Gameplay` logic changes).

## YOU — UNITY EDITOR

1. **Before Claude Code runs anything:** copy `Docs/Art/Reference/A_Door_Exit.png` into the art
   folder from Step 0. Let Unity import it (this creates the `.meta`).
2. Import settings for `A_Door_Exit`: Texture Type **Sprite (2D and UI)**, Sprite Mode **Single**,
   Pixels Per Unit **196.667**, Mesh Type **Full Rect**, Pivot **Bottom Center**, Filter and
   Compression matching the cat sprite. Apply.
3. Select the shared greybox sprite from Step 0. Set Mesh Type **Full Rect**. Apply.
4. Run the room setup menu (`RoomSetup`).
5. **Save the scene (Cmd+S).** Check that `git status` lists `Sandbox_Realities.unity`,
   `A_Door_Exit.png`, its `.meta`, and the greybox sprite's `.meta` as changed.
6. Run the acceptance test below.

## DO NOT

- Import, wire or reference `B_Door_Exit.png` (co-op update).
- Change the door zone size, position, layer, or completion logic.
- Resize or rescale colliders to fit the art. If the art and the 0.6 × 1.5 zone don't match, report
  it. Size changes are a separate decision (see Q-10).
- Add open/closed states, animation, VFX or sound. The door that moves away is PAX-042.
- Hand-edit `.meta`, `.asset` or `.unity` files. Import settings are set by the developer.

## REQUIREMENTS

- Both Reality A doors show `A_Door_Exit`, standing on the ground, drawn behind the cat.
- Doors complete rooms exactly as in PAX-040: `Door_0` → checkpoint 1, `Door_1` → level complete.
- No pink greybox visible on doors.
- Running the room setup twice produces no duplicate `Art` children.
- The sliced-sprite warning no longer appears on a setup run.

## ACCEPTANCE TEST (Editor only)

Clear the console.

1. Scene view: both doors show the art with its bottom edge on the ground, centred on the door
   zone. Note, in your output, how the art's size compares with the 0.6 × 1.5 zone.
2. Play. Walk into `Door_0`: room 0 completes, the cat appears at checkpoint 1. Walk into
   `Door_1`: level complete. The debug panel room row reads as before.
3. The cat walks in front of the door art, not behind it.
4. Flip gravity (Q) near a door: the art stays world-aligned and is unchanged.
5. Stop. Run the room setup again. Each door still has exactly one `Art` child.
6. Console: no sliced-sprite warning, and nothing else beyond the known noise in `CLAUDE.md`.

## DELIVERABLE (from Claude Code)

The `CLAUDE.md` required output, plus Step 0's findings (art folder, sorting layer, shared sprite
path) and the art-vs-zone size comparison from step 1.

## DEFINITION OF DONE

Universal DoD + acceptance steps 1–6 passed by the developer in the Editor + the sprite, its
`.meta`, the greybox sprite's `.meta` and the saved scene all committed.

Commit: `PAX-A05: Exit door art (Reality A), shared sprite Full Rect (D-031)`
