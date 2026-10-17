"""PAX-A18 W3 (amends D-114): bumpy stone. Drawn only; colliders, triggers and routes are untouched.

Every outline is cut into texture alpha here (the builder places the strips; SoloRoomSkin decides which strip a face gets):

- Walkable tops (ENV_Cap, the slab's top, ENV_UnderWalk for undersides a gravity-up cat stands on): stone lumps from the
  walk line up to TOP_LUMP beyond it, on the cat's side, drawn behind the cat; never alpha missing at or inside the line,
  so no paw ever stands over a drawn dip.
- Side faces where a cat can be (the walk band: the top 0.6 u of a face, and anything a cat beside or above can touch):
  ENV_SideChip_<fill>_<v>, an outline within BAND_REACH of the collider's face both ways (chipped, rounded, a proud course).
- Side faces beside a hazard: ENV_SideIn_<fill>_<v>, D-114's own strips (env_kit.chip_strip), so what's behind a hazard stays as it was.
- Side faces nothing can reach (pit walls, faces below the band over air): ENV_SideDeep_<fill>_<v>, big broken courses,
  overhangs and notches within DEEP_REACH both ways.
- Undersides no cat stands on (ENV_Under, the slab's underside in rooms without a gravity flip): broken, with hanging stones
  down to HANG below the line.
- Thin slabs: an irregular body, a hanging underside, ENV_SlabEnd (a broken end within BAND_REACH) and ENV_SlabEndThick
  (the thick end: a chunk hanging to SLAB_THICK below the slab); ENV_SlabFlip keeps a walkable underside for flip rooms.
- ENV_CapEnd_<v>: the broken corner at a top's exposed end (outline within BAND_REACH).

Every strip is periodic along its tiling axis (they tile in world space), and deterministic per seed.
"""
import numpy as np

# The developer (2026-10-16): "All edges need to be a bit bumpy" (no straight stretch on any edge, tops included).
# 2026-10-17 (the developer: "All the bumpy edges are done WAAAAAY too obvious reduce it a bit"): every reach about 40% less.
TOP_LUMP = 0.05      # stone drawn beyond a walkable line (behind the cat)
HAZARD_LUMP = 0.03   # under a hazard's spikes: smaller lumps (D-108, the blades' bases keep their air)
TOP_FLAT = 0.0       # the share of a top left on its line (was 0.45: long ruler-straight stretches)
LUMP_FLOOR = 0.3     # the lowest lump, as a share of the lump height (outside the flat share)
BAND_REACH = 0.08    # side faces the cat can touch: outline within this inside the collider's face (never proud: the cat stands
                     # against it, 2026-10-17 "Cat is overlapping ... This should be a general rule everywhere")
DEEP_REACH = 0.18    # side faces nothing reaches: outline within this
HAZARD_REACH = 0.06  # side faces beside a hazard keep D-114's strips (env_kit.CHIP_REACH)
# The fill stops BAND_REACH inside a wholly exposed side along its band strips and DEEP_REACH along its deep strips; each
# strip covers STRIP_OVERLAP past that, fading into the fill.
STRIP_OVERLAP = 0.10
STRIP_BLEND = 0.07
HANG = 0.09          # hanging stones under undersides no cat stands on
SLAB_THICK = 0.25    # the thick end's chunk below a slab
CORNER_IN = BAND_REACH   # how far a broken top corner may eat into the top, inward from the face (the side's band)


def periodic_knots(rng, length, step_range, value_range, closed=True):
    """Knots at random spacing over [0, length] with random values; the last equals the first (seamless)."""
    knots = [0.0]
    while knots[-1] < length:
        knots.append(knots[-1] + rng.uniform(*step_range))
    knots = np.array(knots[:-1] + [length], np.float32)
    values = rng.uniform(*value_range, len(knots)).astype(np.float32)
    if closed:
        values[-1] = values[0]
    return knots, values


def wrap_dist(y, c, n):
    d = np.abs(y - c)
    return np.minimum(d, n - d)


def fractal(n, rng, cycles_lo, cycles_hi, power=0.9):
    """Periodic 1D fractal noise over n samples (integer cycles only, so it wraps), zero-mean, peak about 1."""
    y = np.arange(n, dtype=np.float32) * (2 * np.pi / n)
    out = np.zeros(n, np.float32)
    for k in range(int(cycles_lo), int(cycles_hi) + 1):
        out += np.sin(k * y + rng.uniform(0, 2 * np.pi)).astype(np.float32) / k ** power
    return out / max(1e-4, np.abs(out).max())


def courses(n, ppu, rng, height_range, level_range, joint_u=0.05):
    """Masonry courses down a face: runs of height_range (u) at depths from level_range, joined by short slanted joints
    (joint_u), each course's lower corner chipped round. Periodic over n."""
    y = np.arange(n, dtype=np.float32)
    edges = [0.0]
    while edges[-1] < n:
        edges.append(edges[-1] + rng.uniform(*height_range) * ppu)
    edges = np.array(edges[:-1] + [n], np.float32)
    levels = rng.uniform(*level_range, len(edges)).astype(np.float32)
    levels[-2:] = levels[0]          # the last course is the first one continuing across the seam
    idx = np.clip(np.searchsorted(edges, y, side="right") - 1, 0, len(levels) - 2)
    depth = levels[idx].copy()
    j = joint_u * ppu
    for i in range(1, len(edges) - 1):                         # slanted joints and chipped corners
        lo, hi, e = levels[i - 1], levels[i], edges[i]
        d = y - e
        ramp = np.clip((d + j) / (2 * j), 0, 1)
        near = np.abs(d) < j
        depth = np.where(near, lo + (hi - lo) * ramp, depth)
        chip = rng.uniform(0.04, 0.09) * ppu                   # the proud course's corner, rounded off
        if lo < hi:   # the course above stands prouder: round its lower corner
            m = (d < 0) & (d > -chip)
            depth = np.where(m, np.maximum(depth, lo + (hi - lo) * (1 - np.sqrt(np.clip(-d / chip, 0, 1)))), depth)
        else:
            m = (d >= 0) & (d < chip)
            depth = np.where(m, np.maximum(depth, hi + (lo - hi) * (1 - np.sqrt(np.clip(d / chip, 0, 1)))), depth)
    return depth


def band_profile(n, ppu, rng):
    """Walk band: 0 = the outer limit (proud by BAND_REACH), 1 = the inner limit. Shallow courses with chipped corners and a
    rough edge (fractal), so the face reads as stacked, weathered blocks. Periodic over n."""
    depth = courses(n, ppu, rng, (0.12, 0.32), (0.0, 1.0), 0.05)
    # (round 4: no finer than 0.15 u, weaker: 0.07 u detail read as an even sawtooth, "torn paper")
    depth += 0.12 * fractal(n, rng, n / (0.3 * ppu), n / (0.15 * ppu), 1.4)
    return np.clip(depth, 0, 1).astype(np.float32)


def deep_profile(n, ppu, rng):
    """Below the band: bolder courses over the whole reach (a block set back, one standing out), a missing block now and
    then, and a rougher fractal edge. Periodic over n."""
    depth = courses(n, ppu, rng, (0.22, 0.6), (0.3, 1.0), 0.09)   # mostly set back (no false ledges), soft joints
    y = np.arange(n, dtype=np.float32)
    for _ in range(int(rng.integers(0, 3))):                   # a block fallen out: a deep pocket with a rough floor
        c, half = rng.uniform(0, n), rng.uniform(0.06, 0.25) * ppu
        d = wrap_dist(y, c, n)
        depth = np.maximum(depth, np.where(d < half, 0.95 - 0.25 * (d / half) ** 4, 0))
    depth += 0.1 * fractal(n, rng, n / (0.35 * ppu), n / (0.15 * ppu), 1.4)
    return np.clip(depth, 0, 1).astype(np.float32)


def cut_strip(stone, reach_px, inner_px, blend_px, depth, rng, x0=None):
    """A left-face strip (the builder mirrors it for right faces) of `stone`, reach_px + inner_px wide, its face line
    reach_px from its air side. `depth` (per row, 0..1) places the broken edge between 0 and 2 * reach_px. Shaded as a
    block turning away at its edge, with a dark rim, a few cracks and short roots (as D-114's chip_strip)."""
    w = int(round(reach_px + inner_px))
    h = len(depth)
    if x0 is None:
        x0 = int(rng.integers(0, stone.shape[1] - w))
    rows = (np.arange(h) % stone.shape[0])
    a = stone[rows][:, x0:x0 + w].copy()
    cut = depth * 2 * reach_px
    x = np.arange(w, dtype=np.float32)[None, :]
    a[..., 3] = np.clip(x - cut[:, None] + 0.5, 0, 1)
    a[..., 3] *= np.clip((w - x) / blend_px, 0, 1)
    turn = np.clip(1 - (x - cut[:, None]) / max(2.5 * reach_px, 12.0), 0, 1)
    rim = np.clip(1 - (x - cut[:, None]) / 3.0, 0, 1)
    a[..., :3] *= (1 - 0.32 * turn - 0.4 * rim)[..., None]
    # A lit lip on every ledge the profile makes (the top of a proud stone catches the light).
    step = np.diff(cut, prepend=cut[-1])
    for yy in np.nonzero(step < -2)[0]:
        xs = int(cut[yy]); xe = int(min(w, cut[yy] - step[yy] + 2))
        a[yy:yy + 2, xs:xe, :3] = a[yy:yy + 2, xs:xe, :3] * 0.5 + np.array([1.0, 0.9, 0.7], np.float32) * 0.5
    for _ in range(int(rng.integers(2, 5))):
        y0 = rng.uniform(10, h - 40)
        length, slope = rng.uniform(0.5, 1.0) * w, rng.uniform(0.3, 1.0)
        for s in np.arange(0, length, 0.5):
            yy = int(y0 + s * slope + 2 * np.sin(s / 6.0))
            if 0 <= yy < h:
                xx = int(cut[yy] + s)
                if 0 <= xx < w:
                    a[yy, xx, :3] *= 0.42
                    if xx + 1 < w: a[yy, xx + 1, :3] *= 0.72
    return a


def cut_mask(h, reach_px, inner_px, blend_px, depth, rng, ledges=True):
    """The edge strip as a mask for Env-Sprite-Lit-EdgeMask: alpha is the broken outline (as cut_strip's), rgb a grey shade
    (0.5 = the fill unchanged, so 0..2): the block turning away toward its broken edge, a dark rim along it, a few cracks, and
    (optionally) a lit lip on top of each proud stone. The stone itself is the fill's, sampled by the shader in world space."""
    w = int(round(reach_px + inner_px))
    cut = depth * 2 * reach_px
    x = np.arange(w, dtype=np.float32)[None, :]
    a = np.zeros((h, w, 4), np.float32)
    a[..., 3] = np.clip(x - cut[:, None] + 0.5, 0, 1) * np.clip((w - x) / blend_px, 0, 1)
    d = x - cut[:, None]
    turn = np.clip(1 - d / max(2.5 * reach_px, 14.0), 0, 1)
    rim = np.clip(1 - d / 3.0, 0, 1)
    # The broken face, lit from above: where the outline steps out going down (rows are top to bottom, so cut falls), the
    # stone below shows its upper face: a light band; where it steps back in, an overhang's underside: shade.
    slope = np.gradient(np.convolve(np.concatenate([cut[-4:], cut, cut[:4]]), np.ones(9) / 9, mode="same")[4:-4])
    up = np.clip(-slope / 1.5, 0, 1)[:, None] * np.clip(1 - d / 10.0, 0, 1)
    down = np.clip(slope / 1.5, 0, 1)[:, None] * np.clip(1 - d / 8.0, 0, 1)
    shade = 1 - 0.18 * turn - 0.22 * rim + (0.35 if ledges else 0.2) * up - 0.25 * down
    for _ in range(int(rng.integers(2, 5))):
        y0 = rng.uniform(10, h - 40)
        length, slope = rng.uniform(0.5, 1.0) * w, rng.uniform(0.3, 1.0)
        for s in np.arange(0, length, 0.5):
            yy = int(y0 + s * slope + 2 * np.sin(s / 6.0))
            if 0 <= yy < h:
                xx = int(cut[yy] + s)
                if 0 <= xx < w:
                    shade[yy, xx] *= 0.45
                    if xx + 1 < w: shade[yy, xx + 1] *= 0.75
    a[..., :3] = np.clip(shade * 0.5, 0, 1)[..., None]
    return a


def ragged_top_fade(a, W, rng):
    """An underside strip's top fades into the stone it hangs from along a ragged line (2026-10-17 visual review, L004:
    "every floating deck has a bottom band ... its top edge is a hard straight line"; a straight 0.12 u ramp still read as a
    seam): each column starts fading in at its own depth (0-0.14 u, fractal), over 0.16 u."""
    h, w = a.shape[:2]
    start = fractal(w, rng, max(1, int(w / W / 0.8)), max(2, int(w / W / 0.1)), 0.9)
    start = (start - start.min()) / max(1e-4, start.max() - start.min()) * 0.14 * W
    rows = np.arange(h, dtype=np.float32)[:, None]
    a[..., 3] *= np.clip((rows - start[None, :]) / (0.16 * W), 0.0, 1.0)


def lump_profile(n, px, rng, flat=TOP_FLAT):
    """Top lumps: LUMP_FLOOR..1 of px rows beyond the line, periodic over n (at the play PPU, px / TOP_LUMP). 2026-10-16
    ("All edges need to be a bit bumpy"): periodic fractal noise over wavelengths of 0.12-0.6 u, never level for long (the old
    knots-and-clip profile left plateaus up to 0.7 u wide, at its floor and at its top). `flat` is kept for callers."""
    ppu = px / TOP_LUMP if px > 0 else 1.0
    length_u = n / ppu
    v = fractal(n, rng, max(1, int(length_u / 0.6)), max(2, int(length_u / 0.12)), 0.5)
    v = (v - v.min()) / max(1e-4, v.max() - v.min())
    v = LUMP_FLOOR + (1 - LUMP_FLOOR) * v
    return (v * px).astype(np.float32)


def raise_top(strip, line_row, lump, lit=(1.0, 0.9, 0.7)):
    """Stone lumps above a top line at `line_row` (rows above it are air): each column's stone, from the line down, moves up
    by lump[x] rows (so its lit edge rides on the lump), and the rows it uncovers at the bottom keep the column's own stone.
    Nothing at or below the line loses alpha."""
    out = strip.copy()
    h = strip.shape[0]
    for x in range(strip.shape[1]):
        k = int(round(lump[x % len(lump)]))
        if k <= 0:
            continue
        top = max(0, line_row - k)
        col = strip[line_row:, x].copy()
        out[top:top + len(col), x] = np.maximum(col, 0)
        out[top:top + len(col), x, 3] = np.maximum(col[:, 3], strip[top:top + len(col), x, 3])
        out[top:top + 3, x, :3] = out[top:top + 3, x, :3] * 0.45 + np.array(lit, np.float32) * 0.55
    return out


def break_lit_edge(strip, line_row, rng, band=6):
    """The cap's lit edge (its first `band` rows under the line, painted light) varies along the strip: brighter, dimmer, thicker,
    thinner, and gone in places, so the top never reads as a ruled highlight. Periodic over the width."""
    out = strip.copy()
    w = strip.shape[1]
    k, v = periodic_knots(rng, w, (0.15 * 196.667, 0.6 * 196.667), (0.0, 1.0))
    strength = np.interp(np.arange(w, dtype=np.float32), k, v)
    thick = 2 + (np.interp(np.arange(w, dtype=np.float32), *periodic_knots(rng, w, (0.2 * 196.667, 0.7 * 196.667), (0.0, 1.0))) * 6).astype(int)
    below = strip[line_row + band + 2:line_row + 2 * band + 4]   # the stone under the painted edge
    for x in range(w):
        for r in range(band):
            y = line_row + r
            if y >= strip.shape[0]: break
            keep = strength[x] if r < thick[x] else 0.0
            stone = below[min(r, len(below) - 1), x, :3]
            out[y, x, :3] = strip[y, x, :3] * keep + stone * (1 - keep)
    return out


def lower_bottom(strip, line_row, lump):
    """raise_top upside down: lumps below a bottom line at `line_row` (rows from line_row down are air), for undersides a
    gravity-up cat stands on."""
    return raise_top(strip[::-1].copy(), strip.shape[0] - line_row, lump)[::-1].copy()


def hang_bottom(strip, line_row, depth_px, rng, wavelength_px, stone=None):
    """Stones hanging below a line at `line_row` (the underside's edge), for undersides no cat stands on: angular chunks
    (straight facets, periodic over the width) of `stone` (a lit fill tile; the strip's own rows if None), in soft shade
    toward their tips, with a dark rim. The strip above the line is unchanged."""
    h, w = strip.shape[:2]
    # 2026-10-16 ("All edges need to be a bit bumpy"): hanging stone everywhere, never level (the old knots, clipped at 0 and 1,
    # left half the underside bare and plateaus up to a unit wide): periodic fractal noise over 0.2-1.5 of the wavelength.
    v = fractal(w, rng, max(1, int(w / (1.5 * wavelength_px))), max(2, int(w / (0.2 * wavelength_px))), 0.9)
    v = (v - v.min()) / max(1e-4, v.max() - v.min())
    v = (0.15 + 0.85 * v) * depth_px
    need = int(np.ceil(v.max())) + 2
    out = np.zeros((max(h, line_row + need), w, 4), np.float32)
    out[:h] = strip
    src = stone if stone is not None else strip[:line_row]
    for x in range(w):
        k = int(round(v[x]))
        if k <= 0:
            continue
        col = src[(np.arange(k) + 37) % src.shape[0], x % src.shape[1]]
        out[line_row:line_row + k, x, :3] = col[:, :3] * np.linspace(0.95, 0.72, k, dtype=np.float32)[:, None]
        out[line_row:line_row + k, x, 3] = 1.0
        out[line_row + k - 2:line_row + k, x, :3] *= 0.7                   # the broken edge's rim
    return out


def corner_end(cap, above_px, ppu, rng, face_px, width_px):
    """A top's exposed end (left end; mirrored for right): `cap`'s first columns, the outer face line at face_px from
    the sprite's left, broken within BAND_REACH: the face's profile down the lip, and a rounded broken corner eating at
    most CORNER_IN into the top."""
    h = cap.shape[0]
    a = cap[:, :width_px].copy()
    reach = BAND_REACH * ppu
    depth = band_profile(h, ppu, rng)
    cut = face_px - reach + depth * 2 * reach                   # the broken face, within ±reach of face_px
    # The corner: a rounded chunk from the top's line, CORNER_IN wide, 0.08-0.14 u deep.
    cin, cdown = CORNER_IN * ppu, rng.uniform(0.14, 0.26) * ppu
    rows = np.arange(h, dtype=np.float32)
    below = rows - above_px
    corner = np.where((below >= -1) & (below < cdown), cin * np.sqrt(np.clip(1 - (below / cdown) ** 2, 0, 1)), 0)
    cut = np.maximum(cut, face_px + corner)
    cut[: max(0, above_px)] = np.maximum(cut[: max(0, above_px)], face_px + cin)   # lumps never stand proud at the corner
    x = np.arange(width_px, dtype=np.float32)[None, :]
    a[..., 3] *= np.clip(x - cut[:, None] + 0.5, 0, 1)
    rim = np.clip(1 - (x - cut[:, None]) / 3.0, 0, 1)
    a[..., :3] *= (1 - 0.35 * rim)[..., None]
    return a


THICK_END_FACE = 0.15     # the thick underside's end: its face line from the sprite's air side
THICK_END_REACH = 0.14    # how far its broken outline strays from the face, both ways
THICK_END_WIDTH = 0.6
THICK_END_FADE = 0.15     # the end fades into the strip over its inner side


def thick_end(strip, ppu, rng):
    """A thick underside's left end (mirrored for the right): THICK_END_WIDTH of the strip's columns, the face line
    THICK_END_FACE from the left, broken within THICK_END_REACH both ways (deep_profile, so the stone and the ivy stop
    raggedly), a dark rim along the break, and the inner THICK_END_FADE fading out over the strip it joins."""
    h = strip.shape[0]
    face, width, reach, fade = THICK_END_FACE * ppu, int(round(THICK_END_WIDTH * ppu)), THICK_END_REACH * ppu, THICK_END_FADE * ppu
    start = int(rng.integers(0, strip.shape[1] - width))
    a = strip[:, start:start + width].copy()
    cut = face - reach + deep_profile(h, ppu, rng) * 2 * reach
    x = np.arange(width, dtype=np.float32)[None, :]
    a[..., 3] *= np.clip(x - cut[:, None] + 0.5, 0, 1) * np.clip((width - x) / fade, 0, 1)
    rim = np.clip(1 - (x - cut[:, None]) / 3.0, 0, 1)
    a[..., :3] *= (1 - 0.35 * rim)[..., None]
    return a


# 2026-10-17 (the developer: "Whatever falls needs to crash and look like a pile of rubble and not like a perfect square, but
# this goes only for the ones that fall and never go back up"): a fallen block's heap, a mask for Env-Sprite-Lit-EdgeMask
# (alpha the outline, rgb a grey shade, 0.5 = the fill unchanged), so it's cut from the stone it fell from. Inside its
# footprint only (the same day: "Cat is overlapping the rubble. This should be a general rule everywhere"): its top keeps the
# block's walkable top (lumps within TOP_LUMP over it, never a dip under it), its corners are broken off and its sides cut
# inward, and it's cracked into big chunks in two layers.
TRAP_PROUD = 0.03    # a trap solid's sides stand proud of its face by at most this (its skin is a whole rectangle)
RUBBLE_SHAPES = {"Sq": (1.0, 1.0), "Wide": (2.0, 1.0), "Slab": (2.0, 0.5)}


RUBBLE_LIFT = 1.5   # the heap's brightness over its chunks' own shading (1.3 still read as a dark box)


def rubble_heap(ppu, w_u, h_u, rng):
    """A fallen block's heap filling a w_u x h_u footprint: canvas w_u x (h_u + TOP_LUMP) units, the footprint's bottom on its
    bottom row. Never outside the footprint's sides or under its top line."""
    W, H = int(round(w_u * ppu)), int(round((h_u + TOP_LUMP) * ppu))
    line = TOP_LUMP * ppu
    xs = np.arange(W, dtype=np.float32)[None, :] + 0.5
    ys = np.arange(H, dtype=np.float32)[:, None] + 0.5

    def wobble(n, amp_px, lo=1, hi=4):
        v = fractal(n, rng, lo, hi, 1.2)
        return (v / max(1e-4, np.abs(v).max())) * amp_px

    alpha = np.zeros((H, W), np.float32)
    shade = np.ones((H, W), np.float32)

    def chunk(xa, xb, ya, yb, in_l, in_r, flat_top=False, slope=0.0):
        """A broken stone between columns xa..xb and rows ya (top)..yb (bottom); at the footprint's sides (in_l / in_r: how
        far it may cut in there) its outline only goes inward; drawn over what's there with a dark rim and a lit top face."""
        rows = np.arange(H)
        left = xa + (np.abs(wobble(H, in_l)) if in_l > 0 else wobble(H, 0.02 * ppu))
        right = xb - (np.abs(wobble(H, in_r)) if in_r > 0 else wobble(H, 0.02 * ppu))
        topline = ya - (lump_profile(W, TOP_LUMP * ppu, rng) * 0.8 if flat_top else np.abs(wobble(W, 0.05 * ppu, 1, 3)) + slope * (xs[0] - (xa + xb) * 0.5))
        bottom = yb + (np.zeros(W, np.float32) if yb >= H - 1 else wobble(W, 0.04 * ppu, 1, 3))
        m = (xs >= left[:, None]) & (xs <= right[:, None]) & (ys >= topline[None, :]) & (ys <= bottom[None, :])
        for cx, dirn in ((xa, 1), (xb, -1)):   # broken corners
            r = rng.uniform(0.04, 0.08) * ppu
            inside = dirn * (xs - cx)
            m &= ~((inside < r) & (ys - ya < r) & (np.hypot(r - inside, r - (ys - ya)) > r))
        d_edge = np.minimum(np.minimum(xs - left[:, None], right[:, None] - xs), np.minimum(ys - topline[None, :], bottom[None, :] - ys))
        sh = rng.uniform(0.88, 1.1) * (1 - 0.45 * np.clip(1 - d_edge / 4.0, 0, 1))
        sh *= 1 + 0.75 * np.clip(1 - (ys - topline[None, :]) / (0.14 * ppu), 0, 1)   # each chunk's top catches the sun
        sh *= 1 - 0.18 * np.clip((ys - ya) / max(1.0, yb - ya), 0, 1)
        alpha[m] = 1.0
        shade[m] = sh[m]

    # The lower layer: from about half way down to the floor, its outer sides cut in up to DEEP_REACH (nothing stands there
    # but the floor), each stone leaning a little.
    split = rng.uniform(0.38, 0.55) * h_u * ppu + line
    n_low = 3 + int(w_u > 1.4)
    xs_low = np.concatenate([[0.0], np.sort(rng.uniform(0.25 * ppu, W - 0.25 * ppu, n_low - 1)), [float(W)]])
    for i in range(n_low):
        chunk(xs_low[i] - (0 if i == 0 else 0.04 * ppu), xs_low[i + 1] + (0 if i == n_low - 1 else 0.04 * ppu),
              split + rng.uniform(-0.06, 0.08) * ppu, H - 1,
              DEEP_REACH * ppu * rng.uniform(0.4, 0.8) if i == 0 else 0, DEEP_REACH * ppu * rng.uniform(0.4, 0.8) if i == n_low - 1 else 0,
              slope=rng.uniform(-0.18, 0.18))
    # The upper layer: the footprint's top (the walk line, lumps over it only), its outer sides cut in within BAND_REACH.
    n_top = 2 + int(w_u > 1.4)
    xs_top = np.concatenate([[0.0], np.sort(rng.uniform(0.3 * ppu, W - 0.3 * ppu, n_top - 1)), [float(W)]])
    for i in range(n_top):
        chunk(xs_top[i] - (0.02 * ppu if i else 0), xs_top[i + 1] + (0.02 * ppu if i < n_top - 1 else 0), line,
              split + rng.uniform(0.0, 0.16) * ppu,
              BAND_REACH * ppu if i == 0 else 0, BAND_REACH * ppu if i == n_top - 1 else 0, flat_top=True)
    for _ in range(int(rng.integers(2, 4))):   # a few cracks
        px, py = rng.uniform(0, W), rng.uniform(line + 6, H - 6)
        ang, length = rng.uniform(0.3, 1.3) * rng.choice([-1, 1]), rng.uniform(0.2, 0.45) * ppu
        for t in np.arange(0, length, 0.7):
            qx, qy = int(px + t * np.cos(ang) + 2 * np.sin(t / 5)), int(py + t * np.sin(ang))
            if 0 <= qx < W and 0 <= qy < H and alpha[qy, qx] > 0:
                shade[qy, qx] *= 0.45
    a = np.zeros((H, W, 4), np.float32)
    a[..., 3] = alpha
    # 2026-10-17 visual review ("the landed blocks are near-black boxes ... the darkest objects in a sunny scene"): lifted so
    # the heap is the stone it fell from, about as bright as the face it lies against (the shader doubles the mask).
    a[..., :3] = np.clip(shade * 0.5 * RUBBLE_LIFT, 0, 1)[..., None]
    return a


def build(ek, play, slots, put, fills):
    """W3's slots for the play layer. `fills`: (fill slot, suffix) pairs; each gets four band and four deep side strips
    cut from that stone."""
    W = ek.WORLD_PPU
    rng = np.random.default_rng(1801)
    lump_px = TOP_LUMP * W

    # Tops: the cap's lumps (its line is the row `above` px from the top).
    cap = play["ENV_Cap"]
    cap_above = int(round(slots["ENV_Cap"]["above"] * W))
    lumps = lump_profile(cap.shape[1], lump_px, rng)
    cap2 = raise_top(break_lit_edge(cap, cap_above, np.random.default_rng(1802)), cap_above, lumps)
    # Under a hazard: smaller lumps (HAZARD_LUMP), so the top is still broken but the blades' bases keep their air (D-108).
    flat_lumps = lump_profile(cap.shape[1], HAZARD_LUMP * W, np.random.default_rng(1803))
    cap_flat = raise_top(break_lit_edge(cap, cap_above, np.random.default_rng(1804)), cap_above, flat_lumps)
    put(play, "ENV_CapFlat", cap_flat, W, kind="strip", normal=True, above=round(cap_above / W, 4), lump=HAZARD_LUMP)
    put(play, "ENV_Cap", cap2, W, kind="strip", normal=True, above=round(cap_above / W, 4), lump=TOP_LUMP)

    # Broken corner ends for exposed tops (four variants), within BAND_REACH of the face.
    face_px, width_px = int(round(BAND_REACH * W)), int(round(0.45 * W))
    padded = np.zeros((cap2.shape[0], cap2.shape[1] + face_px, 4), np.float32)
    padded[:, face_px:] = cap2
    padded[:, :face_px] = cap2[:, -face_px:]       # the stone continues past the face (it's cut away again)
    for v in range(4):
        start = int(rng.integers(0, cap2.shape[1] - width_px - face_px))
        piece = np.concatenate([padded[:, start + face_px - face_px:start + face_px], cap2[:, start:start + width_px - face_px]], axis=1)
        end = corner_end(piece, cap_above, W, np.random.default_rng(1810 + v), face_px, width_px)
        put(play, f"ENV_CapEnd_{v}", end, W, kind="end", normal=True, above=round(cap_above / W, 4), outside=round(face_px / W, 4))

    # Undersides: ENV_Under hangs stones (free); ENV_UnderWalk keeps a whole line and lumps beyond it (flip rooms).
    under = play["ENV_Under"]
    under_line = under.shape[0] - int(round(slots["ENV_Under"]["below"] * W))
    # The walkable copy: the stone solid down to the line (the old rag filled back in), lumps beyond it.
    walk = under[:under_line].copy()
    filler = under[max(0, under_line - 40):under_line - 20]
    for x in range(walk.shape[1]):
        miss = np.nonzero(walk[:, x, 3] < 0.5)[0]
        if len(miss):
            walk[miss, x] = filler[np.arange(len(miss)) % len(filler), x]
            walk[miss, x, 3] = 1.0
            walk[miss, x, :3] *= 0.55
    walk_lumps = lump_profile(walk.shape[1], lump_px, np.random.default_rng(1820))
    canvas = np.zeros((under_line + int(np.ceil(lump_px)) + 2, walk.shape[1], 4), np.float32)
    canvas[:under_line] = walk
    canvas = lower_bottom(canvas, under_line, walk_lumps)
    ragged_top_fade(canvas, W, np.random.default_rng(1824))
    put(play, "ENV_UnderWalk", canvas, W, kind="strip", normal=True, below=round((canvas.shape[0] - under_line) / W, 4), lump=TOP_LUMP)
    hung = hang_bottom(walk, under_line, HANG * W, np.random.default_rng(1821), 0.6 * W, stone=play[fills[0][0]])
    # Its top fades into whatever it hangs from (2026-10-16: the strip's top edge drew a straight seam across the stone).
    ragged_top_fade(hung, W, np.random.default_rng(1823))
    put(play, "ENV_Under", hung, W, kind="strip", normal=True, below=round((hung.shape[0] - under_line) / W, 4))

    # Thin slabs: lumps on top; a hanging underside (free) and the flip rooms' flat one; broken, thick and plain ends.
    slab = play["ENV_Slab"]
    s_above = int(round(slots["ENV_Slab"]["above"] * W))
    body_bottom = s_above + int(round(0.5 * W))
    slab_top = raise_top(slab, s_above, lump_profile(slab.shape[1], lump_px, np.random.default_rng(1830)))
    # A flip room's slab: its underside is walkable too (an upside-down cat stands on it), so it takes lumps beyond that line
    # as well (2026-10-16: it was a ruler-straight underside).
    flip = lower_bottom(slab_top.copy(), body_bottom, lump_profile(slab.shape[1], lump_px, np.random.default_rng(1832)))
    put(play, "ENV_SlabFlip", flip, W, kind="strip", normal=True, above=round(s_above / W, 4), lump=TOP_LUMP)
    hung_slab = hang_bottom(slab_top[:body_bottom], body_bottom, 0.12 * W, np.random.default_rng(1831), 0.5 * W, stone=play[fills[0][0]])
    out = np.zeros_like(slab_top)
    out[:hung_slab.shape[0]] = hung_slab[:slab_top.shape[0]]
    keep = slab_top[body_bottom:, :, 3:4] * (out[body_bottom:, :, 3:4] < 0.5)   # drapes stay where no stone hangs
    out[body_bottom:] = out[body_bottom:] + slab_top[body_bottom:] * keep
    put(play, "ENV_Slab", out, W, kind="strip", normal=True, above=round(s_above / W, 4))
    for name, src, thick in (("ENV_SlabEnd", flip, 0.0), ("ENV_SlabEndThick", out, SLAB_THICK)):
        width_px = int(round(0.6 * W))
        piece = np.zeros((src.shape[0] + int(thick * W), width_px, 4), np.float32)
        piece[:src.shape[0]] = src[:, :width_px]
        if thick:
            # The thick end: a chunk under the slab's end, deepest at the face, tapering inward.
            stone = play[fills[0][0]]
            for x in range(width_px):
                k = int(thick * W * max(0.0, 1 - x / (0.55 * W)) ** 0.7 * (0.85 + 0.15 * np.sin(x / 7.0)))
                if k > 0:
                    col = stone[(np.arange(k) + 211) % stone.shape[0], (x + 90) % stone.shape[1]]
                    piece[body_bottom:body_bottom + k, x, :3] = col[:, :3] * np.linspace(0.8, 0.55, k, dtype=np.float32)[:, None]
                    piece[body_bottom:body_bottom + k, x, 3] = 1.0
                    piece[body_bottom + k - 1, x, :3] *= 0.5
        end = corner_end(piece, s_above, W, np.random.default_rng(1840 + int(thick * 100)), face_px, width_px)
        put(play, name, end, W, kind="end", normal=True, above=round(s_above / W, 4), outside=round(face_px / W, 4))

    # The thick undersides' broken ends (2026-10-16, "All edges need to be a bit bumpy": the strip's ends were cut straight
    # down through its stone and ivy). A left end: the strip's own columns, its face line THICK_END_FACE from the sprite's
    # left, broken within THICK_END_REACH both ways down the whole height, fading into the strip over THICK_END_FADE.
    for name in ("ENV_ThickUnder", "ENV_ThickUnderShort", "ENV_LipVines"):
        if name not in play:
            continue
        put(play, name + "End", thick_end(play[name], W, np.random.default_rng(1860 + len(name))), W, kind="end", normal=False,
            outside=THICK_END_FACE)

    # Side faces: band and deep strips for each fill, periodic down their height.
    # 2026-10-16: every side strip reaches as far into the stone as the deepest one, so a face's fill and its shadow can stop
    # at one inset (the side's deepest reach) under any of its strips.
    band_in, deep_in = (DEEP_REACH + STRIP_OVERLAP) * W, (DEEP_REACH + STRIP_OVERLAP) * W
    for fill_key, suffix in fills:
        stone = play[fill_key]
        n = stone.shape[0]
        for v in range(4):
            r = np.random.default_rng(1850 + v + 10 * len(suffix))
            # 2026-10-17: inward only (0.5..1 of the profile), so nothing stands proud where the cat stands against the face.
            put(play, f"ENV_SideChip_{suffix}_{v}", cut_mask(n, BAND_REACH * W, band_in, STRIP_BLEND * W, 0.5 + 0.5 * band_profile(n, W, r), r),
                W, kind="vstrip", normal=True, outside=BAND_REACH)
            # A moving trap's sides (its trims are plain sprites that travel with it, and its body is a whole rectangle): a
            # bumpy outline that only stands proud of the face (0..BAND_REACH), so no notch would show the body's straight edge.
            r = np.random.default_rng(1890 + v + 10 * len(suffix))
            # (driven by fractal noise, not courses: two equal courses in a row stood flat on the face for up to 0.6 u)
            proud = fractal(n, r, max(1, int(n / W / 0.6)), max(2, int(n / W / 0.08)), 0.9)
            proud = 0.08 + 0.84 * (proud - proud.min()) / max(1e-4, proud.max() - proud.min())
            put(play, f"ENV_SideIn_{suffix}_{v}", cut_strip(stone, TRAP_PROUD * W, band_in + (BAND_REACH - TRAP_PROUD) * W, STRIP_BLEND * W, 0.5 * proud, r),
                W, kind="vstrip", normal=True, outside=TRAP_PROUD)
            r = np.random.default_rng(1870 + v + 10 * len(suffix))
            put(play, f"ENV_SideDeep_{suffix}_{v}", cut_mask(n, DEEP_REACH * W, deep_in, STRIP_BLEND * W, deep_profile(n, W, r), r, ledges=False),
                W, kind="vstrip", normal=True, outside=DEEP_REACH)

    # A fallen block's heap (SolidArt, once it has landed): two of each footprint shape.
    for shape, (w_u, h_u) in RUBBLE_SHAPES.items():
        for v in range(2):
            put(play, f"ENV_RubbleHeap_{shape}_{v}", rubble_heap(W, w_u, h_u, np.random.default_rng(1900 + 10 * v + len(shape))),
                W, kind="end", normal=True, outside=0.0, above=TOP_LUMP)
