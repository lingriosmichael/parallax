"""Cut a cat out of the character sheet crop: colour distance from the paper, largest blob only, padded square RGBA."""
import sys
import numpy as np
from PIL import Image
from scipy import ndimage

src, dst, size = sys.argv[1], sys.argv[2], int(sys.argv[3])
rgb = np.asarray(Image.open(src).convert("RGB")).astype(np.float32)
h, w, _ = rgb.shape
# Paper colour per row from the left and top margins (the sheet has a soft gradient).
bg = np.median(rgb[:, :8, :], axis=1, keepdims=True)
dist = np.sqrt(((rgb - bg) ** 2).sum(axis=2))
alpha = np.clip((dist - 22) / 70, 0, 1)
blobs, n = ndimage.label(alpha > 0.35)
sizes = ndimage.sum(np.ones_like(alpha), blobs, range(1, n + 1))
keep = blobs == (1 + int(np.argmax(sizes)))
keep = ndimage.binary_dilation(keep, iterations=3)
alpha = alpha * keep
alpha[alpha < 0.08] = 0
# Un-premultiply the paper out of soft edges so they don't carry a light halo.
a = alpha[..., None]
fg = np.where(a > 0.05, (rgb - (1 - a) * bg) / np.maximum(a, 0.05), rgb)
out = np.dstack([np.clip(fg, 0, 255), alpha * 255]).astype(np.uint8)
img = Image.fromarray(out, "RGBA")
img = img.crop(img.getbbox())
side = int(max(img.size) * 1.12)
canvas = Image.new("RGBA", (side, side), (0, 0, 0, 0))
canvas.paste(img, ((side - img.width) // 2, side - img.height - int(side * 0.06)))
canvas = canvas.resize((size, size), Image.LANCZOS)
canvas.save(dst)
print(dst, img.size, "->", canvas.size)
