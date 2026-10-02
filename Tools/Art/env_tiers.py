"""PAX-A16: the depth-tier slots, made from the same ENV stills as env_kit.py (no new paintings). Called by env_kit.build();
`ek` is env_kit itself (its helpers), `put` stores a slot.

- ENV_CapWash  the walkable cap with a warm light wash fading over 1 u below it: the lit lip over the dark body (§3.1).
- ENV_SkyPlate ENV-01's painted sky, the farthest tier (§3.4 tier 0).
- ENV_FarCity  a seamless band of distant ruins from ENV-05, ENV-07a and ENV-07d, hazed and softened (tier 2).
- ENV_FrameL/R the near-black foreground corners: ENV-26's ferns and roots and ENV-26b's trunk, rim-lit, blurred (tier 7).
- ENV_FrameTop a seamless curtain of dark hanging vines from ENV-20 for the top of the frame (tier 7).
- ENV_Chain    a rusted iron chain, tiling downward (a slab's hanger, §3.3), drawn here.
- ENV_Mote     a soft dot for the dust particles (§3.5).
- ENV_FaceShade(+Solid) the one warm shadow on every block's face below its lip (round 3).
- ENV_Ray      a soft vertical light shaft for the additive god rays (§3.4), from ENV-28's beam.
"""
import numpy as np


def wash(width, height, colour, top_alpha):
    """A colour fading from `top_alpha` at the top to clear at the bottom (eased)."""
    t = np.linspace(0, 1, height, dtype=np.float32)
    alpha = top_alpha * (1 - t) ** 2.2
    out = np.zeros((height, width, 4), np.float32)
    out[..., :3] = np.array(colour, np.float32)
    out[..., 3] = alpha[:, None]
    return out


def silhouette(ek, a, value=0.07, rim=(1.0, 0.68, 0.32), rim_px=5, rim_alpha=0.85):
    """A piece pushed nearly black, with a warm rim on its upper and outer edges (the backlight)."""
    out = a.copy()
    lum = out[..., :3].mean(axis=-1, keepdims=True)
    out[..., :3] = np.array([0.11, 0.07, 0.045], np.float32) * (0.6 + 0.8 * lum) * (value / 0.07)
    # Rim: where the alpha drops within rim_px above the pixel (light from above and behind).
    alpha = out[..., 3]
    shifted = np.zeros_like(alpha)
    shifted[rim_px:] = alpha[:-rim_px]
    edge = np.clip(alpha - shifted, 0, 1) * rim_alpha
    out[..., :3] = out[..., :3] * (1 - edge[..., None]) + np.array(rim, np.float32) * edge[..., None]
    return out


def place(canvas, piece, x, y):
    """Alpha-composites `piece` onto `canvas` with its top-left at (x, y), clipped."""
    h, w = piece.shape[:2]
    H, W = canvas.shape[:2]
    x0, y0, x1, y1 = max(0, x), max(0, y), min(W, x + w), min(H, y + h)
    if x1 <= x0 or y1 <= y0:
        return
    src = piece[y0 - y:y1 - y, x0 - x:x1 - x]
    dst = canvas[y0:y1, x0:x1]
    a = src[..., 3:4]
    out_a = a + dst[..., 3:4] * (1 - a)
    rgb = (src[..., :3] * a + dst[..., :3] * dst[..., 3:4] * (1 - a)) / np.maximum(out_a, 1e-4)
    canvas[y0:y1, x0:x1] = np.dstack([np.where(out_a > 1e-4, rgb, 0), out_a])


def chain(width=26, link=40, links=8):
    """Alternating oval links (face-on, then edge-on), dark iron with a warm highlight; tiles downward."""
    h = link * links
    out = np.zeros((h, width, 4), np.float32)
    yy, xx = np.mgrid[0:h, 0:width].astype(np.float32)
    cx = width / 2 - 0.5
    for i in range(links):
        cy = i * link + link / 2
        dy = (yy - cy + h / 2) % h - h / 2   # wrapped, so the links overlapping the ends tile seamlessly
        if i % 2 == 0:
            rx, ry, thick = width * 0.42, link * 0.62, 4.2
            d = np.sqrt(((xx - cx) / rx) ** 2 + (dy / ry) ** 2)
            ring = np.clip(1 - np.abs(d - 0.78) * (rx / thick), 0, 1)
        else:
            ring = np.clip(1 - np.abs(xx - cx) / 2.6, 0, 1) * (np.abs(dy) < link * 0.62)
        ring = np.clip(ring * 1.6, 0, 1)   # solid iron at the link's core
        light = np.clip(1 - (xx - cx + 3) / width, 0.35, 1)
        a = np.maximum(out[..., 3], ring)
        col = np.array([0.24, 0.16, 0.11], np.float32)[None, None] * light[..., None] * 1.35
        out[..., :3] = np.where(ring[..., None] > out[..., 3:4], col, out[..., :3])
        out[..., 3] = a
    return out


def build(ek, load, put, play, layers):
    W, H = ek.WORLD_PPU, ek.HALF_PPU

    # The lit lip: ENV_Cap's strip with a warm wash below it, 1 u deep (the builder uses it on blocks tall enough).
    cap = play["ENV_Cap"]
    depth = round(0.7 * W)
    body = wash(cap.shape[1], depth, (0.98, 0.80, 0.55), 0.5)
    capwash = np.concatenate([cap, body], axis=0)
    above = round(34 * ek.EDGE_SCALE / W, 4)
    put(play, "ENV_CapWash", capwash, W, kind="strip", normal=False, above=above)

    # Tier 0: the painted sky, whole.
    put(layers, "ENV_SkyPlate", load("ENV-01"), H, kind="sky")

    # Tier 2: the far city, a seamless band of hazed ruins, 3200 px wide, skyline in the lower 60%.
    band = np.zeros((760, 3200 + 300, 4), np.float32)
    pieces = []
    for src, n in (("ENV-05", 4), ("ENV-07d", 2), ("ENV-07a", 2)):
        sheet = load(src)
        for box in sorted(ek.split_pieces(sheet, threshold=0.3)[:n], key=lambda b: b[0]):
            pieces.append(sheet[box[1]:box[3], box[0]:box[2]])
    order = [0, 4, 1, 6, 2, 5, 3, 7]
    heights = [620, 520, 700, 460, 640, 560, 600, 480, 560, 500, 660]
    x, k = -60, 0
    while x < band.shape[1]:
        i, hgt = order[k % len(order)], heights[k % len(heights)]
        p = ek.scale(pieces[i], hgt / pieces[i].shape[0])
        if k % 3 == 1:
            p = p[:, ::-1]
        place(band, p, x, band.shape[0] - p.shape[0] - 40 + (k % 2) * 30)
        x += int(p.shape[1] * 0.62)
        k += 1
    band = ek.crossfade(band, 300, 1)
    band = ek.haze(band, (0.98, 0.88, 0.70), 0.52)
    # The base sinks into the haze: alpha fades over the lowest 30%.
    fade = np.clip((band.shape[0] - np.arange(band.shape[0], dtype=np.float32)) / (band.shape[0] * 0.3), 0, 1) ** 1.5
    band[..., 3] *= fade[:, None]
    put(layers, "ENV_FarCity", ek.blur(band, 1.5), H, kind="band")

    # Tier 7: the frame corners (bottom-left; the builder mirrors it for the right) and the top curtain.
    fg = load("ENV-26")
    top, roots = ek.split_pieces(fg, threshold=0.3, gap=4)[:2]
    ferns = fg[ek.FG_CUT:top[3], 0:fg.shape[1]]
    root = fg[roots[1]:roots[3], roots[0]:roots[2]]
    trunk = ek.tp.trim(load("ENV-26b"), 0.05)
    corner = np.zeros((1100, 1300, 4), np.float32)
    # Gauntlet: no trunk (blurred at the view's edge it read as a pale strip, a gap beside the floor).
    place(corner, ek.scale(root, 1.1), 120, 1100 - int(root.shape[0] * 1.1) + 60)
    place(corner, ek.scale(ferns, 1.15), -40, 1100 - int(ferns.shape[0] * 1.15) + 40)
    # Gauntlet: out-of-focus foliage near the lens, as in the concept: its own colours deep in shade with a warm rim, and a
    # strong blur (not a black cut-out).
    corner = ek.tp.trim(corner, 0.02)
    rim = silhouette(ek, corner, value=0.07, rim=(1.0, 0.72, 0.4), rim_px=10, rim_alpha=0.9)
    lit = corner.copy()
    lit[..., :3] = lit[..., :3] * np.array([0.6, 0.6, 0.5], np.float32)   # deep warm shade, with enough value to keep its form
    corner = lit
    # P1-R5: no rim (an even glowing outline read as a sticker); the leaves keep their own values.
    put(layers, "ENV_FrameL", ek.blur(ek.scale(corner, 0.75), 5.0), H, kind="foreground")
    # The right corner (drawn mirrored): roots in front, ferns behind, no trunk, a lower, wider mass.
    cr = np.zeros((900, 1400, 4), np.float32)
    place(cr, ek.scale(ferns[:, ::-1], 1.35), 0, 900 - int(ferns.shape[0] * 1.35) + 60)
    place(cr, ek.scale(root[:, ::-1], 1.45), 380, 900 - int(root.shape[0] * 1.45) + 90)
    cr = ek.tp.trim(cr, 0.02)
    cr[..., :3] = cr[..., :3] * np.array([0.58, 0.56, 0.46], np.float32)
    put(layers, "ENV_FrameR", ek.blur(ek.scale(cr, 0.5), 4.0), H, kind="foreground")

    drapes = load("ENV-20")
    dboxes = ek.split_pieces(drapes)[:3]
    curtain = np.zeros((560, 2400 + 260, 4), np.float32)
    xs = [-40, 260, 520, 880, 1150, 1500, 1790, 2120, 2420]
    for k, xv in enumerate(xs):
        b = dboxes[k % 3]
        d = drapes[b[1]:b[3], b[0]:b[2]]
        d = ek.scale(d, (0.85 + 0.25 * ((k * 7) % 3) / 2) * 520 / d.shape[0] * (0.7 if k % 2 else 1.0))
        place(curtain, d, xv, -10)
    # A ragged dark ledge of leaves along the very top joins the drapes.
    curtain[:28, :, :3] = np.array([0.1, 0.07, 0.04], np.float32)
    curtain[:28, :, 3] = 1.0
    curtain = ek.crossfade(curtain, 260, 1)
    curtain = silhouette(ek, curtain, value=0.09, rim_alpha=0.6)
    put(layers, "ENV_FrameTop", ek.blur(curtain, 3.0), H, kind="foreground")

    # Round 3 ("nowhere close to the concept"): the concept's framing. ENV_FrameHang: A_FG_01's roots, vines and banner
    # hanging from the view's top corner (keyed off its magenta), darkened with a warm rim, blurred (it's near the lens).
    # ENV_FramePillar: ENV-14a's column, huge and near-black, for the view's left edge (the concept's left pillar).
    hang = ek.tp.load_rgba(ek.SRC / "A_FG_01.png")
    rgb = hang[..., :3]
    key = np.clip((np.minimum(rgb[..., 0], rgb[..., 2]) - rgb[..., 1] - 0.35) / 0.25, 0, 1)   # magenta: red and blue high, green low
    hang[..., 3] = 1 - key
    hang[..., :3] = np.where(key[..., None] > 0.5, 0, rgb)
    hang = ek.tp.trim(hang, 0.05)
    # Its left half (the roots) only: the right half repeats it with the banner, which the builder adds as its own piece.
    banner = hang[:, hang.shape[1] // 2:]
    roots = hang[:, :hang.shape[1] // 2]
    roots = silhouette(ek, roots, value=0.1, rim_alpha=0.7)
    roots[:10, :, :3] = roots[10:11, :, :3].mean(axis=1, keepdims=True) * 0 + np.array([0.11, 0.07, 0.045], np.float32)   # no rim along the cut top
    put(layers, "ENV_FrameHang", ek.blur(roots, 2.5), H, kind="foreground")
    keep = banner.copy()
    keep[..., :3] *= 0.55   # the banner keeps its red, in shade
    put(layers, "ENV_FrameBanner", ek.blur(keep, 2.0), H, kind="foreground")
    col = ek.tp.trim(load("ENV-14a")[:, 330:694], 0.05)
    col[..., :3] = col[..., :3] * np.array([0.34, 0.35, 0.4], np.float32)   # its own stone in cool shade (not a black bar)
    # Gauntlet: no rim on the pillar (a pale glowing strip at the view's edge read as a gap).
    put(layers, "ENV_FramePillar", ek.blur(col, 3.0), H, kind="foreground")

    # Lip vines: small ivy drapes in colour, a seamless strip hung from every block's lit lip ("moss spilling over", §3.3).
    # Gauntlet: sparser and darker (a dense bright band read as a repeating yellow stripe).
    vines = np.zeros((300, 2400 + 160, 4), np.float32)
    rv = np.random.default_rng(77)
    xv, k = -30, 0
    while xv < vines.shape[1]:
        b = dboxes[rv.integers(3)]
        d = drapes[b[1]:b[3], b[0]:b[2]]
        hgt = int(300 * rv.uniform(0.3, 1.0))
        d = ek.scale(d, hgt / d.shape[0])
        place(vines, d if rv.random() < 0.5 else d[:, ::-1], xv, 0)
        xv += int(rv.uniform(60, 260)); k += 1
    vines[..., :3] *= 0.72
    vines = ek.crossfade(vines, 160, 1)
    put(play, "ENV_LipVines", ek.scale(vines, 0.85 * W / vines.shape[0]), W, kind="strip")

    # The thick underside (A16 round 2: "thicker, with more vines and moss hanging"): 0.6 u of the fill's stone, darkening
    # downward, then ENV_Under's course, cornice and fringe, then long ivy drapes. Seamless, one fill tile wide (4 u), so
    # one renderer draws it under a whole floor; the top edge meets the floor's underside.
    under = play["ENV_Under"]
    tile_w = play["ENV_Fill_A"].shape[1]   # one fill tile at the play layer's PPU (gauntlet: the fill is baked at it)
    fill = ek.tp.resize(play["ENV_Fill_A"], tile_w, tile_w)
    pad = 160
    band_h = round(0.6 * W)
    band = np.concatenate([fill[:band_h], fill[:band_h, :pad]], axis=1)
    band[..., :3] *= np.linspace(1.0, 0.72, band_h, dtype=np.float32)[:, None, None]
    under_r = ek.tp.resize(under, tile_w, round(under.shape[0] * tile_w / under.shape[1]))
    under_r = np.concatenate([under_r, under_r[:, :pad]], axis=1)
    drape_h = round(1.25 * W)
    thick = np.zeros((band_h + under_r.shape[0] + drape_h, tile_w + pad, 4), np.float32)
    place(thick, band, 0, 0)
    cornice = band_h - round(0.12 * W)
    # Phase 2 round 3 (critics: "a cream wavy strip under every slab"): the sky showed through between the course's ragged
    # edge and the drapes. A dark shadow band now sits behind the ragged edge (fading out below it), and the drapes start
    # inside the course, so their tops tuck behind the stone.
    under_h = under_r.shape[0]
    rag_zone = round(under_h * 0.45)
    shadow_top, shadow_h = cornice + under_h - rag_zone, rag_zone + round(0.08 * W)
    shade = np.zeros((shadow_h, thick.shape[1], 4), np.float32)
    shade[..., :3] = np.array([0.09, 0.075, 0.06], np.float32)
    shade[..., 3] = np.clip(1.25 - np.linspace(0, 1, shadow_h, dtype=np.float32) ** 1.5 * 1.25, 0, 1)[:, None] * 0.92
    place(thick, shade, 0, shadow_top)
    for k, xv in enumerate(range(-40, thick.shape[1], 64)):
        b = dboxes[(k * 5) % 3]
        d = drapes[b[1]:b[3], b[0]:b[2]]
        hgt = int(drape_h * (0.45 + 0.55 * ((k * 7) % 5) / 4))
        d = ek.scale(d, hgt / d.shape[0])
        place(thick, d, xv, cornice + under_h - rag_zone)
    place(thick, under_r, 0, cornice)
    thick = ek.crossfade(thick, pad, 1)
    rows = np.nonzero((thick[..., 3] > 0.02).any(axis=1))[0]
    thick = thick[:rows[-1] + 2]   # no empty rows under the longest drape
    put(play, "ENV_ThickUnder", thick, W, kind="strip", normal=False)

    # The bridge fringe: the arch tops and spandrels of ENV-07c's aqueduct (its upper 42%), seamless, 0.9 u tall, hung
    # under floating floors so they read as bridge spans (§3.3). Darkened by the builder's support tint.
    aq = load("ENV-07c")
    rows = np.nonzero((aq[..., 3] > 0.02).any(axis=1))[0]
    span = aq[rows[0]:rows[-1] + 1, 389:1415 + 24]
    fringe = ek.crossfade(span[:int(span.shape[0] * 0.42)], 24, 1)
    # The lowest rows fade out (the cut through the piers).
    t = np.linspace(0, 1, fringe.shape[0], dtype=np.float32)
    fringe[..., 3] *= np.clip((1 - t) / 0.25, 0, 1)[:, None]
    put(play, "ENV_ArchFringe", ek.scale(fringe, 0.9 * W / fringe.shape[0]), W, kind="strip")

    # The arch shade: the aqueduct's openings (rows 120-600 of the span) as deep recesses, a seamless strip drawn on the
    # faces of thick blocks below their lit lip, so the ground reads as a bridge (§3.3). Warm dark, a faint haze low in each
    # opening, the piers left clear (the block's own stone shows), the feet fading out.
    band = span[120:600]
    hole = np.clip(1 - band[..., 3] / 0.6, 0, 1)
    t = np.linspace(0, 1, band.shape[0], dtype=np.float32)[:, None]
    shade = np.zeros(band.shape, np.float32)
    top, low = np.array([0.16, 0.11, 0.08], np.float32), np.array([0.42, 0.32, 0.24], np.float32)
    shade[..., :3] = top * (1 - t[..., None]) + low * t[..., None]
    shade[..., 3] = hole * 0.92 * np.clip((1 - t) / 0.3, 0, 1)
    shade = ek.blur(ek.crossfade(shade, 24, 1), 1.5)
    # Wider bays than the aqueduct's (stretched 1.9x across): the ground's arches read as a bridge, not a row of slots.
    k = 2.6 * W / shade.shape[0]
    shade = ek.tp.resize(shade, round(shade.shape[1] * k * 1.9), round(shade.shape[0] * k))
    put(play, "ENV_ArchShade", shade, W, kind="strip")

    # The face shadow (A16 round 3: "consistency"): every block's face falls into one warm shadow below its lit lip. A
    # vertical ramp (clear to full, eased) 1.6 u tall and a solid swatch for the rest; the builder tints both by the
    # level's FaceShadeAlpha.
    n = round(1.6 * W)
    t = np.linspace(0, 1, n, dtype=np.float32)
    ramp = np.zeros((n, 16, 4), np.float32)
    ramp[..., :3] = np.array([0.1, 0.12, 0.22], np.float32)   # gauntlet: cool shade (falloff to blue-violet, not black)
    ramp[..., 3] = (t * t * (3 - 2 * t))[:, None]
    put(play, "ENV_FaceShade", ramp, W, kind="piece")
    solid = np.zeros((16, 16, 4), np.float32)
    solid[..., :3] = np.array([0.1, 0.12, 0.22], np.float32)
    solid[..., 3] = 1
    put(play, "ENV_FaceShadeSolid", solid, W, kind="piece")

    # Water at the bottom of an open world (§3.4): ENV-30's waterline and its glints over a reflection of the far city
    # (flipped, squashed, faded with depth), a seamless band 1600 px wide.
    city = layers["ENV_FarCity"]
    refl = ek.scale(city[::-1], 0.5)[:, :1600]
    refl = refl[:int(refl.shape[0] * 0.7)]
    water = ek.load("ENV-30")[520:880]
    water = ek.crossfade(ek.scale(water, 1600 / water.shape[1]) if water.shape[1] != 1600 else water, 120, 1)[:, :1600]
    hgt = max(refl.shape[0], water.shape[0])
    plane = np.zeros((hgt, 1600 - 120, 4), np.float32)
    r2 = refl[:, :plane.shape[1]].copy()
    r2[..., 3] *= 0.4 * np.linspace(1, 0.2, r2.shape[0], dtype=np.float32)[:, None]
    place(plane, r2, 0, 0)
    place(plane, water[:, :plane.shape[1]], 0, 0)
    put(layers, "ENV_WaterPlane", plane, H, kind="band")

    # Supports and particles.
    put(play, "ENV_Chain", chain(), W, kind="vstrip")
    put(layers, "ENV_Mote", ek.soft_disc(32, (1.0, 0.92, 0.75), inner=0.0, power=2.0), H, kind="piece")
    ray = ek.glow_alpha(load("ENV-28"), floor=0.03, ceil=0.7)
    # Cream, not orange: the grade's light tints it, and additive orange can never reach a highlight's white.
    ray[..., :3] = ray[..., :3] * 0.25 + np.array([1.0, 0.95, 0.86], np.float32) * 0.75
    ray = ek.tp.trim(ray, 0.02)
    # Soft at every edge of the sprite: a ramp over the top 35% and the bottom 20%, and a pad so the blur fades out
    # inside the sprite (a cut edge drew a hard line across the sky).
    pad = 48
    ray = np.pad(ray, ((pad, pad), (pad, pad), (0, 0)))
    n = ray.shape[0]
    t = np.arange(n, dtype=np.float32) / n
    ramp = np.clip(t / 0.35, 0, 1) ** 1.5 * np.clip((1 - t) / 0.2, 0, 1)
    ray[..., 3] *= ramp[:, None]
    put(layers, "ENV_Ray", ek.blur(ray, 10.0), H, kind="piece")
