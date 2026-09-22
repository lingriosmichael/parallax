#!/usr/bin/env python3
"""Plan, generate, inspect, and stage AutoSprite candidates outside Unity Assets."""

import argparse
import hashlib
import io
import json
import re
from dataclasses import dataclass
from pathlib import Path

from PIL import Image, ImageDraw

from autosprite_client import AutoSpriteClient, AutoSpriteError


ROOT = Path(__file__).resolve().parents[2]
MANIFEST = ROOT / "Docs/Art/A02_asset_manifest.md"
MAX_CREDITS = 20
PIVOTS = {"centre", "bottom centre", "top centre"}


@dataclass(frozen=True)
class Target:
    width: int
    height: int
    ppu: float
    pivot: str
    path: str


def target_for(spec, manifest=MANIFEST):
    if "slot" in spec:
        slot = spec["slot"]
        for line in manifest.read_text().splitlines():
            if not line.startswith(f"| {slot} |"):
                continue
            fields = [part.strip() for part in line.strip().strip("|").split("|")]
            match = re.fullmatch(r"(\d+) × (\d+)", fields[6])
            if not match:
                raise ValueError(f"manifest size is invalid for {slot}")
            sub = "Backgrounds/" if "_BG_" in slot or "_MG_" in slot else ""
            path = f"Assets/_Game/Art/Reality{fields[1]}/Environment/{sub}{slot}"
            target = Target(int(match[1]), int(match[2]),
                            98.333 if sub else 196.667, fields[7], path)
            break
        else:
            raise ValueError(f"slot is absent from manifest: {slot}")
    else:
        data = spec.get("target")
        if not isinstance(data, dict):
            raise ValueError("new assets require target width, height, ppu, pivot, and path")
        target = Target(data["width"], data["height"], data["ppu"],
                        data["pivot"], data["path"])
    if not (isinstance(target.width, int) and isinstance(target.height, int)
            and 1 <= target.width <= 4096 and 1 <= target.height <= 4096):
        raise ValueError("target dimensions must be positive integers at most 4096")
    if not isinstance(target.ppu, (int, float)) or target.ppu <= 0:
        raise ValueError("target PPU must be positive")
    if target.pivot not in PIVOTS:
        raise ValueError(f"unsupported pivot: {target.pivot}")
    path = Path(target.path)
    if (path.is_absolute() or ".." in path.parts or path.suffix.lower() != ".png"
            or path.parts[:3] != ("Assets", "_Game", "Art")):
        raise ValueError("target path must be a PNG under Assets/_Game/Art")
    return target


def validate_spec(spec, manifest=MANIFEST):
    kind = spec.get("type")
    if kind not in {"static", "character", "animation"}:
        raise ValueError("type must be static, character, or animation")
    target = target_for(spec, manifest)
    name = spec.get("name", "")
    if kind in {"static", "character"} and not (0 < len(name) <= 100):
        raise ValueError("name must contain 1 to 100 characters")
    prompt = spec.get("prompt", "")
    if kind in {"static", "character"}:
        max_length = 300 if kind == "static" else 600
        if not (0 < len(prompt.strip()) <= max_length):
            raise ValueError(f"prompt must contain 1 to {max_length} characters")
    if kind == "static" and spec.get("quality", "turbo") not in {"turbo", "ultra"}:
        raise ValueError("static quality must be turbo or ultra")
    if kind == "character" and spec.get("quality", "turbo") not in {"turbo", "pro"}:
        raise ValueError("character quality must be turbo or pro")
    if kind == "animation":
        animations = spec.get("animations", [])
        if not spec.get("character_id") or not 1 <= len(animations) <= 10:
            raise ValueError("animation needs character_id and 1 to 10 animations")
        if spec.get("video_tier", "turbo") not in {"turbo", "pro"}:
            raise ValueError("only soundless turbo and pro animation tiers are budgeted")
        if not 2 <= spec.get("frame_count", 25) <= 64:
            raise ValueError("frame_count must be 2 to 64")
        if not 32 <= spec.get("frame_size", 256) <= 512:
            raise ValueError("frame_size must be 32 to 512")
        if (target.width, target.height) != (spec.get("frame_size", 256),) * 2:
            raise ValueError("animation target cell dimensions must match frame_size")
        for item in animations:
            if item.get("kind") == "custom" and not (item.get("name") and
                    0 < len(item.get("prompt", "")) <= 600):
                raise ValueError("custom animation requires name and prompt up to 600 characters")
            if not item.get("kind") or any(key in item for key in
                    ("firstFrameQuality", "withSound", "poseId", "lastFramePoseId")):
                raise ValueError("animation entry has an unbudgeted option or missing kind")
    return target


def estimated_credits(spec):
    kind = spec["type"]
    if kind == "static":
        return 1
    if kind == "character":
        return 3 if spec.get("quality") == "pro" else 1
    return len(spec["animations"]) * (10 if spec.get("video_tier") == "pro" else 5)


class Ledger:
    """Reserve before POST; uncertain requests still count toward the 20-credit cap."""

    def __init__(self, path):
        self.path = Path(path)
        self.rows = json.loads(self.path.read_text()) if self.path.exists() else []

    @property
    def reserved(self):
        return sum(row["estimate"] for row in self.rows)

    def _save(self):
        self.path.parent.mkdir(parents=True, exist_ok=True)
        temporary = self.path.with_suffix(".tmp")
        temporary.write_text(json.dumps(self.rows, indent=2) + "\n")
        temporary.replace(self.path)

    def reserve(self, operation, estimate):
        if estimate < 0 or self.reserved + estimate > MAX_CREDITS:
            raise ValueError(f"20-credit cap for this asset: {self.reserved} reserved, "
                             f"next operation estimates {estimate}")
        self.rows.append({"operation": operation, "estimate": estimate, "status": "pending"})
        self._save()
        return len(self.rows) - 1

    def complete(self, index, result):
        # Keep only stable IDs, never a signed URL or key.
        self.rows[index]["status"] = "received"
        for field in ("id", "characterId", "jobId", "creditsUsed"):
            if field in result:
                self.rows[index][field] = result[field]
        if "workflows" in result:
            self.rows[index]["jobs"] = [row["jobId"] for row in result["workflows"]]
        self._save()


def workspace_for(target, work_root):
    digest = hashlib.sha256(target.path.encode("utf-8")).hexdigest()[:12]
    return Path(work_root) / (Path(target.path).stem + "_" + digest)


def _foreground(image):
    rgba = image.convert("RGBA")
    if rgba.getchannel("A").getextrema()[0] < 250:
        return rgba, "alpha"
    pixels = rgba.load()
    width, height = rgba.size
    border = [pixels[x, y][:3] for y in (0, height - 1) for x in range(width)]
    border += [pixels[x, y][:3] for x in (0, width - 1) for y in range(height)]
    white = sum(min(rgb) >= 235 for rgb in border) / len(border)
    magenta = sum(rgb[0] >= 235 and rgb[1] <= 25 and rgb[2] >= 235
                  for rgb in border) / len(border)
    if max(white, magenta) < .8:
        raise ValueError("opaque source needs a mostly white or magenta border")
    key = (255, 255, 255) if white >= magenta else (255, 0, 255)
    result = rgba.copy()
    output = result.load()
    for y in range(height):
        for x in range(width):
            rgb = pixels[x, y][:3]
            distance = max(abs(rgb[i] - key[i]) for i in range(3))
            alpha = 0 if distance < 20 else 255 if distance > 70 else round((distance - 20) * 255 / 50)
            fraction = max(alpha / 255, .02)
            unmixed = tuple(max(0, min(255, round((rgb[i] - (1 - fraction) * key[i]) / fraction)))
                            if alpha else 0 for i in range(3))
            output[x, y] = (*unmixed, alpha)
    return result, "white" if white >= magenta else "magenta"


def prepare_static(raw_bytes, target):
    try:
        source = Image.open(io.BytesIO(raw_bytes))
        source.load()
    except Exception as error:
        raise ValueError("download is not a readable image") from error
    if source.width < 16 or source.height < 16:
        raise ValueError("source image is too small")
    foreground, background = _foreground(source)
    alpha = foreground.getchannel("A")
    mask = alpha.point(lambda value: 255 if value > 25 else 0)
    box = mask.getbbox()
    if not box:
        raise ValueError("source has no visible foreground")
    crop = foreground.crop(box)
    source_aspect = crop.width / crop.height
    target_aspect = target.width / target.height
    aspect_factor = source_aspect / target_aspect
    if not .5 <= aspect_factor <= 2:
        raise ValueError(f"foreground aspect differs too much from target: {aspect_factor:.2f}×")
    scale = min(target.width / crop.width, target.height / crop.height)
    if scale > 2:
        raise ValueError(f"source needs {scale:.2f}× upscaling; request a sharper source")
    size = (max(1, round(crop.width * scale)), max(1, round(crop.height * scale)))
    resized = crop.resize(size, Image.Resampling.LANCZOS)
    result = Image.new("RGBA", (target.width, target.height))
    x = (target.width - size[0]) // 2
    y = 0 if target.pivot == "top centre" else target.height - size[1] if target.pivot == "bottom centre" else (target.height - size[1]) // 2
    result.alpha_composite(resized, (x, y))
    if result.size != (target.width, target.height) or not result.getchannel("A").getbbox():
        raise ValueError("processed image failed size or alpha validation")
    metrics = {"raw_size": source.size, "crop": box, "content_size": size,
               "target_size": result.size, "world_units": (round(target.width / target.ppu, 3),
                round(target.height / target.ppu, 3)), "scale": round(scale, 3),
               "aspect_factor": round(aspect_factor, 3), "background": background,
               "review_aspect": not .7 <= aspect_factor <= 1.4}
    return result, metrics


def contact_sheet(raw_bytes, processed):
    raw = Image.open(io.BytesIO(raw_bytes)).convert("RGBA")
    sheet = Image.new("RGB", (900, 360), (72, 72, 72))
    draw = ImageDraw.Draw(sheet)
    for index, image in enumerate((raw, processed)):
        preview = image.copy()
        preview.thumbnail((420, 300), Image.Resampling.LANCZOS)
        x = 15 + 450 * index + (420 - preview.width) // 2
        y = 35 + (300 - preview.height) // 2
        draw.text((15 + 450 * index, 8), "raw" if index == 0 else "game size", fill="white")
        sheet.paste(preview, (x, y), preview)
    return sheet


def stage_static(raw_bytes, target, directory, number):
    directory.mkdir(parents=True, exist_ok=True)
    raw_path = directory / f"raw_v{number}.png"
    try:
        source = Image.open(io.BytesIO(raw_bytes)).convert("RGBA")
    except Exception as error:
        raise ValueError("download is not a readable image") from error
    source.save(raw_path, format="PNG")
    normalized = raw_path.read_bytes()
    processed, metrics = prepare_static(normalized, target)
    candidate_path = directory / f"candidate_v{number}.png"
    processed.save(candidate_path)
    contact_sheet(normalized, processed).save(directory / f"contact_v{number}.png")
    return candidate_path, metrics


def stage_sheet(client, sheet_id, directory, expected_frame_size=None):
    sheet = client.spritesheet(sheet_id)
    frame_width = sheet["frameWidth"]
    frame_height = sheet["frameHeight"]
    if expected_frame_size and (frame_width, frame_height) != (expected_frame_size, expected_frame_size):
        raise ValueError("sheet frame size differs from request")
    png = client.download(sheet["sheetUrl"])
    with Image.open(io.BytesIO(png)) as image:
        image.load()
        if (image.format != "PNG" or image.width % frame_width
                or image.height % frame_height or image.width // frame_width != sheet["columns"]
                or (image.width // frame_width) * (image.height // frame_height)
                < sheet["frameCount"]):
            raise ValueError("invalid downloaded spritesheet grid")
    atlas = client.download(sheet["atlasUrl"])
    try:
        json.loads(atlas)
    except (ValueError, UnicodeError) as error:
        raise ValueError("downloaded atlas is not valid JSON") from error
    directory.mkdir(parents=True, exist_ok=True)
    path = directory / f"{sheet_id}.png"
    path.write_bytes(png)
    (directory / f"{sheet_id}.json").write_bytes(atlas)
    return path


def _load_spec(path):
    spec = json.loads(Path(path).read_text())
    target = validate_spec(spec)
    return spec, target


def _run(args):
    spec, target = _load_spec(args.spec)
    directory = workspace_for(target, args.work_root)
    ledger = Ledger(directory / "ledger.json")
    cost = estimated_credits(spec)
    if args.command == "plan":
        print(json.dumps({"target": target.__dict__, "estimated_next_credits": cost,
                          "reserved_credits": ledger.reserved, "cap": MAX_CREDITS}, indent=2))
        return
    if args.command == "prepare":
        if spec["type"] != "static":
            raise ValueError("prepare accepts a static asset spec")
        output, metrics = stage_static(Path(args.raw).read_bytes(), target, directory,
                                       len(ledger.rows) + 1)
        print(json.dumps({"candidate": str(output), "metrics": metrics}, indent=2))
        return
    client = AutoSpriteClient()
    if args.command == "account":
        print(json.dumps(client.account(), indent=2))
        return
    if args.command == "sheet":
        if spec["type"] != "animation" or not args.sheet_id:
            raise ValueError("sheet command requires an animation spec and --sheet-id")
        path = stage_sheet(client, args.sheet_id, directory, spec.get("frame_size", 256))
        print(json.dumps({"sheet": str(path), "added_credits": 0}, indent=2))
        return
    if not args.execute:
        raise ValueError("generation or regeneration requires --execute")
    if args.command == "static":
        if spec["type"] != "static":
            raise ValueError("static command requires static spec")
        existing = next((row for row in reversed(ledger.rows)
                         if row["operation"] == "static" and row.get("id")
                         and not (directory / f"raw_v{ledger.rows.index(row) + 1}.png").exists()), None)
        if existing:
            index = ledger.rows.index(existing)
            result = client.asset(existing["id"])
        else:
            index = ledger.reserve("static", cost)
            name = f"{spec['name']} v{index + 1}"
            if len(name) > 100:
                name = name[:100]
            result = client.create_asset(name, spec["prompt"], spec.get("quality", "turbo"),
                                         spec.get("use_prompt_template", True))
            ledger.complete(index, result)
        raw = client.download(result["baseImageUrl"])
        output, metrics = stage_static(raw, target, directory, index + 1)
        print(json.dumps({"asset_id": result["id"], "candidate": str(output),
                          "metrics": metrics, "reserved_credits": ledger.reserved}, indent=2))
        return
    if args.command == "character":
        if spec["type"] != "character":
            raise ValueError("character command requires character spec")
        index = ledger.reserve("character", cost)
        result = client.create_character(spec["name"], spec["prompt"], spec.get("quality", "turbo"))
        ledger.complete(index, result)
        print(json.dumps({"character_id": result["id"], "reserved_credits": ledger.reserved}, indent=2))
        return
    if args.command == "animation":
        if spec["type"] != "animation":
            raise ValueError("animation command requires animation spec")
        index = ledger.reserve("animation", cost)
        result = client.generate_animations(spec["character_id"], spec["animations"],
                                            spec.get("frame_size", 256),
                                            spec.get("frame_count", 25),
                                            spec.get("video_tier", "turbo"))
        ledger.complete(index, result)
        sheets = []
        for workflow in result.get("workflows", []):
            job = client.wait_for_job(workflow["jobId"])
            for sheet_id in job.get("spritesheetIds", []):
                sheets.append(str(stage_sheet(client, sheet_id, directory,
                                              spec.get("frame_size", 256))))
        print(json.dumps({"sheets": sheets, "reserved_credits": ledger.reserved}, indent=2))
        return
    if args.command == "regenerate":
        if spec["type"] != "animation":
            raise ValueError("regenerate command requires animation spec")
        if not args.sheet_id or spec.get("frame_count", 25) < 4:
            raise ValueError("regenerate requires --sheet-id and frame_count of at least 4")
        job = client.regenerate_spritesheet(args.sheet_id, spec.get("frame_count", 25),
                                            spec.get("frame_size", 256))
        result = client.wait_for_job(job["jobId"])
        paths = [str(stage_sheet(client, sheet_id, directory, spec.get("frame_size", 256)))
                 for sheet_id in result.get("spritesheetIds", [])]
        print(json.dumps({"sheets": paths,
                          "added_credits": 0}, indent=2))


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("command", choices=("plan", "prepare", "account", "static", "character",
                                            "animation", "sheet", "regenerate"))
    parser.add_argument("--spec", required=True)
    parser.add_argument("--work-root", type=Path, default=ROOT / "Art_Source/AutoSprite")
    parser.add_argument("--raw", type=Path)
    parser.add_argument("--sheet-id")
    parser.add_argument("--execute", action="store_true")
    args = parser.parse_args()
    try:
        _run(args)
    except (AutoSpriteError, ValueError, KeyError, OSError) as error:
        parser.exit(1, f"{error}\n")


if __name__ == "__main__":
    main()
