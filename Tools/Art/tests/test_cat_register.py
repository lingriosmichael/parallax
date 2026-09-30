"""PAX-A08 Stage 0: offline tests for cat_register (no Unity, no AutoSprite call). Run from Tools/Art:
    python3 -m unittest discover -s tests"""
import sys
import unittest
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).parents[1]))
import cat_register as cr


def blank(h=256, w=256):
    return np.zeros((h, w, 4), np.float32)


def fill(a, x0, x1, y0, y1, rgb=(0.24, 0.18, 0.14)):
    a[y0:y1, x0:x1, :3] = rgb
    a[y0:y1, x0:x1, 3] = 1.0
    return a


def synthetic_cat(dx=0, dy=0):
    """A side-view block cat: torso x 90..217, back line at row 131, legs to paw row 240, head and ears above the back
    at the front, a thin tail at the back."""
    a = blank()
    fill(a, 90 + dx, 218 + dx, 131 + dy, 200 + dy)          # torso
    for x in (95, 130, 170, 205):                             # legs
        fill(a, x + dx, x + 10 + dx, 200 + dy, 241 + dy)
    fill(a, 190 + dx, 240 + dx, 80 + dy, 140 + dy)           # head and neck
    fill(a, 40 + dx, 90 + dx, 150 + dy, 156 + dy)            # tail (thin)
    return a


class LoopTests(unittest.TestCase):
    def test_loop_counts_divide_the_source_loop_evenly(self):
        self.assertEqual(cr.pick_loop_count(20, 10), 10)
        self.assertEqual(cr.pick_loop_count(10, 10), 10)
        self.assertEqual(cr.pick_loop_count(22, 11), 11)
        self.assertEqual(cr.pick_loop_count(25, 5), 5)

    def test_an_uneven_target_takes_the_nearest_even_divisor_or_the_whole_loop(self):
        self.assertEqual(cr.pick_loop_count(22, 10), 11)     # 11 divides 22 and is within 50% of 10
        self.assertEqual(cr.pick_loop_count(7, 3), 7)        # no divisor within 50%: keep the whole loop

    def test_loop_frames_take_a_fixed_stride_from_the_window_start(self):
        self.assertEqual(cr.loop_frames(22, 20, 10), list(range(22, 42, 2)))
        self.assertEqual(cr.loop_frames(24, 25, 5), [24, 29, 34, 39, 44])

    def test_mask_iou(self):
        m = np.zeros((8, 8), bool); m[2:6, 2:6] = True
        self.assertAlmostEqual(cr.mask_iou(m, m), 1.0)
        other = np.zeros((8, 8), bool); other[0:2, 0:2] = True
        self.assertAlmostEqual(cr.mask_iou(m, other), 0.0)


class MeasureTests(unittest.TestCase):
    def test_body_height_is_paw_row_to_back_line_ignoring_head_and_tail(self):
        m = cr.measure_body(synthetic_cat())
        self.assertEqual(m["pawRow"], 240)
        self.assertEqual(m["backRow"], 131)
        self.assertEqual(m["bodyHeightPx"], 109)
        self.assertAlmostEqual(m["torsoX0"], 90, delta=1)
        self.assertAlmostEqual(m["torsoX1"], 217, delta=1)

    def test_collider_margins_to_nose_and_tail_tip(self):
        # 100 px per unit, pivot at x 128: the collider spans 78..178; the painted cat covers pixels 60..200 (edges 60 and
        # 201). Margins are how far the painting reaches past the collider: positive = the collider stays inside.
        a = fill(blank(), 60, 201, 100, 200)
        margins = cr.collider_margins(a, pivot_x=128, ppu=100.0, facing_right=True)
        self.assertAlmostEqual(margins["nose"], 0.23, places=3)
        self.assertAlmostEqual(margins["tail"], 0.18, places=3)
        flipped = cr.collider_margins(a[:, ::-1], pivot_x=256 - 128, ppu=100.0, facing_right=False)
        self.assertAlmostEqual(flipped["nose"], 0.23, places=3)
        self.assertAlmostEqual(flipped["tail"], 0.18, places=3)


class CleanupTests(unittest.TestCase):
    def test_faint_alpha_is_zeroed_and_light_grey_halo_removed(self):
        a = fill(blank(16, 16), 4, 12, 4, 12)
        a[1, 1] = (0.2, 0.2, 0.2, 0.05)                  # below 10% alpha
        a[2, 2] = (0.9, 0.9, 0.9, 0.4)                   # light grey fringe
        out = cr.cleanup(a)
        self.assertEqual(out[1, 1, 3], 0.0)
        self.assertEqual(out[2, 2, 3], 0.0)
        self.assertEqual(out[6, 6, 3], 1.0)

    def test_vine_key_removes_green_and_bright_stem_in_the_vine_band_but_keeps_the_dark_cat(self):
        a = fill(blank(), 100, 160, 80, 200)                          # dark cat
        fill(a, 150, 158, 0, 256, rgb=(0.35, 0.45, 0.12))           # olive vine through it
        fill(a, 158, 162, 0, 256, rgb=(0.62, 0.45, 0.16))           # golden-brown stem beside it
        out = cr.key_vine(a)
        self.assertEqual(out[10, 154, 3], 0.0)       # vine above the cat
        self.assertEqual(out[10, 160, 3], 0.0)       # stem
        self.assertEqual(out[150, 120, 3], 1.0)      # cat body away from the vine
        self.assertEqual(out[150, 154, 3], 0.0)      # vine painted over the cat: gone (the review sheet shows the cat)


class VineOutlineTests(unittest.TestCase):
    def test_the_dark_ink_outline_hugging_the_vine_goes_with_it(self):
        a = fill(blank(), 60, 130, 80, 200)                                  # dark cat, left of the vine
        fill(a, 150, 158, 0, 256, rgb=(0.35, 0.45, 0.12))                  # olive vine
        fill(a, 148, 150, 0, 256, rgb=(0.10, 0.07, 0.05))                  # its dark ink outline, as dark as fur
        out = cr.key_vine(a)
        self.assertEqual(out[40, 149, 3], 0.0)
        self.assertEqual(out[150, 90, 3], 1.0)

    def test_fragments_not_connected_to_the_body_are_dropped(self):
        a = fill(blank(), 60, 160, 80, 200)                                  # the cat
        fill(a, 200, 204, 10, 30)                                            # a stray dark fragment
        fill(a, 220, 228, 0, 256, rgb=(0.35, 0.45, 0.12))                  # a vine (so the key runs)
        out = cr.key_vine(a)
        self.assertEqual(out[20, 202, 3], 0.0)
        self.assertEqual(out[150, 100, 3], 1.0)


class PlacementTests(unittest.TestCase):
    def test_ground_placement_puts_the_lowest_paw_on_the_paw_row_and_the_torso_centre_on_the_pivot(self):
        frames = [synthetic_cat(dx=-20, dy=-10)]
        placed, report = cr.place(frames, role="ground", pivot_x=154.0, paw_row=240)
        m = cr.measure_body(placed[0])
        self.assertEqual(m["pawRow"], 240)
        self.assertAlmostEqual((m["torsoX0"] + m["torsoX1"] + 1) / 2, 154.0, delta=1)
        self.assertEqual(report["clippedPx"], 0)

    def test_pixels_pushed_out_of_the_cell_are_counted(self):
        frames = [synthetic_cat(dx=0, dy=0)]
        _, report = cr.place(frames, role="ground", pivot_x=20.0, paw_row=240)
        self.assertGreater(report["clippedPx"], 0)


class DeterminismTests(unittest.TestCase):
    def test_the_same_input_gives_the_same_sheet_bytes(self):
        frames = [synthetic_cat(), synthetic_cat(dx=3)]
        b1 = cr.sheet_bytes(frames)
        b2 = cr.sheet_bytes([f.copy() for f in frames])
        self.assertEqual(b1, b2)
        self.assertEqual(len(cr.sheet_image(frames).size), 2)
        self.assertEqual(cr.sheet_image(frames).size, (2 * 256, 256))


if __name__ == "__main__":
    unittest.main()
