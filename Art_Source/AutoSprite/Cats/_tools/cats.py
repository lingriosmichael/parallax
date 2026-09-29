"""Cat clip generation for AutoSprite (generation only, no Unity). Budget-gated by ../ledger.json.

  python3 cats.py upload A                 upload the cat's base painting as a character (free)
  python3 cats.py clip A 01_idle           generate one clip (turbo, 5 credits), download, GIF, sidecar
  python3 cats.py recut A 01_idle --frames N --size S   free re-cut of the clip's last sheet
  python3 cats.py gif <sheet.png> <atlas.json> <out.gif> [--fps F]
  python3 cats.py contact                  middle frame of every clip side by side
"""
import argparse
import io
import json
import sys
import time
from datetime import datetime, timezone
from pathlib import Path

from PIL import Image, ImageDraw

sys.path.insert(0, str(Path(__file__).parent))
from autosprite_client import AutoSpriteClient  # noqa: E402  (a read-only copy of Tools/Art/autosprite_client.py)
from clips import CLIPS, STYLE_TAIL, DESCRIPTIONS  # noqa: E402

ROOT = Path(__file__).resolve().parent.parent
LEDGER = ROOT / "ledger.json"
BUDGET = 400
COST = {"turbo": 5, "pro": 10}


def now():
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def load_ledger():
    if LEDGER.exists():
        return json.loads(LEDGER.read_text())
    return {"budget": BUDGET, "spent": 0, "balanceBefore": None, "clips": {}}


def save_ledger(ledger):
    LEDGER.write_text(json.dumps(ledger, indent=2) + "\n")


def reserve(ledger, key, cost, note):
    if ledger["spent"] + cost > ledger["budget"]:
        raise SystemExit(f"STOP: {key} would bring the session to {ledger['spent'] + cost} of {ledger['budget']} credits.")
    entry = ledger["clips"].setdefault(key, {"calls": [], "credits": 0, "jobs": []})
    entry["calls"].append({"at": now(), "note": note, "credits": cost, "status": "reserved"})
    entry["credits"] += cost
    ledger["spent"] += cost
    save_ledger(ledger)
    return entry["calls"][-1]


def base_record(cat):
    return ROOT / cat / "base.json"


def cmd_upload(args):
    client = AutoSpriteClient()
    path = ROOT / "_base" / f"cat{args.cat}_side_cutout.png"
    record = base_record(args.cat)
    if record.exists():
        print(record.read_text())
        return
    result = client.create_character_from_image(f"PARALLAX Cat {args.cat}", path.read_bytes(),
                                                filename=path.name, is_humanoid=False,
                                                description=DESCRIPTIONS[args.cat])
    record.parent.mkdir(parents=True, exist_ok=True)
    data = {"cat": args.cat, "endpoint": "POST /characters (upload, free)",
            "image": str(path.relative_to(ROOT)), "characterId": result.get("id"), "created": now(),
            "response": result}
    record.write_text(json.dumps(data, indent=2) + "\n")
    print(json.dumps(data, indent=2))


def stage_sheet(client, sheet_id, directory, stem):
    sheet = client.spritesheet(sheet_id)
    png = client.download(sheet["sheetUrl"])
    with Image.open(io.BytesIO(png)) as image:
        image.load()
        fw, fh = sheet["frameWidth"], sheet["frameHeight"]
        if image.width % fw or image.height % fh:
            raise ValueError(f"sheet {sheet_id}: {image.size} is not a grid of {fw}x{fh}")
    atlas = client.download(sheet["atlasUrl"])
    json.loads(atlas)
    (directory / f"{stem}.png").write_bytes(png)
    (directory / f"{stem}_atlas.json").write_bytes(atlas)
    meta = {k: v for k, v in sheet.items() if not k.endswith("Url")}
    return directory / f"{stem}.png", directory / f"{stem}_atlas.json", meta


def frames_of(sheet_png, atlas_json):
    atlas = json.loads(Path(atlas_json).read_text())
    sheet = Image.open(sheet_png).convert("RGBA")
    rects = atlas["frames"]
    keys = sorted(rects, key=lambda k: int(k)) if isinstance(rects, dict) else range(len(rects))
    out = []
    for k in keys:
        r = rects[k]
        r = r.get("frame", r)
        out.append(sheet.crop((r["x"], r["y"], r["x"] + r["w"], r["y"] + r["h"])))
    return out


def make_gif(sheet_png, atlas_json, out, fps=12, bg=(236, 228, 214)):
    frames = []
    for f in frames_of(sheet_png, atlas_json):
        canvas = Image.new("RGBA", f.size, bg + (255,))
        d = ImageDraw.Draw(canvas)
        d.line((0, int(f.height * 0.94), f.width, int(f.height * 0.94)), fill=(190, 178, 160, 255))
        canvas.alpha_composite(f)
        frames.append(canvas.convert("P", palette=Image.ADAPTIVE))
    frames[0].save(out, save_all=True, append_images=frames[1:], duration=int(1000 / fps), loop=0, disposal=2)


def write_sidecar(directory, data):
    path = directory / "sidecar.json"
    old = json.loads(path.read_text()) if path.exists() else {}
    old.update(data)
    path.write_text(json.dumps(old, indent=2) + "\n")


def cmd_clip(args):
    spec = CLIPS[args.clip]
    record = json.loads(base_record(args.cat).read_text())
    directory = ROOT / args.cat / args.clip
    directory.mkdir(parents=True, exist_ok=True)
    tier = args.tier
    if tier != "turbo" and not args.approved_pro:
        raise SystemExit("pro needs the developer's approval (--approved-pro)")
    ledger = load_ledger()
    key = f"{args.cat}/{args.clip}"
    attempts = len([c for c in ledger["clips"].get(key, {}).get("calls", []) if c["note"].startswith("generate")])
    if attempts >= 2 and not args.force:
        raise SystemExit(f"{key}: already generated twice (one retry per clip).")
    prompt = (args.prompt or spec["prompt"]) + " " + STYLE_TAIL
    if len(prompt) > 600:
        raise SystemExit(f"{key}: prompt is {len(prompt)} characters (max 600)")
    animation = {"kind": "custom", "name": spec["name"], "prompt": prompt}
    if spec.get("loop") is not None:
        animation["loop"] = spec["loop"]
    call = reserve(ledger, key, COST[tier], f"generate v{attempts + 1} ({tier})")
    client = AutoSpriteClient()
    result = client.generate_animations(record["characterId"], [animation], frame_size=args.size,
                                        frame_count=args.frames, video_tier=tier)
    jobs = [w["jobId"] for w in result.get("workflows", [])] or [result.get("jobId")]
    call["status"] = "accepted"
    call["jobs"] = jobs
    call["creditsUsed"] = result.get("creditsUsed", result.get("credits"))
    ledger["clips"][key]["jobs"] += jobs
    save_ledger(ledger)
    fetch(client, args, spec, record, ledger, key, call, jobs, attempts + 1, tier, prompt)


def fetch(client, args, spec, record, ledger, key, call, jobs, v, tier, prompt):
    """Poll accepted jobs and stage their sheets. Re-runnable (resume) without paying again."""
    directory = ROOT / args.cat / args.clip
    sheets = []
    for job_id in jobs:
        for attempt in range(4):
            try:
                done = client.wait_for_job(job_id, timeout=900, interval=8)
                break
            except Exception as error:  # network resets while polling: the job is paid, keep polling
                if "JOB_FAILED" in str(error) or attempt == 3:
                    raise
                print(f"{key}: polling {job_id} failed ({error}); retrying", file=sys.stderr)
                time.sleep(10)
        sheets += done.get("spritesheetIds") or [s.get("id") for s in done.get("spritesheets", [])] or [done.get("spritesheetId")]
    staged = []
    for sheet_id in [s for s in sheets if s]:
        png, atlas, meta = stage_sheet(client, sheet_id, directory, f"raw_v{v}")
        make_gif(png, atlas, directory / f"preview_v{v}.gif", fps=spec.get("fps", 12))
        staged.append({"sheet": png.name, "atlas": atlas.name, "gif": f"preview_v{v}.gif", "spritesheetId": sheet_id, "meta": meta})
    # Re-read the ledger before marking, so a resume running beside a batch never overwrites newer entries.
    ledger = load_ledger()
    for c in ledger["clips"][key]["calls"]:
        if c.get("jobs") == jobs:
            c["status"] = "received"
    save_ledger(ledger)
    write_sidecar(directory, {
        "cat": args.cat, "clip": args.clip, "name": spec["name"], "kind": "custom", "loop": spec.get("loop"),
        "characterId": record["characterId"],
        f"v{v}": {"prompt": prompt, "tier": tier, "frameCount": args.frames, "frameSize": args.size,
                  "removeBg": "ultra", "jobs": jobs, "credits": COST[tier], "sheets": staged, "created": now()},
        "credits": ledger["clips"][key]["credits"],
    })
    print(json.dumps({"clip": key, "staged": staged, "session_spent": ledger["spent"]}, indent=2))


def cmd_resume(args):
    """Fetch the last accepted-but-not-received generation of a clip (free)."""
    spec = CLIPS[args.clip]
    record = json.loads(base_record(args.cat).read_text())
    ledger = load_ledger()
    key = f"{args.cat}/{args.clip}"
    gens = [c for c in ledger["clips"][key]["calls"] if c["note"].startswith("generate")]
    call = gens[-1]
    if call["status"] != "accepted":
        raise SystemExit(f"{key}: last call is {call['status']}, nothing to resume")
    (ROOT / args.cat / args.clip).mkdir(parents=True, exist_ok=True)
    prompt = (args.prompt or spec["prompt"]) + " " + STYLE_TAIL
    fetch(AutoSpriteClient(), args, spec, record, ledger, key, call, call["jobs"], len(gens),
          call["note"].split("(")[-1].rstrip(")"), prompt)


def cmd_recut(args):
    directory = ROOT / args.cat / args.clip
    side = json.loads((directory / "sidecar.json").read_text())
    versions = sorted(k for k in side if k.startswith("v") and k[1:].isdigit())
    sheet_id = args.sheet or side[versions[-1]]["sheets"][0]["spritesheetId"]
    client = AutoSpriteClient()
    job = client._request("POST", f"/spritesheets/{client._id(sheet_id)}/regenerate",
                          {"frameCount": args.frames, "frameSize": args.size, "removeBg": args.bg, "compression": "none"})
    done = client.wait_for_job(job["jobId"], timeout=600, interval=5)
    ids = done.get("spritesheetIds") or [sheet_id]
    stem = f"recut_{args.frames}f_{args.size}" + ("" if args.bg == "ultra" else f"_bg{args.bg}")
    png, atlas, meta = stage_sheet(client, ids[0], directory, stem)
    make_gif(png, atlas, directory / f"{stem}.gif", fps=args.fps)
    side.setdefault("recuts", []).append({"from": sheet_id, "frameCount": args.frames, "frameSize": args.size,
                                         "removeBg": args.bg, "sheet": png.name, "credits": 0, "created": now()})
    (directory / "sidecar.json").write_text(json.dumps(side, indent=2) + "\n")
    print(png)


def cmd_gif(args):
    make_gif(args.sheet, args.atlas, args.out, fps=args.fps)


def cmd_contact(args):
    cells = []
    for cat_dir in sorted(p for p in ROOT.iterdir() if p.is_dir() and not p.name.startswith("_")):
        for clip_dir in sorted(p for p in cat_dir.iterdir() if p.is_dir()):
            side_path = clip_dir / "sidecar.json"
            if not side_path.exists():
                continue
            side = json.loads(side_path.read_text())
            pick = side.get("chosen") or {}
            png = clip_dir / pick.get("sheet", "raw_v1.png")
            atlas = clip_dir / pick.get("atlas", "raw_v1_atlas.json")
            if not png.exists():
                continue
            frames = frames_of(png, atlas)
            cells.append((f"{cat_dir.name} {clip_dir.name}", frames[len(frames) // 2], side.get("verdict", "?")))
    if not cells:
        raise SystemExit("no clips yet")
    cell, cols = 200, 6
    rows = (len(cells) + cols - 1) // cols
    sheet = Image.new("RGBA", (cols * cell, rows * (cell + 30)), (236, 228, 214, 255))
    d = ImageDraw.Draw(sheet)
    for i, (label, frame, verdict) in enumerate(cells):
        x, y = (i % cols) * cell, (i // cols) * (cell + 30)
        sheet.alpha_composite(frame.resize((cell, cell), Image.LANCZOS), (x, y))
        d.text((x + 4, y + cell + 2), label[:30], fill=(40, 30, 20, 255))
        d.text((x + 4, y + cell + 15), str(verdict).split(":")[0], fill=(120, 60, 30, 255))
    sheet.convert("RGB").save(ROOT / "contact_sheet.png")
    print(ROOT / "contact_sheet.png")


def main():
    p = argparse.ArgumentParser()
    sub = p.add_subparsers(dest="cmd", required=True)
    s = sub.add_parser("upload"); s.add_argument("cat"); s.set_defaults(fn=cmd_upload)
    s = sub.add_parser("clip"); s.add_argument("cat"); s.add_argument("clip")
    s.add_argument("--frames", type=int, default=25); s.add_argument("--size", type=int, default=256)
    s.add_argument("--tier", default="turbo"); s.add_argument("--approved-pro", action="store_true")
    s.add_argument("--prompt"); s.add_argument("--force", action="store_true"); s.set_defaults(fn=cmd_clip)
    s = sub.add_parser("resume"); s.add_argument("cat"); s.add_argument("clip")
    s.add_argument("--frames", type=int, default=25); s.add_argument("--size", type=int, default=256)
    s.add_argument("--prompt"); s.set_defaults(fn=cmd_resume)
    s = sub.add_parser("recut"); s.add_argument("cat"); s.add_argument("clip")
    s.add_argument("--frames", type=int, required=True); s.add_argument("--size", type=int, default=256)
    s.add_argument("--fps", type=int, default=12); s.add_argument("--sheet"); s.add_argument("--bg", default="ultra")
    s.set_defaults(fn=cmd_recut)
    s = sub.add_parser("gif"); s.add_argument("sheet"); s.add_argument("atlas"); s.add_argument("out")
    s.add_argument("--fps", type=int, default=12); s.set_defaults(fn=cmd_gif)
    s = sub.add_parser("contact"); s.set_defaults(fn=cmd_contact)
    args = p.parse_args()
    args.fn(args)


if __name__ == "__main__":
    main()
