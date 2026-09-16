# PARALLAX — Architecture

**Status:** Authoritative for technical design. Changes require a `07_DECISIONS.md` entry.
**Precedence:** `07_DECISIONS.md` > `00_VISION.md` > **this file** > `CLAUDE.md` > implementation plan > original spec.
**Last updated:** 2026-09-10

> The code in this document is **interface sketches**. It shows intent, names, and contracts, not final implementations. Tickets refine them.

---

## 1. Principles

1. **Two Observers, always.** An Observer is not a human, a player, or a network peer.
2. **Separate geometry per reality.** Realities are linked only through semantic state.
3. **Synchronize meaning, not pixels.** Network anchors, control values, and cues. Never sprites, particles, or animation frames.
4. **Gameplay is transport-agnostic.** Puzzles run identically on `LocalTransport` and `FusionTransport`.
5. **Local feedback never waits for the network.** The causing player always sees an immediate response.
6. **Requests go up, state comes down.** Only the owner of a piece of state commits changes to it.
7. **Idempotent by construction.** Requests carry absolute values and event IDs.
8. **Artwork never decides collision.**
9. **Small components.** Classes stay under ~400 lines. No singleton sprawl: one composition root wires things up.

---

## 2. Assemblies and dependencies

| Assembly | Folder | Contains | May reference |
|---|---|---|---|
| `Parallax.Core` | `_Game/Core` | IDs, structs, events, `AnchorRegistry`, Echo data, filters, pure logic | UnityEngine only |
| `Parallax.Gameplay` | `_Game/Gameplay` | MonoBehaviours: observers, motor, input, reality, presenters, echo, puzzles, `LocalTransport` | Core, Input System, URP |
| `Parallax.Net.Fusion` | `_Game/Net/Fusion` | `FusionTransport`, session service, network behaviours | Core, Gameplay, **Photon Fusion** |
| `Parallax.App` | `_Game/App` | Composition root (`GameBootstrap`): chooses the transport and binds Observers | Core, Gameplay, Net.Fusion |
| `Parallax.DebugTools` | `_Game/DebugTools` | Debug panel, latency slider, PiP | Any (dev builds only) |
| `Parallax.Editor` | `_Game/Editor` | Editor menu/setup scripts | Any (Editor platform only) |
| `Parallax.Tests.EditMode` | `_Game/Tests/EditMode` | Unit tests for Core | Core, Gameplay |

**Hard rule:** `Core` and `Gameplay` **never** reference Photon. If a gameplay feature seems to need Photon, the transport interface is missing something. Extend the interface instead.

---

## 3. Spatial model

### 3.1 Reality roots

```text
Scene: VS_Level01
├── RealityRoot_A      world position (0, 0)       layer: RealityA
│   ├── Geometry/      colliders (invisible) + art (visual only)
│   ├── Anchors/       manifestations (vine, statue…)
│   ├── Lights/        Light2Ds targeting A_* sorting layers only
│   ├── Spawn_A
│   └── Cat_A
├── RealityRoot_B      world position (0, 1000)    layer: RealityB
│   ├── Geometry/
│   ├── Anchors/       manifestations (elevator, monolith…)
│   ├── Lights/        Light2Ds targeting B_* sorting layers only
│   ├── Spawn_B
│   └── Cat_B
├── Camera_A           culling mask: RealityA (+UI)
├── Camera_B           culling mask: RealityB (+UI)
└── Systems/           GameBootstrap, AnchorRegistry host, CheckpointManager…
```

- **The offset** is a single constant (`RealitySpace.OffsetB`, default `(0, 1000)`). Everything uses it; nothing hard-codes it.
- **Correspondence:** a position "in the same place" in the other reality has the **same local coordinates** under the other root. `RealityRoot.MapTo(other, worldPos)` (delegating to `RealitySpace.MapTo`) converts between them.
- **Physics layers:** `RealityA` and `RealityB`, with cross-collision disabled in the collision matrix. The world offset already separates them, and the matrix is a second line of defense.
- **All physics queries** (ground checks, sensors) use a layer mask for their own reality.
- **Sorting layers:** `A_Background, A_Middle, A_Gameplay, A_Foreground, B_Background, B_Middle, B_Gameplay, B_Foreground, UI`. **This matters:** a global `Light2D` affects every sprite on its target sorting layers regardless of position, so per-reality sorting layers are the only way to keep warm light out of the cold reality.
- **Both realities are loaded on every device.** In solo, both are simulated. In co-op, each device simulates its own cat; the remote cat is a network proxy.
- `PARALLAX/Validate/Reality Isolation` (read-only) checks layers, sorting layers, cross-root references, Light2D targets, and camera culling masks for exactly this kind of leak. Run it after hand-editing either reality; `RealitySetup` also runs it as its last step.

### 3.2 Cameras

- One camera per Observer. It follows its cat.
- **Solo:** only the active Observer's camera renders to the screen. The other is disabled, or renders to a small viewport in the debug PiP.
- **Co-op:** each device enables only its local Observer's camera.
- World-aligned, never rotates (D-020, confirmed on device by D-021).
- A hidden camera is disabled at the **component** level (`Camera.enabled = false`), never by deactivating its GameObject, so its `CatCameraFollow` keeps tracking its target while hidden and there's no swoop from a stale position when it's shown again.

---

## 4. Observer model

```csharp
public enum ObserverId : byte { A = 0, B = 1 }

public static class ObserverIdExtensions
{
    public static ObserverId Other(this ObserverId id) =>
        id == ObserverId.A ? ObserverId.B : ObserverId.A;
}

public enum InputSourceKind : byte { Inactive, LocalHuman, RemoteHuman, EchoReplay }
```

The four input sources drive a cat in fundamentally different ways. Some feed commands to the physics motor, and others move the body directly. So an input source is implemented as a **driver**:

```csharp
public interface IObserverDriver
{
    InputSourceKind Kind { get; }
    void Activate(ObserverContext observer);   // take control (enable/disable motor, set body type)
    void Deactivate();                         // release control cleanly
    void FixedTick(int tick);                  // called by ObserverContext every fixed step
}
```

| Driver | Motor | Rigidbody2D | Source of movement |
|---|---|---|---|
| `LocalHumanDriver` | enabled | Dynamic | `CatCommand` from touch/keyboard |
| `InactiveDriver` | enabled | Dynamic | `CatCommand.None` (gravity still applies) |
| `EchoReplayDriver` | disabled | Kinematic | Recorded frames (§8) |
| `RemoteProxyDriver` | disabled | Kinematic | Network layer moves the transform |

```csharp
public sealed class ObserverContext : MonoBehaviour
{
    [SerializeField] ObserverId id;
    [SerializeField] RealityRoot reality;
    [SerializeField] CatMotor2D cat;
    [SerializeField] Camera observerCamera;

    public ObserverId Id => id;
    public RealityRoot Reality => reality;
    public CatMotor2D Cat => cat;
    public Camera Camera => observerCamera;
    public IObserverDriver Driver { get; private set; }

    public void SetDriver(IObserverDriver next)
    {
        Driver?.Deactivate();
        Driver = next;
        Driver.Activate(this);
    }
}
```

`ObserverSet` holds both contexts (`Get(ObserverId)`). Systems receive it from the composition root. There is no global singleton.

`ObserverSet.FixedUpdate` is the **only** per-tick entry point for cats (D-022). It owns `Tick`, incrementing once per fixed step, then steps Observer A, then Observer B, through their drivers: `Tick++; observerA.Step(Tick); observerB?.Step(Tick);`. `CatMotor2D` has no `FixedUpdate` of its own — it exposes `Step(in CatCommand, float dt)`, called only by a driver's `FixedTick`. "Motor enabled" / "motor disabled" in the table above means "driver calls `Step`" / "driver doesn't call `Step`", not a Unity component-enabled flag.

### 4.1 Commands

```csharp
public struct CatCommand
{
    public float Move;          // -1..1 along the cat's local "right" (perpendicular to gravity)
    public bool  JumpPressed;   // edge: pressed this tick
    public bool  JumpHeld;
    public bool  InteractPressed;
    public bool  InteractHeld;

    public static CatCommand None => default;
}
```

Touch, keyboard, and (later) gamepad all produce `CatCommand`. The motor never reads input devices directly.

### 4.2 Solo switching

`SoloSwitchController` is the **only** runtime code that assigns Observer drivers or enables/disables Observer cameras; `ObserverBootstrap` delegates to it (D-023). On `SwitchTo`/`Toggle`:

1. For the **current** Observer: set `InactiveDriver`. (Stopping/replaying an Echo recording here is PAX-024.)
2. For the **target** Observer: set `LocalHumanDriver`.
3. Swap cameras at the component level: the target's `Camera` is enabled with `rect = (0,0,1,1)` and `depth = 0`; the other's `Camera` component is disabled (not its GameObject).
4. Raise a **local-only** C# event `Switched(ObserverId from, ObserverId to)`. It is never networked, and carries no VFX/audio in v1 — `Switched` firing is enough (PAX-017).

Switches are requested from `Update`, so they always land between fixed ticks, never mid-`FixedUpdate`.

### 4.3 Co-op binding

The composition root binds drivers per device:

| Device | Observer A | Observer B |
|---|---|---|
| Phone of human 1 (creator) | `LocalHuman` | `RemoteHuman` |
| Phone of human 2 (joiner) | `RemoteHuman` | `LocalHuman` |

Echo is disabled in co-op in v1. The driver model keeps it possible later.

---

## 5. Input

### 5.1 Movement input

Movement input is a **device-level rig**, not part of the cat prefab (D-021, D-022): a `CatInputRouter` merges `KeyboardCatInput` (Editor) and `TouchStickCatInput` (touch, read cat-relative per D-021). Each implements `ICatCommandSource { CatCommand Read(); void ResetTransientState(); }`. `LocalHumanDriver` binds the router to its Observer's `GravityReceiver` on `Activate` (`CatInputRouter.SetGravityFrame`, forwarded to the touch source) and calls `ResetTransientState()` on both `Activate` and `Deactivate`, so latched edges (e.g. a jump press) never survive a driver handover.

**Reserved touch regions (D-023):** on-screen controls (SWITCH, the debug panel, the gravity debug buttons) declare `ITouchReservedRegion { bool ContainsScreenPoint(Vector2 screenPos); }`. `TouchStickCatInput` takes a list of these; a touch that **begins** inside any region is never claimed as stick or jump for its whole lifetime, even if it drifts elsewhere. Without this, tapping an on-screen control could also start the stick or a jump.

### 5.2 Gravity control input

```csharp
public interface IGravityControlInput
{
    bool  IsAvailable { get; }
    void  Calibrate();           // capture neutral (tilt); reset (dial)
    float ReadNormalized();      // -1..1, already filtered
}
```

**`TiltGravityInput`**
- Sensor: `GravitySensor` if present, else `Accelerometer` with low-pass filtering. **No gyroscope needed.** Sensors are disabled by default in the Input System and must be enabled explicitly (`InputSystem.EnableDevice`).
- Orientation is locked to Landscape Left, so the screen-to-device mapping is constant. Verify that sensor values are compensated to screen orientation (the Input System has a setting for this) and fix the axis mapping once.
- Roll angle = angle of the gravity vector in the screen's X/Y plane, relative to the calibrated neutral.
- Pipeline: raw → low-pass → neutral subtraction → **dead zone** (~4°) → **clamp** (~±35°) → normalize → exponential smoothing.
- Calibrate on entering a Control Station.

**`DialGravityInput`**: an on-screen rotary control that outputs the same normalized value.

The active implementation is chosen in settings. If `TiltGravityInput.IsAvailable` is false, the game falls back to the dial.

### 5.3 Control Station

While seated at a Control Station, the controlling cat's movement is locked and its `IGravityControlInput` value is published as a control stream (§7.3) targeting `observer.Other()`. The station shows **immediate local feedback** (glow/pulse scaled by the value), followed by a short "energy in transit" effect, so network latency reads as intentional theatre.

---

## 6. Cat motor and per-cat gravity

**`Physics2D.gravity` is never used for cats.** Each cat's `Rigidbody2D.gravityScale` is forced to 0, and the motor applies gravity itself.

```csharp
[RequireComponent(typeof(Rigidbody2D))]
public sealed class GravityReceiver : MonoBehaviour
{
    [SerializeField] Vector2 initialDirection = Vector2.down;
    [SerializeField] float strength = 30f;
    [SerializeField] float turnSpeedDegPerSec = 360f;

    Vector2 target;
    public Vector2 Direction { get; private set; }
    public float Strength => strength;

    void Awake() { Direction = target = initialDirection.normalized; }

    public void SetTargetDirection(Vector2 dir)
    {
        if (dir.sqrMagnitude > 1e-4f) target = dir.normalized;
    }

    public void FixedTick(float dt)
    {
        float cur  = Vector2.SignedAngle(Vector2.down, Direction);
        float goal = Vector2.SignedAngle(Vector2.down, target);
        float next = Mathf.MoveTowardsAngle(cur, goal, turnSpeedDegPerSec * dt);
        Direction  = Quaternion.Euler(0f, 0f, next) * Vector2.down;
    }
}
```

Motor step (sketch):

```csharp
void FixedUpdate()
{
    float dt = Time.fixedDeltaTime;
    gravity.FixedTick(dt);

    Vector2 down  = gravity.Direction;
    Vector2 right = new Vector2(-down.y, down.x);    // down (0,-1) → right (1,0)

    Vector2 v   = body.linearVelocity;               // Unity 6 name (formerly .velocity)
    float along = Vector2.Dot(v, right);
    float fall  = Vector2.Dot(v, down);

    along = Mathf.MoveTowards(along, command.Move * maxSpeed, accel * dt);
    fall += gravity.Strength * dt;
    if (CanJump()) fall = -jumpSpeed;                // CanJump: grounded/coyote + buffered press
    fall = Mathf.Min(fall, maxFallSpeed);

    body.linearVelocity = right * along + down * fall;

    // Align body so local up = -down. Rigidbody has FreezeRotation; set rotation explicitly.
    body.rotation = Vector2.SignedAngle(Vector2.down, down);   // verify smoothing/behaviour in PAX-010
}
```

**Ground detection:** `body.Cast(down, groundFilter, hits, probeDistance)`. Casting the cat's own collider never hits itself. `groundFilter`'s mask comes from the cat's parent `RealityRoot` (`GetComponentInParent<RealityRoot>().PhysicsMask`, resolved once in `Awake`), not from `CatMotorConfig` — a cat with no `RealityRoot` ancestor logs an error and its ground cast matches nothing (no silent fallback). A hit counts as ground if `Vector2.Dot(hit.normal, -down) > 0.7`.

**Gravity sources:** only `GravityReceiver.SetTargetDirection` changes gravity. It is called by the control-stream receiver, checkpoint restore, and debug tools.

Mapping the gravity control value to a direction is a tunable per Control Station, for example `angle = value * maxAngle` around straight down, or discrete snapping to 90° steps.

---

## 7. Semantic state

There are three kinds of shared meaning. Choosing the right kind is most of the design work.

| Kind | Nature | Example | Networked as |
|---|---|---|---|
| **Anchor** | Discrete/logical target, changes occasionally | `VerticalPathway01 = 1` | Request → authority commit → replicated state |
| **Control stream** | Continuous value from one controller | Gravity angle from A's station | Owner-published state, latest wins, ~20 Hz |
| **Cue** | One-shot synchronized cosmetic | Finale convergence at tick N, seed S | Reliable event with start tick + seed |

### 7.1 Anchors

```csharp
public readonly struct AnchorId : System.IEquatable<AnchorId>
{
    public readonly ushort Value;
    public AnchorId(ushort value) { Value = value; }
    public bool Equals(AnchorId other) => Value == other.Value;
    public override int GetHashCode() => Value;
}

public struct AnchorState
{
    public float Value;      // logical target, usually 0..1; meaning defined per anchor
    public uint  Revision;   // incremented by the authority on every commit
}

public enum EventOrigin : byte { HumanA, HumanB, EchoA, EchoB, System }

public readonly struct AnchorRequest
{
    public readonly AnchorId    Anchor;
    public readonly float       TargetValue;   // ABSOLUTE. Never a delta.
    public readonly EventOrigin Origin;
    public readonly uint        Sequence;      // per-origin counter → event ID
}
```

**Anchors hold logical targets, not animation.** "The pathway is up (1)" is state. The elevator gliding up over 1.5 s is presentation, done locally by the manifestation. Pulling a vine sends `Target = 1` on pull and `Target = 0` on release (if the anchor springs back). It never streams values every frame.

```csharp
public sealed class AnchorRegistry          // Parallax.Core, pure C#
{
    public event System.Action<AnchorId, AnchorState> Changed;

    public bool Register(AnchorId id, float initialValue); // Revision 0; false if already registered
    public bool TryGet(AnchorId id, out AnchorState state);

    // Authority side only.
    public CommitResult Commit(in AnchorRequest request);
    //  - unregistered anchor -> UnknownAnchor
    //  - NaN/infinite target -> InvalidValue (sequence NOT recorded)
    //  - request.Sequence <= lastAppliedSequence[request.Origin] -> Duplicate (per-origin highest-applied sequence, not a bounded recent-ID set)
    //  - else record lastAppliedSequence[Origin] = Sequence, then:
    //      Value == TargetValue (exact) -> NoChange (no revision change, no event)
    //      else set Value, Revision++, raise Changed -> Applied

    // Non-authority side: apply replicated state.
    public void ApplyReplicated(AnchorId id, AnchorState state);
    //  - ignore if state.Revision <= current Revision; create the anchor if unknown
}
```

**Duplicate protection (D-024):** per-origin highest-applied sequence, not a bounded recent-ID set — a late duplicate arriving outside a fixed window could otherwise re-apply an outdated absolute target and revert a newer change. Because tracking is per origin across all anchors, not per (origin, anchor), a transport must deliver each origin's requests in send order, or an out-of-order later request for one anchor can cause an earlier request for a different anchor to be dropped as `Duplicate`. An Echo replay must issue fresh sequences from its own origin's `EventSequencer` on every playback (§8.3), never re-send recorded sequence numbers, or the second replay is dropped as `Duplicate`.

### 7.2 Presenters and manifestations

```text
SharedAnchor (logical, one per AnchorId)
   └── RealityPresenter (listens to AnchorRegistry.Changed)
         ├── RealityManifestation in Reality A  (e.g. VineManifestation)
         └── RealityManifestation in Reality B  (e.g. ElevatorManifestation)
```

- A manifestation animates locally toward the committed value at its own speed/easing.
- A manifestation **may carry colliders** (the elevator is a moving platform). This is safe because only its own reality's cat can touch it, and that cat is simulated on the same device that animates it.
- Interactables (`VineInteractable`) issue `AnchorRequest`s through the transport. They never set manifestations directly.
- **Changing art never changes anchor or network code.** Swapping `ElevatorManifestation` for `CrystalLiftManifestation` is an art change.

### 7.3 Control streams

```csharp
public enum ControlChannel : byte { GravityAngle }

public struct ControlSample
{
    public ControlChannel Channel;
    public ObserverId     Target;
    public float          Value;   // normalized -1..1
    public int            Tick;
}
```

- Owned and published by the **controller's** device. Latest value wins. No authority round-trip.
- The receiver smooths toward the latest value (and holds it if samples stop).
- The receiving device applies it to its own cat's `GravityReceiver`.

### 7.4 Cues (synchronized spectacle)

```csharp
public struct SpectacleCue { public ushort CueId; public int StartTick; public uint Seed; }
```

Both devices start the cue at `StartTick` using `Seed` for any randomness. Cues are cosmetic: if one is missed, gameplay is unaffected.

---

## 8. Echo Replay (state-based)

### 8.1 Why state-based

Replaying recorded *inputs* through physics diverges as soon as anything differs from the recording: a moved platform, a changed anchor, a float rounding difference. Replaying recorded *state* is exact.

### 8.2 Data

```csharp
public struct EchoFrame
{
    public Vector2 Position;     // local to the Observer's RealityRoot
    public float   Rotation;
    public Vector2 Velocity;     // for animation only
    public byte    AnimState;
    public bool    FacingRight;
    public bool    InteractHeld;
}

public struct EchoAnchorEvent  { public int TickOffset; public AnchorRequest Request; }
public struct EchoControlEvent { public int TickOffset; public ControlSample Sample; }

public sealed class EchoRecording
{
    public ObserverId Observer;
    public List<EchoFrame>        Frames;         // one per fixed tick
    public List<EchoAnchorEvent>  AnchorEvents;   // requests issued while recording
    public List<EchoControlEvent> ControlEvents;  // control samples issued while recording
}
```

At 50 Hz, a 10 s recording is 500 frames, which is trivial in memory.

### 8.3 Recording

- `EchoRecorder` attaches to an Observer while it is `LocalHuman`.
- Each fixed tick it appends an `EchoFrame`.
- It intercepts the Observer's outgoing `AnchorRequest`s and `ControlSample`s and stores them with `TickOffset`, re-tagging the origin as `EchoA`/`EchoB` for playback.
- It stops at the cap (default 10 s) or on SWITCH.

### 8.4 Playback (`EchoReplayDriver`)

- Motor disabled. `Rigidbody2D.bodyType = Kinematic`. Each tick: `MovePosition`/`MoveRotation` to the frame, and update animation from the frame.
- Stored events are re-issued through the transport when their tick offset is reached, with fresh sequence numbers under the Echo origin.
- **End of playback:** hold the final frame. Held interactions stay held (a paw on a lever stays). Control streams keep their last value.
- **Cancel:** switching back to this Observer cancels playback, restores Dynamic body type, and resumes from the current Echo position.
- **Ghost rule:** the Echo follows its recording even if the world changed. Level design keeps Echo-relevant geometry static or deterministically anchor-driven.
- v1: one Echo at a time, solo only.

### 8.5 Sensors must detect Echoes

Kinematic bodies do not reliably generate contacts or trigger callbacks with static colliders. So **puzzle sensors (pressure plates, zones) use explicit overlap queries** (`Physics2D.OverlapBox` with the reality's layer mask, then check for a `CatBody` component) every fixed tick. This works identically for live cats and Echo cats.

---

## 9. Transport

```csharp
public interface IRealityTransport
{
    bool IsSessionAuthority { get; }   // may this device commit anchors/puzzle state?
    int  Tick { get; }                 // shared tick (network tick in co-op, local fixed-step count in solo)

    void RequestAnchor(in AnchorRequest request);                 // routed to session authority
    void PublishControl(in ControlSample sample);                 // owner-published stream
    void SendCue(in SpectacleCue cue);

    event System.Action<ControlSample> ControlReceived;
    event System.Action<SpectacleCue>  CueReceived;
    // Anchor state arrives via AnchorRegistry.Changed (authority) or ApplyReplicated (others).
}
```

### 9.1 `LocalTransport` (Gameplay assembly)

- Always the session authority.
- `RequestAnchor` → (optional artificial delay) → `AnchorRegistry.Commit`.
- `PublishControl` → (optional artificial delay) → `ControlReceived`.
- **Artificial latency** (0–400 ms, set in the debug panel) lets causality theatre be designed and tested before Photon exists.
- **Never synchronous (D-024):** `RequestAnchor`, `PublishControl`, and `SendCue` only enqueue — never deliver inside the call, even at 0 ms latency, so no caller can come to rely on synchronous commits the network won't give. Delivery tick is computed at enqueue time as `the current simulation tick (ObserverSet.Tick, via a tick source) + max(1, ceil(LatencyMs / (fixedDeltaSeconds * 1000)))`; changing `LatencyMs` afterward never reschedules items already queued. A request made during tick N is never delivered before tick N+1, wherever it's called from (inside `A.Step`/`B.Step`, from `OnGUI`, from a trigger callback). `LocalTransportHost` pumps the transport once per tick from `ObserverSet.Stepped`, after both Observers have stepped.

### 9.2 `FusionTransport` (Net.Fusion assembly)

- Session authority = the Photon **master client**.
- A master-client-owned `NetworkObject` (`AnchorStateHost`) holds a networked array of `AnchorState`. On change, other clients call `AnchorRegistry.ApplyReplicated`.
- `RequestAnchor` on a non-authority client → RPC to the state authority → `Commit` → replicated.
- Control streams: each Observer's controller owns a `NetworkObject` with a networked `ControlSample` field.
- Cues: reliable RPC to all, carrying `StartTick` and `Seed`.
- **Authority migration:** if the master client leaves, the new master client must take over `AnchorStateHost`. Fusion 2 provides a master-client-object mechanism for this. Confirm its exact behavior in the PAX-S01 spike and document it in `04_NETWORKING.md`.

### 9.3 Authority table

| State | Owner | Others |
|---|---|---|
| Cat A body | Device controlling A | Proxy via `NetworkTransform` |
| Cat B body | Device controlling B | Proxy via `NetworkTransform` |
| Control stream from A / B | Controlling device | Receive + smooth |
| Anchor states | Session authority | Request via RPC, receive replicated |
| Puzzle phase, checkpoint | Session authority | Request, receive |
| Cues | Anyone may send | All play at `StartTick` |
| VFX, particles, animation, UI, camera | **Never networked** | Derived locally |

### 9.4 Latency rules

- Local feedback first, always (§5.3).
- Presentation animates toward committed values. It never snaps unless the difference is large, such as after a reconnect.
- Nothing gameplay-critical depends on both devices seeing the same animation frame.

---

## 10. Time

- **Solo:** `Tick` = count of `FixedUpdate` steps since the level loaded.
- **Co-op:** `Tick` = Fusion's network tick.
- Echo uses tick offsets, never `Time.time`.
- Cues use absolute ticks.

---

## 11. Checkpoints

```csharp
public struct CheckpointSnapshot
{
    public ushort CheckpointId;
    public Vector2 SpawnA, SpawnB;         // local to each RealityRoot
    public Vector2 GravityA, GravityB;
    public byte PuzzlePhase;
    public uint Seed;
    public AnchorState[] Anchors;          // indexed by AnchorId
}
```

- Taken and restored by the session authority. In co-op, a restore is a replicated state change.
- Restore is instant: teleport cats, snap gravity, apply anchors (presenters snap), cancel any Echo. No scene reload.

---

## 12. Sessions (co-op)

- **CREATE ROOM** creates a Fusion Shared Mode session with a short human-readable code. **JOIN ROOM** joins by code.
- **Roles are stable:** creator = Observer A, joiner = B. The role is stored against the player's identity so a reconnecting player gets the same Observer.
- Both devices load the same level scene (both realities).
- On disconnect: show "Partner reconnecting…", pause puzzle-critical state, restore to the last checkpoint on rejoin.

---

## 13. Debug tooling (dev builds only)

Debug panel (toggle with a multi-finger tap or an Editor key):
- active Observer, both drivers, Echo state/progress;
- both cats' gravity direction, grounded state, velocity;
- live anchor table (ID, value, revision);
- control stream values in/out;
- **artificial latency slider** (LocalTransport);
- RTT and session role (FusionTransport);
- picture-in-picture view of the other reality;
- FPS.

**v1 (PAX-017), `DebugPanel`:** toggled by an OnGUI "DBG" button (top-left) or the backquote key; starts collapsed, showing only the DBG button. Open, it shows the active Observer, `ObserverSet.Tick`, FPS (smoothed over ~0.5s), and per Observer: driver kind, gravity direction as an angle, grounded, velocity magnitude. Control stream values and the RTT/session-role row are later phases. `DebugPanel` never assigns drivers — it only reads Observer state and (for PiP) enables/disables an Observer's `Camera` component.

**PAX-019 addition:** with a `LocalTransportHost` wired in, the panel also shows a latency slider (0–400 ms, labeled with the equivalent tick count), `PendingCount`, `LastCommitResult`, a live anchor table (ID, Value, Revision) from `AnchorRegistry.All`, and a "Debug anchor 0<->1" button that registers `AnchorId(65535)` (reserved for debug) on first use and requests the opposite of the last-requested value with origin `System`. The panel's rect grew to fit; `ContainsScreenPoint` still reads the same `PanelRect` field the panel draws with, so the reserved touch region grows with it.

PiP toggle (visible only while the panel is open, default off): when on, the **inactive** Observer's `Camera` is enabled with `rect = (0.70, 0.70, 0.28, 0.28)` and `depth = 1`; off, it's disabled again. `DebugPanel` subscribes to `SoloSwitchController.Switched` and re-applies PiP to the now-inactive camera after every switch. The debug panel and PiP are compiled only under `UNITY_EDITOR || DEVELOPMENT_BUILD` (`Parallax.DebugTools`'s `defineConstraints`), so they are absent from release builds.

---

## 14. Code vs. Editor conventions

- Claude edits: `.cs`, `.asmdef`, `.md`, `.json`, test files, Editor scripts.
- Claude does **not** hand-edit `.unity`, `.prefab`, `.meta`, `ProjectSettings/*` unless a ticket explicitly allows it.
- Repetitive scene/prefab setup is done by **Editor menu scripts** (`PARALLAX/Setup/…`) that Claude writes and you run.
- Components use `[RequireComponent]` and fetch siblings in `Awake`/`Reset` to reduce manual wiring.
- Tunables live in ScriptableObjects under `_Game/Data` (e.g. `CatMotorConfig`, `TiltConfig`, `EchoConfig`).

---

## 15. Testing

- **EditMode (Unity Test Framework):** `AnchorRegistry` (idempotency, dedupe, revision ordering), Echo frame indexing and event timing, tilt filter math, `ObserverId.Other`, the checkpoint snapshot round-trip.
- **Manual acceptance tests** per ticket, labeled Editor / device / two devices.
- **Two-client Editor tests** via Multiplayer Play Mode or Fusion multi-peer (decided in PAX-S01/PAX-030).
- **Device tests** for anything involving touch, sensors, performance, or networking conditions.

---

## 16. Performance notes

- Solo renders one reality at a time and simulates both. Keep the inactive reality free of per-frame cosmetic work (disable particle systems and animators outside its camera).
- Sprite Atlases per reality. Watch the Light2D count per sorting layer.
- Target 60 FPS, with 30 FPS as an emergency fallback, on a mid-range Android phone.

---

## 17. Questions for PAX-S01 (record answers in `04_NETWORKING.md`)

1. Joining a Shared Mode session by name from two Editor instances.
2. `NetworkTransform` quality on a client-owned `Rigidbody2D`.
3. RPC to the state authority of a shared object.
4. Master-client-object authority transfer when the master leaves.
5. Plain `FixedUpdate` gameplay alongside Fusion in Shared Mode.
6. Which works better with Fusion: Multiplayer Play Mode or multi-peer mode.
