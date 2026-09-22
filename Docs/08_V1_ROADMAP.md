# PARALLAX — v1 roadmap (after PAX-050)

Rev. 2026-09-22, per D-062 and D-064: build everything first, test once at the end.
Tickets run one at a time in this order unless noted. Numbers after PAX-059 may shift if a
kit-gap ticket is inserted during Phase D.

| Phase | Ticket | What | Depends on |
|---|---|---|---|
| B · Levels & flow | PAX-050 | Level list, next level, progress save (in progress) | PAX-049 |
| | PAX-051 | One-room level authoring pipeline; Level_001–004 seeds | PAX-050 |
| | PAX-052 | Single-room camera framing | PAX-051 |
| | PAX-053 | Menu scene: title screen + level select | PAX-051 |
| | PAX-054 | Pause menu + settings (+ settings store) | PAX-053 |
| C · Kit v3 & hard tier | PAX-055 | Trap kit v3a: arrow launchers | PAX-051 |
| | PAX-056 | Trap kit v3b: carrying platforms | PAX-055 |
| | PAX-057 | Difficulty tiers + provisional hard-tier rules (D-065) | PAX-056 |
| | PAX-058 | Bounded randomness: seeded trap variants (D-066) | PAX-057 |
| D · Content | PAX-059 | Levels 1–10 (novice) | PAX-052, PAX-057 |
| | PAX-060 | Levels 11–20 (hard, batch 1) | PAX-058, PAX-059 |
| | PAX-061 | Levels 21–30 (hard, batch 2) | PAX-060 |
| | PAX-062 | Levels 31–40 (hard, batch 3) | PAX-061 |
| | PAX-063 | Levels 41–50 (hard, batch 4) | PAX-062 |
| | (as needed) | Kit-gap tickets: chained flips, rearming doors, queued chains… | raised by a batch |
| E · Art | PAX-A06 / A07 | Reality A environment, trap art (already planned) | — |
| | PAX-A08 | Remaining cat animations (death, flip, jump set) | — |
| | PAX-A09 | UI art: title, level select, buttons, font | PAX-054 |
| | PAX-A10 | App icon + store graphics | PAX-A06 |
| F · Audio & haptics | PAX-064 | Audio: mixer, ambient bed, trap/death/UI cues, volume wiring | PAX-054 |
| | PAX-065 | Haptics: trap and death cues, toggle | PAX-064 |
| G · Release prep | PAX-066 | Paid unlock (Unity IAP), locked levels, restore | PAX-053, D-067 |
| | PAX-067 | Release build: signed AAB, stripping, build checks | PAX-066 |
| | PAX-068 | Store listing and compliance (developer checklist) | PAX-A10, PAX-067 |
| H · Validate (moved here, D-064) | PAX-037 | Device session, whole game on the Pixel 8a | all above |
| | PAX-069 | Hard-tier retune to device numbers (D-065 final) | PAX-037 |
| | PAX-070 | Closed test track = blind playtest, Gate 3 | PAX-069 |
| | (as needed) | Fix tickets from PAX-037 / PAX-070 | — |
| | PAX-071 | Production release on Google Play | PAX-070 |
| I · iOS | PAX-072 | iOS port (outline only) | PAX-071 |
| Later | — | Co-op update (D-047) | — |

Phase E (art) and the art tickets can run alongside B–D, as today; they don't block code.

## Decisions still to write

- **D-065** (in PAX-057): provisional hard-tier slack and max jump.
- **D-066** (in PAX-058): randomness as built.
- **D-067** (before PAX-066): which levels are free, and the price.

## Lead-time items to start early (not tickets)

- Google Play developer account: check whether it is a **personal** account created after
  13 Nov 2023. If so, production access needs a closed test with ≥ 12 testers opted in for 14
  continuous days first; recruit 15–25 people well before Phase H. An organization account
  (D-U-N-S) is exempt.
- Privacy policy URL (needed for the listing even with no data collected).
- Audio source material (licences) for PAX-064.
