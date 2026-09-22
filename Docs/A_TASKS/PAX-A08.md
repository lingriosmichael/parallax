# PAX-A08 · Remaining cat animations

**Status:** Draft. Runs alongside Phases B–D.
**Depends on:** PAX-A01 (walk), the existing `CatSpriteImporter` flipbook; PAX-A06 tooling
**Decisions:** D-052 (hitbox follows art, paw line −0.4), D-058 (death hold shows the death)
**Rules:** `CLAUDE.md` AutoSprite section: 20 credits per asset including retries, key only in
`AUTOSPRITE_API_KEY`, raw candidates outside `Assets/`, code-driven flipbook (no Animator).

## Scope

One asset slot per clip, each defined (frames, size, PPU, pivot on the paw line, path) in
`Docs/Art/A02_asset_manifest.md` **before** generating:
1. **Death** (plays during the 0.5 s hold; ends on a readable pose, ears back / scramble).
2. **Respawn** (short pop-in, ≤ 0.25 s, never delays control).
3. **Gravity flip** (mid-air twist; the root still rotates 180°, the clip only sells it).
4. **Jump set** completion if any of Rise / Fall / Land are still placeholder.
5. **Door enter** (level complete beat).

## Code (separate small engineering ticket if needed)

Hooking Death/Respawn/Flip/Door clips to `RoomDeath`/`GravityReceiver`/door events is a code
change: raise it as PAX-0xx once the clips exist, not inside this art ticket.

## Acceptance (Editor)

Each clip at phone scale in the Device Simulator: silhouette readable, pivot on the paw line in
both gravities, no collider change. Credit ledger per clip in the output. Device readability is
**unverified (Phase H)**.
