"""PAX-A14 §3 A: the consistency pass for cat_register. Offline and deterministic; pure functions on float RGBA arrays
(0..1), like cat_frames.

- Scale: a clip drawn at another size is matched to its reference by torso length (`torso_scale`): the length of the
  body's core, the silhouette eroded until the legs, tail, ears and whiskers are gone.
- Style: a clip's tones are matched to the style reference's (Walk) by luma quantiles over the opaque pixels
  (`match_style`). Each pixel's RGB is scaled by one factor, so its hue stays; alpha is never touched.
- Specks: detached islands of fur fringe (never drawn: no pixel at alpha 0.5) are removed (`drop_specks`); a detached
  piece with drawn pixels (a spark, a tail tip) stays.
- Reach: how far the lowest band of a frame reaches ahead of the pivot (`reach`), for re-registering a death clip onto
  what killed the cat.
"""

import numpy as np
from scipy import ndimage

CORE_RADIUS = 10                 # source px: erodes the legs (8-14 px wide), the tail and the ears away; the torso stays
OPAQUE = 0.9                     # the style statistics use only fully painted pixels (no fringe, no bleed)
DRAWN = 0.5                      # a pixel is drawn at alpha 0.5 (the capture harness's rule)
LUMA = np.array([0.299, 0.587, 0.114], np.float32)
QUANTILES = np.concatenate([np.linspace(0.0, 0.99, 100), np.linspace(0.991, 1.0, 10)])   # finer at the top: the eyes


# ---------- scale ----------

def core_length(a, radius=CORE_RADIUS):
    """The horizontal length of the body's core, in px: the largest region left after eroding the drawn silhouette by
    `radius`, plus the eroded margin back. 0 when nothing survives the erosion."""
    m = a[..., 3] >= DRAWN
    e = ndimage.binary_erosion(m, structure=np.ones((2 * radius + 1, 2 * radius + 1), bool))
    lab, n = ndimage.label(e)
    if n == 0:
        return 0
    sizes = ndimage.sum(e, lab, range(1, n + 1))
    xs = np.nonzero(lab == int(np.argmax(sizes)) + 1)[1]
    return int(xs.max() - xs.min() + 1 + 2 * radius)


def torso_scale(ref_frames, frames, radius=CORE_RADIUS):
    """The factor that makes `frames`' median torso length equal `ref_frames`'."""
    ref = float(np.median([core_length(f, radius) for f in ref_frames]))
    got = float(np.median([core_length(f, radius) for f in frames]))
    if ref <= 0 or got <= 0:
        raise ValueError("torso_scale: a clip has no measurable torso")
    return ref / got


# ---------- style ----------

def luma_quantiles(frames):
    """The luma at QUANTILES over every opaque pixel of the frames (one distribution per clip)."""
    px = np.concatenate([f[..., :3][f[..., 3] > OPAQUE] for f in frames])
    return np.quantile(px @ LUMA, QUANTILES).astype(np.float64)


def _increasing(q):
    """`q` made strictly increasing (ties at the ends of a distribution would make the mapping a step)."""
    out = np.maximum.accumulate(np.asarray(q, np.float64))
    return out + np.arange(len(out)) * 1e-7


def match_style(a, src_q, ref_q):
    """`a` with its luma mapped from the clip's distribution `src_q` onto the reference's `ref_q` (both from
    luma_quantiles), applied to every pixel with any alpha: RGB is scaled by one factor per pixel, so the hue stays and
    alpha is unchanged. Luma outside the source range is extended at the end slopes' ratio."""
    out = a.copy()
    on = out[..., 3] > 0
    if not on.any():
        return out
    rgb = out[..., :3][on].astype(np.float64)
    lum = rgb @ LUMA.astype(np.float64)
    s, r = _increasing(src_q), _increasing(ref_q)
    mapped = np.interp(lum, s, r)
    mapped = np.where(lum > s[-1], r[-1] * lum / max(s[-1], 1e-6), mapped)
    mapped = np.where(lum < s[0], r[0] * lum / max(s[0], 1e-6), mapped)
    factor = np.where(lum > 1e-6, mapped / np.maximum(lum, 1e-6), 1.0)
    out[..., :3][on] = np.clip(rgb * factor[:, None], 0.0, 1.0).astype(out.dtype)
    return out


# ---------- specks ----------

def _islands(a, alpha_max):
    """The labels of the detached islands never drawn (every pixel under `alpha_max`), and the label image."""
    lab, n = ndimage.label(a[..., 3] > 0, structure=np.ones((3, 3), bool))
    if n <= 1:
        return [], lab
    idx = range(1, n + 1)
    sizes = np.asarray(ndimage.sum(np.ones(lab.shape), lab, idx))
    peaks = np.asarray(ndimage.maximum(a[..., 3], lab, idx))
    body = int(np.argmax(sizes))
    return [i + 1 for i in range(n) if i != body and peaks[i] < alpha_max], lab


def detached_islands(a, alpha_max=DRAWN):
    """How many detached, never-drawn islands `a` has (8-connected, any alpha, apart from the body)."""
    return len(_islands(a, alpha_max)[0])


def drop_specks(a, alpha_max=DRAWN):
    """`a` without its detached, never-drawn islands (every channel zeroed there)."""
    labels, lab = _islands(a, alpha_max)
    out = a.copy()
    if labels:
        out[np.isin(lab, labels)] = 0.0
    return out


# ---------- reach ----------

def reach(a, pivot_x, ppu, band_px):
    """How far the drawn pixels of the lowest `band_px` rows reach ahead of the pivot (facing +1), in units."""
    m = a[..., 3] >= DRAWN
    rows = np.nonzero(m.any(axis=1))[0]
    if rows.size == 0:
        return 0.0
    band = m[max(0, rows.max() - band_px + 1):rows.max() + 1]
    return float((np.nonzero(band.any(axis=0))[0].max() + 1 - pivot_x) / ppu)
