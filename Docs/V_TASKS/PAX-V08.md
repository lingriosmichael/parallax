# PAX-V08 · Hazard readability: arrows, spikes and launchers read on every background

**Status:** Approved 2026-10-03 (the developer's answers below); built, uncommitted (§7).
**Wait after Phase 1:** post this plan and stop.
**Phase 1 size: Lite.**
**Depends on:** PAX-102 (committed). **Decisions:** D-085 (no tells: a hidden hazard stays hidden until it reveals),
D-057/D-078 (the arrow's tell ≥ 6 ticks), D-107 (art rulings), R6 (150 effect sprites at once).

## 1. Why

The developer's play-test (2026-10-03): "Because of the background colors and the theme, arrows are super hard to see.
This was already a complaint before and still hasn't been fixed. Recommend solutions that make the arrow more visible
regardless of the level it is in." "Level 004: all arrows and traps are so hard to see. They meld into the background."
The arrow is a thin brown shaft; spikes are dark grey. Brown melts into the ochre and green ruins, grey into stone.

## 2. Plan (the recommended set)

1. **Double outline on every lethal sprite** (arrow, spear, spikes, hidden spikes once shown, launcher mouth): a dark
   outer line and a light inner rim, baked by the art tool into the sprites. One of the two contrasts on bright sky and
   on dark stone alike.
2. **One danger colour** no background uses: saturated red-orange on arrow tips and fletching, spike tips and launcher
   mouths. Background and environment art never use it (a check on the kit's palette).
3. **The launcher's tell glows:** during an arrow's tell (≥ 6 ticks before every shot) its launcher mouth glows
   red-orange. Only during the tell, so a disguised launcher stays disguised until then (D-085).
4. **A contrast test:** for every level, each hazard is rendered at the shipped camera and its luminance contrast against
   the pixels behind it is measured; below 3:1 fails, naming the level, the hazard and the value.

If 1–4 still read weak on the phone: a short bright trail behind a flying arrow and a soft dark halo behind spike rows
(a follow-up, within R6's sprite budget).

## 3. Tests

- The contrast test (red first on today's L004 arrows and spikes).
- `TrapArtParityTests`, `TrapArtRevealFrameTests` (the glow starts at the tell, never earlier), `SavedSceneSyncTests`.
- The full suite once at the end, only when the developer asks.

## 4. Allowed files

- `Tools/Art/*` (the outline and accent pass), the trap sprites they write, `TrapArtSetup*.cs`, the arrow/spike art
  presenters in `Assets/_Game/Presentation/`, `TrapArtConfig` fields if a new sprite slot is needed (asset edit via the
  setup menu), their tests. The level rebuild through its setup menus.

## 5. Out of scope

Gameplay and layouts (PAX-103), the cat, backgrounds' own colours beyond keeping red-orange out of them, sound.

## 6. Open questions

1. Red-orange as the danger colour, or another (it must not appear in any biome planned in PAX-A17)?
2. Should hidden spikes also get the outline after they reveal (recommended), or keep their stone disguise look?

## 7. As built (2026-10-03)

The developer's answers: red, pushed toward crimson; checked against the PAX-A17 biomes (Ember Night's braziers and Golden
Ruins' gold stay amber/yellow, never the danger hue); the 3:1 contrast measured in luminance, with 2D lights on, at every
camera pose; hidden spikes get the outline from their reveal on.

- **The danger colour** is crimson (0.80, 0.06, 0.14), hue 353.5° (`Tools/Art/hazard_readable.py` `DANGER`,
  `Presentation/HazardPalette.cs`). The danger band is ±22° of it, saturation ≥ 0.55, value ≥ 0.30; amber and gold (25–55°)
  are outside it.
- **The sprites** (`hazard_readable.py`, called by `trap_process.py` and `trap_bodies.py`; the build stays deterministic):
  - the arrow: twice as thick (it was fitted by its length into its 0.5 × 0.4 box, so it drew 0.08 u thick), a bone shaft,
    a crimson head and fletching, made at 2× resolution with a 5 px dark line and a 4 px light rim (about 2 screen pixels
    each on a phone);
  - spikes: black blades, a 3 px dark line, a 3 px light rim, the rim and the blade's top crimson toward the tips;
  - the spear: a crimson blade and bone socket (outlined); a near-black iron shaft with a light rim along its length;
  - the launcher: its mouth crimson, and a 5 px line and 5 px rim around it (honest launchers always; a disguised one's slot
    from its fire tick, D-085).
- **Lethal bodies draw unlit** (`TrapArtSetup.HazardMaterial`: URP's unlit sprite material for spikes, arrows, spears and
  launchers/slots; glints and dust stay lit). Measured first: lit, a night level's dim light took the light rim and the
  crimson down until they sank into an unlit sky (L010 Spikes_D2: 7% of its pixels at 3:1 lit, 35% unlit). They lose their
  normal-map shading; a visible style change, reversible in one line.
- **The tell glows crimson:** a launcher (or its slot) flushes toward `HazardPalette.TellTint` and the glint at the head is
  `TellGlint` (`ArrowArt`; it was amber).
- **The spear's shaft** tiles along its length only, stretched to its thickness (`ArrowArt.DrawSpear`); a thick spear had
  stacked rows of it.
- **No environment art carries the danger colour:** `env_kit.py` moves any crimson pixel to amber as it's built and written
  (scattered pixels in about 50 kit images); the banners' red cloth turned a deep royal blue; Ember's sky ramp's dark end
  moved from crimson (hue 7°) to an ember brown (hue 21°). The A02 originals (`A_*.png`) aren't covered: the only one in use,
  `A_OBJ_Vine` (1.3% crimson), is the climb vine's grey-box, hidden at runtime.
- **`HazardContrastTests`** (one case per level, plus two for the measure): every spike strip (from its reveal), every arrow
  and spear at three points of its lane, every launcher (an honest one's body, a disguised one's slot), posed as the game
  draws it, at each of the level camera's own poses that keep it in view (the camera's target set around it and snapped,
  2400 × 1080), rendered with the level's lights (global, point and shape, prepared as `EnvironmentCapture` does), the grade
  and the parallax, with and without the hazard. A hazard reads when at least 30% of the pixels it changes reach 3:1 against
  what's behind them. The 30% was set before the first measurement and not changed.
  - Red first, on the old art: 18 of 20 levels failed (L004's Arrow_O 3–10%, Spikes_1 19%; L008's Arrow_O 2–5%; L010's
    Arrow_4 2–5%; pit-bottom spikes 0%).
  - Now: all 20 levels pass, 419 hazard poses, median 48%, the lowest 30.1% (L015 Pit_2): little margin, so an art change
    that weakens a hazard fails it.
- **Python tests** (`Tools/Art/tests/test_hazard_readable.py`): the danger hue vs amber and gold, the outline, the seam wrap,
  and no danger colour in any kit or background image.
- **Rebuilt:** every level (the hazards' material) and the Trap Lab (its setup menu), on the batch clone; the main project's
  scenes need the same (Rebuild All Levels and the Trap Lab menu).
- **Known reds, unchanged:** `TrapArtRevealFrameTests` L010 Pit10_Cover (PAX-100), `CatSheetImportTests` PPU ×2 (PAX-A16).

