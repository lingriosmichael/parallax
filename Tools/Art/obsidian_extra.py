#!/usr/bin/env python3
"""D-281: Obsidian Void's premium pass, the pieces obsidian_env.py adds to the overlay. Everything here comes from paintings
already in Art_Source/Environment/ObsidianVoid (never changed):
- skies(): OV-01 in seven colourings, mirrored or rolled, so ten levels don't share one sky;
- planet(): the crescent planet of Inspiration.png, in place of OV-02's star on the sun slot;
- desmear(): the fills without the pale diagonal streaks that gave their tiling away;
- portal(): OV-07a's broken ring closed into a whole ring, and a spiralling window of OV-01's void in each portal colour;
- pieces(): the paintings no level showed: B_MG_01's citadel, OV-15's monolith, OV-09's voidfall spray, B_FG_01's shards.

Imported by obsidian_env.py, which writes the files; no output of its own.
"""
import numpy as np
from PIL import Image
from scipy import ndimage

import trap_process as tp

# OV-01's colourings: hue shift (degrees), saturation, brightness, mirrored, and the window of the painting shown (left and top
# as fractions; ZOOM of each side), so each sky's clouds lie differently.
SKIES = {
    "ENV_Sky_Moon": (0, 1.0, 1.0, False, 0.0, 0.0),
    "ENV_Sky_Teal": (-24, 0.9, 0.95, True, 0.0, 0.2),
    "ENV_Sky_Dusk": (24, 1.0, 0.95, False, 0.2, 0.2),
    "ENV_Sky_Rose": (46, 0.85, 0.95, True, 0.2, 0.0),
    "ENV_Sky_Storm": (0, 0.42, 0.92, False, 0.1, 0.1),
    "ENV_Sky_Jade": (-38, 0.6, 0.92, True, 0.12, 0.05),
    "ENV_Sky_Dawn": (-12, 0.8, 1.14, False, 0.0, 0.2),
}
ZOOM = 0.8
# Inspiration.png: the planet's centre and radius (source pixels). A tower and a small moon stand over its lower half, so only
# its upper limb is painted: the lit side, a crescent, is what the night disc (shifted PLANET_SHIFT radii from the planet's
# centre, along PLANET_DARK) leaves of it; PLANET_SOFT radii of soft terminator.
PLANET_C, PLANET_R = (1350.0, 217.5), 103.0
PLANET_SHIFT, PLANET_SOFT = 0.34, 0.12
PLANET_DARK = (0.25, 0.97)
PLANET_NIGHT = np.array([0.05, 0.07, 0.115], np.float32)
PLANET_NIGHT_ALPHA = 0.42    # the sky shows faintly through the night side: opaque, it read as a hole in the nebula
# OV-07a's ring (in its bottom-left cell): centre and the radii kept. The ring's lower half is buried in rubble, so its upper
# half is mirrored down.
RING_C, RING_R = (383, 267), (152.0, 244.0)
PORTAL_PX, RING_SOFT = 512, 3.0
# The swirl's window, as a fraction of the sprite's radius, and its ramps (shadow, mid, light).
SWIRL_ARMS, SWIRL_TWIST = 3, 7.0
RAMP_IN = np.array([[0.02, 0.07, 0.2], [0.06, 0.42, 0.82], [0.75, 0.97, 1.0]], np.float32)
RAMP_OUT = np.array([[0.1, 0.03, 0.22], [0.5, 0.2, 0.86], [0.96, 0.82, 1.0]], np.float32)


def key_magenta(a):
    """obsidian_env.key_magenta's key, for the B_ sheets (their fringe is wider: flat magenta with a soft antialiased edge)."""
    out = a.copy()
    rgb = out[..., :3]
    key = np.clip((np.minimum(rgb[..., 0], rgb[..., 2]) - rgb[..., 1] - 0.22) / 0.2, 0, 1)
    out[..., 3] = np.minimum(out[..., 3], 1 - key)
    spill = np.clip(np.minimum(rgb[..., 0], rgb[..., 2]) - rgb[..., 1], 0, 1)
    out[..., 0] -= spill
    out[..., 2] -= spill * 0.6
    out[..., :3] = np.clip(out[..., :3], 0, 1)
    return out


def skies(sky, gr):
    """Each sky slot's plate. A slot left out of SKIES (it no level names) stays OV-01 as painted."""
    out = {}
    for name in ("ENV_Sky_Moon", "ENV_Sky_Dawn", "ENV_Sky_Dusk", "ENV_Sky_Ember", "ENV_Sky_Gold", "ENV_Sky_Jade", "ENV_Sky_Mist",
                 "ENV_Sky_Noon", "ENV_Sky_Rose", "ENV_Sky_Storm", "ENV_Sky_Teal", "ENV_SkyPlate"):
        hue, sat, val, mirror, left, top = SKIES.get(name, SKIES["ENV_Sky_Moon"])
        h, w = sky.shape[:2]
        a = sky if (left, top) == (0.0, 0.0) and name == "ENV_Sky_Moon" else sky[int(top * h):int((top + ZOOM) * h), int(left * w):int((left + ZOOM) * w)]
        a = a[:, ::-1].copy() if mirror else a
        hsv = np.asarray(Image.fromarray((a[..., :3] * 255 + 0.5).astype(np.uint8), "RGB").convert("HSV")).astype(np.float32)
        hsv[..., 0] = (hsv[..., 0] + hue * 255 / 360) % 256
        hsv[..., 1] = np.clip(hsv[..., 1] * sat, 0, 255)
        hsv[..., 2] = np.clip(hsv[..., 2] * val, 0, 255)
        rgb = np.asarray(Image.fromarray(hsv.astype(np.uint8), "HSV").convert("RGB")).astype(np.float32) / 255
        g = gr(name)
        out[name] = tp.resize(np.dstack([rgb, np.ones(rgb.shape[:2], np.float32)]), g.shape[1], g.shape[0])
    return out


def planet(inspiration, size):
    """The crescent planet on a `size` px square: its painted upper side fading into a plain night side, on a hard disc so no
    star shows through it."""
    cx, cy = PLANET_C
    r = PLANET_R + 2
    x0, y0, side = int(cx - r), int(cy - r), int(2 * r)
    crop = inspiration[y0:y0 + side, x0:x0 + side, :3]
    big = tp.resize(np.dstack([crop, np.ones(crop.shape[:2], np.float32)]), size, size)[..., :3]
    ys, xs = np.mgrid[0:size, 0:size].astype(np.float32)
    k = size / side
    px, py = (xs - (cx - x0) * k), (ys - (cy - y0) * k)
    d = np.sqrt(px ** 2 + py ** 2) / (PLANET_R * k)
    alpha = np.clip((1.0 - d) * PLANET_R * k / 2.5, 0, 1)
    rx, ry = PLANET_DARK
    night = np.sqrt((px / (PLANET_R * k) - PLANET_SHIFT * rx) ** 2 + (py / (PLANET_R * k) - PLANET_SHIFT * ry) ** 2)
    lit = np.clip((night - 1.0) / PLANET_SOFT + 0.5, 0, 1)[..., None]
    rgb = big * lit + PLANET_NIGHT * (1 - lit)
    alpha = alpha * (PLANET_NIGHT_ALPHA + (1 - PLANET_NIGHT_ALPHA) * lit[..., 0])
    return np.dstack([rgb, alpha]).astype(np.float32)


def desmear(fill):
    """The pale, grey diagonal streaks out of a fill: grey pixels brighter than their neighbourhood come down to it. The cyan
    seams and the block edges stay (they're saturated, or thin enough to leave the neighbourhood alone)."""
    rgb = fill[..., :3]
    lum = (rgb * np.array([0.299, 0.587, 0.114], np.float32)).sum(-1)
    local = ndimage.median_filter(lum[::4, ::4], size=31, mode="wrap")
    local = np.asarray(Image.fromarray(local).resize(lum.shape[::-1], Image.BILINEAR))
    grey = 1 - np.clip((rgb.max(-1) - rgb.min(-1)) / np.maximum(rgb.max(-1), 1e-3) / 0.45, 0, 1)
    wide = ndimage.gaussian_filter(np.clip(lum - local - 0.015, 0, 1) * grey, 3, mode="wrap")
    streak = np.clip(wide / 0.03, 0, 1)
    out = fill.copy()
    target = np.minimum(lum, local)
    out[..., :3] = np.clip(rgb * (1 - streak[..., None]) + rgb * (target / np.maximum(lum, 1e-3))[..., None] * streak[..., None], 0, 1)
    return out


def feather(a, px):
    """Alpha fading to nothing over the last `px` pixels of each side (a crop that cut through a soft mist)."""
    h, w = a.shape[:2]
    ramp_x = np.clip(np.minimum(np.arange(w), np.arange(w)[::-1]) / px, 0, 1)
    ramp_y = np.clip(np.minimum(np.arange(h), np.arange(h)[::-1]) / px, 0, 1)
    out = a.copy()
    out[..., 3] *= ramp_y[:, None] * ramp_x[None, :]
    return out


def soft_band(a, side_px, bottom_px):
    """A soft band that overlaps its neighbours: its ends and its foot fade out (OV-27's fog and OV-06's haze stop in a straight cut,
    which a floor hid until a level's lake showed it)."""
    h, w = a.shape[:2]
    out = a.copy()
    out[..., 3] *= np.clip(np.minimum(np.arange(w), np.arange(w)[::-1]) / side_px, 0, 1)[None, :] ** 1.5
    out[..., 3] *= np.clip((h - 1 - np.arange(h)) / bottom_px, 0, 1)[:, None]
    return out


def key_black(a, floor=0.03, ceil=0.11):
    """A piece painted on black (OV-26's corners): the black falls away, its lit rock and crystals stay."""
    out = a.copy()
    out[..., 3] *= np.clip((a[..., :3].max(-1) - floor) / (ceil - floor), 0, 1)
    return out


def colder(a, amount, ramp):
    """Colour pulled `amount` of the way to the cold ramp (OV-04's monoliths stand in a pale pink-grey mist)."""
    l = (a[..., :3] * np.array([0.299, 0.587, 0.114], np.float32)).sum(-1)[..., None]
    lo = ramp[0] + (ramp[1] - ramp[0]) * np.clip(l * 2, 0, 1)
    hi = ramp[1] + (ramp[2] - ramp[1]) * np.clip(l * 2 - 1, 0, 1)
    out = a.copy()
    out[..., :3] = a[..., :3] * (1 - amount) + np.where(l < 0.5, lo, hi) * amount
    return out


def aerial(a, amount, colour):
    """Baked air: a sprite's tint can only darken it, so the mid and near rows' blacks are lifted here, keeping them lighter than
    the play stone (the darkest thing on screen, what the cat stands on)."""
    out = a.copy()
    out[..., :3] = a[..., :3] * (1 - amount) + colour * amount
    return out


def ring(mid_sheet):
    """OV-07a's ring as a whole ring on PORTAL_PX square, transparent inside and out."""
    cell = mid_sheet[mid_sheet.shape[0] // 2:, :mid_sheet.shape[1] // 2]
    cx, cy = RING_C
    top = cell[:cy]
    whole = np.concatenate([top, top[::-1]], axis=0)
    r = int(RING_R[1]) + 2
    crop = whole[cy - r:cy + r, cx - r:cx + r].copy()
    ys, xs = np.mgrid[0:2 * r, 0:2 * r].astype(np.float32)
    d = np.sqrt((xs - r) ** 2 + (ys - r) ** 2)
    keep = np.clip((d - RING_R[0]) / RING_SOFT, 0, 1) * np.clip((RING_R[1] - d) / RING_SOFT, 0, 1)
    crop[..., 3] *= keep
    return tp.resize(crop, PORTAL_PX, PORTAL_PX)


def swirl(sky, ramp, core):
    """A disc of OV-01's void twisted into a spiral: painted starfield and a few broad arms (they read at phone size), through
    `ramp`, brightest at `core` of the way out from a dark eye."""
    n = PORTAL_PX
    side = sky.shape[0]
    src = sky[:side, (sky.shape[1] - side) // 2:(sky.shape[1] + side) // 2, :3].mean(-1)
    ys, xs = np.mgrid[0:n, 0:n].astype(np.float32)
    px, py = (xs - n / 2 + 0.5) / (n / 2), (ys - n / 2 + 0.5) / (n / 2)
    r = np.sqrt(px ** 2 + py ** 2)
    theta = np.arctan2(py, px)
    bent = theta + SWIRL_TWIST * (1 - np.clip(r, 0, 1)) ** 1.5
    coords = np.stack([(r * 0.46 * np.sin(bent) + 0.5) * (side - 1), (r * 0.46 * np.cos(bent) + 0.5) * (side - 1)])
    stars = ndimage.map_coordinates(src, coords, order=1, mode="reflect")
    stars = np.clip((stars - np.percentile(stars, 5)) / (np.percentile(stars, 99) - np.percentile(stars, 5)), 0, 1)
    arms = 0.5 + 0.5 * np.sin(SWIRL_ARMS * bent + 2.5 * np.log(r + 0.08))
    band = np.exp(-((r - core) / 0.34) ** 2)
    v = np.clip((0.3 + 0.62 * arms) * band + 0.5 * stars * band, 0, 1)[..., None]
    lo = ramp[0] + (ramp[1] - ramp[0]) * np.clip(v * 2, 0, 1)
    hi = ramp[1] + (ramp[2] - ramp[1]) * np.clip(v * 2 - 1, 0, 1)
    rgb = np.where(v < 0.5, lo, hi)
    alpha = np.clip((1.0 - r) * n / 2 / 6.0, 0, 1)
    return np.dstack([rgb, alpha]).astype(np.float32)


def portal(mid_sheet, sky):
    return {"ENV_PortalRing": ring(mid_sheet), "ENV_PortalSwirl_In": swirl(sky, RAMP_IN, 0.62), "ENV_PortalSwirl_Out": swirl(sky, RAMP_OUT, 0.34)}


def pieces(load, glow_alpha):
    """The unused paintings, cut out at background resolution (128 px/u): name -> (image, width in units)."""
    citadel = key_magenta(load("B_MG_01_Mid__raw_v1.png"))
    citadel = citadel[:int(citadel.shape[0] * 0.895)]            # the sheet's straight baseline cut off
    monolith = key_magenta(load("OV-15_broken_monolith_v1.png"))
    spray = glow_alpha(load("OV-09_voidfall_spray_v1.png"), 0.05, 0.55)
    shards = key_magenta(load("B_FG_01.png"))
    ramp = np.clip(np.arange(shards.shape[0], dtype=np.float32) / 90.0, 0, 1)
    shards[..., 3] *= ramp[:, None]                              # its flat top fades out into the stone it hangs over
    return {"ENV_MidCitadel": (tp.trim(citadel, 0.05), 15.0), "ENV_MidMonolith": (tp.trim(monolith, 0.05), 4.2),
            "ENV_VoidSpray": (tp.trim(spray, 0.03), 6.5), "ENV_FG_Shards": (tp.trim(shards, 0.05), 7.0)}
