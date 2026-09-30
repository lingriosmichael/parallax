"""PAX-A08 Stage 0: the review sheet and the climb previews for cat_register (PAX-A08 §3.5). Every sheet has its own
cell and records where the shared world pivot falls in it, so frames are drawn by their pivot."""

import io
import math

import numpy as np
from PIL import Image, ImageDraw

from cat_frames import COLLIDER_H, COLLIDER_W, REPO, cleanup, clip_frames, resize

PHONE = 90.0                      # px per unit at phone scale (a Pixel 8a landscape shows about 12 units)
BG = (236, 209, 158, 255)         # the styleframe's warm haze
INK = (40, 28, 18, 255)
RED = (180, 20, 20, 255)
FLOOR = (185, 140, 80, 255)
CEIL = (150, 108, 60, 255)
COLLIDER = (0, 150, 255, 255)
PAWLINE = (220, 40, 40, 255)


def asset_value(path, key, default):
    import re
    try:
        m = re.search(rf"{key}: ([-\d.]+)", (REPO / path).read_text())
        return float(m.group(1)) if m else default
    except OSError:
        return default


def to_image(a):
    return Image.fromarray((np.clip(a, 0, 1) * 255.0 + 0.5).astype(np.uint8), "RGBA")


def scaled(frame, k):
    h, w = frame.shape[:2]
    return to_image(resize(frame, max(1, round(w * k)), max(1, round(h * k))))


class Pen:
    """Draws a frame by its pivot, at k canvas px per sprite px, with the collider (1.0 x 0.56) and the paw line."""

    def __init__(self, ppu, k):
        self.ppu, self.k = ppu, k

    def cat(self, canvas, frame, pivot, at, flip_x=False, rotate=False, collider=True):
        k = self.k
        h, w = frame.shape[:2]
        f = frame[:, ::-1] if flip_x else frame
        px = w - pivot[0] if flip_x else pivot[0]
        img = scaled(f, k)
        if rotate:
            img = img.rotate(180)
            ox, oy = at[0] - (w - px) * k, at[1] - (h - pivot[1]) * k
        else:
            ox, oy = at[0] - px * k, at[1] - pivot[1] * k
        canvas.alpha_composite(img, (int(round(ox)), int(round(oy))))
        if collider:
            d = ImageDraw.Draw(canvas)
            cw, ch = COLLIDER_W * self.ppu * k, COLLIDER_H * self.ppu * k
            y0, y1 = (at[1], at[1] + ch) if rotate else (at[1] - ch, at[1])
            d.rectangle([at[0] - cw / 2, y0, at[0] + cw / 2, y1], outline=COLLIDER, width=2)
            d.line([at[0] - cw / 2 - 6, at[1], at[0] + cw / 2 + 6, at[1]], fill=PAWLINE, width=1)


def slot_rows(sheets, slot_manifest, pen, width):
    rows = []
    for e in slot_manifest:
        sh = sheets[e["slot"]]
        w, h = (round(v * pen.k) for v in sh["cell"])
        per_row = max(1, (width - 200) // (w + 4))
        lines = math.ceil(len(sh["frames"]) / per_row)
        im = Image.new("RGBA", (width, max(110, lines * (h + 4) + 6)), BG)
        d = ImageDraw.Draw(im)
        info = [e["slot"], f'{e["sourceClip"]} f{e["sourceFrames"][0]}-{e["sourceFrames"][-1]}',
                f'{e["frames"]} frames, {e["role"]}, cell {e["cell"][0]}x{e["cell"][1]}']
        if e.get("loop"):
            lo, hi = e["loopStepIoU"]
            info.append(f'loop /{e["loopStride"]}: close {e["loopCloseIoU"]:.2f}, steps {lo:.2f}-{hi:.2f}'
                        + (" FLAG" if e["loopFlag"] else ""))
        if e["clippedPx"]:
            info.append(f'clipped {e["clippedPx"]} px')
        if e["sourceCutAtCellEdge"]:
            info.append("source clip cut at its own edge")
        if e["scale"] != 1.0:
            info.append(f'scale {e["scale"]:.3f}')
        if e["vineKeyed"]:
            info.append("vine keyed")
        if not e["wired"]:
            info.append("NOT WIRED (imported only)")
        for i, t in enumerate(info):
            d.text((6, 6 + 12 * i), t, fill=RED if ("FLAG" in t or "clipped" in t) else INK)
        for i, f in enumerate(sh["frames"]):
            x, y = 200 + (i % per_row) * (w + 4), 3 + (i // per_row) * (h + 4)
            pen.cat(im, f, sh["pivot"], (x + sh["pivot"][0] * pen.k, y + sh["pivot"][1] * pen.k))
        rows.append(im)
    return rows


def gap_panel(items, pen, gap_units, title, flipped, horizontal=False):
    ppu, k = pen.ppu, pen.k
    step = 1.9 * ppu * k
    w, h = round(len(items) * step) + 40, round(2.1 * ppu * k)
    im = Image.new("RGBA", (w, h), BG)
    d = ImageDraw.Draw(im)
    floor_y = round(1.45 * ppu * k)
    d.rectangle([0, floor_y, w, h], fill=FLOOR)
    if horizontal:
        half, wall = gap_units * ppu * k / 2, 0.4 * ppu * k
        for i in range(len(items)):
            cx = 20 + (i + 0.5) * step
            d.rectangle([cx - half - wall, floor_y - 1.2 * ppu * k, cx - half, floor_y], fill=CEIL)
            d.rectangle([cx + half, floor_y - 1.2 * ppu * k, cx + half + wall, floor_y], fill=CEIL)
    else:
        d.rectangle([0, 0, w, floor_y - gap_units * ppu * k], fill=CEIL)
    for i, (frame, pivot) in enumerate(items):
        pen.cat(im, frame, pivot, (20 + (i + 0.5) * step, floor_y))
    if flipped:
        im = im.rotate(180)
        d = ImageDraw.Draw(im)
    d.text((6, 4), title + ("  (gravity flipped: root rotated 180, paws on the underside)" if flipped else ""),
           fill=(255, 255, 255, 255))
    return im


def facing_panel(sheets, facing, pen, width):
    items = [(r, sheets[r["slot"]]) for r in facing]
    cw, ch = (round(v * pen.k) for v in sheets["Idle"]["cell"])
    cw = max(round(sheets[n]["cell"][0] * pen.k) for n in ("Idle", "Walk", "Run"))
    per_row = max(1, (width - 200) // (cw + 4))
    rows = math.ceil(len(items) * 2 / per_row)
    im = Image.new("RGBA", (width, rows * (ch + 16) + 24), BG)
    d = ImageDraw.Draw(im)
    d.text((6, 4), "COLLIDER vs NOSE / TAIL TIP (Idle, Walk, Run; each frame facing right, then left). Margins in units; "
                   "negative = the collider sticks out (stop).", fill=INK)
    i = 0
    for r, sh in items:
        f = sh["frames"][r["frame"]]
        for side, flip in (("right", False), ("left", True)):
            x, y = 200 + (i % per_row) * (cw + 4), 22 + (i // per_row) * (ch + 16)
            px = f.shape[1] - sh["pivot"][0] if flip else sh["pivot"][0]
            pen.cat(im, f, sh["pivot"], (x + px * pen.k, y + sh["pivot"][1] * pen.k), flip_x=flip)
            m = r[side]
            d.text((x, y + ch), f'{r["slot"][0]}{r["frame"]}{side[0].upper()} n{m["nose"]:+.2f} t{m["tail"]:+.2f}',
                   fill=RED if min(m["nose"], m["tail"]) < 0 else INK)
            i += 1
    return im


def vine_panel(sheets, k, width):
    frames = [(n, f) for n in ("Climb", "Hang", "Leap") for f in sheets[n]["frames"]]
    tiles = [scaled(f, k) for _, f in frames]
    h = max(t.height for t in tiles)
    im = Image.new("RGBA", (max(width, 200 + sum(t.width + 6 for t in tiles)), h + 30), (238, 238, 246, 255))
    d = ImageDraw.Draw(im)
    d.text((6, 4), "VINE KEY-OUT (light background shows any leftover vine; check the fur)", fill=INK)
    x = 200
    for (n, _), t in zip(frames, tiles):
        im.alpha_composite(t, (x, 22))
        d.text((x, h + 18), n, fill=INK)
        x += t.width + 6
    return im


def body_markup(ref):
    src, _ = clip_frames("01_idle", [ref["idleFrame0Source"]])
    a = cleanup(src[0])
    m = ref["idle0"]
    z, cell = 3, a.shape[0]
    im = Image.new("RGBA", (cell * z, cell * z), BG)
    im.alpha_composite(to_image(a).resize((cell * z, cell * z), Image.NEAREST))
    d = ImageDraw.Draw(im)
    d.line([0, (m["pawRow"] + 1) * z, cell * z, (m["pawRow"] + 1) * z], fill=PAWLINE, width=2)
    d.line([0, m["backRow"] * z, cell * z, m["backRow"] * z], fill=(0, 120, 0, 255), width=2)
    for x in (m["torsoX0"], m["torsoX1"] + 1):
        d.line([x * z, 0, x * z, cell * z], fill=COLLIDER, width=2)
    d.line([ref["pivotXSource"] * z, 0, ref["pivotXSource"] * z, cell * z], fill=PAWLINE, width=1)
    c0, c1 = m["backColumns"]
    d.rectangle([c0 * z, m["backRow"] * z - 6, (c1 + 1) * z, m["backRow"] * z + 6], outline=(0, 120, 0, 255), width=1)
    d.text((8, 8), f'Idle frame 0 (01_idle f{ref["idleFrame0Source"]}, source 256 px): paw row {m["pawRow"]}, back line {m["backRow"]} '
                   f'(median over columns {c0}-{c1}), body {m["bodyHeightPx"]} px = 0.56 u: PPU {ref["ppuSource"]:.2f} '
                   f'at 256 px, {ref["ppu"]:.2f} at the 192 px base', fill=INK)
    d.text((8, 22), f'torso (blue) {m["torsoX0"]}-{m["torsoX1"]} = {(m["torsoX1"] - m["torsoX0"] + 1) / ref["ppuSource"]:.3f} u; '
                    f'shared pivot (thin red) 0.06 u behind the torso centre', fill=INK)
    return im


def climb_gif(sheet, k, fps):
    imgs = []
    for f in sheet["frames"]:
        h, w = f.shape[:2]
        im = Image.new("RGBA", (round(w * k), round(h * k)), BG)
        d = ImageDraw.Draw(im)
        d.line([sheet["pivot"][0] * k, 0, sheet["pivot"][0] * k, h * k], fill=(110, 120, 40, 255), width=3)
        im.alpha_composite(scaled(f, k))
        imgs.append(im.convert("RGB").quantize(colors=128, method=Image.MEDIANCUT, dither=Image.Dither.NONE))
    buf = io.BytesIO()
    imgs[0].save(buf, format="GIF", save_all=True, append_images=imgs[1:], duration=round(1000 / fps), loop=0,
                 optimize=False, disposal=1)
    return buf.getvalue()


def climb_previews(sheets, k):
    """The Climb loop at its slowest and full climb speed, in the 5-frame and the 25-frame version (the developer picks).
    Rate = FlipbookMath.FpsForSpeed(speed, referenceSpeed, climbFps, minFps, maxFps); climbFps keeps the source loop's
    cycle (25 frames at 14 fps, 1.79 s) at full climb speed. The slowest climb speed approaches 0 (the stick's climb value
    ramps from its dead zone), so the slowest rate is the minFps clamp."""
    cfg = "Assets/_Game/Data/CatA_VisualConfig.asset"
    min_fps, max_fps = asset_value(cfg, "minFps", 2.0), asset_value(cfg, "maxFps", 18.0)
    ref_speed = asset_value(cfg, "referenceSpeed", 6.0)
    climb_speed = asset_value("Assets/_Game/Data/CatMotorConfig_Default.asset", "climbSpeed", 4.0)
    gifs, info = {}, {"minFps": min_fps, "maxFps": max_fps, "referenceSpeed": ref_speed, "climbSpeed": climb_speed,
                      "slowestClimbSpeed": "approaches 0 (analog stick, 0.35 dead zone): the minFps clamp"}
    for name, frames in (("5", "Climb5"), ("25", "Climb")):
        n = len(sheets[frames]["frames"])
        full = 14.0 * n / 25
        climb_fps = full * ref_speed / climb_speed
        rates = {"slowest": min(max(0.0, min_fps), max_fps), "full": min(max(full, min_fps), max_fps)}
        for speed, fps in rates.items():
            gifs[f"review_climb{name}_{speed}.gif"] = climb_gif(sheets[frames], k, fps)
        info[f"frames{name}"] = {"climbFps": round(climb_fps, 3), "fullSpeedFps": round(rates["full"], 3),
                                 "slowestFps": round(rates["slowest"], 3), "cycleSecondsFull": round(n / rates["full"], 3)}
    return gifs, info


def build_review(sheets, slot_manifest, ref, facing):
    ppu = ref["ppu"]
    k1 = PHONE / ppu                  # phone scale
    phone, big = Pen(ppu, k1), Pen(ppu, 2 * k1)
    width = 2600
    blocks = slot_rows(sheets, slot_manifest, phone, width)
    pick = lambda n, i: (sheets[n]["frames"][i], sheets[n]["pivot"])
    four = [pick("Idle", 0), pick("Run", 3), pick("Rise", 1), pick("HardLand", -1)]
    two = [pick("Idle", 0), pick("HardLand", -1)]
    blocks += [
        gap_panel(four, big, 0.70, "TIGHTEST CEILING 0.70 u (L017, L012): Idle, Run, Rise, HardLand", False),
        gap_panel(four, big, 0.70, "TIGHTEST CEILING 0.70 u", True),
        gap_panel(two, big, 0.20, "TIGHTEST CLOSED CRUSHER 0.20 u (L018 Drop_3 over the Well floor; a kill)", False),
        gap_panel(two, big, 0.20, "CRUSHER 0.20 u", True),
        gap_panel([pick("Idle", 0), pick("Run", 3)], big, 0.70,
                  "PUSH WALL CLOSED 0.70 u, horizontal (L020 Push_3 against Post_P; a kill)", False, horizontal=True),
        facing_panel(sheets, facing, phone, width),
        vine_panel(sheets, 2 * k1, width),
    ]
    markup = body_markup(ref)
    blocks.append(markup)
    title_h = 40
    sheet = Image.new("RGBA", (max(b.width for b in blocks), title_h + sum(b.height + 8 for b in blocks)), (44, 36, 30, 255))
    d = ImageDraw.Draw(sheet)
    d.text((10, 8), f'PAX-A08 STAGE 0 REVIEW: Cat A, {len(slot_manifest)} slots, phone scale ({PHONE:.0f} px/u; panels 2x). '
                    f'192 px base cell, PPU {ppu:.2f} (body 0.56 u). Blue = collider 1.0 x 0.56, red = paw line.',
           fill=(255, 230, 190, 255))
    y = title_h
    for b in blocks:
        sheet.alpha_composite(b, (0, y))
        y += b.height + 8
    gifs, climb = climb_previews(sheets, 2 * k1)
    return {"sheet": sheet, "markup": markup, "gifs": gifs, "climb": climb}
