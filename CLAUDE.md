# CLAUDE.md — Rules for AI agents working on PARALLAX

Read this file completely before every task.

## Project

- **PARALLAX:** a landscape mobile 2D/2.5D puzzle adventure, 1–2 players, two cats, two realities.
- **Engine:** Unity 6 LTS · URP 2D Renderer · C# · Input System package.
- **Platform:** Android first (IL2CPP, ARM64), Landscape Left locked.
- **Networking:** Photon Fusion 2, Shared Mode. Used only inside `Parallax.Net.Fusion`.

## Source of truth (highest first)

1. `Docs/07_DECISIONS.md` (newest entry wins)
2. `Docs/00_VISION.md`
3. `Docs/02_ARCHITECTURE.md`
4. This file
5. `PARALLAX_detailed_implementation_plan.md`
6. The original specification PDF: **reference only.** Ignore anything in it about 3D, humanoids, photo-avatars, or 3D portals.

If documents conflict, or a ticket conflicts with the architecture: **stop and report the conflict.** Do not resolve it silently.

## Working rules

- Implement **exactly one approved ticket** (`Docs/TASKS/PAX-XXX.md`) at a time.
- Modify **only** files in the ticket's allowed list. If another file must change, stop and ask.
- **Never add features, systems, or "improvements" not requested.**
- **Never install or update a Unity package** without explicit approval.
- Keep classes small (~400 lines max without justification). Prefer composition.
- No new global singletons. Dependencies are wired by the composition root (`Parallax.App`).
- Do not silently swallow exceptions. Log with context.
- Put tunables in ScriptableObject configs under `Assets/_Game/Data`, not magic numbers.

## Unity file rules

- You may create/edit: `.cs`, `.asmdef`, `.md`, `.json`, test files, Editor scripts.
- **Do not hand-edit** `.unity`, `.prefab`, `.asset`, `.meta`, or anything in `ProjectSettings/` unless the ticket explicitly permits it.
- **Never delete, regenerate, or rename `.meta` files.** To move an asset, tell the developer to move it in the Unity Editor.
- Scene/prefab wiring goes in the ticket's **"YOU — UNITY EDITOR"** steps. Where setup is repetitive, write an Editor menu script (`PARALLAX/Setup/…`) the developer can run.
- Use Unity 6 APIs (e.g. `Rigidbody2D.linearVelocity`, `Rigidbody2D.bodyType`). Do not use deprecated members.

## Assembly rules

- `Parallax.Core`: pure logic, IDs, structs, registries. UnityEngine only.
- `Parallax.Gameplay`: MonoBehaviours, including `LocalTransport`.
- `Parallax.Net.Fusion`: the **only** assembly allowed to reference Photon.
- `Parallax.App`: the composition root.
- **`Core` and `Gameplay` must never reference Photon.** If a gameplay feature seems to need Photon, extend `IRealityTransport` instead and report it.

## Player model

- There are **always exactly two logical Observers: A and B.**
- **An Observer is NOT a human player or a network peer.**
- Each Observer has a cat, a reality (`RealityRoot`), a camera, logical state, and a driver whose kind is `LocalHuman`, `RemoteHuman`, `EchoReplay`, or `Inactive`.
- Gameplay must never assume Observer B is remote, or that a human controls either Observer.

## Spatial model

- Each reality has **its own geometry**, physics layer (`RealityA`/`RealityB`), sorting layers (`A_*`/`B_*`), lights, and camera.
- Reality B's root sits at the fixed offset `RealityRoot.OffsetB`. Never hard-code the offset elsewhere.
- Corresponding positions share **local coordinates** under their roots.
- **Cats never physically interact** across realities. All physics queries use the own-reality layer mask.
- Light2Ds target only their own reality's sorting layers.

## Gravity

- **Never use or modify `Physics2D.gravity` for cats.** Cats have `gravityScale = 0`.
- Each cat's gravity comes from its `GravityReceiver`. `CatMotor2D` handles movement, jumping, and grounding **relative to that direction**.
- Only `GravityReceiver.SetTargetDirection` changes a cat's gravity.

## Semantic state and networking

- **Synchronize semantic state, never presentation.** VFX, particles, animation, camera, and UI are never networked.
- **Anchors** hold logical targets. Presenters animate locally toward them.
- **Requests go up, state comes down.** Only the owner commits.
  - Cat body → the device controlling that Observer.
  - Control stream → the controlling device.
  - Anchors, puzzle phase, checkpoints → the session authority (master client in co-op; the local device in solo).
- Requests use **absolute target values**, never deltas, and carry `(Origin, Sequence)` event IDs. Commits must be **idempotent**.
- **Local input feedback never waits for the network.**
- Puzzles talk only to `IRealityTransport`. They must work unchanged with `LocalTransport` and `FusionTransport`.

## Echo

- Echo is **state-based**: record per-tick frames plus issued requests and control samples. Play back **kinematically**. Never replay inputs through physics.
- Echo uses tick offsets, never `Time.time`.
- Puzzle sensors use **overlap queries**, not trigger callbacks, so Echo cats are detected.

## Scope — not until explicitly approved

No voice chat · no IAP · no matchmaking · no final art · no iOS · no analytics · no cloud saves · nothing beyond the current ticket.

## Required output after every task

1. Confirm the project compiles (or say clearly that you could not verify it).
2. **Files changed**, as a complete list.
3. A short explanation of what each changed script does (two sentences each).
4. **Exact manual test steps**, labeled Editor / device / two devices.
5. EditMode tests added or run, if any.
6. **Known limitations** and anything left for a later ticket.
7. Any conflicts with the docs you noticed.

**Never claim something was tested on a device, or in the Editor, unless it actually was.** Say "untested" when it is.
