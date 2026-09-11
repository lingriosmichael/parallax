# PARALLAX — Detailed Implementation Plan (v2)

**Status:** Authoritative plan. Replaces v1 of this document in full.
**Date:** 2026-09-10

---

## 0. How to read this document

### 0.1 Document precedence

When two documents disagree, the higher one wins:

1. `Docs/07_DECISIONS.md`: dated decision log. The newest entry wins.
2. `Docs/00_VISION.md`: what the game is.
3. `Docs/02_ARCHITECTURE.md`: how the game is built.
4. `CLAUDE.md`: rules for AI implementation agents.
5. **This plan**: order of work, tickets, gates.
6. The original PARALLAX specification (PDF): reference only. Superseded wherever it mentions 3D, humanoids, photo-avatars, or anything contradicting the above.

If you find a contradiction, don't "just pick one." Add a decision to `07_DECISIONS.md` and fix the lower document.

### 0.2 What changed from v1

| # | Change | Why |
|---|---|---|
| 1 | **Spatial model defined.** Each Observer has its own reality with its own collision geometry. Realities are linked only through semantic anchors. Cats do not touch or see each other by default. | v1 never said this, and it was internally contradictory ("each sees two cats" vs. vine/elevator puzzles vs. "same collision skeleton"). |
| 2 | **Per-cat gravity from the first motor ticket.** Global `Physics2D.gravity` is never used for cats. | Global gravity cannot give A and B different gravity. Retrofitting it later would mean rewriting the motor. |
| 3 | **Echo Replay is state-based** (recorded positions + semantic event timeline, played back kinematically), not input replay. | Input replay through physics diverges as soon as the world differs slightly. |
| 4 | **Explicit authority rules.** Each cat is owned by its controller. Control streams are owned by the controller. Shared anchors and puzzle state are owned by the session authority. | v1 left anchor ownership undefined in Fusion Shared Mode, which is a recipe for desync. |
| 5 | **Tilt has a touch-dial alternative** behind the same interface. Orientation is locked to Landscape Left. The controlling cat sits at a **Control Station** while steering gravity. | Tilting a phone you are also using for touch controls is awkward. Not every phone has a gyroscope. Accessibility. |
| 6 | **Ticket order and phase order now match.** The plan proves everything locally on one phone before Photon. | v1's phases said Photon first while its ticket list said local first. |
| 7 | **Assembly definitions enforce** "gameplay never depends on Photon." | Makes the most important architecture rule a compiler error instead of a hope. |
| 8 | **Each ticket separates Claude (code) from You (Unity Editor wiring).** | Claude Code edits text files well. Hand-editing Unity scenes/prefabs as YAML is error-prone. |
| 9 | **Day-1 repo hygiene:** Force Text serialization, visible meta files, Unity `.gitignore`, Git LFS. | Must exist before the first art file lands. |
| 10 | **Two-client Editor testing, a debug panel, and a throwaway Photon spike** were added. | Faster iteration, and early de-risking of the scariest dependency. |
| 11 | **AI team simplified** to Architect / Implementer / Reviewer. | Five named agents is coordination overhead for one developer. |
| 12 | **Time expectations, a separate solo validation gate, same-room playtest guidance, and a realistic in-game art target** were added. | Prevents discouragement and false-positive playtests. |
| 13 | Section numbering fixed. Tickets renumbered **PAX-001 → PAX-035**. | v1 had duplicate/missing section numbers. |

---

## 1. Frozen decisions

| Decision | Prototype decision |
|---|---|
| Engine | **Unity 6 LTS** (latest 6.x LTS available when you start) |
| Rendering | **URP 2D Renderer** |
| Gameplay | **2D physics, 2D gameplay** |
| Presentation | **2.5D illusion:** layered sprites, 2D lights, shaders, parallax |
| First platform | **Android** (IL2CPP, ARM64) |
| Later platform | iOS, only after the vertical slice succeeds |
| Orientation | **Landscape Left, locked** (no auto-rotation) |
| Logical Observers | **Exactly 2: Observer A and Observer B** |
| Human players | **1–2** |
| Solo mode | **Reality switching + Echo Replay** |
| Characters | **Cats** |
| Spatial model | **Separate geometry per reality, linked only by semantic anchors** |
| Gravity | **Per-cat custom gravity vector** |
| Echo | **State-based recording, kinematic playback** |
| Networking | **Photon Fusion 2, Shared Mode** |
| Core architecture | **Semantic state synchronization** behind a transport interface |
| Authority | Cat = its controller · control stream = its controller · anchors/puzzle state = session authority |
| First local proof | **PAX-024:** two realities + cross-reality anchor + Echo on one phone |
| First network proof | **PAX-035:** Phone A's gravity control changes Phone B's gravity |
| Gravity input | **Tilt and touch dial**, both behind the same interface |
| Voice | External (Discord/phone) during prototype |
| Matchmaking | Invite/code only |
| Monetization | None during prototype |
| Art | Greybox first → Sorceress only after Gate 4 |

Unity's URP 2D Renderer supports 2D lights, sprite normal maps, sprite masks, and 2D shadow casting. That is enough to evoke the illustrated lighting of the key art without 3D environments. Photon Fusion 2 Shared Mode gives each client authority over its own objects and avoids Host Mode's prediction/rollback complexity, which suits this project.

---

## 2. Changes from the original PARALLAX specification

### Keep

- "Your friend controls your reality."
- Two contradictory realities.
- Exactly two logical Observers, whether 1 or 2 humans are playing.
- Solo Reality Switching and Echo Replay as a first-class mode.
- One shared semantic world.
- Cross-phone causality.
- Gravity, scale, and perspective manipulation.
- Reality anchors.
- Cheap, fast failure.
- Communication as gameplay.
- Networking events and state rather than visual effects.
- Synchronized spectacle using shared ticks and seeds.
- Free prologue + premium/friend-pass concept, eventually.

### Replace

| Original | v1 prototype |
|---|---|
| 3D humanoids | 2D cats |
| Full 3D environments | Layered 2D/2.5D environments |
| Photo-to-3D-avatar cloud pipeline | Simple customizable cats |
| Expensive rigging | Small reusable cat animation set |
| 3D mesh portals | Sprite masks / shaders / render textures where necessary |
| (unspecified) shared space | Separate geometry per reality, linked by anchors |

### Defer until the vertical slice passes Gate 5

Built-in voice chat, iOS, payments, friend-pass entitlement, random matchmaking, deep linking, AI avatar generation, cloud saves, cosmetics, achievements, chapters beyond the slice, trailers, store pages, localization.

---

## 3. Tool stack

| Purpose | Tool | When |
|---|---|---|
| Engine | Unity 6 LTS via Unity Hub | Day 1 |
| Android toolchain | Unity Android Build Support + SDK/NDK/OpenJDK (installed via Unity Hub, not manually) | Day 1 |
| Input | Unity Input System package (default in Unity 6 projects) | Day 1 |
| Tests | Unity Test Framework (EditMode tests for pure logic) | From PAX-020 |
| Source control | Git + GitHub (+ GitHub Desktop if you prefer a GUI) | Day 1 |
| Large files | **Git LFS** | Day 1, before any art |
| Networking | Photon Fusion 2 (Shared Mode) | Spike anytime after Gate 1; integration at PAX-028 |
| Two-client testing | Unity **Multiplayer Play Mode** or Fusion's multi-peer mode | PAX-030 |
| Coding AI | Claude Code | Day 1 |
| Architecture/review AI | Strongest available Claude model (e.g. Fable 5.1 or Opus 5) in chat | As needed |
| Art generation | Sorceress | **After Gate 4** |
| Image cleanup | Krita / Affinity Photo / Photoshop | Art phase |
| Audio | Unity Audio (FMOD only much later, if justified) | — |
| Profiling | Unity Profiler | From vertical slice |
| Device logs | Logcat (Unity Logcat package or `adb logcat`) | From first phone build |
| Tracking | GitHub Issues + Projects | Day 1 |
| Real testing | **Two physical Android phones** | Essential from PAX-035 |

---

## 4. Working with Claude

### 4.1 Three roles, not five agents

| Role | Where | Does | Does not |
|---|---|---|---|
| **Architect** | Chat with the strongest available model | Writes tickets, owns `02_ARCHITECTURE.md` and `07_DECISIONS.md`, reviews milestones, diagnoses architectural problems, guards scope | Write routine code |
| **Implementer** | Claude Code in the repo | Implements **one** ticket, within its allowed files | Invent features, touch files outside the ticket, install packages |
| **Reviewer** | A **fresh** Claude session (no memory of implementing it) | Adversarially reviews diffs for networking- and state-changing tickets | Rewrite the feature |

The Reviewer asks questions like these:

- What if this event arrives twice? Late? Out of order?
- What happens at 220 ms round-trip time?
- What if Android backgrounds the app mid-action?
- What if the sensor is noisy, or missing?
- What if B loads five seconds after A?
- What happens after reconnect, or if the session authority leaves?
- What if the Echo replays a request that is no longer valid?

**The art lane** is separate. Claude writes asset manifests, prompts, and style/consistency rules. Sorceress generates. Claude checks the results against technical requirements (sizes, pivots, silhouettes, collider compatibility). Code and art lanes meet only inside Unity.

### 4.2 Code vs. Editor: who does what

Claude Code works on text files. Unity scenes (`.unity`), prefabs (`.prefab`), and most `.asset` files are serialized YAML that is fragile to hand-edit.

- **Claude does:** `.cs`, `.asmdef`, `.md`, `.json`, test files, and **Editor scripts** (menu items that create repetitive scene/prefab setup deterministically).
- **You do:** dragging components onto GameObjects, wiring Inspector references, building prefabs, configuring Project Settings, making builds.
- Every ticket lists both sections explicitly (see §7).
- Prefer code-driven setup where it reduces manual wiring: `[RequireComponent]`, sensible defaults, ScriptableObject configs, and auto-finding components on the same GameObject.

### 4.3 You are the real safety net

AI will occasionally produce plausible-looking wrong code. Your defense is understanding it.

**Rule:** you do not commit a script you cannot explain in two sentences. If you can't, ask Claude to explain it until you can. This is part of the Definition of Done.

### 4.4 No swarms

Never run multiple implementation agents on the Unity project at once. Scenes, prefabs, settings, and `.meta` GUIDs merge badly.

**Loop:** Architect → one ticket → Implementer → compile/test → Reviewer (if needed) → you verify → commit → next ticket.

---

## 5. Repository structure and project settings

### 5.1 Structure

```text
PARALLAX/
├── Assets/
│   ├── _Game/
│   │   ├── Core/                  Parallax.Core.asmdef       (ids, events, anchors, echo data, math; no MonoBehaviours, no Photon)
│   │   ├── Gameplay/              Parallax.Gameplay.asmdef   (MonoBehaviours; no Photon)
│   │   │   ├── Observers/
│   │   │   ├── Player/            (CatMotor2D, GravityReceiver, animation hooks)
│   │   │   ├── Input/             (touch, tilt, dial → commands)
│   │   │   ├── Reality/           (RealityRoot, presenters, manifestations)
│   │   │   ├── Anchors/
│   │   │   ├── Echo/
│   │   │   ├── Transport/         (IRealityTransport, LocalTransport)
│   │   │   ├── Puzzles/
│   │   │   ├── Checkpoints/
│   │   │   ├── Camera/
│   │   │   ├── UI/
│   │   │   ├── Audio/
│   │   │   └── Effects/
│   │   ├── Net/Fusion/            Parallax.Net.Fusion.asmdef (the ONLY assembly referencing Photon)
│   │   ├── App/                   Parallax.App.asmdef        (composition root: picks LocalTransport or FusionTransport)
│   │   ├── DebugTools/            Parallax.DebugTools.asmdef (dev builds only)
│   │   ├── Editor/                Parallax.Editor.asmdef     (editor-only setup scripts)
│   │   ├── Tests/EditMode/        Parallax.Tests.EditMode.asmdef
│   │   ├── Art/
│   │   │   ├── Characters/
│   │   │   ├── RealityA/
│   │   │   ├── RealityB/
│   │   │   └── Shared/
│   │   ├── Scenes/
│   │   │   ├── Bootstrap/
│   │   │   ├── Sandbox/
│   │   │   └── VerticalSlice/
│   │   └── Data/                  (ScriptableObject configs)
│   └── ThirdParty/                (Photon and other imports)
├── Docs/
│   ├── 00_VISION.md
│   ├── 01_SCOPE.md
│   ├── 02_ARCHITECTURE.md
│   ├── 03_ART_DIRECTION.md
│   ├── 04_NETWORKING.md
│   ├── 05_TEST_PLAN.md
│   ├── 06_ASSET_MANIFEST.md
│   ├── 07_DECISIONS.md
│   ├── Art/keyart_north_star.png
│   └── TASKS/                     (one file per ticket: PAX-001.md …)
├── CLAUDE.md
├── README.md
├── .gitignore                     (GitHub's standard Unity template)
└── .gitattributes                 (Git LFS patterns)
```

**Assembly dependency rule:**
`Core` ← `Gameplay` ← `Net.Fusion` ← `App`. `DebugTools` and `Tests` may reference what they need.
**`Core` and `Gameplay` must never reference Photon.** The compiler enforces this.

### 5.2 Day-1 settings

- **Project Settings → Editor:** Asset Serialization = **Force Text**, Version Control = **Visible Meta Files**. These are the Unity 6 defaults, but verify them.
- **`.gitignore`:** GitHub's official Unity template.
- **`.gitattributes`:** LFS tracking for `*.png *.jpg *.jpeg *.psd *.kra *.tga *.wav *.ogg *.mp3 *.fbx *.ttf *.otf *.mp4`. Run `git lfs install` once.
- **LFS quota:** check GitHub's current LFS storage/bandwidth quotas. Art-heavy repos can exceed the free allowance later.
- **Optional:** configure UnityYAMLMerge ("Smart Merge") as the merge tool for `.unity`/`.prefab`.

---

## 6. `CLAUDE.md`

The authoritative `CLAUDE.md` is a separate file at the repo root. It is not duplicated here, so the two cannot drift apart. Its key rules:

- One approved ticket at a time. Touch only allowed files. No unrequested features. No new packages without approval.
- Never hand-edit `.unity`, `.prefab`, `.meta`, or `ProjectSettings` unless the ticket explicitly allows it.
- `Core`/`Gameplay` never reference Photon.
- Exactly two Observers. An Observer is not a player or a network peer.
- Separate geometry per reality. Cats never physically interact.
- Per-cat gravity. Never use or change `Physics2D.gravity` for cats.
- Echo is state-based and kinematic.
- Authority rules as in §1.
- Sync semantic state, never presentation.
- Report changed files, test steps, and limitations. Never claim device testing that didn't happen.

---

## 7. Ticket format

Every ticket lives in `Docs/TASKS/PAX-XXX.md`:

```text
TASK: PAX-XXX
TITLE:
PHASE / GATE:

OBJECTIVE
One or two sentences.

CONTEXT
Relevant sections of 02_ARCHITECTURE.md.

CLAUDE — CODE
Files to create/modify (allowed list). What to implement.

YOU — UNITY EDITOR
Exact wiring steps (components, references, layers, settings, build).

DO NOT
Explicit forbidden scope.

REQUIREMENTS
Bulleted, testable.

ACCEPTANCE TEST
Numbered manual steps (+ EditMode tests where logic is pure).
Specify: Editor only / Editor + device / two devices.

DELIVERABLE (from Claude)
Changed files · explanation · test steps · known limitations.

DEFINITION OF DONE
Universal DoD (§8) + ticket-specific items.
```

### Worked example

```text
TASK: PAX-007
TITLE: GravityReceiver + gravity-relative horizontal movement
PHASE / GATE: Phase 2 / Gate 2

OBJECTIVE
The placeholder cat moves left/right relative to its own gravity vector,
using a custom per-cat gravity instead of Physics2D.gravity.

CONTEXT
02_ARCHITECTURE.md §6 (Cat motor and per-cat gravity).

CLAUDE — CODE
Create: Assets/_Game/Gameplay/Player/GravityReceiver.cs
Create: Assets/_Game/Gameplay/Player/CatMotor2D.cs
Create: Assets/_Game/Core/Input/CatCommand.cs
Create (temporary): Assets/_Game/Gameplay/Input/KeyboardCatInput.cs

YOU — UNITY EDITOR
1. Open Sandbox_Player.
2. On Cat_Player: add GravityReceiver and CatMotor2D.
3. Confirm Rigidbody2D Gravity Scale shows 0 at runtime (the motor sets it).
4. Add KeyboardCatInput to Cat_Player.

DO NOT
Add jumping, ground detection, networking, animation, touch input.
Use or modify Physics2D.gravity. Modify ProjectSettings.

REQUIREMENTS
- Rigidbody2D.gravityScale forced to 0 by the motor.
- Gravity applied by the motor from GravityReceiver.Direction * Strength.
- Horizontal movement computed along the axis perpendicular to gravity.
- Tunable max speed and acceleration.
- Sprite flips based on movement direction.
- Uses Unity 6 API (Rigidbody2D.linearVelocity).

ACCEPTANCE TEST (Editor only)
1. Press Play. The cat falls onto the ground.
2. A/D moves the cat; releasing stops it smoothly.
3. The cat cannot pass through the ground.
4. Stop Play. Set GravityReceiver's Initial Direction to (1,0), press Play:
   the cat falls to the right. Reset it to (0,-1) afterwards. (Body alignment
   and full any-direction movement are completed in PAX-010.)
5. No Console errors.

DEFINITION OF DONE
Universal DoD + you can explain in two sentences why gravityScale is 0.
```

---

## 8. Universal Definition of Done

Nothing is done because Claude says it is done. For every engineering ticket, done means all of the following:

1. Unity compiles.
2. No new unexplained Console errors or warnings.
3. All acceptance tests pass, including EditMode tests where applicable.
4. Device test passes if the ticket involves mobile behavior. Editor-only testing is labeled as such.
5. Changed files are listed. No unrelated files are modified.
6. No `.meta` files were deleted or regenerated unexpectedly.
7. Reviewer pass is complete for any networking or shared-state ticket.
8. Docs are updated if architecture changed (plus a `07_DECISIONS.md` entry).
9. **You can explain each changed script in two sentences.**
10. **You personally reproduced the primary behavior once.**
11. A Git commit exists, with the ticket ID in the message.

---

## 9. Core model in one page

Full detail lives in `02_ARCHITECTURE.md`. This summary exists so the plan makes sense on its own.

**Observers.** Two logical Observers, A and B. Each has a cat, a reality, a camera, logical state, and an **input source**: `LocalHuman`, `RemoteHuman`, `EchoReplay`, or `Inactive`. Gameplay never asks "is B on another phone?"

**Spatial model.** Each reality is a `RealityRoot` with its own geometry, physics layer, sorting layers, lights, and camera. Reality B's root sits at a large fixed world offset from Reality A's. **Corresponding positions share the same local coordinates** under their roots. Cat A only ever collides with Reality A. The cats never meet physically. Both realities are loaded on every device; each device renders only the Observer(s) it controls.

**Per-cat gravity.** `GravityReceiver` holds each cat's gravity direction. `CatMotor2D` moves, jumps, and detects ground relative to that direction. `Physics2D.gravity` is irrelevant to cats.

**Semantic state.**
- *Anchors* hold logical values/targets (e.g. `VerticalPathway01 = 1`). Presenters in each reality turn that into a vine or an elevator and animate locally.
- *Control streams* carry continuous values from a controller (e.g. gravity angle). Latest value wins.
- *Cues* start synchronized cosmetic spectacle at a shared tick with a shared seed.

**Authority.**

| State | Owner |
|---|---|
| Cat A / Cat B body | The client controlling that Observer |
| Control stream from Observer X | The client controlling X |
| Shared anchors, puzzle phase, checkpoints | Session authority (Photon master client in co-op, the local device in solo) |

Requests go up, state comes down. Requests use absolute values and carry an event ID, so duplicates are harmless.

**Transport.** Gameplay talks to `IRealityTransport`. `LocalTransport` (solo/offline, with optional artificial latency) and `FusionTransport` (co-op) implement it. Puzzles never change when the transport changes.

**Echo.** Records the cat's state every fixed tick, plus the semantic requests and control samples it issued. It plays back kinematically and re-issues those requests at the recorded times. When playback ends, it holds its final pose and any held interaction.

---

## 10. Solo / Echo design

Co-op is the **flagship**: two people describing contradictory realities to each other. Solo is a **first-class mode** built on the same systems.

### 10.1 Modes

```text
CO-OP   Human 1 → Observer A (LocalHuman on phone 1, RemoteHuman on phone 2)
        Human 2 → Observer B (LocalHuman on phone 2, RemoteHuman on phone 1)

SOLO    One human → controls A or B, switches between them
        The uncontrolled Observer is Inactive or EchoReplay
```

Only one reality is on screen at a time. **No permanent split-screen.** The switch eventually becomes a fracture/shard transition. In the prototype it is instant.

### 10.2 Echo flow (v1)

```text
Controlling Cat A
  → press RECORD (max 10 s, tunable)
  → act (pull vine, stand on plate, run a route, steer gravity…)
  → press SWITCH
      recording stops · A becomes EchoReplay · you now control B
      A's Echo replays from the start of the recording
  → Echo ends → holds final pose and any held interaction
  → switching back to A cancels the Echo; you resume from where the Echo is
```

- One Echo at a time in v1.
- SWITCH without RECORD makes the previous Observer `Inactive`. It stands still, but gravity still applies.
- You can't see your Echo while controlling the other cat, because it is in the other reality. You see its **effects**. A small HUD timeline shows Echo progress.
- The Echo is a ghost of your past. It follows its recorded path even if the world has changed. Levels designed for Echo keep Echo-relevant geometry static or anchor-driven.

### 10.3 Puzzle compatibility tags

Every puzzle is tagged: `COOP: Native / SOLO: Native`, `COOP: Native / SOLO: Adapted`, or `COOP: Exclusive`. Communication puzzles that become trivial when one person sees both realities need a solo adaptation (memory constraint, Echo timing) or stay co-op-exclusive.

### 10.4 Three solo puzzle types

- **Type A, State:** A pulls a lever → `Anchor_17 = 1` → switch to B → the elevator is raised. No Echo needed.
- **Type B, Sustained:** A stands on a plate → record → switch → A's Echo holds the plate → B crosses.
- **Type C, Synchronization:** record A's sequence (including a gravity-control choreography) → switch → play B in sync with the replay.

### 10.5 Solo gravity (adapted)

Gravity control targets the *other* reality, which a solo player can't see while steering. Prototype two adaptations and playtest both:

1. **Discrete state:** at A's Control Station, set a persistent gravity direction (Type A), then switch and traverse as B.
2. **Choreography:** record A steering gravity as an Echo (the control stream is recorded), then switch and survive the replayed gravity as B (Type C).

### 10.6 Solo architecture proof (checked at Gate 3)

1. One phone contains Observer A and Observer B.
2. The player switches control between them.
3. Each Observer renders a different reality.
4. An action by A changes B's world via an anchor.
5. After switching to B, the change is visible and correct.
6. A short Echo records and replays identically every time.
7. Everything runs with `LocalTransport`. No Photon code is present in `Gameplay`.

---

## 11. Phase 0 — Learn Unity through PARALLAX (1–2 weeks)

Goal: understand just enough Unity to supervise AI. No months-long courses. Use a throwaway `Sandbox_Learning` scene.

| Task | Do | Done when |
|---|---|---|
| L-1 | Scene, GameObject, Component: sprite + BoxCollider2D + Rigidbody2D + camera | You can explain GameObject vs. Component and find both in the Inspector |
| L-2 | Prefabs: turn the square into `Cat_Player.prefab` | You can delete it from the scene, drag it back, and it works |
| L-3 | Scripts: `HelloParallax.cs` logs `PARALLAX BOOT OK` | You know where scripts attach and can read a Console error |
| L-4 | Git: two commits, then deliberately break something and revert it | You can undo an accidental change confidently |
| L-5 | Read a script Claude wrote and explain it back to Claude | Claude confirms your explanation is correct |

---

## 12. Phase 1 — Foundation and empty Android build (1–2 weeks) → Gate 1

**No networking, no cat, no Sorceress.**

| Ticket | Summary |
|---|---|
| **PAX-001** | Create the project from Unity 6 LTS's **Universal 2D** template. Verify Force Text + Visible Meta Files. Confirm the Input System is the active input handling. |
| **PAX-002** | Git repo, GitHub remote, Unity `.gitignore`, `.gitattributes` with LFS, `git lfs install`, first commit. |
| **PAX-003** | Folder structure (§5.1), assembly definitions with the dependency rule, `Docs/` with `00_VISION.md`, `02_ARCHITECTURE.md`, `07_DECISIONS.md`, and `CLAUDE.md`. |
| **PAX-004** | Android settings: default orientation **Landscape Left**, auto-rotation off, **IL2CPP**, **ARM64** (some recent phones can't run 32-bit apps), minimum API level chosen and recorded, package name, version 0.0.1. Enable developer options + USB debugging on the phone. |
| **PAX-005** | `Bootstrap` scene showing "PARALLAX PROTOTYPE · Build 0.0.1". Build and Run to the phone. |

### Gate 1 — Android

The app launches on your **physical phone**, stays in landscape, shows the build label, and can be quit and relaunched. Logcat shows Unity output.
**No → fix setup. Do not proceed.**

---

## 13. Phase 2 — One ugly cat (2–4 weeks) → Gate 2

A white rectangle with two triangle ears is fine.

| Ticket | Summary |
|---|---|
| **PAX-006** | `Cat_Player` placeholder prefab (Rigidbody2D, CapsuleCollider2D, sprite child) and `Sandbox_Player` scene with BoxCollider2D platforms. |
| **PAX-007** | `GravityReceiver` + `CatMotor2D`: gravity-relative horizontal movement; `gravityScale = 0`; custom gravity applied by the motor. (Worked example in §7.) |
| **PAX-008** | Jump along `-gravity`. Tunable jump height, **coyote time**, and **jump buffering**. These two small features matter a lot for touch controls. |
| **PAX-009** | Ground detection by casting the cat's own collider along the gravity direction (so it never hits itself), with a surface-normal check. |
| **PAX-010** | **Gravity debug control:** a key rotates this cat's gravity by 90° steps. The cat smoothly aligns its body to the new "down" and movement/jump/grounding all work in every direction. |
| **PAX-011** | Fall/reset volume + spawn point. Falling into the abyss resets the cat instantly. |
| **PAX-012** | `CatCameraFollow`: simple follow with dead zone. World-aligned (does not rotate with gravity yet; see Open Questions in `00_VISION.md`). |
| **PAX-013** | Touch input via the Input System: left, right, jump/interact. Produces the same `CatCommand` as the keyboard. No elaborate virtual gamepad. |

**PAX-S01 — Photon spike (throwaway, timeboxed to one evening, separate Unity project).** Do it anytime after Gate 1 and before PAX-028. Never merge it into the main repo. Answer these questions and record the answers in `04_NETWORKING.md`:

1. Can two Editor instances join the same Shared Mode session by name?
2. Does `NetworkTransform` on a client-owned `Rigidbody2D` cat look acceptable?
3. How is an RPC sent to the **state authority** of a shared object?
4. How do master-client-owned objects behave when the master client leaves? (Fusion 2 has a master-client object option; verify its current behavior in the docs.)
5. Does gameplay in plain `FixedUpdate` coexist cleanly with Fusion in Shared Mode for our needs?

### Gate 2 — Movement

On a real phone, the cat moves, jumps, lands, falls, resets, and works in all four gravity directions (debug). Controls feel responsive. Five minutes of play without errors.
**No → fix input/feel. Do not proceed.**

---

## 14. Phase 3 — Two Observers, locally (2–3 weeks)

| Ticket | Summary |
|---|---|
| **PAX-014** | `ObserverId` (A, B, `Other()`), `ObserverContext`, `IObserverDriver`, `InputSourceKind`. Implement `LocalHumanDriver` and `InactiveDriver`; stub `EchoReplay` and `RemoteHuman`. |
| **PAX-015** | **Spatial model:** `RealityRoot` for A and B, B at a fixed world offset. Physics layers `RealityA`/`RealityB` with cross-collision disabled. Correspondence helper (same local coordinates). Spawn Cat A and Cat B in their own realities. |
| **PAX-016** | One camera per Observer with culling masks. Per-reality **sorting layers** (`A_Background … A_Foreground`, `B_…`) so each reality's Light2Ds, including global lights, only affect their own reality. Warm placeholder global light in A, cold in B. |
| **PAX-017** | Solo **SWITCH**: instant camera swap + driver swap. On-screen switch button. |
| **PAX-018** | **Debug panel v1** (dev builds only): current Observer, both drivers, both cats' gravity, picture-in-picture of the other reality, FPS. |

**Done when:** on one phone you can switch between two cats in two visibly different placeholder realities, and each cat only collides with its own world.

---

## 15. Phase 4 — Semantic core and Echo (3–5 weeks) → Gate 3

| Ticket | Summary |
|---|---|
| **PAX-019** | `RealityEvent` types, `AnchorRequest`, `ControlSample`, `SpectacleCue`, `IRealityTransport`, and `LocalTransport` with an **artificial latency** setting (0–400 ms) exposed in the debug panel. |
| **PAX-020** | `AnchorId`, `AnchorState`, `AnchorRegistry` with authority commit, revision numbers, and duplicate-request protection. **EditMode tests** for idempotency and revision ordering. |
| **PAX-021** | `RealityPresenter` / `RealityManifestation`: one anchor, two manifestations. Placeholder vine in A, placeholder elevator (with collider) in B. Presenters animate locally toward the committed value. |
| **PAX-022** | Cross-reality interaction: Cat A interacts with the vine → request → commit → B's elevator rises. Switch to B: the elevator is raised and rideable. Works with 300 ms artificial latency. |
| **PAX-023** | Echo **recording** (state-based): per-tick frames + issued requests + control samples, 10 s cap. EditMode tests for frame indexing and event timing. |
| **PAX-024** | Echo **playback**: `EchoReplayDriver` (kinematic), RECORD button, HUD Echo timeline, hold-final-state rule. A **pressure plate using an overlap query** (so live and Echo cats both trigger it) opens a gate in B. |

### Gate 3 — Local Reality Proof

All seven checks in §10.6 pass on a **physical phone**. The Reviewer has reviewed PAX-019 through PAX-024.
If the architecture feels messy here, **fix it now**. Networking will amplify every flaw.

---

## 16. Phase 5 — Gravity control, locally (1–3 weeks)

| Ticket | Summary |
|---|---|
| **PAX-025** | **Tilt input:** use the Input System's `GravitySensor` when available, falling back to a low-pass-filtered `Accelerometer` (no gyroscope required). Enable sensors explicitly. Compute the roll angle in the screen plane. Calibrate neutral on entering a Control Station. Dead zone, clamp, smoothing. Debug readout. |
| **PAX-026** | **Touch dial:** an on-screen rotary control. Both tilt and dial implement `IGravityControlInput` and output the same normalized value. Selectable in settings/debug. |
| **PAX-027** | **Control Station + control stream:** Cat A sits at a station (movement locked, hands free). Its control value is published as a `ControlSample` targeting Observer B's `GravityReceiver`. **Immediate local feedback** for the controller (station glow/pulse), plus a theatrical "energy in transit" effect. Test at 0 / 150 / 300 ms artificial latency. Echo records the control stream. |

**Done when:** in solo on one phone, A steers B's gravity with tilt *or* dial. After switching to B, the gravity is in effect, and a recorded gravity choreography replays onto B.

---

## 17. Phase 6 — Networking (3–6 weeks) → Gate 4

| Ticket | Summary |
|---|---|
| **PAX-028** | Import Photon Fusion 2 into `ThirdParty`. `Parallax.Net.Fusion` assembly. App ID stored in Fusion's config asset, not in code. `NetworkBootstrap`. Confirm `Gameplay` still compiles without referencing Photon. |
| **PAX-029** | `NetworkSessionService`: **CREATE ROOM / JOIN ROOM (code)**. Stable roles: creator = Observer A, joiner = B. The role is remembered for reconnect. |
| **PAX-030** | Two-client Editor testing via Multiplayer Play Mode or Fusion multi-peer mode, whichever the spike showed works better. Documented in `04_NETWORKING.md`. |
| **PAX-031** | `FusionTransport : IRealityTransport`. `App` picks Local or Fusion transport. Observers are bound to `LocalHuman`/`RemoteHuman` per device. Each device renders only its own Observer. |
| **PAX-032** | Networked cats: each client owns its cat. `NetworkTransform`. The remote cat is visible in the debug PiP only. |
| **PAX-033** | Networked anchors: a master-client-owned `NetworkObject` holds anchor states. Non-authority clients send request RPCs. Commits replicate. Event-ID dedupe. Vine/elevator works across two clients. |
| **PAX-034** | Networked control stream: the controller owns and publishes its value (~20 Hz), and the receiver smooths it. RTT shown in the debug panel. |
| **PAX-035** | **THE NETWORK MAGIC TEST** (below). |

### Gate 4 — Network Magic (PAX-035)

- Phone A on Wi-Fi, Phone B on cellular (or a different network).
- A enters a Control Station and tilts (then repeat with the dial).
- A sees instant local feedback. B's gravity visibly changes.
- Roles reverse, and it works both ways.
- Ten minutes stable, then five disconnect/reconnect cycles without crashes.
- Players perceive it as responsive enough to understand: **"I did that to you."**

**No → stop and fix the mechanic or networking.** Don't buy assets to distract from a weak mechanic.
**Yes → buy Sorceress now.**

---

## 18. Phase 7 — Art pipeline test and visual language

The goal here is to prove the pipeline, not to produce the game's art. Full art production waits for Gate 5.

### 18.1 Realistic in-game target

The key art (`Docs/Art/keyart_north_star.png`) is a marketing composition with painterly depth that is hard to reproduce as sprite layers at phone scale. The in-game style should **evoke** it: fewer layers, simpler shapes, strong silhouettes, and readable contrast. The split-screen composition itself is reserved for the finale convergence, the store page, and trailers.

### 18.2 Visual language

**Reality A (warm, organic):** sandstone, roots, ruins, cloth banners, vegetation, floating architecture, clouds, warm light, dark reflective water.
**Reality B (cold, geometric):** obsidian, glass, grids, cyan edge lines, void, monoliths, wireframes, cold mist, stars.
**Shared:** both cats have identical gameplay dimensions and colliders regardless of visual treatment.

### 18.3 Asset rule: artwork never decides collision

```text
Beautiful ledge sprite     → visual only
Invisible BoxCollider2D    → the actual gameplay surface
```

### 18.4 Sorceress workflow (per environment)

1. **Concept:** one whole-screen image, for art direction only.
2. **Layer breakdown:** `BG_00_Sky, BG_01_FarRuins, MG_01_Arches, GAME_01_Platform, GAME_02_Anchor, FG_01_Roots, FX_01_Fog …`
3. **Generate individual assets**, never screenshots of a "finished" level.
4. **Clean up** in Krita/Affinity/Photoshop: pivots, edges, consistent scale.
5. **Import** into `Art/RealityA` or `Art/RealityB` (LFS-tracked).
6. **Sprite Atlases** per reality.
7. **Parallax depths:** Sky 0.05 · Far 0.15 · Mid 0.35 · Gameplay 1.00 · Foreground 1.20.
8. **Light2D:** A = soft warm global light + highlights. B = dim global light + cyan emissive accents. Each targets its own sorting layers.

### 18.5 Cat asset manifest (v1)

`Idle, Walk, Run, Jump, Fall, Land, Interact_Paw, Surprised, Celebrate`, plus `Sit_ControlStation` and a `Scramble` reaction for gravity shifts. Mostly side-on.
Later: hang, push, slide, sleep, look up, scared, portal transition.

**Cat DoD:** reads at phone scale · distinctive silhouette · works on light and dark backgrounds · consistent animation · collider stable across frames · flippable · looks good when rotated to any gravity direction.

**Pipeline test DoD:** one cat + one small platform kit per reality is imported, atlased, lit, running at target FPS on the phone, and swapped in without changing any gameplay code or colliders.

---

## 19. Phase 8 — Contradictory-world puzzle

- **A sees** an ancient stone ruin with a vine.
- **B sees** a dark laboratory with an elevator.
- **Shared anchor:** `VerticalPathway01`.
- A pulls the vine → B: *"Something moved."* → the elevator rises → B rides it and hits a switch → A's blocking root retracts → A continues.
- Tags: `COOP: Native / SOLO: Native (Type A or B)`.

**DoD:** without seeing each other's screens, two testers discover they must communicate. The puzzle requires **qualitative language**, not "move 43 pixels left."

---

## 20. Phase 9 — Checkpoints and cheap failure

`CheckpointId`, `CheckpointManager`, `CheckpointSnapshot`. A snapshot stores only meaningful state: checkpoint ID, spawn points per Observer, anchor states, puzzle phase, gravity state per Observer, and shared seed. Owned by the session authority. Never serialize particles or animation.

**DoD:** the cat falls → no death screen, no scene reload → within a moment it respawns with correct puzzle state, in solo and in co-op.

---

## 21. Phase 10 — Gravity puzzle

The Magic Test becomes gameplay. A (at a Control Station) steers B's gravity so B can traverse a room, then the roles reverse.

- The controller always gets obvious feedback that the action is being transmitted.
- The affected cat is expressive: ears back, paws scrambling, tail reacting, surprised landing. This is where cats make failure funny.
- Solo: test both adaptations from §10.5.

---

## 22. Phase 11 — Perspective / shadow puzzle

A positions an object in a normal ruin. In B's blueprint-like reality, its **shadow** becomes a physical platform. Implementation: B's platform manifestation derives its position/size from an anchor that A's object drives. This is still pure anchors, and nothing spatial is shared. It preserves the original Chapter 5 idea without 3D.

---

## 23. Phase 12 — Disconnect / reconnect

Test these: Wi-Fi off, Wi-Fi → cellular, backgrounding, a brief screen lock, force-closing one client, and **the session authority leaving**.

Expected: "Partner reconnecting…" → puzzle-critical state pauses → after reconnect, both return to the same checkpoint with the same roles.

**DoD:** a temporary disconnect never dumps both players to the title screen.

---

## 24. Phase 13 — Assemble the 10–15 minute vertical slice

| Minutes | Content |
|---|---|
| 0–2 | Connect. Simple movement. No explanation that the worlds differ; let players discover it. |
| 2–5 | Contradiction puzzle (vine ↔ elevator). |
| 5–8 | Gravity mechanic. A manipulates B, then switch roles. |
| 8–11 | Perspective/shadow mechanic. Both must describe what they see. |
| 11–14 | Combine two learned mechanics. |
| 14–15 | Synchronized finale: both activate anchors, both screens transform at the same tick, music, haptic pulse. The realities briefly converge (the key-art composition), then **PARALLAX**. |

Solo runs through the same slice using its adaptations.

---

## 25. Gate 5 — Social Magic (playtesting)

Do not evaluate the slice yourself.

### 25.1 Co-op: 10 pairs

Mix the setups: at least **5 remote pairs** (different rooms, voice call) and **5 same-room pairs**. Same-room testers will peek at each other's screens. That's expected, but record when it happens and whether peeking killed the fun or added to it.

Signals:
1. Players spontaneously ask **"What do you see?"**
2. Players laugh or yell when one affects the other's world.
3. Cause and effect is understood after one or two attempts.
4. Players communicate rather than sitting silently confused.
5. Someone asks **"Is there another level?"**

### 25.2 Solo: 6–8 players

Signals:
1. Players use RECORD/SWITCH correctly after at most one explanation.
2. Echo puzzles are described as clever, not tedious.
3. The median number of re-records per Echo puzzle is reasonable (target ≤ 3–4).
4. Players describe "cooperating with myself" positively.
5. They would play solo again.

**Fail on either mode → fix that mode before building more content.** If co-op passes and solo fails, solo can be cut back to a lighter mode. If co-op fails, the game is not ready.

---

## 26. Phase 14 — Art production (only after Gate 5)

**Reality A kit:** 4–6 platform types, 3 arches, 3 ruined columns, roots, vines, cloth, distant floating ruins, clouds, water, fog, particle accents.
**Reality B kit:** 4–6 geometric platforms, monoliths, cyan edge structures, grid planes, hanging cubes, void background, glass, energy lines, mist, particle accents.

Each reality's art sits on its own collider layout. Anchors connect them. That is the payoff of the semantic-anchor architecture: art can be replaced freely.

---

## 27. Reusable systems, not new games per chapter

```text
AnchorMove · GravityShift · ScaleShift · MaterialSwap · WorldRotate
PerspectiveShift · ShadowProjection · TimeEcho · ControlSwap · RealityReveal
```

| Chapter | Systems |
|---|---|
| 1 | AnchorMove |
| 2 | AnchorMove + GravityShift |
| 3 | ScaleShift + AnchorMove |
| 4 | GravityShift + WorldRotate |
| 5 | PerspectiveShift + ShadowProjection |
| 6 | TimeEcho (Echo mechanics shared with solo) |
| Finale | Combinations of everything |

---

## 28. Chapter Definition of Done

```text
Design idea → paper description → co-op/solo tags → greybox → solo test
→ two-player test → network test → reset/checkpoint → art concept
→ asset production → lighting/VFX → audio → performance test
→ pair + solo playtest → approved
```

**Never** make beautiful art first and build a puzzle underneath it.

---

## 29. Performance Definition of Done

On a real target phone (include one low/mid-range device): average FPS, 1% low FPS, memory, texture memory, draw calls, thermal behavior over 20 minutes, battery drain, scene load time, RTT, disconnects.

Targets: **60 FPS**, with **30 FPS** as an emergency fallback. In solo, only one reality renders at a time. Both are *simulated*, but the inactive one is cheap.

---

## 30. Commercial scope and systems

**First commercial scope:** free 20–30 minute prologue · ~2–3 hour paid campaign · 6 chapters · 6–8 reusable systems. Add Chapters 7–8 later only if sales and reviews justify them.

**Then add:** Google Play Console, AAB release, purchase entitlement, free prologue, friend pass, deep links, analytics, crash reporting, privacy policy, save data, store assets, achievements (if worthwhile).
**Still defer:** random matchmaking, moderation, built-in voice, live-service economy, battle passes, subscriptions, UGC.

---

## 31. Development gates

| Gate | Question | At | If no |
|---|---|---|---|
| **1 — Android** | Does a Unity app run reliably on my phone? | PAX-005 | Fix setup |
| **2 — Movement** | Does the cat feel good on a phone, in any gravity direction? | PAX-013 | Fix input/feel |
| **3 — Local Reality** | Do two realities, anchors, and Echo work cleanly on one phone? | PAX-024 | Fix architecture before Photon |
| **4 — Network Magic** | Can Phone A convincingly change gravity on Phone B? | PAX-035 | Fix mechanic/networking. **Buy Sorceress on "yes."** |
| **5 — Social Magic** | Do pairs communicate and laugh, and do solo players enjoy Echo? | Vertical slice | Redesign puzzles |
| **6 — Commercial** | After the polished prologue, do people want the next chapter enough to pay? | Prologue | Don't produce more content |

---

## 32. Full ticket list (PAX-001 → PAX-035)

| Ticket | Task | Test scope |
|---|---|---|
| PAX-001 | Create Unity 6 Universal 2D project; verify serialization/meta/input settings | Editor |
| PAX-002 | Git, GitHub, `.gitignore`, `.gitattributes` + LFS | — |
| PAX-003 | Folders, assembly definitions, Docs, `CLAUDE.md` | Editor |
| PAX-004 | Android settings: Landscape Left, IL2CPP, ARM64, API level | — |
| PAX-005 | Bootstrap scene + first device build → **Gate 1** | Device |
| PAX-006 | Placeholder cat prefab + sandbox scene | Editor |
| PAX-007 | `GravityReceiver` + gravity-relative horizontal movement | Editor |
| PAX-008 | Jump with coyote time + buffering | Editor |
| PAX-009 | Ground detection along gravity | Editor |
| PAX-010 | Gravity debug rotation + body alignment | Editor |
| PAX-011 | Fall/reset volume + spawn | Editor |
| PAX-012 | Camera follow | Editor |
| PAX-013 | Touch input → `CatCommand` → **Gate 2** | Device |
| PAX-S01 | Photon spike (throwaway project, not merged) | Editor ×2 |
| PAX-014 | Observer model + drivers | Editor |
| PAX-015 | Reality roots, offset, layers, spawn both cats | Editor |
| PAX-016 | Per-Observer cameras, sorting layers, lights | Editor |
| PAX-017 | Solo switch | Editor + device |
| PAX-018 | Debug panel v1 | Editor + device |
| PAX-019 | Events, transport interface, `LocalTransport` + latency | Editor |
| PAX-020 | Anchor registry + EditMode tests | Editor |
| PAX-021 | Presenters/manifestations (vine/elevator) | Editor |
| PAX-022 | Cross-reality interaction via anchor | Editor + device |
| PAX-023 | Echo recording + EditMode tests | Editor |
| PAX-024 | Echo playback, HUD, overlap pressure plate → **Gate 3** | Device |
| PAX-025 | Tilt input with sensor fallback + calibration | Device |
| PAX-026 | Touch dial behind the same interface | Device |
| PAX-027 | Control Station + control stream + local feedback | Editor + device |
| PAX-028 | Import Fusion 2, Net assembly, bootstrap | Editor |
| PAX-029 | Create/join by code, stable roles | Editor ×2 |
| PAX-030 | Two-client Editor testing setup | Editor ×2 |
| PAX-031 | `FusionTransport` + Observer binding | Editor ×2 |
| PAX-032 | Networked cats | Editor ×2 → devices |
| PAX-033 | Networked anchors (master-client authority) | Editor ×2 → devices |
| PAX-034 | Networked control stream + RTT | Editor ×2 → devices |
| **PAX-035** | **Network Magic Test → Gate 4** | **Two devices, two networks** |

---

## 33. Time expectations

These are rough numbers for a first-time developer working **part-time (~10–15 h/week)** with AI assistance. Full-time is roughly half.

| Stretch | Estimate |
|---|---|
| Phase 0 (learning) | 1–2 weeks |
| Phase 1 (Gate 1) | 1–2 weeks. The Android toolchain is the usual stumble. |
| Phase 2 (Gate 2) | 2–4 weeks |
| Phase 3 | 2–3 weeks |
| Phase 4 (Gate 3) | 3–5 weeks |
| Phase 5 | 1–3 weeks |
| Phase 6 (Gate 4) | 3–6 weeks |
| **To Gate 4 total** | **~3–6 months** |
| Vertical slice to Gate 5 | a further ~3–6 months |

Slipping these estimates is normal and is not a sign the project is failing. Skipping a gate is the thing to worry about.

---

## 34. First prompt for the Architect

Attach `00_VISION.md`, `02_ARCHITECTURE.md`, `07_DECISIONS.md`, `CLAUDE.md`, this plan, and (for reference only) the original specification:

> You are the technical architect of PARALLAX. The attached documents are authoritative in the precedence order stated in the plan (§0.1). The original specification is reference material only, and anything in it about 3D, humanoids, or photo-avatars is superseded.
>
> Do not write production code. Your job is to turn tickets **PAX-001 through PAX-035** (plus spike PAX-S01) into individual ticket files in the format of plan §7, one file per ticket.
>
> For each ticket: allowed files; the CLAUDE (code) vs. YOU (Unity Editor) split; forbidden scope; testable requirements; numbered acceptance tests with test scope (Editor / device / two devices); EditMode tests where logic is pure; and any architecture sections it depends on.
>
> Optimize for a first-time solo Unity developer. Keep each ticket independently testable and small enough to finish in one or two sessions. If any ticket is too large, split it and say so. If you find a contradiction or gap in the architecture, list it at the top with a proposed `07_DECISIONS.md` entry rather than silently resolving it. Do not add features beyond PAX-035.

Start with only the first five tickets to check the format. Then do the rest.

---

## 35. The workflow from here on

```text
YOU
 ↓
ARCHITECT (chat)            → one precise ticket file
 ↓
IMPLEMENTER (Claude Code)   → code + explanation + test steps
 ↓
YOU in UNITY                → Editor wiring, compile, Editor test
 ↓
PHYSICAL PHONE(S)           → when the ticket's test scope says so
 ↓
REVIEWER (fresh session)    → networking/state tickets
 ↓
YOU                         → explain it back · accept or reject
 ↓
GIT COMMIT "PAX-XXX: …"
 ↓
NEXT TICKET
```

The art lane runs separately and only after Gate 4:

```text
Claude (art direction) → asset spec → Sorceress → cleanup → Unity → performance check
```

**The two lanes meet only inside Unity.**

---

## Final direction

The two cats make a vast, serious, surreal universe feel approachable without making it childish. Their small scale makes the architecture feel enormous, and gravity or control mistakes become naturally funny. The original finale can still be delivered with layers, shaders, masks, parallax, 2D physics, and clever transitions instead of thousands of bespoke 3D assets.

**The immediate goal is not "make PARALLAX."** It is:

1. **PAX-024:** prove two realities, anchors, and Echo on one phone.
2. **PAX-035:** prove that Phone A can change Phone B's gravity.

Everything else waits for those two proofs.
