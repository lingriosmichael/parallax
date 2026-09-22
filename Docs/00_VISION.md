# PARALLAX — Vision

**Status:** Authoritative. Supersedes the original specification wherever they differ.
**Precedence:** `07_DECISIONS.md` > **this file** > `02_ARCHITECTURE.md` > `CLAUDE.md` > implementation plan > original spec.
**Last updated:** 2026-09-22 (death hold and out-of-bounds kill, D-058/D-059)

---

## 1. One line

**PARALLAX is a landscape mobile troll puzzle platformer. A small cat crosses a vast, surreal world
one room at a time, and every room is lying to it.**

## 2. Core fantasy

> **"I know what this room is going to do to me now."**

Each room looks simple: a checkpoint, a door, an obvious way across. The obvious way is a trap. The
player dies, laughs or swears, and tries again straight away, because the room behaves exactly the
same way every time. Winning means reading the room, not out-reflexing it.

## 3. Pillars

1. **The room is the puzzle.** Every room is a small puzzle with one idea. The obvious route is the
   setup, the betrayal teaches the rule, and the solution uses it.
2. **Fair betrayal.** Traps are deterministic (D-040): the same trigger does the same thing on every
   attempt. Nothing is random, nothing depends on frame timing. The room is the difficulty, never the
   controls.
3. **Cheap, funny failure.** Death briefly freezes the room exactly as it killed you — the gap,
   the revealed spikes, the block that landed — then resets and puts the cat back at the
   checkpoint, in ≤ 0.75 s total, with no fade, death screen or reload (D-041, D-058). The cats
   make mistakes charming: scrambling paws, ears back, surprised landings.
4. **One verb that changes everything.** Gravity flips between up and down, instantly (D-037,
   D-048). The same room read upside down is a different room, and the room can flip it on you.

## 4. How a room works

- **A room is a checkpoint and a door (D-040, D-050).** The cat starts at the checkpoint and has to
  reach the door. Touching the door completes the room and the cat appears at the next checkpoint.
  After the last door, the level is complete.
- **Anatomy of a room:** setup → obvious route → betrayal → learned solution. Every room should be
  describable in one sentence of that shape.
- **Death resets the room (D-041).** A short hold (0.5 s default, D-058) freezes the room in its
  fired state first, so the player sees what killed them; then the cat respawns at the room's
  checkpoint and every trap re-arms. Rooms already completed stay completed.
- **Leaving the room's bounds kills you too (D-058).** A hole in the layout is a death, not an
  endless fall — the room computes its own kill bounds from its geometry.
- **Unlimited retries** are the working assumption. A per-room death count is now tracked
  internally (D-058) but has no UI or persistence yet; lives stay undecided (D-044, proposed;
  Q-9).
- **Level-design constraint (D-050):** reaching a later room's checkpoint skips the current room,
  so room N+1's checkpoint must be unreachable before room N's door.

## 5. v1 mechanics

| Mechanic | What it is |
|---|---|
| **Move and jump** | Screen-relative movement (D-049): right is screen-right, even upside down. |
| **Gravity flip** | Gravity is up or down only, flipped instantly by the player (FLIP / Q) or by a trap. No wall gravity, no tilt (D-045). |
| **Hazards** | Touching one kills the cat and resets the room. |
| **Trap kit v1** | Collapsing floor, hidden spikes, falling block, door that moves away, gravity-flip trap (PAX-042). Each trap is armed or fired and re-arms on death. |
| **Checkpoints and doors** | One checkpoint and one door per room. |

## 6. Controls

- **Touch:** floating stick (left), jump button, FLIP button (top-right).
- **Keyboard (Editor):** A/D or arrows to move, Space to jump, F to interact, Q to flip gravity.
- **Camera:** world-aligned; it never rotates with gravity (D-020).
- **Orientation:** landscape, locked.

## 7. Characters and tone

A small cat in a vast, serious, surreal world. It is not childish. Its small scale makes the
architecture feel enormous. It is expressive and physical, and its failures are funny rather than
punishing. The world plays tricks on the cat; the game never plays tricks on the player's hands.

## 8. Visual direction

Reference: `Docs/Art/keyart_north_star.png`.

- **v1 is Reality A:** sandstone, roots, ruins, cloth banners, vegetation, floating architecture,
  clouds, warm golden light, dark reflective water.
- **Reality B** (co-op update): obsidian, glass, grids, cyan edge light, void, monoliths,
  wireframes, cold mist, stars.
- **In-game style:** evokes the key art with fewer layers, stronger silhouettes, and readable
  contrast at phone scale. The key art's painterly depth is a marketing target, not an in-game
  requirement.
- **Traps must read at phone scale after the betrayal.** Before it, a trap may hide; once it has
  fired, the player must be able to see what happened.
- **Artwork never decides collision.** Invisible colliders define gameplay surfaces.

## 9. Audio and haptics (brief)

Warm, organic ambient bed for Reality A. Every trap firing gets a sound and a haptic cue, so the
betrayal is felt as well as seen. Death and respawn are short and light, never punishing.

## 10. Scope

### In v1
Android · landscape · solo · one cat · Reality A · rooms (checkpoint + door) · hazards and room
reset · trap kit v1 · up/down gravity flip · a short run of greybox rooms first, art after
validation.

### Not in v1
Co-op and all networking (co-op update) · Reality B as a play space · Echo and the reality switch as
player features (dev tooling only, D-038) · Control Station and gravity dial · tilt (removed, D-045)
· iOS · voice · matchmaking · payments · cloud saves · cosmetics · achievements · localization ·
store pages and trailers.

### Removed from the original specification
3D environments · 3D humanoids · the photo-to-3D-avatar cloud pipeline · 3D mesh portals. Any section
of the original spec describing these is void.

## 11. The co-op update (later, parked)

Co-op returns after v1 (D-047). The earlier co-op vision is kept here as direction, not as v1 scope:
two players, two cats, two contradictory realities of the same world, each phone showing only its
own reality. Anchors link the worlds (a vine in one is an elevator in the other), and the core line
was **"What do you see?"**: players solve rooms by describing worlds the partner can't see.

Rules already agreed for it: cross-reality traps change state and never demand timing (D-043); a
death resets both cats (D-042, deferred). All co-op code (Reality B, Echo, switch, Control Station,
cross-reality anchors, transport, PAX-028–035) stays in the repo, untouched and frozen.

## 12. Open questions (resolve by playtest, record in `07_DECISIONS.md`)

1. **Lives and death counter (D-044, Q-9):** unlimited retries with a per-room death counter, or
   something else?
2. **Cat collider height (Q-10):** 0.8 today vs art ~0.6 (proposed PAX-A04, ~0.62). Settle before
   the first real rooms (PAX-043); it sets every jump and gap.
3. **Screen-relative movement on device (D-049):** confirm on the Pixel 8a in PAX-037.
4. **Partner presence hint (Q-1):** co-op, parked.

## 13. Success criteria

The first greybox rooms (PAX-043) succeed when, across 6–8 solo playtesters:
- players die to a room's betrayal and retry immediately rather than put the phone down;
- after a death, players can say what the room did to them;
- players blame the room, not the controls;
- players ask **"Is there another room?"**

If these don't happen, no further rooms are built until the game is fixed.
