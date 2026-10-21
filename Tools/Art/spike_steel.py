"""D-161 (the developer picked "option 3" on 2026-10-20): stout steel spike blades, SPIKE_steel_<i>.

The painted iron blades (SPIKE_blade_<i>, trap_v2.py) are thin, dark and pale-rimmed, and among grass tufts they read as
grass. These are the same shapes repainted: a cold steel body (bright edge, dark core) and a red tip, which stand out
against warm stone and green tufts. The level builder draws them wider and taller than the standard blades
(TrapArtConfig.StoutBladeScale) for levels whose look asks for them (LevelLooks.Look.StoutSpikes).

trap_process.py calls build() after trap_v2, so a full re-run keeps them. Run this file on its own to make them from the
blades already in the kit, and add them to Assets/_Game/Art/Traps/trap_kit.json. Then run PARALLAX/Art/Import Trap Kit.
"""
import json
import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
import trap_process as tp  # noqa: E402

BLADE_PPU = 256
RED_TIP = np.array([1.0, 0.36, 0.3], np.float32)
BRIGHT_STEEL = np.array([0.82, 0.86, 0.92], np.float32)
MID_STEEL = np.array([0.34, 0.37, 0.44], np.float32)
CORE_DARK = np.array([0.07, 0.08, 0.1], np.float32)
CORE_LIGHT = np.array([0.18, 0.2, 0.25], np.float32)
OUTLINE_PX = 3      # the painted blades' dark outer line (trap_v2: outer_px=3)
EDGE_GROW_PX = 1


def recolor(a):
    """A blade (RGBA floats 0..1) repainted in steel: its crimson tip red, its lit rim bright steel, its edges mid steel and
    its core dark steel, by the painted pixel's own colour and value. Alpha is kept."""
    rgb = a[..., :3]
    lum = rgb @ np.array([0.3, 0.59, 0.11], np.float32)
    core = CORE_DARK + (CORE_LIGHT - CORE_DARK) * np.clip(lum / 0.2, 0, 1)[..., None]
    out = np.where((lum > 0.2)[..., None], MID_STEEL, core)
    out = np.where((lum > 0.55)[..., None], BRIGHT_STEEL, out)
    tip = (rgb[..., 0] - rgb[..., 1] > 0.12) & (rgb[..., 0] > 0.5)
    # The bright edge a pixel thicker, into the dark core only (never the outer dark line): a stout blade's core is mostly
    # dark, and against dark stone only its bright edge reaches 3:1 (HazardContrastTests).
    solid = a[..., 3] >= 0.5
    inner = _erode(solid, OUTLINE_PX)
    bright = (lum > 0.55) & ~tip
    thicker = inner & (lum <= 0.2) & _dilate(bright, EDGE_GROW_PX)
    out = np.where(thicker[..., None], BRIGHT_STEEL, out)
    out = np.where(tip[..., None], RED_TIP, out)
    painted = a[..., 3] >= 0.01
    res = a.copy()
    res[..., :3] = np.where(painted[..., None], out, rgb)
    return res


def _dilate(m, px):
    out = m.copy()
    for _ in range(px):
        grown = out.copy()
        grown[1:] |= out[:-1]; grown[:-1] |= out[1:]; grown[:, 1:] |= out[:, :-1]; grown[:, :-1] |= out[:, 1:]
        out = grown
    return out


def _erode(m, px):
    return ~_dilate(~m, px)


def blades():
    """The kit's painted blades, in index order."""
    found = [p for p in (tp.OUT / "Bodies").glob("SPIKE_blade_*.png") if not p.stem.endswith("_n")]
    return sorted(found, key=lambda p: int(p.stem.rsplit("_", 1)[1]))


def build(emit, load_rgba):
    for path in blades():
        i = path.stem.rsplit("_", 1)[1]
        emit(f"Bodies/SPIKE_steel_{i}", recolor(load_rgba(path)), pivot=(0.5, 0.0), ppu=BLADE_PPU, sources=[path])


def main():
    manifest = json.loads(tp.MANIFEST.read_text())

    def emit(name, a, pivot=(0.5, 0.5), normal=True, tiled=False, sources=(), placeholder=None, ppu=None):
        path = tp.OUT / f"{name}.png"
        tp.save(a, path)
        npath = tp.OUT / f"{name}_n.png"
        tp.save(tp.normal_map(a) if normal else tp.normal_map(a, strength=0.0, bevel_px=0.0), npath)
        manifest["sprites"][name] = {"path": str(path.relative_to(tp.REPO)), "size_px": [a.shape[1], a.shape[0]], "pivot": list(pivot),
                                     "tiled": tiled, "palette": tp.palette(a), "sources": [str(Path(s).relative_to(tp.REPO)) for s in sources],
                                     "placeholder": placeholder or "", "ppu": ppu or tp.PPU, "normal": str(npath.relative_to(tp.REPO))}

    build(emit, tp.load_rgba)
    manifest["entries"] = [{"path": e["path"], "pivotX": e["pivot"][0], "pivotY": e["pivot"][1], "tiled": e["tiled"], "normal": e.get("normal", ""),
                            "placeholder": e.get("placeholder", ""), "ppu": e.get("ppu", tp.PPU)}
                           for _, e in sorted(manifest["sprites"].items())]
    tp.MANIFEST.write_text(json.dumps(manifest, indent=2, sort_keys=True) + "\n")
    print(f"{len(blades())} steel blades written")
    return 0


if __name__ == "__main__":
    sys.exit(main())
