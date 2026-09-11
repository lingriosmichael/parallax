# PARALLAX — Vision

**Status:** Authoritative. Supersedes the original specification wherever they differ.
**Precedence:** `07_DECISIONS.md` > **this file** > `02_ARCHITECTURE.md` > `CLAUDE.md` > implementation plan > original spec.
**Last updated:** 2026-09-10

---

## 1. One line

**PARALLAX is a landscape mobile puzzle adventure for one or two players. Two cats inhabit two contradictory realities of the same world, and what you do in yours changes theirs.**

## 2. Core fantasy

> **"Your friend controls your reality."**

In co-op you never see your partner's world. You only see its consequences in yours, and you have to talk to understand them. In solo, you move between both worlds and cooperate with your own past self.

## 3. Pillars

1. **"What do you see?"** Communication is the core mechanic. Puzzles are solved by describing contradictory worlds in qualitative language.
2. **"I did that to you."** Cause and effect crosses realities, and crosses phones. It must feel immediate, legible, and a little theatrical.
3. **Cheap, funny failure.** Instant respawns, no death screens. The cats make mistakes charming: scrambling paws, ears back, surprised landings.
4. **Two worlds, one truth.** Both realities are manifestations of one logical world. A vine in one is an elevator in the other. The player learns the mapping by experimenting.

## 4. Player model

There are always exactly **two logical Observers**, A and B, however many humans are playing.

| | Observer A | Observer B |
|---|---|---|
| Cat | Cat A | Cat B |
| Reality | **Reality A:** warm, organic | **Reality B:** cold, geometric |
| Controlled by | a human, an Echo, or nobody | a human, an Echo, or nobody |

### Co-op (flagship)
Each human controls one Observer on their own phone. Each phone shows **only its own reality**, full-screen. Players talk by voice (an external call during the prototype).

### Solo (first-class)
One human switches control between A and B. Only one reality is on screen at a time; **there is never a permanent split-screen**. The **Echo** lets the player record a short sequence as one cat, switch, and have that cat replay it while they control the other.

## 5. The spatial model (player-facing)

- Each cat lives in **its own reality with its own geography**. Platforms, walls, and routes differ.
- The realities are linked only through **anchors**: objects that exist in both worlds in different forms (vine ↔ elevator, statue ↔ monolith, sundial ↔ gravity console).
- **The cats never physically meet** during normal play. By default they cannot see each other. (A faint "presence" hint is an open question; see §12.)
- The finale is the one moment the realities **converge**. The split composition from the key art appears on both screens.

## 6. v1 mechanics (vertical slice)

| Mechanic | What it is | Solo |
|---|---|---|
| **Anchor Move** | Changing an anchor in one reality changes its counterpart in the other | Native |
| **Gravity Shift** | A cat at a **Control Station** steers the *other* cat's gravity by tilting the phone or turning an on-screen dial | Adapted (persistent setting, or Echo-recorded choreography) |
| **Perspective / Shadow** | An object positioned in Reality A casts a "shadow" that becomes solid geometry in Reality B | Native or Adapted |
| **Echo** (solo) | Record ≤10 s as one cat, switch, and the Echo replays and holds its final state | Solo core |

Each puzzle is tagged `COOP: Native/Exclusive` and `SOLO: Native/Adapted`.

## 7. Controls

- **Movement:** touch left / right / jump-interact.
- **Gravity control:** tilt (roll) *or* a touch dial, chosen in settings. Only used while seated at a Control Station, so the hands are free.
- **Solo:** SWITCH and RECORD buttons.
- **Orientation:** landscape, locked.

## 8. Characters and tone

Two small cats in a vast, serious, surreal world. They are not childish. Their small scale makes the architecture feel enormous. They are expressive and physical, and their failures are funny rather than punishing. The warm-reality cat and the cold-reality cat look different but have **identical gameplay dimensions**.

## 9. Visual direction

Reference: `Docs/Art/keyart_north_star.png`.

- **Reality A:** sandstone, roots, ruins, cloth banners, vegetation, floating architecture, clouds, warm golden light, dark reflective water.
- **Reality B:** obsidian, glass, grids, cyan edge light, void, monoliths, wireframes, cold mist, stars.
- **In-game style:** evokes the key art with fewer layers, stronger silhouettes, and readable contrast at phone scale. The key art's painterly depth is a marketing target, not an in-game requirement.
- **The split composition** (both realities side by side) is used for the finale, the store page, and trailers only. It is never the normal gameplay view.
- **Artwork never decides collision.** Invisible colliders define gameplay surfaces.

## 10. Audio and haptics (brief)

Each reality has its own ambient bed: warm/organic vs. cold/tonal. Cross-reality causality gets a sound and a haptic cue on the *causing* phone immediately and on the *receiving* phone on arrival. The finale is a synchronized musical and haptic hit on both devices.

## 11. Scope

### In v1 (prototype → vertical slice)
Android · landscape · 1–2 players · two cats · two realities · anchors · gravity control (tilt + dial) · perspective/shadow puzzle · solo switching + Echo · invite-code co-op via Photon Fusion 2 · checkpoints · reconnect handling · a 10–15 minute slice · greybox first, art after validation.

### Not in v1
iOS · built-in voice · random matchmaking · payments / friend pass · deep links · cloud saves · cosmetics · achievements · localization · chapters beyond the slice · trailers/store pages.

### Removed from the original specification
3D environments · 3D humanoids · the photo-to-3D-avatar cloud pipeline · 3D mesh portals. Any section of the original spec describing these is void.

## 12. Open questions (resolve by playtest, record in `07_DECISIONS.md`)

1. **Presence hint:** should each player see a faint shimmer where the partner's cat corresponds in their reality, or nothing at all?
2. **Camera and gravity:** should the camera rotate when a cat's gravity changes, or stay world-aligned?
3. **Tilt vs. dial:** which do players prefer? Is tilt worth keeping as the default?
4. **Solo gravity adaptation:** persistent setting, Echo choreography, or both?
5. **Echo length:** is 10 seconds right? Should Echoes loop for some puzzles?
6. **Same-room play:** does peeking at the partner's screen hurt or help?

## 13. Success criteria

The vertical slice succeeds when, across 10 co-op pairs and 6–8 solo players:
- pairs spontaneously ask **"What do you see?"**, laugh at cross-reality effects, grasp cause and effect within one or two attempts, and ask **"Is there another level?"**;
- solo players find Echo puzzles clever rather than tedious and would play solo again.

If these don't happen, no further chapters are built until the game is fixed.
