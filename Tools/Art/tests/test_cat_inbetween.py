"""PAX-A14 §3 B: offline tests for cat_inbetween (the seam metric and the in-between techniques). Run from Tools/Art:
    python3 -m unittest discover -s tests"""
import sys
import unittest
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).parents[1]))
import cat_inbetween as ib
from test_cat_register import blank, fill, synthetic_cat


def ell(h=96, w=96):
    """An asymmetric L shape, so a rotation is measurable."""
    a = fill(blank(h, w), 30, 70, 44, 54)
    return fill(a, 30, 40, 20, 54)


class SeamTests(unittest.TestCase):
    def test_the_seam_is_the_centroid_step_in_phone_px(self):
        a = fill(blank(64, 64), 10, 20, 10, 20)
        b = fill(blank(64, 64), 18, 28, 10, 20)                   # 8 sprite px to the right
        self.assertAlmostEqual(ib.seam_phone_px(a, (0, 0), b, (0, 0), ppu=160.0), 4.0, places=4)

    def test_the_seam_is_measured_from_each_frames_own_pivot(self):
        a = fill(blank(64, 64), 10, 20, 10, 20)
        b = fill(blank(64, 64), 18, 28, 10, 20)
        self.assertAlmostEqual(ib.seam_phone_px(a, (0, 0), b, (8, 0), ppu=160.0), 0.0, places=4)

    def test_a_pixel_counts_at_alpha_one_half(self):
        a = fill(blank(64, 64), 10, 20, 10, 20)
        a[10:20, 40:50, 3] = 0.49                                  # faint: not drawn
        self.assertAlmostEqual(ib.centroid(a)[0], 15.0, places=4)


class LowerBodyTests(unittest.TestCase):
    def test_the_paw_row_stays_and_the_back_drops(self):
        a = synthetic_cat()
        out = ib.lower_body(a, paw_row=240, drop_px=10.0, leg_px=40.0)
        rows_in, rows_out = np.nonzero(a[..., 3] > 0.5)[0], np.nonzero(out[..., 3] > 0.5)[0]
        self.assertEqual(rows_out.max(), rows_in.max())             # the paws stay planted
        self.assertAlmostEqual(rows_out.min() - rows_in.min(), 10, delta=1)

    def test_columns_never_move(self):
        a = synthetic_cat()
        out = ib.lower_body(a, paw_row=240, drop_px=10.0, leg_px=40.0)
        np.testing.assert_array_equal(np.nonzero((a[..., 3] > 0.5).any(0))[0], np.nonzero((out[..., 3] > 0.5).any(0))[0])

    def test_a_negative_drop_raises_the_body(self):
        a = synthetic_cat()
        out = ib.lower_body(a, paw_row=240, drop_px=-8.0, leg_px=40.0)
        self.assertAlmostEqual(np.nonzero(out[..., 3] > 0.5)[0].min() - np.nonzero(a[..., 3] > 0.5)[0].min(), -8, delta=1)


class RotationTests(unittest.TestCase):
    def test_two_quarter_turns_make_a_half_turn(self):
        a = ell()
        c = (48.0, 48.0)
        twice = ib.rotate_about(ib.rotate_about(a, c, 90.0), c, 90.0)
        np.testing.assert_allclose(twice[..., 3], ib.rotate_about(a, c, 180.0)[..., 3], atol=0.02)

    def test_the_best_rotation_recovers_a_known_turn(self):
        a = ell()
        b = ib.rotate_about(a, (48.0, 48.0), 30.0)
        self.assertAlmostEqual(ib.best_rotation(a, b, step=2.0), 30.0, delta=2.0)

    def test_colour_at_the_edge_is_not_darkened_by_the_transparent_background(self):
        a = ell()
        a[..., :3] = np.where(a[..., 3:] > 0, 0.8, 0.0)
        out = ib.rotate_about(a, (48.0, 48.0), 17.0)
        edge = (out[..., 3] > 0.05) & (out[..., 3] < 0.95)
        self.assertTrue(edge.any(), "a rotation by 17 degrees leaves no soft edge")
        self.assertGreater(float(out[..., 0][edge].min()), 0.75)


class RollTests(unittest.TestCase):
    def test_in_betweens_split_the_turn_and_the_travel_evenly(self):
        a = ell(128, 128)
        b = ib._shift(ib.rotate_about(a, ib.centroid(a), -60.0), 12, 0)
        mids = ib.roll_inbetweens(a, b, 2)
        self.assertEqual(len(mids), 2)
        self.assertAlmostEqual(ib.best_rotation(a, mids[0], step=2.0), -20.0, delta=2.0)
        self.assertAlmostEqual(ib.best_rotation(a, mids[1], step=2.0), -40.0, delta=2.0)
        self.assertAlmostEqual(ib.centroid(mids[0])[0] - ib.centroid(a)[0], 4.0, delta=1.0)
        self.assertAlmostEqual(ib.centroid(mids[1])[0] - ib.centroid(a)[0], 8.0, delta=1.0)


class RaiseTests(unittest.TestCase):
    def test_a_contact_frame_is_the_crouch_raised_over_its_planted_paws(self):
        a = synthetic_cat()
        c = ib.contact_frame(a, paw_row=240, raise_px=12.0, leg_px=60.0)
        self.assertEqual(np.nonzero(c[..., 3] > 0.5)[0].max(), 240)
        self.assertAlmostEqual(np.nonzero(a[..., 3] > 0.5)[0].min() - np.nonzero(c[..., 3] > 0.5)[0].min(), 12, delta=1)


class SpliceTests(unittest.TestCase):
    def test_left_of_the_seam_is_a_right_of_it_is_b(self):
        a = fill(blank(32, 64), 0, 64, 0, 32, rgb=(1, 0, 0))
        b = fill(blank(32, 64), 0, 64, 0, 32, rgb=(0, 0, 1))
        out = ib.splice(a, b, seam_x=32, feather_px=4)
        self.assertEqual(tuple(out[5, 10, :3]), (1.0, 0.0, 0.0))
        self.assertEqual(tuple(out[5, 50, :3]), (0.0, 0.0, 1.0))
        self.assertAlmostEqual(float(out[5, 32, 0]), 0.5, delta=0.15)


class LegOverlapTests(unittest.TestCase):
    def test_matching_legs_overlap_fully_and_disjoint_legs_not_at_all(self):
        a = synthetic_cat()
        self.assertAlmostEqual(ib.leg_overlap(a, (128, 240), a, (128, 240), band_px=40), 1.0)
        moved = synthetic_cat(dx=15)
        self.assertLess(ib.leg_overlap(a, (128, 240), moved, (128, 240), band_px=40), 0.2)
        self.assertAlmostEqual(ib.leg_overlap(a, (128, 240), moved, (143, 240), band_px=40), 1.0)


if __name__ == "__main__":
    unittest.main()
