#!/usr/bin/env python3
"""PAX-A20 Obsidian Void trap art: the painted OT-* sheets (Art_Source/Traps_Obsidian/) in, game-ready sprites out,
deterministically, with their own manifest beside the Golden Ruins kit's (PARALLAX/Art/Import Trap Kit reads both).

  python3 Tools/Art/obsidian_traps.py            # (re)writes Assets/_Game/Art/Traps/Obsidian/ and obsidian_kit.json
  python3 Tools/Art/obsidian_traps.py --check    # fails if a re-run would change any output byte

The laser beam (OT-05's middle row, the full beam) is cut to a tile one unit tall that repeats along x: LaserArt scales
it to a band's thickness and tiles it along the band. Its two ends are cross-faded, so the repeat shows no seam.
"""

import argparse
import hashlib
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image

REPO = Path(__file__).resolve().parents[2]
SRC = REPO / "Art_Source" / "Traps_Obsidian"
OUT = REPO / "Assets" / "_Game" / "Art" / "Traps" / "Obsidian"
MANIFEST = OUT / "obsidian_kit.json"

# OT-05: the middle row (y BEAM_Y0-BEAM_Y1 of the 2017 x 780 sheet) is the full laser beam. A stretch of it BEAM_X0 to
# BEAM_X0 + BEAM_W (clear of the sheet's ends, where the beam fades), its last FADE px cross-faded into its first.
BEAM_Y0, BEAM_Y1, BEAM_X0, BEAM_W, FADE = 250, 456, 520, 960, 160
BEAM_TILE_H = 128   # px: one unit at 128 px per unit
# OT-16 (on magenta): the OFF panel (x GATE_X0-GATE_X1) holds the ceiling emitter (rows GATE_TOP) and the floor emitter
# (rows GATE_BOTTOM). Each is cut, keyed and drawn EMITTER_W units wide at EMITTER_PPU.
GATE_X0, GATE_X1, GATE_TOP, GATE_BOTTOM = 360, 658, (2, 205), (500, 705)
EMITTER_W, EMITTER_PPU = 0.6, 256
# OT-04 (on magenta): the turret housing IDLE (x TURRET_IDLE) and CHARGING (x TURRET_LOCK), rows TURRET_ROWS; drawn
# TURRET_W units wide (TurretArt fits it to the turret's box).
TURRET_IDLE, TURRET_LOCK, TURRET_ROWS, TURRET_W = (69, 830), (942, 1706), (66, 791), 0.7
# OT-06 (the top of the shared OT-06-07 sheet, real alpha): the hard-light bar, rows BAR_ROWS, x BAR_COLS. Drawn BAR_SIZE
# units (a bar's length 1.4 by a thickness that reads on a phone; the painting is thinner, so it is stretched across), and
# its launcher, OT-04's idle housing at BAR_LAUNCHER_W.
BAR_ROWS, BAR_COLS, BAR_SIZE, BAR_LAUNCHER_W = (48, 201), (40, 2123), (1.4, 0.3), 0.5
# Stage 4. OT-13 (on magenta): the flip ring (columns RING_FLIP: arrows up and down) and the inverter ring (RING_INVERT:
# arrows left and right), drawn RING_W and INVERT_W units wide (the Golden Ruins glyph ring's and inverter orb's sizes).
RING_FLIP, RING_INVERT, RING_W, INVERT_W = (40, 880), (900, 1760), 253 / 256, 200 / 256
# OT-10 (on magenta): the crusher's striking edge, its plate's lip and the crimson blades under it (rows PLATE_ROWS, columns
# PLATE_COLS), tiled along a slam's bottom edge PLATE_H units tall.
PLATE_ROWS, PLATE_COLS, PLATE_H = (740, 905), (160, 1380), 0.3
# OT-09 (real alpha): three crack stages (CRACK_BOXES, small to shattering), drawn CRACK_W units wide, and the broken edge
# (EDGE_ROWS), tiled along a glass floor's top, EDGE_H units tall.
CRACK_BOXES = [(56, 164, 349, 574), (433, 103, 858, 577), (917, 100, 1485, 621)]
CRACK_W, EDGE_ROWS, EDGE_H = 1.0, (672, 941), 0.22


def load(name):
    with Image.open(SRC / name) as im:
        return np.asarray(im.convert("RGBA")).astype(np.float32) / 255.0


def to_image(a):
    return Image.fromarray(np.clip(a * 255.0 + 0.5, 0, 255).astype(np.uint8), "RGBA")


def seamless_x(a, fade):
    """The strip with its last `fade` columns blended into its first, then cut to width - fade: tiles with no seam."""
    w = a.shape[1] - fade
    out = a[:, :w].copy()
    t = np.linspace(0.0, 1.0, fade, dtype=np.float32)[None, :, None]
    out[:, :fade] = a[:, w:w + fade] * (1.0 - t) + a[:, :fade] * t
    return out


def beam():
    a = load("OT-05_ray_beam_v1.png")[BEAM_Y0:BEAM_Y1, BEAM_X0:BEAM_X0 + BEAM_W + FADE]
    a = seamless_x(a, FADE)
    h = BEAM_TILE_H
    w = int(round(a.shape[1] * h / a.shape[0]))
    # Premultiplied resize, so the transparent edge's colour doesn't bleed dark into the glow.
    pre = a.copy(); pre[:, :, :3] *= pre[:, :, 3:4]
    img = to_image(pre).resize((w, h), Image.LANCZOS)
    r = np.asarray(img).astype(np.float32) / 255.0
    alpha = r[:, :, 3:4]
    r[:, :, :3] = np.where(alpha > 1e-4, r[:, :, :3] / np.maximum(alpha, 1e-4), 0.0)
    return to_image(r)


def key_magenta(a):
    """Painted on flat magenta: red and blue high, green low becomes clear, with a soft edge; any magenta cast is pulled
    down to green's level (env_v2.py's key)."""
    out = a.copy()
    rgb = out[..., :3]
    key = np.clip((np.minimum(rgb[..., 0], rgb[..., 2]) - rgb[..., 1] - 0.35) / 0.25, 0, 1)
    out[..., 3] = np.minimum(out[..., 3], 1 - key)
    spill = np.clip(np.minimum(rgb[..., 0], rgb[..., 2]) - rgb[..., 1], 0, 1)
    out[..., 0] -= spill
    out[..., 2] -= spill
    out[..., :3] = np.where(out[..., 3:4] > 0.02, np.clip(out[..., :3], 0, 1), 0)
    return out


def trim(a, threshold=0.03):
    ys, xs = np.nonzero(a[..., 3] > threshold)
    return a[ys.min():ys.max() + 1, xs.min():xs.max() + 1]


def resized(a, width):
    """`a` scaled to `width` px wide (premultiplied, so keyed edges don't darken)."""
    h = int(round(a.shape[0] * width / a.shape[1]))
    pre = a.copy(); pre[:, :, :3] *= pre[:, :, 3:4]
    r = np.asarray(to_image(pre).resize((width, h), Image.LANCZOS)).astype(np.float32) / 255.0
    alpha = r[:, :, 3:4]
    r[:, :, :3] = np.where(alpha > 1e-4, r[:, :, :3] / np.maximum(alpha, 1e-4), 0.0)
    return to_image(r)


def emitter(rows):
    a = key_magenta(load("OT-16_pulse_gate_v1.png")[rows[0]:rows[1], GATE_X0:GATE_X1])
    return resized(trim(a), int(round(EMITTER_W * EMITTER_PPU)))


def housing(cols):
    a = key_magenta(load("OT-04_laser_turret_v1.png")[TURRET_ROWS[0]:TURRET_ROWS[1], cols[0]:cols[1]])
    return resized(trim(a), int(round(TURRET_W * EMITTER_PPU)))


def bar():
    a = trim(load("OT-06-07_hard_light_bar_and_drone_v1.png")[BAR_ROWS[0]:BAR_ROWS[1], BAR_COLS[0]:BAR_COLS[1]])
    w, h = int(round(BAR_SIZE[0] * EMITTER_PPU)), int(round(BAR_SIZE[1] * EMITTER_PPU))
    pre = a.copy(); pre[:, :, :3] *= pre[:, :, 3:4]
    r = np.asarray(to_image(pre).resize((w, h), Image.LANCZOS)).astype(np.float32) / 255.0
    alpha = r[:, :, 3:4]
    r[:, :, :3] = np.where(alpha > 1e-4, r[:, :, :3] / np.maximum(alpha, 1e-4), 0.0)
    return to_image(r)


def bar_launcher():
    a = key_magenta(load("OT-04_laser_turret_v1.png")[TURRET_ROWS[0]:TURRET_ROWS[1], TURRET_IDLE[0]:TURRET_IDLE[1]])
    return resized(trim(a), int(round(BAR_LAUNCHER_W * EMITTER_PPU)))


def ring(cols, width_units):
    a = key_magenta(load("OT-13_gravity_rings_v1.png")[:, cols[0]:cols[1]])
    return resized(trim(a), int(round(width_units * EMITTER_PPU)))


def tiled_strip(a, height_units, seam=60):
    """A strip made seamless across and resized to `height_units` tall (its width in proportion)."""
    import env_kit as ek   # noqa: E402 (the env kit's cross-fade)
    a = ek.crossfade(a, seam, 1)
    h = int(round(height_units * EMITTER_PPU))
    w = int(round(a.shape[1] * h / a.shape[0]))
    pre = a.copy(); pre[:, :, :3] *= pre[:, :, 3:4]
    r = np.asarray(to_image(pre).resize((w, h), Image.LANCZOS)).astype(np.float32) / 255.0
    alpha = r[:, :, 3:4]
    r[:, :, :3] = np.where(alpha > 1e-4, r[:, :, :3] / np.maximum(alpha, 1e-4), 0.0)
    return to_image(r)


def plate():
    a = key_magenta(load("OT-10_crusher_v1.png")[PLATE_ROWS[0]:PLATE_ROWS[1], PLATE_COLS[0]:PLATE_COLS[1]])
    return tiled_strip(a, PLATE_H)


def cracks():
    a = load("OT-09_glass_cracks_v1.png")
    return [resized(trim(a[y0:y1, x0:x1]), int(round(CRACK_W * EMITTER_PPU))) for x0, y0, x1, y1 in CRACK_BOXES]


def glass_edge():
    a = load("OT-09_glass_cracks_v1.png")[EDGE_ROWS[0]:EDGE_ROWS[1], 11:1523]
    return tiled_strip(a, EDGE_H)


# D-276: every chasing laser is pale blue-white: the beam's brightness through a ramp from CHASER_GLOW (its soft edge) to
# CHASER_CORE (its core), alpha kept.
CHASER_GLOW, CHASER_CORE = np.array([0.55, 0.78, 1.0], np.float32), np.array([0.96, 0.99, 1.0], np.float32)


def chaser_beam():
    a = np.asarray(beam()).astype(np.float32) / 255.0
    lum = a[..., :3].max(axis=-1, keepdims=True) ** 0.7
    a[..., :3] = CHASER_GLOW * (1 - lum) + CHASER_CORE * lum
    return to_image(a)


def build():
    sprites = {"OBS_laser_beam": (beam(), (0.5, 0.5), True, BEAM_TILE_H),
               "OBS_chaser_beam": (chaser_beam(), (0.5, 0.5), True, BEAM_TILE_H),
               "OBS_gate_top": (emitter(GATE_TOP), (0.5, 0.5), False, EMITTER_PPU),
               "OBS_gate_bottom": (emitter(GATE_BOTTOM), (0.5, 0.5), False, EMITTER_PPU),
               "OBS_turret_idle": (housing(TURRET_IDLE), (0.5, 0.5), False, EMITTER_PPU),
               "OBS_turret_lock": (housing(TURRET_LOCK), (0.5, 0.5), False, EMITTER_PPU),
               "OBS_bar": (bar(), (0.5, 0.5), False, EMITTER_PPU),
               "OBS_bar_launcher": (bar_launcher(), (0.5, 0.5), False, EMITTER_PPU),
               "OBS_flip_ring": (ring(RING_FLIP, RING_W), (0.5, 0.5), False, EMITTER_PPU),
               "OBS_inverter_ring": (ring(RING_INVERT, INVERT_W), (0.5, 0.5), False, EMITTER_PPU),
               "OBS_slam_plate": (plate(), (0.5, 0.0), True, EMITTER_PPU),
               "OBS_glass_edge": (glass_edge(), (0.5, 1.0), True, EMITTER_PPU)}
    for i, c in enumerate(cracks()):
        sprites[f"OBS_crack_{i}"] = (c, (0.5, 0.5), False, EMITTER_PPU)
    entries, files = [], {}
    for name, (img, pivot, tiled, ppu) in sprites.items():
        path = OUT / f"{name}.png"
        files[path] = img
        entries.append({"normal": "", "path": str(path.relative_to(REPO)), "pivotX": pivot[0], "pivotY": pivot[1],
                        "placeholder": "", "ppu": ppu, "tiled": tiled})
    return files, {"entries": entries, "ppu": BEAM_TILE_H}


def png_bytes(img):
    import io
    b = io.BytesIO(); img.save(b, "PNG", optimize=False); return b.getvalue()


def main():
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("--check", action="store_true")
    args = p.parse_args()
    files, manifest = build()
    text = json.dumps(manifest, indent=2, sort_keys=True) + "\n"
    if args.check:
        stale = [str(f) for f, img in files.items() if not f.exists() or f.read_bytes() != png_bytes(img)]
        if not MANIFEST.exists() or MANIFEST.read_text() != text: stale.append(str(MANIFEST))
        if stale: sys.exit("obsidian_traps.py: would change " + ", ".join(stale))
        print("obsidian_traps.py: up to date"); return
    OUT.mkdir(parents=True, exist_ok=True)
    for f, img in files.items():
        f.write_bytes(png_bytes(img))
        print(f"{f.relative_to(REPO)}  {img.size[0]}x{img.size[1]}  {hashlib.sha1(f.read_bytes()).hexdigest()[:10]}")
    MANIFEST.write_text(text)
    print(MANIFEST.relative_to(REPO))


if __name__ == "__main__":
    main()
