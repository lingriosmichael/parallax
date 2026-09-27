# PAX-A13 · Trap art kit: every trap, every state

**Status:** Approved (after PAX-A12, alongside or after PAX-A08). **Depends on:** PAX-A12 (host surfaces: traps disguised as floors, walls and ceilings reuse them), PAX-V03 (budget, lights, normal maps).
**Target:** `Docs/Art/LOOK_AND_FEEL.md` §3 (rules 2–5) and §4. **Rules:** as PAX-A12. Slots defined in the manifest first. **Budget note:** the developer confirms the credit total before generation.

## 1. Why

Every trap in the game is a flat colour block today. Traps are the stars of a troll game: their reveal is the joke and the death is the punchline. Each needs weight, a readable reveal and a death that shows the cause.

## 2. The rule that shapes everything: disguised traps look like their host

A disguised trap's **idle** state uses the host's pieces (A12), pixel-identical. Only its **reveal** frames are new. Honest hazards have their own consistent danger language (bronze/rust metal, dark water, red-rust cloth for "danger").

## 3. The kit (state list per kind; frames are about)

| Kind | Idle (before reveal) | Reveal / tell | Active | After / spent |
|---|---|---|---|---|
| Collapsing floor | host platform | crack lines spread (3) | crumble + falling debris (5) | gone; debris particles |
| Fake platform | host platform | — | vanishes: leaves/dust puff (4) | gone |
| Hidden spikes | nothing (host floor/ceiling) | spikes punch out with dust (3) | bronze spikes, static | stay up |
| Falling block | flush carved ruin stone in its host | dust trickle + shudder (3) | falling stone, motion streak | impact dust, cracks at rest |
| Moving trap: Solid / Hazard | host wall / honest blade or spiked log | grinding dust | moving | — |
| Periodic spikes (honest) | retracted slots in the floor | rising (2) | up | lowering (2) |
| Arrow launcher + arrow | carved face in the wall (host colour if disguised) | eye-glint tell (≥ 6 ticks) | bronze arrow in flight + trail | stuck, quivering (3) |
| Spear | same launcher family | glint | long spear in flight | stuck in the wall, a standable shaft (reads as a step) |
| Gravity flip zone (honest) | shimmering glyph field (loop) | — | pulse on flip | — |
| Door retreat | the exit door (A05) | grinding start | slides with dust | at rest |
| Geyser vent | stone vent with a grate | bubbling + steam tell (loop) | air/water column (loop), spray at the top | settling |
| Inverter orb | glowing glyph orb on a pillar (honest) / nothing (disguised) | — | burst on touch; the ⇄ ring cue on the cat (loop, blink in the last 30 ticks) | orb gone / rearms |
| Climb vine + snap vine | A08 item 15's vine | snap vine: fibres tearing (2) | snap and fall (4) | stub |
| Storm cloud | a soft cloud like the sky's (dormant) | darkening + crackle, the target line (charge) | lightning bolt (3, flickering) | scorch on the ground (fades) |
| Checkpoint gate marker | unlit brazier/glyph stone | — | lights up (4) + ember loop | lit |
| Pit hazard (water) | A12 water | — | splash on death (4) | ripples |
| Death effects | — | — | per cause: impact flash (spikes/arrows), crush dust (blocks), splash (water), zap (lightning) | — |

## 4. Code

PAX-V05 wires all of it. This ticket delivers art and manifest slots only.

## 5. Acceptance (Editor)

Each kind in its Trap Lab room at phone scale: the idle is indistinguishable from the host (screenshot diff), the reveal reads, the death shows its cause during the hold. Credit ledger per slot.
