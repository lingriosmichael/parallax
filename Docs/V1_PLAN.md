# PARALLAX — v1 definition and roadmap

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
