"""Cut a light cat off light paper by growing the paper inward from the border (and from seed points in enclosed
gaps such as between the legs). The painted outline stops the growth, so white fur that matches the paper stays cat.
  python3 cutout_grow.py src dst size T x,y x,y ...   (seeds in src pixel coordinates)"""
import sys
import numpy as np
from PIL import Image
from scipy import ndimage

src, dst, size, T = sys.argv[1], sys.argv[2], int(sys.argv[3]), float(sys.argv[4])
seeds = [tuple(int(v) for v in s.split(",")) for s in sys.argv[5:]]
rgb = np.asarray(Image.open(src).convert("RGB")).astype(np.float32)
h, w, _ = rgb.shape
# smooth paper model from the border ring
paper0 = np.zeros((h, w), bool); paper0[:6] = paper0[-6:] = True; paper0[:, :6] = paper0[:, -6:] = True
wgt = ndimage.gaussian_filter(paper0.astype(np.float32), 40)
bg = np.dstack([ndimage.gaussian_filter(rgb[..., c] * paper0, 40) for c in range(3)]) / np.maximum(wgt, 1e-4)[..., None]
dist = np.sqrt(((rgb - bg) ** 2).sum(axis=2))
lum = rgb.mean(axis=2)
grad = np.hypot(ndimage.sobel(lum, 0), ndimage.sobel(lum, 1))
cand = (dist < T) & (grad < 60)
lab, n = ndimage.label(cand)
ids = set(np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]]))) - {0}
for x, y in seeds:
    if lab[y, x]:
        ids.add(lab[y, x])
    else:
        print(f"seed {x},{y} is not paper (dist {dist[y, x]:.0f}, grad {grad[y, x]:.0f})")
paper = np.isin(lab, list(ids))
paper = ndimage.binary_dilation(paper, iterations=1)
cat = ~paper
blobs, nb = ndimage.label(cat)
sizes = ndimage.sum(np.ones((h, w)), blobs, range(1, nb + 1))
cat = blobs == (1 + int(np.argmax(sizes)))
cat = ndimage.binary_fill_holes(cat) & ~paper  # fill specks in the fur, never re-fill seeded paper
cat = ndimage.binary_opening(cat, structure=np.ones((7, 1), bool))  # drop the thin ground-shadow line
blobs, nb = ndimage.label(cat)
sizes = ndimage.sum(np.ones((h, w)), blobs, range(1, nb + 1))
cat = blobs == (1 + int(np.argmax(sizes)))
alpha = ndimage.gaussian_filter(cat.astype(np.float32), 0.8)
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
