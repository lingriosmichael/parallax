"""PAX-A16 gauntlet (the developer's brief, 2026-10-01): the kit rebuilt from the triage's keepers. Called by env_kit.build()
before the depth tiers, so the tiers (cap wash, thick underside) derive from these slots.

- The play layer: the painterly A_GAME stone at native resolution, replacing the cartoon ENV-10/11/12/14 tiles (rejected
  in the triage: mosaic artefacts, a different style, and they only work tiled). Fills are 6 u wide before they repeat, and
  the lip, undersides, faces, slabs and posts are cut from the same stone, so every solid reads as one material.
- The layers: every background element is a whole piece at LAYER_PPU (128 px/u, finer than a 20:9 phone's 122 at the 1.8x
  camera), never a tiled band: the composer places pieces, it never repeats one across the view.
- Skies: A_BG_00 and ENV-01 gradient-mapped to each palette (a time of day is a different sky, not an orange one tinted).
- Neutral clouds: ENV-03 a/b/c with their colour pulled to warm white, so a palette's tint sets their colour.
"""
import numpy as np

LAYER_PPU = 128.0

# Each palette's sky ramp, dark to light (a sky's shadowed cloud bellies, its mid sky, its lit cloud rims).
SKY_RAMPS = {
    "Gold":     [(0.62, 0.40, 0.30), (0.96, 0.72, 0.45), (1.00, 0.93, 0.74)],
    "Mist":     [(0.58, 0.56, 0.66), (0.90, 0.82, 0.80), (1.00, 0.96, 0.90)],
    "Jade":     [(0.36, 0.46, 0.38), (0.78, 0.82, 0.62), (0.98, 0.96, 0.80)],
    "Noon":     [(0.46, 0.60, 0.78), (0.80, 0.88, 0.94), (1.00, 0.98, 0.92)],
    "Rose":     [(0.46, 0.30, 0.44), (0.92, 0.58, 0.58), (1.00, 0.86, 0.74)],
    "Storm":    [(0.22, 0.22, 0.28), (0.52, 0.48, 0.48), (0.98, 0.78, 0.52)],
    "Dusk":     [(0.20, 0.18, 0.36), (0.56, 0.40, 0.58), (1.00, 0.70, 0.48)],
    "Dawn":     [(0.34, 0.44, 0.66), (0.78, 0.74, 0.82), (1.00, 0.84, 0.64)],
    "Ember":    [(0.40, 0.16, 0.14), (0.90, 0.42, 0.22), (1.00, 0.80, 0.50)],
    "Moon":     [(0.06, 0.08, 0.16), (0.22, 0.30, 0.48), (0.74, 0.82, 0.94)],
}


def key_magenta(a):
    """The A_* stills are painted on magenta: red and blue high, green low becomes clear (with a soft edge)."""
    out = a.copy()
    rgb = out[..., :3]
    key = np.clip((np.minimum(rgb[..., 0], rgb[..., 2]) - rgb[..., 1] - 0.35) / 0.25, 0, 1)
    out[..., 3] = np.minimum(out[..., 3], 1 - key)
    # Despill everywhere: any magenta cast (red and blue both above green) is pulled down to green's level. Warm golds
    # (red > green > blue) are untouched.
    spill = np.clip(np.minimum(rgb[..., 0], rgb[..., 2]) - rgb[..., 1], 0, 1)
    out[..., 0] -= spill
    out[..., 2] -= spill
    out[..., :3] = np.where(out[..., 3:4] > 0.02, np.clip(out[..., :3], 0, 1), 0)
    return out


def gradient_map(a, ramp, keep=0.18):
    """The sky's luminance through a three-colour ramp, with a little of its own colour kept for detail."""
    lum = (a[..., :3] * np.array([0.3, 0.55, 0.15], np.float32)).sum(-1, keepdims=True)
    lo, mid, hi = (np.array(c, np.float32) for c in ramp)
    l = np.clip((lum - lum.min()) / max(1e-4, lum.max() - lum.min()), 0, 1)
    low = lo + (mid - lo) * np.clip(l / 0.55, 0, 1)
    out = np.where(l < 0.55, low, mid + (hi - mid) * np.clip((l - 0.55) / 0.45, 0, 1))
    res = a.copy()
    res[..., :3] = out * (1 - keep) + a[..., :3] * keep * (out.mean(-1, keepdims=True) / np.maximum(a[..., :3].mean(-1, keepdims=True), 1e-3))
    res[..., :3] = np.clip(res[..., :3], 0, 1)
    return res


def neutral(a, warmth=0.08):
    """Colour pulled to its luminance with a hint of warmth: a tint then sets the palette's colour."""
    out = a.copy()
    lum = (out[..., :3] * np.array([0.3, 0.55, 0.15], np.float32)).sum(-1, keepdims=True)
    out[..., :3] = np.clip(lum * np.array([1 + warmth, 1.0, 1 - warmth], np.float32) * 1.08, 0, 1)
    return out


def feather_sides(a, fraction=0.22):
    """A piece painted to tile (it runs into the image's side edges) ends in a hard vertical cut when placed whole: its
    left and right ends fade out instead (eased), so it sits in the sky like a cloud bank, not a pasted rectangle."""
    out = a.copy()
    w = out.shape[1]
    k = max(2, int(w * fraction))
    ramp = np.ones(w, np.float32)
    t = np.linspace(0, 1, k, dtype=np.float32)
    ramp[:k] = t * t * (3 - 2 * t)
    ramp[w - k:] = ramp[:k][::-1]
    out[..., 3] *= ramp[None, :]
    return out


def rows_gain(h, top, bottom, power=1.0):
    t = np.linspace(0, 1, h, dtype=np.float32) ** power
    return (top + (bottom - top) * t)[:, None, None]


def stamp_strip(ek, place, canvas, pieces, rng, base_y, height_range, spacing, sink=0.35):
    """Stamps clumps along a strip: random piece, height and gap, bottoms at base_y (sunk by `sink` of their height). Each
    stamp is placed again one strip-width to the left, so a seamless strip stays seamless."""
    x, n = int(rng.uniform(0, 30)), canvas.shape[1]
    while x < n:
        p = pieces[rng.integers(len(pieces))]
        h = int(rng.uniform(*height_range))
        q = ek.scale(p, h / p.shape[0])
        if rng.random() < 0.5:
            q = q[:, ::-1]
        y = int(base_y - h * (1 - sink))
        place(canvas, q, x, y)
        place(canvas, q, x - n, y)
        x += int(q.shape[1] * rng.uniform(0.45, 0.9) + rng.uniform(*spacing))


def sharpen(a, radius=1.4, amount=0.55):
    """Unsharp mask on colour (alpha kept): the painted stone is drawn at about 60% of its size on a phone, which softened it."""
    from PIL import ImageFilter, Image
    out = a.copy()
    for ch in range(3):
        im = Image.fromarray((np.clip(a[..., ch], 0, 1) * 255).astype(np.uint8), "L")
        blurred = np.asarray(im.filter(ImageFilter.GaussianBlur(radius))).astype(np.float32) / 255
        out[..., ch] = np.clip(a[..., ch] + (a[..., ch] - blurred) * amount, 0, 1)
    return out


def tame_lichen(a, amount=0.55):
    """The fill's bright yellow lichen spots, which repeat with the tile, pulled toward the stone's grey."""
    out = a.copy()
    rgb = out[..., :3]
    lum = (rgb * np.array([0.3, 0.55, 0.15], np.float32)).sum(-1, keepdims=True)
    yellow = np.clip(((rgb[..., 0:1] + rgb[..., 1:2]) * 0.5 - rgb[..., 2:3] - 0.12) / 0.15, 0, 1) * np.clip((lum - 0.38) / 0.15, 0, 1)
    out[..., :3] = rgb * (1 - yellow * amount) + (lum * np.array([1.0, 0.97, 0.9], np.float32) * 0.85) * yellow * amount
    return out


def build(ek, load, put, play, layers):
    import env_tiers as et
    W = ek.WORLD_PPU
    rng = np.random.default_rng(1601)
    fill = ek.tp.load_rgba(ek.SRC / "A_GAME_Platform_Fill__raw_v1.png")
    wall = ek.tp.load_rgba(ek.SRC / "A_GAME_Wall__raw_v1.png")

    # ---------- fills: native, seamless both ways, ~5.9 u before they repeat ----------
    def seamless(a):
        return ek.crossfade(ek.crossfade(a, 100, 1), 100, 0)
    fa, fw = seamless(sharpen(tame_lichen(fill))), seamless(sharpen(tame_lichen(wall)))
    # P1-R4: the two stones blended by a soft seamless noise mask over a 2x2 tile (no quadrant seams), ~11.7 u before the
    # pattern comes round again.
    def quad(a, b):
        A = np.concatenate([np.concatenate([a, a], axis=1)] * 2, axis=0)
        # Offset, never mirrored (a mirror made symmetric "Rorschach" patterns in the wall).
        bb = np.roll(np.roll(b, b.shape[0] // 3, axis=0), b.shape[1] // 2, axis=1)
        B = np.concatenate([np.concatenate([bb, bb], axis=1)] * 2, axis=0)
        n = A.shape[0]
        yy, xx = np.mgrid[0:n, 0:n].astype(np.float32) * (2 * np.pi / n)
        noise = (np.sin(xx * 1 + 0.7) * np.cos(yy * 2 + 1.1) + 0.6 * np.sin(xx * 3 + yy * 2 + 2.3) + 0.4 * np.cos(xx * 5 - yy * 3 + 0.4)) / 2.0
        m = np.clip(noise * 1.6 + 0.5, 0, 1)[..., None]
        m = m * m * (3 - 2 * m)
        return A * (1 - m) + B * m
    put(play, "ENV_Fill_A", quad(fa, fw), W, kind="fill", normal=True)
    put(play, "ENV_Fill_A2", quad(fw, fa), W, kind="fill", normal=True)
    put(play, "ENV_Fill_A3", quad(fa[:, ::-1], fw) * np.array([0.92, 0.95, 0.9, 1.0], np.float32), W, kind="fill", normal=True)

    moss = load("ENV-21")
    clumps = [moss[b[1]:b[3], b[0]:b[2]] for b in ek.split_pieces(moss)[:3]]
    drapes_sheet = load("ENV-20")
    drapes = [drapes_sheet[b[1]:b[3], b[0]:b[2]] for b in ek.split_pieces(drapes_sheet)[:3]]

    # A long stone band from two different rows of the fill: ~12 u before it repeats.
    def band(src, h, rows):
        parts = [src[r:r + h] for r in rows]
        return ek.crossfade(np.concatenate(parts, axis=1), 120, 1)

    # ---------- the lip: a stone course, its sunlit edge, grass and moss along the top ----------
    body_h = round(0.42 * W)
    lip = band(fa, body_h, (180, 620))
    lip[..., :3] *= rows_gain(body_h, 1.75, 0.95, 0.6) * np.array([1.06, 1.0, 0.9], np.float32)   # the lip catches the sun
    # P1-R4: the lip's lower edge fades into the wall below (no straight coping line).
    fade_rows = round(body_h * 0.45)
    lip[body_h - fade_rows:, :, 3] *= np.linspace(1, 0, fade_rows, dtype=np.float32)[:, None] ** 1.3
    lip[:6, :, :3] = lip[:6, :, :3] * np.linspace(0.4, 1.0, 6, dtype=np.float32)[:, None, None] + np.array([1.0, 0.92, 0.74], np.float32) * np.linspace(0.6, 0.0, 6, dtype=np.float32)[:, None, None]   # the lit edge
    above = round(0.26 * W)
    cap = np.zeros((above + body_h, lip.shape[1], 4), np.float32)
    et.place(cap, lip, 0, above)
    # Round P1-R3: no tufts baked in (a strip repeats them every period); the builder places tufts one by one.
    pad = 140
    put(play, "ENV_Cap", cap, W, kind="strip", normal=True, above=round(above / W, 4))

    # ---------- the underside: a darker course, a cornice line, short ivy below ----------
    course_h = round(0.3 * W)
    under_body = band(fa, course_h, (900, 400))
    under_body[..., :3] *= rows_gain(course_h, 0.8, 0.55)
    under_body[-5:, :, :3] = under_body[-5:, :, :3] * 0.5 + np.array([0.86, 0.72, 0.55], np.float32) * 0.25   # the cornice lip
    # Round 9: a broken lower edge (seamless along the strip), never a ruler line.
    nx = under_body.shape[1]
    xx = np.arange(nx, dtype=np.float32) * (2 * np.pi / nx)
    rag = (12 + 7 * np.sin(xx * 6 + 0.2) + 4 * np.sin(xx * 13 + 0.9) + 2 * np.sin(xx * 29 + 2.1)).astype(int)
    rows = np.arange(course_h)[:, None]
    under_body[..., 3] *= (rows < (course_h - rag[None, :])).astype(np.float32) * 0.9 + 0.1 * (rows < course_h - rag[None, :] + 3)
    hang = 2   # gauntlet: drapes are placed by the builder; nothing hangs in the strip
    under = np.zeros((course_h + hang, under_body.shape[1], 4), np.float32)
    et.place(under, under_body, 0, 0)
    # Round P1-R3: no drapes baked in; the builder hangs them one by one.
    under = ek.crossfade(np.concatenate([under, under[:, :pad]], axis=1), pad, 1)
    put(play, "ENV_Under", under, W, kind="strip", normal=True, below=round(hang / W, 4))

    # ---------- the wall face: the wall stone, a lit outer edge fading inward ----------
    side_w = round(0.42 * W)
    side = ek.crossfade(np.concatenate([fw[:, 200:200 + side_w], fw[:, 700:700 + side_w]], axis=0), 120, 0)
    ramp = np.linspace(1.15, 0.92, side_w, dtype=np.float32)   # gentle: a steep ramp read as a darker inner block
    side[..., :3] *= ramp[None, :, None]
    side[:, :3, :3] = side[:, :3, :3] * 0.5 + np.array([1.0, 0.88, 0.68], np.float32) * 0.5
    # P1-R5: a ragged outer edge (chipped stone), not a ruler-straight cut; seamless down its length.
    n = side.shape[0]
    yy = np.arange(n, dtype=np.float32) * (2 * np.pi / n)
    # Broken stone, not a saw: a few long irregular bulges (low frequencies only), a soft 5 px edge.
    jag = 9 + 6 * np.sin(yy * 5 + 0.3) + 4 * np.sin(yy * 11 + 1.7) + 2.5 * np.sin(yy * 23 + 0.4)
    xx = np.arange(side_w, dtype=np.float32)[None, :]
    side[..., 3] *= np.clip((xx - jag[:, None]) / 2.0, 0, 1)
    # Round 11: the sunlit rim follows the broken edge (6 px inside it), so a block's end reads as a lit corner turning away.
    d = xx - jag[:, None]
    rim = np.clip(1 - np.abs(d - 3) / 4.0, 0, 1)[..., None] * 0.65
    side[..., :3] = side[..., :3] * (1 - rim) + np.array([1.0, 0.86, 0.6], np.float32) * rim
    put(play, "ENV_Side", side, W, kind="vstrip", normal=True, outside=round(float(jag.max()) / W, 4))

    # ---------- the slab: 0.5 u of stone, lit edge and grass on top, a shadowed lower edge, a few drapes ----------
    slab_body = band(fa, round(0.5 * W), (300, 760))
    sb = slab_body.shape[0]
    slab_body[..., :3] *= rows_gain(sb, 1.25, 0.6, 0.8)
    slab_body[:4, :, :3] = slab_body[:4, :, :3] * 0.4 + np.array([1.0, 0.9, 0.7], np.float32) * 0.6
    s_above, s_below = round(0.22 * W), round(0.35 * W)
    slab = np.zeros((s_above + sb + s_below, slab_body.shape[1], 4), np.float32)
    x = 0
    while x < slab.shape[1]:
        d = drapes[rng.integers(3)]
        h = int(rng.uniform(0.15, 0.35) * W)
        et.place(slab, ek.scale(d, h / d.shape[0]), x, s_above + sb - 10)
        x += int(rng.uniform(120, 300))
    et.place(slab, slab_body, 0, s_above)
    stamp_strip(ek, et.place, slab, clumps, rng, s_above + 6, (0.12 * W, 0.24 * W), (30, 140))
    slab = ek.crossfade(np.concatenate([slab, slab[:, :pad]], axis=1), pad, 1)
    put(play, "ENV_Slab", slab, W, kind="strip", normal=True, above=round(s_above / W, 4))
    # Its broken end: the slab's first 0.6 u with a ragged, rounded outer edge.
    end_w = round(0.6 * W)
    end = slab[:, :end_w].copy()
    yy, xx = np.mgrid[0:end.shape[0], 0:end_w].astype(np.float32)
    jag = (np.sin(yy * 0.21) * 6 + np.sin(yy * 0.057 + 1.3) * 10 + 18)
    end[..., 3] *= np.clip((xx - jag) / 6.0, 0, 1)
    end[..., :3] *= np.clip(0.7 + (xx - jag) / 60.0, 0.7, 1.0)[..., None]
    put(play, "ENV_SlabEnd", end, W, kind="end", normal=True, above=round(s_above / W, 4))

    # ---------- posts: stacked wall stone, rounded by shading ----------
    def post(width_u):
        w = round(width_u * W)
        col = ek.crossfade(np.concatenate([fw[:, 300:300 + w], fw[:, 820:820 + w]], axis=0), 120, 0)
        u = np.linspace(-1, 1, w, dtype=np.float32)
        shade = 0.55 + 0.6 * np.sqrt(np.clip(1 - u * u, 0, 1)) + 0.12 * u   # lit a little from the right
        col[..., :3] *= shade[None, :, None]
        return col
    put(play, "ENV_Post", post(1.0), W, kind="vstrip", normal=True)
    put(play, "ENV_SlimPost", post(0.5), W, kind="vstrip", normal=True)
    for key, wu, hu in (("ENV_PostCap", 1.5, 0.42), ("ENV_PostBase", 1.25, 0.38)):
        w, h = round(wu * W), round(hu * W)
        blk = fa[500:500 + h, 100:100 + w].copy()
        blk[..., :3] *= rows_gain(h, 1.25, 0.65)
        blk[:3, :, :3] = blk[:3, :, :3] * 0.4 + np.array([1.0, 0.9, 0.7], np.float32) * 0.6
        if key == "ENV_PostCap":
            cv = np.zeros((h + round(0.18 * W), w, 4), np.float32)
            et.place(cv, blk, 0, cv.shape[0] - h)
            stamp_strip(ek, et.place, cv, clumps, rng, cv.shape[0] - h + 6, (0.1 * W, 0.18 * W), (20, 80))
            blk = cv
        put(play, key, blk, W, kind="object", normal=True)

    # ---------- foot ferns (the developer: front foliage fixed in the world, not blurry): ENV-26's fern band, sharp, cut
    # into three clumps at its emptiest columns, at the play layer's PPU, a little into shade (near the camera, low) ----------
    fg = load("ENV-26")
    top_piece = ek.split_pieces(fg, threshold=0.3, gap=4)[0]
    band = fg[ek.FG_CUT:top_piece[3]]
    cols = band[..., 3].mean(axis=0)
    w = band.shape[1]
    cuts = [int(np.argmin(cols[int(w * f) - 60:int(w * f) + 60]) + int(w * f) - 60) for f in (1 / 3, 2 / 3)]
    for i, (x0, x1) in enumerate(zip([0] + cuts, cuts + [w])):
        piece = ek.tp.trim(band[:, x0:x1], 0.05)
        piece[..., :3] *= np.array([0.78, 0.82, 0.74], np.float32)
        # The band's lower half is a solid mass of dark leaves cut square at the bottom and the sides (it read as a dark box
        # on the wall): it fades out downward into the stone, and its sides fade only where it is dense, so the leaves on
        # top keep their crisp silhouettes.
        ph, pw = piece.shape[:2]
        v = np.linspace(0, 1, ph, dtype=np.float32)[:, None]
        def ease(t0, t1, t):
            t = np.clip((t - t0) / (t1 - t0), 0, 1)
            return t * t * (3 - 2 * t)
        sides = feather_sides(np.ones((1, pw, 4), np.float32), 0.2)[..., 3]
        piece[..., 3] *= (1 - ease(0.5, 0.97, v)) * (1 - ease(0.3, 0.75, v) * (1 - sides))
        piece = ek.tp.trim(piece, 0.05)
        put(play, f"ENV_Fern_{i}", ek.fit_width(piece, round(1.5 * W)), W, kind="dressing")

    # ---------- three more ivy shapes: the drapes' halves (round 9: the same three curtains read as repeats) ----------
    k = 0
    for d in drapes:
        half = d.shape[1] // 2
        for part in (d[:, :half], d[:, half:]):
            if k < 3:
                put(play, f"ENV_Drape_{3 + k}", ek.fit_width(ek.tp.trim(part, 0.05), round(0.55 * W)), W, kind="dressing")
            k += 1

    # ---------- layers: whole pieces at LAYER_PPU ----------
    L = LAYER_PPU
    mg = key_magenta(ek.tp.load_rgba(ek.SRC / "A_MG_01_Mid__raw_v1.png"))
    put(layers, "ENV_MidRuins", ek.soft_base(ek.tp.trim(mg, 0.05), 0.1), L, kind="piece")
    far = key_magenta(ek.tp.load_rgba(ek.SRC / "A_BG_01_Far__raw_v1.png"))
    put(layers, "ENV_FarCluster", ek.tp.trim(far, 0.05), L, kind="piece")
    for i, src in enumerate(("ENV-03c", "ENV-03b", "ENV-03a")):
        a = load(src)
        rows = np.nonzero((a[..., 3] > 0.02).any(axis=1))[0]
        put(layers, f"ENV_Cloud_{i}", feather_sides(neutral(a[max(0, rows[0] - 8):rows[-1] + 8])), L, kind="piece")
    sky_a = ek.tp.load_rgba(ek.SRC / "A_BG_00_Sky__raw_v1.png")
    sky_b = load("ENV-01")
    for name, ramp in SKY_RAMPS.items():
        src = sky_b if name in ("Mist", "Noon", "Dawn", "Moon") else sky_a
        put(layers, f"ENV_Sky_{name}", gradient_map(src, ramp), L, kind="sky")
    bw = load("ENV-32")
    rows = np.nonzero((bw[..., 3] > 0.02).any(axis=1))[0]
    put(layers, "ENV_RuinWall", feather_sides(bw[rows[0]:rows[-1] + 1], 0.08), L, kind="piece")


def edge_fade(a, px=14, sides="tblr"):
    """A piece's own border fades out over `px`, so a glow or haze painted into its edge never ends in a line."""
    out = a.copy()
    h, w = out.shape[:2]
    def ramp(n):
        r = np.ones(n, np.float32)
        k = min(px, n // 3)
        t = np.linspace(0, 1, k, dtype=np.float32)
        r[:k] = t * t * (3 - 2 * t)
        r[n - k:] = r[:k][::-1]
        return r
    rh, rw = ramp(h), ramp(w)
    if "t" not in sides: rh[:len(rh) // 2] = 1
    if "b" not in sides: rh[len(rh) // 2:] = 1
    if "l" not in sides: rw[:len(rw) // 2] = 1
    if "r" not in sides: rw[len(rw) // 2:] = 1
    out[..., 3] *= rh[:, None] * rw[None, :]
    return out


def finish(layers, slots):
    """After every slot exists: background pieces and foreground get their border fade."""
    for name, a in list(layers.items()):
        kind = slots[name].get("kind")
        if kind in ("piece", "foreground"):
            # A foreground piece is cropped where it leaves the view: a long fade, so its cut edge never shows.
            # The frame corner runs off the view's bottom and outer side: only its top and inner edges fade.
            layers[name] = edge_fade(a, 90, "tr") if name in ("ENV_FrameL", "ENV_FrameR") else edge_fade(a, 90 if kind == "foreground" else 14)
