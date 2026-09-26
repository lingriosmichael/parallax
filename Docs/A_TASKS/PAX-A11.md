# PAX-A11 · Cat climb animation and a seamless vine

**Status:** Draft. Starts after PAX-087 is accepted.
**Depends on:** PAX-087 (climbing, `CatAnimState.Climb`, the interim pose), PAX-A01 (walk sheet: PPU and pivot), the
existing `CatSpriteImporter` flipbook, PAX-A06 tooling.
**Decisions:** D-089 (vines, the Climb state, the interim pose), D-052 (hitbox follows art, paw line −0.4), D-036 (no
Animator: code-driven flipbook).
**Rules:** `CLAUDE.md` AutoSprite section: 20 credits per asset including retries, key only in `AUTOSPRITE_API_KEY`, raw
candidates, contact sheets and the ledger outside `Assets/`, never overwrite an existing Unity asset during generation.
The developer provides the art prompts.

## 1. Why

PAX-087 shipped climbing with a placeholder: while `Climb`, `CatVisualPresenter` turns the cat's Visual ±90° around the
collider's centre and plays the Walk frames (moving) or the Idle frame (hanging). The developer accepted it for now
(2026-09-26, room 9 play-through) but it reads as a walking cat turned on its side, not a cat climbing. The vine itself
shows seams because its sprite is stacked in Simple segments (D-089 (2)): the importer mesh is Tight, and Tiled needs
Full Rect.

## 2. Scope

Define each slot (frames, cell size, PPU, pivot, path) in `Docs/Art/A02_asset_manifest.md` **before** generating.

1. **`CatA_Climb.png`** (new slot, `Assets/_Game/Art/Cats/CatA/`). A looping climb cycle, about 4–6 frames, the cat
   seen from the side hugging a vertical vine, head up, facing right (the presenter mirrors it for left). Same 256 px
   cells, PPU and paw-line pivot rule as the other Cat A sheets (the importer takes them from `CatA_Walk`). It must read
   at phone scale against the vine and the Reality A background.
2. **Hang pose.** Either one extra frame in the same sheet (still on the vine) or the cycle's first frame reused; decide
   in the manifest before generating.
3. **Vine tiling (optional, only if the developer approves the importer change here):** switch
   `A_OBJ_Vine.png`'s importer mesh to Full Rect (a developer step in the Editor, not a hand edit of the `.meta`), then
   draw vines with `SpriteDrawMode.Tiled` in `TrapKitSetup.BuildClimbVineCore` instead of stacked segments. Check the
   sprite tiles vertically without a visible seam; if it doesn't, it needs a seamless-vertical variant (a separate slot).

## 3. Code (a separate small engineering ticket, PAX-0xx, once the art exists)

Not inside this art ticket:
- `CatSpriteImporter`: add `CatA_Climb.png` to the Cat A sheet list.
- `CatVisualPresenter`: a `climbClip`; `ClipFor(Climb)` returns it; remove the interim ±90° pose (`ClimbPose`) or keep
  it only if the new art is drawn upright; frame rate scaled by the climb speed (as Walk is by run speed), the hang frame
  when Climb is 0.
- The setup that wires the new clip onto `Cat_Player.prefab` goes in a `PARALLAX/Setup/…` menu (never a hand edit).
- If item 3 is approved: the Tiled vine in `TrapKitSetup`, and D-089 (2) updated.
- Update D-089's presentation note; `ClimbPoseTests` change with the pose.

## 4. Acceptance (Editor)

- Trap Lab room 9 in the Device Simulator: climbing up, down and hanging read clearly at phone scale; the cat stays
  centred on the vine; the head points up in both gravities; leaving the vine (leap, release, snap) goes straight to the
  Rise/Fall frames with no pop.
- No collider, motor or route change: every EditMode route pin identical.
- The credit ledger per slot in the output.
- Device readability is **unverified (Phase H)**.

## 5. Out of scope

Other cat clips (PAX-A08), vine art redesign beyond tiling, ropes/swinging, climb sound or particles.
