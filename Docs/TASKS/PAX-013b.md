# PAX-013b — Virtual stick + jump button

**Status:** Open
**Replaces:** PAX-013 three-zone touch scheme
**Blocks:** Gate 2
**Related:** D-020 (camera stays world-aligned), Q-2 (wall-walking readability), Q-7 (Phase 5 rotating camera)

---

## Why

PAX-013 shipped three rectangular zones — MoveLeft, MoveRight, Jump — so `CatCommand.Move` is only ever −1, 0 or +1. The intended control was an analog stick. This ticket replaces the move zones with a floating virtual stick in the bottom-left and keeps a jump button on the right.

The stick also opens a third answer to Q-2. With buttons, movement can only be cat-relative: "left" means "left along the surface the cat is standing on", which on a wall reads as *down the screen*. A stick can instead be read in screen space and projected onto the cat's ground axis — push right, cat goes right on screen, regardless of gravity. Both modes are built here behind a serialized enum so Test 11 becomes a direct comparison rather than a single verdict.

---

## Claude Code implements

### 1. `VirtualStick` — `Parallax.Core`, pure static class, no MonoBehaviour

```csharp
public enum StickProjection { CatRelative, ScreenRelative }

// Returns stick vector in screen space, magnitude 0..1.
// Zero inside deadZone; rescaled so deadZone edge = 0 and radius = 1.
static Vector2 Evaluate(Vector2 origin, Vector2 current, float radius, float deadZone)

// Collapses the 2D stick to CatCommand.Move in [-1, 1].
// CatRelative:    returns stick.x
// ScreenRelative: returns Dot(stick, catRight), clamped
static float ToMove(Vector2 stick, Vector2 catRight, StickProjection mode)
```

`catRight` is the cat's local right axis in world space. Because the camera is world-aligned and never rotates (D-020), world orientation equals screen orientation, so no camera transform is needed in the projection.

> **Comment this in the source.** It is load-bearing and it breaks if Q-7 ever rotates the camera.

**Tests** — follow the existing `TouchZones` / `CameraMath` pattern:

- zero inside dead zone
- magnitude 1 at and beyond radius
- correct rescale at the midpoint between dead zone and radius
- diagonal input clamped to unit length
- `origin == current` returns zero
- `ToMove` CatRelative returns `stick.x` unchanged
- `ToMove` ScreenRelative with `catRight = (1,0)` returns `stick.x`
- `ToMove` ScreenRelative with `catRight = (0,1)` (cat on a wall) returns `stick.y`
- `ToMove` ScreenRelative with `catRight = (0,1)` and stick pushed right returns ~0
- result clamped to [−1, 1] for all inputs

### 2. `TouchStickCatInput` — `Parallax.Gameplay.Input`, implements `ICatCommandSource`

**Serialized fields**

| Field | Type | Notes |
|---|---|---|
| `stickZone` | `Rect` | normalised safe-area coords |
| `jumpZone` | `Rect` | normalised safe-area coords |
| `stickRadius` | `float` | fraction of screen height |
| `deadZone` | `float` | fraction of radius |
| `projection` | `StickProjection` | flipped in Inspector for Test 11 |
| `gravityReceiver` | `GravityReceiver` | source of the cat's up axis |

**Behaviour**

- **Floating origin.** A finger that Begins inside `stickZone` records its start point as the origin; the stick vector runs from there. No fixed base is drawn — the thumb should not have to find it.
- **Derive `catRight` from `GravityReceiver`**, not `transform.right`:
  `up = -gravityDirection`, `right = (up.y, -up.x)`.
  Verify the actual `GravityReceiver` API before wiring — do not assume the property name.
- **Jump** keeps the existing latch-and-clear edge so a tap is never lost between physics ticks.
- **Reuse the finger reconciliation from the current `TouchCatInput`** — rebuild active IDs from `Touch.activeTouches` each Update and prune anything the OS stopped reporting. Same bug class, same fix.
  **Do not reintroduce counters that depend on matched Began/Ended pairs.**
- **Dev-only `OnGUI` overlay:** stick origin, current point, radius ring, live `Move` value, and the current projection mode as text. The mode needs to be readable on screen during Test 11.

### 3. Removals

- **Delete `TouchCatInput`.** Two touch sources feeding `CatInputRouter` would fight. Git history keeps it.
- **Keep `TouchZones`** — still needed for the jump rect hit test. Drop only the tests covering the move zones.

### 4. Check before finishing

`Move` is now continuous, not −1/0/+1. Grep `CatMotor2D` and everything downstream for `Move != 0`, `Mathf.Sign(Move)`, or equality comparisons. Anything assuming three discrete values needs a threshold instead.

`KeyboardCatInput` is unchanged and still emits −1/0/+1.

---

## Developer wires in Unity

- Extend the `TouchInputSetup` menu to add `TouchStickCatInput` with the `GravityReceiver` reference and sane default rects.
- Swap the component on the cat; repoint `CatInputRouter`'s source list.
- Starting values to tune on device:
  - stick zone ≈ bottom-left quarter
  - `stickRadius` ≈ 0.12 of screen height
  - `deadZone` ≈ 0.15

---

## Definition of Done

**Editor**

- [ ] All EditMode tests pass, including the new `VirtualStick` tests.

**On the Pixel 8a (development build, `adb logcat -s Unity` running)**

- [ ] Analog movement confirmed — a half-pushed stick visibly walks slower than a full push.
- [ ] Jump works simultaneously with the stick.
- [ ] Backgrounding the app mid-touch leaves nothing stuck (power button or notification shade while holding the stick).

**Test 11, run twice**

- [ ] Wall-walk with `CatRelative`.
- [ ] Flip `projection` to `ScreenRelative` in the Inspector, wall-walk again.
- [ ] Written verdict: which mode would you rather play a whole level with, and is the perpendicular dead spot in `ScreenRelative` a real problem or a curiosity?

That verdict is the Q-2 answer and decides whether Q-7's rotating camera stays a Phase 5 maybe.

**Commit**

- [ ] Final tuned stick values in the commit message, alongside the camera dead-zone and smooth-time values still owed from PAX-012.
- [ ] Push — the branch is currently ahead of origin.

---

## Consequences

- Gate 2 slips by roughly one session.
- PAX-013 acceptance tests 5, 6 and 8–10 are partly obsolete; they tested move zones that no longer exist.
- Tests 7 (twenty taps, twenty jumps) and 11 (wall-walk) survive and still matter.
