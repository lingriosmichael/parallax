# PARALLAX · What's left (updated 2026-10-31)

Read this first. Finished tickets are removed from `Docs/`; decisions stay in `07_DECISIONS.md`, and the rest is in git history.
**v1 is:** Android, solo, paid unlock (levels 1–10 free). The finish line is a Play Store release (PAX-071). iOS and co-op come after.

## Where it stands

| Area | State |
|---|---|
| Levels | **40 built.** L001–L030 Golden Ruins, L031–L040 Obsidian Void. All pass validation; L031–L040 and much of L021–L030 are still to be played and approved by you |
| Traps, audio, music, haptics, paid unlock, title | Built. Audio, haptics, unlock and title await your Editor steps below |
| Art | Worlds finished. Some traps are still grey placeholders (16 paintings), and the cat has known animation gaps |
| On a real phone | **Nothing tested yet.** No device session has happened |
| Git | **Nothing is committed since `6ac4b1d`.** L031–L040, the Obsidian art and everything from 2026-10-31 exist only on this Mac. Commit soon |

## Do next: you (about an hour, no new code)

1. **Commit.** Say the word and Claude commits and pushes it all.
2. **Run the setup menus,** in this order, saving first:
   1. `PARALLAX/Setup/Audio (PAX-064)` (music, and the cat's meows, which main still lacks)
   2. `PARALLAX/Setup/Haptics (PAX-065)`
   3. `PARALLAX/Setup/Paid Unlock (PAX-066)`
   4. `PARALLAX/Setup/Levels/Rebuild All Levels`
   5. Open `Title.unity`, run `PARALLAX/Setup/Title Screen`, then Cmd+S
3. **Look and listen:** the music (does it fit?), the title's motion, L031's swinging chaser, the unlock panel from L011.
4. **Play L031–L040** and say what's fun and what isn't.

Steps in detail: `0_TASKS/PAX-125.md` §6, `PAX-065.md` §9, `PAX-066.md` §10, `PAX-108.md` amendment 2.

## Decide: only you can

1. **How many levels ship?** 40 are built, the plan says 50, and Obsidian Void has room up to 60. This sets how much is left more than anything else. (`PAX-063` is levels 41–50.)
2. **A settings screen?** D-062 planned one (music, sound, vibration). Today there's only Vibration and Restore on the pause menu. Recommended: a small one, or drop it on purpose.
3. **`PAX-A12`:** polish of L011–L020 and the Trap Lab. Much of it was done by later art work. Keep it or close it?
4. **`PAX-V07b`:** parked cat polish. Keep it or close it?

## Build: in this order

| # | Ticket | What | Who |
|---|---|---|---|
| 1 | `PAX-063` | Levels 41+, if you want more (Decide 1) | Claude + your play tests |
| 2 | Trap art | 16 paintings: `Art/GoldenRuins_NEW_TRAP_PROMPTS.md` (8) and `Art/ObsidianVoid_TRAP_PROMPTS.md` (8, OT-19 first); Claude imports them | **You paint**, Claude imports |
| 3 | Android back | Level select → title; title → "Quit?" (D-157 open point). Needs a new ticket | Claude |
| 4 | `PAX-A09` | Rest of the UI art: level select, in-level HUD, styled stick | Claude |
| 5 | `PAX-V06` | Game feel: squash and stretch, dust, camera impulse, death and level-complete beats | Claude |
| 6 | `Art/NEEDED_ASSETS.md` | Cat animation gaps: run brake, landing contact, falling loop and others. Pick the ones worth painting | **You paint**, Claude imports |
| 7 | `PAX-098` | Validator rule: head clearance above walkable surfaces | Claude |
| 8 | `PAX-A10` | App icon, feature graphic, screenshots | Both |
| 9 | `PAX-067` | Signed release build and build checks | Claude + your Player settings |
| 10 | `PAX-V03` | Performance budget on the Pixel 8a (the look itself is done) | Both, on the phone |

**Order:** items 2 and 6 are art, so they can run alongside the rest and block nothing.

## Then release

| # | Ticket | What |
|---|---|---|
| 11 | Device session | The whole game on the Pixel 8a: touch, feel, sound, haptics, frame rate |
| 12 | `PAX-069` | Retune the hard levels to device numbers |
| 13 | `PAX-068` | Store listing and compliance; **create `full_unlock` at 4.99 in Play Console** |
| 14 | `PAX-070` | Closed test track: blind playtest, and the first real purchase test |
| 15 | `PAX-071` | **Production release on Google Play** |
| Later | `PAX-072` | iOS. Then the co-op update (D-047) |

## Start early (not tickets)

- **Google Play account:** if it's a personal account made after 13 Nov 2023, production access needs a closed test with at least 12 testers for 14 straight days. Recruit 15–25 people well
  before item 14. An organization account (D-U-N-S) is exempt.
- **Privacy policy URL:** needed for the listing even though the game collects no data.

## Small clean-ups

- `CLAUDE.md`'s scope list still says "no IAP". It should go now that PAX-066 is approved and built.
- `Docs/PARALLAX_detailed_implementation_plan copy.md`: `CLAUDE.md` names it as a source (without "copy"), so it stayed, but it's long out of date. Your call.
- `Docs/INVENTORY.md` (the Level Devil research) stays as a source of level ideas.
