#!/usr/bin/env python3
"""PAX-A13 trap kit generation (rulings §11 R3): AutoSprite statics and animations for the trap art, one folder per asset
under Art_Source/AutoSprite/Traps/, each with a credit ledger, every raw download and a sidecar of its prompt and
parameters. The key comes only from the AUTOSPRITE_API_KEY environment variable and is never written anywhere.

  trap_kit.py account                      # free: the credit balance
  trap_kit.py plan T01 [T02 ...]           # the exact prompt, its length, the ledger and the line's ceiling
  trap_kit.py static T01 --execute         # 1 credit (ultra)
  trap_kit.py upload T07 --image PATH      # free: a character from an approved still (the animation's base)
  trap_kit.py animate T07 --execute        # 5 credits (turbo); refused since §12 R7 (no AutoSprite animation for props)
  trap_kit.py recut T07 --frames N --size S  # free: the same video cut again
"""

import argparse
import json
import sys
import time
from datetime import datetime, timezone
from pathlib import Path

from autosprite_client import AutoSpriteClient
from autosprite_pipeline import MAX_CREDITS, Ledger, stage_sheet

HERE = Path(__file__).resolve().parent
REPO = HERE.parent.parent
SPECS = HERE / "trap_specs.json"
ROOT = REPO / "Art_Source" / "AutoSprite" / "Traps"
STATIC_LIMIT, ANIMATION_LIMIT = 300, 600
COST = {"static": 1, "animation": 5}


def load_specs():
    return json.loads(SPECS.read_text())


def prompt_for(specs, asset_id):
    a = specs["assets"][asset_id]
    if a["kind"] == "static":
        return a["object"] + " " + specs["style"][a["background"]]
    return a["motion"] + " " + specs["style"]["animation"] + " " + specs["style"].get("animation_negative", "")


def folder(specs, asset_id):
    return ROOT / f"{asset_id}_{specs['assets'][asset_id]['name']}"


def check_prompt(specs, asset_id):
    a = specs["assets"][asset_id]
    text = prompt_for(specs, asset_id)
    limit = STATIC_LIMIT if a["kind"] == "static" else ANIMATION_LIMIT
    if len(text) > limit:
        raise ValueError(f"{asset_id}: prompt is {len(text)} characters, the API allows {limit}")
    return text


def reserve(specs, asset_id, operation):
    a = specs["assets"][asset_id]
    ledger = Ledger(folder(specs, asset_id) / "ledger.json")
    cost = COST[a["kind"]]
    ceiling = min(a["ceiling"], MAX_CREDITS)
    if ledger.reserved + cost > ceiling:
        raise SystemExit(f"{asset_id}: {ledger.reserved} credits reserved; {cost} more passes this line's ceiling of "
                         f"{ceiling} (§11 R3). Stop and ask the developer.")
    return ledger, ledger.reserve(operation, cost)


def next_version(directory, stem):
    n = 1
    while (directory / f"{stem}_v{n}.png").exists():
        n += 1
    return n


def sidecar(path, data):
    path.write_text(json.dumps(data, indent=2, ensure_ascii=False) + "\n")


def now():
    return datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")


def cmd_account(_args, client):
    account = client.account()
    print(json.dumps({"credits": account.get("credits")}))


def cmd_plan(args, _client):
    specs = load_specs()
    for asset_id in args.assets:
        a = specs["assets"][asset_id]
        text = check_prompt(specs, asset_id)
        ledger = Ledger(folder(specs, asset_id) / "ledger.json")
        print(f"{asset_id} {a['name']} ({a['kind']}{', base ' + a['base'] if a['kind'] == 'animation' else ''}): "
              f"{len(text)} chars, reserved {ledger.reserved} of {a['ceiling']}\n  {text}")


def cmd_static(args, client):
    specs = load_specs()
    asset_id = args.asset
    a = specs["assets"][asset_id]
    if a["kind"] != "static":
        raise SystemExit(f"{asset_id} is not a static asset")
    text = check_prompt(specs, asset_id)
    if not args.execute:
        print("dry run; add --execute to spend 1 credit:\n" + text)
        return
    directory = folder(specs, asset_id)
    directory.mkdir(parents=True, exist_ok=True)
    ledger, row = reserve(specs, asset_id, "static")
    name = f"PAX-A13 {asset_id} {a['name']}"
    result = client.create_asset(name, text, quality="ultra", use_prompt_template=False)
    ledger.complete(row, result)
    url = result.get("baseImageUrl") or client.asset(result["id"]).get("baseImageUrl")
    png = client.download(url)
    v = next_version(directory, "raw")
    raw = directory / f"raw_v{v}.png"
    raw.write_bytes(png)
    sidecar(directory / f"raw_v{v}.json", {
        "asset": asset_id, "name": name, "endpoint": "POST /assets", "quality": "ultra", "usePromptTemplate": False,
        "background": a["background"], "prompt": text, "assetId": result.get("id"),
        "creditsReserved": COST["static"], "creditsUsed": result.get("creditsUsed"), "created": now(),
    })
    print(f"{asset_id}: {raw.relative_to(REPO)} ({len(png)} bytes); ledger {ledger.reserved} of {a['ceiling']}")


def cmd_upload(args, client):
    specs = load_specs()
    asset_id = args.asset
    a = specs["assets"][asset_id]
    image = Path(args.image).read_bytes()
    directory = folder(specs, asset_id)
    directory.mkdir(parents=True, exist_ok=True)
    result = client.create_character_from_image(f"PAX-A13 {asset_id} base", image, filename=Path(args.image).name,
                                                description=specs.get("base_description", "")[:200])
    character_id = result.get("id") or result.get("characterId")
    sidecar(directory / "base.json", {"asset": asset_id, "endpoint": "POST /characters (upload, free)", "image": str(Path(args.image).resolve().relative_to(REPO)),
                                      "characterId": character_id, "created": now()})
    print(f"{asset_id}: base character {character_id}")


def cmd_animate(args, client):
    # §12 R7 (2026-09-29): trap motion is code; AutoSprite animation is kept for the cat (PAX-A08), not props. A paid call
    # needs a new approved ruling, and this line removed with it.
    if args.execute:
        raise SystemExit("animate --execute is refused: PAX-A13 §12 R7 cancelled AutoSprite animation for traps")
    specs = load_specs()
    asset_id = args.asset
    a = specs["assets"][asset_id]
    if a["kind"] != "animation":
        raise SystemExit(f"{asset_id} is not an animation")
    text = check_prompt(specs, asset_id)
    directory = folder(specs, asset_id)
    base = directory / "base.json"
    if not base.exists():
        raise SystemExit(f"{asset_id}: upload its approved base still first (trap_kit.py upload {asset_id} --image ...)")
    character_id = json.loads(base.read_text())["characterId"]
    if not args.execute:
        print(f"dry run; add --execute to spend 5 credits (turbo) on character {character_id}:\n{text}")
        return
    ledger, row = reserve(specs, asset_id, "animation")
    animations = [{"kind": "custom", "name": a["name"], "prompt": text}]
    result = client.generate_animations(character_id, animations, frame_size=a["frame_size"], frame_count=a["frame_count"], video_tier="turbo")
    ledger.complete(row, result)
    jobs = [w["jobId"] for w in result.get("workflows", [])] or [result.get("jobId")]
    sheets = []
    for job_id in jobs:
        done = client.wait_for_job(job_id, timeout=1200, interval=10)
        sheets += done.get("spritesheetIds") or [s.get("id") for s in done.get("spritesheets", [])] or [done.get("spritesheetId")]
    v = next_version(directory, "raw")
    for sheet_id in [s for s in sheets if s]:
        png = stage_sheet(client, sheet_id, directory)
        png.rename(directory / f"raw_v{v}.png")
        (directory / f"{sheet_id}.json").rename(directory / f"raw_v{v}_atlas.json")
        sidecar(directory / f"raw_v{v}.json", {
            "asset": asset_id, "endpoint": "POST /characters/{id}/spritesheets", "videoTier": "turbo", "kind": "custom",
            "characterId": character_id, "prompt": text, "frameCount": a["frame_count"], "frameSize": a["frame_size"],
            "removeBg": "ultra", "spritesheetId": sheet_id, "jobs": jobs, "creditsReserved": COST["animation"], "created": now(),
        })
        print(f"{asset_id}: raw_v{v}.png (sheet {sheet_id}); ledger {ledger.reserved} of {a['ceiling']}")
        v += 1


def cmd_recut(args, client):
    specs = load_specs()
    directory = folder(specs, args.asset)
    sheet_id = args.sheet or json.loads(sorted(directory.glob("raw_v*.json"))[-1].read_text())["spritesheetId"]
    client.regenerate_spritesheet(sheet_id, args.frames, args.size)
    time.sleep(5)
    v = next_version(directory, "raw")
    png = stage_sheet(client, sheet_id, directory)
    png.rename(directory / f"raw_v{v}.png")
    (directory / f"{sheet_id}.json").rename(directory / f"raw_v{v}_atlas.json")
    sidecar(directory / f"raw_v{v}.json", {"asset": args.asset, "endpoint": "POST /spritesheets/{id}/regenerate (free)", "spritesheetId": sheet_id,
                                           "frameCount": args.frames, "frameSize": args.size, "created": now()})
    print(f"{args.asset}: raw_v{v}.png (recut of {sheet_id})")


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = parser.add_subparsers(dest="command", required=True)
    sub.add_parser("account")
    p = sub.add_parser("plan"); p.add_argument("assets", nargs="+")
    p = sub.add_parser("static"); p.add_argument("asset"); p.add_argument("--execute", action="store_true")
    p = sub.add_parser("upload"); p.add_argument("asset"); p.add_argument("--image", required=True)
    p = sub.add_parser("animate"); p.add_argument("asset"); p.add_argument("--execute", action="store_true")
    p = sub.add_parser("recut"); p.add_argument("asset"); p.add_argument("--frames", type=int, required=True); p.add_argument("--size", type=int, required=True); p.add_argument("--sheet")
    args = parser.parse_args()
    client = AutoSpriteClient()
    {"account": cmd_account, "plan": cmd_plan, "static": cmd_static, "upload": cmd_upload, "animate": cmd_animate, "recut": cmd_recut}[args.command](args, client)


if __name__ == "__main__":
    sys.exit(main())
