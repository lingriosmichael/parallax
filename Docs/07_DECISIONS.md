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

---

## Open questions (to be resolved by playtest → new D-entries)

- **Q-1** Partner presence hint: shimmer or nothing?
- **Q-2** Camera rotates with gravity, or stays world-aligned?
- **Q-3** Tilt or dial as default?
- **Q-4** Solo gravity adaptation: persistent setting, Echo choreography, or both?
- **Q-5** Echo length and looping.
- **Q-6** Effect of same-room screen peeking.
