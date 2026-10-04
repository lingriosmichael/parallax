"""PAX-V08: hazard readability. Every lethal sprite reads on any background: a dark outer line and a light inner rim around
its silhouette (one of the two always contrasts, on bright sky or dark stone), and one danger colour, crimson, that no
background or environment art uses (arrow heads and fletching, spike tips, the spear's blade, the launcher's mouth).

The developer (2026-10-03): "arrows are super hard to see … make the arrow more visible regardless of the level it is in";
rulings: a red danger colour, pushed toward crimson; Ember Night's braziers and Golden Ruins' gold stay amber/yellow, never
the danger hue; hidden spikes get the outline from their reveal on.

Arrays are float32 RGBA in 0..1, (height, width, 4), sRGB, as trap_process.py uses them.
"""

import colorsys

import numpy as np
from PIL import Image, ImageFilter

# The danger colour (sRGB). Hue about 352 degrees: crimson, well away from amber/gold (30-55 degrees). The C# side
# (Parallax.Presentation.HazardPalette.Danger) carries the same value.
DANGER = np.array([0.80, 0.06, 0.14], np.float32)
DANGER_HUE = colorsys.rgb_to_hsv(*DANGER.tolist())[0] * 360.0
OUTER = np.array([0.04, 0.03, 0.03], np.float32)       # the dark outer line
INNER = np.array([0.93, 0.90, 0.84], np.float32)       # the light inner rim (bone white)
BONE = np.array([0.88, 0.83, 0.72], np.float32)        # an arrow's or spear's shaft
TIP_RIM = np.array([1.0, 0.70, 0.72], np.float32)     # a spike's rim near its tip: the danger colour, light


def is_danger_hue(rgb, hue_tolerance=22.0, min_saturation=0.55, min_value=0.30):
    """Pixels (..., 3) whose hue is within hue_tolerance degrees of the danger colour's, saturated and bright enough to
    read as it. Used by the palette check: background and environment art must not contain the danger colour."""
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    mx, mn = np.maximum(np.maximum(r, g), b), np.minimum(np.minimum(r, g), b)
    delta = mx - mn
    sat = np.where(mx > 1e-6, delta / np.maximum(mx, 1e-6), 0.0)
    safe = np.maximum(delta, 1e-6)
    hue = np.where(mx == r, ((g - b) / safe) % 6.0, np.where(mx == g, (b - r) / safe + 2.0, (r - g) / safe + 4.0)) * 60.0
    diff = np.abs(((hue - DANGER_HUE) + 180.0) % 360.0 - 180.0)
    return (diff <= hue_tolerance) & (sat >= min_saturation) & (mx >= min_value) & (delta > 1e-6)


def _dilate(mask, px, wrap_x=False):
    """A binary dilation by px pixels (a square neighbourhood); wrap_x wraps across the left and right edges, so a tiled
    strip's outline joins at its seam."""
    out = mask.copy()
    for dy in range(-px, px + 1):
        for dx in range(-px, px + 1):
            if dx == 0 and dy == 0: continue
            shifted = np.roll(mask, dy, axis=0)
            if dy > 0: shifted[:dy] = False
            elif dy < 0: shifted[dy:] = False
            shifted = np.roll(shifted, dx, axis=1)
            if not wrap_x:
                if dx > 0: shifted[:, :dx] = False
                elif dx < 0: shifted[:, dx:] = False
            out |= shifted
    return out


def pad(a, px, sides=(True, True, True, True)):
    """Transparent padding (top, right, bottom, left) so an outer line has room."""
    top, right, bottom, left = (px if s else 0 for s in sides)
    return np.pad(a, ((top, bottom), (left, right), (0, 0)))


def outline(a, outer_px=2, inner_px=1, threshold=0.5, wrap_x=False):
    """The double outline: silhouette pixels within inner_px of its edge turn bone white, and a dark line outer_px wide is
    drawn outside it (opaque). The input must already have outer_px of transparent room where the line goes."""
    a = a.copy()
    sil = a[..., 3] >= threshold
    outside = _dilate(sil, outer_px, wrap_x) & ~sil
    inside_edge = sil & _dilate(~sil, inner_px, wrap_x)
    a[..., :3] = np.where(inside_edge[..., None], INNER, a[..., :3])
    a[..., 3] = np.where(inside_edge, 1.0, a[..., 3])
    a[..., :3] = np.where(outside[..., None], OUTER, a[..., :3])
    a[..., 3] = np.where(outside, 1.0, a[..., 3])
    return a


def _value(a):
    """The painted shading as 0..1 (luminance, stretched over the opaque pixels), to keep detail when recolouring."""
    lum = (a[..., :3] * np.array([0.3, 0.55, 0.15], np.float32)).sum(-1)
    op = a[..., 3] > 0.5
    if not op.any(): return lum
    lo, hi = float(lum[op].min()), float(lum[op].max())
    return np.clip((lum - lo) / max(1e-4, hi - lo), 0, 1)


def recolour(a, mask, colour, low=0.55, high=1.25):
    """Pixels in mask take `colour`, shaded by the painted value (low..high times the colour)."""
    a = a.copy()
    v = _value(a)[..., None]
    tinted = np.clip(colour * (low + (high - low) * v), 0, 1)
    a[..., :3] = np.where(mask[..., None], tinted, a[..., :3])
    return a


def arrow(a, thicken=2.0, upscale=2, outer_px=5, inner_px=4):
    """TRAP-02: the painted arrow (pointing right), drawn thicker (thicken times its height: the grey-box arrow is fitted by
    its length, so a thin sprite drew 0.08 u thick), its shaft bone white, its head (the right quarter) and fletching (the
    left fifth) crimson, then outlined. Made at `upscale` times the resolution: the game draws an arrow at about half its
    sprite's pixels on a phone, so a 2 px line became one blended screen pixel (HazardContrastTests); at 2x a 5 px line
    and a 3 px rim land as about 2 screen pixels each. Its drawn size is set by the grey-box, not by its pixels."""
    h, w = a.shape[:2]
    big = np.asarray(Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8), "RGBA").resize((w * upscale, int(round(h * thicken * upscale))), Image.LANCZOS)).astype(np.float32) / 255
    w = big.shape[1]
    xs = np.arange(w)[None, :].repeat(big.shape[0], 0)
    op = big[..., 3] > 0.5
    head, tail = xs >= int(w * 0.76), xs < int(w * 0.20)
    out = recolour(big, op & ~head & ~tail, BONE, 0.7, 1.08)
    out = recolour(out, op & (head | tail), DANGER, 0.7, 1.25)
    return outline(pad(out, outer_px), outer_px=outer_px, inner_px=inner_px)


def spear_head(a, outer_px=3, inner_px=2):
    """TRAP-05's head (pointing right): its blade (the right 70%) crimson, the socket bone, outlined."""
    h, w = a.shape[:2]
    xs = np.arange(w)[None, :].repeat(h, 0)
    op = a[..., 3] > 0.5
    out = recolour(a, op & (xs >= int(w * 0.30)), DANGER, 0.7, 1.25)
    out = recolour(out, op & (xs < int(w * 0.30)), BONE, 0.65, 1.05)
    return outline(pad(out, outer_px), outer_px=outer_px, inner_px=inner_px)


IRON = np.array([0.10, 0.09, 0.09], np.float32)       # a spear's shaft: near-black, like the spike blades


def spear_shaft(a, outer_px=4, inner_px=6):
    """TRAP-05's shaft (tiled along its length): near-black iron (a long pale shaft didn't stand out on a bright sky;
    HazardContrastTests), its painted grain kept as a faint value change, outlined along its top and bottom only (wrapped
    across the seam, so it tiles): the light rim carries it on dark stone and night skies."""
    out = recolour(a, a[..., 3] > 0.5, IRON, 0.7, 1.6)
    return outline(pad(out, outer_px, (True, False, True, False)), outer_px=outer_px, inner_px=inner_px, wrap_x=True)


def spike_strip(a, outer_px=3, inner_px=3, tip_fraction=0.45):
    """TRAP-04 (tiled across): the blades' top tip_fraction turns crimson (a gradient from the blade's black), then the
    double outline, wrapped across the seam. A strip has no room above its tips, so it's padded on top; the caller sizes
    it back to its tile height."""
    out = a.copy()
    op = out[..., 3] > 0.5
    rows = np.flatnonzero(op.any(axis=1))
    if rows.size:
        top, bottom = rows[0], rows[-1]
        t = np.clip(1.0 - (np.arange(out.shape[0]) - top) / max(1.0, (bottom - top) * tip_fraction), 0, 1)[:, None]
        k = np.clip(t * 1.6, 0, 1)[..., None] * op[..., None]
        out[..., :3] = out[..., :3] * (1 - k) + DANGER * k
    out = outline(pad(out, outer_px, (True, False, False, False)), outer_px=outer_px, inner_px=inner_px, wrap_x=True)
    # The rim near the tips turns a light crimson, so the tips read red on every background (the black blade and its pale
    # rim lower down keep the "black spikes" look the developer asked for in the gauntlet).
    rim = np.all(np.abs(out[..., :3] - INNER) < 1e-3, axis=-1)
    if rows.size:
        t = np.clip(1.0 - (np.arange(out.shape[0]) - (top + outer_px)) / max(1.0, (bottom - top) * tip_fraction), 0, 1)[:, None]
        k = (np.clip(t * 1.6, 0, 1) * rim)[..., None]
        out[..., :3] = out[..., :3] * (1 - k) + TIP_RIM * k
    return out


def launcher_mouth(a, box=(0.72, 0.42, 0.97, 0.57), outer_px=5, inner_px=5):
    """TRAP-01: the launcher's mouth (the slot in its right half; `box` is its x0, y0, x1, y1 as fractions of the sprite,
    measured on the painted v1 launcher) turns crimson, darker pixels more, so the slot reads as the danger point; the stone
    around it is unchanged. A disguised launcher wears its host's skin and shows this only from its fire tick (D-085)."""
    h, w = a.shape[:2]
    x0, y0, x1, y1 = int(box[0] * w), int(box[1] * h), int(round(box[2] * w)), int(round(box[3] * h))
    out = a.copy()
    region = out[y0:y1, x0:x1]
    lum = (region[..., :3] * np.array([0.3, 0.55, 0.15], np.float32)).sum(-1, keepdims=True)
    k = np.clip((0.55 - lum) / 0.35, 0.35, 1.0)
    region[..., :3] = region[..., :3] * (1 - k) + np.clip(DANGER * (0.85 + 0.6 * lum), 0, 1) * k
    # The double outline too (inside its square: the launcher fills its box, and its outer line takes the edge pixels), so a
    # launcher, honest or a disguised one's slot from its fire tick, stands out from the stone around it.
    out = out[outer_px:h - outer_px, outer_px:w - outer_px]
    return outline(pad(out, outer_px), outer_px=outer_px, inner_px=inner_px)


# ---------- keeping the danger colour out of everything else ----------

def _hsv(rgb):
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    mx, mn = np.maximum(np.maximum(r, g), b), np.minimum(np.minimum(r, g), b)
    delta = mx - mn
    safe = np.maximum(delta, 1e-6)
    hue = np.where(mx == r, ((g - b) / safe) % 6.0, np.where(mx == g, (b - r) / safe + 2.0, (r - g) / safe + 4.0)) * 60.0
    hue = np.where(delta > 1e-6, hue, 0.0)
    sat = np.where(mx > 1e-6, delta / np.maximum(mx, 1e-6), 0.0)
    return hue, sat, mx


def _rgb(hue, sat, val):
    h = (hue % 360.0) / 60.0
    c = val * sat
    x = c * (1 - np.abs(h % 2 - 1))
    z = np.zeros_like(c)
    i = np.floor(h).astype(int) % 6
    r = np.choose(i, [c, x, z, z, x, c]); g = np.choose(i, [x, c, c, x, z, z]); b = np.choose(i, [z, z, x, c, c, x])
    m = val - c
    return np.stack([r + m, g + m, b + m], axis=-1)


def set_hue(a, mask, to_hue):
    """Pixels in mask take hue `to_hue` (degrees), keeping their saturation and value. An image with nothing to move comes
    back unchanged, and the input's dtype is kept, so untouched pixels stay bit-identical."""
    if not mask.any(): return a
    out = a.copy()
    hue, sat, val = _hsv(a[..., :3])
    moved = _rgb(np.full_like(hue, to_hue), sat, val).astype(a.dtype)
    out[..., :3] = np.where(mask[..., None], moved, a[..., :3])
    return out


def out_of_danger(a, to_hue=28.0):
    """Every pixel in the danger hue (is_danger_hue) moves to `to_hue`, amber by default (warm light and gold stay
    amber/yellow, never crimson: the developer's ruling, 2026-10-03). Environment art passes through this, so only hazards
    carry the danger colour."""
    # A slightly wider band than the check (is_danger_hue's defaults), so a pixel at its edge can't round back into it when
    # it's saved as 8 bits; pixels that show only (a faint fringe never reads as a colour).
    wide = is_danger_hue(a[..., :3], hue_tolerance=24.0, min_saturation=0.52, min_value=0.28)
    return set_hue(a, wide & (a[..., 3] > 0.08), to_hue)


def reddish(a, within=40.0, min_saturation=0.2):
    """Pixels whose hue is within `within` degrees of red (0) and at least slightly saturated: a red cloth's whole range."""
    hue, sat, _ = _hsv(a[..., :3])
    diff = np.abs(((hue + 180.0) % 360.0) - 180.0)
    return (diff <= within) & (sat >= min_saturation) & (a[..., 3] > 0.02)
