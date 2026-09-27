# PAX-A08 · The cat comes alive: the full Cat A animation set

**Status:** Approved (after PAX-A12; can start as soon as PAX-V03 sets the budget). **Supersedes** the earlier A08 draft and **absorbs PAX-A11** (climb and seamless vine).
**Depends on:** PAX-A01 (walk sheet: 256 px cells, PPU, paw-line pivot), `CatSpriteImporter`, PAX-A06 tooling, PAX-V03 (normal maps, rim light).
**Target:** `Docs/Art/LOOK_AND_FEEL.md` §1 and `DesignImages/13_cat-character-sheet.png` (Cat A: slim, dark brown, amber eyes, long curled tail).
**Decisions:** D-036 (code-driven flipbook, no Animator), D-052 (hitbox follows art, paw line −0.4), D-058 (the death hold shows the death), D-041 (death to control ≤ 0.75 s, no fade), D-089/D-092 (climbing).
**Rules:** `CLAUDE.md` AutoSprite section; every slot defined in `Docs/Art/A02_asset_manifest.md` before generating. The developer provides the prompts. **Budget note:** at 20 credits per clip, the set below is up to ~300 credits; the developer confirms the total before generation starts.

## 1. Why

Today the cat has Idle, Walk, Rise, Fall, Land, and a placeholder Climb (Walk frames turned 90°). A paid game needs a cat that reacts to everything that happens to it.

## 2. The clip list (one slot each)

| # | Clip | Frames (about) | Plays when | Notes |
|---|---|---|---|---|
| 1 | **Idle** (redo, breathing) | 6–8 loop | standing still | from the sheet's IDLE pose; tail sways |
| 2 | **Idle variants** | 2 × 8 | after 3 s idle: tail flick; look around (CURIOUS pose) | chosen by a deterministic idle timer, not random |
| 3 | **Run** (redo Walk at full stride) | 8 loop | moving; frame rate scales with speed | Walk kept for slow stick pushes (< 0.5) |
| 4 | **Turn** | 3 | reversing direction on the ground | quick, never delays movement |
| 5 | **Jump take-off / Rise / Apex / Fall** | 2 / 3 / 2 / 3 | airborne phases by vertical speed | from JUMPING and FALLING (MID-AIR) poses |
| 6 | **Land** (redo) + **hard land** | 3 / 4 | landing; hard land after a long fall | hard land = squash |
| 7 | **Death** | 6, ends on a held pose | at the kill; holds through the death hold | FRIGHTENED pose, fur up; must read in 0.6 s |
| 8 | **Respawn** | 4 | respawn tick | ≤ 0.2 s, never delays control, no fade |
| 9 | **Gravity flip** | 4 | on a flip | a mid-air twist; the root still rotates 180° |
| 10 | **Climb** (from A11) | 6 loop + 1 hang | on a vine | head up, hugging the vine; frame rate by climb speed |
| 11 | **Leap off vine** | 3 | leaving a vine with a jump | blends into Rise |
| 12 | **Launched** (geyser) | 3 loop | inside a geyser column | ears back, legs tucked |
| 13 | **Dizzy** (inverted) | overlay or 4 loop | while an inverter is active | layered with the ring cue; optional if the cue reads |
| 14 | **Door enter / celebrate** | 8 | touching the door | the CELEBRATING pose; plays under the level-complete screen |
| 15 | **Seamless vine** (from A11) | — | vine sprite | Full Rect importer (developer step) + Tiled draw, or a seamless-vertical variant |

All clips: same 256 px cells, PPU and paw-line pivot as `CatA_Walk` (the importer enforces it), facing right (mirrored in code), readable at phone scale, **with normal maps** (PAX-V03's technique).

## 3. Code (a separate engineering ticket, PAX-V07, once the clips exist)

`CatAnimStateMachine` gains the new states (appended to `CatAnimState`, D-036 pure state machine, EditMode-tested); `CatVisualPresenter` gets the clips; the interim climb pose is removed; setup menus wire the prefab. **No collider, motor or route change:** every pin identical.

## 4. Acceptance (Editor)

Trap Lab rooms 0–11 and L011–L020 in the Device Simulator: every state reads at phone scale, in both gravities; no frame pops between clips; the death reads within the hold. Credit ledger per clip. Device feel **unverified (Phase H)**.
