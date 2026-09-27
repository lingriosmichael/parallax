# PAX-V06 · Game feel: weight, impact and flow

**Status:** Approved (after PAX-V05 and PAX-V07). Runs alongside PAX-064/065 (audio, haptics): pull those forward to land together.
**Phase 1 size: Lite.** **Depends on:** PAX-A08/V07 (cat clips), PAX-V05 (trap presentation). **Decisions:** D-020 (camera never rotates), D-041 (death to control ≤ 0.75 s, no fade), D-058 (death hold), D-073 (pause), D-083 (camera tell rule), LOOK_AND_FEEL §5.

## 1. Why

Art makes it look good in a screenshot; feel makes it good in the hand. This is the pass that makes deaths funny and landings satisfying.

## 2. What to build (presentation only)

1. **Cat squash and stretch** on take-off and landing (Visual scale, owned by `CatVisualPresenter`, D-034; never the collider).
2. **Dust:** run puffs, turn skid, landing puff (bigger on hard landings), wall-contact dust.
3. **Camera impulse:** a short translational shake (no rotation, D-020) on death, block impacts and geyser launches; small, decaying, **off during the camera tell measurements**, and a "reduce motion" toggle in settings.
4. **Death beat:** a 2–3 frame white flash on the cat, the death clip, the room frozen (the hold already does this), then the respawn clip: still ≤ 0.75 s to control.
5. **Checkpoint beat:** the gate marker lights with a burst and a soft chime hook (PAX-064).
6. **Level complete:** the celebrate clip, a warm light swell, then the existing level-complete screen; **level transitions:** a short styled wipe between levels (menus only; never during play).
7. **Hooks for audio and haptics** at every beat above and every trap reveal (events only; PAX-064/065 fill them).

## 3. Rules

No gameplay change, every pin identical. Effects stay inside the V03 budget. Every effect is off-able by a single settings switch for accessibility ("reduce motion") except the minimal death flash.

## 4. YOU: Unity Editor (+ phone if you allow it)

Play five levels. Say, per beat, "too much / right / too little". This ticket is tuned by your hands, not by numbers.
