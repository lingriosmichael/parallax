# PARALLAX — v1 roadmap (after PAX-050)

Rev. 2026-09-23, per D-062, D-064 and D-069: build everything first, test once at the end.
Tickets run one at a time in this order unless noted. Numbers after PAX-059 may shift if a
kit-gap ticket is inserted during Phase D.

### Phase B · One-room levels, flow and trap kit

| Order | Ticket | What | Decision |
|---|---|---|---|
| ✓ | PAX-051 | One-room level authoring pipeline, L001–L004 seeds | D-066 |
| 1 | PAX-052 (refreshed) | Level camera: follows the cat through rooms wider than one screen, keeps the next landing and trap reveals on screen. Scaffolding menus target `_LevelTemplate`; `Rebuild All Levels` regenerates each scene as template + layout. Test reading baked `RoomManager` bounds from the `Level_00N` scenes. Background placement per level. | — |
| 2 | PAX-053 | Level select | — |
| 3 | PAX-054 | Pause menu | — |
| 4 | KIT-1 (PAX-073) | Trap fixes and platform geometry: trigger zones cover every approach to the danger, including jump arcs (fixes the falling ceiling you can jump past); thin, narrow floating platform element; validator rule for trigger coverage. | — |
| 4a | PAX-077 | Tick rate: one source, 50 Hz. `TickTime` seam for every seconds↔ticks conversion, guard test pinning the 0.02 s fixed step, door clearance derived as one tick at run speed. No tick-count threshold changes. | D-075 |
| 4b | PAX-078 | 50 Hz timing gaps: L002 Lift slack (10.88 < 12 ticks) and the L004 final room (Block_1 vs Flip_A). Removes the two PAX-078 60 Hz pins in `SoloRoomsLayoutTests`. | D-075 (7) |
| 5 | KIT-2 (PAX-074) | Arrow trap, fired left→right and right→left: deterministic (zone/jump-triggered or periodic), visible tell ≥ 6 ticks (D-057), door clearance (D-060), validator support. | — |
| 6 | KIT-3 (PAX-075) | Troll-route sections: 10–12-platform sections with several routes, some failing; validator proves at least one valid route. | — |
| 7 | KIT-4 (PAX-076) | Precision sections: section marker in the element types, per-section validator thresholds, first precision level, Pixel 8a play session. | D-069 as built |
| 8 | PAX-057 | Tiers: novice / hard configs, including precision thresholds from the KIT-4 device session | D-065 |
| 9 | PAX-058 | Bounded randomness | D-067 |

**Decision placeholders:** D-065 hard-tier numbers (now also precision thresholds) · D-066 level
format ✓ · D-067 randomness · D-068 free levels and price · D-069 precision sections ✓

KIT-1–KIT-4 take PAX-073–PAX-076 (the next free PAX numbers at time of writing), ordered ahead of
PAX-057 in this phase's build order even though their numbers are higher; PAX-055 and PAX-056
(the old "Trap kit v3a/v3b" plan) are superseded by KIT-1–KIT-4 above and are not used.

| Phase | Ticket | What | Depends on |
|---|---|---|---|
| B · Levels & flow | PAX-050 | Level list, next level, progress save (in progress) | PAX-049 |
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
