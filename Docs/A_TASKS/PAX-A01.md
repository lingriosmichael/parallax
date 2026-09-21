```text
TASK: PAX-A01
TITLE: Cat A walk sprite in-game + visibility outline (art pipeline test, part 1)
PHASE / GATE: Art lane (plan Phase 7, pulled forward — see D-031) / none
```

## OBJECTIVE

Replace Cat A's placeholder sprite with the AutoSprite walk cycle. The cat walks, idles and flips
driven by its actual motion, stays correctly oriented under all four gravity directions, sits on
the ground at its paws, and stays readable on dark backgrounds through an unlit outline. Cat B uses the same sheet as a
stand-in so neither reality keeps a grey-box cat once PAX-A02 lands.
No gameplay code, colliders, physics or networking change.

## CONTEXT

- `02_ARCHITECTURE.md` §2 (spatial model, sorting layers, Light2D per reality), §6 (motor, gravity), Echo section (kinematic playback), Performance ("keep the inactive reality free of per-frame cosmetic work").
- `CLAUDE.md`: "Synchronize semantic state, never presentation"; artwork never decides collision (plan §18.3).
- Plan §18.4–18.5: pipeline test DoD and cat DoD.
- `07_DECISIONS.md` D-031 (art pipeline test starts now) and D-030/D-032 (checkpoints: `CatRespawn.RespawnAt` teleports the fallen cat and snaps its gravity).
- Source asset: `Regulus-walk.png` from AutoSprite (Side-scroller, 256 px cells, 5 columns). Analysis of the first export:
  - The true cycle is **10 frames** (frame 11 ≈ frame 1). Frames 11–15 repeat 1–5 and must not be in the loop.
  - Paws sit on the same row in every frame; nose x is fixed. The cat walks in place (correct).
  - Tail tip clipped at the cell edge in frames 4, 5, 14, 15 → re-export with more padding (YOU, step 1).
  - Silhouette disappears on dark warm (Reality A ruins) and navy (Reality B) backgrounds → outline.

## PRE-FLIGHT (Claude Code — report before writing code)

Read, do not edit, and report in 5 lines or fewer:
1. The structure of `Cat_Player` (child names, which object has the `SpriteRenderer`).
2. Where sprite flipping currently happens (the PAX-006/007 requirement "sprite flips based on movement direction").
3. Whether the cat's root transform rotates when gravity changes, or only the motor's `right`/`down` vectors do.
4. What `CatMotor2D` exposes publicly for grounded state, and whether that value is valid while an Echo (kinematic) cat plays back.
5. Whether `.gitattributes` tracks `*.png` through LFS.
6. Whether Cat A and Cat B are both instances of `Cat_Player.prefab`, and how a cat can tell which reality it belongs to (parent `RealityRoot`, its Observer, or physics layer).
7. Whether `GravityReceiver` exposes the *current* (smoothed) direction separately from the target set by `SetTargetDirection`.

If any answer conflicts with this ticket's assumptions, **stop and report** before implementing.

## CLAUDE — CODE

Create: `Assets/_Game/Core/Presentation/FlipbookMath.cs` (pure: frame index from elapsed time, fps, loop range; speed-scaled fps)
Create: `Assets/_Game/Core/Presentation/GravityFrame.cs` (pure: project a world velocity onto gravity-relative along/up axes; rotation angle so "up" = −gravity)
Create: `Assets/_Game/Gameplay/Presentation/CatVisualConfig.cs` (ScriptableObject)
Create: `Assets/_Game/Gameplay/Presentation/CatVisualPresenter.cs` (MonoBehaviour)
Create: `Assets/_Game/Art/Shaders/SpriteOutlineUnlit.shader` (**explicitly permitted by this ticket**)
Create: `Assets/_Game/Editor/Art/CatSpriteImporter.cs` (`PARALLAX/Art/Import Cat Sheet` menu)
Create: `Assets/_Game/Editor/Art/CatVisualSetup.cs` (`PARALLAX/Setup/Cat Visual` menu, idempotent)
Create: `Assets/_Game/Tests/EditMode/FlipbookMathTests.cs`, `GravityFrameTests.cs`
Modify (only if pre-flight item 2 shows flipping lives there): `CatMotor2D.cs`, to remove sprite flipping. The presenter becomes the only owner of flip.

## YOU — UNITY EDITOR

1. **In AutoSprite:** re-export the walk with Auto Padding moved towards *More padding* until no tail tip touches a cell edge. Keep 256 px cells.
2. Put the PNG at `Assets/_Game/Art/Cats/CatA/CatA_Walk.png`. Confirm in `git lfs status` that it's tracked by LFS before committing.
3. Select the PNG → run `PARALLAX/Art/Import Cat Sheet`.
4. Run `PARALLAX/Setup/Cat Visual` with `Sandbox_Realities` open.
5. Create a Sprite Atlas `Assets/_Game/Art/Cats/CatA/CatA.spriteatlas`: add the folder, **Allow Rotation off, Tight Packing off, Padding 8**.
6. Tune on the config asset (not in code) until paws don't slide: `referenceSpeed`, `walkFps`, `outlineWidth`, `outlineColorA`.

## REQUIREMENTS

**Import (`CatSpriteImporter`)**
- Sprite Mode Multiple, grid slice from `cellSize` (default 256) and column count read from the texture width. Sprites are named `CatA_Walk_00…NN`.
- Pixels Per Unit chosen so the drawn cat body length matches the `Cat_Player` collider length. Compute it from the collider and the opaque pixel width of frame 0, and log the value used.
- **Pivot computed from pixels, not hard-coded:** y = lowest opaque row across all frames (paw line); x = midpoint of the paw span across frames. Apply the same pivot to every frame. Log it.
- **Mesh Type = Full Rect** (tight meshes would clip the outline), Filter Bilinear, Compression None for now, Generate Mip Maps off.
- Idempotent: re-running on a new export overwrites the slicing and pivot without breaking references.

**Presenter (`CatVisualPresenter`, on the `Visual` child)**
- Drives a `SpriteRenderer` flipbook from `CatVisualConfig`. **No Animator** in this ticket.
- States: `Idle` (hold `idleFrame`), `Walk` (loop `walkLoopStart…walkLoopEnd`, default 0–9), `Air` (hold `airFrame`). Grounded comes from what pre-flight item 4 finds. If there is no Echo-valid source, use `Air` only when `|up velocity| > airThreshold`.
- **Motion is measured from the transform's position delta per `LateUpdate`**, not from the motor or Rigidbody. That way live, Inactive and Echo (kinematic) cats all animate identically.
- Along-speed = velocity projected on the gravity-perpendicular axis (`GravityFrame`). Walk fps scales with |along-speed| / `referenceSpeed`, clamped `[minFps, maxFps]`. Below `idleSpeedThreshold` → Idle.
- Facing flips on the sign of along-speed with a small hysteresis. It holds the last facing when stopped and flips the `Visual` transform (`localScale.x`), so the outline flips with it.
- Orientation: if the root doesn't rotate with gravity (pre-flight item 3), the presenter rotates `Visual` so up = −gravity, reading `GravityReceiver`'s **current** direction (not the target), so dial-driven gravity rotates the sprite smoothly. If the root already rotates, do nothing.
- **Teleport guard:** if the position delta in one `LateUpdate` exceeds `teleportDistance`, treat it as a teleport (checkpoint respawn via `CatRespawn.RespawnAt`, Echo replay start, Echo cancel, becoming visible again after being skipped): reset the position baseline, show `Idle`, keep the current facing, and do not flip or show `Air` that frame. The same reset runs in `OnEnable` and whenever the renderer becomes visible after being skipped.
- **Echo tint:** while the cat is driven by `EchoReplay` (use the same check `CheckpointPolicy.FallResets` uses), multiply the cat's alpha by `echoAlpha` (outline stays full alpha), so the Echo reads as a ghost next to the live cat. Presentation only; nothing is recorded.
- Skips all work while its renderer isn't visible (`SpriteRenderer.isVisible`), which keeps the inactive reality free.
- Never networked, never read by gameplay, never writes to anything outside `Visual`.

**Outline**
- `SpriteOutlineUnlit.shader`: samples the sprite's alpha at 8 offsets (`_OutlineWidth` in texels) and outputs `_OutlineColor` where the centre alpha is low and a neighbour's alpha is high; transparent elsewhere. It must render under the URP 2D Renderer; verify the pass tag in Play mode.
- `CatVisualSetup` adds a child `Outline` `SpriteRenderer` under `Visual`, with the same sprite each frame (the presenter sets both), material from the shader, same sorting layer as the cat, order −1.
- Outline colour per reality from config: warm amber for A, cyan for B. It is unlit, so reality Light2Ds don't darken it.
- Cat sorting layer = its reality's `*_Gameplay` layer (`A_Gameplay` for Cat A, `B_Gameplay` for Cat B), for both the cat and its outline. Outline colour and sorting layer are resolved **per instance from the cat's reality** (pre-flight item 6), at runtime or as instance values set by the Setup script, never baked into the prefab as A's values.
- **Cat B stand-in:** if both cats are instances of `Cat_Player`, the Setup script puts `Visual`/`Outline`/presenter on the prefab so both cats get it; Cat B uses the Cat A sheet with `outlineColorB`. If they are not the same prefab, apply the Setup to both cat objects.

**Config (`CatVisualConfig`)**
- `cellSize, walkLoopStart, walkLoopEnd, idleFrame, airFrame, walkFps, minFps, maxFps, referenceSpeed, idleSpeedThreshold, airThreshold, flipHysteresis, teleportDistance, echoAlpha, outlineWidth, outlineColorA, outlineColorB`. Defaults: `teleportDistance` = 2 world units, `echoAlpha` = 0.6. Saved at `Assets/_Game/Data/CatA_VisualConfig.asset` by the setup script.

## DO NOT

- Change colliders, `CatMotor2D` movement or jump logic, `GravityReceiver`, physics layers, Echo recording, or transport.
- Add an Animator, Animation Clips, the 2D Animation package, or any package.
- Add Cat B-specific art (Cat B uses the Cat A sheet as a stand-in), run or jump animations, or any other states.
- Network or record anything visual.
- Hand-edit `.prefab`, `.unity`, `.meta`, `.spriteatlas` files.

## ACCEPTANCE TEST (Editor only; the device check joins the next device session)

1. Play `Sandbox_Realities`. Cat A stands on the platform with its paws on the collider surface, not floating or sunk (Scene view, Gizmos on).
2. Walk left and right with the keyboard and the touch stick in Simulator. Legs cycle smoothly with no hitch at the loop point; the cat faces its direction of travel and keeps facing when stopped.
3. Push the stick halfway: the walk plays slower, and paws don't visibly skate.
4. Jump: the `Air` frame shows while airborne; the walk resumes on landing.
5. Q/E through all four gravity directions: the cat stands upright relative to each surface, walks and flips correctly, paws on the surface.
6. Place the cat in front of a near-black and a navy test quad: the outline makes the silhouette clearly readable in both.
7. Record an Echo, switch Observers, and watch it play back: the Echo cat animates, faces and orients the same as live.
8. With Observer B active, Profiler shows no presenter work for Cat A.
9. Swap in a re-export of the PNG, re-run the importer: everything still works without touching the scene.
10. Walk off a ledge into a `FallResetVolume` with a checkpoint reached: on respawn the cat shows `Idle`, keeps its facing, is upright for the checkpoint's gravity, and shows no one-frame `Air`/walk burst or flip.
11. Switch to Observer B: Cat B walks, flips and orients with the same sheet, cyan outline, on `B_Gameplay`, lit only by B's lights.
12. During Echo playback, the Echo cat is visibly translucent next to the live cat; after it ends or is cancelled, it is fully opaque again.

**EditMode:** `FlipbookMath` (loop range wrap, fps clamping, zero-speed hold), `GravityFrame` (along/up projection and rotation angle for all four directions, and 45°), and the teleport-guard decision if it is extracted as a pure function. Existing tests remain green.

**Deferred to next device session (Pixel 8a):** outline readability at phone scale, frame rate unchanged versus the placeholder, and colour banding with Compression None.

## DELIVERABLE (from Claude Code)

The required output in `CLAUDE.md`, plus: the pre-flight report, the computed PPU and pivot values, and a screenshot-worthy note of any shader pass-tag issue.

## DEFINITION OF DONE

Universal DoD + Editor acceptance 1–12 passed + EditMode green + LFS confirmed. No new decision entry: the art pipeline decision is already recorded as D-031.
