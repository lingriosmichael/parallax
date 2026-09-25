# Research · Level Devil: pattern inventory for PARALLAX (2026-09-25)

Purpose: study how Level Devil (Unept, 2023; Steam 2025) builds its troll levels, list its trap and moving-floor patterns, and map each one to PARALLAX's kit. Input for PAX-059 half B and PAX-060.

**Confidence.** The structure and the per-level themes are well sourced (Poki, Steam, NamuWiki, reviews). Stage-by-stage details come mostly from fan walkthrough sites of mixed quality; items marked *(unverified)* should be checked by watching a playthrough before we copy them.

---

## 1. How the game is built

| Aspect | Level Devil | Note for PARALLAX |
|---|---|---|
| Size | ~200–240 levels over 3 worlds ("Level Devil", "-er", "-est") | The same order as our planned 50 |
| Unit of play | A themed "door" (level) holds **5 short stages** ("gateways"), each about one screen | Our unit is one bigger room of 10–30 s |
| On death | **Instant restart of the current stage only**; earlier stages stay done | This is the key difference, see §5.1 |
| Theme | Each door has one gimmick in its name: Pits, Spikes, Push, Coins, Controls, Platforms, Springs, Warps, Scale, Doors, Saws, Flappy, Gravity, Movement (slide), Wraparound, then a boss door that mixes them all | The same as our "one lesson per level"; our L10 exam is their boss door |
| Escalation | Stage 1 shows the gimmick plainly; stages 2–5 twist it; the boss door combines everything learned | The same as our progression table |
| Randomness | None found; stages are fixed and memorisable | Matches D-067 |
| Visuals | Flat, minimal pixel blocks; the ground is plain, so there's nothing to tell a trap apart | Matches our no-tells rule |
| Tone | Comedic: reviewers laugh "because the game knew what I was going to do" | The feeling we want for the hook |
| Extras | Hidden keys (5, then 10) unlock secret levels and a true ending; a 2-player race mode | Possible later retention ideas |
| Monetisation | Free on web (Poki) and mobile; $6.99 on Steam | Compare D-068 |

### The core design move
The Steam description puts it best: layered traps where **the game anticipates your reaction and sets the next trap there**. You dodge trap A the obvious way, and trap B waits exactly where the dodge lands you. That is our R-2.2 ("punish the lesson you just learned"). Level Devil uses it in nearly every stage, often twice.

---

## 2. Inventory: floors, walls and platforms that move

| # | Pattern | How it fools you | Kit in PARALLAX |
|---|---|---|---|
| F1 | **A pit opens under you** as you run over a plain floor | Solid-looking floor | ✅ CollapsingFloor / FakePlatform |
| F2 | **A pit opens just in front of you**, triggered one step early | You run; the gap is too close to stop | ✅ CollapsingFloor with a trigger ahead of it |
| F3 | **A hole opens where you land** after you jump a visible gap | Punishes the jump | ✅ (used in our L3 T2, L4 T4) |
| F4 | **The pit moves**, sliding toward you or following you *(unverified detail)* | The gap you measured isn't where it was | ❌ kit gap (a moving pit) |
| F5 | **The floor drops while you stand still** (a timer on standing) | Punishes hesitation | ✅ CollapsingFloor with a delay; our "don't linger" |
| F6 | **The floor suddenly appears and blocks your path** (a wall rises out of the ground) | The route closes | ⚠️ MovingTrap Solid rising; check it can block, not crush |
| F7 | **A wall protrudes and pushes you** into a pit or spikes (the "Push" door) | Knocked off a safe spot | ⚠️ MovingTrap Solid horizontal; does the motor get pushed? Check |
| F8 | **The ceiling falls** | Running under things | ✅ FallingBlock (flush, as ruled) |
| F9 | **A floor block rises** and presses you into ceiling spikes | Standing on a "safe" block | ✅ FallingBlock Up / MovingTrap Solid, plus ceiling hazard |
| F10 | **A platform vanishes on touch**, forcing touch-jump-leave | Hesitating on it | ✅ CollapsingFloor with a short delay |
| F11 | **A slippery floor**: momentum keeps sliding you (the "Movement" door) | Your stopping distance lies | ❌ kit gap (motor change) |
| F12 | **The ground reshapes around you** while a saw chases (the "Saws" door) | Time pressure plus a shifting route | ❌ mostly a gap (execution; band 2+) |

## 3. Inventory: spikes, hazards and projectiles

| # | Pattern | How it fools you | Kit in PARALLAX |
|---|---|---|---|
| S1 | **Spikes pop up from the ground** near a trigger | A "safe" gap or floor | ✅ HiddenSpikes |
| S2 | **Spikes slide horizontally** toward you | The place you waited in isn't safe | ⚠️ MovingTrap Hazard (it's red, so honest: D-056 (4)) |
| S3 | **Spikes on a rhythm** (a fixed cycle) | Needs counting, not reaction | ✅ periodic spikes (planned for L6) |
| S4 | **A spring launches you** into spikes above | Panic-jumping on springs | ❌ kit gap (spring) |
| S5 | **Saws, static or chasing** | Pressure | ⚠️ Sweep only; chasing is a gap |
| S6 | **Balls bouncing on a rhythm**; one chases you; the last one shrinks when you step on it (Part 2) | Rhythm, then the last step lies | ❌ gap (band 2+) |
| S7 | **Bombs fall on you**, and split, shrink or move just before landing (Part 2) | Reaction | ❌ gap, and execution heavy |
| S8 | **Guns firing along a lane** (Part 2 "Bullets") | A lane opens on a trigger | ✅ Arrow (disguised) |

## 4. Inventory: the door, lures and rule changes

| # | Pattern | How it fools you | Kit in PARALLAX |
|---|---|---|---|
| D1 | **The door runs away** as you approach | "The end" isn't the end | ✅ DoorRetreat |
| D2 | **The door runs away more than once**, or into a trap | You chase it into the trap | ✅ several retreats, or retreat plus a fake landing (our L2) |
| D3 | **The door is a platform, or an obstacle** (the "Doors" door) | The goal is something else | ❌ kit gap |
| D4 | **A fake final door**: an invisible warp near the exit throws you back (the boss door, "hope torture") | False hope at the very end | ❌ kit gap (warp); a possible L10 idea if the kit grows |
| D5 | **Coins as bait**: a coin sits near a trigger, and taking certain coins fires a trap | Greed | ❌ no collectibles; ⚠️ any visible lure works the same (our visible flip, a "shortcut" ledge) |
| D6 | **Reversed controls**, left and right, sometimes mid-stage | Your hands lie | ❌ runtime gap; unfair on touch without a clear sign |
| D7 | **A gravity toggle** (the "Gravity" door) | Floor and ceiling swap | ✅ GravityFlip (ours is an area, not a toggle) |
| D8 | **Size change** by a button, changing your jump and hitbox | Your jumps are measured wrong | ❌ gap |
| D9 | **Wraparound screen edges** (the "Wraparound" door) | Walls are exits | ❌ gap |
| D10 | **Warps that move**, and pull a warp out from under you | Mapping the destinations | ❌ gap |
| D11 | **Flappy mode**: continuous jumping | A new rule | ❌ gap, and it's execution |
| D12 | **Hidden keys** in alcoves, some needing you to go backwards or off-route | Exploration reward | ❌ gap; fits our "dead ends" if we ever reward them |

✅ in the kit · ⚠️ in the kit but needs a check or has a limit · ❌ kit gap (runtime, or out of band 1)

---

## 5. What this means for PARALLAX

### 5.1 How much a death costs you (the biggest finding)
Level Devil keeps every retry tiny: 1–2 traps per stage, restart only that stage. A player dies often but never replays more than a few seconds. PARALLAX levels are one room of 10–30 s with 5–7 traps in sequence, so dying at trap 7 replays about 20–25 s of solved traps. That's where fun turns into chore, and it's what your half-A playtest should watch. Options (needs an Architect ruling; mid-room checkpoints may need kit work):
- (a) **Mid-room checkpoints** after every 2–3 traps (the closest match to Level Devil's stages);
- (b) keep the rooms but **front-load the time**: early traps quick to pass once known, so the replay is short;
- (c) accept the replay cost for levels 1–10 and measure it in PAX-070.

### 5.2 Patterns to reuse in half B (existing kit)
- **F2**, a pit that opens just ahead (a trigger in front of the trap), is a cleaner "the floor lies" than F1.
- **F6/F7**, a wall that rises or pushes, for L7 "trust nothing" (check MovingTrap Solid can push the motor safely).
- **F9**, a floor that lifts you into the ceiling, for L8 "chains".
- **D2**, a door that retreats twice, for the L10 exam.
- **The anticipation trick** as a habit: for every trap, ask "where does the obvious dodge land?" and put the next trap there.

### 5.3 Kit-gap wishlist for levels 11+ (from their themes)
Moving pit (F4), a door as a platform (D3), a fake exit or warp (D4), springs (S4), a slippery floor (F11), wraparound (D9). Each Level Devil door is one of these gimmicks explored across 5 stages, which suggests a cheap content model for levels 11–50: **one new kit element, five rooms**.

### 5.4 What not to copy
- **Reversed controls and reaction traps** (bombs, chasing saws): they need execution or feel unfair on touch, and they break our band 1 rule.
- **Hints by offsets or seams**: one fan site claims traps are "hinted by tiny offsets, missing seams" *(unverified)*. Our rule stays: no tells at all.

---

## Sources
- [Poki: Level Devil](https://poki.com/en/g/level-devil): developer, structure (doors of 5 stages, 3 worlds), trap types
- [Steam: Level Devil](https://store.steampowered.com/app/3242750/Level_Devil/): ~240 levels, "anticipates your reaction", price, release
- [NamuWiki: Level Devil](https://en.namu.wiki/w/Level%20Devil): the per-door themes and mechanics, boss door, "hope torture"
- [WellPlayed review](https://www.well-played.com.au/level-devil-review/): structure, humour, "the game knew what I was going to do"
- [GeeksVsGeeks review](https://www.geeksvsgeeks.com/2025/03/level-devil-review-platformer-that.html) and [Rectify Gaming review](https://www.rectifygaming.com/review-level-devil/): deception over difficulty, instant respawn
- [Causal Zap: levels 1–16](https://causalzap.com/en/blogs/level-devil-levels-1-16/) and [Causal Zap: 2 Balls](https://causalzap.com/en/blogs/level-devil-balls-guide/): stage detail (fan guides, mixed quality)
- [level-devil.org walkthrough](https://level-devil.org/en/walkthrough) and [leveldevil.co.uk](https://leveldevil.co.uk/): fan guides, stage counts, trap descriptions (unverified)