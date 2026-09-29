"""Find the best closing loop in a clip (IoU of alpha masks), and write a loop GIF + strip. Free, local."""
import sys
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw
sys.path.insert(0, str(Path(__file__).parent))
from cats import frames_of

ROOT = Path(__file__).resolve().parent.parent
import os
CAT = os.environ.get("CAT", "A")


def best_loop(frames, pmin, pmax, skip=0):
    m = [np.asarray(f)[..., 3] > 128 for f in frames]
    iou = lambda a, b: (a & b).sum() / max((a | b).sum(), 1)
    best = None
    for p in range(pmin, pmax + 1):
        for i in range(skip, len(m) - p):
            s = iou(m[i], m[i + p])
            # prefer the shortest period that closes well: penalise longer periods slightly
            score = s - 0.002 * p
            if best is None or score > best[0]:
                best = (score, i, p, s)
    return best[1], best[2], round(float(best[3]), 3)


def write(clip, stem, start, period, fps, tag="loop"):
    d = ROOT / CAT / clip
    frames = frames_of(d / f"{stem}.png", d / f"{stem}_atlas.json")[start:start + period]
    out = []
    for f in frames:
        c = Image.new("RGBA", f.size, (236, 228, 214, 255)); c.alpha_composite(f)
        out.append(c.convert("P", palette=Image.ADAPTIVE))
    name = f"{tag}{period}_f{start}-{start + period - 1}_{fps}fps"
    out[0].save(d / f"{name}.gif", save_all=True, append_images=out[1:], duration=int(1000 / fps), loop=0, disposal=2)
    strip = Image.new("RGBA", (frames[0].width * len(frames), frames[0].height))
    for i, f in enumerate(frames):
        strip.alpha_composite(f, (i * f.width, 0))
    strip.save(d / f"{name}_strip.png")
    return name


if __name__ == "__main__":
    clip, stem, pmin, pmax, fps = sys.argv[1], sys.argv[2], int(sys.argv[3]), int(sys.argv[4]), int(sys.argv[5])
    skip = int(sys.argv[6]) if len(sys.argv) > 6 else 0
    d = ROOT / CAT / clip
    frames = frames_of(d / f"{stem}.png", d / f"{stem}_atlas.json")
    i, p, s = best_loop(frames, pmin, pmax, skip)
    print(clip, stem, "frames", len(frames), "loop start", i, "period", p, "closing IoU", s, write(clip, stem, i, p, fps))
