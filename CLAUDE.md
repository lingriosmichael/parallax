# CLAUDE.md — Rules for AI agents working on PARALLAX

Read this file completely before every task.

## Project

- **PARALLAX:** a landscape mobile 2D troll puzzle platformer (D-040). Puzzles combined with
  Level Devil-style ragebait: each room is a checkpoint and a door, and the room betrays you.
  Levels 11 and up (the hard band, D-065) add precision sections with tighter, still-validated
  jumps (D-069).
- **v1 is solo only (D-047):** one player, one cat (Observer A), one reality (A). Co-op (two cats,
  two realities, two players) is a later update.
- **Engine:** Unity 6 LTS · URP 2D Renderer · C# · Input System package.
- **Platform:** Android first (IL2CPP, ARM64), Landscape Left locked.
- **Networking:** none in v1. Photon Fusion 2 (Shared Mode) belongs to the co-op update
  (PAX-028–035, frozen) and is only ever used inside `Parallax.Net.Fusion`.

## Frozen co-op code

Reality B, Echo, the reality switch, the Control Station and gravity dial, cross-reality anchors,
`IRealityTransport` / `LocalTransport`, and PAX-028–035 stay in the repo **untouched**.

- **Do not delete, rename or refactor them.** They must keep compiling, and their existing tests
  must stay green.
- **Do not build new v1 features on them.** Echo and the switch are dev tooling only (D-038).
- Touch them only when a ticket names them in its allowed list. If a v1 change seems to require
  editing frozen code, stop and report.
- Shared infrastructure that frozen code also uses (`AnchorRegistry`, `ObserverSet`,
  `GravityReceiver`, `CheckpointManager`) is **not** frozen, but changes to it must not break the
  frozen features.

## Source of truth (highest first)

1. `Docs/07_DECISIONS.md` (newest entry wins)
2. `Docs/00_VISION.md`
3. `Docs/02_ARCHITECTURE.md`
4. This file
5. `PARALLAX_detailed_implementation_plan.md`
6. The original specification PDF: **reference only.** Ignore anything in it about 3D, humanoids, photo-avatars, or 3D portals.

If documents conflict, or a ticket conflicts with the architecture: **stop and report the conflict.** Do not resolve it silently.

## Working rules

- Implement **exactly one approved ticket** at a time: `Docs/0_TASKS/PAX-XXX.md` (engineering),
  `Docs/A_TASKS/` (art), `Docs/V_TASKS/` (visual/readability).
- Modify **only** files in the ticket's allowed list. If another file must change, stop and ask.
- **Never add features, systems, or "improvements" not requested.**
- **Never install or update a Unity package** without explicit approval.
- Keep classes small (~400 lines max without justification). Prefer composition.
- No new global singletons. Dependencies are wired by the composition root (`Parallax.App`).
- Do not silently swallow exceptions. Log with context.
- Put tunables in ScriptableObject configs under `Assets/_Game/Data`, not magic numbers.
- An EditMode test that opens or creates scenes restores the Test Runner's scene afterwards
  (`RouteSession.RecreateUntitledScene`), and never leaves a `Level_NNN` scene loaded (PAX-075,
  R21/R22).

## Unity file rules

- You may create/edit: `.cs`, `.asmdef`, `.md`, `.json`, test files, Editor scripts.
- **Do not hand-edit** `.unity`, `.prefab`, `.asset`, `.meta`, or anything in `ProjectSettings/` unless the ticket explicitly permits it. **Having Unity MCP does not create an exception** — see "Unity MCP" below.
- **Never delete, regenerate, or rename `.meta` files.** To move an asset, tell the developer to move it in the Unity Editor.
- Scene/prefab wiring goes in the ticket's **"YOU — UNITY EDITOR"** steps. Where setup is repetitive, write an Editor menu script (`PARALLAX/Setup/…`) the developer can run.
- **A setup menu only changes the open scene in memory.** After any setup menu runs (by you or the
  developer), the scene must be saved (Cmd+S) and `git status` must list
  `Sandbox_Realities.unity` as modified before commit. Check `git status` yourself and say in your
  output whether the scene file shows as modified.
- Use Unity 6 APIs (e.g. `Rigidbody2D.linearVelocity`, `Rigidbody2D.bodyType`). Do not use deprecated members.

## AutoSprite art pipeline

- `Docs/Art/AutoSprite_workflow.md` is the procedure for AutoSprite REST generation. Use it only
  within an approved art ticket; PAX-A06 authorizes the local tooling, not replacement of a
  Unity asset. The developer provides art prompts. Derive each target's dimensions, PPU, pivot,
  and path from the approved ticket and `Docs/Art/A02_asset_manifest.md`; for a new slot, define
  those values explicitly before generating. Compare `Docs/Art/Reference/` and existing game art.
- Keep the API key only in the local `AUTOSPRITE_API_KEY` environment variable. A placeholder is
  acceptable for offline work. Never write the real key to the repo, a spec, a log, a ticket,
  or client-side Unity code.
- The developer's spending limit is **20 AutoSprite credits per asset, including retries**.
  Check the plan and the local ledger before paid calls; preserve the same ledger/work directory
  across attempts. Do not reset or bypass it to continue generating. Use no paid operation whose
  cost cannot be bounded within that limit.
- Keep raw downloads, candidates, contact sheets, and the credit ledger outside `Assets/`.
  Check exact pixel size, alpha, proportions, pivot, and phone-scale readability against the
  references. Iterate prompts and processing within the credit cap. Copy an approved candidate
  into Unity only under an asset-specific ticket, using the existing importer and setup rules;
  never overwrite an existing Unity asset as part of a generation attempt.
- Cat animation remains a code-driven flipbook with the existing `CatSpriteImporter`: no Animator,
  `.anim` assets, or new Unity package. After a real asset completes the generation, review,
  import, and Editor/device checks, capture the tested pattern in a Codex agent skill.

## Unity MCP (MCP for Unity, pinned v10.0.0)

MCP gives you hands inside the running Editor. It changes **who presses the buttons**, not what is allowed.

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
- After any change to `Parallax.Core` or `Parallax.Gameplay`: run EditMode tests via `run_tests` / `get_test_job`. Report the **pass count** and the **names** of any failures. The baseline is the total reported by the previous committed ticket; a lower total is a regression, not a rounding error. Report it.
- Clear the console before an acceptance run so the output you report belongs to that run.
- **Known console noise.** Exclude these from "zero new warnings", but still quote anything else:
  - `MCP-FOR-UNITY: [WebSocket] Unexpected receive error: WebSocket is not initialised`
  - `Error reason is 'NoSubscription' … generators.ai.unity.com`
  - `connection.state_change … newState=Failed error=Process exited unexpectedly`
  - `RoomDeath: no RoomSafetyConfig assigned; using default HoldTicks 30.` in `Sandbox_Realities`
    only (frozen co-op sandbox carries an unconfigured `RoomDeath` from earlier trap-kit work,
    D-059) — in `Level_Solo01`/`Sandbox_TrapLab` this means the setup menu hasn't been run and is
    a real problem, not noise
  - `LevelCameraFollow` zero-frame warning (logged once) when playing `_LevelTemplate` directly:
    the template's camera frame is never baked (D-071). In a `Level_NNN` scene it means the level
    wasn't rebuilt, and is a real problem.
  - `LevelSceneLoader` error naming the active scene when pressing Restart in a scene that isn't
    in Build Settings (`_LevelTemplate`, sandboxes): expected, same as on device.
  - `LevelSceneLoader: 'Level_003' is not in Build Settings; staying on the current screen.`, printed
    by `LevelSceneLoaderTests`/`PauseLoaderTests` on purpose (their tests pass).
  - `CatPlayerSetup: Rigidbody2D on 'Assets/_Game/Gameplay/Player/Cat_Player.prefab' has no
    serialized 'config' field. Stopping without saving.`, printed by `CatColliderConfigTests` (its
    tests pass).
  - `route-hygiene warning`, logged on purpose by `RouteHygieneTests` (it checks that the route
    session's log filter forwards warnings).
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

- **v1 (solo, D-038/D-047):** only Observer A is played, as `LocalHuman`, in Reality A
  (`RoomManager.soloReality`). Observer B exists in code and in the sandbox, but no v1 feature may
  depend on it.
- The code model is unchanged, because the co-op update will need it:
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

## Gravity and movement

- **Never use or modify `Physics2D.gravity` for cats.** Cats have `gravityScale = 0`.
- **Movement numbers (D-082):** run 6 u/s, jump height 1.6 at gravity 30 (flat jump 34 ticks, 4.08 u in the harness; the validator's reach is 3.92 u, 0.75 of it 2.94 u). Levels are built to fit the cat, never the reverse.
- Each cat's gravity comes from its `GravityReceiver`. `CatMotor2D` handles movement, jumping, and grounding **relative to that direction**.
- Only `GravityReceiver` changes a cat's gravity (`SetTargetDirection`, `Flip()`).
- **Gravity is up or down only (D-037, D-048).** `GravityReceiver` quantizes every direction to
  `(0, -1)` or `(0, +1)` and applies it **instantly**; `Flip()` flips from the target. No wall
  gravity, no arbitrary angles, no tilt (D-045).
- **Movement is screen-relative (D-049):** right is always screen-right, including upside down,
  for touch and keyboard.
- The camera stays world-aligned and never rotates with gravity (D-020).

## Rooms, traps and death (v1)

- **A room is a checkpoint and a door (D-040, D-050).** Room id = checkpoint id. Only the current
  room is live. A `LocalHuman` touching the live room's door completes it and respawns the cat at
  checkpoint N+1 on the same tick; no checkpoint N+1 means level complete.
- **Traps are deterministic (D-040).** The same trigger does the same thing on every attempt. No
  randomness, no `Time.time`; timing uses ticks. The room is the difficulty, never the controls.
- **Death holds, then resets the current room (D-041, D-058).** A death freezes the room —
  `RoomLifeTick`, traps, hazards, door and bounds checks all gated off via `RoomDeath.IsHolding` —
  for `RoomSafetyConfig.HoldTicks` (default 30 = 0.6 s at 50 Hz), showing the room exactly as it
  killed the player, then runs the same reset as before: the cat respawns at the room's
  checkpoint, and every trap and room-owned anchor in that room returns to its declared initial
  value. Completed rooms are never reset. Anchor resets go out as **new requests** from the
  session authority, never as a registry rollback (D-024). `HoldTicks = 0` reproduces the old
  synchronous reset exactly. Death to regained control: **≤ 0.75 s** total, with no fade, screen
  or reload. A cat whose collider centre leaves its room's computed kill bounds dies the same way,
  cause `OutOfBounds` (D-058) — never build a room whose intended play space isn't inside its
  bounds.
- **The tick is 50 Hz (D-075):** one `FixedUpdate` of 0.02 s, owned by `ObserverSet.FixedUpdate`.
  No seconds↔ticks conversion hard-codes a rate (`60f`, `/3600f`, `.1f` u/tick). Validators,
  layout tests and new code convert through `Parallax.Core.TickTime`; existing runtime reads of
  `Time.fixedDeltaTime` (drivers, Echo, DebugPanel) stay as they are. Tests swap
  `TickTime.SecondsPerTickSource` and restore it in `[TearDown]`; nothing writes
  `Time.fixedDeltaTime`. Changing the tick rate is a new decision.
  Motor windows (coyote, jump buffer) are whole ticks derived from seconds via `TickTime` (D-077).
- **A minimal per-room death count exists (D-058)**, in `RoomDeath`/`DeathCounter`, with no UI.
  Whether/how it's shown, persisted, or turned into lives is still D-044 (undecided).
- Arrows: tell ≥ 6 ticks, harmless when stopped, lane checked by the validator (D-078).
- Every room declares a solution route and its betrayal routes; `ValidateRoutes` replays them through the real game code (D-079).
- A betrayal route Dies (killer, lead ≥ 6) or Recovers (the room completes after the reveal); non-lethal betraying surfaces need trigger coverage, and a fake platform is never a landing surface (D-080).

### Scenes, Build Settings and loading (D-072)

- `BuildSceneList.Sync()` is the only code that writes `EditorBuildSettings.scenes`. Never assign
  the list anywhere else, and never add scenes by hand in Build Profiles. After adding or
  reordering levels in `LevelListConfig`, run `PARALLAX/Setup/Levels/Sync Build Scene List`.
- Every runtime scene load goes through `LevelSceneLoader.Load(sceneName)`. Never call
  `SceneManager.LoadScene` directly, and never add an Editor-only load path.
- Every `Time.timeScale` write goes through `RunningState` (D-073). Never assign
  `Time.timeScale` anywhere else. New tests swap `RunningState.SetTimeScale` and never write the
  real `Time.timeScale`.
- Level UI is built on `_LevelTemplate` only (D-070), then `Rebuild All Levels`.
  `Level_Solo01` is frozen and gets no new UI.
- Every HUD or overlay button that can be tapped during play is an `ITouchReservedRegion`,
  appended to `TouchStickCatInput.reservedRegions` by its setup menu (D-023, D-073).
- Level UI buttons use `Navigation.Mode.None`, so keyboard and gamepad Submit and Navigate can't
  reach them (D-073).
- A button that leaves a level must not reuse `LevelsButton` unless it's meant to record a
  completion; `LevelsButton` saves progress before loading (D-073).

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

## Echo (frozen, dev tooling only)

- Echo is **state-based**: record per-tick frames plus issued requests and control samples. Play back **kinematically**. Never replay inputs through physics.
- Echo uses tick offsets, never `Time.time`.
- Puzzle sensors use **overlap queries**, not trigger callbacks, so Echo cats are detected.

## Scope — not until explicitly approved

No co-op or networking work (co-op update) · no lives or death counter (D-044) · no tilt (D-045) · no voice chat · no IAP · no matchmaking · no final art · no iOS · no analytics · no cloud saves · nothing beyond the current ticket.

## Ticket phases and agent budgets

**Phase 1 settles the design. Phase 2 measures.**

- **What Phase 1 answers:** only questions whose answer could change what gets built or trigger a
  stop condition: feasibility, files touched, reflection users, global state, definitions, and
  conflicts with the docs.
  - A number that the ticket's own code or tests will compute is not Phase 1 work. List it as
    "measured in Phase 2".
  - A wrong number found in Phase 2 is a stop condition, not a reason to compute it early.
- **Phase 1 size:** the ticket states one.
  - **None:** mechanical or docs-only changes. Go straight to tests first.
  - **Lite (the default):** one screen covering the files touched, the allowed-list check, the
    risks and the open questions.
  - **Full:** only when the ticket asks for it. That means a new system, a runtime timing or
    motor change, or a design that's expensive to undo if it's wrong. The trace is at most about
    150 lines.
- **Subagents (`pax-room-auditor`, `pax-reviewer`):**
  - Call one only when a ruling or acceptance depends on its answer.
  - Every call names:
    - the one question it answers;
    - the files it reads;
    - the ticket's out-of-scope list;
    - a budget: stop and report after about 10 min or 50k tokens, with what it has.
  - Never spend agent time on anything in the ticket's out-of-scope section.
  - In Phase 1, ask before a second auditor run.
- **Ticket authors (Architect):**
  - State the Phase 1 size.
  - Ask at most 6 Phase 1 questions.
  - Put every "confirm the number" item in Phase 2's tests, not in Phase 1.

## Required output after every task

1. Confirm the project compiles, stating **how** you verified it (`refresh_unity` + `validate_script` + `read_console`), or say clearly that you could not verify it.
2. **Files changed**, as a complete list.
3. A short explanation of what each changed script does (two sentences each).
4. **Exact manual test steps**, labeled Editor / device / two devices.
5. EditMode tests: the `run_tests` result — total, passed, failed by name — or "not run" with a reason.
6. **MCP calls that changed project state**, as a list (menu items executed, builds triggered, assets imported). Read-only queries need not be listed.
7. **Known limitations** and anything left for a later ticket.
8. Any conflicts with the docs you noticed.
9. Whether `git status` shows `Sandbox_Realities.unity` as modified, if any setup menu ran.

**Never claim something was tested on a device, or in the Editor, unless it actually was.** Say "untested" when it is. A green `run_tests` is not device validation.
