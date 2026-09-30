"""PAX-A14 §3 B: in-between frames from the existing Cat A frames, and the seam metric. Offline and deterministic; pure
functions on float RGBA arrays (0..1), like cat_frames.

- The seam metric (`seam_phone_px`): the silhouette centroid's step between two frames, each measured from its own
  pivot, in phone px (80 px per unit): the metric the V07 capture harness calls a pop. A pixel is drawn at alpha 0.5.
- Techniques, simplest first: re-use a frame; `rotate_about` a point (premultiplied, so edges don't darken);
  `lower_body`, a vertical warp that lowers or raises the body over planted paws (the legs bend, the columns stay);
  `splice`, two frames joined along a feathered vertical seam.
- `best_rotation` finds the turn between two cels; `leg_overlap` how well two frames' legs match (the Walk↔Run switch).
"""

import numpy as np
from PIL import Image

PHONE_PPU = 80.0
DRAWN = 0.5


# ---------- the seam metric ----------

def centroid(a):
    """The drawn silhouette's centroid (x, y) in px, pixel centres at +0.5."""
    ys, xs = np.nonzero(a[..., 3] >= DRAWN)
    if xs.size == 0:
        return 0.0, 0.0
    return float(xs.mean()) + 0.5, float(ys.mean()) + 0.5


def seam_phone_px(a, pivot_a, b, pivot_b, ppu):
    """The centroid step from frame `a` to frame `b`, each relative to its own pivot (px), in phone px."""
    (ax, ay), (bx, by) = centroid(a), centroid(b)
    dx, dy = (bx - pivot_b[0]) - (ax - pivot_a[0]), (by - pivot_b[1]) - (ay - pivot_a[1])
    return float(np.hypot(dx, dy) / ppu * PHONE_PPU)


# ---------- resampling helpers ----------

def _premultiply(a):
    out = a.astype(np.float32).copy()
    out[..., :3] *= out[..., 3:4]
    return out


def _unpremultiply(p):
    out = p.copy()
    alpha = out[..., 3:4]
    out[..., :3] = np.where(alpha > 1e-6, out[..., :3] / np.maximum(alpha, 1e-6), 0.0)
    out[..., 3] = np.clip(out[..., 3], 0.0, 1.0)
    out[out[..., 3] < 1.0 / 255.0] = 0.0
    return np.clip(out, 0.0, 1.0)


# ---------- techniques ----------

def rotate_about(a, centre, degrees):
    """`a` turned by `degrees` (counter-clockwise on screen) about `centre` (x, y px), bicubic on premultiplied colour."""
    p = _premultiply(a)
    channels = [np.asarray(Image.fromarray(p[..., c], "F").rotate(degrees, resample=Image.BICUBIC, center=centre))
                for c in range(4)]
    return _unpremultiply(np.clip(np.stack(channels, axis=-1), 0.0, None))


def _mask_centre(a):
    ys, xs = np.nonzero(a[..., 3] >= DRAWN)
    return float(xs.mean()) + 0.5, float(ys.mean()) + 0.5


def _shift(a, dx, dy):
    out = np.zeros_like(a)
    h, w = a.shape[:2]
    sx0, sy0, sx1, sy1 = max(0, -dx), max(0, -dy), min(w, w - dx), min(h, h - dy)
    if sx1 > sx0 and sy1 > sy0:
        out[sy0 + dy:sy1 + dy, sx0 + dx:sx1 + dx] = a[sy0:sy1, sx0:sx1]
    return out


def best_rotation(a, b, lo=-180.0, hi=180.0, step=2.0):
    """The turn (degrees) that best maps `a` onto `b`: `a` rotated about its silhouette centre and moved onto `b`'s,
    scored by the drawn masks' IoU."""
    ca, cb = _mask_centre(a), _mask_centre(b)
    mb = b[..., 3] >= DRAWN
    best, best_iou = 0.0, -1.0
    for deg in np.arange(lo, hi + 1e-9, step):
        r = _shift(rotate_about(a, ca, float(deg)), int(round(cb[0] - ca[0])), int(round(cb[1] - ca[1])))
        ma = r[..., 3] >= DRAWN
        union = np.logical_or(ma, mb).sum()
        iou = np.logical_and(ma, mb).sum() / union if union else 0.0
        if iou > best_iou + 1e-9:
            best, best_iou = float(deg), iou
    return best


def lower_body(a, paw_row, drop_px, leg_px):
    """A vertical warp: each row moves down by drop_px x its height above `paw_row` over `leg_px` (clamped to 0..1), so
    the paws stay planted, the legs bend (or stretch, for a negative drop) and everything above the legs moves down as
    one. Columns never move. Rows are resampled linearly on premultiplied colour."""
    h = a.shape[0]
    y_in = np.arange(h, dtype=np.float64) + 0.5
    d = drop_px * np.clip((paw_row + 0.5 - y_in) / float(leg_px), 0.0, 1.0)
    y_out = y_in + d
    if np.any(np.diff(y_out) <= 0):
        raise ValueError("lower_body: the drop folds the legs over (|drop_px| must stay under leg_px)")
    src = np.interp(np.arange(h, dtype=np.float64) + 0.5, y_out, y_in) - 0.5   # the input row each output row samples
    p = _premultiply(a)
    y0 = np.clip(np.floor(src).astype(int), 0, h - 1)
    y1 = np.clip(y0 + 1, 0, h - 1)
    t = np.clip(src - np.floor(src), 0.0, 1.0)[:, None, None].astype(np.float32)
    outside = (src < -0.5) | (src > h - 0.5)
    out = p[y0] * (1.0 - t) + p[y1] * t
    out[outside] = 0.0
    return _unpremultiply(out)


def splice(a, b, seam_x, feather_px):
    """`a` left of the vertical seam at `seam_x`, `b` right of it, blended linearly over `feather_px` (premultiplied)."""
    w = a.shape[1]
    x = np.arange(w, dtype=np.float32) + 0.5
    t = np.clip((x - (seam_x - feather_px / 2.0)) / max(feather_px, 1e-6), 0.0, 1.0)[None, :, None]
    return _unpremultiply(_premultiply(a) * (1.0 - t) + _premultiply(b) * t)


def leg_overlap(a, pivot_a, b, pivot_b, band_px):
    """The IoU of the two frames' drawn legs (the `band_px` rows above each pivot's paw row), aligned at the pivots."""
    def band(f, pivot):
        m = f[..., 3] >= DRAWN
        py, px = int(round(pivot[1])), int(round(pivot[0]))
        rows = m[max(0, py - band_px):py + 1]
        return rows, px
    ma, xa = band(a, pivot_a)
    mb, xb = band(b, pivot_b)
    h = min(ma.shape[0], mb.shape[0])
    ma, mb = ma[-h:], mb[-h:]
    left = max(xa, xb)
    width = max(ma.shape[1] - xa, mb.shape[1] - xb) + left
    ca, cb = np.zeros((h, width), bool), np.zeros((h, width), bool)
    ca[:, left - xa:left - xa + ma.shape[1]] = ma
    cb[:, left - xb:left - xb + mb.shape[1]] = mb
    union = np.logical_or(ca, cb).sum()
    return float(np.logical_and(ca, cb).sum() / union) if union else 0.0


def roll_inbetweens(a, b, n):
    """`n` cels between two cels of a roll: `a` turned by k/(n+1) of the turn that maps it onto `b` (best_rotation) about
    its silhouette centre, and moved k/(n+1) of the way to `b`'s centre (k = 1..n). Real drawing, turned: no blend."""
    turn = best_rotation(a, b, step=3.0)
    (ax, ay), (bx, by) = _mask_centre(a), _mask_centre(b)
    out = []
    for k in range(1, n + 1):
        t = k / (n + 1.0)
        r = rotate_about(a, (ax, ay), turn * t)
        out.append(_shift(r, int(round((bx - ax) * t)), int(round((by - ay) * t))))
    return out


def contact_frame(a, paw_row, raise_px, leg_px):
    """A landing's contact frame: the crouch `a` with its legs part extended, the body `raise_px` higher over the same
    planted paws (lower_body with a negative drop): the pose between the fall and the compression."""
    return lower_body(a, paw_row, -raise_px, leg_px)
