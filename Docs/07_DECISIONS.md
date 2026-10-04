# PARALLAX — Decision Log

**Highest-precedence document.** The newest entry wins. Never edit old entries. Add a new entry that supersedes them.

Format: `D-### · date · status` · Decision · Why · Supersedes

---

### D-001 · 2026-09-10 · Accepted
**Decision:** 2D gameplay with 2.5D presentation (layered sprites, URP 2D lights, parallax). Unity 6 LTS + URP 2D Renderer.
**Why:** Achieves the visual direction without the cost of 3D environments.
**Supersedes:** 3D gameplay/environments in the original spec.

### D-002 · 2026-09-10 · Accepted
**Decision:** The protagonists are two cats. The photo-to-3D-avatar pipeline is removed.
**Why:** Removes a whole secondary technology project, and the cats make failure charming.
**Supersedes:** 3D humanoids and the avatar pipeline in the original spec.

### D-003 · 2026-09-10 · Accepted
**Decision:** Android first (IL2CPP, ARM64), landscape locked to Landscape Left. iOS after the vertical slice.
**Why:** Single-platform focus. ARM64 because some recent phones can't run 32-bit apps. A fixed orientation keeps tilt axes constant and prevents screen flips.

### D-004 · 2026-09-10 · Accepted
**Decision:** Always exactly two logical Observers (A, B). An Observer is not a player or a peer. Input sources are drivers: `LocalHuman`, `RemoteHuman`, `EchoReplay`, `Inactive`.
**Why:** One gameplay codebase serves solo and co-op.

### D-005 · 2026-09-10 · Accepted
**Decision:** Spatial model: separate geometry per reality, linked only by semantic anchors. Reality B sits at a fixed world offset. Physics layers `RealityA`/`RealityB` do not collide with each other. Per-reality sorting layers for lighting. One camera per Observer. Cats never physically interact and do not see each other by default.
**Why:** Required by contradictory-world puzzles. Removes a whole class of cross-reality physics bugs. Global Light2Ds would otherwise leak between realities.
**Supersedes:** v1 plan's "each sees two cats" and "same collision skeleton under both."

### D-006 · 2026-09-10 · Accepted
**Decision:** Per-cat gravity via `GravityReceiver`. `gravityScale = 0`, and `CatMotor2D` works relative to the gravity direction from PAX-007 onward. `Physics2D.gravity` is never used for cats.
**Why:** Global gravity cannot give A and B different gravity. Building this in later would mean rewriting the motor.

### D-007 · 2026-09-10 · Accepted
**Decision:** Echo Replay is state-based: per-tick frames + issued requests + control samples, played back kinematically. It holds its final state at the end. Solo only, one at a time, 10 s cap (tunable) in v1.
**Why:** Input replay through physics diverges whenever the world differs. State replay is exact.

### D-008 · 2026-09-10 · Accepted
**Decision:** Authority: cat body → controlling device; control stream → controlling device; anchors/puzzle phase/checkpoints → session authority (Photon master client in co-op, the local device in solo). Requests are absolute and carry `(Origin, Sequence)` IDs.
**Why:** Fusion Shared Mode needs an explicit owner for shared objects. Absolute requests are naturally idempotent.

### D-009 · 2026-09-10 · Accepted
**Decision:** Gravity control input is tilt (GravitySensor → Accelerometer fallback, no gyro required) **or** a touch dial, both behind `IGravityControlInput`. The controlling cat is seated at a Control Station while steering.
**Why:** Tilting a phone you're touch-steering with is awkward. Not all phones have gyros. Accessibility. Lets us playtest which input is actually more fun.

### D-010 · 2026-09-10 · Accepted
**Decision:** Assembly definitions enforce that `Parallax.Core` and `Parallax.Gameplay` never reference Photon. `LocalTransport` and `FusionTransport` implement `IRealityTransport`.
**Why:** The compiler enforces the transport/semantics separation.

### D-011 · 2026-09-10 · Accepted
**Decision:** Build order: prove everything locally on one phone (Gate 3, PAX-024) before integrating Photon. Throwaway Photon spike (PAX-S01) in a separate project beforehand. Network Magic Test at PAX-035 (Gate 4).
**Why:** Architecture flaws are cheaper to fix without networking. The spike de-risks the scariest dependency early.

### D-012 · 2026-09-10 · Accepted
**Decision:** Claude Code edits text/code only. Unity scenes, prefabs, `.meta`, and ProjectSettings are wired by the developer, or by Editor scripts Claude writes. Every ticket separates CLAUDE (code) from YOU (Editor).
**Why:** Hand-edited Unity YAML is fragile.

### D-013 · 2026-09-10 · Accepted
**Decision:** Repo hygiene from day 1: Force Text serialization, Visible Meta Files, Unity `.gitignore`, Git LFS for binary assets.
**Why:** Must be in place before art arrives.

### D-014 · 2026-09-10 · Accepted
**Decision:** AI roles: Architect (chat, writes tickets/reviews milestones), Implementer (Claude Code, one ticket), Reviewer (fresh session, networking/state tickets). No parallel implementation agents.
**Why:** Lower coordination overhead; avoids Unity merge conflicts.

### D-015 · 2026-09-10 · Accepted
**Decision:** Sorceress is bought at Gate 4. Full art production begins only after Gate 5. The in-game style evokes the key art rather than reproducing its painterly depth. The split composition is reserved for the finale, store, and trailers.
**Why:** Don't fund art for an unvalidated mechanic. Keep the art bar realistic for phone-scale sprites.

### D-016 · 2026-09-10 · Accepted
**Decision:** Gate 5 validates co-op (10 pairs, mixed remote/same-room) and solo (6–8 players) separately.
**Why:** Solo is first-class and needs its own evidence. Same-room peeking must be observed, not assumed.

### D-017 · 2026-09-11 · Accepted
**Decision:** The project uses exactly **Unity 6.3 LTS (6000.3.24f1)**, Universal 2D (URP 2D Renderer) template. The Unity project folder is `Parallax_Game`. Everyone (human and AI) installs this exact editor version. Upgrades to a newer 6.3 LTS patch happen only as a deliberate, recorded decision, never mid-ticket. The earlier Unity 6.6 project is archived as `Parallax_OLD_6.6` for reference only and is never opened or merged into.
**Why:** 6.6 is an Update release that must be upgraded every few months to stay supported; 6.3 LTS is supported until December 2027. Stability, tutorial/forum reliability, and Photon Fusion compatibility matter more than new features for a first game. Switching was cheapest before Git, Android settings, and the first build.
**Supersedes:** Clarifies D-001's "Unity 6 LTS" with an exact version.

### D-018 · 2026-09-11 · Accepted
**Decision:** Android app identity and build settings, verified by the first successful Build and Run to a physical phone: Company Name `Regulus Games`; Product Name `PARALLAX`; Package Name `com.regulusgames.parallax` (permanent once published); Version `0.0.1`; Minimum API Level Android 7.1 (API 25, Unity 6.3 default); Target API Level Automatic; Scripting Backend IL2CPP; Target Architecture ARM64 only; Default Orientation Landscape Left. `Bootstrap` is scene 0 in the Android build profile. Builds are written to `Builds/` at the project root, outside `Assets`.
**Why:** Records the permanent and hard-to-change choices required by PAX-004. API 25 excludes practically no phones in use and needs no custom configuration.
**Supersedes:** —

### D-019 · 2026-09-11 · Accepted
**Decision:** Workflow adjustments. (1) The Phase 0 learning sandbox (L-1…L-5) is skipped; concepts are learned inside real tickets, but the "explain every script before committing" rule and a one-time Git undo exercise remain. (2) The developer receives a ticket's Unity steps in larger batches. (3) Unity wiring is done by idempotent Editor menu scripts (`PARALLAX/Setup/…`) written by Claude Code and run by the developer, wherever practical. (4) Every Claude Code ticket is reviewed by the Architect (changed-file list + key files) before commit. (5) Direct Editor control via MCP is deferred; it would require a new decision amending D-012. (6) Plan tickets PAX-008 (jump) and PAX-009 (ground detection) are combined into one ticket, PAX-008.
**Why:** Reduce manual Editor work and back-and-forth without weakening review or the D-012 file rules.
**Supersedes:** Plan §11 (Phase 0 sandbox) as a required phase; plan PAX-009 as a separate ticket.

### D-020 · 2026-09-12 · Accepted
**Decision:** The camera stays world-aligned and never rotates with gravity. Movement input remains cat-relative (perpendicular to the cat's gravity), so the on-screen direction of the move buttons changes when gravity rotates. A temporary gravity-aligned or snap-rotating camera may be revisited in Phase 5 as a deliberate disorientation effect while a Control Station is actively steering the other cat's gravity; that would need a new entry.
**Why:** Two players can only coordinate ("it's above you, go left") if both phones share one frame of reference. A rotating camera destroys that shared spatial vocabulary and risks motion sickness on a phone. World-aligned also keeps level layouts mentally mappable across gravity changes.
**Resolves:** Q-2. **Supersedes:** nothing.

## D-021 — Touch movement is a virtual stick, read cat-relative

Status: Accepted (2026-09-16)
Supersedes: the three-zone touch scheme from PAX-013
Resolves: Q-2
Closes: Q-7

Movement on touch is a floating-origin virtual stick in the bottom-left;
jump is a button on the right. CatCommand.Move is continuous in [-1, 1].

The stick is read cat-relative: Move = stick.x in the cat's own frame, so
"right" always means "forward along the surface the cat stands on",
regardless of gravity. On a wall this means pushing right walks the cat
down the screen.

Verified on Pixel 8a (PAX-013b, Gate 2 session): tested against the
ScreenRelative alternative and found tolerable in play. Cat-relative kept.

Consequence: Q-7's snap-rotating camera is closed, not deferred.
Cat-relative movement is playable as-is, so the rotating camera has no
problem left to solve. D-020 (camera stays world-aligned, never rotates)
is now confirmed by play rather than argument, and PAX-016 can build two
world-aligned cameras without hedging.

ScreenRelative remains implemented behind the `projection` enum on
TouchStickCatInput, with VirtualStick test coverage. It is not used.
Retained deliberately: if hostile anchors (PAX-020-022) later demand
faster reactions than a sandbox does, the comparison can be re-run
without rebuilding it. Note that ScreenRelative depends on D-020 — it
projects onto catRight in world space, which is only equal to screen
space while the camera never rotates.

## Q-7 — Snap-rotating camera on gravity change

Status: Closed by D-021 (2026-09-16). Not needed.

### D-022 · 2026-09-16 · Accepted
**Decision:** (1) `ObserverSet.FixedUpdate` is the only per-tick entry point for cats. It owns `Tick` and steps Observer A, then B, through their drivers. `CatMotor2D` has no `FixedUpdate`; it exposes `Step(in CatCommand, float dt)`, called only by drivers. (2) Movement input is a device-level rig (`CatInputRouter` + sources), not part of the cat prefab. `LocalHumanDriver` binds the rig to its Observer's `GravityReceiver` on Activate and resets transient input state on Activate and Deactivate. (3) `EchoReplay` and `RemoteHuman` exist as enum members only; driver classes are written in their own tickets.
**Why:** Explicit step order replaces execution-order coupling and gives Echo a single tick source. A per-cat input rig would make two cats read the same touches, and inactive cats would accumulate stale jump latches that fire on switch-back. Stubs are deferred pending Q-8.
**Supersedes:** 02_ARCHITECTURE §5.1 (per-cat `TouchCatInput`); the handoff note to apply input components to the Cat_Player prefab.

### D-023 · 2026-09-16 · Accepted
**Decision:** (1) `SoloSwitchController` is the only runtime owner of drivers and Observer cameras; `ObserverBootstrap` calls `Initialize(A)`. (2) UI that must not start movement implements `ITouchReservedRegion`: a touch that begins on SWITCH, the DBG button/debug panel, or the gravity debug buttons is never claimed by the stick or jump. (3) `DebugPanel` (backquote or DBG) only reads Observer state; it never assigns drivers.
**Why:** Two owners of drivers/cameras can leave both cats LocalHuman on one router, or show one reality while driving the other. Without reserved regions, tapping SWITCH also starts the stick or jump.
**Supersedes:** `RealityViewDebugToggle` and `ObserverDriverDebugToggle` (PAX-014/015).

### D-024 · 2026-09-16 · Accepted
**Decision:** (1) Duplicate protection is per-origin highest-applied sequence (`Sequence <= last` → Duplicate), not a bounded recent-ID set. (2) `AnchorRegistry.Commit` returns `CommitResult`; anchors must be `Register`ed with an initial value. (3) `LocalTransport` never delivers synchronously: minimum one tick, delay computed in ticks at enqueue, pumped once per tick after both Observers step.
**Why:** A recent-ID window lets a late duplicate outside the window re-apply an outdated absolute target and revert a newer change; per-origin ordering rejects it and is bounded by the number of origins. Synchronous local delivery would let gameplay code silently depend on behavior the network can't provide. Tick-based delay keeps delivery deterministic for Echo.
**Consequence for PAX-023/024:** an Echo replay must issue fresh sequences from its own origin's sequencer on every playback, never re-send recorded sequence numbers, or the second replay is dropped as Duplicate.
**Consequence for transports:** sequence tracking is per origin across all anchors, so a transport must deliver each origin's requests in send order. If an origin's later request for anchor Y arrives before its earlier request for anchor X, X is dropped as Duplicate. Photon reliable RPCs preserve this. LocalTransport only breaks it when latency is lowered while items are pending, which is debug-only.
**Supersedes:** 02_ARCHITECTURE §7.1 "bounded recent-ID set" and `bool Commit`.

### D-025 · 2026-09-16 · Accepted
**Decision:** (1) Interaction is driven by the Observer's `CatCommand`: `LocalHumanDriver` calls `CatInteractor.Step` with the command it gave the motor. Interactables never read input devices. (2) Interactables never touch the transport, registry, or manifestations. They receive an `IAnchorRequester` from the interacting cat, which stamps the Observer's origin and a sequence from the device's single `EventSequencer` on `TransportHost`. (3) Presenters live at scene root, outside both RealityRoots; they register their anchor, snap manifestations on enable, and forward committed values. Manifestations animate locally in FixedUpdate and may carry colliders. (4) Anchor IDs come from `AnchorDefinition` assets (1..65534; 65535 reserved for debug). (5) Gameplay references the abstract `TransportHost`, never `LocalTransportHost`.
**Why:** Reading input in the interactable would let the active human trigger an inactive cat's zone. A per-Observer requester is the interception point Echo recording needs (§8.3) and keeps origin and sequencing out of puzzle code. Two sequencers issuing the same origin would collide under D-024. Presenters outside the roots keep the isolation rule (no A↔B references).
**Supersedes:** 02_ARCHITECTURE §7.2 "issue AnchorRequests through the transport"; the CLAUDE.md line "Puzzles talk only to IRealityTransport" (narrowed, not removed).

### D-026 · 2026-09-17 · Accepted
**Decision:** Solo stays in v1. Echo (PAX-023/024) and solo as a first-class mode are in scope.
**Why:** Solo widens the audience beyond players with a partner, and the Echo machinery also serves Type B and Type C puzzles.
**Consequence:** Every hostile anchor must remain solvable against a ≤10 s Echo. SWITCH is a player-facing feature, not only a dev tool.
**Resolves:** Q-8.

### D-027 · 2026-09-17 · Accepted
**Decision:** Echo v1 as built: (1) `EchoSession` records the active Observer after RECORD; frames are captured on `ObserverSet.Stepped`, anchor requests via `CatInteractor.Requested` as (TickOffset, Anchor, Target). (2) Playback is `EchoReplayDriver` (kinematic, never calls the motor), advanced by tick count; events are re-issued through an Echo-origin `AnchorRequester` with fresh sequences. (3) One Echo at a time: RECORD is unavailable while the other Observer is EchoReplay; RECORD while recording discards. (4) The 10 s cap stops capture but keeps the recording for SWITCH. (5) End of playback holds the last frame; cancel restores Dynamic, keeps position, and sets gravity from the current frame. (6) `SoloSwitchController` remains the only code that assigns drivers.
**Why:** Tick-indexed state replay is exact and independent of physics; recording requests instead of interactions keeps the Echo working even if interactables change; a single interception point on the cat covers every interactable.
**Supersedes:** 02_ARCHITECTURE §8.2 `EchoFrame` field list and `EchoAnchorEvent.Request` (refined; the removed fields return with animation/hold interactions).

### D-028 · 2026-09-17 · Accepted
**Decision:** (1) Puzzle sensors run an `OverlapBox` query on `ObserverSet.Stepped` with their reality's mask and count that reality's cat only when its driver is `LocalHuman` or `EchoReplay`. Inactive cats do not press sensors; RemoteHuman cats are not counted on this device. (2) Sensors request on state change only, through their own `AnchorRequester` with origin `EventOrigins.Sensor(reality)` (= that reality's Human origin) and the device's single sequencer. (3) Sensor requests are not recorded by Echo; during replay the sensor detects the Echo body. (4) Each anchor has exactly one writer (validator-enforced).
**Why:** If Inactive cats pressed plates, every sustained puzzle could be solved by switching away while standing on the plate, making Echo pointless. In co-op a reality's cat is simulated only on its owner's device, so only that device may report its sensors, and its Human origin already belongs to that device's sequencer (a shared System origin from two devices would collide under D-024). Recording sensor requests as well would double-issue them on replay.
**Consequence:** Level design: a cat left Inactive is scenery for sensors but still collides and falls. Networking (PAX-033): sensors must be disabled for realities this device does not own.

### D-030 · 2026-09-17 · Accepted
**Decision:** (1) Checkpoints use shared checkpoint IDs across both realities. (2) A fall respawns only the cat that fell, at its Observer's spawn for the last reached checkpoint; the other cat is unaffected. (3) There is no world rewind: anchors, puzzle phase and the other cat's state are not restored on a fall. (4) Checkpoints v1 (PAX-036) is pulled ahead of networking (PAX-028–035).
**Why:** One player's mistake should not undo the partner's progress. Leaving world state alone keeps respawn simple and avoids rolling back committed anchors. Building checkpoints before Photon means networking is designed around an existing checkpoint model rather than retrofitted.
**Consequence:** Full state restore (anchors, puzzle phase, gravity) belongs to the later checkpoint snapshot work, not to fall respawn.

### D-029 · 2026-09-17 · Accepted
**Decision:** (1) Gravity control input is `IGravityControlInput` (Core); the touch dial ships first, tilt plugs into the same interface later. (2) A cat sits at a `ControlStation` via interact; `LocalHumanDriver` filters its motor command (no move, no jump) while seated and releases the seat on Deactivate. (3) Stations publish control samples only through the seated cat's `CatInteractor.PublishControl`, only when the value changes, never on sitting down; the target is the other Observer. (4) Each cat's `GravityControlReceiver` applies samples for its Observer through `GravityControlMapping` (positive = clockwise, per-receiver max angle and optional snap) and holds the last direction. (5) Echo records `ControlPublished` and re-publishes due control events during replay.
**Why:** One interception point on the cat serves anchors and streams, so Echo records both the same way. Publishing on change keeps recordings small and means sitting down does not reset the other cat's gravity. Releasing the seat on Deactivate means switching away never leaves a stale occupant.
**Consequence:** Tilt must only implement `IGravityControlInput` and be selectable; station, stream, receiver, and Echo remain unchanged.

### D-031 · 2026-09-17 · Accepted
**Decision:** Amends D-015. The art pipeline test (plan Phase 7) starts now, in parallel with
code, limited to one cat and a small kit per reality. Concept art: ChatGPT (existing concepts are
the style reference). Character animation: AutoSprite (free tier / Starter month as needed).
Sorceress is dropped. Placeholder-quality in-game art is allowed; full art production still
begins only after Gate 5.
**Why:** Concepts already exist and are approved. AutoSprite produced a usable quadruped walk at
near-zero cost, so the reason to wait for a paid tool at Gate 4 is gone. Proving import, pivots,
lighting and readability early removes risk without committing to production art.


### D-032 · 2026-09-18 · Accepted
Checkpoints restore cats, not the world. On respawn, gravity is snapped
to the checkpoint's stored direction and then overridden by the
respawned cat's current control-stream value if one is held; the
receiver re-asserts locally, without a republish. Control-stream values
are live state owned by the controlling device and are not part of
CheckpointSnapshot. Consequence: a hostile dial can cause repeated
respawns, which is accepted as legible co-op failure under Vision
pillar 3 and revisited at Gate 5.

D-033 · 2026-09-18 · Accepted
Decision: MCP for Unity is committed to the repo as dev tooling, pinned to v10.2.0 on both the Unity package and the Python server. Its MCPForUnity.Runtime assembly is not platform-constrained upstream; this is accepted unfixed. Gate 3 adds a one-time check that the assembly is absent from the Android player build.
Why: Embedding the package to constrain the asmdef means owning a vendored fork of a peripheral tool. Nothing in Assets references the runtime assembly, so IL2CPP managed stripping should remove it; the cost of being wrong is APK size, not correctness.
Consequence: If the Gate 3 check finds the assembly shipped, embed the package and set includePlatforms: ["Editor"], recorded as an amendment.

### D-034 · 2026-09-18 · Accepted
**Decision:** Visual facing is presentation-owned. `CatVisualPresenter` derives facing from motion for live and Echo cats and is the sole writer of Visual scale. Gameplay holds no facing state; `EchoFrame.FacingRight` is retained for format stability, no longer written or read.
**Why:** Facing is purely visual and derivable from motion; two writers fought during replay.
**Consequence:** Any future gameplay need for facing (for example, directional interact) must come from motor/command state, never from Visual.

### D-035 · 2026-09-21 · Reserved
**Reserved** for the Gate 3 verdict (PAX-037). Gate 3 is redefined by D-038.

### D-036 · 2026-09-21 · Accepted
**Decision:** Cat animation state is selected in code: pure `CatAnimStateMachine` in `Parallax.Core`, per-state `Sprite[]` clips on `CatVisualPresenter`. No Unity Animator, Animator Controllers or `.anim` assets. All cat sheets share Walk's PPU and pivot, enforced by `CatSpriteImporter`.
**Why:** Deterministic, EditMode-testable, reviewable in a diff, and cannot fight the gravity-aligned body rotation. A shared pivot is what keeps frames aligned across states.
**Supersedes:** Single-frame `idleFrame`/`airFrame` selection from PAX-A01.

### D-037 · 2026-09-21 · Accepted
**Decision:** Gravity is up or down only. No wall gravity. `GravityDial` becomes a flip, `GravityDebugControl` a single flip key, and the spawn-point validator rejects sideways directions. D-020 (world-aligned camera) stands.
**Why:** With a world-aligned camera and world-down environment art, a cat on a wall contradicts everything around it. Upside-down reads as a clear idea; sideways does not.
**Supersedes:** Four-direction gravity (PAX-010, PAX-026 dial). **Reopens D-021:** cat-relative movement was chosen with walls in play; with walls gone, `ScreenRelative` must be re-tested on device before the gravity ticket is built.

### D-038 · 2026-09-21 · Accepted
**Decision:** Solo is one cat in one reality. Co-op is the two-cat, two-reality mode and remains mandatory two-player. Echo and the reality switch are removed from the player's game and kept as dev-only tooling behind `defineConstraints`. Gate 3 is redefined as local validation of both-cat mechanics using that tooling, still on one phone.
**Why:** Echo does not explain itself and solo does not need it. Keeping it as tooling preserves one-phone testing of cross-reality mechanics until networking lands at Gate 4.
**Supersedes:** Q-8 (solo in v1 via Echo); the solo half of D-004's rationale; D-007 and D-026–D-028 as player-facing features. D-035 stays reserved for the Gate 3 verdict.

### D-039 · 2026-09-21 · Accepted
**Decision:** Solo mode is a rage platformer mixing troll traps (Level Devil style) with precision platforming (Getting Over It / Jump King style).
**Why:** Gives solo its own identity instead of a reduced co-op.
**Open:** Room structure and failure rules — see Q-11. Q-9 (nine lives) is now tied to this.

### D-040 · 2026-09-21 · Accepted
**Decision:** Parallax is a troll puzzle platformer: puzzles combined with Level Devil-style ragebait. The unit of play is a **room**: one checkpoint, one door per cat. A room fits on one screen or close to it and takes roughly 10–20 s once its solution is known. Traps are deterministic: the same trigger gives the same outcome on every attempt. The room is the source of difficulty, never the controls. **Solo:** one cat, one reality, a room of local traps and puzzle logic. **Co-op:** two humans only, two realities. Co-op is harder than solo because each player's actions can trigger traps in the partner's reality through anchors. A room is complete when the solo cat enters its door, or in co-op when both cats have entered theirs.
**Why:** Every death should teach one rule and be blamed on the room, which is what makes an instant retry feel fair. Precision platforming makes the controls the difficulty and works against that. Puzzles give the rooms a solution to learn instead of an execution to grind.
**Supersedes:** D-039 (the precision-platforming half, Getting Over It / Jump King). **Resolves:** Q-11 (room structure). `00_VISION.md` §1–3 must be rewritten to match.
**Consequence:** Anchors in co-op rooms are designed as threats, not gifts. Every level-design ticket describes each room as setup → obvious route → betrayal → learned solution.
**Amended by:** D-069 (precision sections in hard-tier levels; larger rooms allowed for them).

### D-041 · 2026-09-21 · Accepted
**Decision:** Death resets the room. The dead cat respawns at the room's checkpoint, and every trap and anchor owned by that room returns to its initial armed value. The time from death to regained control is at most 0.75 s, with no fade, screen or reload. Anchor resets are issued as **new** requests with fresh sequences from the session authority (the local device in solo, the master client in co-op); the registry is never rolled back (D-024). Completed rooms are not reset.
**Why:** A trap that stays fired cannot be relearned or tested again, which breaks the troll loop. Resetting through new requests keeps D-024's per-origin ordering valid and works unchanged under Photon.
**Supersedes:** D-030 (3) "no world rewind" and D-032 "checkpoints restore cats, not the world", for room-scoped state.
**Consequence:** This is the full restore that D-030's consequence required before the first irreversible anchor; traps are irreversible anchors. Every trap and room-owned anchor declares an initial value and registers with its room.

### D-042 · 2026-09-21 · Proposed
**Decision:** In co-op, any death resets the room for both players: both cats respawn at their checkpoints, both realities re-arm, Control Stations release their seats, and both cats take their checkpoint gravity.
**Why:** Rooms are short, so there is little partner progress to protect. A cross-reality trap's trigger lives in one reality and its effect in the other, so resetting only one side leaves the room in a state it was not designed for. A shared reset also drives the "you killed us" comedy that co-op is built on.
**Alternative considered:** reset only the fallen cat and its own reality (D-030 (2)). Rejected because a partner-triggered trap would re-arm without its trigger re-arming.
**Supersedes:** D-030 (2) for co-op; D-032's control-stream override on respawn.
**Status note:** Awaiting the developer's confirmation. Change to Accepted or Rejected before PAX-040 starts.

### D-043 · 2026-09-21 · Accepted
**Decision:** Cross-reality traps change state; they never demand timing. A trap caused from the other reality may remove a floor, close a door or flip gravity, but it must not require a reaction inside the transport's latency window. Design budget: assume up to 500 ms of variable latency. Timing-precise traps are local only.
**Why:** Network latency varies. A cross-reality trap with a tight window would feel random, and randomness breaks the deterministic contract of D-040.
**Consequence:** Level design and the AnchorValidator reviews check every hostile anchor against this rule.

### D-044 · 2026-09-21 · Accepted
**Decision:** No lives, unlimited retries. A death resets the current room (D-041) and costs
nothing else: no level fail state, no lives counter. The game keeps a per-room death count; its UI,
and whether it persists between sessions, are decided with the ticket that shows it. Resolves Q-9:
there is no nine-lives mechanic.
**Why:** The rage-platformer loop depends on instant, free retries. Failure stays cheap and funny
(Vision pillar 3), and a death count gives the pressure and bragging rights without punishing the
player.
**Consequence:** Difficulty comes only from room design (D-050, D-053), never from a resource the
player can run out of. Rooms can be tuned for many deaths per room on a blind run.

### D-045 · 2026-09-21 · Accepted
**Decision:** Tilt is removed. Gravity control at a Control Station is a touch flip only (D-037). Plan PAX-025 and ticket PAX-038 (tilt) are cancelled; the number PAX-038 is retired and not reused. `IGravityControlInput` stays as the station's input seam; whether it simplifies to a flip is decided in the gravity ticket.
**Why:** With binary gravity (D-037), a flip is a button. Tilt would add sensor noise, calibration and a second input mode for no gameplay gain.
**Supersedes:** D-009 (tilt as an option) and D-029 (1) "tilt plugs into the same interface later". **Resolves:** Q-3.

### D-047 · 2026-09-21 · Accepted
**Decision:** v1 is solo only: one cat, one reality (A). Co-op is a later update. Until then, tickets, tests and device sessions cover only the solo cat and its reality. Existing two-reality code (Reality B, anchors across realities, Echo, switch, Control Station, transport) stays in the repo untouched and is not extended; nothing is deleted.
**Why:** Finish one mode end to end before starting the second. Solo needs no networking.
**Defers:** co-op as a v1 feature (co-op half of D-038), networking (PAX-028–035), D-042 (stays Proposed until the co-op update), PAX-043 (hostile anchor). D-043 remains a rule for the co-op update.
**Consequence:** Gate 3 is the solo device verdict (D-035). Solo features take an `ObserverId` rather than hard-coding A, and must not break the two-reality sandbox.

### D-048 · 2026-09-21 · Accepted
**Decision:** Gravity is vertical-only in code. Every direction passed to `GravityReceiver` is quantized to `(0, -1)` or `(0, 1)` (`|y| ≤ 0.1` keeps the current side) and applied instantly, with no turn speed. `GravityReceiver.Flip()` is the flip; the debug control is a single key (Q). Validators reject any serialized non-vertical gravity.
**Why:** D-037. Turning at 360°/s passed through sideways gravity for ~0.5 s on every flip, pushing the cat sideways, and the 180° turn direction was ambiguous. Quantizing at the receiver means no caller, including untouched co-op code, can produce sideways gravity.
**Supersedes:** The turn-speed behaviour in `02_ARCHITECTURE.md` §6 (PAX-010).
**Consequence:** The Control Station dial (co-op, untouched per D-047) can only produce down with its current mapping; the co-op update replaces it with a flip.

### D-049 · 2026-09-21 · Accepted
**Decision:** Movement is ScreenRelative for touch and keyboard: pushing right moves the cat right on screen in both gravities.
**Why:** D-021 chose cat-relative because on a wall ScreenRelative has no useful axis. D-037 removed walls, so that case is gone. Screen-relative matches the genre convention (VVVVVV) and removes the “controls reversed” death that ragebait must not have (D-040).
**Supersedes:** D-021’s cat-relative reading. The virtual stick itself (floating origin, jump button) stands.
**Consequence:** Confirm on device in PAX-037 when it runs.

### D-050 · 2026-09-21 · Accepted
**Decision:** (1) A room's id is its checkpoint id; a room is its `RoomDoor` plus the checkpoint markers with that id. (2) Solo rooms live in one reality, set on `RoomManager.soloReality`; doors elsewhere are invalid. (3) Only the current room (`CheckpointManager.Current`) is evaluated. (4) A LocalHuman touch completes the room once; on the same tick the cat is respawned at checkpoint N+1, or, with no door N+1, the level completes.
**Why:** D-040 defines a room as a checkpoint and a door. Reusing checkpoint ids gives rooms progress and respawn for free. Completing on touch with an instant move keeps the troll loop fast.
**Consequence:** Walking into a later room's checkpoint marker skips the current room, so level design keeps room N+1's marker unreachable before room N's door. Co-op rooms (doors in two realities) are designed in the co-op update.

### D-051 · 2026-09-21 · Accepted
**Decision:** Trap kit v1 (PAX-042). Traps are ticked only by `RoomManager` in the live-traps phase
(traps → hazards → door, each list snapshotted just before its phase). Time is in integer ticks;
delay N fires exactly N ticks after the trigger tick. Motion is a pure function of ticks since
firing. Only the `LocalHuman` cat triggers traps, via own-reality overlap queries on collider
bounds. Kills go only through `RoomDeath`. Room reset restores authored state. Traps fire once per
room life unless explicitly re-armable. Kit v1 uses no anchors; presenter snap-vs-animate on reset
is decided with the first anchor-using trap.
**Why:** Deterministic, ordered, resettable traps make every room learnable (D-050), and one tick
order means a trap, a hazard and the door can never disagree about the same tick.

   ### D-052 · 2026-09-21 · Accepted
   **Decision:** Cat hitbox follows the art body (PAX-A04, resolves Q-10). Both cats use a
   horizontal `CapsuleCollider2D` 1.0 × 0.56 at offset (0, −0.12), covering torso and legs (not
   tail or ear tips), with its bottom on the paw line at −0.4 from the root. The values live only
   in `CatMotorConfig`; `PARALLAX/Setup/Configure Cat Player` applies them to `Cat_Player.prefab`,
   and scene cats inherit them with no overrides. With gravity up the root rotates 180°, so one
   offset holds in both directions. Grounding casts the collider itself (`Rigidbody2D.Cast`) and
   spawns place the root, so neither needs size constants.
   **Why:** Players judge hits by the art; in a troll platformer a hit that looks clear must be
   clear (D-050).
   **Consequence:** Hazards and rooms (PAX-043) are authored against the art. Changing the
   collider size alone moves nothing else; changing the paw line (−0.4) moves spawns and the
   visual seat.

### D-053 · 2026-09-21 · Accepted
**Decision:** Room grammar v1 (PAX-043).
- No tutorial or teaching rooms: every solo room is a troll room from the start.
- Rooms stack betrayals: the obvious fix for one trap leads into the next. Players learn by dying.
- Betrayals are disguised as their surroundings; tools the player must use are visible.
- No room can soft-lock: every state is either completable or ends in a death that resets the room
  (D-041).
- Rooms are physically separate and joined only by door teleports, so the skip rule (D-050) holds
  by construction.
- Rooms are scene data scaffolded by a setup menu and then tuned in the scene, not gameplay code.
**Why:** Players of the genre already know it, so teaching rooms only slow them down. Stacked,
disguised betrayals are what make the troll loop work, and a soft-lock would break the
cheap-retry promise of D-044.
**Consequence:** New rooms are reviewed against these rules. The first playtest found the
PAX-043 rooms too easy; PAX-044 raises the density to 4–6 chained betrayals per room.

### D-054 · 2026-09-22 · Accepted
**Decision:** Solo room layouts are data. `SoloRoomsLayout` is the single source of truth for
geometry and every trap setting; the scene is always rebuilt from it (delete `Room_N`, rerun the
setup menu, save). Tuning means editing the layout data; Inspector tweaks in Play mode are for
finding a value only and are never saved to the scene. Supersedes D-053's clause that rooms are
tuned in the scene after scaffolding; the rest of D-053 stands.
**Why:** Layout tests only mean something if the scene equals the data they check. Scene-side
tuning would silently diverge from the tests and be lost on the next rebuild.

### D-055 · 2026-09-22 · Accepted
**Decision:** Trap kit v2 (PAX-045).
(1) Trigger source. Every trap fires from exactly one source: Overlap (its own trigger box, as
in v1) or Chain (another trap in the same room firing). A trap "fires" at the tick its effect
starts: trigger tick + delay for Overlap, source fire tick + delay for Chain. A chained trap's
delay is at least 1 tick, so tick-list order never changes the result. Chains are acyclic and
stay inside one room; setup and tests reject cycles and cross-room links.
(2) Repeat mode. Once (v1 default): fires once per room life. Rearm: after firing, the trap
returns to its authored state when its cooldown ends (snap, no return animation in v2) and can
fire again from its trigger source. Periodic: fires every P ticks, counted from room start
(tick 0 of the room life) plus a phase offset; it has no trigger. Motion is a pure function of
ticks since the latest fire.
(3) Moving trap. A new trap moves a body from its authored pose by an offset over M ticks, holds
H ticks, and optionally returns over R ticks. Kind Hazard: kills on overlap, tested against its
pose computed for that tick, not physics state. Kind Solid: kinematic geometry moved with
MovePosition; a cat whose collider overlaps the solid's tick pose shrunk by crushDepth on every
side dies through RoomDeath (a crush).
(4) Room reset restores every trap's authored pose, arming, chain state and repeat counters.
**Amends:** D-051's "fire once per room life unless explicitly re-armable": repeat mode is the
explicit mechanism. `GravityFlipTrap`'s existing `rearmOnExit` stays as is.
**Why:** Level Devil rooms mutate as you solve them. Chains, moving and repeating traps are the
three gaps PAX-044 found; defining them on ticks keeps every room deterministic and learnable
(D-040, D-050).

Cooldown means ticks from a fire until the trap returns to its authored state, for Rearm and
Periodic alike. A Periodic trap needs cooldown < period. A MovingTrap's cooldown is at least
M + H + R. An Overlap trap re-fires while armed if the cat is inside its trigger (presence,
sampled each tick, as in v1); leaving and re-entering is not required. A Rearm chain target
that is not armed when its source fires ignores that fire.

### D-056 · 2026-09-22 · Accepted
**Decision:** Rooms v3 layout rules for trap kit v2 (PAX-046).
(1) Timing slack. Every survive case that depends on timing (periodic windows, chain delays,
moving traps, collapse delays) leaves at least 12 ticks (0.2 s at 60 Hz) of slack beyond the
minimum the cat needs, computed from the measured movement numbers. Where the obvious move is
to stop and wait (periodic hazards, closing walls), the minimum is computed from rest, with the
measured acceleration, not at full run speed. Touch input on a phone cannot be tighter than that.
(2) Platforms. Every jump the solution requires is a RequiredJump in layout data and passes the
reachability contract (D ≤ 0.75 reach), including jumps between platforms at different heights.
Difficulty comes from what the room does, never from a jump near the limit.
(3) Moving solids. A moving Solid's swept path (authored pose to authored pose + offset, full
size) overlaps no fixed geometry; touching is allowed. A cat is only ever crushed against
geometry the layout names as the crush partner. A Solid that carries the cat upward launches it
when it stops (rise = v²/2g, v = its upward speed): the launch box, from the Solid's stopped
pose up by the rise plus the cat height, touches no hazard, and a launch is never a betrayal.
(4) Disguise. Before it fires, a betraying trap looks like ordinary level geometry or like
nothing: collapsing floors and moving Solids use the floor/wall look, hidden spikes are
invisible, chained traps have no visible trigger. Honest hazards (visible spikes, visibly
moving hazards) are allowed and are not counted as betrayals.
(5) Learnability. After a death the player can see what killed them: every betrayal leaves a
visible result (a gap, revealed spikes, a moved block) until the room resets.
**Why:** Kit v2 lets rooms mutate (D-055). These rules keep the mutation fair on a phone:
enough slack for touch, platforming that is never the hard part, no physics surprises, and
deaths that teach (D-040, D-053).
**Amended by:** D-069 (rules (1) and (2) use tier thresholds inside marked precision sections).

### D-058 · 2026-09-22 · Accepted
**Decision:** Death hold and out-of-bounds kill (PAX-047).
(1) Death hold. A death freezes the room for HoldTicks (default 30 = 0.5 s at 60 Hz, in a config
asset) before the existing reset runs. During the hold: no trap steps and RoomLifeTick does not
advance (the room shows the exact state at the kill); the cat is frozen where it died and takes
no input; input given during the hold is discarded, so nothing pressed during the hold (e.g. a
buffered jump) acts after the reset; further kills, door touches and trigger entries are
ignored. The death counts once, at the kill. HoldTicks 0 reproduces today's synchronous reset.
(2) Out-of-bounds kill. Each room has kill bounds computed from its layout (every element's
bounds plus a margin, default 2 u). The room's checkpoint and door always lie inside its bounds.
A cat whose centre leaves the bounds of its current room is killed through the normal death
path (hold, then reset) and a warning names the room and position, because it means the layout
has a hole.
(3) Learnability. With the hold, D-056 (5) applies again as written: every betrayal's result is
visible after the death until the reset. D-057's 6-tick visible lead before a kill stays in
force.
**Why:** Deaths must teach (D-040, D-053): the player needs to see what killed them. And no
layout mistake may soft-lock a room.

---

## Open questions (to be resolved by playtest → new D-entries)

- **Q-1** Partner presence hint: shimmer or nothing?
- ~~**Q-2** Camera rotates with gravity, or stays world-aligned?~~ Resolved by D-020: world-aligned.
- ~~**Q-3** Tilt or dial as default?~~ Closed by D-045: tilt removed; gravity control is a touch flip.
- ~~**Q-4** Solo gravity adaptation: persistent setting, Echo choreography, or both?~~ Closed by D-038: solo has one cat and nothing to steer.
- ~~**Q-5** Echo length and looping.~~ Moot as a player feature by D-038; Echo is dev tooling only.
- ~~**Q-8** Is v1 co-op-only?~~ Closed by D-026, superseded by D-038: solo is one cat in one reality.
- ~~**Q-7** Should a temporary gravity-aligned/snap-rotating camera be introduced in Phase 5 as a deliberate disorientation effect while a Control Station actively steers the other cat's gravity?~~ Closed by D-021: not needed.
- **Q-9** ~~Nine lives~~ → resolved by D-044 (no lives, unlimited retries, per-room death count).
- **Q-10 · Cat collider height vs. art silhouette.** Collider is a horizontal capsule, 1.2 × 0.8. Art PPU (196.667) is derived from Walk frame-0 width over collider length, so the art matches length by construction but not height: Walk draws 0.580 u tall, Idle 0.656, Rise 0.702, Fall 0.524, Land 0.447. The standing cat leaves ~0.14 u of empty collider above its back, so ceilings and head bumps read as gaps. Proposed: shrink collider height to ~0.62 as PAX-A04, after A03 Play acceptance and before PAX-037. `CatVisualSetup` places Visual at the collider's bottom edge, so the setup menu must be re-run after any collider change. Blocks level design.
- ~~**Q-11 · Solo room structure.**~~ Closed by D-040 (room = checkpoint + door, deterministic traps) and D-044 (no lives, pending confirmation). Precision platforming, first excluded by D-040, is allowed in hard-tier precision sections by D-069.

### D-057 · 2026-09-22 · Accepted
**Decision:** Amends D-056 (5). Death reset stays synchronous (RoomDeath.Kill), so nothing is
visible after a death. Learnability is therefore met before the kill: every betrayal that can
kill shows its visible change (a gap opening, spikes revealed, a block or Solid moving, a door
moving) at least 6 ticks (0.1 s at 60 Hz) before it can kill. The other betrayals keep D-056 (5)
as written: their result stays visible until the room resets. A death hold that shows the fired
room state after a death is a separate runtime ticket (PAX-047); once it lands, this rule is
reviewed.
**Why:** The death frame is never rendered, so "visible after death" was unachievable with the
kit as built. 6 ticks is long enough to see and shorter than a human reaction (≈ 15 ticks), so
a betrayal still kills the first time but the player saw what did it.

### D-059 · 2026-09-22 · Accepted
**Decision:** PAX-047 (D-058) as built. (1) `DeathHold` (`Parallax.Core`) is the pure hold
countdown: `Begin()` at the kill tick, `Step()` once per subsequent tick, reporting
`Live`/`Holding`/`ResetNow`. `RoomDeath` owns one `DeathHold` plus a new `DeathCounter`
(`Parallax.Core`, per-room, never `IRoomResettable` so a room reset never clears it — this is
the "minimal death counter" D-058 pre-approved; it has no UI and does not resolve D-044). (2)
`RoomDeath.Kill` counts the death and either runs the reset immediately (`HoldTicks` 0,
byte-for-byte the old synchronous path) or freezes the cat and defers the reset to
`StepHold()`. The reset (`checkpoints.Respawn`, trap/anchor reset, `Died`) is unified into one
`PerformReset()` run either way, so `Died` now fires at reset completion — synchronously for
`HoldTicks` 0, at hold-end otherwise — carrying the original kill tick and cause.
`RoomManager.OnDied` is unchanged and remains the single `roomLifeStartsNextStep` setter.
(3) `RoomManager.OnStepped` gates on `RoomDeath.IsHolding` at the top: while holding, `RoomLifeTick`
does not advance and the trap/hazard/bounds/door phases do not run at all that tick.
(4) `CatMotor2D.Freeze()`/`Unfreeze()` hold `RigidbodyConstraints2D.FreezeAll` + zero velocity,
restoring the exact pre-freeze constraints; `Step()` no-ops while frozen, so a command still
drained by the (unchanged) input router that tick has no effect. `CatRespawn.RespawnAt` now
unfreezes (no-op unless frozen) before every respawn — co-op/dev respawns are unaffected since
`Freeze` is only ever called from `RoomDeath.Kill`'s solo branch. (5) Out-of-bounds: each room's
kill bounds are computed once, at setup time, from `SoloRoomsLayout`/`TrapLabLayout` data —
the union of every element's AABB (both poses for `MovingTrap`/`FallingBlock`; the room's `Door`
element's retreated pose for a `DoorRetreat` pairing) plus a margin — and serialized as a
world-space `RoomBoundsEntry[]` on `RoomManager`, via a new `RoomSafetyConfig` asset
(`HoldTicks` default 30, `BoundsMargin` default 2) created/assigned by `SoloRoomsSetup`/
`TrapLabSetup`. The runtime check compares the cat's collider **world bounds centre**
(`Collider2D.bounds.center`, not `transform.position + offset`, since the offset's world
direction flips with gravity) against the room's bounds on **x/y only**
(`RoomManager.ContainsXY`) — room bounds are always `z = [0,0]`, so a plain `Bounds.Contains`
would reject any point whose z isn't exactly 0; the cat's z is 0 in `Level_Solo01` today, so
this was latent, not live-broken, but must stay xy-only regardless. A missing `RoomSafetyConfig`
on `RoomDeath` logs a `Debug.LogWarning` (not an error) and falls back to `HoldTicks` 30 —
downgraded from error because `Sandbox_Realities` (frozen co-op) also carries a `RoomDeath`
component, from earlier trap-kit work, with no config and no PAX-047 setup menu ever run
against it; that scene is untouched and out of scope.
**Why:** Deaths must teach (D-040, D-053) and no layout mistake may soft-lock a room (D-058).
Reusing one `PerformReset` for both `HoldTicks` 0 and N keeps the two paths from silently
drifting apart. Bounds baked at setup time (not computed at runtime) keep the per-tick check a
plain comparison with no allocation or reality-space conversion.
**Consequence:** `Sandbox_Realities` now logs a (harmless, expected) warning instead of an
error on load, until someone wires a `RoomSafetyConfig` there or removes the leftover
component — not this ticket's concern.

D-060 · 2026-09-22 · Accepted

Decision: Door clearance (PAX-048). A door is never entered over a hazard. In every room, the Door element's footprint, both authored and retreated (including the retreat's swept path), keeps at least 0.1 u (one tick at run speed) from every volume that can kill, in every pose and swept path and every armed state. A grounded cat can always touch the door without touching a hazard. Enforced by a general layout test over SoloRoomsLayout and TrapLabLayout.
Why: Room 3's retreat put its door over ExitSpikes. The only grounded touch was a 0.05 u sliver, and an airborne touch depended on where the cat entered a flip zone. The door ends a room's troll; reaching it must not be the precision test (D-040, D-056).
Amends: D-056 (adds the door rule).

D-061 · 2026-09-22 · Accepted

Decision: Death count UI (PAX-049, completes D-044). Per-room deaths are shown once, on a level-complete screen: one row per room in level order, the total, and a Restart button. Nothing is shown during play or when a single room is cleared, so a door still moves the cat to the next room on the same tick (D-050). Counts last for one play of the level: they are not saved, and Restart resets them. Restart reloads the level scene; death resets stay reload-free (D-041). Each room clear and the level summary are also logged with the prefix PARALLAX_STATS, for playtest data.
Why: An end screen gives the score without interrupting the troll loop. Saving waits until there are levels worth saving. Log lines give playtest numbers without extra tooling.

D-062 · 2026-09-22 · Proposed (level-is-one-room and Flow clauses split out and accepted as D-063; the rest of this entry — hard tier, luck, business model, audio/haptics — stays Proposed)

Decision: v1 definition.

Content. Solo v1 ships 50 levels. A level is one room (Level Devil style): one checkpoint, one door, one screen or close to it. Levels 1–10 are the novice tier; levels 11–50 are the hard tier.
Novice tier keeps every current rule: deterministic traps (D-040), D-056's slack and reachability, D-057's lead, D-060's door clearance. Novice rooms are real troll rooms, not tutorials (D-053).
Hard tier is extremely hard and uses three sources of difficulty:
Memory: longer chains of betrayals, the same deterministic rules as novice.
Execution: tighter timing and jumps than D-056 allows. Hard-tier slack and reach limits are set after the device session (PAX-037), because touch precision on the phone decides what "tight but fair" means. OPEN: hard-tier slack (ticks) and max jump (fraction of reach).
Luck: bounded randomness, under these guardrails:
randomness only chooses between authored variants of a trap (e.g. which side the arrows come from, which floor tile collapses), never timing, speed or hitbox size;
every variant is survivable on its own and passes the hard-tier rules;
the chosen variant is visible at least D-057's lead before it can kill;
it is seeded per attempt from the level id and attempt number, and the seed is logged, so any death can be reproduced in the Editor;
novice levels never use it.
Flow: title screen, level select (locked until the previous level is cleared, best death count shown), level complete with Next level and Restart, pause (resume, restart, level select), settings (music and sound volume, haptics, touch stick size/position), progress saved on the device (unlocked levels, best death counts). No cloud saves.
Business model: free download with a one-time paid unlock. OPEN: which levels are free (proposal: the 10 novice levels plus the first 5 hard levels) and the price. No ads, no consumables.
Platforms: Android on Google Play for v1. iOS on the App Store follows as its own phase after the Android release.
Audio and haptics are in v1: an ambient bed, a sound and haptic cue for every trap firing, short death/respawn sounds (Vision §9).

Why: A finite, written finish line turns "until the game is done" into a countable list. One-room levels keep 50 levels achievable for a solo developer. The hard tier's rules are bounded so that deaths still read as the room's fault, which is what keeps players retrying.

Supersedes / amends: D-040's determinism and D-056's limits apply to the novice tier only; the hard tier gets its own rules (above). Vision §10 (store pages, iOS, payments now in scope as stated). CLAUDE.md's "no IAP" scope line lifts when the monetization ticket starts.

**Supersedes / amends:** D-062 (the gating sentence in its Consequence), D-049's "confirm on
device in PAX-037 when it runs" (still true, now in Phase H), Vision §13 timing.
**Consequence:** D-062 can move to Accepted with that sentence struck. `00_VISION.md` §12–13 and
`CLAUDE.md`'s scope section are updated in the same commit. `08_V1_ROADMAP.md` replaces the
Phase A validation block with Phase H.
D-062 · 2026-09-22 · Accepted (amended by D-063, D-064)
…
Consequence: 00_VISION.md §3 (pillar 2), §10 and §13, and CLAUDE.md's scope section are updated to match. ~~The playtest (PAX-037) still gates content: no levels are built beyond the current prototype until it passes.~~ Struck by D-064. OPEN items: hard-tier numbers → D-065 (PAX-057); free levels and price → D-067 (before PAX-066).
Numbering note (PAX-075): free levels and price is D-068; D-067 is randomness (08_V1_ROADMAP).

### D-063 · 2026-09-22 · Accepted

**Decision:** Level structure and flow for Phase B (PAX-050). Splits D-062 (Proposed): promotes only its level-is-one-room and Flow clauses to Accepted; D-062's content/hard-tier/luck/business-model/audio clauses are untouched and stay Proposed for their own later tickets.
(1) A level is one room: one checkpoint, one door, one screen or close to it (D-062). `Level_Solo01`'s existing multi-room sequence is dev/test scaffolding for room mechanics (trap kit, chains, gravity flips) built before this decision; it is not a template for a shipped level and is not renamed or restructured by this ticket.
(2) Level order and identity are data, not build-settings order or a hardcoded switch: an ordered list of level entries (id, scene name, display name) in a single ScriptableObject config under `Assets/_Game/Data`, following the existing config convention (e.g. `RoomSafetyConfig`).
(3) Level complete → Next level: the level-complete screen offers **Next level** (when one exists in order) alongside the existing **Restart**. Next level loads that level's scene by name (`SceneManager.LoadScene`), the same mechanism `RestartButton` already uses.
(4) Progress is saved on the device: per level, whether unlocked and the best (lowest) death count. Completing a level unlocks the next one in order. Saved via `PlayerPrefs` (first use of device persistence in the project), keyed so it survives app restarts; no cloud save. Pure unlock/best-deaths logic lives in `Parallax.Core` and is EditMode-tested there; only the `PlayerPrefs` read/write adapter lives in `Parallax.Gameplay`.
(5) Level select, pause and settings screens are **not** part of this decision or PAX-050; they stay open Phase B work.

**Why:** Phase B needs a real next-level and progress-save path now, and D-062 already answered exactly this shape (level = one room; Flow's level-complete/progress-save clause) as its newest, most specific statement on the subject. The rest of D-062 (hard-tier numbers, randomness, monetization, audio) depends on the device session and later phases and isn't needed to unblock this ticket.

**Consequence:** A future ticket that adds real Phase D content (levels 1–10+) populates the level-list data with real one-room scenes; PAX-050 itself adds no new level content, since only `Level_Solo01` exists today.

Consequence: 00_VISION.md §3 (pillar 2), §10 and §13, and CLAUDE.md's scope section are updated to match. The playtest (PAX-037) still gates content: no levels are built beyond the current prototype until it passes.


### D-064 · 2026-09-22 · Accepted
**Decision:** All player-facing validation moves to the end of v1 production. The device session
(PAX-037), the blind playtest (Gate 3) and every other phone or outside-player test run once, as
Phase H, after all 50 levels, the front end (menus, pause, settings), final art, audio, haptics
and the paid unlock are built. Until then, acceptance for every ticket is EditMode tests plus
the developer's own Editor play.
1. D-062's clause "the playtest (PAX-037) still gates content" is removed. Content (Phase D) is
   not gated on any playtest. Vision §13's "no further rooms are built until the game is fixed"
   no longer applies before Phase H; §13's criteria become Phase H's pass criteria.
2. D-062's OPEN hard-tier numbers (slack, max jump) get **provisional** values from the Editor
   (D-065, PAX-057). They live in one config asset, every layout test reads them from there, and
   PAX-069 replaces them with device-derived values in Phase H. A level that fails the final
   numbers is fixed in Phase H, not before.
3. Device-only behaviour (touch feel, D-049 on device, safe areas, haptics, frame time and
   thermals, real Play Billing) is built to spec and checked in the Editor where possible
   (Device Simulator, IAP fake store), but is recorded as **unverified** until Phase H. No ticket
   claims device validation before then.
4. The Google Play closed test required before production access doubles as the blind playtest
   (PAX-070), so the two are not run twice.

**Why:** The developer's call: finish the game first, then test the whole thing once with real
visuals and real content, instead of re-validating a greybox that will change.

**Risk accepted:** Hard-tier levels are authored against touch precision that has not been
measured. If the device numbers turn out stricter than the provisional ones, some hard levels
need rework in Phase H. Keeping the numbers in one asset and checking every level against them
in tests makes that rework a list, not a search.

### D-065 · 2026-09-24 · Accepted
(Number reserved earlier for PAX-057; written 2026-09-24.)
**Decision:** No difficulty tiers. The difficulty curve comes from the level number alone.
- Levels 1–10 are easy. D-056's defaults apply (0.75 reach, 12-tick slack). No precision
  sections.
- Level 11 onward is extremely hard. Precision sections are allowed (D-069), using the hard
  thresholds from KIT-4's config asset. They are provisional until the PAX-069 device
  session sets them.
- Hard stays fair. D-069 (2)(a)–(b) and D-057 still apply: every required jump stays inside
  the reachability contract, every timing case keeps positive slack, and every trap that
  can kill is revealed at least 6 ticks before it can.
**Why:** The developer's call: a fixed, simple curve (an easy run-in, then hard) instead of
selectable tiers with their own configs.
**Supersedes:** D-069 (2) in part: "Allowed only in hard-tier levels (after the novice
levels, D-062)" now reads "allowed only in levels 11 and up"; "(D-065, PAX-057)" in (2)(a)
now means this decision plus KIT-4's config. D-062's Content clause, "Levels 1–10 are the
novice tier; levels 11–50 are the hard tier", now reads as these two bands, not as tiers; its
OPEN "hard-tier slack (ticks) and max jump (fraction of reach)" is answered by KIT-4's config.
PAX-057 (tiers) is dropped.
**Consequence:** KIT-4 (PAX-076) makes the validator reject a precision-section marker in
levels 1–10, holds one set of hard thresholds, and exempts the Trap Lab (not a numbered
level). L001–L004 are in the easy band. Phase D content follows the two bands.

### D-066 · 2026-09-23 · Accepted

**Decision:** Level format for the one-room authoring pipeline (PAX-051). A level layout is
code-as-data in the Editor assembly, not a ScriptableObject and not JSON: one static class per
level under `Assets/_Game/Editor/Levels` (`L001Layout`, `L002Layout`, …), each exposing one
`SoloRoomDefinition` built with the existing readonly element/definition types (moved verbatim
out of `SoloRoomsLayout.cs` into `SoloRoomElementTypes.cs`, unchanged, no new fields, no
`readonly` removed). `LevelLayouts` is the single id → layout registry, checked against
`LevelListConfig` both ways. A level's room lives in its own local space: room id 0, origin
`(0,0)`. Nothing reads a layout at runtime — the scene is built from it once by a setup menu
(`SoloRoomBuilder`, extracted from `SoloRoomsSetup`), and anything runtime needs (kill bounds) is
baked into `RoomManager` at that same setup time, exactly as D-059 already does for
`SoloRoomsLayout`. `LevelLayoutValidator` reimplements the D-055–D-058/D-060 generic rules
(chain acyclicity, reach/timing slack, 6-tick reveal lead, room-frame containment, door
clearance) without changing any threshold, so a level layout can be checked outside a specific
room's narrative test assertions; the existing `SoloRoomsLayoutTests.cs` narrative tests are
untouched. A level's scene is a copy of `_LevelTemplate.unity` with its one room built by
`SoloRoomBuilder`, via `PARALLAX/Setup/Levels/New Level...` (first build) or
`Rebuild Current Level`/`Rebuild All Levels` (later relayout).

A `LevelLayout` ScriptableObject (rev A of this ticket) was rejected: every element/definition
type (`SoloRoomElement`, `SoloRoomTrapSettings`, `SoloRoomOpening`, `RequiredJump`,
`RequiredStep`, `SoloRoomDefinition`) is declared with `public readonly` fields set only through
a constructor, and Unity's built-in serializer silently drops `readonly` fields — a ScriptableObject
built from them unchanged would save every authored value as blank/default with no error.
Forking parallel mutable types, or serializing through an opaque JSON string field, were also
rejected: both still require either changing the reference types or losing native Inspector
editing, for no benefit, since nothing ever needs a layout after its one-time scene build.

**Why:** The scene, not the layout, is what the game reads at runtime (D-054); a level's
data only has to survive from the Editor menu that builds the scene to that build finishing.
Code-as-data keeps the exact same constructors, structs and validation the four `SoloRoomsLayout`
rooms already use and are already tested against, so splitting them into standalone levels
(`L001`–`L004`) needed no format conversion — only a translated `Origin` and room id.

**Consequence:** A future ticket authoring `L005`–`L050` adds one file each under
`Assets/_Game/Editor/Levels` plus a `LevelLayouts` entry; no tooling change is needed unless a
level needs an element kind that doesn't exist yet.

### D-069 · 2026-09-23 · Accepted
**Decision:** A room is built from two kinds of section.
(1) **Troll-route section.** Jumps are comfortable; the danger is which platforms and routes are
real. All D-055–D-060 rules apply unchanged. A section may offer several routes, some of which
fail (collapsing or fake platforms, dead ends, triggered traps).
(2) **Precision section.** A marked run of narrow, thin platforms where execution is part of the
difficulty. Allowed only in hard-tier levels (after the novice levels, D-062). Inside a precision
section:
  (a) Reach and timing slack use tier thresholds (D-065, PAX-057) instead of D-056's 0.75 reach
  and 12-tick slack. They may be tighter but never reach the limit: every required jump stays
  strictly inside the reachability contract and every timing case keeps positive slack. Until
  D-065 sets the numbers, the D-056 values apply.
  (b) Traps and arrows may be mixed in. D-056 (4)–(5) and D-057's 6-tick reveal lead apply
  unchanged: a trap that can kill during a precision jump is revealed at least 6 ticks before it
  can kill.
  (c) The section is marked in layout data. `LevelLayoutValidator` applies precision thresholds
  only to jumps inside marked sections.
Every level, in both kinds of section, has at least one valid route that passes the validator.
**Room size:** D-040's "one screen or close to it, 10–20 s once known" stays the default. A room
with a precision section may be wider than one screen (the camera follows the cat) and take up to
about 40 s once known. One checkpoint, one door, and death resets the room: D-040 and D-041 are
unchanged.
**Why:** The designer's play of L001–L004 (6 deaths across 4 levels) was too easy. The target is
Level Devil troll rooms plus Jump King-style execution, and D-062 already names execution as one
source of hard-level difficulty. Positive thresholds and telegraphed traps keep D-040's core rule:
every death can be blamed on the room or on a readable execution mistake, never on an invisible
trap or an impossible jump.
**Supersedes:** D-040 in part: the clause "the room is the source of difficulty, never the
controls", and the room size/time limit for rooms with a precision section. D-056 (1) and (2)
inside precision sections only. Restores the precision half of D-039, bounded by the rules above.
D-040's anatomy (setup → obvious route → betrayal → learned solution) still applies to every
troll-route section.
**Consequence:** The level camera (PAX-052) follows the cat through rooms wider than one screen and
keeps the next landing and any trap's reveal on screen. Precision thresholds come from a Pixel 8a
session with the touch stick, not from the Editor. The element types (moved to
`SoloRoomElementTypes.cs` by D-066) gain a section marker in a kit ticket.

### D-070 · 2026-09-23 · Accepted
**Decision:** Level scenes are generated (PAX-052, rev B). `_LevelTemplate` is the only source of
a level's non-room content (HUD, level-complete UI, camera, background/foreground). `New Level…`,
`Rebuild Current Level` and `Rebuild All Levels` funnel through one function, `LevelSetup.
RegenerateScene`: the target `Level_NNN.unity`'s file bytes on disk are overwritten with
`_LevelTemplate.unity`'s current bytes (`File.Copy`, the target's own `.meta`/GUID is never
touched, so Build Settings and every existing GUID reference keep working), reimported
(`AssetDatabase.ImportAsset(ForceUpdate)`), reopened, then the room is built from that level's
`LevelLayouts` entry via `SoloRoomBuilder` and the camera frame/background are baked via
`LevelCameraBuilder`. `New Level…` still creates the scene file via `AssetDatabase.CopyAsset` only
when it doesn't exist yet, then calls the same regeneration function. `RegenerateScene` refuses if
the template, the target, or whatever scene is currently active/loaded has unsaved changes (it
replaces the active scene in memory without saving). `Build Level Template` stays bootstrap-only:
it errors if the template already exists; there is no template "rebuild" path, since from now on
the template — not `Level_Solo01` — holds scaffolding changes. Every scaffolding menu that
configures level content (the Level Camera menu, D-071) has a scene guard that accepts only
`_LevelTemplate`; the regeneration menus refuse `Sandbox_Realities`, `Level_Solo01` and
`_LevelTemplate` itself (`LevelSetup.IsGuardedScene`).

**Determinism, amended from rev B's original wording.** Whole-scene regeneration is deterministic
at the *data* level, not the byte level: the room subtree is destroyed-and-rebuilt from
`SoloRoomBuilder` on every regeneration (as it already was pre-PAX-052, for the room alone), and
Unity assigns each freshly-constructed GameObject a new fileID from its own session-internal
counter, which is not stable run to run. Verified empirically during this ticket: running
`Rebuild All Levels` twice in a row, with nothing else changed, produced two *different* diffs —
same objects, same components, same values, different fileIDs — never an empty one. Non-room
scaffolding copied verbatim from the template (HUD, camera config reference, background layer
objects) *is* byte-stable across runs, since it's never destroyed and rebuilt. "Deterministic"
therefore means: the same set of objects, components and serialized values every run, checked by
data-level tests — `LevelLayoutTests`' `BakedBounds_…`/`SplitFidelity_…` (layout data, not the
scene), and `LevelSceneTests`' scene-read bounds test plus its camera-wiring test, which reads the
baked `frameCenter`/`frameSize` themselves (non-zero, matching the scene's own kill bounds centre
and margin-adjusted size) rather than only checking that the reference fields are wired — not
`git diff --stat` showing nothing. The developer
checklist (§9 of the ticket) is amended to match: after two `Rebuild All Levels` runs, expect a
diff confined to room-subtree fileIDs (no added/removed objects, no changed values), not an empty
one; no `.meta` file changes in either run, which still holds exactly as written.

**Why:** 50 level scenes can't be hand-fixed every time scaffolding changes (PAX-051's own review
found `Rebuild All Levels` only ever touched the room). Overwriting the target's bytes from the
template, rather than copying GameObjects between scenes, avoids cross-scene reference breakage
and fileID churn on the *non-room* content, which is the content actually shared across all 50
levels; the room's own fileID churn is accepted because nothing outside that scene ever references
a room GameObject by fileID (bounds and the camera frame are baked as values, not references).
**Supersedes:** D-066's "`Rebuild Current Level`/`Rebuild All Levels` (later relayout)" description
insofar as it now regenerates the whole scene, not only the room.

### D-071 · 2026-09-23 · Accepted
**Decision:** The level camera (PAX-052). A new component, `Parallax.Gameplay.Cameras.
LevelCameraFollow`, on `Camera_A` only — `CatCameraFollow` (`Sandbox_Realities`/`Level_Solo01`,
frozen-adjacent per D-047) is untouched, and `Camera_B` stays disabled and unmodified. Pure math
lives in `Parallax.Core.Cameras.CameraMath` (extended, not forked): `IsFitMode`/
`RequiredFitViewHeight`/`FollowViewHeight`/`ResolveViewHeight`/`ApplyLookAhead`/
`ResolveFollowCentre`.
1. **Frame, not kill bounds.** The camera reads a frame baked onto the component itself
   (`frameCenter`/`frameSize`), computed at regeneration time as `SoloRoomBuilder.
   ComputeRoomBounds(layout, ViewMargin)` — the room's content bounds plus `ViewMargin`, not
   `RoomManager`'s (differently-margined) kill bounds. Baked the same way D-059 bakes
   `RoomManager.bounds`; never a runtime layout read (D-066).
2. **Fit vs follow.** Fit mode requires the frame — width *and* height, via the current aspect —
   to fit in a view no taller than `MaxViewHeight`; its view height is the minimum that shows the
   whole frame (`RequiredFitViewHeight`). Otherwise follow mode: view height is `min(frame height,
   MaxViewHeight)` (only the frame's height, since horizontal coverage comes from panning, not
   sizing wider); the camera resolves a dead-zone target, applies horizontal look-ahead in the
   cat's direction of travel, locks to the frame's vertical centre unless the frame is taller than
   the view (§2.2.2), and clamps to the frame. Recomputed every step, so aspect changes (16:9,
   20:9, 4:3) reselect mode and size live.
3. **Snap, not smoothed.** `SnapToTarget()` (no `SmoothDamp`) runs on `Start()` and on
   `CatRespawn.Respawned` — the same hook `CatCameraFollow` already used, since `CatRespawn.
   RespawnAt` sets position before firing the event, same tick as the checkpoint respawn. The
   whole per-step update (position *and* size) is skipped while `RoomDeath.IsHolding`, mirroring
   `RoomManager.OnStepped`'s own hold gate, so the death hold never produces a camera swoosh.
4. **Constants**, one place (`LevelCameraConfig`, a `ScriptableObject` asset, `PARALLAX/Level
   Camera Config`): `ViewMargin` 0.5, `MaxViewHeight` 16, `LookAhead` 2.5, dead zone (2, 1.6),
   `smoothTime` 0.18, `maxSpeed` 40 — the dead zone/smoothing/max-speed values are `CatCameraFollow`'s
   existing ones. L001–L004's shipped camera frame is 33×13 (32×12 content + `ViewMargin` 0.5/side);
   at `MaxViewHeight` 16 the required fit height (`max(frame height, frame width / aspect)`) is
   14.85 at 20:9 — **fit** — but 18.56 at 16:9 and 24.75 at 4:3 — **follow** at both. So today's four
   levels are fit-mode only at aspects at or above roughly 20:9; narrower phones see them pan
   horizontally in follow mode. `LevelCameraMathTests.SameRoom_At20x9_IsFit_At16x9And4x3_IsFollow`
   asserts exactly this split. Provisional; tuned on device later (out of scope here, per D-064) —
   whether that's the intended novice-tier feel on a 16:9/4:3 device, or `MaxViewHeight` should rise
   (≥ ~18.6 would make 16:9 fit too), is an open call for the developer, not decided here.
5. **Setup menu.** `PARALLAX/Setup/Levels/Level Camera` configures `Camera_A` in `_LevelTemplate`
   only (guard: `LevelCameraSetup.RefusesToRun`): adds `LevelCameraFollow`, wires
   `target`/`respawn`/`roomDeath` (looked up by name/type in the open scene, not hardcoded),
   removes the template's copied-in `CatCameraFollow`, and sizes/offsets the three tiled
   background layers (`BG_00_Sky`/`BG_01_Far`/`MG_01_Mid`) for `MaxViewHeight` via the existing
   `ParallaxMath.BackgroundTileOffsetY` (no code change to `ParallaxLayer`/`ParallaxMath` — same
   sprite, tiled taller; `maxAbsCameraY` 0 since no room triggers vertical follow yet). The
   regeneration step (D-070) then bakes each level's own frame and repositions the background
   layers' (and `Foreground/FG_01`'s) vertical placement from that frame — horizontal placement
   stays `ParallaxLayer`'s existing runtime job.
6. **Trap-reveal visibility** in follow-mode rooms is the validator's job (KIT-2, KIT-4), not the
   camera's — the camera guarantees the frame is on screen, not that every reveal lands inside the
   current view before a kill.
**Why:** A world-aligned camera (D-020) that shows a whole troll room at once by default (D-040),
and follows through D-069's wider precision rooms without hiding the next landing or a trap's
reveal, while never risking `Sandbox_Realities`' existing camera behaviour.
**Consequence:** L001–L004 already exercise follow mode below ~20:9 aspect (see §4), so follow mode
is not untested in practice — but no *novice-tier* room has been played through it on a real device
yet (D-064 defers all device validation to Phase H), and no room is wide/tall enough to need
vertical follow at all. `LevelCameraMathTests`' clamp/look-ahead/vertical-lock assertions use a
synthetic frame rather than a built level for that reason, not because follow mode itself is
unexercised.

### D-072 · 2026-09-23 · Accepted

**Decision:** The Build Settings scene list is generated (PAX-053). `BuildSceneList.Compute
(currentEntries, configScenePaths, levelsFolder)` (`Parallax.Editor.Setup`) is a pure function:
`LevelSelect.unity` first (enabled), then every `LevelListConfig` entry's scene in list order
(enabled), then every other scene already in `currentEntries` kept with its own enabled state and
relative order (`Sandbox_Realities` and the disabled dev scenes) — deduplicated so a stray repeat
of `LevelSelect` or a listed level can never demote the canonical, first-placed entry. Any scene
under `Assets/_Game/Scenes/Levels/` that isn't a listed level's scene (the template, or an
unlisted `Level_NNN`) is always dropped; scenes outside that folder (`Level_Solo01.unity`,
`Sandbox_Realities.unity`) are untouched and never removed. `BuildSceneList.Sync()` is the thin,
impure wrapper `PARALLAX/Setup/Levels/Sync Build Scene List` runs: it first checks every scene
`Compute` would need actually exists on disk (`LevelSelect.unity` and each listed level's scene),
writing nothing and logging an error if any is missing, then calls `Compute`, assigns
`EditorBuildSettings.scenes`, and persists the change immediately with `AssetDatabase.SaveAssets()`
— named explicitly because `EditorBuildSettings.scenes = ...` alone only dirties the in-memory
list; without an explicit save the change can be lost, which is what actually happened to
`Level_004` after PAX-051/052 (fixed by hand in commit `PAX-051 fix: Level_004 in Build Settings,
_LevelTemplate removed`). This targets the classic global `EditorBuildSettings.scenes` because no
Build Profile in this project overrides the scene list (`Library/BuildProfiles/*.asset` all carry
empty `m_Scenes` and no `overrideGlobalScenes`); a future ticket that introduces an overriding
Build Profile must revisit this decision. `Sync()` is the only code that writes
`EditorBuildSettings.scenes` — `LevelSetup`'s old `AddToBuildSettings` (a plain append, never
saved) is deleted, and `New Level…`, `PARALLAX/Setup/Levels/Level Select`, and
`Sync Build Scene List` itself all call `Sync()` instead. Every runtime level/menu scene load
(Restart, Next level, Levels, and loading from `LevelSelect`) goes through one loader,
`Parallax.Gameplay.Levels.LevelSceneLoader.Load(sceneName)`, which refuses and logs an error naming
the scene when `Application.CanStreamedLevelBeLoaded` returns false, instead of calling
`SceneManager.LoadScene` directly — so a scene that isn't in the Build Settings list fails in the
Editor with an error naming it, the same way it would fail on the device.

Two different failure modes, two different catches. In the Editor, the loader's
`CanStreamedLevelBeLoaded` pre-check reads Unity's **in-memory** `EditorBuildSettings.scenes` list
(a player build has no `EditorBuildSettings`; it reads the baked scene list instead, which is why
both resolve identically once Build Settings is saved) — the same list a `SceneManager.LoadScene`
call would consult — so it catches "this scene isn't in the list at all" regardless of whether
that in-memory list has ever been saved to disk; this is what makes a scene missing from the list
fail the same way in the Editor as on the device. The `Level_004` drift was the other failure
mode: the scene was in the in-memory list but never saved, so this check passed; Sync's explicit
save and the on-disk §5.3 test catch that one. Put precisely: the pre-check does **not** detect
"the in-memory list disagrees with what's saved to `ProjectSettings/EditorBuildSettings.asset`" —
an Editor session with an unsaved change resolves every load correctly right up until the Editor
restarts (or a fresh clone/CI checkout reads only the committed file), which is exactly the state
`Level_004` was left in after PAX-051/052. That failure mode — unsaved drift — is caught instead
by `Sync()`'s explicit `AssetDatabase.SaveAssets()` (never leaving the list only dirtied) plus
`BuildSceneListTests.RealProject_CurrentBuildSettingsOnDisk_IsAFixedPointOfCompute` (§5.3), which
parses the **on-disk** file directly and fails if it and `Compute`'s output ever disagree — a check
the loader's runtime pre-check cannot perform, since it only ever sees whatever is currently in
memory.

**Why:** A hand-maintained or half-saved Build Settings list breaks silently as the level count
grows (PAX-051's `Level_004` drift, and `_LevelTemplate` ending up in the in-memory list); this
ticket also needed a level select screen, which must always be build index 0. Making the list a
pure function of `LevelListConfig` plus the scenes already present makes drift a test failure
(`BuildSceneListTests.RealProject_CurrentBuildSettingsOnDisk_IsAFixedPointOfCompute`, which parses
`ProjectSettings/EditorBuildSettings.asset` from disk) instead of a manual `grep`.

**Consequence:** Adding level 5–50 no longer needs a manual Build Settings edit — `New Level…`
calls `Sync()` automatically, provided `LevelSelect.unity` and every already-listed level's scene
exist on disk. `LevelSetup.NewLevel`'s log line only reports a successful sync; if `Sync()` refuses
(e.g. `LevelSelect.unity` not yet built), the level's own scene is still created and regenerated,
but it is not added to Build Settings until `Sync Build Scene List` is run again after fixing the
cause.

### D-073 · 2026-09-23 · Accepted

**Decision:** A level can be paused from a HUD button in a reserved touch region (top-right, inset
below the dev-only FLIP button), and it pauses itself when the app goes to the background. While
paused, no game state advances and input is ignored; resume continues exactly where it stopped,
with no input carried over from before the pause. The pause menu offers Resume, Restart and
Levels. Restart and Levels abandon the current attempt: nothing is recorded, and best deaths
remains the fewest deaths in one completed run (the pause panel's Levels button is its own
component, `PausePanel.GoToLevelSelect`, and never reuses `LevelsButton`, which records a
completion). Every scene load restores the running state, so no scene starts paused. Pausing is
refused once the level is complete, and the complete screen hides the pause button.

**Mechanism: (c), time scale plus a pause gate** (PAX-054).
- `Time.timeScale = 0` is the global stop. Physics (`Physics2D.simulationMode` = FixedUpdate),
  every `FixedUpdate` including frozen co-op code, and `deltaTime`-driven presentation (camera
  `SmoothDamp`, cat flipbook) stop with it. UI keeps working: the EventSystem and
  `InputSystemUIInputModule` run in `Update` (default dynamic-update input), so Resume is
  clickable.
- Every write to `Time.timeScale` goes through one seam, `Parallax.Gameplay.Levels.RunningState`
  (`Freeze` = 0, `Restore` = 1; `SetTimeScale` is swappable for tests). `LevelPause` and
  `LevelSceneLoader` are its only callers.
- `ObserverSet.FixedUpdate` checks an optional `pauseGate` (a `MonoBehaviour` implementing
  `IPauseGate`; `LevelPause` in level scenes) before `Tick++`, so the level tick owner itself
  honours the pause — cat movement, the room tick (traps, trap timers, hazards, bounds, door,
  `RoomLifeTick`), the death hold, respawn and the death count all hang off it. A null gate
  (`Sandbox_Realities`, `Level_Solo01`) changes nothing.
- `LevelSceneLoader.Load` calls `RunningState.Restore()` after the Build Settings refusal and
  before `LoadScene`, so a refused load leaves a paused level paused.
- `LevelPause.OnDestroy` restores the running state only if that instance froze it and never
  resumed (a paused level torn down some other way, e.g. leaving Play mode).
- Pure logic in `Parallax.Core`: `PauseState` (Pause, Resume, ApplyPendingResume, IsPaused) and
  `AutoPausePolicy`.

**Input.** Resume takes effect at the end of the frame it's requested in, after all input has
been read for that frame: `Resume()` only sets a pending flag, and `LevelPause.LateUpdate` applies
it (hide the panel, restore the running state, then `CatInputRouter.ResetTransientState()`).
Input read in that frame and every latch left over from before the pause are dropped. A Pause
after a Resume in the same frame cancels it; two Resumes in one frame apply once. The motor's
`jumpBufferTimer` and `coyoteTimer` are game state: they freeze and continue after resume. Keys
still physically held after resume are live input from the next frame (`KeyboardCatInput` polls
`isPressed` every frame). A finger held through the pause (stick or jump) is dropped by
`ResetTransientState` and must be lifted and put down again, since fingers are only claimed on
their `Began`. While shown, the pause panel is one full-screen reserved touch region, so no touch
that begins while paused — including the tap on Resume, which begins while the panel is still
active — is ever claimed: its `Began` is reserved, and after the resume it has no new `Began`.
Pause UI buttons are never selectable (Navigation None, as on the complete screen), and the
EventSystem selection (a serialized reference on `LevelPause`) is cleared on pause and resume, so
keyboard and gamepad Submit and Navigate can't reach them.

**Auto-pause.** `OnApplicationPause(true)` (background) always pauses. `OnApplicationFocus(false)`
pauses only when `LevelPause.IgnoreFocusLoss` is false; it is set from `Application.isEditor` in
`Awake`, so focus loss pauses on device but not in the Editor (clicking the Console or Inspector
would otherwise pause constantly). Coming back never resumes; the player taps Resume.

**Known limitation.** The dev-only keyboard tools (`KeyboardSwitchInput` Tab, `KeyboardEchoInput`
Z, `DebugPanel`) keep reading keys in `Update` while paused. They are frozen co-op/dev tooling,
removed from non-debug builds, and are left untouched.

**Why:** Time scale alone would work at runtime but leaves nothing of ours to test; a pause flag
alone would leave the dynamic cat body and the frozen anchor presenters moving. The deferred
resume closes a real frame-order gap: the EventSystem runs at execution order −1000, before
`TouchStickCatInput`, and a tap that presses and releases in one frame surfaces its `Began` in
that same frame, so an immediate resume would have hidden the panel before the touch was checked.

**Consequence:** New level UI goes through `PARALLAX/Setup/Levels/Pause Menu` on `_LevelTemplate`
only (D-070), then Rebuild All Levels. `Level_Solo01` has no pause menu. Any future code that
changes `Time.timeScale` must go through `RunningState`.

### D-074 · 2026-09-23 · Accepted

**Note (PAX-091, 2026-09-27):** the approach search ("seen from the checkpoint", per storey) also sees a geyser launch
(Up vents; Down vents in rooms with a gravity flip), a vine climb (a grab, then a leap or release from any height) and a
stuck spear shaft as ways up. A shaft counts for every trap outside the spear's own chain family. A drop is blocked
where fixed solids (Floor, Wall, PitBottom, Ceiling) between the two heights cover every x of its way to the landing.
No threshold changed. Code: `TriggerCoverage.Edges.cs`.

**Decision:** Trigger coverage and thin platforms (PAX-073, KIT-1).
(1) **Coverage.** Every trap fired from an Overlap trigger must catch the cat before it can reach
the trap's danger, from every approach: walking in from either side, jumping, falling in from
above, and the mirrored cases with gravity up. `LevelLayoutValidator.ValidateTriggerCoverage`
enforces this for every `LevelLayouts` entry (geometry in `Editor/Setup/TriggerCoverage.cs`). It
is not part of `Validate()`.
(2) **Danger set.** A trap's `KillVolumes` (the door-clearance definition), plus every chain
descendant's `KillVolumes`, plus the door sweep of a `DoorRetreat` descendant. Chains are
followed.
(3) **The check.** The trigger must be a cut. Over its x-span, its y-span covers the lowest
standable top, where pit interiors count as death, up to the underside of the ceiling over it
(epsilon 1e-3). Pit interiors are an opening's `PitBottom` and any top under its `OpeningBottom`
hazard; a Floor sunk inside a pit still counts as standable. Every danger must lie beyond the
trigger's near edge, as seen from the checkpoint. Alternatively, the **flip-entry clause**
applies: a danger on a floor stretch that is
bounded by `UnjumpableFloor` hazards or the room's ends, doesn't contain the checkpoint, and has
nothing standable above its floor is covered when the trigger contains every gravity flip whose
drop can land in the stretch. The drop is taken as full run speed for a fall from the flip's top.
The clause applies only to dangers below a gravity-up cat's reach from the ceiling (ceiling
underside − collider height − jump height = 3.24 in a 7-high room); a gravity-up cat can walk the
ceiling into the stretch without any flip.
A trigger that moves with its trap is checked at its authored pose. That covers Rearm traps
completely, since D-055 snaps a Rearm trap back to its authored state before it can fire again.
Collapsing floors and gravity flips trigger on their own body and are covered by construction.
Periodic traps have no trigger. Per-tick sampling can't skip a cut: the cat is 1.0 × 0.56 and
moves at most 0.12 / 0.40 / 0.277 u per 0.02 s tick (run / max fall / jump take-off).
(4) **Learned bypass.** `SoloRoomTrapSettings.LearnedBypassReason`, an optional last constructor
parameter, null by default. A non-null reason skips the rule, and the validator lists the trap.
An empty reason is rejected. No trap in L001–L004 is a learned bypass.
(5) **Levels fixed.**
- L001 `Spikes_A`: trigger (20, 0.5) 1×1 → (21, 3.5) 0.5×7. It was the falling-ceiling gap: a
  full-speed jump over `Collapse_C` cleared the floor-height trigger, so `Spikes_A`, `Block_A` and
  `Retreat` never fired.
- L002 `Lift`: trigger (19.55, 0.25) 0.3×1 → (19.55, 3.375) 0.3×7.25. The x-span and bottom are
  unchanged, so a standing cat fires it on the same tick. A direct jump to the Receiver used to
  skip it and with it `ReceiverBlock`.

Baked bounds are unchanged. `SoloRoomsLayout` and `Level_Solo01` keep the `Spikes_A` and `Lift`
gaps on purpose (frozen). `LevelLayoutTests.SplitFidelity_…` exempts exactly
`{("L001","Spikes_A"), ("L002","Lift")}` from its trigger-box comparison and fails if either
entry goes stale.
(6) **Thin platforms** are ordinary `Floor` elements with the floor look: solid, two-sided, no
effector. `LevelLayoutValidator.ValidatePlatformSizes` rejects any `Floor` thinner than 0.5 or
narrower than 1.0, naming it. The limits live in `Assets/_Game/Data/PlatformSizeConfig.asset`,
created by `PARALLAX/Setup/Levels/Platform Size Config`. At 0.5, the minimum thickness is at
least the cat's maximum fall per physics step (20 u/s × 0.02 s = 0.4 u), so no platform can be
tunnelled. The cat's Rigidbody2D is also already Continuous, and its physics is unchanged.
**Limits (not checked):**
- betrayals that don't kill, e.g. a Solid that moves away (L004 `FalseLanding`);
- an Overlap `DoorRetreat` that is itself the root has no danger of its own and is skipped (the
  door sweep counts only for chain descendants, as ruled); none exists in L001–L004;
- the `UnjumpableFloor` role is trusted, not validated: the flip-entry clause assumes such a
  hazard really can't be jumped;
- the flip-entry gravity-up reach check uses the highest ceiling spanning the danger and assumes
  one ceiling height over the stretch;
- rooms with no ceiling over the trap: rejected as "coverage band undefined", with no fallback
  band;
- the flip-entry clause ignores upward speed when a cat enters a flip, treats `FallingBlock`s as
  non-standable, and handles floor stretches only;
- `SoloRoomsLayout` and `TrapLabLayout` are not checked.
**Why:** A trigger that can be jumped past isn't a troll, it's a hole in the room. Thin platforms
are needed by KIT-3 and KIT-4.

### D-075 · 2026-09-23 · Accepted

**Decision:** One tick source at 50 Hz (PAX-077).
(1) **The tick.** The game ticks at 50 Hz: one tick is one `FixedUpdate` of 0.02 s, and
`ObserverSet.FixedUpdate` owns it. Changing the tick rate is a new decision: every trap speed is
per tick (a FallingBlock at 0.3 u/tick falls at 15 u/s), so it changes every level's feel.
(2) **Rules keep their tick counts.** 12 ticks of timing slack (0.24 s), a 6-tick visible lead
(0.12 s), `HoldTicks` 30 (0.6 s). The seconds in D-056, D-057 and D-058 ("at 60 Hz") are
superseded by these; those entries are not edited.
(3) **One source.** Every seconds↔ticks conversion goes through `Parallax.Core.TickTime`
(`SecondsPerTick`, `TicksPerSecond`, `ToTicks`, `ToSeconds`; unrounded floats, no integer API),
which reads `Time.fixedDeltaTime`. Tests swap `TickTime.SecondsPerTickSource` and restore it in
`[TearDown]`; nothing writes `Time.fixedDeltaTime`. No seconds↔ticks conversion hard-codes a rate.
A literal dt passed as input to a pure step function in a unit test is not a conversion.
`TickTimeTests.FixedTimestep_OnDiskAndLoaded_IsTwentyMilliseconds` parses
`ProjectSettings/TimeManager.asset` from disk (0.02 within 1e-6) and checks `Time.fixedDeltaTime`
equals it (1e-7); both are needed because Unity's default is also 0.02.
(4) **Stored step.** Unity 6's re-save (abb55f9) stores the step as 2822399/141120000 =
0.0199999929 s: it truncated 0.02f × 141120000 = 2822399.94. The float is 0.019999992. Accepted
as is. Side effect: `CatMotor2D` counts coyote and jump buffer down by dt (`CatMotor2D.cs:110-111`)
and fires while both are > 0. At dt 0.02f the coyote window allows a jump on 4 airborne ticks
and the jump buffer lasts 5 ticks including the press tick; at the stored step,
0.1 − 5 × 0.019999992 = 4.5e-8 > 0, so they are 5 and 6. abb55f9 moved both windows by one tick.
No code change. For the same reason a `CeilToInt` of a whole-second value gains a tick
(0.6 s / step = 30.00001); `TickTime` does not round.
(5) **Door clearance (supersedes D-060's 0.1 u).** The margin is one tick at run speed,
`MaxSpeed × TickTime.SecondsPerTick` = 0.12 u, from the same `CatMotorConfig` periodic slack
uses. The tightest door in L001–L004, `SoloRoomsLayout` and `TrapLabLayout` is 0.20 u from a kill
volume, so no result changed. A missing `CatMotorConfig` is a validator error, not a skip (door
clearance ran without one before).
(6) **Death to control (D-041).** Kill at tick T, hold steps T+1..T+30, respawn at T+30, first
input-driven step at T+31: 31 ticks = 0.62 s. Worst case with `FallResetVolume` ordering: 32 ticks
= 0.64 s ≤ 0.75 s (`TickTimeTests.DeathToRegainedControl_WorstCase_…`).
(7) **50 Hz layout gaps (PAX-078).** Two `SoloRoomsLayout` narrative tests pass only at 60 Hz and
pin `TickTime` to 60 Hz with a `PAX-078` comment until PAX-078 fixes the rooms:
- L002 / Solo room 1 `Lift`: a full-speed cat lands on the Lift with 10.88 ticks of slack before
  its trigger fires (0.218 s), below 12.
- L004 / Solo room 3 final room: a full-speed cat reaches `Flip_A` while `Block_1` is still
  falling (block top 2.53 vs paw 1.44); `Block_2`'s visible fall before contact is 0.75 ticks
  (< 6) and it is not cleared (paw − block top −3.90).
The generic validator does not catch either gap; only the `SoloRoomsLayout` narrative tests see
them.
(8) **Presentation (Phase H device check, D-064).** The cat's Rigidbody2D interpolates;
FallingBlock and MovingTrap bodies (every level scene: `m_Interpolate: 0`) and the DoorRetreat
door (`doorRoot.position` per tick) do not, and `LevelCameraFollow` updates in `LateUpdate`. On a
60 or 120 Hz screen those 50 Hz bodies can judder. Not changed.
**Why:** Every level and trap was tuned and played at 50 Hz, while the docs and the validator
assumed 60 Hz, so every seconds↔ticks conversion was 20% off. KIT-2's arrow tell and KIT-4's
per-section thresholds are counted in ticks, so the rate is fixed in one place first.
Rejected: 60 Hz to match 60 Hz displays — a feel change that belongs to Phase H.

### D-076 · 2026-09-23 · Accepted

**Decision:** L002 and L004 retimed for 50 Hz (PAX-078).
(1) **L002.** `Lift` trigger centre x 19.55 → 19.80 (xMin 19.40 → 19.65). A full-speed cat off
`Floor_C` lands on the Lift 12.96 ticks before the trigger fires (continuous model; was 10.88), and
14–15 ticks stepped per tick (was exactly 12, zero margin). At 60 Hz, for the record: 15.56. The
betrayal is unchanged: `ReceiverBlock` is timed from the Lift's fire.
The trigger now fires 2 ticks later than `SoloRoomsLayout`'s. `TriggerCoverageTests.L002Lift_ExtendedTrigger_…`
(PAX-073) pinned its x-span to `SoloRoomsLayout`; it is renamed
`L002Lift_ExtendedTrigger_StillCatchesAStandingCatOnTheLift` and asserts x [19.65, 19.95] inside the
Lift's top, with the bottom and full-height asserts unchanged (R13).
(2) **L004.** `Flip_A` x 26.5 → 25.0 (box x 24.75–25.25, y 2–4 unchanged), `Block_1` delay 37 → 28,
`Block_2` delay 25 → 4. At 37/25, with `Flip_A` inside `Block_1`'s column, every full-speed flip
was killed by `Block_1` (or `Block_2`), and no delay pair kept the floor-run kill with a flip window
of 12 ticks and ±2 margin. Now:
- the right-holding fast-flip window is **14.83 ticks** at the worst trigger phase (15.83 at the
  best); 18.7 if the cat reacts 15 ticks after `CeilingHiddenSpikes` is revealed (simulator);
  18.8–19.8 at 60 Hz, for the record;
- every hop over `SourceSpikes` is forced into `Flip_A` (its box starts at x 24.75, just past the
  spikes' right edge 24.5), so there is no floor run and no floor bypass. The betrayal changes from
  "`Block_1` crushes the floor runner" to "`Block_1` kills the early flipper" (R15): full-speed
  take-offs from the trigger crossing onward flip and are killed by `Block_1` over a band
  5.67–6.33 ticks wide (worst phase 5.67), and `Block_1` is visibly moving for 13–14 ticks before it
  can first overlap an early flipper (D-057 ≥ 6). `FalseLanding`, `SourceSpikes`, `Block_1` (early
  flip) and `CeilingHiddenSpikes` still stack;
- with the 15-tick reaction the window stays ≥ 12 for `Block_1` delays 25–31 (simulator);
- standing still left of `SourceSpikes` for 0–139 ticks before the hop still survives;
- `Block_2` never overlaps a cat on 3,096 simulated routes, so its D-057 lead is unbounded;
- walking the ceiling without jumping is killed by `CeilingHiddenSpikes`; 120 of 128 ceiling
  landings have a route to the door; the other 8 land 0.07 u from the spikes and die on them (a
  reset, not a soft-lock) within the searched policies;
- no floor-only route skips the ceiling, so `CeilingHiddenSpikes` stays on every route to the door;
- `FalseLanding` and `SourceSpikes` are unchanged. Door clearance, coverage, platform sizes and
  baked bounds are unchanged.
(3) **Checked at the real rate.** `ShippedLevelTimingTests` reads L002/L004 through `LevelLayouts`,
with no pin: the L002 landing slack (continuous), and for L004 a per-tick stepper (`RoomStepper`)
that asserts no full-speed take-off crosses `SourceSpikes` without flipping, the early-flip band
killed by `Block_1` (≥ 5 ticks), and the ≥ 12-tick window (a ceiling landing counts only once
`Block_2` has fired and is below the cat). Stepper model: per tick, motor step, then
the room step on poses from the previous physics step, then physics; the cat is a 1 × 0.56 box;
static blocks (hanging and landed) are solid; a moving `FallingBlock` kills on overlap of its box
with its size shrunk by 0.04 (0.02 per side, `Bounds.Expand(-.04f)`), with no push-out; moving
Solids neither carry nor crush the cat; gravity flips on first overlap; Once timing only; the
stretch ends before the door. The stepper
holds right; the reaction and steering numbers above come from the Python simulator used in
Phase 1 and are evidence, not tested.
(4) **Scenes.** `LevelSceneTimingTests` checks that every configured trap's serialized delay and
cooldown/period/phase in each `Level_00N.unity` equals its layout, so a stale scene fails. It is
red between a layout edit and `Rebuild All Levels`. Known gap: no scene test compares element
positions with the layout, so a position-only change (like `Flip_A`) is not caught if a scene is
left stale.
(5) **Frozen scaffolding.** `SoloRoomsLayout` and `Level_Solo01` keep their 60 Hz-era timing. Their
two narrative tests (`V3_RoomTwoLanding…`, `V3_FinalFastFlip…`) stay pinned to 60 Hz as a permanent,
documented exception, not a to-do. Their flip-at-centre model is about 6 ticks lenient: a flip
starts at first overlap with `Flip_A` (cat centre 25.75 at the old position), not at its centre (26.5).
`SplitFidelity` lists `("L004","Flip_A")` in `PositionExceptions`. It doesn't compare trap
settings, so L004's delays now differ from `SoloRoomsLayout`'s without an entry.
(6) **Limit.** Timing gaps of this kind (a route's slack against a chain of traps) are caught only
by per-level tests until KIT-3's route validator. `LevelLayoutValidator` has no rule for them.
(7) **Evidence still open.** No pre-change developer play (R11b). The developer's §9 play checks the
simulator's predictions; if it contradicts one, PAX-078 is reopened.
**Why:** At the real 50 Hz both rooms were unfair: the L002 landing had no timing margin, and the
L004 learned fast flip killed every full-speed player.

### D-077 · 2026-09-24 · Accepted

**Decision:** Coyote time and the jump buffer are counted in whole ticks (PAX-079). Each window's
length is `TickTime.ToWholeTicks(seconds)`, the config's seconds divided by the tick length and
rounded half up, computed on every Step. At 50 Hz, coyote allows a jump on the first 5 airborne
steps after leaving the ground, and the buffer honours a press on its own step and the next 5. A
grounded cat can always jump when a press is buffered, including with CoyoteTime 0 (the old float
code refused that case; no config uses it). The logic is the pure `Parallax.Core` `JumpWindows`,
and it counts Step calls, not dt. `CatMotor2D` keeps `coyoteTimer` and `jumpBufferTimer` as floats
holding whole ticks, so the two reflection tests are unchanged. An equivalence test proves the new
logic matches the old float logic at the committed step for every input sequence tested. Rounding
half up means a config value that is a whole hundredth of a second (0–1 s) gives the same count at
0.02 and at the committed step. Freeze (D-059) and pause (D-073) are unchanged: Step doesn't run,
so the windows hold and continue. Supersedes: D-075 (3) 'no integer API' and D-075 (4) 'does not
round', for ToWholeTicks only; and the float countdown described in D-075 (4). The rest of D-075
stands.

### D-078 · 2026-09-24 · Accepted

**Decision:** Trap kit: the arrow (PAX-074, KIT-2). A launcher hosted in fixed geometry fires one
arrow along a horizontal lane, left or right. Tell T ticks (≥ 6, D-057): the arrow is visible at the
mouth and harmless, and the first lethal pose is the tell pose. It then flies at v u/tick as a pure
function of ticks since the fire (`ArrowMath`), stops at the lane's authored end face, and is
harmless and visible from then until reset or rearm. The kill test is the tick pose through
`RoomDeath` (`DeathCause.Hazard`). v ≤ Length + (collider width − collider height) − 2 × run per
tick (1.00 today), so an arrow can't pass through the cat between ticks. It uses the existing
trigger sources and repeat modes; 'jump-triggered' is an Overlap trigger in the jump arc. An arrow
can be a chain source, firing at the start of its tell. Coverage (D-074) for arrows: either the
trigger is a cut and the lane extends at least one collider width beyond its near edge, or the
trigger contains the lane box. An unfired arrow has no danger, so the lane on the checkpoint side of
the trigger is not reachable danger. Validator: `ValidateArrowTell`, `ValidateArrowSpeed`,
`ValidateArrowLane`, `ValidateArrowDoorClearance`, `ValidateArrowCooldown`,
`ValidateArrowPeriodicSlack`. Disguise: a disguised launcher takes its host's colour and switches to
the honest colour on fire. Built on `RoomTrap` and `TrapMotion.Travel`, with no `MovingTrap` change.
Limits: one arrow per fire; horizontal only; no lane may cross a moving Solid's swept path or a
`CollapsingFloor`, because a pushed cat breaks the speed cap; a diagonal graze on the second sample
can be missed; the existing `ValidatePeriodicSlack` also runs on the launcher width (harmless);
camera visibility of the tell in follow mode is deferred to KIT-4 (it needs the dead zone,
look-ahead and lag in `CameraMath`); the `OverlapBox` kill test sees Box2D's contact skin (about
0.01 u), so a lane within about 0.01 u of the cat counts as touching, while the measured capsule
width stays ≥ 0.44, so the speed cap holds. Dev room: Trap Lab room 3.

### D-079 · 2026-09-24 · Accepted

**Decision:** Route validator (PAX-075, KIT-3a).
(1) **What a room declares.** A solution route and one betrayal route per real, lethal betrayal, as
scripted actions in code-as-data: `Hold(Right|Left)`, `Release()`, `Jump()` (a one-tick press),
`Until(condition)`, `For(n)`, `Margin(name, from, to, atLeast)`, with `.Timed(Shift|Hesitate)` on a
step. Conditions read the previous tick's post-physics record, before this tick's motor step. A
betrayal route reuses the solution's steps through `Route.PrefixOf`. Routes live in
`LevelRoutes` (keyed like `LevelLayouts`) and `TrapLabRoutes.Room3`, outside `SoloRoomDefinition`.
(2) **How they're checked.** `LevelLayoutValidator.ValidateRoutes` (not part of `Validate`) calls
`RouteValidator`, which replays each route through the real game code in a `RouteSession`: the open
scenes are swapped for one empty scene and restored from disk afterwards, `Physics2D.simulationMode`
is `Script` while it is open, and plain logs are filtered. It refuses to start if any loaded scene
is dirty, untitled or not. The room is built by `SoloRoomBuilder` as in a shipped level, the cat is
`Cat_Player.prefab`, and the rest is the gameplay part of `_LevelTemplate`. Scripted input reaches
`LocalHumanDriver` through the real `CatInputRouter`, as a plain `ICatCommandSource` added to its
`validSources`. Each tick runs `ObserverSet.FixedUpdate` (driver, motor, then `RoomManager`'s traps,
hazards, bounds and door) and then `Physics2D.Simulate(Time.fixedDeltaTime)`. A route's Move is
screen-relative (D-049). The harness never decides whether a kill happened: `RoomDeath` does, and
the harness only names the killer by re-running each kind's own kill test on the kill tick. Zero or
several matches is an "ambiguous kill" failure.
(3) **What passes.**
- The solution completes the room. For each timed step, the window is the count of surviving start
  ticks in the contiguous run that contains the authored tick (d = 0), walked outward one tick at a
  time over ±25 (no bisection). `Shift` moves the step's start tick both ways; `Hesitate` inserts
  d idle ticks before it (D-056 (1)'s "from rest"). Each timed step is swept alone. A window passes
  at ≥ 12 (D-056 (1)). A `Margin` is the gap between two events' first ticks in the same replay,
  used for L002's Lift, where a late landing is still carried rather than killed; it fails if
  either event never happens.
- Each betrayal dies at its declared killer and cause. Lead = min(kill tick, the killer's first
  lethal tick where its kind exposes one; for arrows, fire + tell) − the first tick at which
  `revealedBy` (by default the killer) visibly changes. "Visible" is every `SpriteRenderer` in the
  element's subtree: enabled and active, colour, sprite, world position and rotation. A gravity flip
  is revealed by the pseudo-element `Cat.Gravity`. It passes at a lead of ≥ 6 (D-057), and fails if
  there is no visible change.
- Two replays of the solution give the same per-tick record.
(4) **Fidelity.** The harness reproduces the motor: 0.12 u/tick run, rest→max in 5 ticks (0.36 u),
stop in 4 (0.168 u), jump apex 24 ticks and 3.339 u, a 48-tick 5.76 u flat jump, coyote 5 and buffer
6 (D-077). It gives L002's Lift margin (fire tick − landing tick) as 16, and 14 at the pre-PAX-078
trigger x 19.55. So PAX-078's Lift move wasn't needed by the real code; the layout stays (PAX-075
§4). The boundary is x 19.30 (exactly 12, passes); x 19.20 gives 11 and fails. For L004, the real
code doesn't reproduce D-076 (2)'s early-flip kill: from `FalseLanding`, no full-speed, stopped,
hesitating (0–40 ticks) or stepper-parity take-off is killed by `Block_1`. The right-holding
fast-flip window is 19 ticks (d −15..+3), pinned. This supersedes, in part, D-076 (2)'s box-model
L004 figures (14.83–15.83 and the `Block_1` early-flip band); D-076 is not edited.
(5) **Measured per room.**

| Room | Timed window / margin | Betrayal leads |
|---|---|---|
| L001 | spike-jump Hesitate 26 (open) | `Collapse_C` 21, `Spikes_A` 24, `Block_A` hesitation (33 ticks) 17 |
| L002 | Lift margin 16; `ReceiverBlock` Hesitate 26 (open); Sweep Shift 51 (open) | `ReceiverBlock` 12, `Collapse_C` 21, Sweep 24 |
| L003 | `PeriodicUp` Hesitate 15 | `Collapse_C` 21, `CeilingSpikes` 19, `ExitSpikes` 22 |
| L004 | `Flip_A` take-off Shift 17 (d −13..+3) | `SourceSpikes` 21, `CeilingHiddenSpikes` 20 |
| Trap Lab room 3 | Hesitate 26 (open); `ArrowB` hop Shift 15 | `ArrowB` 6, `ArrowC` 6 (both exactly 6: a pass with zero margin) |

(6) **What it satisfies.** D-069 (1)'s "at least one valid route" is now enforced for every
`LevelLayouts` entry. A level with no routes fails `ValidateRoutes`.
(7) **Redundant checks, kept until a cleanup ruling.** `ShippedLevelTimingTests`
`L002_Lift_FullSpeedLandingHasTwelveTicksBeforeItsTrigger_AtTheRealRate` (covered by the L002 Lift
margin). `L004_EarlyFlipBand_IsKilledByBlock1` and
`L004_FastFlip_RightHoldingWindowIsAtLeastTwelveTicks_AndClearsBothBlocks`: box-model results
contradicted by the harness. `L004_NoFullSpeedTakeoff_CrossesSourceSpikesWithoutFlipping` is only
partly covered (routes prove only authored take-offs). `RoomStepper` becomes redundant once those go.
`LevelLayoutValidator.ValidateRevealLead` overlaps the measured lead, but it is static and cheap.
Not redundant: `LevelSceneTimingTests` (saved scenes against layouts) and the `SoloRoomsLayoutTests`
V3 narratives (D-076 (5)).
(8) **Limits.**
- Only authored routes are proven. There is no search for unintended routes, and no proof that
  there are no soft-locks.
- Claimed betrayals with no betrayal route, as measured:
  - L004 `Block_1`: no route dies on it (window 19, pinned);
  - L002 `Block_A`: no route dies on it (take-offs 25.2–28.4, waits 0–30, no brake); the Sweep kills
    the cat that runs on;
  - L003 `PeriodicUp`: honest;
  - L003 `Retreat` and L001 `Retreat`: non-lethal, so KIT-3b;
  - L004 `Block_2`: never kills;
  - L004's floor run: gone (D-076 (2)).
  L004 `FalseLanding` and `Block_1` go to PAX-080 (KIT-3b).
- On-screen visibility isn't modelled: "visible" means a renderer change, not one inside the camera
  frame.
- Only `DeathCause.Hazard` kills occur in these rooms. `Fall` and `OutOfBounds` betrayals are
  supported, but none is exercised.
- Windows are counted over ±25 ticks; a side that doesn't fail inside that range is reported as
  open.
- A route session swaps scenes, so it can't run over a dirty scene; tests recreate the Test Runner's
  own untitled scratch scene first.

### D-080 · 2026-09-24 · Accepted

**Note (PAX-091, 2026-09-27):** surface coverage works per storey, like D-074's trigger coverage: the band is the root
trigger's storey (`StoreyLow` to `TryStoreyCeiling`), and the top strip must lie beyond the trigger from every side the
approach search reaches it from, not only the checkpoint's side.

**Decision:** Troll-route kit (PAX-080, KIT-3b).
(1) **The fake platform.** A new element kind, `SoloRoomElementKind.FakePlatform`, appended to the
enum. It is builder-only: `TrapKitSetup.BuildFakePlatformCore` builds a `CollapsingFloorTrap` whose
body collider is a trigger, with Overlap, Once and delay 0 forced by the builder; no runtime class
changed. It has a Floor's colour, sorting order and size from the same rect, so it looks like a
floor, and it never holds the cat up (the motor's ground cast ignores triggers). It reveals by
vanishing on the first trap step after the first physics step that leaves the cat touching it
("touch + 1", the same ordering as a `CollapsingFloor`): contact is the cat's capsule against the
fake's box grown by the collapse's touch skin (0.05 u). It resets with the room (D-041).
`ValidateFakePlatformSettings` rejects a fake whose data has non-default trap settings or a trigger
box. `ValidateReach` rejects a `RequiredJump` that lands on a fake (by destination name, or by its
landing point on a fake's top). A fake is neither a chain source (the existing chain validator
rejects it) nor a chain target (the settings rule rejects it). It counts as a gameplay element
for D-058 containment.
(2) **Betrayal outcomes** (extends D-079). A betrayal route ends one of two ways. **Dies**: at its
declared killer and cause, with a lead of at least 6 from `revealedBy` (the existing constructor).
**Recovers** (`Betrayal.Recovers(name, revealedBy, route)`, goal forced to `RoomComplete()`,
`revealedBy` required): no death, the room completes, and `revealedBy` changes visibly before the
completion tick; results go to `RouteReport.Recoveries`, so `Leads` holds deaths only. A solution
or betrayal that ends alive at the 1500-tick replay cap or the 600-tick step cap fails with
"possible soft-lock (D-053)".
(3) **Non-lethal coverage** (extends D-074; `LevelLayoutValidator.ValidateSurfaceCoverage`). The
betraying surfaces are fake platforms, collapsing floors, and Solid `MovingTrap`s that move down
or sideways at all (stricter than "no longer cover their top strip's x span"; no result in scope
changes). A surface's danger is its top strip: the element's width, one cat-collider height tall.
Fake platforms and Overlap, non-periodic, unchained collapsing floors cover themselves by touch.
Every other betraying surface needs its trigger (for a chained surface, its chain root's trigger)
to be D-074's cut against the strip: over the trigger's x span it covers the cat's band from the
lowest standable top to the ceiling underside, and the strip lies beyond its near edge as seen
from the checkpoint. Periodic surfaces are left to `ValidatePeriodicSlack`; none is in scope.
Scope: L001–L004 and Trap Lab rooms 3 and 4. The one exemption is L004 `FalseLanding`
(`SurfaceCoverageExemptions`): its trigger doesn't cut the band. Setting a learned-bypass reason on
it instead (the ruling's branch (a)) was ruled out because `TriggerCoverageTests` asserts that
L001–L004 have no learned bypass, so branch (b), a named exemption, was taken.
(4) **Dev room: Trap Lab room 4.** Ten platforms on one screen. The valid route is Start_Floor →
Up_1 → Up_2 → Exit_Perch, where the door sits on the raised perch. The betrayals:
- `Stone_A`, a fake that looks like the start floor going on: dies in the pit.
- `Thin_Collapse`, a thin collapsing stone between Up_1 and Up_2: dies in the pit.
- `Ledge_End`, a fake that looks like Up_2 going on: drops the cat into the Gutter, a dead end it
  climbs out of back to Up_2. It recovers.
- The `Bridge`, the low route on past the exit: crossing `ArrowD`'s trigger fires the arrow over
  it, then the Bridge collapses (chained from the arrow, D-078): dies in the pit.
The door sits left of `ArrowD`'s full-band trigger because (3) needs that cut and the solution must
never fire the arrow. "Exactly one valid route" holds by design and play, not by proof: there is no
route search. Measured: the Up_1→Up_2 jump window is 22 ticks (d −10..11) and the Up_2→Exit_Perch
window 21 (d −5..15); leads are `Stone_A` 26, `Thin_Collapse` 28 and the Bridge (revealed by
`ArrowD`) 45; `Ledge_End` recovers. The solution never fires or visibly changes any betrayal
element.
(5) **L001–L004.** Their route results are unchanged from D-079 (5), pinned exactly.
`FalseLanding` and L002 `Block_A` are claimed but aren't a betrayal of any route; `FalseLanding` is
exempt from surface coverage ((3), branch (b)). L004 `Block_1` is disputed: the harness finds no
kill, but two developer plays were killed on an early hop (PAX-078 and 2026-09-24). PAX-081
resolves it. There is no layout change (D-069, PAX-078 R15).
(6) **Process.** PAX-075's code/docs commit split was waived for PAX-075 only; e23cc0d holds both
and is not rewritten (PAX-080 pre-flight A1).
(7) **Limits.**
- Recovers proves only the authored path; there is no search for other soft-locks.
- Contact is the touch skin, not a zero-distance overlap.
- A fake platform is neither a chain source nor a chain target.
- Not play-tested yet: whether the fakes read as floor, the Thin_Collapse hop and the Bridge lure.
PAX-083: L001 `Retreat` declared as Recovers: the door moves at t252 and the room completes at t345, pinned in `ShippedRouteResultsTests`. Its `revealedBy` is `Door` on purpose: `DoorRetreatTrap` moves the door, not its own element, which never changes visibly. L003 `Retreat` stays unclaimed: the retreat fires on the solution itself; it's part of the solution, not a betrayal.

### D-081 · 2026-09-24 · Accepted

**Decision:** L004 `Block_1` fidelity (PAX-081).
(1) **What caused the dispute:** the play reports, not the harness. The developer confirms that no
falling block killed them in any L004 play. D-080's "disputed" line rested on deaths the play
reports wrongly blamed on `Block_1`.
(2) **What was checked (read-only):**
- `Level_004.unity` matches `L004Layout` field by field, so the scene isn't stale.
- Play and the harness run the same per-tick pipeline: `ObserverSet`, then the motor, then the
  `RoomManager` trap step, then physics.
- The scene-only `Stepped` subscribers never write the cat's state or its command, and never step a
  Reality A trap.
- Input reaches the motor with 0 ticks of latency. The jump-press latch is the same in Play and the
  harness.
(3) **`Block_1`:** kills no route. D-079 (4) stands. D-080's L004 `Block_1` line is superseded
("kills no route"); D-080's text is not edited.
(4) **Known gap:** the route format can't express a stick magnitude below 1 (`VirtualStick` is
continuous past its 0.15 dead zone, and the motor scales speed by it). No current finding depends
on it. Address it before touch input feeds measured windows (KIT-4 thresholds, PAX-069).
(5) **Cleanup:** `L004_EarlyFlipBand_IsKilledByBlock1` (box model) is contradicted by the harness.
Its removal belongs to the cleanup ticket.
(6) **Developer feel feedback**, recorded but not acted on here: falling blocks fall too slowly; the
jump is far too high; the run speed is too fast.

### D-082 · 2026-09-24 · Accepted

**Decision:** Movement feel (PAX-082). The cat's numbers come first; levels fit the cat.
(1) **Run:** max speed 6 u/s, acceleration 60, deceleration 80, max fall 20: unchanged. D-081 (6) called
the run too fast; after the tuning session the developer kept it.
(2) **Jump:** `jumpHeight` 3.2 → **1.6** (the developer: "twice the height it should be"); gravity 30
unchanged (`Cat_Player.prefab`). Apex 3.2 → 1.6 (discrete 3.339 → 1.700, 24 → 17 rising ticks); flat
airtime 0.924 → 0.653 s (48 → 34 ticks); full-speed flat reach 5.54 → 3.92 u (discrete 5.76 → 4.08),
allowed (0.75) 4.16 → 2.94 u. Limits used for the refits: 2.37 u rising 1 u, 3.34 u dropping 1 u, spike
patch at most 2.65 u wide, rise at most 1.6 u.
(3) **Moving traps 20% faster:** every falling block (incl. L003's rising `PeriodicUp`) and every arrow
0.30 → 0.36 u/tick; every `moveTicks`/`returnTicks` divided by 1.2 and rounded (24 → 20, 36 → 30, 40 → 33,
18 → 15). Delays, holds and cooldowns unchanged. The values live in the layouts (`L001`–`L004Layout`,
`TrapLabLayout`); there is no kit-wide default. Level_Solo01 (`SoloRoomsLayout`) is unchanged.
(4) **Derived:** door clearance 0.12 u (MaxSpeed × tick) unchanged; coyote 5 and buffer 6 ticks unchanged.
(5) **Levels rebuilt around the cat.** The developer asked for it, which overrides §4 (c)'s stop:
| Level | Change | Measured (windows; leads) |
|---|---|---|
| L001 | Platform_B a 0.5 step over a 1.5 gap; Collapse_C 2 → 1.6; the spike jump from x 23; the Block_A hesitation 33 → 34 (kill band 30–38) | 26; Collapse_C 30, Spikes_A 24, Block_A 14 |
| L002 | Platform_B a 1.0 step over a 1.25 gap (3.0 down to Floor_C); the spike/Sweep crossing 4 → 2.8 (spikes 0.75, Sweep 0.8, door ledge from x 30.8); take-off 28.5 | 26, 51, Lift margin 16; ReceiverBlock 10, Collapse_C 21, Sweep 23 |
| L003 | Platform_B a 1.0 step (3.25 down to Floor_Pre); Collapse_C 1.6; Flip_A where the jump over it peaks; Flip_B and its ExitSpikes trigger raised to y 4.5; the ceiling wait at x 22.4; the ExitSpikes betrayal a straight drop | 26; Collapse_C 21, CeilingSpikes 19, ExitSpikes 15 |
| L004 | two 0.5 steps over 1.5 gaps; Platform_C to x 17 (3.0 down to FalseLanding); braked release 18.95; Flip_A low and wide (x 23.75–25.25, y 0.8–2.8) so every hop clearing SourceSpikes flips; Flip_B raised | window 20; SourceSpikes 21, CeilingHiddenSpikes 20 |
| Trap Lab 3 | walls 0.6; Backboard down to y 1.5; ArrowC's lane y 1.7 (tell 6); PillarB take-off 20.6 | all rules pass |
| Trap Lab 4 | Up_1/Thin_Collapse/Up_2 at 1.1, the perch at 2.1 (out of reach from the Gutter); the climb back jumps straight up, then steers onto Up_2 | all rules pass |
(6) **Re-pinned:** `ShippedRouteResultsTests` to (5)'s numbers. Outside §7's list, accepted: the route files
`L001Routes.cs`–`L004Routes.cs` and `TrapLabRoutes.cs`, refitted with the geometry (§12 amendment 1); and (§12
R10) `RouteHarnessFidelityTests` apex 24 → 17 ticks and 3.339 → 1.700; flat jump 48 → 34 ticks and
5.76 → 4.08; coyote and buffer detect a jump at Vy > 9 (was 13; a test threshold, 0.80 under the 9.798
launch); the L004 fast-flip window 19 (d −15..+3) → 21 (d −18..+2).
(7) **Frozen scaffolding pinned to the old cat:** `SoloRoomsLayoutTests` (§12 R5) and `LevelLayoutTests`
parity (§12 R9, through `LevelLayoutValidator.ValidateWithMotor`; `Validate` is unchanged) use 6 / 60 /
80 / 20 / 3.2 and gravity 30. Split fidelity (L00N = SoloRoomsLayout room N) has ended:
`SplitFidelity_L00N…` is removed (§12 R8). SoloRoomsLayout stays frozen scaffolding (D-076).
(8) **Removed:** the D-079 (7) box-model tests `L002_Lift_FullSpeedLandingHasTwelveTicksBeforeItsTrigger_AtTheRealRate`,
`L004_EarlyFlipBand_IsKilledByBlock1`, `L004_FastFlip_RightHoldingWindowIsAtLeastTwelveTicks_AndClearsBothBlocks`
(§12 R6). `L004_NoFullSpeedTakeoff_CrossesSourceSpikesWithoutFlipping` and `RoomStepper` stay; it passes.
(9) **Tuning tools:** `Parallax.Core.JumpReach` (the reach math, moved verbatim from the validator);
`DebugTools/MovementReadout` (apex, airtime, reach, allowed reach, run speed, gravity), shown in Editor Play
by the `PARALLAX/Debug/Movement Readout` toggle; the motor already read its config every step, so live
editing needed no code. A block's `delayTicks` is read in `Awake`, so it is tuned before Play.
(10) **Tests:** 624, all green. Editor-only; the developer played the rebuilt L001–L004 and approved the feel.
Not device-validated (D-064). Open: `Sandbox_TrapLab.unity` not rebuilt; Trap Lab 3–4 unplayed.

### D-083 · 2026-09-24 · Accepted

**Decision:** Precision sections, difficulty bands, bait gaps and the camera tell rule (KIT-4, PAX-076).
(1) **The marker:** `PrecisionSection[] PrecisionSections` on `SoloRoomDefinition` (a name and a room-local region,
edges inclusive), not an element, so the builder, the room bounds and the camera frame never see it. A `RequiredJump`
is a precision jump when its take-off and its landing are both in one section; a straddling jump (the entry and exit
jumps) is checked with D-056's 0.75 and isn't an error. A periodic trap (its footprint) or periodic arrow (its lane)
wholly inside a section uses the section's slack; one partly inside keeps 12. A timed route step uses the section's
slack when the cat's recorded position is inside one section at every tick its window spans (authored tick + Low to
+ High); otherwise 12. This covers chain and collapse timing inside sections. Leads stay at 6 everywhere (D-057).
(2) **Bait gaps:** `BaitGap[] BaitGaps` on `SoloRoomDefinition` (`Name`, `TakeoffX`, `TakeoffPawHeight`, `TargetX`,
`TargetPawHeight`; the X values are the two platforms' edges). From the best take-off (full speed, trailing side at
the edge, all 5 coyote ticks), at fraction 1.0, the target is out of reach by at least one run tick (MaxSpeed × tick,
0.12 u); otherwise a validator error naming the gap, the reach and the margin. A target higher than the jump can rise
is out of reach. Allowed in every level, both bands; never on a solution route. `JumpReach` gained only `EdgeReach`
(speed × (flight + coyote) + collider width). It ignores the height lost during the coyote time, so it overstates
the reach: room 5's harness attempt came down through the target's height with its leading side at x 43.06, against
the analytic 43.20. The D-082 discrete excess (4.08 u against 3.92 u flat) did not show up here.
(3) **Thresholds:** `PrecisionThresholds` (Editor assembly; `Assets/_Game/Data/PrecisionThresholds.asset`, made by
`PARALLAX/Setup/Precision Thresholds (PAX-076)`), reach 0.85, slack 8 ticks. They're provisional; PAX-069 sets device
values. This amends D-069's Consequence ("thresholds come from a Pixel 8a session"): provisional values here, device
values in PAX-069. The validator rejects a room with a section and no asset, and values outside 0.75 ≤ reach < 1,
0 < slack ≤ 12.
(4) **The band (D-065):** a precision section in a level numbered 1–10 is an error that names the level. The number is
the level id's place in `LevelListConfig` plus one; an id that isn't listed (the Trap Lab, test fixtures) is exempt.
`DifficultyBandTests.NoDevRoomIsAListedLevel` keeps every listed id a `LevelLayouts` level and no Trap Lab id listed.
(5) **Room 5** (Trap Lab, origin 225, 49 wide, follow mode at every aspect): eight thin platforms (2 × 0.5) over a
pit, the gaps alternating 1.9 (a 2.9 u jump, within 0.75) and 2.2 (3.2 u, precision only); `Block_P5` falls on a cat
that stops on P5; the bait gap `Exit_Gap`, 6.5 u from P8 to the Exit (best reach 5.80 u, margin 0.70 u); the way
round is a drop onto `Step`, then a 3.0 u rising jump to the Exit (precision). One section, `Precision_Run` (x 6–48).
Solution windows 14 and 26 (both need 8); `Block_P5` lead 14. Without the marker, the four 3.2 u jumps and the 3.0 u
jump fail 0.75. The bait attempt (full speed, the jump on the 5th coyote tick) dies in the pit. It's a route test
(`TrapLabRoutes.Room5BaitAttempt`), not a `Betrayal`: nothing reveals, so there's no lead to measure.
(6) **The camera tell rule:** for every betrayal that dies, the reveal (RevealedBy's first visible change) is on
screen for at least 6 ticks before it can first kill (`RouteValidator.Lead`'s end: the kill, or an arrow's first
lethal tick), at 4:3, 16:9 and 20:9. `LevelCameraFollow`'s step moved verbatim into `CameraMath.Step`
(`FollowState`, `FollowParams`), proven bit for bit by `CameraStepEquivalenceTests`. The rule runs that step over the
replay's recorded cat Transform positions:
- at 30 and 60 fps, 4 frame phases each, and a starting look direction of −1, 0 and +1 (a respawn snap keeps the last
  attempt's direction);
- the drawn cat is interpolated between the previous tick's pose and the current one, so up to one tick behind;
- the frames drive only the smoothing and the interpolated target. At tick k the element is visible when its bounds
  at tick k overlap the view of the latest frame rendered at or before tick k (after it vanishes, the bounds it was
  last drawn at). No frame delay is added: the lead counts simulation ticks (D-057, §13 R10);
- the on-screen lead is the end tick minus the first tick from which the element is visible at every tick up to the
  end, and must be at least 6. The worst of the 24 cases counts, and fit mode passes trivially.
No extra replays per camera case. The harness records the cat's Transform position and each element's rendered bounds
(added fields; `SameAs` is unchanged).
(7) **Results (on screen / lead, worst case; 20:9 is fit for all 32-wide rooms):** outcome (a) for L001–L004. Every
reveal is on screen for its whole lead at every aspect; the camera's position never cuts one off.
| Room | Reveal: 4:3 and 16:9 |
|---|---|
| L001 | Collapse_C 30/30, Spikes_A 24/24, Block_A 14/14 |
| L002 | ReceiverBlock 10/10, Collapse_C 21/21, Sweep 23/23 |
| L003 | Collapse_C 21/21, CeilingSpikes 19/19, ExitSpikes 15/15 |
| L004 | SourceSpikes 21/21, CeilingHiddenSpikes 20/20 |
| Trap Lab 3 | ArrowB 6/6, ArrowC 6/6 |
| Trap Lab 4 | Stone_A 26/26, Thin_Collapse 25/25, ArrowD 45/45 |
| Trap Lab 5 | Block_P5 14/14 (also at 20:9) |
Trap Lab 0–2 declare no routes, so the rule has nothing to measure there. The Trap Lab scene uses `CatCameraFollow`;
the rule checks its rooms as if they were levels. A first version counted a 30 fps frame's delay and made Trap Lab 3's
tell-6 arrows fail (5/6); §13 R10 removed that, and Trap Lab 3 is unchanged.
(8) **Unchanged:** D-057's 6-tick lead and D-078's minimum tell of 6. The camera rule only asks whether the element is in
the view.
(9) **Limits:** partial stick magnitude isn't modelled (D-081 (4)); no device data yet.
- The lead counts simulation ticks. On a 30 fps screen a reveal can appear up to one frame later. Whether 6 ticks is
  enough on a real screen is a Phase H device check. (30 fps is Unity's documented mobile default; no `targetFrameRate`
  is set.)
- The camera model starts every route from a respawn snap (Start). A retry is covered by the three starting look
  directions.
- The bait-gap reach ignores the height lost during coyote time. That overestimates the reach, so it's on the safe side
  for bait gaps. A flat gap exactly at the pass boundary isn't replayed in the harness.
(10) **Trap Lab refit (developer request during §9):** `PARALLAX/Setup/Trap Lab (PAX-045)` built only missing rooms, so
rooms 3–4 kept their pre-PAX-082 geometry in `Sandbox_TrapLab.unity` (D-082's "run the Trap Lab menu" never applied
the refit). `TrapLabSetup.SyncRooms` now builds each room fresh beside the scene's copy, compares every object path,
component and serialized field, and rebuilds only a room that differs; nothing outside a room references into it, and a
second run changes nothing. Rooms 0–2, never refitted to the 1.6 jump, now fit it: `ThinPlatform`'s top 2.0 → 1.1,
`FixedPillar` and `Crusher` 2 → 1 tall; every trigger spans the cat's band (D-074) and `PeriodicSpikes` has a 6-tick
reveal (D-057). Rooms 0–2 declare no routes; `TrapLabRefitTests` replays a way through each.
(11) **Tests:** 675 EditMode (624 + 51 new); `TrapLabRoom5Tests.TheCommittedThresholdsAsset_…` needs the asset from
`PARALLAX/Setup/Precision Thresholds (PAX-076)`. `PrecisionFixtures.cs` joined the allowed files (§13 R9).

### D-084 · 2026-09-24 · Accepted

**Decision:** Level camera judder fix (PAX-082 follow-up). D-083 stays reserved for KIT-4.
(1) **Cause:** `LevelCameraFollow` took the look-ahead side from the sign of the cat's x change since the last
frame. A cat at rest still moves by a float step between frames (interpolated Transform), which flipped the
side and swung the camera's target by up to ~9 u (2.5 look-ahead + 2 dead zone, each side). Measured live:
the cat still at x 13.5679893 while the camera moved 15.01 → 12.40.
(2) **Fix:** `CameraMath.ResolveLookDirection`: the side changes only after the cat travels more than
`LevelCameraConfig.lookAheadFlipDistance` (default 0.1 u, under one tick of running) the other way from the
furthest point it reached. Frame-rate independent. Only the direction choice changed; the dead zone, the
look-ahead offset, the bounds clamp and SmoothDamp are unchanged. Runtime files: `CameraMath.cs` (Core),
`LevelCameraFollow.cs`, `LevelCameraConfig.cs` (Gameplay).
(3) **Tests:** `CameraLookAheadTests` (4), all seen red against the old sign rule; the camera-level test
failed with a 5 u jump of the target from a one-float-step jitter.


### D-067 · 2026-09-25 · Accepted

**Decision:** No randomness. Every room is exactly the same on every attempt and every playthrough. PAX-058 (bounded randomness) is dropped, and its number isn't reused.
**Why:** the core loop (D-040) is see the room → get betrayed → learn the trick → beat it, and a fixed room keeps every lesson valid. The troll comes from authored betrayals, not chance.
**Considered, and not taken:** the betrayal re-picked after each death (with a safe route beating every version); room variants per level start; timing jitter; cosmetic variation.
**Supersedes:** D-062's open point on luck.

### D-068 · 2026-09-25 · Accepted

**Decision:** Free levels and price. Levels 1–10 are free, and they're the hook. After level 10, the player either pays 4.99 once to unlock the full game (one in-app purchase, restorable), or waits 12 hours for the next level to unlock free.
**Open, for PAX-066:** when the 12-hour wait starts; whether the timer runs while the app is closed (an unlock timestamp); protecting it against changes to the phone's clock (accept it, or use server time); regional price tiers; the choice screen after level 10; the total number of levels (the roadmap says 50).
**Supersedes:** D-062's open point on the business model.

### D-085 · 2026-09-25 · Accepted

**Decision:** Band-1 content (PAX-059; rulings in `Docs/0_TASKS/PAX-059_halfB_rulings.md`): levels 1–10, the free
hook (D-068). They die by troll and memory, not
execution: many deaths the first time (one per trap), each one visible and learnable. Execution stays at D-056's
numbers; there are no precision sections (D-065, D-083). The design guide is `Docs/LEVEL_DESIGN_GUIDE.md`; where it and
this entry differ, this entry wins.

(1) **Checked per level** (`LevelLayoutValidator.Band1.cs`, `Band1LevelTests`):
- lethal betrayals, counted as distinct killers over every Dies route (dead ends included): ≥ 5 (L1–5), ≥ 6 (L6–7),
  ≥ 7 (L8–10);
- of those, in sequence on the way to the door: ≥ 4 (L1–5), ≥ 5 (L6–10). `SequentialChain` is a structural proxy
  (betrayals that share a strictly longer start of the solution, distinct killers), not a proof that a cat survives
  each earlier trap;
- dead ends: ≥ 1 (L1–5), ≥ 2 (L6–10), declared as Dies routes named "Dead end…" (§2.1 of the ticket only said "may";
  this makes it a rule). The validator also counts Recovers routes, but by (2) a band-1 dead end kills; every band-1
  dead end is a Dies route. Each level needs at least one real side route, and every way off a dead end's approach
  (stepping off, running off, jumping, steering in the air) is declared and dies;
- width ≤ 32; rooms may be taller than wide and the camera follows vertically (amends D-040/D-069 for band 1);
- the door at least half the width **or** half the playable height from the start. The playable height runs from the
  lowest floor top to the highest ceiling underside (not the solid ground and shafts under the floor);
- the solution: 500–1500 ticks (10–30 s once known). L001 is the one exemption: ≥ 280 ticks (the Architect said
  "about 300"; the developer set 280 once L001's blocks moved next to their triggers);
- no precision sections, and no learned bypasses (D-074 (4)) in band 1;
- plus everything the kit already checks: timed windows ≥ 12 (D-056), leads ≥ 6 (D-057), the camera tell rule at
  4:3, 16:9 and 20:9 (D-083), trigger and surface coverage (D-074, D-080), door clearance, arrow lanes (D-078), and
  `ValidateFallingBlockLanding` (the kill quirk).

(2) **From the developer's play of levels 1–5** (2026-09-25), checked from half A on:
- a floor that gives way on touch is out of reach of a jump from below (`ValidateTrapFloorHeadroom`); treads that
  should go only under a standing cat trigger from a box over their top;
- a falling block or a moving hazard is set off within 3 u of it, through its chain root's trigger, so it can't be set
  off from afar and waited out (`ValidateTriggerNearTrap`);
- the level scene's cat starts on the room's checkpoint (`LevelSetup.CatStart`);
- **dead ends kill.** Nothing may leave the cat alive and unable to finish (a soft-lock). A non-lethal betrayal
  (L003's S1_Mid) must leave the way forward open.

(3) **Direction mix across 1–10** (`Band1DirectionMixTests`): ≤ 3 plain left to right, ≥ 2 climb, ≥ 2 descend,
≥ 3 start in the middle third or the right third, ≥ 2 double back. "Middle" is the middle third of the width
(x 10.7–21.3 in a 32 u room), so L004 (x 9.5) doesn't count; the mix holds without it. As built: plain 1 (L1),
climb 6 (L2, L3, L4, L7, L9, L10), descend 3 (L5, L6, L8), middle or right start 5 (L2, L5, L6, L7, L10), double
back 5 (L2, L4, L6, L9, L10).

(4) **No tells:** trap floors (CollapsingFloor, FakePlatform) draw at sorting −1 over solid ground, and a trap floor
over a pit fills its whole shaft; no honest hazard singles out a trap floor or a lift in the direction it moves the cat
(`ValidateBand1Tells`, which covers lifts from PAX-059b's review on; L008's roof spikes run 10 u along the roof); blocks sit flush in their host; disguised launchers take their host's colour;
nothing visible points at a trap (no hazard under a trap floor and nowhere else on its storey); honest hazards
(periodic spikes, red sweeps, L008's roof spikes) stay visible. Coverage is storey-aware (C3); every
element sits inside the room's frame (C1). No runtime disguise was added; `02_ARCHITECTURE.md` is unchanged.

(5) **Long chains** (Architect, half B Q3) link only permanent changes (floors that give way, spikes that come up and
stay up), and the player sees the chain happen, or its result, before walking into it. Blocks keep local triggers. A
short, local chain may rearm (L007's landing spikes go back down; L008's spikes under the lift's landing too).

(6) **Retry cost (interim, amendment 1 §6, until the developer's playtest):** quick-to-pass traps go first; solutions
aim for ≤ ~20 s in L6–L9 (L6 10.1 s, L7 18.7 s, L8 14.3 s, L9 11.5 s). L010's late death replays ≤ ~18 s (half B Q5,
overriding the amendment's 30 s for L10, the last free level before the choice to pay or wait); later exam levels may
take up to 30 s. Worst late-death replays: L6 t484 (9.7 s), L7 t760 (15.2 s, the dead end past S2), L8 t529 (10.6 s),
L9 t487 (9.7 s), L10 t519 (10.4 s). After the playtest the developer picks (a) mid-room checkpoints, (b) shorter
replays across 1–10, or (c) accept and measure in PAX-070; that ruling is still open.

(7) **Progression (as built):**

| Level | Lesson | Shape |
|---|---|---|
| L001 | The floor lies; blocks right at you | plain, left to right (281 ticks) |
| L002 | Look up (blocks in the slab); the door backs away | climb, starts right, doubles back, door over the start |
| L003 | Towers: the rhythm step lies; don't stop where a jump lands | climb |
| L004 | Gravity flips; the floating flip is a lure | climb, doubles back |
| L005 | Arrows; wait in the nook | descend, starts right |
| L006 | Rhythm: count the spikes, don't wait twice, stand still for the sweep, floors that aren't there; the lid "toward the door" | descend, starts mid, doubles back (505 ticks) |
| L007 | Trust nothing: the floor ahead drops; the jump meets an overhang; the drop comes back with spikes at the landing (wait); L3 reversed | climb, starts right (934 ticks) |
| L008 | Chains: a block's landing brings up spikes ahead; a lift into visible roof spikes; a collapse brings up spikes on S1 | descend (713 ticks) |
| L009 | Both ways: walking under the lure fires an arrow (wait in the nook); the real flip fires a roof arrow | climb, door over the start, doubles back (574 ticks) |
| L010 | The exam: L2, L3, L6, L9/L5, L8 and L2/L1 in a new order, plus L4's lure and a stair "straight up to the door" | climb, starts mid, door over the start, loops (512 ticks, 24 u wide) |

Every level's windows, killers and leads are pinned in `ShippedRouteResultsTests`.

(8) **L001–L004** were redesigned; their pre-PAX-059 results are superseded (L001: `Collapse_C=30, Spikes_A=24,
Block_A=14` → six betrayals and one Recovers; L002: windows `26, 51`, margin `16`, three leads → six windows, seven
leads; L003: three leads → five and one Recovers; L004: two leads → six).

(9) **Kit findings** (half B amendment 1, §3): MovingTrap Solid lifts the cat cleanly (used in L008); pushing it
sideways and a lip rising in front of or under it are unproven, and two retreats on one door don't compose, so those
patterns wait for kit-gap tickets. Also found while building: a chained HiddenSpikes' delay is its `revealDelayTicks`
(`delayTicks` is ignored); a falling block pushes an airborne cat instead of killing it (every falling block in
L1–L10 can meet a cat that jumps under it while it falls, so this kit gap is a priority before PAX-070's playtest); a MovingTrap needs its
trigger box even in Periodic mode; `Stopped(arrow)` is true during the arrow's tell.

(10) **Limits:** "dies the first time" is proven only as a route property. Real first-play deaths are measured with
new players in PAX-070. The developer's play of levels 6–10 is still to come.

**Supersedes:** D-040's 10–20 s room size and D-069's oversize-only-for-precision, for band 1.

### D-086 · 2026-09-26 · Accepted

**Decision:** The spear (PAX-084, KIT-5): a variant of the arrow (D-078) for levels 11+ that fires once and sticks in the
wall at its lane end, where it becomes a small platform. Rulings: `Docs/0_TASKS/PAX-084.md` §11.

(1) **What it is.** `ArrowLane.Spear` (`ArrowLane.SpearLane`: 1.4 long, 0.4 thick, 1.2 u/tick, tell 8). An `ArrowTrap` with
`spear` set and a shaft collider on its arrow child, named `<spear>_Shaft` (the route harness reports ground by object
name). The tell, flight and stop phases are the arrow's. It's lethal on its flight ticks and on its stop tick.

(2) **Stop + 1.** On the first tick after the stop (s = T+N+1, `ArrowMath.StuckCheckTick`), before the shaft switches on,
a cat inside the stop pose shrunk by `ArrowMath.StuckShrink` (0.02) a side is killed through `RoomDeath`
(`DeathCause.Hazard`), and the shaft stays off through the death hold. Otherwise the shaft switches on: a non-trigger
`BoxCollider2D` the size of the visible shaft (Length × Thickness), on the reality's layer, that the cat stands on and
jumps from like any floor. After that the spear is harmless until the room resets (D-041), which puts it back in the
launcher, unfired, with the shaft off. A shaft that is only sometimes solid was rejected.
**Measured:** `Physics2D.OverlapBox` reaches about 0.02 across the two boxes' skins, not the ~0.01 §11 R2 assumed. A cat
resting on the stop pose's top (0.005 above it, where physics leaves a standing cat) survives stop + 1. A cat exactly
touching (a gap of 0) still counts as inside.

(3) **Thickness (amends D-074 (6) for spears only).** `PlatformSizeConfig.spearMinThickness`, default 0.4: one
max-fall tick (20 u/s × 0.02 s). Floors keep 0.5. **Measured:** a cat that walks off a ledge 7.5 u up meets a 0.4 shaft
at 20.00 u/s and lands on it (paw 1.505 on the 1.5 top). No fall-through.

(4) **Speed cap (D-078).** At the committed collider (1.0 × 0.56) and length 1.4 the cap is 1.4 + 0.44 − 0.24 =
1.60 u/tick, so 1.2 fits. `SpearValidatorTests` reads the collider from `CatMotorConfig`.

(5) **Validator** (`LevelLayoutValidator.Spears.cs`, separately named, not in `Validate`):
- `ValidateSpear`: Once only (Rearm and Periodic are errors); length ≥ `PlatformSizeConfig.MinWidth` (1.0); thickness
  ≥ `SpearMinThickness`; the face it sticks in belongs to fixed geometry (Wall, Floor, Ceiling, PitBottom, or the
  room's end), never a moving Solid, `CollapsingFloor` or `FakePlatform`; the stuck shaft overlaps no other element and
  no other arrow's lane.
- Every arrow rule applies to spears unchanged.
- `ValidateBand` becomes the shared "levels 11+ only" check. A spear in a level numbered 1–10 is an error naming the
  level. KIT-6–KIT-9 add their kinds to it.

(6) **Routes (amends D-079 (2)'s killer naming, harness only; no runtime change).** `RouteReplay` counts an arrow as a
kill candidate only on the ticks its own kill test runs, through the runtime's `ArrowTrap.TryGetKillBox`: flight and
stop ticks, plus a spear's stop + 1 check. This applies to every arrow. A stopped arrow was always harmless (D-078), so
naming it was wrong, just never exercised. Every pinned route result was unchanged by this.

(7) **`AfterFire` isn't needed.** The solution route proves the order: before the fire there's no shaft, so an early
landing fails the replay. Jumps onto stuck spears are ordinary `RequiredJump`s, so reach is still checked.

(8) **Trap Lab room 6** (origin 287, 16 wide).
- **Layout.** Crossing the cut at x 3.5 fires `Spear_1` 30 ticks later; `Spear_2` follows 72 ticks after `Spear_1`,
  and `Spear_3` 12 after that. Lanes run at y 0.5, 1.6 and 2.7 (3.6, 2.4 and 1.2 long) into `FarWall`'s face. The
  steps are 1.1 apart, so each is reachable only from the one below.
- **Cover.** `Spear_1` flies at shin height over the whole floor. The only cover is the Dip (0.4 deep); a running cat
  drops into it.
- **Solution.** Wait in the Dip until the volley has stuck, then climb all three spears to the door: 305 ticks.
  Timed window: the climb out, 51 (±25, open at both ends).
- **Betrayals.**
  - Hopping over the Dip dies to `Spear_1`, lead 8.
  - Climbing as soon as `Spear_1` sticks dies to `Spear_2`, lead 8.
- The camera tell rule passes at 4:3, 16:9 and 20:9.
- **Changed from the ticket.** The crate idea was dropped because a 0.6 crate lets a jump skip `Spear_1`. §2.5's
  "jump to the ledge before firing" is impossible here (the cut is on the way in), so the second betrayal replaced it.

(9) **Limits:** horizontal spears only, one spear per fire, no vertical or retracting spears, placeholder colour (an
art ticket later). A stuck spear isn't fixed geometry for other rules: an arrow can't be hosted in it or stop at it.
Not device-tested (Phase H).

### D-087 · 2026-09-26 · Accepted

**Decision:** The inverter (PAX-085, KIT-6): a visible, timed swap of horizontal input on touch, for levels 11+ only.
Rulings: `Docs/0_TASKS/PAX-085.md` §11. **Amends D-040's and D-049's "no controls-reversed death":** reversal as a
*trap* is allowed in levels 11+; accidental reversal (from gravity or projection) stays banned, and the default mapping
stays D-049's screen-relative one.

(1) **What it is.** `SoloRoomElementKind.Inverter` (appended), built as an `InverterTrap` (a `RoomTrap`). Trigger: Overlap
on its own body (a trigger box), a separate child box (the element's secondary box), or Chain. Repeat: Once (default) or
Rearm; Periodic is an error. `InverterSettings`: `DurationTicks` (default 150 = 3 s at 50 Hz) and `Disguised`. Honest
(default): a cyan orb on its body, hidden while fired and shown again when it rearms or the room resets. Disguised: no
body visual, an invisible trigger (D-056 (4)).

(2) **The window, in motor steps (§11 R4).** Fired in the room step at tick T, after that tick's motor step, it inverts
the motor steps T+1 through T+DurationTicks: exactly DurationTicks of them. A refire at T′ restarts the full window at
T′+1. The pure timer is `Parallax.Core.ControlInversion` (`Fire`, `IsActive`, `Remaining`, `IsCueVisible`, `Clear`).

(3) **Where it applies.** `LocalHumanDriver` negates the motor command's `Move`, after `SeatCommandFilter`, and nothing
else: Jump, the interactor's command and the recorded input are untouched. It's the only code path that inverts input.
The driver collects every `IControlModifier` (`{ bool InvertsMove { get; } }`, Core) under its own reality root in
`Activate`; a reality with none (`Sandbox_Realities`) changes nothing. `InvertsMove` is true only while the trap's room
is live, the death hold isn't running, and the upcoming motor step is inside the window. Several inverters active at
once: the cat is inverted while any one is active; they never toggle each other. Play and the route harness use the
same driver.

(4) **Counted in room ticks.** The death hold (D-059) and the pause (D-073) freeze it. The room reset (D-041) clears it.
A pause's `router.ResetTransientState` doesn't. When its room stops being live (door, level complete) the trap clears
itself (`ObserverSet.Stepped`) by the next tick at the latest (the same tick when it's subscribed after `RoomManager`),
so no cue follows the cat into the next room. `InvertsMove` is false from the tick the room stops being live. Disabling
the trap clears it too.

(5) **Cue (§11 R1).** Two child renderers of the trap: `Cue_Ring` (1.5 × 1.1, translucent, behind the cat) and `Cue_Mark`
(0.7 × 0.2, over the cat's head side, never rotated). Placeholder art; the cat prefab and `CatVisualPresenter` are
untouched. On/off is game state, set in the room step: on from the fire tick through the last inverted step, and over
the last 30 ticks off/on in runs of 5, ending on. Their position is presentation only: `LateUpdate` puts them on the
cat, and nothing in the game or the harness reads it. If the ring doesn't read clearly in play, a later ticket adds an
overlay through `CatVisualPresenter`.

(6) **Validator** (`LevelLayoutValidator.Inverter.cs`, separately named, not in `Validate`, like `ValidateSpear`):
- `ValidateInverter`: a duration of 25–500 ticks, never Periodic, a non-empty box, and the box (and any separate
  trigger) inside the room's frame. The frame rule doesn't list inverters, so it's checked here.
- An inverter is never a kill volume, so door clearance ignores it.
- `ValidateBand`: an inverter in a level numbered 1–10 is an error naming the level.
- **Limits:** an inverter can be a chain target but not a chain source. `TrapLayoutValidator`'s list of trap kinds doesn't
  include it; that file was outside PAX-085's allowed list. `ValidateInverter`, which also does the inverter's frame
  containment, isn't part of `Validate()`, so a shipped level with an inverter needs its tests to call it until a
  follow-up wires it into `LevelLayoutValidator.cs`.
  **Superseded in part (PAX-089 B):** an inverter can now be a chain source; `TrapLayoutValidator.IsTrap` lists it
  (`TrapLayoutValidator.cs:39-40`). Noted 2026-09-28 (PAX-060 half B ruling 4).

(7) **Routes (amends D-079 (3)).** A second extra element, `Cat.Inverted` (`R.CatInverted`), after `Cat.Gravity`. It is
visible while any inverter's cue is on, so its first visible change is the fire tick. Its render box for the camera
tell rule is the cat's collider. A betrayal that dies because of the inversion declares `revealedBy: Cat.Inverted`, with
a lead ≥ 6 as usual. Route moves stay screen-relative *input*: `Hold(Right)` pushes the stick right, and the cat goes
left while inverted. No `Inverted()` condition was needed: the solution waits with `For(150)`. Every pinned route
result was unchanged.

(8) **Trap Lab room 7** (origin 316, 20 wide).
- **Layout.** A corridor: hop `Spikes_Back` (1 wide), then the `Inverter` (an honest pillar 0.6 × 3, so no jump clears
  it) just before a 1.5 u spike pit, and the door beyond.
- **Solution.** Stop, wait out the 150 steps (`Release`, `For(150)`), then run and jump the pit. Timed window: the restart
  after the wait, 50 (d −24..+25). Starting up to 24 ticks early walks the cat left, still inverted, and it turns before
  `Spikes_Back`.
- **Betrayals.**
  - **Dies:** keep holding right, and the cat runs back into `Spikes_Back`. Fire t63, kill t97, lead 34.
  - **Recovers:** play it inverted. Hold left to go right, jump the pit while inverted, and reach the door (t130).
- The camera tell rule passes at 4:3, 16:9 and 20:9.

(9) **Limits:** it inverts the horizontal axis only. No jump or gravity inversion, no permanent (until-reset) inversion,
no toggles, placeholder art. KIT-8's `Climb` axis is not inverted (KIT-8's decision will state it). The Inverter element's own route signature
includes its cue children, where they're built; betrayals read `Cat.Inverted`, never that. Not device-tested (Phase H).

### D-088 · 2026-09-26 · Accepted

**Decision:** The geyser (PAX-086, KIT-7): a vent that erupts on a Periodic cycle and launches the cat far above its jump, for
levels 11+ only. Rulings: `Docs/0_TASKS/PAX-086.md` §11. **Amends D-056 (3)** for geysers only: a geyser launch into a
*hidden* hazard may be a betrayal (with a declared, measured route, (6)). Moving Solids keep D-056 (3) unchanged: their
launch is never a betrayal.

(1) **What it is.** `SoloRoomElementKind.Geyser` (appended), built as a `GeyserTrap` (a `RoomTrap`). The element's box is
the vent, flush in a Floor's top (direction Up) or a Ceiling's underside (Down). The vent is a trigger, so the host holds
the cat. `GeyserSettings`: `Direction`, `ColumnWidth` 1.0, `ColumnHeight` 1.5 (from the vent's face), `TellTicks` 25,
`EruptTicks` 40, `LaunchSpeed` 14 u/s (the defaults, `GeyserMath`). Always Periodic (D-055 (2)): `PeriodTicks` and
`PhaseTicks` are the ordinary timing settings. Placeholder art: a grey vent that turns orange through the tell and the
eruption, and a pale translucent `Column` child shown only while erupting.

(2) **The cycle, in room ticks** (`Parallax.Core.GeyserMath`). It fires every period from room tick `phase` (TrapTiming's
Periodic rule); the fire tick is the tell's first tick. Tell (a visible change of the vent, harmless, no push), then
erupt, then idle. The runtime reads the phase from room ticks since the latest fire, so the death hold (D-058) and the
pause (D-073) freeze it, and the room reset (D-041) restarts it from room start.

(3) **The push (Q1).** In the room step (traps -> hazards -> bounds -> door), after the tick's motor step and before
physics, in Play and in the route harness alike: while erupting, a LocalHuman cat whose collider overlaps the column (the
previous physics pose, like every trap) gets `CatMotor2D.ApplyLaunch(direction, LaunchSpeed)`. The next tick's motor step
adds gravity to it as usual; the push sets it back while the cat is still in the column. Not lethal. `Physics2D.gravity`
and every Physics2D setting are untouched.

(4) **The seam (Q2).** `ApplyLaunch` sets the velocity component along the direction to the speed (absolute, not added),
keeps the cross component (the player steers at the motor's acceleration and run speed), sets coyote to 0 and
`IsGrounded` false, and leaves the jump buffer alone. Frozen (the death hold): nothing. `JumpWindows` is unchanged.
- A jump pressed on the first push tick fires in that tick's motor step and is then overwritten by the launch (measured:
  the record shows 14 u/s, not the jump's 9.8).
- A press after the launch is no coyote jump; it waits in the buffer, and one made up to 6 ticks before landing jumps on
  the landing (measured on the Ledge in room 8).

(5) **No speed cap (Q4).** The motor's cap (`MaxFallSpeed` 20) is along gravity only, and the push is applied after the
motor step, so nothing clamps it. At 14 u/s the push moves 0.28 u a tick, under the cat's 0.56 collider height, so it
can't tunnel through a ceiling (`m_MaxTranslationSpeed` 100, continuous collision).

(6) **Validator** (`LevelLayoutValidator.Geyser.cs`, separately named, not in `Validate`, like `ValidateSpear`):
- `ValidateGeyser`: Periodic; tell ≥ 6 (D-057); erupt ≥ 1; tell + erupt < period. The vent is inside a Floor or Ceiling
  and flush with its top or underside, and the direction matches that face (Floor → Up, Ceiling → Down), else an error
  (R6). The room's gravity isn't checked: a Down geyser in a gravity-down room pushes the cat into the floor, harmlessly.
  The column **plus the cat's collider height** (from `CatMotorConfig`, 0.56) beyond it overlaps no solid (fixed
  geometry, collapsing floors, falling blocks, Solid moving traps), or the cat would be pinned into it (R5). A bonk in
  the ballistic rise beyond that is allowed.
- `ValidateGeyserEnvelope` (R2): the launch envelope, counted from the first push to the apex: the column's width plus one
  run tick (0.12 u) each side per tick of flight, from the vent's face out to the pushes × 0.28, the discrete rise and the
  cat's height. (1) It stays inside the room's frame (no launch out through the top or sides; D-058). (2) A *disguised*
  hazard in it (hidden spikes, a disguised arrow's lane, a chained trap's kill volume) needs a declared Dies betrayal with
  it as the killer, so its lead is measured (≥ 6). Honest, visible hazards are allowed without one (D-056 (4)). Past the
  apex the cat falls like after any jump; the bounds kill and the route replays cover that.
- `ValidateBand`: a geyser in a level numbered 1–10 is an error naming the level.
- A geyser is never a kill volume, so door clearance ignores it.
- Timing: a route step that must enter the column during an eruption is a timed step (window ≥ 12, ≥ 8 inside a
  precision section, D-083). Room 8's solution stands on the vent before the eruption, so its timed step is the steer.

(7) **Measured flight (Q5), defaults, D-082's cat** (gravity 30, 50 Hz). The cat is pushed on 6 ticks (its collider
starts in the 1.5 column at 0, 0.28 … 1.40), leaving 1.68 u up. Then 23 rising ticks, the sum of (14 − 0.6 k) × 0.02:
**3.128 u** (the continuous v²/2g is 3.27). **Apex 4.808 u** above where the cat stood, measured in the harness equal to
the prediction; tolerance one push tick (0.28 u), since the overshoot past the column's top depends on where the cat
starts. From the first push to the apex, 29 ticks: the envelope reaches 3.48 u each side. The ticket's 3.27/4.8 figures
are superseded by these.

(8) **Trap Lab room 8** (origin 349, 20 wide).
- **Layout.** The exit `Ledge` (top 3.5, over the start, out of any jump's reach) with the door on it. The `Geyser`, a
  vent in the floor at x 9, period 100 (2 s), phase 80: tell from room tick 80, erupt 105–144. Right of the vent, the
  side the solution never goes: a `LowCeiling` (underside 5.2) with hidden `Ceiling_Spikes` under it, revealed by a
  full-storey cut (x 9.6–10.1, floor to ceiling underside), so `ValidateTriggerCoverage` passes with no exemption or
  learned bypass (R4). A cat walking there on the floor sees them pop out, harmlessly.
- **Solution.** Walk onto the vent, wait, ride the eruption, and 10 ticks into the flight steer left onto the Ledge and
  on to the door. Timed window: the steer, 34 (d −9..+24). Earlier, the cat meets the Ledge's face below its top.
- **Betrayals.**
  - **Dies:** a launch steered right rises into `Ceiling_Spikes`. Revealed t119, kill t127, lead 8.
  - **Recovers:** step on the vent during the tell and walk off: the cat is left under the Ledge. It walks back and
    rides the next eruption to the door (the Geyser's tell visible t81, complete t264).
- The camera tell rule passes at 4:3, 16:9 and 20:9 (lead 8 at each).

(9) **Limits:** Periodic only (no constant or trigger/chain geysers), up and down only (no sideways), placeholder art, no
particles or sound. A geyser can't be a chain source: `TrapLayoutValidator`'s list of trap kinds doesn't include it
(that file was outside PAX-086's allowed list), which also keeps room 8's spikes on their own trigger (R4). A room that
stops being live mid-eruption (the door) leaves its column drawn, since nothing steps a room that isn't live;
presentation only, and the next room is 13 u away. `ValidateGeyser` and `ValidateGeyserEnvelope` aren't part of
`Validate()`, so a shipped level with a geyser needs its tests to call them until a follow-up wires them in. Not
device-tested (Phase H).

### D-089 · 2026-09-26 · Accepted

**Decision:** Climbable vines (PAX-087, KIT-8): the input's first vertical axis, a climbing state in the motor, and snap
vines, for levels 11+ only. Rulings: `Docs/0_TASKS/PAX-087.md` §11. Not the frozen co-op vine (`VineInteractable`,
`VineManifestation`, D-025): the new classes are `ClimbVine`, `CatClimber`, `ClimbState`.

(1) **The Climb axis.** `CatCommand.Climb`, −1..1, **screen-up positive** (D-049), never gravity-projected and never
inverted (D-087 inverts Move only). Default 0, so every existing path is unchanged. `CatCommand` now has two axes, and
D-081 (4)'s gap (routes can't express a stick magnitude below 1) covers Climb too.
- **Touch:** `VirtualStick.ToClimb` on `Evaluate`'s already dead-zoned output: `Climb = sign(y)·(|y| − 0.35)/0.65`, 0
  inside `climbDeadZone` (0.35, a serialized field on `TouchStickCatInput`, above x's 0.15). The camera never rotates
  (D-020), so the stick's y is screen up. Measured by the angle sweep through `Evaluate`: at full push, the smallest angle
  that reaches the 0.5 grab threshold is **42.5°** above horizontal (asin 0.675); every push within 20° reads Climb 0.
  Device tuning is Phase H.
- **Keyboard (R1):** **Space is the only jump key.** W/Up climb up, S/Down climb down; A/D and Left/Right move.
  `KeyboardCatInput.Axis(negative, positive)` is the pure key mapping (both held: 0). The keyboard is Editor/dev input.
- **Router:** Climb merges like Move, independently: the largest |Climb| of all sources wins.
- **Seated:** `LocalHumanDriver` zeroes Climb while the cat is seated (`SeatCommandFilter` is frozen co-op code).
- **Echo** records poses, not commands, and ignores Climb.

(2) **Vines.** `SoloRoomElementKind.Vine` (appended). Position is the vine's centre, Size (0.6, height): the grab box, a
trigger `BoxCollider2D` on a `ClimbVine` (a `RoomTrap`). The grab test is bounds math (the vine's box against the cat's
collider), not a physics query. `LocalHumanDriver.Activate` finds the reality's `ClimbVine`s (inactive included) and
hands them to the motor; only the LocalHuman cat climbs, and `Deactivate` ends any climb.
- **Rendering (R3):** the vine sprite (`A_OBJ_Vine.png`, 236 × 433) stacked in Simple child renderers `Segment_i`, each
  scaled to the 0.6 width (≈ 1.10 u a segment), the top one squashed so the vine ends exactly at its top. Not Tiled: the
  sprite's importer mesh is Tight, and Tiled needs Full Rect (it warns otherwise); the `.meta` is the art ticket's.

(3) **The rules** (`ClimbState`, pure; `CatClimber`, owned by `CatMotor2D`, not a component, so the cat prefab is unchanged).
- **Grab (R5 (1)):** the cat's collider overlaps a grabbable vine and, grounded, Climb ≥ +0.5 (up only: pushing down at a
  vine's foot does nothing), or, airborne, |Climb| ≥ 0.5. No automatic grab. x snaps to the vine's centre on the grab tick.
- **Climbing:** no gravity; velocity (0, Climb × `ClimbSpeed`), `ClimbSpeed` 4 u/s = 0.08 u a tick (a shallow push climbs
  slower). Move alone does nothing. **Top (R5 (2), amended by the developer after the first play, 2026-09-26):** the
  collider's *top* stops at the vine's top, so the cat never climbs off the vine; a vine that reaches about the cat's
  height (0.56) above a ledge lets a cat at its top leap straight onto it. **Bottom:** climbing down releases once the collider's centre is below the
  vine's bottom, or when the cat stands on ground while pushing down.
- **Leap:** a jump press while climbing (or a buffered jump on the grab tick) sets the velocity to the normal jump launch
  (`JumpMath`, 1.6 u apex) against gravity plus `sign(Move) × MaxSpeed` along the cat's right (straight up with Move 0).
  Move is the motor's, after the inverter, so an inverted cat leaps the other way. Coyote and buffer are 0 after it.
- **Release causes:** the leap; climbing below the bottom; a gravity-side change (the step it's seen, the cat falls
  under the new gravity); `ApplyLaunch` (a geyser); a snap; and `ResetMotion` (death, room reset, respawn). The death hold
  freezes a climb as it is (`IsFrozen`), and the pause runs no motor step.
- **Regrab lock:** after any release but a reset, the same vine can't be grabbed for the next `RegrabLockTicks` (10)
  motor steps, whether the release happened in the motor step or after it (a launch in the room step). Other vines can.
- **Where it runs:** `CatMotor2D.Step`, after the ground test, as an early return. With no vines, or Climb 0 and no climb
  in progress, it returns having touched nothing, so the rest of the step is unchanged. Proven: every
  `RouteHarnessFidelityTests` pin and every shipped and Trap Lab route result are identical.
- **Tunables** (`CatMotorConfig`, field initializers; the `.asset` isn't edited): `ClimbSpeed` 4, `RegrabLockTicks` 10,
  `GrabThreshold` 0.5.
- **Presentation:** `CatAnimState.Climb` (appended), entered while climbing; leaving it, the ordinary rules pick Rise
  (a leap), Fall (a release) or Idle/Walk. Until the art ticket's climb sheet, the presenter turns the Visual ±90° around
  the collider's centre, placing the current sprite's own centre on it (head screen-up for either facing and gravity,
  `CatVisualPresenter.ClimbPose`), and shows the Walk
  frames while the cat moves on the vine, the Idle frame while it hangs still. No Animator.

(4) **Snap vines.** A vine with configured trap settings snaps: Overlap on its `Trigger` child (the element's secondary
box) or on its own box without one, or Chain; Once; `DelayTicks`. When it fires, every segment renderer goes off (its
visible reveal, D-080 (1)), it can't be grabbed, and it releases the LocalHuman cat in that room step; the cat falls from
the next motor step. The room reset restores it. A snap vine can't be a chain source (`TrapLayoutValidator`'s trap list,
outside the allowed list, as for geysers). **Superseded (PAX-089 B):** a snap vine (one with snap settings) can now be a
chain source; `TrapLayoutValidator.IsTrap` lists vines, and a plain vine is rejected as a source because it never fires
(`TrapLayoutValidator.cs:22-23, 39-40`). Noted 2026-09-28 (PAX-060 half B ruling 4).

(5) **Routes (D-079 (1)).** `Hold(Up)`, `Hold(Down)` (the `Vertical` enum, a `Hold` overload), `ReleaseClimb()`,
conditions `Climbing()` and `YAtLeast(y)`; `Release()` still clears Move only. `TickRecord` gains `Climb` and
`IsClimbing` (0/false on every existing route). No existing route file changed. A leap off a vine is a timed route step,
not a `RequiredJump`.

(6) **Validator** (`LevelLayoutValidator.Vine.cs`, separately named, not in `Validate`, like `ValidateGeyser`):
- `ValidateVine`: width 0.6; height ≥ 1.5; inside the frame; overlaps no solid; the cat snapped to its centre, over the
  vine's height (standing at its bottom to its collider's top at its top), overlaps no solid and stays in the frame (R6); its bottom is reachable: some
  solid top beside it (the cat's centre within 0.8 u) is below its top and within the cat's height plus a jump (1.6 u)
  below its bottom (R5); a snap vine is Once, and its Overlap trigger overlaps the vine.
- `ValidateVineRoutes`: a snap vine needs a declared betrayal revealed by it.
- `ValidateBand`: a vine in a level numbered 1–10 is an error naming the level. A vine is never a kill volume.

(7) **Trap Lab room 9** (origin 382, 20 wide).
- **Layout.** `Floor_Left` (x 0–9), a `Pit` (x 9–11, `PitHazard` on `PitBottom`), and the `Cliff` (x 11–20, top 4.5, out
  of any jump's reach) with the door on it. `Vine_Real`, x 7.9, y 0–5.1, standing on Floor_Left. `Vine_Obvious`, x 9.6,
  y 0–5.1, over the pit next to the Cliff: a snap vine, `Trigger` y 2–2.5, delay 12. At a vine's top the cat's feet are
  at 4.54, just above the Cliff.
- **Solution.** Walk right pushing up: grabbed from the ground, 57 ticks to the top, leap right onto the Cliff (lands at
  x 11.98, 33 ticks after the leap). Timed window: the leap, **48** (d −22..+25, open high: the cat can wait at the top).
- **Betrayals.**
  - **Dies:** jump at the obvious vine and climb it: it snaps (t88) and the cat falls onto the `PitHazard` (t126),
    **lead 38**.
  - **Recovers:** touch the snap, leap back to Floor_Left, see it go (t88), take the real vine (complete t281).
- The camera tell rule passes at 4:3, 16:9 and 20:9 (lead 38 at each).

(8) **Limits:** no ropes or swinging, no wall climbing without a vine, placeholder climb pose, levels 11+ only, dead zone
and speed not device-tuned (Phase H). `ValidateVine` isn't part of `Validate()`, so a shipped level with vines needs its
tests to call it until a follow-up wires it in. The seated-cat zeroing is untested (seating needs the frozen Control
Station). Not device-tested.

(9) **Accepted at acceptance (PAX-087, 2026-09-26):** two changes outside §7's list: the route condition `YAtLeast(y)`
((5)), and `RouteTestApi.Call` choosing among overloads by argument type (`R.Hold` now has an `int` and a `Vertical`
overload). Both are test and route vocabulary; no runtime behaviour changed.

### D-090 · 2026-09-26 · Accepted

**Decision:** The storm cloud (PAX-088, KIT-9), the kit's first hazard whose position depends on the cat, for levels 11+
only. Rulings: `Docs/0_TASKS/PAX-088.md` §11. Deterministic (D-040): a pure function of room ticks since its wake and of
the cat's recorded body positions; no `Time.time`, no randomness, no physics query in the follow or the strike.

(1) **Element.** `SoloRoomElementKind.StormCloud` (appended). Position/Size is the cloud's authored pose and box; the
secondary box is its wake trigger. `StormCloudSettings`: `MinX`/`MaxX` (room-local range of its centre), `FollowSpeed`
0.08 u a tick, `FirstStrikeDelay` 50, `StrikePeriod` 100, `TellTicks` 25, `StrikeTicks` 6, `StrikeWidth` 0.8. Trigger:
Overlap or Chain, **Once**. At most one per room. While dormant it is a plain grey cloud (disguise, D-056 (4)).

(2) **Follow (§11 Q1).** `StormCloudTrap` (a `RoomTrap`) reads the LocalHuman cat's `Rigidbody2D.position` plus its
collider offset, in its room step, before the tick's physics: the previous tick's post-physics pose, identical in Play
and the route harness. Never the Transform, which Play interpolates. On a vine-grab tick it reads the snapped x (the
grab moves the body in the motor step, before the room step). Each follow tick: `x += clamp(catX − x, ±FollowSpeed)`,
clamped to the range (`StormCloudMath.Follow`). Order among traps doesn't matter: no trap moves the cat's position in the
room step (§11 Q2); `RoomManager` is unchanged.

(3) **Cycle (ruling B).** Wake W (the trap's fire tick): dormant through W, following from W+1, the first charge at
W + 50, the first strike at W + 75, then a charge every `StrikePeriod` from the first. **Charge:** the cloud stops,
darkens, and draws a faint target line down its locked x; harmless. **Strike:** a lethal column `StrikeWidth` wide from
the cloud's bottom to the strike's bottom. Then it follows again. `StormCloudMath.PhaseSince` is the whole rule.

(4) **What stops a strike (ruling A, §11 Q3).** The highest static top at or below the cloud's bottom that overlaps any
part of the column's width. Static = `IsFixedSolid` (Floor, Wall, PitBottom, Ceiling); fake platforms, collapsing
floors, moving Solids, falling blocks and stuck spears don't block lightning. The builder bakes the room's static tops
onto the trap as (xMin, xMax, top) segments (`LevelLayoutValidator.StrikeProfile`); no runtime raycast. The drawn and the
lethal column end at the same top.

(5) **Kill test (ruling C).** `StormCloudMath.Hits(column, catBox)`, a strict overlap (touching is safe), no `Physics2D`.
`catBox` is the body position plus the collider offset **rotated by the body's rotation** (the cat turns 180° with
gravity up, D-052), with the collider's size; never `Collider2D.bounds`. A hit kills through `RoomDeath`,
`DeathCause.Hazard`. The route harness names the cloud as the killer through the same function (`StrikeHits`). A cat
wholly above the cloud's bottom can't be hit, so a gravity-up cat above it is safe; clouds are allowed in rooms with
gravity flips (§11 Q4).

(6) **Reset, hold, pause.** The phase is room ticks since the wake, so the death hold and the pause freeze it; the room
reset returns it to its authored pose, dormant.

(7) **Validator** (`LevelLayoutValidator.StormCloud.cs`, `ValidateStormCloud`, separately named, not in `Validate`, like
`ValidateGeyser`): at most one per room; configured; Once; strike ≥ 1 tick; first strike delay ≥ 1; `StrikePeriod ≥
TellTicks + StrikeTicks + 1`; the authored x inside the range; the swept cloud (range × its box) inside the frame and
overlapping no static element; a static top under every column pose; the **dodge rule** (§2.4): `TellTicks ≥
ceil(from-rest ticks to clear half the strike width plus half the collider width) + 12`, or + the precision slack (8)
when the strikes' envelope is wholly in a precision section (defaults: 0.9 u, 10 ticks, so ≥ 22); **door clearance**
(D-060): the strikes' envelope over the whole range, grown by one run tick (0.12 u), doesn't touch the door (or its
retreat sweep). The dodge rule replaces `ValidatePeriodicSlack` for clouds (their trigger is Once, so that rule never
sees them). `KillVolumes` and `TriggerCoverage` are unchanged. `ValidateBand`: a cloud in a level numbered 1–10 is an
error naming the level.

(8) **Routes.** A betrayal that dies to a strike is revealed by the cloud itself; its lead runs from the cloud's first
visible change (its first follow step when it moves, else its charge), so it is at least `TellTicks` (§11 Q5). Adding the cloud to the
harness's killer naming left every pinned route result identical.

(9) **Trap Lab room 10** (origin 415, 28 wide). The cloud 2 × 0.8 at (4, 5.5), range x 1.5–24, trigger x 5.5–6.0; the
`Rise` (x 11–17, top 0.8); `Overhang` (Ceiling, x 19.5–22.5, y 1.4–1.9); `Fake_Overhang` (FakePlatform, x 6.5–8.5, the
same height); the door at x 26.
- **Solution:** run right, jump onto the Rise, run on to the door without stopping; the cloud (0.08 u a tick) never
  catches a running cat (0.12), so every strike lands behind it. Timed window: the jump, **51** (d −25..+25, open both).
- **Betrayals (Dies, killer and reveal `StormCloud`):** stop once the cloud wakes; wait under `Fake_Overhang`. Both: wake
  t28, first visible change t29, killed by the first strike at t103 (wake + 75), **lead 74**.
- **Harness:** a cat standing still dies on the first strike; one that starts running on the charge's first tick
  (wake + 50) clears the column and survives; one waiting under the real Overhang lives through several strikes with the
  cloud settled over it. The camera tell rule passes at 4:3, 16:9 and 20:9.

(10) **Limits:** vertical strikes only; no vertical cloud motion; one cloud per room; static geometry blocks lightning,
moving Solids and stuck spears don't; placeholder art; no sound or VFX. `ValidateStormCloud` isn't part of `Validate()`,
so a shipped level with a cloud needs its tests to call it. The room 10 solution never needs the Overhang; the cover is
proven by a harness probe. Not device-tested.

(11) **Clarification (PAX-060 L018, 2026-09-28).** The strike locks at the charge's start: the column is fixed at the
cloud's x then, and strikes `TellTicks` (25) later. A cat that moves during the charge leaves the column (a running cat
clears the 0.9 u it needs in about 8 ticks), and a cat climbing a vine can't be caught either in practice: the cloud
follows at 0.08 u a tick, so it arrives over the vine only after the cat has climbed for as long as it took to get
there, and a vine's feet clear the cloud's bottom long before the next lock. A level can't time a "climb between
strikes" lesson on the cloud; only a cat that stays in the locked column is struck (L018's T8 became a wait instead).

### D-091 · 2026-09-26 · Accepted (Architect, 2026-09-27; PAX-090 as built, rulings §11–§12)

**Decision:** Checkpoint sections. A level stays one scene, one room, one continuous space and one camera frame (D-063,
D-071), and may be divided into **sections**; a death replays only the current one. Rulings: `Docs/0_TASKS/PAX-090.md`
§11. Amends D-041: "every trap returns to its declared initial value" becomes "every element returns to its state when
the cat reached the current checkpoint". A room with no sections behaves exactly as before.

(1) **Layout.** `SoloRoomDefinition.CheckpointSections` (`CheckpointSection`: `Name`, `Checkpoint` (room-local paw
point), `Gate` (a box), `Owns`, `GravityUp`). Section 0 is the start: no gate, its checkpoint is the room's Checkpoint
element. Allowed in every level, used from level 11 (§11 Q6).

(2) **Runtime.** `SoloRoomBuilder.BuildRoom` bakes a `RoomSectionEntry[]` on `RoomManager` (world gate box, spawn body
position and gravity, marker) for sectioned rooms only, plus a `<Name>_Marker` (the checkpoint art, dim) and an empty
`<Name>_Gate` in the room. Each live tick, after every kill check and before the door, the LocalHuman cat's collider
overlapping the next gate (in order only) makes that section current, lights its marker, and snapshots every trap of the
room (`IRoomSnapshot.Capture`, a `TrapSnapshot` by value) with `RoomLifeTick`. Gates are not `CheckpointMarker`s: the
room's checkpoint id (= room id) never changes. Everything stays live in every section.

(3) **Rewind.** After the death hold (unchanged, D-058), with a gate passed: the cat respawns at rest at the section's
checkpoint through `CatRespawn.RespawnAt`; the ordinary room reset runs; then `RoomLifeTick` goes back to the gate tick
and every snapshotted trap is restored (`Restore(snapshot, gateTick)`); the next live tick is gate tick + 1 (§11 Q5).
`RoomLifeTick` is set inside the reset, so the first motor step after it (the inverter's `RoomLifeTick + 1`) reads the
restored clock. With no gate passed, today's reset runs unchanged, including its known `HoldTicks = 0` quirk (a falling
block's move queued on the kill tick overrides its reset pose; the rewind path sets the pose and the move target both).
The cat's body is then taken out of the simulation and put back, which drops the contacts Box2D kept from where it died
(their warm-start impulses left a 1e-14 u/s residue that differed with the place of death), so a rewind replays
identically however the cat died. `CheckpointManager` and `CatRespawn` are unchanged.

(4) **The snapshot (§11 Q1/Q2).** Every trap: `State` and the whole `TrapTiming` (pending, seen-source, armed-since, fire
tick, armed, just-rearmed). Extras by value: a geyser's phase, a cloud's `OffsetX` and phase (its x follows the cat, so
it isn't a function of ticks), a retreated door's pose (a rearmed retreat stays where it stopped), a `rearmOnExit` flip's
`TrapCountdown`, an inverter's fire tick, a vine's snapped flag. Everything else is redrawn from the timing at the gate
tick: arrow and spear poses, the spear's shaft, collapses, hidden spikes; kinematic falling blocks and moving traps are
set to their formula pose at the gate tick and given it as their `MovePosition` target. The cat's own state (climb,
motion) is cleared by its respawn. A sectioned room must not own anchors: no layout element can, and `RoomDeath` logs an
error if a scene pairs them (the rewind resets anchors to their initial values).

(5) **Deaths.** Counted per section in `SectionProgress` and logged when the room completes as `PARALLAX_SECTIONS
room=<id> <name>=<deaths> … total=<n>`; `PARALLAX_STATS`, the level total and the level-complete screen are unchanged
(D-061).

(6) **Rules.** Layout (`LevelLayoutValidator.ValidateSections`, part of `ValidateKit`): at least two sections, unique
names that aren't element names; section 0 as in (1); every later section has a gate and a checkpoint inside the room,
standable on a fixed surface (checked against the room's x range and vertical bounds, not the level camera's
frame), outside its gate and on the gate's far side from the previous checkpoint; every trap
element owned by exactly one section. Routes (`RouteValidator.ValidateSections`, run by `RouteValidator.Run` for a
sectioned room): the solution passes every gate in order; each section's time on the solution (gate to next gate, or
the door) ≤ **1000 ticks** (20 s; the design target is 750, over it needs a sentence in the sketch); standing still at
each checkpoint after a rewind survives ≥ **50 ticks**; each gate's rewind is exact (`CheckRewind`: two replays dying at
the gate and 10 ticks later give the same 100 ticks after their rewinds, and every element looks at the rewind as it did
at the gate).

(7) **Routes.** `Route.FromSection(solution, checkpointSection, [rewindAfterTicks,] name, then…)`: the solution's steps
(untimed) until the gate is passed, then `R.Rewind()` (the harness kills the cat through `RoomDeath.Kill` and steps the
death hold), then `then`. One forced rewind per rig (`CatRespawn`'s same-frame guard). A `FromSection` betrayal's reveal
and arrow first-lethal ticks are measured after the rewind. Gate markers are harness elements (`<Name>_Marker`), whose
first visible change is the gate tick.

(8) **Trap Lab room 11** (origin 456, 66 wide): Act1 (Collapse_1, Spear_1), Act2 (gate x 23.5, checkpoint under the
Overhang: the awake StormCloud, the Geyser under Vent_Roof), Act3 (gate x 41.5: Spikes_Back, the Inverter, Vine_Real to
the Cliff). Solution per section 222 / 234 / 388 ticks; standing still at each checkpoint survives 50; both rewinds
exact; four betrayals (leads 21, 8, 79, 34), Act2's and Act3's from their gates.

(9) **Limits.** A checkpoint must stand on fixed geometry (not a stuck spear); per-section deaths aren't shown in the UI
or saved; no mid-level save between sessions; the section's time is measured on the solution from the gate, not from a
respawn. Not device-tested.

(10) **Fix (PAX-060 §13 R5, 2026-09-27).** `SoloRoomBuilder.BuildCheckpointSections` returns early when the room has no
sections or no `RoomManager` is given. Before, it wrote an empty section list through `new SerializedObject(rooms)` for
every room, which threw on a bare room build (`ThinPlatformTests`' Trap Lab build passes none). Rooms without
sections wrote nothing before either, so every built scene and result is unchanged.

### D-092 · 2026-09-26 · Accepted (Architect, 2026-09-27; PAX-090 item B as built)

**Decision:** Amends D-089 / PAX-087 R5. A **grounded** cat grabs a vine by pushing **away from its ground**: Climb ≥
+threshold with gravity down (unchanged), Climb ≤ −threshold with gravity up (a cat on the ceiling pushes screen-down).
A grounded, climbing cat lets go by pushing **into** its ground (Climb < 0 with gravity down, > 0 with gravity up), so the
grab tick itself never releases. Airborne grabs (|Climb| ≥ threshold), climbing speed and direction, the top stop and the
bottom release (collider centre below the vine's bottom while climbing screen-down) stay screen-relative (D-049). Every
gravity-down expression is identical, so no route result moved. `ClimbState.WantsGrab` and `ReleasesAtBottom` take
`gravityUp` (default false). Tests: `ClimbGravityUpTests`.

### D-093 · 2026-09-27 · Proposed (PAX-060 half A as built; half B completes it)

**Decision:** Band-2 content rules for levels 11–20, checked by `LevelLayoutValidator.Band2.cs` (`IsBand2`: levels 11–20).
Rulings: `Docs/0_TASKS/PAX-060.md` §12. Band 1's rules (D-085) are unchanged.

(1) **The rules as built** (`ValidateBand2Content`, `ValidateBand2Duration`, `ValidateBand2Replays`):
- **Lethal:** ≥ 8 distinct killers over Dies routes. §12 asks every band-2 level for 9 as a design margin; the check stays 8.
- **In sequence:** ≥ 6 (`SequentialChain`, as band 1). **Dead ends:** ≥ 2 (Dies routes named "Dead end…", or Recovers).
- **Door far from start:** band 1's D-085 rule (half the room's width or height), rechecked here because band 1's check
  stops at level 10.
- **Sections:** 2–3 (D-091's ≤ 1000 ticks each is `ValidateSections`'). **Solution:** 1100–2500 ticks
  (§13 R4 lowered the floor from 1500: length comes from content, not distance or waits). A reported (not checked)
  review item (§14 R2): no stretch of the solution over 250 ticks without a decision, a decision being a betrayal
  branching off the solution or a timed step with a measured window (one exception: L013's sync wait, the counting).
- **Precision sections:** only in L012 (one section, at most one `RequiredJump` inside it), L015 and L019.
- **Answers (P7):** every betrayal's name starts with a tag, `"T4 [BAIT]: …"`, from J, NJ, W, NW, SS, B, OL, LW, BAIT; a
  missing tag is an error. Counted per trap (the label before the tag), dead ends aside: a code answers ≤ 3 traps, BAIT ≤ 1.
- **The level's element in ≥ 3 betrayals** (11 spear, 12 inverter, 13 geyser, 14 vine, 15 storm cloud): by the killer
  or `RevealedBy` being one (`Cat.Inverted` counts for the inverter), or by the betrayal's own replay (after the shared
  steps, or after its rewind) showing it act on the cat: standing on a spear's shaft, the inversion changing, a push
  (|Vy| ≥ 13 inside a vent's bounds), climbing.
- **Everything else** as band 1 (§2.2): trigger and surface coverage, the camera tell rule, falling-block landing,
  trap-floor headroom, trigger near trap, `ValidateKit`, `ValidateSections`.

(2) **The chaos moment** (§12 finding 4). An onset is an element's signature change after a tick with no change (or its
first change); the cat's extras, gate markers and the door don't count. The solution's best 60-tick window needs ≥ 5
distinct elements whose onset is in the 16:9 view at its tick: the camera tell rule's worst case over its 24 camera
cases (`InView16x9`). A periodic level has onsets every cycle, so its best window can be a beat the solution doesn't ride;
PAX-060's report names both.

(3) **Replay cap** (§12 finding 1): `1000 × max(1, sections) + 500` ticks (1500 without sections, as before).

(4) **Half A as built** (solution ticks; sections; chaos):
- **L011 Scaffold** (spear, BUILD): a Z climb, 32 × 27, start (20, 0), door top left. 1614 ticks; 723 / 622 / 269; the
  volley t940–t999, V1–V5 all in view. 12 killers, 10 in sequence (after §14's padding pass).
- **L012 Mirror, Mirror** (inverter, UNLEARN): five stacked halls walked west, east, west, east, west, about 32 × 26
  (ruled 32 × 16; grown for length). 1323 ticks; 265 / 579 / 479 (after §14: no inversion is waited out); t283–t342, 5 of 5 in view. 12
  killers, 10 in sequence.
- **L013 Old Faithful** (geyser, COUNT): mirrored as ruled, 32 × 27.5, five storeys and a three-geyser stack, rhythms of
  100, 150 and 300 ticks. 1613 ticks; 653 / 682 / 278. The validator's best window is t911–t970 (10 of 14 in view, an earlier
  beat of the same rhythms); the sync the solution rides is t1216–t1275, 10 of 14 in view (the stack, its pads and G_D
  among them). 10 killers, 8 in sequence.
- **L014, L015:** stopped (outcome (c)) and redrawn under the 1100 floor (§13 R4).
- **L014 Root System** (vines, GO DOWN; built after PAX-093): 32 × 44, start (22, 40), door bottom right; Top → Lip_1 →
  V1/V2 (Spear_V2 across V2) → Trunk (bait block, V_Up dead end, V3 and Spear_4) → Low walk (Spear_L over the Trough) →
  the Knot (Shrink, V4 holds, V5 snaps) → the Canopy (C1–C6, 6 vines). 1133 ticks; Drop 278 / Trunk 606 / Canopy 249;
  chaos t950–t1009, 5 of 5 in view (Block_7, C1, C2, Spikes_8, C3). 13 killers, 8 in sequence (Pit_1 > Spikes_V1 >
  Spear_V2 > Block_T > Spear_4 > Spear_L > Pit_K > Block_7); answers B 1, BAIT 1, J 1, NJ 2, NW 3, OL 1, SS 1, W 1.
  Changes from the sketch, ruled 2026-09-28: T6 (lip thorns) removed (the lip is reached from V4 before C1's column, so
  no trigger can cover it); T7 reworked (a falling block pushes a climbing cat down its vine instead of crushing it: C2
  snaps onto the Perch and Block_7 lands there); a 6th canopy vine, the exit at x 26; T2b [BAIT] and T4b [NJ] added as
  decisions for the 1100 floor. Pit_1, Pit_1E and Pit_3 are honest pits. T1's reveal is Lip_1 (the Top's last 1 u, a
  crumble, not KIT-10) giving way at the cat's feet; its escape (hold Down and Right, catch V2) is declared and checked
  by D-097 (last escape t105, +32; `L014EscapeTests`). T3 (Pit_3) is no longer a declared betrayal; T4 (Spear_4) takes
  its place in sequence. The Trunk's west edge is at x 14.5 and V2 at x 14.6, so the lip's straight drop can't land on
  the Trunk; that cost 18 ticks, and T2c [W] (Spear_V2: launched from x 24 at y 32, trigger above the lane, 22-tick tell,
  stuck at the west wall) brought the level back over 1100. Its lane crosses V1 and the Pit_1 column only while no cat
  is there (the solution's cat is on V2 above the lane; T1 and T2 die before it gets there); coverage counts the stuck
  shaft as a floor (PAX-091) and finds nothing new. The Knot's step onto V4 (the resume after stopping on Shrink) has a
  51-tick window (d −25..+25, open both ways): not a precision move; V4 is caught by an ordinary airborne grab
  (|Climb| ≥ 0.5 on overlap, D-089 (3), D-092).
- **L015 Hunted** (storm cloud, KEEP MOVING; built after PAX-093): 64 × 14, start (2, 0), door bottom right; Open (the
  gap, Arrow_3, Collapse_2 under Overhang_B) → Shelter (Fake_4, Mover_M ridden under the storm, the planted spear under
  Roof_S and the Spear_B bridge) → Run (the cave-in, the dance, P1 under Roof_9, the finale). 1284 ticks; Open 281 /
  Shelter 419 / Run 584; chaos t699–t758, 5 in view of 6 (Cloud, Block_1, Block_2, Block_3, D3). 10 killers, 8 in sequence
  (Cloud > Arrow_3 > Pit_2 > Pit_S > Pit_9 > Arrow_9 > Spear_10 > Pit_M); answers B 2, J 1, LW 1, NJ 3, NW 3, OL 1, SS 1,
  W 1. Changes from the sketch, ruled 2026-09-28:
  - **The dance** (Option 1 with a gate): Block_3, the cave-in's last block, also knocks out D3, the last of three thin
    platforms over Pit_9 (a crumble, D-055 (2) Rearm snap, `CollapsingFloorTrap.OnTimingRearmed`; not KIT-10), for 257
    ticks. With no cover, the cloud locks over D1 at t874 (hop on to D2) and over D2 at t974 (D3 gone: hop back to D1);
    D3 is back at t1004. Windows 40, 24, 32 in the precision section `Dance` (x 46.2–53.8; reach and slack within D-083).
    Bait gaps `D2_P1` (P1 is 1.7 above D2, over the jump's 1.6) and `D1_P1` prove that with D3 gone P1 can't be reached;
    T13 [NJ] shows it (the cat reaches P1's face and falls). T11 [NW] (stay on D2 through the lock) and T12 [B] (dodge it
    forward) die; the cave-in is shorter (Block_1–3 within 3 u of the cut; Block_4 and Spikes_6 with T8 [J] removed).
    Strike width 0.8 (`StormCloudMath.DefaultStrikeWidth`): every dance hop ends ≥ 0.9 u from the lock.
  - **The finale** (the §12 "now stop"): Spear_10 replaces the sketched arrow. Floor_F1 (top 0.9) is the stopping ledge;
    Spear_10 (36-tick tell) runs along Floor_F2 below it at shin height and sticks in Floor_F1's face; a cat that runs on
    drops into its lane. Post_F and Curb_9 are gone, the finale is 1 u further east, P1 is 0.8 up (Roof_9 with it).
  - **Arrow_9** fires once, from a cut on P1, with a 50-tick tell (it was periodic, and its first shot was off screen);
    T9 stays a wait under Roof_9.
  - Also: Arrow_3's trigger is floor to ceiling from x 10.6 (it cuts the band for Collapse_2); Fake_4's underside is 2.35;
    Spear_B is 2.8 long; the bridge's timed step is the walk's start, not the jump.
  - Reveals (the 2026-09-28 rule): every reveal is on screen from its first change to the kill at every aspect; T9's
    (Arrow_9's tell, t1068) 15 ticks before its last escape (staying on P1, t1082), measured.

(4c) **Real cover only (the developer's play, 2026-09-29).** A strike drawn through a roof reads as a bug, not a trap:
the fake roofs under a cloud never gave way (they sat above a jump's reach), so the bolt simply passed through them. Every
roof under a cloud is now real. D-090 (4) is unchanged (fake platforms still don't stop a strike); no level puts one
where it would look like cover.
- **L015:** Fake_4 is now Roof_4, a fixed roof (x 19.5–21.5, y 2.3–2.8, like the other covers); T4 [LW] (shelter under
  Fake_4) is removed. Answers B 2, J 1, NJ 3, NW 3, OL 1, SS 1, W 1; the element in 5 betrayals. Nothing else moved:
  1284 ticks, the same sections, windows, chaos and 10 killers (8 in sequence).
- **L018:** Fake_1 is now Roof_1, a fixed roof (x 27.2–28.8, y 2.3–2.8); T1 [NW] (shelter under Fake_1) is removed.
  Answers B 2, J 3, NW 2, OL 2, SS 1, W 3; the element in 8 betrayals. The launch jump's window is 31 ticks (was 29;
  measured, cause not traced); nothing else moved: 1143 ticks, the same sections, chaos and
  12 killers (11 in sequence). Fake_A (Sky A, above the cloud) stays: it's a landing, not a roof.
- **L020:** the cloud (x 20–30, bottom 22.1) has no fake roof under it; unchanged. L014, L016, L017 and L019 have no cloud.

(5) **Limits found (half A):**
- The coverage search can't see geyser launches (§12 Q5): a hidden hazard in a storey that only a launch reaches is
  judged from the checkpoint's side. L013's `Spikes_D2` now takes its whole ledge's storey as its trigger (§13 R3), the
  same pattern as L012's orbs; PAX-091 teaches the search launches, vine climbs and stuck spears before half B, and
  half B uses no such workaround.
- **PAX-091 closed** the launch, vine-climb, stuck-spear, per-storey surface and closed-pit blind spots below (D-074 and
  D-080 notes). Still open: a drop past a **vertical wall** (fixed solids spanning the drop's full height between
  departure and landing; PAX-093's coverage item, closed by D-095 (4)) and **gravity-up** vine and launch edges (only
  Down vents launch a flipped cat; closed by PAX-095, D-098).
- **PAX-091 finding (L011):** the search saw a jump from the stuck `Spear_Door` onto `Ledge_Hi`'s west end, onto
  `Spikes_D2` before their trigger. The ledge is 2 u wide, so a landing from the west lands on the spikes the tick it
  enters any trigger over the ledge (replayed: lead 0). Fix (ruled 2026-09-27): the trigger holds the ledge's whole top
  strip (x 9–11), and `Stop_Hi`, a post on its west face (x 8.7–9.0, y 20.4–21.4, a 1.85 rise from the shaft), takes the
  west jump away (replayed: it never lands). D2's lead is 8 (was 7); every other pin is unchanged.
- Surface coverage's band isn't per storey (`BandLow`, `TryCeilingUnderside`) and takes its side from the first
  checkpoint. A chained collapsing floor in a stacked level passes only when its root's trigger holds the floor's top
  strip (L012's orbs).
- `ApproachSides` lets the cat drop off a surface's end through a closed pit below it. L012's `Spikes_D` sits inside
  `Orb_A`'s trigger box for that reason.
- A periodic hidden-spike strip shows on its fire tick; `RevealDelayTicks` delays only an overlap fire.
- **L014 finding:** grabs onto vines ignored solids in between; fixed by D-096.
- **Known limit: the coverage search ignores trap order** (ruled 2026-09-28, L018). It treats every side of a trigger it
  can reach as an approach, even one a cat reaches only after that trigger has fired. L018's `Collapse_V` (chained from
  Drop_3) was "reached from the west" through V_6 and Ground_W1, a side a cat reaches only after Drop_3 has set it off.
  L018 roots the chain in `Spikes_V`, hidden spikes on the pit's floor whose floor-to-ceiling cut holds the crumble's
  strip from both sides; Spikes_V stays as the pit's real killer. Not on PAX-095's list. `Lid_V` blocks a real skip
  (without it a cat jumps the open pit: 3.6 u edge to edge against a 4.08 u flat jump).
  L017's `Spikes_5e` are the same limit (ruled 2026-09-28): chained into the far end's rearrangement they were "before"
  their root's trigger, and a cut at the stair's top was "seen from" the checkpoint's side, since the upper storey is
  reached only across that cut. They fire on their own cut one step off the stair (x 26.3–26.5), so the strip east of
  it is the approach, and Block_6 hangs on them (a chained block within 3 u of its root's cut is exempt).
- **PAX-094 items (L018):** four betrayals have no escape once their reveal has happened, so an escape-backed reveal
  rule would need a declared escape or a change for each: T4 (Fake_A gives way; the cloud's charge at t255 is the one
  reveal with an escape, 15 ticks), T6a (SL_2 gives way under a cat standing wholly on it), T10 (Block_T, t730–t744) and
  T12a (the crumbles under a cat that stops). Each is on screen from its first change to the kill (D-083).

(4b) **Half B as built** (ruled 2026-09-28: built in the order L018, L019, L020, L017, PAX-095, L016, each measured unlisted
as its own number and listed with the others once L016 and L017 exist). A half-B level names several elements; the
level's element rule counts a betrayal that uses any of them. **Listed 2026-09-29:** L016–L020 registered in
`LevelLayouts`/`LevelRoutes`, added to `LevelListConfig` by New Level…, every level scene rebuilt, and their route results
pinned in `Band2RouteResultsTests`; L016 accepted pending the developer's play.
- **L018 Eye of the Storm** (storm cloud, geysers and vines, GET ABOVE): 32 × 26, start (30.5, 0), door top right on the
  Summit (23.2, 20); a U: west under the cloud (bottom 3.2), G_1's launch above it and west across Sky A, Drop_3 back down
  into the storm, V_6 up through the cloud's band, east along Sky B under G_D's downdraft, G_B's launch onto the Top, G_C's
  onto the Crown, V_9 to the Summit. 1143 ticks; Storm 271 / Sky 402 / Top 470; both rewinds exact. 12 killers, 11 in
  sequence; answers B 2, J 3, NW 3, OL 2, SS 1, W 3; the element in 9 betrayals. Chaos (the validator's best window)
  t321–t380, 11 in view of 12 (Sky A giving way under the cat, Drop_3, Collapse_V, the geysers' beat); the Top's collapse
  t831–t890, 8 in view of 10. Longest stretch without a decision 172 ticks. Every reveal is on screen from its first
  change to the kill at 4:3, 16:9 and 20:9 (Block_T hangs in Lintel_T, low enough to be in view as it starts). Moving floor: Drop_3, a drop-and-return
  (`SurfaceMotion.Slip`). Changes from the sketch: 32 × 26 kept, but Sky B, the Crown and the Summit replace the Nest (the
  sketch measured about 900 ticks as drawn: every beat before G_C's eruption waited on a geyser's clock); T4 dies to the
  cloud the fake drops it into (the concept's "straight into its charge"), not Pit_S; T8 is the wait for Collapse_V (a
  climb through the band can't be timed: the cloud locks where the cat is when it charges, so a moving or climbing cat
  always outruns the strike); Collapse_V's root is Spikes_V (hidden, on the pit's floor), whose floor-to-ceiling cut
  holds the crumble's strip from both sides (surface coverage; the search ignores trap order, (5)), and its cut takes in the drop's
  column, so the floor goes as the cat lands on Drop_3 (Collapse_V visible t361, the last stop before the pit ~t411);
  Lid_V over the pit takes away the jump across it (a real skip). TL_2 gives way 18 ticks after a touch, so Spikes_7
  are up 18 ticks before a running cat reaches them.
- **L019 Muscle Memory** (spears and the inverter, EXECUTE a memorised map): 64 × 16 (size (a), ruled 2026-09-28), start on
  the Cliff (62, 13), door bottom left (1, 0.75); right to left and down, one precision section over the run (x 2–60). The
  bridge (the approved rebuild): stepping to the cliff's edge sets the volley off, five spears from the Rack sticking in
  the cliff's and the pillars' west faces, each shaft the next step, 1.5 lower; then four arrows (sweepers) chained from
  the last spear. Map A: Orb_A (honest) flips the run as it starts; three hops mirrored (P2 gives way under a cat that
  lingers); the flip ends in the jump to P4, so the held direction switches back mid-air; Mover_1 carries the cat to
  Ledge_M. Map B: Orb_B (disguised) flips the cat in the jump off Ledge_M, so it switches into the mirror mid-air; it waits
  out the flip on Q1; Mover_2, a lift, takes it down into the corridor under the Slab; Spear_9 crosses the corridor at
  jump height and Floor_C gives way under a cat that stops. Every element is chained from a cat trigger; the two movers
  run on the room's clock and boarding them is the read. The second map is new input, not the first one twice: the flip
  starts in the air instead of on the ground (switch into the mirror, not out of it), the beat after it is a wait instead
  of a run of hops, the mover is a lift instead of a ferry, and the map ends on a walk-don't-stop, don't-jump corridor.
  1142 ticks; Bridge 363 / Map_A 411 / Map_B 368; both rewinds exact. 12 killers, 10 in sequence; answers B 2, J 1, NJ 2,
  NW 3, SS 2, W 3; the element in 11 betrayals. Chaos t21–t80, 5 in view of 7 (the volley). Longest stretch without a
  decision 111 ticks. Windows: 9 for the mid-air switch into Orb_B's mirror (8–11 only inside the precision section);
  every other timed step 27 or more. Moving floors: Mover_1 (a mover) and Mover_2 (a lift), both Carry. Every reveal is
  on screen from its first change to the kill at 4:3, 16:9 and 20:9, except the two movers, which run on the room clock
  from t2 and t32 and are betrayals' reveals only by being read: Mover_1 (T9) on screen 160/179/196 of 633 lead ticks,
  Mover_2 (T11) 367/472/502 of 1010 and (D2) 466/571/601 of 1109, at 4:3/16:9/20:9. The level's element rule names the
  spear and the inverter (`Band2Element` case 19, as 18: a spear's stuck shaft stood on, or the inversion changing).
  **Decided** (the working rule of 2026-09-28: within every band-2 rule, the verb and the size):
  1. The Rack is 1.1 u west of step 5's end, and its east face is thorned (`Thorns_R`, honest) from its foot to step 1:
     a running cat carries 2–3 u in a 1.5 u drop, so without them it ran off step 3 straight onto Landing A and past
     every sweeper. The steps are dropped from their ends, not run.
  2. The sweepers are timed on the run as built: Sweeper_2 takes a cat that stops on step 2, Sweeper_4 one that steps
     straight down from step 3, Sweeper_3 one that stands on step 3 (jump it), Sweeper_H crosses step 4 at jump height
     while the cat waits there (the jump that saved it on step 3 kills it here).
  3. Map B as built: no Q2; Block_E (honest thorns) under Q1; the lift's shaft; the corridor under the Slab (thorns on
     top) with Spear_9 flying east from Stub_9 into Stub_E at jump height (it crossed the lift's path flying west).
  4. Mover_2 runs every 200 from t30: the cat lands on Q1 as the lift leaves and waits a whole trip (the read). With the
     lift on its old period the solution was about 1060 ticks.
  5. P2's betrayal dies in the pit, not on hidden spikes: a spike strip under P2 alone would be a gap in the pit's
     hazard, pointing at the trap (P10). Floor_C fills its well, with hidden `Spikes_C` on the well's floor and no
     visible hazard under it (P10).
  6. T1 (step off the cliff before Spear_1 has stuck) dies to Spear_4 in the volley, not in the pit.
  7. Dead ends: D1 recovers (stay on Mover_1: it rides back to P4 and out again), D2 dies (turn back east once the lift
     has left: its shaft).
- **PAX-094 items (L019):** four betrayals have no escape once their reveal has happened: T1 (Spear_4 shows at t57; the
  cat has been falling since t27; its lesson is the wait for Spear_1, in view from t21), T4 (Sweeper_4's tell at t158;
  the cat left step 3 at t152), T7 (P2 gives way under a cat that stops, t524) and T13 (Floor_C gives way under a cat
  that stops, t1120). Each is on screen from its first change to the kill (D-083).
- **L020 The Machine** (the exam, every band-2 element; RIDE THE CHAIN REACTION): 32 × 24, start (16.2, 8) in the pen
  between Post_P and Gate_1, door on the Loft (7.5, 23.25); a spiral that doubles back: west to the lever, back east
  along the mid storey, up the chimney by the east wall, west along the Top over the start, up V_L to the Loft. The lever
  (the cut at the storey's west end) sets Gate_1 rising, Arrow_1 flying east at standing height and Push_3 shoving east
  under it into Post_P; past the gate Collapse_2 goes a step early; the chimney's mouth fires the volley (Spear_4a–4c,
  30 ticks apart) that builds the stair; climbing V_5 snaps it, and the snap flips the controls (Inv_6) and wakes the
  Cloud over the Top's east half; crossing the Spine's top drops Block_7a and Block_7b behind the cat; Shrink_8 narrows
  over its well; on the Loft the door backs away into G_9's column (Retreat_10), and the next eruption carries the cat
  into it. P8 ("The exam level … is exempt: it reuses earlier lessons on purpose, in a new order") exempts it. 1143
  ticks; Out 470 / Back 487 / Last 186; both rewinds exact. 9 killers, 9 in sequence; answers B 1, BAIT 1, J 2, NW 3,
  W 3; the element in 7 betrayals. Chaos t536–t595, 6 in view of 6 (G_9, V_5, Inv_6, Block_7a, Block_7b, Cloud). Longest
  stretch without a decision 222 ticks (the vent wait, t974–t1143, is 169). Windows 21 or more. Moving floors: Push_3
  (a push wall, Slip, crush partner Post_P), Gate_1 (a rising gate, Slip) and Shrink_8 (`ShrinkFrom.Both`, 180 ticks).
  Every reveal is on screen from its first change to the kill at 4:3, 16:9 and 20:9, except Push_3 at 4:3 (81 of its
  153 lead ticks, the last 81). The level's element rule names every band-2 element (`Band2Element` case 20: a stuck
  shaft stood on, the inversion changing, a launch or a climb). **Decided** (the working rule of 2026-09-28):
  1. The machine starts at a lever, not the step-off, and each stage has its own root in a cat trigger (the lever, the
     gate's cut, the chimney's mouth, the vine, the Spine's top, the shrinker, the Loft): the design needs waits the cat
     chooses (out the flip, under a strike, for the vent), and one root from the start would make every later beat a
     fixed run from t0 that no wait fits. Each stage is still a chain the cat sets off and moves with.
  2. G_9 runs on the room clock: a geyser is Periodic only (`GeyserTrap.cs:52`) and a Periodic trap can't take a chain
     source (`TrapLayoutValidator.cs:17`), so ruling 6's other branch applies: Shrink_8 takes 180 ticks (≥ 175).
  3. Length: the way east is shut (Gate_1) and the lever is at the storey's far west end, so the mid storey is run out
     and back (the double back); the Loft and V_L are added above the Top for the finale. The first draft, as
     sketched, measured about 613 ticks.
  4. The volley is three spears and the Perch, not four spears; T4b (Spear_4b) is the ninth killer.
  5. Dead ends: both recover (D1 G_9 on the way west, D2 Roof_N under the cloud); the sketch's Dies dead end (Spikes_D1 on
     the low road) went when the low road and its hole were cut in the fourth draft, while clearing validator findings.
  6. Top_W runs over Post_9, one piece under the vent: stepping from Top_W onto a separate Post_9 of the same height
     gave the two replays in one session a Vx differing by 5e-7 at one tick (t1089), and the determinism check failed.
- **L017 Déjà Vu** (spears and chains, PLAN THE WAY BACK; the approved variant, "the way out builds the way back, in
  plain view"): 32 × 16, two storeys (lower floor 0, upper floor 8, 0.7 thick), start (2, 0), door on the floating
  Door_Ledge above the start (6.25, 10.55). Out along the lower storey: the start's cut fires Spear_D into the
  Door_Ledge's west face (its shaft, top 9.45, is the only step up to the ledge, 1.8 above the upper floor); Crack_1
  (0.6 u, narrower than the cat) invites a hop into Spear_1's lane at jump height; Block_2, flush in the upper floor,
  drops onto the path 10 ticks after its cut and sets off Collapse_U beside it 2 ticks later, which leaves the hole for
  the way back (ruled 2026-09-28: the builder never cuts a flush block out of its host, so a falling block can't leave
  a hole itself; `CollapsingFloorTrap.cs:26–30` keeps a Once collapse open until the room resets, and a rewind restores
  it from `IsTimingEffectActive`); Collapse_3 gives way under a cat that stops; Slide_4 (Carry) carries a cat that
  stands still over Pit_4. Far end: the far cut fires Arrow_5a along the shaft's floor at shin height into the Sill
  (BAIT: step back over the Sill into the nook), and 150 ticks later the volley V1–V5, 12 apart, builds the stair,
  alternating Face_R and the Spine. Back along the upper storey: a step off the stair brings up Spikes_5e (they stay;
  jump them) and drops Block_6 where the cat stepped off; hop Post_8; Arrow_8 comes out of it at jump height over a
  standing cat (let it pass, then jump the hole); under the Door_Ledge to Post_W, up Spear_D's shaft, onto the ledge.
  1152 ticks; Out 305 / Far 514 / Back 333; both rewinds exact. 9 killers, 7 in sequence; answers BAIT 1, J 1, NJ 1,
  NW 2, SS 1, W 2; the element (spear or chained trap, `Band2Element` case 17, as 11: a stuck shaft stood on) in 4
  betrayals. Chaos t523–t582, 5 of 5 in view (the volley). Longest stretch without a decision 219 ticks (the nook
  wait, ending in the timed step out of it, window 51). Windows 12 or more. Moving floor: Slide_4, a slide-away that
  comes home (Carry, Rearm). Every reveal is on screen from its first change to the kill at 4:3, 16:9 and 20:9
  (Block_6 for 20 of its 21 lead ticks). **Decided** (the working rule of 2026-09-28):
  1. The volley is five spears, not three (the stair has to climb the whole 8 u storey at 1.45 a step), starting on
     Face_R so no step hangs over the Sill; every launcher sits in a post inside the room (Face_R, Post_W, Post_8),
     since a launcher may not lie outside the room's frame.
  2. The nook is behind the Sill (x 28.6–29, a cat's width clear of the Spine, so it is hopped under open air): an
     arrow's lane must end at a solid face, and the Sill is Arrow_5a's.
  3. T6 is Block_6 on the upper floor where the cat steps off the stair (NW), not a near spear setting off a block (OL);
     Spikes_5e and Block_6 fire at the stair's top (see (5)).
  4. The sketch's T9 (Fake_9 over Spikes_9) is dropped: a fake step beside the ledge can't clear the trap-floor
     headroom rule over the upper floor. 9 killers without it.
  5. Dead ends: D1 dies on the Shelf over the start, reached from Spear_1's shaft (hidden spikes); D2 recovers (stay on
     Slide_4: it rides home, is set off again and carries the cat out). The nook is the BAIT's answer, so it's in the
     solution, not a dead end.
  6. Slide_4 comes home (Rearm) instead of staying as the stair's first step, so a cat that drops through the hole to the
     lower storey can go round again (no soft-lock).
  7. The Door_Ledge floats (underside 8.7, over a standing cat) so the way back runs under it to the west end.
  8. Length: the volley starts 150 ticks after Arrow_5a (the nook wait ends in a timed step, window 51) and Arrow_8
     tells for 60 ticks (the hole jump after it is timed, window 15). The slide is not slowed.
- **L016 Upside-Down Garden** (gravity flips and vines, TWO SURFACES): 32 × 20, the Bed (x 0–29, y 9–10), start on its
  top (22, 10), door on the ceiling over the floor's west end (2.5, 18.25). A coil: east along the Bed's top, jump from
  before Lip_2 into the gap; stand in the Dip while Arrow_3 crosses it at jump height; west along the floor; V_B's top
  flip (ForceUp) onto the underside (V_A's lands the cat on Tile_6, which gives way onto Thorns_6); east upside down,
  down V_7 and a leap under the hidden Flip_H7 across Recess_7, down V_8 and a leap across Recess_8; over Lip_2's
  underside, which gives way, and up the gap to the ceiling; jump Spikes_C; Flip_S0 (Once) drops the cat onto the Bed's
  top west of the Hedge and sets the storm off (Spikes_H on the Hedge, Spikes_S1 ahead for 60 ticks, Spikes_S4 under
  the Bed, Block_S3 onto the landing, Spikes_S5 on the ceiling); Flip_S2 back up past Thorns_A; jump Spikes_S5; the
  door. 1155 ticks; Bed 466 / Underside 318 / Sky 371; both gravity-up rewinds exact, gravity included (the compared
  records carry it; ruled 2026-09-28). 11 killers, 9 in sequence; answers J 3, LW 2, NJ 1, NW 1, OL 1, SS 1, W 1; the
  element (gravity flip or vine, `Band2Element` case 16: a climb, or the cat's gravity changing in the betrayal's own
  part) in 3 betrayals. Chaos t928–t987, 5 of 5 in view (the storm). Longest stretch without a decision 209 ticks.
  Windows 12 or more. No moving floor. Every reveal is on screen from its first change to the kill at 4:3, 16:9 and
  20:9 (Spikes_C for 72 of its 75 lead ticks). **Decided** (the working rule of 2026-09-28):
  1. The sketch's floor-to-underside flip beat (Flip_U1, Flip_D1, Thorns_F, Arrow_5, Tile_4: T4, T5) is cut: it put a
     gravity-up cat on the underside's east end, from where it walks off into the gap and falls up to the ceiling,
     skipping the underside; every way round a stub there is open to that cat too.
  2. Crack_1 and the hidden Flip_H1 (T1) are cut: a cat thrown up by the flip drifts about 4.4 u while it falls up 8 u,
     so no ceiling thorns stop it short of the Sky storey (a shortcut past two sections).
  3. The Bed's holes are recesses in its underside (the top half solid, thorns or hidden spikes inside): a real hole
     lets a gravity-up cat fall straight up to the ceiling, a shortcut to the door.
  4. The Hedge (x 19.5–20, 3 tall) parts the start's stretch of the Bed's top from the storm's: Flip_S2 a step from the
     start was a shortcut to the ceiling by the door. The storm is on the Bed's top west of it, with its own elements.
  5. V_8 is a plain vine, and the underside's vines reach down to 1.5 u over the floor (the vine rule R5): a snap vine
     within a jump of the floor could be snapped in the Bed section, and the underside would soft-lock. T8 is Recess_8,
     too wide to jump, with hidden Spikes_8 in it (they come up once the cat walks the floor under it).
  6. Stub_9, G_9 and Slide_9 (T9, the BAIT) are cut: the underside is left by walking onto Lip_2's underside, which
     gives way under the cat and lets it fall up the gap. L016 has no BAIT.
  7. T2's Spikes_2 are in the Dip's east end, where a cat that walks off the Bed's end lands (one that jumps from before
     Lip_2 lands short of them); Arrow_3 crosses the Dip at jump height from Post_E, since the Dip is left only by a hop.
  8. Both dead ends die (V_D, "straight up to the door", and west under the Bed, both onto Spikes_X): a plain vine or a
     nook gives a Recovers route nothing that changes to reveal it.
  9. Spikes_C (T13) hang on Lip_2's chain rather than a cut of their own (see (5)).
- **Known limit (L016):** the coverage search can't climb a vine into the flip at its top (V_B, V_A), so from the
  checkpoint it reaches neither the underside nor the ceiling; a trigger there is judged from the checkpoint's side. No
  L016 trigger there depends on it: Spikes_C hang on Lip_2's chain (a collapsing floor's chain is covered by
  construction) and the storm on Flip_S0's. For a PAX-095-style ticket: a climb into a flip.
- **PAX-094 items (L016):** T2 (Lip_2 gives way under the walker, t45) and T6 (Tile_6 gives way under a cat that stays,
  t433) have no escape once their reveal has happened; T3's is 2 ticks (Arrow_3's tell at t99, the hop at t101). Each is
  on screen from its first change to the kill (D-083).
- **PAX-094 items (L017):** T3 (Collapse_3 gives way under a cat that stops, t167) and D1 (Spikes_D1 show at t113,
  with the cat already over the Shelf in its jump) have no escape once their reveal has happened; T1's is 4 ticks (the
  tell at t31, the takeoff at t35). Each is on screen from its first change to the kill (D-083).
- **PAX-094 items (L020):** two betrayals have no escape once their reveal has happened: T3 (Collapse_2 goes, t291, as
  the running cat crosses its cut) and T4 (Spear_4a's tell, t368, as the cat takes off). Each is on screen from its first
  change to the kill (D-083).

Direction mix across 11–20: half B. Tests: `Band2RulesTests` (47), `Band2LevelTests`, `Band2RouteResultsTests`.

### D-094 · 2026-09-29 · Accepted (PAX-A13 rulings §11, R6)
**Decision:** Visual production starts. The target for Reality A is `Docs/Art/LOOK_AND_FEEL.md`, with
`Docs/Art/Reference/styleframe_A_v1.png` and `DesignImages/14_environment-breakdown.png` as the references: the in-game
frame should read like the styleframe on a phone. This supersedes D-015's "evokes the key art rather than reproducing its
painterly depth" and lifts CLAUDE.md's "no final art" for Reality A (Reality B stays as it is, D-047). The performance
budget (draw calls, lights, particles, texture memory, fps on the Pixel 8a) is set by PAX-V03; until it lands, each art
ticket works to interim numbers its rulings set.
**Why:** PARALLAX is a paid game (D-068); the developer's bar is that it looks spot on, with a real 2.5D feel, finished
trap animations and a cat that feels alive.
**Consequence:** Phase E tickets (PAX-V03, A12, A08, A13, V06, A09, A10) build to LOOK_AND_FEEL. Art never moves a
collider, tick, route pin or validator result (D-052; LOOK_AND_FEEL §3).

### D-095 · 2026-09-27 · Proposed (PAX-093 as built; rulings §7)

**Decision:** KIT-10, floors that move (PAX-093).
(1) **The carry.** A `MovingTrap` Solid has a `SurfaceMotion`: **Legacy** (the default, every element before PAX-093: not
carried, exactly as before), **Carry** or **Slip** (not carried, declared). A LocalHuman cat grounded on a Carry floor
(`CatMotor2D.GroundCollider`) moves with its displacement this tick, pose(t) − pose(t−1), in the room step after every trap
and before the hazards (`RoomManager`, `CatMotor2D.ApplyCarry`). The part along the ground moves the cat by position; the
part away from the ground raises its fall speed to at least the floor's for the step (a position shift there starts the
physics step inside the floor, and the solver undoes it); the part into the cat stays physics', so D-056 (3)'s
launch-on-stop is unchanged. Not while climbing, frozen (the death hold) or not grounded; a launch that tick has already
cleared the grounding. On the tick a grounded cat jumps, the floor's sideways speed is added to its velocity once instead
(Q3); walking off adds nothing. The carry keeps no state (a rewind has nothing to restore).
(2) **The patterns.** A *mover* is a Periodic Carry Solid; a *slide-away* moves sideways once (Carry: you ride it into
danger; Slip: it slides out from under you); a *drop-and-return* floor drops and rises back (Rearm, move/hold/return,
cooldown ≥ move + hold + return; the return is the existing return motion, not the Rearm snap); a *push wall* is a Solid with
`Pushes`: the kit puts a cat in its way flush against its leading edge each tick (no physics shove), and a push into its
named crush partner by the crush depth kills (D-055 (3)); `Crusher` and every Legacy Solid are untouched. A *shrinking
floor* (`ShrinkingFloor`, appended) narrows its one collider and its sliced look together, from its left, right or both
edges, over `ShrinkTicks` to `MinWidth`, as a pure function of the ticks since its fire; Rearm, reset and rewind restore it
by the same formula. A PAX-093 `MovingTrap` honours its `DelayTicks` (a drop is harmless until it goes); a Legacy one keeps
the 0 it always had, so `Crusher`'s and `SlidingSpikes`' unused 6 still do nothing. **Known legacy inconsistency:** those two declare a `DelayTicks` they ignore; it stays unchanged so their pins hold. A Periodic `MovingTrap` runs without a
trigger box (there were none before).
(3) **Validators** (`ValidateMovingFloors`, in `ValidateKit`): in levels 14+ and Trap Lab room 12 a sideways Solid declares
Carry or Slip; a repeating Solid's cooldown covers its motion; a shrinker's settings (ticks ≥ 1, 0 ≤ minimum < width); a push
path (the wall's leading edge to its stop, plus one cat width, over its height) ends against its named partner or in open
space, and runs into no other fixed solid; D-056 (3), a Solid's swept path overlaps no fixed geometry; D-065, movers,
shrinkers and push walls are levels 11+, slide-away and drop-and-return are allowed in 1–10. Surface coverage treats a
shrinker as a betraying surface (its own top covers it, as a collapsing floor's).
(4) **Coverage** (D-074): a Carry mover carries the cat between the ends of its path (both ways when it returns); a drop is
also blocked by a vertical wall, fixed solids spanning its whole height between where the cat leaves and where it could
land. Existing results: no error appears or disappears; L012's `Spikes_D2` (the Nook) is now reached only from the east.
L012 `Orb_B`'s natural form still fails, for a real reason: the Shelf carries a cat over the orb to Collapse_F's west side.
(5) **The harness** sees the carry and the shrink with no new format; its sprite signature gains the drawn size, so a
shrink is a visible change (every other element's comparisons are unchanged). A push wall's partner crush names the wall.
(6) **Trap Lab room 12**, one of each pattern in three checkpoint sections (Ride 584, Sink 177, Shove 172 ticks; solution
933): windows 51 (off the Slider, open both ways), 15 (off the Drop), 25 (up onto Ledge_P); leads Mover 60, Slider 30, Drop
25, Shrink 56, Pusher 74; both rewinds exact.
**Why:** Level Devil's core moves; the kit had moving Solids that carried a cat only upward and nothing that shrank.
**Tests:** `MovingFloorMathTests`, `MovingFloorHarnessTests`, `MovingFloorValidatorTests`, `CoverageEdgesTests`
(`MoverOnlyLedge`, `VerticalWallDrop`), `TrapLabRoom12Tests`. Red-checked at acceptance (each broken once, seen failing, restored): `ASidewaysMover_LeftLegacy_IsRejectedInLevel14_AndAllowedInLevel13`, `AShrinker_ConfiguredWithUnderOneTick_IsRejected`, `APushPartner_ThatIsntAFixedSolid_IsRejected`.

### D-096 · 2026-09-28 · Accepted (PAX-060 L014 finding)

**Decision:** Trigger coverage (D-074, PAX-091's approach search): a grab obeys drop blocking, and a drop is closed by
every fixed solid that reaches into its height band.
(1) **Grabs.** A grab onto a vine that drops to it, from a surface or as a leap from another vine, is blocked as a walk or
a drop is: by `ClosedPit` (fixed solids closing off the drop between them) or `Walled` (a wall the drop's full height).
Before, the search let a cat on a floor slab grab a vine hanging under it (L014's canopy vines under the Low floor), and a
cat on V3's foot grab C6 through Pit3_Floor.
(2) **`ClosedPit`** counts every fixed solid that reaches into the drop's height band (below the departure, above the
landing), not only those whose top lies in it. A floor block beside a trough rises above the trough's floor and still
closes a drop off the trough's end (L014's Trough beside the Low floor). This applies to walks and drops as well as grabs.
(3) **No change elsewhere:** trigger coverage, surface coverage, learned bypasses and `ValidateKit` for L001–L013 and Trap
Lab rooms 0–12 were snapshotted before and after: all 0 errors both times. Only L014 changed (2 → 0), with C1's
approved column trigger.
**Why:** the search's grab and drop edges ignored solids a real cat can't pass, so L014's canopy was "reached from both
sides" through the Low floor, and the workaround (widening C1's trigger) was ruled out.
**Tests:** `CoverageEdgesTests.AGrabOntoAVineUnderASlab_IsNotAWayIn_SoTheCutPasses` (from the slab; from a vine standing
on it) and `AGrabFromATroughOntoAVineUnderTheSlab_IsNotAWayIn_SoTheCutPasses`, each seen red before its part of the fix.

### D-097 · 2026-09-28 · Accepted (PAX-060 L014 T1; PAX-094 pulled forward in part)

**Decision:** an opt-in escape-backed camera tell check. A betrayal may declare its escape; its reveal is then checked
against the last tick the escape still works, not against the kill. Betrayals without a declared escape keep D-083's
check unchanged.
(1) **The declaration.** `Betrayal(…, escape: d => Route)`: `Escape(d)` presses its way out `d` ticks after `RevealedBy`'s
first visible change (`R.Revealed`) and completes the level.
(2) **The last escape tick.** The validator replays `Escape(0)`, `Escape(1)`, … (at most the betrayal's own lead) while each
completes the level; the last escape tick is the reveal plus the last such `d`. An escape that doesn't complete even
pressed at the reveal is an error.
(3) **The check.** In that last escape's replay (the same path as a cat that hasn't pressed yet, up to the press), the
reveal is on screen at every tick from the reveal through the last escape tick, and that span is at least
`RouteValidator.WindowTicks` (12, D-056 (1)). The worst of D-083's 24 camera cases counts, at 4:3, 16:9 and 20:9; fit mode
passes the on-screen part trivially but still needs the 12. The table row reads "escape, on screen N of M (reveal t…, last
escape t…)", N counting from the reveal to the first tick off screen.
(4) **L014 T1** declares `L014Routes.EscapeLip` (stop on Lip_1, hold Right and Down, catch V2): Lip_1 gives way at t73,
the last escape is t105 (+32), and Lip_1 is on screen 33 of 33 at every aspect. Under D-083 alone it read "on screen 0 of
lead 53": the camera follows the 15 u fall and leaves Lip_1 4 ticks before the kill at t126.
(5) **No change elsewhere:** `ValidateKit`, trigger and surface coverage, and the camera tell table for L001–L013 and Trap
Lab rooms 0–12 were snapshotted before and after: identical (Trap Lab room 11's three D-083 errors for `Spear_1` are
unchanged; room 11 isn't in `CameraTellTests`).
(6) **Making it the default** (the reveal rule of 2026-09-28 for every hidden hazard) stays PAX-094. Note for PAX-094:
Trap Lab room 11 has 3 existing D-083 errors (`Spear_1`, "on screen 0 of lead 8" at every aspect), not covered by
`CameraTellTests`.
**Why:** a reveal at the cat's feet followed by a long fall passes the reveal rule (on screen before the last tick its
escape works, with time to do it) and fails D-083, which measures up to the kill.
**Tests:** `CameraTellTests.AnEscapeBackedReveal_ThatLeavesTheViewBeforeTheLastEscape_Fails` (`PrecisionFixtures.EscapeTellRoom`:
Lip on screen 50 of 103 ticks; seen red with the span check weakened to 12 ticks) and
`L014_T1_WithItsEscapeDeclared_Passes` (seen red under D-083 before the check existed).

### D-098 · 2026-09-28 · Accepted (PAX-095 as built, ruled 2026-09-28 before L016)

**Decision:** The trigger-coverage approach search (`TriggerCoverage`, D-074; PAX-091) follows a gravity-up cat onto and
off vines, launches a climbing cat, and starts where the checkpoint stands, an underside included.
(1) **Gravity-up vines.** In a room with a gravity flip, each vine also gets gravity-up nodes. A cat keeps its gravity
on a vine (D-089 (3): a gravity change releases it; D-092: a grounded gravity-up cat grabs by pushing screen-down), so a
grab or a leap never changes gravity: a gravity-up cat grabs a vine from an underside and leaves it (a leap or a release)
falling up onto an underside or another gravity-up vine node. These edges are the gravity-down ones on the room mirrored
top to bottom (`Mirror`: y → −y, an underside becomes a top, a gravity-up node's top range a paws range), so every rule
the gravity-down edges keep (the trigger cut, the closed pit, the vertical wall, D-096's grab through a slab) holds for
them too. Before, `Climbs` refused an underside and a leap onto one.
(2) **A launch from a vine.** A cat climbing a vine whose collider overlaps a vent's column is launched with that vent's
envelope, as one standing on the vent is (the eruption launches whatever overlaps the column, and the launch releases the
climb, D-089 (3)); a Down vent launches a gravity-up node. Before, `Launches` refused a vine.
(3) **The start.** The search starts on the surface the checkpoint stands on, a top or an underside (a gravity-up start);
otherwise, as before, on the highest top at or below it.
(4) **No change elsewhere:** the approach sides of every trigger, and every coverage error, of L001–L015 and Trap Lab
rooms 0–12 are identical before and after (snapshotted; L017–L020 too). Everything here only adds reach.
(5) **Known limit (found building L016, ruled 2026-09-29):** the search doesn't follow a cat climbing a vine into a
gravity flip at its top (the flip releases it, D-089 (3), and it falls to the flip's gravity). A flip is reached only
from a surface within a jump of it, so a flip at a vine's top is out of reach, and whatever lies past it (L016's
underside and ceiling, reached through V_B's flip) is judged from the checkpoint's side. No L016 trigger depends on it:
Spikes_C hang on Lip_2's chain and the storm on Flip_S0's. A future edge: a climb into a flip.
**Why:** L016 (two surfaces) moves between a slab's faces by flips and vines; the search couldn't follow it (D-093
half-B stop 1). **Tests:** `CoverageEdgesTests`: `DangerBeyondItsTrigger…`/`DangerBeforeItsTrigger…` with
`CeilingVineSlab` (ceiling → vine → underside) and `CeilingStartVineSlab` (a gravity-up start), and the launch edge on
hand-made pieces (`ACatOnAVineInsideAnUpVentsColumn_IsLaunched`, `…OutsideTheColumn_IsNotLaunched`,
`AGravityUpCatOnAVineInsideADownVentsColumn_IsLaunched`); all but the control were seen red before the change.

### D-099 · 2026-09-29 · Proposed (PAX-A13 as built; rulings §11 R1–R6, §12 R7–R10)

**Decision:** The trap presentation contract.
(1) **Presentation reads, never writes.** Trap art lives in `Parallax.Presentation`, on a separate `Room_N/Art` root
beside the grey-box, never under an element. Every presenter is a pure function of trap state and ticks (fire tick,
phase, `RoomLifeTick`, the death hold), reapplied every frame; its randomness is `TrapArtMath.Seed(name, fire tick)`, never
`Time.time` or `Random`. The grey-box renderers stay as they are, and the route harness still reads them; the art only
sets their `forceRenderingOff`, at runtime, in `Awake`. The art adds no collider and moves no tick, route pin or
validator result (D-052, D-094). The kill's source is read through `RoomDeath.HoldKiller`/`HoldCause`/`HoldTick`
(`DeathInfo.Killer`), which only report it.
(2) **Visibility parity (R4).** The art never shows more, earlier, or elsewhere than the grey-box. A body is drawn exactly
when its grey-box is, inside its grey-box's bounds (1 px at 128 PPU); each effect belongs to a named group whose rule
says when it may show (the tell only in the tell window, a shard only after the reveal, a death effect only during the
hold, and only the effect of what killed the cat). The windows are read from trap state (a block's landing from its travel,
a mover's dust from its grey-box moving), with each effect's length in `TrapArtMath`. `TrapArtParityTests` replays every
declared route of L001–L020 and Trap Lab rooms 3–12 (rooms 0–2 have no routes) with that check on every tick.
(3) **A disguise is its host's skin (P10, R8).** Before its reveal, a disguised trap (collapsing floor, fake platform,
falling block, moving and KIT-10 floors, push wall, shrinker, a disguised launcher) draws exactly its host's look,
recorded at build time (`HostSkin`) and checked by `ValidateTrapSkins`. A world-tiled host (`Sprite-Lit-WorldTile`)
samples its tile in world space; a disguise and each of its shards sample at the pose where the reveal finds them and
keep it, so the reveal frame is pixel-identical to the frame before (`TrapArtRevealFrameTests`), except effects that
start that tick. A crumbling disguise on a patterned host (Tiled, or Sliced with a border) must be world-tiled, since
its shards would otherwise each tile from their own corner; `ValidateTrapSkins` rejects one that isn't.
(4) **Bodies and effects (R7).** Solid bodies are ChatGPT stills post-processed by `Tools/Art/trap_process.py`
(deterministic, 128 PPU, generated normal maps; `trap_kit.json` lists the placeholders), imported by
`PARALLAX/Art/Import Trap Kit` into one atlas. All motion is code. AutoSprite is used only for effect stills on black
(1 credit each, 16 spent of the 40 planned / 80 ceiling); its animation pipeline is not used for props (it inserts a
character). **AutoSprite is kept for the cat's animations (PAX-A08), not props.** A code-drawn body marked PLACEHOLDER
stands in until its ChatGPT still is saved; swapping it changes no test.
(5) **Interim budget (R6, until PAX-V03).** One trap atlas of at most 2048², at most 150 effect sprites drawn at once
(peak measured: 87, L013), no new 2D lights; flipped sprites use `Sprite-Lit-Flip` so their normal maps shade correctly.
**Why:** PAX-A13 dresses every trap without letting presentation change what the game does or tell the player more than
the grey-box did; the troll room still betrays exactly as validated.
**Tests:** `TrapArtBuildTests` (colliders and harness renderers identical with and without the art; every trap and bare
hazard has art; `ValidateTrapSkins`), `TrapArtParityTests`, `TrapArtRevealFrameTests`, `TrapArtBudgetTests`,
`TrapArtMathTests`.

### D-100 · 2026-10-01 · Accepted (the developer's ruling, PAX-A16 §3.0)
**Decision:** The level camera shows a fixed 1.2× zoom. It amends D-071's constants (item 4) and leaves its rules alone.
- `MaxViewHeight` is 16 ÷ 1.2 = 13.33. Every shipped level's frame is taller or wider than that at every aspect, so every
  level is in follow mode. Fit mode stays in the code, but no level reaches it.
- `LookAhead` is 1.5 (was 2.5) and the dead zone is (1, 1.6) (was (2, 1.6)). At the narrower view these keep D-083's
  reveals on screen at 4:3, 16:9 and 20:9.
- `ViewMargin`, `smoothTime` and `maxSpeed` are unchanged.
- The values are written by `PARALLAX/Setup/Levels/Environment Stack` (`EnvironmentStackSetup.ApplyCameraZoom`), never
  by hand.

**Layout changes this ruling needed** (the smallest that pass every validator):
- **L011:** `Spear_1` fires from a mount under `Core_W` (x 5.75), over the gap, delay 41. `Ledge_M` sits one storey lower
  (y 7.45), between `Spear_5` and `Spear_6`, so its fall to the foot stays in view.
- **L018:** `Post_W`, `Spear_S`'s host, hangs under `SB_W` at x 4.95.
- **Fixtures:** `CameraTellRoom`'s fit-mode room is 15 wide, and `Band2RouteResultsTests` re-pins L011 and L018.

**Why:** the developer asked for the concept art's framing: the cat larger, the room's outside out of view (2026-10-01,
"1.2x zoom as the fixed state, much like in the concept art").

**Accepted side effect:** the door is often off screen when a level starts. The camera brings it into view as the cat
moves.

### D-101 · 2026-10-01 · Accepted (the developer's ruling, PAX-A16 round 3)
**Decision:** Trap art draws 10% bigger than the trap; hitboxes, routes and timing are unchanged. It amends PAX-A13's
parity rule ("a body sits inside its grey-box's bounds").
- A trap body may draw past its grey-box by (`TrapArtConfig.BodyScale` − 1) of the grey-box's size on each side.
  `BodyScale` is 1.1. It's never offset, and never shown earlier or later than the grey-box.
- Bigger: spikes (each tooth, base on its host), arrows and spears with their launchers, geyser vents and columns
  (wider, never longer), the storm cloud and its target, flip glyphs, and the inverter.
- Exact (1.0): every disguise, because P10 needs it to cover exactly its trap: collapsing floors and fake platforms,
  falling blocks, moving and shrinking floors, and a disguised launcher's host skin and its projectile (which rests
  behind the host skin; 10% more would poke out before the reveal, caught by `TrapArtRevealFrameTests`).
- `TrapArtParityTests` checks the grown bounds. `TrapSkinValidator` is unchanged (the disguises are exact).

**Why:** the developer asked for every trap 10% bigger and chose art only (2026-10-01). Danger then reads slightly
beyond the kill zone, never less.

### D-102 · 2026-10-01 · Accepted (the developer's ruling, PAX-A16 round 3)
**Decision:** The level camera's fixed zoom is 1.8× (`MaxViewHeight` 16 ÷ 1.8 = 8.89). It amends D-100's zoom.
D-100's look-ahead and dead zone stay until the camera-tell validators are re-run. The foreground frame scales with the
view height, so it keeps its share of the screen.

**Why:** the developer compared a capture with the concept art (2026-10-01, "nowhere close"). In the concept the cat is
about 1/7 of the screen height, and at 1.2× it was about 1/17. The developer chose 1.8× as the compromise that keeps a
room readable ahead of the cat.

**Open:** D-083's reveal-on-screen checks (`CameraTellTests`, `Band1LevelTests`, `Band2LevelTests`, Trap Lab rooms 6–10)
haven't been re-run at 1.8×. They run after the developer approves the look, and any level that fails gets the smallest
layout change, as D-100's did.

### D-103 · 2026-10-02 · Accepted (the developer approved the gauntlet's Phase 1 look on L002)
**Decision:** The environment follows the gauntlet's rules (PAX-A16, the developer's brief of 2026-10-01). These amend
PAX-A15's §2.2–2.4 rules:
- **Textures:** layers are imported at 128 px/u (finer than a 20:9 phone at D-102's 1.8× view), the play layer and its
  fills at 196.667. Every environment texture and the dressing atlas are mipmapped and bilinear.
- **The play layer:** the painterly A_GAME stone replaces the ENV-10/11/12/14 tiles (rejected in the triage). Fills blend
  two stones through a seamless noise mask (about 11.7 u before a repeat). No dressing is baked into a tiled strip.
- **Dressing:** tufts, ivy and roots are placed one by one from a seeded hash, the same rule for real and disguised blocks
  (P10). Tufts draw behind the stone and every trap and avoid only the door and checkpoints. Ivy and drapes avoid
  everything the player must see, but never an invisible trigger (a bare patch over a trigger would mark the trap).
  Rubble, banners and glyphs keep PAX-A15's keep-out.
- **The background:** whole pieces placed once (clouds, far skyline, the level's signature), never a tiled band, with
  haze gradients between depths and a palette per level (`LevelPalettes`). The sky and the far haze still cover every view.
  Painted mid pieces are never scaled. Haze gradients and the water strip are exempt.
- **Readability:** pits fall into a dark void with a warm glow at the lip; block ends are broken stone with a sunlit rim.

**Why:** the developer's gauntlet brief (no tiling, no crunchy resampling, depth and light from code). Phase 1 brought L002
from 5.6 to 7.8 on the critic's scorecard, and the developer approved the look on 2026-10-02.

### D-104 · 2026-10-02 · Accepted (the developer's camera ruling of 2026-10-02; D-102 measured)
**Decision:** Each level has its own camera, in `LevelCameras` (Editor), baked onto its `LevelCameraFollow` by the level
build. The camera tell, escape and chaos validators read the same table, so the game and its checks agree.
- **Zoom:** D-102's 1.8× where every camera rule passes (D-083's tell, D-097's escape, D-093's chaos), else the largest of
  1.5× and 1.2× that does. Measured by replaying every route through the game code at each zoom:
  - 1.8×: L001, L002, L003, L006, L007, L010, L013.
  - 1.5×: L008, L012, L014, L018, L020.
  - 1.2×: L004, L005, L009, L011, L015, L016, L017, L019.

  Trap Lab rooms 3, 6 and 12 validate at 1.5×. The lab's own camera doesn't read the table, and room 11 isn't listed: its
  camera tell isn't tested.
- **Lift:** the follow target sits 1.5 u above the cat (`CameraMath.FollowParams.VerticalBias`, follow mode only) where
  every hazard on the solution route stays as visible as before (the developer's rule). This applies to L001, L003, L005,
  L009 and L015, on the room auditor's ruling: a pit under a trap floor is read from its opening (the floor covers its kill
  plane), while an open pit's kill plane counts. The lift breaks D-083 in L008, L012 and L016, and hides a hazard in the
  rest.
- The fit-mode test runs under a 16 u view (no shipped level's frame fits a zoomed view).

**Why:** at 1.8× everywhere, 13 levels failed D-083 or D-093. A per-level zoom keeps every gameplay rule without layout
changes; the alternative was layout edits in about 13 levels.

**Open (test gap, the auditor's):** no validator holds the lift's rule. A check per lifted level should count only ticks
where an element can kill and isn't covered by a trap floor (open-pit planes counted).

### D-105 · 2026-10-02 · Accepted (the developer's play-test notes, Phase 2)
**Decision:** Three readability changes, all art only (no collider, trigger, route or timing changes):
- **Spikes:** the strip is blades only, without the stone base that blended into the floor: near-black iron (value
  0.07–0.15; the developer: "spikes are grey, make them black, it's more noticeable") with a pale lit edge and a faint grey
  rim, so they read on bright skies and keep their shape on dark stone. They draw `TrapArtConfig.SpikeHeightScale` (1.8) times
  their hitbox's height, from their base outward. The hitbox is unchanged, so the drawn tips reach past the kill zone
  (the forgiving direction, as D-101). Only thin strips (grey-box ≤ `SpikeArt.MaxStripHeight`, 0.5 u) are lengthened;
  a taller box, such as a sweep waiting inside a post, keeps its height so it never pokes out of its host. Every spike
  body is one row of blades tiled across and stretched to its drawn height, never a second, cut-off row.
  `TrapArt.BodyGrowth` gives the parity test each body's allowance.
- **Foreground:** the view-pinned, blurred corner foliage is gone (it moved with the camera and could cover a trap), and
  so are the foreground corner ferns and roots (`ENV_FG_Ferns`, `ENV_FG_Roots`; the developer: "these front ferns
  everywhere, I don't like it"). Nothing is drawn in front of the play layer.
  Sharp fern clumps (ENV-26) are fixed in the world at the foot of tall block faces, at least 1 u under the walk line,
  clear of everything the player must see, with the same rule for real and disguised blocks (P10). Their dense lower
  leaves fade into the stone, so a clump never reads as a dark box.
- **Doors:** superseded by D-106 (the developer: the platform must be real geometry, not art).

**Why:** the developer's play-test (2026-10-02): "spikes need to be longer and clearer"; "I don't like that the front
ferns move with the camera… they could cover a trap… make sure they don't look blurry"; "exits just hanging in the air
don't make sense… keep the door on a platform."

### D-106 · 2026-10-02 · Accepted (PAX-099, the developer's play-test and Phase 1 rulings)
**Decision:** KIT-11. Angled and repeating arrows, every floor pattern from level 3, and doors on platforms.
- **Angled arrows** (amends D-078 "horizontal only"). An arrow, never a spear, may fly at 0°, ±30°, ±45° or ±60° from
  horizontal (`ArrowLane.AngleDegrees`). The sign is world up, whatever the cat's gravity. The mouth stays on the
  launcher's side face at `LaneY`; the tip stops where it reaches x = `LaneEndX`, on a fixed solid's face or at the room's
  end. Pose, tell, flight and stop are D-078's, as a pure function of ticks along the turned lane. The kill test and the
  harness's killer naming are the same `OverlapBox`, turned by the lane's angle (`ArrowTrap.KillAngle`). A stopped angled
  arrow is harmless and non-solid: spears stay the only footholds.
- **Speed cap.** D-078's rule along the lane, with the cat's closing speed along it:
  `v ≤ L + (w − h) − 2·(run·cos a + vertical·|sin a|)`. Here `vertical` is the larger of the jump launch speed and the fall
  cap (0.4 u/tick today), and at 0° the cap is D-078's exactly. With an 0.8 arrow, the caps are 0.63 (30°), 0.50 (45°)
  and 0.43 (60°) u/tick. The EditMode sweep proves that at the cap the cat's core never crosses an arrow between ticks,
  from any direction, including across the lane's thin axis. A graze of the capsule's round rim can still be missed,
  as D-078 already accepts.
- **Lane checks.**
  - The turned lane, trimmed at each end by where its thickness meets the launcher face and the end face, crosses no
    fixed solid (separating axes).
  - The swept-path, door-clearance and frame checks use the axis-aligned box around the turned lane (conservative).
  - Periodic slack measures the lane's crossing within the cat's band, not its whole length.
  - A Periodic arrow's cooldown must be below its period (otherwise it would skip shots).
- **Camera tell (D-083).** An angled arrow counts as on screen when a corner of its launcher or of its turned box is in
  view (`TickRecord.TurnedCorners`), never by the sprite's axis-aligned box.
- **Repeating arrows** need no kit change: Periodic and Rearm arrows have worked since D-078. Each level sets its own
  `PeriodTicks`.
- **Floor patterns from level 3** (amends D-095 (3) and D-065's split for floors). Movers, shrinkers and push walls are
  allowed from level 3; levels 1–2 keep slide-away and drop-and-return only. From level 3, a sideways Solid declares
  Carry or Slip (14+ before).
- **Doors on platforms** (supersedes D-105's plinth). A level's door stands on a fixed solid whose top is its bottom
  and covers its width, at its authored pose and at its retreated pose (`ValidateDoorStands`).
  - L004: `Door_Pad_R` and `Door_Pad_L`, under each pose. Nothing stands under `Roof_5`, which gives way on a touch:
    a long shelf there failed band 1's trap-floor headroom.
  - L009: `Door_Ledge`.
  - L010: `Pad_A` and `Pad_B`, with the retreat lengthened from 3.5 to 5 u so the last jump's arc clears `Pad_B`.
  - L016: `Door_Ledge`.
  - L020: the door no longer backs east into G_9's column (a ledge there would block the eruption ride into it). It
    drops through the Loft onto `Door_Ledge` under the Loft's east end (`Retreat_10` offset (0.4, −3)), and the solution
    rides the eruption and steers west onto the ledge. Dead end D3 proves that a cat dropping off the Loft's end and
    steering back misses the ledge.

- **Route checks for repeating killers** (the developer approved, 2026-10-02). A killer that fired before the shot that
  kills is measured from that shot: its reveal is that shot's fire and its first lethal tick is that shot's
  (`RouteValidator.Lead`, `RecordArrowLethal`). Single-shot traps are unchanged.
- **Provisional until the developer's play-test** (2026-10-02). If any is rejected, the layout is changed, never a rule
  relaxed:
  - L010's camera (D-104 table) goes from zoom 1.8 to 1.2, and PAX-100's Drop_10 sinks at 0.1 u a tick (it dropped at
    0.5). At 1.8, and still at 1.5, the floor just under the cat's feet left the frame within a tick of moving (D-083).
  - L001's shelf is raised to top 1.30 (about +7% under it), the most band 1's reach allows; the shelf–ledge gap gets +10%.

**Why:** the developer's play-test (2026-10-02): "arrows fire only once … fire multiple times at an interval … at
multiple angles … the point is variety and certain amount of challenge"; "add moving platforms and moving floors … throw
these around in levels 3–20"; "exits just hanging in the air doesn't make sense … keep it on a platform." Rulings: several
angles from level 3; every floor pattern in 3–10; real platforms; two fixed pads for L010.

### D-104 amendment · 2026-10-02 · Accepted (the developer, PAX-A16 Phase 2 round 2; accepted after the play-through)
**Decision:** every level (L001–L020) uses L001's camera, 1.8× with the 1.5 u lift, for the developer's play-through ("Use
the same zoom as in level 001 … I want to play through with it with that zoom. I know you said there might be some
issues. But don't fix them yet."). The camera tell (D-083) and chaos (D-093) rules fail in some levels at this zoom (the
Phase 1 sweep found 13 and 9), and so does L010's sinking floor (PAX-100); they are left failing on purpose until the
developer has played. After that the developer rules: keep 1.8× and amend the rules, change the layouts, or return
levels to the measured table (recorded in `LevelCameras.cs`).

**Ruling (2026-10-02, after the play-through):** 1.8× stays on every level, including the traps that set off out of
view at that zoom. The findings accepted with it are pinned in the tests (`LevelZoomAccepted`): 22 camera tell findings
in L004, L005, L008–L012, L014 and L016–L020, and the chaos moment in L014–L017, L019 and L020. Each
tell entry names its level, betrayal and trap, so the rule still fails for any other trap, betrayal or level; a layout
change that clears one removes its entry. The rules themselves (D-083, D-093) are unchanged for new work.
**Superseded in part (2026-10-03):** the D-083 amendment replaced the camera tell; the pins (`LevelZoomAccepted`) are gone.

### D-107 · 2026-10-02 · Accepted (PAX-A16 Phase 2 rounds 2–6; the developer approved the checkpoint)
**Decision:** the environment art rulings of the Phase 2 critic loop, art only (no collider, trigger, route or timing
changes):
- **The cat keeps its true colours.** No level grades the cat or the hazards: the post profile is hue-neutral (no white
  balance, split toning, coloured lift or colour filter; saturation within ±5), Neutral tonemapping, post exposure at most
  +0.15, **no bloom** (its glow off the bright skies veiled the cat: its darkest fifth read 40–45 with bloom, 22–26 without,
  against the sprite's own 21,15,12), 2D light colours capped at HSV saturation 0.15, and the cat's key light 0.9. A
  level's colour lives in its sky, haze and background pieces.
- **The cat's Reality A outline** is a pale warm rim at 15% (`CatA_VisualConfig.outlineColorA` 1, 0.86, 0.62, 0.15), set
  by the Environment Stack setup; the solid orange 1-texel line read as a jagged cut-out at the 1.8× zoom. (The developer
  confirmed the config change.)
- **Removed:** the arch fringe under floating floors (hard-cut ends, ghost pillars under bridges); the waterfalls a
  level's signature added on its own (they ran behind walkable floors; a level's own look still places them); sunbeams at
  night; the overhang trims (edge, bounce, roots) over a room whose ceiling closes its top; the `Overcast` palette and its
  `Silver` sky.
- **Added:** a second, hazier row of the signature under a high walk (open-below rule; L016 and L019 hand-set); the far
  lake only where its waterline sits under every solid's foot; the sun moved a little per level; the rays behind the back
  walls; a moon's halo smaller and cool.
- **Fixed:** the pale strip under every platform (the sky showed between the underside and its drapes), the black line
  across pits (two overlapping shade quads), the navy cast on the outside walls (the shade sprite now tinted neutral), the
  stone column over a side wall where a ceiling meets it.
- Metrics: `catDark` is measured on the cat's own pixels (its sprite's opaque share), not the frame's box.

**Why:** the developer's round-2 rules ("colour grading must never tint the cat or hazards", "no blown-out whites",
"Readability ≥ 7 everywhere"), and six rounds of critics (average 4.62 → 5.44; Readability ≥ 7 in 19 of 20 levels).

### D-083 amendment · 2026-10-03 · Accepted (the developer's ruling)
**Decision:** surprise is allowed. A trap no longer has to be on screen before it fires. The camera rule is now: every
dying betrayal's **killer**, in its lethal pose, is **on screen at the moment of death**, so every death is readable.
- The moment of death is the kill tick (the tick whose room step killed). The camera stands still for the whole death hold
  (D-058, `LevelCameraFollow`), so that frame is what the player sees for the hold.
- The killer is the trap the replay attributes the kill to; it's on screen when its drawn bounds at the kill tick (an
  angled arrow: its turned corners; a trap that has vanished: where it was last drawn) overlap the view. Same camera model
  as before: the game's own `CameraMath.Step` over the recorded cat, 30/60 fps × 4 phases × 3 starting look directions,
  worst case, at 4:3, 16:9 and 20:9; a room in fit mode passes.
- A killer that isn't a drawn element fails with its own message.
- Unchanged: D-097's escape-backed reveal (opt-in, still checked as well), D-057's tells (hidden spikes' reveal delay and
  an arrow's tell ≥ 6 ticks) and D-079/D-080's route lead (the reveal changes ≥ 6 ticks before the kill, camera or not).
- `LevelZoomAccepted` and its 28 pins are removed; `CameraTellTests` runs the rule on all 20 levels.

**Measured (2026-10-03, the killer-at-death rule, deaths failing per camera):**

| Camera | Levels failing | Deaths failing |
|---|---|---|
| 1.8× with the 1.5 u lift (shipped) | 17: L001, L002, L005–L008, L010–L016, L018–L020 | 58 |
| 1.8×, no lift | 8: L004, L008, L012, L014, L016, L018, L019, L020 | 14 |
| 1.5×, no lift | 3: L008, L012, L014 | 3 |
| 1.2×, no lift | 0 | 0 |

Almost every failure is a fall: the 1.5 u lift keeps the view high, and the smoothed camera hasn't followed the cat down
by the kill tick, so the cat and the spikes or pit it dies on are below the frozen frame. L010 fails only at 1.8× with
the lift (T2, onto Spikes_2) and passes at 1.8× without it. The levels are left failing, to be ruled on (the lift, the
zoom, the camera during the hold, or the layouts).

**D-093 (chaos) under the same relaxation, proposed, not changed:** the chaos moment's elements in view at 16:9 (band 2):

| Level | 1.8×/1.5 | 1.8×/0 | 1.5×/0 | 1.2×/0 | every change, on screen or not |
|---|---|---|---|---|---|
| L014 | 4 | 3 | 5 | 5 | 5 |
| L015 | 3 | 3 | 3 | 5 | 6 |
| L016 | 1 | 1 | 2 | 5 | 5 |
| L017 | 4 | 3 | 4 | 5 | 5 |
| L019 | 3 | 4 | 4 | 5 | 7 |
| L020 | 4 | 4 | 5 | 6 | 6 |

(L011–L013 and L018 pass at every camera.) Counting every change in the window, on screen or not, passes all ten band-2
levels; keeping "in view" fails these six at the shipped camera.

**Revised the same day (the developer's rulings, 2026-10-03):** the killer must be on screen **at some point before the
death hold ends**, not only on the kill tick, because the camera now keeps easing during the hold (D-058 amendment). The
validator steps the camera on through `RoomSafetyConfig.HoldTicks` (30) with the cat and the killer frozen in their kill
poses, and passes the death if the killer is in view at any of those ticks, in every camera case. Measured with the 1.5 u
lift kept: **no death fails** in L001–L020 (58 failed at the kill tick alone), so the lift stays and no layout changes.
D-097's escape check is kept as it is.

### D-058 amendment · 2026-10-03 · Accepted (the developer's ruling; play-tested by the developer 2026-10-03)
**Decision:** during the death hold the level camera keeps easing toward the cat (it stood still, PAX-047 §2.2.3). The
room stays frozen exactly as it killed (traps, hazards, `RoomLifeTick`, door and bounds checks, all still gated by
`RoomDeath.IsHolding`); only `LevelCameraFollow.Step` runs on, toward the frozen cat. `LevelCameraFollow.roomDeath` is
still wired by the setup but no longer gates `Step`. Respawn still snaps (`SnapToTarget` on `CatRespawn.Respawned`).
**Why:** at 1.8× with the 1.5 u lift, a fall outran the camera, so the frozen frame often showed neither the cat nor what
killed it (58 deaths in 17 levels). Easing during the hold shows every one of them before the room resets.

### D-093 amendment · 2026-10-03 · Accepted (the developer's ruling)
**Decision:** the chaos moment counts every change, on screen or not: a 60-tick window in which at least 5 elements change
(`ChaosMinElements`), at least 3 of them in view (`ChaosMinInView`, the same 16:9 worst-case view as before). The best
window is the one that passes, then the one with most in view, then the one with most changes.
**Measured (shipped camera, 1.8× with the lift):** L011–L014, L017–L020 pass. Two fail:
- **L016:** 5 changes, 1 in view (t975–t1034: Spikes_S1, Spikes_S4, Block_S3 and Spikes_S5 off screen). Goes to its level
  ticket for a layout fix (the developer's ruling).
- **L015:** no window has both: at best 4 changes with 3 in view (t71–t130: Arrow_3, Mover_M off screen, Collapse_2,
  Cloud), or 6 changes with fewer than 3 in view (t699–t758). Not foreseen when the rule was proposed (the proposal read the
  best in-view count and the best total from different windows). Left failing, for the developer's ruling.

### D-085 amendment · 2026-10-03 · Accepted (the developer's ruling, PAX-102)
**Decision:** a falling block may be a section of a split floor slab, so its fall leaves a real hole. The slab's colliders
are split around the block, with a floor on each side touching it at the same top and bottom; the floor is drawn as one
continuous surface, with no seam, outline or colour change around the block. `ValidateBand1Tells` fails on any visible
seam (an exposed side edge of the skin) at a floor block's edges, and on a block that is neither flush in a solid nor
such a section. A real hole is wide enough for the cat's collider with margin (1.6 u for the 1 u collider); the collider
is never changed. Roof blocks get an art hole instead, shown only after the block has left (`SolidArt`'s socket).
Trigger coverage counts such a block as the slab over its storey until it falls.
**Applied:** L010 Block_1, L008 Block_6 and L002 Block_1–3 (real holes, 1.6 u). L002 follows the developer's play-test
(2026-10-03): "If part of the floor falls, there should be a hole there", superseding the earlier "no holes in L002";
its S1 was reworked to fit (PAX-102 §8).

### D-108 · 2026-10-03 · Accepted (PAX-V08, the developer's rulings)
**Decision:** hazards read on every background. One danger colour, crimson (0.80, 0.06, 0.14; `HazardPalette.Danger`,
`hazard_readable.DANGER`), marks lethal things only: arrow heads and fletching, spike tips, the spear's blade, the launcher's
mouth and its tell glow. Background and environment art never use it (warm light and gold stay amber/yellow; checked by
`test_hazard_readable.py`). Every lethal sprite has a dark outer line and a light inner rim. Lethal bodies (spikes, arrows,
spears, launchers and their slots) draw with URP's unlit sprite material, so level lighting never dims them; effects stay
lit. **The rule:** for every level, every hazard in its lethal pose, at each level-camera pose that shows it (2400 × 1080,
2D lights on), at least 30% of its pixels reach a 3:1 luminance contrast against what's behind them
(`HazardContrastTests`).
**Why:** the developer (2026-10-03): "arrows are super hard to see … make the arrow more visible regardless of the level it
is in"; "Level 004: all arrows and traps are so hard to see. They meld into the background." Measured on the old art: L004's
arrows 3–10% of their pixels at 3:1, L008's and L010's 2–5%.
