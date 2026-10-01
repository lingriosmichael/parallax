import json
import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).parents[1]))
import env_kit as tool  # noqa: E402


def ramp(w=200, h=40):
    a = np.zeros((h, w, 4), np.float32)
    a[..., 0] = np.linspace(0, 1, w)[None, :]
    a[..., 3] = 1
    return a


def test_crossfade_makes_a_ramp_wrap_seamlessly():
    a = ramp()
    assert tool.seam_ratio(a, 1) > 50
    out = tool.crossfade(a, 40, 1)
    assert out.shape == (40, 160, 4)
    assert tool.seam_ratio(out, 1) < 3


def test_crossfade_vertical_axis():
    a = np.transpose(ramp(), (1, 0, 2)).copy()
    out = tool.crossfade(a, 40, 0)
    assert out.shape[0] == 160 and tool.seam_ratio(out, 0) < 3


def test_crossfade_rejects_a_band_over_half():
    try:
        tool.crossfade(ramp(), 100, 1)
    except ValueError:
        return
    raise AssertionError("expected ValueError")


def test_split_pieces_finds_separate_and_stacked_pieces():
    a = np.zeros((400, 400, 4), np.float32)
    a[20:120, 20:120, 3] = 1      # top-left
    a[20:120, 250:380, 3] = 1     # top-right
    a[200:380, 0:400, 3] = 1      # a full-width piece below
    boxes = tool.split_pieces(a, min_area=100)
    assert len(boxes) == 3
    assert (20, 20, 120, 120) in [tuple(int(v) for v in b) for b in boxes]


def test_split_halves_trims_each_side():
    a = np.zeros((100, 200, 4), np.float32)
    a[10:50, 20:60, 3] = 1
    a[30:90, 130:180, 3] = 1
    left, right = tool.split_halves(a, 100)
    assert tuple(int(v) for v in left) == (20, 10, 60, 50)
    assert tuple(int(v) for v in right) == (130, 30, 180, 90)


def test_glow_alpha_keeps_colour_bright_at_soft_edges():
    a = np.zeros((1, 3, 4), np.float32)
    a[0, 0, :3] = [0.6, 0.4, 0.2]     # bright
    a[0, 1, :3] = [0.06, 0.04, 0.02]  # a dim edge of the same hue
    out = tool.glow_alpha(a, floor=0.0, ceil=0.6)
    assert out[0, 0, 3] == 1.0 and 0 < out[0, 1, 3] < 0.2
    assert np.allclose(out[0, 1, :3], out[0, 0, :3], atol=1e-3)
    assert out[0, 2, 3] == 0.0


def test_vertical_fade_is_opaque_at_top_and_clear_at_bottom():
    f = tool.vertical_fade(4, 64)
    assert f[0, 0, 3] == 1.0 and f[-1, 0, 3] == 0.0 and np.all(np.diff(f[:, 0, 3]) <= 0)


def test_written_manifest_lists_every_kit_png():
    manifest = tool.KIT / "env_kit.json"
    if not manifest.exists():
        return  # the kit hasn't been written in this checkout
    data = json.loads(manifest.read_text())
    assert abs(data["worldPpu"] - 196.66666) < 1e-3
    for slot in data["slots"]:
        folder = tool.KIT if slot["folder"] == "Kit" else tool.LAYERS
        assert (folder / slot["file"]).exists(), slot["file"]
        if slot.get("normal"):
            assert (folder / slot["file"].replace(".png", "_n.png")).exists()


# ---------- PAX-A16 tiers ----------

def test_soft_base_fades_only_the_lowest_rows():
    a = np.ones((100, 4, 4), np.float32)
    out = tool.soft_base(a, 0.2)
    assert np.all(out[:80, :, 3] == 1.0) and out[-1, 0, 3] == 0.0 and np.all(np.diff(out[:, 0, 3]) <= 0)


def test_wash_fades_from_its_top_alpha_to_clear():
    import env_tiers
    w = env_tiers.wash(3, 50, (1.0, 0.8, 0.5), 0.6)
    assert abs(w[0, 0, 3] - 0.6) < 1e-6 and w[-1, 0, 3] < 0.01 and np.all(np.diff(w[:, 0, 3]) <= 0)


def test_chain_tiles_downward():
    import env_tiers
    c = env_tiers.chain()
    assert c.shape[0] % 40 == 0 and tool.seam_ratio(c, axis=0) < 3.0 and c[..., 3].max() > 0.9


if __name__ == "__main__":
    for name, fn in sorted(globals().items()):
        if name.startswith("test_") and callable(fn):
            fn()
            print("ok", name)
