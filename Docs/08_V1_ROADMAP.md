# PARALLAX · What's left (updated 2026-10-31)

Read this first. Finished tickets are removed from `Docs/`; decisions stay in `07_DECISIONS.md`, and the rest is in git history.
**v1 is:** Android, solo, paid unlock (levels 1–10 free). The finish line is a Play Store release (PAX-071). iOS and co-op come after.

## Where it stands

| Area | State |
|---|---|
| Levels | **40 built.** L001–L030 Golden Ruins, L031–L040 Obsidian Void. All pass validation; L031–L040 and much of L021–L030 are still to be played and approved by you |
| Traps, audio, music, haptics, paid unlock, title | Built and in every scene. Not yet judged in play |
| Art | Worlds finished. All traps are painted. The cat's animation gaps are filled with stand-ins (D-293), except C-17. Store art is still to do |
| On a real phone | **Nothing tested yet.** No device session has happened |

## Do next: you (no new code)

0. **Premium pass, game UI and orbs (D-281, D-282, D-283, D-284):** let Unity import the new art, run `PARALLAX/Setup/Levels/Environment Stack`, then `PARALLAX/Setup/Levels/Game UI (PAX-A09)`, then `PARALLAX/Art/Import Trap Kit`, then Rebuild All Levels.
1. **Look and listen:** the music (does it fit?), the title's motion, L031's swinging chaser, the unlock panel from L011.
2. **Play L031–L040** and say what's fun and what isn't.

**To try the unlock in the Editor:** set Editor Purchase in `Data/IapConfig` to Succeed, Cancel, Fail, Defer or StoreDown, then open L011 from level select, or press Next level on L010.
Setting `waitHours` to 0.01 opens the wait in 36 s. Buying saves for real, so delete `PARALLAX_FULL_UNLOCK` from PlayerPrefs to lock it again.

## Decide: only you can

1. **How many levels ship?** Decided: 150, 30 per world (D-302). 40 are built. Open: that's five worlds and six are painted. (`PAX-063` is levels 41+.)
2. **A settings screen?** Decided: a small one (D-314), from the title: music, sound, vibration and motion.
3. **`PAX-A12`:** polish of L011–L020 and the Trap Lab. Much of it was done by later art work. Keep it or close it?
4. **`PAX-V07b`:** parked cat polish. Keep it or close it?

## Build: in this order

| # | Ticket | What | Who |
|---|---|---|---|
| 1 | `PAX-063` | Levels 41+, if you want more (Decide 1) | Claude + your play tests |
| 2 | Trap art | All 15 paintings are in the game (D-292) | Done |
| 3 | Android back | Level select → title; title → "Quit?" (D-157 open point). Needs a new ticket | Claude |
| 4 | `PAX-A09` | Only settings is left, and it waits on Decide 2; the rest is built (D-283) | Claude |
| 5 | `PAX-V06` | Game feel: squash and stretch, dust, camera impulse, death and level-complete beats | Claude |
| 6 | `Art/CAT_AND_STORE_PROMPTS.md` | Cat animation gaps (from `Art/NEEDED_ASSETS.md`): D-293 covers them from the cat's own frames, as stand-ins; only C-17 has nothing to build from. Paint any that look weak in play | **You paint**, Claude imports |
| 7 | `PAX-098` | Validator rule: head clearance above walkable surfaces | Claude |
| 8 | `PAX-A10` | App icon and feature graphic done (D-296). Only screenshots are left, once the art is final | Both |
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
- `ProjectSettings` has `cloudServicesEnabled: Purchasing: 0`, written when the IAP package installed. Check it in `PAX-067`, before the first real purchase test.
- `Docs/PARALLAX_detailed_implementation_plan copy.md`: `CLAUDE.md` names it as a source (without "copy"), so it stayed, but it's long out of date. Your call.
- `Docs/INVENTORY.md` (the Level Devil research) stays as a source of level ideas.
