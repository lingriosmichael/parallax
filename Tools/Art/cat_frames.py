"""PAX-A08 Stage 0: the frame work for cat_register (loading clips, even loops, measuring, cleanup, vine key-out,
placement on the shared pivot, sheets). Pure functions on float RGBA arrays (0..1)."""

import io
import json
import math
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

sys.path.insert(0, str(Path(__file__).resolve().parent))
from trap_process import load_rgba, normal_map, resize  # noqa: E402  (the trap kit's method, PAX-A08 §3.4)

REPO = Path(__file__).resolve().parents[2]
CATS = REPO / "Art_Source" / "AutoSprite" / "Cats" / "A"
OUT = CATS / "_import"
CELL = 256
COLLIDER_W, COLLIDER_H = 1.0, 0.56
ALPHA_FLOOR = 0.10
BLEED_PX = 12          # two ASTC 6x6 blocks past the silhouette
LOOP_CLOSE_MIN = 0.85

# ---------- loops ----------

def pick_loop_count(length, target):
    """The frame count that divides the source loop evenly and is nearest the target (within ±50%); else the whole loop.
    Loops are never subsampled unevenly (PAX-A08 §3.2)."""
    counts = [length // s for s in range(1, length + 1) if length % s == 0]
    near = [c for c in counts if abs(c - target) <= 0.5 * target]
    return min(near, key=lambda c: (abs(c - target), -c)) if near else length


def loop_frames(start, length, target):
    count = pick_loop_count(length, target)
    stride = length // count
    return [start + i * stride for i in range(count)]


def mask(a):
    return a[..., 3] > ALPHA_FLOOR


def mask_iou(a, b):
    a, b = np.asarray(a, bool), np.asarray(b, bool)
    union = np.logical_or(a, b).sum()
    return float(np.logical_and(a, b).sum() / union) if union else 1.0


# ---------- measuring ----------

def longest_run(row):
    best, start, run = (0, -1, -1), None, 0
    for x, v in enumerate(list(row) + [False]):
        if v and start is None:
            start = x
        elif not v and start is not None:
            if x - start > best[0]:
                best = (x - start, start, x - 1)
            start = None
    return best


def measure_body(a):
    """The standing body on one frame: the paw row (lowest opaque row), the torso's horizontal run (the median longest
    run over the body band, so a thin tail or a leg doesn't count), and the back line (the median top over the torso's
    middle columns, from the rump + 15% to the chest - 35%, so the tail base and the rising neck don't count)."""
    m = mask(a)
    rows = np.nonzero(m.any(axis=1))[0]
    paw, top = int(rows.max()), int(rows.min())
    h = paw - top
    x0s, x1s = [], []
    for y in range(int(paw - 0.65 * h), int(paw - 0.35 * h) + 1):
        length, x0, x1 = longest_run(m[y])
        if length > 0:
            x0s.append(x0)
            x1s.append(x1)
    x0, x1 = int(np.median(x0s)), int(np.median(x1s))
    span = x1 - x0
    c0, c1 = int(round(x0 + 0.15 * span)), int(round(x1 - 0.35 * span))
    tops = [int(np.nonzero(m[:, x])[0].min()) for x in range(c0, c1 + 1) if m[:, x].any()]
    back = int(np.median(tops))
    return {"pawRow": paw, "backRow": back, "bodyHeightPx": paw - back, "torsoX0": x0, "torsoX1": x1,
            "torsoCX": (x0 + x1 + 1) / 2.0, "backColumns": [c0, c1]}


def body_centre(a, radius=7):
    """The centroid of the silhouette after an erosion that removes the thin tail, legs and ears."""
    m = Image.fromarray((mask(a) * 255).astype(np.uint8), "L").filter(ImageFilter.MinFilter(2 * radius + 1))
    e = np.asarray(m) > 127
    if not e.any():
        e = mask(a)
    ys, xs = np.nonzero(e)
    return float(xs.mean()) + 0.5, float(ys.mean()) + 0.5


def collider_margins(a, pivot_x, ppu, facing_right):
    """How far the painting reaches past the collider's front and back edges, in units (negative: the collider sticks
    out past the painted nose or tail tip)."""
    cols = np.nonzero(mask(a).any(axis=0))[0]
    left, right = float(cols.min()), float(cols.max() + 1)
    c_left, c_right = pivot_x - COLLIDER_W * ppu / 2.0, pivot_x + COLLIDER_W * ppu / 2.0
    ahead, behind = (right - c_right) / ppu, (c_left - left) / ppu
    return {"nose": ahead, "tail": behind} if facing_right else {"nose": behind, "tail": ahead}


# ---------- cleanup ----------

def hsv(a):
    rgb = a[..., :3]
    v = rgb.max(axis=-1)
    mn = rgb.min(axis=-1)
    d = v - mn
    s = np.where(v > 0, d / np.maximum(v, 1e-6), 0.0)
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    dd = np.maximum(d, 1e-6)
    h = np.where(v == r, ((g - b) / dd) % 6, np.where(v == g, (b - r) / dd + 2, (r - g) / dd + 4)) * 60.0
    return np.where(d > 0, h, 0.0), s, v


def cleanup(a):
    """Alpha below 10% is zeroed; a light grey or white fringe (the background-removal halo) is removed."""
    out = a.copy()
    out[out[..., 3] < ALPHA_FLOOR] = 0.0
    _, s, v = hsv(out)
    halo = (out[..., 3] < 0.6) & (v > 0.7) & (s < 0.15)
    out[halo] = 0.0
    return out


def key_vine(a):
    """Removes the baked-in vine (PAX-A08 §3.3): its olive green anywhere, grown by 3 px so the dark ink outlines that hug
    the strands go too; anything bright (the golden-brown stem, leaf highlights) in the vine's column band; then every
    fragment not connected to the cat's body. The cat is dark and warm, so it stays; its rim light where it touches the
    vine can go (the review sheet shows every keyed frame)."""
    out = a.copy()
    h, s, v = hsv(out)
    visible = out[..., 3] > ALPHA_FLOOR
    green = visible & (h >= 55) & (h <= 170) & (s > 0.25) & (v > 0.10)
    if not green.any():
        return out
    grown = np.asarray(Image.fromarray((green * 255).astype(np.uint8), "L").filter(ImageFilter.MaxFilter(7))) > 127
    cols = np.nonzero(green.sum(axis=0) > 3)[0]
    band = np.zeros(out.shape[1], bool)
    for x in cols:
        band[max(0, x - 6):x + 7] = True
    bright = visible & (v > 0.40) & band[None, :]
    out[grown | bright] = 0.0
    return keep_body(out)


def keep_body(a):
    """Only the connected region that holds the body centre (fragments left by a key-out are dropped)."""
    m = mask(a)
    if not m.any():
        return a
    cx, cy = body_centre(a)
    ys, xs = np.nonzero(m)
    seed = int(np.argmin((xs + 0.5 - cx) ** 2 + (ys + 0.5 - cy) ** 2))
    img = Image.fromarray((m * 255).astype(np.uint8), "L").copy()   # a writable copy: floodfill skips a read-only buffer
    ImageDraw.floodfill(img, (int(xs[seed]), int(ys[seed])), 128)
    keep = np.asarray(img) == 128
    out = a.copy()
    out[~keep] = 0.0
    return out


# ---------- placement ----------

def scale_frame(a, factor):
    if abs(factor - 1.0) < 1e-6:
        return a
    h, w = a.shape[:2]
    return resize(a, max(1, round(w * factor)), max(1, round(h * factor)))


def shift_into_cell(a, dx, dy, size=(CELL, CELL)):
    """`a` moved by (dx, dy) whole pixels into a canvas of `size` (w, h); returns it and the opaque pixels that fell
    outside."""
    out = np.zeros((size[1], size[0], 4), np.float32)
    h, w = a.shape[:2]
    sx0, sy0 = max(0, -dx), max(0, -dy)
    sx1, sy1 = min(w, size[0] - dx), min(h, size[1] - dy)
    kept = 0
    if sx1 > sx0 and sy1 > sy0:
        region = a[sy0:sy1, sx0:sx1]
        out[sy0 + dy:sy1 + dy, sx0 + dx:sx1 + dx] = region
        kept = int(mask(region).sum())
    return out, int(mask(a).sum()) - kept


def place(frames, role, pivot_x, paw_row, air_target=None, climb_target=None, canvas=(CELL, CELL)):
    """Registers a clip's frames on the shared pivot (PAX-A08 §3.3). Returns the placed frames and a report."""
    if role == "ground":
        paw = max(int(np.nonzero(mask(f).any(axis=1))[0].max()) for f in frames)
        centres = []
        for f in frames:
            try:
                m = measure_body(f)
                centres.append(m["torsoCX"] if m["torsoX1"] - m["torsoX0"] >= 20 else body_centre(f)[0])
            except (ValueError, IndexError):
                centres.append(body_centre(f)[0])
        shifts = [(int(round(pivot_x - float(np.median(centres)))), paw_row - paw)] * len(frames)
    elif role == "air":
        shifts = []
        for f in frames:
            cx, cy = body_centre(f)
            shifts.append((int(round(air_target[0] - cx)), int(round(air_target[1] - cy))))
    elif role == "climb":
        cs = [body_centre(f) for f in frames]
        cx, cy = float(np.median([c[0] for c in cs])), float(np.median([c[1] for c in cs]))
        shifts = [(int(round(climb_target[0] - cx)), int(round(climb_target[1] - cy)))] * len(frames)
    else:
        raise ValueError(f"unknown placement role '{role}'")
    placed, clipped = [], 0
    for f, (dx, dy) in zip(frames, shifts):
        p, lost = shift_into_cell(f, dx, dy, canvas)
        placed.append(p)
        clipped += lost
    return placed, {"shifts": [list(s) for s in shifts], "clippedPx": clipped}


# ---------- sheets ----------

def sheet_image(frames):
    """The frames left to right; every frame of a sheet has the same cell size."""
    h, w = frames[0].shape[:2]
    im = Image.new("RGBA", (w * len(frames), h), (0, 0, 0, 0))
    for i, f in enumerate(frames):
        im.paste(Image.fromarray((np.clip(f, 0, 1) * 255.0 + 0.5).astype(np.uint8), "RGBA"), (i * w, 0))
    return im


def bleed(a, px=BLEED_PX):
    """Colour bleed (ruled 2026-09-30): every pixel that is transparent in the 8-bit sheet takes the colour of its
    nearest visible neighbours, ring by ring out to `px`, so bilinear filtering and ASTC blocks at the silhouette never
    mix in black. Each ring is the weighted mean of its filled 8-neighbours: visible pixels weigh their alpha, bled
    pixels weigh 1. Alpha is never changed. (Unity's Alpha Is Transparency dilates too; this keeps the PNGs safe on
    their own, e.g. in an atlas.)"""
    out = a.copy()
    filled = out[..., 3] * 255.0 + 0.5 >= 1.0
    rgb = np.where(filled[..., None], out[..., :3], 0.0).astype(np.float32)
    weight = np.where(filled, out[..., 3], 0.0).astype(np.float32)
    h, w = filled.shape
    for _ in range(px):
        pr, pw = np.pad(rgb * weight[..., None], ((1, 1), (1, 1), (0, 0))), np.pad(weight, 1)
        acc, total = np.zeros_like(rgb), np.zeros_like(weight)
        for dy in (0, 1, 2):
            for dx in (0, 1, 2):
                if dy == 1 and dx == 1:
                    continue
                acc += pr[dy:dy + h, dx:dx + w]
                total += pw[dy:dy + h, dx:dx + w]
        ring = ~filled & (total > 0)
        if not ring.any():
            break
        rgb[ring] = acc[ring] / total[ring][:, None]
        weight[ring] = 1.0
        filled |= ring
    out[..., :3] = rgb
    return out


def png_bytes(im):
    buf = io.BytesIO()
    im.save(buf, format="PNG", optimize=False, compress_level=9)
    return buf.getvalue()


def sheet_bytes(frames):
    return png_bytes(sheet_image(frames))


def normal_frames(frames):
    out = []
    for f in frames:
        n = normal_map(f)
        n[..., 3] = 1.0
        out.append(n)
    return out


def touches_edge(a):
    """Whether the source frame's silhouette reaches its own cell border (the clip itself was cut there)."""
    m = mask(a)
    return bool(m[0].any() or m[-1].any() or m[:, 0].any() or m[:, -1].any())


def downscale(a, k):
    h, w = a.shape[:2]
    return resize(a, max(1, round(w * k)), max(1, round(h * k)))


def cell_for(frames, pivot, base_pivot, base, pad=2, block=6):
    """The sheet's cell (origin x, origin y, width, height) in a frame's canvas: the base cell around the shared pivot,
    grown on any side the clip reaches past it (plus `pad`), sized to whole ASTC blocks. Every sheet keeps the same world
    pivot, so frames line up across sheets whatever their cell (PAX-A08 §3.3, ruled 2026-09-30)."""
    m = np.zeros(frames[0].shape[:2], bool)
    for f in frames:
        m |= mask(f)
    ys, xs = np.nonzero(m)
    x0, x1, y0, y1 = int(xs.min()), int(xs.max()) + 1, int(ys.min()), int(ys.max()) + 1
    left = max(base_pivot[0], pivot[0] - x0 + pad)
    right = max(base - base_pivot[0], x1 - pivot[0] + pad)
    up = max(base_pivot[1], pivot[1] - y0 + pad)
    down = max(base - base_pivot[1], y1 - pivot[1] + pad)
    ox, oy = math.floor(pivot[0] - left + 1e-6), math.floor(pivot[1] - up + 1e-6)
    w = math.ceil(pivot[0] + right - 1e-6) - ox
    h = math.ceil(pivot[1] + down - 1e-6) - oy
    return ox, oy, block * math.ceil(w / block), block * math.ceil(h / block)


def astc_bytes(width, height, block):
    return math.ceil(width / block) * math.ceil(height / block) * 16


# ---------- the source clips ----------

def clip_frames(clip, indices):
    sidecar = json.loads((CATS / clip / "sidecar.json").read_text())
    chosen = sidecar["chosen"]
    atlas = json.loads((CATS / clip / chosen["atlas"]).read_text())["frames"]
    sheet = load_rgba(CATS / clip / chosen["sheet"])
    out = []
    for i in indices:
        f = atlas[str(i)]
        out.append(sheet[f["y"]:f["y"] + f["h"], f["x"]:f["x"] + f["w"]].copy())
    return out, chosen["sheet"]


def slot_indices(slot):
    if "loop" in slot:
        lp = slot["loop"]
        return loop_frames(lp["start"], lp["length"], lp["target"])
    return list(slot["frames"])
