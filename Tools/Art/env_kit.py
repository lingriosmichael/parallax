"""PAX-A15: the environment kit. Turns the developer's ChatGPT stills (Art_Source/Environment/A/ENV-*.png, never changed)
into the slots the level builder draws, at their exact pixel sizes:

- play layer -> Assets/_Game/Art/RealityA/Environment/Kit/ (196.667 px/u; fills are materials at 98.333)
- layers    -> Assets/_Game/Art/RealityA/Environment/Backgrounds/Kit/ (98.333 px/u)

Every step is deterministic: crops, premultiplied Lanczos resamples, offset-and-crossfade seams on tiling axes, luma to
alpha for the effects painted on black, a light blur for the foreground, normal maps for the play layer. It writes
env_kit.json beside the play-layer sprites: each slot's file, size in units and the offsets the builder aligns by
(where a strip's walkable line sits), so the C# side holds no art measurements of its own.

Usage: python3 Tools/Art/env_kit.py [--check]   (--check: report seams and sizes, write nothing)
"""
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter

sys.path.insert(0, str(Path(__file__).parent))
import trap_process as tp  # noqa: E402

ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT / "Art_Source/Environment/A"
KIT = ROOT / "Assets/_Game/Art/RealityA/Environment/Kit"
LAYERS = ROOT / "Assets/_Game/Art/RealityA/Environment/Backgrounds/Kit"
WORLD_PPU = 196.66666
# Gauntlet: layers at 128 px/u, finer than a 20:9 phone at the 1.8x camera (122), so they never draw upsampled.
HALF_PPU = 128.0

# The play layer's one scale: a source pixel of the edge strips becomes this many kit pixels, so the stones on caps,
# undersides and wall faces come out the size of the fill's (about 0.65 u a block).
EDGE_SCALE = 0.556
# ENV-26: the thinnest row between the hanging branch and the ferns.
FG_CUT = 580
# ENV-14b: the thinnest column between the capital and the base (a few vine pixels cross it).
POST_CUT = 989


# ---------- pure helpers (tested) ----------

def crossfade(a, band, axis=1):
    """Seamless along `axis`: the last `band` pixels fade into the first ones and are dropped, so the far edge flows into
    the near one. Premultiplied, so alpha edges don't darken."""
    a = a.astype(np.float32)
    n = a.shape[axis]
    if band <= 0 or band * 2 >= n:
        raise ValueError("band must be positive and under half the length")
    pre = a.copy()
    pre[..., :3] *= pre[..., 3:4]
    head = np.take(pre, range(band), axis=axis)
    tail = np.take(pre, range(n - band, n), axis=axis)
    shape = [1, 1, 1]
    shape[axis] = band
    t = np.linspace(0.0, 1.0, band, dtype=np.float32).reshape(shape)
    t = t * t * (3 - 2 * t)
    blended = tail * (1 - t) + head * t
    body = np.take(pre, range(band, n - band), axis=axis)
    out = np.concatenate([blended, body], axis=axis)
    alpha = out[..., 3:4]
    out[..., :3] = np.where(alpha > 1e-4, out[..., :3] / np.maximum(alpha, 1e-4), 0.0)
    return np.clip(out, 0, 1)


def seam_ratio(a, axis=1):
    """Wrap difference over the mean neighbour difference along `axis` (1 is seamless), over visible pixels."""
    rgb = a[..., :3] * a[..., 3:4]
    if axis == 1:
        wrap = np.abs(rgb[:, 0] - rgb[:, -1]).mean()
        inner = np.abs(rgb[:, 1:] - rgb[:, :-1]).mean()
    else:
        wrap = np.abs(rgb[0] - rgb[-1]).mean()
        inner = np.abs(rgb[1:] - rgb[:-1]).mean()
    return float(wrap / max(inner, 1e-6))


def split_pieces(a, threshold=0.15, gap=6, min_area=4000):
    """Separate pieces on a transparent sheet: runs of visible columns, then runs of visible rows inside each, then the
    columns again inside each row run (pieces stacked and touching the sides split by rows first)."""
    def runs(mask_1d):
        idx = np.nonzero(mask_1d)[0]
        if len(idx) == 0:
            return []
        out, start, prev = [], idx[0], idx[0]
        for i in idx[1:]:
            if i - prev > gap:
                out.append((start, prev + 1))
                start = i
            prev = i
        out.append((start, prev + 1))
        return out

    visible = a[..., 3] > threshold
    boxes = []
    for x0, x1 in runs(visible.any(axis=0)):
        for y0, y1 in runs(visible[:, x0:x1].any(axis=1)):
            for cx0, cx1 in runs(visible[y0:y1, x0:x1].any(axis=0)):
                sub = visible[y0:y1, x0 + cx0:x0 + cx1]
                ys = np.nonzero(sub.any(axis=1))[0]
                box = (x0 + cx0, y0 + ys[0], x0 + cx1, y0 + ys[-1] + 1)
                if (box[2] - box[0]) * (box[3] - box[1]) >= min_area:
                    boxes.append(box)
    return sorted(boxes, key=lambda b: (b[1] // 200, b[0]))


def split_halves(a, x, threshold=0.15):
    """Two pieces either side of column `x`, each trimmed to its visible pixels."""
    boxes = []
    for x0, x1 in ((0, x), (x, a.shape[1])):
        ys, xs = np.nonzero(a[:, x0:x1, 3] > threshold)
        boxes.append((x0 + xs.min(), ys.min(), x0 + xs.max() + 1, ys.max() + 1))
    return boxes


def first_solid_row(a, fraction=0.9):
    rows = (a[..., 3] > 0.9).mean(axis=1)
    hits = np.nonzero(rows >= fraction)[0]
    return int(hits[0]) if len(hits) else 0


def last_solid_row(a, fraction=0.7):
    rows = (a[..., 3] > 0.9).mean(axis=1)
    hits = np.nonzero(rows >= fraction)[0]
    return int(hits[-1]) if len(hits) else a.shape[0] - 1


def scale(a, k):
    return tp.resize(a, max(1, round(a.shape[1] * k)), max(1, round(a.shape[0] * k)))


def fit_width(a, width):
    return tp.resize(a, width, max(1, round(a.shape[0] * width / a.shape[1])))


def soft_disc(size, colour, inner=0.25, power=1.6):
    yy, xx = np.mgrid[0:size, 0:size].astype(np.float32)
    r = np.sqrt((xx - size / 2 + 0.5) ** 2 + (yy - size / 2 + 0.5) ** 2) / (size / 2)
    alpha = np.clip((1 - r) / (1 - inner), 0, 1) ** power
    out = np.zeros((size, size, 4), np.float32)
    out[..., :3] = np.array(colour, np.float32)
    out[..., 3] = alpha
    return out


def vertical_fade(width, height):
    """White, opaque at the top fading to clear at the bottom (smoothstep): the sky's upper colour over its lower one."""
    t = np.linspace(0, 1, height, dtype=np.float32)
    alpha = 1 - t * t * (3 - 2 * t)
    out = np.ones((height, width, 4), np.float32)
    out[..., 3] = alpha[:, None]
    return out


def blur(a, radius):
    pre = a.copy()
    pre[..., :3] *= pre[..., 3:4]
    chans = [np.asarray(Image.fromarray((pre[..., c] * 255 + 0.5).astype(np.uint8), "L").filter(ImageFilter.GaussianBlur(radius))).astype(np.float32) / 255 for c in range(4)]
    out = np.dstack(chans)
    alpha = out[..., 3:4]
    out[..., :3] = np.where(alpha > 1e-3, out[..., :3] / np.maximum(alpha, 1e-3), 0)
    return np.clip(out, 0, 1)


def glow_alpha(a, floor=0.04, ceil=0.6):
    """An effect painted on black: brightness becomes alpha and the colour keeps its full brightness, so the soft edge
    fades out instead of darkening what's behind it."""
    rgb = a[..., :3]
    lum = rgb.max(axis=-1)
    alpha = np.clip((lum - floor) / (ceil - floor), 0.0, 1.0)
    colour = np.clip(rgb / np.maximum(lum[..., None], 1e-3), 0, 1)
    return np.dstack([colour, alpha])


def haze(a, colour, amount):
    """Colour pushed toward `colour` by `amount` (0..1), alpha kept: aerial perspective baked into a layer."""
    out = a.copy()
    out[..., :3] = out[..., :3] * (1 - amount) + np.array(colour, np.float32) * amount
    return out


def soft_base(a, fraction=0.14):
    """PAX-A16 §3.1: a piece's lowest rows fade out, so a cut base never draws a hard line across the scene."""
    out = a.copy()
    n = out.shape[0]
    k = max(2, int(n * fraction))
    ramp = np.ones(n, np.float32)
    ramp[n - k:] = np.linspace(1, 0, k, dtype=np.float32) ** 1.4
    out[..., 3] *= ramp[:, None]
    return out


def darken(a, value):
    out = a.copy()
    out[..., :3] *= value
    return out


# ---------- the kit ----------

def load(name):
    return tp.load_rgba(SRC / f"{name}.png")


def build():
    play, layers, slots = {}, {}, {}

    def put(store, name, a, units_ppu, **meta):
        a = tp.clean_alpha(a)
        store[name] = a
        slots[name] = dict(file=name + ".png", folder="Kit" if store is play else "Backgrounds/Kit",
                           width=round(a.shape[1] / units_ppu, 4), height=round(a.shape[0] / units_ppu, 4), **meta)

    # Fills: 4 u materials, seamless both ways.
    for key, src in (("ENV_Fill_A", "ENV-10a"), ("ENV_Fill_A2", "ENV-10a2"), ("ENV_Fill_A3", "ENV-10a3")):
        a = load(src)
        a = crossfade(crossfade(a, 90, 1), 90, 0)
        # PAX-A16: the mortar's contrast softened (dark-tinted bodies turned the joints into a black grid).
        mean = a[..., :3].mean(axis=(0, 1), keepdims=True)
        a[..., :3] = mean + (a[..., :3] - mean) * 0.72
        grey = a[..., :3].mean(axis=-1, keepdims=True)
        a[..., :3] = a[..., :3] * 0.75 + grey * 0.25   # and a little less orange
        put(play, key, tp.resize(a, 394, 394), HALF_PPU, kind="fill", normal=True)

    # Top cap: ENV-10b's tufts and first course. The walkable line is the stone's top row.
    b = load("ENV-10b")
    surface = first_solid_row(b)
    cap = b[surface - 34: surface + 128]
    cap = crossfade(cap, 120, 1)
    cap = scale(cap, EDGE_SCALE)
    put(play, "ENV_Cap", cap, WORLD_PPU, kind="strip", normal=True, above=round(34 * EDGE_SCALE / WORLD_PPU, 4))

    # Underside: ENV-10c's lower strip (a course, the cornice, hanging vines). The walkable line (gravity up) is the
    # cornice's bottom.
    c = load("ENV-10c")[577:900]
    under_line = last_solid_row(c)
    c = crossfade(c, 120, 1)
    c = scale(c, EDGE_SCALE * 0.9)
    put(play, "ENV_Under", c, WORLD_PPU, kind="strip", normal=True,
        below=round((c.shape[0] - (under_line + 1) * EDGE_SCALE * 0.9) / WORLD_PPU, 4))

    # Thin slab (0.5 u solids): ENV-11's tiling middle and its two ends. Body rows 452..594 become exactly 0.5 u.
    e = load("ENV-11")
    body_top, body_bottom = 452, 595
    k = 0.5 * WORLD_PPU / (body_bottom - body_top)
    strip = e[408:688]
    put(play, "ENV_Slab", scale(crossfade(strip[:, 330:1230], 60, 1), k), WORLD_PPU, kind="strip", normal=True,
        above=round((body_top - 408) * k / WORLD_PPU, 4))
    put(play, "ENV_SlabEnd", scale(strip[:, 32:330], k), WORLD_PPU, kind="end", normal=True,
        above=round((body_top - 408) * k / WORLD_PPU, 4))

    # Wall face: ENV-12's left edge (leaves outside, one block column inside); the right side mirrors it.
    w = load("ENV-12")[:, 407:623]
    w = crossfade(w, 120, 0)
    put(play, "ENV_Side", scale(w, EDGE_SCALE), WORLD_PPU, kind="vstrip", normal=True,
        outside=round(66 * EDGE_SCALE / WORLD_PPU, 4))

    # Posts: 1 u pillar shaft (14a), its capital and base (14b), and the 0.5 u slim post (14c).
    p = load("ENV-14a")[:, 380:644]
    p = crossfade(p, 120, 0)
    put(play, "ENV_Post", scale(p, WORLD_PPU / 226), WORLD_PPU, kind="vstrip", normal=True)
    sheet = load("ENV-14b")
    cap_base = split_halves(sheet, POST_CUT)   # the capital's vines touch the base
    for key, box in zip(("ENV_PostCap", "ENV_PostBase"), cap_base[:2]):
        piece = sheet[box[1]:box[3], box[0]:box[2]]
        put(play, key, fit_width(piece, round(1.5 * WORLD_PPU) if key == "ENV_PostCap" else round(1.25 * WORLD_PPU)), WORLD_PPU, kind="object", normal=True)
    s = load("ENV-14c")[:, 455:572]
    s = crossfade(s, 120, 0)
    put(play, "ENV_SlimPost", scale(s, 0.5 * WORLD_PPU / 82), WORLD_PPU, kind="vstrip", normal=True)

    # Pit water: ENV-30's waterline, tiling sideways.
    water = load("ENV-30")[520:880]
    put(play, "ENV_Water", scale(crossfade(water, 160, 1), 0.45), WORLD_PPU, kind="strip")

    # Door (the lit frame, ruling 5 / Phase 1 answer 2), checkpoint stones idle and crossed.
    door = load("ENV-24")
    boxes = split_pieces(door)
    lit = door[boxes[1][1]:boxes[1][3], boxes[1][0]:boxes[1][2]]
    put(play, "ENV_Door", fit_width(lit, round(1.7 * WORLD_PPU)), WORLD_PPU, kind="object")
    cp = load("ENV-25")
    boxes = split_pieces(cp)
    for key, box in zip(("ENV_Checkpoint", "ENV_CheckpointLit"), boxes[:2]):
        put(play, key, fit_width(cp[box[1]:box[3], box[0]:box[2]], round(1.5 * WORLD_PPU)), WORLD_PPU, kind="object")

    # Dressing: moss caps, drapes, banners, rubble and glyph panels.
    for src, keys, width in (("ENV-21", ["ENV_Moss_0", "ENV_Moss_1", "ENV_Moss_2"], 1.1),
                             ("ENV-20", ["ENV_Drape_0", "ENV_Drape_1", "ENV_Drape_2"], 0.9),
                             ("ENV-22", ["ENV_Banner_0", "ENV_Banner_1"], 1.0),
                             ("ENV-23", ["ENV_Rubble_0", "ENV_Rubble_1", "ENV_Glyph_0", "ENV_Glyph_1"], 1.4)):
        sheet = load(src)
        # ENV-22's two banner rods overlap at the top: split it at the clear gap between the cloths.
        pieces = split_halves(sheet, 540) if src == "ENV-22" else split_pieces(sheet)
        if len(pieces) < len(keys):
            raise ValueError(f"{src}: found {len(pieces)} pieces, expected {len(keys)}")
        for key, box in zip(keys, pieces):
            put(play, key, fit_width(sheet[box[1]:box[3], box[0]:box[2]], round(width * WORLD_PPU)), WORLD_PPU, kind="dressing")

    # Climb vine (PAX-A08's old item 15): top anchor, tiling middle, tip; 0.6 u wide like the grab box.
    v = load("ENV-18")
    anchor, mid, tip = v[119:763, 50:436], v[:, 453:669], v[1020:1498, 762:997]
    mid = crossfade(mid, 140, 0)
    vk = 0.6 * WORLD_PPU / mid.shape[1]
    put(play, "ENV_VineMid", scale(mid, vk), WORLD_PPU, kind="vstrip")
    put(play, "ENV_VineAnchor", scale(anchor, vk), WORLD_PPU, kind="object")
    put(play, "ENV_VineTip", scale(tip, vk), WORLD_PPU, kind="object")

    # Glow behind the door and the flip rings' dark backing (made here, not painted).
    put(play, "ENV_Glow", soft_disc(256, (1.0, 0.82, 0.5)), WORLD_PPU, kind="object")
    put(play, "ENV_Halo", soft_disc(256, (0.16, 0.09, 0.05), inner=0.45, power=1.2), WORLD_PPU, kind="object")

    # ---------- layers (98.333) ----------
    put(layers, "ENV_SkyFade", vertical_fade(8, 256), HALF_PPU, kind="sky")
    put(layers, "ENV_White", np.ones((8, 8, 4), np.float32), HALF_PPU, kind="sky")
    sun = glow_alpha(load("ENV-02"), floor=0.04, ceil=0.8)
    put(layers, "ENV_Sun", tp.resize(sun, 640, 640), HALF_PPU, kind="piece")
    for key, src, band in (("ENV_CloudsFar", "ENV-03c", 260), ("ENV_CloudsMid", "ENV-03b", 260), ("ENV_CloudsNear", "ENV-03a", 260)):
        a = load(src)
        y0, y1 = (lambda r: (max(0, r[0] - 8), min(a.shape[0], r[-1] + 8)))(np.nonzero((a[..., 3] > 0.02).any(axis=1))[0])
        put(layers, key, crossfade(a[y0:y1], band, 1), HALF_PPU, kind="band")
    for key, src, band in (("ENV_Haze", "ENV-06", 300), ("ENV_Fog", "ENV-27", 300)):
        a = glow_alpha(load(src), floor=0.03, ceil=0.6)
        # PAX-A16: luminous mist, cream rather than orange (a saturated glow can't reach a highlight's brightness).
        a[..., :3] = a[..., :3] * 0.45 + np.array([1.0, 0.97, 0.9], np.float32) * 0.55
        rows = np.nonzero((a[..., 3] > 0.02).any(axis=1))[0]
        put(layers, key, crossfade(a[rows[0]:rows[-1] + 1], band, 1), HALF_PPU, kind="band")
    shaft = glow_alpha(load("ENV-28"), floor=0.03, ceil=0.7)
    put(layers, "ENV_Shaft", tp.trim(shaft, 0.02), HALF_PPU, kind="piece")
    mist = glow_alpha(load("ENV-09"), floor=0.04, ceil=0.6)
    put(layers, "ENV_Mist", tp.trim(mist, 0.02), HALF_PPU, kind="piece")
    for src, keys in (("ENV-04", [f"ENV_FarIsland_{i}" for i in range(4)]), ("ENV-05", [f"ENV_FarSpire_{i}" for i in range(4)]),
                      ("ENV-07a", ["ENV_MidArch_0", "ENV_MidArch_1"]), ("ENV-07b", ["ENV_MidTree_0", "ENV_MidTree_1"]),
                      ("ENV-07d", ["ENV_MidColonnade", "ENV_MidTowers"])):
        sheet = load(src)
        pieces = split_pieces(sheet, threshold=0.3)
        if len(pieces) < len(keys):
            raise ValueError(f"{src}: found {len(pieces)} pieces, expected {len(keys)}")
        for key, box in zip(keys, sorted(pieces[:len(keys)], key=lambda b: b[0])):
            put(layers, key, soft_base(sheet[box[1]:box[3], box[0]:box[2]]), HALF_PPU, kind="piece")
    put(layers, "ENV_MidPillar", tp.trim(load("ENV-15"), 0.05), HALF_PPU, kind="piece")
    aq = load("ENV-07c")
    rows = np.nonzero((aq[..., 3] > 0.02).any(axis=1))[0]
    import env_v2 as v2  # noqa: E402
    put(layers, "ENV_MidAqueduct", v2.feather_sides(aq[rows[0]:rows[-1] + 1], 0.1), HALF_PPU, kind="piece")
    wf = load("ENV-08b")[:, 300:722]
    put(layers, "ENV_Waterfall", crossfade(wf, 180, 0), HALF_PPU, kind="vband")
    bw = load("ENV-32")
    rows = np.nonzero((bw[..., 3] > 0.02).any(axis=1))[0]
    # Painted as dark as the platform stone; baked pale and hazy (far-distance stone) so the black cat and the platforms
    # read against it. A hazed copy following the painting's own shape (a veil quad over it showed hard edges).
    put(layers, "ENV_BackWall", haze(crossfade(bw[rows[0]:rows[-1] + 1], 120, 1), (0.97, 0.9, 0.78), 0.6), HALF_PPU, kind="band")
    fg = load("ENV-26")
    # The branch's vine tips touch the ferns: cut the upper piece at its thinnest row (FG_CUT).
    top, roots = split_pieces(fg, threshold=0.3, gap=4)[:2]
    fg_boxes = [(0, 0, fg.shape[1], FG_CUT), (0, FG_CUT, fg.shape[1], top[3]), roots]
    for key, box in zip(("ENV_FG_Branch", "ENV_FG_Ferns", "ENV_FG_Roots"), fg_boxes):
        put(layers, key, blur(scale(fg[box[1]:box[3], box[0]:box[2]], 0.5), 2.0), HALF_PPU, kind="foreground")
    put(layers, "ENV_FG_Trunk", blur(scale(tp.trim(load("ENV-26b"), 0.05), 0.5), 2.0), HALF_PPU, kind="foreground")
    # Gauntlet: the triage's keepers, whole pieces, the painterly play layer (env_v2.py), before the tiers derive from them.
    import env_v2  # noqa: E402
    env_v2.build(sys.modules[__name__], load, put, play, layers)
    # PAX-A16: the depth tiers (env_tiers.py).
    import env_tiers  # noqa: E402
    env_tiers.build(sys.modules[__name__], load, put, play, layers)
    env_v2.finish(layers, slots)
    return play, layers, slots


def write(play, layers, slots):
    KIT.mkdir(parents=True, exist_ok=True)
    LAYERS.mkdir(parents=True, exist_ok=True)
    for store, folder in ((play, KIT), (layers, LAYERS)):
        for name, a in store.items():
            tp.save(a, folder / f"{name}.png")
            if slots[name].get("normal"):
                tp.save(tp.normal_map(a, strength=1.6, bevel_px=2.0), folder / f"{name}_n.png")
    manifest = {"worldPpu": WORLD_PPU, "halfPpu": HALF_PPU,
                "slots": [dict(name=k, **v) for k, v in sorted(slots.items())]}
    (KIT / "env_kit.json").write_text(json.dumps(manifest, indent=2) + "\n")


def report(play, layers, slots):
    lines = []
    for name, a in {**play, **layers}.items():
        s = slots[name]
        axis = {"fill": None, "strip": 1, "band": 1, "vstrip": 0, "vband": 0}.get(s["kind"])
        seams = "" if axis is None else f" seam {seam_ratio(a, axis):.2f}"
        if s["kind"] == "fill":
            seams = f" seam x {seam_ratio(a, 1):.2f} y {seam_ratio(a, 0):.2f}"
        lines.append(f"{name:18} {a.shape[1]:5}x{a.shape[0]:<5} {s['width']:6.2f}x{s['height']:<6.2f}u{seams}")
    return "\n".join(lines)


def main():
    play, layers, slots = build()
    print(report(play, layers, slots))
    if "--check" not in sys.argv:
        write(play, layers, slots)
        print(f"wrote {len(play)} play-layer and {len(layers)} layer sprites")


if __name__ == "__main__":
    main()
