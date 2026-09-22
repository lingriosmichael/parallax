# PARALLAX — v1 definition and roadmap

Draft, 2026-09-22. The decision goes into `Docs/07_DECISIONS.md`; the roadmap can live as
`Docs/08_V1_ROADMAP.md`. Items marked **OPEN** are for the developer to fill in.

---

## D-062 · 2026-09-22 · Proposed

**Decision:** v1 definition.

1. **Content.** Solo v1 ships **50 levels**. A level is **one room** (Level Devil style): one
   checkpoint, one door, one screen or close to it. Levels 1–10 are the **novice tier**;
   levels 11–50 are the **hard tier**.
2. **Novice tier** keeps every current rule: deterministic traps (D-040), D-056's slack and
   reachability, D-057's lead, D-060's door clearance. Novice rooms are real troll rooms, not
   tutorials (D-053).
3. **Hard tier** is extremely hard and uses three sources of difficulty:
   - **Memory:** longer chains of betrayals, the same deterministic rules as novice.
   - **Execution:** tighter timing and jumps than D-056 allows. Hard-tier slack and reach limits
     are set after the device session (PAX-037), because touch precision on the phone decides
     what "tight but fair" means. **OPEN:** hard-tier slack (ticks) and max jump (fraction of
     reach).
   - **Luck:** bounded randomness, under these guardrails:
     - randomness only chooses between authored variants of a trap (e.g. which side the arrows
       come from, which floor tile collapses), never timing, speed or hitbox size;
     - every variant is survivable on its own and passes the hard-tier rules;
     - the chosen variant is visible at least D-057's lead before it can kill;
     - it is seeded per attempt from the level id and attempt number, and the seed is logged, so
       any death can be reproduced in the Editor;
     - novice levels never use it.
4. **Flow:** title screen, level select (locked until the previous level is cleared, best death
   count shown), level complete with Next level and Restart, pause (resume, restart, level
   select), settings (music and sound volume, haptics, touch stick size/position), progress
   saved on the device (unlocked levels, best death counts). No cloud saves.
5. **Business model:** free download with a one-time paid unlock. **OPEN:** which levels are
   free (proposal: the 10 novice levels plus the first 5 hard levels) and the price. No ads, no
   consumables.
6. **Platforms:** Android on Google Play for v1. iOS on the App Store follows as its own phase
   after the Android release.
7. **Audio and haptics** are in v1: an ambient bed, a sound and haptic cue for every trap
   firing, short death/respawn sounds (Vision §9).

**Why:** A finite, written finish line turns "until the game is done" into a countable list.
One-room levels keep 50 levels achievable for a solo developer. The hard tier's rules are
bounded so that deaths still read as the room's fault, which is what keeps players retrying.

**Supersedes / amends:** D-040's determinism and D-056's limits apply to the novice tier only;
the hard tier gets its own rules (above). Vision §10 (store pages, iOS, payments now in scope as
stated). `CLAUDE.md`'s "no IAP" scope line lifts when the monetization ticket starts.

**Consequence:** `00_VISION.md` §3 (pillar 2), §10 and §13, and `CLAUDE.md`'s scope section are
updated to match. The playtest (PAX-037) still gates content: no levels are built beyond the
current prototype until it passes.

---

## Roadmap

Phases run in order; tickets inside a phase can be reordered. Numbers are placeholders.

### Phase A — Finish validation (now)
- PAX-049 level complete and death count (in progress).
- Blind playtest + PAX-037 device session → Gate 3 solo verdict. Deaths per room, what killed
  them, touch feel, D-049 screen-relative controls.
- Fix tickets from the playtest.
- **Decide:** hard-tier numbers (D-062 OPEN items), from the device session.

### Phase B — One-room levels and flow
- Level structure: a level is one room; how 50 levels load (one scene per level, or one scene
  loading layout data by level id; decided in the ticket, D-054 favours data).
- Level complete → Next level; progress save (unlocks, best deaths).
- Title screen, level select, pause, settings.
- Level-authoring workflow: making a new room from data must be fast, since 50 are needed.

### Phase C — Trap kit v3 and hard-tier rules
- Reveal-on-fire moving hazards (arrows from left and right).
- Sideways carrying platforms.
- Other kit gaps as the levels need them (chained flips, rearming doors, queued chains…).
- Hard-tier rules as tests: tighter slack/reach, and the randomness system (seeded variants,
  telegraph, logged seed) with its own layout tests.

### Phase D — Content
- Levels 1–10 (novice), then 11–50 in batches of about 10, each batch playtested before the
  next. This is the longest phase.

### Phase E — Art
- Reality A environment, trap art and animation (PAX-A06/A07), remaining cat animations, UI art,
  app icon.

### Phase F — Audio and haptics
- Ambient bed, per-trap sound and haptic cues, death/respawn, UI sounds, volume settings wired.

### Phase G — Monetization and Android release
- Paid unlock with Unity IAP (Google Play Billing), restore purchase, locked-level UI.
- Performance pass on a mid-range Android phone; release build (signed AAB); D-033 check that
  MCP is not in the build.
- Store listing, screenshots, privacy policy, content rating, data-safety form.
- Closed testing track on Google Play before production (check the current requirements for
  new developer accounts), then release.

### Phase H — iOS
- Mac + Xcode build, Apple Developer Program, StoreKit through Unity IAP, iOS safe areas and
  touch check, App Store review.

### Later — Co-op update (parked, D-047)
