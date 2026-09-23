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
