"""D-305: Premium Pack A and B, the developer's paintings into the Golden Ruins kit (Docs/Art/PREMIUM_PACK_A_PROMPTS.md, _B_). Keyed off their
blue, matched to the kit art each sits beside (every painting came back about 1.7 times brighter than the game's stone), cut into pieces,
and added to env_kit.json. Runs after env_kit.py, which rewrites the manifest: run this again after it."""
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image
from scipy import ndimage

sys.path.insert(0, str(Path(__file__).resolve().parent))
ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT / "Art_Source/Environment/GoldenRuins"
KIT = ROOT / "Assets/_Game/Art/RealityA/Environment/Kit"
LAYERS = ROOT / "Assets/_Game/Art/RealityA/Environment/Backgrounds/Kit"
WORLD_PPU, HALF_PPU = 196.667, 98.333
OBSIDIAN_FILL = 2308
# The old Obsidian fill's colour (each channel's mean and spread), which the walls keep: fixed, as this tool overwrites that fill.
FILL_LOOK = np.dstack([np.random.default_rng(1).normal(m, s, (256, 256)) for m, s in ((0.0986, 0.0389), (0.1457, 0.0519), (0.192, 0.0602))] + [np.ones((256, 256))]).astype(np.float32)


def load(path):
    return np.asarray(Image.open(path).convert("RGBA")).astype(np.float32) / 255


def key(name):
    """Clear where blue beats red and green; what's left has no cast of it."""
    a = load(SRC / f"{name}.png")
    top = np.maximum(a[..., 0], a[..., 1])
    a[..., 3] = np.minimum(a[..., 3], 1 - np.clip((a[..., 2] - top - 0.05) / 0.25, 0, 1))
    a[..., 2] = np.minimum(a[..., 2], top * 0.9 + 0.02)
    return a


def fringe(a, px=2):
    """The keyed edge eaten back a little: the blue's blend sat in the rim."""
    solid = a[..., 3] > 0.6
    a = a.copy()
    a[..., 3] *= ndimage.binary_erosion(solid, iterations=px) | ~ndimage.binary_dilation(solid, iterations=1)
    a[..., 3] = ndimage.gaussian_filter(a[..., 3], 0.7)
    return a


def trim(a):
    ys, xs = np.nonzero(a[..., 3] > 0.02)
    return a[ys.min():ys.max() + 1, xs.min():xs.max() + 1]


def parts(a, count, axis):
    """The sheet's `count` biggest separate pieces, in reading order."""
    labels, n = ndimage.label(ndimage.binary_closing(a[..., 3] > 0.5, iterations=4))
    sizes = ndimage.sum_labels(np.ones_like(labels), labels, range(1, n + 1))
    found = []
    for i in np.argsort(sizes)[::-1][:count]:
        ys, xs = np.nonzero(labels == i + 1)
        found.append((ys.min(), ys.max() + 1, xs.min(), xs.max() + 1, i + 1))
    found.sort(key=lambda f: f[2 if axis == 1 else 0])
    out = []
    for y0, y1, x0, x1, i in found:
        p = a[y0:y1, x0:x1].copy()
        p[..., 3] *= ndimage.binary_dilation(labels[y0:y1, x0:x1] == i, iterations=2)
        out.append(p)
    return out


def match(a, ref, lift=1.0, keep=0.0, spread=1.0):
    """`a`'s colours moved to `ref`'s: each channel's mean and spread, `lift` times brighter; `keep` of the painting's own colour stays."""
    m, r = a[..., 3] > 0.5, ref[..., 3] > 0.5
    out = a.copy()
    for c in range(3):
        mine, theirs = a[..., c][m], ref[..., c][r]
        moved = (a[..., c] - mine.mean()) / max(float(mine.std()), 1e-3) * float(theirs.std()) * spread + float(theirs.mean()) * lift
        out[..., c] = np.clip(moved * (1 - keep) + a[..., c] * keep, 0, 1)
    return out


def seated(a, blur=14, pad=36, strength=0.6):
    """A piece set into stone: a soft warm shadow around its foot and right side, so it never reads as pasted on."""
    h, w = a.shape[:2]
    out = np.zeros((h + pad * 2, w + pad * 2, 4), np.float32)
    out[pad:pad + h, pad:pad + w] = a
    shade = ndimage.gaussian_filter(np.roll(out[..., 3], (10, 6), axis=(0, 1)), blur) * strength
    alpha = out[..., 3] + shade * (1 - out[..., 3])
    rgb = (out[..., :3] * out[..., 3:] + np.array([0.06, 0.04, 0.03], np.float32) * (shade * (1 - out[..., 3]))[..., None]) / np.maximum(alpha, 1e-3)[..., None]
    return np.dstack([rgb, alpha])


def resize(a, w, h):
    premult = a.copy(); premult[..., :3] *= a[..., 3:]
    ch = [np.asarray(Image.fromarray(premult[..., c]).resize((w, h), Image.LANCZOS)) for c in range(4)]
    out = np.dstack(ch).astype(np.float32)
    out[..., :3] = np.clip(out[..., :3] / np.maximum(out[..., 3:], 1e-3), 0, 1)
    return np.clip(out, 0, 1)


def fit(a, width):
    return resize(a, width, int(round(a.shape[0] * width / a.shape[1])))


def seamless(a, overlap):
    """A strip whose ends meet: its last `overlap` pixels fade into its first."""
    w = a.shape[1] - overlap
    out = a[:, :w].copy()
    t = np.linspace(0, 1, overlap, dtype=np.float32)[None, :, None]
    out[:, :overlap] = a[:, :overlap] * t + a[:, w:] * (1 - t)
    return out


def wrap(a, overlap=80):
    """A painting whose edges nearly meet, made to tile: its far edges fade into its near ones."""
    h, w = a.shape[:2]
    t = np.linspace(0, 1, overlap, dtype=np.float32)[None, :, None]
    out = a[:, :w - overlap].copy()
    out[:, :overlap] = a[:, :overlap] * t + a[:, w - overlap:] * (1 - t)
    t = np.linspace(0, 1, overlap, dtype=np.float32)[:, None, None]
    top = out[:h - overlap].copy()
    top[:overlap] = out[:overlap] * t + out[h - overlap:] * (1 - t)
    return top


def wall(upper, lower):
    """A square fill of two rows of 2:1 paintings, the lower shifted half a width. Each row tiles down on its own, so both run the fill's whole height
    and a soft, wandering line picks between them: a straight join drew a ruled line across every wall."""
    size = OBSIDIAN_FILL
    def tall(p, shift):
        row = np.roll(resize(p, size, size // 2), shift, axis=1)
        return np.concatenate([row, row], axis=0)
    a, b = tall(upper, 0), tall(lower, size // 2)
    yy, xx = np.mgrid[0:size, 0:size].astype(np.float32)
    wander = 70 * np.sin(xx * 2 * np.pi / size * 3) + 45 * np.sin(xx * 2 * np.pi / size * 7 + 1)
    edge = np.sin((yy + wander) * 2 * np.pi / size)   # > 0 in the upper row
    t = np.clip(edge * 6 + 0.5, 0, 1)
    t = (t * t * (3 - 2 * t))[..., None]
    return a * t + b * (1 - t)


def obsidian():
    """Pack F: the Obsidian Void fills and hero machines. Runs after obsidian_env.py, which writes the fills it replaces."""
    import obsidian_env
    import trap_process as tp
    src = ROOT / "Art_Source/Environment/ObsidianVoid"
    kit = ROOT / "Assets/_Game/Art/ObsidianVoid/Environment/Kit"
    ref = FILL_LOOK
    def graded(name):
        a = load(src / f"{name}.png")
        a[..., 3] = 1
        return match(wrap(a), ref, spread=1.15)
    fractured, machine = graded("OF-01_wall_fractured_v1"), graded("OF-02_wall_machine_v1")
    fills = {"A": wall(fractured, fractured[:, ::-1]), "A2": wall(machine, fractured), "A3": wall(np.roll(fractured, fractured.shape[1] // 3, axis=1), machine[:, ::-1])}
    def save(path, a):
        Image.fromarray((np.clip(a, 0, 1) * 255 + 0.5).astype(np.uint8), "RGBA").save(path)
    for k, fill in fills.items():
        save(kit / f"ENV_Fill_{k}.png", fill)
        save(kit / f"ENV_Fill_{k}_n.png", tp.normal_map(fill, strength=1.6, bevel_px=2.0))
        was, now = load(kit / f"ENV_Fill_{k}_Recess.png"), obsidian_env.recess(fill)
        lip = load(kit / f"ENV_RecessLip_{k}.png")
        save(kit / f"ENV_RecessLip_{k}.png", obsidian_env.recut(lip, was, now))
        save(kit / f"ENV_Fill_{k}_Recess.png", now)
    # ---- hero machines ----
    a = load(src / "OF-03_hero_machines_v1.png")
    top = np.maximum(a[..., 0], a[..., 2]) - a[..., 1]
    near = np.minimum(a[..., 0], a[..., 2]) - a[..., 1]
    a[..., 3] = 1 - np.clip((near - 0.12) / 0.25, 0, 1)
    a[..., 0] = np.minimum(a[..., 0], a[..., 1] + 0.1); a[..., 2] = np.minimum(a[..., 2], np.maximum(a[..., 1] * 1.5, a[..., 2] * (1 - np.clip(near * 3, 0, 1))))
    slots = []
    for i, p in enumerate(parts(a, 3, 1)):
        p = fit(fringe(trim(p)) * np.array([1.5, 1.5, 1.5, 1], np.float32), 1000)
        p = seated(p, blur=14, pad=36, strength=0.7)
        save(kit / f"ENV_HeroMachine_{i}.png", p)
        slots.append(dict(name=f"ENV_HeroMachine_{i}", file=f"ENV_HeroMachine_{i}.png", folder="Kit", width=round(p.shape[1] / WORLD_PPU, 4), height=round(p.shape[0] / WORLD_PPU, 4), kind="dressing"))
    slots.append(void_heart(src, kit, save))
    slots.append(stone_depth(kit, save))

    manifest = kit / "env_kit.json"
    data = json.loads(manifest.read_text())
    names = {s["name"] for s in slots}
    data["slots"] = [s for s in data["slots"] if s["name"] not in names] + slots
    manifest.write_text(json.dumps(data, indent=1) + "\n")
    print(f"{len(slots)} Obsidian slots written")


def void_heart(src, kit, save):
    """Pack G: the Heart of the Void (OG-01), L040's finale vista, keyed off its magenta like the hero machines; its slot."""
    a = load(src / "OG-01_void_heart_v1.png")
    # Unmixed from the magenta, not cut and eroded: the beams are a pixel or two wide and an eroded edge lost them, and a cut left
    # a pink rim on the stone. A pixel's magenta share sets its alpha; its colour is what's left with the magenta taken out.
    magenta = np.array([0.99, 0.01, 0.99], np.float32)
    share = np.clip((np.minimum(a[..., 0], a[..., 2]) - a[..., 1]) / 0.95, 0, 1)
    alpha = 1 - share
    rgb = (a[..., :3] - magenta * share[..., None]) / np.maximum(alpha, 1e-3)[..., None]
    alpha = np.where(alpha < 0.05, 0, alpha)
    # The magenta also lit the stone it sat beside (a violet cast on the tail's undersides): violet loses its red; cyan has none.
    rgb[..., 0] = np.minimum(rgb[..., 0], rgb[..., 1] * 0.9)   # the stone is teal, its red always under its green
    heart = trim(np.dstack([np.clip(rgb, 0, 1), alpha]))
    heart = resize(heart, int(round(heart.shape[1] * 1150 / heart.shape[0])), 1150)   # about 9 u tall on the far layer
    save(kit.parent / "Backgrounds/Kit/ENV_VoidHeart.png", heart)
    return dict(name="ENV_VoidHeart", file="ENV_VoidHeart.png", folder="Backgrounds/Kit", width=round(heart.shape[1] / 128, 4), height=round(heart.shape[0] / 128, 4), kind="piece")


def stone_depth(kit, save):
    """D-310: depth for L034's glass tiles on the face no cat reaches: the cyan-seamed band and hanging shards of Obsidian's own
    thick underside, less its wall of blocks above (that alone was 0.7 u of stone under a 0.3 u tile)."""
    a = load(kit / "ENV_ThickUnderShort.png")
    band = a[int(a.shape[0] * 0.455):]
    save(kit / "ENV_StoneDepth.png", band)
    return dict(name="ENV_StoneDepth", file="ENV_StoneDepth.png", folder="Kit", width=round(band.shape[1] / WORLD_PPU, 4), height=round(band.shape[0] / WORLD_PPU, 4), kind="strip")


def heart_only():
    """Pack G and the tiles' depth alone (the walls' lips are re-cut from their old fills, so obsidian() mustn't run twice)."""
    src = ROOT / "Art_Source/Environment/ObsidianVoid"
    kit = ROOT / "Assets/_Game/Art/ObsidianVoid/Environment/Kit"
    def save(path, a):
        Image.fromarray((np.clip(a, 0, 1) * 255 + 0.5).astype(np.uint8), "RGBA").save(path)
    added = [void_heart(src, kit, save), stone_depth(kit, save)]
    manifest = kit / "env_kit.json"
    data = json.loads(manifest.read_text())
    names = {s["name"] for s in added}
    data["slots"] = [s for s in data["slots"] if s["name"] not in names] + added
    manifest.write_text(json.dumps(data, indent=1) + "\n")
    print("ENV_VoidHeart and ENV_StoneDepth written")


HERO_WALLS = (("ENV_HeroDoor", "HW-04_sealed_door_v1", 950), ("ENV_HeroFrieze", "HW-05_frieze_v1", 1400),
              ("ENV_HeroArcade", "HW-06_blind_arcade_v1", 1336), ("ENV_HeroNiche", "HW-07_statue_niche_v1", 760))


def hero_walls(stone):
    """Pack A2 (D-311): four more carved walls, each matched to the wall's stone and seated like the relief. Less of their own
    colour kept than the relief's (they came back yellower), and a wider rim eaten (the blue sat deeper in their broken edges)."""
    return [(slot, seated(fit(match(fringe(trim(key(name)), 3), stone, lift=1.0, keep=0.04), width))) for slot, name, width in HERO_WALLS]


def walls_only():
    """Pack A2 alone (main() re-cuts every Pack A and B piece)."""
    stone = load(KIT / "ENV_Fill_A.png")[::4, ::4]
    added = []
    for slot, a in hero_walls(stone):
        Image.fromarray((np.clip(a, 0, 1) * 255 + 0.5).astype(np.uint8), "RGBA").save(KIT / f"{slot}.png")
        added.append(dict(name=slot, file=f"{slot}.png", folder="Kit", width=round(a.shape[1] / WORLD_PPU, 4), height=round(a.shape[0] / WORLD_PPU, 4), kind="dressing"))
    manifest = KIT / "env_kit.json"
    data = json.loads(manifest.read_text())
    names = {s["name"] for s in added}
    data["slots"] = [s for s in data["slots"] if s["name"] not in names] + added
    manifest.write_text(json.dumps(data, indent=1) + "\n")
    print(", ".join(f"{s['name']} {s['width']}x{s['height']}" for s in added))


DISTRICT_DOORS = (("ENV_DoorOvergrown", "TE-02_door_overgrown_v1", 0.14), ("ENV_DoorCitadel", "TE-03_door_citadel_v1", 0.0))


def opening(a, bright):
    """The door's lit opening: the biggest bright region, its holes filled."""
    labels, n = ndimage.label(bright)
    if n == 0: return bright
    sizes = ndimage.sum_labels(np.ones_like(labels), labels, range(1, n + 1))
    return ndimage.binary_fill_holes(labels == 1 + int(np.argmax(sizes)))


def district_doors():
    """Pack E (D-312): The Overgrown Quarter's and The High Citadel's exit doors. Each frame is matched to the wall's stone (a
    little brighter, beside the light; matched to the Gardens door's frame they stayed yellow), each opening shows the Gardens
    door's own light (theirs was a flat yellow), and each is sized so its opening is as wide as the Gardens door's: the cat's
    target reads the same in every district. TE-02's trunk ran off the painting's top: it fades out above its roots."""
    door = load(KIT / "ENV_Door.png")
    stone = load(KIT / "ENV_Fill_A.png")[::4, ::4]
    lit = lambda a: (a[..., 0] > 0.9) & (a[..., 1] > 0.78) & (a[..., 3] > 0.5)
    home = opening(door, lit(door))
    ys, xs = np.nonzero(home)
    home_light = door[ys.min():ys.max() + 1, xs.min():xs.max() + 1, :3]
    home_w = np.ptp(xs) + 1
    out = []
    for slot, name, cut in DISTRICT_DOORS:
        a = fringe(trim(key(name)), 3)
        if cut:
            a = a[int(a.shape[0] * cut):]
            fade = int(a.shape[0] * 0.1)
            a[:fade, :, 3] *= np.linspace(0, 1, fade, dtype=np.float32)[:, None] ** 1.5
            a = trim(a)
        light = opening(a, lit(a) & (a[..., 2] < 0.7))
        matched = match(np.dstack([a[..., :3], a[..., 3] * ~light]), stone, lift=1.15, keep=0.04)
        ly, lx = np.nonzero(light)
        glow = np.zeros_like(a[..., :3])
        glow[ly.min():ly.max() + 1, lx.min():lx.max() + 1] = resize(np.dstack([home_light, np.ones(home_light.shape[:2], np.float32)]), np.ptp(lx) + 1, np.ptp(ly) + 1)[..., :3]
        rgb = np.where(light[..., None], glow, matched[..., :3])
        a = np.dstack([rgb, a[..., 3]])
        k = home_w / (np.ptp(np.nonzero(light.any(axis=0))[0]) + 1)
        out.append((slot, resize(a, int(round(a.shape[1] * k)), int(round(a.shape[0] * k)))))
    return out


def doors_only():
    """Pack E's doors alone."""
    added = []
    for slot, a in district_doors():
        Image.fromarray((np.clip(a, 0, 1) * 255 + 0.5).astype(np.uint8), "RGBA").save(KIT / f"{slot}.png")
        added.append(dict(name=slot, file=f"{slot}.png", folder="Kit", width=round(a.shape[1] / WORLD_PPU, 4), height=round(a.shape[0] / WORLD_PPU, 4), kind="object"))
    manifest = KIT / "env_kit.json"
    data = json.loads(manifest.read_text())
    names = {s["name"] for s in added}
    data["slots"] = [s for s in data["slots"] if s["name"] not in names] + added
    manifest.write_text(json.dumps(data, indent=1) + "\n")
    print(", ".join(f"{s['name']} {s['width']}x{s['height']}" for s in added))


# D-317: Pack J's four Obsidian skies, each on two palettes' slots (the second mirrored), so neighbouring levels don't share one.
VOID_SKIES = (("ENV_Sky_Moon", "OV-01b_sky_shattered_moon_v1", False), ("ENV_Sky_Storm", "OV-01b_sky_shattered_moon_v1", True),
              ("ENV_Sky_Jade", "OV-01c_sky_aurora_v1", False), ("ENV_Sky_Teal", "OV-01c_sky_aurora_v1", True),
              ("ENV_Sky_Rose", "OV-01d_sky_ring_nebula_v1", False), ("ENV_Sky_Dusk", "OV-01d_sky_ring_nebula_v1", True),
              ("ENV_Sky_Dawn", "OV-01e_sky_frozen_dawn_v1", False))


def void_skies():
    """Pack J (D-317): the Obsidian skies and the streaked mist, written over their slots at the slots' own sizes (the manifest is
    unchanged). Each sky fills its plate (cropped to the plate's shape, centred); the mist's black is its transparency, and its ends
    and foot fade as the fog's did."""
    import obsidian_extra as ox
    src = ROOT / "Art_Source/Environment/ObsidianVoid"
    layers = ROOT / "Assets/_Game/Art/ObsidianVoid/Environment/Backgrounds/Kit"
    def save(path, a):
        Image.fromarray((np.clip(a, 0, 1) * 255 + 0.5).astype(np.uint8), "RGBA").save(path)
    for slot, name, mirror in VOID_SKIES:
        w, h = Image.open(layers / f"{slot}.png").size
        a = load(src / f"{name}.png")
        a[..., 3] = 1.0
        ah, aw = a.shape[:2]
        cw = min(aw, int(round(ah * w / h)))
        x0 = (aw - cw) // 2
        a = a[:, x0:x0 + cw]
        save(layers / f"{slot}.png", resize(a[:, ::-1] if mirror else a, w, h))
    w, h = Image.open(layers / "ENV_Fog.png").size
    m = load(src / "OV-03d_mist_streaks_v1.png")
    lum = m[..., :3].max(-1)
    mist = np.dstack([np.full(lum.shape + (3,), 0.92, np.float32), np.clip((lum - 0.03) / 0.6, 0, 1)])
    ys = np.nonzero((mist[..., 3] > 0.03).any(axis=1))[0]
    streaks = mist[ys.min():ys.max() + 1]
    save(layers / "ENV_Fog.png", ox.soft_band(resize(streaks, w, h), 180, 260))
    # D-318: the cloud rows as the streaks too, but in each row's own colour (the old mist's) and a third as strong: plain white at
    # half strength read as grey smoke over the sky.
    for i, slot in enumerate(("ENV_Cloud_0", "ENV_Cloud_1", "ENV_Cloud_2", "ENV_CloudsFar", "ENV_CloudsMid", "ENV_CloudsNear")):
        old = load(layers / f"{slot}.png")
        weight = old[..., 3:] + 1e-6
        colour = (old[..., :3] * weight).sum((0, 1)) / weight.sum()
        # A row already drawn as the streaks is one flat colour: keep it, so running this again changes nothing (lifting it by 1.1
        # each time drifted it brighter on every run).
        if float(old[..., :3][old[..., 3] > 0.05].std(axis=0).max()) > 0.01:
            colour = np.clip(colour * 1.1, 0, 1)
        h, w = old.shape[:2]
        band = resize(streaks[:, ::-1] if i % 2 else streaks, w, h)
        band[..., :3] = colour
        band[..., 3] *= 0.32
        save(layers / f"{slot}.png", band)
    # Gate 9: the far skyline's foot fades into the mist. It stood on a square cut, hidden by the old milky cloud rows until D-319
    # thinned them (a hard line across every Obsidian level). A fixed ramp on the canvas, taken as a minimum, so a second run changes
    # nothing: clear at 98.5 % of the height (the painting's foot), whole 17 % of the height above it.
    for slot in ("ENV_CitySkyline", "ENV_FarCluster"):
        a = load(layers / f"{slot}.png")
        h = a.shape[0]
        ramp = np.clip((h * 0.985 - np.arange(h)) / (h * 0.17), 0, 1).astype(np.float32)
        a[..., 3] = np.minimum(a[..., 3], ramp[:, None])
        save(layers / f"{slot}.png", a)
    print("Obsidian skies and fog written")


def main():
    stone = load(KIT / "ENV_Fill_A.png")[::4, ::4]
    fern = load(KIT / "ENV_Fern_0.png")
    ivy = load(KIT / "ENV_Drape_0.png")
    towers = load(LAYERS / "ENV_MidTowers.png")
    slots = []

    def put(folder, slot, a, ppu, kind):
        Image.fromarray((np.clip(a, 0, 1) * 255 + 0.5).astype(np.uint8), "RGBA").save(folder / f"{slot}.png")
        slots.append(dict(name=slot, file=f"{slot}.png", folder="Kit" if folder == KIT else "Backgrounds/Kit",
                          width=round(a.shape[1] / ppu, 4), height=round(a.shape[0] / ppu, 4), kind=kind))

    # ---- hero walls (dressing on the play layer's stone) ----
    put(KIT, "ENV_HeroRelief", seated(match(fringe(trim(key("HW-01_relief_v1"))), stone, lift=1.02, keep=0.12)), WORLD_PPU, "dressing")
    put(KIT, "ENV_HeroRoots", seated(match(fringe(trim(key("HW-03_roots_v1"))), stone, lift=1.05, keep=0.15), blur=9, pad=24, strength=0.5), WORLD_PPU, "dressing")
    for slot, a in hero_walls(stone):
        put(KIT, slot, a, WORLD_PPU, "dressing")
    for slot, a in district_doors():
        put(KIT, slot, a, WORLD_PPU, "object")
    # ---- background pieces ----
    put(LAYERS, "ENV_MidBreach", fit(match(fringe(trim(key("HW-02_breach_v1"))), towers, keep=0.2), 700), HALF_PPU, "piece")
    for i, name in enumerate(("DL-01_garden_gate_v1", "DL-02_great_fig_v1", "DL-03_summit_citadel_v1")):
        put(LAYERS, f"ENV_Landmark_{i}", fit(match(fringe(trim(key(name))), towers, keep=0.12), 860), HALF_PPU, "piece")
    # ---- plants ----
    for i, p in enumerate(parts(key("FL-01_leaf_clusters_v1"), 4, 1)):
        put(KIT, f"ENV_Leaf_{i}", fit(match(fringe(trim(p)), fern, keep=0.35), 300), WORLD_PPU, "dressing")
    for i, p in enumerate(parts(key("FL-02_ivy_curtains_v1"), 4, 1)):
        put(KIT, f"ENV_Ivy_{i}", fit(match(fringe(trim(p)), ivy, keep=0.4), 240), WORLD_PPU, "dressing")
    for i, p in enumerate(parts(key("FL-03_ledge_roots_v1"), 3, 1)):
        put(KIT, f"ENV_Root_{i}", fit(match(fringe(trim(p)), stone, lift=1.05, keep=0.15), 300), WORLD_PPU, "dressing")
    # ---- the bronze presses (High Citadel, honest movers only): the bronze alone, over the level's own stone (its painted stone came out an orange slab) ----
    for i, p in enumerate(parts(key("SB-02_bronze_faces_v2"), 3, 1)):
        p = trim(p)
        top, low = p[..., :3].max(-1), p[..., :3].min(-1)
        cool = ((top - low) / np.maximum(top, 1e-3) < 0.66) & (p[..., 3] > 0.5)
        # Straight-edged straps: a mask by colour alone tore their edges.
        h, w = cool.shape
        wide = lambda run: ndimage.binary_opening(run, structure=np.ones(30, bool))   # a band, not a joint between stones
        keep = wide(cool.mean(axis=1) > 0.5)[:, None] | wide(cool.mean(axis=0) > 0.5)[None, :]
        if i == 1:   # the boss and its cross
            yy, xx = np.mgrid[0:h, 0:w]
            dx, dy = (xx - w / 2) / w, (yy - h / 2) / h
            keep |= (dx ** 2 + dy ** 2 < 0.275 ** 2) | ((np.abs(dx) < 0.07) & (np.abs(dy) < 0.4)) | ((np.abs(dy) < 0.07) & (np.abs(dx) < 0.4))
        p[..., 3] *= ndimage.gaussian_filter(keep.astype(np.float32), 1.5)
        put(KIT, f"ENV_PressFace_{i}", fit(p * np.array([1.3, 1.24, 1.16, 1], np.float32), 400), WORLD_PPU, "dressing")
    # ---- platform undersides: the kit's strips, repainted under their own names (same size, so nothing else changes) ----
    strips = parts(key("SB-01_slab_undersides_v2"), 4, 0)
    for slot, strip, (w, h) in (("ENV_ThickUnderShort", strips[1], (2308, 313)), ("ENV_ThickUnder", strips[2], (2308, 376))):
        s = match(fringe(trim(strip)), stone, lift=1.0, keep=0.15)
        s = s[int(s.shape[0] * 0.12):]   # less its lit top course, which doubled the platform's own edge
        overlap = 160
        s = seamless(resize(s, w + overlap, min(h, int(s.shape[0] * (w + overlap) / s.shape[1]))), overlap)
        canvas = np.zeros((h, w, 4), np.float32)
        canvas[:s.shape[0]] = s
        Image.fromarray((canvas * 255 + 0.5).astype(np.uint8), "RGBA").save(KIT / f"{slot}.png")

    manifest = KIT / "env_kit.json"
    data = json.loads(manifest.read_text())
    names = {s["name"] for s in slots}
    data["slots"] = [s for s in data["slots"] if s["name"] not in names] + slots
    manifest.write_text(json.dumps(data, indent=1) + "\n")
    print(f"{len(slots)} slots written")
    return 0


if __name__ == "__main__":
    sys.exit(void_skies() or 0 if "--void-skies" in sys.argv else doors_only() or 0 if "--doors" in sys.argv else walls_only() or 0 if "--walls" in sys.argv else heart_only() or 0 if "--heart" in sys.argv else obsidian() or 0 if "--obsidian" in sys.argv else main())
