#!/usr/bin/env python3
"""D-162 (PAX-A19): the geyser's thermal-vent steam (the developer's pick "2D", 2026-10-22), rendered as one sprite per tick.

  python3 Tools/Art/geyser_steam.py            # (re)writes Assets/_Game/Art/Geyser/ and its manifest
  python3 Tools/Art/geyser_steam.py --check    # fails if a re-run would change any output byte

Steam is a density field built from particles an emitter puts out each tick (every particle's position is a formula of
its age and a seeded random draw, so the same tick always gives the same frame), broken up by advected, domain-warped
noise, lit by the sun from the upper left with self-shadowing (optical depth marched toward the light, plus a multiple-
scattering term so the inside stays luminous) and drawn with alpha = 1 - exp(-density). The vent glows orange through
the tell and the eruption and warms the steam's underside.

Per direction (Up, and Down for a vent in a ceiling, whose jet fires down while its steam still rises):
  tell_NN   the tell's hissing wisps and the glow (an effect, the tell group), NN = ticks since the fire
  core_NN   the jet inside the launch column (the column's body, exactly 1.1 x 1.5 u, never outside it)
  plume_NN  the rest of the eruption (an effect, the burst group), NN = ticks into the eruption
  fade_NN   the steam dying away after the eruption (an effect, the fade group), NN = ticks since it ended
core_NN and plume_NN split one density field, D = D*m + D*(1 - m), so drawn together their coverage is exactly the
whole frame's (the transmittances multiply) and there is no seam. Every sprite's pivot is the vent's mouth.
"""

import argparse
import hashlib
import json
import math
import sys
import zlib
from multiprocessing import Pool
from pathlib import Path

import numpy as np
from PIL import Image
from scipy.ndimage import gaussian_filter

REPO = Path(__file__).resolve().parents[2]
OUT = REPO / "Assets" / "_Game" / "Art" / "Geyser"
MANIFEST = OUT / "geyser_steam.json"

PPU = 80                         # the sprites' pixels per unit (soft vapour; the play layer's 128 would cost 2.6x the atlas)
SS = 3                           # rendered at 3x, then downsampled
P = PPU * SS
TELL, ERUPT, FADE = 25, 40, 22   # GeyserMath's default tell and erupt; TrapArtMath.GeyserFadeTicks
COL_W, COL_H = 1.1, 1.5          # the column art's box (the grey-box 1 x 1.5, drawn BodyScale 1.1 wide)
HALF_W = 2.2
UP_EXT, DOWN_EXT = 3.4, 0.8      # along the jet / behind the mouth
LIGHT = (-0.6, 0.8)
SUN = (1.0, 0.95, 0.84)
SHADOW = (0.52, 0.55, 0.66)
AMBIENT = (0.74, 0.75, 0.8)      # neutral: the level's 2D lights tint the sprites in the game
GLOW = (1.0, 0.45, 0.15)
GLOW_AIR = (1.0, 0.62, 0.3)
ALPHA_MIN = 2 / 255              # trimmed away

# The look ("2D thermal vent"), as sent in the round-2 mock-ups.
LOOK = dict(erode=0.35, contrast=1.0, scale=0.9, octaves=3, warp=0.25, relief=0.08, blur=0.03, dens=3.0, core=0.8,
            sigma_a=2.3, sigma_s=1.9, glow=0.4, rim=0.35, wisp=0.8)


def R(*k):
    return np.random.default_rng(zlib.crc32(repr(k).encode()))


def smooth(e0, e1, v):
    t = np.clip((v - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


class Grid:
    """World units relative to the mouth, y up; d = +1 for an Up geyser, -1 for Down (its layer reaches toward -y)."""

    def __init__(self, d):
        self.d = d
        self.y_top, y_bot = (UP_EXT, -DOWN_EXT) if d > 0 else (DOWN_EXT, -UP_EXT)
        h, w = int(round((self.y_top - y_bot) * P)), int(round(2 * HALF_W * P))
        ys, xs = np.mgrid[0:h, 0:w].astype(np.float32)
        self.x = (xs + 0.5) / P - HALF_W
        self.y = self.y_top - (ys + 0.5) / P
        self.shape = (h, w)

    def px(self, x, y):
        return (x + HALF_W) * P, (self.y_top - y) * P


_grids = {}


def grid(d):
    if d not in _grids: _grids[d] = Grid(d)
    return _grids[d]


def noise2(seed, sx, sy, shape=(96, 96)):
    g = np.random.default_rng(seed).random(shape).astype(np.float32)

    def f(x, y):
        x = x * sx; y = y * sy
        x0 = np.floor(x).astype(np.int32); y0 = np.floor(y).astype(np.int32)
        fx = x - x0; fy = y - y0
        fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy)
        x0 %= shape[1]; y0 %= shape[0]; x1 = (x0 + 1) % shape[1]; y1 = (y0 + 1) % shape[0]
        return (g[y0, x0] * (1 - fx) + g[y0, x1] * fx) * (1 - fy) + (g[y1, x0] * (1 - fx) + g[y1, x1] * fx) * fy
    return f


def fbm(seed, sx, sy, octaves=4, gain=0.5):
    ns = [noise2(seed + i * 7, sx * 2 ** i, sy * 2 ** i) for i in range(octaves)]

    def f(x, y):
        v = 0; amp = 1.0; tot = 0
        for n in ns:
            v = v + amp * n(x, y); tot += amp; amp *= gain
        return v / tot
    return f


# ---------- density ----------

def splat(G, parts):
    """Sum of soft gaussian puffs (x, y, r, w), each evaluated only inside its own box."""
    D = np.zeros(G.shape, np.float32)
    for x, y, r, w in parts:
        if r <= 0 or w <= 0: continue
        cx, cy = G.px(x, y); rp = r * P * 2.6
        x0, x1 = max(0, int(cx - rp)), min(G.shape[1], int(cx + rp) + 1)
        y0, y1 = max(0, int(cy - rp)), min(G.shape[0], int(cy + rp) + 1)
        if x0 >= x1 or y0 >= y1: continue
        gx, gy = G.x[y0:y1, x0:x1], G.y[y0:y1, x0:x1]
        D[y0:y1, x0:x1] += w * np.exp(-((gx - x) ** 2 + (gy - y) ** 2) / (r * r))
    return D


def shift(a, dx, dy):
    """a sampled at (col + dx, row + dy), zero outside."""
    out = np.zeros_like(a)
    h, w = a.shape
    sx0, sx1 = max(0, dx), min(w, w + dx); sy0, sy1 = max(0, dy), min(h, h + dy)
    out[sy0 - dy:sy1 - dy, sx0 - dx:sx1 - dx] = a[sy0:sy1, sx0:sx1]
    return out


def optical_depth(D, light, steps=12, step_u=0.045):
    lx, ly = light; n = math.hypot(lx, ly); lx, ly = lx / n, ly / n
    tau = np.zeros_like(D)
    for k in range(1, steps + 1):
        tau += shift(D, int(round(lx * k * step_u * P)), int(round(-ly * k * step_u * P)))
    return tau * step_u


def jet(s, d, rate=3, v=0.27, T=5.5, mush=0.3, life=12, rmax=0.12, rise=0.003, wob=0.06, dens=0.5, ceil=1.55, sizevar=0.25):
    """The eruption: particles leave the mouth along d, slow, spread with height, roll sideways at the top, rise on
    buoyancy (always up) and thin out. s = ticks since the eruption started (past ERUPT the emitter has stopped)."""
    parts = []
    for e in range(0, min(s, ERUPT - 1) + 1):
        for j in range(rate):
            a = s - e - j / rate
            if a < 0: continue
            r = R("jet", e, j)
            first = e < 3
            V = (0.6 if first else v) * r.uniform(0.85, 1.15)
            TT = 3.2 if first else T
            h = V * TT * (1 - math.exp(-a / TT))
            hn = h / COL_H
            side = r.uniform(-1, 1)
            side = math.copysign(abs(side) ** 0.7, side)
            slow = smooth(0.7, 2.2, a / TT)
            x = side * (0.06 + 0.24 * min(hn, 1.2)) + side * mush * slow * r.uniform(0.6, 1.1)
            x += wob * math.sin(a * 0.33 + r.uniform(0, 6.3)) * min(hn, 1)
            y = d * h + rise * a
            rad = (0.06 + rmax * min(hn, 1.25) + 0.0015 * a) * r.uniform(1 - sizevar, 1 + sizevar)
            w = r.uniform(0.7, 1.2) * (0.075 / rad) ** 1.6 * math.exp(-max(0.0, a - 8) / life) * dens
            w *= math.exp(-max(0.0, h - ceil) / 0.35)
            parts.append((x, y, rad, w))
    return parts


def skirt(s, d, n=2, reach=0.7, life=14):
    """Steam spilling along the surface round the vent (the floor for Up, the ceiling for Down)."""
    parts = []
    for e in range(0, min(s, ERUPT - 1) + 1, 2):
        for j in range(n):
            a = s - e - j
            if a < 0: continue
            r = R("skirt", e, j)
            side = 1 if (e // 2 + j) % 2 else -1
            x = side * (0.22 + reach * r.uniform(0.6, 1.0) * (1 - math.exp(-a / 9)))
            y = d * (0.04 + 0.03 * min(a / 15, 1)) + 0.004 * a
            parts.append((x, y, 0.06 + 0.004 * a, 0.35 * math.exp(-a / life)))
    return parts


def wisps(s, d, strength):
    """The tell: thin threads hissing from the mouth, more each pulse, with spits on ticks 8, 15, 20, 23."""
    parts = []
    for e in range(0, s + 1):
        for j in range(2):
            a = s - e - j * 0.5
            if a < 0: continue
            r = R("wisp", e, j)
            x0 = r.uniform(-0.28, 0.28)
            h = (0.05 + 0.04 * e / TELL) * 14 * (1 - math.exp(-a / 14))
            x = x0 + 0.05 * math.sin(a * 0.4 + r.uniform(0, 6)) * h * 3
            parts.append((x, d * h + 0.004 * a, 0.035 + 0.06 * h, strength * (0.35 + 0.5 * e / TELL) * math.exp(-a / 10)))
    for k, at in enumerate((8, 15, 20, 23)):
        a = s - at
        if a < 0: continue
        for j in range(5):
            r = R("spit", k, j)
            h = r.uniform(0.18, 0.32) * (0.7 + 0.5 * k / 3) * 3 * (1 - math.exp(-a / 3))
            parts.append((r.uniform(-0.2, 0.2) + r.uniform(-0.25, 0.25) * h, d * h + 0.005 * a, 0.06 + 0.05 * h + 0.004 * a,
                          strength * 0.7 * math.exp(-a / 6)))
    return parts


def core(G, s, d, after):
    """The column's body: a continuous dense jet filling the launch column, so its reach reads at a glance."""
    if after:
        k = max(0.0, 1 - s / 5); s = ERUPT
    else:
        k = 1.0 if s < ERUPT - 5 else (ERUPT - s) / 5
    grow = min(1.0, (s + 1) / 4)
    yy = G.y * d
    hw = 0.1 + 0.2 * np.clip(yy / COL_H, 0, 1) ** 1.1
    return k * np.exp(-(G.x / hw) ** 2) * smooth(-0.02, 0.06, yy) * (1 - smooth(COL_H * grow * 0.75, COL_H * grow * 1.08, yy)) * 1.4


def erode(G, D, s):
    """Billows: soft noise advected upward with the steam and domain-warped; the thin outside erodes into wisps."""
    f = fbm(5, 3.2 * LOOK["scale"], 3.2 * LOOK["scale"], octaves=LOOK["octaves"], gain=0.55)
    q = fbm(55, 1.4 * LOOK["scale"], 1.4 * LOOK["scale"], octaves=2)
    x, y = G.x, G.y - s * 0.025
    wx = x + LOOK["warp"] * (q(x, y) - 0.5) * 2
    wy = y + LOOK["warp"] * (q(x + 9.1, y + 3.3) - 0.5) * 2
    b = smooth(0.2, 0.8, f(wx, wy))
    c = LOOK["contrast"]
    Dn = D * (1 - c / 2 + c * b)
    edge = 1 - smooth(0.0, 1.4, Dn)
    return np.clip(Dn - LOOK["erode"] * (1 - b) * edge, 0, None)


def vent_glow(G, phase, s, d):
    if phase == "tell": k = 0.35 + 0.65 * (s / TELL) ** 1.5 * (0.85 + 0.15 * math.sin(s * 1.7))
    elif phase == "erupt": k = 1.0 if s < ERUPT - 6 else (ERUPT - s) / 6
    else: k = max(0.0, 0.3 - s / 30)
    dist = np.hypot(G.x / 0.55, (G.y * d) / 0.75)
    return k * np.exp(-dist * dist * 1.6) * (G.y * d > -0.05)


def shade(D):
    """Colour and coverage: sunlit tops, ambient-tinted shadow, a bright fringe on thin sunlit edges."""
    Db = gaussian_filter(D, LOOK["blur"] * P)
    tau = optical_depth(gaussian_filter(D, 0.06 * P), LIGHT)
    ss = LOOK["sigma_s"]
    lit = 0.6 * np.exp(-ss * tau) + 0.4 * np.exp(-0.3 * ss * tau)
    dr, dc = np.gradient(Db, 1 / P)
    nx, ny = -dc * LOOK["relief"], dr * LOOK["relief"]
    lx, ly = LIGHT; ln = math.hypot(lx, ly); lx, ly = lx / ln, ly / ln
    ndl = np.clip((nx * lx + ny * ly + 0.55) / np.sqrt(nx * nx + ny * ny + 0.3025), 0, 1)
    lit = lit * (0.55 + 0.45 * ndl)
    k = lit[..., None]
    shadow = np.asarray(SHADOW) * 0.7 + np.asarray(AMBIENT) * 0.3
    rgb = shadow * (1 - k) + np.asarray(SUN) * k
    rgb = rgb * 0.88 + np.asarray(AMBIENT) * 0.12
    alpha = 1 - np.exp(-LOOK["sigma_a"] * D)
    fr = (smooth(0.03, 0.3, alpha) * (1 - smooth(0.4, 0.85, alpha)) * np.clip(lit * 1.3, 0, 1))[..., None]
    rgb = rgb * (1 - fr * LOOK["rim"]) + np.asarray((1.0, 0.98, 0.9)) * fr * LOOK["rim"]
    return rgb, alpha


def column_mask(G, d):
    """1 inside the launch column, easing to 0 before its edges (the body never reaches past its box)."""
    yy = G.y * d
    mx = 1 - smooth(COL_W / 2 - 0.12, COL_W / 2 - 1.0 / PPU, np.abs(G.x))
    my = smooth(0.0, 0.04, yy) * (1 - smooth(COL_H - 0.16, COL_H - 1.0 / PPU, yy))
    return (mx * my).astype(np.float32)


# ---------- frames ----------

def frame(d, phase, s):
    """{name: rgba at SS x} for one tick: tell -> {tell}, erupt -> {core, plume}, fade -> {fade}."""
    G = grid(d)
    if phase == "tell":
        parts = wisps(s, d, LOOK["wisp"])
    else:
        se = s if phase == "erupt" else ERUPT + s
        parts = jet(se, d) + skirt(se, d)
    D = splat(G, parts) * LOOK["dens"]
    if phase == "fade": D *= max(0.0, 1 - s / FADE) ** 1.2
    if phase != "tell": D += core(G, s, d, phase == "fade") * LOOK["core"]
    D = erode(G, D, s if phase != "fade" else ERUPT + s)
    rgb, alpha = shade(D)
    glow = vent_glow(G, phase, s, d)
    rgb = np.clip(rgb + np.asarray(GLOW) * glow[..., None] * (0.15 + 0.85 * alpha[..., None]) * LOOK["glow"], 0, 1)
    air = glow * smooth(0.0, 0.25, glow) * 0.5 * LOOK["glow"]   # the glow in clear air, right at the mouth
    if phase != "erupt":
        a = 1 - (1 - alpha) * (1 - air)
        rgb = np.where(alpha[..., None] < 1e-3, np.asarray(GLOW_AIR), rgb)
        return {phase: np.dstack([rgb, a])}
    m = column_mask(G, d)
    a_core = 1 - np.exp(-LOOK["sigma_a"] * D * m)
    a_plume = 1 - np.exp(-LOOK["sigma_a"] * D * (1 - m)) * (1 - air)
    rgb_plume = np.where(alpha[..., None] < 1e-3, np.asarray(GLOW_AIR), rgb)
    return {"core": np.dstack([rgb, a_core]), "plume": np.dstack([rgb_plume, a_plume])}


def down(a):
    """SS x straight-alpha float -> 1x, premultiplied box filter (no fringes), back to straight alpha."""
    h, w = a.shape[0] // SS * SS, a.shape[1] // SS * SS
    a = a[:h, :w]
    pre = np.dstack([a[..., :3] * a[..., 3:], a[..., 3:]])
    pre = pre.reshape(h // SS, SS, w // SS, SS, 4).mean(axis=(1, 3))
    al = pre[..., 3:]
    rgb = np.where(al > 1e-4, pre[..., :3] / np.maximum(al, 1e-4), 0.0)
    return np.dstack([np.clip(rgb, 0, 1), np.clip(al, 0, 1)])


def to_bytes(a):
    """8-bit straight alpha. The colour of the steam is spread under its transparent pixels: a sprite is sampled
    bilinearly with straight alpha, so black there would draw a dark fringe round every wisp."""
    out = (np.clip(a, 0, 1) * 255 + 0.5).astype(np.uint8)
    rgb = out[..., :3].astype(np.float32)
    seen = out[..., 3] > 0
    if seen.any():
        w = seen.astype(np.float32)
        filled = seen.copy()
        for sigma in (1, 2, 4, 8, 16, 32):
            num = np.dstack([gaussian_filter(rgb[..., c] * w, sigma) for c in range(3)])
            den = gaussian_filter(w, sigma)[..., None]
            take = (~filled) & (den[..., 0] > 1e-3)
            rgb[take] = num[take] / den[take]
            filled |= take
        rgb[~filled] = np.asarray(GLOW_AIR) * 255
    out[..., :3] = np.where(seen[..., None], out[..., :3], (rgb + 0.5).astype(np.uint8))
    return out


def column_box(d):
    """The body's canvas in the 1x layer: rows/cols of the 1.1 x 1.5 u column above (Up) or below (Down) the mouth."""
    G = grid(d)
    mx, my = HALF_W * PPU, G.y_top * PPU
    x0, x1 = int(round(mx - COL_W / 2 * PPU)), int(round(mx + COL_W / 2 * PPU))
    y0, y1 = (int(round(my - COL_H * PPU)), int(round(my))) if d > 0 else (int(round(my)), int(round(my + COL_H * PPU)))
    return x0, y0, x1, y1


def outputs(job):
    """(relative path, rgba uint8, pivot) for one job."""
    d, phase, s = job
    tag = "Up" if d > 0 else "Down"
    G = grid(d)
    mx, my = HALF_W * PPU, G.y_top * PPU                 # the mouth in the 1x layer, from its top-left
    result = []
    for name, big in frame(d, phase, s).items():
        a = to_bytes(down(big))
        if name == "core":
            x0, y0, x1, y1 = column_box(d)
            a = a[y0:y1, x0:x1]
            pivot = (0.5, 0.0 if d > 0 else 1.0)
        else:
            ys, xs = np.nonzero(a[..., 3] >= ALPHA_MIN * 255)
            if len(xs) == 0: ys, xs = np.array([int(my)]), np.array([int(mx)])
            y0, y1 = max(0, ys.min() - 2), min(a.shape[0], ys.max() + 3)
            x0, x1 = max(0, xs.min() - 2), min(a.shape[1], xs.max() + 3)
            a = a[y0:y1, x0:x1]
            pivot = ((mx - x0) / (x1 - x0), 1 - (my - y0) / (y1 - y0))   # Unity pivots count from the bottom
        result.append((f"{tag}/{name}_{s:02d}.png", a, pivot))
    return result


def jobs():
    for d in (1, -1):
        for s in range(TELL): yield (d, "tell", s)
        for s in range(ERUPT): yield (d, "erupt", s)
        for s in range(FADE): yield (d, "fade", s)


def png_bytes(a):
    import io
    buf = io.BytesIO()
    Image.fromarray(a, "RGBA").save(buf, format="PNG", optimize=False, compress_level=9)
    return buf.getvalue()


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--check", action="store_true")
    args = ap.parse_args()
    with Pool() as pool:
        rendered = [o for out in pool.map(outputs, list(jobs())) for o in out]
    entries, changed, pixels = [], [], 0
    for rel, a, pivot in rendered:
        path = OUT / rel
        data = png_bytes(a)
        pixels += a.shape[0] * a.shape[1]
        if not path.exists() or path.read_bytes() != data:
            changed.append(rel)
            if not args.check:
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_bytes(data)
        entries.append({"path": f"Assets/_Game/Art/Geyser/{rel}", "pivotX": round(pivot[0], 5), "pivotY": round(pivot[1], 5),
                        "ppu": PPU, "sha1": hashlib.sha1(data).hexdigest()})
    manifest = json.dumps({"ppu": PPU, "tell": TELL, "erupt": ERUPT, "fade": FADE, "entries": entries}, indent=1) + "\n"
    if not MANIFEST.exists() or MANIFEST.read_text() != manifest:
        changed.append(MANIFEST.name)
        if not args.check: MANIFEST.write_text(manifest)
    print(f"{len(rendered)} sprites, {pixels / 1e6:.2f} M px; {len(changed)} changed")
    if args.check and changed:
        print("would change:", ", ".join(changed[:12]))
        sys.exit(1)


if __name__ == "__main__":
    main()
