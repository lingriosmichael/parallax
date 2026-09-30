#!/usr/bin/env python3
"""PAX-V07 gauntlet item 0: contact sheets from a cat capture (Parallax.Editor.Art.CatCapture).

Usage: python3 Tools/Art/cat_capture_sheet.py <capture dir> [--only <scenario regex>]

For each scenario folder (frames.jsonl, transitions.json, scenario.json, frames/fNNNNN.png at 160 px/u) it writes to
<scenario>/sheets/:
  <scenario>__overview[_pN].png        every 6th frame (10 per second), at 160 px/u (2x phone)
  <scenario>__overview_phone[_pN].png  the same frames at 80 px/u (phone scale)
  <scenario>__tNN_fFFFFF_<kinds>.png   one per transition: its frames (8 before to 24 after; a death runs to the respawn),
                                       at 160 px/u (2x phone)
  <scenario>__strip_fFFFFF_<kind>.png  takeoffs, landings and walk-offs: a crop that follows the paws (and keeps the
                                       floor line in view while it is in range), at 320 px/u (4x phone)
Every tile is labelled "f<frame> state=<state> clip=<clip>[<index>]". The scenario name carries its gravity
(_down / _up). Nothing here measures anything: the checks are computed in C# (checks.json).
"""
import argparse
import json
import os
import re
import sys

from PIL import Image, ImageDraw, ImageFont

OVERVIEW_EVERY = 6          # frames between overview tiles (60 fps -> 10 per second)
OVERVIEW_COLUMNS, OVERVIEW_MAX_TILES = 6, 36              # the 2x overview, per part
PHONE_COLUMNS, PHONE_MAX_TILES = 8, 64                    # the phone-scale overview, per part
SHEET_COLUMNS = 6
PHONE_SCALE = 0.5           # 160 px/u captures -> 80 px/u (a phone)
STRIP_BEFORE, STRIP_AFTER = 10, 8
STRIP_COLUMNS = 6
STRIP_WINDOW_U = (2.2, 2.0)  # the paw crop, in units: width, height (wide enough for the Rise pose's tail)
STRIP_FLOOR_AT = 0.8         # the floor line sits this far down the crop when the paws are near it (gravity up: this far up)
STRIP_PAW_BAND = (0.7, 0.9)   # the paws are always kept within this band of the crop's height (gravity up: mirrored)
STRIP_ZOOM = 2              # 160 px/u -> 320 px/u
LABEL_H = 30
GAP = 4
BACK = (40, 40, 44)
TEXT = (235, 235, 235)
MARK = (230, 60, 60)


def font(size=13):
    try:
        return ImageFont.load_default(size=size)
    except TypeError:  # Pillow < 10.1
        return ImageFont.load_default()


def slug(text):
    return re.sub(r"[^A-Za-z0-9]+", "-", text).strip("-")


def load(scenario_dir):
    rows = [json.loads(line) for line in open(os.path.join(scenario_dir, "frames.jsonl")) if line.strip()]
    transitions = json.load(open(os.path.join(scenario_dir, "transitions.json")))
    meta = json.load(open(os.path.join(scenario_dir, "scenario.json")))
    return rows, transitions, meta


def label(row):
    hold = "  HOLD+outline" if row.get("holding") else ""
    return f"f{row['frame']}  state={row['state']}  clip={row['clip']}[{row['clip_frame']}]{hold}"


def tile(scenario_dir, row, scale, marked=False, crop=None):
    im = Image.open(os.path.join(scenario_dir, row["image"])).convert("RGB")
    if crop is not None:
        im = im.crop(crop)
    if scale != 1:
        im = im.resize((round(im.width * scale), round(im.height * scale)), Image.NEAREST if scale > 1 else Image.LANCZOS)
    out = Image.new("RGB", (im.width, im.height + LABEL_H), BACK)
    out.paste(im, (0, LABEL_H))
    d = ImageDraw.Draw(out)
    d.text((4, 3), label(row), fill=MARK if marked else TEXT, font=font(22 if out.width >= 500 else 17))
    if marked:
        d.rectangle([0, LABEL_H, out.width - 1, out.height - 1], outline=MARK, width=2)
    return out


def wrap(text, width_px, f):
    """Greedy word wrap to `width_px` (so no header is cut off)."""
    lines, line = [], ""
    for word in text.split(" "):
        trial = (line + " " + word).strip()
        if line and f.getlength(trial) > width_px:
            lines.append(line)
            line = word
        else:
            line = trial
    if line:
        lines.append(line)
    return lines


def grid(tiles, columns, title):
    w, h = tiles[0].width, tiles[0].height
    rows = (len(tiles) + columns - 1) // columns
    width = columns * (w + GAP) + GAP
    f = font(24)
    lines = wrap(title, width - 2 * GAP, f)
    title_h = 12 + 30 * len(lines)
    sheet = Image.new("RGB", (width, rows * (h + GAP) + GAP + title_h), BACK)
    d = ImageDraw.Draw(sheet)
    for i, line in enumerate(lines):
        d.text((GAP, 6 + 30 * i), line, fill=TEXT, font=f)
    for i, t in enumerate(tiles):
        sheet.paste(t, (GAP + (i % columns) * (w + GAP), title_h + GAP + (i // columns) * (h + GAP)))
    return sheet


def strip_crop(row, meta, surface_y):
    """The paw crop of one frame, centred on the paws horizontally. Vertically it puts the floor line the cat stands on
    around the transition (world y `surface_y`) at STRIP_FLOOR_AT, but moves as needed to keep the paws inside
    STRIP_PAW_BAND, so it follows the cat through the air and never shows an empty frame."""
    ppu = meta["render_ppu"]
    view_w, view_h = meta["view_units"]
    w, h = STRIP_WINDOW_U[0] * ppu, STRIP_WINDOW_U[1] * ppu
    x, paw_y = row["paw_img"]
    surf_y = view_h * ppu / 2 - (surface_y - row["cam"][1]) * ppu
    down = meta["gravity"] == "down"
    floor_at = STRIP_FLOOR_AT if down else 1 - STRIP_FLOOR_AT
    lo, hi = STRIP_PAW_BAND if down else (1 - STRIP_PAW_BAND[1], 1 - STRIP_PAW_BAND[0])
    top = surf_y - h * floor_at
    top = min(max(top, paw_y - h * hi), paw_y - h * lo)
    img_w, img_h = view_w * ppu, view_h * ppu
    left = min(max(0, round(x - w / 2)), round(img_w - w))
    top = min(max(0, round(top)), round(img_h - h))
    return (left, top, left + round(w), top + round(h))


def sheets_for(scenario_dir):
    rows, transitions, meta = load(scenario_dir)
    name = meta["name"]
    if not meta.get("images", True) or not rows or rows[0].get("image") is None:
        print(f"{name}: no images captured, skipped")
        return []
    out_dir = os.path.join(scenario_dir, "sheets")
    os.makedirs(out_dir, exist_ok=True)
    for f in os.listdir(out_dir):
        if f.endswith(".png"):
            os.remove(os.path.join(out_dir, f))
    written = []
    grav = meta["gravity"]

    picks = rows[::OVERVIEW_EVERY]
    holds = any(r.get("holding") for r in rows)
    note = ("; on death-hold frames a cyan outline marks the cat's silhouette (a capture overlay: "
            + (f"the game draws {len(meta.get('drawn_above_cat', []))} room sprites above the cat)" if meta.get("drawn_above_cat") else "nothing is drawn above the cat)")) if holds else ""
    grid_note = (f"; world grid 1 u, surface ticks every {meta.get('grid', {}).get('tick', 0.25)} u (capture only)"
                 "; the orange rim round the cat is the game's own outline shader; the framing is the capture tool's crop following the cat, not the level camera")
    for scale, cols, per, suffix0, scale_text in ((1, OVERVIEW_COLUMNS, OVERVIEW_MAX_TILES, "", "160 px/u (2x phone)"),
                                                  (PHONE_SCALE, PHONE_COLUMNS, PHONE_MAX_TILES, "_phone", "80 px/u (phone scale)")):
        parts = [picks[i:i + per] for i in range(0, len(picks), per)]
        for p, part in enumerate(parts, 1):
            suffix = suffix0 + (f"_p{p}" if len(parts) > 1 else "")
            title = (f"{name} (gravity {grav}) overview{f' part {p}/{len(parts)}' if len(parts) > 1 else ''}: every {OVERVIEW_EVERY}th frame "
                     f"at 60 fps, frames {part[0]['frame']}-{part[-1]['frame']}, {scale_text}{grid_note}{note}")
            path = os.path.join(out_dir, f"{name}__overview{suffix}.png")
            grid([tile(scenario_dir, r, scale) for r in part], cols, title).save(path)
            written.append(path)

    # Transitions on the same frame (a landing and Fall-to-Land) share one sheet, named for all of them.
    merged = []
    for t in transitions:
        if merged and merged[-1]["frame"] == t["frame"]:
            merged[-1]["kinds"].append(t["kind"])
        else:
            merged.append({"frame": t["frame"], "start": t["start"], "end": t["end"], "kinds": [t["kind"]]})
    for i, m in enumerate(merged):
        kind = " + ".join(m["kinds"])
        frames = rows[m["start"]:m["end"] + 1]
        title = f"{name} (gravity {grav}): {kind} at frame {m['frame']}, frames {m['start']}-{m['end']}, 160 px/u (2x phone), the change framed in red{grid_note}{note if any(r.get('holding') for r in frames) else ''}"
        path = os.path.join(out_dir, f"{name}__t{i:02d}_f{m['frame']:05d}_{slug('+'.join(m['kinds']))}.png")
        grid([tile(scenario_dir, r, 1, marked=r["frame"] == m["frame"]) for r in frames], SHEET_COLUMNS, title).save(path)
        written.append(path)
    strip_frames = set()
    for t in transitions:
        if t.get("strip") and t["frame"] not in strip_frames:   # a takeoff that is also a walk-off: one strip
            strip_frames.add(t["frame"])
            if t["kind"] == "takeoff" and any(o["frame"] == t["frame"] and o["kind"] == "walk-off" for o in transitions):
                t = dict(t, kind="walk-off")
            lo, hi = max(0, t["frame"] - STRIP_BEFORE), min(len(rows) - 1, t["frame"] + STRIP_AFTER)
            frames = rows[lo:hi + 1]
            grounded = rows[t["frame"]] if t["kind"] == "landing" else rows[max(0, t["frame"] - 1)]   # takeoff / walk-off: the frame before
            surface_y = grounded["paw"][1]
            title = f"{name} (gravity {grav}): {t['kind']} at frame {t['frame']}, the paws (crop follows the cat, floor line kept in view while in range), frames {lo}-{hi}, 320 px/u (4x phone){grid_note}"
            path = os.path.join(out_dir, f"{name}__strip_f{t['frame']:05d}_{slug(t['kind'])}.png")
            grid([tile(scenario_dir, r, STRIP_ZOOM, marked=r["frame"] == t["frame"], crop=strip_crop(r, meta, surface_y)) for r in frames],
                 STRIP_COLUMNS, title).save(path)
            written.append(path)
    print(f"{name}: {len(written)} sheets in {out_dir}")
    return written


def main(argv):
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("capture_dir")
    ap.add_argument("--only", default=".*", help="scenario name regex")
    a = ap.parse_args(argv)
    only = re.compile(a.only)
    n = 0
    for entry in sorted(os.listdir(a.capture_dir)):
        d = os.path.join(a.capture_dir, entry)
        if os.path.isfile(os.path.join(d, "frames.jsonl")) and only.search(entry):
            n += len(sheets_for(d))
    print(f"{n} sheets")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
