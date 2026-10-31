"""PAX-108 amendment 2 (D-280): draws the title's gold line art into Assets/_Game/Art/UI/Title/. Everything is drawn here, nothing is generated
by a paid service: thin rings and ticks (the sigil, after the banner emblem in the key art), a tapering flourish, a chamfered button plate, a
soft glow, a light shaft, and the vignette and scrim. White lines on alpha, tinted in Unity, except the plate and the darkening layers.

    python3 Tools/Art/title_ui.py      (numpy and Pillow)
"""
import math, os
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, "Assets", "_Game", "Art", "UI", "Title")
SS = 4     # drawn this much larger, then reduced, for clean thin lines


def canvas(w, h): return Image.new("L", (w * SS, h * SS), 0)


def finish(mask, w, h):
    return mask.resize((w, h), Image.LANCZOS)


def save_white(mask, name):
    a = np.array(mask)
    rgba = np.dstack([np.full(a.shape, 255, "uint8")] * 3 + [a])
    Image.fromarray(rgba, "RGBA").save(os.path.join(OUT, name))


def circle(d, c, r, width):
    d.ellipse([c - r, c - r, c + r, c + r], outline=255, width=width)


def polar(c, r, deg):
    t = math.radians(deg)
    return c + r * math.cos(t), c + r * math.sin(t)


def sigil_outer(size=1400):
    img = canvas(size, size); d = ImageDraw.Draw(img); c = size * SS / 2; R = c * 0.97
    circle(d, c, R, 3 * SS)
    circle(d, c, R * 0.965, 1 * SS)
    for i in range(120):
        deg = i * 3
        long_ = i % 10 == 0
        mid = i % 5 == 0
        inner = R * (0.905 if long_ else 0.93 if mid else 0.945)
        d.line([polar(c, R * 0.965, deg), polar(c, inner, deg)], fill=255, width=(2 if long_ else 1) * SS)
    circle(d, c, R * 0.89, 1 * SS)
    for i in range(12):                      # small diamonds on the band
        deg = i * 30 + 15
        x, y = polar(c, R * 0.86, deg); s = R * 0.012
        d.polygon([(x - s, y), (x, y - s * 1.8), (x + s, y), (x, y + s * 1.8)], fill=255)
    circle(d, c, R * 0.83, 1 * SS)
    save_white(finish(img, size, size), "TUI_SigilOuter.png")


def sigil_inner(size=1000):
    img = canvas(size, size); d = ImageDraw.Draw(img); c = size * SS / 2; R = c * 0.97
    circle(d, c, R, 2 * SS)
    for turn in (0, 45):                     # the banner's star: two squares
        pts = [polar(c, R * 0.985, turn + i * 90) for i in range(4)]
        d.line(pts + [pts[0]], fill=255, width=2 * SS, joint="curve")
    circle(d, c, R * 0.54, 1 * SS)
    for i in range(16):
        d.line([polar(c, R * 0.54, i * 22.5), polar(c, R * 0.46, i * 22.5)], fill=255, width=1 * SS)
    circle(d, c, R * 0.46, 1 * SS)
    for i in range(4):
        pts = [polar(c, R * 0.46, 45 + i * 90 - 14), polar(c, R * 0.72, 45 + i * 90), polar(c, R * 0.46, 45 + i * 90 + 14)]
        d.line(pts, fill=255, width=1 * SS)
    save_white(finish(img, size, size), "TUI_SigilInner.png")


def flourish(w=1400, h=96):
    img = canvas(w, h); d = ImageDraw.Draw(img); cx, cy = w * SS / 2, h * SS / 2
    half = w * SS * 0.49; gap = 44 * SS
    for side in (-1, 1):
        d.polygon([(cx + side * gap, cy - 2 * SS), (cx + side * half, cy), (cx + side * gap, cy + 2 * SS)], fill=255)
        x = cx + side * (gap - 16 * SS); s = 5 * SS
        d.polygon([(x - s, cy), (x, cy - s), (x + s, cy), (x, cy + s)], fill=255)
    s = 16 * SS
    d.polygon([(cx - s * 1.5, cy), (cx, cy - s * 1.5), (cx + s * 1.5, cy), (cx, cy + s * 1.5)], outline=255, width=3 * SS)
    d.polygon([(cx - s * 0.6, cy), (cx, cy - s * 0.6), (cx + s * 0.6, cy), (cx, cy + s * 0.6)], fill=255)
    save_white(finish(img, w, h), "TUI_Flourish.png")


def chamfered(w, h, inset, cut):
    return [(inset + cut, inset), (w - inset - cut, inset), (w - inset, inset + cut), (w - inset, h - inset - cut),
            (w - inset - cut, h - inset), (inset + cut, h - inset), (inset, h - inset - cut), (inset, inset + cut)]


def plate(w=512, h=180):
    fill = Image.new("L", (w * SS, h * SS), 0); ImageDraw.Draw(fill).polygon(chamfered(w * SS, h * SS, 4 * SS, 22 * SS), fill=255)
    line = canvas(w, h); d = ImageDraw.Draw(line)
    pts = chamfered(w * SS, h * SS, 4 * SS, 22 * SS); d.line(pts + pts[:2], fill=255, width=3 * SS, joint="curve")
    pts = chamfered(w * SS, h * SS, 13 * SS, 26 * SS); d.line(pts + pts[:2], fill=120, width=1 * SS, joint="curve")
    fill, line = np.array(finish(fill, w, h)) / 255.0, np.array(finish(line, w, h)) / 255.0
    y = np.linspace(0, 1, h)[:, None]
    dark = np.dstack([0.13 - 0.07 * y, 0.08 - 0.05 * y, 0.04 - 0.025 * y])
    dark = np.repeat(dark, w, axis=1)
    gold = np.dstack([1.0 - 0.1 * y, 0.84 - 0.2 * y, 0.5 - 0.22 * y])
    gold = np.repeat(gold, w, axis=1)
    rgb = dark * (1 - line[..., None]) + gold * line[..., None]
    alpha = np.maximum(fill * 0.93, line)
    Image.fromarray((np.dstack([rgb, alpha]) * 255).clip(0, 255).astype("uint8"), "RGBA").save(os.path.join(OUT, "TUI_Plate.png"))


def soft(size=256):
    y, x = np.mgrid[-1:1:size * 1j, -1:1:size * 1j]
    a = np.clip(1 - np.sqrt(x * x + y * y), 0, 1) ** 2.2
    save_white(Image.fromarray((a * 255).astype("uint8")), "TUI_Soft.png")


def ray(w=256, h=1024):
    v, u = np.mgrid[0:1:h * 1j, -1:1:w * 1j]
    spread = 0.12 + 0.88 * v
    a = np.exp(-(u / (spread * 0.72)) ** 2 * 2.4) * (1 - v) ** 1.3 * np.clip(v / 0.05, 0, 1)
    save_white(Image.fromarray((a * 255).astype("uint8")).filter(ImageFilter.GaussianBlur(3)), "TUI_Ray.png")


def foliage(name, w, h, envelope, seed):
    """Fine, pointed leaves in near silhouette with a warm edge, as the key art frames its corners. They fill the space under
    envelope(x, 0 to 1), the height as a fraction of h, and stop short of the sprite's edges, so no leaf is ever cut by its rectangle."""
    rng = np.random.default_rng(seed)
    def leaves(count, size, tall):
        img = Image.new("L", (w * 2, h * 2), 0); d = ImageDraw.Draw(img)
        for _ in range(count):
            u = rng.uniform(0, 1); top = envelope(u) * tall
            if top < 0.04: continue
            x = u * w * 2; y = (1 - rng.uniform(0, 1) ** 0.6 * top) * h * 2
            L = size * 2 * rng.uniform(0.6, 1.25) * min(1.0, 0.45 + top * 1.6); W = L * rng.uniform(0.13, 0.2)
            t = math.radians(-90 + rng.uniform(-62, 62))
            c, s_ = math.cos(t), math.sin(t)
            side = [(x + c * k / 12 * L - s_ * math.sin(k / 12 * math.pi) ** 0.75 * W, y + s_ * k / 12 * L + c * math.sin(k / 12 * math.pi) ** 0.75 * W) for k in range(13)]
            back = [(x + c * k / 12 * L + s_ * math.sin(k / 12 * math.pi) ** 0.75 * W, y + s_ * k / 12 * L - c * math.sin(k / 12 * math.pi) ** 0.75 * W) for k in range(12, -1, -1)]
            d.polygon(side + back, fill=255)
        return img.resize((w, h), Image.LANCZOS)
    far = np.array(leaves(420, 62, 1.0).filter(ImageFilter.GaussianBlur(2.6))) / 255.0
    near_mask = leaves(300, 74, 0.72).filter(ImageFilter.GaussianBlur(1.3))
    near = np.array(near_mask) / 255.0
    rim = np.clip(np.array(near_mask.filter(ImageFilter.GaussianBlur(5))) / 255.0 - near, 0, 1)
    edge = np.clip(np.arange(h)[:, None] / 30.0, 0, 1) * np.ones((h, w))
    shade_far = np.array([0.17, 0.115, 0.05]); shade_near = np.array([0.07, 0.047, 0.026]); warm = np.array([0.8, 0.54, 0.18])
    alpha = np.maximum(far * 0.95, near) * edge
    mix = (near / np.maximum(np.maximum(far, near), 1e-3))[..., None]
    rgb = shade_far[None, None] * (1 - mix) + shade_near[None, None] * mix + warm[None, None] * rim[..., None] * 0.5
    Image.fromarray((np.dstack([rgb.clip(0, 1), alpha]) * 255).astype("uint8"), "RGBA").save(os.path.join(OUT, name))


def darkening(a, name):
    h, w = a.shape
    rgba = np.zeros((h, w, 4)); rgba[..., 0] = 0.07; rgba[..., 1] = 0.04; rgba[..., 2] = 0.025; rgba[..., 3] = a
    Image.fromarray((rgba * 255).astype("uint8"), "RGBA").save(os.path.join(OUT, name))


def vignette(w=512, h=288):
    y, x = np.mgrid[-1:1:h * 1j, -1:1:w * 1j]
    d = np.sqrt((x * 0.92) ** 2 + (y * 0.95) ** 2)
    darkening(np.clip((d - 0.55) / 0.85, 0, 1) ** 1.7 * 0.92, "TUI_Vignette.png")


def scrim(h=512):
    v = np.linspace(0, 1, h)[:, None]
    darkening(np.repeat(np.clip((v - 0.2) / 0.8, 0, 1) ** 1.3 * 0.92, 8, axis=1), "TUI_Scrim.png")


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    sigil_outer(); sigil_inner(); flourish(); plate(); soft(); ray()
    # Left: low under the cat, up at the ledge's cut (about 0.56 across), then away. Right: a low corner.
    foliage("TUI_FoliageL.png", 1200, 400, lambda u: 0.34 + 0.56 * math.exp(-((u - 0.58) / 0.08) ** 2) if u < 0.58 else max(0.0, 0.9 * (1 - (u - 0.58) / 0.32)) ** 1.3, 5)
    foliage("TUI_FoliageR.png", 900, 340, lambda u: max(0.0, 0.92 * (1 - u / 0.88)) ** 0.8, 8)
    vignette(); scrim()
    for f in sorted(os.listdir(OUT)):
        if f.endswith(".png"): print(f, os.path.getsize(os.path.join(OUT, f)) // 1024, "KB")
