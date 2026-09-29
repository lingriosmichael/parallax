"""Cut a light cat off light paper: smooth 2D paper model (normalised blur of non-cat pixels), colour distance, largest blob."""
import sys
import numpy as np
from PIL import Image
from scipy import ndimage

src, dst, size = sys.argv[1], sys.argv[2], int(sys.argv[3])
lo, hi = float(sys.argv[4]), float(sys.argv[5])
rgb = np.asarray(Image.open(src).convert("RGB")).astype(np.float32)
paper = np.ones(rgb.shape[:2], bool)
for _ in range(3):
    w = ndimage.gaussian_filter(paper.astype(np.float32), 25)
    bg = np.dstack([ndimage.gaussian_filter(rgb[..., c] * paper, 25) for c in range(3)]) / np.maximum(w, 1e-3)[..., None]
    dist = np.sqrt(((rgb - bg) ** 2).sum(axis=2))
    paper = dist < lo
alpha = np.clip((dist - lo) / (hi - lo), 0, 1)
raw = ndimage.binary_opening(alpha > 0.3, iterations=2)  # drops the thin ground-shadow line that closes the legs off
filled = ndimage.binary_fill_holes(raw)
holes, nh = ndimage.label(filled & ~raw)
hole_sizes = ndimage.sum(np.ones_like(alpha), holes, range(1, nh + 1))
solid = filled  # with the ground line gone, the gap between the legs is open to the outside and stays empty
blobs, n = ndimage.label(solid)
sizes = ndimage.sum(np.ones_like(alpha), blobs, range(1, n + 1))
keep = blobs == (1 + int(np.argmax(sizes)))
# inside the silhouette the cat is opaque (white fur is close to the paper colour); soft only at the rim
inner = ndimage.binary_erosion(keep, iterations=3)
alpha = np.where(inner, 1.0, alpha * keep)
alpha[alpha < 0.08] = 0
a = alpha[..., None]
fg = np.where(a > 0.05, (rgb - (1 - a) * bg) / np.maximum(a, 0.05), rgb)
out = np.dstack([np.clip(fg, 0, 255), alpha * 255]).astype(np.uint8)
img = Image.fromarray(out, "RGBA")
img = img.crop(img.getbbox())
side = int(max(img.size) * 1.12)
canvas = Image.new("RGBA", (side, side), (0, 0, 0, 0))
canvas.paste(img, ((side - img.width) // 2, side - img.height - int(side * 0.06)))
canvas.resize((size, size), Image.LANCZOS).save(dst)
print(dst, img.size)
