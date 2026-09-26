# PAX-059 · Half B rulings

Status: **Ruled** (2026-09-25). This file collects the developer's half B rulings as given in the session, verbatim:
amendment 1 (§1), the answers to the five Phase 1 questions (§2) and the Phase 2 instruction (§3). §4 records how each
ruling was applied, for the reviewer and for D-085. Where this file and `07_DECISIONS.md` differ, D-085 wins.

---

## 1 · Amendment 1 (before half B's Phase 1)

### Amendment 1 (2026-09-25)

Status: **Ruled, ready for half B's Phase 1.** The developer chose to build half B **before** playing half A. The playtest moves to after half B and covers all ten levels (§5). Where this differs from PAX-059 or the half A rulings, it amends them; everything else stands.

**Implementer reads first (added):** `Docs/LEVEL_DESIGN_GUIDE.md` (project copy: `claude/LEVEL_DESIGN_GUIDE.md`). Sketches use its format (§6) and self-check (§7). Background: `claude/RESEARCH_level_devil_inventory.md`; pattern IDs (F2, F9, D2, …) refer to that doc.

**Precondition:** PAX-059a is committed, with the menus run and EditMode 721/721 (or the count after the half A follow-ups below). Report the baseline at that commit.

---

#### 0 · Half A follow-ups (answer in half B's Phase 1 trace, no redraw)

1. **L005 T3's killer (Spikes_3b):** confirm it's the designed trap, not a relabel. If it's a relabel, fix the geometry instead.
2. **L005 T6 jump under D1's ledge:** give the measured clearance, or make that jump a swept timed step.
3. **L001's Recovers route "Door retreat":** say what it is, and confirm L001 still has a dead end off the solution path.
4. **Approved:** the SceneLeakTests change, and TriggerCoverage.cs at 456 lines (split it later).

#### 1 · Carried from half A (rulings)

1. **"Middle" means the middle third** of the room's width (or height, for a vertical room). L004 starts at x 9.5 of 32 and **doesn't count** as a middle start. The ten-level direction test uses this definition.
2. **Dead-end minimums are rules**, not "may": at least 1 per level in 1–5, at least 2 in 6–10. They count Dies routes named "Dead end…" and Recovers routes, and at least one must be off the solution path. They go into D-085.
3. **Pattern cap:** a trap pattern (a combination) appears in at most 2 levels per half. **L10 is exempt**: the exam reuses earlier lessons on purpose, in a new order, without repeating whole sections.
4. **The falling-block kill quirk** stays guarded by ValidateFallingBlockLanding in every half B level. The kit-gap ticket proposal goes in the report.

#### 2 · Patterns suggested for half B (existing kit)

**Suggestions for the sketches**, not requirements. Each must pass every band 1 rule: no tells, windows ≥ 12, leads ≥ 6, reach 0.75.

| Pattern | What it is | Suggested level | Kit |
|---|---|---|---|
| **F2 · The pit opens one step ahead** | The trigger sits before the trap, so the floor ahead of a running cat falls while it's still one step away. | L7 (trust nothing) | CollapsingFloor with its trigger moved ahead |
| **F9 · The floor lifts you into the ceiling** | A "safe" block you stand on rises and presses you into a ceiling hazard. | L8 (chains), as one link | FallingBlock Up or MovingTrap Solid, plus a disguised ceiling hazard |
| **D2 · The door retreats twice** | Once (known from L2), then again at the last moment, onto a trap. | L10 (the exam) | DoorRetreat ×2; confirm it works in the kit |
| **One gap, many betrayals** (guide P3) | The same spot betrays you 2–3 times in one level, differently each time. | At least 2 of L6–L10 | Any |
| **Traps on the jump** (guide P4) | A take-off trigger changes the landing: a rising lip, a hole past it, a fake landing. The lead ≥ 6 still applies. | At least 2 of L6–L10 | CollapsingFloor / FakePlatform now; a rising lip after §3's check |

Every sketch applies the guide's P1 ("where does the obvious dodge land?") and P5 (one or two hazard kinds per level, plus the lesson).

#### 3 · Kit checks for Phase 1 (they count within the 6 questions)

1. **MovingTrap Solid pushing or lifting the cat (F6/F7, and the rising lip).** Can it push the cat sideways, or rise under or in front of it, without crushing it, sticking it or tunnelling through it? What happens when the cat is pushed against a wall? Give file:line.
   - Clean → use it in L7 or L9.
   - Not clean → **outcome (d)** for those patterns only: propose a kit-gap ticket, drop them from half B, no stop.
2. **Two retreats on one door (D2).** Does DoorRetreat support a second retreat, or does it need two elements? Give file:line. If neither works without runtime code → outcome (d) for D2; L10's "the door retreats last" uses one retreat plus a trap.

#### 4 · Kit gaps for levels 11+ (roadmap note, in half B's docs commit)

Add this to **08_V1_ROADMAP.md** under PAX-060 (08 is already in §7). It's a note, not a ticket:

> **Kit gaps from the Level Devil study (candidates, not scheduled).** Moving pit · door as a platform · fake exit / warp · springs · slippery floor · wraparound screen edges. Content model for levels 11–50: **one new kit element per theme, explored across ~5 rooms, with a mixed "exam" level closing each theme**. Each element gets its own KIT ticket before the levels that use it (KIT-5 to KIT-9 already planned). Not in band 1: reversed controls and reaction-based traps. Design guidance: Docs/LEVEL_DESIGN_GUIDE.md.

No code for any of these in PAX-059 (§8).

#### 5 · Developer playtest (after half B, all ten levels)

Per level 1–10, ideally without looking at the sketches:

| Level | Deaths before finishing | Unfair deaths | Clues (spotted before it fired) | Knew where to go? | Fun: retry right away, or a chore? |
|---|---|---|---|---|---|
| L1–L10 | | | | | |

**Retry cost:** did replaying solved traps after a late death feel like a chore? Note the level and roughly how long.

Findings go into a follow-up ticket (PAX-059c, if needed) before PAX-060.

#### 6 · Retry cost (interim rule until the playtest)

Until the playtest decides, half B follows the guide's P13:
- traps that are **quick to pass once known go first**;
- aim for a solution **≤ ~20 s** in levels 6–9 (L10 may use up to 30 s);
- the report gives each level's **worst late-death replay** (the time from the start to the last trap's trigger).

After the playtest, pick: (a) mid-room checkpoints (a kit-gap ticket if layout can't do it), (b) shorter replays across 1–10, or (c) accept it and measure in PAX-070.

Ruling after the playtest: ______

---

## 2 · Answers to the five Phase 1 questions, and sketch problems (after Phase 1)

My answers to the five questions

Push and rising lip: accept that they need new code. The lift is fine for L8.
Double door retreat: also needs new code. L10's door retreats once, onto a trap.
Long chains in L8: approved for permanent changes only (floors that give way, spikes that come up and stay up). One condition: the player must see the chain happen, or see its result before walking into it.
L10's door directly above the start: approved. The level's tempting dead end becomes the obvious way straight up to the door.
Retry cost: move L7's T6 onto S1 as proposed, and also shorten L10 so a late death replays about 18 s or less. L10 is the last free level before the choice to pay or wait, so a 24 s chore replay there is the worst place for it.

Problems in the sketches they didn't flag

L8's T4 jump looks too long. Clearing the lift and the landing that gives way means about 4.8 u, over the 0.75 reach limit.
"Freeze, then don't linger" is spreading. It's already in L1, L2, L6 and L10. L8 would make it half the free levels, and players would just learn "never stand still". L8's T2 should become a real chain lesson instead.
L7's T6 repeats T1 and T2 in the same level. Moving it onto S1 is the chance to make it escalate instead.
L10 doesn't really test L8 or L9. A cheap fix that adds no trap: the real flip fires an arrow, and one early trigger sets off a change the player meets later.
L6's "stand still" answer must have a real zone of at least 1 u to stand in, not a single point.

---

## 3 · Phase 2 instruction

PAX-059 half B: rulings are in Docs/0_TASKS/PAX-059_halfB_rulings.md. Read it fully.

Summary: Q1–Q4 approved as proposed (Q3 with a visibility condition, Q4 with D1 changed to the "straight up to the door" lure). Q5: (b) for L7 with a new twist for T6, and L10 shortened to a late-death replay of ≤ ~18 s with ≥ 7 lethal.

Sketch changes before building (§3): L6 T3 stand zone ≥ 1 u; L7 T6 moved to S1 and escalated (not NW again); L8 T4 jump within 0.75 reach (show the distance) and T2 changed from freeze/don't-linger into a chain lesson; L10 door above start, shortened, and covering L8 (a chain) and L9 (the real flip fires an arrow).

Start Phase 2. Tests first; build L6–L10 one at a time, each passing every band-1 rule before the next. If a sketch change can't meet a band-1 rule, stop that level only and report the closest variant. Also in Phase 2: the L005 T3 variant proving Spikes_3, and L005's T6 jump as a swept timed step.

Docs per §4 of the rulings (the guide's file name, the 280-tick floor, the play-fix rules, D-085 in full). Then the full EditMode run against 733, the pax-reviewer loop (max two rounds), pax059_review.txt, and the menu steps and git add commands for PAX-059b. Don't run menus or change scenes through MCP. If Unity disconnects, stop and tell me.

---

## 4 · How each ruling was applied (PAX-059b, 2026-09-25/26)

**Amendment §0 (half A follow-ups):** answered in the Phase 1 trace.
- L005 T3's killer Spikes_3b is the designed trap. A second T3 variant now proves Spikes_3 (lead 25).
- L005 T6's jump is a swept timed step (window 32).
- L001's "Door retreat" is a Recovers route declared on the solution, and its dead end is "Spikes_2 rise on the high ledge".

**Amendment §1 (carried rulings):** all in D-085 (1) and (3).
- The middle third is used by `Band1DirectionMixTests`; L004 is not a middle start.
- Dead-end minimums are rules. The developer's play later ruled that dead ends kill, so every band-1 dead end is a Dies
  route (D-085 (2)).
- The pattern cap has L10 exempt.
- `ValidateFallingBlockLanding` runs on every level.

**Amendment §2 (suggested patterns):**
- F2: L007 T1.
- F9: L008 T3, a lift into roof spikes that are always in view.
- D2: not built (§2 Q2).
- One gap, many betrayals: L006 (Floor_5's pit, for T5 and the step-off dead end) and L007 (pit 2, for T2 and T6).
- Traps met on the jump: L007 T5 (the overhang meets the jumper), and the floating-flip lures of L009 and L010, entered by
  the jump.

**Amendment §3 (kit checks):** answered in Phase 1.
- Lifting is clean. Pushing and a rising lip are outcome (d).
- Two retreats on one door don't compose: outcome (d).
- Kit-gap proposals are in the report and D-085 (9).

**Amendment §4 (roadmap note):** added word for word under PAX-060 in `08_V1_ROADMAP.md`. The guide is now at
`Docs/LEVEL_DESIGN_GUIDE.md`, the name the note uses.

**Amendment §5 (playtest of L1–L10):** **open** (the developer). Findings go to PAX-059c if needed.

**Amendment §6 (retry cost):** the interim rule was followed.
- Quick traps come first.
- Solutions: L6 10.1 s, L7 18.7 s, L8 14.3 s, L9 11.5 s (all ≤ ~20 s); L10 10.2 s.
- Worst late-death replays: L6 9.7 s, L7 15.2 s, L8 10.6 s, L9 9.7 s, L10 10.4 s.
- **The ruling after the playtest ((a), (b) or (c)) is open.**

**§2 answers:**
- **Q1:** pushing and a rising lip are left for a kit-gap ticket. The lift is used in L008. The Blocker found in review
  (the lift's roof spikes singled it out) is fixed: they now run 10 u, and `ValidateBand1Tells` covers lifts.
- **Q2:** L010's door retreats once, over a roof section that gives way (T6).
- **Q3:** long chains link permanent changes only. The visibility condition holds: L008 Spikes_2 comes up in view, and
  L008 Spikes_5 and L010 Spikes_5 are on screen before they can kill (the camera tell rule: 90 of 90 ticks and 237 of
  393). Short, local chains may rearm (D-085 (5)).
- **Q4:** L010's door is directly above the start, and its dead end is the stair "straight up to the door".
- **Q5:** L007's T6 is on S1. L010's late death replays 10.4 s (cap ~18 s), with 8 distinct killers (≥ 7).
- **The sketch problems:**
  - L008 T4's jump is 2.9 u (13.5 to 16.4, within 2.94).
  - L008 T2 is a chain lesson: the block's landing brings up spikes ahead.
  - L007 T6 escalates to "wait" (spikes come up at the landing).
  - L010 covers L8 (the block's landing brings up roof spikes met later) and L9 (the real flip fires an arrow).
  - L006's stand-still zone is 3.5 u.

**§3 (Phase 2 instruction):**
- Tests came first.
- L6–L10 were built one at a time, each passing every band-1 rule.
- No level had to stop at a closest variant. The L008 lift's first version needed a learned bypass, which band 1
  doesn't allow; it was rebuilt within the rules.
- The L005 follow-ups are done.
- Docs: the guide's file name, the 280-tick floor, the play-fix rules and D-085 in full.
- Full EditMode: 774 tests, 773 passed (the one failure clears after the developer's `New Level…` runs).
- One pax-reviewer round, recorded in `pax059_review.txt`.
- Menus and git add commands are in the hand-off.
- No menus or scenes were run through MCP. One Unity disconnect was reported.

**Still open:** the developer's menu runs, the saved-scene check, the playtest (amendment §5) and the retry-cost ruling
(amendment §6).
