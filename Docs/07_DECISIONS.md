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

---

## Open questions (to be resolved by playtest → new D-entries)

- **Q-1** Partner presence hint: shimmer or nothing?
- ~~**Q-2** Camera rotates with gravity, or stays world-aligned?~~ Resolved by D-020: world-aligned.
- **Q-3** Tilt or dial as default?
- **Q-4** Solo gravity adaptation: persistent setting, Echo choreography, or both?
- **Q-5** Echo length and looping.
- **Q-6** Effect of same-room screen peeking.
- ~~**Q-7** Should a temporary gravity-aligned/snap-rotating camera be introduced in Phase 5 as a deliberate disorientation effect while a Control Station actively steers the other cat's gravity?~~ Closed by D-021: not needed.
- ~~**Q-8** Is v1 co-op-only?~~ Closed by D-026: solo stays in v1; Echo and solo as a first-class mode remain in scope.
- **Q-9** Nine lives: shared between both cats or per-cat? Reset per level or per checkpoint? On zero, hard fail or a rating penalty? Note: a hard fail conflicts with Vision pillar 3 (cheap, funny failure, no death screens); resolving Q-9 toward hard fail requires a Vision change. Recorded, not designed.
