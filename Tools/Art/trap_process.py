#!/usr/bin/env python3
"""PAX-A13 trap kit post-processing (§6.5): the committed raw files in, game-ready sprites out, deterministically.

  python3 Tools/Art/trap_process.py            # (re)writes Assets/_Game/Art/Traps/ and its manifest
  python3 Tools/Art/trap_process.py --check    # fails if a re-run would change any output byte

Every output is a pure function of the raw inputs and the constants below: the same raws give the same bytes, so a
re-run produces no diff. Sizes are 128 px per unit (the play layer, rulings §11 R1). Bodies get a normal map (from their
luminance and an alpha bevel) so 2D lights shade them like the environment. The palette check flags cyan (Reality B's
colour) and black shading (LOOK_AND_FEEL §4); flags are written to the manifest and printed.
"""

import argparse
import hashlib
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter

REPO = Path(__file__).resolve().parents[2]
OUT = REPO / "Assets" / "_Game" / "Art" / "Traps"
MANIFEST = OUT / "trap_kit.json"
PPU = 128

CHATGPT = REPO / "Art_Source" / "Traps_ChatGPT"
AUTOSPRITE = REPO / "Art_Source" / "AutoSprite" / "Traps"


# ---------- helpers ----------

def load_rgba(path):
    with Image.open(path) as im:
        return np.asarray(im.convert("RGBA")).astype(np.float32) / 255.0


def to_image(a):
    return Image.fromarray((np.clip(a, 0.0, 1.0) * 255.0 + 0.5).astype(np.uint8), "RGBA")


def luminance_alpha(a, floor=0.06, ceil=0.55):
    """A black-background effect: brightness becomes alpha, colour is un-premultiplied so it blends normally."""
    rgb = a[..., :3]
    lum = rgb.max(axis=-1)
    alpha = np.clip((lum - floor) / (ceil - floor), 0.0, 1.0)
    colour = np.where(alpha[..., None] > 1e-3, np.clip(rgb / np.maximum(lum[..., None], 1e-3) * np.minimum(lum[..., None] / ceil, 1.0), 0, 1), 0.0)
    return np.dstack([colour, alpha])


def trim(a, threshold=0.03, pad=2):
    ys, xs = np.nonzero(a[..., 3] > threshold)
    if len(xs) == 0:
        raise ValueError("image has no visible pixels")
    y0, y1 = max(0, ys.min() - pad), min(a.shape[0], ys.max() + 1 + pad)
    x0, x1 = max(0, xs.min() - pad), min(a.shape[1], xs.max() + 1 + pad)
    return a[y0:y1, x0:x1]


def clean_alpha(a):
    """Colour under fully transparent pixels is zeroed, so edges never bleed a hidden background when scaled."""
    out = a.copy()
    out[..., :3] *= (out[..., 3:] > 0.0)
    return out


def resize(a, width, height):
    """Premultiplied Lanczos resize (no dark or coloured fringes), back to straight alpha."""
    pre = a.copy()
    pre[..., :3] *= pre[..., 3:]
    im = Image.fromarray((np.clip(pre, 0, 1) * 65535.0).astype(np.uint16).reshape(pre.shape[0], -1), "I;16") if False else None
    channels = []
    for c in range(4):
        ch = Image.fromarray((np.clip(pre[..., c], 0, 1) * 255.0 + 0.5).astype(np.uint8), "L")
        channels.append(np.asarray(ch.resize((width, height), Image.Resampling.LANCZOS)).astype(np.float32) / 255.0)
    out = np.dstack(channels)
    alpha = out[..., 3:]
    out[..., :3] = np.where(alpha > 1e-3, out[..., :3] / np.maximum(alpha, 1e-3), 0.0)
    return np.clip(out, 0, 1)


def normal_map(a, strength=2.2, bevel_px=3.0):
    """Tangent-space normals (+x right, +y up) from luminance plus an alpha bevel, encoded 0..1, alpha 1."""
    lum = (a[..., :3] * np.array([0.299, 0.587, 0.114])).sum(-1)
    alpha = a[..., 3]
    height_img = Image.fromarray((np.clip(lum * alpha, 0, 1) * 255).astype(np.uint8), "L").filter(ImageFilter.GaussianBlur(1.2))
    bevel_img = Image.fromarray((alpha * 255).astype(np.uint8), "L").filter(ImageFilter.GaussianBlur(bevel_px))
    h = np.asarray(height_img).astype(np.float32) / 255.0 * 0.6 + (np.asarray(bevel_img).astype(np.float32) / 255.0 * 0.4 if bevel_px > 0 else 0.0)
    gx = np.zeros_like(h); gy = np.zeros_like(h)
    gx[:, 1:-1] = (h[:, 2:] - h[:, :-2]) * 0.5
    gy[1:-1, :] = (h[2:, :] - h[:-2, :]) * 0.5
    nx, ny, nz = -gx * strength, gy * strength, np.ones_like(h)     # image rows go down; tangent-space +y is up
    length = np.sqrt(nx * nx + ny * ny + nz * nz)
    n = np.dstack([nx / length, ny / length, nz / length]) * 0.5 + 0.5
    return np.dstack([n, np.ones_like(h)])


def palette(a):
    """Fractions of visible pixels that are cyan (hue 160-200°, saturated) or black (value < 0.06)."""
    visible = a[..., 3] > 0.5
    rgb = a[..., :3][visible]
    if len(rgb) == 0:
        return {"cyan": 0.0, "black": 0.0}
    mx, mn = rgb.max(-1), rgb.min(-1)
    sat = np.where(mx > 0, (mx - mn) / np.maximum(mx, 1e-6), 0)
    r, g, b = rgb[:, 0], rgb[:, 1], rgb[:, 2]
    hue = np.degrees(np.arctan2(np.sqrt(3) * (g - b), 2 * r - g - b)) % 360
    cyan = ((hue > 160) & (hue < 200) & (sat > 0.35) & (mx > 0.2)).mean()
    black = (mx < 0.06).mean()
    return {"cyan": round(float(cyan), 4), "black": round(float(black), 4)}


def save(a, path):
    path.parent.mkdir(parents=True, exist_ok=True)
    to_image(a).save(path, format="PNG", optimize=False, compress_level=9)


def fit(a, width_units, height_units):
    """Scale uniformly to fit width x height units at PPU; returns the image and its pixel size."""
    h, w = a.shape[:2]
    s = min(width_units * PPU / w, height_units * PPU / h)
    width, height = max(1, round(w * s)), max(1, round(h * s))
    return resize(a, width, height)


def vent_mouth(vent):
    """The x centre of the vent's glowing slot: the columns whose top quarter is at least 60% as bright as the brightest."""
    h = vent.shape[0]
    glow = (vent[: max(1, h // 4), :, :3].max(-1) * vent[: max(1, h // 4), :, 3]).sum(0)
    xs = np.nonzero(glow >= 0.6 * glow.max())[0]
    return int(round((xs.min() + xs.max()) / 2))


def grid(a, columns, rows, column):
    """The trimmed pieces of one column of a regular sheet, top to bottom."""
    h, w = a.shape[:2]
    out = []
    for r in range(rows):
        cell = a[r * h // rows:(r + 1) * h // rows, column * w // columns:(column + 1) * w // columns]
        out.append(trim(cell))
    return out


# ---------- the kit ----------

def build():
    manifest = {"ppu": PPU, "sprites": {}, "flags": []}

    # Every sprite carries a normal map (effects a flat one), so the whole kit packs into one atlas with the same secondary
    # textures and batches together (Unity splits sprites whose secondary textures differ).
    def emit(name, a, pivot=(0.5, 0.5), normal=True, tiled=False, sources=(), placeholder=None):
        path = OUT / f"{name}.png"
        save(a, path)
        entry = {"path": str(path.relative_to(REPO)), "size_px": [a.shape[1], a.shape[0]], "pivot": list(pivot),
                 "tiled": tiled, "palette": palette(a), "sources": [str(Path(s).relative_to(REPO)) for s in sources],
                 "placeholder": placeholder or ""}
        npath = OUT / f"{name}_n.png"
        save(normal_map(a) if normal else normal_map(a, strength=0.0, bevel_px=0.0), npath)
        entry["normal"] = str(npath.relative_to(REPO))
        if entry["palette"]["cyan"] > 0.002: manifest["flags"].append(f"{name}: cyan {entry['palette']['cyan']:.1%}")
        if entry["palette"]["black"] > 0.02: manifest["flags"].append(f"{name}: black shading {entry['palette']['black']:.1%}")
        manifest["sprites"][name] = entry

    # TRAP-01, the launcher: its mouth faces right (flipped in code for a left-facing lane). 0.5 u square.
    src = CHATGPT / "TRAP-01_arrow_launcher_v1.png"
    emit("Bodies/TRAP-01_arrow_launcher", fit(clean_alpha(trim(load_rgba(src))), 0.5, 0.5), sources=[src])

    # TRAP-02, the arrow, pointing right: 0.8 u long, the kit's default arrow length (fits inside any lane's box).
    src = CHATGPT / "TRAP-02_arrow_v1.png"
    emit("Bodies/TRAP-02_arrow", fit(clean_alpha(trim(load_rgba(src))), 0.8, 0.16), sources=[src])

    # TRAP-03, the vent: cropped around its mouth to the vent's 1 x 0.3 box, 128 x 38 px.
    src = CHATGPT / "TRAP-03_geyser_vent_v1.png"
    vent = clean_alpha(trim(load_rgba(src)))
    h, w = vent.shape[:2]
    mouth = vent_mouth(vent)
    half = int(round(h * (1.0 / 0.3) / 2))
    x0 = int(np.clip(mouth - half, 0, max(0, w - 2 * half)))
    vent = vent[:, x0:x0 + 2 * half]
    emit("Bodies/TRAP-03_geyser_vent", fit(vent, 1.0, 0.3), sources=[src])

    # T04, the water column (black background -> brightness alpha): 1 u wide, a 2 u seamless tile, 8 scroll frames.
    src = AUTOSPRITE / "T04_geyser_column" / "raw_v1.png"
    col = trim(luminance_alpha(load_rgba(src)))
    # Water, not fire: graded toward the styleframe's cream-white waterfalls (self-review, 2026-09-29).
    cream = np.array([1.0, 0.97, 0.9])
    col[..., :3] = col[..., :3] * 0.35 + cream * 0.65
    col = fit(col, 1.0, 100.0)
    tile_h = 2 * PPU
    col = resize(col, col.shape[1], tile_h) if col.shape[0] < tile_h else col[:tile_h]
    blend = tile_h // 6                                   # crossfade the bottom band into the top, so it tiles vertically
    t = np.linspace(0, 1, blend)[:, None, None]
    col[:blend] = col[:blend] * t + col[-blend:] * (1 - t)
    col = col[: tile_h - blend]
    frames = 8
    for k in range(frames):
        shift = int(round(k * col.shape[0] / frames))
        emit(f"Effects/geyser_column_{k:02d}", np.roll(col, shift, axis=0), normal=False, tiled=True, sources=[src])

    # T06, the soft effects sheet: dust puffs (left column) and water splashes (right column).
    src = AUTOSPRITE / "T06_softfx_sheet" / "raw_v1.png"
    # It was asked for as a 2 x 3 grid (dust left, splashes right), so it's cut as one: a splash's droplets sit farther
    # apart than any join distance that keeps neighbouring pieces separate.
    sheet = luminance_alpha(load_rgba(src))
    left, right = grid(sheet, 2, 3, 0), grid(sheet, 2, 3, 1)
    for i, p in enumerate(left):
        emit(f"Effects/dust_{i}", fit(p, 0.5, 0.5), normal=False, sources=[src])
    for i, p in enumerate(right):
        emit(f"Effects/spray_{i}", fit(p, 0.4, 0.4), normal=False, sources=[src])

    # The glint: drawn here (a soft four-point star), not generated.
    n = 32
    yy, xx = np.mgrid[0:n, 0:n].astype(np.float32)
    cx = cy = (n - 1) / 2
    dx, dy = np.abs(xx - cx) / cx, np.abs(yy - cy) / cy
    star = np.clip(1 - np.minimum(dx * 6, 1) * 0 - (dx * dy * 12) - np.sqrt(dx * dx + dy * dy), 0, 1) ** 1.5
    glint = np.dstack([np.full((n, n), 1.0), np.full((n, n), 0.9), np.full((n, n), 0.65), star])
    emit("Effects/glint", glint, normal=False)

    # TRAP-04 onward (§12 R10): the developer's painted bodies, or code-drawn placeholders until they're saved.
    import trap_bodies
    trap_bodies.bodies(emit, load_rgba, luminance_alpha, clean_alpha, trim, fit, resize, grid, CHATGPT)

    return manifest


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--check", action="store_true", help="fail if a re-run would change any output")
    args = parser.parse_args()
    before = {p: hashlib.sha256(p.read_bytes()).hexdigest() for p in OUT.rglob("*.png")} if args.check else {}
    manifest = build()
    MANIFEST.parent.mkdir(parents=True, exist_ok=True)
    # A flat list for Unity's JsonUtility (PARALLAX/Art/Import Trap Kit), beside the readable dictionary.
    manifest["entries"] = [{"path": e["path"], "pivotX": e["pivot"][0], "pivotY": e["pivot"][1], "tiled": e["tiled"], "normal": e.get("normal", ""),
                            "placeholder": e.get("placeholder", "")}
                           for _, e in sorted(manifest["sprites"].items())]
    MANIFEST.write_text(json.dumps(manifest, indent=2, sort_keys=True) + "\n")
    for flag in manifest["flags"]:
        print("FLAG", flag)
    if args.check:
        after = {p: hashlib.sha256(p.read_bytes()).hexdigest() for p in OUT.rglob("*.png")}
        changed = [str(p.relative_to(REPO)) for p in sorted(set(before) | set(after)) if before.get(p) != after.get(p)]
        if changed:
            print("CHANGED:\n" + "\n".join(changed))
            return 1
        print(f"no diff: {len(after)} PNGs identical")
    print(f"{len(manifest['sprites'])} sprites written")
    return 0


if __name__ == "__main__":
    sys.exit(main())
