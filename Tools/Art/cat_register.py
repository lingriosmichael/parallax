#!/usr/bin/env python3
"""PAX-A08 Stage 0: register the Cat A AutoSprite clips into import-ready slot sheets. Offline (no Unity, no AutoSprite
call) and deterministic: the same input gives the same bytes.

Reads `Art_Source/AutoSprite/Cats/A/_import/slots.json` (one entry per slot: source clip, frames or an even loop, a
placement role) and each clip's `chosen` sheet and atlas from its sidecar. Writes into `_import/`:
  - `sheets/CatA_<Slot>.png`: the frames left to right in 256 px cells, one shared scale and pivot;
  - `sheets/CatA_<Slot>_n.png`: its normal map (the trap kit's `trap_process.normal_map`);
  - `manifest.json`: PPU, pivot, body height, and per slot the frames, loop closing IoU, clipped pixels, memory;
  - `review.png`, `review_climb_slowest.gif` and `review_climb_typical.gif` for the developer's review.

Scale (PAX-A08 §3.1, option B): PPU = the standing body height on Idle frame 0 (paw row to back line) / 0.56.

    python3 cat_register.py            # write everything
    python3 cat_register.py --check    # regenerate in memory and compare with the files on disk
"""

import json
import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).resolve().parent))
from cat_frames import *  # noqa: E402,F401,F403  (re-exported: the tests use cat_register's frame functions)
from cat_frames import (ALPHA_FLOOR, BLEED_PX, CATS, CELL, COLLIDER_H, COLLIDER_W, LOOP_CLOSE_MIN, OUT, REPO,  # noqa: E402
                        body_centre, cleanup, clip_frames, collider_margins, key_vine, mask, mask_iou, measure_body,
                        normal_frames, pick_loop_count, place, png_bytes, scale_frame, sheet_image, slot_indices,
                        astc_bytes, bleed, cell_for, downscale, touches_edge)
from cat_review import build_review  # noqa: E402
from cat_consistency import detached_islands, drop_specks, luma_quantiles, match_style, reach, torso_scale  # noqa: E402
from cat_inbetween import contact_frame, roll_inbetweens  # noqa: E402


# ---------- the whole set ----------

FINAL_CELL = 192                    # the base cell after scaling (developer, 2026-09-30); PPU scales with it
CELL_SCALE = FINAL_CELL / CELL      # 0.75
NOSE_BACK_UNITS = 0.06              # the shared pivot sits 0.06 u behind the torso centre, so the collider stays inside the nose
WORK_PAD = CELL                     # the working canvas has a whole source cell of room on every side, so nothing is clipped
ASTC_BLOCK = 6
STYLE_REFERENCE = "Walk"            # PAX-A14 §3 A: every clip's tones are matched to Walk's (slot "style": false opts out)
REACH_BAND_PX = 20                  # PAX-A14: a death clip's reach is measured on its lowest 20 source px (about 0.1 u)


def load_sources(spec):
    sources = {}
    for s in spec["slots"]:
        idx = slot_indices(s)
        frames, sheet = clip_frames(s["clip"], idx)
        frames = [cleanup(f) for f in frames]
        if s.get("vine"):
            frames = [cleanup(key_vine(f)) for f in frames]
        sources[s["slot"]] = {"indices": idx, "sheet": sheet, "frames": frames,
                              "edgeTouch": any(touches_edge(f) for f in frames)}
    return sources


def register(preview_climb_loop=5):
    """Everything Stage 0 writes, as {relative path: bytes}, plus the placed sheets and the reference values. Frames are
    placed at source scale in a large working canvas (the shared pivot in its middle), scaled to the 192 px base, then
    cropped to each sheet's cell (the base cell, grown where a clip reaches past it)."""
    spec = json.loads((OUT / "slots.json").read_text())
    slots = {s["slot"]: s for s in spec["slots"]}
    climb = dict(slots["Climb"], slot="Climb5", loop=dict(slots["Climb"]["loop"], target=preview_climb_loop))
    sources = load_sources({"slots": spec["slots"] + [climb]})   # Climb5: a preview only, not a sheet
    slots["Climb5"] = climb

    # Scale (option B): the standing body height on Idle frame 0 is 0.56 u (at source scale).
    idle0 = measure_body(sources["Idle"]["frames"][0])
    ppu_src = idle0["bodyHeightPx"] / COLLIDER_H
    torso_cx = float(np.median([measure_body(f)["torsoCX"] for f in sources["Idle"]["frames"]]))
    pivot_x = torso_cx - NOSE_BACK_UNITS * ppu_src
    paw_row = max(measure_body(f)["pawRow"] for f in sources["Idle"]["frames"])

    # PAX-A14 §3 A: style first (each clip's luma distribution onto Walk's; alpha untouched), then scale: a standing
    # frame's body height (option B, "scaleFromFrame") or the torso length against another clip ("scaleByTorso": Run's
    # gallop is drawn low, so its body height can't be the rule).
    style_ref = luma_quantiles(sources[STYLE_REFERENCE]["frames"])
    for name, src in sources.items():
        src["styled"] = name != STYLE_REFERENCE and slots[name].get("style", True)
        if src["styled"]:
            q = luma_quantiles(src["frames"])
            src["frames"] = [match_style(f, q, style_ref) for f in src["frames"]]
    scales = {}
    for name, src in sources.items():
        factor = 1.0
        if "scaleFromFrame" in slots[name]:
            ref, _ = clip_frames(slots[name]["clip"], [slots[name]["scaleFromFrame"]])
            factor = idle0["bodyHeightPx"] / measure_body(cleanup(ref[0]))["bodyHeightPx"]
        elif "scaleByTorso" in slots[name]:
            factor = torso_scale(sources[slots[name]["scaleByTorso"]]["frames"], src["frames"])
        if factor != 1.0:
            src["frames"] = [scale_frame(f, factor) for f in src["frames"]]
        scales[name] = factor

    work = CELL + 2 * WORK_PAD
    wx, wy = pivot_x + WORK_PAD, paw_row + WORK_PAD
    placed, reports = {}, {}
    order = ["Idle", "Walk"] + [n for n in sources if n not in ("Idle", "Walk")]
    air_target = climb_target = None
    for name in order:
        # Ground clips put their torso centre 0.06 u ahead of the pivot, so the collider (centred on the pivot) sits back.
        torso_target = wx + NOSE_BACK_UNITS * ppu_src
        frames, rep = place(sources[name]["frames"], slots[name]["role"], torso_target, wy, air_target, climb_target,
                            (work, work))
        if "reachUnits" in slots[name]:
            # PAX-A14 §3 A.4: re-registered along the facing so the reach frame's lowest band ends `reachUnits` ahead of
            # the pivot (the death clip's paws meet what killed the cat).
            k = slots[name].get("reachFrame", 0)
            dx = int(round((slots[name]["reachUnits"] - reach(frames[k], wx, ppu_src, REACH_BAND_PX)) * ppu_src))
            frames = [shift_into_cell(f, dx, 0, (work, work))[0] for f in frames]
            rep["reachShiftUnits"] = round(dx / ppu_src, 3)
        placed[name], reports[name] = frames, rep
        if name == "Walk":
            cs = [body_centre(f) for f in frames]
            air_target = (float(np.median([c[0] for c in cs])), float(np.median([c[1] for c in cs])))
            climb_target = (wx, wy + 0.5 - COLLIDER_H / 2.0 * ppu_src)

    # PAX-A14 §3 B: the bridge frames, made from the registered frames (own sheet per seam, a slot with `bridgeFor`).
    for b in spec.get("bridges", []):
        placed[b["slot"]] = bridge_frames(b, placed[b["bridgeFor"]], wy, ppu_src)

    # Scale to the 192 px base and crop each sheet to its cell around the shared world pivot.
    ppu = ppu_src * CELL_SCALE
    pivot = (wx * CELL_SCALE, (wy + 0.5) * CELL_SCALE)
    base_pivot = (pivot_x * CELL_SCALE, (paw_row + 0.5) * CELL_SCALE)
    sheets = {}
    for name, frames in placed.items():
        small = [downscale(f, CELL_SCALE) for f in frames]
        ox, oy, w, h = cell_for(small, pivot, base_pivot, FINAL_CELL, block=ASTC_BLOCK)
        cropped = [f[oy:oy + h, ox:ox + w] for f in small]
        specks = sum(detached_islands(f) for f in cropped)
        cropped = [drop_specks(f) for f in cropped]      # PAX-A14 §3 A.3: detached fur fringe, never drawn
        sheets[name] = {"frames": cropped, "pivot": (pivot[0] - ox, pivot[1] - oy), "cell": (w, h), "specksRemoved": specks}

    files, slot_manifest = {}, []
    for s in spec["slots"]:
        name = s["slot"]
        sh = sheets[name]
        frames = sh["frames"]
        colour, normal = sheet_image([bleed(f) for f in frames]), sheet_image(normal_frames(frames))
        files[f"sheets/CatA_{name}.png"] = png_bytes(colour)
        files[f"sheets/CatA_{name}_n.png"] = png_bytes(normal)
        w, h = sh["cell"]
        entry = {
            "slot": name, "file": f"CatA_{name}.png", "sourceClip": s["clip"], "sourceSheet": sources[name]["sheet"],
            "sourceFrames": sources[name]["indices"], "frames": len(frames), "role": s["role"],
            "loop": "loop" in s, "scale": round(scales[name], 4),
            "cell": [w, h], "pivotPx": [round(sh["pivot"][0], 3), round(sh["pivot"][1], 3)],
            "pivotNormalized": [round(sh["pivot"][0] / w, 5), round(1.0 - sh["pivot"][1] / h, 5)],
            "clippedPx": reports[name]["clippedPx"], "sourceCutAtCellEdge": sources[name]["edgeTouch"],
            "vineKeyed": bool(s.get("vine")), "wired": s.get("wired", True), "sheetSize": [colour.width, colour.height],
            "styleMatched": sources[name]["styled"], "specksRemoved": sh["specksRemoved"],
            "detachedIslands": sum(detached_islands(f) for f in frames),
            "astc6x6Bytes": astc_bytes(colour.width, colour.height, ASTC_BLOCK),
        }
        if "loop" in s:
            lp = s["loop"]
            entry["loopStride"] = lp["length"] // pick_loop_count(lp["length"], lp["target"])
            entry["loopSource"] = [lp["start"], lp["start"] + lp["length"] - 1]
            entry.update(loop_check(frames))
        if "reachShiftUnits" in reports[name]:
            entry["reachUnits"] = s["reachUnits"]
            entry["reachShiftUnits"] = reports[name]["reachShiftUnits"]
        if "scaleByTorso" in s:
            entry["scaleRule"] = f"torso length = {s['scaleByTorso']}'s"
        if "note" in s:
            entry["note"] = s["note"]
        slot_manifest.append(entry)

    for b in spec.get("bridges", []):
        host = next(e for e in slot_manifest if e["slot"] == b["bridgeFor"])
        slot_manifest.append(bridge_entry(b, host, sheets[b["slot"]], files))

    return spec, files, sheets, slot_manifest, {"idle0": idle0, "ppu": ppu, "ppuSource": ppu_src, "torsoCX": torso_cx,
                                                "pivotXSource": pivot_x, "pawRow": paw_row,
                                                "idleFrame0Source": sources["Idle"]["indices"][0]}


def bridge_frames(b, host, paw_row, ppu_src):
    """PAX-A14 §3 B: a bridge's frames from its host clip's registered frames (source scale, the working canvas).
    - contact: the host's frame `frame` raised `raiseUnits` over its planted paws, its legs part extended over `legUnits`;
    - roll: for each [k, n] in `between`, n cels turned and moved evenly from host frame k to frame k + 1."""
    if b["kind"] == "contact":
        return [contact_frame(host[b["frame"]], paw_row, b["raiseUnits"] * ppu_src, b["legUnits"] * ppu_src)]
    if b["kind"] == "roll":
        return [f for k, n in b["between"] for f in roll_inbetweens(host[k], host[k + 1], n)]
    raise ValueError(f"unknown bridge kind '{b['kind']}'")


def bridge_entry(b, host, sheet, files):
    """A bridge's manifest entry: imported like any sheet, not wired on its own (its frames play inside `bridgeFor`'s
    clip, at `bridgePositions` in the combined clip)."""
    frames = sheet["frames"]
    colour, normal = sheet_image([bleed(f) for f in frames]), sheet_image(normal_frames(frames))
    files[f"sheets/CatA_{b['slot']}.png"] = png_bytes(colour)
    files[f"sheets/CatA_{b['slot']}_n.png"] = png_bytes(normal)
    w, h = sheet["cell"]
    if len(b["positions"]) != len(frames):
        raise ValueError(f"{b['slot']}: {len(frames)} frames for {len(b['positions'])} positions")
    return {
        "slot": b["slot"], "file": f"CatA_{b['slot']}.png", "bridgeFor": b["bridgeFor"], "bridgePositions": b["positions"],
        "kind": b["kind"], "sourceClip": host["sourceClip"], "sourceSheet": host["sourceSheet"],
        "sourceFrames": host["sourceFrames"], "frames": len(frames), "role": host["role"], "loop": False,
        "scale": host["scale"], "cell": [w, h], "pivotPx": [round(sheet["pivot"][0], 3), round(sheet["pivot"][1], 3)],
        "pivotNormalized": [round(sheet["pivot"][0] / w, 5), round(1.0 - sheet["pivot"][1] / h, 5)], "clippedPx": 0,
        "sourceCutAtCellEdge": False, "vineKeyed": False, "wired": False, "sheetSize": [colour.width, colour.height],
        "astc6x6Bytes": astc_bytes(colour.width, colour.height, ASTC_BLOCK), "styleMatched": host["styleMatched"],
        "specksRemoved": sheet["specksRemoved"], "detachedIslands": sum(detached_islands(f) for f in frames),
        "note": b["note"]}


def loop_check(frames):
    """A loop is flagged only when its closing step (last frame into the first) is below its lowest ordinary step
    (ruled 2026-09-30): fast motion sampled at a stride has low step IoUs everywhere, and that isn't a hitch."""
    steps = [mask_iou(mask(frames[i]), mask(frames[i + 1])) for i in range(len(frames) - 1)]
    close = mask_iou(mask(frames[-1]), mask(frames[0]))
    return {"loopCloseIoU": round(close, 3), "loopStepIoU": [round(min(steps), 3), round(max(steps), 3)] if steps else None,
            "loopFlag": bool(steps) and close < min(steps)}


def facing_check(sheets, ppu):
    rows = []
    for name in ("Idle", "Walk", "Run"):
        px = sheets[name]["pivot"][0]
        for i, f in enumerate(sheets[name]["frames"]):
            r = collider_margins(f, px, ppu, True)
            l = collider_margins(f[:, ::-1], f.shape[1] - px, ppu, False)
            rows.append({"slot": name, "frame": i, "right": {k: round(v, 3) for k, v in r.items()},
                         "left": {k: round(v, 3) for k, v in l.items()}})
    worst = min(min(r["right"]["nose"], r["right"]["tail"], r["left"]["nose"], r["left"]["tail"]) for r in rows)
    return rows, worst


def consistency_report(sheets, slot_manifest):
    """PAX-A14 §3 A's numbers: the median body height of Idle, Walk and Run (sheet px) and what the pass changed."""
    heights = {n: float(np.median([measure_body(f)["bodyHeightPx"] for f in sheets[n]["frames"]])) for n in ("Idle", "Walk", "Run")}
    return {"ticket": "PAX-A14 §3 A", "styleReference": STYLE_REFERENCE, "bodyHeightMedianPx": heights,
            "bodyHeightSpread": round((max(heights.values()) - min(heights.values())) / heights["Idle"], 4),
            "styleMatched": [e["slot"] for e in slot_manifest if e["styleMatched"]],
            "specksRemoved": sum(e["specksRemoved"] for e in slot_manifest),
            "detachedIslands": sum(e["detachedIslands"] for e in slot_manifest)}


def memory_report(slot_manifest, sheets):
    colour = sum(e["astc6x6Bytes"] for e in slot_manifest)
    climb = next(e["astc6x6Bytes"] for e in slot_manifest if e["slot"] == "Climb")
    mb = lambda b: round(b / 2**20, 2)
    return {"block": "6x6", "colourMB": mb(colour), "normalsMB": mb(colour), "totalMB": mb(2 * colour),
            "climbSheetWithNormalsMB": mb(2 * climb)}


def camera_report(ppu):
    """The cat's on-screen size in the level cameras (PAX-A08 §6): px per unit = screen height / view height, where the
    view is at most MaxViewHeight tall and fits the level's camera frame at the aspect."""
    import re
    cfg = (REPO / "Assets/_Game/Data/LevelCameraConfig.asset").read_text()
    max_view = float(re.search(r"maxViewHeight: ([\d.]+)", cfg).group(1))
    levels = []
    for scene in sorted((REPO / "Assets/_Game/Scenes/Levels").glob("Level_0[0-2][0-9].unity")):
        m = re.search(r"frameSize: \{x: ([\d.]+), y: ([\d.]+)\}", scene.read_text())
        if not m:
            continue
        fx, fy = float(m.group(1)), float(m.group(2))
        for aspect_name, aspect in (("20:9", 20 / 9), ("16:9", 16 / 9)):
            view = min(max_view, fy, fx / aspect)
            levels.append({"level": scene.stem, "aspect": aspect_name, "view": round(view, 2), "pxPerUnit": 1080 / view})
    px = [l["pxPerUnit"] for l in levels]
    return {"maxViewHeight": max_view, "screenHeight": 1080, "levels": len(levels) // 2,
            "pxPerUnitMin": round(min(px), 1), "pxPerUnitMax": round(max(px), 1),
            "baseCellPxOnScreenMin": round(FINAL_CELL / ppu * min(px), 1),
            "baseCellPxOnScreenMax": round(FINAL_CELL / ppu * max(px), 1)}



def main(argv):
    check = "--check" in argv
    spec, files, sheets, slot_manifest, ref = register()
    ppu, idle0 = ref["ppu"], ref["idle0"]
    facing, worst = facing_check(sheets, ppu)
    torso_u = (idle0["torsoX1"] - idle0["torsoX0"] + 1) / ref["ppuSource"]

    review = build_review(sheets, slot_manifest, ref, facing)
    files["review.png"] = png_bytes(review["sheet"])
    files["review_body_height.png"] = png_bytes(review["markup"])
    for name, gif in review["gifs"].items():
        files[name] = gif

    manifest = {
        "ticket": "PAX-A08 Stage 0; PAX-A14 consistency", "scaleRule": "option B: standing body height on Idle frame 0 = 0.56 u",
        "baseCell": FINAL_CELL, "sourceCell": CELL, "cellScale": CELL_SCALE,
        "idleFrame0Source": ref["idleFrame0Source"], "bodyHeightPxSource": idle0["bodyHeightPx"], "ppuSource": round(ref["ppuSource"], 4), "ppu": round(ppu, 4),
        "pawRowSource": ref["pawRow"], "backRowSource": idle0["backRow"], "backColumnsSource": idle0["backColumns"],
        "torsoPxSource": [idle0["torsoX0"], idle0["torsoX1"]], "torsoLengthUnits": round(torso_u, 3),
        "pivotRule": f"the shared world pivot: the paw row, {NOSE_BACK_UNITS} u behind Idle's median torso centre; each "
                     "sheet records where it falls in its own cell",
        "colliderUnits": [COLLIDER_W, COLLIDER_H],
        "colourBleedPx": BLEED_PX,
        "facingCheck": {"worstMarginUnits": round(worst, 3), "passes": worst >= 0, "frames": facing},
        "ceilingGap": {"units": 0.70, "where": "L017 UF_W under Door_Ledge; L012 F4_B under Stub_C (pax-room-auditor 2026-09-30)"},
        "crusherGaps": {"vertical": {"units": 0.20, "where": "L018 Drop_3 over the Well floor"},
                        "horizontal": {"units": 0.70, "where": "L020 Push_3 against Post_P"},
                        "fallingBlocks": "every falling block closes to 0.00 (a kill)"},
        "climbPreview": review["climb"],
        "consistency": consistency_report(sheets, slot_manifest),
        "memoryASTC": memory_report(slot_manifest, sheets),
        "onScreen": camera_report(ppu),
        "slots": slot_manifest,
    }
    files["manifest.json"] = (json.dumps(manifest, indent=2) + "\n").encode()

    if check:
        diffs = [p for p, b in files.items() if not (OUT / p).exists() or (OUT / p).read_bytes() != b]
        print("no diff: %d files identical" % len(files) if not diffs else "DIFF: " + ", ".join(diffs))
        return 1 if diffs else 0
    for p, b in files.items():
        (OUT / p).parent.mkdir(parents=True, exist_ok=True)
        (OUT / p).write_bytes(b)
    print(f"{len(files)} files written to {OUT.relative_to(REPO)}; PPU {ppu:.2f} (192 px base), torso {torso_u:.3f} u, "
          f"worst collider margin {worst:+.3f} u, ASTC 6x6 {manifest['memoryASTC']['totalMB']} MB with normals")
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
