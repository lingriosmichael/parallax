#!/usr/bin/env python3
"""PAX-A20 stage 2: Obsidian Void's play-layer stone. Turns the developer's ObsidianVoid paintings (Art_Source/Environment/
ObsidianVoid/OV-*.png, never changed) into overrides of the Golden Ruins kit's play-layer slots: the same names, the same
pixel sizes and the same alignment offsets (copied from the Golden Ruins env_kit.json), so every rule the builder aligns by
holds unchanged. EnvironmentKit reads them as an overlay while it builds an L031+ level; a slot not written here (the
broken-stone masks, which take their colour from the fill; the shading; the backgrounds until stage 3) stays Golden Ruins'.

Each slot is one of:
- painted: cut from its Obsidian painting at the kit's scale (fills OV-10a/a2/a3, cap OV-10b, underside OV-10c, slab
  OV-11, wall side OV-12, door OV-24, checkpoint OV-25), seams cross-faded on its tiling axis;
- re-cut: the Golden Ruins slot's own shape (its alpha) and light and shade (its luminance against its stone's), filled with
  the Obsidian stone at the same pixels (ends, posts, inner sides, thick undersides, recess lips);
- recoloured: warm light and shade turned to the cold palette through a gradient map (washes, glows);
- empty: organic dressing (moss, drapes, ferns, banners, glyphs, rubble, hanging vines) until stage 4's props.

Usage: python3 Tools/Art/obsidian_env.py [--check]   (--check: write nothing, fail if any output would change)
"""
import io
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter

sys.path.insert(0, str(Path(__file__).parent))
import env_kit as ek  # noqa: E402
import hazard_readable  # noqa: E402
import trap_process as tp  # noqa: E402

ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT / "Art_Source/Environment/ObsidianVoid"
OUT = ROOT / "Assets/_Game/Art/ObsidianVoid/Environment/Kit"
OUT_LAYERS = ROOT / "Assets/_Game/Art/ObsidianVoid/Environment/Backgrounds/Kit"
GR_MANIFEST = ek.KIT / "env_kit.json"

# The edge strips' scale: a painting's seamless stretch is resized to half the Golden Ruins strip's length and tiled
# twice, so the Obsidian blocks come out about the fill's size.
SEAM = 150
# The paintings' lines (rows or columns, in source pixels): where each strip's walkable or face line sits.
CAP_LINE, UNDER_LINE, SLAB_LINE, SIDE_LINE = 452, 430, 334, 250
# Fills: each painting made seamless, drawn FILL_TILES x FILL_TILES times across the Golden Ruins fill's pixels (blocks
# about 0.8 u).
FILL_TILES = 2
FILLS = {"A": "OV-10a_fill_v1.png", "A2": "OV-10a2_fill_fractured_v1.png", "A3": "OV-10a3_fill_rimed_v1.png"}
# A fill's recessed twin (the back wall of pits and shafts): softer, half the contrast, darker.
RECESS_BLUR, RECESS_CONTRAST, RECESS_DARK = 6, 0.5, 0.72
# A thick underside's last THICK_FRINGE px are OV-10c's underside, its line THICK_FRINGE_ABOVE px down them.
THICK_FRINGE, THICK_FRINGE_ABOVE = 170, 95
# The cold ramp the recoloured slots' luminance runs through (shadow, mid, light).
RAMP = np.array([[0.03, 0.06, 0.11], [0.22, 0.38, 0.52], [0.72, 0.92, 1.0]], np.float32)
EMPTY = ["ENV_Moss_0", "ENV_Moss_1", "ENV_Moss_2", "ENV_Drape_0", "ENV_Drape_1", "ENV_Drape_2", "ENV_Drape_3", "ENV_Drape_4",
         "ENV_Drape_5", "ENV_Fern_0", "ENV_Fern_1", "ENV_Fern_2", "ENV_LipVines", "ENV_LipVinesEnd", "ENV_ArchFringe", "ENV_Chain"]
RECOLOUR = ["ENV_CapWash", "ENV_ArchShade", "ENV_Glow", "ENV_Halo", "ENV_Water"]
# Stage 3's pieces: OV-04's islands (source boxes, left to right as they sit on the sheet), OV-05's spires and OV-07a's mid
# pieces and OV-26's foreground corners by grid cell (column, row).
ISLANDS = [(16, 178, 446, 920), (467, 239, 741, 685), (773, 100, 1185, 946), (1227, 242, 1536, 866)]
SPIRES = {"ENV_FarSpire_0": (0, 0), "ENV_FarSpire_1": (2, 0), "ENV_FarSpire_2": (1, 0), "ENV_FarSpire_3": (2, 1)}
MIDS = {"ENV_MidArch_0": (0, 0), "ENV_MidTowers": (1, 0), "ENV_MidArch_1": (0, 1), "ENV_MidPillar": (1, 1)}
CLOUD_ALPHA = 0.5
# Stage 4's props (D-275): OV-20's six ceiling fixtures (3 x 2 grid), drawn FIXTURE_H units tall, and OV-21's six tech props
# (3 x 2), PROP_W units wide, are Obsidian-only slots the dressing places sparsely (ENV_Fixture_*, ENV_Prop_*); OV-22's two
# holo-banners, OV-23's wreckage (top row) and circuit panels (bottom row, the debris at their foot cut away, PANEL_KEEP of the
# cell kept) fill the Golden Ruins banner, rubble and glyph slots.
FIXTURE_H, PROP_W, PANEL_KEEP = 0.9, 1.2, 0.74
# OV-21's and OV-23's two painted rows (source y ranges): their pieces straddle the sheet's half.
PROP_ROWS, WRECK_ROWS = ((180, 545), (620, 960)), ((180, 510), (540, 940))
FOREGROUND = {"ENV_FG_Ferns": (0, 1), "ENV_FG_Roots": (1, 1), "ENV_FG_Branch": (1, 0)}
# D-114 in Obsidian: a grip face's sign is OV-31's lit circuit panel, not moss. GRIP_TUFTS pieces of the panel's bright bar
# (columns GRIP_COLS, each GRIP_ROWS tall, starting at GRIP_STARTS), laid on their side as the moss tufts are (the builder
# stands them upright along the face), GRIP_TUFT_PX in size: Obsidian-only slots, no Golden Ruins twin.
GRIP_COLS, GRIP_ROWS, GRIP_STARTS, GRIP_TUFT_PX = (440, 600), 480, (120, 820, 1520), (216, 72)


def lum(a):
    return (a[..., :3] * np.array([0.299, 0.587, 0.114], np.float32)).sum(-1)


def key_magenta(a):
    """Painted on flat magenta: red and blue high, green low becomes clear; any magenta cast pulled down (env_v2's key)."""
    out = a.copy()
    rgb = out[..., :3]
    key = np.clip((np.minimum(rgb[..., 0], rgb[..., 2]) - rgb[..., 1] - 0.35) / 0.25, 0, 1)
    out[..., 3] = np.minimum(out[..., 3], 1 - key)
    spill = np.clip(np.minimum(rgb[..., 0], rgb[..., 2]) - rgb[..., 1], 0, 1)
    out[..., 0] -= spill
    out[..., 2] -= spill
    out[..., :3] = np.where(out[..., 3:4] > 0.02, np.clip(out[..., :3], 0, 1), 0)
    return out


def tile_to(a, width, height):
    reps = (int(np.ceil(height / a.shape[0])), int(np.ceil(width / a.shape[1])), 1)
    return np.tile(a, reps)[:height, :width]


def strip_x(a, rows, line, above_px, height, width):
    """A horizontal strip `height` px tall whose line (source row `line`) lands `above_px` rows from its top: the painting
    made seamless across, resized to half `width` and tiled twice."""
    a = ek.crossfade(a, SEAM, 1)
    half = width // 2
    k = half / a.shape[1]
    top = int(round(line - above_px / k))
    band = a[max(top, 0):top + int(round(height / k))]
    if top < 0:
        band = np.concatenate([np.zeros((-top, a.shape[1], 4), np.float32), band])
    band = tp.resize(band, half, height)
    return tile_to(band, width, height)


def strip_y(a, cols, line, outside_px, width, height):
    """A vertical strip `width` px wide whose face line (source column `line`) lands `outside_px` columns from its left."""
    return np.transpose(strip_x(np.transpose(a, (1, 0, 2)), None, line, outside_px, width, height), (1, 0, 2))


def recess(fill):
    pad = RECESS_BLUR * 3
    rgb = np.pad(fill[..., :3], ((pad, pad), (pad, pad), (0, 0)), mode="wrap")
    soft = np.dstack([np.asarray(Image.fromarray((rgb[..., c] * 255 + 0.5).astype(np.uint8), "L").filter(ImageFilter.GaussianBlur(RECESS_BLUR))).astype(np.float32) / 255
                      for c in range(3)])[pad:-pad, pad:-pad]
    mean = soft.mean(axis=(0, 1), keepdims=True)
    out = np.ones(fill.shape, np.float32)
    out[..., :3] = np.clip((mean + (soft - mean) * RECESS_CONTRAST) * RECESS_DARK, 0, 1)
    return out


def recut(gr_slot, gr_stone, stone):
    """The Golden Ruins slot's alpha and its light and shade (its luminance over its own stone's mean), filled with
    `stone` (tiled from the top left, as the world-space fill is)."""
    tex = tile_to(stone, gr_slot.shape[1], gr_slot.shape[0])
    shade = np.clip(lum(gr_slot) / max(float(lum(gr_stone).mean()), 1e-3), 0.35, 1.6)[..., None]
    out = np.empty_like(gr_slot)
    out[..., :3] = np.clip(tex[..., :3] * shade, 0, 1)
    out[..., 3] = gr_slot[..., 3]
    return out


def recolour(a):
    """Luminance through the cold ramp (a two-segment gradient map), alpha kept."""
    l = np.clip(lum(a) / max(float(np.percentile(lum(a)[a[..., 3] > 0.05], 98)) if (a[..., 3] > 0.05).any() else 1.0, 1e-3), 0, 1)[..., None]
    lo = RAMP[0] + (RAMP[1] - RAMP[0]) * np.clip(l * 2, 0, 1)
    hi = RAMP[1] + (RAMP[2] - RAMP[1]) * np.clip(l * 2 - 1, 0, 1)
    out = a.copy()
    out[..., :3] = np.where(l < 0.5, lo, hi)
    return out


def fit_object(src, gr, top=False, centre=False):
    """`src` (trimmed) fitted inside the Golden Ruins object's opaque box, keeping its shape, standing on that box's foot (or
    hung from its top, or centred in it) and centred across; the canvas is the Golden Ruins slot's."""
    ys, xs = np.nonzero(gr[..., 3] > 0.08)
    x0, x1, y0, y1 = xs.min(), xs.max() + 1, ys.min(), ys.max() + 1
    s = tp.trim(src, 0.05)
    k = min((x1 - x0) / s.shape[1], (y1 - y0) / s.shape[0])
    w, h = max(1, int(round(s.shape[1] * k))), max(1, int(round(s.shape[0] * k)))
    r = tp.resize(s, w, h)
    out = np.zeros_like(gr)
    left = x0 + ((x1 - x0) - w) // 2
    bottom = y0 + h if top else (y0 + (y1 - y0 + h) // 2 if centre else y1)   # hung from the box's top, centred, or standing
    out[bottom - h:bottom, left:left + w] = r
    return out


def halves(name, magenta=False):
    """A sheet painted twice side by side (off and on): its left and right pieces."""
    a = load(name)
    if magenta:
        a = key_magenta(a)
    cols = (a[..., 3] > 0.05).mean(axis=0)
    mid = a.shape[1] // 2
    gap = mid - 200 + int(np.argmin(cols[mid - 200:mid + 200]))
    return a[:, :gap], a[:, gap:]


def load(name):
    return tp.load_rgba(SRC / name)


def cell(a, cols, rows, c, r):
    """Cell (c, r) of a sheet painted as a cols x rows grid, trimmed to its visible pixels."""
    h, w = a.shape[0] // rows, a.shape[1] // cols
    return tp.trim(a[r * h:(r + 1) * h, c * w:(c + 1) * w], 0.05)


def row_cell(a, rows, cols, c):
    """Column `c` of `cols` equal columns within the painted row `rows` (y0, y1) of a sheet (for sheets whose rows don't sit
    in equal halves): its largest piece only (slivers of the neighbours cut away), trimmed."""
    from scipy import ndimage
    w = a.shape[1] // cols
    piece = a[rows[0]:rows[1], c * w:(c + 1) * w].copy()
    labels, n = ndimage.label(ndimage.binary_dilation(piece[..., 3] > 0.2, iterations=6))
    if n > 1:
        sizes = ndimage.sum(np.ones_like(labels), labels, range(1, n + 1))
        piece[..., 3] *= (labels == 1 + int(np.argmax(sizes)))
    return tp.trim(piece, 0.05)


def band(a, gr_slot):
    """A soft band (mist, haze, fog) stretched to the Golden Ruins band's canvas, trimmed to its visible rows first."""
    ys = np.nonzero((a[..., 3] > 0.03).any(axis=1))[0]
    return tp.resize(a[ys.min():ys.max() + 1], gr_slot.shape[1], gr_slot.shape[0])


def layers(gr):
    """Stage 3: the background pieces Obsidian levels draw (PAX-A20), each on its Golden Ruins slot's canvas."""
    out = {}
    sky = load("OV-01_sky_v1.png"); sky[..., 3] = 1.0
    for name in ("ENV_Sky_Moon", "ENV_Sky_Dawn", "ENV_Sky_Dusk", "ENV_Sky_Ember", "ENV_Sky_Gold", "ENV_Sky_Jade", "ENV_Sky_Mist",
                 "ENV_Sky_Noon", "ENV_Sky_Rose", "ENV_Sky_Storm", "ENV_Sky_Teal", "ENV_SkyPlate"):
        g = gr(name)
        out[name] = tp.resize(sky, g.shape[1], g.shape[0])
    star = ek.glow_alpha(load("OV-02_star_v1.png"), 0.03, 0.7)
    out["ENV_Sun"] = tp.resize(star, *gr("ENV_Sun").shape[1::-1])
    # Clouds and the soft bands: the void's mists, far to near.
    mists = {"far": load("OV-03c_mist_far_v1.png"), "mid": load("OV-03b_mist_mid_v1.png"), "near": load("OV-03a_mist_near_v1.png")}
    for name, m in (("ENV_Cloud_0", "far"), ("ENV_Cloud_1", "mid"), ("ENV_Cloud_2", "near"),
                    ("ENV_CloudsFar", "far"), ("ENV_CloudsMid", "mid"), ("ENV_CloudsNear", "near")):
        b = band(mists[m], gr(name))
        b[..., 3] *= CLOUD_ALPHA   # the mists' bright wisps are busy behind the play layer: half strength
        out[name] = b
    out["ENV_Haze"] = band(ek.glow_alpha(load("OV-06_haze_v1.png"), 0.04, 0.6), gr("ENV_Haze"))
    out["ENV_Fog"] = band(load("OV-27_fog_v1.png"), gr("ENV_Fog"))
    out["ENV_Mist"] = fit_object(load("OV-03a_mist_near_v1.png"), gr("ENV_Mist"))
    # The far skyline: OV-04's four floating monoliths are the islands; OV-05's towers, bridge and crystal spire the spires.
    mono = load("OV-04_far_monoliths_v1.png")
    for i, box in enumerate(ISLANDS):
        x0, y0, x1, y1 = box
        out[f"ENV_FarIsland_{i}"] = fit_object(mono[y0:y1, x0:x1], gr(f"ENV_FarIsland_{i}"))
    arch = load("OV-05_far_architecture_v1.png")
    for name, (c, r) in SPIRES.items():
        out[name] = fit_object(cell(arch, 3, 2, c, r), gr(name))
    skyline = load("OV-41_far_skyline_v1.png")
    for name in ("ENV_CitySkyline", "ENV_FarCluster", "ENV_FarCity"):
        out[name] = fit_object(skyline, gr(name)) if name != "ENV_FarCity" else band(skyline, gr(name))
    pool = load("OV-30_void_pool_v1.png")
    for name in ("ENV_FarLake_0", "ENV_FarLake_1"):
        out[name] = fit_object(pool if name.endswith("0") else pool[:, ::-1].copy(), gr(name))
    out["ENV_WaterPlane"] = band(pool, gr("ENV_WaterPlane"))
    # Mid distance (a signature, where a level asks for one): OV-07a's gateway, ring, stepped pyramid and leaning slab,
    # OV-07b's crystal formations in place of trees, OV-07c's span, OV-07d's colonnade.
    mid = load("OV-07a_mid_architecture_v1.png")
    for name, (c, r) in MIDS.items():
        out[name] = fit_object(cell(mid, 2, 2, c, r), gr(name))
    crystals = load("OV-07b_mid_crystals_v1.png")
    out["ENV_MidTree_0"] = fit_object(cell(crystals, 2, 1, 0, 0), gr("ENV_MidTree_0"))
    out["ENV_MidTree_1"] = fit_object(cell(crystals, 2, 1, 1, 0), gr("ENV_MidTree_1"))
    out["ENV_MidAqueduct"] = fit_object(load("OV-07c_span_v1.png"), gr("ENV_MidAqueduct"))
    out["ENV_MidColonnade"] = fit_object(load("OV-07d_colonnade_v1.png"), gr("ENV_MidColonnade"))
    out["ENV_MidRuins"] = fit_object(load("OV-07d_colonnade_v1.png"), gr("ENV_MidRuins"))
    back = load("OV-32_back_wall_v1.png"); back[..., 3] = 1.0
    for name in ("ENV_BackWall", "ENV_RuinWall"):
        g = gr(name)
        w = tp.resize(back, g.shape[1], g.shape[0])
        w[..., 3] = g[..., 3]          # the Golden Ruins wall's feathered ends, so pieces overlap without a seam
        out[name] = w
    out["ENV_Waterfall"] = tp.resize(load("OV-08_voidfall_v1.png"), *gr("ENV_Waterfall").shape[1::-1])
    for name in ("ENV_Shaft", "ENV_Ray"):
        out[name] = fit_object(load("OV-28_beam_v1.png"), gr(name))
    # The foreground: OV-26's crystal corners and OV-26b's monolith (Golden Ruins' ferns, roots, branch and trunk).
    fg = load("OV-26_foreground_v1.png")
    for name, (c, r) in FOREGROUND.items():
        out[name] = fit_object(cell(fg, 2, 2, c, r), gr(name))
    out["ENV_FG_Trunk"] = fit_object(key_magenta(load("OV-26b_foreground_monolith_v1.png")), gr("ENV_FG_Trunk"))
    out["ENV_Mote"] = recolour(gr("ENV_Mote"))
    return out


def build():
    gr_manifest = json.loads(GR_MANIFEST.read_text())
    gr_slots = {s["name"]: s for s in gr_manifest["slots"]}
    gr = lambda n: tp.load_rgba((ek.KIT if gr_slots[n]["folder"] == "Kit" else ek.LAYERS) / gr_slots[n]["file"])  # noqa: E731
    out = {}

    # ---- fills and their recessed twins ----
    fills = {}
    for key, file in FILLS.items():
        g = gr(f"ENV_Fill_{key}")
        a = ek.crossfade(ek.crossfade(load(file), 120, 1), 120, 0)
        size = g.shape[1] // FILL_TILES
        a = tp.resize(a, size, size)
        a[..., 3] = 1.0
        fills[key] = tile_to(a, g.shape[1], g.shape[0])
        out[f"ENV_Fill_{key}"] = fills[key]
        out[f"ENV_Fill_{key}_Recess"] = recess(fills[key])
        out[f"ENV_RecessLip_{key}"] = recut(gr(f"ENV_RecessLip_{key}"), gr(f"ENV_Fill_{key}_Recess"), out[f"ENV_Fill_{key}_Recess"])

    def px(name, field):
        s = gr_slots[name]
        return int(round(s.get(field, 0.0) * gr(name).shape[1] / s["width"]))

    # ---- painted strips ----
    cap_src = load("OV-10b_top_edge_v1.png")
    g = gr("ENV_Cap")
    cap = strip_x(cap_src, None, CAP_LINE, px("ENV_Cap", "above"), g.shape[0], g.shape[1])
    # Its foot fades into the fill as the Golden Ruins cap's does (that cap's mean alpha below its line, row by row).
    foot = g[..., 3].mean(axis=1)
    line = px("ENV_Cap", "above")
    cap[line:, :, 3] *= (foot[line:] / max(float(foot[line:].max()), 1e-3))[:, None]
    out["ENV_Cap"] = cap
    out["ENV_CapFlat"] = cap.copy()
    for i in range(4):
        e = gr(f"ENV_CapEnd_{i}")
        end = cap[:, :e.shape[1]].copy()
        end[..., 3] *= e[..., 3]
        out[f"ENV_CapEnd_{i}"] = end

    under_src = load("OV-10c_underside_v1.png")
    for name in ("ENV_Under", "ENV_UnderWalk"):
        g = gr(name)
        below = px(name, "below")
        u = strip_x(under_src, None, UNDER_LINE, g.shape[0] - below, g.shape[0], g.shape[1])
        u[..., 3] *= np.linspace(1.0, 0.0, g.shape[0], dtype=np.float32)[:, None] ** 0.25   # its tips fade out, never cut
        out[name] = u

    slab_src = key_magenta(load("OV-11_slab_v1.png"))
    g = gr("ENV_Slab")
    slab = strip_x(slab_src, None, SLAB_LINE, px("ENV_Slab", "above"), g.shape[0], g.shape[1])
    out["ENV_Slab"] = slab
    out["ENV_SlabFlip"] = slab.copy()
    for name in ("ENV_SlabEnd", "ENV_SlabEndThick"):
        e = gr(name)
        end = tile_to(np.concatenate([slab[:, :e.shape[1]], recut(e, gr("ENV_Fill_A"), fills["A"])[slab.shape[0]:]], axis=0), e.shape[1], e.shape[0]) if e.shape[0] > slab.shape[0] else slab[:, :e.shape[1]].copy()
        end[..., 3] = np.minimum(end[..., 3], e[..., 3]) if e.shape[0] > slab.shape[0] else end[..., 3] * e[..., 3]
        out[name] = end

    side_src = load("OV-12_wall_edge_v1.png")[:, ::-1]   # its outside is on the right; the kit's on the left
    g = gr("ENV_Side")
    out["ENV_Side"] = strip_y(side_src, None, side_src.shape[1] - SIDE_LINE, px("ENV_Side", "outside"), g.shape[1], g.shape[0])

    # ---- thick undersides: the stone, then OV-10c's underside and its crystals (never Golden Ruins' hanging ivy) ----
    for name, end in (("ENV_ThickUnder", "ENV_ThickUnderEnd"), ("ENV_ThickUnderShort", "ENV_ThickUnderShortEnd")):
        g = gr(name)
        fringe = strip_x(under_src, None, UNDER_LINE, THICK_FRINGE_ABOVE, THICK_FRINGE, g.shape[1])
        fringe[..., 3] *= np.linspace(1.0, 0.0, THICK_FRINGE, dtype=np.float32)[:, None] ** 0.25
        body = tile_to(fills["A"], g.shape[1], g.shape[0] - THICK_FRINGE + THICK_FRINGE_ABOVE)
        thick = np.concatenate([body[:g.shape[0] - THICK_FRINGE], fringe], axis=0)
        out[name] = thick
        e = gr(end)
        piece = thick[:, :e.shape[1]].copy()
        piece[..., 3] *= e[..., 3].max(axis=0)[None, :]   # the Golden Ruins end's fade across, not its ivy
        out[end] = piece

    # ---- re-cut from the Obsidian stone ----
    for name in ("ENV_Post", "ENV_SlimPost", "ENV_PostCap", "ENV_PostBase", "ENV_Socket"):
        out[name] = recut(gr(name), gr("ENV_Fill_A"), fills["A"])
    for key in FILLS:
        for i in range(4):
            name = f"ENV_SideIn_{key}_{i}"
            out[name] = recut(gr(name), gr(f"ENV_Fill_{key}"), fills[key])

    # ---- recoloured, and empty ----
    for name in RECOLOUR:
        out[name] = recolour(gr(name))
    for name in EMPTY:
        out[name] = np.zeros_like(gr(name))

    # ---- objects: the door (open, lit) and the checkpoint (dark, lit) ----
    closed, opened = halves("OV-24_door_v1.png")
    out["ENV_Door"] = fit_object(opened, gr("ENV_Door"))
    dark, lit = halves("OV-25_checkpoint_v1.png")
    out["ENV_Checkpoint"] = fit_object(dark, gr("ENV_Checkpoint"))
    out["ENV_CheckpointLit"] = fit_object(lit, gr("ENV_CheckpointLit"))

    # ---- grip panels (Obsidian's grip-face sign) ----
    panel = load("OV-31_grip_panel_v1.png")
    extra = {}
    for i, y0 in enumerate(GRIP_STARTS):
        piece = panel[y0:y0 + GRIP_ROWS, GRIP_COLS[0]:GRIP_COLS[1]]
        piece = np.rot90(piece, 1).copy()          # on its side, as a moss tuft is painted
        piece[..., 3] *= np.minimum(1.0, np.minimum(np.arange(piece.shape[1]), np.arange(piece.shape[1])[::-1]) / 24.0)[None, :]
        name = f"ENV_GripTuft_{i}"
        out[name] = tp.resize(piece, *GRIP_TUFT_PX)
        extra[name] = {"name": name, "file": name + ".png", "folder": "Kit", "kind": "dressing",
                       "width": round(GRIP_TUFT_PX[0] / ek.WORLD_PPU, 4), "height": round(GRIP_TUFT_PX[1] / ek.WORLD_PPU, 4)}

    out.update(layers(gr))

    # ---- stage 4: props ----
    fixtures = load("OV-20_fixtures_v2.png")
    props = load("OV-21_tech_props_v2.png")
    for i in range(6):
        c, r = i % 3, i // 3
        f = cell(fixtures, 3, 2, c, r)
        name = f"ENV_Fixture_{i}"
        h = int(round(FIXTURE_H * ek.WORLD_PPU))
        out[name] = tp.resize(f, max(1, int(round(f.shape[1] * h / f.shape[0]))), h)
        extra[name] = {"name": name, "file": name + ".png", "folder": "Kit", "kind": "dressing",
                       "width": round(out[name].shape[1] / ek.WORLD_PPU, 4), "height": round(h / ek.WORLD_PPU, 4)}
        q = row_cell(props, PROP_ROWS[r], 3, c)
        name = f"ENV_Prop_{i}"
        w = int(round(PROP_W * ek.WORLD_PPU))
        out[name] = tp.resize(q, w, max(1, int(round(q.shape[0] * w / q.shape[1]))))
        extra[name] = {"name": name, "file": name + ".png", "folder": "Kit", "kind": "dressing",
                       "width": round(w / ek.WORLD_PPU, 4), "height": round(out[name].shape[0] / ek.WORLD_PPU, 4)}
    banners = load("OV-22_holo_banners_v2.png")
    out["ENV_Banner_0"] = fit_object(cell(banners, 2, 1, 0, 0), gr("ENV_Banner_0"), top=True)
    out["ENV_Banner_1"] = fit_object(cell(banners, 2, 1, 1, 0), gr("ENV_Banner_1"), top=True)
    wreck = load("OV-23_wreckage_panels_v2.png")
    out["ENV_Rubble_0"] = fit_object(row_cell(wreck, WRECK_ROWS[0], 3, 0), gr("ENV_Rubble_0"))
    out["ENV_Rubble_1"] = fit_object(row_cell(wreck, WRECK_ROWS[0], 3, 2), gr("ENV_Rubble_1"))
    for i, c in enumerate((1, 2)):
        panel = row_cell(wreck, WRECK_ROWS[1], 3, c)
        out[f"ENV_Glyph_{i}"] = fit_object(tp.trim(panel[:int(panel.shape[0] * PANEL_KEEP)], 0.05), gr(f"ENV_Glyph_{i}"), centre=True)

    slots = {}
    for name, a in out.items():
        if name in extra:
            slots[name] = extra[name]
            continue
        g = gr_slots[name]
        assert a.shape[:2] == gr(name).shape[:2], f"{name}: {a.shape[:2]} != {gr(name).shape[:2]}"
        slots[name] = dict(g)
    return out, slots


def png(a):
    b = io.BytesIO()
    Image.fromarray((np.clip(a, 0, 1) * 255 + 0.5).astype(np.uint8), "RGBA").save(b, "PNG")
    return b.getvalue()


def main():
    out, slots = build()
    files = {}
    for name, a in out.items():
        a = hazard_readable.out_of_danger(a)
        folder = OUT if slots[name]["folder"] == "Kit" else OUT_LAYERS
        files[folder / slots[name]["file"]] = png(a)
        if slots[name].get("normal"):
            files[folder / slots[name]["file"].replace(".png", "_n.png")] = png(tp.normal_map(a, strength=1.6, bevel_px=2.0))
    manifest = (json.dumps({"worldPpu": ek.WORLD_PPU, "halfPpu": ek.HALF_PPU,
                            "slots": [slots[k] for k in sorted(slots)]}, indent=2) + "\n").encode()
    files[OUT / "env_kit.json"] = manifest
    if "--check" in sys.argv:
        stale = [str(p.relative_to(ROOT)) for p, b in files.items() if not p.exists() or p.read_bytes() != b]
        if stale:
            sys.exit("obsidian_env.py: would change " + ", ".join(stale[:10]) + (" ..." if len(stale) > 10 else ""))
        print("obsidian_env.py: up to date")
        return
    OUT.mkdir(parents=True, exist_ok=True)
    OUT_LAYERS.mkdir(parents=True, exist_ok=True)
    for p, b in files.items():
        p.write_bytes(b)
    print(f"obsidian_env.py: {len(out)} slots ({sum(1 for s in slots.values() if s.get('normal'))} with normal maps) -> {OUT.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
