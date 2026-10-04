import sys
from pathlib import Path

import numpy as np
from PIL import Image

sys.path.insert(0, str(Path(__file__).parents[1]))
import hazard_readable as hr  # noqa: E402

REPO = Path(__file__).resolve().parents[3]
ENV = REPO / "Assets" / "_Game" / "Art" / "RealityA" / "Environment"


def px(*rgb):
    return np.array([[rgb]], np.float32)


def test_the_danger_colour_is_crimson_and_distinct_from_amber_and_gold():
    # The developer's ruling (PAX-V08): red, pushed toward crimson; Ember Night's braziers and Golden Ruins' gold stay
    # amber/yellow, never the danger hue.
    assert hr.is_danger_hue(px(*hr.DANGER))[0, 0]
    assert 330 < hr.DANGER_HUE < 360
    for warm in [(1.0, 0.62, 0.12), (1.0, 0.5, 0.15), (0.9, 0.75, 0.2), (0.85, 0.55, 0.25), (1.0, 0.8, 0.5)]:
        assert not hr.is_danger_hue(px(*warm))[0, 0], warm


def test_out_of_danger_moves_crimson_to_amber_and_leaves_everything_else_bit_identical():
    a = np.zeros((1, 3, 4), np.float32)
    a[0, 0] = [*hr.DANGER, 1]
    a[0, 1] = [0.2, 0.5, 0.3, 1]
    a[0, 2] = [*hr.DANGER, 0.05]          # a faint fringe: left as it is
    out = hr.out_of_danger(a)
    assert not hr.is_danger_hue(out[..., :3])[0, 0]
    assert np.array_equal(out[0, 1], a[0, 1]) and np.array_equal(out[0, 2], a[0, 2])
    assert hr.out_of_danger(a[:, 1:2]) is a[:, 1:2] or np.array_equal(hr.out_of_danger(a[:, 1:2]), a[:, 1:2])


def test_outline_draws_a_dark_line_outside_and_a_light_rim_inside():
    a = np.zeros((12, 12, 4), np.float32)
    a[4:8, 4:8] = [0.5, 0.5, 0.5, 1]
    out = hr.outline(a, outer_px=2, inner_px=1)
    assert np.allclose(out[2, 6, :3], hr.OUTER) and out[2, 6, 3] == 1      # the line, two pixels out
    assert np.allclose(out[4, 6, :3], hr.INNER)                            # the rim, the silhouette's edge
    assert np.allclose(out[5, 5, :3], 0.5)                                  # the inside, untouched
    assert out[0, 0, 3] == 0


def test_a_tiled_strips_outline_wraps_across_its_seam():
    a = np.zeros((10, 8, 4), np.float32)
    a[4:, :] = [0.1, 0.1, 0.1, 1]                                           # a strip that runs off both sides
    out = hr.outline(a, outer_px=2, inner_px=1, wrap_x=True)
    assert np.array_equal(out[:, 0], out[:, 7]) and np.array_equal(out[:, 1], out[:, 6])


def test_no_environment_art_carries_the_danger_colour():
    # The kit and background art (written by env_kit.py / env_v2.py); the A02 originals (A_*.png) are grey-box or retired.
    pngs = [p for p in ENV.rglob("*.png") if "/Kit/" in str(p) and not p.name.endswith("_n.png")]
    assert pngs, "no environment art found"
    found = []
    for p in pngs:
        a = np.asarray(Image.open(p).convert("RGBA")).astype(np.float32) / 255
        n = int((hr.is_danger_hue(a[..., :3]) & (a[..., 3] > 0.1)).sum())
        if n: found.append(f"{p.relative_to(ENV)}: {n} px")
    assert not found, "danger colour in environment art:\n" + "\n".join(found)
