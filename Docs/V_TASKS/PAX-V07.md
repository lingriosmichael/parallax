# PAX-V07 · Wire the cat animation set

**Status:** **Approved 2026-09-29** (revised text, reviewed by the developer). It replaces the earlier V07 text.
**Order:** after PAX-A08 is accepted (PAX-A13 is committed, 2026-09-30). Then PAX-V07b, then the seamless vine.
**Phase 1 size:** Lite.
**Depends on:**
- PAX-A08: the Cat A slot sheets are imported.
- PAX-A13 (committed 2026-09-30): `DeathInfo.Killer` and `RoomDeath.HoldKiller`.

**Decisions:**
- D-036: pure `CatAnimStateMachine`, no Animator.
- D-034: the presenter owns Visual's scale and facing.
- D-041/D-058: the death hold; death to control ≤ 0.75 s.
- D-044: no lives, a per-room death count.
- D-067: no randomness.
- D-075: the tick is 50 Hz, converted through `TickTime`.
- D-089/D-092: climbing.

**Developer rulings (2026-09-29):**
- Per-killer deaths come through a death-kind tag. A ScriptableObject maps tag → clip, and anything unmapped plays
  the frightened pose.
- The presenter never type-checks trap classes.
- No `CatMotor2D` change. Launched falls back to Rise.
- The walk/run threshold is a fraction of max speed.
- Hard-land distance is measured along gravity.
- Every visual state is visual only.
- Idle fidgets cycle look around → ear twitch → sit down. Sit down is last and any input cancels it. Tail flick is not
  wired.
- A death kind is declared on the trap component or prefab, with an optional per-object override. Level builders never
  set it per placed object, **except `Hazard`** (ruled 2026-09-30, Phase 1 question 6): builders are the only place a
  `Hazard` is created, so the builder that adds one sets its kind (spikes → Spiked, pit floors → Pit).
- The geyser is not a death kind: it is air, and it gets an entry only if it can kill on its own. Splash stays unmapped
  until a water hazard exists.
- Door / level complete plays celebrate (`CatA_Door`). The door-enter clip (`CatA_DoorEnter`) is imported by A08 but not
  wired.
- PAX-A13 is committed, so the old Part A and Part B are merged: the trap death-kind tags and their table entries ship in
  this ticket.
- Menu idle → PAX-A09. Dizzy is parked. Fed up → PAX-V07b.

## 1. What changes for the player

The cat shows the state it's in:
- a slow walk or a full run;
- a turn, a take-off, the apex, a hard landing;
- a death that shows what killed it, then a respawn;
- a roll on a gravity flip;
- climbing up and down, hanging still, and leaping off a vine;
- a celebration on level complete;
- small fidgets after standing still.

Nothing about movement, timing, collisions or routes changes.

## 2. States

New states are appended to `CatAnimState` and never renumbered. Today's values are Idle, Walk, Rise, Fall, Land and
Climb. The additions:

`Run, Turn, TakeOff, Apex, HardLand, Death, Respawn, Flip, Hang, Leap, Door, IdleFidget`

The table is in priority order: the highest applicable state wins.

| # | State | Enters when | Leaves when |
|---|---|---|---|
| 1 | Door | the level-complete event (`RoomManager.LevelCompleted`) | never (holds the last frame under the level-complete screen) |
| 2 | Death | this cat's room is holding (`RoomDeath.IsHolding` for this observer) | the hold ends |
| 3 | Respawn | the `CatRespawn.Respawned` event | the clip ends (≤ 0.2 s), or any input moves the cat |
| 4 | Flip | the sign of `GravityReceiver.Direction.y` changes | the clip ends |
| 5 | Climb / Hang | the cat is climbing (`motor.IsClimbing`): Climb while moving on the vine, Hang when still | climbing ends |
| 6 | Leap | climbing ends on a step where `JumpedThisStep` is true | the clip ends, then Rise |
| 7 | TakeOff | `JumpedThisStep` is true on the ground | the clip ends, then Rise |
| 8 | Rise / Apex / Fall | airborne, by velocity along gravity: above `airThreshold` against gravity is Rise, the band in between is Apex, above `airThreshold` along gravity is Fall | landing |
| 9 | HardLand / Land | touching down: the fall distance along gravity is at least `hardLandDistance` for HardLand, otherwise Land | the clip ends |
| 10 | Turn | facing reverses on the ground at a surface speed above `walkEnter` | the clip ends |
| 11 | Run / Walk | the surface speed is at least `runFraction` × `CatMotorConfig.MaxSpeed` for Run (with hysteresis `runExitFraction`), otherwise Walk above `walkEnter` | the speed changes |
| 12 | IdleFidget | idle for at least `fidgetDelay` seconds; the next fidget in the fixed cycle look around → ear twitch → sit down | look around and ear twitch: the clip ends, which resets the timer. Sit down: holds the seated frame until any input. Any input cancels any fidget on the same frame. |
| 13 | Idle | otherwise | |

Details:
- **Fall distance.** It is measured along gravity: the displacement along `GravityReceiver.Direction` from the highest
  point since the cat left the ground, to touchdown. A gravity flip in the air resets the high point.
- **Idle fidgets.**
  - **The cycle:** after the sit, the cycle restarts with look around. Any movement, landing, death or respawn resets
    the cycle to its start and the idle timer to zero.
  - **"Any input":** a non-zero move or a jump in the observer's current control sample, read through its
    `ObserverContext` driver and never written, or any motion of the cat.
- **Turn.** The facing scale flips at the end of Turn. If another state interrupts Turn, the flip is applied at once.
- **Climbing down.** Climb plays in reverse when the vertical velocity points down. Its frame rate follows the climb
  speed (`FlipbookMath.FpsForSpeed`).
- **Launched (geyser), Dizzy and FedUp** are not added. A cat in a geyser column shows Rise. Launched comes with a
  later ticket, after its clip is redone.

## 3. Visual only

The presenter and the state machine only read. They never write motor state, input, timers or physics, and no state
holds or delays input.

When the player acts during Turn, Land, HardLand, Respawn, IdleFidget or TakeOff:
- the cat's movement on that tick is identical to a run without the presenter;
- the visual moves on to the state the motion implies on the same frame.

The parity test in §6 checks this.

## 4. Death clips

- **`CatDeathKind`** (a new Core enum, append-only): `Default, Pit, Spiked, Crushed, Zapped, Arrow`. `Splash` is
  appended when a water hazard exists.
- **`IDeathKindSource`** (a new Core interface): `CatDeathKind DeathKind { get; }`.
- **Resolving a kind:**
  - if the killer (`RoomDeath.HoldKiller`) implements `IDeathKindSource`, use its kind;
  - otherwise a Fall or OutOfBounds cause is Pit (a pit floor is a `Hazard` whose declared kind is Pit, so it resolves
    through the first rule);
  - otherwise Default.

  The presenter only checks for the interface, never for a trap class.
- **`CatDeathClipTable`** (a ScriptableObject) maps kind → clip. A missing entry falls back to the Default clip (the
  frightened pose). It is created and filled by `PARALLAX/Setup/Cat Visual` at `Assets/_Game/Data/CatA_DeathClips.asset`.
- **Timing:** every death clip must reach its last, held frame within the hold, meaning (frames − 1) ÷ fps ≤
  `RoomSafetyConfig.HoldTicks` × the tick length from `TickTime`. That is 0.6 s at the default 30 ticks. A test checks
  every table entry.

**In this ticket** (Parts A and B merged, since PAX-A13 is committed):
- The enum, the interface, the table, the resolution rule, and the Default and Pit entries.
- **Where a kind is declared:** on the trap component or its prefab, never by the level builders on each placed object.
  - **`RoomTrap`** implements `IDeathKindSource`.
    - Its `DeathKind` returns the per-object override when one is set (a serialized `overrideDeathKind` flag plus
      `deathKindOverride`, off by default).
    - Otherwise it returns the component's declared kind: `protected virtual CatDeathKind DeclaredDeathKind =>
      CatDeathKind.Default`.
  - **`Hazard`** implements `IDeathKindSource` with a serialized `CatDeathKind`. Hazards have no prefab; the builders
    create them in code, so the builder that adds a hazard sets its kind: the spike builder sets Spiked, the pit-floor
    builder sets Pit (Phase 1 question 6).
- **Declared kinds:**

  | Trap | Kind |
  |---|---|
  | `HiddenSpikesTrap` | Spiked |
  | `FallingBlockTrap` | Crushed |
  | `StormCloudTrap` | Zapped |
  | `ArrowTrap`, including its spear variant | Arrow |
  | `MovingTrap`, Solid kind (push walls, crushers, lifts, drop-and-return and falling ceilings) | Crushed |
  | `MovingTrap`, Hazard kind (sliding spikes, sweeps) | Spiked |
  | `GeyserTrap` | none (it is air, not water; it gets a kind only if it can kill on its own) |
  | Splash | nothing is mapped until a water hazard exists |

- The table has the Spiked, Crushed, Zapped and Arrow entries as well as Default and Pit.

## 5. Code

- **Core** (pure, EditMode-tested):
  - `CatAnimState` (append), `CatDeathKind`, `IDeathKindSource`.
  - `CatAnimInput`: the per-frame inputs as a readonly struct.
  - `CatAnimStateMachine`:
    - It takes a `CatAnimInput`, and the old `Step` overloads stay as wrappers so today's tests keep compiling.
    - It owns the idle timer, the fidget counter, the fall high point along gravity, and the clip clocks for one-shot
      states.
- **Gameplay presentation:**
  - `CatClipSet` (new): the serializable clip table keyed by state, with the Walk clip as the fallback for Run until
    wired.
  - `CatPresentationSignals` (new):
    - It subscribes to `RoomDeath`, `CatRespawn`, `RoomManager.LevelCompleted` and `GravityReceiver`.
    - It exposes plain per-frame values, reads only, and resets on `OnDisable`.
  - `CatDeathClipTable` (new, ScriptableObject).
  - `CatVisualPresenter`: renders only.
    - The interim climb rotation (`ApplyClimbPose`) is removed. The new climb frames are drawn vertical and registered
      on the collider.
    - It stays under 400 lines.
  - `CatVisualConfig`: new fields `runFraction`, `runExitFraction`, `hardLandDistance`, `fidgetDelay`. Defaults are set
    by the setup menu, with no magic numbers in code.
- **Editor:** `CatVisualSetup`.
  - The clip list becomes data covering every A08 slot, and it builds the death table.
  - It wires `CatPresentationSignals` on `Cat_Player.prefab`. It stays re-runnable, and the prefab is never edited by
    hand.

## 6. Tests (red first)

- **`CatAnimStateMachineTests`** (extended): every transition in the table above. Also:
  - priority conflicts: death during Land, respawn during Death, flip during Climb, level complete during Death;
  - no Turn in the air;
  - fidgets only after `fidgetDelay`, in the fixed cycle look around → ear twitch → sit down;
  - sit down holds until input, and a move or jump in the control sample cancels any fidget on the same frame;
  - movement resets the cycle;
  - HardLand by distance along gravity in both gravities, with a mid-air flip resetting the high point;
  - Run and Walk hysteresis;
  - climbing down plays in reverse.
- **`CatVisualOnlyParityTests`** (new):
  - A scripted input sequence is replayed through the test rig twice, with the presenter enabled and disabled.
  - The inputs are pressed during Turn, Land, HardLand, Respawn, IdleFidget and TakeOff.
  - The cat's position and velocity must be identical on every tick.
- **`CatDeathClipTableTests`** (new):
  - Every entry reaches its held frame within the hold, computed from `RoomSafetyConfig` and `TickTime`.
  - An unmapped kind resolves to Default.
  - A killer without the interface, with a Fall cause, resolves to Pit.
  - The Respawn clip is ≤ 0.2 s.
- **`ClimbPoseTests`:** rewritten for the new placement without rotation. The test count must not drop; report the
  total against the baseline.
- **Death-kind tags:**
  - Each tagged trap reports its declared kind.
  - A per-object override wins over the declared kind.
  - `GeyserTrap` and an untagged `Hazard` report Default.
  - A built spike hazard reports Spiked and a built pit floor reports Pit.

## 7. Phase 1 (Lite): questions

1. How does the presenter on the shared `Cat_Player.prefab` reach this scene's `RoomDeath`, `RoomManager` and
   `CatRespawn` without scene wiring? Through the `ObserverContext` or the composition root, with no new singleton.
2. Does the level-complete screen set the time scale to 0 through `RunningState`? If so, Door needs unscaled time.
3. Observer B, Echo cats and `Level_Solo01`: confirm the signals are per observer, so B and Echo cats never show A's
   death.
4. Which existing tests pin the Climb pose or the presenter's fields, so they change with it?
5. Can the presenter read the observer's current control sample (move and jump) through `ObserverContext.Driver`
   without touching input code? This is needed for "any input cancels the fidget".
6. **Hazards have no prefab** (checked 2026-09-30): the builders add `Hazard` in code (`HazardSetup.cs`,
   `TrapKitSetup.Classic.cs`), and static spikes and pit floors are the same component. A pit floor kills with cause
   `Hazard`, not `Fall`. How do spikes resolve to Spiked and pit floors to Pit without the builders setting a kind on each
   placed object? This question comes before any code.
   **Answer (developer, 2026-09-30):** builders declare the kind in code, since they're the only place `Hazard` is
   created. When a builder adds a `Hazard`, it sets that hazard's `CatDeathKind`: the spike builder sets Spiked, the
   pit-floor builder sets Pit. Pit therefore comes from the hazard's kind, not from a Fall cause. It doesn't affect A08.

## 8. Allowed files

- `Assets/_Game/Core/Presentation/CatAnimState.cs`, `CatAnimStateMachine.cs`, `CatAnimInput.cs` (new).
- `Assets/_Game/Core/Rooms/CatDeathKind.cs` (new), `IDeathKindSource.cs` (new).
- `Assets/_Game/Gameplay/Presentation/CatVisualPresenter.cs`, `CatVisualConfig.cs`, `CatClipSet.cs` (new),
  `CatPresentationSignals.cs` (new), `CatDeathClipTable.cs` (new).
- `Assets/_Game/Editor/Art/CatVisualSetup.cs`.
- `Assets/_Game/Data/CatA_VisualConfig.asset` and `CatA_DeathClips.asset`, both written by the setup menu only.
- `Assets/_Game/Gameplay/Player/Cat_Player.prefab`, changed by the setup menu only.
- Tests: `CatAnimStateMachineTests.cs`, `ClimbPoseTests.cs`, `CatVisualOnlyParityTests.cs` (new),
  `CatDeathClipTableTests.cs` (new).

- `Assets/_Game/Gameplay/Rooms/RoomTrap.cs`, `HiddenSpikesTrap.cs`, `FallingBlockTrap.cs`, `StormCloudTrap.cs`,
  `ArrowTrap.cs`, `MovingTrap.cs`, `Hazard.cs`.
- The builders that create a `Hazard` (`HazardSetup.cs`, `TrapKitSetup.Classic.cs` and any other that adds one): they set
  its kind only.
- A tests file for the tags.

## 9. Out of scope

- The collider, motor (including `CatMotor2D`), routes, levels and tick timing.
- Launched, Dizzy, menu idle, and FedUp (PAX-V07b).
- The tail-flick fidget, the door-enter clip, the Splash kind, and any geyser kind.
- Setting death kinds per placed object in level builders, except `Hazard` (question 6).
- Cat B clips.
- The seamless vine.
- Audio and haptics.

## 10. Stop conditions

- **A state can only be shown by delaying input** or changing motion.
- **The parity test differs** on any tick.
- **A death clip can't reach its held frame** within the hold at a readable frame rate.
- **The presenter can't reach the signals** without a new singleton or scene wiring outside a setup menu.

## 11. Acceptance

**Editor:**
- EditMode tests green, and the total is at least the baseline plus the new tests.
- `refresh_unity`, `validate_script` and `read_console` are clean, apart from the known noise.

**YOU (Editor):**
- Play Trap Lab rooms 0–11 and L011–L020 in the Device Simulator.
- Every state reads at phone scale in both gravities, with no frame pops between clips.
- Deaths read within the hold, and each tagged trap shows its own death. A geyser death shows the frightened pose.
- Standing still plays look around, then ear twitch, then sit down. Touching the stick while seated ends the sit at once.
- Turning, landing and respawning never feel delayed.

**Device:** feel is unverified until Phase H.
