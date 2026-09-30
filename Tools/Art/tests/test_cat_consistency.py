"""PAX-A14 §3 A: offline tests for cat_consistency (scale, style, specks, reach). Run from Tools/Art:
    python3 -m unittest discover -s tests"""
import sys
import unittest
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).parents[1]))
import cat_consistency as cc
from cat_frames import scale_frame
from test_cat_register import blank, fill, synthetic_cat


def shaded(a, levels=(0.10, 0.15, 0.20, 0.60)):
    """`a` with its opaque pixels in horizontal bands of four greys (a dark coat with a bright rim band)."""
    out = a.copy()
    ys = np.nonzero(out[..., 3] > 0)[0]
    edges = np.linspace(ys.min(), ys.max() + 1, len(levels) + 1).astype(int)
    for lv, y0, y1 in zip(levels, edges[:-1], edges[1:]):
        band = out[y0:y1]
        on = band[..., 3] > 0
        band[on, 0], band[on, 1], band[on, 2] = lv * 1.4, lv, lv * 0.8
    return out


def opaque_luma(a):
    """The test's own luma percentiles (0, 5, ..., 100) over the opaque pixels, independent of cat_consistency."""
    rgb = a[..., :3][a[..., 3] > 0.9]
    return np.percentile(rgb @ np.array([0.299, 0.587, 0.114]), np.arange(0, 101, 5))


class ScaleTests(unittest.TestCase):
    def test_the_torso_scale_undoes_a_drawing_made_smaller(self):
        ref = [synthetic_cat()]
        small = [scale_frame(synthetic_cat(), 0.8)]
        self.assertAlmostEqual(cc.torso_scale(ref, small), 1.25, delta=0.03)

    def test_the_torso_ignores_legs_tail_and_ears(self):
        a = synthetic_cat()
        longer_tail = fill(a.copy(), 10, 90, 150, 156)           # the thin tail doubles in length
        self.assertAlmostEqual(cc.core_length(a), 150, delta=3)  # torso x 90..217 and the head to 239
        self.assertEqual(cc.core_length(a), cc.core_length(longer_tail))

    def test_a_frame_with_no_core_measures_zero(self):
        self.assertEqual(cc.core_length(fill(blank(64, 64), 10, 50, 30, 33)), 0)


class StyleTests(unittest.TestCase):
    def test_matching_moves_the_luma_distribution_onto_the_reference(self):
        ref = [shaded(synthetic_cat(), (0.08, 0.12, 0.14, 0.18))]
        src = [shaded(synthetic_cat(), (0.10, 0.15, 0.20, 0.60))]
        out = cc.match_style(src[0], cc.luma_quantiles(src), cc.luma_quantiles(ref))
        got, want = opaque_luma(out), opaque_luma(ref[0])
        self.assertLess(float(np.abs(got - want).max()), 0.01)
        self.assertGreater(float(np.abs(opaque_luma(src[0]) - want).max()), 0.1)

    def test_matching_never_changes_alpha_and_keeps_the_hue(self):
        src = shaded(synthetic_cat())
        src[100:110, 100:110, 3] = 0.4                           # some half-transparent fur
        out = cc.match_style(src, cc.luma_quantiles([src]), cc.luma_quantiles([shaded(synthetic_cat(), (0.05, 0.07, 0.09, 0.1))]))
        np.testing.assert_array_equal(out[..., 3], src[..., 3])
        on = src[..., 3] > 0
        ratio_in = src[..., 0][on] / np.maximum(src[..., 1][on], 1e-6)
        ratio_out = out[..., 0][on] / np.maximum(out[..., 1][on], 1e-6)
        self.assertLess(float(np.abs(ratio_in - ratio_out).max()), 1e-3)

    def test_matching_a_clip_to_itself_changes_nothing(self):
        src = shaded(synthetic_cat())
        q = cc.luma_quantiles([src])
        np.testing.assert_allclose(cc.match_style(src, q, q), src, atol=1e-5)


class SpeckTests(unittest.TestCase):
    def speckled(self):
        a = fill(blank(64, 64), 10, 40, 10, 40)
        a[50, 50, 3] = 0.3                                        # a faint one-pixel speck
        a[5:7, 55:58, 3] = 0.2                                    # a faint fringe fragment
        a[5:7, 55:58, :3] = 0.8
        fill(a, 50, 55, 20, 25)                                   # a solid detached piece (a spark, a tail tip): kept
        a[40, 40, 3] = 0.2                                        # faint, but touching the body diagonally: kept
        return a

    def test_faint_detached_islands_are_removed(self):
        a = self.speckled()
        self.assertEqual(cc.detached_islands(a), 2)
        out = cc.drop_specks(a)
        self.assertEqual(cc.detached_islands(out), 0)
        self.assertEqual(out[50, 50, 3], 0.0)
        self.assertTrue((out[5:7, 55:58] == 0).all())

    def test_the_body_solid_pieces_and_attached_fur_stay(self):
        a = self.speckled()
        out = cc.drop_specks(a)
        np.testing.assert_array_equal(out[10:40, 10:40], a[10:40, 10:40])
        np.testing.assert_array_equal(out[20:25, 50:55], a[20:25, 50:55])
        self.assertAlmostEqual(float(out[40, 40, 3]), 0.2, places=6)


class ReachTests(unittest.TestCase):
    def test_reach_is_the_front_of_the_lowest_band_ahead_of_the_pivot(self):
        a = fill(blank(64, 64), 10, 30, 40, 60)                  # the body, down to the paw row 59
        fill(a, 30, 50, 20, 30)                                   # a raised fore paw well above the floor band
        self.assertAlmostEqual(cc.reach(a, pivot_x=20.0, ppu=10.0, band_px=8), 1.0)


if __name__ == "__main__":
    unittest.main()
