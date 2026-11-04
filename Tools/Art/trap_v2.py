"""PAX-A18 W2 (D-151): the trap kit from the developer's painted pilot set (Art_Source/Traps_ChatGPT/PILOT-*).

Every body keeps its painted material. D-108 is applied lightly: a thin dark outer line and a light inner rim, and the
danger colour only on the cutting edges and tips, never a fill. Every kind has ONE drawn size in world units, the same in
every level (SpikeArt, ArrowArt and SolidArt draw these at their native size, never fitted to a box). The sprites are
made at HI_PPU, finer than a phone's ~152 px/u at the 1.8x camera.

Called by trap_process.build() after the older bodies, so each sprite here replaces its older namesake. A pilot image that
isn't saved yet leaves the older sprite as it was.
"""
import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from scipy import ndimage

import hazard_readable as hr

HI_PPU = 256

# The drawn sizes (world units), one per kind, everywhere.
LAUNCHER_FRAME_TONE = np.array([0.66, 0.62, 0.56], np.float32)   # the carved frame against the walls' stone
# The slot's lip: worn bronze, not pale cream (2026-10-17 visual review, L007-L010: "a bright cream bar ... like a UI button").
LAUNCHER_LIP = np.array([0.97, 0.8, 0.5], np.float32)   # light bronze (0.9, 0.7, 0.4 left L012 Arrow_C at 26 % at 3:1)
VENT_SATURATION = 0.4      # the vent's colour kept (of its painted terracotta)
VENT_TONE = np.array([0.92, 0.88, 0.82], np.float32)
LAUNCHER_U = 0.7          # the launcher's carved frame (square); its niche adds about 0.1 u a side
ARROW_U = (0.8, 0.16)     # the arrow, pointing right
SPEAR_HEAD_U = 0.6        # the spear's blade and collar, along its length
SUNLIT_WOOD = np.array([0.96, 0.8, 0.58], np.float32)   # the spear shaft's highlight (gate 6: lighter, so it clears 3:1 over L017's dark stone; over a pale sky the dark wood reads)
SPEAR_U, SPEAR_TALL_U = 1.4, 0.26   # 2026-10-17: the whole spear, one size everywhere (ArrowLane.SpearLane's default length)
BLADE_H_U = 0.594         # the tallest spike blade (a 0.3 u strip x 1.1 x 1.8, D-101/D-105)
VENT_U = (1.0, 0.3)
CLOUD_U = (2.0, 0.8)
FLIP_U = 1.0
ORB_U = FLIP_U   # D-286: the two orbs are one size (was 0.78)
# D-292: the painted set's cuts and sizes. GT-01's pivot is its collar, GT-10's are as OT-18's (obsidian_traps.py).
ARM_H_U, ARM_PIVOT = 0.62, (0.0436, 0.5)
HUB_U, PLATE_U, STRIP_H_U, CRUSHER_U = 0.5, 1.32, 0.46, 3.0
MOUNT_BOX, EYE_IDLE_BOX, EYE_LOCK_BOX = (10, 1160, 165, 850), (1225, 1712, 8, 414), (1225, 1712, 424, 832)
MOUNT_U, EYE_U, MOUNT_PIVOT, EYE_PIVOT = 0.9, 0.48, (0.5, 0.71), (0.415, 0.5)
PENDULUM_U, PENDULUM_RING_Y, PENDULUM_SHAFT_Y, PENDULUM_HEAD_Y, PENDULUM_SHAFT_X = 0.85, 470, (520, 830), 1040, (430, 594)
BEAM_ROWS = (250, 470)
CUE_MARK_U = (0.7, 0.28)   # the mark over the cat (TrapKitSetup.CueMarkSize)
CUE_RING_U = (1.5, 0.39)  # the halo behind it
CUE_GREEN = np.array([0.07, 0.84, 0.49], np.float32)   # the direction orb's emerald (INV-01)
CUE_MINT = np.array([0.8, 1.0, 0.9], np.float32)
SLAM_PLATE_H_U = 0.16     # the slam's iron striking plate, along its bottom edge
HINGE_U = 0.42            # the hinge floor's knuckle (its height)
CRACK_U = (1.0, 1.0)
CRUMBLE_EDGE_H_U = 0.22


def _img(a):
    return Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8), "RGBA")


def _arr(im):
    return np.asarray(im).astype(np.float32) / 255


def resize(a, w, h):
    return _arr(_img(a).resize((max(1, int(round(w))), max(1, int(round(h)))), Image.LANCZOS))


def fit_h(a, height_u):
    """Uniform scale to `height_u` world units tall at HI_PPU."""
    h, w = a.shape[:2]
    k = height_u * HI_PPU / h
    return resize(a, w * k, h * k)


def fit_box(a, w_u, h_u):
    h, w = a.shape[:2]
    k = min(w_u * HI_PPU / w, h_u * HI_PPU / h)
    return resize(a, w * k, h * k)


def key_magenta(a):
    """Painted on flat magenta: red and blue high and green low goes clear, and any magenta cast is pulled down to green's level."""
    out = a.copy()
    rgb = out[..., :3]
    spill = np.clip(np.minimum(rgb[..., 0], rgb[..., 2]) - rgb[..., 1], 0, 1)
    out[..., 3] = np.minimum(out[..., 3], 1 - np.clip((spill - 0.35) / 0.25, 0, 1))
    out[..., 0] -= spill
    out[..., 2] -= spill
    return np.clip(out, 0, 1)


def key_blue(a):
    """Painted on flat blue (Pack E): blue beating red and green goes clear, and what's left has no cast of it."""
    out = a.copy()
    top = np.maximum(out[..., 0], out[..., 1])
    out[..., 3] = np.minimum(out[..., 3], 1 - np.clip((out[..., 2] - top - 0.05) / 0.25, 0, 1))
    out[..., 2] = np.minimum(out[..., 2], top * 0.9 + 0.02)
    return out


def key_trim(a, box=None):
    if box:
        x0, x1, y0, y1 = box
        a = a[y0:y1, x0:x1]
    a = key_magenta(a)
    ys, xs = np.nonzero(a[..., 3] > 0.03)
    return a[ys.min():ys.max() + 1, xs.min():xs.max() + 1]


def block_on_magenta(a):
    """A keyed piece back on magenta, for key_trim to trim it (its colours are already clean)."""
    out = a.copy()
    out[..., 3] = 1.0
    out[a[..., 3] <= 0.0] = (1.0, 0.0, 1.0, 1.0)
    return out


def seamless_x(a, fade):
    w = a.shape[1] - fade
    out = a[:, :w].copy()
    t = np.linspace(0, 1, fade, dtype=np.float32)[None, :, None]
    out[:, :fade] = a[:, w:w + fade] * (1 - t) + a[:, :fade] * t
    return out


def laser_beam(a):
    """GT-11 on black: a tile one unit tall along x, brightness as alpha, its ends cross-faded, its yellow pulled toward crimson."""
    a = seamless_x(a[BEAM_ROWS[0]:BEAM_ROWS[1], :, :3], 260)
    alpha = np.clip((a.max(-1, keepdims=True) - 0.04) / 0.5, 0, 1)
    rgb = a / np.maximum(a.max(-1, keepdims=True), 1e-3)
    rgb[..., 1] = np.minimum(rgb[..., 1], rgb[..., 2] + (rgb[..., 1] - rgb[..., 2]) * 0.55)
    out = np.dstack([rgb, alpha]).astype(np.float32)
    k = HI_PPU / out.shape[0]
    return resize(out, out.shape[1] * k, HI_PPU)


# D-294: the colours a laser passes through from its centre out: it must read on a bright sky (a dark crimson edge, drawn over it)
# and on black stone (a white-hot core).
BEAM_HOT, BEAM_RED, BEAM_DEEP = np.array([1.0, 0.96, 0.9], np.float32), np.array([1.0, 0.13, 0.08], np.float32), np.array([0.5, 0.02, 0.05], np.float32)


def beam_core(h=256, w=1024):
    """A thin aiming line's tile, one unit tall: white-hot, then crimson, then a dark edge fading out, breathing a little along x."""
    y = np.abs(np.linspace(-1, 1, h, dtype=np.float32))[:, None]
    x = np.arange(w, dtype=np.float32)[None, :] / w * 2 * np.pi
    breathe = 1 + 0.08 * np.sin(x * 3) + 0.05 * np.sin(x * 7 + 1.3)
    d = y / breathe
    hot = np.clip(1 - d / 0.26, 0, 1)[..., None]
    red = np.clip((d - 0.2) / 0.34, 0, 1)[..., None]
    rgb = (BEAM_HOT * hot + BEAM_RED * (1 - hot)) * (1 - red) + BEAM_DEEP * red
    return np.dstack([rgb, np.clip((1 - d) / 0.28, 0, 1)]).astype(np.float32)


def beam_glow(h=256, w=64):
    """A soft glow's tile, white (the renderer tints it), even along x and gone at its edges."""
    y = np.linspace(-1, 1, h, dtype=np.float32)[:, None]
    a = np.exp(-(y / 0.42) ** 2) * np.clip((1 - np.abs(y)) / 0.2, 0, 1)
    return np.dstack([np.ones((h, w, 3), np.float32), np.repeat(a, w, axis=1)]).astype(np.float32)


def beam_burn(n=256):
    """Where a beam lands: a white-hot point in a crimson bloom, with a few uneven rays."""
    yy, xx = np.mgrid[0:n, 0:n].astype(np.float32)
    dx, dy = (xx - (n - 1) / 2) / (n / 2), (yy - (n - 1) / 2) / (n / 2)
    r, angle = np.hypot(dx, dy), np.arctan2(dy, dx)
    rng = np.random.default_rng(2940)
    rays = np.zeros_like(r)
    for _ in range(9):
        at, reach, sharp = rng.uniform(-np.pi, np.pi), rng.uniform(0.55, 1.0), rng.uniform(40, 120)
        off = np.angle(np.exp(1j * (angle - at)))
        rays = np.maximum(rays, np.exp(-sharp * off ** 2) * np.clip(1 - r / reach, 0, 1))
    bloom = np.exp(-(r / 0.42) ** 2)
    a = np.clip(0.75 * bloom + 0.9 * rays, 0, 1) * np.clip((1 - r) / 0.2, 0, 1)
    hot = np.clip(1 - r / 0.2, 0, 1)[..., None]
    rgb = BEAM_HOT * hot + BEAM_RED * (1 - hot)
    return np.dstack([rgb, a]).astype(np.float32)


def cue_mark(orb):
    """The orb's painted arrow, lifted off its glass: its red channel, which the green glass hardly has. A dark halo keeps it on a bright sky."""
    h, w = orb.shape[:2]
    a = orb[int(h * 0.36):int(h * 0.64), int(w * 0.18):int(w * 0.82)]
    shape = np.clip((a[..., 0] * a[..., 3] - 0.42) / 0.3, 0, 1)
    pad = a.shape[0] // 6
    shape = np.pad(shape, pad)
    # Only the arrow: the wisps' brightest crests pass the same test, but they don't touch it.
    solid = Image.fromarray(((shape > 0) * 255).astype(np.uint8), "L").copy()
    y0, x0 = np.unravel_index(np.argmax(shape), shape.shape)
    ImageDraw.floodfill(solid, (int(x0), int(y0)), 128)
    shape = shape * (np.asarray(solid) == 128)
    img = Image.fromarray((shape * 255).astype(np.uint8), "L")
    halo = np.asarray(img.filter(ImageFilter.MaxFilter(9)).filter(ImageFilter.GaussianBlur(5))).astype(np.float32) / 255 * 0.7
    alpha = 1 - (1 - shape) * (1 - halo)
    keep = np.maximum(alpha, 1e-3)[..., None]
    rgb = (CUE_MINT * shape[..., None] + np.array([0.02, 0.2, 0.12], np.float32) * (alpha - shape)[..., None]) / keep
    return trim_alpha(np.dstack([rgb, alpha]))


def trim_alpha(a):
    ys, xs = np.nonzero(a[..., 3] > 0.03)
    return a[ys.min():ys.max() + 1, xs.min():xs.max() + 1]


def cue_ring():
    """Drawn, not painted: a thin halo around the cat's feet, a bright line in a soft glow."""
    w, h = int(CUE_RING_U[0] * HI_PPU), int(CUE_RING_U[1] * HI_PPU)
    y, x = np.mgrid[0:h, 0:w].astype(np.float32) + 0.5
    ox, oy = x - w / 2, y - h / 2
    r = np.maximum(np.hypot(ox / (w / 2 - 20), oy / (h / 2 - 20)), 1e-3)
    d = np.abs(1 - 1 / r) * np.hypot(ox, oy)    # along the ray from the centre, so the glow is one width all the way round
    line = np.exp(-(d / 2.2) ** 2)
    glow = np.exp(-(d / 8) ** 2) * 0.55
    alpha = np.maximum(line, glow)
    rgb = CUE_GREEN + (CUE_MINT - CUE_GREEN) * line[..., None]
    return np.dstack([rgb, alpha]).astype(np.float32)


def column_runs(mask):
    filled = mask.any(axis=0)
    runs, start = [], None
    for x, f in enumerate(list(filled) + [False]):
        if f and start is None: start = x
        elif not f and start is not None: runs.append((start, x)); start = None
    return runs


# The inner rim: warm lit light (a rim light from the sun), not D-108's first bone white, so it reads as light on the material.
RIM = np.array([1.0, 0.86, 0.62], np.float32)


def light_outline(a, outer_px=5, inner_px=6, inner_alpha=0.85, wrap_x=False):
    """D-108: a dark outer line (opaque) and a light inner rim, about 3 and 2 phone pixels at HI_PPU, the rim a warm light
    blended over the painting (its texture shows through), so every lethal silhouette reads on bright sky and dark stone."""
    a = hr.pad(a, outer_px) if not wrap_x else hr.pad(a, outer_px, (True, False, True, False))
    sil = a[..., 3] >= 0.5
    outside = hr._dilate(sil, outer_px, wrap_x) & ~sil
    edge = sil & hr._dilate(~sil, inner_px, wrap_x)
    out = a.copy()
    out[..., :3] = np.where(edge[..., None], out[..., :3] * (1 - inner_alpha) + RIM * inner_alpha, out[..., :3])
    out[..., :3] = np.where(outside[..., None], hr.OUTER, out[..., :3])
    out[..., 3] = np.where(outside, 1.0, out[..., 3])
    return out


LIT = np.array([1.0, 0.9, 0.7], np.float32)
STEEL = np.array([0.64, 0.58, 0.52], np.float32)   # the shadow side's edge: dim polished steel
BRIGHT_STEEL = np.array([0.9, 0.8, 0.64], np.float32)   # a flying piece's shadow edge: it needs both edges bright


def _shift(m, dy, dx):
    out = np.zeros_like(m)
    h, w = m.shape
    ys, yd = (slice(0, h - dy), slice(dy, h)) if dy >= 0 else (slice(-dy, h), slice(0, h + dy))
    xs, xd = (slice(0, w - dx), slice(dx, w)) if dx >= 0 else (slice(-dx, w), slice(0, w + dx))
    out[yd, xd] = m[ys, xs]
    return out


def ragged_vignette(h, w, rng, inset=0.2, soft=0.16, jitter=0.07):
    """An alpha mask that is 1 in the middle and fades out toward the edges along a rounded, irregular line: a carving set
    into the wall rather than a tile laid on it. `inset`, `soft` and `jitter` are fractions of the half-size."""
    yy, xx = np.mgrid[0:h, 0:w].astype(np.float32)
    u, v = (xx + 0.5) / w * 2 - 1, (yy + 0.5) / h * 2 - 1
    d = 1 - (np.abs(u) ** 4 + np.abs(v) ** 4) ** 0.25          # 0 at a rounded-square edge, 1 at the centre
    noise = rng.normal(0, 1, (max(2, h // 12), max(2, w // 12))).astype(np.float32)
    noise = np.array(Image.fromarray(noise).resize((w, h), Image.BICUBIC), np.float32)
    noise /= max(1e-4, np.abs(noise).max())
    edge = inset + jitter * noise
    return np.clip((d - edge) / soft + 0.5, 0, 1).astype(np.float32)


ARROW_EDGE = np.array([1.0, 0.98, 0.94], np.float32)


def iron_edge(a, tip=None, outer_px=4, lit_px=3, tip_px=4, wrap_x=False, shade_px=2, steel=STEEL, core=1.0, lit_colour=None):
    """D-108 for dark forged iron (the developer, 2026-10-16: keep the spikes dark, not cream): a dark outer line; a light
    rim on the LIT side only (light from the upper left: the pixels whose outside lies above or to the left), so the
    blade stays near-black with a bright edge where the sun catches it; and a light-crimson glow on the rim around the
    tips (`tip`: a boolean mask of the tip region), the danger colour where it matters, bright enough to read on dark pits."""
    a = hr.pad(a, outer_px) if not wrap_x else hr.pad(a, outer_px, (True, False, True, False))
    sil = a[..., 3] >= 0.5
    outside = hr._dilate(sil, outer_px, wrap_x) & ~sil
    out = a.copy()
    # `core` darkens the painted iron inside the edges (a near-black core reads at 3:1 on any mid or light background).
    out[..., :3] = np.where(sil[..., None], out[..., :3] * core, out[..., :3])
    lit = np.zeros_like(sil)
    for d in range(1, lit_px + 1):
        lit |= sil & (~_shift(sil, d, 0) | ~_shift(sil, 0, d) | ~_shift(sil, d, d))   # outside is above or left
    # The shadow side's edge: a mid steel-grey (polished iron catching the sky), light enough to read on a dark pit (3:1),
    # dark enough that the blade still reads as iron, not a pale border.
    shade = sil & hr._dilate(~sil, shade_px, wrap_x) & ~lit
    out[..., :3] = np.where(shade[..., None], out[..., :3] * 0.1 + steel * 0.9, out[..., :3])
    out[..., :3] = np.where(lit[..., None], out[..., :3] * 0.1 + (LIT if lit_colour is None else lit_colour) * 0.9, out[..., :3])
    if tip is not None:
        tipmask = np.zeros_like(sil)
        tipmask[outer_px:outer_px + tip.shape[0], outer_px:outer_px + tip.shape[1]] = tip if not wrap_x else tip[:, :tipmask.shape[1]]
        glow = sil & hr._dilate(~sil, tip_px, wrap_x) & tipmask
        out[..., :3] = np.where(glow[..., None], out[..., :3] * 0.1 + hr.TIP_RIM * 0.9, out[..., :3])
    out[..., :3] = np.where(outside[..., None], hr.OUTER, out[..., :3])
    out[..., 3] = np.where(outside, 1.0, out[..., 3])
    return out


def niche(a, depth_px=22, soft_px=10):
    """A carved recess around a piece set into a wall: a dark band depth_px wide outside its silhouette, deepest against
    it, fading over its last soft_px into the wall (so its outer edge has no hard line)."""
    a = hr.pad(a, depth_px)
    sil = a[..., 3] >= 0.5
    dist = np.zeros(sil.shape, np.float32)
    ring = sil.copy()
    for d in range(1, depth_px + 1):
        grown = hr._dilate(ring, 1)
        dist[grown & ~ring] = d
        ring = grown
    band = (dist > 0)
    alpha = np.where(band, np.clip((depth_px - dist) / soft_px, 0, 1) * 0.92, 0)
    shade = 0.04 + 0.10 * (dist / depth_px)
    out = a.copy()
    out[..., :3] = np.where(band[..., None], np.array([0.30, 0.22, 0.15], np.float32) * shade[..., None] / 0.14, out[..., :3])
    out[..., 3] = np.where(band, alpha, out[..., 3])
    return out


def edge_danger(a, region, strength=0.75, width_px=3):
    """The danger colour along the silhouette's edge inside `region` (a boolean mask): the cutting edges and tips."""
    sil = a[..., 3] >= 0.5
    edge = sil & hr._dilate(~sil, width_px) & region
    out = a.copy()
    out[..., :3] = np.where(edge[..., None], out[..., :3] * (1 - strength) + hr.DANGER * strength, out[..., :3])
    return out


def build(emit, load_rgba, clean_alpha, trim, chatgpt):
    def src(name):
        p = chatgpt / f"{name}.png"
        return p if p.exists() else None

    def put(name, a, tiled=False, normal=True, pivot=(0.5, 0.5), source=None):
        emit(name, a, pivot=pivot, normal=normal, tiled=tiled, sources=[source] if source else [], ppu=HI_PPU)

    def projectile_rim(a):
        """Gate 6: the soft dark rim behind an arrow, as hazard_art's behind the thorns and blades (about 0.02 u). Padded evenly, so its
        centre pivot matches the arrow's."""
        from hazard_art import rim
        return rim(a, (2, 3))[0]

    # ---- the launcher (2A): its frame in the wall, the slot facing right, the slot's lips edged crimson ----
    if (s := src("PILOT-02A_launcher_v1")):
        a = clean_alpha(trim(load_rgba(s)))
        # The carved frame only: the painting's outer margin of plain wall (fading out) would sit over the level's own wall.
        h0, w0 = a.shape[:2]
        a = a[int(h0 * 0.11):int(h0 * 0.89), int(w0 * 0.11):int(w0 * 0.89)]
        a = fit_box(a, LAUNCHER_U, LAUNCHER_U)
        h, w = a.shape[:2]
        yy, xx = np.mgrid[0:h, 0:w]
        # The slot: the darkest band across the middle. Its lips catch the danger colour, a few px deep.
        lum = (a[..., :3] * np.array([0.3, 0.55, 0.15], np.float32)).sum(-1)
        slot = (lum < 0.12) & (a[..., 3] > 0.5) & (yy > h * 0.3) & (yy < h * 0.62) & (xx > w * 0.2) & (xx < w * 0.8)
        # 2026-10-17: only the slot itself, the band of rows that are mostly dark around the darkest one (the painted panel's
        # grid lines above and below it made the mouth a rectangle).
        band = slot[:, int(w * 0.2):int(w * 0.8)].mean(axis=1)
        r0, r1 = int(h * 0.4), int(h * 0.62)   # the arrows' slot, under the panel's top bar
        mid = r0 + int(np.argmax(band[r0:r1]))
        lo, hi = mid, mid
        while lo > 0 and band[lo - 1] > 0.5: lo -= 1
        while hi < h - 1 and band[hi + 1] > 0.5: hi += 1
        slot &= (yy >= lo) & (yy <= hi)
        # Only the slot's own rim (a 1-2 px line just inside the dark), so the mouth reads as the danger point without
        # turning the carving red; the tell (ArrowArt) adds the crimson glow on top.
        rim = slot & hr._dilate(~slot, 1)
        a[..., :3] = np.where(rim[..., None], a[..., :3] * 0.75 + hr.DANGER * 0.25, a[..., :3])
        # The slot as deep a shadow as the painting allows (it is the danger point, D-108), the frame outlined.
        a[..., :3] = np.where(slot[..., None], a[..., :3] * 0.4, a[..., :3])
        # D-108 amendment (the developer, 2026-10-16): the launcher is two layers. The carved frame isn't lethal and blends into
        # the wall (no outline); the MOUTH (the slot, its bronze lips, the crimson rim) is the lethal part, drawn over it on the
        # same canvas, outlined to read on any wall; HazardContrastTests measures the mouth. A disguised launcher reveals the
        # mouth alone, opening in its own wall.
        # 2026-10-17 (the developer: "The arrow launchers are way to square and dont look like a part of the wall"): the mouth is
        # the slot and its pale bronze lip, a few px (it was the whole panel filled pale gold and outlined: a cream box on the
        # wall); the lip and rim are what read on a dark wall (HazardContrastTests).
        mouth_mask = hr._dilate(slot, max(3, int(h * 0.035)))
        mouth = a.copy()
        mouth[..., 3] = np.where(mouth_mask, mouth[..., 3], 0.0)
        # The slot a deep shadow; its bronze lips (the ring around it) catch the light, pale gold: dark against light, so the
        # mouth reads on light and dark walls alike.
        mouth[..., :3] = np.where(slot[..., None], mouth[..., :3] * 0.5, mouth[..., :3])
        lips = mouth_mask & ~slot
        mouth[..., :3] = np.where(lips[..., None], np.clip(mouth[..., :3] * 0.4 + LAUNCHER_LIP * 0.6, 0, 1), mouth[..., :3])
        put("Bodies/TRAP-01_launcher_mouth", iron_edge(mouth, tip=None, outer_px=4, lit_px=5, shade_px=5, steel=BRIGHT_STEEL), source=s)   # D-303: wider edges, for dark walls (L015)
        # The frame under the mouth is plain carved stone (filled from the stone around it), so the mouth is the only place
        # the slot exists: hidden, the frame shows unbroken stone there; shown, the dark slot reads against that stone.
        frame = a.copy()
        keep = (~mouth_mask).astype(np.float32)[..., None]
        num = _arr(_img(np.concatenate([frame[..., :3] * keep, keep], -1)).filter(ImageFilter.GaussianBlur(14)))
        fill = num[..., :3] / np.maximum(num[..., 3:4], 1e-3)
        frame[..., :3] = np.where(mouth_mask[..., None], np.clip(fill, 0, 1), frame[..., :3])
        # 2026-10-17 visual review ("the arrow launchers are pale cream boxes that look unlit and flat ... about twice as bright
        # as the stone around them"): the carving toned to the walls' lit stone (it isn't lethal; the mouth reads on its own).
        frame[..., :3] = frame[..., :3] * LAUNCHER_FRAME_TONE
        # 2026-10-17 ("way to square ... dont look like a part of the wall"): the carving fades into the wall along a ragged,
        # rounded line, no square silhouette.
        frame[..., 3] *= ragged_vignette(h, w, np.random.default_rng(1207))
        put("Bodies/TRAP-01_arrow_launcher", hr.pad(frame, 5), source=s)

    # ---- the arrow (2C): painted, the head's cutting edges and the fletching's edge crimson ----
    if (s := src("PILOT-02C_arrow_v1")):
        a = clean_alpha(trim(load_rgba(s)))
        a = fit_box(a, ARROW_U[0], ARROW_U[1])
        # Twice as thick as painted (the grey-box arrow is fitted by its length: a thin painting drew a hairline, D-108).
        a = resize(a, a.shape[1], a.shape[0] * 2.0)
        h, w = a.shape[:2]
        xx = np.mgrid[0:h, 0:w][1]
        a[..., :3] = a[..., :3] * 0.55
        # D-303: its edges near white: the game's grade lifts blacks to about L 0.03, so on a mid-tone wall (L 0.12) only a light edge can reach
        # 3:1, and this one missed by a hair (HazardContrastTests: L004 Arrow_Run, 2.97:1).
        # D-318: a wider lit edge (8 px read 28 % at 3:1 mid-lane in L004's Arrow_Run, 10 px 30 %).
        arrow = iron_edge(a, tip=(xx >= w * 0.78) | (xx <= w * 0.14), lit_px=12, shade_px=7, steel=ARROW_EDGE, lit_colour=ARROW_EDGE)
        put("Bodies/TRAP-02_arrow", arrow, source=s)
        put("Bodies/TRAP-02_arrow_rim", projectile_rim(arrow), normal=False, source=s)   # gate 6: its dark rim (ArrowArt's Rim)

    # ---- the spear (2D): the blade and collar as the head; a straight run of the shaft, repeated ----
    # D-312: Pack E's TE-01 (on blue, its collar from 72 % of its length) replaces the pilot when it's saved.
    def spear(s, a, head_from, metal_from, tone=0.58, rim=3):
        a = clean_alpha(trim(a))
        op = a[..., 3] > 0.5
        thick = op.sum(axis=0)
        shaft_t = float(np.median(thick[thick > 0]))
        # The head: the blade and its iron collar (the pilot's right 23%), a little shorter than painted (x0.6):
        # the painted blade is slender, and in game the head sits on a 0.4 u thick shaft the cat can stand on.
        w = a.shape[1]
        head = trim(a[:, int(w * head_from):])
        head = resize(head, head.shape[1] * 0.6, head.shape[0])
        k = SPEAR_HEAD_U * HI_PPU / max(1, head.shape[1])
        head = resize(head, head.shape[1] * k, head.shape[0] * k)
        # The shaft at its drawn thickness (a spear's grey-box is 0.4 u, drawn 0.85 x 1.1 of it), so it isn't stretched.
        k = 0.4 * 1.1 * 0.85 * HI_PPU / shaft_t
        hh, hw = head.shape[:2]
        xx = np.mgrid[0:hh, 0:hw][1]
        head[..., :3] = head[..., :3] * 0.55
        # 2026-10-17 (the developer: "Why are lances still using the old design? ... I did a new design for it", "We also have a
        # lot of different size lances. We agreed on same sizes"; ruled: "Fixed art, keep hitboxes, and fixed length"): the
        # whole painted spear (pommel, wooden shaft, bindings, collar, blade) at one size, SPEAR_U long, its height stretched
        # to SPEAR_TALL_U so it reads at phone scale; a thin dark outline and lit rim (D-108), the blade's edge crimson.
        whole = resize(a, SPEAR_U * HI_PPU, SPEAR_TALL_U * HI_PPU)
        wh, ww = whole.shape[:2]
        wx = np.mgrid[0:wh, 0:ww][1]
        # Darkened to 0.65 with a 4 px rim: with 0.8 and a 2 px rim, L017 V1 and L019 Spear_1 read under 30 % at 3:1 (the dark
        # wood over L019's dark drapery); darker wood barely helped (0.52: 28 %) and lost the painting, the wider rim reads there.
        # 0.58 (was 0.65) once the pits' back walls were lightened (L015 Spear_B over one: 28 % at 0.65).
        whole[..., :3] = whole[..., :3] * tone
        # The shaft is round, lit from the upper left: a band of sunlit wood along its top (0.22-0.38 of its thickness down,
        # fading out by 0.14 and 0.46), so it also reads over dark stone (L019 Spear_1: 28 % at 3:1 without it); on the collar
        # and blade (from 0.5-0.6 of the length) it's a thinner steel glint along the bevel.
        sil = whole[..., 3] >= 0.5
        rows = np.arange(wh)[:, None]
        has = sil.any(axis=0)
        top = np.where(has, sil.argmax(axis=0), 0)[None, :]
        bot = np.where(has, wh - 1 - sil[::-1].argmax(axis=0), 0)[None, :]
        f = (rows - top) / np.maximum(1, bot - top)
        metal = np.clip((wx - ww * metal_from) / (ww * 0.1), 0.0, 1.0)
        # Narrower on the metal (a thin bevel line, the bindings kept); 0.2 on the wood (0.16 read 29 % over L015's lightened pit).
        reach, soft = 0.2 - 0.11 * metal, 0.08 - 0.03 * metal
        band = (np.clip((reach - np.abs(f - 0.3)) / soft, 0.0, 1.0) * 0.95 * sil)[..., None]
        glint = SUNLIT_WOOD * (1 - metal[..., None]) + BRIGHT_STEEL * metal[..., None]
        whole[..., :3] = whole[..., :3] * (1 - band) + glint * band
        put("Bodies/TRAP-05_spear", iron_edge(whole, tip=wx >= ww * 0.86, outer_px=rim, lit_px=rim + 1, shade_px=4, steel=BRIGHT_STEEL), pivot=(0.5, 0.5), source=s)
        put("Bodies/TRAP-05_spear_head", iron_edge(head, tip=xx >= hw * 0.55, lit_px=5, shade_px=5, steel=BRIGHT_STEEL), pivot=(0.5, 0.5), source=s)
        # The shaft: the most even stretch between a quarter and a half of the length.
        x0, x1 = int(w * 0.22), int(w * 0.48)
        seg = a[:, x0:x1]
        rows = np.nonzero((seg[..., 3] > 0.5).any(axis=1))[0]
        seg = seg[rows[0]:rows[-1] + 1]
        seg = resize(seg, seg.shape[1] * k, seg.shape[0] * k)
        seg[..., :3] *= 0.45  # dark oiled wood, toward the near-black shafts that read on bright skies (D-108)
        # Crossfade its ends so it repeats without a seam.
        band = max(4, seg.shape[1] // 8)
        t = np.linspace(0, 1, band, dtype=np.float32)[None, :, None]
        seg[:, :band] = seg[:, :band] * t + seg[:, -band:] * (1 - t)
        seg = seg[:, :-band]
        put("Bodies/TRAP-05_spear_shaft", iron_edge(seg, wrap_x=True, lit_px=15, shade_px=15, steel=BRIGHT_STEEL), tiled=True, source=s)

    if (s := src("TE-01_spear_v1")):
        # Darker wood and a wider rim than the pilot's: at 0.58 and 3 px, L011's Spear_Gap read 26 % at 3:1 over its pale sky.
        spear(s, key_blue(load_rgba(s)), 0.72, 0.68, tone=0.5, rim=4)
    elif (s := src("PILOT-02D_spear_v1")):
        spear(s, load_rgba(s), 0.77, 0.5)

    # ---- the spike blades (1R's row and 01's five shapes): each blade on its own, at one scale; the tip edged crimson ----
    def cut_blades(path, socket):
        """The separate blades of a painting (left to right), cropped from the top of the tallest to the base line, and the
        base line's row. `socket`: the painting stands its blades on a stone course (cut off at the course's first row)."""
        a = clean_alpha(trim(load_rgba(path)))
        op = a[..., 3] > 0.5
        base = int(np.argmax(op.mean(axis=1) > 0.85)) if socket else a.shape[0] - 1
        area = a[:base + 2]
        runs = [r for r in column_runs(area[:max(1, base - 12), :, 3] > 0.5) if r[1] - r[0] > 24]
        top = min(int(np.nonzero((area[:, x0:x1, 3] > 0.5).any(axis=1))[0][0]) for x0, x1 in runs)
        return [area[top:, max(0, x0 - 2):x1 + 2] for x0, x1 in runs]

    blades = []
    for name, socket in (("PILOT-01R_spike_row_v1", True), ("PILOT-01_spike_blades_iron_v1", False)):
        if (s := src(name)):
            cut = cut_blades(s, socket)
            tallest = max(b.shape[0] for b in cut)
            blades += [(b, tallest, s) for b in cut]
    for i, (b, tallest, s) in enumerate(blades):
        # Each painting's tallest blade at the one standard height, its outline included (light_outline pads 5 px a side).
        k = (BLADE_H_U * HI_PPU - 10) / tallest
        b = resize(b, b.shape[1] * k, b.shape[0] * k)
        h = b.shape[0]
        rows = np.nonzero((b[..., 3] > 0.5).any(axis=1))[0]
        tip = rows[0] if rows.size else 0
        yy = np.mgrid[0:h, 0:b.shape[1]][0]
        # D-105 (black spikes): the painted iron pushed toward near-black, its texture kept as a value change.
        b[..., :3] = b[..., :3] * 0.4
        put(f"Bodies/SPIKE_blade_{i}", iron_edge(b, tip=yy <= tip + (h - tip) * 0.32, core=0.4, outer_px=3, lit_px=4, shade_px=3, steel=BRIGHT_STEEL, tip_px=6), pivot=(0.5, 0.0), source=s)
    emit_meta["blades"] = len(blades)

    # ---- the vent (2B) ----
    if (s := src("PILOT-02B_vent_v1")):
        a = clean_alpha(trim(load_rgba(s)))
        # 2026-10-18 visual review (L013: "a saturated orange vent blob and flat orange vent strips"): the terracotta pulled
        # toward the walls' stone (VENT_SATURATION, VENT_TONE); it warms through its tell in code (GeyserArt).
        lum = (a[..., :3] * np.array([0.299, 0.587, 0.114], np.float32)).sum(-1, keepdims=True)
        a[..., :3] = np.clip((lum + (a[..., :3] - lum) * VENT_SATURATION) * VENT_TONE, 0.0, 1.0)
        put("Bodies/TRAP-03_geyser_vent", fit_box(a, VENT_U[0], VENT_U[1] * 1.6), pivot=(0.5, 0.5), source=s)

    # ---- the storm cloud (8), the inverter orb (9), the flip ring (13) ----
    if (s := src("PILOT-08_storm_cloud_v1")):
        cloud = fit_box(clean_alpha(trim(load_rgba(s))), CLOUD_U[0], CLOUD_U[1] * 1.4)
        # 2026-10-17 visual review (L007: "its palette matches the background storm clouds ... reads as scenery, not a hazard";
        # L010: "a warm sepia, sunset-lit cumulus ... reads as a cute puff"): a darker, cooler thundercloud, so it never passes
        # for the sky's own clouds.
        grey = cloud[..., :3].mean(-1, keepdims=True)
        cloud[..., :3] = np.clip((cloud[..., :3] * 0.6 + grey * 0.4) * np.array([0.68, 0.7, 0.78], np.float32), 0, 1)
        put("Bodies/TRAP-09_storm_cloud", cloud, source=s)
    # D-284: the direction orb, one art in every world. The cue on the cat follows it: the mark is the orb's own arrow, the ring its green.
    if (s := src("INV-01_direction_orb_v2")):
        orb = clean_alpha(trim(load_rgba(s)))
        put("Bodies/TRAP-06_inverter_orb", fit_box(orb, ORB_U, ORB_U), source=s)
        put("Bodies/TRAP-07_cue_mark", fit_box(cue_mark(orb), CUE_MARK_U[0], CUE_MARK_U[1]), normal=False, source=s)
        put("Bodies/TRAP-07_cue_ring", cue_ring(), normal=False, source=s)
    if (s := src("PILOT-13_flip_orb_v1")):
        put("Bodies/TRAP-08_glyph_ring", fit_box(clean_alpha(trim(load_rgba(s))), FLIP_U, FLIP_U), normal=False, source=s)

    # ---- D-292: the painted set ----
    if (s := src("GT-01_spinner_arm_v2")):
        arm = key_trim(load_rgba(s))
        put("Bodies/GT-01_arm", fit_box(arm, 99, ARM_H_U), tiled=True, pivot=ARM_PIVOT, source=s)
    if (s := src("GT-02_spinner_hub_v1")):
        put("Bodies/GT-02_hub", fit_box(key_trim(load_rgba(s)), HUB_U, HUB_U), source=s)
    if (s := src("GT-04_spiked_underside_v1")):
        strip = seamless_x(clean_alpha(key_magenta(load_rgba(s))), 80)
        put("Bodies/GT-04_strip", fit_h(strip, STRIP_H_U), tiled=True, pivot=(0.5, 1.0), source=s)
    if (s := src("GT-05_spiked_crusher_v1")):
        # The painting's loose chips float off it: only the block itself goes in.
        block = key_trim(load_rgba(s))
        lab, n = ndimage.label(block[..., 3] > 0.5)
        block[..., 3] *= lab == 1 + int(np.argmax(ndimage.sum(block[..., 3] > 0.5, lab, range(1, n + 1))))
        put("Bodies/GT-05_crusher", fit_box(key_trim(block_on_magenta(block)), CRUSHER_U, CRUSHER_U), pivot=(0.5, 1.0), source=s)
    if (s := src("GT-07_pendulum_blade_v1")):
        p = load_rgba(s)
        k = PENDULUM_U / (p.shape[1] - 132)
        part = lambda box: key_trim(p, box)
        def scaled(a): return resize(a, a.shape[1] * k * HI_PPU, a.shape[0] * k * HI_PPU)
        put("Bodies/GT-07_ring", scaled(part((300, 724, 0, PENDULUM_RING_Y))), pivot=(0.5, 0.92), source=s)
        shaft = key_magenta(p[PENDULUM_SHAFT_Y[0]:PENDULUM_SHAFT_Y[1], PENDULUM_SHAFT_X[0]:PENDULUM_SHAFT_X[1]])
        mid = shaft.shape[0] // 2
        shaft = np.concatenate([shaft[mid:], shaft[:mid]], 0)
        put("Bodies/GT-07_shaft", scaled(shaft), tiled=True, pivot=(0.5, 1.0), source=s)
        put("Bodies/GT-07_head", scaled(part((0, p.shape[1], PENDULUM_HEAD_Y, p.shape[0]))), pivot=(0.5, 0.0), source=s)
    if (s := src("GT-08_pressure_plate_v1")):
        put("Bodies/GT-08_plate", fit_box(key_trim(load_rgba(s)), PLATE_U, PLATE_U), source=s)
    if (s := src("GT-10_turret_v1")):
        t = load_rgba(s)
        put("Bodies/GT-10_mount", fit_box(key_trim(t, MOUNT_BOX), MOUNT_U, MOUNT_U), pivot=MOUNT_PIVOT, source=s)
        put("Bodies/GT-10_eye_idle", fit_box(key_trim(t, EYE_IDLE_BOX), EYE_U, EYE_U), pivot=EYE_PIVOT, source=s)
        put("Bodies/GT-10_eye_lock", fit_box(key_trim(t, EYE_LOCK_BOX), EYE_U, EYE_U), pivot=EYE_PIVOT, source=s)
    if (s := src("GT-11_turret_beam_v2")):
        put("Effects/GT-11_beam", laser_beam(load_rgba(s)), tiled=True, normal=False, source=s)
    # D-294: a laser's light, made here: a thin line's hot core, a soft glow, and the burn where it lands.
    put("Effects/LASER_core", beam_core(), tiled=True, normal=False)
    put("Effects/LASER_glow", beam_glow(), tiled=True, normal=False)
    put("Effects/LASER_burn", beam_burn(), normal=False)

    # ---- the slam's iron striking plate (7): its bottom band, repeated along a slam's width ----
    if (s := src("PILOT-07_slam_v2")):
        a = clean_alpha(trim(load_rgba(s)))
        h, w = a.shape[:2]
        lum = (a[..., :3] * np.array([0.3, 0.55, 0.15], np.float32)).sum(-1)
        dark = (lum < 0.22).mean(axis=1)
        start = next((y for y in range(int(h * 0.6), h) if dark[y] > 0.5), int(h * 0.82))
        plate = a[start:, int(w * 0.08):int(w * 0.92)]
        plate = fit_h(plate, SLAM_PLATE_H_U)
        band = max(4, plate.shape[1] // 10)
        t = np.linspace(0, 1, band, dtype=np.float32)[None, :, None]
        plate[:, :band] = plate[:, :band] * t + plate[:, -band:] * (1 - t)
        plate = plate[:, :-band]
        put("Bodies/SLAM_plate", plate, tiled=True, pivot=(0.5, 0.0), source=s)

    # ---- the hinge floor's knuckle (11): its left end ----
    if (s := src("PILOT-11_hinge_floor_v1")):
        a = clean_alpha(trim(load_rgba(s)))
        h, w = a.shape[:2]
        knuckle = trim(a[:int(h * 0.62), :int(w * 0.075)])
        put("Bodies/HINGE_knuckle", fit_h(knuckle, HINGE_U), pivot=(0.5, 0.5), source=s)

    # ---- cracks and the crumbling edge (15) ----
    if (s := src("PILOT-15_crumble_v1")):
        a = clean_alpha(load_rgba(s))
        h, w = a.shape[:2]
        top, bottom = a[:int(h * 0.55)], a[int(h * 0.55):]
        runs = [r for r in column_runs(top[..., 3] > 0.3) if r[1] - r[0] > 20]
        for i, (x0, x1) in enumerate(runs[:3]):
            put(f"Effects/CRACK_{i}", fit_box(trim(top[:, x0:x1]), CRACK_U[0], CRACK_U[1]), normal=False, source=s)
        if runs[1:2]:
            x0, x1 = runs[1]
            put("Effects/TRAP-11_block_crack", fit_box(trim(top[:, x0:x1]), CRACK_U[0], CRACK_U[1]), normal=False, source=s)
        edge = trim(bottom)
        put("Effects/CRUMBLE_edge", fit_h(edge, CRUMBLE_EDGE_H_U), tiled=True, normal=False, pivot=(0.5, 1.0), source=s)


emit_meta = {}
