"""PAX-A18 W3: bumpy stone stays honest to the collider (the strips' outlines within their reach; tops never missing at
or inside their line; lumps no taller than TOP_LUMP)."""
import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).parents[1]))
import env_bumpy as eb  # noqa: E402

PPU = 196.667


def stone(h=400, w=400):
    rng = np.random.default_rng(3)
    a = np.ones((h, w, 4), np.float32)
    a[..., :3] = rng.uniform(0.3, 0.7, (h, w, 3))
    return a


def test_profiles_wrap_and_stay_in_range():
    for f in (eb.band_profile, eb.deep_profile):
        d = f(800, PPU, np.random.default_rng(1))
        assert d.min() >= 0 and d.max() <= 1
        assert abs(float(d[0]) - float(d[-1])) < 0.25          # periodic (the last sample meets the first)


def test_strip_outline_within_reach_both_ways():
    for reach, prof in ((eb.BAND_REACH, eb.band_profile), (eb.DEEP_REACH, eb.deep_profile)):
        r = np.random.default_rng(2)
        reach_px, inner_px = reach * PPU, (reach + eb.STRIP_OVERLAP) * PPU
        s = eb.cut_strip(stone(), reach_px, inner_px, eb.STRIP_BLEND * PPU, prof(400, PPU, r), r)
        opaque = s[..., 3] > 0.5
        first = opaque.argmax(axis=1)                         # the outline's column per row, from the air side
        assert first.min() >= 0 and first.max() <= 2 * reach_px + 1


def test_raised_top_never_loses_the_line_and_lumps_are_capped():
    h, line = 120, 40
    cap = np.zeros((h, 300, 4), np.float32)
    cap[line:] = stone(h - line, 300)
    lumps = eb.lump_profile(300, eb.TOP_LUMP * PPU, np.random.default_rng(5))
    out = eb.raise_top(cap, line, lumps)
    assert (out[line:, :, 3] >= cap[line:, :, 3] - 1e-6).all()         # nothing at or below the line goes
    above = out[:line, :, 3] > 0.5
    rows = np.where(above.any(axis=0), line - above.argmax(axis=0), 0)
    assert rows.max() <= int(round(eb.TOP_LUMP * PPU)) + 1


def test_lowered_bottom_keeps_its_line():
    h, line = 120, 80
    under = np.zeros((h, 300, 4), np.float32)
    under[:line] = stone(line, 300)
    out = eb.lower_bottom(under, line, eb.lump_profile(300, eb.TOP_LUMP * PPU, np.random.default_rng(6)))
    assert (out[:line, :, 3] >= 0.999).all()
    below = out[line:, :, 3] > 0.5
    assert below.sum(axis=0).max() <= int(round(eb.TOP_LUMP * PPU)) + 1
