# PARALLAX · Level design guide

**Who reads this:** the implementer, before drawing sketches for any content ticket (PAX-059 onward), and the Architect, when ruling on sketches.
**What it is:** how to design a PARALLAX level that is hard, fun and fair. The validators check the numbers; this guide covers what they can't check.
**Where it comes from:** PAX-059's rulings (preflight, half B amendment 1), the Level Devil study (`claude/RESEARCH_level_devil_inventory.md`) and the decisions in `07_DECISIONS.md`. If this guide and a decision disagree, **the decision wins**; flag the difference.

---

## 1 · The goal

Levels die by **troll and memory, not execution**. A first-time player dies many times, once per trap, and every death teaches something they can see and remember. The jumps and timing stay forgiving; the room lies to you. Levels 1–10 are the free hook (D-068): the player should laugh at a death and retry straight away, not sigh.

**The core loop (D-040):** see the room → get betrayed → learn the trick → beat it. Rooms are fixed on every attempt (D-067), so every lesson stays true.

---

## 2 · Hard rules (the validators check these)

Band 1 = levels 1–10. Band 2+ numbers come from each content ticket.

| Rule | Band 1 | Source |
|---|---|---|
| Lethal betrayals per level | ≥ 5 (L1–5), ≥ 6 (L6–7), ≥ 7 (L8–10) | D-085 |
| Of those, in sequence on the way to the door | ≥ 4 (L1–5), ≥ 5 (L6–10) | D-085 |
| Tempting dead ends (Dies routes named "Dead end…", or Recovers routes), at least one off the solution path | ≥ 1 (L1–5), ≥ 2 (L6–10) | D-085 |
| Lead (the trap visibly moves before it can kill) | ≥ 6 ticks | D-057 |
| Timing windows on every timed step, including mid-air releases | ≥ 12 ticks | D-056 |
| Jumps | ≤ 0.75 of reach | D-056 |
| Precision sections, bait gaps as the only difficulty | none | D-065 / D-083 |
| Solution time once known | 10–30 s (L1 exempt, ~300 ticks) | D-085 |
| Width | ~32 (today's frame); tall rooms allowed, followed vertically | D-085 |
| Door distance from start | ≥ half the width or half the playable height, never a straight line | D-085 |
| Camera tell rule, trigger coverage (per storey), surface coverage, door clearance | pass | D-083, D-074, D-080 |
| Falling blocks | pass ValidateFallingBlockLanding (the kill quirk) | PAX-059 |

---

## 3 · Design principles (the Architect checks these in sketches)

### P1 · Anticipate the dodge
For every trap, ask: **"Where does the obvious dodge land?"** Put the next trap, or a dead end, there. This is the single most important habit; Level Devil does it in almost every stage.

### P2 · Punish the lesson just learned
At least **2 traps per level** kill you *for doing the answer to an earlier trap*. You learned to jump suspicious floors, so the landing collapses. You learned to wait, so a chained block punishes waiting. Mark these ⟲ in the sketch.

### P3 · Reuse one spot
Put the same gap, ledge or doorway in a level **two or three times**, betraying you differently each time: first the floor before it drops, then a block rises at its far lip, then a hole opens past the landing. It costs nothing to build, it loads memory, and every death happens at a spot the player already knows.

### P4 · Traps on the jump, not the run
The best troll fires **at take-off**, so the landing changes while you're in the air:
- a block rises at the far lip, you hit it and fall;
- a hole opens just past the landing;
- the landing itself is fake or collapses.

**Lead still applies.** A take-off trap must show its move at least 6 ticks before the fall kills, so the player sees what happened. If the rise and the death are too close, move the trigger earlier.

### P5 · Economy: one hazard, many ways in
A level can use **one hazard (a pit) and three or four ways into it**: pushed, blocked, dropped, tricked. That teaches more cleanly than five different trap kinds. Aim for one or two hazard kinds per level, plus the level's lesson.

### P6 · Tempting dead ends
Add extra platforms, even ones that lead nowhere and cost the player a death: a higher "shortcut" ledge, a platform toward the door you can see, a safe-looking nook. Each is declared as a Dies or Recovers route.

### P7 · Vary the answers
At most **2 traps per level** share the same learned answer. The answer types:

| Code | Answer | Code | Answer |
|---|---|---|---|
| J | jump it | NJ | don't jump |
| W | wait | NW | don't wait |
| SS | stand still | B | go back |
| OL | take the other ledge | LW | go the long way |
| BAIT | trigger it, back off, let it land, go (≤ 1 per level) | | |

### P8 · Don't repeat a pattern
A trap *pattern* (a combination, such as "freeze, then don't linger") appears in **at most 2 levels per half**. Single elements (a fake floor) and answer types (J) recur by design. The exam level (L10, and each later theme's closing level) is exempt: it reuses earlier lessons on purpose, in a new order.

### P9 · Show the door early
Where it fits, let the player see the door early (through a gap, from above, right over the start), with the obvious way to it being a trap.

### P10 · No tells, ever
Nothing marks a trap before it fires:
- trap floors (CollapsingFloor, FakePlatform) draw at sorting −1 over **solid ground**; a trap floor over a pit fills the whole shaft;
- falling or rising blocks sit **flush inside their host** (ceiling, floor or wall), never hanging in view;
- disguised launchers take their host's colour; side walls that host them are layout Wall elements;
- **nothing visible points at a trap**: no hazard directly under (or, upside down, above) a trap floor and nowhere else on that storey. Tall rooms show other storeys, so check what the camera sees;
- honest hazards (red sweeps, periodic spikes) stay honest and visible (D-056 (4)).

### P11 · Every death shows its cause
The player must be able to say what killed them after one death. If a death needs a replay or a guess to understand, it's unfair: simplify it (Level Devil's cheap trick, our L5 Arrow_C lesson).

### P12 · Direction and shape
Levels don't all run left to right: they climb, descend, double back, and start in the middle (the middle third) or on the right. Across each ten levels: ≤ 3 plain left-to-right, ≥ 2 climb, ≥ 2 descend, ≥ 3 start middle or right, ≥ 2 double back.

### P13 · Keep the retry cheap
A late death replays every solved trap before it. Level Devil keeps retries to a few seconds. Until the retry-cost ruling (PAX-059 half B amendment 1, §6) is made: put the quick-to-pass traps early, keep the longest levels under ~20 s where possible, and report any level whose late deaths replay more than ~15 s.

---

## 4 · Pitfalls from building levels 1–5

- **Mid-air releases are precision in disguise.** Letting go of the stick in the air stops the cat within ~0.23 u. Any required landing that depends on it must be a timed step with a window ≥ 12, or the target gets wider.
- **The falling-block kill quirk.** The kill check uses the block's position from the tick before. A long last step pins the cat without killing it. ValidateFallingBlockLanding catches it; don't design around it by hand.
- **Grazes count.** A touch within 0.05 of a surface counts. Jumps near a ledge underside, and take-offs under a low ledge, can bump the head and fall short. Leave margin and measure it.
- **Pins with no slack break first.** A lead of exactly 6, a window of exactly 12, or a time exactly at the floor will fail after the next motor or trap retune. Aim for a little margin.
- **Killers are the designed trap.** Never relabel a betrayal's killer to match what the harness hit. If it dies on the wrong trap, fix the geometry.

---

## 5 · Pattern library (current kit)

Full list and sources: `claude/RESEARCH_level_devil_inventory.md`.

| Pattern | Kit | Notes |
|---|---|---|
| Floor gives way under you | CollapsingFloor / FakePlatform | The classic |
| **The pit opens one step ahead** | CollapsingFloor, trigger moved ahead | Cleaner than a collapse underfoot |
| The hole opens where you land | CollapsingFloor at the landing | A P2 / P4 favourite |
| The floor drops if you stand still | CollapsingFloor with a delay | Punishes hesitation |
| The ceiling falls | FallingBlock, flush in the ceiling | Chains well |
| **The floor lifts you into the ceiling** | FallingBlock Up / MovingTrap Solid, plus a ceiling hazard | Check the lead |
| **A block rises at the far lip** of a jump | MovingTrap Solid rising | Needs the kit check (below) |
| **A wall pushes you** into a pit | MovingTrap Solid horizontal | Needs the kit check (below) |
| Spikes pop up | HiddenSpikes | |
| Spikes on a rhythm | periodic spikes | Honest; count, then commit |
| A lane fires from a wall | Arrow (disguised) | |
| The door retreats (once or **twice**) | DoorRetreat | Two retreats: confirm in the kit |
| A flip that looks like an escape | GravityFlip + hidden spikes | The lure isn't the killer |
| A chain: one trigger, a delayed sequence | Trigger chains + delays | Memorise the order |

**Kit check pending:** can MovingTrap Solid push or lift the cat cleanly (no crush, no sticking, no tunnelling)? Until it's confirmed (half B amendment 1, §3), don't rely on the rising-lip or pushing-wall patterns.

### Worked example: one gap, many betrayals (Level Devil, Pits)
One screen: a pillar, a gap one block wide, a slab, a small block on the slab. The jump itself is easy. Across the stages:
1. the small block slides to the landing lip, you land into it and fall back into the gap;
2. a second gap opens in the slab just past where you land;
3. the floor piece inside the gap sinks, turning it into a deeper pit;
4. a block rises out of the far lip while you're in the air.

Everything is the same colour and comes out of the ground itself, and the only way to die is the pit. That's P3, P4, P5 and P10 in one small room.

---

## 6 · Sketch format

Every sketch, one per level:

**Header:** size (width × height), storeys, start (x, y), door (x, y), direction, whether the door is visible early, and an estimate of the solution time.

**Trap table:**

| # | Trap (element, rect, trigger) | What kills a first-timer | Answer (code) | ⟲ punishes |
|---|---|---|---|---|
| T1 | … | … | J | — |
| T2 ⟲T1 | … | … | NW | T1's answer |
| D1 | dead end … | … | LW | — |

**Footer:** lethal count / in sequence; dead ends (Dies / Recovers); bait count; answer mix; which spots are reused (P3); which traps fire on the jump (P4); which hazard kinds are used (P5).

---

## 7 · Self-check before submitting a sketch

- [ ] Lethal count, sequence count and dead ends meet §2.
- [ ] Each trap answers "where does the obvious dodge land?" (P1).
- [ ] At least 2 ⟲ traps (P2).
- [ ] At least one spot reused, or a note saying why not (P3).
- [ ] At least one trap fires on the jump, with its lead checked (P4).
- [ ] One or two hazard kinds, plus the lesson (P5).
- [ ] No answer type more than twice; at most one BAIT (P7).
- [ ] No pattern already used in 2 levels of this half, unless this is the exam (P8).
- [ ] Nothing visible points at any trap, from any storey (P10).
- [ ] Every death can be understood after one death (P11).
- [ ] Late-death replay estimated (P13).
- [ ] No required landing depends on a mid-air release with a window under 12 (§4).

---

## 8 · Levels 11+

The content model for levels 11–50: **one new kit element per theme, explored across ~5 rooms, with an exam level closing each theme** (Level Devil's doors and boss doors). Tight execution starts at level 11 (D-065), so band 2 adds precision sections on top of everything above; it doesn't replace the troll.

The planned kit elements (`claude/KIT-5-9_levels11plus_mechanics.md`): spear (KIT-5), inverter (KIT-6), geyser (KIT-7), vines (KIT-8), storm cloud (KIT-9). Later candidates from the research: moving pit, door as a platform, fake exit, springs, slippery floor, wraparound. For each new element, draft its five-room theme using P1–P5 before building the kit ticket, so the kit is built for the levels it has to serve.