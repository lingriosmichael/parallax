# CLAUDE.md — Rules for AI agents working on PARALLAX

Read this file completely before every task.

Before any MCP work, query mcpforunity://instances. If instance_count is 0, stop and report immediately. Do not retry, poll, or investigate around it.
If any MCP call reports no Unity session, stop and report. A missing Editor is a precondition failure for the human, never something to work around.

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
- **Do not hand-edit** `.unity`, `.prefab`, `.asset`, `.meta`, or anything in `ProjectSettings/` unless the ticket explicitly permits it. **Having Unity MCP does not create an exception** — see "Unity MCP" below.
- **Never delete, regenerate, or rename `.meta` files.** To move an asset, tell the developer to move it in the Unity Editor.
- Scene/prefab wiring goes in the ticket's **"YOU — UNITY EDITOR"** steps. Where setup is repetitive, write an Editor menu script (`PARALLAX/Setup/…`) the developer can run.
- Use Unity 6 APIs (e.g. `Rigidbody2D.linearVelocity`, `Rigidbody2D.bodyType`). Do not use deprecated members.

## Unity MCP (MCP for Unity, pinned v10.2.0)

MCP gives you hands inside the running Editor. It changes **who presses the buttons**, not what is allowed.

### Multiple agents

- One Unity Editor and project folder are shared by every client. The agent holding the current approved ticket is the **only** agent allowed to make MCP calls that change Editor or project state.
- Other agents may make read-only MCP queries only when they cannot interfere with the ticket holder; otherwise they must not use MCP.
- Never run concurrent `refresh_unity`, `run_tests`, console-clearing, builds, or other state-changing MCP operations. A domain reload can invalidate another call, Test Runner jobs collide, and clearing the console can erase evidence another agent needs.
- Commit at every handoff between agents. There are no separate worktrees for Unity's shared project folder.

### Allowed tool groups

- **On:** `core`, `testing`, `docs`.
- **Off unless the ticket names it:** `asset_gen`, `vfx`, `animation`, `ui`, `probuilder`, `profiling`, `scripting_ext`.
- **Never call `execute_code`.** Arbitrary C# in the Editor bypasses review and leaves nothing in git. If you believe a task needs it, stop and report.
- **Never call `manage_packages`** with install/remove/update/embed. The package-approval rule above still applies. Querying is fine.
- **`manage_build`:** only when the ticket asks for a build. Never change target platform, the build scene list, or player settings on your own initiative.

### Scene and prefab wiring — unchanged

- Wiring is **delivered as an idempotent `PARALLAX/Setup/…` Editor menu script**, committed to git. Then run it with `execute_menu_item` and verify with `find_gameobjects`.
- **Do not use `manage_gameobject`, `manage_components`, `manage_prefabs`, `manage_scene`, `manage_material` or `manage_camera` as the delivery mechanism for wiring.** Scene YAML you mutated directly is not reproducible, not reviewable, and not a source of truth.
- **Read-only inspection is always allowed and encouraged:** query objects, components and serialized values to *verify* the ticket's DoD instead of asking the developer to confirm it.
- Throwaway diagnostic objects are acceptable only in a scratch scene, never in `Sandbox_Realities`, and must be listed in your output.

### Compile, tests, console

- Before claiming the project compiles: `refresh_unity` (with compilation), then `validate_script` on changed files, then `read_console`. Quote the relevant console lines.
- After any change to `Parallax.Core` or `Parallax.Gameplay`: run EditMode tests via `run_tests` / `get_test_job`. Report the **pass count** and the **names** of any failures. Baseline is **110 green** — a lower total is a regression, not a rounding error. Report it.
- Clear the console before an acceptance run so the output you report belongs to that run.
- Use `batch_execute` for long sequences of calls rather than dozens of round trips.

### What MCP does not do

- **MCP acceptance is Editor-only and never counts as device validation.** Touch input, finger lifecycle, analog feel, frame timing and thermals are device-only, and only the developer signs those off.
- If a tool call fails or the Editor is not connected, **say so plainly.** Never infer Editor state you did not read.
- If the available tool list differs from this file, report the difference before proceeding.

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

1. Confirm the project compiles, stating **how** you verified it (`refresh_unity` + `validate_script` + `read_console`), or say clearly that you could not verify it.
2. **Files changed**, as a complete list.
3. A short explanation of what each changed script does (two sentences each).
4. **Exact manual test steps**, labeled Editor / device / two devices.
5. EditMode tests: the `run_tests` result — total, passed, failed by name — or "not run" with a reason.
6. **MCP calls that changed project state**, as a list (menu items executed, builds triggered, assets imported). Read-only queries need not be listed.
7. **Known limitations** and anything left for a later ticket.
8. Any conflicts with the docs you noticed.

**Never claim something was tested on a device, or in the Editor, unless it actually was.** Say "untested" when it is. A green `run_tests` is not device validation.
