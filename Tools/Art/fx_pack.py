"""D-312: Premium Pack I, the developer's effect sheets (Docs/Art/PREMIUM_PACK_I_PROMPTS.md) cut into the trap kit's effects for
PAX-V06. Each sheet was painted as a grid of separate pieces; a piece is every connected shape whose centre falls in its cell
(a cell line can cross a wisp or a spark's streak). A sheet is scaled as one, so its pieces keep their painted sizes against
each other. trap_process.py calls build() after the other bodies, so a full re-run keeps them."""
from pathlib import Path

import numpy as np
from scipy import ndimage

from trap_process import PPU, luminance_alpha, resize, trim
from trap_v2 import key_blue, key_magenta
import premium_pack

REPO = Path(__file__).resolve().parents[2]
SRC = REPO / "Art_Source" / "Effects"
STONE = REPO / "Assets/_Game/Art/RealityA/Environment/Kit/ENV_Fill_A.png"
SAND = np.array([0.86, 0.79, 0.68], np.float32)

# name: (key, columns, rows, names by cell in reading order, the largest piece's width in units)
SHEETS = {
    "FX-01_dust_v1": ("blue", 4, 2, ["FX_Puff_0", "FX_Puff_1", "FX_Puff_2", "FX_Puff_3", "FX_DustLand_0", "FX_DustLand_1", "FX_DustSkid_0", "FX_DustSkid_1"], 1.0),
    "FX-02_debris_golden_v1": ("blue", 4, 3, [f"FX_Debris_{i}" for i in range(12)], 0.45),
    "FX-03_sparks_v1": ("black", 4, 2, ["FX_Spark_0", "FX_Spark_1", "FX_Spark_2", "FX_Spark_3", "FX_SparkFan_0", "FX_SparkFan_1", "FX_SparkFan_2", "FX_SparkFan_3"], 0.7),
    "FX-04_impact_v1": ("blue", 1, 3, ["FX_Impact_0", "FX_Impact_1", "FX_Impact_2"], 3.0),
    "FX-05_door_burst_v1": ("black", 1, 1, ["FX_DoorBurst"], 3.0),
    "FX-06_obsidian_v1": ("magenta", 4, 3, [f"FX_Shard_{i}" for i in range(8)] + [f"FX_SparkCyan_{i}" for i in range(4)], 0.5),
}


def unmix_blue(a, warmth=0.45):
    """Soft dust on flat blue: each pixel is the dust (whose red/green beat its blue by about `warmth`) laid over pure blue at
    some cover; the cover is solved for and the blue divided out. Keyed like a hard edge, the soft rim kept the blue as purple."""
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    cover = np.clip((1 - (b - np.maximum(r, g))) / (1 + warmth), 0, 1)
    safe = np.maximum(cover, 1e-3)
    rgb = np.dstack([r / safe, g / safe, (b - (1 - cover)) / safe])
    return np.dstack([np.clip(rgb, 0, 1), np.minimum(a[..., 3], cover)])


def keyed(a, how, soft):
    if how == "black":   # a light: brightness is its cover; the soft burst on a gentle ramp (steep, it read as a hard star)
        return luminance_alpha(a, floor=0.02, ceil=1.0) if soft else luminance_alpha(a, floor=0.03, ceil=0.6)
    if how == "blue" and soft: return unmix_blue(a)
    a = key_blue(a) if how == "blue" else key_magenta(a)
    return premium_pack.fringe(a, 2)


def cells(a, columns, rows, join_px=10):
    """Each grid cell's piece, in reading order: the connected shapes (joined within `join_px`) whose centres fall in it."""
    h, w = a.shape[:2]
    labels, n = ndimage.label(ndimage.binary_dilation(a[..., 3] > 0.04, iterations=join_px))
    centres = ndimage.center_of_mass(np.ones((h, w)), labels, range(1, n + 1))
    pieces = []
    for r in range(rows):
        for c in range(columns):
            mine = [i + 1 for i, (cy, cx) in enumerate(centres) if int(cy * rows // h) == r and int(cx * columns // w) == c]
            if not mine:
                raise ValueError(f"cell ({r}, {c}) is empty")
            p = a.copy()
            p[..., 3] *= np.isin(labels, mine)
            pieces.append(trim(p))
    return pieces


def build(emit, load_rgba):
    stone = premium_pack.load(STONE)[::4, ::4]
    for sheet, (how, columns, rows, names, widest_u) in SHEETS.items():
        path = SRC / f"{sheet}.png"
        if not path.exists(): continue
        soft = sheet.startswith(("FX-01", "FX-04", "FX-05"))   # dust and light: soft edges
        pieces = cells(keyed(load_rgba(path), how, soft), columns, rows)
        if sheet.startswith("FX-02"):   # the stone's own colour (the sheet came back orange, like every painting)
            pieces = [premium_pack.match(p, stone, lift=1.05, keep=0.08) for p in pieces]
        if sheet.startswith(("FX-01", "FX-04")):   # dust half way to pale sand: orange, it read as fire over dark stone
            for p in pieces: p[..., :3] = p[..., :3] * 0.5 + SAND * 0.5
        k = widest_u * PPU / max(p.shape[1] for p in pieces)
        for name, p in zip(names, pieces):
            emit(f"Effects/{name}", resize(p, max(1, round(p.shape[1] * k)), max(1, round(p.shape[0] * k))), normal=False, sources=[path])
