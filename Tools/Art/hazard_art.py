"""D-295: fixed hazards and metal spikes, from the developer's paintings. Thorn clusters (HZ-01, on blue), crystal clusters (HZ-03,
on magenta) and iron blades (HZ-02, on green, replacing the steel blades under their names, so every spike strip takes them).
Keyed, toned to sit in their world, cut into pieces; ThornArt and SpikeArt lay the pieces out in each hazard.

Called by trap_process.py after spike_steel.py.
"""
import sys
from pathlib import Path

import numpy as np
from scipy import ndimage

sys.path.insert(0, str(Path(__file__).resolve().parent))
from trap_v2 import key_magenta  # noqa: E402

ROOT = Path(__file__).resolve().parents[2]
THORNS = ROOT / "Art_Source/Traps_ChatGPT/HZ-01_thorn_branches_v3.png"   # D-317: Pack J's bolder v4 read worse (27 hazards failing to v3's 16)
SPIKES = ROOT / "Art_Source/Traps_ChatGPT/HZ-02_metal_spikes_v2.png"   # D-317: v3 fixed the hinge spikes but lost as many floor strips (29 to 24)
SPIKES_BOLD = ROOT / "Art_Source/Traps_ChatGPT/HZ-02_metal_spikes_v3.png"   # D-318: so v3 is the hinge floors' own (SPIKE_bold_*)
CRYSTAL = ROOT / "Art_Source/Traps_Obsidian/HZ-03_crystal_shards_v1.png"

THORN_PPU = 340         # a cluster about 2 u wide
CRYSTAL_PPU = 447       # about 0.85 u tall
BLADE_PPU = 256         # as the steel blades: the tallest is 148 px, so SpikeArt's standard blade size holds
BLADE_TALL = 148
BLADES = 12             # as many as the steel set
BLADE_STOUT = 1.35      # a little wider than painted: a blade this thin at phone size has no room for both its dark core and its lit edges
RIM = np.array([1.0, 0.95, 0.84], np.float32)   # the sun on an iron edge (D-303: lighter: the grade lifts blacks, so on a mid tone only the light edge reaches 3:1)
SHADE_RIM = np.array([0.95, 0.94, 0.94], np.float32)   # the sky on the other. A blade has no mid-tones: bright edges read on dark stone, a near-black
                                                        # core on a bright sky, and between them it holds D-108 on the mid-tones in between
THORN_HALO_PX = 0      # D-316: a dark halo round each branch (about 1 px on a phone), so the thin pale bark reads over a bright sky too
THORN_HALO = np.array([0.1, 0.07, 0.05], np.float32)
# Gate 6: a soft near-black rim behind each lit cluster, its own sprite (THORNRIM_*), so the pale bark has dark behind it on any sky
# without darkening the branch itself. Solid for RIM_SOLID px past the bark, gone RIM_FADE px later (about 1.5 to 2.5 px on a phone).
RIM_SOLID, RIM_FADE, RIM_ALPHA = 5, 7, 0.85
BLADE_RIM = (3, 4)       # the same rim behind the iron blades (SPIKERIM_*), about as wide on a phone: a blade's pixel is larger
RIM_TINT = np.array([0.06, 0.045, 0.04], np.float32)
BLADE_AIR = 0.55         # clear space each side of a blade, over its width: SpikeLayout spaces blades by their sprites, and the painting's rhythm is open


def resize(a, w, h):
    from PIL import Image
    channels = [Image.fromarray(a[..., c]).resize((w, h), Image.LANCZOS) for c in range(4)]
    return np.dstack([np.asarray(c) for c in channels]).astype(np.float32)


def key(a, screen, near, wide):
    """Clear where channel `screen` beats the other two; what's left has no cast of it."""
    others = [c for c in range(3) if c != screen]
    top = np.maximum(a[..., others[0]], a[..., others[1]])
    out = a.copy()
    out[..., 3] = np.minimum(out[..., 3], 1 - np.clip((a[..., screen] - top - near) / wide, 0, 1))
    out[..., screen] = np.minimum(out[..., screen], top)
    return out


def thorns(load):
    """The four clusters. The pale bark is calmed, or it sparkles at phone size; the thorns' pink stays."""
    a = key(load(THORNS), 2, 0.12, 0.3)
    labels, count = ndimage.label(a[..., 3] > 0.5)
    sizes = ndimage.sum_labels(np.ones_like(labels), labels, range(1, count + 1))
    pieces = []
    for i in np.argsort(sizes)[::-1][:4]:
        mask = ndimage.binary_dilation(labels == i + 1, iterations=3 + THORN_HALO_PX)
        ys, xs = np.nonzero(mask)
        p = a[ys.min():ys.max() + 1, xs.min():xs.max() + 1].copy()
        p[..., 3] *= mask[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
        lum = p[..., :3] @ np.array([0.3, 0.59, 0.11], np.float32)
        pink = np.clip((p[..., 0] - p[..., 1]) / 0.3, 0, 1)
        p[..., :3] *= (1 - 0.1 * np.clip((lum - 0.5) / 0.4, 0, 1) * (1 - pink))[..., None]   # D-303: 0.2 kept the lit bark under 3:1 on a mid tone
        p[..., :3] = np.clip((p[..., :3] - 0.4) * 1.4 + 0.52, 0, 1)   # D-108: a thin branch needs its lights and darks apart
        # D-316: on bright skies a pale branch had nothing dark round it (HazardContrastTests: 12 levels under 30 % at 3:1).
        # D-317: off at 0 (dilating 0 times would fill all); v4's thick branches read worse with it over dark pits.
        if THORN_HALO_PX > 0:
            solid = p[..., 3] > 0.5
            halo = ndimage.binary_dilation(solid, iterations=THORN_HALO_PX) & ~solid
            p[..., :3] = np.where(halo[..., None], THORN_HALO, p[..., :3])
            p[..., 3] = np.maximum(p[..., 3], halo * 0.85)
        pieces.append(p)
    return sorted(pieces, key=lambda p: p.shape[1])


def rim(p, reach=(RIM_SOLID, RIM_FADE)):
    """A piece's dark rim, on a canvas padded by its reach on every side: the piece's foot pivot sits at (0.5, pad / height)."""
    hard, fade = reach
    pad = hard + fade
    solid = np.pad(p[..., 3] > 0.3, pad)
    out = ndimage.distance_transform_edt(~solid)
    a = np.zeros(solid.shape + (4,), np.float32)
    a[..., :3] = RIM_TINT
    a[..., 3] = RIM_ALPHA * np.clip(1 - (out - hard) / fade, 0, 1)
    return a, pad / solid.shape[0]


def crystal(load):
    """Toned down (D-295: the raw red fights the Obsidian scene), its flat bottom strip of blocks cut off, split at the valleys of its
    top edge into four clusters, whose cut sides fade, so overlapped they read as one ridge."""
    a = key_magenta(load(CRYSTAL))
    ys, _ = np.nonzero(a[..., 3] > 0.5)
    a = a[ys.min():ys.max() + 1 - 62]
    grey = a[..., :3].mean(-1, keepdims=True)
    a[..., :3] = np.clip((grey + (a[..., :3] - grey) * 0.62) * 0.72, 0, 1)
    h, w = a.shape[:2]
    top = (a[..., 3] > 0.5).argmax(axis=0).astype(np.float32)
    cuts = [0]
    for f in (0.25, 0.5, 0.75):
        lo, hi = int(w * (f - 0.08)), int(w * (f + 0.08))
        cuts.append(lo + int(np.argmax(top[lo:hi])))
    cuts.append(w)
    pieces = []
    fade = 36
    for x0, x1 in zip(cuts, cuts[1:]):
        x0, x1 = max(0, x0 - fade), min(w, x1 + fade)
        p = a[:, x0:x1].copy()
        ramp = np.clip(np.minimum(np.arange(p.shape[1]), p.shape[1] - 1 - np.arange(p.shape[1])) / fade, 0, 1)[None, :]
        p[..., 3] *= ramp
        rows = (p[..., 3] > 0.05).any(axis=1)
        pieces.append(p[int(rows.argmax()):])
    return pieces


def blades(load, source=SPIKES):
    """Each spike of the row, cut out and given what the painting lacks: a warm lit edge so it reads on dark stone, a worn surface, and a
    foot that goes dark where it enters the stone."""
    a = key(load(source), 1, 0.15, 0.3)
    rows = np.nonzero((a[..., 3] > 0.5).any(axis=1))[0]
    a = a[:rows.max() + 1]   # D-317: v3 paints its row above the canvas's foot; the feet are read 40 rows up from the blades' own base
    foot = a[-40, :, 3] > 0.5
    edges = np.nonzero(np.diff(foot.astype(int)))[0]
    runs = list(zip(edges[::2], edges[1::2]))
    tall = (a[..., 3] > 0.5).argmax(axis=0)
    highest = tall[(a[..., 3] > 0.5).any(axis=0)].min()
    out = []
    rng = np.random.default_rng(2950)
    for x0, x1 in runs[:BLADES]:
        pad = 4
        col = a[:, x0 - pad:x1 + pad + 1].copy()
        y0 = int((col[..., 3] > 0.5).any(axis=1).argmax())
        p = col[y0:]
        size = BLADE_TALL * (p.shape[0] / (a.shape[0] - highest))
        k = size / p.shape[0]
        p = resize(p, max(6, int(round(p.shape[1] * k * BLADE_STOUT))), int(round(size)))
        h, w = p.shape[:2]
        solid = p[..., 3] > 0.5
        left = solid.argmax(axis=1).astype(np.float32)
        right = w - 1 - solid[:, ::-1].argmax(axis=1).astype(np.float32)
        across = np.clip((np.arange(w, dtype=np.float32)[None, :] - left[:, None]) / np.maximum(right - left, 1)[:, None], 0, 1)
        rim = np.clip((0.33 - across) / 0.05, 0, 1)[..., None]
        far = np.clip((across - 0.79) / 0.05, 0, 1)[..., None]
        streak = 1 + 0.16 * ndimage.gaussian_filter1d(rng.normal(size=h).astype(np.float32), 5)[:, None, None] * 6
        down = (np.arange(h, dtype=np.float32) / h)[:, None, None]
        rgb = np.clip(p[..., :3] * 0.42 * streak, 0, 1)
        rgb = rgb * (1 - rim) + RIM * rim
        rgb = rgb * (1 - far) + SHADE_RIM * far
        rgb *= 1 - 0.65 * np.clip((down - 0.92) / 0.08, 0, 1)
        blade = np.dstack([rgb, p[..., 3]]).astype(np.float32)
        air = int(round(w * BLADE_AIR))
        out.append(np.pad(blade, ((0, 0), (air, air), (0, 0))))
    return out


def build(emit, load_rgba):
    for path in (THORNS, SPIKES, CRYSTAL):
        if not path.exists():
            print(f"hazard_art: {path.name} isn't there; its sprites stay as they are")
            return
    for i, p in enumerate(thorns(load_rgba)):
        emit(f"Bodies/THORN_{i}", p, pivot=(0.5, 0.0), ppu=THORN_PPU, sources=[THORNS])
        r, foot = rim(p)
        emit(f"Bodies/THORNRIM_{i}", r, pivot=(0.5, foot), normal=False, ppu=THORN_PPU, sources=[THORNS])
    for i, p in enumerate(crystal(load_rgba)):
        emit(f"Bodies/CRYSTAL_{i}", p, pivot=(0.5, 0.0), ppu=CRYSTAL_PPU, sources=[CRYSTAL])
    for i, p in enumerate(blades(load_rgba)):
        emit(f"Bodies/SPIKE_steel_{i}", p, pivot=(0.5, 0.0), ppu=BLADE_PPU, sources=[SPIKES])
        r, foot = rim(p, BLADE_RIM)
        emit(f"Bodies/SPIKERIM_steel_{i}", r, pivot=(0.5, foot), normal=False, ppu=BLADE_PPU, sources=[SPIKES])
    if SPIKES_BOLD.exists():
        for i, p in enumerate(blades(load_rgba, SPIKES_BOLD)):
            emit(f"Bodies/SPIKE_bold_{i}", p, pivot=(0.5, 0.0), ppu=BLADE_PPU, sources=[SPIKES_BOLD])
            r, foot = rim(p, BLADE_RIM)
            emit(f"Bodies/SPIKERIM_bold_{i}", r, pivot=(0.5, foot), normal=False, ppu=BLADE_PPU, sources=[SPIKES_BOLD])
