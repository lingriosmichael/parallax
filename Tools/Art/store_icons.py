#!/usr/bin/env python3
"""PAX-A10: the app icon and store graphics, from the developer's paintings in Art_Source/Store (never changed).

Writes the icon layers into Assets/_Game/Art/AppIcon/ (PARALLAX/Setup/App Icon gives them to Android) and the two files
uploaded by hand to Play Console into Art_Source/Store/export/.

    python3 Tools/Art/store_icons.py [mock.png]      (numpy, Pillow; mock.png: a launcher preview, for checking by eye)
"""
import os
import sys

import numpy as np
from PIL import Image, ImageDraw

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from trap_v2 import key_magenta  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
STORE = os.path.join(ROOT, "Art_Source", "Store")
ICONS = os.path.join(ROOT, "Assets", "_Game", "Art", "AppIcon")
EXPORT = os.path.join(STORE, "export")
ADAPTIVE = 432          # 108 dp at xxxhdpi
MASK_RADIUS = 144       # launchers show at most the middle 72 dp
FIT_RADIUS = 134        # the cat's outline stays inside this, so no launcher's mask cuts an ear
LEGACY = 192
PLAY = 512
FULL_FIT = 0.46         # a full-square icon: the cat's outline as a share of the side
FEATURE = (1024, 500)
MASK_ALPHA = 0.5        # counted as cat when fitting (whiskers fade past it)


def load(name):
    return np.asarray(Image.open(os.path.join(STORE, name)).convert("RGBA")).astype(np.float32) / 255.0


def enclosing_circle(mask):
    """The smallest circle around the opaque pixels, near enough (Badoiu-Clarkson): (cx, cy, r)."""
    ys, xs = np.nonzero(mask)
    pts = np.stack([xs, ys], 1).astype(np.float32)[::7]
    c = pts.mean(0)
    for i in range(1, 400):
        far = pts[np.argmax(((pts - c) ** 2).sum(1))]
        c = c + (far - c) / (i + 1)
    return c[0], c[1], float(np.sqrt(((pts - c) ** 2).sum(1).max()))


def to_image(a):
    return Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8), "RGBA")


def placed(cat, circle, size, radius):
    """The keyed cat on a clear `size` square, its enclosing circle centred, `radius` px across from that centre."""
    cx, cy, r = circle
    k = radius / r
    w, h = cat.size
    scaled = cat.resize((int(round(w * k)), int(round(h * k))), Image.LANCZOS)
    out = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    out.paste(scaled, (int(round(size / 2 - cx * k)), int(round(size / 2 - cy * k))), scaled)
    return out


def main(argv):
    os.makedirs(ICONS, exist_ok=True)
    os.makedirs(EXPORT, exist_ok=True)
    keyed = key_magenta(load("S-01_icon_foreground_v1.png"))
    circle = enclosing_circle(keyed[..., 3] > MASK_ALPHA)
    cat = Image.fromarray((np.clip(keyed, 0, 1) * 255).astype(np.uint8), "RGBA")
    sky = Image.open(os.path.join(STORE, "S-02_icon_background_v1.png")).convert("RGB")

    foreground = placed(cat, circle, ADAPTIVE, FIT_RADIUS)
    foreground.save(os.path.join(ICONS, "ICON_foreground.png"))
    sky.resize((ADAPTIVE, ADAPTIVE), Image.LANCZOS).save(os.path.join(ICONS, "ICON_background.png"))

    def full(size):
        out = sky.resize((size, size), Image.LANCZOS).convert("RGBA")
        out.alpha_composite(placed(cat, circle, size, size * FULL_FIT))
        return out

    full(LEGACY).convert("RGB").save(os.path.join(ICONS, "ICON_legacy.png"))
    disc = Image.new("L", (LEGACY * 4, LEGACY * 4), 0)
    ImageDraw.Draw(disc).ellipse([0, 0, LEGACY * 4 - 1, LEGACY * 4 - 1], fill=255)
    rounded = full(LEGACY)
    rounded.putalpha(disc.resize((LEGACY, LEGACY), Image.LANCZOS))
    rounded.save(os.path.join(ICONS, "ICON_round.png"))

    full(PLAY).save(os.path.join(EXPORT, "play_icon_512.png"))      # 32-bit, as Play asks

    art = Image.open(os.path.join(STORE, "S-03_feature_graphic_v1.png")).convert("RGB")
    w, h = art.size
    keep = int(round(w * FEATURE[1] / FEATURE[0]))
    top = (h - keep) // 2
    art.crop((0, top, w, top + keep)).resize(FEATURE, Image.LANCZOS).save(os.path.join(EXPORT, "feature_graphic_1024x500.png"))

    if len(argv) > 1:
        mock(foreground, sky, argv[1])


def mock(foreground, sky, path):
    """The adaptive layers through a circle and a rounded-square mask, at launcher sizes, on a dark and a light wallpaper."""
    sheet = Image.new("RGB", (900, 420), (28, 30, 38))
    sheet.paste((225, 228, 232), (450, 0, 900, 420))
    layered = sky.resize((ADAPTIVE, ADAPTIVE), Image.LANCZOS).convert("RGBA")
    layered.alpha_composite(foreground)
    view = layered.crop((72, 72, 360, 360))
    for side, x0 in ((0, 20), (1, 470)):
        x = x0
        for size in (192, 96, 48):
            for row, y in ((0, 20), (1, 225)):
                m = Image.new("L", (size * 4, size * 4), 0)
                d = ImageDraw.Draw(m)
                if row == 0:
                    d.ellipse([0, 0, size * 4 - 1, size * 4 - 1], fill=255)
                else:
                    d.rounded_rectangle([0, 0, size * 4 - 1, size * 4 - 1], radius=size, fill=255)
                sheet.paste(view.resize((size, size), Image.LANCZOS).convert("RGB"), (x, y), m.resize((size, size), Image.LANCZOS))
            x += size + 20
    sheet.save(path)


if __name__ == "__main__":
    main(sys.argv)
