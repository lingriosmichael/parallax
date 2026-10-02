"""PAX-A13 (§12 R7, R10): the trap bodies painted in ChatGPT (TRAP-04 onward) and the code-drawn effects.

Each body has a processing rule for the developer's image (cut, key, scale to 128 px per unit) and, until that image is
saved, a code-drawn PLACEHOLDER of the same size, facing and palette at the same output path, so swapping the painted one in
changes pixels only: no path, GUID, config or test moves. Everything here is deterministic (no randomness, fixed seeds).
"""

import math

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

# The styleframe's warm palette (LOOK_AND_FEEL §4): bronze and rust metal, warm browns, gold light, cream water, no cyan.
BRONZE = (0.74, 0.52, 0.26)
BRONZE_LIT = (0.98, 0.80, 0.46)
RUST = (0.55, 0.30, 0.16)
SOCKET = (0.30, 0.21, 0.15)
WOOD = (0.42, 0.28, 0.17)
WOOD_DARK = (0.30, 0.19, 0.12)
GOLD = (1.00, 0.82, 0.46)
CREAM = (1.00, 0.96, 0.88)
CLOUD_TOP = (0.96, 0.87, 0.70)
CLOUD_BELLY = (0.50, 0.44, 0.41)
SCORCH = (0.30, 0.20, 0.14)
LEAF = (0.52, 0.58, 0.22)
LEAF_LIT = (0.86, 0.80, 0.36)

SS = 4   # supersampling for the placeholders' edges


def canvas(w, h):
    return Image.new("RGBA", (w * SS, h * SS), (0, 0, 0, 0))


def rgba(c, a=1.0):
    return tuple(int(round(v * 255)) for v in c) + (int(round(a * 255)),)


def finish(img, w, h):
    return np.asarray(img.resize((w, h), Image.Resampling.LANCZOS)).astype(np.float32) / 255.0


def radial(w, h, colour, power=1.6, core=0.0):
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    dx, dy = (xx - (w - 1) / 2) / (w / 2), (yy - (h - 1) / 2) / (h / 2)
    r = np.sqrt(dx * dx + dy * dy)
    a = np.clip(1 - r, 0, 1) ** power
    if core > 0:
        a = np.maximum(a, np.clip((core - r) / core, 0, 1))
    return np.dstack([np.full((h, w), colour[0]), np.full((h, w), colour[1]), np.full((h, w), colour[2]), a])


# ---------- placeholders (code-drawn, marked PLACEHOLDER) ----------

def ph_spike_strip(w=128, h=45):
    img = canvas(w, h); d = ImageDraw.Draw(img); W, H = w * SS, h * SS
    socket = int(H * 0.22)
    d.rectangle([0, H - socket, W, H], fill=rgba(SOCKET))
    n = 4
    for i in range(n):
        x0, x1 = i * W / n, (i + 1) * W / n
        tip = ((x0 + x1) / 2, H * 0.04)
        d.polygon([(x0 + 2, H - socket), tip, (x1 - 2, H - socket)], fill=rgba(BRONZE))
        d.polygon([(x0 + 2, H - socket), tip, ((x0 + x1) / 2, H - socket)], fill=rgba(BRONZE_LIT))   # the backlit side
        d.line([tip, ((x0 + x1) / 2 + W / n * 0.18, H * 0.45)], fill=rgba(RUST), width=SS)
    return finish(img, w, h)


def ph_spear_head(w=96, h=44):
    img = canvas(w, h); d = ImageDraw.Draw(img); W, H = w * SS, h * SS
    d.rectangle([0, H * 0.36, W * 0.30, H * 0.64], fill=rgba(BRONZE))                     # the socket
    d.polygon([(W * 0.26, H * 0.06), (W, H * 0.5), (W * 0.26, H * 0.94), (W * 0.36, H * 0.5)], fill=rgba(BRONZE))
    d.polygon([(W * 0.26, H * 0.06), (W, H * 0.5), (W * 0.36, H * 0.5)], fill=rgba(BRONZE_LIT))
    return finish(img, w, h)


def ph_spear_shaft(w=128, h=44):
    img = canvas(w, h); d = ImageDraw.Draw(img); W, H = w * SS, h * SS
    d.rectangle([0, H * 0.22, W, H * 0.78], fill=rgba(WOOD))
    d.rectangle([0, H * 0.22, W, H * 0.34], fill=rgba((0.62, 0.44, 0.26)))                 # rim light along the top
    for k in range(4):
        y = H * (0.42 + 0.08 * k)
        d.line([(0, y), (W, y + (4 if k % 2 else -4))], fill=rgba(WOOD_DARK), width=SS)
    d.rectangle([W * 0.46, H * 0.18, W * 0.56, H * 0.82], fill=rgba(RUST))                # a leather wrap
    return finish(img, w, h)


def ph_orb(w=77, h=77):
    a = radial(w, h, GOLD, power=1.2, core=0.45)
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    ang = np.arctan2(yy - h / 2, xx - w / 2); r = np.hypot(xx - w / 2, yy - h / 2) / (w / 2)
    swirl = (np.abs(np.sin(ang * 2 + r * 6)) < 0.25) & (r < 0.5)
    a[..., :3][swirl] = CREAM
    return a


def ph_cue_ring(w=192, h=141):
    img = canvas(w, h); d = ImageDraw.Draw(img); W, H = w * SS, h * SS
    d.ellipse([W * 0.05, H * 0.08, W * 0.95, H * 0.92], outline=rgba(GOLD, 0.9), width=int(SS * 3))
    return np.asarray(img.filter(ImageFilter.GaussianBlur(SS * 1.5)).resize((w, h), Image.Resampling.LANCZOS)).astype(np.float32) / 255.0


def ph_cue_mark(w=90, h=26):
    img = canvas(w, h); d = ImageDraw.Draw(img); W, H = w * SS, h * SS
    t = int(SS * 2.5)
    d.line([(W * 0.40, H * 0.2), (W * 0.12, H * 0.5), (W * 0.40, H * 0.8)], fill=rgba(GOLD), width=t)     # <
    d.line([(W * 0.60, H * 0.2), (W * 0.88, H * 0.5), (W * 0.60, H * 0.8)], fill=rgba(GOLD), width=t)     # >
    d.line([(W * 0.12, H * 0.5), (W * 0.88, H * 0.5)], fill=rgba(GOLD, 0.7), width=int(SS * 1.5))
    return finish(img, w, h)


def ph_glyph_ring(w=128, h=128):
    img = canvas(w, h); d = ImageDraw.Draw(img); W, H = w * SS, h * SS
    d.ellipse([W * 0.12, H * 0.12, W * 0.88, H * 0.88], outline=rgba(GOLD, 0.95), width=int(SS * 2.5))
    for k in range(12):
        a = k * math.pi / 6
        r0, r1 = W * 0.30, W * 0.36
        d.line([(W / 2 + r0 * math.cos(a), H / 2 + r0 * math.sin(a)), (W / 2 + r1 * math.cos(a), H / 2 + r1 * math.sin(a))], fill=rgba(GOLD, 0.8), width=int(SS * 1.5))
    for sgn in (-1, 1):   # the up-and-down arrows at top and bottom
        y = H / 2 + sgn * H * 0.44
        d.polygon([(W / 2 - W * 0.06, y), (W / 2 + W * 0.06, y), (W / 2, y + sgn * H * 0.07)], fill=rgba(GOLD))
    return np.asarray(img.filter(ImageFilter.GaussianBlur(SS * 0.6)).resize((w, h), Image.Resampling.LANCZOS)).astype(np.float32) / 255.0


def ph_cloud(w=256, h=102):
    img = canvas(w, h); d = ImageDraw.Draw(img); W, H = w * SS, h * SS
    puffs = [(0.18, 0.62, 0.20), (0.34, 0.45, 0.26), (0.52, 0.40, 0.30), (0.70, 0.48, 0.25), (0.84, 0.62, 0.18), (0.50, 0.68, 0.30)]
    for cx, cy, r in puffs:
        d.ellipse([W * (cx - r * 0.55), H * (cy - r), W * (cx + r * 0.55), H * (cy + r)], fill=rgba(CLOUD_TOP))
    a = np.asarray(img.filter(ImageFilter.GaussianBlur(SS * 2))).astype(np.float32) / 255.0
    yy = np.linspace(0, 1, a.shape[0])[:, None]
    belly = np.clip((yy - 0.45) / 0.45, 0, 1)                  # darker, heavier underneath (§11 R2)
    for c in range(3):
        a[..., c] = CLOUD_TOP[c] * (1 - belly) + CLOUD_BELLY[c] * belly
    return np.asarray(Image.fromarray((a * 255).astype(np.uint8), "RGBA").resize((w, h), Image.Resampling.LANCZOS)).astype(np.float32) / 255.0


def ph_scorch(w=154, h=45):
    img = canvas(w, h); d = ImageDraw.Draw(img); W, H = w * SS, h * SS
    for k in range(9):
        a = (k - 4) * 0.16
        d.line([(W / 2, H * 0.85), (W / 2 + math.sin(a) * W * 0.48, H * 0.85 - math.cos(a) * H * 0.55)], fill=rgba(SCORCH, 0.85), width=int(SS * 3))
    for k, (x, y) in enumerate([(0.36, 0.55), (0.58, 0.5), (0.47, 0.7), (0.66, 0.72)]):
        d.ellipse([W * x - SS * 2, H * y - SS * 2, W * x + SS * 2, H * y + SS * 2], fill=rgba((1.0, 0.62, 0.25)))
    return np.asarray(img.filter(ImageFilter.GaussianBlur(SS)).resize((w, h), Image.Resampling.LANCZOS)).astype(np.float32) / 255.0


def ph_crack(w=128, h=128):
    img = canvas(w, h); d = ImageDraw.Draw(img); W, H = w * SS, h * SS
    rays = [(-2.6, 0.46), (-1.4, 0.40), (-0.3, 0.47), (0.9, 0.42), (2.1, 0.45), (3.0, 0.30)]
    for a0, length in rays:
        pts = [(W / 2, H / 2)]
        for j in range(1, 5):
            jitter = 0.18 * (1 if j % 2 else -1)
            r = length * W * j / 4
            pts.append((W / 2 + r * math.cos(a0 + jitter), H / 2 + r * math.sin(a0 + jitter)))
        d.line(pts, fill=rgba(SCORCH, 0.9), width=int(SS * 1.6))
    return finish(img, w, h)


def ph_steam(i, w=64, h=128):
    a = np.zeros((h, w, 4), np.float32)
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    for k in range(10):
        t = k / 9
        cy = h * (0.95 - 0.8 * t)
        cx = w / 2 + math.sin(t * 5 + i * 1.7) * w * 0.18
        r = w * (0.14 + 0.18 * t)
        blob = np.exp(-(((xx - cx) ** 2 + (yy - cy) ** 2) / (2 * r * r))) * (1 - 0.7 * t)
        a[..., 3] = np.maximum(a[..., 3], blob)
    a[..., :3] = CREAM
    return a


def ph_leaf(i, w=24, h=24):
    img = canvas(w, h); d = ImageDraw.Draw(img); W, H = w * SS, h * SS
    angle = i * 0.9
    pts = []
    for k in range(24):
        t = k / 23 * 2 * math.pi
        x, y = math.cos(t) * 0.42, math.sin(t) * 0.20 * (1 + 0.4 * math.cos(t))
        pts.append((W / 2 + (x * math.cos(angle) - y * math.sin(angle)) * W, H / 2 + (x * math.sin(angle) + y * math.cos(angle)) * H))
    d.polygon(pts, fill=rgba(LEAF if i % 2 else LEAF_LIT))
    return finish(img, w, h)


# ---------- code effects (always code) ----------

def fx_bolt_segment(w=16, h=64):
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    d = np.abs(xx - (w - 1) / 2) / (w / 2)
    a = np.clip(1 - d, 0, 1) ** 2.2
    core = np.clip(1 - d * 3, 0, 1)
    rgb = np.dstack([np.full((h, w), 1.0), 0.92 + 0.08 * core, 0.70 + 0.30 * core])
    return np.dstack([rgb, a])


def fx_flash(w=128, h=128):
    return radial(w, h, CREAM, power=2.0)


def fx_ring(w=128, h=128):
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    r = np.hypot(xx - (w - 1) / 2, yy - (h - 1) / 2) / (w / 2)
    a = np.exp(-((r - 0.8) ** 2) / (2 * 0.06 ** 2))
    return np.dstack([np.full((h, w), GOLD[0]), np.full((h, w), GOLD[1]), np.full((h, w), GOLD[2]), a])


def fx_mote(w=16, h=16):
    return radial(w, h, GOLD, power=2.0)


# ---------- the painted versions ----------

def bodies(emit, load_rgba, luminance_alpha, clean_alpha, trim, fit, resize, grid, chatgpt):
    """Emits every body: the developer's painted image when it's saved, a placeholder otherwise."""

    def source(name):
        p = chatgpt / f"{name}_v1.png"
        return p if p.exists() else None

    def pieces(a, count, threshold=0.03):
        """The `count` widest pieces of a row of separate pieces, left to right, split where a whole column is empty (an
        equal-width cut would slice a piece that runs past its third, as the spear head's collar does)."""
        filled = (a[..., 3] > threshold).any(axis=0)
        runs, start = [], None
        for x, f in enumerate(list(filled) + [False]):
            if f and start is None: start = x
            elif not f and start is not None: runs.append((start, x)); start = None
        runs = sorted(sorted(runs, key=lambda r: r[1] - r[0], reverse=True)[:count])
        if len(runs) < count: raise ValueError(f"expected {count} separate pieces, found {len(runs)}")
        return [trim(a[:, x0:x1]) for x0, x1 in runs]

    def whole_periods(a, threshold=0.5):
        """A spike strip cropped to whole spike periods (tip spacing measured from the art), cut halfway between tips, so
        it repeats without a doubled or missing spike at the seam."""
        opaque = a[..., 3] > threshold
        h, w = opaque.shape
        top = np.where(opaque.any(axis=0), opaque.argmax(axis=0), h)
        tips = [x for x in range(2, w - 2) if top[x] < h * 0.35 and top[x] <= top[x - 2:x + 3].min()]
        merged = []
        for x in tips:
            if merged and x - merged[-1][-1] <= 3: merged[-1].append(x)
            else: merged.append([x])
        centres = [sum(g) / len(g) for g in merged]
        if len(centres) < 3: raise ValueError("couldn't find the spike tips")
        period = float(np.median(np.diff(centres)))
        x0 = centres[0] + period / 2
        n = int((centres[-1] - x0) // period)
        return a[:, int(round(x0)):int(round(x0 + n * period))]

    def straight_run(a, fraction=0.97, inset=2, threshold=0.5):
        """A shaft cropped to its full-thickness middle (rounded or capped ends removed), so it repeats as one pole."""
        height = (a[..., 3] > threshold).sum(axis=0)
        typical = np.median(height[height > 0])   # the wood's thickness; a wrap band may stand proud of it
        full = np.nonzero(height >= fraction * typical)[0]
        return a[:, full[0] + inset:full[-1] + 1 - inset]

    def tile_height(a, height_px):
        h, w = a.shape[:2]
        return resize(a, max(1, round(w * height_px / h)), height_px)

    def body(key, name, rule, placeholder, tiled=False, normal=True):
        src = source(key)
        if src is not None:
            out = rule(src)
            if isinstance(out, list):
                for suffix, img in out: emit(f"{name}{suffix}", img, tiled=tiled, normal=normal, sources=[src])
            else:
                emit(name, out, tiled=tiled, normal=normal, sources=[src])
        else:
            out = placeholder()
            if isinstance(out, list):
                for suffix, img in out: emit(f"{name}{suffix}", img, tiled=tiled, normal=normal, placeholder=key)
            else:
                emit(name, out, tiled=tiled, normal=normal, placeholder=key)

    def blades(a):
        """PAX-A16 gauntlet (the developer: "spikes need to be longer and clearer"; then "spikes are grey, make them black, it's
        more noticeable"): the strip's blades only, no stone base (it blended into the floor), near-black iron (value 0.07-0.15,
        just above the black-shading flag) with a pale lit edge on the right and top and a faint grey rim, so they read as
        black on a bright sky and still show their shape on dark stone. The base's collar row is kept as a 3 px dark seat."""
        opaque = a[..., 3] > 0.5
        cover = opaque.mean(axis=1)
        base = int(np.argmax(cover > 0.85))          # the first row where the stone base spans the strip
        out = a[:base + 3].copy()
        # Solid blades: the painted source has semi-clear pits inside each blade; close them (a dilate then an erode of the
        # silhouette) so the rim only runs around the outside.
        sil = Image.fromarray(((out[..., 3] > 0.35) * 255).astype(np.uint8), "L").filter(ImageFilter.MaxFilter(5)).filter(ImageFilter.MinFilter(5))
        out[..., 3] = np.maximum(out[..., 3], np.asarray(sil).astype(np.float32) / 255)
        rgb, alpha = out[..., :3], out[..., 3]
        lum = (rgb * np.array([0.3, 0.55, 0.15], np.float32)).sum(-1, keepdims=True)
        # Black iron: the painted shading kept as a faint value change inside the near-black.
        l = (lum - lum.min()) / max(1e-4, float(lum.max() - lum.min()))
        l = np.asarray(Image.fromarray((l[..., 0] * 255).astype(np.uint8), "L").filter(ImageFilter.GaussianBlur(1.6))).astype(np.float32)[..., None] / 255
        out[..., :3] = np.clip((0.07 + 0.08 * l) * np.array([1.0, 0.97, 0.95], np.float32), 0, 1)
        # Round 3 (critic: "flat black triangles"): a two-facet bevel, the blade's right half (towards the light) a step
        # lighter than its left, split along each row's run of blade pixels, still near-black (value ≤ 0.29).
        facet = np.zeros(alpha.shape, np.float32)
        for y in range(alpha.shape[0]):
            xs = np.flatnonzero(alpha[y] > 0.5)
            if xs.size == 0: continue
            for run in np.split(xs, np.flatnonzero(np.diff(xs) > 1) + 1):
                if run.size < 3: continue
                t = (run - run[0]) / max(1, run.size - 1)
                facet[y, run] = np.clip((t - 0.5) * 6, 0, 1)
        out[..., :3] = out[..., :3] + facet[..., None] * np.array([0.13, 0.13, 0.14], np.float32)
        # The lit edge: where alpha falls off to the right of or above a blade pixel (light from the right and above).
        right = np.zeros_like(alpha); right[:, :-2] = alpha[:, 2:]
        above = np.zeros_like(alpha); above[2:] = alpha[:-2]
        edge = np.clip(alpha - np.minimum(right, above), 0, 1)[..., None]
        out[..., :3] = out[..., :3] * (1 - edge * 0.7) + np.array([0.62, 0.6, 0.58], np.float32) * edge * 0.7
        # A faint grey rim (1-2 px) around the silhouette, so a black blade keeps its outline on dark stone.
        im = Image.fromarray((alpha * 255).astype(np.uint8), "L").filter(ImageFilter.MaxFilter(3))
        ring = np.clip(np.asarray(im).astype(np.float32) / 255 - alpha, 0, 1)
        out[..., :3] = np.where(ring[..., None] > 0, np.array([0.26, 0.25, 0.25], np.float32), out[..., :3])
        out[..., 3] = np.maximum(alpha, ring * 0.7)
        out[-3:, :, :3] = np.array([0.08, 0.07, 0.07], np.float32); out[-3:, :, 3] = 1.0
        return out

    body("TRAP-04_spike_strip", "Bodies/TRAP-04_spike_strip", lambda s: tile_height(blades(whole_periods(clean_alpha(trim(load_rgba(s))))), 45), ph_spike_strip, tiled=True)
    body("TRAP-05_spear", "Bodies/TRAP-05_spear_head", lambda s: fit(clean_alpha(np.ascontiguousarray(pieces(clean_alpha(load_rgba(s)), 3)[0][:, ::-1])), 0.75, 0.34), ph_spear_head)   # the v1 image points left; the kit faces right
    body("TRAP-05_spear", "Bodies/TRAP-05_spear_shaft", lambda s: tile_height(straight_run(clean_alpha(pieces(clean_alpha(load_rgba(s)), 3)[1])), 44), ph_spear_shaft, tiled=True)
    body("TRAP-06_inverter_orb", "Bodies/TRAP-06_inverter_orb", lambda s: fit(trim(luminance_alpha(load_rgba(s))), 0.6, 0.6), ph_orb)
    body("TRAP-07_inverter_cue", "Bodies/TRAP-07_cue_ring", lambda s: fit(trim(grid(luminance_alpha(load_rgba(s)), 1, 2, 0)[0]), 1.5, 1.1), ph_cue_ring, normal=False)
    body("TRAP-07_inverter_cue", "Bodies/TRAP-07_cue_mark", lambda s: fit(trim(grid(luminance_alpha(load_rgba(s)), 1, 2, 0)[1]), 0.7, 0.2), ph_cue_mark, normal=False)
    body("TRAP-08_glyph_ring", "Bodies/TRAP-08_glyph_ring", lambda s: fit(trim(luminance_alpha(load_rgba(s))), 1.0, 1.0), ph_glyph_ring, normal=False)
    body("TRAP-09_storm_cloud", "Bodies/TRAP-09_storm_cloud", lambda s: fit(clean_alpha(trim(load_rgba(s))), 2.0, 0.8), ph_cloud)
    body("TRAP-10_scorch", "Effects/TRAP-10_scorch", lambda s: fit(clean_alpha(trim(load_rgba(s))), 1.2, 0.35), ph_scorch, normal=False)
    body("TRAP-11_block_crack", "Effects/TRAP-11_block_crack", lambda s: fit(clean_alpha(trim(load_rgba(s))), 1.0, 1.0), ph_crack, normal=False)
    body("TRAP-12_geyser_steam", "Effects/TRAP-12_steam",
         lambda s: [(f"_{i}", fit(trim(grid(luminance_alpha(load_rgba(s)), 3, 1, i)[0]), 0.5, 1.0)) for i in range(3)],
         lambda: [(f"_{i}", ph_steam(i)) for i in range(3)], normal=False)
    body("TRAP-13_leaves", "Effects/TRAP-13_leaf",
         lambda s: [(f"_{c * 2 + r}", fit(clean_alpha(trim(grid(clean_alpha(load_rgba(s)), 3, 2, c)[r])), 0.19, 0.19)) for c in range(3) for r in range(2)],
         lambda: [(f"_{i}", ph_leaf(i)) for i in range(6)], normal=False)

    # Always code.
    emit("Effects/bolt_segment", fx_bolt_segment(), normal=False)
    emit("Effects/flash", fx_flash(), normal=False)
    emit("Effects/ring", fx_ring(), normal=False)
    emit("Effects/mote", fx_mote(), normal=False)
