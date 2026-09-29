"""Numbers for reviewing a clip: edge clipping, drift, loop closure, best loop period. Read-only apart from printing."""
import json
import sys
from pathlib import Path

import numpy as np

sys.path.insert(0, str(Path(__file__).parent))
from cats import frames_of  # noqa: E402

ROOT = Path(__file__).resolve().parent.parent


def review(cat, clip, stem="raw_v1"):
    d = ROOT / cat / clip
    frames = frames_of(d / f"{stem}.png", d / f"{stem}_atlas.json")
    alpha = [np.asarray(f)[..., 3] for f in frames]
    masks = [a > 128 for a in alpha]
    iou = lambda a, b: (a & b).sum() / max((a | b).sum(), 1)
    boxes = [f.getbbox() or (0, 0, 0, 0) for f in frames]
    edges = [i for i, a in enumerate(alpha) if (a[:, 0] > 60).sum() + (a[:, -1] > 60).sum() + (a[0] > 60).sum() + (a[-1] > 60).sum() > 3]
    empty = [i for i, m in enumerate(masks) if m.sum() < 500]
    area = [int(m.sum()) for m in masks]
    period = None
    n = len(masks)
    scores = {p: float(np.mean([iou(masks[i], masks[i + p]) for i in range(n - p)])) for p in range(3, n // 2)} if n > 8 else {}
    if scores:
        period = max(scores, key=scores.get)
    return {
        "clip": f"{cat}/{clip}", "frames": n,
        "close_iou_last_first": round(float(iou(masks[0], masks[-1])), 3),
        "edge_frames": edges, "near_empty_frames": empty,
        "x0": [min(b[0] for b in boxes), max(b[0] for b in boxes)],
        "x1": [min(b[2] for b in boxes), max(b[2] for b in boxes)],
        "y0": [min(b[1] for b in boxes), max(b[1] for b in boxes)],
        "y1": [min(b[3] for b in boxes), max(b[3] for b in boxes)],
        "area": [min(area), max(area)],
        "period": period, "period_iou": round(scores[period], 3) if period else None,
    }


if __name__ == "__main__":
    cat = sys.argv[1]
    stem = sys.argv[2] if len(sys.argv) > 2 else "raw_v1"
    for clip_dir in sorted((ROOT / cat).iterdir()):
        if (clip_dir / f"{stem}.png").exists():
            print(json.dumps(review(cat, clip_dir.name, stem)))
