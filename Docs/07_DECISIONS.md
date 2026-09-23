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

